import { ChangeDetectorRef, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  LucideBell,
  LucideCamera,
  LucideCheck,
  LucideChevronDown,
  LucideChevronRight,
  LucideCrown,
  LucideFileText,
  LucideGem,
  LucideGlobe,
  LucideHelpCircle,
  LucideKeyRound,
  LucideLogOut,
  LucideMail,
  LucideMoon,
  LucideMonitor,
  LucidePencil,
  LucideSave,
  LucideShield,
  LucideSparkles,
  LucideSquare,
  LucideSun,
  LucideX,
} from '@lucide/angular';
import { finalize, firstValueFrom, forkJoin } from 'rxjs';
import { AvailablePlan, GmailLinkResponse } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';
import { GoogleIdentityService } from '../auth/google-identity.service';
import { I18nService } from '../../core/i18n/i18n.service';

export interface ExtraProfileInfo {
  phone: string;
  birthDate: string;
  country: string;
  gender: string;
  joinedDate: string;
  avatarFrame: 'default' | 'gold' | 'gradient';
}

@Component({
  selector: 'app-account-page',
  imports: [
    FormsModule,
    RouterLink,
    LucideBell,
    LucideCamera,
    LucideCheck,
    LucideChevronDown,
    LucideChevronRight,
    LucideCrown,
    LucideFileText,
    LucideGem,
    LucideGlobe,
    LucideHelpCircle,
    LucideKeyRound,
    LucideLogOut,
    LucideMail,
    LucideMoon,
    LucideMonitor,
    LucidePencil,
    LucideSave,
    LucideShield,
    LucideSparkles,
    LucideSquare,
    LucideSun,
    LucideX,
  ],
  templateUrl: './account.page.html',
  styleUrl: './account.page.css',
})
export class AccountPage {
  readonly store = inject(WorkspaceStore);
  private readonly api = inject(PlanoraApiService);
  private readonly auth = inject(AuthSessionService);
  private readonly googleIdentity = inject(GoogleIdentityService);
  private readonly changeDetector = inject(ChangeDetectorRef);
  private readonly i18n = inject(I18nService);

  // Profile data & edit fields
  displayName = '';
  language: 'vi' | 'en' = 'vi';
  theme: 'light' | 'dark' | 'calm' = 'light';
  timeZone = 'Asia/Ho_Chi_Minh';

  // Extra profile attributes
  readonly extraInfo = signal<ExtraProfileInfo>({
    phone: '+84 912 345 678',
    birthDate: '12/04/1995',
    country: 'Việt Nam',
    gender: 'Nữ',
    joinedDate: '15/03/2023',
    avatarFrame: 'default',
  });

  // Inline editing state
  readonly isEditing = signal(false);
  editPhone = '';
  editBirthDate = '';
  editCountry = '';
  editGender = '';

  // Email notifications
  private emailTaskNotificationsValue = false;
  get emailTaskNotifications(): boolean {
    return this.emailTaskNotificationsValue;
  }
  set emailTaskNotifications(enabled: boolean) {
    // Email delivery uses the shared Planora SMTP mailbox by default. Linking
    // Gmail is optional and only changes the sender used by the dispatcher.
    this.emailTaskNotificationsValue = enabled;
  }

  // Password change state
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  readonly passwordModalOpen = signal(false);
  readonly passwordBusy = signal(false);
  readonly passwordError = signal<string | null>(null);

  // Async & UI states
  readonly availablePlans = signal<AvailablePlan[]>([]);
  readonly avatarUrl = signal<string | null>(null);
  readonly saving = signal(false);
  readonly avatarBusy = signal(false);
  readonly gmailBusy = signal(false);
  readonly gmailLink = signal<GmailLinkResponse>({
    isLinked: false,
    gmailAddress: null,
    isServerConfigured: false,
    lastSendFailedAt: null,
    lastSendFailureReason: null,
  });
  readonly error = signal<string | null>(null);
  readonly toast = signal<string | null>(null);
  readonly logoutConfirmation = signal(false);
  private initialized = false;

  constructor() {
    this.loadPlans();
    effect(() => {
      const profile = this.store.profile();
      if (!profile.userId || this.initialized) return;
      this.initialized = true;
      this.displayName = profile.displayName;
      this.avatarUrl.set(profile.avatarUrl ?? null);
      this.language = profile.preferredLanguage;
      this.i18n.setLanguage(profile.preferredLanguage);
      this.theme = profile.themePreference || 'light';
      this.timeZone = profile.timeZoneId || 'Asia/Ho_Chi_Minh';
      this.gmailLink.set(profile.gmailLink);
      this.emailTaskNotifications = profile.emailTaskNotificationsEnabled ?? false;

      this.loadExtraInfo(profile.userId);
    });
  }

  initials(): string {
    return (this.displayName || this.store.profile().displayName || 'NA')
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }

  setTheme(theme: 'light' | 'dark' | 'calm'): void {
    this.theme = theme;
    document.documentElement.dataset['theme'] = theme;
    this.api
      .updatePreferences(this.language, this.theme, this.timeZone, this.emailTaskNotifications)
      .subscribe({
        next: () => {
          this.store.profile.update((profile) => ({ ...profile, themePreference: theme }));
          const themeLabel =
            theme === 'light'
              ? 'giao diện Sáng'
              : theme === 'dark'
                ? 'giao diện Tối'
                : 'giao diện Yên bình';
          this.notify(`Đã chuyển sang ${themeLabel}.`);
        },
      });
  }

  onLanguageChanged(lang: 'vi' | 'en'): void {
    this.language = lang;
    this.i18n.setLanguage(lang);
    this.api
      .updatePreferences(this.language, this.theme, this.timeZone, this.emailTaskNotifications)
      .subscribe({
        next: () => {
          this.store.profile.update((profile) => ({ ...profile, preferredLanguage: lang }));
          this.notify(lang === 'vi' ? 'Đã đổi ngôn ngữ sang Tiếng Việt.' : 'Language set to English.');
        },
      });
  }

  selectAvatarFrame(frame: 'default' | 'gold' | 'gradient'): void {
    this.extraInfo.update((info) => {
      const updated = { ...info, avatarFrame: frame };
      this.saveExtraInfo(updated);
      return updated;
    });
    this.notify('Đã cập nhật khung avatar.');
  }

  startEditing(): void {
    this.editPhone = this.extraInfo().phone;
    this.editBirthDate = this.extraInfo().birthDate;
    this.editCountry = this.extraInfo().country;
    this.editGender = this.extraInfo().gender;
    this.isEditing.set(true);
  }

  cancelEditing(): void {
    this.displayName = this.store.profile().displayName;
    this.isEditing.set(false);
  }

  saveProfile(): void {
    if (!this.displayName.trim() || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);

    // Save extra info
    this.extraInfo.update((info) => {
      const updated = {
        ...info,
        phone: this.editPhone.trim() || info.phone,
        birthDate: this.editBirthDate.trim() || info.birthDate,
        country: this.editCountry.trim() || info.country,
        gender: this.editGender || info.gender,
      };
      this.saveExtraInfo(updated);
      return updated;
    });

    forkJoin([
      this.api.updateProfile(this.displayName.trim()),
      this.api.updatePreferences(
        this.language,
        this.theme,
        this.timeZone,
        this.emailTaskNotifications,
      ),
    ])
      .pipe(finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.store.profile.update((profile) => ({
            ...profile,
            displayName: this.displayName.trim(),
            preferredLanguage: this.language,
            themePreference: this.theme,
            timeZoneId: this.timeZone,
            emailTaskNotificationsEnabled: this.emailTaskNotifications,
          }));
          this.isEditing.set(false);
          this.notify('Đã cập nhật hồ sơ cá nhân thành công.');
        },
        error: (error) =>
          this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể lưu hồ sơ.'),
      });
  }

  uploadAvatar(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || this.avatarBusy()) return;
    if (!file.type.startsWith('image/') || file.size > 5 * 1024 * 1024) {
      this.error.set('Chọn ảnh PNG, JPG hoặc WEBP có dung lượng tối đa 5 MB.');
      return;
    }
    this.avatarBusy.set(true);
    this.error.set(null);
    this.api
      .uploadAvatar(file)
      .pipe(finalize(() => this.avatarBusy.set(false)))
      .subscribe({
        next: ({ avatarUrl }) => {
          this.avatarUrl.set(avatarUrl);
          this.store.profile.update((profile) => ({ ...profile, avatarUrl }));
          this.notify('Đã cập nhật ảnh đại diện.');
        },
        error: (error) =>
          this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể tải ảnh đại diện.'),
      });
  }

  onEmailTaskNotificationsChanged(enabled: boolean): void {
    if (this.gmailBusy()) return;
    this.emailTaskNotifications = enabled;
    this.api
      .updatePreferences(this.language, this.theme, this.timeZone, enabled)
      .subscribe({
        next: () => {
          this.store.profile.update((profile) => ({
            ...profile,
            emailTaskNotificationsEnabled: enabled,
          }));
          this.notify(enabled ? 'Đã bật email thông báo.' : 'Đã tắt email thông báo.');
        },
        error: (error) => {
          this.emailTaskNotifications = !enabled;
          this.error.set(
            error.error?.errors?.[0]?.message ?? 'Không thể cập nhật tùy chọn email.',
          );
        },
      });
  }

  openPasswordModal(): void {
    this.currentPassword = '';
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordError.set(null);
    this.passwordModalOpen.set(true);
  }

  changePassword(): void {
    this.passwordError.set(null);
    if (this.newPassword.length < 9 || !/[^A-Za-z0-9]/.test(this.newPassword)) {
      return this.passwordError.set('Mật khẩu mới cần ít nhất 9 ký tự và một ký tự đặc biệt.');
    }
    if (this.newPassword !== this.confirmPassword) {
      return this.passwordError.set('Hai mật khẩu mới chưa trùng khớp.');
    }
    this.passwordBusy.set(true);
    this.api
      .changePassword(this.currentPassword, this.newPassword)
      .pipe(finalize(() => this.passwordBusy.set(false)))
      .subscribe({
        next: () => {
          this.passwordModalOpen.set(false);
          this.auth.clearAndReturnToLogin();
        },
        error: (error) =>
          this.passwordError.set(
            error.error?.errors?.[0]?.message ?? 'Mật khẩu hiện tại không đúng.',
          ),
      });
  }

  confirmSignOut(): void {
    this.logoutConfirmation.set(false);
    this.auth.signOut();
  }

  async linkGmail(saveAfterLink = false): Promise<void> {
    if (this.gmailBusy()) return;
    this.gmailBusy.set(true);
    this.error.set(null);
    try {
      const authorization = await this.googleIdentity.requestGmailAuthorizationCode(
        this.store.profile().email,
      );
      const gmailLink = await firstValueFrom(
        this.api.linkGmail(authorization.code, authorization.redirectUri),
      );
      this.gmailLink.set(gmailLink);
      this.emailTaskNotifications = true;
      this.store.profile.update((profile) => ({
        ...profile,
        gmailLink,
        emailTaskNotificationsEnabled: true,
      }));
      this.notify(`Đã liên kết ${gmailLink.gmailAddress} và bật email thông báo.`);
      if (saveAfterLink) this.saveProfile();
    } catch (error) {
      this.error.set(this.readErrorMessage(error, 'Không thể liên kết Gmail.'));
    } finally {
      this.gmailBusy.set(false);
    }
  }

  storageLimitDisplay(): string {
    const quota = this.store.profile().quota;
    if (!quota?.maxStorageBytes) return '100 GB';
    const gb = Math.round(quota.maxStorageBytes / (1024 * 1024 * 1024));
    return gb > 0 ? `${gb} GB` : '100 GB';
  }

  isPlanExpired(): boolean {
    const quota = this.store.profile().quota;
    if (!quota || (quota.planCode || '').toUpperCase() === 'FREE') return false;
    const exp = quota.subscriptionExpiresAt;
    if (!exp) return false;
    try {
      return new Date(exp).getTime() < Date.now();
    } catch {
      return false;
    }
  }

  expirationDisplay(): string {
    const quota = this.store.profile().quota;
    const exp = quota?.subscriptionExpiresAt;
    if (!exp || (quota?.planCode || '').toUpperCase() === 'FREE') {
      return 'Không thời hạn';
    }
    try {
      const d = new Date(exp);
      const formatted = `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
      return `Hết hạn: ${formatted}`;
    } catch {
      return 'Không thời hạn';
    }
  }

  realEntitlements(): string[] {
    const quota = this.store.profile().quota;
    if (!quota) return [];
    const currentPlan = this.availablePlans().find(
      (p) => p.code.toUpperCase() === (quota.planCode || '').toUpperCase(),
    );
    if (currentPlan && currentPlan.entitlements?.length) {
      return currentPlan.entitlements;
    }
    const list: string[] = [];
    if (quota.maxOwnedProjects !== undefined && quota.maxOwnedProjects !== null) {
      list.push(
        quota.maxOwnedProjects >= 999
          ? 'Không giới hạn dự án'
          : `Tối đa ${quota.maxOwnedProjects} dự án sở hữu`,
      );
    }
    if (quota.maxStorageBytes) {
      const gb = Math.round(quota.maxStorageBytes / (1024 * 1024 * 1024));
      list.push(
        `Lưu trữ ${gb > 0 ? gb + ' GB' : Math.round(quota.maxStorageBytes / (1024 * 1024)) + ' MB'}`,
      );
    }
    if (quota.maxMembersPerProject) {
      list.push(`Tối đa ${quota.maxMembersPerProject} thành viên / dự án`);
    }
    if (quota.maxFileSizeBytes) {
      list.push(`Tải lên file tối đa ${Math.round(quota.maxFileSizeBytes / (1024 * 1024))} MB`);
    }
    if (quota.maxVersionsPerFile) {
      list.push(`Lưu tối đa ${quota.maxVersionsPerFile} phiên bản / file`);
    }
    return list;
  }

  private loadPlans(): void {
    this.api.getPlans().subscribe({
      next: (plans) => this.availablePlans.set(plans),
      error: () => {},
    });
  }

  private loadExtraInfo(userId: string): void {
    try {
      const saved = localStorage.getItem(`planora_profile_extra_${userId}`);
      if (saved) {
        this.extraInfo.set({ ...this.extraInfo(), ...JSON.parse(saved) });
      }
    } catch {
      // Ignore storage errors
    }
  }

  private saveExtraInfo(info: ExtraProfileInfo): void {
    const userId = this.store.profile().userId;
    if (!userId) return;
    try {
      localStorage.setItem(`planora_profile_extra_${userId}`, JSON.stringify(info));
    } catch {
      // Ignore storage errors
    }
  }

  private readErrorMessage(error: unknown, fallback: string): string {
    if (error instanceof Error && error.message) return error.message;
    if (typeof error !== 'object' || error === null || !('error' in error)) return fallback;
    const payload = (error as { error?: { detail?: string; errors?: Array<{ message?: string }> } })
      .error;
    return payload?.errors?.[0]?.message ?? payload?.detail ?? fallback;
  }

  private notify(value: string): void {
    this.toast.set(value);
    setTimeout(() => this.toast.set(null), 2800);
  }
}
