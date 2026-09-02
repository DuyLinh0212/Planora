# Session State

## Session intent

Build Planora Phase 0–3 from the approved documentation set under `docs/docs/`.

## Current state

- Documentation read in the prescribed order, including the ERD image.
- Phase 0 complete: isolated `backend/` .NET 10 Clean Architecture solution, central build configuration, PostgreSQL/Neon persistence, EF Core migrations, Problem Details, OpenAPI, JSON logging, health checks, and Docker Compose.
- Phase 1 complete: email/password plus Google/Facebook identity boundaries, JWT/refresh rotation, project CRUD, invitation/member lifecycle, RBAC, and audit records.
- Phase 2 complete: sprint/backlog, tasks and assignees, submission/review/rework, deadline history/extensions, and an API-hosted deadline background service.
- Phase 3 complete: folders, configurable upload limit, Cloudinary/local storage boundary, versioned files/documents, folder rules, and version-pinned submission attachments.
- User web, admin control-room shell, and Flutter mobile workspace have production-ready responsive visual foundations. Admin business APIs remain Phase 6 by roadmap.
- Web source lives under `frontend/Planora.Web.User` and `frontend/Planora.Web.Admin`; Flutter lives under `mobile/user-app`.
- Backend was rebuilt around four projects and business modules. Generic `*UseCases` classes and ambiguous methods such as `CreateAsync`/`GetAsync` were replaced by named services and explicit methods such as `CreateProjectAsync` and `GetProjectsAsync`.

## Decisions

- Product name: Planora.
- Backend: .NET 10 modular monolith with Clean Architecture.
- Web: Angular 20 with Angular CLI, standalone components, and SCSS.
- Mobile: Flutter scaffold, sharing the API contract.
- Memory architecture for this implementation: filesystem-backed plan/state/artifact index; no semantic memory infrastructure because AI is outside MVP.
- API/tool contracts use verb–resource naming, non-overlapping responsibilities, concise result shapes, and actionable Problem Details errors.

## Risks

- External Google/Facebook/Cloudinary integration requires credentials; code must remain testable without real secrets.
- Docker is not installed in the current environment. The PostgreSQL baseline was applied and verified against Neon; CI uses PostgreSQL 17 for repeatable integration tests.
- Google, Facebook, and Cloudinary live calls require credentials. Without Cloudinary credentials, local development storage is selected intentionally.
- Billing checkout and automatic payment reconciliation are implemented for SePay bank-transfer webhooks. Subscription quota enforcement remains a production-hardening item.

## Next actions

1. Configure real secrets from `backend.env.example` or .NET user secrets.
2. Set Render/Neon secrets and run the production smoke-test checklist.
3. Connect the visual frontends to the deployed API and keep authenticated end-to-end tests in CI.
4. Add monitoring/alerts and complete remaining roadmap items (GitHub/calendar integrations, AI assistance, MFA, and malware scanning).

## Verification snapshot

- `dotnet test backend/Planora.slnx`: 14 passed, 0 failed after the architecture rebuild.
- `frontend/Planora.Web.User`: Angular CLI 20 production build passed.
- `frontend/Planora.Web.Admin`: Angular CLI 20 production build passed.
- `mobile/user-app`: Flutter analyzer passed from the new directory.
- PostgreSQL migrations are stored in `backend/src/Planora.Infrastructure/Persistence/PostgresMigrations`; the legacy import utility remains isolated under `backend/tools/Planora.DataMigration` and is not part of normal deploys.
- Payment callbacks are signature-verified and idempotent; duplicate webhook deliveries do not create duplicate transactions or entitlements.
