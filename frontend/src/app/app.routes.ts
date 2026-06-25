import { Routes } from '@angular/router';
import { LandingPage } from './pages/landing-page/landing-page';
import { AuthPage } from './pages/auth/auth-page';
import { StoresPage } from './features/stores/stores';
import { StoreDetailPage } from './features/stores/store-detail';
import { ProductDetailPage } from './features/products/product-detail';
import { roleGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', component: LandingPage },
  { path: 'login', component: AuthPage, canActivate: [guestGuard], data: { mode: 'login' } },
  { path: 'signup', component: AuthPage, canActivate: [guestGuard], data: { mode: 'signup' } },
  { path: 'stores', component: StoresPage },
  { path: 'stores/:id', component: StoreDetailPage },
  { path: 'products/:id', component: ProductDetailPage },
  {
    path: 'ai-search',
    loadComponent: () => import('./features/ai-search/ai-search').then(m => m.AiSearchPage),
  },
  {
    path: 'discover',
    loadComponent: () => import('./pages/discover/discover').then(m => m.DiscoverPage),
    canActivate: [roleGuard],
    data: { roles: ['Shopper'] },
  },
  {
    path: 'profile',
    loadComponent: () => import('./pages/profile/profile').then(m => m.Profile),
    canActivate: [roleGuard],
    data: { roles: ['Shopper'] },
  },
  {
    path: 'admin',
    loadComponent: () => import('./shared/components/admin-dashboard/admin-dashboard').then(m => m.AdminDashboardComponent),
    canActivate: [roleGuard],
    data: { roles: ['Admin'] },
  },
  {
    path: 'merchant',
    loadComponent: () => import('./features/merchant/merchant-dashboard').then(m => m.MerchantDashboard),
    canActivate: [roleGuard],
    data: { roles: ['StoreOwner'] },
  },
  {
    path: '**',
    loadComponent: () => import('./pages/not-found/not-found').then(m => m.NotFound),
  },
];
