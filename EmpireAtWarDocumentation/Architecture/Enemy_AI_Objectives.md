---
type: engineering-reference
system:
  - ai
  - capture
  - combat
verified: 2026-11-14
verification: Live-source and DI review, Unity script compilation/import; tests
  and Play Mode not run. Patrol/recovery battle acceptance pending.
---
# Enemy AI Objectives

## Rules

- `EnemyShipTaskGroupPlanner` owns stable ship membership. Each group has its own `EnemyStrategicContextBuilder`, `EnemyStrategicDecisionModel` and `EnemyTaskForceExecutor`: objective safety, strategy/recovery and orders.
- Station pressure is per player and independent of hostile/own strength ratio; every group builder shares `EnemyStationPressureTracker`. Visible threats or own-station durability loss can trigger `DefendBase`.
- Direct/weapon-tracked station attacks use effective weapon range + station footprint + attacker footprint. Other visible contacts use the narrower center/contact envelope.
- Station pressure latches after ≥0.5% maximum shields+hull loss from ≥2 observations spanning ≥3 s within 5 s, or a visible threatening contact. Release requires 30 s without visible threats or qualifying sustained loss; isolated damage does not refresh the latch; hidden attackers supply no live target or updated position.
- Retreat recovery requires cohesion for normal local action, not global parity. Ordinary offensive rules retain their fleet-advantage thresholds.
- Lost home + scattered fleet + viable local objective → rally through capture/local engagement. No safe anchor or viable action → explicit Hold/rebuild.

## Implementation

- Capture identity: `EnemyCaptureObjective` carries ID, type, owner, position, radius, progress and emergency-defense flag.
- No reinforcement-zone quota on any difficulty. Rank safe zones and facilities by distance/ion cost, regardless of owned capturable-zone count; home zones remain excluded from the diagnostic count.
- Satellite capture requires ownership of every mining site at the same `PointId`, with at least one mine present. Emergency defense of an owned site bypasses this prerequisite; safety still applies.
- Continuous expansion scores `0.6`, ahead of ordinary hunting/assault (`0.5`) and local engagement (`0.55`); overwhelming-advantage attacks (`0.75`), retreat (`0.8`) and base defense (`0.9`) retain priority.
- Each strategic decision scans capture targets; capture completion releases the objective for the next safe target. `EnemySiteConstructionController` checks every `3 s` and prioritizes building mines on owned, uncontested sites when affordable; existing income and production systems fund further ships.
- 2026-10-31: removed `MinimumControlledZones` tuning and the satellite eligibility gate. `Loading_20261031_011454_ai.csv` showed repeated zone attempts starving facility selection under the previous quota. Combat interruptions remain possible; this change does not guarantee uninterrupted capture.
- Retain valid objective/active engagement; stopped ships outside its circle are reissued. Idle ships inside remain assigned.
- Capture diversion beyond closest approach + max(800, radius + 2 × ship navigation radius) → return movement; valid engagements are retained.
- No approach/capture progress for 20 s of executed capture → reject objective for 20 s and consider alternatives. `EnemyUnitCommander` reports the executed objective after each decision; defense/retreat/hold/rebuild pause the accumulated stall budget. A new objective or approach/capture progress resets it. Objective owner/availability changes invalidate commitment.
- Safety uses known target/route threat, ion cost and clear capture-area samples. It does not prove full physical path connectivity; stall recovery handles lack of movement.
- Retreat anchors share recovery safety: safe home/allied relay normally; active relocation uses the committed recovery anchor. Dead default zones are excluded.
- Allied production host remains authoritative; capture ownership uses team relationships.

### Mining objectives (2026-11-01)

- Visible hostile mining facilities outrank other raid structures; cached non-mining targets yield to eligible mines.
- Mining raid utility `0.66` exceeds ordinary capture plus hysteresis (`0.65`). Threatened-site defense disables the mining rule; base defense, retreat and decisive attack remain higher.
- Raid defenders: known ships and squadrons within `1000`; evaluate this group's `CombatMatchup.Advantage` against difficulty `RequiredAttackRatio`, not presence of any ship.
- Raid route: new and retained raids also pass `IsRouteSafe` (known hostiles at origin, within `1000` of the route, or near target vs `RetreatAdvantage`). Same check as capture/retreat objectives, without footprint clearance.
- Built/construction mines must be destroyed before capture becomes eligible. Site location/ownership is globally known; direct building raids require current visibility.
- At least one eligible squadron remains available to capture while objectives exist. Scouts use surplus; carrier guards and escorts do not count as available.
- Safe owned empty mines may build with fewer than two warships; ownership, contesting and affordability remain checked by capture construction. The two-warship gate still applies to satellite construction.
- Source comparison and counter limits → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].

### Independent task groups (2026-10-31)

- With more than one ship, reserve the fastest eligible ship with a safe, unreserved capture target as a capture detachment. Combat groups reserve objectives before extraction; the reused detachment executes next decision tick. Attack/Retreat ships are excluded from initial selection.
- Capture detachment keeps its own capture/retreat/hold/rebuild policy; station pressure recalls it. Release requires 10 s with no available objective and no recovery; reservations by another group also consume that grace period so idle ships rejoin combat.
- Opening combat groups join within 800 and split beyond 1600. At six available combat ships, retain one main force and recruit distant nonrecovering groups; station pressure concentrates available ships immediately. Main-force membership survives travel separation.
- Group origin, local force, offensive strength, route safety, capture stall budget and recovery state are independent. Distant or nonassigned ships do not lend strength to a detached capture route.
- Executed capture objectives reserve their IDs for the decision pass; later ship groups select other objectives. Main force evaluates first once formed; capture detachment chooses another objective.
- Available squadrons cover threatened sites first, retain one valid assignment per objective, then choose the least-covered target by distance. Scouts, escorts and active carrier guards are excluded.
- Squadron reservations release on role removal, pooling or target ineligibility; satellite capture retains the owning-mine prerequisite.
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
- Home defense → default-home deployment only; blocked arrivals stay paid/reserved and retry. Otherwise choose valid home/held relay nearest the main fleet rally.
- Main capture scoring favors relays near the midpoint of home/enemy stations; detached captures retain distance/ion scoring.
- Scattered main force with no viable offensive target rallies instead of stopping permanently.
- Telemetry distinguishes selection, accepted/rejected purchase, completion, deployment/blockage, capacity waits and station health/loss. Existing purchase rows still mean attempts.

## Edge Cases

- Strategic assignment/recovery caches are transient. Restored ownership and orders are reassessed from live state; no save format change.
- Player Idle/Guard behavior is unchanged. No damage-triggered pursuit or separate Providence facing fix was introduced.
- Old Providence `5065384297670371146` save proves Idle/no target, not a damage wake cause. New range/contact/order evidence → [[Tooling/Battle_Telemetry]].
- Large builder/executor classes predate this change and grew with objective policy; a later focused capture-selection extraction is preferable to unrelated refactoring here.
- Runtime acceptance and authored tests remain pending → [[TODOs/User_Acceptance_Queue]].

## Files

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
- Emergency ranking uses useful combat value / arrival seconds; useful candidates arriving before station loss outrank late candidates. Arrival = matching request-ID queue delay + build time + base-speed travel to last known threat; no bearing → queue + build only. Squadron travel uses cruise speed. No known hostile composition → rank combat value / arrival seconds.
- `EnemyBuildQueue` serializes identical request IDs; different IDs advance in parallel. Human `ProductionQueueSnapshot` is not the AI queue. Normal capital reservations remain unchanged outside critical pressure.
- Each committed ship captures if its clearance fits the centre or one of four half-radius interior samples; only failed-fit ships AttackMove to perimeter points at objective radius + their own navigation radius. Capture receivers are batched per identical fitted point through `IssueAttackMove`, preserving compact formation and shared contacts/target distribution. A new/recovering member regroups its point's eligible ships; unchanged groups retain orders. Failed-fit escorts rejoin capture when fit becomes available. Strategic fit uses minimum clearance to admit any fitting capturer and ignores transient reservations; obstacle, ion and known hostile-route checks remain.
- Reservation API cannot identify owners; `transientReservation` means a ship reservation blocked the sampled point, not proof of a hostile or friendly owner. Tactical order placement still applies clearance.
- Implementation → [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]]. Tests authored; runtime battle acceptance remains pending.

### Patrol and recovery relocation (2026-11-14)

- Each task group smooths local/fleet advantage with a 4 s exponential average. Empty-force → real-contact transitions reset that channel, preventing the no-enemy sentinel `100` from delaying retreat.
- Retreat enters at difficulty `RetreatAdvantage`, exits at ×1.25; Engage/Retreat/Capture hold 8 s unless ships/base/defense/cornering or target validity requires a change. Capture/patrol/anchor targets hold 10 s and require ≥20% better utility; invalid targets release immediately.
- `TeamIntelRegistry` owns team `EnemyPatrolMemory`: 6×6 sector centers, relays, mining/satellite sites and unexpired intel. Sample allied visibility on AI ticks at 3 s cadence; score staleness × value / (1 + distance). Values: allied mine 5, neutral mine/relay/pad 3, hostile structure 2, empty sector/other intel 1.
- Squadron scout priority: allied marker → missing build space → common patrol/intel score → known hostile base. Normal+ detaches the cheapest available corvette when spare ships exist; defense, recovery and combat task-force need release it. Easy uses squadrons only.
- `EnemyRecoveryAnchor` updates in `AssessStation`, before task-group decisions. Trigger: own station lost, critical pressure, or weak fleet under attack for 5 s. Clear after 20 s parity without pressure or successful replacement construction; suppress the completed pressure episode until it clears.
- Anchor candidates: safe home/own/allied zones/stations → sector seen within 60 s → neutral relay to capture first. Human and AI allies use the same alliance rules. No survivors → safe owned/allied/seen-sector building remains possible; neutral capture requires ships.
- Anchor utility interpolates near-own-fleet and far-from-known-threat by clamped smoothed fleet advantage. Shared retreat checks reject unsafe routes; pending relay capture takes priority for an eligible group. No anchor → defer rebuilding and scout safe unseen space.
- Free placement: anchor rings → safety-ranked allied zones → map rings from anchor; every candidate passes live spawn and hostile-reach checks. Known weapon reach +100 units is excluded (unknown reach 500); lost-home exclusion max(64 units, own default-home radius) plus placement footprint during recovery.
- Destroyed-structure exclusion = max(32 units, placement radius), last 10 positions. Sites keep fixed mine/satellite placement. Completion revalidates anchor ownership, contesting, recent sector visibility and safety; no candidate → existing refund path.
- Hosted AI may buy its replacement station during recovery. Recovery overrides the critical-station purchase gate; ordinary hosted upgrades remain restricted. Backup construction does not itself order withdrawal.
- Modes using `DirectCombatAiState` do not run strategic recovery/patrol ship decisions; ordinary strategic AI, including applicable Survival AI slots, uses the same policy. Transient patrol/commitment/latch state is reassessed after load; no save schema change.
- Implementation/acceptance → [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]]; event fields → [[Tooling/Battle_Telemetry]]. Tests authored; battle acceptance remains user-gated.

### Patrol/recovery review corrections (2026-11-14)

- A viable threatened owned site immediately preempts a normal capture target, bypassing target age and score margin; unsafe, reserved and stalled objectives remain excluded.
- A living station stays defended during backup construction unless smoothed local advantage falls below difficulty RetreatAdvantage (0.7 Medium/Hard; 0.8 Easy/UltraHard). Withdrawal retains the existing ×1.25 exit threshold; station loss still directs survivors to the recovery anchor.
- Reinforcement rally follows the actual DefendBase decision during recovery; pressure alone does not redirect withdrawn groups home.
- Pressure bypasses minimum state duration only when entering DefendBase, not for arbitrary capture/engage/retreat switches while pressure remains latched.
- Placement/scout map search still checks threats per candidate. Worst-case source complexity is candidate count × (sightings + entities + patrol points); profile a blocked large map before optimizing. No measured timing or performance pass claimed.
- Fix and verification → [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]].
