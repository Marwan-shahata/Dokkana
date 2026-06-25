import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';
import { StoreService } from '../../core/services/store.service';
import { MonetizationService } from '../../core/services/monetization.service';
import { Store } from '../../core/models/catalog.models';
import { PromotionDTO } from '../../core/models/monetization.models';

// Public store discovery page backed by GET /api/stores/approved.
// Demonstrates the real loading / empty / error states the audit asked for.
@Component({
  selector: 'app-stores',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stores.html',
  styleUrl: './stores.css',
})
export class StoresPage implements OnInit {
  private storeService = inject(StoreService);
  private monetization = inject(MonetizationService);
  private router = inject(Router);
  lang = inject(LanguageService);

  readonly stores = signal<Store[]>([]);
  readonly sponsoredStores = signal<PromotionDTO[]>([]);
  readonly loading = signal(true);
  readonly error = signal('');

  // Client-side search over the loaded approved stores (name / address /
  // description). Backed by a signal so the template stays declarative.
  readonly search = signal('');
  readonly filtered = computed(() => {
    const q = this.search().trim().toLowerCase();
    if (!q) return this.stores();
    return this.stores().filter(s =>
      [s.name, s.address, s.description]
        .filter(Boolean)
        .some(field => field!.toLowerCase().includes(q)),
    );
  });

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  ngOnInit(): void {
    this.load();
    this.loadSponsoredStores();
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

  openSponsoredStore(campaign: PromotionDTO): void {
    this.monetization.trackPromotionClick(campaign.id).subscribe({ error: () => {} });
    this.router.navigate(['/stores', campaign.storeId]);
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

  private loadSponsoredStores(): void {
    this.monetization.sponsoredStores(4).subscribe({
      next: stores => this.sponsoredStores.set(stores),
      error: () => this.sponsoredStores.set([]),
    });
  }
}
