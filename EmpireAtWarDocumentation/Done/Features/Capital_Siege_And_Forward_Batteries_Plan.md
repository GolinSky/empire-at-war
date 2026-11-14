---
type: plan
category: Features
status: done
created: 2026-11-14
system:
  - combat
  - ships
  - balance
verification: Source and serialized weapon bindings checked; Unity recompile 0
  errors, 20 prefabs refreshed, reserialized, saved and reloaded; console 0
  errors. Tests and controlled gameplay not run.
completed: 2026-11-14
acceptance: pending-user
---
# Capital Siege and Forward Batteries Plan

## Goal

- Super-heavy ship weapon damage against stations and defense platforms ×1.5; ordinary capitals ×1.0.
- Empire capitals and triangular Empire/Republic hulls can bring their main batteries onto a forward target.
- Combat hull facing prioritizes main-battery firing solutions instead of rotating to maximize every secondary gun.

## Decision

- Requirements approved and implemented 2026-11-14. Player acceptance is pending.
- Siege factor is a separate attacker-class multiplier: HeavyCapital (5) →1.5; Capital (4) →1.0. Use canonical runtime class, not ship name, physical size or weapon caliber.
- Apply to weapon damage against production stations and defense platforms, including shields and hull, exactly once. Capitals retain normal damage-type behavior; ×1.0 does not make ion hull damage equal turbolaser hull damage.
- Preserve existing weapon/caliber bias, research, target mitigation, shield piercing and hardpoint transfer. Do not replace shared Structure matrix values to implement an attacker-specific rule.
- Other attacker classes and targets remain unchanged. Resolve exact station/platform entity identities first; do not automatically extend the bonus to every Structure target (mines, capture sites or unrelated buildings).
- Scope is faction-neutral for the class multiplier. HeavyCapital (5) ships, verified 2026-11-14: Executor, Titan, Allegiance (Empire); Mandator (Republic); Lucrehulk, Malevolence (Separatist); Rebellion has none. Mediator (Rebellion roster, `MediatorBattlecruiserShipData`) is Capital → ×1.0.
- All Victory variants (Victory, Victory I, Victory II) are `Cruiser` (8) → no siege factor; they are in scope only for forward arcs/facing.
- Weapon damage includes normal mounted weapons; special ability/superweapon damage must be inventoried separately and must not accidentally acquire or double-apply the factor.

### Scope decisions (user-approved 2026-11-14)

- Target set: `StructureKind.SpaceStation` + `SecondStation` + every `DefendPlatform` type (`Xq6`, `BattleAsteroid`, `GolanIII`, `Empress`, all five satellites). Excluded: every `MiningFacility`, including `TibannaGasStation`. `ShipClass.Structure` (6) alone does not identify this set.
- Facing scope: main-battery-first facing applies to any ship with ≥1 usable flagged main battery, regardless of class. Includes non-capital Pelta and Centax (existing flags); verify their facing in step 4.

## Implementation

### Scope and assets

- [x] Revalidated rosters, shared views, prefab overrides, active mount IDs and parent transforms across 25 layouts and 618 bindings. Main-battery arcs and flags updated in 20 prefabs.
- [x] Preserved full-circle main arcs on Executor, Titan, Allegiance, Interdictor and Mandator. Kept Pelta and Centax flags and existing Power to Main Batteries ability assignments.
- [x] Verified Victory (Republic), Victory I and Victory II bindings, and the shared Resolute/Venator II view. ISD III remains non-roster and rejected by `ShipFactory`; its Composite Beam turret is outside the mounted weapon loadout.
- [x] Exported per-mount IDs and yaw, nominal and directional DPS, target-adjusted shield/hull ceilings and near/far reach. Retained summary and export locations in `Tools/BattleAudit/CapitalSiegeAudit.md`.

### Damage, facing and range

- [x] `IStructureKindFacade` identifies both station types and all defense platforms. Runtime and simulator use `SiegeDamage` before impact scheduling and tick splitting. Combat reservations use the same outgoing damage.
- [x] Matching AI target estimates boost only the HeavyCapital contribution. The nominal Balance DPS estimate remains independent of any target. The Battle Simulator can calculate siege for definitions with a structure kind; its asset picker currently lists ships and squadrons.
- [x] Source-verified direct damage through `HarmonicBombService`, `SustainedBeamAbility` and `SuperWeaponFireService`; these paths use their own ability damage rules.
- [x] `WeaponFacingSolver` compares actual muzzle and target hardpoint geometry in 2° samples. Nearest heading wins equal main-battery scores. Disabled, destroyed and target-ineligible mounts have zero weight. Eligible secondaries take over when all mains lack a solution, and restored mains regain priority on the next solve.
- [x] `ShipMoveComponent` retains its timed/bearing-based re-solve and tracks target identity. Travel, arrival and ion handling remain under the movement component.
- [x] For an eligible HeavyCapital whose own footprint exceeds the ordinary hold threshold, approach to live hull contact +1 unit and hold within +2 units. Navigation keeps its independent collision checks. Executor benefits on small stations; Separatist level 5 uses the larger live hull.
- [x] Retained projectile cadence from [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]]. Wasted shots and telemetry identity remain owned by [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]].

### Verification

- [x] Unity 6000.4.7f1 recompile completed with 0 errors. Refreshed, reserialized only the 20 updated prefabs, saved and reloaded all 20. Console contained 0 errors on final check.
- [x] Authored regression coverage for damage and reservations, simulator ticks, mixed AI forces, shared forward arcs, firing priority and recovery, and siege standoff. Tests were not run under the local execution override.
- [x] Extracted current rules to [[GameDesign/Combat#Capital siege and hull facing]] and geometry/cadence caveats to [[Rules/WEAPON_EFFECTIVE_DPS_LIMITATIONS]].
- [ ] Player acceptance → [[TODOs/User_Acceptance_Queue#Gameplay and Balance]]. Compare shield/hull DPS at fixed distance and inspect approach, forward firing and fallback in an uncheated battle.

## Important Values

| Baseline ship | All anti-ship raw DPS | Forward | Best common bearing |
|---|---:|---:|---:|
| ISD II |1514|262|789|
| Tector |1261|286|745|
| Imperial Venator |791|31|411|
| Victory I |511|138|270|
| Victory II |597|142|350|
| Executor |5091|5091|5091|

- 2026-11-14 audit; no research, matrix, travel, range, mount loss or abilities included. Forward/best are geometric ceilings.
- ISD II main octuple arcs: −135°…−45° /45°…135°; Tector heavy quad arcs: −140°…−40° /40°…140°.
- Full-circle coverage already exists on Executor, Titan, Allegiance and Interdictor; preserve it.
- Station/platform targets must be distinguished from generic Structure telemetry labels before matchup-specific damage is attributed. `WeaponComponent.GetTelemetryTarget` returns `"Structure"` for every target without `IUnitTypeFacade` (stations, platforms, mines).
- `ShipClass.Structure` (6) data: `SpaceStationData`; 9 `DefendPlatform` data assets (Xq6, Battle Asteroid, Golan III, Empress, Ion/MassDriver/Missile/QuadLaser/Turbolaser satellites); `MiningFacilityData`, `AsteroidMiningFacilityData`, `TibannaGasStationData`.

## Files

- [[Rules/WEAPON_EFFECTIVE_DPS_LIMITATIONS]]
- [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]]
- [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]
- [[GameDesign/Combat]]
- `Assets/Prefabs/Models/Ships/Empire/View/`; `Assets/Prefabs/Models/Ships/Republic/View/`.
- `Assets/Settings/Data/Ship/Empire/`; `Assets/Settings/Data/Ship/Republic/`; faction catalogs and asset mappings.
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`, `WeaponFacingSolver.cs`, `WeaponTargetSelector.cs`, `WeaponTargetSelectionJob.cs`.
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`; `Assets/Scripts/Components/CombatModifiers/CombatModifiers.cs`.
- `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs`; `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs`.
- `Assets/Scripts/Editor/Balance/BalanceDpsEstimator.cs`; `Assets/Scripts/Editor/BattleSimulator/BattleSimulation.cs`.
- `Assets/Scripts/Components/Health/Data/ShipClass.cs`; `Assets/Scripts/Services/Saves/StructureKind.cs`; `Assets/Scripts/Entities/DefendPlatform/DefendPlatformType.cs`.
- Installers for the kind facade: `SpaceStationInstaller.cs`, `DefendPlatformInstaller.cs`.
- `Assets/Scripts/Services/ShipAbilities/Abilities/PowerToMainBatteries/`.
- `Logs/BalanceAudit/2026-11-14/weapon_angles.json`, `weapon_angles_full.csv`, `audit_weapon_angles.py`.
