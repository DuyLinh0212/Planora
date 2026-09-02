import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-admin-reset-password-page',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-reset-password-page.component.html',
  styleUrl: './admin-reset-password-page.component.css',
})
export class AdminResetPasswordPageComponent {
  private readonly api = inject(PlanoraAdminApiService);
  private readonly token = inject(ActivatedRoute).snapshot.queryParamMap.get('token') ?? '';
  newPassword = '';
  confirmPassword = '';
  readonly submitting = signal(false);
  readonly completed = signal(false);
  readonly errorMessage = signal<string | null>(this.token ? null : 'This reset link is missing its token.');

  resetAdministratorPassword(): void {
    this.errorMessage.set(null);
    if (!this.token) { this.errorMessage.set('This reset link is invalid.'); return; }
    if (this.newPassword.length < 10) { this.errorMessage.set('Password must contain at least 10 characters.'); return; }
    if (this.newPassword !== this.confirmPassword) { this.errorMessage.set('Passwords do not match.'); return; }
    this.submitting.set(true);
    this.api.resetPassword(this.token, this.newPassword).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: () => this.completed.set(true),
      error: () => this.errorMessage.set('This reset link is invalid or has expired.'),
    });
  }
}
