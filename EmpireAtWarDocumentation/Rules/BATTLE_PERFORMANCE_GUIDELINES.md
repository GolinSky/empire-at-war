---
type: engineering-reference
system:
  - battle-rendering
  - vfx
  - quality
  - fog-of-war
verified: 2026-10-29
verification: Current nebula/hazard/settings implementations source-reviewed;
  Unity recompile completed without errors; saved bindings/import and isolated
  non-Play-Mode nebula render comparison verified. Prior decor/fog evidence
  retained. No automated tests, battle baseline, Player build or Mac operations
  in this rollout.
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

- `MatchNebula` bakes the selected variant at 128 samples into a 4096×2048 ARGBHalf RT; normal battle views draw a depth-tested impostor after the skybox (`Transparent-440`). Camera angles >1° from the bake direction use the live volume; verify cinematic cost separately.
- `NebulaImpostor` owns and disposes its RT, mesh, material and quad; temporary bake camera isolates the volume on layer 31 and restores its original layer. Bake log measures CPU submission, not GPU duration.
- `HazardCloudView` uses 2/2/3/5/5/5 sheets for quality levels 0–5 with redistributed opacity. Cloud/warp textures are linear, repeat-wrapped and mipmapped; regenerate via `HazardNoiseBuilder`, preserve SDF edge math.
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

- Fixed implementation 2026-10-29: `BuildInfoWriter` writes Windows `<Name>_Data/StreamingAssets` or macOS `<Name>.app/Contents/Resources/Data/StreamingAssets`; unsupported targets throw. Actual Mac player build remains unverified; owner [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan]].
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
