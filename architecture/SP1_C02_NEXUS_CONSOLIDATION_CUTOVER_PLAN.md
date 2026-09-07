# SP1-C02 — NEXUS CONSOLIDATION CUTOVER PLAN (governed plan for the D:\NEXUS physical-move wave)

**Wave:** Nexus V2.3 P1-WAVE-02 · LANE E (SP1-C02)
**Author:** Claude (read-only drafting agent) for Durai
**Date:** 2026-09-07
**Mode:** PLAN-ONLY. This lane moves nothing and writes nothing inside any repository. Physical
repository moves are **explicitly out of scope for P1-WAVE-02** and are reserved for a later,
separately-governed Migrate wave. This document is the governed plan that future wave will execute.
It is the only file this lane creates; it lives OUTSIDE every repo at
`C:\Personal\Nexus-w02-drafts\SP1_C02_NEXUS_CONSOLIDATION_CUTOVER_PLAN.md`.

**Authority it folds in:**
- Discovery/classification: `Nexus.Developer/architecture/SP1_C00_C01_NEXUS_CONSOLIDATION_DISCOVERY_REPORT.md` (the C00/C01 report, 2026-09-07).
- Wave-01 integration & locked target state: `Nexus.Developer/architecture/SP1_PARALLEL_WAVE_01_REPORT.md`.
- Governing directive: `D:\NEXUS\Claude Prompt — Nexus Foundation Reset and Migration.md`.

---

## 1. Executive summary

### 1.1 Goals
Produce a dependency-ordered, reversible, verifiable cutover sequence that moves the local active
checkouts of the three in-scope repositories — **Nexus.Developer, Nexus.Platform, DevTools** — onto
the canonical root **`D:\NEXUS`**, under the ONE-NEXUS model:

- **`D:\NEXUS` is the canonical local root.**
- **One authority** per artifact class (one docs/architecture authority, one workbook authority, one
  repo-of-record per product).
- **One active copy per repository** on disk (today DevTools violates this with three worktrees; the
  target restores the invariant).
- **ONE NEXUS does NOT mean monorepo.** The three products keep separate Git repositories, separate
  GitHub remotes, separate CI/release cadence, and separate identity. The cutover changes *local disk
  path*, never *repo identity or remote URL*.

### 1.2 The explicit non-goal (P1-WAVE-02)
**NO physical move happens during P1-WAVE-02.** No `Move-Item`, no `robocopy /MOVE`, no re-clone, no
junction creation, no `.git` surgery, no `C:\Personal` decommission occurs in this wave. P1-WAVE-02
delivers this plan (Lane E) plus the other lanes already running. The **D:\NEXUS cutover is its own
governed wave**, started only after (a) a human approves this plan's open choices (§7), (b) the
P1-WAVE-02 implementation lanes land and their branches merge, and (c) the §6 governance gates are
signed. Every command in §4 is **shape-only**: it is what the future wave runs, not what this lane runs.

### 1.3 Why a separate wave
The discovery report (§13, §17, §19) established that moving a folder with live tooling is not a file
operation: ACTIVE_RUNTIME scripts hard-code `C:\Personal\*`; DevBridge treats a `C:\Personal` workbook
path as canonical; the DevTools repo currently spans three worktrees whose `.git` pointers are absolute;
two repos carry in-flight uncommitted work. A move therefore couples **filesystem relocation** with
**path-reference repair** and **worktree normalization**, which is why the directive mandates a
governed, human-gated migrate wave rather than a one-shot restructure.

---

## 2. Target-state definition (locked by the wave-01 review)

These homes were recommended by C00/C01 (§14/§15) and accepted as the wave-01 target. They are the
end-state this plan sequences toward. Confirmation of the §7 open choices may adjust a slot *name*;
it will not change the model.

### 2.1 Canonical layout at end state

| D:\NEXUS slot | Holds (repo → path) | Notes |
|---|---|---|
| `D:\NEXUS\Platform` | `Nexus.Platform` repo (moved intact with `.git`). Shared libs + **docs authority** + `NEXUS_MASTER_ARCHITECTURE.md` + `nexus-roadmap.yaml`. | Git history already the NexusAI→Platform lineage. Docs stay with the repo unless the §7 Architecture-split choice says otherwise. |
| `D:\NEXUS\Products\Developer` | `Nexus.Developer` repo (moved intact with `.git`), incl. its `NEXUS_DEVELOPMENT_CONTROL.xlsx` + `control\` mirrors + `architecture\` + `docs\`. | Developer product. Workbook *file* travels with the repo in this wave; the workbook *authority* (single → two-workbook schema migration to `D:\NEXUS\DevelopmentControl`) is a **separate** later wave — never conflated with the cutover. |
| `D:\NEXUS\Forge` | `DevTools` repo (moved intact with `.git`) — DevBridge engine/UI/tests/scripts → the "Nexus Forge" home. | Kept as its own repo (`prtcare/DevTools`). After normalization this is the **single** active checkout of the DevTools repo. |
| `D:\NEXUS\DevelopmentControl` | *(already exists)* `NEXUS_FOUNDATION_DEVELOPMENT_CONTROL.xlsx`, `NEXUS_PRODUCTS_DEVELOPMENT_CONTROL.xlsx`. | Target-schema seeds. Not touched by this wave. |
| `D:\NEXUS\Archives` | Not created by this wave. Follow-on archive sweep (C00/C01 §17 Wave 5). | — |

### 2.2 Repository invariants (unchanged by the cutover)

| Invariant | Nexus.Developer | Nexus.Platform | DevTools |
|---|---|---|---|
| Repo-of-record (remote) | `https://github.com/prtcare/Nexus.Developer.git` | `https://github.com/prtcare/Nexus.Platform.git` | `https://github.com/prtcare/DevTools.git` |
| End-state local active path | `D:\NEXUS\Products\Developer` | `D:\NEXUS\Platform` | `D:\NEXUS\Forge` |
| Identity/workbook artifact | `NEXUS_DEVELOPMENT_CONTROL.xlsx` + `control\` (SHA-tracked) | `NEXUS_MASTER_ARCHITECTURE.md`, `nexus-roadmap.yaml`, `docs\` | DevBridge; tag `pre-forge-hardening-20260905` |
| Active copies on disk at end state | exactly 1 (the D:\NEXUS checkout) | exactly 1 | exactly 1 (the D:\NEXUS checkout) |
| All branches/history | travel as refs in the moved `.git` | travel as refs | travel as refs |

Git **remotes are URL-based, not path-based**; a local directory move therefore never rewrites
`origin`. What *does* break on a move is any hard-coded `C:\Personal\*` path (§3.4, §5).

---

## 3. Inventory & gaps (current on-disk reality → target)

All state below re-verified live on 2026-09-07. A cutover wave must **re-verify every SHA and ref at
execution**; this table is the frozen starting picture, and P1-WAVE-02 lane commits will advance it.

### 3.1 Repos sit outside D:\NEXUS today

| Repo | Current checkout(s) | Current HEAD / branch | Clean? |
|---|---|---|---|
| Nexus.Developer | `C:\Personal\Nexus.Developer` (primary, has real `.git`) | `ffe25e0` on `feature/m-08-1-2-ci-pipeline` (tracks origin, in sync) | dirty: `M NEXUS_DEVELOPMENT_CONTROL.xlsx` only |
| Nexus.Developer (lane worktrees) | `C:\Personal\Nexus-W2-M03` → `sp1-m03-developmentrun-p1`; `C:\Personal\Nexus-W2-M04` → `sp1-m04-developmentcontrol-schema` | both `ffe25e0` | clean |
| Nexus.Platform | `C:\Personal\Nexus.Platform` (primary, real `.git`) | `540beed` on `wave-g-v2-docs-batch03` (tracks origin, in sync); `main` is `behind 2` locally | clean |
| DevTools | `C:\Personal\DevTools` (primary, real `.git`, holds worktree admin) | `04ed758` on `main` (tracks origin, in sync) | dirty (~171 status lines: selftest logs + tracked `bin/obj`/`.vs` debris) |
| DevTools (lane worktrees) | `C:\Personal\DevTools-ForgeV2` → `forge-v2-batch02` (`7f3838f`, tracks origin); `C:\Personal\DevTools-W2-M02` → `sp1-m02-shared-writer-lock` (`7f3838f`, local, no upstream) | see left | ForgeV2 dirty (~78 tracked `bin/obj`); W2-M02 clean |

**Concrete deltas vs target:**
1. All three primary checkouts live on `C:\Personal`, none under `D:\NEXUS` (target: all under `D:\NEXUS`).
2. **Nexus.Developer** has **3** on-disk copies (primary + 2 lane worktrees). **DevTools** has **3**
   on-disk copies (primary + 2 lane worktrees). Both violate the "one active copy per repo" invariant;
   the lane worktrees must be resolved (branches merged or retained as refs, trees removed) before or
   during the cutover.
3. **Nexus.Platform** is a clean single copy — lowest-risk move.
4. `D:\NEXUS` today holds only directive prompts, two Development-Control seed workbooks, and a roadmap
   docx (§2.1) — no product code.
5. Dirty state that must be neutralized before any move: Nexus.Developer **workbook** (uncommitted);
   DevTools main + ForgeV2 **tracked build debris** (no `.gitignore`, 151+ tracked `bin|obj` files).
6. Authority copies still duplicated on disk (`C:\Personal\Roadmaps`, `C:\Personal\Documentation`) —
   already demoted by wave-01 (Nexus.Platform is the docs authority) but not yet archived; the cutover
   wave's decommission phase should archive them (or leave to the follow-on archive wave).

### 3.2 Worktree pointer mechanics (why moves are not trivial)
Linked worktrees carry a `.git` **file** whose content is an absolute path into the primary repo's
admin dir, e.g.:
- `DevTools-ForgeV2/.git` → `gitdir: C:/Personal/DevTools/.git/worktrees/DevTools-ForgeV2`
- `DevTools-W2-M02/.git` → `gitdir: C:/Personal/DevTools/.git/worktrees/DevTools-W2-M02`

and `DevTools/.git/worktrees/` registers both. Moving the primary checkout (or any worktree) *alone*
severs these pointers. **The cleanest path to the target is to remove the lane worktrees first
(branches survive as refs in the primary repo), then move the single primary checkout** (§4.3). Moving
worktrees is possible (`git worktree move` + `git worktree repair`) but is the non-default, higher-risk
option reserved for branches that must stay checked out during the cutover.

### 3.3 Nexus.Developer local commit history that must survive
HEAD `ffe25e0` descends from `5913bc5` (SP1-M00 DevelopmentControl concurrency + atomic-write
stabilization) and `2967383` (C00/C01 discovery report) — all committed, all on the feature branch.
Any move method must preserve these SHAs. `main` (`d32aaff`) is `ahead 5, behind 1` of `origin/main` —
reconcile before cutover so no local-only divergence is stranded on a moved path that then gets
decommissioned.

### 3.4 Load-bearing `C:\Personal` references (must be repaired in the same wave as the move)
From C00/C01 §13 — ACTIVE_RUNTIME / ACTIVE_DEV_TOOL / BUILD:
- `Nexus.Developer\src\Nexus.Developer.Client.Legacy\NexusDev.ps1:14` `$DevToolsPath = "C:\Personal\Nexus.Developer\src\Nexus.Developer.Client.Legacy"`; `install-shortcut.ps1`.
- `DevTools\NexusDev.ps1:14` `$DevToolsPath = "C:\Personal\DevTools"`; `DevTools\install-shortcut.ps1`.
- `DevTools\AI-Config\deepcode.ps1:8` and the migrated copy under Nexus.Developer: dot-source `C:\Personal\UserSecrets\Load-Secrets.ps1`.
- `DevTools\DevBridge\design\*` treat `C:\Personal\Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` as the canonical live workbook; DevBridge code embeds the workbook path.
- `Nexus.Platform\samples\Nexus.Platform.SmokeHost\StoreSecretResolver.cs:84-85` verbatim `C:\Personal\Nexus.Intelligence\src\...` and `C:\Personal\Nexus.Int\src\...` (test/smoke scope).
- `Nexus.Developer\tools\local\start-dev.ps1`, `DevTools\start-dev.ps1` launch sibling repos by absolute `C:\Personal\Nexus.*` path.
- `Nexus.Developer\scripts\Import-LegacyDevTools.ps1` default `$SourceRoot = 'C:\Personal\DevTools'`.
- BUILD: `pack-local.ps1`/`pack-productcore-local.ps1` `$feed = 'C:\Personal\LocalNuGet'`; `nuget.config`; csproj comments citing the local feed.

---

## 4. Frozen cutover sequence (for the future Migrate wave — NOT executed now)

Ordered phases; each phase states **preconditions, command shape, verification gate, rollback, and who/what
it blocks**. "Blocks" means: the named downstream phase or worktree cannot start until the gate passes.

### Phase 0 — Freeze & authority pre-flight
- **Preconditions:** Human approves this plan + §7 decisions; P1-WAVE-02 lanes have landed and their
  branches are merged or explicitly carried; no writing worker is mid-lane in any of the three repos.
- **Actions:**
  1. Publish the freeze notice: no commits/pushes/moves to the three repos' `C:\Personal` checkouts without a named owner.
  2. Re-verify and record the live manifest: for each repo, `git rev-parse HEAD`, `git status --porcelain`,
     `git worktree list --porcelain`, `git remote -v`, and workbook SHA (§4.1 gate G0).
  3. Reconcile Nexus.Developer `main` (`ahead 5, behind 1`) and Nexus.Platform `main` (`behind 2`) — merge or push per human decision, so no local-only divergence is stranded.
- **Verification gate G0:** the recorded manifest exactly matches on-disk reality; every HEAD is pushed
  to its origin (or explicitly logged as intentionally local-only); dirty trees are enumerated and owned.
- **Rollback:** none needed (read-only + notice). If a lane must resume, lift the freeze for that repo only.
- **Blocks:** every later phase.

### Phase 1 — Snapshot / backup (byte-identical + SHA)
- **Preconditions:** G0 passed. Editors/IDEs closed against all three checkouts and their worktrees
  (release `.xlsx`, `.suo`, `.vshost`, `dotnet watch` file locks).
- **Command shape (do once, to a dated backup root outside the repos, e.g. `D:\NEXUS\.cutover-backup\20260907\`):**
  - Workbook first (highest-value asset, per directive): `Copy-Item NEXUS_DEVELOPMENT_CONTROL.xlsx <backup>` and record `Get-FileHash -Algorithm SHA256`.
  - Repos: `robocopy "C:\Personal\Nexus.Developer" "<backup>\Nexus.Developer" /E /COPY:DAT /R:1 /W:1` (repeat for Nexus.Platform, DevTools, and each lane worktree). Cross-volume (`C:`→`D:`) move is a copy+delete; the backup is the same copy without the delete.
  - Worktrees: `git -C <repo> worktree list --porcelain > <backup>\worktrees-<repo>.txt`.
  - Push state: `git -C <repo> push` if not already at origin (freeze owner authorizes).
- **Verification gate G1:** `git -C <backupclone> fsck`; compare `git rev-parse HEAD` backup vs source for every branch; workbook SHA in backup == source SHA; backup size/entry count matches source (`robocopy /L` first for a dry-run).
- **Rollback:** abort now, before any delete; source paths are untouched.
- **Blocks:** Phase 2–9 (nothing moves before the snapshot verifies).

### Phase 2 — Worktree normalization (restore "one active copy per repo" on the OLD path, before the move)
- **Preconditions:** G1 passed. The branches behind the lane worktrees are merged/landed or explicitly
  retained-as-refs by human decision (§7). List to normalize:
  - Nexus.Developer lane worktrees `Nexus-W2-M03` (`sp1-m03-developmentrun-p1`), `Nexus-W2-M04` (`sp1-m04-developmentcontrol-schema`).
  - DevTools lane worktrees `DevTools-ForgeV2` (`forge-v2-batch02`), `DevTools-W2-M02` (`sp1-m02-shared-writer-lock`).
- **Command shape (per repo, per lane branch):**
  - Confirm the branch is safe to unmount: `git -C <worktree> status --porcelain` (must be clean or its dirt committed/stashed first — DevTools ForgeV2 dirt is tracked `bin/obj`, stash or discard after human ok).
  - `git -C <primary> worktree remove <worktree-path>` (branch ref remains in the primary repo).
  - For branches still needed: leave the branch in place; do **not** delete it — it travels as a ref in the moved repo.
  - DevTools hygiene (recommended same wave, human-gated): add `.gitignore` for `bin/`, `obj/`, `.vs/`, `logs/selftest/`; `git rm -r --cached` the tracked debris; commit as a hygiene commit.
- **Verification gate G2:** `git -C <primary> worktree list` shows exactly **one** worktree per repo
  (the primary); all desired branches still listed in `git branch`; no `.git/index.lock` in any repo.
- **Rollback:** recreate a worktree from the retained branch ref: `git -C <primary> worktree add <path> <branch>`.
- **Blocks:** Phase 3 (a move with multiple worktrees is the high-risk path and is avoided by design).

### Phase 3 — Physical moves onto D:\NEXUS (primary checkouts only, each `.git` intact)
- **Preconditions:** G2 passed. Destination parent exists (`D:\NEXUS`, `D:\NEXUS\Products`); target path
  absent or empty. Repos are at their frozen, single-checkout state. No process has a handle in the tree.
- **Recommended method (physical directory move preserving `.git`):**
  - Nexus.Platform → `robocopy "C:\Personal\Nexus.Platform" "D:\NEXUS\Platform" /E /MOVE /COPY:DAT /R:1 /W:1`
  - Nexus.Developer → `robocopy "C:\Personal\Nexus.Developer" "D:\NEXUS\Products\Developer" /E /MOVE /COPY:DAT /R:1 /W:1`
  - DevTools → `robocopy "C:\Personal\DevTools" "D:\NEXUS\Forge" /E /MOVE /COPY:DAT /R:1 /W:1`
  - Alternative if a physical move is declined (§7): `git clone` a local copy then verify, but note a
    re-clone **loses local-only state** (unpushed branches, worktree registrations) — physical move is the
    default because it preserves everything including `.git` byte-for-byte.
  - Do this **one repo at a time**, oldest/lowest-risk first: Nexus.Platform → Nexus.Developer → DevTools.
- **Verification gate G3 (per repo, before touching the next):**
  - `git -C <newpath> rev-parse HEAD` == recorded HEAD; `git status --porcelain` matches the frozen manifest.
  - `git -C <newpath> remote -v` still shows the GitHub URL (unchanged).
  - `git -C <newpath> fsck` clean; `git -C <newpath> branch -a` shows all expected refs.
  - Workbook: `Get-FileHash D:\NEXUS\Products\Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` == recorded SHA (byte identity).
  - Build+test on the new path: `dotnet build <newpath>\Nexus.Developer.slnx`, `dotnet test <newpath>\tests\...` (Developer), `dotnet build <newpath>\Nexus.Platform.slnx`, DevTools `dotnet build DevBridge/src/DevBridge.slnx`. **0 errors; test counts match the pre-move run.**
- **Rollback (per repo):** `robocopy "D:\NEXUS\..." "C:\Personal\<name>" /E /MOVE` (reverse), or restore from the Phase-1 backup, then re-run G3.
- **Blocks:** Phase 4 (path-reference repair must run against the moved trees).

### Phase 4 — Path-reference repair in the same wave (ACTIVE_RUNTIME / ACTIVE_DEV_TOOL / BUILD)
- **Preconditions:** G3 passed for all three repos.
- **Command shape:** edit the §3.4 files to the new D:\NEXUS paths. Representative replacements:
  - `Nexus.Developer.Client.Legacy\NexusDev.ps1`, `install-shortcut.ps1` → `D:\NEXUS\Products\Developer\...`
  - `DevTools\NexusDev.ps1` → `D:\NEXUS\Forge`; `DevTools\install-shortcut.ps1`
  - DevBridge workbook path constant / design docs → `D:\NEXUS\Products\Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx`
  - `deepcode.ps1` dot-source → new UserSecrets home (if UserSecrets moves this wave; otherwise leave the `C:\Personal\UserSecrets` path — see §7)
  - `start-dev.ps1` (Developer + DevTools) sibling-launch paths → new slots
  - `scripts\Import-LegacyDevTools.ps1` default `$SourceRoot` → `D:\NEXUS\Forge`
  - BUILD: `pack-*.ps1` `$feed` and `nuget.config` — only if LocalNuGet moves this wave (see §7, §5)
  - Docs/AGENTS/README path tables in each repo (DOC_ONLY, safe to batch)
- **Verification gate G4:** `rg -n "C:\\Personal\\(Nexus\\.Developer|Nexus\\.Platform|DevTools)"` across the three moved trees returns **zero** hits except intentionally-preserved historical/log strings; re-run the full build+test suite per repo.
- **Rollback:** `git -C <newpath> checkout -- <edited tracked files>` (revert the repair commit) — the files were already committed; or restore from Phase-1 backup.
- **Blocks:** Phase 5.

### Phase 5 — Redirect / publish
- **Preconditions:** G4 passed.
- **Actions:** update any launch shortcuts, PATH entries, shell profiles, and the harness/session
  conventions that name the old `C:\Personal` paths; post the new slot map to the governance record and
  the docs authority; if a junction at the old path is desired (§7), create it as the *last* redirect step
  (never before the move verifies).
- **Verification gate G5:** launching `NexusDev.ps1` from the D:\NEXUS checkout resolves the correct tree;
  DevBridge selftest that opens the workbook reports the D:\NEXUS path and a byte-identical read-back.
- **Rollback:** revert the shortcut/profile edits; remove any junction.
- **Blocks:** Phase 6.

### Phase 6 — Decommission old paths (no deletion of history)
- **Preconditions:** G5 passed; the cutover has been exercised (a normal build + one DevBridge write cycle +
  one docs read) for a human-specified soak period (default: no soak — human may set one).
- **Command shape:** rename, do not delete first: `Rename-Item "C:\Personal\Nexus.Developer" "C:\Personal\Nexus.Developer.pre-cutover-20260907"` (repeat for Platform, DevTools). Leave the renamed trees read-only until the follow-on archive wave (C00/C01 §17 Wave 5) moves them to `D:\NEXUS\Archives` or deletes after evidence sign-off. Optionally archive the now-demoted `Roadmaps\`/`Documentation\` copies under the same wave.
- **Verification gate G6:** no tool, script, shortcut, or open process resolves a *live* `C:\Personal\Nexus*`/`C:\Personal\DevTools` primary path; `git worktree list` from the D:\NEXUS checkouts is clean and single; old path is a renamed tombstone.
- **Rollback:** `Rename-Item` back (the tombstones are full copies until archived).
- **Blocks:** closing the cutover wave; the follow-on archive wave.

### Phase 7 — Evidence & wave close
- Record into the wave report: G0–G6 evidence (HEADs, SHAs, build/test logs, workbook hash before/after,
  `rg` zero-hit proof, worktree lists), the §6 decision log, and any deviations. Sign off the STOP.

---

## 5. Compatibility & risk register

| # | Risk | Product(s) | Mitigation |
|---|---|---|---|
| R1 | **Workbook byte-identity / SHA** — DevBridge opens `C:\Personal\Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` as canonical; any byte drift corrupts the control record. Developer workbook is currently dirty. | Developer, DevTools | Phase 1 byte-identical backup + SHA256 manifest (`control/CONTROL_MANIFEST.json` already the convention). Move with `/COPY:DAT` (no compression/transform). Gate G3 re-hashes at destination. The live workbook *content* is set aside/committed before the move (§7 decision); schema migration to `D:\NEXUS\DevelopmentControl` is explicitly a **later** wave, so this wave never rewrites workbook internals. |
| R2 | **Git remotes vs physical path misconception** — remotes are GitHub URLs; a local move does not and must not change `origin`. The real path-sensitivity is `.git/worktrees/*` admin + worktree `.git` files (absolute `gitdir:`). | all | Gate G3 asserts `git remote -v` unchanged. Worktree pointers handled by Phase 2 (remove lane worktrees before the move) so no cross-path pointer repair is needed; if a worktree must survive, use `git worktree move`/`repair`, never a raw directory move of one worktree. |
| R3 | **Worktree registrations** — moving the DevTools primary repo while `DevTools-ForgeV2`/`DevTools-W2-M02` still point at `C:\Personal\DevTools\.git\worktrees\*` severs both. | DevTools (+ Nexus.Developer lane worktrees) | Phase 2 removes lane worktrees first (branches survive as refs). Phase 3 moves a single-checkout repo. G2/G6 prove one active copy. |
| R4 | **Feed / package paths** — `C:\Personal\LocalNuGet` is referenced by `pack-*.ps1`, `nuget.config`, csproj comments as a fallback because GitHub Packages was unreachable (M-08-1.1). If a repo moves and the feed path string is stale, restore/build breaks. | Platform, Developer (build) | LocalNuGet decommission is tracked by M-08-1.1. Cutover default: **do not move LocalNuGet in this wave**; keep it at `C:\Personal\LocalNuGet` and verify GitHub Packages reachable from CI first. If the human chooses to move it, move it to `D:\NEXUS\Infrastructure\LocalNuGet` in the same Phase-4 repair and update every `$feed`/`nuget.config` — gated by a full `dotnet restore` + build. |
| R5 | **Open worktrees / file locks on moved paths** — `.xlsx`, `.suo`, `bin/obj` files, `dotnet watch`, open IDE tabs, and any running DevBridge/Claude session holding a handle make the OS move fail or half-complete. | all | Phase 0 freeze + Phase 1 close-everything precondition; robocopy `/R:1 /W:1` (one retry, then fail loudly); `git worktree list` + check for `.git/index.lock` before moving; never move a tree a worker is writing to. |
| R6 | **Windows path-length (MAX_PATH)** — DevTools already has deep `DevBridge/src/*/bin/Debug/net10.0/ref/...` paths; adding `D:\NEXUS\Products\Developer\...` nesting could push tracked/untracked paths over the limit. | DevTools, Developer | Keep D:\NEXUS slot names short (`Products\Developer`, `Forge`, `Platform` — not `Products\Nexus.Developer`); enable long-path policy (`LongPathsEnabled`) on the machine before the wave; prefer `\\?\`-prefixed robocopy if a path exceeds 260 chars; run a `robocopy /L` dry-run that reports any too-long paths before the real move. |
| R7 | **Authority ambiguity for Intelligence** (Platform L04 AI vs Product) — out of this lane's three-repo scope but referenced by `StoreSecretResolver.cs`. | Platform (test/smoke) | Flagged to the human (§7); the smoke-host resolver paths are test-scope and repaired in Phase 4 regardless of the placement decision. |
| R8 | **Local-only divergence stranded by decommission** — Nexus.Developer `main` ahead 5/behind 1; any unpushed branch on a moved-then-renamed path looks "lost." | Developer | Phase 0 reconciles `main`; Phase 1 pushes/tags every HEAD (tag `pre-dnexus-cutover-20260907`); Phase 6 renames (never deletes) old paths; Phase-1 backup + remote are the safety net. |
| R9 | **`C:\Personal\UserSecrets` + `infra\.env` secrets** referenced by absolute path in `deepcode.ps1` etc. | DevTools, Developer | Not moved as plaintext in this wave. Repair step keeps the dot-source pointing at the existing secrets location, or the human directs a secrets-manager migration (§7); never robocopy secrets into a repo. |

---

## 6. Governance gates & decision log

### 6.1 Gate authority
Per the directive's wave discipline (`Baseline → Discover → Classify → Propose → Human-approve →
Migrate wave → Build → Test → Verify → Evidence`), every phase in §4 is gated. **Durai** is the approving
human for gates and §7 choices. **Claude (architect)** reviews evidence and confirms each gate before the
next phase starts. No phase auto-starts.

| Gate | Phase | Approver | Evidence required |
|---|---|---|---|
| G0 | 0 Freeze | Durai | Live manifest recorded; freeze notice published; `main` divergence reconciled |
| G1 | 1 Snapshot | Claude (review) + Durai | Backup fsck/HEAD/SHA parity, dry-run entry counts |
| G2 | 2 Normalize | Durai | One worktree per repo; all branches retained; `.gitignore` hygiene commit |
| G3 | 3 Move | Claude (review) + Durai | HEAD/SHA/remote/fsck parity at D:\NEXUS; workbook hash; build+test 0-errors at new path |
| G4 | 4 Repair | Claude (review) | `rg` zero-hit proof; build+test green again |
| G5 | 5 Redirect | Durai | Launch + DevBridge selftest against D:\NEXUS paths |
| G6 | 6 Decommission | Durai | Old paths are tombstones; no live resolution; worktree list clean |
| G7 | 7 Close | Durai | Full evidence bundle in the wave report; STOP signed |

### 6.2 Decision log template (the future wave MUST fill this)
| DEC-ID | Date | Owner | Topic (§) | Options considered | Decision | Rationale | Evidence / ref | Status |
|---|---|---|---|---|---|---|---|---|
| SP1-C02-D01 | | | | | | | | OPEN |
| SP1-C02-D02 | | | | | | | | OPEN |
| SP1-C02-D03 | | | | | | | | OPEN |
| … | | | | | | | | |

---

## 7. What a human must confirm (open choices before the cutover wave may start)

1. **Repo-of-record vs D:\NEXUS subfolder clone.** Keep GitHub `prtcare/*` as the repo-of-record and
   treat the D:\NEXUS checkout as the single active local clone (**recommended** — preserves history,
   remotes, CI, and matches ONE-NEXUS-not-monorepo), versus creating a new git root under `D:\NEXUS`.
2. **Nexus.Developer remains the repo of record.** Confirm Nexus.Developer keeps its own repo/remote and
   lands at `D:\NEXUS\Products\Developer` (recommended) and is **not** folded into a monorepo or made a
   bare D:\NEXUS subfolder with a fresh history.
3. **Move method: physical move vs junction/symlink.** Physical directory move with `.git` intact is
   **recommended** (junctions/symlinks confuse git's realpath handling and add an indirection that breaks
   the "one active copy" invariant; cross-volume junctions are a second system to reason about). If a
   junction is wanted at `C:\Personal\<name>` for compatibility, create it only at Phase 5 after the move
   verifies.
4. **Pre-existing dirty workbook.** The live `NEXUS_DEVELOPMENT_CONTROL.xlsx` (dirty, uncommitted) must be
   (a) byte-backed-up + SHA-recorded, and (b) either committed as its own change record or set aside
   unmodified during the cutover. Decide whether the cutover wave may commit it, and confirm it is **not**
   migrated to the `D:\NEXUS\DevelopmentControl` two-workbook schema in this wave (recommended: separate
   schema-migration wave).
5. **DevTools/Forge branch normalization.** Approve merging `forge-v2-batch02` → `main` (per C00/C01 §16
   and wave-01 sequencing), retiring the `sp1-m02-shared-writer-lock` lane branch after P1-WAVE-02,
   adding `.gitignore` + untracking `bin/obj`, and tagging a Forge baseline before the move.
6. **Nexus.Developer lane worktrees.** Approve removing `Nexus-W2-M03` (`sp1-m03-developmentrun-p1`) and
   `Nexus-W2-M04` (`sp1-m04-developmentcontrol-schema`) once their P1-WAVE-02 lanes land.
7. **Docs/architecture authority home.** Confirm architecture/roadmap authority stays in the moved
   `Nexus.Platform` repo (recommended) versus a new `D:\NEXUS\Architecture` git repo — this determines
   whether Phase 4 moves any docs files between repos (not just paths).
8. **LocalNuGet & UserSecrets.** Whether LocalNuGet moves this wave or waits for M-08-1.1 (recommended:
   waits), and how `C:\Personal\UserSecrets` is handled (recommended: not moved as plaintext; direct Phase
   4 to keep the existing dot-source or to a secrets manager).
9. **Soak period.** Whether Phase 6 needs a human-set soak (default: none) before old paths are renamed.
10. **Archive scope.** Confirm the demoted `Roadmaps\`/`Documentation\` copies and the old-path tombstones
    are archived by the follow-on archive wave, not deleted by this cutover wave.

---

### Appendix — refs frozen at drafting (re-verify at execution)
- Nexus.Developer: HEAD `ffe25e0` (`feature/m-08-1-2-ci-pipeline`), ancestors `2967383` (C00/C01 report), `5913bc5` (SP1-M00); dirty `NEXUS_DEVELOPMENT_CONTROL.xlsx`; worktrees `C:\Personal\Nexus-W2-M03` (`sp1-m03-developmentrun-p1`), `C:\Personal\Nexus-W2-M04` (`sp1-m04-developmentcontrol-schema`).
- Nexus.Platform: HEAD `540beed` (`wave-g-v2-docs-batch03`), clean; `main` `610fc46` (behind 2).
- DevTools (repo): primary `C:\Personal\DevTools` HEAD `04ed758` (`main`, dirty); worktrees `C:\Personal\DevTools-ForgeV2` (`forge-v2-batch02` `7f3838f`, dirty) and `C:\Personal\DevTools-W2-M02` (`sp1-m02-shared-writer-lock` `7f3838f`, clean).
- `D:\NEXUS` today: directive prompts, `DevelopmentControl\{NEXUS_FOUNDATION_DEVELOPMENT_CONTROL.xlsx, NEXUS_PRODUCTS_DEVELOPMENT_CONTROL.xlsx}`, `Nexus_Foundation_Roadmap.docx`, `Nexus Azure Environment Architecture Update.md`, `Nexus Phase and Forge Architecture Update Prompt.md`.
