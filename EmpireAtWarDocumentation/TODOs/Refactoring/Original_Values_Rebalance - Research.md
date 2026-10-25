---
category: Refactoring
status: todo
created: 2026-10-25
---
# Original Values Rebalance - Research

Companion to [[TODOs/Refactoring/Original_Values_Rebalance|Original Values Rebalance]].

## Sources

- Original: `F:/CodexArtifacts/republic-at-war-roster/fleet-catalogue.txt` (Republic at War 1.5.5 XML). HP, shields, hardpoint lists only; no weapon damage/range.
- Catalogue "Our project" blocks are stale. Live values below dumped from `Assets/Settings/Data/Ship/*/*ShipData.asset` on 2026-10-25 (working tree, uncommitted WIP included).

## Republic: original vs live

| Ship | Orig HP/Sh | Live HP/Sh | Orig weapons | Live weapons |
|---|---|---|---|---|
| CR70 | 750/650 | 750/650 | 7 laser | 8 L laser, 2 PD |
| Stealth Corvette | 850/900 | 400/500 | 2 conc, 2 laser | 4 L laser, 2 PD |
| Carrack | 1600/1000 | 1600/1000 | 12 laser X2 | 12 L dual laser |
| Centax | 2000/1300 | 2000/2000 | 1 dbl turbo, 8 laser, 2 proton | 8 L laser, 2 L torp, 1 M dual turbo |
| Pelta | 2200/1000 | 2200/1000 | 4 turbo, 4 laser, 2 ion | 4 L turbo, 4 L laser, 2 L ion |
| Arquitens | 2900/1400 | 2900/1400 | 4 quad turbo, 4 laser, 1 conc | 4 M dual turbo, 1 L torp, 4 L dual laser |
| Heavy Dreadnought | 3600/2500 | 3600/2500 | 8 laser, 4 turbo | 2 H turbo, 2 H dual turbo, 2 H ion |
| Acclamator | 4700/2000 | 4700/2000 | 1 proton, 3 quad turbo, 2 PD | 4 M turbo, 1 L torp, 2 L laser |
| Venator | 5000/3000 | 5000/4500 | 8 DBY-827, 2 turbo, 2 laser, 2 PD, 1 proton, beam | 8 DBY, 2 M dual turbo, 4 M laser, 2 PD, 1 M torp, beam |
| Resolute | 7000/5000 | 7000/5000 | as Venator | as Venator |
| Imperator | 7500/3500 | 6000/4000 | 14 turbo, 6 H dual turbo, 5 ion, 2 H dual ion, tractor | 14 M turbo, 6 H dual turbo, 5 M ion, 2 H ion |
| Mandator | 9000/8000 | 50000/50000 | 8 quad, 8 dual, 4 H turbo, 14 PD, 8 ion, 8 conc, 4 proton (54) | 56, all Long range class |
| Rothana | 10300/3000 | 12000/6000 | 8 DBY, 4 laser, 2 turbo, 2 ion, 2 PD, 1 proton | 8 DBY, 4 M laser, 2 M turbo, 2 M ion, 2 M PD |
| Thranta, Venator II | — | 800/900, 6000/5000 | not in catalogue | keep, scale to bands |

## CIS (Separatist): original vs live

| Ship | Orig HP/Sh | Live HP/Sh | Orig weapons | Live weapons |
|---|---|---|---|---|
| Patrol Frigate | 900/700 | 900/700 | 8 laser | 8 L laser, 2 PD |
| C-9979 | 300/20 (transport) | 500/600 | direct weapon | 4 L laser, 2 PD |
| Munificent | 2500/1200 | 2500/2000 | 2 H turbo, 8 dual turbo, 2 ion, 4 turbo, 4 PD | 4 M dual turbo, 2 L torp, 2 M turbo, 2 PD |
| Captor | 3400/800 | 3400/800 | 6 laser, 2 turbo, 2 ion | match |
| Dispatcher | 3500/1000 | 3500/1000 | 3 ion, 7 turbo, 4 laser, 4 PD, 4 dual turbo | 7 L turbo, 4 M dual turbo, 2 laser, 3 ion, 4 PD |
| Recusant | 3800/2600 | 3800/2600 | 8 laser, 4 PD, 7 turbo, 1 H turbo | 10 H dual turbo |
| Providence | 5000/3000 | 6000/5000 | 12 turbo, 6 quad, 10 laser, 2 conc, 2 proton | 6 H dual, 2 M turbo, 4 M dual, 2 H ion |
| Invincible | 7000/5000 | 15000/8000 | 11 turbo, 6 quad, 10 laser, 4 tracking torp | 12 H dual, 4 M turbo, 8 M dual, 4 H ion |
| Lucrehulk | 8100/6600 | 12000/6600 | 8 quad, 4 ion, 6 PD, 4 laser, 2 conc | 2 H turbo, 6 H dual, 4 M dual, 2 H ion |
| Malevolence | 33420/15000 | 33420/15000 | 36 turbo, 20 laser, 8 mass driver, 2 ion pulse | 34 M turbo, 8 mass driver, 20 M laser |

## Original class bands (Republic + CIS)

- Corvette: 550–900 HP, 500–900 shields, 6–8 guns.
- Frigate: 2000–3500 HP, 800–1400 shields, 10–20 guns.
- Cruiser: 3600–4900 HP, 2000–3100 shields.
- Capital: 5000–7500 HP, 3000–5000 shields.
- Heavy capital: 8000–10300 HP, 2500–8000 shields. Super: Malevolence 33420/15000.
- Original Mandator (9000/8000) is a heavy capital, not a 50000/50000 super.

## Range class: what changes on removal

Rule today (`WeaponsData.rangeClasses`): Close ×0.7 range ×1.25 dmg; Long ×1.5 range ×1.5 dmg ×2 reload ×1.2 accuracy → Long = −25 % DPS. Removing Long: range −33 %, DPS +33 %, accuracy −17 %.

| Unit | Long mounts | Effect |
|---|---|---|
| Mandator | 56/56 | H turbo 877→585, missile 1072→715, PD 345→230. Nav radius 483 → guns reach ~100 past own hull from centre |
| Executor | 120/174 | Nav radius 1059 already > every gun range; standoff = contact distance |
| Titan | 12/24 | radius 266, quad 877→585 |
| Allegiance | 6/21 | quad 877→585 |
| Procursator | 3/15 | quad 877→585 |
| Tector | 2/25 | minor |
| ISD III | 2/31 | ion 975→650; radar 975 can drop |
| Victory I | 4/16 | H missile 1072→715; radar 1075 can drop |
| Arquitens Imperial | 4/8 | M turbo 776→517; radar 675 |
| MC75 Profundity | 12 Close | L turbo 315→450, −20 % DPS |

## Engagement-distance mechanics (live code)

- Hardpoint fire check origin = hardpoint world position (`TargetSelectionBatch` → `WeaponTargetSelector.TryCalculateAim(..., MaxAttackDistance)`).
- Ship standoff = centre-to-centre: `ShipEngagement.GetStandoffPosition` → `max(engagementRange × STANDOFF_RANGE_FACTOR, ContactDistance)`; `ContactDistance` = own + target `NavigationRadius`.
- `WeaponComponent.HasEnoughRange` compares centre distance with engagement range.
- Result: for ships whose radius ≈ gun range, the ship parks at contact distance and far-side hardpoints never reach. Long range class hid this for Mandator; Executor already shows it.
- Largest radii: Executor 1059, Mandator 483, Malevolence 287, Titan 266, Lucrehulk 230, MC80 218, Home One 199, Invincible 162.

## Weapon definitions that become duplicates

- Same family/caliber/mount, only range class differs: 27→1, 39→4, 49→2, 50→3, 53→5, 44→11, 34→65.
- Mandator unit-scoped: 73≈2, 74≈3, 75≈66, 76≈11, 77≈65, 78≈80; quads 20/87/88 (Mandator/Procursator/Titan) identical.
- No standard twin (rename only): 51 Heavy Dual Ion, 67 Heavy Triple Turbolaser, 68 Heavy Triple Ion.
- Referenced by `ShipSfxData` overrides: 20, 27, 39, 44, 49, 50, 51, 67, 68, 73, 87 → remap or merge with the type.

## Code touching range class

- `WeaponRangeClass.cs`, `WeaponRangeClassRule.cs`, `WeaponDefinition.rangeClass`, `WeaponsData.rangeClasses` + `GetRangeClass`, `WeaponProfileFactory.Create` param.
- `WeaponProfile.AccuracyMultiplier` — only source is the range class; drop or keep at 1.
- `Editor/Balance/BalanceWeaponAdapter.cs` (`RegisterRangeClasses`, `rangeClass` field).
- Tests: `WeaponTaxonomyTests` (range-class rule + `RangeClasses_TradeReachForDamage`), `BoltParticleSimulationTests`, `ShotEffectPoolLifecycleTests`.
- Variant `rangeMultiplier` on `WeaponDefinition` stays (Imperial heavy dual ×1.15, Composite Beam ×0.625).

## Baseline 2026-10-25

- Revision `2f45f1371`. 56 ships, 260 same-class cross-faction 1v1 duels × 10 seeds. Raw duels, not price-normalised. Data: `Logs/Rebalance/2026-10-25-baseline/`.

### Duel win rate by class and faction

| Class | Empire | Rebellion | Republic | Separatist |
|---|---|---|---|---|
| Corvette | 29 % | 71 % | 56 % | 43 % |
| Frigate | 28 % | 50 % | 49 % | 67 % |
| Cruiser | 21 % | 80 % | 21 % | 75 % |
| Capital | 59 % | 51 % | 37 % | 56 % |
| HeavyCapital | 67 % | — | 80 % | 12 % |

### Outliers (estimator DPS)

- ISD II 1610 and Tector 1538 DPS at 5000–6000 price vs capital median ~460 (ISD II octuple/quad batteries).
- Home One 30000/40000 at 380 DPS; MC80 30000/40000 at 735 DPS.
- Republic frigates low DPS: Acclamator 99, Pelta 116 (Separatist Dispatcher 322).
- Heavy Dreadnought 144 DPS vs cruiser median ~330.
- Mandator 50000/50000, 1285 DPS; Lucrehulk 12000/6600, 414 DPS → Separatist HeavyCapital 12 %.
- Executor 200000/200000, 3745 DPS (excluded, stays overpowered).

### Executor per-gun baseline (must survive Phase 1)

| Type | Count | Damage | Shots | Reload | Range | Accuracy × | DPS |
|---|---|---|---|---|---|---|---|
| HeavyLaser | 40 | 9.375 | 3 | 1.5 | 390 | 1 | 18.75 |
| HeavyArtilleryRocket (34) | 10 | 93.75 | 4 | 20 | 1072.5 | 1.2 | 18.75 |
| HeavyLongRangeTurbolaser (49) | 10 | 37.5 | 3 | 8 | 877.5 | 1.2 | 14.06 |
| HeavyLongRangeDualTurbolaser (50) | 23 | 75 | 3 | 8 | 877.5 | 1.2 | 28.13 |
| HeavyLongRangeDualIonCannon (51) | 8 | 140.625 | 2 | 12 | 975 | 1.2 | 23.44 |
| MediumLongRangeDualTurbolaser (53) | 69 | 60 | 3 | 8 | 776.25 | 1.2 | 22.5 |
| MediumPointDefense | 14 | 5 | 8 | 2 | 230 | 1 | 20 |
