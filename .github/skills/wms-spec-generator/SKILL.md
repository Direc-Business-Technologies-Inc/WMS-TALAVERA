---
name: wms-spec-generator
description: "Create repository-aware feature, bugfix, or RCA specifications for this WMS solution. Use when asked to plan, specify, or document a feature, defect fix, root-cause analysis, architecture change, or implementation plan as requirements, design, and tasks. Reads CLAUDE.md and the .claude project guides before drafting."
argument-hint: "feature|bugfix|rca, name, desired behavior or defect, scope, and constraints"
user-invocable: true
---

# WMS Specification Generator

Create a reviewed three-document specification package grounded in this repository:

1. Requirements: user needs, acceptance criteria, scope, and preservation requirements.
2. Design: architecture, data and interaction flows, correctness properties, and validation strategy.
3. Tasks: phased implementation steps, checkpoints, measurable completion criteria, and requirement traceability.

This skill generates specifications; it does not implement them. Do not write specification files until the user has reviewed and explicitly confirmed all three phases. RCA-only mode produces a single analysis document after confirmation.

## When to Use

- Planning a new feature, integration, or architectural addition (FEATURE mode).
- Describing a defect, root cause, fix, and regression prevention (BUGFIX mode).
- Investigating and documenting a root cause without authorizing a fix plan (RCA mode).
- Producing an implementation plan grounded in existing WMS conventions.

If the user has not specified a mode, infer it when unambiguous. If a defect request could mean either a fix deliverable or analysis only, ask which they want. Never infer permission to implement a fix from a spec or RCA request.

## Repository Context Is Required

Before drafting, inspect the repository rather than relying on assumptions or generic templates.

1. Read `CLAUDE.md` and every Markdown guide in `.claude/`. These currently include:
   - `application_layer.md`
   - `architectural_debts.md`
   - `auth.md`
   - `blazor_conventions.md`
   - `building_the_project.md`
   - `code_conventions.md`
   - `database_patterns.md`
   - `domain_layer.md`
   - `sap_integration.md`
   - `service_architecture.md`
   - `ui_abstractions.md`
2. Inspect `WMS.slnx` and the relevant project files to establish the actual target stack, project boundaries, frameworks, and available test projects. Treat source and project files as evidence; do not assume every feature uses every listed technology or integration.
3. Inspect the closest existing implementation and its tests, if present. Prefer the owning abstraction and a nearby analogous feature. Read more broadly only when needed to resolve a design decision.
4. Apply the `.claude` guides relevant to the requested feature. Consider all guides when establishing boundaries and constraints; explicitly identify any guidance that does not apply.
5. Check known architectural debts in `.claude/architectural_debts.md`. Do not propose to fix or consolidate a listed debt unless the user explicitly includes it in scope.
6. If current code conflicts with documentation, report the discrepancy and base the proposed design on verified current behavior. Do not silently make up a reconciliation.
7. If `.github/specs/` already contains specifications, search for prior decisions, deferred scope, and related requirements. Cite relevant documents; do not treat old specifications as more authoritative than current code or project guidance.

For this repository, commonly relevant constraints include:

- The solution is organized into Domain, Application, DTO, database, integration, shared, Blazor Server, API, and mobile projects. Verify actual project names and target frameworks before describing a feature's path.
- Application requests use MediatR commands/queries and handlers; transactional document commands follow `ITransactionalRequest`. DTOs cross the Application boundary; Mapster mappings belong in the Application registration.
- Blazor components follow the documented `BaseComponent` / `BaseForm<T>` conventions and call through feature handler interfaces and thin dispatchers. Do not propose direct component access to MediatR, repositories, or `DbContext`.
- Domain entities use the documented `DEM` conventions, domain behavior belongs in entities/value objects, and EF configuration belongs in the database project. Verify aggregate invariants in the target feature.
- Follow documented async, error handling, authorization, UI action, unsaved-change, and EF query patterns where relevant.
- Keep SAP and NS integrations in their verified integration projects and follow their existing outbound-call boundaries. Do not infer that a feature uses SAP merely because the SAP guide exists.
- Build and environment expectations come from `.claude/building_the_project.md`; never put secrets or connection strings in committed configuration.

After context gathering, give the user 3-5 concise findings: verified stack and target projects, relevant local patterns, existing analogous code/tests, and any constraints or discrepancies. Ask only questions that repository evidence cannot answer. Do not ask the user to repeat facts established by the repository.

## Modes

### FEATURE

Capture user stories, value, scope and non-goals, interfaces and data flows, domain and authorization rules, correctness properties, and an appropriate testing strategy.

### BUGFIX

Capture impact, the triggering condition, observed and expected behavior, evidence-backed root cause (or clearly labeled hypothesis), fix properties, and preservation requirements. Use three validation phases: demonstrate the defect on unfixed behavior when feasible, verify the fix, and verify unaffected behavior remains unchanged. Do not invent a failing test when the defect cannot be reproduced or the relevant test surface is unavailable; state the limitation.

### RCA

Produce a standalone root-cause analysis when the user wants investigation/documentation but no implementation plan. Include the symptom and impact, evidence-backed mechanism (label hypotheses), affected paths and blast radius, relevant history only when verified, recommended corrective direction, and deliberately deferred concerns. Do not create requirements/design/tasks or imply that a fix is authorized. Ask the user to confirm the RCA before writing it.

## Confirmation-Gated Procedure

Do not generate all phases at once. Present one complete phase at a time, collect feedback, revise as needed, and wait for explicit confirmation before advancing.

### Phase 1: Requirements

Present the complete requirements document in the conversation. Include:

- Metadata: current date, `Draft` status, mode, verified scope, and related specification links (or `None found`).
- A concise introduction and a mandatory glossary of domain terms.
- Hierarchically numbered requirements (`1`, `1.1`, `1.2`, `2`, ...).
- User stories in FEATURE mode. In BUGFIX mode, include bug conditions and current behavior.
- Acceptance criteria in precise, observable `WHEN ... THEN ... SHALL ...` form.
- In-scope and out-of-scope behavior.
- Preservation requirements describing existing behavior that must not change.

Ask whether the requirements are correct and what should change. Do not proceed until the user explicitly confirms, for example: `Requirements confirmed. Proceed to Design.`

### Phase 2: Design

Only after requirements confirmation, present the complete design document. Map each design choice to confirmed requirements and repository evidence. Include:

- Metadata consistent with the requirements document.
- A decision record: problem, decisions and rationale, and explicit trade-offs.
- A concise “What already exists” inventory with verified file paths and line numbers for relevant plumbing; conclude with what changes and what does not.
- Overview, verified technologies, design principles, and explicit trade-offs.
- A Mermaid architecture or sequence diagram when it clarifies component boundaries or data flow.
- Components, responsibilities, contracts, and data models; identify schema changes only when evidence supports them.
- Three to ten testable correctness properties, each linked to requirement IDs.
- Error handling and user-visible behavior where applicable.
- Unit, integration, UI/API, and regression testing that fits the actual test projects and feature risk.
- An explicit property-based testing assessment (`APPLICABLE` or `NOT APPLICABLE`) with rationale.
- Accepted consequences, out-of-scope items, and a verification checklist.
- In BUGFIX mode, formal bug-condition examples, evidence-backed root cause or labeled hypothesis, and preservation properties.
- In BUGFIX mode, describe blast radius, not only the reported symptom. Use commit history only when it is available and materially clarifies how the defect arose; do not imply forensic certainty without evidence.

Ask whether the design is correct. Do not proceed until the user explicitly confirms, for example: `Design confirmed. Proceed to Tasks.`

### Phase 3: Tasks

Only after design confirmation, present the complete implementation plan. Include:

- Metadata consistent with the other documents, a `Spec` link to the design document, goal, architecture summary, and verified technical context.
- Global constraints and background an implementer needs, drawn from applicable WMS project guidance and verified feature behavior.
- Sequentially numbered tasks organized into phases.
- For each implementation task, identify exact files and relevant line numbers when known, interfaces/contracts consumed or produced, and actionable current-to-target changes. Mark unresolved file locations as investigation tasks rather than guessing.
- Actionable subtasks for complex work.
- Measurable acceptance/completion criteria and validation checkpoints after major phases.
- Requirement traceability on every task, using `_Requirements: 1.1, 2.3_`.
- Optional work clearly marked `Optional` and separated from required MVP tasks.
- Test-first exploratory, fix-checking, and preservation work for BUGFIX mode when feasible.
- Relevant build/test commands and machine-checkable assertions only when supported by repository guidance and available projects. Adapt to the actual test projects and do not invent a test project, expected counts, or verification results.

Ask for review and wait for explicit confirmation, for example: `Tasks confirmed. Proceed to write.` Do not implement the plan as part of this skill.

### RCA-only: Present and Confirm

Present one standalone RCA document in the conversation, using the RCA mode requirements above. Ask for review and wait for explicit confirmation before writing. If the user requests a fix specification instead, restart at the Requirements phase in BUGFIX mode.

### Phase 4: Write the Confirmed Specification

Only after all three phases are confirmed, create:

```text
.github/specs/YYYY-MM-DD-<kebab-case-name>/requirements.md
.github/specs/YYYY-MM-DD-<kebab-case-name>/design.md
.github/specs/YYYY-MM-DD-<kebab-case-name>/tasks.md
```

Use the current date for the folder name. If that folder already exists, do not overwrite or silently edit it; ask the user for a unique suffix or revised slug. Write the confirmed content without introducing unreviewed scope or design changes. Then verify the files exist, metadata and requirement IDs are consistent, every task traces to requirements, and the documents agree with each other. Report the resulting path and any validation limitation.

For confirmed RCA-only mode, write one file instead:

```text
.github/specs/YYYY-MM-DD-<kebab-case-name>-rca.md
```

After implementation is complete, an `implementation-notes.md` may be added only when requested and only from actual changed files and verification evidence. It should record what shipped, how it differs from the design, trade-offs, remaining concerns, and verification; do not generate it from the task plan before implementation.

## Document Conventions

Every written document includes a metadata block with `Date`, `Status`, `Mode`, `Scope`, and `Related`. Start with `Draft`; update to `Confirmed` only after the user's approval. Keep dated specifications immutable: do not silently rewrite a prior confirmed spec. If implementation later disproves a design decision, record the correction in implementation notes or a clearly labeled decision-corrections section, and keep the original decision traceable.

Cross-reference only verified prior specs. In tasks, link to the sibling `design.md`. When there are no related specs, write `None found` rather than inventing a reference.

## Quality Checks

Before presenting each phase, check that:

- Repository facts are supported by the inspected files and local conventions are applied where relevant.
- Written documents have consistent metadata, dates, and verified cross-references; existing dated specs are not overwritten.
- Requirement numbering, terminology, scope, and acceptance criteria are consistent.
- Every correctness property is testable and mapped to requirement IDs.
- Tasks are actionable, ordered by dependency, traceable to requirements, and specific about files/interfaces/verification without guessing.
- Known architectural debts are preserved unless explicitly in scope.
- No secrets, unsupported schema/API assumptions, or unrequested implementation details are introduced.
- The user has confirmed the previous phase before the next phase is presented.
- Repository-specific constraints are verified from WMS guidance and code; do not import LSMS-only rules such as its migration prohibition or handler guard behavior.

When implementation later begins, treat the task plan as guidance, re-check current repository state, and follow the applicable repository and tool instructions. This specification workflow itself must not create branches, commits, or pushes.