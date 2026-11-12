namespace DungeonWorld.Core.Entities;

/// <summary>
/// Persistence projection of a cleaned book (the JSON under <c>Storage/Books/CleanedData</c>).
/// The parser-side <see cref="Book"/>/<see cref="Section"/> types model a PDF being parsed;
/// these model the finished, playable content the API serves.
/// </summary>
public class StoredBook
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Introduction { get; set; }
    public string? MapPath { get; set; }
    public string? AdventureSheetPath { get; set; }

    public int SectionCount { get; set; }
    public int PresentSectionCount { get; set; }
    public int MissingSectionCount { get; set; }
    public int CombatSectionCount { get; set; }
    public int EnemyCount { get; set; }

    /// <summary>Cleaned rules, serialized (<c>CleanedRule[]</c>) — read-only for the client.</summary>
    public string? RulesJson { get; set; }

    public DateTime ImportedAtUtc { get; set; } = DateTime.UtcNow;

    public List<StoredSection> Sections { get; set; } = new();
}
