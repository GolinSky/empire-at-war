---
category: Optimization
status: done
created: 2026-10-12
updated: 2026-10-12
completed: 2026-10-29
acceptance: pending-user
---
# Squadron pooling

> Closed 2026-10-29 by backlog triage: implementation complete; open user gates → [[TODOs/User_Acceptance_Queue]].

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
- [x] `SquadronPool : ISquadronSpawner, ITickable` (`Services/Squadron`), bound per player in `GameUnitsInstaller` (scene container cannot see `SquadronFactory`); key (PlayerId, SquadronType); miss → `SquadronFactory`.
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
- [x] Live battle 2026-10-12 (user-run): reuse in all 4 players' pools; contexts stay ~122 despite losses. Consistency over 106 alive + 13 despawned: all alive in `IEntityLocator`, radar registered, not `Dead` layer, not released; no despawned in locator/radar. No console errors.
- Hangar frames > 3 ms: 19 / 2000 (8–17 ms) early in battle = pool misses (cold pools), not reuse cost.
- [ ] User visual check of respawned squadrons: selection, minimap, fog, audio.

### 7. Spawn cost on pool miss (2026-10-12)
- Profile (hangar frames 506/1350/1493/1546): `HangarComponent.Tick` 12.9–21.2 ms = unmarked self 5.0–9.9 (Zenject container build/resolve) + `Instantiate` ×3 1.7–7.6 + `GameObjectContext.Construct` 1.4–2.9.
- EditMode timings (20 runs): Addressables load data+view 0.01–0.04 ms; data SO clone 0.015–0.04 ms → moving data bindings to the parent container saves ≈0.
- View `Instantiate` (inactive parent): TIE 3.24 / ARC170 2.87 / VWing 2.21 ms; without per-fighter `FighterExplosionVfx` 0.93 / 1.30 / 0.71 ms.
- Prefab weight: ARC170 229 GOs / 422 comps; Vulture 233 / 438; TIE 135 / 298 (file 2.3 MB: 21 `ParticleSystem` × ~96 KB). Per fighter: 3 explosion ParticleSystems (max 1000, playOnAwake off), 2 `TrailRenderer` + `PPFXRendererLayer`, 2–4 `WeaponHardPoint` (no `[Inject]`), fbx empty nodes (`Collision`, `Shadow`, `*_Shadow`).
- Candidates: shared pooled fighter explosion (−1.5–2.3 ms/miss); pool prewarm at battle start (moves Zenject cost to loading); hardpoint removal not worth it (no injection, drives per-muzzle shots).
- Why per-fighter explosion: `SquadronViewPrefabBuilder` (`92b2db55`) bakes `FighterExplosionVfx` (3 ParticleSystems) under each fighter → fog hides it with the unit. Ships differ: `HardPoint` instantiates `ExplosionVfx` lazily on first death (not pooled).

### 8. Prewarm, shared data, fighter assets (2026-10-12, uncommitted)
- [x] Prewarm: `SquadronPool : IInitializable` builds `PREWARM_PER_TYPE = 4` per launchable type at scene init → `Squadron.Stow()` (= `Release(false)` + `Despawn`). Types = faction squadrons ∪ faction ships' `HangarBays` ∪ station bay. Republic 5, Separatist 3, Rebellion 4, Empire 6 types → ~60–100 squadrons over 4 players.
- [x] Read-only data shared, not cloned: `BindScriptableObject`, `BindModel` (→ `ModelDependencyBuilder.BindShared`), `DynamicEntityInstaller` use `FromInstance`. Kept as copies (runtime state): `GameData`, `MiniMapData`.
- [x] Per-player `ReinforcementData`, `EconomyData`, `ShipClassMatchupData` moved from `PlayerCoreInstaller`/`AiPlayerInstaller` to `SkirmishMainInstaller`.
- [x] Empty fighter nodes stripped (prefab-instance removals): model prefabs (Muzzle*, Collision, Shadow, PE_* locators) + view leftovers. TIEInterceptor view 345 → 153 transforms, TIEBrute 303 → 123, TIEPunisher 229 → 105, ARC170 229 → 174.
- [x] 65 squadron body materials `Ship Lit`/URP Lit → URP Unlit (albedo + color; untextured glows take emission color; glass stays transparent); unused saved properties removed. `TeamColorView` removed from 18 views; `ShipLitSetupTool` no longer targets squadrons.
- [x] 54 fighter-only textures: max 256, `CompressedLQ`, no crunch (`smokeSheet03` shared → skipped).
- [x] 18 fighter models + 5 XWing bone meshes: mesh compression High, not readable, tangents None. Normals kept: reinforcement hologram uses URP Simple Lit.
- View `Instantiate` (EditMode): TIE 3.24 → 2.60, ARC170 2.87 → 2.18, VWing 2.21 → 1.76 ms. Remaining bulk = per-fighter explosion (0.65 / 0.77 / 0.48 ms without).
- EditMode 1516: 1471 pass / 45 fail — all ship data/prefab (uncommitted ship imports), `PowerToMainBatteriesTests` (bare `WeaponProfile`), legacy. Radar 8/8, PlayerCoreInstaller 2/2 pass.
- [x] Committed `a3df1bdf7` (2026-10-12).
- Live capture 7 (prewarm 4/type + slim fighters; editor, profiler on, ~138 squadrons, 169 contexts): median 35.4 / p95 62.9 / p99 73.6 ms; 0 frames > 150 ms (2 excluded = profiler start + agent eval blocking main thread); hangar frames > 3 ms 8 (7 in first 700 frames) = drained busy types (TIEFighter, ARC170, YWing, VWing, AWing, Belbullab22, Vulture). 144 alive + 24 despawned consistent.
- Agent note: never `Thread.Sleep`-poll inside Unity eval during a capture — eval runs on the main thread (25 s frame).

### 9. Follow-ups (2026-10-12, committed `73093b17d`)
- [x] Prewarm sized per type: `min(12, 2 if buildable + Σ MaxActive of every faction ship/station bay launching it)` → ~Republic 26, Separatist 15, Rebellion 28, Empire 32.
- [x] Shared fighter explosion: `FighterExplosionPool : IFighterExplosionPool` (scene, prewarm 16, grows on demand); `FighterView` injects it and plays at death position; hidden when `IVisionService.IsAreaVisible(local, pos, 5)` is false. Prefab `FighterExplosionVfx` got `ExplosionVfx` (particles + 3 renderers); `SkirmishMainInstaller.fighterExplosion` set on `SceneContext.prefab`.
- [x] Baked explosions removed from 18 views (incl. unpacked copies in TIE/Vulture/YWingBomber views) + their `FogVisibilityComponent.renderers` entries; 0 ParticleSystems, 0 null fog entries. `SquadronViewPrefabBuilder` no longer bakes it.
- View `Instantiate` (EditMode): TIE 3.24 → 0.60, ARC170 2.87 → 1.00, VWing 2.21 → 0.60 ms.
- Explosion now stays at the death point (was local-space under the fighter).
- [x] Bug: `EnemyShipAbilityController.Tick` enumerated `IEntityLocator.Entities` while `TryActivate` could add units (fighter reinforcements; pooled respawn re-registers synchronously) → "Collection was modified". Casters collected first. Test `AbilityThatAddsUnits_DoesNotBreakTheCasterScan` fails on old code, passes on fix.
- Tests: FighterExplosionPoolTests 3/3, EnemyShipAbilityControllerTests 2/2. Full EditMode 1475/1520; failures are ship data/balance from concurrent ship imports (`3230a90d0` Liberator duplicate X-wing bay identity, ISDII stats) + legacy.
- Found, not fixed: `AWingSquadronView` has no `FogVisibilityComponent` → enemy A-wings never hidden by fog (`BindFogOfWarFeature` uses `FromComponentsInHierarchy`, so no error).
- [ ] User in-game check: fighter look, explosions (size/fog), loading time with ~100 prewarmed squadrons, hangar spikes.

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
