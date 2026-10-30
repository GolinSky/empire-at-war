# CPU investigation tools

- Temporary diagnostic entry points compiled in memory by Unity Pipeline. Sources stay outside `Assets`; no domain reload is needed to run them.
- These profile operations, not automated tests. Repository test restrictions still apply.
- Follow `Tools/BattleAudit/README.md` for Play Mode ownership and one active capture at a time. The user explicitly authorized starting/stopping the crowded battle for the 2026-10-30 investigation.
- Reports: `Logs/LateBattleCpu/`. Retained evidence: `Docs/Audits/2026-10-30-Late-Battle-CPU/analysis.md`.

| Source / entry | Purpose |
|---|---|
| `ProfileShotPrefabs.Main` | Actual prefab Instantiate/Reset timings in an isolated Edit Mode preview scene |
| `ProfileShotNativeComponents.Main` | Native component ablations on disposable preview templates |
| `ProfileShotTransformFence.Main` | Independent-hierarchy transform-job fence control; separates clone and completion time |
| `ProfileShotPopulation.Main` | Registration scaling with stopped native-only populations; memory deltas are global, not per object |
| `SetupCrowdedBattle.Launch` | Full Small seed 1875278133, Republic / UltraHard Separatist; DestroyOpponentBase objective |
| `SetupCrowdedBattle.Prepare` | Cheats: both stations level 5, 1M credits each, 120-population human fleet |
| `SetupCrowdedBattle.StressFleets` | Additional adopted enemy ships/squadrons toward 80 ships / 320 squadrons; deliberately bypasses ordinary enemy cap |
| `SetupCrowdedBattle.Engage` / `Status` | Orders human fleet toward enemy base / reports current counts and battle state |
| `SetupCrowdedBattle.ArmPrewarmDelayed` | Requests 12 extra cached shots per currently used prefab after 3 game seconds, so a profiler can start before the real ordered Tick creates them |
| `ProbeLiveShots.Main` / `Start` | Actual unfired clone per frame in a running battle; Start returns before completion for overlapping profiler capture |
| `DecodeShotAwake.Run(first,last)` | Raw Awake/Copy/Produce attribution by prefab and creation source; select the exact new capture range |
| `ProfileCaptureGeometry.Main` | Paired same-frame registry scans with live versus refresh-scoped geometry; validates identical strength values |

```powershell
unity command run_script --file Tools/Performance/ProfileShotPrefabs.cs --entry ProfileShotPrefabs.Main --json
unity command run_script --file Tools/Performance/SetupCrowdedBattle.cs --entry SetupCrowdedBattle.Status --json
unity command run_script --file Tools/Performance/DecodeShotAwake.cs --entry DecodeShotAwake.Run --args '[0,301]' --json
```

- `SetupCrowdedBattle` mutates only the active runtime battle. Do not invoke it during an existing user-owned match without authorization.
- Wait at least 60 seconds after stress spawning before the diagnostic capture. Keep state `Running`; an ended or paused battle does not service its normal prewarmer.
- Profiler indices accumulate across recordings unless explicitly cleared. Read the loaded recording's first/last indices; never assume a later recording starts at 0.
- Original M2 artifacts are not present locally. Windows results and synthetic controls do not establish the historical M2 root cause or a matched frame-rate improvement.
