---
category: Features
status: done
acceptance: pending-user
created: 2026-11-26
completed: 2026-11-26
tags:
  - ui
  - tooltip
  - ai
---
# Unified Unit Tooltip

## Goal

- One tooltip format for ships and squadrons; every number read from unit data, never typed into text.
- One matchup source shared by tooltips and enemy AI.

## Decision

- Stats: `UnitSheetCatalog` (`IUnitSheetCatalog.Get(UnitTypeId)`) loads `ShipData` / `SquadronData` → `UnitSheet` (class, roles, hull, shields, shield regen/s). Rows: `UnitStatRows`.
- Research bonus shown as `base +bonus` (`TooltipStat.Bonus`, colour `#6DB8E2`).
  - Menus (`UnitRosterTooltips`): local player's `GetMultiplier(HullCapacity|ShieldCapacity, class)`; ships only (squadron health ignores research).
  - World (`ShipTooltipFacade`, `SquadronTooltipFacade`): `current / base +(live max − base)`.
- Roles: `ShipRole` → `UnitRole` (`Components/Health/Data`, namespace `EmpireAtWar.Components.Ship.Health`); `SquadronData.roles` added. Role correctness is data.
- Matchups: `Assets/Settings/Data/Units/MatchupRules.asset` (address `MatchupRules`, group `Model`) replaces `ShipClassMatchupData` + 91 `UnitMatchupData`.
  - Rule = attacker side beats target side; side = classes + roles. Weak list = inverse of strong list → never written twice.
  - Groups (label, members, one icon per faction, optional neutral icon) name rows; viewer's own faction icon hidden.
  - Class rule inside the unit's own group is hidden ("cruisers beat cruisers").
  - AI (`IShipClassMatchups.GetPreference`) reads class sides only; role sides are tooltip-only until `ForceComposition` tracks roles.
- Tooltip: title = name; subtitle = class · role tags; description = `FactionData.Role`; long `description` and weapons not shown for units.
- Avoid: digit-scanning tests; stats come from data, so drift has no source.

## Implementation

- [x] `UnitRole` rename + `SquadronData.roles`
- [x] `MatchupRules`, `MatchupRule`, `MatchupSide`, `MatchupGroup`, `MatchupIcon` (`Entities/Units/Matchups`), `UnitMatchupResolver`
- [x] `UnitSheet`, `UnitSheetCatalog`, `UnitStatRows`, `UnitRosterTooltips` (registered in `SkirmishScope`)
- [x] Rewired `ShipTooltipFacade`, `SquadronTooltipFacade`, `FactionUiController`, `ShipBuildUiController`, `ReinforcementUiController`, `ShipUiController`; structure regen row moved to `HealthTooltipStats`
- [x] Asset created; old AI asset, 91 tooltip matchup assets, 4 old classes and `FactionData.matchups` removed
- [x] Tests reviewed/updated (see Verification)

## Edge Cases

- Old AI table was asymmetric (Frigate weak vs BattleCruiser/Dreadnought, Cruiser weak vs Dreadnought had no strong rule). Migration added BattleCruiser → Frigate, Dreadnought → Frigate/Cruiser → AI values those slightly more vs frigate/cruiser forces.
- New rules beyond the old table: HeroStrikecraft → Fighter/Interceptor/Bomber; Artillery → capitals/structures; AntiStrikecraft → strikecraft; AntiCapital → capitals.
- All `SquadronData.roles` start `None` → squadron tooltips show class only until roles are set.
- 2026-11-26: cleared every ship/squadron `FactionData.description` (108: Empire 30, Rebellion 26, Republic 27, Separatist 25); research, level and structure descriptions kept. Diff checked: only `description` and the removed `matchups` field changed.
- 2026-11-26: Editor held stale in-memory faction assets; `ForceReserializeAssets` wrote old values. Restored HEAD content minus `matchups` lines and force-reimported. Avoid reserializing data assets without diffing first.

## Verification

- `unity recompile`: 0 errors, 0 warnings. Console: no errors since session cursor.
- Asset script: every group icon key resolves in `TooltipIconData`; address `MatchupRules` in `Model`.
- Tests updated: `TooltipPrefabTests.MatchupIconsFitRowSlotsAndResolve` (now reads `MatchupRules`), constructors in `ShipAbilityServiceTests`, `SuperWeaponTests`, `FactionUnitAvailabilityTests`. New: `UnitMatchupResolverTests` (`Ui`), `MatchupRulesPreferenceTests` (`EnemyAi`). Reviewed unchanged: `DefenseSatelliteContractTests` (stub regen 0 → no new row), `TooltipServiceTests`, `CombatMatchupTests`, `ShipRoleValidationTests` (rename only).
- 2026-11-26 EditMode run (user-requested, explicit off): `Combat` 350, `Squadrons` 236, `Balance` 74, `Economy` 145, `Abilities` 261, `Core` 187 all passed. `Ui` 415/417, `EnemyAi` 741/742, `Ships` 488/489 after fixes.
  - Fixed (mine): `AllegianceShipTests.Registrations_ResolveOwnAssets`, `EmpireHeroCapitalTests`, `SeparatistHeroCapitalTests` read the removed `matchups` field by string → now assert via `UnitMatchupResolver` / non-empty `role`.
  - Remaining, not from this change (concurrent uncommitted edits to `ArquitensImperialCruiserShipView.prefab`, `DamageMatrixData.asset`, `XystonShipData.asset`/ISD II hull): `TeamColorViewPrefabTests` ×2, `EnemyDefenseStrategyTests.KnownComposition_SelectsAppropriateDefense(Cruiser…IonSatellite)`, `XystonShipTests.Superlaser_…MoreFragileThanTheLineShips`.
- Reusable guidance: none extracted; rules live in `MatchupRules` / `UnitSheetCatalog` doc comments and this note.
