---
category: Refactoring
status: todo
created: 2026-11-26
follows: "[[Review/Bugs/Carrier_Role_On_Zero_Squadron_Ships]]"
---
# Derived Carrier Role

## Goal
- One carrier rule for AI, tooltip, matchups, tutorial and editor tools: the one the AI uses.
- Hangar edits update "Carrier" automatically; no stored flag to go stale.

## Current state (source, 2026-11-26)
- AI, live: `IHangarAirWingFacade.CanLaunch || LaunchedCount > 0` (`EnemyTaskForceExecutor.cs:1195`).
- AI, static: `UnitCombatProfileCatalog.CreateShip` → hangar squadrons from `HangarBays[].MaxActive`, trimmed to `HangarMaxActive` (`UnitCombatProfileCatalog.cs:61-80`).
- Tooltip subtitle + matchups: stored `ShipData.Roles` (`UnitTooltipContent.cs:36-41`, `UnitMatchupResolver.cs:51`), set by `Tools/AI/Propose Ship Roles`.
- Editor rule: `ShipRoleRules.IsCarrier` → `HangarBays.Count >= MIN_CARRIER_BAYS (2) && HangarMaxActive != 0`.
- Tutorial hint: `FactionUiController.IsCarrier` → `HangarBays.Count > 0 && HangarMaxActive != 0`.
- Mismatch: Interdictor, MC80 Freedom, Mediator (1 bay, cap 1) launch squadrons → AI carrier, tooltip not.

## Decision (proposed, not approved)
- Carrier = the ship's `UnitCombatProfile` has ≥ 1 hangar squadron.
- Why: same rule as the AI; derived from hangar data, cannot go stale.
- Avoid: moving the other roles (Dps, Fast, IonSpecialist…) — display labels only; AI rates by numbers (`CombatMatchup`).

## Implementation
1. [ ] Shared static rule built on the profile hangar count (or one `ShipData` helper the catalog also uses).
2. [ ] Tooltip subtitle, `UnitMatchupResolver`, `FactionUiController.IsCarrier`, `ShipRoleRules` use it.
3. [ ] Remove `UnitRole.Carrier` from stored `roles` on all ships; keep the enum value for matchup rules.
4. [ ] Update `ShipRoleValidationTests.Carrier_HasEnoughHangarBaysAndLaunchesSquadrons`; tooltip/matchup tests for derived carrier.
5. [ ] Run related tests (`Ships`, `Ui`, `Combat` fixtures touched).

## Edge Cases
- `HangarMaxActive` −1 = no cap → bays count.
- Stations use `StationHangarData` (`HangarMaxActive` −1) → not ship tooltips; check matchup rules for stations.
- Interdictor, MC80 Freedom, Mediator start showing "Carrier" → user confirms this is wanted.

## Open question
- Should 1-bay ships count as carriers? (Needed before step 1.)
