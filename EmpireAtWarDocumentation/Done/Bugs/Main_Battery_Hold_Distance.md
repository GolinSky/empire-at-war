---
category: Bugs
status: done
created: 2026-11-24
updated: 2026-11-24
completed: 2026-11-24
acceptance: pending-user
acceptance_note: hold-distance fix passed in game 2026-11-24; ion speed look open
---
# Main Battery Hold Distance

## Goal

- Capitals stop where their whole forward main battery reaches the target, not where one main gun barely does.

## Cause (source + telemetry, 2026-11-24)

- `Battle_20261124_123400` / `Loading_20261124_123349_ai_orders.csv`: ISD II and Venator I both nose-on (`facingDot` 1.0), held 585 apart center-to-center at t=46 s.
- All heavy turbolasers have range 585 (octuple, dual heavy, DBY-827, quad); Executor 877. Range values are not the cause.
- ISD II octuples (its 8 `mainBattery` mounts) sit 33–52 units aft of center → at 585 only 4/8 reach; ISD II fired 47% of effective DPS vs Venator 94%.
- At 585: ISD II ≈666 effective DPS vs Venator regen 600 → net ≈66/s; Venator ≈800 vs ISD II regen 550 → net ≈250/s + beam ≈13k hull. Matches log (Venator hull −2.8k, ISD II hull −20k).
- `WeaponComponent.MainBatteryReaches` returned true when ANY main mount reached ANY target hardpoint; held ships also get `HELD_REACH_FACTOR` 1.1.

## Decision

- Chosen: every main battery that bears bow-on (arc via `WeaponTargetSelector.TryCalculateAim`) must reach a live target hardpoint.
- Skipped mains: arc never covers the target bow-on, or still short hull to hull (target pivot moved to `ShipEngagement.ContactDistance`). Prevents ramming (Executor).
- Fallback: no main bears → old rule (any main in reach).
- Avoid: DPS-share threshold (user wants 100% where possible); range buffs.

## Implementation

- `IWeaponComponent.MainBatteryReaches(IEntity target, float contactDistance, float reachFactor)`.
- Callers: `ShipEngagement.CanHoldOn`, `AttackTargetState.IsInReach` / `IsInHeldReach` pass `ShipEngagement.ContactDistance(...)`.

## Results (prefab geometry vs Venator I, nose-on)

| Ship | Old hold → fires | New hold → fires |
|---|---|---|
| ISD II | 595 → 27% | 576 → 89% |
| ISD I | 599 → 41% | 585 → 79% |
| Tector | 597 → 42% | 576 → 95% |
| Venator I | 620 → 8% | 593 → 91% |
| Executor (contact 1,131) | 1,200 → 12% | 1,138 → 17% |

- Rebel capitals (MC80 Freedom, Nebula, Nebulon-B) have no `mainBattery` mounts → unchanged; they stop by engagement range only.
- Compile clean. Tests not run (local override).

## Follow-up — held slack silenced mains (2026-11-24)

- User test (`Loading_20261124_125918_ai_orders.csv`): ISD II held at 580, stripped Venator shields by t=462, then dealt 0 damage from t≈482 (Venator hull frozen at 13,298, shields 1,860 → 9,000); `facingDot` 1.0 → 0.99.
- Cause: held check used `HELD_REACH_FACTOR` 1.1 for the main battery. Prefab geometry at 580: after the Venator's 3 most-forward hardpoints die, 0/8 octuples reach, but 8/8 "reach" at ×1.1 → ship never closed in.
- Fix: `ShipEngagement.CanHoldOn` and `AttackTargetState.IsInHeldReach` call `MainBatteryReaches(..., 1f)`; general reach keeps the 1.1 margin. Failing only closes in → no outward oscillation.
- Ion speed ×2 (user: ion low damage and slow): Ion family + `ExecutorHeavyDualIonCannon` 107 → 214; all 9 Ion weapons; no other field changed. Flight at 650: 6.1 s → 3.0 s.

## Tests

- `WeaponCandidateTests`: existing bow-on test passes `contactDistance` 0; added `MainBatteryReaches_NeedsEveryBearingMainInRange_AndSkipsMainsThatCannotHelp` (category `Combat`).
- Stubs updated: `ShipPursuitTests`, `ShipStopTests`, `SquadronTargetSelectorTests`.
- Run when authorized: `Combat`, `Navigation`, `Ships`.

## Guidance

- No reusable guidance beyond this note: the rule lives in the `IWeaponComponent` doc comment.

## Files

- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`, `IWeaponComponent.cs`
- `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs`, `AttackTargetState.cs`
- Related: [[Done/Optimization/Weapon_Cadence_Spread_Plan]] (reverted weapon passes; same investigation)
