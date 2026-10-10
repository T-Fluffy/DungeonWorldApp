using System.Text.RegularExpressions;
using DungeonWorld.Infrastructure.Parsing;
using Tesseract;

namespace DungeonWorld.Infrastructure.Parsing.Reconstruction;

/// <summary>
/// Enhanced OCR used <em>only</em> by the hard scans (FF13/FF15/FF17), selected per book via
/// <c>ManifestDungeonWorldParser.UseHardScanOcr</c>. Every other book keeps calling
/// <see cref="ReconstructionService.OcrPdf"/> directly and is byte-for-byte unaffected.
///
/// <para><b>Why a second pass is safe.</b> A book manifest addresses content by
/// <c>{n, page, side, line}</c>, where <c>line</c> is an index into the 300 dpi /
/// <c>PageSegMode.Auto</c> transcript. Changing dpi, page-segmentation mode or engine mode changes
/// that index and would silently re-point every entry in <c>ff13.json</c>…<c>ff17.json</c>. So the
/// original pass always runs first and stays the index backbone: page, side, line index, ordering and
/// line count are taken verbatim from it. This class only substitutes a line's <em>text</em>, and only
/// under the narrow rule below.</para>
///
/// <para><b>Adoption rule (deliberately conservative).</b> A secondary line replaces a primary line
/// only when both are the same physical line (same page/side, vertically aligned, comparable length)
/// and only when the primary has <em>no</em> parseable exit while the secondary has one. Prose is
/// never re-written: Tesseract's line iterator exposes no per-line confidence without disturbing the
/// walk, and churning prose for a marginal legibility gain would put already-correct book text at
/// risk for no measurable benefit. This keeps the change monotone - it can only ever add a lost exit,
/// never alter one that already reads correctly.</para>
/// </summary>
public static class HardScanReconstruction
{
    /// <summary>Vertical tolerance (page-height fraction) for matching a line across the two passes.</summary>
    private const double TopTolerance = 0.010;

    /// <summary>Reject a secondary line whose length drifts from the primary by more than this.</summary>
    private const double MaxLengthDrift = 0.40;

    /// <summary>Secondary pass dpi; higher than the primary's 300 to resolve small print.</summary>
    public const int SecondaryDpi = 400;

    /// <summary>Isolated Tesseract vocabulary folder; see <see cref="OcrData.DataPath(string?)"/>.</summary>
    private const string HardDataVariant = "ocrdata_hard";

    /// <summary>One line from the hardened pass. Its index is never used; only its text.</summary>
    private sealed record HardLine(int Page, string Side, double Top, string Text);

    /// <summary>An exit the graph extractor recognises, bounded to this book's section count.</summary>
    private static readonly Regex RealExit = new(
        @"\b(?:turn|go|burn)\s+(?:to\s+)?\.?\s*(?:the\s+)?(\d{1,4})\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Produces the transcript a manifest book is assembled from: the unchanged primary pass, with a
    /// line's text upgraded only where that upgrade recovers a lost exit. Falls back to the backbone
    /// if the hardened pass cannot run, so it can never become a regression.
    /// </summary>
    public static List<ReconstructionService.OcrLine> OcrPdfMerged(
        string pdfPath, int primaryDpi, IReadOnlyList<int> pages, int maxSection)
    {
        // Backbone: identical call to the pre-existing single-pass path, identical output.
        var primary = ReconstructionService.OcrPdf(pdfPath, primaryDpi, pages);
        List<HardLine> secondary;
        try
        {
            secondary = OcrHardenedPass(pdfPath, SecondaryDpi, pages);
        }
        catch (Exception)
        {
            return primary;
        }
        return Merge(primary, secondary, maxSection);
    }

    /// <summary>Overlays secondary text onto the primary transcript under the adoption rule above.</summary>
    private static List<ReconstructionService.OcrLine> Merge(
        List<ReconstructionService.OcrLine> primary,
        List<HardLine> secondary,
        int maxSection)
    {
        var bySlot = secondary
            .GroupBy(l => (l.Page, l.Side))
            .ToDictionary(g => g.Key, g => g.OrderBy(l => l.Top).ToList());

        var used = new Dictionary<(int, string), HashSet<int>>();
        var result = new List<ReconstructionService.OcrLine>(primary.Count);
        int upgraded = 0;

        foreach (var line in primary)
        {
            // Never touch a line that already yields a usable exit.
            if (HasUsableExit(line.Text, maxSection) || !bySlot.TryGetValue((line.Page, line.Side), out var candidates))
            {
                result.Add(line);
                continue;
            }

            var key = (line.Page, line.Side);
            if (!used.TryGetValue(key, out var taken))
            {
                taken = new HashSet<int>();
                used[key] = taken;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (taken.Contains(i)) continue;
                var cand = candidates[i];
                if (Math.Abs(cand.Top - line.Top) > TopTolerance) continue;
                if (!Comparable(cand.Text, line.Text)) continue;
                if (!HasUsableExit(cand.Text, maxSection)) continue;

                taken.Add(i);
                upgraded++;
                // Backbone fields preserved verbatim; only the text is upgraded.
                result.Add(new ReconstructionService.OcrLine(
                    line.Page, line.N, line.Side, line.Top, cand.Text));
                goto next;
            }

            result.Add(line);
        next: ;
        }

        Console.WriteLine($"HardScan: recovered {upgraded} lost exit(s) across {result.Count} transcript lines.");
        return result;
    }

    /// <summary>Length sanity check so a re-segmented secondary line cannot overwrite a good one.</summary>
    private static bool Comparable(string secondary, string primary)
    {
        if (string.IsNullOrWhiteSpace(secondary)) return false;
        if (primary.Length == 0) return true;
        int lo = Math.Min(primary.Length, secondary.Length);
        int hi = Math.Max(primary.Length, secondary.Length);
        return hi == 0 || 1.0 - (double)lo / hi <= MaxLengthDrift;
    }

    private static bool HasUsableExit(string text, int maxSection)
    {
        foreach (Match m in RealExit.Matches(text))
        {
            if (int.TryParse(m.Groups[1].Value, out int n) && n >= 1 && n <= maxSection)
                return true;
        }
        return false;
    }

    /// <summary>
    /// The hardened pass: higher dpi, inter-word spacing preserved, isolated FF13-FF17 vocabulary.
    /// </summary>
    private static List<HardLine> OcrHardenedPass(string pdfPath, int dpi, IReadOnlyList<int> pages)
    {
        string workDir = Path.Combine(Path.GetTempPath(), "dw-hardscan", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            RenderAllPages(pdfPath, workDir, dpi, pages);
            var lines = new List<HardLine>();
            Parallel.ForEach(
                Directory.GetFiles(workDir, "p-*.png"),
                new ParallelOptions { MaxDegreeOfParallelism = 6 },
                png =>
                {
                    int pp = int.Parse(Regex.Match(Path.GetFileName(png), @"-(\d+)\.png$").Groups[1].Value);
                    foreach (var l in OcrPageLines(png))
                        lock (lines)
                            lines.Add(new HardLine(pp, l.Side, l.Top, l.Text));
                });
            return lines.OrderBy(l => l.Page).ThenBy(l => l.Top).ToList();
        }
        finally
        {
            try { Directory.Delete(workDir, recursive: true); } catch { /* best effort */ }
        }
    }

    private static List<(double Top, string Side, string Text)> OcrPageLines(string pngPath)
    {
        var result = new List<(double, string, string)>();
        using var engine = new TesseractEngine(OcrData.DataPath(HardDataVariant), "eng", EngineMode.Default);
        engine.SetVariable("debug_file", "NUL");
        engine.SetVariable("preserve_interword_spaces", "1");
        using var pix = Pix.LoadFromFile(pngPath);
        int w = pix.Width, h = pix.Height;
        bool wide = w >= h * 1.05;
        if (!wide)
        {
            Collect(engine, pix, new Rect(0, 0, w, h), "M", h, result);
        }
        else
        {
            Collect(engine, pix, new Rect(0, 0, w / 2, h), "L", h, result);
            Collect(engine, pix, new Rect(w / 2, 0, w - w / 2, h), "R", h, result);
        }
        return result.OrderBy(l => l.Item2).ThenBy(l => l.Item1).ToList();
    }

    private static void Collect(
        TesseractEngine engine, Pix pix, Rect region, string side, int imageHeight,
        List<(double, string, string)> result)
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
                result.Add((top, side, line));
            }
        } while (iter.Next(PageIteratorLevel.TextLine));
    }

    private static void RenderAllPages(string pdfPath, string workDir, int dpi, IReadOnlyList<int> pages)
    {
        var pdftoppm = FindTool("pdftoppm")
            ?? throw new InvalidOperationException("pdftoppm (poppler-utils) not found on PATH.");
        int first = pages.Count > 0 ? pages.Min() : 1;
        int last = pages.Count > 0 ? pages.Max() : -1;
        var psi = new System.Diagnostics.ProcessStartInfo(pdftoppm)
        {
            Arguments = $"-png -gray -r {dpi} -f {first} -l {last} \"{pdfPath}\" \"{Path.Combine(workDir, "p")}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var proc = System.Diagnostics.Process.Start(psi);
        proc?.WaitForExit(600_000);
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