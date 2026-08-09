using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DungeonWorld.Infrastructure.Persistence;

/// <summary>
/// Enables `dotnet ef migrations` against the Infrastructure project without
/// booting the API host (a live database is not required to scaffold a migration).
/// </summary>
public class DungeonWorldDbContextFactory : IDesignTimeDbContextFactory<DungeonWorldDbContext>
{
    public DungeonWorldDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile(Path.Combine(basePath, "appsettings.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=dungeonworld;Username=dw_user;Password=dw_password";

        var options = new DbContextOptionsBuilder<DungeonWorldDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null))
            .Options;

        return new DungeonWorldDbContext(options);
    }
}
