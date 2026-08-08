import axios from 'axios';

// --- DTOs mirroring the .NET API ---
export interface ChoiceDto {
  description: string;
  targetSectionNumber: number;
  isDiceRoll: boolean;
}

export interface SectionDto {
  sectionNumber: number;
  content: string;
  imagePath: string | null;
  choices: ChoiceDto[];
  hasCombat: boolean;
}

export interface BookMetaDto {
  title: string;
  introduction: string;
  mapPath: string | null;
  adventureSheetPath: string | null;
  sectionCount: number;
  hasCombatSections: number;
}

export interface IngestResultDto {
  message: string;
  parserUsed: string;
  bookTitle: string;
  processedFile: string;
  sections: number;
  mapFound: boolean;
}

const api = axios.create({
  baseURL: '/api',
  timeout: 120000, // PDF parsing can take a while
});

// Attach the JWT to every request
api.interceptors.request.use((config) => {
  const token = getToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

export const listBooks = async (): Promise<string[]> => {
  const { data } = await api.get<string[]>('/game/list-books');
  return data;
};

export const getSection = async (bookTitle: string, sectionNumber: number): Promise<SectionDto> => {
  const { data } = await api.get<SectionDto>(
    `/game/${encodeURIComponent(bookTitle)}/${sectionNumber}`
  );
  return data;
};

export const getBookMeta = async (bookTitle: string): Promise<BookMetaDto> => {
  const { data } = await api.get<BookMetaDto>(`/game/${encodeURIComponent(bookTitle)}/meta`);
  return data;
};

export const uploadPdf = async (file: File): Promise<{ fileName: string }> => {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await api.post<{ fileName: string }>('/admin/upload', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
  return data;
};

export const ingestBook = async (fileName: string): Promise<IngestResultDto> => {
  const { data } = await api.post<IngestResultDto>('/admin/ingest', null, {
    params: { fileName },
  });
  return data;
};

export const analyzeLayout = async (fileName: string) => {
  const { data } = await api.post('/admin/analyze-layout', null, {
    params: { fileName },
  });
  return data;
};

// --- User / auth DTOs mirroring DungeonWorld.API UserController ---

export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
  displayName?: string | null;
}

export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
}

export interface SubscriptionResponse {
  id: string;
  plan: string;
  status: string;
  startedAt: string;
  expiresAt: string | null;
  renewsAt: string | null;
}

export interface AchievementResponse {
  id: string;
  code: string;
  title: string;
  description: string | null;
  unlockedAt: string;
}

export interface AssetResponse {
  id: string;
  name: string;
  type: string;
  description: string | null;
  bookTitle: string | null;
  sectionNumber: number | null;
  acquiredAt: string;
}

export interface AdventureResponse {
  id: string;
  bookTitle: string;
  currentSection: number;
  skill: number | null;
  stamina: number | null;
  luck: number | null;
  updatedAt: string;
  isComplete: boolean;
}

export interface UserResponse {
  id: string;
  username: string;
  email: string;
  displayName: string | null;
  avatarPath: string | null;
  createdAt: string;
  lastLoginAt: string | null;
  subscription: SubscriptionResponse | null;
  achievements: AchievementResponse[];
  assets: AssetResponse[];
  adventures: AdventureResponse[];
}

export interface AuthResponse {
  token: string;
  user: UserResponse;
}

const TOKEN_KEY = 'dw-token';

export const getToken = (): string | null => localStorage.getItem(TOKEN_KEY);
export const setToken = (token: string | null) => {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
};

// --- User / auth API ---

export const registerUser = async (payload: RegisterRequest): Promise<AuthResponse> => {
  const { data } = await api.post<AuthResponse>('/user/register', payload);
  return data;
};

export const login = async (payload: LoginRequest): Promise<AuthResponse> => {
  const { data } = await api.post<AuthResponse>('/user/login', payload);
  return data;
};

export const getUser = async (): Promise<UserResponse> => {
  const { data } = await api.get<UserResponse>('/user/me');
  return data;
};

export const updateProfile = async (
  payload: { displayName?: string | null; avatarPath?: string | null }
): Promise<UserResponse> => {
  const { data } = await api.put<UserResponse>('/user/me', payload);
  return data;
};

export const getSubscription = async (): Promise<SubscriptionResponse | null> => {
  const { data } = await api.get<SubscriptionResponse | null>('/user/me/subscription');
  return data;
};

export const upsertSubscription = async (
  payload: { plan: string; expiresAt?: string | null }
): Promise<SubscriptionResponse> => {
  const { data } = await api.post<SubscriptionResponse>('/user/me/subscription', payload);
  return data;
};

export const getAchievements = async (): Promise<AchievementResponse[]> => {
  const { data } = await api.get<AchievementResponse[]>('/user/me/achievements');
  return data;
};

export const unlockAchievement = async (
  payload: { code: string; title: string; description?: string | null }
): Promise<AchievementResponse> => {
  const { data } = await api.post<AchievementResponse>('/user/me/achievements', payload);
  return data;
};

export const getAssets = async (): Promise<AssetResponse[]> => {
  const { data } = await api.get<AssetResponse[]>('/user/me/assets');
  return data;
};

export const addAsset = async (
  payload: { name: string; type: string; description?: string | null; bookTitle?: string | null; sectionNumber?: number | null }
): Promise<AssetResponse> => {
  const { data } = await api.post<AssetResponse>('/user/me/assets', payload);
  return data;
};

export const getAdventures = async (): Promise<AdventureResponse[]> => {
  const { data } = await api.get<AdventureResponse[]>('/user/me/adventures');
  return data;
};

export const getAdventure = async (bookTitle: string): Promise<AdventureResponse | null> => {
  const { data } = await api.get<AdventureResponse | null>(
    `/user/me/adventures/${encodeURIComponent(bookTitle)}`
  );
  return data;
};

export const upsertAdventure = async (
  payload: { bookTitle: string; currentSection: number; skill?: number | null; stamina?: number | null; luck?: number | null; isComplete?: boolean }
): Promise<AdventureResponse> => {
  const { data } = await api.post<AdventureResponse>('/user/me/adventures', payload);
  return data;
};

export const apiError = (err: unknown): string => {
  if (axios.isAxiosError(err)) {
    const data = err.response?.data as { error?: string } | undefined;
    if (data?.error) return data.error;
    return err.message;
  }
  return 'Something went wrong.';
};
