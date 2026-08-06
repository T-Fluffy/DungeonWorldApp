namespace DungeonWorld.Core.Entities;

public enum SubscriptionStatus
{
    Active,
    Expired,
    Cancelled,
    PastDue
}

public class Subscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string Plan { get; set; } = "free"; // free | premium
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Active;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RenewsAt { get; set; }
}
