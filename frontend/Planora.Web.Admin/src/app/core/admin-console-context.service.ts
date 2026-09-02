import { Injectable, inject, signal } from '@angular/core';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import {
  AdminAccount,
  AdminActivity,
  AdminAnalytics,
  AdminOverview,
  FeedbackItem,
  PaymentTransaction,
  PlanoraAdminApiService,
  SubscriptionPlan,
} from './planora-admin-api.service';

const OVERVIEW: AdminOverview = {
  totalUsers: 1842,
  activeUsers: 1326,
  totalProjects: 3264,
  activeProjects: 2018,
  completedProjects: 1246,
  subscriptionRevenue: 24860,
  paymentSuccessRate: 97.2,
  aggregateStorageBytes: 1_420_000_000_000,
  userActivationTrend: [],
  projectStatusDistribution: [
    { label: 'Active', value: 2018 },
    { label: 'Completed', value: 1246 },
  ],
  subscriptionDistribution: [
    { label: 'Pro Annual', value: 612 },
    { label: 'Pro Monthly', value: 884 },
    { label: 'Free', value: 346 },
  ],
  paymentRevenueTrend: [],
  needsAttention: [
    { code: 'pending', label: 'Pending payments', count: 12, severity: 'warning' },
    { code: 'feedback', label: 'Unresolved feedback', count: 18, severity: 'info' },
  ],
  recentAdminActivity: [],
};
const ACCOUNTS: AdminAccount[] = [
  {
    id: 'demo-alice',
    email: 'alice@acme.edu',
    displayName: 'Alice Johnson',
    status: 'Active',
    systemRole: 'User',
    planId: 'demo-pro',
    planName: 'Pro Annual',
    joinedAt: '2026-05-14T00:00:00Z',
    lastActiveAt: '2026-05-19T00:00:00Z',
    ownedProjectCount: 12,
    storageBytes: 38_600_000_000,
  },
  {
    id: 'demo-binh',
    email: 'binh@brightlabs.io',
    displayName: 'Binh Nguyen',
    status: 'Active',
    systemRole: 'User',
    planId: 'demo-pro',
    planName: 'Pro Monthly',
    joinedAt: '2026-02-18T00:00:00Z',
    lastActiveAt: '2026-05-19T00:00:00Z',
    ownedProjectCount: 7,
    storageBytes: 12_400_000_000,
  },
  {
    id: 'demo-carla',
    email: 'carla@urbanbridge.io',
    displayName: 'Carla Ruiz',
    status: 'Suspended',
    systemRole: 'User',
    planId: 'demo-free',
    planName: 'Free',
    joinedAt: '2026-02-12T00:00:00Z',
    lastActiveAt: '2026-05-16T00:00:00Z',
    ownedProjectCount: 2,
    storageBytes: 1_100_000_000,
  },
];
const PLANS: SubscriptionPlan[] = [
  {
    id: 'demo-free',
    code: 'plan_free',
    name: 'Free',
    price: 0,
    currency: 'USD',
    billingPeriod: 'Forever',
    maxOwnedProjects: 3,
    maxStorageBytes: 5_000_000_000,
    entitlements: ['Core project features'],
    isActive: true,
    activeSubscriberCount: 346,
    updatedAt: '2026-05-19T00:00:00Z',
  },
  {
    id: 'demo-pro',
    code: 'plan_pro_annual',
    name: 'Pro Annual',
    price: 90,
    currency: 'USD',
    billingPeriod: 'Yearly',
    maxOwnedProjects: 50,
    maxStorageBytes: 100_000_000_000,
    entitlements: ['Core project features', 'File storage', 'Analytics', 'Priority support'],
    isActive: true,
    activeSubscriberCount: 612,
    updatedAt: '2026-05-19T00:00:00Z',
  },
];
const PAYMENTS: PaymentTransaction[] = [
  {
    id: 'demo-pay-1',
    userId: 'alice',
    userEmail: 'alice@acme.edu',
    planId: 'demo-pro',
    planName: 'Pro Annual',
    provider: 'MoMo',
    providerTransactionId: 'MOMO_1758',
    amount: 90,
    currency: 'USD',
    status: 'Success',
    idempotencyKey: 'planora_demo_1',
    createdAt: '2026-05-19T10:31:00Z',
    paidAt: '2026-05-19T10:32:00Z',
    reviewedAt: null,
  },
  {
    id: 'demo-pay-2',
    userId: 'binh',
    userEmail: 'binh@brightlabs.io',
    planId: 'demo-pro',
    planName: 'Pro Monthly',
    provider: 'ZaloPay',
    providerTransactionId: null,
    amount: 9,
    currency: 'USD',
    status: 'Pending',
    idempotencyKey: 'planora_demo_2',
    createdAt: '2026-05-19T09:44:00Z',
    paidAt: null,
    reviewedAt: null,
  },
];
const FEEDBACK: FeedbackItem[] = [
  {
    id: 'demo-fb-1',
    userId: 'alice',
    userEmail: 'alice@acme.edu',
    category: 'Feature request',
    subject: 'Calendar export for sprint dates',
    content: 'Please add an iCal export.',
    status: 'New',
    priority: 'Medium',
    assignedAdminUserId: null,
    internalNote: null,
    createdAt: '2026-05-18T10:31:00Z',
    resolvedAt: null,
  },
  {
    id: 'demo-fb-2',
    userId: 'binh',
    userEmail: 'binh@brightlabs.io',
    category: 'Bug report',
    subject: 'Billing receipt cannot download',
    content: 'The receipt link returns an error.',
    status: 'InReview',
    priority: 'High',
    assignedAdminUserId: null,
    internalNote: null,
    createdAt: '2026-05-17T11:40:00Z',
    resolvedAt: null,
  },
];

@Injectable({ providedIn: 'root' })
export class AdminConsoleContextService {
  private readonly api = inject(PlanoraAdminApiService);
  readonly overview = signal(OVERVIEW);
  readonly accounts = signal(ACCOUNTS);
  readonly plans = signal(PLANS);
  readonly payments = signal(PAYMENTS);
  readonly feedback = signal(FEEDBACK);
  readonly analytics = signal<AdminAnalytics | null>(null);
  readonly activity = signal<AdminActivity[]>([]);
  readonly usingDemoData = signal(true);
  readonly loading = signal(false);
  loadConsole(): void {
    if (!localStorage.getItem('planora.admin.accessToken')) {
      this.usingDemoData.set(true);
      this.loading.set(false);
      return;
    }
    this.loading.set(true);
    forkJoin({
      overview: this.api.getAdminOverview().pipe(catchError(() => of(OVERVIEW))),
      accounts: this.api.getAdministratorAccounts().pipe(catchError(() => of({ items: ACCOUNTS, totalCount: ACCOUNTS.length, page: 1, pageSize: 20 }))),
      plans: this.api.getSubscriptionPlans().pipe(catchError(() => of(PLANS))),
      payments: this.api.getPaymentTransactions().pipe(catchError(() => of({ items: PAYMENTS, totalCount: PAYMENTS.length, page: 1, pageSize: 20 }))),
      feedback: this.api.getFeedbackItems().pipe(catchError(() => of({ items: FEEDBACK, totalCount: FEEDBACK.length, page: 1, pageSize: 20 }))),
      analytics: this.api.getAdminAnalytics().pipe(catchError(() => of(null))),
      activity: this.api.getAdminActivity().pipe(catchError(() => of({ items: [], totalCount: 0, page: 1, pageSize: 20 }))),
    })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe((data) => {
        if (data) {
          this.overview.set(data.overview);
          this.accounts.set(data.accounts.items);
          this.plans.set(data.plans);
          this.payments.set(data.payments.items);
          this.feedback.set(data.feedback.items);
          this.analytics.set(data.analytics);
          this.activity.set(data.activity.items);
          this.usingDemoData.set(false);
        }
      });
  }

  reloadOverview(): void {
    this.api.getAdminOverview().subscribe((overview) => {
      this.overview.set(overview);
      this.usingDemoData.set(false);
    });
  }

  reloadAccounts(search?: string, status?: string): void {
    this.api.getAdministratorAccounts(search, status).subscribe((res) => {
      this.accounts.set(res.items);
      this.usingDemoData.set(false);
    });
  }

  reloadPlans(): void {
    this.api.getSubscriptionPlans().subscribe((plans) => {
      this.plans.set(plans);
      this.usingDemoData.set(false);
    });
  }

  reloadPayments(provider?: string, status?: string): void {
    this.api.getPaymentTransactions(provider, status).subscribe((res) => {
      this.payments.set(res.items);
      this.usingDemoData.set(false);
    });
  }

  reloadFeedback(status?: string, priority?: string): void {
    this.api.getFeedbackItems(status, priority).subscribe((res) => {
      this.feedback.set(res.items);
      this.usingDemoData.set(false);
    });
  }

  reloadActivity(): void {
    this.api.getAdminActivity().subscribe((res) => {
      this.activity.set(res.items);
      this.usingDemoData.set(false);
    });
  }
}
