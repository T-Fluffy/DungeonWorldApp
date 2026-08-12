using System.Text.Json;
using DungeonWorld.Core.Entities;
using DungeonWorld.Core.Options;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DungeonWorld.API;

/// <summary>
/// Seeds the game catalog from the processed book files and adds default game commands.
/// Runs idempotently at startup.
/// </summary>
public static class CatalogSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DungeonWorldDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IOptions<FileStorageOptions>>().Value;

        await SeedCommandsAsync(db);

        var processedPath = Path.Combine(
            Path.GetFullPath(storage.PdfUploadPath), "ProcessedBooks");

        if (!Directory.Exists(processedPath))
            return;

        var itemCount = await db.Items.CountAsync();
        var spellCount = await db.Spells.CountAsync();

        // Multiple processed files may share a title (e.g. "(1)"/"(2)" split copies);
        // only one Adventure row may exist per title, so track what we've queued.
        var queuedTitles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in Directory.GetFiles(processedPath, "*.json"))
        {
            var bookTitle = Path.GetFileNameWithoutExtension(file);

            try
            {
                var jsonString = await System.IO.File.ReadAllTextAsync(file);
                var book = JsonSerializer.Deserialize<Book>(jsonString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (book == null || string.IsNullOrWhiteSpace(book.Title))
                    continue;

                var existing = await db.Adventures.FirstOrDefaultAsync(a => a.BookTitle == book.Title);
                if (existing == null && queuedTitles.Add(book.Title))
                {
                    db.Adventures.Add(new Adventure
                    {
                        BookTitle = book.Title,
                        SectionCount = book.Sections.Count,
                        MedallionTitle = $"Medallion of {book.Title}",
                        MedallionDescription = $"Awarded for conquering the adventure of {book.Title}."
                    });
                }
                else if (existing != null)
                {
                    existing.SectionCount = book.Sections.Count;
                }

                // Heuristic item & spell extraction from the book's prose.
                if (itemCount == 0 || spellCount == 0)
                {
                    var readable = book.Sections
                        .Where(s => s.Content != "[Text missing or unreadable in PDF]")
                        .ToList();

                    if (itemCount == 0)
                    {
                        foreach (var section in readable)
                        {
                            var norm = GameContentExtractor.Normalize(section.Content);
                            foreach (var item in GameContentExtractor.ExtractItems(norm, section.SectionNumber))
                            {
                                db.Items.Add(new GameItem
                                {
                                    Name = item.Name ?? "",
                                    Type = item.Type,
                                    Rarity = item.Rarity,
                                    BookTitle = book.Title,
                                    SectionNumber = item.SectionNumber > 0 ? item.SectionNumber : null,
                                    Description = $"Found in {book.Title}, section {item.SectionNumber}.",
                                    Effects = null
                                });
                            }
                        }
                    }

                    if (spellCount == 0)
                    {
                        foreach (var section in readable)
                        {
                            var norm = GameContentExtractor.Normalize(section.Content);
                            foreach (var spell in GameContentExtractor.ExtractSpells(norm, section.SectionNumber))
                            {
                                db.Spells.Add(new Spell
                                {
                                    Name = spell.Name,
                                    Type = spell.Type,
                                    BookTitle = book.Title,
                                    SectionNumber = section.SectionNumber,
                                    Description = $"Learned in {book.Title}, section {section.SectionNumber}."
                                });
                            }
                        }
                    }
                }
            }
            catch
            {
                // Skip unreadable files rather than crashing startup.
            }
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedCommandsAsync(DungeonWorldDbContext db)
    {
        if (await db.Commands.AnyAsync())
            return;

        db.Commands.AddRange(
            new GameCommand { Name = "GO", Aliases = new[] { "GOTO", "NEXT", "JUMP" }, Usage = "GO <section>", Category = "navigation", Description = "Jump to a section of the adventure." },
            new GameCommand { Name = "LOOK", Aliases = new[] { "EXAMINE", "INSPECT", "READ" }, Usage = "LOOK", Category = "navigation", Description = "Re-read the current section." },
            new GameCommand { Name = "BACK", Aliases = new[] { "RETURN", "PREV" }, Usage = "BACK", Category = "navigation", Description = "Return to the previous section." },
            new GameCommand { Name = "INVENTORY", Aliases = new[] { "INV", "BAG", "ITEMS" }, Usage = "INVENTORY", Category = "inventory", Description = "Show the items you are carrying." },
            new GameCommand { Name = "TAKE", Aliases = new[] { "GET", "PICKUP", "GRAB" }, Usage = "TAKE <item>", Category = "inventory", Description = "Pick up an item." },
            new GameCommand { Name = "USE", Aliases = new[] { "EQUIP", "WIELD" }, Usage = "USE <item>", Category = "inventory", Description = "Use or equip an item from your inventory." },
            new GameCommand { Name = "DROP", Aliases = new[] { "DISCARD", "REMOVE" }, Usage = "DROP <item>", Category = "inventory", Description = "Drop an item from your inventory." },
            new GameCommand { Name = "CAST", Aliases = new[] { "SPELL", "CHANNEL" }, Usage = "CAST <spell>", Category = "combat", Description = "Cast a spell you have learned." },
            new GameCommand { Name = "FIGHT", Aliases = new[] { "ATTACK", "STRIKE", "BATTLE" }, Usage = "FIGHT <monster>", Category = "combat", Description = "Engage an enemy in combat." },
            new GameCommand { Name = "FLEE", Aliases = new[] { "RUN", "ESCAPE", "FLEE!" }, Usage = "FLEE", Category = "combat", Description = "Try to escape from combat." },
            new GameCommand { Name = "ROLL", Aliases = new[] { "DICE", "ROLLDICE", "ROLL DICE" }, Usage = "ROLL <n>d<m>", Category = "combat", Description = "Roll dice for the adventure's checks (defaults to 2d6)." },
            new GameCommand { Name = "SAVE", Aliases = new[] { "SAVEGAME", "SNAPSHOT" }, Usage = "SAVE", Category = "system", Description = "Save your current progress." },
            new GameCommand { Name = "HELP", Aliases = new[] { "?", "COMMANDS", "HINT" }, Usage = "HELP", Category = "system", Description = "List all available commands." },
            new GameCommand { Name = "RESTART", Aliases = new[] { "RESET", "NEWGAME" }, Usage = "RESTART", Category = "system", Description = "Restart the adventure from the beginning." },
            new GameCommand { Name = "REREAD", Aliases = new[] { "REPEAT", "AGAIN" }, Usage = "REREAD", Category = "lore", Description = "Show the last section again." });

        await db.SaveChangesAsync();
    }
}
