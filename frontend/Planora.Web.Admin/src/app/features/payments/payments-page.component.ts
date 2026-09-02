import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';
import { PaymentTransaction, PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-payments-page',
  imports: [DatePipe, FormsModule],
  templateUrl: './payments-page.component.html',
  styleUrl: './payments-page.component.css',
})
export class PaymentsPageComponent {
  readonly context = inject(AdminConsoleContextService);
  private readonly api = inject(PlanoraAdminApiService);
  readonly selectedPayment = signal<PaymentTransaction | null>(null);
  readonly message = signal<string | null>(null);
  provider = '';
  status = '';
  readonly filteredPayments = computed(() =>
    this.context
      .payments()
      .filter(
        (item) =>
          (!this.provider || this.formatProvider(item.provider) === this.provider) &&
          (!this.status || this.formatPaymentStatus(item.status) === this.status),
      ),
  );

  formatProvider(provider: unknown): string {
    if (provider === 0 || provider === '0' || String(provider).toLowerCase() === 'momo') return 'MoMo';
    if (provider === 1 || provider === '1' || String(provider).toLowerCase() === 'zalopay') return 'ZaloPay';
    if (provider === 2 || provider === '2' || String(provider).toLowerCase() === 'banktransfer') return 'Bank Transfer';
    return String(provider ?? 'MoMo');
  }

  formatPaymentStatus(status: unknown): string {
    if (status === 0 || status === '0' || String(status).toLowerCase() === 'pending') return 'Pending';
    if (status === 1 || status === '1' || String(status).toLowerCase() === 'success') return 'Success';
    if (status === 2 || status === '2' || String(status).toLowerCase() === 'failed') return 'Failed';
    return String(status ?? 'Pending');
  }

  formatAmount(amount: number, currency: string): string {
    const cur = currency?.toUpperCase() || 'VND';
    if (cur === 'VND') {
      return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);
    }
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: cur }).format(amount);
  }

  reloadPayments(): void {
    this.api
      .getPaymentTransactions(this.provider || undefined, this.status || undefined)
      .subscribe((response) => this.context.payments.set(response.items));
  }

  markReviewed(payment: PaymentTransaction): void {
    if (payment.id.startsWith('demo-'))
      return this.notify('Preview mode: review maps to the admin payment API.');
    this.api.markPaymentTransactionReviewed(payment.id).subscribe(() => {
      this.context.payments.update((items) =>
        items.map((item) =>
          item.id === payment.id ? { ...item, reviewedAt: new Date().toISOString() } : item,
        ),
      );
      this.selectedPayment.set(null);
      this.notify('Payment marked as reviewed.');
    });
  }

  exportPayments(): void {
    this.download('planora-payments.csv', [
      ['ID', 'User', 'Amount', 'Provider', 'Status'],
      ...this.context
        .payments()
        .map((item) => [item.id, item.userEmail, item.amount, this.formatProvider(item.provider), this.formatPaymentStatus(item.status)]),
    ]);
  }

  private download(name: string, rows: (string | number)[][]): void {
    const url = URL.createObjectURL(
      new Blob([rows.map((row) => row.join(',')).join('\n')], { type: 'text/csv' }),
    );
    const link = document.createElement('a');
    link.href = url;
    link.download = name;
    link.click();
    URL.revokeObjectURL(url);
  }

  private notify(value: string): void {
    this.message.set(value);
    setTimeout(() => this.message.set(null), 2400);
  }
}
