# Quickstart: Rocket-Based Mission Delta-V Calculator

**Phase**: 1 | **Date**: 2026-05-13 | **Plan**: [plan.md](plan.md)

---

## Prerequisites

Same as features 001/002. No new tooling required.

- .NET 8 SDK
- Node.js 20+ and npm
- A modern evergreen browser

---

## Running the Application

### Backend

```bash
cd backend
dotnet run --project MissionControl.Api
```

API available at `https://localhost:7001` (or the port shown in launch output).

### Frontend

```bash
cd frontend
npm install
npm start
```

Angular dev server at `http://localhost:4200`. Proxies `/api/*` to the backend via `proxy.conf.json`.

---

## Running Tests

### Backend (NUnit)

```bash
cd backend
dotnet test
```

All domain unit tests run in isolation — no database, no file system, no network.

New test files for this feature:
- `MissionControl.Tests/Domain/StageDeltaVCalculatorTests.cs`
- `MissionControl.Tests/Domain/RocketDeltaVCalculatorTests.cs`
- `MissionControl.Tests/Domain/CelestialBodyDeltaVEstimatorTests.cs`
- `MissionControl.Tests/Domain/RocketDeltaVRegressionTests.cs` ← regression suite
- `MissionControl.Tests/Api/RocketsControllerTests.cs`
- `MissionControl.Tests/Api/PartsControllerTests.cs`
- `MissionControl.Tests/Api/CelestialBodiesControllerTests.cs`

### Regression Tests

`RocketDeltaVRegressionTests` is a merge prerequisite. It verifies that the calculator produces
values within a tolerance bound derived from the domain's safety margin concept.

**Tolerance formula**:
```
maxAllowedDeviation = requiredDeltaV × (safetyMarginPercent / 100) / 2
```

With the default 10% safety margin this equals ±5% of required delta-v. The intent is that a
calculator regression can never silently flip a mission's readiness state without a user noticing.

**To run regression tests only**:
```bash
cd backend
dotnet test --filter "FullyQualifiedName~RocketDeltaVRegressionTests"
```

**Required fixtures** (all five must pass before merge):

| Fixture | Scenario |
|---|---|
| `single-stage-atm` | One LKO engine + one liquid tank on Kerbin (85% efficiency) |
| `two-stage-atm` | Launch stage jettisoned; upper stage vacuum burn |
| `vacuum-only` | Single stage on Mun — no atmospheric efficiency multiplier |
| `srb-stage` | Solid booster stage at sea-level Isp |
| `asparagus-atm` | Same as single-stage-atm with 8% asparagus bonus |

Pre-calculated reference values and the full derivation are in
[plan.md § Regression Test Constraint](plan.md#regression-test-constraint).

### Frontend (Karma + Jasmine)

```bash
cd frontend
npm test
```

---

## Seed Data

Two new seeded JSON files are created during implementation:

| File | Location | Contents |
|---|---|---|
| `parts.json` | `backend/MissionControl.Api/data/parts.json` | ~368 stock KSP parts (generated from wiki HTML) |
| `celestial-bodies.json` | `backend/MissionControl.Api/data/celestial-bodies.json` | 17 Kerbol system bodies + empty custom array |

These files are loaded at API startup via `JsonPartCatalogueRepository` and `JsonCelestialBodyRepository`.
Parts are held entirely in memory after startup (read-only). Custom celestial bodies written back on POST.

The `rockets.json` file is created automatically on first rocket save (same pattern as `missions.json`).

---

## Key Concepts

### Delta-V Calculation Pipeline

```
User builds rocket in UI
        ↓
POST /api/rockets  →  Rocket stored in rockets.json
        ↓
User assigns rocket to mission
        ↓
PUT /api/missions/{id}  (with assignedRocketId + calculationProfile)
        ↓
Backend:  RocketDeltaVCalculator.Calculate(rocket, parts, launchBody)
                ↓
          CelestialBodyDeltaVEstimator.Estimate(launchBody, targetBody, profile)
                ↓
          Mission.Update(availableDv: computed, requiredDv: computed, ...)
                ↓
          ReadinessCalculator.Calculate(availableDv, requiredDv, ...)  ← unchanged
        ↓
MissionSummaryDto returned with readiness state + full breakdown
```

### Stage Numbering (KSP Convention)

Stage 1 = final/payload stage (last to fire, e.g., orbital insertion engine).  
Highest stage number = launch/booster stage (first to fire).

The Rocket Builder UI displays stages with Stage 1 at the top and the launch stage at the bottom,
matching the KSP VAB convention. The calculation evaluates from the highest number downward.

### Asparagus Staging Approximation

The asparagus control on a rocket is a planning aid — not a physics simulation.
When enabled with a bonus of N%:
```
effective_stage_dv = raw_stage_dv × efficiency_factor × (1 + N/100)
```
This applies only when the launch body has an atmosphere. The bonus partially offsets the
atmospheric efficiency penalty (default 85%) to account for the mass efficiency of crossfeed staging.

The slider is disabled (and bonus forced to 0%) when the selected launch body is airless.

### Backwards Compatibility

All existing missions without a rocket assigned continue to work exactly as before — `availableDeltaV`
and `requiredDeltaV` are still accepted as manual inputs. The `assignedRocketId` and `calculationProfile`
fields are optional on all create/update requests.

---

## Implementing the Part Catalogue Seed Data

The `parts.json` file must be generated from the attached KSP wiki HTML export
(`Parts - Kerbal Space Program Wiki.html`). The HTML contains sortable tables with all
stock part properties.

**Required fields to extract per part**:

| Wiki Column | JSON Field | Notes |
|---|---|---|
| Part name (link text) | `name` | Strip wiki formatting |
| Radial size | (display only) | Not needed for calculation |
| Mass (full) | `wetMass` | In tonnes; parenthetical = empty |
| Mass (empty) | `dryMass` | Extract from parenthetical value |
| Category (table section) | `category` | Map to `PartCategory` enum |
| Isp (ASL / Vac) | `engineStats.ispSeaLevel/ispVacuum` | Engine rows only |
| Thrust (ASL / Vac) | `engineStats.thrustSeaLevel/thrustVacuum` | Engine rows only |
| Fuel type | `engineStats.fuelTypes` | Inferred from fuel columns |

**ID generation**: Slugify the part name to lowercase with hyphens, removing special characters.
Example: `"LV-T45 'Swivel' Liquid Fuel Engine"` → `"lv-t45-swivel-liquid-fuel-engine"`.
Alternatively, use the part's internal KSP config name where available.

The transformation should be done once during implementation, producing the final `parts.json`
as a static build artifact. Do not parse HTML at runtime.

---

## API Quick Reference

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/rockets` | List all rockets |
| `POST` | `/api/rockets` | Create a rocket |
| `GET` | `/api/rockets/{id}` | Get rocket detail + delta-v breakdown |
| `PUT` | `/api/rockets/{id}` | Update a rocket |
| `DELETE` | `/api/rockets/{id}` | Delete a rocket |
| `GET` | `/api/parts` | List parts (optional `?category=` / `?search=`) |
| `GET` | `/api/parts/{id}` | Get a single part |
| `GET` | `/api/celestial-bodies` | List all bodies (stock + custom) |
| `POST` | `/api/celestial-bodies/custom` | Create a custom body |
| `GET` | `/api/missions` | List all missions (existing) |
| `POST` | `/api/missions` | Create mission (extended with optional rocket fields) |
| `GET` | `/api/missions/{id}` | Get mission (extended with breakdown) |
| `PUT` | `/api/missions/{id}` | Update mission (extended with optional rocket fields) |
| `DELETE` | `/api/missions/{id}` | Delete mission (existing) |
