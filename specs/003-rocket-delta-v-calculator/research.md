# Research: Rocket-Based Mission Delta-V Calculator

**Phase**: 0 | **Date**: 2026-05-13 | **Plan**: [plan.md](plan.md)

## Research Tasks

Five areas required resolution before Phase 1 design: the Tsiolkovsky staging calculation
model, orbital mechanics formulas for required delta-v, the asparagus approximation model,
the KSP celestial body dataset, and the part catalogue data structure.

---

### 1. Tsiolkovsky Staging Calculation

**Decision**: Use the rocket equation per stage in KSP numbering order (highest stage number
fires first). Atmospheric Isp used for all stages on atmospheric launch bodies; vacuum Isp for
airless launch bodies. A configurable efficiency multiplier (default 85%) is applied to each
stage's delta-v on atmospheric bodies to account for gravity drag, steering losses, and ascent
profile imperfection.

**Formula**:

```
ΔV_stage = Isp × g₀ × ln(m_wet / m_dry)
```

where:
- `Isp` = specific impulse (seconds): `Isp_sl` for atmospheric launch body, `Isp_vac` for airless
- `g₀` = 9.80665 m/s² (standard gravity)
- `m_wet` = total rocket mass remaining at the start of this stage's burn (kg or tonnes — consistent)
- `m_dry` = m_wet minus all fuel consumed in this stage

**Stage mass accumulation** (KSP convention: Stage 1 = final/payload, highest number = launch):
1. Start from the highest-numbered (launch) stage; `accumulated_mass` = sum of all stages' wet mass
2. For each stage N from highest to 1:
   - `m_wet` = `accumulated_mass`
   - `fuel_in_stage_N` = sum over tanks in stage N of (wet_mass − dry_mass)
   - `m_dry` = `accumulated_mass` − `fuel_in_stage_N`
   - `ΔV_N` = Isp × g₀ × ln(m_wet / m_dry)
   - If stage N is jettisoned: `accumulated_mass` = m_dry − (hardware dry mass of stage N parts)
   - If not jettisoned: `accumulated_mass` = m_dry
3. Total ΔV = sum of all stage ΔVs

**Atmospheric efficiency multiplier**:
```
ΔV_effective = ΔV_stage × efficiency_factor
```
Applied to every stage on an atmospheric launch body. Default `efficiency_factor = 0.85`.

**Asparagus bonus** (applied on top of efficiency factor, atmospheric bodies only):
```
ΔV_effective = ΔV_stage × efficiency_factor × (1 + asparagus_bonus)
```
where `asparagus_bonus ∈ [0.0, 0.20]`. This partially offsets the efficiency penalty —
physically consistent because crossfeed staging reduces the mass hauled during ascent.

**Multiple engines in one stage — combined Isp**:
When a stage has engines of different Isp values, the effective Isp is thrust-weighted:
```
Isp_eff = (Σ thrust_i) / (Σ thrust_i / Isp_i)
```
This is the standard combined specific impulse formula. If all engines are identical, Isp_eff
equals the individual engine Isp (no change).

**Rationale**: This is the standard KSP community delta-v calculation method used by all major
planning tools (KSP Delta-V Map, KER, MechJeb). It is deterministic, pure, and well-understood.
Using Isp_sl for all stages on atmospheric launches is conservative (upper stages in reality fire
in vacuum with higher Isp); the efficiency multiplier is a separate, user-configurable factor
that addresses gravity/steering losses independently.

**Alternatives Considered**:
- *Per-stage Isp interpolation by altitude*: More accurate but requires atmospheric pressure
  profile per body and a model for when each stage fires — far exceeds PoC scope. Deferred to v2.
- *Reference table delta-v values*: Validated by community but cannot flex for custom rocket
  configurations. Rejected in favour of formula approach (Q1 decision in spec).

---

### 2. Required Delta-V: Orbital Mechanics Formulas

**Decision**: Required delta-v is calculated from celestial body parameters using orbital
mechanics formulas. Two tiers of calculation apply based on whether the mission is within a
single parent body's sphere of influence (SOI) or crosses SOI boundaries.

#### 2a. Circular Orbital Velocity

The orbital velocity at a circular orbit at altitude h above body surface:

```
v_circ = sqrt(μ / r)
       = sqrt(g_surface × R² / (R + h))
```

where:
- `g_surface` = surface gravity (m/s²)
- `R` = equatorial radius (m)
- `h` = target orbit altitude (m); default 80,000 m for Kerbin (LKO)
- `μ = g_surface × R²` (gravitational parameter approximation from surface data)

#### 2b. Ascent Delta-V Estimate

For bodies with atmosphere:
```
ΔV_ascent = v_circ(h_orbit) + gravity_drag_loss + atmospheric_drag_loss

gravity_drag_loss  = g_surface × 130       (seconds: typical gravity-turn burn time)
atmospheric_drag_loss = atm_pressure_atm × 200  (m/s per atm: linear drag approximation)
```

For airless bodies:
```
ΔV_ascent = v_circ(h_orbit) + g_surface × 60
```
(60 s accounts for a short direct-ascent gravity loss with no atmosphere)

These are planning approximations. The system MUST label them as "estimated" in the breakdown.

#### 2c. Same-SOI Transfer (Mun, Minmus, Ike, etc.)

Hohmann transfer between two circular orbits around the same parent body:

```
r₁ = R_launch + h_launch_orbit
r₂ = semi_major_axis_target (orbital radius of target from their common parent)
μ  = g_parent × R_parent²

ΔV_departure = sqrt(μ / r₁) × (sqrt(2 × r₂ / (r₁ + r₂)) - 1)
ΔV_arrival   = sqrt(μ / r₂) × (1 - sqrt(2 × r₁ / (r₁ + r₂)))
ΔV_transfer  = |ΔV_departure| + |ΔV_arrival|
```

Used when: launch body and target body share the same parent body in the seed data
(e.g., Kerbin → Mun, both orbiting Kerbol? No — Mun orbits Kerbin, Kerbin orbits Kerbol.
Kerbin→Mun is same-SOI with Kerbin as parent. Kerbin→Duna is Kerbol-SOI transfer).

#### 2d. Interplanetary Transfer (cross-SOI)

For missions where the launch body and target body orbit the same star (Kerbol):
```
r₁ = semi_major_axis_launch_body (from Kerbol)
r₂ = semi_major_axis_target_body (from Kerbol)
μ  = g_Kerbol × R_Kerbol²

ΔV_ejection  = sqrt(μ / r₁) × (sqrt(2 × r₂ / (r₁ + r₂)) - 1)  [approximate; ignores hyperbolic excess]
ΔV_insertion = sqrt(μ / r₂) × (1 - sqrt(2 × r₁ / (r₁ + r₂)))
ΔV_transfer  = ΔV_ejection + ΔV_insertion
```

This is a simplified Hohmann approximation in Kerbol's SOI. It does not account for SOI
transition costs or the Oberth effect. It MUST be labelled "approximated" in the breakdown.

**SOI determination logic**:
- If `target.ParentBodyId == launch.ParentBodyId` → same-SOI transfer (use parent's μ)
- If `target.ParentBodyId == launch.Id` → landing on a moon (ascent from launch + descent to target)
- If `launch.ParentBodyId == target.Id` → ascending from a moon to its parent planet
- Otherwise → interplanetary (both bodies orbit Kerbol, use Kerbol μ)

#### 2e. Mission Profile Components

| Profile Type | Required ΔV Components |
|---|---|
| **Orbit Insertion** (default) | ΔV_ascent(launch) + ΔV_transfer + ΔV_arrival_insertion |
| **Ascent Only** | ΔV_ascent(launch) |
| **Surface Landing** | Orbit Insertion ΔV + ΔV_descent(target) |
| **Full Return** | Surface Landing ΔV + ΔV_ascent(target) + ΔV_return_transfer + ΔV_return_insertion(launch) |

**ΔV_descent** approximation (powered descent to surface):
```
ΔV_descent = v_circ(target low orbit) + g_target × 30
```
For atmospheric targets (Kerbin, Duna, Laythe, Eve, Jool): parachute-assist makes powered
descent optional; system uses ΔV_descent × 0.3 (conservative: retro burn + final landing).

**Rationale**: Simplified Hohmann is the standard approach for KSP mission planning tools.
It produces estimates within 10–15% of real KSP manoeuvre costs for most stock routes —
acceptable for a planning tool. All estimates are clearly labelled. Users can override.

---

### 3. Asparagus Approximation Model

**Decision**: The asparagus bonus is a multiplicative correction on the efficiency-adjusted
per-stage delta-v, applied to atmospheric launches only:

```
ΔV_stage_final = Isp × g₀ × ln(m_wet / m_dry) × efficiency_factor × (1 + asparagus_bonus)
```

Slider reference points:

| Label | Value | Interpretation |
|---|---|---|
| (off) | 0% | No crossfeed benefit — sequential staging only |
| Conservative | 8% | Modest asparagus stack; some mass efficiency from crossfeed |
| Moderate | 12% | Solid asparagus design; good crossfeed routing |
| Optimistic | 15% | Well-optimised crossfeed; near-ideal staging |
| Aggressive (Maximum) | 20% | Hard cap; theoretical ceiling for most designs in this tool |

Real KSP designs can achieve 30–50% crossfeed efficiency. The 20% cap is intentional and
must be displayed prominently to avoid over-confidence in planning estimates. The slider
default when first enabled is Conservative (8%).

The asparagus bonus is disabled (forced to 0%) when the launch body has no atmosphere,
because crossfeed staging provides no differential benefit in vacuum (all engines run
at full vacuum Isp regardless of staging arrangement).

---

### 4. Kerbol System Celestial Body Seed Data

All values from KSP 1.12.x wiki (source: `Kerbol System_Table - Kerbal Space Program Wiki.html`).

| Body | Parent | Radius (m) | Gravity (m/s²) | Atm (atm) | Atm Height (m) | SOI (m) | Semi-Major Axis (m) |
|---|---|---|---|---|---|---|---|
| Kerbol | — | 261,600,000 | 17.10 | 0 | 0 | — | — |
| Moho | Kerbol | 250,000 | 2.70 | 0 | 0 | 9,646,663 | 5,263,138,304 |
| Eve | Kerbol | 700,000 | 16.70 | 5.0 | 90,000 | 85,109,365 | 9,832,684,544 |
| Gilly | Eve | 13,000 | 0.049 | 0 | 0 | 126,123 | 31,500,000 |
| Kerbin | Kerbol | 600,000 | 9.81 | 1.0 | 70,000 | 84,159,286 | 13,599,840,256 |
| Mun | Kerbin | 200,000 | 1.63 | 0 | 0 | 2,429,559 | 12,000,000 |
| Minmus | Kerbin | 60,000 | 0.491 | 0 | 0 | 2,247,428 | 47,000,000 |
| Duna | Kerbol | 320,000 | 2.94 | 0.0667 | 50,000 | 47,921,949 | 20,726,155,264 |
| Ike | Duna | 130,000 | 1.10 | 0 | 0 | 1,049,599 | 3,200,000 |
| Dres | Kerbol | 138,000 | 1.13 | 0 | 0 | 32,832,840 | 40,839,348,203 |
| Jool | Kerbol | 6,000,000 | 7.85 | 15.0 | 200,000 | 2,455,985,185 | 68,773,560,320 |
| Laythe | Jool | 500,000 | 7.85 | 0.8 | 50,000 | 3,723,645 | 27,184,000 |
| Vall | Jool | 300,000 | 2.31 | 0 | 0 | 2,406,401 | 43,152,000 |
| Tylo | Jool | 600,000 | 7.85 | 0 | 0 | 10,856,518 | 68,500,000 |
| Bop | Jool | 65,000 | 0.589 | 0 | 0 | 1,221,060 | 128,500,000 |
| Pol | Jool | 44,000 | 0.373 | 0 | 0 | 1,042,138 | 179,890,000 |
| Eeloo | Kerbol | 210,000 | 1.69 | 0 | 0 | 119,082,941 | 90,118,820,000 |

Default target orbit altitudes (m) for required ΔV estimation:

| Body | Default Orbit Altitude |
|---|---|
| Kerbin | 80,000 |
| Mun | 10,000 |
| Minmus | 10,000 |
| Duna | 50,000 |
| Eve | 100,000 |
| All others | max(10,000, atm_height + 10,000) |

The `celestial-bodies.json` seed file will be structured as:
```json
{
  "stockBodies": [ /* 17 entries from table above */ ],
  "customBodies": []
}
```

---

### 5. Part Catalogue Data Structure

**Decision**: Each part has a canonical `id` (slug), display name, category, dry mass, wet mass,
optional fuel capacity map, and optional engine stats. Only fields required for delta-v
calculation or UI display are seeded; visual/flavour properties are excluded.

**Part record shape**:
```json
{
  "id": "lv-t45",
  "name": "LV-T45 'Swivel' Liquid Fuel Engine",
  "category": "Engines",
  "dryMass": 1.5,
  "wetMass": 1.5,
  "fuelCapacity": null,
  "engineStats": {
    "thrustSeaLevel": 167.97,
    "thrustVacuum": 215.0,
    "ispSeaLevel": 270,
    "ispVacuum": 320,
    "fuelTypes": ["LiquidFuelOxidizer"]
  }
}
```

**Fuel tank record shape** (example):
```json
{
  "id": "fl-t400",
  "name": "FL-T400 Fuel Tank",
  "category": "FuelTanks",
  "dryMass": 0.25,
  "wetMass": 2.25,
  "fuelCapacity": {
    "LiquidFuel": 180,
    "Oxidizer": 220
  },
  "engineStats": null
}
```

**SRB record shape**: An SRB is both an engine and a fuel tank in KSP (self-contained).
The SRB part entry will have `engineStats` for the thruster data AND `fuelCapacity` for the
solid fuel it carries. When calculating delta-v for a stage with an SRB, the SRB's own
fuel (solid) is the fuel source — no separate tank needed.

**Source**: `Parts - Kerbal Space Program Wiki.html` — all 368 stock base-game parts
(KSP 1.12.5). DLC parts (Breaking Ground, Making History) are excluded. The wiki HTML
is the authoritative source; `parts.json` is generated from it during implementation.

**Categories used**:
`Pods`, `FuelTanks`, `Engines`, `CommandAndControl`, `Structural`, `Coupling`, `Payload`,
`Aerodynamics`, `Ground`, `Thermal`, `Electrical`, `Communication`, `Science`, `Cargo`, `Utility`

Parts with no fuel and no engine stats contribute only to dry mass of their stage.

---

### 6. Mission Entity Extension Design

**Decision**: `Mission` stores two new optional fields (`AssignedRocketId`, `CalculationProfile`)
and keeps `AvailableDeltaV` and `RequiredDeltaV` as scalars. When a rocket is assigned, the
API controller calls the domain services to compute both values and passes the results to
`Mission.Update()` as it always has. `Mission` itself has no dependency on `Rocket`.

This approach:
- Preserves the existing `ReadinessCalculator` interface unchanged
- Maintains the `Mission` aggregate as self-contained (no cross-aggregate references in domain)
- Makes calculated values recomputed on every `GET /api/missions/{id}` when a rocket is assigned
  (ensures rocket edits are always reflected without an event-driven update mechanism)
- Existing missions without a rocket continue using manually entered delta-v (FR-035 preserved)

The controller flow for a mission with a rocket:
1. Load `Mission`, `Rocket`, `CatalogueParts`, `CelestialBodies`
2. Call `RocketDeltaVCalculator.Calculate(rocket, parts, launchBody)` → `availableDv`
3. Call `CelestialBodyDeltaVEstimator.Estimate(launchBody, targetBody, profile)` → `requiredDv`
   (or use `profile.RequiredDeltaVOverride` if set)
4. Pass computed scalars to `Mission.Update(...)` as before
5. Return updated `MissionSummaryDto` with full breakdown
