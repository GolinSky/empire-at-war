---
category: Features
status: done
created: 2026-10-14
completed: 2026-10-29
acceptance: pending-user
---
# Game Modes: Survival and Battle Simulation

> Closed 2026-10-29 by backlog triage: implementation complete; open user gates → [[TODOs/User_Acceptance_Queue]].

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
### Execution (2026-10-14)
- Baseline rerun: 1,297/1,314 EditMode tests passed; 17 pre-existing failures (13 SensorJamming allowlist, UI screen count, two Allegiance assertions, fleet count).
- Implemented: per-player production access, hosted purchase/refund gates, host ship spawn anchors, ally squadron ownership, host tech-level synchronization, and AI home/structure placement.
- Focused verification: production access 3/3; Survival tests 6/6; real Survival host/ally/wave composition 1/1.
- Survival Play Mode: level-5 start, hosted ally production, early start, all five authored waves and victory passed. Station destruction during preparation ends in loss. Five-wave verification uses scripted enemy destruction; difficulty balance is not validated.
- Simulation Play Mode: ships and squadrons fight, pause freezes elapsed time, restart and faction replacement create fresh entity IDs without scene reload. Timed results verified. Final lifecycle smoke passed: pause/resume, 4× speed, combat replacement, New/cancel, result, and Ended restart. Same scene handle, fresh entity IDs, zero console errors; setup/HUD visually verified.
- 2026-10-14 AI vs AI UI polish: setup buttons, six dropdowns, fonts and popup rows aligned with Skirmish; simulation controls reduced to a 700 × 108 top bar, expanding to 204 px for results. Follow-up fixed the missing backdrop: copied panel `MPImage` was disabled. Added full-screen starfield/dark layers and enabled framed fleet/settings cards. Saved through Unity; offscreen 1920 × 1080 render inspected, all serialized references bound. Follow-up: six setup dropdowns reduced from 800 × 48 to 640 × 44, option rows 48 → 40, type 20 → 18, arrows aligned to the right edge; 6/6 saved controls and clean scene verified. No new automated test run.
- Final EditMode regression: **1,317/1,333 passed**. All mode/hosted-production tests passed. Remaining 16 pre-existing failures: 13 `SensorJammingSettings` allowlist checks, Allegiance radar and scoped-weapon checks, entity prefab count 76 vs 80. UI count updated to 25; no new failing tests.
- Live-source correction: ordinary AI queued builds intentionally survive base loss; preserve Skirmish behavior and refund hosted ally builds when all host production bases die.
- Live-source correction: super weapons use the shared planet origin, not a station-owned origin; hosted purchase eligibility uses the host's production bases.
- User approved initial escalating Survival wave rosters authored from each faction's available units.
- Survival layout has no asteroid fields; edge clearance also checks hull footprint and minimum spacing. Initial human income is 25 at level 5.
- Evidence: `output/BattleBuilderPhase1/`; full-suite green gate from Phase 0 remains open.

- [x] `IProductionAccess` + `SelfProductionAccess` + `HostedProductionAccess`; registered per player scope from `BattlePlayer`.
- [x] Replace direct `HasProductionBase(Owner)` gates in `FactionService` (purchase + `BuildUnit` refund) and `EnemyFactionController` (`Purchase`, `ForceUpgradeStation`).
- [x] Hosted spawns: ships/squadrons at host production base / reinforcement zones; platforms and mining use host placement areas. Verify `ReinforcementService`, `IReinforcementPlacement` impls, `ReinforcementZonesSystem` ownership.
- [x] Hosted tech level: ally `EnemyFactionModel` level mirrors host `PlayerFactionModel.OnLevelUpgraded`; `LevelUnitRequest` hidden/rejected.
- [x] Hosted super weapons: production eligibility from host base; retain shared planet origin (`SuperWeaponOriginRegistry`). Ally AI controller does not require its own station.
- [x] AI ally home position: `EnemyProductionStrategy` / `EnemyStrategicContextBuilder` use host base position.
- [x] Tests: skirmish unchanged; hosted buy while host base lives; refund when all host bases die; level mirrors host; ally cannot buy level.

## Phase 2 — Survival Mode
### Rules
- Player station spawned at **level 5** (set before first income calc; clamp to `MaxLevel`).
- Optional ally (`AiAlly`). Enemy = `WaveAttacker` (no station/economy/production/super weapons/research).
- 5 waves: `Preparation` (120 s, then 60 s) → `WaveActive` → all wave units dead → next. Optional "Start wave now".
- Win: wave 5 cleared, player station alive. Lose: player station destroyed at any time.

### Implementation
- [x] `SurvivalPreset` + `SurvivalSettings` (factions, map size, difficulty, super weapons toggle); delivered in Phase 0.
- [x] `SurvivalModel` (pure C#): `WaveIndex`, `Phase` (`Preparation`, `WaveActive`, `Won`, `Lost`), `PreparationTimeLeft`; events.
- [x] `SurvivalWaveData` ScriptableObject (`Assets/Settings`):
  - `preparationSeconds` (`120` first, `60` after).
  - Per enemy faction → 5 waves → entries `{unit id, quantity}` + spawn group count.
  - Validated in EditMode tests: every faction has 5 waves, unit ids exist in its `FactionCatalog` roster.
- [x] Difficulty → existing `EnemyAiDifficultyProfile` for wave AI behavior only.
- [x] `SurvivalLayoutGenerator`: station at center; edge ring kept clear of asteroid fields; mining sites around center.
- [x] `EdgeSpawnPointProvider`: random edge points per wave with min spacing; validate hull footprint (`IShipSpawnClearance`) + asteroid fields; retry other points.
- [x] `SurvivalWaveDirector` (`ITickable`): timers, frame-staggered wave spawn, alive tracking via `IEntityLocator`.
- [x] Wave AI: attack-move to player station; engage defenders on contact.
- [x] `SurvivalOutcomeRule`.
- [x] HUD: `Wave N/5`, countdown, "Start wave now", minimap direction pings.
- [x] `SurvivalUi` setup in main menu; delivered in Phase 0. Launch enabled with wave gameplay.
- [x] Tests: `SurvivalModel` transitions; lose during preparation; win after wave 5; edge spawn avoids fields; wave enemy has no super weapon/research; UI prefab reference tests.

### Survival roster update (2026-10-19)
- Updated `Assets/Settings/Data/Battles/SurvivalWaveData.asset`: wave N contains every faction ship with `AvailableLevel <= N`; earlier-tier quantities carry forward unchanged.
- Largest per-level roster across factions: `2 / 6 / 5 / 3 / 3`. User selected 2× counts → tier batches `4 / 12 / 10 / 6 / 6`; cumulative ships `4 / 16 / 26 / 32 / 38` for every faction.
- Duplicate ordinary combat ships within their unlock tier; heroes, Stealth Corvette, Mandator, Imperator, Malevolence, Executor and Titan stay at one copy per wave.
- Existing direct squadron entries remain `2 / 3 / 4 / 5 / 6`; hangar squadrons are additional. Preparation and spawn-group counts unchanged.
- Class/power limitation: Separatist level 3 only offers Captor; Empire has multiple capital ships. Equal counts do not establish equal combat strength; combat playtesting remains unverified.
- Verification: new roster regression fails on old data, passes on updated data; `SurvivalGameplayTests` **4/4 passed**. All 20 waves imported, explicitly reserialized and saved; no new console errors during import. Broader plan remains in-progress for its existing verification gates.

## Phase 3 — Battle Simulation Mode
### Rules
- Empty map. Only chosen units. Two `SimulationSide` players, all features off.
- Setup: Side A / Side B fleets (`unit × quantity`), factions, distance, time limit, game speed.
- On start: direct attack orders on the other side; groups attack-move; retarget when target dies.
- Controls: Pause, Resume, Restart (same setup), New (settings panel, no scene reload).
- Result: winner, elapsed time, survivors/HP per side.

### Implementation
- [x] `SimulationPreset` + `SimulationSetup` (runtime assembly; mirrors Editor `BattleSetup`/`BattleFleetEntry`; no `EmpireAtWar.Editor.*` refs).
- [x] Unit picker from runtime `FactionCatalog`.
- [x] `EmptyLayoutGenerator`: bounds + two spawn lines `Distance` apart.
- [x] `SimulationDirector`: spawn via normal ship/squadron spawning, issue orders, track counts/time, publish `SimulationResult`.
- [x] Restart/New: despawn-all API (pooling, `IEntityLocator`, projectiles, VFX, wrecks, in-flight squadrons), then respawn.
- [x] `SkirmishOrchestrator`: explicit restart `Ended → Loading → Running` only for simulation outcome rule.
- [x] HUD: Pause/Resume, Restart, New, speed 1×/4×, timer, per-side counts, result panel.
- [x] Tests: setup validation (1–100, both sides non-empty), model spawn barrier/end/reset and orchestrator restart; real-entity director lifecycle verified in Play Mode.

## Review fixes (2026-10-14)
- Mode registrations → `IBattleMode` (`SkirmishMode`, `SurvivalMode`, `SimulationMode`), picked once by `BattleModes.Create(battle, assets)` from `OutcomeRule`. `SkirmishScope` has no mode branches. Each mode owns local player, outcome rule, station facing, director/HUD.
- Human/host single source: `BattleDefinition.Human`, `GetProductionHost(player)`; role capabilities `BattlePlayer.IsHosted`, `HasDirectCombatAi`. Survival station level = `BattleDefinition.InitialStationLevel` (`SurvivalPreset.STATION_LEVEL = 5`).
- `SelfProductionAccess` / `HostedProductionAccess` → `IStationRegistry.TryGetProductionBase` (no entity scan).
- Wave soft-lock: `EdgeSpawnPointProvider.TryGet` clears the wave's spacing reservations and retries once; hull clearance still applies.
- Simulation targeting → `SimulationTargeting`: every unassigned unit gets `IssueAttack` on its nearest living enemy; group attack-move to a stale position removed.
- Not changed: `SkirmishOrchestrator` simulation-restart guards; `BattleMapLoader` dispatch on `MapType`; timeout result = Draw.
- Verification: full EditMode 1,321/1,339 passed. 18 failures unrelated: 13 `SensorJammingSettings`, Allegiance radar/scoped weapon, entity prefab count 76 vs 80, Allegiance team-color renderers, Titan composition (concurrent fleet import). New tests: `EdgeSpawns_ReuseEdgeOnceWaveReservationsFillIt`, `Targeting_AttacksNearestEnemy_AndRetargetsOnlyWhenItDies`. No Play Mode run.

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
