# SP1-C00 + SP1-C01 — NEXUS V1/V2 CONSOLIDATION DISCOVERY REPORT

**Wave:** Nexus V2.3 P1-WAVE-01 · LANE C (SP1-C00 + SP1-C01)
**Author:** Claude (read-only discovery agent) for Durai
**Date:** 2026-09-07
**Mode:** STRICTLY READ-ONLY. No file, repo, or ref was moved/renamed/copied/edited/deleted/staged/committed. This report is the only file created.
**Scope roots inspected:** C:\Personal (all Nexus-related + support folders), D:\NEXUS, plus D:\architecture (V1 loose docs, noted).

> Governing directive found on disk: `D:\NEXUS\Claude Prompt — Nexus Foundation Reset and Migration.md` ("NEXUS FOUNDATION RESET" directive). It establishes `D:\NEXUS` as the canonical local root, describes the conceptual `Forge\ | Platform\ | Products\ | Atlas\ | DevelopmentControl\ | Architecture\ | Infrastructure\ | Shared\ | Tools\ | Archives\` structure, mandates a governed baseline→discover→classify→propose→human-approve→migrate wave process, and forbids a one-shot restructure. It also names the successor repos (NexusAI→Platform lineage; Nexus.Int→Intelligence; Nexus.Web→Experience) and states DevBridge must evolve into **Nexus Forge**, not be retired. This report is the SP1-C00/C01 discovery + classification half of that directive's Stage 1–2.

---

## 1. Full folder inventory

Every path inspected (top-level, plus notable subfolders). "Git?" = valid git repository at that path.

| # | Path | Purpose (evidence) | Active / Historical | Git repo? | Disposition guess |
|---|------|--------------------|----------------------|-----------|-------------------|
| 1 | `C:\Personal\Nexus.Developer` | Developer product code (Api/Application/Core/Infrastructure/Client.Legacy) + **authoritative Development Control workbook** `NEXUS_DEVELOPMENT_CONTROL.xlsx` + `control/` mirrors + `architecture/` reports. Repo `prtcare/Nexus.Developer`. | Active (branch `feature/m-08-1-2-ci-pipeline`, 1 uncommitted xlsx) | Yes | CURRENT → eventual `D:\NEXUS\Products\Developer` |
| 2 | `C:\Personal\Nexus.Platform` | Shared platform NuGet libs (`Nexus.Platform.*`, `Nexus.ProductCore.*`) + **authoritative docs** `docs/` (numbered 00–12 + ADRs) + **authoritative architecture/roadmap** (`NEXUS_MASTER_ARCHITECTURE.md`, `nexus-roadmap.yaml`). Git lineage = renamed/continued **NexusAI** repo. | Active (branch `wave-g-v2-docs-batch03`, clean) | Yes | CURRENT → eventual `D:\NEXUS\Platform` (docs → `D:\NEXUS\Architecture`) |
| 3 | `C:\Personal\DevTools` | Worktree (main) of repo `prtcare/DevTools`. Dev shell (`NexusDev.ps1`, `LayerShell.ps1`), **DevBridge** tool (workbook dev-control engine; `src/DevBridge.{Engine,UI}`, `DevBridge.Tests`, scripts, design docs), `AI-Config`, `Archived/NexusDevConsole-MVP`. | Active (dirty: selftest logs + **151 tracked bin/obj files**) | Yes (main worktree) | CURRENT (Forge candidate) → `D:\NEXUS\Forge` |
| 4 | `C:\Personal\DevTools-ForgeV2` | Second **worktree of the SAME `prtcare/DevTools` repo** on branch `forge-v2-batch02`. Holds "Wave B" DevBridge development-control provider migration commits. | Active (dirty: build debris under tracked bin/obj) | Yes (linked worktree, no own `.git`) | Same repo as #3 — KEEP-AS-SEPARATE-REPO (merge branch later) |
| 5 | `C:\Personal\Nexus.Intelligence` | Intelligence product code (`Nexus.Intelligence.*` incl. Core/Api/Context/Memory/Agents). Git lineage = renamed/continued **Nexus-Int** (extracted from NexusAI). Has in-progress uncommitted `Roles/` feature. | Active (branch `main`, 3 modified + 2 untracked) | Yes | CURRENT → eventual `D:\NEXUS\Products` or `Platform` L04 AI (human decision) |
| 6 | `C:\Personal\Nexus.Experience` | Chat product + React clients (`src/Nexus.Products.Chat.*`, `src/Nexus.Experience.Client`, `src/Nexus.Web.Client` legacy rename remnant). Git lineage = renamed/continued **Nexus-web** frontend foundation. **Large dirty tree (313 modified files).** | Active (branch `fix/P1-7/workspace-selector-double-render-and-health-route`) | Yes | CURRENT → eventual `D:\NEXUS\Products` |
| 7 | `C:\Personal\Nexus.Int` | **Empty** directory (0 files), dated 2026-08-26. Leftover from recovery era; real content lives in `_stale-recovery-artifacts\Nexus.Int-fresh` and lineage now = Nexus.Intelligence. | Historical (empty) | No | DELETE-LATER |
| 8 | `C:\Personal\Nexus.Int-fresh` | **Empty** directory (0 files), dated 2026-08-26. | Historical (empty) | No | DELETE-LATER |
| 9 | `C:\Personal\Nexus.Web` | **Empty** directory (0 files), dated 2026-08-26. | Historical (empty) | No | DELETE-LATER |
| 10 | `C:\Personal\Nexus.Web-fresh` | **Empty** directory (0 files), dated 2026-08-26. | Historical (empty) | No | DELETE-LATER |
| 11 | `C:\Personal\NexusAI` | **Empty** directory (0 files), dated 2026-08-26. | Historical (empty) | No | DELETE-LATER |
| 12 | `C:\Personal\NexusAI-fresh` | **Empty** directory (0 files), dated 2026-08-26. | Historical (empty) | No | DELETE-LATER |
| 13 | `C:\Personal\Roadmaps` | Non-git working copy of the architecture authority: `NEXUS_MASTER_ARCHITECTURE.md` (99 KB, 2026-09-07 17:43) + `nexus-roadmap.yaml` (181 KB). yaml self-declares `this_file_status: NOT git-tracked (plain file in C:\Personal\Roadmaps\, no repository)`. **Older/different** than Nexus.Platform copies (see §11). | Active-but-orphaned (no repo) | No | MOVE/merge under Nexus.Platform (or `D:\NEXUS\Architecture`) — verify then remove |
| 14 | `C:\Personal\Nexus-Local` | Local docker-compose infra: `infra/compose.yml` + `infra/.env` (Aug 25). Pre-Azure local dev stack artifact. | Stale | No | MOVE to `D:\NEXUS\Infrastructure` or archive (verify `.env` not needed) |
| 15 | `C:\Personal\ArchitectureAudit` | Single report `NEXUS_ARCHITECTURE_AUDIT_2026-08-25.md` (audit of Platform/Intelligence/Experience v2.2 archives). | Historical (superseded by Nexus.Platform/docs) | No | ARCHIVE into docs; keep evidence |
| 16 | `C:\Personal\_backup` | **Empty** top-level dir (dated 2026-08-26). | Historical (empty) | No | DELETE-LATER |
| 17 | `C:\Personal\_stale-recovery-artifacts` | **Recovery vault** from the 2026-08-20 incident. Holds valid recovered clones `Nexus.Int-fresh`, `Nexus.Web-fresh`, `NexusAI-fresh` (see §2) and a `_backup/` subtree with **3 broken/unrecognized git dirs** (`Nexus.Int-worktree`, `Nexus.Web-worktree`, `NexusAI-worktree` — git says "not a git repository"). | Historical (recovery artifacts; git objects now folded into Platform/Intelligence/Experience) | Mixed (3 valid clones + 3 broken git dirs) | ARCHIVE → freeze; do not delete until human confirms lineage |
| 18 | `C:\Personal\_zips` | `.zip` backups dated 2026-08-25: `ArchitectureAudit.zip`, `DevTools.zip`, `Nexus.Developer.zip`, `Nexus.Experience-*.zip`, `Nexus.Intelligence-*.zip`, `Nexus.Platform-*.zip`, `Roadmaps.zip`. | Historical (superseded by git history + current repos) | No | ARCHIVE / DELETE-LATER after verification |
| 19 | `C:\Personal\Temp` | Scratch: `TempJs`, `TempReact`. | Historical (scratch) | No | DELETE-LATER |
| 20 | `C:\Personal\Documentation` | **Stale historical docs snapshot** (Aug 23): `NEXUS_MASTER_ARCHITECTURE.md`, `nexus-roadmap.yaml`, `docs/` (large standard/ADR set), `Archived/nexus-v2.1-docs`, `change-reports/` (CHANGE_REPORT_v2.1.md, v2.2.md). Nexus.Platform `docs/CURRENT_STATE.md` states this folder is "a stale historical snapshot, not a [source of truth]". | Historical | No | ARCHIVE (V1 docs) — compare against Nexus.Platform/docs before discarding |
| 21 | `C:\Personal\Dataverse` | V1 Dataverse solution export: `N_001_Nexus_1_0_0_1/` + `.zip`. | Historical (V1-only asset) | No | ARCHIVE under `D:\NEXUS\Archives` |
| 22 | `C:\Personal\Compilers` | Older dev-tool area: `C_001_Platform Compiler`, `Nexus`, `Backup`, `PPDS.WorkbookGenerator` (likely DevBridge ancestor). Relation to DevTools UNKNOWN. | Historical (V1) | No | REVIEW → ARCHIVE (may hold workbook-generator lineage) |
| 23 | `C:\Personal\LocalNuGet` | **Local NuGet file feed**: `Nexus.Platform.*.nupkg` (Aug 18) + `Nexus.Intelligence.Contracts.*.nupkg` (Aug 18/25). Referenced by `pack-local.ps1`, `pack-productcore-local.ps1`, csproj comments as fallback because GitHub Packages was unreachable (M-08-1.1). | Active-but-temporary (dev fallback) | No | MOVE to feed-strategy; retire once GitHub Packages reachable |
| 24 | `C:\Personal\UserSecrets` | Secrets loader store referenced by `DevTools/AI-Config/deepcode.ps1` and migrated copies (`C:\Personal\UserSecrets\Load-Secrets.ps1`). **Not scanned (deny rule).** | Active (local secrets) | No | UNKNOWN — move to proper secret management; human review |
| 25 | `C:\Personal\Test` | Unrelated local git repo: "30 Day Fitness Challenge development control baseline" (1 commit, no remote, no upstream). | Not Nexus | Yes (no remote) | IGNORE / DELETE-LATER (keep if personal) |
| 26 | `C:\Personal\.claude` | `settings.local.json` (permission allow-list only). | Active (harness) | No | IGNORE |
| 27 | `D:\NEXUS` | **Canonical target root** per directive. Currently only: 3 `.md` directive/prompt files, `Nexus_Foundation_Roadmap.docx`, and `DevelopmentControl/` holding `NEXUS_FOUNDATION_DEVELOPMENT_CONTROL.xlsx` + `NEXUS_PRODUCTS_DEVELOPMENT_CONTROL.xlsx` (two "foundation/products" workbook seeds, 2026-09-05). | Active target (mostly empty scaffold) | No | Target root for consolidation |
| 28 | `D:\architecture` | V1 loose docs: `CLAUDE_CODE_MIGRATION_PROMPTS.md`, `NEXUS_ARCHITECTURE_V2.md`, `nexus-resturcture.ps1` (Aug 17). | Historical (V1) | No | ARCHIVE into Nexus archives |
| 29 | `D:\A`, `D:\Agent`, `D:\Product`, `D:\New folder`, `D:\Andriod Mirror` etc. | Non-Nexus (camera system, agent install, QuantumPassport/Software product, installers). | Unrelated | — | IGNORE |

> Note: `C:\Personal\DevTools-ForgeV2` has no `.git` *directory* because it is a git **worktree** (its `.git` is a file pointing to `DevTools\.git`) — confirmed by `git worktree list`.

---

## 2. Full repository inventory

9 distinct **valid** git repositories at 10 checked-out locations, plus 3 broken git dirs (recovery vault). All dates from `git log --format=%ci`; all SHAs from `git rev-parse HEAD` (2026-09-07).

### Active primary repos
| Repo path | Remote (origin) | Branch (HEAD) | HEAD SHA | Last commit | Ahead/behind vs upstream | Uncommitted | Untracked | Worktrees |
|---|---|---|---|---|---|---|---|---|
| `C:\Personal\Nexus.Developer` | `https://github.com/prtcare/Nexus.Developer.git` | `feature/m-08-1-2-ci-pipeline` | `5913bc5` | 2026-09-07 20:28 | **ahead 1** | 1 (`NEXUS_DEVELOPMENT_CONTROL.xlsx` M) | 0 | none (1) |
| `C:\Personal\Nexus.Platform` | `https://github.com/prtcare/Nexus.Platform.git` | `wave-g-v2-docs-batch03` | `5f538c2` | 2026-09-07 19:51 | 0/0 | 0 | 0 | none (1) |
| `C:\Personal\DevTools` | `https://github.com/prtcare/DevTools.git` | `main` | `04ed758` | 2026-09-04 | 0/0 | ~150+ (selftest logs, tracked bin/obj) | ~10 | 2 (see below) |
| `C:\Personal\DevTools-ForgeV2` | `https://github.com/prtcare/DevTools.git` (same) | `forge-v2-batch02` | `3bab505` | 2026-09-04 | 0/0 | ~300 (build debris) | 0 | linked worktree of DevTools |
| `C:\Personal\Nexus.Intelligence` | `https://github.com/prtcare/Nexus.Intelligence.git` | `main` | `47f562a` | 2026-08-26 13:21 | 0/0 | 3 (TurnPipeline, DI ext, tests csproj) | 2 (`src/.../Roles/`, `tests/.../Roles/`) | none (1) |
| `C:\Personal\Nexus.Experience` | `https://github.com/prtcare/Nexus.Experience.git` | `fix/P1-7/workspace-selector-double-render-and-health-route` | `10b130e` | 2026-08-30 10:38 | 0/0 | **313** (mostly under `src/`, incl. tracked build artifacts + real source) | 9 (Client feature pages, `.claude/`, stray `src/package-lock.json`) | none (1) |
| `C:\Personal\Test` | *(none)* | `main` | `3f3cc0b` | 2026-09-02 | no upstream | 1 (`30_DAY_FITNESS_CHALLENGE_DEVELOPMENT_CONTROL.xlsx` M) | 0 | none (1) |

### Recovery-vault repos (valid, under `C:\Personal\_stale-recovery-artifacts`)
| Repo path | Remote (origin) | Branch | HEAD SHA | Log | Notes |
|---|---|---|---|---|---|
| `...\Nexus.Int-fresh` | `https://github.com/prtcare/Nexus-Int.git` | `main` | `8141cd0` | 4 commits (v2 Intelligence API/boundary/contracts) | **Predecessor clone** of Nexus.Intelligence (same SHAs `b27cd26`→`8141cd0` present in Nexus.Intelligence). Clean. |
| `...\Nexus.Web-fresh` | `https://github.com/prtcare/Nexus-web.git` | `main` | `2c0b94c` | 37 commits; local main at old commit | **Predecessor clone** of Nexus.Experience. Remote branches include `arch/v2`, `feat/azure-sql`, `feat/T-06-1.1.1.3/workspace-product-core-schema`, `feature/dashboard-api-integration` — **identical set to Nexus.Experience remote branches**. Dirty (tracked obj debris). Has `.git/index.lock` present. |
| `...\NexusAI-fresh` | `https://github.com/prtcare/NexusAI.git` | `main` | `890c37e` | 50 commits ("Final git before Reconstruct") | **Predecessor clone** of Nexus.Platform. `origin/arch/v2` SHA `4b15f71` **matches Nexus.Platform's `origin/arch/v2` exactly** → same upstream lineage. |

### Broken / unrecognized git dirs (recovery vault `_backup`)
`...\_backup\Nexus.Int-worktree`, `Nexus.Web-worktree`, `NexusAI-worktree`: each has a `.git/` (config + HEAD) pointing at remotes `Nexus-Int`, `Nexus-web`, `NexusAI` respectively, but `git rev-parse` fails with "not a git repository". These are the **2026-08-20 damaged leftovers** (lost `.git\objects`) — preserved as raw recovery artifacts. Not usable as repos.

---

## 3. Git topology (common ancestry / dup clones / divergent repos)

**Established lineage facts (read-only, cross-repo SHA checks):**

1. **NexusAI → Nexus.Platform (same lineage).** `Nexus.Platform` history bottom = `877e8e7 "Initial NexusAI solution"`; NexusAI-fresh HEAD `890c37e "Final git before Reconstruct"` is an **ancestor** of Nexus.Platform HEAD `5f538c2` (`merge-base --is-ancestor` = YES). `origin/arch/v2` resolves to the **same SHA `4b15f71`** in both local clones. Conclusion: GitHub `prtcare/NexusAI` was renamed/continued as `prtcare/Nexus.Platform` (the remote string in the stale clone still says `NexusAI.git`; the shared `arch/v2` ref proves the rename). NexusAI = V1 monolith; Nexus.Platform = V2 continuation. **Not a duplicate to merge — the same repo at different points in its life.**
2. **Nexus.Int/Nexus-Int → Nexus.Intelligence (same lineage).** `Nexus.Int-fresh` main `8141cd0` is present as a commit in `Nexus.Intelligence` history; Intelligence's root is `b27cd26 "chore: initial Nexus.Int solution from NexusAI extraction"` (an orphan root — a file-level extraction, no NexusAI git parent). Intelligence renamed `Nexus.Int → Nexus.Intelligence` (commits `564c65e`, `87b4de5`).
3. **Nexus.Web/Nexus-web → Nexus.Experience (same lineage).** `Nexus.Web-fresh` main `2c0b94c "feat: establish Nexus React frontend foundation"` is an orphan root present in Nexus.Experience history (ancestor of HEAD). Nexus.Experience then continued with workspace/Chat product + Azure SQL work. The remote-branch sets of Nexus-web-fresh and Nexus.Experience are **identical** (`arch/v2`, `feat/azure-sql`, `feat/T-06-1.1.1.3/...`, `feature/dashboard-api-integration`, `main`) → the GitHub repo `prtcare/Nexus-web` was renamed/continued as `prtcare/Nexus.Experience`.
4. **Nexus.Developer = independent lineage.** Root `e1ed33e "Initial commit: bootstrap Nexus.Developer"` / `6dd0d29` (2026-08-26). Contains **no** NexusAI/Platform/Intelligence objects. Separate product repo.
5. **DevTools = independent lineage** (19 commits, tag `pre-forge-hardening-20260905`), not ancestrally related to the Nexus product repos. Its `DevBridge` component is the workbook dev-control engine ("Forge" per directive) that writes to `C:\Personal\Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx`.
6. **Duplicate-clone category:** the three `_stale-recovery-artifacts\*-fresh` clones are **stale copies of the same upstreams** that Platform/Intelligence/Experience have already absorbed — they are not additional divergent work (their branches are all contained in the successor repos' `--all`), with one caveat: Nexus.Web-fresh local `main` sits at old commit `2c0b94c` while its `origin/*` refs carry the azure-sql work — treat the *local main state* as not-yet-merged unless Nexus.Experience `--all` already includes it (it does: `2c0b94c` is an ancestor). Low risk.
7. **Divergence to check:** `Nexus.Platform` has remote branches `arch/v2` and `work/M-01-6.1-openai-live` not present locally; `Nexus.Developer` has remote branches `feature/m-12-0-1-*` and `feature/wi-07-10-4-*` not present locally. These are *remote-only* branches (pushed work) — verify they are fully merged before any archive.

**Tags:** `Nexus.Platform` → `pre-l07-split-20260905`; `DevTools` (both worktrees) → `pre-forge-hardening-20260905`. Nexus.Developer/Intelligence/Experience: none.

**History sizes (`git log --oneline --all | wc -l`):** Nexus.Developer 80 · Nexus.Platform 85 · DevTools 19 · Nexus.Intelligence 14 · Nexus.Experience 48 · Nexus.Int-fresh 4 · Nexus.Web-fresh 37 · NexusAI-fresh 50 · Test 1.

---

## 4. Remotes

All remotes are GitHub under owner **prtcare**, HTTPS, single `origin`.

| Repo | Fetch URL |
|---|---|
| Nexus.Developer | `https://github.com/prtcare/Nexus.Developer.git` |
| Nexus.Platform | `https://github.com/prtcare/Nexus.Platform.git` |
| DevTools (both worktrees) | `https://github.com/prtcare/DevTools.git` |
| Nexus.Intelligence | `https://github.com/prtcare/Nexus.Intelligence.git` |
| Nexus.Experience | `https://github.com/prtcare/Nexus.Experience.git` |
| Test | *(no remote)* |
| stale `Nexus.Int-fresh` | `https://github.com/prtcare/Nexus-Int.git` |
| stale `Nexus.Web-fresh` | `https://github.com/prtcare/Nexus-web.git` |
| stale `NexusAI-fresh` | `https://github.com/prtcare/NexusAI.git` |

Remote-name inference: `Nexus-Int` and `Nexus-web` GitHub repos are the pre-rename upstreams now represented by `Nexus.Intelligence` / `Nexus.Experience`; `NexusAI` is the pre-rename upstream of `Nexus.Platform`. (GitHub may redirect the old names.) No SSH remotes; no secondary remotes (e.g. no `upstream`).

---

## 5. Active branches (with unique work notes)

| Repo | Branch | Notes / unique work |
|---|---|---|
| Nexus.Developer | `feature/m-08-1-2-ci-pipeline` **(HEAD)** | CI pipeline milestone; **ahead 1** of origin — the local-only commit `5913bc5 "SP1-M00 DevelopmentControl concurrency + atomic-write stabilization"` (2026-09-07) is **not pushed**. Uncommitted workbook change. |
| Nexus.Developer | `feature/cors-and-health-endpoint`, `feature/launchsettings-dev-api` | local only; both also on origin. |
| Nexus.Developer | `main` | present locally + `origin/main`; remote-only: `feature/m-12-0-1-convert-conversation-to-feature`, `feature/m-12-0-1-convert-conversation-to-more-targets`, `feature/wi-07-10-4-object-chat-link-application-api`. |
| Nexus.Platform | `wave-g-v2-docs-batch03` **(HEAD)** | V2.3 doc batch; HEAD `5f538c2 "Nexus V2.3: freeze Work Universe architecture"`, tag `pre-l07-split-20260905`. |
| Nexus.Platform | `main` (local + origin) | remote also has `arch/v2`, `work/M-01-6.1-openai-live`, `chore/M-08-1.1-productcore-feed-publish`. |
| DevTools | `main` **(HEAD, worktree DevTools)** | Post-Forge-permanent work (`04ed758 "Wave B: make Forge permanent..."`). Dirty from selftest logs. |
| DevTools | `forge-v2-batch02` **(HEAD, worktree DevTools-ForgeV2)** | "Wave B" dev-control provider migration; unmerged dev branch of same repo. |
| Nexus.Intelligence | `main` **(HEAD)** | only branch. Has uncommitted in-progress `Roles/` (AI role resolver) feature. |
| Nexus.Experience | `fix/P1-7/workspace-selector-double-render-and-health-route` **(HEAD)** | Client fix branch. Remote also: `main`, `arch/v2`, `feat/azure-sql`, `feat/T-06-1.1.1.3/workspace-product-core-schema`, `feature/dashboard-api-integration`. |
| Nexus.Experience | `main` | local + origin. |
| Test | `main` | fitness challenge; no upstream. |

---

## 6. Unique / uncommitted work per repo (must be preserved)

- **Nexus.Developer** — `NEXUS_DEVELOPMENT_CONTROL.xlsx` modified (workbook state ahead of the `control/` mirrors); plus the whole unpushed commit `5913bc5` (DevelopmentControl concurrency + atomic-write coordinator source files). **Back up the workbook before anything.**
- **Nexus.Platform** — clean. Nothing uncommitted.
- **DevTools / DevTools-ForgeV2** — both dirty. DevTools main: DevBridge selftest logs/state (`logs/selftest/*`), deleted workbook backups, tracked `bin/obj` churn. ForgeV2: extensive `obj/bin` churn under `DevBridge.Engine`/`DevBridge.Tests`. Real source on `forge-v2-batch02` = dev-control provider migration (committed on the branch). The *tracked build-debris* noise is a repo-hygiene problem (no `.gitignore`, 151 tracked `bin|obj` files), not unique work.
- **Nexus.Intelligence** — uncommitted feature: `src/Nexus.Intelligence.Core/Roles/*` (`AiRole.cs`, `AiRoleAssignment.cs`, `AiRoleResolution.cs`, `AiRoleResolver.cs`, `IAiRoleResolver.cs`, `IAiRoleStore.cs`, `InMemoryAiRoleStore.cs`) + `tests/.../Roles/AiRoleResolverTests.cs`; modified `TurnPipeline.cs`, `IntelligenceServiceCollectionExtensions.cs`, test csproj. This is live in-progress feature work.
- **Nexus.Experience** — **large dirty tree (313 M, 9 untracked).** Includes real source changes under `src/Nexus.Experience.Client` (package.json, App.tsx, pages, API client), plus untracked new pages (`DeveloperChatPage.tsx`, `DeveloperObjectBrowsePage.tsx`, `SubprojectsPage.tsx`, etc.) and `src/package-lock.json`. Mixed with tracked build/deploy artifacts (`api_run.log`, `.env.development`, esproj.user). **Needs a careful stash/commit before any move — high risk of loss.**
- **Test** — fitness workbook modified (unrelated to Nexus).

---

## 7. Worktrees

`git worktree list` results:

- **`C:\Personal\DevTools`** → `04ed758 [main]`
- **`C:\Personal\DevTools-ForgeV2`** → `3bab505 [forge-v2-batch02]`

  Both listed from **either** worktree → confirmed same repo (`prtcare/DevTools`). Relationship: `DevTools-ForgeV2` is a linked worktree for the `forge-v2-batch02` branch of the same repo as `DevTools`. Same total history (19), same tag. **This is the intended "one repo, two checkouts" pattern — NOT two repos.**
- All other repos: single worktree (themselves). No worktrees pointing at Platform/Developer/Intelligence/Experience.

Recovery-vault worktrees: none (each is a standalone clone). The `_backup/*-worktree` dirs are broken git dirs (not registered worktrees).

---

## 8. Duplicate folders/repos

| Duplicate group | Evidence | Recommendation |
|---|---|---|
| `Nexus.Int` / `Nexus.Int-fresh` (empty) vs `_stale-recovery-artifacts\Nexus.Int-fresh` (real clone) vs `Nexus.Intelligence` (successor) | Empty dirs dated same minute (2026-08-26 15:40); real content only in recovery vault; successor repo contains all SHAs | Delete empty dirs; archive recovery clone; keep Nexus.Intelligence as the single current repo |
| `Nexus.Web` / `Nexus.Web-fresh` (empty) vs `_stale-recovery-artifacts\Nexus.Web-fresh` vs `Nexus.Experience` (successor) | Same pattern; identical remote branch sets | Same as above |
| `NexusAI` / `NexusAI-fresh` (empty) vs `_stale-recovery-artifacts\NexusAI-fresh` vs `Nexus.Platform` (successor) | Same pattern; `origin/arch/v2` same SHA in stale clone and Nexus.Platform | Same as above |
| `Nexus.Experience\src\Nexus.Web.Client` vs `Nexus.Experience\src\Nexus.Experience.Client` | Rename remnant inside one repo (V1 name `Nexus.Web.Client` retained alongside `Nexus.Experience.Client`) | Extract/delete `Nexus.Web.Client` in a future refactor (human decision) |
| `Roadmaps\` vs `Nexus.Platform\` architecture/roadmap files | Same filenames, **different content** (md5 differ); Roadmaps not git-tracked and older; Documentation copy oldest | Single source of truth = Nexus.Platform; Roadmaps/Documentation copies → archive |
| `Documentation\docs` vs `Nexus.Platform\docs` | Both hold the standard/ADR set; Nexus.Platform docs is current, Documentation is "stale historical snapshot" (per Platform docs/CURRENT_STATE.md) | Archive Documentation |
| Top-level `_backup` (empty) vs `_stale-recovery-artifacts\_backup` (broken git dirs) | Same name, two places | Delete empty one; archive the broken one |

---

## 9. V1-only useful assets (historical value, not part of V2 current code)

1. **`C:\Personal\Documentation\`** — complete V1/v2.1 standard + ADR + architecture doc set + `CHANGE_REPORT_v2.1.md` / `v2.2.md`, `Archived\nexus-v2.1-docs`. Useful for history; superseded by Nexus.Platform/docs for live use.
2. **`C:\Personal\Dataverse\N_001_Nexus_1_0_0_1`** — the V1 Dataverse solution export (referenced by the Azure-SQL migration ADR as the legacy store being replaced). Keep as evidence/rollback reference.
3. **`C:\Personal\Compilers\`** — `C_001_Platform Compiler`, `Nexus`, `PPDS.WorkbookGenerator` — V1 compiler/workbook-generator lineage (likely ancestor of DevBridge). Confirm relation before discard.
4. **`C:\Personal\_stale-recovery-artifacts\`** — the recovery clones and broken git dirs that document the 2026-08-20 incident and its recovery; contains the only copies of the pre-rename local state.
5. **`D:\architecture\`** — `NEXUS_ARCHITECTURE_V2.md`, `CLAUDE_CODE_MIGRATION_PROMPTS.md`, `nexus-resturcture.ps1` (V1 restructure script).
6. **`C:\Personal\ArchitectureAudit\NEXUS_ARCHITECTURE_AUDIT_2026-08-25.md`** — independent audit of the v2.2 three-way architecture.
7. **`C:\Personal\_zips\*.zip`** — pre-rename snapshot zips (2026-08-25) of every current repo + Roadmaps.

---

## 10. V2/current assets

- **Nexus.Platform** (`src/Nexus.Platform.{Core,Identity,Persistence,Providers.*,Tools}`, `Nexus.ProductCore.{Contracts,Scope}`, `Nexus.Delivery.Contracts`) — shared platform libraries; **docs/** (canonical set); `NEXUS_MASTER_ARCHITECTURE.md`; `nexus-roadmap.yaml`.
- **Nexus.Intelligence** (`Nexus.Intelligence.{Core,Api,Context,Memory,Agents,Contracts}`) — "deciding layer" `/intelligence/v1`.
- **Nexus.Experience** (`Nexus.Products.Chat.{Api,Application,Domain,Infrastructure}`, `Nexus.Experience.Client` React SPA, legacy `Nexus.Web.Client`) — Chat product `/api/v1` + client.
- **Nexus.Developer** (`Nexus.Developer.{Core,Infrastructure,Application,Api}`, `Client.Legacy`) — Developer product; **`NEXUS_DEVELOPMENT_CONTROL.xlsx` + `control/` (machine mirrors)** = the active Development Control system of record.
- **DevTools / DevTools-ForgeV2** — **DevBridge** (workbook dev-control engine + provider boundary, the "Forge" candidate); `forge-v2-batch02` carries the provider-migration branch.
- **D:\NEXUS\DevelopmentControl** — two new workbook seeds (`NEXUS_FOUNDATION_DEVELOPMENT_CONTROL.xlsx`, `NEXUS_PRODUCTS_DEVELOPMENT_CONTROL.xlsx`, 2026-09-05), foundation reset target format.
- **Nexus-Local\infra** (`compose.yml`, `.env`) — local docker stack (candidate for `Infrastructure`).

---

## 11. Authoritative workbooks

| Workbook | Location | Status |
|---|---|---|
| **Development Control (Developer / active)** | `C:\Personal\Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` (+ `control\` CSV/JSON mirrors: `CONTROL_MANIFEST.json`, `ROADMAP_LEDGER.csv`, `ACTIVE_CHANGES.csv`) | **Live system of record** for Developer work items (CHG-2026xxxx-xxx records). SHA-tracked via control manifest; currently has uncommitted changes. |
| **Foundation / Products workbook seeds** | `D:\NEXUS\DevelopmentControl\NEXUS_FOUNDATION_DEVELOPMENT_CONTROL.xlsx`, `NEXUS_PRODUCTS_DEVELOPMENT_CONTROL.xlsx` | Target-schema seeds (2026-09-05), part of the foundation reset — not yet the live record. |
| **Master architecture doc** | `C:\Personal\Nexus.Platform\NEXUS_MASTER_ARCHITECTURE.md` (114 KB, 2026-09-07 18:42) | **Authoritative, git-tracked.** |
| **Roadmap (YAML)** | `C:\Personal\Nexus.Platform\nexus-roadmap.yaml` (217 KB, 2026-09-07 19:38) | **Authoritative, git-tracked.** |
| **Roadmap (docx)** | `D:\NEXUS\Nexus_Foundation_Roadmap.docx` | Seed/context doc. |

Copy comparison (md5): Nexus.Platform vs `C:\Personal\Roadmaps\` vs `C:\Personal\Documentation\` copies of `NEXUS_MASTER_ARCHITECTURE.md` and `nexus-roadmap.yaml` **all differ**; Nexus.Platform copy is newest and largest → **single source of truth = Nexus.Platform**. `Roadmaps\nexus-roadmap.yaml` self-declares *"NOT git-tracked (plain file in C:\Personal\Roadmaps\, no repository)"* → orphaned working copy.

---

## 12. Documentation authority

**`C:\Personal\Nexus.Platform\docs\` is the documentation authority.** It contains the canonical numbered set (`00_DOCUMENTATION_STANDARD.md` … `12_NEXUS_ENTITY_MODEL_AND_RELATIONSHIPS.md`), ADRs (`ADR-014_AZURE_SQL_MIGRATION.md`, `ADR-015_PROJECT_BRIEF.md`, `ADR_STANDARD.md`), and the full standards set. `Nexus.Platform\docs\CURRENT_STATE.md` explicitly demotes `C:\Personal\Documentation\docs\` to "a stale historical snapshot, not a [source of truth]". Supporting authority:
- **Architecture/roadmap data:** `Nexus.Platform\NEXUS_MASTER_ARCHITECTURE.md` + `nexus-roadmap.yaml`.
- **Per-product agent docs:** each repo's `AGENTS.md` + `README.md` (repo-local).
- **Directive/target:** `D:\NEXUS\*.md` (the foundation-reset prompt files) describe the *target* architecture (10-layer platform, products, Atlas, Forge), not the current state.
- Nexus.Developer `docs\` and `architecture\` hold product-local reports (audit, migration plan, SP1-M00 stabilization report) — repo-local authority.

**Conflicts to resolve (human):** multiple non-git copies of master-architecture/roadmap exist (Roadmaps\, Documentation\), and Nexus.Developer's workbook is separate from D:\NEXUS workbook seeds — authority boundaries need an explicit decision (see §20).

---

## 13. Active path references (C:\Personal / D:\NEXUS), classified

Search method: ripgrep fixed-string `C:\Personal` across `C:\Personal` (excluding `.git`, node_modules, bin, obj, .vs, logs, binaries/zips/xlsx). 472 files contain it. Top folders: **DevTools 173, DevTools-ForgeV2 169** (same repo, two worktrees), Documentation 41, **Nexus.Platform 40**, `_stale-recovery-artifacts` 27, **Nexus.Developer 14**, Nexus.Experience 3, Roadmaps 2, Nexus.Intelligence 2, UserSecrets 1. **No file contains the escaped `C:\\Personal` form; no file under `C:\Personal` references `D:\NEXUS`.** (The D:\NEXUS markdown files reference both `D:\NEXUS` and `C:\Personal` as *documentation*.)

Classification of the load-bearing hits (sample-documented):

**ACTIVE_RUNTIME (code that will break if path changes without a code edit):**
- `Nexus.Developer\src\Nexus.Developer.Client.Legacy\NexusDev.ps1:14` `$DevToolsPath = "C:\Personal\Nexus.Developer\src\Nexus.Developer.Client.Legacy"`; `install-shortcut.ps1` (target/WorkingDirectory).
- `DevTools\NexusDev.ps1:14` `$DevToolsPath = "C:\Personal\DevTools"`; `DevTools\install-shortcut.ps1`.
- `DevTools\AI-Config\deepcode.ps1:8` and migrated `Nexus.Developer\...\AI-Config\deepcode.ps1:6` `. "C:\Personal\UserSecrets\Load-Secrets.ps1"`.
- `Nexus.Platform\samples\Nexus.Platform.SmokeHost\StoreSecretResolver.cs:84-85` verbatim `@"C:\Personal\Nexus.Intelligence\src\Nexus.Intelligence.Api"`, `@"C:\Personal\Nexus.Int\src\Nexus.Intelligence.Api"` (host/sample resolver — hard-coded secret-store paths).

**ACTIVE_DEV_TOOL (dev orchestration / workbook tooling coupling):**
- `DevTools\DevBridge\design\DB-M34_FINAL_ACCEPTANCE.md`, `DEVBRIDGE_TRIAL_VS_REAL.md`, `DEVBRIDGE_PRE_REAL_TRANSITION_PLAN.md` — DevBridge treats `C:\Personal\Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` as the **canonical live workbook**.
- `Nexus.Developer\tools\local\start-dev.ps1` and `DevTools\start-dev.ps1` — launch sibling repos from `C:\Personal\Nexus.*` absolute paths.
- `Nexus.Developer\scripts\Import-LegacyDevTools.ps1` default `$SourceRoot = 'C:\Personal\DevTools'`.
- `Nexus.Developer\control\ACTIVE_CHANGES.csv` — change records describing the DevTools→Nexus.Developer migration using `C:\Personal\DevTools` (ledger semantics).

**BUILD (feed/paths in build scripts, not code logic):**
- `Nexus.Platform\pack-local.ps1`, `pack-productcore-local.ps1` `$feed = 'C:\Personal\LocalNuGet'`; `Nexus.Intelligence\pack-local.ps1:15`; `Nexus.Experience\src\Nexus.Products.Chat.Application\...csproj:23` and `Nexus.Developer\src\Nexus.Developer.Core\...csproj:17` comments citing `C:\Personal\LocalNuGet` fallback; `LocalNuGet\*.nupkg` present.
- `nuget.config` in several repos points at local feed (Intelligence's `nuget.config` now omits it; Experience removed machine-local source per its last commit).

**TEST:** `Nexus.Platform\samples\...\StoreSecretResolver.cs` (above) is also test/smoke-host scope.

**DOC_ONLY (docs/architecture describing current or intended topology):**
- `Nexus.Platform\NEXUS_MASTER_ARCHITECTURE.md`, `docs\02_ARCHITECTURE_AND_MODULES.md`, `docs\LOCAL_DEVELOPMENT.md`, `README.md`, `AGENTS.md` of each repo — tables mapping `C:\Personal\<Repo>` ↔ GitHub remote.
- `Nexus.Platform\docs\CURRENT_STATE.md`, `CHANGE_REPORT_v2.2.md`, `Roadmaps\*`, `Documentation\*`, `Nexus.Developer\README.md`, `docs\DEVTOOLS_MIGRATION_PLAN.md`, `AGENTS.md`.

**HISTORICAL (V1-era scripts/records that no longer run):**
- `Nexus.Platform\nexus-v2-restructure.ps1` (params `NexusAIRoot=C:\Personal\NexusAI`, `NexusIntRoot=C:\Personal\Nexus.Int`, `NexusWebRoot=C:\Personal\Nexus.Web`) — one-time V1→V2 restructure.
- `Nexus.Platform\DOCS_CONSOLIDATION_PROMPT.md`, `SQL_PROMPTS_STAGE_1B_2A.md`/`_2B_2C.md`, `FRONTEND_PROMPTS_F0_F4.md` — V1 stage prompts.
- `Nexus.Platform\docs\ADR-014_AZURE_SQL_MIGRATION.md`, `00_DOCUMENTATION_STANDARD.md`, `NEXUS_MASTER_ARCHITECTURE.md` lines still naming `NexusAI`/`Nexus.Int`/`Nexus.Web`/`C:\Personal\NexusAI\docs\`.
- `Nexus.Experience\api_run.log` content root `C:\Personal\Nexus.Web\...` — log from when repo lived at Nexus.Web path.
- `_stale-recovery-artifacts\*`, `_zips\`, `Documentation\` (older copies).

**Rule: DO NOT edit any path today.** Any consolidation to `D:\NEXUS` must first update ACTIVE_RUNTIME + ACTIVE_DEV_TOOL + BUILD path references in the same wave as the move (the directive's "repair solution/project references, build scripts, relative paths, configuration" step).

---

## 14. Proposed D:\NEXUS structure (assessed against actual repo evidence)

Conceptual target from directive: `D:\NEXUS\{Forge, Platform, Products, Atlas, DevelopmentControl, Architecture, Infrastructure, Shared, Tools, Archives}`.

Reconciled with on-disk evidence (this refines — does not force-fit):

| D:\NEXUS slot | Evidence-based owner | Notes |
|---|---|---|
| **Forge\** | `DevTools` repo → **DevTools/DevBridge evolves into Nexus Forge** (directive §3A + §13). `forge-v2-batch02` branch = the provider-migration work; `main` + ForgeV2 worktree should be merged/branched deliberately first. | Keep as its own git repo; MOVE path. Resolve the 2 worktrees → 1 before moving. |
| **Platform\** | `Nexus.Platform` repo (code: shared libs, ProductCore, Delivery contracts). Git history already = NexusAI lineage. | Keep repo; MOVE. Docs split below. |
| **Products\** | `Nexus.Developer` (Developer product), `Nexus.Experience` (Chat product), and — per architecture doc `Nexus.Platform/docs/02_ARCHITECTURE_AND_MODULES.md` — `Nexus.Intelligence` is currently deployed as the `/intelligence/v1` layer. **Decision needed:** is Intelligence a *Platform L04 AI* member or a *Product*? Current docs treat Platform as NuGet-only libraries and Intelligence/Experience as hosts → suggests Platform L04 is implemented *by* Intelligence API, making Intelligence sit at the Platform/Product seam. Human decision required (see §20). | Developer → `Products\Developer`; Experience → `Products\Experience` (or Chat). |
| **Atlas\** | No existing repo. Nexus.Developer's Developer product already implements object/chat-link/development-run domain (F-07-10 foundation). Atlas is future (directive M-17). | No migration now. |
| **DevelopmentControl\** | Currently **two systems**: (a) `Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` + `control\` mirrors (live, single-workbook DevBridge model); (b) `D:\NEXUS\DevelopmentControl\` seeds (Foundation + Products two-workbook model). Directive §8/§9 mandates a **common schema + two-workbook model** (single Foundation + Products resolver). | Map (a)→(b) via governed schema migration (directive Stage 16) — human decision on whether the Developer workbook becomes the Foundation workbook seed. |
| **Architecture\** | Move authority here: `Nexus.Platform\NEXUS_MASTER_ARCHITECTURE.md`, `nexus-roadmap.yaml`, `docs\` (canonical set), plus the `Roadmaps\`/`Documentation\`/`ArchitectureAudit\` copies consolidated into one git-tracked home. | Avoid two homes for the same files (currently 3 differing copies). Human decision on git home (Nexus.Platform vs new docs repo). |
| **Infrastructure\** | `Nexus-Local\infra` (compose), Azure env docs (`D:\NEXUS\Nexus Azure Environment Architecture Update.md`), CI workflow assets (`Nexus.Platform/.github`, `Nexus.Developer/.github`), LocalNuGet feed strategy, `UserSecrets` handling. | Most is embedded in repos already; only loose infra moves here. |
| **Shared\** | Nothing concrete today (Nexus.Platform NuGet packages are the shared layer; Products consume via feed). LocalNuGet feed is the on-disk shared cache. | Design decision only. |
| **Tools\** | DevTools non-DevBridge tooling (NexusDev shell, LayerShell, AI-Config rules) — but these are already migrating into Nexus.Developer `Client.Legacy` + `tools\local` per CHG-20260825-003. | Keep in repos; don't create a parallel Tools tree. |
| **Archives\** | `Documentation\` (V1 docs), `Dataverse\`, `Compilers\`, `_zips\`, `_stale-recovery-artifacts\`, `ArchitectureAudit\`, `D:\architecture\`, `Documentation\change-reports\`. | The recovery vault + zips should be preserved read-only (git history is the real backup once remote-backed). |

**Refinement notes:** (1) `Shared\` and `Tools\` and `Atlas\` are not needed as *physical* folders today — do not create empty scaffolding. (2) `Products\` in V2.3 naming may be `Products\Developer` + `Products\Chat` etc., matching existing project namespaces `Nexus.Products.*`. (3) Nexus.Developer's `Client.Legacy` + DevTools shell = the transition path of the old dev shell into the Developer product — that split is already governed by existing change records. (4) The **workbook** (Development Control) is arguably the highest-value asset and should be the first thing snapshotted.

---

## 15. Repository-by-repository disposition

| Repo / folder | Disposition |
|---|---|
| `C:\Personal\Nexus.Platform` | **CURRENT** (authority) → later **MOVE** to `D:\NEXUS\Platform`; keep repo intact with history. |
| `C:\Personal\Nexus.Developer` | **CURRENT** → later **MOVE** to `D:\NEXUS\Products\Developer` (or `...\Developer`); contains live workbook. Keep separate repo. |
| `C:\Personal\Nexus.Intelligence` | **CURRENT** → **MOVE** (to Products or Platform-L04 — human decision). Keep separate repo (distinct architecture/release boundary from Experience/Platform — see §Evidence). |
| `C:\Personal\Nexus.Experience` | **CURRENT** → **MOVE** to Products. Keep separate repo. (Dirty tree — snapshot first.) |
| `C:\Personal\DevTools` (+ `DevTools-ForgeV2` worktree) | **CURRENT / KEEP-AS-SEPARATE-REPO** → **MOVE** to `D:\NEXUS\Forge`. Evolve DevBridge→Forge. Merge/retire `forge-v2-batch02` deliberately. |
| `C:\Personal\Test` | **DELETE-LATER / IGNORE** (unrelated personal repo, no remote). |
| `_stale-recovery-artifacts\Nexus.Int-fresh` | **ARCHIVE / HISTORICAL** (superseded by Nexus.Intelligence lineage; keep as recovery evidence). |
| `_stale-recovery-artifacts\Nexus.Web-fresh` | **ARCHIVE / HISTORICAL** (superseded by Nexus.Experience). `.git/index.lock` present — verify no dangling write. |
| `_stale-recovery-artifacts\NexusAI-fresh` | **ARCHIVE / HISTORICAL** (superseded by Nexus.Platform lineage; same `arch/v2`). |
| `_stale-recovery-artifacts\_backup\*-worktree` (broken git dirs) | **HISTORICAL / ARCHIVE** (2026-08-20 damaged leftovers; not usable). |
| `C:\Personal\Roadmaps` | **MOVE** → merge into Nexus.Platform (or `D:\NEXUS\Architecture`), then delete. |
| `C:\Personal\Documentation` | **ARCHIVE** (stale doc snapshot; keep for V1 record). |
| `C:\Personal\ArchitectureAudit`, `D:\architecture` | **ARCHIVE** into docs/Archives. |
| `C:\Personal\Nexus-Local` | **REVIEW** → MOVE `infra\` to `D:\NEXUS\Infrastructure` or archive (`.env` contains local secrets — handle separately). |
| `C:\Personal\Dataverse`, `C:\Personal\Compilers` | **ARCHIVE** (V1 assets); Compilers relation to DevTools **UNKNOWN** — review. |
| `C:\Personal\LocalNuGet` | **EXTRACT/DEACTIVATE** once GitHub Packages reachable (M-08-1.1). Keep as read-only fallback meanwhile. |
| `C:\Personal\UserSecrets` | **REVIEW / MOVE** to proper secret management (not scanned). |
| `C:\Personal\Temp`, `C:\Personal\_backup` (top-level empty), empty `Nexus.Int*`/`Nexus.Web*`/`NexusAI*` dirs | **DELETE-LATER** (empty/scratch). |
| `C:\Personal\_zips` | **ARCHIVE / DELETE-LATER** after git-history verification. |
| `D:\NEXUS` | **TARGET ROOT** — build out per approved waves; do not pre-create empty dirs. |

---

## 16. History-preservation strategy

- **MOVE intact (no merge needed):** Nexus.Platform, Nexus.Developer, Nexus.Intelligence, Nexus.Experience, DevTools. Each already has its own GitHub remote + full history. Moving a folder to `D:\NEXUS\...` preserves `.git` history if done as a filesystem move (not re-clone). **Preferred method:** `git clone --mirror`/bare + worktree, or plain file move of the directory, never copy-without-`.git`, to keep SHAs/remotes.
- **Genuinely related repos (share lineage but should NOT be merged as duplicate repos):**
  - NexusAI ↔ Nexus.Platform — *same repo lineage*; the stale `NexusAI-fresh` clone must NOT be merged (objects already in Platform). Just archive the clone.
  - Nexus-Int ↔ Nexus.Intelligence, Nexus-web ↔ Nexus.Experience — same pattern; archive stale clones, keep successor repos.
- **Genuinely needing *content* merge (not git merge):**
  - `Roadmaps\*`, `Documentation\*`, `ArchitectureAudit\*` → fold authoritative copies into Nexus.Platform (or Architecture home) and archive the rest.
  - Two-workbook Development Control (Nexus.Developer live workbook ↔ D:\NEXUS seeds) → governed **schema migration** (directive Stage 16), not a file copy.
  - `DevTools` branches `main` ↔ `forge-v2-batch02` → a real **git merge/PR** within DevTools before any move (both worktrees currently dirty with tracked-build-debris noise).
- **Dirty-tree preservation before any move:** Nexus.Experience (313 files) and Nexus.Intelligence (Roles feature) need a commit/stash first; Nexus.Developer workbook + unpushed commit `5913bc5` need a push + commit first.
- **Do NOT merge:** Nexus.Developer, Nexus.Intelligence, Nexus.Experience, DevTools into one monorepo. Evidence (architecture ADRs, separate remotes, separate release cadences/CI, distinct product namespaces) supports **KEEP-AS-SEPARATE-REPOS**.

---

## 17. Migration order (proposed sequence only — no action taken)

Per the directive's wave discipline (Baseline → Discover → Classify → Propose → Human-approve → Migrate wave → Build → Test → Verify → Evidence):

1. **Wave 0 — Baseline & snapshot (do first):** commit/push Nexus.Developer workbook + `5913bc5`; stash-or-commit Nexus.Experience + Nexus.Intelligence dirty trees; snapshot all five repos + the workbook to a dated backup; record SHA manifest.
2. **Wave 1 — Authority consolidation (docs only, no code):** designate Nexus.Platform as the single docs/roadmap home; archive Roadmaps\, Documentation\, ArchitectureAudit\, D:\architecture\ copies; reconcile the three differing `NEXUS_MASTER_ARCHITECTURE.md`/`nexus-roadmap.yaml` copies.
3. **Wave 2 — DevTools/Forge normalization:** fix DevTools repo hygiene (add `.gitignore`, untrack bin/obj), merge `forge-v2-batch02` → `main` (PR), retire the second worktree, tag a Forge baseline.
4. **Wave 3 — Development Control schema migration:** map live Developer workbook → Foundation/Products two-workbook model; migrate records (directive Stage 16); leave D:\NEXUS seeds as the target until approved.
5. **Wave 4 — Physical moves under D:\NEXUS (after human approval):** move `Nexus.Platform` → `D:\NEXUS\Platform`; `DevTools` → `D:\NEXUS\Forge`; `Nexus.Developer` → `D:\NEXUS\Products\Developer`; `Nexus.Experience` → `D:\NEXUS\Products\...`; `Nexus.Intelligence` per §20 decision. Move as whole directories with `.git`. In the same wave, update ACTIVE_RUNTIME/ACTIVE_DEV_TOOL/BUILD path references (§13) and repair slnx/project/script/config paths.
6. **Wave 5 — Archive sweep:** move `Documentation`, `Dataverse`, `Compilers`, `_zips`, `_stale-recovery-artifacts`, `ArchitectureAudit`, `D:\architecture`, empty dirs to `D:\NEXUS\Archives` (read-only) or delete empties.
7. **Wave 6 — Infrastructure:** LocalNuGet decommission after GitHub Packages verified reachable (M-08-1.1); UserSecrets → proper secret store; Nexus-Local infra decision.
8. Parallel lanes (directive §14) may run Waves 4–6 per-repo after the schema/authority waves unblock them.

---

## 18. Rollback strategy

- **Per-repo safety net:** every active repo has a GitHub remote with pushed state; before any move, `git push` current HEAD + create a tag (e.g. `pre-dnexus-migration-20260907`). Rollback = re-clone from remote or restore the tag.
- **Directory moves:** because each repo moves as a complete directory including `.git`, rollback is a file move back to `C:\Personal\<name>` (or re-clone). Keep the old `C:\Personal` path on disk (read-only) until Wave 5 archiving.
- **Workbook:** the DevBridge design already mandates byte-identical backups + SHA manifest before governed writes (`control/CONTROL_MANIFEST.json`); keep a dated xlsx backup before schema migration.
- **Docs consolidation:** the three differing copies are preserved in git (Nexus.Platform) + zips + stale folders before deleting any copy.
- **Known-good marker:** Nexus.Platform HEAD `5f538c2` + tag `pre-l07-split-20260905`; DevTools tag `pre-forge-hardening-20260905` — use as rollback baselines.
- **Golden rule (directive §22):** at every stage retain ability to return to the known-good pre-reset state; never one-shot restructure.

---

## 19. Risks

1. **Nexus.Experience dirty tree (313 modified + 9 untracked)** — high loss risk if moved/copied carelessly; includes real WIP pages + mixed build artifacts. Needs a clean/commit first.
2. **Nexus.Developer unpushed commit `5913bc5`** (ahead 1) + modified live workbook — only local copy of DevelopmentControl concurrency stabilization work.
3. **Nexus.Intelligence uncommitted `Roles/` feature** — in-progress work would be lost on a re-clone.
4. **DevTools tracks 151 bin/obj files and has no `.gitignore`** — two worktrees both dirty with build debris; merging `forge-v2-batch02` will be noisy. No `.git` isolation issue, but hygiene risk.
5. **Hard-coded ACTIVE_RUNTIME paths** (NexusDev.ps1, install-shortcut.ps1, deepcode.ps1→UserSecrets, StoreSecretResolver.cs, DevBridge workbook path, start-dev.ps1) will break the moment folders move unless updated in the same wave (§13).
6. **Three divergent copies of the master architecture + roadmap** (Platform/Roadmaps/Documentation, all different md5) — risk of acting on stale authority if not reconciled first.
7. **`_stale-recovery-artifacts` & `_zips`** contain the only local record of the 2026-08-20 incident and pre-rename state — delete/compress only after git-history verification and human sign-off. `Nexus.Web-fresh` has a stale `.git/index.lock` (aborted write).
8. **UserSecrets and Nexus-Local `infra/.env`** hold real secrets referenced by absolute paths — must be migrated to managed secret storage, not moved as plaintext. (Not scanned by this report by design.)
9. **Authority ambiguity for Intelligence** (Platform L04 vs Product) — wrong placement in the target structure will propagate through the 10-layer dependency rules.
10. **Broken `_backup\*-worktree` git dirs** — git-unrecognizable; don't attempt repair/merge; preserve as-is.
11. **DevTools `main` vs `forge-v2-batch02`** divergence — the ForgeV2 worktree holds provider-boundary commits not on main; retiring the wrong worktree loses them.
12. **Old repo-name references in docs/scripts** (`NexusAI`, `Nexus.Int`, `Nexus.Web`, `C:\Personal\NexusAI`) could mislead future automation if a script is resurrected (nexus-v2-restructure.ps1 is one-time and must not be re-run).

---

## 20. Human decisions required (Durai must answer)

1. **Target homes:** Approve the mapped homes in §14/§15 — specifically `Nexus.Platform → D:\NEXUS\Platform`, `DevTools/DevBridge → D:\NEXUS\Forge`, `Nexus.Developer → D:\NEXUS\Products\Developer`, `Nexus.Experience → D:\NEXUS\Products\Experience` (or Chat), and whether to keep Intelligence in `Products` vs treat it as Platform L04 AI.
2. **Docs authority home:** Is `Nexus.Platform\docs` + `NEXUS_MASTER_ARCHITECTURE.md` + `nexus-roadmap.yaml` the single authority (this report's recommendation), or should architecture move into a new `D:\NEXUS\Architecture` git repo separate from Platform code?
3. **Workbook model:** Authorize the Development Control migration from the single live `Nexus.Developer\NEXUS_DEVELOPMENT_CONTROL.xlsx` (+ `control\` mirrors) to the Foundation/Products **two-workbook schema** seeded at `D:\NEXUS\DevelopmentControl`, and confirm which workbook is the parent/authority during transition.
4. **DevTools/Forge branch plan:** Approve merging `forge-v2-batch02` → `main` (and retiring the second worktree) as the Forge baseline, and confirm DevBridge is the intended permanent "Nexus Forge" home.
5. **Archive scope & retention:** Which of `Documentation\`, `Dataverse\`, `Compilers\`, `_zips\`, `_stale-recovery-artifacts\`, `ArchitectureAudit\`, `D:\architecture\` to archive under `D:\NEXUS\Archives` vs delete; and whether the fitness-challenge `Test` repo should be kept/deleted.
6. **Empty-dir cleanup:** Confirm deletion of the 6 empty `Nexus.Int*`/`Nexus.Web*`/`NexusAI*` dirs and top-level `_backup`.
7. **Nexus.Experience hygiene:** Approve the approach for its 313-file dirty tree (commit-as-WIP vs selective commit) and the fate of the legacy `src\Nexus.Web.Client` rename remnant.
8. **LocalNuGet decommission:** Authorize removing `C:\Personal\LocalNuGet` as a source once GitHub Packages is verified reachable from CI (M-08-1.1), and decide whether the LocalNuGet folder is archived or deleted.
9. **UserSecrets / Nexus-Local `.env`:** Direct the migration path for these secrets (this report intentionally did not read them).
10. **Wave start:** Approve the §17 Wave 0 baseline actions (push unpushed commits, snapshot + SHA manifest, tag all repos) to unblock physical migration.

---

### Appendix — quick evidence index
- Lineage: NexusAI root `877e8e7` in Platform history; `890c37e` ancestor of Platform HEAD; `arch/v2` = `4b15f71` in both NexusAI-fresh and Platform.
- Lineage: Nexus-Int `8141cd0` + `b27cd26` present in Nexus.Intelligence; rename commits `564c65e`, `87b4de5`.
- Lineage: Nexus-web root `2c0b94c` in Nexus.Experience; identical remote branch sets.
- Worktrees: `git worktree list` in DevTools and DevTools-ForgeV2 both list the other worktree → same repo.
- Authority: md5 mismatches across Platform/Roadmaps/Documentation copies of architecture + roadmap; `Roadmaps\nexus-roadmap.yaml` self-declares NOT git-tracked; Platform `docs/CURRENT_STATE.md` labels `Documentation\docs` stale.
- Directive: `D:\NEXUS\Claude Prompt — Nexus Foundation Reset and Migration.md` §§2,3,8,13,15,21,22,23.
