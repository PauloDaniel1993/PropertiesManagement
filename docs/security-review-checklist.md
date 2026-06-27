# Security Review Checklist

Use this checklist during pull request review for features that touch authentication, tenant data, resident access, payments, files, branding, audit, notifications, background processing, or dependency/runtime configuration.

## Scope Triage

- [ ] The PR states which modules, endpoints, jobs, UI routes, and storage paths changed.
- [ ] The PR identifies whether the change affects admin users, resident users, background workers, or unauthenticated users.
- [ ] The PR identifies new trust boundaries, external inputs, file inputs, provider payloads, webhook-like payloads, or generated links.
- [ ] The PR includes validation notes for backend tests, frontend tests, formatting, OpenSpec validation when applicable, and manual Chrome smoke testing for UI changes.

## Authentication And Sessions

- [ ] Admin and resident login paths stay separated by account type and route guard.
- [ ] Refresh tokens, session cookies, and bearer tokens are never logged, returned in error bodies, or stored in insecure frontend state.
- [ ] Session invalidation, logout, account deactivation, and organization membership changes cannot leave stale elevated access.
- [ ] Password, lockout, and future MFA settings are enforced server-side, not only hidden in the UI.
- [ ] Demo or seed credentials are clearly development-only and do not weaken production configuration.

## Authorization And Permissions

- [ ] Every backend endpoint requires the correct granular permission or resident-specific policy.
- [ ] Frontend menu items, routes, and row actions use the same canonical permission codes as the backend.
- [ ] Frontend visibility is treated as ergonomics only; backend policies remain authoritative.
- [ ] Wildcard permission `*` is honored consistently where platform administrator access is intended.
- [ ] Role seed changes are reviewed for accidental module access, especially for the `Morador` resident role.
- [ ] Permission checks use the active organization context and fail closed when the organization is missing.

## Tenant Isolation

- [ ] Every tenant-owned entity has `organization_id` and tenant-scoped query filtering.
- [ ] List, detail, create, update, archive, restore, search, and relationship queries reject cross-organization access.
- [ ] Background jobs, outbox records, notifications, timeline entries, audit entries, and dashboard projections carry organization context.
- [ ] File storage keys and download authorization cannot leak files across organizations.
- [ ] Tests cover cross-organization denial for new repositories, endpoints, and background projections.

## Resident Portal Policies

- [ ] Resident APIs are scoped to the linked resident identity, active/historical contracts, and permitted organization.
- [ ] Resident routes do not expose administrative module fields such as reconciliation metadata, internal notes, audit payloads, or administrator assignment controls unless explicitly allowed.
- [ ] Resident-created occurrences, uploads, and profile requests are gated by organization settings.
- [ ] Cross-resident access is denied for lists, details, file downloads, notifications, and deep links.
- [ ] Resident account invitation, activation, deactivation, password reset, and unlink flows are audited.

## Payment Provider Mock Boundaries

- [ ] Mock boleto, Pix, and PayPal providers never call external services or require real credentials.
- [ ] Payment instruction DTOs do not expose secrets and are safe to display in admin and resident flows.
- [ ] Mock provider references are tenant-scoped and cannot settle another organization's charge.
- [ ] Provider event mapping validates charge, organization, amount, currency, status transition, and idempotency.
- [ ] Payment settlement, reversal, and reconciliation paths write audit, timeline, notification, and balance updates consistently.

## Uploads And Documents

- [ ] Upload endpoints enforce authentication, permission, tenant scope, size limits, content type allowlists, and extension allowlists.
- [ ] Executable, script, and archive uploads remain blocked unless a future explicit requirement changes the policy.
- [ ] File names are sanitized for display and never used as trusted storage paths.
- [ ] Downloads verify both document permission and permission to the linked entity.
- [ ] Version uploads and document links preserve audit and timeline records.
- [ ] Storage failures leave database state consistent and do not orphan accessible files.

## White-Label Branding

- [ ] Brand colors are validated for allowed formats and contrast-safe combinations.
- [ ] Logo upload validates type, size, dimensions, and private storage behavior.
- [ ] Organization branding is scoped to the active organization and cannot affect other tenants.
- [ ] Custom brand display fields cannot inject HTML, script, unsafe URLs, or broken layout states.
- [ ] Reset-to-default behavior removes tenant-specific tokens and obsolete logo references safely.

## Audit, Timeline, Notifications, And Outbox

- [ ] Mutating use cases write audit records with actor, organization, action, target entity, and changed-field summary.
- [ ] Security-sensitive actions write audit records synchronously.
- [ ] Audit records are immutable through normal application APIs.
- [ ] Timeline entries represent meaningful business events, while audit captures technical/security details.
- [ ] Notifications respect recipient visibility, notification preferences, organization scope, and deep-link permissions.
- [ ] Outbox processors are idempotent and do not process another organization's payloads.

## Sensitive Data And Logging

- [ ] Sensitive personal data is masked in list views and logs unless the user has an explicit permission.
- [ ] Problem Details responses do not reveal stack traces, secrets, tokens, storage paths, SQL details, or existence of cross-tenant records.
- [ ] Structured logs include request ID, organization ID, user ID, action, and entity identifiers where applicable.
- [ ] Logs avoid document contents, payment instruction secrets, passwords, token values, and raw upload payloads.
- [ ] Test data and seed data avoid real personal information.

## Frontend Safety

- [ ] Production UI strings use locale resources and do not leak internal exception text.
- [ ] Route guards protect admin routes, resident routes, active organization requirements, and module permissions.
- [ ] API errors render safe localized states instead of exposing raw JSON or stack traces.
- [ ] Row actions are hidden or disabled according to permissions and archived state.
- [ ] Keyboard/focus states remain usable for dialogs, menus, upload controls, and destructive actions.

## Data Integrity

- [ ] Migrations include indexes for tenant scope, search/filter fields, and uniqueness constraints required to avoid race conditions.
- [ ] Domain validation runs before constructing or mutating entities where invalid input would otherwise throw infrastructure-level errors.
- [ ] Soft delete/archive paths preserve relationships and prevent hidden records from being reused incorrectly.
- [ ] Concurrency tokens or equivalent checks protect update flows that can overwrite another user's change.
- [ ] Delete/archive/restore semantics are consistent with module conventions.

## Dependencies And Runtime Configuration

- [ ] New dependencies are necessary, actively maintained, license-compatible, and covered by package lock files.
- [ ] Dependency usage avoids unsafe dynamic evaluation, shell invocation, path traversal, or deserialization of untrusted data.
- [ ] Environment variables for secrets, auth, database, storage, CORS, localization, and payment mocks are documented.
- [ ] Development defaults are safe to run locally and clearly distinct from production assumptions.
- [ ] Health checks do not disclose secrets or internal topology.

## Review Outcome

- [ ] Blocking findings have file/line references and a concrete remediation request.
- [ ] Non-blocking findings are recorded as follow-up issues or review notes.
- [ ] The reviewer confirms whether security-sensitive behavior was verified by automated tests, manual smoke testing, or code inspection only.
