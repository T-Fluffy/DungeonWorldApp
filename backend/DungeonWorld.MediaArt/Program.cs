using DungeonWorld.Infrastructure.Parsing;

// Media art extraction tool: pulls ONLY the illustrations out of a gamebook PDF,
// skipping text pages. Full-page scans are cropped to the dense ink blocks (the
// artwork); digital PDFs with embedded art export the images as-is.
//
//   dotnet run --project backend/DungeonWorld.MediaArt [--dir <folder>] [--book <prefix>...]
//               [--out <dir>] [--stats] [--page N]
//
// Default scope is the six processed books (FF01-05, FF16). Output lands in
// Storage/GameArtArt/<slug>/ and never touches the existing GameArt folder.

string? dirArg = null;
string? outArg = null;
var books = new List<string>();
bool stats = false;
int? pageOnly = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--dir":
            if (i + 1 < args.Length) dirArg = args[++i];
            break;
        case "--out":
            if (i + 1 < args.Length) outArg = args[++i];
            break;
        case "--book":
            if (i + 1 < args.Length) books.Add(args[++i]);
            break;
        case "--page":
            if (i + 1 < args.Length) pageOnly = int.Parse(args[++i]);
            break;
        case "--stats":
            stats = true;
            break;
        default:
            if (args[i].StartsWith("--book=", StringComparison.OrdinalIgnoreCase))
                books.Add(args[i][(args[i].IndexOf('=') + 1)..]);
            else
                Console.WriteLine($"Ignoring unknown option: {args[i]}");
            break;
    }
}

// The six books fully reconstructed so far. The PDFs live in the scan staging folder.
var defaultBooks = new[] { "FF01", "FF02", "FF03", "FF04", "FF05", "FF16" };
if (books.Count == 0) books.AddRange(defaultBooks);

var storageRoot = FindStorageRoot();
if (storageRoot == null)
{
    Console.Error.WriteLine("Storage/Books not found. Run from the repo root or a parent directory.");
    return 1;
}

string sourceDir = dirArg != null
    ? Path.GetFullPath(dirArg)
    : Path.Combine(storageRoot, "Books", "tmp");
string outputDir = outArg != null
    ? Path.GetFullPath(outArg)
    : Path.Combine(storageRoot, "GameArtArt");

var pdfs = Directory.GetFiles(sourceDir, "*.pdf")
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .Where(f => books.Any(b =>
        Path.GetFileName(f).StartsWith(b, StringComparison.OrdinalIgnoreCase)))
    .ToList();

if (pdfs.Count == 0)
{
    Console.Error.WriteLine($"No PDFs matched the requested books in {sourceDir}.");
    return 1;
}

Console.WriteLine($"Source:  {sourceDir}");
Console.WriteLine($"Output:  {outputDir}");
Console.WriteLine($"Books:   {string.Join(", ", pdfs.Select(p => Path.GetFileName(p)))}");
Console.WriteLine($"Mode:    {(stats ? "STATS (no files written)" : "extract")}");
Console.WriteLine();

var parser = new MediaArtParser();
int totalFiles = 0;

foreach (var pdf in pdfs)
{
    var title = Path.GetFileNameWithoutExtension(pdf);
    var slug = title.Replace(" ", "_").ToLower();
    Console.WriteLine($"== {title} ==");

    if (stats)
    {
        DumpStats(pdf, pageOnly);
        continue;
    }

    try
    {
        var results = parser.Extract(pdf, outputDir, slug);
        int files = results.Sum(r => r.FileCount);
        int artPages = results.Count(r => r.WasEmbeddedArt);
        totalFiles += files;
        Console.WriteLine($"   art images: {files}  (embedded-art pages: {artPages}, scan-crop pages: {results.Count - artPages})");
        Console.WriteLine($"   -> {Path.Combine(outputDir, slug)}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"   FAILED: {ex.Message}");
    }
    Console.WriteLine();
}

if (!stats)
{
    Console.WriteLine($"Total art images extracted: {totalFiles}");
    Console.WriteLine($"Output root: {outputDir}");
}

return 0;

// Prints a per-page band density profile so thresholds can be tuned.
void DumpStats(string pdfPath, int? onlyPage)
{
    using var doc = UglyToad.PdfPig.PdfDocument.Open(pdfPath);
    foreach (var page in doc.GetPages())
    {
        if (onlyPage.HasValue && page.Number != onlyPage.Value) continue;

        foreach (var image in page.GetImages())
        {
            if (image.WidthInSamples < MediaArtParser.MinArtDimension || image.HeightInSamples < MediaArtParser.MinArtDimension)
                continue;

            double boundsArea = image.Bounds.Width * image.Bounds.Height;
            double pageArea = page.Width * page.Height;
            double coverage = boundsArea / pageArea;
            string kind = coverage >= MediaArtParser.FullPageScanCoverage ? "scan" : "embedded";

            Console.WriteLine($"   page {page.Number,3} {image.WidthInSamples}x{image.HeightInSamples,-7} {kind,-8} coverage={coverage:F2}");
        }
    }
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
