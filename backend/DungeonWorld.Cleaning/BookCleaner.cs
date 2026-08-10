using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Cleaning.Cleaner;
using DungeonWorld.Cleaning.Model;

namespace DungeonWorld.Cleaning;

/// <summary>
/// Turns a raw parsed <see cref="Book"/> into the structured <see cref="CleanedBook"/>
/// document and (optionally) persists it to disk. Raw content is always preserved.
/// </summary>
public static class BookCleaner
{
    public static CleanedBook Clean(Book book, string sourceFile)
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

    /// <summary>Writes the cleaned book as <c>{Title}.json</c> into <paramref name="outputDir"/>.</summary>
    public static string WriteCleanedBook(CleanedBook cleaned, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var outPath = Path.Combine(outputDir, $"{cleaned.Meta.Title}.json");
        var json = JsonSerializer.Serialize(cleaned, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outPath, json);
        return outPath;
    }
}
