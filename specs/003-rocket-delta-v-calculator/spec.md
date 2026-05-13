# Feature Specification: Rocket-Based Mission Delta-V Calculator

**Feature Branch**: `003-rocket-delta-v-calculator`

**Created**: 2026-05-13

**Status**: Draft

**Input**: User description: "Expand Mission Control from a manual readiness checker into a practical KSP-inspired mission calculator. Users should be able to define reusable rockets, build those rockets from stages and stock KSP-style parts, assign a rocket to a mission, and calculate whether that rocket has enough estimated delta-v to complete the planned mission."

## User Scenarios & Testing *(mandatory)*

<!--
  Stories are ordered as independently deliverable MVP slices.
  P1 = core value; can be demonstrated and tested alone.
  P2 = closes the loop between the builder and the existing mission system.
  P3 = transparency and usability.
  P4 = niche/modded scenarios.
-->

### User Story 1 - Build a Rocket and See Estimated Delta-V (Priority: P1)

A mission planner creates a named rocket, divides it into stages, adds stock KSP-style parts to each stage, and immediately sees per-stage and total estimated delta-v.

**Why this priority**: This is the core value proposition of the entire feature. A user should be able to go from "empty rocket" to "estimated delta-v number" without involving any mission. If this story alone were shipped, users would already have a meaningful planning tool.

**Independent Test**: A user can open the Rocket Library, create a new rocket, add one stage with a fuel tank and a liquid engine, and see a delta-v figure displayed — with no mission required.

**Acceptance Scenarios**:

1. **Given** a rocket with one stage containing a liquid engine and a compatible fuel tank, **When** the rocket is saved, **Then** the system displays estimated delta-v for that stage and a total.
2. **Given** a rocket with two sequential stages each with a fuel tank and engine, **When** the rocket is saved, **Then** per-stage delta-v is shown and total delta-v is the sum across all stages, accounting for mass jettisoned by the first stage.
3. **Given** a rocket with a stage that has no engine, **When** the user views the rocket, **Then** a "No Engine" warning is displayed and delta-v is shown as zero or indeterminate for that stage.
4. **Given** a rocket with an engine but no compatible fuel in its stage, **When** the user views the rocket, **Then** a "No Fuel Source" warning is displayed.
5. **Given** a rocket with no command part or probe core, **When** the user views the rocket, **Then** a "No Command Part" warning is displayed.
6. **Given** the user marks a stage as "discarded after burn", **When** delta-v is calculated for subsequent stages, **Then** the discarded stage's dry mass is excluded from the wet mass of later stages.

---

### User Story 2 - Assign a Rocket to a Mission (Priority: P2)

A mission planner assigns a saved rocket to an existing mission so that mission readiness is driven by the rocket's calculated delta-v rather than a manually entered value.

**Why this priority**: This closes the loop between the new rocket builder and the existing readiness system. Once a rocket is assigned, the calculator becomes the source of truth for readiness, replacing manual delta-v entry.

**Independent Test**: A user can open an existing mission, pick a rocket from the Rocket Library, and see the readiness state update automatically based on calculated available delta-v vs. estimated required delta-v.

**Acceptance Scenarios**:

1. **Given** a mission with a launch body and destination selected and a valid rocket assigned, **When** the mission is viewed, **Then** readiness is calculated from the rocket's available delta-v vs. estimated required delta-v.
2. **Given** available delta-v is lower than required delta-v, **When** readiness is evaluated, **Then** the mission is marked Not Ready with an "Insufficient Delta-V" warning.
3. **Given** available delta-v exceeds required delta-v but falls within the configured safety margin, **When** readiness is evaluated, **Then** the mission is marked At Risk with a "Low Delta-V Margin" warning.
4. **Given** available delta-v exceeds required delta-v by more than the configured safety margin, **When** readiness is evaluated, **Then** the mission is marked Ready.
5. **Given** a mission has no rocket assigned, **When** the mission is viewed, **Then** the user is offered a choice to assign a rocket or continue entering delta-v manually — both paths remain valid.
6. **Given** a rocket assigned to a mission is deleted, **When** the mission is viewed, **Then** the mission clearly indicates the rocket is missing and falls back to Not Ready pending re-assignment or manual entry.
7. **Given** a user edits a rocket that is assigned to multiple missions, **When** the missions are next viewed, **Then** all affected missions reflect the updated rocket calculation.

---

### User Story 3 - Browse and Select Parts from the Catalogue (Priority: P2)

A mission planner browses a catalogue of stock KSP-style parts, filters by category, searches by name, and adds parts to rocket stages — with mass, fuel capacity, and engine statistics automatically populated from part data.

**Why this priority**: Without a seeded part catalogue, users would need to enter all values manually, undermining the purpose of the calculator. The catalogue is what grounds the estimate in realistic KSP values.

**Independent Test**: A user can open the part picker in the rocket builder, filter by "Engines", select an engine, and see it added to the current stage with its Isp and thrust pre-filled from catalogue data.

**Acceptance Scenarios**:

1. **Given** the part picker is open, **When** the user filters by a category (e.g., "Engines"), **Then** only parts of that category are displayed.
2. **Given** the part picker is open, **When** the user types a partial part name, **Then** matching parts are shown in real time.
3. **Given** the user selects a fuel tank from the catalogue, **When** it is added to a stage, **Then** the stage's fuel capacity and wet/dry mass update automatically.
4. **Given** the user selects an engine from the catalogue, **When** it is added to a stage, **Then** the stage's Isp (sea-level and vacuum) and thrust values are sourced from the part data.
5. **Given** a part has been added to a stage, **When** the user removes it, **Then** the stage mass and delta-v recalculate accordingly.

---

### User Story 4 - View Calculation Breakdown (Priority: P3)

A mission planner expands a calculation breakdown to see exactly how available and required delta-v were derived, including per-stage values, the Isp used, efficiency factors, and estimation method.

**Why this priority**: Without transparency, users cannot diagnose a failing plan or understand what to change. Calculation visibility turns a black box into a planning tool.

**Independent Test**: A user with a rocket assigned to a mission can expand the calculation breakdown and see wet mass, dry mass, Isp, efficiency factor, and delta-v for each stage, plus the required delta-v components.

**Acceptance Scenarios**:

1. **Given** a rocket with two stages is assigned to a mission, **When** the user expands the calculation breakdown, **Then** wet mass, dry mass, Isp, applied efficiency factor, and delta-v are shown per stage.
2. **Given** an atmospheric launch, **When** the breakdown is viewed, **Then** the atmospheric efficiency multiplier and its delta-v impact are explicitly listed.
3. **Given** a required delta-v estimate has been produced, **When** the breakdown is viewed, **Then** the data sources contributing to the estimate (launch body, target, gravity, atmosphere) are listed.
4. **Given** a required delta-v override is active, **When** the breakdown is viewed, **Then** the override is flagged as user-entered and distinguishable from a system estimate.
5. **Given** any calculated value is an approximation, **When** it appears in the breakdown, **Then** it is marked to indicate it is an estimate, not an exact value.

---

### User Story 5 - Plan a Mission Using a Custom Celestial Body (Priority: P4)

A mission planner selects "Other" as the launch or target body and enters custom parameters so that they can roughly estimate delta-v for non-stock or modded scenarios.

**Why this priority**: This serves users running modded KSP systems with planets not in the stock Kerbol system. It is lower priority because the stock body set covers the majority of use cases.

**Independent Test**: A user can select "Other" for the launch body, enter a body name, radius, surface gravity, and atmosphere height, and receive a delta-v estimate with a "Custom Body Approximation" warning — without any mission assignment.

**Acceptance Scenarios**:

1. **Given** the user selects "Other" as the launch body and provides required parameters (name, radius, surface gravity), **When** the calculation runs, **Then** the custom values are used in place of any stock preset.
2. **Given** a custom body is active, **When** the mission or rocket summary is viewed, **Then** a "Custom Body Approximation" warning is displayed.
3. **Given** the user selects "Other" but omits a required field (radius or surface gravity), **When** they attempt to calculate, **Then** validation errors are shown and no calculation is performed.
4. **Given** the user enters no atmosphere height for a custom body, **When** the system calculates, **Then** the body is treated as airless and no atmospheric efficiency loss is applied.

---

### Edge Cases

- Rocket has stages with no parts added.
- Rocket has fuel tanks but no engine in any stage.
- Rocket has an engine with no compatible fuel in its stage.
- Rocket has multiple engines of different fuel types in one stage.
- Rocket launches from a body with no atmosphere (vacuum launch — no drag loss applied).
- Rocket launches from a high-gravity atmospheric body (e.g., Kerbin at maximum surface pressure).
- User assigns a rocket to a mission, then deletes the rocket.
- User edits a rocket that is assigned to multiple missions.
- User overrides required delta-v on a mission after assigning a rocket.
- Safety margin is set to 0%.
- Rocket has a single stage with no decoupler (no staging, entire rocket is payload + propulsion).
- User selects "Other" as launch body but leaves atmosphere pressure blank.
- Available and required delta-v are exactly equal (boundary: Not Ready or At Risk?).

## Requirements *(mandatory)*

### Functional Requirements

#### Rocket Management

- **FR-001**: Users MUST be able to create, edit, and delete rockets.
- **FR-002**: Users MUST be able to view all saved rockets in a Rocket Library.
- **FR-003**: A rocket summary MUST display total estimated delta-v, stage count, total mass, and any active validation warnings.
- **FR-004**: A rocket MUST be assignable to one or more missions.
- **FR-005**: Deleting a rocket that is assigned to one or more missions MUST require explicit confirmation and display how many missions will be affected; affected missions MUST show a missing-rocket warning until re-assigned or manually updated.

#### Stage Management

- **FR-006**: Users MUST be able to add, rename, reorder, and remove stages within a rocket.
- **FR-007**: Users MUST be able to specify, per stage, whether it is discarded after its burn (jettisoned) or remains attached.
- **FR-008**: Each stage MUST independently display its estimated delta-v, total mass, and fuel mass.

#### Part Catalogue

- **FR-009**: The system MUST provide a seeded catalogue of stock KSP-style parts covering at minimum: command pods, probe cores, liquid fuel tanks, liquid fuel engines, solid boosters, and radial decouplers.
- **FR-010**: The catalogue MUST be filterable by part category.
- **FR-011**: The catalogue MUST be searchable by part name.
- **FR-012**: Engine parts MUST expose thrust (sea-level and vacuum) and specific impulse — Isp — (sea-level and vacuum).
- **FR-013**: Fuel tank parts MUST expose dry mass, wet mass, and fuel capacity by fuel type.
- **FR-014**: The catalogue is read-only in v1; users cannot create or modify catalogue parts.

#### Delta-V Calculation

- **FR-015**: The system MUST calculate per-stage delta-v using the Tsiolkovsky rocket equation: `ΔV = Isp × g₀ × ln(m_wet / m_dry)`, where g₀ is standard gravity (9.80665 m/s²).
- **FR-016**: Each stage's dry mass calculation MUST account for mass jettisoned by previously burned and discarded stages.
- **FR-017**: The system MUST use sea-level Isp for launches from atmospheric bodies and vacuum Isp for airless bodies.
- **FR-018**: For atmospheric launches, the system MUST apply a configurable efficiency multiplier (default 85%) to available delta-v to account for drag, gravity losses, and ascent profile imperfection.
- **FR-019**: Vacuum launches MUST NOT apply an atmospheric efficiency multiplier unless explicitly configured.
- **FR-020**: The system MUST display per-stage delta-v and summed total delta-v for the full rocket.
- **FR-021**: The system MUST display warnings when calculation confidence is reduced (e.g., mixed fuel types, custom body parameters).

#### Required Delta-V Estimation

- **FR-022**: The system MUST estimate required mission delta-v based on the selected launch body and target destination.
- **FR-023**: Required delta-v estimation MUST account for launch body surface gravity and atmosphere presence.
- **FR-024**: The system MUST clearly label each required delta-v figure as: calculated (formula-derived), approximated (reference data), or overridden (user-entered).
- **FR-025**: Users MUST be able to manually override the required delta-v estimate with their own value.
- **FR-026**: A manual override MUST trigger a "Manual Override Applied" warning visible in the mission summary and breakdown.
- **FR-027**: Required delta-v MUST be calculated dynamically from celestial body parameters using orbital mechanics formulas. Specifically: circular orbital velocity for the launch body is derived from its radius and surface gravity; ascent-to-orbit cost accounts for atmospheric drag losses where applicable; transfer delta-v to the target body is estimated using simplified orbital transfer calculations (e.g., Hohmann transfer approximation). This approach naturally supports both stock and custom bodies without hardcoded route costs. Stock body data is provided by the seeded Kerbol System dataset.

#### Custom Celestial Bodies

- **FR-028**: Users MUST be able to select "Other" as a launch body or target body in the mission calculation profile.
- **FR-029**: When "Other" is selected, users MUST be able to enter: body name, surface gravity (m/s²), equatorial radius (km), atmosphere presence, surface pressure (atm), and atmosphere height (km).
- **FR-030**: Custom body calculations MUST display a "Custom Body Approximation" warning.
- **FR-031**: Custom body entries MUST be validated: radius and surface gravity MUST be greater than zero; surface pressure MUST be non-negative.

#### Readiness Integration

- **FR-032**: When a valid rocket is assigned to a mission, available delta-v MUST be sourced from the calculated rocket delta-v — not from a manual entry field.
- **FR-033**: Required delta-v MUST be sourced from the mission calculation profile (estimated, approximated, or overridden).
- **FR-034**: Existing readiness states (Ready, At Risk, Not Ready) MUST continue to apply with the same thresholds; only the input source changes.
- **FR-035**: A mission with no rocket assigned MUST remain operable using manually entered delta-v, preserving backwards compatibility with existing missions.

#### Warnings

- **FR-036**: The system MUST generate the following warnings as applicable:
  - **Insufficient Delta-V**: Available delta-v is lower than required delta-v.
  - **Low Delta-V Margin**: Available delta-v exceeds required but is below the configured safety margin.
  - **Atmospheric Loss Applied**: Drag/efficiency multiplier was applied for an atmospheric launch.
  - **Unstable Craft Assumption**: Centre-of-mass and centre-of-drag stability are assumed, not calculated.
  - **No Command Part**: Rocket contains no command pod or probe core.
  - **No Engine**: Rocket contains no engine part.
  - **No Fuel Source**: Rocket has engines but no compatible fuel in any engine stage.
  - **Mixed Fuel Uncertainty**: A stage contains engines and fuels that cannot be confidently matched.
  - **Custom Body Approximation**: Custom body data is user-entered and unverified.
  - **Manual Override Applied**: User has replaced the system required delta-v estimate with their own value.

#### Staging Model

- **FR-037**: v1 MUST support sequential staging only: each stage fires independently in sequence, and a stage's dry mass is calculated after all previously discarded stages have been jettisoned. Asparagus and parallel staging (simultaneous stage firing with crossfeed fuel lines) are explicitly deferred to v2. The data model MUST be designed to accommodate parallel staging as a future extension without requiring a schema migration; this constraint MUST be documented in the planning phase.

### Key Entities

- **Rocket**: A reusable launch vehicle with a unique name, description, ordered list of stages, and optional notes. Assignable to multiple missions.
- **Stage**: One section of a rocket with an ordered list of parts, a stage number, and a flag indicating whether it is jettisoned after its burn.
- **Part**: A catalogue item representing a KSP-style craft component. Has a category, dry mass, wet mass where applicable, and fuel/resource capacity where applicable. Engine parts additionally carry thrust and Isp at sea level and in vacuum.
- **Celestial Body**: A stock or custom planetary body with surface gravity, equatorial radius, atmosphere properties, sphere of influence, and a flag indicating whether it is user-entered.
- **Mission Calculation Profile**: Per-mission settings for a delta-v estimate — selected launch body, target body, launch altitude assumption, target orbit altitude, atmospheric efficiency multiplier, and safety margin percentage.

### Validation Rules

- Rocket name is required and must be unique.
- A rocket must contain at least one stage.
- A stage must contain at least one part.
- Mass values (dry mass, wet mass) must be non-negative.
- Fuel capacity values must be non-negative.
- Required delta-v (estimated or overridden) must be non-negative.
- Safety margin must be between 0% and 100% inclusive.
- Atmospheric efficiency multiplier must be greater than 0% and no greater than 100%.
- Custom body radius and surface gravity must be greater than zero.
- Custom body surface pressure must be non-negative.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can build a two-stage rocket from the catalogue and receive a total delta-v estimate within 2 minutes of opening the Rocket Builder.
- **SC-002**: Given a known rocket configuration (e.g., one LV-T45 engine + one FL-T400 tank), the calculated delta-v is within 5% of the manually verified Tsiolkovsky result for that configuration.
- **SC-003**: Mission readiness state updates automatically when the assigned rocket's configuration changes, without requiring any additional user action.
- **SC-004**: A user can clearly distinguish between a calculated, approximated, and manually overridden delta-v figure at a glance — without needing to open any additional panel.
- **SC-005**: Every warning generated by the calculator includes a short plain-language explanation of why confidence is reduced.
- **SC-006**: A user can plan a Kerbin surface-to-LKO mission — select launch body, select target, assign rocket — and receive a readiness verdict in no more than 3 distinct interactions.
- **SC-007**: All delta-v calculation logic is covered by deterministic unit tests; the same inputs always produce the same outputs.

## Assumptions

- Stock KSP celestial body data (Kerbol System) will be seeded from the provided KSP wiki HTML exports; no runtime HTML parsing is required.
- The part catalogue will be seeded with **all stock KSP base-game parts** (~368 parts as of KSP 1.12.5), sourced from the KSP wiki HTML exports (`Parts - Kerbal Space Program Wiki.html`) provided with this specification. The HTML must be transformed into application seed data during the implementation phase; no runtime HTML parsing is required. DLC parts (Breaking Ground, Making History) are excluded from v1.
- Asparagus/crossfeed staging is out of scope for v1; all staging is sequential (see FR-037). The data model must accommodate parallel staging as a planned v2 extension.
- Centre-of-mass and centre-of-drag are assumed stable; the system does not calculate or validate aerodynamic stability.
- The system does not simulate real-time flight, exact aerodynamics, or patched-conic trajectories.
- Users cannot create or modify catalogue parts in v1; the catalogue is seeded and read-only.
- Custom parts (user-defined part entries) are out of scope for v1.
- `.craft` file import is out of scope for v1.
- Modded KSP parts are out of scope for v1.
- All delta-v calculation logic resides in the backend; the frontend displays results returned by the backend and does not duplicate calculation logic.
- Payload mass is treated as a part added to a stage, not as a separate top-level input.
- The safety margin percentage is configurable per mission; the default is 10% (consistent with the existing readiness planner).
