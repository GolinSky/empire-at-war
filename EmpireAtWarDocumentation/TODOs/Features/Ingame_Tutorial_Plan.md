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
  - Avoid: red/yellow (danger/warning) and team colours for hints; Next-button progression; callout bubbles (B) and top banner (C) — rejected.
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
- Known issue: `InputBindingServiceTests` (and Settings binding labels via `GetBindingDisplayString`) depend on the OS keyboard layout; fail when a non-Latin layout is active.

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
