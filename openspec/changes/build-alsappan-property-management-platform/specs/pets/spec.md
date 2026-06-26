## ADDED Requirements

### Requirement: Pet registry
The system SHALL allow authorized users to create, view, update, archive, and restore pet records linked to residents and optionally properties or contracts.

#### Scenario: User registers a pet
- **WHEN** an authorized user submits pet data with owner resident, name, species, and authorization state
- **THEN** the system saves the pet and displays it in the Pets module and related resident/property views

### Requirement: Pet authorization state
The system SHALL track pet authorization states such as pending, authorized, denied, inactive, and archived.

#### Scenario: Pet is authorized
- **WHEN** an authorized user changes a pet status to authorized
- **THEN** the system records audit and timeline events and shows the localized status label

### Requirement: Pet documentation
The system SHALL allow documents such as vaccination records or authorization forms to be linked to pet records.

#### Scenario: Vaccination document is linked
- **WHEN** a user attaches a vaccination document to a pet
- **THEN** the system stores the link and makes it visible from pet and resident detail views when permitted

### Requirement: Pet filtering
The system SHALL allow users to search and filter pets by resident, property, species, authorization state, and active contract context.

#### Scenario: User filters by property
- **WHEN** the user filters pets by a property
- **THEN** the system returns pets associated with residents or contracts for that property
