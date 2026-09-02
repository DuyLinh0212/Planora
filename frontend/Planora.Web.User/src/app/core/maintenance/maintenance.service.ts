import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { EMPTY, Subject, catchError, exhaustMap, finalize, startWith, switchMap, takeUntil, timer } from 'rxjs';
import { MaintenanceStatus } from '../api/api.models';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class MaintenanceService {
  private readonly http = inject(HttpClient);
  private readonly refresh = new Subject<void>();
  private readonly stopped = new Subject<void>();
  private started = false;

  readonly status = signal<MaintenanceStatus>({ isEnabled: false, message: '', updatedAt: null });
  readonly checking = signal(false);

  start(): void {
    if (this.started) return;
    this.started = true;
    timer(0, 30_000)
      .pipe(
        switchMap(() => this.refresh.pipe(startWith(undefined))),
        exhaustMap(() => {
          this.checking.set(true);
          return this.http
            .get<MaintenanceStatus>(`${environment.apiUrl}/api/system/maintenance`)
            .pipe(catchError(() => EMPTY), finalize(() => this.checking.set(false)));
        }),
        takeUntil(this.stopped),
      )
      .subscribe({
        next: (status) => {
          this.status.set(status);
        },
      });
  }

  checkNow(): void {
    if (!this.started) this.start();
    else this.refresh.next();
  }
}
