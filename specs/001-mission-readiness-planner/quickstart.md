# Quickstart: Mission Readiness Planner

**Phase**: 1 | **Date**: 2026-05-12 (updated after amendment) | **Plan**: [plan.md](plan.md)

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 8.0+ | Backend API |
| Node.js | 20 LTS+ | Frontend build |
| Angular CLI | Latest stable | Frontend scaffolding and dev server |

Verify with:
```bash
dotnet --version
node --version
ng version
```

---

## Repository Structure

```
mission-control/
├── backend/          # .NET solution (Domain, Infrastructure, Api, Tests)
├── frontend/         # Angular SPA
└── specs/            # Design documents (this folder)
```

---

## Running the Backend

```bash
cd backend/MissionControl.Api
dotnet run
```

The API starts on `http://localhost:5000` by default.

**Configuration** (`backend/MissionControl.Api/appsettings.Development.json`):
```json
{
  "JsonStorage": {
    "FilePath": "data/missions.json"
  }
}
```

The `data/` directory and `missions.json` are created automatically on first run if absent.

**Run backend tests** (NUnit):
```bash
cd backend/MissionControl.Tests
dotnet test
```

---

## Running the Frontend

```bash
cd frontend
npm install
ng serve
```

The SPA starts on `http://localhost:4200` and proxies API calls to `http://localhost:5000/api`.

**Run frontend tests**:
```bash
cd frontend
ng test
```

---

## Verify the Setup

1. Open `http://localhost:4200` — the mission list shows an empty-state message.
2. Click **New Mission** and create a crewed test mission:
   - Name: `Mun Test`
   - Target Body: `Mun`
   - Mission Type: `Landing`
   - Available ΔV: `5200`
   - Required ΔV: `4500`
   - Control Mode: `Crewed`
   - Crew: `Jebediah Kerman`
3. Save → **Ready**, no warnings.
4. Edit: set Available ΔV to `4000` → **Not Ready** with two red warning boxes (`InsufficientDeltaV` + `LowReserveMargin`).
5. Edit: set Available ΔV back to `4600` → **AtRisk** with one warning (`LowReserveMargin`).
6. Remove the crew member → **Not Ready** with `MissingCrew` warning.
7. Delete the mission → empty-state list returns.

---

## Quick API Smoke Test (curl)

```bash
# Get reference data (predefined lists)
curl -s http://localhost:5000/api/missions/reference-data | jq .

# Create a crewed mission
curl -s -X POST http://localhost:5000/api/missions \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mun Test",
    "targetBodyValue": "Mun",
    "targetBodyIsCustom": false,
    "missionTypeValue": "Landing",
    "missionTypeIsCustom": false,
    "availableDeltaV": 5200,
    "requiredDeltaV": 4500,
    "controlMode": "Crewed",
    "crewMembers": ["Jebediah Kerman"],
    "probeCoreValue": null,
    "probeCoreIsCustom": false,
    "startMissionTime": null,
    "endMissionTime": null
  }' | jq .

# Create a probe mission
curl -s -X POST http://localhost:5000/api/missions \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Duna Probe",
    "targetBodyValue": "Duna",
    "targetBodyIsCustom": false,
    "missionTypeValue": "Orbital",
    "missionTypeIsCustom": false,
    "availableDeltaV": 2100,
    "requiredDeltaV": 1975,
    "controlMode": "Probe",
    "crewMembers": [],
    "probeCoreValue": "Probodobodyne HECS",
    "probeCoreIsCustom": false,
    "startMissionTime": null,
    "endMissionTime": null
  }' | jq .

# List missions
curl -s http://localhost:5000/api/missions | jq .
```

---

## Kerbin Time Format

Mission times are transmitted as `long` (total Kerbin seconds) and displayed in the format `Yy, Dd, Hh, Mm, Ss`:

| Kerbin Unit | Seconds |
|-------------|---------|
| 1 second | 1 |
| 1 minute | 60 |
| 1 hour | 3,600 |
| 1 day | 21,600 |
| 1 year | 9,201,600 |

Example: `9,244,800` seconds → `1y, 0d, 0h, 0m, 0s` (start of Year 2).

---

## Key Design Notes for Developers

- **Readiness is never stored.** `ReadinessState` and `Warnings` are always re-derived at runtime from the stored delta-v values, control mode, and crew list.
- **Mission time warnings are not readiness warnings.** `InvalidTimeRange` and `AdvisoryEndTimeWithoutStart` are evaluated by the `Mission` aggregate directly — not by `ReadinessCalculator` — because they don't affect `ReadinessState`.
- **Blocking vs advisory warnings.** `Warning.IsBlocking = true` prevents save. `IsBlocking = false` is informational — the mission saves normally. Check `isBlocking` in the frontend before disabling the save button.
- **Conditional field validation.** `CrewMembers` is required only when `ControlMode = Crewed`; `ProbeCore` is required only when `ControlMode = Probe`. This logic lives in the domain `Mission.Create/Update` factory — not in the controller.
- **Storage migration path.** To migrate from JSON to SQL Server: implement `SqlMissionRepository : IMissionRepository` in `MissionControl.Infrastructure` and register it in `Program.cs` in place of `JsonMissionRepository`. No domain or API files change.
- **NUnit test conventions.** Use `[TestFixture]` / `[Test]` / `[TestCase]`. Tests in `MissionControl.Tests` must run with no file system, database, or network dependencies (Principle VI).

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 8.0+ | Backend API |
| Node.js | 20 LTS+ | Frontend build |
| Angular CLI | Latest stable | Frontend scaffolding and dev server |

Verify with:
```bash
dotnet --version
node --version
ng version
```

---

## Repository Structure

```
mission-control/
├── backend/          # .NET solution
│   ├── MissionControl.Domain/
│   ├── MissionControl.Infrastructure/
│   ├── MissionControl.Api/
│   └── MissionControl.Tests/
├── frontend/         # Angular SPA
└── specs/            # Design documents (this folder)
```

---

## Running the Backend

```bash
cd backend/MissionControl.Api
dotnet run
```

The API starts on `http://localhost:5000` by default.

**Configuration** (`backend/MissionControl.Api/appsettings.Development.json`):
```json
{
  "JsonStorage": {
    "FilePath": "data/missions.json"
  }
}
```

The `data/` directory and `missions.json` file are created automatically on first run if they do not exist.

**Run backend tests**:
```bash
cd backend/MissionControl.Tests
dotnet test
```

---

## Running the Frontend

```bash
cd frontend
npm install
ng serve
```

The SPA starts on `http://localhost:4200` and proxies API calls to `http://localhost:5000/api`.

**Run frontend tests**:
```bash
cd frontend
ng test
```

---

## Verify the Setup

1. Open `http://localhost:4200` in a browser.
2. The mission list page loads with an empty-state message ("No missions created yet").
3. Click **New Mission** and create a test mission:
   - Name: `Mun Test`
   - Target Body: `Mun`
   - Mission Type: `Landing`
   - Available ΔV: `5200`
   - Required ΔV: `4500`
4. Save — the mission appears in the list with readiness state **Ready** and no warnings.
5. Edit the mission: set Available ΔV to `4000`. Save — readiness changes to **Not Ready** with two red warning boxes.
6. Delete the mission — the list returns to the empty state.

---

## Quick API Smoke Test (curl)

```bash
# Create a mission
curl -s -X POST http://localhost:5000/api/missions \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Mun Test",
    "targetBodyValue": "Mun",
    "targetBodyIsCustom": false,
    "missionTypeValue": "Landing",
    "missionTypeIsCustom": false,
    "availableDeltaV": 5200,
    "requiredDeltaV": 4500
  }' | jq .

# List missions
curl -s http://localhost:5000/api/missions | jq .

# Get reference data
curl -s http://localhost:5000/api/missions/reference-data | jq .
```

---

## Key Design Notes for Developers

- **Readiness is never stored.** `ReadinessState` and `Warnings` are always re-derived at runtime from delta-v values by `ReadinessCalculator`. The JSON file stores only input fields.
- **Business logic lives only in the domain layer.** Do not add readiness conditions to controllers, Angular services, or the repository.
- **Storage migration path.** To migrate from JSON to SQL Server: implement `SqlMissionRepository : IMissionRepository` in `MissionControl.Infrastructure`, register it in `Program.cs` instead of `JsonMissionRepository`. No other files change.
- **Warnings are independent.** `InsufficientDeltaV` and `LowReserveMargin` fire independently — a `NotReady` mission can have both. Do not short-circuit the margin check when the delta-v is already insufficient.
