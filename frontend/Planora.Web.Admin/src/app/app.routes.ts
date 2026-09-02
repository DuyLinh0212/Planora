import { Routes } from '@angular/router';
import { administratorAuthenticationGuard } from './core/administrator-authentication.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/admin-login-page.component').then(
        (module) => module.AdminLoginPageComponent,
      ),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/admin-forgot-password-page.component').then(
        (module) => module.AdminForgotPasswordPageComponent,
      ),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/admin-reset-password-page.component').then(
        (module) => module.AdminResetPasswordPageComponent,
      ),
  },
  {
    path: '',
    loadComponent: () =>
      import('./layouts/admin-shell.component').then((module) => module.AdminShellComponent),
    canActivate: [administratorAuthenticationGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'overview' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./features/overview/admin-overview-page.component').then(
            (module) => module.AdminOverviewPageComponent,
          ),
        data: { title: 'Overview' },
      },
      {
        path: 'accounts',
        loadComponent: () =>
          import('./features/accounts/accounts-page.component').then(
            (module) => module.AccountsPageComponent,
          ),
        data: { title: 'Accounts' },
      },
      {
        path: 'plans',
        loadComponent: () =>
          import('./features/plans/plans-page.component').then(
            (module) => module.PlansPageComponent,
          ),
        data: { title: 'Plans' },
      },
      {
        path: 'payments',
        loadComponent: () =>
          import('./features/payments/payments-page.component').then(
            (module) => module.PaymentsPageComponent,
          ),
        data: { title: 'Payments' },
      },
      {
        path: 'feedback',
        loadComponent: () =>
          import('./features/feedback/feedback-page.component').then(
            (module) => module.FeedbackPageComponent,
          ),
        data: { title: 'Feedback' },
      },
      {
        path: 'support',
        loadComponent: () => import('./features/support/admin-support-page.component').then((module) => module.AdminSupportPageComponent),
        data: { title: 'Support & refunds' },
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./features/analytics/admin-analytics-page.component').then(
            (module) => module.AdminAnalyticsPageComponent,
          ),
        data: { title: 'Analytics' },
      },
      {
        path: 'activity',
        loadComponent: () =>
          import('./features/activity/admin-activity-page.component').then(
            (module) => module.AdminActivityPageComponent,
          ),
        data: { title: 'Admin activity' },
      },
      {
        path: 'recovery',
        loadComponent: () =>
          import('./features/recovery/recovery-page.component').then(
            (module) => module.RecoveryPageComponent,
          ),
        data: { title: 'Recovery' },
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/settings/admin-settings-page.component').then(
            (module) => module.AdminSettingsPageComponent,
          ),
        data: { title: 'Settings' },
      },
    ],
  },
  { path: '**', redirectTo: 'overview' },
];
