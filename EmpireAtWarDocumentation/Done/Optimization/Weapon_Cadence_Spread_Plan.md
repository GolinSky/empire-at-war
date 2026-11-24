---
category: Optimization
status: done
created: 2026-11-24
updated: 2026-11-24
completed: 2026-11-24
acceptance: not-applicable
outcome: reverted
---
# Weapon Cadence Spread Plan

> **Reverted 2026-11-24 (user).** Every change below — cadence spread, caliber/mount reload multipliers, ±10% reload spread, laser/turbolaser/ion speed ×2, single-shot ×4 then ×2 DPS, description edits, test edits — was rolled back to the pre-session state. Verified: all 135 family/caliber/mount/weapon profile rows identical to the session-start dump; compile clean. Patch: `Logs/Patches/weapon_cadence_2026-11-24.patch` (gitignored). Not reverted: user's own edits to `EmpireShipShieldRegenTiersData.asset` / `RebellionShipShieldRegenTiersData.asset`.
>
> Findings still valid: Empire arcs = Rebel arcs; Empire gets ≈30% less hull+shields and 30–45% less regen per population slot (capitals 10 slots vs Rebel 8; heavy cruiser regen 250 vs 350); saved armada battle was 2 Rebel AIs vs 1 Empire AI stuck at `capacity=119/120` with ≈110k unspent.

## Goal

- Fewer bolts, missiles and torpedoes on screen; same DPS for every weapon.
- Mounts of different weapons stop firing in lockstep.

## Decision

- Chosen (user, 2026-11-24, "apply as written"): longer family reloads + reload multipliers per caliber/mount + ±10% reload spread per salvo.
- Rule: every reload change scales shot damage by the same factor → `damage × shots / cycle` constant.
- Kept: Executor variant reload ×2 stacks on top; Octuple ×1.5; first volley still simultaneous (reload starts with the first shot).
- Avoid: per-weapon overrides; changing Ion, MassDriver, Beam, fighter warheads (one torpedo per attack run → reload does not set cadence).

## Before (source, 2026-11-24)

- Reload/salvo came from the family only; caliber and mount changed damage and range, never timing.
- No reload randomness → mounts that acquire together fire together forever.
- Shots/s with one of every unit: total 1,389; Turbolaser 512, Laser 309, PointDefense 242, fighter lasers ≈250, all warheads ≈26.

## Implementation

| Family | Before | After |
|---|---|---|
| Turbolaser | 2 × 24 / 4 s | 2 × 36 / 6 s |
| Laser | 2 × 9 / 2 s | 2 × 13.5 / 3 s |
| PointDefense | 4 × 8 / 2 s | 3 × 16 / 3 s |
| Fighter / Interceptor / Hero Laser | 2 × 7.5 / 1.35 s | 2 × 11.25 / 2.025 s |
| Missile | 1 × 256 / 10 s | 1 × 307.2 / 12 s |
| Torpedo | 1 × 288 / 12 s | 1 × 360 / 15 s |

- New `reloadMultiplier` on `WeaponCaliberRule` (Light 0.85, Medium 1, Heavy 1.2) and `WeaponMountRule` (Single 1, Dual 1.1, Triple 1.2, Quad 1.3, Octuple 1.5).
- `WeaponProfileFactory`: ship platform only; `cadence = caliber.ReloadMultiplier (not PointDefense) × mount.ReloadMultiplier`; reload and damage both × `cadence`.
- `WeaponHardPoint.RELOAD_SPREAD = 0.1`: each salvo's reload × `Random.Range(0.9, 1.1)`. `ExpectedShotsInFlight` sizes stock for reload × 0.9.
- Example: ISD II mounts now reload at 6.0 / 6.6 / 7.2 / 9.4 / 10.8 s instead of all 4 s.

## Results (modelled from live data, not in game)

- Shots/s with one of every unit: 1,389 → 826 (−41%). Turbolaser −46%, PointDefense −50%, Laser −37%, fighter lasers −33%, Missile −27%, Torpedo −23%.
- Executor 108 → 60 shots/s, Praetor 51 → 32, Mandator 40 → 22, Viscount 36 → 23.
- DPS: re-dumped all 111 weapon profiles from `WeaponsData.asset` → 0 mismatches vs before.
- Unity compile clean (runtime + tests). Tests not run (local override).

## Follow-up — laser speed ×2 (2026-11-24)

- Why (user, in game): longer reloads + higher damage still lost to ships with slightly higher shield regen; shields regenerate while slow bolts fly. Damage left unchanged.
- `projectileSpeed` 172.9 → 345.8 on families Laser + Turbolaser and on 5 Turbolaser `overrideVisual` weapons: `DualHeavyTurboLaserDby827`, `HeavyTripleTurbolaser`, `ExecutorHeavyTurbolaser`, `ExecutorHeavyDualTurbolaser`, `ExecutorMediumDualTurbolaser`.
- Not changed: fighter / interceptor / hero lasers (182), PointDefense (117; `DualRepeatingPointDefense` 172.9), Ion, warheads.
- Verified: profile re-dump → all 28 Laser/Turbolaser weapons at 345.8; no other field changed on any of 111 weapons. Flight at max range 585 u: 3.4 s → 1.7 s.
- Ion ×2 (user: ISD I ion too slow): family Ion + `ExecutorHeavyDualIonCannon` override 107 → 214. All 9 Ion weapons at 214; nothing else changed. Flight at 650 u: 6.1 s → 3.0 s. FighterIon (71.5) unchanged.
- Tests reviewed, no edit: speed is read from the profile or set in synthetic profiles (`WeaponCadenceTests`, `ShotEffectPoolLifecycleTests`).

## Follow-up — single-shot salvos, DPS ×4 (2026-11-24, user test)

- Why: shield regen (550–650/s per capital) ate most of ship DPS (856–1,406 effective), so fights hinged on DPS − enemy regen. User keeps regen as a deflector mechanic; raising all damage ×4 shrinks regen's relative weight to ¼.
- Rejected: regen pause under fire (user wants regen), regen cut, Empire-only arc widening (Empire arcs = Rebel arcs: ~100% nose-on, ~50% off-angle).
- All 15 families: `shotsPerSalvo` → 1, `reload` → reload ÷ old shots, `damage` → 4 × old DPS × new reload. Turbolaser 2×36/6 s → 1×144/3 s; Laser 1×54/1.5 s; PointDefense 1×64/1 s; fighter lasers 1×45/1.013 s; single-shot families damage ×4.
- Verified: profile re-dump → all 111 weapons DPS exactly ×4, no multi-shot salvo left; bolts per second unchanged (Σ 56.56 → 56.73 per mount set).
- Not scaled: hull, shields, regen, abilities (`ShipAbilityCatalog.asset`) and super weapons (`SuperWeaponData.asset`) → relatively ¼ as strong; fights ≈4× shorter.
- Descriptions: "2-burst" removed in `EmpireFaction.asset` (×6) and `RepublicFaction.asset` (×1).
- Test: `WeaponTaxonomyTests.Executor_WeaponProfilesMatchBaseline` baseline × `SINGLE_SHOT_DPS_RAISE` (4). Others reviewed: salvo-count asserts are relational or synthetic.
- Backup of the pre-×4 asset: scratchpad only (not durable); git HEAD has the pre-pass values.
- Revised same day (user: ×4 "bad idea"): all family damage halved → DPS ×2 vs pre-pass; salvos stay 1 shot, reloads unchanged. Turbolaser 1×72/3 s, Laser 1×27/1.5 s, PointDefense 1×32/1 s, fighter lasers 1×22.5/1.013 s. Verified: all 111 weapons exactly ×2 DPS. Test factor `SINGLE_SHOT_DPS_RAISE` = 2. Regen now weighs ½, fights ≈2× shorter.
- Open: survivability per population slot — Empire capitals 10 slots vs Rebel 8 (MC80/Remonda), Empire heavy-cruiser regen 250 vs Rebel 350; Remonda 7,625 hull+shields per slot vs ISD II 5,400. Not changed; awaiting user test of ×4.

## Edge Cases

- Big single hits: Heavy Octuple 741 per shot / 10.8 s; Heavy Dual Missile 1,856 / 15.8 s; Executor medium dual turbolaser 13.2 s. More overkill on corvettes/hardpoints.
- PointDefense 16-damage shots may overkill low-HP fighters.
- `HitAccumulator` rolls per shot → fewer shots, longer hit/miss streaks.
- Reload spread uses `UnityEngine.Random`, same as hit rolls already do.

## Tests

- `WeaponCadenceTests.VisualStock_UsesSlowerOfReloadAndBurst`: boosted stock 24 → 27, 56 → 60 (spread sizing); added `ScheduledSalvo_ReloadVariesWithinSpread`.
- `WeaponTaxonomyTests.Mounts_MultiplyShotDamageByMountRule_AndKeepShots`: damage now × mount reload; asserts DPS follows damage multiplier; added `CaliberAndMountReloads_DifferSoMountsFireOutOfStep_WithoutChangingDps`.
- Reviewed, no edit: `Executor_WeaponProfilesMatchBaseline` (asserts DPS), `ShotEffectPoolLifecycleTests` (lower bound), `WeaponRangeTests`, `WeaponCandidateTests`, `BoltParticleSimulationTests`, `WeaponLoadoutBakeTests`.
- Category: `Combat`. Not run.

## Guidance

- Extracted → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Projectile volume reduction · 2026-11-19]] (cadence spread bullet).

## Files

- `Assets/Scripts/Components/AttackComponent/WeaponCaliberRule.cs`, `WeaponMountRule.cs`, `WeaponProfileFactory.cs`
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`
- Previous pass → [[Done/Optimization/Projectile_Visual_Reduction_Plan]]
