# AGENTS.md — Nexus.Developer

**Repository**: `C:\Personal\Nexus.Developer`  
**Layer**: 07 DEVELOPER — define, plan and build software  
**Temporary control**: `NEXUS_DEVELOPMENT_CONTROL_v1.0.xlsx` until `M-07-1.1` imports it

## Read before every implementation session

1. This file.
2. `NEXUS_DEVELOPMENT_CONTROL_v1.0.xlsx` — `Master Roadmap`, `Active Changes` and
   `Session Protocol` sheets.
3. `control/CONTROL_MANIFEST.json` and `control/ACTIVE_CHANGES.csv` — machine-readable
   mirrors used by terminal and coding agents.
4. `..\Nexus.Platform\docs\DOCUMENTATION_INDEX.md`.
5. `..\Nexus.Platform\docs\CURRENT_STATE.md`.
6. `..\Nexus.Platform\docs\DEVELOPER_ARCHITECTURE.md`.
7. The active work item's named documents and acceptance criteria.

If the workbook or its mirror is unavailable, the requested node has no single current
version, or a required sibling repository is missing, stop and report. Do not guess.

## Mandatory preflight

Before editing, declare:

- change id and roadmap node id;
- repositories and projects;
- files or globs;
- schema/DbContext mutation;
- public contracts or APIs;
- dependencies;
- risk, worker, branch and sibling worktree.

Compare the declaration against every change whose status is not `Completed` or `Cancelled`.
Return exactly one verdict:

- `CLEAR`
- `DEPENDENCY FOUND`
- `OVERLAP FOUND`
- `CONFLICT FOUND`
- `ARCHITECTURE CONFLICT`

`CONFLICT FOUND` and `ARCHITECTURE CONFLICT` stop the work. Every negative verdict names
the rule and the conflicting change/node. Reserve the change before the first edit.

## Boundary rules

- DEVELOPER consumes CORE, DATA, GOVERNANCE, AI, AUTOMATION and PRODUCT CORE contracts.
- It may consume DELIVERY build-result contracts.
- It must not reference a product domain assembly, product `DbContext` or product database.
- It holds `ProductId`; it does not import product types.
- It does not implement chat, CI, document storage, identity, model providers or deployment.
- The desktop shell is a client/launcher only. Layer-owned code stays in the layer repository.
- Graph analysis is pure. Filesystem, git and process effects belong to Orchestration.
- One WorkItem equals one worker, one branch, one sibling worktree and one review.
- No integration occurs without a recorded human review.

## Append-only control

- Never delete roadmap or change history.
- A governed fact changes by adding a higher `Record Version`; the previous row becomes
  `Is Current = No`.
- Additions are appended physically and placed logically using `Sort Key`.
- Exactly one version per Node ID is current.
- A parent not marked `Breakdown Complete = Yes` reports not estimable, never a percentage.

## Completion

Build, test and inspect the reserved scope. Record evidence, human review and integration
result. Update the workbook, machine-readable mirrors, `CURRENT_STATE.md` and continuation
state before declaring completion.

