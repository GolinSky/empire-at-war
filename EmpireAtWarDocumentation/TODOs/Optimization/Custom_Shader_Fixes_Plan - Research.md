---
category: Optimization
status: in-progress
created: 2026-11-16
---
# Custom Shader Fixes — Research

## Decision

- Target: Windows / Direct3D11 on this PC.
- Phase 1–2 committed as `ecb8bf37a`; no Phase 3 candidate accepted or installed in production.
- User instruction after startup failure: continue fixes without tests or another build. Measurements remain deferred.
- Fixed seed alone does not make combat deterministic; paused captures cannot establish shield/beam impact savings.

## Verification

- Two Windows Development IL2CPP builds succeeded: `build_423fb9855b7e` (519725 ms) and instrumented `build_dc995a6a6091` (411801 ms). Neither executed tests.
- Output: `Builds/CustomShaderBaseline/EmpireAtWar.exe`; second build overwrote the first. Working tree included unrelated changes; provenance snapshots and reports are under `Logs/CustomShaderFixes/`.
- Instrumented report also collected unrelated CLI command/time-out errors; build status was Succeeded.
- D3D11 Player launched at 1920 × 1080. Battle startup failed in `CheatPresenter.BuildShipCatalog`: Tector exists in more than one faction. No valid baseline or matched GPU capture obtained.
- Temporary Sandbox command harness and runtime Pipeline configuration removed; generated Addressables state restored. Scratch candidates remain only in ignored `Logs/CustomShaderFixes/`.

## Implementation

- Startup fix: preserve shared roster entries; cheat ship requests resolve data from the selected faction, while Spawn All uses its requested faction.
- Candidate ideas retained for future measurement: livery zero-strength branch, independent ion/non-ion branches, HeavyLaserBeam uniform vertex terms, PlanetAtmosphere object-position varying.
- SeismicCharge graph function has no age input; only core/ring endpoint branches are applicable.
- CityDetail uses URP-owned varyings; avoid copying a whole URP pass for one transform.
- Shield CPU property IDs/constants and GPU impact coefficients require separate experiments; preserve impact lifecycle and CPU surface-clip parity.

## TODO

- After builds/captures are authorized again, obtain a fresh baseline including the startup fix and compare original → candidate → restored shaders with the same scene, camera, resolution, quality and population.
- Accept only gains beyond run variation with no visual regression. No performance gain claimed.
- Owner: [[TODOs/Optimization/Custom_Shader_Fixes_Plan]].
