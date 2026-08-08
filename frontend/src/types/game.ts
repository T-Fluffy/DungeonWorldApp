// --- DEFINITIONS ---
export type ItemType = 'weapon' | 'consumable' | 'quest' | 'artifact';
export type ItemRarity = 'common' | 'rare' | 'legendary';
export type StatType = 'vitality' | 'might' | 'essence' | 'corruption';
export type CharacterClass = 'Dreadknight' | 'Abyssal Mage' | 'Shadow Rogue';
export interface Item {
  id: string;
  name: string;
  description: string;
  type: ItemType;
  rarity: ItemRarity;
}
export interface User {
  id: string;
  username: string;
  email: string;
  name: string;
  level: number;
  title: string;
  class: CharacterClass;
  isLoggedIn: boolean;
  avatarPath?: string | null;
  skill?: number;
  stamina?: number;
  luck?: number;
  experience?: number;
}

export interface PlayerStats {
  vitality: number;
  might: number;
  essence: number;
  corruption: number;
}

export interface GameState {
  user: User | null;
  items: Item[];
  stats: PlayerStats;
  currentBook: string | null;
  isLoggingOut: boolean;
  addItem: (item: Item) => void;
  removeItem: (id: string) => void;
  updateStat: (stat: StatType, value: number) => void;
  setCurrentBook: (book: string | null) => void;
  login: (usernameOrEmail: string, password: string) => Promise<User>;
  register: (req: { username: string; email: string; password: string; className: CharacterClass }) => Promise<User>;
  logout: () => void;
  setAvatar: (avatarPath: string | null) => void;
}

export interface LogEntry {
  id: string;
  type: 'narrator' | 'player' | 'system';
  content: string;
  timestamp: string;
}