import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { MaintenanceService } from './maintenance.service';

export const maintenanceInterceptor: HttpInterceptorFn = (request, next) => {
  const maintenance = inject(MaintenanceService);
  return next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 503 || error.error?.code === 'system.maintenance') maintenance.checkNow();
      return throwError(() => error);
    }),
  );
};
