import { Injectable, signal } from '@angular/core';

// Lightweight app-wide toast. Used mainly by route guards to explain why a
// navigation was blocked (e.g. typing a forbidden URL or pressing back into a
// page you can no longer access).
@Injectable({ providedIn: 'root' })
export class NoticeService {
  readonly message = signal<string | null>(null);
  readonly tone = signal<'info' | 'error'>('info');
  private timer: ReturnType<typeof setTimeout> | null = null;

  show(message: string, tone: 'info' | 'error' = 'info', duration = 3500): void {
    this.message.set(message);
    this.tone.set(tone);
    if (this.timer) clearTimeout(this.timer);
    this.timer = setTimeout(() => this.message.set(null), duration);
  }

  clear(): void {
    if (this.timer) clearTimeout(this.timer);
    this.message.set(null);
  }
}
