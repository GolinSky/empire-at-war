---
category: Bugs
status: done
created: 2026-11-13
updated: 2026-11-13
completed: 2026-11-13
acceptance: pending-user
---
# Planet Decorations Loading and Orientation Fix

## Goal
- Preserve generated planet decorations through battle loading so Kuat docks and Tatooine junk reach the view.
- Preserve imported prefab orientation when applying randomized world yaw.

## Implementation
- User reported zero decorations in Kuat/Tatooine battles after [[Done/Features/Planet_Decorations_Plan]].
- Cause: `BattleMapLoader` rebuilds `MapLayout` to filter capture/mining sites and omitted the optional decorations constructor argument; the replacement layout received an empty list.
- Fix: forward `layout.Decorations` in the rebuilt layout before `MapLayoutView.BuildAsync`.
- Initial seed-7 generator count checks bypassed this loader reconstruction; they did not establish end-to-end visibility.

## TODO
- [x] Forward decorations through loader reconstruction; source verified.
- [x] `BattleMapLoaderHazardTests.LoadAsync_PublishesAndBuildsGeneratedHazardsAfterSiteFiltering` now uses Kuat and asserts nonempty published decorations plus a spawned clone at every generated position.
- [x] Live Kuat observation after the fix: Editor playing; read-only hierarchy inspection found four active dock clone roots (3 `EB_DOCK00`, 1 `EB_DOCK_BREAK`). This confirms runtime spawning for that battle; the user subsequently confirmed Kuat props are visible.
- Observed positions: broken `(1002.53, 8.87, -362.77)`; intact `(1591.34, -14.98, 554.18)`, `(1641.54, -2.14, -598.09)`, `(-1588.12, 2.64, 395.23)`.
- [x] Unity compilation completed: `failed:false`, `errors:[]`; scoped diff check clean. Tests not run under the local override.
- User-only gates → [[TODOs/User_Acceptance_Queue]]: corrected orientation in battle, Tatooine visibility and authored Map regressions on explicit request.
- Reusable rule extracted to [[Architecture/Map_Archetypes#Planet decorations]]: layout reconstruction must preserve decorations; generator count checks cannot establish loader/view delivery.

## Orientation Follow-up
- User confirmed Kuat visibility, then reported broken prop rotation.
- Cause: `MapDecorationView.Spawn` supplied a yaw-only world rotation, replacing the imported prefab root rotation (approximately −90° X).
- Fix: `Quaternion.Euler(0f, spot.Yaw, 0f) * spot.Prefab.transform.localRotation` preserves the import correction beneath world yaw; existing prefab scale multiplication retained.
- `MapDecorationTests.SpawnPreservesImportedOrientationUnderWorldYaw` authored for −90° X import correction, 37° world yaw and scale multiplication. Source inspected; tests not run.
- Latest compile after the rotation fix blocked by unrelated concurrent `Squadron.cs(552,13)` CS0246: `IHealthModelObserver` unresolved. Earlier loader fix compiled cleanly; rotation fix is source/diff-verified only.
- Orientation visual acceptance remains pending; canonical guidance → [[Architecture/Map_Archetypes#Planet decorations]].
