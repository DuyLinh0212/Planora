import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideCalendarPlus, LucideGripVertical, LucidePlay, LucideSquareCheckBig, LucideX } from '@lucide/angular';
import { finalize } from 'rxjs';
import { Sprint } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

@Component({
  selector: 'app-project-sprints-page',
  imports: [DatePipe, FormsModule, LucideCalendarPlus, LucideGripVertical, LucidePlay, LucideSquareCheckBig, LucideX],
  templateUrl: './project-sprints.page.html',
  styleUrl: './project-sprints.page.css',
})
export class ProjectSprintsPage {
  readonly store = inject(WorkspaceStore);
  readonly editorOpen = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly toast = signal<string | null>(null);
  readonly backlog = computed(() => this.store.tasks().filter((task) => !task.sprintId));
  readonly sortedSprints = computed(() => [...this.store.sprints()].sort((a, b) => (a.status === 'Active' ? -1 : b.startAt.localeCompare(a.startAt))));
  draft = { name: `Sprint ${this.store.sprints().length + 1}`, goal: '', startAt: '', endAt: '' };
  private readonly api = inject(PlanoraApiService);

  tasksInSprint(sprintId: string) { return this.store.tasks().filter((task) => task.sprintId === sprintId); }
  doneInSprint(sprintId: string): number { return this.tasksInSprint(sprintId).filter((task) => task.status.toLowerCase() === 'done').length; }
  sprintProgress(sprintId: string): number { const total = this.tasksInSprint(sprintId).length; return total ? Math.round(this.doneInSprint(sprintId) / total * 100) : 0; }
  createSprint(): void {
    if (!this.store.hasPermission('sprint.create')) return;
    if (!this.draft.name.trim() || this.saving()) return;
    if (this.draft.endAt < this.draft.startAt) return this.error.set('Ngày kết thúc phải sau ngày bắt đầu.');
    this.saving.set(true); this.error.set(null);
    this.api.createSprint(this.store.project().id, { name: this.draft.name.trim(), goal: this.draft.goal.trim() || null, startAt: new Date(this.draft.startAt).toISOString(), endAt: new Date(this.draft.endAt).toISOString() }).pipe(finalize(() => this.saving.set(false))).subscribe({ next: (sprint) => { this.store.sprints.update((items) => [...items, sprint]); this.editorOpen.set(false); this.notify('Đã tạo sprint.'); }, error: (error) => this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể tạo sprint.') });
  }
  startSprint(sprint: Sprint): void { if (!this.store.hasPermission('sprint.edit')) return; this.api.startSprint(sprint.id).subscribe({ next: () => { this.store.sprints.update((items) => items.map((item) => item.id === sprint.id ? { ...item, status: 'Active' } : item)); this.notify('Sprint đã bắt đầu.'); }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể bắt đầu sprint.') }); }
  closeSprint(sprint: Sprint): void { if (!this.store.hasPermission('sprint.close') || !confirm(`Đóng “${sprint.name}”?`)) return; this.api.closeSprint(sprint.id).subscribe({ next: () => { this.store.sprints.update((items) => items.map((item) => item.id === sprint.id ? { ...item, status: 'Closed' } : item)); this.notify('Sprint đã đóng.'); }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể đóng sprint.') }); }
  private notify(value: string): void { this.toast.set(value); setTimeout(() => this.toast.set(null), 2300); }
}
