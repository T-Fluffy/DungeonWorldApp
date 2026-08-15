# Persistence

EF Core + Npgsql (PostgreSQL) in `backend/DungeonWorld.Infrastructure/Persistence/`.

## `DungeonWorldDbContext`

The `DbContext` (`Persistence/DungeonWorldDbContext.cs`) maps the domain entities to
PostgreSQL. Registered through `AddPersistence(connectionString)` (in
`PersistenceServiceCollectionExtensions.cs`) and configured with the Npgsql provider.

## Entities (`DungeonWorld.Core/Entities/`)

| Entity | Notes |
| --- | --- |
| `Book` | Ingested book: title, author, introduction, map/adventure-sheet paths, sections |
| `Section` | One numbered section: content, image path, choices, combat flag |
| `Choice` | A navigable option with a target section |
| `User` | Player account: hashed password, SKILL/STAMINA/LUCK, XP, avatar, role |
| `UserAsset` | Player's collected in-game assets |
| `AdventureProgress` | Saved run: book, current section, stats, complete flag |
| `Achievement` | Unlocked medallions |
| `Subscription` | Plan/status/expiry |
| `GameItem` / `Spell` / `GameCommand` / `Adventure` | Seeded game catalog |

## Migrations

`Persistence/Migrations/` contains the initial create migration
(`20260809152707_InitialCreate`) plus the model snapshot. The API applies migrations
automatically at startup (`db.Database.Migrate()` in `Program.cs`), then runs the
seeders.

## Where game content lives

Note the split: **game-book content is NOT in the database.** `GameController` reads
the cleaned book JSON files from `Storage/Books/CleanedData/`. The database holds the
player-facing data (users, progress, assets, achievements, subscriptions) and the
seeded catalog (items, spells, commands, adventure metadata).
