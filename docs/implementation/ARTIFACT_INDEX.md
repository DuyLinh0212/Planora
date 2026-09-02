# Artifact Index

## Read-only source documents

- `docs/README.md`
- `docs/docs/TECH_STACK.md` through `docs/docs/ARCHITECTURE_DECISIONS.md` in index order
- `docs/docs/DATABASE_ERD.png`
- `docs/docs/ENVIRONMENT.md`

## Implementation artifacts

- `docs/implementation/PHASE_0_3_IMPLEMENTATION_PLAN.md` — phase scope and verification gates.
- `docs/implementation/SESSION_STATE.md` — anchored handoff summary.
- `docs/implementation/ARTIFACT_INDEX.md` — durable file trail; append at phase boundaries.
- `backend/Planora.slnx`, `backend/Directory.Build.props`, `backend/Directory.Packages.props`, `backend/global.json` — isolated .NET 10 backend solution and central policy.
- `backend/src/Planora.Domain` — business modules and invariant-bearing domain behavior.
- `backend/src/Planora.Application` — business-oriented application services, ports, permission resolver, requests, and responses.
- `backend/src/Planora.Infrastructure` — EF Core mappings/migration, JWT/external identity, and Cloudinary/local storage implementations.
- `backend/src/Planora.Api` — thin controllers, JWT boundary, Problem Details, OpenAPI, rate limiting, health checks, and overdue-task background service.
- `backend/tests/Planora.UnitTests` — domain and application result tests.
- `backend/tests/Planora.IntegrationTests` — persistence-model and API contract tests.
- `frontend/Planora.Web.User` — Angular CLI 20 user web application scaffold.
- `frontend/Planora.Web.Admin` — Angular CLI 20 admin web application scaffold.
- `mobile/user-app` — responsive Flutter workspace foundation.
- `docs/implementation/FRONTEND_STRUCTURE.md` — ownership rules and target structure for Angular and Flutter frontends.
- `docs/implementation/BACKEND_STRUCTURE.md` — current Clean Architecture tree, dependency direction, and naming rules.
- `backend.env.example`, `docker-compose.yml` — local PostgreSQL configuration and service definition.
- `.config/dotnet-tools.json` — repository-pinned EF Core CLI 10.0.11.
- `backend/src/Planora.Infrastructure/Persistence/PostgresMigrations` — production PostgreSQL migrations.
- `backend/tools/Planora.DataMigration` — one-time SQL Server-to-Neon ETL/verification utility.
- `.github/workflows` — CI, security, production CD and Android release workflows.
