---
category: Optimization
status: in-progress
created: 2026-11-16
tags:
  - optimization
  - plan
  - rendering
  - shaders
---
# Custom Shader Fixes Plan

## Goal

- Act on the verified findings of [[Artifacts/Audits/Custom_Shader_Audit_2026-11-16]] in a safe order.
- Fix source-confirmed defects first; treat every optimization as an experiment that needs a measurement.

## Decision

- Chosen: three phases — exact fixes, user decisions, measured experiments.
- Why: phase 1 changes no visible pixel and needs no capture; phases 2–3 change appearance or need GPU evidence.
- Avoid: speculative rewrites, broad shader warmup, optimizing unreferenced shaders, claiming savings without matched captures.
- 2026-11-16: Phases 1–2 resolved below. Phase 3 remains unimplemented pending a current Player baseline and matched captures; no performance gain claimed.

## Rules

- Follow [[Rules/BATTLE_PERFORMANCE_GUIDELINES]] before any rendering or VFX change.
- Keep `UnityPerMaterial` layouts identical across passes of one shader.
- Preserve stencil bit 4 (zone exclusion) and bit 8 (outline mask) behavior.
- After each shader edit: Unity recompile, check Console for shader errors.
- Tests are not run locally; review the listed fixtures and report them as not run.

## Implementation

### Phase 1 — Exact fixes (no visual change intended)

1. [x] `Vfx/SpaceHazard.shader:88–89` — evaluate the flash Gaussian only when `_Flash.w > 0`; flash contribution `0` otherwise. Removes `0/0` at world XZ `(0,0)` while the flash is off.
2. [x] `Units/ShipWreckDeformation.hlsl:140–145` — compute dissolve progress first; when `progress == 0` return `0` before the two value-noise calls. Exact: noise ≥ 0, so nothing is clipped at zero progress. Applies to forward, shadow, depth and depth-normals passes through the shared include.
3. [x] `Vfx/ParticleLegacyUnlit.shader:26,53` — `multi_compile _ SOFTPARTICLES_ON` → `shader_feature_local_fragment`; use the perspective/orthographic depth conversion from `TurbolaserBolt.shader:63–65`. 0 of 10 materials enable the keyword today.
4. [x] `PlanetCityDetail.hlsl` — compiled exact Coruscant D3D variant; no source edit needed. Compiler already removes overwritten mesh-UV colour/emission. Six required 2D fetches remain: sphere albedo/emission, three city-detail, mesh base alpha/smoothness. Preserve that last fetch.
5. [x] `DecorAsteroid.shader` — retain `_CLUSTER_LIGHT_LOOP`. D3D fragment outputs differ (2,082 vs 2,050 bytes); installed URP `RealtimeLights.hlsl:85–92` uses `unity_LightData.z` versus `1.0` attenuation, which Decor consumes.

### Phase 2 — Decisions and material review

1. [x] Wreck team rim: user chose to keep it (2026-11-16); shader and material values unchanged.
2. [x] Wreck stencil: user chose to hide zone markers behind wrecks. Forward pass now writes bit 4 with `WriteMask 4`; bit 8 remains untouched. In-game visual acceptance pending.
3. [x] Fog grid: user chose the current fade-away appearance (2026-11-16); both shaders unchanged.
4. [x] Two ISD III wreck materials reviewed. Disabled `_ALPHATEST_ON` only on `ISDIII_ISDII_diff_Material1_Wreck.mat`: its base texture alpha is uniformly 0, imported FromInput/DXT5, so cutoff 0.5 discards the entire surface. Retained `ISDIII_steelgirder_Material_Wreck.mat` clipping: its texture has mixed 0–255 alpha. Also cleared the stale keyword from the source hull material so wreck-material rebuilds cannot restore it. Both materials saved/read back through Unity.

### Phase 3 — Measured experiments (one variable each)

1. [ ] Shield: precompute per-impact coefficients; cache property IDs and upload constants once in `Shield.cs:172–191`.
2. [ ] SpaceHazard: skip the unused ion or non-ion sample/pow via uniform branch or local keyword; move flow basis out of the fragment.
3. [ ] ShipLit: early-out livery HSV detection when `_TeamLiveryStrength` is 0. Must keep the single shared variant.
4. [ ] HeavyLaserBeam / SeismicCharge: move age-only terms out of the fragment; core/ring branch for the bomb.
5. [ ] Planet atmosphere and city detail: pass object-space position from vertices.
6. [ ] Cinematic nebula live cost and impostor target size — coordinate with [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan]].

- Acceptance per experiment: matched Development Player capture, same seed, camera, preset and population; gain beyond run variation; no visual regression.

## Edge Cases

- `SpaceHazard` NaN only occurs where the fragment sits exactly on the flash centre; a clean capture does not prove the path is safe.
- Source `x * 0` does not guarantee the compiler removes texture or transcendental work → inspect compiled code before claiming a saving.
- A local keyword reduces fragment work but adds variants; fewer variants help SRP batch continuity. Measure both.
- `PlanetLit.shader:158–160` redirects a URP function by macro → recheck on every URP upgrade.
- Unreferenced shaders (`IconShadow`, `NebulaCloudVolume`, `NebulaBillowVolume`, `VolumetricNebula`, `VolumetricShader`, both `PlanetAutodesk*` graphs) stay untouched; deletion is a separate decision.

## Files

- `Assets/Art/Shaders/Vfx/SpaceHazard.shader`, `Assets/Scripts/Components/Environment/HazardCloudView.cs`
- `Assets/Art/Shaders/Units/ShipWreckDeformation.hlsl`, `ShipWreckForwardPass.hlsl`, `ShipWreck.shader`, `ShipLitInput.hlsl`
- `Assets/Art/Shaders/Vfx/ParticleLegacyUnlit.shader`, `Assets/Scripts/Editor/Rendering/ParticleRecoveryBuilder.cs`
- `Assets/Art/Shaders/PlanetCityDetail.hlsl`, `PlanetLit.shader`, `DecorAsteroid.shader`
- `Assets/Art/Shaders/Vfx/ShipShield.shader`, `Assets/Scripts/Components/ViewComponents/Health/Shield.cs`
- Test areas to review: `Map`, `Health`, `Combat`, `Ui`. Fixtures: `SpaceHazardPresentationTests`, `WreckCutPlanTests`, `WreckMaterialSyncTests`, `ShieldSurfaceClipParityTests`, `PlanetObstaclePresentationTests`.

## TODO

- Phase 3 target chosen by user: Windows / Direct3D11 on this PC (2026-11-16). Earlier unrelated `IVisionService.IsRevealed` compile failure cleared after refresh; latest Editor reports `scriptCompilationFailed = false`.
- Only discovered local Windows Player is `Builds/MusicReplacementWindows/EmpireAtWar.exe` dated 2026-10-22; it is not a before/after build for these changes.
- Next: build a pinned baseline, then run one-variable experiments with the acceptance protocol above. Do not retain speculative Phase 3 rewrites without measurements.
- User visual acceptance remains for flash-off origin, wreck transition/dissolve and zone masking, and orthographic soft particles.
- Rendering performance acceptance shares the capture gate of [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

## Verification — 2026-11-16

- Unity `6000.4.7f1`, Direct3D11: imported shader edits; 30 explicit D3D stage compilations succeeded with 0 messages (hazard vertex/fragment; particle 3 blend modes × soft off/on × 2 stages; wreck 4 passes × alpha off/on × 2 stages). Not a full platform/lighting-keyword sweep.
- `ShaderUtil.GetShaderMessages` reported no messages for the changed shaders. Latest C# compilation succeeded, including the new Health regression; no shader/import/serialization errors observed.
- Planet/Decor compiled inspection is D3D-only; it does not establish Metal parity or measured GPU cost. Evidence: `Logs/CustomShaderFixes/{compile-results.json,CoruscantD3D.asm,PlanetCityD3D.asm,Decor0.bin,Decor1.bin}` (local diagnostics).
- Reviewed Map: `SpaceHazardPresentationTests`, `PlanetObstaclePresentationTests`, `DecorAsteroidBuilderTests`; Health: `WreckCutPlanTests`, `WreckMaterialSyncTests`; Combat: `ShieldSurfaceClipParityTests`, `BoltParticleSimulationTests`. Existing binding/geometry/material-property contracts remain valid; no expectations weakened. Added `WreckMaterialSyncTests.IsdiiiWreck_AlphaClippingSurvivesMaterialRebuild` (Health) for the opaque hull and retained steelgirder cutout, covering saved and source-copy/reassigned-shader keywords. Ui contracts unchanged.
- Tests not run under local execution override. No render acceptance or Development Player capture performed.
- Reusable guidance extracted to [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Custom shader fixes · 2026-11-16]]. Plan stays active because Phase 3 has unimplemented scope.
