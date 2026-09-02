import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';
import { AdminAccount, PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-accounts-page',
  imports: [DatePipe, FormsModule],
  templateUrl: './accounts-page.component.html',
  styleUrl: './accounts-page.component.css',
})
export class AccountsPageComponent {
  readonly context = inject(AdminConsoleContextService);
  private readonly api = inject(PlanoraAdminApiService);
  readonly selectedAccount = signal<AdminAccount | null>(null);
  readonly selectedPlanId = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  search = '';
  status = '';
  readonly filteredAccounts = computed(() =>
    this.context
      .accounts()
      .filter(
        (account) =>
          (!this.status || this.formatStatus(account.status) === this.status) &&
          `${account.displayName} ${account.email} ${account.id}`
            .toLowerCase()
            .includes(this.search.toLowerCase()),
      ),
  );

  formatStatus(status: unknown): string {
    if (status === 0 || status === '0' || String(status).toLowerCase() === 'active') return 'Active';
    if (status === 1 || status === '1' || String(status).toLowerCase() === 'suspended') return 'Suspended';
    if (status === 2 || status === '2' || String(status).toLowerCase() === 'pending') return 'Pending';
    return String(status ?? 'Active');
  }

  reloadAccounts(): void {
    this.api
      .getAdministratorAccounts(this.search, this.status || undefined)
      .subscribe((response) => this.context.accounts.set(response.items));
  }

  suspendAccount(account: AdminAccount): void {
    if (account.id.startsWith('demo-'))
      return this.notify('Preview mode: suspension maps to the admin account API.');
    this.api
      .suspendAdministratorAccount(account.id)
      .subscribe(() => this.applyStatus(account, 'Suspended'));
  }

  restoreAccount(account: AdminAccount): void {
    if (account.id.startsWith('demo-'))
      return this.notify('Preview mode: restoration maps to the admin account API.');
    this.api
      .restoreAdministratorAccount(account.id)
      .subscribe(() => this.applyStatus(account, 'Active'));
  }

  selectAccount(account: AdminAccount): void {
    this.selectedAccount.set(account);
    this.selectedPlanId.set(
      account.planId ??
        this.context.plans().find((plan) => plan.code.toLowerCase().includes('free'))?.id ??
        null,
    );
  }

  assignPlan(): void {
    const account = this.selectedAccount();
    const planId = this.selectedPlanId();
    if (!account || !planId) return this.notify('Select a plan first.');
    const plan = this.context.plans().find(item => item.id === planId);
    if (!plan) return this.notify('The selected plan is unavailable.');

    if (account.id.startsWith('demo-')) {
      this.updatePlan(account, plan.id, plan.name);
      return this.notify('Preview mode: plan assignment maps to the admin account API.');
    }

    this.api.assignPlanToAccount(account.id, plan.id).subscribe({
      next: () => {
        this.updatePlan(account, plan.id, plan.name);
        this.notify(`Plan ${plan.name} assigned.`);
      },
      error: () => this.notify('Could not assign the plan.'),
    });
  }

  formatBytes(bytes: number): string {
    if (!bytes || bytes === 0) return '0 GB';
    return `${(bytes / 1_000_000_000).toFixed(1)} GB`;
  }

  private applyStatus(account: AdminAccount, status: string): void {
    this.context.accounts.update((items) =>
      items.map((item) => (item.id === account.id ? { ...item, status } : item)),
    );
    this.selectedAccount.set(null);
    this.notify(`Account ${status.toLowerCase()}.`);
  }

  private updatePlan(account: AdminAccount, planId: string, planName: string): void {
    this.context.accounts.update((items) =>
      items.map((item) => (item.id === account.id ? { ...item, planId, planName } : item)),
    );
    this.selectedAccount.update((item) => item ? { ...item, planId, planName } : item);
    this.selectedPlanId.set(planId);
  }

  private notify(value: string): void {
    this.message.set(value);
    setTimeout(() => this.message.set(null), 2400);
  }
}
