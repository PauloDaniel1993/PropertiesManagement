## ADDED Requirements

### Requirement: Cross-entity timeline
The system SHALL provide a chronological timeline of meaningful business events across properties, contracts, residents, payments, documents, occurrences, inspections, pets, vehicles, and users.

#### Scenario: User opens global timeline
- **WHEN** an authorized user opens Timeline
- **THEN** the system displays events the user is permitted to see in reverse chronological order

### Requirement: Entity-specific timeline
The system SHALL expose timeline sections on entity detail pages for events related to that entity.

#### Scenario: User opens property timeline
- **WHEN** the user opens a property detail page
- **THEN** the system displays timeline events associated with the property and its relevant linked records according to permissions

### Requirement: Timeline filtering
The system SHALL allow users to filter timeline entries by entity type, event type, actor, date range, and related entity.

#### Scenario: User filters by event type
- **WHEN** the user filters timeline by document uploads
- **THEN** the system returns matching timeline entries with localized event labels

### Requirement: Timeline deep links
The system SHALL link timeline entries to the source record when the user has permission to view it.

#### Scenario: User clicks timeline entry
- **WHEN** the user activates a timeline entry for an occurrence
- **THEN** the system navigates to the occurrence detail or shows an authorization-safe denial
