namespace DungeonWorld.Core.Entities;

/// <summary>
/// Catalog entry for an adventure book: title, number of sections, and the medallion
/// a player is rewarded for conquering it.
/// </summary>
public class Adventure
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookTitle { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public string? Description { get; set; }

    // Medallion rewarded to a user who conquers this adventure
    public string MedallionTitle { get; set; } = string.Empty;
    public string? MedallionDescription { get; set; }

    /// <summary>
    /// Comma-separated command names this adventure supports (a subset of the global
    /// <see cref="GameCommand"/> catalog), derived from the book's content — combat verbs only
    /// when the book has fights, casting only when it has spells.
    /// </summary>
    public string CommandsCsv { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
