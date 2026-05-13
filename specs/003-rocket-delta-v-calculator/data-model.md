# Data Model: Rocket-Based Mission Delta-V Calculator

**Phase**: 1 | **Date**: 2026-05-13 | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

---

## Domain Layer (`MissionControl.Domain`)

### Aggregate Root: `Rocket`

A reusable launch vehicle. Contains stages, asparagus approximation settings, and optional
metadata. The aggregate enforces its own validity rules and is never returned from the domain
in an invalid state.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `Guid` | Required; generated at creation | |
| `Name` | `string` | Required; 1–200 chars; unique in repository | |
| `Description` | `string` | Required; 1–500 chars | |
| `Notes` | `string?` | Optional | |
| `Stages` | `IReadOnlyList<Stage>` | At least 1 stage required | KSP numbering: Stage 1 = last to fire |
| `UsesAsparagusStaging` | `bool` | Default: false | Enables bonus multiplier |
| `AsparagusEfficiencyBonus` | `double` | `[0.0, 0.20]`; only meaningful when `UsesAsparagusStaging = true` | 0.08 default when first enabled |

**Factory method**: `Rocket.Create(name, description, stages, usesAsparagus, asparagusBonus, notes?)`

**Update method**: `Rocket.Update(...)` — re-validates all inputs atomically

**Validation**:
- Name is required and must be unique (uniqueness enforced at repository layer)
- Must contain at least one stage
- `AsparagusEfficiencyBonus` must be in `[0.0, 0.20]`
- If `UsesAsparagusStaging = false`, `AsparagusEfficiencyBonus` is ignored (treated as 0)

---

### Entity: `Stage` *(within Rocket aggregate)*

One discrete burn section of a rocket. Stages are numbered using KSP convention.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `Guid` | Required | Stable identity within aggregate |
| `StageNumber` | `int` | ≥ 1; unique within rocket | 1 = last to fire (payload/upper), highest = first to fire (launch) |
| `Name` | `string` | Required; 1–100 chars | e.g., "Launch Stage", "Upper Stage" |
| `Parts` | `IReadOnlyList<StageEntry>` | At least 1 entry per stage | |
| `IsJettisoned` | `bool` | Default: true | If true, stage hardware is discarded after burn; dry mass removed from remaining mass |
| `Notes` | `string?` | Optional | |

---

### Value Object: `StageEntry`

An immutable reference to a catalogue part with a quantity. Multiple units of the same part
(e.g., 4× FL-T400 tanks) are represented as a single entry with quantity 4.

| Field | Type | Constraints |
|---|---|---|
| `PartId` | `string` | Must reference a valid `CataloguePart.Id` |
| `Quantity` | `int` | ≥ 1 |

---

### Reference Entity: `CataloguePart`

Seeded read-only part. Loaded at startup from `parts.json` and held in memory. Never mutated
at runtime. Not a DDD aggregate — it is reference data accessed by the domain calculators.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `string` | Slug (e.g., `"lv-t45"`); unique | |
| `Name` | `string` | Required | Display name |
| `Category` | `PartCategory` | Required | See enum below |
| `DryMass` | `double` | ≥ 0 (tonnes) | Mass when empty/inert |
| `WetMass` | `double` | ≥ DryMass (tonnes) | Mass when fully loaded with fuel |
| `FuelCapacity` | `FuelCapacity?` | Null if part carries no fuel | See value object below |
| `EngineStats` | `EngineStats?` | Null if not a propulsive part | See value object below |

**Note on SRBs**: Solid Rocket Boosters are self-contained — they carry both engine stats and
fuel capacity in the same part record. `FuelCapacity.FuelType = SolidFuel`.

---

### Reference Entity: `CelestialBody`

A planetary body. Stock bodies are seeded from `celestial-bodies.json`. Custom bodies are
created by the user and stored in the same file under a separate array.

| Field | Type | Constraints | Notes |
|---|---|---|---|
| `Id` | `string` | Slug for stock (e.g., `"kerbin"`); GUID for custom | |
| `Name` | `string` | Required | |
| `ParentBodyId` | `string?` | Null for Kerbol (root star) | References another `CelestialBody.Id` |
| `EquatorialRadius` | `double` | > 0 (metres) | |
| `SurfaceGravity` | `double` | > 0 (m/s²) | |
| `SurfacePressure` | `double` | ≥ 0 (atm) | 0 = airless/vacuum body |
| `AtmosphereHeight` | `double` | ≥ 0 (metres) | 0 = no atmosphere |
| `SphereOfInfluence` | `double?` | > 0 (metres); null for Kerbol | |
| `SemiMajorAxis` | `double?` | > 0 (metres from parent); null for Kerbol | Used in transfer ΔV calculations |
| `DefaultOrbitAltitude` | `double` | > 0 (metres) | Pre-set per body; see research.md table |
| `IsCustom` | `bool` | Default: false for seeded | Custom = user-entered data |

---

### Value Object: `EngineStats`

Propulsion data for an engine part.

| Field | Type | Constraints |
|---|---|---|
| `ThrustSeaLevel` | `double` | ≥ 0 (kN) |
| `ThrustVacuum` | `double` | ≥ 0 (kN) |
| `IspSeaLevel` | `double` | > 0 (seconds) |
| `IspVacuum` | `double` | ≥ IspSeaLevel (seconds) |
| `FuelTypes` | `IReadOnlyList<FuelType>` | At least 1 fuel type required |

---

### Value Object: `FuelCapacity`

The fuel a tank (or SRB) carries, keyed by fuel type.

| Field | Type | Constraints |
|---|---|---|
| `Resources` | `IReadOnlyDictionary<FuelType, double>` | All values > 0 (KSP resource units) |

The mass contribution of fuel is:
- For LiquidFuel/Oxidizer: determined by wet mass − dry mass of the part
- The `Resources` dictionary is used for fuel compatibility checking (FR: Mixed Fuel Uncertainty)

---

### Value Object: `MissionCalculationProfile`

Per-mission calculation settings. Stored inline on the `Mission` entity. Not shared or reused
across missions (v1 scope).

| Field | Type | Constraints | Default |
|---|---|---|---|
| `LaunchBodyId` | `string` | Must reference a valid `CelestialBody.Id` | `"kerbin"` |
| `TargetBodyId` | `string` | Must reference a valid `CelestialBody.Id` | `"mun"` |
| `ProfileType` | `MissionProfileType` | Required | `OrbitInsertion` |
| `TargetOrbitAltitude` | `double` | > 0 (metres) | body's `DefaultOrbitAltitude` |
| `AtmosphericEfficiencyMultiplier` | `double` | `(0.0, 1.0]` | `0.85` |
| `SafetyMarginPercent` | `double` | `[0.0, 100.0]` | `10.0` |
| `RequiredDeltaVOverride` | `double?` | ≥ 0; null = use estimated | `null` |

---

### Value Object: `StageDeltaVResult`

Output of `StageDeltaVCalculator` for one stage.

| Field | Type | Notes |
|---|---|---|
| `StageNumber` | `int` | KSP stage number |
| `StageName` | `string` | |
| `WetMass` | `double` | (tonnes) Total rocket mass at start of stage burn |
| `DryMass` | `double` | (tonnes) Total rocket mass after stage fuel consumed |
| `IspUsed` | `double` | Effective Isp applied (sea-level or vacuum; combined if multiple engines) |
| `RawDeltaV` | `double` | (m/s) Before efficiency multiplier |
| `EfficiencyFactor` | `double` | Multiplier applied (1.0 for vacuum; <1.0 for atmospheric) |
| `AsparagusBonus` | `double` | Bonus applied (0.0 if disabled or vacuum launch) |
| `EffectiveDeltaV` | `double` | (m/s) After all multipliers |
| `Warnings` | `IReadOnlyList<Warning>` | Stage-level warnings (e.g., Mixed Fuel Uncertainty) |

---

### Value Object: `RocketDeltaVResult`

Output of `RocketDeltaVCalculator`.

| Field | Type | Notes |
|---|---|---|
| `TotalEffectiveDeltaV` | `double` | (m/s) Sum of all stages' effective delta-v |
| `Stages` | `IReadOnlyList<StageDeltaVResult>` | Per-stage breakdown |
| `Warnings` | `IReadOnlyList<Warning>` | Rocket-level warnings (No Engine, No Command Part, etc.) |
| `IsValid` | `bool` | False if any blocking warning prevents a meaningful result |

---

### Value Object: `RequiredDeltaVResult`

Output of `CelestialBodyDeltaVEstimator`.

| Field | Type | Notes |
|---|---|---|
| `TotalRequiredDeltaV` | `double` | (m/s) |
| `AscentDeltaV` | `double` | Launch-body ascent component |
| `TransferDeltaV` | `double` | Transfer/insertion component (0 for AscentOnly profile) |
| `DescentDeltaV` | `double` | Target descent component (0 unless SurfaceLanding or FullReturn) |
| `ReturnDeltaV` | `double` | Return transfer component (0 unless FullReturn) |
| `EstimationMethod` | `string` | Human-readable description of formulas used |
| `IsApproximated` | `bool` | True when any component uses simplified formula |

---

### Enum: `MissionProfileType`

| Value | Description |
|---|---|
| `OrbitInsertion` | Ascent + transfer + target orbit insertion (default) |
| `AscentOnly` | Launch-body ascent to orbit only |
| `SurfaceLanding` | Orbit insertion + powered descent to target surface |
| `FullReturn` | Surface landing + target ascent + return transfer + launch-body re-entry |

---

### Enum: `PartCategory`

`Pods`, `FuelTanks`, `Engines`, `CommandAndControl`, `Structural`, `Coupling`, `Payload`,
`Aerodynamics`, `Ground`, `Thermal`, `Electrical`, `Communication`, `Science`, `Cargo`, `Utility`

---

### Enum: `FuelType`

| Value | Used By |
|---|---|
| `LiquidFuelOxidizer` | Most liquid rocket engines (LV-T45, Mainsail, etc.) |
| `SolidFuel` | Solid Rocket Boosters (BACC, Kickback, etc.) |
| `MonoPropellant` | RCS, Ant, Spider, Twitch engines |
| `Xenon` | Ion engine (Dawn) |
| `LiquidFuelOnly` | Jet engines (out of scope for v1 rockets but present in catalogue) |

---

### Enum: `WarningType` *(extended)*

New values added to existing enum:

| Value | Blocking? | Trigger |
|---|---|---|
| `NoCommandPart` | No | Rocket has no command pod or probe core |
| `NoEngine` | Yes | Rocket has no engine part in any stage |
| `NoFuelSource` | Yes | Rocket has engines but no compatible fuel in any stage |
| `MixedFuelUncertainty` | No | Stage has engines and fuel that cannot be confidently matched |
| `AtmosphericLossApplied` | No | Efficiency multiplier < 1.0 was applied |
| `UnstableCraftAssumption` | No | CoM/CoD stability assumed, not calculated |
| `CustomBodyApproximation` | No | Custom celestial body data is user-entered |
| `ManualOverrideApplied` | No | User has overridden the required delta-v estimate |
| `AsparagusApproximationApplied` | No | Asparagus bonus was applied to atmospheric ascent |

---

### Domain Services

#### `StageDeltaVCalculator` (static, pure)

```csharp
/// <summary>
/// Calculates delta-v for a single stage using the Tsiolkovsky rocket equation:
///   ΔV = Isp × g₀ × ln(m_wet / m_dry)
/// </summary>
public static StageDeltaVResult Calculate(
    Stage stage,
    IReadOnlyList<CataloguePart> catalogueParts,
    double precedingRocketMass,      // total mass of all payload-side stages above this one
    double accumulatedWetMass,       // total remaining mass at start of this burn
    bool useVacuumIsp,               // true for airless launch body
    double efficiencyFactor,         // 1.0 for vacuum; default 0.85 for atmospheric
    double asparagusBonus)           // 0.0–0.20; 0.0 if vacuum or not enabled
```

#### `RocketDeltaVCalculator` (static, pure)

```csharp
/// <summary>
/// Orchestrates per-stage calculation across all rocket stages in firing order
/// (highest stage number first), accumulating mass correctly as stages are jettisoned.
/// Applies atmospheric efficiency multiplier and asparagus bonus for atmospheric launches.
/// </summary>
public static RocketDeltaVResult Calculate(
    Rocket rocket,
    IReadOnlyList<CataloguePart> catalogueParts,
    CelestialBody launchBody,
    double efficiencyFactor)
```

#### `CelestialBodyDeltaVEstimator` (static, pure)

```csharp
/// <summary>
/// Estimates required mission delta-v using orbital mechanics formulas.
/// Ascent: v_circ = sqrt(g × R² / (R + h)) + drag/gravity losses.
/// Transfer: Hohmann approximation using body semi-major axes and parent μ.
/// </summary>
public static RequiredDeltaVResult Estimate(
    CelestialBody launchBody,
    CelestialBody targetBody,
    MissionCalculationProfile profile)
```

---

## Modified: `Mission` Entity

New optional fields added. All existing fields and behaviour are unchanged.

| New Field | Type | Default | Notes |
|---|---|---|---|
| `AssignedRocketId` | `Guid?` | `null` | Foreign key to a Rocket; no aggregate dependency |
| `CalculationProfile` | `MissionCalculationProfile?` | `null` | Set when a rocket is assigned; null = manual delta-v mode |
| `RocketName` | `string?` | `null` | Denormalised for display; set when rocket is assigned |

**Behaviour**:
- When `AssignedRocketId` is null: `AvailableDeltaV` and `RequiredDeltaV` are manually entered (existing behaviour)
- When `AssignedRocketId` is set: `AvailableDeltaV` and `RequiredDeltaV` are calculated by the controller on every read and save
- `ReadinessCalculator` is called with the same scalar interface — it has no knowledge of rockets

---

## Repository Interfaces

### `IRocketRepository`

```csharp
Task<IReadOnlyList<Rocket>> GetAllAsync();
Task<Rocket?> GetByIdAsync(Guid id);
Task<Rocket?> GetByNameAsync(string name);
Task AddAsync(Rocket rocket);
Task UpdateAsync(Rocket rocket);
Task DeleteAsync(Guid id);
Task<IReadOnlyList<Guid>> GetMissionIdsAssignedToRocketAsync(Guid rocketId);
```

### `IPartCatalogueRepository`

```csharp
Task<IReadOnlyList<CataloguePart>> GetAllAsync();
Task<CataloguePart?> GetByIdAsync(string id);
Task<IReadOnlyList<CataloguePart>> GetByCategoryAsync(PartCategory category);
Task<IReadOnlyList<CataloguePart>> SearchByNameAsync(string query);
```

### `ICelestialBodyRepository`

```csharp
Task<IReadOnlyList<CelestialBody>> GetAllAsync();
Task<CelestialBody?> GetByIdAsync(string id);
Task AddCustomAsync(CelestialBody body);
```

---

## Test Support Types (`MissionControl.Tests`)

### Record: `RocketDeltaVFixture`

Defines a named regression scenario used by `RocketDeltaVRegressionTests`. Each fixture
encapsulates a complete input configuration and the pre-calculated reference output used
to assert calculator accuracy within the tolerance bound.

The tolerance bound is derived from the domain's safety margin concept:
`maxAllowedDeviation = requiredDeltaV × (safetyMarginPercent / 100) / 2`

```csharp
/// <summary>
/// A named test fixture for regression-testing RocketDeltaVCalculator output.
/// The calculator's TotalEffectiveDeltaV MUST satisfy:
///   |result - PreCalculatedDeltaV| ≤ RequiredDeltaV × (SafetyMarginPercent / 100) / 2
/// </summary>
public record RocketDeltaVFixture(
    string FixtureName,
    Rocket RocketConfig,
    IReadOnlyList<CataloguePart> CatalogueParts,
    CelestialBody LaunchBody,
    double EfficiencyFactor,
    double RequiredDeltaV,
    double SafetyMarginPercent,
    double PreCalculatedDeltaV)
{
    public double MaxAllowedDeviation =>
        RequiredDeltaV * (SafetyMarginPercent / 100.0) / 2.0;
}
```

**Pre-calculated reference values** (see [plan.md § Regression Test Constraint](plan.md) for
the full derivation table):

| Fixture | `PreCalculatedDeltaV` (m/s) | `RequiredDeltaV` (m/s) | `MaxAllowedDeviation` (m/s) |
|---|---|---|---|
| `single-stage-atm` | 1 288.5 | 3 400 | 170.0 |
| `two-stage-atm` | 3 968.5 | 3 400 | 170.0 |
| `vacuum-only` | 2 754.9 | 860 | 43.0 |
| `srb-stage` | 1 440.0 | 3 400 | 170.0 |
| `asparagus-atm` | 1 391.6 | 3 400 | 170.0 |

---

## Frontend Models

### `rocket.model.ts`

```typescript
export interface RocketListItem {
  id: string;
  name: string;
  stageCount: number;
  totalEffectiveDeltaV: number;
  warnings: Warning[];
}

export interface RocketSummary {
  id: string;
  name: string;
  description: string;
  notes: string | null;
  usesAsparagusStaging: boolean;
  asparagusEfficiencyBonus: number;
  stages: StageDto[];
  deltaVBreakdown: RocketDeltaVBreakdown;
}

export interface StageDto {
  id: string;
  stageNumber: number;
  name: string;
  isJettisoned: boolean;
  parts: StageEntryDto[];
  notes: string | null;
}

export interface StageEntryDto {
  partId: string;
  quantity: number;
}

export interface RocketDeltaVBreakdown {
  totalEffectiveDeltaV: number;
  stages: StageDeltaVDto[];
  warnings: Warning[];
}

export interface StageDeltaVDto {
  stageNumber: number;
  stageName: string;
  wetMass: number;
  dryMass: number;
  ispUsed: number;
  rawDeltaV: number;
  efficiencyFactor: number;
  asparagusBonus: number;
  effectiveDeltaV: number;
  warnings: Warning[];
}

export interface CreateRocketRequest {
  name: string;
  description: string;
  notes: string | null;
  usesAsparagusStaging: boolean;
  asparagusEfficiencyBonus: number;
  stages: CreateStageRequest[];
}

export interface CreateStageRequest {
  stageNumber: number;
  name: string;
  isJettisoned: boolean;
  parts: StageEntryDto[];
  notes: string | null;
}

export type UpdateRocketRequest = CreateRocketRequest;
```

### `part.model.ts`

```typescript
export type PartCategory =
  | 'Pods' | 'FuelTanks' | 'Engines' | 'CommandAndControl'
  | 'Structural' | 'Coupling' | 'Payload' | 'Aerodynamics'
  | 'Ground' | 'Thermal' | 'Electrical' | 'Communication'
  | 'Science' | 'Cargo' | 'Utility';

export type FuelType = 'LiquidFuelOxidizer' | 'SolidFuel' | 'MonoPropellant' | 'Xenon' | 'LiquidFuelOnly';

export interface PartDto {
  id: string;
  name: string;
  category: PartCategory;
  dryMass: number;
  wetMass: number;
  fuelCapacity: Record<FuelType, number> | null;
  engineStats: EngineStatsDto | null;
}

export interface EngineStatsDto {
  thrustSeaLevel: number;
  thrustVacuum: number;
  ispSeaLevel: number;
  ispVacuum: number;
  fuelTypes: FuelType[];
}
```

### `celestial-body.model.ts`

```typescript
export interface CelestialBodyDto {
  id: string;
  name: string;
  parentBodyId: string | null;
  equatorialRadius: number;
  surfaceGravity: number;
  surfacePressure: number;
  atmosphereHeight: number;
  sphereOfInfluence: number | null;
  semiMajorAxis: number | null;
  defaultOrbitAltitude: number;
  isCustom: boolean;
}

export interface CreateCustomBodyRequest {
  name: string;
  equatorialRadius: number;
  surfaceGravity: number;
  surfacePressure: number;
  atmosphereHeight: number;
  sphereOfInfluence: number | null;
  semiMajorAxis: number | null;
}
```

### `mission.model.ts` *(additions)*

```typescript
// New fields on MissionSummary
export interface MissionSummary {
  // ... existing fields ...
  assignedRocketId: string | null;
  rocketName: string | null;
  calculationProfile: MissionCalculationProfileDto | null;
  requiredDeltaVBreakdown: RequiredDeltaVBreakdownDto | null;
}

export interface MissionCalculationProfileDto {
  launchBodyId: string;
  targetBodyId: string;
  profileType: MissionProfileType;
  targetOrbitAltitude: number;
  atmosphericEfficiencyMultiplier: number;
  safetyMarginPercent: number;
  requiredDeltaVOverride: number | null;
}

export type MissionProfileType = 'OrbitInsertion' | 'AscentOnly' | 'SurfaceLanding' | 'FullReturn';

export interface RequiredDeltaVBreakdownDto {
  totalRequiredDeltaV: number;
  ascentDeltaV: number;
  transferDeltaV: number;
  descentDeltaV: number;
  returnDeltaV: number;
  estimationMethod: string;
  isApproximated: boolean;
}
```
