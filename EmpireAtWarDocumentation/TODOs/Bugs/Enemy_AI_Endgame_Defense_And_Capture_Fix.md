---
category: Bugs
status: in-progress
created: 2026-11-01
system:
  - ai
parent: TODOs/Bugs/Enemy_AI_And_Faction_Balance
base_commit: 9b7c225d7
---
# Enemy AI — Endgame Defense and Capture Fix

## Goal

- Station under siege keeps a defending, firing fleet until the siege ends; emergency production arrives before the station dies.
- Main force can capture the centre with capitals in the group; capture detachments stop oscillating.
- Evidence and battle numbers → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review]]. Parent → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]].

## Review of 9b7c225d7 — Source-Confirmed Root Causes

| # | Battle symptom | Root cause in source |
|---|---|---|
| R1 | 48.5 s defense gap | `EnemyStrategicContextBuilder.BuildBaseThreat` needs the attacker currently visible (`_vision.IsVisible`) and within reach; station damage is never an input. Siege range 1,755 can exceed what the AI sees → pressure false, 20 s memory expires. |
| R2 | Same; also inconsistent group state | Pressure memory (`_rememberedStationThreat`, `_stationThreatSeenAt`) is per builder. `CreateGroupBuilder()` copies services only → every new/recreated group starts with no memory; the root builder used by `Reconcile` can disagree with group builders. |
| R3 | Defenders 419–485 from station, 1,464–1,988 from attacker | `EnemyTaskForceExecutor` `DefendBase` with `OwnBase != null`: AttackMove to station centre; re-issued whenever order ≠ AttackMove (completion → `None`). Defenders are re-pulled to centre every decision; `context.DefenseTarget` is ignored. |
| R4 | 5 CommonMiners bought while saving for Mandator | `EnemyProductionDecisionModel.EvaluateUltraHard`: mining below target precedes ships (and returns `None` = save for mining if unaffordable). Only `IsLevelDue` checks `DefendBase`. Target grows with `ActiveShipCount / 2`. |
| R5 | Mandator accepted 2711, deployed 2821 | Counter selection ranks value per credit/population; ignores `FactionData.BuildTime` and queue delay. Verified correction: AI uses per-request-ID `EnemyBuildQueue`; `ProductionQueueSnapshot` belongs to human production. Venator 55 s base → 148 s real. |
| R6 | 43 Capture↔Hold flips in 24.5 s | `EnemyShipTaskGroupPlanner.TryCreateCaptureGroup` builds the candidate snapshot without reserved objectives → sees main-force objective as free → creates detachment; group `Evaluate` with reservations → Hold; next `Reconcile` drops it (`!HasCaptureTarget`) → repeat. `CreateGroup(true)` also discards builder/decision memory each cycle. |
| R7 | Centre rejected in 436/973 rows | `IsObjectiveSafe`: clearance = max `NavigationRadius` of all group ships; centre + 4 half-radius points; `includeReservations: true` (verify whether own-fleet reservations count). |
| R8 | Decision log shows attacker while ships go home | Telemetry destination = selected threat, not the issued order point. |

## Decision

- One per-player station-pressure tracker shared by all builders. Why: R1+R2 in one owner. Avoid: per-group memory, revealing hidden attacker positions.
- Damage latch uses only own station health (shields + hull) — legitimate own-side knowledge. Avoid: reading attacker transforms not in team intel.
- Defense positions face the last known threat bearing; bounded interception only for counter-capable ships. Avoid: unconditional station-centre orders and unbounded chase.
- Emergency mode is a production input, not a new production system. Avoid: task-specific production queue.
- Capture fit checks each committed ship; every ship with a clear interior point captures. Only ships that fail fit hold perimeter. Keep ion/hostile-route checks.

## Implementation

### Phase 1 — Station pressure latch (R1, R2)

1. [x] Add `EnemyStationPressureTracker` (pure C#, one per player, VContainer scope beside the root builder). Inputs per tick: station shields+hull, `Time.time`, visible threat (from `BuildBaseThreat`).
2. [x] Latch on: visible threat OR durability loss ≥ `DAMAGE_LATCH_FRACTION` (start 0.5% max) within `DAMAGE_WINDOW_SECONDS` (start 5 s).
3. [x] Release only after `QUIET_SECONDS` (start 30 s) with no visible threat and no durability loss. Keep last known threat bearing/position from intel only.
4. [x] `CreateGroupBuilder()` passes the shared tracker; delete per-builder `_rememberedStationThreat` / `_stationThreatSeenAt`.
5. [x] `HasStationPressure` = tracker latched; `DefenseTarget` may be null while latched (damage-only) → `StationPressureReason` "Station losing durability; attacker not visible".

### Phase 2 — Threat-facing defense positions (R3, R8)

1. [x] `DefendBase`: pick a defense point = station centre + bearing to last known threat × (`stationRadius + clearance + min(defender weapon range, 0.6 × gap)`), clamped to a clear navigation point.
2. [x] Threat visible and within `INTERCEPT_LEASH` (start station radius + 1,500) of station → strike ships (`EnemyAttackTaskForce` selection, no assembly wait) Attack it; others AttackMove to the defense point.
3. [x] Damage continues ≥ `NO_FIRING_SECONDS` (start 8 s) while no defender has a target in range → advance defense point along the bearing (bounded by leash).
4. [x] Damage-only latch with no bearing → AttackMove ring around station (4 points), not centre.
5. [x] Re-issue orders only when the defense point moves > 200 units or target changes; never re-pull on order completion.
6. [x] Telemetry: add `orderDestination` and `defensePointReason` to decision rows; document in [[Tooling/Battle_Telemetry]].

### Phase 3 — Emergency production (R4, R5)

1. [x] Add `IsStationCritical` to `EnemyProductionSnapshot`: latched AND (station durability < `CRITICAL_FRACTION` start 50%, or estimated time-to-loss < 120 s from tracker loss rate).
2. [x] Critical → `EvaluateUltraHard`/normal path: skip Mining, Defense, Research, Level; Ship only.
3. [x] Critical → counter choice ranks by `value / (queueRemaining + BuildTime + travel)`; drop candidates whose arrival > estimated time-to-loss when any faster useful option exists. Normal reservations unchanged outside critical.
4. [x] `EnemySiteConstructionController.Tick`: skip site mines and satellites while critical (read tracker).
5. [x] Telemetry `production-wait`/purchase: add `critical`, `etaSeconds`, `timeToLoss`.

### Phase 4 — Capture fit and detachment stability (R6, R7)

1. [x] `TryCreateCaptureGroup`: pass current reserved objectives (main force evaluated first) into the candidate `Build`; create only if an unreserved objective exists.
2. [x] Distinguish `ReservedByOtherGroup` from `NoViableObjective`; reserved → keep detachment, retarget next objective; drop only after `NoViableObjective` persists `DETACHMENT_GRACE_SECONDS` (start 10 s).
3. [x] Reuse the existing capture group object (builder/decision model) instead of `CreateGroup(true)` per cycle.
4. [x] `IsObjectiveSafe` for capture: clearance = max radius of capturing ships (smallest-radius subset that can capture); escorts → perimeter points at `radius + escort clearance`. Verify and exclude own-fleet transient reservations; keep obstacle/hazard/hostile-route failures.
5. [x] Telemetry: capture rejection reasons split `transientReservation` / `obstacle` / `hostile`; reservation ownership is not exposed by the navigation API.

### Phase 5 — Secondary (Battle Review §6)

- [ ] Cap anti-fighter (Carrack) demand by observed hostile strikecraft minus existing screens.
- [ ] Before retaining a distant raid, check route/local danger at current position, not only destination.

### Phase 4b — Review of cf4a22850 (2026-11-01, source review only)

- [x] **C1 Capture escorts cut capture speed.** `ExecuteCapture` keeps only ships with the single minimum `NavigationRadius` (strict `>`) inside; all others go to perimeter `radius + NavigationRadius` (outside zone). `ReinforcementZoneModel` uses `captureSpeedPerNetShip` → fewer net ships, contested capture can stall. Fix: per-ship clear-point fit inside the zone; perimeter only for ships that fail it.
- [x] **C2 Intercept leash shorter than siege range.** `INTERCEPT_DISTANCE` 1,500 < observed attacker range 1,755 → that attacker is never intercepted; only +250 per 8 s advance (≈40 s for 300-range ships). Fix: leash = station radius + max(1,500, known attacker `WeaponRange` + margin); first advance jumps to `gap − own range`.
- [x] **C3 Damage latch too sensitive.** One burst ≥ 0.5% latches; any later loss holds 30 s → superweapon hit or lone bomber recalls whole fleet (detachment dissolved, offense aborted). Fix: require sustained loss (≥ 2 events over ≥ 3 s) or scale committed defenders to loss rate.
- [x] **C4 Defenders converge on one point.** Same bearing for all ships; distance differs only by range → clumping. Spread laterally (±15–30° per slot).
- [x] C5 (minor) Critical disables squadrons; no ship unlocked → nothing bought. Squadrons are the fastest arrivals.
- [x] C6 (minor) Detachment whose objectives are all reserved holds indefinitely with an idle ship; rejoin main after grace.

## Tests (authored only; local override → not run)

- Authored/updated fixture categories: `EnemyAi`, `Combat`, `GameFlow`, `Abilities`. Station facade changes also touch `Ships`; no tests were executed.
- Tracker: damage-only latch; quiet release; shared across two group builders.
- Defense: point faces threat bearing; no re-issue on completion; leash bound; ring fallback.
- Production: critical skips Mining; faster defender beats Mandator when ETA > time-to-loss; non-critical keeps reservation.
- Planner: reserved objective → no detachment flip over 10 decisions; capital escort does not block capture fit.

## Acceptance (user, in-game)

- Siege: no defense gap > 5 s while station loses durability; defenders have targets in range within 15 s of latch.
- Critical: no Mining/Defense purchases while critical; first emergency unit deploys before station loss when credits allow.
- Centre: main force reaches ≥ 100% capture at least once with capitals present.
- Detachment: < 5 Capture↔Hold changes per 30 s.

## Files

- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs` — `BuildBaseThreat`, `IsObjectiveSafe`, copy constructor.
- `Assets/Scripts/Services/Enemy/EnemyTaskForceExecutor.cs` — `DefendBase` branch.
- `Assets/Scripts/Services/Enemy/EnemyShipTaskGroupPlanner.cs`, `EnemyShipTaskGroup.cs` — detachment lifecycle.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyProductionDecisionModel.cs`, `Services/Enemy/EnemyProductionStrategy.cs`, `EnemyCounterProductionModel.cs` — emergency production.
- `Assets/Scripts/Services/Enemy/EnemySiteConstructionController.cs`, `EnemyAiTelemetry.cs`.
- New: `Assets/Scripts/Services/Enemy/EnemyStationPressureTracker.cs`.

## TODO

- [x] Phases 1–4 implemented. Phase 5 remains unimplemented and deferred until a fresh battle; plan stays active.
- [ ] Update [[Architecture/Enemy_AI_Objectives]] with tracker, defense-point and emergency rules on completion.

## Implementation Status — 2026-11-01

- Agreed implementation scope: Phases 1–4. Phase 5 remains deferred until fresh battle evidence, as scheduled above.
- Verification: source review, compilation/import and scoped diff checks; tests authored only, not executed under the checkout override.

## Verified Implementation Details

- Queue correction: identical request IDs build FIFO; different IDs build in parallel. Emergency ETA sums only the matching ID lane plus build and base-speed travel to known threat.
- True station maximum comes from `IStationDurabilityFacade` / `StationDurabilityFacade` (`HealthModel.MaxHull + MaxShields`), including restored stations with zero shields.
- Capture strategic clearance uses the minimum radius to determine whether any ship fits. Execution checks each ship at centre + four half-radius samples; only failed-fit ships use perimeter orders; strategic checks ignore all transient ship reservations because the API has no ownership filter. Navigation source confirms own registered ships were included.
- Capture candidate object and its memory persist; creation happens after combat-group reservations and publication, with execution next decision tick. `ReservedByOtherGroup` preserves membership for the same 10 s no-objective grace; persistent reservation starvation releases the detachment.
- Authored regressions: tracker latch/release/reset/repeated observations; new-group shared pressure after hidden contact; defense bearing/ring/completed-order retention/leash; critical category choice, timely counter, per-ID queue delay; reservation/obstacle distinction, capturer clearance and capital escorts. Tests not run.
- Reusable guidance extracted → [[Architecture/Enemy_AI_Objectives]]; telemetry fields → [[Tooling/Battle_Telemetry]].
- Remaining implementation: Phase 5 anti-fighter demand and route/local raid danger, after fresh battle evidence. In-game acceptance listed above remains unverified.

- Emergency spending integration: new superweapon charges also pause while critical; existing paid charging and firing continue. Regression authored in `PlayerSaveTests`.

## Verification — 2026-11-01

- Unity refreshed scripts and generated metadata; compilation/import finished (`isCompiling=false`, `isUpdating=false`, `scriptCompilationFailed=false`). New tracker and station durability facade types are loaded.
- New console errors since pre-change cursor 2814: none. Historical errors were not cleared or attributed to this change.
- Scoped `git diff --check` passed. Independent production/defense integration review corrected depleted-shield maximum durability and gated new superweapon spending.
- Automated tests and Play Mode: not run under checkout policy. No battle performance or acceptance claim.

## Phase 4b Outcome — 2026-11-01

- C1: all fitting ships capture, regardless of relative size; each receives its clear interior point. Failed-fit escorts rejoin capture when an interior point becomes clear.
- C2: leash = station radius + max(1,500, last observed attacker weapon range + 300). Visible counter-capable ships intercept immediately. First 8 s no-firing advance jumps to known gap − own range + ship clearance, bounded by leash.
- C3: damage-only latch/refresh needs ≥2 losses spanning ≥3 s within 5 s and ≥0.5% maximum durability loss. Single hits and no-loss polling do not latch/extend defense; visible threats remain immediate.
- C4: support slots spread across a threat-facing arc bounded to ±30°; interceptors receive distinct cached fleet formation offsets.
- C5: critical counter selection includes unlocked squadrons, bypasses ordinary ship/squadron ratios, and ranks useful combat value by arrival time when hostile composition is unknown. Squadron ETA includes cruise-speed travel.
- C6: no available objective, including reservations by other groups, consumes the 10 s grace; released ships rejoin combat/main groups.
- Regression coverage authored/updated: mixed-size capture, blocked capital perimeter, off-centre fit/rejoin, 1,755-unit interception + bounded chase, lateral slots, sustained/isolated damage and critical squadron selection with/without unlocked ships.
- Verification: Unity script refresh/import and compilation completed (`isCompiling=false`, `isUpdating=false`, `scriptCompilationFailed=false`); scoped diff checks passed. Existing console errors are historical (latest error seq 1612); no current compilation failure. Tests and Play Mode not run under checkout override. Categories touched: `EnemyAi`, `Combat`.
- Reusable rules updated → [[Architecture/Enemy_AI_Objectives]]. Phase 5 remains unimplemented/deferred until fresh battle evidence; plan stays `in-progress`. In-game acceptance remains pending.
