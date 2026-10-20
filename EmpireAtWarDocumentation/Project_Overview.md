---
type: reference
system:
  - project
verified: 2026-10-20
verification: project-files
---
# Empire At War

## Goal

- RTS space battles with faction rosters, production, economy, capture sites and tactical combat.
- Current gameplay rules: [[GameDesign/00_GameDesign]]; current work: [[TODOs/TODOs]].

## Important Values

| Project dependency | Recorded version |
|---|---|
| Unity Editor | `6000.4.7f1` |
| VContainer | `1.19.0` |
| Universal Render Pipeline | `17.4.0` |
| Addressables | `2.9.1` |
| Input System | `1.19.0` |
| Unity Pipeline | `0.6.0-exp.1` |

## Rules

- Versions above come from `ProjectSettings/ProjectVersion.txt` and `Packages/manifest.json`; reverify after package upgrades.
- MVP is optional for suitable stateful UI. Entity composition and communication follow repository `AGENTS.md`.
- Agent Unity automation uses the official `unity` CLI / Pipeline bridge. Presence of a legacy package does not change that policy.
- Docs explain current sources and recorded decisions; completed plans and import records remain dated evidence.

## Files

- [[00_Home]]
- [[Architecture/00_Architecture]]
- [[Architecture/PROJECT_ORGANIZATION]]
- [[Rules/AGENTS]]
- [[Tooling/00_Tooling]]
- `README.md`, `AGENTS.md`, `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`.
