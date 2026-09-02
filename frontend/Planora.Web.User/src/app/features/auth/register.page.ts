import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LucideCheck, LucideEye, LucideEyeOff, LucideLoaderCircle } from '@lucide/angular';
import { finalize } from 'rxjs';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { PlanoraLogoComponent } from '../../shared/planora-logo.component';

@Component({
  selector: 'app-register-page',
  imports: [FormsModule, RouterLink, PlanoraLogoComponent, LucideCheck, LucideEye, LucideEyeOff, LucideLoaderCircle],
  templateUrl: './register.page.html',
  styleUrl: './register.page.css',
})
export class RegisterPage {
  displayName = '';
  username = '';
  email = '';
  password = '';
  confirmPassword = '';
  acceptedTerms = false;
  readonly showPassword = signal(false);
  readonly showConfirmPassword = signal(false);
  readonly passwordBlurred = signal(false);
  readonly confirmBlurred = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  passwordScore(): number {
    if (!this.password) return 0;
    let score = 1;
    if (this.password.length >= 9) score++;
    if (/[a-z]/.test(this.password) && /[A-Z]/.test(this.password) && /\d/.test(this.password)) score++;
    if (/[^A-Za-z0-9]/.test(this.password)) score++;
    return score;
  }

  passwordValid(): boolean {
    return (
      this.password.length >= 9 &&
      /[a-z]/.test(this.password) &&
      /[A-Z]/.test(this.password) &&
      /\d/.test(this.password) &&
      /[^A-Za-z0-9]/.test(this.password)
    );
  }

  passwordLabel(): string {
    return ['', 'Yếu', 'Trung bình', 'Khá', 'Mạnh'][this.passwordScore()];
  }

  private readonly api = inject(PlanoraApiService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);

  canSubmit(): boolean {
    return this.passwordValid() && this.password === this.confirmPassword && this.acceptedTerms;
  }

  togglePassword(): void {
    this.showPassword.set(!this.showPassword());
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword.set(!this.showConfirmPassword());
  }

  submit(): void {
    this.passwordBlurred.set(true);
    this.confirmBlurred.set(true);
    if (!this.canSubmit() || this.busy()) return;
    this.error.set(null);
    this.busy.set(true);
    this.api
      .register(this.displayName.trim(), this.username.trim(), this.email.trim(), this.password, true)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (response) => {
          this.session.establish(response, true);
          void this.router.navigate(['/projects'], { replaceUrl: true });
        },
        error: (error) =>
          this.error.set(
            error.error?.errors?.[0]?.message ?? error.error?.detail ?? 'Không thể tạo tài khoản.',
          ),
      });
  }
}
