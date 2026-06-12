import { Component } from '@angular/core';
import { SignUpComponent } from './shared/components/sign-up/sign-up';
import { LoginComponent } from './shared/components/login/login';

@Component({
  selector: 'app-root',
  imports: [LoginComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {}
