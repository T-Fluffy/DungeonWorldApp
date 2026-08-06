using DungeonWorld.API.DTOs;
using DungeonWorld.Core.Entities;
using DungeonWorld.Infrastructure.Helpers;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DungeonWorld.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly DungeonWorldDbContext _db;

    public UserController(DungeonWorldDbContext db)
    {
        _db = db;
    }

    private IQueryable<User> Query() =>
        _db.Users
            .Include(u => u.Subscription)
            .Include(u => u.Achievements)
            .Include(u => u.Assets)
            .Include(u => u.Adventures);

    // --- Auth ---

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

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToDto(user));
    }

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

        return Ok(ToDto(user));
    }

    // --- Profile ---

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await Query().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound(new { error = "User not found." });

        return Ok(ToDto(user));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateUser(Guid id, UpdateUserRequest request)
    {
        var user = await Query().FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
            return NotFound(new { error = "User not found." });

        if (request.DisplayName != null)
            user.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? null : request.DisplayName.Trim();
        if (request.AvatarPath != null)
            user.AvatarPath = request.AvatarPath;

        await _db.SaveChangesAsync();
        return Ok(ToDto(user));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { error = "User not found." });

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Subscription ---

    [HttpGet("{id:guid}/subscription")]
    public async Task<IActionResult> GetSubscription(Guid id)
    {
        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == id);

        return Ok(subscription == null ? null : ToDto(subscription));
    }

    [HttpPost("{id:guid}/subscription")]
    public async Task<IActionResult> UpsertSubscription(Guid id, SubscriptionRequest request)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == id))
            return NotFound(new { error = "User not found." });

        if (string.IsNullOrWhiteSpace(request.Plan))
            return BadRequest(new { error = "Plan is required." });

        var subscription = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.UserId == id);

        if (subscription == null)
        {
            subscription = new Subscription
            {
                UserId = id,
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

    [HttpDelete("{id:guid}/subscription")]
    public async Task<IActionResult> DeleteSubscription(Guid id)
    {
        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == id);
        if (subscription == null)
            return NotFound(new { error = "Subscription not found." });

        _db.Subscriptions.Remove(subscription);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Achievements ---

    [HttpGet("{id:guid}/achievements")]
    public async Task<IActionResult> GetAchievements(Guid id)
    {
        var achievements = await _db.Achievements
            .Where(a => a.UserId == id)
            .OrderBy(a => a.UnlockedAt)
            .ToListAsync();

        return Ok(achievements.Select(ToDto));
    }

    [HttpPost("{id:guid}/achievements")]
    public async Task<IActionResult> UnlockAchievement(Guid id, AchievementRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { error = "Code and Title are required." });

        if (!await _db.Users.AnyAsync(u => u.Id == id))
            return NotFound(new { error = "User not found." });

        var code = request.Code.Trim().ToUpperInvariant();

        var alreadyUnlocked = await _db.Achievements
            .AnyAsync(a => a.UserId == id && a.Code == code);

        if (alreadyUnlocked)
            return Conflict(new { error = $"Achievement '{code}' is already unlocked." });

        var achievement = new Achievement
        {
            UserId = id,
            Code = code,
            Title = request.Title.Trim(),
            Description = request.Description
        };

        _db.Achievements.Add(achievement);
        await _db.SaveChangesAsync();

        return Ok(ToDto(achievement));
    }

    [HttpDelete("{id:guid}/achievements/{achievementId:guid}")]
    public async Task<IActionResult> DeleteAchievement(Guid id, Guid achievementId)
    {
        var achievement = await _db.Achievements
            .FirstOrDefaultAsync(a => a.Id == achievementId && a.UserId == id);

        if (achievement == null)
            return NotFound(new { error = "Achievement not found." });

        _db.Achievements.Remove(achievement);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Assets (items collected in adventures) ---

    [HttpGet("{id:guid}/assets")]
    public async Task<IActionResult> GetAssets(Guid id)
    {
        var assets = await _db.UserAssets
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.AcquiredAt)
            .ToListAsync();

        return Ok(assets.Select(ToDto));
    }

    [HttpPost("{id:guid}/assets")]
    public async Task<IActionResult> AddAsset(Guid id, AssetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Type))
            return BadRequest(new { error = "Name and Type are required." });

        if (!await _db.Users.AnyAsync(u => u.Id == id))
            return NotFound(new { error = "User not found." });

        var asset = new UserAsset
        {
            UserId = id,
            Name = request.Name.Trim(),
            Type = request.Type.Trim(),
            Description = request.Description,
            BookTitle = request.BookTitle,
            SectionNumber = request.SectionNumber
        };

        _db.UserAssets.Add(asset);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAssets), new { id }, ToDto(asset));
    }

    [HttpDelete("{id:guid}/assets/{assetId:guid}")]
    public async Task<IActionResult> DeleteAsset(Guid id, Guid assetId)
    {
        var asset = await _db.UserAssets
            .FirstOrDefaultAsync(a => a.Id == assetId && a.UserId == id);

        if (asset == null)
            return NotFound(new { error = "Asset not found." });

        _db.UserAssets.Remove(asset);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Adventure progress ---

    [HttpGet("{id:guid}/adventures")]
    public async Task<IActionResult> GetAdventures(Guid id)
    {
        var adventures = await _db.AdventureProgress
            .Where(a => a.UserId == id)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();

        return Ok(adventures.Select(ToDto));
    }

    [HttpGet("{id:guid}/adventures/{bookTitle}")]
    public async Task<IActionResult> GetAdventure(Guid id, string bookTitle)
    {
        var adventure = await _db.AdventureProgress
            .FirstOrDefaultAsync(a => a.UserId == id && a.BookTitle == bookTitle);

        return Ok(adventure == null ? null : ToDto(adventure));
    }

    [HttpPost("{id:guid}/adventures")]
    public async Task<IActionResult> UpsertAdventure(Guid id, AdventureRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BookTitle))
            return BadRequest(new { error = "BookTitle is required." });

        if (!await _db.Users.AnyAsync(u => u.Id == id))
            return NotFound(new { error = "User not found." });

        var adventure = await _db.AdventureProgress
            .FirstOrDefaultAsync(a => a.UserId == id && a.BookTitle == request.BookTitle);

        if (adventure == null)
        {
            adventure = new AdventureProgress
            {
                UserId = id,
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

    [HttpDelete("{id:guid}/adventures/{bookTitle}")]
    public async Task<IActionResult> DeleteAdventure(Guid id, string bookTitle)
    {
        var adventure = await _db.AdventureProgress
            .FirstOrDefaultAsync(a => a.UserId == id && a.BookTitle == bookTitle);

        if (adventure == null)
            return NotFound(new { error = "Adventure not found." });

        _db.AdventureProgress.Remove(adventure);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // --- Mapping ---

    private static UserResponse ToDto(User u) => new(
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
