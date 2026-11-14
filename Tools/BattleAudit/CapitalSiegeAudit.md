# Capital Siege and Forward Batteries Audit

2026-11-14. Baseline prefabs at commit `3210bfae1`. Compared 25 ship view layouts covering 26 mapped ship types.

Unity readback after saving verified 618 bound hardpoints by weapon type, hardpoint ID, main-battery flag, local yaw and parent yaw. Reach samples use positions from the imported prefabs.

## Directional coverage

Nominal DPS uses the current serialized cadence, damage × shots ÷ max(reload, burst duration). Coverage uses bound anti-ship mounts and composed parent rotations. Forward is hull +Z (0°). Main DPS compares the same weapon banks before and after flagging. Side and best values use the finished layout. Geometry figures exclude range, damage modifiers, research, flight, and hardpoint loss.

| Layout | Main mounts | Forward DPS before → after | Main forward DPS before → after | Shared main sector | Side DPS L / R | Rear DPS | Best DPS |
|---|---:|---:|---:|---|---:|---:|---:|
| Acclamator | 2 | 88.26 | 77.76 | -22 to 8° | 38.88 / 49.38 | 0 | 88.26 |
| AcclamatorAssault | 4 | 134.96 | 102.96 | -21 to 22° | 51.48 / 51.48 | 51.48 | 134.96 |
| Allegiance | 9 | 986.06 | 658.94 | -180 to 180° | 986.06 / 986.06 | 986.06 | 986.06 |
| Arquitens | 4 | 58.32 → 213.84 | 0 → 155.52 | -7 to 7° | 116.64 / 77.76 | 0 | 213.84 |
| ArquitensImperialCruiser | 4 | 228.74 → 306.5 | 77.76 → 155.52 | -6 to 6° | 116.64 / 116.64 | 0 | 306.5 |
| Centax | 1 | 40.2 | 19.2 | -50 to 50° | 57 / 57 | 0 | 76.2 |
| Executor | 33 | 5,091.16 | 1,228.5 | -180 to 180° | 5,091.16 / 5,091.16 | 5,091.16 | 5,091.16 |
| Gladiator | 4 | 250.11 | 103.68 | -13 to 14° | 51.84 / 51.84 | 0 | 250.11 |
| ISDI | 6 | 762.27 | 329.47 | -8 to 8° | 445.94 / 394.1 | 0 | 762.27 |
| ISDII | 8 | 262.44 → 1,360.68 | 0 → 1,098.24 | -10 to 10° | 789.48 / 757.08 | 0 | 1,360.68 |
| ISDIII | 10 | 942.07 | 604.03 | -14 to 15° | 532.04 / 573.98 | 0 | 942.07 |
| Imperator | 6 | 706.11 | 329.47 | -11 to 13° | 435.14 / 430.82 | 47.52 | 706.11 |
| ImperialVenator | 8 | 30.51 → 544.48 | 0 → 513.98 | -7 to 7° | 410.82 / 410.82 | 0 | 573.08 |
| InterdictorStarDestroyer | 2 | 682.5 | 164.74 | -180 to 180° | 682.5 / 682.5 | 682.5 | 682.5 |
| Mandator | 8 | 1,406.11 | 311.04 | -180 to 180° | 1,406.11 / 1,406.11 | 1,406.11 | 1,406.11 |
| Pelta | 4 | 56 | 0 | none | 60.4 / 60.4 | 0 | 79.4 |
| Procursator | 3 | 423.14 | 247.1 | -89 to 90° | 169.02 / 416.12 | 0 | 520.34 |
| Resolute / Venator II | 8 | 618.87 | 513.98 | -6 to 6° | 302.35 / 302.35 | 0 | 618.87 |
| Rothana | 8 | 506.44 → 634.94 | 385.48 → 513.98 | -10 to 10° | 308.13 / 436.63 | 0 | 704.02 |
| Tector | 8 | 286 → 944.94 | 0 → 658.94 | -12 to 12° | 744.74 / 744.74 | 28.6 | 979.26 |
| Titan | 12 | 1,258.34 | 988.42 | -180 to 180° | 1,258.34 / 1,258.34 | 1,258.34 | 1,258.34 |
| Venator | 8 | 44.68 → 558.66 | 0 → 513.98 | -7 to 7° | 332.86 / 332.86 | 0 | 558.66 |
| Victory (Republic) | 6 | 469.08 → 546.84 | 155.52 → 233.28 | -8 to 8° | 376.2 / 395.64 | 0 | 546.84 |
| VictoryI | 6 | 137.52 → 293.04 | 0 → 155.52 | -7 to 7° | 154.98 / 269.86 | 0 | 293.04 |
| VictoryII | 6 | 142.2 → 471.67 | 0 → 329.47 | -7 to 7° | 272.92 / 350.14 | 0 | 471.67 |

Victory (200, Republic) uses the prefab under the Empire view folder. Victory I (202) and Victory II (203) belong to Empire. All three use `ShipClass.Cruiser`.

Venator II (12) and Resolute (8) use the same view. ISD III (206) has a mapped view and data entry outside the faction rosters. Its Proton Turret transform belongs to the Composite Beam ability. The ten bound Imperial Heavy Dual Turbolasers form its main battery.

Executor, Titan, Allegiance, Interdictor and Mandator retain their full-circle arcs. Pelta and Centax retain their existing main battery flags and arcs.

## Executor standoff

Each sample points the attacker hull toward the target centre and rotates the target by 0°, 90°, 180° and 270°. Station hardpoint positions come from the installed `StationLevelArt` mount anchors read in Unity. Hull contact adds Executor’s 1,059.436-unit navigation radius to the station bounds offset and planar half-diagonal. Golan III has no navigation footprint. The far sample adds 250 units to the larger of the legacy approach and hull-contact distances.

| Target | Legacy stop distance | Main mounts at legacy stop | Hull contact distance | Main mounts at hull contact | Main mounts at far sample |
|---|---:|---:|---:|---:|---:|
| Empire main station (level 1) | 1,455.44 | 0 | 1,121.08 | 5–7 | 0 |
| Empire main station (level 5) | 1,455.44 | 0–2 | 1,400.52 | 0–2 | 0 |
| Rebellion main station (level 1) | 1,455.44 | 0 | 1,176.77 | 2–4 | 0 |
| Rebellion main station (level 5) | 1,455.44 | 0 | 1,298.76 | 2 | 0 |
| Republic main station (level 1) | 1,455.44 | 0 | 1,169.51 | 2 | 0 |
| Republic main station (level 5) | 1,455.44 | 0 | 1,271.1 | 2 | 0 |
| Separatist main station (level 1) | 1,320.44 | 0–2 | 1,183.88 | 2 | 0 |
| Separatist main station (level 5) | 1,320.44 | 2–6 | 1,397.05 | 0–2 | 0 |
| Golan III | 1,059.44 | 9–13 | 1,059.44 | 9–13 | 2 |

All 33 Executor main mounts have 877.5 units of range. Their longitudinal positions span −767.15 to +404.74 units relative to the pivot. Secondary bow turbolasers can still reach stations at several sampled placements where the main batteries cannot.

The implemented siege approach adds 1 unit to hull contact. Across eight target rotations and 2° hull-yaw samples, Executor's best reachable main-battery nominal DPS against the Rebel level-5 station rose from 176–219 at the legacy standoff to 307–483 at the new standoff. The new centre distance is 1,299.76 units. This sample measures geometry and nominal firepower only.

The Separatist level 5 legacy stop lies inside navigation hull clearance. The associated mount counts describe the requested point under the old formula.

## Full exports

- Local evidence under `Logs/BalanceAudit/2026-11-14/CapitalSiege/`.
- `ship_mappings.csv` records `ShipType`, class, roster, data and actual view mapping.
- `mounts.csv` records file IDs, hardpoint IDs, instance overrides, parent and composed yaw, position, range and nominal DPS.
- `coverage.csv` records eligible, forward, left, right, rear and best-bearing DPS for all anti-ship mounts and for the designated main banks.
- `siege_reach.csv` records mount counts and theoretical shield/hull DPS for 25 layouts, 9 structure samples and four target bearings. Shield and hull are independent capacity estimates using matrix, caliber bias and the eligible attacker-class multiplier.

Unity compilation completed with zero errors and all 20 changed prefabs were saved and reloaded. Gameplay checks remain queued in the vault. Tests were authored and left unrun under the checkout override.
