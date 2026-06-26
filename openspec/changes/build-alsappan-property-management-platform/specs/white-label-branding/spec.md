## ADDED Requirements

### Requirement: Default brand theme
The system SHALL ship with a polished default design for typography, spacing, color tokens, component states, icons, shell layout, and status styling.

#### Scenario: Organization has no custom branding
- **WHEN** a user opens the application for an organization without custom branding
- **THEN** the system renders the default Alsappan design

### Requirement: Organization branding
The system SHALL allow each organization to configure white-label branding metadata such as display name, logo, primary color, accent color, and optional support/contact metadata.

#### Scenario: Organization has custom branding
- **WHEN** a user opens the application for an organization with branding configured
- **THEN** the system applies that organization's branding without changing domain behavior or permissions

### Requirement: Branding safety
The system SHALL validate tenant brand colors and assets to preserve accessibility, contrast, layout stability, and file safety.

#### Scenario: Low-contrast color is submitted
- **WHEN** an administrator submits a brand color combination that does not meet the configured contrast threshold
- **THEN** the system rejects the configuration with a localized validation message

### Requirement: Portal and admin consistency
The system SHALL apply organization branding consistently across the administrative shell and resident portal while preserving role-appropriate navigation.

#### Scenario: Resident opens branded portal
- **WHEN** a resident opens the portal for a branded organization
- **THEN** the portal uses the organization's branding and still shows only resident-permitted navigation
