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

  private notify(value: string): void {
    this.message.set(value);
    setTimeout(() => this.message.set(null), 2400);
  }
}
