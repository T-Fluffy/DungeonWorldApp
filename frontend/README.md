# 🌑 Dungeon World — Frontend

[![React](https://img.shields.io/badge/React-19-61DAFB.svg?style=for-the-badge&logo=react&logoColor=white)](https://reactjs.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-5.9-3178C6.svg?style=for-the-badge&logo=typescript&logoColor=white)](https://www.typescriptlang.org/)
[![Vite](https://img.shields.io/badge/Vite-7-646CFF.svg?style=for-the-badge&logo=vite&logoColor=white)](https://vitejs.dev/)
[![Tailwind CSS](https://img.shields.io/badge/Tailwind-3.4-38B2AC.svg?style=for-the-badge&logo=tailwindcss&logoColor=white)](https://tailwindcss.com/)
[![Framer Motion](https://img.shields.io/badge/Framer_Motion-12-ff69b4.svg?style=for-the-badge)](https://www.framer.com/motion/)

> *"The ancient tome lies open before you. The runes pulse with a forbidden energy. Will you dare to read what has been written in the shadows?"*

**Dungeon World** is an immersive dark-fantasy web client for the Dungeon World Engine. It turns Fighting Fantasy-style gamebook PDFs into a playable, atmospheric adventure — complete with real user accounts, a chronicle-style reader, and a character sheet.

---

## ✨ What It Does

| Area | Capability |
| --- | --- |
| **Accounts** | Register / login against the real backend API, session persisted in `localStorage` (`dw-session`, `dw-character-{id}`) |
| **The Ritual** | Drag-and-drop PDF "summoning" — uploads the file, triggers backend ingestion, and binds the book to your session |
| **The Chronicle** | Terminal-style reader that loads a book, walks its sections, shows choices and combat, and accepts commands (`GO 42`, `LOOK`, `INVENTORY`, `SAVE`, `HELP`) |
| **Character Sheet** | Profile page with stats (Vitality / Might / Essence / Corruption), a 16-slot Traveler's Pack, saved adventures with progress bars, and earned achievements |
| **Protection** | `ProtectedRoute` guards `Files`, `Log`, and `Profile` behind login |

## 🕯️ Screens

| Route | View | Description |
| --- | --- | --- |
| `/` | **HomePage** | Thematic landing with "Begin the Ritual" call-to-action |
| `/login` · `/register` | **LoginPage / RegisterPage** | Real auth against `POST /api/user/login` & `/register` with inline errors |
| `/files` | **FileSelector** | Animated ritual circle that uploads & ingests a PDF |
| `/log` | **StoryLog** | Chronicle reader: grimoire picker, section text, choice buttons, command line, Status HUD, Quest Tracker, Quick Gear |
| `/profile` | **ProfilePage** | Character silhouette, stat bars, pack, saved adventures (resume → `/log`), medallions |

## 🧩 Key Implementation Details

- **API client** (`src/api/client.ts`) — typed `axios` wrapper mirroring the backend DTOs (`SectionDto`, `UserResponse`, `IngestResultDto`, …) with a shared `apiError()` helper.
- **Session state** (`src/Context/GameContext.tsx`) — `GameProvider` restores the logged-in user from `localStorage` on boot and exposes `login` / `register` / `logout`, plus `currentBook`, inventory, and stats.
- **Game loop** (`src/hooks/useGameSession.ts`) — fetches book meta + section 1 on load, handles `GO [n]` jumps, and builds the narrated log.
- **Theming** — persistent fog overlay, mouse-tracked torchlight, flickering vignette, custom gothic font, and ember/crimson palette via Tailwind.
- **Routing** — `react-router-dom` with `AnimatePresence` page transitions and a `RitualLoading` screen on login.

## 🛠️ Tech Stack

- **Framework**: React 19 + TypeScript 5.9
- **Build tool**: Vite 7
- **Styling**: Tailwind CSS 3.4
- **Animations**: Framer Motion 12
- **Icons**: Lucide React
- **Data / Routing**: Axios, React Router 7

## 📜 Local Setup

```bash
npm install
npm run dev        # http://localhost:5173
```

The Vite dev server proxies `/api` and `/assets/game-art` to the backend (default `http://localhost:8080`, overridable via `VITE_BACKEND_URL`).

Useful scripts:

```bash
npm run build      # tsc -b && vite build
npm run lint       # eslint .
npm run preview    # serve the production build
```

## 🐳 Docker

```bash
# From the repo root (full stack: postgres + API + frontend)
docker compose up -d --build
```

The frontend runs on port `5173` with `usePolling` file watching so edits hot-reload inside the container.

## 📂 Project Structure

```text
src/
├── api/client.ts            # Typed axios client for every backend endpoint
├── Context/GameContext.tsx  # Global session / inventory / stats state
├── hooks/useGameSession.ts  # Chronicle game loop (meta, sections, commands)
├── views/                   # HomePage, LoginPage, RegisterPage, FileSelector, StoryLog, ProfilePage
├── components/              # FogOverlay, TorchlightEffect, Vignette, RitualCircle, RitualLoading,
│                            # Navigation, ProtectedRoute, StatusHUD, QuestTracker, QuickGear
├── styles/                  # Tailwind directives and gothic font-face
├── types/game.ts            # Game-side types (Item, PlayerStats, User, ...)
├── App.tsx                  # Routing & AnimatePresence transitions
└── main.tsx                 # React entry point
```

## ⚔️ Roadmap

- [x] Real user auth wired to the backend API
- [x] PDF ingestion ritual wired to the ingestion endpoints
- [ ] Persist adventure saves to the backend (`/adventures`) from the Chronicle
- [ ] Inventory grid backed by server-side assets
- [ ] LLM-driven narrator

---

*Crafted in the shadows by Halloul Tarek*
