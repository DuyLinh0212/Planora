import { Component, ElementRef, inject, input, model, output, signal, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  LucideBold,
  LucideCode,
  LucideColumns2,
  LucideEye,
  LucideHeading1,
  LucideHeading2,
  LucideHeading3,
  LucideImage,
  LucideItalic,
  LucideLink,
  LucideList,
  LucideListOrdered,
  LucideListTodo,
  LucidePenLine,
  LucideQuote,
  LucideStrikethrough,
  LucideTable,
  LucideUpload,
} from '@lucide/angular';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { MarkdownComponent } from './markdown.component';

@Component({
  selector: 'app-markdown-editor',
  standalone: true,
  imports: [
    FormsModule,
    MarkdownComponent,
    LucideBold,
    LucideItalic,
    LucideStrikethrough,
    LucideHeading1,
    LucideHeading2,
    LucideHeading3,
    LucideList,
    LucideListOrdered,
    LucideListTodo,
    LucideQuote,
    LucideCode,
    LucideTable,
    LucideLink,
    LucideImage,
    LucideUpload,
    LucideEye,
    LucidePenLine,
    LucideColumns2,
  ],
  templateUrl: './markdown-editor.component.html',
  styleUrl: './markdown-editor.component.css',
})
export class MarkdownEditorComponent {
  readonly value = model<string>('');
  readonly placeholder = input('Nhập nội dung Markdown hoặc dán ảnh từ clipboard…');
  readonly rows = input<number>(6);
  readonly disabled = input<boolean>(false);
  readonly imageUploaded = output<string>();

  @ViewChild('textareaEl') textareaRef?: ElementRef<HTMLTextAreaElement>;

  readonly viewMode = signal<'edit' | 'preview' | 'split'>('edit');
  readonly uploading = signal(false);

  private readonly api = inject(PlanoraApiService);
  private readonly store = inject(WorkspaceStore);

  wordCount(): number {
    const text = this.value()?.trim();
    if (!text) return 0;
    return text.split(/\s+/).filter(Boolean).length;
  }

  onValueChange(val: string): void {
    this.value.set(val);
  }

  onKeyDown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'b') {
      event.preventDefault();
      this.wrapSelection('**', '**', 'chữ đậm');
    } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'i') {
      event.preventDefault();
      this.wrapSelection('*', '*', 'chữ nghiêng');
    } else if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.insertLink();
    }
  }

  onPaste(event: ClipboardEvent): void {
    const items = event.clipboardData?.items;
    if (!items) return;

    for (let i = 0; i < items.length; i++) {
      const item = items[i];
      if (item.type.indexOf('image') !== -1) {
        const file = item.getAsFile();
        if (file) {
          event.preventDefault();
          this.uploadAndInsertImage(file, 'clipboard-image');
          break;
        }
      }
    }
  }

  onUploadImage(event: Event): void {
    const inputEl = event.target as HTMLInputElement;
    const file = inputEl.files?.[0];
    if (!file) return;
    this.uploadAndInsertImage(file, file.name);
    inputEl.value = '';
  }

  private uploadAndInsertImage(file: File, defaultAlt: string): void {
    const projectId = this.store.project().id;
    const rootFolder = this.store.storage().folders.find((f) => !f.parentFolderId);

    if (projectId && rootFolder) {
      this.uploading.set(true);
      this.api.uploadFile(projectId, rootFolder.id, file).subscribe({
        next: (uploadedFile) => {
          this.uploading.set(false);
          const imgUrl = this.api.getFileContentUrl(uploadedFile.id);
          const mdImg = `\n![${defaultAlt}](${imgUrl})\n`;
          this.insertAtCursor(mdImg);
          this.imageUploaded.emit(imgUrl);
          this.store.reloadStorage();
        },
        error: () => {
          this.uploading.set(false);
          const reader = new FileReader();
          reader.onload = () => {
            const dataUrl = reader.result as string;
            this.insertAtCursor(`\n![${defaultAlt}](${dataUrl})\n`);
          };
          reader.readAsDataURL(file);
        },
      });
    } else {
      const reader = new FileReader();
      reader.onload = () => {
        const dataUrl = reader.result as string;
        this.insertAtCursor(`\n![${defaultAlt}](${dataUrl})\n`);
      };
      reader.readAsDataURL(file);
    }
  }

  wrapSelection(prefix: string, suffix: string, defaultText: string): void {
    const textarea = this.textareaRef?.nativeElement;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const current = this.value() || '';
    const selected = current.slice(start, end);

    const replacement = selected ? `${prefix}${selected}${suffix}` : `${prefix}${defaultText}${suffix}`;
    const nextValue = current.slice(0, start) + replacement + current.slice(end);

    this.value.set(nextValue);
    setTimeout(() => {
      textarea.focus();
      const cursorStart = start + prefix.length;
      const cursorEnd = selected ? cursorStart + selected.length : cursorStart + defaultText.length;
      textarea.setSelectionRange(cursorStart, cursorEnd);
    });
  }

  insertPrefix(prefix: string, defaultText: string): void {
    const textarea = this.textareaRef?.nativeElement;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const current = this.value() || '';
    const selected = current.slice(start, end);

    const textToInsert = selected ? `${prefix}${selected}` : `${prefix}${defaultText}`;
    const nextValue = current.slice(0, start) + textToInsert + current.slice(end);

    this.value.set(nextValue);
    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + prefix.length, start + textToInsert.length);
    });
  }

  insertCodeBlock(): void {
    const textarea = this.textareaRef?.nativeElement;
    if (!textarea) return;

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const current = this.value() || '';
    const selected = current.slice(start, end);

    if (selected.includes('\n')) {
      const replacement = `\n\`\`\`\n${selected}\n\`\`\`\n`;
      this.value.set(current.slice(0, start) + replacement + current.slice(end));
    } else if (selected) {
      const replacement = `\`${selected}\``;
      this.value.set(current.slice(0, start) + replacement + current.slice(end));
    } else {
      const replacement = `\n\`\`\`typescript\n// Nhập mã nguồn ở đây\n\`\`\`\n`;
      this.value.set(current.slice(0, start) + replacement + current.slice(end));
    }
  }

  insertTable(): void {
    const template = `\n| Tiêu đề 1 | Tiêu đề 2 | Tiêu đề 3 |\n| :--- | :--- | :--- |\n| Dữ liệu 1 | Dữ liệu 2 | Dữ liệu 3 |\n| Dữ liệu 4 | Dữ liệu 5 | Dữ liệu 6 |\n`;
    this.insertAtCursor(template);
  }

  insertLink(): void {
    const url = prompt('Nhập địa chỉ URL liên kết (https://…):');
    if (!url) return;
    this.wrapSelection('[', `](${url.trim()})`, 'Tiêu đề liên kết');
  }

  insertImageLink(): void {
    const url = prompt('Nhập địa chỉ URL hình ảnh (https://…):');
    if (!url) return;
    const alt = prompt('Nhập mô tả hình ảnh (tùy chọn):') || 'Hình ảnh';
    this.insertAtCursor(`\n![${alt}](${url.trim()})\n`);
  }

  insertAtCursor(text: string): void {
    const textarea = this.textareaRef?.nativeElement;
    if (!textarea) {
      this.value.set((this.value() || '') + text);
      return;
    }

    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const current = this.value() || '';
    const nextValue = current.slice(0, start) + text + current.slice(end);

    this.value.set(nextValue);
    setTimeout(() => {
      textarea.focus();
      textarea.setSelectionRange(start + text.length, start + text.length);
    });
  }
}
