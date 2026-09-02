# Team workflow E2E

`team_workflows.py` chạy trên Chromium headless bằng Playwright, dùng Angular UI thật,
API thật và SQL Server thật. Suite tạo dữ liệu riêng theo timestamp, không dùng fixture
có sẵn và chụp ảnh sau mỗi trạng thái quan trọng.

## Phạm vi

- Kịch bản 1: tạo ba tài khoản, leader đăng nhập, tạo project, mời hai thành viên,
  tạo sprint, giao hai task, kiểm tra thông báo, upload TXT/PNG, nộp/duyệt và kiểm tra
  analytics.
- Kịch bản 2: tạo task chung, leader sửa nội dung, kiểm tra audit history và thông báo
  của cả hai thành viên, sau đó kiểm tra quy tắc hai assignee cùng nộp và tự hoàn tất.
- Kịch bản 3: tạo hai task có deadline chính xác tới giờ và đã quá hạn, chờ deadline
  worker đánh dấu `Expired`, hai thành viên yêu cầu gia hạn, leader duyệt, kiểm tra
  `MemberRequestApproved` và `countsAsLate = true`, rồi nộp bài.
- Kịch bản 4: leader tự dời deadline, kiểm tra `LeaderDirect`,
  `countsAsLate = false` và không có extension request.
- Kịch bản 5: leader mở folder, sửa tài liệu và kiểm tra version history lưu đúng
  nội dung, thời gian, user chỉnh sửa; hai member được xem lịch sử nhưng mọi lần lưu
  thay đổi đều bị API từ chối.
- Kịch bản 6: leader cấp rule riêng theo member. Member 1 được cấp đầy đủ
  `view/create/upload/edit/delete` và thực thi thành công; member 2 chỉ có `view`, các
  thao tác `create/upload/edit/delete` phải bị từ chối.

## Chạy test

Chuẩn bị database/migration và chạy API ở `http://127.0.0.1:5273`:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project backend\src\Planora.Infrastructure --startup-project backend\src\Planora.Api
$env:DeadlineWorker__IntervalSeconds = '10'
dotnet run --project backend\src\Planora.Api
```

Ở terminal thứ hai, chạy web:

```powershell
Set-Location frontend\Planora.Web.User
npm start -- --host 127.0.0.1 --port 4200
```

Ở terminal thứ ba, chạy Playwright:

```powershell
py -3 tests\e2e\team_workflows.py
```

Biến môi trường tùy chọn:

- `PLANORA_TEST_BASE_URL`: URL Web.User.
- `PLANORA_TEST_API_URL`: URL API.
- `PLANORA_TEAM_E2E_PASSWORD`: mật khẩu test.
- `PLANORA_TEAM_E2E_ARTIFACTS`: thư mục ảnh/report.

Mỗi lần chạy sẽ xóa ảnh cũ trong đúng thư mục artifact của suite rồi tạo lại
`01-...png` đến `20-...png` và `report.json`. Suite tiếp tục chạy các assertion độc lập
để thu đủ bằng chứng, sau đó trả exit code khác 0 nếu còn contract nghiệp vụ bị lỗi.

## Giới hạn hiện được phát hiện

Backend hiện chuyển task sang `Submitted` ngay khi assignee đầu tiên nộp. Vì vậy
assignee thứ hai nhận `409 task.cannot_submit`, và task chung chưa thể tự hoàn tất sau
khi tất cả assignee đã nộp. Hai assertion cuối của kịch bản 2 cố ý giữ đỏ cho tới khi
quy tắc multi-assignee được thống nhất và triển khai.
