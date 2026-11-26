---
category: Features
status: review
created: 2026-11-26
tags:
  - enemy-ai
  - tactics
acceptance: pending-user
---
# Enemy AI Hit And Run

## Goal
- A group of anti-capital ships beats a hostile big ship by cycles: strike → the strike group pulls back together → recover → strike again.
- Works in open fights and while defending the AI's own station.
- Losses are replaced: the AI builds and spawns reinforcements into the strike; it does not give up the fight.
- Source: user, after the 2026-11-26 player-build battle. Production rebuild pool: [[Review/Bugs/Enemy_AI_Rebuild_And_Reserve]] (`EnemyRebuildRolePolicy`).
- Status 2026-11-26: implemented, compiled, related EditMode tests pass. Waiting for the user's in-game check → see Implemented and Acceptance.

## User Decisions (2026-11-26)
- No fixed group size; sized by target, time and money.
- Ships without `AntiCapital`/`Artillery`/`Dps` join when their capital damage is high.
- Thresholds live in a settings asset. Easy uses the tactic, less precisely.
- **No single ship runs.** A damaged striker keeps fighting until the strike group pulls back or it dies; the AI sends reinforcements instead.
- **Only the strike ships cycle.** They pull back and return together; every other ship of the group keeps its state orders.
- **Slower strikers → plain fight.** Target cruise speed ≥ slowest striker speed → no hit-and-run. Exception: the own-station safe point is closer to the strikers than the target's weapon range.
- **Hostile Field Repairs is read directly** from the hostile research (simplification for the AI, not treated as a cheat).
- **Ships left at the fight must survive the away period.** Hit-and-run is used only when they do, or when every group ship is a striker; else plain fight (rule 7). Agent recommendation, accepted by the user.
- **Minimum 2 strikers.** A strike group of 1 → plain fight. Agent recommendation, accepted by the user.
- Chased into station cover → strikers turn and fight at once.
- Recovery depends on the case (see Recovery).
- AI focusing retreating hostile ships: ignored for now.

## Open Questions (user)
- None. Rejected for the ships left behind: always hold whatever the losses; pull back with the strikers.

## What Exists Today (verified 2026-11-26)
- `EnemyStrategicDecisionModel.Evaluate` (`Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs:288`; override block `:372`): `standAndFight` = local threat and `LocalAdvantage <= profile.RetreatAdvantage`; `disengage` = `standAndFight && CanDisengage` → whole group `RetreatValue`, `Bypass = "disengage"`. Else `standAndFight` → **`EngageLocal`** ("fight in place"), all ships committed.
  - `RetreatAdvantage`: Easy 0.8, Medium 0.7, Hard 0.7, UltraHard 0.5 (`EnemyAiDifficultyProfile`, static code).
  - W49 `retreatLatched`: retreat holds until `HasReachedRetreatPoint`. `MIN_STATE_SECONDS` = 8 s on non-urgent switches.
  - Whole-group only; no return-to-strike step.
- `LocalAdvantage` (`BuildLocalForces`, `EnemyStrategicContextBuilder.cs:2963`): own units vs hostiles within `LOCAL_ENGAGEMENT_RADIUS` (1000) of the committed body's centre. A group ship farther away is not counted.
- `EnemyStrategicContextBuilder.FindWithdrawPoint` (`Services/Enemy/EnemyStrategicContextBuilder.cs:2715`): nearest uncontested held zone with no hostile near, else home. Null under station pressure, within 1000 of an allied station, inside a held zone, within `FleetCohesion.RADIUS`, or when `IsCutOff` (`WITHDRAW_BLOCK_REACH` 500).
  - → While defending the station `CanDisengage` is false: the group fights in place.
- `EnemyTaskForceExecutor`: state → orders via `IUnitOrderService` (`IssueAttack`, `IssueAttackMove`, `IssueRetreat`, `IssueGuard`, `IssueStop`). `ExecuteRetreat` sends every group ship to `RetreatAnchor`; skips a ship already on `Retreat` or arrived.
  - `EnemyAttackTaskForce.Assemble` picks strike ships with `EnemyTaskForceSelectionModel.Select` by `EffectiveDps` vs the target's class/hull/shields and `RequiredAttackRatio`.
  - `Select` adds ships greedily up to `maximumCount`; it does **not** stop at the first ready set.
  - Every state path reads `context.Ships`: `IssueAttackOrHunt` in 6 places (`StopRemaining`, assemble, push ships, support loop), `ExecuteDefense`, `ExecuteLocalEngagement`. Only `ExecuteSiteGuard` has an `excluded` list.
- Group wiring: `EnemyShipTaskGroupPlanner` owns the reserve (main force, injected singletons) and zone groups (`new` builder/decision model/executor per group, `:575`). New ships always join the reserve (`Reconcile`).
- Reinforcements: `EnemyShipTaskGroup.TryRequestReinforcementDrop` (needs `Executor.TryFindEngagedEnRoute`, 1 per 30 s) → `EnemyUnitCommander.RequestReinforcementDrop` (request lives 15 s, none in a last stand) → `EnemyFactionController.DropHeldReinforcement` lands **one finished held ship** beside the point.
  - `ResolveReinforcementRally`: station under pressure → new ships land at the station first.
- Ship side: `ShipOrderRunner.Retreat(destination)` cancels own Interdict, resets the weapon target, navigates (`Entities/Ship/Orders/ShipOrderRunner.cs:410`); no anchored-rejection. Order ends → `IdleState`: ship never moves, shoots what its guns reach.
- Order filter `AiOrderDeduplicator`: `IssueRetreat` is never filtered and clears the ship's memory → an Attack on the same target right after passes. Attack on the same target repeats only after 10 s (`REISSUE_SECONDS`) or when the order ended. A ship `IsEngagingEnRoute` takes only Retreat.
- Data per ship: `UnitCombatProfile` (`HullDps/ShieldDps/PiercingDps(ShipClass)`, `Hull`, `Shields`, `ShieldRegen`, `Speed`, `Price`, `Roles`); `context.ShipForces[ship]` is built live (current hull, shields, working weapons); live `IHealthModelObserver.HullPercentage`, `ShieldPercentage`; `IUnitCombatObserverFacade.WeaponRange`.
- `context.TargetForces[target]` = the target + known hostiles within 1000. Built only for fleet, base, assault, raid, local-threat and defense targets. Holds no repairers.
- Shields (`HealthComponent.Tick`): regenerate all the time, also under fire: `ShieldRegenerateValue` every `ShieldRegenerateDelay`. Zero with the shield generator lost, suppressed or recovering.
- Hull repair:
  - `FieldRepairService`: only with `ResearchType.FieldRepairs` done; 10 s with no weapon target and no hull/shield loss → 1 % hull/s. Destroyed hardpoints stay destroyed.
  - Research lookup: `IPlayerRegistry.TryGetResearch(owner, out IFactionResearchModelObserver)` → `GetCompletedTiers(ResearchType.FieldRepairs) > 0`. In `Services/Enemy` only `EnemyService` holds `IPlayerRegistry` today.
  - Repair satellites/platforms: `IRepairSourceFacade` (`CanRepair`, `Position`, `Profile.Radius`); rate `Profile.FractionPerTick / Profile.Interval`, `MaxHealersPerTarget`. `ForceComposition.AddRepairSource` / `HullRepairPerUnit` exist; only `EnemyZoneObjectiveCollector` fills them.
- Production: `EnemyCounterProductionModel.TrySelect` already has an emergency path for a **critical station**: drops candidates arriving after `timeToLossSeconds`, scores `gain × preference / (cost × ArrivalSeconds)`. `GetArrivalSeconds` = queue + build + travel to the station threat.
- Decision cadence (`DecisionInterval`): Easy 4 s, Medium 2.5 s, Hard 1.25 s, UltraHard 0.5 s. The cycle model reacts no faster.
- Speeds (cruise, ship data): heavy cruisers 25–45, capitals 18–40, battlecruisers 18–35, dreadnoughts 13–20.

## Verification (2026-11-26, against source)
### Corrected
- Line refs: `Evaluate` `:288` (was `:374`), `FindWithdrawPoint` `:2715`.
- "Keeps `DefendBase`": wrong when outmatched. `LocalAdvantage <= RetreatAdvantage` near the station → state is `EngageLocal`, not `DefendBase`. The cycle must run under both and key on the target, not the state.
- "Smallest set that passes `IsReady`": `Select` does not do this → new sizing code (G4).
- "Hostile gravity well traps the group": wrong. A well anchors only its caster and blocks reinforcement spawns inside its range (`BattleAreaLayer.SpawnBlock`). Ships fly through it.
- Emergency buying: the critical-station half already exists; only the fleet half is new.
- No AI settings asset exists; all difficulty tuning is static code. `EnemyHitAndRunSettings` is the first.

### Gaps found
- **G1 Rule 4 needs shield regen.** The target regains `ShieldRegen × time away` shields every cycle; strikers must break them again.
- **G2 Break contact.** Decided: slower strikers → plain fight (rule 5). Damage taken on the way out counts against the strikers.
- **G3 Strikers out of the state orders.** Decided approach below (Fit): one filtered ship list, not per-path exclusion lists.
- **G4 Sizing**: add ships by `EffectiveDps` rank until `EnemyTaskForceSelectionModel.IsReady` passes; cap at the executor's `StrikeLimit` (focus-fire slots × `FocusFireScale`).
- **G5 Hostile well auto-raise**: a hostile interdictor raises its well when it sees a ship on a `Retreat` order in range (`GravityWellSettings.HasInterdictionTarget`) → the pull-back can block the AI's own reinforcement drop at the safe point. Drop point must be outside known hostile wells, else the station.
- **G6 Retreat-order side effects**: a striker on `Retreat` → `IsRetreating` true in the context builder (keeps a stale `_retreatPoint` outside station pressure); `CollectPool` skips reserve ships on `Retreat`. Zone blocking (`NoteRetreats`) reads the state `RetreatValue`, so it is not hit. Check both in step 4.
- **G7 Recovery that never ends**: a striker with its shield generator lost has regen 0 and never reaches `ResumeShieldFraction`. Count only ships that can regenerate; add `MaxRecoverSeconds`.
- **G8 Reinforcement**: the drop is one held finished ship; nothing is bought for it. With no held ship the request lapses after 15 s. A dropped ship joins the reserve, not a zone group.
- **G9 Hostile repairers** are not in `TargetForces` → rule 4 must collect hostile `IRepairSourceFacade` within `Profile.Radius` of the target itself.
- **G10 Paths that skip `Execute`**: allied marker request (`ExecuteMarker`), `Rally`, `Turtle`, `LastStand` → cycle goes `Idle`.
- **G11 Strike target** may differ from `LocalThreatTarget` (closest hostile) and `DefenseTarget` → the builder must add its `TargetForces` entry.
- **G12 Rating while strikers are away** (from "strikers only"): strikers > 1000 from the body drop out of `LocalAdvantage` → `standAndFight` / `disengage` can fire for the ships left behind. Strikers behind the own station stay inside 1000 and still count. → rule 7 keeps the tactic off when the ships left behind cannot hold.
- **G13 Group shape while strikers are away**: `IsRegrouped` reads false; the main force in `Hold` then calls `Rally`, which would pull the strikers back. The cycle only runs in fighting states, so check that no path reaches `Rally` with an active cycle.

## When The AI Uses Hit-And-Run (accepted 2026-11-26)
All must hold:
1. **Where**: the group is in a fight near its own station under attack (`DefendBase`, or `EngageLocal` forced by `standAndFight`), in a local fight (`EngageLocal`), or meets a defending big ship on a hunt/assault (`HuntFleet`, `AssaultBase`). Not capture detachments, not a last stand, not while answering an allied request.
2. **Target**: a visible (or confidently tracked) hostile warship of class `HeavyCruiser`, `Capital`, `BattleCruiser` or `Dreadnought`. Smaller ships and strikecraft are fought normally.
3. **A straight fight is bad**: the group's strength vs the target plus its nearby support (`CombatMatchup.Advantage(group, TargetForces[target])`) is below the settings value `StraightFightAdvantage` (lose, or win with heavy losses).
4. **One strike pays** (formula below).
5. **A safe point exists** (see Where To Pull Back), reachable and not cut off, **and the strikers can break contact**: slowest striker `Speed` > target `Speed`, or the own-station safe point is closer to the strikers than the target's `WeaponRange`.
6. The target is not under hostile station or platform guns the group cannot beat.
7. **The ships left behind hold**: `CombatMatchup.TimeToDestroy(TargetForces[target], non-strikers)` > `awaySeconds`, or the group has no non-striker. At least 2 strikers.
- Fails 3 (group already wins) → plain fight. Fails 4 or 5 → plain fight or existing `disengage`. Fails 7 → plain fight.
- Rule 7 is re-checked before every pull-back: fails → the strikers stay and the cycle goes `Idle`. Fails while the strikers are away → they return at once (`Strike`), then `Idle`.
- Below `RetreatAdvantage` away from the station: an active cycle that still passes 4 and 5 suppresses `disengage`; otherwise `disengage` wins and the cycle goes `Idle`.

### Rule 4 formula (draft)
- `strikeSeconds` = striker shields above the pull-back line / (hostile DPS on strikers − striker shield regen).
- `breakSeconds` = target shields / (striker shield DPS − target regen); ≤ 0 net → fails.
- `hullDamage` = piercing × `breakSeconds` + (hull + piercing DPS) × (`strikeSeconds` − `breakSeconds`).
- `awaySeconds` = travel out + recovery + travel back (distances / slowest striker `Speed`).
- `healed` = hostile repairers in range × rate × `awaySeconds` + (target's owner has Field Repairs: 1 % × max hull × (`awaySeconds` − 10 s)).
  - Field Repairs counts only while nothing of the AI's keeps the target under fire; ships left at the fight stop it (the 10 s calm timer never fills).
- Pays when `strikeSeconds > breakSeconds` and `hullDamage > healed`. Target shields at the next strike = min(max, regen × `awaySeconds`).
- Reuse `CombatMatchup.TimeToDestroyClass` terms and `ForceComposition.DamageRates`; rate from `context.ShipForces` (live weapons).

## Strike Group Selection
1. Rank group ships by damage against the target: `EffectiveDps` (hull + shield + piercing vs target class, current hull/shields), same math as `EnemyAttackTaskForce.TargetDps`. Roles (`AntiCapital`, `Artillery`, `Dps`) break ties. No ship names, no type ids.
2. Size: add by rank until `EnemyTaskForceSelectionModel.IsReady` passes vs the target and its support; cap at `StrikeLimit`. No fixed maximum; minimum 2 (fewer → plain fight).
3. Keep members while valid (`_retained` as in `Assemble`). Membership is frozen from `PullBack` until the next `Strike`, so no ship changes side mid-cycle.

## Strike, Pull-Back, Recovery
- Settings: `EnemyHitAndRunSettings` ScriptableObject in `Assets/Settings`, one entry per difficulty, read through a pure C# interface (models hold no `UnityEngine` types). Load like `Assets.Load<MiniMapData>(nameof(MiniMapData))` in `SkirmishScope`; pass through `EnemyShipTaskGroupPlanner` to each group. Read `AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE` and `PROJECT_ORGANIZATION` first.
  - Fields (names draft): `StraightFightAdvantage`, `PullBackShieldFraction`, `ResumeShieldFraction`, `ResumeHullFraction`, `MinPhaseSeconds`, `MaxRecoverSeconds`, `ReactionDelaySeconds` (Easy: larger delay, later pull-back).
- **Strike**: strikers attack the target together through `IssueStrikeAttack` (focus-fire caps as today).
- **Pull back (the whole strike group)**: when the strikers' average shield fraction drops below `PullBackShieldFraction`, or when the strike stops paying (rule 4). No single-ship escape; a damaged striker stays with the strike group. Non-strikers keep their state orders.
  - Order: `IssueRetreat(receivers, point)` once per ship; skip ships already on `Retreat` or arrived (guard as in `ExecuteRetreat`).
- **Recover**: no order needed; the retreat ends in `IdleState`. The safe point must be outside hostile weapon reach, or Field Repairs never starts (needs 10 s with no target).
  - Station under heavy attack (`HasStationPressure` / `IsStationCritical`) → wait only until shields reach `ResumeShieldFraction`.
  - Ordinary battle, and own Field Repairs research done or a working own/allied repairer covers the safe point → wait for shields and hull to reach `ResumeShieldFraction` / `ResumeHullFraction`.
  - Ordinary battle, no research and no repairer → shields only.
  - Ships with no shield regeneration are not waited for; `MaxRecoverSeconds` ends any recovery.
- **Chased**: the target follows into station/platform/repairer cover → strike at once, no waiting.
- Hysteresis: resume fraction > pull-back fraction; `MinPhaseSeconds` stops flip-flops.
- **Reinforce**: every striker lost → request a drop at the safe point (second source for `TryRequestReinforcementDrop`, beside `TryFindEngagedEnRoute`). Under station pressure new ships land at the station anyway. Dropped ships join the reserve; a zone group's strike gets them only through the allocator. A new ship becomes a striker by rank at the next `Strike`.

## Where To Pull Back
First valid point; each must pass `IsCutOff` and lie outside known hostile weapon reach:
1. Own/allied repairer (`IRepairSourceFacade.CanRepair`), inside `Profile.Radius`.
2. Own station: far side from the threat, inside the station's guns. Only option while defending. Point = station centre − threat bearing × (station `NavigationRadius` + ship `NavigationRadius` + margin); bearing from `StationPressure.ThreatX/Z` or the target; validate with `context.IsDefensePointClear` (as `ExecuteDefense`).
3. Main fleet body (reserve group's centre) when it is not this group.
4. `FindWithdrawPoint` result (held zone or home).
- New context-builder method `FindStrikeFallbackPoint(threat)`. `FindWithdrawPoint` stays as is for whole-group `RetreatValue`.

## Emergency Anti-Capital Buying (time + money)
- Exists: critical station → arrival-weighted counter pick (`TrySelect(isStationCritical, timeToLossSeconds)`).
- New: the same path when the **fleet** dies fast with no station pressure.
  - Time = `CombatMatchup.TimeToDestroy(hostile local force, own local force)` from the context builder, exposed through the commander's state provider (as `TryCopyOutmatchingArmada`).
  - Rename the `TrySelect` flag to cover both cases; pass min(station, fleet) time-to-loss.
  - `GetArrivalSeconds` adds travel only to a station threat → add travel to the strike's safe point.
- Money = damage vs the hostile big ships per credit (already the counter score).
- Rebuild pool (`EnemyRebuildRolePolicy`) stays as is.

## Fit With Strategic States And Orders
- No new strategic state or rule. A per-group `EnemyStrikeCycleModel : Model` (`Entities/EnemyFaction/Models/Combat/`), owned by `EnemyShipTaskGroup`.
  - Phases: `Idle → Strike → PullBack → Recover → Strike`; `Idle` when the target dies, is lost, or `disengage` fires.
  - Why: a new state would fight rule scores, the 8 s hold and the W49 latch. Avoid: a second whole-group retreat path.
- **Strikers out of the state orders (decision)**: while the cycle is active, the executor's state paths see `context.Ships` without the strikers; `ApplyStrikeCycle` orders the strikers from a separate `context.Strikers` list. `Receivers` and `ShipForces` keep every ship.
  - Why: one filter point instead of exclusion checks in 8+ places. Avoid: `excluded` parameters through `IssueAttackOrHunt`, `ExecuteDefense`, `ExecuteLocalEngagement`, `StopRemaining`.
  - The state's committed ship count is clamped to the remaining ships.
  - `Idle` → `context.Ships` is the full list again; the state places the former strikers afresh (`ForgetShip`).
- Decision model: snapshot gets `StrikePhase`; an active, paying cycle blocks `disengage`. Rule 7 guarantees the ships left behind hold for the away period, so no separate rating for them.
- Hostile research: inject `IPlayerRegistry` into `EnemyStrategicContextBuilder`; pass a `targetSelfRepairs` flag into the cycle model.
- Orders only through `IUnitOrderService` → `ShipOrderRunner`. Attack after Retreat passes the filter (verified).
- Telemetry: `EnemyAiTelemetry.RecordEvent(..., "strikeCycle", "group=;phase=;strikers=;target=;reason=")` on each phase change; `ShipOrderTelemetry.Detail("path=strike")` around the orders.

## Implementation Steps
- [x] 0a. User answers 2026-11-26: strikers only; slower strikers → plain fight; hostile research read directly.
- [x] 0b. 2026-11-26: ships left behind must survive the away period (rule 7); minimum 2 strikers.
- [x] 1. `EnemyHitAndRunSettings` asset + interface + loading through the planner.
- [x] 2. `EnemyStrikeGroupRules`: ranking + sizing (G4).
- [x] 3. `EnemyStrikeCycleModel`: use conditions, rule 4 formula (G1), break contact (G2), left-behind check and 2-striker minimum (rule 7, G12), phases, recovery cases, hysteresis, timeouts (G7).
- [x] 4. Context builder: target pick + its `TargetForces` entry (G11), hostile repairers (G9), hostile Field Repairs flag, `TryFindStrikeFallbackPoint`, snapshot field, `Strikers` list; `IsRetreating` ignores strike ships (G6).
- [x] 5. Executor: `ApplyStrikeCycle`, filtered ship list (G3), reinforcement request (G8), `Idle` on marker/rally/turtle/last stand (G10, G13), telemetry.
- [x] 6. Decision model: `StrikePhase` blocks `disengage`.
- [x] 7. Production: fleet time-to-loss into the existing emergency path.
- [x] 8. Tests written and run (see Implemented). User in-game check pending.

## Implemented (2026-11-26)
- Models (`Entities/EnemyFaction/Models/Combat/`): `EnemyStrikePhase`, `EnemyHitAndRunTuning`, `IEnemyHitAndRunSettings`, `EnemyStrikeGroupRules`, `EnemyStrikeCycleInput`, `EnemyStrikeCycleModel`, `EnemyStrikeFallbackKind` / `Candidate` / `Rules`.
- Services (`Services/Enemy/`): `EnemyHitAndRunSettings` (ScriptableObject), `IEnemyStrikeSituation` (implemented by `EnemyStrategicContextBuilder`), `EnemyStrikeCycleAssessor` (one per `EnemyShipTaskGroup`).
- Asset: `Assets/Settings/Data/AI/EnemyHitAndRunSettings.asset`; Addressables group `Model`, address and mapping key `EnemyHitAndRunSettings`; loaded in `SkirmishScope`.
- Flow per decision: `Build` → `EnemyShipTaskGroup.UpdateStrikeCycle` → snapshot `WithStrikePhase` → decision → cycle stops on a request or a non-fight state → `StrikeCycle.Apply(context)` → `Execute` → `ApplyStrikeCycle`.
- Fight states: `DefendBase`, `EngageLocal`, `HuntFleet`, `AssaultBase`, or an outmatched local fight (the decision then fights in place because the cycle blocks `disengage`).
- Executor: strike ships keep own battle slots (`BattleSlots`), so a state strike on another target does not re-slot them. Order telemetry: `path=strike;retarget=…`, `path=strikePullBack`.
- Telemetry event `strikeCycle`: `group=;phase=;strikers=;target=;reason=`. Reasons: `start`, `lowShields`, `arrived`, `recovered`, `recoverTimeout`, `pullBackTimeout`, `chased`, `leftBehind`, `groupWins`, `fewStrikers`, `noFight`, `noTarget`, `noShieldRegen`, `noSafePoint`, `cannotBreakContact`, `targetCovered`, `strikeDoesNotPay`, `state`.
- Production: `IEnemyAiStateProvider.TryGetFleetEmergency` (shortest `LocalTimeToLossSeconds` of the groups < `FleetEmergencySeconds`, no station pressure) → `EnemyCounterProductionModel.TrySelect(isEmergency, min time-to-loss)`; `GetArrivalSeconds` adds travel to the safe point or the group's ships.
- Added setting: `FleetEmergencySeconds`.

### Settings values (first pass, to tune in game)
| | Easy | Medium | Hard | UltraHard |
|---|---|---|---|---|
| `straightFightAdvantage` | 1.0 | 1.15 | 1.25 | 1.3 |
| `pullBackShieldFraction` | 0.1 | 0.2 | 0.25 | 0.3 |
| `resumeShieldFraction` | 0.7 | 0.8 | 0.85 | 0.9 |
| `resumeHullFraction` | 0.6 | 0.7 | 0.8 | 0.9 |
| `minPhaseSeconds` | 8 | 6 | 6 | 6 |
| `maxRecoverSeconds` | 75 | 60 | 60 | 60 |
| `reactionDelaySeconds` | 4 | 2 | 1 | 0 |
| `fleetEmergencySeconds` | 30 | 45 | 60 | 75 |

### Differences from the plan
- Rule 4 rates the steady-state strike: strikers start at the resume line; target shields = min(max, max(current, regen × away)).
- Rule 7 while away compares survival with the rest of the absence (never less than the way back). Fails → `Idle`; the state orders send the strikers back to the fight.
- Shield and hull averages count only strikers whose shields regenerate; none → `Idle` (`noShieldRegen`).
- Chased = the target's weapons reach the safe point during `PullBack` / `Recover`.
- Pull-back arrival = no striker on a `Retreat` order from the decision after the pull-back started.
- G5: no own well check. The drop uses the existing rally → nearest valid spawn point path. Not verified in game.
- G6 `CollectPool`: unchanged. A reserve striker on `Retreat` stays out of zone allocation, as before.
- `FindWithdrawPoint` (held-zone candidate) returns null near a station, inside a held zone or under station pressure → there only repairer, station or main body can be the safe point.

### Tests run (EditMode, 2026-11-26)
- Category `EnemyAi`: 843 / 843 passed (before the last `fewStrikers` guard edit).
- After that edit: `EnemyUnitCommanderTests` 198 / 198, `EnemyStrike*` 54 / 54, `CombatMatchupTests` 44 / 44, `RealEntityCompositionTests` 37 / 37, `VContainerCompositionTests` 7 / 7.
- New fixtures: `EnemyStrikeGroupRulesTests`, `EnemyStrikeCycleModelTests`, `EnemyStrikeFallbackPointTests`. Extended: `EnemyStrategicDecisionModelTests`, `EnemyUnitCommanderTests`, `AiOrderDeduplicatorTests`.
- Not written: Navigation tests with the real ship runner (point behind a station reachable; Retreat → Attack → Retreat leaves no stuck ship); production-strategy test for the fleet emergency; context-builder tests for target pick and safe-point search (pure rules are tested).

## Acceptance (user, in game)
- AI group with 2+ anti-capital ships vs a stronger capital: strikers attack, pull back together on low shields, return; `_ai.csv` shows `strikeCycle` rows.
- Same while the AI defends its station: strikers recover behind the station; other ships keep defending.
- Lost striker → a held ship lands at the safe point (`reinforcementDrop` row).
- Tune `EnemyHitAndRunSettings.asset` per difficulty.

## Tests To Write
Read `Rules/TEST_GUIDELINES` first. Logic tests build their own profiles with literal numbers; tests on shipped data assert wiring only.
- `EnemyAi`:
  - `EnemyStrikeGroupRulesTests`: ranked by capital damage; roles break ties; non-role high-damage ship joins; size stops at first ready set; capped at the strike limit; members retained; membership frozen from pull-back to the next strike.
  - `EnemyStrikeCycleModelTests`: each use condition (1–7) turns the tactic off; one striker → off; ships left behind die before the strikers return → off; no ship left behind → on; rule 7 fails while the strikers are away → they return; target as fast as the slowest striker → off; same case with the station point inside the target's weapon range → on; target shield regen ≥ striker shield damage → off; heal ≥ hull damage → off; target owner with Field Repairs raises the heal; Strike → PullBack on low average shields; no single-ship pull-back; recovery cases (station pressure → shields only; research or repairer → shields + hull; neither → shields only); ship without shield regen not waited for; `MaxRecoverSeconds` ends recovery; chased into cover → Strike; min phase time; Idle on target death / lost intel / marker request.
  - `EnemyStrikeFallbackPointTests`: repairer > station > main body > held zone; cut-off skipped; defending → behind the station.
  - `EnemyStrategicDecisionModelTests` (extend): pull-back does not trigger `disengage`; state unchanged by the phase; failed rule 4 → `disengage` again.
  - `EnemyUnitCommanderTests` (extend): strikers get Attack / Retreat per phase; non-strikers keep the state's orders in `DefendBase`, `EngageLocal` and `HuntFleet`; no state path orders a striker; Retreat not re-issued each cycle; cycle end → former strikers get state orders again; lost striker → reinforcement drop at the safe point; no drop in a last stand.
  - `AiOrderDeduplicatorTests` (extend): Attack after Retreat on the same target passes (no test covers it today).
  - `EnemyProductionDecisionModelTests` (extend): short fleet time-to-loss → faster-arriving pick; long time → pick unchanged; critical-station behaviour unchanged.
- `Navigation`: point behind a station is reachable outside the station hull obstacle; Retreat → Attack → Retreat cycle leaves no stuck ship (review `Ships` tests first).
- Run only `EnemyAi`, `Navigation` (and `Ships` if runner tests change) via `Tools/Tests/Run-EditModeTests.ps1 -Category`.

## Edge Cases
- Ion-disabled striker (no engines) → stays and fights.
- Target cloaks or leaves vision → `Idle` after intel confidence drops (`HostileIntelModel.GetConfidence` ≤ 0).
- Repairer destroyed during recovery → next point; recovery case re-checked.
- Hostile gravity well over the safe point → reinforcement drop moves to the station; the strikers still pull back (wells do not stop ships).
- Own interdictor among the strikers: `Retreat` ends its well; it re-raises on its auto rule.
- Station lost mid-cycle → fallback skips step 2.
- Striker `IsEngagingEnRoute` (hit on an attack-move) → takes only Retreat; gets the Attack after its fight ends.
- Strikers lose weapons for good (hardpoints never heal) → rule 4 re-rated each cycle from live `ShipForces`; fails → plain fight.
- Target dies or switches during `PullBack` → `Idle`; the state's orders resume next cycle.
- Target turns on the ships left behind while the strikers recover → rule 7 re-check; they cannot hold → strikers return at once.
- Every group ship is a striker → nothing is left behind; the group behaves as one unit.

## Files
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`
- `Assets/Scripts/Entities/EnemyFaction/Models/Combat/UnitCombatProfile.cs`, `ForceComposition.cs`, `CombatMatchup.cs`, `EnemyTaskForceSelectionModel.cs`, `EnemyCounterProductionModel.cs`
- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs`, `EnemyTaskForceExecutor.cs`, `EnemyAttackTaskForce.cs`, `EnemyShipTaskGroup.cs`, `EnemyShipTaskGroupPlanner.cs`, `EnemyUnitCommander.cs`, `EnemyProductionStrategy.cs`, `EnemyAiTelemetry.cs`
- `Assets/Scripts/Entities/EnemyFaction/Controllers/EnemyFactionController.cs` (`DropHeldReinforcement`), `Assets/Scripts/Services/VContainer/Scopes/Players/AiPlayerScope.cs`
- Read only: `Entities/Ship/Orders/ShipOrderRunner.cs`, `Entities/Ship/StateMachine/IdleState.cs`, `Services/Enemy/AiOrderDeduplicator.cs`, `Services/Repair/FieldRepairService.cs`, `IRepairSourceFacade.cs`, `Services/Player/IPlayerRegistry.cs`, `Components/Health/HealthComponent.cs`, `Services/ShipAbilities/Abilities/GravityWell/GravityWellSettings.cs`

## Not Verified
- Station weapon range and whether the far side of the station is inside it.
- Ship turn time when leaving a fight (affects break-contact time).
- How the allocator treats a zone group whose strikers sit at a safe point for a long recovery.

## Reusable Guidance
- No reusable guidance yet: thresholds are untuned and the tactic is unproven in game. Extract after acceptance.
