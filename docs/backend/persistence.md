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
(`20260809152707_InitialCreate`), `AddBookContent` (the playable-content tables) and
`AddAdventureCommands` (per-adventure command sets), plus the model snapshots. The API
applies migrations automatically at startup (`db.Database.Migrate()` in `Program.cs`),
then runs the seeders.

## Where game content lives

The cleaned book JSON under `Storage/Books/CleanedData/` remains the **source of truth**
(the git-ignored data repository holds it), but it is now **projected into the database**
so the API can query it instead of re-reading files on every request:

| Table | Row | Key columns |
| --- | --- | --- |
| `Books` | one per cleaned book | `Title` (unique), `Author`, `Introduction`, `MapPath`, section/combat/enemy counts, `RulesJson` |
| `Sections` | one per book + section number | `(BookId, Number)` unique, `Raw`, `Clean`, `ImagePath`, `HasCombat`/`MissingText`/`IsEnd`/`DeathEnd`/`VictoryEnd`, `FeaturesJson`, `ReferencesCsv` |
| `Choices` | one per navigable exit | `SectionId`, `Target`, `Label`, `Text`, `Kind`, `IsDiceRoll`; indexed on `(SectionId, Target)` |

`BookContentImporter` fills them from the JSON — automatically at startup when `Books` is
empty, and on demand via `POST /api/admin/import-books?replace=true`. `Adventure.CommandsCsv`
records which commands an adventure supports (combat verbs only where the book has fights,
`CAST` only where it has spells), recomputed at startup by `AdventureCommandCatalog`.

Everything else — users, progress, assets, achievements, subscriptions, and the seeded
catalog (items, spells, commands, adventure metadata) — stays in its own tables as before.

