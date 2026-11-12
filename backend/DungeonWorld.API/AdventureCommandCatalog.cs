using DungeonWorld.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DungeonWorld.API;

/// <summary>
/// Decides which commands each adventure offers and stores that per-adventure set on the
/// <see cref="Core.Entities.Adventure"/> row. The global catalog (<c>Commands</c> table) lists
/// every verb the engine knows; an adventure only advertises the ones that can actually do
/// something in that book.
/// </summary>
public static class AdventureCommandCatalog
{
    // Every adventure can navigate, inspect, save and restart.
    private static readonly string[] Always =
    {
        "GO", "LOOK", "BACK", "REREAD", "INVENTORY", "SAVE", "RESTART", "HELP",
    };

    // Verbs that only make sense when the book contains fights.
    private static readonly string[] Combat = { "FIGHT", "FLEE", "ROLL" };

    // Verbs backed by the reader's pack. Every book mentions items.
    private static readonly string[] Inventory = { "TAKE", "USE", "DROP" };

    public static List<string> ForAdventure(int combatSectionCount, bool hasSpells)
    {
        var commands = new List<string>(Always);
        if (combatSectionCount > 0) commands.AddRange(Combat);
        commands.AddRange(Inventory);
        if (hasSpells) commands.Add("CAST");
        return commands;
    }

    /// <summary>Recomputes <c>CommandsCsv</c> for every adventure from its imported book.</summary>
    public static async Task SyncAsync(DungeonWorldDbContext db, ILogger? logger = null)
    {
        var combatByTitle = await db.Books.AsNoTracking()
            .ToDictionaryAsync(b => b.Title, b => b.CombatSectionCount);

        var titlesWithSpells = await db.Spells.AsNoTracking()
            .Select(s => s.BookTitle)
            .Distinct()
            .ToListAsync();
        var spellTitles = new HashSet<string>(titlesWithSpells, StringComparer.OrdinalIgnoreCase);

        var adventures = await db.Adventures.ToListAsync();
        int updated = 0;

        foreach (var adventure in adventures)
        {
            int combat = combatByTitle.GetValueOrDefault(adventure.BookTitle);
            bool hasSpells = spellTitles.Contains(adventure.BookTitle);

            var commands = ForAdventure(combat, hasSpells);
            var csv = string.Join(',', commands);
            if (!string.Equals(adventure.CommandsCsv, csv, StringComparison.Ordinal))
            {
                adventure.CommandsCsv = csv;
                updated++;
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync();
            logger?.LogInformation("Adventure command sets updated for {Count} book(s).", updated);
        }
    }
}
