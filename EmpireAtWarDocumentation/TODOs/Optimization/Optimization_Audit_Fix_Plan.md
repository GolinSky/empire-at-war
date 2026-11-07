---
category: Optimization
status: in-progress
created: 2026-11-06
updated: 2026-11-07
tags:
  - optimization
  - plan
---
# Optimization fixes — verified results and remaining work

## Goal

- Recheck the November 6 findings against live source, serialized assets and packed Mac content.
- Retain fixes backed by builds, measurements or explicit lifecycle checks; preserve broader owner-plan scope.

## Decision

- **Fix next: recurring large fleet stalls.** The capture metadata is corrected and a known shot-pool allocation site is removed. The 296 ms Player stall is attributed to a synchronous AssetBundle wait. Follow-up 576 ms / 1.8 s stalls are attributed to synchronous Metal GPU-program creation. Satellite warmup is fixed; the original asset key, historical 198 ms cause and exact long-stall shader variants remain unresolved.
- Retain the measured ownership, texture, readability, music, preload, nebula and VFX changes. Incremental GC did not establish an improvement → original setting restored.
- Evidence → [builds, memory, probes and visual comparisons](../../../Docs/Audits/2026-11-07-Optimization-Fixes/README.md). Base `3cdc86cba` + local fixes; Unity 6000.4.7f1 / StandaloneOSX / Apple M2. Diagnostic Players used Mono; project backend remains IL2CPP.

## Implementation

| Priority | Finding | Result | Owner / next verification |
|---|---|---|---|
| P0 | Incorrect output dimensions | Implemented. Packed output 2940×1846 / scale 0.77, then 1470×923 / 0.5; metadata matches camera pixels. Missing camera reports unavailable. | [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. Menu-started Editor capture acceptance remains. |
| P0 | Large fleet stalls | **Open.** Baseline Player maxima 28/193/134 ms; Editor 60/628/70 ms. GC trial still reached 656/244/169 ms, so reverted. New native capture: 296 ms with 260 ms blocked in synchronous AssetBundle loading and 282 ms GPU idle. Satellite warmup fix verified separately; not every stall is attributed. | [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. Resolve the original 296 ms asset/caller and historical 198.6 ms event; new 576 ms / 1.8 s captures prove a separate Metal shader-program stall mechanism. Identify exact variants before scoped warmup. |
| P1 | Hardpoint smoke off-camera/fog | Implemented. Both loops explicitly bound; emission suppressed while owner lease remains. Packed hidden particles 11,930 → 0; reveal resumes; stop drains to active 0 / fading 0 / idle 34. Isolated simulation 0.221–0.247 → 0.072–0.080 ms/step. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]]. Full crowded visual/GPU-pass acceptance remains; simulation saving is measured. |
| P1 | Station destruction obscures scene | Station-only scale 0.75. Other deaths retain 1.0. Near/far, fixed-age captures saved. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]]. Perceptual tracking of nearby fighters, hull and HUD remains a user gate. |
| P1 | Providence RGBA32 | Three shipped maps retained compressed: Albedo/Emissive BC7, Normal DXT5; all numerical gates pass. Player object memory 67,110,444 → 16,778,880 B. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size]]. Specular is absent from packed layouts → restored, not counted as a saving. Windows/close-zoom user approval remains. |
| P2 | Nebula baked while disabled | Implemented. Packed texture counts: disabled 0 → enable 1 → re-enable 1 → exit 0. First enable still costs 23.66–43.03 ms wall time in these probes. | [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan\|Nebula and hazards]]. Compare camera/cinematic transitions; optional smoothing of first enable requires a separate measured budget. |
| P2 | Five faction entries in common View | Moved into existing SeparatistUnitViews; 13 proven duplicate icons promoted. 54 duplicated assets → 0. Common View closure 119,130,974 → 83,960,560 B after ownership; 81,396,375 B after the readability changes. Original aliases load in packed Player. | [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan\|Addressables]]. Broader placement/wreck visual gates stay with owner. |
| P2 | Cold ship/death waits | Advance CR70/Providence views and Recalcitrant/Peleset wreck GUIDs only for participating factions. Ship requests 26.900/43.947 → 0.020/0.001 ms; actual wreck GUID requests use completed cached handles at 0.010/0.001 ms. Other assets retain paced warmup. | [[TODOs/Optimization/Loading_Freeze_Plan\|Loading]]. GUID consumer cache verified; startup tradeoff, save restore and simulation acceptance remain. |
| P3 | Beam / shot-pool allocations | Reuse MaterialPropertyBlock and cached event delegates. Compiles in Player. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]], [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. No isolated total-allocation or broad FPS gain claimed. |
| P3 | Music held by Model | Typed per-track references; Audio packs by Menu/Battle labels. All 119 ordered bindings / 53 GUIDs preserved. Baseline → clean final Model closure 172,468,675 → 28,636,447 B. Track handoff releases prior handle. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size]]. Listening/crossfade/ducking acceptance; codec unchanged. |
| P3 | Readable meshes / bolt | 30 safe satellite FBXs; 240 of 248 loaded meshes become non-readable with 329,046 vertices unchanged. Native mesh memory 32,741,320 → 13,821,774 B; bolt 400,864 → 200,528 B. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size]], [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|FPS recovery]]. Wreck/collider/shield sources excluded. |

## Important Values

- Final retained GC-off fleet: maxima 48.389/86.557/49.646 ms; >50 ms counts 0/1/0 per 60 s. p95 21.529/25.730/25.433 ms is worse than baseline; visual variant/background load uncontrolled → no general FPS improvement claim.
- Clean final packed content: 1,096,866,097 → 1,048,201,156 B (−48,664,941 B, −4.44%). Providence texture step −8,173,097 B. Intermediate stage totals include temporary font build artifacts → no isolated ownership-byte claim; original fonts restored before final build.
- Bundles 18 → 20; groups remain 19. Catalog 124,765 → 138,437 B. Final recorded layout: zero duplicated assets/objects.
- Three consecutive 60 s windows per timing condition. Workload: seed 565646751, Republic/Medium Separatist, Small Coruscant, 120 population requested, attack-move, time scale 1, uncapped, VSync off.
- Editor 2560×1440 / 0.77 and Player 2940×1846 / 1 are not interchangeable rendering baselines. The first post-change run at 0.77 with an active OS scan is excluded from performance conclusions.

## TODO

1. [ ] P0 · Native 296 ms stall mechanism captured; exact asset/managed caller remains unresolved. Independently attribute the 198.6 ms interval outside measured cached loads, then repeat in a quiet Player. Do not generalize the satellite fix to every stall.
2. [ ] After an attributable fix, repeat ≥3 × 60 s with identical output, scale and GC mode. Require improvement outside variation without worsening p95/p99 or combat behavior.
3. [ ] User visual/listening acceptance: station/fighter readability, Providence at closest zoom on both targets, music transitions/ducking and normal/cinematic nebula switching.

## Files

- General guidance updated → [[Rules/UNITY_OPTIMIZATION_COOKBOOK]]. Project guidance → [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]], [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]].
- Tests not run under the local override. Content and Development Player builds, packed probes and source inspection are separate from test execution.
- Keep this coordination plan active while P0 remains unresolved. Do not mark all issues fixed or close broader owner plans based on these scoped changes.


### P0 investigation · 2026-11-07

- Unprofiled retained Mono Player: harness maxima 125.901/105.088/47.853 ms; rolling history maximum 145.299 ms. Combined native recording captured 296.146 ms at `2026-11-07T09:01:09.441741Z`, Unity frame 11598.
- Native mechanism: 260.103 ms main-thread blocked; 188 raw stack occurrences in `AssetBundleLoadAssetOperation::GetLoadedAsset`, 183 in `PreloadManager::WaitForAllAsyncOperationsToComplete`; Player GPU idle gap 281.889 ms. Exact managed request/key not resolved.
- Raw trace recovery: retained 15 s slice plus completed CPU/Metal and 1 s scheduling imports. Abandoned 27.55 GB full container removed. [Measurements, limits and native evidence](../../../Docs/Audits/2026-11-07-P0-Fleet-Stalls/README.md).
- Verified separate omission: AI site construction loaded `TurbolaserSatelliteView` synchronously in 43.034/50.416 ms; direct AssetBundle getter 47.202 ms. All five satellite types were absent from warmup.
- Fixed `BattleAssetPreload`: enumerate `DefendPlatformType` through the factory's `DefendPlatformAssets` mapping. Satellite data → startup; views/wrecks → paced warmup, or upfront for restore/simulation.
- Verification: changed source compiles against retained Player assemblies outside Unity. Isolated packed copy with equivalent five-key expansion: actual AI Turbolaser factory load 0.0022 ms. No fresh Unity build claimed. Core regression coverage extended; tests not run.
- Separate probe reached 198.608 ms without a ≥5 ms `CachedAssetService.Load` call. Its cause remains unknown. Do not claim the satellite fix resolves that frame or the original native event.
- User requires concurrent work to continue → no controlled broad FPS conclusion. Shared Editor not used for verification; unrelated PlayerSettings changes left untouched.
- Reusable guidance → [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]]. This coordination plan stays in progress.

### Mac compilation correction · 2026-11-07

- Reported `DLSSIUpscaler.cs` CS1069: the DLSS prototype commit `7df3fb0c1` forced `ENABLE_NVIDIA` in shared Standalone scripting defines. Mac compilation included DLSS code without a NVIDIA module reference; the built-in NVIDIA package was already enabled.
- Removed only the manually forced `ENABLE_NVIDIA` through Unity PlayerSettings; retained `ENABLE_UPSCALER_FRAMEWORK` and the NVIDIA package. Do not force platform capability symbols globally.
- Unity 6000.4.7f1 Editor recompile completed with `failed: false`, `errors: []`. Settings persisted; tests not run. Windows DLSS compilation/runtime not verified by this Mac correction. P0 fleet attribution remains open.

- Consolidated findings, causal evidence, fixes, verification limits and current probes → [[TODOs/Optimization/Optimization_Audit_Fix_Plan - Research]].

### Follow-up native attribution · 2026-11-07

- Captured 576.310 ms and 1797.483 ms harness gaps. Main thread waits in `GfxDeviceClient::CreateGpuProgram`; graphics worker waits in `MTLCompilerScheduler::buildRequest`. These new events establish runtime Metal shader/program creation as a second stall mechanism.
- Historical 198.608 ms event localized to rolling frame 12691 / UTC `09:35:26.831144`; its native stack was not recorded. Similar timing/counters do not prove the same cause.
- Original 296 ms asset key/caller remains unavailable in retained symbol data. No new production patch yet; broad shader warmup previously caused keyword-space errors. Tests remain disabled; overall FPS improvement unestablished.
- Full previous findings, fixes, exact measurements, diagnostic scripts and remaining attribution work → [[TODOs/Optimization/Optimization_Audit_Fix_Plan - Research]].

- Shader-log repeat completed: 149 variant-upload messages, but native messages bypass managed frame callbacks; exact long-stall variants still unproven. Also captured a separate 150.927 ms capture-writer wait and up to 83.530 ms probe stack-formatting cost. Full classifications → research decision matrix. No new production patch; tests not run.
