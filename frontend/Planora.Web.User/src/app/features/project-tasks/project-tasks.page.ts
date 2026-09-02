import { DatePipe } from '@angular/common';
import { Component, computed, inject, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import {
  LucideAlertCircle,
  LucideCalendarClock,
  LucideChevronRight,
  LucideCirclePlus,
  LucideDownload,
  LucideExternalLink,
  LucideEye,
  LucideFileText,
  LucideListFilter,
  LucideRotateCcw,
  LucideSearch,
  LucideTrash2,
  LucideUpload,
  LucideX,
} from '@lucide/angular';
import { concatMap, finalize, from, Subscription } from 'rxjs';
import { ProjectFile, ProjectMember, ProjectTask, TaskDeadlineChange, TaskDraft, TaskExtensionRequest, TaskSubmissionDetail } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { FilePreviewModalComponent, PreviewFileItem } from '../../shared/file-preview/file-preview-modal.component';
import { MarkdownEditorComponent } from '../../shared/markdown/markdown-editor.component';
import { MarkdownComponent } from '../../shared/markdown/markdown.component';

const TASK_STATUSES = ['Todo', 'InProgress', 'Submitted', 'Rework', 'Done', 'Expired'];
const DEFAULT_TASK_TYPES = ['General', 'Feature', 'BugFix', 'Documentation', 'Design', 'Testing', 'Research', 'Meeting'];
const FORMAT_GROUPS = [
  { label: 'PDF', extensions: ['pdf'] },
  { label: 'Word', extensions: ['doc', 'docx'] },
  { label: 'Excel', extensions: ['xls', 'xlsx'] },
  { label: 'PowerPoint', extensions: ['ppt', 'pptx'] },
  { label: 'Hình ảnh', extensions: ['png', 'jpg', 'jpeg', 'webp'] },
];

@Component({
  selector: 'app-project-tasks-page',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    LucideCalendarClock,
    LucideChevronRight,
    LucideCirclePlus,
    LucideListFilter,
    LucideSearch,
    LucideTrash2,
    LucideUpload,
    LucideX,
    LucideEye,
    LucideDownload,
    LucideExternalLink,
    LucideFileText,
    LucideAlertCircle,
    LucideRotateCcw,
    MarkdownComponent,
    MarkdownEditorComponent,
    FilePreviewModalComponent,
  ],
  templateUrl: './project-tasks.page.html',
  styleUrl: './project-tasks.page.css',
})
export class ProjectTasksPage implements OnDestroy {
  readonly store = inject(WorkspaceStore);
  readonly statuses = TASK_STATUSES;
  readonly search = signal('');
  readonly priority = signal('all');
  readonly selectedTask = signal<ProjectTask | null>(null);
  readonly reviewSubmission = signal<TaskSubmissionDetail | null>(null);
  readonly reviewing = signal(false);
  readonly editorOpen = signal(false);
  readonly editingTaskId = signal<string | null>(null);
  readonly saving = signal(false);
  readonly message = signal<string | null>(null);
  readonly toast = signal<string | null>(null);
  readonly addingTaskType = signal(false);
  readonly assigneeToAdd = signal('');
  readonly assigning = signal(false);
  readonly submissionTask = signal<ProjectTask | null>(null);
  readonly submissionFiles = signal<ProjectFile[]>([]);
  readonly uploadingSubmission = signal(false);
  readonly submitting = signal(false);
  readonly submissionError = signal<string | null>(null);
  readonly previewModalFile = signal<PreviewFileItem | null>(null);
  readonly thumbnailUrls = signal<Record<string, string>>({});
  readonly unavailableThumbnails = signal<Set<string>>(new Set());
  readonly formatGroups = FORMAT_GROUPS;

  readonly extensionRequests = signal<TaskExtensionRequest[]>([]);
  readonly deadlineHistory = signal<TaskDeadlineChange[]>([]);
  readonly showExtensionModal = signal<ProjectTask | null>(null);
  readonly showDirectExtendModal = signal<ProjectTask | null>(null);
  readonly showDeadlineHistory = signal(false);
  readonly extensionBusy = signal(false);
  readonly extensionError = signal<string | null>(null);

  requestedExtensionDueAt = '';
  requestedExtensionReason = '';
  directExtendDueAt = '';
  directExtendReason = '';
  extensionReviewNote = '';

  readonly pendingExtensionRequests = computed(() => {
    return this.extensionRequests().filter((item) => item.status === 'Pending');
  });

  readonly taskTypes = computed(() => {
    const projectTypes = this.store.tasks().map((task) => task.type?.trim()).filter((type): type is string => !!type);
    const currentType = this.draft?.type?.trim();
    return [...new Set([...DEFAULT_TASK_TYPES, ...projectTypes, ...(currentType ? [currentType] : [])])];
  });

  readonly filteredTasks = computed(() => {
    const query = this.search().trim().toLowerCase();
    return this.store.tasks().filter((task) => {
      const queryMatch = !query || `${task.title} ${task.description}`.toLowerCase().includes(query);
      const priorityMatch = this.priority() === 'all' || task.priority === this.priority();
      return queryMatch && priorityMatch;
    });
  });

  draft = this.emptyDraft();
  customTaskType = '';
  submissionNote = '';
  submissionLink = '';
  reviewFeedback = '';

  private readonly api = inject(PlanoraApiService);
  private readonly thumbnailRequests = new Map<string, Subscription>();

  constructor() {
    if (inject(ActivatedRoute).snapshot.queryParamMap.has('create')) queueMicrotask(() => this.openCreate());
  }

  tasksFor(status: string): ProjectTask[] {
    return this.filteredTasks().filter((task) => task.status.toLowerCase() === status.toLowerCase());
  }

  statusLabel(status: string): string {
    return ({ Todo: 'Cần làm', InProgress: 'Đang làm', Submitted: 'Chờ duyệt', Rework: 'Làm lại', Done: 'Hoàn tất', Expired: 'Quá hạn' } as Record<string, string>)[status] ?? status;
  }

  submissionStatusLabel(status: string): string {
    return ({ PendingReview: 'Chờ duyệt', Approved: 'Đã duyệt', ReworkRequested: 'Cần làm lại' } as Record<string, string>)[status] ?? status;
  }

  priorityLabel(priority: string): string {
    return ({ Urgent: 'Khẩn', High: 'Cao', Medium: 'Vừa', Low: 'Thấp' } as Record<string, string>)[priority] ?? priority;
  }

  shortId(id: string): string { return id.length > 12 ? id.slice(0, 8).toUpperCase() : id; }
  sprintName(id: string | null): string { return this.store.sprints().find((sprint) => sprint.id === id)?.name ?? 'Backlog'; }
  fileSize(bytes: number): string { return bytes < 1024 * 1024 ? `${Math.max(1, Math.round(bytes / 1024))} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`; }

  isImageFile(name: string, mime?: string): boolean {
    const ext = name.split('.').pop()?.toLowerCase() || '';
    return ['png', 'jpg', 'jpeg', 'webp', 'gif', 'svg'].includes(ext) || (mime?.startsWith('image/') ?? false);
  }

  thumbnailUrl(fileId: string, versionId?: string): string | null {
    const key = versionId ?? fileId;
    const cachedUrl = this.thumbnailUrls()[key];
    if (cachedUrl) return cachedUrl;
    if (this.unavailableThumbnails().has(key) || this.thumbnailRequests.has(key)) return null;

    const request = versionId ? this.api.getFileVersionBlob(versionId) : this.api.getFileBlob(fileId);
    const subscription = request.subscribe({
      next: (blob) => {
        const objectUrl = window.URL.createObjectURL(blob);
        this.thumbnailUrls.update((urls) => ({ ...urls, [key]: objectUrl }));
        this.thumbnailRequests.delete(key);
      },
      error: () => {
        this.unavailableThumbnails.update((ids) => new Set(ids).add(key));
        this.thumbnailRequests.delete(key);
      },
    });
    this.thumbnailRequests.set(key, subscription);
    return null;
  }

  thumbnailUnavailable(fileId: string, versionId?: string): boolean {
    return this.unavailableThumbnails().has(versionId ?? fileId);
  }

  ngOnDestroy(): void {
    this.thumbnailRequests.forEach((subscription) => subscription.unsubscribe());
    Object.values(this.thumbnailUrls()).forEach((url) => window.URL.revokeObjectURL(url));
  }

  openTask(task: ProjectTask): void {
    this.selectedTask.set(task);
    this.reviewSubmission.set(null);
    this.reviewFeedback = '';
    this.extensionRequests.set([]);
    this.deadlineHistory.set([]);
    this.showDeadlineHistory.set(false);

    // Load latest submission for any task that has submissions (Submitted, Rework, Done, etc.)
    if (['Submitted', 'Rework', 'Done'].includes(task.status)) {
      this.api.getLatestTaskSubmission(task.id).subscribe({
        next: (submission) => this.reviewSubmission.set(submission),
        error: () => {},
      });
    }

    // Load extension requests & deadline changes
    this.api.getTaskExtensionRequests(task.id).subscribe({
      next: (requests) => this.extensionRequests.set(requests),
      error: () => {},
    });
    this.api.getTaskDeadlineHistory(task.id).subscribe({
      next: (history) => this.deadlineHistory.set(history),
      error: () => {},
    });
  }

  previewSubmittedFile(file: { projectFileId: string; fileVersionId: string; name: string; mimeType: string; sizeBytes: number }): void {
    this.previewModalFile.set({
      id: file.projectFileId,
      versionId: file.fileVersionId,
      name: file.name,
      mimeType: file.mimeType,
      sizeBytes: file.sizeBytes,
    });
  }

  previewSubmissionItem(file: ProjectFile): void {
    this.previewModalFile.set({
      id: file.id,
      versionId: file.currentVersionId,
      name: file.name,
      mimeType: file.mimeType,
      sizeBytes: file.sizeBytes,
    });
  }

  downloadSubmittedFile(file: { projectFileId: string; fileVersionId: string; name: string }): void {
    this.api.downloadFileBlob(file.projectFileId, file.name, file.fileVersionId);
  }

  assigneeNames(task: ProjectTask): string[] {
    return task.assigneeMemberIds
      .map((membershipId) => this.store.members().find((member) => member.membershipId === membershipId)?.displayName)
      .filter((name): name is string => !!name);
  }

  assignableMembers(task: ProjectTask): ProjectMember[] {
    return this.store.members().filter((member) => member.status === 'Active' && !task.assigneeMemberIds.includes(member.membershipId));
  }

  assignTask(task: ProjectTask): void {
    const membershipId = this.assigneeToAdd();
    if (!membershipId || this.assigning()) return;
    this.assigning.set(true);
    this.api
      .assignTask(task.id, membershipId)
      .pipe(finalize(() => this.assigning.set(false)))
      .subscribe({
        next: () => {
          this.assigneeToAdd.set('');
          this.selectedTask.set(null);
          this.store.reloadTasks();
          this.notify('Đã giao việc. Thành viên sẽ nhận thông báo và email nếu đã bật.');
        },
        error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể giao việc cho thành viên này.'),
      });
  }

  canSubmitTask(task: ProjectTask): boolean {
    if (!this.store.hasPermission('task.submit') || !['Todo', 'InProgress', 'Rework'].includes(task.status)) return false;
    const membership = this.store.members().find((member) => member.userId === this.store.profile().userId && member.status === 'Active');
    return !!membership && task.assigneeMemberIds.includes(membership.membershipId);
  }

  openCreate(): void {
    if (!this.store.hasPermission('task.create')) return;
    this.editingTaskId.set(null);
    this.draft = this.emptyDraft();
    this.customTaskType = '';
    this.addingTaskType.set(false);
    this.message.set(null);
    this.editorOpen.set(true);
  }

  openEdit(task: ProjectTask): void {
    if (!this.store.hasPermission('task.edit')) return;
    this.editingTaskId.set(task.id);
    this.draft = {
      title: task.title,
      description: task.description,
      priority: task.priority,
      sprintId: task.sprintId ?? '',
      dueAt: task.effectiveDueAt?.slice(0, 10) ?? '',
      criteria: task.acceptanceCriteria.join('\n'),
      type: task.type ?? 'General',
      submissionRequirement: task.submissionRequirement ?? 'Any',
      extensions: task.allowedExtensions ?? [],
      isMilestone: task.isMilestone ?? false,
    };
    this.selectedTask.set(null);
    this.editorOpen.set(true);
  }

  closeEditor(): void {
    if (!this.saving()) {
      this.editorOpen.set(false);
      this.editingTaskId.set(null);
    }
  }

  saveTask(): void {
    if (!this.draft.title.trim() || this.saving()) return;
    const request: TaskDraft = {
      sprintId: this.draft.sprintId || null,
      title: this.draft.title.trim(),
      description: this.draft.description.trim(),
      priority: this.draft.priority,
      dueAt: this.draft.dueAt ? new Date(this.draft.dueAt).toISOString() : null,
      acceptanceCriteria: this.draft.criteria.split('\n').map((item) => item.trim()).filter(Boolean),
      type: this.draft.type,
      submissionRequirement: this.draft.submissionRequirement,
      allowedExtensions: this.draft.extensions,
      dependsOnTaskId: null,
      isMilestone: this.draft.isMilestone,
    };
    this.saving.set(true);
    this.message.set(null);
    const editingId = this.editingTaskId();
    if (editingId) {
      this.api
        .updateTask(editingId, request)
        .pipe(finalize(() => this.saving.set(false)))
        .subscribe({
          next: () => this.finishSave('Đã cập nhật công việc.'),
          error: (error: any) => this.message.set(error.error?.errors?.[0]?.message ?? 'Không thể lưu công việc.'),
        });
      return;
    }
    this.api
      .createTask(this.store.project().id, request)
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => this.finishSave('Đã tạo công việc.'),
        error: (error: any) => this.message.set(error.error?.errors?.[0]?.message ?? 'Không thể lưu công việc.'),
      });
  }

  startTask(task: ProjectTask): void {
    if (!this.canSubmitTask(task)) return;
    this.api.startTask(task.id).subscribe({
      next: () => {
        this.store.reloadTasks();
        this.selectedTask.set(null);
        this.notify('Công việc đã bắt đầu.');
      },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể bắt đầu công việc.'),
    });
  }

  openSubmission(task: ProjectTask): void {
    if (!this.canSubmitTask(task)) return;
    this.submissionTask.set(task);
    this.submissionFiles.set([]);
    this.submissionLink = '';
    this.submissionNote = '';
    this.submissionError.set(null);
  }

  closeSubmission(): void {
    if (this.submitting() || this.uploadingSubmission()) return;
    this.submissionTask.set(null);
    this.submissionError.set(null);
  }

  uploadSubmissionFiles(task: ProjectTask, event: Event): void {
    const input = event.target as HTMLInputElement;
    const files = Array.from(input.files ?? []);
    if (!files.length || this.uploadingSubmission()) return;
    this.uploadingSubmission.set(true);
    this.submissionError.set(null);
    from(files)
      .pipe(
        concatMap((file) => this.api.uploadTaskSubmissionFile(task.id, file)),
        finalize(() => {
          this.uploadingSubmission.set(false);
          input.value = '';
        }),
      )
      .subscribe({
        next: (uploaded) => this.submissionFiles.update((items) => [...items, uploaded]),
        error: (error) => this.submissionError.set(error.error?.errors?.[0]?.message ?? 'Không thể tải tệp bài nộp.'),
      });
  }

  removeSubmissionFile(fileVersionId: string): void {
    this.submissionFiles.update((items) => items.filter((file) => file.currentVersionId !== fileVersionId));
  }

  canSendSubmission(task: ProjectTask): boolean {
    const hasFile = this.submissionFiles().length > 0;
    const hasLink = this.isValidUrl(this.submissionLink);
    return task.submissionRequirement === 'LinkOnly' ? hasLink : task.submissionRequirement === 'Any' ? hasFile || hasLink : hasFile;
  }

  submitTask(task: ProjectTask): void {
    if (!this.canSubmitTask(task) || !this.canSendSubmission(task) || this.submitting()) return;
    const links = this.isValidUrl(this.submissionLink)
      ? [{ url: this.submissionLink.trim(), linkType: 'Result', title: 'Kết quả công việc' }]
      : [];
    this.submitting.set(true);
    this.submissionError.set(null);
    this.api
      .submitTask(task.id, this.submissionNote.trim(), links, this.submissionFiles().map((file) => file.currentVersionId))
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.store.reloadTasks();
          this.store.reloadStorage();
          this.selectedTask.set(null);
          this.submissionTask.set(null);
          this.notify('Đã gửi kết quả để Leader duyệt.');
        },
        error: (error) => this.submissionError.set(error.error?.errors?.[0]?.message ?? 'Không thể nộp công việc.'),
      });
  }

  approveSubmission(task: ProjectTask): void {
    const submission = this.reviewSubmission();
    if (!submission || this.reviewing()) return;
    this.reviewing.set(true);
    this.api
      .approveTaskSubmission(submission.id)
      .pipe(finalize(() => this.reviewing.set(false)))
      .subscribe({
        next: () => {
          this.selectedTask.set(null);
          this.reviewSubmission.set(null);
          this.store.reloadTasks();
          this.notify('Đã duyệt hoàn thành công việc.');
        },
        error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể duyệt bài nộp.'),
      });
  }

  requestRework(task: ProjectTask): void {
    const submission = this.reviewSubmission();
    if (!submission || !this.reviewFeedback.trim() || this.reviewing()) {
      if (!this.reviewFeedback.trim()) this.notify('Hãy nhập phản hồi trước khi yêu cầu làm lại.');
      return;
    }
    this.reviewing.set(true);
    this.api
      .requestTaskSubmissionRework(submission.id, this.reviewFeedback.trim())
      .pipe(finalize(() => this.reviewing.set(false)))
      .subscribe({
        next: () => {
          this.selectedTask.set(null);
          this.reviewSubmission.set(null);
          this.store.reloadTasks();
          this.notify('Đã gửi yêu cầu làm lại.');
        },
        error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể yêu cầu làm lại.'),
      });
  }

  deleteTask(task: ProjectTask): void {
    if (!confirm(`Xóa công việc “${task.title}”?`)) return;
    this.api.deleteTask(task.id).subscribe({
      next: () => {
        this.store.tasks.update((items) => items.filter((item) => item.id !== task.id));
        this.selectedTask.set(null);
        this.notify('Đã xóa công việc.');
      },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể xóa công việc.'),
    });
  }

  taskTypeLabel(type: string): string {
    return ({ General: 'Chung', Feature: 'Tính năng', BugFix: 'Sửa lỗi', Documentation: 'Tài liệu', Design: 'Thiết kế', Testing: 'Kiểm thử', Research: 'Nghiên cứu', Meeting: 'Cuộc họp' } as Record<string, string>)[type] ?? type;
  }

  useCustomTaskType(): void {
    const value = this.customTaskType.trim().replace(/\s+/g, ' ');
    if (!value) return;
    this.draft.type = value;
    this.customTaskType = '';
    this.addingTaskType.set(false);
  }

  setSubmissionRequirement(requirement: string): void {
    this.draft.submissionRequirement = requirement;
    this.draft.extensions = ({ Word: ['doc', 'docx'], Excel: ['xls', 'xlsx'], Pdf: ['pdf'], PowerPoint: ['ppt', 'pptx'], Image: ['png', 'jpg', 'jpeg', 'webp'] } as Record<string, string[]>)[requirement] ?? (requirement === 'FileOnly' ? this.draft.extensions : []);
  }

  submissionHint(): string {
    return ({ Any: 'Người thực hiện có thể nộp link hoặc tải tệp.', LinkOnly: 'Chỉ yêu cầu một đường dẫn hợp lệ.', FileOnly: 'Chọn một hoặc nhiều nhóm định dạng bên dưới.', Word: 'Chấp nhận .doc và .docx.', Excel: 'Chấp nhận .xls và .xlsx.', Pdf: 'Chỉ chấp nhận .pdf.', PowerPoint: 'Chấp nhận .ppt và .pptx.', Image: 'Chấp nhận PNG, JPG, JPEG và WebP.' } as Record<string, string>)[this.draft.submissionRequirement] ?? '';
  }

  submissionRequirementLabel(requirement: string): string {
    return ({ Any: 'Link hoặc tệp', LinkOnly: 'Đường dẫn', FileOnly: 'Tệp đúng định dạng được phép', Word: 'Tài liệu Word', Excel: 'Bảng tính Excel', Pdf: 'Tệp PDF', PowerPoint: 'Bản trình chiếu PowerPoint', Image: 'Hình ảnh' } as Record<string, string>)[requirement] ?? requirement;
  }

  extensionLabel(extensions: string[]): string {
    return extensions.map((extension) => `.${extension}`).join(', ');
  }

  submissionAccept(task: ProjectTask): string {
    return (task.allowedExtensions?.length ? task.allowedExtensions : this.extensionsForRequirement(task.submissionRequirement ?? 'Any')).map((extension) => `.${extension}`).join(',');
  }

  formatSelected(extensions: string[]): boolean {
    return extensions.every((extension) => this.draft.extensions.includes(extension));
  }

  toggleFormat(extensions: string[]): void {
    const remove = this.formatSelected(extensions);
    this.draft.extensions = remove ? this.draft.extensions.filter((extension) => !extensions.includes(extension)) : [...new Set([...this.draft.extensions, ...extensions])];
  }

  openExtensionModal(task: ProjectTask): void {
    this.showExtensionModal.set(task);
    this.requestedExtensionDueAt = task.effectiveDueAt ? new Date(task.effectiveDueAt).toISOString().slice(0, 16) : '';
    this.requestedExtensionReason = '';
    this.extensionError.set(null);
  }

  sendExtensionRequest(task: ProjectTask): void {
    if (!this.requestedExtensionDueAt || !this.requestedExtensionReason.trim() || this.extensionBusy()) return;
    this.extensionBusy.set(true);
    this.extensionError.set(null);
    this.api
      .requestDeadlineExtension(task.id, new Date(this.requestedExtensionDueAt).toISOString(), this.requestedExtensionReason.trim())
      .pipe(finalize(() => this.extensionBusy.set(false)))
      .subscribe({
        next: () => {
          this.showExtensionModal.set(null);
          this.openTask(task);
          this.notify('Đã gửi yêu cầu gia hạn hạn chót.');
        },
        error: (error) => this.extensionError.set(error.error?.errors?.[0]?.message ?? 'Không thể gửi yêu cầu gia hạn.'),
      });
  }

  openDirectExtendModal(task: ProjectTask): void {
    this.showDirectExtendModal.set(task);
    this.directExtendDueAt = task.effectiveDueAt ? new Date(task.effectiveDueAt).toISOString().slice(0, 16) : '';
    this.directExtendReason = '';
    this.extensionError.set(null);
  }

  sendDirectExtend(task: ProjectTask): void {
    if (!this.directExtendDueAt || !this.directExtendReason.trim() || this.extensionBusy()) return;
    this.extensionBusy.set(true);
    this.extensionError.set(null);
    this.api
      .extendTaskDeadline(task.id, new Date(this.directExtendDueAt).toISOString(), this.directExtendReason.trim())
      .pipe(finalize(() => this.extensionBusy.set(false)))
      .subscribe({
        next: () => {
          this.showDirectExtendModal.set(null);
          this.store.reloadTasks();
          this.openTask(task);
          this.notify('Đã điều chỉnh hạn chót công việc.');
        },
        error: (error) => this.extensionError.set(error.error?.errors?.[0]?.message ?? 'Không thể điều chỉnh hạn chót.'),
      });
  }

  approveExtension(request: TaskExtensionRequest): void {
    if (this.extensionBusy()) return;
    this.extensionBusy.set(true);
    this.api
      .approveTaskDeadlineExtension(request.id, this.extensionReviewNote)
      .pipe(finalize(() => this.extensionBusy.set(false)))
      .subscribe({
        next: () => {
          const task = this.selectedTask();
          if (task) {
            this.store.reloadTasks();
            this.openTask(task);
          }
          this.notify('Đã duyệt gia hạn hạn chót.');
        },
        error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể duyệt gia hạn.'),
      });
  }

  rejectExtension(request: TaskExtensionRequest): void {
    if (this.extensionBusy()) return;
    this.extensionBusy.set(true);
    this.api
      .rejectTaskDeadlineExtension(request.id, this.extensionReviewNote)
      .pipe(finalize(() => this.extensionBusy.set(false)))
      .subscribe({
        next: () => {
          const task = this.selectedTask();
          if (task) this.openTask(task);
          this.notify('Đã từ chối gia hạn hạn chót.');
        },
        error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể từ chối gia hạn.'),
      });
  }

  private emptyDraft() {
    return { title: '', description: '', priority: 'Medium', sprintId: this.store.activeSprint().id || '', dueAt: '', criteria: '', type: 'General', submissionRequirement: 'Any', extensions: [] as string[], isMilestone: false };
  }

  private isValidUrl(value: string): boolean {
    try {
      const url = new URL(value.trim());
      return url.protocol === 'http:' || url.protocol === 'https:';
    } catch {
      return false;
    }
  }

  private extensionsForRequirement(requirement: string): string[] {
    return ({ Word: ['doc', 'docx'], Excel: ['xls', 'xlsx', 'csv'], Pdf: ['pdf'], PowerPoint: ['ppt', 'pptx'], Image: ['png', 'jpg', 'jpeg', 'gif', 'webp'] } as Record<string, string[]>)[requirement] ?? [];
  }

  private finishSave(message: string): void {
    this.store.reloadTasks();
    this.editorOpen.set(false);
    this.editingTaskId.set(null);
    this.notify(message);
  }

  private notify(value: string): void {
    this.toast.set(value);
    setTimeout(() => this.toast.set(null), 2400);
  }
}
