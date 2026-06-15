import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  ApiResult,
  AuthResponse,
  CurrentUser,
  LoginRequest,
  RegisterRequest,
  UserProfileDTO,
  UserProfileUpdateRequest,
  UserRole,
} from '../models/auth.models';

const TOKEN_KEY = 'dokkana_token';
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

  logout(): void {
    // Backend logout is stateless; we just call it best-effort and clear locally.
    this.http.post(`${this.base}/logout`, {}).subscribe({ error: () => {} });
    this.clearSession();
  }

  getToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  // Default landing route per role after login/register.
  homeRouteForRole(role: UserRole | null): string {
    switch (role) {
      case 'Admin':
        return '/admin';
      case 'StoreOwner':
        return '/merchant';
      default:
        return '/';
    }
  }

  private persistSession(res: ApiResult<AuthResponse>): void {
    if (!res.success || !res.data) return;
    const data = res.data;
    localStorage.setItem(TOKEN_KEY, data.token);
    const user: CurrentUser = {
      userId: data.userId,
      email: data.email,
      firstName: data.firstName,
      lastName: data.lastName,
      role: data.role,
      storeId: data.storeId ?? null,
      storeName: data.storeName ?? null,
    };
    localStorage.setItem(USER_KEY, JSON.stringify(user));
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
    localStorage.setItem(USER_KEY, JSON.stringify(next));
    this.currentUser.set(next);
  }

  private clearSession(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
  }

  private readUser(): CurrentUser | null {
    try {
      const raw = localStorage.getItem(USER_KEY);
      const token = localStorage.getItem(TOKEN_KEY);
      if (!raw || !token) return null;
      return JSON.parse(raw) as CurrentUser;
    } catch {
      return null;
    }
  }
}
