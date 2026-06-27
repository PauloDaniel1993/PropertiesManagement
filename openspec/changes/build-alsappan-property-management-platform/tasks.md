## 1. Repository and Project Foundation

- [x] 1.1 Initialize the Git repository and add a .gitignore for .NET, Node, local env files, build outputs, and temporary uploads
- [x] 1.2 Create the .NET 10 solution structure with Api, Application, Domain, Infrastructure, and test projects
- [x] 1.3 Create the React TypeScript frontend project under the agreed web project folder with Zustand included for client state management
- [x] 1.4 Add shared formatting configuration for C#, TypeScript, JSON, Markdown, and YAML
- [x] 1.5 Add backend linting, analyzers, nullable reference types, warnings policy, and consistent test settings
- [x] 1.6 Add frontend linting, formatting, type checking, and test runner configuration
- [x] 1.7 Add Docker Compose for PostgreSQL and local development dependencies
- [x] 1.8 Add environment variable templates for backend, frontend, database, auth, file storage, and localization
- [x] 1.9 Add local development scripts for restoring, building, testing, running API, running web, and applying migrations
- [x] 1.10 Add a repository README with setup steps, project structure, and implementation conventions
- [x] 1.11 Add CI workflow placeholders for backend build/test, frontend build/test, OpenAPI validation, and formatting checks
- [x] 1.12 Add initial seed data plan for organizations, memberships, roles, permissions, locales, statuses, and catalog values

## 2. Shared Product and API Contracts

- [x] 2.1 Create a domain glossary mapping Portuguese UI terms to English code identifiers
- [x] 2.2 Define route names, menu labels, and default `pt-BR` copy for every visible menu item
- [x] 2.3 Define common REST API conventions for route versioning, pagination, sorting, filtering, and errors
- [x] 2.4 Implement Problem Details response helpers with localized validation and authorization messages
- [x] 2.5 Implement common DTOs for paged results, list filters, select options, status labels, and audit metadata
- [x] 2.6 Implement common entity metadata for tenant-scoped organization, created/updated/deleted audit fields, and concurrency tokens
- [x] 2.7 Implement value object conventions for IDs, money, dates, date ranges, addresses, and localized catalog labels
- [x] 2.8 Configure OpenAPI generation and document endpoint naming, tags, auth schemes, and error contracts
- [x] 2.9 Generate or validate a typed frontend API client from the backend OpenAPI contract
- [x] 2.10 Create shared backend validation patterns and localized message resources
- [x] 2.11 Create shared frontend validation patterns aligned with backend DTOs
- [x] 2.12 Define the module event envelope used by audit, timeline, notifications, dashboard projections, resident portal events, and tenant-scoped background processing

## 3. Backend Platform Core

- [x] 3.1 Configure dependency injection, configuration binding, logging, health checks, CORS, and request localization
- [x] 3.2 Create the EF Core DbContext, migration setup, schema naming conventions, and design-time migration factory
- [x] 3.3 Implement active organization context resolution from authenticated organization membership
- [x] 3.4 Implement soft delete and mandatory tenant-scoped query filtering conventions for organization-owned entities
- [x] 3.5 Implement authentication token services, refresh session persistence, and secure cookie/header strategy
- [x] 3.6 Implement role and permission policy services for backend endpoint authorization
- [x] 3.7 Implement audit writer infrastructure for synchronous security and mutation audit records
- [x] 3.8 Implement outbox table, outbox writer, and background processor skeleton
- [x] 3.9 Implement timeline projection infrastructure consuming domain/application events
- [x] 3.10 Implement notification dispatch infrastructure consuming domain/application events
- [x] 3.11 Implement file storage abstraction with local filesystem provider for development
- [x] 3.12 Implement database seed runner for organizations, memberships, roles, permissions, locales, statuses, and catalogs
- [x] 3.13 Add backend integration test fixture with real PostgreSQL or containerized database
- [x] 3.14 Add smoke tests for health checks, localization, Problem Details, auth rejection, and database connectivity
- [x] 3.15 Add tenant isolation integration tests for cross-organization read, write, search, audit, notification, storage, and background processing paths

## 4. Frontend Platform Shell

- [x] 4.1 Configure app routing with public login routes and protected authenticated routes
- [x] 4.2 Add the authenticated admin shell layout with sidebar, top bar, content region, active organization controls, and responsive behavior
- [x] 4.3 Add localized sidebar menu labels and icons for all menu items shown in the screenshot
- [x] 4.4 Add active menu state, route highlighting, collapsed navigation behavior, and mobile navigation behavior
- [x] 4.5 Add top-bar search entry point, theme toggle, notification button, and user avatar/profile menu
- [x] 4.6 Add `pt-BR` and `en-US` locale resources and locale provider with persisted user preference
- [x] 4.7 Add locale-aware formatting helpers for dates, times, money, numbers, and pluralization
- [x] 4.8 Add theme tokens, light/dark mode support, status colors, spacing, typography, and table density defaults
- [x] 4.9 Add shared page header, primary action, search input, filter bar, data table, pagination, badge, and row action components
- [x] 4.10 Add shared form controls, validation message rendering, dialogs/drawers, detail sections, tabs, and relationship panels
- [x] 4.11 Add shared loading, empty, error, forbidden, not found, archived, and optimistic refresh states
- [x] 4.12 Add API client provider, auth token handling, query cache, retry policy, and global error handling
- [x] 4.13 Add frontend tests for shell rendering, menu navigation, locale switching, theme switching, and protected route behavior
- [x] 4.14 Add responsive screenshot or Playwright checks for desktop, tablet, and mobile shell layouts
- [x] 4.15 Create Zustand stores for auth/session view state, active organization, shell state, theme, locale, persisted preferences, filters, and resident portal UI state
- [x] 4.16 Add default Alsappan design tokens and white-label token application for organization-specific branding
- [x] 4.17 Add frontend validation for organization brand colors, logo previews, contrast-safe states, and fallback to the default design

## 5. Identity and Administrators

- [x] 5.1 Model users, organizations, organization memberships, roles, permissions, role permissions, resident account links, refresh sessions, invitations, and user status
- [x] 5.2 Add migrations and seed data for starter roles: Administrador, Gestor, Operador, Leitura, and Morador
- [x] 5.3 Implement admin and resident login, refresh, logout, current user, active organization, and session invalidation endpoints
- [x] 5.4 Implement secure password hashing, password policy, failed login handling, and account lockout policy
- [x] 5.5 Implement permission claims and endpoint policies for every planned module permission, organization membership, and resident-specific access policy
- [x] 5.6 Implement security audit events for login, failed login, logout, token refresh, role change, and account status change
- [x] 5.7 Build the localized login page and authenticated session bootstrap flow
- [x] 5.8 Build frontend route guards, organization-aware guards, resident portal guards, and permission-aware action guards
- [x] 5.9 Implement administrators list, detail, invite/create, edit role, deactivate, reactivate, and archive endpoints
- [x] 5.10 Build Administradores list page with search, status filters, role filters, and row actions
- [x] 5.11 Build administrator create/invite/edit forms with localized validation
- [x] 5.12 Add tests for auth success, auth failure, permission denial, role changes, and administrator lifecycle
- [x] 5.13 Implement organization switching endpoints and membership-aware current user responses
- [x] 5.14 Build active organization switcher for users with multiple memberships
- [x] 5.15 Add tests for active organization switching, membership denial, resident account login, and resident/admin route separation

## 6. Imoveis Module

- [x] 6.1 Model properties with name, description, address, status, suggested rent, currency, garage data, notes, and lifecycle metadata
- [x] 6.2 Add property statuses and property types to seed catalogs with localized labels
- [x] 6.3 Add property migrations, indexes for search/filter fields, and organization scoping
- [x] 6.4 Implement property create, list, detail, update, archive, restore, and status transition use cases
- [x] 6.5 Implement property API endpoints with pagination, sorting, text search, and structured filters
- [x] 6.6 Emit property audit, timeline, and outbox events for all mutating actions
- [x] 6.7 Build Imoveis list page matching the screenshot pattern with search, columns, badges, and icon row actions
- [x] 6.8 Build property create/edit form with address, rent, garage, status, and notes sections
- [x] 6.9 Build property detail page with tabs or sections for contracts, residents, payments, utilities, documents, pets, vehicles, occurrences, inspections, timeline, and audit links
- [x] 6.10 Add frontend and backend tests for property CRUD, search, status labels, permissions, localization, and archived state

## 7. Moradores Module

- [x] 7.1 Model residents with personal data, contacts, identification, emergency contact, notes, privacy flags, optional linked user account, portal status, and lifecycle metadata
- [x] 7.2 Add resident migrations, indexes for name, email, phone, document identifier, and organization scoping
- [x] 7.3 Implement resident create, list, detail, update, archive, restore, and duplicate warning use cases
- [x] 7.4 Implement resident API endpoints with search, filters, permission-aware sensitive field masking, and relationship summaries
- [x] 7.5 Emit resident audit, timeline, and outbox events for all mutating actions
- [x] 7.6 Build Moradores list page with search, status filters, contact summary, and row actions
- [x] 7.7 Build resident create/edit form with localized validation and duplicate warning flow
- [x] 7.8 Build resident detail page with contracts, properties, payments, documents, pets, vehicles, occurrences, timeline, and audit links
- [x] 7.9 Add tests for resident CRUD, duplicate detection, sensitive field masking, permissions, localization, and archived state
- [x] 7.10 Implement resident account invitation, activation, deactivation, password reset, and link/unlink use cases
- [x] 7.11 Implement resident portal summary APIs for profile, linked property, contracts, payments, documents, occurrences, inspections, and notifications
- [x] 7.12 Build resident portal shell with localized navigation, profile menu, notifications, theme, locale, and responsive layout
- [x] 7.13 Build resident portal pages for profile, property, contracts, payments, documents, occurrences, inspections, and notifications
- [x] 7.14 Implement resident-created occurrence and resident document upload flows controlled by organization settings
- [x] 7.15 Add tests for resident portal login, linked-record visibility, cross-resident denial, resident-created occurrence, resident upload permissions, and localization

## 8. Contratos Module

- [x] 8.1 Model contracts, contract residents, primary responsible resident, lifecycle status, dates, rent terms, due day, deposit, adjustment data, and notes
- [x] 8.2 Add contract statuses and adjustment/indexer catalogs with localized labels
- [x] 8.3 Add contract migrations, indexes for property, resident, status, start date, end date, and organization scoping
- [x] 8.4 Implement contract create, list, detail, update, activate, terminate, cancel, archive, and restore use cases
- [x] 8.5 Implement contract validation for property availability, resident associations, date ranges, and lifecycle transitions
- [x] 8.6 Implement contract API endpoints with search, filters, relationship summaries, and document link summaries
- [x] 8.7 Emit contract audit, timeline, outbox, property status update, and notification events for lifecycle changes
- [x] 8.8 Build Contratos list page with filters for status, property, resident, date range, and ending soon
- [x] 8.9 Build contract create/edit form with property picker, resident picker, terms, lifecycle action, and document link section
- [x] 8.10 Build contract detail page with residents, property, payments, utility accounts, documents, timeline, and audit links
- [x] 8.11 Add tests for contract lifecycle, property availability, multi-resident contracts, permissions, localization, and relationship display

## 9. Documentos Module

- [x] 9.1 Model documents, document versions, document links, categories, storage keys, file metadata, access metadata, and lifecycle status
- [x] 9.2 Add document categories and allowed file type configuration with localized labels
- [x] 9.3 Add document migrations, indexes for category, linked entity, filename, upload date, and organization scoping
- [x] 9.4 Implement document upload, list, detail, metadata update, archive, restore, download, and version upload use cases
- [x] 9.5 Implement document link use cases for properties, contracts, residents, payments, utility accounts, pets, vehicles, occurrences, and inspections
- [x] 9.6 Implement file validation for size, content type, extension, authorization, and private storage access
- [x] 9.7 Emit document audit, timeline, and outbox events for upload, download where configured, versioning, link, and archive actions
- [x] 9.8 Build Documentos list page with category, linked entity, date, and owner filters
- [x] 9.9 Build upload/edit/version dialogs with progress, validation, metadata, and entity linking controls
- [x] 9.10 Build document detail page with versions, links, metadata, download action, timeline, and audit links
- [x] 9.11 Add tests for upload, download authorization, entity links, versions, storage failures, localization, and archived state

## 10. Pagamentos Module

- [x] 10.1 Model charges, payment transactions, balances, payment methods, receipt links, discounts, penalties, reconciliation metadata, and lifecycle status
- [x] 10.2 Add payment statuses, methods, and reconciliation catalogs with localized labels
- [x] 10.3 Add payment migrations, indexes for contract, property, resident, due date, status, and organization scoping
- [x] 10.4 Implement charge create, list, detail, update, cancel, archive, restore, and status calculation use cases
- [x] 10.5 Implement transaction record, partial settlement, full settlement, reversal/correction, and balance recalculation use cases
- [x] 10.6 Implement overdue detection job or query projection for dashboard and notifications
- [x] 10.7 Implement payment API endpoints with filters for status, due date, contract, resident, property, and overdue state
- [x] 10.8 Emit payment audit, timeline, outbox, notification, and dashboard projection events
- [x] 10.9 Build Pagamentos list page with status tabs, due date filters, amount columns, and settlement actions
- [x] 10.10 Build charge create/edit and payment settlement forms with receipt document attachment
- [x] 10.11 Build payment detail page with transactions, balance history, linked documents, timeline, and audit links
- [x] 10.12 Add tests for charge lifecycle, partial payments, overdue status, receipt links, permissions, localization, and dashboard impact
- [x] 10.13 Define payment provider interfaces for payment instructions, provider references, status mapping, mock events, and reconciliation callbacks
- [x] 10.14 Implement mocked boleto provider returning placeholder barcode, linha digitavel, due date, amount, payer summary, and provider reference
- [x] 10.15 Implement mocked Pix provider returning placeholder QR payload, copy-and-paste code, expiration, amount, payer summary, and provider reference
- [x] 10.16 Implement mocked PayPal provider returning placeholder payment intent ID, approval URL, status, amount, payer summary, and provider reference
- [x] 10.17 Add payment instruction endpoints and DTOs for boleto, Pix, and PayPal mock provider responses
- [x] 10.18 Add resident portal payment instruction display for mocked boleto, Pix, and PayPal
- [x] 10.19 Add tests for mocked provider instruction generation, mock event settlement mapping, tenant isolation, resident visibility, and future-provider contract stability

## 11. Contas de Consumo Module

- [x] 11.1 Model utility accounts with type, billing period, amount, currency, due date, responsible party, linked property, linked contract, and status
- [x] 11.2 Add utility account type and responsibility catalogs with localized labels
- [x] 11.3 Add utility account migrations, indexes for type, property, contract, due date, status, and organization scoping
- [x] 11.4 Implement utility account create, list, detail, update, mark paid, cancel, archive, and restore use cases
- [x] 11.5 Implement utility payment metadata and document link use cases for bills and receipts
- [x] 11.6 Implement utility account API endpoints with filters for type, status, responsible party, property, contract, and billing period
- [x] 11.7 Emit utility audit, timeline, outbox, notification, and dashboard projection events
- [x] 11.8 Build Contas de Consumo list page with type, status, due date, property, and responsible party filters
- [x] 11.9 Build utility account create/edit and mark-paid forms with bill/receipt document attachment
- [x] 11.10 Build utility account detail page with linked property, contract, responsible party, documents, timeline, and audit links
- [x] 11.11 Add tests for utility lifecycle, responsibility assignment, document links, overdue state, permissions, and localization

## 12. Pets Module

- [x] 12.1 Model pets with resident owner, optional property/contract context, name, species, breed, authorization state, notes, and lifecycle metadata
- [x] 12.2 Add pet species and authorization status catalogs with localized labels
- [x] 12.3 Add pet migrations, indexes for resident, property, species, authorization state, and organization scoping
- [x] 12.4 Implement pet create, list, detail, update, authorize, deny, archive, and restore use cases
- [x] 12.5 Implement pet document link use cases for vaccination records and authorization forms
- [x] 12.6 Implement pet API endpoints with filters for resident, property, species, authorization state, and active contract context
- [x] 12.7 Emit pet audit, timeline, and notification events for registration and authorization changes
- [x] 12.8 Build Pets list page with owner, property, species, and authorization filters
- [x] 12.9 Build pet create/edit form with resident picker, property context, authorization state, and document links
- [x] 12.10 Build pet detail page with owner, documents, authorization history, timeline, and audit links
- [x] 12.11 Add tests for pet CRUD, authorization flow, resident/property relationships, document links, permissions, and localization

## 13. Veiculos Module

- [x] 13.1 Model vehicles with resident owner, optional property/contract context, plate, type, color, brand/model, authorization state, parking allocation, and lifecycle metadata
- [x] 13.2 Add vehicle type and authorization status catalogs with localized labels
- [x] 13.3 Add vehicle migrations, indexes for plate, resident, property, authorization state, parking allocation, and organization scoping
- [x] 13.4 Implement vehicle create, list, detail, update, authorize, deny, archive, and restore use cases
- [x] 13.5 Implement parking allocation validation against property garage information where available
- [x] 13.6 Implement vehicle API endpoints with filters for plate, resident, property, type, parking allocation, and authorization state
- [x] 13.7 Emit vehicle audit, timeline, and notification events for registration, authorization, and parking changes
- [x] 13.8 Build Veiculos list page with plate search, owner, property, type, parking, and authorization filters
- [x] 13.9 Build vehicle create/edit form with resident picker, vehicle data, authorization state, and parking allocation controls
- [x] 13.10 Build vehicle detail page with owner, property, parking, authorization history, timeline, and audit links
- [x] 13.11 Add tests for vehicle CRUD, normalized plate search, parking allocation, authorization flow, permissions, and localization

## 14. Ocorrencias Module

- [x] 14.1 Model occurrences with type, priority, status, description, linked entities, assigned user, due date, resolution data, and lifecycle metadata
- [x] 14.2 Model occurrence comments, attachments, status history, priority history, and assignment history
- [x] 14.3 Add occurrence type, priority, and status catalogs with localized labels
- [x] 14.4 Add occurrence migrations, indexes for status, priority, assignee, property, resident, date, and organization scoping
- [x] 14.5 Implement occurrence create, list, detail, update, assign, change priority, change status, resolve, cancel, archive, and restore use cases
- [x] 14.6 Implement occurrence comment and attachment use cases with document integration
- [x] 14.7 Implement occurrence API endpoints with filters for type, priority, status, assignee, property, resident, date range, and unresolved state
- [x] 14.8 Emit occurrence audit, timeline, notification, and dashboard projection events
- [x] 14.9 Build Ocorrencias list page with priority/status filters, assignee filters, unresolved shortcuts, and row actions
- [x] 14.10 Build occurrence create/edit/detail workflow with comments, attachments, assignments, status changes, and resolution notes
- [x] 14.11 Add tests for occurrence workflow, comments, attachments, assignment notifications, permissions, localization, and dashboard impact

## 15. Vistorias Module

- [x] 15.1 Model inspections with type, property, optional contract/resident context, scheduled date, assigned user, status, completion metadata, and lifecycle metadata
- [x] 15.2 Model inspection areas, checklist items, condition ratings, observations, photos, document links, and signature slots
- [x] 15.3 Add inspection type, condition rating, and status catalogs with localized labels
- [x] 15.4 Add inspection migrations, indexes for property, contract, assignee, scheduled date, status, and organization scoping
- [x] 15.5 Implement inspection schedule, list, detail, update, start, complete, cancel, archive, and restore use cases
- [x] 15.6 Implement checklist item create/update/delete and completion progress use cases
- [x] 15.7 Implement inspection document/photo link use cases through the documents module
- [x] 15.8 Implement inspection API endpoints with filters for property, contract, assignee, status, date range, and pending state
- [x] 15.9 Emit inspection audit, timeline, notification, and dashboard projection events
- [x] 15.10 Build Vistorias list page with date, status, assignee, property, and pending filters
- [x] 15.11 Build inspection schedule/edit form and checklist execution experience
- [x] 15.12 Build completed inspection report view using structured checklist data and attachments
- [x] 15.13 Add tests for scheduling, checklist progress, completion lock, document links, permissions, localization, and dashboard impact

## 16. Notificacoes Module

- [x] 16.1 Model notifications with recipient, category, event type, message payload, deep link, read state, channel, and delivery status
- [x] 16.2 Model notification preferences by user, category, and supported channel
- [x] 16.3 Add notification category and channel catalogs with localized labels
- [x] 16.4 Add notification migrations and indexes for recipient, unread state, category, timestamp, and organization scoping
- [x] 16.5 Implement notification creation from outbox/domain events using recipient and preference rules
- [x] 16.6 Implement notification list, unread count, mark read, mark all read, archive, and preference endpoints
- [x] 16.7 Implement localized notification rendering from canonical event payloads
- [x] 16.8 Integrate top-bar notification badge and popover with unread count and latest notifications
- [x] 16.9 Build Notificacoes page with filters, read/unread actions, deep links, and preferences entry point
- [x] 16.10 Add tests for event-triggered notifications, unread counts, read state, preferences, localization, permissions, and deep links

## 17. Timeline Module

- [x] 17.1 Model timeline entries with event type, actor, target entity, related entities, occurred timestamp, localized display payload, and organization scope
- [x] 17.2 Add timeline migrations and indexes for entity type, entity ID, event type, actor, date, and organization scoping
- [x] 17.3 Implement timeline projection handlers for property, resident, contract, payment, utility, document, pet, vehicle, occurrence, inspection, and administrator events
- [x] 17.4 Implement global timeline API with filters for entity type, event type, actor, date range, and related entity
- [x] 17.5 Implement entity timeline API for detail pages with permission-aware filtering
- [x] 17.6 Build Timeline page with grouped chronological entries, filters, localized labels, and deep links
- [x] 17.7 Build reusable entity timeline panel for detail pages
- [x] 17.8 Add tests for projection creation, permission filtering, localization, entity panels, global filters, and deep links

## 18. Auditoria Module

- [x] 18.1 Model immutable audit entries with actor, action, target entity, changed field summary, request context, security category, and timestamp
- [x] 18.2 Add audit migrations and indexes for actor, action, entity type, entity ID, date, security category, and organization scoping
- [x] 18.3 Implement audit writing in all mutating use cases and security-sensitive identity use cases
- [x] 18.4 Implement guardrails that prevent normal application APIs from editing or deleting audit entries
- [x] 18.5 Implement audit list and detail endpoints with filters for actor, action, entity, date range, and security category
- [x] 18.6 Implement audit permission policies and administrator-only navigation visibility
- [x] 18.7 Build Auditoria page with filters, results table, detail expansion, and protected access handling
- [x] 18.8 Add tests for audit immutability, permission denial, mutation coverage, filter accuracy, localization, and sensitive payload redaction

## 19. Dashboard and Global Search

- [x] 19.1 Implement dashboard metrics queries for occupancy, overdue payments, contract expirations, open occurrences, pending inspections, and recent activity
- [x] 19.2 Implement permission-aware dashboard API responses with hidden or masked metrics where required
- [x] 19.3 Build dashboard widgets, empty states, loading states, error states, and deep links to filtered module views
- [x] 19.4 Add dashboard tests for metric accuracy, empty data, permission masking, localization, and deep links
- [x] 19.5 Define global search result contract with entity type, localized label, summary, matched field, and target route
- [x] 19.6 Implement global search backend across properties, residents, contracts, payments, documents, occurrences, and inspections
- [x] 19.7 Add search indexes or optimized queries for all included entity fields
- [x] 19.8 Integrate top-bar global search UI with keyboard behavior, grouped results, loading, empty, and permission-safe states
- [x] 19.9 Add tests for global search matching, permission filtering, localization, and deep links

## 20. Configuracoes Module

- [x] 20.1 Model organization settings, tenant isolation defaults, localization settings, resident portal settings, security settings, notification settings, and domain catalog settings
- [x] 20.2 Add settings migrations and seed defaults for organization, white-label branding, locale, security, notification, and catalogs
- [x] 20.3 Implement organization profile settings endpoints with audit events
- [x] 20.4 Implement locale enablement, default locale, user locale preference, and fallback behavior endpoints
- [x] 20.5 Implement domain catalog endpoints for property types, occurrence types, inspection types, document categories, utility types, and other configurable labels
- [x] 20.6 Implement security settings endpoints for session timeout, password rules, and MFA policy placeholders
- [x] 20.7 Implement notification settings endpoints for categories and channels
- [x] 20.8 Build Configuracoes page with tabs for organization, tenant behavior, resident portal, localization, catalogs, notifications, security, and profile preferences
- [x] 20.9 Add localized forms for editing settings, white-label branding, and catalog labels
- [x] 20.10 Add tests for settings permissions, audit events, tenant-specific settings, resident portal settings, white-label branding, locale behavior, catalog updates, validation, and frontend tab navigation
- [x] 20.11 Add backend validation for brand asset type, size, dimensions, allowed color values, and contrast-safe color combinations
- [x] 20.12 Add organization branding endpoints for logo upload/removal, brand token update, default design reset, and support/contact metadata

## 21. Cross-Module Relationship Integration

- [x] 21.1 Add relationship panels to property details for contracts, residents, payments, utility accounts, documents, pets, vehicles, occurrences, inspections, timeline, and audit links
- [x] 21.2 Add relationship panels to resident details for contracts, properties, payments, documents, pets, vehicles, occurrences, timeline, and audit links
- [x] 21.3 Add relationship panels to contract details for property, residents, payments, utility accounts, documents, inspections, timeline, and audit links
- [x] 21.4 Add document link controls to all modules that support attachments
- [x] 21.5 Add timeline panels to all detail pages that require activity history
- [x] 21.6 Add permission-aware visibility rules to all relationship panels and row actions
- [x] 21.7 Add consistent archive/restore behavior across all archive-capable modules
- [x] 21.8 Add consistent status badge rendering across all modules and locales
- [x] 21.9 Add cross-module tests for deep links, related data visibility, permission filtering, archive state, and localized relationship labels

## 22. Localization, Accessibility, and UI Hardening

- [ ] 22.1 Audit frontend code to ensure production UI strings use locale keys instead of hardcoded text
- [x] 22.2 Audit backend validation and Problem Details messages for `pt-BR` default localization
- [ ] 22.3 Add missing `en-US` translations for all implemented modules
- [ ] 22.4 Validate currency, date, number, pluralization, and status formatting in `pt-BR` and `en-US`
- [ ] 22.5 Add keyboard navigation, focus states, aria labels, and accessible names for icons and row actions
- [ ] 22.6 Verify table, form, dialog, sidebar, and topbar accessibility with automated checks
- [ ] 22.7 Verify responsive layouts for desktop, tablet, and mobile across all module list pages
- [ ] 22.8 Verify text overflow, wrapping, and table behavior for long Portuguese labels and long entity names
- [ ] 22.9 Add visual regression or screenshot checks for the authenticated shell and representative module pages

## 23. End-to-End Testing and Quality Gates

- [x] 23.1 Add demo seed data covering properties, residents, contracts, payments, utilities, documents, pets, vehicles, occurrences, inspections, notifications, timeline, and audit
- [ ] 23.2 Add E2E test for login, active organization switching, shell navigation, locale switch, and logout
- [ ] 23.3 Add E2E test for creating a property, resident, contract, and linked document
- [ ] 23.4 Add E2E test for creating and settling a payment with receipt document
- [ ] 23.5 Add E2E test for creating, assigning, commenting on, and resolving an occurrence
- [ ] 23.6 Add E2E test for scheduling, editing, completing, and viewing an inspection report
- [ ] 23.7 Add E2E test for notification creation, unread count, read state, and deep link navigation
- [ ] 23.8 Add E2E test for audit access control and audit filtering
- [x] 23.9 Add API integration tests for authorization on every module endpoint
- [x] 23.10 Add CI gates for backend tests, frontend tests, type checks, linting, formatting, OpenAPI generation, and migration validation
- [ ] 23.11 Add E2E test for resident portal login, resident-scoped data visibility, resident-created occurrence, and cross-resident access denial
- [ ] 23.12 Add E2E test for cross-organization isolation in admin lists, details, global search, dashboard, audit, notifications, and file downloads
- [ ] 23.13 Run manual Chrome browser smoke testing for the admin shell, resident portal, locale switching, organization switching, core CRUD flows, and permission-denied states when UI implementation is available
- [ ] 23.14 Add E2E test for mocked boleto, Pix, and PayPal payment instruction display in admin and resident flows
- [ ] 23.15 Add E2E test for white-label branding defaults, organization branding changes, contrast validation, and resident portal branding

## 24. Deployment and Operational Readiness

- [x] 24.1 Add production-ready configuration documentation for API, web, database, auth, multi-tenancy, resident portal, file storage, localization, and CORS
- [x] 24.2 Add Dockerfiles or deployment artifacts for backend and frontend
- [x] 24.3 Add database migration execution guidance for deployment environments
- [x] 24.4 Add structured logging fields for organization, user, request ID, action, entity type, and entity ID
- [x] 24.5 Add health checks for API, database, storage, background worker, and localization resource loading
- [x] 24.6 Add backup and restore assumptions for PostgreSQL and file storage
- [x] 24.7 Add basic performance checks for large lists, indexed filters, dashboard queries, and global search
- [x] 24.8 Add security review checklist for auth, tenant isolation, resident portal policies, payment provider mock boundaries, permissions, white-label asset validation, upload restrictions, audit, sensitive data masking, and dependency scanning
- [ ] 24.9 Run full local validation from clean checkout instructions and update README gaps
- [ ] 24.10 Run final OpenSpec validation/status check before implementation starts
