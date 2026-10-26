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
- quad turbolaser battery → `MediumQuadTurbolaser` (90) on every ship (Phase 5, 2026-10-26). Superseded rule: Capital main-battery quads → Heavy Quad (89).
- Caliber source: RaW projectile `Scale_Factor` — 0.1 Light, 0.175 standard/twin/quad → Medium, 0.2 Heavy. Never infer caliber from damage/DPS.
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
- [ ] Unity scenes clean (`unity command list_open_scenes --json`).

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

### Phase 5 — Caliber from projectile + sublinear mounts ✅ 2026-10-26 (`ed36e0040`)

- Problem: damage = family × caliber (1 / 1.25 / 1.5625) × barrels → barrels dominated caliber (Medium Quad 80 > Heavy Dual 62.5; Heavy Quad 100).
- RaW 1.5.5 turbolaser projectile damage, red (CIS) / blue (Republic): single 15/15, twin 25/25, quad 30/40, heavy 20/35. Hardpoints may override damage (Providence).
- A: Republic/CIS Heavy Quad (89) → Medium Quad (90): Mandator 8, Providence 6 (+3 → 9), Invincible 6, Lucrehulk 8. `mainBattery` kept. Empire Heavy Quads (Allegiance, Tector, Titan, Procursator) wait for their hardpoint pass.
- B: `WeaponMountRule` + `WeaponsData.mounts`: Single 1, Dual 1.6, Triple 2.0, Quad 2.4, Octuple 4.0. `WeaponProfileFactory` uses it for ship weapons; squadron weapons keep ×barrels.
- Turbolaser shot damage now: Medium 20, Medium Dual 32, Medium Quad 48, Heavy 25, Heavy Dual 40, Heavy Quad 60.
- Executor: 50/51/53 `damageMultiplier` 1.8 → 2.25 (×2/1.6); `caliber_executor.csv` identical to `final_executor.csv`.
- Tests: EditMode 2078: 2075 pass; same 3 unrelated failures (`ExecutorShipTests` type 54, `ISDIIShipTests` 32 vs 28, `HealthModelTests.PointDefense_WhileCloaked…`). Changed: `Mounts_MultiplyShotDamageByMountRule_AndKeepShots`, `EveryFamilyCaliberAndMount_HasExactlyOneRule`, `QuadTurbolasers_AreMediumOnRepublicAndCisShips` (replaces `QuadBatteries_AreHeavyOnlyAsCapitalMainBattery`), ISD II mount asserts; added `SquadronMounts_KeepFullDamagePerBarrel`.
- Simulator `Logs/Rebalance/2026-10-26-caliber/`: DPS avg Separatist −16 %, Empire −15 %, Republic −15 %, Rebellion −8 %; 17/56 ships unchanged. Biggest: Lucrehulk −42 %, ISD II −41 %, Providence −41 %, Endurance −38 %. Within ±15 % baseline TTK 153 → 121/260 (lower DPS → longer TTK).

| Class | Empire | Rebellion | Republic | Separatist |
|---|---|---|---|---|
| Corvette | 29 → 19 → 19 % | 71 → 69 → 69 % | 56 → 76 → 76 % | 43 → 30 → 30 % |
| Frigate | 28 → 27 → 27 % | 50 → 42 → 45 % | 49 → 56 → 48 % | 67 → 70 → 75 % |
| Cruiser | 21 → 34 → 36 % | 80 → 76 → 82 % | 21 → 50 → 41 % | 75 → 12 → 12 % |
| Capital | 59 → 65 → 71 % | 51 → 46 → 49 % | 37 → 27 → 31 % | 56 → 76 → 50 % |
| HeavyCapital | 67 → 67 → 67 % | — | 80 → 80 → 80 % | 12 → 12 → 12 % |

Columns: baseline → Phase 4 final → Phase 5.

### Phase 6 — Medium guns on big ships, caliber-free point defense, targeting ✅ 2026-10-26

- Rule: Cruiser/Capital/HeavyCapital carry no Light weapons (all factions; Executor untouched). Retype → same family, Medium: 1→4, 6→48, 7→94, 8→23, 10→32, 13→93, 36→5, 82→90, 92→95; Procursator unit triple (86) → Medium caliber, renamed `ProcursatorMediumTripleTurbolaser`.
- New shared weapons: `MediumMissileLauncher` 93, `MediumDualLaser` 94, `MediumDualMissileLauncher` 95. SFX entry for 94 copied from 7.
- Point defense has no caliber: `WeaponProfileFactory` skips caliber damage/range/turn/bias for `WeaponFamily.PointDefense`. Shared PD = `PointDefense` 14 (single), `DualRepeatingPointDefense` 62 (dual); `HeavyPointDefense` 84 deleted (Endurance 6 → 14); 80 → Executor-only `ExecutorPointDefense` (dmg ×1.25, range ×1.15; profile identical to baseline).
- Retyped hardpoints: Victory 11, Victory I 8, MC75 12, MC50 4, Heavy Dreadnought 4, Imperator 5, Mandator 28, Resolute/Venator II 3, Rothana 7, Venator 3, Invincible 9, Lucrehulk 8, Malevolence 20, Providence 9, Recusant 4. Loadouts rebaked (`WeaponLoadoutBaker.BakeAll`). Victory radar 600 → 640 (medium dual missile 632.5).
- Targeting:
  - Facing: `WeaponFacingSolver.FindTurn(arcs, weights)`; `WeaponComponent.GetFiringTurnAngle` weights each gun by DPS×10, skips PD, counts only guns in reach while any reach.
  - Enemy order: listed groups re-sorted nearest first (planar) every 1 s, staggered per ship, and after every rebuild (`SortListedGroupsByDistance`; reorders slots, no re-acquire). Main target block stays first.
  - Hardpoint: first group in reach is the target; its nearest reachable hardpoint is hit (job, `TargetSelectionBatch` serial, `TryFireWeapon` identical).
  - `BattleSimulation` keeps its own targeting → simulator does not cover these fixes.
  - Playtest fix: the sort read destroyed enemies' transforms (`MissingReferenceException` aborted `WeaponComponent.Tick` → ships stopped firing). Destroyed groups now sort last unread; test `ListedGroups_DestroyedTargetSortsLastWithoutReadingItsTransform`.
  - Playtest fix: a ship that killed its attack target went idle and only faced enemies already in range (corvettes ignored 30 nearby squadrons). `ShipOrderRunner.CompleteOrder` now chains the Attack order to `ShipEngagement.FindNearestEnemy(radar, …, inRangeOnly: false)` — nearest live preferred radar contact, else nearest other; `IdleState` shares the helper (`inRangeOnly: true`). Tests `Attack_TargetKilled_AttacksNearestPreferredRadarContactEvenOutOfRange`, `Attack_TargetKilled_NoRadarContactGoesIdle` — pass (`ShipPursuitTests` 21/21).
- Tests: EditMode 2086: 2084 pass; fails `ISDIIShipTests` (32 vs 28), `HealthModelTests.PointDefense_WhileCloaked…` (both pre-existing). Fixed stale `ExecutorShipTests` (type 54 → 80). Added `FindTurn_OneHeavyGunOutweighsSeveralLightOnes`, `ScheduledJob_KeepsTheFirstGroupInReachOverANearerLaterGroup`, `ListedGroups_SortNearestFirstBehindTheMainTarget`.
- Simulator `Logs/Rebalance/2026-10-26-medium/`: big-ship raw DPS +3 to +16 % (Victory 396 → 459, Heavy Dreadnought 125 → 144, Mandator 1009 → 1114); Endurance −11 % (heavy PD → PD). Class win rates unchanged except Cruiser: Empire 36 → 41, Rebellion 82 → 74, Republic 41 → 46 %.

### Phase 7 — Shield regeneration tiers by class ✅ 2026-10-26 (values A approved by user)

- Mechanics (unchanged): every `ShieldRegenerateDelay` (1 s, all ships) shields gain the regen value, also under fire, until all shield generators die → regen cancels that much shield DPS. Damage vs shields: Turbolaser/Beam/IonCannon ×1, Ion ×3, Laser ×0.5, PD ×0.25, FighterLaser ×0.2; missiles, torpedoes, mass drivers pierce shields.
- Before: per-ship values, no rule (corvette 2–7, frigate 4–31, cruiser 12.5–41, capital flat 50 or 83–167, heavy 50–278); several stale after HP/shield rebalance (Stealth Corvette, Victory, Heavy Dreadnought).
- Now: `ShipShieldRegenTiersData` (`Assets/Settings/Data/Ship/Shared/`) + `ShipData.ShieldRegenTier` (enum `ShieldRegenTier`); `ShipData.ShieldRegenerateValue` reads the tier. Old per-ship field removed. Same pattern as `ShipHeightTiersData`. Balance editor: `ShieldRegenTier` enum field (Combat).
- Tier = ship class by default; override = pick another tier. Executor → own `Executor` tier (1111.1111, unchanged).
- Proposed values (A, saved): Corvette 5, Frigate 25, Cruiser 90, Capital 180, HeavyCapital 400 /s.
- Simulator `Logs/Rebalance/2026-10-26-regen/` (C 4/15/60/120/250, A, B 5/30/150/300/600): lower class beats higher class — Corvette>Frigate 11/9/7 %, Frigate>Cruiser 6/4/3 %, Cruiser>Capital 6/6/10 %, Capital>Heavy 5/4/4 %. Same-class stalemates 1/2/11 of 2600.
- Outliers no tier fixes: Munificent and Dispatcher beat Heavy Dreadnought 10–0 in C/A/B (Heavy Dreadnought 144 DPS). Recusant vs Munificent 3–7 (C), 5–5 (A), 10–0 (B); B drops Separatist cruiser same-class win rate 12 → 0 %.
- Corvette radar (2026-10-26): CR70, Carrack, C-9979, Patrol Frigate 300 → 500 (= Thranta). At 300 they only detected what their lasers already reached, so the kill-retarget (radar contacts) could not find nearby squadrons. Full EditMode 2153: 2150 pass; fails `ISDIIShipTests` (pre-existing), `MapLayoutGeneratorTests` (CLI bridge timeout log; passes 23/23 alone), `LoadingUiIsolationTests` (ran while the user was playtesting; passes 4/4 alone). Simulator `Logs/Rebalance/2026-10-26-radar/` vs `2026-10-26-regen/` A: 21 duel rows changed, wins ±1 run at most, TTK +1 s; class win rates and cross-class results unchanged (duels have no squadrons, so the squadron-detection gain is playtest-only).
- Tests: `ShieldRegen_FollowsShipClassTiers_AndRisesWithClass`. EditMode 2153: 2151 pass; fails `ISDIIShipTests` (32 vs 28), `HealthModelTests.PointDefense_WhileCloaked…` (pre-existing).

### Phase 8 — Heavy caliber beats more barrels; slower lasers ✅ 2026-10-26

- Problem: caliber step ×1.25 vs mount step ×1.6/×2.4 → heavy single turbolaser (20.0 vs capital) ≤ medium dual (20.5) and < light quad (22.2); medium quad ≥ heavy dual vs frigate/cruiser.
- Caliber damage: Medium 1.25 → 1.35, Heavy 1.5625 → 2.2 (rule: heavy/medium > 1.53 so heavy single > medium dual vs Cruiser).
- Laser family reload 1.5 → 2 s (laser DPS −25 %; user: let DPS drop). Carrack 230 → 173 raw DPS.
- Per-mount hull DPS after (vs Cruiser / Capital): Medium Dual TL 22.2 / 22.2; Light Quad 23.4 / 22.2; Heavy TL 23.6 / 28.2; Medium Quad 33.2 / 33.2; Heavy Dual 37.7 / 45.1; Heavy Quad 56.6 / 67.7; Heavy Octuple 94.4 / 112.9. Medium still wins vs Frigate/Corvette.
- Executor unchanged: 34/49 dmg ×1.2784091, 50/51 ×1.5980114, 53 ×2.0833333; new `ExecutorHeavyLaser` 96 (unit Executor, dmg ×0.7102273, reload ×0.75) replaces its 40 shared Heavy Lasers (28). `Executor_WeaponProfilesMatchBaseline` passes.
- Squadrons unchanged: 21, 47 dmg ×0.7102273; 72 ×0.9259259.
- Simulator `Logs/Rebalance/2026-10-26-heavy/` vs `2026-10-26-radar/`: median TTK Capital 50.6 → 40.0 s, HeavyCapital 84.5 → 72.3 s, Corvette 27.1 → 36.3 s. Venator I/II, Resolute now beat Providence (0–10 → 9–1/10–0). Win rates: Capital Sep 58 → 47, Rep 33 → 37; Cruiser Rep 49 → 40, Reb 70 → 78; HeavyCapital Rep 80 → 60, Emp 67 → 78. Lower class beats higher: 9/4/6/4 → 3/7/5/6 %.
- Tests: EditMode 2227: 2216 pass; failures not from this change: `FactionAudioAssetTests` ×8 and `SquadronIconPrefabTests` (other session's uncommitted audio/UI work), `ISDIIShipTests`, `HealthModelTests.PointDefense_WhileCloaked…` (pre-existing).

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
- [x] Phase 5 caliber from projectile + mount rules (2026-10-26, `ed36e0040`)
- [x] Phase 6 medium guns on big ships, caliber-free point defense, targeting fixes (2026-10-26)
- [x] Phase 7 shield regen tiers — values A (5/25/90/180/400) approved 2026-10-26
- [ ] Phase 4 user skirmish playtest (now includes Phases 5–7)
- [ ] Decide: accept uneven class win rates, or tune Empire/Rebellion DPS (needs their hardpoint simplification)
- [ ] Later: Empire/Rebellion hardpoint simplification
