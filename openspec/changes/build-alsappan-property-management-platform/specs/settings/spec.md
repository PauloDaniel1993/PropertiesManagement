## ADDED Requirements

### Requirement: Organization settings
The system SHALL allow authorized users to manage organization profile settings such as display name, locale defaults, timezone, contact information, and branding metadata.

#### Scenario: Administrator updates organization name
- **WHEN** an authorized administrator updates the organization display name
- **THEN** the system saves the setting, audits the change, and uses the new name where applicable

### Requirement: Localization settings
The system SHALL allow authorized users to configure supported locales and default locale behavior.

#### Scenario: Administrator enables locale
- **WHEN** an administrator enables a supported locale
- **THEN** the system makes that locale selectable by users without changing stored domain data

### Requirement: Domain catalog settings
The system SHALL support configurable domain catalogs for non-critical labels such as property types, occurrence types, inspection types, document categories, and utility account types.

#### Scenario: Administrator adds occurrence type
- **WHEN** an administrator adds a new occurrence type with localized labels
- **THEN** the system makes the type available in occurrence forms and filters

### Requirement: Security and notification settings
The system SHALL expose administrator-controlled settings for session policy, MFA requirement, password rules, notification categories, and delivery channels as supported by the platform.

#### Scenario: Administrator changes session timeout
- **WHEN** an authorized administrator updates session timeout settings
- **THEN** the system applies the policy to future session validation and records an audit event
