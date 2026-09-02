import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideChevronDown,
  LucideChevronRight,
  LucideDownload,
  LucideEye,
  LucideFile,
  LucideFileCode,
  LucideFileSpreadsheet,
  LucideFileText,
  LucideFolder,
  LucideFolderPlus,
  LucideImage,
  LucideLayers,
  LucideMoreHorizontal,
  LucideSearch,
  LucideUpload,
  LucideX,
} from '@lucide/angular';
import { finalize } from 'rxjs';
import { DocumentVersionHistory, ProjectDocument, ProjectFile, ProjectFolder, ProjectTask, Sprint } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { QuotaNoticeService } from '../../core/feedback/quota-notice.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { FilePreviewModalComponent, PreviewFileItem } from '../../shared/file-preview/file-preview-modal.component';
import { FilePropertiesDrawerComponent } from '../../shared/file-preview/file-properties-drawer.component';
import { MarkdownEditorComponent } from '../../shared/markdown/markdown-editor.component';

export type FileNavFilter =
  | { type: 'all' }
  | { type: 'folder'; folderId: string }
  | { type: 'sprint'; sprintId: string }
  | { type: 'task'; taskId: string };

@Component({
  selector: 'app-project-files-page',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    LucideFile,
    LucideFileText,
    LucideFileSpreadsheet,
    LucideFileCode,
    LucideImage,
    LucideFolder,
    LucideFolderPlus,
    LucideLayers,
    LucideMoreHorizontal,
    LucideChevronRight,
    LucideChevronDown,
    LucideSearch,
    LucideUpload,
    LucideDownload,
    LucideEye,
    LucideX,
    FilePropertiesDrawerComponent,
    FilePreviewModalComponent,
    MarkdownEditorComponent,
  ],
  templateUrl: './project-files.page.html',
  styleUrl: './project-files.page.css',
})
export class ProjectFilesPage {
  readonly store = inject(WorkspaceStore);
  private readonly api = inject(PlanoraApiService);
  private readonly quotaNotice = inject(QuotaNoticeService);

  readonly activeNav = signal<FileNavFilter>({ type: 'all' });
  readonly search = signal('');
  readonly sprintTreeExpanded = signal(true);
  readonly customFoldersExpanded = signal(true);
  readonly expandedSprintIds = signal<Set<string>>(new Set());

  readonly folderDialog = signal(false);
  readonly documentDialog = signal(false);
  readonly editingDocument = signal<ProjectDocument | null>(null);
  readonly documentVersions = signal<DocumentVersionHistory[]>([]);
  readonly selectedFileProperties = signal<ProjectFile | null>(null);
  readonly previewModalFile = signal<PreviewFileItem | null>(null);

  readonly busy = signal(false);
  readonly toast = signal<string | null>(null);

  folderName = '';
  documentDraft = { title: '', content: '# Tài liệu mới\n', changeNote: '' };

  readonly rootFolder = computed(() => {
    return this.store.storage().folders.find((folder) => !folder.parentFolderId) || null;
  });

  readonly customFolders = computed(() => {
    return this.store.storage().folders.filter((folder) => !!folder.parentFolderId);
  });

  readonly folderTree = computed(() => {
    const rootId = this.rootFolder()?.id;
    const byParent = new Map<string | null, ProjectFolder[]>();
    for (const folder of this.store.storage().folders) {
      if (folder.id === rootId) continue;
      const parentId = folder.parentFolderId ?? null;
      byParent.set(parentId, [...(byParent.get(parentId) ?? []), folder].sort((a, b) => a.name.localeCompare(b.name)));
    }
    const walk = (parentId: string | null, depth: number): Array<ProjectFolder & { depth: number }> =>
      (byParent.get(parentId) ?? []).flatMap((folder) => [{ ...folder, depth }, ...walk(folder.id, depth + 1)]);
    return walk(rootId ?? null, 0);
  });

  readonly visibleFolders = computed(() => {
    const nav = this.activeNav();
    if (nav.type === 'sprint' || nav.type === 'task') return [];
    const parentId = nav.type === 'folder' ? nav.folderId : nav.type === 'all' ? this.rootFolder()?.id : null;
    return this.store.storage().folders
      .filter((folder) => folder.parentFolderId === parentId)
      .sort((a, b) => a.name.localeCompare(b.name));
  });

  readonly breadcrumbs = computed(() => {
    const root = this.rootFolder();
    const items: Array<{ label: string; filter: FileNavFilter }> = [{ label: root?.name ?? 'Tệp project', filter: { type: 'all' } }];
    const active = this.activeNav();
    if (active.type !== 'folder') return items;
    const ancestors: ProjectFolder[] = [];
    let current = this.store.storage().folders.find((folder) => folder.id === active.folderId) ?? null;
    while (current && current.id !== root?.id) {
      ancestors.unshift(current);
      current = this.store.storage().folders.find((folder) => folder.id === current?.parentFolderId) ?? null;
    }
    return [...items, ...ancestors.map((folder) => ({ label: folder.name, filter: { type: 'folder' as const, folderId: folder.id } }))];
  });

  readonly filteredDocuments = computed(() => {
    const nav = this.activeNav();
    const query = this.search().trim().toLowerCase();
    const docs = this.store.storage().documents;

    let result = docs;
    if (nav.type === 'folder') {
      result = result.filter((d) => d.folderId === nav.folderId);
    } else if (nav.type === 'task') {
      result = result.filter((d) => d.sourceTaskId === nav.taskId);
    } else if (nav.type === 'sprint') {
      const sprintTaskIds = new Set(this.store.tasks().filter((t) => t.sprintId === nav.sprintId).map((t) => t.id));
      result = result.filter((d) => d.sourceTaskId && sprintTaskIds.has(d.sourceTaskId));
    }

    if (query) {
      result = result.filter((d) => d.title.toLowerCase().includes(query));
    }
    return result;
  });

  readonly filteredFiles = computed(() => {
    const nav = this.activeNav();
    const query = this.search().trim().toLowerCase();
    const allFiles = this.store.storage().files;

    let result = allFiles;
    if (nav.type === 'folder') {
      result = result.filter((f) => f.folderId === nav.folderId);
    } else if (nav.type === 'task') {
      result = result.filter((f) => f.sourceTaskId === nav.taskId);
    } else if (nav.type === 'sprint') {
      const sprintTaskIds = new Set(this.store.tasks().filter((t) => t.sprintId === nav.sprintId).map((t) => t.id));
      result = result.filter((f) => f.sourceTaskId && sprintTaskIds.has(f.sourceTaskId));
    }

    if (query) {
      result = result.filter((f) => f.name.toLowerCase().includes(query));
    }
    return result;
  });

  readonly currentFilterLabel = computed(() => {
    const nav = this.activeNav();
    if (nav.type === 'all') return 'Tất cả tệp & tài liệu';
    if (nav.type === 'folder') {
      const f = this.store.storage().folders.find((x) => x.id === nav.folderId);
      return `Thư mục: ${f?.name || 'Chưa đặt tên'}`;
    }
    if (nav.type === 'sprint') {
      const s = this.store.sprints().find((x) => x.id === nav.sprintId);
      return `Sprint: ${s?.name || 'Backlog'}`;
    }
    if (nav.type === 'task') {
      const t = this.store.tasks().find((x) => x.id === nav.taskId);
      return `Công việc: ${t?.title || 'Công việc'}`;
    }
    return 'Kho lưu trữ';
  });

  getFolderId(nav: FileNavFilter): string | null { return nav.type === 'folder' ? nav.folderId : null; }
  getSprintId(nav: FileNavFilter): string | null { return nav.type === 'sprint' ? nav.sprintId : null; }
  getTaskId(nav: FileNavFilter): string | null { return nav.type === 'task' ? nav.taskId : null; }

  totalItems(): number {
    return this.store.storage().files.length + this.store.storage().documents.length;
  }

  countInFolder(id: string): number {
    return (
      this.store.storage().files.filter((item) => item.folderId === id).length +
      this.store.storage().documents.filter((item) => item.folderId === id).length
    );
  }

  countInSprint(sprintId: string): number {
    const sprintTaskIds = new Set(this.store.tasks().filter((t) => t.sprintId === sprintId).map((t) => t.id));
    const fileCount = this.store.storage().files.filter((f) => f.sourceTaskId && sprintTaskIds.has(f.sourceTaskId)).length;
    const docCount = this.store.storage().documents.filter((d) => d.sourceTaskId && sprintTaskIds.has(d.sourceTaskId)).length;
    return fileCount + docCount;
  }

  countInTask(taskId: string): number {
    const fileCount = this.store.storage().files.filter((f) => f.sourceTaskId === taskId).length;
    const docCount = this.store.storage().documents.filter((d) => d.sourceTaskId === taskId).length;
    return fileCount + docCount;
  }

  tasksInSprint(sprintId: string): ProjectTask[] {
    return this.store.tasks().filter((t) => t.sprintId === sprintId);
  }

  isSprintExpanded(sprintId: string): boolean {
    return this.expandedSprintIds().has(sprintId);
  }

  toggleSprintCollapse(sprintId: string, event: Event): void {
    event.stopPropagation();
    this.expandedSprintIds.update((set) => {
      const next = new Set(set);
      if (next.has(sprintId)) next.delete(sprintId);
      else next.add(sprintId);
      return next;
    });
  }

  selectNav(filter: FileNavFilter): void {
    this.activeNav.set(filter);
  }

  getAssociatedTask(sourceTaskId?: string | null): ProjectTask | null {
    if (!sourceTaskId) return null;
    return this.store.tasks().find((t) => t.id === sourceTaskId) || null;
  }

  getFileExtBadge(fileName: string): string {
    return fileName.split('.').pop()?.toUpperCase() || 'FILE';
  }

  getFileType(name: string, mime?: string): string {
    const ext = name.split('.').pop()?.toLowerCase() || '';
    if (['png', 'jpg', 'jpeg', 'webp', 'gif', 'svg'].includes(ext) || mime?.startsWith('image/')) return 'image';
    if (ext === 'pdf') return 'pdf';
    if (['doc', 'docx'].includes(ext)) return 'docx';
    if (['xls', 'xlsx', 'csv'].includes(ext)) return 'spreadsheet';
    if (['js', 'ts', 'html', 'css', 'json', 'xml', 'md', 'txt'].includes(ext)) return 'code';
    return 'generic';
  }

  selectFileProperties(file: ProjectFile): void {
    this.selectedFileProperties.set(file);
  }

  previewFile(file: ProjectFile): void {
    this.previewModalFile.set({
      id: file.id,
      name: file.name,
      mimeType: file.mimeType,
      sizeBytes: file.sizeBytes,
      versionId: file.currentVersionId,
      versionNumber: file.versionNumber,
      sourceTaskId: file.sourceTaskId,
    });
  }

  previewFromDrawer(item: PreviewFileItem): void {
    this.previewModalFile.set(item);
  }

  downloadFile(file: ProjectFile): void {
    this.api.downloadFileBlob(file.id, file.name, file.currentVersionId);
  }

  deleteFile(file: ProjectFile): void {
    if (!confirm(`Xóa tệp "${file.name}"?`)) return;
    this.api.deleteFile(file.id).subscribe({
      next: () => {
        this.store.storage.update((st) => ({
          ...st,
          files: st.files.filter((f) => f.id !== file.id),
        }));
        this.selectedFileProperties.set(null);
        this.notify('Đã xóa tệp.');
      },
      error: (err: any) => this.notify(err.error?.errors?.[0]?.message ?? 'Không thể xóa tệp.'),
    });
  }

  renameFolder(folder: ProjectFolder): void {
    const name = window.prompt('Tên thư mục mới', folder.name)?.trim();
    if (!name || name === folder.name) return;
    this.api.renameFolder(folder.id, name).subscribe({
      next: () => { this.store.reloadStorage(); this.notify('Đã đổi tên thư mục.'); },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể đổi tên thư mục.'),
    });
  }

  deleteFolder(folder: ProjectFolder): void {
    if (!window.confirm(`Xóa thư mục “${folder.name}”? Thư mục phải trống trước khi xóa.`)) return;
    this.api.deleteFolder(folder.id).subscribe({
      next: () => { this.selectNav({ type: 'all' }); this.store.reloadStorage(); this.notify('Đã xóa thư mục.'); },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể xóa thư mục.'),
    });
  }

  createFolder(): void {
    if (!this.store.hasPermission('folder.create') || !this.folderName.trim() || this.busy()) return;
    this.busy.set(true);
    const nav = this.activeNav();
    const parentFolderId = nav.type === 'folder' ? nav.folderId : this.rootFolder()?.id || null;

    this.api
      .createFolder(this.store.project().id, this.folderName.trim(), parentFolderId)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (folder) => {
          this.store.storage.update((storage) => ({ ...storage, folders: [...storage.folders, folder] }));
          this.folderName = '';
          this.folderDialog.set(false);
          this.notify('Đã tạo thư mục.');
        },
        error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể tạo thư mục.'),
      });
  }

  openUpload(input: HTMLInputElement): void {
    if (!this.store.hasPermission('file.upload')) return;
    const quota = this.store.profile().quota;
    if (!this.quotaNotice.checkUpload(quota, this.currentProjectStorageBytes(), 0)) return;
    input.click();
  }

  upload(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const nav = this.activeNav();
    const folderId = nav.type === 'folder' ? nav.folderId : this.rootFolder()?.id;
    if (!folderId) return this.notify('Không xác định được thư mục để lưu.');

    if (!this.quotaNotice.checkUpload(this.store.profile().quota, this.currentProjectStorageBytes(), file.size)) {
      input.value = '';
      return;
    }

    this.busy.set(true);
    this.api
      .uploadFile(this.store.project().id, folderId, file)
      .pipe(finalize(() => this.busy.set(false)))
      .subscribe({
        next: (uploaded) => {
          this.store.storage.update((storage) => ({ ...storage, files: [...storage.files, uploaded] }));
          this.notify('Đã tải tệp lên.');
        },
        error: (error) => {
          if (!this.quotaNotice.isQuotaError(error)) this.notify(error.error?.errors?.[0]?.message ?? 'Không thể tải tệp.');
        },
      });
  }

  openCreateDocument(): void {
    this.editingDocument.set(null);
    this.documentVersions.set([]);
    this.documentDraft = { title: '', content: '# Tài liệu mới\n', changeNote: '' };
    this.documentDialog.set(true);
  }

  editDocument(document: ProjectDocument): void {
    this.editingDocument.set(document);
    this.documentVersions.set([]);
    this.documentDraft = { title: document.title, content: '', changeNote: '' };
    this.api.getDocumentVersions(document.id).subscribe({
      next: (versions) => {
        this.documentVersions.set(versions);
        this.documentDraft.content = versions[0]?.content ?? '';
      },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể tải lịch sử tài liệu.'),
    });
  }

  closeDocument(): void {
    if (!this.busy()) {
      this.documentDialog.set(false);
      this.editingDocument.set(null);
      this.documentVersions.set([]);
      this.documentDraft = { title: '', content: '# Tài liệu mới\n', changeNote: '' };
    }
  }

  saveDocument(): void {
    if (!this.store.hasPermission('document.edit')) return;
    const nav = this.activeNav();
    const folderId = nav.type === 'folder' ? nav.folderId : this.rootFolder()?.id;
    if (!folderId && !this.editingDocument()) return this.notify('Hãy chọn thư mục chứa tài liệu.');

    this.busy.set(true);
    const editing = this.editingDocument();
    const operation = editing
      ? this.api.saveDocument(editing.id, this.documentDraft.content, this.documentDraft.changeNote)
      : this.api.createDocument(this.store.project().id, folderId!, this.documentDraft.title.trim(), this.documentDraft.content);

    operation.pipe(finalize(() => this.busy.set(false))).subscribe({
      next: () => {
        this.closeDocument();
        this.store.reloadStorage();
        this.notify('Đã lưu tài liệu.');
      },
      error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể lưu tài liệu.'),
    });
  }

  formatBytes(bytes: number): string {
    if (!bytes) return '0 B';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private notify(value: string): void {
    this.toast.set(value);
    setTimeout(() => this.toast.set(null), 2400);
  }

  private currentProjectStorageBytes(): number {
    return this.store.storage().files.reduce((total, file) => total + file.sizeBytes, 0);
  }
}
