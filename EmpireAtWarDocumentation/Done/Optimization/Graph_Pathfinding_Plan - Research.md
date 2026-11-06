---
category: Optimization
status: done
created: 2026-11-06
completed: 2026-11-06
---
# Graph Pathfinding — Research

## Verified ship data (2026-11-06)
- 63 `*ShipData.asset`; `NavigationRadius` 60 distinct values, 5.7576–1059.4355.
- `ShipHeightTier` enum: Deep=0, Lowest=1 … Highest=7. Shared tiers + offset 50 → −341, −241, −154, −68, −22, 38, 65, 80; Mandator Deep −570+50 = −520 → 9 heights.
- Quasar Fire r 22.75, Titan r 265.93 (both Lowest −241); Executor r 1059.44 (Deep −341).
- Hull spans are runtime renderer bounds (`ShipMoveComponent.NavigationHullSpan`); not verifiable from assets.

## Packages
| Option | Facts | Verdict |
|---|---|---|
| BurstTriangulator 3.10.0 | MIT; unity 2022.2; Burst 1.8.7, Collections 2.2.0; CDT, holes (manual/auto), refinement, generic `float2/double2/int2`; `UnsafeTriangulator<T2>` for jobs; dynamic `DynamicInsertPoint`, `DynamicRemoveBulkPoint`, `ConstrainEdge` (no constraint removal) | Chosen |
| DotsNav 0.2.4 | Zlib; unity 2021.3; only dep Entities 0.51.1-preview.21; radius-aware dynamic planar navmesh | Port needed |
| Recast/Detour | C++; mature navmesh + straight path | Native plugin cost |

## BurstTriangulator rules
- Validation (`ValidateInput`): positions not duplicated; constraints not intersecting, duplicated or self-looping; constraints not collinear with other positions. Failure → no triangulation, `Status` not OK.
- Output: `Triangles` (clockwise), `Halfedges` (opposite id or −1), `ConstrainedHalfedges`, `Positions`, `Status` (`NativeReference<Status>`).
- Caller allocates outputs; `allocator` arg is internal scratch (`Allocator.Temp` fine inside one job).
- No published numeric benchmarks (images only).

## Sources
- https://github.com/andywiecko/BurstTriangulator ; `Documentation~/manual/advanced/{unsafe-triangulator,input-validation,dynamic-triangulation,input-jobs,output-halfedges}.md`
- https://github.com/dotsnav/dotsnav
- https://github.com/recastnavigation/recastnavigation
- Funnel: https://digestingduck.blogspot.com/2010/03/simple-stupid-funnel-algorithm.html
- Radius-aware triangulation search: Demyen & Buro 2006, "Efficient Triangulation-Based Pathfinding" (TA*).
