# ⚔️ Dungeon World

> *"Where steel meets shadow, and legends are forged in the ink of the abyss."*

**Dungeon World** is a full-stack web application that digitizes Fighting Fantasy-style gamebook PDFs and lets you play them in a dark-fantasy, atmospheric UI — with real accounts, saved adventures, and an inventory.

It is a single monorepo composed of two applications that talk over HTTP:

```
┌──────────────────────────────┐      /api      ┌──────────────────────────────────────────────┐
│   frontend/  (React + Vite)  │ ◄────────────► │   backend/  (.NET 8 + ASP.NET Core Web API)    │
│   The playable dark-fantasy  │                │   PDF ingestion engine + user/game REST API    │
│   client (Chronicle, Profile)│                │                                              │
└──────────────────────────────┘                │   ├─ DungeonWorld.API           (controllers) │
       │ Vite dev proxy                         │   ├─ DungeonWorld.Infrastructure (parsers,    │
       ▼                                        │   │                               EF Core)    │
  5173 (frontend)                               │   ├─ DungeonWorld.Core           (domain)     │
                                                │   └─ DungeonWorld.Tests          (xUnit)      │
                                                └──────────────┬───────────────────────────────┘
                                                               │
                                              ┌────────────────▼────────────────┐
                                              │  PostgreSQL 16 (dungeonworld)    │
                                              └─────────────────────────────────┘
```

---

## 📦 Repository Layout

| Path | What it is | Read more |
| --- | --- | --- |
| [`frontend/`](frontend/) | React 19 + Vite + Tailwind client — accounts, PDF "Ritual" ingestion, Chronicle reader, character profile | [frontend/README.md](frontend/README.md) |
| [`backend/`](backend/) | .NET 8 Clean-Architecture API — PDF → structured JSON engine, layout-aware parsers, EF Core/PostgreSQL persistence, user & game endpoints | [backend/Readme.md](backend/Readme.md) |
| `compose.yaml` | Root Docker Compose that runs all three services together (Postgres + API + frontend) | — |

## 🚀 Quick Start (Docker)

```bash
docker compose up -d --build
```

That brings up the entire stack:

| Service | URL | Notes |
| --- | --- | --- |
| **frontend** | http://localhost:5173 | The web app |
| **backend** | http://localhost:8080 | REST API + Swagger UI at `/swagger` |
| **postgres** | localhost:5433 | DB `dungeonworld` · user `dw_user` · password `dw_password` |

Then: **Register** an account → visit the **Ritual** to upload a gamebook PDF (the backend parses it automatically) → open the **Chronicle** to start playing.

## 🧱 What Each Part Does

### Backend — the engine (`.NET 8`)
- Parses gamebook PDFs into structured JSON (sections, choices, illustrations, map) using PdfPig, with automatic **single-page vs double-page layout detection** and a parser factory that picks the right engine.
- Serves the parsed books through a REST API (`/api/game`, `/api/admin`).
- Handles player accounts and progression: PBKDF2-hashed auth, profiles, subscriptions, achievements, assets, and per-book adventure saves — persisted in **PostgreSQL via EF Core**.
- Ships with an xUnit test suite covering layout detection and parser regressions.
- Includes an optional Python **ML_Pipeline** (`pdf_to_images.py`) that renders PDFs to 300-DPI image datasets for OCR/CNN training.

### Frontend — the adventure (React + Vite)
- Fully themed dark-fantasy UI (fog, torchlight, vignette, custom gothic type) with real login/register wired to the backend.
- **The Ritual**: a drag-and-drop "grimoire summoning" screen that uploads a PDF and triggers ingestion.
- **The Chronicle**: a terminal-style reader that plays the book section-by-section — choices, combat prompts, images, and text commands (`GO 42`, `LOOK`, `INVENTORY`, `SAVE`, `HELP`).
- **The Profile**: character silhouette, stat bars, a 16-slot Traveler's Pack, saved adventures with resume + progress bars, and earned "Medallion" achievements.
- Session and character data persist across reloads via `localStorage`.

## 🛠️ Developing Without Docker

Each half runs independently — full instructions in its own README:

```bash
# Backend (needs a local PostgreSQL on port 5433)
cd backend && dotnet run --project DungeonWorld.API

# Frontend (proxies /api → localhost:8080)
cd frontend && npm install && npm run dev
```

## 🧪 Testing

```bash
cd backend && dotnet test DungeonWorldBackend.sln   # xUnit parser/layout suite
cd frontend && npm run lint                          # ESLint
```

## 🗺️ Roadmap

- [x] PDF ingestion → structured, playable game data
- [x] Real user auth + profiles backed by PostgreSQL
- [x] Chronicle reader with section navigation & commands
- [x] Character sheet with pack, achievements, and saved adventures
- [ ] Persist chronicle saves to the backend from the reader
- [ ] Server-driven inventory grid
- [ ] ML-assisted layout correction via the ML_Pipeline

---

*Crafted in the shadows by Halloul Tarek — Dungeon World (2026)*
