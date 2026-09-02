import { Component, computed, inject } from '@angular/core';
import { LucideCircleCheckBig, LucideClock3, LucideGauge, LucideTriangleAlert } from '@lucide/angular';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

@Component({
  selector: 'app-project-analytics-page',
  imports: [LucideCircleCheckBig, LucideClock3, LucideGauge, LucideTriangleAlert],
  templateUrl: './project-analytics.page.html',
  styleUrl: './project-analytics.page.css',
})
export class ProjectAnalyticsPage {
  readonly store = inject(WorkspaceStore);
  readonly trendValues = [12, 19, 17, 31, 38, 46, 43, 61, 68, 77, 83, 92];
  readonly doneCount = computed(() => this.store.tasks().filter((task) => task.status.toLowerCase() === 'done').length);
  readonly activeCount = computed(() => this.store.tasks().filter((task) => ['todo', 'inprogress'].includes(task.status.toLowerCase())).length);
  readonly riskCount = computed(() => this.store.tasks().filter((task) => ['rework', 'expired'].includes(task.status.toLowerCase())).length);
  completionRate(): number { return this.store.tasks().length ? Math.round(this.doneCount() / this.store.tasks().length * 100) : 0; }
  onTimeRate(): number { const dated = this.store.tasks().filter((task) => task.effectiveDueAt); if (!dated.length) return 100; const onTime = dated.filter((task) => task.status.toLowerCase() === 'done' || new Date(task.effectiveDueAt!).getTime() >= Date.now()).length; return Math.round(onTime / dated.length * 100); }
  healthScore(): number { return Math.max(0, Math.min(100, Math.round(this.completionRate() * .55 + this.onTimeRate() * .35 + (100 - this.riskCount() * 8) * .1))); }
  statusData() { const labels = ['Todo', 'InProgress', 'Submitted', 'Rework', 'Done', 'Expired']; const total = Math.max(1, this.store.tasks().length); return labels.map((label) => { const value = this.store.tasks().filter((task) => task.status.toLowerCase() === label.toLowerCase()).length; return { label, value, percent: Math.round(value / total * 100) }; }); }
  initials(value: string): string { return value.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase(); }
}
