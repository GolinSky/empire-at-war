---
category: Optimization
status: done
created: 2026-10-30
completed: 2026-10-30
acceptance: pending-user
---
# Mac GPU Frame Budget Plan

## Goal

- Implement the five trace-based fixes in user order; no new measurement phases.
- Parent: [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan]].
- Historical targets: M2 hazard GPU p95 ≤16.7 ms, median ≤10 ms. Savings remain estimates.

## Implementation

1. [x] Camera FXAA disabled in `Main Camera.prefab`; existing MSAA settings unchanged (Performant 2×; other presets currently off).
2. [x] Mac defaults: `GraphicsSettingsData` starts at 77% / FSR 1; PC stays 100% / Bilinear. Serialized saved values overwrite defaults; player controls unchanged.
3. [x] Hazard sheets retain only grid cells with a contributing RHalf corner sample ≤90 m reach. Bilinear sample mapping, old outer bounds, winding and shader remain unchanged; holes and narrow arms conservatively covered. UInt32 indices supported for large meshes.
4. [x] `BattleStartupSequence` yields a covered frame after battle preloads/fog reveal before fade-out. Broad `Shader.WarmupAllShaders()` was removed after reproducing 28 native keyword-space errors; fresh/restored Windows Play Mode startup now reports 0 new errors. Correction → [[Done/Bugs/Shader_Warmup_Keyword_Errors]].
5. [x] Mac-hosted macOS builds write build-info JSON, ad-hoc sign with `codesign --force --sign -`, then verify with `--verify --deep --strict`; command failure fails the build.

## Important Values

- Trace: 2026-10-29, commit `2aa218c52`; source `C:/Users/golin/Documents/Mac-GPU-Audit-2026-10-29/20261029-Mac-GPU-Profiling/hazard-summary.json`.
- 2940×1846 =5.43 M px; 77% =3.22 M px (−41%). Historical Fragment 11.55 / GPU 12.42 ms median; p95 17.90 ms.
- Final post-processing 1.93 ms; `RenderLoop.Draw` 3.41 ms; one 452 ms hitch.
- Estimated savings: resolution 3–4 ms; FXAA 0.5–1 ms. Not measured after implementation.

## Edge Cases

- Existing saved Mac settings retain their values; select 77% and FSR 1 manually if desired.
- Covered-frame rendering prepares currently rendered content only; offscreen/future variants and Metal pipeline states may still hitch. Shader compilation as the historical 452 ms cause is unconfirmed.
- Automatic codesign runs only on macOS hosts; Windows cannot execute or verify it.
- `HazardCloudView` already exceeded 200 lines; focused mesh extraction can be considered separately. No unrelated refactor.
- No new traces, measuring phases, commit or build requested for this execution.

## Verification

- Source-reviewed settings serialization, conservative bilinear/RHalf coverage, shader reach, loading cover and signing order.
- Unity imported/saved camera prefab; read-back FXAA =0. Final Unity compilation idle, `scriptCompilationFailed=false`; Console returned 0 errors (cursor 57). `git diff --check` passed.
- Tests not run: checkout execution override. No Play Mode, Mac build, codesign execution or performance capture.
- Follow-up 2026-10-30: [[Done/Bugs/Shader_Warmup_Keyword_Errors]] removed broad warmup; `GameFlow` 153/153 passed before/after, `Combat` 220/220 and `Capture` 101/101 passed; fresh/restored Windows Play Mode startup 0 new errors. Mac acceptance remains open.
- Reusable rules updated in [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].
- User acceptance → [[TODOs/User_Acceptance_Queue]]: Mac image/edge/hazard appearance, loading behavior and next Mac build signature.

## Files

- `Assets/Prefabs/View/Camera/Main Camera.prefab`
- `Assets/Scripts/Services/Settings/GraphicsSettingsData.cs`
- `Assets/Scripts/Components/Environment/HazardCloudView.cs`
- `Assets/Scripts/Entities/CoreGame/BattleStartupSequence.cs`
- `Assets/Scripts/Editor/Versioning/BuildInfoWriter.cs`
