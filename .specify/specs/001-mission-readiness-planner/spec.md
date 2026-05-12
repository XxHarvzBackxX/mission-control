# Feature Specification: Mission Readiness Planner

**Feature Branch**: `001-mission-readiness-planner`

**Created**: 2026-05-12

**Status**: Draft

**Input**: User description: "Create a Mission Readiness Planner for planning Kerbal Space Program (KSP) missions. Users can create missions with a name, target body, mission type, available delta-v, and required delta-v. The system should evaluate mission readiness based on mission constraints and resource margins. Missions can have three readiness states: Ready, At Risk, Not Ready. The system should generate warnings when: available delta-v is lower than required delta-v, reserve delta-v margin falls below 10%, required mission information is missing. Users should be able to view mission summaries and readiness results clearly."

**Context**: This planner is scoped to missions within Kerbal Space Program (KSP), a fictional space simulation game. Target bodies, mission types, and delta-v values are all drawn from KSP's in-game universe. The system is a web application with a responsive AngularTS frontend and a .NET REST API backend.

## Clarifications

### Session 2026-05-12

- Q: Should target body and mission type be free-text fields or predefined dropdown lists? → A: Hybrid — predefined KSP options with an "Other" free-text escape hatch, to support modded planetary systems.
- Q: Should mission names be unique across the system? → A: Yes — duplicate names are rejected with a validation error.
- Q: Should missions be persisted between sessions or in-memory only? → A: Persisted — local JSON file backend for initial PoC, with a planned migration path to SQL Server.
- Q: Can users edit or delete an existing mission after saving? → A: Yes — users can update any field or remove a mission entirely.
- Q: Should the low-margin warning fire independently on "Not Ready" missions, or only on "At Risk"? → A: Independent — both warnings can appear simultaneously. Warnings are displayed as small red indicator boxes attached to the mission.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create and Evaluate a Mission (Priority: P1)

A mission planner creates a new mission by providing a name, target body, mission type, available delta-v, and required delta-v. The system immediately evaluates and displays the mission's readiness state along with any active warnings.

**Why this priority**: This is the core workflow of the system. Without the ability to create a mission and receive a readiness evaluation, the planner has no value. Every other story depends on this one.

**Independent Test**: Can be fully tested by creating a single mission with known delta-v values and verifying the correct readiness state and warnings appear.

**Acceptance Scenarios**:

1. **Given** a user has provided a mission name, target body, mission type, available delta-v, and required delta-v where available delta-v exceeds required delta-v by at least 10%, **When** the mission is saved, **Then** the readiness state is displayed as "Ready" with no warnings.
2. **Given** a user has provided all required mission details where available delta-v meets or exceeds required delta-v but the reserve margin is less than 10%, **When** the mission is saved, **Then** the readiness state is displayed as "At Risk" with a warning about low reserve margin.
3. **Given** a user has provided all required mission details where available delta-v is less than required delta-v, **When** the mission is saved, **Then** the readiness state is displayed as "Not Ready" with a warning that available delta-v is insufficient.
4. **Given** a user has left one or more required fields blank, **When** the user attempts to save the mission, **Then** the system displays a warning identifying the missing information and does not save the mission.

---

### User Story 2 - View Mission Summary and Readiness Results (Priority: P2)

A mission planner views a detailed summary of a saved mission, including all entered parameters, the evaluated readiness state, and any active warnings — enabling a quick review before committing to launch.

**Why this priority**: Viewing results is the direct output of the core evaluation. Without clear presentation of the readiness state and warnings, users cannot act on the system's assessments.

**Independent Test**: Can be fully tested by opening a saved mission and verifying all parameters, readiness state, and warnings are displayed accurately and legibly.

**Acceptance Scenarios**:

1. **Given** a saved mission in "Ready" state, **When** the user views the mission summary, **Then** all mission parameters (name, target body, mission type, available delta-v, required delta-v), the readiness state "Ready", and zero warnings are displayed.
2. **Given** a saved mission in "At Risk" state, **When** the user views the mission summary, **Then** the readiness state "At Risk" and the low reserve margin warning are both prominently displayed alongside mission parameters.
3. **Given** a saved mission in "Not Ready" state, **When** the user views the mission summary, **Then** the readiness state "Not Ready" and the insufficient delta-v warning are both prominently displayed alongside mission parameters.

---

### User Story 3 - View All Missions with Readiness States (Priority: P3)

A mission planner views a list of all created missions, each showing its name and readiness state, enabling quick identification of which missions are ready for launch.

**Why this priority**: Once multiple missions exist, users need an overview to prioritize and manage them. This builds on P1 and P2 but is not required to deliver core value.

**Independent Test**: Can be fully tested by creating two or more missions with different readiness states and verifying the list displays each mission's name and correct readiness state.

**Acceptance Scenarios**:

1. **Given** multiple missions exist in different readiness states, **When** the user views the mission list, **Then** each mission is shown with its name and readiness state (Ready, At Risk, or Not Ready).
2. **Given** no missions have been created, **When** the user views the mission list, **Then** an empty state message is shown indicating no missions have been created yet.

---

### User Story 4 - Edit and Delete a Mission (Priority: P3)

A mission planner updates the parameters of an existing mission (e.g., correcting a delta-v value) or removes a mission entirely. The system re-evaluates readiness immediately after any edit.

**Why this priority**: Editing and deletion are essential for a practical planning tool but depend on mission creation (P1) being in place. They deliver lower incremental value than viewing results.

**Independent Test**: Can be fully tested by editing a saved mission's delta-v values, verifying the readiness state updates correctly, then deleting the mission and confirming it no longer appears in the list.

**Acceptance Scenarios**:

1. **Given** a saved mission, **When** the user edits any field and saves, **Then** the mission is updated and readiness is re-evaluated and displayed immediately.
2. **Given** a user edits a mission's name to one already used by another mission, **When** the user attempts to save, **Then** a validation error is shown and the mission is not updated.
3. **Given** a saved mission, **When** the user deletes it, **Then** the mission is removed from the list and is no longer accessible.

---

### Edge Cases

- What happens when available delta-v exactly equals required delta-v (0% reserve margin)? → Treated as "At Risk" because the reserve falls below the 10% threshold, and a warning is generated.
- What happens when required delta-v is zero? → Treated as a missing or invalid required field; a warning is generated and the mission cannot be saved.
- What happens when delta-v values are entered as negative numbers? → Treated as invalid input; a warning is generated identifying the missing or invalid information.
- What happens when no missions have been created yet? → The mission list displays an empty state message.
- What happens when a user edits a mission name to match an existing mission? → The system rejects the change with a validation error; the original name is preserved.

## Non-Functional Requirements

- **NFR-001**: The frontend MUST be implemented as a responsive web application using Angular with TypeScript, accessible on both desktop and tablet screen sizes.
- **NFR-002**: The backend MUST expose a REST API implemented in .NET. Mission data MUST be persisted to a local JSON file store for the initial PoC, designed for a future migration to SQL Server without requiring API contract changes.
- **NFR-003**: Mission readiness calculations MUST complete instantaneously — no perceptible delay following user input.
- **NFR-004**: The application MUST function as a single-user planning tool; no authentication or multi-user access is required.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow users to create a mission with the following fields: name, target body, mission type, available delta-v, and required delta-v. Target body and mission type MUST be selectable from a predefined list of KSP-specific options and MUST also allow a free-text "Other" value to support modded content.
- **FR-002**: System MUST evaluate mission readiness as "Ready" when the available delta-v exceeds the required delta-v by a reserve margin of 10% or more.
- **FR-003**: System MUST evaluate mission readiness as "At Risk" when the available delta-v meets or exceeds the required delta-v but the reserve margin is less than 10%.
- **FR-004**: System MUST evaluate mission readiness as "Not Ready" when the available delta-v is less than the required delta-v.
- **FR-005**: System MUST generate a warning when the available delta-v is lower than the required delta-v. This warning MUST appear regardless of whether a low-margin warning is also active.
- **FR-006**: System MUST generate a warning when the reserve delta-v margin falls below 10%. This warning MUST appear regardless of readiness state — including on "Not Ready" missions — and independently of FR-005.
- **FR-007**: System MUST generate a warning when any required mission field (name, target body, mission type, available delta-v, required delta-v) is missing or invalid.
- **FR-008**: System MUST prevent a mission from being saved when any required field is missing or invalid.
- **FR-009**: System MUST display a mission summary showing: mission name, target body, mission type, available delta-v, required delta-v, readiness state, and any active warnings.
- **FR-010**: System MUST display a list of all saved missions, showing each mission's name and current readiness state.
- **FR-011**: System MUST reject a mission name that duplicates an existing mission name and display a validation error identifying the conflict.
- **FR-012**: System MUST allow users to edit any field of a saved mission. Readiness state and warnings MUST be re-evaluated immediately upon saving the edit.
- **FR-013**: System MUST allow users to delete a saved mission. The deleted mission MUST be removed from the mission list immediately.
- **FR-014**: Active warnings MUST be displayed as visually distinct red indicator boxes attached to the mission in both the mission list and mission summary views.

### Key Entities

- **Mission**: Represents a planned space mission. Key attributes: name (text), target body (predefined KSP option or free-text "Other"), mission type (predefined KSP option or free-text "Other"), available delta-v (numeric, m/s), required delta-v (numeric, m/s), readiness state, warnings.
- **Readiness State**: A derived classification of a mission's viability — one of: Ready, At Risk, Not Ready. Calculated from the relationship between available and required delta-v.
- **Warning**: A message associated with a mission that identifies a constraint violation (insufficient delta-v, low reserve margin) or missing required information. Multiple warnings can be active simultaneously on a single mission. Warnings are rendered as red indicator boxes attached to the mission in the UI.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a complete mission and view its readiness state in under 1 minute from starting the creation process.
- **SC-002**: Readiness state and warnings are displayed immediately upon saving mission parameters, with no perceptible delay.
- **SC-003**: Every mission where available delta-v is below required delta-v displays a "Not Ready" readiness state and the corresponding warning — 100% accuracy.
- **SC-004**: Every mission where the reserve margin is below 10% displays the low-margin warning as a red indicator box — regardless of whether the mission is "At Risk" or "Not Ready" — 100% accuracy.
- **SC-005**: Users can identify the readiness state of each mission at a glance from the mission list without opening individual mission summaries.
- **SC-006**: Every attempt to save a mission with missing required information results in a warning identifying the missing fields — 100% of the time.

## Assumptions

- Reserve delta-v margin is calculated as: `(available delta-v − required delta-v) / required delta-v × 100%`.
- Delta-v values are entered and displayed in meters per second (m/s), consistent with KSP conventions.
- Target body is selected from a predefined list of KSP stock celestial destinations (e.g., Kerbin, Mun, Minmus, Duna, Eve, Moho, Jool, Eeloo) with an "Other" free-text option to support modded planetary systems.
- Mission type is selected from a predefined list of KSP mission categories (e.g., Orbital, Landing, Flyby, Transfer, Rescue) with an "Other" free-text option.
- Mission names are unique within the system; the system rejects duplicate names with a validation error.
- The system is a single-user planning tool; multi-user collaboration and authentication are out of scope.
- Missions are persisted between sessions using a local JSON file store. This is an intentional PoC-stage decision; the storage layer is expected to be migrated to SQL Server in a future iteration. The REST API contract MUST NOT expose storage implementation details.
- Readiness evaluation is computed entirely from the delta-v values provided; no external orbital mechanics data or calculations are required.
- The frontend is responsive and accessible on desktop and tablet; phone-sized viewports are out of scope.
