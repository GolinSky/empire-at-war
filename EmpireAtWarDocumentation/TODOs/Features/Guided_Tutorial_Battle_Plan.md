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

### Seams outside tutorial folders (removal = delete exactly these)
1. `GameMode.Tutorial = 3` — explicit value, never reuse (saves store the number).
2. One arm in the `BattleModes` switch.
3. Play menu: `PlayUi.tutorialButton` + listener, `IPlayPresenter.OpenTutorial`, `PlayRouteUiController` dependency + method, `TutorialButton` on `PlayUi.prefab` (shift buttons back 82 px), `MainMenuScope` registration, `UiPlacement` entry, `TutorialFactionUi` addressable, test lines in `MainMenuUiControllerLifecycleTests`.
4. Tutorial map: `MapType.Tutorial` + one layout arm, or a hand-made layout asset.
5. Main menu scope: one registration for the first-time popup + highlight + Skirmish setup hints.
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
- [ ] `TutorialDirector`: garrisons, enemy groups, enemy mine, money top-up.
- [ ] Ship role lines per faction.
- [ ] New lessons: Hunt/Retreat, edge expansion, level to max, central zone, enemy mine, station kill.
- [ ] User in-game check.
