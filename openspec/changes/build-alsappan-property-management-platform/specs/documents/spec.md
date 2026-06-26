## ADDED Requirements

### Requirement: Document catalog
The system SHALL provide a document catalog where authorized users can upload, view, download, classify, archive, and restore documents.

#### Scenario: User uploads document
- **WHEN** an authorized user uploads an allowed file with required metadata
- **THEN** the system stores the file, saves metadata, audits the upload, and displays the document in the catalog

### Requirement: Document entity links
The system SHALL allow a document to be linked to one or more supported entities, including properties, contracts, residents, payments, utility accounts, pets, vehicles, occurrences, and inspections.

#### Scenario: Document links to multiple entities
- **WHEN** a user links one document to a property and a contract
- **THEN** the system displays the document from both entity detail views when the user has permission

### Requirement: Document access control
The system SHALL enforce permissions for document listing, preview, download, upload, update, archive, and delete-like actions.

#### Scenario: User lacks document download permission
- **WHEN** the user attempts to download a protected document
- **THEN** the backend rejects the request and no file URL is exposed

### Requirement: Document versioning
The system SHALL support version metadata for documents so replacements preserve history.

#### Scenario: User uploads new version
- **WHEN** an authorized user uploads a new version of an existing document
- **THEN** the system records the new version while preserving access to prior version metadata
