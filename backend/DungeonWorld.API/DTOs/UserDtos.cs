namespace DungeonWorld.API.DTOs;

// --- Requests ---

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? DisplayName = null);

public record LoginRequest(string UsernameOrEmail, string Password);

public record UpdateUserRequest(string? DisplayName = null, string? AvatarPath = null);

public record SubscriptionRequest(string Plan, DateTime? ExpiresAt = null);

public record AchievementRequest(string Code, string Title, string? Description = null);

public record AssetRequest(
    string Name,
    string Type,
    string? Description = null,
    string? BookTitle = null,
    int? SectionNumber = null);

public record AdventureRequest(
    string BookTitle,
    int CurrentSection,
    int? Skill = null,
    int? Stamina = null,
    int? Luck = null,
    bool IsComplete = false);

// --- Responses ---

public record AuthResponse(
    string Token,
    UserResponse User);

public record SubscriptionResponse(
    Guid Id,
    string Plan,
    string Status,
    DateTime StartedAt,
    DateTime? ExpiresAt,
    DateTime? RenewsAt);

public record AchievementResponse(
    Guid Id,
    string Code,
    string Title,
    string? Description,
    DateTime UnlockedAt);

public record AssetResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    string? BookTitle,
    int? SectionNumber,
    DateTime AcquiredAt);

public record AdventureResponse(
    Guid Id,
    string BookTitle,
    int CurrentSection,
    int? Skill,
    int? Stamina,
    int? Luck,
    DateTime UpdatedAt,
    bool IsComplete);

public record UserResponse(
    Guid Id,
    string Username,
    string Email,
    string? DisplayName,
    string? AvatarPath,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    SubscriptionResponse? Subscription,
    List<AchievementResponse> Achievements,
    List<AssetResponse> Assets,
    List<AdventureResponse> Adventures);
