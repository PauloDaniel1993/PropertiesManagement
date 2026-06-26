## ADDED Requirements

### Requirement: Lease contract management
The system SHALL allow authorized users to create, view, update, archive, and restore lease contracts linked to one property and one or more residents.

#### Scenario: User creates a contract
- **WHEN** an authorized user selects a property, primary resident, dates, rent terms, and valid required fields
- **THEN** the system saves the contract in draft or active status according to the submitted lifecycle action

### Requirement: Contract lifecycle
The system SHALL support draft, active, ending soon, ended, terminated, cancelled, and archived contract states.

#### Scenario: Contract reaches end date
- **WHEN** an active contract is near or past its configured end date
- **THEN** the system exposes the appropriate ending or ended state in lists, details, dashboard, and notifications

### Requirement: Contract financial terms
The system SHALL store rent amount, currency, due day, deposit information, adjustment information, penalty/discount notes, and payment generation settings.

#### Scenario: User views contract terms
- **WHEN** a user opens a contract detail page
- **THEN** the system displays financial terms using the user's locale and permission constraints

### Requirement: Contract document links
The system SHALL allow documents to be attached or linked to contracts with access controlled by contract and document permissions.

#### Scenario: User links signed contract document
- **WHEN** an authorized user uploads or selects a document for a contract
- **THEN** the system links the document to the contract and records timeline and audit events
