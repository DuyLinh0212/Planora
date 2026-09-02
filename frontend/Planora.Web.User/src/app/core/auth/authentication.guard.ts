import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthSessionService } from './auth-session.service';
import { hasUsableAccessToken } from './session.store';

export const authenticationGuard: CanActivateFn = (_route, state) => {
  if (hasUsableAccessToken()) return true;
  const router = inject(Router);
  return inject(AuthSessionService).restoreSession().pipe(
    map((restored) => restored || router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } })),
  );
};
