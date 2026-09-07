# SP1-M05 — Developer DevelopmentControl Application/API Wiring Report (Lane A)

| | |
|---|---|
| **Repo** | `C:\Personal\Nexus.Developer` (worktree: `C:\Personal\Nexus-W3-M05`) |
| **Branch** | `sp1-m05-developer-api` |
| **Base** | `ffe25e0` (P1-WAVE-01 parallel wave 01 report) + uncommitted combined SP1-M03+SP1-M04 (B1+B2) candidate preserved untouched underneath |
| **Date** | 2026-09-07 |
| **Status** | Working-tree change only — **no commit, no push, no merge** |
| **Baseline tests** | 327 passing |
| **After tests** | **352 passing** (327 + 25 new), 0 failed |

---

## 1. Purpose

Make DevelopmentControl runnable through the real Developer application path and bind the
SP1-M00 writer lock / atomic-write coordinator through the real host composition root
(`Program.cs`), so a later cross-process Forge ↔ Developer governed proof has one
deterministic writer-lock contract to stand on. This is **Lane A** of the SP1-M05
Developer API lane set.

Nothing in this lane references Forge / DevTools / DevBridge assemblies. Developer exposes
its own governed surface (Core → Application → Infrastructure → Api) under the existing
`NexusDeveloperBoundaryGuard`.

---

## 2. Prep-report claims verified against the tree

The SP1-M05 prep report (`architecture/SP1_M05_DEVELOPER_API_PREP_REPORT.md`, main repo)
was checked before and during the work:

| Prep-report claim | Verified |
|---|---|
| No DevelopmentControl type is registered in any host composition root | **Correct.** `AddDeveloperInfrastructure` registers DbContext / Sql repos / HTTP clients only; no `AddDevelopmentControl` existed anywhere. |
| SP1-M00 delivered `NamedDevelopmentControlWriteLockFactory` in **Core**, so no new factory is needed | **Correct.** The concrete factory lives in `Nexus.Developer.Core/DevelopmentControl/DevelopmentControlWriteLock.cs`, implementing `IDevelopmentControlWriteLockFactory`. Lane A only registers it. |
| B1 (SP1-M03) delivered DevelopmentRun **Core lifecycle transitions** but left the Application/API surface at Create + Get only | **Correct.** `DevelopmentRun.Start/Cancel/Succeed/Fail` existed on the aggregate with no Application command handlers and no lifecycle endpoints. |
| Api test host (Microsoft.AspNetCore.Mvc.Testing) may not resolve; fall back to handler-level tests in the existing test project and state so | **Correct.** There is no Api.Tests project and the Api project is not referenced by any test project. Used the handler/composition-root fallback (see §6). |

No prep-report claim was found to be wrong.

---

## 3. What existed vs. what Lane A added

### Already present (SP1-M00 / B1 / B2, base candidate — untouched by this lane)
- Core `IDevelopmentControlStore` 22-op contract + DTOs; `NodeId` string identity.
- SP1-M00 concurrency/atomic-write layer: `IDevelopmentControlWriteLockFactory`,
  `NamedDevelopmentControlWriteLockFactory`, `ConcurrencyGuardedDevelopmentControlStore`,
  `IConcurrencyGuardedDevelopmentControlStore`, `DevelopmentControlAtomicWriteCoordinator`,
  `AtomicWriteRequest`/`AtomicWriteResult`/`DevelopmentControlConcurrencyOutcome`,
  `DevelopmentControlMutexIdentity`.
- Infrastructure `ExcelDevelopmentControlStore` (atomic temp-write → validate → promote) and
  the schema validator.
- B1 DevelopmentRun Core lifecycle (`Start/Cancel/Succeed/Fail`, worker/session/result fields).
- B1/B2 test additions in the working tree (DevelopmentRun lifecycle, node phase,
  workbook-schema, dependency tests).

### Added by Lane A (this lane)
- **Infrastructure composition root** `DevelopmentControlServiceCollectionExtensions.cs`
  (`AddDevelopmentControl`, both the `IConfiguration` overload and the direct-path overload).
- **Application DevelopmentControl surface**: 4 read query/handler pairs (State, Node,
  Active Changes, Preflight) and 3 governed mutation command/handler pairs (Reserve,
  Release, Complete), all over the guarded store via `ExecuteAtomicWriteAsync`.
- **Api DevelopmentControl surface**: minimal grouped contracts + one endpoint static class
  (`/api/v1/development-control/...`), mapping every guarded outcome to a real HTTP status
  (never an unhandled 500).
- **Application DevelopmentRun lifecycle surface** (B1 left only Core transitions):
  4 command/handler pairs (Start, Cancel, Succeed, Fail) + typed exceptions +
  `DevelopmentRunLifecycleResult`; read model extended with execution-session fields.
- **Api DevelopmentRun lifecycle surface**: 4 POST lifecycle endpoints + request/lifecycle
  response contracts + `RunLifecycleAsync` 404/409 translation; GET response extended with
  execution-session fields.
- **Program.cs wiring**: `AddDevelopmentControl(builder.Configuration,
  builder.Environment.ContentRootPath)` + `MapDevelopmentControlEndpoints()`.
- **appsettings.json**: `DevelopmentControl:WorkbookPath` + `LockTimeoutSeconds`.
- **25 new tests** (see §6).

---

## 4. Composition-root binding (the critical deliverable)

`src/Nexus.Developer.Infrastructure/DevelopmentControl/DevelopmentControlServiceCollectionExtensions.cs`
binds the full governed stack for host lifetime. All registrations are lazy
(`AddSingleton` factories) so a missing workbook only surfaces when a store/coordinator is
first resolved, never at host startup.

```
ExcelDevelopmentControlStore (singleton, over the canonical full path)
  └─> IConcurrencyGuardedDevelopmentControlStore  = ConcurrencyGuardedDevelopmentControlStore
        (inner: ExcelDevelopmentControlStore, lockFactory, DevelopmentControlMutexIdentity, timeout)
  └─> IDevelopmentControlStore                    = the guarded store (public read/write surface)
IDevelopmentControlWriteLockFactory                = NamedDevelopmentControlWriteLockFactory (Core)
DevelopmentControlMutexIdentity (singleton)        = DevelopmentControlMutexIdentity.FromWorkbookPath(fullPath)
IDevelopmentControlAtomicWriteCoordinator          = DevelopmentControlAtomicWriteCoordinator(inner, lockFactory)
```

Config (host): `"DevelopmentControl": { "WorkbookPath": "../../NEXUS_DEVELOPMENT_CONTROL.xlsx",
"LockTimeoutSeconds": 10 }`. A relative `WorkbookPath` is resolved against the content root
passed by `Program.cs`. The mutex identity is derived from the canonical full path
(`MutexNamePrefix "NexusDevelopmentControl_"` + SHA-256), so **any process binding the same
canonical path contends on the same kernel object** — that is the cross-process writer-lock
contract a future Forge ↔ Developer proof relies on.

Registered in `Program.cs`:

```csharp
builder.Services.AddDevelopmentControl(builder.Configuration, builder.Environment.ContentRootPath);
...
app.MapDevelopmentControlEndpoints();
```

---

## 5. File manifest (Lane A additions/edits)

Absolute root: `C:\Personal\Nexus-W3-M05`

### Added — Infrastructure
- `src/Nexus.Developer.Infrastructure/DevelopmentControl/DevelopmentControlServiceCollectionExtensions.cs` — composition root.

### Added — Application (DevelopmentControl)
- `src/Nexus.Developer.Application/DevelopmentControl/AtomicWriteResultFactory.cs`
- `.../DevelopmentControl/Queries/GetDevelopmentControlState/{GetDevelopmentControlStateQuery.cs,GetDevelopmentControlStateHandler.cs}`
- `.../DevelopmentControl/Queries/GetDevelopmentControlNode/{GetDevelopmentControlNodeQuery.cs,GetDevelopmentControlNodeHandler.cs}`
- `.../DevelopmentControl/Queries/GetActiveDevelopmentChanges/{GetActiveDevelopmentChangesQuery.cs,GetActiveDevelopmentChangesHandler.cs}`
- `.../DevelopmentControl/Queries/RunDevelopmentControlPreflight/{RunDevelopmentControlPreflightQuery.cs,RunDevelopmentControlPreflightHandler.cs}`
- `.../DevelopmentControl/Commands/ReserveDevelopmentControlWorkItem/{ReserveDevelopmentControlWorkItemCommand.cs,ReserveDevelopmentControlWorkItemHandler.cs}`
- `.../DevelopmentControl/Commands/ReleaseDevelopmentControlReservation/{ReleaseDevelopmentControlReservationCommand.cs,ReleaseDevelopmentControlReservationHandler.cs}`
- `.../DevelopmentControl/Commands/CompleteDevelopmentControlWorkItem/{CompleteDevelopmentControlWorkItemCommand.cs,CompleteDevelopmentControlWorkItemHandler.cs}`

### Added — Application (DevelopmentRun lifecycle)
- `.../DevelopmentRuns/DevelopmentRunNotFoundException.cs`
- `.../DevelopmentRuns/DevelopmentRunStateException.cs`
- `.../DevelopmentRuns/DevelopmentRunLifecycleResult.cs`
- `.../DevelopmentRuns/Commands/StartDevelopmentRun/{StartDevelopmentRunCommand.cs,StartDevelopmentRunHandler.cs}`
- `.../DevelopmentRuns/Commands/CancelDevelopmentRun/{CancelDevelopmentRunCommand.cs,CancelDevelopmentRunHandler.cs}`
- `.../DevelopmentRuns/Commands/SucceedDevelopmentRun/{SucceedDevelopmentRunCommand.cs,SucceedDevelopmentRunHandler.cs}`
- `.../DevelopmentRuns/Commands/FailDevelopmentRun/{FailDevelopmentRunCommand.cs,FailDevelopmentRunHandler.cs}`

### Added — Api
- `src/Nexus.Developer.Api/Endpoints/DevelopmentControl/DevelopmentControlContracts.cs`
- `src/Nexus.Developer.Api/Endpoints/DevelopmentControl/DevelopmentControlEndpoint.cs`
- `src/Nexus.Developer.Api/Endpoints/DevelopmentRuns/DevelopmentRunLifecycleContract.cs`

### Added — Tests
- `tests/Nexus.Developer.Core.Tests/DevelopmentControlApplicationTests.cs` (15 facts)
- `tests/Nexus.Developer.Core.Tests/DevelopmentRunLifecycleHandlerTests.cs` (10 facts)

### Edited
- `src/Nexus.Developer.Api/Program.cs` — composition-root call + endpoint mapping.
- `src/Nexus.Developer.Api/appsettings.json` — `DevelopmentControl` config section.
- `src/Nexus.Developer.Application/ServiceCollectionExtensions.cs` — 11 new `AddScoped`
  handler registrations (7 DevelopmentControl + 4 DevelopmentRun lifecycle).
- `src/Nexus.Developer.Api/Endpoints/DevelopmentRuns/DevelopmentRunEndpoint.cs` —
  4 lifecycle POSTs + `RunLifecycleAsync` 404/409 helper; GET extended with session fields.
- `src/Nexus.Developer.Api/Endpoints/DevelopmentRuns/GetDevelopmentRunResponse.cs` — extended.
- `src/Nexus.Developer.Application/DevelopmentRuns/Queries/GetDevelopmentRun/GetDevelopmentRunResult.cs` — extended.
- `src/Nexus.Developer.Application/DevelopmentRuns/Queries/GetDevelopmentRun/GetDevelopmentRunHandler.cs` — maps new fields.

---

## 6. Tests

No Api.Tests project exists and the Api project is not referenced by the test project
(prep-report fallback confirmed). Coverage is therefore at handler + composition-root level,
which exercises the **real** guarded store + **real** Excel adapter over disposable temp
workbooks (never the live workbook).

- **Composition root (3):** DI resolves guarded store = `IDevelopmentControlStore`,
  lock factory, coordinator, deterministic identity; a governed write through the
  DI-resolved store persists; missing `DevelopmentControl:WorkbookPath` throws
  `InvalidOperationException`.
- **Read queries (5):** zeroed ControlState on empty workbook; node read by id; unknown node
  → null; empty open-change register; preflight Clear with no open changes.
- **Governed mutations (7):** reserve (node → InProgress, open change row w/ worker,
  branch, worktree); release (row → "Released -- ..."); complete (node → Completed, change
  closed, notes carry evidence); unknown node → NotFound; malformed/blank node id and blank
  ChangeId → InvalidRequest; blank evidence → InvalidRequest.
- **DevelopmentRun lifecycle (10):** Start stamps worker + StartedAt + persists; Start on
  InProgress → StateException; Start unknown → NotFoundException (carrying the id); Succeed
  after Start completes with the caller summary (CompletedAt ≥ StartedAt); Succeed on a
  Completed run → StateException; Cancel on NotStarted → standard summary; Cancel custom
  summary recorded; Fail after Start → Failed with reason; Fail on NotStarted →
  StateException; Get maps WorkerId/WorkerType/StartedAt onto the read model.

Run command and result:

```
dotnet test Nexus.Developer.slnx -c Release
Passed!  - Failed: 0, Passed: 352, Skipped: 0, Total: 352
```

Build (full solution): `dotnet build Nexus.Developer.slnx -c Release` → 0 warnings, 0 errors.

---

## 7. Wired API surface (how a caller triggers a governed write)

Config (appsettings.json): set `DevelopmentControl:WorkbookPath` to the canonical workbook
(relative paths resolve against the content root) and optionally `LockTimeoutSeconds`
(default 10).

DevelopmentControl endpoints (`/api/v1/development-control/...`):
- `GET  /state` — ControlState snapshot.
- `GET  /active-changes` — open change register.
- `GET  /nodes/{nodeId}` — one current node.
- `POST /preflight` — mandatory preflight declaration → single verdict.
- `POST /nodes/{nodeId}/reserve` — governed reserve (ChangeId + actor + branch/worktree).
- `POST /nodes/{nodeId}/release-reservation` — governed release.
- `POST /nodes/{nodeId}/complete` — governed complete (ResultOrEvidence required).

Outcome → HTTP mapping (DevelopmentControlEndpoint): Success → 200; ConcurrencyConflict →
**409**; NotFound → 404; ValidationFailure → **422**; InvalidRequest → 400;
LockTimeout / IoFailure → **503**; anything unexpected → 500 (explicit `Results.Json` default,
never an unhandled exception).

DevelopmentRun endpoints (`/api/v1/development-runs/{id:guid}/...`): `POST start|cancel|succeed|fail`.
Unknown run → 404; illegal transition for current status → 409; blank worker id / blank
required summary → 400. GET returns the extended run read model.

Example governed reserve (HTTP):
```
POST /api/v1/development-control/nodes/WI-07-2.1.1/reserve
{ "changeId": "CHG-20260907-XXXXXX", "actorName": "claude", "branch": "feature/x", "worktree": "w3" }
→ 200 { "outcome": 1, "node": { ...status: InProgress, rowVersion: n+1 } }
```

---

## 8. Decisions and assumptions

1. **Singleton (host-lifetime) registrations.** Reads/writes open the workbook fresh per op;
   the guarded store serializes writes behind the named lock, and the identity/coordinator
   must be shared. A scoped Excel store would break the singleton lock contract, so the whole
   governed stack is singleton.
2. **Mutation handlers use `ExecuteAtomicWriteAsync`**, not the bare 22-op surface, so a
   caller receives the full `AtomicWriteResult` + `DevelopmentControlConcurrencyOutcome`
   vocabulary and the guard performs verify-while-locked.
3. **Handler pre-reads the current RowVersion** to seed `ExpectedRowVersion`; the guard
   re-verifies inside the lock. A node changed between read and write surfaces as a
   controlled ConcurrencyConflict (409), never a lost update.
4. **Application handlers inject `IConcurrencyGuardedDevelopmentControlStore`** (the guarded
   decorator), not the raw inner adapter, and never the concrete Excel store.
5. **No MediatR, no large REST surface.** One handler class per operation, scoped DI, plain
   static endpoint mappers — mirroring the existing conventions.
6. **DevelopmentRun lifecycle is genuinely missing from B1's Application surface**, so four
   thin command handlers were added (each: load → transition → persist → return lifecycle
   read model). Blank-input validation stays at the Api boundary (400); aggregate
   `InvalidOperationException` is translated to `DevelopmentRunStateException` → 409.
7. **No Api test host** — handler + composition-root tests in the existing test project (see §6).

---

## 9. Risks, caveats, deviations

- **Mutex thread-affinity (documented SP1-M00 caveat, unchanged).** Guarded writes run
  synchronously under the lock (no `await` between acquire/release), so `System.Threading.Mutex`
  ownership never leaves the acquiring thread. The Excel adapter executes synchronously under
  `Task.FromResult`, so this does not block in practice.
- **Workbook file locking.** A reader that opens the workbook with a share-exclusion could
  contend with the guarded writer's atomic replace and surface as an IoFailure (503). This is
  the pre-existing adapter boundary, surfaced as a controlled outcome, not changed here.
- **Release does not transition the node** (by SP1-M00/B1 design): the open change row is
  marked `Released -- ...`; the node stays at its post-reserve status. The Complete path
  marks the node Completed and closes the change in the same atomic save.
- **Coverage boundary (deviation from an ideal WebApplicationFactory test).** Endpoint
  handlers themselves are exercised only by compile + the shared Application handler tests,
  because no Api test host is present. Noted per the prep-report fallback.
- **Workbook never authored through any code path added here.** Every Lane A test writes
  only to disposable temp workbooks under `%TEMP%`; the tracked
  `NEXUS_DEVELOPMENT_CONTROL.xlsx` is byte-untouched (verified via `git status`).

---

## 10. Compliance statement

- **No commits, no push, no merge, no `git add`.** All work is uncommitted working-tree
  change on branch `sp1-m05-developer-api` at base `ffe25e0`.
- The pre-existing uncommitted SP1-M03+SP1-M04 (B1+B2) candidate was preserved exactly;
  Lane A additions layer on top.
- **No reference to Forge / DevTools / DevBridge assemblies** anywhere in the added code.
- The authoritative `NEXUS_DEVELOPMENT_CONTROL.xlsx` was **never modified** by this lane.
