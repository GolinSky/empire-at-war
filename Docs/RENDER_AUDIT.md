# Render audit

Audit date: 2026-09-21. Unity 6000.4.7f1, Universal Render Pipeline 17.4.0, Apple M2 / Metal, StandaloneOSX Editor. This document distinguishes configured settings from measured match behavior. Raw captures are written to `Logs/RenderAudit` and are local generated artifacts.

## Pipeline and quality settings

The connected Editor uses **High Fidelity / URP-HighFidelity / Forward+**. This is not deferred rendering. In this installed URP version, renderer mode 0 is Forward, 1 is Deferred, 2 is Forward+, and 3 is Deferred+.

`GraphicsSettings` defaults to URP-Performant, but the active quality level overrides it. Standalone defaults to High Fidelity; Android, iPhone, tvOS and WebGL default to Balanced. **At runtime, `SettingsService.Initialize()` selects the last quality level, High Fidelity, unconditionally**, and sets a 60 FPS target. The platform defaults therefore do not determine the startup runtime profile. The audit leaves this behavior unchanged.

| Setting | Performant | Balanced | High Fidelity |
|---|---|---|---|
| Renderer path | Forward | Forward | Forward+ |
| Render scale | 1.0 | 1.0 | 1.0 |
| HDR | Off | On | On |
| MSAA | 2× | Disabled (1 sample) | Disabled (1 sample) |
| Depth texture / opaque texture | On / Off | On / Off | On / Off |
| Main light | Per pixel | Per pixel | Per pixel |
| Additional lights | Per vertex | Per pixel | Per pixel, clustered |
| Serialized additional-light per-object limit | 4 | 2 | 8; Forward+ does not use the Forward per-object limit |
| Main-light shadows | On, 1024 atlas | On, 1024 atlas | On, 4096 atlas |
| Additional-light shadows | Off | Off | On, 4096 atlas |
| Shadow distance | 50 | 50 | 1000 |
| Cascades | 1 | 1 | 4 |
| Soft shadows | Off | On, Medium | On, High |
| SRP Batcher | On | On | On |
| Dynamic batching | Off | Off | Off |
| GPU Resident Drawer | Disabled | Disabled | Instanced Drawing |
| GPU occlusion culling | Off | Off | Off |
| VSync | Off | Off | Off |
| LOD bias | 0.4 | 1 | 2 |
| Texture mip limit | 2 | 1 | 0 |

All three renderer assets use disabled depth priming, copy depth after opaques, and an Always intermediate texture. High Fidelity and Balanced have SSAO active; the two ScreenSpaceOutlines features are inactive. High Fidelity SSAO uses full resolution with Depth Normals, intensity 0.5, radius 0.25, and direct-lighting strength 0.25. Balanced SSAO is downsampled and reconstructs from depth. Render Graph execution is verified by the capture adapter; the old `m_UseNativeRenderPass` serialized field alone does not establish whether Render Graph merges native passes.

High Fidelity cascade splits are 0.067 / 0.2 / 0.467, with cascade border 0.1. All profiles have URP depth and normal shadow bias 1. Additional-light shadow tiers are 128 / 256 / 512. A light can override pipeline bias and resolution, so the live light snapshot is needed alongside this table.

Sources: `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset`, `Assets/Resources/Settings/URP-*.asset`, `Assets/Settings/Render Pipelines/UniversalRenderPipelineGlobalSettings.asset`, `Assets/Scripts/Services/Settings/SettingsService.cs`, and installed URP `UniversalRenderer.cs`. Imported asset snapshots: `Logs/RenderAudit/settings/URP-*.json`.

## Culling and shadows: interpretation

Camera frustum culling excludes renderers outside the camera volume. It does not require baked occlusion or GPU occlusion. `Renderer.allowOcclusionWhenDynamic` is permission to use occlusion data, not evidence that objects are being occluded. `Renderer.isVisible` can include Scene View and shadow visibility and is not a reliable standalone test for the Game camera. See [Unity's distinction between frustum and occlusion culling](https://docs.unity3d.com/Manual/OcclusionCulling.html).

Both Corusant and Kamino scene files have null baked occlusion-data references. MainMenuScene has a baked occlusion reference, which does not establish occlusion coverage in a match. GPU occlusion is disabled in every URP profile. No project-owned custom culling matrix or custom mesh submission code was found in the targeted C# search.

The reusable Main Camera prefab enables camera occlusion, with near/far clips 0.3 / 2000. The reusable directional light requests soft shadows, strength 1, light bias 0.05, normal bias 0.4, and near plane 0.2. Live runtime settings, active casters, shader ShadowCaster passes, and actual shadow events must be checked together. Offscreen geometry may legitimately remain in a shadow pass.

### Verified Coruscant runtime configuration

The normal `IGameCommand.StartGame` route successfully loaded `Assets/Scenes/Planets/Corusant/Corusant.unity` with Republic versus Separatists, Easy AI, and 100,000 starting credits.

- The single active Game camera renders at **2560 × 1440**, perspective vertical FOV 29.8755°, clips 0.3–2000, all-layer culling mask, camera occlusion enabled, and dynamic resolution disabled. Camera HDR/MSAA permissions are enabled; the active pipeline still disables MSAA. Camera post-processing and shadows are enabled, antialiasing is **FXAA**, and there are no overlay cameras in its stack.
- The directional light is realtime, intensity 1.2, soft shadows at strength 1, culling mask 247. Its stored Light bias is 0.03 / 0.2, but `UniversalAdditionalLightData.m_UsePipelineSettings` is true, so the pipeline's bias values apply. Its soft-shadow quality inherits the pipeline. The other three enumerated lights in the raw snapshot are disabled Editor SceneLight objects in an unnamed scene; they are not active match lighting.
- Fog is disabled. Ambient lighting is Trilight, intensity 0.75; sky/equator/ground colors are (0.8, 0.84, 0.95), (0.58, 0.62, 0.72), (0.36, 0.4, 0.5). Reflection intensity is 1, default resolution 128, one bounce. The skybox is `Assets/ThirdParty/Stagit/SkyboxEarthPlanets/skyboxes/skyboxv1_starsonly.mat`. No lightmaps are loaded.
- Live inspection confirms `renderingModeActual = ForwardPlus`, a non-null GPU Resident Drawer instance, and `IsInstanceOcclusionCullingEnabled() = false`.
- Project Frame Timing Stats is disabled. Missing GPU timing must be reported as unavailable, not zero cost. Editor counters can include Editor work; a few frames establish capture functionality and pass structure, not a statistically representative performance benchmark.

## Capture procedure and measured results

Fleet benchmarking remains pending. The current-scene checks below validate the capture tools only.

While playing the desired scene, use **Tools → Render Audit → Capture All (2 frames)**. Separate Profiler and Frame Debugger menu actions are also available, with **Cancel Capture** to restore capture-owned state. Output is written to `Logs/RenderAudit/<UTC timestamp>-capture/`.

- `profiler-summary.json`: `profiler_frame_summaries[].rendering_metrics` contains recorded-frame draw calls, instancing/BRG counts, batch categories, SetPass calls, triangles, vertices, and shadow casters. All available raw rendering counters are retained. Unity 6.4 draw-call totals are derived from the eight disjoint categories used by UnityStats when a total counter is absent.
- `rendered_frame_identities[].unity_stats` contains a separately identified end-of-render snapshot, including the legacy total batches field. These snapshots are not asserted to match the recorded Profiler frame indices. A missing Profiler total-batches counter stays null; batch categories remain available. Legacy batch counts do not describe SRP/BRG draw submissions.
- `frame-debugger-frame-*.json`: every event has its type and selected-event details, including shader pass, draw/instance counts, and geometry. Clear/compute events are not draw calls. Frame Debugger replays affect rendering behavior, so compare its data separately from normal Profiler frames.
- `render-graph-frame-*.json` and `rendering-debugger.json`: pass/resource information and debug settings.

These are Editor captures and can include Editor rendering. Zero unavailable GPU timings must not be interpreted as zero GPU cost. Check `capture-status.json` before using a run; timeout/cancelled runs may contain only partial output. No automated test runner is required.

Frame Debugger fixes: target the local Editor connection (-1), release the debugger before stepping, defer stepping across Editor updates, and select each event before waiting for its data. Pause/debug state is restored on completion, timeout, and cancellation.

### Current-scene verification — 2026-09-21

All four phases completed in `Logs/RenderAudit/20260921T150958824Z-capture`. See that folder’s `RESULTS.md` for counts and scope. Two Frame Debugger exports contain 22 events each, with all 44 event indices matching their returned details. This was MainMenuScene capture verification, not fleet benchmarking. Compilation passed and the Console contained no errors.
