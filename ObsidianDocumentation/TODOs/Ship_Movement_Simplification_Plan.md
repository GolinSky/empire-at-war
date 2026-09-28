# Ship Movement Simplification Plan — Revision 2

## Recorded Status

- Created/revised: 2026-09-23; revision 2.
- Phases 0–2 done and verified; preserve them.
- User rejected the old Model/Presenter/View split; merge back into one `ShipMoveComponent : MonoComponent<ShipMoveModel>`.
- Advisory snapshot; check live source before implementation.

## Decision

- Use the existing Radar/Selection/Health component pattern.
- Pure model owns state/rules; one component owns Unity references and lifecycle.
- Target: component ≤300 lines; model ≤120 lines.
- Merge removes 4 types and approximately 400 lines; avoid new wrappers/interfaces.

## Implementation

1. Rename `ShipMoveView.cs` → `ShipMoveComponent.cs` through Unity; preserve GUID `675872bfa3914db5aaf6d0f8c8d65ba8`.
2. Preserve `hyperSpaceEase`, `lineRenderer`, `bodyTransform`, `logNavigationDecisions` and prefab fileIDs.
3. Merge presenter logic; remove `ShipMovePresenter`, `IShipMoveView`, `ShipVisionRegistration`.
4. Keep one pending destination after verifying all existing write paths; remove duplicate model state/events.
5. Remove unused interface members and duplicate Vector2 overload; preserve callers/signatures otherwise.
6. Update installer/test source; explicitly reserialize/save all 10 prefabs and verify imports.

## Rules

- Keep yaw acceleration, rate-based bank, bounded Bezier curves, and hyperspace-only DOTween.
- Fog vision returns to movement initialization/release; shared RadarComponent is not its owner in this revision.
- Keep recorded mediator behavior; no unrelated `Ship.cs`, AI, avoidance, weapon, or audio changes.
- Write tests without running them unless explicitly requested.
- Recorded commit convention for Phase 3: `movement:`.

## Edge Cases

- Arriving → queue → idle → move; blocked move → radar retry; pursue → deferred; Stop from every phase.
- Two simultaneously needed pending slots → stop and report; do not assume one is enough.
- Preserve route-line selection via `HandleSelection`.
- SD2 aggregate bounds are skewed; approved collider center `(0.1206, 10.4224)` became `(0, 0)` in xz.
- SD1/HeavyDreadnought pivots: offsets 7.89 / 8.98; fixes require user approval.

## TODO

- Optional navigation signature: remove constant `0.5` height tolerance and redundant clearance; replace two flags with one enum only after approval.
- Manual skirmish checks: hyperspace, movement, blocked retry, pursuit, engine slowdown, SD2 pivot.

## Files

- [[Ship_Movement_Simplification_Plan - Research]] — exact component/model contracts, merge steps, source sizes and measurement table.
