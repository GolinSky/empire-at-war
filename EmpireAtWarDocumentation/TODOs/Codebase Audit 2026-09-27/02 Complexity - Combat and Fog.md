---
tags:
  - code-audit
  - refactoring
created: 2026-09-27
status: implemented
scope: read-only source review
updated: 2026-09-27
---
# Complexity — Combat and fog

> [!info] Implementation update
> C3 and C4 implemented: target-selection batch and buffers extracted; owner checks/event ordering retained; fog projection unified, component lookups removed, sharedMesh used and serialized renderer bound.
> See [[TODOs/Codebase Audit 2026-09-27/09 Implementation Results|implementation results and verification]]. Evidence/line numbers below describe the original audit snapshot unless marked implemented.

[[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]]

> [!warning] Follow-up 2026-09-28
> `FogVisibilityGridModel`/`FogVisibilityModel` sit loose in the `Scripts/Entities` root. CombatAttackCoordinator is still 506 lines. See [[TODOs/Codebase Audit 2026-09-27/10 Follow-up Sweep 2026-09-28|the follow-up sweep]].

## C3 — CombatAttackCoordinator owns two batching pipelines plus telemetry
**Priority:** P2 · **Confidence:** confirmed structure; performance impact unmeasured.

**Evidence:** [Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:204](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs#L204) combines targeting, due-event advancement, ownership-generation checks, native-buffer accounting, and workload reporting. [Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs:301](file:///F:/Private/empire-at-war/Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs#L301) owns target capture, serial/job selection, scheduling, and application. The file is about 719 lines.

**Solution:** first extract the target-selection batch and its buffer lifetime into a focused combat service. Keep ordered impact/sequence commitment and owner-generation checks together in the coordinator. Move reporting assembly to the existing diagnostics area only if that simplifies the call site.

**Do not remove:** serial paths, generation guards, or deterministic due-event ordering merely to reduce line count. They have observable correctness/performance roles. This belongs in a combat service, not generic Utils.

**Future verification:** compare target selection, cancellation, owner reuse, same-frame event order, and disposal. Existing coordinator/job test sources are relevant; none were run.

## C4 — Fog computation, projection and rendering are mixed
**Priority:** P2 · **Confidence:** confirmed duplication and responsibility mix.

**Evidence:** [Assets/Scripts/Components/ViewComponents/FogOfWarSystem.cs:159](file:///F:/Private/empire-at-war/Assets/Scripts/Components/ViewComponents/FogOfWarSystem.cs#L159) maps world positions to pixels, tracks historical visibility, paints circles and modifies color buffers. [Assets/Scripts/Components/ViewComponents/FogOfWarSystem.cs:303](file:///F:/Private/empire-at-war/Assets/Scripts/Components/ViewComponents/FogOfWarSystem.cs#L303) repeats the projection but obtains bounds through a fresh component lookup instead of the serialized `meshFilter`. Different references can therefore produce inconsistent mappings.

**Solution:** use one explicitly bound/cached geometry source and one projection implementation for both painting and querying. Extract visibility-grid/history computation into a pure model; keep texture/material/Transform operations in the Unity view, coordinated by a presenter. Reuse the existing `FogVisibilityModel` falloff calculation.

**Future verification:** compare painting and sampling at identical positions, boundaries, flipped axes, rotated surfaces and non-default meshes. Preserve history and fade behavior. No runtime failure was reproduced.
