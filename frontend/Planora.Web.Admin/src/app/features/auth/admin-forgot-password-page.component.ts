import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-admin-forgot-password-page',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-forgot-password-page.component.html',
  styleUrl: './admin-forgot-password-page.component.css',
})
export class AdminForgotPasswordPageComponent {
  private readonly api = inject(PlanoraAdminApiService);
  email = '';
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly developmentResetToken = signal<string | null>(null);

  requestAdministratorPasswordReset(): void {
    this.errorMessage.set(null); this.submitting.set(true);
    this.api.requestPasswordReset(this.email).pipe(finalize(() => this.submitting.set(false))).subscribe({
      next: (response) => { this.successMessage.set(response.message); this.developmentResetToken.set(response.resetToken); },
      error: () => this.errorMessage.set('Recovery service is unavailable. Try again shortly.'),
    });
  }
}
