# ACTORS AND PERMISSIONS

## Tác nhân

1. Guest
2. Authenticated User
3. Project Owner
4. Project Leader
5. Project Member
6. Project Viewer
7. System Administrator
8. API-hosted Background Services/System
9. Google/Facebook
10. MoMo/SePay (bank-transfer webhook)

## Role mặc định

| Hành động | Owner | Leader | Member | Viewer |
|---|:---:|:---:|:---:|:---:|
| View Project | ✅ | ✅ | ✅ | ✅ |
| Edit Project | ✅ | tùy quyền | ❌ | ❌ |
| Delete Project | ✅ | ❌ | ❌ | ❌ |
| Manage Members | ✅ | tùy quyền | ❌ | ❌ |
| Manage Roles | ✅ | tùy quyền | ❌ | ❌ |
| Manage Sprint | ✅ | ✅ | ❌ | ❌ |
| Create/Edit Task | ✅ | ✅ | ❌* | ❌ |
| Assign Task | ✅ | ✅ | ❌ | ❌ |
| Submit assigned Task | ✅ | ✅ | ✅ | ❌ |
| Review Submission | ✅ | ✅ | ❌ | ❌ |
| Extend Deadline | ✅ | ✅ | ❌ | ❌ |
| Request Extension | ✅ | ✅ | ✅ | ❌ |
| View Storage | ✅ | ✅ | tùy quyền | tùy quyền |
| Upload | ✅ | ✅ | tùy quyền | ❌ |
| Edit Own File/Document | ✅ | ✅ | ✅ | ❌ |
| Edit Others | ✅ | tùy quyền | tùy quyền | ❌ |
| Delete Folder/File | ✅ | tùy quyền | tùy quyền | ❌ |
| Analytics | ✅ | ✅ | tùy quyền | tùy quyền |

`*` Chỉ khi custom permission cho phép; assignee không tự động có quyền sửa yêu cầu Task.

## Permission codes

### Project
`project.view`, `project.edit`, `project.delete`, `project.manage_members`, `project.manage_roles`, `project.view_analytics`

### Sprint
`sprint.view`, `sprint.create`, `sprint.edit`, `sprint.close`

### Task
`task.view`, `task.create`, `task.edit`, `task.assign`, `task.submit`, `task.review`, `task.extend_deadline`, `task.request_extension`

### Storage
`folder.view`, `folder.create`, `folder.edit`, `folder.delete`, `file.view`, `file.upload`, `file.edit`, `file.delete`, `document.view`, `document.edit`, `document.delete`

## Permission resolution

```text
Authenticated?
  ↓
Project Membership?
  ↓
Project Role Permission
  ↓
Folder/Resource Override
  ↓
Ownership Rule
  ↓
ALLOW / DENY
```

- Backend là nơi quyết định cuối cùng.
- Deny cụ thể có thể override Allow tổng quát.
- Owner không thể bị remove trước khi transfer ownership.
- Admin hệ thống không tự động là member của Project.
