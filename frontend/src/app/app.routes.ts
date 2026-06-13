import { Routes } from '@angular/router';
import { LandingPage } from './pages/landing-page/landing-page';
import { Profile } from './pages/profile/profile';
import { Cart } from './pages/cart/cart';

export const routes: Routes = [
  {
    path: '',
    component: LandingPage
  },
  {
    path: 'profile',
    component: Profile
  },
  {
    path: 'cart',
    component: Cart
  }
];