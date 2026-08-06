namespace DungeonWorld.Core.Entities;

/// <summary>
/// Saved progress inside a gamebook adventure (one row per user + book).
/// </summary>
public class AdventureProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string BookTitle { get; set; } = string.Empty;
    public int CurrentSection { get; set; } = 1;
    public int? Skill { get; set; }
    public int? Stamina { get; set; }
    public int? Luck { get; set; }
    public bool IsComplete { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
