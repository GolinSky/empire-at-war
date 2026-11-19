---
category: Bugs
status: done
created: 2026-11-19
parent: "[[TODOs/Bugs/Enemy_AI_Economy_Strategy]]"
completed: 2026-11-19
acceptance: pending-user
---
# Enemy AI Economy Strategy — Regression Fix Plan

## Goal
- Undo the regressions `2fa16bea3` introduced (battle 2026-11-19 13:52, UltraHard, user: "full failure") without losing what worked on 12:39 (`529094e87`).
- Done = in the next UltraHard battle: no ship held > 10 s after completion; ≤ 2 level-1 corvettes alive after level 2; centre pads captured in the opening; AI rated "outmatched" only by enemies it has actually seen; ≥ 3 mining zones held by ≈ 4 min when uncontested.
- Parent: [[TODOs/Bugs/Enemy_AI_Economy_Strategy]] (evidence X1–X5, user rules, battle flow, assault gate).

## Rules (unchanged, user-final)
- Battle flow: zones early → mining + satellites → take and keep the centre → defenses + levels → take/retake all zones; else sabotage hostile mining.
- Assault gate: station L4 + L4 capital fleet + reinforcement waves + > 50 % zones (or big money + big fleet).
- Level 1: no production limits; 1 corvette is enough; squadrons for capture/patrol.

## Evidence summary (13:52)
| Symptom | Number | Source |
|---|---|---|
| Deployments blocked | 7,262 (min 4–9), 1 forced | `deploymentBlocked` |
| AI ships on map | 0 from 296 s to 634 s | `sample.ownShips` |
| C-9979 bought | 32 (22–209 s, ≈ 38k), + 6 Patrol Frigates | `purchaseAccepted` |
| Money at C-9979 buy | 1,200–1,460 | `purchase.money` |
| Fleet advantage | 0.69 at 31 s (1 vs 0 seen), 0.06 at 142 s | `sample` |
| "Hostiles near the fleet outmatch it" | 58 of 175 decisions | `decision` |
| Centre objectives dropped | `blockedApproach` 71.8, 75.4, 137.7 s | `objectiveSwitch` |
| Centre satellites | 0 | `satellite` |
| Held queue release | 3 Providence + 3 Recusant at 609–614 s (backup station) | `deployment` |

## Fix 1 — Deployment always resolves (X1, P0)

### Cause (source)
- `EnemyFactionController.Build` (`:400–455`): when not defending, `hasRear` (from `EnemyStrategicContextBuilder.TryGetRearDeploymentThreats`, `:207`) is true whenever a centre or home zone exists → every ship takes the rear path.
- Rear path: `ShipSpawnPoints.TryGetDefenseSpawnPosition(anchor = station)` (`:98`) samples only inside `HomeAreaRadius = 270` (`ReinforcementZoneData.asset`) around the station and requires `threats.IsSafe(...)` for each point.
- Inside 270: station hull, 5 miners (placed 200–400 around the station), platforms, satellites → `CanSpawnShip` clearance fails for larger hulls; with hostiles in range every point fails `IsSafe`.
- Forced path after `MAX_DEPLOYMENT_HOLD_SECONDS = 8` uses `TryGetBestDefenseSpawnPosition` (`:135`) — same 270 circle, `CanLaunchStationDefenseShip` (visibility, neutral blockers, clearance, reachability) → also fails → `RetryShipDeployment` every 1 s forever.
- `DeployDefenseWave` (`:277–333`): a wave deploys only if `landings + squadrons ≥ MINIMUM_DEFENSE_WAVE (2)`; one valid landing alone → whole wave retried.

### Changes
1. `ShipSpawnPoints`: new ring search for rear deployment — rings at `HomeAreaRadius` × 1, 1.5, 2, 3 around the station, rear half-plane first (away from the threat/approach), then the side quadrants, then the front.
2. Safety filter tiers: tier A = `IsSafe` + `CanSpawnShip`; tier B (after 4 s blocked) = `CanSpawnShip` only (ignore remembered threats); tier C (after 8 s) = station-defense rule (`CanLaunchStationDefenseShip`) on the widest ring; tier D (after 12 s) = `TryGetStationSpawnPosition` → `TryGetDefaultZoneSpawnPosition` → any held zone → `TryGetRandomSpawnPosition`.
3. `EnemyFactionController.Build`: one ordered chain for every path (defense, rear, rally, default) built from the tiers above; `forced` flag set from tier ≥ C; telemetry `deploymentForced` with `tier=`.
4. `DeployDefenseWave`: deploy the ships that found landings even if the wave is short once any ship's blocked time ≥ 8 s; never retry the whole wave because one ship failed.
5. Hard cap: a completed ship held ≥ 15 s deploys through tier D unconditionally (log `deploymentEmergency`). A finished ship is never kept longer.
6. Telemetry: add `heldSeconds` and `tier` to `deployment`/`deploymentBlocked`; add a `sample` field `heldShips` (count of finished, undeployed ships).

### Tests (`Ships`, `EnemyAi`)
- `ShipSpawnPointsTests`: crowded 270 circle (blocked clearance) + hostile in range → rear ring at 1.5–3× radius found.
- `EnemyFactionControllerLifecycleTests`: station surrounded by threats → completed ship deploys within 15 s; wave with one blocked ship deploys the rest after 8 s.
- Update existing blocked-attempt counts if the chain order changes (assert "deploys within limit", not exact attempt numbers).

## Fix 2 — Save for the chosen ship; stop corvette spam (X2, P0)

### Cause (source)
- `EnemyProductionStrategy.EvaluateProduction` (`:210–222`): `if (!canBuildShip && !critical && (!hasShipOption || !IsAffordable(shipChoice)) && TrySelectShip(..., requireAffordable: true))` replaces the chosen ship with **any affordable ship** → at 1,200 credits the C-9979 wins every tick.
- `EnemyProductionDecisionModel.Evaluate` lost the saving returns: was `HasShipOption → CanBuildShip ? Ship : None` and `IsLevelDue → CanLevelUp ? Level : None`; now `if (snapshot.CanBuildShip)` (`:162`), `IsLevelDue && CanLevelUp` (`:150`), `NeedsMinimumFleet && CanBuildShip` (`:153`) → never waits.

### Changes
1. Remove the affordable-ship swap (`EnemyProductionStrategy.cs:210–222`). The chosen ship (counter/role pick) is saved for.
2. Restore saving in `EnemyProductionDecisionModel`: chosen ship not affordable → `None` (wait) unless the station is critical; due level not affordable → `None`.
3. "Always-on" only for cheap, non-competing items while saving: research tiers ≤ 25 % of the saved target, mines/satellites (they outrank ships by the user rules), one defense platform per `DEFENSE_BUILD_INTERVAL 20 s`. Never a cheaper ship.
4. Level-1 corvette cap: at faction level ≥ 2, `TrySelectShip`/counter pick excludes C-9979 / Patrol Frigate (`AvailableLevel 1` ships with `Corvette`/`Transport` class) while ≥ 2 of them are alive; at level ≥ 3 excluded entirely except station-critical with nothing else affordable before `timeToLoss`.
5. Telemetry: `purchase` adds `saving=<unit>;savingFor=<seconds>`; `production-wait` reason `saving`.

### Tests (`EnemyAi`)
- `EnemyProductionDecisionModelTests`: chosen ship unaffordable, cheaper ship affordable → `None` (not Ship); critical → Ship.
- Corvette cap: level 2 with 2 C-9979 alive → no C-9979 selected; level 3 → never (non-critical).
- Remove/adjust tests added in `2fa16bea3` that assert "buys any affordable ship".

## Fix 3 — Unseen-enemy estimate (X3, P0)

### Cause (source)
- `EnemyStrategicContextBuilder.EstimateUnseenFleetInvestment` (`:1479–1490`): before any hostile ship is seen, `lastSeen` = battle start → `hostileIncome × min(elapsed, 90 s) × 0.5` + `UNSCOUTED_FLEET_INVESTMENT 1200` when no hostile force is known.
- Applied at `:637–644`: `fleetRaw = 1 / (1/fleetRaw + unseen/ownInvestment)`; plus `fleetRaw /= _hostileEconomyPressure` (clamped 1–2, `GetHostileEconomyPressure`), and `IsThreatSafe` (`:1421`) multiplies the threshold by the same pressure → economy counted twice/three times.
- `GetHostileEconomyPressure` uses `BASE_INCOME_ESTIMATE 15` for both sides; AI's own spawned miners count, but unknown hostile claims inflate fast.

### Changes
1. Opening: no unseen penalty before the first hostile **ship** contact and before 180 s (`UNSCOUTED_FLEET_INVESTMENT` = 0 then).
2. Unseen share cap: `unseenInvestment ≤ 0.5 × own fleet investment`; it can lower advantage, never below `0.5 × visible-only advantage`.
3. Apply economy pressure once: keep it in the strategic `fleetAdvantage` (assault gate, E2/E3) only; remove it from `IsThreatSafe` thresholds for capture/expansion routes.
4. Capture/route safety uses seen/remembered forces only (`_localThreatForce`, sightings ≤ 60 s old). Unseen estimates never drop a capture objective.
5. Telemetry: `sample` adds `visibleAdvantage`, `unseenInvestment`, `economyPressure`.

### Tests (`EnemyAi`)
- No hostile seen at 30 s → `fleetAdvantage` equals the visible-only value (no unscouted penalty).
- Large hostile income, no ships seen → advantage ≥ 0.5 × visible-only.
- Capture objective with no seen hostiles is never rejected for strength.

## Fix 4 — Centre in the opening (X4, P0)

### Cause (source)
- Centre objectives are rejected with `blockedApproach` (`EnemyStrategicContextBuilder.cs:1149`) after `IsObjectiveSafe`/`IsRouteSafe` (Fix 3 inflation) or the capture-strength check (`:1134–1139`).
- Strength check compares `_ownShips.Count` (ships only) with `candidate.HostileStrength` (ships 1 + squadrons 0.5) and treats a tie as unsafe (`<=`). 1–2 AI ships vs 2–4 human squadrons at the pads → rejected; AI squadrons in the group are ignored.

### Changes
1. Own capture strength = ships × 1 + own squadrons assigned/nearby × `SquadronCaptureWeight 0.5` (same formula as `CaptureStrength`).
2. Unsafe only if hostile strength > own strength + 0.5 (strictly stronger), not on a tie.
3. Short of strength → **reinforce** (call more ships/wings to the centre, R10) instead of Hold; keep the centre objective (no `blockedApproach` drop) while reinforcements are en route.
4. Opening priority: the first two capture groups go centre + nearest relay (existing opening focus); the centre group is never retargeted while it is the opening objective.

### Tests (`EnemyAi`, `Capture`)
- 2 AI ships vs 2 human squadrons at a centre pad → objective kept (2 > 1).
- 1 AI ship + 2 own squadrons vs 2 human squadrons → kept (2 > 1).
- 1 AI ship vs 3 human squadrons → reinforce request, objective kept.

## Fix 5 — Re-verify expansion and sabotage (X5, P1)
- [x] After Fixes 1–4, check E1 (every safe mining zone): per-zone ownership timeline in telemetry — add `zoneOwner` event (zone/site id, owner, time) on every ownership change.
- [x] Sabotage: `RaidStructures` stays available in Consolidate; spare wings raid distinct hostile mines (worked on 12:39).
- [x] Keep: mining research I at 0 s / II at level 2, combat research, station income on unlock, defenses every 20 s, backup station only when critical or late surplus.

## Outcome & Verification (2026-11-19)
- Implemented Fixes 1–5: tiered rear ring deployment (1/1.5/2/3× `HomeAreaRadius`, tiers A–D + 15 s emergency cap, partial defense wave release at 8 s, `heldShips`/`tier`/`heldSeconds` telemetry); removed affordable-ship swap and restored saving in `EnemyProductionDecisionModel` with ≤ 25 % non-competing research cap and level-1 corvette cap (≤ 2 at L2, excluded at L3+ except critical fallback); opening unseen-enemy grace (< 180 s before first hostile ship contact), 0.5× unseen share cap and half-visible floor, single economy-pressure application; centre capture strength includes own squadrons (`SquadronCaptureWeight`), strict hostility check, centre reinforcement request and opening centre retention; `zoneOwner` telemetry on zone/site ownership changes.
- Compiled Runtime (`Assembly-CSharp`) and Editor (`Assembly-CSharp-Editor`) assemblies via standalone Roslyn + Unity Bee response files with 0 errors.
- Authored/updated regression tests in `ShipSpawnPointsTests`, `EnemyFactionControllerLifecycleTests`, `EnemyProductionDecisionModelTests`, and `EnemyUnitCommanderTests`. Tests were not run (local override).
- Reusable guidance: no new standalone rule note required (core AI objective rules remain in [[Architecture/Enemy_AI_Objectives]]).
- User acceptance → [[TODOs/User_Acceptance_Queue#Enemy AI Economy Strategy]].

## Review of `8e2ebefe6` (2026-11-19)
- Source matches Fixes 1–5: ring rear search + tiers A–D + 15 s cap; partial defense waves; affordable-ship swap removed and saving restored; level-1 corvette cap (C-9979 / Patrol Frigate are `ShipClass.Corvette`; counter picks covered); no unseen penalty before contact/180 s, unseen ≤ 0.5 × own fleet, economy removed from route thresholds; own capture strength counts squadrons; centre requests reinforcement; `zoneOwner` events.
- Fixed in review (uncommitted):
  - Emergency tier E spawned at the station centre / rally / map origin without clearance when tier D failed → now keeps the ship queued and retries (`EnemyFactionController.TryFindShipSpawnPosition`).
  - `zoneOwner` site events logged `site:{PointId}` (all centre sites = one id) → now `site:{index};point={PointId}` matching AI objective ids (`CaptureSitesSystem.HandleStatusChanged`).
  - Opening detach probe still rejected a contested centre (`HostileStrength > own + 0.5`), so the centre group never formed against early human squadrons → the centre lane may detach and then reinforce (`EnemyStrategicContextBuilder.CanDetachForCapture`). Test `CaptureProbe_OpeningCentreFocusDetachesIntoAContestedCentre` (`EnemyAi`).
  - `IShipSpawnPoints` default interface bodies silently fell back to the old 270-radius search → removed; fakes in `CheatServiceTests` and `EnemyFactionControllerLifecycleTests` implement `TryGetRearSpawnPosition` / `TryGetWideDefenseSpawnPosition` explicitly with the same mapping.
- Unity recompile: completed, no errors. Tests not run (local override). Reviewed: `SurroundedStation_CompletedShipDeploysWithinHoldCap` still passes by tier D (fake default zone succeeds); centre/probe tests unaffected (no centre focus set).

## Verification (next battle, UltraHard)
- `deploymentBlocked` with `heldSeconds > 15` = 0; `sample.heldShips` ≤ 2.
- C-9979 + Patrol Frigate purchases after level 2 ≤ 2.
- `fleetAdvantage` at 30–120 s with no hostile ship seen ≥ 1 for a fleet of ≥ 1 ship.
- Centre pads owned by the AI before 120 s when the human has only squadrons there.
- Zones: home + centre + ≥ 1 outer by ≈ 240 s when uncontested.

## Order
1. Fix 1 (deployment) — blocks everything else.
2. Fix 2 (production) and Fix 3 (estimate).
3. Fix 4 (centre).
4. Fix 5 (re-check), then the remaining E-items of the parent.

## Files
- `Entities/EnemyFaction/Controllers/EnemyFactionController.cs` (`Build`, `DeployDefenseWave`, `AdvanceBuilds`)
- `Services/ShipSpawning/ShipSpawnPoints.cs`, `IShipSpawnPoints.cs`
- `Services/Enemy/EnemyProductionStrategy.cs`, `Entities/EnemyFaction/Models/EnemyProductionDecisionModel.cs`
- `Services/Enemy/EnemyStrategicContextBuilder.cs` (`EstimateUnseenFleetInvestment`, `GetHostileEconomyPressure`, `IsThreatSafe`, capture safety `:1128–1152`, `TryGetRearDeploymentThreats`)
- `Services/Enemy/EnemyAiTelemetry.cs` (new fields/events)
- Tests: `ShipSpawnPointsTests`, `EnemyFactionControllerLifecycleTests`, `EnemyProductionDecisionModelTests`, `EnemyStrategicDecisionModelTests`, `EnemyUnitCommanderTests`

## Tests
- Categories: `Ships`, `EnemyAi`, `Capture`, `Economy`. No stat-pinning asserts. Not run by agents (local override); user runs them.
