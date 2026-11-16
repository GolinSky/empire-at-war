---
type: index
system:
  - architecture
updated: 2026-11-16
---
# Architecture

## Rules

- This area owns implementation contracts and responsibilities; [[GameDesign/00_GameDesign]] owns player-facing rules.
- Repository `AGENTS.md` takes precedence over older reference notes. MVP is optional, not a mandatory entity decomposition.
- Dated analysis is retained under Artifacts; outstanding changes remain in their active plans.

## Files

- [[Architecture/Online_Skirmish_Lobby]] — automatic seating up to eight humans, consistent snapshots and dropdown ownership.
- [[Architecture/Multiplayer_Reconnect]] — persisted client recovery, host identity/deadlines and loading lifecycle.

- [[Architecture/PROJECT_ORGANIZATION]] — asset/script placement policy; unresolved policy work: [[TODOs/Refactoring/Project_Organization_Followup]].
- [[Architecture/VCONTAINER_LIFECYCLE]] — scopes, initialization and disposal.
- [[Architecture/UI_REFACTORING_PLAYBOOK]] and [[Architecture/UI_REFACTORING_PLAYBOOK - Research]] — focused UI migration.
- [[Architecture/POPUP_SYSTEM]] — popup stack, feature services, and the shared Invite Friends picker. Implementation → [[Done/Refactoring/Shared_Invite_Friends_Popup_Plan]].
- [[Architecture/SELECTION_SYSTEM]] — selection responsibilities; current behavior: [[GameDesign/Commands_And_Controls]].
- [[Architecture/Map_Generation]] and [[Architecture/Map_Archetypes]] — layout constraints and archetype ownership.
- [[Architecture/Enemy_AI_Objectives]] — battle AI objectives, production and fleet decisions.
- [[Architecture/TACTICAL_MARKERS]] — team marker authority, visibility and allied AI response.
- [[Rules/AGENTS]]; [[Rules/UI_UX_GUIDELINES]]; [[Rules/UI_CODE_BUILD_GUIDE]]; [[Rules/UNITY_JOBS_AND_THREADING_RULES]].
- [[AssetPipeline/00_AssetPipeline]] — import workflows and integration records.
- [[Tooling/00_Tooling]] — tool entrypoints and canonical repository manuals.
- [[Artifacts/00_Artifacts]] — historical audit evidence, not current implementation contracts.
