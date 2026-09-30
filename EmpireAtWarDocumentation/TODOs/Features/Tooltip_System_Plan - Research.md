---
category: Features
status: in-progress
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

## Implementation (2026-09-30)

- `UiType.Tooltip = 15`; `UiInstaller` loads `TooltipUi` through the existing type-name mapping.
- `TooltipService` → `TooltipModel` events → `TooltipUiController` → `ITooltipUi`. The base `UiController` has no tooltip dependency.
- `TooltipSettings` defaults: delay `0.35 s`, refresh `0.1 s`, cursor offset `(18, 24) px`, edge padding `12 px`; time uses `Time.unscaledDeltaTime`.
- Views publish events through `TooltipTrigger` / `TooltipHoverView`. Presenters subscribe with `TooltipHoverSubscription` and own their `TooltipRequests`.
- Same source + key preserves the delay; source or key replacement restarts it. Repeated production-row renders preserve hover; recycling a hovered row publishes its new key.
- UI: `Assets/Prefabs/Ui/Tooltip/TooltipUi.prefab`; settings/icons/matchups: `Assets/Settings/Data/Tooltip/`. Main Menu and skirmish installers bind the service independently.
- `FactionData` now has `Description`, `Role`, `IconKey`, and a per-unit `UnitMatchupData` reference. `ShipAbilityDefinition` now has `Description` and `IconKey`.
- Matchups use authored asset rows. Initial roles/rows use current weapon loadouts; the heavy dreadnought remains the existing `Frigate` class with heavy turbolasers and ion cannons.

### Live sources
- World picking: `ISelectionQuery.TryFindAt(Vector2, out SelectionEntry)`; UI priority uses the existing `IUiHitTest.IsOverUi(Vector2)`.
- Marquee suppression: `IPointerGestures.DragStarted/DragEnded`. World cursor: `IPointerInput.Position`. Enemy validity checks the existing fog system.
- Hull/shields: `IHealthTooltipObserver` adds capacities and regeneration to the existing health observer; lost shield generators suppress regeneration.
- Orders: `IUnitOrderObserverFacade` exposes ship/squadron order state through entity facades.
- Reserves: `ReinforcementModel.GetReserveCount`; successful deployment consumes one reserve. Population is charged on deployment.
- Research: next-tier `ResearchEffect` multipliers and affected classes. Queues supply counts and remaining build time; cancellation refunds the item's recorded cost.
- Superweapon `Charging` is production construction; consumption requires another build. Fighters are rejected by current `CanTarget`; there is no separate recharge timer or ability resource cost.
- Obstacles expose movement obstruction; no nebula damage/visibility effects, control-group UI, or in-battle objective/notification widgets were found. Battle-result objective text has a tooltip.

### Verification
- Compilation clean; automated tests authored and unrun per repository policy.
- Saved prefab inspection: `26` hover owners and `98` triggers; no missing tooltip references or raycast targets in the tooltip panel/row prefabs.
- `13` matchup assets resolve their sprite keys. `TooltipUi` is in the existing Addressables `Ui` group; settings/icons are in `Data`.
- A populated Venator tooltip was rendered and visually inspected in an isolated preview scene.
- Manual in-game acceptance remains in the active plan: disabled cards, live values/rebinding, fog loss, marquee/HUD/route lifecycle, placement edges, and scene teardown.
- Catalog fields without a live source remain absent: carrier origin, population reservations, and a separate operational superweapon recharge timer.
