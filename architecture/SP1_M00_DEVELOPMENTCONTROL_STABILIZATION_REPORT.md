# SP1-M00 — Development Control Stabilization Report

**Batch:** SP1-M00 (stabilize existing Nexus.Developer DevelopmentControl work)
**Date:** 2026-09-07
**Governance:** Nexus V2.3 architecture (Developer = Product; read-only-until-report stabilization milestone)
**Author:** Claude (agent) for Durai, under CHG-20260830-013/014/015 governing work items
**Milestone guard:** Report + final diff only. **No commit, no push, staging remains empty.** SP1-M01 not started.

---

## 1. Starting git baseline

| Fact | Value |
|---|---|
| Branch | `feature/m-08-1-2-ci-pipeline` (12 commits ahead of `origin`) |
| HEAD | `ea39db9` — `CHG-20260830-015: workbook v3.26 - WI-07-0.2.2 verified clean, marked Complete; M-07-0.2 at 20%` |
| Local `main` | `d32aaff` |
| Staging | empty |
| Tracked modified | `NEXUS_DEVELOPMENT_CONTROL.xlsx` (in-flight workbook revision, present at session start) |
| Untracked (start) | 13 DevelopmentControl source files (10 Core + 3 Infrastructure) — the in-flight WI-07-0.2.x batch |
| Untracked (added by M00) | 6 test files |

The working-copy workbook carries an uncommitted in-flight revision newer than the committed `v3.26` (the governing M-07-0.2 / WI-07 activity). SP1-M00 left that workbook byte-for-byte as found (see §12).

## 2. Existing in-flight file inventory

All in-flight work is the **Development Control** feature governed by WI-07-0.2.x, an Excel-backed control store over `NEXUS_DEVELOPMENT_CONTROL.xlsx`. It was uncommitted because the governing work item (WI-07-0.2.4 concurrency / atomic-write layer) was mid-flight when SP1-M00 began.

**In-flight, uncommitted at start (13 files):**

| File | Project | Role |
|---|---|---|
| `AtomicWriteResult.cs` | Core | Classification layer mapping `MutationResult<T>` → `DevelopmentControlConcurrencyOutcome` |
| `DevelopmentControlConcurrencyOutcome.cs` | Core | Machine-readable outcome vocabulary (Success / Conflict / LockTimeout / IoFailure / ValidationFailure / InvalidRequest / NotFound) |
| `DevelopmentControlMutexIdentity.cs` | Core | Deterministic named-object identity (SHA-256 of canonicalized store identity) |
| `DevelopmentControlWriteLock.cs` | Core | Lock outcome + held-lock contract (`DevelopmentControlLockAttempt`, `IDevelopmentControlWriteLock`) |
| `IDevelopmentControlWriteLockFactory.cs` | Core | Lock-factory abstraction + attempt result |
| `NamedDevelopmentControlMutex.cs` | Core | Cross-process writer lock over `System.Threading.Mutex` |
| `SemaphoreDevelopmentControlWriteLock.cs` | Core | Cross-process writer lock over named `Semaphore` (any-thread release) |
| `IDevelopmentControlAtomicWorkUnitRunner.cs` | Core | Single-save atomic work-unit capability marker + entry point |
| `DevelopmentControlAtomicWriteCoordinator.cs` | Core | Default coordinator: acquire → verify → run → controlled result |
| `ConcurrencyGuardedDevelopmentControlStore.cs` | Core | Guard decorator over the 22-op store contract |
| `DevelopmentControlCellCodec.cs` | Infrastructure | Cell codecs / vocabulary maps / key generation |
| `ExcelWorkbookColumnMap.cs` | Infrastructure | Name-based column resolution over governed sheets |
| `ExcelDevelopmentControlStore.cs` | Infrastructure | ClosedXML adapter (open / mutate / temp-save / validate / atomic replace) |

**No other source tree was in flight.** Tracked DevelopmentControl types that pre-date SP1-M00 (the WI-07-0.2.1/0.2.2 model: `Node`, `IDevelopmentControlStore`, `MutationEnvelope`, `MutationResult`, `Preflight*`, `ActiveChange`, `ActivityLogEntry`, `AuditFinding`, codec-adjacent schema validator, activity-log migration) were already committed and were **not** modified by SP1-M00 — only read for signature verification.

## 3. Existing intent reconstructed

The Development Control feature governs the workbook: a roadmap of typed `Node`s (Milestone / Work Item), changed through optimistic mutations (`MutationEnvelope` with `ExpectedRowVersion` → `MutationResult<T>`), versioned in Version History, with an Active Changes register, an audit trail of Activity Log entries, and a preflight gate.

The **in-flight WI-07-0.2.4 intent** (the part SP1-M00 stabilized) is a concurrency + atomicity layer layered on that domain:

1. **Cross-process mutual exclusion** over a governed store — deterministic named-object identity derived from the store identity (a workbook path), so two processes over the same workbook contend on the same kernel object.
2. **A guard decorator** serializing all guarded writers behind that lock, re-verifying the optimistic `ExpectedRowVersion` against authoritative persisted state *while the lock is held* so a stale writer never reaches the inner store, and surfacing a bounded lock wait as a controlled `LOCK_TIMEOUT` result (never an exception).
3. **An atomic single-save work unit** — a runner-capable store executes N mutations against one opened in-memory workbook and promotes it with one temp-write → re-validate → atomic-replace; any failed operation aborts with no temp save and no promote.

SP1-M00 was explicitly **not** asked to (and did not) add: DevelopmentControlAddress, Product identity, Subfeature, Subchat, Outcome entity, Work Universe links, Git Workspace Tool, two-workbook migration, a new DCR, Context Resolver expansion, or Release/Deployment shapes.

## 4. DeepSeek usage / results

**`DEEPSEEK_NOT_AVAILABLE`.** No DeepSeek CLI wrapper, API configuration, or assistant hook exists in the repository, environment, or prior session tooling for this project. Per the milestone brief, an AI-routing system was **not** built to compensate. All inventory, review, stabilization, and test work was performed by Claude under Nexus governance.

## 5. Claude review findings (pre-stabilization)

Findings from the M00-5/M00-6 read of the in-flight code, each resolved before the report:

| # | Finding | Resolution |
|---|---|---|
| F1 | `DevelopmentControlMutexIdentity.NormalizeIdentity` treats any identity containing `/`, `\`, or a file extension as a filesystem path (`Path.GetFullPath`). Test fixtures using URI-ish identities (`test://…`) would silently path-normalize into garbage on Windows. | Tests now use slash-free `test-store-<purpose>-<guid>` tokens. Behavior itself is intentional and documented. |
| F2 | `System.Threading.Mutex` is thread-affine: `ReleaseMutex` must run on the acquiring thread. An `await` between acquire and release would strand ownership. | Guard runs the inner write **synchronously** under the held lock (documented in `ConcurrencyGuardedDevelopmentControlStore`); the semaphore variant exists for genuinely-async critical sections. |
| F3 | `WorkbookSchemaValidator.Validate(IXLWorkbook)` enforces **sheet presence only**; a missing *column* does not throw at validate time and instead surfaces later as a failed mutation. | Not a defect — the disposable-workbook test fixture reproduces full headers so exercised paths are realistic; behavior documented. |
| F4 | xUnit analyzer debt in planned tests (xUnit1031 blocking task ops; CS8618; use of a non-existent `MutationResult.Conflict` semantics on `AtomicWriteResult`). | Corrected in the M00 test files (async-aware tests; nullable init; `ConflictDetails` assertion). |
| F5 | Fixture property named `Path` shadowed `System.IO.Path`, breaking `Path.GetTempPath()/Combine`. | Renamed to `FilePath` throughout the Excel test fixture. |

## 6. Build failures before

The in-flight batch did not form a complete, compilable, tested unit at M00 start:

- **No tests existed** for the concurrency/atomic-write layer or the Excel adapter (0 coverage for 13 files).
- The intended concurrency *control flow* (verify-while-locked, LOCK_TIMEOUT classification, work-unit routing, atomic-result mapping) was specified in code comments and contracts but not proven end-to-end by any executable check.
- Test-authoring against the layer surfaced several real compile/semantic mismatches (see F4/F5 and §10) — an indication the layer's consumer contract had not yet been exercised.

## 7. Changes made (SP1-M00)

Changes were confined to **stabilizing existing DevelopmentControl intent**; no tracked production file was edited and no new V2.3 feature was introduced.

- **Stabilized / completed the in-flight concurrency layer in place** (the 13 files of §2): finished the coordinator's seven-step guarded sequence, the guard's 22-op pass-through + guarded engine + atomic-write entry, deterministic lock identity, both named-object primitives, the atomic-result classification, and the Excel adapter's single-save atomic promote path. All method bodies now compile and the intended control flow is wired to the contracts.
- **Corrected** the test-surface mismatches in F4/F5 and the identity fixture hazard in F1.
- **Added 49 tests across 6 new files** exercising the stabilized layer (see §10).
- **No change** to the workbook schema, the committed domain model, the Activity Log 34-column layout, the schema validator's gate, or the dependency graph model.

## 8. Concurrency / atomicity verification

Verified by code reading plus dedicated tests (`DevelopmentControlWriteLockTests`, `DevelopmentControlMutexIdentityTests`, `ConcurrencyGuardedDevelopmentControlStoreTests`, `DevelopmentControlAtomicWriteCoordinatorTests`, and the Excel adapter's atomic-save tests):

- **Deterministic identity:** `DevelopmentControlMutexIdentity.FromStoreIdentity` canonicalizes (trim; path-aware full-path normalization on `/` `\` or extension; case-fold on Windows) then maps to `NexusDevelopmentControl_` + uppercase SHA-256. The same governed store reached through different casing/separator spellings yields the **same** kernel-object name; no absolute path leaks into the object name.
- **Bounded waits, controlled failures:** `NamedDevelopmentControlMutex.TryAcquire` / `Semaphore…TryAcquire` return `DevelopmentControlLockAttempt` with `Acquired` / `Timeout` / `AbandonedRecovered` / `SystemFailure` and an `Elapsed` measurement. The guard and coordinator translate failure into `LOCK_TIMEOUT` (mutation-level message or structured `AtomicWriteResult`), **never a throw**. Verified against genuinely-held kernel objects on a background thread.
- **Verify-while-locked:** the guard re-reads the authoritative `RowVersion` *after* acquiring the writer lock and *before* invoking the inner write; a stale or unknown node aborts with `Conflict` / NotFound and the inner store is never touched (`MutatingCalls == 0` asserted).
- **Work-unit routing:** a runner-capable inner store receives the whole unit via `ExecuteAtomicWorkUnitAsync` (single save); a plain store keeps the generic verify + per-operation path.
- **Excel adapter atomicity:** mutations run against one in-memory workbook; on success the unit is saved to a random temp file **in the same directory**, re-opened through the same schema gate, and promoted with `File.Move(temp, canonical, overwrite: true)`. Any failing operation or structural-invalid temp deletes the temp and leaves the canonical workbook untouched — verified by byte-identical-file assertions in tests (conflict / duplicate-create / rollback-on-second-op / precondition-stale cases).
- **Deliberately not implemented:** the SP1-M02 Forge/Developer *shared* lock protocol. The lock identity prefix is owned by Nexus.Developer alone; no cross-product protocol was introduced.

## 9. Dependency model verification

The Development Control dependency surface and the pre-existing work-item dependency aggregate are disjoint and both intact:

- `Node.Dependencies` is a typed `NodeId` set resolved from the workbook's "Depends On" columns; the Excel store converts declared dependency strings via `IReadOnlyList<string>` → `NodeId[]`.
- The SQL-backed `WorkItemDependency` + `DependencyGraphTraversal` (CHG-20260830-011) are a separate aggregate and were **not** touched; their tests (`WorkItemDependencyTests`, `DependencyGraphTraversalTests`) still pass in the full run.
- **No `WorkItemDependency.Reason` field was added** (explicitly out of scope).
- Full suite §11 confirms both models coexist under one build with no warnings.

## 10. Tests added / fixed

Six new test files (**+49 tests**), all in `tests/Nexus.Developer.Core.Tests`:

| File | Tests | Covers |
|---|---|---|
| `DevelopmentControlMutexIdentityTests.cs` | 7 | Deterministic identity; path normalization; distinct identities; mutex-name derivation |
| `DevelopmentControlWriteLockTests.cs` | 8 | Named mutex + semaphore acquire/release/idempotent release/timeout/distinct identity; factory kinds |
| `ConcurrencyGuardedDevelopmentControlStoreTests.cs` | 8 | Guard pass-through, matching/stale/unknown writes, LOCK_TIMEOUT, atomic-write success/timeout, exposed identity/timeout/inner |
| `DevelopmentControlAtomicWriteCoordinatorTests.cs` | 6 | Coordinator verify/run/routing/timeout/kind against fake + runner stores |
| `AtomicWriteResultTests.cs` | 6 | `FromMutation` classification across all terminal states; `LockTimeout`/`IoFailure` factories |
| `ExcelDevelopmentControlStoreTests.cs` | 14 | End-to-end adapter: empty state, create/read, optimistic update, stale-conflict byte-identical, duplicate-create, retire, reserve-conflict, next-executable, preflight, atomic multi-create work unit, rollback-on-failure byte-identical, precondition stale, ctor missing-file |

Test fixes applied during M00: xUnit1031 async-conversion for blocking/thread-affine tests; CS8618 nullable-init; `Assert.Null(result.ConflictDetails)` (no `.Conflict` on `AtomicWriteResult`); `Path`→`FilePath` fixture rename; slash-free test identities; `IReadOnlyList<string>` dependency conversion; corrected hierarchy-path assertion.

## 11. Final build / test results

```
dotnet build Nexus.Developer.slnx -c Release   → Build succeeded. 0 Warning(s) / 0 Error(s)   (5 projects, 5.54 s)
dotnet test  Nexus.Developer.slnx -c Release   → Passed! Failed: 0, Passed: 248, Skipped: 0, Total: 248  (5 s)
```

- Baseline before M00-8 was **199 passed**; final is **248 passed / 0 failed / 0 warnings**.
- No production code was modified after the final full run; these numbers are the end state.

## 12. Workbook safety

The live workbook was verified **byte-identical before and after the entire M00 run**, so no test or stabilization step mutated the governed artifact:

```
SHA-256  8aa73778a3f1cdb97d1a58bb05bd4797ca5a409be8f986ef19fd9edbb7cfe69f
Size     441,893 bytes
```

- All Excel integration tests run against **disposable temp copies** (`ExcelTestWorkbook : IDisposable` fixture): a temp directory is created per test, a `NEXUS_DEVELOPMENT_CONTROL.xlsx` with all six governed sheets is written there, and the temp directory is deleted on dispose. The fixture comment states tests never touch the real workbook.
- The workbook's committed `v3.26` vs. working-copy delta is the **pre-existing in-flight revision** that was present at M00 start; SP1-M00 neither advanced nor reverted it.
- Workbook schema was not modified (the schema validator's sheet-presence gate and the Activity Log 34-column layout are unchanged).

## 13. Architecture conformance (V2.3)

Verified by project-reference inspection, per-file `using` audit, assembly-reference gate, and keyword scans:

| Rule | Result | Evidence |
|---|---|---|
| Layered references (no Core→Infra, no Api→Core) | ✅ | Api → Application+Infrastructure; Application → Core; Infrastructure → Core; Tests → Core+Application+Infrastructure. Core references only `Nexus.ProductCore.Contracts` (product-scope kernel pkg, pre-existing). |
| Core DevelopmentControl = zero-I/O | ✅ | Every `src/…/Core/DevelopmentControl/*.cs` has **0** references to `System.IO`/`ClosedXML`/EF/SQL/Infra/Application (only BCL `Security.Cryptography`, `Text`, `Diagnostics` for the lock layer; `Path` string helpers via implicit `System.IO`, no file handles). `DevelopmentControlZeroIoTests` asserts the Core assembly references no Excel/OpenXML/EF/SQL assembly — passes. |
| Infrastructure owns persistence/assurance | ✅ | `ExcelDevelopmentControlStore`, codec, column map, schema validator, activity-log migration all live in Infrastructure; each references only `Core.DevelopmentControl` + its I/O library. |
| No Forge runtime dependency | ✅ | Keyword scan for `Forge|Nexus.Forge|SharedPlatform|Nexus.Shared` across all `.cs`: zero matches. |
| No Shared Platform shortcut | ✅ | Locks are OS-level named kernel objects owned by Nexus.Developer; no shared-platform service referenced. |
| No Work Universe duplicates | ✅ | No `WorkUniverse` type/link introduced; Development Control remains the workbook-governing model. |
| No DevelopmentRun drift | ✅ | No DevelopmentRun type added to DevelopmentControl; pre-existing DevelopmentRun feature untouched. |
| No Assurance authority moved | ✅ | Schema gate + activity-log migration remain in Infrastructure/DevelopmentControl; validation behavior unchanged. |
| In-scope keywords only | ✅ | Scan of the 13 files for `WorkUniverse|DevelopmentRun|Subfeature|Subchat|ContextResolver|GitWorkspace` and the "Outcome/Release" words returns only lock-release comments, `ReleaseReservationAsync`, and the pre-existing `NodeType.Release` forward-compat enum value. |

**Verdict: conformant.** SP1-M00 added no V2.3 features, moved no authority, and introduced no dependency or ownership violation.

## 14. Remaining known limitations

1. **Mutex thread-affinity** constrains the named-mutex path: the guard executes guarded writes synchronously under the lock, so a genuinely asynchronous inner store would need the semaphore variant or a dedicated lock-holder thread. Documented in code; acceptable for the synchronous Excel adapter.
2. **Schema gate is sheet-presence only.** A dropped *column* is not caught at validate time; it surfaces later as a failed mutation (mapped through `WorksheetColumnMap.Required()` → controlled failure). Not changed under SP1-M00.
3. **Identity path heuristic:** any store identity containing `/`, `\`, or an extension is assumed to be a filesystem path and full-path-normalized. Works for the Excel adapter (path identity); non-path store identities must be slash-free tokens.
4. **Per-mutation / per-unit workbook save** (full reopen + temp-write + replace) is correct and atomic but not a high-throughput store. Fine for interactive development-governance writes.
5. **SP1-M02 shared lock protocol, Git Workspace Tool, DevelopmentRun expansion, API wiring, BOOTSTRAP_SAFE extraction** were all deliberately **out of scope** and remain unimplemented (see §17 for readiness assessment only).

## 15. Exact changed files (final diff)

No tracked production file was modified. Staging is empty. The complete working-tree delta:

**A. In-flight DevelopmentControl files stabilized by SP1-M00 (untracked, 13)**
`src/Nexus.Developer.Core/DevelopmentControl/`: `AtomicWriteResult.cs`, `ConcurrencyGuardedDevelopmentControlStore.cs`, `DevelopmentControlAtomicWriteCoordinator.cs`, `DevelopmentControlConcurrencyOutcome.cs`, `DevelopmentControlMutexIdentity.cs`, `DevelopmentControlWriteLock.cs`, `IDevelopmentControlAtomicWorkUnitRunner.cs`, `IDevelopmentControlWriteLockFactory.cs`, `NamedDevelopmentControlMutex.cs`, `SemaphoreDevelopmentControlWriteLock.cs`
`src/Nexus.Developer.Infrastructure/DevelopmentControl/`: `DevelopmentControlCellCodec.cs`, `ExcelDevelopmentControlStore.cs`, `ExcelWorkbookColumnMap.cs`

**B. M00-added test files (untracked, 6)**
`tests/Nexus.Developer.Core.Tests/`: `DevelopmentControlMutexIdentityTests.cs`, `DevelopmentControlWriteLockTests.cs`, `ConcurrencyGuardedDevelopmentControlStoreTests.cs`, `DevelopmentControlAtomicWriteCoordinatorTests.cs`, `AtomicWriteResultTests.cs`, `ExcelDevelopmentControlStoreTests.cs`

**C. Pre-existing tracked modification (untouched by M00)**
`NEXUS_DEVELOPMENT_CONTROL.xlsx` (in-flight workbook revision; byte-identical across the M00 run — §12)

**Classification: no unrelated changes.** Everything present is either the governing in-flight DevelopmentControl work or M00 test additions for it.

## 16. Recommended commit scope

Not executed — provided for the governing change process. Suggested as **one change** under the WI-07-0.2.4 / M-07-0.2 lineage (e.g. `CHG-<date>-NNN`):

1. **The 13 stabilized source files** (A) as the DevelopmentControl concurrency + atomic-write layer.
2. **The 6 test files** (B) in the same change (they exist only to prove A).
3. **The workbook revision** (C) as a follow-up governed workbook-version change under the same work item, so the code and the workbook's governing state land in the canonical two-step rhythm (code change → workbook verified-clean commit) used by commits `05e0432`/`4a601d6`/`4c81b1b`/`ea39db9`.

Commit message shape to match history: `CHG-<date>-<seq>: WI-07-0.2.4 DevelopmentControl concurrency + atomic-write stabilization; +49 tests; SP1-M00 (workbook untouched)`.

## 17. Stability for onward work (readiness only — not implemented)

DevelopmentControl is now **build-clean, warning-free, and covered by 49 focused tests plus the pre-existing suite (248 total)**, with the workbook proven untouched. Readiness per the milestone brief:

- **Git Workspace Tool integration** — *Prerequisite shape present, integration not started.* The adapter exposes the workbook path and a single-save atomic promote + the guard's bounded writer lock, which is the serialization contract such a tool would contend on. SP1-M00 deliberately added no Git Workspace Tool code.
- **Shared writer-lock work (SP1-M02)** — *Not started, by explicit instruction.* The lock factory + deterministic identity abstraction exists and is Nexus-Developer-owned; extending the object-name namespace to a Forge/Developer shared protocol is a separate governed design and was **not** pre-empted.
- **DevelopmentRun expansion** — *Not started.* The DevelopmentRun feature and Development Control remain independent; nothing in the stabilized layer blocks or implies expansion.
- **API wiring** — *Not started.* No endpoint/DI registration for the control store was added. The Core contracts and Infrastructure adapter are in a state that makes future wiring straightforward.
- **Later BOOTSTRAP_SAFE extraction** — *Readiness improved.* The Core layer is zero-I/O and assembly-gated, so the DevelopmentControl contract types remain cleanly extractable; no extraction was performed.

**Bottom line:** SP1-M00 achieved its charter — the existing in-flight DevelopmentControl concurrency/atomic-write intent is stabilized, integrated with its contracts, proven by tests, and conformant with V2.3 — without a single commit, push, workbook mutation, or new-V2.3-feature start.
