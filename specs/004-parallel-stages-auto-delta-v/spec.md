# Feature Specification: Parallel Stages and Auto Delta-V Assignment

**Feature Branch**: `004-parallel-stages-auto-delta-v`

**Created**: 2026-05-13

**Status**: Draft

**Input**: User description: "we need to add the ability for a stage to have parallel/inline stages (like boosters during a lifter stage). essentially a 'stage within a stage' that is released as soon as mass has decreased from wet to dry. additionally, 'available delta-v' in a mission should automatically be added to a mission from the assigned rocket being used."

## User Scenarios & Testing *(mandatory)*

<!--
  Stories are ordered as independently deliverable MVP slices.
  P1 = core value; can be demonstrated and tested alone.
  P2 = closes the loop or improves transparency.
-->

### User Story 1 - Attach Booster Stages to a Lifter Stage (Priority: P1)

A rocket designer opens the rocket builder, selects a main (sequential) stage, and attaches one or more parallel/inline booster stages to it. Each booster stage contains its own parts — typically solid rocket boosters or liquid side-boosters — and fires simultaneously with the parent stage. When a booster's fuel is fully consumed (wet mass → dry mass), it is automatically jettisoned from the rocket.

**Why this priority**: Parallel strap-on boosters represent the most common real-world and KSP launch vehicle configuration. Without the ability to model them, the delta-v calculator cannot accurately represent the vast majority of complex rockets. This delivers the core new mechanical capability independently of any mission assignment.

**Independent Test**: A user can open a rocket, navigate to the launch stage, add one parallel stage containing two solid rocket booster parts, save, and see the rocket's total delta-v increase compared to the same rocket without the boosters — with the parallel stage displayed nested below the parent stage.

**Acceptance Scenarios**:

1. **Given** a main stage exists in the rocket builder, **When** the user adds a parallel stage and populates it with solid rocket booster parts, **Then** the parallel stage appears nested beneath the parent stage in the UI.
2. **Given** a parent stage with two attached parallel booster stages, **When** the rocket is saved and delta-v is calculated, **Then** the parallel stages contribute their thrust and propellant mass to the combined calculation.
3. **Given** a parallel stage has exhausted all its fuel, **When** the stage group delta-v is evaluated, **Then** the parallel stage's full dry mass (structure + empty tanks) is jettisoned and excluded from subsequent mass calculations.
4. **Given** multiple parallel stages are attached to one parent stage with different total fuel masses, **When** the first parallel stage's fuel is exhausted, **Then** that stage is released and its dry mass subtracted; remaining parallel stages continue burning alongside the parent.
5. **Given** the user attempts to add a parallel stage to an existing parallel stage, **When** the action is triggered, **Then** the system prevents the nesting and displays a validation message — parallel stage nesting is limited to one level.
6. **Given** a parallel stage has no engine parts, **When** the rocket is viewed, **Then** a "No Engine in Parallel Stage" warning is displayed on both the parallel stage and its parent stage summary.
7. **Given** a parallel stage has engine parts but no compatible fuel in its own parts list, **When** the rocket is viewed, **Then** a "No Fuel in Parallel Stage" warning is displayed.

---

### User Story 2 - View Combined Stage Group Delta-V Breakdown (Priority: P2)

A mission planner opens the rocket delta-v breakdown and can see the combined contribution of the parent stage and all its booster stages displayed as a group — including per-booster metrics and the effective Isp of the combined group.

**Why this priority**: Transparency of the parallel stage calculation allows users to understand the source of their delta-v figures, diagnose low-delta-v configurations, and tune booster sizing confidently.

**Independent Test**: A user with a rocket containing a main stage and two parallel booster stages can expand the stage group in the calculation breakdown and read the combined wet/dry mass, thrust-weighted Isp, and total group delta-v — without needing to calculate anything manually.

**Acceptance Scenarios**:

1. **Given** a parent stage with parallel stages, **When** the user views the rocket summary, **Then** the stage group displays combined total wet mass, total dry mass, thrust-weighted effective Isp, and total group delta-v.
2. **Given** a stage group is expanded in the calculation breakdown, **When** individual parallel stages are inspected, **Then** each parallel stage displays its own parts, wet mass, dry mass, fuel type, and estimated delta-v contribution.
3. **Given** a stage group with a parallel stage that has a warning, **When** the parent stage is viewed in the summary, **Then** the warning is surfaced at the parent level and in the overall rocket warnings list.
4. **Given** a stage group with three parallel stages each running dry at different points, **When** the breakdown is expanded, **Then** the calculation phases are listed: one phase per jettison event, each showing the mass before and after the release.

---

### User Story 3 - Mission Auto-Populates Available Delta-V from Assigned Rocket (Priority: P1)

A mission planner assigns a saved rocket to a mission. The available delta-v field is immediately and automatically set to the rocket's current total calculated delta-v — clearly labelled as rocket-sourced — without requiring any manual input from the user.

**Why this priority**: This removes the last manual, error-prone step between building a rocket and using it in a mission. Without auto-population, a user who builds a rocket and assigns it must still remember to manually re-enter the same delta-v figure in the mission — which defeats the purpose of the rocket library.

**Independent Test**: A user can open a mission with no rocket assigned, select any saved and valid rocket from the picker, and immediately see the available delta-v field populate with the rocket's total delta-v — labelled with the rocket's name — without pressing any additional buttons or entering any values.

**Acceptance Scenarios**:

1. **Given** a mission with no rocket assigned, **When** the user selects and assigns a rocket, **Then** the available delta-v field is immediately populated with the rocket's total calculated delta-v.
2. **Given** a mission with a rocket assigned, **When** the rocket's stages or parts are modified and the mission is next viewed, **Then** the available delta-v field reflects the updated rocket calculation.
3. **Given** a mission with a rocket assigned, **When** the mission is viewed, **Then** the available delta-v field is read-only and labelled as sourced from the assigned rocket (e.g., "From: [Rocket Name]").
4. **Given** a user removes a rocket assignment from a mission, **When** the mission is next viewed, **Then** the available delta-v field reverts to manual entry mode and is pre-filled with the last rocket-derived value as a starting default.
5. **Given** an assigned rocket is subsequently deleted, **When** the mission is viewed, **Then** the available delta-v field displays a missing-rocket warning and reverts to manual entry using the last known calculated value.
6. **Given** a rocket with an invalid or incomplete configuration (e.g., no engine) is assigned, **When** the mission is viewed, **Then** the available delta-v is shown as zero or indeterminate, and a "Rocket Configuration Incomplete" warning is displayed on the mission.
7. **Given** a rocket is assigned to multiple missions, **When** the rocket is updated and each mission is individually viewed, **Then** all missions display the updated rocket delta-v without any user action on the missions themselves.

---

### Edge Cases

- Parent stage has no engine; all thrust comes from parallel booster stages — after boosters separate, the parent coast-stage contributes zero delta-v but its dry mass still factors into subsequent stage calculations.
- All parallel stages within a group run dry at exactly the same moment — treated as a single simultaneous jettison event with combined dry mass removed at once.
- Parallel stage has engine parts only, no fuel tanks — the engine's fuel type has no compatible tank in the stage; triggers "No Fuel in Parallel Stage" warning.
- Stage group contains a mix of fuel types across parent and parallel stages — "Mixed Fuel Uncertainty" warning is applied to the entire group.
- Rocket is configured and assigned to a mission; user then modifies the rocket causing it to become invalid (e.g., all parts removed from all stages) — mission shows indeterminate delta-v and "Rocket Configuration Incomplete" warning.
- Mission's available delta-v auto-populates with a positive value, then the user removes the rocket — field pre-fills with the last positive value, which the user can then manually adjust or clear.
- Parallel stage is deleted from a rocket after the rocket has been assigned to missions — missions reflect the updated (lower) delta-v on next view.
- Rocket with parallel stages is assigned to a mission on a vacuum body — stage group delta-v calculation uses vacuum Isp throughout (atmospheric efficiency not applied to parallel stages separately).

## Requirements *(mandatory)*

### Functional Requirements

#### Parallel Stage Management

- **FR-040**: Users MUST be able to add one or more parallel stages to any main (sequential) stage in a rocket via the rocket builder interface.
- **FR-041**: Each parallel stage MUST support an independent list of parts selected from the existing part catalogue.
- **FR-042**: Parallel stages MUST fire simultaneously with their parent stage and MUST be automatically jettisoned when their own fuel supply is fully exhausted (wet mass to dry mass transition).
- **FR-043**: Each parallel stage MUST be released independently as its fuel runs out; multiple parallel stages do NOT need to share the same burn time.
- **FR-044**: Parallel stage nesting MUST be limited to one level: a parallel stage MUST NOT contain further parallel stages. The system MUST prevent users from creating nested parallel stages and MUST display a clear validation message if attempted.
- **FR-045**: The rocket builder MUST visually present parallel stages nested beneath their parent stage, clearly distinguished from sequential stages through labelling and layout.
- **FR-046**: Users MUST be able to add, remove, and reorder parts within a parallel stage using the same part catalogue interaction supported for main stages.

#### Parallel Stage Delta-V Calculation

- **FR-047**: When a parent stage has one or more parallel stages, the delta-v calculation for the combined stage group MUST use a thrust-weighted average Isp across all engines active in the group at each calculation phase.
- **FR-048**: The calculation MUST proceed in sequential phases, where each phase boundary is defined by a parallel stage exhausting its fuel and being jettisoned. The final phase is the parent stage burning alone after all parallel stages have separated.
- **FR-049**: The initial mass for each calculation phase MUST be the total remaining wet mass: parent stage remaining wet mass, plus all still-attached parallel stages' remaining wet mass, plus the combined mass of all stages above in the rocket's firing sequence (payload mass stack).
- **FR-050**: At the end of each phase (parallel stage jettison), the jettisoned parallel stage's dry mass MUST be subtracted from the mass entering the next phase.
- **FR-051**: After all parallel stages have separated, the parent stage MUST continue its burn using its own remaining fuel and its own engine(s)' Isp.
- **FR-052**: The total delta-v contribution of a stage group MUST equal the sum of delta-v contributions across all phases in that group.
- **FR-053**: Cross-feed fuel sharing between a parallel stage and its parent stage is out of scope; each parallel stage consumes its own fuel independently. The existing asparagus staging approximation (FR-039 in feature 003) is not affected by parallel stage calculations and MUST NOT be double-applied.

#### Stage Group Display

- **FR-054**: Each parent stage with one or more parallel stages MUST display combined group metrics in the summary: total wet mass, total dry mass, thrust-weighted effective Isp, and total group delta-v.
- **FR-055**: Expanding a stage group MUST reveal per-parallel-stage details: parts list, wet mass, dry mass, fuel type, and estimated delta-v contribution for each phase in which the stage is active.
- **FR-056**: Warnings generated by any parallel stage MUST be surfaced at both the parallel stage level and the parent stage summary level, and MUST appear in the overall rocket warnings list.

#### Parallel Stage Validation

- **FR-057**: A parallel stage containing no engine parts MUST trigger a "No Engine in Parallel Stage" warning; the affected stage group's total delta-v MUST be flagged as indeterminate.
- **FR-058**: A parallel stage with engine parts but no compatible fuel in its own parts list MUST trigger a "No Fuel in Parallel Stage" warning.
- **FR-059**: A parallel stage containing mixed or incompatible fuel types MUST trigger the existing "Mixed Fuel Uncertainty" warning.
- **FR-060**: A stage group where neither the parent nor any parallel stage contains an engine MUST have its delta-v shown as zero, and a "No Engine" warning MUST be shown at the group level.

#### Auto Delta-V Population (Mission Assignment)

- **FR-061**: When a rocket is assigned to a mission, the mission's available delta-v value MUST be automatically derived and displayed from the rocket's current total calculated delta-v without requiring any additional user action.
- **FR-062**: The available delta-v field MUST be displayed as read-only when a rocket is assigned and MUST clearly identify the source rocket (e.g., "From: [Rocket Name]").
- **FR-063**: When a rocket assigned to a mission is modified and saved, the mission's available delta-v MUST automatically reflect the updated rocket calculation on the next mission view.
- **FR-064**: When a rocket assignment is removed from a mission, the available delta-v field MUST revert to manual entry mode and MUST pre-fill with the last rocket-sourced value as a starting default for the user to adjust.
- **FR-065**: When an assigned rocket is deleted, the mission MUST display a "Rocket Missing" warning, show the last known calculated delta-v as a reference, and prompt the user to either re-assign a rocket or confirm a manual value.
- **FR-066**: When a rocket with an incomplete or invalid configuration (e.g., no engine, no stages) is assigned to a mission, the available delta-v MUST be displayed as zero or indeterminate and the mission MUST show a "Rocket Configuration Incomplete" warning, with readiness set to Not Ready.
- **FR-067**: The auto-population behaviour specified in FR-061–FR-066 extends and formalises the intent of FR-032 from feature 003. The rocket-sourced delta-v is always derived dynamically from the current saved rocket state; it is never stored as a separate copy on the mission.

### Key Entities

- **Parallel Stage**: A child stage attached to a parent (sequential) stage in a rocket. Fires simultaneously with its parent stage, contains its own independent list of catalogue parts (engines and fuel), and is automatically jettisoned when its own fuel is fully consumed. Cannot contain further parallel stages (maximum nesting depth: 1).

### Validation Rules

- Parallel stage nesting depth must be exactly 1; a parallel stage cannot be the parent of another parallel stage.
- A stage group (parent + all parallel stages) with no engine in any member stage is invalid for calculation and must be flagged with a "No Engine" warning; delta-v is zero for the group.
- When a rocket is assigned to a mission, the available delta-v field is not editable; manual input is only permitted when no rocket is assigned.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A rocket with strap-on parallel booster stages produces a measurably higher total delta-v than the same rocket configuration without those boosters, as calculated by the system.
- **SC-002**: All existing sequential-staging rocket configurations continue to produce identical delta-v calculations after parallel staging support is introduced — zero regression on existing rockets.
- **SC-003**: Assigning a rocket to a mission populates the available delta-v without any additional user steps beyond the assignment action itself.
- **SC-004**: A mission's available delta-v is always consistent with the assigned rocket's most recently saved total delta-v when the mission is viewed.
- **SC-005**: Users cannot accidentally overwrite a rocket-sourced available delta-v value via the mission form while a rocket is assigned — the field is protected.
- **SC-006**: The calculation breakdown for a stage group with parallel stages is sufficiently detailed that a user can verify the booster contribution by inspecting displayed wet/dry mass values and phase-by-phase delta-v figures.

## Assumptions

- Parallel stages are modelled as strap-on boosters that fire simultaneously with the parent stage and carry their own engines and fuel. True crossfeed fuel sharing between a parallel stage and its parent is out of scope and deferred to a future iteration.
- All parallel stages within a single parent are independent; they may have different burn times and each is released as soon as its own fuel is spent.
- The existing asparagus staging approximation (FR-039, feature 003) continues to function as a top-level rocket setting and is not impacted by the introduction of parallel stages. The asparagus bonus is applied to the overall rocket's atmospheric ascent delta-v, not to individual parallel stage calculations.
- The auto-population of available delta-v (FR-061) formalises behaviour that was originally intended but under-specified in FR-032 of feature 003. Downstream planning should treat this as an explicit replacement of the earlier requirement for that behaviour.
- Missions do not recalculate delta-v in real-time as a rocket is being edited — they reflect the last saved state of the assigned rocket at the time the mission page is loaded.
- The part catalogue (FR-009 onwards, feature 003) already includes solid rocket boosters, radial decouplers, and liquid side-booster parts; no new catalogue additions are required solely to support this feature.
- Stage numbering and KSP firing-order conventions established in FR-037 (feature 003) continue to apply; parallel stages do not carry their own sequential stage numbers — they are sub-items of the parent stage.
