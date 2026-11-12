using System.Text.Json;
using DungeonWorld.Cleaning.Model;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API;

/// <summary>
/// Imports the cleaned book JSON (<c>Storage/Books/CleanedData/*.json</c>) into the relational
/// content tables so the API can serve books from PostgreSQL instead of re-reading files.
///
/// <para>Idempotent: a book already present is skipped unless <paramref name="replace"/> is set,
/// in which case its row is deleted first (sections and choices cascade).</para>
/// </summary>
public static class BookContentImporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public sealed record ImportSummary(int Imported, int Skipped, int Sections, int Choices);

    /// <summary>Imports if the Books table is empty — used at startup so a fresh database fills itself.</summary>
    public static async Task<ImportSummary?> SeedIfEmptyAsync(IServiceProvider services, ILogger? logger = null)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DungeonWorldDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

        if (await db.Books.AnyAsync())
        {
            logger?.LogInformation("Book content tables already populated; skipping import.");
            return null;
        }

        var cleanedDir = Path.Combine(Path.GetFullPath(storage.PdfUploadPath), "CleanedData");
        return await ImportAsync(db, cleanedDir, replace: false, logger);
    }

    public static async Task<ImportSummary> ImportAsync(
        DungeonWorldDbContext db, string cleanedDir, bool replace, ILogger? logger = null)
    {
        if (!Directory.Exists(cleanedDir))
            return new ImportSummary(0, 0, 0, 0);

        var existingTitles = await db.Books.Select(b => b.Title).ToListAsync();
        var existing = new HashSet<string>(existingTitles, StringComparer.OrdinalIgnoreCase);

        int imported = 0, skipped = 0, sections = 0, choices = 0;

        foreach (var file in Directory.GetFiles(cleanedDir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            CleanedBook? book;
            try
            {
                var json = await File.ReadAllTextAsync(file);
                book = JsonSerializer.Deserialize<CleanedBook>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Could not read cleaned book {File}", Path.GetFileName(file));
                continue;
            }

            if (book?.Meta is null || string.IsNullOrWhiteSpace(book.Meta.Title))
                continue;

            var title = book.Meta.Title!;
            if (existing.Contains(title))
            {
                if (!replace)
                {
                    skipped++;
                    continue;
                }

                var stale = await db.Books.FirstOrDefaultAsync(b => b.Title == title);
                if (stale is not null) db.Books.Remove(stale);
                await db.SaveChangesAsync();
            }

            var stored = new StoredBook
            {
                Title = title,
                Author = book.Meta.Author,
                Introduction = book.Meta.Introduction,
                MapPath = book.Meta.MapPath,
                AdventureSheetPath = book.Meta.AdventureSheetPath,
                SectionCount = book.Meta.SectionCount,
                PresentSectionCount = book.Meta.PresentSectionCount,
                MissingSectionCount = book.Meta.MissingSectionCount,
                CombatSectionCount = book.Meta.CombatSectionCount,
                EnemyCount = book.Meta.EnemyCount,
                RulesJson = JsonSerializer.Serialize(book.Rules),
                ImportedAtUtc = DateTime.UtcNow,
            };

            foreach (var section in book.Sections)
            {
                var row = new StoredSection
                {
                    Number = section.Number,
                    ImagePath = section.ImagePath,
                    Raw = section.Raw ?? string.Empty,
                    Clean = section.Clean ?? string.Empty,
                    HasCombat = section.Features?.HasCombat ?? false,
                    MissingText = section.Features?.MissingText ?? false,
                    IsEnd = section.Features?.IsEnd ?? false,
                    DeathEnd = section.Features?.DeathEnd ?? false,
                    VictoryEnd = section.Features?.VictoryEnd ?? false,
                    FeaturesJson = JsonSerializer.Serialize(section.Features ?? new CleanedFeatures()),
                    ReferencesCsv = string.Join(',', section.References),
                };

                foreach (var choice in section.Choices)
                {
                    row.Choices.Add(new StoredChoice
                    {
                        Target = choice.Target,
                        Label = choice.Label,
                        Text = choice.Text,
                        Kind = choice.Kind,
                        IsDiceRoll = choice.IsDiceRoll,
                    });
                }

                stored.Sections.Add(row);
                sections += 1;
                choices += row.Choices.Count;
            }

            db.Books.Add(stored);
            await db.SaveChangesAsync();
            existing.Add(title);
            imported++;
        }

        logger?.LogInformation(
            "Book content import: {Imported} imported, {Skipped} skipped, {Sections} sections, {Choices} choices.",
            imported, skipped, sections, choices);

        return new ImportSummary(imported, skipped, sections, choices);
    }
}
