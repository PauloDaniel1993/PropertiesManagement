# Alsappan Property Management

Alsappan is a multi-tenant property management platform with a React frontend and a .NET 10 backend. The product defaults to Brazilian Portuguese (`pt-BR`) and is planned to support multiple languages, resident portal access, auditability, notifications, and white-label organization branding.

## Repository Layout

- `src/Alsappan.Api`: .NET 10 HTTP API.
- `src/Alsappan.Application`: application use cases, DTOs, validation, and contracts.
- `src/Alsappan.Domain`: domain entities, value objects, events, and rules.
- `src/Alsappan.Infrastructure`: persistence, storage, auth integrations, background work, and external adapters.
- `src/Alsappan.Web`: React, TypeScript, Vite, Zustand, TanStack Query, React Hook Form, Zod, and lucide-react.
- `tests`: backend test projects.
- `openspec`: product and implementation planning artifacts.
- `scripts`: local development helper scripts.
- `docs`: supporting implementation notes.

## Supporting Docs

- [API conventions](docs/api-conventions.md)
- [Domain glossary](docs/domain-glossary.md)
- [Deployment and operational readiness](docs/deployment-readiness.md)
- [Parallel feature development](docs/parallel-feature-development.md)
- [Security review checklist](docs/security-review-checklist.md)
- [Seed data plan](docs/seed-data.md)

## Prerequisites

- .NET SDK 10.0.301 or compatible latest feature SDK.
- Node.js 24 and npm 11.
- Docker Desktop or another Docker Compose-compatible runtime.
- PowerShell 7+ for local helper scripts.

## Local Setup

1. Restore dependencies:

   ```powershell
   ./scripts/restore.ps1
   ```

2. Start PostgreSQL:

   ```powershell
   docker compose up -d postgres
   ```

3. Run the API:

   ```powershell
   ./scripts/run-api.ps1
   ```

4. Run the web app:

   ```powershell
   ./scripts/run-web.ps1
   ```

## Validation

```powershell
./scripts/build.ps1
./scripts/test.ps1
npm run lint --workspace src/Alsappan.Web
npm run typecheck --workspace src/Alsappan.Web
npm run format:check --workspace src/Alsappan.Web
dotnet format Alsappan.slnx --verify-no-changes --no-restore
./scripts/validate-openapi.ps1
./scripts/validate-migrations.ps1
openspec validate build-alsappan-property-management-platform
```

Operational smoke checks for deployed environments:

```powershell
./scripts/performance-smoke.ps1 -BaseUrl https://api.example.com
```

## Git Flow

- `main`: production-ready releases.
- `develop`: integrated development branch.
- `feature/*`: feature and functionality work created from `develop`.
- `release/*`: release stabilization branches created from `develop`.
- `hotfix/*`: urgent fixes created from `main` and reconciled back into `develop`.

Each completed feature or functionality must be delivered through a pull request and wait for review before merge.
