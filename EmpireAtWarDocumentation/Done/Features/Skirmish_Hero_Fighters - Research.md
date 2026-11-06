---
type: research
category: Features
updated: 2026-11-06
---
# Skirmish hero fighters — balance research

## Decision

- Three independent implementation plans: [[Done/Features/Jango_Fett_Slave_I]], [[Done/Features/Boba_Fett_Slave_I]], [[Done/Features/Han_Solo_Millennium_Falcon]]. Shared bomb ability for both Fetts: [[Done/Features/Harmonic_Bomb_Ability]]. All three hero plans and their shared research are archived in `Done/Features`.
- One independently purchased fighter-class squadron with one physical member per hero. Provisional shop values: station level 2, two population, one reserved copy per player, replaceable after destruction. Grant access through each faction’s skirmish shop.
- Keep existing squadron stats, weapon families and damage matrix unchanged. Apply buffs/debuffs to the heroes. Source stats establish identity. Project targets use project units.

## Original tactical data

| Installed XML field | Jango / RaW | Boba / vanilla | Han / vanilla |
|---|---:|---:|---:|
| Hull / shields | 3000 / 150 | 1000 / 150 | 800 / 300 |
| Shield refresh | 15 | 15 | 10 |
| Min / max speed | 3.5 / 5.5 | 3.5 / 5.5 | 3.5 / 6 |
| Turn / thrust / roll / bank | 5 / 1 / 5.5 / 60° | 5 / 1 / 5.5 / 60° | 2 / 0.1 / 5 / 50° |
| Damage per pulse × pulses | 12 × 8 | 12 × 8 | 16 × 8 |
| Pulse delay / recharge (s) | 0.15 / 0 | 0.15 / 0 | 0.2 / 0.5 |
| Weapon range / reveal | 800 / 1000 | 800 / 1000 | 1000 / 1000 |
| Skirmish credits / build seconds | 1500 / 25 | 1700 / 28 | 1800 / 33 |

- Both Fetts: harmonic bomb, 1.5 s fuse, 20 s recharge, 300 blast damage, 600 source-unit radius, three-tier falloff. RaW supports autofire. Han: 6 s invulnerability, 60 s recharge. Falcon XML has duplicate turret fields; inspect model and test coverage before assigning an arc.
- Vanilla root: `D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/`. `patch2.meg → DATA/XML/UNIQUEUNITS.XML`, `Millennium_Falcon` (~919), `Slave_I` (~1156). `config.meg → DATA/XML/HEROCOMPANIES.XML` (~478, ~974), `STARBASES.XML`, `PROJECTILES.XML` (~1893). The two ALOs are under `models.meg → DATA/ART/MODELS/`.
- RaW root: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/`. `XML/Units_Hero_Cis_Jango.xml` → `Jango_Fett_Team` / tactical `Slave_I` (~301). `XML/Spacebuildables_Battlepads.xml` (~939–945) recruits that team. Its price is 1500/25 s. The separate 1337/28 s MP entry is unused by this relay. Bomb: `XML/Projectiles.xml` (~1919). Paint: `Art/Textures/Ev_slave1.dds`.

## Current project benchmarks

| Faction / intact squadron | Members × hull/shield | Total HP | Cruise / combat (u/s) | Accel (u/s²) | Turn (°/s) | Raw gun DPS |
|---|---|---:|---:|---:|---:|---:|
| CIS Vulture | 7 × 70/0 | 490 | 58 / 66 | 27 | 100 | 311 |
| CIS Belbullab-22 | 4 × 150/70 | 880 | 58 / 66 | 27 | 100 | 200 |
| Empire TIE Fighter | 7 × 50/0 | 350 | 51 / 58 | 30 | 120 | 156 |
| Empire Interceptor | 8 × 25/0 | 200 | 67 / 76 | 30 | 120 | 356 |
| Empire Avenger | 6 × 40/12 | 312 | 92 / 92 | 30 | 120 | — |
| Rebellion X-wing | 5 × 60/40 | 500 | 46 / 52 | 22 | 110 | 222 |
| Rebellion A-wing | 6 × 130/40 | 1020 | 78 / 91 | 45 | 155 | 267 |

- Sources: `Assets/Settings/Data/Squadron/*SquadronData.asset`, `Assets/Settings/Data/Models/Weapon/{WeaponsData,DamageMatrixData}.asset`, `WeaponProfileFactory`, `UnitCombatProfileFactory`; shop values in `Assets/Settings/Data/Factions/`. Current roster maxima across 18 profiles: cruise/combat 92, acceleration 45, turn 155.
- Nominal HP = members × (hull + shields). One surviving hero retains full gun output until death. Real survival depends on shield suppression, damage type, lost wingmen, hit rate and regeneration.
- Raw laser DPS = sum of independently firing prefab guns at sustained cadence. Example: a light gun at 5 × 2 / 0.9 ≈ 11.11; 28 Vulture guns ≈ 311. Excludes aim time, firing arc, travel, damage matrix, missiles and specials. Fighter laser hull modifiers include 1.5× against fighters, 0.08× against frigates and 0.03× against capitals.

## Proposed targets

| One fighter | Hull / shields | Cruise / combat | Accel | Turn | Combat turn radius | Raw gun DPS |
|---|---:|---:|---:|---:|---:|---:|
| Jango | 1000 / 400 | 100 / 110 | 54 | 215°/s | 29.3 u | 400 |
| Boba | 820 / 480 | 103 / 112 | 52 | 240°/s | 26.7 u | 425 |
| Han | 710 / 740 | 106 / 116 | 58 | 225°/s | 29.5 u | 375 |

- Combat turning radius ≈ combat speed ÷ turn rate in rad/s. Vulture/Belbullab ≈ 37.8 u, TIE Interceptor ≈ 36.3 u, A-wing ≈ 33.6 u. Navigation leader uses 60% of the data turn rate. Verify both order response and combat reacquisition.
- Strong point: one hero can beat a fresh full conventional wing. Counterplay: concentrated anti-fighter fire, simultaneous threats, shield suppression and modest capital damage. No fleet-wide passive buff. Use ordinary fighter laser multipliers. Keep damage, range and fire cadence local to each hero profile.
- Tune basic guns with specials off first. If the hero misses too many passes, adjust break/extend geometry and firing arc before increasing raw DPS. If a hero erases two wings reliably, reduce special coverage or gun damage before reducing agility.

- Boba's verified no-ability baseline at 100 units, 10 seeds per side: 20/20 against either single Imperial reference wing, 0/20 against two Interceptor wings and 5/20 against two Avenger wings. Stationary project simulator; live pursuit and specials remain separate gates. Full results → [[Done/Features/Boba_Fett_Slave_I]].

## Verification

1. Use stock unmodified reference wings, equal starting distance, no research, hazards, station fire or auras. Swap spawn sides, use deterministic seeds where supported, record remaining HP, time to kill, fired/hit counts and time to reacquire after overshoot.
2. First-pass acceptance target per hero: win ≥8/10 trials versus one fresh reference wing with manual specials off. Repeat versus two full wings together with specials on; aim for the two wings to win ≥6/10. If the bands conflict, document matchup/cost and tune the hero locally.
3. Measure cruise acceleration, 180° reversal and combat turn radius against the faction reference. Check pursuit of a fleeing interceptor and a turn back onto a target. A paper speed increase alone cannot pass.
4. Verify mixed-fleet behavior against frigates/capitals, player/AI recruitment, hero portrait, single-member targeting, save/load and pool reuse. Simulator results that omit flight or ability behavior require an in-game check. These gates are planned, not measured.
