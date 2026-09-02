import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Observable, catchError, finalize, shareReplay, switchMap, throwError } from 'rxjs';
import { AuthenticationResponse } from '../api/api.models';
import { PlanoraApiService } from '../api/planora-api.service';
import { AuthSessionService } from './auth-session.service';
import { accessToken, updateTokens } from './session.store';

let refreshRequest: Observable<AuthenticationResponse> | null = null;

export const authenticationInterceptor: HttpInterceptorFn = (request, next) => {
  const token = accessToken();
  const authenticatedRequest = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = request.url.includes('/api/auth/');
      if (error.status !== 401 || isAuthEndpoint) return throwError(() => error);

      const api = inject(PlanoraApiService);
      const session = inject(AuthSessionService);
      refreshRequest ??= api.refresh().pipe(
        shareReplay(1),
        finalize(() => (refreshRequest = null)),
      );

      return refreshRequest.pipe(
        switchMap((response) => {
          updateTokens(response);
          return next(
            request.clone({ setHeaders: { Authorization: `Bearer ${response.accessToken}` } }),
          );
        }),
        catchError((refreshError) => {
          session.expireSession(location.pathname + location.search);
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
