# Research: Mission Readiness Planner

**Phase**: 0 | **Date**: 2026-05-12 | **Plan**: [plan.md](plan.md)

## Research Tasks

The Technical Context had no NEEDS CLARIFICATION markers. Research focused on confirming best practices for the four key design decisions implied by the feature and constitution constraints.

---

### 1. JSON File Persistence in ASP.NET Core (Repository Pattern)

**Decision**: Implement a `JsonMissionRepository` class in `MissionControl.Infrastructure` that reads/writes a single `missions.json` file using `System.Text.Json`. The file path is injected via `IOptions<JsonStorageOptions>` from `appsettings.json`. The class implements `IMissionRepository` defined in the domain layer — the API project and domain layer have zero knowledge of the file system.

**Rationale**: `System.Text.Json` is built into .NET 8 with no additional dependencies (Principle VIII). The repository interface boundary ensures the JSON file is a pluggable adapter, not a hard dependency — fulfilling the SQL Server migration path requirement and Principle I (modular architecture). Using `IOptions<T>` for the file path avoids hardcoding and enables environment-specific configuration without code changes.

**Alternatives Considered**:
- *LiteDB / SQLite for PoC*: Would work but introduces a third-party dependency (Principle VIII violation) with no additional value for a single-user PoC.
- *In-memory dictionary*: Rejected because the spec explicitly requires sessions to be persisted.
- *EF Core with SQLite*: Heavier than needed for PoC; EF Core also pulls in additional packages.

**Boundary Condition**: Concurrent write safety is out of scope for the single-user PoC. The repository may use a simple file lock (`SemaphoreSlim`) to protect against unlikely race conditions from rapid API calls, without investing in full concurrency infrastructure.

---

### 2. Angular Standalone Components (Latest Angular)

**Decision**: Use Angular standalone components (no `NgModule`). All components (`MissionListComponent`, `MissionFormComponent`, `MissionSummaryComponent`) are declared with `standalone: true` and imported directly into the root `AppComponent` or lazy-loaded routes as needed.

**Rationale**: Standalone components are the default and recommended approach in Angular v17+. They eliminate `NgModule` boilerplate, improve tree-shaking, and align with Angular's official guidance. Using `NgModule` in a new project targeting the latest stable Angular would be working against the framework's direction.

**Alternatives Considered**:
- *NgModule-based architecture*: Rejected — NgModules are a legacy pattern in Angular v17+; new projects should not use them.
- *React / Vue*: Rejected — constitution mandates Angular (Technology Stack constraint).

---

### 3. KSP Stock Celestial Bodies and Mission Types

**Decision**: The following predefined lists will be used for the hybrid dropdown fields. An "Other (custom)" option at the end of each list allows free-text entry for modded content.

**Target Bodies** (KSP stock solar system):
- Kerbol (Sun)
- Moho
- Eve
- Gilly
- Kerbin
- Mun
- Minmus
- Duna
- Ike
- Dres
- Jool
- Laythe
- Vall
- Tylo
- Bop
- Pol
- Eeloo
- Other (custom)

**Mission Types** (KSP common mission categories):
- Orbital
- Landing
- Flyby
- Transfer
- Rescue
- Station Resupply
- Return
- Other (custom)

**Rationale**: These are the complete stock KSP2-era and KSP1 celestial bodies. Encoding them as a server-side constant avoids magic strings scattered across layers. The "Other" option is the escape hatch for Outer Planets Mod (OPM), JNSQ, Galileo's Planet Pack, and similar mods.

**Alternatives Considered**:
- *Hardcode only Tier 1 bodies (Kerbin, Mun, Duna)*: Too restrictive for real planning use.
- *Free text only*: Rejected during clarification in favour of the hybrid approach.

---

### 4. DDD Layering for a Simple CRUD + Calculation Domain in .NET 8

**Decision**: Use a lightweight DDD structure with four projects as documented in `plan.md`. The `Mission` class is a rich aggregate root with encapsulated validation and state derivation (not an anemic model). `ReadinessCalculator` is a stateless domain service — a static class with a single `Calculate(double availableDv, double requiredDv): ReadinessResult` method.

**Rationale**: Constitution Principle III mandates DDD layering and Principle IV mandates business logic isolation. The rich aggregate model keeps `ReadinessState` and `Warnings` derived from `Mission`'s own data, making the aggregate the single source of truth. A stateless calculator service cleanly satisfies Principle V (deterministic calculations).

**Key design decisions**:
- `Mission.Id` is a `Guid` generated at creation — opaque, storage-agnostic identity.
- `ReadinessState` is an `enum` (not a string) to prevent invalid states at compile time.
- `Warning` is a value object with a `WarningType` enum and a `Message` string — enables multiple warnings per mission and makes warning types independently testable.
- `TargetBody` and `MissionType` are represented as a small value object pair: `(string Value, bool IsCustom)` — the predefined list is validated in the domain; custom values pass through with `IsCustom = true`.
- Controllers return strongly typed DTOs only (Principle III) — no anonymous objects, no raw entity exposure.

**Alternatives Considered**:
- *Anemic domain model*: Rejected — would push business logic into services or controllers (Principle IV violation).
- *Single project with folders*: Rejected — would couple infrastructure to domain, blocking the SQL Server migration (Principle I violation; Complexity Tracking entry in plan.md justifies the 4-project structure).
- *MediatR / CQRS*: Rejected — over-engineering for a single aggregate PoC (Principle VII).
