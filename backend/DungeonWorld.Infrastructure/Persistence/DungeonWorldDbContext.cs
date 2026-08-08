using DungeonWorld.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DungeonWorld.Infrastructure.Persistence;

public class DungeonWorldDbContext : DbContext
{
    public DungeonWorldDbContext(DbContextOptions<DungeonWorldDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAsset> UserAssets => Set<UserAsset>();
    public DbSet<AdventureProgress> AdventureProgress => Set<AdventureProgress>();

    public DbSet<GameItem> Items => Set<GameItem>();
    public DbSet<Spell> Spells => Set<Spell>();
    public DbSet<GameCommand> Commands => Set<GameCommand>();
    public DbSet<Adventure> Adventures => Set<Adventure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();

            entity.HasOne(u => u.Subscription)
                  .WithOne(s => s.User)
                  .HasForeignKey<Subscription>(s => s.UserId);

            entity.HasMany(u => u.Achievements)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId);

            entity.HasMany(u => u.Assets)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId);

            entity.HasMany(u => u.Adventures)
                  .WithOne(a => a.User)
                  .HasForeignKey(a => a.UserId);
        });

        modelBuilder.Entity<Achievement>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.Code }).IsUnique();
        });

        modelBuilder.Entity<AdventureProgress>(entity =>
        {
            entity.HasIndex(a => new { a.UserId, a.BookTitle }).IsUnique();
        });

        modelBuilder.Entity<GameItem>(entity =>
        {
            entity.HasIndex(i => i.Name);
        });

        modelBuilder.Entity<Spell>(entity =>
        {
            entity.HasIndex(s => s.Name);
        });

        modelBuilder.Entity<GameCommand>(entity =>
        {
            entity.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Adventure>(entity =>
        {
            entity.HasIndex(a => a.BookTitle).IsUnique();
        });
    }
}
