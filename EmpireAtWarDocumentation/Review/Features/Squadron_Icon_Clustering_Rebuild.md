---
category: Features
status: review
acceptance: pending-user
created: 2026-11-18
---
# Squadron Icon Clustering Rebuild

## Goal
- Squadron icons never stack: nearby squadrons → one compact row/column matrix grid; spread out → icons peel away smoothly.
- Icons stay readable near their squadrons but never cover ships (ships must stay clickable). Grid may sit offset from the squadrons.
- Smooth, jitter-free motion and cheap: ≤ 0.3 ms main-thread p95 at 150 squadrons + 80 ships, 0 B/frame GC (target, unmeasured).
- Spec source: user feature description `squadron_icon_clustering_feature.md` (2026-11-18). Replaces the grouping part of [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]]. Small-ship markers stay as they are.

## User-reported problems (2026-11-18)
- Icons still stack.
- Grid is not a real matrix; no smoothness.
- Icon/frame "drags", especially while squadrons move.
- Icons cover ships when squadrons attack → ship cannot be selected.
- Too expensive.

## Root causes (verified in source 2026-11-18)
### Stacking
- Grouping runs in **world XZ**, not screen space: `SquadronGroupingJob.JOIN_RADIUS` 70 u, leave 94.5 u. Zoomed out, squadrons > 70 u apart overlap on screen but stay separate.
- **Velocity gate**: `JOIN_SPEED` 22 u/s, `LEAVE_SPEED` 33 u/s. Dogfighting squadrons have diverging anchor velocities → leave or never join.
- Standalone icons only add themselves to `_occupied`; `BattleMarkerProjection.FindPlacement` moves grid partitions only → standalone icons overlap each other freely.
### Not a matrix
- `SquadronGroupingJob.GetCell` fills concentric rings around (0,0): 4 squadrons → plus/T shape, 5 → plus.
- `ClaimFirstVacancy` leaves holes after departures; `AccumulateGroup` keeps the old `Min/Max` footprint ("keep footprint when members leave") → sparse, off-center grids.
### No smoothness / dragging
- Group center chases its target with a 2 px dead zone (`ANCHOR_DEAD_ZONE_PIXELS`) → lags, then steps.
- Center target = the member closest to the old center (`AccumulateGroup`) → jumps when that member changes.
- `MembershipChanged` resets `Center = Sum / Count` instantly → whole grid jumps on join/leave.
- `BattleMarker.Display` writes `anchoredPosition` only after > 0.5 px → stair-stepping at slow speed.
- Double smoothing: per-icon `_layoutOffset` decay (12/s) on top of the group center pursuit → rubber band.
- `SquadronMarkerAnchor`: combat response 0.8/s, step capped by cruise speed, tether 3× loiter → icon trails the squadron in combat/travel ("whole icon dragging").
- `FindPlacement` hops between candidate spots when obstacles change (only memory = previous offset).
- "Icon inside frame drags": prefab has `Frame` and `Icon` under the same `iconRoot` → not explained by source yet. Candidates: per-frame `localScale` change (`visualScale = Pitch / (BasePitch * zoom)`) with the icon's (0, −2) offset; separate batches rebuilt at different times. → Phase 0.
### Icons over ships
- Ships are not obstacles: `_occupied` holds marker rects only. Grid sits at anchor + 60 px; attacking squadrons orbit the target → grid on the hull.
- `SelectionQuery.CollectAt` tests markers before hulls → the click selects the squadron.
- Standalone hit rect 132 px (`clickSize`) vs 72 px visual → invisible click area over hulls.
### Cost
- Per frame per marker: 3× `TryGetSlot` dictionary lookups, tuple-key dictionaries (`_partitionIndices`, `_previousOffsets`).
- `IsFree` is O(occupied) inside 5 rings × 4 sides × 2·ring steps, plus a full-viewport scan fallback → worst case O(partitions × cells × occupied).
- Every `anchoredPosition`/`localScale` write dirties the shared `BattleMarkerLayer` canvas → batch rebuild of all markers (ships included) almost every frame.

## Decision (defaults — change before implementation if wrong)
- Membership in **screen pixels**, same owner only, no velocity/order gate. Separation follows real on-screen movement + dwell.
- Grid = real matrix: `cols = min(MAX_COLS, ceil(sqrt(n)))`, `rows = ceil(n / cols)`, row-major, last row centered. Standalone icon = 1×1 grid (one code path).
- Stable slots: each cluster keeps an ordered member list (join order). Leave → later members shift one slot (animated). Join → append. Merge → larger cluster first, incoming keep their relative order.
- Avoid **all** ship hulls (any owner) and ship markers. No depth exception (user priority: ships must be clickable).
- Placement may move the grid away from the squadrons up to `MAX_DISPLACEMENT`; thin leader line when offset > `LEADER_PX`.
- Off-screen: hide (current policy). Fog/cloak: unchanged (`SquadronIconComponent.RefreshVisibility`).
- Why: screen space matches what the player sees; one layout path fixes standalone stacking; matrix + rank order is predictable.
- Avoid: world-space radii, velocity gates, ring/spiral slots, dead zones, pixel thresholds on writes.

## Implementation
### Phase 0 — Baseline (user-run, no agent Play Mode)
- [ ] User captures a crowded battle (Development Player): `Battle.Markers.*`, `Canvas.BuildBatch`/`SendWillRenderCanvases`, GC alloc. Same save/camera for the after-capture.
- [ ] Identify the "icon inside frame drags" cause from a short video/frame step (scale change vs batch). Record the result here.

### Phase 1 — Screen-space membership (`SquadronGroupingJob`, `SquadronIconGrouping`)
- [ ] Input per visible squadron: entity id, owner, projected screen point (from Phase 3 anchor), on-screen flag.
- [ ] Spatial hash in pixels, cell = `JOIN_PX`; neighbors 3×3 → O(n).
- [ ] Join: distance to cluster centroid ≤ `JOIN_PX` for `JOIN_DWELL`. Leave: > `LEAVE_PX` for `LEAVE_DWELL`. Merge clusters: centroids ≤ `MERGE_PX` for `JOIN_DWELL`.
- [ ] Co-leavers re-cluster through the normal join rule (→ new smaller grid).
- [ ] Zoom: radii scale with `zoomScale` pitch; dwell absorbs churn. Pan does not change screen distances.
- [ ] Keep 10 Hz Burst job + persistent native buffers; publish member rank + cluster id, not cells.

### Phase 2 — Matrix layout (new `SquadronClusterLayout`, replaces `GetCell`/partitions)
- [ ] `cols/rows` from count, cells row-major, last row centered; pitch = 84 px × zoomScale (existing).
- [ ] Ranks compact on removal; no holes, no footprint memory, no partitions (cap with `MAX_COLS` × `MAX_ROWS`; overflow → extra rows, viewport-clamped).
- [ ] Destroyed squadron / spawn burst → one reflow per batch, not per event.

### Phase 3 — Motion (`BattleMarker`, `SquadronMarkerAnchor`)
- [ ] Final icon position = `projectedCentroid` (exact every frame) + `placementOffset` (spring) + `cellOffset` (spring per icon).
- [ ] Camera pan/zoom never goes through a spring → no drag while panning.
- [ ] Membership change: add `oldCentroid − newCentroid` into the placement spring state → visually continuous, no jump.
- [ ] Critically damped spring (position + velocity, `SMOOTH_TIME`) → retargeting mid-flight stays continuous; peel-away = cell offset springs from slot to own anchor.
- [ ] Remove the 0.5 px write threshold and the 2 px dead zone.
- [ ] Anchor: low-pass fighter centroid (`ANCHOR_SMOOTH_TIME`), no cruise-speed cap/tether lag. Keep the teleport reset (spawn/respawn).
- [ ] Constant `localScale` per zoom step; no per-frame visual scale from pitch.

### Phase 4 — Ship-aware placement + selection (new `SquadronClusterPlacement`)
- [ ] Ship obstacles: project the 8 corners of `ShipSpawnClearance.GetLocalHullBounds(shipType)` through the ship view transform → screen rect. Bind via a ship facade (reuse/extend `ShipMarqueeHull` if it fits; it is bound only for HeavyCruiser+ today → extend to all ship classes and stations via `IStationHullFacade`). Cache once per frame, visible ships only.
- [ ] Obstacles = hull rects + ship marker rects + already placed grids (bigger clusters first).
- [ ] Candidates: above (default), below, left, right, 4 diagonals × 2 distances ≤ `MAX_DISPLACEMENT`. Cost = displacement + overlap area × penalty + side-change penalty (hysteresis, `SIDE_SWITCH_GAIN`).
- [ ] No free spot → least-overlap candidate (never vanish, never jump across screen).
- [ ] Leader line from grid edge to centroid when offset > `LEADER_PX` (one UI line per displaced cluster).
- [ ] Hit rect = displayed visual cell (72 px × zoomScale), grouped and standalone. Drop the 132 px click area.
- [ ] Obstacle query: uniform screen grid over rects → no O(n²) `IsFree` scans.

### Phase 5 — Render cost
- [ ] Squadron icons on their own canvas (separate from ship markers) so squadron motion does not rebuild ship marker batches.
- [ ] If Phase 0/after-capture still shows `Canvas.BuildBatch` dominant: one custom `MaskableGraphic` (or one mesh) drawing all squadron icons + frames from an atlas; per-icon `GameObject`/`Image` removed.
- [ ] No `SetActive` churn: hide by alpha/skip in the batch graphic.
- [ ] 0 B/frame GC: no LINQ/closures/tuple dictionaries in the per-frame path; preallocated arrays.

### Phase 6 — Tests, guidance, acceptance
- [ ] Tests (category `Squadrons`, not run — local override): rewrite `SquadronIconGroupingTests` (screen join/leave hysteresis, dwell, owner split, merge order, co-leavers, zoom); new layout tests (dims for 1–16, rank compaction, centered last row); placement tests (avoids hull rect, max displacement, side hysteresis, fallback); motion tests (continuity on retarget and membership change).
- [ ] Review: `SquadronIconPrefabTests` (Squadrons), `SelectionQueryTests`/`SelectionInputTests`/`PlayerOrderInputHandlerTests` (BattleInput, hit-rect change), `ShipBattleMarkerAssetTests` (Ships), `SquadronTargetSelectorTests`.
- [ ] Update `Rules/BATTLE_PERFORMANCE_GUIDELINES` → "Battle markers" section (screen-space grouping, spring model, obstacle grid).
- [ ] Acceptance line in [[TODOs/User_Acceptance_Queue]].

## Important Values (initial, tune in-game)
| Name | Value |
|---|---|
| `PITCH` | 84 px × zoomScale (existing `CELL_PITCH`) |
| `JOIN_PX` | 1.25 × pitch |
| `LEAVE_PX` | 3 × pitch |
| `MERGE_PX` | 1.5 × pitch + half grid widths |
| `JOIN_DWELL` / `LEAVE_DWELL` | 0.25 s / 0.5 s |
| `MAX_COLS` | 4 |
| `MAX_DISPLACEMENT` | 220 px × zoomScale |
| `LEADER_PX` | 90 px |
| `SMOOTH_TIME` | 0.12 s (placement), 0.10 s (cells) |
| `ANCHOR_SMOOTH_TIME` | 0.15 s |
| Membership rate | 10 Hz Burst job; layout + motion every frame |
| Budget | ≤ 0.3 ms main p95 @ 150 squadrons + 80 ships, 0 B/frame |

## Edge Cases
- One squadron gets a move order → stays in grid until it is > `LEAVE_PX` on screen for `LEAVE_DWELL`.
- Squadron hovering at the boundary → hysteresis (1.25× vs 3× pitch) + dwell, no flicker.
- Two squadrons cross paths → ranks unchanged, no slot swap.
- Destroyed/spawned squadron → only later ranks shift; others stay.
- Carrier launch burst → new squadrons join the nearest same-owner cluster in one reflow.
- Cluster over a capital ship → grid moves to a free side within `MAX_DISPLACEMENT` + leader line.
- No free spot (huge fleet) → least-overlap candidate; ships still clickable where not covered.
- Zoom in → clusters split naturally (screen distance grows); zoom out → merge after dwell.
- Selected squadron → selection follows entity id, never the slot.
- Fog/cloak hide → immediate removal from membership (existing `InvalidateMembership`).
- Cinematic camera → all hidden (existing).

## Files
- Rewrite: `Assets/Scripts/Components/Squadron/Icon/SquadronGroupingJob.cs`, `SquadronIconGrouping.cs`, `BattleMarkerProjection.cs` (→ split into layout / placement / motion, each < 200 lines).
- Change: `BattleMarker.cs`, `BattleMarkerLayer.cs`, `SquadronMarkerAnchor.cs`, `SquadronIconComponent.cs`.
- Ship obstacles: `Entities/Ship/ShipInstaller.cs` (`ShipMarqueeHull` binding), `Services/ShipSpawning/ShipSpawnClearance.cs` (`GetLocalHullBounds`), `Entities/SpaceStation/IStationHullFacade.cs`.
- Selection: `Services/Selection/SelectionQuery.cs` (hit rect only, ordering unchanged).
- Tests: `Tests/Editor/Squadrons/SquadronIconGroupingTests.cs`, `SquadronIconPrefabTests.cs`, `Tests/Editor/BattleInput/Selection*Tests.cs`.

## Implemented 2026-11-18 (compiled; tests not run — local override)
- Phase 1: `SquadronGroupingJob` rewritten — screen-pixel clusters, spatial hash, join/leave dwell (0.25/0.5 s), ranks compact, merge keeps larger first. Ships no longer group (they are obstacles).
- Phase 2: `SquadronClusterLayout` (row-major, ≤4 cols, last row centered).
- Phase 3: `BattleMarker.Display` = exact projected target + decaying follow spring (0.1 s) that absorbs layout-key changes; cluster placement spring 0.12 s; `SquadronMarkerAnchor` = SmoothDamp 0.15 s (cruise/tether/dead zones removed; `AdvanceAnchor(centroid, dt)`).
- Phase 4: `SquadronClusterPlacement` (16 candidates, uniform obstacle grid, hysteresis), `ShipHullObstacle` registered for every ship via `IBattleMarkerLayer.RegisterHull` (`SquadronLeaderLines`), hit rect = visual cell (`clickSize`/`followSharpness` removed).
- Phase 5: squadron and ship markers on separate nested canvases; no per-frame allocations in the layout path.
- Phase 6: rewrote `SquadronIconGroupingTests`, `SquadronIconPrefabTests`; new `SquadronClusterLayoutTests`, `SquadronClusterPlacementTests`, `BattleMarkerProjectionTests`; fixed `SelectionQueryTests` fake layer and `StructureStatusIconPresenterTests` root field.

## Remaining
- Phase 0 user capture + frame-drag cause (nothing recorded; constant `localScale` per zoom already applied).
- Station hull obstacles (`IStationHullFacade` only exposes a planar radius; no `ShipHullObstacle` equivalent yet).
- Custom mesh graphic only if after-capture still shows `Canvas.BuildBatch` dominant.
- Guidance note update after tuning; acceptance lines (in-game feel, perf capture, authored `Squadrons`/`BattleInput`/`Ships` tests on explicit request).

## TODO
1. [ ] Phase 0 baseline + frame-drag cause (user capture).
2. [ ] Screen-space membership.
3. [ ] Matrix layout.
4. [ ] Motion model.
5. [ ] Ship-aware placement + hit rect.
6. [ ] Render cost.
7. [ ] Tests, guidance, acceptance queue.


## Verification 2026-11-18 (implementation review + wobble fix)
### Wobble root cause (fixed, committed in `61cc6a19b`)
- `BattleMarkerLayer` canvas was `ScreenSpaceCamera` at `nearClipPlane + 0.05` = 0.35 u. Main camera FOV 29.9° → 1 px ≈ 0.00017 u at 1080p.
- Canvas vertices go through world space; float ulp at camera coords 2048–4096 u = 0.00024 u → 1–2 px rounding per vertex. Frame and icon vertices round independently → icon wobbles inside the frame; whole marker wobbles while the camera moves.
- Earlier anchor-smoothing fix (`SquadronMarkerAnchor` 0.8 s settled) could not remove this: error is in rendering, not position.
- Fix: `BattleMarkerLayer` → `RenderMode.ScreenSpaceOverlay`, `sortingOrder` −20 (below health/hardpoint overlays −1 and prefabs −10); `BattleMarker.Display` rounds `anchoredPosition` to whole pixels.
- Side effects to check in-game: markers no longer pass through camera post-processing (bloom/tonemap) → colors may look slightly different. Structure status icons share the canvas → same fix applies to them.
- Unity recompile: 0 errors. Tests not run (local override). Reviewed: `BattleMarkerProjectionTests`, `SquadronIconPrefabTests` (no canvas/anchoredPosition assertions), `StructureStatusIconPresenterTests` (own camera canvas; presenter math unchanged).

### Capture `Logs/BattleAudit/20261118T150734656Z-attribution` (Editor)
| Battle | Main avg | `BattleMarkerLayer.LateTick` | `.Tick` | `Canvas.BuildBatch` | GC |
|---|---|---|---|---|---|
| 150734 (3050 f) | 16.31 ms | 0.144 ms | 0.011 ms | 0.025 ms | 0 B |
| 151035 (2038 f) | 20.01 ms | 0.166 ms | 0.014 ms | 0.027 ms | 0 B |
- Markers ≈ 0.18–0.21 ms/frame ≈ 1% of main thread → within the 0.3 ms budget; not an FPS cause.
- Spikes: 5 hitch frames with `LateTick` 2.7–3.4 ms (frames 13986, 14957, 15165, 15169; 21998 1.0 ms). Cause not attributed (no sub-markers in the capture). Candidates: placement overlap scan with many large hull rects; first-use list growth. Low priority.
- Real FPS owners in this capture: `EnemyAiScheduler.Tick` 1.6–1.8 ms, `CombatAttackCoordinator.LateTick` 0.74–1.29 ms.

### Implementation vs plan
- Done: screen-space membership with hysteresis/dwell (Burst, 10 Hz); matrix layout (`SquadronClusterLayout`, 4 cols); springs (`ScreenSpring`); hull obstacles (`ShipHullObstacle`, `StationHullObstacle`); visual-size hit rect; separate ship/squadron sub-canvases; tests (`BattleMarkerProjectionTests`, `SquadronClusterLayoutTests`, `SquadronClusterPlacementTests`, `SquadronIconGroupingTests`).
- Deviations: leader lines removed by user (`2303997e0`). Anchor uses 0.8 s smooth time when not travelling (adds lag in combat); revisit now that the render wobble is fixed.
- Old `SquadronIconPrefabTests` grid/ship-sharing tests removed in `8a4b9d8db`: contracts changed (ships no longer share grids); replaced by the new fixtures.
- Open: in-game acceptance (wobble gone, colors, ship clickability); optional spike follow-up.

## Review 2026-11-26
- Source check: P1–P4, P5.1, P5.4, P6 done (`8a4b9d8db`, `61cc6a19b`, `25aca7db0`). Steps were not ticked.
- Changed: hash cell = `JoinPx + 3·Pitch`; merge folded into the join check (no `MERGE_PX`); no `MAX_ROWS`/viewport clamp (off-screen markers hide); per-marker follow spring replaces the cell spring.
- Dropped: leader line (removed by user, `2303997e0`).
- Deferred to the capture: P5.2 custom `MaskableGraphic` and P5.3 `SetActive` churn (`Canvas.BuildBatch` 0.025 ms in Editor).
- Open user gates → [[TODOs/User_Acceptance_Queue]]: in-game feel and ship clickability; Development Player before/after capture; `Squadrons`/`BattleInput`/`Ships` tests.
- Guidance: `Rules/BATTLE_PERFORMANCE_GUIDELINES` "Battle markers".
