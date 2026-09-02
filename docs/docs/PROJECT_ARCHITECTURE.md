# PROJECT ARCHITECTURE

## 1. Mục tiêu

Ưu tiên:
1. Gọn.
2. Dễ đọc.
3. Dễ sửa.
4. Dễ bảo trì.
5. Dễ mở rộng.
6. Dễ test.

Chọn **Clean Architecture + Modular Monolith**. Không Microservices ở MVP.

## 2. Modules

```text
Identity
Projects
Agile
Tasks
Storage
Notifications
Analytics
Billing
Integrations
Feedback
Administration
```

## 3. Dependency Rule

```text
Api ------------> Application ------------> Domain
Infrastructure -> Application ------------> Domain

Domain -> không phụ thuộc ASP.NET/EF/Cloudinary/SignalR/Payment SDK
```

## 4. Solution Structure

```text
backend/
├── Planora.slnx
├── src/
│   ├── Planora.Domain/
│   │   ├── Users/
│   │   ├── Projects/
│   │   ├── Sprints/
│   │   ├── Tasks/
│   │   ├── Storage/
│   │   └── Common/
│   ├── Planora.Application/
│   │   ├── Authentication/
│   │   ├── Authorization/
│   │   ├── Projects/
│   │   ├── ProjectMembers/
│   │   ├── Sprints/
│   │   ├── Tasks/
│   │   ├── TaskSubmissions/
│   │   ├── TaskDeadlines/
│   │   ├── Storage/
│   │   └── Common/
│   ├── Planora.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Authentication/
│   │   ├── ExternalAuth/
│   │   └── Storage/
│   └── Planora.Api/
│       ├── Controllers/
│       ├── Middleware/
│       ├── Authorization/
│       ├── Extensions/
│       └── BackgroundServices/
└── tests/
    ├── Planora.UnitTests/
    └── Planora.IntegrationTests/

frontend/
├── Planora.Web.User/
└── Planora.Web.Admin/

mobile/
└── user-app/
```

## 5. Tổ chức Application theo nghiệp vụ

Không gom nhiều nghiệp vụ vào một lớp `UseCases` lớn và không dùng tên hàm mơ hồ.

```text
Application/
├── Projects/
│   ├── ProjectService.cs
│   └── ProjectContracts.cs
├── Tasks/
│   ├── TaskService.cs
│   └── TaskContracts.cs
├── TaskSubmissions/
│   ├── TaskSubmissionService.cs
│   └── TaskSubmissionContracts.cs
└── TaskDeadlines/
    ├── TaskDeadlineService.cs
    └── TaskDeadlineContracts.cs
```

Method phải gọi rõ nghiệp vụ, ví dụ `GetProjectsAsync`, `GetProjectByIdAsync`, `CreateProjectTaskAsync` và `ApproveTaskSubmissionAsync`.

## 6. Request Flow

```text
HTTP
 ↓
Controller
 ↓
Authentication
 ↓
Authorization
 ↓
Application Service
 ↓
Domain Rules
 ↓
Infrastructure Interface
 ↓
SQL / Cloudinary / Provider
```

## 7. Background Services

Scheduled job chạy trong `Planora.Api/BackgroundServices` và chỉ gọi Application Service:
- Expire Task.
- Analytics aggregation.
- Subscription expiration.
- Scheduled notifications.
- Deferred file cleanup.

Business rule vẫn ở Application/Domain. Chỉ tách thành worker project riêng khi có nhu cầu triển khai hoặc scale độc lập.

## 8. Side effects

Có thể dùng Domain/Application Events cho:
- TaskAssigned -> Notification.
- TaskExpired -> Notification + Analytics.
- SubmissionApproved -> Metrics + Notification.

Không cần event bus/messaging ở MVP.

## 9. API

- REST.
- Base `/api`.
- OpenAPI generated.
- Không bắt buộc `/v1` ở MVP.
- Không trả EF Entity trực tiếp.

## 10. EF Core

- DbContext ở Infrastructure.
- Entity mapping bằng Fluent API.
- Migrations trong Infrastructure.
- Không Generic Repository “cho có”.
- Application có abstraction khi cần cô lập persistence/external service.

## 11. Nguyên tắc giữ code gọn

- Controller/Endpoint không chứa business logic.
- Không duplicate permission logic.
- Không gọi Cloudinary từ Domain.
- Không gọi payment SDK từ Application trực tiếp.
- Feature mới phải nằm đúng module.
- Không tạo abstraction nếu chỉ có một use-case nhỏ và không mang giá trị test/thay thế.
