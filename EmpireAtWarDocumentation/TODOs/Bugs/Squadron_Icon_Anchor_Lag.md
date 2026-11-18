---
category: Bugs
status: todo
created: 2026-11-18
---
# Squadron Icon Anchor Lag

## Goal
- Squadron icon follows its squadron without visible lag in every pilot mode (travel, loiter, engage), on host and replicas.
- No jitter from fighters circling a target.
- Parent: [[TODOs/Features/Squadron_Icon_Clustering_Rebuild]].

## Current state (source, 2026-11-18)
- `SquadronMarkerAnchor.Advance`: `Vector3.SmoothDamp` of the fighter centroid.
  - `TRAVEL_SMOOTH_TIME` 0.15 s when `travelling`; `SETTLED_SMOOTH_TIME` 0.8 s otherwise.
- `Squadron.cs:279`: `travelling = _isReplica ? CurrentOrder.IsMoveOrder() : _pilot.IsTravelling` (`Mode.Travel` only).
- Loiter and Engage both use 0.8 s. On replicas, attack/guard orders always use 0.8 s, including the flight to the target.
- Added as a wobble fix (centroid jitter). The real wobble was the canvas render precision (fixed: `ScreenSpaceOverlay`). The 0.8 s filter is no longer needed for that.

## Lag size
- SmoothDamp following constant speed `v` trails by ≈ `v × smoothTime`.
- 0.8 s: chasing a ship at 20 u/s → ~16 u behind; flying to a target at 60 u/s in Engage → ~48 u behind (≈ 2.5 loiter radii).
- Retarget/jump of 300 u → settles in ≈ 2–2.5 s (vs ≈ 0.4 s at 0.15 s).
- Effect: grid trails squadrons in combat and on clients; selection rect lags the fighters.
- Unmeasured: whether circling fighters still make visible jitter at 0.15 s. Loiter radius 18–20 u; centroid orbit ≤ that.

## Decision
- Chosen: one adaptive low-pass (One Euro filter) on the centroid for all modes. Slow centroid motion (orbit wobble) → strong smoothing; fast bulk motion → light smoothing, low lag.
- Why: removes the `travelling` mode switch (wrong on replicas, wrong in Engage), one rule for all states.
- Avoid: per-mode smooth times, screen-pixel dead zones, velocity caps (cause lag/steps; removed earlier).

## Implementation
1. [ ] Quick check first: set `SETTLED_SMOOTH_TIME` = `TRAVEL_SMOOTH_TIME` (0.15 s). User checks a dogfight around a capital ship: jitter visible? If not → remove the `travelling` flag and stop here.
2. [ ] If jitter is visible: replace SmoothDamp in `SquadronMarkerAnchor` with a One Euro filter on the 3D centroid.
   - `cutoff = MIN_CUTOFF + BETA × |filtered speed|`; `alpha = 1 / (1 + 1 / (2π × cutoff × dt))`.
   - Start values: `MIN_CUTOFF` 0.6 Hz, `BETA` 0.05, `DERIVATIVE_CUTOFF` 1 Hz. Tune in-game.
   - Keep the teleport reset (`TELEPORT_DISTANCE` 150 u → `Reset`).
3. [ ] Remove the `travelling` parameter: `SquadronMarkerAnchor.Advance`, `SquadronIconComponent.AdvanceAnchor`, `Squadron.cs:279–280`.
4. [ ] Tests (`Squadrons`, not run — local override): new `SquadronMarkerAnchorTests`: constant-speed lag ≤ 10 u at 60 u/s after settling; orbit of radius 20 u at 60 u/s → anchor amplitude ≤ 25% of radius; teleport resets. No tuned-value pinning beyond these relationships.
5. [ ] User acceptance: icon on squadron during travel, dogfight, target switch; host and client.

## Edge Cases
- Replica snap (host root jump) → teleport reset, no glide.
- Respawn/pool reuse → `Reset` in `Initialize` (existing).
- Very low FPS (dt 0.1–0.3 s) → filter alpha uses real dt; no overshoot.
- Squadron destroyed mid-filter → anchor released with the marker (existing).

## Files
- `Assets/Scripts/Components/Squadron/Icon/SquadronMarkerAnchor.cs`
- `Assets/Scripts/Components/Squadron/Icon/SquadronIconComponent.cs`
- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Tests/Editor/Squadrons/SquadronMarkerAnchorTests.cs` (new)
