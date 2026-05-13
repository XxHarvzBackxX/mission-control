# Implementation Plan: Frontend UI Overhaul

**Branch**: `002-frontend-ui-overhaul` | **Date**: 2026-05-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/002-frontend-ui-overhaul/spec.md`

## Summary

Replace the Angular scaffold boilerplate in `app.html` with a minimal application shell
(persistent header + router-outlet). Apply a cohesive KSP-inspired aerospace mission control
visual theme across all three views (mission list, form, summary) using SASS with a centrally
defined design token set. Add a `KerbinTimeInputComponent` that replaces the raw-seconds inputs
for start/end mission time with a clamped Y/D/H/M/S composite picker. No backend changes.

Technical approach (from research):
- `sass` devDependency added; all `.css` files renamed to `.scss`
- Design tokens as SASS variables in `styles.scss` (`_tokens.scss` partial)
- New `HeaderComponent` (logo icon, nav links, burger menu dropdown stub via `@HostListener`)
- New `KerbinTimeInputComponent` (`@Input value / @Output valueChange`, clamped sub-fields)
- Inline `styles:[]` blocks migrated to per-component `.scss` files
- Unit tests for KerbinTimeInputComponent conversion and clamping (Karma + Jasmine)

## Technical Context

**Language/Version**: TypeScript 5.9 / Angular 20.3

**Primary Dependencies**: Angular 20.3 (Common, Forms, Router); `sass` (new devDependency — SASS compiler, compile-time only, no runtime footprint)

**Storage**: N/A — frontend-only; API contract unchanged

**Testing**: Karma + Jasmine (Angular default); `ng test`

**Target Platform**: Web browser (modern evergreen); Angular SSR not in use

**Project Type**: Web application (frontend layer of Angular + .NET system)

**Performance Goals**: No additional performance targets beyond existing app. Angular build budget: initial bundle ≤ 500 kB warning / 1 MB error (existing `angular.json` budget preserved).

**Constraints**: WCAG AA minimum on all text and UI elements; no third-party UI component libraries; no CSS-in-JS; no Angular CDK

**Scale/Scope**: 4 components restyled; 2 new components; 1 new SASS token partial; ~7 `.css` → `.scss` migrations

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Requirement | Assessment |
|---|---|---|
| I. Modular Architecture | Frontend and backend remain independent layers | ✅ PASS — all changes are frontend-only; API contract is unchanged |
| II. Component-Driven Frontend | New UI features as discrete standalone components; no presentation logic in services | ✅ PASS — `HeaderComponent` and `KerbinTimeInputComponent` are standalone; no service changes |
| III. Domain-Driven Backend | Not applicable (no backend changes) | ✅ N/A |
| IV. Business Logic Isolation (**NON-NEGOTIABLE**) | No business logic in presentation layer | ✅ PASS — Y/D/H/M/S conversion is input assistance (presentation math mirroring backend constants), not a domain rule. Readiness calculation untouched. |
| V. Deterministic Readiness (**NON-NEGOTIABLE**) | Readiness calculations must be pure, deterministic, in domain layer | ✅ PASS — not touched |
| VI. Unit Test Coverage (**NON-NEGOTIABLE**) | Core calculation logic must have unit tests | ✅ PASS — `KerbinTimeInputComponent` has non-trivial conversion + clamping logic; unit tests are **required** deliverables |
| VII. Readability First | Clarity over cleverness; abstractions earn their keep | ✅ PASS — `@Input/@Output` chosen over ControlValueAccessor; `@HostListener` over CDK; SASS tokens over CSS-in-JS |
| VIII. Minimal Dependencies | Only `sass` (devDependency) added | ✅ PASS — `sass` is the canonical Angular SASS compiler, compile-time only, no runtime footprint, no sub-dependencies. Justified: enables the full design token system required by FR-006/FR-007. |
| IX. Purposeful Documentation | Comments on non-obvious logic only | ✅ PASS — KerbinTime constants and conversion logic in the new component warrant brief inline comments |

**Gate result: ALL PASS.** No violations. No Complexity Tracking entries required.

**Required action from Principle VI**: `KerbinTimeInputComponent` unit tests (decomposition, composition, clamping bounds, null/zero not-set) are **mandatory** implementation deliverables, not optional.

## Project Structure

### Documentation (this feature)

```text
specs/002-frontend-ui-overhaul/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/
│   └── components.md    # Phase 1 output — KerbinTimeInputComponent + HeaderComponent APIs
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (frontend — all changes confined here)

```text
frontend/
├── package.json                          # add: "sass": "^1.x.x" (devDependency)
└── src/
    ├── styles.scss                       # RENAMED from styles.css; expanded with tokens
    └── app/
        ├── app.ts                        # Updated: imports HeaderComponent
        ├── app.html                      # REPLACED: app shell (header + router-outlet)
        ├── app.scss                      # RENAMED from app.css (empty → no change needed)
        ├── components/
        │   ├── header/                   # NEW component
        │   │   ├── header.component.ts
        │   │   ├── header.component.html
        │   │   └── header.component.scss
        │   ├── shared/
        │   │   ├── warning-badge/
        │   │   │   ├── warning-badge.component.ts      # Updated: styleUrl → .scss
        │   │   │   └── warning-badge.component.scss    # NEW (migrated from inline styles)
        │   │   └── kerbin-time-input/                  # NEW component
        │   │       ├── kerbin-time-input.component.ts
        │   │       ├── kerbin-time-input.component.html
        │   │       ├── kerbin-time-input.component.scss
        │   │       └── kerbin-time-input.component.spec.ts  # REQUIRED unit tests
        │   ├── mission-list/
        │   │   ├── mission-list.component.ts           # Updated: styleUrl → .scss
        │   │   ├── mission-list.component.html         # Updated: readiness strip classes
        │   │   └── mission-list.component.scss         # NEW (migrated + redesigned)
        │   ├── mission-form/
        │   │   ├── mission-form.component.ts           # Updated: styleUrl → .scss; integrate KerbinTimeInput
        │   │   ├── mission-form.component.html         # Updated: replace time inputs
        │   │   └── mission-form.component.scss         # NEW (migrated + redesigned)
        │   └── mission-summary/
        │       ├── mission-summary.component.ts        # Updated: styleUrl → .scss
        │       ├── mission-summary.component.html      # Updated: readiness strip classes
        │       └── mission-summary.component.scss      # NEW (migrated + redesigned)
        └── angular.json                  # Updated: styles entry → styles.scss
```

**Structure Decision**: Option 2 (web application — frontend + backend). All changes are frontend-only, within the existing `frontend/` tree. No new directories at repository root. No backend files touched.
