# STORAGE DESIGN

## 1. Hai loại nội dung

### Project File
Binary: PDF, DOCX, XLSX, ZIP, image, drawio...
- Binary trên Cloudinary.
- DB lưu metadata/version.
- “Edit” file binary = upload version mới.

### Project Document
Tài liệu soạn trực tiếp trong app.
- Content Markdown hoặc Rich Text JSON trong SQL.
- Mỗi lần save tạo DocumentVersion.
- Lưu EditedBy.

Nhờ vậy đáp ứng yêu cầu Leader chỉnh tài liệu mà không phải xây Word/Google Docs editor.

## 2. Folder tree

```text
Project Root
├── Requirements
│   ├── SRS.pdf
│   └── Requirement Notes [Document]
├── Design
│   └── ERD.drawio
└── Deliverables
```

`ParentFolderId` tạo cây thư mục.

## 3. Cloudinary convention

Logical public id:
`projects/{projectId}/files/{fileId}/v{versionNumber}`

Không dùng original filename làm identifier duy nhất.

## 4. Version

```text
ERD.drawio
├── v1 - Member A
├── v2 - Member A
└── v3 - Leader
```

Task Submission attach `FileVersionId`, không attach chỉ `FileId`.

## 5. Permission

Các quyền:
- View.
- Create subfolder.
- Upload.
- Edit.
- Delete.

Resolution:
`Project Role -> Folder Override -> Ownership`.

Member mặc định:
- Edit tài liệu/file của mình.
- Không edit tài liệu người khác trừ khi được grant.

## 6. Delete

Ưu tiên soft delete metadata.
Nếu FileVersion đã gắn Submission thì không xóa physical artifact ngay.

Có thể có retention rồi background cleanup.

## 7. Quota

Trước upload:
- Permission.
- Plan quota.
- Project/User storage usage.
- Max file size.
- MIME/extension policy.

## 8. Security

- Cloudinary secret chỉ ở Backend.
- Signed/private resource cho Project private.
- Không lưu permanent public URL nếu resource cần authorization.
- Validate upload.
- Audit upload/edit/move/delete/permission.
