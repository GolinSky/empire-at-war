---
type: index
system:
  - game-design
updated: 2026-10-20
---
# GameDesign

## Rules

- Current references below were checked against code and serialized assets on 2026-10-20. They describe implementation, not proof of balanced gameplay.
- Runtime data remains authoritative; each reference identifies its source files and scope. Reverify values after balance changes.
- Proposals, external research and import snapshots retain their own status; do not copy their values into assets without a design decision.

## Files

| Domain | Current reference | Covers |
|---|---|---|
| Economy | [[GameDesign/Economy]] | Starting money, income, spending, refunds, ownership |
| Production | [[GameDesign/Production_And_Research]] | Queues, station progression, research, population and limits |
| Structures | [[GameDesign/Capture_And_Structures]] | Capture, mining, facilities and reinforcement placement |
| Units | [[GameDesign/Units_And_Factions]] | Roster, faction identity, unit data ownership and stat interpretation |
| Combat | [[GameDesign/Combat]] | Weapons, targeting, damage, shields and hardpoints |
| Abilities | [[GameDesign/Abilities_And_Superweapons]] | Activation, timing, restrictions, catalog and superweapons |
| Modes | [[GameDesign/Modes_And_Victory]] | Skirmish, Survival, Simulation and outcomes |
| Input/orders | [[GameDesign/Commands_And_Controls]] | Current selection, bindings, movement and order behavior |
| Visibility | [[GameDesign/Visibility]] | Fog, radar, cloaking and detection |
| Persistence | [[GameDesign/Save_And_Load]] | Save/load, autosave, restored state and exclusions |

- [[GameDesign/Proposals/Empire Fleet Import]]; [[GameDesign/Proposals/Rebel units]] — preserved proposals, including superseded choices.
- [[GameDesign/References/Republic at War - Republic Unit Stats Reference]]; [[GameDesign/References/Unit description]] — external/unverified unit references.
- [[GameDesign/References/Battle GameDesign basics]]; [[GameDesign/References/Empire at War, Republic at War combat — short version]] — external combat research.
- [[GameDesign/References/CONTROLS_ANALYSIS]] — original EaW controls, not current project bindings.
- [[AssetPipeline/00_AssetPipeline]] — model conversion, source mapping and import evidence.
- [[Architecture/00_Architecture]]; [[Tooling/00_Tooling]]; [[TODOs/TODOs]] — implementation, authoring tools and execution.
- [[Artifacts/Audits/Unit_Stats_2026-09-27/Unit Stats Comparison - 2026-09-27]] — historical roster snapshot.
