---
category: Features
status: in-progress
created: 2026-11-10
---

# Guided Tutorial Battle Plan

## Goal
- Scripted solo battle that teaches the game chapter by chapter: hint lessons + weak, controlled enemy.
- Own game mode (`GameMode.Tutorial`) started from the Play menu next to Skirmish, Survival, Simulation (AI vs AI), Online.
- Win like a normal skirmish: destroy the enemy station.
- Removable: deleting the tutorial folders + a short list of seam lines leaves no broken code and no empty methods.

## Decisions (2026-11-10, user)
- `GameMode.Tutorial` is a real mode. Battle = Mode (Skirmish/Survival/Simulation/Tutorial) × Session (offline/online, existing `IMatchSession`) × player line-up (preset). Solo vs co-op = line-up, not a mode.
- Play menu gets a Tutorial button beside Skirmish / Survival / Simulation / Online.
- **Faction pick screen** before the tutorial; enemy = an opposing faction.
- **Skirmish setup screen hints** the first time the player opens it (factions, map size, AI slots, difficulty, start).
- **First-time suggestion**: popup "Play the tutorial?" → Yes opens the tutorial faction pick; No → Tutorial button highlighted until the player finishes the tutorial or wins any battle.
- **No money waits**: if the player cannot afford what the current step asks for, the tutorial adds the missing money.
- Station in a map corner (example: top-right). Expansion goes to another edge corner (top-left), never the center first.
- Central zone already has **4 mining structures** → taking it = 4 sites, each Mine (economy) or Battle Asteroid (firepower), plus map control.
- Victory = destroy the enemy station. Final arc: central zone → enemy's extra mining structure → enemy station.
- **Fleet chapter = level the station to max**; at each level build every newly unlocked ship type, each with a short explanation of its role and advantages.
- Enemy must be weak and short: controlled, not a full AI economy.
- Ship-action lessons interleaved between main chapters.
- Reuse the in-match hint engine ([[TODOs/Features/Ingame_Tutorial_Plan|In-Game Tutorial Plan]]): lessons, steps, targets, anchors, card.

## Enemy
- Enemy = `PlayerRole.AiOpponent`, `EnemyAiDifficulty.Easy`, economy off (never builds or levels up).
  - Existing path: `AiPlayerScope` registers `PlayerService` for a station owner without economy → station spawns; `EnemyUnitCommander` commands what exists.
  - Easy profile: attacks the base only at fleet advantage ≥ 1.6, decides every 4 s, abilities 35 % chance → mostly defends.
- All enemy units come from `TutorialDirector`: start garrisons (center, enemy mine, station) + scripted groups per chapter. Nothing spawns on its own → length is controlled.
- Victory and defeat = existing Skirmish outcome rule (station destroyed). No `TutorialOutcomeRule`.
- Avoid: a new `EnemyAiDifficulty.Tutorial` or tutorial checks in `Services/Enemy`.

### Enemy mine (checked 2026-11-10)
- `CaptureSitesModule.ConfigurePlayer`: `SiteFacilityBuilder` and `EnemySiteConstructionController` register only for players with `BattleFeatures.Economy` → an AI without economy cannot build a Mine.
- No reusable "start owned + built" path: `CaptureSitesSaveSource.RestoreAfterUnits` only links facilities the save already spawned; `CaptureSitesSystem.TryStartConstruction` needs the owner's `ISiteFacilityBuilder` (economy model).
- Option A (inside tutorial folder): director spawns the enemy facility through `MiningFacilityFactory`, then `CaptureSitePresenter.Restore(...)` + `SetFacility(id)` + subscribe `OnRelease` / `ConstructionProgressChanged` (same linking as the save restore). Check that `MiningFacilityFactory` resolves in an AI scope without economy.
- Option B: chapter 15 targets a free site guarded by director-spawned enemy units (no enemy mine).
- Decide at chapter 15 implementation.

## Money top-up
- `TutorialDirector` checks the current step's price (unit, level up, research, facility) against the player's money; if short → `IIncomeProvider.AddMoney(missing)` (existing API, used by `EconomyService` and cheats).
- Only for the local human player; enemy is never topped up.

## Flow
Map: own station top-right; free mining structure at the top-left edge; central zone with 4 mining structures; enemy station in the opposite corner with one extra mining structure near it.

| # | Chapter | Enemy (director) |
|---|---|---|
| 1 | Camera: pan, zoom | — |
| 2 | Time: pause, resume, speed up | — |
| 3 | Fleet: select, drag-select, move | — |
| 3a | Ship actions: Stop, Guard (guard station) | — |
| 4 | First contact: Attack, Attack-Move | 2 fighters at a marked spot |
| 5 | Economy: build + place mining facility | — |
| 6 | Station: build unit, counters tooltip, build queue | Small raid once a unit is queued |
| 6a | Ship actions: Waypoint move (patrol route) | — |
| 7 | First level up → build newly unlocked ship | — |
| 8 | Abilities → auto-power | Frigate group as ability target |
| 8a | Ship actions: Hunt, Retreat (pull back damaged ship) | Strong group that forces a retreat, then leaves |
| 9 | Reinforcements: open, drag, Shift+drag | — |
| 10 | Minimap: camera, orders | Enemy shown on minimap |
| 11 | Research: economy, then fleet | — |
| 12 | Expand to edge: send ships to marked top-left corner → capture → build Mine → income note | Raid on the new mine → Guard |
| 13 | Level to max: level up → for each newly unlocked ship type: mark its button, card explains role + strong/weak against + key ability → build one → repeat until max level | Central garrison already in place |
| 14 | Take central zone: gather fleet → Attack-Move → abilities → capture 4 structures → choose Mine or Battle Asteroid per site | Garrison; one counter-attack after capture |
| 15 | Attack enemy's extra mining structure: capture it → enemy economy note | Small guard |
| 16 | Destroy enemy station: Attack-Move fleet, superweapon/abilities | Station defenders → victory on station kill |

### Chapter 13 ship explanations
- Data: one short role line per ship in tutorial settings (e.g. "Anti-fighter escort: shreds squadrons, weak to capital guns").
- Strong/weak against from existing matchup data (same source as tooltip `TooltipMatchupRowView`), not retyped.
- Faction pick → each faction needs role lines for its ships.
- Risk: many ship types → long chapter. Mitigate: money top-up, build queue fills in parallel, suggest speed up.

## Menu
- **Play menu**: Tutorial button → tutorial faction pick screen → `StartBattle(TutorialPreset.Create(faction))`.
- **First-time popup** on main menu, **first launch only** (closing it without answering counts as No): Yes → faction pick; No → highlight Tutorial button (existing `TutorialAnchor` frame).
  - Highlight ends when the tutorial is finished or any battle is won.
  - Stored in a small tutorial progress file (not settings; settings use draft/apply). Flags: `suggestionAnswered`, `tutorialCompleted`, `anyBattleWon`.
- **Skirmish setup hints**: first time the Skirmish screen opens → short steps over its controls. Menu has no hint engine today (engine is registered per battle in `HumanPlayerScope`) → needs a menu-scope hint runner or a small step list in the tutorial menu folder.

## Architecture
### Step 0 — new modes plug in safely (done 2026-11-10)
- Audit: ~35 `GameMode` / `OutcomeRule` checks. All but two are Survival- or Simulation-specific → a new mode falls through to Skirmish behaviour, which the tutorial wants (station-kill victory text, notifications, music).
- Many checks run outside the battle container (save repository, load menu, asset preload, end screen) → moving all of them into `IBattleMode` is not possible cleanly. Not done; avoid re-proposing a full move without a concrete need.
- Fixed the two that would break a new mode:
  1. `BattleModes.Create` switches on `battle.Mode` (was `Outcome`; a Tutorial with `OutcomeRule.Skirmish` would have built `SkirmishMode`).
  2. Saveable modes have one source: `BattleSetupSave.Supports(GameMode)` = Skirmish or Survival. Used by `BattleSaveActions.IsSupported`, `BattleSaveService.CanLoad` + refusal, `JsonBattleSaveRepository.GetFolder`. Was `Outcome != Simulation` → a Tutorial would have been saveable and thrown in `BattleSetupSave.From`. New modes are unsaveable by default.
- Refusal text now `"{Mode} battles are not saved."` (was "Battle Simulation is not saved.").
- Verified: Unity recompile clean, console has no errors. Tests not run (local override); touched categories `GameFlow`, `Core`. Not committed.
- Avoid: implementing the tutorial as `IBattleModule` — 4 required methods, modules carry empty ones (`FogModule`).

### Tutorial (own folders)
- `Services/Tutorial/Guided/`: `TutorialMode : IBattleMode` (reuses Skirmish outcome rule), `TutorialDirector` (listens to `IHintService.Closed` → spawns enemy garrisons/groups, places the enemy mine, tops up money).
- `Entities/Game/TutorialPreset.cs`: faction from the pick screen, map, line-up (human + Easy AiOpponent without economy).
- `Entities/MainMenu/Tutorial/`: faction pick screen, first-time popup, button highlight, Skirmish setup hints, tutorial progress file.
- `Assets/Settings/Data/Tutorial/Guided/`: chapters (reuse existing lesson assets), enemy groups, ship role lines, tutorial hint track.
- Not saveable → `TutorialMode` registers no save/autosave services.
- Hint progress: tutorial always plays from the start; it never reads or writes saved hint progress.
- Dependencies one way: tutorial → hint engine, spawner, enemy AI, economy. Nothing else references the tutorial.

### Step 1 — mode + menu (done 2026-11-10)
- `GameMode.Tutorial = 3`; `BattleModes` arm → `Services/Tutorial/Guided/TutorialMode.cs` (offline `IMatchSession`, `LocalPlayer`, `BattleVictoryModel`, `StationFacingService`; no save services).
- `Entities/Game/TutorialPreset.cs`: `Create(FactionType)`; human (`BattleFeatures.All`) vs `AiOpponent` Easy with `BattleFeatures.None`; opponent = Republic↔Separatist, Rebellion↔Empire; `DestroyOpponentBase`; `MapType.Full`, Small, Coruscant, new seed (placeholder until tutorial map).
- Enemy without economy resolves statically: `EnemyUnitCommander` itself is the `IEnemyAiStateProvider`; `PlayerService` spawns its station; capture-objective list may be empty (same as Survival `WaveAttacker`). Not run in-game.
- Menu: `Entities/MainMenu/Tutorial/` — `ITutorialRoute`, `ITutorialPresenter`, `TutorialFactionUi` (4 faction buttons + Back), `TutorialRouteUiController` (`StartBattle(TutorialPreset.Create(faction))`). View named `TutorialFactionUi`, not `TutorialUi` (namespace `EmpireAtWar.Entities.TutorialUi` exists).
- Prefabs: `PlayUi.prefab` gets `TutorialButton` first (others shifted 82 px, panel 650 → 732); `Assets/Prefabs/Ui/MainMenu/TutorialFactionUi.prefab` copied from PlayUi styling; addressable `TutorialFactionUi` in `Ui` group.
- Test updated: `MainMenuUiControllerLifecycleTests.PlayRoute_ReusesViewForwardsModesAndReturnsOnCancel` covers the Tutorial button.
- Hints in the tutorial currently use the normal `HintTrack` and saved progress (tutorial track + fresh progress = later step).
- Verified: recompile clean, console clean, prefab references + addressable read back. User in-game 2026-11-10: Tutorial button visible, faction pick → battle loads. Tests not run (`Ui`, `GameFlow`, `Core`); not committed.

### Step 2 — director + tutorial hint track (done 2026-11-10)
- Engine seam: `IHintVisibility` (`IsOn`) replaces `ISettingsService` in `HintService`. Default `SettingsHintVisibility` (Show tutorial hints setting); tutorial `AlwaysOnHintVisibility`.
- `HintRegistrations.RegisterEngine(builder)` = engine + card without track/visibility/progress; `Register` adds `HintTrack` + settings visibility + cheat panel.
- `HumanPlayerScope`: `Mode == Tutorial` → `GuidedTutorialRegistrations.Register(builder, Assets)` instead of normal hints.
- `GuidedTutorialRegistrations`: `TutorialScript` (addressable `TutorialScript`, `Model` group) + its `Track`, `AlwaysOnHintVisibility`, `TutorialHintProgress`, engine, `TutorialDirector`.
- `TutorialHintProgress : IHintProgressRepository`: fresh per battle, never written to `hint_progress.json`; `Save` raises `Changed` (director wake-up, no empty member).
- `TutorialDirector` (local player scope):
  - Enemy groups: when `TutorialHintProgress` holds the group's `afterLesson` id (empty = battle start), spawns its units for the enemy faction via `PlayerFactoryLookup` at `Lerp(enemyStation, playerStation, alongFromEnemy)`, sunflower spread `spawnSpread` (60). Only while `BattleState.Running`.
  - Money: tops up to `GetMoneyFloor(level)` on money change and level-up completion.
- Data: `TutorialScript`, `TutorialEnemyGroup` (afterLesson, alongFromEnemy, per-faction units), `TutorialFactionUnits` (reuses `SurvivalWaveEntry` for unit × count).
- Assets `Assets/Settings/Data/Tutorial/Guided/`:
  - `TutorialHintTrack`: onboarding camera → time → fleet → facility → station → level_up → new_units → reinforcements → deploy_all → minimap → research_economy → research_fleet → shortcuts; events attack, abilities, carrier, hero, superweapon, hazard; gaps 0.5 / 0.4 s, no combat hold.
  - `TutorialScript`: money floor per level 2500 / 3700 / 4500 / 6000 / 6000 (level-up prices 2000 / 3200 / 4000 / 5500); groups after `lesson_fleet` (2 fighter squadrons @0.55: ARC170 / Vulture / XWing / TIEFighter), `lesson_station` (2 corvettes @0.5: CR70 / PatrolFrigate / CorellianCorvette / RaiderCorvette), `lesson_new_units` (2 frigates @0.45: Carrack / C9979 / NebulonB / Tartan).
- Known limit: the Easy AI commands every enemy unit, scripted ones included (it may hunt or assault with them). Scripted orders would need a seam in enemy code; decide after playtest.
- Not yet: new chapters (Stop/Guard split, waypoint, Hunt/Retreat, edge mine, level to max with ship explanations, central zone, enemy mine, station kill) — need new lessons/conditions/targets. Tutorial map.
- Verified: recompile clean, console clean, assets read back through the API. Tests not run (`Ui`, `GameFlow`, `Core`); not run in-game; not committed.

### Playtest 1 (2026-11-10) — fixed, in-game check pending
- User: no cards after building the Acclamator; tutorial never asked to capture a mining structure, build all 5 mining facilities or level the station repeatedly.
- Cause: not an engine stall. Onboarding runs the first lesson whose trigger fired, so while builds/level-ups were in progress, minimap, research, shortcuts, reinforcements ran early; after the Acclamator the track was empty. Card open/close and money top-up checked: fine.
- Engine (generic): `HintConditionId.MiningFacilitiesAtLimit` (`MiningFacilityLimitHintCondition`: every unlocked facility kind at `IsUnitLimitReached`; queued/waiting/placed count). `HintStateId.LevelUpReady` (below `StationLevelData.MaxLevel`, no `LevelUnitRequest` queued), `LevelUpIdle` (none queued). `HintStates` now takes `IPlayerFactionModelObserver` + `StationLevelData`.
- New tutorial assets `Assets/Settings/Data/Tutorial/Guided/Hints|Lessons/`:
  - `lesson_facilities_all` (trigger `MiningFacilityAffordable`): `facilities_all` — buy marked facility until limit.
  - `lesson_level_max` (trigger `LevelUpgraded`): `level_up_3` → `new_unit_3` → `level_up_4` → `new_unit_4` → `level_up_5` → `new_unit_5`; buy steps need `LevelUpReady`, unit steps need `LevelUpIdle`; both fix to silent `level_wait` (lesson parks while the upgrade builds). Unit text points to Strong/Weak against on hover (per-ship role lines still TODO).
  - Existing `Lesson_mine` re-used (select unit → move into marked mining site → capture (silent) → build Mine → income).
- Track onboarding now: camera, time, fleet, facility, facilities_all, station, level_up, new_units, level_max, mine, reinforcements, deploy_all, minimap, research_economy, research_fleet, shortcuts.
- Money floor raised to cover new units: 3000 / 6000 / 8000 / 12000 / 12000 (Acclamator 5800 was above the old L2 floor 3700).
- Edge: station already at max when `level_max` starts → its remaining step parks forever (harmless). Mining site target = nearest free to the station, not yet a specific edge corner.
- Verified: recompile clean, console clean, ids unique, asset fields read back. Tests not run; not run in-game; not committed.

### Playtest 2 (2026-11-10) — implemented, in-game check pending
- User: Shift+drag should deploy the 5 built facilities; deploy marker beside the station, not on it; long gaps; build several ships from the start (not one per level); attack the center before level 5 (levelling optional); after the center, capture mining zones again; center and enemy-station attacks never happened (not built yet).
- Engine (generic):
  - `HintTargetId`: `NearOwnStation` (15 % from station toward site middle), `CentralMiningSite`, `EdgeMiningSite`, `EnemyStation` (first hostile living station via `IPlayerRoster`). `HintTargets` takes `IPlayerRoster`; `IsSiteOf(target, site)`.
  - Central zone = mining sites within 2× the nearest mining site's distance to the middle of all sites. Measured on Small 2-player maps (seeds 1/7/42): 4 sites at 273, others ≥ 1720.
  - `HintDefinition.completionCount` (min 1); factory `CreateCompletion(HintDefinition)` so completions can read the step's target and count. Step-only ids throw when used as a trigger.
  - Conditions: `TargetSiteHintCondition` (`UnitInTargetSite`, `TargetSiteCaptured`, `TargetSiteBuilding`; capture/build count only progress after the step starts → repeatable), `CentralZoneHintCondition` (`CentralSiteCaptured`, `CentralZoneTaken`), `FleetOwnedHintCondition` (`FleetOwned` = 5 living ships), `ShipsQueuedHintCondition` (`ShipsQueued`, count), `ReserveHintCondition` (`ShipsBuilt`, `ShipsDeployed`, `MiningFacilitiesBuilt`, `MiningFacilitiesDeployed`).
- Content (`Guided/Hints|Lessons`):
  - `lesson_facilities_all`: buy to limit → silent `facilities_ready` → `facilities_open` → `facilities_deploy` (Shift+drag beside station).
  - `lesson_build_fleet` (trigger None, right after station): queue 5 ships → silent `fleet_ready` → `fleet_open` → `fleet_deploy`.
  - `level_max` unit steps now `ShipsQueued` 3 ("new unit and 2 more").
  - `lesson_central` (trigger `FleetOwned`, independent of level): select fleet → attack-move into central ring → silent capture → build Mine/Battle Asteroid → 8 s "hold all 4" note.
  - `lesson_expand` (trigger `CentralZoneTaken`): edge mining site move → silent capture → build Mine.
  - `lesson_enemy_station` (trigger `CentralSiteCaptured`): select fleet → attack-move the marked enemy station → 10 s "win the battle" note.
  - Shared `Hint_facility_place`, `Hint_reinforce_deploy`, `Hint_reinforce_all` target `NearOwnStation` (normal hints too).
  - Track: camera, time, fleet, facility, facilities_all, station, build_fleet, level_up, new_units, level_max, central, expand, enemy_station, minimap, research_economy, research_fleet, shortcuts. Removed from the tutorial track: reinforcements, deploy_all (covered), mine (replaced by expand). Gaps 0.3 / 0.3 s.
  - Enemy groups: fighters @0.75 after fleet, corvettes @0.6 after station, frigates @0.55 after new_units, 3-ship central group @0.5 after build_fleet, 2 frigates @0.12 after central.
- Verified: recompile clean, console clean, step ids unique, placeholders covered, no step-only trigger. Tests not run; not run in-game; not committed.

### Playtest 3 (2026-11-10) — implemented, in-game check pending
- User: facility deploy card switched to ships; "open reinforcements" asked while open; Shift+drag unclear; level-ups too long and nothing to do meanwhile; "select station" asked while selected; deselecting after a move order asked to select + move again (orders ignored); minimap attack-move not counted; build more ships; Shift hint whenever 2+ units wait.
- Engine (generic):
  - `TutorialAnchorId.ReinforcementFacility` — `ReinforcementUi.RefreshTutorialMarks` marks the `MiningFacilityUnitRequest` card.
  - `IReinforcementPanelObserver.IsOpen` (`ReinforcementUiController.Activate`); `ReinforcementPanelHintCondition` met when already open.
  - `SelectionHintCondition` met when the selection already matches at start.
  - `OrderIntoSiteHintCondition` (`OrderIntoTargetSite`): Move / AttackMove / last waypoint point inside a not-owned site of the step target kind; `IUnitOrderService.OrderIssued` covers map and minimap (`PlayerOrderInputHandler.TryIssueMove` → `IssueAttackMove` when Attack-move is pending). Units need not arrive or stay selected.
- Tutorial: `TutorialScript.buildSpeed` = 3 → `TutorialDirector` calls `PlayerFactionModel.Advance(deltaTime × 2)` while Running (all player builds 3× faster; level 1→2 120 s → 40 s).
- Content:
  - `facilities_deploy` anchors `ReinforcementFacility`, clearer Shift text.
  - `central_move`, `expand_move` complete on `OrderIntoTargetSite`.
  - `fleet_build` 6 ships; `fleet_deploy` text explains Shift.
  - New lessons triggered by `LevelUpQueued` (run during the upgrade): `home_mine` (order into nearest non-central site → silent capture → Mine), `satellite_take` (order into pad → silent capture → satellite), `fleet_more` (queue 4 → silent built → open → Shift deploy).
  - Event `lesson_deploy_all` (trigger `SeveralInReserve`) back in the tutorial.
  - Track: camera, time, fleet, facility, facilities_all, station, build_fleet, level_up, home_mine, satellite_take, fleet_more, new_units, level_max, central, expand, enemy_station, minimap, research_economy, research_fleet, shortcuts.
- Verified: recompile clean, console clean, ids unique, placeholders covered, step-only ids not used as triggers, site steps have targets. Tests not run (`Ui`, `BattleInput`, `Economy`, `Capture`); not run in-game; not committed.

### Playtest 4 (2026-11-10) — implemented, in-game check pending
- User: no lessons after level 4, no fleet lesson; expansion target on the opposite edge (top-right player sent bottom-right, wanted top-left); capture lessons pulled fighting units away; Shift needs a Shift + LMB glyph and only on cards with 2+; big attacks should use select-all; build-N steps need a live counter; name what to build at level 1 (corvettes).
- Root cause (engine bug, also affects normal hints): `HintService.RunLesson` created a listen-from-shown step's condition while its fix step / silent wait showed. `level_up_5` saw the level-4 upgrade already queued, `ShipsQueued` steps saw ships already queued → completed unseen. Fix: create the condition only when `show == step.Definition`.
- Engine:
  - `ShipsQueuedHintCondition` counts ships ordered since the step started (queued + completed − queued at start; cancels subtract) and is an `IHintTextSource` for `[count]` ("2/6"). `HintService.TryGetToken` asks the visible step's condition before the trigger; `HintUiController` refreshes the body each LateTick when it has a `[token]`.
  - `HintInputFormatter`: a fixed control written `A+B` is one chord (`<Keyboard>/shift+<Mouse>/leftButton` → Shift + LMB glyphs).
  - `TutorialAnchorId.ReinforcementStack` (first non-facility card with count ≥ 2); `HintStateId.ShipStackInReserve`.
  - `HintTargets` site ranking: distance to own station minus distance to nearest hostile station (our side first).
- Content:
  - Fleet deploy split everywhere: `*_shift` (skipUnless `ShipStackInReserve`, `SeveralDeployed`, Shift + LMB, `ReinforcementStack`) then `*_deploy` (drag the rest). `facilities_deploy` and shared `Hint_reinforce_all` show the Shift chord.
  - `fleet_build`: "Queue 6 of the marked ship: at level 1 these corvettes are your fleet. Ordered [count]." anchor `BuildShipButton`; `[count]` also on `more_build`, `new_unit_3..5`.
  - `lesson_expand_home` (trigger `LevelUpQueued`) replaces home_mine + satellite_take: build 3 → silent built → open → Shift → deploy → select new ships (box near station) → mining site on our side → capture → Mine → satellite pad → capture → satellite.
  - `central_select`, `enemy_select` → select-all shortcut (`SelectAllUsed`).
  - Track: camera, time, fleet, facility, facilities_all, station, build_fleet, level_up, expand_home, fleet_more, new_units, level_max, central, expand, enemy_station, minimap, research_economy, research_fleet, shortcuts.
- Verified: recompile clean, console clean, no null/dup ids, placeholders covered, `[count]` only on `ShipsQueued` steps, no unused guided hint assets. Chord glyph not render-checked. Tests not run; not run in-game; not committed.

### Seams outside tutorial folders (removal = delete exactly these)
1. `GameMode.Tutorial = 3` — explicit value, never reuse (saves store the number).
2. One arm in the `BattleModes` switch.
3. Play menu: `PlayUi.tutorialButton` + listener, `IPlayPresenter.OpenTutorial`, `PlayRouteUiController` dependency + method, `TutorialButton` on `PlayUi.prefab` (shift buttons back 82 px), `MainMenuScope` registration, `UiPlacement` entry, `TutorialFactionUi` addressable, test lines in `MainMenuUiControllerLifecycleTests`.
4. Tutorial map: `MapType.Tutorial` + one layout arm, or a hand-made layout asset.
5. Main menu scope: one registration for the first-time popup + highlight + Skirmish setup hints.
5a. `HumanPlayerScope`: the `Mode == Tutorial` branch to `GuidedTutorialRegistrations` (`IHintVisibility` / `RegisterEngine` stay — normal hints use them).
6. "Any battle won" recorder: one registration where battle results are known (battle scope).

## Edge Cases
- Player skips ahead (e.g. builds before told) → step completes silently, as in hints.
- Player attacks the enemy station early → allowed; ends with victory.
- Player loses the station → defeat; enemy is weak, so this should be rare. Defeat → offer restart.
- Player quits the tutorial mid-way → highlight stays until finished or any battle won.
- Online / co-op → not supported; Tutorial is offline only.

## Open Questions
- None (popup timing decided 2026-11-10: first launch only).

## Test Categories
- Step 0: `GameFlow`, `Ui`, `Core`. Tutorial: `Ui`, `GameFlow`, `EnemyAi`, `Capture`, `Economy`; add `Tutorial` category with the first fixture.

## TODO
- [x] Enemy mine check (2026-11-10): no existing path; options A/B recorded under Enemy.
- [x] Step 0 (2026-11-10): mode class by `GameMode`; `BattleSetupSave.Supports` single saveable-mode source.
- [x] `GameMode.Tutorial`, `TutorialMode`, `TutorialPreset` (2026-11-10). Tutorial map still open (uses `MapType.Full` Small for now).
- [x] Play menu Tutorial button + faction pick screen (2026-11-10).
- [ ] First-time popup, button highlight, tutorial progress file, battle-won recorder.
- [ ] Skirmish setup screen hints.
- [x] `TutorialDirector`: enemy groups after lessons, money top-up; tutorial hint track with fresh progress (2026-11-10).
- [ ] Director: central garrison, enemy mine, station defenders (with the late chapters).
- [ ] Ship role lines per faction.
- [ ] New lessons: Hunt/Retreat, edge expansion, level to max, central zone, enemy mine, station kill.
- [ ] User in-game check.
