import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { LucideCheck, LucideChevronDown, LucideCircleCheckBig, LucideClock3, LucideDatabase, LucideReceiptText, LucideRefreshCw, LucideSparkles, LucideTriangleAlert, LucideX } from '@lucide/angular';
import { catchError, EMPTY, finalize, Subscription, switchMap, take, timer } from 'rxjs';
import { AvailablePlan, BankTransferInstructions, UserPayment } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

@Component({
  selector: 'app-billing-page',
  imports: [DatePipe, DecimalPipe, LucideCheck, LucideChevronDown, LucideCircleCheckBig, LucideClock3, LucideDatabase, LucideReceiptText, LucideRefreshCw, LucideSparkles, LucideTriangleAlert, LucideX],
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
  readonly bankTransferState = signal<'checking' | 'waiting' | 'confirmed' | 'connection-error'>('waiting');
  readonly bankTransferLastCheckedAt = signal<Date | null>(null);
  readonly bankTransferCheckingNow = signal(false);
  private readonly api = inject(PlanoraApiService);
  private bankTransferWatcher?: Subscription;
  private bankTransferPaymentId?: string;

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

  createPayment(provider: 'BankTransfer'): void {
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
          return;
        }
        this.notify('Giao dịch đã được ghi nhận.');
      },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Kết nối bị gián đoạn. Hãy bấm lại cùng phương thức để tiếp tục giao dịch cũ, không tạo thêm giao dịch mới.'),
    });
  }

  private getIdempotencyKey(planId: string, provider: 'BankTransfer'): string {
    const key = this.paymentStorageKey(planId, provider);
    const existing = sessionStorage.getItem(key);
    if (existing) return existing;
    const value = crypto.randomUUID();
    sessionStorage.setItem(key, value);
    return value;
  }

  private clearIdempotencyKey(planId: string, provider: 'BankTransfer'): void {
    sessionStorage.removeItem(this.paymentStorageKey(planId, provider));
  }

  private paymentStorageKey(planId: string, provider: 'BankTransfer'): string {
    return `planora.billing.payment.${planId}.${provider}`;
  }

  private watchBankTransfer(paymentId: string): void {
    this.bankTransferWatcher?.unsubscribe();
    this.bankTransferPaymentId = paymentId;
    this.bankTransferLastCheckedAt.set(null);
    this.bankTransferState.set('checking');
    this.bankTransferWatcher = timer(0, 3000).pipe(
      take(120),
      switchMap(() => this.loadBankTransferStatus()),
    ).subscribe({
      next: (payments) => {
        this.payments.set(payments);
        this.bankTransferLastCheckedAt.set(new Date());
        this.bankTransferState.set('waiting');
        const payment = payments.find((item) => item.id === paymentId);
        if (payment?.status !== 'Success') return;
        this.bankTransferWatcher?.unsubscribe();
        this.bankTransferState.set('confirmed');
        this.store.refreshProfile();
      },
    });
  }

  checkBankTransferNow(): void {
    const paymentId = this.bankTransferPaymentId;
    if (!paymentId || this.bankTransferCheckingNow() || this.bankTransferState() === 'confirmed') return;
    this.bankTransferCheckingNow.set(true);
    this.bankTransferState.set('checking');
    this.loadBankTransferStatus().pipe(finalize(() => this.bankTransferCheckingNow.set(false))).subscribe({
      next: (payments) => {
        this.payments.set(payments);
        this.bankTransferLastCheckedAt.set(new Date());
        const payment = payments.find((item) => item.id === paymentId);
        if (payment?.status !== 'Success') {
          this.bankTransferState.set('waiting');
          return;
        }
        this.bankTransferWatcher?.unsubscribe();
        this.bankTransferState.set('confirmed');
        this.store.refreshProfile();
      },
    });
  }

  dismissBankTransfer(): void {
    this.bankTransferWatcher?.unsubscribe();
    this.bankTransferPaymentId = undefined;
    this.bankTransferInstructions.set(null);
  }

  private loadBankTransferStatus() {
    return this.api.getPayments().pipe(
      catchError(() => {
        this.bankTransferState.set('connection-error');
        return EMPTY;
      }),
    );
  }

  private notify(value: string): void { this.toast.set(value); setTimeout(() => this.toast.set(null), 2800); }
}
