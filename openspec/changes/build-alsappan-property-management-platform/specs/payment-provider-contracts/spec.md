## ADDED Requirements

### Requirement: Payment provider abstraction
The system SHALL define payment provider interfaces for boleto, Pix, and PayPal without requiring real external integrations in the first implementation.

#### Scenario: Payment instruction is requested
- **WHEN** the system requests payment instructions for a supported mocked provider
- **THEN** the provider returns deterministic mocked instructions suitable for UI, testing, and future integration replacement

### Requirement: Mock boleto provider contract
The system SHALL provide a mocked boleto provider contract for generating boleto instructions, due date metadata, barcode/linha digitavel placeholders, and reconciliation references.

#### Scenario: Boleto mock is generated
- **WHEN** a charge requests boleto instructions
- **THEN** the system returns mocked boleto data linked to the charge and records provider metadata

### Requirement: Mock Pix provider contract
The system SHALL provide a mocked Pix provider contract for generating QR code payload placeholders, copy-and-paste codes, expiration metadata, and reconciliation references.

#### Scenario: Pix mock is generated
- **WHEN** a charge requests Pix instructions
- **THEN** the system returns mocked Pix data linked to the charge and records provider metadata

### Requirement: Mock PayPal provider contract
The system SHALL provide a mocked PayPal provider contract for creating payment intent placeholders, redirect URL placeholders, status polling, and reconciliation references.

#### Scenario: PayPal mock is generated
- **WHEN** a charge requests PayPal payment intent data
- **THEN** the system returns mocked PayPal intent data linked to the charge and records provider metadata

### Requirement: Provider event mapping
The system SHALL define a provider event mapping contract for future webhooks/callbacks while using mocked events in the first implementation.

#### Scenario: Mock provider event is processed
- **WHEN** a mocked provider event indicates payment settlement
- **THEN** the system maps the event to the charge and updates payment state through the same application path future real providers will use
