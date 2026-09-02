# ARCHITECTURE DECISIONS

## ADR-001: .NET 10 / ASP.NET Core 10
**Accepted.** Backend type-safe, phù hợp ASP.NET Core 10, EF Core, SignalR và triển khai container trên Render.

## ADR-002: Clean Architecture + Modular Monolith
**Accepted.** Một backend deployable, module boundaries rõ. Không dùng Microservices ở MVP.

## ADR-003: PostgreSQL / Neon
**Accepted.** PostgreSQL là database runtime duy nhất của backend. Local dùng PostgreSQL; production dùng Neon PostgreSQL qua Npgsql/EF Core migrations. SQL Server chỉ còn trong tài liệu/lịch sử migration dữ liệu, không được dùng cho runtime mới.

## ADR-004: Cloudinary
**Accepted.** Binary tách khỏi SQL; SQL lưu metadata/version. Cần signed/private access và quota.

## ADR-005: JWT + Google/Facebook
**Accepted.** JWT Access/Refresh cho API; external provider map về User nội bộ.

## ADR-006: SignalR
**Accepted.** Realtime delivery; Notification vẫn persist trong SQL.

## ADR-007: GitHub URL First
**Accepted.** MVP chỉ link repository URL. Full GitHub App để phase sau.

## ADR-008: Internal Document khác Uploaded File
**Accepted.**
- Internal Document: content edit trong app, version trong SQL.
- Uploaded File: binary Cloudinary, edit bằng upload version mới.

Điều này giữ MVP khả thi nhưng vẫn đáp ứng “lưu người chỉnh sửa”.

## ADR-009: Automatic payment reconciliation
**Accepted.** Chỉ MoMo IPN và SePay bank-transfer webhook được phép kích hoạt subscription. Backend xác minh chữ ký, provider order/reference, amount và trạng thái; callback/request lặp lại phải idempotent. Redirect của trình duyệt không được xem là bằng chứng thanh toán.

## ADR-010: API-hosted background services
**Accepted.** Deadline expiration, email delivery và notification retention chạy trong `Planora.Api/BackgroundServices`. Chỉ tách worker riêng khi cần scale hoặc triển khai độc lập; hiện Render chỉ deploy một API web service.
