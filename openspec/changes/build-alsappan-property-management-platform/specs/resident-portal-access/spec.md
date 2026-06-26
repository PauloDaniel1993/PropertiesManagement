## ADDED Requirements

### Requirement: Resident account access
The system SHALL allow residents/tenants to authenticate with their own accounts linked to resident records and organization memberships.

#### Scenario: Resident signs in
- **WHEN** a resident submits valid credentials for an active resident account
- **THEN** the system creates a resident session scoped to the resident's permitted organization and resident record

### Requirement: Resident portal shell
The system SHALL provide a resident portal experience separate from the administrative shell with localized navigation and only resident-appropriate actions.

#### Scenario: Resident opens portal
- **WHEN** an authenticated resident opens the portal
- **THEN** the system displays only resident-permitted areas such as profile, property, contract, payments, documents, occurrences, inspections, and notifications

### Requirement: Resident self-service visibility
The system SHALL allow residents to view only records linked to their resident identity, active or historical contracts, and permitted organization context.

#### Scenario: Resident views payments
- **WHEN** a resident opens the payments area
- **THEN** the system returns only payments linked to the resident's contracts and hides administrative reconciliation fields

### Requirement: Resident-initiated requests
The system SHALL allow residents to create permitted self-service records such as occurrences, document uploads, and profile update requests when enabled by organization settings.

#### Scenario: Resident reports an occurrence
- **WHEN** a resident submits an occurrence from the portal with valid linked property or contract context
- **THEN** the system creates the occurrence, notifies configured administrative users, and records timeline and audit events
