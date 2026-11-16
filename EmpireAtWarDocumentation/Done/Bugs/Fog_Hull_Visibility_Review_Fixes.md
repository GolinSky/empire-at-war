---
category: Bugs
status: done
created: 2026-11-16
completed: 2026-11-16
acceptance: pending-user
---
# Fog Hull Visibility — Review Fixes

## Goal

- Close review gaps from `d19023145` without changing radar/weapon ranges or host authority.
- Outcome: entity-based audio, staggered ship visibility, saved hull freshness guard, configurable firing reveal, all-attacker reveal and replication coverage implemented.
- Canonical rules updated: [[GameDesign/Visibility]]. Acceptance → [[TODOs/User_Acceptance_Queue#Fog Hull Visibility]].

## Decision

- [x] D1 — Reveal every actual attacker: ships, squadrons and structures; attacked team only; cloak wins.
- [x] D2 — User chose faster staggered hostile-ship checks (2026-11-16): 10 Hz; other entities retain 1 Hz.
- [x] D3 — User chose serialized duration: `FogOfWarSystem.firingRevealSeconds = 5` on `SceneContext.prefab`; `FogModule` supplies `IVisionService.FiringRevealSeconds`.
- F5 — Global reveal-event fan-out remains unchanged pending measured cost, as required by the review. Matching fog handlers now filter both entity ID and allied viewer. CPU capture is an acceptance gate, not evidence of a speedup.

## Implementation

- [x] F1 — `ShipSfxService` resolves live audio handles through `IEntityLocator` and uses the shared entity query; `ShipAmbienceService` uses its existing entity. Cloak gating preserved; detached/world effects retain point rules.
  - Tooltip finding disproved: `WorldTooltipPresenter.hit.point` is obstacle fallback only. Entity/hardpoint tooltips already use fog-aware `UnitHoverService`; no tooltip edit.
- [x] F2 — `FogVisibilityComponent` uses lifecycle `ITickable` and a 0.1 s accumulator for footprint holders. Entity-ID phases stagger checks; long frames do one query, not catch-up loops. Non-footprint entities stay on `ISlowTickService`.
  - `EntityFeatureBindings` registers all component interfaces; `OrderedLifecycleCoordinator` ticks them. Despawn removes subscriptions, respawn resets phase; `DynamicEntityInstaller` supplies entity ID.
- [x] F3 — Saved hull test compares vertex count and each X/Z coordinate to `ShipVisionHullBaker.MeasureHull(prefab)` (0.001 tolerance), retaining nondegenerate-area checks.
  - `WeaponLoadoutBaker.BakeAll()` calls `ShipVisionHullBaker.BakeAll()` so existing authoring/bake entry points refresh hull data. No ship assets rebaked in this task.
- [x] F4 — Weapon, Ion Shot and sustained beam callers read the duration through `IVisionService`; concrete tuning constant removed. Runtime value comes from saved fog settings; direct test constructors retain default 5 s.
- [x] F6 — Removed structure/strikecraft emission exclusions. Entity point queries, fog presentation, minimap and squadron marker sampling honor entity-ID reveals. Per-fighter explosion/Harmonic Bomb point checks unchanged.
- [x] F7 — Added host transition coalescing, spawn-flush ordering, duplicate suppression, baseline filtering and later-transition tests; receiver regression rejects pre-spawn unknown IDs and accepts a fresh post-spawn reveal.

## Verification

- Source and test diffs reviewed together; Unity compilation completed with `scriptCompilationFailed = false`, no new Console errors after final refresh/save.
- `SceneContext.prefab` imported, explicitly reserialized and saved; disk diff contains only `firingRevealSeconds: 5`.
- Regression fixtures updated: `VisionServiceTests`, `FogDetectionEventTests`, `VisionHullGeometryTests`, `WeaponFiringRevealTests`, `MiniMapUnitMarkerVisibilityTests`, `SquadronTargetSelectorTests`, `ShipSfxBudgetTests`, `VisionRevealReplicationTests`; added `HostVisionRevealSenderTests`.
- `TacticalMarkerRegistryTests` mock updated for interface contract. Reviewed unchanged `WeaponLoadoutBakeTests`, `HardPointPlacementToolTests`, `UnitHoverServiceTests`, tooltip/ambience tests and `ShipAbilityServiceTests`: existing behavior/assertions remain applicable.
- Categories: Vision, Combat, Networking, Ui, Squadrons, Audio, BattleInput; reviewed Ships, Abilities and Capture consumers. No tests run under the local execution override.
- [x] T3 — Static audit of all 72 saved ship loadouts: zero baseline weapon ranges exceed radar range. Faction/research modifiers, muzzle offsets and spatial contact remain separate; this does not promise return fire.
- Graphify freshness query blocked by active update lock; Serena handshake unavailable after idempotent launcher. Live source used; generated graph not treated as evidence.

## Edge Cases

- Hull overlap remains circle/bounds rejection + polygon edge/containment; cached world hulls and allied source snapshots retained. Convex hulls bridge concave gaps such as Lucrehulk rings.
- Reveals refresh only after actual emission, not orders/failed emission. Host owns expiry; reconnect sends reveal baseline after synchronous client spawns; despawn clears reused IDs.
- Host broadcasts reveal records to all clients: functional, not hidden-information safe.
- Fog overlay y = -40, alpha ≤0.03; revealed hulls/minimap markers outside circles may sit in fog-tinted space.
- Host AI takeover rebuilds the same IDs; removal clears reveal, re-fire restores it.
- CPU flatness and 10 Hz cost are unmeasured. Global event fan-out changes require capture evidence.

## TODO

- [ ] T1 — On explicit request, run non-explicit Vision, Combat, Networking, Ui, Squadrons, Abilities, BattleInput, Capture and Audio checks.
- [ ] T2 — Matched large-battle CPU capture versus `d19023145`: vision queries, acquisition, fog cadence and reveal fan-out; remove `profiler.data` after analysis.
- [ ] In-game: Executor/Mandator hull edge while rotating; all attacker kinds reveal, markers/audio respond, five seconds after last shot, cloak precedence.
- [ ] Online/reconnect: same reveal/hide and mid-reveal baseline. Gates tracked in acceptance queue; implementation complete.

## Files

- `Assets/Scripts/Services/Vision/`, `Components/FogOfWar/FogVisibilityComponent.cs`, `Components/Weapon/WeaponComponent.cs`.
- `Components/Squadron/Icon/SquadronIconComponent.cs`, `Entities/MiniMap/Presenter/MiniMapUnitMarkerPresenter.cs`.
- `Services/Audio/{ShipSfxService,ShipAmbienceService}.cs`; `Services/ShipAbilities/Abilities/{SustainedBeamAbility,IonShot/IonShotAbility}.cs`.
- `Services/FogOfWar/FogOfWarSystem.cs`, `Services/VContainer/Battle/Modules/FogModule.cs`, `Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab`.
- `Assets/Scripts/Editor/AI/WeaponLoadoutBaker.cs`; `Assets/Scripts/Tests/Editor/{Vision,Combat,Networking,Ui,Squadrons,Audio,BattleInput}/`.
