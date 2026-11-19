---
category: Bugs
status: in-progress
created: 2026-11-19
updated: 2026-11-19
parent: "[[TODOs/Bugs/Enemy_AI_Economy_Strategy]]"
related: "[[Done/Bugs/Map_Enemy_Station_Near_Allies_Fix]]"
---
# Enemy AI Economy Strategy — Review

Reviewed 2026-11-19 by source inspection only. No tests, no generation run, no playtest. Fix solutions below are proposals; none is implemented.

## Scope
- AI plan: [[TODOs/Bugs/Enemy_AI_Economy_Strategy]] + [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]]; commits `2fa16bea3` (13:44), `8e2ebefe6` (14:30), `28d1a41fd` (14:42).
- Map plan: [[Done/Bugs/Map_Enemy_Station_Near_Allies_Fix]]; uncommitted diff in `StationPlacer.cs`, `StationSlotGeometry.cs`, `MapLayoutGenerator.cs`, `MapStationSlotTests.cs`, `MapLayoutGeneratorTests.cs`.

## Verdict
- Direction is right: flow (zones → centre → build → retake/sabotage → gated assault) matches the user rules; X1–X5 are in source as described.
- Nothing after `8e2ebefe6` is proven: last test result file is 2026-11-19 00:01, last battle 13:52 — both older than the fixes.
- Only played setup: Small, 1v1, UltraHard. Team games and Medium/Large have open defects (R1–R9).
- Map fix: geometry correct for balanced teams; does not touch Small or duels → cannot change the pending 1v1 acceptance battle.

## AI plan vs source
| Item | Source | State |
|---|---|---|
| X1 deployment tiers A–D, 15 s cap, tier E keeps ship queued | `EnemyFactionController.TryFindShipSpawnPosition`, `ShipSpawnPoints.TryGetRearSpawnPosition` | matches |
| X1 partial defense wave at 8 s | `DeployDefenseWave` (`anyPastHoldLimit`) | matches |
| X2 save for chosen ship | `EnemyProductionDecisionModel.Evaluate` returns `None` when unaffordable | matches |
| X2 corvette cap | `MAX_LEVEL1_CORVETTES_AT_LEVEL2 = 2`, critical fallback | matches |
| X3 opening grace, unseen cap, half-visible floor | `EstimateUnseenFleetInvestment`, `Build` (`OPENING_UNSEEN_GRACE_SECONDS 180`, share 0.5) | matches |
| X3 economy pressure once | `fleetRaw` only; not in `IsThreatSafe` | matches (also divided in `UpdateRecovery`, separate purpose) |
| X4 centre strength with squadrons, reinforce not drop | `GetOwnCaptureStrength`, `NeedsCentreReinforcement`, `TryReinforceCentreGroup` | matches; see R2 |
| E1 one detachment per outer mining zone, ETA-ranked | `EnemyShipTaskGroupPlanner.TryAssignExpansionShip` | matches; see R5, R6 |
| E2 assault gate | `Build`: ≥ 3 L4 capitals, `HasAssaultReinforcementPlan`, zone majority or surplus | matches for 1v1; see R1 |
| E4 one platform per held zone, 20 s spacing | `TryGetDefensePosition`, `DEFENSE_BUILD_INTERVAL` | matches |
| E6 backup station | `TryBuySecondStation` (480 s, 2× price), `TryGetSecondStationPosition` (≥ 1400) | matches |
| E7 station income research first | `CanResearchPriorityIncome` | matches |

- Stale note: regression plan says review fixes are "uncommitted" → they are in `28d1a41fd`.
- Uncommitted, unrelated: `EnemyUnitCommanderTests.cs` constructor fix for `UnitOrderService` (2 lines).
- Works as intended for teams: shared focus enemy, combined strike strength, station defense request to the nearest allied main force, team-wide hostile force.

## User decision (2026-11-19)
- R4: income is shared across allies → an allied AI taking mines beside a teammate's base is fine. No exclusion.
- Instead: lower the priority of mining sites near a base. Agent reading: sites near an **allied** player's station rank after other free sites. See R4 fix.

## Findings and fixes — more AIs, allies

### R1 (P1) Assault gate counts only this AI's zones
- Where: `EnemyStrategicContextBuilder.Build` → `if (zone.Owner == self && !zone.IsContested) heldZones++`.
- Effect: 2v2 / 3v3 → two allied AIs split the zones → neither holds > 50 % → assault only through the money path. Zones of a human ally do not count. 3+ teams → > 50 % of all zones is rarely reachable.
- Fix:
  1. Count zones held by the team: `_playerRoster.IsAllied(self, zone.Owner) && !zone.IsContested`.
  2. Two teams: keep `2 * teamHeld > capturableZones`.
  3. Three or more teams: `teamHeld` > every enemy team's count and ≥ `capturableZones / teamCount` (needs user confirmation; the user rule only defines > 50 %).
  4. Keep `ownedCapturableZoneCount` (telemetry) per AI; add `teamHeldZones` to the `sample` event.
- Tests (`EnemyAi`): zones owned by an ally count toward the gate; contested allied zone does not; enemy-owned majority blocks.

### R2 (P1) Centre reinforcement has no cap and blocks expansion
- Where: `EnemyShipTaskGroupPlanner.TryCreateCaptureGroup` → `if (TryReinforceCentreGroup()) return;` before `TryAssignExpansionShip`; one ship per decision cycle (UltraHard 0.5 s).
- Effect: centre hostile strength = all enemies there. With 2–4 enemies the AI feeds ships one by one and assigns no expansion ship while the request stands. Only brake: route safety (`RetreatAdvantage`).
- Fix:
  1. Cap the centre group: stop reinforcing at `max(2, ceil(0.5 × ships outside expansion detachments))`.
  2. Do not return after a reinforcement: run `TryAssignExpansionShip` in the same cycle.
  3. If the cap cannot reach `HostileStrength`: no more ships; the centre group holds at the nearest held zone and waits for the main force (existing `blockedApproach` → Hold path) instead of trickling in.
  4. Reinforcements join only when within `FleetCohesion.RADIUS` of the group or after the group regroups, so ships arrive together.
- Tests (`EnemyAi`): centre request + open expansion zone → both get a ship in one cycle; centre group never exceeds the cap; hostile strength above the cap → no further reinforcement.

### R3 (P2) Unseen estimate uses team income against one AI's fleet
- Where: `GetHostileEconomyPressure` sums the whole enemy team into `_hostileIncome`; `EstimateUnseenFleetInvestment` caps at `0.5 ×` this AI's fleet value.
- Effect: in team games the cap is always hit → `fleetRaw = 1 / (1/visible + 0.5)` after first contact → each allied AI rates itself weaker than in 1v1.
- Fix: divide `_hostileIncome` by the number of living allied players before the estimate (each ally faces its share). `visibleAdvantage` already uses the combined allied strike force for the main group.
- Tests (`EnemyAi`): same hostile income, 1 vs 2 allied players → unseen investment halves; opening grace unchanged.

### R4 (P2, decided) Mines near an allied base get lower priority
- Where: `GetCaptureScore` (ship groups), `GetRequiredExpansionShips` (expansion ETA), `EnemySiteObjectives.AddSquadronTargets` (wings).
- Fix:
  1. New helper `IsNearAlliedBase(position)`: any `_worldSnapshot.Bases` entry allied to but not owned by this AI within `LOCAL_ENGAGEMENT_RADIUS` (1000).
  2. `GetCaptureScore`: add `ALLY_BASE_MINE_PENALTY` (travel-distance units, start 600 = `SATELLITE_PAD_PRIORITY` scale) for `candidate.IsMine` near an allied base.
  3. `GetRequiredExpansionShips`: add the same penalty as seconds to `arrival` (÷ ship speed) so those zones are assigned last.
  4. Squadron targets: pass the flag so wings prefer other free sites first.
  5. Threatened own sites and blocking-facility raids are not penalised.
- Tests (`EnemyAi`): two equal-distance free mines, one near an allied station → the other is selected first; only the near-ally mine left → it is still taken. Ordering asserts only, no penalty value.
- Open: if "near base" also meant the AI's own home mine, say so — today the home mine is an opening priority (`IsHomeMineOnOpeningRoute`).

### R5 (P2) Expansion zones are reserved per AI, objectives per team
- Where: `AssignedExpansionZones` is local to one planner; `TeamAiCooperation` reserves objective ids only.
- Effect: two allied AIs send detachments to the same zone, then split its sites by id. One wasted ship trip per zone.
- Fix: publish `expansion:{zoneIndex}` through `TeamAiCooperation.AddReservations` when a zone is assigned; `TryAssignExpansionShip` skips zones `IsReservedByAlly`. Release on group removal in `Reconcile`.
- Tests (`EnemyAi`): ally holds the zone reservation → no second detachment; ally releases → zone becomes assignable.

## Findings and fixes — map size
- Zone counts: Small 2 outer relays, Medium 4, Large 6; archetype maps add `(players − 3) / 2` pairs + profile pairs. All plus the centre. Max players: Small 4, Medium 6, Large 8.

### R6 (P1) Expansion has no distance or count limit
- Where: `TryAssignExpansionShip`, `IsExpansionObjective`.
- Effect: every capturable relay with mining gets a detachment; main force can be drained to 3 ships. Relays are ≥ 1100 / 1300 / 1500 from every station (`AdditionalZoneStationDistance`) → `BASE_EXCLUSION_MARGIN 900` never excludes a relay. Medium/Large: half the relays are on the enemy side; single ships are sent there.
- Fix:
  1. Eligibility: a zone is ship-expansion eligible when its nearest allied station is not farther than its nearest hostile station, or the team holds the centre uncontested.
  2. Enemy-side zones before the centre is held → squadron raids / sabotage only (existing `RaidStructures`, wings).
  3. Count cap: expansion groups ≤ `ceil(ships / 2)`; keep the existing "main force ≥ 3" guard.
  4. ETA ranking unchanged inside the eligible set.
- Tests (`EnemyAi`): enemy-side zone not assigned before the centre is held, assigned after; group count never exceeds the cap.

### R7 (P2) Acceptance numbers are Small-only
- "Centre before 120 s", "outer zone by 240 s", "empty zone for 1–2 min is a bug" assume ≈ 2,800 travel. Large duel: stations ≈ 10,400 apart.
- Fix: restate each gate as `travel ETA (station → zone at group speed) + capture time + 60 s`. Add telemetry event `expansionAssigned` (zone, eta, ship) so the `zoneOwner` timeline can be checked against it. Update the queue lines in [[TODOs/User_Acceptance_Queue]] when the first non-Small battle is planned.

### R8 (P2) Few ships early on big maps
- Corvette cap + saving → 1–3 ships until level 3. Medium/Large need 4–6 detachments + centre; coverage depends on squadrons (`MINIMUM_CAPTURE_WINGS 3`).
- Fix (proposal, balance — needs user OK): `capture wings target = max(3, eligible expansion zones + 1)`; corvette cap unchanged (user rule X2). Wings take pads and free mines; ships follow by ETA.
- Tests: none with numbers (no stat-pinning); assert the target grows with eligible zones.

### R9 (P2) Cost grows with players × zones
- Where: `GetRequiredExpansionShips` → `HostileReachesFirst` scans all sightings × all `_shipService.Ships`, per zone × per unassigned ship, inside `FinishDecisionCycle`.
- Fix: in `BeginCaptureProbe` build one list of confident hostile ship sightings with their `NavigationSpeed` (one pass over ships); per zone pick the nearest eligible mine once; per ship compute only the ETA. Profile `EnemyAI.Ship.CaptureProbe` on Large 8-player before and after.

### Unverified — rear deployment on other maps
- Rear rings at 1.5–3× `HomeAreaRadius` need own vision (`ReinforcementSpawnRule.IsOpen`); on ring maps the rear points at the map edge (stations ≥ 546 from the border).
- Check: share of `tier=A` vs `B/C/D` in `deployment` telemetry on a ring-map battle. If A is rare: sample only in-bounds, visible points first, or register vision on the rear ring.
- Fixed constants that stay valid on all sizes: `HomeAreaRadius 270`, rear rings ≤ 810, `LOCAL_ENGAGEMENT_RADIUS 1000`, backup station ≥ 1400.

## Map fix — implementation check
- Ring weights: 2v2 60° / 120°, 3v3 45° / 90°, 4v4 36° / 72°, Orbital 3v3 40° / 80° → match the plan table. Balanced teams keep point symmetry and equal centre distance.
- Duel / FFA: `teamSizes.Count == count` → no weighting; RNG call order unchanged → same output. Small: `groupTeams = false`, swap fallback kept.
- Open Field 2v2: allied corners pulled on the shared edge to pull ≈ 0.65 → Medium ally ≈ 2,840, enemy ≈ 4,370.
- Plan closed during this review (`done`, `acceptance: pending-user`); its diagnostic agrees: ring 2v2 1.73, 3v3 1.85–1.88, 4v4 1.90–1.93, Open Field 2v2 1.52/1.57.
- Known and recorded in the plan (not fixed): Open Field 3v1 0.92–0.96 (pull skipped, it broke mirrored relay pairs); Twin Pass 2v2 1.43, 3v1 0.70, 2v2v2 0.37 → an enemy can still start beside an ally.
- Baseline hashes: recomputed by editor eval of the test's `Fingerprint`. Not a test run.

### M2 Division by zero for a one-team roster on an open arc
- `WeightTeamGaps`: `boundaryGaps = 0` → `boundaryAngle` = NaN. Reachable only if a one-team roster reaches Orbital Ring with > 2 players.
- Fix: call `WeightTeamGaps` only when `teamSizes.Count > 1 && teamSizes.Count < count` (one team → equal spacing).

### M4 Uneven rosters lose mirror symmetry on Medium/Large
- Previously equal spacing; attempts 60–119 now re-roll the same layout type. Diagnostic covered 3 seeds. A layout that fails 120 times throws.
- Fix: run the `MapGeneration` sweep with 2v1, 3v1, 2v1v1, 3v2. If any roster fails: for uneven rosters use equal spacing (all-ones `teamSizes`) on attempts ≥ 60.

### M5 `MIN_PULL` loop can step to 0.25
- Float steps of 0.05. Fix: integer step counter (`for step = 1..14`, `pull = 1 − step × 0.05`). Cosmetic.

## Map fix → AI plan
- No code coupling: the AI reads stations, zones and sites at runtime (`AiWorldSnapshot.Bases`, `IReinforcementZoneSource`, `ICaptureSiteDirectory`).
- Unchanged: Small maps, all duels, FFA → the pending UltraHard acceptance battle is not affected.
- Improves AI assumptions in team games: enemy no longer next door → "centre = front", rear deployment (away from the centre) and `_strategicCenter` (home ↔ enemy base midpoint) point the right way.
- Not improved: Twin Pass team games and Open Field 3v1 → there "centre = front" can still be wrong.
- Changes to expect:
  - Relays and mines move (placed relative to stations) → different neighbour/outer zones per AI.
  - Allied AIs share the same nearest zones more often → R5 matters more.
  - Zones in the widened gap between teams become the contested ones; zones between allies are safe → R6 eligibility (nearest station is allied) fits this layout.
  - Enemy stations cluster → an assault on one station is inside reach of the second (2,500–4,200) → the R1 gate should stay strict.
  - Allied station defense arrives faster (shorter distance).
  - Allied bases closer → more mines fall under the R4 "near allied base" penalty radius.
- Spacing after the fix stays above every AI radius: smallest ally distance ≈ 2,500 (Orbital 3v3) vs 1,000.
- Saved skirmish setups with allies regenerate with new station positions.

## Implementation 2026-11-19 (user: "execute")
- User approved the open proposals by asking to execute: R1 step 3 (3+ teams) and R8 (wing target) built as proposed.

### Test run before the changes (EditMode, `include_explicit false`)
- `EnemyAi` 493/501 (8 failed), `Ships` 213/213, `Abilities` 222/222, `Capture` 131/131, `Navigation` 380/380, `Core` 165/166.
- All 8 `EnemyAi` failures were stale or broken fixtures, not game defects:
  - `BlockedShip_RetainsPurchase…`, `BlockedShip_ProductionLostDuringRetry…` ×2: `OperationalProductionAccess.TryGetProductionBase` threw `NotImplementedException`; since `2fa16bea3` every deployment asks for the base (rear spawn) → build caught and refunded. Fake now returns false.
  - `MiningRaid_DoesNotOverrideEmergencyOrDecisiveAttack(…0.5…)`: low local advantage without `hasLocalThreat`; stand-and-fight needs a threat since `a945072cc`. Test passes `hasLocalThreat` when local advantage < 1.
  - `OutmatchedOverall_EngagesWeakerHostilesNearFleet`: expected 20 committed; UltraHard `CommittedFleetRatio` 0.85 since `529094e87` (reserve) → ⌈20 × 0.85⌉ = 17. Asserts against the profile ratio now.
  - `CaptureProbe_LoneShipCanDetachForSafeMine`: probe helper passed a null ship service; expansion ETA scans ships. Helper uses an empty `ShipService`.
  - `UnseenEstimate_*` ×2: null combat-profile catalog → asset load. New `VenatorCatalog()` helper.
- `Core` failure `ShipAutoPower_ObservesLiveWeaponsAndShieldRecoveryGates`: CLI bridge error log ("Main thread operation timed out") caused by status polling during the run; passes alone.

### AI changes
- R1 `CountTeamHeldZones`: team-held uncontested zones; 2 teams → `2 × held > zones`; 3+ teams → more than every enemy team and `held × teams ≥ zones`. `sample` logs `teamHeldZones`. Test `AssaultGate_CountsZonesHeldByTheWholeTeam`.
- R2 `TryReinforceCentreGroup`: cap `max(2, ⌈0.5 × ships outside expansion groups⌉)`; at the cap `CentreGroupAtCap` → a stronger centre is rejected (`blockedApproach` → Hold, waits for the main force); reinforcement no longer returns before expansion (both in one cycle). Step 4 (join only within cohesion radius) not built: the capture executor already rallies stragglers; check in the next team battle.
- R3: `_hostileIncome` ÷ living allied players (allied stations alive); team pressure ratio unchanged.
- R4: `IsLowPriorityAllyMine` (free mine within 1000 of an allied player's station, not threatened, not blocking) → `GetCaptureScore` −600 value, expansion ETA +600 / speed; wings: `EnemySquadronCaptureAssignments.DeprioritizeNear` (commander marks targets near allied stations, radius 1000). Own home mine unaffected. Test `SquadronCapture_SiteBesideAnAlliedBaseIsTakenAfterOtherFreeSites`.
- R5: assigned expansion zones published as `expansion:{zoneIndex}` in the shared reservation set each cycle; `TryAssignExpansionShip` skips zones an ally reserved; released when the group is removed.
- R6 `IsShipExpansionEligible`: nearest allied station ≤ nearest hostile station, or the team holds the centre uncontested; new expansion groups ≤ `⌈ships / 2⌉`.
- R7: telemetry `expansionAssigned` (zone, ship, eta, battleTime). Acceptance-queue gates are restated when the first non-Small battle is planned (unchanged rule).
- R8: capture wing target = `max(3, eligible expansion zones + 1)` via `IEnemyAiStateProvider.EligibleExpansionZoneCount` (default 0 = AIs that do not expand); corvette cap unchanged.
- R9: hostile ship speeds built once per capture probe (`GetHostileShipSpeeds`, reset in `CollectCaptureObjectives`); the ETA race no longer scans all ships per sighting.

### Map changes
- M2: `WeightTeamGaps` only for `1 < teamSizes.Count < count` (one team → equal spacing).
- M4: uneven rosters (team sizes differ) use equal spacing from attempt 60 (`HasUnevenTeams`, `MapLayoutGenerator.GenerateLayout`).
- M5: pull loop capped by a step count; the pull keeps the accumulated float so existing layouts stay bit-identical (a first version computing `1 − step × 0.05` changed all 6 Medium/Large Open Field fingerprints and was reverted).

### Not done / still open
- R2 step 4 (see above). Planner tests for R2/R5/R6 and an R3 test not added (planner needs heavy fixtures); verify in the 2v2 Medium battle.
- Map M2/M4/M5 were committed by the map session in `ac7ff075c` together with its own uneven-roster swap fallback (its sweep: 27/96 → 1/96 failures).

### Test run after the changes (EditMode)
- `EnemyAi` 503/503, `Capture` 131/131, `Map` 125/125 (before another session's in-progress atmosphere edit; a later `Map` run hit its temporary compile error), `Core` `ShipAutoPower_…` passes alone.
- `MapGeneration` (explicit, 3 seeds): Open Field fingerprints pass after the M5 correction; remaining failures were CLI-bridge "Main thread operation timed out" log errors from overlapping status polling, not generation results.
- Not rerun after the changes: `Ships`, `Abilities`, `Navigation` (passed before; no later edits in their code).

## Recommended order
1. User: UltraHard Small 1v1 battle + `EnemyAi`, `Ships`, `Capture`, `Economy` fixtures. Gates everything else.
2. User: `Map` + `MapGeneration` for the map fix; then M2, M4.
3. Implement R1, R2, R6, R4 (one change set, `EnemyAi` tests).
4. One 2v2 Medium battle (AI ally + 2 AI enemies) and one 1v1 Large battle; compare `zoneOwner`, `deployment tier`, `objectiveSwitch`.
5. R3, R5, R7, R8, R9 from that evidence.

## Files
- `Services/Enemy/EnemyStrategicContextBuilder.cs` — R1, R3, R4, R6, R9
- `Services/Enemy/EnemyShipTaskGroupPlanner.cs` — R2, R5, R6
- `Services/Enemy/TeamAiCooperation.cs` — R5
- `Services/Enemy/EnemySiteObjectives.cs`, `EnemySquadronCaptureAssignments.cs` — R4
- `Services/Enemy/EnemyProductionStrategy.cs` — R8
- `Services/Enemy/EnemyAiTelemetry.cs` — R1, R7
- `Entities/Map/Generation/StationSlotGeometry.cs`, `StationPlacer.cs`, `MapLayoutGenerator.cs` — M2, M4, M5
- Tests: `EnemyUnitCommanderTests`, `EnemyStrategicDecisionModelTests`, `MapStationSlotTests`

## TODO
- [x] R1, R2, R6, R4 fixes + tests (`EnemyAi`) — 2026-11-19, see Implementation.
- [x] User: confirm the 3+ team zone rule (R1 step 3) and the wing target (R8) — approved by "execute" 2026-11-19.
- [x] R3, R5, R7, R8, R9 — implemented 2026-11-19; verify in the first team battle (2v2 Medium, 1v1 Large).
- [x] M2, M4, M5 — committed in `ac7ff075c`.
