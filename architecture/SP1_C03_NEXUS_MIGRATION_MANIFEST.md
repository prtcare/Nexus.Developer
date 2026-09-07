# SP1-C03 — NEXUS D:\NEXUS MIGRATION MANIFEST (pre-cutover, Wave-03)

**Lane:** F · **Wave:** P1-WAVE-03 · **Mode:** read-only / manifest preparation. **NO physical repository moves occur.**
**Author:** Claude (integration authority). **Date:** 2026-09-07.
**Inputs:** SP1-C00+C01 discovery report, SP1-C02 frozen cutover plan (8-phase, gates G0–G7). This manifest is the **exact record** the C02 §4 phases consume at execution time; it supersedes C02 §3.1/§Appendix stale snapshots with live re-verified state.

**Hard rule carried:** not one `Move-Item`, `robocopy /MOVE`, re-clone, junction, or `.git` surgery happens in this wave. Every OLD → NEW record below is **preparation** for the human-gated Migrate wave (C02 §4/§6/§7).

---

## 1. Scope: active repositories in the ONE-NEXUS ecosystem

Active = a git repo that is a **current working product/authority** (C00/C01 §15 `CURRENT`), i.e. it will need a D:\NEXUS slot in the end state. Repos whose primary checkout is under `C:\Personal` today:

| # | Repo-of-record (remote) | Primary checkout (live) | End-state slot (C02 §2.1) |
|---|---|---|---|
| 1 | `prtcare/Nexus.Platform` | `C:\Personal\Nexus.Platform` | `D:\NEXUS\Platform` |
| 2 | `prtcare/Nexus.Developer` | `C:\Personal\Nexus.Developer` | `D:\NEXUS\Products\Developer` |
| 3 | `prtcare/Nexus.Experience` | `C:\Personal\Nexus.Experience` | `D:\NEXUS\Products\Experience` (C02 §2.1 lists Products\Developer + Chat product → confirmed by discovery §14 `Products\Experience (or Chat)`; final slot name is a §7 decision) |
| 4 | `prtcare/DevTools` (→ Nexus Forge) | `C:\Personal\DevTools` | `D:\NEXUS\Forge` |
| 5 | `prtcare/Nexus.Intelligence` | `C:\Personal\Nexus.Intelligence` | **Platform/Product seam — HUMAN DECISION** (discovery §14/§20). Not sequenced by C02 §2.1. |

Every record below gives: repo · current branch · remote · unique branches · worktrees · uncommitted state · move method · required path rewrites · verification command · rollback command.

**Workbook authority (separate, never conflated):** live workbook `Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` (SHA below) travels with the repo file-wise in the cutover; the single→two-workbook schema migration to `D:\NEXUS\DevelopmentControl` is a **later** governed wave (C02 §2.1 note, R1).

---

## 2. Record 1 — Nexus.Platform

| Field | Value (live, re-verified 2026-09-07) |
|---|---|
| Remote | `https://github.com/prtcare/Nexus.Platform.git` |
| Current branch | `wave-g-v2-docs-batch03` @ `540beed` — **in sync** with `origin/wave-g-v2-docs-batch03` (0 ahead / 0 behind) |
| Other local branches | `main` (local `behind 2` vs `origin/main`), `chore/M-08-1.1-productcore-feed-publish` (local, tracks origin — confirmed remote has it) |
| Unique local-only branches | none (all three exist on origin) |
| Worktrees | **1** (primary only) — `git worktree list` → `C:/Personal/Nexus.Platform [wave-g-v2-docs-batch03]` only |
| Uncommitted state | **clean** (`git status --porcelain` = 0) |
| Identity/artifacts | `NEXUS_MASTER_ARCHITECTURE.md`, `nexus-roadmap.yaml`, `docs\` — the docs/architecture authority |
| Move method | Physical directory move with `.git` intact (C02 §4 Phase 3 default): `robocopy "C:\Personal\Nexus.Platform" "D:\NEXUS\Platform" /E /MOVE /COPY:DAT /R:1 /W:1` |
| Path rewrites | Primary load-bearing references in THIS repo: `StoreSecretResolver.cs` hard-coded `C:\Personal\Nexus.Intelligence\...` + `C:\Personal\Nexus.Int\...` secret paths (ACTIVE_RUNTIME, discovery §13); `pack-*.ps1` `$feed = 'C:\Personal\LocalNuGet'` (**only if** LocalNuGet moves — default: does NOT move this wave, C02 §7/§5 R4); nuget.config local feed references; DOC_ONLY path tables |
| Verification | `git -C D:\NEXUS\Platform rev-parse HEAD` = `540beed`; `git -C D:\NEXUS\Platform status --porcelain` empty; `git -C D:\NEXUS\Platform remote -v` unchanged; `git -C D:\NEXUS\Platform fsck` clean; `dotnet build D:\NEXUS\Platform\Nexus.Platform.slnx` 0 errors (C02 G3) |
| Rollback | `robocopy "D:\NEXUS\Platform" "C:\Personal\Nexus.Platform" /E /MOVE /COPY:DAT /R:1 /W:1`, or restore Phase-1 backup; re-run G3 |

**Risk noted:** `main` is behind origin by 2 — reconcile before freeze (C02 §7 + G0) so no local-only divergence strands on a moved-then-renamed path.

---

## 3. Record 2 — Nexus.Developer

| Field | Value (live, re-verified 2026-09-07) |
|---|---|
| Remote | `https://github.com/prtcare/Nexus.Developer.git` |
| Current branch | `feature/m-08-1-2-ci-pipeline` @ `ffe25e0` — **in sync** with origin (0/0) |
| Other local branches | `main` (**local `ahead 5, behind 1`** vs `origin/main` — must reconcile), `feature/cors-and-health-endpoint`, `feature/launchsettings-dev-api`; **lane branches (all @ `ffe25e0`):** `sp1-m03-developmentrun-p1`, `sp1-m04-developmentcontrol-schema`, `sp1-m05-developer-api`, `sp1-d03-d04-product-subfeature`, `sp1-w05-roadmap-runtime-bridge`, `w02-integration` |
| Unique local-only branches | ALL lane branches are local-only (no origin equivalent): `sp1-m03-developmentrun-p1`, `sp1-m04-developmentcontrol-schema`, `sp1-m05-developer-api`, `sp1-d03-d04-product-subfeature`, `sp1-w05-roadmap-runtime-bridge`, `w02-integration` |
| Worktrees | **7 total** (primary + 6 lane worktrees) — violates the "one active copy" invariant. Lane worktrees: `Nexus-W2-M03` (`sp1-m03-developmentrun-p1`), `Nexus-W2-M04` (`sp1-m04-developmentcontrol-schema`), `Nexus-W3-M05` (`sp1-m05-developer-api`), `Nexus-W3-D04` (`sp1-d03-d04-product-subfeature`), `Nexus-W3-W05` (`sp1-w05-roadmap-runtime-bridge`), `Nexus-W2-INT` (`w02-integration`) |
| Uncommitted state (primary) | `NEXUS_DEVELOPMENT_CONTROL.xlsx` modified (SHA `8AA73778A3F1CDB97D1A58BB05BD4797CA5A409BE8F986EF19FD9EDBB7CFE69F`) + untracked Wave-02 reports (`SP1_C02…`, `SP1_D03_D04…`, `SP1_M05…PREP`, `SP1_PARALLEL_WAVE_02…`) |
| Uncommitted state (lane worktrees) | **Heavy in-flight lane work** — Wave-02 B1/B2 candidates and Wave-03 lanes A/C/E are live working trees (see Wave-03 report). Each lane worktree carries its own diff. **Do NOT move until every lane branch is reviewed/merged/retained as ref.** |
| Identity/artifacts | `NEXUS_DEVELOPMENT_CONTROL.xlsx` + `control\` mirrors (SHA-tracked) + `architecture\` + `docs\` + `src\Nexus.Developer.Client.Legacy` shell |
| Move method | Physical move after worktree normalization (C02 Phase 2 → 3): normalize the 6 lane worktrees first (merge/retain as refs, `git worktree remove`), then `robocopy "C:\Personal\Nexus.Developer" "D:\NEXUS\Products\Developer" /E /MOVE /COPY:DAT /R:1 /W:1` |
| Path rewrites | ACTIVE_RUNTIME: `NexusDev.ps1:14` `$DevToolsPath = "C:\Personal\Nexus.Developer\src\Nexus.Developer.Client.Legacy"`; `install-shortcut.ps1`; `AI-Config\deepcode.ps1:6` dot-source `C:\Personal\UserSecrets\Load-Secrets.ps1`; `tools\local\start-dev.ps1` sibling `C:\Personal\Nexus.*` paths; `scripts\Import-LegacyDevTools.ps1` `$SourceRoot='C:\Personal\DevTools'`. BUILD: nuget.config / csproj comments citing `C:\Personal\LocalNuGet` (default: leave feed path). DevBridge design docs treating the workbook path as canonical. DOC_ONLY path tables |
| Verification | `git -C D:\NEXUS\Products\Developer rev-parse HEAD` = `ffe25e0`; workbook SHA at destination == `8AA73778…`; `git status --porcelain` matches frozen manifest (incl. workbook `M` disposition); `git fsck`; `dotnet build D:\NEXUS\Products\Developer\Nexus.Developer.slnx` 0/0; `dotnet test …` matches pre-move count (C02 G3) |
| Rollback | reverse robocopy or Phase-1 backup; re-run G3. Lane branches survive as refs even if a worktree was removed (Phase-2 rollback = `git worktree add <path> <branch>`) |

**Risk noted:** workbook is dirty (uncommitted `M`) — freeze decision required on whether the cutover wave may commit it (§7 item 4). `main` ahead 5/behind 1 must be reconciled at G0. Lane worktree dirt is the biggest pre-move normalization burden.

---

## 4. Record 3 — Nexus.Experience (independent Chat product repo)

| Field | Value (live, re-verified 2026-09-07) |
|---|---|
| Remote | `https://github.com/prtcare/Nexus.Experience.git` |
| Current branch | `fix/P1-7/workspace-selector-double-render-and-health-route` @ `10b130e` — **in sync** with its origin branch (0/0) |
| Other local branches | `main`, `feat/T-06-1.1.1.3/workspace-product-core-schema`, `feat/azure-sql`, `feature/dashboard-api-integration`, `sp1-d06-subchat` (lane, local-only) |
| Unique local-only branches | `sp1-d06-subchat` (Wave-03 Lane D lane branch) |
| Worktrees | **2 total** (primary + Lane-D worktree `Nexus-Exp-W3-D06` on `sp1-d06-subchat` @ `617aae7`) |
| Uncommitted state (primary) | **dirty — 322 porcelain lines** (largely tracked-source churn: `.github`, `.gitignore`, slnx, client `package.json`/esproj/user, etc. — NOT bin/obj; bin/obj/.vs/dist are gitignored). Needs a commit/stash decision before any move (C00/C01 §16). |
| Uncommitted state (Lane D worktree) | in-flight Wave-03 Lane D work (see Wave-03 report) |
| Identity/artifacts | Chat product; `Nexus.Products.Chat.*`; Azure SQL persistence; `.env.development` (local secrets — do NOT move as plaintext into a repo) |
| Move method | Physical move with `.git` intact, after Lane-D branch lands/retained and the primary dirty tree is neutralized: `robocopy "C:\Personal\Nexus.Experience" "D:\NEXUS\Products\Experience" /E /MOVE /COPY:DAT /R:1 /W:1` (slot name = §7 decision) |
| Path rewrites | DOC_ONLY `api_run.log` content root `C:\Personal\Nexus.Web\...` (historical log — leave); any `C:\Personal\LocalNuGet` references in csproj comments/nuget.config (default: feed stays). Experience already removed its machine-local NuGet source (last commit). `.env.*` secrets handled separately (R9) |
| Verification | `git rev-parse HEAD` = `10b130e` (primary); `git status --porcelain` == frozen (dirty tree owned); remote unchanged; fsck clean; `dotnet build Nexus.Experience.slnx` 0 errors at new path |
| Rollback | reverse robocopy or backup; re-run G3 |

**Risk noted:** 322-line dirty tree is the largest pre-move uncommitted burden in this repo set; lane `sp1-d06-subchat` must be resolved before Phase 2 normalization of Experience.

---

## 5. Record 4 — DevTools (→ Nexus Forge)

| Field | Value (live, re-verified 2026-09-07) |
|---|---|
| Remote | `https://github.com/prtcare/DevTools.git` |
| Current branch | `main` @ `04ed758` — **in sync** with `origin/main` (0/0) |
| Other local branches | `forge-v2-batch02` (local + origin, `7f3838f`), `sp1-m02-shared-writer-lock` (local-only lane branch, `7f3838f`) |
| Unique local-only branches | `sp1-m02-shared-writer-lock` (Wave-02 Lane A lane branch) |
| Worktrees | **3 total** (primary + `DevTools-ForgeV2` on `forge-v2-batch02` + `DevTools-W2-M02` on `sp1-m02-shared-writer-lock`) — violates "one active copy". Worktree `.git` files point at `C:/Personal/DevTools/.git/worktrees/*` (absolute — severs on a raw move) |
| Uncommitted state (primary `main`) | dirty (~171 porcelain lines: selftest logs + **tracked `bin/obj`/`.vs` debris** — repo has no `.gitignore`; ~151+ tracked build artifacts) |
| Uncommitted state (ForgeV2) | dirty (~78 tracked `bin/obj`) |
| Uncommitted state (W2-M02) | clean tree + in-flight lane report; tracked `bin/obj` regenerated by verification builds |
| Identity/artifacts | DevBridge engine/UI/tests/scripts → "Nexus Forge" home; tag `pre-forge-hardening-20260905` exists per C02 §2.2 |
| Move method | Normalize 3 worktrees → 1 (merge `forge-v2-batch02` → `main` per C02 §7.5, retain/retire `sp1-m02`, add `.gitignore` + untrack `bin/obj` as hygiene commit), then physical move: `robocopy "C:\Personal\DevTools" "D:\NEXUS\Forge" /E /MOVE /COPY:DAT /R:1 /W:1` |
| Path rewrites | ACTIVE_RUNTIME: `DevTools\NexusDev.ps1:14` `$DevToolsPath = "C:\Personal\DevTools"`; `install-shortcut.ps1`; `AI-Config\deepcode.ps1:8` dot-source `C:\Personal\UserSecrets\Load-Secrets.ps1`; `DevBridge\design\*` + DevBridge code embed `C:\Personal\Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` as canonical workbook (→ `D:\NEXUS\Products\Developer\...`); `start-dev.ps1` sibling paths; scripts `$SourceRoot`. BUILD: `pack-*.ps1` `$feed='C:\Personal\LocalNuGet'` (default: feed stays). DOC_ONLY path tables |
| Verification | `git rev-parse HEAD` = `04ed758`; `git status` matches frozen (debris owned/discarded); remote unchanged; `git fsck`; `dotnet build DevBridge/src/DevBridge.slnx` 0 errors; DevTools harness exits 0 (M01 71/71 + locking 59/59 as of Wave-02) |
| Rollback | reverse robocopy / backup; re-run G3 |

**Risk noted:** tracked build debris must be untracked via exact-pathspec hygiene commit (never `git add .`); 3→1 worktree normalization is a §7 human choice; cross-volume MAX_PATH risk on deep `DevBridge\src\*\bin\Debug\net10.0\ref\...` paths (R6 — prefer short slot names, enable LongPaths).

---

## 6. Record 5 — Nexus.Intelligence (seam repo — HUMAN DECISION, not sequenced)

| Field | Value (live, re-verified 2026-09-07) |
|---|---|
| Remote | `https://github.com/prtcare/Nexus.Intelligence.git` |
| Current branch | `main` @ `47f562a` — **in sync** with `origin/main` (0/0); single local branch |
| Unique local-only branches | none |
| Worktrees | **1** (primary only) |
| Uncommitted state | dirty (5 porcelain lines: `IntelligenceServiceCollectionExtensions.cs`, `TurnPipeline.cs`, `Nexus.Intelligence.Tests.csproj` modified + new `Roles\` folder under Core + tests) — a real in-flight feature (Roles) needing a commit/stash decision |
| End-state slot | **OPEN — Platform L04 member or Product?** (discovery §14/§20). Until a human decides, this repo is **excluded from the C02 cutover sequence**. Record kept for completeness; NO move method/path-rewrite is committed for it. |
| Verification / rollback | n/a until disposition decided |

---

## 7. Non-active / UNKNOWN dispositions resolved this lane (from C00/C01)

Live re-check of every flagged UNKNOWN / ambiguous folder:

| Path | Prior flag | Resolved disposition (live evidence) |
|---|---|---|
| `C:\Personal\Nexus.Int`, `Nexus.Web`, `NexusAI`, `Nexus.Int-fresh`, `Nexus.Web-fresh`, `NexusAI-fresh` | "empty / DELETE-LATER" | **CONFIRMED empty** — no `.git`, 0 files. Not repos. Archive/delete-later only. |
| `C:\Personal\Nexus-Local` | REVIEW → infra | No `.git` at root; only `infra\` (2 files). Non-git infra scratch → follow-on archive wave, `.env` secrets handled separately. |
| `C:\Personal\Compilers` | UNKNOWN relation to DevTools; REVIEW→ARCHIVE | No `.git`; **1224 files** incl. `PPDS.WorkbookGenerator`, `C_001_Platform Compiler`, `Backup`, `Nexus`. **UNKNOWN now downgraded**: it is a **V1-era standalone workbook-generator / compiler asset area**, not part of the DevTools git repo (which has no such tracked tree). Relation to DevTools = historical ancestry only. Disposition: **ARCHIVE (V1 historical)**; no move into the Forge repo. |
| `C:\Personal\Dataverse` | ARCHIVE | No `.git`; `N_001_Nexus_1_0_0_1` + a zip (4 files). **ARCHIVE** confirmed. |
| `C:\Personal\UserSecrets` | UNKNOWN — secret mgmt | **Not scanned (deny rule), never moved as plaintext** (R9). Dot-sources from `deepcode.ps1` (DevTools + Developer) + `Load-Secrets.ps1`. Decision: keep at current path OR migrate to a secrets manager — §7 human choice. |

---

## 8. Cross-cutting cutover prerequisites (this wave changed nothing physically; these are the freeze conditions)

1. **One active copy per repo** is currently violated in 3 repos (Developer: 7, DevTools: 3, Experience: 2). Lane worktrees must be normalized at Phase 2 — Wave-03 lanes are still in-flight on 4 of them; the Wave-03 integration review (PASS/FIX/BLOCKED) and a human accept decision are required before any normalization.
2. **Uncommitted work to own at freeze:** Developer primary workbook (`M`, SHA `8AA73778…`) + Wave-02 reports; Developer lane diffs; Experience 322-line tree + Lane-D; Intelligence Roles feature; DevTools tracked `bin/obj` debris.
3. **`main` divergence to reconcile at G0:** Developer `main` ahead 5 / behind 1; Platform `main` behind 2.
4. **Secrets** (`.env.*`, `UserSecrets`) never travel inside a moved repo.
5. **LocalNuGet** (`C:\Personal\LocalNuGet`) does **not** move in the cutover wave by default (M-08-1.1 owns its decommission); only path references are updated if the human chooses otherwise.
6. **Workbook authority** stays single-workbook in this wave; the two-workbook `D:\NEXUS\DevelopmentControl` schema migration is a separate governed wave.

*No file was moved, no branch committed, no remote pushed, no junction created by this lane. The authoritative workbook was not opened for writing.*
