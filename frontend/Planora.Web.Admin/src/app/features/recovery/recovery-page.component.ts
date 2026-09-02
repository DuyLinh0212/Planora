import { DatePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { LucideArchiveRestore, LucideRefreshCw } from '@lucide/angular';
import { DeletedWorkspaceItem, PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-recovery-page',
  imports: [DatePipe, LucideArchiveRestore, LucideRefreshCw],
  templateUrl: './recovery-page.component.html',
  styleUrl: './recovery-page.component.css',
})
export class RecoveryPageComponent {
  readonly items = signal<DeletedWorkspaceItem[]>([]);
  readonly loading = signal(true);
  readonly restoringId = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  private readonly api = inject(PlanoraAdminApiService);
  constructor() { this.load(); }
  load(): void {
    this.loading.set(true); this.error.set(null);
    this.api.getDeletedWorkspaceItems().subscribe({ next: (items) => { this.items.set(items); this.loading.set(false); }, error: () => { this.error.set('Could not load the recovery queue.'); this.loading.set(false); } });
  }
  restore(item: DeletedWorkspaceItem): void {
    if (!window.confirm(`Restore ${item.kind.toLowerCase()} “${item.name}”?`)) return;
    this.restoringId.set(item.id); this.error.set(null);
    const request = item.kind === 'Project' ? this.api.restoreDeletedProject(item.id) : this.api.restoreDeletedSprint(item.id);
    request.subscribe({ next: () => { this.items.update((items) => items.filter((candidate) => candidate.id !== item.id)); this.restoringId.set(null); }, error: (response) => { this.error.set(response.error?.errors?.[0]?.message ?? 'Could not restore this item.'); this.restoringId.set(null); } });
  }
}
