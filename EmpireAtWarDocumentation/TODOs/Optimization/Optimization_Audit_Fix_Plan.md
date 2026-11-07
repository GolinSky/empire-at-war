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

- **Fix next: recurring large fleet stalls.** The capture metadata is corrected and a known shot-pool allocation site is removed. The 193–656 ms Player stalls are not explained or fixed.
- Retain the measured ownership, texture, readability, music, preload, nebula and VFX changes. Incremental GC did not establish an improvement → original setting restored.
- Evidence → [builds, memory, probes and visual comparisons](../../../Docs/Audits/2026-11-07-Optimization-Fixes/README.md). Base `3cdc86cba` + local fixes; Unity 6000.4.7f1 / StandaloneOSX / Apple M2. Diagnostic Players used Mono; project backend remains IL2CPP.

## Implementation

| Priority | Finding | Result | Owner / next verification |
|---|---|---|---|
| P0 | Incorrect output dimensions | Implemented. Packed output 2940×1846 / scale 0.77, then 1470×923 / 0.5; metadata matches camera pixels. Missing camera reports unavailable. | [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. Menu-started Editor capture acceptance remains. |
| P0 | Large fleet stalls | **Open.** Baseline Player maxima 28/193/134 ms; Editor 60/628/70 ms. GC trial still reached 656/244/169 ms, so reverted. Native tracing did not capture the large stall. | [[TODOs/Optimization/Battle_CPU_Performance_Plan\|Battle CPU]]. Capture an actual long interval with synchronized native CPU, render/GPU waits and OS scheduling. |
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

1. [ ] P0 · Reproduce and attribute the large stall in a quiet unprofiled Player, then capture that stall separately with native profiling. Do not infer causality from collection-count coincidence or from a trace in which the stall is absent.
2. [ ] After an attributable fix, repeat ≥3 × 60 s with identical output, scale and GC mode. Require improvement outside variation without worsening p95/p99 or combat behavior.
3. [ ] User visual/listening acceptance: station/fighter readability, Providence at closest zoom on both targets, music transitions/ducking and normal/cinematic nebula switching.

## Files

- General guidance updated → [[Rules/UNITY_OPTIMIZATION_COOKBOOK]]. Project guidance → [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]], [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]].
- Tests not run under the local override. Content and Development Player builds, packed probes and source inspection are separate from test execution.
- Keep this coordination plan active while P0 remains unresolved. Do not mark all issues fixed or close broader owner plans based on these scoped changes.
