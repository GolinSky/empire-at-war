---
type: engineering-reference
system:
  - ai
  - capture
  - combat
verified: 2026-11-19
verification: Reviewed affected source and regression fixtures. Unity 6000.4.7f1
  compiled with zero errors. Test execution and battle acceptance pending.
---
# Enemy AI Objectives

## Rules

- `EnemyShipTaskGroupPlanner` owns stable ship membership. Each group has its own `EnemyStrategicContextBuilder`, `EnemyStrategicDecisionModel` and `EnemyTaskForceExecutor`: objective safety, strategy/recovery and orders.
- Station pressure is per player and independent of hostile/own strength ratio; every group builder shares `EnemyStationPressureTracker`. Visible threats or own-station durability loss can trigger `DefendBase`.
- Direct/weapon-tracked station attacks use effective weapon range + station footprint + attacker footprint. Other visible contacts use the narrower center/contact envelope.
- Station pressure latches after ≥0.5% maximum shields+hull loss from ≥2 observations spanning ≥3 s within 5 s, or a visible threatening contact. Release requires 30 s without visible threats or qualifying sustained loss; isolated damage does not refresh the latch; hidden attackers supply no live target or updated position.
- Autonomous groups stay in combat under local pressure and focus the weakest known local ship, with the nearest known mine as a fallback. Allied retreat requests require a safe nearby anchor. Placement recovery issues no fleet orders.

## Implementation

- Capture identity: `EnemyCaptureObjective` carries ID, type, owner, position, radius, progress and emergency-defense flag.
- Opening assignments fill nearest relay → authored main relay → next outer relay. The neighbour detachment can take the nearby home mine on departure. Ship groups filter by the assigned area. Squadrons cover these three areas in order and can keep pushing outer zones independently of the centre fleet.
- Pads can be captured before their point mines. The authored `IsMain` relay identifies the centre, including offset map layouts. Centre objectives bypass hostile-strength rejection while retaining geometry, hazard and stall checks.
- The main force captures or guards the centre. Local combat, home defense and decisive attacks can preempt centre duty. Other groups continue expanding and raiding.
- `EnemySiteConstructionController` checks every `3 s`. After the opening miners and level 2 order, eligible required satellites precede mining and optional defense. Centre pads have the highest construction priority. Centre mines wait for three operational centre satellites.
- 2026-10-31: removed `MinimumControlledZones` tuning and the satellite eligibility gate. `Loading_20261031_011454_ai.csv` showed repeated zone attempts starving facility selection under the previous quota. Combat interruptions remain possible; this change does not guarantee uninterrupted capture.
- Retain valid objective/active engagement; stopped ships outside its circle are reissued. Idle ships inside remain assigned.
- Capture diversion beyond the distance leash, or 5 s outside the circle without approach progress → AttackMove to the inside edge of the circle.
- No approach/capture progress for 20 s of executed capture → reject objective for 20 s and consider alternatives. `EnemyUnitCommander` reports the executed objective after each decision; defense/retreat/hold/rebuild pause the accumulated stall budget. A new objective or approach/capture progress resets it. Objective owner/availability changes invalidate commitment.
- Safety uses known target/route threat, ion cost and clear capture-area samples. It does not prove full physical path connectivity; stall recovery handles lack of movement.
- Allied retreat anchors must be within 1,000 units with a safe route and destination. Placement recovery uses a nearby living station, home or held relay. A pressured station can supply a placement origin while each candidate still passes safety and spawn checks.
- Allied production host remains authoritative; capture ownership uses team relationships.

### Mining objectives (2026-11-01)

- Visible or remembered hostile mines outrank other raid structures. Team intel retains observed stationary mines until removal. Unseen, unobserved mines are unavailable.
- A surplus purchased squadron that damages structures raids remembered hostile mines outside the centre, including home sites, spawned facilities and outer mines. It attacks when the mine is visible and uses its remembered position under fog. The three opening wings retain capture priority.
- Raid defenders: known ships and squadrons within `1000`; evaluate this group's `CombatMatchup.Advantage` against difficulty `RequiredAttackRatio`, not presence of any ship.
- Raid route: new and retained raids also pass `IsRouteSafe` (known hostiles at origin, within `1000` of the route, or near target vs `RetreatAdvantage`). Same check as capture/retreat objectives, without footprint clearance.
- A constructing or operational hostile mine with team intel is a strategic objective. The fleet attacks the facility before entering the circle. Once it is destroyed, the site can be captured normally.
- At least one eligible squadron remains available to capture while objectives exist. Scouts use surplus; carrier guards and escorts do not count as available.
- Site construction checks ownership, hostile presence and affordability without a minimum warship count.
- Source comparison and counter limits → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].

### Independent task groups (2026-10-31)

- Reserve the fastest eligible ship with a safe, unreserved objective as a capture detachment. A lone ship may detach for an unblocked mine. Retreating ships and ships currently attacking amid local threats are excluded. Combat groups reserve objectives before extraction.
- Capture detachments keep separate objectives and orders. Release follows 10 s without an available objective, including when another group reserves it. Station pressure brings detachments into the defense allocation.
- Opening combat groups join within 800 and split beyond 1,600. At six available combat ships, form the main force. Station pressure can form it earlier. Main-force membership survives travel separation.
- Group origin, local force, offensive strength, route safety, capture stall budget and recovery state are independent. Distant or nonassigned ships do not lend strength to a detached capture route.
- Executed capture objectives reserve their IDs for the decision pass; later ship groups select other objectives. Main force evaluates first once formed; capture detachment chooses another objective.
- Available squadrons cover threatened sites first, retain one valid assignment per objective, then choose the least-covered target by distance. Scouts, escorts and active carrier guards are excluded.
- Squadron reservations release on role removal, pooling or target ineligibility.
- Telemetry publishes the primary combat group's strategy/context with aggregate active/body/rallying counts and an action summary of all groups. It is not a separate telemetry row for each group.
- Implementation and acceptance owner → [[Done/Bugs/Enemy_AI_Independent_Task_Groups]].

### Target-specific strike forces (2026-11-01)

- `EnemyAttackTaskForce` selects persistent useful members for Hunt/Assault/Raid. Main force commits all its ships; small opening groups retain difficulty budgets.
- `EnemyTaskForceSelectionModel` scores combined target + defender kill time using current hull/shields, surviving-weapon strength and class-specific DPS; capped matchup advantage rewards useful defender counters.
- Nearby defenders come from team sightings within `1000` units. The visible unit target is added once at current strength; structure health is evaluated separately.
- Insufficient total available strike strength → reevaluate with offensive rules disabled before execution. A viable but scattered strike force waits for assembly; a force that cannot win does not block other objectives.
- Launch = at least 75% of selected ships in the cohesive body + primary-target damage + difficulty `RequiredAttackRatio`. Large capture forces (six or more committed ships) also wait for 75%. Distant members do not lend launch strength.
- Fill the committed budget with useful candidates; the 30 s target-clear goal no longer stops recruitment after one ship.
- Viable membership persists; losses/insufficient composition cause reselection. Nonselected ships Guard the strike anchor. Attack/formation offset stability remains in the existing executor.
- Highest target-effective DPS ship remains a primary attacker. Defender-specialist screens are bounded by defender clearing demand and use AttackMove at the objective; surplus capable ships attack the primary target.
- DefendBase/EngageLocal respond immediately without offensive assembly waiting; capture/retreat transitions clear attack membership.
- Scope limits: no task-owned production queue or multiple primary strike targets within one group. Station support comes from defensive positioning; static structure DPS, arcs and active abilities remain outside combat estimates.

### Production and deployment (2026-11-01)

- Combat profiles apply weapon caliber accuracy/damage modifiers, matching actual combat.
- Counter score uses matchup gain divided by max(price / available credits, capacity / free capacity), with existing preference/variety weights.
- Strikecraft cap: own force (live + pending) clears known hostile strikecraft in ≤ `30 s` → skip `UnitCombatProfile.IsStrikecraftSpecialist()` candidates (own-weapon average hull DPS vs strikecraft > vs warships). Current specialists: Carrack, CR70, Centax; not Venator/Resolute. Telemetry `counter-selection` → `strikecraftCovered`.
- Cap applies to counter selection only; the size-based `TrySelectShip` fallback is unchanged.
- Rank unlocked, type-limit-eligible ships before checking free population; wait for the chosen reinforcement instead of filling smaller gaps. Queue reservations count toward used population.
- Every difficulty purchases the mining catalog maximum first when a placement is available, then permits one level-1 corvette and prioritizes the level-2 upgrade. Other level-1 combat purchases are squadrons. A single scout can reveal mining space when none is available. Site construction reserves funds ahead of optional production.
- AI production rejects new second-station purchases and upgrades. Previously built stations load from saves. Paid pending backup-station requests and upgrades are refunded on the first production advance.
- Paid combat units remain in the build queue as a reserve. A capture, defense or raid rally can release a group of ready ships and squadrons. Critical pressure releases all ready units. Blocked launches retry without charging again. Targeted ship deployment searches living allied stations and held relay spawn areas by distance, preferring a station at equal distance.
- Scattered main force with no viable offensive target rallies instead of stopping permanently.
- Telemetry distinguishes selection, accepted/rejected purchase, completion, deployment/blockage, capacity waits and station health/loss. Existing purchase rows still mean attempts.

## Edge Cases

- Strategic assignment/recovery caches are transient. Restored ownership and orders are reassessed from live state; no save format change.
- Damage to an idle ship leaves its order unchanged. Shared direct Attack and AttackMove still choose targets through their normal order/radar logic.
- Old Providence `5065384297670371146` save proves Idle/no target, not a damage wake cause. New range/contact/order evidence → [[Tooling/Battle_Telemetry]].
- Large builder/executor classes predate this change and grew with objective policy; a later focused capture-selection extraction is preferable to unrelated refactoring here.
- Runtime acceptance and authored tests remain pending → [[TODOs/User_Acceptance_Queue]].

## Files

- Current policy and battle evidence → [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]].

- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs`; `EnemyTaskForceExecutor.cs`; `EnemyCaptureObjective.cs`.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`.
- `Assets/Scripts/Entities/BaseEntity/EntityFacades/IUnitCombatObserverFacade.cs`; ship/squadron installer bindings.
- [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]]; [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan - Research]].

## Edge Cases

- 2026-10-31 recovery fixes: retreat anchor checks ignore transient destination reservations through `IsPositionClear(..., includeReservations: false)` while retaining map-obstacle, ion-cost and known hostile-force checks. Formation placement still resolves ship reservations.
- Executed capture stall time pauses during other strategies; candidate selection alone never consumes the `20 s` budget. Cooldown remains `20 s` of wall time.
- Fix implementation and verification → [[Done/Bugs/Enemy_AI_Recovery_Review_Fixes]]. Runtime acceptance and authored regression execution remain pending.

### Endgame defense and capture (2026-11-01)

- Defense line faces visible or last known threat intel; no bearing → four-point station ring. Line distance includes station/ship clearance and weapon reach, bounded by station radius + max(1,500, last observed attacker weapon range + 300) units. Support slots spread over a threat-facing arc bounded to ±30°. One `IssueAttackMove(receivers, destinations)` call preserves those slots and one shared engagement for contact sharing/target distribution; regroup support defenders together when a slot needs updating. Interceptors use cached fleet formation offsets.
- Useful counter ships intercept visible targets inside that leash without assembly waiting. Contact loss/leash exit ends interception; completed defense-point orders are retained until the target changes or the point moves >200 units.
- Ongoing station damage with no defender target in range for 8 s → first advance jumps to known gap − own weapon range + ship clearance, bounded by the leash. Static clearance checks ignore transient navigation reservations.
- Critical = latched pressure AND (durability <50% maximum OR estimated loss time <120 s). Critical production compares affordable ships and squadrons fitting available population, bypassing ordinary fleet ratios; site mines/satellites and new superweapon charging purchases pause. Paid charging and firing continue.
- Critical production rejects candidates arriving after estimated station loss. Rank timely combat gain by matchup, available credits/population and arrival time. Arrival = matching request-ID queue delay + build time + base-speed travel to last known threat. No bearing → queue + build only. Squadron travel uses cruise speed. No timely candidate → save credits.
- `EnemyBuildQueue` serializes identical request IDs; different IDs advance in parallel. Human `ProductionQueueSnapshot` is not the AI queue. Normal capital reservations remain unchanged outside critical pressure.
- Each committed ship captures if its clearance fits the centre or one of four half-radius interior samples; only failed-fit ships AttackMove to perimeter points at objective radius + their own navigation radius. Capture receivers are batched per identical fitted point through `IssueAttackMove`, preserving compact formation and shared contacts/target distribution. A new/recovering member regroups its point's eligible ships; unchanged groups retain orders. Failed-fit escorts rejoin capture when fit becomes available. Strategic fit uses minimum clearance to admit any fitting capturer and ignores transient reservations; obstacle, ion and known hostile-route checks remain.
- Reservation API cannot identify owners; `transientReservation` means a ship reservation blocked the sampled point, not proof of a hostile or friendly owner. Tactical order placement still applies clearance.
- Implementation → [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]]. Tests authored; runtime battle acceptance remains pending.

### Historical patrol and recovery relocation (2026-11-14)

- The 2026-11-19 active-play plan supersedes fleet retreat, distant relocation and backup-station rules below. Patrol memory and placement checks remain reference material.

- Each task group applies local/fleet advantage drops immediately and smooths rises with a 4 s exponential average. A change in contact presence resets that channel.
- Retreat enters immediately at difficulty `RetreatAdvantage` and exits at ×1.25. EngageLocal/CaptureZone use an 8 s commitment. Cornered tactical choices, including Hold, use 2.5 s while the previous objective stays viable. Capture/patrol/anchor targets hold 10 s and require ≥20% better utility. Invalid targets release immediately.
- `TeamIntelRegistry` owns team `EnemyPatrolMemory`: 6×6 sector centers, relays, mining/satellite sites and unexpired intel. Sample allied visibility on AI ticks at 3 s cadence; score staleness × value / (1 + distance). Values: allied mine 5, neutral mine/relay/pad 3, hostile structure 2, empty sector/other intel 1.
- Squadron scout priority: allied marker → missing build space → common patrol/intel score → known hostile base. Normal+ detaches the cheapest available corvette when spare ships exist; defense, recovery and combat task-force need release it. Easy uses squadrons only.
- `EnemyRecoveryAnchor` updates in `AssessStation`, before task-group decisions. With station construction available, recovery triggers when the AI has no station, pressure is critical, or a weak fleet is under attack for 5 s. It clears after 20 s parity without pressure or successful station construction.
- Anchor candidates: safe home/own/allied zones/stations → sector seen within 60 s → neutral relay to capture first. Human and AI allies use the same alliance rules. No survivors → safe owned/allied/seen-sector building remains possible; neutral capture requires ships.
- Anchor utility interpolates near-own-fleet and far-from-known-threat by clamped smoothed fleet advantage. Shared retreat checks reject unsafe routes; pending relay capture takes priority for an eligible group. No anchor → defer rebuilding and scout safe unseen space.
- Free placement: anchor rings → safety-ranked allied zones → map rings from anchor; every candidate passes live spawn and hostile-reach checks. Known weapon reach +100 units is excluded (unknown reach 500); lost-home exclusion max(64 units, own default-home radius) plus placement footprint during recovery.
- Destroyed-structure exclusion = max(32 units, placement radius), last 10 positions. Sites keep fixed mine/satellite placement. Completion revalidates anchor ownership, contesting, recent sector visibility and safety; no candidate → existing refund path.
- A self-hosted AI can complete one backup station while its primary stands. Losing the primary before completion cancels and refunds the build when it reaches completion. A completed backup can keep production running after the primary falls.
- A hosted AI may build its first station during recovery while allied production is available. Each AI can complete its additional station slot once. Skip additional station purchases while critical.
- Modes using `DirectCombatAiState` do not run strategic recovery/patrol ship decisions; ordinary strategic AI, including applicable Survival AI slots, uses the same policy. Transient patrol/commitment/latch state is reassessed after load; no save schema change.
- Implementation/acceptance → [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]]; event fields → [[Tooling/Battle_Telemetry]]. Tests authored; battle acceptance remains user-gated.

### Historical patrol/recovery review corrections (2026-11-14)

- The 2026-11-19 active-play plan supersedes the withdrawal, backup-construction and reinforcement-rally rules below.

- A viable threatened owned site immediately preempts a normal capture target, bypassing target age and score margin; unsafe, reserved and stalled objectives remain excluded.
- A living station stays defended during backup construction unless smoothed local advantage falls below difficulty RetreatAdvantage (0.7 Medium/Hard; 0.8 Easy/UltraHard). Withdrawal retains the existing ×1.25 exit threshold; station loss still directs survivors to the recovery anchor.
- Reinforcement rally follows the actual DefendBase decision during recovery; pressure alone does not redirect withdrawn groups home.
- Station-pressure defense and viable retreat take priority over offensive commitment. See [[Done/Bugs/Enemy_AI_UltraHard_Collapse_And_Attack_Order_Loss]].
- Placement/scout map search still checks threats per candidate. Worst-case source complexity is candidate count × (sightings + entities + patrol points); profile a blocked large map before optimizing. No measured timing or performance pass claimed.
- Fix and verification → [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]].
