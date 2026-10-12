---
category: Optimization
status: todo
created: 2026-10-12
updated: 2026-10-12
---
# Squadron pooling

## Goal
- Remove the per-launch spawn spike: reuse released squadron contexts instead of `SquadronFactory.Create` building a new Zenject sub-container every time.
- Keep gameplay identical: launch position/rotation, guard order, health, flight, weapons, radar, fog, selection, minimap, audio, abilities.

## Important Values
- Editor capture 2026-10-12, frame 1747 (`HangarComponent.Tick`, one squadron): **22.97 ms total**.
  - `Instantiate` ×3: 10.62 ms (`Instantiate.Copy` 5.65, `Instantiate.Produce` 4.41 incl. `WeaponComponent..ctor` 1.65, `Awake` 0.54).
  - `HangarComponent.Tick` self (unmarked Zenject resolution/install): 7.85 ms.
  - `GameObjectContext.Construct`: 3.32 ms (`LazyInjectAll` 0.78, `MiniMapUnitMarkerPresenter.Initialize` 0.50, `WeaponComponent.Initialize` 0.33).
  - `GameObject.Activate` 0.52 ms (`MPImage.Awake`).
- Editor numbers ≈2–3× a Development Player; re-measure in a player before/after.
- Already shipped mitigations: `ISquadronLaunchSlots` = max **1 reserve launch per frame** across hangars; Zenject reflection baking enabled (`Assets/Settings/Data/Zenject/ZenjectReflectionBakingSettings.asset`). Measure their effect first — baseline for this plan.
- Squadrons alive in large battles: ~107–149; ~60 hangars ticking.

## Current Lifecycle (verified 2026-10-12)
- Creators: `HangarComponent.Launch`/`LaunchFromBay`, `SquadronReinforcementPlacement`, `ReinforcementPlacementFactory` → `SquadronFactory.Create(owner, type, position, rotation)`.
- Factory: `GameUnitsInstaller` → `BindFactory<PlayerId, SquadronType, Vector3, Quaternion, Squadron, SquadronFactory>().FromSubContainerResolve().ByNewContextPrefab<SquadronInstaller>(...)`.
- `DynamicEntityInstaller.InstallBindings`: binds start position, clones data with `FromNewScriptableObject(data)` (per spawn), `InstallFeatures`, `InstantiatePrefabForInstall(<Type>SquadronView)`, `EntityInstaller` (Entity, EntityTransformFacade, ViewEntity, `MiniMapUnitMarkerPresenter`, new unique id).
- `SquadronInstaller.InstallFeatures` depends on **owner**: `BindFogOfWarFeature(isHiddenByLocalFog)`, `AudioDialogShipComponent` only when local. Depends on **type**: data/prefab path, `SeekerWarheadCountermeasure` (data), `SFoilsView`/`SFoilsFacade` (abilities).
- Release: `Squadron.Release(destroyed)` → unsubscribes, `ShipSfxService.ReleaseShip`, `_orders.Clear()`, `EntityComponentLifecycle.Release()`, `Released` event; destroyed → layer `Dead` + `Destroy(context, RELEASED_CONTEXT_LIFETIME = 6 s)`.
- Hangar listens to `ISquadron.Released` → `HangarModel.SquadronLost(bay)`.

## Decision
- Chosen: pool **fully built squadron contexts keyed by (PlayerId, SquadronType)**; reuse only after the death presentation ends (≥ `RELEASED_CONTEXT_LIFETIME`).
- Why: owner and type change installed bindings; a cross-owner/type pool would need re-installation, which is the cost being removed.
- Avoid: re-running `InstallBindings` on a pooled container; reusing a context while anything still holds the old `IEntity` identity (see Edge Cases).

## Rules
- Follow AGENTS.md entity rules: reset happens through the entity (`Squadron`) calling component interfaces; components never reset siblings.
- Every per-squadron model/component gets an explicit `Reset`/`Respawn` path; no hidden state carried between lives.
- One pool owner service (e.g. `SquadronPool : ISquadronSpawner`) replaces direct `SquadronFactory` use in the 3 creators; the factory stays as the pool's miss path.
- No routine null guards; missing bindings must fail fast.
- Measure in a Development Player before claiming gains.

## Implementation

### 1. Baseline
- [ ] Re-measure launch cost with reflection baking + launch slots (editor and Development Player): per-spawn ms, frames > 16 ms during hangar waves.
- [ ] If spawn ≤ ~3 ms in the player, stop and record the result (pooling may not be worth its risk).

### 2. State inventory
- [ ] List every per-squadron object and its mutable state: `SquadronHealthModel`, `SquadronFlightModel`/`SquadronFlightComponent` (craft spawn/death), `SquadronPilot`, `SquadronTargetSelector`, `UnitOrderModel`, `SFoilsModel`, `CombatModifiers`, `WeaponComponent`/`WeaponModel`/hardpoints (`AttackSequenceState`, reload timers, `_mountYaw`, target slots), `RadarComponent`/`RadarModel`, `SelectionModel`/`SelectionComponent`, `FogVisibilityComponent`, `SquadronIconComponent`, `MiniMapUnitMarkerPresenter`, `AudioDialogShipComponent`, `SeekerWarheadCountermeasure`, cloned `SquadronData`.
- [ ] List every global registration: radar service, selection service, fog system, minimap, `ISquadronRegistry`, `IEntityLocator`, ship SFX service, combat coordinator (`Register/Unregister`), target table slots.

### 3. Identity and stale references
- [ ] Decide identity on reuse: new unique id + re-registration in every global registry, or an entity generation counter checked by holders.
- [ ] Find holders of squadron `IEntity`/health after death (attackers' `AttackData`, AI commanders, radar enemy lists, hangar `_launched`, selection) and prove they drop or ignore the old life.

### 4. Pool service
- [ ] Add pool service keyed by (PlayerId, SquadronType); `Rent(position, rotation)` → reset + activate; `Return` on `Released` after the death lifetime instead of `Destroy`.
- [ ] Optional prewarm per owner/type from faction data at battle start, spread one per frame.
- [ ] Replace `SquadronFactory` use in `HangarComponent`, `SquadronReinforcementPlacement`, `ReinforcementPlacementFactory`.

### 5. Reset path
- [ ] `Squadron.Respawn(position, rotation)`: entity-driven reset of every component from step 2, re-register globals, re-run `Initialize`-equivalent subscriptions.
- [ ] Tests: respawned squadron equals a fresh one (health, craft count, orders, radar, weapon state, selection, fog, minimap, audio).

### 6. Acceptance
- [ ] Play Mode: launches, deaths, reinforcement squadrons, hangar destruction, owner defeat, battle end/scene unload (no leaks, no errors).
- [ ] Matched Development Player capture vs step 1.

## Edge Cases
- Squadron dies while pooled return pending → returned twice; guard with the existing `_isReleased` flow.
- Battle end/scene unload → pool must dispose pooled contexts (`LateDispose`).
- Owner defeated → drop that owner's pool.
- Hangar launches before fighters spawn (`Initialize` comment in `Squadron`): guard order must still apply on reuse.
- `Destroy(context, 6 s)` death presentation (layer `Dead`) must finish before reuse; reset the layer.
- Pooled context hidden: must not be selectable, visible on minimap, scanned by radar, or targeted.

## Files
- `Assets/Scripts/Entities/Squadron/Squadron.cs`, `SquadronInstaller.cs`, `SquadronFactory.cs`
- `Assets/Scripts/Services/SceneContext/DynamicEntityInstallers/DynamicEntityInstaller.cs`
- `Assets/Scripts/Entities/BaseEntity/EntityInstaller.cs`, `EntityFeatureBindings.cs`
- `Assets/Scripts/Services/SceneContext/Skirmish/GameUnitsInstaller.cs`
- `Assets/Scripts/Components/Hangar/HangarComponent.cs`
- `Assets/Scripts/Services/Reinforcement/SquadronReinforcementPlacement.cs`, `ReinforcementPlacementFactory.cs`
- `Assets/Scripts/Services/Squadron/SquadronLaunchSlots.cs`
- Context: [[TODOs/Optimization/Battle_CPU_Performance_Plan]]
