import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

// ── DTOs — matching backend exactly ──────────────────────

/** Source: CategoryDTO.cs */
export interface CategoryDto {
  id: number;
  name: string;
  description?: string;
  imageUrl?: string;
}

/** Source: CategoryCreateDTO.cs — used for POST + PUT */
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
  id: number;
  name: string;
  email: string;
  role: 'Admin' | 'StoreOwner' | 'Shopper';
  joinDate: string;
  status: 'Active' | 'Disabled';
}

export interface OrderDto {
  id: number;
  orderNumber: string;
  customerName: string;
  date: string;
  total: number;
  status: 'Pending' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
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

type Section = 'overview' | 'stores' | 'categories' | 'orders' | 'users';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin-dashboard.html',
  styleUrls: ['./admin-dashboard.css'],
})
export class AdminDashboardComponent implements OnInit {
  // ── Navigation ───────────────────────────────────────
  activeSection = signal<Section>('overview');
  sidebarOpen = signal(true);
  mobileMenuOpen = signal(false);

  // ── Search & Filters ─────────────────────────────────
  searchQuery = signal('');
  storeFilter = signal<'All' | 'Pending' | 'Approved'>('Pending');
  orderFilter = signal<'All' | OrderDto['status']>('All');
  userRoleFilter = signal<'All' | UserDto['role']>('All');

  // ── Category Modal ────────────────────────────────────
  showCategoryModal = signal(false);
  editingCategory = signal<CategoryDto | null>(null);
  catFormName = signal('');
  catFormDescription = signal('');
  catFormImageUrl = signal('');

  // ── Data ─────────────────────────────────────────────
  stats = signal<DashboardStatsDto>({
    totalUsers: 1284,
    totalStores: 47,
    pendingApprovals: 7,
    totalProducts: 3621,
    totalOrders: 892,
    totalRevenue: 458750,
  });

  stores = signal<StoreDto[]>([
    {
      id: 1,
      name: 'Nile Threads',
      description: 'Egyptian handcrafted fashion',
      address: 'Zamalek, Cairo',
      isApproved: false,
      ownerName: 'Sara Ahmed',
      productCount: 0,
    },
    {
      id: 2,
      name: 'Cairo Couture',
      description: 'Modern Egyptian streetwear',
      address: 'Maadi, Cairo',
      isApproved: false,
      ownerName: 'Mohamed Ali',
      productCount: 0,
    },
    {
      id: 3,
      name: 'Lotus Fashion',
      description: 'Handmade luxury accessories',
      address: 'Heliopolis, Cairo',
      isApproved: true,
      ownerName: 'Nour Hassan',
      productCount: 42,
    },
    {
      id: 4,
      name: 'Delta Designs',
      description: 'Delta region craft wear',
      address: 'Mansoura',
      isApproved: true,
      ownerName: 'Karim Saad',
      productCount: 28,
    },
    {
      id: 5,
      name: 'Luxor Labels',
      description: 'Upper Egypt inspired fashion',
      address: 'Luxor',
      isApproved: false,
      ownerName: 'Hana Fathy',
      productCount: 0,
    },
    {
      id: 6,
      name: 'Aswan Atelier',
      description: 'Nubian fashion house',
      address: 'Aswan',
      isApproved: false,
      ownerName: 'Dina Ramadan',
      productCount: 0,
    },
    {
      id: 7,
      name: 'Pharaoh Fit',
      description: 'Activewear with pharaonic art',
      address: 'New Cairo',
      isApproved: false,
      ownerName: 'Tarek Nasser',
      productCount: 0,
    },
  ]);

  categories = signal<CategoryDto[]>([
    { id: 1, name: 'Dresses', description: 'All types of dresses and gowns' },
    { id: 2, name: 'Tops', description: 'Shirts, blouses, and casual tops' },
    { id: 3, name: 'Pants', description: 'Trousers, jeans, and wide-leg pants' },
    { id: 4, name: 'Shoes', description: 'Heels, flats, sneakers, and sandals' },
    { id: 5, name: 'Bags', description: 'Handbags, totes, clutches, and backpacks' },
    { id: 6, name: 'Accessories', description: 'Jewellery, scarves, belts, and more' },
    { id: 7, name: 'Abayas', description: 'Modest fashion abayas and kaftans' },
    { id: 8, name: 'Sportswear', description: 'Activewear and athletic clothing' },
  ]);

  orders = signal<OrderDto[]>([
    {
      id: 1,
      orderNumber: 'DK-10042',
      customerName: 'Fatma Omar',
      date: '2026-06-13',
      total: 1250,
      status: 'Pending',
      itemsCount: 3,
    },
    {
      id: 2,
      orderNumber: 'DK-10041',
      customerName: 'Ahmed Samy',
      date: '2026-06-12',
      total: 780,
      status: 'Processing',
      itemsCount: 2,
    },
    {
      id: 3,
      orderNumber: 'DK-10040',
      customerName: 'Layla Mostafa',
      date: '2026-06-12',
      total: 2100,
      status: 'Shipped',
      itemsCount: 5,
    },
    {
      id: 4,
      orderNumber: 'DK-10039',
      customerName: 'Hassan Ibrahim',
      date: '2026-06-11',
      total: 450,
      status: 'Delivered',
      itemsCount: 1,
    },
    {
      id: 5,
      orderNumber: 'DK-10038',
      customerName: 'Mona Ragab',
      date: '2026-06-10',
      total: 1830,
      status: 'Delivered',
      itemsCount: 4,
    },
    {
      id: 6,
      orderNumber: 'DK-10037',
      customerName: 'Youssef Gaber',
      date: '2026-06-09',
      total: 320,
      status: 'Cancelled',
      itemsCount: 1,
    },
    {
      id: 7,
      orderNumber: 'DK-10036',
      customerName: 'Rania Salah',
      date: '2026-06-09',
      total: 960,
      status: 'Processing',
      itemsCount: 2,
    },
    {
      id: 8,
      orderNumber: 'DK-10035',
      customerName: 'Khaled Fouad',
      date: '2026-06-08',
      total: 3400,
      status: 'Shipped',
      itemsCount: 6,
    },
  ]);

  users = signal<UserDto[]>([
    {
      id: 1,
      name: 'Admin User',
      email: 'admin@dokkana.com',
      role: 'Admin',
      joinDate: '2026-01-01',
      status: 'Active',
    },
    {
      id: 2,
      name: 'Sara Ahmed',
      email: 'sara@nilethreads.com',
      role: 'StoreOwner',
      joinDate: '2026-06-01',
      status: 'Active',
    },
    {
      id: 3,
      name: 'Mohamed Ali',
      email: 'mo@cairocouture.com',
      role: 'StoreOwner',
      joinDate: '2026-06-03',
      status: 'Active',
    },
    {
      id: 4,
      name: 'Fatma Omar',
      email: 'fatma@email.com',
      role: 'Shopper',
      joinDate: '2026-03-15',
      status: 'Active',
    },
    {
      id: 5,
      name: 'Ahmed Samy',
      email: 'ahmed@email.com',
      role: 'Shopper',
      joinDate: '2026-04-20',
      status: 'Active',
    },
    {
      id: 6,
      name: 'Layla Mostafa',
      email: 'layla@email.com',
      role: 'Shopper',
      joinDate: '2026-05-01',
      status: 'Disabled',
    },
    {
      id: 7,
      name: 'Karim Saad',
      email: 'karim@deltadesigns.com',
      role: 'StoreOwner',
      joinDate: '2026-05-20',
      status: 'Active',
    },
  ]);

  // ── Computed ──────────────────────────────────────────
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

  navItems: { id: Section; label: string; icon: string }[] = [
    { id: 'overview', label: 'Overview', icon: 'grid' },
    { id: 'stores', label: 'Stores', icon: 'store' },
    { id: 'categories', label: 'Categories', icon: 'tag' },
    { id: 'orders', label: 'Orders', icon: 'box' },
    { id: 'users', label: 'Users', icon: 'users' },
  ];

  orderStatuses: OrderDto['status'][] = [
    'Pending',
    'Processing',
    'Shipped',
    'Delivered',
    'Cancelled',
  ];

  // ── Navigation ────────────────────────────────────────
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
      overview: 'Dashboard Overview',
      stores: 'Store Management',
      categories: 'Categories',
      orders: 'Orders',
      users: 'Users',
    };
    return map[this.activeSection()];
  }

  // ── Store Actions ──────────────────────────────────────
  /** PUT /api/stores/{id}/approve */
  approveStore(id: number): void {
    this.stores.update((l) => l.map((s) => (s.id === id ? { ...s, isApproved: true } : s)));
    this.stats.update((s) => ({ ...s, pendingApprovals: Math.max(0, s.pendingApprovals - 1) }));
  }

  /** DELETE /api/stores/{id} */
  deleteStore(id: number): void {
    const store = this.stores().find((s) => s.id === id);
    if (!confirm(`Delete "${store?.name}"? This cannot be undone.`)) return;
    if (store && !store.isApproved) {
      this.stats.update((s) => ({ ...s, pendingApprovals: Math.max(0, s.pendingApprovals - 1) }));
    }
    this.stores.update((l) => l.filter((s) => s.id !== id));
  }

  setStoreFilter(f: string): void {
    this.storeFilter.set(f as 'All' | 'Pending' | 'Approved');
  }

  // ── Category Actions ───────────────────────────────────
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
      this.categories.update((l) => l.map((c) => (c.id === editing.id ? { ...c, ...payload } : c)));
    } else {
      this.categories.update((l) => [...l, { id: Date.now(), ...payload }]);
    }
    this.closeCategoryModal();
  }

  /** DELETE /api/categories/{id} */
  deleteCategory(id: number): void {
    if (!confirm('Delete this category?')) return;
    this.categories.update((l) => l.filter((c) => c.id !== id));
  }

  // ── Order Actions ──────────────────────────────────────
  setOrderFilter(s: string): void {
    this.orderFilter.set(s as OrderDto['status'] | 'All');
  }

  updateOrderStatus(id: number, status: OrderDto['status']): void {
    this.orders.update((l) => l.map((o) => (o.id === id ? { ...o, status } : o)));
  }

  // ── User Actions ───────────────────────────────────────
  setUserRoleFilter(r: string): void {
    this.userRoleFilter.set(r as UserDto['role'] | 'All');
  }

  toggleUserStatus(id: number): void {
    this.users.update((l) =>
      l.map((u) =>
        u.id === id ? { ...u, status: u.status === 'Active' ? 'Disabled' : 'Active' } : u,
      ),
    );
  }

  // ── Helpers ────────────────────────────────────────────
  formatCurrency(n: number): string {
    return n.toLocaleString('en-EG') + ' EGP';
  }

  formatDate(d: string): string {
    return new Date(d).toLocaleDateString('en-GB', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    });
  }

  orderBadge(s: OrderDto['status']): string {
    return (
      {
        Pending: 'badge-pending',
        Processing: 'badge-processing',
        Shipped: 'badge-shipped',
        Delivered: 'badge-delivered',
        Cancelled: 'badge-cancelled',
      }[s] ?? ''
    );
  }

  storeBadge(isApproved: boolean): string {
    return isApproved ? 'badge-delivered' : 'badge-pending';
  }
  storeLabel(isApproved: boolean): string {
    return isApproved ? 'Approved' : 'Pending';
  }

  userRoleBadge(r: UserDto['role']): string {
    return { Admin: 'badge-admin', StoreOwner: 'badge-store', Shopper: 'badge-shopper' }[r] ?? '';
  }

  ngOnInit(): void {
    if (window.innerWidth < 1024) this.sidebarOpen.set(false);
    window.addEventListener('resize', () => {
      if (window.innerWidth >= 1024) this.sidebarOpen.set(true);
    });
  }
}
