# CI/CD

Planora uses GitHub Actions as the delivery gate for the .NET API, both Angular applications, and the production container.

## Delivery flow

1. A pull request or push to `main`/`develop` starts `CI`.
2. CI rejects tracked local credentials/backups, builds and tests the backend against a disposable PostgreSQL 17 database, checks EF migrations, tests and builds both Angular applications, and builds the backend Docker image.
3. `Security` runs dependency review and CodeQL when GitHub Advanced Security is available.
4. A successful `CI` run on `main` starts `Deploy Production`.
5. The CD job checks that the SHA is reachable from `main` and has a successful `CI success` check, deploys that exact SHA to Render, waits for Render to report `live`, verifies `/health/ready`, and only then deploys both Vercel applications.

Render and Vercel Git-based automatic production deployments should be disabled. `render.yaml` already sets `autoDeployTrigger: off`; this prevents an untested commit from racing the gated workflow.

## GitHub repository configuration

Create a GitHub Environment named `production`. Add a required reviewer for production deployments and prevent administrators from bypassing the protection if the repository policy permits it.

Add these Environment secrets:

| Name | Purpose |
|---|---|
| `RENDER_API_KEY` | Render API key with deploy access |
| `RENDER_SERVICE_ID` | Render API service id (`srv-...`) |
| `VERCEL_TOKEN` | Vercel deployment token |
| `VERCEL_ORG_ID` | Vercel team/account id |
| `VERCEL_USER_PROJECT_ID` | Vercel project id for `Planora.Web.User` |
| `VERCEL_ADMIN_PROJECT_ID` | Vercel project id for `Planora.Web.Admin` |

Add these Environment variables:

| Name | Example/meaning |
|---|---|
| `PLANORA_API_URL` | Public HTTPS API origin, without a trailing slash |
| `PLANORA_GOOGLE_CLIENT_ID` | Public Google OAuth browser client id used by the user web |

For a private repository with GitHub Advanced Security enabled, add repository variable `ENABLE_GHAS=true`. Without it, the GHAS-only dependency review and CodeQL jobs are intentionally skipped; all other CI checks still run.

Protect `main` and require pull requests plus the `CI success` status check. Do not allow force pushes. Protect release tags matching `v*` if supported by the repository plan.

## Render configuration

Create the service from `render.yaml`, then fill every `sync: false` value in the Render dashboard. At minimum, configure the Neon PostgreSQL connection string, JWT secret, CORS origins, and the selected payment provider credentials/webhook secrets. Keep `Database__ApplyMigrationsOnStartup=true`; the CD workflow waits for the deploy and readiness check before moving to the web applications.

`PLANORA_API_URL` must point to this service. Render's service API key and service id belong only in the GitHub `production` Environment, not in source code or Vercel.

## Vercel configuration

Create two Vercel projects rooted at:

- `frontend/Planora.Web.User`
- `frontend/Planora.Web.Admin`

Disable Vercel Git production deployments because GitHub Actions owns the production gate. The CD workflow pulls each project's production settings, generates Angular's production environment from GitHub variables, builds with Vercel CLI, and deploys the prebuilt output. Both projects include an SPA rewrite in `vercel.json`.

Set the Render CORS allowed origins to the final user/admin production domains, not temporary Vercel preview URLs.

## Dependency policy

Dependabot checks GitHub Actions, NuGet, both npm projects, and Docker weekly. High/critical production npm advisories fail CI unless an exact advisory has a non-expired entry in `.github/security/npm-audit-allowlist.json`.

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

Do not run production deployment workflows from a fork. GitHub withholds protected Environment secrets from ordinary pull requests, and the production workflow additionally refuses commits outside `main` or without the required successful CI check.
