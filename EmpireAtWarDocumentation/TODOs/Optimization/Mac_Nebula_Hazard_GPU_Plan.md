---
category: Optimization
status: in-progress
created: "2026-10-29\r"
---
# Mac Nebula and Hazard GPU Plan

## Goal

- Remove the remaining Mac M2 fill-rate costs after [[TODOs/Optimization/Battle_FPS_Recovery_Plan|Battle FPS Recovery]]: backdrop nebula raymarch (~7 ms) and hazard cloud sheets.
- Add player-facing resolution/upscaler settings (Bilinear, FSR 1, STP, DLSS, FSR 2) instead of forcing a lower render scale.
- Target (Mac M2, Performant, 2560×1440, 100% resolution): ≥ 60 FPS at Audit A/B poses, a hazard-filled pose and the standard combat scenario; nebula ≤ 1 ms, hazards in view ≤ 1.5 ms of frame time.
- Evidence: [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research#Mac Validation — 2026-10-29|Mac validation 2026-10-29]] (commit `306c689d1`).

## PC Implementation — 2026-10-29

- Scope: PC implementation; all Mac tasks skipped by user. Plan stays active for prepared-battle captures and the untouched Mac follow-up.
- Implemented: Windows/macOS BuildInfo paths; largest-hazard audit pose, 32×18 SDF coverage estimate, separate nebula/hazard off/restored phases (up to 208 s).
- Nebula: 4096×2048 ARGBHalf bake, 128 samples, explicit shader binding, owned RT/quad/material cleanup; camera angle >1° from bake direction uses live volume. Bake CPU submission time logged.
- Render ordering correction: impostor uses `Transparent-440`, depth test and premultiplied blending after skybox; pre-opaque `ZWrite Off` would be overwritten by skybox.
- Hazards: 2/2/3/5/5/5 sheets by quality, opacity redistributed; 1024² linear repeat mipmapped cloud/warp textures replace runtime gradient noise. SDF/gameplay owners unchanged.
- Settings shipped: Nebula On/Off, Render resolution 50–100% (default 100%), Bilinear / FSR 1 / STP filtered by support. Runtime pipeline copies leave preset assets unchanged; unsupported saved selection is normalized before Draft clone and persisted when readable.
- STP correction: installed URP 17.4 consumes `renderScale` for STP; keep resolution slider visible. Only experimental IUpscaler negotiates input dimensions.
- DLSS / FSR 2 rejected for this rollout: Context7 quota exhausted, local documentation index empty, no official 6000.4 activation guidance confirmed. Installed source requires undocumented defines, exposes only a getter for asset selection, uses internal pipeline state and resets selection every frame. No modules, defines or private serialization workaround added.
- Verification: official Unity recompile completed `failed=false, errors=[]`; shader import messages empty; hazard GPU bake succeeded; saved shader/UI bindings checked. Material verification was incomplete: PC source review below found `_Noise` unassigned in both hazard materials. No automated tests or Mac target switch.
- Isolated non-Play-Mode nebula comparison: live mean RGB 0.0127621, baked 0.0127357, mean absolute RGB error 0.00255812 across 921600 pixels; composition/framing inspected. Local evidence: `Logs/RenderAudit/nebula-preview-live.png`, `nebula-preview-baked.png`, `nebula-preview-qa.json`.
- Isolated non-Play-Mode settings preview: all three controls bound/visible in scroll content; corrected render-resolution label/track spacing. Screenshot: `Logs/RenderAudit/settings-preview.png`. Runtime pipeline cleanup supports both Play Mode and Editor disposal.
- PC performance/visual capture gates remain: seed -802094515, A/B/hazard/combat, full-map pan, zoom/cinematic fallback, preset switching, STP ghosting/resize. Editor had only a clean untitled stopped scene; BattleAudit policy requires a user-started battle. No baseline or performance gain claimed.
- Reusable guidance updated: [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]. No companion research note created.

## PC Source Verification — 2026-10-29

- Source review of commit `9ae2d4313`; no Play Mode, no tests.
- OK: bake direction = `Main Camera.prefab` rotation (x quat 0.46174863 = 55°) = nebula volume rotation → impostor path is used in normal play; far clip 200000 covers quad (~84000 away).
- OK: `BuildInfoWriter` macOS path, settings data/Sanitize/Matches, UI row bindings in `SettingsUi.prefab`, `impostorShader` bound, ablation hazard pose + nebula toggle via `MatchNebula.NebulaEnabled`.
- OK: H1 sheet counts 2/2/3/5/5/5 match quality order Performant/Low/Balanced/High Fidelity/Ultra/Cinematic.
- **BUG:** `_Noise` unassigned (`fileID: 0`) in `NebulaHazard.mat` and `IonStormHazard.mat`; only `_WarpNoise` bound → shader default "gray" → cloud n/fine/ridge/sparkle constant 0.5, hazards look flat. Cause likely `HazardNoiseBuilder.Build` holding the first texture across the second import; load both after both bakes. Fix: assign `HazardNoise.png` (guid `735fa15d82b717f469685568e1b55e87`).
- **Regression:** `MatchNebula.cs` now always uses 128 samples (was 48/48/64/96/128/128 by quality). Cinematic fallback and any camera >1° off the bake direction (including Editor Scene view during Play) pay ~2.7× the old Performant sample cost → estimated Mac cost ≈18 ms in cinematic shots, not a new measurement. Restore per-quality samples for live rendering; keep 128 for bake only.
- OK: baked noise reproduces the old procedural noise and repeats without visible seams.
- OK: saved settings, out-of-range cleanup, change detection and all three new settings rows are wired up; upscaler choices are filtered by machine support and URP preset assets remain untouched.
- OK: audit tool includes hazard pose, hazard screen coverage and nebula on/off toggles.
- Findings supplied in the PC review; no additional runtime checks or tests performed for this documentation update.

## PC Fixes — 2026-10-29

- Resolved both defects from PC source verification above; original findings retained as history.
- `HazardNoiseBuilder.Build`: complete both bakes/imports before loading either texture; `Bake` no longer returns a texture reference held across imports.
- Ran corrected builder; `NebulaHazard.mat` and `IonStormHazard.mat` now persist `_Noise` GUID `735fa15d82b717f469685568e1b55e87` and retain `_WarpNoise`. Restored original noise PNG bytes after regeneration; texture content/import settings unchanged in final diff.
- `MatchNebula`: live samples 48/48/64/96/128/128; `LateUpdate` reapplies on quality change without rebaking. `Bake` overrides to 128, then clears `_Baking` and restores current quality in `finally`.
- Verification: official Unity recompile `completed`, `failed=false`, `errors=[]`; corrected builder completed; Unity reports correct cloud/warp paths and both materials clean after refresh/save. Serialized material GUIDs and `git diff --check` verified; no new Console warnings/errors after asset operations. Existing unrelated Console history retained.
- No tests, Play Mode, performance captures or Mac operations run. PC runtime visual/cinematic gates and all Mac phases remain open; plan stays `in-progress`.
- Reusable rules updated in [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Quality and post-processing]].

## Follow-up — 2026-10-30

- Trace analysis and new GPU levers (output resolution, MSAA + FXAA, Player toggles, per-draw capture) → [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]]. Its five fixes are implemented; no new measurement phase was executed.

## Execution Order — PC first, then Mac

- **Part 1 — PC (Windows 11):** all code, shaders, assets and settings UI (Phases 1–5). Verify on PC by compile, visual check and PC captures. Commit before switching machines.
- **Part 2 — Mac (M2):** pull the commit, build the macOS player on the Mac, measure, visual check, tune Mac-only values (Phases 6–8). Only Mac-specific numbers/fixes happen here.
- Each TODO below is tagged **[PC]** or **[Mac]**. Do not start a [Mac] item on PC: Mac IL2CPP players cannot be built from Windows, and Metal/Xcode tools only exist on the Mac.

## Decisions

### Nebula → baked backdrop impostor (variant A)

- Chosen: bake the raymarched nebula once at battle load into a 2D texture, viewed along the battle camera's fixed pitch (55°), and draw it on a large flat quad at the volume's mid-depth, perpendicular to that view direction.
- Why: the volume sits ~70,000 units away and is a thin slab (22,000 thick) → nearly all parallax comes from distance, which a real quad at that depth keeps. Per-frame cost → one 2D texture fetch per pixel. Bake with 128 samples on all presets → Mac looks better than live 48.
- Lost: internal drift (`_Flow = 0.002`, near-invisible) and parallax inside the slab itself.
- Also: add **Nebula On/Off** graphics setting (off = stars only) as a fallback for weakest hardware.
- Avoid: cubemap skybox (removes pan parallax — defeats the purpose of the volume).

### Upscaling → settings, not a forced render scale

- Chosen: Graphics settings get **Render resolution** (50–100%, default 100%) and **Upscaler** (Bilinear / FSR 1 / STP / DLSS / FSR 2). Only options supported on the current machine are shown.
- Avoid: lowering `renderScale` in any quality preset asset.

| Upscaler | Kind | Availability | Source in URP 17.4 |
|---|---|---|---|
| Bilinear | Plain stretch | All | `UpscalingFilterSelection.Linear` |
| FSR 1.0 | Spatial upscale + sharpen (single frame) | All incl. Mac (SM 4.5) | `UpscalingFilterSelection.FSR`, `fsrSharpness` |
| STP | Unity temporal upscaler (like FSR 2 / TAAU, not AI) | Compute: Windows, Mac Metal | `UpscalingFilterSelection.STP` |
| DLSS | NVIDIA AI temporal upscaler | Windows + NVIDIA RTX only | `DLSSIUpscaler` ("DLSS (IUpscaler)"), experimental |
| FSR 2 | AMD temporal upscaler | Windows (AMD module) | `FSR2IUpscaler` ("FSR2 (IUpscaler)"), experimental |

### Hazards → H1, then H2, then H3 if needed

- H1 sheets per quality: Performant/Low 2, Balanced 3, High+ 5 (top sheet keeps gameplay edge).
- H2 baked noise texture replaces 18 per-pixel gradient-noise evaluations per sheet.
- H3 tight sheet mesh replaces AABB quad (removes empty `discard` fragments).
- Parked: single-pass multi-sheet shader, zoom-based octave LOD.

## Measured Facts (Mac M2, Editor, Performant, 2026-10-29)

- Decor/field rocks no longer matter on Mac: on/off deltas inside run noise (B: 91.4 → 89.7 FPS).
- Nebula B pose: 12.03 ms → off 5.11 ms → restored 12.24 ms → **≈ 7 ms/frame at 48 samples** (81.7 → 195.7 FPS). A pose: 85.5 → 118.6 FPS.
- All heavy groups off: B 253 FPS, A 171 FPS → remaining frame is light.
- Hazards: `hazard_dust_off` shows no clear signal at A/B (B 81.3 vs 85.8/79.8; A 101.1 vs 94.3/100.7) → poses had few hazard pixels. User sees a large drop with hazards on screen → **hazard cost not yet measured**.
- `FrameTimingManager` GPU values on Mac exceed frame intervals → unusable. Editor adds ~2.5 ms `EditorLoop`.
- Mac render: 2560×1440, render scale 1, MSAA 2×, HDR off (`URP-Performant.asset`).

## Cost Model (source)

### Nebula — `NebulaFilamentVolume.shader`, `MatchNebula.cs`

- Volume box scale `(85000, 60000, 22000)` at `(0, -69000, 48000)` in `SkirmishVolumetricNebulaClouds.prefab` → ~full screen from the RTS camera.
- Per pixel: 48 steps × 2 `SAMPLE_TEXTURE3D_LOD` → ~96 3D fetches × 3.7 M px ≈ 350 M fetches/frame.
- `ZTest Always`, clipped by scene depth; all battle geometry is in front → backdrop behaviour.
- Shader already supports orthographic rays (`unity_OrthoParams.w`) → can bake with an orthographic camera.

### Hazards — `SpaceHazard.shader`, `HazardCloudView.cs`

- `SHEET_COUNT = 5` (`SpaceHazardVisualBuilder`), sheets 25 m apart, each a quad over hazard AABB + 2 × 90 m.
- Per pixel per sheet: `Fbm` octaves 3+3+4+3+3+2 = 18 gradient-noise evals → ~90 per pixel where 5 sheets overlap.
- Hazards per map: 2 / 4 / 6 (`MapGenerationSettings.HazardCount`).

## Part 1 — PC (Windows 11)

### Phase 1 — Measurement tooling and PC baseline [PC]

1. Fix `Assets/Scripts/Editor/Versioning/BuildInfoWriter.cs:28`: write `BuildInfo` to the macOS layout `<Name>.app/Contents/Resources/Data/StreamingAssets` (Windows keeps `<Name>_Data/StreamingAssets`); throw only for unsupported targets. Unblocks the Mac player build in Part 2.
2. Add a hazard-filled pose to `render_audit_ablation` (`RenderAuditAblationCapture.cs`): gameplay zoom over the largest hazard; record hazard screen coverage. Keep nebula and hazards as separate on/off/restored phases.
3. Record PC baseline (seed `-802094515`, Cinematic + Performant, 2560×1440): A, B, hazard pose, combat scenario. Save under `Logs/RenderAudit/`.

### Phase 2 — Nebula impostor [PC]

1. Bake at battle load: temporary orthographic camera with battle pitch/rotation, culling mask = nebula layer only, framing the volume footprint; render `Nebula Volume` at 128 samples into an RT (start 4096×2048, ARGB32 or RGB111110; tune).
2. Impostor: quad at the volume's mid-depth, perpendicular to the bake view direction, sized to the bake frustum; unlit background material sampling the RT; render queue before opaques (behaves as backdrop).
3. Disable the raymarch `MeshRenderer` after bake while the impostor is active. `MatchNebula` keeps variant selection and per-quality live samples (48/48/64/96/128/128) for off-angle fallback; bake at 128 samples only.
4. Release RT on battle exit. No per-frame allocations.
5. Hide the bake under the loading screen; log bake time.
6. Graphics setting **Nebula On/Off** (`GraphicsSettingsData`) → toggles the impostor.
7. Verify on PC: side-by-side screenshots live vs impostor at A/B poses, full-map pan, min/max zoom; nebula on/off delta in ablation.

### Phase 3 — Hazards [PC]

1. H1: `HazardCloudView.Configure` enables the first K sheets by quality; re-spread `LAYER_OPACITY` over active sheets; reapply on quality change (pattern: `MatchNebula.LateUpdate`).
2. Measure hazard pose (PC). Continue to H2 regardless of PC result — PC GPU hides costs that M2 shows.
3. H2: editor tool bakes tileable noise textures (R n, G fine, B ridge, A sparkle; RG warp); shader samples them with UV scroll along `_FlowDirection`. Keep SDF body/feather/wisp math. Side-by-side vs procedural.
4. H3 only if Mac Phase 7 shows hazards still > 1.5 ms: mesh from distance-grid cells with `sdf ≤ WISP_REACH`.
5. Gameplay unchanged: hazard edge `sdf = 0`, `HazardService`, damage and navigation untouched.

### Phase 4 — Resolution + built-in upscalers [PC]

1. `GraphicsSettingsData`: add `renderResolution` (0.5–1.0, default 1.0) and `upscaler` (enum); include in `Matches`/`Sanitize`.
2. Applier: on apply and on quality change, set the active URP asset's `renderScale` and `upscalingFilter` (`Linear` / `FSR` / `STP`); FSR sharpness via `fsrOverrideSharpness`/`fsrSharpness`.
3. Settings UI (read `UI_UX_GUIDELINES` and `UI_CODE_BUILD_GUIDE` first): slider + dropdown; hide resolution slider when STP/DLSS/FSR 2 negotiate their own input size.
4. STP checks: motion vectors for ships/projectiles; ghosting on bolts, particles, hazard sheets, HUD-in-world elements. Note STP replaces MSAA/FXAA as anti-aliasing.
5. Verify on PC: each upscaler at 100% and 67%, combat scenario, screenshots.

### Phase 5 — DLSS + FSR 2 (experimental IUpscaler framework) [PC]

1. Confirm in Unity 6000.4 docs (Context7) how `ENABLE_UPSCALER_FRAMEWORK` is enabled; source shows it is not set by package `versionDefines`.
2. Add built-in modules `com.unity.modules.nvidia` and `com.unity.modules.amd` (define `ENABLE_NVIDIA_MODULE` / `ENABLE_AMD_MODULE` in `Unity.RenderPipelines.Core.Runtime.asmdef`); add `ENABLE_UPSCALER_FRAMEWORK` to Standalone scripting defines if docs confirm.
3. URP assets: `UpscalingFilterSelection.IUpscaler` + upscaler options for "DLSS (IUpscaler)" and "FSR2 (IUpscaler)".
4. Runtime selection: `Upscaling.SetActiveUpscaler(name)` (core `Runtime/Upscaling/Upscaling.cs`); `upscalerName` has no public setter. Fallback if runtime switching fails: dedicated URP asset variants.
5. Availability gating: DLSS only when `NVUnityPlugin.IsLoaded()` and feature available (RTX, D3D/Vulkan); FSR 2 only when its check passes; never shown on Mac.
6. Code that touches these types stays behind the same `#if` defines so the Mac build compiles without the modules.
7. Verify on PC (RTX 5070): DLSS quality/balanced/performance, FSR 2; ghosting on bolts/particles; Mac build still compiles (switch target check only, no build).
8. Commit Part 1. Record in this note which upscalers shipped and any rejected (with reason).

## Part 2 — Mac (M2)

### Phase 6 — Mac player build [Mac]

- No paid Apple Developer account needed for local builds and testing. Paid account only needed to notarize/distribute to other players.
- Requirements: Unity Hub + Unity 6000.4.7f1 with **Mac Build Support (IL2CPP)**; **Xcode** (free, App Store, Apple ID) for IL2CPP compiler and GPU tools. Project uses IL2CPP (`scriptingBackend Standalone: 1`) → must build on the Mac.

1. Pull Part 1 commit; open project; let import finish.
2. Build Settings → macOS, Apple Silicon, **Development Build** on (Profiler connect optional).
3. Build; confirm `BuildInfo` JSON exists in `<Name>.app/Contents/Resources/Data/StreamingAssets` and main menu shows version.
4. Run locally. Apple Silicon builds are ad-hoc signed by Unity; if macOS blocks a copied app: `xattr -cr <Name>.app`.

### Phase 7 — Mac measurement [Mac]

1. Player captures (seed `-802094515`, Performant, 2560×1440, 100%): A, B, hazard pose, combat — baseline is the 2026-10-29 Editor data; also run Part 1 toggles on/off in the player.
2. Xcode GPU frame capture (Xcode → Debug → Debug Executable → the `.app`, then Capture GPU Workload) at B pose and hazard pose → per-draw GPU ms for nebula impostor, each hazard sheet, post-FX. Replaces unusable `FrameTimingManager` values.
3. Instruments → Metal System Trace for a 10 s combat window (frame pacing, GPU busy).
4. Camera-pan hitch (Editor: p99 73.5 ms, max 404.5 ms): re-run sweep in player; if hitches remain, check shader/PSO compile on first visibility → warm-up via `ShaderVariantCollection` / `GraphicsStateCollection`.

### Phase 8 — Mac tuning and acceptance [Mac]

1. Hazards > 1.5 ms → Phase 3 H3 (code on Mac or back on PC; note which).
2. Pick Mac defaults: Performant upscaler (STP vs FSR 1 vs none) and resolution — defaults only, the player can change them.
3. Visual acceptance (user): nebula impostor parallax/pan/zoom, hazard look per preset, STP/FSR 1 ghosting.
4. Record final Mac numbers here; move plan to `Done/Optimization/` per lifecycle; open gates → `TODOs/User_Acceptance_Queue.md`.

## Rules

- Gameplay hazard boundary, damage and navigation must not change.
- One variable per measurement; restore toggles; same seed/pose/preset/resolution.
- No automated tests locally (repo override); verify by compile, captures and user visual checks.
- No per-preset `renderScale` reduction; resolution is a user setting.

## Edge Cases

- Cinematic camera (`CinematicShotSolver`, `WIDE_SHOT_YAW`) views from other angles → flat impostor may look skewed. Check; fallback: re-enable the live volume only during cinematic shots.
- Battle camera has no yaw in `Services/Camera` → single bake direction is valid for normal play.
- Max zoom-out: impostor quad must cover the whole far view; size from the farthest camera frustum.
- Quality change mid-battle: nebula needs no rebake; hazard sheet count and upscaler reapply.
- Window resize: STP/DLSS/FSR 2 history reset handled by URP; verify no corruption.
- DLSS/FSR 2 unavailable at runtime (driver/plugin) → fall back to STP and save the fallback.

## Files

- `Assets/Art/Shaders/Vfx/NebulaFilamentVolume.shader`, `Assets/Scripts/Components/Environment/MatchNebula.cs`, `Assets/Prefabs/Vfx/Nebula/SkirmishVolumetricNebulaClouds.prefab`
- `Assets/Art/Shaders/Vfx/SpaceHazard.shader`, `Assets/Scripts/Components/Environment/HazardCloudView.cs`, `Assets/Scripts/Editor/SpaceHazardVisualBuilder.cs`
- `Assets/Scripts/Services/Settings/GraphicsSettingsData.cs`, `SettingsService.cs`, `ISettingsApplier.cs`; settings UI under `Entities/MainMenu` / `Entities/Menu`
- `Assets/Settings/Render Pipelines/URP-*.asset`, `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset` (defines)
- `Assets/Scripts/Editor/Versioning/BuildInfoWriter.cs`, `Assets/Scripts/Editor/RenderAuditAblationCapture.cs`

## TODO

### Part 1 — PC

- [x] [PC] Phase 1 implementation — `BuildInfoWriter` macOS path and hazard pose/coverage in ablation tool.
- [x] [PC] Fix hazard cloud noise: `HazardNoiseBuilder.Build` loads both textures after both bakes/imports; both saved materials bind `_Noise` to `HazardNoise.png`. Builder execution, Unity bindings and serialized GUIDs verified; in-game cloud appearance remains part of visual capture acceptance.
- [x] [PC] Restore live nebula quality samples 48/48/64/96/128/128, including quality-change handling; bake temporarily uses 128 and restores current quality in `finally`. Compilation and source review complete; cinematic/off-angle runtime capture remains pending.
- [ ] [PC] Prepared-battle baseline and matched captures (see PC implementation gates above).
- [x] [PC] Phase 2 implementation — nebula impostor bake + Nebula On/Off setting; isolated render comparison verified.
- [x] [PC] Phase 3 implementation — hazards H1 + H2; H3 deferred to Mac result.
- [x] [PC] Phase 4 implementation — resolution slider + Bilinear/FSR 1/STP setting.
- [x] [PC] Phase 5 evaluation — DLSS + FSR 2 rejected for this rollout; reasons recorded above. Mac target switch skipped.
- [x] [PC] Commit implemented Part 1 with this plan; performance gates and Mac follow-up remain active.

### Part 2 — Mac

- [x] [Mac] Phase 6 — Development player build (IL2CPP, Apple Silicon); BuildInfo/signature and launch verified below.
- [ ] [Mac] Phase 7 — player captures, Xcode GPU capture, pan-hitch check.
- [ ] [Mac] Phase 8 — H3 if needed, Mac defaults, visual acceptance, close plan.

## Mac Execution — 2026-10-29

- Scope: Part 2 only; clean checkout already contained PC fixes at `2aa218c52`. No code, shader, preset or gameplay changes.
- Host: Apple M2 / Metal; Unity `6000.4.7f1`; macOS `27.0 (26A428)`; Xcode `15.0.1 (15A507)`; IL2CPP.
- [x] Phase 6 prerequisites: installed missing official `mac-il2cpp` module; verified ARM64 development IL2CPP support. Local Editor architecture `OSXUniversal/ARM64` (previous `x64ARM64`).
- Initial builds failed Addressables/SBP configuration validation before producing an app. Script reload after module installation refreshed the build-window extension; `EnabledBuildButton()` then returned true. No Addressables group/schema edits.
- [x] Development + DetailedBuildReport build succeeded: `Builds/MacGPU/EmpireAtWar.app`; ARM64 Mach-O; `3,161,995,381 B`; report duration `1,555,027 ms`; `0` errors / `4` warnings.
- Warnings: disabled optional Pipeline runtime bridge; empty Heathen UX assembly; Metal vector truncation in URP `DebugOccluder`; incomplete vertex initialization in MPUIKit `MPImageBasic`. None is a build failure.
- [x] BuildInfo verified: `Contents/Resources/Data/StreamingAssets/build-info.json` → version `0.1.0`, build `local`, full source commit `2aa218c52a51b93fa72e4f32d9cd3a9cd8d39458`.
- [x] Local signing repaired: `BuildInfoWriter` adds JSON after Unity signs the app, invalidating its resource seal. `codesign --force --sign - Builds/MacGPU/EmpireAtWar.app` → `codesign --verify --deep --strict` passes, including nested native libraries. This repairs this artifact; automatic post-BuildInfo ad-hoc signing and verification implemented 2026-10-30; next Mac build acceptance is pending.
- [x] Player launched on M2/Metal; UI inspection showed a running Republic skirmish, rendered nebula, Development Build label and debug overlay with zero errors. Main-menu version label was not inspected. Battle seed/preset/render scale/pose were not verified, so its visible FPS is not a matched performance result.
- Player bake log: `4096×2048`, `128` samples, `51.6 ms` CPU submission. This is initialization evidence, not GPU bake duration or per-frame cost.
- [ ] Phase 7 matched Player A/B/hazard/combat and pan captures: not recorded. Existing `render_audit_ablation` is Editor-only and the optional Player Pipeline bridge is disabled in this artifact; a Player capture route remains necessary.
- Phase 7 GPU frame capture / Metal System Trace deferred by user: Instruments `15.0.1` fails to launch on macOS `27.0` (`kLSIncompatibleApplicationVersionErr`, OSStatus `-10664`); `xcrun xctrace list templates` exits `137`. Update Xcode before resuming. Clang and Metal compiler version probes succeed.
- [ ] Phase 8 H3 decision / defaults / visual acceptance: pending measurements and user review. No H3 mesh or default upscaler/resolution change is justified by current evidence. No new nebula/hazard GPU cost or ≥60 FPS acceptance claimed.
- Cleanup verified through Unity: restored build-generated font/preloaded-asset changes; removed generated Addressables OSX/link.xml output; fallback font loads; importing/compiling false. Only vault documentation differs. No automated tests or Editor Play Mode run.
- Evidence: `Logs/RenderAudit/20261029-Mac-Nebula-Hazard/{environment.json,first-build-failure.log,build-report.json,build-summary.json,player-startup.log}`; app above. UI inspection is recorded in this conversation.
- Reusable guidance: Mac module/reload/signing rules updated in [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]. Plan remains active because matched measurements, tuning decisions and earlier PC capture scope are unfinished.

## GPU Profiling Retry — 2026-10-29

- User resumed GPU profiling after updating Xcode; the earlier profiling deferral is superseded by this request.
- Verified Xcode `27.1 (27A9275)`. Initial `xctrace` exit `69` (unaccepted license) resolved after user completed first-run setup. `xcrun xctrace list templates` and attached Metal System Trace captures now succeed.
- Existing development app signature still passes `codesign --verify --deep --strict`; relaunched successfully. Main-menu version label verified: `v0.1.0 | Build local | 2aa218c`.

- [x] GPU profiling route verified: attached `Metal System Trace` to development Player PID `90514`; 5 s menu connection-check and requested 10 s user-prepared hazard-battle recording saved successfully. Recording completion/export took minutes; no game restart or pause.
- Hazard trace actual run duration `17.485275 s`; analysis window `14.912472 s` after excluding first/last two GPU frame IDs → `861` frames / `81,822` player GPU intervals. Trace setup/stop overhead means this is not an exact 10 s steady combat acceptance window.
- Method: use Metal execution intervals attributed to `EmpireAtWar`; exclude WindowServer; merge overlaps/nesting per frame across Vertex/Fragment/Compute. Per-channel/pass times overlap and must not be added. GPU execution-start cadence is not display cadence or unprofiled FPS.
- Player GPU active union: median `12.421748 ms/frame`, p95 `17.897297 ms`, p99 `19.954326 ms`, max `24.834039 ms`; union busy fraction `70.62%` of analyzed window.
- GPU channel medians: Fragment `11.552584 ms`, Vertex `1.024836 ms`, Compute `0.357625 ms`. Fragment work dominates GPU activity; this alone does not establish the overall frame bottleneck.
- Named GPU-pass medians: `RenderLoop.DrawSRPBatcher` `3.4589 ms`; `RenderLoop.Draw` `3.4093 ms`; final post-processing `1.9277 ms`; post-processing `0.9492 ms`; CopyDepth `0.8391 ms`. Raw object labels are blank; formatted execution labels provide pass names.
- Limitations: exact seed/preset/render scale/camera coordinates were not independently verified; last Player-log surface size `2940×1846` differs from requested `2560×1440`. Workload was the user's prepared hazard skirmish, not a verified standard combat scenario. No before/off/restored comparison or ≥60 FPS acceptance claim.
- Per-draw nebula and individual hazard-sheet costs remain unresolved: shared Unity encoder groups do not isolate those draws. Native UI automation failed player clicks (`noWindowsAvailable`) and Xcode screenshot/debugger control; no `.gputrace` frame capture produced. Do not choose H3 or new defaults from these aggregate pass values.
- Evidence: `Logs/RenderAudit/20261029-Mac-GPU-Profiling/{menu-connection-check.trace,hazard-battle.trace,hazard-toc.xml,hazard-gpu.xml,hazard-summary.json,analyze_trace.py}`. Tests not run; source/presets unchanged. Next: verified fixed-resolution A/B/hazard/combat captures plus Xcode per-draw frame capture.
