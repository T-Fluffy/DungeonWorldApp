using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly FileStorageOptions _storageOptions;
    private readonly string _processedPath;

    public GameController(IOptions<FileStorageOptions> storageOptions)
    {
        _storageOptions = storageOptions.Value;
        // Combines the root upload path with the "ProcessedBooks" subfolder
        _processedPath = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), "ProcessedBooks");
    }

    [HttpGet("{bookTitle}/{sectionNumber}")]
    public async Task<ActionResult<Section>> GetSection(string bookTitle, int sectionNumber)
    {
        // Explicitly use System.IO to avoid CS0119 naming conflict
        string filePath = Path.Combine(_processedPath, $"{bookTitle}.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = $"Book '{bookTitle}' not found. Please ingest it first." });
        }

        try
        {
            var jsonString = await System.IO.File.ReadAllTextAsync(filePath);
            var book = JsonSerializer.Deserialize<Book>(jsonString, new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true 
            });

            var section = book?.Sections.FirstOrDefault(s => s.SectionNumber == sectionNumber);

            if (section == null)
            {
                return NotFound(new { error = $"Section {sectionNumber} not found in '{bookTitle}'." });
            }

            return Ok(section);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to load game data.", details = ex.Message });
        }
    }

    [HttpGet("{bookTitle}/meta")]
    public IActionResult GetBookMeta(string bookTitle)
    {
        string filePath = Path.Combine(_processedPath, $"{bookTitle}.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = $"Book '{bookTitle}' not found. Please ingest it first." });
        }

        try
        {
            var jsonString = System.IO.File.ReadAllText(filePath);
            var book = JsonSerializer.Deserialize<Book>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (book == null)
            {
                return StatusCode(500, new { error = "Failed to load book." });
            }

            return Ok(new
            {
                Title = book.Title,
                Introduction = book.Introduction,
                MapPath = book.MapPath,
                AdventureSheetPath = book.AdventureSheetPath,
                SectionCount = book.Sections.Count,
                HasCombatSections = book.Sections.Count(s => s.HasCombat)
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to load game data.", details = ex.Message });
        }
    }

    [HttpGet("list-books")]
    public IActionResult ListAvailableBooks()
    {
        if (!Directory.Exists(_processedPath))
            return Ok(new string[] { });

        var books = Directory.GetFiles(_processedPath, "*.json")
                             .Select(Path.GetFileNameWithoutExtension)
                             .ToList();

        return Ok(books);
    }
}