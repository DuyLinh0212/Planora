# DEPLOYMENT

```text
User Web (Vercel) ─────┐
Admin Web (Vercel) ────┼──> Backend API (Render)
Mobile ────────────────┘          │
                                  ├── Neon PostgreSQL
                                  ├── Cloudinary
                                  ├── SignalR
                                  ├── Google/Facebook
                                  └── MoMo + SePay
```

## Environments
- Development.
- Staging khi cần (có thể dùng Render/Vercel preview).
- Production.

## Backend
- HTTPS.
- Health check.
- Structured logs.
- CORS allowlist.
- Secrets qua environment.

### Render

Repository có sẵn `render.yaml` và `backend/Dockerfile`. Tạo Blueprint từ repository root, điền các biến có `sync: false`, rồi đặt health check là `/health/live` (đã khai báo trong Blueprint). API tự bind `0.0.0.0:$PORT`, xử lý `X-Forwarded-Proto` của Render trước HTTPS redirect và chỉ chạy migration khi `Database__ApplyMigrationsOnStartup=true`.

`ConnectionStrings__DefaultConnection` chấp nhận trực tiếp Neon URI dạng `postgresql://...` hoặc chuỗi Npgsql dạng `Host=...`; luôn lưu biến này dưới dạng secret.

Với payment webhook, dùng web service luôn hoạt động; không dùng instance sleep được vì MoMo/SePay có thể gửi callback lúc service đang ngủ. Sau deploy, cập nhật hai callback public HTTPS:

- `Payment__Momo__IpnUrl=https://YOUR-API.onrender.com/api/payments/momo/ipn`
- SePay webhook URL: `https://YOUR-API.onrender.com/api/payments/bank-transfer/sepay/ipn`

## Background services
Không phụ thuộc request user để auto-expire. Các service hiện chạy trong API:

- `OverdueTaskExpirationService`: tự chuyển task quá hạn.
- `TaskEmailDeliveryService`: xử lý queue email.
- `NotificationRetentionService`: dọn notification hết hạn.

Render hiện chỉ deploy một API web service. Chỉ tách worker/scheduler riêng khi cần scale hoặc khi tier hosting không đảm bảo background execution.

## Neon PostgreSQL
- Dùng connection string pooled của Neon và bắt buộc SSL.
- Least privilege.
- Không commit connection string; cấu hình `ConnectionStrings__DefaultConnection` dưới dạng Render secret.
- Bật backup/PITR phù hợp với gói Neon.
- Migration có kiểm soát.

`Database__ApplyMigrationsOnStartup=true` chỉ nên bật cho một Render instance trong lúc deploy để tránh nhiều instance cùng chạy migration.

## Vercel
Không đưa DB/Cloudinary/Payment secret vào client/public env.

## SignalR
1 backend instance: SignalR trực tiếp.
Khi scale nhiều instance mới xem xét backplane/managed SignalR.

## CI/CD
PR/push:
`policy -> restore -> build -> PostgreSQL migration -> test -> web/mobile build -> Docker build`

Main sau khi CI thành công:
`deploy exact SHA to Render -> wait live -> health/ready -> deploy User/Admin to Vercel`

Chi tiết secrets, environment protection và release Android nằm trong `docs/docs/CI_CD.md`.

Destructive migration phải review.
