# Quickstart: Mission Readiness Planner

**Phase**: 1 | **Date**: 2026-05-12 | **Plan**: [plan.md](plan.md)

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
