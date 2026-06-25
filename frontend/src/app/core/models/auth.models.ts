// Auth-related types shared across the app. These mirror the backend DTOs
// in Dokkana.BLL/DTOs/Auth so the frontend speaks the same shape as the API.

export type UserRole = 'Admin' | 'StoreOwner' | 'Shopper';

// Standard backend response envelope (Dokkana.Common.GeneralResult<T>).
export interface ApiResult<T> {
  success: boolean;
  message: string | null;
  data: T | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber?: string;
  storeName?: string; // when provided the backend creates a StoreOwner + Store
}

// Matches AuthResponseDTO returned by /api/auth/login and /register.
export interface AuthResponse {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  token: string;
  tokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  storeId?: number | null;
  storeName?: string | null;
}

export interface RefreshTokenRequest {
  userId: string;
  refreshToken: string;
}

export interface UserProfileDTO {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  address?: string | null;
  gender?: string | null;
  location?: string | null;
  avatarUrl?: string | null;
  createdAt: string;
}

export interface UserProfileUpdateRequest {
  firstName: string;
  lastName: string;
  phoneNumber?: string | null;
  address?: string | null;
  gender?: string | null;
  location?: string | null;
  avatarUrl?: string | null;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

// The slice of AuthResponse we keep in memory/sessionStorage to describe the session.
export interface CurrentUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  storeId?: number | null;
  storeName?: string | null;
}
