---
category: Features
status: in-progress
created: 2026-11-08
---

# In-Game Tutorial Plan

## Goal
- In-match hints in Skirmish/Survival: tell the player what to press, highlight the UI/world target, close when the action is actually done.
- Covers camera, selection, movement, minimap, building, unit counters (tooltip strong/weak), build queue, abilities + auto-power, station level up, research, then map systems.
- Works with rebound keys; never blocks play; never changes simulation (safe in multiplayer).

## Decision
- **In-match hints only** (2026-11-08). No separate tutorial mode for now.
  - Guided Tutorial battle (`GameMode.Tutorial`) → deferred; not in this plan's scope. Hint engine (steps, conditions, anchors, card) should be reusable for it later.
  - Avoid: tutorial logic inside existing presenters (`FactionUi`, `ShipUi`, `MiniMapUi`); forced or blocking hints.
- **Two hint kinds, one engine.**
  - **Basics track**: ordered hints from match start (camera → select → move → … → research). Next basic hint shows only after the previous is done or dismissed.
  - **Event hints**: one-shot, fired on first occurrence (level up affordable, ability unlocked, capture site seen…).
- **Non-blocking.** Input stays live; no `IInputLock`. Each card has Dismiss; "Turn off hints" on the card.
- **Pacing.** Max one card visible; min gap between cards **3 s**; event hints queue by priority.
- **Combat pause** (decided 2026-11-09): basics track waits while any owned unit fires or takes damage; resumes after **~5 s** quiet (tune). Combat-relevant event hints (attack, abilities, auto-power) still show. Signal: reuse `AutoPowerCombatSnapshot` if it fits, else a read-only in-combat observer.
- **Card layout A** (decided 2026-11-09): fixed coach card at a screen edge, clear of minimap/build bar/selection panel.
  - Card: `#080C14` @ 0.96 bg, 3 px cyan `#38BDF8` left accent, cyan uppercase title + basics progress (`5/7`), body ≤ ~12 words verb-first, key-cap glyphs, "Dismiss (Esc) · Turn off hints".
  - Raycasts only on card buttons; no full-screen dim.
  - Target highlight: pulsing cyan ring on UI anchor (only pulsing element); world target → ground ring + bouncing chevron; off-screen target → screen-edge arrow.
  - Completion: ~0.5 s checkmark, card slides out; slide-in ~0.2 s; quiet audio cue.
  - Red is reserved for the required mouse control (user request 2026-11-09); keep target frames cyan. Avoid: yellow warning accents and team colours for hints; Next-button progression; callout bubbles (B) and top banner (C) — rejected.
- **Local only.** Hints are local-player UI; nothing goes through the multiplayer command sink. `BattleSimulation` mode excluded.
- **Plain text** in hint SOs (project has no localisation). Avoid: localisation keys/tables.
- **Text-only card** — no advisor portrait, no voice.
- **Content is data, not code** → hint list/order in ScriptableObjects; no hardcoded hint text or order.

## Architecture
- `HintService` (pure C#, `ITickable`): owns basics-track position, event-hint queue, pacing, seen/completed flags. One source of truth.
- `HintDefinition` (ScriptableObject, `Assets/Settings/Tutorial/`): id, title + body plain text, `TutorialAnchorId` to highlight, optional world target, key binding slot(s) to show, trigger condition (event hints), completion condition, priority.
- `HintTrack` (ScriptableObject): ordered list of basics `HintDefinition`s; event-hint list.
- `IHintCondition` (Strategy), one class per condition, observing existing events — no polling of UI:
  - camera moved/zoomed → `ICameraInput` / `CameraService`
  - selection changed (count, unit kind) → `ISelectionService` (`INotifier<ISelectionSubject>`)
  - order issued (Move/AttackMove/Waypoint/Stop/Guard…) → `UnitActionId` via `UnitActionsPresenter` / `Services/UnitOrders`
  - minimap camera move / minimap order → events from `MiniMapUi` (`MoveCamera`, `OrderMove` — add observer events)
  - unit queued / completed / cancelled → `IPlayerFactionModelObserver.OnProductionChanged`, `OnUnitCompleted`
  - level up affordable / completed → `IFactionLevelObserver.LevelUpgradeCompleted` + economy money
  - research affordable / completed → `IFactionResearchModelObserver.OnResearchCompleted` + economy money
  - ability used / auto-power toggled → `ShipAbilityService`, `PlayerAutoPowerService` (add read-only observer if missing)
  - tooltip shown for anchor X + N seconds → `TooltipLifecyclePresenter`
  - entity seen / entered area (capture site, hazard, enemy) → entity locator, vision, owned-area events
- `TutorialAnchor` MonoBehaviour with serialized `TutorialAnchorId`, placed on UI prefabs (build buttons, level button, research buttons, ability bar, auto-power button, unit action buttons, minimap, money/HUD). Registers into `ITutorialAnchorRegistry` on enable — no `Find`/`GetComponent`.
- `HintUi` (View) + `HintPresenter` (MVP fits: real state + input):
  - card (title, 1–2 lines, key glyphs, Dismiss, Turn off hints); placed so it doesn't cover minimap/build panel.
  - highlight: pulsing frame on anchor RectTransform (no full-screen dim — play continues); arrow to world target (screen-projected marker).
  - key glyphs read from `IInputBindings` / `BindingSlotCatalog` so rebinds display correctly.
- Persistence: `GameplaySettingsData` → `showHints` (default on), completed/dismissed hint ids. Settings: "Show hints", "Reset hints".
- Analytics (optional): hint shown/completed/dismissed through existing `IAnalyticsService` ([[TODOs/Features/Gameplay_Analytics|Gameplay Analytics]]).

## Basics Track (ordered, from match start)
Each hint: card → highlight → completion condition → short confirm feedback.

1. **Camera** — pan (keys/edge), zoom (wheel), rotate if bound. Done: moved ≥ N units, zoomed.
2. **Select units** — left-click a ship; drag marquee (`ShipMarqueeHull`); selection group (`ShipSelectionGroupUi`). Done: ship selected; marquee used.
3. **Move units** — right-click ground; waypoint move; Stop; Guard. Highlight matching `UnitActionButton` + hotkey. Done: move order issued.
4. **Minimap** — left-click/drag moves camera; right-click orders selected ships (`MiniMapUi.OrderMove`). Done: camera moved via minimap; order via minimap.
5. **Build a unit** — highlight build panel, cost vs money; queue one ship. Done: unit queued.
6. **Unit types & counters** — hover build buttons; highlight tooltip `TooltipMatchupRowView` (Strong against / Weak against). Done: unit tooltip open ≥ N s.
7. **Build queue** — pipelines (`MAX_ACTIVE_PIPELINES = 7`), cancel = refund, unit limit. Done: 2+ units queued or one cancelled.

## Event Hints (one-shot)
- **Attack** — first enemy visible while ships selected → right-click enemy / Attack-Move.
- **Ship abilities & actions** — first owned ship with abilities selected → ability bar, instant vs targeted, cooldown, ability tooltip (`ShipAbilityTooltipContent`).
- **Auto-power** — after first manual ability use → `ShipAutoPowerButtonUi` toggle.
- **Station level up** — level upgrade first affordable → level button (`FactionUi.CreateLevelUnit`); after completion: new units unlocked.
- **Research** — research tier first affordable → research entry; after completion: stat effect on tooltip.
- **Capture site** — first seen (Mine + Battle Asteroid on one site).
- **Mining facility** — first owned → income.
- **Reinforcement zone** — first available deploy.
- **Defense satellite pad** — first seen/captured.
- **Super weapon** — first ready.
- **Heroes** — first available.
- **Space hazard** — own ship first enters Nebula / Ion Storm.
- **Fog of war / radar** — first enemy hidden by fog.
- **Squadrons** — first hangar launch.
- **Survival** — first wave timer → "start wave now".

## Implementation Phases
- [x] **Phase 0 — decisions** (2026-11-09): gap 3 s, combat pause on, card layout A. Optional: Penpot mock of A in allowed project before Phase 2; read `UI_UX_GUIDELINES` + `UI_CODE_BUILD_GUIDE` before UI work (Penpot mock in allowed project, read `UI_UX_GUIDELINES` + `UI_CODE_BUILD_GUIDE`).
- [x] **Phase 1 — engine** (2026-11-09; compiles clean, tests not run, not run in-game):
  - `Services/Tutorial/`: `HintService` (`IHintService`: `Shown`, `Closed(def, completed)`, `Current`, `Dismiss()`), `HintDefinition`, `HintTrack`, `HintConditionId`, `HintConditionFactory`, `Conditions/` (ShipSelected, UnitQueued, UnitCompleted, LevelUpgraded, ResearchCompleted, Immediate, Never), `LocalUnitDamageTracker`, `HintProgress`, `JsonHintProgressRepository`.
  - Trigger `None` → show when pacing allows; completion `None` → only Dismiss closes. Completed-before-shown → recorded, never shown.
  - Combat signal = local unit lost hull/shields (`LocalUnitDamageTracker`); "own unit firing" not detected (no attack event). `AutoPowerCombatSnapshot` not used — it is enemy-proximity, not combat state.
  - Persistence: `GameplaySettingsData.showHints` (default true; no UI yet); done ids in separate `persistentDataPath/hint_progress.json` (not settings — settings use a draft/apply flow).
  - DI: `HintRegistrations.Register` from `HumanPlayerScope` local branch, skipped for `BattleSimulation`. Needs faction/research models, which live in the player scope.
  - Asset: `Assets/Settings/Data/Tutorial/HintTrack.asset` (empty; gap 3 s, quiet 5 s), addressable `HintTrack` in `Model` group.
  - Added `OnUnitCompleted` to `IPlayerFactionModelObserver`.
  - Not done: EditMode tests + `Tutorial` category in `AGENTS.md` table (add with first fixture).
- [x] **Phase 2 — UI** (2026-11-09): `Entities/TutorialUi/` `HintUi` (coach card top-left `(24,-140)`, 340 px, slide 0.2 s, DONE mark 0.5 s, pulsing 2 px cyan frame, world marker + edge arrow), `HintUiController` (`IHintPresenter`: Dismiss, TurnOffHints), `TutorialAnchor` (static active list — prefabs are not DI-injected).
  - Prefab `Assets/Prefabs/Ui/Tutorial/HintUi.prefab`, addressable `HintUi` (Ui group), placed on `dynamicCanvas` via `UiPlacement`.
  - Key names: first binding's path via `InputControlPath.ToHumanReadableString` (layout-independent; `GetBindingDisplayString` showed Cyrillic on a UA layout). Body `{0}` placeholders ← `HintDefinition.inputActions`.
  - Anchors placed: MiniMap, BuildPanel (FactionUi root), BuildQueue (ShipBuildUi root), Money, UnitActions, AttackMove/Stop/Guard buttons (no Move button exists), AbilityBar + AutoPowerButton (ShipUi + ShipGroupUi), TooltipMatchups (whole tooltip Panel), ReinforcementButton, SuperWeapons (MiddlePanel), Heroes, SurvivalHud (Start wave button). Level/research items are spawned dynamically → highlight BuildPanel.
- [x] **Phase 3 — observer events**: `IMiniMapInputObserver` (`MiniMapUiController`), `IAutoPowerToggleObserver` (`ShipUiController`); others reuse existing events (`IUnitOrderService.OrderIssued`, `ShipAbilityService.ManualAction`, `ITooltipModelObserver`, `ICameraInput`, economy/research polling). `IPolledHintCondition` for poll-only conditions; `IHintWorldTarget` for located triggers. Optional systems (economy, capture sites, Survival) via `IObjectResolver.TryResolve` → missing system = never-firing condition.
- [x] **Phases 4–7 — content**: `Assets/Settings/Data/Tutorial/Hints/Hint_*.asset`; `HintTrack` = 10 basics (camera pan, zoom, select ship, select fleet, move, minimap camera, minimap order, build, counters, build queue) + 18 events (attack, super weapon, abilities, auto-power, level up, level unlocked, research, hazard, Survival prep, mining site, reinforcements, mining income, satellite pad, heroes, squadrons, waypoints, stop/guard, fog). All basics listen from match start (done early → skipped).
- [ ] **Phase 8 — settings & polish**: done — `GameplaySettingsData.showHints` toggle row "Show tutorial hints" + "Reset hints" button in `SettingsUi` (tooltips added), `ISettingsService.SetShowHints` for card "Turn off hints", `JsonHintProgressRepository` moved to `ProjectScope` (shared cached instance; Reset applies next match). **Open**: audio cue (no neutral UI clip / one-shot API in `IInterfaceSfxService` — needs a sound choice); analytics funnel (optional, not done).
- Verification 2026-11-09: compiles clean, console clean, prefab diffs additive, hint ids unique, all input paths resolve. Full EditMode suite (user-requested): run 1 → 8 failures: `BattleHudControls_DoNotAcquireKeyboardFocusOrNavigate` (HintUi buttons had Automatic navigation → fixed to None) + 7 `InputBindingServiceTests` that read localized key names from the active OS keyboard layout (Ukrainian: `Ц` vs `W`; unrelated to hints). Run 2 → **2926/2926 passed**. Not run in-game.
- Rule: battle HUD `Selectable`s need `Navigation.Mode.None` (enforced by `BattleInputPrefabTests`).
- Fixed 2026-11-09: binding labels were OS-layout dependent (`GetBindingDisplayString` asks the device). `InputBindingService.GetBindingDisplayString` + hint card now use `BindingPathNames.Readable` (bound path, short names). Side effect: arrows read "Up Arrow" instead of "Up". `BattleInput` 272/272, `Ui` 249/249 pass (run under a Latin layout; Ukrainian layout not re-tested). Committed `8868fcda0`.

## Playtest fixes (2026-11-09, user feedback)
- Capture hint showed before ship selection → event hints now wait until basics are finished unless `interruptsBasics` (attack, super weapon ready, Survival prep).
- Level-up hint had no station marking → new basic `select_station`; `needsStationSelected` hints (build, counters, queue, level up, research) put the world marker on the local station (`IStationRegistry.TryGetLivingStation`) until it is selected.
- Level/research counted on completion → completion now `LevelUpQueued` / `ResearchQueued` (`QueuedRequestHintCondition`, request enters build queue = bought); follow-ups `level_building` / `research_building` point at the build queue until done.
- Capture site taught two things at once → `mining_site` (capture, done = `MiningSiteCaptured`) then `site_build` (choose Mine / Battle Asteroid, done = `MiningSiteBuilding`). `CaptureSiteStage`: Revealed, Captured, Building, MiningOperational.
- Hints stopped after attacking → ongoing combat held every non-combat hint forever; `HintTrack.maxCombatHoldSeconds = 20` caps the hold.
- `TutorialAnchor.visibility` = owning view's `CanvasGroup`; hidden views (alpha 0) no longer get a frame.
- Content now 11 basics + 21 events. Old saved progress keeps finished ids → use Settings ▸ Reset hints to replay.
- Verified: compiles clean, console clean, 32 hints validate. Tests not run for this batch; not run in-game. Not committed.
- Reset tools (2026-11-09): Cheats ▸ Tutorial ▸ "Reset and Restart Hints" (`HintCheatPresenter` → `IHintService.Restart()`: hides the card, clears progress, rebuilds queues live; registered only when the economy cheat panel exists). Editor: `Tools/Empire At War/Tutorial/Reset Hint Progress` (`HintProgressMenu`, disabled in Play Mode — the match would save its copy back). Settings ▸ Reset hints still applies from the next match. Compiles clean; not run in-game.
## Playtest round 2 (2026-11-09)
- Engine: `HintDefinition.after` chains event hints into step-by-step lessons; `autoCloseSeconds` for info hints (queue 4 s, mining income 5 s, hero/hazard 6 s, fog info 7 s); triggers keep their unit/site (`IHintWorldTarget` live, follows moving ships) and name it via `IHintTextSource` tokens `[unit]`, `[hazard]`, `[effects]` (`IHintService.ResolveText`).
- Specific buttons: `TutorialAnchor.Mark/Unmark` for runtime-spawned elements. `FactionUi` marks first shown ship (`BuildShipButton`), first fast ship sort order ≤ 1 (`ScoutShipButton`), first research (`ResearchButton`), level-up (`LevelUpButton`); `ShipAbilityBarUi` marks first ability (`AbilityButton`).
- Visuals: marks are yellow `#FFC83D` (frame 3 px, world ring, chevron, minimap ring) — blue-on-blue was unreadable. `HintWorldMarkerView` (pulsing ring + bobbing chevron, edge arrow), `HintDragSelectDemoView` (LMB-held mouse drags a box over 3 ships), `HintElementPreviewView` (still copy of the real HUD button on the card; scripts stripped before Awake). Sprites `Assets/Art/Textures/Ui/Tutorial/HintRing|HintChevron|HintDot.png`.
- Minimap API: `IMiniMapTargetMarker.ShowTarget/HideTarget` (`MiniMapUiController` → `MiniMapUi` → `MiniMapTargetView`); hint card mirrors every world target (station, sites, ships) on the minimap.
- Content (11 basics + 27 events): build/counters point at one deterministic ship; abilities = select [unit] → use highlighted ability → auto-power (with button preview); level/research highlight their button, short text, then timed queue note; satellite pad done on build start; reinforcements trigger = reserve not empty, done = unit deployed (capacity grows); hero names + marks the unit (data: Republic hero = Resolute, Rebellion = Home One, Falcon…); squadrons → `carrier` (own carrier, done = squadron ordered); stop/guard split with button highlights; hazard names ship/hazard/effects, only for hazards that change the ship (no hover-tooltip claim); mining income only when the mine is operational; fog = build scout → deploy → select → move into fog → timed info.
- Removed assets: `Hint_ship_abilities`, `Hint_stop_guard`, `Hint_squadrons`, `Hint_fog_of_war`.
- Verified: compiles clean, 38 hints validate. Tests not run; not run in-game; not committed. Parallel session committed round 1 as `3c8f24fee` (with its input glyphs).
- Rejected 2026-11-09: pausing enemy attacks while basics run (user: skip). Avoid re-proposing.

## Onboarding Redesign (playtest round 3, 2026-11-09) — implemented, in-game check pending
### Implemented (2026-11-09; compiles clean, console clean, content validated; tests not run; not run in-game; not committed)
- Data: `HintLesson` SO (id, steps, trigger, priority, interruptsOnboarding) in `Assets/Settings/Data/Tutorial/Lessons/`; `HintTrack` = `onboarding` + `events` lessons, `stepGapSeconds = 0.6`. `HintDefinition` = step: adds `target` (`HintTargetId`), `requires` (`HintRequirement` state → fix step), `skipUnless` (`HintStateId`), `listenFromShown`, `anchorFallback`; drops trigger/after/priority/interruptsBasics/needsStationSelected.
- `HintService`: one lesson at a time; onboarding = first lesson in track order whose trigger fired (waiting ones never block); events wait unless `interruptsOnboarding`. Fix steps never recorded; prerequisite lost mid-step → fix step again. Skip rules judged only once prerequisites hold. Dismiss skips the whole lesson. Ticks while Paused on unscaled time (pause lesson). Already-done lessons finish silently with no gap. Progress ids: step ids + `lesson_<id>`.
- `HintStates` (live: UnitSelected, StationSelected, AbilityUnitSelected, AutoPowerOn, AbilityNeedsMove = `AutoPowerRole.Travel`, AbilityNeedsTarget = `RequiresEnemyTarget`, AbilityInstant; `[ability]` token). `HintTargets` (OwnUnit nearest station, OwnStation, MiningSite/SatelliteSite = own unbuilt else free nearest station, OwnMine, Trigger). Station marking moved out of `HintUiController` into `select_station` fix steps.
- New conditions: `UnitInSiteHintCondition`, `GameTimeHintCondition` (Paused/Resumed/SpeedUp via `SkirmishStateModel`), `SelectionShortcutHintCondition` (`ISelectionInput`), `ReinforcementPanelHintCondition` (new `IReinforcementPanelObserver.Opened` on `ReinforcementUiController.Activate(true)`), `SeveralDeployedHintCondition` (reserve drops ≥ 2 in one placement), `ResearchQueuedHintCondition` + group filter on `ResearchAffordableHintCondition` (`ResearchGroup.IsEconomy` = IncreasedProduction, IncreasedStationIncome), `NewUnitQueuedHintCondition` (AvailableLevel == current level, level above start), `AnyAttackOrdered` (Attack or AttackMove), `SeveralInReserve`, `UnitOwned`, site `Exists` stage.
- Anchors: `PauseButton`/`SpeedButton` (CoreGameUi `UpperPanel/PlayButton`, `SpeedUpButton`); `TooltipMatchups` moved from tooltip Panel to `Strong Against` + `Weak Against` rows, frame = union of all visible anchors with one id (`TutorialAnchor.TryFindAll`); FactionUi marks `EconomyResearchButton`, `FleetResearchButton`, `NewShipButton` (`IFactionPresenter.IsUnlockedAtCurrentLevel`). Drag-select demo box fill alpha 0 (outline only).
- Content: 50 steps, onboarding: camera → time → mine (trigger MiningSiteExists) → fleet → minimap → satellite (SatelliteSiteExists) → station → level_up (LevelUpAffordable) → new_units (LevelUpgraded) → reinforcements (ReinforcementReady) → deploy_all (SeveralInReserve) → research_economy → research_fleet → shortcuts. Events: attack, superweapon, abilities (select → auto off → move if travel ability → targeted or instant use), hazard, survival, hero, fog, carrier.
- Deleted steps: `Hint_auto_power`, `Hint_move_fleet`, `Hint_reinforcements`, `Hint_research`, `Hint_select_ship`. Old saved progress → Settings ▸ Reset hints or Cheats ▸ Tutorial to replay.
- `HintService.cs` ≈ 370 lines (> 200): candidate split = lesson picking vs step runner; not done (out of scope).
- Open: guard targets the station (mine guardability not checked); `level_unlocked` waits for a fresh station selection; deploy step marks the station (no reinforcement-zone target).

### Playtest round 4 (2026-11-09) — fixed, in-game check pending
- Research lessons end on any research purchase: new `ResearchBought` (queued research count grows; research already queued does not count), `listenFromShown`; `research_building` step deleted. `ResearchQueuedHintCondition` now counts increases (group or any).
- Stall after research: shortcuts trigger `UnitOwned` used `LocalUnitHintCondition` (event latch + catalog lookup) and never fired in play → new polled `LocalUnitOwnedHintCondition` (any living local unit with `IMoveFacade`). `LocalUnitKind.Ship` removed.
- Squadrons: `HintTargetId.OwnUnit` now picks any local movable unit (ships and squadrons), nearest the station.
- Minimap vs screen marker: same world point and same math as minimap icons; off-screen the edge indicator also drew the target ring at the screen edge → now arrow only. Re-check in game; if still offset, note which marker is wrong.
- Played with old saved progress (old ids such as `site_build`, `research_building`, `level_unlocked` were recorded, so those steps were skipped). Reset hints before testing.
- Compiles clean, console clean; tests not run; not run in-game.

### Order change round 5 (2026-11-09) — implemented, in-game check pending
- Onboarding: camera → time (pause, resume, speed up, normal speed) → facility (trigger `MiningFacilityAffordable`) → level_up (`LevelUpAffordable`) → mine → fleet → minimap → satellite → station → new_units → reinforcements → deploy_all → research_economy → research_fleet → shortcuts.
- Facility lesson: `facility_build` (marked `MiningFacilityButton`, done = `MiningFacilityQueued`) → `facility_place` (reinforcement button, station marked, done = `MiningFacilityPlaced` = local `IMiningFacilityFacade` entity). Requires `HintStateId.MiningFacilityReady` (in reserve or placed) → fix `facility_wait` (build queue note).
- New units: `NewShipButton` = carrier first (`IFactionPresenter.IsCarrier` = ShipData.HangarBays > 0), else most expensive ship unlocked at the current level. User examples: Acclamator, Munificent.
- Fog: `fog_move` targets `HintTargetId.FoggedMiningSite` = hidden (not revealed), not owned mining site nearest the station; picked once and kept. Completion still `MoveIntoFog`.
- `GameTimeHintCondition.Kind.NormalSpeed` (Normal after Fast). 53 steps, 15 onboarding + 8 event lessons. Compiles clean, console clean, content validated; tests not run; not run in-game.

### Disabled lessons (2026-11-09, user request)
- `lesson_mine` (go to and capture the mining site, build Mine) and `lesson_fog` (scout into the fog) removed from `HintTrack`; assets kept (`Lessons/Lesson_mine.asset`, `Lessons/Lesson_fog.asset` and their `Hint_*` steps). Re-enable by adding them back to `HintTrack.onboarding` / `events`.
- Avoid: deleting their step assets as "unused".

### Playtest round 6 (2026-11-09) — implemented, in-game check pending
- User: tutorial too slow; no waiting cards, no delays. `HintDefinition.waitSilently` replaces `backgroundAfterSeconds`: waiting steps (`mining_site`, `satellite_capture`) and waiting fix steps (`facility_wait`, `fog_wait`) never show a card; lesson parks at once and resumes when done. Pacing: lesson gap 0.5 s, step gap 0.4 s, combat hold off (`combatQuietSeconds = 0`, `maxCombatHoldSeconds = 0`).
- Removed: waypoints step (useless, broke the mine flow), `level_building` card. Research lessons now right after level up (one by one, single step each).
- Build queue: only once, in the station lesson after build/counters: 4 s info on `BuildQueue`, cancel counts as done (`UnitCancelled` via new `IBuildQueueCancelObserver.Canceled` on `ShipBuildUiController.CancelBuilding`).
- Reinforcement drag was skipped (open/deploy completions latched from earlier deploys) → `listenFromShown`; `ReinforcementUi` marks `ReinforcementUnit` (first card) and `ReinforcementNewUnit` (card that got a unit last) through a new `ReinforcementPanel` anchor; drag steps frame the card, fallback the reinforcement button.
- Scout flow: `fog_build` = marked fighter (`ScoutShipButton` = first squadron or ship with sort order 0; `ScoutUnits.IsScout` = squadron or Fighter/Bomber/Interceptor), done = `ScoutQueued` → silent `fog_wait` until `ScoutReady` → `fog_deploy` drags the marked new card → `fog_select` marks `HintTargetId.NewestUnit` (highest local movable entity id), done = `NewestUnitSelected` → `fog_move` to the marked site.
- Fog target: `FoggedMiningSite` = free mining site in the outer half by distance from the middle of all sites (never the center), nearest the station; picked once.
- `TutorialAnchor.TryFindAll(None)` returns nothing (anchors without a step id never frame).
- Compiles clean, console clean, 52 steps validated; tests not run; not run in-game.

### Background waits (2026-11-09) — superseded by round 6 (silent waits)
- `HintDefinition.backgroundAfterSeconds`: a waiting step (or waiting fix step) shows that long, then the lesson parks (`HintService._parked`) and other lessons run. Parked lesson resumes first, between lessons, once its step is done or the step to show changes (`CanResume`).
- Set on `mining_site` capture 5 s, `satellite_capture` 6 s, `facility_wait` 5 s; texts say "carry on meanwhile". Mine lesson resumes at Build Mine after capture.
- Compiles clean, console clean; tests not run; not run in-game.

### Proposal (accepted 2026-11-09; user moved level up right after the station lesson)
### Decisions (user)
- Stays in Skirmish with hints on; no Tutorial mode.
- Onboarding teaches economy first: capture the mining site nearest the station and build a Mine as one of the first lessons. Not a balance change; nobody gets free mines.
- Missing prerequisite (no ship selected, station not selected) → insert a reusable small step, then return to the original step.
- Goal: by the end the player has a mine, a satellite, levelled station, research, built ships — not dead.
- Text: 1 short line; step 2+ of a lesson relies on markers, not prose. Never say "ship" when any unit works.

### Engine changes
- `HintLesson` SO = ordered steps + trigger + priority + flags. `HintTrack`: onboarding lessons (ordered) + event lessons. `HintDefinition` = one step (drops `after`/`trigger`).
- Prerequisites: step lists `requires` (live state id → reusable fix step). State conditions are live (`IsSatisfied`), not one-shot latches. Fix steps are never recorded as done.
  - Library: `SelectUnit` (mark own unit), `SelectStation` (mark station), `OwnUnit` (no units → station → build), `OpenReinforcements`.
  - Prereq lost mid-step (deselect) → back to the fix step.
- World targets per step (`HintTargetId`): OwnUnit, OwnStation, MiningSiteNearStation, SatellitePadNearest, OwnMine, ReinforcementZone. Target picked once per lesson and shared by its steps (same site for move → capture → build).
- Completion "move into marked ring" (order destination inside target site radius).
- Anchors: matchup rows only (not whole tooltip), pause button, speed button, reinforcement zone, all newly unlocked build buttons.

### Onboarding order
1. Camera: pan → zoom.
2. Time: pause/resume → speed up.
3. Mine: select unit (marked) → move to marked mining site → capture → select site → build Mine → income note (auto-close).
4. Fleet: drag-select (transparent box demo) → waypoints → guard the mine (marked) → stop.
5. Minimap: click marked satellite pad on minimap → [unit selected] right-click it on minimap → capture → build satellite.
6. Station: select station → build unit (no gap) → counters (mark matchup rows) → queue.
7. Level up (right after station): buy → queue note; after it completes: mark station → select → mark new unit → build it.
8. Reinforcements: open → drag unit near station → Shift+drag deploys all (own lesson, when 2+ of one kind wait).
9. Research: economy research (income/production) and fleet research (weapons/shields) as two lessons.
10. Shortcuts: select visible, select all.

### Event lessons
- Attack: completion = Attack or AttackMove (bug: attack-move to the suggested place left card open).
- Abilities: auto-power off first → use ability; step depends on ability (engines → move first, targeted → pick target, instant → click).
- Fog: info only right after the scout reaches the dark (no detached card).
- Hero, hazard, super weapon, Survival: unchanged.

## Input glyphs (2026-11-09)
- Implemented: `TutorialInputAtlas.png` (transparent 1536×1024; neutral/LMB/RMB/wheel/both mouse variants + blank keycap), sliced sprites in `Assets/Art/Textures/Ui/`; `HintUi.prefab` explicitly wires the input row and inactive glyph template.
- `HintInputFormatter` resolves primary `InputBinding.effectivePath`; axis keys use `/`, modifier chords use `+`, alternative actions use `or`, missing bindings show `Unbound`. `HintInputGlyph` overlays dynamic letters on the white keycap; active mouse controls are red.
- `HintUiController` subscribes/unsubscribes `IInputBindings.BindingsChanged`; open cards refresh without restarting their animation. Station prerequisite → live `Battle/Select`; selected station → configured purchase/hover input.
- Action-backed prompts: camera pan/zoom, ship/group/station/site selection, movement/attack, waypoints. Fixed `inputControls`: minimap LMB/RMB and UI button clicks. Hover uses neutral mouse; informational hints omit the input row.
- Copy: auto-power A is a button label, not a key; superweapon prompt explicitly covers clicking its activation button. Fixed minimap controls remain independent of rebinding `Battle/Command`.
- Verification: Unity imports saved; six sprites and all glyph Inspector references resolve; compilation finished; rendered camera pan, RMB movement, wheel/R/F zoom and LMB selection cards inspected. Preview-only render-target cleanup warning fixed; final Console delta has no errors. Tests not run (`Ui`, `BattleInput`); in-game acceptance pending.
- Broader plan stays active: audio cue and other existing unimplemented items remain out of this input-glyph task.

## Edge Cases
- Rebound or unbound key → glyph from current binding; unbound → action name + "unbound".
- Anchor not present (panel closed) → highlight the button that opens it first.
- Player already did the action → hint completed silently, never shown.
- Experienced player → basics auto-complete fast; "Turn off hints" available on first card.
- Highlighted unit dies → hint re-targets another owned unit or waits.
- Several triggers at once → priority queue, one card at a time.
- Hint visible at match end → discard; not marked completed.
- Multiplayer → hints shown locally per player; no network traffic.
- Pause menu / alt-tab → pacing timers pause with game.

## Test Categories
- New feature area → add `Tutorial` category to `AGENTS.md` Test Selection table.
- Also touches: `Ui`, `BattleInput`, `GameFlow`, `Economy` (level/research), `Abilities`.

## Files (planned)
- `Assets/Scripts/Services/Tutorial/` — `HintService`, conditions, `ITutorialAnchorRegistry`.
- `Assets/Scripts/Entities/TutorialUi/` — `HintUi`, `HintPresenter`, `TutorialAnchor`.
- `Assets/Settings/Tutorial/` — `HintTrack`, `HintDefinition` SOs.
- `Assets/Scripts/Services/Settings/GameplaySettingsData.cs` — hint flags.
- Anchors on: Faction build UI, `ShipAbilityBarUi`, `ShipAutoPowerButtonUi`, `UnitActionsView`, `MiniMapUi`, Tooltip prefab.

## Deferred
- Guided Tutorial battle (`GameMode.Tutorial`, fixed faction/map via `TutorialPreset` SO, scripted enemy, ordered chapters). Earlier decisions kept for it: fixed not hardcoded, plain text, text-only card.

## TODO
- Audio cue on hint show (pick a clip; add a one-shot UI method).
- Optional: analytics funnel (shown/completed/dismissed).
- EditMode tests for `HintService` + `Tutorial` category in `AGENTS.md`.
- User in-game check: card placement vs HUD, highlight frames, pacing, combat pause, settings rows.
