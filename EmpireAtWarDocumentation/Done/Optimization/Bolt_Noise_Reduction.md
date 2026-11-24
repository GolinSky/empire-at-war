---
category: Optimization
status: done
created: 2026-11-24
updated: 2026-11-24
completed: 2026-11-24
acceptance: pending-user
---
# Bolt Noise Reduction

## Goal

- Fewer bolts on screen; same DPS, same reload rhythm, same time-to-kill.
- Faster bolts (+35%) without the large reload/DPS swings of the reverted pass ([[Done/Optimization/Weapon_Cadence_Spread_Plan]]).

## Decision

- Multi-shot families → 1 shot per salvo, damage × old shots; PointDefense 4 → 2. Reloads unchanged.
- Bolt speed ×1.35: Laser, Turbolaser, MassDriver, PointDefense, Fighter/Interceptor/Hero Laser, FighterIon + 6 bolt `overrideVisual` weapons.
- Not changed: Ion (already ×2 = 214), Missile/Torpedo/IonTorpedo/fighter warheads (×2 on 2026-11-19), Beam (no speed).
- Avoid: reload/DPS changes (shield regen sensitivity), caliber/mount reload spread, reload randomness.

## Implementation (`WeaponsData.asset` family rules)

| Family | Before | After | Speed |
|---|---|---|---|
| Turbolaser | 2×24 / 4 s | 1×48 / 4 s | 172.9 → 233.4 |
| Laser | 2×9 / 2 s | 1×18 / 2 s | 172.9 → 233.4 |
| PointDefense | 4×8 / 2 s (0.16 s) | 2×16 / 2 s | 117 → 158 |
| MassDriver | 2×52.5 / 5 s | 1×105 / 5 s | 117 → 158 |
| Fighter/Interceptor/Hero Laser | 2×7.5 / 1.35 s | 1×15 / 1.35 s | 182 → 245.7 |
| FighterIon | 2×1.3 / 4.91 s | 1×2.6 / 4.91 s | 71.5 → 96.5 |

- Overrides 172.9 → 233.4: `DualHeavyTurboLaserDby827`, `HeavyTripleTurbolaser`, `ExecutorHeavyTurbolaser`, `ExecutorHeavyDualTurbolaser`, `ExecutorMediumDualTurbolaser`, `DualRepeatingPointDefense`.
- Descriptions: "2-burst" removed (`EmpireFaction.asset` ×6, `RepublicFaction.asset` ×1).

## Verification

- Profile re-dump: all 111 weapons DPS identical, reload identical.
- Shots/s with one of every unit: 1,389 → 731 (−47%); every multi-shot family −50%.
- Compile clean. Tests not run; reviewed: salvo/speed asserts are synthetic (`WeaponCadenceTests`) or relational (`WeaponTaxonomyTests`, `ISDIIShipTests`, `ShotEffectPoolLifecycleTests`, `WeaponLoadoutBakeTests`) → no edits.

## Edge Cases

- Per-shot damage ×2 → more overkill on dying hardpoints/fighters; DPS unchanged.
- `HitAccumulator` rolls per shot → fewer, larger hit/miss events.
- Full Salvo / weapon-power interval multipliers do nothing on 1-shot salvos (already true for missiles).

## Guidance

- Updated [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Projectile volume reduction · 2026-11-19]] salvo line.
