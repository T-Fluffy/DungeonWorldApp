using System.Text;
using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using Tesseract;

namespace DungeonWorld.Infrastructure.Parsing.Reconstruction;

/// <summary>
/// Shared manual-reconstruction pipeline used by the batch CLI (--reconstruct /
/// --reconstruct-apply) and by the per-book manifest parsers. Renders PDF pages at
/// a given dpi, OCRs per-page line transcripts, then applies an ordered manifest of
/// {n, page, side, line, [end]} body-start points to assemble the book's sections.
/// </summary>
public static class ReconstructionService
{
    /// <summary>A single OCR'd transcript line on a page.</summary>
    public sealed record OcrLine(int Page, int N, string Side, double Top, string Text);

    /// <summary>One manifest body-start point: section <paramref name="Number"/> begins at line
    /// <paramref name="Line"/> on page <paramref name="Page"/>'s <paramref name="Side"/>. An optional
    /// <paramref name="EndLine"/> caps the section before the next entry when back-matter follows.</summary>
    public sealed record ManifestEntry(int Number, int Page, string Side, int Line, int? EndLine);

    /// <summary>
    /// Renders every requested page of the PDF at <paramref name="dpi"/> and OCRs the content
    /// region, returning per-page line transcripts ordered by (page, n). When no page list is
    /// given the whole document is rendered. Line numbering matches the transcript format the
    /// manifest line references were produced against.
    /// </summary>
    public static List<OcrLine> OcrPdf(string pdfPath, int dpi, IReadOnlyList<int>? onlyPages = null)
    {
        string workDir = Path.Combine(Path.GetTempPath(), "dw-reconstruct", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var pngs = RenderAllPages(pdfPath, workDir, dpi, onlyPages);
            var lines = new List<OcrLine>();
            Parallel.ForEach(pngs, new ParallelOptions { MaxDegreeOfParallelism = 6 }, png =>
            {
                int pp = int.Parse(new Regex(@"-(\d+)\.png$").Match(Path.GetFileName(png)).Groups[1].Value);
                foreach (var l in OcrPageLines(png))
                    lock (lines) lines.Add(new OcrLine(pp, l.n, l.side, l.top, l.text));
            });
            return lines.OrderBy(l => l.Page).ThenBy(l => l.N).ToList();
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { }
        }
    }

    /// <summary>
    /// Assembles sections from the line transcripts using an ordered manifest. Pure-illustration
    /// halves (no section starts and few wordy lines) are dropped; a spanning section flows from
    /// the previous R half into the next text half. Sections are joined with newlines and trimmed
    /// of noise/header lines; ImagePath is set to "p{page}" for the page where the section starts.
    /// </summary>
    public static List<Section> ApplyManifest(
        IReadOnlyList<OcrLine> lineStream,
        IReadOnlyList<ManifestEntry> entries)
    {
        // Pure-illustration halves carry only garbled captions (few wordy lines);
        // a spanning section flows from the previous R half to the next text half,
        // so drop lines only from halves with no section starts AND little real text.
        var textHalves = entries.Select(e => (e.Page, e.Side)).ToHashSet();
        var wordyCount = lineStream
            .GroupBy(l => (l.Page, l.Side))
            .ToDictionary(g => g.Key, g => g.Count(l =>
            {
                var words = l.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length >= 4 && w.Any(char.IsLetter));
                return words.Count() >= 3;
            }));

        var filtered = lineStream.Where(l =>
            textHalves.Contains((l.Page, l.Side)) ||
            wordyCount.GetValueOrDefault((l.Page, l.Side)) >= 5).ToList();
        var lineIndex = filtered
            .Select((l, i) => (Key: (l.Page, l.N), Idx: i))
            .ToDictionary(x => x.Key, x => x.Idx);

        var sections = new List<Section>();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (!lineIndex.TryGetValue((e.Page, e.Line), out int startIdx)) continue;
            int endIdx;
            if (e.EndLine.HasValue)
            {
                if (!lineIndex.TryGetValue((e.Page, e.EndLine.Value), out int ei)) ei = startIdx;
                endIdx = ei + 1;
            }
            else if (i + 1 < entries.Count &&
                     lineIndex.TryGetValue((entries[i + 1].Page, entries[i + 1].Line), out int ni))
            {
                endIdx = ni;
            }
            else
            {
                endIdx = filtered.Count;
            }

            var raw = filtered.GetRange(startIdx, Math.Max(0, endIdx - startIdx)).Select(l => l.Text).ToList();
            var content = TrimContent(raw);
            sections.Add(new Section
            {
                SectionNumber = e.Number,
                Content = string.Join("\n", content),
                ImagePath = $"p{e.Page}",
            });
        }

        return sections.OrderBy(s => s.SectionNumber).ToList();
    }

    /// <summary>Concatenates the transcripts of the given pages as introduction/front-matter text.</summary>
    public static string BuildIntroduction(
        IReadOnlyList<OcrLine> lineStream,
        IReadOnlyList<int> pages)
    {
        var sb = new StringBuilder();
        foreach (var page in pages)
            foreach (var line in lineStream.Where(l => l.Page == page).OrderBy(l => l.N))
            {
                string t = line.Text.Trim();
                if (t.Length == 0) continue;
                sb.Append(t).Append('\n');
            }
        return sb.ToString().Trim();
    }

    public static List<string> TrimContent(List<string> raw)
    {
        var list = new List<string>();
        foreach (var line in raw)
        {
            string t = line.Trim();
            if (t.Length == 0) continue;
            if (list.Count > 0 && DanglingTurnTo(list[^1]) && IsTurnTargetLine(t))
            {
                list[^1] = list[^1].TrimEnd() + " " + t;
                continue;
            }
            if (IsNoiseLine(t)) continue;
            list.Add(t);
        }
        while (list.Count > 0 && IsHeaderLine(list[^1])) list.RemoveAt(list.Count - 1);
        return list;
    }

    public static bool DanglingTurnTo(string line)
    {
        // A line ending in a bare/empty turn instruction whose target sits on the
        // next (short) line, e.g. "…Turn to" + "256."  or  "…(turn to" + "44."
        return Regex.IsMatch(line.TrimEnd(),
            @"[\(\{\[]?\s*(?:[tfb]u(?:r|m)n?)(?:\s+[tfko]o?)?\s*$",
            RegexOptions.IgnoreCase);
    }

    public static bool IsTurnTargetLine(string line)
    {
        // Short all-digit target like "256." / "44," / "o 130." / "z7"
        return Regex.IsMatch(line.Trim(), @"^(?:[a-z]\s*)?\d{1,3}[.,;:]?$", RegexOptions.IgnoreCase);
    }

    public static bool IsNoiseLine(string line)
    {
        string t = line.Trim();
        if (t.Length == 0) return true;
        if (t.Length <= 6) return true;                        // headers, folios, tiny glyph noise
        if (t.Contains(' ') == false && t.Any(char.IsDigit)) return true; // folio ranges ("Jo-32", "T0-12")
        if (t.Length <= 12 && t.All(char.IsLetter) && t == t.ToUpperInvariant()) return true; // garbled all-caps folios
        int letters = t.Count(char.IsLetter);
        if (letters / (double)t.Length < 0.4) return true;     // mostly symbols
        return false;
    }

    public static bool IsHeaderLine(string line)
    {
        string t = line.Trim();
        if (t.Length == 0 || t.Length > 6) return false;
        if (t.Contains(' ')) return false;
        return true;
    }

    private static List<string> RenderAllPages(string pdfPath, string workDir, int dpi, IReadOnlyList<int>? only)
    {
        var wanted = only?.ToList() ?? new List<int>();
        int first = wanted.Count > 0 ? wanted.Min() : 1;
        int last = wanted.Count > 0 ? wanted.Max() : -1;
        var pdftoppm = FindTool("pdftoppm")
            ?? throw new InvalidOperationException("pdftoppm (poppler-utils) not found on PATH.");
        var psi = new System.Diagnostics.ProcessStartInfo(pdftoppm)
        {
            Arguments = wanted.Count > 0
                ? $"-png -r {dpi} -f {first} -l {last} \"{pdfPath}\" \"{Path.Combine(workDir, "p")}\""
                : $"-png -r {dpi} \"{pdfPath}\" \"{Path.Combine(workDir, "p")}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using (var proc = System.Diagnostics.Process.Start(psi)) proc?.WaitForExit(300_000);
        return Directory.GetFiles(workDir, "p-*.png")
            .Select(f => (Full: f, N: int.Parse(new Regex(@"-(\d+)\.png$").Match(Path.GetFileName(f)).Groups[1].Value)))
            .OrderBy(x => x.N)
            .Select(x => x.Full)
            .ToList();
    }

    private static List<(int n, string side, double top, string text)> OcrPageLines(string pngPath)
    {
        string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        var result = new List<(int, string, double, string)>();
        using var engine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
        engine.SetVariable("debug_file", "NUL");
        using var pix = Pix.LoadFromFile(pngPath);
        int w = pix.Width, h = pix.Height;
        bool wide = w >= h * 1.05;
        if (!wide)
        {
            CollectRegion(engine, pix, new Rect(0, 0, w, h), "M", h, result);
        }
        else
        {
            CollectRegion(engine, pix, new Rect(0, 0, w / 2, h), "L", h, result);
            CollectRegion(engine, pix, new Rect(w / 2, 0, w - w / 2, h), "R", h, result);
        }
        return result.OrderBy(l => l.Item2).ThenBy(l => l.Item3).Select((l, i) => (i, l.Item2, l.Item3, l.Item4)).ToList();
    }

    private static void CollectRegion(
        TesseractEngine engine, Pix pix, Rect region, string side, int imageHeight,
        List<(int, string, double, string)> result)
    {
        using var page = engine.Process(pix, region, PageSegMode.Auto);
        using var iter = page.GetIterator();
        iter.Begin();
        do
        {
            string line = (iter.GetText(PageIteratorLevel.TextLine) ?? "").Trim();
            if (line.Length == 0) continue;
            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var r))
            {
                double top = imageHeight > 0 ? Math.Clamp((double)r.Y1 / imageHeight, 0, 1) : 0;
                result.Add((0, side, top, line));
            }
        } while (iter.Next(PageIteratorLevel.TextLine));
    }

    private static string? FindTool(string name)
    {
        foreach (string dir in Environment.GetEnvironmentVariable("PATH")?.Split(';') ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            string candidate = Path.Combine(dir.Trim('"'), name + ".exe");
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
