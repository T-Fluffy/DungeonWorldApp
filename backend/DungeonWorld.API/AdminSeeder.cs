using DungeonWorld.Core.Entities;
using DungeonWorld.Infrastructure.Helpers;
using DungeonWorld.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DungeonWorld.API;

/// <summary>
/// Bootstraps the initial administrator account from configuration
/// ("Admin" section). Runs idempotently at startup.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DungeonWorldDbContext>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var username = config["Admin:Username"]?.Trim();
        var password = config["Admin:Password"];
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return;

        if (await db.Users.AnyAsync(u => u.Username == username))
            return;

        db.Users.Add(new User
        {
            Username = username,
            Email = config["Admin:Email"]?.Trim().ToLowerInvariant() ?? $"{username.ToLowerInvariant()}@localhost",
            PasswordHash = PasswordHasher.Hash(password),
            Role = "Admin",
            DisplayName = "Game Master",
            Skill = 12,
            Stamina = 24,
            Luck = 12
        });

        await db.SaveChangesAsync();
    }
}
