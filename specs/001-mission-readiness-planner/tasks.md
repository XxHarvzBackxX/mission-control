# Tasks: Mission Readiness Planner

**Input**: Design documents from `specs/001-mission-readiness-planner/`

**Prerequisites**: plan.md ✅ · spec.md ✅ · research.md ✅ · data-model.md ✅ · contracts/api.md ✅ · quickstart.md ✅

**Tests**: Domain unit tests are included (constitution Principle VI — NON-NEGOTIABLE). Frontend component tests are not included (not requested in spec).

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

---

## Phase 1: Setup

**Purpose**: Scaffold both projects and configure cross-cutting infrastructure

- [ ] T001 Create .NET 8 solution with four projects (MissionControl.Domain, MissionControl.Infrastructure, MissionControl.Api, MissionControl.Tests) in backend/
- [ ] T002 [P] Scaffold Angular SPA with Angular CLI (`ng new mission-control --standalone --routing --style=css`) in frontend/
- [ ] T003 Configure Angular proxy to `http://localhost:5000` in frontend/proxy.conf.json and angular.json serve config
- [ ] T004 [P] Configure CORS (allow `http://localhost:4200`) and JSON serialisation in backend/MissionControl.Api/Program.cs
- [ ] T005 [P] Add NUnit 4 and NSubstitute NuGet packages to backend/MissionControl.Tests/MissionControl.Tests.csproj

**Checkpoint**: Both projects scaffold successfully; `dotnet build` and `ng build` pass with no errors

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain types, calculation engine, repository abstraction, and shared frontend primitives that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Backend — Domain Enums & Value Objects

- [ ] T006 Create `ReadinessState`, `MissionControlMode`, and `WarningType` enums in backend/MissionControl.Domain/Enums/ (one file per enum)
- [ ] T007 [P] Create `Warning` value object (`WarningType Type`, `string Message`, `bool IsBlocking`) in backend/MissionControl.Domain/ValueObjects/Warning.cs
- [ ] T008 [P] Create `KspBodyValue` value object (`string Value`, `bool IsCustom`) with predefined KSP body, mission-type, and probe-core lists as static constants in backend/MissionControl.Domain/ValueObjects/KspBodyValue.cs
- [ ] T009 [P] Create `KerbinTime` readonly record struct (`long TotalSeconds`) with `Decompose()` and `ToDisplayString()` producing `Yy, Dd, Hh, Mm, Ss` in backend/MissionControl.Domain/ValueObjects/KerbinTime.cs
- [ ] T010 [P] Create `KerbinTimeTests` (decomposition, `1y 0d 0h 0m 0s`, multi-year, boundary `0s`, negative guard) in backend/MissionControl.Tests/Domain/KerbinTimeTests.cs

### Backend — Calculation Engine (Constitution Principle VI)

- [ ] T011 Implement `ReadinessCalculator` static domain service (`Calculate(availableDv, requiredDv, controlMode, crewMembers) : ReadinessResult`) in backend/MissionControl.Domain/Services/ReadinessCalculator.cs
- [ ] T012 Create `ReadinessCalculatorTests` covering: Ready (≥10% margin, crewed with crew), AtRisk (<10% margin, both warnings fire independently), AtRisk (0% margin — availableDv exactly equals requiredDv → LowReserveMargin warning fires), NotReady (insufficient ΔV), NotReady (MissingCrew), NotReady+LowMargin simultaneous, zero-required-ΔV guard, negative-ΔV guard in backend/MissionControl.Tests/Domain/ReadinessCalculatorTests.cs

### Backend — Repository & Infrastructure

- [ ] T013 Create `IMissionRepository` interface (GetAllAsync, GetByIdAsync, GetByNameAsync, AddAsync, UpdateAsync, DeleteAsync) in backend/MissionControl.Domain/Interfaces/IMissionRepository.cs
- [ ] T014 Implement `JsonMissionRepository : IMissionRepository` (System.Text.Json, SemaphoreSlim file lock, re-derives readiness on load) in backend/MissionControl.Infrastructure/Persistence/JsonMissionRepository.cs
- [ ] T015 [P] Create all DTO classes: `CreateMissionDto`, `UpdateMissionDto`, `MissionSummaryDto`, `MissionListItemDto`, `WarningDto`, `ReferenceDataDto` in backend/MissionControl.Api/DTOs/
- [ ] T016 Register `IMissionRepository` → `JsonMissionRepository`, configure `JsonStorageOptions`, add CORS policy, and configure JSON enums-as-strings in backend/MissionControl.Api/Program.cs

### Frontend — Shared Models & Services

- [ ] T017 [P] Create TypeScript models (`ReadinessState`, `MissionControlMode`, `WarningType`, `Warning`, `MissionListItem`, `MissionSummary`, `CreateMissionRequest`, `UpdateMissionRequest`) in frontend/src/app/models/mission.model.ts
- [ ] T018 [P] Implement `MissionService` with all six methods (getAll, getById, create, update, delete, getReferenceData) using `HttpClient` in frontend/src/app/services/mission.service.ts
- [ ] T019 [P] Create standalone `WarningBadgeComponent` (red indicator box, `@Input() warning: Warning`, blocking vs advisory visual distinction) in frontend/src/app/components/shared/warning-badge/warning-badge.component.ts and .html

**Checkpoint**: `dotnet test` passes all domain tests; `ng build` passes; `GET /api/missions/reference-data` not yet wired (Phase 3)

---

## Phase 3: User Story 1 — Create and Evaluate a Mission (Priority: P1) 🎯 MVP

**Goal**: Users can fill in a mission form, submit it, and immediately see the evaluated readiness state and any active warnings

**Independent Test**: Open `/missions/new`; create a crewed mission (Mun, Landing, 5200 / 4500 ΔV, Jebediah Kerman); submit; verify "Ready" state with no warnings. Then edit ΔV to 4000; verify "Not Ready" with two red warning boxes.

### Backend

- [ ] T020 Create `Mission` aggregate root (`Create()` and `Update()` factories with full validation: required fields, positive ΔV, conditional crew/probe, time-range check; calls `ReadinessCalculator`) in backend/MissionControl.Domain/Entities/Mission.cs
- [ ] T021 Create `MissionTests` covering: Create→Ready, Create→AtRisk, Create→NotReady (ΔV), Create→NotReady (MissingCrew), Create→NotReady (ProbeMode+no core), Create with End MT < Start MT → blocking warning, Create with End MT set and no Start MT → advisory warning, Update re-evaluates readiness in backend/MissionControl.Tests/Domain/MissionTests.cs
- [ ] T022 Implement `GET /api/missions/reference-data` returning predefined body, type, and probe core lists in backend/MissionControl.Api/Controllers/MissionsController.cs
- [ ] T023 [P] Implement `POST /api/missions` (validate DTO, check name uniqueness, create Mission aggregate, persist, return 201 MissionSummaryDto) in backend/MissionControl.Api/Controllers/MissionsController.cs
- [ ] T024 [P] Implement `GET /api/missions/{id}` (load, re-derive readiness, return MissionSummaryDto or 404) in backend/MissionControl.Api/Controllers/MissionsController.cs
- [ ] T024a [US1] Create `MissionsControllerTests` (NSubstitute mock of `IMissionRepository`; test POST 201/400/409, GET/{id} 200/404, PUT 200/400/404/409, DELETE 204/404 happy paths and error cases) in backend/MissionControl.Tests/Api/MissionsControllerTests.cs

### Frontend

- [ ] T025 Implement `MissionFormComponent` (create mode): all fields, `controlMode` toggle showing/hiding crew-list vs probe-core section, crew member add/remove, Kerbin Time inputs for start/end MT, validation, calls `MissionService.create()` in frontend/src/app/components/mission-form/mission-form.component.ts and .html
- [ ] T026 [P] Implement `MissionSummaryComponent`: displays all `MissionSummary` fields, readiness state badge (colour-coded), `WarningBadgeComponent` list, `KerbinTime` formatted display for mission times in frontend/src/app/components/mission-summary/mission-summary.component.ts and .html
- [ ] T027 [P] Create `KerbinTimePipe` (`transform(totalSeconds: number | null): string`) formatting `long` → `Yy, Dd, Hh, Mm, Ss` in frontend/src/app/pipes/kerbin-time.pipe.ts
- [ ] T028 Configure Angular routes (`/` → stub list, `/missions/new` → `MissionFormComponent`, `/missions/:id` → `MissionSummaryComponent`) and wire form submit → navigate to `/missions/:id` in frontend/src/app/app.routes.ts

**Checkpoint**: `/missions/new` fully functional end-to-end. Creation, readiness evaluation, and summary display all work independently of other user stories.

---

## Phase 4: User Story 2 — View Mission Summary and Readiness Results (Priority: P2)

**Goal**: Users can navigate directly to a saved mission's summary page and see all parameters, readiness state, and warnings at a glance

**Independent Test**: With a saved AtRisk mission, navigate to `/missions/:id`; verify: all fields displayed, reserve margin % shown, "At Risk" badge visible, `LowReserveMargin` red warning box visible, Kerbin times displayed in `Yy, Dd, Hh, Mm, Ss` format.

### Backend

All required endpoints (GET /api/missions/{id}) were implemented in Phase 3. No new backend tasks.

### Frontend

- [ ] T029 Enhance `MissionSummaryComponent` with: reserve margin % display, full crew-member list (Crewed) or probe core name (Probe), start/end mission time rows (hidden when null), "Back to missions" navigation link in frontend/src/app/components/mission-summary/mission-summary.component.ts and .html
- [ ] T030 [P] Add `MissionSummaryComponent` route guard (redirect to `/` if mission id not found, show 404 message) in frontend/src/app/components/mission-summary/mission-summary.component.ts
- [ ] T031 [P] Add acceptance scenario coverage to `MissionSummaryComponent` for all three readiness states (ready, at-risk, not-ready with two simultaneous warnings) via Jasmine specs in frontend/src/app/components/mission-summary/mission-summary.component.spec.ts

**Checkpoint**: Navigating to `/missions/:id` shows a complete, accurate mission summary for all readiness states. Works independently without the list view.

---

## Phase 5: User Story 3 — View All Missions with Readiness States (Priority: P3)

**Goal**: Users can see all saved missions on a single page with readiness state, mode, crew/probe summary, and warning indicators

**Independent Test**: Create three missions (one Ready crewed, one AtRisk probe, one NotReady crewed with no crew); navigate to `/`; verify all three appear with correct readiness badges, mode labels, abbreviated crew/probe info, and warning indicator boxes.

### Backend

- [ ] T032 Implement `GET /api/missions` (load all, re-derive readiness for each, return `MissionListItemDto[]`) in backend/MissionControl.Api/Controllers/MissionsController.cs

### Frontend

- [ ] T033 Implement `MissionListComponent`: calls `MissionService.getAll()`, renders each mission as a card/row with name, readiness badge, controlMode, abbreviated crew summary (`"Jebediah +2"`) or probe core name, `WarningBadgeComponent` list, and link to `/missions/:id` in frontend/src/app/components/mission-list/mission-list.component.ts and .html
- [ ] T034 [P] Add empty-state message ("No missions planned yet. Create your first mission.") with a "New Mission" button to `MissionListComponent` when the missions array is empty in frontend/src/app/components/mission-list/mission-list.component.ts and .html
- [ ] T035 Wire `/` route to `MissionListComponent` and add "New Mission" button navigating to `/missions/new` in frontend/src/app/app.routes.ts

**Checkpoint**: `/` shows all missions with correct readiness states, warning boxes, and empty-state message. List is fully functional without depending on edit/delete.

---

## Phase 6: User Story 4 — Edit and Delete a Mission (Priority: P3)

**Goal**: Users can correct any mission field and immediately see readiness re-evaluated, or permanently remove a mission

**Independent Test**: Open an existing AtRisk mission summary; click "Edit"; change Available ΔV to a Ready value; save; verify readiness updates to "Ready". Then delete the mission; verify it disappears from the list. Verify editing a name to a duplicate shows a validation error without saving.

### Backend

- [ ] T036 Implement `PUT /api/missions/{id}` (validate, check name uniqueness excluding self, call Mission.Update, persist, return 200 MissionSummaryDto or 400/404/409) in backend/MissionControl.Api/Controllers/MissionsController.cs
- [ ] T037 [P] Implement `DELETE /api/missions/{id}` (find or 404, delete from store, return 204) in backend/MissionControl.Api/Controllers/MissionsController.cs

### Frontend

- [ ] T038 Add `/missions/:id/edit` route and extend `MissionFormComponent` with edit mode: pre-populate all fields from `MissionService.getById()`, call `MissionService.update()` on submit, navigate to `/missions/:id` on success in frontend/src/app/components/mission-form/mission-form.component.ts and frontend/src/app/app.routes.ts
- [ ] T039 [P] Add "Edit Mission" button to `MissionSummaryComponent` navigating to `/missions/:id/edit` in frontend/src/app/components/mission-summary/mission-summary.component.ts and .html
- [ ] T040 [P] Add "Delete Mission" button to `MissionSummaryComponent`: calls `MissionService.delete()` on confirmation, navigates to `/` on success in frontend/src/app/components/mission-summary/mission-summary.component.ts and .html
- [ ] T041 Handle `409 Conflict` response in `MissionFormComponent` to display duplicate-name validation error inline in frontend/src/app/components/mission-form/mission-form.component.ts

**Checkpoint**: Full CRUD loop works end-to-end. Edit immediately re-evaluates readiness. Delete removes mission from list. Duplicate name rejected with clear error.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that apply across all user stories

- [ ] T042 [P] Add Angular HTTP error interceptor (map API errors to user-friendly messages; surface 400/404/409 in affected components) in frontend/src/app/interceptors/error.interceptor.ts
- [ ] T043 [P] Add responsive CSS for desktop and tablet viewports (min-width breakpoints per NFR-001) in frontend/src/styles.css and component stylesheets
- [ ] T044 [P] Add XML doc comments to `ReadinessCalculator.Calculate()`, `Mission.Create()`, `Mission.Update()`, and `KerbinTime.ToDisplayString()` in respective backend/MissionControl.Domain/ files
- [ ] T045 Run quickstart.md validation end-to-end: scaffold → run → create crewed mission → create probe mission → edit ΔV → verify readiness changes → delete; confirm all smoke-test curl commands pass

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — **blocks all user stories**
- **Phase 3 (US1)**: Depends on Phase 2 — no dependency on other stories
- **Phase 4 (US2)**: Depends on Phase 2 — can start in parallel with US1 (no backend overlap); frontend depends on T026 from US1
- **Phase 5 (US3)**: Depends on Phase 2 — can start in parallel with US1/US2; one new backend endpoint (T032)
- **Phase 6 (US4)**: Depends on Phase 2 + T020 (Mission.Update uses the Mission aggregate) — can start in parallel once aggregate is defined
- **Phase 7 (Polish)**: Depends on all stories being functionally complete

### User Story Dependencies

- **US1 (P1)**: Foundational complete → independent; no story dependencies
- **US2 (P2)**: Foundational complete + T026 (MissionSummaryComponent base) → independent of US1 routing
- **US3 (P3)**: Foundational complete → independent; reuses WarningBadgeComponent (T019)
- **US4 (P3)**: Foundational complete + T020 (Mission aggregate) → independent; reuses MissionFormComponent (T025, edit mode extension)

### Within Each User Story

- Enums → value objects → domain service → aggregate → repository → controller → frontend service → components
- Backend tasks within a story can start as soon as the foundational layer is complete
- Frontend tasks within a story can start as soon as models (T017) and service (T018) are complete

### Parallel Opportunities

- T002, T004, T005 can all run in parallel with T001 (different files/projects)
- T007, T008, T009 can all run in parallel with T006 (different value object files)
- T010 can start in parallel with T011 once T009 is done
- T013, T015 can run in parallel with T012
- T017, T018, T019 can all run in parallel once T016 is complete
- T023 and T024 can run in parallel once T020 and T021 are complete
- T026, T027 can run in parallel with T025
- T036 and T037 can run in parallel
- T039 and T040 can run in parallel
- T042, T043, T044 can all run in parallel

---

## Parallel Example: User Story 1

```bash
# Backend track (after Phase 2 complete):
Task T020: Mission aggregate  →  Task T021: MissionTests
Task T022: GET reference-data  →  T023: POST /missions (parallel with T024)
Task T024: GET /missions/{id}

# Frontend track (after T017+T018+T019 complete):
Task T025: MissionFormComponent
Task T026: MissionSummaryComponent  (parallel with T025)
Task T027: KerbinTimePipe           (parallel with T025 and T026)
Task T028: Routing + wiring         (depends on T025 + T026)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (all domain tests must pass before proceeding)
3. Complete Phase 3: User Story 1 (T020–T028)
4. **STOP and VALIDATE**: Run quickstart smoke test for US1 end-to-end
5. Demo or deploy if ready

### Incremental Delivery

1. Setup + Foundational → ✅ all `dotnet test` domain tests pass
2. US1 → Create mission + see readiness → **MVP demo ready**
3. US2 → Full summary view with times and margin %
4. US3 → Mission list at `/`
5. US4 → Edit + delete
6. Polish → Error handling + responsive CSS

### Parallel Team Strategy

With two developers after Foundational is done:
- **Developer A**: US1 backend (T020–T024) then US3 backend (T032) then US4 backend (T036–T037)
- **Developer B**: US1 frontend (T025–T028) then US2 frontend (T029–T031) then US3 frontend (T033–T035) then US4 frontend (T038–T041)

---

## Notes

- `[P]` tasks = different files, no task-level dependencies — safe to run in parallel within the same phase
- `[US1]–[US4]` labels map each task to its user story for traceability
- Domain tests (T010, T012, T021) are **not optional** — Constitution Principle VI (NON-NEGOTIABLE)
- All `dotnet test` domain tests MUST pass before any user story controller work begins
- `ReadinessState` and `Warnings` are never stored in JSON — always re-derived on load
- `InvalidTimeRange` blocks save; `AdvisoryEndTimeWithoutStart` does not — handle both in `MissionFormComponent` (T025) and `Mission.Create` (T020)
- Commit after each phase checkpoint at minimum
