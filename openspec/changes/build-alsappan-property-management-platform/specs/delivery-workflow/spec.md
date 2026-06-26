## ADDED Requirements

### Requirement: Git Flow branch model
The project SHALL use Git Flow with `main` for production-ready releases, `develop` for integrated development, `feature/*` for feature work, `release/*` for release stabilization, and `hotfix/*` for urgent production fixes.

#### Scenario: New feature work starts
- **WHEN** implementation starts for a new feature or functionality
- **THEN** the work is performed on a `feature/*` branch created from `develop`

### Requirement: Pull request review gate
The project SHALL open a pull request for each completed feature or functionality and SHALL wait for review before merging.

#### Scenario: Feature is completed
- **WHEN** a feature branch is ready for review
- **THEN** a pull request targets `develop` and the branch is not merged until review is complete

### Requirement: Release branch flow
The project SHALL use `release/*` branches from `develop` when preparing a version for production.

#### Scenario: Release is prepared
- **WHEN** a set of reviewed features is ready for production hardening
- **THEN** a `release/*` branch is created from `develop` for final fixes, validation, and release notes

### Requirement: Hotfix branch flow
The project SHALL use `hotfix/*` branches from `main` for urgent production fixes and merge completed hotfixes back into both `main` and `develop`.

#### Scenario: Production bug requires urgent fix
- **WHEN** an urgent production issue must be fixed outside the normal release cycle
- **THEN** the fix is made on a `hotfix/*` branch from `main` and reconciled into `develop`
