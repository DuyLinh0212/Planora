import { Injectable, computed, effect, inject, signal } from '@angular/core';
import { catchError, finalize, forkJoin, of } from 'rxjs';
import {
  MaintenanceStatus,
  Project,
  ProjectMember,
  ProjectStorage,
  ProjectTask,
  Sprint,
  UserNotification,
  UserProfile,
} from '../api/api.models';
import { PlanoraApiService } from '../api/planora-api.service';
import { accessToken } from '../auth/session.store';

const EMPTY_PROJECT: Project = {
  id: '',
  name: '',
  description: '',
  status: 'Draft',
  startAt: null,
  endAt: null,
  memberCount: 0,
  updatedAt: '',
};

const EMPTY_SPRINT: Sprint = {
  id: '',
  projectId: '',
  name: 'Chưa có sprint',
  goal: 'Tạo sprint đầu tiên để bắt đầu lập kế hoạch.',
  startAt: '',
  endAt: '',
  status: 'Draft',
};

const EMPTY_STORAGE: ProjectStorage = { folders: [], files: [], documents: [] };

const EMPTY_PROFILE: UserProfile = {
  userId: '',
  email: '',
  username: '',
  displayName: 'Planora user',
  avatarUrl: null,
  preferredLanguage: 'vi',
  themePreference: 'calm',
  timeZoneId: 'Asia/Ho_Chi_Minh',
  emailTaskNotificationsEnabled: false,
  gmailLink: {
    isLinked: false,
    gmailAddress: null,
    isServerConfigured: false,
    lastSendFailedAt: null,
    lastSendFailureReason: null,
  },
  participatingProjectCount: 0,
  quota: {
    planCode: 'FREE',
    planName: 'Free',
    ownedProjects: 0,
    maxOwnedProjects: 0,
    storageBytes: 0,
    maxStorageBytes: 0,
    maxProjectStorageBytes: 0,
    maxFileSizeBytes: 0,
    dailyUploadBytes: 0,
    dailyUploadCount: 0,
    maxMembersPerProject: 0,
    maxVersionsPerFile: 0,
    subscriptionExpiresAt: null,
    autoRenew: false,
  },
};

@Injectable({ providedIn: 'root' })
export class WorkspaceStore {
  private readonly api = inject(PlanoraApiService);
  private accountLoaded = false;
  private requestedProjectId = '';

  readonly profile = signal<UserProfile>(EMPTY_PROFILE);
  readonly projects = signal<Project[]>([]);
  readonly notifications = signal<UserNotification[]>([]);
  readonly maintenance = signal<MaintenanceStatus>({
    isEnabled: false,
    message: '',
    updatedAt: null,
  });

  readonly project = signal<Project>(EMPTY_PROJECT);
  readonly sprints = signal<Sprint[]>([]);
  readonly tasks = signal<ProjectTask[]>([]);
  readonly members = signal<ProjectMember[]>([]);
  readonly storage = signal<ProjectStorage>(EMPTY_STORAGE);
  readonly permissionCodes = signal<string[]>([]);

  readonly accountLoading = signal(false);
  readonly projectLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly hasProject = computed(() => Boolean(this.project().id));
  readonly activeSprint = computed(
    () =>
      this.sprints().find((sprint) => sprint.status === 'Active') ??
      this.sprints()[0] ??
      EMPTY_SPRINT,
  );
  readonly unreadCount = computed(
    () => this.notifications().filter((notification) => !notification.readAt).length,
  );

  constructor() {
    const cachedTheme = localStorage.getItem('planora.user.theme');
    if (cachedTheme) {
      document.documentElement.dataset['theme'] = cachedTheme;
    }
    effect(() => {
      const theme = this.profile().themePreference;
      if (theme) {
        document.documentElement.dataset['theme'] = theme;
        try {
          localStorage.setItem('planora.user.theme', theme);
        } catch {}
      }
    });
  }

  hasPermission(code: string): boolean { return this.permissionCodes().includes(code); }

  loadAccount(force = false): void {
    if (!accessToken()) return;
    if (this.accountLoaded && !force) return;
    this.accountLoaded = true;
    this.accountLoading.set(true);
    this.error.set(null);

    forkJoin({
      profile: this.api.getProfile(),
      projects: this.api.getProjects(),
      notifications: this.api.getNotifications().pipe(catchError(() => of([]))),
      maintenance: this.api
        .getMaintenance()
        .pipe(catchError(() => of({ isEnabled: false, message: '', updatedAt: null }))),
    })
      .pipe(finalize(() => this.accountLoading.set(false)))
      .subscribe({
        next: ({ profile, projects, notifications, maintenance }) => {
          this.profile.set(profile);
          this.projects.set(projects.items);
          this.notifications.set(notifications);
          this.maintenance.set(maintenance);
          document.documentElement.dataset['theme'] = profile.themePreference;
        },
        error: () => {
          this.accountLoaded = false;
          this.error.set('Không thể tải workspace. Hãy kiểm tra backend hoặc đăng nhập lại.');
        },
      });
  }

  openProject(projectId: string): void {
    if (!projectId) return this.clearProject();
    if (this.requestedProjectId === projectId && this.hasProject()) return;

    this.requestedProjectId = projectId;
    this.projectLoading.set(true);
    this.error.set(null);

    forkJoin({
      project: this.api.getProject(projectId),
      sprints: this.api.getSprints(projectId).pipe(catchError(() => of([]))),
      tasks: this.api.getTasks(projectId).pipe(catchError(() => of([]))),
      members: this.api.getMembers(projectId).pipe(catchError(() => of([]))),
      storage: this.api.getStorage(projectId).pipe(catchError(() => of(EMPTY_STORAGE))),
      capabilities: this.api.getProjectCapabilities(projectId).pipe(catchError(() => of({ permissionCodes: [] }))),
    })
      .pipe(finalize(() => this.projectLoading.set(false)))
      .subscribe({
        next: ({ project, sprints, tasks, members, storage, capabilities }) => {
          if (this.requestedProjectId !== projectId) return;
          this.project.set(project);
          this.sprints.set(sprints);
          this.tasks.set(tasks);
          this.members.set(members);
          this.storage.set(storage);
          this.permissionCodes.set(capabilities.permissionCodes);
          this.projects.update((items) =>
            items.some((item) => item.id === project.id)
              ? items.map((item) => (item.id === project.id ? project : item))
              : [project, ...items],
          );
        },
        error: () => {
          this.clearProject();
          this.error.set('Project không tồn tại hoặc bạn không có quyền truy cập.');
        },
      });
  }

  reloadProjects(): void {
    this.api.getProjects().subscribe({
      next: (response) => this.projects.set(response.items),
      error: () => this.error.set('Không thể làm mới danh sách project.'),
    });
  }

  reloadTasks(): void {
    if (!this.project().id) return;
    this.api.getTasks(this.project().id).subscribe({
      next: (tasks) => this.tasks.set(tasks),
      error: () => this.error.set('Không thể làm mới công việc.'),
    });
  }

  reloadStorage(): void {
    if (!this.project().id) return;
    this.api.getStorage(this.project().id).subscribe({
      next: (storage) => {
        this.storage.set(storage);
        this.refreshProfile();
      },
      error: () => this.error.set('Không thể làm mới kho tệp.'),
    });
  }

  refreshProfile(): void {
    if (!accessToken()) return;
    this.api.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        document.documentElement.dataset['theme'] = profile.themePreference;
      },
    });
  }

  clearProject(): void {
    this.requestedProjectId = '';
    this.project.set(EMPTY_PROJECT);
    this.sprints.set([]);
    this.tasks.set([]);
    this.members.set([]);
    this.storage.set(EMPTY_STORAGE);
    this.permissionCodes.set([]);
  }
}
