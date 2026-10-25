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

### Phase 1 — Remove range classes, hull-relative reach (done except step 6, 2026-10-25)

- Done: steps 1–5, 7, 8. Range-class types/rule/field/`AccuracyMultiplier` deleted; merges remapped in prefabs, `ShipData` loadouts, `ShipSfxData` (20 → 89, others dropped).
- Executor variants reuse ints 34/49/50/51/53 (Unit scope `Executor`, dmg ×1.8, reload ×2, range ×1.5) so `ExecutorShipTests` ints stay. Enum names: `HeavyArtilleryRocket`, `ExecutorHeavyTurbolaser`, `ExecutorHeavyDualTurbolaser`, `ExecutorHeavyDualIonCannon`, `ExecutorMediumDualTurbolaser`.
- Deviation: 51 is Executor-only → Phase 2 heavy dual ion turret needs a new shared `HeavyDualIonCannon` value.
- Renamed: 67 `HeavyTripleTurbolaser`, 68 `HeavyTripleIonCannon` (Empress only).
- **Step 6 dropped (2026-10-25, user asked to complete the plan; dropped as the recommended option).** `HullRelativeEngagement_BigShipsReachTheirTarget` dropped with it. Adding own `NavigationRadius` to standoff parks big ships too far: DPS in reach vs Capital (bow / broadside) old → new: Mandator 34/43 % → 9/0 %, Titan 86/100 % → 0/1 %, MC80 100/100 % → 53/3 %, Malevolence 77/89 % → 25/0 %. Centre-to-centre + contact distance already handles big ships after range removal.
- Tests: full EditMode 2052 → 2041 pass; own failures fixed (taxonomy guard, Empress 800 → 500). Remaining unrelated: `ExecutorShipTests` (expects type 54), `ISDIIShipTests` (32 vs 28 hardpoints, from `64747b214`), 7 `InputBindingServiceTests`.
- Snapshot: `Logs/Rebalance/2026-10-25-phase1/` (includes `60d9cf9d0` Tector/ISD II changes, not only Phase 1).

1. Delete `WeaponRangeClass`, `WeaponRangeClassRule`, `WeaponsData.rangeClasses`, `GetRangeClass`, the factory parameter and `WeaponDefinition.rangeClass`.
2. Drop `WeaponProfile.AccuracyMultiplier` (only fed by range class) from `WeaponComponent`, `WeaponEngagementRange`, `BattleSimulation`.
3. Merge duplicates: 27→1, 39→4, 49→2, 50→3, 53→5, 44→11, 34→65; Mandator 73–78 → shared twins; quads 20/87/88 → one Heavy Quad. Remap `ShipData` loadouts, prefab hardpoints, `ShipSfxData` overrides; drop "Long-Range" from display names.
4. Rename 51/67/68 without range wording; keep enum int values (serialized).
5. Executor: its Long weapons become Executor-scoped variants that keep current expected stats: damage ×1.8 (1.5 × lost 1.2 accuracy), reload ×2, range ×1.5; `reason` = "Executor stays overpowered". Affected types: 34, 49, 50, 51, 53 (all shared with other ships, so variants are new Executor-only definitions). Heavy Laser and Medium PD are Standard → unchanged.
6. Hull-relative reach for all ships: add own `NavigationRadius` to engagement distance in `ShipEngagement.GetStandoffPosition`, `WeaponComponent.HasEnoughRange` and `AttackTargetState` close/stop checks.
7. Remove the Capital/HeavyCapital ≥ Medium class caliber rule.
8. Update `BalanceWeaponAdapter` (`RegisterRangeClasses`, `rangeClass` field).

### Phase 2 — Republic and CIS original values + simplified hardpoints ✅ 2026-10-25

- Script: `ShipHardPointRegistry` retype → add (midpoint of two same-side sockets) → remove → renumber; `ShipHardPointValidator` clean on every ship. Script copy: `Logs/Rebalance/2026-10-25-final/`.
- New shared weapons: `MediumQuadTurbolaser` 90, `HeavyDualIonCannon` 91, `LightDualMissileLauncher` 92.
- Republic Victory (`Empire/VictoryShipData`, Republic faction) added: 4900/3100, 4 MQ, 2 Light Quad, 4 Dual Laser, 4 Light Dual Missile, 1 torpedo. No hangar added (ship has no `HangarComponent`).
- Deviations: Munificent 2 heavy turbolasers → Medium (Frigate no-Heavy rule); Munificent's 8 unregistered `DualProjectile` sockets registered as Light Dual Turbolasers; corvettes CR70, Stealth, Patrol Frigate lose PD (original has none); Lucrehulk `IonCannonL1` moved to port (sat on top of `IonCannonR1`).
- Systems added: Imperator tractor beam (+ `TractorBeam` ability, health 400); second hangar on Providence, Invincible, Lucrehulk (`bayHardPoints` empty → shared bays).
- Radar raised to longest gun: Arquitens 540 → 550, Pelta 500 → 520, Stealth Corvette 300 → 550 (concussion missiles).

1. [x] HP/shields from the original column for every mapped ship, except Mandator, Invincible, Lucrehulk (keep live). 2026-10-25: Stealth Corvette 850/900, Centax 2000/1300, Imperator 7500/3500, Rothana 10300/3000, Venator 5000/3000, Munificent 2500/1200, Providence 5000/3000; C-9979 600/600 (corvette band). Thranta, Venator II already in band → unchanged.
2. Loadouts from original lists via the mapping; simplify: merge same-type batteries into fewer, larger mounts where the hull has fewer sockets (same total barrels).
3. System hardpoints (engines, shield gens, hangars, tractor) by original counts.
4. Prefabs: retype existing hardpoint transforms first; delete surplus; add only where needed, placed on the hull.
5. C-9979: corvette band (550–900 HP, 500–900 shields, 6–8 guns).
6. Not in catalogue (Thranta, Venator II): scale into class band from nearest sibling.

### Phase 3 — Empire and Rebellion HP/shields to the bands ✅ 2026-10-25

- Decision: clamp into the Republic/CIS min–max band per class (live exceptions included); in-band values unchanged. Least-squares price fit rejected: 3–7 points per class gave Tector 12000 → 5000, Titan → 50000.
- Bands HP / shields: Corvette 600–1600 / 600–1000; Frigate 2000–4700 / 800–2000; Cruiser 3600–4900 / 2500–3100; Capital 5000–15000 / 3000–8000; HeavyCapital 12000–50000 / 6600–50000.
- Changed: Acclamator Assault 4700/2000, Raider 600/600, Tartan 600/600, Victory I 3600/2500, Victory II 4000/3100, Assault Frigate Mk II 3500/2000, Bothan 4900/3100, CD-273 1600/1000, Corellian Battlecruiser 5000/3000, CR90 600/600, Dauntless 4900/3100, Home One 15000/8000, Liberator 5000/4200, MC30c 2000/2000, MC75 4500/3100, MC80 Independence 15000/8000, Mon Cal Cruiser 8500/8000, Quasar Fire 2000/1300.

1. Derive class bands (HP, shields, DPS per price) from Phase 2 data.
2. Set Empire/Rebellion HP/shields into their band by role and price. No hardpoint changes. Executor excluded.

### Phase 4 — Verify (automated done 2026-10-25; playtest pending)

- EditMode 2077: 2074 pass. Failing, unrelated to this plan: `ExecutorShipTests` (expects nonexistent type 54), `ISDIIShipTests` (32 vs 28 hardpoints, `64747b214`), `HealthModelTests.PointDefense_WhileCloaked…` (fails only in a long-running Editor; passed headless).
- Executor 200000/200000; expected per-gun DPS pinned by `Executor_WeaponProfilesMatchBaseline`.
- Simulator `Logs/Rebalance/2026-10-25-final/`: ±15 % target **not met** — 153/260 matchups within 15 % of baseline TTK; class win rates still uneven (Cruiser: Separatist 12 %, Republic 50 %, Rebellion 76 %; Capital: Separatist 76 %, Republic 27 %). Cause: Empire/Rebellion hardpoints unchanged → DPS gaps remain.

| Class | Empire | Rebellion | Republic | Separatist |
|---|---|---|---|---|
| Corvette | 29 → 19 % | 71 → 69 % | 56 → 76 % | 43 → 30 % |
| Frigate | 28 → 27 % | 50 → 42 % | 49 → 56 % | 67 → 70 % |
| Cruiser | 21 → 34 % | 80 → 76 % | 21 → 50 % | 75 → 12 % |
| Capital | 59 → 65 % | 51 → 46 % | 37 → 27 % | 56 → 76 % |
| HeavyCapital | 67 → 67 % | — | 80 → 80 % | 12 → 12 % |

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
- [x] Phase 1 range-class removal + Executor variants (`38c0a9d4e`); step 6 dropped
- [x] Phase 2 Republic/CIS original values + simplified hardpoints
- [x] Phase 3 Empire/Rebellion HP/shields
- [x] Phase 4 automated verification (compile, EditMode, simulator)
- [ ] Phase 4 user skirmish playtest
- [ ] Decide: accept uneven class win rates, or tune Empire/Rebellion DPS (needs their hardpoint simplification)
- [ ] Later: Empire/Rebellion hardpoint simplification
