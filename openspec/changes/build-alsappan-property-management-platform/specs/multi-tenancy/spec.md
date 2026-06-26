## ADDED Requirements

### Requirement: Organization data isolation
The system SHALL isolate all tenant-scoped business data by organization and SHALL prevent users from reading or mutating records outside their active organization.

#### Scenario: User requests another organization's property
- **WHEN** a user authenticated for one organization requests a property that belongs to another organization
- **THEN** the system rejects or hides the record without exposing cross-organization data

### Requirement: Organization membership
The system SHALL support users with membership in one or more organizations and SHALL derive effective permissions from the active organization membership.

#### Scenario: User switches organization
- **WHEN** a user with membership in multiple organizations selects a different active organization
- **THEN** the system updates the active organization context and recalculates visible routes, actions, and data

### Requirement: Tenant-scoped configuration
The system SHALL store organization-specific settings, catalogs, notification preferences, localization defaults, and branding separately per organization.

#### Scenario: Organization changes default locale
- **WHEN** an administrator changes the default locale for one organization
- **THEN** the system applies that default only to users and data views in that organization

### Requirement: Tenant-aware audit and timeline
The system SHALL include organization context in audit records, timeline entries, notifications, and background processing.

#### Scenario: Audit user filters by organization
- **WHEN** an audit user views audit records in an active organization
- **THEN** the system returns only audit entries for that organization unless the user has platform-level permissions
