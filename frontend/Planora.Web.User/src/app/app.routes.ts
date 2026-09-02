import { Routes } from '@angular/router';
import { authenticationGuard } from './core/auth/authentication.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register.page').then((m) => m.RegisterPage),
  },
  {
    path: 'forgot-password',
    loadComponent: () =>
      import('./features/auth/forgot-password.page').then((m) => m.ForgotPasswordPage),
  },
  {
    path: 'reset-password',
    loadComponent: () =>
      import('./features/auth/reset-password.page').then((m) => m.ResetPasswordPage),
  },
  {
    path: 'terms',
    loadComponent: () => import('./features/legal/terms.page').then((m) => m.TermsPage),
  },
  {
    path: '',
    canActivate: [authenticationGuard],
    loadComponent: () => import('./shell/workspace.shell').then((m) => m.WorkspaceShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'projects' },
      {
        path: 'projects',
        title: 'Dự án · Planora',
        data: { title: 'Dự án', eyebrow: 'Workspace cá nhân' },
        loadComponent: () => import('./features/projects/projects.page').then((m) => m.ProjectsPage),
      },
      {
        path: 'projects/:projectId',
        loadComponent: () => import('./shell/project.shell').then((m) => m.ProjectShell),
        children: [
          { path: '', pathMatch: 'full', redirectTo: 'overview' },
          {
            path: 'overview',
            title: 'Tổng quan dự án · Planora',
            data: { title: 'Tổng quan' },
            loadComponent: () =>
              import('./features/project-overview/project-overview.page').then(
                (m) => m.ProjectOverviewPage,
              ),
          },
          {
            path: 'tasks',
            title: 'Công việc · Planora',
            data: { title: 'Công việc' },
            loadComponent: () =>
              import('./features/project-tasks/project-tasks.page').then((m) => m.ProjectTasksPage),
          },
          {
            path: 'sprints',
            title: 'Sprint · Planora',
            data: { title: 'Sprint' },
            loadComponent: () =>
              import('./features/project-sprints/project-sprints.page').then(
                (m) => m.ProjectSprintsPage,
              ),
          },
          {
            path: 'views',
            title: 'Góc nhìn · Planora',
            data: { title: 'Góc nhìn' },
            loadComponent: () =>
              import('./features/project-views/project-views.page').then((m) => m.ProjectViewsPage),
          },
          {
            path: 'files',
            title: 'Tệp & tài liệu · Planora',
            data: { title: 'Tệp & tài liệu' },
            loadComponent: () =>
              import('./features/project-files/project-files.page').then((m) => m.ProjectFilesPage),
          },
          {
            path: 'members',
            title: 'Thành viên · Planora',
            data: { title: 'Thành viên' },
            loadComponent: () =>
              import('./features/project-members/project-members.page').then(
                (m) => m.ProjectMembersPage,
              ),
          },
          {
            path: 'analytics',
            title: 'Phân tích · Planora',
            data: { title: 'Phân tích' },
            loadComponent: () =>
              import('./features/project-analytics/project-analytics.page').then(
                (m) => m.ProjectAnalyticsPage,
              ),
          },
          {
            path: 'settings',
            title: 'Cài đặt dự án · Planora',
            data: { title: 'Cài đặt dự án' },
            loadComponent: () =>
              import('./features/project-settings/project-settings.page').then(
                (m) => m.ProjectSettingsPage,
              ),
          },
        ],
      },
      {
        path: 'notifications',
        title: 'Thông báo · Planora',
        data: { title: 'Thông báo', eyebrow: 'Hộp thư 7 ngày' },
        loadComponent: () =>
          import('./features/notifications/notifications.page').then((m) => m.NotificationsPage),
      },
      {
        path: 'account',
        title: 'Tài khoản · Planora',
        data: { title: 'Tài khoản', eyebrow: 'Cá nhân & bảo mật' },
        loadComponent: () => import('./features/account/account.page').then((m) => m.AccountPage),
      },
      {
        path: 'billing',
        title: 'Gói & thanh toán · Planora',
        data: { title: 'Gói & thanh toán', eyebrow: 'Tài khoản' },
        loadComponent: () => import('./features/billing/billing.page').then((m) => m.BillingPage),
      },
      {
        path: 'support',
        title: 'Hỗ trợ · Planora',
        data: { title: 'Hỗ trợ', eyebrow: 'Tài khoản' },
        loadComponent: () => import('./features/support/support.page').then((m) => m.SupportPage),
      },
      {
        path: 'guide',
        title: 'Hướng dẫn · Planora',
        data: { title: 'Hướng dẫn', eyebrow: 'Trung tâm trợ giúp' },
        loadComponent: () => import('./features/guide/guide.page').then((m) => m.GuidePage),
      },
      { path: 'overview', redirectTo: 'projects' },
      { path: 'tasks', redirectTo: 'projects' },
      { path: 'sprints', redirectTo: 'projects' },
      { path: 'views', redirectTo: 'projects' },
      { path: 'files', redirectTo: 'projects' },
      { path: 'members', redirectTo: 'projects' },
      { path: 'analytics', redirectTo: 'projects' },
      { path: 'settings', redirectTo: 'account' },
    ],
  },
  { path: '**', redirectTo: 'projects' },
];
