# Research: Mission Readiness Planner

**Phase**: 0 | **Date**: 2026-05-12 (updated after amendment) | **Plan**: [plan.md](plan.md)

## Research Tasks

Research covers the original four design decisions plus three additional topics introduced by the spec amendment (NUnit, MissionControlMode conditional validation, KerbinTime encoding, warning severity model).

---

### 1. JSON File Persistence in ASP.NET Core (Repository Pattern)

**Decision**: Implement `JsonMissionRepository : IMissionRepository` in `MissionControl.Infrastructure`. Reads/writes a single `missions.json` via `System.Text.Json`. File path injected via `IOptions<JsonStorageOptions>` from `appsettings.json`. Domain and API layers have zero knowledge of the file system.

**Rationale**: `System.Text.Json` is in-box (.NET 8); no additional packages needed (Principle VIII). Repository interface ensures the JSON file is a pluggable adapter — fulfilling the SQL Server migration path and Principle I. `IOptions<T>` avoids hardcoding and enables environment-specific config without code changes.

**Alternatives Considered**:
- *LiteDB / SQLite*: Third-party dependency, no additional PoC value (Principle VIII violation).
- *In-memory dictionary*: Rejected — spec requires session persistence.
- *EF Core with SQLite*: Over-engineered for PoC; additional packages.

**Boundary Condition**: `SemaphoreSlim(1,1)` file lock protects against concurrent API requests. No full concurrency infrastructure needed for single-user PoC.

---

### 2. Angular Standalone Components (Latest Angular)

**Decision**: All components (`MissionListComponent`, `MissionFormComponent`, `MissionSummaryComponent`) use `standalone: true`. No `NgModule`.

**Rationale**: Default approach in Angular v17+. Eliminates boilerplate, improves tree-shaking, aligns with Angular's direction. Using NgModules in a new project would be working against the framework.

**Alternatives Considered**:
- *NgModule-based*: Legacy pattern in Angular v17+; rejected.
- *React / Vue*: Constitution mandates Angular (Technology Stack constraint).

---

### 3. KSP Stock Celestial Bodies, Mission Types, and Probe Cores

**Target Bodies** (KSP stock): Kerbol, Moho, Eve, Gilly, Kerbin, Mun, Minmus, Duna, Ike, Dres, Jool, Laythe, Vall, Tylo, Bop, Pol, Eeloo, Other (custom)

**Mission Types**: Orbital, Landing, Flyby, Transfer, Rescue, Station Resupply, Return, Other (custom)

**Probe Cores** (KSP stock): Stayputnik, Probodobodyne OKTO, Probodobodyne HECS, Probodobodyne QBE, Probodobodyne OKTO2, Probodobodyne HECS2, Probodobodyne RoveMate, RC-001S Remote Guidance Unit, RC-L01 Remote Guidance Unit, MK2 Drone Core, Other (custom)

**Decision**: Encode all three lists as domain-layer constants. The "Other (custom)" option is injected by the frontend UI — it is not part of the `GET /api/missions/reference-data` response. When a custom value is submitted, `IsCustom: true` signals the backend to skip predefined-list validation.

**Rationale**: Avoids magic strings scattered across layers. Constants in the domain layer keep the lists as the single source of truth. The escape hatch supports Outer Planets Mod, JNSQ, Galileo's Planet Pack, and similar mods.

---

### 4. DDD Layering for .NET 8

**Decision**: Four-project solution as documented in `plan.md`. `Mission` is a rich aggregate root. `ReadinessCalculator` is a stateless domain service. `KerbinTime` and `ProbeCoreValue` are value objects.

**Key design decisions**:
- `Mission.Id` is a `Guid` generated at creation.
- `ReadinessState` is an `enum` (compile-time safety).
- `Warning` is a value object with `WarningType`, `Message`, and `IsBlocking` — enables independent accumulation of blocking and advisory warnings.
- `KerbinTime` is a `readonly record struct` wrapping `long TotalSeconds` with conversion and formatting logic.
- `MissionControlMode` is a two-value enum (`Crewed | Probe`); conditional field requirements are enforced inside the `Mission.Create/Update` factory, not in the controller.
- `ReadinessCalculator` signature: `Calculate(double availableDv, double requiredDv, MissionControlMode mode, IReadOnlyList<string> crewMembers) : ReadinessResult`.

**Alternatives Considered**:
- *Anemic domain model*: Rejected — pushes business logic to services/controllers (Principle IV violation).
- *Single project*: Rejected — couples infrastructure to domain (Principle I violation).
- *MediatR / CQRS*: Rejected — over-engineering for a single aggregate PoC (Principle VII).

---

### 5. NUnit 4 as Backend Testing Framework (Constitution Amendment v1.2.0)

**Decision**: Use NUnit 4 with FluentAssertions for readable assertions and NSubstitute for repository mocking. Replace any prior xUnit references.

**Rationale**: NUnit is the constitution-mandated framework as of v1.2.0. NUnit 4 is the current major release targeting .NET 8. FluentAssertions and NSubstitute are minimal, well-understood companions that satisfy Principle VIII (justified value; no alternatives provide the same assertion fluency or interface-mocking capability without similar dependency weight).

**NUnit 4 test structure for domain tests**:
```csharp
[TestFixture]
public class ReadinessCalculatorTests
{
    [Test]
    public void Calculate_WhenAvailableExceedsRequired_ReturnsReady() { ... }

    [TestCase(5000, 5000, ReadinessState.AtRisk)]
    [TestCase(4999, 5000, ReadinessState.NotReady)]
    public void Calculate_BoundaryConditions(double available, double required, ReadinessState expected) { ... }
}
```

**Alternatives Considered**:
- *xUnit*: Previously specified; superseded by constitution amendment v1.2.0.
- *MSTest*: Less ergonomic; not standard for new .NET projects.

---

### 6. KerbinTime Value Object Encoding

**Decision**: Store as `long TotalSeconds` (total Kerbin seconds since mission epoch T=0). Display as `Yy, Dd, Hh, Mm, Ss`. Transmitted over the API as a plain `long` (integer seconds). Frontend formats for display using a shared utility function.

**Kerbin calendar constants**:
- 1 minute = 60 s
- 1 hour = 60 min = 3,600 s
- 1 Kerbin day = 6 hours = 21,600 s
- 1 Kerbin year = 426 days = 9,201,600 s

**Decomposition algorithm**:
```
years  = totalSeconds / 9,201,600
rem    = totalSeconds % 9,201,600
days   = rem / 21,600
rem    = rem % 21,600
hours  = rem / 3,600
rem    = rem % 3,600
minutes = rem / 60
seconds = rem % 60
```

**Rationale**: A `long` is the simplest lossless representation with no ambiguity about calendar drift or leap seconds (KSP has none). Storing formatted strings would couple the storage layer to a display decision. Transmitting as integer keeps the API contract storage- and locale-agnostic.

**Validation**: `TotalSeconds >= 0` (negative mission time is invalid). `EndMissionTime.TotalSeconds > StartMissionTime.TotalSeconds` when both are set (enforced in `Mission.Update`).

**Alternatives Considered**:
- *ISO 8601 string*: Real-world time format, not KSP-native — would require client-side conversion and adds confusion.
- *Separate year/day/hour/minute/second fields*: More fields to validate and store; single `long` is simpler and faster to compare.

---

### 7. Warning Severity Model (Blocking vs. Advisory)

**Decision**: `Warning` value object gains an `IsBlocking` boolean property. Blocking warnings (`true`) prevent the mission from being saved. Advisory warnings (`false`) are informational only — the mission saves normally.

**Blocking warnings** (prevent save):
- `InsufficientDeltaV`
- `LowReserveMargin` — note: this does NOT block save on its own; a mission with AtRisk state (low margin but sufficient ΔV) CAN be saved
- `MissingRequiredField`
- `MissingCrew` (Crewed mode, empty crew list)
- `InvalidTimeRange` (End MT < Start MT)

**Correction**: `LowReserveMargin` on its own (AtRisk) is NOT blocking — the mission is valid and can be saved. Only `InsufficientDeltaV` (NotReady) is blocking from the delta-v perspective. Clarifying the blocking rules:

| Warning | Blocks Save? | Reason |
|---------|-------------|--------|
| `InsufficientDeltaV` | Yes | Mission is not viable |
| `LowReserveMargin` | No | Mission is marginal but viable (AtRisk); user's informed choice |
| `MissingRequiredField` | Yes | Required data absent |
| `MissingCrew` | Yes | Crewed mission with no crew is not launchable |
| `InvalidTimeRange` | Yes | End MT before Start MT is logically invalid |
| `AdvisoryEndTimeWithoutStart` | No | End MT set without Start MT is unusual but not invalid |

**Rationale**: Distinguishing blocking from advisory prevents over-validation. An "At Risk" mission should be saveable — the user may intentionally accept the risk. Forcing a block on low-margin missions would make the planner unusable for real KSP scenarios where margins are tight by design.

