---
category: Features
status: done
created: 2026-10-19
completed: 2026-10-19
---
# Battle Session Save

## Goal
- Save and load an in-progress **Skirmish** or **Survival** battle. Battle Simulation is never saved.
- Save only the battle's rules state. On load, rebuild the battle, then apply the saved values. Visual and short-lived state rebuilds itself.

## Decision
- **Chosen**: deterministic rebuild. Save the inputs used to build the battle setup plus the map seed, rebuild the battle through the same preset, then restore units and per-system values.
- **Why**: map geometry comes out the same from the same seed, and unit components rebuild their visuals from their underlying data on the first ticks.
- **Avoid**: serializing the scene or `BattleDefinition` (constructor-built, partly computed); saving projectiles, VFX or radar contacts.

## Rules (user decisions 2026-10-19)
- **Modes**: Skirmish and Survival only. Simulation: the save service refuses (`OutcomeRule.Simulation`) and `SimulationMode` registers nothing for saving.
- **Slots per mode**: `persistentDataPath/Saves/<Skirmish|Survival>/{autosave.json, quicksave.json, <name>.json}`. The Play menu shows the load list for the selected mode.
- **Abilities**: `Active` with *t* seconds left → `Recovering` with `TimeLeft = t + Definition.RecoveryDelay`. `Recovering` keeps its time. `Ready` stays `Ready`. Active effects and `RunningAbility` are not saved.
- **Hyperspace**: a ship mid-jump is saved at its arrival point and loads already arrived, with no jump. The reinforcement placement preview is not saved.
- **Camera**: save position, rotation and `CameraZoom.TargetHeight`. Restore with `CameraService.SetPose` and a zoom reset, instead of `MoveTo(station)`.
- Saving is refused while the battle is `Loading` or `Ended`.

## Important Values
- Autosave default: every 5 min of battle time, plus at the end of each Survival wave.
- Snapshot taken on the main thread in one call between frames. JSON write (`JsonUtility`, temp file then swap, as in `JsonSettingsRepository`) runs off the main thread.
- Save header: save version, mode, slot kind, date/time, battle time elapsed, `GameSpeed`.

## Save Contents

### All saved modes
- Rebuild input: Skirmish → `SkirmishPreset.Create` inputs + map seed; Survival → `SurvivalSettings`.
- Camera pose and zoom target.
- `BattleVictoryModel._observedBases`. Without it, a player whose station died before the save is never counted as defeated.
- Units: save id, owner, `UnitTypeId`, position, rotation.
  - Health: hull, shields, per-hardpoint health and installed flag, level (`HealthModel`, `HardPointModel`).
  - Orders: type, destination, waypoints + current index, target id, targeted hardpoint (`UnitOrderModel`).
  - Ability slots: state + `TimeLeft`, with the ability rule applied.
  - Hangar: reserve/active/disabled per bay, cooldown (`HangarModel`).
  - `CombatModifiers`: ion-disable count, cloaked, focus target id.
  - Squadrons also: health per fighter, carrier id (or production base).
- Stations, mining facilities, defend platforms: save id, owner, type, site/slot, health, hardpoints.

### Skirmish
- Map seed (`MapType.Full` only; `Survival` and `Empty` layouts come out the same without one).
- Per player that can produce:
  - money (`EconomyModel`)
  - station level, production queues (`ProductionQueueItem.RemainingBuildTime`), structure counts, Resolute reserved (`PlayerFactionModel`)
  - finished research tiers (`FactionResearchModel._completedTiers`)
  - superweapon state + cooldown (`SuperWeaponModel`)
  - reinforcement reserve counts (`ReinforcementModel._reserveCounts`)
- AI opponents: queued builds with time left, unit-limit counts.
- AI allies: no level of their own; `HostedFactionLevel` copies the human's level.
- Capture sites by layout index: owner, state, player capturing, capture progress, facility type, construction progress. Only when `BattleFeatures.CaptureSites` is on.

### Survival
- `SurvivalModel`: `WaveIndex`, `Phase`, `PreparationTimeLeft`.
- `SurvivalWaveDirector`: `_wave`, `_pending`, `_spawned`, `_settleTime`, `_commanded`.
- Human/ally economy, production, research, mining sites as in Skirmish, limited by `BattleFeatures`.

### Not saved
- Projectiles, beams and impacts in flight: `BoltFlightBatch`, `IncomingMissileRegistry`, shot pools, `ImpactEffectView`, superweapon bolts.
- Radar contacts (re-scanned on first tick).
- Fog texture/grid (`RevealImmediately()` runs after restore).
- Wrecks and explosions (`UnitWreckService`, `UnitExplosionService`, `FighterExplosionPool`, `HardPointExplosionPool`), ion-stun arcs, cloak shader, audio, music.
- Weapon reload, shield-regen delay, income and AI decision timers (they restart).
- `EnemyStrategicDecisionModel._lastState`, `TeamIntelRegistry`, squadron retarget timers, steering details, selection, UI panels, targeting modes.

## Implementation (code changes)
1. **Map seed**: add a seed to `MapSpec`/`BattleDefinition`, picked when the Skirmish definition is built. `BattleMapLoader.cs:62` `new Random()` → `new Random(seed)`. `MatchNebula` static `System.Random` is visual only: seed it the same way or accept a different nebula.
2. **Unit ids**: `UniqueIdGenerator` returns a random `Guid`-based id. Add a supplied-id path to `ShipFactory`, `SquadronFactory`, `SpaceStationFactory`, `MiningFacilityFactory`, `DefendPlatformFactory`, plus a matching `BattleUnitSpawner` overload. Fix up targets, focus target and carrier through the unit registry in `EntityMediator` (`Dictionary<long, IEntity>`).
3. **Restore spawning**: take a rotation, skip `MovementPhase.Arriving`. Squadrons spawn already launched, not from a hangar.
4. **Restore methods on models**: one narrow method each, firing existing change events. Covers `HealthModel`, `EconomyModel`, `PlayerFactionModel`, `SuperWeaponModel`, `CaptureSiteModel`, `HangarModel`, `ReinforcementModel`, `ShipAbilitySlot`, `UnitOrderModel`, `SurvivalModel`, `BattleVictoryModel`, and the `CameraService` zoom target (`_zoom` is private). `FactionResearchModel`: re-run `Complete` per tier so bonus multipliers rebuild.
5. **Ability bar**: `Progress01 = TimeLeft / RecoveryDelay` would start above 1 after a restore. The slot's restore method records the total time for that cooldown, and `Progress01` divides by it while the restored cooldown runs.
6. **AI build queue**: `EnemyFactionController.cs:310` uses `TimerPoolService.Invoke` callbacks, which can't be saved. Replace with a model that tracks time left per build, like `ProductionQueueItem`.
7. **Survival director**: add snapshot/restore to `SurvivalWaveDirector`. Without it, `_wave` (0) ≠ `WaveIndex` → the whole wave is queued again.
8. **Startup**: `BattleStartupSequence.RunAsync`: a fresh battle spawns stations where `OwnsStation`; a loaded battle runs `BattleRestoreStep`. Order: stations/structures → ships → squadrons → link fix-up → model values → Survival director → camera pose → `RevealImmediately`.
9. **Unit capacity**: restore units through the normal capacity and unit-limit calls; don't save capacity totals (avoids counting twice).
10. **Save service**: new `BattleSaveService` in the battle scope. Each owning system implements a narrow `IBattleSnapshotSource<TDto>`. Mode sources register in `SkirmishMode`/`SurvivalMode`; feature sources in their `BattleModules` module, so disabled features add nothing.
11. **Load entry**: `IGameCommand.LoadBattle(BattleSave save)` → rebuild the definition via the preset → `MatchRules.Validate` → keep the snapshot for the restore step → load `SceneType.Battle`.
12. **UI**: quicksave/quickload input bindings; named saves in `PauseMenuUi`; load lists in `PlayRouteUiController`; loading in-game goes back through the scene load.

## TODO
- [x] Phase 1: rebuild the definition for a load + seed the Skirmish map. Tests: same seed → same `MapLayout`; each preset round-trips to an equal definition.
  - 2026-10-19 done, uncommitted. `MapSpec.Seed` (default `0`); `SkirmishPreset.Create(..., bool superWeapons, int mapSeed)` — the superweapon toggle moved from `SkirmishModel` into the preset; `SkirmishPreset.NewMapSeed()` (Guid hash) used by `SkirmishModel.CreateBattle` and `Default()`. `BattleMapLoader` → `new Random(_battle.Map.Seed)`.
  - Rebuild input: `BattleSetupSave` (mode + `SkirmishSetupSave` | `SurvivalSetupSave`, `PlayerSlotSave`), `From(BattleDefinition)` / `Rebuild()` → preset `.Build()` (runs `MatchRules.Validate`). Simulation → `InvalidOperationException`. Enums serialized as ints; Phase 2 version field guards reorders.
  - `MatchNebula` left unseeded: a loaded battle may show a different nebula variant (visual only).
  - Verification: compile clean, zero console errors. EditMode `BattleSetupSaveTests` 7/7, `MapLayoutGeneratorTests` 18/18 (new `SavedSkirmishSeed_RebuildsIdenticalLayout` compares every station/zone/site/lane/field/rock), `BattleDefinitionTests` 14/14, `GameControllerTests` 2/2. `RealEntityCompositionTests` 7/9: the 2 fighter-bank failures (`All`, `All & ~SuperWeapons`) also fail with Phase 1 stashed → pre-existing.
- [x] Phase 2: `BattleSaveService`, Skirmish/Survival file layout, slots, version field, refusal in Simulation. Header, camera and setup round-trip.
  - 2026-10-19 done, uncommitted. All in `Assets/Scripts/Services/Saves/` (namespace `EmpireAtWar.Services.Saves`; not `BattleSave`, which would clash with the class).
  - Data: `BattleSave` (header + `BattleSetupSave` + `CameraPoseSave`), `BattleSaveHeader` (`CURRENT_VERSION = 1`, mode, slot name, UTC ticks, battle time, `GameSpeed`; slot kind derived from the name), `BattleSaveSlot` (`Autosave`, `Quicksave`, `Named(name)` rejects empty/padded/trailing-dot/invalid file chars/reserved names).
  - `JsonBattleSaveRepository` (project scope, `persistentDataPath`): `Saves/<Skirmish|Survival>/<slot>.json`, temp file → `File.Replace`. `Load` → `FileNotFoundException` for an empty slot, `InvalidDataException` for corrupt/foreign-mode/older-version. `List(mode)` newest first, skips unreadable files with a warning, lists older versions (`IsCompatible = false`). Simulation → `ArgumentException`.
  - `BattleSaveService` (battle scope, all modes): `CanSave`, `Capture(slot)` on the main thread, `SaveAsync(slot)` writes on `Awaitable.BackgroundThreadAsync`. Refuses Simulation, any state other than `Running`/`Paused`, and a second save while one is writing.
  - `BattleClock` (battle scope `ITickable`): adds `Time.deltaTime` only while `Running`. `ICameraService.ZoomTargetHeight` added (reads `CameraZoom.TargetHeight`).
  - Deferred: `IBattleSnapshotSource<TDto>` → Phase 3 (first real source). Camera/clock restore methods → the restore step, when there is a caller. `SaveAsync` is not covered by EditMode tests (needs the player loop); `Capture` + repository are.
  - Verification: compile clean, zero console errors. EditMode `BattleSaveRepositoryTests` 16/16, `BattleSaveServiceTests` 10/10, `BattleSetupSaveTests` 7/7, `SelectionQueryTests` 2/2, `PlayerOrderInputHandlerTests` 4/4, `RealEntityCompositionTests` 7/9 (same 2 pre-existing fighter-bank failures; Survival composition now also resolves `BattleSaveService` from the real battle scope). One bug found by tests and fixed: an empty slot in a missing mode folder threw `DirectoryNotFoundException`.
- [x] Phase 3: units — ids, restore spawning, health/hardpoints, orders, link fix-up, squadrons/hangars, ability rule + bar fix.
  - 2026-10-19 done for ships and squadrons, uncommitted. Structures are Phase 4.
  - Ids: all 5 factories have a `Create(..., long entityId)` overload; the old one delegates with `GenerateUniqueId()`. `BattleUnitSpawner.SpawnRestoredShip/SpawnRestoredSquadron`. `ISquadronSpawner.SpawnRestored` builds a new squadron (pooled ones keep their own ids). `ISquadron.EntityId` added.
  - Spawning: `ShipSpawnPose` (`Hyperspace` | `Restored(rotation)`) registered by `ShipInstaller`; `ShipMoveComponent.Initialize` places a restored ship directly, no jump, landing reservation or arrival sound.
  - Health saved as fractions: research (`ResearchHealthData`) and fleet command rescale maxima after load, so absolute values would scale twice. `HealthModel.Restore`, `HardPointModel.Restore`, `SquadronHealthModel.Restore` (+ `GetMemberShieldPercentage`); count mismatch throws. Restoring through change events rebuilds disabled bays, engine slow-down, lost shield generator and burning-hardpoint VFX.
  - Not saved (plan correction): hardpoint installed flag and unit level — they follow from station level (Phase 4). `CombatModifiers` ion count / cloak / focus — they belong to active abilities, which load as recovering, so restored effects would never be removed.
  - Abilities: `AbilitySlotSave.From` applies Active → Recovering `t + RecoveryDelay`. `ShipAbilityService.Restore` adds a recovering slot to its running list. `ShipAbilitySlot._recoveryTotal` fixes `Progress01` for cooldowns longer than `RecoveryDelay`.
  - Hangar: `IHangarSaveFacade` on `HangarComponent` — reserve, cooldown, launched `{squadronId, bay}`; restore adopts restored squadrons (`HangarModel.AdoptLaunched`) and watches them again.
  - Orders: `UnitOrderSave` re-issued through the unit's order facades. Waypoints keep only unreached ones. Not saved: ability approach (`UseAbility` → idle). Attack-move gets one engagement per unit. Missing target → unit idles.
  - `UnitSaveService` (battle scope): capture living units with `IUnitSaveFacade` (`ShipSaveFacade`, `SquadronSaveFacade`); restore = spawn all → health/abilities/hangars → orders. `BattleSave.units`; `BattleSaveService.Capture` includes them. Still a direct dependency; `IBattleSnapshotSource` deferred to Phase 4.
  - Verification: compile clean. New EditMode: `SavedUnits_RestoreWithIdsPoseHealthAbilitiesHangarAndOrders` (real scopes: Acclamator + launched squadron → capture → JSON → dispose → restore; ids, pose, no jump, hull/hardpoints, recovering ability + full bar, move order, fighter health, squadron guards restored carrier, hangar active count) passes; `UnitSaveTests` 4/4; restore cases added to `HealthModelTests`, `SquadronHealthModelTests`, `HangarModelTests`, `ShipAbilityServiceTests`. Full EditMode 1431/1459: 8 `ShipNavigationServiceTests` failures were this change (reflective `Construct`), fixed → 23/23. Remaining 20 outside this work: 13 Balance* (`SensorJammingSettings`), Allegiance TeamColor/WeaponTaxonomy ×3, Titan count, `EntityBindingsTests` prefab count 76→80, 2 pre-existing fighter-bank.
  - In-game checks (user): fighters dead at save play their explosion once on load; restored local units may voice their re-issued orders; burning hardpoints reappear (intended).
- [x] Runtime load path: completed with Phase 6 — `IGameCommand.LoadBattle` rebuilds/validates the definition and reloads the battle scene; `BattleStartupSequence` invokes `BattleRestoreStep` instead of spawning fresh stations.
- [x] Phase 4: Skirmish state — economy, production, research, superweapons, sites, AI build queue change, victory memory.
  - 2026-10-19 done, uncommitted; includes structures.
  - Mechanism: `IBattleSaveSource` (`Capture` / `RestoreBeforeUnits` / `RestoreAfterUnits(BattleRestoreContext)`) registered by modes/modules; `BattleSaveService` calls every source. `PlayerSaveRegistry` (battle scope) collects player-scope `PlayerSaveSource`s, which register themselves on `Initialize`. `BattleRestorer.Restore(save)` = sources before units → `UnitSaveService.Restore` → sources after units. `BattleRestoreContext` holds spawned views by saved id, plus hangar-owned squadron ids and site-facility ids.
  - Sources: `EconomySaveSource` (EconomyModule, money); `HumanProductionSaveSource` (EconomyModule human: level, research, queue); `ReinforcementSaveSource` (LocalPlayerScope: reserve, capacity, structure slots); `AiProductionSaveSource` (EconomyModule AI: level unless hosted, research, pending builds, ship orders, limit adoption); `SuperWeaponSaveSource` (both scopes); `CaptureSitesSaveSource` (CaptureSitesModule); `BattleVictorySaveSource` (SkirmishMode).
  - Requests: `UnitRequestSave` (kind + key + tier + time left) rebuilds `UnitRequest` from roster / `StationLevelData` / research tier. Queued items are not charged again.
  - Structures: `StructureSaveFacade` on station / mining / platform installers; `UnitSaveService` spawns structures before units (station spawns at the restored level), restores health and hangars (stations too). `BattleUnitSpawner` takes per-player `StructureFactories`; asteroid miners and battle asteroids use the asteroid factories.
  - Bookkeeping is rebuilt, not saved (plan correction): restored ships and squadrons not owned by a hangar bay take capacity (human) or limit (AI) slots again and get their release hooks. Restored mining/platforms not built by a site take structure slots. AI squadrons are handed to `EnemySquadronCommander` again.
  - AI build queue: `EnemyBuildQueue` + `EnemyPendingBuild` replace `TimerPoolService` callbacks; `EnemyFactionController` is `ITickable` with `Advance(dt)`, and shares `Track(...)` release hooks between new builds and `Adopt*`. `EnemyUnitLimitModel.Adopt`.
  - Sites: `ISiteFacilityBuilder.Build` returns the facility id (`SiteFacilityBuilder` generates it); `CaptureSitePresenter.FacilityId/SetFacility/Restore`. Operational sites re-hook the restored facility's `OnRelease`. `CaptureSitesSystem.SitePresenters` exposes layout order.
  - Other: `FactionService.Income` computed from level; restore methods on `EconomyModel`, `PlayerFactionModel` (`QueuedItems`, `RestoreQueue`), `ProductionQueueItem(request, remaining)`, `SuperWeaponModel.Restore`, `ReinforcementModel.Reserve`, `CaptureSiteModel.Restore`, `BattleVictoryModel.ObservedBases/RestoreObservedBases`.
  - Not saved: `EnemyStructurePlacementService` recent destroyed positions, AI decision timers.
  - Verification: compile clean. `SavedSkirmish_RestoresPlayerStateStructuresAndOwnerBookkeepingIntoNewBattle` (real scopes: state on both players → capture → JSON → dispose battle → build new battle → `BattleRestorer`): money, level + income, research, queue time left, reserve, capacity, superweapon cooldown, structure slot, AI money / pending build / limits / ship orders, station + ship + mine ids, victory memory — passes. `PlayerSaveTests` 5/5; capture-site restore cases 2/2 (model only: the harness skips map loading, so the site source has no EditMode coverage); `EnemyFactionControllerLifecycleTests` 5/5 moved off timers. Full EditMode 1458/1478; the same 20 out-of-scope failures as Phase 3.
  - Edit-mode artifact: level/research changes make `FactionUi` call `Object.Destroy` (one console error each in EditMode only); the test expects them.
  - Known limits: units/structures of types missing from the owner's roster (cheat-spawned foreign units) fail the restore. Over 200 lines: `PlayerFactionModel` (~310), `EnemyFactionController` (~383), `CaptureSitesSystem` (~390) — refactor candidates.
- [x] Phase 5: Survival state — `SurvivalModel` + `SurvivalWaveDirector`.
  - 2026-10-19 done, uncommitted. `SurvivalSaveSource` (registered in `SurvivalMode`) → `BattleSave.Survival` (`SurvivalSave`: wave, phase, preparation time left, director wave, pending `UnitTypeSave[]`, spawned, settle time). Null outside Survival; a Survival load without it throws `InvalidDataException`.
  - `SurvivalModel.Restore`; `SurvivalWaveDirector.Wave/Pending/Spawned/SettleTime`, `Restore(...)` (re-picks spawn edges via `BeginWave` when units are pending), `AdoptLivingAttackers()` after units so restored attackers keep their saved attack order instead of being ordered again.
  - Human/ally economy, production, research and sites use the Phase 4 sources unchanged (module-registered).
  - Verification: compile clean. `SavedSurvival_ResumesTheWaveInProgressWithoutQueuingItAgain` (real scopes: start wave 1, one attacker spawned → capture → JSON → new battle → `BattleRestorer`; wave/phase/director queue/attacker id match, next tick does not re-queue) passes. Full EditMode 1460/1480; the same 20 out-of-scope failures.
- [x] Phase 6: UI — quicksave/quickload keys, pause-menu saves, Play-menu load lists, autosave timer.
  - 2026-10-19 done, uncommitted. `BattleSaveActions` handles named/quicksave requests and visible failure messages; `BattleSaveInput` uses rebindable Battle actions (`F5` quicksave, `F9` quickload). Simulation registers neither input nor autosave services; pause save controls are hidden.
  - `PauseMenuUi`: name input, Save / Overwrite, Quicksave, Quickload, status. `PlayRouteUiController`: mode-specific slot list, local save date + battle duration, incompatible-version refusal and load error feedback. Both existing prefabs updated and persisted through Unity; `Tools/BattleBuilder/BuildSaveUi.cs` rebuilds their controls.
  - `GameController.LoadBattle`: validates version and rebuilt definition, retains `GameData.PendingBattleSave`, resets scene-loading time scale, reloads Battle through the loading scene. New battle / exit clears a pending snapshot.
  - `BattleRestoreStep`: calls `BattleRestorer`, restores `BattleClock`, speed and `CameraService.RestorePose` (independent zoom target), then consumes the pending snapshot. Startup skips fresh stations / default camera movement on load and reveals fog after registration frames.
  - `AutosaveSchedule` / `BattleAutosave`: every 300 battle seconds, starting a full interval after load; defers while a save is busy. `SurvivalAutosave` requests a save on WaveActive → Preparation. Final victory is excluded because Ended battles cannot be saved.
  - Verification: compilation clean; full EditMode **1466/1486 passed**, including all 6 new tests and 23/23 tests in directly affected fixtures. Remaining 20 failures match the Phase 3–5 baseline. Tested load handoff/consumption, clock/speed/camera restore, rebindable F5/F9, deferred autosave timing, mode filtering, version refusal and load errors. Existing real-scope Skirmish/Survival round trips pass.
  - UI: 1920×1080 renders inspected; dropdown arrow overlap corrected. Required references and Color Tint buttons verified; assets saved, no new import/serialization errors, MainMenuScene clean. Evidence: `output/BattleSessionSave/{EditModeResults.json,Play.png,Pause.png}`. Interactive gameplay checks remain user-owned per the verification rule.
- Verification per phase: compile + EditMode snapshot→restore round-trip tests per model. In-game checks are done by the user.

## Edge Cases
- Station destroyed before the save → `_observedBases` must be restored, or that player is never counted as defeated.
- AI ally level comes from the host → don't restore it separately.
- Restored cooldown longer than `RecoveryDelay` → the bar uses the recorded total, not `RecoveryDelay`.
- Survival attackers already alive on load → their ids are pre-filled into `_commanded` so they aren't re-ordered.
- Load from an older save version → refuse with a clear message (no migration yet).

## Files
- `Assets/Scripts/Entities/Game/{GameData,BattleDefinition,BattleDefinitionBuilder,SkirmishPreset,SurvivalPreset,SurvivalModel,BattleVictoryModel}.cs`
- `Assets/Scripts/Entities/Map/BattleMapLoader.cs`, `Components/Environment/MatchNebula.cs`
- `Assets/Scripts/Entities/CoreGame/{BattleStartupSequence,SkirmishOrchestrator}.cs`
- `Assets/Scripts/Services/BattleService/{BattleUnitSpawner,SurvivalWaveDirector}.cs`
- `Assets/Scripts/Services/IdGeneration/UniqueIdGenerator.cs`, entity factories in `Entities/{Ship,Squadron,SpaceStation,MiningFacility,DefendPlatform}`
- `Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs`
- `Assets/Scripts/Entities/Ship/Abilities/ShipAbilitySlot.cs`
- `Assets/Scripts/Services/Camera/{CameraService,CameraZoom}.cs`
- `Assets/Scripts/Services/VContainer/Battle/{Modes,Modules}/`
- `Assets/Scripts/Services/Settings/JsonSettingsRepository.cs` (pattern for writing files)
