import { Component, computed, signal } from '@angular/core';
import { Navbar } from './shared/navbar/navbar';
import { Footer } from './shared/footer/footer';
import { LandingPage } from './pages/landing-page/landing-page';
import { Profile } from './pages/profile/profile';
import { RouterOutlet } from '@angular/router';


@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Navbar, Footer],
import { Component } from '@angular/core';
import { SignUpComponent } from './shared/components/sign-up/sign-up';
import { LoginComponent } from './shared/components/login/login';
import { AuthDemoComponent } from './shared/components/auth-demo/auth-demo';
import { AdminDashboardComponent } from './shared/components/admin-dashboard/admin-dashboard';

@Component({
  selector: 'app-root',
  imports: [AdminDashboardComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  
}
export class App {}
