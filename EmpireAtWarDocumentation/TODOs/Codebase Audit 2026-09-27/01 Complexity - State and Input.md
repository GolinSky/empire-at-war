---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: implemented
scope: read-only source review
updated: 2026-09-27
---
# Complexity — State and input

> [!info] Implementation update
> C1 and C2 implemented: menu close restores requested time, controls are blocked in menus, effective mode is explicit, and UI hit testing/gesture state are extracted.
> See [[TODOs/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

## C1 — Restore requested time after menu close
**Priority:** P1 · **Confidence:** confirmed source-level bug.

The original audit understated this issue. Menu close forced Common while the private stored mode remained SpeedUp or Pause. After closing the menu, speed/pause controls needed an extra press and manual pause was lost.

**Implemented:** `_requestedTimeMode` stores the user's choice; `SkirmishSessionModel.EffectiveTimeMode` exposes applied simulation state. Menu close applies the requested mode. Pause/speed-up presses are ignored while the menu is open. The existing menu-pause test is preserved; two new cases cover restoring SpeedUp and Pause and the next toggle.

**Verification:** compiled, not executed in Unity's test runner.

## C2 — InputService handles several independent jobs
**Priority:** P2 · **Confidence:** confirmed responsibility mix.

**Evidence:** [Assets/Scripts/Services/InputService/InputService.cs:194](file:///F:/Private/empire-at-war/Assets/Scripts/Services/InputService/InputService.cs#L194) coordinates pointer drag, camera panning, keyboard zoom, touch pinch state, selection shortcuts, and command keys. [Assets/Scripts/Services/InputService/InputService.cs:307](file:///F:/Private/empire-at-war/Assets/Scripts/Services/InputService/InputService.cs#L307) also performs UI raycasts, allocates event data/results, and resolves the UI layer on each call.

**Solution:** retain InputService as the input adapter; extract gesture state into a focused model and UI hit detection into an injected service that owns reusable raycast storage. Keep camera mapping separate only where it reduces the current branching. No generic input-event framework.

**Reuse:** UI hit detection can serve command input and selection; gesture state can support mouse/touch without coupling rules to device polling.

**Future verification:** preserve click-versus-drag threshold, press-start-over-UI behavior, blocked input, pinch, right-click, and modifier-release ordering. Allocation savings are a hypothesis until measured.
