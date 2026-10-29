---
category: Optimization
status: in-progress
created: 2026-10-29
---
# Battle FPS Recovery Plan

## Goal

- Win back the ~80 FPS lost after the 2026-10-28 changes; restore battle FPS on PC (RTX 5070) and Mac (M2).
- Reduce the cost of asteroids (decor + field), nebula/volumetric shaders, VFX particles and quality presets without changing gameplay.
- Evidence: [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research|PC capture 2026-10-29 vs Mac audits]]; Mac audits: [[Done/Optimization/Battle_Rendering_Performance_Audit]].

## Decision

- **Root cause of the regression (strong evidence):** `b19830c53` "- hazard system" (2026-10-28) added `DecorAsteroidBuilder` (`MAX_INSTANCES = 15000`) and `DecorAsteroidRenderer`, which draws every instance within distance with no frustum culling.
- **PC proof:** baseline **135.8 FPS / GPU 7.0 ms** → decor off **174.0 FPS / GPU 2.6 ms**; live reading with decor off ≈ **199 FPS**. 57.2 M → 0.2 M triangles.
- **Frustum evidence:** in the measured gameplay view **0 of 6,415** submitted decor rocks were inside the camera frustum, yet **43.0 M triangles** were drawn → per-instance culling alone removes ~all decor cost in normal views.
- **Mac:** same renderer costs **15.6 ms (Audit A)** and **31.2 ms (Audit B, Performant)**; M2 is GPU-bound, so every GPU ms matters ~5× more there than on the RTX 5070.
- **Second GPU cost:** Nebula Filament Volume (`_Samples = 128`, `ZTest Always`) ≈ **2 ms GPU on PC**; on M2 it is likely the largest cost after asteroids. Not yet isolated on Mac.
- **Not significant on PC:** post-processing, fog-of-war overlay, hazard clouds + map dust, star particles (all within noise); sun shadows ≈ 0.6 ms GPU.
- **Avoid:** cutting visible rock coverage or changing gameplay asteroid fields/colliders/navigation; claiming FPS wins without a matched before/after with the same seed, camera and preset.

## Rules

- Keep decor materials instanced (`enableInstancing = true`, `Graphics.RenderMeshInstanced`).
- Cull per instance using transformed mesh bounds vs. camera frustum planes; bounds that intersect a plane stay visible. No center-point-only test.
- Gameplay field rocks keep GameObjects, colliders, fog entries and navigation; only their render mesh/LOD may change.
- Every measurement: same map seed, fixed camera pose, same quality preset, 2 s settle + 6 s sample, on/off/restored.
- Restore all temporary runtime toggles after measuring. No automated tests unless the user asks (repo override).

## Implementation

### Phase 0 — Confirm the regression (investigation)

1. Build a reusable editor tool `RenderAuditAblation` from the 2026-10-29 eval script (see Research): phases toggle decor, field rocks, nebula, stars, hazards/dust, fog, post-FX, shadows; writes JSON to `Logs/RenderAudit/<ts>-ablation/`.
2. Fix the tool gaps found on 2026-10-29: pin the camera pose for the whole run (camera moved between phases), write numbers with `CultureInfo.InvariantCulture` (JSON had `135,79`), and record map seed.
3. Bisect: measure `b19830c53^` vs `b19830c53` vs `HEAD` with the same seed/camera/preset on PC. Accept the root cause when the delta ≈ decor-off delta.
4. Repeat the ablation on Mac (Performant) to rank nebula/bloom/motion blur/shadows on M2.

### Phase 1 — Decor asteroids (largest win, P1)

1. **Spatial cells:** in `DecorAsteroidRenderer.Configure`, bucket instances into grid cells (e.g. 1,000 units) per mesh; store a cell AABB + its matrix array.
2. **Culling per frame:** `GeometryUtility.CalculateFrustumPlanes(camera)` → test cell AABB → for intersecting cells test each instance's precomputed world AABB → fill `_batch`. Fully-inside cells can be submitted without per-instance tests.
3. **CPU:** removes the 0.8 ms `DecorAsteroidRenderer.LateUpdate` linear scan; move to a Burst job only if the cell version still exceeds ~0.2 ms.
4. **Large-rock distance:** replace `camera.farClipPlane` (200,000) with a dedicated `largeDistance` (start 12,000; tune visually).
5. **LOD meshes:** add low-poly decor meshes — targets: medium 11,080 → ≤ 1,500 tris, large 7,116 → ≤ 1,200, debris 714/934 → ≤ 200. Decor sits outside the playable area, so far LOD is visually safe. Pick LOD by distance per instance in the culling loop.
6. Reconsider `MAX_INSTANCES = 15000` and `RockDensity` (6/4/2 per size preset) only if 1–5 are insufficient.

### Phase 2 — Gameplay field rocks (P1)

- `AsteroidRockMedium` = **11,080 tris** per instance; field spawns ~1,350 medium + ~740 large + ~3,000 debris GameObjects per map (seed-dependent: 2,182–5,108 measured).
- Add `LODGroup` (or swap to the Phase 1 low-poly mesh at distance) on `AsteroidRockMedium`/`AsteroidRockLarge` prefabs; Unity frustum culling already applies.
- Check `AsteroidRockMedium` material `AsteroidMiningFacility_Glass` (URP/Lit, environment reflections + specular on) — likely a wrong/expensive material for a rock; switch to the asteroid surface material.

### Phase 3 — Nebula and volumetric shaders (P2)

- `NebulaFilamentVolume.shader`: all three nebula materials (`AzureRift`, `EmberWings`, `VioletPillars`) use **128 samples**, two 3D texture fetches per step, `ZTest Always`.
- Options in order: (a) per-quality sample count (Performant 48, Balanced 64, High+ 96–128); (b) render at half/quarter resolution and upsample; (c) blue-noise jitter + fewer samples; (d) for the top-down RTS camera, bake the backdrop to a render texture and refresh only when the camera moves far.
- Also review `NebulaCloudVolume` (64), `NebulaBillowVolume` (80), `VolumetricNebulaRaymarch` (≤ 30) if they appear in battle.
- Target: ≤ 0.5 ms GPU on RTX 5070 at 1440p; measure on M2.

### Phase 4 — Quality presets (P2)

- **Performant (Mac):** nebula low samples, Bloom with high-quality filtering off and fewer iterations (17 bloom passes captured), motion blur off, keep MSAA 2× only if GPU budget allows.
- **Cinematic:** shadow distance **3,000**, 4 cascades, 4096 map, HDR, GPU Resident Drawer on; shadows cost ~0.6 ms on PC — keep, but review `shadowDistance` vs. camera height.
- Expose decor density/LOD bias per preset if Phase 1 still leaves Mac below target.

### Phase 5 — VFX particles (P2)

- Live battle: **128** particle renderers use `Legacy Shaders/Particles/*`; **112** have shadow casting on (explosion pools: `ExplosionVfx`, `FighterExplosionVfx`, `thickSmoke`, `fireball`, `fire`).
- Set `ShadowCastingMode.Off` on all particle renderers; migrate legacy materials to `Universal Render Pipeline/Particles/Unlit` (coordinate with [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan]] and [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan]]).
- Measure under real combat (Battle Simulator scenario) — idle battles only had 6 playing systems (stars).
- Fix `MissingReferenceException` in `ExplosionVfx.StopEmitting` ← `HardPointExplosionPool.FadeAt` during teardown (seen in console).

### Phase 6 — CPU follow-ups (P3)

- `FogFadeWriteJob (Burst)` runs on main thread, **0.69 ms/frame** → schedule async or reduce update rate (see [[Done/Optimization/Burst_Jobs_Optimization_Plan]]).
- `MusicService.SetVoiceDucking` throws `MissingReferenceException` on scope dispose (`ShipSfxService.Dispose`) — teardown order bug, not FPS.

## Important Values

| Metric | Mac M2 Audit A | Mac M2 Audit B (Performant) | PC RTX 5070 (Cinematic) |
|---|---:|---:|---:|
| FPS decor on | 39.2 | 19.3 | 135.8 |
| FPS decor off | 100.9 | 48.6 | 174.0 |
| Decor cost (frame ms) | 15.6 | 31.2 | 1.6 frame / **4.4 GPU** |
| Triangles on → off | 52.1 M → 0.29 M | 59.0 M → 0.11 M | 57.2 M → 0.20 M |
| Resolution | 2560×1440 | 2560×1440 | 2560×1440 |

- PC after all rocks + nebula + post-FX + shadows off: GPU **0.6 ms**, FPS ~168 → CPU-bound in Editor (main thread ≈ 4.7 ms).
- Success targets (same seed/camera/preset): PC Cinematic Editor ≥ **190 FPS**; Mac Performant ≥ **45 FPS** (Audit B view) and ≥ **90 FPS** (Audit A view); typical-view triangles ≤ **3 M**.

## Edge Cases

- Camera moved between ablation phases on PC (not pinned) and the battle evolved; the decor-off delta is large and consistent with the Mac on/off/restored audits, but small deltas (< 0.5 ms) are noise.
- PC run is Editor Play Mode with `EditorLoop` ≈ 1.1 ms; confirm in a Development Player before final numbers.
- Map is random per battle (field rocks 5,108 in run 1, 2,182 in run 2) → always record the seed.
- `UnityStats.triangles` is Editor-wide; Scene view open adds draws.
- Rocks at frustum edges, rotated/scaled rocks (scale 1.5–2.8 for large), and fast camera pans must not pop.

## TODO

- [x] Phase 0 tool: `render_audit_ablation` / Editor menu; fixed camera + input lock; invariant JSON; actual seed, quality, resolution, source revision/dirty paths and nebula variant. Runtime acceptance passed: all 21 phases completed; actual field-root discovery, cancellation/restoration, pinned camera and invariant numeric JSON verified.
- [ ] Phase 0 PC: matched Development Players `b19830c53^` / `b19830c53` / current changes. Deferred by current user instruction: no builds or performance runs; do not resume the historical build.
- Mac measurement deferred by explicit user request on 2026-10-29; do not run in this execution.
- [x] Phase 1 implementation: 1,000-unit cells + transformed AABB frustum tests; large distance 12,000; decor LOD distance 1,000. Meshes: medium 1,496; large 1,196; debris 196 each.
- [x] Phase 2 implementation: large/medium field LODGroups (8% screen height transition; final LOD stays visible); medium uses `Asteroid_01_Surface`. Colliders/gameplay retained.
- [x] Phase 3 implementation: cached per-renderer samples — Performant/Low 48, Balanced 64, High Fidelity 96, Ultra/Cinematic 128. Reduced resolution/bake reserved for unmet GPU target.
- [x] Phase 4 implementation: Performant bloom quarter resolution, max 3 mips, HQ filtering off; motion blur intensity 0. Saved/read back and verified in the actual camera stack: Performant (index 0) 48 samples / 3 bloom mips / Quarter / HQ false / motion blur 0. A priority-100 sparse volume is active only on Performant; Cinematic restores original effects.
- [x] Phase 5 implementation: 131 source-prefab particle renderer occurrences, 32 shadow casters → 0; nine shared legacy materials migrated. Eight use a compact URP-compatible pass preserving legacy alpha/premultiply formulas; double-multiply uses standard URP settings. Explosion pool expires destroyed effects during teardown; required `Play` references still fail fast.
- [x] Phase 5 measurement: Simulator 10.001 s / 1,895-frame running capture (Cinematic, 2560×1440, 1×): average frame 5.276 ms, GPU 2.211 ms, 1.307 M triangles; live snapshot 11,319 particles, 0 shadow casters and 0 legacy particle materials. No pre-migration combat baseline; appearance remains a user gate.
- [x] Phase 6 implementation: schedule fog write in `Update`, complete/upload in `LateUpdate`; synchronize grid access/disposal before releasing texture data. 120-frame profiler confirms worker execution (frames 10–119: workers 0.87314 ms/frame combined; main marker 0.07546 ms/frame). Music ducking gain now applies in normal `Tick`, avoiding teardown access to a destroyed AudioSource.
- [ ] Final: matched before/after on PC; Development Player confirmation. Mac measurement explicitly deferred by user for this execution.

## Absorbed Open Items (2026-10-29)

- This plan is the single owner of these obligations. Source plans are closed history; read them for evidence before acting.
- Shared gate for all items: matched before/after captures (same seed, camera, preset, population) and Development Player confirmation.
- From [[Done/Features/Space_Hazards_Rework_Plan]] step 8: matched frame time of the decor asteroid ring vs baseline (only CPU submission measured). From [[Done/Features/Space_Hazards_Shape_Rework_Plan]] step 8: transparent overdraw of the 5 hazard sheets at max zoom.

### From [[Done/Optimization/Battle_Rendering_Performance_Audit]]

- [x] Implement camera-frustum culling with instanced asteroid materials; source rechecked 2026-10-29. Runtime edge/pan acceptance remains below.
- [ ] Verify offscreen rejection, frustum-edge visibility, rotated/scaled bounds, camera movement and unchanged gameplay.
- [ ] Repeat fixed-camera on/off/restored and before/after captures with matched quality, workload and actual render resolution.
- [ ] Measure nebula/post-processing GPU cost and representative explosion/projectile load.
- [x] Review mesh LOD/distance policy, legacy particle materials, shadow flags and culling implementation against live source/assets (2026-10-29); visual acceptance remains open.
- [ ] Fix audit-tool exceptions and validate GPU-counter interpretation; confirm results in a Development Player.

### From [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan]]

- [ ] Isolate smokeSheet03 (247 draws), BlasterBolt (183), BlasterBolt_Tiled (111) independently; record visible effects, vertices, material/state groups, submission CPU and GPU scope time.
- [ ] Separate batching/submission experiments from smoke size/lifetime/density/overdraw experiments; change one variable, restore diagnostic toggles.
- [x] Particle-shadow policy reconciled: the active 2026-10-29 user request explicitly disables all particle casters, superseding the historical no-blanket-change restriction. Historical evidence retained: 0.218 ms GPU scope; 347 ship + 7 wreck draws; no combat-particle ShadowCaster draws in that old capture.
- [ ] Separately measure ship/wreck caster distance, LOD and wreck lifetime after higher-priority groups; ship/wreck shadow settings were not changed by this execution.
- [ ] Obtain usable motion-blur timing with matched variants. Bloom 0.046 ms and SSAO 0.055 ms in the old adjacent snapshot → low priority; 16 bloom passes are mostly reduced resolution.
- [ ] Record matched baseline and isolate cost.
  - Compare battle start (~200 FPS / 5 ms) with mid-battle (~12 FPS / 83.3 ms); values are user observations, not recorded benchmarks.
  - Match camera, resolution, quality, time scale and Editor layout; record changing ship/squadron counts, orders and shot activity.
  - Track active/idle/ownerless projectile effects, beam smoke, explosions, engine particles and pool growth; distinguish cold from warmed pools.
  - Repeat ≥3 captures covering ≥10 seconds each for idle/movement/firing/deaths; record source revision.
  - Compare frame/CPU/render/GPU median/p95/p99, draw/SetPass counts and particle Update/Geometry markers.
  - Temporarily isolate stars, nebula, engine trails and beam smoke one family at a time in a diagnostic run; restore afterward.
  - Raw timeline reviewed: 8 flood jobs/frame; 8/7 execute on main thread; 8 navigation-containing ticks account for ~96% of ship-tick time. Capture order/state and Plan-reason data next.
- [ ] Investigate ship/navigation CPU growth first.
  - Attribute `Battle.Ship.Tick` inclusive/self time and route queries to active orders and ships; distinguish computation, waits and allocations.
  - Source review: each Plan creates/disposes a full-map grid; jobs complete immediately; managed candidate checks and route construction remain outside Jobs.
  - Check stopped/out-of-range Hunt/Guard and congested formation retries; these source paths can request routes every tick.
  - Measure Plan reasons, candidate counts, grid sizes and managed route validation before selecting a fix.
  - Propose route reuse/invalidation or scheduling changes only after attribution; preserve movement, obstacle avoidance and command responsiveness.
- [ ] Reduce persistent beam-hit smoke and fire overlap.
  - [x] `LaserProjectile.prefab` and `ProtonBeamView.prefab`: nested `HitEffect/ppfxPipelineFire` 100/s → 50/s. Death-explosion source, lifetime 1–6 s, sizes and other hit systems retain their values.
  - [x] Seeded preview simulation: 362 → 185 live particles after 6 s; zero after draining for 6.1 s in both beam prefabs. Stop/impact code was not changed.
  - 25/s remains a later density experiment; retain 50/s until combat appearance and measured benefit justify another reduction.
  - Preserve armor/shield distinctions, impact timing and stop/drain behavior; check reuse, owner death and scene exit.
- [ ] Test targeted materials and shared batching.
  - [x] Reconcile obsolete migration step: live `DualProjectile.mat`, `ShipExplosionSmoke.mat`, `Engines.mat` and `Vulture_MuzzleFlashHelper.mat` already use URP Particles/Unlit. No redundant material migration.
  - Engines/projectiles/impacts already share materials; different-material batch breaks do not prove per-instance clones.
  - Attempt an atlas only for effects whose texture, blend, vertex streams and transparent ordering can be preserved.
  - Existing bolts are one stretched billboard each; consider a shared emitter/mesh submission only after measured submission cost justifies it.
- [ ] Tune background and engine budgets only if isolation identifies material cost.
  - Stars are present at the reported ~200 FPS start; do not treat their 54,000-quad count alone as the cause of mid-battle slowdown.
  - Current stars: 5,400/s, lifetime 15 s, cap 54,000; compare population and screen coverage across both battle stages before reducing them.
  - Nebula: 48 instanced mesh particles, 80 ray samples/material; compare 64/48 samples only if pass measurements justify it.
  - Engines: 19 prefab systems sharing `Engines.mat`, 15 particles/m, 1 s lifetime; moving scenes determine actual demand.
- [ ] Accept or revert each experiment before advancing.
  - Improvement must exceed repeat-run variation; keep before/after median/p95/p99 and visual evidence.
  - No gameplay, lifecycle, transparency-order or visual regressions; no new console/import errors.
  - Confirm meaningful improvements in a Development Player before making broad performance claims.

### From [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan]]

- [ ] Export effective Unity-resolved nested settings and shader/import state; distinguish active renderers from inherited dormant components.
- [ ] Record reference images and material-instance identities for the variation matrix below; capture source revision and current local diff.
- [ ] Use existing complete battle captures to choose candidates. Obtain targeted before/after PC captures for acceptance; no broad idle-scene investigation.
- [ ] Compare `IonShotHead.mat` ↔ `ImpactFlash.mat`, and `IonShotTrail.mat` ↔ `ProtonTorpedoTrail.mat`: same shader/base texture/active keyword families; serialized emission colors differ while `_EMISSION` is absent.
- [ ] Check all consumers and effective pass/queue/blend/UV/fade/color state. Share a material only after rendered equivalence is demonstrated; keep GUIDs and unused assets until separately reviewed.
- [ ] Keep `Projectile.mat` and `DualProjectile.mat` separate initially: clamp versus repeat textures and different multi-bolt shape behavior must survive.
- [ ] Check all runtime/editor consumers of `BlasterBolt.png` and `BlasterBolt_Tiled.png`; disable Read/Write only when no CPU pixel reader needs them.
- [ ] Both sources are 1000×50, readable, no mipmaps, bilinear; first clamps and second repeats. Preserve wrap, alpha and current visual footprint.
- [ ] Inspect effective Standalone GPU format before choosing compression; pilot moderate PC alpha-preserving compression only if memory benefit and thin-bolt quality justify it.
- [ ] Profile bolt/muzzle/beam draws separately; test soft-particle and camera fading independently on scoped material copies.
- [ ] Keep fades whenever removing them produces intersection seams or camera popping. Do not disable the camera depth texture globally.
- [ ] Investigate `TractorBeam.mat`: `_FADING_ON` remains listed although camera fading is disabled; verify generated effective behavior before normalizing keywords.
- [ ] If muzzle fragment cost is measurable, test algebraically equivalent simplification of `IonGlow.shader`; retain HDR color/core/glow, vertex alpha, additive blend and all users' appearance.
- [ ] Evaluate additive bolt per-particle sorting only after overlap parity checks; keep particle counts, shape, lifetime and culling unchanged.
- [ ] Run the variation matrix: single/dual/quad/octuple; Triple mount's current fallback; four faction colors; explicit cyan override; small fighter bolts and all visual overrides.
- [ ] Include ion/concussion/proton trails, laser/composite/tractor beams, hypervelocity slug, IonPulseShot and IonPulseWave; superweapon sizes/speeds must not be capped.
- [ ] Exercise simultaneous mixed colors, repeated reuse, shield/armor hits, misses, target/owner death, interception, pause/resume, time-scale changes, offscreen exit/reentry and scene exit.
- [ ] Compare ≥3 matched firing runs per candidate: CPU/render-thread/GPU median and p95/p99, draws/SetPass, particle Update/Geometry, material/texture memory and GC. Accept only gains exceeding run variation with no visual/lifecycle regression.
- [ ] Validate targeted serialized references and relevant existing effect tests; follow AGENTS.md clean-scene + async test rules. No new blanket test suite required for importer-only changes.
- [ ] Import/save changes through Unity, inspect console/shader errors, read back persisted assets. Record each retained/reverted experiment and exact target hardware.

## Files

- `Assets/Scripts/Components/Environment/DecorAsteroidRenderer.cs`, `DecorAsteroidMesh.cs`
- `Assets/Scripts/Entities/Map/Generation/DecorAsteroidBuilder.cs`, `Assets/Scripts/Entities/Map/MapLayoutView.cs`
- `Assets/Scripts/Editor/DecorAsteroidVisualBuilder.cs`
- `Assets/Prefabs/Models/Obstacles/AsteroidRockMedium.prefab`, `AsteroidRockLarge` prefab
- `Assets/Art/Shaders/Vfx/NebulaFilamentVolume.shader`; `Assets/Art/Materials/Vfx/Nebula/*.mat`
- `Assets/Settings/Render Pipelines/URP-*.asset`
- `Assets/Settings/Data/Models/Map/MapGenerationSettings.asset` (`RockDensity`, `EdgeMargin`)
- `Assets/Scripts/Services/UnitExplosion/HardPointExplosionPool.cs`, `Assets/Scripts/Components/ViewComponents/Health/ExplosionVfx.cs`

## Current Execution — 2026-10-29

- User instruction: no builds or similar execution. Do not resume historical Player builds, launch Play Mode, run tests, or collect new performance measurements in this execution.
- Source review confirms Phase 0 tool and Phases 1–6 implementations already exist; no duplicate code or asset changes needed.
- Rechecked: camera pin + invariant JSON + actual seed; 1,000-unit decor cells, transformed AABBs, 12,000-unit large cap, 1,000-unit LOD threshold; field LODGroups and asteroid surface material.
- Rechecked: nebula samples 48/64/96/128; Performant bloom Quarter/3/HQ off and motion blur 0; particle URP compatibility path and teardown; asynchronous fog scheduling and disposal synchronization.
- Verification this execution: source/serialized-asset inspection and scoped diff whitespace check. Existing capture numbers remain historical; no new FPS claim.
- Plan stays active: absorbed navigation attribution, effect-specific isolation, material/texture experiments and matched performance acceptance remain unfinished. Do not treat unchecked investigations as permission for speculative optimization.