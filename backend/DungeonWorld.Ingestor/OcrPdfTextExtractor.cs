using System.Diagnostics;
using System.Text.RegularExpressions;
using DungeonWorld.Infrastructure.Parsing;
using Tesseract;

namespace DungeonWorld.Ingestor;

/// <summary>How two-page (2-up) landscape scans are handled during OCR.</summary>
public enum TwoUpMode
{
    /// <summary>Treat every rendered page as one logical page. Simple and safe.</summary>
    Flat,

    /// <summary>
    /// OCR the whole sheet once, then assign each line to the left or right book page by
    /// its horizontal centre. One OCR pass; relies on Tesseract reading both columns.
    /// </summary>
    ColumnCentre,

    /// <summary>OCR the left and right half of a wide sheet as two separate logical pages.</summary>
    RegionSplit,
}

/// <summary>
/// IPdfTextExtractor implementation that renders each page to an image and runs
/// Tesseract OCR over it. Used for scanned books whose embedded text layer is
/// missing or too garbled to parse. Produces one block per OCR line, with
/// vertical position preserved so the shared section-header heuristics work.
/// </summary>
/// <remarks>
/// Two-page (2-up) scans: scanning books layouted side-by-side. The extractor
/// supports several strategies (see <see cref="TwoUpMode"/>); the batch tool runs
/// the safe <see cref="TwoUpMode.Flat"/> pass and a <see cref="TwoUpMode.RegionSplit"/>
/// pass per book and keeps whichever yields more sections.
/// </remarks>
public sealed class OcrPdfTextExtractor : IPdfTextExtractor
{
    private static readonly Regex PageFile = new(@"-(\d+)\.png$", RegexOptions.Compiled);
    private static readonly Regex NumberOnlyLine = new(@"^\W*\d{1,4}\W*$", RegexOptions.Compiled);

    // Mirrors PdfPigTextExtractor.DoublePageAspectThreshold.
    private const double DoublePageAspectThreshold = 1.15;

    private readonly string? _pdftoppmPath;
    private readonly int _dpi;
    private readonly int _workers;
    private readonly TwoUpMode _twoUpMode;
    private readonly double _doublePageAspectThreshold;

    public OcrPdfTextExtractor(
        string? pdftoppmPath = null,
        int dpi = 200,
        int workers = 6,
        TwoUpMode twoUpMode = TwoUpMode.Flat,
        double doublePageAspectThreshold = DoublePageAspectThreshold)
    {
        _pdftoppmPath = pdftoppmPath ?? FindTool("pdftoppm");
        _dpi = dpi;
        _workers = workers;
        _twoUpMode = twoUpMode;
        _doublePageAspectThreshold = doublePageAspectThreshold;
    }

    public List<TextBlock> Extract(string filePath)
    {
        string fullPath = Path.GetFullPath(filePath);
        string workDir = Path.Combine(Path.GetTempPath(), "dw-ocr", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);

        var pngs = RenderPages(fullPath, workDir);
        var blocks = new List<TextBlock>();

        Parallel.ForEach(pngs, new ParallelOptions { MaxDegreeOfParallelism = _workers }, png =>
        {
            var pageBlocks = OcrPage(png, _twoUpMode, _doublePageAspectThreshold);
            lock (blocks) blocks.AddRange(pageBlocks);
        });

        try { Directory.Delete(workDir, recursive: true); }
        catch { /* best effort */ }

        return blocks
            .OrderBy(b => b.PhysicalPage)
            .ThenBy(b => b.LogicalPage)
            .ThenBy(b => b.TopFraction)
            .ToList();
    }

    private List<string> RenderPages(string pdfPath, string workDir)
    {
        string prefix = Path.Combine(workDir, "p");
        var psi = new ProcessStartInfo(_pdftoppmPath!)
        {
            Arguments = $"-png -r {_dpi} \"{pdfPath}\" \"{prefix}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        using var proc = Process.Start(psi);
        proc?.WaitForExit(300_000);

        var files = Directory.GetFiles(workDir, "p-*.png")
            .Select(f => (FullName: f, PageNum: int.Parse(PageFile.Match(Path.GetFileName(f)).Groups[1].Value)))
            .OrderBy(x => x.PageNum)
            .ToList();

        if (files.Count == 0)
            throw new InvalidOperationException(
                $"pdftoppm produced no images (exit {proc?.ExitCode}); stdout: {proc?.StandardOutput.ReadToEnd()}");

        return files.Select(f => f.FullName).ToList();
    }

    private static List<TextBlock> OcrPage(string pngPath, TwoUpMode mode, double aspectThreshold)
    {
        var result = new List<TextBlock>();
        string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
        int pageNum = int.Parse(PageFile.Match(Path.GetFileName(pngPath)).Groups[1].Value);

        using var engine = new TesseractEngine(dataPath, "eng", EngineMode.Default);
        using var pix = Pix.LoadFromFile(pngPath);

        int imageHeight = pix.Height;
        int imageWidth = pix.Width;
        bool isWide = imageWidth >= imageHeight * aspectThreshold;

        if (mode == TwoUpMode.RegionSplit && isWide)
        {
            int midX = imageWidth / 2;
            CollectBlocks(engine, pix, new Rect(0, 0, midX, imageHeight),
                pageNum * 2 - 1, pageNum, imageHeight, result);
            CollectBlocks(engine, pix, new Rect(midX, 0, imageWidth - midX, imageHeight),
                pageNum * 2, pageNum, imageHeight, result);
            return result;
        }

        // Flat (single logical page) or ColumnCentre (assign by X centre).
        using var page = engine.Process(pix, PageSegMode.Auto);
        var lines = new List<(int LogicalPage, double Top, string Text)>();
        using var iter = page.GetIterator();
        iter.Begin();
        do
        {
            string line = (iter.GetText(PageIteratorLevel.TextLine) ?? "").Trim();
            if (line.Length == 0) continue;

            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
            {
                // TextBlock.TopFraction is 0 at the top of the page. Tesseract rects
                // use top-left origin (Y1 = top edge, y grows down), so Y1/height maps
                // directly onto that convention.
                double topFraction = imageHeight > 0
                    ? Math.Clamp((double)rect.Y1 / imageHeight, 0, 1)
                    : 0;

                // Page number / folio lines are dropped here; header/footer bands and
                // page numbers are filtered again by the parser.
                if (NumberOnlyLine.IsMatch(line) && (topFraction > 0.88 || topFraction < 0.04))
                    continue;

                int logicalPage = pageNum;
                if (mode == TwoUpMode.ColumnCentre && isWide)
                {
                    int centre = rect.X1 + (rect.X2 - rect.X1) / 2;
                    logicalPage = centre < imageWidth / 2 ? pageNum * 2 - 1 : pageNum * 2;
                }

                lines.Add((logicalPage, topFraction, line));
            }
        } while (iter.Next(PageIteratorLevel.TextLine));

        foreach (var l in lines.OrderBy(l => l.Top))
        {
            result.Add(new TextBlock
            {
                LogicalPage = l.LogicalPage,
                PhysicalPage = pageNum,
                Text = l.Text,
                TopFraction = l.Top,
                FontSize = 0,
                IsBold = false,
            });
        }

        return result;
    }

    private static void CollectBlocks(
        TesseractEngine engine,
        Pix pix,
        Rect region,
        int logicalPage,
        int physicalPage,
        int imageHeight,
        List<TextBlock> result)
    {
        using var page = engine.Process(pix, region, PageSegMode.Auto);
        using var iter = page.GetIterator();
        iter.Begin();
        do
        {
            string line = (iter.GetText(PageIteratorLevel.TextLine) ?? "").Trim();
            if (line.Length == 0) continue;

            if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
            {
                double topFraction = imageHeight > 0
                    ? Math.Clamp((double)rect.Y1 / imageHeight, 0, 1)
                    : 0;

                if (NumberOnlyLine.IsMatch(line) && (topFraction > 0.88 || topFraction < 0.04))
                    continue;

                result.Add(new TextBlock
                {
                    LogicalPage = logicalPage,
                    PhysicalPage = physicalPage,
                    Text = line,
                    TopFraction = topFraction,
                    FontSize = 0,
                    IsBold = false,
                });
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