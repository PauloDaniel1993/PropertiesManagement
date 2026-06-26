## ADDED Requirements

### Requirement: Vehicle registry
The system SHALL allow authorized users to create, view, update, archive, and restore vehicle records linked to residents and optionally properties or contracts.

#### Scenario: User registers a vehicle
- **WHEN** an authorized user submits vehicle data with owner resident, plate, type, and authorization state
- **THEN** the system saves the vehicle and displays it in the Veiculos module and related views

### Requirement: Vehicle parking allocation
The system SHALL support garage or parking allocation data including property garage context, parking space identifiers, and allocation notes.

#### Scenario: Vehicle receives parking space
- **WHEN** a user assigns a parking space to a vehicle
- **THEN** the system stores the allocation and shows it in vehicle and property detail views

### Requirement: Vehicle authorization state
The system SHALL track vehicle authorization states such as pending, authorized, denied, inactive, and archived.

#### Scenario: Vehicle authorization changes
- **WHEN** an authorized user changes vehicle authorization state
- **THEN** the system audits the change and shows the localized state in lists and details

### Requirement: Vehicle search and filtering
The system SHALL allow users to search and filter vehicles by plate, resident, property, vehicle type, parking allocation, and authorization state.

#### Scenario: User searches by plate
- **WHEN** the user enters a license plate value
- **THEN** the system returns matching vehicles according to normalized plate matching rules
