# P0 stall follow-up — November 7

## Findings

- Historical 198.608 ms harness event localized to rolling frame 12691 (199.309006 ms), UTC 09:35:26.831144, run `2ed5b28d56b841cd9c7046e6d0791db4`. Native stacks were not recorded. Its exact cause remains unproven.
- Original 296 ms native event remains a proven AssetBundle wait with unresolved managed key/caller. A new run cannot retroactively prove the original request.
- Two new diagnostic runs: first maxima 83.524 / 66.671 / 53.211 ms; second 62.189 / 576.310 / 1797.483 ms. Same retained Mono workload, original satellite omission preserved for attribution. No general FPS conclusion.
- Second run long gaps: frame observer 11667 at UTC 10:45:26.924229, 576.917 ms; frame 12974 at UTC 10:45:47.980456, 1797.563 ms. Rolling frame numbers/deltas differ by callback phase (11666/577.830 ms and 12973/1810.098 ms).
- Both new long gaps: main thread waits in `GfxDeviceClient::CreateGpuProgram → ShaderLab::SubProgram::Compile → shader variant selection → draw submission`. Graphics worker waits in `MTLCompilerScheduler::buildRequest → MTLLibraryBuilder`. This directly attributes the new stalls to synchronous runtime GPU-program/Metal compilation.
- Main-thread recorded wait-sample weights: 508.99 + 40.54 ms in the first gap; approximately 1.49 s + 294.80 ms in the second. These are trace sample weights, not a separate scheduling-duration audit.
- No ≥5 ms wrapped asset load, gameplay Tick/LateTick, or telemetry Flush covers those long gaps; GC count unchanged. Native attribution is independent of that absence.
- First run 48.241 ms lifecycle LateTick includes Mono JIT compilation (18 of 33 main-thread samples within the selected 49 ms interval). Second run pinpoints `CombatAttackCoordinator.LateTick` at 45.077 ms in a shorter event. Do not claim the same cost in IL2CPP.
- First-run 83 ms gap occurred just after Instruments attachment; 79 ms sample gap, resuming near `ProfilerManager::StartNewFrame`. Capture disturbance possible; cause not established.

## Method and limits

- `DeepProbe.cs` buffers slow method stacks and frame-gap records until quit. Individual stack formatting sometimes costs 1–16 ms, explicitly recorded as `probeCost`.
- `Patch.cs` uses Mono.Cecil to wrap methods in an isolated Player copy. Compile helper against that Player's mscorlib/System/System.Core/CoreModule, copy `DeepProbe.dll` into Managed, run patcher with the Managed directory, replace `.patched` outputs and ad-hoc sign the copied app. `patched-methods.txt` lists exact coverage. No production source or Editor mutation.
- Wrapper timings cover normal returns; exceptions are not timed. Native extern calls are covered only where a managed wrapper exists. The probe is diagnostic, not a zero-overhead benchmark.
- `ShaderLogging/DeepProbe.cs` is a later helper variant enabling `GraphicsSettings.logWhenShaderIsCompiled`; shader logging adds overhead. Compile it as `DeepProbe.dll` into a copied patched app, not alongside the original helper.
- Direct launch required: Instruments launch mode selected the original bundle via launch resolution. Excluded attempted launch, then attached by verified PID.
- Discovered and terminated orphan PID 24664 from this investigation's original trace launch (command pointed to `traced/player.log`). Other user work left running. Concurrent Player/Editor/tracing load makes causal stall mechanisms useful but duration/FPS comparisons uncontrolled.
- Tests disabled and not run. No additional production fix yet: exact shader/pass/keywords and a safe scoped warmup require evidence. Broad `Shader.WarmupAllShaders()` previously caused keyword-space errors; do not reintroduce it.

## Files

- `historical-198ms.json`: history metadata and neighboring detailed rows.
- `first-deep-events.txt`, `second-deep-events.txt`: buffered method/stack/frame evidence.
- `first-timing.txt`, `second-timing.txt`: three 60 s timing windows each.
- `first-selected-cpu.json`, `second-native-stall-samples.json`: selected native samples with symbolicated engine/Metal paths; Mono JIT addresses may remain unresolved.
- Completed native captures outside repository: `/Users/golinsky/Library/Caches/EmpireOptimization/20261107-P0/deep-probe/attached-cpu.trace` and `deep-repeat/cpu.trace`; XML exports alongside. Second trace start UTC 10:43:56.316; long-gap offsets approximately 90.031–90.608 s and 109.867–111.665 s.
- Obsidian: `TODOs/Optimization/Optimization_Audit_Fix_Plan - Research.md`; active owner `Optimization_Audit_Fix_Plan.md`.

## Shader-log repeat and capture overhead

- No native profiler attached. Harness maxima 43.240 / 217.039 / 846.885 ms. Shader logging and method instrumentation remain overhead; concurrent user work continued.
- 149 native shader-upload messages saved in `shader-uploads.txt`; largest reported upload 50 ms (`Universal Render Pipeline/Particles/Unlit`, ForwardLit fragment, `_FADING_ON _SOFTPARTICLES_ON _SURFACE_TYPE_TRANSPARENT`). None of these native messages reached `Application.logMessageReceived`, so the attempted managed frame/UTC correlation produced zero shader records. Do not assign these variants to the prior 576 ms/1.8 s waits or this run's 217 ms gap.
- Probe frame 13881: 217.525 ms at UTC 10:55:56.667784; no matching wrapped slow call and no native trace for this repeat. Cause unproven.
- Probe frame 18743: 188.188 ms; `BattleHitchCapture.Flush` 152.142 ms inside `PerformanceAnalyticsSampler.Tick`. This is a capture-infrastructure stall, not evidence that the historical 198 ms event had the same cause. Nearby CSV wait accounting is preserved in `capture-writer-wait.json`.
- Probe frame 18708 includes 83.530 ms of stack-formatting instrumentation cost; its 846 ms gap is not a clean game-only measurement. All overhead rows retained.
- Shader-log repeat exited 139 after saving all windows; completed traces and previous results are retained. No additional production change made. Next work: per-request asset tracing plus native capture; timestamp native shader uploads externally or use native profiler metadata before choosing a scoped material/variant warmup.
