## ADDED Requirements

### Requirement: Operational dashboard overview
The system SHALL provide a dashboard with localized operational indicators for occupancy, overdue payments, upcoming contract expirations, open occurrences, pending inspections, and recent activity.

#### Scenario: Dashboard loads with available data
- **WHEN** an authorized user opens the dashboard
- **THEN** the system displays current metrics derived from the user's permitted organization data

### Requirement: Dashboard empty state
The system SHALL display useful empty states when source modules do not yet have data.

#### Scenario: No properties exist
- **WHEN** the dashboard loads before any property is registered
- **THEN** the system shows zero-state metrics and a shortcut to create the first property

### Requirement: Dashboard deep links
The system SHALL allow users to navigate from dashboard widgets to the relevant filtered module view.

#### Scenario: User opens overdue payments widget
- **WHEN** the user activates the overdue payments widget
- **THEN** the system navigates to the payments page filtered to overdue charges

### Requirement: Dashboard permission awareness
The system SHALL hide or mask dashboard metrics for modules the user is not allowed to read.

#### Scenario: User lacks payment permission
- **WHEN** the user opens the dashboard without payment read permission
- **THEN** the system excludes payment values or shows an authorization-safe placeholder
