import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { LucideEye, LucideEyeOff, LucideLoaderCircle, LucideTimer } from '@lucide/angular';
import { finalize } from 'rxjs';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { PlanoraLogoComponent } from '../../shared/planora-logo.component';

@Component({
  selector: 'app-forgot-password-page',
  standalone: true,
  imports: [FormsModule, RouterLink, PlanoraLogoComponent, LucideEye, LucideEyeOff, LucideLoaderCircle, LucideTimer],
  templateUrl: './forgot-password.page.html',
  styleUrl: './forgot-password.page.css',
})
export class ForgotPasswordPage implements OnInit, OnDestroy {
  email = '';
  otpCode = '';
  newPassword = '';
  confirmPassword = '';
  readonly step = signal<'email' | 'otp'>('email');
  readonly busy = signal(false);
  readonly showPassword = signal(false);
  readonly showConfirmPassword = signal(false);
  readonly message = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly secondsRemaining = signal(900);
  private timerInterval: ReturnType<typeof setInterval> | null = null;

  private readonly api = inject(PlanoraApiService);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('code') ?? this.route.snapshot.queryParamMap.get('token');
    const emailParam = this.route.snapshot.queryParamMap.get('email');
    if (emailParam) this.email = emailParam;
    if (code) {
      this.otpCode = code;
      this.step.set('otp');
      this.startCountdown(900);
    }
  }

  ngOnDestroy(): void { this.stopCountdown(); }
  emailValid(): boolean { return !!this.email && this.email.trim().toLowerCase().endsWith('@gmail.com'); }

  sendOtp(): void {
    if (!this.emailValid() || this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    this.api.requestPasswordReset(this.email.trim()).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: (response) => {
        this.step.set('otp');
        if (response.resetToken?.length === 6) this.otpCode = response.resetToken;
        this.startCountdown(this.secondsUntil(response.expiresAt));
        this.message.set('Mã xác nhận 6 số đã được gửi qua email.');
      },
      error: (error) => this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể gửi mã xác nhận.'),
    });
  }

  resendOtp(): void { this.sendOtp(); }
  sanitizeOtp(): void { this.otpCode = this.otpCode.replace(/\D/g, '').slice(0, 6); }

  resetPasswordWithOtp(): void {
    if (!this.canSubmitReset() || this.busy()) return;
    if (this.newPassword !== this.confirmPassword) {
      this.error.set('Hai mật khẩu chưa trùng khớp.');
      return;
    }
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    this.api.resetPassword(this.otpCode.trim(), this.newPassword).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: () => {
        this.stopCountdown();
        this.message.set('Đã đổi mật khẩu thành công! Bạn có thể đăng nhập ngay.');
      },
      error: (error) => this.error.set(error.error?.errors?.[0]?.message ?? 'Mã xác nhận không hợp lệ hoặc đã hết hạn.'),
    });
  }

  canSubmitReset(): boolean {
    return this.otpCode.trim().length >= 6 && this.newPassword.length >= 9 && /[^A-Za-z0-9]/.test(this.newPassword) && this.newPassword === this.confirmPassword && this.secondsRemaining() > 0;
  }

  passwordScore(): number {
    if (!this.newPassword) return 0;
    let score = 1;
    if (this.newPassword.length >= 9) score++;
    if (/[a-z]/.test(this.newPassword) && /[A-Z]/.test(this.newPassword) && /\d/.test(this.newPassword)) score++;
    if (/[^A-Za-z0-9]/.test(this.newPassword)) score++;
    return score;
  }

  passwordLabel(): string {
    const score = this.passwordScore();
    if (score <= 1) return 'Rất yếu';
    if (score === 2) return 'Tạm ổn';
    if (score === 3) return 'Khá mạnh';
    return 'Mạnh';
  }

  formattedTime(): string {
    const seconds = Math.max(0, this.secondsRemaining());
    return `${Math.floor(seconds / 60).toString().padStart(2, '0')}:${(seconds % 60).toString().padStart(2, '0')}`;
  }

  private startCountdown(seconds: number): void {
    this.stopCountdown();
    this.secondsRemaining.set(seconds);
    this.timerInterval = setInterval(() => {
      const current = this.secondsRemaining();
      if (current <= 1) {
        this.secondsRemaining.set(0);
        this.stopCountdown();
      } else {
        this.secondsRemaining.set(current - 1);
      }
    }, 1000);
  }

  private secondsUntil(expiresAt: string | null): number {
    return expiresAt ? Math.max(0, Math.ceil((new Date(expiresAt).getTime() - Date.now()) / 1000)) : 900;
  }

  private stopCountdown(): void {
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }
}
