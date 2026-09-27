---
tags:
  - code-audit
  - implementation
created: 2026-09-27
status: implemented
---
# Implementation results — 27 September 2026

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Status of all 17 entries]]

## Outcome

Implemented all 17 entries from the attached correction plan. The follow-up request authorized D3/D4, focused tests, and committing only this task's changes.

- **D3 completed:** ShipPopulation.CountShips is shared by CaptureSitesSystem and ReinforcementZonesSystem. It takes a containment predicate and returns Player/Opponent ship counts. Existing squadron weights stay in the systems.
- **D4 completed:** MiniMapMarkerCollection owns registration and cleanup through composition. Marker creation, zone diameter, last-seen ownership, and operational-site visibility stay in the presenters.
- **C4 scope:** completed projection/sharedMesh/explicit binding changes. The pure visibility-grid model, first deferred, was added in the follow-up verification pass (see below).

## Main code changes

| Area | Implementation |
|---|---|
| Time controls | SkirmishOrchestrator restores _requestedTimeMode after menu close and blocks pause/speed controls in menus. SkirmishSessionModel and its observer expose EffectiveTimeMode; consumers updated. |
| Input | IUiHitTest/UiHitTest own UI raycasts, reusable results/event data and cached layer. PointerGestureState owns drag/pinch state using System.Numerics; InputService retains polling and event dispatch. |
| Combat | TargetSelectionBatch owns target requests, candidates, job buffers, serial/job evaluation and application. Coordinator supplies its registration/generation predicate and retains event sequencing. NativeArrayBuffer is shared by targeting/due buffers. |
| Fog | One PositionToPixel function is used by painting and visibility sampling. All component lookups and mesh property accesses removed; uses meshFilter.sharedMesh. SceneContext.prefab has one added serialized fogRenderer reference. |
| Shared operations | FormationConversion shared by ShipOrderRunner/Squadron; PlanarGeometry shared by map and avoidance. UnitOrderService height handling remains separate. |
| Reservations | UnitLimitKey replaces concatenated strings, preserves request-kind separation and normalizes derived requests to the base request kind. Reserve/check/release and existing fixtures updated. |
| Offline audio | Tools/Audio/wav_writer.py owns PCM encoding. Both generators reuse it; weapon-only RMS/seam diagnostics remain local. |
| Cleanup | CollectionUtility deleted through AssetDatabase. BuildPathToFile and mutable cached naming removed; existing bindings compute their path per call. |
| Models | WeaponModel takes plain accuracy dictionaries/ranges and an explicit random roll; WeaponComponent owns asset lookup and random sampling. Radar loses unused masks/LayerData and uses constructor injection. Icon lookup is injected into views through IShipIconProvider backed by ShipUiData. |
| File organization | SelectionType moved to Services/Selection and namespace usages updated. The empty NavigationService folder was deleted. SceneReference and UnityToolbarExtender moved to ThirdParty with GUIDs and existing license/attribution material. Listed selection/purchase/random/radar types split into matching files. |

The radar ObservableList contract is still Unity-provided. This change removes the specified dead layer members and hidden injections; it does not claim complete radar purity.

## Verification performed

- Unity imported the new/moved scripts and completed compilation. Final check reported **isCompiling=false** and **scriptCompilationFailed=false**.
- Fog renderer binding was refreshed, the single changed prefab path was force-reserialized, assets saved, and the fileID verified on disk against the prefab's MeshRenderer.
- Moved SelectionType, SceneReference and toolbar folder GUIDs match their previous metadata; existing toolbar LICENSE and SceneReference attribution were preserved.
- New source files have Unity-generated .meta files. No stale first-party NavigationService namespace references were found.
- git diff --check passed; line-ending conversion notices are repository warnings, not whitespace errors.
- One small offline equivalence check compared the shared WAV writer against the prior weapon encoder with two representative sample inputs. Both produced **identical WAV bytes**. Output lived only in a temporary directory; no game audio assets were regenerated.

## Tests and remaining limits

Focused Unity Edit Mode tests were run asynchronously after checking that the open MainMenuScene was clean and no other test run was active.

| Fixture/filter | Passed | Failed |
|---|---:|---:|
| AuditSharedOperationsTests | 2 | 0 |
| SkirmishOrchestratorTests | 11 | 0 |
| EnemyUnitLimitModelTests | 9 | 0 |
| CombatAttackCoordinator (due + selection) | 8 | 0 |
| SelectionInputTests | 1 | 1 |
| **Total, final result per test** | **31** | **1** |

The initial combat selection run exposed outdated reflection setup from the refactor: the tests passed two constructor arguments instead of four and looked for target-selection storage on the coordinator. Updated their setup to use the extracted batch without changing their assertions; the rerun passed all eight combat tests.

**Remaining pre-existing failure:** SelectionInputTests.DisabledMiniMapInteraction_KeepsMapVisible expects a private ActivateInteraction method. That method is already absent from MiniMapUi in HEAD, while the HEAD test still reflects it. This task changed only that fixture's moved Selection namespace import. The unrelated stale test was left unchanged.

New shared-operation coverage checks containment boundaries, neutral ships, empty queries, current ship membership, independent marker ownership, repeated cleanup, and reuse after cleanup. No full suite, build, or Play Mode smoke test was run. Tests ran in the shared working tree, which also contains other agents' changes.

The vendored toolbar still emits its existing-style Unity internal-toolbar compatibility warning; this task only relocated it. No runtime performance improvement is claimed without profiling.

## Coordination

Other-agent edits continued during implementation, including navigation, capture logic and enemy squadron work. This task preserves those edits. Commit preparation uses an explicit file list plus partial-file staging for the shared capture systems and enemy controller/test, excluding the other agents' changes and all Obsidian configuration.

The original audit's incorrect C1/R2 assessment and exaggerated D2 concern have been corrected in the overview and category notes.


## Verification pass — 27 September 2026 (follow-up)

Every entry was re-checked against live source at HEAD 1a7b901d. C1–C3, D2–D6, L2–L4 and R1–R3 hold. Four gaps were found and closed:

| ID | Gap found | Fix |
|---|---|---|
| D1 | Inline `new FormationPoint(v.x, v.z)` / `new Vector3(p.X, 0f, p.Z)` remained in UnitOrderService, PlayerOrderInputHandler (both named by the audit), EnemyStrategicContextBuilder, EnemyTaskForceExecutor, EnemyUnitCommander, UnitOrderFeedbackUiController, and a private `ToFormationPoint` copy in ShipNavigationService | All use `FormationConversion.ToPoint/ToVector`; the private copy was deleted. UnitOrderService still preserves ship height on its output slots. |
| C4 | Pure visibility-grid/history model had been deferred | `FogVisibilityGridModel` (pure C#, next to FogVisibilityModel) owns current/target visibility, history reset, circle reveal, fade and clamped sampling. FogOfWarSystem keeps the Transform projection, source registry, texture and material. There is no separate presenter: the system is a single scene view, so one would only forward calls. |
| L1 | Empty `Components/Utils/Collections` folder + .meta left behind | Deleted through AssetDatabase. |
| Tests | Stale `SelectionInputTests.DisabledMiniMapInteraction_KeepsMapVisible` reflected removed `ActivateInteraction` | Replaced by `MiniMapPointerExit_KeepsMapVisible`, which tests the current pointer-exit fade (map alpha stays ≥ 0.75). |

New `FogVisibilityGridModelTests` cover fade step, history retention, no-history reset and clamped sampling.

**Verification:** Unity refresh + forced recompile completed with `scriptCompilationFailed=false`. No `error CS` in the latest compile. The new model, fog tests and the replacement selection test are all present in loaded assemblies. Tests were **not executed** (AGENTS policy: run only on explicit request).
