namespace DungeonWorld.Core.Entities;

public class Achievement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Code { get; set; } = string.Empty;      // e.g. "FIRST_BOOK", "SLAYER"
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}
