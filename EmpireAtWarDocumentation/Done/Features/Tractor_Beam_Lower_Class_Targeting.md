---
category: Features
status: done
created: 2026-11-19
completed: 2026-11-19
acceptance: pending-user
---
# Tractor Beam Lower-Class Targeting

## Outcome (2026-11-19)
- Tractor Beam targets every enemy ship of a lower class than the caster; same class, strikecraft and `Structure` immune.
- `TractorBeamSettings.targetClasses` removed; rule derived from `ShipClass.HullTier()`.
- Verification: Unity recompile `0` errors; Console clean after asset save; `targetClasses` absent from the catalog; beam profile unchanged in the diff.
- Tests run 2026-11-19 on user request (EditMode, explicit off): `Abilities` 223/223 passed, `Core` 169/169 passed.
- Open gates → [[TODOs/User_Acceptance_Queue]].
- Reusable guidance → [[GameDesign/Abilities_And_Superweapons]] (Tractor Beam row: target rule, range `500`).

## Goal
- Tractor Beam targets every enemy ship of a lower class than the caster.
- Example: ISD II (`Capital`) → `HeavyCruiser`, `Cruiser`, `Frigate`, `Corvette`.
- Tooltips and tests describe the new rule.

## Rule
- Class order: `Corvette` < `Frigate` < `Cruiser` < `HeavyCruiser` < `Capital` < `BattleCruiser` < `Dreadnought`.
- Target allowed → target tier < caster tier. Same class → not allowed.
- Never targetable (unchanged): `Fighter`, `Bomber`, `Interceptor`, `HeroStrikecraft`, `Structure`.

| Caster class | Casters (ShipData, verified 2026-11-19) | Targets |
|---|---|---|
| `HeavyCruiser` | Victory I, Victory II, Liberator | Corvette, Frigate, Cruiser |
| `Capital` | ISD I, ISD II, Tector, Endurance, Imperator | + HeavyCruiser |
| `BattleCruiser` | Titan, Allegiance | + Capital |
| `Dreadnought` | Executor | + BattleCruiser |

- `ImperatorShipData.asset` still lists ability `22`, although its roster text says "Laser Beam replaces Tractor Beam" → not changed here; data/text mismatch for the user to decide.
- Mandator Tractor Beam still pending in [[TODOs/Features/Mandator_Import]].

## Decision
- Chosen: derive targets from caster class; delete the serialized `targetClasses` list.
- Why: one rule, no per-ship data, new ships work without setup.
- Avoid: per-caster class lists in the catalog; comparing raw enum values (`Cruiser = 8`, `Capital = 4`).

## Confirmed by user (2026-11-19)
- `Fighter`, `Bomber`, `Interceptor`, `HeroStrikecraft` and `Structure` stay immune.
- Same class not targetable (ISD II cannot beam an ISD I).
- Speed stays `×0.25` for all targets; duration `20 s`, recovery `25 s`; range `500` unchanged.
- Watch after playtest: a battlecruiser can pin a capital for 20 of every 45 s.

## Implementation
1. [x] Casters confirmed from ShipData (table above).
2. [x] `ShipClassExtensions.HullTier()` → `Corvette 1 … Dreadnought 7`, `0` for strikecraft and `Structure`.
3. [x] `TractorBeamSettings`: `targetClasses` and `CanTarget(ShipClass)` removed; `CanTarget(ShipClass caster, ShipClass target)` added; facade overload reads `caster.Entity.HealthModel.ShipClass`, keeps the `ICombatModifiersFacade` check.
4. [x] `BalanceAbilityAdapter`: `"targetClasses"` entry and its `ShipClass` branch removed; unused `using` dropped.
5. [x] `ShipAbilityCatalog.asset` reserialized; `targetClasses` gone.
6. [x] AI check, no change needed:
   - `EnemyShipAbilityController.BestTarget` → prefers ships, then lowest hull %.
   - `AutoPowerCombatSnapshot.FindTarget` → ordered attack target, else nearest eligible non-strikecraft.
   - Auto power: `Weapons` outranks `Target` → Tractor Beam waits while a power ability is active.
7. [x] Tooltips: catalog key `22`; `EmpireFaction.asset` Victory I, ISD I, Victory II, Tector; `RepublicFaction.asset` Tector → "Tractor Beam slows enemy ships of a lower class."
8. [x] Tests (below).
9. [x] Docs: `GameDesign/Abilities_And_Superweapons.md` Tractor Beam row (targets, range `225` → `500`).

## Tests (`Abilities` 223/223, `Core` 169/169 passed 2026-11-19)
- `TractorBeamSettingsTests` (`Abilities`): `CanTarget_DefaultsToCorvettesAndFrigates` replaced by
  - `CanTarget_OnlyShipsOfALowerClassThanTheCaster` (7 × 7 matrix; `Corvette` caster → no targets).
  - `CanTarget_StrikecraftAndStructuresAreImmuneAndNeverCast`.
- `ShipAbilityServiceTests` (`Abilities`): `TractorBeam_TargetsOnlyLowerClassShips` (facade level).
- New `ShipClassExtensionsTests` (`Core`): ascending tiers, untiered classes, every `ShipClass` classified; added to `Tools/Jenkins/TEST_INVENTORY.md`.
- Reviewed, unchanged: `TractorBeam_EndsOnlyOutsideTheActivationRange`, `ShipAbilitySubsystemAvailabilityTests`, `ShipAbilityPerformanceTests` (no target path), `RealEntityCompositionTests.PlayerAutoPower_MainBatteriesUsesNormalLifecycle` (Allegiance can now beam the `Capital` Providence, but batteries activate first → tractor slot still `Ready`), `ShipPursuitTests`, ISD II / Allegiance / Executor fixtures, `BattleSaveServiceTests`.

## Edge Cases
- Target of equal class → button refuses the target; same feedback as today's invalid target.
- Active beam on a large target → existing end conditions apply (range, cloak, hardpoint destroyed, abilities suppressed).
- Old `targetClasses` data → dropped on reserialize; no save-file impact (`BattleSaveValidator` only handles slot ids).
- New `ShipClass` member without a tier → `ShipClassExtensionsTests` fails.

## Files
- `Assets/Scripts/Components/Health/Data/ShipClassExtensions.cs`
- `Assets/Scripts/Services/ShipAbilities/Abilities/TractorBeam/TractorBeamSettings.cs`
- `Assets/Scripts/Editor/Balance/BalanceAbilityAdapter.cs`
- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`
- `Assets/Settings/Data/Factions/Empire/EmpireFaction.asset`, `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset`
- `Assets/Scripts/Tests/Editor/Abilities/TractorBeamSettingsTests.cs`, `ShipAbilityServiceTests.cs`
- `Assets/Scripts/Tests/Editor/Core/ShipClassExtensionsTests.cs`
- `EmpireAtWarDocumentation/GameDesign/Abilities_And_Superweapons.md`

## User acceptance
- In battle: ISD II beams a heavy cruiser; refuses another capital and a fighter squadron; tooltip text reads correctly.
