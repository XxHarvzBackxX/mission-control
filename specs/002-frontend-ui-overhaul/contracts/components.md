# Component Contracts: Frontend UI Overhaul

**Phase**: 1 | **Date**: 2026-05-13 | **Plan**: [plan.md](../plan.md)

This feature introduces two new public Angular component interfaces that other parts of the
application can consume. Backend API contracts are unchanged.

---

## 1. KerbinTimeInputComponent

**Selector**: `app-kerbin-time-input`
**File**: `frontend/src/app/components/shared/kerbin-time-input/kerbin-time-input.component.ts`
**Type**: Standalone Angular component

### Public API

#### Inputs

| Property | Type | Default | Description |
|---|---|---|---|
| `value` | `number \| null` | `null` | Total Kerbin seconds. `null` or `0` → fields render blank (not-set). |
| `label` | `string` | `''` | Optional accessible label prefix shown above the field group. If empty, no label element is rendered. |

#### Outputs

| Event | Type | Description |
|---|---|---|
| `valueChange` | `EventEmitter<number \| null>` | Emits updated total seconds after any sub-field change. Emits `null` when all fields are blank/zero. |

### Usage

```html
<!-- New mission — no pre-populated value -->
<app-kerbin-time-input
  label="Start Mission Time"
  [value]="startMissionTime"
  (valueChange)="startMissionTime = $event"
/>

<!-- Edit mission — pre-populated from API -->
<app-kerbin-time-input
  label="End Mission Time"
  [value]="endMissionTime"
  (valueChange)="endMissionTime = $event"
/>
```

### Behaviour Contract

1. When `value` input changes:
   - If `value` is `null` or `0`: all sub-fields render as blank (empty `<input>` elements)
   - Otherwise: decompose to `{ years, days, hours, minutes, seconds }` using Kerbin constants

2. When a sub-field changes (user input or blur):
   - Clamp to bounds: `days ∈ [0, 425]`, `hours ∈ [0, 5]`, `minutes ∈ [0, 59]`, `seconds ∈ [0, 59]`, `years ≥ 0`
   - If all fields are empty/null/zero: emit `null`
   - Otherwise: compute and emit `totalSeconds`

3. Sub-fields accept only non-negative integers; non-numeric input is rejected

4. The component emits on every valid change — the parent is responsible for debouncing if required

### Visual Contract

The component renders five side-by-side numeric inputs with labels below each:

```
[ Years ] [ Days ] [ Hours ] [ Mins ] [ Secs ]
   Y         D        H        M        S
```

On mobile (≤ 480px): inputs wrap to two rows if needed.

---

## 2. HeaderComponent

**Selector**: `app-header`
**File**: `frontend/src/app/components/header/header.component.ts`
**Type**: Standalone Angular component

### Public API

The `HeaderComponent` has no `@Input()` or `@Output()` bindings. It is a self-contained,
route-aware shell element.

### Navigation Links (hardcoded in this feature)

| Label | Route | Active matcher |
|---|---|---|
| Missions | `/` | `routerLinkActiveOptions: { exact: true }` |

Additional links will be added by future features without changes to the HeaderComponent contract.

### Burger Menu Contract

| State | Trigger | Visual |
|---|---|---|
| Closed (default) | — | Burger icon (☰) |
| Open | Click burger button | Dropdown panel below button; contains "Coming soon" card |
| Closed (dismissed) | Click outside component | Panel removed from DOM |

The burger menu panel is a **stub** in this feature. Its internal structure is not part of the
public contract — future features will replace the placeholder content.

### Usage

```html
<!-- app.html — placed once, outside <router-outlet> -->
<app-header />
<router-outlet />
```

---

## API Contracts (Unchanged)

No API endpoint signatures, request bodies, or response shapes are modified by this feature.

The existing `startMissionTime` and `endMissionTime` fields on `CreateMissionRequest` and
`MissionSummary` remain `number | null` (total Kerbin seconds). The Y/D/H/M/S decomposition
is a frontend-only concern.

Reference: `frontend/src/app/models/mission.model.ts`
