# Planora Feature Rebuild State

## Session intent

Rebuild Planora against `docs/docs/FEATURE_DESCRIPTION.MD` across the .NET API, Web.User, Web.Admin, and the existing mobile workspace. The result must expose real business workflows rather than presentation-only preview screens.

## Source of truth

- Product requirements: `docs/docs/FEATURE_DESCRIPTION.MD`
- Existing architecture constraints: `docs/implementation/FULLSTACK_IMPLEMENTATION_STATE.md`
- Existing API/domain code under `backend/src`
- Existing Angular applications under `frontend/Planora.Web.User` and `frontend/Planora.Web.Admin`
- Existing Flutter application under `mobile/user-app`

## Non-negotiable constraints

- Preserve Clean Architecture dependency direction.
- Use explicit verb-resource service and endpoint names.
- Return actionable Problem Details errors with stable codes and field names.
- Keep administrator access outside private project/task/file content.
- Enforce quota against the project owner, including uploads performed by collaborators.
- Require explicit confirmation for destructive or state-changing UI actions.
- Keep all mutable facts timestamped in UTC; present them in the user's configured locale/time zone.
- Keep semantic memory out of the product until an AI feature requires it. Use the database for product state and this file as the durable implementation handoff.

## Gap map

| Capability | Baseline | Rebuild target |
| --- | --- | --- |
| Authentication | Email/password and external login | Username or email login, Gmail registration rule, accepted terms, password strength/special-character validation, 30-day remembered session, reset countdown |
| Profile | Display name in session only | Avatar, display name, preferences, project counts, quota, subscription/payment history, cancellation, feedback/refund entry |
| Projects | CRUD and default roles | Owner-plan quota, explicit lifecycle, confirmations, complete audit trail |
| Members | Invite/accept/reject/role/remove | User lookup, invitation status, member quota, kick reason, notification |
| Sprints | Create/start/close | Automatic `Sprint N`, project date bounds, task date bounds, edit/cancel |
| Tasks | Create/list/start/assign/submit/review | Markdown, task type, submission policy, edit/delete, dependency/milestone metadata, complete mutation history |
| Storage | Browse/create/upload/version/permission | Owner quota, rename/delete/restore policy, preview/download metadata, document-task provenance |
| Project views | Overview/Kanban/sprint/basic analytics | List, Kanban, sprint, backlog, calendar, roadmap, Gantt, workload, analytics, activity, milestone, dependency |
| Billing | Admin-side models only | Free/Pro/VIP quota matrix, MoMo/bank transfer workflow, user subscription view/cancel, payment history |
| Support | Admin feedback only | User feedback/refund request and realtime-style conversation lifecycle with soft close |
| Administration | Accounts/plans/payments/feedback/analytics | Restore log, maintenance switch, richer system/security/storage/subscription metrics |
| Preferences | Device theme only | Light/dark/calm themes, Vietnamese/English, terms and independent illustrated guide |
| Notifications | Missing | Persisted in-app notifications with realtime-compatible contract and email boundary |

## Experience direction

### Subject and audience

Planora is a planning room for Vietnamese student teams, freelancers, and small product teams. Its single job is to turn commitments into verified outcomes without hiding deadlines, ownership, or review state.

### Visual tokens

- Light: Paper `#F7F8FC`, Ink `#172033`, Cobalt `#3157D5`, Tide `#1E9B83`, Coral `#D65D50`, Amber `#B7791F`.
- Dark: Midnight `#08111F`, Panel `#111C30`, Fog `#D8E1EF`, Periwinkle `#7890FF`, Mint `#43D0AC`, Coral `#F07B6E`.
- Calm: Sea mist `#EAF5FF`, Deep sea `#123A5A`, Wave `#2F82C4`, Foam `#F9FCFF`, Lagoon `#2A9D8F`, Sunset `#E47768`.
- Display typography: Manrope Variable for Vietnamese-safe headings; Georgia is retained for the Planora wordmark. Instrument Serif was removed from headings after visual QA exposed unstable Vietnamese combining marks in Chromium.
- Body typography: Manrope Variable.
- Data/utility typography: IBM Plex Mono.

### Layout and signature

Use a stable workspace rail and a wide project canvas. The memorable element is the **commitment horizon**: one horizontal spine joining sprint window, active work, review, and verified outcome. Task view modes reuse that time/status spine so switching between board, calendar, roadmap, and Gantt retains orientation. Admin uses one restrained trust-boundary band to make aggregate-only access explicit.

### Self-critique before build

The existing design is clean but reads as a static dashboard because cards dominate and important state changes are scattered. The rebuild keeps the typography but removes decorative metrics, promotes the commitment horizon to the page thesis, adds explicit action feedback, and makes empty/error/maintenance states actionable. KPI grids remain only where comparison is the actual job (admin analytics).

## Tool/API contract policy

- Organize endpoints by identity, workspace, planning, storage, billing, support, notifications, and administration.
- Use verb-resource application methods such as `UpdateProjectTaskAsync`; avoid generic `UpdateAsync`.
- Every request field uses stable names (`projectId`, `taskId`, `membershipId`, `userId`).
- Errors include stable code, human-readable message, affected field when applicable, and enough correction guidance to retry.
- List APIs default to concise summaries; detail endpoints return full history/policy payloads.
- Avoid overlapping endpoints that make the caller guess which workflow owns a mutation.

## Context and memory policy

- This file is the anchored iterative summary and artifact trail.
- Retrieve source files just in time with `rg` and targeted reads.
- Keep unresolved compiler/test errors verbatim until fixed; retain only the final verification afterward.
- Preserve exact file paths, type names, method names, and migrations in the artifact trail.
- Do not add vector/graph memory: the product currently has no AI retrieval workload. Persist user-facing state in SQL and implementation state in structured Markdown.

## Baseline verification (2026-08-30)

- `dotnet test backend/Planora.slnx -c Release --no-restore`: 33 passed, 0 failed.
- `npm run build` in `frontend/Planora.Web.User`: passed.
- `npm run build` in `frontend/Planora.Web.Admin`: passed.
- Repository has no `.git` directory and no `AGENTS.md`.

## Final verification (2026-08-30)

- `dotnet test backend/Planora.slnx -c Release --no-restore`: 37 passed (16 unit, 21 integration), 0 failed.
- `npm run build` in `frontend/Planora.Web.User`: passed; initial bundle 354.18 kB raw / 97.71 kB estimated transfer.
- `npm run build` in `frontend/Planora.Web.Admin`: passed; initial bundle 335.43 kB raw / 94.28 kB estimated transfer.
- `flutter analyze` in `mobile/user-app`: no issues found.
- `flutter test`: not applicable because the existing app has no `test` directory.
- Playwright QA: 10 desktop/mobile captures, 0 console errors, report at `artifacts/playwright/playwright-report.json`.

## Current phase

Feature rebuild implemented and verified. The generated EF Core migration must be applied to the target database during deployment.

## Artifact trail

- Created `docs/implementation/FEATURE_REBUILD_STATE.md`.
- Added identity/profile/preferences, notification, billing, support, maintenance, quota, task-history and registered-user lookup application services and controllers.
- Added `20260830120351_FeatureRebuildFoundation` migration for the new user preferences and support/system tables.
- Expanded project/member/sprint/task/storage invariants, including owner-plan quota, dependency blocking, submission requirements, task audit history, rename/delete operations, and removal reasons.
- Rebuilt Web.User authentication and routed experiences for project views, billing, support, guide, terms, members, tasks and settings; added remembered-session storage.
- Added role discovery (`GET /api/projects/{projectId}/roles`) and available billing plans (`GET /api/billing/plans`) so UI workflows no longer require internal GUID entry.
- Added Web.Admin support/refund conversation queue and maintenance controls while preserving the aggregate-only trust boundary.
- Expanded Flutter navigation into functional overview, task, storage and profile surfaces.
- Expanded `tests/e2e/capture_planora.py` to cover the rebuilt pages in desktop and mobile viewports.
- Visual QA caught and fixed a five-column List layout bug, commitment-horizon grid contamination, billing quota composition, maintenance-card width, and Vietnamese heading font rendering.

## Deployment actions

1. Apply `20260830120351_FeatureRebuildFoundation` to the target database.
2. Configure SMTP and the Google OAuth client ID/secret before enabling email delivery and Google login in production.
3. Configure the real MoMo callback/signature exchange and bank-transfer reconciliation worker; the current workflow safely creates idempotent pending transactions but does not impersonate an external payment confirmation.
4. Replace preview mode with authenticated API sessions and run the same Playwright suite against the deployed environment.
