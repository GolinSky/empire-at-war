---
category: Features
status: done
created: 2026-11-15
verified: 2026-11-15
design_state: implemented
completed: 2026-11-15
acceptance: pending-user
---
# Artillery Ship Standoff Plan

## Goal

- Explicit Attack and formation Attack: Victory I, Republic Victory, MC30c, Recalcitrant and Peleset fight from surviving launcher reach.
- When all usable missile and torpedo hardpoints are gone, resume ordinary pursuit.

## Decision

- Use the longest surviving missile, proton torpedo or ion torpedo reach against the target class. Measure from the hull centre by subtracting mount offset from effective reach. No ordinary range clamp while a usable launcher band exists.
- Approach to 90%, hold through 95%, and retreat below 80% of effective launcher reach. Raise the thresholds for hull clearance; fall back to ordinary pursuit when clearance leaves no usable launcher band.
- Profundity remains ordinary: its 455 torpedo range is shorter than its 517.5 turbolasers. No weapon range changes.
- Mixed batteries can hold beyond shorter launcher and gun reach until longer mounts are destroyed.

## Implementation

- `ShipData.UsesArtilleryStandoff` enables the policy on five saved assets. `ShipInstaller` supplies `IArtilleryCapability` to `AttackTargetState` only for opted-in ships.
- `WeaponComponent` caches launcher membership; `ArtilleryCapability` caches range by target class. Destruction/restoration, Fleet Command reach changes and respawn invalidate capability. Reload, salvo cooldown and ion disable retain the band.
- Formation offsets choose an approach bearing on the same ring. Retreat uses the ship's current bearing. Keep committed legs until the opposite edge is crossed or the target/formation/reach changes.
- Failed routes retry at 0.5 s intervals across at most six alternate bearings per stable target position and reach. Existing movement owns pathing and braking.
- AttackMove, Guard, Hunt, auto-power and facing retain their existing logic. `ShipOrderRunner` owns transitions; replica authority is unchanged.
- Reusable guidance: [[Rules/WEAPON_EFFECTIVE_DPS_LIMITATIONS#Artillery engagement]] and [[GameDesign/Combat#Artillery standoff]].

## Important Values

Verified 2026-11-15 from live prefabs + `WeaponsData.asset` via read-only Unity eval (`WeaponsData.GetProfile(...).WithRangeMultiplier(rangeMultiplier)`). All six `WeaponComponent.rangeMultiplier` = 1. World units.

| Ship (view prefab) | Launchers | Other weapons |
|---|---|---|
| Victory I (`Empire/VictoryIShipView`) | 4× HeavyAssaultMissile 715; 2× MediumDualMissileLauncher 632.5 | 6× DualMediumTurboLaser 517.5 (main); 4× HeavyLaser 390 |
| Republic Victory (`Empire/VictoryShipView`) | 2× HeavyDualMissileLauncher 715; 2× MediumDualMissileLauncher 632.5; 1× MediumProtonTorpedo 402.5 | 6× MediumQuadTurbolaser 517.5 (main); 4× MediumDualLaser 345 |
| MC75 Profundity | 6× HeavyProtonTorpedo 455; 2× MediumIonTorpedo 402.5 | 12× DualMediumTurboLaser 517.5; 4× HeavyDualLaser 390; 6× PointDefense 200 |
| MC30c | 2× HeavyDualMissileLauncher 715; 2× MC30cClusterBombLauncher 550 (Missile family); 2× HeavyProtonTorpedo 455 | 4× DualMediumTurboLaser 517.5; 4× MediumDualLaser 345; 2× PointDefense 200 |
| Recalcitrant | 6× MediumMissileLauncher 632.5 | 8× DualMediumTurboLaser 517.5; 1× MediumDualIonCannon 575 |
| Peleset | 2× HeavyDualMissileLauncher 715; 1× MediumProtonTorpedo 402.5 | 6× DualHeavyTurboLaser 585; 2× MediumIonCannon 575; 2× MediumPointDefense 200 |

- Missile family range 550; proton/ion torpedo 350; medium/heavy caliber ×1.15/×1.3 → 632.5/715 and 402.5/455. Matches live profiles.
- Missiles outrange every gun on all five missile ships; torpedoes outrange no gun. Own-loadout comparison only, not superiority against every enemy.

### Current engagement range (`WeaponEngagementRange.Calculate`, live)

| Ship | Corvette | Frigate | Cruiser / Capital / HeavyCapital / Structure |
|---|---:|---:|---:|
| Victory I | 517.5 | 517.5 | 517.5 |
| Republic Victory | 517.5 | 517.5 | 517.5 |
| MC75 Profundity | 517.5 | 455 | 455 |
| MC30c | 517.5 | 517.5 | 455 |
| Recalcitrant | 517.5 | 517.5 | 517.5 |
| Peleset | 585 | 585 | 585 |

- Derived distances: approach = 0.65 × range, hold = 0.8 × range. 517.5 → 336 / 414; 455 → 296 / 364; 585 → 380 / 468.
- Longest-launcher candidates: 715 → 465 / 572; 632.5 → 411 / 506 under the historical ordinary factors; artillery uses its own factors and mount-offset correction.
- MC30c and Profundity already hold at torpedo range against cruiser+ because torpedo DPS dominates the 60% coverage; MC30c still under-uses its 715 missiles.

## Verification

- Unity 6000.4.7f1 compiled Editor scripts and Windows player scripts. The CLI response expired during player compilation; the Editor log reported success and Unity emitted the Windows player `Assembly-CSharp.dll`.
- Unity saved, reserialized and read back all five enabled ship assets. Diff and Console inspection found no compilation or serialization errors.
- Updated `ShipPursuitTests` (Navigation) and `WeaponRangeTests` (Combat): band and formation behavior, bounded retreat retry, hull clearance, partial/all launcher loss, restoration, Fleet Command, reload/ion stability, target eligibility and auto-power reach.
- Reviewed existing Navigation, Combat, Ships, Abilities and Squadrons contracts for changed bindings, data and unaffected fallback behavior. Tests not run under the local override. No full player build or Jenkins run.
- Gameplay and matched CPU acceptance pending → [[TODOs/User_Acceptance_Queue]].

### Post-implementation review (2026-11-15, commit `b848a70cc`)

- Source matches plan: opt-in flag on exactly five assets (Profundity absent); launcher families Missile/Torpedo/IonTorpedo, strikecraft-only excluded; per-class cache invalidated by `WeaponHardPoint.DestructionChanged`, `CombatModifiers.Changed` and `Release`/`Respawn`; AttackMove/Guard/Hunt/`ShipEngagement` untouched; `IArtilleryCapability` resolves via `RegisterComponent` (all interfaces), one `WeaponComponent` per in-scope prefab.
- No per-tick allocation in `TryUpdateArtilleryMove`: squared distances, cached reach, dictionary refilled only after invalidation.
- Editor + player `Assembly-CSharp` rebuilt after commit; Console has no current compile errors (earlier `NetworkTickReplicationTests` CS1503 predates the fix in the same snapshot).
- Watch in acceptance: compact Attack slots ring the target; a ship given a far-side slot (≈±120° in 4+ ship groups) heads across target gun range until the 80% retreat trigger re-plans it outward. Self-correcting, extra movement only.
- Watch in acceptance: after six failed retries against a stationary target the ship waits outside the band until the target moves — bounded, but may not fire.
- Accepted tradeoff: no ordinary-range floor. After missile loss, torpedo-only band can sit slightly inside ordinary approach (Peleset ≈ 0.9 × (402.5 − offset) vs 380).
- Out of scope: stale "nebulae" comment in `WeaponComponent.Initialize` (multiplier is Fleet Command).
- Tests not run (local override).

## Files

- `Assets/Scripts/Entities/Ship/{Data/ShipData,ShipInstaller,StateMachine/AttackTargetState}.cs`
- `Assets/Scripts/Components/Weapon/{IArtilleryCapability,ArtilleryCapability,WeaponComponent}.cs`
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`
- `Assets/Settings/Data/Ship/Empire/{VictoryIShipData,VictoryShipData}.asset`, `Rebellion/MC30cShipData.asset`, `Separatist/{RecalcitrantShipData,PelesetShipData}.asset`
