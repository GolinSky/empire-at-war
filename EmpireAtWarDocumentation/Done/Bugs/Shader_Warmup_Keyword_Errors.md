---
category: Bugs
status: done
created: 2026-10-30
updated: 2026-10-30
completed: 2026-10-30
---
# Shader Warmup Keyword Errors

## Goal
- Remove Play Mode startup keyword-space assertions from broad shader warmup.
- Follow-up to [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]]; preserve covered startup and shot/squadron pool prewarming.

## Decision
- Chosen: remove `Shader.WarmupAllShaders()` from `BattleStartupSequence.RunAsync`; keep the covered frame before fade-out.
- Why: actual startup reproduced 28 native keyword-space errors inside broad warmup. The call warms every loaded shader, including unrelated URP fallback variants.
- Avoid: clearing material keywords, editing package fallback chains or generating speculative variant collections.

## Implementation
- [x] Test-first baseline: `GameFlow` 153/153, `Combat` 220/220, `Capture` 101/101 passed (474 unique tests).
- [x] Reproduced Full/Small seed `1875278133`, Republic vs Separatist: 28 new keyword-space errors; battle reached `Running`.
- [x] Removed broad warmup; retained covered rendering frame, fog reveal and shot/squadron prewarm.
- [x] Post-fix `GameFlow` 153/153 passed; no compilation failure; changed-code diff check passed.
- [x] Fresh Play Mode startup reached `Running`; 0 new errors (Console cursor 255 → 264).
- [x] Captured battle through `BattleSaveService.Capture`, then `IGameCommand.LoadBattle` restored it in memory; reached `Running`, 0 new errors (cursor 264 → 273). No save file written.
- [x] Final Editor stopped; saved clean `MainMenuScene`; `isCompiling=false`, `isUpdating=false`, `scriptCompilationFailed=false`.
- Reusable guidance updated in [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Trace-based GPU fixes (2026-10-30)]]. Parent implementation and loading acceptance text reconciled.

## Edge Cases
- Verified on Windows Editor `6000.4.7f1`, URP `17.4.0`; no Mac/Metal run or Player build in this follow-up.
- All supplied stacks enter `WarmupAllShaders`; implicated URP fallback chains include SpeedTree7 and Lit/Simple Lit/Unlit particles. Underlying engine/package keyword-space cause remains inferred; no material corruption established.
- Existing automated startup tests stub the sequence; native regression was verified with actual fresh/restored Play Mode startup.
- Covered scene rendering does not preload offscreen/future effects or guarantee Metal PSO readiness. Mac loading/first-use acceptance remains with [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]] and [[TODOs/User_Acceptance_Queue]].

## Files
- `Assets/Scripts/Entities/CoreGame/BattleStartupSequence.cs`
- Supplied evidence: `C:/Users/golin/.codex/attachments/ce463f3c-0eea-4eaa-8dad-3e2eed032b9b/Pasted text.txt`.
- [Unity 6.4 broad-warmup API and limitations](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/Shader.WarmupAllShaders.html).
