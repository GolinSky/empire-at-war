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
- Execution scope (2026-10-11): phases 0–6; user owns phases 7–8. Latest instruction skips new tests and captures; use saved 15:25/15:31 Editor evidence, compile and review each phase. Runtime parity, new attribution, listening and matched performance gates remain pending.
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
- [ ] Enable GC.Alloc call stacks for one short capture (3.7 MB/frame unattributed). Deferred: user requested no new captures.
- [x] Use existing baseline evidence as requested: `battle_20261011_122536_354` (4×, 78 frames / 10.143 s) + `Logs/RenderAudit/20261011T123143709Z-profiler/` (12 frames). These are observational; no matched 1× baseline or new child attribution.
- Implementation: reason markers export per-frame sample counts; due sort/lookup/emission/impact, squadron order/pilot/flight, ship orders/audio, audio mix and navigation reason/grid markers added. Unity compilation passed; new tests/captures skipped by request.
- Done gate pending: saved evidence does not contain these new child costs or allocation stacks.

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
- [ ] Use Phase 0 child markers to decide whether emission/impact work is the next target.
- Implementation: sequence/impact ID maps updated before callbacks, on initial add, reschedule, cancellation, swap-remove and disposal. Sorting/timestamps remain unchanged. Unity compilation passed; tests/captures skipped.
- Done gate pending: new child timings and runtime reentrant/cancellation parity are unmeasured.

### Phase 4 — Audio mix once per frame
- Cause: `ShipSfxService.Request` calls `ApplyMix()` on every accepted shot and loop renewal; `Tick` calls it again. Each call: 2 loops over 20 slots + `_mixer.SetFloat("SfxDuckVolume")`.
- [ ] Mark mix dirty in `Request`/`TryPlayVoice`/`Stop`; apply once per frame at the end of the frame (late tick after weapon fire).
- [ ] Call `SetFloat` only when voice-duck state changes.
- [ ] Check first-frame audibility: new voices start at volume 0 in `_audioService.Play(..., 0f, ...)`; flushing in the same frame keeps them audible without a one-frame gap.
- Done gate: `ApplyMix` ≤ 1/frame; listening check: same loudness, ducking, fades and voice stealing.

### Phase 5 — Navigation planning
- Cause: `ShipNavigationService.PlanRoute` builds a new `ShipPathGrid` (TempJob obstacle array + raster) per plan; 35 plans in one 81 ms frame.
- [ ] From Phase 0 counters, find the requesters of burst plans (stationary pursuit retries suspected).
- [ ] Add retry backoff for stationary/unreachable pursuit: re-plan only when target cell, obstacle set or order changes.
- [ ] Reuse one grid per frame per (map range, clearance, obstacle version) instead of per plan; keep idle-agent contacts and reservation ordering exact.
- [ ] Preserve nearest-reachable fallback and destination reservations.
- Done gate: plans/frame and max navigation frame fall; same routes and reservations in a reference scenario.

### Phase 6 — Squadron tick
- [ ] Use Phase 0 children (`UpdateOrder` / pilot / flight) to pick the leading cost.
- [ ] Remove duplicate/unchanged work first (e.g. re-evaluating unchanged orders each frame); lower update rates only with visible-behavior approval.
- Done gate: measured child cost reduced; formation, targeting, member loss and order transitions unchanged.

### Phase 7 — Hardpoint readiness independent of visuals (approved gameplay change, 2026-10-11)
- Cause: `WeaponHardPoint.IsBusy => _sequence.IsBusy || !_reloadTimer.IsComplete`; `_sequence` stays `WaitingForEffects` until every `ShotEffect` finishes (`IsVisualComplete`, muzzle flash, missile trail particles). Visual tails can delay the next salvo.
- [ ] `AttackSequenceState`: remove `_activeEffects`, `WaitingForEffects`, `RegisterEffect`, `TryCompleteEffect`; `StopEmitting` → `Ready`.
- [ ] `WeaponHardPoint`: drop `RegisterEffect` in `EmitScheduledShot` and `OnTurretEffectCompleted`; fire rate = salvo + `_reloadTimer` only.
- [ ] `ShotEffectPool`: remove the `effectCompleted` callback; pool still owns leases, drain and retirement.
- [ ] Size `maxIdle` for overlapping salvos (measured active high-water, not just `ShotsPerSalvo`) so overlap does not add Instantiate/Destroy churn (current 462 creations / 78 frames).
- [ ] Update attack plan contract: "Busy means emitting or waiting for leased effects" → superseded.
- Done gate: fire rate matches profile data per weapon; pool creations do not rise in warmed battles; no stuck/never-ready hardpoints.

### Phase 8 — Acceptance
- [ ] Matched captures vs Phase 0 baseline: CPU median/p95/p99, bytes/frame, GC stalls.
- [ ] Behavior: target priority, range/arc edges, shot counts, impact timing, pause, owner/hardpoint death, pool drain, audio mix, routes.
- [ ] Development Player confirmation of the largest gains.

## Edge Cases
- Tilted ship + target exactly on yaw/range boundary → keep serial fallback.
- Shared capture + target dies between capture and commit → generation check rejects; no redirect.
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
