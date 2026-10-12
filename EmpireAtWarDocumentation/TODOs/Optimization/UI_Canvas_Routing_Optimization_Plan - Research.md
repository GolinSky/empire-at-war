---
category: Optimization
status: in-progress
created: 2026-10-12
updated: 2026-10-12
---
# UI Canvas Routing Investigation

## Goal
- Evidence for [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan|UI Canvas Routing Optimization]]; investigation dated 2026-10-12.
- Separate verified source/prefab facts, proposed placement and historical performance evidence.

## Rules
- User: keep UI-instance CanvasGroup components untouched; UiService is a non-Canvas parent of Canvas children.
- User: Default serves infrequent updates; Dynamic serves frequent updates; UiService owns the predefined mapping. Controllers must not choose the Canvas.
- User: replace custom capture-site screen UI with the normal UI architecture, including `view.InitializeBuildUi(_uiService.DynamicCanvasTransform)`.
- A Canvas is a batching boundary; CanvasGroup handles alpha/input, not batching isolation. Dirtying one shared Canvas can trigger batch analysis without rebuilding every Graphic mesh. Splitting does not remove per-view Update/Tick or layout work. [Unity UI optimization guidance](https://unity.com/how-to/unity-ui-optimization-tips)
- More Canvases create independent batches; group by update frequency and ordering needs, then measure CPU, batches and GPU cost. No numerical speedup can be inferred from hierarchy alone.

## Implementation

### Verified hierarchy and creation
- `UiService.prefab`: root Canvas count **0**; DefaultCanvas, DynamicCanvas and PopupCanvas are direct children, all Screen Space Overlay; sort orders **0/10/20**.
- All **19 BaseUi-root prefabs** under `Assets/Prefabs/Ui` inspected: root CanvasGroup present/bound, zero descendant Canvases. This is a prefab inventory, not a runtime UI percentage.
- `UiService.CreateUi(UiType)` unconditionally forwards to DynamicCanvas. The second overload accepts an arbitrary parent; UiInstaller creates the BaseUi prefab via the standard factory/subcontainer.
- `BaseUi` requires CanvasGroup; Show/Hide sets alpha, interactable and blocksRaycasts. Preserve those fields, references and lifecycle.
- Explicit exceptions: UnitOrderFeedback → Default; Tooltip/Fade → Popup; CoreGame reparents its existing EndGame view into Popup; CaptureSites passes Dynamic to a custom screen panel.
- `SetHudVisible` toggles Default and Dynamic only. Any new HUD/feedback branch must join this visibility contract.
- World-space squadron icons, hardpoint overlays and capture-site status UI are separate concerns; service canvas routing does not explain or fix all UI-related CPU work.

### Route and layout dependencies
- CoreGame supplies MiniMap, Content, BuildPipeline and Economy parent Transforms; Reinforcement/SuperWeapon fall back to its root.
- `ISkirmishUiRoute.Activate(bool, Transform)` allows controllers to create/reparent views under those anchors after initial creation. Changing CreateUi's default alone leaves this bypass intact.
- Economy, Reinforcement, Faction, ShipBuild, MiniMap and Ship controllers use route parent arguments; Ship controls both Ship and ShipGroup.
- `FactionUi.SetParent` moves every generated faction card into the external parent. A root-only move would leave its visible content on the old Canvas.
- `ShipGroupUi.SetParent` treats the passed parent as content and its parent as viewport. It requires a coherent scroll/clip hierarchy.
- `CoreGameUi.SetContentLayout` adjusts panel/header size, viewport offsets, grid enablement/cell sizes/columns, fitter and scrolling for faction/single/group modes.
- Proposed placement includes service-owned semantic slots plus their required layout hierarchy. Preserve behavior across activation, selection changes and resolution changes; do not replace CanvasGroups with per-view Canvases.

### Proposed view mapping

| UiType / view | Proposed group | Verified cadence / prerequisite |
| --- | --- | --- |
| CoreGame | Default | Low-change frame/chrome; Tick currently repeats status strings and control values. Guard changed output and relocate frequent children. |
| MainMenu | Default | Menu interaction; no battle-frame display updates identified. |
| Skirmish | Default | Setup choices and selection changes. |
| Economy | Default | Money-change subscription; configured income interval **1 s**, plus purchases/events. |
| Reinforcement | Default | Spawn/reserve/capacity events and panel interaction. |
| Faction | Default | Availability changes with money/queue/state; current Tick polls and refreshes. Avoid unchanged writes; migrate generated cards with their viewport. |
| Hero | Default | Hero add/remove/focusability; Tick currently repeats SetFocusable. Guard unchanged button/alpha values. |
| MiniMap | Dynamic | Moving markers, camera footprint and fog; optional isolated subgroup only after measurement. |
| Ship | Dynamic | Combat health/shield, ability cooldown fills/text. |
| ShipGroup | Dynamic | Group health/ability changes and LateUpdate layout; preserve warmed card/material reuse. |
| ShipBuild | Dynamic | Active production progress display. |
| Fps | Dynamic | Samples each frame; visible FPS event every **0.5 s**. Periodic debug display belongs outside low-change chrome. |
| UnitOrderFeedback | Feedback, below Default | World-to-screen position updates in LateTick and feedback animations while active. Current Default placement is inconsistent with update frequency. |
| PauseMenu | Popup | Modal interaction/order dominates its low update frequency. |
| Settings | Popup | Same modal placement in menu/battle contexts; verify input blocking. |
| Tooltip | Popup | Pointer-following while visible; preserve contextual draw order. |
| Fade | Popup, covering required layers | Animated transient full-screen overlay; central stacking must preserve coverage. |
| EndGame, currently nested | Popup | Existing explicit reparenting; migrate placement/registration through normal service lifecycle without redesigning end-game logic. |
| CaptureSiteBuild, proposed new UiType | Popup, contextual slot | One reusable click-positioned panel; update selected-site state, below blocking modals/Fade. |
| Interaction | Reserved | Enum member exists; no runtime usage or matching prefab found. Do not invent a view. |

- Default is a low-change group, not a promise that its views never change.
- Proposed FeedbackCanvas is a direct UiService child, sorting below Default (for example −10); current feedback is below Dynamic HUD because it uses Default 0.
- Without the Feedback branch, moving CoreGame to Default 0 and feedback to Dynamic 10 would reverse that visual relationship.
- Explicit Popup ordering must cover capture choices, menus/settings, tooltips, EndGame and Fade; creation order alone is not a reliable contract.
- Retain GraphicRaycaster only where interactive graphics need it; feedback remains noninteractive. Do not disable raycasting on actual buttons or scroll regions.
- Repeated setters are verified; repeated mesh rebuilds from identical values are not. Change guards target avoidable formatting/poll output without assuming Unity dirties unchanged values.

### Capture-site exception and target
- `CaptureSitesSystem.UpdateState` calls `InitializeBuildUi(_uiService.DynamicCanvasTransform)` for each site's view before constructing its presenter.
- `CaptureSiteView.InitializeBuildUi` detaches/reparents buildOptions, resets scale/rotation, sets anchors/pivot and **560 × 228** size.
- `SetBuildOptionsPosition` converts the click into parent-local screen coordinates, offsets **+24 px** vertically and clamps the panel to the Canvas rectangle.
- World view owns facility-button handlers and destroys the detached buildOptions in OnDestroy. Presenter combines world rendering with build-panel visibility, affordability and selection.
- `CaptureSitesSystem` keeps one `_selectedSite`; selecting another clears the old selection. One reusable screen view is sufficient.
- Target: gameplay system/model retains capture, ownership, costs, construction and accepted build commands; focused UI controller binds selected-site state to `CaptureSiteBuildUi : BaseUi`.
- Preserve world-view rings/rocks/construction/statusCanvas and its billboard update. Extract only screen build choices; facility option widgets stay child MonoBehaviours.
- Lifecycle gates: Initialize/Dispose, selection switch, outside click/Escape, eligibility or visibility loss, build start/completion, tooltip close and scene teardown.

### Existing checks to preserve/update
- `UiCanvasArchitectureTests.UiScreenPrefabs_UseBoundCanvasGroupsWithoutLocalCanvases`: retain no-local-Canvas and bound-CanvasGroup invariants.
- `BaseUi_ShowAndHide_UpdateCanvasGroupWithoutDeactivation`: preserve behavior.
- `CaptureSiteBuildOptions_UseScreenCanvasAndIgnoreSiteTransform`: replace old direct InitializeBuildUi expectation with service-created panel placement; preserve independence from site rotation/scale.
- `UiServicePrefab_OwnsDefaultDynamicAndPopupCanvases`: retain non-Canvas root and expected Canvas order/bindings; extend explicit expected set for Feedback if implemented.
- Update affected service fakes after interface migration. No tests were run during this investigation.

## Important Values

### Historical performance attribution
- Source of timing: [2026-10-11 battle audit](<F:/Private/empire-at-war/Logs/BattleAudit/20261011T1730Z/analysis.md>), source `e7a1d131` with dirty changes. Current investigation source is later: `e507f050` with existing local changes.
- CSV: **187 frames / 10.008 s**, frame median **51.99 ms**, mean **53.28 ms**, p95 **71.40 ms**. This is the historical whole-frame timing, not Canvas timing.
- Regular Profiler: 90 frames, 1158–1247; table uses selected main-thread **self** samples, averaged over the capture. Truncated top-marker data gives a partial attribution, not a complete subsystem budget.

| Selected UI work | Self ms/frame |
| --- | ---: |
| MiniMapUnitMarkerPresenter.LateTick | 1.646 |
| MiniMapFogMask.Tick | 0.440 |
| SquadronIconComponent.LateTick | 0.851 |
| HardPointOverlayPresenter.Tick | 0.762 |
| CanvasUpdate.PreRender | 1.071 |
| UGUI.Rendering.UpdateBatches | 1.025 |
| ClipperRegistry.Cull | 0.698 |
| Canvas.BuildBatch | 0.696 |
| TooltipUi.LateUpdate | 0.357 |
| TooltipService.Tick | 0.150 |
| Selected UI total | **7.695** |
| Canvas update/batching/clipping subset | **3.490** |

- Global Canvas markers do not identify a specific Canvas. The 3.490 ms is not all attributable to UiService and is not a predicted saving.
- Minimap marker/fog work totals **2.086 ms/frame**; splitting canvases alone does not remove those scripts.
- Allocation capture: **656,712 B / 40 frames** in unused `MPImageHelper.AddQuad` StringBuilder/vertex-UV formatting, **16,418 B/frame**, **21.4%** of captured main-thread bytes.
- Live source still contains the unused builder and AppendLine at `Assets/ThirdParty/MPUIKit/Runtime/Scripts/Utility/MPImageHelper.cs:240–244`. Track its small cleanup separately in [[TODOs/Optimization/Battle_CPU_Fixes_Plan]] so canvas benefit is measured independently.
- No fresh battle was started. Current Unity inspection was stopped/read-only; no new capture, allocation recording, compilation, tests or production changes.

## Files
- Current hierarchy evidence: [prefab-inventory.json](<F:/Private/empire-at-war/Logs/UiCanvasAudit/20261012/prefab-inventory.json>), generated **2026-10-12 07:09:35 UTC**.
- Capture evidence: `Logs/RenderAudit/20261011T173207015Z-profiler/` and `Logs/RenderAudit/20261011T173411830Z-profiler/`.
- Routing: `Assets/Scripts/BaseSystem/Ui/UiService.cs`, `Assets/Scripts/Services/UiRouting/ISkirmishUiRoute.cs`, `Assets/Scripts/Entities/CoreGameUi/{Controller,Ui}/`.
- Content dependencies: `Assets/Scripts/Entities/Faction/Ui/FactionUi.cs`, `Assets/Scripts/Entities/ShipUi/Ui/ShipGroupUi.cs`, `CoreGameUi.SetContentLayout`.
- Custom path: `Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs`, `Assets/Scripts/Entities/CaptureSites/{CaptureSiteView,CaptureSitePresenter,ICaptureSiteView}.cs`.
- Architecture verification: `Assets/Scripts/Tests/Editor/UiCanvasArchitectureTests.cs`.

## Decision
- 2026-10-12 implementation: service-owned UiType mapping, four sibling Canvases (Feedback −10 / Default 0 / Dynamic 10 / Popup 20), semantic route activation, separate Faction/Ship scrolling and explicit Popup stacking.
- Faction cards remain inside their view CanvasGroup; disposal restores the service scroll binding. Ship/ShipGroup use CanvasGroup visibility and retain warmed views/cards.
- EndGame uses normal UiType/UiInstaller registration. One CaptureSiteBuildUi replaces per-site detached panels; gameplay selection/construction authority remains in CaptureSitesSystem.
- CoreGame/FactionUnitUi/HeroIconUi guard unchanged output. Site facility display names are cached when the facility type changes.
- Verification: Unity compilation successful; 154 saved-asset/preview checks passed; no UI/import/serialization errors in the preview checks. Evidence: `Tools/UiCanvasRouting/Verify.cs`, `Logs/UiCanvasRouting/verification.json`.
- User explicitly deferred new performance analysis. No NUnit runner, Play Mode, CSV capture or Development Player acceptance; historical attribution above remains historical.

- 2026-10-12 station-selection fix: faction grid **150×150 → 138×106 px**, matching `FactionUnit.prefab`. Two rows + 6 px gap use **218/220 px**; 30 populated cards fit vertically and retain horizontal scrolling at 1280×720, 1920×1080 and 2560×1440. Compilation and all 154 routing checks passed; no Play Mode run.
