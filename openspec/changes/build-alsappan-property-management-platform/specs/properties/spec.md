## ADDED Requirements

### Requirement: Property record management
The system SHALL allow authorized users to create, view, update, archive, and restore property records with identity, description, address, status, suggested rent, garage, and notes.

#### Scenario: User creates a property
- **WHEN** an authorized user submits a valid new property form
- **THEN** the system saves the property, audits the creation, and shows the property in the property list

### Requirement: Property search and filtering
The system SHALL allow users to search and filter properties by name, address, neighborhood, city, state, status, garage availability, and rent range.

#### Scenario: User searches by neighborhood
- **WHEN** the user enters a neighborhood in the property search field
- **THEN** the system returns matching properties with pagination and localized status labels

### Requirement: Property lifecycle status
The system SHALL track property status using canonical status codes and localized display labels.

#### Scenario: Contract activates for a property
- **WHEN** a contract for an available property becomes active
- **THEN** the system marks the property as rented unless another configured workflow prevents the transition

### Requirement: Property detail relationships
The system SHALL show related contracts, residents, payments, utility accounts, documents, pets, vehicles, occurrences, inspections, timeline entries, and audit access from the property detail view when permitted.

#### Scenario: User opens property detail
- **WHEN** the user views a property detail page
- **THEN** the system displays relationship sections only for modules the user is allowed to read
