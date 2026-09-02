import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { LucideBell, LucideCheck, LucideInbox, LucideX } from '@lucide/angular';
import { finalize } from 'rxjs';
import { UserNotification } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

@Component({
  selector: 'app-notifications-page',
  imports: [DatePipe, LucideBell, LucideCheck, LucideInbox, LucideX],
  templateUrl: './notifications.page.html',
  styleUrl: './notifications.page.css',
})
export class NotificationsPage implements OnInit {
  readonly notifications = signal<UserNotification[]>([]);
  readonly loading = signal(true);
  readonly busyId = signal<string | null>(null);
  readonly feedback = signal<string | null>(null);
  private readonly api = inject(PlanoraApiService);
  private readonly store = inject(WorkspaceStore);

  ngOnInit(): void {
    this.api.getNotifications(false, undefined, true)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (notifications) => this.notifications.set(notifications),
        error: () => this.feedback.set('Không thể tải lịch sử thông báo.'),
      });
  }

  markRead(notification: UserNotification): void {
    if (notification.readAt) return;
    this.api.markNotificationRead(notification.id).subscribe({
      next: () => this.updateNotification(notification.id, { readAt: new Date().toISOString() }),
      error: () => this.feedback.set('Không thể đánh dấu thông báo đã đọc.'),
    });
  }

  respond(notification: UserNotification, accept: boolean): void {
    if (!notification.entityId || this.busyId()) return;
    this.busyId.set(notification.id);
    this.feedback.set(null);
    const request = accept
      ? this.api.acceptProjectInvitation(notification.entityId)
      : this.api.rejectProjectInvitation(notification.entityId);
    request.pipe(finalize(() => this.busyId.set(null))).subscribe({
      next: () => {
        const changes = { readAt: new Date().toISOString(), isActionable: false };
        this.updateNotification(notification.id, changes);
        this.store.notifications.update((items) => items.map((item) => item.id === notification.id ? { ...item, ...changes } : item));
        this.store.reloadProjects();
        this.feedback.set(accept ? 'Đã tham gia project.' : 'Đã từ chối lời mời.');
      },
      error: (error) => this.feedback.set(error.error?.errors?.[0]?.message ?? 'Không thể xử lý lời mời.'),
    });
  }

  private updateNotification(id: string, changes: Partial<UserNotification>): void {
    this.notifications.update((items) => items.map((item) => item.id === id ? { ...item, ...changes } : item));
  }
}
