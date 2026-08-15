# API

ASP.NET Core Web API in `backend/DungeonWorld.API`. Composition root is
`Program.cs`; the controllers implement the REST surface.

## Startup (`Program.cs`)

1. Config: `FileStorageOptions` (PDF/image/avatar paths), `JwtOptions`.
2. JWT bearer auth + authorization.
3. EF Core persistence (`AddPersistence(connectionString)` from Infrastructure).
4. Parsing services (see [`parsing-pipeline.md`](parsing-pipeline.md)): the
   `IPdfTextExtractor` and the `IBookParser` registrations plus
   `DungeonWorldParserFactory`.
5. Controllers, Swagger, CORS (default `http://localhost:5173`).
6. Static files: game art served from `/assets/game-art`, avatars from
   `/assets/avatars` (physical providers configured from `FileStorageOptions`).
7. Global exception handler (logs, returns a generic 500, never leaks stack traces).
8. On startup: `db.Database.Migrate()`, `AdminSeeder.SeedAsync`, `CatalogSeeder.SeedAsync`.

## Auth (`Auth/JwtTokenIssuer.cs`, `Program.cs`)

- `ITokenIssuer` issues JWTs on login/register (validated with the configured key,
  issuer, audience).
- Admin endpoints are `[Authorize(Roles = "Admin")]`; player endpoints resolve the
  user id from the JWT claims (`ClaimTypes.NameIdentifier` / `sub`).
- `UserController` hashes passwords with PBKDF2 (`Helpers/PasswordHasher`).

## Controllers

### `AdminController` — `[Authorize(Roles = "Admin")]` — `api/admin`

| Route | Purpose |
| --- | --- |
| `POST upload` | Save an uploaded PDF to `Storage/Books/` (200 MB limit, `.pdf` only) |
| `POST ingest?fileName=` | Resolve the parser via `IParserFactory`, `ParseAsync`, then `BookCleaner.Clean` + write `CleanedData/<Title>.json`; returns parser used, section count, map found |
| `POST analyze-layout?fileName=` | Diagnostic: `PdfPigLayoutAnalyzer.IsDoublePageLayout` → report double/single layout |

### `GameController` — `api/game`

| Route | Purpose |
| --- | --- |
| `GET list-books` | Titles of every file in `CleanedData/` |
| `GET {bookTitle}/meta` | `BookMetaDto` (title, author, introduction, map/adventure-sheet paths, counts, rules) |
| `GET {bookTitle}/{sectionNumber}` | One `CleanedSection` |

Reads the cleaned JSON directly from `Storage/Books/CleanedData/` (no database for
game content).

### `UserController` — `api/user`

Player account endpoints:

| Route | Purpose |
| --- | --- |
| `POST register` | Create account, hash password, issue token |
| `POST login` | Authenticate, issue token |
| `GET me` | Full profile (user + subscription + achievements + assets + adventures) |
| `PUT me` | Update profile (display name, avatar path, SKILL/STAMINA/LUCK, XP) |
| `POST me/avatar` / `DELETE me/avatar` | Upload / remove avatar |
| `POST me/subscription` / `GET me/subscription` | Upsert / read subscription |
| `GET me/achievements` / `POST me/achievements` | Read / unlock achievements |
| `GET me/assets` / `POST me/assets` | Read / add assets |
| `GET me/adventures` / `POST me/adventures` | Read / save adventure progress; `GET me/adventures/{book}` |

`UserController` uses `Query()` to eagerly load subscription, achievements, assets,
and adventures with every user read.

### `CatalogController` — `api/catalog`

Seeded game catalog (admin CRUD + public reads):

| Route | Purpose |
| --- | --- |
| `GET items` / `GET spells` / `GET commands` / `GET adventures` | Public reads |
| `POST/PUT/DELETE …` | Admin writes (seeded `Items`, `Spells`, `Commands`, `Adventures`) |

## Seeding

- **`AdminSeeder`** — creates the initial admin from `Admin:Username` / `Admin:Email`
  / `Admin:Password` configuration.
- **`CatalogSeeder`** — seeds commands (the Chronicle command vocabulary), adventures
  (from cleaned book metadata), and heuristic items/spells extracted from processed
  book prose via **`GameContentExtractor`**.

## DTOs (`DTOs/UserDtos.cs`, controller-local objects)

API responses are shaped with small DTOs (e.g. `BookMetaDto` in `GameController`,
user DTOs in `UserDtos.cs`). The frontend mirrors these in `frontend/src/api/client.ts`.
