# ⚔️ Dungeon World Engine (Backend)

The backend of **Dungeon World** — a .NET 8 service that turns Fighting Fantasy-style gamebook PDFs into structured, playable game data and exposes it (plus player accounts, progress, and inventory) through a REST API.

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET 8](https://img.shields.io/badge/.NET%208-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white)
![EF Core](https://img.shields.io/badge/EF_Core-512BD4?style=for-the-badge&logo=.net&logoColor=white)
![PdfPig](https://img.shields.io/badge/PdfPig-FF6F00?style=for-the-badge&logo=pdf&logoColor=white)
![xUnit](https://img.shields.io/badge/xUnit-Passing-brightgreen?style=for-the-badge&logo=github-actions)

---

## 🎯 What It Does

| Area | Capability |
| --- | --- |
| **PDF Ingestion** | Upload a gamebook PDF and parse it into structured JSON (sections, choices, images, map, front matter) |
| **Role-Gated Admin** | Ingestion + catalog writes are `[Authorize(Roles = "Admin")]`; the initial admin is seeded from configuration (`Admin:Username` / `Admin:Password`)
| **Layout Detection** | Automatically detects whether a scan is single-page or double-page (2-up) and picks the right parser |
| **Game Data API** | Read book metadata, individual sections, choices, and the list of ingested books |
| **Player Accounts** | Register / login with PBKDF2-hashed passwords, profiles, avatar upload, subscriptions, achievements |
| **Player Stats** | Fighting Fantasy-style **SKILL / STAMINA / LUCK** plus **Experience Points** earned on each conquered adventure (used to unlock items & spells) |
| **Game Catalog** | Auto-seeded `Items`, `Spells`, `Commands`, and `Adventures` tables; admin CRUD + public read endpoints |
| **Progress & Inventory** | Save adventure progress, collect assets, resume books per player |

## 🏗️ Architecture

Clean Architecture split across four projects:

```
backend/
├── DungeonWorld.Core/             # Domain model & contracts (no dependencies)
│   ├── Entities/                  # Book, Section, Choice, User, GameItem, Spell, GameCommand,
│   │                              # Adventure, Subscription, Achievement, UserAsset, AdventureProgress
│   ├── Interfaces/                # IBookParser
│   └── Options/                   # FileStorageOptions (PDF/image/avatar paths)
├── DungeonWorld.Infrastructure/   # Implementations
│   ├── Parsers/                   # SinglePageParser, DoublePageParser, DungeonWorldParserFactory
│   ├── Helpers/                   # PdfPigLayoutAnalyzer, PasswordHasher
│   ├── Interfaces/                # IParserFactory, ILayoutAnalyzer
│   └── Persistence/               # DungeonWorldDbContext (EF Core / Npgsql)
├── DungeonWorld.API/              # ASP.NET Core Web API
│   ├── Controllers/               # Admin, Game, User, Catalog
│   ├── CatalogSeeder.cs           # Startup seeding: commands, adventures, heuristic item/spell extraction
│   ├── GameContentExtractor.cs    # Heuristic item/spell extraction from processed book prose
│   ├── DTOs/                      # UserDtos
│   └── Program.cs                 # DI, CORS, Swagger, static files, schema bootstrap
├── DungeonWorld.Tests/            # xUnit + FluentAssertions
└── ML_Pipeline/                   # Python tooling for PDF→image dataset extraction (PyMuPDF, torch)
```

## 📦 API Surface

All routes are exposed under `/api`. Swagger UI is available at `/swagger` in Development.

### Admin — ingestion pipeline *(admin role only)*
| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/admin/upload` | Upload a PDF (multipart, ≤ 200 MB) |
| `POST` | `/api/admin/analyze-layout` | Diagnose single vs double-page layout of a file |
| `POST` | `/api/admin/ingest?fileName=` | Parse the PDF into structured book JSON |

### Game — reading the book
| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/game/list-books` | List all ingested book titles |
| `GET` | `/api/game/{bookTitle}/meta` | Book metadata (title, intro, section count, map) |
| `GET` | `/api/game/{bookTitle}/{sectionNumber}` | A single section (content, choices, image, combat flag) |

### User — accounts & progression
| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/api/user/register` | Create an account (unique username/email) + initial SKILL/STAMINA/LUCK |
| `POST` | `/api/user/login` | Login by username or email (PBKDF2 verify) |
| `GET` | `/api/user/me` | Fetch the current user's profile + stats + XP |
| `PUT` | `/api/user/me` | Update profile / avatar path / stats / XP |
| `POST` | `/api/user/me/avatar` | Upload an avatar image (≤ 5 MB, jpg/png/webp/gif) |
| `DELETE` | `/api/user/me/avatar` | Remove the avatar |
| `DELETE` | `/api/user/me` | Delete the current user |
| `GET/POST/DELETE` | `/api/user/me/subscription` | Manage the player's plan subscription |
| `GET/POST/DELETE` | `/api/user/me/achievements` | Unlock / list / remove achievements (medallions on conquered adventures) |
| `GET/POST/DELETE` | `/api/user/me/assets` | Collected in-game items |
| `GET/POST/DELETE` | `/api/user/me/adventures` | Per-book progress (section, Skill/Stamina/Luck); marking `IsComplete` awards XP + a medallion achievement |

### Catalog — the game database (public read, admin write)
| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/catalog/items` | All game items (auto-extracted from the adventure books) |
| `GET` | `/api/catalog/spells` | All spells a player can cast when requirements are met |
| `GET` | `/api/catalog/commands` | Chat-box commands the player uses to interact with the game |
| `GET` | `/api/catalog/adventures` | Adventure books: title, section count, medallion awarded |
| `GET` | `/api/catalog/adventures/{bookTitle}` | A single adventure's catalog entry |
| `POST/PUT/DELETE` | `/api/catalog/{items,spells,commands,adventures}[/{id}]` | Admin CRUD for the catalog tables *(admin role only)* |

## 🔐 Roles

- Every user carries a **Role** (default `"Player"`), stored on the `Users` row and embedded in the JWT as a `ClaimTypes.Role` claim.
- All write/ingestion endpoints (`/api/admin/*` and catalog `POST/PUT/DELETE`) require `[Authorize(Roles = "Admin")]`; public reads stay `[AllowAnonymous]`.
- The first admin is bootstrapped at startup from the `Admin` configuration section (`Admin:Username` / `Admin:Email` / `Admin:Password`). In Docker, set them via `ADMIN_USERNAME` / `ADMIN_EMAIL` / `ADMIN_PASSWORD`. If unset, no admin is created.

## 🗄️ Catalog Seeding

On startup, `CatalogSeeder` populates the game database:

- **Commands** — 14 default chat-box commands (`GO`, `LOOK`, `BACK`, `INVENTORY`, `TAKE`, `USE`, `DROP`, `CAST`, `FIGHT`, `FLEE`, `SAVE`, `HELP`, `RESTART`, `REREAD`) with aliases and usage examples.
- **Adventures** — one row per processed book (title, section count, `Medallion of <Title>` reward).
- **Items & Spells** — `GameContentExtractor` heuristically scans each processed book's prose for item mentions (acquisition phrases, `If you have the X...`, and proper-noun artifacts like *Crossbow of Axillon*), classifies each into a type (`weapon`, `armour`, `consumable`, `quest`, `artifact`) and rarity, and records the section where it appears. Place names and in-world scores (e.g. *Booty*) are filtered out.

> The schema is managed with **EF Core migrations** (see `DungeonWorld.Infrastructure/Persistence/Migrations`). On startup the API runs `db.Database.Migrate()` before seeding. `EnsureCreated()` is no longer used — migrations are the only way the schema is created or updated.

## 🗄️ Persistence

- EF Core over Npgsql (`DungeonWorldDbContext`), with retry-on-failure enabled for container startup races.
- Schema is created via `Database.Migrate()` at startup (initial migration: `InitialCreate`).
- Tables: `Users` (incl. SKILL/STAMINA/LUCK/XP/Role + avatar path), `GameItems`, `Spells`, `Commands`, `Adventures`, `Subscriptions`, `Achievements`, `UserAssets`, `AdventureProgresses`.
- Passwords are hashed with **PBKDF2** (100k iterations, 16-byte salt), stored as `base64(salt):base64(hash)`.
- Avatars are saved to `Storage/Avatars` and served statically at `/assets/avatars`.
- Adding a new migration: `dotnet ef migrations add <Name> --project DungeonWorld.Infrastructure --startup-project DungeonWorld.Infrastructure` (a design-time `DungeonWorldDbContextFactory` avoids needing the API host or a live DB).

## 🔍 How The Parser Works

1. **Layout analysis** — `PdfPigLayoutAnalyzer` inspects the PDF and reports single-page vs 2-up layout.
2. **Parser selection** — `DungeonWorldParserFactory` picks the first registered parser whose `CanHandle()` matches (defaults to `DoublePageParser`, the most common FF scan format).
3. **Geometric parsing** — `BaseDungeonWorldParser` locates section headers by centered X/Y coordinates (ignoring page-top navigation numbers), extracts body text, choices (`Turn to N` / `If you have...`), and embedded illustrations.
4. **Physical page tracking** — each text line records the **physical PDF page** it came from (`LineInfo.PhysicalPage`). When a section is flushed, its illustration path is derived from that physical page (`p{physicalPage}_i0.png`) rather than a sequential index — critical for double-page books, where the section's logical position and its source page diverge. This guarantees `/assets/game-art/...` links match the extracted PNGs.
5. **Patches** — hard-coded rules fix known layout quirks (e.g. a section that is printed without its number) and the *Victory Terminator* stops ingestion once the story's "You have won" signature appears, keeping publisher back-matter out of the data.
6. **Output** — each book is written as `<Title>.json` under `Storage/Books/ProcessedBooks`, with extracted images under `Storage/GameArt`, served statically at `/assets/game-art`.
7. **Optional structure pass** — `dotnet run --project DungeonWorld.DataCleaner` reads every `<Title>.json` and writes a sidecar `<Title>.cleaned.json` (raw content always preserved). It extracts rule formulas (SKILL/STAMINA/LUCK/CREW/Booty/LOG) from the intro, labels each section's `Turn to N` choices, parses individual and large-scale (STRIKE/STRENGTH) combat enemies, flags dice/Luck tests, stat & Booty/LOG changes, and builds a section graph (outgoing/incoming, dead ends, terminal ends, unreachable, orphan links).

> **Existing books:** the physical-page change was also applied in place to already-ingested books — `Seas of Blood` had 345 illustration paths remapped and 48 nulled (kept under `Storage/Books/ProcessedBooks/Seas of Blood.json.bak`). New ingestions get correct paths automatically from the parser.

## 🛠️ Local Setup (without Docker)

1. **PostgreSQL**: start one locally and point `ConnectionStrings:DefaultConnection` at it (default port `5433`).
2. **Restore & run**:
   ```bash
   dotnet restore DungeonWorldBackend.sln
   dotnet run --project DungeonWorld.API
   ```
3. Open `http://localhost:8080/swagger` to try the endpoints.

## 🐳 Docker (preferred)

```bash
# From the repo root (spins up postgres + API together)
docker compose -f backend/docker-compose.yml up -d --build
```

- API → `http://localhost:8080` · Swagger → `/swagger`
- PostgreSQL → `localhost:5433` (db `dungeonworld`, user `dw_user`)
- PDFs are stored in `./Storage` on the host and bound into the container.

> For the full stack (with the React frontend), use the root `compose.yaml` instead.

## 🧪 Testing

The suite uses **xUnit** with **FluentAssertions**:

- **Layout detection** — real PDF fixture (`Storage/Books/Seas of Blood.pdf`) exercised through the analyzer and both parsers, verifying the single-page classification, plus factory unit tests for parser priority, fallback, and exception tolerance.
- **Content extraction** — `GameContentExtractorTests` cover normalization, proper-noun artifact detection, item classification, and place-name rejection.

```bash
dotnet test DungeonWorldBackend.sln
```

## 🧠 ML_Pipeline

A Python companion (`pdf_to_images.py`) that renders PDF pages to 300-DPI images using **PyMuPDF**, producing training datasets (see `ML_Pipeline/dataset`) for CNN-based layout/OCR models (torch, torchvision, ultralytics).
