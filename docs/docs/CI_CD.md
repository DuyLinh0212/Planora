# CI/CD

Planora uses GitHub Actions for continuous integration and security checks. Production deployment is handled by the native Git integrations of Render and Vercel.

## Delivery flow

1. A pull request or push to `main`/`develop` starts `CI`.
2. CI rejects tracked local credentials/backups, builds and tests the backend against a disposable PostgreSQL 17 database, checks EF migrations, tests and builds both Angular applications, and builds the backend Docker image.
3. `Security` runs dependency review and CodeQL when GitHub Advanced Security is available.
4. Render and Vercel automatically deploy the latest commit on `main` through their Git integrations.

Keep `main` protected and require the `CI success` check before merging. This keeps failed pull requests out of the production branch without duplicating deployment credentials in GitHub Actions.

## GitHub repository configuration

For a private repository with GitHub Advanced Security enabled, add repository variable `ENABLE_GHAS=true`. Without it, the GHAS-only dependency review and CodeQL jobs are intentionally skipped; all other CI checks still run.

Protect `main` and require pull requests plus the `CI success` status check. Do not allow force pushes. Protect release tags matching `v*` if supported by the repository plan.

## Render configuration

Create the service from `render.yaml`, then fill every `sync: false` value in the Render dashboard. At minimum, configure the Neon PostgreSQL connection string, JWT secret, CORS origins, and the selected payment provider credentials/webhook secrets. Keep `Database__ApplyMigrationsOnStartup=true` for the single production instance.

`PLANORA_API_URL` must point to this service. No Render API key is required in GitHub Actions when Render Git auto-deploy is enabled.

## Vercel configuration

Create two Vercel projects rooted at:

- `frontend/Planora.Web.User`
- `frontend/Planora.Web.Admin`

Keep Vercel Git production deployments enabled. Each project generates its Angular production environment from Vercel project variables and deploys from its configured root directory. Both projects include an SPA rewrite in `vercel.json`.

Set the Render CORS allowed origins to the final user/admin production domains, not temporary Vercel preview URLs.

## Dependency policy

Dependency updates are performed deliberately by maintainers. CI still audits the committed dependencies, and high/critical production npm advisories fail CI unless an exact advisory has a non-expired entry in `.github/security/npm-audit-allowlist.json`.

The current `xlsx` exceptions expire on 2026-12-01. Replace or upgrade `xlsx` before that date; CI will fail automatically after expiry.

## Local equivalents

```powershell
dotnet build backend/Planora.slnx --configuration Release
dotnet test backend/Planora.slnx --configuration Release

Push-Location frontend/Planora.Web.User
npm ci
npm test -- --watch=false --browsers=ChromeHeadless
npm run build -- --configuration production
Pop-Location

Push-Location frontend/Planora.Web.Admin
npm ci
npm test -- --watch=false --browsers=ChromeHeadless
npm run build -- --configuration production
Pop-Location

```

Production deployment is configured in Render and Vercel, not through a GitHub deployment workflow.
