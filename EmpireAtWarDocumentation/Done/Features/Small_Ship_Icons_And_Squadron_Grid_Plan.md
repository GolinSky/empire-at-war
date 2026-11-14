---
category: Features
status: done
created: 2026-11-14
updated: 2026-11-14
design_state: implemented
completed: 2026-11-14
acceptance: pending-user
---
# Small Ship Icons and Squadron Icon Grid

## Goal
- Add battle markers matching existing squadron icons to corvettes, Centax, Pelta, Carrack and the Republic Stealth Corvette (`ShipType.StealthCorvette`).
- Give each squadron a stable, individually selectable grid slot while fighters continue independent flight/combat.
- Implemented 2026-11-14. Unity compiled and saved the configuration. Live battle acceptance and Development Player measurements are queued.

## Decision
- Interpret “3D icons” as existing battle-projected UI markers (`Image` on the shared screen-space canvas), not miniature rendered ship models. The attached image illustrates nine squadron slots; it does not require a fixed nine-unit limit.
- Small ships: follow their own hull anchor; no squadron-style stacking in this scope. Squadron grids contain squadrons only.
- `ShipData.ShowBattleMarker` opts in all ten Corvette-class hulls (including PatrolFrigate and C9979) plus Centax and Pelta. Source sprites come from `IShipIconProvider`.
- Presentation-only anchors and slots never replace gameplay positions used by movement, weapons, capture, radar, fog, saves or network authority.
- Reuse the shared icon canvas and visual language. Extract only the shared marker view/selection contract required by the two consumers; keep squadron grouping separate.
- Preserve team colors and existing input rules. Group by exact owner initially; avoid mixing enemy and friendly units in one formation.
- Grouping is driven by distance and shared motion, not order type: idle, moving and fighting squadrons all stack under the same rules.

## Grouping Requirements
- Idle together: squadrons idling/guarding near each other (same escort, same rally point, arrived together) → icons form one grid stack.
- Moving together: squadrons travelling near each other (same move order, or separate orders on parallel paths) → icons form one grid stack that travels as a unit.
- Fighting together: squadrons engaging in the same area → icons form one grid stack while fighters dogfight freely.
- Mixed states stack too: an idle squadron and a squadron that has stopped beside it join one grid.
- Passing through does not stack: a squadron crossing a group at a clearly different velocity stays separate.
  - Join requires both: anchor distance ≤ join radius for ≥ join dwell, and presentation-anchor velocity difference ≤ velocity tolerance.
  - Leave requires: distance > leave radius, or velocity difference > leave tolerance, for ≥ leave dwell.
- Moving grid: slot offsets are relative to the damped group center (mean of member anchors), so leaders/trailers inside the radius never distort or reshuffle the grid.
- Split while moving: when part of a moving group takes a different order and separates, survivors keep their relative cells; leavers ease out to standalone or into their new group.
- Arrival: a moving group that stops keeps the same grid and slots; idle → move → idle never reshuffles a group whose membership did not change.

## Implementation

### Verified baseline (2026-11-14)
- `Assets/Scripts/Components/Squadron/Icon/SquadronIconComponent.cs` (188 lines): smooths `transform.position` into private `_anchor` (exp. damping), projects it, gates visibility, handles click/marquee and lifecycle.
- `SquadronIconLayer.cs`: one `ScreenSpaceCamera` canvas, `LateTick` loop over a `List<SquadronIconComponent>`; zoomScale = `Lerp(1.5, 0.5, zoom)`. Position writes skip changes ≤0.5 px. Created in `Services/VContainer/Registrations/SkirmishServiceRegistrations.cs:78`.
- `ISquadronIconLayer.Register/Unregister` take the concrete `SquadronIconComponent`; `Unregister` is `List.Remove` (O(n)).
- Values: visual 72 px; click square 132 px; vertical offset 60 px; follow sharpness 12. Audited: all 21 squadron prefabs use these defaults, no overrides.
- Existing hit-test mismatch: `ContainsScreenPoint` uses target `_screenPosition` + `clickSize`; `OverlapsMarquee` uses displayed `anchoredPosition` + `screenSize`.
- Squadron visibility: per icon per frame `IsFriendly || IVisionService.IsVisible(point)`; not cloak-aware; main-thread cost every frame.
- Ship hull visibility: `FogVisibilityComponent` (slow tick) hides on `IsCloaked || !IsAreaVisible(position, revealRadius)`; exposes `IsHiddenByFog` and `event Action<bool> VisibilityChanged`. Minimap uses `IsFriendly || !IsCloaked && IsVisible`.
- `SquadronFlightComponent.cs:148/162` puts the squadron root on the fighter centroid; `Squadron.WorldPosition` → `_flight.Centroid`.
- `Services/Selection/SelectionQuery.cs`: icon hits precede hull raycasts; point query (line 64) and marquee (line 254) enumerate all `_entityLocator.Entities` for `ISquadronIconCommand`.
- Installed packages: Burst 1.8.29, Collections 6.4.0, Mathematics 1.3.3. No new package required.

### 1. Shared marker integration
- Introduce a shared battle-marker contract (view + hit test) and registered marker collection. Migrate squadron selection consumers and add ship binding through entity composition.
- Generalize the layer: `Register/Unregister` take the shared marker interface, not `SquadronIconComponent`; rename `SquadronIconLayer`/`ISquadronIconLayer` to a battle-marker layer (Unity-aware rename; update `SkirmishServiceRegistrations` and `Editor/Squadron/SquadronViewPrefabBuilder.cs` comment). Replace `List.Remove` with swap-remove by stored index.
- Keep explicit serialized image/root references, team/selected state, pooled reset (`IRespawnable`) and disposal.
- Add an explicit ship-marker configuration/binding for approved prefabs. Reuse suitable existing ship silhouettes (`Editor/ShipIconGenerator.cs` output); inspect readability before creating new artwork.
- Preserve hull selection. Clicking a marker resolves its entity once; right-click orders, hover and tooltips resolve that same entity.
- Fix the existing mismatch: click, hover and marquee all use the final displayed rectangle. Avoid target-position hitboxes while the visible icon is damping.
- Replace the per-query all-entity icon scan with the registered visible marker set; avoid unrelated selection refactoring.

### 2. Marker visibility
- Ship marker visibility = hull visibility: friendly → shown; enemy → bind to the hull's `FogVisibilityComponent` (`IsHiddenByFog` / `VisibilityChanged`). Marker and hull must never disagree (cloaked Stealth Corvette, fog edge).
- Hide happens in the same tick the hull hides; while hidden the marker has no hit target and does not join any layout.
- Squadron markers keep point vision at 10 Hz, exclude cloak, and respect hull fog concealment. Cloak and hull hide events invalidate membership immediately. Owner is bound for the entity lifetime.

### 3. Logical squadron anchors
- Store one presentation anchor per squadron identity; initialize from its current centroid on spawn/load.
- Squadron entity supplies order intent and existing centroid to the presentation service. Components never command siblings or inject the owning entity.
- Movement: advance toward the current movement leg at a bounded presentation speed, corrected toward the sampled centroid when lag exceeds a tether distance. Never jump to a distant destination.
- Idle/guard: retain the idle/guard center through fighter orbiting; gradually recenter only when centroid displacement exceeds a dead zone.
- Combat: hold the engagement center through individual attack runs; slowly recenter as the whole engagement moves. Clamp drift against the existing centroid without scanning fighters again.
- Track a damped presentation velocity per anchor; it feeds the moving-together rule.
- Define order-change, target-death, retreat, launch/landing, teleport/load and respawn resets explicitly. Reset discontinuities; do not interpolate across the map.
- Gameplay pause freezes anchor progression (`Time.deltaTime` = 0 already does). Camera reprojection remains responsive while paused.
- Keep `Squadron.WorldPosition` and the fighter-root transform unchanged. Expose the icon anchor only to presentation.

### 4. Proximity and stable groups
- Detect proximity from logical world anchors on the battle plane, never from fighter positions or displaced icons.
- Apply the Grouping Requirements: distance + velocity-compatibility join with dwell; distance or velocity-divergence leave with dwell. Same rules in idle, movement and combat.
- Maintain a spatial hash and persistent group IDs; evaluate only nearby cells.
- Use bounded-radius groups around a stable seed/center, not unlimited transitive chains where A-near-B-near-C joins distant fleets.
- Retain existing members while eligible; deterministic entity-ID tie breaks for ambiguous joins. Evaluate whole eligible groups for merge; split disconnected/out-of-radius members after hysteresis.
- On merge retain the dominant group's ID/slots; place incoming members in free cells. On split preserve relative cells within each surviving subgroup, then recenter its presentation.
- Hidden enemy units never contribute to visible group centers, counts or spacing. Visibility derives from gameplay visibility (section 2), never icon position.
- Maintain world membership independently of camera panning; cull display work separately.

### 5. Grid and smoothing
- Assign persistent integer row/column cells keyed by entity ID. Nine newly grouped units form a centered 3×3 arrangement.
- Fill vacant cells before expanding into outer rows/columns. Do not recompute row width from sqrt(count) every tick or compact holes after every death.
- Keep established cell coordinates until membership changes require reassignment. Retain holes temporarily; compact only after a settled membership interval.
- Project group center once (keep the 60 px vertical offset); apply grid offsets in screen/canvas units so zoom and camera angle cannot collapse spacing.
- Pitch and hit size:
  - Pitch = (visual size + gutter) × zoomScale; proposal 72 + 12 = 84 px at mid zoom.
  - Grouped click area = cell (pitch × pitch): cells tile with no overlap and no dead gaps.
  - Standalone markers keep the full 132 px click square.
  - Avoid pitch ≥ 132 px: at zoomScale 1.5 that is ~200 px per slot and ~600 px for 3×3.
- Damp group center and grid expansion coherently; stable members share group translation rather than following independent crossing trajectories. A moving grid translates as one block.
- Entering/leaving icons ease between standalone and grid positions; hit area interpolates between standalone and cell size. Pointer-down captures entity identity until release; slot changes never retarget a click.
- Keep the whole grid inside the usable battle viewport where possible; never clamp individual icons onto the same edge.
- If grids overlap, place their bounding rectangles in nearby free screen cells with retained placement preference; do not merge world-distant squadrons merely because of camera projection.
- Ship markers vs grids: ship markers stay on their hull (never displaced). A squadron grid overlapping a ship marker shifts to the nearest free placement with the same retained preference. Transient overlap hit priority: ship marker first (smaller, fixed), then grid cell; deterministic.
- Respect HUD/input blockers. For a group too large to fit, reduce spacing only down to the readable/hit-safe minimum; partition into adjacent grids, never collapse into an unselectable count badge.
- Resolve transient overlapping hit rectangles by displayed draw order and nearest center deterministically; settled slots must have non-overlapping hit areas.

### 6. CPU and jobs
- One battle-scoped coordinator and reusable dense buffers; no per-icon Update, physics overlap queries, LINQ, per-frame allocation or per-fighter work.
- Snapshot cached anchors/velocities/IDs/visibility once. Burst jobs handle numeric spatial bucketing, candidate grouping (distance + velocity test) and target-layout calculations; Unity objects, facade resolution, vision queries and UI writes stay on the main thread.
- Use a deterministic Burst `IJob` for stateful group/slot reconciliation initially; parallelize independent projection/anchor math only when measured population justifies scheduling overhead.
- Rebuild grouping at 10 Hz, with event invalidation for spawn/death/visibility changes; never replay missed ticks after a hitch. Hide/unregister dead or concealed markers immediately.
- Squadron vision checks run in the 10 Hz snapshot, not per frame per icon.
- Bound candidate work in dense cells: search retained group representatives first and assign members without materializing every pair. Record candidate counts and dense-cell worst cases; spatial hashing alone does not guarantee linear cost.
- Schedule batches early and consume completed results later. Keep the previous valid layout if a batch is late; use generation/version checks so removed/reused IDs cannot receive stale output.
- Persistent native buffers grow outside steady-state work; complete dependencies before resize/remap/disposal. Mark read-only inputs and prevent main-thread access during job ownership.
- Smooth/apply only visible markers each render frame, retaining the existing 0.5 px write threshold. Cache camera matrices and icon dimensions centrally.
- Do not assume Burst solves canvas rebuild costs. Profile snapshot, vision, grouping, completion wait, projection, UI apply, canvas rebuild and input queries separately.
- Jobs documentation reference: [Unity native container ownership](https://docs.unity.com/en-us/engine/6000.6/manual/scripting/programming-distribute-work-threads/job-system/thread-safe-types/native-container). Check API usage against the installed Editor/packages.

## Important Values
- Initial tuning, unmeasured in battle: 10 Hz grouping; 70 world-unit join radius and 22 units/s relative velocity. Leave at 94.5 units or 33 units/s. Join dwell 0.2 s; leave dwell 0.3 s. Sparse outer rings can compact after 1.5 s settled membership.
- Proposed grid: gutter 12 px; pitch 84 px × zoomScale; grouped click = cell; standalone click 132 px × zoomScale.
- Proposed benchmarks: 32 / 128 / 512 visible markers, plus at least 321 squadrons + 83 ships to cover a previously documented crowded-battle scale.
- Include all markers in one neighborhood, many separate groups, a moving group, a group crossing another, rapid churn, camera zoom/pan and mixed owner visibility.
- Proposed acceptance budget on recorded target hardware: 0 B/frame steady-state managed allocation; ≤0.5 ms p95 total main-thread marker cost at 512 visible markers, including vision and UI contribution.
- Budget is provisional and unmeasured. Record worker time, completion stalls, p99 spikes and buffer memory; compare identical before/after scenes and camera conditions in a Development Player.
- Grouping complexity follows squadron count, not fighter count. No per-frame global all-pairs search.

## Edge Cases
- Idle group receives one shared move order → same grid, same slots, translates; stops → same grid.
- Two squadrons given different move orders from one idle group → they leave the grid after velocity divergence + dwell, not instantly.
- Fast squadron flying past an idle group → never joins (velocity tolerance + join dwell).
- Moving group catches an idle group and stops → merge; dominant group keeps its slots, incoming members take free cells.
- Cloak/fog: hidden enemy Stealth Corvette exposes neither marker nor hit target; marker follows the hull's `FogVisibilityComponent` state. Friendly cloaked markers stay visible.
- Visibility removal takes effect immediately even while an old layout job is running. Do not briefly show stale enemy markers after cloak.
- Destruction, pooling and battle unload release marker registrations, group membership and native storage safely.
- Single survivor returns smoothly to its own anchor. Reinforcements take free slots without moving established selections.
- Selected/hovered markers retain identity through merge, split and movement; overlapping hull and icon candidates are deduplicated.
- Verify UI scale, ultrawide, minimum resolution, camera cuts, behind-camera anchors, screen edges and pause/speed changes.
- Layout is local presentation state; reconstruct after loading/network spawn. Do not serialize or replicate screen slots.

## Files
- Existing: `Assets/Scripts/Components/Squadron/Icon/`; `Assets/Scripts/Components/Squadron/Flight/`; `Assets/Scripts/Entities/Squadron/Squadron.cs`; `Assets/Scripts/Entities/Squadron/SquadronPilot.cs`.
- Visibility: `Assets/Scripts/Components/FogOfWar/FogVisibilityComponent.cs`; `Assets/Scripts/Entities/BaseEntity/EntityDetection.cs`; reference `Entities/MiniMap/Presenter/MiniMapUnitMarkerPresenter.cs`.
- Integration: `Assets/Scripts/Services/Selection/SelectionQuery.cs`; `Services/VContainer/Registrations/SkirmishServiceRegistrations.cs`; `Editor/Squadron/SquadronViewPrefabBuilder.cs`; ship entity bindings; approved ship and squadron prefabs.
- Ship data: `Assets/Settings/Data/Ship/**/<Ship>ShipData.asset` (`ShipClass`).
- Existing regression locations: `Assets/Scripts/Tests/Editor/Squadrons/SquadronIconPrefabTests.cs`; `Assets/Scripts/Tests/Editor/BattleInput/SelectionQueryTests.cs`.
- Guidance: [[Rules/UI_UX_GUIDELINES]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]], [[Architecture/PROJECT_ORGANIZATION]], [[TODOs/Optimization/Late_Battle_CPU_Fix_Plan]].
- Before new UI implementation read UI_CODE_BUILD_GUIDE; before new asset folders read PROJECT_ORGANIZATION; before imports/Addressables work read ADDRESSABLES_BUNDLE_GUIDE. Preserve Addressables structure.

## Implementation record

- Shared `BattleMarkerLayer` and displayed rectangles serve ship/squadron click, marquee, hover and orders. Captured input validates the original entity and registration on release.
- Squad anchors use the pilot's virtual leader or loiter center. Combat uses the tethered centroid; clients use their replicated centroid and order intent. Spawn, pool reuse and large discontinuities reset the anchor. Gameplay positions are untouched.
- Burst spatial reconciliation keeps membership, slot state and dwell across snapshots. Hidden or removed IDs invalidate published and in-flight results. Grid layout is per exact owner, with stable cells, whole-grid viewport clamping, and best-effort separation from other grids and ship markers.
- A fully congested viewport can still overlap. Prioritized hit testing and repeat-click cycling handle remaining overlaps. Minimum pitch is 42 screen pixels; oversize sets are partitioned.
- Static asset verification: Unity 6000.4.7f1 compiled, explicitly reserialized and saved 12 ship data assets and `SceneContext.prefab`; read back all 12 opt-ins and the shared material binding.
- Regression sources cover grouping/slot retention, pass-through, split/merge, vacancy, stale snapshots, displayed hit rectangles, pooled click identity, cloak and asset references. Automated tests were not run under the local override.
- Reusable guidance → [[GameDesign/Commands_And_Controls]], [[GameDesign/Visibility]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].

## TODO

1. [x] Set the explicit 12-ship roster.
2. [x] Share marker layer, display bounds and input queries.
3. [x] Bind ship fog/cloak and sample squadron vision centrally.
4. [x] Add bounded, presentation-only squadron anchors.
5. [x] Add proximity, hysteresis, stable slots and screen layout.
6. [x] Add reusable Burst snapshots and profiling scopes.
7. [x] Author regression coverage.
8. [x] Verify Unity compile, import and saved references.
9. [ ] Live battle visual/readability and benchmark acceptance → [[TODOs/User_Acceptance_Queue#Small Ship Icons and Squadron Icon Grid]]. 0 B/frame and ≤0.5 ms p95 remain targets.
10. [x] Update current controls, visibility and profiling guidance.
