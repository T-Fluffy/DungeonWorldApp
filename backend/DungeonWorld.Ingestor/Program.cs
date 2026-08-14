using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Interfaces;
using DungeonWorld.Core.Options;
using DungeonWorld.Cleaning;
using DungeonWorld.Cleaning.Model;
using DungeonWorld.Infrastructure.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Tesseract;
using DungeonWorld.Ingestor;

// Batch ingestion tool: parses every PDF in a folder through the same block-based
// pipeline the API uses, writes ProcessedBooks/<Title>.json, CleanedData/<Title>.json
// and GameArt images, and prints a per-book quality report.
//
//   dotnet run --project backend/DungeonWorld.Ingestor [--dir <folder>] [--exclude <substring>...] [--no-images]
//
// Run from the repo root. Output never touches curated titles such as "Seas of Blood".

const string Placeholder = "[Text missing or unreadable in PDF]";
const int MaxSection = 400;
const int MergeThreshold = 400;
var mergeDpis = new[] { 200, 250, 300, 400 };

var protectedTitles = new[] { "Seas of Blood" };

string? dirArg = null;
var excludes = new List<string>();
var noImages = false;
int ocrDpi = 250;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dir":
            if (i + 1 < args.Length) dirArg = args[++i];
            break;
        case "--no-images":
            noImages = true;
            break;
        case "--dpi":
            if (i + 1 < args.Length) ocrDpi = int.Parse(args[++i]);
            break;
        case "--exclude":
            if (i + 1 < args.Length) excludes.Add(args[++i]);
            break;
        default:
            if (args[i].StartsWith("--exclude=", StringComparison.OrdinalIgnoreCase))
                excludes.Add(args[i][(args[i].IndexOf('=') + 1)..]);
            else
                Console.WriteLine($"Ignoring unknown option: {args[i]}");
            break;
    }
}

if (args.Length >= 2 && args[0] == "--ocr-extract")
{
    var ocrBlocks = new OcrPdfTextExtractor().Extract(args[1]);
    var numeric = ocrBlocks.Where(b => Regex.IsMatch(b.Text.Trim(), @"^\W*\d{1,4}\W*$") && b.TopFraction >= 0.04 && b.TopFraction <= 0.88).ToList();
    Console.WriteLine($"total blocks {ocrBlocks.Count}, mid-page numeric {numeric.Count}");
    foreach (var b in ocrBlocks.Take(60))
        Console.WriteLine($"LP{b.LogicalPage,3} top{b.TopFraction,5:F2} | {b.Text}");
    Console.WriteLine($"--- numeric blocks ---");
    foreach (var b in numeric.Take(60))
        Console.WriteLine($"LP{b.LogicalPage,3} top{b.TopFraction,5:F2} | {b.Text}");
    return 0;
}

if (args.Length >= 3 && args[0] == "--ocr-dump")
{
    int pageNum = int.Parse(args[1]);
    var pdf = Path.GetFullPath(args[2]);
    string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    string workDir = Path.Combine(Path.GetTempPath(), "dw-ocr-dump", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(workDir);
    var finder = typeof(OcrPdfTextExtractor).GetMethod(
        "FindTool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
    var pdftoppm = (string?)finder.Invoke(null, new object[] { "pdftoppm" });
    var psi = new System.Diagnostics.ProcessStartInfo(pdftoppm!)
    {
        Arguments = $"-png -r 200 -f {pageNum} -l {pageNum} \"{pdf}\" \"{Path.Combine(workDir, "p")}\"",
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    using (var proc = System.Diagnostics.Process.Start(psi)) proc?.WaitForExit(120_000);
    var png = Directory.GetFiles(workDir, "p-*.png").Single();
    using var engine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
    using var pix = Pix.LoadFromFile(png);
    Console.WriteLine($"page {pageNum}: {pix.Width}x{pix.Height} ratio {pix.Width / (double)pix.Height:F2}");

    using (var whole = engine.Process(pix, PageSegMode.Auto))
    {
        Console.WriteLine("=== WHOLE PAGE ===");
        using var iter = whole.GetIterator(); iter.Begin();
        do
        {
            var t = (iter.GetText(PageIteratorLevel.TextLine) ?? "").Trim();
            if (t.Length == 0) continue;
            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var r))
                Console.WriteLine($"  x{r.X1,4}-{r.X2,4} y{r.Y1,4}-{r.Y2,4} | {t}");
        } while (iter.Next(PageIteratorLevel.TextLine));
    }

    foreach (var (half, rect) in new[] {
        ("LEFT", new Rect(0, 0, pix.Width / 2, pix.Height)),
        ("RIGHT", new Rect(pix.Width / 2, 0, pix.Width - pix.Width / 2, pix.Height)) })
    {
        Console.WriteLine($"=== {half} HALF (region x{rect.X1}-{rect.X1 + rect.Width}) ===");
        using var region = engine.Process(pix, rect, PageSegMode.Auto);
        using var iter = region.GetIterator(); iter.Begin();
        do
        {
            var t = (iter.GetText(PageIteratorLevel.TextLine) ?? "").Trim();
            if (t.Length == 0) continue;
            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var r))
                Console.WriteLine($"  x{r.X1,4}-{r.X2,4} y{r.Y1,4}-{r.Y2,4} | {t}");
        } while (iter.Next(PageIteratorLevel.TextLine));
    }

    try { Directory.Delete(workDir, recursive: true); } catch { }
    return 0;
}

if (args.Length >= 2 && args[0] == "--ocr-test")
{
    var testPng = Path.GetFullPath(args[1]);
    string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    using var engine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
    engine.SetVariable("preserve_interword_spaces", "1");
    using var img = Pix.LoadFromFile(testPng);
    using (var page = engine.Process(img))
    {
        Console.WriteLine(page.GetText());
        Console.WriteLine($"... mean confidence {page.GetMeanConfidence():F2}");
    }
    if (args.Length >= 3 && args[2] == "--psm-sparse")
    {
        using var sparse = engine.Process(img, PageSegMode.SparseText);
        Console.WriteLine($"--- SPARSE TEXT (mean confidence {sparse.GetMeanConfidence():F2}) ---");
        Console.WriteLine(sparse.GetText());
    }
    if (args.Length >= 3 && args[2] == "--psm-line")
    {
        using var line = engine.Process(img, PageSegMode.SingleLine);
        Console.WriteLine($"--- SINGLE LINE (mean confidence {line.GetMeanConfidence():F2}) ---");
        Console.WriteLine(line.GetText());
    }
    return 0;
}

if (args.Length >= 2 && args[0] == "--probe")
{
    var probePath = Path.GetFullPath(args[1]);
    var probeBlocks = new PdfPigTextExtractor().Extract(probePath);
    int printed = 0;
    foreach (var b in probeBlocks)
    {
        bool numeric = Regex.IsMatch(b.Text.Trim(), @"^\W*\d{1,4}\W*$");
        bool shortish = b.Text.Trim().Length <= 12;
        if (printed < 60 || numeric || shortish)
        {
            Console.WriteLine($"LP{b.LogicalPage,3} PP{b.PhysicalPage,3} top{b.TopFraction,5:F2} font{b.FontSize,5:F1} bold={(b.IsBold ? "T" : "F")} | {b.Text.Trim().Replace('\n', ' ')}");
            printed++;
        }
    }
    Console.WriteLine($"... total blocks {probeBlocks.Count}");
    return 0;
}

// --reconstruct <pdf> --out <dir> [--dpi 300] [--pages 1,2,3 | --start N [--end M]]
// Renders every page, OCRs the content half, and dumps a line-numbered transcript per page
// (pages/PageNNN.txt + .json). Used to rebuild a scan-heavy book section by section.
if (args.Length >= 2 && args[0] == "--reconstruct")
{
    string pdf = Path.GetFullPath(args[1]);
    string? outDir = null;
    int dpi = 300;
    var pages = new List<int>();
    int startPage = -1, endPage = -1;
    for (int i = 2; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--out":
                if (i + 1 < args.Length) outDir = Path.GetFullPath(args[++i]);
                break;
            case "--dpi":
                if (i + 1 < args.Length) dpi = int.Parse(args[++i]);
                break;
            case "--pages":
                if (i + 1 < args.Length) pages = args[++i].Split(',').Select(int.Parse).ToList();
                break;
            case "--start":
                if (i + 1 < args.Length) startPage = int.Parse(args[++i]);
                break;
            case "--end":
                if (i + 1 < args.Length) endPage = int.Parse(args[++i]);
                break;
            default:
                Console.WriteLine($"Ignoring unknown option: {args[i]}");
                break;
        }
    }
    if (outDir == null) { Console.Error.WriteLine("--out <dir> required"); return 1; }
    Directory.CreateDirectory(Path.Combine(outDir, "pages"));
    ReconstructPdf(pdf, outDir, dpi, pages, startPage, endPage);
    return 0;
}

// --reconstruct-apply <dumpDir> <overrides.json> --out <sections.json>
// Assembles the 400 sections from the per-page transcripts using an ordered manifest of
// {n, page, side, line} body-start points produced by reviewing the transcripts.
if (args.Length >= 3 && args[0] == "--reconstruct-apply")
{
    string dumpDir = Path.GetFullPath(args[1]);
    string overridesPath = Path.GetFullPath(args[2]);
    string? outFile = null;
    for (int i = 3; i < args.Length; i++)
    {
        if (args[i] == "--out" && i + 1 < args.Length) outFile = Path.GetFullPath(args[++i]);
        else Console.WriteLine($"Ignoring unknown option: {args[i]}");
    }
    if (outFile == null) { Console.Error.WriteLine("--out <sections.json> required"); return 1; }
    return ReconstructApply(dumpDir, overridesPath, outFile);
}

var storageRoot = FindStorageRoot();
if (storageRoot == null)
{
    Console.Error.WriteLine("Storage/Books not found. Run from the repo root or a parent directory.");
    return 1;
}

var booksRoot = Path.Combine(storageRoot, "Books");
var sourceDir = dirArg != null ? Path.GetFullPath(dirArg) : Path.Combine(booksRoot, "tmp");

if (!Directory.Exists(sourceDir))
{
    Console.Error.WriteLine($"Source directory not found: {sourceDir}");
    return 1;
}

var imagesDir = noImages
    ? Path.Combine(Path.GetTempPath(), "dungeonworld-ingestor", Guid.NewGuid().ToString("N"))
    : Path.Combine(storageRoot, "GameArt");

var storageOptions = Options.Create(new FileStorageOptions
{
    PdfUploadPath = booksRoot,
    ImageOutputPath = imagesDir,
    AvatarPath = Path.Combine(storageRoot, "Avatars")
});

var processedDir = Path.Combine(booksRoot, "ProcessedBooks");
var cleanedDir = Path.Combine(booksRoot, "CleanedData");
Directory.CreateDirectory(processedDir);
Directory.CreateDirectory(cleanedDir);

// Parser registry mirroring the API's Program.cs DI registrations.
var extractor = new PdfPigTextExtractor();
var defaultParser = new DefaultDungeonWorldParser(extractor, storageOptions);
var factory = new DungeonWorldParserFactory(
    new IBookParser[]
    {
        new SeasOfBloodParser(extractor, storageOptions),
        new WarlockOfFiretopMountainParser(extractor, storageOptions),
    },
    defaultParser,
    NullLogger<DungeonWorldParserFactory>.Instance);

var pdfs = Directory.GetFiles(sourceDir, "*.pdf")
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .Where(f => !excludes.Any(e => Path.GetFileName(f).Contains(e, StringComparison.OrdinalIgnoreCase)))
    .ToList();

if (pdfs.Count == 0)
{
    Console.Error.WriteLine("No PDF files to process.");
    return 1;
}

Console.WriteLine($"Source:  {sourceDir}");
Console.WriteLine($"Images:  {(noImages ? "SKIPPED (temp dir)" : imagesDir)}");
Console.WriteLine($"Books:   {pdfs.Count}");
Console.WriteLine();

var results = new List<(string Title, bool Check)>();
foreach (var pdf in pdfs)
{
    var title = Path.GetFileNameWithoutExtension(pdf);
    var slug = title.Replace(" ", "_").ToLower();

    try
    {
        CleanPreviousOutput(title, processedDir, cleanedDir, protectedTitles);
        if (!noImages)
        {
            var gameArtFolder = Path.Combine(storageRoot, "GameArt", slug);
            if (Directory.Exists(gameArtFolder))
                Directory.Delete(gameArtFolder, recursive: true);
        }

        var parser = factory.CreateParser(pdf, title);
        var book = await parser.ParseAsync(pdf);

        DungeonWorldBookParserBase CreateOcrParser(IPdfTextExtractor ext) =>
            title.Contains("Warlock of Firetop Mountain", StringComparison.OrdinalIgnoreCase)
                ? new WarlockOfFiretopMountainParser(ext, storageOptions)
                : new DefaultDungeonWorldParser(ext, storageOptions);

        var present = book.Sections.Count(s => s.Content != Placeholder);
        var candidates = new List<Book> { book };
        if (present < MergeThreshold)
        {
            DungeonWorldBookParserBase RunOcr(IPdfTextExtractor ext) => CreateOcrParser(ext);

            async Task<Book> RunOcrPass(TwoUpMode mode, int dpi) =>
                await RunOcr(new OcrPdfTextExtractor(twoUpMode: mode, workers: 10, dpi: dpi)).ParseAsync(pdf);

            int CountPresent(Book b) => b.Sections.Count(s => s.Content != Placeholder);

            var flatBook = await RunOcrPass(TwoUpMode.Flat, ocrDpi);
            var splitBook = await RunOcrPass(TwoUpMode.RegionSplit, ocrDpi);
            Console.WriteLine($"  [OCR] dpi={ocrDpi} embedded={present}, flat={CountPresent(flatBook)}, split={CountPresent(splitBook)}");
            candidates.Add(CountPresent(splitBook) > CountPresent(flatBook) ? splitBook : flatBook);

            if (CountPresent(candidates[^1]) < MergeThreshold)
            {
                foreach (var dpi in mergeDpis.Where(d => d != ocrDpi))
                {
                    var flat = await RunOcrPass(TwoUpMode.Flat, dpi);
                    var split = await RunOcrPass(TwoUpMode.RegionSplit, dpi);
                    Console.WriteLine($"  [OCR] dpi={dpi} flat={CountPresent(flat)}, split={CountPresent(split)}");
                    candidates.Add(CountPresent(split) > CountPresent(flat) ? split : flat);
                }
            }

            book = MergeBooks(candidates, MaxSection);
            Console.WriteLine($"  [MERGE] {CountPresent(book)}/{book.Sections.Count} present across {candidates.Count} passes");
        }

        var cleaned = BookCleaner.Clean(book, $"{title}.json");
        var outPath = BookCleaner.WriteCleanedBook(cleaned, cleanedDir);

        var check = PrintReport(title, parser.ParserId, book, cleaned, outPath);
        results.Add((title, check));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  [ERROR] {title}: {ex.Message}");
        results.Add((title, true));
    }
}

Console.WriteLine();
Console.WriteLine($"=== Summary: {results.Count(r => !r.Check)} OK, {results.Count(r => r.Check)} need attention ===");
foreach (var (title, check) in results)
    Console.WriteLine($"  {(check ? "[CHECK]" : "[OK]   ")} {title}");

return 0;

static int ReconstructApply(string dumpDir, string overridesPath, string outFile)
{
    var pagesDir = Path.Combine(dumpDir, "pages");
    var lineStream = new List<(int page, int n, string side, string text)>();
    foreach (var jf in Directory.GetFiles(pagesDir, "Page*.json")
        .OrderBy(f => int.Parse(new Regex(@"Page(\d+)\.json").Match(Path.GetFileName(f)).Groups[1].Value)))
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(jf));
        int page = doc.RootElement.GetProperty("page").GetInt32();
        foreach (var l in doc.RootElement.GetProperty("lines").EnumerateArray())
        {
            lineStream.Add((page, l.GetProperty("n").GetInt32(), l.GetProperty("side").GetString()!,
                l.GetProperty("text").GetString()!));
        }
    }
    lineStream = lineStream.OrderBy(x => x.page).ThenBy(x => x.n).ToList();

    using var doc2 = JsonDocument.Parse(File.ReadAllText(overridesPath));
    var entries = new List<(int number, int page, string side, int line, int? endLine)>();
    foreach (var e in doc2.RootElement.GetProperty("entries").EnumerateArray())
        entries.Add((e.GetProperty("n").GetInt32(), e.GetProperty("page").GetInt32(),
            e.GetProperty("side").GetString()!, e.GetProperty("line").GetInt32(),
            e.TryGetProperty("end", out var en) && en.ValueKind == JsonValueKind.Number ? en.GetInt32() : null));
    entries = entries.OrderBy(e => e.number).ToList();

    // Pure-illustration halves carry only garbled captions (few wordy lines);
    // a spanning section flows from the previous R half to the next text half,
    // so drop lines only from halves with no section starts AND little real text.
    var textHalves = entries.Select(e => (e.page, e.side)).ToHashSet();
    var wordyCount = lineStream
        .GroupBy(l => (l.page, l.side))
        .ToDictionary(g => g.Key, g => g.Count(l => {
            var words = l.text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 4 && w.Any(char.IsLetter));
            return words.Count() >= 3;
        }));
    lineStream = lineStream.Where(l =>
        textHalves.Contains((l.page, l.side)) ||
        wordyCount.GetValueOrDefault((l.page, l.side)) >= 5).ToList();
    var lineIndex = lineStream
        .Select((l, i) => (Key: (l.page, l.n), Idx: i))
        .ToDictionary(x => x.Key, x => x.Idx);

    var missing = new List<int>();
    int expected = entries[0].number;
    foreach (var e in entries)
    {
        if (e.number > expected)
            for (int g = expected; g < e.number; g++) missing.Add(g);
        if (e.number >= expected) expected = e.number + 1;
    }
    var sections = new List<Section>();
    for (int i = 0; i < entries.Count; i++)
    {
        var e = entries[i];
        if (!lineIndex.TryGetValue((e.page, e.line), out int startIdx))
        {
            Console.Error.WriteLine($"entry n={e.number} at p{e.page} L{e.side} line {e.line} NOT FOUND");
            continue;
        }
        int endIdx;
        if (e.endLine.HasValue)
        {
            if (!lineIndex.TryGetValue((e.page, e.endLine.Value), out int ei)) ei = startIdx;
            endIdx = ei + 1;
        }
        else if (i + 1 < entries.Count &&
                 lineIndex.TryGetValue((entries[i + 1].page, entries[i + 1].line), out int ni))
        {
            endIdx = ni;
        }
        else
        {
            endIdx = lineStream.Count;
        }
        var raw = lineStream.GetRange(startIdx, Math.Max(0, endIdx - startIdx)).Select(l => l.text).ToList();
        var content = TrimContent(raw);
        sections.Add(new Section
        {
            SectionNumber = e.number,
            Content = string.Join("\n", content),
            ImagePath = $"p{e.page}",
        });
    }

    Console.WriteLine($"entries {entries.Count}, lines {lineStream.Count}, sections {sections.Count}");
    if (missing.Count > 0) Console.WriteLine($"  MISSING numbering: {string.Join(",", missing)}");
    foreach (var s in sections)
        if (s.Content.Trim().Length == 0) Console.WriteLine($"  EMPTY section {s.SectionNumber}");
    foreach (var s in sections.Take(4))
        Console.WriteLine($"  sec {s.SectionNumber} [{s.Content.Length}] {s.Content.Split('\n')[0].Trim()}");

    Directory.CreateDirectory(Path.GetDirectoryName(outFile)!);
    File.WriteAllText(outFile, JsonSerializer.Serialize(sections, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"-> {outFile}");
    return 0;
}

static List<string> TrimContent(List<string> raw)
{
    var list = raw
        .Where(l => l.Trim().Length > 0)
        .Where(l => !IsNoiseLine(l))
        .ToList();
    while (list.Count > 0 && IsHeaderLine(list[^1])) list.RemoveAt(list.Count - 1);
    return list;
}

static bool IsNoiseLine(string line)
{
    string t = line.Trim();
    if (t.Length == 0) return true;
    if (t.Length <= 6) return true;                        // headers, folios, tiny glyph noise
    if (t.Contains(' ') == false && t.Any(char.IsDigit)) return true; // folio ranges ("Jo-32", "T0-12")
    if (t.Length <= 12 && t.All(char.IsLetter) && t == t.ToUpperInvariant()) return true; // garbled all-caps folios ("FEFTIET")
    int letters = t.Count(char.IsLetter);
    if (letters / (double)t.Length < 0.4) return true;     // mostly symbols
    return false;
}

static bool IsHeaderLine(string line)
{
    string t = line.Trim();
    if (t.Length == 0 || t.Length > 6) return false;
    if (t.Contains(' ')) return false;
    return true;
}

static void ReconstructPdf(string pdfPath, string outDir, int dpi, List<int> pages, int startPage, int endPage)
{
    string workDir = Path.Combine(Path.GetTempPath(), "dw-reconstruct", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(workDir);

    var pngs = RenderAllPages(pdfPath, workDir, dpi);
    var index = new List<object>();
    int pageNum = 0;

    Parallel.ForEach(pngs, new ParallelOptions { MaxDegreeOfParallelism = 6 }, png =>
    {
        var pp = int.Parse(new Regex(@"-(\d+)\.png$").Match(Path.GetFileName(png)).Groups[1].Value);
        var lines = OcrPageLines(png);
        lock (index)
        {
            index.Add(new { page = pp, lines = lines.Count, file = $"pages/Page{pp:D3}.json" });
            var json = new { page = pp, lineCount = lines.Count, lines = lines.Select(l => new { n = l.n, side = l.side, top = l.top, text = l.text }) };
            File.WriteAllText(Path.Combine(outDir, "pages", $"Page{pp:D3}.json"),
                JsonSerializer.Serialize(json, new JsonSerializerOptions { WriteIndented = true }));
            var sb = new StringBuilder();
            sb.AppendLine($"PAGE {pp}  ({lines.Count} lines)");
            foreach (var l in lines) sb.AppendLine($"{l.n,3} {l.side} {l.top,6:F3}  {l.text}");
            File.WriteAllText(Path.Combine(outDir, "pages", $"Page{pp:D3}.txt"), sb.ToString());
        }
        pageNum = Math.Max(pageNum, pp);
    });

    try { Directory.Delete(workDir, recursive: true); } catch { }

    var wanted = pages.Count > 0
        ? pages
        : startPage > 0
            ? Enumerable.Range(startPage, (endPage > 0 ? endPage : pageNum) - startPage + 1).ToList()
            : Enumerable.Range(1, pageNum).ToList();
    var kept = index.Cast<dynamic>().Where(x => wanted.Contains((int)x.page)).OrderBy(x => (int)x.page).ToList();
    File.WriteAllText(Path.Combine(outDir, "index.json"),
        JsonSerializer.Serialize(kept, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Reconstruct dump: {kept.Count} pages -> {outDir} (max page {pageNum})");
}

static List<string> RenderAllPages(string pdfPath, string workDir, int dpi)
{
    var psi = new System.Diagnostics.ProcessStartInfo(
        (string?)typeof(OcrPdfTextExtractor).GetMethod(
            "FindTool", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object[] { "pdftoppm" })!)
    {
        Arguments = $"-png -r {dpi} \"{pdfPath}\" \"{Path.Combine(workDir, "p")}\"",
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

static List<(int n, string side, double top, string text)> OcrPageLines(string pngPath)
{
    string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    var result = new List<(int, string, double, string)>();
    using var engine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
    engine.SetVariable("debug_file", "NUL");
    using var pix = Pix.LoadFromFile(pngPath);
    int w = pix.Width, h = pix.Height;
    bool wide = w >= h * 1.15;
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

static void CollectRegion(
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

static string? FindStorageRoot()
{
    var dir = Directory.GetCurrentDirectory();
    while (dir != null)
    {
        foreach (var candidate in new[]
        {
            Path.Combine(dir, "Storage", "Books"),
            Path.Combine(dir, "backend", "Storage", "Books"),
        })
        {
            if (Directory.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
        }
        dir = Directory.GetParent(dir)?.FullName;
    }
    return null;
}

static void CleanPreviousOutput(string title, string processedDir, string cleanedDir, string[] protectedTitles)
{
    if (protectedTitles.Contains(title, StringComparer.OrdinalIgnoreCase)) return;

    foreach (var dir in new[] { processedDir, cleanedDir })
    {
        if (!Directory.Exists(dir)) continue;
        foreach (var file in Directory.GetFiles(dir))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            bool match = name.Equals(title, StringComparison.OrdinalIgnoreCase) ||
                         name.StartsWith($"{title} (", StringComparison.OrdinalIgnoreCase);
            if (match && Path.GetExtension(file).Equals(".json", StringComparison.OrdinalIgnoreCase))
                File.Delete(file);
        }
    }
}

static bool PrintReport(string title, string parserId, Book book, CleanedBook cleaned, string outPath)
{
    var present = book.Sections.Where(s => s.Content != Placeholder).ToList();
    var missing = book.Sections.Count - present.Count;
    var maxNum = book.Sections.Count > 0 ? book.Sections.Max(s => s.SectionNumber) : 0;
    var avgLen = present.Count > 0 ? (int)present.Average(s => s.Content.Length) : 0;
    var totalChars = present.Sum(s => s.Content.Length);

    var choices = book.Sections.SelectMany(s => s.Choices).ToList();
    var outOfRange = choices.Count(c => c.TargetSectionNumber < 1 || c.TargetSectionNumber > MaxSection);
    var missingSet = book.Sections.Where(s => s.Content == Placeholder).Select(s => s.SectionNumber).ToHashSet();
    var toMissing = choices.Count(c => missingSet.Contains(c.TargetSectionNumber));

    int introLen = cleaned.Meta.Introduction?.Length ?? 0;

    bool check = maxNum < 200 ||
                 present.Count < 100 ||
                 outOfRange > 0 ||
                 toMissing > 5 ||
                 (present.Count > 0 && (avgLen < 300 || avgLen > 2500)) ||
                 introLen == 0;

    Console.WriteLine($"{title}  [{parserId}]");
    Console.WriteLine($"  sections {present.Count}/{book.Sections.Count} present, max #{maxNum}, missing {missing}");
    Console.WriteLine($"  text avg {avgLen} chars (total {totalChars:N0})");
    Console.WriteLine($"  choices {choices.Count} (outOfRange {outOfRange}, toMissing {toMissing})");
    Console.WriteLine($"  combat {cleaned.Meta.CombatSectionCount}, enemies {cleaned.Meta.EnemyCount}");
    Console.WriteLine($"  deadEnds {cleaned.Graph.DeadEnds.Count}, terminal {cleaned.Graph.Terminal.Count}, " +
                      $"unreachable {cleaned.Graph.Unreachable.Count}, orphanLinks {cleaned.Graph.OrphanLinks.Count}");
    Console.WriteLine($"  intro {introLen} chars, rules {cleaned.Rules.Count}");
    Console.WriteLine($"  -> {outPath}");
    Console.WriteLine($"  {(check ? "[CHECK]" : "[OK]")}");
    return check;
}

static Book MergeBooks(IEnumerable<Book> candidates, int maxSection)
{
    var byNumber = new Dictionary<int, List<Section>>();
    foreach (var b in candidates)
        foreach (var s in b.Sections)
        {
            if (!byNumber.TryGetValue(s.SectionNumber, out var list))
                byNumber[s.SectionNumber] = list = new List<Section>();
            list.Add(s);
        }

    var merged = new List<Section>();
    for (int n = 1; n <= maxSection; n++)
    {
        Section? best = null;
        if (byNumber.TryGetValue(n, out var list))
        {
            foreach (var s in list)
            {
                if (best == null) { best = s; continue; }
                bool sReal = s.Content != Placeholder;
                bool bReal = best.Content != Placeholder;
                if (sReal && !bReal) { best = s; continue; }
                if (!sReal && bReal) continue;
                if (s.Content.Length > best.Content.Length) best = s;
            }
        }
        merged.Add(best ?? new Section { SectionNumber = n, Content = Placeholder });
    }

    var src = candidates.OrderByDescending(b => b.Sections.Count(s => s.Content != Placeholder)).First();
    return new Book
    {
        Title = src.Title,
        Introduction = src.Introduction,
        AdventureSheetPath = src.AdventureSheetPath,
        MapPath = src.MapPath,
        Author = src.Author,
        Sections = merged,
    };
}
