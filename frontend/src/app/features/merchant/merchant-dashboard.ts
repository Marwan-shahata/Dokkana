import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Title } from '@angular/platform-browser';
import { Category, ProductListItem, Store } from '../../core/models/catalog.models';
import { AuthService } from '../../core/services/auth.service';
import { CategoryService } from '../../core/services/category.service';
import { LanguageService } from '../../core/services/language.service';
import { ProductService } from '../../core/services/product.service';
import { StoreService } from '../../core/services/store.service';
import { UploadService } from '../../core/services/upload.service';

type MerchantTab = 'inventory' | 'store' | 'catalog' | 'copilot';
type StockState = 'in' | 'low' | 'out';

interface StoreForm {
  name: string;
  description: string;
  logoUrl: string;
  address: string;
  latitude: number | null;
  longitude: number | null;
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
  imports: [CommonModule, FormsModule],
  templateUrl: './merchant-dashboard.html',
  styleUrl: './merchant-dashboard.css',
})
export class MerchantDashboard implements OnInit {
  private auth = inject(AuthService);
  private stores = inject(StoreService);
  private products = inject(ProductService);
  private categoriesApi = inject(CategoryService);
  private uploads = inject(UploadService);
  private title = inject(Title);
  readonly lang = inject(LanguageService);

  readonly tabs: { id: MerchantTab; en: string; ar: string }[] = [
    { id: 'inventory', en: 'Inventory', ar: 'المخزون' },
    { id: 'store', en: 'Store profile', ar: 'ملف المحل' },
    { id: 'catalog', en: 'Catalog upload', ar: 'رفع الكتالوج' },
    { id: 'copilot', en: 'Business Copilot', ar: 'مساعد الأعمال' },
  ];

  readonly activeTab = signal<MerchantTab>('inventory');
  readonly loading = signal(true);
  readonly savingStore = signal(false);
  readonly savingProduct = signal(false);
  readonly uploadingProductImage = signal(false);
  readonly uploadingStoreLogo = signal(false);
  readonly deletingProductId = signal<number | null>(null);
  readonly store = signal<Store | null>(null);
  readonly productsList = signal<ProductListItem[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly error = signal('');
  readonly notice = signal('');
  readonly editingProductId = signal<number | null>(null);

  storeForm: StoreForm = emptyStoreForm();
  productForm: ProductForm = emptyProductForm();

  readonly currentUser = computed(() => this.auth.currentUser());
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

  ngOnInit(): void {
    this.title.setTitle(this.L('Dokkana | Merchant Portal', 'دكانة | بوابة التاجر'));
    this.loadCategories();
    this.loadStore();
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
      address: payload.address,
      latitude: payload.latitude,
      longitude: payload.longitude,
    }).subscribe({
      next: () => this.afterStoreSaved(this.L('Store profile created.', 'تم إنشاء ملف المحل.')),
      error: (err: unknown) => this.afterStoreSaveFailed(err),
    });
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
    this.activeTab.set('inventory');
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

  private loadStore(showLoader = true): void {
    if (showLoader) this.loading.set(true);
    this.error.set('');

    this.stores.getMy().subscribe({
      next: store => {
        this.store.set(store);
        this.storeForm = this.toStoreForm(store);
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

  private toStoreForm(store: Store): StoreForm {
    return {
      name: store.name,
      description: store.description ?? '',
      logoUrl: store.logoUrl ?? '',
      address: store.address ?? '',
      latitude: store.latitude ?? null,
      longitude: store.longitude ?? null,
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
    const maybe = err as { error?: { message?: string }; message?: string };
    return maybe?.error?.message ?? maybe?.message ?? '';
  }
}
