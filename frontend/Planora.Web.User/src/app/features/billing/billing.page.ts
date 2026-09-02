import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { LucideCheck, LucideChevronDown, LucideCreditCard, LucideDatabase, LucideReceiptText, LucideSparkles, LucideX } from '@lucide/angular';
import { finalize, Subscription, switchMap, take, timer } from 'rxjs';
import { AvailablePlan, BankTransferInstructions, UserPayment } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

@Component({
  selector: 'app-billing-page',
  imports: [DatePipe, DecimalPipe, LucideCheck, LucideChevronDown, LucideCreditCard, LucideDatabase, LucideReceiptText, LucideSparkles, LucideX],
  templateUrl: './billing.page.html',
  styleUrl: './billing.page.css',
})
export class BillingPage implements OnInit, OnDestroy {
  readonly store = inject(WorkspaceStore);
  readonly plans = signal<AvailablePlan[]>([]);
  readonly payments = signal<UserPayment[]>([]);
  readonly checkoutPlan = signal<AvailablePlan | null>(null);
  readonly plansLoading = signal(true);
  readonly plansError = signal<string | null>(null);
  readonly paymentsError = signal<string | null>(null);
  readonly historyOpen = signal(false);
  readonly historyLoaded = signal(false);
  readonly busy = signal(false);
  readonly toast = signal<string | null>(null);
  readonly bankTransferInstructions = signal<BankTransferInstructions | null>(null);
  private readonly api = inject(PlanoraApiService);
  private bankTransferWatcher?: Subscription;

  ngOnInit(): void {
    this.store.refreshProfile();
    this.loadPlans();
  }

  ngOnDestroy(): void {
    this.bankTransferWatcher?.unsubscribe();
  }

  loadPlans(): void {
    this.plansLoading.set(true);
    this.plansError.set(null);
    this.api.getPlans().pipe(finalize(() => this.plansLoading.set(false))).subscribe({
      next: (plans) => this.plans.set(plans),
      error: () => this.plansError.set('Không thể kết nối API bảng giá.'),
    });
  }

  toggleHistory(): void {
    const next = !this.historyOpen();
    this.historyOpen.set(next);
    if (next && !this.historyLoaded()) this.loadPayments();
  }

  loadPayments(): void {
    this.historyLoaded.set(true);
    this.paymentsError.set(null);
    this.api.getPayments().subscribe({
      next: (payments) => this.payments.set(payments),
      error: () => this.paymentsError.set('Không thể tải lịch sử giao dịch lúc này. Bạn có thể thử lại bằng cách đóng và mở lại mục này.'),
    });
  }

  periodLabel(period: string): string {
    return ({ Forever: 'Miễn phí vĩnh viễn', Monthly: 'Theo tháng', Yearly: 'Theo năm' } as Record<string, string>)[period] ?? 'Gói dịch vụ';
  }

  providerLabel(provider: string): string {
    return ({ Momo: 'MoMo', ZaloPay: 'ZaloPay', BankTransfer: 'Chuyển khoản' } as Record<string, string>)[provider] ?? 'Chưa xác định';
  }

  paymentStatusLabel(status: string): string {
    return ({ Pending: 'Đang chờ', Success: 'Đã thanh toán', Failed: 'Không thành công' } as Record<string, string>)[status] ?? 'Đang xử lý';
  }

  storagePercent(): number {
    const quota = this.store.profile().quota;
    return quota.maxStorageBytes ? Math.min(100, Math.round(quota.storageBytes / quota.maxStorageBytes * 100)) : 0;
  }

  createPayment(provider: 'Momo' | 'BankTransfer'): void {
    const plan = this.checkoutPlan();
    if (!plan || this.busy()) return;
    const idempotencyKey = this.getIdempotencyKey(plan.id, provider);
    this.busy.set(true);
    this.api.createPayment(plan.id, provider, idempotencyKey).pipe(finalize(() => this.busy.set(false))).subscribe({
      next: (checkout) => {
        this.clearIdempotencyKey(plan.id, provider);
        this.payments.update((items) => [checkout.payment, ...items.filter((item) => item.id !== checkout.payment.id)]);
        this.historyLoaded.set(true);
        this.checkoutPlan.set(null);
        if (checkout.checkoutUrl) {
          window.location.assign(checkout.checkoutUrl);
          return;
        }
        if (checkout.bankTransferInstructions) {
          this.bankTransferInstructions.set(checkout.bankTransferInstructions);
          this.watchBankTransfer(checkout.payment.id);
          this.notify('Hãy chuyển đúng số tiền và nội dung. Gói sẽ tự kích hoạt ngay khi ngân hàng báo có.');
          return;
        }
        this.notify('Giao dịch đã được ghi nhận.');
      },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Kết nối bị gián đoạn. Hãy bấm lại cùng phương thức để tiếp tục giao dịch cũ, không tạo thêm giao dịch mới.'),
    });
  }

  private getIdempotencyKey(planId: string, provider: 'Momo' | 'BankTransfer'): string {
    const key = this.paymentStorageKey(planId, provider);
    const existing = sessionStorage.getItem(key);
    if (existing) return existing;
    const value = crypto.randomUUID();
    sessionStorage.setItem(key, value);
    return value;
  }

  private clearIdempotencyKey(planId: string, provider: 'Momo' | 'BankTransfer'): void {
    sessionStorage.removeItem(this.paymentStorageKey(planId, provider));
  }

  private paymentStorageKey(planId: string, provider: 'Momo' | 'BankTransfer'): string {
    return `planora.billing.payment.${planId}.${provider}`;
  }

  private watchBankTransfer(paymentId: string): void {
    this.bankTransferWatcher?.unsubscribe();
    this.bankTransferWatcher = timer(5000, 5000).pipe(
      take(36),
      switchMap(() => this.api.getPayments()),
    ).subscribe({
      next: (payments) => {
        this.payments.set(payments);
        const payment = payments.find((item) => item.id === paymentId);
        if (payment?.status !== 'Success') return;
        this.bankTransferWatcher?.unsubscribe();
        this.bankTransferInstructions.set(null);
        this.store.refreshProfile();
        this.notify('Đã nhận được chuyển khoản và kích hoạt gói tự động.');
      },
    });
  }

  private notify(value: string): void { this.toast.set(value); setTimeout(() => this.toast.set(null), 2800); }
}
