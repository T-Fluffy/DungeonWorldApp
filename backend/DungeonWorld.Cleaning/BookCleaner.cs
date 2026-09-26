using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Cleaning.Cleaner;
using DungeonWorld.Cleaning.Model;

namespace DungeonWorld.Cleaning;

/// <summary>
/// Turns a raw parsed <see cref="Book"/> into the structured <see cref="CleanedBook"/>
/// document and (optionally) persists it to disk. Raw content is always preserved.
/// </summary>
/// <param name="introOverride">Canonical introduction text for the book (each
/// adventure's intro is unique). When non-blank it replaces the OCR-derived
/// introduction for both display and rule extraction, keeping the two in sync.
/// When null, a curated embedded intro is used if one exists for the title,
/// otherwise the parsed introduction. Existing behavior unchanged for books
/// without a curated intro.</param>
public static class BookCleaner
{
    public static CleanedBook Clean(Book book, string sourceFile, string? introOverride = null)
    {
        var intro = !string.IsNullOrWhiteSpace(introOverride)
            ? introOverride
            : Text.IntroOverrides.Get(book.Title) ?? book.Introduction;
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
                Introduction = string.IsNullOrWhiteSpace(intro) ? null : intro,
            },
            Rules = RulesExtractor.Extract(intro),
            Sections = book.Sections.Select(s => ContentAnalyzer.Analyze(s, book.Sections.Count)).ToList(),
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

    /// <summary>
    /// Writes the cleaned book as <c>{Title}.json</c> into <paramref name="outputDir"/>.
    /// Never overwrites an existing file; a " (n)" suffix is used instead so earlier
    /// extractions stay available for comparison.
    /// </summary>
    public static string WriteCleanedBook(CleanedBook cleaned, string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var outPath = UniquePath(Path.Combine(outputDir, $"{cleaned.Meta.Title}.json"));
        var json = JsonSerializer.Serialize(cleaned, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outPath, json);
        return outPath;
    }

    private static string UniquePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;

        string dir = Path.GetDirectoryName(desiredPath)!;
        string name = Path.GetFileNameWithoutExtension(desiredPath);
        string ext = Path.GetExtension(desiredPath);

        for (int i = 1; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
    }
}
