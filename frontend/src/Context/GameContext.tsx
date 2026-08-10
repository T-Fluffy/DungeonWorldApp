// GameContext.tsx
import { Item, PlayerStats, StatType, User, CharacterClass } from '@/types/game';
import { useState, ReactNode, useCallback } from 'react';
import { login as apiLogin, registerUser as apiRegister, setToken, AuthResponse } from '@/api/client';
import { GameContext } from './useGame';
import { LogoutLoading } from '@/components/LogoutLoading';

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

function toGameUser(resp: AuthResponse, meta: CharacterMeta): User {
  const user = resp.user;
  return {
    id: user.id,
    username: user.username,
    email: user.email,
    name: user.displayName || user.username,
    level: meta.level,
    title: meta.title,
    class: meta.class,
    isLoggedIn: true,
    avatarPath: user.avatarPath,
    skill: user.skill,
    stamina: user.stamina,
    luck: user.luck,
    experience: user.experience,
  };
}

export function GameProvider({ children }: { children: ReactNode }) {
  // 1. Initialize User State (restored from session)
  const [user, setUser] = useState<User | null>(() => loadSession());

  // The book currently being played (set from the FileSelector ritual)
  const [currentBook, setCurrentBook] = useState<string | null>(null);

  const [isLoggingOut, setIsLoggingOut] = useState(false);

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

  const updatePlayerStats = useCallback((patch: Partial<Pick<User, 'skill' | 'stamina' | 'luck'>>) => {
    setUser((prev) => {
      if (!prev) return prev;
      const next = { ...prev, ...patch };
      localStorage.setItem(SESSION_KEY, JSON.stringify(next));
      return next;
    });
  }, []);

  const login = useCallback(async (usernameOrEmail: string, password: string): Promise<User> => {
    const resp = await apiLogin({ usernameOrEmail, password });
    setToken(resp.token);
    const meta = loadCharacter(resp.user.id) ?? { level: 1, title: 'Returning Shade', class: 'Dreadknight' };
    const next = toGameUser(resp, meta);
    localStorage.setItem(SESSION_KEY, JSON.stringify(next));
    setUser(next);
    return next;
  }, []);

  const register = useCallback(async (req: { username: string; email: string; password: string; className: CharacterClass }): Promise<User> => {
    // Fighting Fantasy starting stats per class (SKILL / STAMINA / LUCK)
    const classStats: Record<CharacterClass, { skill: number; stamina: number; luck: number }> = {
      'Dreadknight': { skill: 9, stamina: 22, luck: 8 },
      'Abyssal Mage': { skill: 8, stamina: 18, luck: 12 },
      'Shadow Rogue': { skill: 11, stamina: 18, luck: 10 },
    };
    const stats = classStats[req.className];

    const resp = await apiRegister({
      username: req.username,
      email: req.email,
      password: req.password,
      skill: stats.skill,
      stamina: stats.stamina,
      luck: stats.luck,
    });
    setToken(resp.token);
    const meta: CharacterMeta = { level: 1, title: 'Initiate of the Abyss', class: req.className };
    saveCharacter(resp.user.id, meta);
    const next = toGameUser(resp, meta);
    localStorage.setItem(SESSION_KEY, JSON.stringify(next));
    setUser(next);
    return next;
  }, []);

  const logout = useCallback(() => {
    if (isLoggingOut) return;
    setIsLoggingOut(true);
    // Let the parting ritual play out before severing the pact
    setTimeout(() => {
      setToken(null);
      localStorage.removeItem(SESSION_KEY);
      setUser(null);
      setCurrentBook(null);
      setIsLoggingOut(false);
    }, 3200);
  }, [isLoggingOut]);

  const setAvatar = useCallback((avatarPath: string | null) => {
    setUser((prev) => {
      if (!prev) return prev;
      const next = { ...prev, avatarPath: avatarPath ?? null };
      localStorage.setItem(SESSION_KEY, JSON.stringify(next));
      return next;
    });
  }, []);

  return (
    <GameContext.Provider value={{
      user,
      items,
      stats,
      currentBook,
      isLoggingOut,
      addItem,
      removeItem,
      updateStat,
      updatePlayerStats,
      setCurrentBook,
      login,
      register,
      logout,
      setAvatar
    }}>
      {isLoggingOut && <LogoutLoading />}
      {children}
    </GameContext.Provider>
  );
}
