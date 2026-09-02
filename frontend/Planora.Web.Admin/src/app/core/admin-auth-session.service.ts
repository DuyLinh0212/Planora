import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, of } from 'rxjs';
import { AdminAuthenticationResponse, PlanoraAdminApiService } from './planora-admin-api.service';

@Injectable({ providedIn: 'root' })
export class AdminAuthSessionService {
  private readonly api = inject(PlanoraAdminApiService);
  private readonly router = inject(Router);

  storeAdministratorAuthentication(response: AdminAuthenticationResponse): void {
    localStorage.removeItem('planora.admin.preview');
    localStorage.setItem('planora.admin.accessToken', response.accessToken);
    localStorage.setItem('planora.admin.refreshToken', response.refreshToken);
    localStorage.setItem('planora.admin.userId', response.userId);
    localStorage.setItem('planora.admin.displayName', response.displayName);
  }

  logoutCurrentAdministrator(): void {
    const refreshToken = localStorage.getItem('planora.admin.refreshToken');
    const request = refreshToken
      ? this.api.logoutAdministrator(refreshToken).pipe(catchError(() => of(undefined)))
      : of(undefined);
    request.pipe(finalize(() => this.clearSessionAndReturnToLogin())).subscribe();
  }

  clearSessionAndReturnToLogin(): void {
    localStorage.removeItem('planora.admin.accessToken');
    localStorage.removeItem('planora.admin.refreshToken');
    localStorage.removeItem('planora.admin.userId');
    localStorage.removeItem('planora.admin.displayName');
    localStorage.removeItem('planora.admin.preview');
    void this.router.navigateByUrl('/login');
  }
}
