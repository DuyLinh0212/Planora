# Planora

Planora là nền tảng quản lý dự án và cộng tác dành cho nhóm nhỏ. Sản phẩm gom kế hoạch, công việc, tài liệu, thành viên, thông báo và tiến độ vào một không gian làm việc thống nhất, dễ theo dõi và có kiểm soát quyền truy cập.

README này chỉ mô tả sản phẩm và kiến trúc. Thông tin vận hành, hướng dẫn triển khai và cấu hình môi trường không được đặt tại đây.

## Dự án làm gì?

Planora hỗ trợ toàn bộ vòng đời công việc của một dự án:

- Tạo workspace/project và tổ chức thành viên theo vai trò.
- Lập sprint, quản lý backlog, giao việc và theo dõi deadline.
- Nộp kết quả, duyệt, yêu cầu làm lại và lưu lịch sử thay đổi.
- Quản lý folder, document, file và phiên bản tài liệu theo từng project.
- Cập nhật thông báo, hoạt động và trạng thái theo thời gian thực.
- Theo dõi tiến độ, mức đóng góp và các chỉ số tổng hợp.
- Quản lý gói dịch vụ và thanh toán chuyển khoản ngân hàng tự động qua SePay.
- Cung cấp khu vực quản trị để quản lý tài khoản, gói dịch vụ, giao dịch, phản hồi và số liệu tổng hợp.

Các callback thanh toán được xác thực và xử lý idempotent, giúp một giao dịch không bị ghi nhận nhiều lần khi mạng chập chờn hoặc nhà cung cấp gửi lại webhook.

## Dành cho ai?

- Sinh viên và nhóm làm đồ án.
- Freelancer quản lý nhiều khách hàng hoặc dự án.
- Nhóm nhỏ cần phân công và kiểm soát tiến độ.
- Leader cần theo dõi deadline, quyền truy cập và kết quả bàn giao.
- Quản trị viên cần số liệu tổng hợp mà không truy cập nội dung riêng tư của project.

## Nền tảng sản phẩm

Planora được tổ chức thành ba trải nghiệm chính:

| Nền tảng | Vai trò |
| --- | --- |
| User Web | Không gian làm việc chính cho người dùng và thành viên project |
| Admin Web | Control room dành cho quản trị viên hệ thống |
| Mobile | Ứng dụng di động cho việc theo dõi, cập nhật và cộng tác khi di chuyển |

Các client dùng chung backend API và cùng áp dụng mô hình xác thực, phân quyền theo project và chính sách bảo mật của hệ thống.

## Kiến trúc

Backend là modular monolith theo Clean Architecture:

```text
User Web ─────┐
Admin Web ────┼──> ASP.NET Core API ──> PostgreSQL
Mobile ───────┘            │
                            ├── Identity & RBAC
                            ├── Projects, Sprints & Tasks
                            ├── Storage & Versions
                            ├── Notifications & SignalR
                            ├── Billing & Payment Reconciliation
                            └── Admin & Audit
```

Các lớp chính của backend:

- **Domain** — entity, value object và invariant nghiệp vụ; không phụ thuộc framework hạ tầng.
- **Application** — use case, policy, permission resolver, DTO và các port/gateway.
- **Infrastructure** — EF Core, PostgreSQL, xác thực token, storage provider, email và tích hợp bên ngoài.
- **API** — controller mỏng, Problem Details, health checks, rate limiting, OpenAPI và background services.

Frontend User, Admin và Mobile được tách riêng theo mục đích sử dụng nhưng chia sẻ cùng hợp đồng API. Các tác vụ nền cần thiết cho hạn deadline, gửi email và dọn retention chạy cùng API dưới dạng hosted background services.

## Tính năng chính

### Identity và phân quyền

- Đăng ký, đăng nhập, refresh token và đăng xuất an toàn.
- Biên tích hợp cho đăng nhập Google/Facebook.
- Vai trò Owner, Leader, Member và Viewer ở cấp project.
- Audit log cho các thao tác quản trị và thay đổi quan trọng.

### Project và Agile workflow

- Project CRUD, mời thành viên và quản lý membership.
- Sprint, backlog, trạng thái task, priority, assignee và acceptance criteria.
- Deadline history, xin gia hạn, gia hạn trực tiếp và tự xử lý task quá hạn.
- Submission theo version, review, approve và request rework.

### Tài liệu và lưu trữ

- Folder tree và quyền truy cập chi tiết.
- Document, file, version và liên kết file-version vào submission.
- Storage boundary hỗ trợ Cloudinary và local fallback theo cấu hình môi trường.

### Thông báo và cộng tác

- Thông báo trong hệ thống và cập nhật realtime qua SignalR.
- Email cho các sự kiện công việc cần thiết.
- Hỗ trợ liên kết Gmail ở cấp người dùng khi được cấu hình.

### Billing và quản trị

- Subscription plans và payment transactions.
- SePay bank-transfer webhook.
- Signature verification, idempotency key và trạng thái giao dịch nhất quán.
- Admin overview, analytics, account management, payment metadata và feedback workflow.

## Framework và công nghệ

| Thành phần | Công nghệ |
| --- | --- |
| Backend runtime | .NET 10, ASP.NET Core 10 |
| Backend architecture | Clean Architecture, modular monolith |
| ORM và database access | Entity Framework Core 10, Npgsql |
| Database | PostgreSQL; production database tương thích Neon |
| API | REST, OpenAPI, RFC 9457 Problem Details |
| Realtime | ASP.NET Core SignalR |
| User/Admin Web | Angular 20, standalone components, SCSS |
| Mobile | Flutter 3.38.7, Dart 3.10.7 |
| File storage | Cloudinary gateway và local storage boundary |
| CI/CD | GitHub Actions |
| Production hosting | Render cho backend, Vercel cho web client |

## Nguyên tắc thiết kế

- Quyền truy cập được kiểm tra ở backend, không tin tưởng client.
- Dữ liệu project riêng tư không xuất hiện trong các API tổng hợp của admin.
- Thanh toán ưu tiên webhook xác thực và xử lý lặp an toàn.
- Migrations, logging, health checks và kiểm thử được quản lý cùng mã nguồn.
- Bí mật, khóa ký, connection string và file môi trường không thuộc README hoặc source control.

## Tài liệu kỹ thuật

Các tài liệu chi tiết nằm trong [`docs/docs`](docs/docs), gồm phạm vi sản phẩm, business rules, kiến trúc, database, bảo mật, thanh toán, kiểm thử và roadmap.
