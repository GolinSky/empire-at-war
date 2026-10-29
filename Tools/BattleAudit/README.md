# Battle performance audit

How to measure a running battle, find what costs CPU or allocates, and where results go.

## Rules
- The user starts the battle and owns Play Mode. Never start, stop, pause or reload it during a capture; stop it only with the user's permission (Unity is set to stop Play Mode before recompiling).
- One capture at a time. Unity `eval` calls time out (5 s) while a Profiler capture runs: wait for `capture-status.json` to leave `running` first.
- Always restore GC call-stack recording to `None` after an allocation capture, while the battle is still playing.
- Close Inspector windows showing materials before capturing: `MaterialEditor` allocates ~1 MB per 40 frames in the Editor.
- Timings from the Editor are inflated, especially with the Profiler attached. Use the battle CSV for timing, the Profiler for attribution. Compare runs only with similar workload (`target_candidates`, `target_requests`, projectile counts).
- Do not add a parent marker to its children (nested scopes); worker-thread waits are not costs.

## Files
- `Assets/Scripts/Editor/RenderAuditAblation.cs` — rendering ablation menu/CLI; camera and input stay pinned, runtime toggles restore on completion, cancellation, Play Mode exit and assembly reload.
- `Tools/BattleAudit/battle_audit.py` — CSV comparison, Profiler summary, decoder script generator.
- `Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs` — 10 s battle CSV recorder.
- `Assets/Scripts/Services/Timing/BattleProfilerMarkers.cs` — `Battle.*` markers used in captures.
- Unity MCP tools: `eval`, `eval_file`, `render_audit_profiler`, `get_console_logs`, `editor_status`.

## Procedure

### Rendering ablation

1. Start a battle. Use the same map seed, nebula variant, camera pose, preset, resolution and battle workload for comparisons. Close Scene view (triangle counters are Editor-wide).
2. Run `unity command render_audit_ablation --json`, or **Tools → Empire At War → Diagnostics → Performance → Rendering Ablation (Up to 208 Seconds)**. Requires an existing running, unpaused battle; finish any other Render Audit capture first.
3. Each phase settles for 2 s and samples for 6 s. Baseline then ten off/restored pairs: decor, field rocks, all rocks, nebula, stars, hazards/dust, fog, post-processing, directional shadows and combined. Each restored sample brackets its toggle with the preceding enabled sample.
   - Maps with hazards append five phases over the largest hazard at the current gameplay camera height: baseline, nebula off/restored, hazards/dust off/restored. Maps without hazards retain the original 168 s capture.
   - Each result records `hazard_screen_coverage` (0–1): a 32×18 viewport ray grid against the gameplay SDF at the top sheet height. This estimates body coverage, excluding wisps and occlusion; metadata records the hazard camera position. The original camera pose is restored after capture.
4. Read `Logs/RenderAudit/<timestamp>-ablation/ablation.json` and `capture-status.json`. Metadata includes actual map seed, source revision/dirty and untracked paths, nebula variant, camera, quality, resolution, graphics API and device. JSON numbers use invariant culture. Missing GPU timing is `null`, never zero cost.
5. Cancel with `unity command render_audit_ablation_cancel --json`. The tool leaves Play Mode running. A paused battle or 20 s without advancing a phase interrupts the capture and restores its toggles.

- FPS/mean/p95 use rendered-camera frame intervals. GPU timing uses `FrameTimingManager`; compare supported measurements only. Battle simulation continues, so workload growth can bias later phases.
- This Editor tool does not establish Development Player or historical-build results. Those require the same setup in separately identified Players. Mac capture is deferred by user request on 2026-10-29.

### 1. Check state and workload
`editor_status` must report `playing`. Then record the workload and start the CSV in one `eval`:

```csharp
EmpireAtWar.Services.Timing.BattlePerformanceCapture.StartCapture();
return new {
    paused = UnityEditor.EditorApplication.isPaused, timeScale = UnityEngine.Time.timeScale, time = UnityEngine.Time.time,
    weapons = UnityEngine.Object.FindObjectsByType<EmpireAtWar.Components.Weapon.WeaponComponent>(UnityEngine.FindObjectsSortMode.None).Length,
    squadrons = UnityEngine.Object.FindObjectsByType<EmpireAtWar.Entities.Squadrons.Squadron>(UnityEngine.FindObjectsSortMode.None).Length,
    ships = UnityEngine.Object.FindObjectsByType<EmpireAtWar.Ship.Ship>(UnityEngine.FindObjectsSortMode.None).Length,
    utc = System.DateTime.UtcNow };
```

### 2. Battle CSV (10 s, no Profiler)
1. Wait for `%USERPROFILE%/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleCaptures/battle_<yyyyMMdd_HHmmss_fff>_summary.txt` (UTC timestamp of step 1).
2. Create `Logs/BattleAudit/<yyyyMMddTHHmmZ>/` (UTC) and copy the `.csv` and `_summary.txt` there.
3. Compare with the audit baseline and the previous run:
   ```bash
   python Tools/BattleAudit/battle_audit.py csv Logs/BattleAudit/<run>/battle_<id>.csv Logs/BattleAudit/<previous>/battle_<id>.csv Logs/BattleAudit/20261011T1412Z/battle_20261011_141152_737.csv
   ```
   Output: median/mean/p95/max per column and spike frames (frame > 50 ms, navigation > 10 ms, squadron/ship orders > 6 ms, GC > 1 MB). The last CSV frame includes the recorder writing its file.

### 3. Profiler (90 frames, regular)
1. `render_audit_profiler` with `frames: 90`, `timeout_seconds: 120`. Output: `Logs/RenderAudit/<timestamp>-profiler/` (`profiler.data`, `profiler-summary.json`, `capture-status.json`).
2. Wait until `capture-status.json` is `completed`.
3. Summarize:
   ```bash
   python Tools/BattleAudit/battle_audit.py profiler Logs/RenderAudit/<timestamp>-profiler/profiler-summary.json
   ```
   Output: frame index range, CPU main / GPU median and p95, top-20 main-thread self markers (per-frame mean over top-40 lists).

### 4. Allocation stacks (24–80 frames)
1. Enable GC call stacks:
   ```csharp
   var p = typeof(UnityEditorInternal.ProfilerDriver).GetProperty("memoryRecordMode",
       System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
   p.SetValue(null, System.Enum.Parse(p.PropertyType, "GCAlloc"));
   return p.GetValue(null).ToString();
   ```
2. `render_audit_profiler` with `frames: 40` (80 to catch rare bursts). Wait for `completed`.
3. Restore immediately: same `eval` with `"None"`.
4. Read the frame range (`battle_audit.py profiler …` first line), then generate and run the decoder:
   ```bash
   python Tools/BattleAudit/battle_audit.py decoder allocations <first> <last> Logs/BattleAudit/<run>/decode-allocations.cs Logs/BattleAudit/<run>/allocation-stacks.json
   ```
   Run `eval_file` on the `.cs` (timeout 90000). Output: total bytes/calls, KB per frame, top methods by bytes; also written to `allocation-stacks.json`.
5. The first resolved method is often a framework method (`String.Concat`, `Object.Internal_CloneSingle`, `MulticastDelegate.CombineImpl`). Find the game caller with a custom decoder that keeps 6–14 resolved stack frames and filters on that method (examples: `Logs/BattleAudit/20261011T1650Z/decode-*.cs`). If the game frame is the method's opening line, inspect its IL for `newarr`/`newobj`/`box` (e.g. `params` overloads such as 5-argument `Mathf.Max`).

### 5. Attribute spikes and callers
- Who calls a marker, per frame:
  ```bash
  python Tools/BattleAudit/battle_audit.py decoder callers Battle.Navigation.Plan <first> <last> Logs/BattleAudit/<run>/decode-navigation.cs
  ```
  then `eval_file`. Works for any marker name (`Battle.Squadron.UpdateOrder`, `GameObject.ActivateAwakeRecursively`, …).
- Periodic spikes (same cost every N frames) usually mean timers in lockstep: list the CSV column per frame and check its period.
- Live state via `eval` + reflection (after captures finish): ship order states (`Ship._orders._stateMachine.CurrentState`), navigation queue (`ShipNavigationService._planQueue`), queued ships (`ShipMoveComponent._hasQueuedPlan`).
- Check the console: `get_console_logs` with `severity: error` and `warning`.

### 6. Write results
1. `Logs/BattleAudit/<run>/analysis.md` (Logs is git-ignored):
   - `Setup`: source commit, battle time, unit counts, capture files and frame ranges, time GC recording was restored.
   - CSV table against the audit baseline and previous run (median / mean / p95 / max).
   - Profiler hotspots, allocation findings, spikes with their attributed cause.
   - `Open items`, ranked by measured cost.
2. Vault: add a dated line with the key numbers and an evidence link to `TODOs/Optimization/Battle_CPU_Fixes_Plan.md` (Phase 9), plus any new open items as unchecked tasks. Follow AGENTS.md plan lifecycle.
3. Report to the user: headline frame time vs baseline, what changed, new bugs found, decisions needed.

## Baselines
- Audit: `Logs/BattleAudit/20261011T1412Z/battle_20261011_141152_737.csv` — 105 ms frame median, 52,310 candidates/frame.
- Run history: `Logs/BattleAudit/2026101*` folders, each with `analysis.md` or `recapture.md`.
