import { Component, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { AuthSessionService } from './core/auth/auth-session.service';
import { QuotaNoticeService } from './core/feedback/quota-notice.service';
import { MaintenanceService } from './core/maintenance/maintenance.service';
import { PlanoraLogoComponent } from './shared/planora-logo.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, PlanoraLogoComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  readonly auth = inject(AuthSessionService);
  readonly quotaNotice = inject(QuotaNoticeService);
  readonly maintenance = inject(MaintenanceService);
  private readonly router = inject(Router);

  constructor() {
    this.maintenance.start();
  }

  openBilling(): void {
    this.quotaNotice.dismiss();
    void this.router.navigate(['/billing']);
  }
}
