import { Injectable, signal } from '@angular/core';
import { UsageQuota } from '../api/api.models';

type ApiProblem = {
  code?: unknown;
  errors?: Array<{ code?: unknown; message?: unknown }>;
};

export type QuotaNotice = {
  title: string;
  message: string;
};

@Injectable({ providedIn: 'root' })
export class QuotaNoticeService {
  readonly notice = signal<QuotaNotice | null>(null);
  private dismissTimer: ReturnType<typeof setTimeout> | null = null;

  isQuotaError(error: unknown): boolean {
    return this.quotaCode(error) !== null;
  }

  showApiError(error: unknown): boolean {
    const code = this.quotaCode(error);
    if (!code) return false;
    this.showForCode(code);
    return true;
  }

  checkProjectCreation(quota: UsageQuota): boolean {
    if (!quota.maxOwnedProjects || quota.ownedProjects < quota.maxOwnedProjects) return true;
    this.showForCode('quota.owned_projects_reached');
    return false;
  }

  checkMemberInvitation(quota: UsageQuota, activeMembers: number): boolean {
    if (!quota.maxMembersPerProject || activeMembers < quota.maxMembersPerProject) return true;
    this.showForCode('quota.project_members_reached');
    return false;
  }

  checkUpload(quota: UsageQuota, projectStorageBytes: number, incomingBytes: number): boolean {
    if (incomingBytes > quota.maxFileSizeBytes) {
      this.showForCode('quota.file_too_large');
      return false;
    }
    if (
      quota.storageBytes + incomingBytes > quota.maxStorageBytes ||
      (incomingBytes === 0 && quota.storageBytes >= quota.maxStorageBytes)
    ) {
      this.showForCode('quota.total_storage_reached');
      return false;
    }
    if (
      projectStorageBytes + incomingBytes > quota.maxProjectStorageBytes ||
      (incomingBytes === 0 && projectStorageBytes >= quota.maxProjectStorageBytes)
    ) {
      this.showForCode('quota.project_storage_reached');
      return false;
    }
    return true;
  }

  dismiss(): void {
    if (this.dismissTimer) clearTimeout(this.dismissTimer);
    this.dismissTimer = null;
    this.notice.set(null);
  }

  private quotaCode(error: unknown): string | null {
    const problem = this.problemFrom(error);
    const code = problem?.code ?? problem?.errors?.find((item) => typeof item.code === 'string')?.code;
    return typeof code === 'string' && code.startsWith('quota.') ? code : null;
  }

  private problemFrom(error: unknown): ApiProblem | null {
    if (!error || typeof error !== 'object') return null;
    const response = error as { error?: unknown };
    const problem = response.error ?? error;
    return problem && typeof problem === 'object' ? (problem as ApiProblem) : null;
  }

  private showForCode(code: string): void {
    const copy: Record<string, QuotaNotice> = {
      'quota.owned_projects_reached': {
        title: 'Đã đạt giới hạn project',
        message: 'Gói hiện tại đã dùng hết số project được sở hữu. Nâng cấp gói hoặc xóa một project do bạn sở hữu để tiếp tục.',
      },
      'quota.project_members_reached': {
        title: 'Project đã đủ thành viên',
        message: 'Số thành viên hoạt động đã chạm giới hạn của gói hiện tại. Nâng cấp gói để mời thêm người.',
      },
      'quota.file_too_large': {
        title: 'Tệp vượt giới hạn dung lượng',
        message: 'Chọn tệp nhỏ hơn hoặc nâng cấp gói để tải tệp có dung lượng lớn hơn.',
      },
      'quota.total_storage_reached': {
        title: 'Đã hết dung lượng lưu trữ',
        message: 'Dung lượng của workspace đã đạt giới hạn gói. Dọn bớt tệp hoặc nâng cấp trước khi tải lên.',
      },
      'quota.project_storage_reached': {
        title: 'Project đã hết dung lượng',
        message: 'Dung lượng dành cho project này đã đầy. Dọn bớt tệp hoặc nâng cấp gói để tiếp tục.',
      },
      'quota.daily_upload_bytes_reached': {
        title: 'Đã hết dung lượng tải lên hôm nay',
        message: 'Gói hiện tại đã dùng hết dung lượng tải tệp trong ngày. Hãy thử lại vào ngày mai hoặc nâng cấp gói.',
      },
      'quota.daily_upload_count_reached': {
        title: 'Đã hết lượt tải tệp hôm nay',
        message: 'Gói hiện tại đã dùng hết số lượt tải tệp trong ngày. Hãy thử lại vào ngày mai hoặc nâng cấp gói.',
      },
      'quota.file_versions_reached': {
        title: 'Tệp đã đủ phiên bản',
        message: 'Tệp này đã chạm giới hạn số phiên bản của gói hiện tại. Nâng cấp gói để tiếp tục lưu phiên bản mới.',
      },
    };
    this.show(copy[code] ?? {
      title: 'Đã đạt giới hạn gói',
      message: 'Thao tác này vượt giới hạn của gói hiện tại. Nâng cấp gói để tiếp tục.',
    });
  }

  private show(notice: QuotaNotice): void {
    if (this.dismissTimer) clearTimeout(this.dismissTimer);
    this.notice.set(notice);
    this.dismissTimer = setTimeout(() => this.dismiss(), 8_000);
  }
}
