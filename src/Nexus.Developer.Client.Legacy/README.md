# Legacy client adapter

This folder receives the verified PowerShell/WPF common shell from `C:\Personal\DevTools`.
It is a transitional Layer 07 client adapter, not the work graph and not a home for other
layers' implementations.

The import script copies only the reviewed shell/config files and preserves the source.
Layer cards continue to call layer-owned local files first. A later API adapter must keep
the UI contract stable.

