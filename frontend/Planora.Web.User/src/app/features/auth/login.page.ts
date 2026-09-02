import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { LucideEye, LucideEyeOff, LucideLoaderCircle } from '@lucide/angular';
import { finalize } from 'rxjs';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { accessToken } from '../../core/auth/session.store';
import { PlanoraLogoComponent } from '../../shared/planora-logo.component';
import { GoogleIdentityService } from './google-identity.service';

@Component({
  selector: 'app-login-page',
  imports: [FormsModule, RouterLink, PlanoraLogoComponent, LucideEye, LucideEyeOff, LucideLoaderCircle],
  templateUrl: './login.page.html',
  styleUrl: './login.page.css',
})
export class LoginPage {
  identifier = '';
  password = '';
  remember = false;
  readonly showPassword = signal(false);
  readonly busy = signal(false);
  readonly googleBusy = signal(false);
  readonly error = signal<string | null>(null);

  private readonly api = inject(PlanoraApiService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly google = inject(GoogleIdentityService);

  constructor() {
    if (accessToken()) void this.router.navigate(['/projects'], { replaceUrl: true });
  }

  submit(): void {
    if (!this.identifier.trim() || !this.password || this.busy()) return;
    this.error.set(null);
    this.busy.set(true);
    this.api
      .login(this.identifier.trim(), this.password, this.remember)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (response) => this.completeLogin(response),
        error: (error) => {
          this.error.set(
            error.status === 429
              ? 'Bạn đã thử quá nhiều lần. Chờ một lát rồi đăng nhập lại.'
              : (error.error?.errors?.[0]?.message ??
                  error.error?.detail ??
                  'Email, tên đăng nhập hoặc mật khẩu không đúng.'),
          );
        },
      });
  }

  async loginWithGoogle(): Promise<void> {
    this.error.set(null);
    this.googleBusy.set(true);
    try {
      const token = await this.google.requestIdToken();
      this.api
        .externalLogin('google', token, this.remember)
        .pipe(finalize(() => this.googleBusy.set(false)))
        .subscribe({
          next: (response) => this.completeLogin(response),
          error: (error) =>
            this.error.set(error.error?.errors?.[0]?.message ?? 'Google Login không thành công.'),
        });
    } catch (error) {
      this.googleBusy.set(false);
      this.error.set(error instanceof Error ? error.message : 'Google Login không thành công.');
    }
  }

  togglePassword(): void {
    this.showPassword.set(!this.showPassword());
  }

  private completeLogin(response: Parameters<AuthSessionService['establish']>[0]): void {
    this.session.establish(response, this.remember);
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    void this.router.navigateByUrl(returnUrl?.startsWith('/') ? returnUrl : '/projects', {
      replaceUrl: true,
    });
  }
}
