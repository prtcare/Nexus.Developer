# DevTools → Nexus.Developer migration plan

## Boundary

The current `C:\Personal\DevTools` desktop remains a common shell/launcher. Its Layer 07
home is the transitional `Nexus.Developer.Client.Legacy` adapter. Files that implement a
different Nexus layer remain in that layer's repository; only links/configuration remain
in the shell.

## Verified source files required

| Source | Destination | Role |
|---|---|---|
| `NexusDev.ps1` | `src\Nexus.Developer.Client.Legacy\NexusDev.ps1` | Main WPF shell |
| `LayerShell.ps1` | `src\Nexus.Developer.Client.Legacy\LayerShell.ps1` | Layer-page shell |
| `config\layers.json` | `src\Nexus.Developer.Client.Legacy\config\layers.json` | Layer registry/links |
| `notify.ps1` | `tools\local\notify.ps1` | Notification utility |
| `checkpoint.ps1` | `tools\local\checkpoint.ps1` | Checkpoint utility |
| `verify.ps1` | `tools\local\verify.ps1` | Verification utility |
| `start-dev.ps1` (optional) | `tools\local\start-dev.ps1` | Launcher |

## Safe sequence

1. Attach or make available the actual DevTools folder.
2. Reserve change `CHG-20260825-003` and run preflight.
3. Run `Import-LegacyDevTools.ps1 -WhatIf`.
4. Run the import without `-WhatIf`; it copies, never moves or deletes.
5. Inspect `control\migration-report-*.csv` and verify every SHA-256 pair matches.
6. Rework hard-coded paths into one configuration boundary; preserve current UI behavior.
7. Launch, test every layer card and tool, and record evidence.
8. Only after human review, retire the old folder as a separate recoverable operation.

No source deletion is included in this package.

