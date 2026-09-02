import { DatePipe } from '@angular/common';
import { Component, OnInit, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { PlanoraAdminApiService, SupportConversation } from '../../core/planora-admin-api.service';
import { AdminRealtimeService } from '../../core/admin-realtime.service';

@Component({
  selector: 'app-admin-support-page',
  imports: [DatePipe, FormsModule],
  templateUrl: './admin-support-page.component.html',
  styleUrl: './admin-support-page.component.css',
})
export class AdminSupportPageComponent implements OnInit {
  private readonly api = inject(PlanoraAdminApiService);
  private readonly realtime = inject(AdminRealtimeService);
  readonly conversations = signal<SupportConversation[]>([]);
  readonly selected = signal<SupportConversation | null>(null);
  readonly message = signal<string | null>(null);
  filter = '';
  replyText = '';
  constructor() {
    effect(() => { const selected = this.selected(); if (selected && !selected.id.startsWith('demo-')) void this.realtime.joinSupport(selected.id); });
    effect(() => { if (this.realtime.supportMessageVersion()) this.load(); });
  }
  ngOnInit(): void { this.load(); }
  load(): void { if (localStorage.getItem('planora.admin.preview') === 'true') return this.loadDemo(); this.api.getSupportConversations(this.filter || undefined).subscribe({ next: (items) => { this.conversations.set(items); const current = this.selected(); this.selected.set(items.find((item) => item.id === current?.id) ?? items[0] ?? null); }, error: () => this.loadDemo() }); }
  loadDemo(): void { const now = new Date().toISOString(); const demo: SupportConversation[] = [{ id: 'demo-refund', kind: 'Refund', subject: 'Yêu cầu hoàn tiền gói Pro', status: 'Open', paymentTransactionId: 'PAY-2026-0830', createdAt: now, closedAt: null, messages: [{ id: 'm1', senderUserId: 'u1', senderDisplayName: 'Nguyễn An', content: 'Mình mua nhầm gói theo năm và chưa sử dụng tính năng trả phí.', createdAt: now }] }, { id: 'demo-feedback', kind: 'Feedback', subject: 'Gợi ý cải thiện lịch sprint', status: 'WaitingForUser', paymentTransactionId: null, createdAt: now, closedAt: null, messages: [{ id: 'm2', senderUserId: 'u2', senderDisplayName: 'Lê Minh', content: 'Mình muốn kéo thả task trực tiếp trên lịch.', createdAt: now }] }]; this.conversations.set(demo); this.selected.set(demo[0]); }
  reply(): void { const conversation = this.selected(); const content = this.replyText.trim(); if (!conversation || !content) return; if (conversation.id.startsWith('demo-')) { conversation.messages.push({ id: crypto.randomUUID(), senderUserId: 'admin', senderDisplayName: 'Planora Support', content, createdAt: new Date().toISOString() }); this.selected.set({ ...conversation }); this.replyText = ''; return this.notify('Đã mô phỏng gửi phản hồi.'); } this.api.sendSupportMessage(conversation.id, content).subscribe((reply) => { conversation.messages.push(reply); conversation.status = 'WaitingForUser'; this.selected.set({ ...conversation }); this.replyText = ''; this.notify('Đã gửi phản hồi.'); }); }
  close(): void { const conversation = this.selected(); if (!conversation || !confirm('Đóng yêu cầu này? Người dùng vẫn xem được lịch sử trao đổi.')) return; if (conversation.id.startsWith('demo-')) { conversation.status = 'Closed'; this.selected.set({ ...conversation }); return; } this.api.closeSupportConversation(conversation.id).subscribe(() => { conversation.status = 'Closed'; this.selected.set({ ...conversation }); this.notify('Đã đóng yêu cầu.'); }); }
  statusLabel(status: string): string { return ({ Open: 'Mới', WaitingForUser: 'Chờ người dùng', Closed: 'Đã đóng' } as Record<string, string>)[status] ?? status; }
  initials(name: string): string { return name.split(' ').map((part) => part[0]).slice(0, 2).join('').toUpperCase(); }
  private notify(value: string): void { this.message.set(value); setTimeout(() => this.message.set(null), 2400); }
}
