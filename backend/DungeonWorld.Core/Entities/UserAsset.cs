namespace DungeonWorld.Core.Entities;

/// <summary>
/// An asset the user collected during an adventure (weapon, artifact, quest item, ...).
/// </summary>
public class UserAsset
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "artifact"; // weapon | consumable | quest | artifact
    public string? Description { get; set; }
    public string? BookTitle { get; set; }          // which book it was found in
    public int? SectionNumber { get; set; }         // where it was found
    public DateTime AcquiredAt { get; set; } = DateTime.UtcNow;
}
