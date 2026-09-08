# SP1 — Parallel Wave 03 (Nexus V2.3 Phase-1) Integration Report

**Repo:** `C:\Personal\Nexus.Developer` (primary) · **Base commit:** `ffe25e0`
**Date:** 2026-09-07
**Scope:** P1-WAVE-03 lanes A–G across isolated worktrees.
**Workbook guard:** `NEXUS_DEVELOPMENT_CONTROL.xlsx` SHA-256 must remain
`8AA73778A3F1CDB97D1A58BB05BD4797CA5A409BE8F986EF19FD9EDBB7CFE69F` (verified live at report time — see §9).
**Git discipline:** No automatic merges, commits, pushes, or `git add .`. Every lane independently reviewable.

---

## 1. Lane result summary

| Lane | Scope | Verdict | Evidence / report |
|---|---|---|---|
| A — M05 Developer API wiring | Make DevelopmentControl a usable Application/API surface; bind M00 writer-lock through real composition root | **PASS** — reviewed + independently verified | `SP1_M05_DEVELOPER_API_REPORT.md` (§1.1) |
| B — M02 final live interop proof | Run real cross-process Forge↔Developer writer-lock proof (6 cases) against disposable workbooks | **PASS** — live 15/15 (§1.3); SP1-M02 FULLY COMPLETE | `DevTools-W2-M02 …/SP1_M02_SHARED_WRITER_LOCK_REPORT.md` (+§10 `LIVE_INTEROP_PROOF`) |
| C — D03/D04 product projection + Subfeature | Minimal Product work-management projection over Governance ProductId + `Feature.ParentFeatureId` | **D04 PASS** — verified (268 lane / 372 combined); **D03 BLOCKED** (§1.4) | `SP1_D03_D04_PRODUCT_SUBFEATURE_REPORT.md` |
| D — Subchat `Branch.ParentBranchId` | Only `Branch.ParentBranchId`; Conversation stays canonical; self-parent/cycles prevented | **PASS** — reviewed + independently verified | `SP1_D06_SUBCHAT_REPORT.md` (§1.2) |
| E — W05 roadmap/runtime bridge | READ + decide `SourceRoadmapNodeId` | **HUMAN_DECISION_REQUIRED** — no implementation | `Nexus-W3-W05 …/SP1_W05_ROADMAP_RUNTIME_BRIDGE_REPORT.md` |
| F — C03 migration manifest | Exact OLD → D:\NEXUS manifest for every active repo; UNKNOWNs resolved | **PASS** — one manifest file, no physical moves | `SP1_C03_NEXUS_MIGRATION_MANIFEST.md` |
| G — Wave-04 prep (READ-ONLY) | Dependencies for DevelopmentControlAddress, typed DCR, Work-Universe-aware Context Resolver | **COMPLETE** — planning section in §7; typed-DCR term flagged | §7 of this report |

### 1.1 Lane A (SP1-M05 — DevelopmentControl Application/API wiring) — `PASS` (reviewed + independently verified)

Worktree `Nexus-W3-M05`, branch `sp1-m05-developer-api` (base `ffe25e0`), carrying the
pre-existing uncommitted B1+B2 candidate with Lane A layered on top. Integration-authority
review of the full diff:

- **Composition root (the critical deliverable for Lane B):**
  `Infrastructure/DevelopmentControl/DevelopmentControlServiceCollectionExtensions.cs`
  `AddDevelopmentControl` binds the **real** governed stack as host singletons:
  `ExcelDevelopmentControlStore` → `ConcurrencyGuardedDevelopmentControlStore` (the public
  `IDevelopmentControlStore`), `NamedDevelopmentControlWriteLockFactory` as
  `IDevelopmentControlWriteLockFactory`, a shared singleton
  `DevelopmentControlMutexIdentity.FromWorkbookPath(fullPath)`, and
  `DevelopmentControlAtomicWriteCoordinator` over the same factory. Registered in
  `Program.cs` (`AddDevelopmentControl(builder.Configuration, ContentRootPath)` +
  `MapDevelopmentControlEndpoints()`); `appsettings.json` carries
  `DevelopmentControl:WorkbookPath = ../../NEXUS_DEVELOPMENT_CONTROL.xlsx` (content-root →
  repo root), `LockTimeoutSeconds = 10`. A **path-direct overload**
  (`AddDevelopmentControl(path, timeout)`) lets a probe/host bind the same governed stack over a
  disposable workbook — the seam Lane B uses. Lazy singletons: a missing workbook only surfaces
  at first resolve, not host startup.
- **Mutation pipeline is genuinely guarded.** Reserve/Release/Complete handlers build an
  `AtomicWriteRequest` from the guard's `MutexIdentity`/`LockTimeout`, pre-read the node's
  `RowVersion`, and call `ExecuteAtomicWriteAsync` — so verify-while-locked + bounded lock wait are
  real, and a stale write returns ConcurrencyConflict, never a lost update. Reads (state /
  node / active-changes / preflight) pass through the guarded store.
- **Outcome → HTTP mapping is exhaustive** (never an unhandled 500): Success→200,
  ConcurrencyConflict→409, NotFound→404, ValidationFailure→422, InvalidRequest→400,
  LockTimeout/IoFailure→503, plus a controlled defensive default.
- **Boundary honored:** Application handlers inject `IConcurrencyGuardedDevelopmentControlStore`,
  never the raw adapter; zero Forge/DevTools/DevBridge references in added code (only a comment
  noting the future proof).
- **DevelopmentRun lifecycle surface** (Start/Cancel/Succeed/Fail commands/handlers + typed
  `DevelopmentRunNotFoundException`→404, `DevelopmentRunStateException`→409 + 4 POST endpoints +
  extended GET read-model): B1 left Core transitions with no Application surface; Lane A filled the
  gap so a run is drivable through the API. Within the brief's "DevelopmentRun access where
  appropriate"; flagged as a deliberate surface widening, consistent with the exit-goal Q8/Q9.
- **Verification (re-run by the integration authority):** `dotnet build Nexus.Developer.slnx -c
  Release` → 0 warnings / 0 errors; `dotnet test` → **352/352 (327 B1+B2 baseline + 25 new)**.
- **Coverage boundary (documented):** no Api HTTP test host exists, so endpoints are exercised at
  compile + handler + composition-root level against the real guarded store on disposable temp
  workbooks. Acceptable per prep-report; the live HTTP path is exercised by Lane B.
- Compliance: uncommitted working-tree changes only, branch still at base `ffe25e0`; tracked
  `NEXUS_DEVELOPMENT_CONTROL.xlsx` byte-untouched (no entry in `git status`); no
  commits/merges/`git add .`.

### 1.2 Lane D (SP1-D06 — Subchat recursion) — `PASS` (reviewed + independently verified)

Worktree `Nexus-Exp-W3-D06`, branch `sp1-d06-subchat`. Optional nullable `Branch.ParentBranchId` only.
Integration-authority review of the full diff (19 tracked files + migration + 21 tests):

- **Domain** (`Branch.cs`): self-parent rejected in ctor and `ChangeParent`; cycle rejected by
  `ChangeParent(BranchId?, IReadOnlyCollection<BranchId> ancestors)` against a caller-supplied
  ancestor chain.
- **Application**: Create validates parent exists and is same-Conversation; Update reparents only on
  explicit `ParentBranchId` (`null` = leave unchanged, so existing PUTs are byte-compatible) and walks
  the **full persisted ancestor chain** (`CollectAncestorBranchIdsAsync`) before calling `ChangeParent`
  — rejects direct (`A→B` re-parent of B under A) and transitive (`A→B→C→A`) cycles, with corruption
  guards that throw on an already-cyclic or dangling persisted chain rather than passing silently.
- **Persistence**: additive migration `20260907163653_SubchatBranchParent` (nullable `ParentBranchId`,
  `IX_Branch_ParentBranchId`, self-FK `Restrict` — avoids a second cascade path through the
  Conversation→Branch cascade; existing rows stay `NULL` = root, zero backfill). Clean `Down`.
- **API**: Create/Get/List/Put carry optional `Guid? ParentBranchId`.
- **Verification (re-run by the integration authority):** `dotnet build` → 0 errors (only the
  pre-existing `ConversationMessage.cs` CS8618 warning); `dotnet test Nexus.Experience.slnx` → **44/44
  (40 Chat + 4 Architecture)**, 0 failed. Architecture tests confirm no boundary violation.
- **Scope fidelity:** no second Subchat aggregate; Conversation and Branch remain canonical; existing
  create/read/list/update semantics preserved.
- **Known limitations (reported, accepted):** HTTP PUT cannot distinguish absent vs explicit-null
  parent, so clear-to-root is domain-supported (`ChangeParent(null)`, tested) but not exposed over PUT;
  cross-Conversation parent rule is enforced in Application (DB FK enforces existence only). Both are
  deliberate minimal scope.
- Compliance: uncommitted working-tree changes only; no commits/merges/`git add .`; no workbook exists
  or was written in this repo.

### 1.3 Lane B (SP1-M02 — live Forge↔Developer interop proof) — `PASS` (15/15); SP1-M02 FULLY COMPLETE

Built on Lane A's real composition root (`AddDevelopmentControl`, §1.1). Two probes under
`%TEMP%\nexus-sp1-m02-live` — **Developer probe** ProjectReferences
`Nexus-W3-M05\src\Nexus.Developer.Infrastructure` and binds the real `AddDevelopmentControl`; the
**Forge probe** binds the real M02 `DevBridge.Workspace.Locking` library — contended on one Windows OS
named mutex over **disposable** workbook copies (the authoritative workbook was never opened). The
orchestrator (`run-proof.ps1`) drove six cases, all asserted:

| Case | Claim | Live outcome |
|---|---|---|
| 3 | Same canonical path → identical lock identity both sides | dev hash == forge hash == `ABB0153E…BBD0`; object name `NexusDevelopmentControl_`+64 hex identical |
| 4 | Different paths → different identity, both sides agree | PASS — wb2 hash `E78A8D55…` ≠ wb1; dev==forge |
| 1 | **Developer holds** real mutex → **Forge acquire times out** | `Timeout` @ 3006ms (bounded); positive control `Acquired` @ 2ms after release |
| 2 | **Forge holds** → **Developer guarded write `LOCK_TIMEOUT`**, workbook bytes unchanged | wall ~4.2s vs a 2s budget; SHA-256 byte-identical; positive `SUCCESS` after release then mutates bytes |
| 5 | Explicit bounded timeout, no infinite wait | `Timeout` @ 1211ms against a 1200ms request |
| 6 | Failed contention never partially mutates | bytes byte-identical under a blocked guarded write |

**15/15 PASSED.** Developer's guard surfaced contention as `MutationResult.Success = false` +
`ValidationErrors` `LOCK_TIMEOUT` (never a throw); no partial write was ever observed on timeout. This
is the end-to-end proof that the two independently-built stacks (Forge `DevBridge.Workspace.Locking`;
Developer's real composition-root Mutex) genuinely contend on the same OS object. **SP1-M02 is marked
FULLY COMPLETE**, and M02 §9 decision 2 (Mutex-vs-Semaphore) is resolved empirically: Developer's bound
primitive is the named **Mutex**, matching the M02 library's default. Full record + reproducible harness
(re-runnable, preserved at `DevBridge/src/DevBridge.Workspace.Locking.LiveInteropProof/` with captured
`evidence/`) in `DevTools-W2-M02 …/SP1_M02_SHARED_WRITER_LOCK_REPORT.md` §10. No commits/merges/`git add .`.

### 1.4 Lane C (SP1-D03/D04 — Product projection + `Feature.ParentFeatureId`) — D04 `PASS`, D03 `BLOCKED`

Worktree `Nexus-W3-D04`, branch `sp1-d03-d04-product-subfeature` (base `ffe25e0`). Integration-authority
review of the full diff + re-run:

- **D04 implemented and verified.** `Feature` gained optional `FeatureId? ParentFeatureId` (children
  point up; `null` == root), a create-ctor self-parent guard, `SetParent(Feature?)` enforcing
  self-parent and cross-Subproject inside the aggregate, and `Restore` round-trip. New
  `SetFeatureParentCommand/Handler` is the **write boundary** that enforces the D04 hierarchy rules
  needing repository state: parent exists (`FeatureParentNotFoundException`), self rejected, and
  **ancestor-cycle prevention** — the handler walks the candidate parent's ancestor chain and throws
  `FeatureHierarchyCycleException` (with the closing cycle path) if the child is already an ancestor of
  its own ancestor. Multi-level depth is allowed (child-has-no-children is **not** an invariant) — the
  one deliberate, brief-required deviation from the Wave-02 single-level default; schema delta is
  identical (existing rows `NULL` = root, zero backfill). Create-subfeature resolves/validates the
  parent; queries/API DTOs project `ParentFeatureId`; additive EF migration + self-FK `Restrict` +
  `IX_Feature_ParentFeatureId`. No `ProductId` on Feature; no Governance import; no new Subfeature
  aggregate; no generic `WorkRelationship`. **Independent re-run:** build 0/0; **268/268** in the lane
  (248 baseline + 20 new — all new Feature/SetParent/Create tests discovered and green); the deep-cycle
  test genuinely builds `A(root)←B←C` and rejects re-parenting A under C with path `[A,C,B,A]`.
- **D03 remains BLOCKED** (governance blocker, out-of-lane — same AVAILABLE_BLOCKER as Wave-02). No
  projection fabricated against an unreferenceable identity. Re-verified by the integration authority:
  `C:\Personal\LocalNuGet` has **no** `Nexus.Governance.*` nupkg; the `Nexus.Governance.Contracts`
  commit `b21e059` is **NOT** an ancestor of `Nexus.Platform main` (branch-only); no
  product→scope binding exists in code (`grep ProductId` over ProductCore = empty); Developer.Core's
  only package reference is `Nexus.ProductCore.Contracts 0.1.0-*`. Exact unblock steps (merge
  Governance.Contracts to `main`, publish a local nupkg, implement `IProductRegistry` DI, choose the
  product→scope representation, add a product→subprojects query) are in the D03 report §5.
- Compliance: uncommitted working-tree changes only; authoritative workbook untouched; no
  commits/merges/`git add .`.

---

## 2. Lane E — SP1-W05 roadmap/runtime identity bridge (READ + decide) — `HUMAN_DECISION_REQUIRED`

Reported fully in `Nexus-W3-W05/architecture/SP1_W05_ROADMAP_RUNTIME_BRIDGE_REPORT.md` (read in full by the integration authority). Verdict: **HUMAN_DECISION_REQUIRED**, no implementation performed. Evidence-backed finding:

- No aggregate in Nexus.Developer is today originated from a roadmap node through any real creation/import/use path. Creation sources are manual handlers, Chat-conversation conversion, or the one-time Chat `WorkItem`→`Task` Guid migration. Zero hits for roadmap-importer classes; zero `using …DevelopmentControl` references outside DevelopmentControl folders.
- The planned origin seam (`nexus-roadmap.yaml` M-07-1.1 / WI-07-1.1.3 "Roadmap import") is **not built**, and its target mapping is unresolved: roadmap vocabulary (`ProductDevelopment > Module > Feature > Milestone > WorkItem > Task > Subtask`) diverges from the implemented ADR-005 hierarchy (`Workspace > Project > Subproject > Feature > Task > Subtask`, `WorkItem` folded into `Task`).
- Per the strict WU-02 origin test, zero aggregates qualify today. Adding `string? SourceRoadmapNodeId` to `Feature`/`Task` now would be a dormant speculative column whose name, target, and requiredness a future importer could shape differently.
- Lane left a precise implementation manifest (aggregate, EF config/migration, factory-only population, cross-repo naming decision) for a follow-on lane gated on the importer or a Forge bootstrap writer existing.
- Working tree of `sp1-w05-roadmap-runtime-bridge` otherwise clean; baseline build 0/0 warnings/errors, 248/248 tests; nothing committed/staged/pushed; workbook untouched.

**Decision needed from a human (see §4 of the W05 report for the full six-item list):** timing/trigger, target aggregate, field name (must be frozen jointly across Developer + Forge + Platform), value semantics/population rule, consumer contract/read path, and roadmap source-of-truth policy. This lane is **not** a Wave-04 implementation item until a human decides.

---

## 3. Lane F — SP1-C03 consolidation migration manifest — `PASS`

`SP1_C03_NEXUS_MIGRATION_MANIFEST.md` (primary tree, `architecture/`) is complete: five repo records with per-repo (repo, branch, remote, unique branches, worktrees, uncommitted state, move method, path rewrites, verification, rollback) plus cross-cutting prerequisites. Every UNKNOWN disposition C00/C01 flagged was resolved against live git/filesystem evidence: empty directories confirmed empty; `Compilers` confirmed a V1 standalone archive; `Dataverse` an archive; `UserSecrets` never moved as plaintext. **No physical moves performed; no workbook writes.** Lane F output does not touch the code-producing lanes' files and is committed-ready independently.

---

## 4. Lane G — Wave-04 planning section (READ-ONLY) — `COMPLETE`

Read-only design lane. Produces §7 (Wave-04 planning section) of this report. No source, test, config, or workbook file was modified in any repo.

---

## 5. Same-repo integration order (exact, content-derived — not finish time)

*Method: diff each lane against base `ffe25e0`; detect file overlap per repo; order candidate lanes so later lanes layer cleanly onto earlier ones; run combined tests once after the final candidate is in place. Not by wall-clock finish.*

- **Nexus.Experience** — Lane D (Branch.ParentBranchId) is the single code candidate: integrated and
  verified in its own worktree (§1.2, 44/44). No ordering question.
- **Nexus.Developer** — exact content-derived order, computed by intersecting the per-lane file sets
  against the canonical B1+B2 tree (`Nexus-W2-INT`, branch `w02-integration`, 327 tests):
  1. **Layer order: Lane A over B1+B2, then Lane C.** Lane A (`Nexus-W3-M05` = B1+B2+A, 352 tests) is
     already expressed on the B1+B2 working tree, so its **net** (the 69-file set where `Nexus-W3-M05`
     differs from `ffe25e0`) overlays the B1+B2 baseline with zero further conflict. Lane C
     (`Nexus-W3-D04`, pure `ffe25e0`, 268 tests) then overlays. Lane C's Feature-domain files
     (`Feature.cs`, `FeatureConfiguration.cs`, Application/Features, Api/Features, Feature tests,
     `SetFeatureParent*`, three typed exceptions) are **disjoint** from B1+B2 and Lane A — clean copy.
  2. **Two-file overlap needing a merge, both in disjoint regions:** `Application/ServiceCollectionExtensions.cs`
     (Lane C adds `SetFeatureParentHandler` to the B1+B2+A registration list — union by adding one
     `AddScoped` + one `using`) and `Migrations/NexusDeveloperDbContextModelSnapshot.cs` (B1+B2 and
     Lane C both wrote it). The snapshot is not hand-merged: Lane C's EF artifacts (migration `.cs`,
     `.Designer.cs`, snapshot delta) were **excluded** from the overlay and regenerated on the combined
     base, because Lane C's migration was authored against the pure-`ffe25e0` snapshot and would
     otherwise carry a snapshot that silently lacks the B1+B2 model.
  3. **Regeneration (authoritative reconciliation):** `dotnet ef migrations add AddFeatureParentFeatureId`
     (EF Core 10.0.11, design-time factory) on the merged sources produced exactly the one additive
     migration `20260907170618_AddFeatureParentFeatureId` (nullable `ParentFeatureId` + `IX_Feature_ParentFeatureId`
     + self-FK `Restrict`) folded onto the B1+B2 migration history
     (`…_AddDevelopmentRunExecutionFields` → `…_AddReasonToWorkItemDependency`), and
     `dotnet ef migrations has-pending-model-changes` → **"No changes"** after a fresh build (the first
     `--no-build` probe falsely reported drift — stale-assembly artifact; a rebuild cleared it).
  4. **Result candidate:** integration worktree `C:\Personal\Nexus-W3-INT` (branch `w03-integration`, base
     `ffe25e0`), 38 modified tracked + 52 new untracked files, **uncommitted** — the assembled
     B1+B2+A+C candidate. Combined tests in §6.
  - Because Lane C and Lane A touch no common Feature-domain file, Lane A could equally have layered
    onto B1+B2 first or Lane C onto a B1+B2+A tree; the *only* true ordering constraint is that the
    **snapshot/EF regeneration must run last**, after both source sets are present.
- **DevTools-W2-M02** — Lane B deliverable is a report update + preserved proof harness
  (`DevBridge.Workspace.Locking.LiveInteropProof/`, all untracked) — no production source. Single
  candidate → free.
- **Nexus.Developer lineage, non-code:** Lane E (report only), Lane F (manifest in primary
  `architecture/`), Lane G (§7). These never contend with code lanes.

## 6. Combined verification after integration

Run in the assembled candidate `C:\Personal\Nexus-W3-INT` (B1+B2 + Lane A + Lane C):

```
dotnet build Nexus.Developer.slnx -c Release        → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  Nexus.Developer.slnx -c Release --no-build
  → Passed!  Failed: 0, Passed: 372, Skipped: 0, Total: 372, Duration: 4 s
dotnet ef migrations has-pending-model-changes      → No changes (drift-free combined model)
```

- **372/372 combined tests green** (327 B1+B2 baseline + 25 Lane A + 20 Lane C — the exact predicted
  total), across the single Developer solution.
- **No workbook mutation:** authoritative `NEXUS_DEVELOPMENT_CONTROL.xlsx` SHA-256 verified
  `8aa73778…` (§9). Integration never opens it.
- **No architecture/boundary violations:** Lane C adds no ProductId to Feature and imports no
  Governance/Product domain; Lane A adds no Forge reference; the merged solution contains no duplicate
  Product identity, no ProductProfile/Membership/Entitlement, no new Subfeature aggregate.
- **Developer API runnable** and **live writer-lock proof complete** are established at lane level
  (§1.1, §1.3) on the same code now combined here; the Api project compiles in the combined solution.
- Nexus.Experience (Lane D) verified separately: build 0 errors (one pre-existing CS8618 warning),
  **44/44**. DevTools (Lane B) harness: **59/59 M02 + 71/71 M01** preserved; live proof 15/15 (§1.3).

### 6.5 Recommended commits (for the human to apply when authorizing — none were made)

Git discipline forbade automatic commits, so all lane work is uncommitted working-tree changes on each
lane's own branch. When a human authorizes commits, the recommended grouping (each independently
reviewable, ordered by dependency):

- **Nexus.Developer / Lane A** (worktree `Nexus-W3-M05`, branch `sp1-m05-developer-api`): two commits —
  (1) the B1+B2 carry-forward already present in that tree, then (2) the Lane A surface:
  `DevelopmentControl` Application handlers + DI (`AddDevelopmentControl`), the guarded
  Reserve/Release/Complete commands, DevelopmentRun lifecycle commands, Api endpoints + DTOs,
  `Program.cs`/`appsettings.json` wiring, `ControlTestWorkbook`-based tests.
- **Nexus.Developer / Lane C** (worktree `Nexus-W3-D04`, branch `sp1-d03-d04-product-subfeature`):
  `Feature.ParentFeatureId` + config + `SetFeatureParent*` + typed exceptions + Api/DTO projection +
  migration `…_AddFeatureParentFeatureId` + tests (D04 only; D03 carries no code).
- **Nexus.Developer / combined** (integration worktree `Nexus-W3-INT`, branch `w03-integration`): the
  assembled B1+B2+A+C candidate, with the EF artifact as **`20260907170618_AddFeatureParentFeatureId`**
  (regenerated on the combined base; the per-lane migration file is superseded by this one in any
  merged lineage). Recommended commit order if applied as a lineage onto a shared main:
  B1+B2 candidate → Lane A → Lane C (regenerated migration last, so the snapshot stays drift-free).
- **Nexus.Experience / Lane D** (worktree `Nexus-Exp-W3-D06`, branch `sp1-d06-subchat`): one commit —
  `Branch.ParentBranchId` domain + handler + API + migration `…_SubchatBranchParent` + tests.
- **DevTools-W2-M02 / Lane B** (worktree `C:\Personal\DevTools-W2-M02`, branch `sp1-m02-shared-writer-lock`):
  the M02 `DevBridge.Workspace.Locking` library + harness (already delivered in Wave-02), plus this
  wave's report §10 `LIVE_INTEROP_PROOF` and the preserved proof harness
  `DevBridge.Workspace.Locking.LiveInteropProof/` (untracked, kept for reproducibility).

### 6.6 Progress toward the Phase-1 goal (WORKING FORGE · WORKING DEVELOPER · ONE NEXUS · GATE A)

- **WORKING DEVELOPER:** DevelopmentControl is genuinely **runnable** through a real composition root,
  guarded read/mutation surface, exhaustive error mapping — combined 372/372. Substantially advanced.
- **WORKING FORGE → governed interaction:** the cross-process writer lock is **live-proven** (Lane B,
  15/15); Forge and Developer contend safely on one OS named object. Forge's next governed step needs
  the Wave-04 typed-address/resolver increment (exit Q9).
- **ONE NEXUS:** the C03 migration manifest (Lane F) is the exact OLD→D:\NEXUS map; cutover waits on
  human acceptance + a quiet window (exit Q10). Not yet moved (by design).
- **GATE A DEVELOPMENT READY:** the remaining gate inputs are the three Wave-04 shape ratifications and
  the Governance `ProductId` unblock (exit Q5/Q8/Q9). Everything else in the Phase-1 plan is now in a
  reviewed, combined-green, human-reviewable state.

---

## 7. Wave-04 planning section (Lane G output)

**Purpose.** Map only the minimal dependencies that make Wave-04 implementable: `DevelopmentControlAddress`, a *typed* Dependency/Context Resolver, and a Work-Universe-aware Context Resolver that extends Forge's existing modules. This section is **design only** — nothing below was implemented, and nothing below authorizes a Wave-04 start (Wave-04 is not started; see §10).

### 7.1 Frozen direction this section is grounded in

| Authority | Statement |
|---|---|
| `LAYER_MODEL.md` (R06 freeze, lines ~460–540) | Permanent cross-workbook address is `DevelopmentControlAddress { DevelopmentControlRole Role; NodeId Id; }`. `Role` is `Foundation` or `Products`; **never** a routing decision made from a node-ID prefix (prefixes are migration-discovery evidence only). A `DevelopmentRun` is one execution attempt; `ActiveChange` remains the governed envelope; `PreflightDeclaration` the declared scope envelope — these three never collapse. |
| `LAYER_MODEL.md` (lines 492–503) | "Dependency/Context Resolver and Task Resolver … frozen R06." Forge already has real modules under `DevBridge/scripts/ai-routing/` — `DependencyLineage.ps1`, `ContextPackage.ps1`, `TaskClassification.ps1`, `router/*`. The future cross-workbook-aware Context Resolver **extends these, it is not a green-field build**; missing piece is narrow: DevelopmentControlAddress awareness, cross-workbook resolution, task-eligibility composition, general DevelopmentControl integration. A future Task Resolver is a thin composition over existing pieces. |
| `WORK_UNIVERSE.md` §2/§8/§9/§12 (WU-02 freeze) | Product identity owned by 03 GOVERNANCE (`Product` aggregate, `IProductRegistry`, M-03-1.1/1.2); Developer references it, never defines its own. Subfeature = `Feature.ParentFeatureId`. Two identity systems remain, deliberately unmerged; no cross-reference field exists today. Forge consumes the universe; gains **no new context engine**; stays `BOOTSTRAP_SAFE`; resolves "where available": roadmap/work item, runtime work object, Product, Feature/Subfeature, Planning, Chat/turn evidence, `ActiveChange`/Preflight, `DevelopmentRun`, dependencies, repository reality, Assurance evidence, Outcome. `DevelopmentControlAddress` is an **approved SP1 implementation milestone — not part of WU-02 itself**; `WorkObjectAddress` is not introduced until real `DevelopmentControlAddress` usage exists. |
| `CURRENT_STATE.md` (Platform docs) | The two seed workbooks (`.xlsx` / `NEXUS_PRODUCTS_DEVELOPMENT_CONTROL.xlsx`) and `DevelopmentControlAddress` are the intended second-workbook + address scheme. |
| M02 Forge deliverable (`DevTools-W2-M02`) | Cross-process lock identity today is a deterministic **string** identity (`NexusDevelopmentControl_` + UPPERCASE SHA-256 hex) composed by `SharedLockIdentity`, honored by both Forge's PowerShell `NamedObjectWriterLock`/`FileWriterLock` and Developer's C# `NamedDevelopmentControlMutex`. This string identity is the live carrier a typed address must serialize to/from — no new lock protocol is invented in Wave-04. |
| Developer boundary (`AGENTS.md`) | Developer holds `ProductId` but must not import the Governance product-domain assembly / DbContext / db. No API code couples to Forge internals. |

### 7.2 The five Wave-03 consumables Forge must consume in Wave-04

| Consumable | Owned by / exists today | How Forge will consume it in Wave-04 |
|---|---|---|
| `DevelopmentRun` | Developer (B1): real aggregate with lifecycle Start/Succeed/Fail/Cancel, additive migrations; targets runtime Guid ids via `DevelopmentRunTargetType` + `Guid TargetId`. | Read-only, "where available." Forge reads a run's state/result to govern and reason about execution attempts (Work-Universe §9 resolution list includes `DevelopmentRun`). Reachability is **via the Developer application read surface** (the Lane-A runnable API is the seam) or, per BOOTSTRAP_SAFE, degraded to "unavailable" when no Developer process/DB is reachable. Forge does **not** write DevelopmentRun rows directly. |
| `ProductId` | Governance identity (M-03-1.1/1.2), ARCHITECTURE-ONLY-SP1 until implemented; Developer holds the id without importing product types. | Resolved only when a live Governance product registry exists; used to scope Forge's view of a product's work. Absent a registry (BOOTSTRAP_SAFE), resolution returns unavailable — Forge never fabricates a ProductId. |
| `SourceRoadmapNodeId` | **Nothing.** Lane E = HUMAN_DECISION_REQUIRED; field does not exist. | **Not a Wave-04 implementation item.** Per WU-02 §9, Forge must follow the bridge without a live Developer runtime → a persisted column, but the column is gated on the roadmap importer / bootstrap writer (Lane E decision). Wave-04 planning therefore does **not** reserve a column; it records the dependency and the joint naming decision as a prerequisite. |
| `DevelopmentControlRole` | Forge (`DevBridge.Engine`, enum `Foundation`/`Products`) exists today; Developer Core has no Role type. | The `Role` half of every `DevelopmentControlAddress`. Consumed as an **enum, never a prefix-routing basis** (LAYER_MODEL). Developer's own address side must gain a Role without importing Forge types (see 7.3 — shape parity, not coupling). |
| `NodeId` | Developer (`Core/DevelopmentControl/NodeId.cs`): roadmap-ledger string value type (`F-`/`M-`/`WI-`/`T-`/`S-`…). Forge's matching token is `idSpaces.RoadmapNode`. | The `Id` half of every `DevelopmentControlAddress`, plus the cross-workbook resolution key. Wave-04 must state the explicit equivalence `NodeId` ⇄ `idSpaces.RoadmapNode` as the **same string space** (already implied by WU-02 §8; make it a joint test/contract fact, never a merged Guid space). |

### 7.3 Minimal dependency map (read-only design — what Wave-04 will build, not now)

1. **Ratify three frozen shapes first** (one human-visible decision, cross-repo):
   - `DevelopmentControlAddress { DevelopmentControlRole Role; NodeId Id; }` **shape + canonical serialization** — must map 1:1 onto the M02 lock-identity string so a typed address and the live Forge↔Developer lock protocol are the same object viewed twice.
   - The **DCR term** (see 7.4) — scope and name frozen in the Wave-04 spec, not in code.
   - The `NodeId` ⇄ `idSpaces.RoadmapNode` equivalence statement + no-prefix-routing rule recorded as a joint contract.
2. **Developer side (smallest real surface):** introduce `DevelopmentControlAddress` as a Core DevelopmentControl value type where `NodeId` already lives, with a `DevelopmentControlRole` **enum mirror** (Foundation/Products) defined locally so Core never references Forge. Address ↔ lock-identity (de)serialization lives with the existing identity primitive; no change to the 22-op store contract, the guard, or the coordinator. Lane A's read/query surface is the exposure point.
3. **Forge side (extend, never rebuild):** teach `DependencyLineage.ps1` / `ContextPackage.ps1` / `TaskClassification.ps1` / `router/*` to accept a `DevelopmentControlAddress` and resolve it across Foundation/Products **when a Developer store/API is reachable**; stay `BOOTSTRAP_SAFE` (degrade to available-side-only resolution, no fabricated state) when not.
4. **Explicit non-goals for Wave-04 (already frozen):** no `WorkObjectAddress` (revisit only after real `DevelopmentControlAddress` usage — WU-02 §2); no rebuild of DependencyLineage/ContextPackage (extend only — LAYER_MODEL); no `SourceRoadmapNodeId` implementation (Lane E gate); no new context engine (WU-02 §9); no Task Resolver as a large new engine (LAYER_MODEL — it is a thin composition).

### 7.4 "typed DCR" — term flagged, not guessed

The brief's "typed DCR" has **no authoritative expansion anywhere in code or docs.** Evidence gathered:

- `LAYER_MODEL.md` R06 spells the concept out as **"Dependency/Context Resolver and Task Resolver"** but never introduces the bare acronym with a definition.
- Forge/DevBridge source and design comments use "DCR" only loosely and adjacent to lineage/ID/dependency lookup (`ExcelDevelopmentControlProvider.cs`: "real ID/dependency/DCR/lineage lookup is future work"; `IDevelopmentControlProvider.cs`: "DCR linkage, lineage … future work"; the M02/DevBridge harness family references DCR in passing).
- The M00 stabilization report lists "a new DCR" among deliberately-not-implemented items with no expansion.

The only grounded reading consistent with LAYER_MODEL R06 and the DevBridge comments is **DCR = Dependency/Context Resolver**, and "typed" = resolving **strongly-typed** objects (`DevelopmentControlAddress`, `DevelopmentRun`, typed node/work-object references) rather than returning loose/string results. **This is the integration authority's reading, not a ratified term.** Wave-04 must ratify the term, its scope, and whether "typed DCR" is the Work-Universe-aware Context Resolver of 7.3 or a distinct earlier increment before any implementation — the read-only Lane G deliberately did not choose for the wave.

### 7.5 What remains before Forge executes a full governed dev cycle (context for Wave-04 sizing)

*Filled after Lane A (runnable API + real composition root) and Lane B (live interop proof) landed.*

The prerequisite chain is now proven end-to-end up to the Wave-04 items:

- **(a) real cross-process writer lock proven** — Lane B, 15/15 live (§1.3). Forge and Developer
  genuinely contend on one OS named mutex; identity parity byte-for-byte; bounded timeouts; no partial
  mutation. ✅ done
- **(b) Developer read/write surface runnable and guarded** — Lane A (§1.1): real composition root,
  exhaustive outcome mapping, guarded atomic-write pipeline, runnable HTTP surface; combined 372/372
  green (§6). ✅ done
- **(c) typed `DevelopmentControlAddress` + cross-workbook resolver** — NOT yet built; this is the
  Wave-04 increment. Its prerequisites are (i) the three frozen-shape ratifications of §7.3 item 1
  (address shape + lock-identity serialization; the typed-DCR term, §7.4; `NodeId` ⇄
  `idSpaces.RoadmapNode` equivalence), (ii) the Developer-side address/Role value types (small,
  §7.3 item 2), and (iii) the Forge-side extension of `DependencyLineage`/`ContextPackage`/
  `TaskClassification` (§7.3 item 3). ⏳ Wave-04.
- **(d) Forge's first governed mutation of a Products-side workbook** via the shared lock + typed
  address, still `BOOTSTRAP_SAFE` on Foundation — the terminal Wave-04 demonstration. It additionally
  needs the Governance registry decision (§1.4 / exit Q5) so a real `ProductId` can scope a Products
  workbook.

So before Forge executes a **full** governed development cycle, three external/forward items remain:
ratify the three Wave-04 shapes, deliver Wave-04's address + resolver increment, and unblock the
Governance `ProductId` (D03) so Products-side work can be scoped authoritatively. With (a) and (b)
done, the only Forge-internal blocker left is the address/resolver increment itself.

---

## 8. Wave-03 exit questions (answered explicitly)

**1. Can Nexus Developer read DevelopmentControl through its real application path?**
**YES.** Lane A exposes guarded read queries (DevelopmentControl state, node, active changes, preflight)
through the real composition root (`AddDevelopmentControl`) over the genuine guarded store, with
exhaustive 200/404/409/422/400/503 result mapping (§1.1). Exercised on disposable workbooks in
`ControlTestWorkbook`-layout tests; the read path is the same surface a live Developer process binds.

**2. Can Nexus Developer perform a governed mutation through that path?**
**YES.** Reserve/Release/Complete go through the guarded atomic-write pipeline (pre-read RowVersion,
verify-while-locked, bounded lock wait, concurrency conflict on stale writes). Lane B CASE 2's positive
control is a real governed Reserve that **mutated** the disposable workbook bytes only after acquiring
the lock — the guarded mutation path is proven live, not just in-memory.

**3. Does the real writer lock engage?**
**YES.** The M00 `NamedDevelopmentControlWriteLockFactory` (System.Threading.Mutex) is bound through the
real composition root (Lane A), and Lane B proves it engages across processes: Developer holding the
mutex makes Forge's acquire time out; Forge holding it makes Developer's guarded write return
`LOCK_TIMEOUT`. This resolves M02 §9 decision 2 (Mutex confirmed).

**4. Can Forge and Developer safely contend on the same workbook?**
**YES.** Lane B 15/15 live (§1.3): identical lock identity for the same canonical path (CASE 3),
different identity for different paths (CASE 4), Forge-refuses-while-Developer-holds (CASE 1),
Developer-refuses-while-Forge-holds with bytes unchanged (CASE 2), bounded no-infinite-wait (CASE 5),
and no partial mutation on failed contention (CASE 6). SP1-M02 is FULLY COMPLETE.

**5. Can Product/Feature work be represented using authoritative ProductId?**
**NO — not yet; BLOCKED (out-of-lane).** There is no consumable Governance `ProductId`: no
`Nexus.Governance.*` nupkg exists locally and the `Governance.Contracts` commit is not on
`Nexus.Platform main`. D03 was not fabricated against an unreferenceable identity (§1.4). The unblock
needs a Governance owner to merge + publish Contracts and a human L03/L06 decision on the
product→scope representation.

**6. Is Subfeature functional?**
**YES.** `Feature.ParentFeatureId` (children point up), create-subfeature and re-parent write paths,
self-parent forbidden, parent must exist and be same-Subproject, **ancestor-cycle prevention** enforced
at the write boundary, multi-level depth allowed, additive EF migration. Verified 268/268 in the lane and
372/372 combined (§1.4, §6).

**7. Is Subchat functional?**
**YES.** `Branch.ParentBranchId` only — no second Subchat aggregate, Conversation remains canonical,
self-parent and direct/transitive cycles rejected against the full persisted ancestor chain. Verified
44/44 in `Nexus.Experience` (§1.2).

**8. Are we ready for DevelopmentControlAddress + DCR?**
**Mostly — with three ratifications outstanding (Lane G, §7).** The consumables are confirmed:
`DevelopmentRun` (real lifecycle, readable via Lane A's surface), `NodeId` and `DevelopmentControlRole`
(the two halves of every `DevelopmentControlAddress`), `ProductId` (identity only, gated on D03). What is
**not** ready: the `DevelopmentControlAddress` type/serialization, the term "typed DCR" (no authoritative
expansion — §7.4), and the `NodeId` ⇄ `idSpaces.RoadmapNode` equivalence statement. These are human
ratifications (§7.3 item 1), not code, and are the gate to starting Wave-04.

**9. What remains before Forge can execute a full development cycle?**
Three items, now that (a) the writer lock is live-proven and (b) Developer's guarded surface is
runnable/combined-green: (i) ratify the three Wave-04 shapes; (ii) deliver Wave-04's typed
`DevelopmentControlAddress` + cross-workbook resolver increment (Forge extends, never rebuilds,
`DependencyLineage`/`ContextPackage`/`TaskClassification`); (iii) unblock Governance `ProductId`
(D03/exit Q5) so a Products-side workbook can be scoped. Then Forge's first governed mutation of a
Products workbook via the shared lock + address is reachable, still `BOOTSTRAP_SAFE` on Foundation
(§7.5).

**10. When is the safest D:\NEXUS physical cutover point?**
After the C03 migration manifest (Lane F) and the C02 cutover plan are **human-accepted**, and during a
**quiet window with no active interop development in flight** — the manifest is a pure OLD→NEW path map
with verification/rollback per repo and no physical move yet (§3). Recommended sequencing: accept the
combined B1+B2+A+C candidate + this report, then run cutover as a dedicated, scheduled phase (each repo
moved one at a time with its recorded verification command), **not** while Lanes/Wave-04 code is still
landing on the same working trees. There is no wave-internal dependency forcing the move sooner; nothing
in this wave is path-dependent.

---

## 9. Workbook + git-discipline verification

Re-verified live at report time (final pass): authoritative `NEXUS_DEVELOPMENT_CONTROL.xlsx` SHA-256 =
`8AA73778A3F1CDB97D1A58BB05BD4797CA5A409BE8F986EF19FD9EDBB7CFE69F` — **unchanged**. No lane or the
integration authority opened or wrote it; every lane proof/test used disposable copies
(`ControlTestWorkbook` layout or `%TEMP%` scratch workbooks).

Git discipline held end-to-end: **no automatic merges, commits, pushes, or `git add .`** were performed
by any lane or the integration authority. Each lane's work is uncommitted working-tree change on its own
branch (see §6.5 for the recommended, human-authorized commits). Primary tree `C:\Personal\Nexus.Developer`
contains only the pre-existing `M NEXUS_DEVELOPMENT_CONTROL.xlsx` plus untracked Wave reports; the Lane A
(`SP1_M05_DEVELOPER_API_REPORT.md`) and Lane C (`SP1_D03_D04_PRODUCT_SUBFEATURE_REPORT.md`, refreshed over
the stale Wave-02 design copy) records were collected there for review. No lane work leaked staged/committed
into the primary tree. Worktrees + branches in play:

| Worktree | Branch | Contents (all uncommitted) | Tests |
|---|---|---|---|
| `Nexus-W3-M05` | `sp1-m05-developer-api` | B1+B2 + Lane A | 352/352 |
| `Nexus-W3-D04` | `sp1-d03-d04-product-subfeature` | Lane C (D04) | 268/268 |
| `Nexus-W3-INT` | `w03-integration` | **B1+B2 + A + C combined candidate** (38 M / 52 new; regenerated EF) | **372/372** |
| `Nexus-Exp-W3-D06` | `sp1-d06-subchat` | Lane D | 44/44 |
| `Nexus-W2-INT` | `w02-integration` | canonical B1+B2 (Wave-02, human-accepted) | 327/327 |
| `DevTools-W2-M02` | `sp1-m02-shared-writer-lock` | M02 library + harness + §10 LIVE_INTEROP_PROOF + proof harness | 59+71; live 15/15 |

---

## 10. Stop condition

P1-WAVE-03 ends here. **Wave-04 is NOT started.** This report is the wave's terminal deliverable; Lane E's `HUMAN_DECISION_REQUIRED`, Lane G's §7 planning (and the §8 exit-question answers), and the Governance `ProductId` unblock are the inputs a human uses to decide whether/how Wave-04 proceeds. Lanes A, B, C (D04), D, F passed; D03 is BLOCKED (governance); Lane E is `HUMAN_DECISION_REQUIRED`. No commit, merge, push, or `git add .` was performed.
