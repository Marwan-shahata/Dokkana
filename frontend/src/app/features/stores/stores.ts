import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';
import { StoreService } from '../../core/services/store.service';
import { Store } from '../../core/models/catalog.models';

// Public store discovery page backed by GET /api/stores/approved.
// Demonstrates the real loading / empty / error states the audit asked for.
@Component({
  selector: 'app-stores',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stores.html',
  styleUrl: './stores.css',
})
export class StoresPage implements OnInit {
  private storeService = inject(StoreService);
  private router = inject(Router);
  lang = inject(LanguageService);

  readonly stores = signal<Store[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.storeService.getApproved().subscribe({
      next: stores => {
        this.stores.set(stores);
        this.loading.set(false);
      },
      error: () => {
        this.error.set(
          this.L('Could not load stores. Please try again.', 'تعذّر تحميل المحلات. حاول مرة أخرى.'),
        );
        this.loading.set(false);
      },
    });
  }

  openStore(store: Store): void {
    this.router.navigate(['/stores', store.id]);
  }

  initials(name: string): string {
    return name
      .split(' ')
      .filter(Boolean)
      .slice(0, 2)
      .map(w => w[0])
      .join('')
      .toUpperCase();
  }
}
