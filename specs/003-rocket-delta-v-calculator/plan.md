# Implementation Plan: Rocket-Based Mission Delta-V Calculator

**Branch**: `003-rocket-delta-v-calculator` | **Date**: 2026-05-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/003-rocket-delta-v-calculator/spec.md`

## Summary

Extends Mission Control from a manual readiness checker into a KSP-inspired delta-v calculator.
Users build reusable rockets from staged KSP-style parts drawn from a seeded catalogue, assign
rockets to missions, and receive calculated readiness driven by the Tsiolkovsky rocket equation
and formula-derived orbital mechanics. Staging is sequential (v1); asparagus staging is supported
as a configurable atmospheric-ascent efficiency bonus. All calculation logic is pure, domain-layer,
and covered by unit tests — including regression tests that bound calculator output deviation to
no more than half the configured safety margin tolerance.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core Web API (existing); Angular latest stable (existing);
NUnit 4 + NSubstitute (existing); Karma + Jasmine (existing). **No new dependencies required.**

**Storage**: JSON file persistence (`rockets.json`, `parts.json` seeded, `celestial-bodies.json`
seeded) — same pattern as existing `missions.json` via `SemaphoreSlim`-locked repository.

**Testing**: NUnit 4 (.NET domain), Karma + Jasmine (Angular frontend)

**Target Platform**: Local dev server (existing); no deployment target change.

**Project Type**: Web application — Angular SPA + .NET Web API (existing architecture extended).

**Performance Goals**: Delta-v calculation completes synchronously in-process; no observable
latency beyond the existing mission GET response time.

**Constraints**:
- All calculation logic MUST be deterministic and pure (Constitution Principle V)
- Unit tests MUST cover all calculation paths (Constitution Principle VI)
- **Regression constraint (new)**: A dedicated regression test suite MUST assert that for each
  named fixture configuration, the calculator's effective delta-v deviates from the pre-calculated
  reference value by no more than `(requiredDeltaV × safetyMarginPercent / 100) / 2`. With the
  default 10% safety margin this equals ±5% of required delta-v. See *Regression Test Constraint*
  section below for full specification.
- No new third-party packages (Constitution Principle VIII)
- Backwards compatibility: missions without a rocket remain fully functional (FR-035)

**Scale/Scope**: ~368 seeded parts, 17 seeded celestial bodies, unlimited user rockets/missions.

## Constitution Check

*Gate: verified before Phase 0 research. Re-verified after Phase 1 design.*

| # | Principle | Status | Notes |
|---|---|---|---|
| I | Modular Architecture | ✅ PASS | Angular SPA and .NET API remain independent layers; no new cross-layer coupling |
| II | Component-Driven Frontend | ✅ PASS | Rocket Builder, Part Picker, and Celestial Body selector are discrete, independently renderable components |
| III | Domain-Driven Backend | ✅ PASS | `Rocket`, `Stage`, `MissionCalculationProfile` as DDD aggregates/value objects; strongly typed DTOs at all API boundaries |
| IV | Business Logic Isolation | ✅ PASS | `StageDeltaVCalculator`, `RocketDeltaVCalculator`, `CelestialBodyDeltaVEstimator` are pure domain services; controllers only orchestrate and map |
| V | Deterministic Calculations | ✅ PASS | All three domain services are static pure functions; no I/O, no randomness, no external state |
| VI | Unit Test Coverage | ✅ PASS | Full unit test suite required; regression tests (`RocketDeltaVRegressionTests`) are a merge prerequisite |
| VII | Readability First | ✅ PASS | No premature optimisations; asparagus bonus is a simple multiplier, not a physics simulation |
| VIII | Minimal Dependencies | ✅ PASS | No new packages; all calculation is first-party |
| IX | Purposeful Documentation | ✅ PASS | Concise XML doc comments on non-obvious public service methods only |

**Post-design re-check**: All principles continue to pass after Phase 1 design. The `MissionCalculationProfile` value object is inline per-mission (not shared) which avoids aggregate boundary violations.

## Regression Test Constraint

### Purpose

The regression test suite verifies that the delta-v calculator implementation produces values
that are sufficiently accurate relative to the precision required for mission planning. The
tolerance bound is derived directly from the domain's safety margin concept: a calculator error
larger than half the safety margin could silently flip a mission's readiness state without the
user making any real change.

### Tolerance Formula

```
deltaVTolerance(R, P) = R × (P / 100)
maxAllowedDeviation    = deltaVTolerance / 2
                       = R × (P / 100) / 2
```

Where:
- `R` = required delta-v for the reference scenario (m/s)
- `P` = safety margin percent (default: 10%)
- `maxAllowedDeviation` = the maximum permitted absolute difference between the calculator's
  output and the hand-computed reference value

**Example (default 10% margin, Kerbin-to-Mun orbit insertion, R ≈ 3 400 m/s)**:
- `deltaVTolerance = 3 400 × 0.10 = 340 m/s`
- `maxAllowedDeviation = 340 / 2 = 170 m/s`

### Fixture Requirements

Each fixture MUST document:

| Field | Description |
|---|---|
| `FixtureName` | Human-readable scenario name |
| `RocketConfig` | Full stage/part configuration (stage numbers, part IDs, quantities) |
| `LaunchBodyId` | Celestial body used for efficiency factor selection |
| `EfficiencyFactor` | Pre-agreed atmospheric efficiency multiplier (1.0 for vacuum) |
| `AsparagusBonus` | Pre-agreed asparagus multiplier (0.0 if disabled) |
| `RequiredDeltaV` | Reference required delta-v used to derive the tolerance bound |
| `SafetyMarginPercent` | Safety margin used to derive the tolerance bound (default: 10.0) |
| `PreCalculatedDeltaV` | Reference effective delta-v computed by hand using the Tsiolkovsky formula |
| `MaxAllowedDeviation` | `PreCalculatedDeltaV × (SafetyMarginPercent / 100) / 2` — asserted as the tolerance |

**Minimum required fixtures** (MUST be implemented before merge):

1. **Single-stage atmospheric liquid rocket** — one LKO engine + one liquid fuel tank on Kerbin
2. **Two-stage atmospheric rocket** — launch stage jettisoned; upper stage vacuum burn; total
   effective DV is sum across both stages with atmospheric multiplier on stage 2 only
3. **Vacuum-only rocket** — single stage, airless body (e.g., Mun); no efficiency multiplier
4. **SRB-only stage** — solid booster stage; pre-calculated reference uses SRB Isp at sea level
5. **Asparagus atmospheric bonus** — same config as fixture 1 with asparagus enabled at 8%;
   pre-calculated reference includes the bonus multiplier

### Assertion Shape

```csharp
// In MissionControl.Tests/Domain/RocketDeltaVRegressionTests.cs
[TestCase("single-stage-atmospheric", ...)]
public void RocketDeltaVCalculator_Regression_MatchesPreCalculatedValueWithinTolerance(
    string fixtureName,
    double preCalculatedDeltaV,
    double requiredDeltaV,
    double safetyMarginPercent)
{
    var result = RocketDeltaVCalculator.Calculate(/* fixture config */);
    var maxDeviation = requiredDeltaV * (safetyMarginPercent / 100.0) / 2.0;

    Assert.That(
        Math.Abs(result.TotalEffectiveDeltaV - preCalculatedDeltaV),
        Is.LessThanOrEqualTo(maxDeviation),
        $"[{fixtureName}] Expected ΔV {preCalculatedDeltaV:F2} ±{maxDeviation:F2} m/s, " +
        $"got {result.TotalEffectiveDeltaV:F2} m/s");
}
```

### Fixture Pre-Calculated Reference Values

All reference values below were derived using:
`ΔV_effective = Isp × g₀ × ln(m_wet / m_dry) × efficiencyFactor × (1 + asparagusBonus)`
where `g₀ = 9.80665 m/s²`.

See [research.md](research.md) for complete formula derivation.

| Fixture | Config Summary | Wet (t) | Dry (t) | Isp (s) | EffFactor | AspBonus | ΔV_ref (m/s) | Required (m/s) | MaxDev (m/s) |
|---|---|---|---|---|---|---|---|---|---|
| single-stage-atm | Mk1 Pod + FL-T400 + LV-T45; Kerbin | 4.59 | 2.59 | 270 | 0.85 | 0.00 | 1 288.5 | 3 400 | 170.0 |
| two-stage-atm | S2: FL-T800+LV-T45 (jettisoned); S1: FL-T400+LV-909+Mk1 Pod; Kerbin | — | — | 270/345 | 0.85/1.0 | 0.00 | 3 968.5 | 3 400 | 170.0 |
| vacuum-only | FL-T400 + LV-909 + Mk1 Pod; Mun | 3.59 | 1.59 | 345 | 1.00 | 0.00 | 2 754.9 | 860 | 43.0 |
| srb-stage | RT-10 "Hammer" SRB + Mk1 Pod; Kerbin | 4.40 | 1.59 | 170 | 0.85 | 0.00 | 1 440.0 | 3 400 | 170.0 |
| asparagus-atm | Mk1 Pod + FL-T400 + LV-T45; Kerbin; asparagus 8% | 4.59 | 2.59 | 270 | 0.85 | 0.08 | 1 391.6 | 3 400 | 170.0 |

*Note: Two-stage fixture reference is computed stage-by-stage accumulating mass; see implementation notes
in [research.md](research.md) §1 for the full walkthrough.*

## Project Structure

### Documentation (this feature)

```text
specs/003-rocket-delta-v-calculator/
├── plan.md              ← this file
├── research.md          ← Phase 0 research findings
├── data-model.md        ← Phase 1 domain model
├── quickstart.md        ← Phase 1 developer guide
├── contracts/
│   └── api.md           ← Phase 1 REST contract
└── tasks.md             ← Phase 2 task list (not yet generated)
```

### Backend

```text
backend/
├── MissionControl.Domain/
│   ├── Entities/
│   │   ├── Mission.cs                         MODIFIED  (+ AssignedRocketId, CalculationProfile, RocketName)
│   │   └── Rocket.cs                          NEW
│   ├── Enums/
│   │   ├── FuelType.cs                        NEW
│   │   ├── MissionProfileType.cs              NEW
│   │   ├── PartCategory.cs                    NEW
│   │   └── WarningType.cs                     MODIFIED  (+ 9 new values)
│   ├── Interfaces/
│   │   ├── ICelestialBodyRepository.cs        NEW
│   │   ├── IMissionRepository.cs              unchanged
│   │   ├── IPartCatalogueRepository.cs        NEW
│   │   └── IRocketRepository.cs               NEW
│   ├── Services/
│   │   ├── CelestialBodyDeltaVEstimator.cs    NEW
│   │   ├── ReadinessCalculator.cs             unchanged
│   │   ├── RocketDeltaVCalculator.cs          NEW
│   │   └── StageDeltaVCalculator.cs           NEW
│   └── ValueObjects/
│       ├── KerbinTime.cs                      unchanged
│       ├── KspBodyValue.cs                    unchanged
│       ├── MissionCalculationProfile.cs       NEW
│       ├── RequiredDeltaVResult.cs            NEW
│       ├── RocketDeltaVResult.cs              NEW
│       ├── StageDeltaVResult.cs               NEW
│       ├── StageEntry.cs                      NEW
│       └── Warning.cs                         unchanged
│
├── MissionControl.Infrastructure/
│   └── Persistence/
│       ├── JsonCelestialBodyRepository.cs     NEW
│       ├── JsonMissionRepository.cs           unchanged
│       ├── JsonPartCatalogueRepository.cs     NEW
│       └── JsonRocketRepository.cs            NEW
│
├── MissionControl.Api/
│   ├── Controllers/
│   │   ├── CelestialBodiesController.cs       NEW
│   │   ├── MissionsController.cs              MODIFIED  (rocket assignment + breakdown)
│   │   ├── PartsController.cs                 NEW
│   │   └── RocketsController.cs               NEW
│   ├── DTOs/
│   │   ├── CelestialBodyDto.cs                NEW
│   │   ├── CreateCustomBodyDto.cs             NEW
│   │   ├── CreateMissionDto.cs                MODIFIED  (+ AssignedRocketId, CalculationProfile)
│   │   ├── CreateRocketDto.cs                 NEW
│   │   ├── MissionCalculationProfileDto.cs    NEW
│   │   ├── MissionSummaryDto.cs               MODIFIED  (+ rocketName, breakdown fields)
│   │   ├── PartDto.cs                         NEW
│   │   ├── RequiredDeltaVBreakdownDto.cs      NEW
│   │   ├── RocketDeltaVBreakdownDto.cs        NEW
│   │   ├── RocketListItemDto.cs               NEW
│   │   ├── RocketSummaryDto.cs                NEW
│   │   ├── StageDeltaVDto.cs                  NEW
│   │   ├── StageDto.cs                        NEW
│   │   ├── StageEntryDto.cs                   NEW
│   │   └── UpdateRocketDto.cs                 NEW
│   ├── data/
│   │   ├── celestial-bodies.json              NEW  (seeded; 17 Kerbol system bodies)
│   │   ├── missions.json                      unchanged
│   │   └── parts.json                         NEW  (seeded; ~368 stock KSP parts)
│   └── Program.cs                             MODIFIED  (register new repositories)
│
└── MissionControl.Tests/
    ├── Api/
    │   ├── CelestialBodiesControllerTests.cs  NEW
    │   ├── PartsControllerTests.cs            NEW
    │   └── RocketsControllerTests.cs          NEW
    └── Domain/
        ├── CelestialBodyDeltaVEstimatorTests.cs  NEW
        ├── RocketDeltaVCalculatorTests.cs         NEW
        ├── RocketDeltaVRegressionTests.cs         NEW  ← regression suite (this constraint)
        └── StageDeltaVCalculatorTests.cs          NEW
```

### Frontend

```text
frontend/src/app/
├── models/
│   ├── celestial-body.model.ts    NEW
│   ├── mission.model.ts           MODIFIED  (+ rocket fields, profile, breakdown)
│   ├── part.model.ts              NEW
│   └── rocket.model.ts            NEW
│
├── services/
│   ├── celestial-bodies.service.ts  NEW
│   ├── missions.service.ts          unchanged
│   ├── parts.service.ts             NEW
│   └── rockets.service.ts           NEW
│
└── rockets/                         NEW feature module
    ├── rockets.routes.ts
    ├── rocket-list/
    │   ├── rocket-list.component.ts/html/scss
    ├── rocket-builder/
    │   ├── rocket-builder.component.ts/html/scss
    │   └── part-picker/
    │       └── part-picker.component.ts/html/scss
    └── rocket-detail/
        └── rocket-detail.component.ts/html/scss
```

*(Missions components modified to add rocket assignment UI — paths unchanged)*

## Complexity Tracking

No constitution violations. No unjustified complexity introduced.

