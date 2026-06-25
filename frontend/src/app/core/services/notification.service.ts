import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, tap } from 'rxjs';
import * as signalR from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { ApiResult } from '../models/auth.models';
import { BroadcastNotificationRequest, NotificationDTO } from '../models/notification.models';
import { AuthService } from './auth.service';
import { NoticeService } from './notice.service';

// Notifications (/api/notifications) + a live SignalR connection to
// /hubs/notifications. Components read the signals; the service keeps them in
// sync from both the REST API (history) and the websocket (live pushes).
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private notice = inject(NoticeService);
  private base = `${environment.apiUrl}/notifications`;

  // Live state for the bell UI.
  readonly items = signal<NotificationDTO[]>([]);
  readonly unreadCount = computed(() => this.items().filter(n => !n.isRead).length);

  private hub: signalR.HubConnection | null = null;

  // Load history + open the realtime connection. Call once after login.
  start(): void {
    this.refresh();
    this.connect();
  }

  // Drop the connection and clear state on logout.
  stop(): void {
    this.hub?.stop().catch(() => {});
    this.hub = null;
    this.items.set([]);
  }

  refresh(): void {
    this.http
      .get<ApiResult<NotificationDTO[]>>(this.base)
      .pipe(map(res => res.data ?? []))
      .subscribe({
        next: list => this.items.set(list),
        error: () => {},
      });
  }

  markAsRead(id: number): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/${id}/read`, {}).pipe(
      tap(() =>
        this.items.update(list =>
          list.map(n => (n.id === id ? { ...n, isRead: true } : n)),
        ),
      ),
    );
  }

  markAllAsRead(): Observable<ApiResult<unknown>> {
    return this.http.put<ApiResult<unknown>>(`${this.base}/read-all`, {}).pipe(
      tap(() => this.items.update(list => list.map(n => ({ ...n, isRead: true })))),
    );
  }

  broadcast(payload: BroadcastNotificationRequest): Observable<ApiResult<number>> {
    return this.http.post<ApiResult<number>>(`${this.base}/broadcast`, payload);
  }

  // --- SignalR ---

  private connect(): void {
    if (this.hub) return;

    // /hubs/notifications is mounted at the API root (not under /api).
    const hubUrl = environment.apiUrl.replace(/\/api\/?$/, '') + '/hubs/notifications';

    this.hub = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        // The API reads the JWT from ?access_token= for websocket handshakes.
        accessTokenFactory: () => this.auth.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.hub.on('notificationReceived', (n: NotificationDTO) => {
      // Prepend new pushes; de-dupe by id in case history already has it.
      this.items.update(list => [n, ...list.filter(x => x.id !== n.id)]);
      if (n.isSponsored) {
        this.notice.show(`Sponsored: ${n.title}`, 'info', 6500);
      }
    });

    this.hub.start().catch(() => {
      // Realtime is best-effort; the REST history still works without it.
      this.hub = null;
    });
  }
}
