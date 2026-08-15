# Architecture

## Monorepo layout

```
DungeonWorldApp/
├── frontend/                      React 19 + Vite + Tailwind client
├── backend/                       .NET 8 solution (DungeonWorldBackend.sln)
│   ├── DungeonWorld.Core/         Domain model + contracts (zero dependencies)
│   ├── DungeonWorld.Infrastructure/  Parsers, EF Core persistence, helpers
│   ├── DungeonWorld.Cleaning/     BookCleaner + structured-output model
│   ├── DungeonWorld.API/          ASP.NET Core Web API (controllers, auth, seeding)
│   ├── DungeonWorld.Ingestor/     Batch CLI: parse PDFs → ProcessedBooks + CleanedData
│   ├── DungeonWorld.DataCleaner/  CLI: ProcessedBooks → CleanedData
│   ├── DungeonWorld.Tests/        xUnit tests
│   └── ML_Pipeline/               Python: PDF pages → image dataset
├── docs/                          This documentation
├── compose.yaml                   Postgres + backend + frontend
└── PLANNING.md                    Book-by-book reconstruction progress
```

## Clean architecture layers

```
                    ┌─────────────────────────────────────────────┐
                    │ DungeonWorld.API (composition root / UI)    │
                    └─────────────────────────────────────────────┘
                                        │ depends on
          ┌─────────────────────────────┴──────────────────────────────┐
          │ DungeonWorld.Infrastructure    │ DungeonWorld.Cleaning      │
          │ parsers, EF Core, helpers      │ BookCleaner / CleanedBook  │
          └─────────────────────────────┬──────────────────────────────┘
                                        │ depends on
                    ┌─────────────────────────────────────────────┐
                    │ DungeonWorld.Core (domain model, interfaces)│
                    └─────────────────────────────────────────────┘
```

- **DungeonWorld.Core** — no dependencies. Holds the entities (`Book`, `Section`,
  `Choice`, `User`, `GameItem`, …), the `IBookParser` contract, and option
  classes (`FileStorageOptions`, `JwtOptions`).
- **DungeonWorld.Infrastructure** — implements parsing and persistence. Depends on
  Core + third-party packages (PdfPig, Tesseract, EF Core/Npgsql).
- **DungeonWorld.Cleaning** — a standalone library that turns a raw `Book` into the
  structured `CleanedBook` the game reads. Depends only on Core entities + its own
  `Model`.
- **DungeonWorld.API** — wires everything together (DI), exposes controllers, seeds
  the catalog, serves static game art.
- **DungeonWorld.Ingestor / DataCleaner** — console entry points that run the same
  parsing/cleaning logic in batch for all books in a folder.

## Data flow

### Ingestion (admin-triggered, or batch CLI)

```
1. PDF uploaded → Storage/Books/ (AdminController.UploadPdf)
2. AdminController.IngestBook:
   a. IParserFactory.CreateParser(filePath, title)  → picks a dedicated parser
   b. parser.ParseAsync(filePath)                    → Book (raw sections)
        → writes Storage/Books/ProcessedBooks/<Title>.json
   c. BookCleaner.Clean(book, ...)                   → CleanedBook
        → writes Storage/Books/CleanedData/<Title>.json
3. GameController serves the cleaned data:
   GET /api/game/{book}/{section}        → CleanedSection
   GET /api/game/{book}/meta             → BookMetaDto
   GET /api/game/list-books              → titles
```

The **Ingestor CLI** (`backend/DungeonWorld.Ingestor`) does the same parse+clean
loop for every PDF in a folder, including a multi-dpi OCR fallback merge for books
whose embedded text layer is missing (see
[`backend/parsing-pipeline.md`](backend/parsing-pipeline.md)).

### Playing (runtime)

```
frontend useGameSession.goTo(n)
  → GET /api/game/{book}/{n}             → CleanedSection
  → renders clean text, choices, combat box (frontend /api/game/… utils)
  → auto-save via UserController adventures endpoints (best-effort)
```

## Request flow for the Chronicle

```
React (StoryLog) ──▶ useGameSession ──▶ api/client.ts (axios) ──▶ ASP.NET Core
                                                                     │
                                                     GameController ──▶ CleanedData/<Title>.json
                                                     UserController ──▶ PostgreSQL (EF Core)
                                                     CatalogController ──▶ PostgreSQL (seeded catalog)
```

## Docker services (`compose.yaml`)

| Service | Image | Port | Purpose |
| --- | --- | --- | --- |
| `postgres` | `postgres:16-alpine` | host 5433 → 5432 | Database `dungeonworld`, user `dw_user` |
| `backend` | built from `backend/` | host 8080 → 8080 | ASP.NET API; mounts `./backend/Storage:/app/Storage` |
| `frontend` | built from `frontend/` | host 5173 → 5173 | Vite dev server proxying `/api` to the backend |

Backend env vars configure storage paths and the connection string; the `Admin__*`
variables seed the initial admin (see `Program.cs`).
