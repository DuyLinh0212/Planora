import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import {
  LucideBell,
  LucideBookOpen,
  LucideBriefcaseBusiness,
  LucideCreditCard,
  LucideLifeBuoy,
  LucideMenu,
  LucidePlus,
  LucideSettings,
  LucideTrash2,
  LucideX,
} from '@lucide/angular';
import { filter, finalize } from 'rxjs';
import { UserNotification } from '../core/api/api.models';
import { PlanoraApiService } from '../core/api/planora-api.service';
import { WorkspaceStore } from '../core/workspace/workspace.store';
import { RealtimeNotificationService } from '../core/realtime/realtime-notification.service';
import { PlanoraLogoComponent } from '../shared/planora-logo.component';
import { TranslatePipe } from '../core/i18n/i18n.service';

@Component({
  selector: 'app-workspace-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    LucideBell,
    LucideBookOpen,
    LucideBriefcaseBusiness,
    LucideCreditCard,
    LucideLifeBuoy,
    LucideMenu,
    LucidePlus,
    LucideSettings,
    LucideTrash2,
    LucideX,
    PlanoraLogoComponent,
    TranslatePipe,
  ],
  templateUrl: './workspace.shell.html',
  styleUrl: './workspace.shell.css',
})
export class WorkspaceShell {
  readonly store = inject(WorkspaceStore);
  readonly menuOpen = signal(false);
  readonly notificationsOpen = signal(false);
  readonly notificationActionBusy = signal<string | null>(null);
  readonly notificationFeedback = signal<string | null>(null);
  readonly title = signal('Dự án');
  readonly eyebrow = signal('Workspace cá nhân');
  readonly isProjectRoute = signal(false);
  readonly isProjectsPage = signal(true);
  readonly compactNotifications = computed(() => this.store.notifications().slice(0, 5));
  readonly initials = computed(() =>
    this.store
      .profile()
      .displayName.split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase(),
  );

  private readonly router = inject(Router);
  private readonly api = inject(PlanoraApiService);
  private readonly realtime = inject(RealtimeNotificationService);

  constructor() {
    this.store.loadAccount();
    void this.realtime.start();
    const updateRouteState = () => {
      let route = this.router.routerState.root;
      while (route.firstChild) route = route.firstChild;
      this.title.set(route.snapshot.data['title'] ?? 'Planora');
      this.eyebrow.set(route.snapshot.data['eyebrow'] ?? 'Đang trong project');
      this.isProjectRoute.set(/^\/projects\/[^/]+/.test(this.router.url));
      this.isProjectsPage.set(/^\/projects(?:\?|$)/.test(this.router.url));
      this.menuOpen.set(false);
      this.notificationsOpen.set(false);
    };
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe(updateRouteState);
    queueMicrotask(updateRouteState);
  }

  markRead(notificationId: string): void {
    const notification = this.store.notifications().find((item) => item.id === notificationId);
    if (!notification || notification.readAt) return;
    this.api.markNotificationRead(notificationId).subscribe(() =>
      this.store.notifications.update((items) =>
        items.map((item) =>
          item.id === notificationId ? { ...item, readAt: new Date().toISOString() } : item,
        ),
      ),
    );
  }

  isPendingProjectInvitation(notification: UserNotification): boolean {
    return notification.type === 'project.invitation' && Boolean(notification.entityId) && notification.isActionable;
  }

  respondToInvitation(notification: UserNotification, accept: boolean): void {
    if (!notification.entityId || this.notificationActionBusy()) return;
    this.notificationActionBusy.set(notification.id);
    this.notificationFeedback.set(null);
    const request = accept
      ? this.api.acceptProjectInvitation(notification.entityId)
      : this.api.rejectProjectInvitation(notification.entityId);

    request.pipe(finalize(() => this.notificationActionBusy.set(null))).subscribe({
      next: () => {
        this.store.notifications.update((items) =>
          items.map((item) =>
            item.id === notification.id ? { ...item, readAt: new Date().toISOString(), isActionable: false } : item,
          ),
        );
        this.store.reloadProjects();
        this.notificationFeedback.set(accept ? 'Đã tham gia project.' : 'Đã từ chối lời mời.');
        if (accept) {
          this.notificationsOpen.set(false);
          void this.router.navigate(['/projects']);
        }
      },
      error: (error) => this.notificationFeedback.set(
        error.error?.errors?.[0]?.message ?? 'Không thể xử lý lời mời. Hãy thử lại.',
      ),
    });
  }

  deleteNotification(notification: UserNotification): void {
    if (this.notificationActionBusy()) return;
    this.notificationActionBusy.set(notification.id);
    this.notificationFeedback.set(null);
    this.api.deleteNotification(notification.id)
      .pipe(finalize(() => this.notificationActionBusy.set(null)))
      .subscribe({
        next: () => {
          this.store.notifications.update((items) => items.filter((item) => item.id !== notification.id));
          this.notificationFeedback.set('Đã ẩn khỏi hộp thư. Bạn vẫn xem được trong trang Thông báo.');
        },
        error: (error) => this.notificationFeedback.set(
          error.error?.errors?.[0]?.message ?? 'Không thể xóa thông báo. Hãy thử lại.',
        ),
      });
  }

  toggleNotifications(): void {
    this.notificationFeedback.set(null);
    this.notificationsOpen.set(!this.notificationsOpen());
  }
}
