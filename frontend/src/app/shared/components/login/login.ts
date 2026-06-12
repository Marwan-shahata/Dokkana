// // import { Component } from '@angular/core';

// // @Component({
// //   selector: 'app-login',
// //   imports: [],
// //   templateUrl: './login.html',
// //   styleUrl: './login.css',
// // })
// // export class Login {

// // }

import { Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class LoginComponent implements OnInit, OnDestroy {
  form!: FormGroup;
  showPassword = false;
  submitted = false;
  isLoading = false;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false],
    });
  }

  ngOnDestroy(): void {}

  get f() {
    return this.form.controls;
  }

  fieldInvalid(name: string): boolean {
    const ctrl = this.form.get(name);
    return !!(ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched || this.submitted));
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.form.invalid) return;

    this.isLoading = true;
    // Simulate async login — replace with your auth service call
    setTimeout(() => {
      this.isLoading = false;
      console.log('Login payload:', this.form.value);
    }, 1800);
  }
}
