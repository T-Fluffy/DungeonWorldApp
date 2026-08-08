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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
