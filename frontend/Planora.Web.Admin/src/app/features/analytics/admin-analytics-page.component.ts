import { Component, inject } from '@angular/core';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';

@Component({
  selector: 'app-admin-analytics-page',
  templateUrl: './admin-analytics-page.component.html',
  styleUrl: './admin-analytics-page.component.css',
})
export class AdminAnalyticsPageComponent {
  readonly context = inject(AdminConsoleContextService);
  readonly cells = Array.from({ length: 84 }, (_, index) => (index % 12) * 6);
  projectMetrics = () =>
    this.context.analytics()?.projectsByStatus ?? this.context.overview().projectStatusDistribution;
  paymentMetrics = () =>
    this.context.analytics()?.paymentsByStatus ?? [
      { label: 'Success', value: 97 },
      { label: 'Pending', value: 2 },
      { label: 'Failed', value: 1 },
    ];
  paymentTotal = () => this.paymentMetrics().reduce((sum, item) => sum + item.value, 0);
  percent(value: number, total: number): number {
    return total ? (value / total) * 100 : 0;
  }
  exportAnalytics(): void {
    const rows = [
      ['Metric', 'Value'],
      ...this.projectMetrics().map((item) => [item.label, item.value]),
    ];
    const url = URL.createObjectURL(
      new Blob([rows.map((row) => row.join(',')).join('\n')], { type: 'text/csv' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = 'planora-admin-analytics.csv';
    link.click();
    URL.revokeObjectURL(url);
  }
}
