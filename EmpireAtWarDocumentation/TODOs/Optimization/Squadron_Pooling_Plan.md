---
category: Optimization
status: in-progress
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
- Already shipped mitigations: `ISquadronLaunchSlots` = max **1 reserve launch per frame** across hangars. Zenject reflection baking was tried and **removed 2026-10-12** (woven DLLs referenced themselves; editor silently ran stale code) — see [[TODOs/Optimization/Battle_CPU_Performance_Plan]].
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
- No agent Play Mode runs or captures; gains are measured by the user.

## Implementation

### 1. Baseline
- [x] Use existing capture 4 (baked + launch slots, editor): spawn frames median 13.8 ms, max 26 ms. No agent re-measurement; user measures in-game.

### 2. State inventory (2026-10-12)
- [x] Per-life state reset by re-running `Initialize` after clearing `_isReleased`: health (new members/adapters, `IsDestroyed` reset in `InitializeMembers`), flight (formation at new pose), radar, weapon (+ hardpoint `RestartAttackSequence`, effect pool rebuilt), selection (`IsSelected = false`), icon, minimap marker, seeker countermeasure.
- [x] Orders cleared by `Release`; pilot reset by `Loiter`/`Escort` in `Squadron.Initialize`; `SFoilsModel` reopened; ability slots `Recovering → Ready`.
- [x] Views: `FighterView.UpdateData` 0 → >0 restores body, collider, trails, guns; `HardPoint` replays its explosion on the next death (`_hasExploded`); `WeaponHardPoint.Destroyed` clears.
- [x] Fog: keeps tracked explosion/ion renderers (reused), resubscribes, recomputes hidden state.
- [x] Not reset: `CombatModifiers` (owned by external abilities) → reuse gated instead; `SquadronData` clone, pilot RNG, `TeamColorView` (owner-constant).

### 3. Identity and stale references
- [x] Same `Entity`/id per pooled squadron. Removed from `IEntityLocator` at despawn (= old `Destroy` time, 6 s after death), re-added at respawn.
- [x] Holders re-check `IsAlive`/`IsDestroyed` every tick; weapons drop destroyed `AttackData` on next rebuild; dead ≥ 6 s before reuse.
- [x] `Squadron.Released` listeners cleared on respawn (`SquadronReinforcementPlacement` never unsubscribed → would double-remove capacity).
- Residual risk: a holder that keeps a dead squadron's `IEntity` > 6 s without checking health would see it alive again at the new position.

### 4. Pool service
- [x] `SquadronPool : ISquadronSpawner, ITickable` (`Services/Squadron`), bound in `SkirmishMainInstaller`; key (PlayerId, SquadronType); miss → `SquadronFactory`.
- [x] Destroyed → despawn after `DEATH_PRESENTATION_SECONDS = 6` (replaces `Destroy(context, 6 s)`); `Squadron.Despawn` → `IDespawnable` parts + `SetActive(false)`.
- [x] Reuse only when `Squadron.CanRespawn`: no `CombatModifiers.HasTemporaryEffects`, no active ability slot.
- [x] `HangarComponent`, `SquadronReinforcementPlacement`, `ReinforcementPlacementFactory` use `ISquadronSpawner`.
- Skipped: prewarm (optional); dropping a defeated owner's pool (inactive objects; destroyed with the scene).

### 5. Reset path
- [x] `Squadron.Respawn(position, rotation)`: activate → health → flight → `IRespawnable` parts (binding order; `Entity` re-registers before minimap) → clear `Released` → `EntityComponentLifecycle.Restart` → `Initialize`.
- [x] `IRespawnable`/`IDespawnable` (`Entities/BaseEntity`), `ISquadronHealthComponent`, `ISquadronFlightComponent.Respawn`; lists injected via `LazyInject` (Entity → facades → Squadron cycle).
- [x] EditMode tests added: `AttackSequenceStateTests`, `CombatModifiersTests.HasTemporaryEffects_*`, `SquadronHealthModelTests.InitializeMembers_AfterDestruction_*`.

### 6. Acceptance
- [x] Compiles (no errors; build output contains new types).
- [ ] EditMode run blocked: Editor domain keeps stale `Assembly-CSharp` (MVID 319b…, not on disk) after recompiles, clean rebuild and `RequestScriptReload`; new tests fail with `MissingMethodException`. Needs Editor restart, then rerun EditMode (expect 26 legacy failures only).
- In-game checks are done by the user, not agents (no agent Play Mode runs).

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
