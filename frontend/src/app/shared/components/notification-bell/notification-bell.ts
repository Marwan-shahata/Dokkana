import { CommonModule } from '@angular/common';
import { Component, ElementRef, HostListener, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationService } from '../../../core/services/notification.service';
import { LanguageService } from '../../../core/services/language.service';
import { NotificationDTO } from '../../../core/models/notification.models';

// Bell icon with an unread badge and a dropdown of recent notifications.
// Rendered in the navbar for any signed-in user.
@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-bell.html',
  styleUrl: './notification-bell.css',
})
export class NotificationBell {
  notifications = inject(NotificationService);
  private lang = inject(LanguageService);
  private router = inject(Router);
  private host = inject(ElementRef<HTMLElement>);

  open = false;

  L(en: string, ar: string): string { return this.lang.t(en, ar); }

  toggle(): void {
    this.open = !this.open;
    if (this.open) this.notifications.refresh();
  }

  onItemClick(n: NotificationDTO): void {
    if (!n.isRead) this.notifications.markAsRead(n.id).subscribe({ error: () => {} });
    this.open = false;
    if (n.link) this.router.navigateByUrl(n.link).catch(() => {});
  }

  markAll(): void {
    this.notifications.markAllAsRead().subscribe({ error: () => {} });
  }

  // Localized "2m ago" style relative time. The API serializes CreatedAt as UTC
  // but without a trailing 'Z' (SQL Server datetimes carry no kind), so we add it
  // to stop the browser from misreading the value as local time.
  timeAgo(iso: string): string {
    const utc = /[zZ]|[+-]\d{2}:?\d{2}$/.test(iso) ? iso : `${iso}Z`;
    const diff = Date.now() - new Date(utc).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return this.L('just now', 'الآن');
    if (mins < 60) return this.L(`${mins}m ago`, `منذ ${mins} د`);
    const hrs = Math.floor(mins / 60);
    if (hrs < 24) return this.L(`${hrs}h ago`, `منذ ${hrs} س`);
    const days = Math.floor(hrs / 24);
    return this.L(`${days}d ago`, `منذ ${days} يوم`);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    if (this.open && !this.host.nativeElement.contains(event.target as Node)) {
      this.open = false;
    }
  }
}
