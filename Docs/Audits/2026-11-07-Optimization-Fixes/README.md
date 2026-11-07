# Optimization recheck and fixes

- Base revision: `3cdc86cba`, with the fixes in the working tree. Rechecked November 6–7, 2026.
- Target: Unity 6000.4.7f1, StandaloneOSX, Apple M2 / Metal. Diagnostic Development Players used Mono consistently; the project's IL2CPP setting is preserved.
- Tests were not run. The checkout prohibits test execution. Verification used content builds, Development Player builds, packed loads, native memory readings, live playback, frame captures and source inspection.

## Retained changes

| Area | Change | Verification |
|---|---|---|
| Ownership | Five Separatist wreck/preview entries moved to the existing faction group; 13 duplicated icons promoted into existing SharedIcons. GUIDs, addresses and aliases preserved. | 54 duplicated assets / 90 objects / 195,947,527 redundant object bytes → zero. Intermediate packed deltas include font build artifacts; no isolated ownership saving is claimed. |
| Providence | Albedo and Emissive use BC7; Normal uses DXT5 for Standalone. | Pixel/normal gates pass. Three packed textures use 16,778,880 native object bytes, previously 67,110,444. Eight team colors × two camera angles: foreground RGB RMSE 0.737–0.966 / 255. |
| Music | 119 ordered references / 53 GUIDs migrate to typed AssetReferences. Load one selected track; release on transition, cancellation and disposal. Pack the existing Audio group by Menu/Battle labels. | Model bundle 146,471,297 → 2,654,062 B after the ownership step. Baseline → clean final Model closure: 172,468,675 → 28,636,447 B. Packed playback advances and releases its old handle. Original codec, mixer routing and volume behavior retained. |
| Readable geometry | Disable CPU readability for 30 shipped static satellite mount FBXs. Exclude collider base, shields, hulls, skinned/animated geometry and wrecks. | Five packed satellite prefabs: 248 meshes / 329,046 vertices unchanged. 240 meshes no longer readable; native object memory 32,741,320 → 13,821,774 B. |
| Bolt texture | Disable CPU readability only. | 400,864 → 200,528 native object bytes. Image, dimensions, format, alpha, wrapping and the 1/2/4/8-bolt prefabs unchanged. |
| Hardpoint smoke | Gate the two authored looping emitters using the battle camera and fog. Keep owner leases while suppressed; let stopped systems simulate and drain. | 34 effects: hidden/fog particles → zero; all 68 loops resume on visibility/reveal; stop → active 0, fading 0, idle 34. Repeated isolated simulation: 0.221–0.247 → 0.072–0.080 ms/step. |
| Station burst | Station-only coverage multiplier 0.75; other entity calls retain 1.0. | Fixed-seed captures at near/far zoom and three burst ages. Perceptual acceptance remains a user gate. |
| Reused effects | Cache beam property block and shot-pool event delegates. | Source and successful Player compilation verify persistent ownership. No standalone allocation-total or FPS gain is claimed. |
| Disabled nebula | Defer impostor creation and bake until enabled. Discard stale bake after a disabled variant change. | Packed lifecycle: textures 0 → 1 → 1 on disable/re-enable → 0 after battle exit. First-enable wall time 23.66 ms, previously 43.03 ms in another probe; this cost is deferred, not eliminated. |
| Cold spawn/death | Preload measured CR70 and Providence views and Recalcitrant/Peleset wreck GUIDs before Running, only for participating factions. Other assets retain paced warmup. | Original ship requests: 26.900/43.947 ms; retained completed handles: 0.020/0.001 ms. Actual wreck GUID consumers also have completed cached handles: 0.010/0.001 ms. Friendly aliases use separate cache slots. |
| Capture metadata | Record battle camera pixels and active URP render scale separately. Missing camera → unavailable. | Packed probe: 2940×1846 / 0.77, then 1470×923 / 0.5; recorded dimensions match the camera. |

## Size and visual evidence

- [Content stages](content.csv): baseline → ownership → music/readability → texture pilot → clean final. Production content: 1,096,866,097 → 1,048,201,156 B, a 48,664,941 B / 4.44% reduction. Bundles: 18 → 20; groups remain 19. Catalog: 124,765 → 138,437 B.
- Intermediate builds changed dynamic font data. Restoring original fonts increased the final build by 152,381 B (SharedFonts +36,187; Ui +116,194). The ownership-stage delta of 38,210,464 B includes these incidental font effects; retain it as raw stage evidence, not an isolated optimization result. Clean-final source has no font or ProjectSettings changes. Final common View closure: 81,396,375 B, originally 119,130,974 B.
- [Player object memory](player-memory.csv): reports native object bytes for the checked cohort, not whole-process RSS, total VRAM or installed size. Before/after loads use identical prefab paths and preserved bundle snapshots.
- [Texture gates](providence.csv), [16 rendered comparisons](providence-renders.tsv), [smoke simulation](smoke-cost.tsv), [packed probes](packed-probes.csv).
- Specular passed its numerical compression gate but appears in neither packed layout. Its original importer was restored. Serialized dependency discovery alone had overstated the shipped texture set.

| Providence before | Providence after |
|---|---|
| ![Original](providence-before.png) | ![Compressed](providence-after.png) |

![Station burst comparison](station-comparison.png)

- Station captures isolate particle scale with deterministic seeds and frozen simulation after a 120-population setup. Three nearby strikecraft are included. HUD FPS during captures is not a performance measurement; perceptual readability remains an explicit user gate.
- No Windows build or Windows visual verification was performed.

## Fleet hitches — still open

- [Fleet runs](fleet.csv): seed 565646751, Republic versus Medium Separatist AI, Small Coruscant map, 120 population requested, attack-move toward the enemy base, High Fidelity, time scale 1, VSync off, uncapped. Each timing condition uses three consecutive 60 s windows; populations evolve.
- Original Player: 2940×1846, render scale 1, GC incremental off; maximum intervals 28.029 / 193.185 / 133.904 ms, hitches >50 ms 0 / 3 / 1.
- Original Editor: 2560×1440, scale 0.77, incremental off; maxima 59.674 / 628.167 / 70.057 ms. Editor and Player outputs differ, so their numbers are not a matched rendering comparison.
- The first post-change GC-off run used scale 0.77 and overlapped an OS scan. It is retained as excluded evidence, not an improvement/regression claim.
- Incremental-GC trial at Player scale 1: maxima 656.005 / 243.653 / 169.104 ms. It did not establish an improvement; the original setting was restored.
- A separate native Time Profiler repeat recorded waiting threads. Its three timed windows reached 77.760 / 43.405 / 62.246 ms; it did not capture the large stall. Sampling and context-switch instrumentation affect timing. Do not use this as a faster unprofiled result or a diagnosis of the absent 193–656 ms stall.
- Final retained GC-off Player, scale 1: p95 21.529 / 25.730 / 25.433 ms; p99 25.669 / 28.120 / 29.604 ms; maxima 48.389 / 86.557 / 49.646 ms; >50 ms counts 0 / 1 / 0. Sustained frame times were worse than the baseline. Random visual variant and background load were uncontrolled, so neither a general FPS win nor a causal regression is established.
- Remaining P0: capture a recurring large Player stall with synchronized frame timestamps, CPU stacks, render/GPU waits and OS scheduling. Change only the attributable cause; repeat the quiet unprofiled workload. The cached-delegate fix removes a known allocation site, not proof that the historical collector-lock or Editor GUI stalls are resolved.

## Reproduce and accept

1. Build content for StandaloneOSX with a build layout enabled. Require success, populated groups, unchanged addresses and zero duplicated objects. Compare the closures of MiningFacilityView, a Separatist ship and MusicAudioData.
2. In a packed Development Player, request each preload target at Running before warmup completes. Inspect the actual `(GameObject, key)` slot: ship names for ship views, `UnitWreckData.PrefabKey` GUIDs for wrecks. Repeat warm; check restore/simulation separately.
3. Create 34 hardpoint effects: camera-away → visible → fog-hidden → revealed → owner-stop. Record emission, live particles and active/fading/idle counts. Require full drain and pool return. Measure GPU work separately from the isolated simulation timings.
4. Load the five satellite prefabs and Providence in the Player. Compare mesh counts, vertices, bounds, colliders and native memory; verify shield and wreck consumers remain unchanged.
5. Cold-start with nebula disabled; enable, disable/re-enable and leave battle. Confirm 0/1/1/0 bake textures and inspect normal/cinematic rendering. Record first-enable latency separately.
6. Play menu → battle music, seek near a track end and confirm the next track plays while the prior handle becomes invalid. Listen to transitions and voice ducking.
7. Repeat fleet runs without Editor command polling, screenshots, builds or profiler collection during timing. Keep resolution/scale, seed, actual populations, frame cap, camera, background load and GC mode with every result. A native trace is a separate attribution run.

- Raw build catalogs, layout summaries, diagnostic Players, images and native trace: `~/Library/Caches/EmpireOptimization/20261106/`. Temporary in-Assets diagnostic scripts were removed; retained copies are in that cache. Clean production content rebuilt successfully after removal. The CLI timed out while the build continued; its completion marker and layout confirm success.
- Execution order and remaining work are maintained in Obsidian: `TODOs/Optimization/Optimization_Audit_Fix_Plan.md`. Broader owner plans remain active.
