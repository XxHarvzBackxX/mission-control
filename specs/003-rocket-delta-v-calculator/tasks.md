---
description: "Task list for Rocket-Based Mission Delta-V Calculator (003)"
---

# Tasks: Rocket-Based Mission Delta-V Calculator

**Input**: Design documents from `specs/003-rocket-delta-v-calculator/`

**Prerequisites**: plan.md ✅ | spec.md ✅ | research.md ✅ | data-model.md ✅ | contracts/api.md ✅

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Parallelisable — operates on different files with no dependency on in-progress tasks
- **[Story]**: User story label (US1–US5); omitted for Setup and Foundational phases
- All file paths are repository-relative

---

## Phase 1: Setup

**Purpose**: Seed data generation and project initialisation; required before any repository implementation.

- [X] T001 Generate `backend/MissionControl.Api/data/parts.json` seed file from attached KSP wiki HTML export (~368 stock parts; fields: id, name, category, dryMass, wetMass, fuelCapacity, engineStats per data-model.md § CataloguePart)
- [X] T002 [P] Generate `backend/MissionControl.Api/data/celestial-bodies.json` seed file with all 17 Kerbol system bodies from research.md §4 (stock array + empty custom array; fields per data-model.md § CelestialBody)
- [X] T003 Add `celestial-bodies.json` and `parts.json` as `<Content CopyToOutputDirectory="PreserveNewest">` items in `backend/MissionControl.Api/MissionControl.Api.csproj`

**Checkpoint**: Seed data files exist on disk; project builds without errors.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain enums, value objects, the Rocket aggregate, all three domain calculators, their unit and regression tests, all three repository implementations, and DI registration. **No user story phase can begin until T029 is complete.**

**⚠️ CRITICAL**: The regression test suite (T025) is a merge prerequisite — all five fixtures must pass before any PR is raised.

### Enums

- [X] T004 [P] Create `FuelType` enum (LiquidFuelOxidizer, SolidFuel, MonoPropellant, Xenon, LiquidFuelOnly) in `backend/MissionControl.Domain/Enums/FuelType.cs`
- [X] T005 [P] Create `MissionProfileType` enum (OrbitInsertion, AscentOnly, SurfaceLanding, FullReturn) in `backend/MissionControl.Domain/Enums/MissionProfileType.cs`
- [X] T006 [P] Create `PartCategory` enum (15 values: Pods, FuelTanks, Engines, CommandAndControl, Structural, Coupling, Payload, Aerodynamics, Ground, Thermal, Electrical, Communication, Science, Cargo, Utility) in `backend/MissionControl.Domain/Enums/PartCategory.cs`
- [X] T007 [P] Extend `WarningType` enum with 9 new values (NoCommandPart, NoEngine, NoFuelSource, MixedFuelUncertainty, AtmosphericLossApplied, UnstableCraftAssumption, CustomBodyApproximation, ManualOverrideApplied, AsparagusApproximationApplied) in `backend/MissionControl.Domain/Enums/WarningType.cs`

### Reference Entities

- [X] T008 [P] Create `CataloguePart` class with nested `EngineStats` and `FuelCapacity` value objects in `backend/MissionControl.Domain/Entities/CataloguePart.cs` (fields per data-model.md § CataloguePart, EngineStats, FuelCapacity)
- [X] T009 [P] Create `CelestialBody` class in `backend/MissionControl.Domain/Entities/CelestialBody.cs` (all 11 fields per data-model.md § CelestialBody, including `SurfacePressure > 0` = has atmosphere)

### Value Objects

- [X] T010 [P] Create `StageEntry` immutable value object (PartId, Quantity) in `backend/MissionControl.Domain/ValueObjects/StageEntry.cs`
- [X] T011 [P] Create `MissionCalculationProfile` value object (LaunchBodyId, TargetBodyId, ProfileType, TargetOrbitAltitude, AtmosphericEfficiencyMultiplier, SafetyMarginPercent, RequiredDeltaVOverride) in `backend/MissionControl.Domain/ValueObjects/MissionCalculationProfile.cs`
- [X] T012 [P] Create `StageDeltaVResult` value object (StageNumber, StageName, WetMass, DryMass, IspUsed, RawDeltaV, EfficiencyFactor, AsparagusBonus, EffectiveDeltaV, Warnings) in `backend/MissionControl.Domain/ValueObjects/StageDeltaVResult.cs`
- [X] T013 [P] Create `RocketDeltaVResult` value object (TotalEffectiveDeltaV, Stages, Warnings, IsValid) in `backend/MissionControl.Domain/ValueObjects/RocketDeltaVResult.cs`
- [X] T014 [P] Create `RequiredDeltaVResult` value object (TotalRequiredDeltaV, AscentDeltaV, TransferDeltaV, DescentDeltaV, ReturnDeltaV, EstimationMethod, IsApproximated) in `backend/MissionControl.Domain/ValueObjects/RequiredDeltaVResult.cs`

### Rocket Aggregate

- [X] T015 Create `Rocket` aggregate root and nested `Stage` entity in `backend/MissionControl.Domain/Entities/Rocket.cs`; implement `Rocket.Create()` factory and `Rocket.Update()` with full validation (name 1–200 chars; ≥1 stage; AsparagusEfficiencyBonus ∈ [0.0, 0.20]); `Stage` must have StageNumber ≥ 1, unique within rocket, and ≥1 part (depends on T004, T006, T010)

### Repository Interfaces

- [X] T016 [P] Create `IRocketRepository` interface (GetAllAsync, GetByIdAsync, GetByNameAsync, AddAsync, UpdateAsync, DeleteAsync, GetMissionIdsAssignedToRocketAsync) in `backend/MissionControl.Domain/Interfaces/IRocketRepository.cs`
- [X] T017 [P] Create `IPartCatalogueRepository` interface (GetAllAsync, GetByIdAsync, GetByCategoryAsync, SearchByNameAsync) in `backend/MissionControl.Domain/Interfaces/IPartCatalogueRepository.cs`
- [X] T018 [P] Create `ICelestialBodyRepository` interface (GetAllAsync, GetByIdAsync, AddCustomAsync) in `backend/MissionControl.Domain/Interfaces/ICelestialBodyRepository.cs`

### Domain Calculators

- [X] T019 Implement `StageDeltaVCalculator` static pure service in `backend/MissionControl.Domain/Services/StageDeltaVCalculator.cs`; calculates `ΔV = Isp × g₀ × ln(m_wet/m_dry) × efficiencyFactor × (1 + asparagusBonus)` per stage; resolves combined Isp for multi-engine stages; emits NoEngine, NoFuelSource, MixedFuelUncertainty warnings; g₀ = 9.80665 m/s² (depends on T004, T008-T010, T012, T015)
- [X] T020 Implement `RocketDeltaVCalculator` static pure service in `backend/MissionControl.Domain/Services/RocketDeltaVCalculator.cs`; iterates stages from highest StageNumber down to 1; accumulates wet/dry mass accounting for jettisoned stages; selects sea-level Isp for atmospheric launch bodies and vacuum Isp for airless; returns `RocketDeltaVResult` with per-stage breakdown (depends on T013, T019)
- [X] T021 Implement `CelestialBodyDeltaVEstimator` static pure service in `backend/MissionControl.Domain/Services/CelestialBodyDeltaVEstimator.cs`; ascent: `v_circ = sqrt(g × R) + drag/gravity losses`; transfer: Hohmann approximation using semi-major axes; applies MissionProfileType to include descent/return components; uses `RequiredDeltaVOverride` when set (depends on T005, T009, T011, T014)

### Calculator Unit Tests

- [X] T022 [P] Unit tests for `StageDeltaVCalculator` in `backend/MissionControl.Tests/Domain/StageDeltaVCalculatorTests.cs`: correct DV for single liquid stage; correct DV for SRB stage; zero DV + NoEngine warning when no engine; NoFuelSource warning when engine has no compatible fuel; MixedFuelUncertainty warning for mixed types; asparagus bonus applied correctly; atmospheric vs vacuum Isp selection (depends on T019)
- [X] T023 [P] Unit tests for `RocketDeltaVCalculator` in `backend/MissionControl.Tests/Domain/RocketDeltaVCalculatorTests.cs`: two-stage total DV with jettisoned stage; jettisoned stage dry mass excluded from subsequent stage wet mass; non-jettisoned stage mass carried forward; atmospheric efficiency applied for atmospheric body; no efficiency multiplier for airless body; asparagus bonus suppressed for airless body (depends on T020)
- [X] T024 [P] Unit tests for `CelestialBodyDeltaVEstimator` in `backend/MissionControl.Tests/Domain/CelestialBodyDeltaVEstimatorTests.cs`: OrbitInsertion returns ascent + transfer; AscentOnly returns only ascent component; custom body produces CustomBodyApproximation warning; manual override returns override value + ManualOverrideApplied warning; airless body uses vacuum ascent formula (depends on T021)
- [X] T025 Regression test suite in `backend/MissionControl.Tests/Domain/RocketDeltaVRegressionTests.cs` with 5 named fixtures from plan.md §*Regression Test Constraint*; each fixture asserts `|result.TotalEffectiveDeltaV - preCalculatedDeltaV| ≤ requiredDeltaV × (safetyMarginPercent/100) / 2` using `RocketDeltaVFixture` record from data-model.md §*Test Support Types*; fixtures: `single-stage-atm` (±170 m/s), `two-stage-atm` (±170 m/s), `vacuum-only` (±43 m/s), `srb-stage` (±170 m/s), `asparagus-atm` (±170 m/s); all five MUST pass (depends on T020)

### Repository Implementations

- [X] T026 [P] Implement `JsonRocketRepository` in `backend/MissionControl.Infrastructure/Persistence/JsonRocketRepository.cs`; same SemaphoreSlim-locked JSON file pattern as existing `JsonMissionRepository`; file path: `rockets.json` in data directory; created on first write (depends on T015-T016)
- [X] T027 [P] Implement `JsonPartCatalogueRepository` in `backend/MissionControl.Infrastructure/Persistence/JsonPartCatalogueRepository.cs`; reads `parts.json` once at startup into `IReadOnlyList<CataloguePart>`; implements category filter and name search in memory; no write operations (depends on T001, T008, T017)
- [X] T028 [P] Implement `JsonCelestialBodyRepository` in `backend/MissionControl.Infrastructure/Persistence/JsonCelestialBodyRepository.cs`; reads stock bodies from `celestial-bodies.json` at startup; appends custom bodies to same file's custom array on `AddCustomAsync`; SemaphoreSlim-locked writes (depends on T002, T009, T018)

### DI Registration

- [X] T029 Register `IRocketRepository → JsonRocketRepository`, `IPartCatalogueRepository → JsonPartCatalogueRepository`, `ICelestialBodyRepository → JsonCelestialBodyRepository` as singletons in `backend/MissionControl.Api/Program.cs` (depends on T026-T028)

**Checkpoint**: `dotnet test --filter "FullyQualifiedName~RocketDeltaVRegressionTests"` — all 5 fixtures pass. `dotnet test` — all calculator unit tests pass.

---

## Phase 3: User Story 1 — Build a Rocket and See Estimated Delta-V (Priority: P1) 🎯 MVP

**Goal**: A user opens the Rocket Library, builds a rocket from staged catalogue parts, and immediately sees per-stage and total estimated delta-v — no mission required.

**Independent Test**: Navigate to `/rockets/new`; add one stage with a liquid fuel tank and LV-T45 engine; save; see delta-v figure and stage breakdown displayed.

### Backend — US1

- [X] T030 [P] [US1] Create `CreateRocketDto` and `UpdateRocketDto` (name, description, notes, usesAsparagusStaging, asparagusEfficiencyBonus, stages array of `CreateStageDto`) in `backend/MissionControl.Api/DTOs/CreateRocketDto.cs` and `UpdateRocketDto.cs`
- [X] T031 [P] [US1] Create `StageDto`, `StageEntryDto`, `RocketListItemDto`, `RocketSummaryDto`, `StageDeltaVDto`, `RocketDeltaVBreakdownDto` in `backend/MissionControl.Api/DTOs/` (shapes per contracts/api.md §Rockets)
- [X] T032 [US1] Implement `RocketsController` with five actions in `backend/MissionControl.Api/Controllers/RocketsController.cs`: `GET /api/rockets` (list, calls `RocketDeltaVCalculator` per rocket for summary DV), `POST /api/rockets` (create, 201 + full summary, 409 on name conflict), `GET /api/rockets/{id}` (full summary + DV breakdown, 404 if missing), `PUT /api/rockets/{id}` (replace, 200 + updated summary), `DELETE /api/rockets/{id}` (200 + affectedMissionCount); all map domain → DTOs, no business logic in controller (depends on T020, T029-T031)
- [X] T033 [P] [US1] Unit tests for `RocketsController` in `backend/MissionControl.Tests/Api/RocketsControllerTests.cs`: GET returns list with DV figures; POST 201 with full summary; POST 409 on duplicate name; GET/{id} 404 for missing; PUT replaces stages atomically; DELETE returns affectedMissionCount (depends on T032)

### Frontend — US1

- [X] T034 [P] [US1] Create `rocket.model.ts` with `RocketListItem`, `RocketSummary`, `StageDto`, `StageEntryDto`, `RocketDeltaVBreakdown`, `StageDeltaVDto`, `CreateRocketRequest`, `UpdateRocketRequest` interfaces in `frontend/src/app/models/rocket.model.ts`
- [X] T035 [P] [US1] Implement `RocketsService` with `getAll()`, `getById(id)`, `create(dto)`, `update(id, dto)`, `delete(id)` methods wrapping HTTP calls to `/api/rockets` in `frontend/src/app/services/rockets.service.ts` (depends on T034)
- [X] T036 [US1] Create rockets feature module with `rockets.routes.ts` defining routes: `''` → rocket-list, `'new'` → rocket-builder, `':id'` → rocket-detail, `':id/edit'` → rocket-builder; register in app routing in `frontend/src/app/rockets/rockets.routes.ts` (depends on T035)
- [X] T037 [P] [US1] Implement `RocketListComponent` displaying all rockets with name, stage count, total DV, and warning badges; "New Rocket" button navigates to builder in `frontend/src/app/rockets/rocket-list/`
- [X] T038 [US1] Implement `RocketBuilderComponent` with: rocket name/description/notes form fields; stage list (add/remove/reorder stages, each stage has name + isJettisoned toggle + parts list); asparagus checkbox + 0–20% slider with labels Conservative (8%), Moderate (12%), Optimistic (15%), Aggressive (20%); default slider to 8% on first check; save/cancel; in `frontend/src/app/rockets/rocket-builder/` (depends on T036-T037)
- [X] T039 [US1] Implement `RocketDetailComponent` showing rocket metadata, per-stage breakdown table (wet mass, dry mass, Isp, efficiency factor, asparagus bonus, effective DV), total DV, and warning list in `frontend/src/app/rockets/rocket-detail/` (depends on T035)

**Checkpoint**: Navigate to `/rockets`; create a two-stage rocket; see per-stage DV and total displayed correctly.

---

## Phase 4: User Story 3 — Browse and Select Parts from the Catalogue (Priority: P2)

**Goal**: A user opens the part picker in the rocket builder, filters by category or searches by name, and selects parts — their mass, fuel capacity, and engine statistics populate automatically.

**Independent Test**: Open part picker in rocket-builder; filter by "Engines"; select "LV-T45 'Swivel'"; confirm it appears in the stage with Isp and thrust values from catalogue data.

### Backend — US3

- [X] T040 [P] [US3] Create `PartDto` (id, name, category, dryMass, wetMass, fuelCapacity dict, engineStats) in `backend/MissionControl.Api/DTOs/PartDto.cs`
- [X] T041 [US3] Implement `PartsController` in `backend/MissionControl.Api/Controllers/PartsController.cs`: `GET /api/parts` (optional `?category=` and `?search=` query params; delegates to `IPartCatalogueRepository`), `GET /api/parts/{id}` (404 if not found); map `CataloguePart` → `PartDto` in controller (depends on T029, T040)
- [X] T042 [P] [US3] Unit tests for `PartsController` in `backend/MissionControl.Tests/Api/PartsControllerTests.cs`: GET returns all parts; GET with category filter returns only matching parts; GET with search returns partial name matches (case-insensitive); GET/{id} 404 for unknown id (depends on T041)

### Frontend — US3

- [X] T043 [P] [US3] Create `part.model.ts` with `PartDto`, `EngineStatsDto`, `PartCategory` union type, `FuelType` union type in `frontend/src/app/models/part.model.ts`
- [X] T044 [P] [US3] Implement `PartsService` with `getAll(category?, search?)` method in `frontend/src/app/services/parts.service.ts` (depends on T043)
- [X] T045 [US3] Implement `PartPickerComponent` inside `RocketBuilderComponent`: category dropdown filter, real-time name search input, scrollable part list with name/mass/Isp preview, "Add" button appending a `StageEntryDto` to the active stage; encapsulate in `frontend/src/app/rockets/rocket-builder/part-picker/` (depends on T038, T044)

**Checkpoint**: Open builder; search "FL-T"; FL-T200, FL-T400, FL-T800 appear; add FL-T400; stage mass updates.

---

## Phase 5: User Story 2 — Assign a Rocket to a Mission (Priority: P2)

**Goal**: A user assigns a saved rocket to an existing mission; readiness is thereafter driven by the rocket's calculated available delta-v vs. the estimated required delta-v derived from the mission's calculation profile.

**Independent Test**: Open an existing mission; assign "Kerbin Express Mk1"; select OrbitInsertion profile to Mun; readiness state updates automatically from calculated DV.

### Domain — US2

- [X] T046 [P] [US2] Extend `Mission` entity in `backend/MissionControl.Domain/Entities/Mission.cs` with three nullable fields: `AssignedRocketId` (Guid?), `CalculationProfile` (MissionCalculationProfile?), `RocketName` (string?); update `Mission.Update()` to accept them; no validation coupling to Rocket aggregate (depends on T011)

### Backend — US2

- [X] T047 [P] [US2] Extend `CreateMissionDto` in `backend/MissionControl.Api/DTOs/CreateMissionDto.cs` with optional `assignedRocketId` (Guid?) and `calculationProfile` (MissionCalculationProfileDto?); update `MissionListItemDto` and `MissionSummaryDto` with `assignedRocketId`, `rocketName`, `calculationProfile`, `requiredDeltaVBreakdown` fields
- [X] T048 [P] [US2] Create `MissionCalculationProfileDto` and `RequiredDeltaVBreakdownDto` in `backend/MissionControl.Api/DTOs/MissionCalculationProfileDto.cs` and `RequiredDeltaVBreakdownDto.cs` (shapes per contracts/api.md §Missions)
- [X] T049 [US2] Modify `MissionsController` in `backend/MissionControl.Api/Controllers/MissionsController.cs`: on POST/PUT/GET when `assignedRocketId` is set — load `Rocket` from `IRocketRepository`; load parts from `IPartCatalogueRepository`; load launch body from `ICelestialBodyRepository`; call `RocketDeltaVCalculator.Calculate()` for available DV; call `CelestialBodyDeltaVEstimator.Estimate()` (or use override) for required DV; pass scalars to `Mission.Update()` as before; return `requiredDeltaVBreakdown` in `MissionSummaryDto`; return 400 if `assignedRocketId` references a missing rocket (depends on T020-T021, T029, T046-T048)
- [X] T050 [P] [US2] Unit tests for modified `MissionsController` in `backend/MissionControl.Tests/Api/MissionsControllerTests.cs`: PUT with valid rocketId computes and stores DV; GET returns breakdown in response; PUT with unknown rocketId returns 400; mission without rocket unchanged; missing-rocket scenario produces warning (depends on T049)

### Frontend — US2

- [X] T051 [P] [US2] Extend `mission.model.ts` with `assignedRocketId`, `rocketName`, `calculationProfile` (MissionCalculationProfileDto), `requiredDeltaVBreakdown` (RequiredDeltaVBreakdownDto) in `frontend/src/app/models/mission.model.ts`
- [X] T052 [US2] Add rocket assignment panel to mission create/edit form: rocket dropdown (sourced from `RocketsService.getAll()`), calculation profile sub-form (launch body, target body, profile type, target orbit altitude, efficiency multiplier, safety margin %, manual DV override toggle); hide manual DV fields when a rocket is assigned; in relevant mission form component in `frontend/src/app/` (depends on T035, T051)

**Checkpoint**: Assign a rocket to a mission; save; reload mission; readiness state reflects calculated DV; unassign rocket; manual DV fields reappear.

---

## Phase 6: User Story 4 — View Calculation Breakdown (Priority: P3)

**Goal**: A user expands a calculation breakdown to see exactly how available and required delta-v were derived — wet/dry mass, Isp, efficiency factors per stage, and required DV components.

**Independent Test**: Open a rocket-assigned mission; expand the delta-v breakdown; see per-stage wet mass, dry mass, Isp, efficiency factor, and DV, plus ascent/transfer/descent/return required DV components.

- [X] T053 [P] [US4] Add per-stage delta-v expansion panel (wet mass, dry mass, Isp, efficiency factor, asparagus bonus, effective DV columns) to `RocketDetailComponent` in `frontend/src/app/rockets/rocket-detail/`; panel is collapsible
- [X] T054 [P] [US4] Add required delta-v breakdown panel (ascent DV, transfer DV, descent DV, return DV, estimation method label, approximation badge) to the mission detail view in `frontend/src/app/`; badge distinguishes estimated / approximated / overridden
- [X] T055 [US4] Wire breakdown panels to mission detail: display `requiredDeltaVBreakdown` from `MissionSummaryDto`; show "Manual Override" badge when `requiredDeltaVOverride` is set; show "Approximated" badge when `isApproximated = true`; show atmospheric efficiency note when `efficiencyFactor < 1.0` (depends on T052-T054)

**Checkpoint**: Expand breakdown on a rocket-assigned mission; all six data points visible per stage; required DV components listed with source badges.

---

## Phase 7: User Story 5 — Plan a Mission Using a Custom Celestial Body (Priority: P4)

**Goal**: A user selects "Other" as launch or target body and enters custom parameters to estimate delta-v for non-stock or modded KSP scenarios.

**Independent Test**: Select "Other" for launch body; enter name "Alternis Kerbin", radius 550 000 m, gravity 8.5 m/s², no atmosphere; receive a delta-v estimate with "Custom Body Approximation" warning.

### Backend — US5

- [X] T056 [P] [US5] Create `CelestialBodyDto` and `CreateCustomBodyDto` in `backend/MissionControl.Api/DTOs/CelestialBodyDto.cs` and `CreateCustomBodyDto.cs` (shapes per contracts/api.md §Celestial Bodies)
- [X] T057 [US5] Implement `CelestialBodiesController` in `backend/MissionControl.Api/Controllers/CelestialBodiesController.cs`: `GET /api/celestial-bodies` (returns stock + custom); `POST /api/celestial-bodies/custom` (validates radius > 0, gravity > 0, pressure ≥ 0; generates GUID id; sets isCustom=true; 201 + full CelestialBodyDto); controller maps domain → DTOs (depends on T009, T029, T056)
- [X] T058 [P] [US5] Unit tests for `CelestialBodiesController` in `backend/MissionControl.Tests/Api/CelestialBodiesControllerTests.cs`: GET returns all 17 stock bodies; POST custom 201 with isCustom=true; POST custom 400 on radius ≤ 0; POST custom 400 on gravity ≤ 0; custom body appears in subsequent GET (depends on T057)

### Frontend — US5

- [X] T059 [P] [US5] Create `celestial-body.model.ts` with `CelestialBodyDto` and `CreateCustomBodyRequest` interfaces in `frontend/src/app/models/celestial-body.model.ts`
- [X] T060 [P] [US5] Implement `CelestialBodiesService` with `getAll()` and `createCustom(dto)` methods in `frontend/src/app/services/celestial-bodies.service.ts` (depends on T059)
- [X] T061 [US5] Replace plain text body inputs in mission calculation profile form with a celestial body selector component: dropdown of stock bodies + "Other" option that expands an inline custom-body entry form (name, radius, gravity, surface pressure, atmosphere height); on selection, populate `launchBodyId` or `targetBodyId` on the profile DTO; in `frontend/src/app/` (depends on T052, T060)

**Checkpoint**: Select "Other" as target body; fill custom form; save mission; "Custom Body Approximation" warning appears in mission summary.

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Asparagus UI refinements, warning surfaces, and validation edge cases not tied to a single story.

- [X] T062 [P] Disable asparagus checkbox and suppress asparagus bonus in `RocketBuilderComponent` when the selected launch body (from mission calculation profile) is airless (`surfacePressure = 0`); show inline note "Asparagus staging has no effect on vacuum launches" in `frontend/src/app/rockets/rocket-builder/`
- [X] T063 [P] Display all domain warnings (NoEngine, NoFuelSource, NoCommandPart, MixedFuelUncertainty, AtmosphericLossApplied, AsparagusApproximationApplied) as styled badge chips in `RocketDetailComponent`; blocking warnings (NoEngine, NoFuelSource) rendered in error style; non-blocking in warning style in `frontend/src/app/rockets/rocket-detail/`
- [X] T064 [P] Display mission-level warnings (InsufficientDeltaV, LowDeltaVMargin, ManualOverrideApplied, CustomBodyApproximation, missing-rocket) in the mission summary header in `frontend/src/app/`; mirror existing readiness state badge pattern from feature 001
- [X] T065 [P] Validate rocket name uniqueness client-side (debounced GET check against `/api/rockets`) in `RocketBuilderComponent`; display inline "Name already taken" validation error before form submission in `frontend/src/app/rockets/rocket-builder/`

---

## Dependencies

```
Phase 1 (T001–T003)
    └── Phase 2: Foundational
            ├── T004–T009 (enums + reference entities) ─────────────────────────────────────────────┐
            │       └── T010–T014 (value objects)                                                    │
            │               └── T015 (Rocket aggregate) ──────────────────────────────────────────┐ │
            │                       ├── T016–T018 (repository interfaces) ───────────────────────┐│ │
            │                       ├── T019 → T020 (calculators) ──────────────────────────┐   ││ │
            │                       │       └── T021 (DeltaV Estimator)                     │   ││ │
            │                       ├── T022–T024 (calculator unit tests)                   │   ││ │
            │                       ├── T025 (regression tests — merge gate) ←── T020 ──────┘   ││ │
            │                       └── T026–T028 (repository impls) ←── T001/T002 ─────────────┘│ │
            │                                       └── T029 (DI registration) ──────────────────┘ │
            └── Phase 3: US1 ──────────────────────────────────────────────────────────────────────┘
                    ├── T030–T031 (rocket DTOs) ──── T032 (RocketsController) ──── T033 (tests)
                    └── T034 → T035 → T036 → T037/T038/T039 (frontend)
                            └── Phase 4: US3
                                    ├── T040–T042 (parts backend)
                                    └── T043 → T044 → T045 (part picker frontend) ← T038
                            └── Phase 5: US2
                                    ├── T046 (Mission entity) → T047–T048 (DTOs) → T049 (controller) → T050 (tests)
                                    └── T051 → T052 (frontend assignment form) ← T035
                                            └── Phase 6: US4
                                                    └── T053–T055 (breakdown panels)
                                    └── Phase 7: US5
                                            ├── T056–T058 (celestial bodies backend)
                                            └── T059 → T060 → T061 (frontend selector) ← T052
            └── Final Phase: Polish (T062–T065) ← depends on US1–US5 frontend components
```

**User story completion order**: US1 → (US3 ‖ US2) → US4 → US5
- US1 must be complete before US3 (part picker needs the rocket builder component)
- US3 and US2 can run in parallel once US1 backend is done
- US4 depends on US2 (breakdown data comes from mission + rocket combo)
- US5 is independent except for the mission profile form (US2 frontend)

---

## Parallel Execution Examples

### Sprint: Foundational (Phase 2 — can run in parallel streams)

**Stream A** (domain model):
T004 → T010 → T015 → T019 → T020 → T022/T023 → T025

**Stream B** (reference data + repositories):
T008/T009 → T017/T018 → T027/T028

**Stream C** (regression fixtures preparation):
Write `RocketDeltaVFixture` test data while A implements calculators

### Sprint: US1 (Phase 3 — two streams)

**Backend** (after T029):
T030/T031 → T032 → T033

**Frontend** (after T029):
T034/T043 → T035 → T036 → T037/T038 → T039

### Sprint: US3 + US2 (Phase 4 + Phase 5 — fully parallel)

**Backend US3**: T040 → T041 → T042
**Frontend US3**: T043/T044 → T045

**Backend US2**: T046/T047/T048 → T049 → T050
**Frontend US2**: T051 → T052

---

## Implementation Strategy

### MVP Scope (US1 alone — deployable after Phase 3)

After completing Phases 1–3:
- Rocket Library with create/edit/delete
- Stage management with part entry
- Per-stage and total delta-v displayed immediately
- All domain warnings visible in rocket detail
- Regression tests passing (merge gate met)

This is a fully demonstrable tool — a user can build a rocket and see a delta-v estimate without any mission involvement.

### Increment 2 (US3 — adds part catalogue UX)

Part picker with category filter and name search replaces manual part entry. No backend changes beyond the new `/api/parts` endpoint.

### Increment 3 (US2 — closes the loop with missions)

Rocket assignment to missions. Readiness driven by calculated DV. Backwards-compatible — missions without a rocket continue to work.

### Increment 4+ (US4, US5 — transparency and modded KSP)

Calculation breakdown panels and custom celestial body support.

---

## Task Summary

| Phase | Tasks | Parallelisable | Story |
|---|---|---|---|
| Phase 1: Setup | T001–T003 | T002 | — |
| Phase 2: Foundational | T004–T029 | T004–T014, T016–T018, T022–T024, T026–T028 | — |
| Phase 3: US1 | T030–T039 | T030–T031, T033–T035, T037 | US1 |
| Phase 4: US3 | T040–T045 | T040, T042–T044 | US3 |
| Phase 5: US2 | T046–T052 | T046–T048, T050–T051 | US2 |
| Phase 6: US4 | T053–T055 | T053–T054 | US4 |
| Phase 7: US5 | T056–T061 | T056, T058–T060 | US5 |
| Final Phase: Polish | T062–T065 | T062–T065 | — |
| **Total** | **65 tasks** | **~37** | |

**Regression gate**: T025 must pass before any PR. Run with:
```bash
dotnet test --filter "FullyQualifiedName~RocketDeltaVRegressionTests"
```

