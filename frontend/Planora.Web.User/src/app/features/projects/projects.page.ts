import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  LucideArrowUpRight,
  LucideBriefcaseBusiness,
  LucideCalendarClock,
  LucideGauge,
  LucideFolderPlus,
  LucideSearch,
  LucideUsersRound,
  LucideX,
} from '@lucide/angular';
import { finalize } from 'rxjs';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { Project } from '../../core/api/api.models';
import { QuotaNoticeService } from '../../core/feedback/quota-notice.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { I18nService, TranslatePipe } from '../../core/i18n/i18n.service';

@Component({
  selector: 'app-projects-page',
  imports: [
    DatePipe,
    FormsModule,
    RouterLink,
    TranslatePipe,
    LucideArrowUpRight,
    LucideBriefcaseBusiness,
    LucideCalendarClock,
    LucideGauge,
    LucideFolderPlus,
    LucideSearch,
    LucideUsersRound,
    LucideX,
  ],
  templateUrl: './projects.page.html',
  styleUrl: './projects.page.css',
})
export class ProjectsPage {
  readonly store = inject(WorkspaceStore);
  readonly i18n = inject(I18nService);
  readonly search = signal('');
  readonly filter = signal('all');
  readonly createOpen = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly filters = [
    { value: 'all', key: 'projects.all' },
    { value: 'active', key: 'projects.active' },
    { value: 'draft', key: 'projects.draft' },
    { value: 'completed', key: 'projects.completed' },
  ];
  readonly filteredProjects = computed(() => {
    const query = this.search().trim().toLowerCase();
    const activeFilter = this.filter();
    return this.store.projects().filter((project) => {
      const matchesQuery =
        !query || `${project.name} ${project.description}`.toLowerCase().includes(query);
      const sk = this.statusKey(project.status);
      const matchesFilter =
        activeFilter === 'all' ||
        sk === activeFilter ||
        (activeFilter === 'draft' && (sk === 'planning' || sk === 'draft'));
      return matchesQuery && matchesFilter;
    });
  });
  draft = { name: '', description: '', startAt: '', endAt: '' };

  private readonly api = inject(PlanoraApiService);
  private readonly router = inject(Router);
  private readonly quotaNotice = inject(QuotaNoticeService);

  constructor() {
    if (inject(ActivatedRoute).snapshot.queryParamMap.has('create')) {
      queueMicrotask(() => this.openCreate());
    }
  }

  activeCount(): number {
    return this.store.projects().filter((project) => this.statusKey(project.status) === 'active').length;
  }

  quotaPercent(): number {
    const quota = this.store.profile().quota;
    return quota.maxOwnedProjects
      ? Math.min(100, Math.round((quota.ownedProjects / quota.maxOwnedProjects) * 100))
      : 0;
  }

  latestProject() {
    return [...this.store.projects()].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt))[0] ?? null;
  }

  latestProjectDate(): string {
    const latest = this.latestProject();
    return latest ? new Date(latest.updatedAt).toLocaleDateString('vi-VN') : '—';
  }

  initials(name: string): string {
    return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase();
  }

  statusKey(status: Project['status']): string {
    if (typeof status === 'number') {
      return ['planning', 'active', 'paused', 'completed', 'cancelled'][status] ?? '';
    }
    return String(status ?? '').trim().toLowerCase();
  }

  statusLabel(status: Project['status']): string {
    const key = this.statusKey(status);
    const label = ({
      planning: 'status.planning', active: 'status.active', paused: 'status.paused', completed: 'status.completed', cancelled: 'status.cancelled', draft: 'status.draft',
    } as Record<string, string>)[key];
    return label ? this.i18n.t(label) : String(status ?? '');
  }

  openCreate(): void {
    if (!this.quotaNotice.checkProjectCreation(this.store.profile().quota)) return;
    this.error.set(null);
    this.createOpen.set(true);
  }

  closeCreate(): void {
    if (!this.saving()) this.createOpen.set(false);
  }

  createProject(): void {
    if (!this.draft.name.trim() || this.saving()) return;
    if (!this.quotaNotice.checkProjectCreation(this.store.profile().quota)) return;
    if (this.draft.startAt && this.draft.endAt && this.draft.endAt < this.draft.startAt) {
      this.error.set('Ngày kết thúc phải sau ngày bắt đầu.');
      return;
    }
    this.saving.set(true);
    this.error.set(null);
    this.api
      .createProject({
        name: this.draft.name.trim(),
        description: this.draft.description.trim(),
        startAt: this.draft.startAt ? new Date(this.draft.startAt).toISOString() : null,
        endAt: this.draft.endAt ? new Date(this.draft.endAt).toISOString() : null,
      })
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: (project) => {
          this.store.projects.update((items) => [project, ...items]);
          void this.router.navigate(['/projects', project.id, 'overview']);
        },
        error: (error) => {
          if (!this.quotaNotice.isQuotaError(error)) {
            this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể tạo project.');
          }
        },
      });
  }
}
