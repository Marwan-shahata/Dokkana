import { Component, computed, signal } from '@angular/core';
import { Navbar } from './shared/navbar/navbar';
import { Footer } from './shared/footer/footer';
import { LandingPage } from './pages/landing-page/landing-page';
import { Profile } from './pages/profile/profile';
import { RouterOutlet } from '@angular/router';
import { AdminDashboardComponent } from './shared/components/admin-dashboard/admin-dashboard';
import { SignUpComponent } from './shared/components/sign-up/sign-up';
import { LoginComponent } from './shared/components/login/login';
import { AuthDemoComponent } from './shared/components/auth-demo/auth-demo';
import { CommonModule } from '@angular/common';
import { Cart } from "./pages/cart/cart";


@Component({
  selector: 'app-root',
  imports: [RouterOutlet,
    Navbar,
    Footer,
    LoginComponent,
    SignUpComponent,
    CommonModule, Cart],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
showLogin = false;
showSignUp = false;

openLogin() {
  this.showLogin = true;
  this.showSignUp = false;
}

openSignUp() {
  this.showSignUp = true;
  this.showLogin = false;
}

closeLogin() {
  this.showLogin = false;
}

closeSignUp() {
  this.showSignUp = false;
}
switchToSignUp(): void {
    this.showSignUp = true;
    this.showLogin = false;
  }
  switchToLogin(): void {
    this.showLogin = true;
    this.showSignUp = false;
  }


   isCartOpen = false;
  openCart(): void {
    this.isCartOpen = true;
  }
  
  closeCart() {
    this.isCartOpen = false;
  }

}
