import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AdminConsoleContextService } from '../../core/admin-console-context.service';
import { FeedbackItem, PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-feedback-page',
  imports: [DatePipe, FormsModule],
  templateUrl: './feedback-page.component.html',
  styleUrl: './feedback-page.component.css',
})
export class FeedbackPageComponent {
  readonly context = inject(AdminConsoleContextService);
  private readonly api = inject(PlanoraAdminApiService);
  readonly selectedFeedback = signal<FeedbackItem | null>(null);
  readonly message = signal<string | null>(null);
  search = '';
  status = '';
  priority = '';
  internalNote = '';
  formatFeedbackStatus(status: unknown): string {
    if (status === 0 || status === '0' || String(status).toLowerCase() === 'open') return 'Open';
    if (status === 1 || status === '1' || String(status).toLowerCase() === 'inreview') return 'In Review';
    if (status === 2 || status === '2' || String(status).toLowerCase() === 'resolved') return 'Resolved';
    if (status === 3 || status === '3' || String(status).toLowerCase() === 'dismissed') return 'Dismissed';
    return String(status ?? 'Open');
  }

  formatFeedbackPriority(priority: unknown): string {
    if (priority === 0 || priority === '0' || String(priority).toLowerCase() === 'low') return 'Low';
    if (priority === 1 || priority === '1' || String(priority).toLowerCase() === 'medium') return 'Medium';
    if (priority === 2 || priority === '2' || String(priority).toLowerCase() === 'high') return 'High';
    return String(priority ?? 'Low');
  }

  readonly filteredFeedback = computed(() =>
    this.context
      .feedback()
      .filter(
        (item) =>
          (!this.status || this.formatFeedbackStatus(item.status) === this.status) &&
          (!this.priority || this.formatFeedbackPriority(item.priority) === this.priority) &&
          `${item.subject} ${item.userEmail}`.toLowerCase().includes(this.search.toLowerCase()),
      ),
  );
  reloadFeedback(): void {
    this.api
      .getFeedbackItems(this.status || undefined, this.priority || undefined)
      .subscribe((response) => this.context.feedback.set(response.items));
  }
  assignToMe(item: FeedbackItem): void {
    if (item.id.startsWith('demo-'))
      return this.notify('Preview mode: assignment maps to the admin feedback API.');
    const administratorId = localStorage.getItem('planora.admin.userId');
    if (!administratorId) return this.notify('Administrator identity is missing from the session.');
    this.api
      .assignFeedbackItem(item.id, administratorId)
      .subscribe(() => this.notify('Feedback assigned.'));
  }
  resolveFeedback(item: FeedbackItem): void {
    if (item.id.startsWith('demo-'))
      return this.notify('Preview mode: resolution maps to the admin feedback API.');
    this.api.resolveFeedbackItem(item.id, this.internalNote).subscribe(() => {
      this.context.feedback.update((items) =>
        items.map((entry) =>
          entry.id === item.id
            ? {
                ...entry,
                status: 'Resolved',
                internalNote: this.internalNote,
                resolvedAt: new Date().toISOString(),
              }
            : entry,
        ),
      );
      this.selectedFeedback.set(null);
      this.notify('Feedback resolved.');
    });
  }
  private notify(value: string): void {
    this.message.set(value);
    setTimeout(() => this.message.set(null), 2500);
  }
}
