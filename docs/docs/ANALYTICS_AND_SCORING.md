# ANALYTICS AND SCORING

## Raw Metrics
- Assigned tasks.
- Completed tasks.
- On-time.
- Early.
- Late/Expired.
- Extension requests.
- Leader direct extensions.
- Rework.
- Approved submissions.
- Average completion time.

## Deadline logic
- Submit trước/equal EffectiveDueAt: on-time.
- Reviewer xử lý sau hạn không biến thành late.
- Member-request extension được approve: late.
- Leader direct extension: không late theo rule hiện tại.

## Default Score đề xuất

Không hard-code vĩnh viễn; cho cấu hình sau.

```text
Completion Rate     30%
On-time Rate        35%
Quality/Approval    25%
Early Contribution  10%
```

Hiển thị breakdown, không chỉ một số duy nhất.

## Charts
### Project
- Task status.
- Completion timeline.
- Overdue trend.
- Sprint burndown.
- Member contribution.
- On-time rate.
- Storage usage.

### Admin
- Total users.
- Active users.
- Projects.
- Active/completed projects.
- Subscription distribution.
- Payment statistics.
- Aggregate storage.

Admin analytics dùng aggregate metadata, không đọc Project content.
