using DungeonWorld.Core.Options;
using DungeonWorld.Cleaning;
using DungeonWorld.Infrastructure.Parsing;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IParserFactory _parserFactory; // Changed from IBookParser
    private readonly FileStorageOptions _storageOptions;
    private readonly DungeonWorldDbContext _db;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IParserFactory parserFactory,
        IOptions<FileStorageOptions> storageOptions,
        DungeonWorldDbContext db,
        ILogger<AdminController> logger)
    {
        _parserFactory = parserFactory;
        _storageOptions = storageOptions.Value;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Imports (or re-imports) the cleaned book JSON into the Books/Sections/Choices tables.
    /// Runs automatically on an empty database; this endpoint forces a refresh after re-ingesting.
    /// </summary>
    [HttpPost("import-books")]
    public async Task<IActionResult> ImportBooks([FromQuery] bool replace = false)
    {
        var cleanedDir = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), "CleanedData");
        var summary = await BookContentImporter.ImportAsync(_db, cleanedDir, replace, _logger);

        var totals = new
        {
            books = await _db.Books.CountAsync(),
            sections = await _db.Sections.CountAsync(),
            choices = await _db.Choices.CountAsync()
        };

        return Ok(new
        {
            imported = summary.Imported,
            skipped = summary.Skipped,
            sectionsTouched = summary.Sections,
            choicesTouched = summary.Choices,
            totals
        });
    }

    [HttpPost("upload")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file uploaded." });

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { error = "Only .pdf files are accepted." });

        try
        {
            var booksPath = Path.GetFullPath(_storageOptions.PdfUploadPath);
            Directory.CreateDirectory(booksPath);

            var safeName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(booksPath, safeName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            return Ok(new { FileName = safeName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload PDF {FileName}", file.FileName);
            return StatusCode(500, new { error = "Failed to save the uploaded file." });
        }
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestBook([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest(new { error = "FileName is required." });

        try
        {
            // Resolve full path for layout analysis
            string fullPath = Path.Combine(
                Path.GetFullPath(_storageOptions.PdfUploadPath),
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? fileName
                    : $"{fileName}.pdf");

            if (!System.IO.File.Exists(fullPath))
                return NotFound(new { error = $"PDF not found: {fullPath}" });

            // Factory selects appropriate parser based on layout
            var parser = _parserFactory.CreateParser(fullPath,
                Path.GetFileNameWithoutExtension(fileName));

            var book = await parser.ParseAsync(fullPath);

            // Generate the structured cleaned book (the game API's primary source).
            var cleaned = BookCleaner.Clean(book, $"{book.Title}.json");
            var cleanedDataDir = Path.Combine(
                Path.GetFullPath(_storageOptions.PdfUploadPath), "CleanedData");
            BookCleaner.WriteCleanedBook(cleaned, cleanedDataDir);

            return Ok(new
            {
                Message = "Ingestion Successful",
                ParserUsed = parser.ParserId,
                BookTitle = book.Title,
                ProcessedFile = $"{book.Title}.json",
                CleanedFile = $"{book.Title}.json",
                Sections = book.Sections.Count,
                MapFound = book.MapPath != null
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest book {FileName}", fileName);
            return StatusCode(500, new { error = "Book ingestion failed." });
        }
    }

    [HttpPost("analyze-layout")]
    public IActionResult AnalyzeLayout([FromQuery] string fileName)
    {
        try
        {
            // Diagnostic endpoint to check layout detection
            string fullPath = Path.Combine(
                Path.GetFullPath(_storageOptions.PdfUploadPath),
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                    ? fileName
                    : $"{fileName}.pdf");

            var analyzer = new PdfPigLayoutAnalyzer();
            bool isDouble = analyzer.IsDoublePageLayout(fullPath);

            return Ok(new
            {
                File = fileName,
                DetectedLayout = isDouble ? "DoublePage (2-up)" : "SinglePage",
                // The block-based pipeline auto-splits 2-up scans; one parser covers both.
                RecommendedParser = "DefaultDungeonWorldParser"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze layout for {FileName}", fileName);
            return StatusCode(500, new { error = "Layout analysis failed." });
        }
    }
}