# PAYMENT AND SUBSCRIPTION

## Plan
Plan chứa:
- Price.
- Billing period.
- Max owned projects.
- Max storage.
- Feature flags/entitlements khi cần.

Không hard-code quota ở nhiều chỗ.

## Providers
- Chuyển khoản ngân hàng tự động qua SePay webhook.

Abstraction gợi ý:

```csharp
IPaymentGateway
- CreatePaymentAsync(...)
- VerifyWebhookAsync(...)
- QueryTransactionAsync(...)
```

Infrastructure:
- `BankTransferPaymentDetailsProvider`

## Flow
1. User chọn Plan.
2. Backend đọc Plan/Price từ DB.
3. Tạo PaymentTransaction PENDING.
4. Backend trả về số tài khoản và mã nội dung duy nhất.
5. User chuyển khoản.
6. SePay gọi webhook tới Backend.
7. Verify API key, tài khoản nhận, amount và mã nội dung.
8. Idempotency.
9. Mark SUCCESS.
10. Activate/extend subscription.

### Chuyển khoản ngân hàng

- Client nhận số tài khoản và mã nội dung chuyển khoản duy nhất từ backend.
- QR được tạo động bằng VietQR Quick Link, gồm số tiền và mã nội dung của đúng giao dịch. Cấu hình `Payment:BankTransfer:VietQrBankId` bằng mã BIN VietQR của ngân hàng nhận (ví dụ ACB: `970416`). Nếu chưa cấu hình, hệ thống dùng `BankName` làm bank ID.
- Mã nội dung mới có tối đa 25 ký tự để tương thích giới hạn VietQR.
- SePay gọi `POST /api/payments/bank-transfer/sepay/ipn` khi tài khoản báo có.
- Backend chỉ xử lý tiền vào, đúng tài khoản nhận, đúng amount và đúng mã nội dung. Header `Authorization: Apikey …` phải khớp secret đã cấu hình.
- Không có bước admin duyệt để kích hoạt gói; endpoint admin review chỉ phục vụ đối soát/audit.

### Idempotency và mạng chập chờn

- Client giữ cùng `idempotencyKey` trong `sessionStorage` khi retry một lần thanh toán bị mất kết nối.
- `ProviderOrderId`, `ProviderTransactionId` và `UserSubscription.PaymentTransactionId` đều có unique index. Do đó callback retry từ provider hoặc request retry từ browser không thể tạo hai subscription cho cùng một giao dịch.
- Mã nội dung chuyển khoản được lưu server-side, nên retry cùng idempotency key trả lại đúng giao dịch cũ thay vì tạo đơn mới.

## Không làm
- Tin amount từ frontend.
- Upgrade chỉ vì redirect success.
- Process duplicate webhook.
- Expose payment secret.

## Expiration
Không xóa Project/File ngay.
- Block create mới nếu vượt quota.
- Có thể giữ read-only/grace period.
