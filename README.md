# Nexus.Developer — Layer 07 bootstrap

Nexus.Developer is the system of record for defining, planning and building software.
This bootstrap introduces the temporary workbook-era development control, machine-readable
mirrors, preflight conflict analysis and a safe DevTools import path.

The permanent work graph is built at `M-07-1.1`. This package deliberately does not create
empty `.NET` projects merely to make the repository look complete. The architecture-approved
projects are created with real behavior when that milestone starts:

- `Nexus.Developer.Contracts`
- `Nexus.Developer.Core`
- `Nexus.Developer.Graph`
- `Nexus.Developer.Orchestration`
- `Nexus.Developer.Infrastructure`
- `Nexus.Developer.Api`
- `Nexus.Developer.Client`

## Naming decision

The Layer 07 human name is **DEVELOPER** and the repository/namespace name is
`Nexus.Developer`, as specified by Nexus architecture v2.2. `Nexus.Development` is not
created as a second competing name because that would contradict the roadmap, architecture
documents, schema and future package names.

## Start here

1. Read `AGENTS.md`.
2. Open `NEXUS_DEVELOPMENT_CONTROL_v1.0.xlsx`.
3. Read `control/ACTIVE_CHANGES.csv`.
4. Run `scripts/Invoke-NexusPreflight.ps1` with the exact proposed scope.
5. Reserve only after the verdict permits work.
6. Complete the change using `scripts/Complete-NexusChange.ps1`.

## DevTools migration

The current PowerShell/WPF source from `C:\Personal\DevTools` was not included in the three
archives audited on 2026-08-25. `scripts/Import-LegacyDevTools.ps1` is therefore copy-first
and verification-first. It never deletes or moves the source. Run it only after the real
DevTools folder is available.

The shell remains a common launcher. Layer-owned implementation stays in its owning
repository and is reached through the existing local-file contract first; an HTTP/API adapter
can replace that contract later without redesigning the UI.

