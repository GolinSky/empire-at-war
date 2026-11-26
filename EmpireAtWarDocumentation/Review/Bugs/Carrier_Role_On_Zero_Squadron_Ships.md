---
category: Bugs
status: review
acceptance: pending-user
created: 2026-11-26
follows: "[[Review/Features/Squadron_Bay_Rotation_Plan]]"
---
# Carrier Role On Zero-Squadron Ships

## Goal
- Ships that launch no squadrons (`HangarMaxActive` 0) do not show or count as carriers.

## Evidence (source, 2026-11-26)
- 12 `ShipData` assets have `HangarMaxActive` 0 and still carry `UnitRole.Carrier` (`1 << 6`): Executor, Corellian Battlecruiser, Dauntless, MC75 Profundity, Nebula, Imperator, Mandator, Procurator, Rothana, Bulwark, Bulwark II, Recusant Dreadnought.
- `Editor/Ship/ShipRoleRules.cs:106` `IsCarrier` → `HangarBays.Count >= MIN_CARRIER_BAYS`; ignores `HangarMaxActive`.
- `Entities/Faction/Ui/FactionUiController.cs:163-164` `IsCarrier` → `HangarBays.Count > 0`.
- Role text still says carrier: Rothana (`RepublicFaction.asset:102`), Bulwark I (`SeparatistFaction.asset:182`), Bulwark Mk II (`SeparatistFaction.asset:246`).

## Implementation
1. [ ] `ShipRoleRules.IsCarrier` and `FactionUiController.IsCarrier`: also require `HangarMaxActive != 0`.
2. [ ] Re-run `Propose Ship Roles` (or clear the flag) for the 12 ships; update the three role texts.
3. [ ] Check other `UnitRole.Carrier` / `HangarBays.Count` readers (tooltip tags `2a932b5ec`, AI).
4. [ ] Run related tests (`Ships`, `Ui` fixtures touching roles/tooltips).

## Files
- `Assets/Scripts/Editor/Ship/ShipRoleRules.cs`
- `Assets/Scripts/Entities/Faction/Ui/FactionUiController.cs`
- `Assets/Settings/Data/Factions/*/…Faction.asset`, the 12 `*ShipData.asset`

## Progress 2026-11-26
- [x] `ShipRoleRules.IsCarrier` → `HangarBays.Count >= MIN_CARRIER_BAYS && HangarMaxActive != 0`; `FactionUiController.IsCarrier` → `HangarBays.Count > 0 && HangarMaxActive != 0` (tutorial "new ship" hint).
- [x] Carrier flag (`1 << 6`) cleared on the 12 ships; role texts: Rothana "Capital · Heavy gunship", Bulwark I / Bulwark Mk II "Capital / Battle line". Refresh + `ForceReserializeAssets` + `SaveAssets`; recompile clean, 0 console errors.
- [x] Other readers checked: Enemy AI uses the live hangar (`IHangarAirWingFacade.CanLaunch`, `EnemyTaskForceExecutor.cs:1195`) and trims hangars to the cap (`UnitCombatProfileCatalog.cs:74`) → no AI change. Tooltip subtitle and matchups read `ShipData.Roles` → fixed by the data change.
- Test: `ShipRoleValidationTests.Carrier_HasEnoughHangarBays` renamed `…AndLaunchesSquadrons`; message adds max active.
- Not in scope: `SquadronPool.cs:197` counts bays of 0-cap ships when sizing pools (possible over-allocation).
- [x] Related tests ran 2026-11-26: `ShipRoleValidationTests` 8/8, `FactionUnitAvailabilityTests` 20/20 passed.
- Review: code complete, no known bug; open user gate → tooltip of the 12 ships shows no "Carrier".
