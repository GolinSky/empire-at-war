---
category: Optimization
status: in-progress
created: 2026-11-07
updated: 2026-11-07
---
# Optimization audit — evidence, fixes and P0 stall research

## Goal
- Preserve all findings and fixes from this investigation, with measured effects separated from unverified claims.
- Owner: [[TODOs/Optimization/Optimization_Audit_Fix_Plan]]. P0 remains open; automated tests remain disabled.

## Decision
- Exact historical event attribution requires evidence from that event. A later reproduction can establish a mechanism, but cannot retroactively prove the original asset or caller.
- Other Unity/build work must continue per user instruction. Use isolated retained diagnostic Players; do not claim controlled overall FPS improvement.
- Commit `bbbb9a102`: satellite preload coverage, authored Core regression coverage, native/managed evidence, Mac NVIDIA compile correction.

## Implementation

### Previous retained fixes
| Priority | Finding | Result | Owner / next verification |
|---|---|---|---|
| P0 | Incorrect output dimensions | Implemented. Packed output 2940×1846 / scale 0.77, then 1470×923 / 0.5; metadata matches camera pixels. Missing camera reports unavailable. | [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. Menu-started Editor capture acceptance remains. |
| P0 | Large fleet stalls | **Open.** Baseline Player maxima 28/193/134 ms; Editor 60/628/70 ms. GC trial still reached 656/244/169 ms, so reverted. New native capture: 296 ms with 260 ms blocked in synchronous AssetBundle loading and 282 ms GPU idle. Satellite warmup fix verified separately; not every stall is attributed. | [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. Resolve the exact asset/caller for the native 296 ms event and the separate 198.6 ms interval outside measured cached loads; repeat under controlled conditions. |
| P1 | Hardpoint smoke off-camera/fog | Implemented. Both loops explicitly bound; emission suppressed while owner lease remains. Packed hidden particles 11,930 → 0; reveal resumes; stop drains to active 0 / fading 0 / idle 34. Isolated simulation 0.221–0.247 → 0.072–0.080 ms/step. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]]. Full crowded visual/GPU-pass acceptance remains; simulation saving is measured. |
| P1 | Station destruction obscures scene | Station-only scale 0.75. Other deaths retain 1.0. Near/far, fixed-age captures saved. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]]. Perceptual tracking of nearby fighters, hull and HUD remains a user gate. |
| P1 | Providence RGBA32 | Three shipped maps retained compressed: Albedo/Emissive BC7, Normal DXT5; all numerical gates pass. Player object memory 67,110,444 → 16,778,880 B. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size]]. Specular is absent from packed layouts → restored, not counted as a saving. Windows/close-zoom user approval remains. |
| P2 | Nebula baked while disabled | Implemented. Packed texture counts: disabled 0 → enable 1 → re-enable 1 → exit 0. First enable still costs 23.66–43.03 ms wall time in these probes. | [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan\|Nebula and hazards]]. Compare camera/cinematic transitions; optional smoothing of first enable requires a separate measured budget. |
| P2 | Five faction entries in common View | Moved into existing SeparatistUnitViews; 13 proven duplicate icons promoted. 54 duplicated assets → 0. Common View closure 119,130,974 → 83,960,560 B after ownership; 81,396,375 B after the readability changes. Original aliases load in packed Player. | [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan\|Addressables]]. Broader placement/wreck visual gates stay with owner. |
| P2 | Cold ship/death waits | Advance CR70/Providence views and Recalcitrant/Peleset wreck GUIDs only for participating factions. Ship requests 26.900/43.947 → 0.020/0.001 ms; actual wreck GUID requests use completed cached handles at 0.010/0.001 ms. Other assets retain paced warmup. | [[TODOs/Optimization/Loading_Freeze_Plan\|Loading]]. GUID consumer cache verified; startup tradeoff, save restore and simulation acceptance remain. |
| P3 | Beam / shot-pool allocations | Reuse MaterialPropertyBlock and cached event delegates. Compiles in Player. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]], [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. No isolated total-allocation or broad FPS gain claimed. |
| P3 | Music held by Model | Typed per-track references; Audio packs by Menu/Battle labels. All 119 ordered bindings / 53 GUIDs preserved. Baseline → clean final Model closure 172,468,675 → 28,636,447 B. Track handoff releases prior handle. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size]]. Listening/crossfade/ducking acceptance; codec unchanged. |
| P3 | Readable meshes / bolt | 30 safe satellite FBXs; 240 of 248 loaded meshes become non-readable with 329,046 vertices unchanged. Native mesh memory 32,741,320 → 13,821,774 B; bolt 400,864 → 200,528 B. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size]], [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]]. Wreck/collider/shield sources excluded. |


### 296 ms native stall
- Run `4ca26c184e134a568e3724b64a47a021`, Player PID 24672, main thread 236766. Unity frame 11598 reports 296.146124 ms at UTC `2026-11-07T09:01:09.441741Z` (11:01:09 local).
- Actual observer gap: realtime 123.965625 → 124.264309 s, 298.684 ms. Unity delta appears one row later; align native evidence to the observer gap.
- Main thread: Blocked 260.103282 ms; Running 35.058836 ms; Runnable 1.240464 ms; Preempted 2.281418 ms.
- 188 raw stack occurrences: `AssetBundleLoadAssetOperation::GetLoadedAsset`; 183: `PreloadManager::WaitForAllAsyncOperationsToComplete` plus timed semaphore wait, under `AssetBundleRequest_CUSTOM_GetResult`.
- Player GPU idle 281.888792 ms. Evidence supports synchronous AssetBundle waiting starving rendering, rather than a long GPU pass or primarily runnable starvation.
- Stack occurrence counts are not independent duration samples. Frame counters have different phases and are not a causal breakdown of the observer gap.
- Native symbolication uses retained `UnityPlayer.dylib` with inferred base `0x10a400000`, independently corroborated by PlayerMain/semaphore frames. Original image/JIT mapping unavailable → exact managed caller and asset key unproven.
- Full recording processing paged heavily. Recovered 15 s raw slice, 1 s scheduling slice and completed CPU/Metal/System Trace imports; removed abandoned 27.55 GB full container after recovery.

### Satellite omission and fix
- Independent probes: AI `TurbolaserSatelliteView` load 43.0343 / 50.4156 ms; second run directly measured `AssetBundleRequest.asset` at 47.2024 ms.
- Caller: `EnemySiteConstructionController.Tick → CaptureSitesSystem.TryStartConstruction → SiteFacilityBuilder.Build → DefendPlatformFactory.Create → CachedAssetService.LoadComponent/LoadPrefab/Load`.
- Cause: old prefix list omitted QuadLaser, Turbolaser, Ion, MassDriver and Missile satellite types.
- Fix: `BattleAssetPreload` enumerates `DefendPlatformType` via `DefendPlatformAssets.GetDataName/GetViewName`; data loads before Running, views/wrecks during paced warmup or upfront restore/simulation preload.
- Existing Core fixture extended to request every platform data/view/wreck after preload/warmup and assert no handle growth. Authored only; tests not run.
- Changed source compiled outside Unity against retained Player assemblies. Equivalent five-prefix addition in an isolated retained binary verified actual AI factory request at 0.0022 ms.
- Final diagnostic windows: maxima 34.015 / 29.608 / 44.940 ms. This confirms the scoped cache fix, not elimination of all rare stalls. The binary probe is not a fresh production-source Player build.

### 198 ms interval
- `load-probe` window 1 maximum 198.607791 ms; no ≥5 ms `CachedAssetService.Load` measurement coincided. No GC-count increment reported for that maximum.
- That probe timed cached loads only; it did not establish the cost of asynchronous provider callbacks, rendering, native waits, OS scheduling, or its own stack formatting/file writes.
- Cause remains unproven. Do not equate absence of a slow cached load with absence of all asset-related work.

### NVIDIA compilation correction
- `7df3fb0c1` forced `ENABLE_NVIDIA` in shared Standalone scripting defines. Mac SRP compilation included DLSS code without a NVIDIA module reference → CS1069 for `DLSSContext` and related types.
- NVIDIA built-in package was already enabled. Package resolution alone did not establish a source fix.
- Removed only manually forced `ENABLE_NVIDIA` via PlayerSettings; kept `ENABLE_UPSCALER_FRAMEWORK` and NVIDIA package. Unity 6000.4.7f1 recompile completed with `failed:false, errors:[]`.
- Windows DLSS compilation/runtime not verified; do not force platform-capability symbols globally.

### Current investigation
- Added diagnostic-only timing in a copied Player: AssetBundle getter/result name, cached key, Addressables provider waits/update, lifecycle Tick/LateTick, music Tick, VContainer runner.
- Slow-call records include Stopwatch boundaries and managed stacks. Frame gaps include realtime/UTC and GC-count delta. Records buffered until quit; probe stack-formatting cost measured separately.
- Native Time Profiler attached by verified PID. Launch-mode attempt selected original app via macOS launch resolution → stopped and excluded; direct launch selected the instrumented copy.
- No production code changes from this continuation yet. Concurrent enemy-planner/font/Graphify edits remain outside scope.

## Important Values
- Retained Mono Development Player: embedded source `3cdc86cba` plus earlier retained fixes; not a fresh current-checkout build. Original build provenance: [prior audit](../../../Docs/Audits/2026-11-07-Optimization-Fixes/README.md).
- Apple M2, Unity 6000.4.7f1, High Fidelity, 2940×1846, render scale 1, incremental GC off, VSync off, uncapped, time scale 1.
- Workload: seed 565646751, Republic / Medium Separatist, Small Coruscant, 120 population requested, attack-move; evolving combat and visuals vary between runs.
- Unprofiled harness maxima 125.901 / 105.088 / 47.853 ms; rolling maximum 145.299 ms. Traced harness maxima 168.607 / 297.662 / 83.474 ms.
- Diagnostic Players can SIGSEGV after all windows save during shutdown. This is separate from measured battle stalls; do not call a run clean end-to-end.

## Files
- [P0 audit and raw-evidence pointers](../../../Docs/Audits/2026-11-07-P0-Fleet-Stalls/README.md).
- Cache: `/Users/golinsky/Library/Caches/EmpireOptimization/20261107-P0/`: baseline, traced, load-probe, asset-probe, fixed-probe, deep-probe.
- Native evidence: `traced/stall.atrc`, `stall-one-second.atrc`, completed `stall-cpu.trace`, `stall-metal.trace`, `stall-system.trace`; XML/CSV exports and stack symbolication alongside.
- Reusable rules: [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].
- Prior CPU evidence: [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]].

## TODO
- [ ] Resolve exact historical caller/key only if retained evidence permits; otherwise label that limitation and separately identify reproduced events.
- [ ] Capture >100 ms interval with synchronized managed/native evidence and attribute missing time.
- [ ] Apply only evidence-backed changes; repeat with matching capture settings.
- [ ] Overall FPS improvement remains unestablished. Tests remain disabled.

### Follow-up captures · 2026-11-07

- Historical 198.608 ms harness interval localized: run `2ed5b28d56b841cd9c7046e6d0791db4`, rolling frame 12691 = 199.309006 ms at UTC `09:35:26.831144`; observation gap realtime 114.101147 → 114.299517 s. Capture writer wait 0 ms; recorded main/render/GPU counters 4.170/0.889/15.770 ms have different phases and cannot explain the missing time.
- No slow cached-load record anywhere in that middle window: prior records end at realtime 9.001 s; next is satellite load at 167.840 s. Logging those measured loads does not explain this interval. Native stack absent → historical cause remains unproven.
- First deeper run, three 60 s windows: maxima 83.524 / 66.671 / 53.211 ms. Satellite getter 38.681 ms; cached load 43.451 ms. Diagnostic uses original omitted-satellite binary to investigate the old workload.
- First run frame 10972: lifecycle `LateTick` 48.241 ms. Native overlap includes 18 Mono JIT-compilation samples among 33 main-thread samples in the examined 49 ms interval. Do not equate Mono diagnostic first-use compilation with IL2CPP shipping behavior.
- First run 83.456 ms probe gap: 79 ms main-thread sample gap immediately after Instruments attachment; samples resume at `ProfilerManager::StartNewFrame`. Waiting/scheduling evidence needed; capture disturbance is possible, not proven.
- Second run expands timing to individual project Tick/LateTick/Flush and records native waiting/kernel/context-switch stacks. Harness maxima 62.189 / 576.310 / 1797.483 ms; longest intervals have no GC-count change or matching ≥5 ms wrapped managed method.
- Second run 576.917 ms probe gap at frame 11667, UTC `10:45:26.924229`; 1797.563 ms at frame 12974, UTC `10:45:47.980456`. Rolling delta rows are 11666/12973. These are newly captured events; their causes are not automatically the same as the historical 198/296 ms events.
- Individual tick finding: `CombatAttackCoordinator.LateTick` 45.077 ms at frame 9032. `EnemyProductionStrategy.Tick` 60.688 ms at frame 12756 coincides with a GC-count increment. Probe stack-formatting itself sometimes costs 1–16 ms; those overhead rows are retained.
- [Follow-up records and instrumentation](../../../Docs/Audits/2026-11-07-P0-Fleet-Stalls/Followup/README.md). Native trace completed; no additional production fix implemented.

### Native shader-stall attribution

- Completed second native trace attributes both new 576 ms / 1.8 s gaps to synchronous runtime GPU-program creation: main thread `GfxDeviceClient::CreateGpuProgram → ShaderLab::SubProgram::Compile → shader variant selection → draw submission`; graphics worker `MTLCompilerScheduler::buildRequest → MTLLibraryBuilder`.
- Main-thread wait-sample weights: 508.99 + 40.54 ms in first gap; ~1.49 s + 294.80 ms in second. Native samples directly support this mechanism; they do not retroactively prove the historical 198 ms cause.
- Shader-compilation log repeat completed; exact historical long-stall variants remain unidentified. No broad warmup patch: previous `Shader.WarmupAllShaders()` produced keyword-space errors. Owner → [[Done/Bugs/Shader_Warmup_Keyword_Errors]].
- Discovery: original trace-launch orphan PID 24664 still existed; command line pointed to this investigation's `traced/player.log`. Terminated only that owned process. Concurrent workload is another duration/FPS confound.
- Evidence → [follow-up analysis and native samples](../../../Docs/Audits/2026-11-07-P0-Fleet-Stalls/Followup/README.md). Second native trace completed successfully.

### Shader-log repeat and diagnostic overhead
- Third run, no native profiler: maxima 43.240 / 217.039 / 846.885 ms. Frame 13881: 217.525 ms at UTC `10:55:56.667784`, no matching slow wrapped call; cause not established.
- 149 native shader-upload messages include shader/pass/stage/keywords/time. Maximum reported upload: 50 ms, `Universal Render Pipeline/Particles/Unlit`, ForwardLit fragment, `_FADING_ON _SOFTPARTICLES_ON _SURFACE_TYPE_TRANSPARENT`.
- Native upload messages bypassed `Application.logMessageReceived` (0 correlated shader callback records). Therefore exact long-stall variant association is unavailable; timestamp native log output externally on the next capture.
- Separate frame 18743: probe gap 188.188 ms; `BattleHitchCapture.Flush` 152.142 ms inside `PerformanceAnalyticsSampler.Tick`; detailed CSV independently records writer wait 150.927 ms and delta 191.559 ms.
- This is measured capture-writer backpressure. Historical 198 ms row recorded writer wait 0 ms → do not conflate those events.
- Frame 18708: diagnostic stack formatting alone cost 83.530 ms during an 846 ms observer gap. No clean gameplay-only attribution for that burst.
- No new production code fix in this continuation. Tests not run. Owned capture processes finished; original owned orphan stopped. User's other work left untouched.

## Decision matrix

| Event | Established cause | Fix / remaining work |
|---|---|---|
| Original 296 ms | Synchronous AssetBundle/preload-manager wait | Exact historical key/caller not recoverable from retained native symbols alone; capture future requests with managed keys. |
| Satellite 43–50 ms | Five platform types omitted from warmup; AI factory load | Fixed in `bbbb9a102`; equivalent packed cache request 0.0022 ms. |
| Historical 198 ms | Localized frame/UTC; no slow cached load or writer wait | Cause remains unproven; shader creation is a hypothesis informed by later captures, not retrospective proof. |
| New 576 ms / 1.8 s | Native GPU-program creation and Metal compiler waits | Identify exact variants/render state, then evaluate scoped warmup; no broad warmup. |
| New ~48 ms LateTick | Native samples include Mono JIT work | Diagnostic backend caveat; IL2CPP behavior unverified. |
| New 188 ms diagnostic gap | Capture writer waits 150.927 ms | Capture infrastructure issue; use bounded nonblocking capture design with explicit loss reporting before changing it. |
| NVIDIA CS1069 | Forced platform capability symbol on Mac | Removed manual `ENABLE_NVIDIA`; Unity recompile 0 errors. |

- Final evidence → [follow-up README](../../../Docs/Audits/2026-11-07-P0-Fleet-Stalls/Followup/README.md). P0 remains open; no overall FPS claim.
