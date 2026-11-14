---
type: engineering-reference
system:
  - combat
  - weapons
  - ships
  - balance
verified: 2026-11-14
verification: "Source, serialized mount arcs, Unity compilation, and loaded
  prefab geometry checked after the siege/facing changes. Legacy telemetry
  predates the cadence and siege changes. Tests and controlled gameplay not
  run."
---
# Weapon Effective DPS Limitations

## Goal

- Explain why listed weapon DPS can exceed damage delivered during a siege.
- Evidence: 2026-11-14 Empire/Rebellion audit; source/serialized assets and recorded telemetry, not a controlled playtest.

## Rules

- Keep nominal DPS, directional coverage, firing availability and delivered shield/hull damage separate.
- Nominal DPS = damage × shots per salvo / max(reload, (shots − 1) × interval); summing mounts does not prove simultaneous fire.
- Forward anti-ship DPS = nominal sum of non-strikecraft-only mounts admitting a common bearing at hull-local +Z. It includes ion/missile profiles before target multipliers.
- Common-bearing charts approximate a distant target. Nearby station hardpoints have different bearings/distances from each muzzle.
- Implemented siege and main-battery rules → [[GameDesign/Combat#Capital siege and hull facing]]. Verification and remaining player checks → [[Done/Features/Capital_Siege_And_Forward_Batteries_Plan]].

## Implementation

### Getting weapons into range

- Runtime eligibility checks planar XZ distance from each mount to a target hardpoint, plus parent-local yaw limits; no pitch gate (`WeaponTargetSelectionJob`).
- Projectile travel uses a resolved impact surface; range eligibility and flight timing use different endpoints.
- `engagementRange` (per target class, `WeaponEngagementRange.Calculate`) = longest weapon range at which mounts covering ≥60% (`DAMAGE_COVERAGE`) of effective DPS still reach. Up to 40% of effective DPS may be out of reach by design.
- Standard approach = max(0.65 × engagementRange, own + target navigation radius). Standard hold gate = max(0.8 × engagementRange, 1.15 × combined navigation radius).
- Contact-limited HeavyCapital siege approach uses own radius + live structure hull envelope + 1 unit; the hold gate adds another 1 unit. The pathfinder retains its own obstacle and hull-span clearance checks.
- Executor, Rebel L5 sample: old/new centre standoff 1455.4/1299.8 units; best reachable main-battery nominal DPS 176–219/307–483 across eight station orientations. Re-sample with actual hardpoint positions when checking another layout or station upgrade.
- Facing samples 2° yaw steps around the hull pivot. Eligible flagged batteries score first using planar reach and the target selector's parent-local yaw. Use secondary coverage only when the main batteries have no firing solution.
- Siege damage ×1.5 applies to eligible HeavyCapital mounted fire and impact reservations before tick splitting. Nominal profile DPS is unchanged; hit chance, armor, shields, abilities and in-flight limits still affect delivery.

### Projectile-flight delays

- Audit baseline (before `e18290ca9`): `AttackSequenceState` held `WaitingForEffects` until every shot effect finished → real cycle = max(reload, burst + flight + visual tail). Recorded battle telemetry reflects this gate.
- Executor heavy turbolaser: 8 s nominal reload / 15 s maximum-range flight; heavy ion: 12 s reload / 16.7 s maximum-range flight → under the baseline both fired well below nominal DPS at range.
- Current (`e18290ca9`, 2026-11-14): `IsBusy` = emitting || reload running || turning. `StopEmitting` → `Ready` immediately; salvos overlap in flight.
- New limit: `TryStart` refuses while `activeEffects + ShotsPerSalvo > inFlightCap`. Cap = `ExpectedShotsInFlight` = ShotsPerSalvo × (1 + ceil(range/projectileSpeed ÷ cycle)) → max-range nominal cycle fits the cap; cadence modifiers on units without Boost Weapon Power (Full Salvo, Assault, Main Batteries) can still hit it at long range.
- Also gated: pool not prewarmed (`!GetPool().IsPrewarmed`) → no salvo start; ≤1 shot per hardpoint per frame (`EarliestFrame = frame + 1`) → short intervals round up to frame time.
- Boost Weapon Power: reload ×0.1 and within-burst interval ×0.1; running reload and pending shots retime on modifier change. Boosted cap only on units whose `ShipData.Abilities` contains ability 5.
- Owner (closed, `acceptance: pending-user`) → [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]]. Runtime cadence/DPS not yet measured in a battle; tests not run.

### Wasted shots

- CommitImpact records waste when the whole target is destroyed or a strikecraft member can no longer be hit.
- A destroyed ship hardpoint alone is not recorded waste: surviving ship hardpoints can receive redistributed damage.
- Executor: 1,756,681 landed raw damage versus 1,537,637 wasted raw damage → 46.7% waste among these recorded arrivals.
- Executor delivered 1,562,243 effective shield+hull damage; 94,129 (6.0%) targeted generic Structure.
- Type aggregates and cheats prevent per-instance/pre-cheat DPS or causal attribution. Large volleys dying in flight are an investigation target, not proof of one faulty selector.
- Attribution/overkill investigation owner → [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]].

## Important Values

- Battle file: Battle_20261114_125915.csv; 3089 unscaled seconds. User confirmed cheats/added ships; final fleet totals are not fair-match balance evidence.
- Executor aggregate effective damage / first-to-last-shot window = approximately 619/s; this is not individual in-combat DPS.
- Tracked Rebel main station: 126000 hull / 100800 shields; full at sampled times until closing phase. Station telemetry selects one production base, not all defenses.
- Level-5 station regeneration = 456 shields per 3 s (152/s); Reinforced Structures = incoming damage ×0.85.
- Empire HeavyCapital baseline: Executor, Titan, Allegiance. No blanket attacker-class anti-station multiplier existed in the audit.
- Existing heavy-caliber structure bias =1.25; ion shield/hull multipliers =3/0.1; concussion missile structure hull multiplier =0.4 with shield bypass.

## Edge Cases

- Nominal all-mount DPS includes guns unable to share a bearing and may include fighter-only PD. Exclude PD for anti-ship coverage.
- The November 14 telemetry predates main-battery-first auto-facing. During a current solve, destroyed, disabled and target-ineligible mounts contribute zero weight.
- Ships only perform combat-facing turns while stationary; navigation controls moving headings.
- All 37 Empire/Rebellion roster ships were audited; the siege audit then covered 25 layouts incl. Republic (`Tools/BattleAudit/CapitalSiegeAudit.md`). Separatist/Rebellion arcs outside those layouts are unaudited.
- Gate removal raised unboosted long-flight DPS without stat changes (user-accepted): Missile cycle ~12.9 → 10 s (+27% at max range), Ion ~7.2 → 6 s (+20%). Executor turbolaser/ion gain more (flight > reload).
- Battle_20261114_125915 predates the gate removal → its Executor delivered-damage figures are not current-cadence evidence. Re-capture before siege tuning; do not silently compensate away the user-approved 1.5× rule.
- `ShipEngagement` standoff (0.65×) sits inside the 0.8× gate. AttackMove/Guard/Hunt pursue only while `CanEngage` is false → a ship inside the gate does not move closer, even when individual mounts are out of reach.

## Files

- [[GameDesign/Combat]]
- [[Done/Features/Capital_Siege_And_Forward_Batteries_Plan]]
- `Tools/BattleAudit/CapitalSiegeAudit.md` — serialized arc and standoff comparisons.
- [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]]
- [[Rules/BATTLE_PERFORMANCE_GUIDELINES]] — weapon cadence / pooled overlap contract.
- [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs` — GetFiringTurnAngle, HasEnoughRange, CanEngageMainWeapon, CommitImpact.
- `Assets/Scripts/Components/Weapon/WeaponEngagementRange.cs`; `WeaponTargetSelectionJob.cs`; `CombatAttackCoordinator.cs`.
- `Assets/Scripts/Components/AttackComponent/WeaponProfile.cs` — CycleSeconds, DamagePerSecond.
- `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs`; `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs`.
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`; `Assets/Scripts/Components/Weapon/AttackSequenceState.cs`.
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotEffect.cs`; `Assets/Scripts/Components/AttackComponent/AttackData.cs`.
- `Logs/BalanceAudit/2026-11-14/weapon_angles.json`, `weapon_angles_full.csv`, `weapon_arc_raw_dps.csv`, `audit_weapon_angles.py`.
- `C:/Users/golin/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/Battle_20261114_125915.csv`.
