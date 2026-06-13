import { Component } from '@angular/core';
import { SignUpComponent } from './shared/components/sign-up/sign-up';
import { LoginComponent } from './shared/components/login/login';
import { AuthDemoComponent } from "./shared/components/auth-demo/auth-demo";

@Component({
  selector: 'app-root',
  imports: [ AuthDemoComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
