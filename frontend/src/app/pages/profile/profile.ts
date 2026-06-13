import {
  Component,
  OnInit,
  signal,
  computed,
} from '@angular/core';

import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

export type OrderStatus = 'Delivered' | 'Processing' | 'Cancelled' | 'Shipped';
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
  constructor(private sanitizer: DomSanitizer, private title: Title) {}

   // ── State ──────────────────────────────────────────────────
  activeTab = signal<'overview' | 'orders' | 'wishlist' | 'settings'>('overview');
  isEditOpen = signal(false);
  darkMode = signal(false);
  notifications = signal(true);
  editForm: Partial<UserProfile> = {};
  orderFilter: OrderStatus | 'All' = 'All';
  toastMessage = signal<string | null>(null);

  // ── User data ──────────────────────────────────────────────
  user: UserProfile = {
    fullName:    'Sara El-Masry',
    email:       'sara.elmasry@gmail.com',
    phone:       '+20 100 234 5678',
    address:     '14 Tahrir Square, Zamalek, Cairo, Egypt',
    gender:      'Female',
    location:    'Cairo, Egypt',
    memberSince: 'March 2022',
    avatar:      '',
    initials:    'SE',
    avatarColor: 'var(--clay)',
  };

  // ── Stats ──────────────────────────────────────────────────
  get stats(): AccountStat[] {
    return [
      {
        label: 'Total Orders', value: this.orders.length, accent: 'var(--clay)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M6 6h15l-2 9H8L6 6Z"/><path d="M6 6 5 3H2"/><circle cx="9" cy="20" r="1"/><circle cx="18" cy="20" r="1"/></svg>`,
      },
      {
        label: 'Wishlist', value: this.wishlist.length, accent: 'var(--hibiscus)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>`,
      },
      {
        label: 'Saved Stores', value: 8, accent: 'var(--nile)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2z"/><polyline points="9 22 9 12 15 12 15 22"/></svg>`,
      },
      {
        label: 'Reviews', value: 12, accent: 'var(--ochre)',
        icon: `<svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z"/></svg>`,
      },
    ];
  }

  // ── Orders ─────────────────────────────────────────────────
  orders: Order[] = [
    { id:'#DOK-0041', productName:'Linen Summer Dress',      store:'Boutique El Nour', date:'2 Jun 2025',  status:'Delivered',  total:480,  currency:'EGP', quantity:1 },
    { id:'#DOK-0038', productName:'Tan Leather Tote',        store:'Cairo Craft',      date:'28 May 2025', status:'Shipped',    total:920,  currency:'EGP', quantity:1 },
    { id:'#DOK-0035', productName:'Floral Midi Dress',       store:'Reem Fashion',     date:'18 May 2025', status:'Processing', total:560,  currency:'EGP', quantity:2 },
    { id:'#DOK-0031', productName:'Egyptian Cotton Shirt',   store:'Delta Weave',      date:'4 May 2025',  status:'Delivered',  total:340,  currency:'EGP', quantity:1 },
    { id:'#DOK-0027', productName:'Canvas Sneakers',         store:'Step & Style',     date:'22 Apr 2025', status:'Delivered',  total:650,  currency:'EGP', quantity:1 },
    { id:'#DOK-0022', productName:'Suede Ankle Boots',       store:'Step & Style',     date:'10 Apr 2025', status:'Cancelled',  total:1100, currency:'EGP', quantity:1 },
    { id:'#DOK-0019', productName:'Woven Crossbody Bag',     store:'Cairo Craft',      date:'2 Apr 2025',  status:'Delivered',  total:430,  currency:'EGP', quantity:1 },
  ];

  get filteredOrders(): Order[] {
    if (this.orderFilter === 'All') return this.orders;
    return this.orders.filter(o => o.status === this.orderFilter);
  }

  readonly orderFilters: (OrderStatus | 'All')[] = ['All', 'Delivered', 'Processing', 'Shipped', 'Cancelled'];

  // ── Wishlist ───────────────────────────────────────────────
  wishlist: WishlistItem[] = [
    { id:1, name:'Silk Slip Dress',       store:'Boutique El Nour', price:760,  currency:'EGP', gradient:'linear-gradient(135deg, oklch(0.64 0.14 25/0.7), oklch(0.5 0.12 22/0.85))' },
    { id:2, name:'Structured Blazer',     store:'Delta Weave',      price:890,  currency:'EGP', gradient:'linear-gradient(135deg, oklch(0.3 0.02 50/0.85), oklch(0.22 0.015 48/0.9))' },
    { id:3, name:'Gold Hoop Earrings',    store:'Nile Jewels',      price:185,  oldPrice:250, currency:'EGP', gradient:'linear-gradient(135deg, oklch(0.72 0.1 78/0.65), oklch(0.58 0.12 70/0.8))' },
    { id:4, name:'Linen Wide Leg Pants',  store:'Reem Fashion',     price:440,  currency:'EGP', gradient:'linear-gradient(135deg, oklch(0.76 0.04 80/0.6), oklch(0.64 0.05 75/0.75))' },
    { id:5, name:'Embroidered Kaftan',    store:'Al-Masry House',   price:1250, currency:'EGP', gradient:'linear-gradient(135deg, oklch(0.54 0.17 22/0.7), oklch(0.44 0.14 18/0.85))' },
    { id:6, name:'Leather Belt Bag',      store:'Cairo Craft',      price:320,  oldPrice:420, currency:'EGP', gradient:'linear-gradient(135deg, oklch(0.5 0.08 48/0.75), oklch(0.38 0.06 44/0.88))' },
  ];

  // ── Settings ───────────────────────────────────────────────
  get settingsItems(): SettingItem[] {
    return [
      {
        id: 'password', label: 'Change Password', action: 'navigate',
        desc: 'Update your account password regularly for security.',
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>`,
      },
      {
        id: 'notifications', label: 'Notification Preferences', action: 'toggle',
        desc: 'Manage email and push notification settings.',
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9M13.73 21a2 2 0 01-3.46 0"/></svg>`,
        toggleValue: this.notifications(),
      },
      {
        id: 'language', label: 'Language Settings', action: 'navigate',
        desc: 'Switch between Arabic and English interface.',
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><circle cx="12" cy="12" r="10"/><line x1="2" y1="12" x2="22" y2="12"/><path d="M12 2a15.3 15.3 0 014 10 15.3 15.3 0 01-4 10 15.3 15.3 0 01-4-10 15.3 15.3 0 014-10z"/></svg>`,
      },
      {
        id: 'darkmode', label: 'Dark Mode', action: 'toggle',
        desc: 'Switch to a darker interface for low-light environments.',
        icon: `<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round"><path d="M21 12.79A9 9 0 1111.21 3 7 7 0 0021 12.79z"/></svg>`,
        toggleValue: this.darkMode(),
      },
    ];
  }

  // ── Lifecycle ──────────────────────────────────────────────
  ngOnInit(): void {
    this.editForm = { ...this.user };
    this.title.setTitle('Dokana | Profile');
  }

  // ── Methods ────────────────────────────────────────────────
  setTab(tab: 'overview' | 'orders' | 'wishlist' | 'settings'): void {
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
    if (this.editForm.fullName) {
      this.user = { ...this.user, ...this.editForm };
      const parts = this.user.fullName.trim().split(' ');
      this.user.initials = (parts[0][0] + (parts[1]?.[0] ?? '')).toUpperCase();
    }
    this.isEditOpen.set(false);
    this.showToast('Profile updated successfully.');
  }

  editProfile(): void {
    this.openEdit();
  }

  removeWishlistItem(id: number): void {
    this.wishlist = this.wishlist.filter(w => w.id !== id);
    this.showToast('Item removed from wishlist.');
  }

  toggleDarkMode(): void {
    this.darkMode.update(v => !v);
  }

  toggleNotifications(): void {
    this.notifications.update(v => !v);
  }

  logout(): void {
    this.showToast('You have been logged out.');
  }

  private showToast(msg: string): void {
    this.toastMessage.set(msg);
    setTimeout(() => this.toastMessage.set(null), 3500);
  }

  starsArray(n: number): boolean[] {
    return Array(5).fill(false).map((_, i) => i < Math.round(n));
  }

  statusClass(s: OrderStatus): string {
    return {
      Delivered:  'status--delivered',
      Processing: 'status--processing',
      Shipped:    'status--shipped',
      Cancelled:  'status--cancelled',
    }[s] ?? '';
  }

  readonly tabs = [
    { id: 'overview',  label: 'Overview',  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>` },
    { id: 'orders',    label: 'Orders',    icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6h15l-2 9H8L6 6Z"/><path d="M6 6 5 3H2"/><circle cx="9" cy="20" r="1"/><circle cx="18" cy="20" r="1"/></svg>` },
    { id: 'wishlist',  label: 'Wishlist',  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M20.84 4.61a5.5 5.5 0 00-7.78 0L12 5.67l-1.06-1.06a5.5 5.5 0 00-7.78 7.78l1.06 1.06L12 21.23l7.78-7.78 1.06-1.06a5.5 5.5 0 000-7.78z"/></svg>` },
    { id: 'settings',  label: 'Settings',  icon: `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-2 2 2 2 0 01-2-2v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83 0 2 2 0 010-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 01-2-2 2 2 0 012-2h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 010-2.83 2 2 0 012.83 0l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 012-2 2 2 0 012 2v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 0 2 2 0 010 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 012 2 2 2 0 01-2 2h-.09a1.65 1.65 0 00-1.51 1z"/></svg>` },
  ] as const;
  
  safeSvg(svg: string): SafeHtml {
  return this.sanitizer.bypassSecurityTrustHtml(svg);
}
}
