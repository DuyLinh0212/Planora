# Planora Phase 0–3 Implementation Plan

## Objective

Deliver a buildable .NET 10 modular monolith and usable web foundation covering Foundation, Identity/Project, Agile/Task, and Storage as defined by `docs/docs/ROADMAP.md`.

## Scope by phase

### Phase 0 — Foundation

- .NET 10 backend solution with Domain, Application, Infrastructure, Api, UnitTests, and IntegrationTests.
- Central package management, shared build settings, editor/configuration files, Docker Compose, environment template.
- PostgreSQL EF Core context, Neon connection configuration, and reviewed migrations.
- RFC 9457 Problem Details, built-in OpenAPI, health checks, rate limiting, structured console logging.
- User Web and Admin Web foundations; Flutter mobile scaffold.

### Phase 1 — Identity + Project

- Email/password registration and login, JWT access token, hashed/rotated refresh token.
- External login contract for Google/Facebook with server-side token verification boundary.
- Project CRUD, invitations, membership, default/custom roles, permission resolution, audit log.

### Phase 2 — Agile + Task

- Sprint and backlog operations.
- Task lifecycle, assignees, acceptance criteria, submission/review/rework.
- Deadline history, member extension requests, leader direct extension.
- API-hosted deadline background service invoking `TaskService.ExpireOverdueProjectTasksAsync`.

### Phase 3 — Storage

- Project folder tree and access rules.
- Cloudinary storage gateway with local file storage when credentials are absent.
- File and document versioning.
- Submission attachment pinned to an exact file version.

## Architectural constraints

- Dependency direction: Api → Application → Domain; Infrastructure implements Application abstractions and references only Application + Domain.
- Domain contains business behavior and does not reference ASP.NET Core, EF Core, Cloudinary, or payment SDKs.
- API endpoints are thin and return typed responses or Problem Details.
- Database access uses projections; migrations are reviewed artifacts and never auto-applied in production.
- Admin is not implicitly authorized to inspect private project content.

## Context and memory policy

- Keep this document and `SESSION_STATE.md` as anchored handoff summaries.
- Record created/modified files in `ARTIFACT_INDEX.md` at phase boundaries.
- Retrieve source documents just in time; do not duplicate all documentation into active implementation context.
- Preserve exact identifiers, paths, test failures, and architectural decisions.

## Frontend design direction

- Subject: a calm project cockpit for students, freelancers, and small teams.
- Page job: make the next meaningful action and project health obvious within five seconds.
- Palette: Porcelain `#F5F7FB`, Night Ink `#121A2D`, Draft Blue `#3155C6`, Quiet Mist `#DDE8FF`, Lagoon `#36A6A0`, Signal Coral `#F47B6B`.
- Type: Instrument Serif for restrained display moments, Manrope for interface text, IBM Plex Mono for dates/metrics.
- Layout: an asymmetric project canvas with a persistent navigation rail and a central workstream ribbon.
- Signature: a continuous workstream ribbon connecting sprint intent, active work, review, and completion.
- Accessibility floor: keyboard focus, reduced motion, semantic landmarks, responsive layout, and status labels that do not rely on color alone.

## Verification gates

- `dotnet build` succeeds with warnings treated as errors.
- Domain and Application tests cover deadline, extension, approval, permission, and quota-critical rules.
- API integration tests cover auth and cross-project authorization boundaries.
- Web lint/build succeeds.
- Flutter analyzer succeeds.

## Completion

All Phase 0–3 roadmap capabilities listed above are implemented. Static and automated verification passed. PostgreSQL migrations and the Neon baseline are ready for Render deployment; the legacy SQL Server import has completed and its isolated ETL utility is retained only for audit/recovery work.
