---
type: engineering-reference
system:
  - battle-rendering
  - vfx
  - quality
  - fog-of-war
  - battle-cpu
verified: 2026-10-30
verification: "Nebula/hazard/settings source, Unity compilation, saved
  bindings/import and isolated nebula render comparison verified. ARM64 IL2CPP
  Development Player build succeeded; BuildInfo, repaired ad-hoc signature and
  M2/Metal launch checked. Xcode 27.1 Metal System Trace and XML export analyzed
  for one user-prepared hazard workload. Matched capture conditions, isolated
  per-draw budgets and acceptance remain unverified. Prior decor/fog evidence
  retained. No automated tests run. CPU changes source-reviewed and Unity
  6000.4.7f1 Windows compilation/import checked 2026-10-30; selection shader
  messages/binding checked. Tests, Play Mode and matched CPU captures not run.
  Follow-up: user-authorized crowded Windows Editor battle and actual prewarm
  capture, paired capture-geometry scans with identical tallies, geometry
  snapshot and target-lifetime source/import/compile checks; post-fix Console 0
  errors, Editor stopped on clean MainMenuScene. Automated tests not run;
  historical M2 causes unreproduced."
tags:
  - flag/important
---
# Battle Performance Guidelines

## Goal

- Preserve battle rendering correctness while changing culling, LOD, quality and VFX.
- `flag/important`: prevents regression to offscreen decor submission, incorrect particle blending and unsafe fog-job teardown.

## Rules

- Read live owners and saved assets before applying this reference; repository `AGENTS.md` remains authoritative.
- Keep decor instanced: `DecorAsteroidRenderer` owns instancing-enabled material copies and `Graphics.RenderMeshInstanced`; release owned copies on reconfiguration/destruction.
- Cull transformed world AABBs, including both source and LOD mesh bounds. Keep intersecting bounds visible; center-only tests reject visible edges.
- Cull cells first; skip per-instance plane tests only when the whole cell is inside. Distance and LOD selection still apply per instance.
- Gameplay field rocks retain GameObjects, colliders, fog and navigation. Their render LODs are separate from decorative instances.
- Migrate particle materials by effective blend/alpha/UV/tint/queue behavior. Shader-name replacement alone does not establish visual equivalence.
- Particle casters are disabled by the current recovery policy; do not extend that decision to ship/wreck mesh shadows without scoped evidence.
- A scheduled fog write owns its NativeArray/pixel data until completion; synchronize before reads, mutation, upload or disposal.

## Implementation

### Decor and field rocks

- `DecorAsteroidRenderer.cs`: 1,000-unit cells; batch size 511; cell bounds enclose transformed instance bounds.
- Saved `SceneContext.prefab`: debris distance 2,200; medium 5,000; large 12,000; LOD distance 1,000 world units. These are current decor settings, not universal rendering budgets.
- `DecorAsteroidCell.Add`: transform mesh center and sum absolute transformed extent axes; include rotation, scale and mesh-local transform.
- Medium/large field prefabs switch at 0.08 relative screen height; final LOD threshold 0 keeps distant rocks visible.
- Both field LODs reference `Asteroid_01_Surface.mat` (GUID `ff43801ad8a50574db1db7d2a0e86aca`); do not restore the historical mining-glass assignment.

### Quality and post-processing

- `MatchNebula` bakes the selected variant at 128 samples into a 4096×2048 ARGBHalf RT; normal battle views draw a depth-tested impostor after the skybox (`Transparent-440`). Camera angles >1° from the bake direction use the live volume at 48/48/64/96/128/128 samples by quality. Keep 128 as a temporary bake override; restore current live quality in `finally` and reapply on quality changes without rebaking. Verify cinematic cost separately.
- `NebulaImpostor` owns and disposes its RT, mesh, material and quad; temporary bake camera isolates the volume on layer 31 and restores its original layer. Bake log measures CPU submission, not GPU duration.
- `HazardCloudView` uses 2/2/3/5/5/5 sheets for quality levels 0–5 with redistributed opacity. Cloud/warp textures are linear, repeat-wrapped and mipmapped; regenerate via `HazardNoiseBuilder`, preserve SDF edge math. Load both textures after both bakes/imports finish; persist `_Noise` = `HazardNoise.png` and `_WarpNoise` = `HazardWarp.png` on both hazard materials. Verify saved bindings after imports.
- Graphics settings expose nebula visibility, 50–100% render resolution and supported Bilinear/FSR 1/STP. Apply scale/filter to owned runtime pipeline copies; restore originals on disposal. Do not save user scaling into preset assets.
- STP uses `renderScale` in installed URP 17.4; retain its resolution slider. DLSS/FSR 2 remain unshipped until supported activation/configuration is established; do not assume internal `Upscaling` selection is a public pipeline API.
- Quality names in current project: 0 Performant, 1 Low, 2 Balanced, 3 High Fidelity, 4 Ultra, 5 Cinematic.
- `PerformantPostProcessing` enables its serialized Volume only at index 0; other quality levels and component disable turn it off.
- `SceneContext` binds a global priority-100 sparse `PerformantVolumeProfile`: bloom Quarter / max 3 iterations / HQ false; motion-blur intensity override 0.
- Check the effective camera volume stack after quality switching. A pipeline profile can be overridden by a scene volume; saved profile values alone do not establish runtime behavior.

### Particles and fog

- `ParticleRecoveryBuilder` converts nine known shared materials; eight use `EmpireAtWar/Vfx/Particle Legacy Unlit`, double-multiply uses standard URP settings.
- Compatibility shader preserves legacy premultiply vertex-alpha multiplication and alpha/additive output-alpha clamping; retain texture transform, tint, queue and soft-particle behavior.
- `FogOfWarSystem.Update` schedules the write; `LateUpdate` completes it before texture Apply. `FogVisibilityGridModel` completes pending work before direct access and disposal.
- A synchronous grid read between scheduling/upload forces completion and can reduce worker overlap; preserve correctness and measure callers before changing scheduling.

### CPU scheduling and snapshots

- Capture-strength predicates snapshot center/radius once per due area via the sampler preparation callback. Refresh on each scan so moved/resized geometry remains correct; do not repeat Unity site-transform reads per unit.
- Budget target searches, but validate model target lifetime before every pilot tick. Dead Attack/Guard targets stop; dead opportunistic engagements disengage and resume their course, including Hunt. Cached view transforms must not outlive their target.
- Profiler frame indices accumulate across captures. Use exact ranges or explicitly clear an idle buffer after saving; top-40 summaries cannot recover uninstrumented descendants. Reproduction/verified CPU evidence → `Docs/Audits/2026-10-30-Late-Battle-CPU/analysis.md`.

- Source/Editor compile verified 2026-10-30; runtime speedup unmeasured. Owner and open investigations → [[TODOs/Optimization/Late_Battle_CPU_Fix_Plan]].
- Interval timers alone do not bound overloaded-frame work. Squadron orders cap 64 visits / 32 updates per frame; capture strength caps 4 visits / 2 area refreshes. Preserve elapsed time without replaying missed intervals; validate order/capture latency under overload.
- Shot storage is battle-scoped per prefab; mount leases retain profile/color/callback state. Owner release lets in-flight shots return; battle teardown cancels them. Loading warms roster/live station demand; runtime prewarm creates ≤1 shot/frame.
- A 0.5 ms prewarm budget checked between creations cannot interrupt one expensive Awake. Use `Battle.Projectile.Prewarm.<prefab>` / `Instantiate.<prefab>` markers before changing native prefab components.
- Selection sprites use the existing world-aligned `SelectionBrackets.shader`, `DisableBatching=true`, conservative bounds and attack-only CPU updates. Keep the saved shared material binding; validate yaw/pitch/culling/spin visually.
- `TargetOriginCapture` registers mount transforms once and completes `TargetOriginReadJob` before snapshot consumption or disposal. Parent rotation = world × inverse(local); unregister/remap slots before destroying or pooling owners.
- `SquadronIconLayer` centrally caches camera/screen/zoom and skips position changes ≤0.5 px; retain dedicated canvas isolation, vision and click behavior.
- Per-iteration captured locals can allocate even when the lambda branch is skipped. Hazard subscriptions allocate in a separate helper only on membership entry; use indexed iteration over interface lists to avoid boxed enumerators.
- `EnemyStrategicContextBuilder.Build` lends per-player collections until the next Build; current commander consumes them synchronously. Do not retain or asynchronously consume these collections.
- `BattleTelemetry` performs full CSV serialization only on tracked combat-scene exit/quit. Auxiliary additive unloads do not flush combat telemetry; data since the previous exit is not crash-persistent.

## Important Values

- Measurement protocol: record source revision + dirty paths, hardware/API, Editor versus Player, map seed + nebula variant, camera pose/FOV, preset, actual camera resolution/render scale/MSAA, population/orders/effects, time scale and capture tooling.
- Existing `RenderAuditAblationCapture`: pinned camera/input, invariant JSON, 21 base phases plus 5 largest-hazard phases when hazards exist, with 2 s settle + 6 s sample; use enabled/off/restored brackets and restore toggles on completion/cancellation.
- Match workload across before/after runs; report changing ship/squadron counts. Evolving combat windows are not deterministic matched repeats.
- Compare median/p95/p99 and run variation; validate meaningful broad claims in a Development Player. Do not add nested/parallel profiler durations or infer FPS from inconsistent GPU counters.
- Existing PC report `fog-decor-attribution.json` confirms worker fog execution; its 0.07546 ms/frame main marker and 0.87314 ms/frame combined workers are dated capture results, not a current budget.
- Existing `performant-zero-after-fix.json` records effective 48 samples / Quarter / 3 / HQ false / motion blur 0. This rollout produced no new FPS result.

## Edge Cases

### Verified solutions

- Resolved 2026-10-29, source-verified: offscreen decor submission → distance-only historical renderer → cell + transformed AABB culling. Owners above; rendered LOD/edge/gameplay acceptance remains with the recovery plan.
- Resolved 2026-10-29, source + saved JSON evidence: scene volume overrode Performant motion blur → priority-100 Performant-only sparse volume. `performant-zero-after-fix.json` records intensity 0; keep preset-switch checks.
- Resolved implementation 2026-10-29: explosion teardown accessed destroyed effects → `HardPointExplosionPool.FadeAt` removes the active entry and skips expired/destroyed effects; mandatory `ExplosionVfx.Play` setup still fails fast.
- Resolved implementation 2026-10-29: scope teardown ducking touched destroyed AudioSource → `MusicService.SetVoiceDucking` stores gain; normal Tick applies it. Research records no recurrence in later smoke checks; full lifecycle matrix remains open.

### Open issues and ownership

- Mac build verified 2026-10-29: `BuildInfoWriter` writes Windows `<Name>_Data/StreamingAssets` or macOS `<Name>.app/Contents/Resources/Data/StreamingAssets`; ARM64 IL2CPP Development Player built with 0 errors / 4 warnings, packaged JSON checked and player launched on M2/Metal. Its post-build JSON addition invalidates Unity's existing resource seal → for local development artifacts run `codesign --force --sign - <Name>.app`, then `codesign --verify --deep --strict <Name>.app`. Verified on `Builds/MacGPU/EmpireAtWar.app`; automatic post-BuildInfo signing and strict verification implemented 2026-10-30 in `BuildInfoWriter` for Mac-hosted macOS builds; next Mac build acceptance pending.
- Mac module installation: platform support alone does not prove IL2CPP is installed. Check `MacStandaloneSupport/Variations/macos_arm64_player_development_il2cpp`; install official `mac-il2cpp` if absent. Reload scripts after installing into an open Editor: cached build-window support otherwise rejects Addressables/SBP configuration. Rechecked `EnabledBuildButton()` and successful Player build establish this recovery.
- Mac tooling verified 2026-10-29: Xcode `27.1 (27A9275)` with completed first-run license/setup records the existing IL2CPP Development Player via `xcrun xctrace record --template 'Metal System Trace' --attach <PID> --time-limit 10s --output <capture>.trace`. Wait for successful file-save completion before `xctrace export --input <capture>.trace --toc` / `--xpath`; recording finalization/export can take minutes. Earlier Instruments 15.0.1 incompatibility and license exit 69 are resolved on this host.
- Metal export analysis: `metal-gpu-intervals` identifies process, GPU frame ID, start/duration, channel and formatted pass label. Filter to the Player and merge overlapping/nested active intervals; never add parallel Vertex/Fragment/Compute or pass durations. Object labels may be blank while formatted execution labels retain Unity pass names. GPU execution-start cadence is not display cadence or unprofiled FPS. Verified analyzer/evidence: `Logs/RenderAudit/20261029-Mac-GPU-Profiling/{analyze_trace.py,hazard-summary.json}`.
- Instrumented hazard sample: GPU active union median `12.42 ms` / p95 `17.90 ms`, Fragment median `11.55 ms`; no fixed-condition acceptance or isolated nebula/hazard cost established. Trace workload seed/preset/render scale were not independently verified; Player log reports `2940×1846`, not requested `2560×1440`. Per-draw frame capture and matched on/off/restored runs remain in [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan]]. The Editor-only ablation tool and disabled optional Player bridge do not provide those matched Player toggles.
- Historical Mac live-volume cost ≈7.03 ms at 48 samples; pan p99 73.54 ms / max 404.51 ms. PC bake/H1/H2 implementation is compiled/import-verified, with isolated nebula render comparison; no current battle speedup or Mac acceptance claimed. Matched captures and hitch attribution remain in [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan]].
- Mac capture limitations: GPU counters exceed frame intervals; Screen reports 642×1156 while camera is 2560×1440; camera-render screenshots omit decor/HUD; startup counter spikes are not steady combat latency. Use recorded frame intervals/camera metadata/composited evidence; tooling fixes remain under the recovery plan.
- Full faction/mount/effect reuse/impact/scene-exit matrix, rendered LOD silhouettes, PC historical comparisons, navigation attribution and absorbed experiments remain unfinished in [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]. Guidance extraction does not close or authorize them.

## Files

- Provenance: [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]; [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research]].
- Culling: `Assets/Scripts/Components/Environment/DecorAsteroidRenderer.cs`, `DecorAsteroidCell.cs`.
- Quality: `Assets/Scripts/Components/Environment/MatchNebula.cs`, `PerformantPostProcessing.cs`.
- Saved assets: `Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab`; `Assets/Prefabs/Models/Obstacles/AsteroidRockMedium.prefab`, `AsteroidRockLarge.prefab`; `Assets/Settings/Render Pipelines/PerformantVolumeProfile.asset`.
- VFX: `Assets/Scripts/Editor/Rendering/ParticleRecoveryBuilder.cs`; `Assets/Art/Shaders/Vfx/ParticleLegacyUnlit.shader`; `Assets/Scripts/Services/UnitExplosion/HardPointExplosionPool.cs`; `Assets/Scripts/Services/Audio/MusicService.cs`.
- Fog: `Assets/Scripts/Services/FogOfWar/FogOfWarSystem.cs`; `Assets/Scripts/Entities/FogOfWar/FogVisibilityGridModel.cs`.
- Capture/build: `Assets/Scripts/Editor/RenderAuditAblationCapture.cs`; `Assets/Scripts/Editor/Versioning/BuildInfoWriter.cs`.
- Read existing reports: `Logs/RenderAudit/performant-zero-after-fix.json`; `Logs/RenderAudit/20261029T122050363Z-profiler/fog-decor-attribution.json`. Mac report provenance: `Logs/BattleAudit/20261029-Mac-FPS-Recovery/results.json` on the capture host.

### Trace-based GPU fixes (2026-10-30)

- Main camera FXAA disabled; preset MSAA preserved (Performant 2×, others currently off).
- First-run Mac defaults: 77% render resolution / FSR 1; PC 100% / Bilinear. Saved settings retain their values; user controls remain available.
- Hazard sheets retain cells where any RHalf corner distance ≤90 m; bilinear interpolation cannot fall below all four corners. Preserve UV grid mapping, old outer bounds and shader SDF math.
- Loading calls `Shader.WarmupAllShaders()` after loaded battle content, before fade-out. This warms loaded variants, adds loading cost and does not guarantee Metal PSO or later-loaded shader coverage.
- Source/import/Unity compilation verified; no tests, new trace or Mac build. Savings and shader-compilation cause remain unconfirmed. Owner → [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]].
