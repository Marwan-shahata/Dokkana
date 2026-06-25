import { Component, EventEmitter, HostListener, Input, OnInit, Output, inject } from '@angular/core';
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
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { RegisterRequest } from '../../../core/models/auth.models';

@Component({
  selector: 'app-sign-up',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './sign-up.html',
  styleUrls: ['./sign-up.css'],
})
export class SignUpComponent implements OnInit {
  @Input() isOpen = false;
  // When true the component renders inline (no fixed full-screen backdrop), so
  // it can be hosted inside the shared AuthPage which owns the background.
  @Input() embedded = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() goToLogin = new EventEmitter<void>();

  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private language = inject(LanguageService);

  form!: FormGroup;
  showPassword = false;
  showConfirm = false;
  submitted = false;
  isLoading = false;
  errorMessage = '';

  constructor(private fb: FormBuilder) {}

  /** Bilingual string helper - reactive because lang.t() reads the lang signal. */
  L(en: string, ar: string): string {
    return this.language.t(en, ar);
  }

  ngOnInit(): void {
    this.form = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.maxLength(50)]],
        lastName: ['', [Validators.required, Validators.maxLength(50)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
        password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(100)]],
        confirmPassword: ['', [Validators.required]],
        phoneNumber: ['', [Validators.pattern(/^[+]?[\d\s\-().]{7,20}$/)]],
        storeName: ['', [Validators.maxLength(100)]],
        role: ['shopper', [Validators.required]],
      },
      { validators: this.matchPasswords() },
    );
    this.syncStoreNameValidators();

    // Auto-open when shown as the routed /signup page.
    if (this.isRouted) this.isOpen = true;
  }

  private get isRouted(): boolean {
    return this.route.snapshot.routeConfig?.path === 'signup';
  }

  // "Sign In" link: emit when embedded (AuthPage handles the morph) or used as a
  // modal; navigate only on the standalone routed page.
  onGoLogin(): void {
    if (this.embedded || !this.isRouted) this.goToLogin.emit();
    else this.router.navigate(['/login']);
  }

  private matchPasswords(): ValidatorFn {
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

  get isStore(): boolean {
    return this.form.get('role')?.value === 'store';
  }

  fieldInvalid(name: string): boolean {
    const ctrl = this.form.get(name);
    return !!(ctrl && ctrl.invalid && (ctrl.touched || this.submitted));
  }

  confirmInvalid(): boolean {
    const ctrl = this.form.get('confirmPassword');
    if (!ctrl || !(ctrl.touched || this.submitted)) return false;
    return ctrl.invalid || this.form.hasError('passwordMismatch');
  }

  selectRole(role: 'shopper' | 'store'): void {
    this.form.get('role')?.setValue(role);
    const storeName = this.form.get('storeName');
    if (role === 'shopper') {
      storeName?.reset('', { emitEvent: false });
    }
    this.syncStoreNameValidators();
  }

  private syncStoreNameValidators(): void {
    const storeName = this.form?.get('storeName');
    if (!storeName) return;
    const validators = [Validators.maxLength(100)];
    if (this.isStore) validators.unshift(Validators.required);
    storeName.setValidators(validators);
    storeName.updateValueAndValidity({ emitEvent: false });
  }

  close(): void {
    this.closeModal.emit();
  }

  onBackdropClick(event: MouseEvent): void {
    if (this.embedded) return;
    if ((event.target as HTMLElement).classList.contains('modal-backdrop')) {
      this.close();
    }
  }

  @HostListener('document:keydown.escape')
  onEsc(): void {
    if (this.isOpen) this.close();
  }

  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }
  toggleConfirm(): void {
    this.showConfirm = !this.showConfirm;
  }

  onSubmit(): void {
    this.submitted = true;
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { role, confirmPassword, ...raw } = this.form.getRawValue();
    const dto: RegisterRequest = {
      firstName: raw.firstName.trim(),
      lastName: raw.lastName.trim(),
      email: raw.email.trim(),
      password: raw.password,
    };
    const phoneNumber = raw.phoneNumber?.trim();
    if (phoneNumber) dto.phoneNumber = phoneNumber;
    if (role === 'store') dto.storeName = raw.storeName.trim();

    this.isLoading = true;
    this.auth.register(dto).subscribe({
      next: res => {
        this.isLoading = false;
        if (!res.success || !res.data) {
          this.errorMessage = res.message || this.L('Registration failed. Please try again.', 'فشل إنشاء الحساب. حاول مرة أخرى.');
          return;
        }
        this.close();
        // replaceUrl drops /signup from history so back can't return to it.
        this.router.navigateByUrl(this.auth.homeRouteForRole(res.data.role), { replaceUrl: true });
      },
      error: err => {
        this.isLoading = false;
        this.errorMessage =
          err?.error?.message || this.L('Registration failed. Please try again.', 'فشل إنشاء الحساب. حاول مرة أخرى.');
      },
    });
  }
}
