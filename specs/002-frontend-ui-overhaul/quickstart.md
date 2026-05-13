# Quickstart: Frontend UI Overhaul

**Phase**: 1 | **Date**: 2026-05-13 | **Plan**: [plan.md](plan.md)

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 8.0+ | Backend API |
| Node.js | 20 LTS+ | Frontend build |
| Angular CLI | Latest stable | Dev server and build |

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

## First-Time Setup (SASS dependency)

This feature adds `sass` as a devDependency. If it is not yet installed:

```bash
cd frontend
npm install
```

`npm install` installs all dependencies including `sass`. If `sass` is not yet in
`package.json`, install it explicitly:

```bash
npm install --save-dev sass
```

---

## Running the Backend

```bash
cd backend/MissionControl.Api
dotnet run
```

API starts on `http://localhost:5000`. The `data/missions.json` file is created automatically.

**Run backend tests** (NUnit):

```bash
cd backend/MissionControl.Tests
dotnet test
```

---

## Running the Frontend

```bash
cd frontend
ng serve
```

Frontend starts on `http://localhost:4200` and proxies API calls to `http://localhost:5000`
via `proxy.conf.json`.

**Run frontend tests** (Karma + Jasmine):

```bash
cd frontend
ng test
```

---

## Verifying This Feature

### 1. Angular boilerplate removed

Navigate to `http://localhost:4200`.

- ✅ No Angular logo, welcome heading, pill-group links, or gradient divider visible
- ✅ Mission list renders as the default view
- ✅ Persistent header at the top with Mission Control logo icon, app name, navigation links,
  and burger menu button

### 2. Visual design applied

Load the mission list, create/edit form, and mission summary.

- ✅ Consistent dark charcoal background across all routes
- ✅ Off-white text — no pure black or pure white backgrounds
- ✅ Readiness state rows / badges use coloured left borders (cyan / amber / red)
- ✅ State labels are present alongside colours (no colour-only indicators)
- ✅ No inline `style="..."` attributes on rendered elements (verify via browser DevTools)

### 3. Header navigation and burger menu

- ✅ "Missions" navigation link navigates to `/`
- ✅ Clicking the burger menu (☰) opens the dropdown panel with "Coming soon" text
- ✅ Clicking outside the panel closes it

### 4. KerbinTime Y/D/H/M/S picker

Navigate to **New Mission** or open an existing mission for editing.

- ✅ Start Mission Time and End Mission Time show five labelled sub-fields (Y / D / H / M / S)
- ✅ Entering `2` in Years, `15` in Days, `3` in Hours, all others blank → submitting saves correctly
  (verify via mission summary showing `2y, 15d, 3h, 0m, 0s`)
- ✅ Editing an existing mission with a saved start time pre-populates the correct sub-fields
- ✅ Entering `430` in Days clamps to `425` on blur
- ✅ Leaving all Y/D/H/M/S fields blank saves as "not set" (no time displayed in summary)

### 5. Responsive layout

Resize the browser to ≤ 480px width (or use DevTools device emulator).

- ✅ No horizontal scrollbars on mission list, form, or summary views
- ✅ Header remains usable at narrow widths
- ✅ KerbinTime sub-fields wrap cleanly if needed

---

## WCAG Accessibility Check

Quick browser check using Lighthouse or axe DevTools:

1. Open `http://localhost:4200` in Chrome
2. DevTools → Lighthouse → Accessibility audit
3. Target: **score ≥ 90**, zero contrast failures

All design token foreground/background combinations verified to ≥ 4.5:1 ratio — see
[research.md](research.md) for full contrast table.
