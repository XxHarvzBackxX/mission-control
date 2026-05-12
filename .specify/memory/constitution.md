<!--
SYNC IMPACT REPORT
==================
Version change: 1.1.0 → 1.2.0
Modified principles: Technology Stack (Testing framework changed from xUnit to NUnit)
Added sections: N/A
Removed sections: N/A
Templates reviewed:
  - .specify/templates/plan-template.md  ✅ no xUnit references
  - .specify/templates/spec-template.md  ✅ no xUnit references
  - .specify/templates/tasks-template.md ✅ no xUnit references
Follow-up TODOs: Update all existing plan.md files referencing xUnit → NUnit
-->
<!--
SYNC IMPACT REPORT
==================
Version change: 1.0.0 → 1.1.0
Modified principles: IX. Purposeful Documentation (added)
Added sections:
  - Core Principles (I–IX)
  - Technology Stack
  - Development Standards
  - Governance
Removed sections: N/A (initial ratification)
Templates reviewed:
  - .specify/templates/plan-template.md  ✅ aligned (Constitution Check gates placeholder present)
  - .specify/templates/spec-template.md  ✅ aligned (no principle-specific constraints to update)
  - .specify/templates/tasks-template.md ✅ aligned (test tasks marked optional; unit test requirement
    captured here in constitution)
  - No commands/ directory present under templates — no CLAUDE-specific references to fix
Follow-up TODOs: none
-->

# Mission Control Constitution

## Core Principles

### I. Modular Architecture

The application MUST maintain a strict separation between frontend and backend. The Angular
frontend and the .NET Web API backend are independent layers — each with its own responsibilities,
build artefacts, and deployment surface. Cross-layer coupling (e.g., shared state assumptions,
tight timing dependencies) is prohibited. Every feature MUST be designed to respect this boundary
from the outset.

### II. Component-Driven Frontend

The Angular frontend MUST be structured as a composition of well-defined, self-contained
components. Components MUST be responsive by default. Presentation logic MUST NOT bleed into
services or state management layers, and services MUST NOT contain rendering or layout concerns.
New UI features MUST be delivered as discrete, independently renderable components.

### III. Domain-Driven Backend

The .NET Web API backend MUST be modelled using Domain-Driven Design (DDD) principles.
Domain entities, aggregates, value objects, and domain services MUST be defined in an isolated
domain layer with no dependency on infrastructure or presentation concerns. All API surface
contracts MUST use strongly typed DTOs — primitive obsession and anonymous objects are
prohibited at API boundaries.

### IV. Business Logic Isolation (NON-NEGOTIABLE)

Business rules MUST reside exclusively in the domain layer and MUST NOT be duplicated in or
dependent on presentation code, API controllers, or infrastructure adapters. Controllers and
components are permitted only to orchestrate calls and map results; they MUST NOT contain
conditional business logic. Any rule that could change based on domain requirements belongs in
the domain, not the UI or transport layer.

### V. Deterministic Readiness Calculations (NON-NEGOTIABLE)

All readiness and status calculations MUST be fully deterministic: given the same inputs, the
same output MUST always be produced with no reliance on external state, randomness, or
side-effects. Calculation functions MUST be pure and free of I/O. This constraint exists to
ensure auditability and to make every calculation independently verifiable without running the
full application stack.

### VI. Unit Test Coverage for Core Mission Logic (NON-NEGOTIABLE)

All core mission calculation logic MUST be covered by unit tests. Tests MUST be written before
or alongside implementation (not retroactively). Each calculation unit MUST have tests that
verify correct output for expected inputs, boundary conditions, and invalid/edge-case inputs.
Tests MUST be runnable in isolation with no infrastructure dependencies (no database, no
network, no file system).

### VII. Readability and Maintainability First

Code MUST be written for the next reader, not for the compiler. Clarity MUST take precedence
over cleverness or micro-optimisation. Premature optimisation is prohibited unless a measurable
performance problem exists and is documented. Abstractions MUST earn their complexity by solving
a concrete, recurring problem — not by anticipating future requirements.

### VIII. Minimal Dependencies

The application MUST avoid introducing third-party dependencies unless no reasonable
first-party implementation exists, or the dependency provides substantial, well-understood
value. Every new dependency MUST be justified in the relevant design document. Dependencies
that are added for convenience (e.g., single-function utilities, thin wrappers) are prohibited.

### IX. Purposeful Documentation

Public APIs and non-obvious business logic SHOULD use concise XML documentation comments
where appropriate. Documentation MUST improve readability without becoming excessively verbose
or duplicating implementation details. Comments that merely restate the code (e.g.,
`/// Gets the name`) are prohibited — if a name is self-explanatory, no comment is required.

## Technology Stack

| Layer    | Technology              | Constraint                                           |
|----------|-------------------------|------------------------------------------------------|
| Frontend | Angular (latest stable) | Component-driven; no framework mixing                |
| Backend  | .NET Web API            | DDD layering; strongly typed DTOs at all boundaries  |
| Testing  | NUnit (.NET) / Karma + Jasmine (Angular) | Unit tests mandatory for domain logic |

The technology choices above are load-bearing constraints. Introducing an additional framework
or replacing a listed technology requires a constitution amendment.

## Development Standards

- All pull requests MUST be reviewed against Principles I–IX before merge.
- A PR introducing business logic outside the domain layer MUST be rejected.
- A PR introducing a new dependency without documented justification MUST be rejected.
- Unit tests for new or modified mission calculation logic are a merge prerequisite, not
  a follow-up.
- Performance optimisations MUST be accompanied by a benchmark or profiling artefact that
  justifies the complexity cost.

## Governance

This constitution supersedes all other documented practices, conventions, and team norms.
Where conflict exists between this document and any other guideline, this document takes
precedence.

**Amendment procedure**: Amendments require (1) a written proposal describing the change
and rationale, (2) version bump per semantic versioning rules (MAJOR for principle removal
or redefinition, MINOR for additions, PATCH for clarifications), and (3) propagation of
changes to all dependent templates under `.specify/templates/`.

**Compliance reviews**: Compliance against this constitution MUST be verified at every
pull-request review. Exceptions require explicit, documented justification recorded in the
Complexity Tracking section of the relevant `plan.md`.

**Version**: 1.2.0 | **Ratified**: 2026-05-12 | **Last Amended**: 2026-05-12
