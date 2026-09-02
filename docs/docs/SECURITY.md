# SECURITY

## Authentication
- JWT Access Token ngắn hạn.
- Refresh Token revoke/rotation.
- HTTPS production.
- Password dùng password hashing chuẩn.
- Google/Facebook verify server-side.

## Authorization
- Backend enforce RBAC.
- UI chỉ là presentation.
- User A không được đọc Project B bằng cách đổi ID.
- Folder rule/ownership phải enforce ở endpoint/use case.

## Rate Limit & Cost Abuse
Ưu tiên các endpoint:
- Login/Register/Refresh.
- Create Project/Task.
- Invitation.
- Upload signature/proxy.
- Search.
- Payment.
- Feedback.

Ngoài request limit cần business quota:
- Max Projects.
- Max storage.
- Max file size.
- Max invitation/hour.
- Upload/day nếu cần.

## PostgreSQL / Neon
- Không dùng superuser cho app runtime.
- Secret qua Render Environment, không commit connection string.
- Bắt buộc SSL với Neon production; local có thể dùng `SSL Mode=Prefer`.
- Dùng pooled connection string phù hợp Neon và giới hạn quyền database.
- EF/Npgsql parameterization; không concat SQL input.
- Migration phải review, chạy trong CI trên PostgreSQL disposable trước khi production.

## Cloudinary
- Secret không ở frontend.
- Signed upload/private delivery.
- Verify upload metadata.
- Check quota/size/type.

## Upload
- File size limit.
- MIME + extension.
- Normalize filename.
- Archive/zip bomb policy.
- Malware scanning có thể là phase sau.

## SignalR
- Hub authenticated.
- Join Project group sau authorization.
- Client không được tự join arbitrary group.

## Payment
- Verify webhook signature.
- Idempotent.
- Amount/Plan lấy từ server record.
- Không tin frontend success redirect.

## Admin
- Admin policy riêng.
- Admin không bypass Project access mặc định.
- Audit admin sensitive action.
- MFA có thể thêm production.

## Audit
Audit:
- Project/member/role changes.
- Deadline.
- Submission review.
- File/document versions.
- Billing/admin actions.

Không log:
- Password.
- Raw token.
- API secret.

## HTTP
- CORS allowlist.
- HTTPS.
- Request body limit.
- Timeout.
- Rate limit.
- Không leak stack trace production.
