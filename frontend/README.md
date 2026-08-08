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
| **Avatars** | Upload a profile image during registration **or** from the profile page; served via `/assets/avatars` |
| **Player Stats** | Fighting Fantasy-style SKILL / STAMINA / LUCK + Experience Points shown on the profile, persisted to the backend |
| **The Ritual** | Drag-and-drop PDF "summoning" — uploads the file, triggers backend ingestion, and binds the book to your session |
| **The Chronicle** | Terminal-style reader that loads a book, walks its sections, shows choices and combat, and accepts commands (`GO 42`, `LOOK`, `INVENTORY`, `SAVE`, `RESET`, `LOGOUT`, `HELP`). Progress auto-saves to the backend on every navigation |
| **Grimoire Switching** | Swap books mid-session via the header slider (with cover thumbnails) or the feed's "Switch Grimoire" dropdown — the current run is sealed (saved) first; saved progress shows as `S<n>` / `Complete` badges |
| **Character Sheet** | Profile page with FF stats + XP, avatar, a 16-slot Traveler's Pack, saved adventures with progress bars, and earned medallion achievements |
| **Protection** | `ProtectedRoute` guards `Files`, `Log`, and `Profile` behind login |

## 🕯️ Screens

| Route | View | Description |
| --- | --- | --- |
| `/` | **HomePage** | Thematic landing with "Begin the Ritual" call-to-action |
| `/login` · `/register` | **LoginPage / RegisterPage** | Real auth against `POST /api/user/login` & `/register` with inline errors; registration includes class selection + optional avatar upload and a Return button. Sign-in plays a cinematic **RitualLoading** screen |
| `/files` | **FileSelector** | Animated ritual circle that uploads & ingests a PDF |
| `/log` | **StoryLog** | Three-column Chronicle: **left** section art, **center** terminal feed (log, choices, command line, quick-command chips, docked navigation), **right** QuestTracker + QuickGear; slim cinematic header holds the bound-grimoire title + cover slider |
| `/profile` | **ProfilePage** | Character avatar + silhouette, FF stat grid (SKILL/STAMINA/LUCK/XP), pack, saved adventures (resume → `/log`), medallions |

## ⌨️ Chronicle Commands

The Chronicle is played by typing intent into the command line (quick chips for `Look`, `Inventory`, `Help`, `Reset`, `Reread` sit beneath it):

| Command | Effect |
| --- | --- |
| `GO <n>` (or just `<n>`) | Jump to section **n** and narrate it |
| `LOOK` | Re-read the current section's opening |
| `INVENTORY` | Inspect the contents of your pack |
| `SAVE` | Seal current progress (section + SKILL/STAMINA/LUCK) into the Grimoire on the backend |
| `RESET` / `RESTART` | Tear the chronicle asunder — rewind to section 1, clear the feed & history, and re-seal the fresh start |
| `LOGOUT` / `SEVER` | Sever the pact and leave the session (via a parting-ritual **LogoutLoading** screen) |
| `HELP` | Print this guide in the feed |

## 🧩 Key Implementation Details

- **API client** (`src/api/client.ts`) — typed `axios` wrapper mirroring the backend DTOs (`SectionDto`, `UserResponse`, `IngestResultDto`, catalog DTOs, …) with a shared `apiError()` helper; includes a response interceptor that clears stale/invalid JWTs. Exposes `upsertAdventure` / `getAdventure` so the Chronicle can persist and resume runs.
- **Session state** (`src/Context/GameContext.tsx`) — `GameProvider` restores the logged-in user from `localStorage` on boot and exposes `login` / `register` / `logout` / `setAvatar`, plus `currentBook`, inventory, and stats. Class-based starting SKILL/STAMINA/LUCK are computed on registration.
- **Game loop** (`src/hooks/useGameSession.ts`) — fetches book meta + section 1 on load, resumes a saved run when one exists, handles `GO [n]` jumps, auto-saves after every navigation (best-effort), and powers the `RESET` (rewind + re-seal) and `LOGOUT` (sever) flows.
- **Theming** — persistent fog overlay, mouse-tracked torchlight, flickering vignette, custom gothic font, and ember/crimson palette via Tailwind.
- **Routing** — `react-router-dom` with `AnimatePresence` page transitions, a **RitualLoading** screen on login, and a **LogoutLoading** parting ritual (randomized farewell phrase) on logout.
- **Resilience** — illustration `<img>`s degrade gracefully (`artFailed` / `failedCovers`) instead of showing broken images.
- **Branding** — custom Fighting Fantasy–style dragon favicon (`public/favicon.svg`).

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

The Vite dev server proxies `/api`, `/assets/game-art`, and `/assets/avatars` to the backend (default `http://localhost:8080`, overridable via `VITE_BACKEND_URL`).

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
├── components/              # FogOverlay, TorchlightEffect, Vignette, RitualCircle, RitualLoading, LogoutLoading,
│                            # Navigation (supports `docked`), ProtectedRoute, StatusHUD, QuestTracker, QuickGear
├── styles/                  # Tailwind directives and gothic font-face
├── types/game.ts            # Game-side types (Item, PlayerStats, User, ...)
├── App.tsx                  # Routing & AnimatePresence transitions
└── main.tsx                 # React entry point
```

Also: `public/favicon.svg` (custom dragon icon) replaces the default Vite logo in `index.html`.

## ⚔️ Roadmap

- [x] Real user auth wired to the backend API
- [x] PDF ingestion ritual wired to the ingestion endpoints
- [x] Avatar upload (registration + profile)
- [x] Fighting Fantasy SKILL/STAMINA/LUCK + XP on the character sheet
- [x] Persist adventure saves to the backend (`/adventures`) from the Chronicle (`SAVE`, auto-save, grimoire switching)
- [x] Chronicle command language (`GO`, `LOOK`, `INVENTORY`, `SAVE`, `RESET`, `LOGOUT`, `HELP`) + formatted help guide
- [ ] Inventory grid backed by server-side catalog items
- [ ] Cast spells from the catalog once requirements are met
- [ ] LLM-driven narrator

---

*Crafted in the shadows by Halloul Tarek*
