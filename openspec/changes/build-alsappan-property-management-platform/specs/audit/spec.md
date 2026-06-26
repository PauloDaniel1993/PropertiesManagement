## ADDED Requirements

### Requirement: Immutable audit log
The system SHALL record immutable audit entries for authentication events, authorization-sensitive actions, mutating commands, permission changes, and sensitive data access where configured.

#### Scenario: Property is updated
- **WHEN** an authorized user updates a property
- **THEN** the system records an audit entry with actor, timestamp, action, target entity, and changed field summary

### Requirement: Audit access control
The system SHALL restrict audit log access to users with explicit audit permissions.

#### Scenario: User without audit permission opens Auditoria
- **WHEN** a user without audit read permission navigates to the audit route
- **THEN** the system blocks access and does not expose audit records

### Requirement: Audit filtering
The system SHALL allow audit users to filter entries by actor, action, entity type, entity identifier, date range, security category, and organization.

#### Scenario: Administrator filters role changes
- **WHEN** an audit user filters audit records to role changes
- **THEN** the system returns matching security audit entries sorted by timestamp

### Requirement: Audit data integrity
The system SHALL prevent normal application users from editing or deleting audit entries.

#### Scenario: User attempts audit mutation
- **WHEN** any application user attempts to alter an audit entry through supported APIs
- **THEN** the system rejects the operation
