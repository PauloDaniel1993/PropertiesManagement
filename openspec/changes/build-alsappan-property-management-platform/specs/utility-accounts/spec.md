## ADDED Requirements

### Requirement: Utility account management
The system SHALL allow authorized users to create, view, update, archive, and restore utility account records for electricity, water, gas, internet, condominium fees, IPTU, insurance, and other account types.

#### Scenario: User creates utility account record
- **WHEN** an authorized user submits utility account data with type, billing period, amount, due date, and responsible party
- **THEN** the system saves the record and shows it in the Contas de Consumo module

### Requirement: Utility responsibility model
The system SHALL support responsibility assignment to organization, property, contract, resident, owner placeholder, or other configured party.

#### Scenario: Utility belongs to active contract
- **WHEN** a utility account is assigned to an active contract
- **THEN** the system shows the linked property and primary resident context in the list and detail view

### Requirement: Utility payment state
The system SHALL track open, overdue, paid, cancelled, disputed, and archived utility account states.

#### Scenario: Utility is marked paid
- **WHEN** an authorized user marks a utility account as paid
- **THEN** the system records settlement metadata and updates timeline, audit, and dashboard data

### Requirement: Utility document links
The system SHALL allow utility account bills and receipts to be linked as documents.

#### Scenario: Bill PDF is uploaded
- **WHEN** a user uploads a bill PDF for a utility account
- **THEN** the system stores the document metadata and links it to the utility account
