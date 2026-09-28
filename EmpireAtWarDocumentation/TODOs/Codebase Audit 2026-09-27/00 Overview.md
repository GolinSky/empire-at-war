---
tags:
  - code-audit
  - refactoring
  - index
created: 2026-09-27
updated: 2026-09-28
status: implemented
---

# Codebase audit — implementation status

- The attached correction plan has been implemented for **all 17 entries**.
- The follow-up request authorized completing D3 and D4, focused tests, and committing this task's changes.

- [[TODOs/Codebase Audit 2026-09-27/09 Implementation Results|Read implementation results and verification]]

> [!warning] Follow-up sweep 2026-09-28
> All 17 entries were re-verified as present at f3fdf3d4. Two leftovers remain: an empty `Utils/Scenes` folder (from L3) and the fog models loose in the `Entities` root (from C4). The same issue classes still exist elsewhere: 79 multi-type files, silent null returns, property injection in services, a string-keyed pipeline dictionary, and hand-rolled owner counting in BattleVictoryService.
> [[TODOs/Codebase Audit 2026-09-27/10 Follow-up Sweep 2026-09-28|Read the follow-up sweep]]

| ID | Status | Result |
|---|---|---|
| C1 | Implemented | Restores requested pause/speed after menu close; ignores controls inside menu; requested/effective naming is explicit |
| C2 | Implemented | Injected UiHitTest reuses storage; drag/pinch state moved to PointerGestureState |
| C3 | Implemented | TargetSelectionBatch owns targeting/buffers; coordinator retains registration/generation and due-event ordering |
| C4 | Implemented | Shared position-to-pixel mapping, sharedMesh, no GetComponent, explicit saved renderer binding; pure FogVisibilityGridModel owns grid/history/fade (follow-up) |
| D1 | Implemented | FormationConversion used by every Vector3↔FormationPoint call site (orders, enemy AI, navigation, feedback UI — follow-up); height-preserving order output retained |
| D2 | Implemented | Small shared squared segment-distance helper; MapGeometry takes square root |
| D3 | Implemented | ShipPopulation.CountShips shares area/faction counting; existing squadron weighting is preserved |
| D4 | Implemented | Composed MiniMapMarkerCollection shares registration/cleanup; presenter visibility policies remain local |
| D5 | Implemented | UnitLimitKey replaces string keys and normalizes subclasses to the request kind |
| D6 | Implemented | Shared offline WAV encoder; weapon RMS/seam reporting retained |
| L1 | Implemented | Unused CollectionUtility and its empty folder deleted through Unity AssetDatabase |
| L2 | Implemented | Removed BuildPathToFile and cached name; compute name per binding |
| L3 | Implemented | SelectionType moved; empty folder removed; vendor code moved with metadata/license material |
| L4 | Implemented | Listed selection, purchase, random and radar types split into matching files |
| R1 | Implemented | WeaponModel takes plain accuracy/ranges and caller-supplied roll; engine assets remain in component |
| R2 | Implemented | Dead masks deleted; constructor injection and installer bindings updated |
| R3 | Implemented | Injected icon provider backed by ShipUiData; model/observer no longer resolve Sprites |

## Corrections to the original audit

- **C1 was a real bug.** The existing menu-pause test did not cover closing the menu and the next control press.
- **D2 was overstated.** Both zero-length paths return distance to the start.
  - No behavior-design decision was necessary for this small consolidation.
- **R2 was dead code.** No new layer service was needed.
- **C4 also cloned meshes through mesh access and used forbidden component lookups.** Those uses are removed.
- **L3's old NavigationService folder contained only SelectionType.** It has been removed after the Unity-aware move.

## Category notes

| Category | Note 1 | Note 2 |
|---|---|---|
| Complexity | [[TODOs/Codebase Audit 2026-09-27/01 Complexity - State and Input\|State/input]] | [[TODOs/Codebase Audit 2026-09-27/02 Complexity - Combat and Fog\|Combat/fog]] |
| Duplication | [[TODOs/Codebase Audit 2026-09-27/03 Duplication - Coordinates and Population\|Coordinates/population]] | [[TODOs/Codebase Audit 2026-09-27/04 Duplication - Markers Keys and Tooling\|Markers/keys/tooling]] |
| Cleanup | [[TODOs/Codebase Audit 2026-09-27/05 Cleanup - Unused Candidates\|Unused candidates]] | [[TODOs/Codebase Audit 2026-09-27/06 Structure - Placement and Type Files\|Placement/type files]] |
| Reuse | [[TODOs/Codebase Audit 2026-09-27/07 Reuse - Combat and Radar Boundaries\|Combat/radar]] | [[TODOs/Codebase Audit 2026-09-27/08 Reuse - UI Assets and Extraction Rules\|UI/extraction rules]] |

## Scope

- Original audit: inventoried 714 C# files under Assets/Scripts, used broad searches and focused live-source review, with Graphify as a non-authoritative map.
- Vendor code and every source line were not exhaustively audited.
- Original baseline was f61f9e49; other agents were editing the shared working tree.

- Implementation: all planned changes are complete.
- File moves used AssetDatabase and preserved GUIDs.
- Only the fog renderer reference in SceneContext.prefab was intentionally changed by this task.
- The follow-up includes focused Edit Mode tests and a commit containing only this task's changes; see the results note for verification.
- Other agents' edits are preserved.

- See the implementation results for exact verification limits; successful compilation is not a substitute for runtime regression tests.
