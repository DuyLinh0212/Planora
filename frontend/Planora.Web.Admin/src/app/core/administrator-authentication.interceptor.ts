import { HttpInterceptorFn } from '@angular/common/http';

export const administratorAuthenticationInterceptor: HttpInterceptorFn = (request, next) => {
  const accessToken = globalThis.localStorage?.getItem('planora.admin.accessToken');
  if (!accessToken) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } }));
};
