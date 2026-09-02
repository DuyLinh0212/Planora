# BUSINESS RULES

## Project
- **BR-PRJ-001:** Mỗi Project có đúng 1 Owner.
- **BR-PRJ-002:** Chỉ User còn quota mới tạo Project.
- **BR-PRJ-003:** Status: `PLANNING`, `ACTIVE`, `PAUSED`, `COMPLETED`, `CANCELLED`.
- **BR-PRJ-004:** Completed không xóa dữ liệu.
- **BR-PRJ-005:** Thay đổi quan trọng phải Audit.

## Invitation / Member
- **BR-MEM-001:** Invitee phải Accept mới thành ProjectMember.
- **BR-MEM-002:** Invitation có expiration.
- **BR-MEM-003:** `(ProjectId, UserId)` membership active không được trùng.
- **BR-MEM-004:** Remove member không xóa lịch sử công việc.
- **BR-MEM-005:** Owner phải transfer ownership trước khi rời Project.

## Sprint
- **BR-SPR-001:** Sprint thuộc Project.
- **BR-SPR-002:** StartAt < EndAt.
- **BR-SPR-003:** MVP khuyến nghị chỉ 1 Sprint ACTIVE tại một Project.
- **BR-SPR-004:** Close Sprint không tự Done các Task chưa hoàn thành.

## Task
- **BR-TSK-001:** Task thuộc Project; có thể ở Backlog hoặc Sprint.
- **BR-TSK-002:** Member được assign không được tự sửa title/description/criteria/priority/deadline nếu thiếu permission.
- **BR-TSK-003:** Status: `TODO`, `IN_PROGRESS`, `SUBMITTED`, `REWORK`, `DONE`, `EXPIRED`, `CANCELLED`.
- **BR-TSK-004:** Task chỉ `DONE` khi Submission được approve.
- **BR-TSK-005:** Quá EffectiveDueAt và chưa có Submission hợp lệ trước hạn → `EXPIRED`.
- **BR-TSK-006:** Submit trước hạn nhưng review sau hạn vẫn tính đúng hạn.
- **BR-TSK-007:** Mọi đổi deadline phải có history.

## Extension
- **BR-EXT-001:** Member có thể tạo Extension Request.
- **BR-EXT-002:** Request gồm new due time + reason.
- **BR-EXT-003:** Leader/Owner approve/reject.
- **BR-EXT-004:** Member-request extension được duyệt → `CountsAsLate = true`.
- **BR-EXT-005:** Leader chủ động extend không dựa trên request → `CountsAsLate = false`.
- **BR-EXT-006:** EXPIRED Task có thể reopen sau khi extension được approve.
- **BR-EXT-007:** Không overwrite deadline history.

## Submission
- **BR-SUB-001:** Một Task có nhiều attempt.
- **BR-SUB-002:** Lưu `SubmittedAt`.
- **BR-SUB-003:** Result có thể là text, URL, Project FileVersion, GitHub URL hoặc kết hợp.
- **BR-SUB-004:** Reviewer Approve hoặc Require Rework.
- **BR-SUB-005:** Rework phải có feedback.
- **BR-SUB-006:** Attachment tham chiếu chính xác `FileVersion`.

## Storage
- **BR-STO-001:** Mỗi Project có root storage.
- **BR-STO-002:** Folder cây bằng ParentFolderId.
- **BR-STO-003:** Binary ở Cloudinary; metadata ở SQL.
- **BR-STO-004:** Version mới không xóa version cũ.
- **BR-STO-005:** Member mặc định chỉ edit file/document mình sở hữu.
- **BR-STO-006:** Role/Folder rule có thể grant quyền edit tài liệu người khác.
- **BR-STO-007:** Internal Document có version + editor history.
- **BR-STO-008:** Delete ưu tiên soft delete.

## RBAC
- **BR-RBAC-001:** Quyền luôn enforce ở Backend.
- **BR-RBAC-002:** Project có default/custom role.
- **BR-RBAC-003:** Folder/resource có override theo Role hoặc Member.
- **BR-RBAC-004:** Change permission phải Audit.

## Analytics
- **BR-ANA-001:** Score dựa trên raw data, có breakdown.
- **BR-ANA-002:** Leader direct extension `CountsAsLate=false` không phạt member.
- **BR-ANA-003:** Submit đúng hạn không bị late do reviewer chậm.
- **BR-ANA-004:** Score chỉ là chỉ số nội bộ Project, không phải kết luận năng lực tuyệt đối.

## Billing
- **BR-BIL-001:** Plan quyết định quota.
- **BR-BIL-002:** Chỉ activate subscription sau server-side payment verification.
- **BR-BIL-003:** Webhook idempotent.
- **BR-BIL-004:** Hết gói không xóa Project ngay.

## Admin
- **BR-ADM-001:** Admin không tự động là ProjectMember.
- **BR-ADM-002:** Admin dashboard dùng aggregate metadata.
- **BR-ADM-003:** Admin quản lý account/plan/payment/feedback, không sửa Project content.
