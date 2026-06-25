import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResult,
  AuthResponse,
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
  RefreshTokenRequest,
  UserProfileDTO,
  UserProfileUpdateRequest,
  UserRole,
} from '../models/auth.models';

const TOKEN_KEY = 'dokkana_token';
const REFRESH_TOKEN_KEY = 'dokkana_refresh_token';
const USER_KEY = 'dokkana_user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private base = `${environment.apiUrl}/auth`;

  // Reactive session state. Components read these signals to render auth UI.
  readonly currentUser = signal<CurrentUser | null>(this.readUser());
  readonly isLoggedIn = computed(() => this.currentUser() !== null);
  readonly role = computed<UserRole | null>(() => this.currentUser()?.role ?? null);
  readonly isAdmin = computed(() => this.role() === 'Admin');
  readonly isStoreOwner = computed(() => this.role() === 'StoreOwner');
  readonly isShopper = computed(() => this.role() === 'Shopper');

  login(payload: LoginRequest): Observable<ApiResult<AuthResponse>> {
    return this.http
      .post<ApiResult<AuthResponse>>(`${this.base}/login`, payload)
      .pipe(tap(res => this.persistSession(res)));
  }

  register(payload: RegisterRequest): Observable<ApiResult<AuthResponse>> {
    return this.http
      .post<ApiResult<AuthResponse>>(`${this.base}/register`, payload)
      .pipe(tap(res => this.persistSession(res)));
  }

  getProfile(): Observable<UserProfileDTO> {
    return this.http
      .get<ApiResult<UserProfileDTO>>(`${this.base}/me`)
      .pipe(tap(res => {
        if (res.success && res.data) this.mergeCurrentUser(res.data);
      }), map(res => res.data!));
  }

  updateProfile(payload: UserProfileUpdateRequest): Observable<ApiResult<UserProfileDTO>> {
    return this.http
      .put<ApiResult<UserProfileDTO>>(`${this.base}/me`, payload)
      .pipe(tap(res => {
        if (res.success && res.data) this.mergeCurrentUser(res.data);
      }));
  }

  changePassword(payload: ChangePasswordRequest): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/me/password`, payload);
  }

  logout(): void {
    const refresh = this.refreshPayload();
    this.clearSession();
    if (refresh) this.http.post(`${this.base}/logout`, refresh).subscribe({ error: () => {} });
  }

  getToken(): string | null {
    return sessionStorage.getItem(TOKEN_KEY);
  }

  refreshSession(): Observable<ApiResult<AuthResponse>> {
    const payload = this.refreshPayload();
    if (!payload) return throwError(() => new Error('No refresh token available.'));

    return this.http
      .post<ApiResult<AuthResponse>>(`${this.base}/refresh`, payload)
      .pipe(tap(res => this.persistSession(res)));
  }

  clearSession(): void {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_TOKEN_KEY);
    sessionStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  // Default landing route per role after login/register.
  homeRouteForRole(role: UserRole | null): string {
    switch (role) {
      case 'Admin':
        return '/admin';
      case 'StoreOwner':
        return '/merchant';
      case 'Shopper':
        return '/discover';
      default:
        return '/';
    }
  }

  private persistSession(res: ApiResult<AuthResponse>): void {
    if (!res.success || !res.data) return;
    const data = res.data;
    sessionStorage.setItem(TOKEN_KEY, data.token);
    sessionStorage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
    const user: CurrentUser = {
      userId: data.userId,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      role: data.role,
      storeId: data.storeId ?? null,
      storeName: data.storeName ?? null,
    };
    sessionStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  private mergeCurrentUser(profile: UserProfileDTO): void {
    const current = this.currentUser();
    if (!current) return;
    const next: CurrentUser = {
      ...current,
      email: profile.email,
      firstName: profile.firstName,
      lastName: profile.lastName,
    };
    sessionStorage.setItem(USER_KEY, JSON.stringify(next));
    this.currentUser.set(next);
  }

  private readUser(): CurrentUser | null {
    try {
      // Clear older persistent auth entries from previous builds. Theme and
      // language preferences still use localStorage intentionally.
      localStorage.removeItem(TOKEN_KEY);
      localStorage.removeItem(REFRESH_TOKEN_KEY);
      localStorage.removeItem(USER_KEY);

      const raw = sessionStorage.getItem(USER_KEY);
      const token = sessionStorage.getItem(TOKEN_KEY);
      const refreshToken = sessionStorage.getItem(REFRESH_TOKEN_KEY);
      if (!raw || !token || !refreshToken) return null;
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }

  private refreshPayload(): RefreshTokenRequest | null {
    try {
      const user = this.currentUser();
      const refreshToken = sessionStorage.getItem(REFRESH_TOKEN_KEY);
      if (!user || !refreshToken) return null;
      return { userId: user.userId, refreshToken };
    } catch {
      return null;
    }
  }
}
