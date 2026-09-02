import { Component, OnDestroy, effect, inject, input, signal } from '@angular/core';
import DOMPurify from 'dompurify';
import { marked } from 'marked';
import { Subscription } from 'rxjs';
import { PlanoraApiService } from '../../core/api/planora-api.service';

@Component({
  selector: 'app-markdown',
  templateUrl: './markdown.component.html',
  styleUrl: './markdown.component.css',
})
export class MarkdownComponent implements OnDestroy {
  readonly value = input('');
  readonly html = signal('');

  private readonly api = inject(PlanoraApiService);
  private readonly imageRequests: Subscription[] = [];
  private readonly objectUrls = new Set<string>();
  private renderVersion = 0;

  constructor() {
    effect(() => this.render(this.value()));
  }

  ngOnDestroy(): void {
    this.disposeImages();
  }

  private render(value: string): void {
    const version = ++this.renderVersion;
    this.disposeImages();

    const source = value.trim() || '*Chưa có mô tả.*';
    const rendered = marked.parse(source, { async: false, breaks: true }) as string;
    const safeHtml = DOMPurify.sanitize(rendered);
    this.html.set(safeHtml);

    // Native <img> requests cannot carry Planora's Bearer token. Fetch images from
    // protected storage through HttpClient and swap only those sources for object URLs.
    const documentFragment = new DOMParser().parseFromString(safeHtml, 'text/html');
    for (const image of Array.from(documentFragment.images)) {
      const storageReference = this.parseStorageReference(image.getAttribute('src'));
      if (!storageReference) continue;

      const request = storageReference.kind === 'file'
        ? this.api.getFileBlob(storageReference.id)
        : this.api.getFileVersionBlob(storageReference.id);
      this.imageRequests.push(request.subscribe({
        next: (blob) => {
          if (version !== this.renderVersion) return;
          const objectUrl = window.URL.createObjectURL(blob);
          this.objectUrls.add(objectUrl);
          image.setAttribute('src', objectUrl);
          this.html.set(documentFragment.body.innerHTML);
        },
      }));
    }
  }

  private parseStorageReference(source: string | null): { kind: 'file' | 'version'; id: string } | null {
    if (!source) return null;
    try {
      const path = new URL(source, window.location.origin).pathname;
      const match = path.match(/\/api\/storage\/(files|file-versions)\/([0-9a-f-]{36})\/content$/i);
      if (!match) return null;
      return { kind: match[1].toLowerCase() === 'files' ? 'file' : 'version', id: match[2] };
    } catch {
      return null;
    }
  }

  private disposeImages(): void {
    while (this.imageRequests.length) this.imageRequests.pop()!.unsubscribe();
    for (const objectUrl of this.objectUrls) window.URL.revokeObjectURL(objectUrl);
    this.objectUrls.clear();
  }
}
