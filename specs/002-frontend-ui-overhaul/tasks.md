# Tasks: Frontend UI Overhaul

**Input**: Design documents from `specs/002-frontend-ui-overhaul/`

**Prerequisites**: [plan.md](plan.md) · [spec.md](spec.md) · [research.md](research.md) · [data-model.md](data-model.md) · [contracts/components.md](contracts/components.md)

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no incomplete dependencies)
- **[Story]**: User story this task belongs to (`[US1]`, `[US2]`, `[US3]`)
- File paths shown relative to repository root

---

## Phase 1: Setup

**Purpose**: SASS toolchain wired up — prerequisite for all `.scss` work in every story

- [X] T001 Install `sass` devDependency — run `npm install --save-dev sass` in `frontend/`; confirm entry appears in `frontend/package.json`
- [X] T002 Rename `frontend/src/styles.css` → `frontend/src/styles.scss`; rename `frontend/src/app/app.css` → `frontend/src/app/app.scss`; update `frontend/angular.json` styles array entry to `"src/styles.scss"`; update `styleUrl` in `frontend/src/app/app.ts` to `'./app.scss'`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Design tokens and app shell skeleton — MUST be complete before US1 header styling and ALL of US2 component styling

**⚠️ CRITICAL**: Component `.scss` files in US2 and the header `.scss` in US1 depend on the tokens partial from T003. T004 and T003 can run in parallel with each other.

- [X] T003 [P] Create `frontend/src/app/_tokens.scss` with complete SASS variable set
- [X] T004 [P] Replace all Angular scaffold content in `frontend/src/app/app.html` with a minimal app shell

**Checkpoint**: Tokens partial exists; app.html is clean — US1 and US2 can now proceed

---

## Phase 3: User Story 1 — Remove Angular Boilerplate and Apply App Shell (Priority: P1) 🎯 MVP

**Goal**: Angular scaffold content gone; persistent header with logo, nav links, and burger menu dropdown visible on every route

**Independent Test**: Navigate to `/` — no Angular logo/welcome/pills/divider present; mission list renders; header shows Mission Control logo, app name, Missions link, and burger button; clicking burger opens "Coming soon" dropdown; clicking outside closes it

- [X] T005 [US1] Create `HeaderComponent` file scaffold
- [X] T006 [US1] Implement `HeaderComponent` HTML template
- [X] T007 [US1] Implement `HeaderComponent` TypeScript class
- [X] T008 [US1] Style `HeaderComponent`
- [X] T009 [US1] Update `AppComponent`

**Checkpoint**: US1 fully functional — visit any route and confirm clean app shell with working header

---

## Phase 4: User Story 2 — Visual Design Applied Consistently (Priority: P2)

**Goal**: Cohesive aerospace dark theme across mission list, form, and summary — no inline styles remaining, readiness strip left-borders applied

**Independent Test**: Load all three views; inspect rendered HTML — no `style=""` attributes on any element; dark charcoal backgrounds throughout; readiness rows show coloured left borders (cyan/amber/red) plus text labels

**⚠️ Depends on T003 (tokens partial). All five tasks below are parallel — they touch different component files.**

- [X] T010 [P] [US2] Expand `frontend/src/styles.scss`
- [X] T011 [P] [US2] Style `MissionListComponent` — `@use '../../_tokens' as t;`; migrate all styles from inline `styles: []` block in `mission-list.component.ts`; apply dark theme to container, table, `th`/`td`; define `.readiness-strip` with `&.Ready { border-left: 4px solid $color-ready }`, `&.AtRisk { border-left: 4px solid $color-at-risk }`, `&.NotReady { border-left: 4px solid $color-not-ready }` left-border classes; update `mission-list.component.html` — replace badge `[ngClass]` with `readiness-strip` row class; ensure text label is always present alongside colour; update `mission-list.component.ts` `styleUrl` to `'./mission-list.component.scss'`; remove inline `styles:[]`
- [X] T012 [P] [US2] Style `MissionFormComponent` — `@use '../../_tokens' as t;`; migrate all styles from inline `styles: []` block; apply dark theme to form container, `label`, `input`, `select`, `.btn-*` classes, `.crew-item`, `.mode-toggle`, `.error-msg`; remove all `style=""` inline attribute overrides from `mission-form.component.html` (e.g., `style="margin-top: 4px"`); update `mission-form.component.ts` `styleUrl` to `'./mission-form.component.scss'`; remove inline `styles:[]`
- [X] T013 [P] [US2] Style `MissionSummaryComponent` — `@use '../../_tokens' as t;`; migrate all styles from inline `styles: []` block; apply dark theme to summary container, `.detail-row`, `.detail-label`, `.detail-value`, `.readiness-badge`, `.actions`, `.not-found`; update `.readiness-badge` classes to use `$color-ready`/`$color-at-risk`/`$color-not-ready` with coloured left-border strip pattern and text label; remove any `style=""` inline attribute overrides from `mission-summary.component.html`; update `mission-summary.component.ts` `styleUrl` to `'./mission-summary.component.scss'`; remove inline `styles:[]`
- [X] T014 [P] [US2] Style `WarningBadgeComponent` — `@use '../../../_tokens' as t;`; migrate all styles from inline `styles: []` block in `warning-badge.component.ts`; apply dark-theme warning colour from `$color-accent-amber` / `$color-accent-red` based on `isBlocking`; update `warning-badge.component.ts` `styleUrl` to `'./warning-badge.component.scss'`; remove inline `styles:[]`

**Checkpoint**: All three views fully themed — no inline styles remain; readiness states show coloured left borders with text labels; WCAG AA contrast confirmed

---

## Phase 5: User Story 3 — KerbinTime Year/Day/Hour/Minute/Second Picker (Priority: P3)

**Goal**: Start and End Mission Time fields replaced with five clamped sub-field inputs (Y/D/H/M/S); decomposition from saved seconds on edit load; unit tests passing

**Independent Test**: Open New Mission form — see Y/D/H/M/S sub-fields; enter values; save; confirm summary shows correct decomposed time string; edit same mission — sub-fields pre-populated correctly

**Tests required (Constitution Principle VI — NON-NEGOTIABLE)**

- [X] T015 [US3] Create `KerbinTimeInputComponent` scaffold `frontend/src/app/components/shared/kerbin-time-input/` and create four empty files: `kerbin-time-input.component.ts`, `kerbin-time-input.component.html`, `kerbin-time-input.component.scss`, `kerbin-time-input.component.spec.ts`
- [X] T016 [US3] Implement `KerbinTimeInputComponent` TypeScript class in `frontend/src/app/components/shared/kerbin-time-input/kerbin-time-input.component.ts` — `standalone: true`; `@Input() value: number | null`; `@Output() valueChange = new EventEmitter<number | null>()`; internal `fields: KerbinTimeFields` object (`{ years, days, hours, minutes, seconds }` all `number | null`); `ngOnChanges` to decompose `value` input into fields (null/0 → all null); `onFieldChange()` method that clamps each field to bounds (days 0–425, hours 0–5, minutes 0–59, seconds 0–59, years ≥ 0), computes `totalSeconds`, emits null if all fields are null/zero, otherwise emits computed total; Kerbin constants as private `readonly` class members
- [X] T017 [US3] Implement `KerbinTimeInputComponent` HTML template in `frontend/src/app/components/shared/kerbin-time-input/kerbin-time-input.component.html` — five `<div class="time-field">` containers each with `<input type="number" min="0">` bound to the matching `fields.x` value via `[(ngModel)]` with `(change)="onFieldChange()"`, and `<label>` element below showing the unit abbreviation (Y / D / H / M / S); import `FormsModule` in component imports
- [X] T018 [P] [US3] Style `KerbinTimeInputComponent` in `frontend/src/app/components/shared/kerbin-time-input/kerbin-time-input.component.scss` — `@use '../../../_tokens' as t;`; `.time-fields` flex row with `gap: $space-2`, `align-items: flex-end`; each `.time-field` sized to ~60px width; `input` dark-themed matching form inputs in T012; unit `label` below input in `$color-text-muted`, `$font-size-sm`; `@media (max-width: 480px)` wrap rule
- [X] T019 [US3] Write unit tests for `KerbinTimeInputComponent` `frontend/src/app/components/shared/kerbin-time-input/kerbin-time-input.component.spec.ts` — test cases MUST cover: (1) `value = 9_201_600` decomposes to `{ years:1, days:0, hours:0, minutes:0, seconds:0 }`; (2) `value = null` sets all fields to null; (3) `value = 0` sets all fields to null (not-set semantics); (4) `fields = { years:2, days:15, hours:3, minutes:0, seconds:0 }` composes to `2×9_201_600 + 15×21_600 + 3×3_600 = 18,727,800`; (5) days input of 430 clamps to 425 on `onFieldChange()`; (6) hours input of 6 clamps to 5; (7) all-null fields emit null; (8) value spanning all five units round-trips correctly
- [X] T020 [US3] Update `MissionFormComponent` with KerbinTimeInput in `frontend/src/app/components/mission-form/mission-form.component.ts` and `.html` — add `KerbinTimeInputComponent` to component imports; replace `<input id="startTime" type="number" ...>` with `<app-kerbin-time-input label="Start Mission Time" [value]="startMissionTime" (valueChange)="startMissionTime = $event" />`; replace `<input id="endTime" type="number" ...>` with the equivalent for `endMissionTime`; remove the `label` elements for the old inputs (label is now provided by the component's `label` input)

**Checkpoint**: KerbinTime picker works end-to-end; `ng test` passes all unit tests including T019 specs

---

## Final Phase: Polish & Cross-Cutting Concerns

**Purpose**: Responsive layout verification; full test suite green

- [X] T021 Responsive layout review — open each of the three main views at ≤480px viewport width (browser DevTools device emulator); confirm no horizontal scrollbars; fix any overflow in `frontend/src/styles.scss`, `mission-list.component.scss`, `mission-form.component.scss`, `mission-summary.component.scss`, or `header.component.scss`
- [X] T022 Run `ng test` — all tests pass in `frontend/` — confirm all unit tests pass; verify `KerbinTimeInputComponent` spec (T019) has 8+ passing tests; fix any failures before marking complete

---

## Dependencies

```
T001 → T002 → [T003, T004] (parallel)
                 T003 → T008 (header styles)
                 T003 → [T010, T011, T012, T013, T014] (parallel — all US2)
                 T004 → T009 (AppComponent wiring confirms app.html shell)
              T005 → T006 → T007 → T008 → T009  (US1 — mostly sequential)
              T015 → T016 → T017                 (US3 logic → template)
              T015 → T018                        (US3 styles — parallel to T016)
              T016 → T019                        (US3 unit tests)
              T016 → T020                        (US3 form integration)
T009, T014 → T021 → T022                        (Polish — after all stories)
```

## Parallel Execution Examples

**Phase 2** (after T002):
```
T003 ──────────────────── (tokens)
T004 ──────────────────── (app.html shell)
```

**Phase 4 US2** (after T003):
```
T010 ──── (styles.scss global)
T011 ──── (mission-list)
T012 ──── (mission-form)
T013 ──── (mission-summary)
T014 ──── (warning-badge)
```

**Phase 5 US3** (after T015):
```
T016 ──────────────── (component logic)
T018 ──────────────── (component styles — parallel to T016)
          T017 ─────── (template — after T016)
          T019 ─────── (unit tests — after T016)
          T020 ─────── (form integration — after T016)
```

## Implementation Strategy

**Deliver in story order — each story is an independently testable increment:**

1. **MVP**: US1 + US2 — clean shell and full visual theme (no KerbinTime picker, but app is fully usable and visually complete)
2. **Full**: + US3 — KerbinTime picker replaces raw-seconds inputs

**Suggested agent execution order**: T001 → T002 → T003+T004 → T005 → T006 → T007 → T008 → T009 → T010+T011+T012+T013+T014 → T015 → T016+T018 → T017 → T019 → T020 → T021 → T022

## Summary

| Phase | Tasks | User Story | Parallelizable |
|---|---|---|---|
| Phase 1: Setup | T001–T002 | — | Sequential |
| Phase 2: Foundational | T003–T004 | — | T003 ‖ T004 |
| Phase 3: App Shell | T005–T009 | US1 (P1) | Mostly sequential |
| Phase 4: Visual Design | T010–T014 | US2 (P2) | All 5 parallel |
| Phase 5: KerbinTime Picker | T015–T020 | US3 (P3) | T016 ‖ T018 |
| Final: Polish | T021–T022 | — | Sequential |

**Total tasks**: 22
**Tasks per user story**: US1 = 5, US2 = 5, US3 = 6
**Parallel opportunities**: 3 batches (Phase 2, Phase 4, Phase 5 partial)
**Mandatory test tasks**: T019 (KerbinTimeInputComponent — Constitution Principle VI)
**MVP scope**: T001–T014 (US1 + US2 complete)
