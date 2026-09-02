import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const administratorAuthenticationGuard: CanActivateFn = () => {
  const hasAdministratorSession = Boolean(localStorage.getItem('planora.admin.accessToken'));
  const hasExplicitPreviewSession = localStorage.getItem('planora.admin.preview') === 'true';
  return hasAdministratorSession || hasExplicitPreviewSession
    ? true
    : inject(Router).createUrlTree(['/login']);
};
