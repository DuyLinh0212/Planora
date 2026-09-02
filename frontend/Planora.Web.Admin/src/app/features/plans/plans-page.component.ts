import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';
import { PlanoraAdminApiService, SubscriptionPlan } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-plans-page',
  imports: [FormsModule],
  templateUrl: './plans-page.component.html',
  styleUrl: './plans-page.component.css',
})
export class PlansPageComponent {
  readonly context = inject(AdminConsoleContextService);
  private readonly api = inject(PlanoraAdminApiService);
  readonly selectedPlan = signal<SubscriptionPlan | null>(null);
  readonly message = signal<string | null>(null);
  draft = this.emptyDraft();

  constructor() {
    effect(() => {
      const plans = this.context.plans();
      if (plans.length > 0 && !this.selectedPlan()) {
        this.editPlan(plans[0]);
      }
    });
  }

  createDraft(): void {
    this.selectedPlan.set(null);
    this.draft = this.emptyDraft();
  }

  editPlan(plan: SubscriptionPlan): void {
    this.selectedPlan.set(plan);
    this.draft = {
      code: plan.code,
      name: plan.name,
      price: plan.price,
      currency: plan.currency || 'VND',
      billingPeriod: this.formatBillingPeriod(plan.billingPeriod),
      maxOwnedProjects: plan.maxOwnedProjects,
      maxStorageGb: Math.round(plan.maxStorageBytes / 1_000_000_000),
      entitlements: plan.entitlements.join('\n'),
      isActive: plan.isActive,
    };
  }

  savePlan(): void {
    const request = {
      name: this.draft.name,
      price: Number(this.draft.price) || 0,
      currency: this.draft.currency || 'VND',
      billingPeriod: this.draft.billingPeriod,
      maxOwnedProjects: Number(this.draft.maxOwnedProjects) || 1,
      maxStorageBytes: (Number(this.draft.maxStorageGb) || 1) * 1_000_000_000,
      entitlements: this.draft.entitlements.split('\n').map(s => s.trim()).filter(Boolean),
      isActive: this.draft.isActive,
    };
    const selected = this.selectedPlan();
    if (selected?.id.startsWith('demo-'))
      return this.notify('Preview mode: plan update maps to the admin plans API.');
    if (selected)
      this.api.updateSubscriptionPlan(selected.id, request).subscribe((plan) => this.upsert(plan));
    else
      this.api
        .createSubscriptionPlan({ ...request, code: this.draft.code })
        .subscribe((plan) => this.upsert(plan));
  }

  formatBillingPeriod(period: unknown): string {
    if (period === 0 || period === '0' || String(period).toLowerCase() === 'forever') return 'Forever';
    if (period === 1 || period === '1' || String(period).toLowerCase() === 'monthly') return 'Monthly';
    if (period === 2 || period === '2' || String(period).toLowerCase() === 'yearly') return 'Yearly';
    return String(period ?? 'Monthly');
  }

  formatPrice(price: number, currency: string): string {
    if (!price || price === 0) return 'Free (0₫)';
    const cur = currency?.toUpperCase() || 'VND';
    if (cur === 'VND') {
      return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(price);
    }
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: cur }).format(price);
  }

  private upsert(plan: SubscriptionPlan): void {
    this.context.plans.update((items) =>
      items.some((item) => item.id === plan.id)
        ? items.map((item) => (item.id === plan.id ? plan : item))
        : [plan, ...items],
    );
    this.editPlan(plan);
    this.notify('Plan saved successfully.');
  }

  private emptyDraft() {
    return {
      code: '',
      name: '',
      price: 0,
      currency: 'VND',
      billingPeriod: 'Monthly',
      maxOwnedProjects: 3,
      maxStorageGb: 5,
      entitlements: 'Core project features\nPriority support',
      isActive: true,
    };
  }

  private notify(value: string): void {
    this.message.set(value);
    setTimeout(() => this.message.set(null), 2400);
  }
}
