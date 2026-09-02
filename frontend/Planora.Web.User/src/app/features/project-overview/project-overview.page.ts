import { DatePipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  LucideArrowRight,
  LucideCalendarRange,
  LucideCircleCheckBig,
  LucideClock3,
  LucideFileStack,
  LucideListChecks,
  LucideUsersRound,
} from '@lucide/angular';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

@Component({
  selector: 'app-project-overview-page',
  imports: [
    DatePipe,
    RouterLink,
    LucideArrowRight,
    LucideCalendarRange,
    LucideCircleCheckBig,
    LucideClock3,
    LucideFileStack,
    LucideListChecks,
    LucideUsersRound,
  ],
  templateUrl: './project-overview.page.html',
  styleUrl: './project-overview.page.css',
})
export class ProjectOverviewPage {
  readonly store = inject(WorkspaceStore);
  readonly doneCount = computed(() => this.store.tasks().filter((task) => task.status.toLowerCase() === 'done').length);
  readonly reviewCount = computed(() => this.store.tasks().filter((task) => ['submitted', 'rework'].includes(task.status.toLowerCase())).length);
  readonly activeCount = computed(() => this.store.tasks().filter((task) => ['todo', 'inprogress'].includes(task.status.toLowerCase())).length);
  readonly stages = computed(() => [
    { key: 'goal', value: this.store.activeSprint().id ? '01' : '—', label: 'Mục tiêu sprint', help: this.store.activeSprint().id ? 'Đã xác định' : 'Chưa tạo sprint' },
    { key: 'active', value: this.activeCount(), label: 'Đang thực hiện', help: 'To do + In progress' },
    { key: 'review', value: this.reviewCount(), label: 'Đang duyệt', help: 'Submitted + Rework' },
    { key: 'done', value: this.doneCount(), label: 'Đã xác minh', help: 'Công việc hoàn tất' },
  ]);

  sprintTitle(): string {
    return this.store.activeSprint().id ? this.store.activeSprint().name : 'Chưa có sprint đang hoạt động';
  }

  progress(): number {
    const total = this.store.tasks().length;
    return total ? Math.round((this.doneCount() / total) * 100) : 0;
  }

  remainingCount(): number {
    return Math.max(0, this.store.tasks().length - this.doneCount());
  }

  attentionTasks() {
    return [...this.store.tasks()]
      .filter((task) => task.status.toLowerCase() !== 'done')
      .sort((a, b) => (a.effectiveDueAt ?? '9999').localeCompare(b.effectiveDueAt ?? '9999'));
  }

  fileCount(): number {
    return this.store.storage().files.length + this.store.storage().documents.length;
  }

  projectLink(section: string): string[] {
    return ['/projects', this.store.project().id, section];
  }
}
