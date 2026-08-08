using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Helpers;
using DungeonWorld.Infrastructure.Parsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IParserFactory _parserFactory; // Changed from IBookParser
    private readonly FileStorageOptions _storageOptions;

    public AdminController(
        IParserFactory parserFactory, 
        IOptions<FileStorageOptions> storageOptions)
    {
        _parserFactory = parserFactory;
        _storageOptions = storageOptions.Value;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(200_000_000)]
    public async Task<IActionResult> UploadPdf(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Only .pdf files are accepted.");

        try
        {
            var uploadsPath = Path.GetFullPath(_storageOptions.PdfUploadPath);
            Directory.CreateDirectory(uploadsPath);

            var safeName = Path.GetFileName(file.FileName);
            var fullPath = Path.Combine(uploadsPath, safeName);

            await using var stream = System.IO.File.Create(fullPath);
            await file.CopyToAsync(stream);

            return Ok(new { FileName = safeName });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Stack = ex.StackTrace });
        }
    }

    [HttpPost("ingest")]
    public async Task<IActionResult> IngestBook([FromQuery] string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return BadRequest("FileName is required.");

        try
        {
            // Resolve full path for layout analysis
            string fullPath = Path.Combine(
                Path.GetFullPath(_storageOptions.PdfUploadPath),
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) 
                    ? fileName 
                    : $"{fileName}.pdf");

            if (!System.IO.File.Exists(fullPath))
                return NotFound($"PDF not found: {fullPath}");

            // Factory selects appropriate parser based on layout
            var parser = _parserFactory.CreateParser(fullPath, 
                Path.GetFileNameWithoutExtension(fileName));
            
            var book = await parser.ParseAsync(fullPath);

            return Ok(new 
            { 
                Message = "Ingestion Successful", 
                ParserUsed = parser.ParserId,
                BookTitle = book.Title,
                ProcessedFile = $"{book.Title}.json",
                Sections = book.Sections.Count,
                MapFound = book.MapPath != null
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message, Stack = ex.StackTrace });
        }
    }

    [HttpPost("analyze-layout")]
    public IActionResult AnalyzeLayout([FromQuery] string fileName)
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
            RecommendedParser = isDouble ? "DoublePageParser" : "SinglePageParser"
        });
    }
}