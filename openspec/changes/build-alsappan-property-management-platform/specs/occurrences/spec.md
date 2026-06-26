## ADDED Requirements

### Requirement: Occurrence management
The system SHALL allow authorized users to create, view, update, archive, and restore occurrences linked to properties, residents, contracts, documents, or administrators.

#### Scenario: User creates occurrence
- **WHEN** an authorized user submits an occurrence with type, priority, description, and related entity context
- **THEN** the system saves the occurrence with an initial status and records timeline and audit events

### Requirement: Occurrence workflow
The system SHALL support open, assigned, in progress, waiting, resolved, cancelled, and archived occurrence states.

#### Scenario: Occurrence is resolved
- **WHEN** an authorized user marks an occurrence as resolved with resolution notes
- **THEN** the system records the resolution, updates status, and exposes the change in dashboard and timeline

### Requirement: Occurrence collaboration
The system SHALL support comments, assignments, attachments, priority changes, and status history for occurrences.

#### Scenario: User adds occurrence comment
- **WHEN** a permitted user adds a comment to an occurrence
- **THEN** the system stores the comment, updates the activity history, and notifies relevant users according to preferences

### Requirement: Occurrence filtering
The system SHALL allow users to search and filter occurrences by type, priority, status, assigned user, property, resident, date range, and unresolved state.

#### Scenario: User filters high priority open occurrences
- **WHEN** the user selects high priority and open filters
- **THEN** the system returns only matching occurrences sorted by urgency and date
