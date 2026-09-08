# SP1-W05 — Roadmap/Runtime Identity Bridge (WU-02) — Lane Report

**Repository:** `C:\Personal\Nexus-W3-W05` (worktree of `C:\Personal\Nexus.Developer`)
**Branch:** `sp1-w05-roadmap-runtime-bridge`
**Base:** `ffe25e0` (`P1-WAVE-01: add parallel wave 01 integration report`)
**Date:** 2026-09-07
**Lane:** P1-WAVE-03 · Lane E — Roadmap/runtime identity bridge (SP1-W05 / WU-02)
**Mode:** READ + decide (implement only if aggregate ownership is clear)
**Author:** Claude (bounded worker, lane isolation)

**Frozen direction applied:** WU-02 — `SourceRoadmapNodeId` is **optional, narrow, NOT added
everywhere**. It may be added only to an aggregate where a roadmap node was the origin of the
runtime aggregate and a later Forge/roadmap round-trip must follow that origin **without a
running Developer process** (so the reference must be persisted on the aggregate, never
inferred at read time).

---

## 1. Verdict

**HUMAN_DECISION_REQUIRED** — do not implement in this lane.

No aggregate in Nexus.Developer is today originated from a roadmap node through any real
creation/import/use path, and the planned origin seam (the roadmap importer, `WI-07-1.1.3`)
is not implemented and its target-node mapping has already diverged from the aggregates that
exist in code. Per the lane's hard constraint, ownership/use path is not clear enough to
write the field onto any aggregate; guessing which aggregate the future importer will create
would be speculative. The authoritative WU-02 document (`WORK_UNIVERSE.md` §8) itself stops
short of adding the field — it names `Feature` and `Task` as *"most plausibly"* needing the
bridge *"during the bootstrap phase"* and defers the field-name and aggregate choice to
implementation time. Implementation time is not now: there is no roadmap→runtime writer and
no Forge/roadmap reader yet.

---

## 2. The two identity spaces (confirmed DIFFERENT)

Evidence: `src/Nexus.Developer.Core/DevelopmentControl/NodeId.cs` (class comment):

> "The Development Control Node's identity is the Master Roadmap's own human-authored node id
> scheme — strings like `WI-07-2.1.1`, `M-07-2.1`, `F-07-10` … NOT a Guid. Every other
> aggregate in this repo (Feature, Task, WorkItemDependency, DevelopmentRun…) uses a
> Guid-backed strongly-typed id; this one deliberately does not."

- **Roadmap-ledger id space:** human-authored strings (`F-`/`M-`/`WI-`/`T-`/`S-`/`CHG-`…),
  used by the DevelopmentControl workbook (Master Roadmap sheet), `nexus-roadmap.yaml`, the
  Forge `idSpaces.RoadmapNode`, and the Developer `DevelopmentControl.NodeId` value type.
- **Runtime aggregate id space:** Guid-backed strongly-typed ids
  (`FeatureId`, `TaskId`, `SubtaskId`, `MilestoneId`, `IssueId`, `DevelopmentRunId`,
  `WorkItemDependencyId` in `src/Nexus.Developer.Core/Common/Identifiers/`), plus opaque
  foreign `SubprojectId(Guid)` and `ObjectChatLinkId`.

A bridge between them is therefore a **plain stored string reference**, never id-prefix
routing, never a Guid, and never merged/renumbered. `WORK_UNIVERSE.md` §8 ("Roadmap-ledger ↔
runtime identity bridge") states the same and lists the field naming direction
(`RoadmapLedgerReference` / `SourceRoadmapNodeId` / `ExternalPlanningReference`) with the
final name deferred to implementation.

---

## 3. Inspection evidence — creation / import / use paths

Files read and evidence gathered (all paths absolute):

**Core aggregates**
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Features\Feature.cs`
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Tasks\Task.cs`
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Subtasks\Subtask.cs`
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Milestones\Milestone.cs`
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Issues\Issue.cs`
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\DevelopmentRuns\DevelopmentRun.cs`
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Scope\ScopeSubproject.cs` (foreign DTO)
- `C:\Personal\Nexus-W3-W05\src\Nexus.Developer.Core\Common\Identifiers\*.cs`

**Creation handlers (Application)**
- `...\Application\Features\Commands\CreateFeature\CreateFeatureHandler.cs`
- `...\Application\Tasks\Commands\CreateTask\CreateTaskHandler.cs`
- `...\Application\ChatCore\Commands\ConvertConversationToFeature\ConvertConversationToFeatureHandler.cs`
  (+ sibling `ConvertConversationTo{Task,Subtask,Milestone,Issue}` slices)

**API contracts (requests carry no roadmap field)**
- `...\Api\Endpoints\{Features,Tasks,Subtasks,Milestones,Issues,DevelopmentRuns}\Create*Request.cs`
- `...\Api\Endpoints\Features\FeatureEndpoint.cs`, `...\ChatCore\ConvertConversationTo*Endpoint.cs`

**Persistence**
- `...\Infrastructure\Sql\NexusDeveloperDbContext.cs` (EF schema `dev`; DbSets: Feature, Task,
  Subtask, Milestone(+Link), Issue(+Link), ObjectChatLink, DevelopmentRun, WorkItemDependency)
- `...\Infrastructure\Sql\Configurations\{Feature,Task,Subtask,Milestone,Issue,DevelopmentRun,...}Configuration.cs`
- `...\Infrastructure\Migrations\{20260827064146_InitialSqlSchema, 20260830082704_AddWorkItemDependency}`
- `...\Infrastructure\Sql\Repositories\SqlFeatureRepository.cs`

**DevelopmentControl (roadmap ledger) surface — read path only, disconnected**
- `...\Core\DevelopmentControl\NodeId.cs`, `Node.cs`, `ActiveChange.cs`,
  `IDevelopmentControlStore.cs`
- `...\Infrastructure\DevelopmentControl\ExcelDevelopmentControlStore.cs`
- grep across `src` for `using Nexus.Developer.Core.DevelopmentControl` **outside** the
  `DevelopmentControl` folders → **zero hits**. The runtime slices never reference the
  DevelopmentControl node model.

**Cross-repo architecture authorities**
- `C:\Personal\Nexus.Platform\docs\WORK_UNIVERSE.md` (WU-02 output; §8 identity bridge, §9
  Forge `BOOTSTRAP_SAFE`)
- `C:\Personal\Nexus.Platform\docs\DEVELOPER_ARCHITECTURE.md` (§3 target map: roadmap importer
  in Infrastructure; §17: `nexus-roadmap.yaml` "exists to be imported — M-07-1.1,
  WI-07-1.1.3, idempotent")
- `C:\Personal\Roadmaps\nexus-roadmap.yaml` (M-07-1.1 Work graph aggregates; WI-07-1.1.3
  Roadmap import)
- `C:\Personal\Roadmaps\NEXUS_MASTER_ARCHITECTURE.md`
- `C:\Personal\Nexus.Developer\architecture\SP1_PARALLEL_WAVE_02_REPORT.md` (context;
  DevelopmentRun + schema lanes)

### 3.1 How the aggregates are created today

| Aggregate | Creation sources actually in code | Roadmap node involved? |
|---|---|---|
| `Feature` | `CreateFeatureHandler` (manual, under a Product-Core `SubprojectId` validated via `IScopeClient.GetSubprojectAsync`); `ConvertConversationToFeatureHandler` (from a Chat Core conversation) | **No** — parent is a Product-Core Subproject (opaque Guid), not a roadmap node |
| `Task` | `CreateTaskHandler` (manual, under a `FeatureId`); `ConvertConversationToTaskHandler` (from conversation); `Task.CreateFromWorkItemMigration` (one-time Chat `WorkItem` migration, keeps `MigratedFromWorkItemId` as a **Guid**) | **No** — migration provenance is a Chat `WorkItem` Guid, not a roadmap string id |
| `Subtask` | `CreateSubtaskHandler` (under `TaskId`) / conversation conversion | No |
| `Milestone` | `CreateMilestoneHandler` (under `SubprojectId`) / conversation conversion | No |
| `Issue` | `CreateIssueHandler` (free-floating, positioned via `IssueLink`) / conversation conversion | No |
| `DevelopmentRun` | Create + Get only (base `ffe25e0`); targets a runtime aggregate by `DevelopmentRunTargetType {Feature,Task,Issue}` + `Guid TargetId` | No — targets runtime Guids, not roadmap nodes |
| `Subproject` | Not a Developer aggregate — foreign Product-Core concept; Developer holds `SubprojectId(Guid)` + `ScopeSubproject` echo DTO only | No |

Every aggregate also carries an auto-generated, unique, DB-computed `Reference`
(`FEA-00000001`, `TSK-…`, `MIL-…`, `ISS-…`, `RUN-…`) — a runtime ref space, **distinct** from
the roadmap id space. No code path writes a roadmap node id anywhere onto these rows.

### 3.2 Import / conversion seams — what exists vs. what is planned

**Existing seams (all non-roadmap):**
1. Product-Core Subproject scope validation (`IScopeClient` / `HttpScopeClient`).
2. Chat-conversation → work-object conversion (`ConvertConversationTo*`, M-12-0.1).
3. Chat `WorkItem` → `Task` one-time migration (`CreateFromWorkItemMigration`, WI-07-10.2.1).

**Planned, NOT built (roadmap→runtime):**
- `nexus-roadmap.yaml` M-07-1.1 `WI-07-1.1.3` "Roadmap import" — idempotent import of the
  roadmap into the work graph, scoped to `Nexus.Developer.Infrastructure`
  (confirmed in the yaml and in `DEVELOPER_ARCHITECTURE.md` §3/§17).
- Architecture audit (2026-08-25): "152 milestones … temporary nodes can be imported into the
  permanent work graph" — the same future import.
- No importer class, repository, or endpoint for roadmap import exists anywhere in
  `src/` (grep for `class.*Importer` / `RoadmapImport` → zero hits), nor in Forge/DevTools
  (DevBridge has no Developer-API client and no feature/task/roadmap integration).
- Forge resolution of "roadmap node → runtime work object" is stated in `WORK_UNIVERSE.md` §9
  as a future capability ("should be able to resolve, **where available**"), with Forge
  remaining `BOOTSTRAP_SAFE`. No reader exists today.

**Vocabulary divergence (why a roadmap→aggregate mapping is not settled):** the roadmap's
M-07-1.1 model names `ProductDevelopment > Module > Feature > Milestone > WorkItem > Task >
Subtask`; the implemented v2.3 model (ADR-005 / CHG-20260826-003) is `Workspace > Project >
Subproject > Feature > Task > Subtask`, with `WorkItem` folded into `Task` via migration
provenance. A future importer therefore cannot be assumed to land roadmap nodes on `Feature`
and `Task` 1:1 without a mapping decision that does not exist yet.

---

## 4. Ownership analysis per candidate aggregate

The strict WU-02 test — *"a roadmap node was the origin of the runtime aggregate, and a later
Forge/roadmap round-trip must follow it without a running Developer process"*:

| Aggregate | Roadmap-originated today? | Genuine bridge need today? | Notes |
|---|---|---|---|
| `Feature` | No (Subproduct/conversation origins only) | Not demonstrable | WORK_UNIVERSE §8 names it the *most plausible* future candidate, but only "during the bootstrap phase" and only once an origin seam exists |
| `Task` | No (manual / conversation / Chat `WorkItem` Guid migration) | Not demonstrable | Same — plausible future candidate; today its only external-origin precedent is a **Guid** (`MigratedFromWorkItemId`), which is a different pattern and would not be a roadmap bridge |
| `Subtask` | No | No | Deepest level of the owned hierarchy; no roadmap analog mapping shown |
| `Milestone` | No | No | Roadmap `M-…` nodes govern *building Nexus itself*; runtime `Milestone` is a delivery-grouping object under a Product-Core Subproject. Distinct concepts; a stored `M-…` id would invite conflation |
| `Issue` | No | No | Free-floating, positioned by links; no roadmap origin |
| `DevelopmentRun` | No | No | Targets runtime Guids (`Feature`/`Task`/`Issue`); a run's *governance* envelope is `ActiveChange` on the workbook side, already keyed by roadmap `NodeId` on the other side of the boundary |
| `Subproject` | No | No | Foreign Product-Core identity; Developer must not own its provenance |

**Conclusion:** zero aggregates satisfy the strict test today. The only credible candidates
(`Feature`, `Task`) are forward-looking, with no writer and no reader, and the roadmap→node
mapping their field would encode is unresolved. Adding `string? SourceRoadmapNodeId` now to
either would be guessing the future importer's shape, naming, and target aggregate.

---

## 5. What is ambiguous — the minimal decision a human must make

1. **Timing / trigger.** Add the field only when the roadmap importer (WI-07-1.1.3) or a
   Forge bootstrap writer exists — or add it now as an empty, forward-compatible column?
   (Recommendation in evidence: add it *with* the writer, so it is never a dormant column.)
2. **Target aggregate.** `Feature` only, `Task` only, or both? (WORK_UNIVERSE says "most
   plausibly Feature and Task", not settled.)
3. **Field name.** `SourceRoadmapNodeId` vs `RoadmapLedgerReference` vs
   `ExternalPlanningReference` — WORK_UNIVERSE leaves this to implementation; it should be one
   decision across Developer + Forge + Platform docs.
4. **Semantics.** Does the value hold the roadmap `NodeId` verbatim (e.g. `WI-07-2.1.1`)?
   Is it populated at import, at manual create, at conversation-convert, or by an explicit
   "link to roadmap node" action? Is it required for imported rows?
5. **Consumer contract.** Who reads it, and how (Developer query surface? Forge reading the
   `dev` schema directly, BOOTSTRAP_SAFE? a roadmap→runtime lookup API)? No reader exists; the
   bridge's whole purpose is Forge following origin without a live Developer process, which
   implies a **persisted column** (never inferred), but the read path is undefined today.
6. **Roadmap source of truth.** The workbook Master Roadmap and `nexus-roadmap.yaml` are
   reconciled but separate; the field should store the versionless `NodeId` string, and
   workbook re-import/drift policy must be decided.

---

## 6. If the human decides to implement (for the follow-on lane)

Concrete manifest that a later implementation would touch (base `ffe25e0`, following existing
additive-field conventions e.g. M04 `WorkItemDependency.Reason`):

- Core: `Feature.cs` / `Task.cs` — optional `string? SourceRoadmapNodeId`, blank→null on
  construction, get-only, set only at creation or via a named factory (mirroring
  `Task.CreateFromWorkItemMigration`), carried through `Restore`. Never changes the aggregate's
  own Guid identity. No routing logic.
- Infrastructure: `FeatureConfiguration.cs` / `TaskConfiguration.cs` (`nvarchar(200)`,
  nullable), EF model snapshot, additive migration (a new migration timestamp; the two prior
  migrations are `2026…`).
- Application: `CreateFeatureCommand` / `CreateTaskCommand` (+ Handler), request DTOs if the
  value may be caller-supplied; **or** an importer-only factory if not exposed on the public
  create contract.
- Tests: round-trip persistence via `Restore`, optionality (null default), identity unchanged,
  no routing; following `FeatureTests.cs` / `TaskTests.cs` conventions.
- The roadmap `NodeId` type is string-space; the stored field must be a plain `string?`, never
  a Guid, never the DevelopmentControl `NodeId` value type on a runtime aggregate unless the
  dependency direction is explicitly accepted (Core runtime currently has zero references to
  the DevelopmentControl namespace).

**Not implemented here** because no aggregate currently passes the strict origin test and the
ownership/use path is therefore unresolved.

---

## 7. Verification

- Read-only lane: **no source, test, config, or workbook file was modified.**
- Baseline build: `dotnet build Nexus.Developer.slnx -c Release` → **0 warnings / 0 errors**
  (5 projects, .NET 10).
- Baseline tests: `dotnet test Nexus.Developer.slnx -c Release --no-build` → **248 passed /
  0 failed / 0 skipped**.
- Grep proofs: zero occurrences of `SourceRoadmapNodeId` / `RoadmapNodeId` in the Developer
  tree (the field does not exist anywhere yet); zero `using …DevelopmentControl` references in
  non-DevelopmentControl source; zero roadmap-importer classes in `src`.

---

## 8. Risks / notes

1. **Speculative-column risk.** Adding `SourceRoadmapNodeId` without a writer and a reader
   would create a dormant EF column + public-contract expectation that the roadmap importer
   may later shape differently (name, aggregate, requiredness).
2. **Vocabulary drift.** The roadmap's M-07-1.1 node vocabulary does not match the implemented
   ADR-005 hierarchy; a roadmap→runtime mapping decision is prerequisite to a correct column.
3. **Cross-repo naming.** The field name must be frozen jointly with Forge and Platform docs
   (`WORK_UNIVERSE.md` names three candidate directions).
4. **Identity conflation risk.** A `Milestone`-level roadmap `M-…` id and the runtime
   `Reference` (`MIL-…`) are easily confused; the report found no safe basis to add the bridge
   to `Milestone`.
5. **Nothing was committed, staged, pushed, or merged.** This lane produced one file
   (this report) in `architecture/`; the working tree is otherwise clean.

---

## 9. Statement

Nothing was committed, staged, pushed, or merged in this lane. `NEXUS_DEVELOPMENT_CONTROL.xlsx`
was not touched. This report is the lane's only output.
