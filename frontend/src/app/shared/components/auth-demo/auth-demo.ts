import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LoginComponent } from '../login/login';
import { SignUpComponent } from '../sign-up/sign-up';

@Component({
  selector: 'app-auth-demo',
  standalone: true,
  imports: [CommonModule, LoginComponent, SignUpComponent],
  templateUrl: './auth-demo.html',
  styleUrls: ['./auth-demo.css'],
})
export class AuthDemoComponent {
  showLogin = false;
  showSignUp = false;

  openLogin(): void {
    this.showLogin = true;
    this.showSignUp = false;
  }
  openSignUp(): void {
    this.showSignUp = true;
    this.showLogin = false;
  }
  closeLogin(): void {
    this.showLogin = false;
  }
  closeSignUp(): void {
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
}
