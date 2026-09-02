# Project Management Platform

> **Tên sản phẩm:** Planora  
> **Trạng thái:** Đã triển khai nền tảng; đang chuẩn bị production

Nền tảng quản lý dự án dành cho sinh viên, nhân viên, freelancer, nhóm nhỏ và bất kỳ ai cần tổ chức dự án, phân chia công việc, lưu tài liệu và theo dõi đóng góp một cách có hệ thống.

## Mục tiêu chính

- Quản lý Project, thành viên, Sprint, Task.
- Task có deadline, assignee, acceptance criteria và phần **Submission/Kết quả**.
- Task quá hạn tự chuyển trạng thái hết hạn; thành viên có thể xin gia hạn.
- Leader có thể chủ động gia hạn; trường hợp này không tính là trễ theo business rule.
- Mỗi Project có hệ thống Folder/File/Document riêng.
- RBAC theo Project và quyền chi tiết theo Folder/Resource.
- Lưu lịch sử chỉnh sửa, version tài liệu và Audit Log.
- Thống kê tiến độ, đóng góp, on-time/late, performance bằng biểu đồ.
- Gói tài khoản và thanh toán chuyển khoản ngân hàng tự động qua SePay.
- Link Project với GitHub bằng URL ở MVP.
- Admin quản lý tài khoản, gói, payment, feedback và số liệu tổng hợp; **không can thiệp nội dung Project của User**.
- UI có Light/Dark/Calm Blue theme, logo và dấu `?` hướng dẫn chức năng.

## Hệ thống

```text
User Web ─────┐
Mobile ───────┼──> ASP.NET Core 10 API ──> Neon PostgreSQL
Admin Web ────┘              │
                             ├── Cloudinary
                             ├── SignalR
                             ├── Google/Facebook
                             └── SePay bank-transfer webhook
```

## Tech Stack

- Backend: ASP.NET Core 10 / .NET 10
- Architecture: Clean Architecture + Modular Monolith
- ORM: EF Core 10
- DB: PostgreSQL local, Neon PostgreSQL production
- Storage: Cloudinary
- Auth: JWT + Refresh Token
- External Login: Google, Facebook
- Realtime: SignalR
- Payment: SePay bank-transfer webhook, idempotency
- Deploy: Vercel (Web/Admin), Render (Backend), Neon PostgreSQL (DB)

## Documentation

Đọc theo thứ tự:

1. `docs/TECH_STACK.md`
2. `docs/PRODUCT_SCOPE.md`
3. `docs/ACTORS_AND_PERMISSIONS.md`
4. `docs/BUSINESS_RULES.md`
5. `docs/WORKFLOWS.md`
6. `docs/PROJECT_ARCHITECTURE.md`
7. `docs/DATABASE_DESIGN.md`
8. `docs/DATABASE_ERD.drawio`
9. `docs/API_DESIGN.md`
10. `docs/STORAGE_DESIGN.md`
11. `docs/SECURITY.md`
12. `docs/ANALYTICS_AND_SCORING.md`
13. `docs/PAYMENT_AND_SUBSCRIPTION.md`
14. `docs/UI_UX_REQUIREMENTS.md`
15. `docs/DEPLOYMENT.md`
16. `docs/TESTING.md`
17. `docs/ROADMAP.md`
18. `docs/ARCHITECTURE_DECISIONS.md`
19. `docs/CI_CD.md`
20. `docs/ENVIRONMENT.md`
