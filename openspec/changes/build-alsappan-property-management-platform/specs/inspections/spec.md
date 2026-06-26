## ADDED Requirements

### Requirement: Inspection scheduling
The system SHALL allow authorized users to schedule inspections linked to properties, contracts, residents, and assigned administrators.

#### Scenario: User schedules inspection
- **WHEN** an authorized user submits inspection type, property, scheduled date, and assignee
- **THEN** the system saves the inspection and makes it visible in Vistorias, property detail, dashboard, and timeline

### Requirement: Inspection checklist
The system SHALL allow inspections to capture structured checklist items, condition ratings, observations, photos, documents, and room/area context.

#### Scenario: User records checklist item
- **WHEN** a user adds a condition rating and observation to an inspection item
- **THEN** the system saves the item and updates inspection completion progress

### Requirement: Inspection lifecycle
The system SHALL support scheduled, in progress, completed, cancelled, and archived inspection states.

#### Scenario: Inspection is completed
- **WHEN** all required inspection fields are complete and the user completes the inspection
- **THEN** the system locks completion metadata, records audit and timeline events, and updates dashboard indicators

### Requirement: Inspection report readiness
The system SHALL store inspection data in a report-ready structure even before PDF generation is implemented.

#### Scenario: User views completed inspection
- **WHEN** the user opens a completed inspection
- **THEN** the system displays a complete report view using structured checklist data and attachments
