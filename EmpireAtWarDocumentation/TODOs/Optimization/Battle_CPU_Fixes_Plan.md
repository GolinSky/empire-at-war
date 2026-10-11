---
category: Optimization
status: in-progress
created: 2026-10-11
updated: 2026-10-11
---
# Battle CPU fixes — implementation plan

## Goal
- Cut main-thread cost in battle; latest evidence is CPU-bound: median CPU 96.6 ms, GPU 3.0 ms ([[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]]).
- Apply concrete source-verified fixes in measured order; one gameplay change (phase 7) is explicitly approved.
- Rendering/instancing work is out of scope: GPU is not the bottleneck.

## Evidence → fix map

| Area | Latest cost | Verified cause in source | Phase |
| --- | --- | --- | --- |
| Target preparation | 16–21 ms/frame; ~200 requests, 34,144 candidate records/frame | Per-hardpoint candidate rebuild + Transform reads + second copy; 9,864/15,579 requests fall back to serial | 1, 2 |
| Due events | 12–17 ms/frame; 287 ms spike | `TryFindEvent` linear scan per due event; 40,792 inspected / 11,821 committed | 3 |
| Audio | share unknown | `ApplyMix` loops 20 slots ×2 + `AudioMixer.SetFloat` on every accepted shot and loop renewal, plus `Tick` | 4 |
| Navigation | 4.8 ms avg; 81 ms burst = 35 plans | New `ShipPathGrid` (TempJob arrays + obstacle raster) per `Plan`; stationary pursuit retries | 5 |
| Squadrons | 8.47 ms/frame | `Squadron.Tick` = `UpdateOrder` + `_pilot.Tick` + `_flight.Step`; children unmeasured | 6 |
| Hardpoint readiness | gameplay, not perf | `AttackSequenceState` waits for visual completion (`WaitingForEffects`) | 7 |

## Rules
- Implementation execution scope (2026-10-11): phases 0–6; user owns phases 7–8. That execution skipped tests/new captures and used saved 15:25/15:31 evidence. Later user-requested live audit is recorded under Phase 8; runtime parity, listening and matched performance gates remain pending.
- Measure → change one thing → measure. ≥3 matched 10 s runs per variant, 1× and 4×; report median/p95/p99 + bytes/frame.
- 4× Editor at ~8 FPS inflates per-frame work; compare only matched runs, confirm big wins in a Development Player.
- Phases 1–6 must not change gameplay: same targets, shot counts, impact order/timing, cancellation, audio audibility.
- Read live source before each phase; one writer per combat file; follow `AGENTS.md` Unity test safety.
- Nested profiler scopes are not additive.

## Implementation

### Phase 0 — Baseline and missing attribution (no behavior change)
- [x] Add fallback-reason counters in `WeaponTargetSelectionJob` / `TargetSelectionBatch.ApplyTargetSelection` / `WeaponComponent.CommitTargetSelection`: parent tilt, range boundary, yaw boundary, degenerate direction, target-version change, invalid candidate, out-of-span.
- [x] Child markers in `CombatAttackCoordinator.CommitCollectedDueEvents`: sort, `TryFindEvent`, shot emission, impact/damage.
- [x] Child markers in `Squadron.Tick`: `UpdateOrder`, `_pilot.Tick`, `_flight.Step`. Split `Ship.Tick` order/state vs `AudioShipComponent.UpdateAudio`.
- [x] `ShipSfxService`: marker + count for `ApplyMix` calls/frame.
- [x] Navigation: count `Plan` calls by caller/reason and grid builds/frame.
- [x] 2026-10-11 GC.Alloc call stacks captured and decoded (latest: 17:34 UTC, 40 frames, 3,063,858 B; all 45,927 samples resolved); see Phase 9 audit evidence. Recording restored to `None` while playing.
- [x] Use existing baseline evidence as requested: `battle_20261011_122536_354` (4×, 78 frames / 10.143 s) + `Logs/RenderAudit/20261011T123143709Z-profiler/` (12 frames). These are observational; no matched 1× baseline or new child attribution.
- Implementation: reason markers export per-frame sample counts; due sort/lookup/emission/impact, squadron order/pilot/flight, ship orders/audio, audio mix and navigation reason/grid markers added. Unity compilation passed; new tests/captures skipped by request.
- Attribution evidence now includes child costs and resolved allocation stacks (Phase 9). Matched timing and behavior acceptance remain under Phase 8.

### Phase 1 — Stop target-selection serial fallbacks
- Cause: `WeaponTargetSelectionJob.Execute` falls back when `ParentRotation.x/z > 0.000001` (any ship tilt/bank). Serial `WeaponTargetSelector` uses `(Quaternion.Inverse(parent) * LookRotation(dir, up)).eulerAngles.y`; job uses `atan2` of local direction → differ for tilted parents, so job bails out.
- [ ] Confirm via Phase 0 counters that tilt is the leading reason.
- [x] Use Burst-compatible `math.EulerZXY(inverseParent * worldAim).y` matching serial Unity rotation order; remove blanket tilt rejection. Keep range/yaw/degenerate fallback, including near-gimbal Euler branch ambiguity. Unity compilation passed; existing tilted-parent expectation updated, tests not run.
- Tilt attribution/randomized parity/time gate pending: latest saved capture lacks reason counts; user skipped new runs.
- [ ] EditMode parity test: random tilted/rolled parents, ranges and arcs → job and serial return the same `CandidateIndex` and aim.
- Done gate: fallbacks drop to boundary cases only; target capture + selection time falls; zero parity mismatches.

### Phase 2 — Capture targets once per ship, not per hardpoint
- Cause: `WeaponComponent.CaptureTargetSelection` clears `_targetPositions` and rebuilds the full candidate list per request; `TargetSelectionBatch.CaptureTargetSelections` copies it again.
- [x] Per `LateTick` batch, capture each `WeaponComponent`'s ordered candidates once (validity, generation, position, target class).
- [x] Hardpoint requests reference the shared span; apply `CanEngage` (strikecraft-only) as a per-request class mask inside the job instead of a filtered copy.
- [x] Write candidates directly into the batch buffer; remove the second copy.
- [x] Invalidation: capture happens at one boundary per frame; keep `_targetVersion` + generation checks at commit.
- Implementation: batch-owned candidate list receives snapshots directly; owner spans and group acquisition eligibility are cached only during capture. Native class flags preserve strikecraft-only filtering on both paths. Unity compilation passed; existing job fixture supplies the new native buffer. New runs skipped.
- Done gate pending: candidate counts, warmed allocations and target parity require the next user capture.

### Phase 3 — O(1) due-event lookup
- Cause: `TryFindEvent` scans `_impacts` / `_sequences` for every due event; records use swap-remove (`RemoveImpactAt`, `RemoveSequenceAt`).
- [x] Keep `Dictionary<long, int>` (event sequence → index) per record list; update on add and swap-remove. Alternative: store index in `DueEvent` and verify `EventSequence` matches.
- [x] Preserve stable due order, cancellation, generations and reentrant record changes during commit.
- [x] 2026-10-11 live audit child attribution: impact 4.781 ms/frame vs emission 1.283 and lookup 0.009; impact is the next measured due-work target. Matching/parity gate remains open.
- Implementation: sequence/impact ID maps updated before callbacks, on initial add, reschedule, cancellation, swap-remove and disposal. Sorting/timestamps remain unchanged. Unity compilation passed; tests/captures skipped.
- Done gate pending: new child timings and runtime reentrant/cancellation parity are unmeasured.

### Phase 4 — Audio mix once per frame
- Cause: `ShipSfxService.Request` calls `ApplyMix()` on every accepted shot and loop renewal; `Tick` calls it again. Each call: 2 loops over 20 slots + `_mixer.SetFloat("SfxDuckVolume")`.
- [x] Mark mix dirty in `Request`/`TryPlayVoice`/`Stop`; apply once per frame at the end of the frame (late tick after weapon fire).
- [x] Call `SetFloat` only when voice-duck state changes.
- [ ] Check first-frame audibility: new voices start at volume 0 in `_audioService.Play(..., 0f, ...)`; flushing in the same frame keeps them audible without a one-frame gap.
- Implementation: dirty mix flush in `ShipSfxService.LateTick` at priority +1000, after coordinator −1000; new sources still start at zero and receive their volume during that frame's LateTick. Active voices keep per-frame gain/fade updates; duck mixer writes only on state transitions. Unity compilation passed, no new console errors; listening/captures skipped.
- Done gate pending: runtime first-frame audibility, ≤1 mix/frame and listening parity require the user check.

### Phase 5 — Navigation planning
- Cause: `ShipNavigationService.PlanRoute` builds a new `ShipPathGrid` (TempJob obstacle array + raster) per plan; 35 plans in one 81 ms frame.
- [ ] From Phase 0 counters, find the requesters of burst plans (stationary pursuit retries suspected).
- [x] Add retry backoff for stationary/unreachable pursuit: re-plan only when target cell, obstacle set or order changes.
- [x] Reuse one grid per frame per (map range, clearance, obstacle version) instead of per plan; keep idle-agent contacts and reservation ordering exact.
- [x] Preserve nearest-reachable fallback and destination reservations.
- Implementation: `ShipNavigationCache` owns frame grids and exact stationary-pursuit results. Raster key compares map bounds, clearance and every height-filtered/idle contact; floods start fresh per plan to preserve direct-route candidate ordering. Failed pursuit retries resume on request/pose/speed/contact/reservation changes; movement, Stop and Unregister invalidate appropriately. Frame-end +2000 cleanup and disposal release native buffers. Existing navigation fixture disposes services.
- Conservative limit: exact target changes or any moving reservation invalidate stationary reuse; no approximate cell backoff or delayed route response introduced. Saved evidence identifies 35 plans/81.268 ms burst but not their runtime callers.
- Unity compilation passed; tests/captures skipped. Done gate pending: cache hit counts, routes/reservations and frame timings require the user capture.

### Phase 6 — Squadron tick
- [x] 2026-10-11 live audit: `UpdateOrder` 10.725 ms/frame vs pilot 0.205 and flight 1.027; attribute order selection next. No additional implementation or cadence change.
- [x] Remove duplicate pilot work: one formation world offset per live rank per travel tick, reused by lag and steering; one aim-position read per used target index per engage tick. Buffers allocated once with pilot; member-loss rank ordering retained. No update-rate changes.
- Saved evidence: `Squadron.Tick` 8.469 ms / 109 calls per frame; children were uninstrumented. Choice is source-verified duplicate work, not a claimed measured leading child.
- Unity compilation passed; tests/captures skipped. Done gate pending: child timings and formation/targeting/member-loss/order parity need the user capture.

### Phase 7 — Hardpoint readiness independent of visuals (approved gameplay change, 2026-10-11)
- Owner: user; skipped in this execution.
- Cause: `WeaponHardPoint.IsBusy => _sequence.IsBusy || !_reloadTimer.IsComplete`; `_sequence` stays `WaitingForEffects` until every `ShotEffect` finishes (`IsVisualComplete`, muzzle flash, missile trail particles). Visual tails can delay the next salvo.
- [ ] `AttackSequenceState`: remove `_activeEffects`, `WaitingForEffects`, `RegisterEffect`, `TryCompleteEffect`; `StopEmitting` → `Ready`.
- [ ] `WeaponHardPoint`: drop `RegisterEffect` in `EmitScheduledShot` and `OnTurretEffectCompleted`; fire rate = salvo + `_reloadTimer` only.
- [ ] `ShotEffectPool`: remove the `effectCompleted` callback; pool still owns leases, drain and retirement.
- [ ] Size `maxIdle` for overlapping salvos (measured active high-water, not just `ShotsPerSalvo`) so overlap does not add Instantiate/Destroy churn (current 462 creations / 78 frames).
- [ ] Update attack plan contract: "Busy means emitting or waiting for leased effects" → superseded.
- Done gate: fire rate matches profile data per weapon; pool creations do not rise in warmed battles; no stuck/never-ready hardpoints.

### Phase 8 — Acceptance
- Owner: user; 2026-10-11 live audit requested and completed without stopping/pausing Play Mode. Acceptance remains open.
- [x] Observational evidence: 10.047 s / 91-frame battle capture at 1×; fresh 60-frame Profiler and raw main-thread allocation attribution; two Render Graph frames and six Rendering Debugger panels; read the existing complete 888-event Frame Debugger capture and logs.
- Current frame median/p95/p99: 104.981/194.972/227.891 ms; mean 1,365,793 allocated B/frame. Phase 0 baseline is 4× with different workload/source → no matched gain claim.
- Attribution: target capture 22.789 ms, squadron orders 10.725 ms, projectile update 7.865 ms; due lookup 0.009 ms, SFX mix 0.007 ms and exactly one mix/frame. Impact/radar scopes contain 67.6%/21.3% of fresh main-thread allocated bytes; exact allocating stacks remain absent.
- Evidence and full next-step checklist: [[TODOs/Optimization/Battle_CPU_Fixes_Plan - Research]]. New Frame Debugger capture requires pausing and was skipped under the active no-interruption constraint.
- [ ] Matched captures vs Phase 0 baseline: ≥3 × 10 s per variant at 1× and separately 4×, fixed scenario/camera/population/settings; CPU/frame median/p95/p99, bytes/frame, GC stalls.
- [ ] Behavior: target priority, range/arc/tilt edges, shot counts, impact timing, pause, owner/hardpoint death, pool drain, first-frame audio/ducking, routes/reservations and squadron formations/member loss.
- [ ] Phase 7 prerequisite for whole-plan acceptance: live source still has `WaitingForEffects`, effect-completion callbacks and `ShotsPerSalvo` pool cap. Finish its fire-rate/overlap gates, or explicitly limit acceptance to phases 0–6.
- [ ] Development Player confirmation of the largest gains. Tests/builds/scenario resets were not run against the protected live session.

### Phase 9 — Post-audit hotspot fixes (2026-10-11)
- [x] `2f57563f`: batch-shared target position cache; lazy candidate rebuild; allocation-free `AddCandidates`, `RefreshStaleUnits`, `HasLiveHardPoints`. Target capture 22.8 → 5.8 ms mean (CSV, different workload).
- [x] `8c20aa58`: per-frame vision centre snapshot (`OwnedCircleSet`); Burst fog grid jobs; squadron idle/guard/attack-move search every ≤0.25 s; ship facing solve reused while bearing holds (2° / 1 s); per-hardpoint destruction subscriptions removed (wreck transition re-checked at capture); bolt single transform write + `Projectile.Track/Move/Effect` markers.
- [x] 15:43 UTC recapture of `8c20aa58`: main thread median 44.3 ms (audit 86.0, 15:18 62.2); GC 97 KB/frame mean; allocation stacks 147 KB/frame. Evidence: `Logs/BattleAudit/20261011T1543Z/recapture.md`.
- [x] `4327e1ec`: bugs found in that recapture — shield clip over 1,024 planes per tracked bolt per frame (`Projectile.Track` 18.5 ms) → Burst `ShieldSurfaceClipJob`; squadron search lockstep (8.6 ms every 0.25 s) → jittered reset; `FindNearestWalkableCell` full-grid scan (60.5 ms plan spike) → bounded ring search with parity test; `ForceComposition.Add` boxed enumerator.
- Tests: EditMode 1,408/1,432; 24 failures are pre-existing asset/data checks unrelated to these files.
- [x] 16:08 UTC recapture of `4327e1ec`: frame median 33.1 ms (~30 FPS), main thread 31.2 ms (audit 86.0); target capture 3.8, projectiles 1.8 (`Projectile.Track` 18.5 → 0.83 Profiler), squadron orders 0.83 (max 5.9), ship orders 0.71; allocations 23.7 KB/frame; 0 console errors. Evidence: `Logs/BattleAudit/20261011T1608Z/recapture.md`.
- [x] `ShipSfxService.GetGain` 52 B/call = 5-argument `Mathf.Max` params array → two-argument rewrite (`889b247e`).
- [x] Navigation burst: 17 plans in one frame → 36.5 ms (route build/flood). Resolved by the per-frame plan cap in `d138cb65`.
- [x] 16:18 UTC matched-workload analysis (52,171 candidates vs audit 52,282): main thread median 55.7 ms vs 86.0; target capture 10.5 vs 22.7. Evidence: `Logs/BattleAudit/20261011T1618Z/analysis.md`.
- [x] 2026-10-11 pool lifecycle fix: `ShotEffectPool` retains active GameObjects; lease acquisition enables the `ShotEffect` component, return/prewarm calls `ResetForPool`. All four shot types explicitly stop/clear idle particles; muzzle/beam/pulse renderers are reset. Ten projectile prefab lifecycle tests pass. Latest 5.54 ms activation cost has not been recaptured.
- [x] Navigation bursts up to 72 ms (24 plans): 104/135 plans from ship states' preserve-course pursuit, 29 from `EnemyUnitCommander`; group retarget, not lockstep.
- [x] `d138cb65` (user chose A+B+F): destination rings 16 → 2, slots ordered nearest the ship, first routable slot wins; fallback nearest-reachable probed from the ring slot facing the ship (1 ring); orders queue plans, `ShipNavigationService.LateTick` runs ≤4 per frame FIFO; queued ship counts as moving. Not chosen: 7 → 3 route variants, stand-off pursuit, 0.5 s re-path limit, nearby-only idle obstacles. Tests 1,409/1,433 (24 pre-existing). Runtime recapture pending.
- [x] `889b247e`: `GetGain` params-array allocation (already in the user's build).
- [x] `48210bd6`: route variants 7 → 3 (heading curve, turned curve at handle scale 0.5, polyline); pursuit re-path of a moving ship ≤ every 0.5 s (`ShipMoveComponent.REPATH_INTERVAL`), new orders immediate; idle ships removed as route obstacles (`IsIdle`/`HullsOverlap` deleted). Tests 1,409/1,433. Runtime recapture pending.
- [x] 16:50 UTC analysis of `48210bd6` (283 weapons, 154 squadrons): frame median 49.3 ms (~20 FPS), main 47.0; navigation max 18.0 ms at ≤4 plans/frame (was 72.2); target capture 8.2; projectiles 1.4; 137 KB/frame GC mean. Evidence: `Logs/BattleAudit/20261011T1650Z/analysis.md`.
- [x] 2026-10-11 selection reuse: `ShipGroupUi` retains group instances; `ShipSelectionGroupUi` retains up to its warmed card count and reuses ability buttons/materials. Clear/rebind releases old health, focus and click references. Regression covers 4 individual cards → 5-unit summary → 2 cards → clear, with identical card instances and no duplicate click/health subscriptions. The ~2.5 MB selection spike still needs a warm-selection allocation recapture.
- [x] 2026-10-11 target capture: `TargetSelectionCapture` shares health/live-hardpoint/class checks per target and generation/position snapshots per hardpoint within one capture. Buffers retain capacity; owner-specific `AttackData`, candidate ordering and live commit validation remain intact. This addresses repeated health scans; no persistent native-candidate redesign or measured speedup is claimed.
- [x] 2026-10-11 bolt particle simulation: all five `BoltShot` prefabs have one static burst, no ongoing emission or animated particle modules. `BoltShot.Play` simulates the first 0.001 s then leaves the system paused; `Fly` still moves it and clears it at impact. Missile trails, beam smoke and ion flash behavior unchanged. Counts 1/2/4/8/1 retained; five seeded mesh comparisons against 0.25 s normal simulation pass after movement/rotation (matching colors/UVs, vertices within 0.001 units).
- Verification: Unity compilation successful, 52/52 weapon tests + 1/1 selection reuse test. New source files/metas imported; `MainMenuScene` clean. A new test's `Camera` namespace collision was corrected before the passing compile/run. Evidence: `Logs/BattleFixes/20261011/weapon-tests.json`, `ui-tests.json`, `verification.json`.
- [x] 2026-10-11 17:31 UTC observational recapture (`e7a1d131`, dirty; 94 ships, 130 squadrons, 252 weapons): 187 frames / 10.008 s at 1×; frame median/mean/p95/max **51.99/53.28/71.40/93.21 ms** (audit median 104.98; previous 49.28), **67,367 B/frame** mean. Candidates 28,218/frame vs 46,907 previously; projectiles 1,556 vs 1,162 → no matched latest-fix speedup claim. [Evidence](<F:/Private/empire-at-war/Logs/BattleAudit/20261011T1730Z/analysis.md>).
- [x] Fresh 90-frame Profiler (1158–1247) + 40-frame GC stacks (1250–1289): activation **0.065 self ms/frame** (previous 5.54), particles **0.768 self** (previous 2.41); target capture **7.360 self**. Runtime: 2,824 idle effect components disabled on active GameObjects; 1,093/1,093 flying bolts paused. GC recording restored to `None` at 17:34:40 UTC; Play Mode uninterrupted; zero retained console warnings/errors.
- [x] Allocation attribution: **3,063,858 B / 40 frames**, all 45,927 main-thread samples resolved. Selection refresh stacks total 94,554 B; no captured `ShipGroupUi` clone/material-constructor stack. Natural entity-removal refreshes support reuse; controlled warmed selection remains unverified.
- [ ] Remove unused `StringBuilder` and vertex/UV formatting in `MPImageHelper.AddQuad` (`Assets/ThirdParty/MPUIKit/Runtime/Scripts/Utility/MPImageHelper.cs:240`): **656,712 B / 40 frames**, 16.4 KB/frame (21.4% of captured allocation). Inspect material-update strings and subscription churn afterward; no fix in this audit.
- [ ] Reduce navigation fallback after normal candidate planning fails: **223 plans/90 frames**, 219 preserve-course, 174 nearest-reachable searches; Plan **8.89 inclusive ms/frame**, flood **4.315 self**, `TryFindNearestReachableCell` full-grid scan **3.892 self**. ≤4 plans/frame holds. Attribute originating queued order before changing pursuit behavior; this is separate from the fixed nearest-walkable ring search.
- [ ] Reproduce Profiler frame 1202 asset-loading hitch: **629.74 ms raw main thread**, **499.79 ms File.Read** mainly below GPU Resident Drawer `InstanceCullingBatcher.BuildBatch`; outside CSV window. Identify assets/renderer introduction and verify preload/warm-up in Player.
- [ ] Matched target/effect/particle recaptures and controlled warm-selection repeats remain open. CSV pool misses: **160 projectile creations / 10 s**, 0.527 inclusive ms/frame; inspect per-weapon capacity/retirement before resizing. Exact 11.3 ms CSV ship-order spike and combat visual acceptance remain unverified.
- Procedure for future captures: `Tools/BattleAudit/README.md` + `Tools/BattleAudit/battle_audit.py`.
- [x] 2026-10-11 approved navigation route commitment: attack endpoints stay fixed during each leg; attack-move/guard combat/hunt choose firing-range standoff points; guard following and ability approach keep their active leg. New targets/orders still replace a route; combat can stop early in range.
- [x] All ship plans, including hyperspace completion, deferred corrections and arrival conflicts, enter the existing FIFO queue (≤4/frame). Arrival permits one correction; another conflict or failed plan holds until an autonomous destination shifts by ≥navigation radius or a fresh explicit order arrives. A newer queued order wins over old completion callbacks.
- Verification: 71/71 focused EditMode tests (59 movement/navigation, 2 NavigateState, 3 Stop, 7 movement-model), including 13 new cases. Unity compilation clean; new test script/meta imported; `MainMenuScene` clean; no new Console errors after cursor 47568. Evidence: `Logs/ShipNavigation/20261011/`. Existing >200-line movement/state classes retained; a later extraction of queue/lifecycle coordination can be considered separately.
- Remaining: matched battle capture and pursuit/guard feel in play. Slower response to moving targets is intentional; no measured speedup claimed.
- [ ] Unattributed: 5 MB CSV allocation frames and a 9-frame squadron-order streak (~12.3 ms); neither reproduced under Profiler.
- Behavior changes to confirm in play: idle squadrons react ≤0.25 s later; stationary ship facing re-solves on 2° bearing change or 1 s.

## Edge Cases
- Tilted ship + target exactly on yaw/range boundary → keep serial fallback.
- Shared capture + target dies between capture and commit → preserve existing version/generation serial retry; never commit the stale snapshot.
- Event swap-remove during commit loop → index map must update before next lookup.
- Mix flushed before weapons fire → one silent frame for new voices; flush after firing.
- Grid reuse across agents with different hull spans/heights → key by clearance/height or keep per-agent contacts.
- Phase 7 overlap → pool high-water rises; retire-above-cap must not destroy/recreate every salvo.

## Open decisions
- Keep attack-plan corrections? (a) destroyed firing hardpoint cancels remaining salvo; (b) destroyed target hardpoint drops pending damage. Currently implemented; not yet confirmed by user.

## Files
- `Assets/Scripts/Components/Weapon/{WeaponComponent,TargetSelectionBatch,WeaponTargetSelectionJob,WeaponTargetSelector,CombatAttackCoordinator,AttackSequenceState}.cs`
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`, `Components/ViewComponents/Weapon/{ShotEffect,ShotEffectPool}.cs`
- `Assets/Scripts/Services/Audio/ShipSfxService.cs`
- `Assets/Scripts/Services/ShipNavigation/{ShipNavigationService,ShipPathGrid}.cs`, `Components/Ship/Movement/ShipMoveComponent.cs`
- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Services/Timing/{BattlePerformanceCapture,BattleProfilerMarkers}.cs`
- Evidence: [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]]; context: [[TODOs/Optimization/Battle_CPU_Performance_Plan]], [[TODOs/Optimization/Battle_Attack_Optimization_Plan]]
