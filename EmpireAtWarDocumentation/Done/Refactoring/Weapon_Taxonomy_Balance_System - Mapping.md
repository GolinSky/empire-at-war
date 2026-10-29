---
category: Refactoring
status: todo
created: 2026-10-10
---
# Weapon Taxonomy - Mapping

Phase 1 of [[Done/Refactoring/Weapon_Taxonomy_Balance_System|Weapon Taxonomy and Balance System]]. Classification only; stats come next.

- Profiles: 66 → archetypes: 58 (8 merges).
- `HP` = hardpoints in ShipData/SquadronData loadouts + platform prefabs. `DPS` = current raw DPS per hardpoint (after `c9fdcf9a`).
- Rules: no size in name → Light; turbo-ion → Ion of the same size; damage type from family.
- Burst = N shots with a short gap, then reload. Every "N-Burst" weapon and the Victory I rockets. Gaps shortened in `ef5f6c55` (0.25 s energy, 0.4 s missiles); DPS unchanged.

## Classification Decisions (user, 2026-10-10)

- Mandator `QuadTurboLaser`, ISD II `ISDIIOctupleTurbolaser`, ISD II `ISDIIQuadIonCannon` → **Heavy**.
- Rockets are missiles; one Missile family.
- Ion torpedo is its own family (Ion damage); used by MC75 Profundity (2 hardpoints). Y-Wing Ion Shot is a separate squadron ability.
- `LaserBeam` is a Beam-family hardpoint on Venator and Imperial Venator; its profile is also read by the Laser Beam ability (`ProtonBeamAbility`). `CompositeBeam` is the ISD III ability only.

## Implemented Rules (`daeae113`)

Stored in `WeaponsData.asset`; edited in the balance editor. Base = Light, Single, Standard.

| Family | Damage type | Shot dmg | Shots | Reload s | Range | DPS |
|---|---|---|---|---|---|---|
| Laser | Laser | 6 | 3 | 1.5 | 300 | 12 |
| Turbolaser | Turbolaser | 16 | 3 | 4 | 450 | 12 |
| Ion | Ion | 30 | 2 | 6 | 500 | 10 |
| Mass driver | MassDriver | 35 | 3 | 5 | 450 | 21 |
| Missile | ConcussionMissile | 40 | 4 | 10 | 550 | 16 |
| Torpedo | ProtonTorpedo | 90 | 2 | 12 | 350 | 15 |
| Ion torpedo | Ion | 64 | 1 | 15 | 350 | 4.3 |
| Point defense | PointDefense | 4 | 8 | 2 | 200 | 16 |
| Beam | Beam | 640 | 1 | 30 | 460 | 21 |
| Fighter laser | FighterLaser | 5 | 2 | 0.9 | 70 | 11.1 |
| Fighter ion | Ion | 1.3 | 2 | 4.91 | 70 | 0.5 |
| Fighter missile | ConcussionMissile | 40 | 1 | 15 | 80 | 2.7 |
| Fighter torpedo | ProtonTorpedo | 90 | 1 | 8 | 60 | 11.3 |

- Caliber: Light ×1 dmg / ×1 range; Medium ×1.25 / ×1.15; Heavy ×1.5625 / ×1.3.
- Caliber target-size bias (ship families only): Heavy damage ×0.3 vs strikecraft → ×1.35 vs heavy capital; Heavy accuracy ×0.2 vs strikecraft.
- Range class: Close range ×0.7, dmg ×1.25; Long range ×1.5, dmg ×1.5, reload ×2, accuracy ×1.2 → DPS ×0.75.
- Mount: shot dmg × barrels (1/2/3/4/8), same shots; Dual+ uses `DualProjectile`.
- Burst: N fast shots, reload scaled so DPS equals Standard.
- Faction bolt colors (laser, turbolaser, point defense, fighter laser): Republic orange-red, Separatist red, Rebellion red, Empire green.

## Per-Unit DPS Change (raw, before → after `daeae113`)

| Unit | Weapon HPs | DPS before | DPS after | Change | Longest gun |
|---|---|---|---|---|---|
| Executor | 174 | 3,357 | 3,689 | +10% | 1,072 |
| ISDII | 24 | 504 | 1,514 | +200% | 650 |
| Mandator | 56 | 1,159 | 1,439 | +24% | 585 |
| Malevolence | 62 | 918 | 816 | -11% | 450 |
| ISDIII | 31 | 630 | 733 | +16% | 975 |
| MC80Independence | 36 | 520 | 696 | +34% | 575 |
| Tector | 25 | 526 | 603 | +15% | 776 |
| ImperialVenator | 15 | 279 | 515 | +84% | 673 |
| Imperator | 27 | 510 | 474 | -7% | 650 |
| MC75Profundity | 30 | 418 | 474 | +13% | 402 |
| Venator | 15 | 433 | 440 | +1% | 650 |
| Lucrehulk | 14 | 408 | 414 | +2% | 650 |
| Providence | 14 | 400 | 400 | +0% | 650 |
| Rothana | 18 | 400 | 394 | -2% | 585 |
| Resolute | 15 | 393 | 393 | +0% | 585 |
| HomeOne | 16 | 370 | 358 | -3% | 585 |
| ISDI | 24 | 346 | 346 | +0% | 650 |
| VictoryII | 18 | 424 | 328 | -23% | 575 |
| VictoryI | 16 | 272 | 326 | +20% | 1,072 |
| Dispatcher | 20 | 343 | 322 | -6% | 518 |
| CorellianBattlecruiser | 17 | 380 | 280 | -26% | 715 |
| Victory | 11 | 274 | 274 | +0% | 585 |
| MonCalCruiser | 12 | 275 | 269 | -2% | 585 |
| Arquitens | 9 | 231 | 231 | +0% | 518 |
| Munificent | 10 | 206 | 206 | +0% | 518 |
| RaiderCorvette | 10 | 88 | 193 | +119% | 715 |
| Recusant | 10 | 172 | 170 | -2% | 585 |
| HeavyDreadnought | 6 | 138 | 144 | +5% | 650 |
| Captor | 10 | 122 | 116 | -5% | 500 |
| AcclamatorAssault | 6 | 91 | 107 | +18% | 550 |
| Acclamator | 7 | 99 | 99 | +0% | 518 |
| Thranta | 8 | 98 | 98 | +0% | 550 |
| CorellianCorvette | 8 | 96 | 96 | +0% | 300 |
| NebulonB | 8 | 108 | 96 | -11% | 450 |
| PatrolFrigate | 8 | 96 | 96 | +0% | 300 |
| ArquitensImperialCruiser | 8 | 103 | 84 | -19% | 675 |
| C9979 | 4 | 60 | 60 | +0% | 345 |
| StealthCorvette | 4 | 56 | 56 | +0% | 550 |
| TIEInterceptor (sq.) | 32 | 356 | 356 | -0% | 70 |
| YWingBomber (sq.) | 24 | 140 | 312 | +123% | 91 |
| Vulture (sq.) | 28 | 311 | 311 | -0% | 70 |
| ARC170 (sq.) | 20 | 218 | 285 | +31% | 91 |
| VWing (sq.) | 20 | 222 | 222 | -0% | 70 |
| XWing (sq.) | 20 | 67 | 222 | +233% | 70 |
| TIEBrute (sq.) | 12 | 128 | 208 | +63% | 91 |
| YWing (sq.) | 15 | 186 | 167 | -10% | 70 |
| TIEAvenger (sq.) | 24 | 145 | 165 | +14% | 80 |
| TIEPunisher (sq.) | 20 | 262 | 156 | -41% | 80 |
| TIEFighter (sq.) | 14 | 156 | 156 | -0% | 70 |
| DroidBomber (sq.) | 12 | 134 | 134 | -0% | 70 |
| NTB630 (sq.) | 12 | 134 | 134 | -0% | 70 |
| V19Torrent (sq.) | 10 | 111 | 111 | -0% | 70 |
| TIEBomber (sq.) | 8 | 89 | 89 | -0% | 70 |
| AWing (sq.) | 6 | 67 | 67 | -0% | 70 |
| Delta7 (sq.) | 5 | 56 | 56 | -0% | 70 |
| Belbullab22 (sq.) | 4 | 44 | 44 | -0% | 70 |

## Merges

Implemented in `daeae113`: 7 removed types → kept type (hardpoints remapped, loadouts rebaked).

| Kept type | Removed types |
|---|---|
| `TurboLaser` (Light Turbolaser) | `LightTurbolaser` (33) |
| `IonCannon` (Light Ion) | `LightTurboIon` (57) |
| `HeavyIonCannon` | `HeavyTurboIon` (31) |
| `BarrageRocket` (Light 6-Burst Missile) | `AssaultMissile` (63) |
| `HeavyBurstBarrageRocket` (Heavy 2-Burst Missile) | `HeavyBurstConcussionMissile` (61) |
| `HeavyArtilleryRocket` (Heavy Long-Range 8-Burst Missile) | `HeavyBurstArtilleryRocket` (52) |
| `MediumProtonTorpedo` | `MediumLargeProtonTorpedo` (25) |

- Not merged: `HeavyDualTurbolaser` became the Empire faction variant (ISD III, Imperial Venator; ×1.1 damage, ×1.15 range); `DualHeavyTurboLaser` stays shared.
- Turbo-ion types renamed to ion cannons (`MediumIonCannon`, `MediumDualIonCannon`, `MC80MediumDualIonCannon`, `…LongRange…IonCannon`); ids unchanged.
- Squadrons moved off ship weapons: Y-Wing `ProtonTorpedo` → `FighterProtonTorpedo`; TIE Punisher `ConcussionMissile` → `FighterConcussionMissile`; Y-Wing Bomber `MediumProtonTorpedo` → new `MediumFighterProtonTorpedo` (72).

## Ship

### Laser

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `DualLaser` | Light | Dual | Standard | Standard | Shared | 4 | 24.0 |  |
| `BurstLaserCannon` | Light | Single | Burst | Standard | Shared | 8 | 12.0 | 2-burst |
| `Laser` | Light | Single | Standard | Standard | Shared | 87 | 12.0 |  |
| `MediumLaser` | Medium | Single | Standard | Standard | Shared | 4 | 15.0 |  |
| `HeavyDualLaser` | Heavy | Dual | Standard | Standard | Shared | 6 | 37.5 |  |
| `HeavyLaser` | Heavy | Single | Standard | Standard | Shared | 76 | 18.8 |  |

### Turbolaser

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `LightRapidDualTurbolaser` | Light | Dual | Burst | Long | Shared | 0 | 31.4 | 4-burst; unused |
| `LightDualTurbolaser` | Light | Dual | Standard | Standard | Shared | 20 | 24.0 |  |
| `LightCloseRangeTurbolaser` | Light | Single | Standard | Close | Shared | 12 | 14.4 |  |
| `LightLongRangeTurbolaser` | Light | Single | Burst | Long | Shared | 4 | 13.8 | 2-burst |
| `LightTurbolaser` | Light | Single | Standard | Standard | Shared | 20 | 12.0 |  |
| `TurboLaser` | Light | Single | Standard | Standard | Shared | 72 | 15.0 | no size → Light |
| `MediumLongRangeDualTurbolaser` | Medium | Dual | Burst | Long | Shared | 77 | 19.3 | 2-burst |
| `DualMediumTurboLaser` | Medium | Dual | Standard | Standard | Shared | 38 | 30.0 |  |
| `MC80MediumDualTurbolaser` | Medium | Dual | Standard | Standard | Ship: MC80 | 8 | 15.7 |  |
| `MediumBurstTurbolaser` | Medium | Single | Burst | Standard | Shared | 18 | 31.1 | 3-burst |
| `MediumTurboLaser` | Medium | Single | Standard | Standard | Shared | 30 | 15.0 |  |
| `HeavyLongRangeDualTurbolaser` | Heavy | Dual | Burst | Long | Shared | 35 | 30.0 | 2-burst |
| `DualHeavyTurboLaser` | Heavy | Dual | Standard | Standard | Shared | 31 | 37.5 |  |
| `DualHeavyTurboLaserDby827` | Heavy | Dual | Standard | Standard | Faction: Republic | 26 | 33.8 | DBY-827 |
| `HeavyDualTurbolaser` | Heavy | Dual | Standard | Standard | Shared | 28 | 18.3 | same name as DualHeavyTurboLaser |
| `ISDIIOctupleTurbolaser` | Heavy | Octuple | Standard | Standard | Ship: ISD II | 8 | 30.0 | user: Heavy |
| `QuadTurboLaser` | Heavy | Quad | Standard | Standard | Ship: Mandator | 8 | 40.0 | user: Heavy |
| `HeavyLongRangeBurstTurbolaser` | Heavy | Single | Burst | Long | Shared | 10 | 20.0 | 2-burst |
| `HeavyTurboLaser` | Heavy | Single | Standard | Standard | Shared | 24 | 18.8 |  |
| `HeavyLongRangeTripleTurbolaser` | Heavy | Triple | Burst | Long | Shared | 3 | 29.5 | 3-burst |

### Ion

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `IonCannon` | Light | Single | Standard | Standard | Shared | 34 | 10.0 | no size → Light |
| `LightTurboIon` | Light | Single | Standard | Standard | Shared | 6 | 10.0 | turbo-ion → ion |
| `MC80MediumDualTurboIon` | Medium | Dual | Standard | Standard | Ship: MC80 | 6 | 14.6 | same name as MediumDualTurboIon |
| `MediumDualTurboIon` | Medium | Dual | Standard | Standard | Shared | 4 | 25.0 | turbo-ion → ion |
| `MediumLongRangeTurboIon` | Medium | Single | Burst | Long | Shared | 0 | 23.4 | 2-burst; unused; turbo-ion → ion |
| `MediumTurboIon` | Medium | Single | Standard | Standard | Shared | 8 | 12.5 | turbo-ion → ion |
| `HeavyLongRangeDualTurboIon` | Heavy | Dual | Burst | Long | Shared | 8 | 27.8 | 2-burst; turbo-ion → ion |
| `ISDIIQuadIonCannon` | Heavy | Quad | Standard | Standard | Ship: ISD II | 2 | 37.5 | user: Heavy |
| `HeavyLongRangeBurstTurboIon` | Heavy | Single | Burst | Long | Shared | 2 | 34.3 | 2-burst; turbo-ion → ion |
| `HeavyBurstIonBlaster` | Heavy | Single | Burst | Standard | Ship: Raider | 2 | 4.9 | 2-burst |
| `HeavyIonCannon` | Heavy | Single | Standard | Standard | Shared | 12 | 12.5 |  |
| `HeavyTurboIon` | Heavy | Single | Standard | Standard | Shared | 2 | 15.6 | turbo-ion → ion |
| `HeavyLongRangeTripleTurboIon` | Heavy | Triple | Burst | Long | Shared | 3 | 27.4 | 3-burst; turbo-ion → ion |

### Mass driver

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `MassDriver` | Light | Single | Standard | Standard | Shared | 8 | 21.0 | no size → Light |

### Missile

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `AssaultMissile` | Light | Single | Burst | Standard | Shared | 2 | 8.0 | 4-burst |
| `BarrageRocket` | Light | Single | Burst | Standard | Shared | 2 | 16.0 | 6-shot rocket burst (Victory I) |
| `ConcussionMissile` | Light | Single | Standard | Standard | Shared | 21 | 16.0 |  |
| `HeavyArtilleryRocket` | Heavy | Single | Burst | Long | Shared | 4 | 5.3 | 4-shot rocket burst (Victory I) |
| `HeavyBurstArtilleryRocket` | Heavy | Single | Burst | Long | Shared | 10 | 7.6 | 8-burst rocket |
| `HeavyAssaultMissile` | Heavy | Single | Standard | Standard | Shared | 4 | 10.0 |  |
| `HeavyBurstBarrageRocket` | Heavy | Single | Burst | Standard | Shared | 4 | 50.0 | 2-burst rocket; 50 DPS outlier |
| `HeavyBurstConcussionMissile` | Heavy | Single | Burst | Standard | Shared | 2 | 6.7 | 5-burst |

### Torpedo

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `ProtonTorpedo` | Light | Single | Standard | Standard | Shared | 14 | 15.0 |  |
| `MediumLargeProtonTorpedo` | Medium | Single | Standard | Standard | Shared | 6 | 10.7 | "Med Lrg" duplicate of Medium |
| `MediumProtonTorpedo` | Medium | Single | Standard | Standard | Shared | 12 | 4.5 |  |
| `HeavyProtonTorpedo` | Heavy | Single | Standard | Standard | Shared | 12 | 15.0 |  |

### Ion torpedo

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `MediumIonTorpedo` | Medium | Single | Standard | Standard | Shared | 2 | 5.3 | own family; MC75 Profundity |

### Point defense

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `DualRepeatingPointDefense` | Light | Dual | Burst | Close | Shared | 2 | 8.6 |  |
| `PointDefense` | Light | Single | Standard | Close | Shared | 30 | 16.0 |  |
| `RepeatingPointDefense` | Light | Single | Burst | Close | Shared | 14 | 6.0 |  |

### Beam

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `LaserBeam` | Heavy | Single | Standard | Standard | Shared | 2 | 33.3 | Venator + Imperial Venator hardpoint; profile also used by LaserBeam ability |
| `CompositeBeam` | — | — | — | — | Outside | 0 | 88.9 | ISD III ability |

## Squadron

### Fighter laser

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `LightDualLaser` | Light | Dual | Standard | Standard | Shared | 6 | 12.7 |  |
| `FighterLaser` | Light | Single | Standard | Standard | Shared | 158 | 11.1 |  |
| `LightRepeatingLaser` | Light | Single | Burst | Standard | Shared | 12 | 9.4 |  |
| `XWingLaser` | Light | Single | Standard | Standard | Ship: X-Wing | 20 | 3.3 | 3.3 DPS vs 11.1 generic |
| `HeavyFighterLaser` | Heavy | Single | Standard | Standard | Shared | 22 | 10.7 |  |

### Fighter ion

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `HeavyDualIonStunner` | Heavy | Dual | Standard | Standard | Shared | 6 | 1.6 |  |

### Fighter missile

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `FighterConcussionMissile` | Light | Single | Standard | Standard | Shared | 12 | 2.7 |  |

### Fighter torpedo

| Weapon | Caliber | Mount | Pattern | Range | Scope | HP | DPS | Note |
|---|---|---|---|---|---|---|---|---|
| `FighterProtonTorpedo` | Light | Single | Standard | Standard | Shared | 25 | 11.2 |  |


## Class Caliber Follow-Up (2026-10-11)

- Capital / HeavyCapital minimum Medium; Recusant explicitly included despite its Cruiser class. 186 mounts upgraded on 15 ships. Corvette no Turbolaser; Frigate no Heavy Turbolaser; no existing violations in those classes.
- Existing Medium definitions reused for single/dual Turbolaser, Laser, Ion and Proton Torpedo; originals remain available to smaller ships.
- New `MediumPointDefense` (80) clones shared Light PointDefense taxonomy with Medium caliber; Rothana only current user.
- `MassDriver` (Malevolence), `RepeatingPointDefense` (Executor), `MandatorPointDefense` (Mandator) → Medium in place; no other users found. Mandator reason updated.
- All mount counts/patterns and prefab geometry preserved; saved loadouts rebaked; no radar increase needed. 68 regular definitions + 3 superweapons.
- Cruiser Heavy Turbolaser unchanged: Heavy Dreadnought 2 single + 2 dual (4/6 mounts, ~78% raw DPS); Victory 2 single + 2 dual (4/11, ~41%); Recusant 1 dual (1/10, ~22%). All three have no heavy `mainBattery` flags.
- Verification: 38 saved ships, zero class-rule violations; WeaponTaxonomyTests 25/26 passed. Existing `StrikeFighterProtonTorpedo` Shared/multiplier mismatch confirmed in the pre-change catalog; no Play Mode run.
