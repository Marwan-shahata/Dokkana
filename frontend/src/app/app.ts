import { Component, computed, signal } from '@angular/core';

type NavItem = {
  label: string;
  hasMenu: boolean;
};

type Feature = {
  title: string;
  text: string;
};

type Product = {
  category: string;
  name: string;
  price: number;
  visual: string;
};

type Testimonial = {
  name: string;
  role: string;
  text: string;
  initials: string;
  theme: string;
};

type CartItem = {
  name: string;
  price: number;
  quantity: number;
};

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly navItems = signal<NavItem[]>([
    { label: 'Women', hasMenu: true },
    { label: 'Men', hasMenu: false },
    { label: 'Stores', hasMenu: false },
    { label: 'Contact', hasMenu: false }
  ]);

  protected readonly features = signal<Feature[]>([
    {
      title: 'Local Brands',
      text: 'Discover unique fashion from local designers and boutiques in your area.'
    },
    {
      title: 'Quality Assured',
      text: 'Every store is vetted so you can shop with confidence and trust.'
    },
    {
      title: 'Easy Shopping',
      text: 'Browse, order, and get your favorite styles delivered right to your door.'
    }
  ]);

  protected readonly productTabs = signal(['All', 'Dresses', 'Shoes', 'Bags']);

  protected readonly activeTab = signal(this.productTabs()[0]);

  protected readonly allProducts = signal<Product[]>([
    { category: 'Dresses', name: 'Summer Floral Dress', price: 89, visual: 'chair-one' },
    { category: 'Shoes', name: 'Leather Sneakers', price: 129, visual: 'chair-two' },
    { category: 'Bags', name: 'Tote Bag', price: 59, visual: 'chair-three' },
    { category: 'Dresses', name: 'Evening Gown', price: 199, visual: 'chair-four' },
    { category: 'Shoes', name: 'Running Sneakers', price: 159, visual: 'chair-one' },
    { category: 'Bags', name: 'Crossbody Bag', price: 79, visual: 'chair-two' },
    { category: 'Dresses', name: 'Casual Maxi Dress', price: 109, visual: 'chair-three' },
    { category: 'Shoes', name: 'Casual Loafers', price: 99, visual: 'chair-four' }
  ]);

  protected readonly productsPerPage = 4;

  protected readonly filteredProducts = computed(() => {
    const tab = this.activeTab();
    if (tab === 'All') return this.allProducts();
    return this.allProducts().filter(p => p.category === tab);
  });

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filteredProducts().length / this.productsPerPage))
  );

  protected readonly productIndex = signal(0);

  protected readonly pagedProducts = computed(() => {
    const start = this.productIndex() * this.productsPerPage;
    return this.filteredProducts().slice(start, start + this.productsPerPage);
  });

  protected prevProducts() {
    this.productIndex.update(i => Math.max(0, i - 1));
  }

  protected nextProducts() {
    this.productIndex.update(i => Math.min(this.totalPages() - 1, i + 1));
  }

  protected readonly hasPrev = computed(() => this.productIndex() > 0);
  protected readonly hasNext = computed(() => this.productIndex() < this.totalPages() - 1);

  // Product slider — update tab resets page
  protected selectTab(tab: string) {
    this.activeTab.set(tab);
    this.productIndex.set(0);
  }

  // Review slider
  protected readonly allTestimonials = signal<Testimonial[]>([
    {
      name: 'Sarah Ahmed',
      role: 'Fashion Blogger',
      text: '"Dokkana made it so easy to find unique local boutiques. I discovered my new favorite store!"',
      initials: 'SA',
      theme: 'review-bright'
    },
    {
      name: 'Mona Hany',
      role: 'Store Owner',
      text: '"Being on Dokkana has helped me reach customers who truly appreciate handmade fashion."',
      initials: 'MH',
      theme: 'review-dark'
    },
    {
      name: 'Omar Khaled',
      role: 'Shopper',
      text: '"Great prices, fast delivery, and amazing quality. Highly recommend!"',
      initials: 'OK',
      theme: 'review-warm'
    },
    {
      name: 'Nour Ali',
      role: 'Fashion Designer',
      text: '"Dokkana connects me with customers who value quality handmade pieces."',
      initials: 'NA',
      theme: 'review-bright'
    },
    {
      name: 'Laila Youssef',
      role: 'Stylist',
      text: '"I recommend Dokkana to all my clients looking for unique local fashion."',
      initials: 'LY',
      theme: 'review-dark'
    }
  ]);

  protected readonly reviewsPerPage = 3;

  protected readonly reviewIndex = signal(0);

  protected readonly totalReviewPages = computed(() =>
    Math.max(1, Math.ceil(this.allTestimonials().length / this.reviewsPerPage))
  );

  protected readonly pagedReviews = computed(() => {
    const start = this.reviewIndex() * this.reviewsPerPage;
    return this.allTestimonials().slice(start, start + this.reviewsPerPage);
  });

  protected prevReviews() {
    this.reviewIndex.update(i => Math.max(0, i - 1));
  }

  protected nextReviews() {
    this.reviewIndex.update(i => Math.min(this.totalReviewPages() - 1, i + 1));
  }

  protected readonly hasPrevReview = computed(() => this.reviewIndex() > 0);
  protected readonly hasNextReview = computed(() => this.reviewIndex() < this.totalReviewPages() - 1);

  // ── Cart ────────────────────────────────────────────────────────────────────

  protected readonly cart = signal<CartItem[]>([]);

  protected readonly cartOpen = signal(false);

  protected toggleCart() {
    this.cartOpen.update(v => !v);
  }

  protected readonly cartCount = computed(() =>
    this.cart().reduce((sum, item) => sum + item.quantity, 0)
  );

  protected readonly cartTotal = computed(() =>
    this.cart().reduce((sum, item) => sum + item.price * item.quantity, 0)
  );

  protected addToCart(product: Product) {
    this.cart.update(items => {
      const existing = items.find(i => i.name === product.name);
      if (existing) {
        return items.map(i =>
          i.name === product.name ? { ...i, quantity: i.quantity + 1 } : i
        );
      }
      return [...items, { name: product.name, price: product.price, quantity: 1 }];
    });
  }

  protected removeFromCart(name: string) {
    this.cart.update(items => items.filter(i => i.name !== name));
  }

  protected onSearch(event: Event) {
    event.preventDefault();
    const input = (event.target as HTMLFormElement).querySelector('input')!;
    console.log('Search:', input.value);
  }
}
