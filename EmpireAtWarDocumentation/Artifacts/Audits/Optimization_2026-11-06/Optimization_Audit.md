---
type: audit-record
system:
  - optimization
  - addressables
  - battle-rendering
  - vfx
verified: 2026-11-06
verification: Read Editor and disk state at ebf260217. Resolved Addressables
  entries and sampled imports, prefab renderers, live particles, nebula texture
  and camera. Inspected two camera images and dated build and trace evidence.
record_state: historical
tags:
  - optimization
---
# Optimization audit · 2026-11-06

## Goal

- Compare the two cookbooks to checkout `ebf260217`, Unity 6000.4.7f1, effective StandaloneOSX assets and the open M2/Metal battle. A saved build or timing is identified by its own revision and workload.
- Evidence below distinguishes observed work or pixels, a code path that can incur cost, and a target that still needs measurement. Project rules and plan inventory → [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK]].

- Fix order, owning plans and acceptance criteria → [[TODOs/Optimization/Optimization_Audit_Fix_Plan]].

## Findings

### 1. Hardpoint smoke keeps simulating outside the battle camera

- **Observed.** In one live census, 34 playing `HardPointExplosionPool/ExplosionVfx/ppfxPipelineFire` emitters held 11,648 particles. All 34 renderers were enabled. None of their bounds intersected the main camera frustum.
- The resolved `Assets/Prefabs/Vfx/Explosions/ExplosionVfx.prefab` emitter loops at 100/s, lifetime 1–6 s, cap 1,000, `AlwaysSimulate`. `HardPointExplosionPool.ActiveExplosion.Follow` follows the anchor and sets `forceRenderingOff` under fog. Simulation can continue while drawing is suppressed.
- A later census at 17:58:54 UTC, frame 84489, found 61 instances, 58 emitting, 19,474 particles, 15 bounds intersecting the camera and nine renderers forced hidden. The battle was paused at this snapshot. Bounds and particles describe population. No frame-time delta was measured.
- **Next check.** In a matched destruction sequence, measure this emitter’s simulation separately from transparent GPU cost. Compare current vs reduced off-camera emission vs restored. Check re-entry, fog reveal, owner death and pool return. `ExplosionVfx.IsPlaying` uses `IsAlive(true)`. Suppressing simulation blindly can strand a fading lease. [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

### 2. Four Providence hull maps resolve uncompressed

- **Observed.** `Assets/Art/Textures/Models/SeparatistShips/Providence/Providence_Hull_{Albedo,Emissive,Normal,Specular}.png` all resolve as dependencies of `ProvidenceShipView`. Each imports at 2048² RGBA32. The default platform format explicitly selects RGBA32 even though the importer says `Compressed`. The Standalone override is off.
- Unity’s Editor memory query returned 44,740,216 B for each of Albedo, Emissive and Normal and 22,370,596 B for Specular. These are Editor object readings. Shipped bytes and Player residency require their own measurements.
- **Next check.** Pilot a format per map with the original as reference. Inspect the normal in oblique light, alpha/emissive edges at closest zoom and all team masks used with this hull. Several sharp-alpha maps were deliberately restored during the previous pass. Compare a matching content build before assigning savings. [[TODOs/Optimization/Asset_Size_Followup_Plan]] · [[TODOs/Optimization/Desktop_Asset_Compression_Plan]].

### 3. The shared View group contains faction-only art routes

- **Observed.** The four main ship views are in `SeparatistUnitViews`. Five associated entries are in `Assets/AddressableAssetsData/AssetGroups/View.asset`. Unity resolved the following transitive texture dependencies under `Assets/Art/Textures/Models/SeparatistShips/`.

| View entry | Separatist texture paths |
|---|---:|
| RecalcitrantWreckView | 11 |
| PelesetWreckView | 5 |
| BulwarkWreckView | 6 |
| BulwarkReinforcementView | 6 |
| WavecrestWreckView | 3 |

- Counts are per entry and can overlap. All 612 explicit entries resolved, with 612 distinct addresses and GUIDs. All four faction group schemas serialize PackTogether and CRC off.
- **Next check.** Build a matched layout for this checkout, compare a common facility’s bundle closure with a faction ship’s closure, then trial moving the five entries with their identifiers intact. Packed duplication, bytes saved and runtime residency remain open. [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan]].

### 4. The M2 fleet has long frames, and detailed capture metadata reports the wrong size

- **Measured, saved run.** November 6 High Fidelity Editor, Coruscant seed 565646751, time scale 1, revision `d15d88277` plus local changes. The 60 s fleet window averaged 126.98 FPS. P95 10.68 ms, p99 16.42 ms, six intervals >50 ms and a 721.25 ms maximum. Population changed from 24 ships/27 squadrons to 17/26.
- Frame 41354 recorded 5.51 ms main-thread time, 703,962 allocated bytes and no GPU timing. Its original row cannot attribute the long interval. A separate M2 reproduction captured a 2.040 s stall. The Unity Pipeline worker triggered managed GC while encoding a command list and the main thread waited in the shot-completion path. With native profiling off, two more stalls coincided with collections and a 645 ms stall did not. GC is the leading explanation for frame 41354. The original allocating call remains unknown. Detailed evidence → `Docs/Audits/2026-11-06-Battle-Hitch/analysis.md`. Exclude the 15 s Profiler exports from gameplay baselines.
- **Reproduced metadata defect.** `Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs` builds its resolution field from `Screen.width/height`. The Editor-command context reported 1406×894. Main camera and captured PNG were 2560×1440. The later live inspection reported High Fidelity at 77% render scale. Record both output and internal scale before comparing GPU budgets.
- **Next check.** Correct the metadata source for detailed reports. Compare an unprofiled run with the same fleet/camera in a Development Player and correlate wall-clock hitch rows with native thread/GC/GPU timelines. Keep Editor export stalls separate. Evidence → `Logs/BattleAudit/20261106T172531Z-live-hitch/{analysis.md,verification.json,resolution-check.json}` and `Logs/BattleAudit/20261106T174405Z-721ms-trace/`. [[TODOs/Optimization/Battle_CPU_Performance_Plan]] · [[TODOs/Optimization/Late_Battle_CPU_Fix_Plan]].

### 5. Disabling the nebula still incurs its startup bake and allocation

- **Source-confirmed.** `MatchNebula.Start` always constructs `NebulaImpostor` and bakes 128 samples. `NebulaEnabled` gates visibility in the render callback. The constructor creates a 4096×2048 ARGBHalf texture with depth. It is released on destruction.
- In the running battle `Match Nebula Bake` was created and Unity reported 109,053,088 B for that RenderTexture. The color surface alone is 64 MiB by dimensions. The live reading was taken with a visible nebula. Off-at-start cost follows from the code path.
- **Next check.** Start cold with the saved option off. Measure allocation and startup timing, then enable once, switch quality and enter/leave cinematic before considering lazy creation. Preserve the first-enable appearance and measure any deferred hitch. [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan]].

### 6. Other costs to isolate

| Area | Current evidence | Next check |
|---|---|---|
| Cold spawn | `BattleAssetWarmup` starts deferred views and wrecks at Running, four at a time. `ShipSpawnClearance` loads the view collider. `CachedAssetService.Load` waits synchronously if its typed handle is unfinished. | In a packed Player, time the first spawn of a deferred type and repeat it warm. Record the key, handle and stall. [[TODOs/Optimization/Loading_Freeze_Plan]]. |
| Heavy beam allocation | `BeamVisuals.Begin` allocates a new `MaterialPropertyBlock` on each activation of a pooled beam. `Tick` sets all three beam properties and reset removes the block from the renderer. | Count allocations under sustained beam fire. Try one block per instance and check reused color, width and fade. [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]. |
| Music lifetime | The global Model entry `MusicAudioData.asset` contains 119 direct clip references to 53 distinct clips. The older Mac Player’s Model bundle is 147,763,696 B on disk. | Compare playlist loading, catalog closure, first play and crossfades. A split alone does not remove audio bytes or establish resident PCM. [[TODOs/Optimization/Asset_Size_Followup_Plan]]. |
| Readable geometry | 522 of 576 ModelImporter records under `Assets/Art/Models` have Read/Write on. This includes source-only art. | Intersect with shipped dependencies, CPU readers, shield/wreck geometry and collider cooking before changing any flag. Measure Player memory. [[TODOs/Optimization/Asset_Size_Followup_Plan]]. |
| Bolt texture | `BlasterBolt.png` resolves 1000×50 RGBA32, Clamp, one mip and readable. Current single and dual materials both bind it. The former tiled texture was removed in `db62b55d8`. | Audit CPU readers and render the thin 1/2/4/8 patterns at near/far zoom before changing format or readability. Measure one remaining texture. [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]. |

## Visual inspection

- Same battle station, two moments, High Fidelity, 2560×1440 camera captures. In the destruction view, the bright blast covers parts of the station arms. Small craft and their trails are harder to distinguish against the bright center. This is a combat-readability concern at peak overlap. Duration and player impact need a sequence.
- Pixels with 8-bit red ≥245 and green ≥230 occupy 0.083% of the quiet image and 5.878% of the destruction image. This counts bright pixels across the output. It does not isolate bloom, smoke or fragment cost.
- Capture the same destruction sequence with original/restored settings and compare hull outline, small-craft tracking, damage cues, shield/armor contrast, fade and near/far zoom. Use composited Game view for HUD and input acceptance.

**Station before the burst**
![[Artifacts/Audits/Optimization_2026-11-06/Station_camera.png|720]]

**Station during destruction**
![[Artifacts/Audits/Optimization_2026-11-06/Station_destruction.png|720]]

## Checks that passed

- All effective particle renderers in `Assets/Prefabs/Vfx` were checked, including nested and inactive children. Across 22 prefabs with particles and 70 renderer occurrences, zero cast shadows and zero assigned material slots were missing or used an unsupported shader on this target. This check excludes other prefab roots.
- Single/dual/quad/octuple bolt prefabs resolve to Stretch renderers with caps 1/2/4/8. `LaserProjectile` now uses five heavy-beam systems capped at 48 each. `ProtonBeamView` retains the 50/s nested smoke override. Reapplying the old two-beam recipe to both would target different effects.
- `DecorAsteroidRenderer` still culls cells and transformed instance bounds before instanced draws. These code checks preserve the culling path. Edge-LOD appearance and per-pass budgets need matched captures.

## Evidence limits

- The local October 29 Mac Player identifies revision `2aa218c52`. Its 18 bundle files total 1,080,715,031 B. Catalog 121,342 B. Both local `Library/com.unity.addressables/BuildReports/buildlayout_2026.10.29.*.json` files contain `SBP ErrorException` and empty groups. The packaged output exists. Neither failed report can establish current duplication. The October 24 Windows result belongs to its own platform/content revision.
- This was a read-only project audit. Verification used source, effective assets, existing builds and live camera/particle inspection. Tests and new Player/content builds were not run. Screenshot and particle samples were taken from the existing battle without changing its camera or playback.

## Recheck · 2026-11-07

- Follow-up implementation and measurements → [[TODOs/Optimization/Optimization_Audit_Fix_Plan]]. Preserve this note as the November 6 snapshot.
- Current successful Mac layouts prove 54 duplicated assets, now removed. Three Providence maps actually ship; Specular was present in source dependency discovery but absent from both packed layouts, so its pilot was restored.
- Confirmed preload, smoke, music, mesh/bolt memory, capture and disabled-nebula defects are addressed. Rare large fleet stalls remain unassigned; no blanket FPS or hitch-resolution claim.
