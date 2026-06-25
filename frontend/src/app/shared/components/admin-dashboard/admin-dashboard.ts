import { Component, DestroyRef, OnInit, inject, signal, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Title } from '@angular/platform-browser';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { LanguageService } from '../../../core/services/language.service';
import { StoreService } from '../../../core/services/store.service';
import { CategoryService } from '../../../core/services/category.service';
import { ProductService } from '../../../core/services/product.service';
import { OrderService } from '../../../core/services/order.service';
import { UserService } from '../../../core/services/user.service';
import { EngagementService } from '../../../core/services/engagement.service';
import { AuditLogDTO } from '../../../core/models/engagement.models';
import { MonetizationService } from '../../../core/services/monetization.service';
import { PromotionDTO, PromotionStatus } from '../../../core/models/monetization.models';

// DTOs - matching backend exactly

/** Source: CategoryDTO.cs */
export interface CategoryDto {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
}

/** Source: CategoryCreateDTO.cs - used for POST + PUT */
export interface CategoryCreateDto {
  name: string;
  description?: string;
  imageUrl?: string;
}

/**
 * Combined view for admin list:
 * StoreDTO.cs (list) + StoreDetailsDTO.cs (detail)
 * Admin endpoint GET /api/stores returns StoreDTO[].
 * productCount & ownerName come from GET /api/stores/{id} (StoreDetailsDTO).
 */
export interface StoreDto {
  id: number;
  name: string;
  description?: string;
  logoUrl?: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  isApproved: boolean; // maps to IsApproved
  tier?: 'Free' | 'Pro'; // StoreDTO.Tier — commercial plan
  ownerName?: string; // StoreDetailsDTO.OwnerName
  productCount: number; // StoreDetailsDTO.ProductCount
}

export interface UserDto {
  id: string;
  name: string;
  email: string;
  role: 'Admin' | 'StoreOwner' | 'Shopper';
  joinDate: string;
  status: 'Active' | 'Disabled';
}

// Statuses match the backend OrderStatus enum (Reserved | Completed | Cancelled).
export type OrderStatus = 'Reserved' | 'Completed' | 'Cancelled';

export interface OrderDto {
  id: number;
  orderNumber: string;
  customerName: string;
  date: string;
  total: number;
  status: OrderStatus;
  itemsCount: number;
}

export interface DashboardStatsDto {
  totalUsers: number;
  totalStores: number;
  pendingApprovals: number;
  totalProducts: number;
  totalOrders: number;
  totalRevenue: number;
}

type Section = 'overview' | 'stores' | 'campaigns' | 'categories' | 'orders' | 'users' | 'audit';

interface ConfirmDialog {
  title: string;
  message: string;
  confirmLabel: string;
  cancelLabel: string;
  tone: 'danger' | 'warning';
  action: () => void;
}

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css'],
})
export class AdminDashboardComponent implements OnInit {
  private storeService = inject(StoreService);
  private categoryService = inject(CategoryService);
  private productService = inject(ProductService);
  private orderService = inject(OrderService);
  private userService = inject(UserService);
  private engagement = inject(EngagementService);
  private monetization = inject(MonetizationService);
  private auth = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
  private title = inject(Title);
  // Public so the topbar can drive the theme/language toggles directly.
  theme = inject(ThemeService);
  lang = inject(LanguageService);

  /** Bilingual string helper - reactive because lang.t() reads the lang signal. */
  L(en: string, ar: string): string {
    return this.lang.t(en, ar);
  }

  logout(): void {
    this.auth.logout();
    // replaceUrl so pressing back can't return to the admin dashboard.
    this.router.navigate(['/login'], { replaceUrl: true });
  }

  // Real-data load state for the wired sections (stores / categories).
  loading = signal(true);
  errors = signal<string[]>([]);
  confirmDialog = signal<ConfirmDialog | null>(null);

  // Navigation
  activeSection = signal<Section>('overview');
  sidebarOpen = signal(true);
  mobileMenuOpen = signal(false);

  // Search & Filters
  searchQuery = signal('');
  storeFilter = signal<'All' | 'Pending' | 'Approved'>('Pending');
  campaignFilter = signal<'All' | PromotionStatus>('Pending');
  orderFilter = signal<'All' | OrderDto['status']>('All');
  userRoleFilter = signal<'All' | UserDto['role']>('All');
  private documentTitleEffect = effect(() => {
    this.title.setTitle(`${this.sectionTitle()} | Dokkana Admin`);
  });

  // Category Modal
  showCategoryModal = signal(false);
  editingCategory = signal<CategoryDto | null>(null);
  catFormName = signal('');
  catFormDescription = signal('');
  catFormImageUrl = signal('');

  // Data
  // Stores, categories, product count and store-derived stats are loaded from
  // the backend. Users/orders below remain mock until those APIs exist.
  stats = signal<DashboardStatsDto>({
    totalUsers: 0,
    totalStores: 0,
    pendingApprovals: 0,
    totalProducts: 0,
    totalOrders: 0,
    totalRevenue: 0,
  });

  stores = signal<StoreDto[]>([]);

  categories = signal<CategoryDto[]>([]);

  orders = signal<OrderDto[]>([]);

  users = signal<UserDto[]>([]);
  auditLogs = signal<AuditLogDTO[]>([]);
  campaigns = signal<PromotionDTO[]>([]);
  campaignsLoading = signal(false);

  // Computed
  filteredStores = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const f = this.storeFilter();
    return this.stores().filter((s) => {
      const matchStatus =
        f === 'All' || (f === 'Approved' && s.isApproved) || (f === 'Pending' && !s.isApproved);
      const matchQuery =
        !q ||
        s.name.toLowerCase().includes(q) ||
        (s.ownerName ?? '').toLowerCase().includes(q) ||
        (s.address ?? '').toLowerCase().includes(q);
      return matchStatus && matchQuery;
    });
  });

  filteredOrders = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const f = this.orderFilter();
    return this.orders().filter(
      (o) =>
        (f === 'All' || o.status === f) &&
        (!q || o.orderNumber.toLowerCase().includes(q) || o.customerName.toLowerCase().includes(q)),
    );
  });

  filteredUsers = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const f = this.userRoleFilter();
    return this.users().filter(
      (u) =>
        (f === 'All' || u.role === f) &&
        (!q || u.name.toLowerCase().includes(q) || u.email.toLowerCase().includes(q)),
    );
  });

  filteredCampaigns = computed(() => {
    const q = this.searchQuery().toLowerCase();
    const f = this.campaignFilter();
    return this.campaigns().filter((c) => {
      const matchesFilter = f === 'All' || c.status === f;
      const matchesQuery =
        !q ||
        c.storeName.toLowerCase().includes(q) ||
        (c.productTitle ?? '').toLowerCase().includes(q) ||
        (c.title ?? '').toLowerCase().includes(q) ||
        c.type.toLowerCase().includes(q);
      return matchesFilter && matchesQuery;
    });
  });

  pendingCount = computed(() => this.stores().filter((s) => !s.isApproved).length);
  pendingCampaignsCount = computed(() => this.campaigns().filter((c) => c.status === 'Pending').length);
  campaignBudgetTotal = computed(() => this.campaigns().reduce((sum, c) => sum + c.budget, 0));
  campaignClicksTotal = computed(() => this.campaigns().reduce((sum, c) => sum + c.clicks, 0));
  recentOrders = computed(() => this.orders().slice(0, 5));
  overviewPendingStores = computed(() =>
    this.stores()
      .filter((s) => !s.isApproved)
      .slice(0, 4),
  );

  get navItems(): { id: Section; label: string; icon: string }[] {
    return [
      { id: 'overview', label: this.L('Overview', 'نظرة عامة'), icon: 'grid' },
      { id: 'stores', label: this.L('Stores', 'المحلات'), icon: 'store' },
      { id: 'campaigns', label: this.L('Campaigns', 'الحملات'), icon: 'megaphone' },
      { id: 'categories', label: this.L('Categories', 'الفئات'), icon: 'tag' },
      { id: 'orders', label: this.L('Orders', 'الطلبات'), icon: 'box' },
      { id: 'users', label: this.L('Users', 'المستخدمون'), icon: 'users' },
      { id: 'audit', label: this.L('Audit', 'السجل'), icon: 'shield' },
    ];
  }

  orderStatuses: OrderDto['status'][] = ['Reserved', 'Completed', 'Cancelled'];
  readonly storeFilters: ('All' | 'Pending' | 'Approved')[] = ['All', 'Pending', 'Approved'];
  readonly campaignFilters: ('All' | PromotionStatus)[] = ['All', 'Pending', 'Approved', 'Active', 'Rejected', 'Ended'];
  readonly orderFilters: ('All' | OrderStatus)[] = ['All', 'Reserved', 'Completed', 'Cancelled'];
  readonly userRoleFilters: ('All' | UserDto['role'])[] = ['All', 'Admin', 'StoreOwner', 'Shopper'];

  // Localised display labels
  orderStatusLabel(s: OrderStatus): string {
    return {
      Reserved: this.L('Reserved', 'محجوز'),
      Completed: this.L('Completed', 'مكتمل'),
      Cancelled: this.L('Cancelled', 'ملغي'),
    }[s] ?? s;
  }

  campaignStatusLabel(s: string): string {
    const labels: Record<string, string> = {
      Pending: this.L('Pending', 'قيد المراجعة'),
      Approved: this.L('Approved', 'مُعتمدة'),
      Active: this.L('Active', 'نشطة'),
      Rejected: this.L('Rejected', 'مرفوضة'),
      Ended: this.L('Ended', 'منتهية'),
    };
    return labels[s] ?? s;
  }

  campaignTypeLabel(t: string): string {
    const labels: Record<string, string> = {
      FeaturedProduct: this.L('Featured product', 'منتج مميز'),
      SponsoredNotification: this.L('Sponsored notification', 'إشعار ممول'),
      FeaturedStore: this.L('Featured store', 'محل مميز'),
    };
    return labels[t] ?? t;
  }

  storeFilterLabel(f: 'All' | 'Pending' | 'Approved'): string {
    return {
      All: this.L('All', 'الكل'),
      Pending: this.L('Pending', 'قيد المراجعة'),
      Approved: this.L('Approved', 'مُعتمد'),
    }[f];
  }

  orderFilterLabel(f: 'All' | OrderStatus): string {
    return f === 'All' ? this.L('All', 'الكل') : this.orderStatusLabel(f);
  }

  roleLabel(r: 'All' | UserDto['role']): string {
    return {
      All: this.L('All', 'الكل'),
      Admin: this.L('Admin', 'مدير'),
      StoreOwner: this.L('Store Owner', 'صاحب محل'),
      Shopper: this.L('Shopper', 'متسوّق'),
    }[r];
  }

  userStatusLabel(s: UserDto['status']): string {
    return s === 'Active' ? this.L('Active', 'نشط') : this.L('Disabled', 'معطّل');
  }

  // Navigation
  navigateTo(section: Section): void {
    this.activeSection.set(section);
    this.searchQuery.set('');
    this.mobileMenuOpen.set(false);
    if (window.innerWidth < 1024) this.sidebarOpen.set(false);
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((v) => !v);
  }
  isDesktop(): boolean {
    return window.innerWidth >= 1024;
  }

  sectionTitle(): string {
    const map: Record<Section, string> = {
      overview: this.L('Dashboard Overview', 'نظرة عامة على لوحة التحكم'),
      stores: this.L('Store Management', 'إدارة المحلات'),
      campaigns: this.L('Campaign Approvals', 'مراجعة الحملات'),
      categories: this.L('Categories', 'الفئات'),
      orders: this.L('Orders', 'الطلبات'),
      users: this.L('Users', 'المستخدمون'),
      audit: this.L('Audit Log', 'سجل التدقيق'),
    };
    return map[this.activeSection()];
  }

  dismissError(index: number): void {
    this.errors.update((messages) => messages.filter((_, i) => i !== index));
  }

  closeConfirmDialog(): void {
    this.confirmDialog.set(null);
  }

  confirmPendingAction(): void {
    const dialog = this.confirmDialog();
    if (!dialog) return;

    this.confirmDialog.set(null);
    dialog.action();
  }

  // Store Actions
  /** PUT /api/stores/{id}/approve */
  approveStore(id: number): void {
    this.storeService.approve(id).subscribe({
      next: () => {
        this.stores.update((l) => l.map((s) => (s.id === id ? { ...s, isApproved: true } : s)));
        this.recomputeStoreStats();
      },
      error: () => this.reportError('Could not approve the store. Please try again.'),
    });
  }

  /** DELETE /api/stores/{id} */
  deleteStore(id: number): void {
    const store = this.stores().find((s) => s.id === id);
    this.askConfirm({
      title: this.L('Delete store?', 'حذف المحل؟'),
      message: this.L(
        `Delete "${store?.name ?? 'this store'}"? This cannot be undone.`,
        `حذف "${store?.name ?? 'هذا المحل'}"؟ لا يمكن التراجع عن هذا الإجراء.`,
      ),
      confirmLabel: this.L('Delete store', 'حذف المحل'),
      tone: 'danger',
      action: () => this.deleteStoreNow(id),
    });
  }

  private deleteStoreNow(id: number): void {
    this.storeService.delete(id).subscribe({
      next: () => {
        this.stores.update((l) => l.filter((s) => s.id !== id));
        this.recomputeStoreStats();
      },
      error: () => this.reportError('Could not delete the store. Please try again.'),
    });
  }

  setStoreFilter(f: string): void {
    this.storeFilter.set(f as 'All' | 'Pending' | 'Approved');
  }

  /** PUT /api/stores/{id}/tier — admin override of a store's plan. */
  setStoreTier(id: number, tier: 'Free' | 'Pro'): void {
    this.storeService.setTier(id, tier).subscribe({
      next: () => {
        this.stores.update((l) => l.map((s) => (s.id === id ? { ...s, tier } : s)));
      },
      error: () => this.reportError('Could not change the store plan. Please try again.'),
    });
  }

  setCampaignFilter(f: string): void {
    this.campaignFilter.set(f as 'All' | PromotionStatus);
  }

  approveCampaign(id: number): void {
    this.monetization.approveCampaign(id).subscribe({
      next: updated => this.campaigns.update(items => items.map(c => c.id === id ? updated : c)),
      error: err => this.reportError(err?.error?.message || 'Could not approve the campaign. Please try again.'),
    });
  }

  rejectCampaign(id: number): void {
    const campaign = this.campaigns().find((c) => c.id === id);
    this.askConfirm({
      title: this.L('Reject campaign?', 'رفض الحملة؟'),
      message: this.L(
        `Reject "${campaign?.title || campaign?.productTitle || 'this campaign'}"? The merchant will see it as rejected.`,
        `رفض "${campaign?.title || campaign?.productTitle || 'هذه الحملة'}"؟ سيظهر للتاجر أنها مرفوضة.`,
      ),
      confirmLabel: this.L('Reject campaign', 'رفض الحملة'),
      tone: 'warning',
      action: () => this.rejectCampaignNow(id),
    });
  }

  private rejectCampaignNow(id: number): void {
    this.monetization.rejectCampaign(id, this.L('Rejected by admin review.', 'مرفوضة بعد مراجعة الإدارة.')).subscribe({
      next: () => this.loadCampaigns(),
      error: err => this.reportError(err?.error?.message || 'Could not reject the campaign. Please try again.'),
    });
  }

  // Category Actions
  openAddCategory(): void {
    this.editingCategory.set(null);
    this.catFormName.set('');
    this.catFormDescription.set('');
    this.catFormImageUrl.set('');
    this.showCategoryModal.set(true);
  }

  openEditCategory(cat: CategoryDto): void {
    this.editingCategory.set(cat);
    this.catFormName.set(cat.name);
    this.catFormDescription.set(cat.description ?? '');
    this.catFormImageUrl.set(cat.imageUrl ?? '');
    this.showCategoryModal.set(true);
  }

  closeCategoryModal(): void {
    this.showCategoryModal.set(false);
  }

  /** POST /api/categories  OR  PUT /api/categories/{id} */
  saveCategory(): void {
    const name = this.catFormName().trim();
    if (!name) return;
    const payload: CategoryCreateDto = {
      name,
      description: this.catFormDescription().trim() || undefined,
      imageUrl: this.catFormImageUrl().trim() || undefined,
    };
    const editing = this.editingCategory();
    if (editing) {
      this.categoryService.update(editing.id, payload).subscribe({
        next: () => {
          this.categories.update((l) => l.map((c) => (c.id === editing.id ? { ...c, ...payload } : c)));
          this.closeCategoryModal();
        },
        error: () => this.reportError('Could not update the category.'),
      });
    } else {
      this.categoryService.create(payload).subscribe({
        next: (created) => {
          this.categories.update((l) => [
            ...l,
            {
              id: created.id,
              name: created.name,
              description: created.description ?? undefined,
              imageUrl: created.imageUrl ?? undefined,
            },
          ]);
          this.closeCategoryModal();
        },
        error: () => this.reportError('Could not create the category.'),
      });
    }
  }

  /** DELETE /api/categories/{id} */
  deleteCategory(id: number): void {
    const category = this.categories().find((c) => c.id === id);
    this.askConfirm({
      title: this.L('Delete category?', 'حذف الفئة؟'),
      message: this.L(
        `Delete "${category?.name ?? 'this category'}"? Products may still reference it.`,
        `حذف "${category?.name ?? 'هذه الفئة'}"؟ قد تظل بعض المنتجات مرتبطة بها.`,
      ),
      confirmLabel: this.L('Delete category', 'حذف الفئة'),
      tone: 'warning',
      action: () => this.deleteCategoryNow(id),
    });
  }

  private deleteCategoryNow(id: number): void {
    this.categoryService.delete(id).subscribe({
      next: () => this.categories.update((l) => l.filter((c) => c.id !== id)),
      error: () => this.reportError('Could not delete the category.'),
    });
  }

  // Order Actions
  setOrderFilter(s: string): void {
    this.orderFilter.set(s as OrderDto['status'] | 'All');
  }

  /** PUT /api/orders/{id}/status */
  updateOrderStatus(id: number, status: OrderDto['status']): void {
    const prev = this.orders().find((o) => o.id === id)?.status;
    this.orders.update((l) => l.map((o) => (o.id === id ? { ...o, status } : o)));
    this.orderService.updateStatus(id, status).subscribe({
      next: () => {},
      error: () => {
        this.reportError('Could not update the order status.');
        // Roll back the optimistic change on failure.
        if (prev) this.orders.update((l) => l.map((o) => (o.id === id ? { ...o, status: prev } : o)));
      },
    });
  }

  // User Actions
  setUserRoleFilter(r: string): void {
    this.userRoleFilter.set(r as UserDto['role'] | 'All');
  }

  /** PUT /api/users/{id}/status */
  toggleUserStatus(id: string): void {
    const user = this.users().find((u) => u.id === id);
    if (!user) return;
    const disabled = user.status === 'Active'; // toggling: Active -> disabled
    this.userService.setStatus(id, disabled).subscribe({
      next: () =>
        this.users.update((l) =>
          l.map((u) => (u.id === id ? { ...u, status: disabled ? 'Disabled' : 'Active' } : u)),
        ),
      error: () => this.reportError('Could not change the user status.'),
    });
  }

  // Helpers
  formatCurrency(n: number): string {
    return n.toLocaleString('en-EG') + ' EGP';
  }

  formatDate(d: string): string {
    if (!d) return '—';
    const date = new Date(d);
    if (isNaN(date.getTime())) return '—';
    return date.toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }

  orderBadge(s: OrderDto['status']): string {
    return (
      {
        Reserved: 'badge-pending',
        Completed: 'badge-delivered',
        Cancelled: 'badge-cancelled',
      }[s] ?? ''
    );
  }

  storeBadge(isApproved: boolean): string {
    return isApproved ? 'badge-delivered' : 'badge-pending';
  }
  storeLabel(isApproved: boolean): string {
    return isApproved ? this.L('Approved', 'مُعتمد') : this.L('Pending', 'قيد المراجعة');
  }

  userRoleBadge(r: UserDto['role']): string {
    return { Admin: 'badge-admin', StoreOwner: 'badge-store', Shopper: 'badge-shopper' }[r] ?? '';
  }

  campaignBadge(status: string): string {
    const classes: Record<string, string> = {
      Pending: 'badge-pending',
      Approved: 'badge-processing',
      Active: 'badge-delivered',
      Rejected: 'badge-cancelled',
      Ended: 'badge-shopper',
    };
    return classes[status] ?? 'badge-shopper';
  }

  ngOnInit(): void {
    if (window.innerWidth < 1024) this.sidebarOpen.set(false);
    // Keep the sidebar open on desktop widths. Clean up on destroy so navigating
    // away from /admin and back doesn't stack duplicate listeners.
    const onResize = () => {
      if (window.innerWidth >= 1024) this.sidebarOpen.set(true);
    };
    window.addEventListener('resize', onResize);
    this.destroyRef.onDestroy(() => window.removeEventListener('resize', onResize));
    this.loadData();
  }

  // Backend data loading
  private loadData(): void {
    this.loading.set(true);
    this.errors.set([]);

    this.storeService.getAll().subscribe({
      next: (stores) => {
        this.stores.set(
          stores.map((s) => ({
            id: s.id,
            name: s.name,
            description: s.description ?? undefined,
            logoUrl: s.logoUrl ?? undefined,
            address: s.address ?? undefined,
            latitude: s.latitude ?? undefined,
            longitude: s.longitude ?? undefined,
            isApproved: s.isApproved,
            tier: s.tier ?? 'Free',
            productCount: s.productCount ?? 0,
          })),
        );
        this.recomputeStoreStats();
        this.loading.set(false);
      },
      error: () => {
        this.reportError('Could not load stores from the server.');
        this.loading.set(false);
      },
    });

    this.categoryService.getAll().subscribe({
      next: (cats) =>
        this.categories.set(
          cats.map((c) => ({
            id: c.id,
            name: c.name,
            description: c.description ?? undefined,
            imageUrl: c.imageUrl ?? undefined,
          })),
        ),
      error: () => this.reportError('Could not load categories from the server.'),
    });

    // Total product count comes from the paged products endpoint's totalCount.
    this.productService.getAll({ page: 1, pageSize: 1 }).subscribe({
      next: (page) =>
        this.stats.update((s) => ({ ...s, totalProducts: page.totalCount })),
      error: () => {},
    });

    // Orders (admin): map backend shape → UI rows, then derive order stats.
    this.orderService.getAllAdmin().subscribe({
      next: (orders) => {
        this.orders.set(
          orders.map((o) => ({
            id: o.id,
            orderNumber: o.pickupCode,
            customerName: o.customerName,
            date: o.createdAt,
            total: o.totalAmount,
            status: o.status as OrderDto['status'],
            itemsCount: o.itemCount,
          })),
        );
        this.stats.update((s) => ({
          ...s,
          totalOrders: orders.length,
          // Realized revenue only: completed orders, not open or cancelled ones.
          totalRevenue: orders
            .filter((o) => o.status === 'Completed')
            .reduce((sum, o) => sum + o.totalAmount, 0),
        }));
      },
      error: () => this.reportError('Could not load orders from the server.'),
    });

    // Users (admin).
    this.userService.getAll().subscribe({
      next: (users) => {
        this.users.set(
          users.map((u) => ({
            id: u.id,
            name: u.name,
            email: u.email,
            role: u.role,
            joinDate: u.createdAt,
            status: u.isDisabled ? 'Disabled' : 'Active',
          })),
        );
        this.stats.update((s) => ({ ...s, totalUsers: users.length }));
      },
      error: () => this.reportError('Could not load users from the server.'),
    });

    this.engagement.getAuditLogs(100).subscribe({
      next: logs => this.auditLogs.set(logs),
      error: () => this.reportError('Could not load audit logs from the server.'),
    });

    this.loadCampaigns();
  }

  private loadCampaigns(): void {
    this.campaignsLoading.set(true);
    this.monetization.getAdminCampaigns().subscribe({
      next: campaigns => {
        this.campaigns.set(campaigns);
        this.campaignsLoading.set(false);
      },
      error: () => {
        this.reportError('Could not load campaigns from the server.');
        this.campaignsLoading.set(false);
      },
    });
  }

  // Stats derived from live stores/products/orders/users APIs.
  private recomputeStoreStats(): void {
    this.stats.update((s) => ({
      ...s,
      totalStores: this.stores().length,
      pendingApprovals: this.stores().filter((st) => !st.isApproved).length,
    }));
  }

  private askConfirm(dialog: Omit<ConfirmDialog, 'cancelLabel'> & { cancelLabel?: string }): void {
    this.confirmDialog.set({
      ...dialog,
      cancelLabel: dialog.cancelLabel ?? this.L('Cancel', 'إلغاء'),
    });
  }

  private reportError(message: string): void {
    this.errors.update((messages) =>
      messages.includes(message) ? messages : [...messages, message],
    );
  }
}
