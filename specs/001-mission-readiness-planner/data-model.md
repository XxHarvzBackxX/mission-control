# Data Model: Mission Readiness Planner

**Phase**: 1 | **Date**: 2026-05-12 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

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
