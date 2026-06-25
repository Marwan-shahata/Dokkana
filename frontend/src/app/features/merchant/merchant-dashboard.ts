import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { Category, ProductListItem, Store, StoreTier } from '../../core/models/catalog.models';
import { AuthService } from '../../core/services/auth.service';
import { CategoryService } from '../../core/services/category.service';
import { LanguageService } from '../../core/services/language.service';
import { ProductService } from '../../core/services/product.service';
import { StoreService } from '../../core/services/store.service';
import { UploadService } from '../../core/services/upload.service';
import { DashboardService } from '../../core/services/dashboard.service';
import { SellerStatsDTO } from '../../core/models/dashboard.models';
import { OrderService } from '../../core/services/order.service';
import { StoreOrderDTO } from '../../core/models/order.models';
import { AiAssistantService } from '../../core/services/ai-assistant.service';
import { BusinessCopilotResponse, CatalogQuestionResponse } from '../../core/models/ai-assistant.models';
import { StoreMapComponent, MapCoords } from '../../shared/components/store-map/store-map';
import { MonetizationService } from '../../core/services/monetization.service';
import {
  BillingSummaryDTO,
  MerchantInsightsDTO,
  MerchantLeadDTO,
  PromotionDTO,
  PromotionAudience,
} from '../../core/models/monetization.models';

type MerchantTab = 'inventory' | 'orders' | 'store' | 'billing' | 'promotions' | 'catalog' | 'copilot';
type StockState = 'in' | 'low' | 'out';
type StockFilter = 'all' | StockState;

interface StoreForm {
  name: string;
  description: string;
  logoUrl: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
  whatsAppPhone: string;
}

interface ProductForm {
  title: string;
  description: string;
  price: number | null;
  count: number | null;
  imageUrl: string;
  color: string;
  size: string;
  brand: string;
  gender: string;
  categoryId: number | null;
}

const emptyStoreForm = (): StoreForm => ({
  name: '',
  description: '',
  logoUrl: '',
  address: '',
  latitude: null,
  longitude: null,
  whatsAppPhone: '',
});

const emptyProductForm = (): ProductForm => ({
  title: '',
  description: '',
  price: null,
  count: 1,
  imageUrl: '',
  color: '',
  size: '',
  brand: '',
  gender: '',
  categoryId: null,
});

@Component({
  selector: 'app-merchant-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, StoreMapComponent],
  templateUrl: './merchant-dashboard.html',
  styleUrl: './merchant-dashboard.css',
})
export class MerchantDashboard implements OnInit {
  private auth = inject(AuthService);
  private stores = inject(StoreService);
  private products = inject(ProductService);
  private categoriesApi = inject(CategoryService);
  private uploads = inject(UploadService);
  private dashboard = inject(DashboardService);
  private ordersApi = inject(OrderService);
  private aiAssistant = inject(AiAssistantService);
  private monetization = inject(MonetizationService);
  private title = inject(Title);
  readonly lang = inject(LanguageService);

  readonly tabs: { id: MerchantTab; en: string; ar: string; icon: string }[] = [
    { id: 'inventory', en: 'Inventory', ar: 'المخزون', icon: 'box' },
    { id: 'orders', en: 'Orders', ar: 'الطلبات', icon: 'bag' },
    { id: 'store', en: 'Store profile', ar: 'ملف المحل', icon: 'store' },
    { id: 'billing', en: 'Plan & billing', ar: 'الباقة والفوترة', icon: 'card' },
    { id: 'promotions', en: 'Promotions', ar: 'العروض الممولة', icon: 'megaphone' },
    { id: 'catalog', en: 'Catalog upload', ar: 'رفع الكتالوج', icon: 'file' },
    { id: 'copilot', en: 'Business Copilot', ar: 'مساعد الأعمال', icon: 'spark' },
  ];
  readonly stockFilterOptions: { id: StockFilter; en: string; ar: string }[] = [
    { id: 'all', en: 'All', ar: 'الكل' },
    { id: 'in', en: 'In stock', ar: 'متوفر' },
    { id: 'low', en: 'Low', ar: 'منخفض' },
    { id: 'out', en: 'Out', ar: 'نفد' },
  ];

  readonly activeTab = signal<MerchantTab>('inventory');
  readonly inventorySearch = signal('');
  readonly stockFilter = signal<StockFilter>('all');
  readonly loading = signal(true);
  readonly savingStore = signal(false);
  readonly savingProduct = signal(false);
  readonly uploadingProductImage = signal(false);
  readonly uploadingStoreLogo = signal(false);
  readonly deletingProductId = signal<number | null>(null);
  readonly store = signal<Store | null>(null);
  readonly productsList = signal<ProductListItem[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly sellerStats = signal<SellerStatsDTO | null>(null);
  readonly orders = signal<StoreOrderDTO[]>([]);
  readonly ordersLoading = signal(false);
  readonly updatingOrderId = signal<number | null>(null);
  readonly error = signal('');
  readonly notice = signal('');
  readonly editingProductId = signal<number | null>(null);
  readonly upgradingPlan = signal(false);
  readonly campaigns = signal<PromotionDTO[]>([]);
  readonly billing = signal<BillingSummaryDTO | null>(null);
  readonly leads = signal<MerchantLeadDTO[]>([]);
  readonly insights = signal<MerchantInsightsDTO | null>(null);
  readonly insightsLoading = signal(false);
  readonly campaignsLoading = signal(false);
  readonly walletLoading = signal(false);
  readonly promotionSaving = signal(false);
  readonly notificationSaving = signal(false);
  readonly saleSaving = signal(false);
  readonly aiSuggesting = signal(false);

  promotionProductId: number | null = null;
  promotionBudget = 150;
  promotionDays = 7;
  notificationTitle = '';
  notificationMessage = '';
  notificationAudience: PromotionAudience = 'Followers';
  notificationBudget = 120;
  notificationDays = 3;
  topUpAmount = 500;
  saleProductId: number | null = null;
  salePrice: number | null = null;
  saleDays = 7;

  // Business Copilot (sales/stock Q&A)
  copilotQuestion = '';
  readonly copilotLoading = signal(false);
  readonly copilotResult = signal<BusinessCopilotResponse | null>(null);
  readonly copilotError = signal('');
  readonly copilotSuggestions: { en: string; ar: string }[] = [
    { en: 'What sold most this month?', ar: 'إيه أكتر منتج اتباع الشهر ده؟' },
    { en: 'Give me a sales summary', ar: 'اعملي ملخص للمبيعات' },
    { en: 'Which products are low on stock?', ar: 'إيه المنتجات اللي قربت تخلص؟' },
    { en: "Which products didn't sell?", ar: 'إيه المنتجات اللي مش بتتباع؟' },
  ];

  // Catalog RAG (upload PDF + ask)
  catalogQuestion = '';
  readonly catalogUploading = signal(false);
  readonly catalogAsking = signal(false);
  readonly catalogUpload = signal<{ fileName: string; chunks: number; ready: boolean } | null>(null);
  readonly catalogResult = signal<CatalogQuestionResponse | null>(null);
  readonly catalogError = signal('');

  storeForm: StoreForm = emptyStoreForm();
  productForm: ProductForm = emptyProductForm();

  readonly currentUser = computed(() => this.auth.currentUser());
  readonly isPro = computed(() => this.store()?.tier === 'Pro');
  readonly productCount = computed(() => this.productsList().length);
  readonly inventoryValue = computed(() =>
    this.productsList().reduce((sum, item) => sum + item.price * item.count, 0),
  );
  readonly lowStockCount = computed(() =>
    this.productsList().filter(item => item.count > 0 && item.count <= 5).length,
  );
  readonly outOfStockCount = computed(() =>
    this.productsList().filter(item => item.count === 0).length,
  );
  readonly activeStockCount = computed(() =>
    this.productsList().filter(item => item.count > 5).length,
  );
  readonly reservedOrdersCount = computed(() =>
    this.orders().filter(order => order.status === 'Reserved').length,
  );
  readonly completedOrdersCount = computed(() =>
    this.orders().filter(order => order.status === 'Completed').length,
  );
  readonly cancelledOrdersCount = computed(() =>
    this.orders().filter(order => order.status === 'Cancelled').length,
  );
  readonly averageOrderValue = computed(() => {
    const stats = this.sellerStats();
    if (!stats || stats.ordersCount === 0) return 0;
    return stats.revenue / stats.ordersCount;
  });
  readonly walletBalance = computed(() => this.billing()?.balance ?? this.store()?.walletBalance ?? 0);
  readonly campaignImpressions = computed(() =>
    this.campaigns().reduce((sum, campaign) => sum + campaign.impressions, 0),
  );
  readonly campaignClicks = computed(() =>
    this.campaigns().reduce((sum, campaign) => sum + campaign.clicks, 0),
  );
  readonly campaignSpend = computed(() =>
    this.campaigns()
      .filter(c => ['Approved', 'Active'].includes(String(c.status)))
      .reduce((sum, campaign) => sum + campaign.budget, 0),
  );
  readonly leadCount = computed(() => this.leads().length);
  readonly proInsights = computed(() => {
    const total = this.productCount();
    const low = this.lowStockCount();
    const daysToLow = Math.max(3, Math.round((this.activeStockCount() + 1) / Math.max(this.completedOrdersCount(), 1) * 7));
    const avgPrice = total ? Math.round(this.productsList().reduce((sum, p) => sum + p.price, 0) / total) : 0;
    return [
      {
        en: low ? `${low} items may sell out first` : 'Inventory has healthy coverage',
        ar: low ? `${low} منتجات ممكن تخلص الأول` : 'المخزون حالته مستقرة',
      },
      {
        en: `At current pace, key stock lasts about ${daysToLow} days`,
        ar: `بالوتيرة الحالية، المخزون الأساسي يكفي حوالي ${daysToLow} يوم`,
      },
      {
        en: avgPrice ? `Nearby pricing hint: keep hero items around ${Math.max(avgPrice - 60, 50)}-${avgPrice + 80} EGP` : 'Add products to unlock price hints',
        ar: avgPrice ? `تلميح سعر: خلي القطع المهمة حوالي ${Math.max(avgPrice - 60, 50)}-${avgPrice + 80} ج.م` : 'أضف منتجات لتفعيل تلميحات السعر',
      },
    ];
  });
  readonly inventoryHealth = computed(() => {
    const total = this.productCount();
    if (total === 0) return 0;
    return Math.round((this.activeStockCount() / total) * 100);
  });
  readonly filteredProducts = computed(() => {
    const term = this.inventorySearch().trim().toLowerCase();
    const filter = this.stockFilter();

    return this.productsList().filter(product => {
      const matchesFilter = filter === 'all' || this.stockState(product) === filter;
      const searchable = [
        product.title,
        product.brand,
        product.categoryName,
        product.color,
        product.size,
        product.gender,
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();

      return matchesFilter && (!term || searchable.includes(term));
    });
  });

  ngOnInit(): void {
    this.title.setTitle(this.L('Dokkana | Merchant Portal', 'دكانة | بوابة التاجر'));
    this.loadCategories();
    this.loadStore();
    this.loadSellerStats();
    this.loadOrders();
  }

  // Server-computed sales KPIs (orders, units sold, revenue) for this store.
  private loadSellerStats(): void {
    this.dashboard.getSellerStats().subscribe({
      next: stats => this.sellerStats.set(stats),
      error: () => this.sellerStats.set(null),
    });
  }

  // Reservations that include this store's products.
  private loadOrders(): void {
    this.ordersLoading.set(true);
    this.ordersApi.getStoreOrders().subscribe({
      next: orders => {
        this.orders.set(orders);
        this.ordersLoading.set(false);
      },
      error: () => {
        this.orders.set([]);
        this.ordersLoading.set(false);
      },
    });
  }

  // Store owner marks a reservation as collected (Completed) or Cancelled.
  // The backend restocks cancelled lines and verifies the order is this store's.
  updateOrderStatus(order: StoreOrderDTO, status: 'Completed' | 'Cancelled'): void {
    if (this.updatingOrderId() !== null) return;
    this.updatingOrderId.set(order.id);
    this.ordersApi.updateStoreOrderStatus(order.id, status).subscribe({
      next: () => {
        this.updatingOrderId.set(null);
        this.orders.update(list => list.map(o => (o.id === order.id ? { ...o, status } : o)));
        this.showNotice(
          status === 'Completed'
            ? this.L('Order marked as collected.', 'تم تعليم الطلب كمستلم.')
            : this.L('Order cancelled.', 'تم إلغاء الطلب.'));
        this.loadSellerStats();
      },
      error: err => {
        this.updatingOrderId.set(null);
        this.showNotice(this.apiMessage(err) || this.L('Could not update the order.', 'تعذر تحديث الطلب.'));
      },
    });
  }

  orderStatusLabel(status: string): string {
    const map: Record<string, [string, string]> = {
      Reserved: ['Reserved', 'محجوز'],
      Completed: ['Collected', 'مُستلم'],
      Cancelled: ['Cancelled', 'ملغي'],
    };
    const pair = map[status];
    return pair ? this.L(pair[0], pair[1]) : status;
  }

  orderStatusClass(status: string): string {
    return 'order-' + status.toLowerCase();
  }

  trackOrder(_index: number, order: StoreOrderDTO): number {
    return order.id;
  }

  // ── Business Copilot ─────────────────────────────────────────────────────
  askCopilotSuggestion(s: { en: string; ar: string }): void {
    this.copilotQuestion = this.L(s.en, s.ar);
    this.askCopilot();
  }

  askCopilot(): void {
    const store = this.store();
    const question = this.copilotQuestion.trim();
    if (!store) {
      this.copilotError.set(this.L('Create your store profile first.', 'أنشئ ملف المحل أولاً.'));
      return;
    }
    if (!question || this.copilotLoading()) return;

    this.copilotLoading.set(true);
    this.copilotError.set('');
    this.aiAssistant.askCopilot(store.id, question).subscribe({
      next: result => {
        this.copilotResult.set(result);
        this.copilotLoading.set(false);
      },
      error: err => {
        this.copilotLoading.set(false);
        this.copilotError.set(this.apiMessage(err) || this.L('The assistant is unavailable right now.', 'المساعد غير متاح حالياً.'));
      },
    });
  }

  // Turns the copilot's `data` payload (array of objects) into a simple table.
  copilotRows(): Record<string, unknown>[] {
    const data = this.copilotResult()?.data;
    return Array.isArray(data) ? (data as Record<string, unknown>[]) : [];
  }

  copilotColumns(): string[] {
    const rows = this.copilotRows();
    return rows.length ? Object.keys(rows[0]) : [];
  }

  // ── Catalog RAG ──────────────────────────────────────────────────────────
  uploadCatalog(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    const store = this.store();
    if (!file) return;
    if (!store) {
      this.catalogError.set(this.L('Create your store profile first.', 'أنشئ ملف المحل أولاً.'));
      input.value = '';
      return;
    }

    this.catalogUploading.set(true);
    this.catalogError.set('');
    this.aiAssistant.uploadCatalog(store.id, file).subscribe({
      next: res => {
        this.catalogUploading.set(false);
        this.catalogUpload.set({ fileName: res.fileName, chunks: res.chunksCount, ready: res.status === 'Ready' });
        this.showNotice(res.message || this.L('Catalog uploaded.', 'تم رفع الكتالوج.'));
        input.value = '';
      },
      error: err => {
        this.catalogUploading.set(false);
        this.catalogError.set(this.apiMessage(err) || this.L('Could not upload the catalog.', 'تعذر رفع الكتالوج.'));
        input.value = '';
      },
    });
  }

  askCatalog(): void {
    const store = this.store();
    const question = this.catalogQuestion.trim();
    if (!store || !question || this.catalogAsking()) return;

    this.catalogAsking.set(true);
    this.catalogError.set('');
    this.aiAssistant.askCatalog(store.id, question).subscribe({
      next: result => {
        this.catalogResult.set(result);
        this.catalogAsking.set(false);
      },
      error: err => {
        this.catalogAsking.set(false);
        this.catalogError.set(this.apiMessage(err) || this.L('Could not answer from the catalog.', 'تعذر الإجابة من الكتالوج.'));
      },
    });
  }

  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  tabLabel(tab: { en: string; ar: string }): string {
    return this.L(tab.en, tab.ar);
  }

  selectTab(tab: MerchantTab): void {
    this.activeTab.set(tab);
  }

  setStockFilter(filter: StockFilter): void {
    this.stockFilter.set(filter);
  }

  clearInventoryFilters(): void {
    this.inventorySearch.set('');
    this.stockFilter.set('all');
  }

  // Fills the lat/lng fields from the map's "use my location" pick so owners
  // never have to type raw coordinates.
  onStoreLocationPicked(coords: MapCoords): void {
    this.storeForm.latitude = coords.lat;
    this.storeForm.longitude = coords.lng;
  }

  createOrUpdateStore(): void {
    if (!this.storeForm.name.trim()) {
      this.showNotice(this.L('Store name is required.', 'اسم المحل مطلوب.'));
      return;
    }

    this.savingStore.set(true);
    const current = this.store();
    const payload = {
      name: this.storeForm.name.trim(),
      description: this.nullable(this.storeForm.description),
      logoUrl: this.nullable(this.storeForm.logoUrl),
      address: this.nullable(this.storeForm.address),
      latitude: this.storeForm.latitude,
      longitude: this.storeForm.longitude,
      whatsAppPhone: this.nullable(this.storeForm.whatsAppPhone),
    };

    if (current) {
      this.stores.update(current.id, payload).subscribe({
        next: () => this.afterStoreSaved(this.L('Store profile updated.', 'تم تحديث ملف المحل.')),
        error: (err: unknown) => this.afterStoreSaveFailed(err),
      });
      return;
    }

    this.stores.create({
      name: payload.name,
      description: payload.description,
      logoUrl: payload.logoUrl,
      address: payload.address,
      latitude: payload.latitude,
      longitude: payload.longitude,
      whatsAppPhone: payload.whatsAppPhone,
    }).subscribe({
      next: () => this.afterStoreSaved(this.L('Store profile created.', 'تم إنشاء ملف المحل.')),
      error: (err: unknown) => this.afterStoreSaveFailed(err),
    });
  }

  // Mocked billing: switches the store's plan instantly. Pro unlocks promoted
  // placement, sponsored notifications, and analytics (built in later phases).
  setPlan(tier: StoreTier): void {
    const store = this.store();
    if (!store || this.upgradingPlan() || store.tier === tier) return;

    this.upgradingPlan.set(true);
    this.stores.setTier(store.id, tier).subscribe({
      next: () => {
        this.upgradingPlan.set(false);
        this.store.update(s => (s ? { ...s, tier } : s));
        this.showNotice(
          tier === 'Pro'
            ? this.L('You are now on Dokkana Pro.', 'أنت الآن على باقة دكانة برو.')
            : this.L('Switched back to the Free plan.', 'تم الرجوع إلى الباقة المجانية.'));
      },
      error: err => {
        this.upgradingPlan.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not change your plan.', 'تعذر تغيير الباقة.'));
      },
    });
  }

  topUpWallet(): void {
    const amount = Number(this.topUpAmount);
    if (!amount || amount <= 0 || this.walletLoading()) return;

    this.walletLoading.set(true);
    this.monetization.topUp(amount).subscribe({
      next: summary => {
        this.walletLoading.set(false);
        this.billing.set(summary);
        this.showNotice(this.L('Wallet topped up.', 'تم شحن الرصيد.'));
      },
      error: err => {
        this.walletLoading.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not top up wallet.', 'تعذر شحن الرصيد.'));
      },
    });
  }

  submitFeaturedPromotion(): void {
    if (this.promotionSaving()) return;
    this.promotionSaving.set(true);
    this.monetization.createFeatured({
      productId: this.promotionProductId,
      budget: Number(this.promotionBudget) || 0,
      durationDays: Number(this.promotionDays) || 7,
    }).subscribe({
      next: campaign => {
        this.promotionSaving.set(false);
        this.campaigns.update(list => [campaign, ...list]);
        this.showNotice(this.L('Promotion submitted for approval.', 'تم إرسال العرض للمراجعة.'));
      },
      error: err => {
        this.promotionSaving.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not create promotion.', 'تعذر إنشاء العرض.'));
      },
    });
  }

  promoteProduct(product: ProductListItem): void {
    this.promotionProductId = product.id;
    this.activeTab.set('promotions');
  }

  submitSponsoredNotification(): void {
    if (this.notificationSaving()) return;
    if (!this.notificationTitle.trim() || !this.notificationMessage.trim()) {
      this.showNotice(this.L('Title and message are required.', 'العنوان والرسالة مطلوبان.'));
      return;
    }

    this.notificationSaving.set(true);
    this.monetization.createNotification({
      title: this.notificationTitle.trim(),
      message: this.notificationMessage.trim(),
      audience: this.notificationAudience,
      budget: Number(this.notificationBudget) || 0,
      durationDays: Number(this.notificationDays) || 3,
    }).subscribe({
      next: campaign => {
        this.notificationSaving.set(false);
        this.notificationTitle = '';
        this.notificationMessage = '';
        this.campaigns.update(list => [campaign, ...list]);
        this.showNotice(this.L('Sponsored notification submitted.', 'تم إرسال الإشعار الممول.'));
      },
      error: err => {
        this.notificationSaving.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not create sponsored notification.', 'تعذر إنشاء الإشعار الممول.'));
      },
    });
  }

  scheduleSale(): void {
    const productId = this.saleProductId;
    const price = Number(this.salePrice);
    if (!productId || !price || price <= 0 || this.saleSaving()) return;

    this.saleSaving.set(true);
    const startsAt = new Date().toISOString();
    const endsAt = new Date(Date.now() + Math.max(Number(this.saleDays) || 7, 1) * 86400000).toISOString();
    this.monetization.scheduleSale(productId, { salePrice: price, startsAt, endsAt }).subscribe({
      next: () => {
        this.saleSaving.set(false);
        this.showNotice(this.L('Sale scheduled.', 'تم جدولة التخفيض.'));
        const store = this.store();
        if (store) this.loadProducts(store.id);
      },
      error: err => {
        this.saleSaving.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not schedule sale.', 'تعذر جدولة التخفيض.'));
      },
    });
  }

  generateWithAi(): void {
    if (this.aiSuggesting()) return;
    this.aiSuggesting.set(true);
    this.monetization.aiSuggest(this.productForm.imageUrl, this.productForm.imageUrl.split('/').pop()).subscribe({
      next: suggestion => {
        this.aiSuggesting.set(false);
        this.productForm = {
          ...this.productForm,
          title: this.productForm.title || suggestion.title,
          description: this.productForm.description || suggestion.description,
          color: this.productForm.color || suggestion.color,
          size: this.productForm.size || suggestion.size,
          brand: this.productForm.brand || suggestion.brand,
          gender: this.productForm.gender || suggestion.gender,
          categoryId: this.productForm.categoryId || this.categoryIdFor(suggestion.categoryName),
        };
        this.showNotice(this.L('AI filled editable product fields.', 'الذكاء ملأ حقول المنتج القابلة للتعديل.'));
      },
      error: err => {
        this.aiSuggesting.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not generate product fields.', 'تعذر توليد بيانات المنتج.'));
      },
    });
  }

  campaignTypeLabel(type: string): string {
    const labels: Record<string, [string, string]> = {
      FeaturedProduct: ['Featured product', 'منتج ممول'],
      FeaturedStore: ['Featured store', 'محل ممول'],
      SponsoredNotification: ['Sponsored notification', 'إشعار ممول'],
    };
    const pair = labels[type];
    return pair ? this.L(pair[0], pair[1]) : type;
  }

  campaignStatusLabel(status: string): string {
    const labels: Record<string, [string, string]> = {
      Pending: ['Pending approval', 'بانتظار الموافقة'],
      Approved: ['Approved', 'مقبول'],
      Rejected: ['Rejected', 'مرفوض'],
      Active: ['Active', 'نشط'],
      Ended: ['Ended', 'منتهي'],
    };
    const pair = labels[status];
    return pair ? this.L(pair[0], pair[1]) : status;
  }

  campaignStatusClass(status: string): string {
    return 'campaign-' + status.toLowerCase();
  }

  submitProduct(): void {
    const store = this.store();
    if (!store) {
      this.showNotice(this.L('Create your store profile before adding products.', 'أنشئ ملف المحل قبل إضافة المنتجات.'));
      this.activeTab.set('store');
      return;
    }
    if (!this.productForm.title.trim() || !this.productForm.categoryId || !this.productForm.price) {
      this.showNotice(this.L('Product title, category, and price are required.', 'اسم المنتج والتصنيف والسعر مطلوبة.'));
      return;
    }

    this.savingProduct.set(true);
    const id = this.editingProductId();
    const payload = this.productPayload(store.id);
    if (id) {
      this.products.update(id, payload).subscribe({
        next: () => this.afterProductSaved(store.id, this.L('Product updated.', 'تم تحديث المنتج.')),
        error: (err: unknown) => this.afterProductSaveFailed(err),
      });
      return;
    }

    this.products.create({
      ...payload,
      title: payload.title!,
      price: payload.price!,
      count: payload.count ?? 0,
      categoryId: payload.categoryId!,
      storeId: store.id,
    }).subscribe({
      next: () => this.afterProductSaved(store.id, this.L('Product added.', 'تمت إضافة المنتج.')),
      error: (err: unknown) => this.afterProductSaveFailed(err),
    });
  }

  editProduct(product: ProductListItem): void {
    this.editingProductId.set(product.id);
    this.activeTab.set('inventory');

    // Seed the form from the list row for instant feedback...
    this.productForm = {
      title: product.title,
      description: '',
      price: product.price,
      count: product.count,
      imageUrl: product.imageUrl ?? '',
      color: product.color ?? '',
      size: product.size ?? '',
      brand: product.brand ?? '',
      gender: product.gender ?? '',
      categoryId: this.categoryIdFor(product.categoryName),
    };

    // ...then fetch full details. The list item carries no description, so
    // editing without this would save an empty description and wipe it.
    this.products.getById(product.id).subscribe({
      next: details => {
        if (this.editingProductId() !== product.id) return; // user moved on
        this.productForm = {
          title: details.title,
          description: details.description ?? '',
          price: details.price,
          count: details.count,
          imageUrl: details.imageUrl ?? '',
          color: details.color ?? '',
          size: details.size ?? '',
          brand: details.brand ?? '',
          gender: details.gender ?? '',
          categoryId: details.categoryId,
        };
      },
      error: () => {},
    });
  }

  deleteProduct(product: ProductListItem): void {
    this.deletingProductId.set(product.id);
    this.products.delete(product.id).subscribe({
      next: () => {
        this.deletingProductId.set(null);
        this.showNotice(this.L('Product deleted.', 'تم حذف المنتج.'));
        const store = this.store();
        if (store) this.loadProducts(store.id);
      },
      error: err => {
        this.deletingProductId.set(null);
        this.showNotice(this.apiMessage(err) || this.L('Could not delete product.', 'تعذر حذف المنتج.'));
      },
    });
  }

  resetProductForm(): void {
    this.productForm = emptyProductForm();
    this.editingProductId.set(null);
  }

  uploadProductImage(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingProductImage.set(true);
    this.uploads.uploadImage(file, 'products').subscribe({
      next: url => {
        this.productForm.imageUrl = url;
        this.uploadingProductImage.set(false);
        this.showNotice(this.L('Product image uploaded.', 'تم رفع صورة المنتج.'));
        input.value = '';
      },
      error: err => {
        this.uploadingProductImage.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not upload product image.', 'تعذر رفع صورة المنتج.'));
        input.value = '';
      },
    });
  }

  uploadStoreLogo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.uploadingStoreLogo.set(true);
    this.uploads.uploadImage(file, 'stores').subscribe({
      next: url => {
        this.storeForm.logoUrl = url;
        this.uploadingStoreLogo.set(false);
        this.showNotice(this.L('Store logo uploaded.', 'تم رفع شعار المحل.'));
        input.value = '';
      },
      error: err => {
        this.uploadingStoreLogo.set(false);
        this.showNotice(this.apiMessage(err) || this.L('Could not upload store logo.', 'تعذر رفع شعار المحل.'));
        input.value = '';
      },
    });
  }

  stockState(product: ProductListItem): StockState {
    if (product.count === 0) return 'out';
    if (product.count <= 5) return 'low';
    return 'in';
  }

  stockLabel(product: ProductListItem): string {
    const state = this.stockState(product);
    if (state === 'out') return this.L('Out of stock', 'نفد المخزون');
    if (state === 'low') return this.L('Low stock', 'مخزون منخفض');
    return this.L('In stock', 'متوفر');
  }

  categoryLabel(name: string): string {
    const labels: Record<string, string> = {
      Dresses: 'فساتين',
      Tops: 'بلوزات',
      Pants: 'بناطيل',
      Shoes: 'أحذية',
      Bags: 'شنط',
      Accessories: 'إكسسوارات',
      Outerwear: 'ملابس خارجية',
      Activewear: 'ملابس رياضية',
    };
    return this.lang.isAr() ? labels[name] ?? name : name;
  }

  genderLabel(value: string | null | undefined): string {
    if (!value) return this.L('Not specified', 'غير محدد');
    const labels: Record<string, string> = {
      Women: 'نساء',
      Men: 'رجال',
      Unisex: 'للجميع',
    };
    return this.lang.isAr() ? labels[value] ?? value : value;
  }

  trackProduct(_index: number, product: ProductListItem): number {
    return product.id;
  }

  // Build a safe CSS background-image value. Quoting (and escaping stray quotes)
  // keeps URLs with spaces, parentheses, or query strings from breaking the
  // unquoted url() token. Returns null when empty so the style is removed.
  cssUrl(url: string | null | undefined): string | null {
    const u = (url ?? '').trim();
    return u ? `url("${u.replace(/"/g, '%22')}")` : null;
  }

  initials(value: string | null | undefined): string {
    const cleaned = (value ?? '').trim();
    if (!cleaned) return 'DK';
    return cleaned
      .split(/\s+/)
      .slice(0, 2)
      .map(part => part[0])
      .join('')
      .toUpperCase();
  }

  private loadStore(showLoader = true): void {
    if (showLoader) this.loading.set(true);
    this.error.set('');

    this.stores.getMy().subscribe({
      next: store => {
        this.store.set(store);
        this.storeForm = this.toStoreForm(store);
        this.loadMonetization();
        this.loadProducts(store.id, showLoader);
      },
      error: err => {
        this.loading.set(false);
        if (err?.status === 404) {
          this.store.set(null);
          this.storeForm = emptyStoreForm();
          this.productsList.set([]);
          this.error.set('');
          return;
        }
        this.error.set(this.apiMessage(err) || this.L('Could not load merchant data.', 'تعذر تحميل بيانات التاجر.'));
      },
    });
  }

  private afterStoreSaved(message: string): void {
    this.savingStore.set(false);
    this.showNotice(message);
    this.loadStore(false);
  }

  private afterStoreSaveFailed(err: unknown): void {
    this.savingStore.set(false);
    this.showNotice(this.apiMessage(err) || this.L('Could not save store profile.', 'تعذر حفظ ملف المحل.'));
  }

  private afterProductSaved(storeId: number, message: string): void {
    this.savingProduct.set(false);
    this.showNotice(message);
    this.resetProductForm();
    this.loadProducts(storeId);
    this.loadSellerStats();
  }

  private afterProductSaveFailed(err: unknown): void {
    this.savingProduct.set(false);
    this.showNotice(this.apiMessage(err) || this.L('Could not save product.', 'تعذر حفظ المنتج.'));
  }

  private loadProducts(storeId: number, updateLoader = false): void {
    if (updateLoader) this.loading.set(true);
    this.products.getByStore(storeId, 1, 100).subscribe({
      next: page => {
        this.productsList.set(page.items);
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(this.apiMessage(err) || this.L('Could not load products.', 'تعذر تحميل المنتجات.'));
      },
    });
  }

  private loadCategories(): void {
    this.categoriesApi.getAll().subscribe({
      next: categories => this.categories.set(categories),
      error: () => this.categories.set([]),
    });
  }

  private loadMonetization(): void {
    this.campaignsLoading.set(true);
    this.monetization.getMyCampaigns().subscribe({
      next: campaigns => {
        this.campaigns.set(campaigns);
        this.campaignsLoading.set(false);
      },
      error: () => {
        this.campaigns.set([]);
        this.campaignsLoading.set(false);
      },
    });

    this.monetization.getBilling().subscribe({
      next: summary => this.billing.set(summary),
      error: () => this.billing.set(null),
    });

    this.monetization.getMyLeads().subscribe({
      next: leads => this.leads.set(leads),
      error: () => this.leads.set([]),
    });

    this.insightsLoading.set(true);
    this.monetization.getInsights().subscribe({
      next: insights => {
        this.insights.set(insights);
        this.insightsLoading.set(false);
      },
      error: () => {
        this.insights.set(null);
        this.insightsLoading.set(false);
      },
    });
  }

  private toStoreForm(store: Store): StoreForm {
    return {
      name: store.name,
      description: store.description ?? '',
      logoUrl: store.logoUrl ?? '',
      address: store.address ?? '',
      latitude: store.latitude ?? null,
      longitude: store.longitude ?? null,
      whatsAppPhone: store.whatsAppPhone ?? '',
    };
  }

  private productPayload(storeId: number) {
    return {
      title: this.productForm.title.trim(),
      description: this.nullable(this.productForm.description),
      price: this.productForm.price,
      count: this.productForm.count ?? 0,
      imageUrl: this.nullable(this.productForm.imageUrl),
      color: this.nullable(this.productForm.color),
      size: this.nullable(this.productForm.size),
      brand: this.nullable(this.productForm.brand),
      gender: this.nullable(this.productForm.gender),
      categoryId: this.productForm.categoryId,
      storeId,
    };
  }

  private categoryIdFor(name: string): number | null {
    return this.categories().find(category => category.name === name)?.id ?? null;
  }

  private nullable(value: string): string | null {
    const trimmed = value.trim();
    return trimmed.length ? trimmed : null;
  }

  private showNotice(message: string): void {
    this.notice.set(message);
    setTimeout(() => {
      if (this.notice() === message) this.notice.set('');
    }, 3200);
  }

  private apiMessage(err: unknown): string {
    const maybe = err as {
      error?: { message?: string; title?: string; errors?: Record<string, string[]> };
      message?: string;
    };
    const body = maybe?.error;
    // ASP.NET validation problems arrive as { errors: { Field: [msg] } } — surface
    // the first concrete message instead of a generic "could not save" toast.
    const firstValidation = body?.errors
      ? Object.values(body.errors).flat().find(Boolean)
      : undefined;
    return firstValidation ?? body?.message ?? body?.title ?? maybe?.message ?? '';
  }
}
