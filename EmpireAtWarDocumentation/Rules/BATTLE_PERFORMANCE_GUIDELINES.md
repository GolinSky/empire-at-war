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
  historical M2 causes unreproduced. Review follow-up 2026-10-30: 4 ms loading
  prewarm and next-tick full capture refresh source-reviewed/imported/compiled;
  new Capture regressions authored but not run. Earlier shader-warmup
  keyword-space Console errors remain; no new errors after refresh.
  Shader-warmup follow-up 2026-10-30: reproduced 28 native keyword-space errors,
  removed broad warmup, and verified fresh/restored Windows Play Mode startup
  with 0 new errors; GameFlow 153 passed before/after, Combat 220 and Capture
  101 passed. Mac not run."
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
- Planet atmosphere (2026-11-19): `Battle.unity` `Planet Atmosphere` global Volume, priority 50, swaps a sparse per-biome grade (WhiteBalance + ColorAdjustments filter/saturation only). URP bakes both into the existing grading LUT → no extra pass. Never override exposure/contrast/bloom there; base profile (priority 0) and Performant (100) own them → [[Done/Features/Planet_Biome_Atmosphere_Plan]].
- Check the effective camera volume stack after quality switching. A pipeline profile can be overridden by a scene volume; saved profile values alone do not establish runtime behavior.

### Particles and fog

- `ParticleRecoveryBuilder` converts nine known shared materials; eight use `EmpireAtWar/Vfx/Particle Legacy Unlit`, double-multiply uses standard URP settings.
- Compatibility shader preserves legacy premultiply vertex-alpha multiplication and alpha/additive output-alpha clamping; retain texture transform, tint, queue and soft-particle behavior.
- `FogOfWarSystem.Update` schedules the write; `LateUpdate` completes it before texture Apply. `FogVisibilityGridModel` completes pending work before direct access and disposal.
- A synchronous grid read between scheduling/upload forces completion and can reduce worker overlap; preserve correctness and measure callers before changing scheduling.

### Ship pathfinding

- Line repair: if a blocking disk covers the next internal corner's incoming tangent contact, retain the selected winding, unlink that corner and insert the blocker in the same repair. Otherwise insertion/removal can recreate the same pierced leg. Preserve endpoints/zero-radius corners and recheck the new adjacent geometry.
- Verified 2026-11-15: six seeded/mirrored code-3 regressions failed before the fix; Navigation 280/280 passed after it. Evidence and live acceptance → [[Done/Bugs/Ship_Tangent_Line_Repair_Loop]].

- Arc winding: use the sweep nearest the funnel's angular hint; a hint below 180° can legitimately require a tangent arc above 180°. A hard `SweepHint > π` gate removed the captured battle's 187.92° arc (hint 177.78°).
- Arc repair: keep original-circle prefixes/suffixes only when their tangent contacts lie on the repaired arc; retain their individual hints. Unconditional copies can remove the inserted blocker and restore the same blocked arc.
- Job diagnostics must use native output storage (`NativeReference<int2>` for repair count/limit); `.RunByRef()` does not guarantee scalar output writeback through the Unity native runner. Preserve Editor/Development-only snapshot capture.
- Verified 2026-11-15: captured battle input plus mirrored reinsertion regressions; Navigation 270/270 passed, explicit tests excluded. Live acceptance and evidence → [[Done/Bugs/Ship_Tangent_Arc_Repair_Loop]].

- Heading continuity (2026-11-15): plan from actual hull forward, independently check handoff alignment, and retain bounded yaw recovery until aligned. A lower speed alone does not justify snapping to the tangent; brake on the checked route, align at rest when necessary, then rebuild the remaining speed profile. Preserve exact tangent following on feasible aligned routes.
- Sharp corners are stopping boundaries during both normal motion and replan/recovery braking. Insufficient stopping distance → stop at the checked corner; never integrate onto its outgoing leg with residual momentum.
- Overlapping-disk repair: preferred insertion winding can lack adjoining tangents and alternate blockers until code 3. Try the opposite winding only when both adjoining tangents exist; keep all subsequent line/arc clearance checks and the bounded repair budget.
- Editor/Development builds only: `ShipTangentFailureSnapshot` retains four JSON inputs in `Application.persistentDataPath/ShipNavigationFailures`, including failed collapsed attempts. IDs are build/frame/service registration; repair metrics describe the final pass. Replay reconstructs operation order; no historical-log attribution is implied. Retention bounds file count, not geometry size.
- Evidence: mirrored code-3 regressions failed before correction; EditMode `Navigation` 260/260 and `Ships` 209/209 passed, explicit tests excluded. Runtime/online acceptance → [[Done/Bugs/Ship_Rotation_Snaps_And_Tangent_Failures]].

- Spawn-overlay reachability: cache by graph identity + captured station origin + immutable cell-array identity. Force a fresh batch on selection; inspect topology every 1 s. Never run full spawn validation per raster cell.
- Schedule the bulk mask asynchronously, poll `IsCompleted`, then `Complete` before reading results. Complete before buffer reuse/resizing or graph disposal. Movement queries may change the shared graph origin; publish only the captured matching origin/grid result.
- Use disk buckets for in-map point overlap tests while preserving strict radius math; keep outside-map fallback because buckets omit fully external disks. Initial triangulation remains synchronous. Source-reviewed 2026-11-14; full compile blocked by unrelated UI test errors, runtime savings unmeasured → [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]].

- Cache each triangle graph by map bounds, exact clearance, height-filtered obstacle disks and hazard-source identities. Movement-only changes reuse that graph with a new origin.
- Share the hazard cost raster across clearance layers. Keep raw ship height filtering and destination reservations in `ShipNavigationService`.
- Route geometry (2026-11-13, source/Unity compilation verified): `ShipTangentRouteJob` builds exact line/circular-arc pieces around inflated disks. Keep disk identity through triangulation and persistent disk buckets per graph layer.
- Tangent legs **and arcs** can intersect a non-corner disk after tightening the polygon corridor. Query both analytically and repair locally; corner-circle clearance alone is insufficient for overlapping field disks.
- Hazards are soft A* costs. Funnel smoothing may clip hazard edges; keep the direct-shortcut penalty check, never restore midpoint staircases or route-cost rejection.
- `ShipRouteSpeedProfile`: acceleration = maximum speed / 1.5 s; arc cap = yaw rate in rad/s × radius. Follower uses exact tangent yaw and signed curvature for banking; replans preserve speed while braking to a lower cap.
- Escape leg stays unchanged unless a clear fillet can trim its original adjoining lines. Start arcs require disk and map-extrema checks; rejected start arc → turn in place. A goal closer than stopping distance forces a safe endpoint stop.
- Performance and network improvement remain unmeasured. Navigation/Ships tests and explicit archetype sweeps are authored/unrun; acceptance owner → [[Done/Bugs/Ship_Route_Curvature_Plan]].
- Compare graph builds, A* searches and nearest-reachable queries with the existing `NavigationGridBuild`, `NavigationPathSearch` and `NavigationNearestReachable` markers. Record layer count, triangles, persistent memory and frame times before changing clearance grouping.

### CPU scheduling and snapshots

- Enemy placement: reject bounds, vision, area, hazard and structure exclusions before checking a per-search snapshot of threat disks. Run physics clearance last.
- Share placement and scout results per AI per frame. Invalidate before another query can reuse a spot consumed by a structure build. Revalidate a last good placement before scanning again.
- Same-seed restored Editor battle: 12 full map scans peaked at 3.30 ms, and enemy production tick peaked at 4.55 ms. No failure cooldown was needed. Baseline and restored workloads differ → [[Done/Optimization/Enemy_Production_CPU_Spike_Plan]].

- Capture-strength predicates snapshot center/radius once per due area via the sampler preparation callback. Refresh on each scan so moved/resized geometry remains correct; do not repeat Unity site-transform reads per unit.
- Budget target searches, but validate model target lifetime before every pilot tick. Dead Attack/Guard targets stop; dead opportunistic engagements disengage and resume their course, including Hunt. Cached view transforms must not outlive their target.
- Profiler frame indices accumulate across captures. Use exact ranges or explicitly clear an idle buffer after saving; top-40 summaries cannot recover uninstrumented descendants. Reproduction/verified CPU evidence → `Docs/Audits/2026-10-30-Late-Battle-CPU/analysis.md`.

- Source/Editor compile verified 2026-10-30; runtime speedup unmeasured. Historical evidence → [[Done/Optimization/Late_Battle_CPU_Fix_Plan]]. Open investigations → [[TODOs/Optimization/Battle_CPU_Performance_Plan#Late-battle CPU follow-up]].
- Interval timers alone do not bound overloaded-frame work. Squadron orders cap 64 visits / 32 updates per frame; ordinary capture sampling caps 4 visits / 2 area refreshes. `CaptureStrengthSampler.Add` / `Invalidate` require every area to refresh on the next tick, bypassing deadlines/caps before capture progresses; subsequent ticks resume limits. Preserve elapsed time without replaying missed intervals; validate order/capture latency under overload.
- Shot storage is battle-scoped per prefab; mount leases retain profile/color/callback state. Owner release lets in-flight shots return; battle teardown cancels them. Roster/live-station loading prewarm drains one queue with a 4 ms per-frame creation budget across prefab targets; runtime prewarm creates ≤1 shot/frame. Budget checks cannot interrupt an individual Awake.
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

- [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK]] — plan inventory and recipes. [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit]] — November 6 source, VFX and camera checks.

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
- `BattleStartupSequence` keeps a covered frame after fog reveal before fade-out. Broad `Shader.WarmupAllShaders()` was removed: Windows `6000.4.7f1` / URP `17.4.0` startup reproduced 28 native keyword-space errors; fresh/restored startup after removal produced 0 new errors. Keep material keywords/package shaders unchanged. Covered rendering does not prepare offscreen/future variants or guarantee Metal PSO readiness. Fix → [[Done/Bugs/Shader_Warmup_Keyword_Errors]].
- Source/import/Unity compilation verified; no tests, new trace or Mac build. Savings and shader-compilation cause remain unconfirmed. Owner → [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]].

### Verified recipes · 2026-11-07

- Hardpoint pooled VFX: `ExplosionVfx.sustainedEmitters` explicitly binds both looping emitters. Use battle-camera bounds + fog to gate emission; preserve the lease while suppressed. `AlwaysSimulate` lets stopped particles drain off-camera. Never return a live owner’s suppressed effect just because `IsAlive` is false.
- Camera metadata: record `Camera.main.pixelWidth`/`pixelHeight`; report `unavailable` when the battle camera is absent. Record active URP `renderScale` separately. Desktop window dimensions are not evidence of the battle output.
- `BeamVisuals` owns one reusable `MaterialPropertyBlock`; `ShotEffectPool` owns cached event delegates. Preserve reset/release behavior when removing allocations.
- Disabled nebula startup: no impostor allocation or bake. First enable pays the one-time bake; retain its measured wall time separately from startup and GPU submission time.
- Evidence and remaining gates → [[TODOs/Optimization/Optimization_Audit_Fix_Plan]].

### Fleet shader stalls · 2026-11-07

- Runtime Metal GPU-program creation can block the main thread outside gameplay Tick/load markers: captured 576 ms / 1.8 s gaps show `GfxDeviceClient::CreateGpuProgram → ShaderLab::SubProgram::Compile`; graphics worker waits in `MTLCompilerScheduler::buildRequest`.
- `GraphicsSettings.logWhenShaderIsCompiled = true` in a Development Player records shader/pass/stage/keywords and driver upload time. Correlate records with frame/UTC; a later repeat does not identify an earlier unlogged shader.
- Identify exact variants and rendering state before targeted warmup. Do not restore broad `Shader.WarmupAllShaders()`; prior keyword-space errors remain relevant. Mono JIT costs are separate from IL2CPP runtime behavior.
- Native waiting/context-switch captures can generate gigabytes and perturb an 8 GB host. Preserve complete symbolication, attach to verified PID, and check for owned orphan Players after capture. Mechanism attribution does not establish clean FPS/duration improvement.
- Evidence/remaining work → [[TODOs/Optimization/Optimization_Audit_Fix_Plan - Research]].

### Battle markers · 2026-11-14

- `BattleMarkerLayer` owns one battle canvas and a 10 Hz visibility/grouping snapshot. Hidden markers leave the display and membership immediately when notified. Use registered marker hit rectangles for input.
- `SquadronIconGrouping` schedules one Burst job over persistent numeric buffers, keeps published slots until a batch is ready, and completes ownership before reuse or disposal. Renderer/UI state and vision queries stay on the main thread.
- Compare matching Development Player workloads with `Battle.Markers.Vision`, `Snapshot`, `Grouping.Reconcile`, `Consume`, `Layout`, `HitQuery` and canvas rebuilds. Report candidate counts separately from worker time, main-thread p95 and managed allocations.
- Layout and allocation budgets are unmeasured. Acceptance → [[TODOs/User_Acceptance_Queue#Small Ship Icons and Squadron Icon Grid]].
- Rebuild 2026-11-18 ([[TODOs/Features/Squadron_Icon_Clustering_Rebuild]]): grouping runs in screen pixels (radii scale with zoom pitch), every squadron is a 1×1+ cluster with compact ranks, ships are obstacles not members.
- Motion = exact projected anchor + decaying springs (cell 0.1 s, placement 0.12 s); camera motion never goes through a spring. No write thresholds or dead zones.
- Placement queries a uniform screen grid of obstacles (ship hulls via `IBattleHull`, ship markers, placed grids); squadron and ship markers use separate nested canvases. Budgets still unmeasured.

### Weapon cadence and pooled overlap · 2026-11-14

- `AttackSequenceState` becomes ready after the last emitted shot; leased effects from older salvos still count until completion. Admit a full salvo only when `activeEffects + ShotsPerSalvo ≤ inFlightCap`; pool release suppresses old callbacks and resets sequence counting.
- `WeaponHardPoint.ExpectedShotsInFlight` = `shots × (1 + ceil(flight / max(reload × reloadMult, (shots − 1) × interval × intervalMult, 0.01 s)))`. Ships assigned ability `5` size for its catalog multipliers (`0.1 / 0.1`); other units use `1 / 1`.
- Create mount pools during weapon initialization; retain logical per-mount normal/Boost in-flight caps and pre-size lease dictionaries. Physical shared-cache admission uses FIFO whole-salvo reservations; active leases survive owner release until completion. Summed-capacity fixed thresholds were replaced on 2026-11-16.
- Reinforcements and first large/Boost demand may wait for budgeted pool growth; never instantiate synchronously while firing. Roster prewarm covers the largest normal-cadence loadout. Physical target = max(loading floor, active + reserved + waiting shots); cancel obsolete requests and retain returned shots. Runtime creation remains at most one clone/frame.
- `CombatModifiers.Changed` retimes remaining reload and pending inter-shot waits only after effective multiplier changes. Generic `FireDelayMultiplier` is reload-only; `WeaponPowerShotIntervalMultiplier` controls Boost spacing. Full Salvo, torpedo and main-battery channels affect both.
- Keep `EarliestFrame = frame + 1`: at most one shot per mount per frame, no catch-up. A `0.02 s` interval typically rounds up to `0.033 s` at 60 fps; exact 10× sustained DPS is not promised. Projectile damage/speed and shots per salvo remain unchanged.
- Verify no on-demand expansion with `AttackSequenceDiagnostics.PoolExpansions`; budgeted warm creation also emits `Battle.Projectile.Instantiate.*` nested under `Prewarm.*`. Measure workload counters, frame times and active-effect Update cost in a matched user capture.
- Source and Unity compilation/import verified; authored Combat/Abilities/Audio regressions not run. Gameplay and profiling acceptance → [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]].

### Combat overlap settling · 2026-11-16

- Combat stops use `IShipMovement.HoldPosition(target)`; explicit Stop/new orders/state exits cancel the hold. Do not call Stop every combat tick or cancel an active settling leg with preserve-course pursuit.
- `ShipNavigationService` owns one authority-only 5 Hz numeric Burst spacing batch; snapshot/index are O(N), queries visit ≤64 occupants with fair cursors. Cell width ≥2 × maximum radius makes 3×3 current-overlap queries radius-aware.
- Consume only completed jobs; revalidate registration/revision/target/position/eligibility. Complete ownership before resize or disposal. Only stopped occupants block settling: exclude active/queued movement and nonzero current speed in snapshots and live revalidation. Nonselected/disabled stopped ships remain anchors; obstacle-exempt ships receive no correction. Hash moving state so stopped occupancy wakes failed corrections.
- Admit ≤1 correction from spare capacity within the existing 4-plan/frame, 2 ms check-between-plans limit; normal orders win. Route followers, static graphs, hazard costs and hull-height rules remain unchanged.
- Scoped reservation grid checks current/active/pending centers with ≤256 visited centers and ≤64 cells/candidate; saturation means blocked. Never add O(N) per-candidate revalidation.
- Failed corrections use 1 s cooldown and pair/target/local-occupancy/reservation-change suppression. Whole-cell hashes cover unvisited dense occupants; `ReservationVersion` is O(1), unlike the position-scanning `Version` getter. Unrelated reservation edits can conservatively wake work.
- Distinct attack bearings apply on initial approach; after holding, ordinary/artillery re-closing uses the current ship-side bearing and preserves committed routes. Clear settling intent on arrival or failed planning. Full firing space is not guaranteed. No direct Transform separation, moving obstacles in static graphs, per-ship jobs or continuous collision-avoidance claim.
- Inspect `Battle.Spacing.Snapshot/Index/Consume`, worker `ShipSpacingJob`, navigation markers and `Spacing*` service counters. Source/Unity compilation verified; tests and matched Editor/Development Player CPU/overlap acceptance pending → [[Done/Bugs/Multiselect_Attack_Overlap]].

### Structure status icons · 2026-11-16
- Separate passive world/minimap views from selectable entity markers. Share one serialized glyph/badge mapping; no per-icon Canvas, Update, raycast target, tween or fog query.
- Prewarm one view per source per display. Process a dense active-source list; cache stationary projections until camera/viewport/layout/anchor changes. Latest source revision replaces both lifetimes.
- Advance clocks only after the same transition was displayed in the previous frame and remains eligible; a long first frame or returning HUD must not consume unseen notifications.
- Cache event positions independently of facility transforms. Subscribe to every capture source; reset pools/state on map-source changes and unsubscribe/release on scope disposal.
- Source/Unity compilation verified; zero-allocation target and CPU budget remain unmeasured. Matched workload acceptance → [[Done/Features/Structure_Status_Icons_Plan]].

### Custom shader fixes · 2026-11-16

- Disabled hazard flash uses zero strength and may have zero radius: evaluate its Gaussian only when `_Flash.w > 0`; otherwise contribute zero.
- `ClipWreck` must preserve alpha, appearance-time and torn-triangle clipping before its zero-dissolve early return. At zero progress, nonnegative value noise cannot discard a pixel; all four passes share this include.
- Wreck Forward writes stencil bit 4 (`WriteMask 4`) to hide capture/reinforcement markers behind visible hulls; preserve outline bit 8. User retained wreck team rim and current fog-grid minification.
- Particle compatibility uses material-local fragment `SOFTPARTICLES_ON`; preserve material-authored keyword retention and legacy blend math. Convert depth by perspective/orthographic projection, as in `TurbolaserBolt`.
- Inspect compiled code before removing apparent duplicate work: Coruscant D3D already removes overwritten colour/emission but retains mesh base alpha/smoothness. Decor `_CLUSTER_LIGHT_LOOP` changes main-light attenuation and must remain.
- Alpha clipping on an all-zero-alpha opaque hull discards the whole surface: cleared the ISD III `ISDII_diff_Material1_Wreck` keyword; retained mixed-alpha steelgirder cutouts. Check actual texture alpha/import settings before keyword cleanup.
- Source/import and 30 targeted D3D stage compilations verified; tests/render acceptance/Player measurements not run. Latest C# compilation succeeded; Windows/D3D11 Player experiments remain active. Owner/evidence → [[TODOs/Optimization/Custom_Shader_Fixes_Plan]].
- Clear the same stale alpha keyword on the source hull material: `ShipWreckBuilder` copies source materials before assigning the wreck shader, which can reactivate a formerly invalid keyword. Health regression covers saved and rebuilt hull/girder keyword states; authored, not run.

### Structure icon projection correction · 2026-11-16

- On the unscaled `BattleMarkerLayer` screen-space camera canvas, place bottom-left-anchored icons at projected screen point minus camera `pixelRect.min`, matching `BattleMarker`. Avoid ray conversion through the canvas world plane during camera updates.
- Reproject a fresh source revision even if its anchor is unchanged: inactive sources can retain old coordinates while other active sources update the shared camera cache.
- Position regressions must assert rendered screen coordinates on a real canvas across camera changes, not only active flags and clocks. Fix/source/compilation evidence → [[Done/Bugs/Structure_Status_Icons_Projection]]; tests and live acceptance pending.

- Structure status fade clocks: 0.2 s in / 0.3 s out; world transient stays within 2 s, minimap retains full 3 s opaque hold. Dismiss snapshots current alpha and is idempotent; baseline/map teardown clears immediately.
- State replacement crossfades use one prewarmed outgoing graphic per display, disabled after fading. Advance from the shared presenter; no per-icon Update/tween or event-time view allocation. Gameplay rules → [[GameDesign/Capture_And_Structures#Structure status icons]].

### Capture probes and decision cadence · 2026-11-16

- Capture detachment probes evaluate all sorted candidates in the same decision. Collect objectives once; use each candidate's raw local advantage and shared objective-safety rules. Do not advance shared smoothing, commitment or stall history for rejected candidates.
- Squadron periodic decisions start at per-player offsets. Ship world-change events preserve earlier deadlines and enforce a 0.5 s gap since the last decision; duplicate events must not postpone a pending decision. Pauses freeze both clocks.
- Committed heavy-capital pursuit scans run at most every 100 ms (`HARD_POINT_SCAN_INTERVAL`), moving target or not. Arrival and target replacement rescan at once. Why: a committed leg discards the offset; the scan only detects mount-range entry. Avoid: target-pose invalidation → every-frame scans against a translating target.
- `WeaponComponent.TryGetHardPointApproach` reads target points once per scan into `_approachPoints`, then compares squared distances. Avoid per-pair `Transform.position`/`CanTarget` reads (130 mounts × 177 points).
- Dev markers separate hardpoint, artillery, hold/look-at and per-player decision paths. Report periodic and launch counts separately; rates use running gameplay time.
- Measure pool peak active leases, pending/reserved shots, creation and admission latency. A calm zero-wait sample does not establish overflow behavior or boosted DPS.
- Evidence and verification → [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]]. Evolving Editor samples are not matched FPS comparisons.

### Heavy-capital and boosted-demand measurements · 2026-11-16
- Controlled `profiler` replay: Praetor 130 mounts vs Executor 177 targetable points → hardpoint scan median 4.548 ms. Stationary-position pursuit skips 51/60 scans, averages 0.688 ms/frame; translating target invalidated every frame, 4.405 ms/frame (pre-fix). Pose invalidation removed and scan inner loop reduced afterwards; post-fix cost not yet measured live.
- Actual boosted-overflow proof requires emitted boosted shots and pending demand from the same prefab during overlapping frames. Verified 57 boosted QuadProjectile emissions in four intervals with waiting positive on every sampled frame.
- Stress growth: Quad 108→225, Concussion 200→375; max 1 clone/frame globally, zero synchronous expansion/unmatched completion. Peak queue 408 shots, max ended-request wait 6.238 s. Correct bounded growth can still cause long firing delays; pick an admission-latency budget before choosing headroom.
- Target death cancels demand. Queue clearance alone does not prove every queued request was served. Record cancellation/target lifetime separately from successful admission and continued emissions.
- Diagnostic observers must cache immutable entity IDs/owner metadata before destruction; export must not resolve a dead caster. Use checkpoints and clean callbacks before reloading.
- Evidence/remaining decisions → [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]]. Editor branch attribution is not a matched whole-battle FPS result.

### Squadron budgets and model flashes · 2026-11-18

- `SquadronRegistry` counts registered living entities; pooled `start` telemetry rows are not live population. `Tools/BattleAudit/squadron_summary.py` reports distinct sampled entities per minute, not an instantaneous concurrent peak.
- `PerformanceAnalyticsSampler` writes `[Squadrons]` live-squadron/surviving-craft counts per roster player every 10 running unscaled seconds in Release too. Loading and pauses do not advance that interval.
- One laser mount per fighter; separate torpedo, missile and ion channels remain. Scale existing shared weapon damage by removed battery DPS; rebake `SquadronData.weaponLoadout`. Compare raw DPS per prefab and retain per-fighter torpedo attack-run indices.
- `ModelMuzzleFlashPostprocessor` strips embedded Flash nodes and unused mesh subassets under `Assets/Art/Models`, preserving child muzzle world poses. Preserve actual `Assets/Prefabs/Vfx` effects. Reimport, prune saved fog references, and audit missing references without unpacking inherited model instances.
- Native profiler export/decoding must run with recording disabled; skip resume boundaries, restart interrupted allocation windows, and measure cooldown after export. A live failure captured its own export until a frame exceeded 2 GB; exclude these diagnostic stalls from gameplay attribution.
- Source/import/compilation and raw-DPS/reference audits verified; automated tests not run. Outcomes and replay evidence → [[TODOs/Bugs/Squadron_Count_Stacking_And_FPS]].
