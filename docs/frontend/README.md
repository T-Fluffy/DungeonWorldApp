# Frontend

React 19 + Vite + TypeScript + Tailwind in `frontend/`. The dark-fantasy client talks
to the ASP.NET API over `/api` (Vite dev proxy) and persists the session in
`localStorage`.

## Entry & routing

- `src/main.tsx` — React root.
- `src/App.tsx` — `BrowserRouter`, `GameProvider`, `AnimatedRoutes`. Route table:
  - `/` → `HomePage`
  - `/login` → `LoginPage`
  - `/register` → `RegisterPage`
  - `/log` → `StoryLog` (protected)
  - `/profile` → `ProfilePage` (protected)
  - `*` → redirect `/`
- `ProtectedRoute` guards the Chronicle and Profile behind login.

## State (context)

- `src/Context/useGame.ts` — defines the `GameContext` shape.
- `src/Context/GameContext.tsx` — the provider:
  - restores the user session from `localStorage` (`dw-session`, `dw-character-{id}`);
  - `login` / `register` (calls the API, stores token + session, per-class starting
    stats for Dreadknight / Abyssal Mage / Shadow Rogue);
  - `logout` plays a parting ritual before clearing state;
  - `updatePlayerStats` persists SKILL/STAMINA/LUCK changes;
  - items, decorative stats, avatar management.

## Game session hook

`src/hooks/useGameSession.ts` — the engine driving the Chronicle:

- `boot` loads book meta + intro, resumes a saved adventure (or starts at section 1).
- `goTo(n)` fetches the section, appends narrator/player/system log entries, and
  auto-saves progress (best-effort, never blocks navigation).
- `processCommand` parses the terminal input: `GO <n>` / bare number, `RESET`,
  `LOGOUT`, `LOOK`, `HELP`, `BATTLE`, `FLEE`, `ROLL DICE`, `SAVE`, `INVENTORY`.
- `resetRun` tears the chronicle down and rewinds to section 1.
- Combines `useCombat`.

## Combat

- `src/hooks/useCombat.ts` — manages an active fight: builds enemies (preferring the
  cleaner's `features.enemies`, falling back to prose heuristics via
  `parseEncounters`), resolves rounds, applies damage, handles death (restores
  starting stats then `onDeath` → `resetRun`).
- `src/utils/combat.ts` — pure Fighting Fantasy rules:
  - `rollDie`, `rollDice`, `parseDiceExpr`, `formatDiceResult`;
  - `parseEncounters` — heuristic extraction of encounter-box enemies from prose
    (inline, header+rows, bare-stats formats; OCR artifact normalization);
  - `resolveRound` — 2 dice + SKILL vs 2 dice + SKILL, loser −2 STAMINA, ties miss,
    special-hit tables for enemies with `specialHit: 'skill-luck'`.

## Views

| View | File | Purpose |
| --- | --- | --- |
| Home | `views/HomePage.tsx` | Landing page |
| Login / Register | `views/LoginPage.tsx`, `views/RegisterPage.tsx` | Auth screens |
| StoryLog | `views/StoryLog.tsx` | The Chronicle reader: open-book layout (left page = art/map/adventure sheet, right page = terminal feed), grimoire switcher with cover thumbnails + saved-run badges, choice panel, combat HUD, command input |
| Profile | `views/ProfilePage.tsx` | Character sheet, stats, XP, avatar, travel pack, achievements |

## Components

`src/components/`: `Navigation`, `ProtectedRoute`, `StatusHUD` (player stat HUD),
`FogOverlay`, `TorchlightEffect`, `Vignette`, `RitualLoading`, `LogoutLoading`,
`QuestTracker`, `QuickGear`.

## API client

`src/api/client.ts` — axios instance (`baseURL: '/api'`, 120 s timeout) with:

- request interceptor attaching the JWT;
- response interceptor clearing an invalid/expired token on auth endpoints;
- typed helpers for every endpoint: game (`listBooks`, `getSection`, `getBookMeta`),
  user/auth, and catalog (`getItems`, `getSpells`, `getCommands`, adventures);
- `apiError` to normalize error messages.

Types mirror the .NET DTOs (`SectionDto`, `BookMetaDto`, `UserResponse`, …).
`src/types/game.ts` holds the client-side model (`User`, `Item`, `PlayerStats`,
`LogEntry`, character classes).

## Styling

Tailwind with a dark palette (amber "ember" accents, crimson, black/white), gothic +
mono + serif font stacks, and the atmospheric overlays above. `styles/index.css`
defines the custom utilities and effects.
