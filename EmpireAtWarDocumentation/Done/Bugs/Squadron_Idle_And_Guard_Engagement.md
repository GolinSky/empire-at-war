---
category: Bugs
status: done
created: 2026-11-21
updated: 2026-11-21
system:
  - squadrons
  - combat
  - orders
completed: 2026-11-21
acceptance: pending-user
---
# Squadron Idle and Guard Engagement

## Goal

- Idle squadrons (hero squadrons most visibly) react to enemies inside their radar, not only at 70 u.
- Guarding squadrons stay near the guarded ship; they stop chasing targets across the map.
- Idle squadrons return to their post after a fight.

## Findings (source, 2026-11-21)

- Idle search: `SelectNear(_radar, _pilot.LoiterCenter, Data.GuardRadius)` → `Squadron.cs:668`.
- `GuardRadius` = 70 on all 34 `Assets/Settings/Data/Squadron/*SquadronData.asset`; radar `Range` = 350; `LoiterRadius` 18–20.
- Guard search: `max(_guardedRadius + GuardRadius, SquadronGuardEngageRadius)` → `Squadron.cs:641-643`. `UnitOrderSettings.asset` → 800, so `GuardRadius` never applies to Guard.
- Guard search uses team vision (`SelectSeen`), not own radar → targets up to 800 u from the ship.
- No leash: `ShouldSearchTargets` returns `false` while `IsEngaging` (`Squadron.cs:714`). No distance recheck → squadron follows a fleeing target without limit.
- Idle drift: `ResumeCourse` default → `_pilot.Loiter(_flight.Centroid)` (`Squadron.cs:748`). Post moves to where the fight ended.
- `SquadronPilot._loiterCenter` survives `Engage` and `FlyTo` → original post is still readable through `LoiterCenter` during a fight.
- Hangar squadrons get `Guard` on launch (`Entity.cs:82`). Standalone squadrons (heroes) stay idle → 70 u rule. Hero spawn path inferred from that single call site; not verified in-game.
- Enemy AI escorts share the 800 value (`EnemySquadronCommander.cs:485`, [[Architecture/Enemy_AI_Objectives]]).

## Decision

- Idle radius: raise `GuardRadius` data value. `Why`: field already owns idle and attack-move search; stays per-unit tunable. `Avoid`: second global idle setting.
- Guard radius: lower `squadronGuardEngageRadius`. `Avoid`: removing the floor; large hulls still need reach past their guns.
- Leash: one factor in `UnitOrderSettings`, applied to Guard and idle only. `Avoid`: leash on Attack, Hunt, UseAbility, AttackMove; those are explicit orders.
- Bomber exemption: no leash while the engaged target is the guarded unit's own combat target (`IUnitCombatObserverFacade.Target`). The ship's guns already bound that distance.

## Important Values (starting points, tune in-game)

| Value | Current | Target |
|---|---|---|
| `SquadronData.GuardRadius` (34 assets + default) | 70 | 250 |
| `UnitOrderSettings.squadronGuardEngageRadius` | 800 | 400 |
| `UnitOrderSettings.squadronLeashFactor` (new) | none | 1.3 |

- Leash distance = engage radius × factor: idle 325 u, guard 520 u.
- Reacquire only inside the engage radius → 30 % hysteresis band, no flip-flop.

## Implementation

1. `UnitOrderSettings.cs`: add `[SerializeField, Min(1f)] float squadronLeashFactor = 1.3f` + `SquadronLeashFactor` property, same header group as `squadronGuardEngageRadius`.
2. `Squadron.cs` Guard case: compute guard radius once; if `IsEngaging` and the target is farther than radius × factor from the guarded unit, and it is not the guarded unit's combat target → `Disengage()` + `ResumeCourse()`.
3. `Squadron.cs` default (idle) case: same check against `_pilot.LoiterCenter` and `Data.GuardRadius`.
4. `Squadron.cs` `ResumeCourse`: order `None` → `_pilot.FlyTo(_pilot.LoiterCenter)`; other orders keep `Loiter(_flight.Centroid)`. `Stop()` keeps setting a new post at the centroid.
5. Data: set `GuardRadius` 250 on all 34 squadron assets through Unity (`SerializedObject`, `SetDirty`, `SaveAssets`); change the C# default; update the tooltip only if wording becomes wrong.
6. `UnitOrderSettings.asset`: `squadronGuardEngageRadius` 400, `squadronLeashFactor` 1.3.
7. Docs: update the 800 row in [[Architecture/Enemy_AI_Objectives]].
8. `Squadron.cs` is 823 lines (limit 200). Keep the leash check in one small private method; no wider refactor in this plan.

## Edge Cases

- Target cloaks or dies while leashed → existing paths (`IsEngaging`, `Tick`) already handle it.
- Bomber on strikecraft keeps its interval search for a ship; leash still applies to the strikecraft target.
- Guarded ship moves away from the fight → leash measured from the ship's current position → squadron breaks off and rejoins.
- Idle squadron flying back to post: search stays anchored at `LoiterCenter`, so it re-engages only near the post.
- Hunt test `Hunt_WithoutVisibleTargets_PersistsBeforeAndAfterAKill` asserts `LoiterCenter == Centroid` after a kill → step 4 must not change Hunt.
- Enemy AI escorts get the smaller guard radius too → check `FleetCohesion` (radius 800) stragglers still join fights.
- Saves: no new saved state; post lives in the pilot and is rebuilt on load at the centroid.

## Tests (category `Squadrons`, not run locally)

- `SquadronTargetSelectorTests` (reflection harness at line 216): add
  - idle: target beyond leash → `_engaged` null, pilot travelling to old `LoiterCenter`;
  - idle: target inside leash → stays engaged;
  - guard: target beyond leash from guarded unit → back to escort;
  - guard bomber: guarded unit's combat target beyond leash → stays engaged;
  - idle kill → pilot returns to original post, not centroid.
- Inputs built in the test; no shipped balance values asserted.
- Review `EnemyUnitCommanderTests` and `Balance` suites for `GuardRadius` or 800 assumptions.

## Outcome (2026-11-21)

- Applied starting values as proposed: `GuardRadius` 250 (31 assets, not 34 — that is the live count; C# default 250), `squadronGuardEngageRadius` 400, `squadronLeashFactor` 1.3.
- `Squadron.BreakOffBeyondLeash` (Guard + idle) and `IsGuardedUnitTarget` (bomber exemption); `ResumeCourse` order `None` → `FlyTo(LoiterCenter)`. Hunt, Attack, AttackMove unchanged.
- `GuardRadius` also drives the AttackMove search → attack-move now searches 250 u around the squadron.
- Enemy AI escorts share the 400 guard radius (no split). Row updated in [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]] (the 800 row lives there, not in `Enemy_AI_Objectives`). `FleetCohesion` uses ship guard settings → unaffected.
- Tests: 6 cases added in `SquadronTargetSelectorTests` (idle leash in/out, idle kill → post, guard leash in/out, guarding bomber keeps the carrier's target). No other test reads `GuardRadius` or the 800 value. Unity compile passed; tests not run (local override).
- Reusable guidance: none — leash values are tuning, recorded here.
- Assets edited through Unity `SerializedObject` while [[TODOs/Bugs/Fighter_Interceptor_Rebalance]] had uncommitted edits in the same squadron assets; only the `GuardRadius` line was committed.

## Open (user)

- In-game gate: hero squadron idle reacts at radar range; guarding squadron returns to the ship; values 250 / 400 / 1.3 feel right.

## Files

- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Services/UnitOrders/UnitOrderSettings.cs`
- `Assets/Scripts/Entities/Squadron/Data/SquadronData.cs`
- `Assets/Settings/UnitOrderSettings.asset`
- `Assets/Settings/Data/Squadron/*SquadronData.asset` (34)
- `Assets/Scripts/Tests/Editor/Squadrons/SquadronTargetSelectorTests.cs`
