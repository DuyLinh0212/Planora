import { Injectable, Pipe, PipeTransform, inject, signal } from '@angular/core';

export type PlanoraLanguage = 'vi' | 'en';
type Dictionary = Record<string, string>;

const VI: Dictionary = {
  'nav.projects': 'Dự án', 'nav.account': 'Cài đặt cá nhân', 'nav.billing': 'Gói & thanh toán', 'nav.support': 'Hỗ trợ', 'nav.guide': 'Hướng dẫn',
  'action.createProject': 'Tạo dự án', 'action.signOut': 'Đăng xuất', 'action.retry': 'Thử lại', 'action.accept': 'Chấp nhận', 'action.reject': 'Từ chối',
  'notifications.label': 'Hộp thư', 'notifications.title': 'Thông báo', 'notifications.empty': 'Không có thông báo mới', 'notifications.emptyHint': 'Mọi cập nhật quan trọng sẽ xuất hiện ở đây.', 'notifications.all': 'Xem tất cả thông báo trong 7 ngày',
  'project.overview': 'Tổng quan', 'project.tasks': 'Công việc', 'project.sprints': 'Sprint', 'project.views': 'Góc nhìn', 'project.files': 'Tệp', 'project.members': 'Thành viên', 'project.analytics': 'Phân tích', 'project.settings': 'Cài đặt',
  'common.loading': 'Đang xử lý…', 'common.notAvailable': 'Chưa có',
  'common.save': 'Lưu thay đổi', 'common.cancel': 'Hủy', 'common.close': 'Đóng', 'common.delete': 'Xóa', 'common.download': 'Tải xuống', 'common.preview': 'Xem trước',
  'account.title': 'Tài khoản cá nhân', 'account.description': 'Quản lý danh tính, giao diện, thông báo và bảo mật của bạn.', 'account.profile': 'Hồ sơ',
  'account.appearance': 'Giao diện', 'account.security': 'Bảo mật', 'account.notifications': 'Thông báo', 'account.signOut': 'Đăng xuất',
  'account.signOutHint': 'Kết thúc phiên trên thiết bị này.', 'account.signOutConfirm': 'Bạn có chắc muốn đăng xuất khỏi Planora không?',
  'account.terms': 'Điều khoản sử dụng', 'account.support': 'Hỗ trợ', 'account.billing': 'Gói & thanh toán',
  'files.title': 'Tệp & tài liệu', 'files.description': 'Duyệt thư mục, tài liệu và phiên bản theo cách quen thuộc.', 'files.all': 'Tất cả tệp',
  'files.newFolder': 'Thư mục mới', 'files.upload': 'Tải tệp', 'files.newDocument': 'Tài liệu mới', 'files.search': 'Tìm kiếm tệp, tài liệu…',
  'files.location': 'Vị trí', 'files.name': 'Tên', 'files.type': 'Loại', 'files.version': 'Phiên bản', 'files.size': 'Dung lượng', 'files.modified': 'Cập nhật',
  'views.title': 'Góc nhìn project', 'views.description': 'Một nguồn dữ liệu, nhiều cách theo dõi tiến độ.', 'views.list': 'Danh sách',
  'views.board': 'Kanban', 'views.sprint': 'Sprint', 'views.backlog': 'Backlog', 'views.calendar': 'Lịch', 'views.timeline': 'Roadmap',
  'views.gantt': 'Gantt', 'views.workload': 'Khối lượng', 'views.dependency': 'Phụ thuộc', 'views.milestone': 'Cột mốc', 'views.activity': 'Hoạt động',
  'projects.catalog': 'Danh mục công việc', 'projects.title': 'Chọn một project để bắt đầu', 'projects.description': 'Mọi công việc, sprint, tệp và thành viên chỉ xuất hiện sau khi bạn mở project.',
  'projects.total': 'Tổng project', 'projects.joined': 'Project tham gia', 'projects.active': 'Đang hoạt động', 'projects.search': 'Tìm theo tên hoặc mô tả…',
  'projects.all': 'Tất cả', 'projects.draft': 'Bản nháp', 'projects.completed': 'Hoàn thành', 'projects.open': 'Mở project', 'projects.members': 'Thành viên', 'projects.updated': 'Cập nhật',
  'projects.empty': 'Chưa có project phù hợp', 'projects.emptyHint': 'Tạo project đầu tiên để bắt đầu lập kế hoạch cùng nhóm.', 'projects.createFirst': 'Tạo project đầu tiên',
  'projects.new': 'Project mới', 'projects.createTitle': 'Tạo không gian làm việc', 'projects.name': 'Tên project', 'projects.descriptionLabel': 'Mô tả', 'projects.startDate': 'Ngày bắt đầu', 'projects.endDate': 'Ngày kết thúc',
  'projects.createAndOpen': 'Tạo và mở project', 'status.planning': 'Đang lập kế hoạch', 'status.active': 'Đang chạy', 'status.paused': 'Tạm dừng', 'status.completed': 'Hoàn tất', 'status.cancelled': 'Đã hủy', 'status.draft': 'Bản nháp',
};

const EN: Dictionary = {
  'nav.projects': 'Projects', 'nav.account': 'Personal settings', 'nav.billing': 'Plans & billing', 'nav.support': 'Support', 'nav.guide': 'Guide',
  'action.createProject': 'Create project', 'action.signOut': 'Sign out', 'action.retry': 'Try again', 'action.accept': 'Accept', 'action.reject': 'Decline',
  'notifications.label': 'Inbox', 'notifications.title': 'Notifications', 'notifications.empty': 'No new notifications', 'notifications.emptyHint': 'Important updates will appear here.', 'notifications.all': 'View all notifications from the last 7 days',
  'project.overview': 'Overview', 'project.tasks': 'Tasks', 'project.sprints': 'Sprints', 'project.views': 'Views', 'project.files': 'Files', 'project.members': 'Members', 'project.analytics': 'Analytics', 'project.settings': 'Settings',
  'common.loading': 'Working…', 'common.notAvailable': 'Not available',
  'common.save': 'Save changes', 'common.cancel': 'Cancel', 'common.close': 'Close', 'common.delete': 'Delete', 'common.download': 'Download', 'common.preview': 'Preview',
  'account.title': 'Personal settings', 'account.description': 'Manage your identity, appearance, notifications and security.', 'account.profile': 'Profile',
  'account.appearance': 'Appearance', 'account.security': 'Security', 'account.notifications': 'Notifications', 'account.signOut': 'Sign out',
  'account.signOutHint': 'End this device session.', 'account.signOutConfirm': 'Are you sure you want to sign out of Planora?',
  'account.terms': 'Terms of use', 'account.support': 'Support', 'account.billing': 'Plans & billing',
  'files.title': 'Files & documents', 'files.description': 'Browse folders, documents and versions in a familiar workspace.', 'files.all': 'All files',
  'files.newFolder': 'New folder', 'files.upload': 'Upload file', 'files.newDocument': 'New document', 'files.search': 'Search files and documents…',
  'files.location': 'Location', 'files.name': 'Name', 'files.type': 'Type', 'files.version': 'Version', 'files.size': 'Size', 'files.modified': 'Updated',
  'views.title': 'Project views', 'views.description': 'One source of truth, several ways to follow progress.', 'views.list': 'List',
  'views.board': 'Kanban', 'views.sprint': 'Sprint', 'views.backlog': 'Backlog', 'views.calendar': 'Calendar', 'views.timeline': 'Roadmap',
  'views.gantt': 'Gantt', 'views.workload': 'Workload', 'views.dependency': 'Dependencies', 'views.milestone': 'Milestones', 'views.activity': 'Activity',
  'projects.catalog': 'Project catalogue', 'projects.title': 'Choose a project to get started', 'projects.description': 'Tasks, sprints, files and members appear after you open a project.',
  'projects.total': 'Total projects', 'projects.joined': 'Joined projects', 'projects.active': 'Active', 'projects.search': 'Search by name or description…',
  'projects.all': 'All', 'projects.draft': 'Draft', 'projects.completed': 'Completed', 'projects.open': 'Open project', 'projects.members': 'Members', 'projects.updated': 'Updated',
  'projects.empty': 'No matching projects', 'projects.emptyHint': 'Create your first project to start planning with your team.', 'projects.createFirst': 'Create your first project',
  'projects.new': 'New project', 'projects.createTitle': 'Create a workspace', 'projects.name': 'Project name', 'projects.descriptionLabel': 'Description', 'projects.startDate': 'Start date', 'projects.endDate': 'End date',
  'projects.createAndOpen': 'Create and open project', 'status.planning': 'Planning', 'status.active': 'Active', 'status.paused': 'Paused', 'status.completed': 'Completed', 'status.cancelled': 'Cancelled', 'status.draft': 'Draft',
};

@Injectable({ providedIn: 'root' })
export class I18nService {
  readonly language = signal<PlanoraLanguage>((localStorage.getItem('planora.user.language') as PlanoraLanguage) || 'vi');
  constructor() { this.applyDocumentLanguage(); }
  setLanguage(language: PlanoraLanguage): void { this.language.set(language); localStorage.setItem('planora.user.language', language); this.applyDocumentLanguage(); }
  t(key: string): string { return (this.language() === 'en' ? EN : VI)[key] ?? VI[key] ?? key; }
  private applyDocumentLanguage(): void {
    document.documentElement.lang = this.language();
    document.documentElement.dataset['language'] = this.language();
  }
}

@Pipe({ name: 't', standalone: true, pure: false })
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);
  transform(key: string): string { return this.i18n.t(key); }
}
