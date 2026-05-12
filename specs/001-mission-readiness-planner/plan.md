# Implementation Plan: Mission Readiness Planner

**Branch**: `001-mission-readiness-planner` | **Date**: 2026-05-12 | **Spec**: [spec.md](../001-mission-readiness-planner/spec.md)

**Input**: Feature specification from `specs/001-mission-readiness-planner/spec.md`

## Summary

A full-stack KSP mission readiness planner: an Angular TypeScript SPA (frontend) backed by an ASP.NET Core Web API (backend) that evaluates mission feasibility from delta-v inputs. Users create, view, edit, and delete missions; the system derives a readiness state (Ready / At Risk / Not Ready) and attaches red-box warnings for insufficient delta-v, low reserve margin, or missing fields. Mission data is persisted to a local JSON file store for the PoC, with the storage layer abstracted behind a repository interface ready for a SQL Server migration.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (backend) · TypeScript / Angular latest stable (frontend)

**Primary Dependencies**: ASP.NET Core Web API · Angular · NUnit (backend tests) · Karma + Jasmine (frontend tests)

**Storage**: Local JSON file store (PoC) — abstracted behind `IMissionRepository`; planned migration to SQL Server post-PoC

**Testing**: NUnit — domain unit tests (backend) · Karma + Jasmine — component/service unit tests (frontend)

**Target Platform**: Web browser — desktop and tablet viewport sizes

**Project Type**: Full-stack web application (Angular SPA + .NET REST API)

**Performance Goals**: Readiness calculation instantaneous (pure in-memory computation, no I/O on critical path)

**Constraints**: No authentication; single-user; JSON file store for PoC; API contract MUST be storage-agnostic; phone viewports out of scope

**Scale/Scope**: Single-user PoC; dozens of missions; no concurrency requirements

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Modular Architecture | ✅ PASS | Angular SPA and .NET Web API are fully separate layers with no shared state |
| II. Component-Driven Frontend | ✅ PASS | UI delivered as discrete standalone Angular components (mission-list, mission-form, mission-summary) |
| III. Domain-Driven Backend | ✅ PASS | Domain layer: `Mission` aggregate, `ReadinessState` enum, `Warning` value object, `ReadinessCalculator` domain service |
| IV. Business Logic Isolation | ✅ PASS | Readiness calculation lives exclusively in the domain layer; controllers map DTOs only |
| V. Deterministic Calculations | ✅ PASS | `ReadinessCalculator` is a pure function — same delta-v inputs always produce the same state and warnings |
| VI. Unit Test Coverage | ✅ PASS | `ReadinessCalculator` and `Mission` aggregate MUST have full unit test coverage including boundary and edge cases |
| VII. Readability First | ✅ PASS | No premature optimization; simple JSON file I/O; no over-engineering for PoC scope |
| VIII. Minimal Dependencies | ✅ PASS | No third-party libraries beyond the mandated stack (Angular, ASP.NET Core, NUnit, Karma+Jasmine) |
| IX. Purposeful Documentation | ✅ PASS | XML doc comments on public API surface and non-obvious domain logic only |

**Gate result**: All principles pass. Proceeding to Phase 0.

## Project Structure

### Documentation (this feature)

```text
specs/001-mission-readiness-planner/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── api.md           # Phase 1 output — REST API contract
└── tasks.md             # Phase 2 output (speckit.tasks — NOT created by speckit.plan)
```

### Source Code (repository root)

```text
backend/
├── MissionControl.Domain/
│   ├── Entities/
│   │   └── Mission.cs
│   ├── ValueObjects/
│   │   ├── ReadinessState.cs
│   │   ├── Warning.cs
│   │   ├── TargetBody.cs
│   │   └── MissionType.cs
│   ├── Services/
│   │   └── ReadinessCalculator.cs
│   └── Interfaces/
│       └── IMissionRepository.cs
├── MissionControl.Infrastructure/
│   └── Persistence/
│       └── JsonMissionRepository.cs
├── MissionControl.Api/
│   ├── Controllers/
│   │   └── MissionsController.cs
│   ├── DTOs/
│   │   ├── CreateMissionDto.cs
│   │   ├── UpdateMissionDto.cs
│   │   ├── MissionSummaryDto.cs
│   │   └── MissionListItemDto.cs
│   └── Program.cs
└── MissionControl.Tests/
    ├── Domain/
    │   ├── ReadinessCalculatorTests.cs
    │   └── MissionTests.cs
    └── Api/
        └── MissionsControllerTests.cs

frontend/
├── src/
│   └── app/
│       ├── components/
│       │   ├── mission-list/
│       │   │   ├── mission-list.component.ts
│       │   │   ├── mission-list.component.html
│       │   │   └── mission-list.component.spec.ts
│       │   ├── mission-form/
│       │   │   ├── mission-form.component.ts
│       │   │   ├── mission-form.component.html
│       │   │   └── mission-form.component.spec.ts
│       │   └── mission-summary/
│       │       ├── mission-summary.component.ts
│       │       ├── mission-summary.component.html
│       │       └── mission-summary.component.spec.ts
│       ├── models/
│       │   └── mission.model.ts
│       └── services/
│           ├── mission.service.ts
│           └── mission.service.spec.ts
└── angular.json
```

**Structure Decision**: Option 2 (Web Application) — `backend/` hosts the .NET solution with four projects (Domain, Infrastructure, Api, Tests); `frontend/` hosts the Angular SPA. The four-project backend structure is mandated by the constitution's DDD requirement (Principle III) and the storage-layer abstraction needed for the JSON→SQL Server migration path.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|--------------------------------------|
| 4 backend projects (vs. 1) | Constitution Principle III mandates domain isolation; Infrastructure must be separated to enable SQL Server migration without touching the domain or API | A single project would couple `ReadinessCalculator` to `JsonMissionRepository`, violating Principle IV and making the migration path invasive |
