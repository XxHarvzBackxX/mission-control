# API Contract: Rocket-Based Mission Delta-V Calculator

**Phase**: 1 | **Date**: 2026-05-13 | **Plan**: [plan.md](../plan.md)

Base URL: `/api`  
Content-Type: `application/json`  
All IDs are GUIDs unless noted as strings (catalogue part IDs and celestial body IDs are slugs for stock entries).

---

## Rockets

### `GET /api/rockets`

Returns all saved rockets with summary information.

**Response 200**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Kerbin Express Mk1",
    "stageCount": 2,
    "totalEffectiveDeltaV": 4200.5,
    "warnings": [
      { "type": "NoCommandPart", "message": "Rocket has no command pod or probe core.", "isBlocking": false }
    ]
  }
]
```

---

### `GET /api/rockets/{id}`

Returns full rocket detail including stage breakdown and delta-v calculation.

**Response 200**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Kerbin Express Mk1",
  "description": "Two-stage LKO rocket",
  "notes": null,
  "usesAsparagusStaging": false,
  "asparagusEfficiencyBonus": 0.0,
  "stages": [
    {
      "id": "a1b2c3d4-...",
      "stageNumber": 2,
      "name": "Launch Stage",
      "isJettisoned": true,
      "parts": [
        { "partId": "fl-t800", "quantity": 1 },
        { "partId": "lv-t45", "quantity": 1 }
      ],
      "notes": null
    },
    {
      "id": "e5f6a7b8-...",
      "stageNumber": 1,
      "name": "Upper Stage",
      "isJettisoned": false,
      "parts": [
        { "partId": "fl-t400", "quantity": 1 },
        { "partId": "lv-909", "quantity": 1 },
        { "partId": "mk1-pod", "quantity": 1 }
      ],
      "notes": null
    }
  ],
  "deltaVBreakdown": {
    "totalEffectiveDeltaV": 4200.5,
    "stages": [
      {
        "stageNumber": 2,
        "stageName": "Launch Stage",
        "wetMass": 8.59,
        "dryMass": 4.59,
        "ispUsed": 270.0,
        "rawDeltaV": 1962.8,
        "efficiencyFactor": 0.85,
        "asparagusBonus": 0.0,
        "effectiveDeltaV": 1668.4,
        "warnings": []
      },
      {
        "stageNumber": 1,
        "stageName": "Upper Stage",
        "wetMass": 3.59,
        "dryMass": 1.59,
        "ispUsed": 345.0,
        "rawDeltaV": 2752.4,
        "efficiencyFactor": 1.0,
        "asparagusBonus": 0.0,
        "effectiveDeltaV": 2752.4,
        "warnings": []
      }
    ],
    "warnings": []
  }
}
```

**Response 404**: `{ "message": "Rocket '{id}' not found." }`

---

### `POST /api/rockets`

Creates a new rocket.

**Request body**:
```json
{
  "name": "Kerbin Express Mk1",
  "description": "Two-stage LKO rocket",
  "notes": null,
  "usesAsparagusStaging": false,
  "asparagusEfficiencyBonus": 0.0,
  "stages": [
    {
      "stageNumber": 2,
      "name": "Launch Stage",
      "isJettisoned": true,
      "parts": [
        { "partId": "fl-t800", "quantity": 1 },
        { "partId": "lv-t45", "quantity": 1 }
      ],
      "notes": null
    }
  ]
}
```

**Response 201**: Full `RocketSummaryDto` (same shape as `GET /api/rockets/{id}`)  
**Response 400**: `{ "errors": [ { "field": "name", "message": "..." } ] }`  
**Response 409**: `{ "errors": [ { "field": "name", "message": "A rocket named '...' already exists." } ] }`

---

### `PUT /api/rockets/{id}`

Replaces a rocket's configuration. All stages and parts are replaced atomically.

**Request body**: Same shape as `POST /api/rockets`

**Response 200**: Updated `RocketSummaryDto`  
**Response 404**: Not found  
**Response 400**: Validation errors  
**Response 409**: Name conflict

---

### `DELETE /api/rockets/{id}`

Deletes a rocket. Returns the number of affected missions.

**Response 200**:
```json
{
  "deletedRocketId": "3fa85f64-...",
  "affectedMissionCount": 2
}
```

**Response 404**: Not found

*Note*: Deletion proceeds regardless of mission assignments. Affected missions will display a
missing-rocket warning on next load (FR-005).

---

## Parts (Catalogue — Read Only)

### `GET /api/parts`

Returns all catalogue parts, with optional filtering.

**Query parameters**:
- `category` (optional): filter by `PartCategory` enum value (e.g., `?category=Engines`)
- `search` (optional): partial name match, case-insensitive (e.g., `?search=lv-t`)
- Both parameters can be combined.

**Response 200**:
```json
[
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
  },
  {
    "id": "fl-t400",
    "name": "FL-T400 Fuel Tank",
    "category": "FuelTanks",
    "dryMass": 0.25,
    "wetMass": 2.25,
    "fuelCapacity": { "LiquidFuel": 180, "Oxidizer": 220 },
    "engineStats": null
  }
]
```

---

### `GET /api/parts/{id}`

Returns a single part by its slug ID.

**Response 200**: Single `PartDto`  
**Response 404**: `{ "message": "Part '{id}' not found." }`

---

## Celestial Bodies

### `GET /api/celestial-bodies`

Returns all stock and custom celestial bodies.

**Response 200**:
```json
[
  {
    "id": "kerbin",
    "name": "Kerbin",
    "parentBodyId": "kerbol",
    "equatorialRadius": 600000,
    "surfaceGravity": 9.81,
    "surfacePressure": 1.0,
    "atmosphereHeight": 70000,
    "sphereOfInfluence": 84159286,
    "semiMajorAxis": 13599840256,
    "defaultOrbitAltitude": 80000,
    "isCustom": false
  }
]
```

---

### `POST /api/celestial-bodies/custom`

Creates a user-defined custom celestial body.

**Request body**:
```json
{
  "name": "Alternis Kerbin",
  "equatorialRadius": 550000,
  "surfaceGravity": 8.5,
  "surfacePressure": 0.8,
  "atmosphereHeight": 60000,
  "sphereOfInfluence": 75000000,
  "semiMajorAxis": null
}
```

**Response 201**: Full `CelestialBodyDto` with `isCustom: true` and generated GUID `id`  
**Response 400**: Validation errors (radius or gravity ≤ 0; pressure < 0)

---

## Missions (Updated Endpoints)

The existing missions endpoints are extended to accept rocket assignment and calculation
profile. All existing fields remain unchanged and optional.

### `POST /api/missions` and `PUT /api/missions/{id}` — Additional Fields

New optional fields in `CreateMissionDto` / `UpdateMissionDto`:

```json
{
  "assignedRocketId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "calculationProfile": {
    "launchBodyId": "kerbin",
    "targetBodyId": "mun",
    "profileType": "OrbitInsertion",
    "targetOrbitAltitude": 80000,
    "atmosphericEfficiencyMultiplier": 0.85,
    "safetyMarginPercent": 10.0,
    "requiredDeltaVOverride": null
  }
}
```

When `assignedRocketId` is provided:
- Backend loads the rocket and calculates `availableDeltaV` from `RocketDeltaVCalculator`
- Backend uses `calculationProfile` to calculate `requiredDeltaV` from `CelestialBodyDeltaVEstimator`
  (or uses `requiredDeltaVOverride` if set)
- Calculated values are passed to `Mission.Update()` as the scalar `availableDeltaV` and `requiredDeltaV`
- If the rocket does not exist: `400 Bad Request` with `{ "field": "assignedRocketId", "message": "..." }`

When `assignedRocketId` is null (or absent):
- `availableDeltaV` and `requiredDeltaV` must be provided manually (existing behaviour)

### `GET /api/missions/{id}` — Additional Response Fields

```json
{
  "assignedRocketId": "3fa85f64-...",
  "rocketName": "Kerbin Express Mk1",
  "calculationProfile": {
    "launchBodyId": "kerbin",
    "targetBodyId": "mun",
    "profileType": "OrbitInsertion",
    "targetOrbitAltitude": 80000,
    "atmosphericEfficiencyMultiplier": 0.85,
    "safetyMarginPercent": 10.0,
    "requiredDeltaVOverride": null
  },
  "requiredDeltaVBreakdown": {
    "totalRequiredDeltaV": 3850.0,
    "ascentDeltaV": 3400.0,
    "transferDeltaV": 860.0,
    "descentDeltaV": 0.0,
    "returnDeltaV": 0.0,
    "estimationMethod": "Ascent: v_circ + gravity/drag losses. Transfer: Hohmann approximation (Kerbin→Mun same-SOI).",
    "isApproximated": true
  }
}
```

All other existing response fields remain unchanged.

---

## Error Response Shape

All error responses use the existing shape from 001:
```json
{ "errors": [ { "field": "fieldName", "message": "Human-readable message." } ] }
```
or for not-found:
```json
{ "message": "Resource 'x' not found." }
```
