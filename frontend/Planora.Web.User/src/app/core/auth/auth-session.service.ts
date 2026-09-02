import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, finalize, map, Observable, of, tap } from 'rxjs';
import { PlanoraApiService } from '../api/planora-api.service';
import { clearSession, hasUsableAccessToken, storeSession, updateTokens } from './session.store';
import { AuthenticationResponse } from '../api/api.models';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly api = inject(PlanoraApiService);
  private readonly router = inject(Router);
  readonly sessionExpired = signal(false);

  establish(response: AuthenticationResponse, remember: boolean): void {
    storeSession(response, remember);
  }

  signOut(): void {
    this.api
      .logout()
      .pipe(
        catchError(() => of(undefined)),
        finalize(() => this.clearAndReturnToLogin()),
      )
      .subscribe();
  }

  restoreSession(): Observable<boolean> {
    if (hasUsableAccessToken()) return of(true);
    return this.api.refresh().pipe(
      tap((response) => updateTokens(response)),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  clearAndReturnToLogin(returnUrl?: string): void {
    clearSession();
    void this.router.navigate(['/login'], {
      queryParams: returnUrl ? { returnUrl } : undefined,
      replaceUrl: true,
    });
  }

  expireSession(returnUrl?: string): void {
    if (this.sessionExpired()) return;
    clearSession();
    this.sessionExpired.set(true);
    void this.router.navigate(['/login'], {
      queryParams: returnUrl ? { returnUrl } : undefined,
      replaceUrl: true,
    });
  }

  dismissSessionExpired(): void {
    this.sessionExpired.set(false);
  }
}
