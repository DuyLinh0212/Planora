import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { finalize } from 'rxjs';
import { AdminAuthSessionService } from '../../core/admin-auth-session.service';
import { PlanoraAdminApiService } from '../../core/planora-admin-api.service';

@Component({
  selector: 'app-admin-settings-page',
  imports: [FormsModule],
  templateUrl: './admin-settings-page.component.html',
  styleUrl: './admin-settings-page.component.css',
})
export class AdminSettingsPageComponent implements OnInit {
  private readonly api = inject(PlanoraAdminApiService);
  private readonly authSession = inject(AdminAuthSessionService);
  readonly theme = signal<'light' | 'dark' | 'calm'>((localStorage.getItem('planora.admin.theme') as 'light' | 'dark' | 'calm') ?? 'light');
  readonly changingPassword = signal(false);
  readonly savingMaintenance = signal(false);
  readonly passwordError = signal<string | null>(null);
  currentPassword = '';
  newPassword = '';
  confirmNewPassword = '';
  maintenanceEnabled = false;
  maintenanceMessage = 'Planora đang bảo trì để nâng cấp hệ thống. Vui lòng quay lại sau.';

  ngOnInit(): void { if (localStorage.getItem('planora.admin.preview') === 'true') return; this.api.getMaintenanceStatus().subscribe((status) => { this.maintenanceEnabled = status.isEnabled; this.maintenanceMessage = status.message; }); }
  setTheme(theme: 'light' | 'dark' | 'calm'): void { localStorage.setItem('planora.admin.theme', theme); this.theme.set(theme); location.reload(); }
  changeAdministratorPassword(): void {
    this.passwordError.set(null);
    if (this.newPassword.length < 10 || !/[^A-Za-z0-9]/.test(this.newPassword)) { this.passwordError.set('Mật khẩu mới cần ít nhất 10 ký tự và một ký tự đặc biệt.'); return; }
    if (this.newPassword !== this.confirmNewPassword) { this.passwordError.set('Mật khẩu xác nhận không khớp.'); return; }
    this.changingPassword.set(true);
    this.api.changePassword(this.currentPassword, this.newPassword).pipe(finalize(() => this.changingPassword.set(false))).subscribe({ next: () => this.authSession.clearSessionAndReturnToLogin(), error: () => this.passwordError.set('Mật khẩu hiện tại sai hoặc phiên quản trị đã hết hạn.') });
  }
  saveMaintenance(): void {
    this.passwordError.set(null);
    if (this.maintenanceEnabled && !this.maintenanceMessage.trim()) { this.passwordError.set('Hãy nhập thông báo trước khi bật bảo trì.'); return; }
    if (this.maintenanceEnabled && !confirm('Bật chế độ bảo trì cho toàn bộ người dùng?')) return;
    if (localStorage.getItem('planora.admin.preview') === 'true') return;
    this.savingMaintenance.set(true);
    this.api.updateMaintenanceStatus(this.maintenanceEnabled, this.maintenanceMessage).pipe(finalize(() => this.savingMaintenance.set(false))).subscribe();
  }
}
