import { Component, DestroyRef, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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

type Section = 'overview' | 'stores' | 'categories' | 'orders' | 'users' | 'audit';

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
  private auth = inject(AuthService);
  private router = inject(Router);
  private destroyRef = inject(DestroyRef);
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
  error = signal('');

  // Navigation
  activeSection = signal<Section>('overview');
  sidebarOpen = signal(true);
  mobileMenuOpen = signal(false);

  // Search & Filters
  searchQuery = signal('');
  storeFilter = signal<'All' | 'Pending' | 'Approved'>('Pending');
  orderFilter = signal<'All' | OrderDto['status']>('All');
  userRoleFilter = signal<'All' | UserDto['role']>('All');

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

  pendingCount = computed(() => this.stores().filter((s) => !s.isApproved).length);
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
      { id: 'categories', label: this.L('Categories', 'الفئات'), icon: 'tag' },
      { id: 'orders', label: this.L('Orders', 'الطلبات'), icon: 'box' },
      { id: 'users', label: this.L('Users', 'المستخدمون'), icon: 'users' },
      { id: 'audit', label: this.L('Audit', 'السجل'), icon: 'shield' },
    ];
  }

  orderStatuses: OrderDto['status'][] = ['Reserved', 'Completed', 'Cancelled'];
  readonly storeFilters: ('All' | 'Pending' | 'Approved')[] = ['All', 'Pending', 'Approved'];
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
      categories: this.L('Categories', 'الفئات'),
      orders: this.L('Orders', 'الطلبات'),
      users: this.L('Users', 'المستخدمون'),
      audit: this.L('Audit Log', 'سجل التدقيق'),
    };
    return map[this.activeSection()];
  }

  // Store Actions
  /** PUT /api/stores/{id}/approve */
  approveStore(id: number): void {
    this.storeService.approve(id).subscribe({
      next: () => {
        this.stores.update((l) => l.map((s) => (s.id === id ? { ...s, isApproved: true } : s)));
        this.recomputeStoreStats();
      },
      error: () => this.error.set('Could not approve the store. Please try again.'),
    });
  }

  /** DELETE /api/stores/{id} */
  deleteStore(id: number): void {
    const store = this.stores().find((s) => s.id === id);
    if (!confirm(`Delete "${store?.name}"? This cannot be undone.`)) return;
    this.storeService.delete(id).subscribe({
      next: () => {
        this.stores.update((l) => l.filter((s) => s.id !== id));
        this.recomputeStoreStats();
      },
      error: () => this.error.set('Could not delete the store. Please try again.'),
    });
  }

  setStoreFilter(f: string): void {
    this.storeFilter.set(f as 'All' | 'Pending' | 'Approved');
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
        error: () => this.error.set('Could not update the category.'),
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
        error: () => this.error.set('Could not create the category.'),
      });
    }
  }

  /** DELETE /api/categories/{id} */
  deleteCategory(id: number): void {
    if (!confirm('Delete this category?')) return;
    this.categoryService.delete(id).subscribe({
      next: () => this.categories.update((l) => l.filter((c) => c.id !== id)),
      error: () => this.error.set('Could not delete the category.'),
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
        this.error.set('Could not update the order status.');
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
      error: () => this.error.set('Could not change the user status.'),
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
    this.error.set('');

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
            productCount: 0,
          })),
        );
        this.recomputeStoreStats();
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load stores from the server.');
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
      error: () => this.error.set('Could not load categories from the server.'),
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
          totalRevenue: orders.reduce((sum, o) => sum + o.totalAmount, 0),
        }));
      },
      error: () => this.error.set('Could not load orders from the server.'),
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
            joinDate: '',
            status: u.isDisabled ? 'Disabled' : 'Active',
          })),
        );
        this.stats.update((s) => ({ ...s, totalUsers: users.length }));
      },
      error: () => this.error.set('Could not load users from the server.'),
    });

    this.engagement.getAuditLogs(100).subscribe({
      next: logs => this.auditLogs.set(logs),
      error: () => this.error.set('Could not load audit logs from the server.'),
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
}
