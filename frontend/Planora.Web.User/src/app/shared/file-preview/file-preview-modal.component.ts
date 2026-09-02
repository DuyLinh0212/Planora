import { Component, computed, inject, input, output, signal } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import {
  LucideCopy,
  LucideDownload,
  LucideExternalLink,
  LucideFile,
  LucideMaximize2,
  LucideRotateCw,
  LucideX,
  LucideZoomIn,
  LucideZoomOut,
} from '@lucide/angular';
import DOMPurify from 'dompurify';
import * as mammoth from 'mammoth';
import * as XLSX from 'xlsx';
import { PlanoraApiService } from '../../core/api/planora-api.service';

export interface PreviewFileItem {
  id: string;
  name: string;
  mimeType: string;
  sizeBytes: number;
  versionId?: string;
  versionNumber?: number;
  sourceTaskId?: string | null;
  uploadedAt?: string;
  uploadedBy?: string;
}

@Component({
  selector: 'app-file-preview-modal',
  standalone: true,
  imports: [
    LucideX,
    LucideDownload,
    LucideExternalLink,
    LucideZoomIn,
    LucideZoomOut,
    LucideRotateCw,
    LucideMaximize2,
    LucideCopy,
    LucideFile,
  ],
  templateUrl: './file-preview-modal.component.html',
  styleUrl: './file-preview-modal.component.css',
})
export class FilePreviewModalComponent {
  readonly file = input<PreviewFileItem | null>(null);
  readonly close = output<void>();

  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly blobUrl = signal<string | null>(null);
  readonly safePdfUrl = signal<SafeResourceUrl | null>(null);
  readonly docxHtml = signal<string>('');
  readonly textContent = signal<string>('');
  readonly sheetNames = signal<string[]>([]);
  readonly activeSheet = signal<string>('');
  readonly workbookData = signal<Record<string, any[][]>>({});
  readonly copied = signal(false);

  readonly zoom = signal(1);
  readonly rotation = signal(0);

  private readonly api = inject(PlanoraApiService);
  private readonly sanitizer = inject(DomSanitizer);

  readonly category = computed(() => {
    const item = this.file();
    if (!item) return 'unknown';
    const ext = item.name.split('.').pop()?.toLowerCase() || '';
    const mime = item.mimeType?.toLowerCase() || '';

    if (['png', 'jpg', 'jpeg', 'webp', 'gif', 'svg', 'bmp', 'ico'].includes(ext) || mime.startsWith('image/')) {
      return 'image';
    }
    if (ext === 'pdf' || mime === 'application/pdf') {
      return 'pdf';
    }
    if (['docx', 'doc'].includes(ext) || mime.includes('wordprocessingml') || mime.includes('msword')) {
      return 'docx';
    }
    if (['xlsx', 'xls', 'csv'].includes(ext) || mime.includes('spreadsheetml') || mime.includes('ms-excel') || mime === 'text/csv') {
      return 'spreadsheet';
    }
    if (['txt', 'json', 'md', 'html', 'css', 'js', 'ts', 'xml', 'log', 'yaml', 'yml'].includes(ext) || mime.startsWith('text/')) {
      return 'text';
    }
    return 'unknown';
  });

  readonly activeSheetData = computed(() => {
    const sheet = this.activeSheet();
    return this.workbookData()[sheet] || [];
  });

  readonly columnHeaders = computed(() => {
    const data = this.activeSheetData();
    if (!data.length) return [];
    const maxCols = Math.max(...data.map((row) => row.length));
    return Array.from({ length: maxCols }, (_, i) => this.getExcelColumnName(i));
  });

  readonly imageTransform = computed(() => `scale(${this.zoom()}) rotate(${this.rotation()}deg)`);

  ngOnInit(): void {
    this.loadFileContent();
  }

  ngOnDestroy(): void {
    const url = this.blobUrl();
    if (url) window.URL.revokeObjectURL(url);
  }

  loadFileContent(): void {
    const item = this.file();
    if (!item) return;

    this.loading.set(true);
    this.error.set(null);

    const request = item.versionId
      ? this.api.getFileVersionBlob(item.versionId)
      : this.api.getFileBlob(item.id);

    request.subscribe({
      next: (blob) => {
        this.loading.set(false);
        const objUrl = window.URL.createObjectURL(blob);
        this.blobUrl.set(objUrl);

        const cat = this.category();
        if (cat === 'pdf') {
          this.safePdfUrl.set(this.sanitizer.bypassSecurityTrustResourceUrl(objUrl));
        } else if (cat === 'docx') {
          blob.arrayBuffer().then((buffer) => {
            mammoth
              .convertToHtml({ arrayBuffer: buffer })
              .then((result) => {
                this.docxHtml.set(DOMPurify.sanitize(result.value));
              })
              .catch((err) => {
                this.error.set('Không thể phân tích tệp DOCX: ' + (err.message || 'Lỗi'));
              });
          });
        } else if (cat === 'spreadsheet') {
          blob.arrayBuffer().then((buffer) => {
            try {
              const wb = XLSX.read(buffer, { type: 'array' });
              const sheets: Record<string, any[][]> = {};
              wb.SheetNames.forEach((name) => {
                const ws = wb.Sheets[name];
                sheets[name] = XLSX.utils.sheet_to_json(ws, { header: 1 }) as any[][];
              });
              this.sheetNames.set(wb.SheetNames);
              this.activeSheet.set(wb.SheetNames[0] || '');
              this.workbookData.set(sheets);
            } catch (err: any) {
              this.error.set('Không thể đọc bảng tính: ' + (err.message || 'Lỗi định dạng'));
            }
          });
        } else if (cat === 'text') {
          blob.text().then((text) => {
            this.textContent.set(text);
          });
        }
      },
      error: (err) => {
        this.loading.set(false);
        this.error.set(err.error?.errors?.[0]?.message ?? 'Không thể tải nội dung tệp từ máy chủ.');
      },
    });
  }

  selectSheet(name: string): void {
    this.activeSheet.set(name);
  }

  zoomIn(): void {
    this.zoom.update((z) => Math.min(z + 0.25, 4));
  }

  zoomOut(): void {
    this.zoom.update((z) => Math.max(z - 0.25, 0.25));
  }

  rotate(): void {
    this.rotation.update((r) => (r + 90) % 360);
  }

  resetTransform(): void {
    this.zoom.set(1);
    this.rotation.set(0);
  }

  copyText(): void {
    navigator.clipboard.writeText(this.textContent()).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  download(): void {
    const item = this.file();
    if (!item) return;
    this.api.downloadFileBlob(item.id, item.name, item.versionId);
  }

  onClose(): void {
    this.close.emit();
  }

  formatBytes(bytes: number): string {
    if (!bytes) return '0 B';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  private getExcelColumnName(index: number): string {
    let name = '';
    let num = index;
    while (num >= 0) {
      name = String.fromCharCode((num % 26) + 65) + name;
      num = Math.floor(num / 26) - 1;
    }
    return name;
  }
}
