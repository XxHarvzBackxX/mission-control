# Feature Specification: Frontend UI Overhaul

**Feature Branch**: `002-frontend-ui-overhaul`

**Created**: 2026-05-13

**Status**: Draft

**Input**: User description: "remove default angular stuff in the Frontend, improve styling to a specified design plan, upgrade KerbinTime input such that it has some kind of Y D S picker or something (not seconds)"

## Clarifications

### Session 2026-05-13

- Q: Should `startMissionTime` / `endMissionTime` of `0` be treated as "not set" or as game epoch (Year 0)? → A: Both `null` and `0` mean not set — a UT=0 mission start is not a realistic career scenario. Y/D/H/M/S fields show blank when the value is `null` or `0`.
- Q: Should the Y/D/H/M/S picker normalise over-boundary sub-field values (e.g. 430 days) or leave them as entered? → A: Clamp to natural Kerbin calendar bounds — Days `[0–425]`, Hours `[0–5]`, Minutes `[0–59]`, Seconds `[0–59]`, Years unbounded (≥ 0).
- Q: Which font stack should the typography use? → A: System font stack (`-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`). Additionally, the frontend MUST use SASS (`.scss`) instead of plain CSS for all stylesheets.
- Q: What form should the burger menu open state take? → A: Dropdown panel below the button containing a "Coming soon" placeholder text card.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Remove Angular Boilerplate and Apply App Shell (Priority: P1)

A developer visiting the application no longer sees Angular's default welcome page. The app launches directly into the mission planner interface with a proper application shell (navigation header and content area), styled consistently across all pages.

**Why this priority**: The Angular scaffold content actively breaks the user experience — it replaces the real application UI on the root route. Removing it and establishing a clean app shell is the prerequisite for all visual design work. Without this, the app looks unfinished and the routing structure is unclear.

**Independent Test**: Can be fully tested by navigating to the app root (`/`) and confirming the Angular logo, welcome text, pill links, and gradient dividers are absent; the mission list renders as the default view; and a header/navigation bar is visible at the top of the page containing the Mission Control logo icon, the app name, navigation links, and a burger menu button.

**Acceptance Scenarios**:

1. **Given** a user opens the application, **When** the page loads, **Then** no Angular default content (logo, welcome heading, pill-group links, divider) is visible anywhere on the page.
2. **Given** a user opens the application, **When** the page loads, **Then** the mission list view renders as the default route.
3. **Given** any page in the application, **When** it is displayed, **Then** a persistent header bar is visible at the top containing: the Mission Control logo icon, the text "Mission Control", navigation links (e.g. Missions), and a burger menu button on the right.
4. **Given** a user navigates between routes (mission list, mission form, mission summary), **When** navigation occurs, **Then** the header remains visible and does not flash or reset.
5. **Given** a user clicks the burger menu button, **When** it is activated, **Then** a dropdown panel appears directly below the button containing a "Coming soon" placeholder text card; clicking the button again closes the panel.

---

### User Story 2 - Visual Design Applied Consistently (Priority: P2)

A user experiences a cohesive, KSP-inspired visual theme across the entire application — mission list, mission form, and mission summary — rather than the current mix of plain browser defaults and inline styles scattered across components.

**Why this priority**: The current UI has no unified design. Styles are defined inline per component with no shared language. A cohesive design makes the tool feel purposeful and reduces visual friction during planning sessions.

**Independent Test**: Can be fully tested by loading all three main views (mission list, create/edit form, mission summary) and confirming consistent typography, colour palette, spacing, and component styles (buttons, inputs, tables, readiness badges) are applied without any component-specific overrides contradicting the global theme.

**Acceptance Scenarios**:

1. **Given** any page in the application, **When** it is rendered, **Then** the colour palette, typography, spacing, and component styles are consistent with the aerospace mission control design direction defined in the Design Direction section of this spec.
2. **Given** the mission list, **When** displayed, **Then** readiness state badges (Ready / At Risk / Not Ready) use visually distinct colours consistent with the design (e.g., green/amber/red or equivalent semantic colours).
3. **Given** the mission form, **When** displayed, **Then** all inputs, labels, dropdowns, and buttons follow the same visual style — no inline `style=` overrides remain on elements.
4. **Given** a small screen (mobile viewport, ≤ 480px width), **When** any page is displayed, **Then** the layout is readable and usable without horizontal scrolling.

---

### User Story 3 - KerbinTime Year/Day/Hour/Minute/Second Picker (Priority: P3)

A mission planner entering a Start or End Mission Time no longer types a raw number of seconds. Instead, they use a structured Y / D / H / M / S input control that decomposes Kerbin time into its natural calendar units, making time entry intuitive and readable.

**Why this priority**: Entering `9201600` to mean "Year 1, Day 1" is opaque and error-prone. The picker makes time values human-readable in the KSP calendar and reduces data-entry mistakes. It is lower priority than the visual baseline (P1/P2) because the form is functional without it.

**Independent Test**: Can be fully tested by navigating to the mission creation form, entering values into each sub-field (years, days, hours, minutes, seconds), saving the mission, and confirming the saved mission summary displays the time in the expected decomposed format (e.g., "1y, 0d, 0h, 0m, 0s").

**Acceptance Scenarios**:

1. **Given** a user opens the mission form, **When** the Start Mission Time field is displayed, **Then** it shows five labelled numeric sub-fields: Years, Days, Hours, Minutes, Seconds — not a single seconds input.
2. **Given** a user opens the mission form, **When** the End Mission Time field is displayed, **Then** it also shows the same five-field Y/D/H/M/S structure.
3. **Given** a user enters values into the Y/D/H/M/S fields, **When** the form is submitted, **Then** the values are combined into a total Kerbin seconds value and sent to the API as the existing `startMissionTime` / `endMissionTime` fields (no API contract change required).
4. **Given** a user is editing an existing mission that has a saved start or end time, **When** the form loads, **Then** the saved seconds value is decomposed and pre-populated into the correct Y/D/H/M/S sub-fields.
5. **Given** a user leaves all Y/D/H/M/S sub-fields blank or at zero, **When** the form is submitted, **Then** the time field is treated as not set (null/omitted), consistent with the existing optional behaviour.
6. **Given** a user enters a value that would produce a negative total (e.g., individually invalid inputs), **When** validation runs, **Then** an inline error is shown and submission is blocked.

---

### Edge Cases

- `startMissionTime` or `endMissionTime` of `null` or `0` are both treated as not set — Y/D/H/M/S fields render blank.
- Sub-field values are clamped on input to their Kerbin calendar bounds: Days `[0–425]`, Hours `[0–5]`, Minutes `[0–59]`, Seconds `[0–59]`; Years are unbounded (≥ 0).
- What happens when the existing Angular `*ngIf` / `*ngFor` directives in templates are replaced or updated — are there any other Angular version-specific APIs that need migrating?

## Design Direction

The application must visually resemble a grounded aerospace mission control interface inspired by Kerbal Space Program, modern telemetry dashboards, and industrial flight systems.

**Feel**: technical, operational, information-dense, utilitarian, and readable under long usage sessions.

**Avoid**: excessive animations, neon or cyberpunk aesthetics, glassmorphism, oversaturated colours, oversized decorative headings, overly playful UI elements.

### Colour Palette

Layered greys and muted industrial tones with purposeful accent colours:

- **Backgrounds**: charcoal / deep slate
- **Surfaces / panels**: slate grey, gunmetal
- **Borders**: muted metallic
- **Primary text**: off-white
- **Accent — informational**: blue
- **Accent — warning**: amber / orange
- **Accent — critical**: red
- **Accent — telemetry / highlights**: cyan

All text and UI elements must meet **WCAG AA contrast** requirements at minimum. Lime green primary text is explicitly excluded.

### Mission Readiness State Colours

- **Ready** → muted blue / cyan
- **At Risk** → amber / orange
- **Not Ready** → red

State indicators must use coloured left borders or status strips, with icon-assisted visibility. Colour must not be the sole differentiator.

### Typography

- System font stack: `-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif`
- Tabular numerics (fixed-width digit spacing) for metric values
- Compact but legible line spacing
- Avoid oversized headings, excessive weight variation, and decorative fonts

### Layout

- Panel-based layouts with bordered sections
- Dense but organised information hierarchy
- Mission cards / rows should resemble mission manifests or telemetry panels
- Minimal wasted whitespace
- Persistent visibility of key mission state

### Optional Environmental Styling

Subtle aerospace-inspired details are permitted where they do not reduce readability: thin grid overlays, telemetry separators, panel shadows, technical dividers. Scanline or noise textures are optional.

### Interaction Design

- Subtle hover states and restrained transitions
- Tactile button feedback
- Keyboard accessibility required
- Animations must be fast, subtle, and functional — no decorative motion

---

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application root route (`/`) MUST render the mission list without displaying any Angular default scaffold content (logo, welcome text, pill links, gradient elements).
- **FR-002**: All Angular default template content in `app.html` MUST be replaced with a minimal application shell containing only a persistent header/nav bar and a `<router-outlet>`.
- **FR-003**: The persistent header MUST contain, from left to right: the Mission Control logo icon (sourced from `frontend/public/big_icon.png`), the application name "Mission Control", and at least one navigation link (Missions, linking to the mission list).
- **FR-004**: The persistent header MUST include a burger menu button positioned at the top right. When clicked, the button MUST toggle a dropdown panel directly below it containing a "Coming soon" placeholder text card. The panel closes when the button is clicked again.
- **FR-005**: The header logo icon MUST be displayed at an appropriate size and aspect ratio for a navigation bar and MUST NOT be distorted.
- **FR-006**: All frontend stylesheets MUST be authored in SASS (`.scss`). Plain `.css` component or global stylesheets MUST be converted to `.scss`.
- **FR-007**: All per-component inline `styles` blocks and `style=""` attribute overrides MUST be migrated to component-scoped `.scss` files that conform to the Design Direction.
- **FR-008**: The global stylesheet MUST define a design token set (SASS variables and/or CSS custom properties for colour palette, typography, spacing scale) derived from the Design Direction section and imported or consumed by all component stylesheets.
- **FR-009**: Readiness state indicators (Ready / At Risk / Not Ready) MUST use coloured left borders or status strips and MUST NOT rely on colour alone — a text label or icon must also be present.
- **FR-010**: All text and UI elements MUST meet WCAG AA contrast requirements.
- **FR-011**: The Start Mission Time and End Mission Time inputs MUST be replaced with a Y/D/H/M/S composite input control.
- **FR-012**: The Y/D/H/M/S control MUST accept non-negative integer values only for each sub-field. Each sub-field MUST be clamped to its Kerbin calendar bound: Days `[0–425]`, Hours `[0–5]`, Minutes `[0–59]`, Seconds `[0–59]`; Years are unbounded (≥ 0).
- **FR-013**: The Y/D/H/M/S control MUST convert the entered sub-field values to a single total-seconds integer before sending to the API, using the Kerbin calendar constants (1 Year = 9,201,600 s; 1 Day = 21,600 s; 1 Hour = 3,600 s; 1 Minute = 60 s).
- **FR-014**: When loading an existing mission for editing, the API-provided seconds value for start/end time MUST be decomposed into Y/D/H/M/S and pre-populated into the corresponding sub-fields. A value of `null` or `0` MUST render the fields blank (treated as not set).
- **FR-015**: The Y/D/H/M/S control MUST be implemented as a reusable standalone component usable in any form context.
- **FR-016**: The application layout MUST remain usable and readable at mobile viewport widths (≤ 480px). On small screens, navigation links in the header MAY collapse into the burger menu.

### Key Entities

- **Application Header**: A persistent navigation bar rendered on all routes containing the logo icon, app name, navigation links, and burger menu button.
- **Burger Menu**: A toggleable side-panel or dropdown stub in the header's top-right corner, intended to host complex navigation in a future feature.
- **KerbinTime Input Component**: A reusable component that accepts an optional total-seconds value as input, emits a total-seconds value as output, and renders five labelled numeric sub-fields (Years, Days, Hours, Minutes, Seconds).
- **Design Token Set**: A collection of CSS custom properties (colour palette, font stack, spacing scale, border radii) derived from the Design Direction and consumed by all component stylesheets.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Zero Angular scaffold elements (logo, welcome heading, pill-group, divider) visible on any route after the change.
- **SC-002**: The application header is present on all routes and contains the Mission Control logo icon, the app name, at least one navigation link, and a burger menu button.
- **SC-003**: All three primary views (mission list, mission form, mission summary) pass a visual consistency check — same colour palette, typography, and button style — with no inline `style=""` attributes remaining on rendered HTML elements.
- **SC-004**: A readiness state indicator's colour is always accompanied by a text label, meeting the no-colour-alone requirement.
- **SC-005**: A mission planner can enter and save a mission start time of "Year 2, Day 15, 3 hours" without knowing or calculating the equivalent number of seconds.
- **SC-006**: When editing an existing mission with a previously saved time (e.g., 18,002,000 seconds), the Y/D/H/M/S fields pre-populate with the correct decomposed values on form load.
- **SC-007**: The application renders without horizontal scroll bars on a 375px-wide viewport for all primary views.

## Assumptions

- The API contract (request/response shapes for `startMissionTime` and `endMissionTime`) does not change — the frontend absorbs the Y/D/H/M/S decomposition entirely in the client.
- The Kerbin calendar constants match those defined in the backend `KerbinTime` value object: 1 Year = 9,201,600 s, 1 Day = 21,600 s, 1 Hour = 3,600 s, 1 Minute = 60 s.
- The design direction is fully specified in the Design Direction section above, drawn from the provided design brief. No external mockup or Figma file exists — the spec text is the authoritative design reference.
- The logo icon file `frontend/public/big_icon.png` exists and is suitable for use at navigation-bar scale without modification.
- The burger menu panel content is explicitly a stub in this feature — a dropdown with a "Coming soon" card. The panel's navigation contents are deferred to a future feature.
- The frontend build toolchain supports SASS out of the box (Angular CLI with `sass` package). If not already installed, adding the `sass` package is in scope for this feature.
- The existing Angular component structure (standalone components, `*ngIf`, `*ngFor`, `RouterLink`) is retained; no Angular upgrade or major refactor is in scope.
- The mission summary view's display of KerbinTime (already formatted by the API as `Yy, Dd, Hh, Mm, Ss`) does not require changes to the backend.
- Decorative animations are out of scope; only functional micro-interactions (hover states, button feedback) are permitted.
