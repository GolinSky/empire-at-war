---
category: Optimization
status: in-progress
created: 2026-10-11
updated: 2026-10-11
---
# Battle CPU evidence — 2026-10-11

## Important Values
- All displayed times below are Kyiv (UTC+3); capture filenames use UTC.
- Old battle: `C:/Users/golin/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleCaptures/battle_20261011_114928_331{.csv,_summary.txt}`; 14:49:28–14:49:39, 12 frames / 10.723 s.
- Latest battle: same directory, `battle_20261011_122536_354{.csv,_summary.txt}`; 15:25:36–15:25:46, 78 frames / 10.143 s.
- Both: Editor 6000.4.7f1, 4× speed, High Fidelity, 2560×1440, RTX 5070 / Ryzen 7 9800X3D, revision b514747522461a29857f4cff0ce65175a00f68a2 with dirty source.
- Old render audit: `Logs/RenderAudit/20261011T115023720Z-capture/`; completed profiler/graph/frame; partial rendering panel.
- Latest audit: `Logs/RenderAudit/20261011T122554093Z-capture/`; 15:25:54–15:28:54; profiler/graph completed; camera selector partial; Frame Debugger timeout at event 799, zero saved frames.
- Fresh profiler: `Logs/RenderAudit/20261011T123143709Z-profiler/`; 15:31:43, 12 frames completed without stopping/pausing Play Mode.
- Source changed between observations; marker coverage and battle population differ. Numbers establish priorities, not isolated causal speedups.

| Metric | 14:49 battle | 15:25 battle |
| --- | ---: | ---: |
| Median frame / FPS | 803.165 ms / 1.2 | 123.010 ms / 8.1 |
| Frame p95 / maximum | 1106.522 / 1106.522 ms | 165.786 / 449.412 ms |
| Radar average | 656.729 ms | 8.581 ms |
| Radar samples/frame | 1274.42 | 25.96 |
| Allocated bytes/frame | 44,274,690 | 3,727,008 |
| Target capture average | 18.596 ms | 21.118 ms |
| Due apply average | 44.352 ms | 16.864 ms |
| Navigation average / maximum | 10.293 / 28.894 ms | 4.812 / 81.268 ms |
| Ship tick average | 7.772 ms | 7.069 ms |
| BoltShot update average | 6.945 ms | 8.217 ms |

## Implementation
- Current source verified: centralized `Services/Radar/RadarScanService.cs` with Snapshot/Job/Apply markers replaces the former per-component scan architecture.
- Old radar path: scaled Timer at 0.25 s in inspected assets; FixedTick overlap Unit/Obstacle/trigger scan, ±1000 vertical extent, 64→2048 buffer, per-hit GetComponentInParent entity resolution, enemy-list reconciliation and synchronous listeners.
- Historical profiler radar: 1286/1313 scans, 619.705/629.769 ms inclusive, 589.534/601.173 ms self; overlap queries 26.520/25.721 ms. Rendering agent also verified 66/67 FixedBehaviourUpdate calls.
- These old source concerns are historical handoff evidence; do not apply obsolete fixes without reading the replacement.
- Old battle DueBatch.Apply median 10.867 ms, mean 44.352, maximum 324.023; later audit 305.530 ms scope coincides with 266.846 ms GC and 32.836 ms self.
- Due apply collects/sorts ready events, linearly resolves current event IDs, validates lifetimes, emits shots/applies impacts and reschedules. Child effects and GC can dominate inclusive time.
- Target capture clears per-request position/candidate buffers, validates candidates, reads positions/generations and copies per-hardpoint spans. Cache is not shared across requests.
- Latest target totals: 15,579 requests, 2,663,235 candidate records, 9,864 fallback events / 78 frames; ~200 requests and 34,144 candidate records/frame. Not unique enemy counts.
- Latest target scope averages: capture 21.118, prepare 0.750, job dispatch/completion 0.056, apply 4.030 ms. Zero native target-buffer growth recorded.
- Latest due totals: 40,792 records inspected, 11,821 committed; maximum apply 287.191 ms. No current stack attribution proves this spike is GC.
- Latest radar stages: Snapshot 2.189, Job 0.751, Apply 5.392 ms average; scan total 8.581 ms. Nested stages are not extra costs.
- Latest projectile creation: 462 calls, average Instantiate 1.325 ms; GetOrCreate 3.416 ms includes acquisition work.
- No dedicated audio-performance file/marker found. Ship.Tick includes orders and UpdateAudio; Audio.Thread summaries do not establish gameplay audio API cost.

### Fresh 15:31 profiler
- Median CPU total 96.584 ms; main-thread 93.307 ms; GPU 2.965 ms; 12-frame diagnostic sample.
- Main-thread average inclusive: target capture 16.459; Squadron.Tick 8.469; due apply 12.010 (self 6.315); Ship.Tick 6.469; BoltShot 4.520 ms.
- Fog visibility 2.527 + minimap markers 2.417 + fog system 1.903 ms average self; separate scopes, not one subsystem marker.
- Radar Apply 1.879 and Snapshot 1.722 ms average inclusive; top-40 summaries omit smaller markers.
- No GC.Collect entry in exported top-40 data for this short sample does not establish zero allocation or no future GC stalls.

### Source analysis — 2026-10-11
- Scope: user requested saved-data/live-file analysis and skipped running another battle. No Unity commands, replay, capture, tests, gameplay code or assets changed.
- Reviewed source at `74368d81ead6847d47bdc682f441ce23a45c73c0`; reviewed gameplay/tool paths have no working-tree changes. Captures report older `b5147475` plus dirty source; do not infer controlled gains.

#### Sustained main-thread cost
- Source: `Logs/RenderAudit/20261011T123143709Z-profiler/profiler-summary.json`; 12 frames, `profile_editor=false`.
- All rows below appear in all 12 exported frames. Inclusive/self time differs because instrumented children are nested; uninstrumented callees remain in self time.

| Scope | Inclusive ms/frame | Self ms/frame | Calls/frame |
| --- | ---: | ---: | ---: |
| TargetBatch.Capture | 16.459 | 16.459 | 1 |
| Squadron.Tick | 8.469 | 8.456 | 109 |
| DueBatch.Apply | 12.010 | 6.315 | 1 |
| Ship.Tick | 6.469 | 6.192 | 69.75 |
| Projectile.TurretUpdate | 4.520 | 4.414 | 425.08 |

#### Target preparation and fallbacks
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs:243`: one round-robin hardpoint request per component Tick. `_orderedCandidates` is retained between rebuilds; the whole target list is not rebuilt every frame.
- `WeaponComponent.cs:273,500` + `Assets/Scripts/Components/Weapon/TargetSelectionBatch.cs:150`: each request clears position/candidate scratch data, repeats group cloak/fog and hardpoint eligibility checks, snapshots positions/generations, then copies a separate span.
- 15:25 CSV: 170.95 candidate records/request; capture averages 0.1057 ms/request. Candidate records are not unique targets. Existing position cache removes duplicate reads only within a request.
- Best bounded candidate: cache repeated group eligibility within the snapshot phase and evaluate one batch-local position snapshot across owners. Preserve candidate priority, hardpoint filtering, target generation and validation at commit; measure actual duplicates first.
- `Assets/Scripts/Components/Weapon/WeaponTargetSelectionJob.cs:17`: non-finite inputs, parent quaternion X/Z above `0.000001`, near-degenerate directions, range/yaw boundaries and ±180° yaw request serial fallback.
- `TargetSelectionBatch.cs:202` also counts out-of-span results; `WeaponComponent.cs:363` counts changed target versions/lifetimes. 9,864 fallback events / 15,579 requests = 63.32 events/100 requests; events can stack, so this is not a unique fallback-request rate.
- `Assets/Scripts/Components/ViewComponents/Squadron/FighterView.cs:41` banks the body. Tilt rejection is a plausible contributor only when the mount inherits that rotation; runtime parent rotations/reason counts are unavailable. Keep serial parity safeguards.

#### Due commits and stall separation
- `Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:347,367,392`: sort by due time/event sequence → linear current-ID lookup → remove record → validate → emit shot or apply impact → append rescheduled sequence.
- Lookup cost scales with ready events × live records. A stable ID-to-index map could remove scanning, but must handle swap removals, cancellation, reentrant callbacks and rescheduling; current data does not isolate lookup milliseconds.
- `battle_20261011_122536_354.csv`, completed frame `20777`: frame 449.412 ms, DueBatch.Apply 287.191 ms, navigation 36.530 ms, target capture 20.541 ms, allocations 2,726,969 bytes.
- Completed frame `20800`: frame 233.462 ms, navigation 81.268 ms, due apply 19.531 ms. Navigation issued 35 Plan calls, 52 route builds and 21 floods; flood total 45.008 ms.
- These are separate due/navigation stalls. Allocation bytes do not establish collection time or the cause of the due spike. Navigation exceeds Ship.Tick in these rows → attribute all request callers, not only ship-state ticks.
- Fresh Profiler index `134`: CPU total 285.811 ms, main counter 204.147 ms; exported main-thread `File.Read` 29 calls / 39.414 ms. IO contributes evidence of another stall; caller/ancestry is absent and it does not explain the whole frame.

#### Navigation: retained improvements and remaining work
- `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs:233`: ring candidates already skip obstacle-edge snapping; only candidate zero tries 24 edge positions. `ShipPathGrid.IsKnownUnreachable` skips known blocked/unreachable candidates after flooding.
- `ShipNavigationService.cs:132` still creates/disposes one grid per Plan; `Assets/Scripts/Services/ShipNavigation/ShipPathGrid.cs:39,193` allocates obstacle storage and additional native buffers when flooding. Native buffer costs are not equivalent to managed GC bytes.
- `ShipPathGrid.cs:88,193`: block/flood/route jobs still complete synchronously. Direct clear routes avoid flood; reuse is within one Plan, not across later requests.
- `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs:37` suppresses pursuit replans only while moving. Hunt, Guard and AttackMove callers plus `AttackTargetState.UpdateFormationMove` can retry stationary/out-of-range destinations; requester/state is not recorded.
- `Assets/Scripts/Components/Ship/Movement/ShipMoveComponent.cs:222`: arrival/deferred commands and path-completion clearance checks also issue Plans. Movement holds a failed route, but higher-level pursuit can command it again.
- `Assets/Scripts/Services/ShipNavigation/ShipRoutePlanner.cs:36` builds and validates multiple smooth alternatives; `Assets/Scripts/Components/Ship/Movement/ShipBezierRoute.cs:58,112,139` creates segment, sample and arc-length arrays. These are allocation candidates, not ranked allocating stacks.

#### Squadron work
- `Assets/Scripts/Entities/Squadron/Squadron.cs:133`: UpdateOrder → SquadronPilot.Tick → SquadronFlightComponent.Step. WeaponComponent is independently ticked; do not put its whole batch under Squadron.Tick.
- `Squadron.cs:258` + `Assets/Scripts/Entities/Squadron/SquadronTargetSelector.cs:39,62`: idle/attack-move scans radar contacts; unengaged Guard scans locator entities with hostility/health/cloak/vision checks. Hunt already has a retarget timer.
- `Assets/Scripts/Entities/Squadron/SquadronPilot.cs:215`: engaged aim refresh is already 0.5 scaled seconds; it calls GetShipUnits rather than rebuilding aim targets every Tick. `Assets/Scripts/Components/Health/HealthComponent.cs:183` already uses counted loops; hardpoint results still allocate an array, while hull-only targets return retained storage.
- `Assets/Scripts/Components/Squadron/Flight/SquadronFlightComponent.cs:106,120`: death synchronization → numeric model Step → centroid/root and fighter poses. Split order acquisition, pilot steering, model integration and pose application before changing cadence; dead fighter poses preserve their world placement when the root moves.

#### Ship/audio and effects
- `Assets/Scripts/Entities/Ship/Ship.cs:164,257` includes orders and AudioShipComponent.UpdateAudio under Battle.Ship.Tick. Audio's share remains unmeasured.
- `Assets/Scripts/Components/Ship/Audio/AudioShipComponent.cs:63` renews engine/active-ability loops; `Assets/Scripts/Services/Audio/ShipSfxService.cs:84,194` calls ApplyMix after every matching loop renewal and accepted start, plus its own Tick.
- ApplyMix scans all slots twice and writes active-source volume/pan plus mixer ducking each time. `Assets/Settings/Data/Models/Audio/ShipSfxData.asset`: 20 pooled voices, 2/ship, 4 engine voices, 3 starts/0.06 s. Sources are preconfigured; Request does not instantiate AudioSources.
- Candidate: accumulate mix changes and apply once at a defined frame boundary. Measure first; preserve audible start/stop timing, leases, stealing, pause and voice ducking.
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotEffectPool.cs:69,151`: stack reuse, inactive idle effects and lease-aware completion already exist. `WeaponHardPoint.GetPool` retains up to ShotsPerSalvo; excess/owner-released effects retire. 462 creations could reflect cold demand or churn; available data does not prove leakage or sufficient warm capacity.
- `Assets/Scripts/Components/ViewComponents/Weapon/BoltShot.cs:81` includes tracking/flight/impact and base ShotEffect.Update; the base also ticks muzzle flashes and returns/retires leases. It is not turret aiming. Pool acquisition/creation and completion costs can be nested in broader attack/projectile scopes.

#### Allocation and capture tools
- `Assets/Scripts/Editor/RenderAuditProfilerTool.cs:443` exports top 40 markers/thread with timing/count only; it discards sample ancestry, allocation-size metadata, stacks and source locations.
- Fresh summary explicitly contains 380,239 GC.Alloc calls across eight listed main-thread frame rows. Four frames omit GC.Alloc from the top 40 → total is a lower bound. Calls are not bytes; no GC.Collect row does not establish no collection.
- Full `profiler.data` recordings remain available (415,193,336 bytes fresh; 341,051,356 bytes 15:25). No raw replay was performed; per-caller GC-byte ranking remains unresolved.
- Unity documentation: [CPU allocation bytes/thread](https://docs.unity3d.com/Manual/performance-track-garbage-collection.html), [recording allocation stacks](https://docs.unity3d.com/Manual/profiler-cpu-navigating.html). Stack collection affects future frames only.
- `Assets/Scripts/Editor/RenderAuditFrameDebuggerTool.cs:318,446`: one selected event per Editor update; failed event data retries until deadline; changed event count discards in-memory progress; files are written only at SaveFrame.
- This explains why timeout at event 799 left zero frame artifacts. Exact trigger is not recorded. Candidate repair: bounded event retries and explicit partial checkpoints preserving event/data identity; do not label partial data complete.
- Recounted old frame `59290`: 949/949 events, 1,234 transparent draws; 39×4 indicator draws, Particles/Unlit 661, Ion Glow 219. Historical rendering evidence remains valid for that capture; latest complete Frame Debugger evidence is absent.
- Current short sample remains dominated by CPU work (median CPU/GPU 96.584/2.965 ms). Prioritize target preparation → due commit → squadron children → navigation bursts/audio mix; keep muzzle/bolt/smoke submission and indicators in the rendering plan.

## Rules
- Full profiler recordings preserve more evidence than top-40 JSON summaries; inspect hierarchy/ancestry and allocation metadata before inferring callers.
- GC.Alloc call stacks must be enabled before new frames are recorded; existing summaries do not provide allocation stacks.
- Rendering snapshot at old frame 59289 is adjacent to Frame Debugger frame 59290, not the same timed frame.
- Tool timeouts/partial panel errors are capture reliability issues; never label their incomplete output a successful frame capture.

### Live match capture — 2026-10-29
- Evidence: [Committed audit evidence](../../../Docs/Audits/2026-10-29-Live-Battle-CPU/analysis.md); source `21d64a927629da94d8bc8ca310b48e978a983233`. Match remained playing/unpaused; audit only, no tests.
- Main CSV `battle_20261029_213712_952`: 38 frames, conventional frame median 264.521 ms / p95 411.922, CPU main mean 202.655 ms, GPU counter mean 18.482 ms. Performant/1×/M2/Metal; camera 2560×1440 differs from Screen 642×1156.
- Regular Profiler 525–614: prewarm Awake 2452.0 ms / 91 calls; squadron order self 19.529 ms/frame; target Copy self 10.086 ms/frame. Capture-site Tick max 283.327 ms remains unattributed below the parent scope. EditorLoop/memory-counter/export overhead separated.
- Allocation Profiler 617–640: 4,239,756 main-thread bytes / 80,730 calls; full ancestry groups exported. Telemetry flush ≥1,379,952 B; hazard refresh 464,136 B; AI scoring/queues and shield getters appear. GC stacks restored to None at 21:39:49Z.
- Project Auditor says completed/0 but warns no registered modules → no usable coverage. Rendering Debugger stale CameraSelector → partial; Render Graph complete. Native sample covers profiler export, not gameplay.
- Raw profiler.data files remain in local Logs/RenderAudit; committed compressed summaries, decoder scripts, outputs and SHA-256 manifest preserve portable evidence. No matched Player or historical comparison claimed.

## Files
- [[TODOs/Optimization/Battle_CPU_Performance_Plan]]
- [[Done/Optimization/Battle_Attack_Optimization_Plan]]
- [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research]] — rendering agent's detailed verified asset/event analysis.
