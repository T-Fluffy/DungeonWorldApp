using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using DungeonWorld.Cleaning.Model;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API.Controllers;

/// <summary>
/// Serves playable book content. Reads the relational content tables first (populated by
/// <see cref="BookContentImporter"/>) and falls back to the cleaned JSON files when a database
/// import has not run yet, so a fresh checkout still works before the first import.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly FileStorageOptions _storageOptions;
    private readonly string _cleanedDataPath;
    private readonly DungeonWorldDbContext _db;
    private readonly ILogger<GameController> _logger;

    public GameController(
        IOptions<FileStorageOptions> storageOptions,
        DungeonWorldDbContext db,
        ILogger<GameController> logger)
    {
        _storageOptions = storageOptions.Value;
        _db = db;
        _logger = logger;
        _cleanedDataPath = Path.Combine(Path.GetFullPath(_storageOptions.PdfUploadPath), "CleanedData");
    }

    [HttpGet("{bookTitle}/{sectionNumber}")]
    public async Task<ActionResult<CleanedSection>> GetSection(string bookTitle, int sectionNumber)
    {
        var stored = await _db.Sections
            .Include(s => s.Choices)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Book!.Title == bookTitle && s.Number == sectionNumber);

        if (stored is not null)
            return Ok(ToCleanedSection(stored));

        // Fallback: file-backed content (database not populated yet).
        string filePath = Path.Combine(_cleanedDataPath, $"{bookTitle}.json");
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = $"Book '{bookTitle}' not found. Please ingest it first." });

        try
        {
            var jsonString = await System.IO.File.ReadAllTextAsync(filePath);
            var book = JsonSerializer.Deserialize<CleanedBook>(jsonString, JsonOptions);
            var section = book?.Sections.FirstOrDefault(s => s.Number == sectionNumber);

            if (section is null)
                return NotFound(new { error = $"Section {sectionNumber} not found in '{bookTitle}'." });

            return Ok(section);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load section {SectionNumber} from {BookTitle}", sectionNumber, bookTitle);
            return StatusCode(500, new { error = "Failed to load game data." });
        }
    }

    [HttpGet("{bookTitle}/meta")]
    public async Task<IActionResult> GetBookMeta(string bookTitle)
    {
        var stored = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Title == bookTitle);
        if (stored is not null)
        {
            var rules = string.IsNullOrWhiteSpace(stored.RulesJson)
                ? new List<CleanedRule>()
                : JsonSerializer.Deserialize<List<CleanedRule>>(stored.RulesJson, JsonOptions) ?? new List<CleanedRule>();

            return Ok(new
            {
                Title = stored.Title,
                Author = stored.Author,
                Introduction = stored.Introduction,
                MapPath = stored.MapPath,
                AdventureSheetPath = stored.AdventureSheetPath,
                SectionCount = stored.SectionCount,
                PresentSectionCount = stored.PresentSectionCount,
                MissingSectionCount = stored.MissingSectionCount,
                CombatSectionCount = stored.CombatSectionCount,
                EnemyCount = stored.EnemyCount,
                Rules = rules
            });
        }

        string filePath = Path.Combine(_cleanedDataPath, $"{bookTitle}.json");
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { error = $"Book '{bookTitle}' not found. Please ingest it first." });

        try
        {
            var jsonString = System.IO.File.ReadAllText(filePath);
            var book = JsonSerializer.Deserialize<CleanedBook>(jsonString, JsonOptions);
            if (book == null)
                return StatusCode(500, new { error = "Failed to load book." });

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
    public async Task<IActionResult> ListAvailableBooks()
    {
        var titles = await _db.Books.AsNoTracking()
            .OrderBy(b => b.Title)
            .Select(b => b.Title)
            .ToListAsync();

        if (titles.Count > 0)
            return Ok(titles);

        if (!Directory.Exists(_cleanedDataPath))
            return Ok(Array.Empty<string>());

        return Ok(Directory.GetFiles(_cleanedDataPath, "*.json")
            .Select(Path.GetFileNameWithoutExtension)
            .OrderBy(t => t)
            .ToList());
    }

    /// <summary>Rebuilds the response object from the stored rows, keeping the file-era JSON shape.</summary>
    private static CleanedSection ToCleanedSection(StoredSection s)
    {
        var features = string.IsNullOrWhiteSpace(s.FeaturesJson)
            ? new CleanedFeatures()
            : JsonSerializer.Deserialize<CleanedFeatures>(s.FeaturesJson, JsonOptions) ?? new CleanedFeatures();

        // Columns are authoritative for the flags the client relies on.
        features.HasCombat = s.HasCombat;
        features.MissingText = s.MissingText;
        features.IsEnd = s.IsEnd;
        features.DeathEnd = s.DeathEnd;
        features.VictoryEnd = s.VictoryEnd;

        return new CleanedSection
        {
            Number = s.Number,
            ImagePath = s.ImagePath,
            Raw = s.Raw,
            Clean = s.Clean,
            Features = features,
            References = string.IsNullOrWhiteSpace(s.ReferencesCsv)
                ? new List<int>()
                : s.ReferencesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToList(),
            Choices = s.Choices
                .OrderBy(c => c.Target)
                .Select(c => new CleanedChoice
                {
                    Target = c.Target,
                    Label = c.Label,
                    Text = c.Text,
                    Kind = c.Kind,
                    IsDiceRoll = c.IsDiceRoll,
                })
                .ToList(),
        };
    }
}
