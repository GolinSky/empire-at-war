---
category: Features
status: in-progress
created: 2026-10-14
---
# Game Modes: Survival and Battle Simulation

## Goal
- Turn battle setup into a **modular builder**: a battle = map generator + player roles + feature modules + outcome rule. Skirmish, Survival and Battle Simulation are presets of that builder.
- Add **Survival** (defend a level-5 station through 5 waves) and **Battle Simulation** (pick units, watch AI vs AI, pause/resume/restart/replace).
- Split "station" (a unit) from "shop" (ability to buy), so allies without a station buy through a host's production base.

## Baseline before Phase 0 (verified 2026-10-14)
- `GameMode` enum has only `Skirmish = 0`. `GameController.StartGame(...)` hard-sets `GameMode.Skirmish` and loads `SceneType.Battle`.
- `GameData` holds `Players`, `PlanetType`, `MapSize`, `VictoryCondition`, `StartingMoney`.
- `SkirmishScope.Configure` registers every battle service unconditionally; `PlayerScopeBootstrap` adds `LocalPlayerScope` / `AiPlayerScope` per `PlayerSlot`.
- `SkirmishOrchestrator` owns `BattleState` (`Loading/Running/Paused/Ended`), `GameSpeed`, `Time.timeScale`. `SetState` ignores every change after `Ended`.
- `BattleStartupSequence`: load map → `IPlayerRegistry.GetStationSpawner(id).Spawn()` for **every** player → camera → fog reveal → fade.
- `BattleMapLoader` → `MapLayoutGenerator`; `StationPlacer` puts every station in a map **corner**.
- `BattleVictoryService` + `BattleVictoryModel.Evaluate(BattleVictoryCondition, states, localTeam)`.
- Station level: `PlayerFactionModel.CurrentLevel` (starts `1`, fires `OnLevelUpgraded`); `StationLevelData.MaxLevel`; income via `IEconomyProvider.RecalculateIncome`.

### Buying today
- `IStationRegistry.HasProductionBase(PlayerId owner)` → owner has a living `IProductionBaseFacade` entity (station, Golan III, Empress).
- Local: `FactionService.TryPurchaseUnit` + `FactionUi` / `FactionUiController` / `ShipBuildUiController`; selecting a production base opens the shop.
- AI: `EnemyProductionStrategy` → `IEnemyPurchaseProcessor.Purchase` → `EnemyFactionController`. No UI.
- Both gate purchase and completed builds (refund) on `HasProductionBase(owner)`.
- Requests: `Ship`, `Squadron`, `DefendPlatform`, `MiningFacility`, `Level`, `Research`, `SuperWeapon` (`Entities/Faction/Controller/Requests/`, built by `UnitRequestFactory`).

### Super weapons today
- Battle scope (`SkirmishScope`): `SuperWeaponData`, `SuperWeaponTargetingModel`, `SuperWeaponOriginRegistry`, `SuperWeaponFireService`, `SuperWeaponCatalog`.
- Player scopes: `SuperWeaponModel` + `SuperWeaponPresenter` (local) / `EnemySuperWeaponController` (AI).
- Shop entries: `SuperWeaponUnitRequest` via `UnitRequestFactory`; `FactionService` checks `SuperWeaponModel.CanPurchase`.

### Battle simulator (Editor-only)
- `Assets/Scripts/Editor/BattleSimulator/`: `BattleSimulation`, `BattleSetup`, `BattleFleetEntry` (`Quantity` 1–100). `SimUnitFactory` uses `UnityEditor` → not shippable.

## Decisions
- 2026-10-14 menu navigation: Main Menu → Play → Skirmish / Survival / Battle Simulation. `IPlayRoute` isolates Main Menu from mode routes; existing cancel focus stack returns setup → Play → Main Menu. Simulation remains disabled; Survival setup remains available.
- Play navigation verification: 3/3 focused menu lifecycle tests passed; Main Menu and Play prefabs imported/saved and visually inspected.
### Modular battle builder
- `BattleDefinition` (pure C#, on `GameData`) replaces the loose skirmish fields:
  - `MapSpec` → `Full` (skirmish), `Survival` (center station, clear edges), `Empty` (simulation).
  - `IReadOnlyList<BattlePlayer>` → slot + `PlayerRole` + per-player feature flags.
  - `BattleFeatures` → toggles: `SuperWeapons`, `CaptureSites`, `MiningSites`, `Economy`, `Fog`.
  - `OutcomeRule` → `Skirmish(victoryCondition)`, `Survival`, `Simulation`.
  - Mode payload → `SurvivalSettings`, `SimulationSetup`.
- `BattleDefinitionBuilder` (pure C#) used by each menu route: `SkirmishPreset`, `SurvivalPreset`, `SimulationPreset` fill defaults; menu toggles override.
- Each feature = `IBattleModule` with `ConfigureBattle(IContainerBuilder)` + `ConfigurePlayer(IContainerBuilder, BattlePlayer)`. `SkirmishScope` / player scopes register only enabled modules.
  - Avoid: `if (GameMode == ...)` checks inside services.
- `PlayerRole` → `Human`, `AiOpponent`, `AiAlly` (station-less, hosted shop), `WaveAttacker` (no station, no economy, no production), `SimulationSide` (no station, no economy).
- `GameMode` stays as a label for UI/results only.

### Station vs shop
- `IProductionAccess` per player: `bool CanProduce`, `bool TryGetProductionBase(out IEntity)`, `PlayerId ProductionHost`.
  - Self-hosted (skirmish, survival human) → unchanged behavior.
  - Hosted (survival ally) → host = local player; any host production base (station / Golan / Empress) enables buying.
- Ally keeps own wallet, income, unit limits, research, super weapons, faction roster.
- Ally tech level **follows host station level** (read-only); ally cannot buy `LevelUnitRequest`.

### Survival
- Menu mirrors `SkirmishUi`: player faction, optional ally faction, enemy faction, map size, AI difficulty, **super weapons toggle**.
- Waves spawn at any **map edge** location, validated against asteroids.
- Preparation 120 s before wave 1, 60 s between waves. Money = level-5 station income + mining; no bounty.
- Super weapons + research: allowed for player and ally (when toggle on); **never** for the wave enemy.
- Wave composition: **ScriptableObject config** listing enemies per wave per enemy faction. Difficulty only affects global enemy AI behavior (targeting, micro, abilities), never wave contents.

### Battle Simulation
- Real entities, AI vs AI, direct attack orders; groups attack-move. All builds. Features: all off (no economy, super weapons, sites, fog).

## Phase 0 — Modular Battle Builder
- [x] `BattleDefinition`, `MapSpec`, `BattlePlayer`, `PlayerRole`, `BattleFeatures`, `OutcomeRule` (pure C#). One type per file.
- [x] `BattleDefinitionBuilder` + `SkirmishPreset`; `GameController.StartBattle(BattleDefinition)` replaces `StartGame(...)`; `MatchRules.Validate` extended for roles.
- [x] Extend `GameMode`: `Survival = 1`, `BattleSimulation = 2`.
- [x] `IBattleModule` + module list built from `BattleFeatures`; refactor `SkirmishScope.Configure` into modules without behavior change (skirmish preset = all features on).
- [x] `SuperWeaponsModule`: moves battle-scope + player-scope super weapon registrations; when off, `UnitRequestFactory` emits no `SuperWeaponUnitRequest` and AI has no `EnemySuperWeaponController`.
- [x] Skirmish menu: add super weapons toggle (`SkirmishModel` + `SkirmishUi`).
- [x] Station-less roles: `BattleStartupSequence` spawns stations only for roles that own one.
- [x] `BattleVictoryService` delegates to `IBattleOutcomeRule`; `BattleVictoryModel` = skirmish rule.
- [x] `EndGameUi`: mode-specific result text.
- [x] Main menu: Survival + Battle Simulation buttons (all builds). Read `UI_UX_GUIDELINES` + `UI_CODE_BUILD_GUIDE` first.
- [ ] Tests: builder presets; module selection from features; super weapons off → no requests/controllers; skirmish regression (existing EditMode suite green).

### Phase 0 verification (2026-10-14)
- User scope: Survival setup now; Survival gameplay and simulation launch remain disabled until later phases.
- Implemented all five feature modules; Skirmish defaults remain all-on. Eight real-scope tests pass, including each feature off, all off, and WaveAttacker ship creation without economy/research.
- Baseline: 1,245 EditMode tests, 1,198 passed, 47 failed. User authorized baseline repairs.
- Latest full EditMode run: 1,288/1,308 passed; 20 failed. Phase 0 builder, UI and eight real-scope composition cases passed. Full-suite green gate remains open.
- Remaining failures: 13 balance tests blocked by concurrent `SensorJammingSettings` registration; 5 fleet-import asset checks (Allegiance geometry/references/radar/weapon scope; Procursator team-color bindings); 2 map tests received concurrent Editor request-timeout error logs.
- Baseline repairs: duplicate hangar bay identities; stale balance/targetability assertions and incomplete test fixtures; four tracked radar ranges; helper meshes; 15 team-color renderer lists; weapon descriptions. Authored ISD I/II death-clone wrecks retained and tested separately; generated-wreck tests ignore disabled renderers.
- Verification files: `output/BattleBuilderPhase0/baseline.json`, `composition.json`, `fifth-pass.json`; UI render previews inspected at 1920×1080. No gameplay Play Mode run.
- Concurrent Empire fleet import is changing this checkout and sharing the test runner; its source/assets are excluded from this commit. Re-run the full suite after that work stabilizes.

### Phase 0 review fixes (2026-10-14)
- Disabled features register no-op stand-ins from their module (`IBattleModule.ConfigureDisabled`): `NoFogOfWar`, `RevealedMiniMapFogMask`, `NoCaptureSites`. Consumers take single dependencies again; no `IReadOnlyList<T>` optional injection.
- Inert state is always registered: `SuperWeaponModel` (player), `SuperWeaponTargetingModel`, `SuperWeaponOriginRegistry`, `SuperWeaponCatalog`, `MiningFacilityCatalog` (battle). `FactionRoster` filters super weapons / mining by features. `MiningSitesModule` removed.
- `ICaptureSitesSystem` has one registration: the real system (on) or `NoCaptureSites` (off). `BattleCaptureSites` removed.
- `BattleDefinition` masks each player's features with the battle's; `BattlePlayer.Has(feature)` is the only check.
- `EconomyModule.ConfigurePlayer(builder, player, playerRoot)` owns shop, cheats, AI production, research (AI), debug view.
- `FogModule` registers `VisionService` via factory (no string-named `WithParameter`) and deactivates the fog object; `MapLayoutView` gets `IFogOfWarSystem` by `[Inject]`.
- Super weapon button: hidden by `CoreGameUi.Initialize`, shown by `SuperWeaponPresenter.Bind`.
- Verification: full EditMode 1,295/1,313 passed; all touched suites green (BattleDefinition 14, RealEntityComposition 8, others 84). 18 failures unrelated: 13 balance `SensorJammingSettings`, 4 fleet import (Allegiance/Procursator), 1 `UiCanvasArchitectureTests` screen count 22 vs 21.

## Phase 1 — Shop Abstraction
- [ ] `IProductionAccess` + `SelfProductionAccess` + `HostedProductionAccess`; registered per player scope from `BattlePlayer`.
- [ ] Replace direct `HasProductionBase(Owner)` gates in `FactionService` (purchase + `BuildUnit` refund) and `EnemyFactionController` (`Purchase`, `ForceUpgradeStation`).
- [ ] Hosted spawns: ships/squadrons at host production base / reinforcement zones; platforms and mining use host placement areas. Verify `ReinforcementService`, `IReinforcementPlacement` impls, `ReinforcementZonesSystem` ownership.
- [ ] Hosted tech level: ally `PlayerFactionModel` level mirrors host `OnLevelUpgraded`; `LevelUnitRequest` hidden/rejected.
- [ ] Hosted super weapons: origin/targeting from host base (`SuperWeaponOriginRegistry`); verify ally AI `EnemySuperWeaponController` works without own station.
- [ ] AI ally home position: `EnemyProductionStrategy` / `EnemyStrategicContextBuilder` use host base position.
- [ ] Tests: skirmish unchanged; hosted buy while host base lives; refund when all host bases die; level mirrors host; ally cannot buy level.

## Phase 2 — Survival Mode
### Rules
- Player station spawned at **level 5** (set before first income calc; clamp to `MaxLevel`).
- Optional ally (`AiAlly`). Enemy = `WaveAttacker` (no station/economy/production/super weapons/research).
- 5 waves: `Preparation` (120 s, then 60 s) → `WaveActive` → all wave units dead → next. Optional "Start wave now".
- Win: wave 5 cleared, player station alive. Lose: player station destroyed at any time.

### Implementation
- [x] `SurvivalPreset` + `SurvivalSettings` (factions, map size, difficulty, super weapons toggle); delivered in Phase 0.
- [ ] `SurvivalModel` (pure C#): `WaveIndex`, `Phase` (`Preparation`, `WaveActive`, `Won`, `Lost`), `PreparationTimeLeft`; events.
- [ ] `SurvivalWaveData` ScriptableObject (`Assets/Settings`):
  - `preparationSeconds` (`120` first, `60` after).
  - Per enemy faction → 5 waves → entries `{unit id, quantity}` + spawn group count.
  - Validated in EditMode tests: every faction has 5 waves, unit ids exist in its `FactionCatalog` roster.
- [ ] Difficulty → existing `EnemyAiDifficultyProfile` for wave AI behavior only.
- [ ] `SurvivalLayoutGenerator`: station at center; edge ring kept clear of asteroid fields; mining sites around center.
- [ ] `EdgeSpawnPointProvider`: random edge points per wave with min spacing; validate hull footprint (`IShipSpawnClearance`) + asteroid fields; retry other points.
- [ ] `SurvivalWaveDirector` (`ITickable`): timers, frame-staggered wave spawn, alive tracking via `IEntityLocator`.
- [ ] Wave AI: attack-move to player station; engage defenders on contact.
- [ ] `SurvivalOutcomeRule`.
- [ ] HUD: `Wave N/5`, countdown, "Start wave now", minimap direction pings.
- [x] `SurvivalUi` setup in main menu; delivered in Phase 0. Launch remains disabled until wave gameplay is implemented.
- [ ] Tests: `SurvivalModel` transitions; lose during preparation; win after wave 5; edge spawn avoids fields; wave enemy has no super weapon/research; UI prefab reference tests.

## Phase 3 — Battle Simulation Mode
### Rules
- Empty map. Only chosen units. Two `SimulationSide` players, all features off.
- Setup: Side A / Side B fleets (`unit × quantity`), factions, distance, time limit, game speed.
- On start: direct attack orders on the other side; groups attack-move; retarget when target dies.
- Controls: Pause, Resume, Restart (same setup), New (settings panel, no scene reload).
- Result: winner, elapsed time, survivors/HP per side.

### Implementation
- [ ] `SimulationPreset` + `SimulationSetup` (runtime assembly; mirrors Editor `BattleSetup`/`BattleFleetEntry`; no `EmpireAtWar.Editor.*` refs).
- [ ] Unit picker from runtime `FactionCatalog`.
- [ ] `EmptyLayoutGenerator`: bounds + two spawn lines `Distance` apart.
- [ ] `SimulationDirector`: spawn via normal ship/squadron spawning, issue orders, track counts/time, publish `SimulationResult`.
- [ ] Restart/New: despawn-all API (pooling, `IEntityLocator`, projectiles, VFX, wrecks, in-flight squadrons), then respawn.
- [ ] `SkirmishOrchestrator`: explicit restart `Ended → Loading → Running` only for simulation outcome rule.
- [ ] HUD: Pause/Resume, Restart, New, speed 1×/4×, timer, per-side counts, result panel.
- [ ] Tests: setup validation (1–100, both sides non-empty), director spawn/end/restart with fakes, orchestrator restart.

## Risks / Edge Cases
- Module refactor of `SkirmishScope` must keep skirmish identical — run full EditMode suite before/after Phase 0.
- Station-less players: audit `IPlayerRegistry.GetStationSpawner`, fog vision, reinforcement zones, AI context builder, minimap, camera home.
- `BattleVictoryModel` "observed base" logic → station-less roles never count as defeated.
- Host loses all production bases → ally builds refund; host Golan/Empress alive with station dead → still a production base (survival lost anyway by station death).
- Super weapons off mid-preset defaults: AI must not plan for them (`EnemySuperWeaponController` absent, production strategy skips).
- Edge spawns near asteroids: validate footprints; fallback point.
- Restart leaks: projectiles, VFX, wrecks, pooled objects.
- Level-5 start: build menus reflect level 5; income recalculated after level set; ally mirrors level 5 from start.

## Files
- `Assets/Scripts/Entities/Game/GameMode.cs`, `GameData.cs`, `GameController.cs`, `BattleVictoryModel.cs`, `MatchRules`
- `Assets/Scripts/BaseSystem/IGameCommand.cs`
- `Assets/Scripts/Entities/CoreGame/SkirmishOrchestrator.cs`, `BattleStartupSequence.cs`
- `Assets/Scripts/Services/BattleService/BattleVictoryService.cs`
- `Assets/Scripts/Services/VContainer/SkirmishScope.cs`, `PlayerScopeBootstrap.cs`, `LocalPlayerScope.cs`, `AiPlayerScope.cs`
- `Assets/Scripts/Services/Station/IStationRegistry.cs`, `StationRegistry.cs`
- `Assets/Scripts/Services/Faction/FactionService.cs`
- `Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs`
- `Assets/Scripts/Entities/Faction/Controller/Requests/UnitRequestFactory.cs`, `SuperWeaponUnitRequest.cs`
- `Assets/Scripts/Entities/SuperWeapon/*`, `Services/SuperWeapons/*`, `Services/Enemy/EnemySuperWeaponController.cs`
- `Assets/Scripts/Services/Enemy/EnemyProductionStrategy.cs`, `EnemyStrategicContextBuilder.cs`
- `Assets/Scripts/Services/Reinforcement/*`
- `Assets/Scripts/Entities/Map/BattleMapLoader.cs`, `Generation/StationPlacer.cs`, `Generation/MapLayoutGenerator.cs`
- `Assets/Scripts/Entities/Faction/Model/PlayerFactionModel.cs`, `StationLevelData.cs`
- `Assets/Scripts/Entities/MainMenu/Main/MainMenuUi.cs`, `MainMenu/Skirmish/*`
- `Assets/Scripts/Editor/BattleSimulator/*` (reference only)
