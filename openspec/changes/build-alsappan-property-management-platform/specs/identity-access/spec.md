## ADDED Requirements

### Requirement: User authentication
The system SHALL authenticate users with secure credentials, issue time-limited access tokens, maintain refresh sessions, and allow users to sign out.

#### Scenario: User signs in successfully
- **WHEN** a user submits valid credentials
- **THEN** the system creates an authenticated session and redirects the user to the default authenticated route

#### Scenario: User signs out
- **WHEN** an authenticated user signs out
- **THEN** the system invalidates the active refresh session and prevents further access using that session

### Requirement: Permission enforcement
The system SHALL enforce permissions on backend endpoints and use the same permission model to control frontend routes and actions.

#### Scenario: User lacks write permission
- **WHEN** a user without a module write permission attempts to create or update a record
- **THEN** the backend rejects the request and the frontend does not show the unavailable write action

### Requirement: Role-backed permissions
The system SHALL support roles that map to granular permissions for modules and sensitive actions.

#### Scenario: Administrator assigns a role
- **WHEN** an authorized administrator assigns a role to a user
- **THEN** the user's effective permissions update according to that role

### Requirement: Session security audit
The system SHALL audit security-sensitive session and access events.

#### Scenario: Failed login attempt occurs
- **WHEN** a login attempt fails
- **THEN** the system records an audit event with timestamp, user identifier when known, and request context
