import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { LucideArchiveRestore, LucideBell, LucideChartNoAxesCombined, LucideCreditCard, LucideLayoutDashboard, LucideLogOut, LucideMenu, LucideMessageCircleMore, LucideNotebookTabs, LucideReceiptText, LucideSearch, LucideSettings, LucideShieldCheck, LucideUsersRound, LucideX } from '@lucide/angular';
import { AdminConsoleContextService } from '../core/admin-console-context.service';
import { AdminAuthSessionService } from '../core/admin-auth-session.service';

@Component({
  selector: 'app-admin-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, LucideArchiveRestore, LucideBell, LucideChartNoAxesCombined, LucideCreditCard, LucideLayoutDashboard, LucideLogOut, LucideMenu, LucideMessageCircleMore, LucideNotebookTabs, LucideReceiptText, LucideSearch, LucideSettings, LucideShieldCheck, LucideUsersRound, LucideX],
  templateUrl: './admin-shell.component.html',
  styleUrl: './admin-shell.component.css',
})
export class AdminShellComponent {
  readonly context = inject(AdminConsoleContextService);
  readonly sidebarOpen = signal(false);
  readonly notificationsOpen = signal(false);
  readonly theme = signal<'light' | 'dark' | 'calm'>(
    (localStorage.getItem('planora.admin.theme') as 'light' | 'dark' | 'calm') ?? 'light',
  );
  readonly pageTitle = signal('Overview');
  readonly adminName = signal(localStorage.getItem('planora.admin.displayName') || 'System Admin');
  readonly adminEmail = signal(localStorage.getItem('planora.admin.email') || 'admin@planora.local');

  private readonly router = inject(Router);
  private readonly authSession = inject(AdminAuthSessionService);

  constructor() {
    this.context.loadConsole();
    const updateTitle = () => {
      let route = this.router.routerState.root;
      while (route.firstChild) route = route.firstChild;
      this.pageTitle.set(route.snapshot.data['title'] ?? 'Overview');
      this.sidebarOpen.set(false);
      this.adminName.set(localStorage.getItem('planora.admin.displayName') || 'System Admin');
    };
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        takeUntilDestroyed(inject(DestroyRef)),
      )
      .subscribe(updateTitle);
    queueMicrotask(updateTitle);
  }

  get initials(): string {
    const name = this.adminName();
    const parts = name.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return name.slice(0, 2).toUpperCase() || 'SA';
  }

  setTheme(newTheme: 'light' | 'dark' | 'calm'): void {
    this.theme.set(newTheme);
    localStorage.setItem('planora.admin.theme', newTheme);
  }

  cycleTheme(): void {
    const order: Array<'light' | 'dark' | 'calm'> = ['light', 'dark', 'calm'];
    const next = order[(order.indexOf(this.theme()) + 1) % order.length];
    this.setTheme(next);
  }

  toggleNotifications(): void {
    this.notificationsOpen.update((value) => !value);
  }

  logoutCurrentAdministrator(): void {
    this.authSession.logoutCurrentAdministrator();
  }
}
