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
- MoMo.
- Chuyển khoản ngân hàng tự động qua SePay webhook.

Abstraction gợi ý:

```csharp
IPaymentGateway
- CreatePaymentAsync(...)
- VerifyWebhookAsync(...)
- QueryTransactionAsync(...)
```

Infrastructure:
- `MomoPaymentGateway`
- `BankTransferPaymentDetailsProvider`

## Flow
1. User chọn Plan.
2. Backend đọc Plan/Price từ DB.
3. Tạo PaymentTransaction PENDING.
4. Tạo request provider.
5. Provider nhận payment.
6. Webhook tới Backend.
7. Verify signature.
8. Idempotency.
9. Mark SUCCESS.
10. Activate/extend subscription.

### MoMo

- Backend tạo `PaymentTransaction` và một `orderId` duy nhất, rồi ký request HMAC-SHA256 với `SecretKey`.
- Client chỉ nhận `payUrl`; kết quả redirect không được dùng để kích hoạt gói.
- MoMo gọi `POST /api/payments/momo/ipn`; backend kiểm chữ ký, đối chiếu `orderId` và chính xác amount trước khi kích hoạt.

### Chuyển khoản ngân hàng

- Client nhận số tài khoản và mã nội dung chuyển khoản duy nhất từ backend.
- SePay gọi `POST /api/payments/bank-transfer/sepay/ipn` khi tài khoản báo có.
- Backend chỉ xử lý tiền vào, đúng tài khoản nhận, đúng amount và đúng mã nội dung. Header `Authorization: Apikey …` phải khớp secret đã cấu hình.
- Không có bước admin duyệt để kích hoạt gói; endpoint admin review chỉ phục vụ đối soát/audit.

### Idempotency và mạng chập chờn

- Client giữ cùng `idempotencyKey` trong `sessionStorage` khi retry một lần thanh toán bị mất kết nối.
- `ProviderOrderId`, `ProviderTransactionId` và `UserSubscription.PaymentTransactionId` đều có unique index. Do đó callback retry từ provider hoặc request retry từ browser không thể tạo hai subscription cho cùng một giao dịch.
- Link MoMo được lưu server-side sau khi tạo, nên retry cùng idempotency key trả lại đúng checkout cũ thay vì tạo đơn mới.

## Không làm
- Tin amount từ frontend.
- Upgrade chỉ vì redirect success.
- Process duplicate webhook.
- Expose payment secret.

## Expiration
Không xóa Project/File ngay.
- Block create mới nếu vượt quota.
- Có thể giữ read-only/grace period.
