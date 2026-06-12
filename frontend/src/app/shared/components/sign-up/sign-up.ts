// import { Component } from '@angular/core';

// @Component({
//   selector: 'app-sign-up',
//   imports: [],
//   templateUrl: './sign-up.html',
//   styleUrl: './sign-up.css',
// })
// export class SignUp {

// }

import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';

export function passwordStrengthValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value ?? '';
    if (!value) return null;
    const hasUpper = /[A-Z]/.test(value);
    const hasLower = /[a-z]/.test(value);
    const hasDigit = /\d/.test(value);
    const hasMin = value.length >= 8;
    return hasUpper && hasLower && hasDigit && hasMin ? null : { weakPassword: true };
  };
}

export function confirmPasswordValidator(passwordField: string): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const group = control.parent as FormGroup;
    if (!group) return null;
    const password = group.get(passwordField)?.value;
    return control.value === password ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-sign-up',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './sign-up.html',
  styleUrls: ['./sign-up.css'],
})
export class SignUpComponent implements OnInit {
  form!: FormGroup;
  showPassword = false;
  showConfirm = false;
  submitted = false;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group(
      {
        fullName: ['', [Validators.required, Validators.minLength(2)]],
        email: ['', [Validators.required, Validators.email]],
        username: [
          '',
          [Validators.required, Validators.minLength(3), Validators.pattern(/^[a-zA-Z0-9_]+$/)],
        ],
        password: ['', [Validators.required, passwordStrengthValidator()]],
        confirmPassword: ['', [Validators.required]],
        role: ['shopper', [Validators.required]],
      },
      { validators: this.matchPasswordsGroup() },
    );
  }

  private matchPasswordsGroup(): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const pw = group.get('password')?.value;
      const cpw = group.get('confirmPassword')?.value;
      if (!cpw) return null;
      return pw === cpw ? null : { passwordMismatch: true };
    };
  }

  get f() {
    return this.form.controls;
  }

  fieldInvalid(name: string): boolean {
    const ctrl = this.form.get(name);
    return !!(ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched || this.submitted));
  }

  confirmInvalid(): boolean {
    const ctrl = this.form.get('confirmPassword');
    if (!ctrl || !(ctrl.dirty || ctrl.touched || this.submitted)) return false;
    return ctrl.invalid || this.form.hasError('passwordMismatch');
  }

  selectRole(role: 'shopper' | 'store'): void {
    this.form.get('role')?.setValue(role);
  }

  onSubmit(): void {
    this.submitted = true;
    if (this.form.invalid) return;
    console.log('Sign-up payload:', this.form.value);
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
  toggleConfirm(): void {
    this.showConfirm = !this.showConfirm;
  }
}
