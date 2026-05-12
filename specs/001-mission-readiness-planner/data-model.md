# Data Model: Mission Readiness Planner

**Phase**: 1 | **Date**: 2026-05-12 (updated after amendment) | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

## Domain Layer (`MissionControl.Domain`)

### Aggregate Root: `Mission`

The central domain object. Encapsulates all mission state and derives `ReadinessState` and `Warnings` from its own data via `ReadinessCalculator`. Never exposes mutable state directly.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | Required; generated at creation | Opaque identity; storage-agnostic |
| `Name` | `string` | Required; 1–200 chars; unique in repository | Uniqueness enforced at repository + API layer |
| `TargetBody` | `KspBodyValue` | Required; predefined list or custom | See value object below |
| `MissionType` | `KspTypeValue` | Required; predefined list or custom | See value object below |
| `AvailableDeltaV` | `double` | Required; > 0 (m/s) | Validated in factory/constructor |
| `RequiredDeltaV` | `double` | Required; > 0 (m/s) | Validated in factory/constructor |
| `ControlMode` | `MissionControlMode` | Required; `Crewed` or `Probe` | Determines which conditional fields are required |
| `CrewMembers` | `IReadOnlyList<string>` | Crewed mode: min 1 entry required; Probe mode: must be empty | Each entry: non-empty string |
| `ProbeCore` | `KspBodyValue?` | Probe mode: required; Crewed mode: null | Predefined KSP probe core or custom |
| `StartMissionTime` | `KerbinTime?` | Optional | Null if not recorded |
| `EndMissionTime` | `KerbinTime?` | Optional; if set, must be > StartMissionTime when StartMissionTime is also set | Null if not recorded |
| `ReadinessState` | `ReadinessState` | Derived; never set directly | Calculated by `ReadinessCalculator` |
| `Warnings` | `IReadOnlyList<Warning>` | Derived; never set directly | Both blocking and advisory warnings included |

**Factory method**: `Mission.Create(...)` — validates all inputs, throws `DomainException` for invalid values, calls `ReadinessCalculator` to compute initial readiness.

**Update method**: `Mission.Update(...)` — re-validates and recalculates readiness atomically.

---

### Enum: `MissionControlMode`

| Value | Meaning |
|-------|---------|
| `Crewed` | Mission requires kerbal crew; `CrewMembers` must contain at least one name |
| `Probe` | Mission is uncrewed; `ProbeCore` must be specified |

---

### Enum: `ReadinessState`

| Value | Condition |
|-------|-----------|
| `Ready` | `availableDv >= requiredDv × 1.10` AND crew/probe requirements met |
| `AtRisk` | `availableDv >= requiredDv` AND `availableDv < requiredDv × 1.10` AND crew/probe requirements met |
| `NotReady` | `availableDv < requiredDv` OR crew/probe requirements not met |

**Note**: `MissingCrew` (Crewed with empty crew) always forces `NotReady` regardless of delta-v values.

---

### Enum: `WarningType`

| Value | Blocking? | Trigger |
|-------|-----------|---------|
| `InsufficientDeltaV` | Yes | `availableDv < requiredDv` |
| `LowReserveMargin` | No | Reserve margin < 10% (fires independently of `InsufficientDeltaV`) |
| `MissingRequiredField` | Yes | Any required field null/empty/invalid at save time |
| `MissingCrew` | Yes | `ControlMode = Crewed` and `CrewMembers` is empty |
| `InvalidTimeRange` | Yes | `EndMissionTime` is set and `StartMissionTime` is set and End ≤ Start |
| `AdvisoryEndTimeWithoutStart` | No | `EndMissionTime` is set but `StartMissionTime` is null |

---

### Value Object: `Warning`

| Field | Type | Notes |
|-------|------|-------|
| `Type` | `WarningType` | Enum identifying the warning category |
| `Message` | `string` | Human-readable warning text |
| `IsBlocking` | `bool` | `true` → prevents save; `false` → advisory only |

Multiple warnings can be active simultaneously. Blocking and advisory warnings are accumulated independently.

---

### Value Object: `KspBodyValue`  
*(used for TargetBody, MissionType, and ProbeCore — same shape)*

| Field | Type | Notes |
|-------|------|-------|
| `Value` | `string` | The selected name (e.g., "Mun", "Landing", "OKTO") |
| `IsCustom` | `bool` | `true` when "Other" was chosen and user entered a custom name |

Predefined validation is skipped when `IsCustom = true`.

**Predefined TargetBody values**: Kerbol, Moho, Eve, Gilly, Kerbin, Mun, Minmus, Duna, Ike, Dres, Jool, Laythe, Vall, Tylo, Bop, Pol, Eeloo

**Predefined MissionType values**: Orbital, Landing, Flyby, Transfer, Rescue, Station Resupply, Return

**Predefined ProbeCore values**: Stayputnik, Probodobodyne OKTO, Probodobodyne HECS, Probodobodyne QBE, Probodobodyne OKTO2, Probodobodyne HECS2, Probodobodyne RoveMate, RC-001S Remote Guidance Unit, RC-L01 Remote Guidance Unit, MK2 Drone Core

---

### Value Object: `KerbinTime`

A point in KSP in-game time. Stored as total Kerbin seconds.

| Field | Type | Notes |
|-------|------|-------|
| `TotalSeconds` | `long` | Total seconds since T=0; must be ≥ 0 |

**Decomposition** (for display):
```
years   = TotalSeconds / 9_201_600
days    = (TotalSeconds % 9_201_600) / 21_600
hours   = (TotalSeconds % 21_600) / 3_600
minutes = (TotalSeconds % 3_600) / 60
seconds = TotalSeconds % 60
```

**Display format**: `{years}y, {days}d, {hours}h, {minutes}m, {seconds}s`  
Example: `1y, 42d, 3h, 15m, 0s`

**Kerbin constants**: 1 day = 6 h = 21,600 s · 1 year = 426 d = 9,201,600 s

**C# declaration**: `readonly record struct KerbinTime(long TotalSeconds)` — value equality, immutable, stack-allocated.

---

### Domain Service: `ReadinessCalculator`

Pure, stateless, no I/O. Satisfies Constitution Principle V.

```csharp
ReadinessResult Calculate(
    double availableDv,
    double requiredDv,
    MissionControlMode controlMode,
    IReadOnlyList<string> crewMembers)
```

**Returns**: `ReadinessResult { ReadinessState State, IReadOnlyList<Warning> Warnings, double ReserveMarginPercent }`

**Algorithm**:
1. Compute `reserveMargin = (availableDv - requiredDv) / requiredDv × 100`
2. Accumulate warnings independently:
   - If `availableDv < requiredDv` → add `InsufficientDeltaV` (blocking)
   - If `reserveMargin < 10` → add `LowReserveMargin` (non-blocking)
   - If `controlMode = Crewed` AND `crewMembers.Count = 0` → add `MissingCrew` (blocking)
3. Derive `ReadinessState`:
   - Any blocking warning present → `NotReady`
   - `LowReserveMargin` warning only → `AtRisk`
   - No warnings → `Ready`

**Note**: Mission time warnings (`InvalidTimeRange`, `AdvisoryEndTimeWithoutStart`) are evaluated by the `Mission` aggregate directly, not by `ReadinessCalculator`, because they do not affect readiness state.

---

### Interface: `IMissionRepository`

Defined in domain layer. Implemented in `MissionControl.Infrastructure`.

```csharp
Task<IReadOnlyList<Mission>> GetAllAsync();
Task<Mission?> GetByIdAsync(Guid id);
Task<Mission?> GetByNameAsync(string name);
Task AddAsync(Mission mission);
Task UpdateAsync(Mission mission);
Task DeleteAsync(Guid id);
```

---

## Infrastructure Layer (`MissionControl.Infrastructure`)

### `JsonMissionRepository : IMissionRepository`

Reads/writes `missions.json`. Uses `System.Text.Json`. File path via `IOptions<JsonStorageOptions>`. Uses `SemaphoreSlim(1,1)` for file access protection.

**What is persisted** (stored in JSON):
- `Id`, `Name`, `TargetBody`, `MissionType`, `AvailableDeltaV`, `RequiredDeltaV`
- `ControlMode`, `CrewMembers`, `ProbeCore`
- `StartMissionTime` (as `long?` — total seconds, or null)
- `EndMissionTime` (as `long?` — total seconds, or null)

**What is NOT persisted**: `ReadinessState`, `Warnings` — always re-derived on load from stored inputs. This ensures the calculation is always the canonical source of truth.

**JSON shape** (internal — not the API contract):
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Mun Landing Alpha",
    "targetBody": { "value": "Mun", "isCustom": false },
    "missionType": { "value": "Landing", "isCustom": false },
    "availableDeltaV": 5200.0,
    "requiredDeltaV": 4500.0,
    "controlMode": "Crewed",
    "crewMembers": ["Jebediah Kerman", "Bill Kerman"],
    "probeCore": null,
    "startMissionTime": 9244800,
    "endMissionTime": 9417600
  }
]
```

---

## API Layer (`MissionControl.Api`) — DTOs

### `CreateMissionDto` / `UpdateMissionDto`

```
string Name
string TargetBodyValue
bool TargetBodyIsCustom
string MissionTypeValue
bool MissionTypeIsCustom
double AvailableDeltaV
double RequiredDeltaV
string ControlMode                  // "Crewed" | "Probe"
string[] CrewMembers                // required (can be empty) when ControlMode = "Crewed"
string? ProbeCoreValue              // required when ControlMode = "Probe"
bool ProbeCoreIsCustom              // required when ControlMode = "Probe"
long? StartMissionTime              // total Kerbin seconds; null if not set
long? EndMissionTime                // total Kerbin seconds; null if not set
```

### `MissionSummaryDto`

```
Guid Id
string Name
string TargetBodyValue
bool TargetBodyIsCustom
string MissionTypeValue
bool MissionTypeIsCustom
double AvailableDeltaV
double RequiredDeltaV
double ReserveMarginPercent
string ReadinessState               // "Ready" | "AtRisk" | "NotReady"
string ControlMode                  // "Crewed" | "Probe"
string[] CrewMembers                // populated when ControlMode = "Crewed"
string? ProbeCoreValue              // populated when ControlMode = "Probe"
bool ProbeCoreIsCustom
long? StartMissionTime
long? EndMissionTime
WarningDto[] Warnings
```

### `MissionListItemDto`

```
Guid Id
string Name
string ReadinessState               // "Ready" | "AtRisk" | "NotReady"
string ControlMode                  // "Crewed" | "Probe"
string? CrewSummary                 // e.g., "Jebediah +2" or null when Probe
string? ProbeCoreValue              // probe core name or null when Crewed
WarningDto[] Warnings
```

### `WarningDto`

```
string Type        // "InsufficientDeltaV" | "LowReserveMargin" | "MissingRequiredField" | "MissingCrew" | "InvalidTimeRange" | "AdvisoryEndTimeWithoutStart"
string Message
bool IsBlocking
```

### `ReferenceDataDto`

```
string[] TargetBodies
string[] MissionTypes
string[] ProbeCores
```

---

## Frontend Models (`frontend/src/app/models/mission.model.ts`)

```typescript
export type ReadinessState = 'Ready' | 'AtRisk' | 'NotReady';
export type MissionControlMode = 'Crewed' | 'Probe';
export type WarningType =
  | 'InsufficientDeltaV'
  | 'LowReserveMargin'
  | 'MissingRequiredField'
  | 'MissingCrew'
  | 'InvalidTimeRange'
  | 'AdvisoryEndTimeWithoutStart';

export interface Warning {
  type: WarningType;
  message: string;
  isBlocking: boolean;
}

export interface MissionListItem {
  id: string;
  name: string;
  readinessState: ReadinessState;
  controlMode: MissionControlMode;
  crewSummary: string | null;    // e.g., "Jebediah +2" — null when Probe
  probeCoreValue: string | null; // probe core name — null when Crewed
  warnings: Warning[];
}

export interface MissionSummary {
  id: string;
  name: string;
  targetBodyValue: string;
  targetBodyIsCustom: boolean;
  missionTypeValue: string;
  missionTypeIsCustom: boolean;
  availableDeltaV: number;
  requiredDeltaV: number;
  reserveMarginPercent: number;
  readinessState: ReadinessState;
  controlMode: MissionControlMode;
  crewMembers: string[];
  probeCoreValue: string | null;
  probeCoreIsCustom: boolean;
  startMissionTime: number | null;   // total Kerbin seconds
  endMissionTime: number | null;     // total Kerbin seconds
  warnings: Warning[];
}

export interface CreateMissionRequest {
  name: string;
  targetBodyValue: string;
  targetBodyIsCustom: boolean;
  missionTypeValue: string;
  missionTypeIsCustom: boolean;
  availableDeltaV: number;
  requiredDeltaV: number;
  controlMode: MissionControlMode;
  crewMembers: string[];
  probeCoreValue: string | null;
  probeCoreIsCustom: boolean;
  startMissionTime: number | null;
  endMissionTime: number | null;
}

export type UpdateMissionRequest = CreateMissionRequest;
```

---

## Readiness State Transition Diagram

```
[Created / Updated]
       │
       ▼
ReadinessCalculator.Calculate(availableDv, requiredDv, controlMode, crewMembers)
       │
       ├─ Blocking warning(s) present ──────────────────────► NotReady
       │   • InsufficientDeltaV  (availableDv < requiredDv)    + all active blocking warnings
       │   • MissingCrew         (Crewed + empty crew list)    + LowReserveMargin if margin < 10%
       │
       ├─ LowReserveMargin only (no blocking warnings) ──────► AtRisk
       │   (margin < 10% but availableDv ≥ requiredDv,         + LowReserveMargin warning
       │    and crew/probe requirements met)
       │
       └─ No warnings ──────────────────────────────────────► Ready

Mission time warnings (evaluated separately by Mission aggregate, not ReadinessCalculator):
  • InvalidTimeRange            → blocking  (prevents save; does not affect ReadinessState)
  • AdvisoryEndTimeWithoutStart → advisory  (mission saves; no ReadinessState impact)
```

## Domain Layer (`MissionControl.Domain`)

### Aggregate Root: `Mission`

The central domain object. Encapsulates all mission state and derives `ReadinessState` and `Warnings` from its own data. Never exposes mutable state directly.

| Field | Type | Constraints | Notes |
|-------|------|-------------|-------|
| `Id` | `Guid` | Required; generated at creation | Opaque identity; storage-agnostic |
| `Name` | `string` | Required; 1–200 chars; unique in repository | Uniqueness enforced at repository + API layer |
| `TargetBody` | `MissionBodyValue` | Required; predefined list or custom | See value object below |
| `MissionType` | `MissionTypeValue` | Required; predefined list or custom | See value object below |
| `AvailableDeltaV` | `double` | Required; > 0 (m/s) | Validated in factory/constructor |
| `RequiredDeltaV` | `double` | Required; > 0 (m/s) | Validated in factory/constructor |
| `ReadinessState` | `ReadinessState` | Derived; never set directly | Calculated by `ReadinessCalculator` |
| `Warnings` | `IReadOnlyList<Warning>` | Derived; never set directly | Calculated by `ReadinessCalculator` |

**Factory method**: `Mission.Create(name, targetBody, missionType, availableDv, requiredDv)` — validates inputs, throws `DomainException` for invalid values, computes initial readiness.

**Update method**: `Mission.Update(name, targetBody, missionType, availableDv, requiredDv)` — re-validates and recalculates readiness atomically.

---

### Value Object: `MissionBodyValue`

Represents a target body selection — either a known KSP body or a custom modded value.

| Field | Type | Notes |
|-------|------|-------|
| `Value` | `string` | The body name (e.g., "Mun", "Duna", or user-entered custom text) |
| `IsCustom` | `bool` | `true` when "Other" was selected and a custom name was entered |

**Predefined values** (validated in domain): Kerbol, Moho, Eve, Gilly, Kerbin, Mun, Minmus, Duna, Ike, Dres, Jool, Laythe, Vall, Tylo, Bop, Pol, Eeloo.

**Custom**: Any non-empty string when `IsCustom = true` — no further validation on content.

---

### Value Object: `MissionTypeValue`

Represents a mission type selection — either a known KSP mission category or a custom value.

| Field | Type | Notes |
|-------|------|-------|
| `Value` | `string` | The type name (e.g., "Orbital", "Landing", or user-entered custom text) |
| `IsCustom` | `bool` | `true` when "Other" was selected |

**Predefined values** (validated in domain): Orbital, Landing, Flyby, Transfer, Rescue, Station Resupply, Return.

---

### Enum: `ReadinessState`

| Value | Meaning | Condition |
|-------|---------|-----------|
| `Ready` | Mission is viable with adequate margin | `availableDv >= requiredDv × 1.10` |
| `AtRisk` | Mission is viable but margin is thin | `availableDv >= requiredDv` AND `availableDv < requiredDv × 1.10` |
| `NotReady` | Mission is not viable | `availableDv < requiredDv` |

---

### Value Object: `Warning`

Represents a single diagnostic message attached to a mission. Multiple warnings can be active simultaneously.

| Field | Type | Notes |
|-------|------|-------|
| `Type` | `WarningType` | Enum identifying the warning category |
| `Message` | `string` | Human-readable warning text |

**Enum: `WarningType`**

| Value | Trigger Condition | Message (example) |
|-------|-------------------|-------------------|
| `InsufficientDeltaV` | `availableDv < requiredDv` | "Available delta-v ({X} m/s) is less than required ({Y} m/s)." |
| `LowReserveMargin` | Reserve margin < 10% (regardless of readiness state) | "Reserve margin is {Z}% — below the 10% safety threshold." |
| `MissingField` | Any required field is null/empty/invalid at persistence | "Required field '{FieldName}' is missing or invalid." |

**Independence rule**: `InsufficientDeltaV` and `LowReserveMargin` are evaluated independently. A `NotReady` mission may carry both warnings simultaneously.

---

### Domain Service: `ReadinessCalculator`

Pure, stateless calculation service. No I/O; no side effects. Satisfies Constitution Principle V.

```
ReadinessResult Calculate(double availableDv, double requiredDv)
```

**Returns**: `ReadinessResult { ReadinessState State, IReadOnlyList<Warning> Warnings, double ReserveMarginPercent }`

**Algorithm**:
1. Compute `reserveMargin = (availableDv - requiredDv) / requiredDv × 100`
2. Derive `ReadinessState`:
   - `availableDv < requiredDv` → `NotReady`
   - `reserveMargin < 10` → `AtRisk`
   - else → `Ready`
3. Accumulate warnings independently:
   - If `availableDv < requiredDv` → add `InsufficientDeltaV` warning
   - If `reserveMargin < 10` → add `LowReserveMargin` warning (regardless of state)

---

### Interface: `IMissionRepository`

Defined in the domain layer. Implemented in `MissionControl.Infrastructure`.

```
Task<IReadOnlyList<Mission>> GetAllAsync()
Task<Mission?> GetByIdAsync(Guid id)
Task<Mission?> GetByNameAsync(string name)
Task AddAsync(Mission mission)
Task UpdateAsync(Mission mission)
Task DeleteAsync(Guid id)
```

---

## Infrastructure Layer (`MissionControl.Infrastructure`)

### `JsonMissionRepository : IMissionRepository`

Reads/writes `missions.json` in the configured data directory. Uses `System.Text.Json`. File path configured via `IOptions<JsonStorageOptions>` from `appsettings.json`.

**Key design**: Loads the full list, mutates in memory, writes back atomically. Uses `SemaphoreSlim(1,1)` to protect the file from concurrent API requests. The entire persistence concern is invisible to the domain and API layers.

**JSON shape** (internal, never exposed as API contract):

```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Mun Landing Alpha",
    "targetBody": { "value": "Mun", "isCustom": false },
    "missionType": { "value": "Landing", "isCustom": false },
    "availableDeltaV": 5200.0,
    "requiredDeltaV": 4500.0
  }
]
```

`ReadinessState` and `Warnings` are **not persisted** — they are always re-derived on load from the stored delta-v values, ensuring the calculation logic is always the canonical source of truth.

---

## API Layer (`MissionControl.Api`) — DTOs

All API boundaries use strongly typed DTOs only (Constitution Principle III).

### `CreateMissionDto`

```
string Name
string TargetBodyValue
bool TargetBodyIsCustom
string MissionTypeValue
bool MissionTypeIsCustom
double AvailableDeltaV
double RequiredDeltaV
```

### `UpdateMissionDto`

Same shape as `CreateMissionDto`. Sent via `PUT /api/missions/{id}`.

### `MissionSummaryDto`

```
Guid Id
string Name
string TargetBodyValue
bool TargetBodyIsCustom
string MissionTypeValue
bool MissionTypeIsCustom
double AvailableDeltaV
double RequiredDeltaV
double ReserveMarginPercent
string ReadinessState          // "Ready" | "AtRisk" | "NotReady"
WarningDto[] Warnings
```

### `MissionListItemDto`

```
Guid Id
string Name
string ReadinessState          // "Ready" | "AtRisk" | "NotReady"
WarningDto[] Warnings
```

### `WarningDto`

```
string Type                    // "InsufficientDeltaV" | "LowReserveMargin" | "MissingField"
string Message
```

---

## Frontend Models (`frontend/src/app/models`)

### `mission.model.ts`

```typescript
export type ReadinessState = 'Ready' | 'AtRisk' | 'NotReady';
export type WarningType = 'InsufficientDeltaV' | 'LowReserveMargin' | 'MissingField';

export interface Warning {
  type: WarningType;
  message: string;
}

export interface MissionListItem {
  id: string;
  name: string;
  readinessState: ReadinessState;
  warnings: Warning[];
}

export interface MissionSummary {
  id: string;
  name: string;
  targetBodyValue: string;
  targetBodyIsCustom: boolean;
  missionTypeValue: string;
  missionTypeIsCustom: boolean;
  availableDeltaV: number;
  requiredDeltaV: number;
  reserveMarginPercent: number;
  readinessState: ReadinessState;
  warnings: Warning[];
}

export interface CreateMissionRequest {
  name: string;
  targetBodyValue: string;
  targetBodyIsCustom: boolean;
  missionTypeValue: string;
  missionTypeIsCustom: boolean;
  availableDeltaV: number;
  requiredDeltaV: number;
}

export type UpdateMissionRequest = CreateMissionRequest;
```

---

## State Transitions

```
[Created / Updated]
       │
       ▼
ReadinessCalculator.Calculate(availableDv, requiredDv)
       │
       ├─ availableDv < requiredDv ──────────────────────────► NotReady
       │                                                         + InsufficientDeltaV warning
       │                                                         + LowReserveMargin warning (if margin < 10%)
       │
       ├─ availableDv ≥ requiredDv, margin < 10% ───────────► AtRisk
       │                                                         + LowReserveMargin warning
       │
       └─ availableDv ≥ requiredDv × 1.10 ─────────────────► Ready
                                                               (no warnings)
```

**Note**: `LowReserveMargin` is evaluated independently of `InsufficientDeltaV`. When `availableDv < requiredDv`, the margin is always negative — which is numerically less than 10% — so both warnings always fire together on a `NotReady` mission.
