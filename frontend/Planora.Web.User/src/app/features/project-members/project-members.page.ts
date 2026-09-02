import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { LucideSearch, LucideShieldCheck, LucideUserMinus, LucideUserPlus, LucideUsersRound, LucideX } from '@lucide/angular';
import { finalize } from 'rxjs';
import { ProjectInvitation, ProjectMember, ProjectRole, ProjectRolePermissions, RegisteredUserMatch } from '../../core/api/api.models';
import { PlanoraApiService } from '../../core/api/planora-api.service';
import { QuotaNoticeService } from '../../core/feedback/quota-notice.service';
import { WorkspaceStore } from '../../core/workspace/workspace.store';

const PERMISSION_GROUPS = [
  { label: 'Project', permissions: [
    { code: 'project.view', label: 'Xem project', description: 'Truy cập không gian làm việc.' },
    { code: 'project.edit', label: 'Sửa project', description: 'Đổi tên, mô tả và thời gian project.' },
    { code: 'project.delete', label: 'Xóa project', description: 'Chỉ chủ sở hữu nội bộ được giữ quyền này.' },
    { code: 'project.manage_members', label: 'Quản lý thành viên', description: 'Mời, đổi vai trò và kick thành viên.' },
    { code: 'project.manage_roles', label: 'Cấu hình quyền', description: 'Thay đổi ma trận quyền theo vai trò.' },
    { code: 'project.view_analytics', label: 'Xem phân tích', description: 'Truy cập thống kê của project.' },
  ]},
  { label: 'Sprint', permissions: [
    { code: 'sprint.view', label: 'Xem Sprint', description: 'Xem kế hoạch Sprint.' },
    { code: 'sprint.create', label: 'Tạo Sprint', description: 'Tạo Sprint mới.' },
    { code: 'sprint.edit', label: 'Sửa Sprint', description: 'Cập nhật nội dung và thời gian.' },
    { code: 'sprint.close', label: 'Đóng Sprint', description: 'Kết thúc Sprint.' },
  ]},
  { label: 'Công việc', permissions: [
    { code: 'task.view', label: 'Xem task', description: 'Xem nội dung và trạng thái task.' },
    { code: 'task.create', label: 'Tạo task', description: 'Tạo công việc trong project.' },
    { code: 'task.edit', label: 'Sửa / xóa task', description: 'Cập nhật hoặc xóa công việc.' },
    { code: 'task.assign', label: 'Giao task', description: 'Thêm người thực hiện.' },
    { code: 'task.submit', label: 'Nộp task', description: 'Chỉ nộp task được giao.' },
    { code: 'task.review', label: 'Duyệt bài nộp', description: 'Duyệt hoặc yêu cầu làm lại.' },
    { code: 'task.extend_deadline', label: 'Đổi deadline', description: 'Gia hạn trực tiếp.' },
    { code: 'task.request_extension', label: 'Xin gia hạn', description: 'Gửi yêu cầu gia hạn.' },
  ]},
  { label: 'Tệp và tài liệu', permissions: [
    { code: 'folder.view', label: 'Xem thư mục', description: 'Xem cây thư mục chung.' },
    { code: 'folder.create', label: 'Tạo thư mục', description: 'Tạo thư mục con.' },
    { code: 'folder.edit', label: 'Đổi tên thư mục', description: 'Sửa tên thư mục.' },
    { code: 'folder.delete', label: 'Xóa thư mục', description: 'Xóa thư mục rỗng.' },
    { code: 'file.view', label: 'Xem / tải file', description: 'Mở và tải file chung.' },
    { code: 'file.upload', label: 'Upload file chung', description: 'Không ảnh hưởng quyền upload bài nộp.' },
    { code: 'file.edit', label: 'Sửa file', description: 'Đổi tên hoặc thêm phiên bản.' },
    { code: 'file.delete', label: 'Xóa file', description: 'Xóa file khỏi project.' },
    { code: 'document.view', label: 'Xem tài liệu', description: 'Đọc tài liệu nội bộ.' },
    { code: 'document.edit', label: 'Sửa tài liệu', description: 'Tạo và cập nhật tài liệu.' },
    { code: 'document.delete', label: 'Xóa tài liệu', description: 'Xóa tài liệu nội bộ.' },
  ]},
];

@Component({
  selector: 'app-project-members-page',
  imports: [FormsModule, LucideSearch, LucideShieldCheck, LucideUserMinus, LucideUserPlus, LucideUsersRound, LucideX],
  templateUrl: './project-members.page.html',
  styleUrl: './project-members.page.css',
})
export class ProjectMembersPage implements OnInit {
  readonly store = inject(WorkspaceStore);
  readonly roles = signal<ProjectRole[]>([]);
  readonly invitations = signal<ProjectInvitation[]>([]);
  readonly canManageMembers = computed(() => this.store.hasPermission('project.manage_members'));
  readonly canManageRoles = computed(() => this.store.hasPermission('project.manage_roles'));
  readonly matches = signal<RegisteredUserMatch[]>([]);
  readonly inviteOpen = signal(false);
  readonly removeTarget = signal<ProjectMember | null>(null);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly toast = signal<string | null>(null);
  readonly permissionOpen = signal(false);
  readonly roleMatrix = signal<ProjectRolePermissions[]>([]);
  readonly selectedRoleId = signal('');
  readonly selectedPermissionCodes = signal<string[]>([]);
  readonly selectedPermissionRole = computed(() => this.roleMatrix().find((role) => role.id === this.selectedRoleId()) ?? null);
  readonly savingPermissions = signal(false);
  readonly permissionError = signal<string | null>(null);
  readonly permissionGroups = PERMISSION_GROUPS;
  lookup = '';
  removeReason = '';
  invite = { email: '', roleId: '', days: 7 };
  private readonly api = inject(PlanoraApiService);
  private readonly quotaNotice = inject(QuotaNoticeService);

  ngOnInit(): void { this.loadMeta(); }
  activeCount(): number { return this.store.members().filter((member) => member.status.toLowerCase() === 'active').length; }
  pendingCount(): number { return this.invitations().filter((invitation) => invitation.status?.toLowerCase() === 'pending').length; }
  seatsLeft(): number { return Math.max(0, this.store.profile().quota.maxMembersPerProject - this.store.members().length); }
  initials(value: string): string { return value.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase(); }
  dateLabel(value: string): string { return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value)); }
  roleIdFor(member: ProjectMember): string { return this.roles().find((role) => member.roles.some((name) => name.toLowerCase() === role.name.toLowerCase()))?.id ?? ''; }
  roleLabel(member: ProjectMember): string { return member.roles.join(', ') || 'Chưa gán vai trò'; }
  permissionLabel(member: ProjectMember): string { return member.roles.includes('Leader') ? 'Quản lý & duyệt' : member.roles.includes('Viewer') ? 'Chỉ xem' : 'Xem & nộp việc được giao'; }
  openInvite(): void { if (!this.quotaNotice.checkMemberInvitation(this.store.profile().quota, this.activeCount())) return; this.error.set(null); this.matches.set([]); this.lookup = ''; this.inviteOpen.set(true); }
  findUsers(): void { if (this.lookup.trim().length < 3) return this.error.set('Nhập ít nhất 3 ký tự để tìm kiếm.'); this.api.findUsers(this.store.project().id, this.lookup.trim()).subscribe({ next: (matches) => this.matches.set(matches), error: (error) => this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể tìm tài khoản.') }); }
  selectMatch(match: RegisteredUserMatch): void { this.invite.email = match.email; }
  inviteMember(): void { if (!this.invite.email || !this.invite.roleId || this.busy()) return; if (!this.quotaNotice.checkMemberInvitation(this.store.profile().quota, this.activeCount())) return; this.busy.set(true); this.error.set(null); this.api.inviteMember(this.store.project().id, this.invite.email, this.invite.roleId, this.invite.days).pipe(finalize(() => this.busy.set(false))).subscribe({ next: (invitation) => { this.invitations.update((items) => [invitation, ...items]); this.inviteOpen.set(false); this.notify('Đã gửi lời mời.'); }, error: (error) => { if (!this.quotaNotice.isQuotaError(error)) this.error.set(error.error?.errors?.[0]?.message ?? 'Không thể gửi lời mời.'); } }); }
  changeRole(member: ProjectMember, roleId: string): void { if (!roleId || !this.canManageMembers()) return; this.api.changeMemberRole(this.store.project().id, member.membershipId, roleId).subscribe({ next: () => { const role = this.roles().find((item) => item.id === roleId); if (role) this.store.members.update((items) => items.map((item) => item.membershipId === member.membershipId ? { ...item, roles: [role.name] } : item)); this.notify('Đã cập nhật vai trò. Quyền mới có hiệu lực ngay.'); }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể đổi vai trò.') }); }
  removeMember(): void { const member = this.removeTarget(); if (!member || !this.removeReason.trim()) return; this.api.removeMember(this.store.project().id, member.membershipId, this.removeReason.trim()).subscribe({ next: () => { this.store.members.update((items) => items.filter((item) => item.membershipId !== member.membershipId)); this.removeTarget.set(null); this.removeReason = ''; this.notify('Đã xóa quyền truy cập.'); }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể xóa thành viên.') }); }
  openPermissions(): void { if (!this.canManageRoles()) return; this.permissionError.set(null); this.api.getProjectRolePermissions(this.store.project().id).subscribe({ next: (roles) => { this.roleMatrix.set(roles); const firstEditable = roles.find((role) => role.isEditable) ?? roles[0]; this.selectPermissionRole(firstEditable?.id ?? ''); this.permissionOpen.set(true); }, error: (error) => this.notify(error.error?.errors?.[0]?.message ?? 'Không thể tải ma trận quyền.') }); }
  selectPermissionRole(roleId: string): void { this.selectedRoleId.set(roleId); this.selectedPermissionCodes.set([...(this.roleMatrix().find((role) => role.id === roleId)?.permissionCodes ?? [])]); this.permissionError.set(null); }
  togglePermission(code: string, checked: boolean): void { if (!this.selectedPermissionRole()?.isEditable || code === 'project.view') return; this.selectedPermissionCodes.update((codes) => checked ? [...new Set([...codes, code])] : codes.filter((item) => item !== code)); }
  savePermissions(): void { const role = this.selectedPermissionRole(); if (!role?.isEditable || this.savingPermissions()) return; this.savingPermissions.set(true); this.permissionError.set(null); this.api.updateProjectRolePermissions(this.store.project().id, role.id, this.selectedPermissionCodes()).pipe(finalize(() => this.savingPermissions.set(false))).subscribe({ next: () => { this.roleMatrix.update((roles) => roles.map((item) => item.id === role.id ? { ...item, permissionCodes: [...new Set([...this.selectedPermissionCodes(), 'project.view'])] } : item)); this.notify(`Đã lưu quyền cho ${role.name}.`); }, error: (error) => this.permissionError.set(error.error?.errors?.[0]?.message ?? 'Không thể lưu quyền.') }); }
  private loadMeta(): void { const id = this.store.project().id; if (!id) return; this.api.getRoles(id).subscribe({ next: (roles) => { this.roles.set(roles); this.invite.roleId = roles.find((role) => role.code === 'MEMBER')?.id ?? roles[0]?.id ?? ''; }, error: () => this.notify('Không thể tải danh sách vai trò.') }); if (this.canManageMembers()) this.api.getInvitations(id).subscribe({ next: (invitations) => this.invitations.set(invitations), error: () => this.invitations.set([]) }); }
  private notify(value: string): void { this.toast.set(value); setTimeout(() => this.toast.set(null), 2400); }
}
