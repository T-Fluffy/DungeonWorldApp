// GameContext.tsx
import { GameState, Item, PlayerStats, StatType, User, CharacterClass } from '@/types/game';
import { createContext, useContext, useState, ReactNode, useCallback } from 'react';
import { login as apiLogin, registerUser as apiRegister, UserResponse } from '@/api/client';

const SESSION_KEY = 'dw-session';
const CHARACTER_KEY = 'dw-character';

interface CharacterMeta {
  level: number;
  title: string;
  class: CharacterClass;
}

function loadSession(): User | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as User;
    return parsed?.isLoggedIn ? parsed : null;
  } catch {
    return null;
  }
}

function loadCharacter(userId: string): CharacterMeta | null {
  try {
    const raw = localStorage.getItem(`${CHARACTER_KEY}-${userId}`);
    return raw ? (JSON.parse(raw) as CharacterMeta) : null;
  } catch {
    return null;
  }
}

function saveCharacter(userId: string, meta: CharacterMeta) {
  localStorage.setItem(`${CHARACTER_KEY}-${userId}`, JSON.stringify(meta));
}

function toGameUser(resp: UserResponse, meta: CharacterMeta): User {
  return {
    id: resp.id,
    username: resp.username,
    email: resp.email,
    name: resp.displayName || resp.username,
    level: meta.level,
    title: meta.title,
    class: meta.class,
    isLoggedIn: true,
  };
}

const GameContext = createContext<GameState | undefined>(undefined);

export function GameProvider({ children }: { children: ReactNode }) {
  // 1. Initialize User State (restored from session)
  const [user, setUser] = useState<User | null>(() => loadSession());

  // The book currently being played (set from the FileSelector ritual)
  const [currentBook, setCurrentBook] = useState<string | null>(null);

  const [items, setItems] = useState<Item[]>([
    {
      id: 'initial-map',
      name: 'Tattered Map',
      description: 'A map of the catacombs, stained with ink and blood.',
      type: 'quest',
      rarity: 'common'
    }
  ]);

  const [stats, setStats] = useState<PlayerStats>({
    vitality: 85,
    might: 64,
    essence: 92,
    corruption: 10
  });

  const addItem = (item: Item) => setItems((prev) => [...prev, item]);
  const removeItem = (id: string) => setItems((prev) => prev.filter(i => i.id !== id));

  const updateStat = (stat: StatType, value: number) => {
    setStats(prev => ({
      ...prev,
      [stat]: Math.min(100, Math.max(0, value))
    }));
  };

  const login = useCallback(async (usernameOrEmail: string, password: string): Promise<User> => {
    const resp = await apiLogin({ usernameOrEmail, password });
    const meta = loadCharacter(resp.id) ?? { level: 1, title: 'Returning Shade', class: 'Dreadknight' };
    const next = toGameUser(resp, meta);
    localStorage.setItem(SESSION_KEY, JSON.stringify(next));
    setUser(next);
    return next;
  }, []);

  const register = useCallback(async (req: { username: string; email: string; password: string; className: CharacterClass }): Promise<User> => {
    const resp = await apiRegister({ username: req.username, email: req.email, password: req.password });
    const meta: CharacterMeta = { level: 1, title: 'Initiate of the Abyss', class: req.className };
    saveCharacter(resp.id, meta);
    const next = toGameUser(resp, meta);
    localStorage.setItem(SESSION_KEY, JSON.stringify(next));
    setUser(next);
    return next;
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(SESSION_KEY);
    setUser(null);
    setCurrentBook(null);
  }, []);

  return (
    <GameContext.Provider value={{
      user,
      items,
      stats,
      currentBook,
      addItem,
      removeItem,
      updateStat,
      setCurrentBook,
      login,
      register,
      logout
    }}>
      {children}
    </GameContext.Provider>
  );
}

export function useGame() {
  const context = useContext(GameContext);
  if (!context) throw new Error('useGame must be used within a GameProvider');
  return context;
}
