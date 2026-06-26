## ADDED Requirements

### Requirement: Resident record management
The system SHALL allow authorized users to create, view, update, archive, and restore resident records with personal, contact, identification, emergency contact, and notes fields.

#### Scenario: User creates a resident
- **WHEN** an authorized user submits a valid resident form
- **THEN** the system saves the resident and makes the resident available for contract association

### Requirement: Resident relationship view
The system SHALL show a resident's contracts, properties, payments, documents, pets, vehicles, occurrences, timeline entries, and audit access when permitted.

#### Scenario: User opens resident detail
- **WHEN** the user opens a resident detail page
- **THEN** the system displays related records grouped by module and filtered by permissions

### Requirement: Resident search and duplicate awareness
The system SHALL allow search by name, email, phone, document identifier, and active contract/property context and warn about likely duplicates during creation.

#### Scenario: Potential duplicate resident exists
- **WHEN** the user submits a resident with a matching document identifier or contact data
- **THEN** the system warns the user before saving according to duplicate policy

### Requirement: Resident privacy
The system SHALL protect sensitive resident identification and contact fields according to permissions.

#### Scenario: User lacks sensitive data permission
- **WHEN** the user views resident data without sensitive field permission
- **THEN** the system masks or omits protected fields
