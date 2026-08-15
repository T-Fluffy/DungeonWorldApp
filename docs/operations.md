# Operations

## Docker Compose (`compose.yaml`)

```bash
docker compose up -d --build
```

| Service | URL | Notes |
| --- | --- | --- |
| frontend | http://localhost:5173 | The web app |
| backend | http://localhost:8080 | REST API + Swagger at `/swagger` |
| postgres | localhost:5433 | DB `dungeonworld`, user `dw_user` |

Backend env vars in `compose.yaml`:

- `FileStorage__PdfUploadPath` → `/app/Storage/Books`
- `FileStorage__ImageOutputPath` → `/app/Storage/GameArt`
- `ConnectionStrings__DefaultConnection` → Postgres connection
- `Admin__Username` / `Admin__Email` / `Admin__Password` → seed the initial admin
- `Cors__Origins` → frontend origin

The backend mounts `./backend/Storage:/app/Storage`, so books/art persist on the host.

## Storage layout

`backend/Storage/` is **gitignored** (it holds copyrighted PDFs, extracted art, and
processed JSON — never commit it):

```
backend/Storage/
├── Books/
│   ├── <title>.pdf          uploaded/working PDFs
│   ├── tmp/                 source scans
│   ├── ProcessedBooks/      raw Book JSON per title (parser output)
│   └── CleanedData/         CleanedBook JSON per title (what the game reads)
├── GameArt/
│   └── <slug>/p<page>_i<n>.png   extracted illustrations (full-page scans, text + art)
├── FFArt/
│   └── <slug>/p<page>.png        art-only crops (see MediaArt tool below)
├── Avatars/
└── Uploads/
```

The cleaned book JSON is additionally mirrored to a **private data repository** (see
below).

## Running the pipeline

### Ingest one book via the API (admin)

1. `POST /api/admin/upload` (multipart `.pdf`).
2. `POST /api/admin/ingest?fileName=<title>.pdf` — parses and cleans the book.
3. Check `Storage/Books/ProcessedBooks/` and `Storage/Books/CleanedData/`.

### Batch ingest all books (Ingestor CLI)

From the repo root:

```bash
dotnet run --project backend/DungeonWorld.Ingestor \
  [--dir <folder>] [--exclude <substring>...] [--no-images] [--dpi <n>]
```

- Writes `ProcessedBooks/<Title>.json`, `CleanedData/<Title>.json`, and GameArt.
- Prints a per-book quality report (sections present, avg length, choices, combat,
  graph metrics) and a summary. `[CHECK]` lines need attention.
- **Protected titles** (e.g. *Seas of Blood*) are never overwritten.
- Manifest parsers are authoritative (no OCR merge fallback for FF02–FF05).
- Debug flags: `--ocr-extract`, `--ocr-dump`, `--ocr-test`, `--probe`,
  `--reconstruct`, `--reconstruct-apply` (see
  [`backend/parsing-pipeline.md`](backend/parsing-pipeline.md)).

### Re-clean processed books (DataCleaner CLI)

```bash
dotnet run --project backend/DungeonWorld.DataCleaner
```

Re-reads every `ProcessedBooks/*.json` and rewrites `CleanedData/<Title>.json`.
Run after ingesting a new book or to repair existing output.

### Reconstruct a scan-heavy book (manual workflow)

1. `--reconstruct <pdf> --out <dir>` → per-page line transcripts (`pages/PageNNN.txt`).
2. Review the transcripts and write an overrides manifest (`{n,page,side,line,[end]}`).
3. `--reconstruct-apply <dir> <overrides.json> --out <sections.json>` → assemble sections.
4. Wrap into a `ProcessedBooks` file (see the per-book manifest parsers) and run the
   DataCleaner.

### Extract art-only illustrations (MediaArt CLI)

```bash
dotnet run --project backend/DungeonWorld.MediaArt \
  [--dir <folder>] [--out <dir>] [--book <prefix>...] [--stats] [--page N]
```

Default scope is the six processed books (FF01–FF05, FF16); output lands in
`Storage/FFArt/<slug>/` and never touches `Storage/GameArt`.

- **Full-page scans** (FF01–FF05) are cropped to their dense-ink blocks — the
  illustrations — by `ArtRegionDetector`; text-only pages produce no file.
- **Digital PDFs** (FF16) export each embedded art image as-is.
- `--stats` prints a per-page coverage table instead of writing files; `--page N`
  limits `--stats` to one page. See
  [`backend/parsing-pipeline.md`](backend/parsing-pipeline.md) for the detection
  heuristics and thresholds.

## Tests

```bash
dotnet build backend/DungeonWorldBackend.sln -c Release
dotnet test backend/DungeonWorld.Tests/DungeonWorld.Tests.csproj -c Release --no-build
```

Covers layout detection, parser factory selection, PdfPig text extraction, art-region
detection (synthetic text-only vs illustrated pages), and MediaArt extraction on the
fixture PDF.

## ML tooling

`backend/ML_Pipeline/pdf_to_images.py` — renders PDF pages to images for building a
layout/OCR training dataset (`dataset/images/train/`). Uses PyMuPDF.

## Secrets

- Never commit `*.env` (only `.env.example` is allowed).
- `appsettings.Local.json` is gitignored — use it for local secrets; the API fails
  fast if `Jwt:Key` or the connection string is missing.

## Private cleaned-data repository

The cleaned book JSON (`backend/Storage/Books/CleanedData/`, the OCR-extracted text
of the copyrighted gamebooks) is kept out of this **public** repository. It lives in a
**separate private GitHub repository** (`DungeonWorldApp-Data`). Regenerate the files
with the Ingestor or DataCleaner, then push them to the private repo (it has no access
control from this repo; the app repo and the data repo are independent).

## Book reconstruction status

Per-book completion (400/400 means all printed sections extracted) is tracked in
[`PLANNING.md`](../PLANNING.md):

- FF01 Warlock of Firetop Mountain — 400/400 (manual reconstruction)
- FF02 Citadel of Chaos — 400/400 (manual reconstruction; tails enriched)
- FF03 Forest of Doom — 400/400 (manual reconstruction)
- FF04 Starship Traveller — 343/343 (physical ceiling)
- FF05 City of Thieves — 400/400 (manual reconstruction)
- FF16 Seas of Blood — 400/400 (reference book)
