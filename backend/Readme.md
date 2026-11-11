# ⚔️ Dungeon World Engine (Backend)

The backend of **Dungeon World** — a .NET 10 service that turns Fighting Fantasy-style gamebook PDFs into structured, playable game data and exposes it (plus player accounts, progress, and inventory) through a REST API.

![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)
![.NET 10](https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=.net&logoColor=white)
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
2. **Parser selection** — the API parser factory selects a registered parser for the PDF layout. The batch `DungeonWorld.Ingestor` also supports book-specific parsers and manifest-driven reconstruction for books whose scans need OCR/page-level guidance; these paths can apply book-specific fixes and reference overrides.
3. **Text and section extraction** — the geometric parser finds section headers and extracts text, choices, and illustrations from layout blocks. The reconstruction path combines OCR with the corresponding JSON manifests and boundary rules. Both paths produce the same processed-book model; missing or unreadable section text is retained as a detectable gap rather than silently treated as valid prose.
4. **Physical page tracking** — illustration paths use the **physical PDF page** (`p{physicalPage}_i0.png`), not a logical section index. This matters for double-page scans, where a section's logical position and source page differ; assets are served at `/assets/game-art`.
5. **Book-specific handling** — parser fixes cover known scan/layout issues, and the *Victory Terminator* prevents publisher back-matter from being ingested after the story ends.
6. **Output** — the batch ingestor writes `<BookTitle>.json` to `Storage/Books/ProcessedBooks`, cleaned game data to `Storage/Books/CleanedData/<BookTitle>.json`, and (unless `--no-images` is used) extracted images to `Storage/GameArt`.
7. **Cleaning and analysis** — `dotnet run --project backend/DungeonWorld.DataCleaner` rebuilds cleaned data from processed JSON. Pass `-- --only "FF02 Citadel of Chaos.json,FF03 Forest of Doom.json"` to select processed filenames; without it, all processed books are read. Cleaned output is in `Storage/Books/CleanedData`, not a `.cleaned.json` sidecar. The cleaner derives section choices/references and graph data, rules, combat and game-state features, and missing-text metadata. It avoids overwriting existing cleaned files by choosing a numbered filename, so inspect/remove the intended stale output before rerunning a book to avoid duplicate outputs.

## 📚 Local Book Data Status

The local `Storage/Books` data is generated/ignored content; it is not included in the repository. Current audit snapshot:

- **28 canonical books** are present in both `ProcessedBooks` and `CleanedData`. **27 have bodies for every section**; FF26 is the only book with a gap (four sections whose printed spread is not in the PDF). Across the catalogue that is **11,219 of 11,223 bodies**.
- **FF02 — Citadel of Chaos:** complete. The exit graph is the authoritative Outspaced flowchart (`ff02.svgz`, Wayback 2009-11-17): 715 edges over all 400 sections, the single dotted hidden option (`§229 → 217`) retained, and 20 terminal sections. Page checks at 300 dpi resolved every chart-vs-scan conflict in the chart's favour, so no corrections are layered on top; the scanner's wrong exit numbers are still repaired in prose for the affected sections. The assembled book has 400/400 bodies, all 400 sections reachable from §1, no dangling references, and no empty or oversized bodies. Its legacy manifest numbers lines page-globally, so `ManifestLinesArePageGlobal` translates the pins to the renderer's side-local numbering.
- **FF09 — Caverns of the Snow Witch:** complete at 400/400. Its manifest numbers lines page-globally (the older dump format), and `ManifestLinesArePageGlobal` had never been enabled: measured against a fresh 300 dpi transcript, 288/400 pins land on their own printed header under page-global numbering versus 164/400 without it. Turning it on exposed five blocks parked on the wrong pages, and re-pinning them by hand recovered every lost body — §§5–7, 29–82, 88–110, 162–169, 177–199, 220–224, 275–299, 345–350 and 383–385 — including headers that are garbled rather than missing (`p28L12 "33"` → §55, `p29R14 "bo"` → §60, `p30L19 "fz"` → §62, `p36L0 "Bo"` → §80, `p86L1 "264"` → §269). §64's body has no printed header at all; it is the avalanche-death paragraph on p30R31, and §65 was pinned to its own paragraph a few lines below. §67's merged body dropped from 5,387 to 1,949 characters, clearing the oversized-body gate. The walkthrough override table now only applies to sections that still have no body: several of its guesses contradicted the printed text (§177 prints 237, not 137; §269 prints 73, not 75; §347 prints 108, not 106) and forcing them was costing ~40 reachable sections. Reachability is now 364/400 with 33 dead ends; the 36 unreachable sections are off-path in the printed book rather than lost to the scan.
- **FF11 — Talisman of Death:** complete at 400/400. §§38 and 77 had been written off as full-page artwork, but both are printed without a header directly after the preceding section: p47's folio reads `37-38` and its last six lines are §38 ("…charred to a cinder. Turn to 43"), and p73's folio reads `76-77` with §77 at its foot ("…Turn to 43"). They had been pinned to the next page's first line, so they assembled to nothing; pinning them at p47L7 and p73L13 restores both, and each now derives its printed exit to §43 from the prose rather than the override table.
- **FF26 — Crypts of the Sorcerer:** 396/400 bodies; §§14–17 are the catalogue's only remaining gap and it is a genuine scan loss, not a parse bug. Page 17 ends at §13, page 18's left leaf is a full-page illustration and its right leaf opens at §18, and a content search finds no body anywhere carrying §16's paired exits (100/171), so the text is absent rather than mis-numbered. Their edges come from the flowchart (14/15 terminal, 16 → 100/171, 17 → 30), so the graph is complete. All four are reachable in play (§14 ← 271, §15 ← 225/371, §16 ← 391, §17 ← 229), which is why the reader detects `features.missingText` and never treats such a section as an ending — see the reader notes below.
- **FF28 — Phantoms of Fear:** complete. The exit graph is the authoritative Outspaced flowchart (`ff28.svgz`, Wayback 2009-12-15): 618 edges over all 400 sections, with the dotted hidden options retained. Page checks at 300 dpi resolved every chart-vs-scan conflict in the chart's favour; five page-verified corrections are layered on top (`§53 → 73, 364`, `§190 → 50`, `§328 → 348`, `§333 → 26, 345`, `§342 → 362`). The assembled book has 622 edges, all 400 sections reachable from §1, 28 dead ends, no dangling references, and no empty or oversized bodies. The manifest still matches OCR lines by physical page, side, and side-local line number, so out-of-order sections keep their true bounds.
- **FF28 §309 — Trial of Ghosts:** the game client derives the 165 distinct section totals reachable by five adjacent squares from the six die-start positions, excluding the judges' square, routes that revisit a square, and totals outside §§1–400. Totals without a section fail the trial.
- **FF09 §67** now measures 1,949 characters after the re-pin (it was 5,387 and swallowed pages 31–34, including §68–§74). The structure gate passes for every book.

Alternative local extraction checks for FF28 (200/400 dpi OCR, Tesseract sparse-text segmentation, the PDF's embedded text layer, and targeted 300 dpi contrast/sharpen/threshold preprocessing) did not produce any reliable additional section bodies. Section 191 was recovered from the user transcription and anchored across the p71R–p72L spread. Confirm uncertain section links against the page images before changing parser logic or golden graphs; goldens are expected-data snapshots, not proof that OCR or the source scan is correct. FF28's OCR text is demonstrably wrong at §§349 and 370 (it reads 138/221 where the printed pages read 118/211); the flowchart is correct there.

The last broad cross-book validation (after the FF02 rebuild) had **1,617 tests passing and 14 failing**. Since then **all 28 books have been brought onto their golden snapshots** and the structure gate passes for every book. Run the suite yourself with `dotnet test backend/DungeonWorldBackend.sln`; the graph snapshot and body-structure gates skip vacuously when `CleanedData` is absent, so a CI run without book data cannot regress them.

- **FF01, FF03, FF04, FF05** — regenerated against verified prose. Almost every stale entry was a `section + 1` sequential fallback that the shared turn-to repair has since replaced (the printed text carries the real target, the stale snapshot did not), and a targeted repair for the garbled prepositions `tor`/`toe`/`boy`/`Eo` recovered five genuinely lost exits (FF04 §304; FF05 §§125, 154, 158, 197).
- **FF06 Deathtrap Dungeon** — three exits were being lost because `"turn to m1"/"m8"/"m2"` (the printed 1/8/2 with an OCR speck in front) tripped the single-character salvage guard; that guard now only blocks letter-mapped characters, so the real digit survives (S39, S374, S378). The snapshot also drops a bogus `§387 → 5` that came from the old `"lumbers" → 5` misread in the same guard's absence.
- **FF16 Seas of Blood** — the golden declared `"FF16 Seas of Blood.json"` while the cleaned file was `Seas of Blood.json`, so the gate skipped it entirely and reported nothing. The book now carries its FF number in both filename and title, and the illustrations were consolidated onto the canonical `ff16_seas_of_blood` art folder (the parsed book still carried 395 legacy `/assets/game-art/seas_of_blood/` paths while the hand-verified overrides and parser tests already used the prefixed slug).
- **FF11 Talisman of Death** — §§38 and 77 had been written off as artwork but are printed headerless after §37/§76 (see the FF11 entry above); recovering them took the book to 400/400 and the snapshot to 651 edges.
- **FF07, FF08, FF10, FF14, FF17, FF18** — the remaining stale snapshots. FF17 and FF18 were clean one-section shifts (the snapshot predated the manifest realignment those books received); the rest were `s→5`/`l→1` artifacts and edges the old snapshot never captured. Across all 188 divergent sections there was not a single case where the golden's edge was text-supported and the current data's was not, so no parser change was needed for this batch.

All 28 snapshots now match, `verify_books.py` reports `ALL CLEAN`, and no book has an empty or oversized body. These goldens pin the graph; they are not proof that OCR or the scan is correct.

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
