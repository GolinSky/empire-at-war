---
category: Optimization
status: in-progress
created: 2026-10-12
updated: 2026-10-12
---
# UI Canvas Routing Optimization

## Goal
- Reduce shared HUD Canvas rebuild/batching work by separating views with different update frequencies.
- Centralize canvas and layout-slot placement in `UiService`; migrate capture-site build choices to the standard UI lifecycle.
- Preserve layout, appearance, input, gameplay, selection reuse and every UI instance's `CanvasGroup`.

## Rules
- User requirement: `UiService(transform) → Canvas children → UI instances`. The service root has no Canvas; UI instances retain CanvasGroup and have no local Canvas.
- User hypothesis: roughly 99% of UI uses DynamicCanvas, so frequent changes cause excessive shared-canvas work. Source confirms concentrated routing; the percentage and recoverable cost are not measured.
- User requirement: Default for infrequent changes, Dynamic for frequent changes; additional groups only with a concrete purpose. Controllers must not select a Canvas or pass its Transform.
- CanvasGroup controls visibility/input; it does not isolate Canvas batching. A dirty shared Canvas can require batch analysis without every Graphic rebuilding its mesh.
- Follow [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]] and [[Architecture/UI_REFACTORING_PLAYBOOK]]. No visual redesign or forced MPUIKit conversion.

## Decision
- Replace implicit Dynamic fallback with an explicit service-owned `UiType → canvas + semantic layout slot` mapping. Unmapped registered views must fail visibly; preserve existing UiType numeric values.
- Controllers create by UiType and activate routes. UiService resolves/reapplies placement; remove public Canvas Transform access and arbitrary top-level parent overrides after migrating callers.
- Preserve child-widget parenting inside a view. Service placement governs top-level views and route slots, not every card/button.
- Default: CoreGame chrome, MainMenu, Skirmish, Economy, Reinforcement, Faction and Hero. Guard unchanged CoreGame/Faction/Hero output before treating them as low-change.
- Dynamic: MiniMap, Ship, ShipGroup, ShipBuild and Fps. Popup: PauseMenu, Settings, Tooltip, Fade, EndGame and CaptureSiteBuild; assign explicit stacking within Popup.
- UnitOrderFeedback is animated and tracks world positions. Proposed extra sibling FeedbackCanvas below Default preserves its existing behind-HUD order while isolating frequent updates; preserve noninteractive input behavior.
- A separate MiniMapCanvas is a measured follow-up only. Avoid one Canvas per UI, unit, marker or capture site.

## Implementation

### 1. Establish evidence and routing contract
- [x] Inspect live source, route callers, UI rules and prefab structure; record evidence and view-by-view mapping in [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan - Research|Research]].
- [x] Verify UiService has zero root Canvases and three direct child Canvases; all 19 inspected BaseUi prefab roots have a bound CanvasGroup and no local Canvas.
- [ ] Before implementation, record a repeatable battle/UI baseline with current source, camera, population, UI state and resolution; historical timings are not a matched baseline.
- [x] Inventory every CreateUi/SetParent route and service binding; implement one explicit placement map with serialized Canvas/slot references. Avoid a new general configuration framework.

### 2. Move layout ownership with routing
- [x] Replace `ISkirmishUiRoute.Activate(bool, Transform)` and CoreGame controller parent forwarding with semantic activation. A later route activation must not move a view back to the old Canvas.
- [x] Relocate the MiniMap, Content, BuildPipeline, Economy and other required layout anchors into the mapped Canvas branches; keep shared sizing/layout coordination explicit.
- [x] Resolve Content specially: Faction cards are reparented individually; ShipGroup reads the parent viewport and scrolls inside it. Give each mapped content branch its coherent ScrollRect/viewport/mask/layout hierarchy.
- [x] Preserve CoreGame panel sizing, grid/fitter changes, selection mode, clipping, safe areas and resize behavior. No per-view Canvas shortcut.
- [x] Change CoreGame status text, faction availability and hero focusability only when output changes; preserve actual state updates and immediate interaction.
- [x] Migrate explicit UnitOrderFeedback, Tooltip and Fade parenting; route EndGame through standard service placement instead of controller-selected `PrepareEndGameView(parent)`.
- [x] Extend `SetHudVisible` to all HUD/feedback groups; preserve Popup behavior. Define Popup ordering and input blocking for contextual UI, modals, tooltips and Fade.

### 3. Replace capture-site custom UI
- [x] Remove `CaptureSitesSystem → view.InitializeBuildUi(_uiService.DynamicCanvasTransform)` and the gameplay system's screen-UI placement dependency.
- [x] Extract a reusable `CaptureSiteBuildUi : BaseUi` and focused `CaptureSiteBuildUiController`; keep facility buttons as child widgets. Create through UiService/UiType/UiInstaller and normal asset mapping.
- [x] Bind one view to the selected site; current selection permits only one. Controller observes selection/build eligibility and forwards build requests; gameplay model/system remains authoritative for ownership, costs and construction.
- [x] Keep capture rings, rocks, construction visuals and the world-space status Canvas in the world view. Replace only the detached screen-space build panel.
- [x] Preserve click-position placement/clamping, +24 px offset, choices, affordability, tooltips, cancellation, fog/ownership changes and battle teardown; remove detached-panel destruction from the world view.
- [x] Use explicit bindings and normal SetModel/SetPresenter/SetData → Initialize → Dispose lifecycle. Read PROJECT_ORGANIZATION before creating asset folders.

### 4. Verify and measure
- [ ] Play Mode follow-up: verify every registered top-level view resolves to the expected Canvas/slot, including reactivation and scene transitions. Keep `UiType.Interaction` reserved unless an actual view is introduced.
- [x] Preserve the architecture checks for no Canvas on BaseUi, bound CanvasGroups and Show/Hide without GameObject deactivation. Update old tests that enforce custom capture-site parenting or exactly three service Canvases.
- [ ] Check single/group selection, faction scrolling, minimap, production, economy, reinforcements, feedback behind HUD, tooltips, pause/settings, Fade and EndGame; check resized resolution and HUD hide/show.
- [ ] Repeated site selection/build/cancel/teardown must leave no duplicate panels, listeners or stale site references. Warmed selection must continue reusing cards/materials.
- [ ] Run requested Unity tests only with clean-scene preflight. This investigation ran no tests, new battle captures or production changes.
- [ ] Compare ≥3 matched 10-second CSV runs per variant; use Profiler captures only for causes. Track Canvas update/batch/clipping, layout, UI ticks, allocations, draw calls/batches and GPU cost.
- [ ] Accept only measured improvement without visual/input regressions; retain extra Canvas groups only when the benefit justifies extra batches. Confirm representative results in a Development Player.

## Important Values
- Inspected 2026-10-12 at source `e507f0504f8637d18d4a5db7b91f124237ceecfe`; Unity 6000.4.7f1, stopped. Existing uncommitted changes were present.
- Current sort orders: Feedback −10, Default 0, Dynamic 10, Popup 20. Popup sibling slots: Contextual → EndGame → Modal → Tooltip → Fade; saved-asset order verified.
- Historical 2026-10-11 CSV median: **51.99 ms/frame**. Selected UI-related Profiler self samples: **7.695 ms/frame**, including **3.490 ms/frame** in Canvas update/batching/clipping.
- These are historical Editor attribution numbers, not promised savings. World-space UI and per-frame marker work are included in the wider UI total.
- Separate known allocation issue: unused `MPImageHelper.AddQuad` debug formatting → **656,712 B / 40 frames**, ~16.4 KB/frame. Fix/recapture ownership remains [[TODOs/Optimization/Battle_CPU_Fixes_Plan|Battle CPU Fixes Plan]].

## Files
- `Assets/Scripts/BaseSystem/Ui/{UiService,BaseUi,UiType,UiInstaller}.cs`; `Assets/Prefabs/Ui/UiService.prefab`.
- `Assets/Scripts/Services/UiRouting/ISkirmishUiRoute.cs`; CoreGame, Faction, Ship/ShipGroup, MiniMap and other UI route controllers/views.
- `Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs`; `Assets/Scripts/Entities/CaptureSites/{CaptureSiteView,CaptureSitePresenter,ICaptureSiteView}.cs`.
- `Assets/Scripts/Tests/Editor/UiCanvasArchitectureTests.cs`; affected service fakes/lifecycle checks.
- Prefab inventory: `Logs/UiCanvasAudit/20261012/prefab-inventory.json`; full findings and mapping: [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan - Research]].

## TODO
- Implementation complete; Play Mode behavior and matched performance/Development Player acceptance remain open. The user explicitly deferred new performance analysis.
- Verification: Unity compilation successful; **154** saved-asset/preview checks passed, no UI/import/serialization errors in the preview checks. No NUnit runner, Play Mode, battle CSV or Development Player run.
- Evidence: `Tools/UiCanvasRouting/Verify.cs`, `Logs/UiCanvasRouting/verification.json`. Covers all 19 registered UiTypes, popup order, CanvasGroups, HUD visibility, clipping, 3 faction attachment/disposal cycles and 1280×720 / 1920×1080 / 2560×1440 bounds/clamping.
- Captured build choices now use `ICaptureSiteBuildSelection` + `CaptureSiteBuildUiController`; view receives data/presenter explicitly. Existing site model/selection remain authoritative; no duplicate UI model.
- 2026-10-12 user scope: apply the proposed mapping and implementation; do not collect new performance analysis now.
- Scope is canvas routing, associated layout ownership and capture-site UI extraction; do not fold in unrelated world-space UI or gameplay redesign.

- Final stacking: CoreGame chrome draws first; its existing orbital popup uses the service Contextual slot. HUD hiding covers Contextual while modal/tooltip/EndGame/Fade slots remain available.
- Shared Editor observation: radar-unregister `KeyNotFoundException` errors during battle teardown (RadarScanService / RadarComponent / MiningFacility / squadrons). These are outside the changed UI files; no runtime acceptance claimed.
