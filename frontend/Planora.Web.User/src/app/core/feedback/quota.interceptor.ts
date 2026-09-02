import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { QuotaNoticeService } from './quota-notice.service';

export const quotaInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: HttpErrorResponse) => {
      inject(QuotaNoticeService).showApiError(error);
      return throwError(() => error);
    }),
  );
