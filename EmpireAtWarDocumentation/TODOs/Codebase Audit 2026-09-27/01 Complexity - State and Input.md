---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: proposed
scope: read-only source review
---
# Complexity — State and input

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## C1 — Make time-state ownership explicit
**Priority:** P2 · **Confidence:** confirmed design complexity; not a demonstrated bug.

**Evidence:** [Assets/Scripts/Entities/CoreGame/Controller/SkirmishOrchestrator.cs:62](file:///F:/Private/empire-at-war/Assets/Scripts/Entities/CoreGame/Controller/SkirmishOrchestrator.cs#L62) stores `_gameTimeMode`, while `ChangeTime` writes the effective mode into `SkirmishSessionModel`. Menu notifications apply Pause/Common without changing the private field. [Assets/Scripts/Tests/Editor/SkirmishOrchestratorTests.cs:93](file:///F:/Private/empire-at-war/Assets/Scripts/Tests/Editor/SkirmishOrchestratorTests.cs#L93) explicitly expects this separation in `MenuPause_DoesNotChangeStoredMode`. This test was read, not run.

**Why it matters:** callers must infer whether a mode means requested speed or effective simulation state. A simple “remove duplicate field” fix could break intentional menu behavior.

**Solution:** name requested and effective modes explicitly. Keep speed preference and pause reasons in the session model; let the orchestrator coordinate transitions and a small time adapter apply the resulting scale. First document whether leaving a menu restores Common or the previous speed; preserve the existing rule until a behavior change is approved. Add an adapter only as part of this refactor, not a general clock framework.

**Future verification:** characterize speed → menu → return → speed toggle, manual pause → menu → return, and battle end. Do not change the existing menu semantics accidentally.

**Scope restraint:** the current orchestrator is about 153 lines; the older [[TODOs/SkirmishOrchestrator_Refactoring_Plan]] is not evidence that it still needs a wholesale rewrite.

## C2 — InputService handles several independent jobs
**Priority:** P2 · **Confidence:** confirmed responsibility mix.

**Evidence:** [Assets/Scripts/Services/InputService/InputService.cs:194](file:///F:/Private/empire-at-war/Assets/Scripts/Services/InputService/InputService.cs#L194) coordinates pointer drag, camera panning, keyboard zoom, touch pinch state, selection shortcuts, and command keys. [Assets/Scripts/Services/InputService/InputService.cs:307](file:///F:/Private/empire-at-war/Assets/Scripts/Services/InputService/InputService.cs#L307) also performs UI raycasts, allocates event data/results, and resolves the UI layer on each call.

**Solution:** retain InputService as the input adapter; extract gesture state into a focused model and UI hit detection into an injected service that owns reusable raycast storage. Keep camera mapping separate only where it reduces the current branching. No generic input-event framework.

**Reuse:** UI hit detection can serve command input and selection; gesture state can support mouse/touch without coupling rules to device polling.

**Future verification:** preserve click-versus-drag threshold, press-start-over-UI behavior, blocked input, pinch, right-click, and modifier-release ordering. Allocation savings are a hypothesis until measured.
