import { Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import {
  LucideAlertTriangle,
  LucideArrowLeft,
  LucideCalendar,
  LucideCheck,
  LucideClock,
  LucideFileText,
  LucideFlag,
  LucidePause,
  LucidePlay,
  LucideSave,
  LucideShoppingBag,
  LucideTrash2,
  LucideUsers,
  LucideX,
} from '@lucide/angular';
import { finalize } from 'rxjs';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

export interface ProjectStatusOption {
  value: string;
  label: string;
  desc: string;
  iconKey: 'planning' | 'paused' | 'active' | 'completed' | 'cancelled';
}

@Component({
  selector: 'app-project-settings-page',
  imports: [
    FormsModule,
    RouterLink,
    LucideAlertTriangle,
    LucideArrowLeft,
    LucideCalendar,
    LucideCheck,
    LucideClock,
    LucideFileText,
    LucideFlag,
    LucidePause,
    LucidePlay,
    LucideSave,
    LucideShoppingBag,
    LucideTrash2,
    LucideUsers,
    LucideX,
  ],
  templateUrl: './project-settings.page.html',
  styleUrl: './project-settings.page.css',
})
export class ProjectSettingsPage {
  readonly store = inject(WorkspaceStore);
  name = '';
  description = '';
  startAt = '';
  endAt = '';
  status = 'Planning';
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly toast = signal<string | null>(null);

  readonly statusOptions: ProjectStatusOption[] = [
    {
      value: 'Planning',
      label: 'Lập kế hoạch',
      desc: 'Dự án đang trong giai đoạn lập kế hoạch và chuẩn bị.',
      iconKey: 'planning',
    },
    {
      value: 'Paused',
      label: 'Tạm dừng',
      desc: 'Dự án tạm dừng và chưa tiếp tục thực hiện.',
      iconKey: 'paused',
    },
    {
      value: 'Active',
      label: 'Đang hoạt động',
      desc: 'Dự án đang được triển khai và thực hiện.',
      iconKey: 'active',
    },
    {
      value: 'Completed',
      label: 'Hoàn thành',
      desc: 'Dự án đã hoàn thành tất cả các công việc.',
      iconKey: 'completed',
    },
    {
      value: 'Cancelled',
      label: 'Đã hủy',
      desc: 'Dự án đã bị hủy và không tiếp tục thực hiện.',
      iconKey: 'cancelled',
    },
  ];

  private initializedFor = '';
  private readonly api = inject(PlanoraApiService);
  private readonly router = inject(Router);

  constructor() {
    effect(() => {
      const project = this.store.project();
      if (!project.id || this.initializedFor === project.id) return;
      this.initializedFor = project.id;
      this.name = project.name;
      this.description = project.description;
      this.startAt = project.startAt?.slice(0, 10) ?? '';
      this.endAt = project.endAt?.slice(0, 10) ?? '';
      this.status = this.normalizeStatus(project.status);
    });
  }

  normalizeStatus(status: unknown): string {
    if (typeof status === 'number') {
      return ['Planning', 'Active', 'Paused', 'Completed', 'Cancelled'][status] ?? 'Planning';
    }
    const s = String(status || 'Planning').trim();
    return s.charAt(0).toUpperCase() + s.slice(1).toLowerCase();
  }

  statusKey(status: unknown): string {
    return this.normalizeStatus(status).toLowerCase();
  }

  statusLabel(status: unknown): string {
    const normalized = this.normalizeStatus(status);
    const opt = this.statusOptions.find((o) => o.value.toLowerCase() === normalized.toLowerCase());
    return opt?.label ?? normalized;
  }

  setStatus(newStatus: string): void {
    if (!this.store.hasPermission('project.edit')) return;
    this.status = newStatus;
  }

  clearDate(field: 'startAt' | 'endAt'): void {
    if (!this.store.hasPermission('project.edit')) return;
    this[field] = '';
  }

  formatDisplayDate(iso: string | null | undefined): string {
    if (!iso) return 'Chưa đặt';
    try {
      const d = new Date(iso);
      if (isNaN(d.getTime())) return 'Chưa đặt';
      return d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
    } catch {
      return 'Chưa đặt';
    }
  }

  cancelEdit(): void {
    const project = this.store.project();
    this.name = project.name;
    this.description = project.description;
    this.startAt = project.startAt?.slice(0, 10) ?? '';
    this.endAt = project.endAt?.slice(0, 10) ?? '';
    this.status = this.normalizeStatus(project.status);
    this.error.set(null);
  }

  shortId(): string {
    return this.store.project().id.slice(0, 8).toUpperCase();
  }

  save(): void {
    if (!this.store.hasPermission('project.edit') || !this.name.trim() || this.saving()) return;
    if (this.startAt && this.endAt && this.endAt < this.startAt) {
      return this.error.set('Ngày kết thúc phải sau ngày bắt đầu.');
    }
    this.saving.set(true);
    this.error.set(null);
    const draft = {
      name: this.name.trim(),
      description: this.description.trim(),
      startAt: this.startAt ? new Date(this.startAt).toISOString() : null,
      endAt: this.endAt ? new Date(this.endAt).toISOString() : null,
      status: this.status,
    };
    this.api
      .updateProject(this.store.project().id, draft)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.store.project.update((project) => ({ ...project, ...draft, status: this.status }));
          this.store.projects.update((items) =>
            items.map((p) => (p.id === this.store.project().id ? { ...p, ...draft, status: this.status } : p)),
          );
          this.notify('Đã lưu cài đặt dự án.');
        },
        error: (error) => this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể lưu project.'),
      });
  }

  deleteProject(): void {
    if (!this.store.hasPermission('project.delete')) return;
    const project = this.store.project();
    if (!confirm(`Xóa project “${project.name}”?`)) return;
    this.api.deleteProject(project.id).subscribe({
      next: () => {
        this.store.projects.update((items) => items.filter((item) => item.id !== project.id));
        this.store.clearProject();
        void this.router.navigate(['/projects']);
      },
      error: (error) => this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể xóa project.'),
    });
  }

  private notify(value: string): void {
    this.toast.set(value);
    setTimeout(() => this.toast.set(null), 2400);
  }
}
