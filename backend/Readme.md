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
│   ├── Parsers/                   # SinglePageParser, DoublePageParser, DungeonWorldBookParser, DungeonWorldParserFactory
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

### Admin — ingestion pipeline
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
| `POST/PUT/DELETE` | `/api/catalog/{items,spells,commands,adventures}[/{id}]` | Admin CRUD for the catalog tables |

## 🗄️ Catalog Seeding

On startup, `CatalogSeeder` populates the game database:

- **Commands** — 14 default chat-box commands (`GO`, `LOOK`, `BACK`, `INVENTORY`, `TAKE`, `USE`, `DROP`, `CAST`, `FIGHT`, `FLEE`, `SAVE`, `HELP`, `RESTART`, `REREAD`) with aliases and usage examples.
- **Adventures** — one row per processed book (title, section count, `Medallion of <Title>` reward).
- **Items & Spells** — `GameContentExtractor` heuristically scans each processed book's prose for item mentions (acquisition phrases, `If you have the X...`, and proper-noun artifacts like *Crossbow of Axillon*), classifies each into a type (`weapon`, `armour`, `consumable`, `quest`, `artifact`) and rarity, and records the section where it appears. Place names and in-world scores (e.g. *Booty*) are filtered out.

> Because the schema is bootstrapped with `EnsureCreated()`, changing an entity requires a full DB wipe (see Docker notes).

## 🔍 How The Parser Works

1. **Layout analysis** — `PdfPigLayoutAnalyzer` inspects the PDF and reports single-page vs 2-up layout.
2. **Parser selection** — `DungeonWorldParserFactory` picks the first registered parser whose `CanHandle()` matches (defaults to `DoublePageParser`, the most common FF scan format).
3. **Geometric parsing** — `BaseDungeonWorldParser` locates section headers by centered X/Y coordinates (ignoring page-top navigation numbers), extracts body text, choices (`Turn to N` / `If you have...`), and embedded illustrations.
4. **Patches** — hard-coded rules fix known layout quirks (e.g. a section that is printed without its number) and the *Victory Terminator* stops ingestion once the story's "You have won" signature appears, keeping publisher back-matter out of the data.
5. **Output** — each book is written as `<Title>.json` under `Storage/Uploads/ProcessedBooks`, with extracted images under `Storage/GameArt`, served statically at `/assets/game-art`.

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

- **Layout detection** — `CanHandle` correctly classifies single vs double-page PDFs.
- **Parser regression** — Section 50 / victory-terminator patches stay functional after logic changes.
- **Data validation** — section numbering constraints enforced during ingestion.
- **Content extraction** — `GameContentExtractorTests` cover normalization, proper-noun artifact detection, item classification, and place-name rejection.

```bash
dotnet test DungeonWorldBackend.sln
```

## 🗄️ Persistence

- EF Core over Npgsql (`DungeonWorldDbContext`), with retry-on-failure enabled for container startup races.
- Schema is bootstrapped via `Database.EnsureCreated()` at startup (swap for EF migrations in production).
- Tables: `Users` (incl. SKILL/STAMINA/LUCK/XP + avatar path), `GameItems`, `Spells`, `Commands`, `Adventures`, `Subscriptions`, `Achievements`, `UserAssets`, `AdventureProgresses`.
- Passwords are hashed with **PBKDF2** (100k iterations, 16-byte salt), stored as `base64(salt):base64(hash)`.
- Avatars are saved to `Storage/Avatars` and served statically at `/assets/avatars`.

## 🧠 ML_Pipeline

A Python companion (`pdf_to_images.py`) that renders PDF pages to 300-DPI images using **PyMuPDF**, producing training datasets (see `ML_Pipeline/dataset`) for CNN-based layout/OCR models (torch, torchvision, ultralytics).
