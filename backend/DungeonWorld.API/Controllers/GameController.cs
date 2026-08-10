using System.Text.Json;
using DungeonWorld.Core.Options;
using DungeonWorld.Cleaning.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly FileStorageOptions _storageOptions;
    private readonly string _cleanedDataPath;
    private readonly ILogger<GameController> _logger;

    public GameController(IOptions<FileStorageOptions> storageOptions, ILogger<GameController> logger)
    {
        _storageOptions = storageOptions.Value;
        _logger = logger;
        // Cleaned data (structured book JSON) lives under the root upload path.
        _cleanedDataPath = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), "CleanedData");
    }

    [HttpGet("{bookTitle}/{sectionNumber}")]
    public async Task<ActionResult<CleanedSection>> GetSection(string bookTitle, int sectionNumber)
    {
        string filePath = Path.Combine(_cleanedDataPath, $"{bookTitle}.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = $"Book '{bookTitle}' not found. Please ingest it first." });
        }

        try
        {
            var jsonString = await System.IO.File.ReadAllTextAsync(filePath);
            var book = JsonSerializer.Deserialize<CleanedBook>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var section = book?.Sections.FirstOrDefault(s => s.Number == sectionNumber);

            if (section == null)
            {
                return NotFound(new { error = $"Section {sectionNumber} not found in '{bookTitle}'." });
            }

            return Ok(section);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section {SectionNumber} from {BookTitle}", sectionNumber, bookTitle);
            return StatusCode(500, new { error = "Failed to load game data." });
        }
    }

    [HttpGet("{bookTitle}/meta")]
    public IActionResult GetBookMeta(string bookTitle)
    {
        string filePath = Path.Combine(_cleanedDataPath, $"{bookTitle}.json");

        if (!System.IO.File.Exists(filePath))
        {
            return NotFound(new { error = $"Book '{bookTitle}' not found. Please ingest it first." });
        }

        try
        {
            var jsonString = System.IO.File.ReadAllText(filePath);
            var book = JsonSerializer.Deserialize<CleanedBook>(jsonString, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (book == null)
            {
                return StatusCode(500, new { error = "Failed to load book." });
            }

            return Ok(new
            {
                Title = book.Meta.Title,
                Author = book.Meta.Author,
                Introduction = book.Meta.Introduction,
                MapPath = book.Meta.MapPath,
                AdventureSheetPath = book.Meta.AdventureSheetPath,
                SectionCount = book.Meta.SectionCount,
                PresentSectionCount = book.Meta.PresentSectionCount,
                MissingSectionCount = book.Meta.MissingSectionCount,
                CombatSectionCount = book.Meta.CombatSectionCount,
                EnemyCount = book.Meta.EnemyCount,
                Rules = book.Rules
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load metadata for {BookTitle}", bookTitle);
            return StatusCode(500, new { error = "Failed to load game data." });
        }
    }

    [HttpGet("list-books")]
    public IActionResult ListAvailableBooks()
    {
        if (!Directory.Exists(_cleanedDataPath))
            return Ok(new string[] { });

        var books = Directory.GetFiles(_cleanedDataPath, "*.json")
                             .Select(Path.GetFileNameWithoutExtension)
                             .ToList();

        return Ok(books);
    }
}
