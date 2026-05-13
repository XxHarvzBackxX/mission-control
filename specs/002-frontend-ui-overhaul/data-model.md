# Data Model: Frontend UI Overhaul

**Phase**: 1 | **Date**: 2026-05-13 | **Plan**: [plan.md](plan.md)

## Overview

This feature introduces no new backend entities and makes no API contract changes.
All changes are confined to the Angular frontend. The data model covers:

1. **Design Token Set** — SASS variables defining the global visual language
2. **KerbinTime Fields** — the decomposed form of a Kerbin time value used by the input component
3. **HeaderComponent State** — the runtime state model for the persistent navigation header

---

## 1. Design Token Set

**Location**: `frontend/src/styles.scss` and an optional `frontend/src/app/_tokens.scss` partial

Tokens are expressed as SASS variables (for compile-time use in component `.scss` files) and as CSS
custom properties (for runtime theming flexibility). The SASS variables are the authoritative source;
CSS custom properties are derived from them on `:root`.

### Colour Tokens

```scss
// Backgrounds
$color-bg-base:          #0f1117;
$color-bg-surface:       #1a1d26;
$color-bg-elevated:      #22263a;

// Borders
$color-border:           #2e3352;
$color-border-subtle:    #1e2235;

// Text
$color-text-primary:     #e8eaf0;
$color-text-secondary:   #9aa0b8;
$color-text-muted:       #6b7280;

// Accents
$color-accent-blue:      #4a9eff;
$color-accent-amber:     #f59e0b;
$color-accent-red:       #f87171;
$color-accent-cyan:      #22d3ee;

// Readiness states
$color-ready:            #22d3ee;   // cyan
$color-at-risk:          #f59e0b;   // amber
$color-not-ready:        #f87171;   // red

// Interactive components
$color-btn-primary-bg:   #1d4ed8;
$color-btn-primary-text: #ffffff;
$color-btn-secondary-bg: #22263a;
$color-btn-secondary-text: #9aa0b8;
$color-btn-danger-bg:    #991b1b;
$color-btn-danger-text:  #ffffff;
```

### Spacing & Sizing Tokens

```scss
// Base unit: 4px grid
$space-1:  4px;
$space-2:  8px;
$space-3:  12px;
$space-4:  16px;
$space-5:  20px;
$space-6:  24px;
$space-8:  32px;
$space-10: 40px;

// Layout
$content-max-width: 900px;
$header-height:     56px;

// Borders
$border-radius-sm:  3px;
$border-radius-md:  6px;
$border-width:      1px;
```

### Typography Tokens

```scss
$font-family-base:    -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
$font-size-base:      14px;
$font-size-sm:        12px;
$font-size-lg:        16px;
$font-size-xl:        20px;
$font-weight-normal:  400;
$font-weight-medium:  500;
$font-weight-bold:    600;
$line-height-base:    1.5;

// Tabular numerics for metric values (via font-variant-numeric)
// Applied via utility class: .tabular-nums { font-variant-numeric: tabular-nums; }
```

### Readiness State Metadata

The readiness state visual representation is a pure CSS concern — no new TypeScript model is
introduced. The existing `ReadinessState` union type (`'Ready' | 'AtRisk' | 'NotReady'`)
is used as CSS class selectors, consistent with the current implementation.

**Left-border width for state indicators**: `4px solid` — applied via `.readiness-strip` modifier
classes:

```scss
.readiness-strip {
  &.Ready    { border-left: 4px solid $color-ready; }
  &.AtRisk   { border-left: 4px solid $color-at-risk; }
  &.NotReady { border-left: 4px solid $color-not-ready; }
}
```

---

## 2. KerbinTime Fields

This is the internal state model for the `KerbinTimeInputComponent`.

### TypeScript Interface

```typescript
// frontend/src/app/components/shared/kerbin-time-input/kerbin-time-input.component.ts

interface KerbinTimeFields {
  years:   number | null;   // unbounded, ≥ 0; null means empty/not-set
  days:    number | null;   // [0, 425]
  hours:   number | null;   // [0, 5]
  minutes: number | null;   // [0, 59]
  seconds: number | null;   // [0, 59]
}
```

### Kerbin Calendar Constants

```typescript
const SECONDS_PER_YEAR   = 9_201_600;  // 426 days × 21,600 s/day
const SECONDS_PER_DAY    =    21_600;  // 6 hours × 3,600 s/hour
const SECONDS_PER_HOUR   =     3_600;
const SECONDS_PER_MINUTE =        60;
```

### Field Bounds

| Field   | Min | Max  | Notes |
|---------|-----|------|-------|
| Years   | 0   | —    | Unbounded; clamped to ≥ 0 |
| Days    | 0   | 425  | 426 days per Kerbin year (indices 0–425) |
| Hours   | 0   | 5    | 6 hours per Kerbin day (indices 0–5) |
| Minutes | 0   | 59   | Standard |
| Seconds | 0   | 59   | Standard |

### Conversion Rules

**Fields → total seconds (composition)**:
```
totalSeconds = years×9_201_600 + days×21_600 + hours×3_600 + minutes×60 + seconds
```
If all fields are `null` or `0`, emit `null` (not-set semantics per Clarification Q1).

**Total seconds → fields (decomposition)**:
```
years   = floor(totalSeconds / 9_201_600)
rem     = totalSeconds % 9_201_600
days    = floor(rem / 21_600)
rem     = rem % 21_600
hours   = floor(rem / 3_600)
rem     = rem % 3_600
minutes = floor(rem / 60)
seconds = rem % 60
```
If `totalSeconds` is `null` or `0`, all fields are set to `null` (blank display).

---

## 3. HeaderComponent State

**Location**: `frontend/src/app/components/header/header.component.ts`

The header is a simple stateful component. No service injection required.

```typescript
// Runtime state (component properties)
menuOpen: boolean = false;

// Injected (via constructor)
elementRef: ElementRef   // for outside-click detection
```

### Menu Toggle Behaviour

| Event | Effect |
|---|---|
| Burger button click | Toggle `menuOpen` |
| Click outside component | Set `menuOpen = false` |
| Route navigation | `menuOpen` resets to `false` (subscribe to Router events or use ngOnDestroy) |

The dropdown panel renders conditionally — present in the DOM only when `menuOpen = true`.

---

## 4. File → Component Mapping

| Component | Template | Stylesheet | New File? |
|---|---|---|---|
| `AppComponent` | `app.html` | `app.scss` | Convert `.css` → `.scss` |
| `HeaderComponent` | `header.component.html` | `header.component.scss` | **New** |
| `MissionListComponent` | existing | `mission-list.component.scss` | **New** (migrate from inline) |
| `MissionFormComponent` | existing (updated) | `mission-form.component.scss` | **New** (migrate from inline) |
| `MissionSummaryComponent` | existing (updated) | `mission-summary.component.scss` | **New** (migrate from inline) |
| `WarningBadgeComponent` | existing | `warning-badge.component.scss` | **New** (migrate from inline) |
| `KerbinTimeInputComponent` | `kerbin-time-input.component.html` | `kerbin-time-input.component.scss` | **New** |
| Global styles | — | `styles.scss` | Convert + expand from `styles.css` |

---

## Entity Relationships

```
AppComponent
└── HeaderComponent          (new — persistent on all routes)
    └── BurgerMenuDropdown   (stub panel — inline in header template)

RouterOutlet
├── MissionListComponent
│   ├── ReadinessStrip       (CSS class — not a component)
│   └── WarningBadgeComponent (existing)
├── MissionFormComponent
│   ├── KerbinTimeInputComponent  (new — used twice: start + end time)
│   └── WarningBadgeComponent (existing, if applicable)
└── MissionSummaryComponent
    ├── ReadinessStrip       (CSS class — not a component)
    └── WarningBadgeComponent (existing)
```

No backend entities are added or modified.
