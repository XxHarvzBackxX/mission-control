# API Contract: Mission Readiness Planner

**Phase**: 1 | **Date**: 2026-05-12 (updated after amendment) | **Plan**: [plan.md](../plan.md) | **Data Model**: [data-model.md](../data-model.md)

**Base URL**: `http://localhost:5000/api`

**Format**: JSON (`Content-Type: application/json`)

**Auth**: None (single-user PoC)

---

## Endpoints

### GET `/missions` — List All Missions

Returns all saved missions with abbreviated display data.

**Response `200 OK`**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Mun Landing Alpha",
    "readinessState": "Ready",
    "controlMode": "Crewed",
    "crewSummary": "Jebediah +1",
    "probeCoreValue": null,
    "warnings": []
  },
  {
    "id": "7cb12a34-1234-4321-abcd-ef1234567890",
    "name": "Duna Probe Survey",
    "readinessState": "AtRisk",
    "controlMode": "Probe",
    "crewSummary": null,
    "probeCoreValue": "Probodobodyne HECS",
    "warnings": [
      {
        "type": "LowReserveMargin",
        "message": "Reserve margin is 6.2% — below the 10% safety threshold.",
        "isBlocking": false
      }
    ]
  }
]
```

**Response `200 OK`** (empty): `[]`

---

### POST `/missions` — Create Mission

Creates a new mission and returns the full summary with evaluated readiness.

**Request body — Crewed mission**:
```json
{
  "name": "Mun Landing Alpha",
  "targetBodyValue": "Mun",
  "targetBodyIsCustom": false,
  "missionTypeValue": "Landing",
  "missionTypeIsCustom": false,
  "availableDeltaV": 5200.0,
  "requiredDeltaV": 4500.0,
  "controlMode": "Crewed",
  "crewMembers": ["Jebediah Kerman", "Bill Kerman"],
  "probeCoreValue": null,
  "probeCoreIsCustom": false,
  "startMissionTime": 9244800,
  "endMissionTime": 9417600
}
```

**Request body — Probe mission**:
```json
{
  "name": "Duna Probe Survey",
  "targetBodyValue": "Duna",
  "targetBodyIsCustom": false,
  "missionTypeValue": "Orbital",
  "missionTypeIsCustom": false,
  "availableDeltaV": 2100.0,
  "requiredDeltaV": 1975.0,
  "controlMode": "Probe",
  "crewMembers": [],
  "probeCoreValue": "Probodobodyne HECS",
  "probeCoreIsCustom": false,
  "startMissionTime": null,
  "endMissionTime": null
}
```

**Request body — modded planet + custom probe**:
```json
{
  "name": "Neidon Survey",
  "targetBodyValue": "Neidon",
  "targetBodyIsCustom": true,
  "missionTypeValue": "Flyby",
  "missionTypeIsCustom": false,
  "availableDeltaV": 12000.0,
  "requiredDeltaV": 11000.0,
  "controlMode": "Probe",
  "crewMembers": [],
  "probeCoreValue": "RA-100 Relay Antenna Core",
  "probeCoreIsCustom": true,
  "startMissionTime": null,
  "endMissionTime": null
}
```

**Response `201 Created`**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Mun Landing Alpha",
  "targetBodyValue": "Mun",
  "targetBodyIsCustom": false,
  "missionTypeValue": "Landing",
  "missionTypeIsCustom": false,
  "availableDeltaV": 5200.0,
  "requiredDeltaV": 4500.0,
  "reserveMarginPercent": 15.56,
  "readinessState": "Ready",
  "controlMode": "Crewed",
  "crewMembers": ["Jebediah Kerman", "Bill Kerman"],
  "probeCoreValue": null,
  "probeCoreIsCustom": false,
  "startMissionTime": 9244800,
  "endMissionTime": 9417600,
  "warnings": []
}
```

**Response `400 Bad Request`** — validation failure:
```json
{
  "errors": [
    { "field": "availableDeltaV", "message": "Must be greater than zero." }
  ]
}
```

**Response `400 Bad Request`** — crewed mission with no crew:
```json
{
  "errors": [
    { "field": "crewMembers", "message": "At least one crew member is required for a Crewed mission." }
  ]
}
```

**Response `400 Bad Request`** — probe mission with no probe core:
```json
{
  "errors": [
    { "field": "probeCoreValue", "message": "A probe core is required for a Probe mission." }
  ]
}
```

**Response `400 Bad Request`** — End MT before Start MT:
```json
{
  "errors": [
    { "field": "endMissionTime", "message": "End Mission Time must be later than Start Mission Time." }
  ]
}
```

**Response `409 Conflict`** — duplicate name:
```json
{
  "errors": [
    { "field": "name", "message": "A mission named 'Mun Landing Alpha' already exists." }
  ]
}
```

---

### GET `/missions/{id}` — Get Mission Summary

Returns the full summary of a single mission.

**Path param**: `id` — `Guid`

**Response `200 OK`**: Same shape as `POST /missions` `201 Created` body.

**Response `404 Not Found`**:
```json
{ "message": "Mission '3fa85f64-5717-4562-b3fc-2c963f66afa6' not found." }
```

---

### PUT `/missions/{id}` — Update Mission

Replaces all editable fields. Readiness is re-evaluated and returned.

**Path param**: `id` — `Guid`

**Request body**: Same shape as `POST /missions`.

**Response `200 OK`**: Same shape as `GET /missions/{id}` response.

**Response `400 Bad Request`**: Same shapes as `POST /missions` errors.

**Response `404 Not Found`**: Same shape as `GET /missions/{id}` not-found.

**Response `409 Conflict`**: Same shape as `POST /missions` conflict (name taken by a *different* mission).

---

### DELETE `/missions/{id}` — Delete Mission

**Path param**: `id` — `Guid`

**Response `204 No Content`**: Deleted. No body.

**Response `404 Not Found`**:
```json
{ "message": "Mission '3fa85f64-5717-4562-b3fc-2c963f66afa6' not found." }
```

---

### GET `/missions/reference-data` — Predefined Lists

Returns the predefined option lists for all dropdown fields.

**Response `200 OK`**:
```json
{
  "targetBodies": [
    "Kerbol", "Moho", "Eve", "Gilly", "Kerbin", "Mun", "Minmus",
    "Duna", "Ike", "Dres", "Jool", "Laythe", "Vall", "Tylo",
    "Bop", "Pol", "Eeloo"
  ],
  "missionTypes": [
    "Orbital", "Landing", "Flyby", "Transfer", "Rescue",
    "Station Resupply", "Return"
  ],
  "probeCores": [
    "Stayputnik",
    "Probodobodyne OKTO",
    "Probodobodyne HECS",
    "Probodobodyne QBE",
    "Probodobodyne OKTO2",
    "Probodobodyne HECS2",
    "Probodobodyne RoveMate",
    "RC-001S Remote Guidance Unit",
    "RC-L01 Remote Guidance Unit",
    "MK2 Drone Core"
  ]
}
```

> The "Other (custom)" option is added by the frontend — it is not part of this response. `isCustom: true` in a request signals the backend to skip predefined-list validation.

---

## Field Validation Rules

| Field | Rule |
|-------|------|
| `name` | Required; 1–200 chars; unique across all missions |
| `targetBodyValue` | Required; if `targetBodyIsCustom = false`, must be a predefined target body |
| `targetBodyIsCustom` | Required boolean |
| `missionTypeValue` | Required; if `missionTypeIsCustom = false`, must be a predefined mission type |
| `missionTypeIsCustom` | Required boolean |
| `availableDeltaV` | Required; numeric; > 0 |
| `requiredDeltaV` | Required; numeric; > 0 |
| `controlMode` | Required; must be `"Crewed"` or `"Probe"` |
| `crewMembers` | When `controlMode = "Crewed"`: must contain ≥ 1 non-empty string. When `controlMode = "Probe"`: must be empty or omitted |
| `probeCoreValue` | When `controlMode = "Probe"`: required; if `probeCoreIsCustom = false`, must be a predefined probe core. When `controlMode = "Crewed"`: must be null/omitted |
| `probeCoreIsCustom` | Required when `controlMode = "Probe"`; ignored when `controlMode = "Crewed"` |
| `startMissionTime` | Optional; if provided, must be ≥ 0 |
| `endMissionTime` | Optional; if provided, must be ≥ 0; if `startMissionTime` is also set, must be > `startMissionTime` |

---

## Readiness State Values

| `readinessState` | Meaning |
|------------------|---------|
| `"Ready"` | All requirements met; ΔV margin ≥ 10% |
| `"AtRisk"` | Requirements met; ΔV margin < 10% |
| `"NotReady"` | Blocking condition present (insufficient ΔV or missing crew) |

---

## Warning Type Reference

| `type` | `isBlocking` | Trigger |
|--------|-------------|---------|
| `"InsufficientDeltaV"` | `true` | `availableDeltaV < requiredDeltaV` |
| `"LowReserveMargin"` | `false` | Reserve margin < 10% (fires on both AtRisk and NotReady) |
| `"MissingRequiredField"` | `true` | A required field is absent or invalid |
| `"MissingCrew"` | `true` | `controlMode = "Crewed"` and `crewMembers` is empty |
| `"InvalidTimeRange"` | `true` | `endMissionTime ≤ startMissionTime` when both set |
| `"AdvisoryEndTimeWithoutStart"` | `false` | `endMissionTime` set but `startMissionTime` is null |

---

## Error Response Shapes

Field-level errors:
```json
{ "errors": [{ "field": "fieldName", "message": "Description." }] }
```

Resource-level errors (404, etc.):
```json
{ "message": "Description." }
```

**Base URL**: `http://localhost:5000/api`

**Format**: JSON (`Content-Type: application/json`)

**Auth**: None (single-user PoC)

---

## Endpoints

### GET `/missions` — List All Missions

Returns all saved missions with their names and current readiness states.

**Response `200 OK`**:
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Mun Landing Alpha",
    "readinessState": "Ready",
    "warnings": []
  },
  {
    "id": "7cb12a34-1234-4321-abcd-ef1234567890",
    "name": "Eve Ascent Attempt",
    "readinessState": "NotReady",
    "warnings": [
      {
        "type": "InsufficientDeltaV",
        "message": "Available delta-v (3200 m/s) is less than required (10500 m/s)."
      },
      {
        "type": "LowReserveMargin",
        "message": "Reserve margin is -69.5% — below the 10% safety threshold."
      }
    ]
  }
]
```

**Response `200 OK` (empty list)**:
```json
[]
```

---

### POST `/missions` — Create Mission

Creates a new mission and returns the full summary including evaluated readiness.

**Request body**:
```json
{
  "name": "Mun Landing Alpha",
  "targetBodyValue": "Mun",
  "targetBodyIsCustom": false,
  "missionTypeValue": "Landing",
  "missionTypeIsCustom": false,
  "availableDeltaV": 5200.0,
  "requiredDeltaV": 4500.0
}
```

**Custom body example** (modded planet):
```json
{
  "name": "Neidon Flyby",
  "targetBodyValue": "Neidon",
  "targetBodyIsCustom": true,
  "missionTypeValue": "Flyby",
  "missionTypeIsCustom": false,
  "availableDeltaV": 8000.0,
  "requiredDeltaV": 7200.0
}
```

**Response `201 Created`**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Mun Landing Alpha",
  "targetBodyValue": "Mun",
  "targetBodyIsCustom": false,
  "missionTypeValue": "Landing",
  "missionTypeIsCustom": false,
  "availableDeltaV": 5200.0,
  "requiredDeltaV": 4500.0,
  "reserveMarginPercent": 15.56,
  "readinessState": "Ready",
  "warnings": []
}
```

**Response `400 Bad Request`** — validation failure (missing field or invalid value):
```json
{
  "errors": [
    { "field": "availableDeltaV", "message": "Must be greater than zero." }
  ]
}
```

**Response `409 Conflict`** — duplicate mission name:
```json
{
  "errors": [
    { "field": "name", "message": "A mission named 'Mun Landing Alpha' already exists." }
  ]
}
```

---

### GET `/missions/{id}` — Get Mission Summary

Returns the full summary of a single mission.

**Path param**: `id` — `Guid`

**Response `200 OK`**:
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Mun Landing Alpha",
  "targetBodyValue": "Mun",
  "targetBodyIsCustom": false,
  "missionTypeValue": "Landing",
  "missionTypeIsCustom": false,
  "availableDeltaV": 5200.0,
  "requiredDeltaV": 4500.0,
  "reserveMarginPercent": 15.56,
  "readinessState": "Ready",
  "warnings": []
}
```

**Response `404 Not Found`**:
```json
{ "message": "Mission '3fa85f64-5717-4562-b3fc-2c963f66afa6' not found." }
```

---

### PUT `/missions/{id}` — Update Mission

Replaces all editable fields of an existing mission. Readiness is re-evaluated and returned.

**Path param**: `id` — `Guid`

**Request body**: Same shape as `POST /missions`.

**Response `200 OK`**: Same shape as `GET /missions/{id}` response.

**Response `400 Bad Request`**: Same shape as `POST /missions` validation error.

**Response `404 Not Found`**: Same shape as `GET /missions/{id}` not-found error.

**Response `409 Conflict`**: Same shape as `POST /missions` conflict error (name taken by a *different* mission).

---

### DELETE `/missions/{id}` — Delete Mission

Permanently removes a mission.

**Path param**: `id` — `Guid`

**Response `204 No Content`**: Mission deleted successfully. No body.

**Response `404 Not Found`**:
```json
{ "message": "Mission '3fa85f64-5717-4562-b3fc-2c963f66afa6' not found." }
```

---

### GET `/missions/reference-data` — Get Predefined Lists

Returns the predefined target body and mission type options for populating dropdowns in the frontend.

**Response `200 OK`**:
```json
{
  "targetBodies": [
    "Kerbol", "Moho", "Eve", "Gilly", "Kerbin", "Mun", "Minmus",
    "Duna", "Ike", "Dres", "Jool", "Laythe", "Vall", "Tylo",
    "Bop", "Pol", "Eeloo"
  ],
  "missionTypes": [
    "Orbital", "Landing", "Flyby", "Transfer", "Rescue",
    "Station Resupply", "Return"
  ]
}
```

> The "Other (custom)" option is added by the frontend UI — it is not part of the server response. When a custom value is submitted, `isCustom: true` signals the server to skip predefined-list validation.

---

## Field Validation Rules

| Field | Rule |
|-------|------|
| `name` | Required; 1–200 characters; unique across all missions |
| `targetBodyValue` | Required; if `targetBodyIsCustom = false`, must be one of the predefined target body values |
| `targetBodyIsCustom` | Required boolean |
| `missionTypeValue` | Required; if `missionTypeIsCustom = false`, must be one of the predefined mission type values |
| `missionTypeIsCustom` | Required boolean |
| `availableDeltaV` | Required; numeric; > 0 |
| `requiredDeltaV` | Required; numeric; > 0 |

---

## Readiness State Mapping

| `readinessState` value | Meaning |
|------------------------|---------|
| `"Ready"` | Available ΔV ≥ required ΔV × 1.10 |
| `"AtRisk"` | Available ΔV ≥ required ΔV but reserve margin < 10% |
| `"NotReady"` | Available ΔV < required ΔV |

---

## Warning Type Mapping

| `type` value | Trigger |
|--------------|---------|
| `"InsufficientDeltaV"` | `availableDeltaV < requiredDeltaV` |
| `"LowReserveMargin"` | Reserve margin < 10% (fires on both `AtRisk` and `NotReady`) |
| `"MissingField"` | A required field is absent or invalid at save time |

---

## Error Response Shape (all errors)

```json
{
  "errors": [
    { "field": "fieldName", "message": "Human-readable description." }
  ]
}
```

Or for resource-level errors (404, etc.):

```json
{
  "message": "Human-readable description."
}
```
