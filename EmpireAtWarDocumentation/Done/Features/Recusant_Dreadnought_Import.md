---
type: plan
category: Features
status: done
updated: 2026-11-16
completed: 2026-11-16
acceptance: pending-user
---
# Recusant Dreadnought Import

## Decision

- Add the larger Recusant to the CIS roster as `ShipClass.HeavyCapital`.
- Reuse `BoostWeaponPower` (ID 5). Reload and shot interval ×0.5 for 7 s, with 50 s recovery. Movement speed and shield regeneration ×0.5 while active.
- Use 2,542 m as the ship length. Republic Venator = 1,137 m / 119.383362 units → Recusant Dreadnought = 266.906 units.
- Current project Invincible = 2,011 m / 211.152084 units. The Dreadnought is 26.4% longer.
- Dreadnought = 30,000 hull / 35,000 shields. Invincible baseline = 45,000 hull / 40,000 shields, with 400 shield/s and 1 s regeneration delay.

## Weapon Mapping

Source IDs omit the shared `HP_Recusant_Dreadnought_` prefix. Reuse the 17 source weapon positions. Add 6 heavy dual turbolaser positions and 6 medium quad turbolaser positions in Unity for 29 total weapon hardpoints. The added mounts sit 3.49–8.20 units from their authored counterparts.

| Source IDs | Project weapon profile | Mounts | Base DPS |
|---|---|---:|---:|
| `Turbo_01` | `HeavyOctupleTurbolaser` | 1 | 137.280 |
| `Turbo_02`–`Turbo_07` + 6 new mounts | `DualHeavyTurboLaser` | 12 | 658.944 |
| `Turbo_08`–`Turbo_09` | `HeavyDualIonCannon` | 2 | 91.520 |
| `Laser_01`–`Laser_06` + 6 new mounts | `MediumQuadTurbolaser` | 12 | 466.560 |
| `Laser_07`–`Laser_08` | `MediumDualLaser` | 2 | 38.880 |

- Raw all-mount DPS = 1,393.184 base / 2,786.368 with Boost Weapon Power active. Includes 91.520 base ion DPS before target modifiers.
- Current Invincible = 1,422.240 base / 2,844.480 boosted. Prefab and baked loadout agree on 11 Heavy Dual Turbolaser, 4 Medium Proton Torpedo Launcher, 6 Heavy Quad Turbolaser and 5 Medium Quad Turbolaser.
- DPS uses ship mount, caliber and family modifiers from `WeaponsData.asset` and the `BalanceDpsEstimator` cycle formula. Excludes arcs, accuracy, damage matrix, research and squadrons.

## Verification

- Saved Unity hull measures 266.906342 units along +Z. Blender FBX re-import verified 59,244 intact triangle corners, all 55 imported socket transforms and four lossless staged texture maps.
- Unity asset audit verified 29 weapon IDs and matching baked loadout, 3 separate engine hardpoints, 1 hangar, shield fit, wreck meshes, icon, and CIS roster/view/placement/audio references. The Editor console returned zero errors.
- Runtime target selection accepts 26 of 29 saved mounts against a target 300 units ahead, including all 12 additions → 1,260.512 raw DPS on the bow.
- Reused Invincible movement, Heavy Capital regen and the project’s cut-up wreck. Added source-guided Vulture and Belbullab-22 bays, 3 of each with 1 active per bay. Provisional production is level 5, 18,000 credits, 75 s, 25 capacity, max 20.
- Reviewed affected Ships, Combat, Health, Balance, Navigation, Economy, Core, Audio and Abilities fixtures. Tests were not run under the local execution override.
- In-game, visual and audio checks → [[TODOs/User_Acceptance_Queue]]. No new reusable guidance emerged. The import uses existing model conversion, prefab binding, wreck and Addressables procedures.

## Files

- Fall of the Republic Workshop `1976399102`, `Data/Art/Models/Recusant-class_dreadnaught.alo`.
- Source mappings → `Data/XML/Units/CIS_Space_Units.xml`, `Data/XML/Hardpoints/HardPoints_FotR_Capitals.xml`.
- Live comparison → `Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset`, `Assets/Prefabs/Models/Ships/Separatist/View/InvincibleShipView.prefab`, `Tools/ShipScale/results.csv`.
- Profiles → `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`.
