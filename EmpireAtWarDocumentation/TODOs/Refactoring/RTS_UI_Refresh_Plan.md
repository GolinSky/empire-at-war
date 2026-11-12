---
type: plan
category: Refactoring
status: in-progress
system:
  - ui
updated: 2026-11-12
---
# RTS UI Refresh Plan

## Goal

- First delivery: verify references, then refresh colors, Penpot typography and modest text/control sizing across all project UI; centralize common and primary menu buttons in reusable prefabs/variants.
- Improve decision speed and battle visibility across CoreGameUi, Main Menu and Pause Menu.
- Implement the user-selected navy and gold palette on the existing UI. Keep menu layouts, button order, labels, routes and availability rules.
- Close the verified readability, clipping and feedback gaps, then verify the HUD in a real Skirmish under load.

## Decision

| Role | User-supplied swatch | Use |
| --- | --- | --- |
| Dark navy | `#101A29` | Primary HUD and menu surfaces, dark text over gold controls |
| Muted gold | `#D2A95E` | Selected and primary actions, focus accents, relevant production emphasis |
| Warm off-white | `#F2F0E6` | Enabled labels, key values and neutral information |
| Blue/cyan | `#6DB8E2` | Friendly and informational cues, shields |
| Red | `#E86E6C` | Hostiles, urgent warnings and destructive action outline |

- RGB values sampled from the user's palette attachment. Use related navy shades for surface hierarchy. Keep ship artwork and battlefield visuals as authored.
- Use the exact semantic colors as the reference. Preserve state cues through text, icons, shape or outline. Use navy text on solid gold `PLAY` and `RESUME`.
- The user expanded the visual refresh to all project UI on 2026-11-12. Apply semantic palette roles across menus, settings, lobbies, popups, tooltips and battle HUD; preserve artwork, ownership distinctions and existing behavior.
- Penpot typography verified 2026-11-12: HUD Rajdhani 600; Main/Pause headings Rajdhani 700; menu buttons Inter Tight 700 at design size 23; secondary menu text Inter Tight 500, key hints 600. Ordinary Inter is not Inter Tight. Compare rendered glyph height rather than equating TMP size with pixels.
- Common rectangular menu button prefab + primary variant (`PLAY`/`RESUME`); use a destructive variant where exit styling differs. Keep font, color and state styling inherited; instance overrides only for labels, tooltip keys, placement, size and availability.
- User-selected colors supersede the older electric-blue palette and outline-color prescriptions for this refresh; preserve border geometry and serialized references.
- This is the visual decision for the refresh. [[Rules/UI_UX_GUIDELINES]] still owns shared engineering and styling rules. Update canonical guidance with the scoped palette once integrated and verified.
- Existing HUD zones remain the starting point. Retain the lower-left minimap and adjacent commands, bottom selection/production, and top alert region. Use the attached AoE4-inspired layout suggestions as input to spacing and grouping decisions.
- Main Menu → `PLAY`, `SETTINGS`, `QUIT`. Pause Menu → `RESUME`, `RESTART BATTLE`, `SAVE GAME`, `OPTIONS`, `EXIT`. Preserve Save Game visibility and Restart availability by session mode and authority.

## Files

- [Core Game screens and Pause Menu in Penpot](https://design.penpot.app/?screen=workspace&team-id=19c47d73-0a5d-8067-8008-ba41b7ce6810&file-id=71b39894-c9c5-81cd-8008-ba57254d0595&page-id=71b39894-c9c5-81cd-8008-ba57254d0596) → `09 · CoreGameUi — navy and gold` and `08 · PauseMenuUi — current buttons`.
- [Main Menu in Penpot](https://design.penpot.app/?screen=workspace&team-id=19c47d73-0a5d-8067-8008-ba41b7ce6810&file-id=71b39894-c9c5-81cd-8008-ba57254d0595&page-id=0a9544de-1801-8012-8008-ba893eb18b48) → `04 · MainMenuUi — current buttons`.
- [[TODOs/Refactoring/RTS_UI_Refresh_Plan - Research|Research and audit evidence]] · [[Rules/UI_UX_GUIDELINES]] · [[Rules/UI_CODE_BUILD_GUIDE]].
- Related work → [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]], [[Architecture/TACTICAL_MARKERS]], [[Done/Features/Tactical_Markers_Plan]].
- Unity entry points → `Assets/Prefabs/Ui/SkirmishGame/CoreGameUi.prefab`, `Assets/Prefabs/Ui/UiService.prefab`, `Assets/Prefabs/Ui/MainMenu/MainMenuUi.prefab`, `Assets/Prefabs/Ui/SkirmishMenu/PauseMenuUi.prefab`.

## TODO

1. [x] Record user research, the palette and the Penpot references. → Verified three editable palette boards. HUD variant retained the source geometry, labels and artwork. Menu recolors retained layout, artwork and button order.
2. [x] Finish first-phase verification before asset changes. → Inventory all UI fonts/colors, nested prefab dependencies and runtime styling; compare actual Penpot fonts and size roles. Baseline: Main/Pause had eight inline buttons and no nested button prefabs. The connected Penpot file was verified in the allowed EmpireAtWar/Drafts project on 2026-11-12.
3. [x] Establish shared common/primary menu button prefabs and required variants; convert menu instances without breaking view or tooltip references. Verify style inheritance and absence of redundant style overrides.
4. [x] Apply the palette, verified font families/weights and modest sizing to all project UI surfaces and states. → Saved assets and default menu previews verified against Penpot typography/palette while retaining Unity layout. User acceptance still needed for live focus/hover/pressed/disabled rendering, pause scrim, menu routes and online/offline availability.
5. [ ] Later phase: resolve readability and overflow. → Measure actual glyph height and hit areas. Make overflow discoverable in the fleet tray, compact the minimap and tall tray where needed, and prevent tooltip/orbital overlap from hiding essential controls. Verify the display matrix in the research note.
6. [ ] Later phase: resolve battle feedback and input issues. → Render the real Escape binding in tooltips, distinguish Automatic Power on/off/mixed/paused, and distinguish same-type friendly and hostile markers by more than color. Verify urgent alerts surface promptly during a burst. Check key-binding coverage for commands and abilities, and use the research checklist to define the scope of HUD/text scaling and strong-contrast settings before implementation.
7. [ ] Later phase: verify Unity UI behavior under load. → Inspect effective alpha and raycast targets. Remove unnecessary repeated tooltip layout work and profile while moving across tooltips and cycling fleet selection. Verify contrast and clipping in a real battle with bright VFX and the supported HDR path.
8. [ ] Close the plan after implementation and applicable checks. → Update canonical guidance with verified rules and results, and record any user-only acceptance gates in [[TODOs/User_Acceptance_Queue]].

- Follow-up correction (2026-11-12): original button conversion covered only Main/Pause. Converted another 39 rectangular buttons across PlayUi, SkirmishUi, SettingsUi, OnlineUi, SinglePlayerUi, ServerBrowserUi, SaveBrowserUi, SurvivalUi and SimulationSetupUi. All 47 converted actions now inherit common/primary/destructive styles. Preserved labels, layout, interactability and view/tooltip bindings; specialized close controls and compound list rows retain their structure. Read-back inheritance/reference checks passed; Play/Skirmish/Settings 1920×1080 previews inspected. No new Unity errors; tests not run.
- Implementation evidence (2026-11-12): palette/font pass saved 54 UI prefabs; 11 runtime UI color files updated. Font scan covers 156 UI prefabs / 455 authored font references with no legacy/unknown font references. Common/primary/destructive variants verified; all eight menu instances have no style overrides. Isolated menu previews rendered at 1920×1080. Reusable guidance → [[Rules/UI_UX_GUIDELINES#Visual Refresh Override · 2026-11-12]].
- First visual phase complete, 2026-11-12: Loading.unity refresh/scoped reserialization/save completed after the user stopped Play Mode. Unity idle/not compiling; no new Console errors through cursor 1213. All 156 UI prefabs reloaded without missing scripts or broken serialized object references. Menu inheritance and font/material/atlas persistence verified. Live route/availability, full aspect-ratio and combat visual acceptance remain user checks; tests not run. The plan stays active for unimplemented later phases.

- Expected verification categories → `Ui`, `BattleInput` and `Core` if shared canvas infrastructure changes. Apply the checkout's test execution override. Record actual compile, asset persistence, play checks and tests separately.

- HUD contrast follow-up (2026-11-12): removed the opaque gold/brown corner gradient from FactionUnit cards and bright gradients from 12 CoreGameUi controls; all use flat #101A29. Set 27 icon graphics (including drawn reserve/camera symbol parts and pause/speed icons) to white/alpha 1. Removed HudFrame accentWidth only on CoreGameUi/ShipActions, CoreGameUi/Panel and MiniMapUi/Panel. Preserved borders, selection indicators and disabled/locked behavior. Saved/read-back: 13 backgrounds, 27 icon graphics, 3 strips verified; no Console errors after Editor restart. Preview: `Logs/UiRefresh/hud-contrast-final.png`. Tests not run; live battle visual acceptance remains.

- User review correction (2026-11-12): prior flat `#101A29` treatment rejected as too dark and incorrectly removed gold production emphasis. Corrected 12 CoreGameUi controls to documented control surface `#202E40`; restored FactionUnit's authored gold-to-navy gradient and gold border. Disabled background tint now neutral RGB 0.75 / alpha 1 instead of near-black multiplication. White icons and the three removed strips retained. Thirteen saved styles verified; Core HUD prefab preview inspected; Console clean. No automated tests or live battle visual acceptance run.

### Card gradient and action-label correction · 2026-11-12

- Latest user feedback supersedes the restored-card-gradient decision: `FactionUnit.prefab` uses flat common control surface `#202E40`, consistent with the plan's surface hierarchy. Preserve gold border; disable `MPImage.m_GradientEffect.m_Enabled`. MPUIKit remains for shape/border rendering; its corner gradient caused the fog-like fill.
- `CoreGameUi.prefab` → six ShipActions main labels: fixed size `14` → `11.5`. `ATTACK MOVE` preferred width `77.70` → `63.83`, within existing `66`-unit label. No layout or behavior change.
- Verification: both prefabs saved through Unity and read back; gradient false, card color `202E40FF`, all six label widths fit. HUD preview `Logs/UiRefresh/hud-labels-final.png` inspected; Console error query empty. Tests not run (local override); live battle acceptance pending.
- Reusable current rules updated in [[Rules/UI_UX_GUIDELINES]]. Broader plan remains in progress.

- Hover feedback update (2026-11-12): stronger Color Tint hover on project buttons, input fields, dropdowns, toggles, sliders and scrollbars; dark surfaces → `#415670`, gold → `#F6D089`, transition `0.08 s`. Normal/pressed/selected/disabled colors retained. Shared menu inheritance retained; DebugConsole package UI excluded. Saved 35 prefabs / 212 authored updates; read back 275 controls including inherited instances: all Color Tint, distinct hover, 0.08 s fade. HUD/Main Menu forced-hover previews inspected. No tests run. Console reports a separate battle-map DI circular dependency (`BattleMapLoader → ShipSpawnClearance → ShipNavigationService → MapObstacleContactProvider → BattleMapLoader`); live battle acceptance pending.
