import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { map, Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthenticationResponse,
  AvailablePlan,
  PaymentCheckout,
  DocumentVersionHistory,
  GmailLinkResponse,
  MaintenanceStatus,
  PagedResponse,
  PasswordResetResponse,
  Project,
  ProjectActivity,
  ProjectCapabilities,
  ProjectDocument,
  ProjectDraft,
  ProjectFile,
  ProjectFolder,
  ProjectInvitation,
  ProjectMember,
  ProjectRole,
  ProjectRolePermissions,
  ProjectStorage,
  ProjectTask,
  RegisteredUserMatch,
  Sprint,
  SprintDraft,
  SupportConversation,
  SupportMessage,
  TaskDraft,
  TaskDeadlineChange,
  TaskExtensionRequest,
  TaskSubmissionDetail,
  TaskSubmissionLinkDraft,
  TaskSubmissionResponse,
  UserNotification,
  UserPayment,
  UserProfile,
} from './api.models';

const PROJECT_STATUSES = ['Planning', 'Active', 'Paused', 'Completed', 'Cancelled'];
const SPRINT_STATUSES = ['Planned', 'Active', 'Closed', 'Cancelled'];
const TASK_STATUSES = ['Todo', 'InProgress', 'Submitted', 'Rework', 'Done', 'Expired', 'Cancelled'];
const TASK_PRIORITIES = ['Low', 'Medium', 'High', 'Urgent'];
const SUBMISSION_REQUIREMENTS = ['Any', 'LinkOnly', 'FileOnly', 'Word', 'Excel', 'Pdf', 'PowerPoint', 'Image'];
const MEMBERSHIP_STATUSES = ['Active', 'Removed'];
const INVITATION_STATUSES = ['Pending', 'Accepted', 'Rejected', 'Expired'];
const BILLING_PERIODS = ['Forever', 'Monthly', 'Yearly'];
const PAYMENT_PROVIDERS = ['Momo', 'ZaloPay', 'BankTransfer'];
const PAYMENT_STATUSES = ['Pending', 'Success', 'Failed'];

function enumName(value: unknown, names: readonly string[]): string {
  if (typeof value === 'number') return names[value] ?? '';
  return typeof value === 'string' ? value : '';
}

@Injectable({ providedIn: 'root' })
export class PlanoraApiService {
  private readonly http = inject(HttpClient);
  private readonly api = `${environment.apiUrl}/api`;

  register(displayName: string, username: string, email: string, password: string, rememberMe = true) {
    return this.http.post<AuthenticationResponse>(`${this.api}/auth/register`, {
      displayName,
      username,
      email,
      password,
      acceptedTerms: true,
      deviceInfo: 'Planora Web.User',
      rememberMe,
    }, { withCredentials: true });
  }

  login(identifier: string, password: string, rememberMe: boolean) {
    return this.http.post<AuthenticationResponse>(`${this.api}/auth/login`, {
      identifier,
      password,
      deviceInfo: 'Planora Web.User',
      rememberMe,
    }, { withCredentials: true });
  }

  externalLogin(provider: 'google' | 'facebook', token: string, rememberMe: boolean) {
    return this.http.post<AuthenticationResponse>(`${this.api}/auth/external/${provider}`, {
      token,
      deviceInfo: 'Planora Web.User',
      rememberMe,
    }, { withCredentials: true });
  }

  refresh() {
    return this.http.post<AuthenticationResponse>(`${this.api}/auth/refresh`, {
      deviceInfo: 'Planora Web.User',
    }, { withCredentials: true });
  }

  logout() {
    return this.http.post<void>(`${this.api}/auth/logout`, {}, { withCredentials: true });
  }

  requestPasswordReset(email: string) {
    return this.http.post<PasswordResetResponse>(`${this.api}/auth/password/forgot`, { email });
  }

  resetPassword(token: string, newPassword: string) {
    return this.http.post<void>(`${this.api}/auth/password/reset`, { token, newPassword });
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<void>(`${this.api}/auth/password/change`, {
      currentPassword,
      newPassword,
    });
  }

  getProfile() {
    return this.http.get<UserProfile>(`${this.api}/profile`);
  }

  updateProfile(displayName: string) {
    return this.http.put<void>(`${this.api}/profile`, { displayName });
  }

  uploadAvatar(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ avatarUrl: string }>(`${this.api}/profile/avatar`, form);
  }

  updatePreferences(
    preferredLanguage: string,
    themePreference: string,
    timeZoneId: string,
    emailTaskNotificationsEnabled: boolean,
  ) {
    return this.http.put<void>(`${this.api}/profile/preferences`, {
      preferredLanguage,
      themePreference,
      timeZoneId,
      emailTaskNotificationsEnabled,
    });
  }

  linkGmail(code: string, redirectUri: string) {
    return this.http.post<GmailLinkResponse>(
      `${this.api}/profile/gmail-link`,
      { code, redirectUri },
      { headers: { 'X-Requested-With': 'XmlHttpRequest' } },
    );
  }

  unlinkGmail() {
    return this.http.delete<void>(`${this.api}/profile/gmail-link`);
  }

  getNotifications(unreadOnly = false, limit?: number, includeDismissed = false) {
    let params = new HttpParams()
      .set('unreadOnly', unreadOnly)
      .set('includeDismissed', includeDismissed);
    if (limit !== undefined) params = params.set('limit', limit);
    return this.http.get<UserNotification[]>(`${this.api}/notifications`, { params });
  }

  markNotificationRead(notificationId: string) {
    return this.http.post<void>(`${this.api}/notifications/${notificationId}/read`, {});
  }

  deleteNotification(notificationId: string) {
    return this.http.delete<void>(`${this.api}/notifications/${notificationId}`);
  }

  acceptProjectInvitation(invitationId: string) {
    return this.http.post<void>(`${this.api}/project-invitations/${invitationId}/accept`, {});
  }

  rejectProjectInvitation(invitationId: string) {
    return this.http.post<void>(`${this.api}/project-invitations/${invitationId}/reject`, {});
  }

  getMaintenance() {
    return this.http.get<MaintenanceStatus>(`${this.api}/system/maintenance`);
  }

  getProjects(page = 1, pageSize = 50) {
    return this.http
      .get<PagedResponse<Project>>(`${this.api}/projects`, {
        params: new HttpParams().set('page', page).set('pageSize', pageSize),
      })
      .pipe(map((response) => ({ ...response, items: response.items.map((project) => this.normalizeProject(project)) })));
  }

  getProject(projectId: string) {
    return this.http.get<Project>(`${this.api}/projects/${projectId}`).pipe(map((project) => this.normalizeProject(project)));
  }

  createProject(draft: ProjectDraft) {
    return this.http.post<Project>(`${this.api}/projects`, draft).pipe(map((project) => this.normalizeProject(project)));
  }

  updateProject(projectId: string, draft: ProjectDraft) {
    return this.http.patch<void>(`${this.api}/projects/${projectId}`, draft);
  }

  deleteProject(projectId: string) {
    return this.http.delete<void>(`${this.api}/projects/${projectId}`);
  }

  getProjectActivity(projectId: string, take = 100) {
    return this.http.get<ProjectActivity[]>(`${this.api}/projects/${projectId}/activity`, {
      params: new HttpParams().set('take', take),
    });
  }

  getSprints(projectId: string) {
    return this.http
      .get<Sprint[]>(`${this.api}/projects/${projectId}/sprints`)
      .pipe(map((sprints) => sprints.map((sprint) => this.normalizeSprint(sprint))));
  }

  createSprint(projectId: string, draft: SprintDraft) {
    return this.http
      .post<Sprint>(`${this.api}/projects/${projectId}/sprints`, draft)
      .pipe(map((sprint) => this.normalizeSprint(sprint)));
  }

  startSprint(sprintId: string) {
    return this.http.post<void>(`${this.api}/sprints/${sprintId}/start`, {});
  }

  closeSprint(sprintId: string) {
    return this.http.post<void>(`${this.api}/sprints/${sprintId}/close`, {});
  }

  getTasks(projectId: string, sprintId?: string) {
    let params = new HttpParams();
    if (sprintId) params = params.set('sprintId', sprintId);
    return this.http
      .get<ProjectTask[]>(`${this.api}/projects/${projectId}/tasks`, { params })
      .pipe(map((tasks) => tasks.map((task) => this.normalizeTask(task))));
  }

  createTask(projectId: string, draft: TaskDraft) {
    return this.http
      .post<ProjectTask>(`${this.api}/projects/${projectId}/tasks`, draft)
      .pipe(map((task) => this.normalizeTask(task)));
  }

  updateTask(taskId: string, draft: TaskDraft) {
    return this.http.put<void>(`${this.api}/tasks/${taskId}`, draft);
  }

  deleteTask(taskId: string) {
    return this.http.delete<void>(`${this.api}/tasks/${taskId}`);
  }

  startTask(taskId: string) {
    return this.http.post<void>(`${this.api}/tasks/${taskId}/start`, {});
  }

  assignTask(taskId: string, projectMemberId: string) {
    return this.http.post<void>(`${this.api}/tasks/${taskId}/assignees`, { projectMemberId });
  }

  submitTask(taskId: string, description: string, links: TaskSubmissionLinkDraft[], fileVersionIds: string[]) {
    return this.http.post<TaskSubmissionResponse>(`${this.api}/tasks/${taskId}/submit`, {
      description,
      links,
      fileVersionIds,
    });
  }

  uploadTaskSubmissionFile(taskId: string, file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<ProjectFile>(`${this.api}/tasks/${taskId}/submission-files`, form);
  }

  getLatestTaskSubmission(taskId: string) {
    return this.http.get<TaskSubmissionDetail>(`${this.api}/tasks/${taskId}/submissions/latest`);
  }

  approveTaskSubmission(submissionId: string) {
    return this.http.post<void>(`${this.api}/submissions/${submissionId}/approve`, {});
  }

  requestTaskSubmissionRework(submissionId: string, feedback: string) {
    return this.http.post<void>(`${this.api}/submissions/${submissionId}/request-rework`, { feedback });
  }

  getTaskExtensionRequests(taskId: string) {
    return this.http.get<TaskExtensionRequest[]>(`${this.api}/tasks/${taskId}/extension-requests`);
  }

  requestDeadlineExtension(taskId: string, requestedDueAt: string, reason: string) {
    return this.http.post<{ id: string }>(`${this.api}/tasks/${taskId}/extension-requests`, {
      requestedDueAt,
      reason,
    });
  }

  approveTaskDeadlineExtension(extensionRequestId: string, note?: string) {
    return this.http.post<void>(`${this.api}/extension-requests/${extensionRequestId}/approve`, { note });
  }

  rejectTaskDeadlineExtension(extensionRequestId: string, note?: string) {
    return this.http.post<void>(`${this.api}/extension-requests/${extensionRequestId}/reject`, { note });
  }

  extendTaskDeadline(taskId: string, newDueAt: string, reason: string) {
    return this.http.post<void>(`${this.api}/tasks/${taskId}/extend-deadline`, { newDueAt, reason });
  }

  getTaskDeadlineHistory(taskId: string) {
    return this.http.get<TaskDeadlineChange[]>(`${this.api}/tasks/${taskId}/deadline-history`);
  }

  getMaintenanceStatus() {
    return this.http.get<MaintenanceStatus>(`${this.api.replace(/\/api$/, '')}/api/system/maintenance`);
  }

  getMembers(projectId: string) {
    return this.http
      .get<ProjectMember[]>(`${this.api}/projects/${projectId}/members`)
      .pipe(map((members) => members.map((member) => ({ ...member, status: enumName(member.status, MEMBERSHIP_STATUSES) }))));
  }

  findUsers(projectId: string, query: string) {
    return this.http.get<RegisteredUserMatch[]>(`${this.api}/projects/${projectId}/member-lookup`, {
      params: new HttpParams().set('query', query),
    });
  }

  getInvitations(projectId: string) {
    return this.http
      .get<ProjectInvitation[]>(`${this.api}/projects/${projectId}/invitations`)
      .pipe(map((invitations) => invitations.map((invitation) => ({ ...invitation, status: enumName(invitation.status, INVITATION_STATUSES) }))));
  }

  getRoles(projectId: string) {
    return this.http.get<ProjectRole[]>(`${this.api}/projects/${projectId}/roles`);
  }

  getProjectCapabilities(projectId: string) {
    return this.http.get<ProjectCapabilities>(`${this.api}/projects/${projectId}/capabilities`);
  }

  getProjectRolePermissions(projectId: string) {
    return this.http.get<ProjectRolePermissions[]>(`${this.api}/projects/${projectId}/role-permissions`);
  }

  updateProjectRolePermissions(projectId: string, roleId: string, permissionCodes: string[]) {
    return this.http.put<void>(`${this.api}/projects/${projectId}/roles/${roleId}/permissions`, {
      permissionCodes,
    });
  }

  inviteMember(projectId: string, email: string, roleId: string, expiresInDays: number) {
    return this.http
      .post<ProjectInvitation>(`${this.api}/projects/${projectId}/invitations`, {
        email,
        roleId,
        expiresInDays,
      })
      .pipe(map((invitation) => ({
        ...invitation,
        status: enumName(invitation.status, INVITATION_STATUSES),
      })));
  }

  changeMemberRole(projectId: string, membershipId: string, roleId: string) {
    return this.http.put<void>(
      `${this.api}/projects/${projectId}/members/${membershipId}/role`,
      { roleId },
    );
  }

  removeMember(projectId: string, membershipId: string, reason: string) {
    return this.http.delete<void>(`${this.api}/projects/${projectId}/members/${membershipId}`, {
      body: { reason },
    });
  }

  getStorage(projectId: string, folderId?: string | null) {
    let params = new HttpParams();
    if (folderId) params = params.set('folderId', folderId);
    return this.http.get<ProjectStorage>(`${this.api}/projects/${projectId}/storage`, { params });
  }

  createFolder(projectId: string, name: string, parentFolderId: string | null) {
    return this.http.post<ProjectFolder>(`${this.api}/projects/${projectId}/storage/folders`, {
      name,
      parentFolderId,
    });
  }

  renameFolder(folderId: string, name: string) {
    return this.http.put<void>(`${this.api}/storage/folders/${folderId}/name`, { name });
  }

  deleteFolder(folderId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/storage/folders/${folderId}`);
  }

  createDocument(projectId: string, folderId: string, title: string, content: string) {
    return this.http.post<ProjectDocument>(
      `${this.api}/projects/${projectId}/storage/documents`,
      { folderId, title, content, format: 'markdown' },
    );
  }

  saveDocument(documentId: string, content: string, changeNote: string) {
    return this.http.put<ProjectDocument>(`${this.api}/storage/documents/${documentId}`, {
      content,
      format: 'markdown',
      changeNote,
    });
  }

  getDocumentVersions(documentId: string) {
    return this.http.get<DocumentVersionHistory[]>(
      `${this.api}/storage/documents/${documentId}/versions`,
    );
  }

  setFolderPermissions(
    folderId: string,
    projectMemberId: string,
    permissions: { canView: boolean; canCreate: boolean; canUpload: boolean; canEdit: boolean; canDelete: boolean },
  ) {
    return this.http.put<void>(`${this.api}/storage/folders/${folderId}/permissions`, {
      roleId: null,
      projectMemberId,
      ...permissions,
    });
  }

  uploadFile(projectId: string, folderId: string, file: File) {
    const form = new FormData();
    form.append('file', file);
    form.append('folderId', folderId);
    form.append('changeNote', 'Uploaded from Planora Web');
    return this.http.post<ProjectFile>(`${this.api}/projects/${projectId}/storage/files`, form);
  }

  getFileContentUrl(fileId: string, versionId?: string): string {
    return versionId
      ? `${this.api}/storage/file-versions/${versionId}/content`
      : `${this.api}/storage/files/${fileId}/content`;
  }

  getFileVersionContentUrl(versionId: string): string {
    return `${this.api}/storage/file-versions/${versionId}/content`;
  }

  getFileDownloadUrl(fileId: string, versionId?: string): string {
    return versionId
      ? `${this.api}/storage/file-versions/${versionId}/download`
      : `${this.api}/storage/files/${fileId}/download`;
  }

  getFileBlob(fileId: string, versionId?: string): Observable<Blob> {
    const url = this.getFileContentUrl(fileId, versionId);
    return this.http.get(url, { responseType: 'blob' });
  }

  getFileVersionBlob(versionId: string): Observable<Blob> {
    const url = this.getFileVersionContentUrl(versionId);
    return this.http.get(url, { responseType: 'blob' });
  }

  downloadFileBlob(fileId: string, fileName: string, versionId?: string): void {
    const url = this.getFileDownloadUrl(fileId, versionId);
    this.http.get(url, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const objectUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = objectUrl;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        setTimeout(() => window.URL.revokeObjectURL(objectUrl), 1000);
      },
    });
  }

  deleteFile(fileId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/storage/files/${fileId}`);
  }

  deleteDocument(documentId: string): Observable<void> {
    return this.http.delete<void>(`${this.api}/storage/documents/${documentId}`);
  }

  getPlans() {
    return this.http
      .get<AvailablePlan[]>(`${this.api}/billing/plans`)
      .pipe(map((plans) => plans.map((plan) => ({ ...plan, billingPeriod: enumName(plan.billingPeriod, BILLING_PERIODS) }))));
  }

  getPayments() {
    return this.http
      .get<UserPayment[]>(`${this.api}/billing/payments`)
      .pipe(map((payments) => payments.map((payment) => ({
        ...payment,
        provider: enumName(payment.provider, PAYMENT_PROVIDERS),
        status: enumName(payment.status, PAYMENT_STATUSES),
      }))));
  }

  createPayment(planId: string, provider: 'Momo' | 'BankTransfer', idempotencyKey: string) {
    return this.http
      .post<PaymentCheckout>(`${this.api}/billing/payments`, { planId, provider, idempotencyKey })
      .pipe(map((checkout) => ({
        ...checkout,
        payment: {
          ...checkout.payment,
          provider: enumName(checkout.payment.provider, PAYMENT_PROVIDERS),
          status: enumName(checkout.payment.status, PAYMENT_STATUSES),
        },
      })));
  }

  cancelSubscription() {
    return this.http.post<void>(`${this.api}/billing/subscription/cancel`, {});
  }

  getSupportConversations() {
    return this.http.get<SupportConversation[]>(`${this.api}/support/conversations`);
  }

  createSupportConversation(
    kind: 'Feedback' | 'Refund',
    subject: string,
    message: string,
    paymentTransactionId: string | null,
  ) {
    return this.http.post<SupportConversation>(`${this.api}/support/conversations`, {
      kind,
      subject,
      message,
      paymentTransactionId,
    });
  }

  sendSupportMessage(conversationId: string, content: string) {
    return this.http.post<SupportMessage>(
      `${this.api}/support/conversations/${conversationId}/messages`,
      { content },
    );
  }

  private normalizeProject(project: Project): Project {
    return { ...project, status: enumName(project.status, PROJECT_STATUSES) };
  }

  private normalizeSprint(sprint: Sprint): Sprint {
    return { ...sprint, status: enumName(sprint.status, SPRINT_STATUSES) };
  }

  private normalizeTask(task: ProjectTask): ProjectTask {
    // Older running APIs can still return numeric enum values or PascalCase record
    // properties. Normalize once at the boundary so edit controls always receive the
    // same contract as the create form.
    const raw = task as ProjectTask & Record<string, unknown>;
    const allowedExtensions = task.allowedExtensions ?? raw['AllowedExtensions'];
    return {
      ...task,
      priority: enumName(task.priority ?? raw['Priority'], TASK_PRIORITIES) || 'Medium',
      status: enumName(task.status ?? raw['Status'], TASK_STATUSES) || 'Todo',
      type: (task.type ?? raw['Type'] as string | undefined) || 'General',
      submissionRequirement: enumName(task.submissionRequirement ?? raw['SubmissionRequirement'], SUBMISSION_REQUIREMENTS) || 'Any',
      allowedExtensions: Array.isArray(allowedExtensions) ? allowedExtensions.map(String) : [],
      dependsOnTaskId: (task.dependsOnTaskId ?? raw['DependsOnTaskId'] as string | null | undefined) ?? null,
      isMilestone: (task.isMilestone ?? raw['IsMilestone'] as boolean | undefined) ?? false,
    };
  }
}
