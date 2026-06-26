## ADDED Requirements

### Requirement: Administrator user management
The system SHALL allow authorized administrators to create, invite, view, update, deactivate, and reactivate administrative users.

#### Scenario: Administrator invites user
- **WHEN** an authorized administrator submits an invitation with email and role
- **THEN** the system creates a pending user invitation and records the action in audit

### Requirement: Role assignment
The system SHALL allow authorized administrators to assign roles and effective permissions to administrative users.

#### Scenario: Role changes
- **WHEN** an authorized administrator changes another user's role
- **THEN** the system updates effective permissions and records a security audit event

### Requirement: Administrative access protection
The system SHALL restrict the Administradores menu and all administrator endpoints to users with explicit administrator permissions.

#### Scenario: Non-administrator opens Administradores
- **WHEN** a user without administrator read permission attempts to access the administrators route
- **THEN** the system blocks access and displays an authorization-safe response

### Requirement: User status lifecycle
The system SHALL track invited, active, inactive, locked, and archived administrative user states.

#### Scenario: User is deactivated
- **WHEN** an administrator deactivates a user
- **THEN** the system prevents new sessions for that user and preserves historical audit references
