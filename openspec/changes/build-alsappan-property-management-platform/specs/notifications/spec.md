## ADDED Requirements

### Requirement: Notification center
The system SHALL provide an in-app notification center with unread counts, read/unread state, notification type, timestamp, message, and deep link target.

#### Scenario: User opens notifications
- **WHEN** an authenticated user opens the notification center
- **THEN** the system displays notifications relevant to that user sorted by recency

### Requirement: Event-triggered notifications
The system SHALL create notifications from configured domain events such as overdue payments, upcoming contract expirations, occurrence assignment, inspection scheduling, and document requests.

#### Scenario: Contract is nearing expiration
- **WHEN** a contract reaches the configured expiration warning window
- **THEN** the system creates notifications for users whose roles and preferences require them

### Requirement: Notification preferences
The system SHALL allow users to configure notification preferences by category and supported channel.

#### Scenario: User disables occurrence notifications
- **WHEN** a user disables a notification category
- **THEN** the system stops creating non-mandatory notifications for that category for the user

### Requirement: Notification localization
The system SHALL render notification messages using the recipient's locale while preserving canonical event data.

#### Scenario: User uses English locale
- **WHEN** an event creates a notification for a user with `en-US` preference
- **THEN** the system displays the notification message in English while preserving links and structured payload
