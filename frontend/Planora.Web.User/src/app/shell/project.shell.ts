import { Component, DestroyRef, computed, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  LucideArrowLeft,
  LucideCalendarRange,
  LucideChartNoAxesCombined,
  LucideFolderKanban,
  LucideLayoutDashboard,
  LucideListChecks,
  LucideSettings,
  LucideUsersRound,
  LucideWaypoints,
} from '@lucide/angular';
import { WorkspaceStore } from '../core/workspace/workspace.store';
import { TranslatePipe } from '../core/i18n/i18n.service';

@Component({
  selector: 'app-project-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    LucideArrowLeft,
    LucideCalendarRange,
    LucideChartNoAxesCombined,
    LucideFolderKanban,
    LucideLayoutDashboard,
    LucideListChecks,
    LucideSettings,
    LucideUsersRound,
    LucideWaypoints,
    TranslatePipe,
  ],
  templateUrl: './project.shell.html',
  styleUrl: './project.shell.css',
})
export class ProjectShell {
  readonly store = inject(WorkspaceStore);
  readonly projectInitials = computed(() =>
    this.store
      .project()
      .name.split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase(),
  );

  constructor() {
    inject(ActivatedRoute).paramMap
      .pipe(takeUntilDestroyed(inject(DestroyRef)))
      .subscribe((params) => this.store.openProject(params.get('projectId') ?? ''));
  }

  projectLink(section: string): string[] {
    return ['/projects', this.store.project().id, section];
  }

  statusKey(status: unknown): string {
    if (typeof status === 'number') {
      return ['planning', 'active', 'paused', 'completed', 'cancelled'][status] ?? 'planning';
    }
    return String(status ?? 'planning').trim().toLowerCase();
  }

  statusLabel(status: unknown): string {
    const key = this.statusKey(status);
    const labels: Record<string, string> = {
      planning: 'Đang lập kế hoạch',
      active: 'Đang hoạt động',
      paused: 'Tạm dừng',
      completed: 'Hoàn thành',
      cancelled: 'Đã hủy',
      draft: 'Bản nháp',
    };
    return labels[key] ?? String(status ?? '');
  }
}
