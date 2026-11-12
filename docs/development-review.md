# Development review

_State as of 2026-11-12._

## 1. What the project is

Dungeon World turns Fighting Fantasy gamebook PDFs into playable, structured data and serves
them in a dark-fantasy reader. Two applications share one repository:

| Layer | Stack | Size |
| --- | --- | --- |
| `frontend/` | React 19 + Vite + Tailwind | 25 files, ~3,000 lines |
| `backend/` | .NET 10, 8 projects (Clean Architecture) | 121 files, ~15,900 lines |
| data | 28 parsed books, cleaned JSON + art | ~1.2 GB in git-ignored `backend/Storage` |

The pipeline is the heart of the system:

```
PDF → layout detection → parser (rule-based or manifest reconstruction)
    → ProcessedBooks/<Title>.json → BookCleaner → CleanedData/<Title>.json
    → REST API (game/catalog/user/admin) → React reader
```

## 2. Where it stands

| Area | State |
| --- | --- |
| Books ingested | **28** canonical books; `FF04` 343, `FF13` 380, `FF17` 440, `FF24` 460 are the printed section counts |
| Section bodies | **11,219 / 11,223**; only FF26 §§14–17 are missing (that printed spread is absent from the PDF) |
| Graph gates | **28 / 28** golden snapshots match; no dangling refs, choices == references everywhere |
| Structure gate | green: no empty or oversized section bodies |
| Test surface | 36 test files, 222 `[Fact]`/`[Theory]`, ~1,462 `[InlineData]` cases |
| API surface | 43 routes across `Game`, `Catalog`, `User`, `Admin` |
| Runtime | Docker Compose (Postgres 16, API, Vite) builds and runs on .NET 10 |

### Strengths

- **The data work is unusually rigorous.** Every book is pinned to a golden graph snapshot, bodies
  are length-bounded, and `tools/verify_books.py` produces a per-book reachability/dangling report.
  Regressions in the parser are caught by data, not by hope.
- **Clean layering.** `Core` holds entities/contracts with no dependencies; `Infrastructure`
  implements parsing + persistence; `Cleaning` is a standalone library; `API` composes. The two
  CLI entry points (`Ingestor`, `DataCleaner`) reuse the same logic as the API.
- **Reproducible reconstruction.** Per-book manifests are embedded resources, so a book can be
  rebuilt from its PDF with `--reconstruct` + `--reconstruct-apply` and the output is byte-stable.
- **Docs that reflect reality.** `docs/backend/parsing-pipeline.md` now records the two traps that
  cost the most time (page-global vs side-local numbering, headerless sections).

### Weaknesses / risks

| Risk | Detail |
| --- | --- |
| **Content lives in files, not the database** | `GameController` reads `CleanedData/<title>.json` from disk on each request. The database only holds accounts and a small catalog. No per-section querying, full-text search, or transactional content updates. |
| **Command surface is half-implemented** | The `Commands` table seeds 15 commands (`TAKE`, `USE`, `DROP`, `BACK`, `REREAD`, `CAST`, …) but the client only implements `GO`, `LOOK`, `HELP`, `BATTLE/FIGHT`, `FLEE`, `ROLL`, `SAVE`, `INVENTORY`, `RESET`, `LOGOUT`. The catalog and the engine disagree. |
| **No per-adventure command set** | Commands are global. A book without combat still advertises `FIGHT`; nothing records which commands an adventure actually supports. |
| **Art coverage** | 2,193 of 3,305 referenced illustrations are missing; ten books have no art folder. Source PDFs are present, so this is an extraction run. |
| **Tests can't run in every environment** | The VSTest host opens its parent process handle, which fails under restricted runners. Verify in CI or the container. |
| **Dev-only secrets** | `Jwt:Key` is a committed placeholder; admin seeding needs `ADMIN_*` env vars or `/api/admin/*` is unusable. |
| **FF26 §§14–17** | Only recoverable from an external transcription; the graph is complete and the reader handles the gap explicitly. |

## 3. What was delivered in the recent cycle

1. **.NET 10 migration** — all 8 projects retargeted, EF Core/Npgsql/Swashbuckle/test packages
   bumped, Dockerfile moved to `sdk:10.0` / `aspnet:10.0`, and the container no longer depends on
   the (git-ignored) solution file. Fixed a silent EF failure where `Relational` resolved to
   10.0.4 while the rest was 10.0.11 — the migration types failed to load, so no schema was
   created at all.
2. **Catalogue brought to golden** — 14 stale or broken snapshots reconciled with evidence from the
   printed text; FF06's `"turn to m1/m8/m2"` speck repair; FF16 renamed into the `FFnn` convention
   and its art consolidated on one slug.
3. **FF09 rebuilt** — page-global manifest numbering enabled (288/400 pins hit their header vs
   164/400), 43 pins re-pinned, 38 bodies recovered, §67's four-page merge eliminated.
4. **FF11 completed** — §§38/77 turned out to be headerless bodies after §37/§76, not artwork.
5. **Reader honesty** — missing bodies are reported explicitly and never counted as endings.

## 4. Recommended next steps

Ordered by user-visible impact:

1. **Persist book content in PostgreSQL** (schema + importer + DB-backed endpoints) so content can be
   queried, versioned and updated without redeploying files.
2. **Unify the command surface**: seed per-adventure commands and implement the missing verbs in the
   client so the catalog stops advertising behaviour the engine lacks.
3. **Illustration extraction** for the ten books with no art, then a coverage gate in the test suite.
4. **Content quality pass** on OCR text (spelling/typography), guarded by the golden graph gates so
   exits cannot move.
5. **CI**: run `dotnet test`, `npm run lint`, `verify_books.py` and the Docker build on every push.
