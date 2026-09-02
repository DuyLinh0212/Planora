import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { PlanoraAdminApiService } from '../../core/planora-admin-api.service';
import { AdminAuthSessionService } from '../../core/admin-auth-session.service';

@Component({
  selector: 'app-admin-login-page',
  imports: [FormsModule, RouterLink],
  templateUrl: './admin-login-page.component.html',
  styleUrl: './admin-login-page.component.css',
})
export class AdminLoginPageComponent {
  private readonly api = inject(PlanoraAdminApiService);
  private readonly session = inject(AdminAuthSessionService);
  private readonly router = inject(Router);
  email = 'admin@planora.com';
  password = 'planora-demo';
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  loginAdministrator(): void {
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.api
      .loginAdministrator(this.email, this.password)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: (response) => {
          this.session.storeAdministratorAuthentication(response);
          void this.router.navigateByUrl('/overview');
        },
        error: (err) =>
          this.errorMessage.set(
            err.error?.errors?.[0]?.message ||
            err.error?.title ||
            'Access denied. Verify the API connection and administrator credentials.',
          ),
      });
  }
  previewConsole(): void {
    localStorage.setItem('planora.admin.preview', 'true');
    void this.router.navigateByUrl('/overview');
  }
}
