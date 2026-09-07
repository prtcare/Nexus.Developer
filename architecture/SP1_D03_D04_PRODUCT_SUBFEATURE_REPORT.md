# SP1-D03 (Product work projection) + SP1-D04 (Subfeature hierarchy) — Implementation Record

**Lane C — P1-WAVE-03 (Nexus V2.3 Phase-1).** Supersedes the Wave-02 read-only design of the
same name (authoritative copy: `C:\Personal\Nexus.Developer\architecture\SP1_D03_D04_PRODUCT_SUBFEATURE_REPORT.md`).

| Field | Value |
|---|---|
| Repository | `Nexus.Developer` (worktree `C:\Personal\Nexus-W3-D04`) |
| Branch | `sp1-d03-d04-product-subfeature` |
| Base | `ffe25e0` (`P1-WAVE-01: add parallel wave 01 integration report`) |
| Date | 2026-09-07 |
| Result | **D04 implemented (full). D03 remains BLOCKED.** Nothing committed; workbook untouched. |

---

## 1. Executive summary

- **SP1-D04 (subfeature hierarchy) is implemented** exactly as the Wave-03 brief locked it:
  a Feature may be a child of another Feature in the same Subproject (children point up via a
  nullable `Feature.ParentFeatureId`), with **self-parent forbidden, parent must exist, parent must
  be in the same Subproject, and cycle prevention enforced** (a Feature cannot become an ancestor of
  its own ancestor). **Child-has-no-children is NOT an invariant** — depth is unrestricted
  (multi-level), which is the one deliberate deviation from the Wave-02 single-level default, and is
  required by the brief's explicit rule set and its `cycle (deep) rejected` acceptance test (§4.1).
  Wired Core → Application (create-feature payload + query projection) → API DTOs, plus EF
  persistence mapping and one additive migration. 20 new tests; full suite green (268 = 248 + 20).
- **SP1-D03 (product work projection) is BLOCKED** by the same AVAILABLE_BLOCKER Wave-02 recorded:
  no consumable Governance `Product` identity exists (no `Nexus.Governance.Contracts` package
  anywhere; the commit that added it is not on `Nexus.Platform` `main`) **and** no code binds a
  Governance `Product` to a Product-Core scope node. No projection was fabricated against an
  unreferenceable identity. Evidence and exact unblock steps in §5.

---

## 2. Locked decisions carried forward from Wave-02 (unchanged)

1. `Feature` must **not** gain a `ProductId` (§3.1 of the Wave-02 design). Not added.
2. Developer.Core references only `Nexus.ProductCore.Contracts`; Developer never imports a product
   domain/`DbContext` and holds `SubprojectId` as an opaque Guid. Unchanged.
3. No D03 projection authored inside `Nexus.Developer.Core` / `Nexus.Developer.Infrastructure`.
4. D04 modeled as **children point up** (`ParentFeatureId`), consistent with `Task.FeatureId` /
   `Subtask.TaskId`; no child collection/navigation on `Feature`. Unchanged.
5. No new `Subfeature` aggregate; no generic `WorkRelationship`; no duplicate product identity.

---

## 3. D04 — implemented detail

### 3.1 Domain aggregate (`src\Nexus.Developer.Core\Features\Feature.cs`)

- Added `public FeatureId? ParentFeatureId { get; private set; }` — `null` == root.
- Public create ctor gained a trailing optional `FeatureId? parentFeatureId = null`
  (source-compatible; every existing 6-arg call site is unchanged and produces a root).
  The ctor rejects the degenerate self-parent (`parentFeatureId == id` → `ArgumentException`).
- `Restore(...)` and the private restore ctor gained a trailing optional
  `FeatureId? parentFeatureId = null` so rehydration round-trips the column (EF materializes
  through this constructor; the added parameter binds to the `ParentFeatureId` property).
- New method:
  ```csharp
  public void SetParent(Feature? parent)   // parent == null ⇒ promote to root
  ```
  Guards enforced **inside the aggregate**: self-parent (`parent.Id == Id`) and
  cross-Subproject (`parent.SubprojectId != SubprojectId`) both throw `ArgumentException` before any
  mutation. There is deliberately **no parent-is-root guard** (multi-level). The one structural rule
  that needs repository state — **cycle prevention** — is enforced at the application write boundary
  (the same split `WorkItemDependency` uses: entity rejects degenerate shapes, the handler checks
  the graph before persisting).

### 3.2 Multi-level vs single-level — recorded deviation from Wave-02

Wave-02 §4.1/§4.2 recommended a single-level default with an aggregate `parent-is-root` guard
("a parent feature must itself be a root") and a `HasChildrenAsync` application precondition for
demotion. The Wave-03 brief overrides this:

- *"Child-has-no-children is NOT an invariant (single-level default is an application concern)."*
- *"Cycle prevention required (a feature cannot become an ancestor of its own ancestor)."*
- Acceptance test `cycle (deep) rejected` — which cannot be exercised under strict single-level
  (max depth 2 leaves no deep cycle to reject).

Implementation therefore allows arbitrary depth and enforces **ancestor-cycle prevention** at the
write boundary. Because children point up and no depth is stored, this is the same "relaxation
path" Wave-02 §4.2 anticipated ("purely a guard change … no schema change") — the schema delta is
identical to the Wave-02 single-level design. Existing rows get `NULL` → every current Feature is a
root; zero backfill.

### 3.3 Application layer

New write path that makes cycle prevention real and reachable:

- `src\Nexus.Developer.Application\Features\Commands\SetFeatureParent\`
  - `SetFeatureParentCommand(FeatureId FeatureId, FeatureId? ParentFeatureId)` (`null` = promote).
  - `SetFeatureParentHandler` — loads the child (`FeatureNotFoundException` if absent); on a null
    parent promotes via `SetParent(null)`; otherwise loads the parent
    (`FeatureParentNotFoundException` if absent), rejects self (`ArgumentException`), then runs
    `EnsureNoCycleAsync` (walks `ParentFeatureId` up from the candidate parent through the
    repository; if it reaches the child the re-parent would make the child an ancestor of its own
    ancestor → `FeatureHierarchyCycleException` carrying the closing cycle path, mirroring
    `WorkItemDependencyCycleException`), then applies `child.SetParent(parent)` (aggregate
    cross-Subproject guard) and `UpdateAsync`.
  - `SetFeatureParentResult(FeatureId FeatureId, FeatureId? ParentFeatureId)`.
  - Registered in `AddDeveloperApplication`. The Wave-02 §4.6 **HTTP** surface (`PUT
    /features/{id}/parent`, `GET /features/{id}/children`) stays deferred to Lane D; the Application
    command/handler exists now because it is the required enforcement point for the brief's cycle
    rule and its acceptance test.

Subfeature creation (the brief's primary write path):

- `CreateFeatureCommand` gained trailing optional `FeatureId? ParentFeatureId = null`.
- `CreateFeatureHandler` — after the existing Subproject-exists check, when a parent is supplied it
  resolves the parent through `IFeatureRepository.GetAsync`; missing → `FeatureParentNotFoundException`;
  parent in a different Subproject → `ArgumentException`; then constructs the new Feature with the
  parent id. A brand-new Feature cannot close an ancestor cycle (it has no descendants), so no cycle
  walk is needed on create.

New typed exceptions (`src\Nexus.Developer.Application\Features\`):

- `FeatureNotFoundException` — re-parent target absent.
- `FeatureParentNotFoundException` — supplied parent absent (create-subfeature + re-parent).
- `FeatureHierarchyCycleException` — carries `IReadOnlyList<FeatureId> Path` closing back to start.

Query projections:

- `GetFeatureResult` gained trailing `FeatureId? ParentFeatureId`; `GetFeatureHandler` and
  `ListFeaturesBySubprojectHandler` now map `feature.ParentFeatureId`.

### 3.4 API DTOs / endpoint (`src\Nexus.Developer.Api\Endpoints\Features\`)

- `CreateFeatureRequest` gained `Guid? ParentFeatureId = null`; `FeatureEndpoint` maps it to the
  command and now catches `FeatureParentNotFoundException` and `ArgumentException` → `400` (in
  addition to the existing `SubprojectNotFoundException` → `400`), matching the
  `DependencyEndpoint` convention.
- `GetFeatureResponse` gained `Guid? ParentFeatureId = null`; both GET endpoints
  (`GET /api/v1/features/{id}` and `GET /api/v1/subprojects/{subprojectId}/features`) now return it,
  so callers (including the future D03 projection) can assemble roots + children from the flat list.

### 3.5 Persistence / migration

- `src\Nexus.Developer.Infrastructure\Sql\Configurations\FeatureConfiguration.cs`:
  - `ParentFeatureId` mapped nullable with `StronglyTypedIdConverters.FeatureId`.
  - Self-FK `FK_Feature_ParentFeature` → `OnDelete(DeleteBehavior.Restrict)` (the
    `TaskConfiguration`/`FK_Task_Feature` precedent), giving the DB a parent-exists constraint.
  - Index `IX_Feature_ParentFeatureId`.
- Migration `20260907165020_AddFeatureParentFeatureId` (generated with `dotnet ef` 10.0.11;
  design-time factory `NexusDeveloperDbContextFactory`): adds nullable `ParentFeatureId`
  `uniqueidentifier` to `dev.Feature`, index, and FK (`Restrict`). **Additive**; existing rows are
  `NULL` → all current Features are roots. `.Designer.cs` and `NexusDeveloperDbContextModelSnapshot.cs`
  updated by the tool.

### 3.6 Verification

Exact commands (run in `C:\Personal\Nexus-W3-D04`):

```
dotnet ef migrations add AddFeatureParentFeatureId --project src/Nexus.Developer.Infrastructure --startup-project src/Nexus.Developer.Infrastructure --configuration Release
  → Build succeeded. Done.

dotnet build Nexus.Developer.slnx -c Release --nologo
  → Build succeeded.  0 Warning(s)  0 Error(s)

dotnet test Nexus.Developer.slnx -c Release --no-build --nologo
  → Passed! - Failed: 0, Passed: 268, Skipped: 0, Total: 268, Duration: 4 s
```

Test counts: **248 baseline → 268 after D04 (+20)**.

### 3.7 D04 test manifest

`tests\Nexus.Developer.Core.Tests\FeatureTests.cs` (+10):

- `Create_IsRootByDefault` · `Create_WithParentFeatureId_SetsParent` ·
  `Create_WhenParentFeatureIdEqualsOwnId_Throws` · `Restore_RoundTripsParentFeatureId` ·
  `SetParent_Null_PromotesChildToRoot` · `SetParent_OnRootToNull_IsNoOp` ·
  `SetParent_Self_ThrowsAndDoesNotChange` · `SetParent_ParentInDifferentSubproject_ThrowsAndDoesNotChange` ·
  `SetParent_ValidParent_SetsParentFeatureId` · `SetParent_ToAFeatureThatItselfHasAParent_IsAllowed_MultiLevelHierarchy`

`tests\Nexus.Developer.Core.Tests\CreateFeatureHandlerTests.cs` (+3):

- `Create_UnderExistingParentInSameSubproject_SetsParentFeatureId`
- `Create_WhenParentDoesNotExist_ThrowsAndCreatesNothing` (`FeatureParentNotFoundException`)
- `Create_WhenParentInDifferentSubproject_ThrowsAndCreatesNothing`

`tests\Nexus.Developer.Core.Tests\SetFeatureParentHandlerTests.cs` (new, +7):

- `Reparent_ToAnotherRootInSameSubproject_UpdatesParent` · `PromoteChildToRoot_ClearsParent` ·
  `Reparent_WhenChildDoesNotExist_Throws` · `Reparent_WhenParentDoesNotExist_ThrowsAndDoesNotChange` ·
  `Reparent_Self_ThrowsAndDoesNotChange` · `Reparent_ToParentInDifferentSubproject_ThrowsAndDoesNotChange` ·
  `Reparent_DeepCycle_ThrowsNamingCycleAndDoesNotChange` (A root ← B ← C; re-parent A under C →
  `FeatureHierarchyCycleException`, path `[A, C, B, A]`, A unchanged)

Existing Feature/create behavior is preserved (all 248 baseline tests still pass).

---

## 4. D04 — change manifest (working tree only; nothing committed)

Modified:

- `src\Nexus.Developer.Core\Features\Feature.cs`
- `src\Nexus.Developer.Infrastructure\Sql\Configurations\FeatureConfiguration.cs`
- `src\Nexus.Developer.Infrastructure\Migrations\NexusDeveloperDbContextModelSnapshot.cs`
- `src\Nexus.Developer.Application\Features\Commands\CreateFeature\CreateFeatureCommand.cs`
- `src\Nexus.Developer.Application\Features\Commands\CreateFeature\CreateFeatureHandler.cs`
- `src\Nexus.Developer.Application\Features\Queries\GetFeature\GetFeatureResult.cs`
- `src\Nexus.Developer.Application\Features\Queries\GetFeature\GetFeatureHandler.cs`
- `src\Nexus.Developer.Application\Features\Queries\ListFeaturesBySubproject\ListFeaturesBySubprojectHandler.cs`
- `src\Nexus.Developer.Application\ServiceCollectionExtensions.cs`
- `src\Nexus.Developer.Api\Endpoints\Features\CreateFeatureRequest.cs`
- `src\Nexus.Developer.Api\Endpoints\Features\GetFeatureResponse.cs`
- `src\Nexus.Developer.Api\Endpoints\Features\FeatureEndpoint.cs`
- `tests\Nexus.Developer.Core.Tests\FeatureTests.cs`
- `tests\Nexus.Developer.Core.Tests\CreateFeatureHandlerTests.cs`

New:

- `src\Nexus.Developer.Application\Features\Commands\SetFeatureParent\SetFeatureParentCommand.cs`
- `src\Nexus.Developer.Application\Features\Commands\SetFeatureParent\SetFeatureParentHandler.cs`
- `src\Nexus.Developer.Application\Features\Commands\SetFeatureParent\SetFeatureParentResult.cs`
- `src\Nexus.Developer.Application\Features\FeatureNotFoundException.cs`
- `src\Nexus.Developer.Application\Features\FeatureParentNotFoundException.cs`
- `src\Nexus.Developer.Application\Features\FeatureHierarchyCycleException.cs`
- `src\Nexus.Developer.Infrastructure\Migrations\20260907165020_AddFeatureParentFeatureId.cs`
- `src\Nexus.Developer.Infrastructure\Migrations\20260907165020_AddFeatureParentFeatureId.Designer.cs`
- `tests\Nexus.Developer.Core.Tests\SetFeatureParentHandlerTests.cs`

---

## 5. D03 — verdict: BLOCKED (unchanged from Wave-02)

**No projection was implemented.** The Governance `Product` identity is still not consumable by any
Developer (or composition) component, and no Product→Product-Core-scope binding exists to project
against. Fabricating a projection on an unreferenceable identity was explicitly forbidden by the
brief and remains so.

### 5.1 Evidence (re-verified 2026-09-07)

1. **No Governance package anywhere consumable.** `C:\Personal\LocalNuGet` contains only
   `Nexus.Intelligence.Contracts`, `Nexus.Platform.{Contracts,Core,Identity,Persistence,Providers.*,Tools}`,
   and `Nexus.ProductCore.{Contracts,Scope}` nupkgs. There is **no `Nexus.Governance.*.nupkg`**.
2. **Governance.Contracts code is not on `main`.** On `C:\Personal\Nexus.Platform` (checked out at
   `wave-g-v2-docs-batch03`, HEAD `540beed`), `git merge-base --is-ancestor b21e059 main` →
   **NOT an ancestor**. The commit that added `Nexus.Governance.Contracts` lives only on the
   `wave-g-v2-docs-batch03` branch. No local pack path exists for Governance
   (`pack-local.ps1` targets GitHub Packages; `pack-productcore-local.ps1` covers only Product Core).
3. **No product→scope binding exists in code.** Grep over `Nexus.ProductCore.Scope` and
   `Nexus.ProductCore.Contracts` for `ProductId` returns nothing; `Workspace`/`Project`/`Subproject`
   carry no product/tenant anchor. "List subprojects of a product" is unanswerable in code, so even
   the Developer-side of the projection (which only needs Product-Core subproject identity) is gated.
4. `src\Nexus.Developer.Core\Nexus.Developer.Core.csproj` still has exactly one `PackageReference`:
   `Nexus.ProductCore.Contracts` `0.1.0-*`. No Governance/Product type was referenced.

### 5.2 Exact unblock steps (owner: Governance / cross-layer L03/L06 — outside this lane)

1. **Nexus.Platform / Governance owner:** merge the Governance.Contracts code from
   `wave-g-v2-docs-batch03` (commit `b21e059`) to `main`, and publish a `Nexus.Governance.Contracts`
   nupkg to `C:\Personal\LocalNuGet` (pattern: `pack-productcore-local.ps1`), version `0.1.0-dev.<ts>`.
2. Confirm `Nexus.Governance.Core` implements `IProductRegistry` against a store and registers it in
   the composition root's DI.
3. **Human decision (L03/L06):** choose the product→scope representation — either Product-Core scope
   aggregates gain a product identity / tenant anchor, or the Governance `Product` record gains a
   scope anchor (e.g. a home `WorkspaceId`). This modeling decision currently has no owner.
4. Add a Product-Core query/endpoint: "list subprojects for product" (walk product → workspace →
   projects → subprojects).
5. Build `IProductWorkProjection` at the governed composition boundary (e.g. the product-tenant app)
   consuming Governance.Contracts + the Product-Core query + Developer's existing HTTP
   `features-by-subproject` endpoint (+ the new D04 `ParentFeatureId` for children assembly).

Once steps 1–4 are real, D03 can be implemented as a thin composition-layer service; until then it
is intentionally not started.

---

## 6. Risks & notes

- **R1 (unchanged):** D03's AVAILABLE_BLOCKER is out-of-lane; needs the L03/L06 human modeling
  decision before any code.
- **R2 (deviation, recorded):** multi-level subfeatures vs Wave-02's single-level default. Chosen per
  the Wave-03 brief (child-has-no-children is not an invariant; cycle-deep acceptance test). Relaxed
  vs single-level with **no schema difference**; if single-level is later preferred, the guard is the
  only change (Wave-02 §4.2 relaxation path, inverted).
- **R3:** cycle detection in `SetFeatureParentHandler` walks the parent chain via repeated
  `GetAsync` (O(depth) round-trips). Acceptable for the current write frequency/depth; a future
  Lane-D `ListBySubprojectAsync`-based in-memory walk would reduce it to one query if it ever
  matters.
- **R4:** concurrency on re-parent — two handlers re-parenting the same Feature could race the
  cycle walk. The codebase already assumes single-writer-per-aggregate for other mutations; accepted
  (same as Wave-02 R3).
- **Exception style:** Wave-02 §8(4) suggested `InvalidOperationException` for parenting invariants.
  This implementation uses `ArgumentException` (self-parent, cross-Subproject) to mirror the
  existing `WorkItemDependency` self-loop precedent and to let endpoints map the violation to `400`
  cleanly; missing-parent and cycle use typed exceptions mirroring
  `WorkItemDependencyTargetNotFoundException` / `WorkItemDependencyCycleException`.
- **Workbook:** `NEXUS_DEVELOPMENT_CONTROL.xlsx` was not opened or modified. No Excel test touched
  the authoritative workbook (all Excel tests use disposable temp copies).

## 7. Compliance statement

- **Nothing was committed, pushed, merged, or staged** in this worktree (`git status` shows only
  working-tree modifications + untracked new files; no `git add`/`commit` was run).
- The authoritative `NEXUS_DEVELOPMENT_CONTROL.xlsx` was **not** opened or written.
- Scope held: no ProductProfile/Membership/Entitlement work, no new Subfeature aggregate, no generic
  `WorkRelationship`, no `ProductId` on `Feature`, no D03 projection code.
