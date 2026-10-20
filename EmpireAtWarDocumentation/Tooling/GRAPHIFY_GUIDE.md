---
type: technical-reference
system:
  - tooling
updated: 2026-10-20
---
# Graphify

## Rules

- Canonical operations: `Tools/Graphify/README.md`; repository `AGENTS.md` defines agent usage.
- The configured MCP checks graph freshness before reading; verify graph findings against live source.
- Index scope is configured for `Assets/Scripts`; this is not a complete Obsidian knowledge index.
- `graphify-out/` is generated local state, never authoritative documentation.
- Use the configured project watcher/adapter; do not add a competing stock watcher or Git rebuild hook.

## Important Values

- Current documented adapter: Graphify `0.9.54`; per-user `empire-at-war Graphify` task.
- Background scan interval: 3 s; changes settle for one scan before indexing.
- Configured MCP queries can update on demand and report failures instead of serving stale results.
- Setup/status/recovery commands and detailed exclusions stay in the repository manual; recheck it before operating.

## Files

- `Tools/Graphify/README.md`
- `Tools/Graphify/run.py`
- `Tools/Graphify/watch.json`
- [[Rules/AGENTS]]
- [[Tooling/00_Tooling]]
