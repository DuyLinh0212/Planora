import { DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, input, OnDestroy, output, signal } from '@angular/core';
import {
  LucideCheck,
  LucideCopy,
  LucideDownload,
  LucideEye,
  LucideFile,
  LucideFileCode,
  LucideFileSpreadsheet,
  LucideFileText,
  LucideFolder,
  LucideInfo,
  LucideTrash2,
  LucideX,
} from '@lucide/angular';
import { ProjectFile } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { PreviewFileItem } from './file-preview-modal.component';

@Component({
  selector: 'app-file-properties-drawer',
  standalone: true,
  imports: [
    DecimalPipe,
    LucideX,
    LucideEye,
    LucideDownload,
    LucideCopy,
    LucideTrash2,
    LucideFile,
    LucideFileText,
    LucideFileSpreadsheet,
    LucideFileCode,
    LucideFolder,
    LucideInfo,
    LucideCheck,
  ],
  templateUrl: './file-properties-drawer.component.html',
  styleUrl: './file-properties-drawer.component.css',
})
export class FilePropertiesDrawerComponent implements OnDestroy {
  readonly file = input<ProjectFile | null>(null);
  readonly close = output<void>();
  readonly preview = output<PreviewFileItem>();
  readonly delete = output<ProjectFile>();

  readonly store = inject(WorkspaceStore);
  private readonly api = inject(PlanoraApiService);

  readonly copied = signal(false);
  readonly imageLoadError = signal(false);
  readonly thumbnailUrl = signal<string | null>(null);
  private thumbnailObjectUrl: string | null = null;
  private thumbnailSubscription?: import('rxjs').Subscription;

  readonly fileExtension = computed(() => {
    const name = this.file()?.name || '';
    return name.split('.').pop()?.toUpperCase() || 'FILE';
  });

  readonly isImage = computed(() => {
    const ext = (this.file()?.name || '').split('.').pop()?.toLowerCase() || '';
    const mime = this.file()?.mimeType?.toLowerCase() || '';
    return ['png', 'jpg', 'jpeg', 'webp', 'gif', 'svg', 'bmp'].includes(ext) || mime.startsWith('image/');
  });

  readonly fileTypeCategory = computed(() => {
    const ext = (this.file()?.name || '').split('.').pop()?.toLowerCase() || '';
    if (['pdf'].includes(ext)) return 'pdf';
    if (['doc', 'docx'].includes(ext)) return 'docx';
    if (['xls', 'xlsx', 'csv'].includes(ext)) return 'spreadsheet';
    if (['js', 'ts', 'html', 'css', 'json', 'xml', 'md', 'txt'].includes(ext)) return 'code';
    return 'generic';
  });

  constructor() {
    effect((onCleanup) => {
      const f = this.file();
      const image = this.isImage();
      this.releaseThumbnail();
      this.thumbnailUrl.set(null);
      this.imageLoadError.set(false);
      if (!f || !image) return;

      this.thumbnailSubscription = this.api.getFileBlob(f.id, f.currentVersionId).subscribe({
        next: (blob) => {
          this.thumbnailObjectUrl = window.URL.createObjectURL(blob);
          this.thumbnailUrl.set(this.thumbnailObjectUrl);
        },
        error: () => this.imageLoadError.set(true),
      });
      onCleanup(() => this.releaseThumbnail());
    });
  }

  ngOnDestroy(): void {
    this.releaseThumbnail();
  }

  readonly folderName = computed(() => {
    const f = this.file();
    if (!f) return 'Thư mục gốc';
    const folder = this.store.storage().folders.find((item) => item.id === f.folderId);
    return folder ? folder.name : 'Thư mục gốc';
  });

  readonly associatedTask = computed(() => {
    const f = this.file();
    if (!f || !f.sourceTaskId) return null;
    return this.store.tasks().find((t) => t.id === f.sourceTaskId) || null;
  });

  sprintName(sprintId: string | null): string {
    if (!sprintId) return 'Backlog';
    return this.store.sprints().find((s) => s.id === sprintId)?.name || 'Backlog';
  }

  formatBytes(bytes: number): string {
    if (!bytes) return '0 B';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  onPreview(): void {
    const f = this.file();
    if (!f) return;
    this.preview.emit({
      id: f.id,
      name: f.name,
      mimeType: f.mimeType,
      sizeBytes: f.sizeBytes,
      versionId: f.currentVersionId,
      versionNumber: f.versionNumber,
      sourceTaskId: f.sourceTaskId,
    });
  }

  onDownload(): void {
    const f = this.file();
    if (!f) return;
    this.api.downloadFileBlob(f.id, f.name, f.currentVersionId);
  }

  copyLink(): void {
    const f = this.file();
    if (!f) return;
    const url = this.api.getFileContentUrl(f.id, f.currentVersionId);
    navigator.clipboard.writeText(url).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  onDelete(): void {
    const f = this.file();
    if (f) this.delete.emit(f);
  }

  onClose(): void {
    this.close.emit();
  }

  private releaseThumbnail(): void {
    this.thumbnailSubscription?.unsubscribe();
    this.thumbnailSubscription = undefined;
    if (this.thumbnailObjectUrl) window.URL.revokeObjectURL(this.thumbnailObjectUrl);
    this.thumbnailObjectUrl = null;
  }
}
