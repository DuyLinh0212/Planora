import { DatePipe, NgTemplateOutlet } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { LucideChevronLeft, LucideChevronRight, LucideRefreshCw } from '@lucide/angular';
import { ProjectActivity, ProjectMember, ProjectTask } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { I18nService, TranslatePipe } from '../../core/i18n/i18n.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

type ProjectView = 'list' | 'board' | 'sprint' | 'backlog' | 'calendar' | 'timeline' | 'gantt' | 'workload' | 'dependency' | 'milestone' | 'activity';

@Component({
  selector: 'app-project-views-page',
  imports: [DatePipe, NgTemplateOutlet, TranslatePipe, LucideChevronLeft, LucideChevronRight, LucideRefreshCw],
  templateUrl: './project-views.page.html',
  styleUrl: './project-views.page.css',
})
export class ProjectViewsPage {
  readonly store = inject(WorkspaceStore);
  private readonly api = inject(PlanoraApiService);
  private readonly i18n = inject(I18nService);
  readonly view = signal<ProjectView>('list');
  readonly selectedSprintId = signal<string | null>(null);
  readonly calendarCursor = signal(new Date());
  readonly activity = signal<ProjectActivity[]>([]);
  readonly activityLoading = signal(false);
  readonly activityError = signal<string | null>(null);
  private activityProjectId: string | null = null;
  readonly viewOptions: ReadonlyArray<{ id: ProjectView; key: string }> = [
    { id: 'list', key: 'views.list' }, { id: 'board', key: 'views.board' }, { id: 'sprint', key: 'views.sprint' }, { id: 'backlog', key: 'views.backlog' }, { id: 'calendar', key: 'views.calendar' }, { id: 'timeline', key: 'views.timeline' }, { id: 'gantt', key: 'views.gantt' }, { id: 'workload', key: 'views.workload' }, { id: 'dependency', key: 'views.dependency' }, { id: 'milestone', key: 'views.milestone' }, { id: 'activity', key: 'views.activity' },
  ];
  readonly boardStatuses = ['Todo', 'InProgress', 'Submitted', 'Rework', 'Done'];
  readonly week = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  readonly rangeStart = computed(() => this.calculateRange()[0]);
  readonly rangeEnd = computed(() => this.calculateRange()[1]);
  readonly selectedSprintName = computed(() => this.store.sprints().find((item) => item.id === this.selectedSprintId())?.name ?? 'Chọn Sprint');
  readonly sprintTasks = computed(() => this.store.tasks().filter((task) => task.sprintId === this.selectedSprintId()));
  readonly backlogTasks = computed(() => this.store.tasks().filter((task) => !task.sprintId));
  readonly milestones = computed(() => this.store.tasks().filter((task) => task.isMilestone).sort((a, b) => this.time(a.effectiveDueAt) - this.time(b.effectiveDueAt)));
  readonly activeTaskCount = computed(() => this.store.tasks().filter((task) => !['done', 'cancelled'].includes(this.normalizedStatus(task.status))).length);
  readonly dependencyRows = computed(() => this.store.tasks().filter((task) => task.dependsOnTaskId).map((task) => ({ task, blocker: this.store.tasks().find((candidate) => candidate.id === task.dependsOnTaskId) })));
  readonly workloadRows = computed(() => this.store.members().map((member) => this.workloadFor(member)));
  readonly calendarDays = computed(() => {
    const month = this.calendarCursor(); const first = new Date(month.getFullYear(), month.getMonth(), 1); const mondayOffset = (first.getDay() + 6) % 7; const start = new Date(first); start.setDate(first.getDate() - mondayOffset);
    return Array.from({ length: 42 }, (_, index) => { const date = new Date(start); date.setDate(start.getDate() + index); return { key: date.toISOString(), date, inMonth: date.getMonth() === month.getMonth(), isToday: this.sameDay(date, new Date()) }; });
  });

  constructor() { effect(() => { const sprints = this.store.sprints(); if (!this.selectedSprintId() && sprints.length) this.selectedSprintId.set(sprints.find((sprint) => sprint.status === 'Active')?.id ?? sprints[0].id); if (this.view() === 'activity' && this.store.project().id && this.activityProjectId !== this.store.project().id) this.loadActivity(true); }); }
  label(key: string): string { return this.i18n.t(key); }
  shiftMonth(amount: number): void { const next = new Date(this.calendarCursor()); next.setMonth(next.getMonth() + amount); this.calendarCursor.set(next); }
  tasksByStatusFrom(tasks: ProjectTask[], status: string): ProjectTask[] { return tasks.filter((task) => this.normalizedStatus(task.status) === this.normalizedStatus(status)); }
  tasksOnDate(day: Date): ProjectTask[] { return this.store.tasks().filter((task) => task.effectiveDueAt && this.sameDay(new Date(task.effectiveDueAt), day)); }
  sprintName(id: string | null): string { return this.store.sprints().find((sprint) => sprint.id === id)?.name ?? 'Backlog'; }
  shortId(id: string): string { return id.length > 10 ? id.slice(0, 8).toUpperCase() : id; }
  normalizedStatus(status: string): string { return status.replace(/[^a-z]/gi, '').toLowerCase(); }
  statusLabel(status: string): string { return ({ todo: 'Cần làm', inprogress: 'Đang làm', submitted: 'Chờ duyệt', rework: 'Làm lại', done: 'Hoàn thành', expired: 'Quá hạn', cancelled: 'Đã hủy', unknown: 'Không rõ' } as Record<string, string>)[this.normalizedStatus(status)] ?? status; }
  priorityLabel(priority: string): string { return ({ low: 'Thấp', medium: 'Trung bình', high: 'Cao', urgent: 'Khẩn cấp' } as Record<string, string>)[priority.toLowerCase()] ?? priority; }
  initials(value: string): string { return value.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase(); }
  position(value: string | null): number { const [start, end] = this.calculateRange(); return this.clamp(((this.time(value) - start.getTime()) / Math.max(1, end.getTime() - start.getTime())) * 100, 0, 100); }
  duration(startValue: string | null, endValue: string | null): number { return this.clamp(this.position(endValue) - this.position(startValue), 1.5, 100 - this.position(startValue)); }
  taskStart(task: ProjectTask): number { const sprint = this.store.sprints().find((item) => item.id === task.sprintId); const fallback = task.effectiveDueAt ? new Date(this.time(task.effectiveDueAt) - 3 * 86_400_000).toISOString() : null; return this.position(sprint?.startAt ?? fallback); }
  taskDuration(task: ProjectTask): number { const start = this.taskStart(task); return this.clamp(this.position(task.effectiveDueAt) - start, 1.5, 100 - start); }
  taskProgress(task: ProjectTask): number { return ({ todo: 8, inprogress: 48, submitted: 82, rework: 62, done: 100, expired: 35, cancelled: 0 } as Record<string, number>)[this.normalizedStatus(task.status)] ?? 0; }
  scaleLabels(): Array<{ left: number; value: Date }> { const [start, end] = this.calculateRange(); return Array.from({ length: 5 }, (_, index) => ({ left: index * 25, value: new Date(start.getTime() + (end.getTime() - start.getTime()) * index / 4) })); }
  activityLabel(action: string): string { return ({ 'project.created': 'Đã tạo project', 'project.updated': 'Đã cập nhật project', 'task.created': 'Đã tạo công việc', 'task.updated': 'Đã chỉnh sửa công việc', 'task.deleted': 'Đã xóa công việc', 'task.assigned': 'Đã giao công việc', 'task.submitted': 'Đã nộp công việc', 'submission.approved': 'Đã duyệt bài nộp', 'submission.rework_requested': 'Đã yêu cầu làm lại', 'invitation.created': 'Đã gửi lời mời', 'invitation.accepted': 'Đã chấp nhận lời mời', 'member.role_changed': 'Đã đổi vai trò thành viên' } as Record<string, string>)[action] ?? action.replaceAll('.', ' · '); }
  loadActivity(force = false): void { const projectId = this.store.project().id; if (!projectId || this.activityLoading() || (!force && this.activityProjectId === projectId)) return; this.activityLoading.set(true); this.activityError.set(null); this.api.getProjectActivity(projectId).subscribe({ next: (items) => { this.activity.set(items); this.activityProjectId = projectId; this.activityLoading.set(false); }, error: (error) => { this.activityError.set(error.error?.errors?.[0]?.message ?? 'Không thể tải lịch sử hoạt động.'); this.activityLoading.set(false); } }); }
  private workloadFor(member: ProjectMember) { const tasks = this.store.tasks().filter((task) => task.assigneeMemberIds.includes(member.membershipId)); const count = (status: string) => tasks.filter((task) => this.normalizedStatus(task.status) === status).length; const open = tasks.filter((task) => !['done', 'cancelled'].includes(this.normalizedStatus(task.status))).length; return { member, inProgress: count('inprogress'), submitted: count('submitted'), expired: count('expired'), open, load: this.clamp((open / 7) * 100, 0, 100) }; }
  private calculateRange(): [Date, Date] { const values = [...this.store.sprints().flatMap((sprint) => [this.time(sprint.startAt), this.time(sprint.endAt)]), ...this.store.tasks().map((task) => this.time(task.effectiveDueAt))].filter((value) => Number.isFinite(value) && value > 0); const now = Date.now(); const start = values.length ? Math.min(...values) : now; const end = values.length ? Math.max(...values) : now + 14 * 86_400_000; return [new Date(start), new Date(Math.max(end, start + 86_400_000))]; }
  private time(value: string | null | undefined): number { return value ? new Date(value).getTime() : Number.NaN; }
  private clamp(value: number, min: number, max: number): number { return Math.min(max, Math.max(min, Number.isFinite(value) ? value : min)); }
  private sameDay(left: Date, right: Date): boolean { return left.getFullYear() === right.getFullYear() && left.getMonth() === right.getMonth() && left.getDate() === right.getDate(); }
}
