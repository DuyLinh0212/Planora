# TESTING

## Domain Tests
Ưu tiên:
- Task expiration.
- Member extension => late.
- Leader direct extension => not late.
- Task DONE chỉ sau approved submission.
- Permission resolution.
- Score.
- Plan quota.

## Application Tests
- Create Project.
- Invite/Accept Member.
- Assign Task.
- Submit/Review.
- Extension.
- Folder access.
- File version.
- Upgrade plan.

## Integration Tests
- Chạy trên PostgreSQL 17 disposable trong GitHub Actions.
- CI áp dụng toàn bộ PostgreSQL EF migrations trước khi test.
- Integration test chạy tuần tự để tránh dữ liệu dùng chung bị tranh chấp.
- Unique constraints, transactions, authorization và API errors.

## External Service
Unit test không gọi Cloudinary/SePay thật.
Dùng abstraction/fake. Sandbox test riêng.

## Security Tests
- Cross-project ID access.
- Member edit Task without permission.
- Folder deny.
- Admin project-content denial.
- Revoked/expired token.
- Rate limit.

## Payment Tests
- Invalid signature.
- Duplicate webhook.
- Wrong amount.
- Retry.
