# Development control mirrors

`NEXUS_DEVELOPMENT_CONTROL_v1.0.xlsx` is the temporary human-facing system of record.
The files in this folder are machine-readable mirrors for preflight and automation.

- `CONTROL_MANIFEST.json` records source versions and the workbook checksum.
- `ROADMAP_LEDGER.csv` mirrors current roadmap/control rows.
- `ACTIVE_CHANGES.csv` mirrors the change ledger.

Until `M-07-1.1` imports this state into the permanent database-backed work graph, any
governed edit must update the workbook and the corresponding mirror in the same change.
If they disagree, stop. Do not silently choose one.

