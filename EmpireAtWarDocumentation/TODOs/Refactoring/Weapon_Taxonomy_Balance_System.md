---
category: Refactoring
status: in-progress
created: 2026-10-10
---
# Weapon Taxonomy and Balance System

## Goal

- Weapon stats derive from rules (family × caliber × range class × mount × pattern × variant), not 66 hand-tuned profiles.
- Shared, faction-unique and unit-unique weapons are explicit variants with a written reason.
- Each hardpoint fires to its own range; ships pick an engagement distance per target class.

## Decisions (user, 2026-10-10)

- Laser and Turbolaser are two families (anti-small vs anti-capital).
- Turbo-ion → Ion of the same size. No size in the name → Light.
- Mount = barrels: Dual 2×, Triple 3×, Quad 4×, Octuple 8× single shot damage; same shots; no cap.
- Single ship laser/turbolaser salvo = 3 shots. Burst = N fast shots, then reload.
- Range class level kept: Close / Standard / Long.
- Squadron weapons: separate tree, same rules.
- Damage type from family only; Ion torpedo is its own family.
- Caliber shifts damage and accuracy towards big or small targets; `HeavyTurbolaser` damage type removed.
- Per-gun range; local ship AI picks distance per target class; global AI only picks targets.
- Radar shorter than a gun → raise radar (known risk, guarded by a test).
- Mandator Quad, ISD II Octuple, ISD II Quad Ion → Heavy. Rockets are missiles.

## Implementation (`daeae113`)

- `WeaponsData`: `families`, `calibers`, `rangeClasses`, `factionColors`, `weapons` (`WeaponDefinition`). Profiles built on load by `WeaponProfileFactory`; nothing derived is stored.
- `WeaponProfile` gains family, target-size bias, accuracy multiplier, faction-color flag, `DamagePerSecond`, `CycleSeconds`.
- `WeaponComponent`: hardpoint range = profile range × vision multiplier; `RollHit` × caliber accuracy bias × range-class accuracy; damage × caliber damage bias; owner faction bolt color via `IPlayerRoster`.
- `WeaponEngagementRange`: longest range at which ≥ 60 % of effective DPS (DPS × hit chance × matrix × bias) vs the target class can fire. `AttackTargetState` / `ShipEngagement` use it per target class.
- Removed per-unit `WeaponRange` (`IWeaponRangeData`, `ShipData`, `SquadronData`, `EntityComponentData`).
- Sounds: `ShipSfxData.weaponFamilies` + per-weapon overrides (20 kept).
- Balance editor: edits family/caliber/range-class rules and per-weapon taxonomy/variant fields; `BalanceWeaponPreview` derives draft DPS in memory.
- Rules, merges and per-unit DPS change: [[TODOs/Refactoring/Weapon_Taxonomy_Balance_System - Mapping|Mapping]].

## Data Changes

- 7 weapon types merged, turbo-ion types renamed, `MediumFighterProtonTorpedo` (72) added.
- Squadrons moved off ship weapons: Y-Wing, TIE Punisher, Y-Wing Bomber.
- Radar raised to cover the longest gun: Arquitens Imperial Cruiser 675, Corellian Battlecruiser 725, Heavy Dreadnought 650, ISD III 975, ISD II 350→750, Raider 725, Stealth Corvette 175→550, Thranta 550, Victory I 1,075; Empress 975, Golan III 900, defend platforms 525 (radar and vision).

## Progress

- [x] Quick fixes `c9fdcf9a`, Executor SFX `3845d442`, C-9979/Raider radar `1ecfb53e`, burst gaps `ef5f6c55`.
- [x] Phase 1 mapping, base rule table and per-unit DPS deltas.
- [x] Phase 2 rule data + factory + validation tests (`WeaponTaxonomyTests`, 17 cases).
- [x] Phase 3 merges, prefab remap, rebaked loadouts, `HeavyTurbolaser` removed.
- [x] Phase 4 per-gun range + per-target engagement range; radar ≥ longest gun test.
- [x] Phase 5 variants (DBY-827 Republic, Imperial heavy dual Empire, ISD II, MC80, Mandator, Raider, X-Wing, Composite Beam) + faction bolt colors.
- [x] Phase 6 squadron tree (fighter laser/ion/missile/torpedo, platform test).
- [x] Phase 7 balance editor rules + draft DPS preview.
- [ ] Play Mode / skirmish acceptance (user playtest).
- [ ] Balance tuning pass after playtest.

## Class Caliber Rules (2026-10-11)

- Corvette → no Turbolaser family. Frigate → no Heavy Turbolaser. Existing 38-ship audit found no violations in either class.
- Capital / HeavyCapital → minimum Medium in every weapon family, including point defense. Recusant explicitly included by user; retains its Cruiser class.
- 186 mounts upgraded across 14 Capital / HeavyCapital ships + Recusant; barrel counts, patterns, geometry and other ships' profiles preserved. No radar increases needed.
- Remaps: `TurboLaser` → `MediumTurboLaser`, `Laser` → `MediumLaser`, `IonCannon` → `MediumIonCannon`, `LightDualTurbolaser` → `DualMediumTurboLaser`, `ProtonTorpedo` → `MediumProtonTorpedo`.
- Rothana uses new shared `MediumPointDefense` (80); existing Light PointDefense preserved for Dispatcher, Munificent and MC75 Profundity.
- Only-owner definitions upgraded in place: Malevolence `MassDriver`, Executor `RepeatingPointDefense`, Mandator `MandatorPointDefense`. Catalog: 68 regular definitions + 3 superweapons.
- Cruiser heavy Turbolasers retained: Heavy Dreadnought 4/6 mounts (~78% raw DPS), Victory 4/11 (~41%), Recusant 1/10 (~22%). No `mainBattery` flags set on those heavy mounts.
- Saved class rules (4 cases) and prefab/loadout consistency pass. WeaponTaxonomyTests: 25/26 pass; baseline `StrikeFighterProtonTorpedo` multiplier variant test fails (scope Shared, damage ×0.5, reload ×1.5). No new Console/import errors; no Play Mode run.

## Verification

- 2026-10-10 full EditMode: 1,383 / 1,405 passed. All 22 failures are outside this work: ship height tiers (12), ISD I/II wrecks (4), team colors (1), helper meshes (1), ISD II hull 5,500 vs 7,000 (1), engine hardpoint NRE in `ShipMoveComponent` (2), AI production order (1).
- No Play Mode run.

## Open Questions / Risks

- **Stations outranged**: station guns reach 585, capital guns 650; before, stations fired to 750. Give stations Long or Heavy variants?
- **ISD II +200 %** raw DPS (octuple/quad batteries now truly 8×/4×).
- **X-Wing laser** normalized to the shared fighter laser (3.3 → 11.1 DPS per hardpoint).
- **Y-Wing (BTL-B)** torpedo salvo is now 1 shot (fighter torpedo) instead of 2.
- **LaserBeam profile shared** by Venator hardpoints and the Laser Beam ability; give the ability its own settings.
- **Missile range** 550 base (heavy long 1,072) forced large radar raises (Victory I 1,075). Lower if detection feels too far.
- **Burst alpha** (`HeavyBurstBarrageRocket`, now shield-piercing) — check in playtest.

## Files

- `Assets/Scripts/Components/AttackComponent/` — `WeaponsData`, `WeaponDefinition`, `WeaponFamilyRule`, `WeaponCaliberRule`, `WeaponRangeClassRule`, `WeaponProfileFactory`, `WeaponProfile`, enums.
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`, `WeaponModel.cs`, `WeaponEngagementRange.cs`
- `Assets/Scripts/Entities/Ship/StateMachine/AttackTargetState.cs`, `ShipEngagement.cs`
- `Assets/Scripts/Services/Audio/ShipSfxData.cs`, `WeaponFamilyAudioProfile.cs`
- `Assets/Scripts/Editor/Balance/BalanceWeaponAdapter.cs`, `BalanceWeaponPreview.cs`, `BalanceDpsEstimator.cs`
- `Assets/Scripts/Tests/Editor/WeaponTaxonomyTests.cs`
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`, `DamageMatrixData.asset`; `Assets/Settings/Data/Models/Audio/ShipSfxData.asset`
