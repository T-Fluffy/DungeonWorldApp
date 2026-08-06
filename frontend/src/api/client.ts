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

// --- User / auth API ---

export const registerUser = async (payload: RegisterRequest): Promise<UserResponse> => {
  const { data } = await api.post<UserResponse>('/user/register', payload);
  return data;
};

export const login = async (payload: LoginRequest): Promise<UserResponse> => {
  const { data } = await api.post<UserResponse>('/user/login', payload);
  return data;
};

export const getUser = async (id: string): Promise<UserResponse> => {
  const { data } = await api.get<UserResponse>(`/user/${id}`);
  return data;
};

export const updateProfile = async (
  id: string,
  payload: { displayName?: string | null; avatarPath?: string | null }
): Promise<UserResponse> => {
  const { data } = await api.put<UserResponse>(`/user/${id}`, payload);
  return data;
};

export const getSubscription = async (id: string): Promise<SubscriptionResponse | null> => {
  const { data } = await api.get<SubscriptionResponse | null>(`/user/${id}/subscription`);
  return data;
};

export const upsertSubscription = async (
  id: string,
  payload: { plan: string; expiresAt?: string | null }
): Promise<SubscriptionResponse> => {
  const { data } = await api.post<SubscriptionResponse>(`/user/${id}/subscription`, payload);
  return data;
};

export const getAchievements = async (id: string): Promise<AchievementResponse[]> => {
  const { data } = await api.get<AchievementResponse[]>(`/user/${id}/achievements`);
  return data;
};

export const unlockAchievement = async (
  id: string,
  payload: { code: string; title: string; description?: string | null }
): Promise<AchievementResponse> => {
  const { data } = await api.post<AchievementResponse>(`/user/${id}/achievements`, payload);
  return data;
};

export const getAssets = async (id: string): Promise<AssetResponse[]> => {
  const { data } = await api.get<AssetResponse[]>(`/user/${id}/assets`);
  return data;
};

export const addAsset = async (
  id: string,
  payload: { name: string; type: string; description?: string | null; bookTitle?: string | null; sectionNumber?: number | null }
): Promise<AssetResponse> => {
  const { data } = await api.post<AssetResponse>(`/user/${id}/assets`, payload);
  return data;
};

export const getAdventures = async (id: string): Promise<AdventureResponse[]> => {
  const { data } = await api.get<AdventureResponse[]>(`/user/${id}/adventures`);
  return data;
};

export const getAdventure = async (id: string, bookTitle: string): Promise<AdventureResponse | null> => {
  const { data } = await api.get<AdventureResponse | null>(
    `/user/${id}/adventures/${encodeURIComponent(bookTitle)}`
  );
  return data;
};

export const upsertAdventure = async (
  id: string,
  payload: { bookTitle: string; currentSection: number; skill?: number | null; stamina?: number | null; luck?: number | null; isComplete?: boolean }
): Promise<AdventureResponse> => {
  const { data } = await api.post<AdventureResponse>(`/user/${id}/adventures`, payload);
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
