# WORKFLOWS

## 1. Auth

```mermaid
flowchart TD
  A[Guest] --> B{Email hay Social}
  B -->|Email| C[Validate Credentials]
  B -->|Google/Facebook| D[Verify Provider]
  D --> E[Map/Create User]
  C --> F{Valid?}
  F -->|No| G[Reject]
  F -->|Yes| H[Issue JWT + Refresh Token]
  E --> H
```

## 2. Create Project

```mermaid
flowchart TD
  A[User] --> B[Create Project]
  B --> C[Check Plan Quota]
  C -->|Exceeded| D[Reject / Upgrade]
  C -->|OK| E[Create Project]
  E --> F[Create Owner Membership]
  F --> G[Create Default Roles]
  G --> H[Create Root Folder]
  H --> I[Audit]
```

## 3. Invite Member

```mermaid
flowchart TD
  A[Owner/Leader] --> B[Create Invitation]
  B --> C[Notify Invitee]
  C --> D{Accept?}
  D -->|No| E[Rejected/Expired]
  D -->|Yes| F[Create ProjectMember]
  F --> G[Assign Role]
```

## 4. Sprint

```mermaid
flowchart LR
  A[Backlog] --> B[Create Sprint]
  B --> C[Plan Tasks]
  C --> D[Start Sprint]
  D --> E[Execute]
  E --> F[Review]
  F --> G[Close]
  G --> H[Move incomplete tasks]
```

## 5. Task lifecycle

```mermaid
stateDiagram-v2
  [*] --> TODO
  TODO --> IN_PROGRESS: Start
  IN_PROGRESS --> SUBMITTED: Submit
  SUBMITTED --> DONE: Approve
  SUBMITTED --> REWORK: Reject
  REWORK --> IN_PROGRESS
  TODO --> EXPIRED: Deadline
  IN_PROGRESS --> EXPIRED: Deadline
  EXPIRED --> IN_PROGRESS: Extension approved
  TODO --> CANCELLED
  IN_PROGRESS --> CANCELLED
```

## 6. Auto-expire

```mermaid
flowchart TD
  A[Deadline Worker] --> B[Find task past EffectiveDueAt]
  B --> C{Valid submission before due?}
  C -->|Yes| D[Keep SUBMITTED]
  C -->|No| E[Set EXPIRED]
  E --> F[Analytics event]
  F --> G[Notify]
```

## 7. Member Request Extension

```mermaid
flowchart TD
  A[Member] --> B[Request New Due + Reason]
  B --> C[Leader Review]
  C -->|Reject| D[Rejected]
  C -->|Approve| E[DeadlineChange]
  E --> F[CountsAsLate = true]
  F --> G[Update EffectiveDueAt]
  G --> H[Reopen if EXPIRED]
```

## 8. Leader Direct Extension

```mermaid
flowchart TD
  A[Leader] --> B[Enter New Due + Reason]
  B --> C[DeadlineChange]
  C --> D[CountsAsLate = false]
  D --> E[Update Due]
  E --> F[Audit + Notify]
```

## 9. Submit Task Result

```mermaid
flowchart TD
  A[Assignee] --> B[Prepare Result]
  B --> C{Type}
  C -->|Text| D[Description]
  C -->|URL| E[Link]
  C -->|Existing File| F[Select FileVersion]
  C -->|New File| G[Upload to Project Folder]
  D --> H[Create Submission]
  E --> H
  F --> H
  G --> H
  H --> I[Record SubmittedAt]
  I --> J[Task SUBMITTED]
  J --> K[Notify Reviewer]
```

## 10. Review

```mermaid
flowchart TD
  A[Reviewer] --> B[Open Submission]
  B --> C{Decision}
  C -->|Approve| D[Submission APPROVED]
  D --> E[Task DONE]
  E --> F[Update Metrics]
  C -->|Rework| G[Feedback]
  G --> H[Task REWORK]
```

## 11. Storage Access

```mermaid
flowchart TD
  A[Member] --> B[Open Folder]
  B --> C[Resolve RBAC + Folder Override]
  C -->|Denied| D[403]
  C -->|Allowed| E{Action}
  E -->|Upload| F[Check quota + type + permission]
  E -->|Edit| G[Check ownership/edit permission]
  E -->|Delete| H[Check delete permission]
  F --> I[Cloudinary upload + FileVersion]
  G --> J[New Document/File Version]
  H --> K[Soft Delete]
  I --> L[Audit]
  J --> L
  K --> L
```

## 12. Folder Permission

```mermaid
flowchart TD
  A[Authorized Leader/Owner] --> B[Select Folder]
  B --> C[Select Role or Member]
  C --> D[View/Create/Upload/Edit/Delete]
  D --> E[Save Access Rule]
  E --> F[Audit]
```

## 13. Analytics

```mermaid
flowchart TD
  A[Task/Submission/Deadline Events] --> B[Raw Truth]
  B --> C[Aggregation]
  C --> D[Member Metrics]
  C --> E[Project Metrics]
  C --> F[Sprint Metrics]
  D --> G[Charts/Score]
  E --> G
  F --> G
```

## 14. Payment

```mermaid
flowchart TD
  A[User chooses Plan] --> B[Backend creates Transaction]
  B --> C[Chuyển khoản ngân hàng qua SePay]
  C --> D[Provider Webhook]
  D --> E[Verify Signature]
  E --> F{Valid + Success?}
  F -->|No| G[Pending/Failed]
  F -->|Yes| H[Idempotency Check]
  H --> I[Activate/Extend Subscription]
```

## 15. GitHub link

```mermaid
flowchart LR
  A[Project Settings] --> B[Add Repository URL]
  B --> C[Validate URL]
  C --> D[Save External Link]
```

## 16. Admin

```mermaid
flowchart TD
  A[Admin] --> B[Admin API]
  B --> C[Accounts]
  B --> D[Plans]
  B --> E[Payments]
  B --> F[Feedback]
  B --> G[Aggregate Stats]
  B -. not normal access .-> H[Private Project Content]
```
