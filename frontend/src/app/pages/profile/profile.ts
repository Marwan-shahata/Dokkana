import {
  Component,
  OnInit,
  inject,
  signal,
  computed,
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { EngagementService } from '../../core/services/engagement.service';
import { OrderService } from '../../core/services/order.service';
import { UploadService } from '../../core/services/upload.service';
import { toMediaUrl } from '../../core/services/media-url';
import { ThemeService } from '../../core/services/theme.service';
import { LanguageService } from '../../core/services/language.service';
import { CartService } from '../../core/services/cart.service';
import { CartApiService } from '../../core/services/cart-api.service';
import { MonetizationService } from '../../core/services/monetization.service';
import { OrderSummaryDTO } from '../../core/models/order.models';
import { UserProfileDTO, UserProfileUpdateRequest } from '../../core/models/auth.models';
import { WishlistItemDTO, FollowedStoreDTO } from '../../core/models/engagement.models';
import { AlertDTO } from '../../core/models/monetization.models';

export type OrderStatus = 'Reserved' | 'Completed' | 'Cancelled' | 'Delivered' | 'Processing' | 'Shipped';
export interface UserProfile {
  fullName: string;
  email: string;
  phone: string;
  address: string;
  gender: string;
  location: string;
  memberSince: string;
  avatar: string;
  initials: string;
  avatarColor: string;
}


export interface Order {
  id: string;
  productName: string;
  store: string;
  date: string;
  status: OrderStatus;
  total: number;
  currency: string;
  quantity: number;
}

export interface WishlistItem {
  id: number;
  name: string;
  store: string;
  price: number;
  oldPrice?: number;
  currency: string;
  gradient: string;
  imageUrl?: string | null;
}

export interface SavedStore {
  id: number;
  name: string;
  description: string;
  address: string;
  logoUrl?: string | null;
  productCount: number;
  gradient: string;
  initials: string;
}

export interface AccountStat {
  label: string;
  value: number | string;
  icon: string;
  accent: string;
}

export interface SettingItem {
  id: string;
  label: string;
  desc: string;
  icon: string;
  action: 'navigate' | 'toggle' | 'danger';
  toggleValue?: boolean;
}


@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class Profile implements OnInit {
  private auth = inject(AuthService);
  private orderService = inject(OrderService);
  private upload = inject(UploadService);
  private engagement = inject(EngagementService);
  private router = inject(Router);
  private theme = inject(ThemeService);
  private language = inject(LanguageService);
  private cart = inject(CartService);
  private cartApi = inject(CartApiService);
  private monetization = inject(MonetizationService);
  private avatarStorageUrl = '';

  constructor(private sanitizer: DomSanitizer, private title: Title) {}

   // State
  activeTab = signal<'overview' | 'orders' | 'wishlist' | 'stores' | 'settings'>('overview');
  isEditOpen = signal(false);
  isPasswordOpen = signal(false);
  // Dark mode is owned by the global ThemeService so the toggle here stays in
  // sync with the navbar switch and the rest of the site.
  darkMode = computed(() => this.theme.isDark());
  notifications = signal(true);
  alerts = signal<AlertDTO[]>([]);
  alertsLoading = signal(false);
  editForm: Partial<UserProfile> = {};
  orderFilter: OrderStatus | 'All' = 'All';
  toastMessage = signal<string | null>(null);
  ordersLoading = signal(false);
  ordersError = signal('');
  profileLoading = signal(false);
  profileError = signal('');
  avatarUploading = signal(false);
  passwordLoading = signal(false);
  savedStoresCount = signal(0);
  savedStoresLoading = signal(false);
  reviewsCount = signal(0);
  passwordForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  };

  // User data
  user: UserProfile = {
    fullName:    'Sara El-Masry',
    email:       'sara.elmasry@gmail.com',
    phone:       '',
    address:     '',
    gender:      '',
    location:    'Egypt',
    memberSince: '',
    avatar:      '',
    initials:    'SE',
    avatarColor: 'var(--clay)',
  };

  // Stats
  get stats(): AccountStat[] {
    return [
      {
        label: this.L('Total Orders', 'إجمالي الطلبات'), value: this.orders.length, accent: 'var(--clay)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M6 6h15l-2 9H8L6 6Z"/><path d="M6 6 5 3H2"/><circle cx="9" cy="20" r="1"/><circle cx="18" cy="20" r="1"/></svg>`,
      },
      {
        label: this.L('Wishlist', 'المفضلة'), value: this.wishlist.length, accent: 'var(--hibiscus)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>`,
      },
      {
        label: this.L('Saved Stores', 'المحلات المحفوظة'), value: this.savedStoresCount(), accent: 'var(--nile)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>`,
      },
      {
        label: this.L('Reviews', 'التقييمات'), value: this.reviewsCount(), accent: 'var(--ochre)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>`,
      },
    ];
  }

  // Saved stores
  savedStores: SavedStore[] = [];

  // Orders
  orders: Order[] = [];

  get filteredOrders(): Order[] {
    if (this.orderFilter === 'All') return this.orders;
    return this.orders.filter(o => o.status === this.orderFilter);
  }

  readonly orderFilters: (OrderStatus | 'All')[] = ['All', 'Reserved', 'Completed', 'Cancelled'];

  // Wishlist
  wishlist: WishlistItem[] = [];

  // Settings
  get settingsItems(): SettingItem[] {
    return [
      {
        id: 'password', label: this.L('Change Password', 'تغيير كلمة المرور'), action: 'navigate',
        desc: this.L('Update your account password regularly for security.', 'حدّث كلمة مرور حسابك بانتظام للحفاظ على الأمان.'),
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>`,
      },
      {
        id: 'notifications', label: this.L('Notification Preferences', 'تفضيلات الإشعارات'), action: 'toggle',
        desc: this.L('Allow sponsored and campaign notifications in your feed.', 'السماح بالإشعارات الممولة والتنبيهات داخل حسابك.'),
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"/></svg>`,
        toggleValue: this.notifications(),
      },
      {
        id: 'language', label: this.L('Language Settings', 'إعدادات اللغة'), action: 'navigate',
        desc: this.L('Switch between Arabic and English interface.', 'التبديل بين الواجهة العربية والإنجليزية.'),
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 014 10 15.3 15.3 0 01-4 10 15.3 15.3 0 01-4-10 15.3 15.3 0 014-10z"/></svg>`,
      },
      {
        id: 'darkmode', label: this.L('Dark Mode', 'الوضع الليلي'), action: 'toggle',
        desc: this.L('Switch to a darker interface for low-light environments.', 'التبديل إلى واجهة داكنة مناسبة للإضاءة المنخفضة.'),
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/></svg>`,
        toggleValue: this.darkMode(),
      },
    ];
  }

  // Lifecycle
  ngOnInit(): void {
    this.syncUserFromSession();
    this.editForm = { ...this.user };
    this.title.setTitle('Dokkana | Profile');
    this.loadProfile();
    this.loadOrders();
    this.loadWishlist();
    this.loadSavedStores();
    this.loadReviewCount();
    this.loadNotificationPreferences();
    this.loadAlerts();
  }

  // Methods
  /** Bilingual string helper - reactive because lang.t() reads the lang signal. */
  L(en: string, ar: string): string {
    return this.language.t(en, ar);
  }

  setTab(tab: 'overview' | 'orders' | 'wishlist' | 'stores' | 'settings'): void {
    this.activeTab.set(tab);
  }

  openEdit(): void {
    this.editForm = { ...this.user };
    this.isEditOpen.set(true);
  }

  closeEdit(): void {
    this.isEditOpen.set(false);
  }

  saveEdit(): void {
    this.auth.updateProfile(this.toProfileUpdateRequest(this.editForm)).subscribe({
      next: res => {
        if (res.success && res.data) this.applyProfile(res.data);
        this.isEditOpen.set(false);
        this.showToast(this.L('Profile updated successfully.', 'تم تحديث الملف الشخصي بنجاح.'));
      },
      error: err => this.showToast(err?.error?.message || this.L('Could not update profile.', 'تعذّر تحديث الملف الشخصي.')),
    });
  }

  editProfile(): void {
    this.openEdit();
  }

  removeWishlistItem(id: number): void {
    this.engagement.removeFromWishlist(id).subscribe({
      next: () => {
        this.wishlist = this.wishlist.filter(w => w.id !== id);
        this.showToast(this.L('Item removed from wishlist.', 'تمت إزالة العنصر من المفضلة.'));
      },
      error: err => this.showToast(err?.error?.message || this.L('Could not remove item.', 'تعذّرت إزالة العنصر.')),
    });
  }

  viewStore(id: number): void {
    this.router.navigate(['/stores', id]);
  }

  unfollowStore(id: number): void {
    this.engagement.unfollowStore(id).subscribe({
      next: () => {
        this.savedStores = this.savedStores.filter(s => s.id !== id);
        this.savedStoresCount.set(this.savedStores.length);
        this.showToast(this.L('Store removed from saved.', 'تمت إزالة المحل من المحفوظات.'));
      },
      error: err => this.showToast(err?.error?.message || this.L('Could not remove store.', 'تعذّرت إزالة المحل.')),
    });
  }

  toggleDarkMode(): void {
    this.theme.toggle();
  }

  toggleNotifications(): void {
    const next = !this.notifications();
    this.notifications.set(next);
    this.monetization.setNotificationPreferences({ sponsoredNotificationsEnabled: next }).subscribe({
      next: prefs => {
        this.notifications.set(prefs.sponsoredNotificationsEnabled);
        this.showToast(
          prefs.sponsoredNotificationsEnabled
            ? this.L('Sponsored notifications enabled.', 'تم تفعيل الإشعارات الممولة.')
            : this.L('Sponsored notifications muted.', 'تم كتم الإشعارات الممولة.'),
        );
      },
      error: () => {
        this.notifications.update(v => !v);
        this.showToast(this.L('Could not update notification preferences.', 'تعذّر تحديث تفضيلات الإشعارات.'));
      },
    });
  }

  toggleLanguage(): void {
    this.language.toggle();
  }

  addToCart(item: WishlistItem): void {
    this.cart.add({
      id: item.id,
      name: item.name,
      store: item.store,
      price: item.price,
      image: item.imageUrl ?? '',
    });
    this.showToast(this.L(`${item.name} added to cart.`, `تمت إضافة ${item.name} إلى السلة.`));
    // Persist to the backend cart; ignore failures silently (badge already updated).
    this.cartApi.addItem(item.id, 1).subscribe({ error: () => {} });
  }

  uploadAvatar(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file || this.avatarUploading()) return;

    if (!file.type.startsWith('image/')) {
      this.showToast(this.L('Please choose an image file.', 'اختر ملف صورة صحيح.'));
      return;
    }

    if (file.size > 5_000_000) {
      this.showToast(this.L('Image must be 5MB or smaller.', 'يجب ألا تتجاوز الصورة 5 ميجابايت.'));
      return;
    }

    this.avatarUploading.set(true);
    this.upload.uploadImage(file, 'avatars').subscribe({
      next: url => {
        const nextProfile = { ...this.user, avatar: url };
        this.auth.updateProfile(this.toProfileUpdateRequest(nextProfile)).subscribe({
          next: res => {
            if (res.success && res.data) this.applyProfile(res.data);
            this.avatarUploading.set(false);
            this.showToast(this.L('Profile photo updated.', 'تم تحديث صورة الملف الشخصي.'));
          },
          error: err => {
            this.avatarUploading.set(false);
            this.showToast(err?.error?.message || this.L('Could not save profile photo.', 'تعذّر حفظ صورة الملف الشخصي.'));
          },
        });
      },
      error: err => {
        this.avatarUploading.set(false);
        this.showToast(err?.error?.message || this.L('Could not upload image.', 'تعذّر رفع الصورة.'));
      },
    });
  }

  changePassword(): void {
    this.passwordForm = {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    };
    this.isPasswordOpen.set(true);
  }

  closePassword(): void {
    if (!this.passwordLoading()) this.isPasswordOpen.set(false);
  }

  submitPassword(): void {
    const currentPassword = this.passwordForm.currentPassword.trim();
    const newPassword = this.passwordForm.newPassword.trim();
    const confirmPassword = this.passwordForm.confirmPassword.trim();

    if (!currentPassword || !newPassword || !confirmPassword) {
      this.showToast(this.L('Fill all password fields.', 'املأ كل حقول كلمة المرور.'));
      return;
    }

    if (newPassword.length < 6) {
      this.showToast(this.L('New password must be at least 6 characters.', 'كلمة المرور الجديدة يجب ألا تقل عن 6 أحرف.'));
      return;
    }

    if (newPassword !== confirmPassword) {
      this.showToast(this.L('New passwords do not match.', 'كلمتا المرور الجديدتان غير متطابقتين.'));
      return;
    }

    this.passwordLoading.set(true);
    this.auth.changePassword({ currentPassword, newPassword }).subscribe({
      next: res => {
        this.passwordLoading.set(false);
        if (!res.success) {
          this.showToast(res.message || this.L('Could not update password.', 'تعذّر تحديث كلمة المرور.'));
          return;
        }

        this.isPasswordOpen.set(false);
        this.showToast(this.L('Password updated successfully.', 'تم تحديث كلمة المرور بنجاح.'));
      },
      error: err => {
        this.passwordLoading.set(false);
        this.showToast(err?.error?.message || this.L('Could not update password.', 'تعذّر تحديث كلمة المرور.'));
      },
    });
  }

  deleteAccount(): void {
    this.showToast(this.L('Please contact support to delete your account.', 'يرجى التواصل مع الدعم لحذف حسابك.'));
  }

  handleSetting(item: SettingItem): void {
    if (item.id === 'language') this.toggleLanguage();
    else if (item.id === 'password') this.changePassword();
  }

  logout(): void {
    this.auth.logout();
    this.showToast(this.L('You have been logged out.', 'تم تسجيل خروجك.'));
    // replaceUrl so pressing back can't return to the profile page we left.
    this.router.navigate(['/login'], { replaceUrl: true });
  }

  private showToast(msg: string): void {
    this.toastMessage.set(msg);
    setTimeout(() => this.toastMessage.set(null), 3500);
  }

  statusClass(s: OrderStatus): string {
    return {
      Reserved:   'status--processing',
      Completed:  'status--delivered',
      Delivered:  'status--delivered',
      Processing: 'status--processing',
      Shipped:    'status--shipped',
      Cancelled:  'status--cancelled',
    }[s] ?? '';
  }

  /** Localised label for an order status. */
  statusLabel(s: OrderStatus): string {
    return {
      Reserved:   this.L('Reserved', 'محجوز'),
      Completed:  this.L('Completed', 'مكتمل'),
      Delivered:  this.L('Delivered', 'تم التسليم'),
      Processing: this.L('Processing', 'قيد المعالجة'),
      Shipped:    this.L('Shipped', 'تم الشحن'),
      Cancelled:  this.L('Cancelled', 'ملغي'),
    }[s] ?? s;
  }

  /** Localised label for an order filter chip. */
  filterLabel(f: OrderStatus | 'All'): string {
    return f === 'All' ? this.L('All', 'الكل') : this.statusLabel(f);
  }

  get tabs(): { id: 'overview' | 'orders' | 'wishlist' | 'stores' | 'settings'; label: string; icon: string }[] {
    return [
      { id: 'overview',  label: this.L('Overview', 'نظرة عامة'),  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>` },
      { id: 'orders',    label: this.L('Orders', 'الطلبات'),    icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6h15l-2 9H8L6 6Z"/><path d="M6 6 5 3H2"/><circle cx="9" cy="20" r="1"/><circle cx="18" cy="20" r="1"/></svg>` },
      { id: 'wishlist',  label: this.L('Wishlist', 'المفضلة'),  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>` },
      { id: 'stores',    label: this.L('Saved Stores', 'المحلات المحفوظة'),  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>` },
      { id: 'settings',  label: this.L('Settings', 'الإعدادات'),  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-2 2 2 2 0 01-2-2v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83 0 2 2 0 010-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 01-2-2 2 2 0 012-2h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 010-2.83 2 2 0 012.83 0l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 012-2 2 2 0 012 2v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 0 2 2 0 010 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 012 2 2 2 0 01-2 2h-.09a1.65 1.65 0 00-1.51 1z"/></svg>` },
    ];
  }
  
  // Memoised - the template re-evaluates safeSvg() on every change-detection
  // cycle; without a cache each call mints a new SafeHtml and forces an
  // unnecessary innerHTML re-render of every icon.
  private svgCache = new Map<string, SafeHtml>();
  safeSvg(svg: string): SafeHtml {
    let html = this.svgCache.get(svg);
    if (!html) {
      html = this.sanitizer.bypassSecurityTrustHtml(svg);
      this.svgCache.set(svg, html);
    }
    return html;
  }

  private syncUserFromSession(): void {
    const current = this.auth.currentUser();
    if (!current) return;

    const fullName = `${current.firstName} ${current.lastName}`.trim() || current.email;
    this.user = {
      ...this.user,
      fullName,
      email: current.email,
      initials: this.initials(fullName),
    };
  }

  private loadOrders(): void {
    this.ordersLoading.set(true);
    this.ordersError.set('');

    this.orderService.getMine().subscribe({
      next: orders => {
        this.orders = orders.map(o => this.toOrder(o));
        this.ordersLoading.set(false);
      },
      error: () => {
        this.ordersError.set(this.L('Could not load your orders.', 'تعذّر تحميل طلباتك.'));
        this.ordersLoading.set(false);
      },
    });
  }

  private loadProfile(): void {
    this.profileLoading.set(true);
    this.profileError.set('');

    this.auth.getProfile().subscribe({
      next: profile => {
        this.applyProfile(profile);
        this.profileLoading.set(false);
      },
      error: () => {
        this.profileError.set(this.L('Could not load profile details.', 'تعذّر تحميل بيانات الملف الشخصي.'));
        this.profileLoading.set(false);
      },
    });
  }

  private loadWishlist(): void {
    this.engagement.getWishlist().subscribe({
      next: items => this.wishlist = items.map(item => this.toWishlist(item)),
      error: () => this.wishlist = [],
    });
  }

  private loadSavedStores(): void {
    this.savedStoresLoading.set(true);
    this.engagement.getSavedStores().subscribe({
      next: stores => {
        this.savedStores = stores.map(s => this.toSavedStore(s));
        this.savedStoresCount.set(stores.length);
        this.savedStoresLoading.set(false);
      },
      error: () => {
        this.savedStores = [];
        this.savedStoresCount.set(0);
        this.savedStoresLoading.set(false);
      },
    });
  }

  private toSavedStore(s: FollowedStoreDTO): SavedStore {
    return {
      id: s.storeId,
      name: s.name,
      description: s.description ?? '',
      address: s.address ?? '',
      logoUrl: s.logoUrl,
      productCount: s.productCount,
      gradient: this.wishlistGradient(s.storeId),
      initials: this.initials(s.name),
    };
  }

  private loadReviewCount(): void {
    this.engagement.getMyReviewCount().subscribe({
      next: count => this.reviewsCount.set(count),
      error: () => this.reviewsCount.set(0),
    });
  }

  private loadNotificationPreferences(): void {
    this.monetization.getNotificationPreferences().subscribe({
      next: prefs => this.notifications.set(prefs.sponsoredNotificationsEnabled),
      error: () => {},
    });
  }

  private loadAlerts(): void {
    this.alertsLoading.set(true);
    this.monetization.getAlerts().subscribe({
      next: alerts => {
        this.alerts.set(alerts);
        this.alertsLoading.set(false);
      },
      error: () => {
        this.alerts.set([]);
        this.alertsLoading.set(false);
      },
    });
  }

  alertLabel(alert: AlertDTO): string {
    const labels: Record<string, string> = {
      BackInStock: this.L('Back in stock', 'عودة التوفر'),
      PriceDrop: this.L('Price drop', 'انخفاض السعر'),
      NewMatch: this.L('New search match', 'مطابقة بحث جديدة'),
    };
    return labels[alert.type] ?? alert.type;
  }

  alertTarget(alert: AlertDTO): string {
    if (alert.productTitle) return alert.productTitle;
    if (alert.query) return alert.query;
    return this.L('Saved alert', 'تنبيه محفوظ');
  }

  private toProfileUpdateRequest(profile: Partial<UserProfile>): UserProfileUpdateRequest {
    const fullName = (profile.fullName ?? this.user.fullName).trim();
    const parts = fullName.split(/\s+/).filter(Boolean);
    const firstName = parts[0] ?? this.user.fullName;
    const lastName = parts.slice(1).join(' ') || ' ';

    return {
      firstName,
      lastName,
      phoneNumber: (profile.phone ?? this.user.phone) || null,
      address: (profile.address ?? this.user.address) || null,
      gender: (profile.gender ?? this.user.gender) || null,
      location: profile.location ?? this.user.location,
      avatarUrl: this.avatarUrlForUpdate(profile),
    };
  }

  private avatarUrlForUpdate(profile: Partial<UserProfile>): string | null {
    const avatar = (profile.avatar ?? this.user.avatar).trim();
    if (!avatar) return null;
    if (avatar === this.user.avatar && this.avatarStorageUrl) return this.avatarStorageUrl;
    return avatar;
  }

  private applyProfile(profile: UserProfileDTO): void {
    const fullName = `${profile.firstName} ${profile.lastName}`.trim() || profile.email;
    this.avatarStorageUrl = profile.avatarUrl?.trim() ?? '';
    this.user = {
      ...this.user,
      fullName,
      email: profile.email,
      phone: profile.phoneNumber ?? '',
      address: profile.address ?? '',
      gender: profile.gender ?? '',
      location: profile.location ?? 'Egypt',
      memberSince: this.formatDate(profile.createdAt),
      avatar: toMediaUrl(this.avatarStorageUrl),
      initials: this.initials(fullName),
    };
    this.editForm = { ...this.user };
  }

  private toWishlist(item: WishlistItemDTO): WishlistItem {
    return {
      id: item.productId,
      name: item.title,
      store: item.storeName,
      price: item.price,
      currency: 'EGP',
      imageUrl: item.imageUrl,
      gradient: this.wishlistGradient(item.productId),
    };
  }

  private wishlistGradient(id: number): string {
    const gradients = [
      'linear-gradient(135deg, oklch(0.64 0.14 25/0.7), oklch(0.5 0.12 22/0.85))',
      'linear-gradient(135deg, oklch(0.3 0.02 50/0.85), oklch(0.22 0.015 48/0.9))',
      'linear-gradient(135deg, oklch(0.72 0.1 78/0.65), oklch(0.58 0.12 70/0.8))',
      'linear-gradient(135deg, oklch(0.61 0.085 195/0.65), oklch(0.4 0.06 195/0.82))',
    ];
    return gradients[id % gradients.length];
  }

  private toOrder(order: OrderSummaryDTO): Order {
    const itemLabel = order.itemCount === 1
      ? this.L('1 reserved item', 'عنصر محجوز واحد')
      : this.L(`${order.itemCount} reserved items`, `${order.itemCount} عناصر محجوزة`);

    return {
      id: `#DOK-${order.id.toString().padStart(4, '0')}`,
      productName: itemLabel,
      store: order.pickupCode,
      date: this.formatDate(order.createdAt),
      status: this.toStatus(order.status),
      total: order.totalAmount,
      currency: 'EGP',
      quantity: order.itemCount,
    };
  }

  private toStatus(status: string): OrderStatus {
    switch (status) {
      case 'Reserved':
      case 'Completed':
      case 'Cancelled':
        return status;
      default:
        return 'Processing';
    }
  }

  private formatDate(value: string): string {
    if (!value) return '';
    const date = new Date(value);
    // Guard against invalid dates and the .NET DateTime.MinValue (year 0001)
    // the backend sends when a timestamp was never set - avoids "Jan 1, 1".
    if (Number.isNaN(date.getTime()) || date.getFullYear() < 2000) return '';
    return new Intl.DateTimeFormat('en-EG', {
      day: 'numeric',
      month: 'short',
      year: 'numeric',
    }).format(date);
  }

  private initials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    return ((parts[0]?.[0] ?? 'D') + (parts[1]?.[0] ?? '')).toUpperCase();
  }
}
