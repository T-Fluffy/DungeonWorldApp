namespace DungeonWorld.API.DTOs;

// --- Requests ---

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? DisplayName = null,
    int Skill = 10,
    int Stamina = 20,
    int Luck = 10);

public record LoginRequest(string UsernameOrEmail, string Password);

public record UpdateUserRequest(
    string? DisplayName = null,
    string? AvatarPath = null,
    int? Skill = null,
    int? Stamina = null,
    int? Luck = null,
    int? Experience = null);

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

// --- Game catalog requests ---

public record ItemRequest(
    string Name,
    string Type,
    string? Description = null,
    string Rarity = "common",
    string? BookTitle = null,
    int? SectionNumber = null,
    int RequiredLevel = 0,
    int? RequiredSkill = null,
    int? RequiredStamina = null,
    int? RequiredLuck = null,
    string? Effects = null);

public record SpellRequest(
    string Name,
    string Type,
    string? Description = null,
    string? Effects = null,
    string? BookTitle = null,
    int? SectionNumber = null,
    int RequiredLevel = 0,
    int? RequiredSkill = null,
    int? RequiredStamina = null,
    int? RequiredLuck = null);

public record GameCommandRequest(
    string Name,
    string Description,
    string Usage,
    string Category = "navigation",
    string[]? Aliases = null);

public record AdventureCatalogRequest(
    string BookTitle,
    int SectionCount,
    string? Description = null,
    string MedallionTitle = "",
    string? MedallionDescription = null);

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
    string Role,
    string? DisplayName,
    string? AvatarPath,
    int Skill,
    int Stamina,
    int Luck,
    int Experience,
    DateTime CreatedAt,
    DateTime? LastLoginAt,
    SubscriptionResponse? Subscription,
    List<AchievementResponse> Achievements,
    List<AssetResponse> Assets,
    List<AdventureResponse> Adventures);

// --- Game catalog responses ---

public record ItemResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    string Rarity,
    string? BookTitle,
    int? SectionNumber,
    int RequiredLevel,
    int? RequiredSkill,
    int? RequiredStamina,
    int? RequiredLuck,
    string? Effects);

public record SpellResponse(
    Guid Id,
    string Name,
    string Type,
    string? Description,
    string? Effects,
    string? BookTitle,
    int? SectionNumber,
    int RequiredLevel,
    int? RequiredSkill,
    int? RequiredStamina,
    int? RequiredLuck);

public record GameCommandResponse(
    Guid Id,
    string Name,
    string[] Aliases,
    string Description,
    string Usage,
    string Category);

public record AdventureCatalogResponse(
    Guid Id,
    string BookTitle,
    int SectionCount,
    string? Description,
    string MedallionTitle,
    string? MedallionDescription);
