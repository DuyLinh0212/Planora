# ROADMAP

Roadmap này phản ánh trạng thái của code hiện tại, không phải checklist thiết kế ban đầu.

## Đã hoàn thành

### Foundation
- Tài liệu và Clean Architecture + Modular Monolith.
- ASP.NET Core/.NET 10, EF Core 10, Npgsql.
- PostgreSQL local và Neon PostgreSQL production.
- Problem Details, OpenAPI, logging, health checks, Docker.

### Identity + Project
- Register/login, JWT access/refresh, logout/revoke.
- Password recovery/change và Google/Facebook boundary.
- Project CRUD, invitation/member lifecycle, RBAC và audit log.

### Agile + Task
- Sprint/backlog, task/assignee, submission/review/rework.
- Deadline history, member extension và leader direct extension.
- API-hosted overdue task expiration service.

### Storage
- Folder/file/document, Cloudinary hoặc local storage.
- Versioning, folder rules, quota/file-size validation.
- Link file version vào task submission.

### Realtime, billing và admin
- SignalR notification hub và persisted notifications.
- Subscription plans, payment history.
- SePay bank-transfer webhook với API-key/amount validation và idempotency.
- Admin overview, accounts, plans, payments, feedback, support và maintenance.

## Đang hoàn thiện trước production

- Điền secrets trên Render/Vercel/GitHub Environment.
- Bật branch protection và required `CI success` check.
- Chạy smoke test callback SePay ở sandbox.
- Thiết lập monitoring, backup/PITR Neon và quy trình rollback.
- Thay `xlsx` trước hạn allowlist 2026-12-01.

## Tương lai

- GitHub App, commit/PR liên kết Task.
- Calendar sync, AI hỗ trợ và collaborative editor.
- Tách worker riêng nếu background workload cần scale độc lập.
- MFA và malware scanning upload.
