import { Component, EventEmitter, HostListener, Input, OnInit, Output } from '@angular/core';
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

@Component({
  selector: 'app-sign-up',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './sign-up.html',
  styleUrls: ['./sign-up.css'],
})
export class SignUpComponent implements OnInit {
  @Input() isOpen = false;
  @Output() closeModal = new EventEmitter<void>();
  @Output() goToLogin = new EventEmitter<void>();

  form!: FormGroup;
  showPassword = false;
  showConfirm = false;
  submitted = false;

  constructor(private fb: FormBuilder) {}

  ngOnInit(): void {
    this.form = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.maxLength(50)]],
        lastName: ['', [Validators.required, Validators.maxLength(50)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(100)]],
        password: ['', [Validators.required, Validators.minLength(4), Validators.maxLength(100)]],
        confirmPassword: ['', [Validators.required]],
        phoneNumber: ['', [Validators.pattern(/^[+]?[\d\s\-().]{7,20}$/)]],
        storeName: ['', [Validators.maxLength(100)]],
        role: ['shopper', [Validators.required]],
      },
      { validators: this.matchPasswords() },
    );
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
    return !!(ctrl && ctrl.invalid && (ctrl.dirty || ctrl.touched || this.submitted));
  }

  confirmInvalid(): boolean {
    const ctrl = this.form.get('confirmPassword');
    if (!ctrl || !(ctrl.dirty || ctrl.touched || this.submitted)) return false;
    return ctrl.invalid || this.form.hasError('passwordMismatch');
  }

  selectRole(role: 'shopper' | 'store'): void {
    this.form.get('role')?.setValue(role);
    if (role === 'shopper') {
      this.form.get('storeName')?.reset('');
    }
  }

  close(): void {
    this.closeModal.emit();
  }

  onBackdropClick(event: MouseEvent): void {
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
    if (this.form.invalid) return;

    const { role, confirmPassword, ...dto } = this.form.value;
    if (role === 'shopper') delete dto.storeName;
    if (!dto.phoneNumber) delete dto.phoneNumber;

    console.log('RegisterDTO:', dto);
  }
}
