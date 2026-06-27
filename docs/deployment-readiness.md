# Deployment and Operational Readiness

This runbook captures the first production-ready deployment slice for Alsappan. It is intentionally container-friendly but does not choose a final host; the same configuration applies to Docker Compose, a VPS, or a managed container platform.

## Runtime Components

- API: `src/Alsappan.Api`, .NET 10, listens on port `8080` in the container.
- Web: `src/Alsappan.Web`, Vite build served by nginx on port `8080` in the container.
- Database: PostgreSQL 18 or a compatible managed PostgreSQL service.
- File storage: local mounted volume for now, with the application storage abstraction ready for object storage later.
- Background work: outbox processor wiring exists in infrastructure; production scheduling must call the processor through a hosted worker or external job once that worker is enabled.

## Required Configuration

All production values must be supplied through environment variables or a secret store. Do not bake secrets into images.

| Area | Variable | Purpose |
| --- | --- | --- |
| API | `ASPNETCORE_ENVIRONMENT=Production` | Enables production runtime behavior. |
| Database | `ALSAPPAN_DATABASE__CONNECTIONSTRING` | PostgreSQL connection string used by EF Core and readiness checks. |
| Database | `ALSAPPAN_DATABASE__SCHEMA` | Database schema, default `app`. |
| Auth | `ALSAPPAN_AUTH__ISSUER` | JWT issuer expected by the API. |
| Auth | `ALSAPPAN_AUTH__AUDIENCE` | JWT audience used by the web client. |
| Auth | `ALSAPPAN_AUTH__SIGNINGKEY` | Symmetric signing key. Use at least 32 high-entropy characters and rotate through secret management. |
| Auth | `ALSAPPAN_AUTH__ACCESSTOKENMINUTES` | Access token lifetime, default `15`. |
| Auth | `ALSAPPAN_AUTH__REFRESHTOKENDAYS` | Refresh session lifetime, default `30`. |
| Auth | `ALSAPPAN_AUTH__REFRESHCOOKIENAME` | Refresh cookie name, default `__Host-alsappan-refresh`. |
| Multi-tenancy | `X-Alsappan-Organization-Id` request header | Active organization selector used by authenticated API calls. |
| Resident portal | Auth/session variables above plus organization resident settings | Resident sessions use the same API host and tenant context but resident-specific policies. |
| Storage | `ALSAPPAN_STORAGE__LOCALPATH` | Mounted private storage path for document binaries. |
| Localization | `ALSAPPAN_LOCALIZATION__DEFAULTCULTURE` | Default culture, expected `pt-BR`. |
| Localization | `ALSAPPAN_LOCALIZATION__SUPPORTEDCULTURES` | Comma-separated supported cultures, currently `pt-BR,en-US`. |
| CORS | `ALSAPPAN_CORS__ALLOWEDORIGINS` | Exact allowed web origins. Never use `*` with credentials. |
| Web | `VITE_API_BASE_URL` | API base URL embedded at web build time. |

## Docker Artifacts

- API Dockerfile: `src/Alsappan.Api/Dockerfile`
- Web Dockerfile: `src/Alsappan.Web/Dockerfile`
- Web nginx config: `src/Alsappan.Web/nginx.conf`
- Production compose example: `deploy/docker-compose.production.example.yml`

Example build commands from the repository root:

```powershell
docker build -f src/Alsappan.Api/Dockerfile -t alsappan-api:local .
docker build -f src/Alsappan.Web/Dockerfile --build-arg VITE_API_BASE_URL=https://api.example.com -t alsappan-web:local .
```

The production compose file is an example, not a committed environment. Copy the required values into a deployment secret store or an untracked `.env.production` file before running it.

## Database Migration Execution

Migrations are owned by `src/Alsappan.Infrastructure` and use `src/Alsappan.Api` as the startup project.

Recommended deployment sequence:

1. Build and publish the API image from the same commit being deployed.
2. Take a PostgreSQL backup before applying migrations.
3. Run migrations once per environment before routing user traffic to the new API.
4. Verify `/health/ready` after migrations complete.
5. Deploy the web build after the target API base URL is known.

Local or one-off command:

```powershell
./scripts/migrate.ps1
```

Equivalent command in an API build environment:

```powershell
dotnet ef database update --project src/Alsappan.Infrastructure --startup-project src/Alsappan.Api
```

Production migration rules:

- Prefer additive schema changes and backwards-compatible deployments.
- Do not run multiple migration jobs against the same database at the same time.
- Capture the deployed commit SHA, migration name, start time, end time, and operator/job identity.
- If a destructive migration is unavoidable, create and test a rollback script before deployment.

## Health Checks

The API exposes two health endpoints:

- `GET /health`: liveness check for the API process.
- `GET /health/ready`: readiness check for API, PostgreSQL, file storage, background worker wiring, and localization resources.

Readiness JSON includes each check name, status, duration, description, tags, and an error message when a component is unhealthy. Use `/health` for container liveness and `/health/ready` for load-balancer readiness.

The web container exposes `GET /health` through nginx and returns `ok`.

## Structured Logging

The API creates a structured logging scope per request with these fields:

- `OrganizationId`
- `UserId`
- `RequestId`
- `Action`
- `EntityType`
- `EntityId`

Clients may send `X-Request-ID`; otherwise the ASP.NET Core trace identifier is used and echoed in the response as `X-Request-ID`. Operations should forward this value from ingress/load balancers so audit, application logs, and support investigations can be correlated.

## Backup and Restore Assumptions

PostgreSQL:

- Use `pg_dump` or managed-service snapshots before each production migration.
- Retain point-in-time recovery logs according to the environment's recovery point objective.
- Test restore into a non-production database at least once per release cycle.
- Store backup metadata with commit SHA and migration name.

File storage:

- Treat `ALSAPPAN_STORAGE__LOCALPATH` as private tenant data.
- Back up file storage and PostgreSQL on the same schedule so document metadata and binaries stay consistent.
- Preserve storage keys exactly; they include organization-scoped paths.
- Restore database first, then restore file storage to the path configured for the restored API.

## Performance Smoke Checks

Use `scripts/performance-smoke.ps1` against a seeded environment to check basic latency for:

- Large list pagination with `pageSize=100`.
- Indexed filters for properties, residents, payments, occurrences, inspections, documents, timeline, and audit.
- Dashboard summary endpoint when the dashboard worker branch adds it.
- Global search endpoint when the search worker branch adds it.

Example:

```powershell
$env:ALSAPPAN_PERF_BEARER_TOKEN = "<access-token>"
$env:ALSAPPAN_PERF_ORGANIZATION_ID = "<organization-guid>"
./scripts/performance-smoke.ps1 -BaseUrl https://api.example.com -MaxMilliseconds 750
```

Dashboard and global search probes are marked optional in the script because those features are owned by another active branch. Turn on `-FailOnPending` only after those endpoints are merged.
