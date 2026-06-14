import { Routes } from '@angular/router';
import { LandingPage } from './pages/landing-page/landing-page';
import { Profile } from './pages/profile/profile';
import { Cart } from './pages/cart/cart';
import { LoginComponent } from './shared/components/login/login';
import { SignUpComponent } from './shared/components/sign-up/sign-up';

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