namespace DungeonWorld.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? AvatarPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Player stats (Fighting Fantasy style: SKILL / STAMINA / LUCK)
    public int Skill { get; set; }
    public int Stamina { get; set; }
    public int Luck { get; set; }

    // Experience points earned along adventures (used to unlock items & spells later)
    public int Experience { get; set; }

    public Subscription? Subscription { get; set; }
    public List<Achievement> Achievements { get; set; } = new();
    public List<UserAsset> Assets { get; set; } = new();
    public List<AdventureProgress> Adventures { get; set; } = new();
}
