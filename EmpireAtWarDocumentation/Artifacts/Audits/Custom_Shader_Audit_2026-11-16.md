---
type: audit
system:
  - rendering
  - shaders
  - optimization
created: 2026-11-16
verified: 2026-11-16
record_state: dated-audit
verification: Static shader/HLSL/graph review, material bindings, selected
  callers/render settings and test assertions; no edits, Unity compilation,
  tests, rendered acceptance or GPU profiling.
finding_status: open
---
# Custom Shader Audit — 2026-11-16

## Goal

- Review correctness, usage and optimization opportunities in every project-owned shader.
- Retain per-shader proposals and source evidence; this audit does not authorize or implement fixes.

## Important Values

- Scope: `Assets/Art/Shaders/` — 36 handwritten shaders (35 originally + untracked `DecorAsteroid.shader`, added in gap pass), 4 Shader Graphs, 1 compute shader, 11 shared HLSL files.
- Environment: Unity `6000.4.7f1`, URP `17.4.0`; inspected renderers use Forward / Forward+.
- ShipLit: 1,457 serialized materials; ShipWreck: 833; PlanetLit: 28; PlanetClouds: 5.
- Counts reflect the working tree inspected on 2026-11-16, including existing uncommitted assets; not a clean commit snapshot.
- Vendor shaders under `Assets/ThirdParty` and `Assets/Plugins` were outside the custom-shader audit.
- Verification: shader/HLSL/graph source, saved material bindings, relevant runtime callers, render settings and selected test assertions.
- No shader modifications, Unity compilation, tests, rendered acceptance or GPU captures performed. All performance savings remain unmeasured.

## Decision

- First fix: disabled flash in `SpaceHazard.shader` evaluates invalid arithmetic.
- First optimization experiments: skip pre-dissolve wreck noise; specialize hazard ion/non-ion work; precompute shield impact constants.
- Next: beam/seismic uniform calculations, cinematic nebula cost, planet detail and transparent coverage.
- Keep unreferenced shaders low priority. No material/runtime-name reference found does not prove exclusion from every external workflow or build.
- Avoid speculative rewrites, broad shader warmup, and blanket replacement of existing blending/batching behavior.
- This is a dated audit, not an execution plan. Execution order lives in [[TODOs/Optimization/Custom_Shader_Fixes_Plan]] (created 2026-11-16, backlog entry added); no other plan was changed.

## Implementation

### Confirmed arithmetic defect: disabled hazard flash

- Evidence: `Assets/Art/Shaders/Vfx/SpaceHazard.shader:89` computes `_Flash.w * exp(-dot(toFlash,toFlash) / (_Flash.z * _Flash.z))`.
- Caller: `Assets/Scripts/Components/Environment/HazardCloudView.cs:68` initializes `_Flash = Vector4.zero`; line 176 clears it to zero after a flash.
- Disabled flash → radius² = 0; at world XZ `(0,0)` → `0/0`. Multiplication by zero afterward does not reliably eliminate NaN.
- Confidence: invalid arithmetic confirmed from source. Visible origin artifacts depend on fragment coverage and GPU/compiler behavior; not observed in a rendered capture.
- Proposed fix: initialize flash contribution to zero; evaluate the Gaussian only when `_Flash.w > 0`. Enabled runtime flashes already supply positive radius `200–400`.
- Benefit: removes the invalid disabled path and unnecessary disabled-flash exponential calculation.
- Verification needed after implementation: disabled flash/origin contract coverage and a rendered hazard crossing the origin with flashes off.
- Correction retained: inspecting only the shader default radius `1` or enabled flash radius misses the controller's zero-reset path.

### Ships, planets and markers

Source paths are repository-relative. Line numbers identify the inspected snapshot.

| Shader / usage | Finding | Proposed solution / constraints |
|---|---|---|
| `Assets/Art/Shaders/Units/ShipLit.shader` — 1,457 materials; SharedShaders Addressables entry | Medium-priority candidate: six texture samples in `Units/ShipLitForwardPass.hlsl:95–114`; HSV livery detection still evaluated when strength is zero in `Units/ShipLitInput.hlsl:159–176`. | Benchmark a material-uniform early-out for disabled livery. Consider baked livery masks and compatible channel packing. Preserve shared-material behavior; no measured savings. |
| `Assets/Art/Shaders/Units/ShipWreck.shader` — 833 materials; wreck prefabs and UnitWreckView | High-priority candidate: `Units/ShipWreckDeformation.hlsl:140–145` evaluates two 8-corner value noises throughout lifetime, before checking dissolve progress; shared by forward/shadow/depth/normals passes. | Calculate progress first; after mandatory appearance/part clipping, return zero dissolve-edge contribution when progress is zero. Keep pass deformation/clip math identical. Precomputed per-part transforms for repeated rotations at lines 65–98 are a secondary candidate; compare baked-noise texture versus ALU only after measuring. |
| `Assets/Art/Shaders/PlanetLit.shader` — 28 materials; Coruscant enables city detail | `PlanetCityDetail.hlsl:14–42`: world→object conversion, inverse rotation, atan2/asin and three triplanar detail samples per fragment. Broad variants at shader lines 104–148. | Pass undeformed object position from vertices, preserving seam gradients. Benchmark distance-based detail reduction. Inspect actual build stripping before narrowing variants. |
| `Assets/Art/Shaders/PlanetClouds.shader` — 5 transparent materials, ZWrite off | Broad lightmap/decal/probe/lighting variants at lines 75–110; large transparent shell coverage. No confirmed active defect. | Inspect stripped variants and overdraw before simplifying lighting. Existing shadow/depth passes are not evidence that those passes actually execute. |
| `Assets/Art/Shaders/PlanetAtmosphere.shader` — one shared material, multiple planet prefabs | Lines 64–87: inverse transform, normalization, sqrt, pow and exponentials per fragment. | Pass object-space position from vertices; move camera/light invariants only where mathematically equivalent. Profile shell coverage first. Premultiplied output at line 88 correctly matches Blend One OneMinusSrcAlpha at line 22. |
| `Assets/Art/Shaders/Units/ShipOutlinePass.hlsl` — ShipLit `ShipOutlineMask`/`ShipOutline` passes; `UnitOutlineRenderPass` | Gap-pass review. Outline vertex collapses unhighlighted/zero-width geometry before raster (46–50); w-scaled view-normal offset (55–64) is near-plane safe. Mask pass has no collapse, but the renderer list filters by `UnitOutlineRendererFeature.OUTLINE_RENDERING_LAYER` (`UnitOutlineRenderPass.cs:28–30`), so only flagged renderers draw. Globals outside UnityPerMaterial intentionally preserve shared-material batching. No confirmed defect. | Keep. Outline cost scales with highlighted hull vertex count (`Cull Off` expands both faces for open meshes); profile only for large selections. |
| `Assets/Art/Shaders/TacticalMarker.shader` — material used by WorldMarkerView prefab | Lines 21–28: already minimal unlit vertex-color shader. No confirmed defect. | Keep shader. Optimize marker visibility, geometry or submissions only if profiling identifies cost; preserve intended additive appearance. |

### Environment rendering and offline baking

| Shader / usage | Finding | Proposed solution / constraints |
|---|---|---|
| `Assets/Art/Shaders/Vfx/SpaceHazard.shader` — NebulaHazard (_Ion=0), IonStormHazard (_Ion=1) | Disabled-flash arithmetic defect above. Lines 85–90 evaluate both ion filament and non-ion sparkle sampling/math, then multiply away one result. Lines 63–66 repeat flow normalization/basis work per fragment. | Skip disabled Gaussian. Use a uniform branch or local ion keyword to skip the unused sample/power and non-ion sine work; compare compiled output and timings. Move invariant basis work to vertex/precomputed data. Preserve SDF boundary, wisp reach, trimmed geometry and opacity across sheet counts. |
| `Assets/Art/Shaders/Vfx/NebulaFilamentVolume.shader` — AzureRift, VioletPillars, EmberWings; SkirmishVolumetricNebulaClouds prefab | Lines 88–100: two 3D fetches, three-component sine and extinction per step. Quality selects 48/48/64/96/128/128 steps, not always saved 128. | Keep existing normal-view impostor. Profile cinematic live rendering separately; consider conservative empty-space skipping or reduced-resolution depth-aware rendering if it dominates. Conditional far-plane limitation: exit-face proxy can disappear when exit faces cross far clip; not demonstrated in current setup. |
| `Assets/Art/Shaders/Vfx/NebulaImpostor.shader` — explicit prefab shader reference, runtime-created material | Lines 13–14,28: already one sample with matching premultiplied blending and depth test. Target owned by `NebulaImpostor.cs:28–31`: 4096×2048 ARGBHalf = 64 MiB color, plus 24-bit depth attachment. | Assess preset-dependent resolution and color-only target if render-request requirements permit. Live volume animates while impostor freezes bake phase; MatchNebula switches at 1° without blending → inspect transition popping before choosing phase synchronization, deliberate rebake or short crossfade. |
| `Assets/Art/Shaders/Vfx/NebulaStars.shader` — StarMaterial, active SkirmishVfx prefab | Lines 29–31: four exponential terms per covered fragment; no concrete defect. | Compare a small linear single-channel star sprite only if GPU capture attributes appreciable cost. Preserve additive intensity and vertex alpha. |
| `Assets/Art/Shaders/Vfx/SpaceHazardArc.shader` — hazard arc renderer | Line 24: fragment returns vertex color; no actionable shader defect. | Keep simple. Target line geometry/submissions/lifetime rather than introducing shader machinery. |
| `Assets/Art/Shaders/Vfx/MapSpaceDust.shader` — MapSpaceDust prefab and explicit MapLayoutView reference | Already collapses invisible cards at lines 74–75; correct perspective/orthographic depth handling at 88–93. Visible specks still sample scene depth. | Consider bypassing depth for tiny specks while retaining it for wisps, only if hull-intersection quality permits. Benchmark branch versus separate draws. Do not add blanket alpha clipping. |
| `Assets/Art/Shaders/Vfx/FogOfWar.shader` — SceneContext; grid size 11, gap .12, alpha .03 | Lines 70–76: increasing fwidth widens smoothstep; at strong minification filled-cell coverage tends toward zero, not its average. Source-confirmed behavior; visual intent uncertain. | If opacity should remain stable with zoom/resolution, use analytically filtered periodic coverage or converge toward average coverage (1-gap)². Do not treat the effect as a rendered regression without acceptance. |
| `Assets/Art/Shaders/DecorAsteroid.shader` — untracked; `RockyPack_Decor.mat`; drawn by `DecorAsteroidRenderer` via `Graphics.RenderMeshInstanced` (511/batch, per-instance `_NeighborOcclusion` MPB array, shadows/receive off, `LightProbeUsage.Off`) | Gap-pass review. Three samples (albedo/normal/AO) + unshadowed main-light wrap diffuse + SH ambient. `LightProbeUsage.Off` still supplies the scene ambient probe (Unity 6000.6 docs) → ambient term valid. Forward pass compiles `_CLUSTER_LIGHT_LOOP` (line 150) but evaluates only `GetMainLight()` → probable redundant Forward+ variant. No ShadowCaster pass, consistent with `ShadowCastingMode.Off`. DepthNormals reuses full `Vert` (fog/UV work unused). | Inspect compiled variants; drop `_CLUSTER_LIGHT_LOOP` if output is identical and Forward+ rendering stays correct. Optional slim DepthNormals vertex. `ao *= ao` after strength lerp makes `_OcclusionStrength` nonlinear — confirm intended. No confirmed defect. |
| `Assets/Art/Shaders/Vfx/HazardNoiseBake.shader` — HazardNoiseBuilder finds by name; offline | Lines 53–57: four Fields evaluations with FBM; seam blending appears correct. | Leave unless regeneration time matters. Periodic noise could reduce bake time but changes art; not a battle-FPS optimization. |
| `Assets/Art/Shaders/Vfx/NebulaFilamentBake.compute` — Tools/Nebula/BuildNebula.cs | Bounds guard at line 70; x-major packing at 103 matches builder. Eight six-octave FBM calls per voxel; 512×320×48 voxels per variant. | Optimize only bake throughput: profile group sizes, repeated coordinates and provably conservative coarse rejection. Builder synchronous readback is offline, not runtime frame cost. |

### Combat effects and zone overlays

| Shader / usage | Finding | Proposed solution / constraints |
|---|---|---|
| `Assets/Art/Shaders/Vfx/ShipShield.shader` — ships/stations/mining prefabs and Shield.cs | High-priority candidate: EvaluateImpact lines 46–59 repeats age/fade/radius reciprocals/wave travel per vertex and fragment; fragment loops up to 8 impacts at 83–87. | Upload precomputed per-impact coefficients through existing updates; inspect compiled instructions. Preserve CPU expiration, zero-impact renderer disable and expanded bounds already present in Shield.cs:155–197. Reduced impact-count quality variants require capture evidence. |
| `Assets/Art/Shaders/Vfx/HeavyLaserBeam.shader` — LaserProjectile prefab; BeamVisuals | Age/fade-only width/flicker/fade smoothing/ignition at lines 46,52,54,57. Narrow powered pulse at 51 lacks footprint filtering. | Move uniform results to existing property update or vertex stage; retain spatial flow and derivatives in fragments. Attenuate unresolved pulses if distant shimmer appears. |
| `Assets/Art/Shaders/Vfx/SeismicCharge.shadergraph` — HarmonicBomb prefab | Custom function line 1438 always computes atan2, two sines and ring smoothsteps. HarmonicBombView:100–105 sets Core exactly 0/1; core EdgeNoise=0. Ring width reaches .012, secondary .0066 (caller:88–90), without derivative AA. | Uniform core/ring branch skips unused ring work. Add derivative-aware coverage/energy compensation if zoom shimmer is observed; no rendered defect claimed. |
| `Assets/Art/Shaders/Vfx/TractorBeam.shader` — TractorBeamView prefab | Lines 38–45: tapered envelope, narrow powered bands, wisps and exponentials across line quad. | Consider tapered multi-segment ribbon with correct along-coordinate UVs; preserve existing two-point UV-skew constraint. Filter subpixel bands; LUT/profile texture is a secondary measured tradeoff. |
| `Assets/Art/Shaders/Vfx/TurbolaserBolt.shader` — Dby827Projectile prefab | Lines 59–68: mask plus scene depth sample; world→view depth transform at 66 per fragment. | Calculate/interpolate eye depth from vertices; retain world position where fragment fog needs it. Main candidate is coverage/count. Perspective/orthographic conversion already correct; all six URP presets request depth. |
| `Assets/Art/Shaders/Vfx/IonField.shader` — IonStunField prefab | Lines 46–50: four sin evaluations, pow(...,18), rim normalization; narrow bands lack footprint filtering. | Filter distant bands. Consider scrolling baked profile at lower quality if measured expensive. Preserve float position precision; inspect Metal output before lowering intermediate precision. |
| `Assets/Art/Shaders/Vfx/IonGlow.shader` — IonArcGlow, IonPulseGlow, IonImpactFlash, MuzzleFlash, HeavyBeamFlare | Lines 39–43: compact radial shader; no clear defect. | Tighten particle geometry/size and distant emitter workload first. Optional uniform branch for Roundness=0 only if compiler output shows savings. Constant-power algebra replacement is not guaranteed faster. |
| `Assets/Art/Shaders/Vfx/ShipCloak.shader` — StealthCorvetteShipView; CloakView | Lines 54–57: high-frequency world-Y shimmer. ZWrite Off can accumulate opacity across overlapping hull faces. | Attenuate unresolved shimmer. Verify self-overlap before adding costly depth prepass. Friendly substitution/enemy fog hiding already handled by caller. |
| `Assets/Art/Shaders/Vfx/ConstructionReveal.shader` — mining/defence structure prefabs | Binary construction step at line 60 can alias despite smooth color band at 61. | Apply derivative-width boundary blending if visual validation shows flicker. Full transparent unbuilt preview at 67–69 is intentional; clipping it changes design. |
| `Assets/Art/Shaders/Vfx/SpawnArea.shader` — FogOfWar/SpawnArea material | Lines 66–80: one sample and derivative grid; state-zero pixels still do grid work. | Prefer tighter populated overlay geometry/tile skipping over extra fragment branches if cost matters. Preserve mask interpolation and AA. No bad active GridSize established. |
| `Assets/Art/Shaders/Vfx/CaptureZoneGrid.shader` — CaptureZoneGrid, RelayZoneGrid materials | Compile-time variants already separate shape paths. Detailed grid/atan2 over square coverage at lines 92,108. | Tighter circular geometry can reduce empty fragments. Preserve existing square-grid distant fade (83–84) and stencil bit 4 exclusion. |
| `Assets/Art/Shaders/Vfx/ReinforcementZone.shader` — reinforcement zone material | Depth sample at lines 90–99 needed for fill, not border/outline. | Benchmark skipping it outside interior versus branch/cache cost; consider tighter coverage. Projection handling is correct. DepthFadeDistance default 8; no bad active zero value established. Preserve stencil bit 4. |
| `Assets/Art/Shaders/Vfx/ParticleLegacyUnlit.shader` — 10 materials, including smoke/fire/shockwave | Conditional defect at line 53: soft-particle LinearEyeDepth assumes perspective. Main battle camera is perspective; scanned materials do not enable SOFTPARTICLES_ON. | Before orthographic soft-particle use, adopt projection-aware conversion from TurbolaserBolt. Preserve intentionally legacy-compatible premultiply at line 58. Optimize coverage/variant retention only after usage capture. |

### UI

| Shader / usage | Finding | Proposed solution / constraints |
|---|---|---|
| `Assets/Art/Shaders/Ui/SquadronFrame.shader` — shared marker frame material | Line 66: per-fragment frame-coordinate division on current full rectangular mesh. No confirmed active defect. | Normalize coordinates in vertex stage. Preserve UV1 batching and alpha-as-selection encoding. Low priority; not a generic fade shader. |
| `Assets/Art/Shaders/Ui/SquadronIcon.shader` — shared icon material | Lines 62–63: affine UV scale/offset per fragment; fixed sqrt-based color adjustment at 67. | Move UV transform to vertices. Bake color adjustment only if measured worthwhile and source assets are dedicated to this use; preserve alpha silhouette and out-of-range masking. |
| `Assets/Art/Shaders/Ui/MiniMapFog.shader` — MiniMapFog material | Lines 91–97: same minification behavior as world fog, but affects cell tint rather than base fog opacity. | Filter average cell coverage if stable tint across resolution is desired; otherwise retain current appearance. |
| `Assets/Art/Shaders/Ui/SelectionBrackets.shader` — shared selection material | No confirmed defect. DisableBatching=True at line 12 supports object-transform-dependent world alignment. | Keep batching flag and screen-pixel stroke calculations at line 46. Removing the flag is not a safe generic optimization. |
| `Assets/Art/Shaders/Ui/IconShadow.shader` | No serialized material or runtime-name usage found; line 78 is a minimal alpha-mask fragment. | No worthwhile shader optimization identified. Keep low priority unless usage is restored. |

### Legacy or unreferenced paths

| Shader / usage | Finding | Proposed solution / constraints |
|---|---|---|
| `Assets/Art/Shaders/Vfx/VolumetricNebulaRaymarch.shader` — legacy VolumetricNebulaSystem material/prefab; battle reachability unproven | Lines 148–159: attenuation uses exp(-density), accumulation uses density rather than 1-exp(-density). Default/material SrcAlpha One multiplies already weighted RGB by alpha again. Step .01 is capped to 30 steps (113–118); time-varying jitter at 121 can shimmer without accumulation. | Define intended physical-alpha versus additive contract. For physical compositing use Beer–Lambert sample opacity, premultiplied accumulation and Blend One OneMinusSrcAlpha; for additive apply intended fade once. Use stable noise or supported temporal integration. Low priority until revived. |
| `Assets/Art/Shaders/Vfx/NebulaCloudVolume.shader` — no serialized GUID/runtime-name consumer found | 24–96 steps, two 3D samples per density evaluation plus second density evaluation for lighting: up to four fetches per occupied step. Entry-face selection at 105–107 can leave holes if near plane clips entry faces while camera remains outside. | Prefer maintained baked-field approach if restored. Define robust exit-face/depth-clamped proxy bounds. Do not optimize unused path first. |
| `Assets/Art/Shaders/Vfx/NebulaBillowVolume.shader` — no consumer found | One 3D fetch per step, 32–128 iterations, three exponentials per occupied sample. Exit-face far-plane limitation. Missing _Baking bypass for current bake camera with depth generation disabled. | Profile step count/skipping if revived; define bake depth contract before connecting to MatchNebula. |
| `Assets/Art/Shaders/VolumetricNebula.shader` — no material/runtime-name consumer found | Lines 42–45,63–67: ignores _Color.a and _DetailTex_ST; unbounded alpha despite straight-alpha blending. Envelope uses transformed UV at 51,57, so tiling distorts/moves falloff. | Apply color alpha and detail transform, saturate opacity, separate mesh-envelope UV from texture UV. Put material constants in UnityPerMaterial if revived. |
| `Assets/Art/Shaders/VolumetricShader.shadergraph` — no consumer found | Transparent two-sided procedural Simple Noise at line 1239; gradient and remapped constant alpha; shadow-casting setting at 600. | Prefer baked noise and explicit opacity if revived; verify need for casting. Generated shader/render output was not validated; no compile defect claimed. |
| `Assets/Art/Shaders/PlanetAutodeskInteractiveNoShadows.shadergraph` — no consumer found | Six texture nodes, five runtime Boolean Branch nodes. Lines 5097–5098 disable receiving shadows but retain casting. PlanetRotation function at 5518. | Keep retired or migrate to maintained PlanetLit. Name alone does not establish shadowless behavior; node counts are not measured GPU cost. |
| `Assets/Art/Shaders/PlanetAutodeskInteractiveMaskedNoShadows.shadergraph` — no consumer found | Six samples/six Boolean branches; alpha clipping at 1075. Casting true/receiving false at 1076–1077. Unlike companion, no PlanetRotation custom function. | Add equivalent position/normal/tangent rotation or migrate before reuse for rotating planets. Keep retired otherwise. |

## Edge Cases

- Filament volume inverse-ray, local-length and depth formulas account for projection and nonuniform scale; no defect established there.
- Active filament placement: local position `(0,-69000,48000)`, scale `(85000,60000,22000)`; battle camera far clip `200000`. Exit-face far-plane limitation is conditional, not a demonstrated current failure.
- Impostor already bakes only after nebula enable and reuses its result. Do not propose adding an impostor or stopping a nonexistent per-frame bake.
- All 1,457 ShipLit material BaseMap/BumpMap filter/wrap/aniso settings matched in the audit. Different sampler structure between forward/depth normals is not a current sampler defect.
- Current renderers use Forward/Forward+ and accurate GBuffer normals are disabled. Missing oct-encoded normals is not established as a present bug.
- Coruscant city-detail material has no normal/metallic textures and smoothness zero. Potential future UV differences do not establish a current rendering defect.
- Source multiplication by zero does not guarantee elimination of unused texture/transcendental work; inspect compiled code before claiming savings from a proposed branch.
- Fewer variants can improve SRP batch continuity; local specialization can reduce fragment work but increases variants. Measure both tradeoffs.
- SRP Batcher material properties belong in UnityPerMaterial; MaterialPropertyBlock renderers have compatibility constraints. Do not remove required per-instance data or change batching blindly.
- Do not restore broad `Shader.WarmupAllShaders()`; existing project guidance records keyword-space errors. Targeted warmup requires exact variant/render-state evidence.

## Validation Pass — 2026-11-16

- File inventory re-listed: 36 `.shader`, 4 `.shadergraph`, 1 `.compute`, 11 `.hlsl`. Gaps found → filled: `DecorAsteroid.shader` (untracked, created after original pass) and `ShipOutlinePass.hlsl` (counted, not reviewed).
- Re-confirmed: `SpaceHazard.shader:89` flash Gaussian; `HazardCloudView.cs:68,176` zero `_Flash`; `ShipWreckDeformation.hlsl:140–145` noise before progress; material counts ShipLit 1,457 / ShipWreck 833 / PlanetLit 28 / PlanetClouds 5 (GUID grep of `.mat`).
- Rejected hypothesis: `LightProbeUsage.Off` does not zero SH ambient for `RenderMeshInstanced`; docs state the ambient probe is provided.
- Still static review only: no compile, tests, render or GPU capture.

## Validation Pass 2 — 2026-11-16

- Scope: every handwritten `.shader`, `.hlsl` and the `.compute` under `Assets/Art/Shaders/` re-read line by line. Inventory 36 / 4 / 1 / 11 confirmed; no project-owned shader files outside this folder (`Assets`, `Packages`).
- All cited line ranges in the tables match live source, except: `Ui/IconShadow.shader` fragment is lines 76–84 (alpha mask at 79, not 78).
- Shader Graph references confirmed: `SeismicCharge` custom function at 1438; `VolumetricShader` Simple Noise at 1239, cast shadows at 600; `PlanetAutodeskInteractiveNoShadows` 5097–5098 and 5518, 6 samples / 5 branches; Masked variant 1075–1077, 6 samples / 6 branches, no rotation function.
- Material counts by shader GUID: ShipWreck 833, PlanetLit 28, PlanetClouds 5, ParticleLegacyUnlit 10 unchanged. ShipLit is now 1,465 (audit recorded 1,457; working tree gained 8).
- No-consumer claims confirmed by GUID search of `.mat`, `.prefab`, `.unity`, `.asset`: `IconShadow`, `NebulaCloudVolume`, `NebulaBillowVolume`, `VolumetricNebula`, `VolumetricShader`, both `PlanetAutodesk*` graphs → 0 references. `VolumetricNebulaRaymarch` → 1 material.
- C# callers confirmed: `Shield.cs:155–197`; `NebulaImpostor.cs:28–31` (4096×2048 ARGBHalf, 24-bit depth); `MatchNebula.cs:77–83` samples 48/48/64/96/128/128, bake override 128 at 91, 1° switch at 106; `UnitOutlineRenderPass.cs:28–31`; `DecorAsteroidRenderer.cs:12,160–165`; `HazardCloudView.cs:68,176`, flash radius 200–400 at 226.
- Line drift: `HarmonicBombView` sets Core 0/1 at line 106 and EdgeNoise at 107 (audit cites 100–105); ring width at 93 (audit cites 88–90). Values `.012` and `.0066` confirmed.
- Still static review only: no compile, tests, render or GPU capture.

### Corrections

- `SpaceHazard.shader:89` NaN scope: `0/0` occurs only where `dot(toFlash,toFlash)` is exactly 0. Elsewhere `-x/0 → -inf → exp → 0 → 0 * 0 = 0`. Defect stays confirmed; visible impact is at most isolated fragments at world XZ `(0,0)`.
- `ShipWreckDeformation.hlsl:140–145` skip is exact, not approximate: `WreckValueNoise ≥ 0`, so at `progress == 0` → `remaining = noise ≥ 0` → `clip` never discards and the function returns `0`. Early return before the noise changes no pixel.
- `DecorAsteroid.shader` DepthNormals: only the fog factor is unused. UV is required by `SurfaceNormal` (line 79).

### New findings

| Shader | Finding | Proposed solution / constraints |
|---|---|---|
| `Units/ShipWreckForwardPass.hlsl:130` | Wreck front faces add `GetTeamRim(...) * heat`; `ShipLit` never calls `GetTeamRim`. Wreck default `_TeamRimStrength` 0.6 (`ShipWreck.shader:29`) → team-coloured rim appears at the ship→wreck swap, contradicting the header claim of an identical look. Serialized values across 833 wreck materials: 549 at `0`, 270 at `0.6`, 14 at `0.3` → 284 wrecks gain a rim the live ship never had. | Decide: zero the rim on those 284 materials (or drop the call) to match the ship, or accept it as a deliberate wreck cue. Visual acceptance needed. |
| `Units/ShipLitInput.hlsl:46–47` | `_TeamRimStrength` / `_TeamRimPower` sit in the shared `UnityPerMaterial` buffer but `ShipLit.shader` has no matching Properties and never reads them. | Harmless for batching. Remove only together with the wreck rim decision above. |
| `Units/ShipWreck.shader:69–104` | Forward pass writes no stencil bit 4 (`ShipLit.shader:74–80` does). `CaptureZoneGrid` / `ReinforcementZone` test `NotEqual 4` → zone graphics draw across wreck hulls where the zone plane cuts them. | Confirm intent. If wrecks should mask zones, copy the stencil block; no cost. |
| `PlanetCityDetail.hlsl:9,31–34` | With `_PLANET_CITY_DETAIL`, `InitializeStandardLitSurfaceData` samples `_BaseMap` and `_EmissionMap` at mesh UV, then both are re-sampled at sphere UV and the first colour results are overwritten. Up to 2 discarded samples per fragment on top of the 3 triplanar samples. | Only alpha from the first base sample is used. Skip the mesh-UV colour/emission fetch in the city-detail path; inspect compiled output first. |
| `PlanetLit.shader:158–160` | Macro redirect depends on URP `LitForwardPass.hlsl` naming its fragment input `input` and calling `InitializeStandardLitSurfaceData(uv, surface)`. | URP upgrade risk, not a current defect. Recheck on every URP version change. |
| `Vfx/ParticleLegacyUnlit.shader:26` | `#pragma multi_compile _ SOFTPARTICLES_ON` always compiles both variants ×3 legacy blend modes × fog. `ParticleRecoveryBuilder.cs:154` can enable the keyword per material. Checked all 10 materials: 0 enable it (3 `_LEGACY_PREMULTIPLY`, 2 `_LEGACY_ADDITIVE`, 5 none) → the soft-particle branch, including its perspective-only depth conversion at line 53, is unused in current content. | Switch to `shader_feature_local_fragment` so unused soft variants strip. Fix the orthographic conversion in the same edit so the branch is correct if a material enables it later. |
| `Ui/SelectionBrackets.shader` | Built-in `CGPROGRAM`, no `UnityPerMaterial`, `DisableBatching` → one draw per bracket, not SRP-batched. | Expected for the world-aligned design. Revisit only if large selections show draw-call cost. |
| `Vfx/SpaceHazard.shader:63` | `normalize(_FlowDirection.xy)` → NaN across the whole sheet if a caller ever passes a zero vector. Both materials store `(1,0)`. `HazardCloudView.cs:51,62` overrides it with the XZ of `shape.Spot.Frame.Along`; zero only if that vector is vertical or zero. `Frame.Along` source not traced. | Low risk. Confirm the hazard frame always has a horizontal `Along`. |
| `Vfx/ShipShield.shader` + `Shield.cs:172–191` | While any impact is live, `UpdateMaterial` runs every `LateUpdate` and uploads 10 properties by string name, including per-shield constants (`_ShieldColor`, `_Brightness`, `_FadeDuration`, `_DisplacementRatio`). The property block also keeps shield renderers out of the SRP Batcher. | Belongs with the planned impact-coefficient precompute: cache property IDs, set constants once, upload only time/impacts per frame. CPU cost unmeasured. |
| `Units/ShipWreck.shader:82` | `_ALPHATEST_ON` is enabled on 2 of 833 wreck materials → a second forward/shadow/depth variant set exists for 2 materials. | Check whether those 2 wrecks need alpha clipping; if not, clear the keyword to keep one variant. |

## TODO

- Execution order and status → [[TODOs/Optimization/Custom_Shader_Fixes_Plan]].
- Not traced in pass 2: `HazardShape.Spot.Frame.Along` source; which 2 wreck materials enable `_ALPHATEST_ON`.

- Decor asteroid: compare compiled Forward variants with/without `_CLUSTER_LIGHT_LOOP`; affected test area `Map` (`Components/Environment`).
- Fix disabled SpaceHazard flash evaluation; verify source contract and rendered origin behavior.
- Measure wreck pre-dissolve noise skip, ion/non-ion specialization and shield coefficient precomputation in matched workloads.
- Validate fog minification, thin effect bands, construction edge and nebula transition appearance before changing their visual contracts.
- Future test areas: Combat, Health, Map, Abilities, BattleInput, Squadrons, Capture and Ui; MapSpaceDust has its own fixture category.
- Relevant fixtures: `BeamVisualsTests`, `ShieldSurfaceClipParityTests`, `HarmonicBombTests`, `WreckCutPlanTests`, `WreckMaterialSyncTests`, `MatchNebulaTests`, `SpaceHazardPresentationTests`, `MapSpaceDustTests`, `PlanetObstaclePresentationTests`, `PlanetCatalogTests`, `MapPlanetRosterTests`, `SelectionMarkerTests`, `SquadronIconPrefabTests`, `TacticalMarkerRegistryTests`, `SpawnAreaGridModelTests`.
- Existing binding/shader-error assertions do not establish rendered correctness, minification quality, transition acceptance or GPU savings. No tests were edited or run in this review.
- Compare matched Development Player captures with fixed hardware/API, camera/resolution, quality, map/nebula, population and effects. Report frame-time distributions and actual GPU attribution.

## Files

- `Assets/Art/Shaders/` — audited shaders and shared HLSL; subordinate shader include paths in tables resolve under this directory.
- `Packages/manifest.json`, `ProjectSettings/ProjectVersion.txt`, `Assets/Settings/Render Pipelines/` — inspected engine/package/render settings.
- `Tools/Jenkins/TEST_INVENTORY.md` — fixture inventory; local test-execution restriction remains in force.
- [[Rules/BATTLE_PERFORMANCE_GUIDELINES]] — performance constraints, particle compatibility, capture protocol and warmup history.
- [[Rules/DOCUMENTATION_ORGANIZATION]] — dated audit scope and verification semantics.
- [Unity SRP Batcher](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher.html) — variant/batching tradeoffs.
- [Unity Graphics SRP Batcher compatibility](https://github.com/Unity-Technologies/Graphics/blob/master/Packages/com.unity.render-pipelines.high-definition/Documentation~/SRPBatcher-Materials.md) — constant-buffer and renderer compatibility requirements; documentation consulted via Context7.
