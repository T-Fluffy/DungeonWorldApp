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
