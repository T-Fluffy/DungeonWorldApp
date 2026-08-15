# Dungeon World — Documentation

This folder explains how the whole codebase works: the architecture, the PDF
ingestion pipeline (the most complex part), the data cleaning stage, the REST
API, the PostgreSQL persistence layer, the React frontend, and the operational
tooling.

| Document | What it covers |
| --- | --- |
| [`architecture.md`](architecture.md) | Monorepo layout, clean-architecture layers, end-to-end request flow, Docker services |
| [`backend/parsing-pipeline.md`](backend/parsing-pipeline.md) | How a gamebook PDF becomes `Book` JSON: text extraction, rule-based parsing, manifest reconstruction, dedicated per-book parsers, parser selection |
| [`backend/cleaning.md`](backend/cleaning.md) | `BookCleaner`: content analysis, graph building, rules extraction, and the `CleanedBook` model |
| [`backend/api.md`](backend/api.md) | ASP.NET Core controllers, auth/JWT, DTOs, game-content extraction, seeding |
| [`backend/persistence.md`](backend/persistence.md) | EF Core `DungeonWorldDbContext`, entities, migrations |
| [`frontend/README.md`](frontend/README.md) | React app structure: context, hooks, views, components, API client, combat engine |
| [`operations.md`](operations.md) | Docker Compose, storage layout, how to run the Ingestor and DataCleaner, ML tooling, secrets |
| [`fighting-system.md`](fighting-system.md) | Fighting Fantasy rules extracted from the *Seas of Blood* book (game rules reference) |

## Quick orientation

```
frontend/   React 19 + Vite + Tailwind — accounts, Chronicle reader, combat, profile
backend/    .NET 8 solution — PDF → structured JSON → cleaned JSON → REST API → PostgreSQL
```

The single most important flow to understand is the **PDF ingestion pipeline**
(document a full request in [`architecture.md`](architecture.md), the details in
[`backend/parsing-pipeline.md`](backend/parsing-pipeline.md)):

```
PDF upload → parser selection → raw parse (Book) → BookCleaner (CleanedBook)
          → written to Storage/Books/{ProcessedBooks,CleanedData} → GameController reads it
```
