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

### Wing capture round 2 (2026-11-19, B1–B6) — current

- Supersedes in the next section: one group B, in-zone pad choice, bait at the edge, 400 move order, uncapped wings while ships are locked.
- Capture radii: mine 60 (10 s / net ship), satellite pad 500 (6 s). A wing on a mine spot takes the mine and every pad within 500.
- Groups land on mining spots (`AssignInZone`): base-side first; ≥ 6 wings split over two spots; contested only when advantage ≥ 1; no spot → nearest-home site; never idle. Uncontested spot → `MoveTo` within 2 × spot radius.
- One outer group per outer zone: 1 wing each, the nearer zone up to 2 (cheap wings); bombers → nearest outer group. Zone kept until taken or below half of peak.
- Opening wing cap before the first ship: 6 cheap / 4 otherwise (`EnemyWingPricing`); bombers ≤ 3 per known hostile mine; pending satellites/mines block extra wings.
- Held zone (relay allied or own satellite) with hostiles → tier −1; with own satellites → free ships sent below an even fight. Own wings in the zone count as support.
- New ships arrive at the held relay/station nearest `EnemyZoneAllocator.DeployZone` (gather zone → best served zone → weak target).
- Armed structures are rated by platform type (`IDefendPlatformTypeFacade` → `UnitCombatProfileCatalog.GetPlatform` from the weapon loadout); supersedes the flat 60 DPS profile below.
- Bombers: mine → hostile satellite in their zone (together, power rule) → relay. Ships in a hostile zone: known hostile satellites (blocking-facility candidates, +2000) → relay (+1000) → pads.
- Raids (2026-11-20): a group's raiders go together, rated as one force vs the mine and its cover; one group per mine. Outer groups never share a zone; a held home zone under attack is defended first.
- Stalled siege: a zone force below an even fight for 90 s is released and its zone marked like a retreat (held zones under attack exempt).

### Rounds 3–4 (2026-11-20) — current
- No drip-feed: a free ship joins a zone force only if the force with it is a `Go`; else it gathers in the reserve. Held-zone defence below even only at advantage ≥ 0.6 (`DEFEND_FLOOR`); weak-phase attack only at pool advantage ≥ 0.6.
- Stalled siege: released when advantage stays < 1.25 for 90 s (held zones under attack exempt).
- Armed structures seen once stay defenders while they stand (`HostileIntelModel.RememberStructure`).
- Station lost → 180 s turtle: every force recalled, ships gather at home.
- Wings: zone-less groups without a raid work group A's zone together; no one-per-target spread.
- Production under station pressure buys only what is affordable now. Patrol without a safe point goes to the stalest reachable point (bait).
- Capture moves past a hostile-held centre use a waypoint `2R + 500` beside it.
- Own side with no allied station: measured from the team's fixed relays.
- Spawn and platform placement obey hostile `SpawnBlock` circles: a held but contested relay may give no spawn point (expected).
- A held zone with no candidate site left → its zone force guards the zone's nearest own satellite (round 5, E3c).

### Round 5 (2026-11-20)

- Ship spawn: rear search only for a defended station (`defendHome`); otherwise forward first: `EnemyForwardSpawnAnchors` (spawn beacon → wings → ships → own structures, none within 800 of a known hostile ship, ≤ 3) → `ForwardSpawnRing` (8 points, ring 250, `IsOpen` + hull box, no reachability) → else relay/station in the same tick. Smallest hull lands first.
- Patrol from level 2 = fighter/interceptor wing; meeting hostile ships it holds a spawn beacon toward home (outside hostile block circles, ≥ 1,000 from hostile relays, ≥ 1,200 from ships). Corvette patrol only at level 1.
- Income: `StationIncome` 30/level × research, × 1.5 with a living second station; miner 30. Skirmish money 5,000–30,000 (default 12,000).
- Group A spreads ≥ 2 wings per winnable mining spot, never parks on a contested site it cannot win. Idle bombers hold at the nearest own held zone.
- Battle asteroid instead of a mine when the zone is held and attacked, was lost and retaken, or has hostiles before 120 s.
- Route defenders: hostile ships within 800 of the pool → zone line count for that zone. Zone force with no site change for 120 s = stalled.
- AI task-force orders pass `AiOrderDeduplicator`: no resend of the same running order (≤ 50 / same target); ended moves resent at once, ended same-target attacks after 10 s.
- One auto-ability decision (`AutoPowerDecider`) for player and AI; urgent Shields below 40 % with a hostile warship in radar range.

### Round 6 (2026-11-20) — current; supersedes Round 5 where they differ
- Hull heights differ per ship type, so the 3D hull/landing test lets two types land on one point. Forward spawns add a top-down test: living ships and forward landings of the last 2 s (`ShipSpawnPoints.FORWARD_LANDING_SECONDS`).
- Forward anchors (`EnemyForwardSpawnAnchors.Select`): ships landed this tick → distance to the task → kind; beacon leads only within 1,000 of the best other anchor. Reject within 800 of known hostile ships, seen/remembered armed structures, hostile stations. Deep anchor → 3 own ships within 600; assault capital → 4, else relay/station.
- Platforms: count only purchasable types (`EnemyFactionModel.DefendPlatforms`); satellites and battle asteroids are not platforms. Station ring 1 from level 2, 2 from level 4 → centre 2 → other held zones 1; up to 3 only while ships wait on the unit limit. Contested held zones still get one when the point is safe.
- Production saves for an unaffordable unit only with ≥ 10 ships, advantage ≥ 1 and no recovery; else buys the best affordable unit. From level 2, money ≥ 2 × the ship just bought → one more affordable ship that cycle.
- One battle asteroid per zone. Held zone "attacked" = known hostile ship or ≥ 3 hostile wings (team intel), not any unit at a site.
- Second station: from level 2 at 2 × price, no mandatory satellite/mine pending; ring 400–700 around home on the half away from the map centre, never nearer a hostile station than home; fallback own outer zone nearest home; an unplaceable paid station retries every 10 s (no refund).
- Telemetry: `raidSkip`, `surplusShip`, `ai_orders.issuer` (`executor|dedup|commander|patrol`).

### Opening and wing groups (2026-11-19, A1–A7)

- Supersedes in the allocator section below: retain-line release (`0.85 ×`), idle reserve / waiting fleet, `hostileZoneOnRoute`, pause on any station pressure, wing raid counts (surplus beyond 3 / behind → all but 1).
- Allocator: sent force keeps its zone until its own local fight is lost; no idle ship (leftovers join the nearest force; none out → all attack `WeakTarget`); centre force led by the most durable ship. New forces pause only while the station is critical.
- Weak loop: a zone group in `RetreatValue` marks its zone 90 s; `WeakTarget` = best outer zone by pool advantage, never the centre or the marked zone; reinforcements gather at its staging point.
- Centre never excluded: behind + Assemble → tier 4, or 3 with an outer zone + own mine; behind + Go → 2. Own satellites/platforms (`EnemyZoneObjective.OwnSupport`) fight beside the force in `Assess`.
- Zone defenders: units within radius + 500. Allocator-sent group commits to its zone regardless of verdict.
- Wings: station-hangar wings are free; carrier wings stay on Guard and are the only escorts. Group A → centre, group B (1–2 wings + bombers) → nearest outer zone with a target, then raids (power rule at the mine). In-zone: free site, pads first, base-side first; contested → next free site; none → bait at the edge. Last 400 → move order (hold inside the pad circle).
- Production: level-1 corvette only at level 1, from 30 s, max 2. Ships wait while a mandatory satellite or mine is pending. Wings: fighter/interceptor types alternate; bombers once a hostile mine is known. Ships deploy singly until an outer zone is held.
- Station defence target: ship attacking the station → nearest threatening ship → wing; kept while it threatens.
- Production, round 7 (2026-11-20): ships are bought only from the station level and one below (`SHIP_TIER_SPAN` 1; floor drops until a type exists; critical station exempt). Below 10 reserved ships only the cheapest platform is chosen.
  - Why: buy-now + steady income never reaches a higher-tier price; same-type builds run one at a time, so cheap orders clog the queue and the unit limit.
- Owner and history → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups]].

### Ship allocator (2026-11-19, N1–N6)

- Supersedes below: group formation by proximity, `MAIN_FORCE_SHIP_COUNT`, opening-focus and expansion detachments, the 45 s assemble release, commit at `RetreatAdvantage`, `STATIC` detection through `IUnitCombatObserverFacade`, wing `requiredWings` counts.
- `EnemyShipTaskGroupPlanner`: one reserve (main force, always) + one group per zone (`AssignedZoneId`). `Reconcile` → new ships join the reserve. `Allocate` (cycle end) → `EnemyZoneAllocator`.
- `EnemyZoneAllocator` (pure): keep a sent force at ≥ `0.85 ×` commit → top up to `Go` when possible → rank unserved zones (tier, score) → add nearest free ships until `Go`; not enough → no ship goes, first such zone = `GatherZone`.
- Commit = `EnemyZoneEvaluator.COMMIT_ADVANTAGE` 1.0 (even fight). Tiers: threatened own zone −1; behind on zones: own-side Go 0 / Assemble 1, other Go 2 / Assemble 3, centre 4; else Go 0 / Assemble 1.
- Reserve never captures. Guard point: held centre → staging of `GatherZone` (nearest safe team-held zone, else 500–1500 ahead of home toward it) → idle point ahead of home. New ships rally to `planner.GatherPoint`.
- Reserve excluded from the pool while `DefendBase` / `AssaultBase`. Station pressure → no new force. Critical station → all forces recalled.
- Zone rating additions: enemy-side unseen zone with no known defenders after 180 s → remembered hostile force ÷ hostile-held zones; enemy-side non-centre zone with a hostile-held zone within `1000 + radius` of the straight route from home → `Deny: hostileZoneOnRoute`.
- Armed structures = `IStructureKindFacade.Kind == DefendPlatform` (satellites, platforms), visible only, reach 500, flat 60 DPS profile. They bind no unit or combat-observer facade → never in intel.
- Pull-back cut-off: only a force the group cannot beat, ≥ 500 ahead on the way back and within 500 of it.
- Raids: `RaidWhenBehind` (reserve, behind on zones, no capture target) lifts the centre-guard block; the 2:1 ship-count block does not apply to raids; a covering satellite counts as a defender.
- Wings: raiders are picked before escorts (behind → all purchased wings but 1, max 3; else beyond 3). Capture targets: defenders = known hostile units + armed structures within 1000; wings go only when together `Advantage ≥ 1`, else none.
- Telemetry: `zoneAlloc` (on change), `zoneEval assignedZone`, `wingAssignment … defendersN`.
- Owner and evidence → [[TODOs/Bugs/Enemy_AI_Economy_Strategy]].

### Zone objectives (2026-11-19, rebuild P1–P7)

- Supersedes the per-site yes/no safety gate. Older bullets below that mention route safety for capture/raids, the `2:1` ship-count rule or "hostile capture strength + one ship" sizing are historical.
- Objective = zone. `EnemyZoneObjectiveCollector` groups capture candidates per reinforcement zone (`IsInZone`, 2 × radius); a site outside every zone is its own area.
- Defenders = known hostile units within `1000` + zone radius of the centre, plus visible armed hostile structures within 2 × radius + weapon range. Structures have no catalog profile → flat `STATIC_DEFENSE_DPS = 60` per class at live hull/shields (estimate, not calibrated).
- One strength measure: `EnemyZoneEvaluator.Assess` → `CombatMatchup.Advantage(force, defenders)` vs difficulty `RetreatAdvantage`. `Go` ≥ threshold; `Assemble` below (records `RequiredScale` = threshold / advantage); `Deny` only near a hostile base (900) without a threatened own site.
- Score = best site score in the zone (existing `GetCaptureScore`, travel-distance units) − (`RequiredScale` − 1) × `MISSING_FORCE_COST` (1000).
- Tier before score: not behind → Go, then Assemble. Behind on zones (an enemy team holds more uncontested zones) → own-side Go, own-side Assemble, other Go, other Assemble, centre last. Behind also lifts the main force's centre-only filter.
- Site choice: any Go zone of the chosen tier in rank order (falls through to the next zone when sites are unreachable), threatened own sites, or the kept objective (keeps above `0.85 ×` threshold, 3 s grace below). Hard reachability only: clear point + ion cost (`IsDestinationReachable`).
- No Go zone → group gathers: `GuardPosition` = staging point = nearest uncontested team-held zone to the target with no known hostile near, else home. Main force guards the centre only when the team holds it.
- Hostile satellites/platforms no longer hide a zone or block routes for capture; they are defenders. Retreat routes and marker approaches still use `IsRouteSafe` + static defense.
- Expansion sizing (`GetRequiredExpansionShips`): group alone Go → group size; else (group + ship) × `RequiredScale`. Main force no longer skips zones reserved for an expansion detachment. Zone/centre detachments gathering ≥ 45 s are released.
- Pull-back: outmatched locally (`LocalAdvantage ≤ RetreatAdvantage`) with `CanDisengage` → `RetreatValue` to the nearest safe held zone or home. Fight in place within 1000 of any allied station, inside a held zone, under station pressure, at the point, or when the way back crosses a known force (cut off). 8 s hold against flipping back.
- Reinforcements: a non-main group farther than 2 × `FleetCohesion.RADIUS` stays separate while either group is in a local fight.
- Raids: no route gate; a target covered by a visible armed hostile structure is skipped; defenders vs `RequiredAttackRatio` unchanged.
- Wings (opening): one wing per opening zone before any gets a second; contested zones are topped up afterwards.
- Telemetry `zoneEval`: one row per zone when its verdict for a group changes (`verdict`, `required`, `advantage`, `defenders`, `statics`, `eta`, `score`, `priority`, `staging`, `behind`). `captureBlocked` now only when no Go and no Assemble.
- Not built: unseen-fleet share for never-scouted zones (treated as empty); exact structure weapon profiles.

- Capture identity: `EnemyCaptureObjective` carries ID, type, owner, position, radius, progress and emergency-defense flag.
- Outer mining zones get persistent detachments. Rank open objectives by ship ETA versus known hostile arrival; size for hostile capture strength + one ship-equivalent. Ships retain their zone while waiting for reinforcements. The centre gets an early dedicated assignment.
- Pads can be captured before their point mines. The authored `IsMain` relay identifies the centre, including offset layouts. A valid centre objective stays committed. Blocked approaches, stalled capture and a materially stronger hostile force allow a switch.
- The main force captures the centre and consolidates once it is held. It protects the centre, supports retakes and sends a small raid against exposed mining when expansion is unavailable. Local combat, home defense and decisive assaults can preempt centre duty.
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
- Up to three surplus purchased squadrons select distinct visible or remembered mining facilities, including home sites, spawned miners and outer mines. They attack visible facilities and move to the last known position under fog.
- Raid defenders: known ships and squadrons within `1000`; evaluate this group's `CombatMatchup.Advantage` against difficulty `RequiredAttackRatio`, not presence of any ship.
- Raid route: new and retained raids also pass `IsRouteSafe` (known hostiles at origin, within `1000` of the route, or near target vs `RetreatAdvantage`). Same check as capture/retreat objectives, without footprint clearance.
- A constructing or operational hostile mine with team intel is a strategic objective. The fleet attacks the facility before entering the circle. Once it is destroyed, the site can be captured normally.
- At least one eligible squadron remains available to capture while objectives exist. Scouts use surplus; carrier guards and escorts do not count as available.
- Site construction checks ownership, hostile presence and affordability without a minimum warship count.
- Source comparison and counter limits → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].

- Fleet advantage combines remembered combat strength with estimated unobserved ship purchases. Estimate from last contact, mine claims and last observed effective mine income. Cap the separate hostile income pressure ratio at `2×`. Voluntary offensive orders and contested routes reject known forces outnumbering the group by `2:1` with at least three known warships.

### Independent task groups (2026-10-31)

- Reserve the fastest eligible ship with a safe, unreserved objective as a capture detachment. A lone ship may detach for an unblocked mine. Retreating ships and ships currently attacking amid local threats are excluded. Combat groups reserve objectives before extraction.
- Capture detachments keep separate objectives and orders. The 10 s release timer and station-pressure recall apply to generic detachments. A mining-zone detachment retains its assignment until the eligible objectives in that zone are complete or unavailable.
- Opening combat groups join within 800 and split beyond 1,600. At six available combat ships, form the main force. Station pressure can form it earlier. Main-force membership survives travel separation.
- Group origin, local force, offensive strength, route safety, capture stall budget and recovery state are independent. Distant or nonassigned ships do not lend strength to a detached capture route.
- Executed capture objectives reserve their IDs for the decision pass. Mining-zone assignments reserve their area across ship groups, including while the detachment is waiting. The main force evaluates first once formed.
- `CaptureStrengthSampler` caches player weights for each site and relay. The AI queries its team and the strongest hostile team. Squadrons cover threatened and opening sites in viable groups, retaining assignments. A tie requires another wing. Squadron assignments avoid threatened mine approaches. Ship detachments check route and defender strength and avoid mines within hostile station or static-defense coverage.
- Squadron reservations release on role removal, pooling or target ineligibility.
- Telemetry publishes the primary combat group's strategy/context with aggregate active/body/rallying counts and an action summary of all groups. It is not a separate telemetry row for each group.
- Implementation and acceptance owner → [[Done/Bugs/Enemy_AI_Independent_Task_Groups]].

### Target-specific strike forces (2026-11-01)

- `EnemyAttackTaskForce` selects persistent members for Hunt/Assault/Raid. Assault and hunt keep a ship uncommitted once the group has at least three ships. Forced local fights and critical station defense can commit all available ships.
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
- Prioritize station income research on unlock at levels 4 and 5. Opening miners and `IncreasedProduction` remain priorities. Station upgrades leave combat pipelines open. Choose affordable useful work when the preferred purchase is blocked. Level 3+ excludes level-1 corvettes.
- AI production permits one backup station after the opening, under critical pressure or with late surplus after upgrades and research. Place it at a safe patrol-known point away from the home station and relay, preferring owned zones. Keep pending backup construction on restore. Refund obsolete backup upgrades.
- The first fleet deploys as builds finish. Following reserves deploy in waves of three. Siege waves use two and retry together. Normal and siege landings prefer the rear of the production station. Assault waves can release a smaller batch after 12 s.
- Assault requires a level 4+ station, at least three level 4+ capitals, a held reserve and continued capital production. Require a strict majority of uncontested capturable zones unless surplus credits and clear fleet strength qualify. Recheck each strategic decision.
- Build one free-placement defend platform per safe held zone when there is space, centre and captured zones first. Favor type diversity and stagger platform purchases by 20 s.
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
- Critical = latched pressure AND (durability <50% maximum OR estimated loss time <120 s). Replace affordable miners and prefer a timely ship to a squadron. Starting-tier corvettes are excluded at level 3+. Critical squadron buys use a three-wing cap including hangar wings. Site mines/satellites and new superweapon charging purchases pause.
- Critical production rejects candidates arriving after estimated station loss. Arrival = same-ID queue delay + build time + travel at base speed to the last known threat. No bearing → queue + build only. Forced rear deployment can bypass hostile reinforcement influence while retaining map, visibility, hazard, neutral blocker, hull and reachability checks.
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
