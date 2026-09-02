import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { PlanoraAdminApiService } from './planora-admin-api.service';

describe('PlanoraAdminApiService', () => {
  let api: PlanoraAdminApiService; let http: HttpTestingController;
  beforeEach(() => { TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }); api = TestBed.inject(PlanoraAdminApiService); http = TestBed.inject(HttpTestingController); });
  afterEach(() => http.verify());
  it('queries accounts with paging and filters', () => { api.getAdministratorAccounts('alice', 'Active', 2, 10).subscribe(); const request = http.expectOne((candidate) => candidate.url.endsWith('/api/admin/accounts')); expect(request.request.params.get('search')).toBe('alice'); expect(request.request.params.get('status')).toBe('Active'); expect(request.request.params.get('page')).toBe('2'); request.flush({ items: [], totalCount: 0, page: 2, pageSize: 10 }); });
  it('assigns a subscription plan to an account', () => { api.assignPlanToAccount('account-1', 'plan-1').subscribe(); const request = http.expectOne((candidate) => candidate.url.endsWith('/api/admin/accounts/account-1/plan')); expect(request.request.method).toBe('POST'); expect(request.request.body.planId).toBe('plan-1'); request.flush(null); });
  it('updates the complete plan contract', () => { api.updateSubscriptionPlan('plan-1', { name: 'Pro', price: 9, currency: 'USD', billingPeriod: 'Monthly', maxOwnedProjects: 20, maxStorageBytes: 50_000_000_000, entitlements: ['Analytics'], isActive: true }).subscribe(); const request = http.expectOne((candidate) => candidate.url.endsWith('/api/admin/plans/plan-1')); expect(request.request.method).toBe('PUT'); expect(request.request.body.isActive).toBeTrue(); request.flush({}); });
  it('resolves feedback with an internal note', () => { api.resolveFeedbackItem('feedback-1', 'Verified').subscribe(); const request = http.expectOne((candidate) => candidate.url.endsWith('/api/admin/feedback/feedback-1/resolve')); expect(request.request.method).toBe('POST'); expect(request.request.body.internalNote).toBe('Verified'); request.flush(null); });
  it('requests administrator password recovery through the shared identity API', () => { api.requestPasswordReset('admin@planora.com').subscribe(); const request = http.expectOne((candidate) => candidate.url.endsWith('/api/auth/password/forgot')); expect(request.request.method).toBe('POST'); request.flush({ message: 'If the account exists, instructions were created.', resetToken: null }); });
  it('logs out the administrator refresh token', () => { api.logoutAdministrator('refresh-token').subscribe(); const request = http.expectOne((candidate) => candidate.url.endsWith('/api/auth/logout')); expect(request.request.method).toBe('POST'); expect(request.request.body.refreshToken).toBe('refresh-token'); request.flush(null); });
});
