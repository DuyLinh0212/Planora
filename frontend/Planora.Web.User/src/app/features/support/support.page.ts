import { DatePipe } from '@angular/common';
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideLifeBuoy, LucideMessageSquarePlus, LucideSend, LucideX } from '@lucide/angular';
import { SupportConversation } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { RealtimeNotificationService } from '../../core/realtime/realtime-notification.service';

@Component({
  selector: 'app-support-page',
  imports: [DatePipe, FormsModule, LucideLifeBuoy, LucideMessageSquarePlus, LucideSend, LucideX],
  templateUrl: './support.page.html',
  styleUrl: './support.page.css',
})
export class SupportPage implements OnInit {
  readonly store = inject(WorkspaceStore);
  readonly conversations = signal<SupportConversation[]>([]);
  readonly selected = signal<SupportConversation | null>(null);
  readonly composerOpen = signal(false);
  readonly toast = signal<string | null>(null);
  message = '';
  draft = { kind: 'Feedback' as 'Feedback' | 'Refund', subject: '', content: '' };
  private readonly api = inject(PlanoraApiService);
  private readonly realtime = inject(RealtimeNotificationService);
  constructor() {
    effect(() => { const selected = this.selected(); if (selected) void this.realtime.joinSupport(selected.id); });
    effect(() => { if (this.realtime.supportMessageVersion()) this.loadConversations(false); });
  }
  ngOnInit(): void { this.loadConversations(true); }
  createConversation(): void { if (!this.draft.subject.trim() || !this.draft.content.trim()) return; this.api.createSupportConversation(this.draft.kind, this.draft.subject.trim(), this.draft.content.trim(), null).subscribe({ next: (conversation) => { this.conversations.update((items) => [conversation, ...items]); this.selected.set(conversation); this.composerOpen.set(false); this.draft = { kind: 'Feedback', subject: '', content: '' }; }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể tạo yêu cầu.') }); }
  sendMessage(): void { const conversation = this.selected(); const content = this.message.trim(); if (!conversation || !content) return; this.api.sendSupportMessage(conversation.id, content).subscribe({ next: (message) => { const updated = { ...conversation, messages: [...conversation.messages, message] }; this.selected.set(updated); this.conversations.update((items) => items.map((item) => item.id === updated.id ? updated : item)); this.message = ''; }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể gửi tin nhắn.') }); }
  initials(value: string): string { return value.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase(); }
  private loadConversations(selectFirst: boolean): void { this.api.getSupportConversations().subscribe({ next: (items) => { const currentId = this.selected()?.id; this.conversations.set(items); this.selected.set(items.find((item) => item.id === currentId) ?? (selectFirst ? items[0] ?? null : this.selected())); }, error: () => this.notify('Không thể tải các yêu cầu hỗ trợ.') }); }
  private notify(value: string): void { this.toast.set(value); setTimeout(() => this.toast.set(null), 2600); }
}
