# TECH STACK

## Backend

| Thành phần | Lựa chọn |
|---|---|
| Runtime | .NET 10 LTS |
| API | ASP.NET Core 10 Web API |
| ORM | Entity Framework Core 10 |
| Architecture | Clean Architecture + Modular Monolith |
| Realtime | SignalR |
| Auth | JWT Access Token + Refresh Token |
| Social Login | Google, Facebook |
| API Docs | OpenAPI |
| Background | API-hosted `BackgroundService` |
| Validation | Application services + Problem Details |

## Frontend

### User Web
- Web app cho người dùng.
- Deploy Vercel.
- Framework: Angular 20.
- Tooling: Angular CLI 20, standalone components, Angular Router, SCSS.

### Admin Web
- Web app riêng.
- Framework: Angular 20.
- Dùng chung Backend API.
- Không có quyền mặc định đọc/sửa private Project content.

### Mobile
- Mobile app cho User.
- Framework: Flutter 3.38.7 / Dart 3.10.7.
- Dùng cùng API contract với Web.

## Database

- Local: PostgreSQL.
- Production: Neon PostgreSQL.
- Migration bằng EF Core.
- Không lưu binary file trong DB.

## Storage

- Cloudinary.
- Binary file/attachment/avatar/logo.
- Private resource dùng signed/authenticated access.
- DB chỉ lưu metadata, public id, version, owner, size...

## Notification

- SignalR cho realtime.
- Notification vẫn persist trong SQL để xem lại.

## Authentication & Authorization

- JWT + Refresh Token.
- Google/Facebook external login.
- Project RBAC.
- Resource/Folder permission override.

## Payment

- MoMo.
- Chuyển khoản ngân hàng qua SePay webhook.
- Backend verify webhook/signature, amount và provider reference.
- Có idempotency cho request/callback retry.

## GitHub

### MVP
Lưu repository URL.

### Future
GitHub OAuth/App, webhook, commit/PR link với Task.

## Deploy

| Thành phần | Provider |
|---|---|
| User Web | Vercel |
| Admin Web | Vercel |
| Backend | Render |
| Database | Neon PostgreSQL |
| Storage | Cloudinary |

## Nguyên tắc dependency

Ưu tiên built-in của ASP.NET Core trước khi thêm package. Không thêm abstraction/framework chỉ vì “chuẩn bài” nếu nó làm code khó đọc hơn.
