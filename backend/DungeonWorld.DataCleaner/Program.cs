using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.DataCleaner.Cleaner;
using DungeonWorld.DataCleaner.Model;

var options = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    WriteIndented = true,
};

var dir = FindProcessedBooksDir();
if (dir == null)
{
    Console.Error.WriteLine("ProcessedBooks directory not found. Run from the repo root or pass --dir.");
    return 1;
}

var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
var files = Directory.GetFiles(dir, "*.json")
    .Where(f => !Path.GetFileName(f).EndsWith(".cleaned.json", StringComparison.OrdinalIgnoreCase))
    .Where(f => !Path.GetFileName(f).EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
    .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
    .ToList();

if (files.Count == 0)
{
    Console.Error.WriteLine("No book JSON files found to clean.");
    return 1;
}

foreach (var file in files)
{
    try
    {
        Console.WriteLine($"  [{Path.GetFileName(file)}] cleaning...");
        var book = JsonSerializer.Deserialize<Book>(await File.ReadAllTextAsync(file), jsonOptions);
        if (book == null)
        {
            Console.Error.WriteLine($"  Could not parse {Path.GetFileName(file)}");
            continue;
        }

        var cleaned = Clean(book, Path.GetFileName(file));
        var outPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(file) + ".cleaned.json");
        await File.WriteAllTextAsync(outPath, JsonSerializer.Serialize(cleaned, options));

        Console.WriteLine($"{Path.GetFileName(file)} -> {Path.GetFileName(outPath)}");
        Console.WriteLine($"  sections {cleaned.Meta.PresentSectionCount}/{cleaned.Meta.SectionCount} (missing {cleaned.Meta.MissingSectionCount})");
        Console.WriteLine($"  combat {cleaned.Meta.CombatSectionCount} sections, {cleaned.Meta.EnemyCount} enemies");
        Console.WriteLine($"  deadEnds {cleaned.Graph.DeadEnds.Count}, terminal {cleaned.Graph.Terminal.Count}, unreachable {cleaned.Graph.Unreachable.Count}, orphanLinks {cleaned.Graph.OrphanLinks.Count}");
        Console.WriteLine($"  rules: {string.Join(", ", cleaned.Rules.Select(r => $"{r.Stat}={r.Formula}"))}");
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  ERROR processing {Path.GetFileName(file)}: {ex.Message}");
        Console.Error.WriteLine(ex.StackTrace);
    }
}

return 0;

static CleanedBook Clean(Book book, string sourceFile)
{
    var cleaned = new CleanedBook
    {
        Meta = new CleanedMeta
        {
            Title = book.Title,
            Author = book.Author,
            SourceFile = sourceFile,
            SectionCount = book.Sections.Count,
            PresentSectionCount = book.Sections.Count,
            MissingSectionCount = 0,
            MapPath = string.IsNullOrWhiteSpace(book.MapPath) ? null : book.MapPath,
            AdventureSheetPath = string.IsNullOrWhiteSpace(book.AdventureSheetPath) ? null : book.AdventureSheetPath,
            Introduction = string.IsNullOrWhiteSpace(book.Introduction) ? null : book.Introduction,
        },
        Rules = RulesExtractor.Extract(book.Introduction),
        Sections = book.Sections.Select(ContentAnalyzer.Analyze).ToList(),
    };

    var missing = Enumerable.Range(1, cleaned.Meta.SectionCount)
        .Where(n => cleaned.Sections.All(s => s.Number != n))
        .ToList();
    cleaned.Meta.MissingSectionCount = missing.Count;

    cleaned.Meta.CombatSectionCount = cleaned.Sections.Count(s => s.Features.HasCombat);
    cleaned.Meta.EnemyCount = cleaned.Sections.Sum(s => s.Features.Enemies.Count);

    GraphAnalyzer.Build(cleaned);
    return cleaned;
}

static string? FindProcessedBooksDir()
{
    var root = Directory.GetCurrentDirectory();
    var candidates = new[]
    {
        Path.Combine(root, "backend", "Storage", "Uploads", "ProcessedBooks"),
        Path.Combine(root, "Storage", "Uploads", "ProcessedBooks"),
    };
    foreach (var candidate in candidates)
    {
        if (Directory.Exists(candidate)) return candidate;
    }
    return null;
}
