---
category: Bugs
status: review
acceptance: pending-user
created: 2026-11-20
---
# Hardpoint-Based Weapon Reach

Related: [[Review/Bugs/Attack_Order_Hold_In_Range]], [[Done/Bugs/Capital_Movement_And_Class_Rework_Fixes]], [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]

## Goal

- Ship hold/approach decisions use mount → target-hardpoint distance, not centre → centre.
- Radar lists every enemy a mount can reach.
- All gameplay range checks are planar (XZ).

## Verified State (2026-11-20, source + prefab audit)

- Firing is correct: each mount tests its own position, planar. `WeaponTargetSelector.TryCalculateAim`, `WeaponTargetSelectionJob`, `WeaponComponent.TryFireWeapon`.
- Movement is wrong: `ShipEngagement.IsInRange` / `IsInReach` / `CanEngage` use `movement.GetRange` (centre → centre) against `GetEngagementRange`.
- Hold rule: `distance <= max(engagementRange, contact * 1.15)` → `HoldAndLookAtTarget("inReach")`.
- Standoff: `max(engagementRange * 0.65, contact)` from target centre.
- Mount scan `WeaponComponent.TryGetHardPointApproach` exists but runs only when target is `BattleCruiser`/`Dreadnought` and `footprint.NavigationRadius > AttackDistance` (`AttackTargetState.UpdateMoveState`).
- Gate never fires for attacker-side size. Praetor as target: only attackers with max range < 317.
- Radar: planar, scanner centre → target centre (`RadarDetectionJob`). Weapons only pick radar-listed groups.

## Important Values

Planar mount offsets from prefab root; ranges after `rangeMultiplier`.

| Ship | NavR | Max / median mount offset | Gun range | Engage | Radar | Mult |
|---|---|---|---|---|---|---|
| Praetor | 317 | 314 / 159 | 390–715 | 518 | 750 | 1 |
| Executor | 1059 | 968 / 310 | 230–975 | 230–776 | 1235 | 1 |
| Mandator | 483 | 420 / 157 | 200–1430 | 200–1170 | 1455 | 2 |
| Malevolence | 287 | 241 / 108 | 690–1035 | 690–1035 | 1195 | 2 |
| Titan | 266 | 202 / 179 | 200–1300 | 200–1170 | 1495 | 2 |
| Lucrehulk | 230 | 142 / 141 | 200–1430 | 200–1035 | 1455 | 2 |
| Procurator | 133 | 113 / 55 | 805–1170 | 1035 | 750 | 2 |
| MC80Independence | 218 | 176 / 66 | 200–650 | 390–585 | 750 | 1 |
| HomeOne | 199 | 174 / 90 | 200–650 | 200–585 | 750 | 1 |
| Secutor | 180 | 158 / 94 | 390–650 | 390–585 | 750 | 1 |
| Allegiance | 141 | 109 / 80 | 595–748 | 673 | 775 | 1.15 |

- Praetor, worst-case bearing (mount on far side): 45/130 mounts out of range at standoff, 118/130 at the hold limit.
- Executor: 117/174 at standoff, 167/174 at hold limit.
- Praetor radar 750 < max offset 314 + max gun 715 = 1029 → bow guns cannot see targets they reach.
- Procurator radar 750 < gun range 805–1170 → radar caps every gun.
- Hardpoint height spread: Praetor −41…+48, Executor −73…+47. Planar math already ignores it.

## Issues

1. Centre-based hold. Big attacker holds with most mounts out of range. Root cause of the Praetor bug.
2. `rangeMultiplier = 2` (Titan, Mandator, Procurator, Lucrehulk, Malevolence) and `1.15` (Allegiance) mask issue 1 by inflating every gun. Executor uses `1`; it relies on `Executor*` weapon types and the target-side mount scan.
3. Mount scan gate covers target size only, and only when the footprint exceeds max gun range.
4. Radar centre-to-centre: ignores scanner mount offset and target hull size.
5. Leftover 3D distances (height changes the result):
   - `HuntState.cs:99` nearest target
   - `SquadronTargetSelector.cs:55,86` radius filter
   - `SquadronPilot.cs:67` attack zone
   - `IncomingMissileRegistry.cs:36` point-defence range
   - `HarmonicBombService.cs:74,112,117` blast radius
   - `SimulationTargeting.cs:49,53`
- Not an issue: `ArtilleryCapability.cs:25` uses 3D offset on purpose (bounds planar offset when banking).

## Decision

- Chosen (revised 2026-11-20): keep `rangeMultiplier`, set it from hull size. No runtime scan.
- Formula: `1 + medianMountOffset / engagementRange` (vs Capital, multiplier-free range).
- Why: huge ships are contact-limited. Praetor vs a capital stops at 317 + ~90 = 407; far mounts need 407 + 314 = 721 > 518. Moving closer is impossible, so a reach scan cannot fix it. Only more range can.
- Why: zero per-frame cost; field and `WithRangeMultiplier` already exist.
- Avoid: per-mount scan for every ship pair (130 × 130 for Praetor pairs), guessed `2` values.
- Rejected: stop closer (`range − offset` engagement). Praetor 518 → 365, below contact 407.

### Multiplier table

| Ship | Now | Median-based | Max-based |
|---|---|---|---|
| Praetor | 1 | 1.31 | 1.61 |
| Executor | 1 | 1.40 | 2.25 |
| Mandator | 2 | 1.30 | 1.81 |
| Malevolence | 2 | 1.21 | 1.47 |
| Titan | 2 | 1.31 | 1.34 |
| Lucrehulk | 2 | 1.27 | 1.27 |
| Procurator | 2 | 1.11 | 1.22 |
| HomeOne | 1 | 1.15 | 1.30 |
| MC80Independence | 1 | 1.11 | 1.30 |
| Secutor | 1 | 1.16 | 1.27 |
| Allegiance | 1.15 | 1.14 | 1.19 |
| Mediator, Rothana, Invincible, Tector, RecusantDreadnought | 1 | 1.04–1.08 | 1.13–1.17 |

## Implementation

1. Set `rangeMultiplier` on big-ship prefabs from the table (median-based). Scope pending user decision.
2. Raise radar `Range` where it is below `maxMountOffset + engagementRange * multiplier` (Praetor 750 → ~1000).
3. Replace issue-5 call sites with `PlanarGeometry.Distance` / `DistanceSquared`.
4. Leave `TryGetHardPointApproach` and its gate unchanged.

## Open Decisions (user)

- Lower the five ships at `2` to the table value (range nerf), or keep `2` as deliberate balance.
- Bake values into prefabs (recommended, no code) or compute at `WeaponComponent.Initialize` (auto-follows hardpoint edits, shifts every ship by 2–8 %).

## Tests

- Review/update: `WeaponCandidateTests`, `ShipPursuitTests`, `ShipStopTests`, `SquadronTargetSelectorTests`.
- Add: big attacker vs small target holds only when coverage mounts reach; two ships on lowest/highest tier at equal XZ distance get identical reach, radar, point-defence and squadron results.
- Categories: `Combat`, `Navigation`, `Ships`, `Vision`, `Squadrons`, `Abilities`.
- No stat-pinning asserts.

## TODO

- [x] User decision 2026-11-20: use the median-based value on all big ships (including the five at `2`); raise radar on all ships.
- [x] Step 1 — 2026-11-20: `rangeMultiplier` baked on 16 prefabs (`NavigationRadius >= 100`): Praetor 1.31, Executor 1.4, Mandator 1.3, Titan 1.31, Lucrehulk 1.27, Malevolence 1.21, Procurator 1.11, HomeOne 1.15, MC80Independence 1.11, Secutor 1.16, Allegiance 1.14, Mediator/Rothana/Invincible 1.08, Tector 1.07, RecusantDreadnought 1.04.
- [x] Step 2 — 2026-11-20: radar `Range` raised (never lowered) to `ceil5(max(mountOffset + gunRange))` on 15 `ShipData`: Executor 1235 → 2055, Praetor 750 → 1120, HomeOne 925, Secutor 895, MC80Independence 880, Allegiance 850, Mediator 800, Gladiator 765, Lupus 755, Victory 745, MC30c 735, Peleset 735, Arquitens 730, CD273 570, QuasarFire 530.
- Verification: assets saved through the Editor, console clean, tests not run. No test pins these values (source search).
- Rebake needed when hardpoints or weapon ranges change; script was a one-off Editor eval.
- [x] Step 3, Hunt only — 2026-11-20: `HuntState.FindTarget` uses `PlanarGeometry.DistanceSquared`. User scoped the fix to Hunt; the other five 3D sites stay as they are.
- [ ] Tests updated; in-game check with Praetor, Executor, Mandator

## Review 2026-11-26
- Open user gates: in-game check with Praetor, Executor, Mandator; related tests. Moved to Review by user decision; not re-verified against source in this review.
