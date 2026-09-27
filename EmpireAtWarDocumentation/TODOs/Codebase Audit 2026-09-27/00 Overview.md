---
tags:
  - code-audit
  - refactoring
  - index
created: 2026-09-27
status: proposed
---
# Codebase audit — 27 September 2026

17 actionable findings across four categories. Each detail note includes evidence, a bounded solution, confidence and future acceptance checks.

> [!important] Review only
> No source, prefab, scene, settings, Unity state or Obsidian configuration was changed. No tests, generators, builds or Unity commands were run. Existing work by another agent was left alone. The only writes for this task are these nine new audit notes.

## Quick issue → solution list

P2 = worthwhile focused refactor; P3 = lower-priority simplification/organization. These are maintenance priorities, not claims of reproduced runtime failures.

| ID | Priority | Finding | Proposed solution |
|---|---|---|---|
| [[TODOs/Codebase Audit 2026-09-27/01 Complexity - State and Input#C1 — Make time-state ownership explicit\|C1]] | P2 | Two meanings of game time mode | Make requested/effective time explicit; preserve menu semantics |
| [[TODOs/Codebase Audit 2026-09-27/01 Complexity - State and Input#C2 — InputService handles several independent jobs\|C2]] | P2 | Input service mixes gestures, commands and UI hits | Separate gesture state and UI hit detection |
| [[TODOs/Codebase Audit 2026-09-27/02 Complexity - Combat and Fog#C3 — CombatAttackCoordinator owns two batching pipelines plus telemetry\|C3]] | P2 | Combat coordinator owns two batch pipelines and telemetry | Extract target-selection batching; preserve ordered commitment |
| [[TODOs/Codebase Audit 2026-09-27/02 Complexity - Combat and Fog#C4 — Fog computation, projection and rendering are mixed\|C4]] | P2 | Fog mixes rendering, rules and duplicated projection | Share projection and isolate visibility-grid model |
| [[TODOs/Codebase Audit 2026-09-27/03 Duplication - Coordinates and Population#D1 — Formation-coordinate adapters are repeated\|D1]] | P3 | Repeated formation-coordinate conversions | One adapter with explicit height policy |
| [[TODOs/Codebase Audit 2026-09-27/03 Duplication - Coordinates and Population#D2 — Segment-distance geometry is implemented twice\|D2]] | P3 | Repeated segment-distance mathematics | One planar primitive; settle tiny-segment semantics |
| [[TODOs/Codebase Audit 2026-09-27/03 Duplication - Coordinates and Population#D3 — Capture sites and reinforcement zones repeat ship counting\|D3]] | P2 | Repeated ship counts for capture areas | Shared ship-population query |
| [[TODOs/Codebase Audit 2026-09-27/04 Duplication - Markers Keys and Tooling#D4 — Minimap presenters repeat marker lifetime bookkeeping\|D4]] | P3 | Repeated minimap marker lifetime handling | Small marker collection; preserve separate visibility rules |
| [[TODOs/Codebase Audit 2026-09-27/04 Duplication - Markers Keys and Tooling#D5 — Enemy reservation keys are independently constructed\|D5]] | P2 | Reservation keys constructed in several places | One canonical key representation |
| [[TODOs/Codebase Audit 2026-09-27/04 Duplication - Markers Keys and Tooling#D6 — Audio-generation scripts duplicate WAV encoding\|D6]] | P3 | Duplicate offline WAV encoders | Shared Tools/Audio encoder |
| [[TODOs/Codebase Audit 2026-09-27/05 Cleanup - Unused Candidates#L1 — CollectionUtility has no located callers\|L1]] | P3 | Shuffle utility has no located callers | Confirm external usage, then remove or simplify |
| [[TODOs/Codebase Audit 2026-09-27/05 Cleanup - Unused Candidates#L2 — DependencyBuilder exposes unused custom-path state\|L2]] | P3 | Unused custom-path branch and cached builder name | Remove unused option; compute each binding path directly |
| [[TODOs/Codebase Audit 2026-09-27/06 Structure - Placement and Type Files#L3 — Domain placement and vendor ownership are unclear\|L3]] | P3 | Selection contract and vendor sources misplaced | Clarify domain/vendor boundaries with safe later moves |
| [[TODOs/Codebase Audit 2026-09-27/06 Structure - Placement and Type Files#L4 — Multiple top-level types remain in single files\|L4]] | P3 | Several top-level types per file | Split project-owned types into matching files |
| [[TODOs/Codebase Audit 2026-09-27/07 Reuse - Combat and Radar Boundaries#R1 — WeaponModel contains engine data and random sampling\|R1]] | P2 | Weapon model depends on engine assets and random API | Plain combat data and explicit random input |
| [[TODOs/Codebase Audit 2026-09-27/07 Reuse - Combat and Radar Boundaries#R2 — RadarModel owns Unity layer selection\|R2]] | P2 | Radar model exposes Unity layer masks | Resolve physics masks in existing layer/Unity boundary |
| [[TODOs/Codebase Audit 2026-09-27/08 Reuse - UI Assets and Extraction Rules#R3 — ShipUiModel doubles as an icon repository\|R3]] | P2 | Selection UI model also resolves Sprites | Reuse icon data through a presentation provider |

## Read by category

| Category | Note 1 | Note 2 |
|---|---|---|
| Complexity | [[TODOs/Codebase Audit 2026-09-27/01 Complexity - State and Input\|State and input]] | [[TODOs/Codebase Audit 2026-09-27/02 Complexity - Combat and Fog\|Combat and fog]] |
| Duplication | [[TODOs/Codebase Audit 2026-09-27/03 Duplication - Coordinates and Population\|Coordinates and population]] | [[TODOs/Codebase Audit 2026-09-27/04 Duplication - Markers Keys and Tooling\|Markers, keys and tooling]] |
| Cleanup and structure | [[TODOs/Codebase Audit 2026-09-27/05 Cleanup - Unused Candidates\|Unused candidates]] | [[TODOs/Codebase Audit 2026-09-27/06 Structure - Placement and Type Files\|Placement and type files]] |
| Reusable boundaries | [[TODOs/Codebase Audit 2026-09-27/07 Reuse - Combat and Radar Boundaries\|Combat and radar]] | [[TODOs/Codebase Audit 2026-09-27/08 Reuse - UI Assets and Extraction Rules\|UI assets and extraction rules]] |

## Coverage and evidence

- Snapshot: working tree based on commit `f61f9e49`. Other-agent modifications existed at the start, including prefabs, data assets and SquadronViewPrefabBuilder.
- Inventoried all **714 C# files under Assets/Scripts**: 172 Components, 301 Entities, 143 Services, 26 Editor, 72 Tests. These counts include generated and vendored files under that tree.
- Applied repository-wide source searches for duplication/extraction candidates, Unity dependencies in models, old/deprecated markers, implicit component lookups, helper usage and file/type organization. Used file sizes only to select review candidates, not as proof of bad design.
- Inspected live symbol bodies and references in the relevant CoreGame, combat, input, fog, navigation, map, orders, selection, minimap, AI reservation, DI, radar, UI model and utility code. Read relevant test source to understand intended behavior; did not execute it.
- Inventoried Tools scripts and inspected shared audio encoding plus tooling boundaries. Inventoried vendor C# under Plugins (532) and ThirdParty (81); vendor implementations, every test body, shaders and serialized assets were **not exhaustively audited**. This is a broad first-party architecture review, not a claim that every line has been proven correct.
- Graphify's existing 4,837-node graph was used for navigation. Some names/locations were stale and some queries truncated; no conclusion relies solely on graph output. Concrete follow-up vocabulary included InputService, CollectionUtility and CombatAttackCoordinator. Live source is the authority.
- Serena supplied C# symbols/reference checks; exact repository text searches supplemented them. No graph rebuild or saved graph artifact was produced.
- External API compatibility was not evaluated. No claim of package deprecation or runtime breakage is made. Skill/tool token cost was not exposed; no separate graph generation was run.

## Important distinctions

**C1 is not a confirmed bug.** The existing menu-pause test deliberately preserves stored mode. Refactor naming/ownership while preserving that policy unless a behavior change is separately approved.

**Unused candidates are not deletion authorization.** Source references do not exclude reflection, external consumers or serialized bindings.

**Reuse does not mean a giant Utils folder.** Stateless math belongs in narrow helpers; shared lifetime/query work belongs in services; domain policy remains in models; Unity assets remain at integration boundaries.

**No blanket deprecation finding.** SceneReference is used; its Obsolete annotations protect editor callbacks. The toolbar's reflection dependency is an upgrade-review candidate only.

## Start here

For small independent changes, prioritize D1, D3, D5 and D6. For architecture, prioritize R1–R3, then one of C2–C4 at a time. Treat C1 as a semantic clarification first. Keep cleanup separate from behavior changes.

Related earlier plans: [[TODOs/SkirmishOrchestrator_Refactoring_Plan]], [[TODOs/UI_Service_Refactoring]], [[TODOs/Ship_Movement_Simplification_Plan]], [[TODOs/Battle_Attack_Optimization_Plan]]. Their current implementation status was not inferred from their titles.

The solutions here are proposals only; all future verification listed in the detail notes remains unexecuted.
