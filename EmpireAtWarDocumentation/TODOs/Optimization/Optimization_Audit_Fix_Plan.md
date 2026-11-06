---
category: Optimization
status: todo
created: 2026-11-06
tags:
  - optimization
  - plan
---
# Optimization fixes in priority order

## Decision

- **Fix capture dimensions first. Then establish which fleet hitches reach the Player.** The M2 Editor averaged 126.98 FPS over 60 s, yet six frames exceeded 50 ms and one took 721.25 ms. The longest visible interruption takes priority over increasing average FPS.
- **Start VFX work with hardpoint smoke during station destruction. Start size work with the four Providence hull textures.** The size work can begin while hitch tracing proceeds. It needs a successful same-target content layout before any import or group change.
- This note owns the order. Record implementation, measurements and checkboxes in the linked active plans. Evidence and screenshots → [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit|November 6 audit]]. Project-specific gates → [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK|Project cookbook]].

## Implementation

### P0 · Capture accuracy and fleet hitches

1. **Correct the detailed report.** Record the battle camera output and active render scale separately. Start a capture from the Editor menu. At the audited setup the report should agree with the 2560×1440 camera/PNG, with 77% recorded separately. Repeat with a resized view and a different scale. Owner → [[TODOs/Optimization/Battle_CPU_Performance_Plan|Battle CPU]].
2. **Reproduce the long frames before changing gameplay.** Compare matched M2 Editor and Development Player fleet runs with the native Profiler off and no command polling or exports during sampling. Use ≥3 × 60 s per condition. Record the actual population, orders, seed, camera, preset, scale, time scale and GC mode. Report >50 ms events/minute, worst interval, p95/p99 and collection timing. If short repeats miss rare stalls, extend the observation window and record its duration before drawing a conclusion.
3. **Decide from attribution.** For a recurring Player stall, capture the allocating thread and blocked threads in a separate instrumented repeat. Change the confirmed source, then repeat the unprofiled runs. Accept a reduction in the reproduced stall outside baseline variation without worsening the frame-time tail or combat behavior. If the stall only reproduces with Editor tooling, route the trigger there and keep the Player result explicit. A 645 ms reproduction had no collection-count change and warrants its own attribution. Owner → [[TODOs/Optimization/Battle_CPU_Performance_Plan|Battle CPU]]. [Hitch evidence](../../../Docs/Audits/2026-11-06-Battle-Hitch/analysis.md).

### P1 · Hardpoint smoke, destruction visibility and Providence textures

4. **Hardpoint smoke.** 34 off-camera emitters held 11,648 particles. Compare original → reduced off-camera emission → restored on the same destruction sequence. Record emitting/visible/draining counts, particle simulation time, render-thread cost and transparent GPU time. Keep a change only if the measured cost falls beyond run variation and effects still reappear under fog reveal or camera re-entry, drain on death and return to the pool. Owner → [[TODOs/Optimization/Battle_FPS_Recovery_Plan|Battle FPS recovery]].
5. **Station destruction readability.** Record a composited sequence at near and far zoom with the hull, nearby fighters and HUD visible. If the flash obscures tracking, trial one local effect change and restore it for comparison. Accept when reviewers can follow the craft and station outline through the burst while still identifying the destruction cue. The two audit stills establish peak overlap only. Owner → [[TODOs/Optimization/Battle_FPS_Recovery_Plan|Battle FPS recovery]].
6. **Providence import pilot.** Build and retain a successful baseline layout and catalog on the target. Pilot Albedo, Normal, Emissive and Specular individually. Keep each candidate only if Unity resolves the intended format, the matched build shrinks and the cookbook’s color/alpha/normal and close-zoom visual gates pass. Record packed bytes and Player texture memory separately. Restore each failed map. Owner → [[TODOs/Optimization/Asset_Size_Followup_Plan|Asset size follow-up]]. Build evidence → [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Addressables remediation]].

### P2 · Nebula startup, faction bundles and first spawn

7. **Nebula disabled at startup.** Measure a cold start with the saved option off. Defer creation until first needed. Verify zero `Match Nebula Bake` textures and bake submissions while disabled, a single successful first enable, correct normal/cinematic rendering and cleanup on exit. Compare cold-start and first-enable frame times to expose any displaced hitch. Owner → [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan|Mac nebula and hazards]].
8. **Five shared View entries.** Compare bundle dependencies required by a common facility and a Separatist ship. Trial the five wreck/preview moves on the same baseline, separately from texture changes. Retain moves if the common route sheds faction-only dependencies without new redundant payload, broken aliases or failed packed spawn/preview/wreck loads. Owner → [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Addressables remediation]].
9. **Cold first spawn.** In a packed Player, request a deferred ship before warmup completes, then request the same type again. Record the key, handle, synchronous wait and wall time. Prioritize that exact preload only if the cold wait recurs. Verify the wait disappears on the affected path and startup, save restore and warm spawn remain within baseline variation. Owner → [[TODOs/Optimization/Loading_Freeze_Plan|Loading freezes]].

### P3 · Smaller candidates

| Candidate | Proof required to keep a change | Owner |
|---|---|---|
| Heavy beam property block | Repeated pooled launches show the per-activation allocation gone, with color, width, fade and reuse preserved. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|Battle FPS recovery]] |
| Music lifetime | Compare Model/playlist bundle closures and Player memory. Verify first play, loop and crossfade under load. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size follow-up]] |
| Readable meshes | Restrict to shipped meshes with verified CPU consumers traced. Pilot safe candidates, measure Player RAM and exercise wreck cutting, shields and colliders. | [[TODOs/Optimization/Asset_Size_Followup_Plan\|Asset size follow-up]] |
| Bolt texture | Audit CPU readers of the surviving `BlasterBolt.png`. Measure memory and compare alpha/shape of 1/2/4/8 bolts at near/far zoom. | [[TODOs/Optimization/Battle_FPS_Recovery_Plan\|Battle FPS recovery]] |

## Verification

- For each retained fix, save the revision, target, workload, before/after values, repeat variation and visual/lifecycle result in its owner plan. For a rejected trial, restore the original and record why. Recapture the baseline between independently retained changes.
- Close this coordination plan when each item has a measured outcome, a documented rejection or an explicit deferral with an owner and trigger. Existing plans retain their broader scope and acceptance gates.
