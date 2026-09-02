import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
export interface TimeSeriesPoint {
  date: string;
  value: number;
}
export interface CategoryMetric {
  label: string;
  value: number;
}
export interface AdminActivity {
  id: string;
  actorUserId: string | null;
  actorDisplayName: string;
  action: string;
  entityType: string;
  entityId: string;
  createdAt: string;
}
export interface AdminAttention {
  code: string;
  label: string;
  count: number;
  severity: string;
}
export interface AdminOverview {
  totalUsers: number;
  activeUsers: number;
  totalProjects: number;
  activeProjects: number;
  completedProjects: number;
  subscriptionRevenue: number;
  paymentSuccessRate: number;
  aggregateStorageBytes: number;
  userActivationTrend: TimeSeriesPoint[];
  projectStatusDistribution: CategoryMetric[];
  subscriptionDistribution: CategoryMetric[];
  paymentRevenueTrend: TimeSeriesPoint[];
  needsAttention: AdminAttention[];
  recentAdminActivity: AdminActivity[];
}
export interface AdminAnalytics {
  periodStart: string;
  periodEnd: string;
  newUsers: TimeSeriesPoint[];
  usersByPlan: CategoryMetric[];
  projectsByStatus: CategoryMetric[];
  paymentsByStatus: CategoryMetric[];
  storageGrowth: TimeSeriesPoint[];
}
export interface AdminAccount {
  id: string;
  email: string;
  displayName: string;
  status: string;
  systemRole: string;
  planId: string | null;
  planName: string | null;
  joinedAt: string;
  lastActiveAt: string;
  ownedProjectCount: number;
  storageBytes: number;
}
export interface AdminAccountDetails {
  account: AdminAccount;
  subscriptionId: string | null;
  subscriptionStatus: string | null;
  subscriptionStartedAt: string | null;
  subscriptionExpiresAt: string | null;
  maxOwnedProjects: number;
  maxStorageBytes: number;
  recentAdminActions: AdminActivity[];
}
export interface SubscriptionPlan {
  id: string;
  code: string;
  name: string;
  price: number;
  currency: string;
  billingPeriod: string;
  maxOwnedProjects: number;
  maxStorageBytes: number;
  entitlements: string[];
  isActive: boolean;
  activeSubscriberCount: number;
  updatedAt: string;
}
export interface PaymentTransaction {
  id: string;
  userId: string;
  userEmail: string;
  planId: string;
  planName: string;
  provider: string;
  providerTransactionId: string | null;
  amount: number;
  currency: string;
  status: string;
  idempotencyKey: string;
  createdAt: string;
  paidAt: string | null;
  reviewedAt: string | null;
}
export interface FeedbackItem {
  id: string;
  userId: string | null;
  userEmail: string;
  category: string;
  subject: string;
  content: string;
  status: string;
  priority: string;
  assignedAdminUserId: string | null;
  internalNote: string | null;
  createdAt: string;
  resolvedAt: string | null;
}
export interface AdminAuthenticationResponse {
  accessToken: string;
  refreshToken: string;
  userId: string;
  email: string;
  displayName: string;
}
export interface PasswordResetResponse {
  message: string;
  resetToken: string | null;
}
export interface SupportMessage {
  id: string; senderUserId: string; senderDisplayName: string; content: string; createdAt: string;
}
export interface SupportConversation {
  id: string; kind: string; subject: string; status: string; paymentTransactionId: string | null;
  createdAt: string; closedAt: string | null; messages: SupportMessage[];
}
export interface MaintenanceStatus { isEnabled: boolean; message: string; updatedAt: string | null; }
export interface DeletedWorkspaceItem {
  id: string;
  kind: 'Project' | 'Sprint';
  name: string;
  deletedAt: string;
}

@Injectable({ providedIn: 'root' })
export class PlanoraAdminApiService {
  private readonly httpClient = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/api/admin`;

  loginAdministrator(
    email: string,
    password: string,
  ): Observable<AdminAuthenticationResponse> {
    return this.httpClient.post<AdminAuthenticationResponse>(
      `${environment.apiUrl}/api/auth/login`,
      { identifier: email, email, password, deviceInfo: 'Planora Web.Admin', rememberMe: true },
      { withCredentials: true },
    );
  }
  logoutAdministrator(refreshToken: string): Observable<void> {
    return this.httpClient.post<void>(
      `${environment.apiUrl}/api/auth/logout`,
      { refreshToken },
      { withCredentials: true },
    );
  }
  requestPasswordReset(email: string): Observable<PasswordResetResponse> {
    return this.httpClient.post<PasswordResetResponse>(
      `${environment.apiUrl}/api/auth/password/forgot`,
      { email },
    );
  }
  resetPassword(token: string, newPassword: string): Observable<void> {
    return this.httpClient.post<void>(`${environment.apiUrl}/api/auth/password/reset`, {
      token,
      newPassword,
    });
  }
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.httpClient.post<void>(`${environment.apiUrl}/api/auth/password/change`, {
      currentPassword,
      newPassword,
    });
  }

  getAdminOverview(): Observable<AdminOverview> {
    return this.httpClient.get<AdminOverview>(`${this.apiUrl}/overview`);
  }
  getAdminAnalytics(periodStart?: string, periodEnd?: string): Observable<AdminAnalytics> {
    return this.httpClient.get<AdminAnalytics>(`${this.apiUrl}/analytics`, {
      params: { ...(periodStart && { periodStart }), ...(periodEnd && { periodEnd }) },
    });
  }
  getAdminActivity(page = 1, pageSize = 20): Observable<PagedResponse<AdminActivity>> {
    return this.httpClient.get<PagedResponse<AdminActivity>>(`${this.apiUrl}/activity`, {
      params: { page, pageSize },
    });
  }
  getAdministratorAccounts(
    search?: string,
    status?: string,
    page = 1,
    pageSize = 20,
  ): Observable<PagedResponse<AdminAccount>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    if (status) params = params.set('status', status);
    return this.httpClient.get<PagedResponse<AdminAccount>>(`${this.apiUrl}/accounts`, { params });
  }
  getAdministratorAccountById(accountId: string): Observable<AdminAccountDetails> {
    return this.httpClient.get<AdminAccountDetails>(`${this.apiUrl}/accounts/${accountId}`);
  }
  suspendAdministratorAccount(accountId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/accounts/${accountId}/suspend`, {});
  }
  restoreAdministratorAccount(accountId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/accounts/${accountId}/restore`, {});
  }
  assignPlanToAccount(accountId: string, planId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/accounts/${accountId}/plan`, { planId });
  }
  getSubscriptionPlans(): Observable<SubscriptionPlan[]> {
    return this.httpClient.get<SubscriptionPlan[]>(`${this.apiUrl}/plans`);
  }
  createSubscriptionPlan(request: {
    code: string;
    name: string;
    price: number;
    currency: string;
    billingPeriod: string;
    maxOwnedProjects: number;
    maxStorageBytes: number;
    entitlements: string[];
  }): Observable<SubscriptionPlan> {
    return this.httpClient.post<SubscriptionPlan>(`${this.apiUrl}/plans`, request);
  }
  updateSubscriptionPlan(
    planId: string,
    request: {
      name: string;
      price: number;
      currency: string;
      billingPeriod: string;
      maxOwnedProjects: number;
      maxStorageBytes: number;
      entitlements: string[];
      isActive: boolean;
    },
  ): Observable<SubscriptionPlan> {
    return this.httpClient.put<SubscriptionPlan>(`${this.apiUrl}/plans/${planId}`, request);
  }
  getPaymentTransactions(
    provider?: string,
    status?: string,
    page = 1,
    pageSize = 20,
  ): Observable<PagedResponse<PaymentTransaction>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (provider) params = params.set('provider', provider);
    if (status) params = params.set('status', status);
    return this.httpClient.get<PagedResponse<PaymentTransaction>>(`${this.apiUrl}/payments`, {
      params,
    });
  }
  getPaymentTransactionById(paymentTransactionId: string): Observable<PaymentTransaction> {
    return this.httpClient.get<PaymentTransaction>(
      `${this.apiUrl}/payments/${paymentTransactionId}`,
    );
  }
  markPaymentTransactionReviewed(paymentTransactionId: string): Observable<void> {
    return this.httpClient.post<void>(
      `${this.apiUrl}/payments/${paymentTransactionId}/mark-reviewed`,
      {},
    );
  }
  getFeedbackItems(
    status?: string,
    priority?: string,
    page = 1,
    pageSize = 20,
  ): Observable<PagedResponse<FeedbackItem>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    if (priority) params = params.set('priority', priority);
    return this.httpClient.get<PagedResponse<FeedbackItem>>(`${this.apiUrl}/feedback`, { params });
  }
  getFeedbackItemById(feedbackId: string): Observable<FeedbackItem> {
    return this.httpClient.get<FeedbackItem>(`${this.apiUrl}/feedback/${feedbackId}`);
  }
  assignFeedbackItem(feedbackId: string, administratorUserId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/feedback/${feedbackId}/assign`, {
      administratorUserId,
    });
  }
  resolveFeedbackItem(feedbackId: string, internalNote?: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/feedback/${feedbackId}/resolve`, {
      internalNote,
    });
  }
  getSupportConversations(status?: string): Observable<SupportConversation[]> {
    return this.httpClient.get<SupportConversation[]>(`${this.apiUrl}/support/conversations`, { params: status ? { status } : {} });
  }
  sendSupportMessage(conversationId: string, content: string): Observable<SupportMessage> {
    return this.httpClient.post<SupportMessage>(`${this.apiUrl}/support/conversations/${conversationId}/messages`, { content });
  }
  closeSupportConversation(conversationId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/support/conversations/${conversationId}/close`, {});
  }
  getMaintenanceStatus(): Observable<MaintenanceStatus> {
    return this.httpClient.get<MaintenanceStatus>(`${environment.apiUrl}/api/system/maintenance`);
  }
  updateMaintenanceStatus(isEnabled: boolean, message: string): Observable<void> {
    return this.httpClient.put<void>(`${environment.apiUrl}/api/system/maintenance`, { isEnabled, message });
  }
  getDeletedWorkspaceItems(): Observable<DeletedWorkspaceItem[]> {
    return this.httpClient.get<DeletedWorkspaceItem[]>(`${this.apiUrl}/recovery/workspace-items`);
  }
  restoreDeletedProject(projectId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/recovery/projects/${projectId}/restore`, {});
  }
  restoreDeletedSprint(sprintId: string): Observable<void> {
    return this.httpClient.post<void>(`${this.apiUrl}/recovery/sprints/${sprintId}/restore`, {});
  }
}
