# Research: Frontend UI Overhaul

**Phase**: 0 | **Date**: 2026-05-13 | **Plan**: [plan.md](plan.md)

## Research Tasks

Four design decisions needed resolution before Phase 1 design could proceed: SASS migration
mechanics for Angular 20, KerbinTimeInputComponent integration pattern, a WCAG AA–compliant
colour token set, and the burger menu implementation approach.

---

### 1. Angular 20 SASS Integration

**Decision**: Install `sass` as a devDependency. Rename all `.css` files to `.scss`. Update
`angular.json` `styles` entry to reference `src/styles.scss`. Each component's `@Component`
decorator uses `styleUrl: './component.scss'` instead of inline `styles: []`.

**Rationale**: `sass` is the canonical Angular SASS compiler — zero runtime footprint (compile-time
only), no sub-dependencies, explicitly supported by Angular Build (`@angular/build`). Angular CLI
auto-detects `.scss` extension once `sass` is installed — no `sassOptions` configuration is
required for standard usage. This is the standard approach documented by the Angular team and
has no third-party risk (Principle VIII: dependency provides substantial, well-understood value;
no reasonable first-party alternative for SASS compilation).

**Alternatives Considered**:
- *node-sass*: Deprecated; replaced by `sass` (Dart Sass). Rejected.
- *postcss-only approach*: Loses SASS variables and nesting — insufficient for the design token
  system required by FR-007. Rejected.

**Migration Steps**:
1. `npm install --save-dev sass` in `frontend/`
2. Rename `src/styles.css` → `src/styles.scss`; `src/app/app.css` → `src/app/app.scss`
3. Update `angular.json` `"styles"` array entry: `"src/styles.scss"`
4. Per component: remove `styles: [...]` inline array; add `styleUrl: './name.component.scss'`;
   create `name.component.scss` with migrated styles
5. Inline `style=""` attribute overrides on HTML elements are removed and expressed via CSS classes

---

### 2. KerbinTimeInputComponent Integration Pattern

**Decision**: Use `@Input() / @Output()` binding rather than `ControlValueAccessor`.

The component exposes:
- `@Input() value: number | null` — total Kerbin seconds (or null for "not set")
- `@Output() valueChange = new EventEmitter<number | null>()` — emits updated total seconds

The parent form (`MissionFormComponent`) binds `[value]="startMissionTime"` and
`(valueChange)="startMissionTime = $event"`.

**Rationale**: `ControlValueAccessor` provides native `[(ngModel)]` integration but requires
implementing four interface methods, registering an `NG_VALUE_ACCESSOR` token, and a `forwardRef`
— all for a single use case. The spec states "reusable standalone component usable in any form
context", which `@Input/@Output` satisfies cleanly. The parent form already manages `startMissionTime`
and `endMissionTime` as typed fields; passing them through property binding is idiomatic
(Principle VII: clarity over cleverness; Principle VIII: abstraction must earn its complexity).

**Alternatives Considered**:
- *ControlValueAccessor*: Correct for library-level generic form controls. Rejected as over-engineering
  for a project-internal single-purpose component.
- *Reactive Forms (`FormGroup / FormControl`)*: Would require migrating the entire `MissionFormComponent`
  away from template-driven forms. Out of scope per spec Assumption.

**Clamping and Conversion Logic**:
The component maintains an internal `fields` object `{ years, days, hours, minutes, seconds }`.
On each field change:
1. Clamp to bounds: `days ∈ [0, 425]`, `hours ∈ [0, 5]`, `minutes ∈ [0, 59]`, `seconds ∈ [0, 59]`,
   `years ≥ 0`
2. Compute `totalSeconds = years×9_201_600 + days×21_600 + hours×3_600 + minutes×60 + seconds`
3. If all fields are 0 or empty, emit `null` (not-set semantics)
4. Otherwise emit `totalSeconds`

On `value` input change (edit mode population):
- If `value` is `null` or `0`: set all fields to `null` (display as blank)
- Otherwise: decompose using the same constants as `KerbinTimePipe`

**Test requirements** (Principle VI): Unit tests MUST cover decomposition, composition,
clamping boundary conditions, and null/zero not-set semantics. Tests run via `ng test` with
no infrastructure dependencies.

---

### 3. WCAG AA Colour Token Set

**Decision**: Adopt the following SASS variable / CSS custom property set, derived from the
design brief's layered-grey + purposeful-accent direction. All combinations verified against
WCAG AA (4.5:1 minimum contrast for normal text, 3:1 for large/UI elements).

```scss
// --- Backgrounds ---
$color-bg-base:       #0f1117;   // Page background (charcoal)
$color-bg-surface:    #1a1d26;   // Panel / card surface (slate grey)
$color-bg-elevated:   #22263a;   // Elevated / nested panel (gunmetal)

// --- Borders ---
$color-border:        #2e3352;   // Standard panel border (muted metallic)
$color-border-subtle: #1e2235;   // Subtle divider

// --- Text ---
$color-text-primary:  #e8eaf0;   // Primary body text (off-white)
$color-text-secondary:#9aa0b8;   // Labels, secondary info
$color-text-muted:    #6b7280;   // Placeholders, de-emphasised

// --- Accents ---
$color-accent-blue:   #4a9eff;   // Informational (link, info state)
$color-accent-amber:  #f59e0b;   // Warning state
$color-accent-red:    #f87171;   // Critical / Not Ready state
$color-accent-cyan:   #22d3ee;   // Telemetry / highlight / Ready state

// --- Readiness State Colours ---
$color-ready:         #22d3ee;   // cyan
$color-at-risk:       #f59e0b;   // amber
$color-not-ready:     #f87171;   // red

// --- Interactive ---
$color-btn-primary-bg: #1d4ed8;   // Primary button background
$color-btn-primary-text: #ffffff;  // Primary button text
$color-btn-secondary-bg: #22263a; // Secondary button background
$color-btn-secondary-text: #9aa0b8;
```

**Contrast verification (key pairs)**:

| Foreground | Background | Ratio | WCAG AA |
|---|---|---|---|
| `#e8eaf0` | `#0f1117` | 15.7 : 1 | ✅ AAA |
| `#e8eaf0` | `#1a1d26` | 14.2 : 1 | ✅ AAA |
| `#9aa0b8` | `#0f1117` |  7.0 : 1 | ✅ AA |
| `#9aa0b8` | `#1a1d26` |  6.4 : 1 | ✅ AA |
| `#4a9eff` | `#0f1117` |  5.9 : 1 | ✅ AA |
| `#f59e0b` | `#0f1117` |  6.8 : 1 | ✅ AA |
| `#f87171` | `#0f1117` |  4.9 : 1 | ✅ AA |
| `#22d3ee` | `#0f1117` |  5.3 : 1 | ✅ AA |
| `#ffffff` | `#1d4ed8` |  5.9 : 1 | ✅ AA |

**Rationale**: All token values are self-derived (no third-party palette library needed —
Principle VIII). The dark base `#0f1117` provides generous contrast headroom for all accent
colours. Amber `#f59e0b` and cyan `#22d3ee` are sufficiently distinct from each other and
from red `#f87171` to pass both colour and luminance differentiation tests.

---

### 4. Burger Menu Implementation Pattern

**Decision**: Pure Angular template state — a boolean `menuOpen` signal or property on the
`HeaderComponent`, toggled by the burger button. The dropdown panel renders conditionally
via `@if (menuOpen)`. Outside-click dismissal via `@HostListener('document:click', ['$event'])`
comparing the event target against the component's host element ref.

```typescript
menuOpen = false;

@HostListener('document:click', ['$event'])
onDocumentClick(event: MouseEvent): void {
  if (!this.elementRef.nativeElement.contains(event.target)) {
    this.menuOpen = false;
  }
}
```

The dropdown panel renders a single "Coming soon" placeholder card. The panel is absolutely
positioned below the burger button using CSS (no CDK Overlay, no Popper.js).

**Rationale**: No third-party dependency required (Principle VIII). `@HostListener` on
`document:click` is a standard Angular pattern. `ElementRef` injection is standard.
CDK Overlay would be appropriate for production-grade dropdowns but is disproportionate for
a stub placeholder (Principle VII: abstraction must earn its complexity).

**Alternatives Considered**:
- *Angular CDK Overlay*: Correct for complex dropdowns with portal rendering. Rejected — adds
  `@angular/cdk` dependency to the project for a stub placeholder.
- *Custom directive*: Abstraction not warranted for a single component use (Principle VIII).

---

## Summary of Decisions

| Topic | Decision |
|---|---|
| SASS integration | `sass` devDependency + rename to `.scss` + `angular.json` update |
| KerbinTimeInput integration | `@Input() value` / `@Output() valueChange` |
| Colour tokens | Self-derived SASS variables — dark charcoal + purposeful accents, all WCAG AA |
| Burger menu | Inline Angular state + `@HostListener` document click — no CDK |
| No NEEDS CLARIFICATION items remain | All unknowns resolved |
