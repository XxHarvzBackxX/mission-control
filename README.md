# Mission Control

A full-stack web application for planning and evaluating [Kerbal Space Program](https://www.kerbalspaceprogram.com/) missions.

Users create missions with a name, target body, mission type, delta-v budget, crew (or probe core), and optional Kerbin mission times. The system evaluates each mission as **Ready**, **At Risk**, or **Not Ready** based on delta-v margins and crew assignment rules, and surfaces typed warnings as red indicator boxes.

---

## About This Repository

This project is an experiment in **Spec-Driven Development (SDD)** using [Spec Kit](https://github.com/microsoft/spec-kit) — a VS Code–native workflow that takes a plain-language feature description all the way through to working code via a structured sequence of AI-assisted stages:

```
specify -> clarify -> plan -> tasks -> implement
```

The goal was to test how far a real, non-trivial full-stack feature could be driven by this process with minimal manual intervention — and to surface where the workflow adds the most (and least) value.

### The Workflow

Each stage in the cycle is a discrete Spec Kit command, run inside VS Code Copilot Chat:

| Stage | Command | Output |
|---|---|---|
| Specify | `speckit.specify` | `spec.md` — feature spec from a plain-language description |
| Clarify | `speckit.clarify` | Targeted Q&A encoded back into `spec.md` |
| Plan | `speckit.plan` | `plan.md`, `data-model.md`, `contracts/api.md`, `quickstart.md` |
| Tasks | `speckit.tasks` | `tasks.md` — dependency-ordered implementation task list |
| Implement | `speckit.implement` | Working code, committed to a feature branch |

All design artefacts live in `specs/001-mission-readiness-planner/`. The implementation was committed incrementally as Spec Kit progressed through the task list.

A project constitution (`.specify/memory/constitution.md`) was established upfront to encode architectural principles — things like strict frontend/backend separation, domain-driven backend design, and business logic isolation — which Spec Kit references as a gate before planning begins.

### What Was Built

- **Backend**: ASP.NET Core Web API (.NET 8, C# 12) with a domain layer modelling `Mission`, `ReadinessCalculator`, `KerbinTime`, and typed `Warning` value objects. Persistence is abstracted behind `IMissionRepository` and backed by a local JSON file for the PoC. NUnit test suite covering domain logic and API controllers.
- **Frontend**: Angular SPA (TypeScript) with a mission list, mission form (hybrid dropdowns with "Other" free-text for modded KSP support), mission summary, and warning badge components. Proxies to the .NET API.

### Observations

The Spec Kit workflow enforced a natural cadence: ambiguities that would typically surface mid-implementation (e.g. whether mission names should be unique, whether warnings fire independently or conditionally) were resolved in the clarification stage and encoded into the spec before any code was written. The constitution gate caught architecture drift before it could enter the plan.

The artefacts in `specs/` are not throwaway scaffolding — they are the living design record for the feature, referenced by plan gates and available for future contributors.

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | Angular (TypeScript), Karma + Jasmine |
| Backend | ASP.NET Core Web API, C# 12, .NET 8 |
| Domain testing | NUnit 4 + NSubstitute |
| Persistence | JSON file store (PoC) — `IMissionRepository` abstracted for SQL Server migration |

---

## Getting Started

### Prerequisites

- .NET SDK 8.0+
- Node.js 20 LTS+
- Angular CLI (latest stable)

### Run the backend

```bash
cd backend/MissionControl.Api
dotnet run
```

API starts on `http://localhost:5000`. The `data/missions.json` file is created automatically on first run.

### Run the frontend

```bash
cd frontend
npm install
ng serve
```

SPA starts on `http://localhost:4200` and proxies API calls to `http://localhost:5000/api`.

### Run tests

```bash
# Backend (NUnit)
cd backend/MissionControl.Tests
dotnet test

# Frontend (Karma + Jasmine)
cd frontend
ng test
```

---

## Design Artefacts

All Spec Kit artefacts for the feature are in `specs/001-mission-readiness-planner/`:

| File | Description |
|---|---|
| `spec.md` | Feature specification with user stories and acceptance scenarios |
| `plan.md` | Implementation plan with architecture decisions and constitution check |
| `data-model.md` | Full domain and DTO data model |
| `contracts/api.md` | REST API contract |
| `tasks.md` | Dependency-ordered implementation task list |
| `quickstart.md` | Dev environment setup and verification guide |
| `research.md` | Technology and design research notes |
