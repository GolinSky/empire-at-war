---
category: Features
status: in-progress
created: 2026-09-30
tags:
  - ui
  - tooltip
  - plan
---
# Tooltip System Plan

- Coverage catalog (every hover target and its content): [[TODOs/Features/Tooltip_System_Plan - Research|Tooltip System - Research]]
- Rules to read before each UI phase: [[Rules/UI_CODE_BUILD_GUIDE]], [[Rules/UI_UX_GUIDELINES]], [[Architecture/PROJECT_ORGANIZATION]]

## Goal

- One contextual tooltip system (hover panel) for HUD, production, commands, and world objects.
- Logic lives in `ITooltipService`. Every caller goes through it; nothing drives the tooltip view directly.
- Content = definition data (names, descriptions, icons, matchups) + live state (health, cost, timers, availability).

## Rules (hard constraints)

- **Only `ITooltipService` shows or hides tooltips.** Gameplay code, presenters, and world-hover sources call the service.
- **Views never call the service.** A view forwards hover to its presenter → the presenter calls `ITooltipService`.
  - `TooltipTrigger` (view widget) → owning `<Feature>Ui` → `I<Feature>Presenter.OnTooltipHover*(...)` → `ITooltipService.Show(...)`.
- **`UiController` stays free of tooltips.** No `ITooltipService` in its constructor, fields, or helpers. Each presenter that needs tooltips injects `ITooltipService` itself.
- **The service does not know the view.** `TooltipService` updates `TooltipModel`. `TooltipUiController` watches `ITooltipModelObserver` and renders `ITooltipUi`.
- **No Unity types in the logic layer.** No `Sprite`, `Rect`, `Vector2`, or `RectTransform` in the service, model, or content. Use icon keys and the project's own anchor struct.
- **The tooltip never takes input.** Every `Graphic` in the tooltip prefab has `raycastTarget = false`. UI hover beats world hover.
- **Show one tooltip at a time.** A new request replaces the current one. A stale hide cannot close a newer tooltip.

## Architecture

```text
Hover source (View widget)            World hover source (input service)
TooltipTrigger ─event─► <Feature>Ui          │
        │ forwards                           │
        ▼                                    ▼
I<Feature>Presenter (UiController) ──►  WorldTooltipPresenter
        │ builds ITooltipContentProvider     │
        └──────────────► ITooltipService ◄───┘        (logic layer, pure C#)
                              │ delay, ownership, refresh
                              ▼
                        TooltipModel ──events──► TooltipUiController ──► ITooltipUi (TooltipUi : BaseUi)
                     (ITooltipModelObserver)     (creates via IUiService)
```

### Model / logic (pure C#, `Services/Tooltip/`)

- `ITooltipService`: public API.
  - `TooltipHandle Show(ITooltipContentProvider provider, TooltipAnchor anchor)`. Starts the hover delay or replaces the current tooltip.
  - `void Hide(TooltipHandle handle)`. Does nothing if the handle is no longer current.
  - `void HideAll()`. Used for route changes, HUD hide, and scene teardown.
- `TooltipService : ITooltipService, ITickable`. Owns the delay timer, the current handle, and the provider refresh interval.
- `TooltipModel : ITooltipModelObserver`. State = `IsVisible`, `TooltipContent Content`, `TooltipAnchor Anchor`.
  - Events: `Shown`, `ContentChanged`, `Hidden`.
- `ITooltipContentProvider`: implemented by presenters and entity adapters.
  - `bool IsValid { get; }`. Returns false when the target is destroyed, hidden, or no longer visible (fog of war).
  - `TooltipContent Build()`. Rebuilt each refresh tick for live values.
  - `object Key { get; }`. Same key + same source → the delay does not restart on small pointer moves.
- `TooltipContent`: immutable. Holds these sections; empty sections are hidden:
  - Header: `IconKey`, `Title`, `Subtitle` (class/role), `Shortcut` (display string).
  - `Description` (short gameplay text, not lore).
  - `Stats`: list of `TooltipStat(label, current, max?, format)`.
  - `Matchups`: `StrongAgainst` / `WeakAgainst` lists of `TooltipIconKey + label`.
  - `Requirements`: list of `TooltipRequirement(text, isMet)`. Covers disabled reasons.
  - `Status` line (e.g. "3 of 5 ships ready", "Cooling down 12s").
- `TooltipAnchor`: pure struct `{ Kind: UiRect | Cursor | HudFixed, float X, Y, Width, Height }` in screen space.
- `TooltipHandle`: readonly struct with an incrementing id.
- `TooltipSettings : Data` (ScriptableObject in `Assets/Settings/`): `showDelay = 0.35 s`, `refreshInterval = 0.1 s`, `cursorOffset`, `screenEdgePadding`.

### Presenter (`Entities/Tooltip/`)

- `TooltipUiController`. Injects `IUiService` and `ITooltipModelObserver`. Does **not** inherit `UiController`, since the tooltip needs no cancel focus.
  - Lazily calls `IUiService.CreateUi(UiType.Tooltip, PopupCanvasTransform)` and casts to `ITooltipUi`.
  - `Shown` → `Render(content)` + `Place(anchor)` + `Show()`. `ContentChanged` → `Render`. `Hidden` → `Hide()`.
- `WorldTooltipPresenter`. Subscribes to a world-hover source (entity under the cursor, via `IEntityLocator` / entity facades).
  - Skips the world tooltip while the pointer is over UI.
  - Builds providers from `IEntity` facades only (see AGENTS entity rules).

### View (MonoBehaviour)

- `TooltipUi : BaseUi, ITooltipUi` (top-level view). Handles `Render(TooltipContent)` and `Place(TooltipAnchor)`.
  - Placement: above the anchor rect. Flips below or sideways at screen edges. Cursor anchors use `cursorOffset`.
  - Resolves `IconKey` → `Sprite` through a serialized `TooltipIconData` (ScriptableObject).
- Sub-widgets inherit `MonoBehaviour`, not `BaseUi`: `TooltipHeaderView`, `TooltipStatRowView`, `TooltipMatchupRowView`, `TooltipRequirementRowView`.
- `TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler` (`Components/Ui/Tooltip/`).
  - Raises only C# events `HoverStarted(TooltipAnchor)` and `HoverEnded`. It has no service and no presenter reference.
  - The owning `<Feature>Ui` subscribes via serialized references and forwards to its presenter with its domain key (e.g. `ShipType`).
  - Pointer events still fire on non-interactable `Button`s, so disabled buttons still get tooltips.

## Decision

- **Chosen:** presenters own tooltip content (providers). The service owns timing, ownership, and refresh.
  - Why: presenters already hold model and data refs. The service stays generic and testable.
  - Avoid: a central tooltip "content registry" that knows every feature (god object).
- **Chosen:** refresh live values by polling `Build()` every `refreshInterval` while visible.
  - Why: one rule for every source. No per-feature change events needed.
  - Avoid: event-driven refresh per stat (lots of subscription code for little gain).
- **Chosen:** use icon keys in content and resolve sprites in the view.
  - Why: keeps `UnityEngine` out of the logic layer (MVP rule).
- **Chosen:** make tooltip graphics non-raycast instead of changing `BaseUi` visibility.
  - Why: `BaseUi.SetVisibility` forces `blocksRaycasts = true`. With no raycast targets inside, the prefab never blocks.
- **Chosen:** authored per unit in a new `UnitMatchupData`, as requested on 2026-09-30.
  - Why: unit roles and matchups are explicit balance data; `DamageMatrixData` accuracy is not the tooltip's source of truth.

## Implementation

### Phase 0: Prep
- [x] Read `PROJECT_ORGANIZATION`, `UI_CODE_BUILD_GUIDE`, and `UI_UX_GUIDELINES`. Confirm the folders `Services/Tooltip/`, `Entities/Tooltip/`, and `Components/Ui/Tooltip/`.
- [x] Find the existing world-hover or pointer raycast source in `Services/Input`, `Services/Selection`, and `PointerInput`. Record its API here.

### Phase 1: Tooltip service (logic)
- [x] Add `ITooltipService`, `TooltipService`, `TooltipModel`, `ITooltipModelObserver`, `ITooltipContentProvider`, `TooltipContent` (+ row types), `TooltipAnchor`, `TooltipHandle`, and `TooltipSettings`. One type per file.
- [x] Delay: the delay only counts down while the same `Key` is requested. A different key restarts it. Showing happens after `showDelay`.
- [x] Refresh: while visible, `IsValid == false` → hide. Otherwise `Build()` → `ContentChanged` when the content differs.
- [x] Write EditMode tests for delay, key reuse, stale-handle hide, replacement, invalid-provider hide, and `HideAll`. Run them only on request.

### Phase 2: Tooltip UI
- [x] Add `UiType.Tooltip = 15`.
- [x] Create the prefab `Assets/Prefabs/Ui/Tooltip/TooltipUi.prefab` with MPUIKit panel styling. Every graphic has `raycastTarget = false`. Include `CanvasGroup`.
- [x] Register the Addressable key `TooltipUi` in the `Ui` group.
- [x] Add `TooltipUi`, the sub-widgets, `TooltipIconData`, and `TooltipUiController`.
- [x] Bind `TooltipSettings`, `TooltipModel`, `TooltipService`, and `TooltipUiController` in `SkirmishMainInstaller`. Add `MainMenuInstaller` only when a menu target is added.
- [x] Write EditMode tests: the prefab resolves to `ITooltipUi`, serialized references are assigned, and no graphic has `raycastTarget` set.

### Phase 3: First integration (station roster card)
- [x] Add `TooltipTrigger` to the `FactionUnitUi` card. `FactionUi.TooltipHover` forwards C# hover events → `FactionUiController.HandleTooltipHover` → `TooltipRequests` → `ITooltipService`.
- [x] `FactionUiController` builds a provider from `FactionData`: `Name`, `Price`, `BuildTime`, `UnitCapacity`, `AvailableLevel`. The Requirements section shows "Requires Station Level N", "Missing X credits", and "Population limit reached".
- [ ] Verify the tooltip shows on disabled cards and that the delay and replacement behave correctly.

### Phase 4: Commands, abilities, superweapon
- [x] `UnitActionsPresenter`: command buttons show name, shortcut, behaviour, target, and cancel input. The shortcut comes from `IInputBindings.GetBindingDisplayString(slot)`, so a rebind updates on the next refresh.
- [x] Ability buttons show effect, range, duration, cooldown, and readiness. With several ships selected, show "N of M ready". Toggles show On/Off.
- [x] `SuperWeaponPresenter` shows the actual construction / ready / unavailable states, firing delay, damage, range, area and stun values. Current targeting rejects fighters; no separate recharge timer exists.

### Phase 5: HUD panels
- [x] Ship portrait / `ShipUiController` shows the ship info plus its current order. Grouped icons show type, count, and damaged count.
- [x] `EconomyUiController` shows credits, income, and population (used/max).
- [x] `ReinforcementUiController` shows reinforcement cards. `ShipBuildUiController` shows production queue items (progress, remaining time, refund).

### Phase 6: World hover
- [x] Add `WorldTooltipPresenter` for ships, squadrons (whole squadron), hardpoints (what is lost when destroyed), and capture sites.
- [x] Enemy content respects visibility / fog-of-war rules. A provider becomes invalid when the target is no longer visible.
- [x] Anchor = cursor offset. Suppress the world tooltip while the pointer is over UI or during drag-marquee.

### Phase 7: Matchups
- [x] Resolve the open matchup-data decision and author the Strong/Weak Against rows. Use icon rows, not text only.

### Phase 8: Remaining targets
- [x] Minimap controls and markers, hazards, objectives, and pause/settings/camera buttons. Add each only if the feature exists in the game.

## Edge Cases

- Target destroyed while shown → `IsValid` false → hide on the next refresh.
- Pointer exits the old target after entering a new one → the stale `Hide(oldHandle)` is ignored.
- Rebinding during a visible tooltip → the shortcut updates on the next refresh.
- HUD hidden (`SetHudVisible(false)`), cinematic, or route change → the owning presenter calls `Hide` / `HideAll`.
- Pause menu on the popup canvas → the tooltip also renders on the popup canvas, so it stays above the menu.
- Hovering over a UI panel that covers a ship → only the UI tooltip shows; the world tooltip is suppressed.
- Feature presenter disposed while its tooltip is shown → it calls `Hide(handle)` in `LateDispose`.

## Files

- `Assets/Scripts/Services/Tooltip/` holds the service, model, provider interface, content, anchor, handle, and plain timing.
- `Assets/Scripts/Entities/Tooltip/` holds `TooltipUiController`, `ITooltipUi`, `TooltipUi`, the sub-widgets, `TooltipSettings`, `TooltipIconData`, unit matchup data, content helpers, and `WorldTooltipPresenter`.
- `Assets/Scripts/Components/Ui/Tooltip/TooltipTrigger.cs`.
- `Assets/Scripts/Components/Ui/Base/UiType.cs` gets the new `Tooltip` entry. `UiController.cs` is **unchanged**.
- `Assets/Prefabs/Ui/Tooltip/TooltipUi.prefab`, `Assets/Settings/Data/Tooltip/TooltipSettings.asset`, `Assets/Settings/Data/Tooltip/TooltipIconData.asset`, and `Assets/Settings/Data/Tooltip/Matchups/`.
- `Assets/Scripts/Services/SceneContext/Skirmish/SkirmishMainInstaller.cs` gets the new bindings.

## TODO

- [ ] In-game acceptance: hover disabled station cards, switch targets rapidly, close routes, hide HUD, and exit the scene.
- [ ] In-game acceptance: live health / production / abilities / rebinding, fog loss, marquee suppression, minimap markers, and screen-edge placement.
- [ ] Run the authored EditMode tests only when explicitly requested.

## Implementation (2026-09-30)
- Pure service / model: `0.35 s` delay, `0.1 s` live refresh, source + key reuse, stale-handle protection, invalid-target hide, unscaled clock.
- Views publish hover events through `TooltipHoverView`; presenters own providers. `UiController.cs` remains unchanged.
- UI: popup canvas, MPUIKit panel, reusable rows, icon-key catalog, screen-edge flip / clamp, all graphics non-raycast.
- Coverage: roster / upgrades / research, commands / abilities / superweapons, selection / economy / reserves / queues, world units / hardpoints / capture sites / obstacles, minimap / menu / setup / settings / battle result.
- Matchups: `13` per-unit assets for current faction roster definitions. Roles and initial matchups use current prefab weapons; these are editable tactical guidance.
- World input: `ISelectionQuery.TryFindAt(Vector2, out SelectionEntry)`, `IPointerInput.Position`, `IUiHitTest.IsOverUi(Vector2)`, `IPointerGestures.DragStarted/DragEnded`.
- Superweapons: `Charging` currently means construction. After consumption the charge is unavailable until rebuilt. Fighter eligibility follows `ISuperWeaponFireService.CanTarget`.
- Population is a deployment gate, not a production gate. Reserve quantity comes from `ReinforcementModel`; successful deployment consumes it.

### Verification
- Unity compilation completed without C# errors; no automated tests executed.
- Saved asset inspection: `26` hover owners, `98` triggers, no missing tooltip references; all `13` matchup assets resolve their icon keys.
- Addressables verified: `TooltipUi` → existing `Ui` group; `TooltipSettings` and `TooltipIconData` → existing `Data` group; type-key mappings saved.
- Sample Venator tooltip rendered in an isolated preview scene and visually inspected; no scene or Play Mode changes made for verification.
- `TooltipServiceTests.cs`: delay, reuse, ownership, replacement, stale hide, invalid provider, live refresh, and `HideAll`.
- `TooltipPrefabTests.cs`: interface / CanvasGroup, assigned widget references, and non-raycast graphics.
- `TooltipTriggerTests.cs`: same-key hover reuse, hovered-row recycling, and disable cleanup. All three test files are authored and unrun.
- Plan remains `in-progress` until the in-game acceptance checks above are recorded.
