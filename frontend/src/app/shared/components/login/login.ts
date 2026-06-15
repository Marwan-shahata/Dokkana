import { Component, EventEmitter, HostListener, Input, OnInit, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LanguageService } from '../../../core/services/language.service';
import { UserRole } from '../../../core/models/auth.models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrls: ['./login.css'],
})
export class LoginComponent implements OnInit {
  @Input() isOpen = false;
  // When true the component renders inline (no fixed full-screen backdrop), so
  // it can be hosted inside the shared AuthPage which owns the background.
  @Input() embedded = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() goToSignUp = new EventEmitter<void>();

  private auth = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private language = inject(LanguageService);

  form!: FormGroup;
  showPassword = false;
  submitted = false;
  isLoading = false;
  errorMessage = '';

  constructor(private fb: FormBuilder) {}

  /** Bilingual string helper - reactive because lang.t() reads the lang signal. */
  L(en: string, ar: string): string {
    return this.language.t(en, ar);
  }

  forgotPassword(): void {
    // No reset flow yet - surface a friendly note instead of a dead link.
    this.errorMessage = this.L(
      'Password reset is coming soon — please contact support.',
      'استعادة كلمة المرور قريبًا — يرجى التواصل مع الدعم.',
    );
  }

  ngOnInit(): void {
    this.form = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(6)]],
      rememberMe: [false],
    });

    // When opened as a routed page (/login) there is no parent to set isOpen,
    // so show the panel automatically. As an embedded modal the [isOpen] binding wins.
    if (this.route.snapshot.routeConfig?.path === 'login') {
      this.isOpen = true;
    }
  }

  // "Sign Up" link: emit when embedded (AuthPage handles the morph) or used as a
  // modal; navigate only on the standalone routed page.
  onGoSignUp(): void {
    if (this.embedded || !this.isRouted) this.goToSignUp.emit();
    else this.router.navigate(['/signup']);
  }

  private get isRouted(): boolean {
    return this.route.snapshot.routeConfig?.path === 'login';
  }

  get f() {
    return this.form.controls;
  }

  fieldInvalid(name: string): boolean {
    const ctrl = this.form.get(name);
    return !!(ctrl && ctrl.invalid && (ctrl.touched || this.submitted));
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

  onSubmit(): void {
    this.submitted = true;
    this.errorMessage = '';
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const { email, password } = this.form.value;
    this.auth.login({ email: email.trim(), password }).subscribe({
      next: res => {
        this.isLoading = false;
        if (!res.success || !res.data) {
          this.errorMessage = res.message || this.L('Login failed. Please try again.', 'فشل تسجيل الدخول. حاول مرة أخرى.');
          return;
        }
        this.close();
        this.redirectAfterAuth(res.data.role);
      },
      error: err => {
        this.isLoading = false;
        this.errorMessage =
          err?.error?.message || this.L('Invalid email or password.', 'بريد إلكتروني أو كلمة مرور غير صحيحة.');
      },
    });
  }

  private redirectAfterAuth(role: UserRole): void {
    const returnUrl = this.safeReturnUrl();
    const target = returnUrl || this.auth.homeRouteForRole(role as never);
    // replaceUrl drops /login from history so the back button can't return to it.
    this.router.navigateByUrl(target, { replaceUrl: true });
  }

  private safeReturnUrl(): string | null {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (!returnUrl) return null;
    if (!returnUrl.startsWith('/') || returnUrl.startsWith('//')) return null;
    if (/^\/(?:login|signup)(?:[/?#]|$)/.test(returnUrl)) return null;
    return returnUrl;
  }
}
