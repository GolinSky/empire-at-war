---
category: Optimization
status: done
created: 2026-11-16
completed: 2026-11-16
acceptance: pending-user
---
# Live Battle CPU and Pool Demand

## Decision
- Same-decision capture-only probes replace full candidate builds; use raw per-ship advantage and isolate history. No rotating-cursor delay.
- Shared FIFO whole-salvo admission replaces summed physical capacity. Loading floor = largest normal loadout; runtime target = active + reserved + waiting, bounded below by floor. Logical normal/Boost caps remain.
- Squadron periodic offsets; ship event-triggered minimum gap 0.5 s. Preserve earlier normal deadlines and pause clocks.
- Committed moving hardpoint pursuit scans use 100 ms cadence for unchanged geometry; target motion/replacement and arrival invalidate. Development markers/counters added.

## Implementation
- [x] Capture probe, raw-advantage/history regressions.
- [x] Squadron stagger and separate ship/periodic/launch diagnostics.
- [x] Shared-cache admission, demand growth, peak/latency counters and lifecycle regressions.
- [x] Pursuit cadence, invalidation and branch markers; regressions.
- [x] Compile/import and authorized quick non-explicit test categories.
- [x] Two saved-profiler replay captures; pool/AI/CPU/Console verification.
- [x] Measured world-change acceleration follow-up; final EnemyAi tests and replay.
- [x] Reusable rules extracted to [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Capture probes and decision cadence · 2026-11-16]] and revised pooled-overlap guidance.

## Verification
- EnemyAi final 471/471; Ships 209/209; Abilities 209/209 passed.
- Combat initial 289/291; corrected timer assertion and pool fixture rerun 25/25 passed. Unchanged HyenaBomber/DroidBomberLaser ownership taxonomy test remains failing; no unrelated asset changes.
- Navigation initial 353/354; corrected existing 90 m target versus 80 m mock range; preserved behavior assertions. Pursuit fixture rerun 66/66 passed.
- Two 30 s Editor samples: mean/p95 11.823/18.373 ms and 14.014/26.263 ms. Evolving workload; no matched FPS improvement claim.
- Both samples: 2,147 physical shots, no admission waits or growth during sampling. Prior snapshot 28,870 physical shots under different combat conditions.
- Final fastest AIs: 59 decisions/30.209 s despite 16 world events. No launches sampled, so repeated-launch batching was not justified.
- Native 40-frame capture: largest ship AI calls 8.8–11.8 ms. Raw attack-state 280 calls total ~0.79 ms; heavy-capital branch absent, original 4.4 ms mechanism remains unproven.
- Boost probe: 13 equipped ships, none passed conservative engagement gate, 0 activations. Unit tests cover overflow/cancellation/fairness; live overflow remains acceptance.
- No new replay Console errors; named save hash unchanged; battle was playing after verification; final check found Editor stopped after an assembly reload concurrent with unrelated UI/faction source edits. No restart attempted. No full/exhaustive suite run.

## Files
- `Logs/BattleAudit/20261116-cpu-fix/analysis.txt` — complete evidence, test paths and limits.
- `Logs/RenderAudit/20261116T134817413Z-profiler` — native capture.
- `Logs/BattleAudit/20261116-live-cpu` — baseline evidence.

## TODO
- User acceptance: matched Development Player timings before broader performance claims. Live heavy-capital and boosted-overflow verification completed below; measured latency/cost limitations remain. Queue → [[TODOs/User_Acceptance_Queue]].

## Live Follow-up · 2026-11-16
- User requested finishing heavy-capital and boosted-overflow verification. No production edits or automated suite reruns; controlled live replay tests.
- Praetor 130 mounts vs Executor 177 live target points: actual scan median 4.548 ms. Stationary-position/slow-rotation target: 9 scans/60 state updates, 0.688 ms/frame scan cost; other state work 0.00255 ms/frame.
- Confirmed translation 25.267 m/1.579 s: 60 scans/60 frames, 4.405 ms/frame scan cost. Target-motion invalidation retains the original hotspot; do not describe the cadence as bounded for moving targets.
- Boosted overflow proven: 57 boosted QuadProjectile emissions across four complete intervals with positive pending demand on every observed frame. Quad 108→225; Concussion 200→375; 292 runtime creations, max 1/frame, no skipped frames or budget violations.
- 5,481 shots emitted/reused; zero synchronous expansions or unmatched completions. Quad pending peak 408; max ended-request wait 6.237793 s; final waiting zero. Target died: some requests may have cancelled; not proof every queued request was served.
- Current Boost catalog multipliers 0.5/0.5 preserved. Normal factory/adoption/spawn validation/orders/activation; no pool shrink, cooldown reset or health/damage edits.
- Separate observed issue: duplicate entity IDs 133/134 on squadron respawn through `SquadronPool.Spawn`/`HangarComponent.Launch`, before stress fleets. No subsequent errors after 14:34:20 UTC through final stress.
- Diagnostic callbacks/AI suspension restored; original unchanged `profiler` save reloaded after stress and verified running/unpaused, native profiler disabled. Stress ships discarded by reload.
- Evidence: `Logs/BattleAudit/20261116-cpu-followup/analysis.txt`, `heavy-*-raw.json`, `capture-20261116T143720175Z-analysis.json`.
- Remaining optimization decisions: reduce moving-target rescan cost; choose acceptable admission-latency budget and measured headroom. Correct queue handling does not establish acceptable 6.24 s firing delay.

## Moving-target Scan Fix · 2026-11-16
- `AttackTargetState.TryUpdateHardPointApproach`: removed target position/rotation invalidation. Committed moving leg → scan at most every 100 ms. Arrival (`IsMoving` false) and target replacement (`ResetPursuit`) still rescan at once.
- Why safe: `UpdateHardPointApproach` ignores a non-zero offset while the leg is moving; only mount-range entry is acted on, now within ≤100 ms.
- `WeaponComponent.TryGetHardPointApproach`: target points cached once per scan (`_approachPoints`); squared-distance compare; `sqrt` only on a closer gap. Same result and iteration order.
- Test: `Attack_HardPointTargetMotionBypassesScanInterval` → `Attack_HardPointScanStaysBoundedWhileTargetMoves` (contract changed on purpose). `WeaponCandidateTests.HardPointApproach_UsesPlanarMountRangeAndFocusedTarget` reviewed, unchanged.
- Verification: Unity recompile clean, no Console errors. Tests not run. Post-fix scan cost not measured live.
- Not scan-throttled: stopped ship (holding in range or stalled out of range) still scans every frame; cheaper per scan only.
- Review notes on shipped probe: no stall-rejection memory across detachments (fresh builder per capture group) and no recovery-relay objective in `CanDetachForCapture`; both minor behaviour differences from the old full `Build`.
- Open: admission-latency policy (6.24 s boosted wait) — user decision. Squadron respawn duplicate-ID error (133/134) uninvestigated; suspect pooled-squadron ID vs restored-save ID collision.
