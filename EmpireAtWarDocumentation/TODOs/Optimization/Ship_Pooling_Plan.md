---
category: Optimization
status: todo
created: 2026-10-12
updated: 2026-10-12
---
# Ship pooling

## Goal
- Remove ship spawn spikes: enemy builds cost **13.4–15.8 ms** each (stress capture 2026-10-12 22:46 Kyiv, `Battle.Enemy.Build` ≈ `TimerPoolTick`).
- Same pattern as [[Done/Optimization/Squadron_Pooling_Plan]]: reuse built contexts keyed by (PlayerId, ShipType).

## Important Values
- Spawn frame cost: mostly build-callback self time (Zenject install/resolve, component init); `Instantiate` 2.5–4.7 ms of it.
- Rate at 500 population: 4 enemy builds / 120 frames (~10 s).
- Evidence: [Report](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T194652Z-stress-ai/analysis.md>)

## Current Lifecycle
- `Ship.Release(playDeathEffects)`: orders release → `EntityComponentLifecycle.Release` → unsubscribe → layer `Dead` → `ShipService.Remove` → `OnRelease` → `UnitExplosionService.Spawn(hull renderers)` → `UnitWreckService.Spawn(Data.Wreck)` → `Destroy(context, Data.DestroyDelay)`.
- Creators: `EnemyFactionController` (scheduled build), player reinforcement placement, cheats → `ShipFactory.Create(owner, type, position)`; arrival via hyperspace tween in `ShipMoveComponent.Initialize`.

## State inventory (reset path needed)
- Already `IRespawnable` (squadron work): `SelectionComponent`, `RadarComponent`, `WeaponComponent`, `FogVisibilityComponent`, `AudioDialogShipComponent`, `Entity`, `MiniMapUnitMarkerPresenter`.
- Ship-only, no reset path yet:
  - `HealthComponent`/`HealthModel`: hull, shields, `HardPointModel` destroyed states, shield view, hardpoint overlay, engine hardpoints.
  - `HardPoint` views: destroyed visuals, `HardPointExplosionPool` fires anchored on them.
  - `ShipMoveComponent`/`ShipMoveModel`: motion, navigation registration, hyperspace arrival, body rotation.
  - `HangarComponent`/`HangarModel`: reserves, launched squadrons, cooldowns (carriers).
  - `AudioShipComponent`: loops, `ShipSfxService` registration.
  - Abilities: `ShipAbilityFacade` slots, `CombatModifiers` temporary effects, `CompositeBeamFacade`.
  - `ShipOrderRunner` + state machine.
  - Layers: `Dead` applied recursively on death → restore `Unit` (keep WorldUi/Fog children).
  - Death presentation: hull renderers after explosion; wreck is a separate object (stays).
  - Listeners: `Ship.OnRelease` (`EnemyFactionController.ReleaseUnit`) must be cleared per life.

## Decision
- Pending user go-ahead: multi-step; each step needs an in-game check by the user (no agent Play Mode).

## TODO
- [ ] Verify each inventory item against live source; list fields per component.
- [ ] `ShipPool : IShipSpawner` per player; miss → `ShipFactory`; despawn at `Data.DestroyDelay` instead of `Destroy`.
- [ ] `Ship.Respawn(position)` through the entity: activate → health → movement → `IRespawnable` parts → clear `OnRelease` → `Initialize`.
- [ ] Reuse gate like squadrons (`CanRespawn`: no temporary combat effects, no active ability).
- [ ] EditMode tests per reset path; user battle check (selection, hardpoints, shields, hangars, audio, minimap, fog).
