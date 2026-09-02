import { Component, inject } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';

@Component({
  selector: 'app-admin-overview-page',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './admin-overview-page.component.html',
  styleUrl: './admin-overview-page.component.css',
})
export class AdminOverviewPageComponent {
  readonly context = inject(AdminConsoleContextService);

  storageTb(): string {
    return (this.context.overview().aggregateStorageBytes / 1_000_000_000_000).toFixed(2);
  }

  formatRevenue(amount: number): string {
    if (!amount || amount === 0) return '0 ₫';
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(amount);
  }
}
