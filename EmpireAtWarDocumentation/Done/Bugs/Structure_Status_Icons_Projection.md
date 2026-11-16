---
category: Bugs
status: done
created: 2026-11-16
completed: 2026-11-16
acceptance: pending-user
---
# Structure Status Icons Projection

## Goal

- Fix missing destruction icons, detached world icons and camera-motion instability reported 2026-11-16.
- Failed visual acceptance follow-up to [[Done/Features/Structure_Status_Icons_Plan]].

## Implementation

- Source inspection: `StructureStatusIconPresenter.Project` converts screen coordinates through the canvas world plane; ignores the conversion success result.
- `BattleMarkerLayer.Construct`: `ScreenSpaceCamera`, same scene camera, plane distance = near clip + 0.05. Existing `BattleMarker.Display` uses screen point minus viewport origin directly.
- Strong cause: camera/canvas transform timing can invalidate the world-plane conversion. Exact runtime timing remains unobserved; Editor was stopped during investigation.
- Projection cache watches camera matrix, pixel rect and canvas rect dimensions, but not canvas transform. A bad conversion can remain cached after camera movement stops.
- Visibility and 2 s acknowledgement use the intended screen point, not actual rendered coordinates. Displaced icons can expire unseen.
- Destruction source path is wired: facility `OnRelease` → `CaptureSitePresenter.ReleaseFacility` → `Destroyed(previousOwner)` → local-player filter → `Model.Show`. No missing subscription identified by source inspection.
- Scope: mining/Battle Asteroid, satellite and relay notifications; current audience is local captures/builds and destruction of local facilities. Stations and enemy-facility destruction are outside the original plan.

## Implementation Outcome · 2026-11-16

- World placement now matches `BattleMarker`: projected screen point (including 42 px vertical offset) minus camera viewport origin. Removed canvas-world-plane conversion and canvas-dimension cache dependency.
- Fresh source revisions force projection: an expired source cannot reuse coordinates from before camera movement while another source stayed active.
- Added `Ui` position regressions for perspective/orthographic pan, rotation, zoom, stationary frame after canvas synchronization, viewport move/resize, and same-anchor reactivation with another source active.
- Tests assert actual `RectTransform.anchoredPosition` and rendered screen position with a real `ScreenSpaceCamera` canvas. Existing model/source/view and Capture event tests reviewed; their contracts remain unchanged.
- Verification: Unity refresh succeeded; compilation finished, `EditorUtility.scriptCompilationFailed = false`; targeted diff whitespace check clean. Tests not run under local restriction. Existing Console errors were historical; current compilation succeeded.
- Reusable coordinate/cache guidance extracted to [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]. Live battle destruction/fog/offscreen-return acceptance and test execution → [[TODOs/User_Acceptance_Queue]].

## TODO

- [x] Use the existing battle-marker viewport-relative coordinate contract for world icons.
- [x] Add rendered-position assertions for pan, rotation, zoom, viewport origin and stationary frame after motion; validate transient acknowledgement against actual display.
- Live battle gate transferred to [[TODOs/User_Acceptance_Queue]]: destruction, fog and offscreen return; confirm whether an independent event-path defect remains.
- [x] Preserve unrelated minimap/combat marker behavior and existing uncommitted changes.
- [x] Verify compilation; tests deferred to user acceptance under local restriction.

## Files

- `Assets/Scripts/Entities/StructureStatusIcons/StructureStatusIconPresenter.cs`: projection 109–120, caching 75–80, clocks 98–106.
- `Assets/Scripts/Components/Squadron/Icon/BattleMarkerLayer.cs`: shared canvas 49–53.
- `Assets/Scripts/Components/Squadron/Icon/BattleMarker.cs`: direct viewport-relative placement 161.
- `Assets/Scripts/Tests/Editor/Ui/StructureStatusIconPresenterTests.cs`: fake layer has no Canvas; assertions cover clocks/active state, not rendered coordinates.
- Reviewed `StructureStatusIconSourcesTests`, `StructureStatusIconModel`, `CaptureSitePresenterTests`, `CaptureSiteModelTests` and facility release wiring. Categories: `Ui`, `Capture`; no source/test edits or test execution.
- Serena unavailable: HTTP connection refused; launcher returned AlreadyRunning. Graphify and live source used. Unity reported ready, not compiling, Play Mode stopped; no runtime reproduction claimed.
