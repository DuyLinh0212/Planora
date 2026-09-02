# PRODUCT SCOPE

## 1. Identity
- Register/Login/Logout.
- Refresh Token.
- Forgot/Change Password.
- Google/Facebook Login.
- Profile.

## 2. Project
- CRUD Project.
- Start/End time, status.
- GitHub repository URL.
- Invitation: invite → accept/reject.
- Remove member.
- Project Role/Permission.
- Audit Log.

## 3. Agile / Scrum
- Product/Project Backlog.
- Sprint CRUD.
- Sprint Goal.
- Start/Close Sprint.
- Add/Move Task giữa Backlog và Sprint.
- Sprint progress.

## 4. Task
- CRUD Task.
- Assign member.
- Priority, deadline, acceptance criteria.
- Task Submission/Result.
- Review, approve, rework.
- Auto-expire khi quá deadline.
- Extension Request.
- Leader direct extension.
- Deadline history.

## 5. Project Storage
- Root folder theo Project.
- Folder/Subfolder.
- File upload/download/version.
- Internal editable document/version.
- Move/Rename/Delete.
- Last edited by / Audit.
- Quyền view/create/edit/delete/upload theo role/member/folder.
- Link FileVersion vào Task Submission.

## 6. RBAC
- Owner/Leader/Member/Viewer mặc định.
- Custom Role.
- Permission codes.
- Folder/resource override.

## 7. Analytics
- Project/Sprint/Task statistics.
- On-time/late/early.
- Contribution.
- Member performance.
- Charts.

## 8. Subscription
- Free/Paid Plan.
- Giới hạn số Project.
- Storage quota.
- Upgrade account.
- MoMo và chuyển khoản ngân hàng tự động qua SePay webhook.
- Payment callback được xác minh và xử lý idempotent; không chờ admin duyệt.
- Payment history.

## 9. Notification
- Invitation.
- Task assigned.
- Near deadline/expired.
- Extension request/result.
- Submission/review.
- Payment/subscription.

## 10. Admin
Admin được:
- Quản lý account hệ thống.
- Quản lý plan.
- Xem/xử lý payment.
- Feedback.
- Aggregate stats: user, project, active/completed, storage...

Admin không được:
- Sửa Project/Task của User.
- Tự ý đọc/sửa nội dung private Folder/Document/File.

## 11. UI
- Light.
- Dark.
- Calm Blue.
- Logo web/mobile.
- Context Help `?`.

## Ngoài MVP
- Chat.
- Video call.
- Google Docs-like collaborative editing.
- AI.
- Full GitHub integration.
- Calendar sync.
- Enterprise SSO.
