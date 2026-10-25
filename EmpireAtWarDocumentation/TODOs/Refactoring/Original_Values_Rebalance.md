---
category: Refactoring
status: in-progress
created: 2026-10-25
---
# Original Values Rebalance

Data and comparison tables: [[TODOs/Refactoring/Original_Values_Rebalance - Research|Research]]. Builds on [[TODOs/Refactoring/Weapon_Taxonomy_Balance_System|Weapon Taxonomy and Balance System]].

## Goal

- Republic and CIS use original Republic at War 1.5.5 HP, shields and hardpoint lists, with simplified hardpoints.
- Empire and Rebellion HP/shields rebalanced into the same per-class bands; their hardpoints unchanged for now.
- Range classes (Close / Standard / Long) removed; gun reach measured from the hull for every ship.

## Decisions (user, 2026-10-25)

- **Executor**: not touched. Stays overpowered: same HP, shields, hardpoints and effective per-gun stats.
- **Range fix**: hull-relative engagement for all ships (no per-unit range bonuses).
- **Hardpoint simplification**: Republic and CIS only for now. Empire/Rebellion later.
- **C-9979**: stays a combat corvette, balanced to the corvette band.
- **Class caliber rule** (Capital/HeavyCapital ≥ Medium): removed. Corvette no-Turbolaser and Frigate no-Heavy rules stay.
- **Tests**: updated with the change (see Tests).

## Weapon Mapping (user, 2026-10-25)

- turbolaser battery → `MediumTurboLaser` (4) on every ship.
- quad turbolaser battery:
  - Capital/HeavyCapital and the quads are the ship's main battery (largest turbolaser DPS group) → Heavy Quad (merged 20); set hardpoint `mainBattery` = true.
  - Otherwise → Medium Quad. No such definition exists → add shared `MediumQuadTurbolaser` (new enum value, Turbolaser / Medium / Quad / Standard).
- Remaining defaults (no size in the name → Light):
  - laser cannon battery → `Laser` (8); "X2" → `DualLaser` (7)
  - heavy turbolaser → `HeavyTurboLaser` (2)
  - double/dual turbolaser battery → `LightDualTurbolaser` (36); heavy dual turbolaser turret → `DualHeavyTurboLaser` (3); DBY-827 → 9
  - ion cannon battery → `IonCannon` (10); heavy dual ion turret → 51 renamed `HeavyDualIonCannon`
  - concussion missile → `ConcussionMissile` (13); proton / tracking torpedo → `ProtonTorpedo` (6); PD → `PointDefense` (14); mass driver → 19

## HP / Shields Exceptions (user, 2026-10-25)

- Keep existing values, not original: Mandator 50000/50000, Invincible 15000/8000, Lucrehulk 12000/6600.
- Rothana takes original 10300/3000 (not listed as exception).

## Rules

- Source: `F:/CodexArtifacts/republic-at-war-roster/fleet-catalogue.txt` original blocks. "Our project" blocks are stale; use live `ShipData`.
- Original data has no weapon damage or range → keep `WeaponsData` family/caliber/mount rules.
- Every loadout change updates prefab `WeaponHardPoint`s, `ShipData.weaponLoadout`, `hardPointHealth` and `HealthComponent.ShipUnits` together; every weapon stays targetable.
- A ship with a hangar keeps at least one hangar bay.
- No agent Play Mode. Verify by compile + EditMode + Battle Logic Simulator; user playtests.

## Preconditions

- [ ] Commit or shelve the current uncommitted WIP (fire-pattern removal, `StatModifierAbility`, 11 ship prefabs/data).
- [ ] Unity scenes clean (`assert_test_ready`).

## Implementation

### Phase 0 — Baseline ✅ 2026-10-25

1. [x] Simulator duels: every same-class, cross-faction ship pair, 1v1, 10 seeds (seed 1), 900 s limit, live assets → 260 matchups.
2. [x] Per-unit price, HP, shields, radar, `BalanceDpsEstimator` DPS, mount count (56 ships).
3. [x] Executor per-gun profiles (damage, shots, interval, reload, range, accuracy, DPS).
- Revision `2f45f1371`, recompile clean, no errors building any unit. Data: `Logs/Rebalance/2026-10-25-baseline/` (`baseline_units.csv`, `baseline_matchups.csv`, `baseline_executor.csv`); rerun with `unity command run_script --file <copy of BaselineDump.cs.txt as .cs> --entry BaselineDump.Run --args '[""]'` (~10 s). Summary: [[TODOs/Refactoring/Original_Values_Rebalance - Research#Baseline 2026-10-25|Research]].

### Phase 1 — Remove range classes, hull-relative reach

1. Delete `WeaponRangeClass`, `WeaponRangeClassRule`, `WeaponsData.rangeClasses`, `GetRangeClass`, the factory parameter and `WeaponDefinition.rangeClass`.
2. Drop `WeaponProfile.AccuracyMultiplier` (only fed by range class) from `WeaponComponent`, `WeaponEngagementRange`, `BattleSimulation`.
3. Merge duplicates: 27→1, 39→4, 49→2, 50→3, 53→5, 44→11, 34→65; Mandator 73–78 → shared twins; quads 20/87/88 → one Heavy Quad. Remap `ShipData` loadouts, prefab hardpoints, `ShipSfxData` overrides; drop "Long-Range" from display names.
4. Rename 51/67/68 without range wording; keep enum int values (serialized).
5. Executor: its Long weapons become Executor-scoped variants that keep current expected stats: damage ×1.8 (1.5 × lost 1.2 accuracy), reload ×2, range ×1.5; `reason` = "Executor stays overpowered". Affected types: 34, 49, 50, 51, 53 (all shared with other ships, so variants are new Executor-only definitions). Heavy Laser and Medium PD are Standard → unchanged.
6. Hull-relative reach for all ships: add own `NavigationRadius` to engagement distance in `ShipEngagement.GetStandoffPosition`, `WeaponComponent.HasEnoughRange` and `AttackTargetState` close/stop checks.
7. Remove the Capital/HeavyCapital ≥ Medium class caliber rule.
8. Update `BalanceWeaponAdapter` (`RegisterRangeClasses`, `rangeClass` field).

### Phase 2 — Republic and CIS original values + simplified hardpoints

1. HP/shields from the original column for every mapped ship, except Mandator, Invincible, Lucrehulk (keep live).
2. Loadouts from original lists via the mapping; simplify: merge same-type batteries into fewer, larger mounts where the hull has fewer sockets (same total barrels).
3. System hardpoints (engines, shield gens, hangars, tractor) by original counts.
4. Prefabs: retype existing hardpoint transforms first; delete surplus; add only where needed, placed on the hull.
5. C-9979: corvette band (550–900 HP, 500–900 shields, 6–8 guns).
6. Not in catalogue (Thranta, Venator II): scale into class band from nearest sibling.

### Phase 3 — Empire and Rebellion HP/shields to the bands

1. Derive class bands (HP, shields, DPS per price) from Phase 2 data.
2. Set Empire/Rebellion HP/shields into their band by role and price. No hardpoint changes. Executor excluded.

### Phase 4 — Verify

1. Compile; full EditMode run.
2. Simulator matchups vs Phase 0: mirrored class matchups within ±15 % TTK cross-faction; Executor DPS/HP identical to baseline.
3. Every changed ship spawns (prefab/data contract) — Editor-only check.
4. User skirmish playtest.

## Tests

### Update

- `WeaponTaxonomyTests`:
  - `SavedShipWeapons_RespectClassCaliberRules` → drop Capital/HeavyCapital cases; keep Corvette/Frigate.
  - `EveryFamily_HasExactlyOneRule_AndEveryCaliberAndRangeClassHasOne` → families + calibers only.
  - Delete `RangeClasses_TradeReachForDamage`.
  - `EngagementRange_HoldsHeavyGunsBackFromCapitals_AndClosesLasersOnCorvettes`, `PointDefenseOnly_ApproachesShipsToItsReach` → hull-relative distances.
  - `EveryUnitRadar_ReachesItsLongestGun` → re-run; values only drop.
- `BoltParticleSimulationTests`, `ShotEffectPoolLifecycleTests`, `BattleSimulator/BattleSimulationTests` → no range-class argument / accuracy multiplier.
- `EmpressDefensePlatformCombatTests.LongRangeBatteries_*`, `ISDIIShipTests.SavedLongRangeBatteries_*` → renamed weapon types and test names.
- `ShipPursuitTests`, `ShipStopTests`, `AttackMoveEngagementTests` → standoff includes own radius.
- Stat/loadout asserts on changed ships: `CorvetteLoadoutTests` (CR70, C-9979, Patrol Frigate), `ShipSystemHardpointTests`, `ShipAssetIntegrityTests`, `RaiderCorvetteTests`, `TartanTests`, `AllegianceShipTests`, `ProcursatorShipTests`, `TitanShipTests` (Phase 3 HP/shields; quad type remap).
- `ExecutorShipTests` must pass unchanged.

### Add

- `RepublicCisShips_MatchOriginalValues` — table-driven HP/shields + weapon counts per family vs catalogue; Mandator, Invincible, Lucrehulk assert live HP/shields.
- `QuadBatteries_AreHeavyOnlyAsCapitalMainBattery` — Heavy Quad only on Capital/HeavyCapital hardpoints with `mainBattery`; other quads Medium.
- `HullRelativeEngagement_BigShipsReachTheirTarget` — Mandator, Malevolence, Lucrehulk, MC80, Home One: at standoff ≥ 60 % of DPS in reach.
- `Executor_WeaponProfilesMatchBaseline` — per-gun damage/reload/range equal to Phase 0 snapshot.
- `Corvettes_StayInCorvetteBand` — every Corvette incl. C-9979.
- `NoTwoWeaponDefinitionsShareTaxonomy` — guards against range-only duplicates returning.

## Important Values

- Long rule: range ×1.5, dmg ×1.5, reload ×2, accuracy ×1.2 → removal = range −33 %, DPS +33 %.
- Heavy turbolaser Standard range 585; nav radius Mandator 483, Executor 1059.
- `WeaponEngagementRange.DAMAGE_COVERAGE` = 0.6.

## Edge Cases

- Hull-relative reach makes Executor stronger still → accepted (must be overpowered).
- Mandator PD 345 → 230 leaves far-side fighters uncovered → check in simulator.
- Saved battles store weapon types only for superweapons (`SuperWeaponSave`); verify `BattleSaveValidator` after merges.
- `MandatorHeightTiersData` unaffected.

## Files

- `Assets/Scripts/Components/AttackComponent/` — `WeaponRangeClass*.cs`, `WeaponDefinition.cs`, `WeaponsData.cs`, `WeaponProfile*.cs`, `WeaponType.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`, `WeaponEngagementRange.cs`; `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs`, `AttackTargetState.cs`
- `Assets/Scripts/Editor/Balance/BalanceWeaponAdapter.cs`, `Editor/BattleSimulator/BattleSimulation.cs`
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`, `Models/Audio/ShipSfxData.asset`, `Ship/*/*ShipData.asset`
- `Assets/Prefabs/Models/Ships/**/*ShipView.prefab`
- `Assets/Scripts/Tests/Editor/` — files listed under Tests

## TODO

- [x] Preconditions — WIP committed `d91020e89`, plan `ccd2c259b`. Open scene is untitled + dirty → EditMode test runs blocked (`BLOCKED_DIRTY_UNTITLED_SCENE`) until the user saves or discards it.
- [x] Decisions answered (2026-10-25)
- [x] Phase 0 baseline (2026-10-25, `2f45f1371`)
- [ ] Phase 1 range-class removal + hull-relative reach + Executor variants
- [ ] Phase 2 Republic/CIS original values + simplified hardpoints
- [ ] Phase 3 Empire/Rebellion HP/shields
- [ ] Phase 4 verification + user playtest
- [ ] Later: Empire/Rebellion hardpoint simplification
