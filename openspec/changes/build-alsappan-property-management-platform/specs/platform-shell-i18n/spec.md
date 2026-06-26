## ADDED Requirements

### Requirement: Authenticated application shell
The system SHALL provide an authenticated shell with sidebar navigation, top bar actions, active menu state, profile access, notification entry point, theme toggle, and protected content area.

#### Scenario: User opens an authenticated page
- **WHEN** an authenticated user navigates to any protected route
- **THEN** the system displays the sidebar, top bar, active menu item, and page content without requiring each module to rebuild the shell

#### Scenario: Unauthenticated user opens a protected page
- **WHEN** a user without a valid session navigates to a protected route
- **THEN** the system redirects the user to the login flow and preserves the intended destination when possible

### Requirement: Brazilian Portuguese default localization
The system SHALL use Brazilian Portuguese (`pt-BR`) as the default locale for labels, validation messages, dates, numbers, currency, pluralization, and status text.

#### Scenario: New user has no saved language preference
- **WHEN** the user opens the application for the first time
- **THEN** the system renders the user interface in `pt-BR`

### Requirement: Multiple language support
The system SHALL support additional locales through resource files and locale-aware formatting without changing module code.

#### Scenario: User changes language
- **WHEN** the user selects a supported language in profile or settings
- **THEN** the system persists the preference and renders navigation, forms, tables, validation, and messages in the selected language

### Requirement: Responsive operational layout
The system SHALL adapt the authenticated shell and module pages for desktop, tablet, and mobile viewports while preserving access to all menu items and primary actions.

#### Scenario: User opens the app on a narrow viewport
- **WHEN** the viewport cannot fit the full sidebar
- **THEN** the system provides a compact or collapsible navigation pattern without hiding protected functionality

### Requirement: Shared module page pattern
The system SHALL provide reusable page primitives for title, subtitle, primary action, search, filters, table, pagination, row actions, empty state, loading state, error state, and unauthorized state.

#### Scenario: Module list page loads
- **WHEN** a module route renders a list page
- **THEN** the system uses the shared page pattern so behavior and layout remain consistent across menu items
