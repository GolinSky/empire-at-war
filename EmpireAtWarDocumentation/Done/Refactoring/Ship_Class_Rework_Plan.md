---
category: Refactoring
status: done
created: 2026-11-17
completed: 2026-11-17
acceptance: pending-user
---
# Ship Class Rework

## Goal
- New class ladder: `Corvette → Frigate → Cruiser → HeavyCruiser → Capital → BattleCruiser → Dreadnought`.
- Old `HeavyCapital` becomes `BattleCruiser`; add `HeavyCruiser` and `Dreadnought`.
- Reassign ships, rebuild damage matrix + shield regen tiers, resize Nebulon-B to 600 m.

## Current State (verified in source)
- `ShipClass` (`Components/Health/Data/ShipClass.cs`): Corvette=2, Frigate=3, Cruiser=8, Capital=4, HeavyCapital=5, Structure=6, strikecraft 0/1/7/9.
- `ShipClassValues` struct: one float per class; used by `DamageTypeProfile` (damage, accuracy) and `WeaponCaliberRule` (damageBias, accuracyBias).
- Matrix assets with `heavyCapital:` field: `DamageMatrixData.asset`, `WeaponsData.asset` (Assets/Settings/Data/Models/Weapon).
- `ShieldRegenTier` enum: Corvette, Frigate, Cruiser, Capital, HeavyCapital, Executor. `ShipData.ShieldRegenTier` picks the tier; `ShipShieldRegenTiersData` holds values.
- Regen assets (Assets/Settings/Data/Ship): `Shared` (Republic, Separatist), `Empire/EmpireShipShieldRegenTiersData`, `Rebellion/RebellionShipShieldRegenTiersData`, `Separatist/WavecrestShieldRegenTiersData` (Wavecrest only).
- Current regen values (corvette/frigate/cruiser/capital/heavyCapital/executor):
  - Shared: 5 / 25 / 90 / 180 / 400 / 1111.1111
  - Empire: 4.75 / 23.75 / 85.5 / 171 / 380 / 600
  - Rebellion: 5.5 / 27.5 / 99 / 198 / 440 / 1222.2222
  - Wavecrest: 5 / 30 / 90 / 180 / 400 / 1111.1111
- Class literal users (non-test): `ControlGroupIconPriority`, `FactionUnitUi`, `FactionUiController`, `UnitCombatProfile`, `ForceComposition`, `EnemyStrategicContextBuilder`, `ThreatNotifier`, `MapNotifier`, `AttackTargetState`, `ShipInstaller`, `AllyAiMarkerEmitter`, `WeaponComponent`, `SiegeDamage`, `CompositeBeamSettings`, `LuckyShotSettings`, Editor `BalanceCompareOrder`/`BalanceCompareSummary`/`BalanceUnitAdapter`.
- Ship ids come from `ShipType` enum (`Entities/Faction/Model/ShipType.cs`).

## Decisions
- Keep serialized enum ints: `HeavyCapital=5` renamed `BattleCruiser=5`; new `HeavyCruiser=10`, `Dreadnought=11`. Existing assets keep working.
- `ShipClassValues`: rename `heavyCapital` → `battleCruiser` with `[FormerlySerializedAs("heavyCapital")]`; add `heavyCruiser`, `dreadnought`. Update ctor, indexer, all callers.
- `ShieldRegenTier`: same ladder + `Dreadnought` replaces `Executor`. Keep ints (`HeavyCapital=4` → `BattleCruiser=4`, `Executor=5` → `Dreadnought=5`); add `HeavyCruiser=6`.
- Why: no mass reserialization risk; Avoid renumbering.
- Matrix (confirmed 2026-11-17):
  - `heavyCruiser` columns = interpolation between Cruiser and Capital (midpoint unless a weapon's values are non-monotonic).
  - `dreadnought` columns = old HeavyCapital values raised (amount to pick during balance pass; keep ordering Dreadnought ≥ BattleCruiser).
  - `battleCruiser` columns = old HeavyCapital values.
- Structure siege multiplier (`SiegeDamage.GetMultiplier`, currently `HEAVY_CAPITAL_MULTIPLIER = 1.5` for `HeavyCapital` vs station/second station/defend platform):
  - Dreadnought → 1.5 (`DREADNOUGHT_MULTIPLIER`).
  - BattleCruiser → 1.2 (`BATTLE_CRUISER_MULTIPLIER`).
  - Other classes → 1.

## Damage Model (verified in source 2026-11-17)
- Class affects damage only through the **target's** class. Attacker class changes nothing except the siege multiplier vs structures.
- Per shot (`WeaponComponent.GetHitChance` / `ApplyDamage`, `HealthModel.ApplyDamage`):
  1. Hit chance = `min(1, matrix.accuracy[type][targetClass] × caliber.accuracyBias[targetClass] × modifiers)`.
  2. Shot damage = `profile.Damage × caliber.damageBias[targetClass] × modifiers × siege` (bias only when the weapon family has `TargetSizeBias`).
  3. Shields (non-piercing types): `shieldDamage = damage × matrix.shipShieldMultiplier`. Overflow carries to the hull, converted back by that multiplier.
  4. Hull/hardpoint damage = `damage × matrix.damage[type][targetClass]`. Goes to the hit hardpoint, or live hardpoints if it is dead, or hull if there are none or it is a wreck.
  5. `PrecisionStrike` skips shields and hardpoints and hits the hull directly.
- Damage-matrix column = multiplier the target takes. Low = resists, high = vulnerable. Accuracy is 1.0 from Capital up, so only Cruiser and below can dodge.
- Matrix columns today (Cruiser / Capital / old HeavyCapital) and HeavyCruiser midpoint:

| Damage type | Dmg Cruiser | HeavyCruiser | Capital | Old HeavyCapital |
|---|---|---|---|---|
| Laser | 0.35 | 0.30 | 0.25 | 0.2 |
| PointDefense | 0.075 | 0.0625 | 0.05 | 0.05 |
| Turbolaser, MassDriver | 0.95 | 0.925 | 0.9 | 0.8 |
| Ion | 0.1 | 0.1 | 0.1 | 0.1 |
| ConcussionMissile | 0.45 | 0.375 | 0.3 | 0.2 |
| ProtonTorpedo | 1.125 | 1.1875 | 1.25 | 1.5 |
| Beam | 1.5 | 1.5 | 1.5 | 1.5 |
| HypervelocityGun | 1 | 1 | 1 | 1 |
| PlasmaCannon | 0.8 | 0.7 | 0.6 | 1 |
| FighterLaser | 0.05 | 0.04 | 0.03 | 0.02 |
| InterceptorLaser | 0.03 | 0.025 | 0.02 | 0.015 |
| HeroLaser | 0.15 | 0.125 | 0.1 | 0.07 |

- Accuracy midpoints (Cruiser / HeavyCruiser / Capital): Laser, Concussion 0.975 / 0.9875 / 1; Turbolaser, MassDriver, Ion 0.9 / 0.925 / 0.95 (Turbolaser row from asset; verify); ProtonTorpedo 0.95 / 0.975 / 1; FighterLaser 0.975 / 0.9875 / 1.
- Caliber biases (`WeaponsData.asset`) interpolate the same way: caliber 0 damage 0.95 / 0.925 / 0.9; caliber 2 damage 1.1 / 1.175 / 1.25, accuracy 0.95 / 0.975 / 1.
- Rule for the pass: every `ShipClassValues` field, in both matrix assets, gets `heavyCruiser = (cruiser + capital) / 2`. Rows with equal ends stay equal.
- Effect: a HeavyCruiser takes slightly more laser/turbolaser/missile damage than a Capital, slightly less than a Cruiser; torpedoes are mirrored.
- `plasmaCannon`/`HypervelocityGun` rows are non-monotonic; midpoint still applies, verify against intent.
- BattleCruiser column = old HeavyCapital.
- Dreadnought column (confirmed 2026-11-17: continue the Capital → BattleCruiser trend one more step; matrix value = damage taken):

| Damage type | Dreadnought dmg |
|---|---|
| Laser | 0.15 |
| Turbolaser, MassDriver | 0.7 |
| ConcussionMissile | 0.15 |
| ProtonTorpedo | 1.75 |
| PointDefense | 0.05 (no trend) |
| Ion, Beam, HypervelocityGun | same as BattleCruiser (flat rows) |
| PlasmaCannon | 1 (same as BattleCruiser; non-monotonic, confirm) |
| FighterLaser / InterceptorLaser / HeroLaser | 0.015 / 0.01 / 0.05 (small rows scaled ×0.75; proposal) |
| Accuracy (all types) | same as BattleCruiser (already 1) |
| Caliber damage bias | caliber 0 → 0.8; caliber 2 → 1.45 (trend step; proposal) |
- Siege code to change: `WeaponComponent.ApplyDamage` guards `_attackerClass == ShipClass.HeavyCapital` before calling `SiegeDamage.GetMultiplier`; `ForceComposition` calls `GetMultiplier(ShipClass.HeavyCapital, kind)`; `BattleSimulation` calls it too.

## Ship Reassignment
Unlisted ships keep their class (including Cruiser/Capital/Frigate ones not named below).

| Ship | Old class | New class |
|---|---|---|
| Acclamator (Empire `AcclamatorAssault`, Republic `Acclamator`) | Frigate | Cruiser |
| Carrack | Corvette | Frigate |
| Centax | Frigate | Cruiser |
| Nebulon-B | Frigate | Cruiser (+ resize) |
| Munificent, Captor | Frigate | Cruiser |
| Gladiator | Capital | HeavyCruiser |
| Victory, Victory I, Victory II | Cruiser | HeavyCruiser |
| Republic `HeavyDreadnought` | Cruiser | HeavyCruiser |
| Freedom (`MC80Freedom`) | Capital | HeavyCruiser |
| Profundity (`MC75Profundity`) | Cruiser | HeavyCruiser |
| Bothan Cruiser, Liberator, MC30c | Cruiser | HeavyCruiser |
| Corellian BC (`CorellianBattlecruiser`) | Cruiser | HeavyCruiser |
| Dispatcher, Recusant, Peleset | Cruiser | HeavyCruiser |
| DH-Omni | Capital | HeavyCruiser |
| Dauntless | Cruiser | Capital |
| Allegiance, Titan | HeavyCapital | BattleCruiser |
| Procurator, Secutor | HeavyCapital | BattleCruiser |
| Mediator, Home One (hero), Independence | HeavyCapital/Capital | BattleCruiser |
| Recusant Dreadnought, Invincible, Lucrehulk, Malevolence | HeavyCapital | BattleCruiser |
| Executor | HeavyCapital | Dreadnought |
| Praetor, Pride of the Core (`Mandator`) | HeavyCapital | Dreadnought |

- Home One / Mediator / Independence: Mediator and Independence currently Capital/HeavyCapital mix → set per table; verify each `ShipData`.
- Each ship also sets `ShieldRegenTier` = its new class tier (Executor/Praetor/Mandator → Dreadnought; Carrack → Frigate).

## Shield Regen Rules (per faction asset)
- Cruiser, Frigate, Corvette: unchanged.
- HeavyCruiser = faction Cruiser value × 1.3 (cruiser + 30 %), rounded. Confirmed 2026-11-17.
- Capital = old Capital × 1.2, rounded.
- BattleCruiser = old HeavyCapital value (unchanged).
- Dreadnought = old Executor value per faction (anchor).
- Resulting values (HeavyCruiser / Capital / BattleCruiser / Dreadnought):
  - Shared: 117 / 216 / 400 / 1111
  - Empire: 111 / 205 / 380 / 600
  - Rebellion: 129 / 238 / 440 / 1222
  - Wavecrest: 117 / 216 / 400 / 1111
- Praetor/Mandator jump 400 → 1111 regen; check with Balance tooling.

## Implementation Steps
1. Enums: `ShipClass`, `ShieldRegenTier`, `ShipClassValues`, `ShipShieldRegenTiersData` (+ `GetRegenerateValue`).
2. Update class switches/rank code: `ControlGroupIconPriority` (rank Dreadnought > BattleCruiser > Capital > HeavyCruiser > Cruiser …), `FactionUnitUi` colors, `FactionUiController`, AI (`UnitCombatProfile`, `ForceComposition`, counter weights), notifiers, `AttackTargetState`, `ShipInstaller`, `SiegeDamage` multipliers, `CompositeBeamSettings`, `LuckyShotSettings`, `WeaponComponent`.
3. Editor balance tools: `BalanceCompareOrder`, `BalanceCompareSummary`, `BalanceUnitAdapter`.
4. Damage matrix: `DamageMatrixData.asset`, `WeaponsData.asset` (+ any caliber rules): old `heavyCapital` → `battleCruiser`; fill `heavyCruiser` and `dreadnought` columns; set `dreadnought` from old heavyCapital/executor behavior, `heavyCruiser` interpolated between Cruiser and Capital. Use Unity API / SerializedObject, then reserialize.
5. Regen tier assets: 4 assets per rules above.
6. Reassign `ShipClass` + `ShieldRegenTier` on ~40 `*ShipData.asset` files per table (SerializedObject, then `SaveAssets`).
7. Per-class lookups elsewhere: Hardpoint health budget table (`Hardpoint_Health_From_Hull_Plan` class table), weapon taxonomy min-caliber (Capital/HeavyCapital ≥ Medium), production tier gating (Production_And_Research: tier III adds HeavyCapital), tooltips and matchup assets (`Assets/Settings/Data/Tooltip/Matchups`).
8. Vault docs: `GameDesign/Combat`, `Units_And_Factions`, `Production_And_Research`, weapon notes.

## Nebulon-B Resize (600 m)
- Files: `Assets/Prefabs/Models/Ships/Rebellion/Models/NebulonB.prefab`, `Assets/Art/Models/RebellionShips/NebulonB/NebulonB.fbx`, `NebulonBShipData.asset`, `NebulonBWreckData.asset`, `NebulonBMatchups.asset`, `NebulonBShipViewShield.asset`.
- Measure current length first (mesh bounds × prefab scale); scale factor = 600 / current.
- Scale uniformly via prefab root/visual scale; do not edit the FBX.
- Verify scale-dependent data: hardpoint positions and fire arcs, hardpoint health data, collider/navigation clearance, shield mesh, wreck prefab + fragments, engine/VFX scale, ship icon, selection/marker size, spawn blocking, hangar/bay positions.
- Update tooltip (class, size, regen); `ShipHeightTierTests` and `ForwardMainBatteryTests` reference Nebulon.
- Check ship-vs-ship ordering by size (Nebulon-B must not exceed heavier cruisers unreasonably).

## Test Review (not run — local override)
- Categories: `Combat`, `Health`, `Ships`, `Ui`, `EnemyAi`, `Balance`, `Abilities`, `Navigation` (Nebulon size), `Core`.
- Search tests for `HeavyCapital` (34 files): `ControlGroupPreviewTests`, `ShipPursuitTests`, `EnemyTaskForceSelectionModelTests`, `OverkillTargetingTests`, `BattleSimulationTests`, `BalanceLayoutTests`, `BalanceCompareOrderTests`, `WeaponLoadoutBakeTests`, `TractorBeamSettingsTests`, `ShipAbilityServiceTests`, `CinematicCameraModelTests`.
- No stat-pinning: assert wiring and ordering, not tuned numbers.

## Research Class Unlocks (confirmed 2026-11-17)
- Improved Weapons / Enhanced Shields / Enhanced Engines affected classes: HeavyCruiser joins with Capital; BattleCruiser takes the old HeavyCapital slot (tier III); Dreadnought also joins at tier III.
- Verify the research data assets and `GameDesign/Production_And_Research` (line ~70 "tier III adds HeavyCapital") before editing.

## Open Questions
- Plasma/Hypervelocity rows: `Dreadnought` retains `1` (matching `BattleCruiser` on those flat/non-monotonic rows) pending in-game balance acceptance.

## TODO
- [x] Confirm open questions.
- [x] Steps 1–8.
- [x] Nebulon-B resize + hardpoint/wreck/shield verification.
- [x] Test review pass.

## Outcome & Verification — 2026-11-17
- Implemented the 7-tier warship class ladder (`Corvette → Frigate → Cruiser → HeavyCruiser → Capital → BattleCruiser → Dreadnought`) while preserving serialized enum integer IDs (`BattleCruiser = 5`, `HeavyCruiser = 10`, `Dreadnought = 11`; `ShieldRegenTier.BattleCruiser = 4`, `Dreadnought = 5`, `HeavyCruiser = 6`) and `[FormerlySerializedAs]` fields in `ShipClassValues` and `ShipShieldRegenTiersData`.
- Updated `SiegeDamage` (`Dreadnought = 1.5x`, `BattleCruiser = 1.2x`), all non-test C# class switches (`WeaponComponent`, `ForceComposition`, `UnitCombatProfile`, `ControlGroupIconPriority`, `FactionUnitUi`, `FactionUiController`, `ShipInstaller`, `AttackTargetState`, `MapNotifier`, `ThreatNotifier`, `EnemyStrategicContextBuilder`, `AllyAiMarkerEmitter`, `CompositeBeamSettings`, `LuckyShotSettings`, `HardPointHealthBudget`), and Editor Balance tools (`BalanceCompareOrder`, `BalanceWeaponAdapter`). Zero `HeavyCapital` or `ShieldRegenTier.Executor` references remain in `Assets/Scripts/`.
- Updated rule/faction/ship assets: `DamageMatrixData.asset`, `WeaponsData.asset`, 4 `ShipShieldRegenTiersData` assets (`Shared`, `Empire`, `Rebellion`, `Wavecrest`), `HardPointHealthSettings.asset`, `CinematicCameraData.asset`, `ShipClassMatchupData.asset`, 4 faction research assets (`Empire`, `Rebellion`, `Republic`, `Separatist`), and 27 reassigned `*ShipData.asset` files (76 total `ShipData` assets match their `ShieldRegenTier`).
- Resized `NebulonB` from 300 m (`31.5` units) to 600 m (`63.0` units, `2x` scale) across `NebulonBShipView.prefab`, `NebulonBWreckView.prefab`, `NebulonBReinforcementView.prefab`, `NebulonBShipData.asset`, `Tools/ShipScale/targets.json`, and `Tools/ShipScale/results.csv`, and rebaked `visionHull` via `ShipVisionHullBaker.BakeAll()` (reverted unrelated `1e-6` float noise on untouched `DiamondShipData.asset`).
- Updated 13 EditMode test files (`ShipAbilityServiceTests`, `TractorBeamSettingsTests`, `BalanceCompareOrderTests`, `BalanceLayoutTests`, `CinematicCameraModelTests`, `BattleSimulationTests`, `OverkillTargetingTests`, `WeaponLoadoutBakeTests`, `EnemyProductionDecisionModelTests`, `EnemyTaskForceSelectionModelTests`, `ShipPursuitTests`, `ControlGroupPreviewTests`, `CorvetteLoadoutTests`). Unity tests were **not executed** per the repository Local Test Execution Override.
- Unity Editor verification (`unity command`): `AssetDatabase.Refresh()` compiled cleanly (`diagnostics: []`); `ForceReserializeAssets` + `SaveAssets()` persisted 43 modified `.asset`/`.prefab` files; Console reported 0 errors and 0 warnings; `BalanceInventory.Build()` reported 0 errors (157 units); 76/76 `ShipData` assets passed `ShipClass`/`ShieldRegenTier`/`ShieldRegenerateValue > 0`/`VisionHull`/Corvette+Frigate caliber checks; `DamageMatrixData`, `CinematicCameraData` (12/12 profiles), and `FactionDefinition` research arrays passed; `NebulonB` view (`62.99927`), wreck (`62.9992638`), ship/reinforcement colliders (`62.9991531`), and `visionHull` (`62.9991455`) match `63.0` units, and all 6 forward main batteries converge at `(0, 0, 150)`.
- Canonical guidance updated: [[GameDesign/Combat]], [[GameDesign/Units_And_Factions]], [[GameDesign/Production_And_Research]], and class minimum table in [[Review/Refactoring/Hardpoint_Health_From_Hull_Plan]]. Remaining visual/skirmish and authorized test-run gates queued in [[TODOs/User_Acceptance_Queue]].
