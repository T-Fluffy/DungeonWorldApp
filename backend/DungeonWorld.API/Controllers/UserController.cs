using System.Security.Claims;
using DungeonWorld.API.Auth;
using DungeonWorld.API.DTOs;
using DungeonWorld.Core.Entities;
using DungeonWorld.Infrastructure.Helpers;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly DungeonWorldDbContext _db;
    private readonly ITokenIssuer _tokenIssuer;

    public UserController(DungeonWorldDbContext db, ITokenIssuer tokenIssuer)
    {
        _db = db;
        _tokenIssuer = tokenIssuer;
    }

    private IQueryable<User> Query() =>
        _db.Users
            .Include(u => u.Subscription)
            .Include(u => u.Achievements)
            .Include(u => u.Assets)
            .Include(u => u.Adventures);

    // The authenticated user's id always comes from the JWT, never from the route.
    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                      User.FindFirstValue("sub"), out var id) ? id : null;

    // --- Auth ---

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password are required." });

        var username = request.Username.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();

        var exists = await _db.Users.AnyAsync(u =>
            u.Username == username || (email != null && u.Email == email));

        if (exists)
            return Conflict(new { error = "A user with that name or email already exists." });

        var user = new User
        {
            Username = username,
            Email = email ?? "",
            PasswordHash = PasswordHasher.Hash(request.Password),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim()
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), ToDto(user, _tokenIssuer.CreateToken(user)));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = "Username and password are required." });

        var user = await Query().FirstOrDefaultAsync(u =>
            u.Username == request.UsernameOrEmail ||
            u.Email == request.UsernameOrEmail);

        if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { error = "Invalid credentials." });

        user.LastLoginAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(ToDto(user, _tokenIssuer.CreateToken(user)));
    }

    // --- Profile ---

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetUser()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var user = await Query().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        return Ok(ToUserResponse(user));
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateUser(UpdateUserRequest request)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var user = await Query().FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        if (request.DisplayName != null)
            user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        if (request.AvatarPath != null)
            user.AvatarPath = request.AvatarPath;

        await _db.SaveChangesAsync();
        return Ok(ToUserResponse(user));
    }

    [Authorize]
    [HttpDelete("me")]
    public async Task<IActionResult> DeleteUser()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { error = "User not found." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Subscription ---

    [Authorize]
    [HttpGet("me/subscription")]
    public async Task<IActionResult> GetSubscription()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        return Ok(subscription == null ? null : ToDto(subscription));
    }

    [Authorize]
    [HttpPost("me/subscription")]
    public async Task<IActionResult> UpsertSubscription(SubscriptionRequest request)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(request.Plan))
            return BadRequest(new { error = "Plan is required." });

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (subscription == null)
        {
            subscription = new Subscription
            {
                UserId = userId.Value,
                Plan = request.Plan,
                ExpiresAt = request.ExpiresAt,
                RenewsAt = request.ExpiresAt
            };
            _db.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Plan = request.Plan;
            subscription.ExpiresAt = request.ExpiresAt;
            subscription.RenewsAt = request.ExpiresAt;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(subscription));
    }

    [Authorize]
    [HttpDelete("me/subscription")]
    public async Task<IActionResult> DeleteSubscription()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == userId);
        if (subscription == null)
            return NotFound(new { error = "Subscription not found." });

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Achievements ---

    [Authorize]
    [HttpGet("me/achievements")]
    public async Task<IActionResult> GetAchievements()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var achievements = await _db.Achievements
            .Where(a => a.UserId == userId)
            .OrderBy(a => a.UnlockedAt)
            .ToListAsync();

        return Ok(achievements.Select(ToDto));
    }

    [Authorize]
    [HttpPost("me/achievements")]
    public async Task<IActionResult> UnlockAchievement(AchievementRequest request)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Code and Title are required." });

        var code = request.Code.Trim().ToUpperInvariant();

        var alreadyUnlocked = await _db.Achievements
            .AnyAsync(a => a.UserId == userId && a.Code == code);

        if (alreadyUnlocked)
            return Conflict(new { error = $"Achievement '{code}' is already unlocked." });

        var achievement = new Achievement
        {
            UserId = userId.Value,
            Code = code,
            Title = request.Title.Trim(),
            Description = request.Description
        };

        _db.Achievements.Add(achievement);
        await _db.SaveChangesAsync();

        return Ok(ToDto(achievement));
    }

    [Authorize]
    [HttpDelete("me/achievements/{achievementId:guid}")]
    public async Task<IActionResult> DeleteAchievement(Guid achievementId)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var achievement = await _db.Achievements
            .FirstOrDefaultAsync(a => a.Id == achievementId && a.UserId == userId);

        if (achievement == null)
            return NotFound(new { error = "Achievement not found." });

        _db.Achievements.Remove(achievement);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Assets (items collected in adventures) ---

    [Authorize]
    [HttpGet("me/assets")]
    public async Task<IActionResult> GetAssets()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var assets = await _db.UserAssets
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.AcquiredAt)
            .ToListAsync();

        return Ok(assets.Select(ToDto));
    }

    [Authorize]
    [HttpPost("me/assets")]
    public async Task<IActionResult> AddAsset(AssetRequest request)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { error = "Name and Type are required." });

        var asset = new UserAsset
        {
            UserId = userId.Value,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = request.Description,
            BookTitle = request.BookTitle,
            SectionNumber = request.SectionNumber
        };

        _db.UserAssets.Add(asset);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssets), ToDto(asset));
    }

    [Authorize]
    [HttpDelete("me/assets/{assetId:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid assetId)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var asset = await _db.UserAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == userId);

        if (asset == null)
            return NotFound(new { error = "Asset not found." });

        _db.UserAssets.Remove(asset);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Adventure progress ---

    [Authorize]
    [HttpGet("me/adventures")]
    public async Task<IActionResult> GetAdventures()
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var adventures = await _db.AdventureProgress
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();

        return Ok(adventures.Select(ToDto));
    }

    [Authorize]
    [HttpGet("me/adventures/{bookTitle}")]
    public async Task<IActionResult> GetAdventure(string bookTitle)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var adventure = await _db.AdventureProgress
            .FirstOrDefaultAsync(a => a.UserId == userId && a.BookTitle == bookTitle);

        return Ok(adventure == null ? null : ToDto(adventure));
    }

    [Authorize]
    [HttpPost("me/adventures")]
    public async Task<IActionResult> UpsertAdventure(AdventureRequest request)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        if (string.IsNullOrWhiteSpace(request.BookTitle))
            return BadRequest(new { error = "BookTitle is required." });

        var adventure = await _db.AdventureProgress
            .FirstOrDefaultAsync(a => a.UserId == userId && a.BookTitle == request.BookTitle);

        if (adventure == null)
        {
            adventure = new AdventureProgress
            {
                UserId = userId.Value,
                BookTitle = request.BookTitle,
                CurrentSection = request.CurrentSection,
                Skill = request.Skill,
                Stamina = request.Stamina,
                Luck = request.Luck,
                IsComplete = request.IsComplete
            };
            _db.AdventureProgress.Add(adventure);
        }
        else
        {
            adventure.CurrentSection = request.CurrentSection;
            adventure.Skill = request.Skill;
            adventure.Stamina = request.Stamina;
            adventure.Luck = request.Luck;
            adventure.IsComplete = request.IsComplete;
            adventure.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return Ok(ToDto(adventure));
    }

    [Authorize]
    [HttpDelete("me/adventures/{bookTitle}")]
    public async Task<IActionResult> DeleteAdventure(string bookTitle)
    {
        var userId = CurrentUserId;
        if (userId == null) return Unauthorized(new { error = "Invalid token." });

        var adventure = await _db.AdventureProgress
            .FirstOrDefaultAsync(a => a.UserId == userId && a.BookTitle == bookTitle);

        if (adventure == null)
            return NotFound(new { error = "Adventure not found." });

        _db.AdventureProgress.Remove(adventure);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Mapping ---

    private AuthResponse ToDto(User u, string token) => new(token, ToUserResponse(u));

    private UserResponse ToUserResponse(User u) => new(
        u.Id,
        u.Username,
        u.Email,
        u.DisplayName,
        u.AvatarPath,
        u.CreatedAt,
        u.LastLoginAt,
        u.Subscription == null ? null : ToDto(u.Subscription),
        u.Achievements.Select(ToDto).ToList(),
        u.Assets.Select(ToDto).ToList(),
        u.Adventures.Select(ToDto).ToList());

    private static SubscriptionResponse ToDto(Subscription s) => new(
        s.Id, s.Plan, s.Status.ToString(), s.StartedAt, s.ExpiresAt, s.RenewsAt);

    private static AchievementResponse ToDto(Achievement a) => new(
        a.Id, a.Code, a.Title, a.Description, a.UnlockedAt);

    private static AssetResponse ToDto(UserAsset a) => new(
        a.Id, a.Name, a.Type, a.Description, a.BookTitle, a.SectionNumber, a.AcquiredAt);

    private static AdventureResponse ToDto(AdventureProgress a) => new(
        a.Id, a.BookTitle, a.CurrentSection, a.Skill, a.Stamina, a.Luck, a.UpdatedAt, a.IsComplete);
}
