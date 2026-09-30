---
category: Features
status: todo
created: 2026-09-30
tags:
  - ui
  - tooltip
  - research
---
# Tooltip System - Research

- Plan: [[TODOs/Features/Tooltip_System_Plan|Tooltip System Plan]]
- Source: coverage spec provided by the user on 2026-09-30, modelled on *Empire at War* contextual tooltips (unit description + Strong/Weak Against icons).

## Shared Behaviour

- Layout: dark panel with a thin border. Top row: icon + title + current shortcut. Then short description → stats → requirements/status. Irrelevant sections are hidden.
- Position: above the hovered element. Flip below or sideways near screen edges. World objects use a cursor-offset or fixed HUD area.
- Timing: `0.35 s` hover delay. Hide on pointer exit, panel close, or target gone. Tiny pointer moves do not restart the delay.
- Input: one tooltip at a time. It never intercepts clicks, selection, or orders. UI hover beats the world behind it.
- State: live prices, health, and cooldowns while shown. **Disabled buttons still show tooltips** with the reason.
- Readability: gameplay text, not lore. Colours are paired with labels/icons. Show the **rebound** shortcut, not the default.

## Ships and Combat Objects

| Target | Content |
|---|---|
| Ship (world) | Name, class, owner, role, hull/shields, key status, strong/weak against |
| Squadron world icon | Type, role, surviving/max fighters, carrier origin; describes the whole squadron |
| Ship portrait (selection) | Ship info + current order (attack, guard, move, repair) |
| Grouped unit icon | Type, selected count, damaged count; no single-ship health presented as the group's |
| Hardpoint | Name/type, health, operational state, what is lost (e.g. "Engines: destruction prevents movement") |
| Hull/shield bar | Exact current/max, regeneration, modifiers; ship shields kept distinct from component health |
| Buff/debuff | Effect, bonuses/penalties, source, remaining duration, stacking |

- Enemy info is limited to what visibility/intelligence rules allow.
- Matchups use two labelled icon rows: **Strong Against** and **Weak Against**. They are favourable/unfavourable, not guaranteed wins, and are authored from real balance.
  - Example (anti-fighter corvette): Strong → fighter squadrons. Weak → anti-corvette frigates, heavy capital weapons. Role → escorts larger ships, clears fighters.

## Station, Production, Economy

| Target | Content |
|---|---|
| Roster ship card | Name, class, role, matchups, cost, population, build time, required station/tech level (construction stats, not health) |
| Reinforcement card | Description, available quantity, population, deployment instructions, arrival delay, placement limits |
| Production queue item | Item, progress, remaining time, queue position, cancel input, refund |
| Station upgrade | Current → next level, cost, time, durability/weapons gained, unlocks |
| Research | Benefit, affected units, current → next value, cost/time, prerequisites, max level, status |
| Mining facility / capture point | Owner, capture/build state, income, level, health, next interaction |
| Credits / income / population | Total, rate and sources; used/max population, reservations, how to raise the limit |
| Faction/player badge | Faction, player, team, relationship |

- Unavailable examples: "Requires Station Level 3", "Missing 400 credits", "Population limit reached", "Already researching".

## Commands, Abilities, Superweapons

| Target | Content |
|---|---|
| Command button | Name, shortcut, behaviour, valid target, finish/cancel targeting. Move, Attack, Attack-Move, Stop, Guard, and Waypoints each get their own entry |
| Ability button | Effect + trade-off, target, range, duration, cooldown, cost, readiness |
| Toggle ability | On/Off state, sustained effect, upkeep/penalty, what a click changes |
| Shared ability (multi-select) | "3 of 5 ships ready"; whether it fires all eligible ships or one |
| Superweapon build/research | Unlock, cost, prerequisites, build time, limits |
| Superweapon fire | Damage/effect, shield interaction, valid targets (incl. fighters?), area/range, fire delay, charges/cost, cooldown, targeting |
| Superweapon charging/cooldown | Phase, remaining time, why it cannot fire; separate from the construction timer |

- Guard example: "**Guard — [binding]** Follow and protect a friendly unit. Engage nearby threats, then return. **Target:** friendly unit."

## Remaining Targets

| Target | Content |
|---|---|
| Minimap controls/markers | Function + shortcut; marker identity/owner/status without revealing hidden enemies |
| Asteroid field / nebula / hazard | Movement, targeting, visibility, and damage effects; affected classes |
| Objective / warning / notification | Requirement, progress, urgency, click action (focus camera) |
| Tabs / filters / camera / cinematic / pause / settings | One sentence: function, current state, shortcut |
| Control-group button | Group number, member count, select/focus/reassign inputs |

## Codebase Findings (2026-09-30)

- `UiController` (`Components/Ui/Base/UiController.cs`) injects `IUiService` + `IUiCancelRouter` only. Keep it that way.
- `IUiService` has `CreateUi(UiType, Transform)`, `PopupCanvasTransform`, and `SetHudVisible`. `UiType` goes up to `Fps = 14`.
- `BaseUi.SetVisibility` forces `CanvasGroup.blocksRaycasts = true`, so the tooltip prefab must have no raycast targets.
- `UiInstaller` loads the prefab `<UiType>Ui` via `IAssetService` and binds `BaseUi` + interfaces in a sub-container.
- `FactionData` fields: `Name`, `MaxCount`, `AvailableLevel`, `Price`, `BuildTime`, `UnitCapacity`, `Icon` (Sprite). It has no description or role field yet, so Phase 3 may need them.
- `IInputBindings.GetBindingDisplayString(BindingSlot)` and the `BindingsChanged` event cover shortcut display.
- `DamageMatrixData` gives accuracy per `(DamageType, ShipClass)`. This is a candidate source for matchups.
- No `EventSystem.IsPointerOverGameObject` usage was found, so the UI-over-world priority check is new work (Phase 6).
- Only `MiniMapUi` currently uses `IPointerEnterHandler`.
