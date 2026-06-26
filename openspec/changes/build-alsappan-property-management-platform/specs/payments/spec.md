## ADDED Requirements

### Requirement: Charge management
The system SHALL allow authorized users to create, view, update, cancel, and archive charges linked to contracts, properties, residents, or utility accounts.

#### Scenario: User creates a rent charge
- **WHEN** an authorized user creates a charge for an active contract
- **THEN** the system saves the charge with due date, amount, currency, status, and linked entities

### Requirement: Payment status tracking
The system SHALL track pending, overdue, partially paid, paid, cancelled, disputed, and archived payment states.

#### Scenario: Charge passes due date unpaid
- **WHEN** a pending charge passes its due date without full settlement
- **THEN** the system marks or presents the charge as overdue and exposes it to dashboard and notifications

### Requirement: Partial and full settlement
The system SHALL support payment transactions that can partially or fully settle a charge.

#### Scenario: Partial payment is recorded
- **WHEN** a user records a transaction lower than the open charge balance
- **THEN** the system updates the charge to partially paid and retains the remaining balance

### Requirement: Receipt and reconciliation metadata
The system SHALL allow payment records to store receipt documents, payment method, settlement date, bank/reference data, discounts, penalties, and notes.

#### Scenario: User attaches receipt
- **WHEN** an authorized user attaches a receipt document to a payment
- **THEN** the system links the document and records audit and timeline events
