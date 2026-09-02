# API DESIGN

## Base
`/api`

Không bắt buộc `/v1` ở MVP. Chỉ đưa versioning khi có nhu cầu breaking change thực tế.

## Resource naming

```text
GET    /api/projects
POST   /api/projects
GET    /api/projects/{projectId}
PATCH  /api/projects/{projectId}
DELETE /api/projects/{projectId}

GET    /api/projects/{projectId}/members
POST   /api/projects/{projectId}/invitations
GET    /api/projects/{projectId}/sprints
GET    /api/projects/{projectId}/tasks
GET    /api/projects/{projectId}/folders
```

Không dùng `/getAllTasks`, `/createProject`.

## Business actions

```text
POST /api/project-invitations/{id}/accept
POST /api/project-invitations/{id}/reject
POST /api/tasks/{id}/start
POST /api/tasks/{id}/submit
POST /api/tasks/{id}/extension-requests
POST /api/extension-requests/{id}/approve
POST /api/extension-requests/{id}/reject
POST /api/tasks/{id}/extend-deadline
POST /api/submissions/{id}/approve
POST /api/submissions/{id}/request-rework
POST /api/sprints/{id}/start
POST /api/sprints/{id}/close
```

## Auth endpoints

```text
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
POST /api/auth/password/forgot
POST /api/auth/password/reset
POST /api/auth/password/change
POST /api/auth/external/google
POST /api/auth/external/facebook
```

## Error format

Ưu tiên Problem Details-compatible:

```json
{
  "type": "validation_error",
  "title": "Request validation failed",
  "status": 400,
  "errors": {
    "name": ["Name is required."]
  },
  "traceId": "..."
}
```

## Status codes
- 200 Read/Update/Action.
- 201 Create.
- 204 Delete/no body.
- 400 Invalid request.
- 401 Unauthenticated.
- 403 No permission.
- 404 Not found/not exposed.
- 409 Conflict.
- 429 Rate limited.

## Pagination
`?page=1&pageSize=20`, có server max.

## Authorization
Mọi Project resource:
1. Authenticate.
2. Resolve Project.
3. Check membership.
4. Check permission.
5. Apply Folder/Resource override.
6. Apply ownership rule.

## Upload
Không gửi Cloudinary secret cho client.
- API authorize + check quota.
- Tạo signed upload params hoặc backend proxy có giới hạn.
- Save metadata sau khi upload thành công.

## Idempotency
Bắt buộc cân nhắc cho payment/webhook/invitation approval/submission approval.

## OpenAPI
OpenAPI generated từ source là tài liệu endpoint thực tế; file này chỉ là convention.
