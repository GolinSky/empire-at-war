---
type: research
system:
  - ui
updated: 2026-11-12
---
# RTS UI Refresh Research

## Rules

- User inputs → pasted **PC RTS UI/UX — Design Rules & Validation Guide** and the five-swatch screenshot. The screenshot supplies the project palette recorded in [[TODOs/Refactoring/RTS_UI_Refresh_Plan#Decision]]. The guide supplies design and verification targets.
- Minimize permanent HUD coverage. Group resources, production, selection and related actions in stable zones. Keep frequent actions shallow and predictable. Show detail through context or hover.
- Guide layout reference → objectives and resources upper left, match state upper center, selection lower left, commands lower center, minimap and pings lower right. Keep hardpoints, hangars, weapon properties and advanced stats contextual. See the plan for this project's retained HUD zones.
- Show state directly, including toggles. Support beginners with discoverable actions and contextual guidance, and support experts with consistent mouse and keyboard controls and remapping.
- Accessibility → adjustable text or HUD sizing and strong contrast. Evaluate with shape, outline, symbols or text carrying important state alongside color.
- References → [AoE4 principal UX designer case study](https://kevinhustler.me/aoe4), [Microsoft XAG 101 text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101), [WCAG 2.2 text contrast](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html), [WCAG 2.2 non-text contrast](https://www.w3.org/WAI/WCAG22/Understanding/non-text-contrast.html).
- AoE4 reference examples in the user's guide include supported text scaling to 150%, strong contrast, minimap zoom presets and remappable hotkeys. The numerical targets below come from the user's guide and are project starting points. Measure rendered output over the actual scene.

## Important Values

| Check | Target from the supplied guide |
| --- | --- |
| Text at 1920×1080 | Normal 18–22 px rendered glyphs, key values 22–26 px, section titles 26–32 px |
| Hit areas and spacing | Common actions 40–48 px, small targets ≥32 px, padding 12–20 px, gaps 8–12 px |
| HUD height | 15–22% of screen height as the bottom HUD budget |
| Contrast | Normal text ≥4.5:1, large text and meaningful non-text cues ≥3:1 |
| Opacity | Main panels 85–95%, tooltips and context menus 95–100%, secondary panels 65–85%, decoration 20–45% |
| State opacity | Enabled labels/icons 100%, disabled content 40–60%, hover/selection outlines 90–100% |

## Verification Matrix

| Area | Check |
| --- | --- |
| Structure | Stable controls, related actions grouped, battlefield visible, context only when needed, tooltips clear of essential controls |
| Visual hierarchy | Important controls stronger than decoration, text contrast, bright/dark-map opacity, distinct selected/hover/disabled/active states, independent cues alongside color |
| Size and scaling | Actual 1080p glyph height, hit areas, consistent padding/gaps, viewport matrix below, scaling with no clipping or overlap |
| RTS behavior | Single and multi-selection, hull/shield/cooldown/commands at a glance, alert priority and burst handling, friendly/hostile markers, consistent mouse and shortcuts |
| Unity | Canvas/RectTransform/anchors, decoration raycasts, effective parent/CanvasGroup alpha, layout/canvas rebuild cost, VFX and HDR readability |

- Capture 1920×1080 (16:9), 1920×1200 (16:10) and 2560×1080 (common ultrawide). Use 2520×1080 if exact 21:9 is required. Check default scale and the maximum offered scale once implemented.
- Stress state → normal Skirmish with many selected ship types, damaged hull/shields, active cooldowns, three competing alerts, allied pings, an open tooltip and bright weapons fire. Check light and dark backgrounds, and distinguish internal HDR rendering from native HDR display output.
- Capture default, hover, focused, pressed, disabled and active controls. Measure glyph bounds and effective contrast against composed backgrounds. Check intended HUD blocks and battlefield input at their edges.
- Use normal Skirmish for the full HUD stress run. The simulation mode is useful for fleet VFX and has a different feature set.

## Evidence

- One earlier live Skirmish frame at 2560×1440 showed the Obstacle tooltip covering part of the minimap and commands. Live glyph bounds were about 10.7 px on common command labels and 11.6 px on Credits. Scaled command bounds project to about 8.0 / 8.5 / 9.3 px at the three target sizes. Those target-size figures are projections.
- Authored panels fit the three viewport rectangles. The minimap uses 27.0% / 25.6% / 31.2% of height. At 16:10, 14 one-column fleet groups leave only about 12 px of the last card visible until scrolling. The horizontal scrollbar is absent from the authored tray.
- The orbital popup overlaps the command strip by 144×182 canvas units when open. Clear fleet is 32×28, alert dismiss 28×28 and orbital close 22×22 at the reference scale. Empty fleet tray space consumes pointer input in the sampled live frame.
- `TooltipUi.Place` clamps to the viewport and forces a layout rebuild every visible frame. `TooltipBindings.Get` searches rebinding slots while `Ui/Cancel` is outside those maps, leaving Escape hints empty. Three sampled tooltip foreground/background pairs had nominal contrast of 6.3:1–15.0:1. Full-HUD composed contrast remains open.
- Automatic Power conflates enabled, mixed and paused at a glance. Same-type ship markers use color for ownership. Unavailable command content is authored at 30% opacity. The audited settings lacked text/HUD scaling and a strong-contrast control.
- The alert feed caps its model at three visible and eight pending. Urgent alerts can queue behind three Tactical cards. Outgoing transitions can briefly add visible cards, and long messages can autosize to TMP size 10 and ellipsize.
- Evidence boundary → serialized/source checks plus one earlier live frame. Native aspect captures, dense combat readability, ping visibility, HDR and profiler checks remain open.

## Files

- [[TODOs/Refactoring/RTS_UI_Refresh_Plan|Implementation plan and palette]].
- Source anchors → `CoreGameUi.prefab`, `UiService.prefab`, `UiContentLayout.cs`, `ShipGroupUi.cs`, `ShipSelectionGroupUi.cs`, `UiHitTest.cs`.
- Feedback anchors → `TooltipUi.cs`, `TooltipBindings.cs`, `InputBindingService.cs`, `ShipAutoPowerButtonUi.cs`, `MarkView.cs`, `BattleNotificationFeed.cs`, `BattleNotificationCardView.cs`, `BattleNotificationUi.prefab`.

## Implementation Evidence · 2026-11-12

- Allowed Penpot membership verified in EmpireAtWar/Drafts dashboard; file `71b39894-c9c5-81cd-8008-ba57254d0595`.
- Live typography: HUD board `09` uses Rajdhani 600. Main/Pause headings use Rajdhani 700; buttons Inter Tight 700 at design size 23; secondary text Inter Tight 500 and key hints 600.
- Original Main/Pause assets: eight inline buttons, no nested prefab instances. Converted to CommonMenuButton + PrimaryMenuButton/DestructiveMenuButton variants; preserved view/tooltip references and instance labels/layout. No instance style overrides remain.
- Added Inter Tight Medium/SemiBold/Bold and Rajdhani Bold source/SDF assets; retained OFL license. Fonts registered in existing SharedFonts group. Source: [Google Fonts Inter Tight](https://github.com/google/fonts/tree/main/ofl/intertight), [Rajdhani](https://github.com/google/fonts/tree/main/ofl/rajdhani).
- Theme pass saved 54 UI prefabs, including shared bases; legacy font references replaced. Eleven runtime UI files updated so rendered state colors do not restore the previous palette. Team identity and gameplay logic retained.
- Size changes: common menu labels 27 → 23; small authored text 10–17 → +2 TMP units. Existing layouts/hit areas retained.
- Isolated 1920×1080 renders: `Logs/UiRefresh/main-menu.png`, `pause-menu.png`, `hud.png`. These are prefab previews, not live session captures.
- Final persistence: user stopped Play Mode; Loading.unity refreshed, reserialized and saved. Unity idle/not compiling; no new Console errors through cursor 1213. Read-back of 156 UI prefabs found zero missing scripts/broken object references; 455 authored font references use the intended project font families. Test execution not run under the checkout override.

- Follow-up shared-button coverage: 39 additional buttons across all nine remaining MainMenu screens converted on 2026-11-12; 47 total including Main/Pause. All converted controls inherit InterTight-Bold and variant state styling, with no local style overrides. Reference identity, text, size, position and interactability preserved during conversion; saved assets reloaded for verification. Skirmish/Survival start labels moved out of the decoration-only Content transform to match the shared hierarchy; root CanvasGroup and view references retained. Previews: `Logs/UiRefresh/play-menu.png`, `skirmish-menu.png`, `settings-menu.png`. Specialized close controls/list rows remain distinct. No tests run.

- HUD contrast follow-up (2026-11-12): removed the opaque gold/brown corner gradient from FactionUnit cards and bright gradients from 12 CoreGameUi controls; all use flat #101A29. Set 27 icon graphics (including drawn reserve/camera symbol parts and pause/speed icons) to white/alpha 1. Removed HudFrame accentWidth only on CoreGameUi/ShipActions, CoreGameUi/Panel and MiniMapUi/Panel. Preserved borders, selection indicators and disabled/locked behavior. Saved/read-back: 13 backgrounds, 27 icon graphics, 3 strips verified; no Console errors after Editor restart. Preview: `Logs/UiRefresh/hud-contrast-final.png`. Tests not run; live battle visual acceptance remains.

- User review correction (2026-11-12): prior flat `#101A29` treatment rejected as too dark and incorrectly removed gold production emphasis. Corrected 12 CoreGameUi controls to documented control surface `#202E40`; restored FactionUnit's authored gold-to-navy gradient and gold border. Disabled background tint now neutral RGB 0.75 / alpha 1 instead of near-black multiplication. White icons and the three removed strips retained. Thirteen saved styles verified; Core HUD prefab preview inspected; Console clean. No automated tests or live battle visual acceptance run.

### Card gradient and action-label correction · 2026-11-12

- Latest user feedback supersedes the restored-card-gradient decision: `FactionUnit.prefab` uses flat common control surface `#202E40`, consistent with the plan's surface hierarchy. Preserve gold border; disable `MPImage.m_GradientEffect.m_Enabled`. MPUIKit remains for shape/border rendering; its corner gradient caused the fog-like fill.
- `CoreGameUi.prefab` → six ShipActions main labels: fixed size `14` → `11.5`. `ATTACK MOVE` preferred width `77.70` → `63.83`, within existing `66`-unit label. No layout or behavior change.
- Verification: both prefabs saved through Unity and read back; gradient false, card color `202E40FF`, all six label widths fit. HUD preview `Logs/UiRefresh/hud-labels-final.png` inspected; Console error query empty. Tests not run (local override); live battle acceptance pending.
- Reusable current rules updated in [[Rules/UI_UX_GUIDELINES]]. Broader plan remains in progress.

- Hover feedback update (2026-11-12): stronger Color Tint hover on project buttons, input fields, dropdowns, toggles, sliders and scrollbars; dark surfaces → `#415670`, gold → `#F6D089`, transition `0.08 s`. Normal/pressed/selected/disabled colors retained. Shared menu inheritance retained; DebugConsole package UI excluded. Saved 35 prefabs / 212 authored updates; read back 275 controls including inherited instances: all Color Tint, distinct hover, 0.08 s fade. HUD/Main Menu forced-hover previews inspected. No tests run. Console reports a separate battle-map DI circular dependency (`BattleMapLoader → ShipSpawnClearance → ShipNavigationService → MapObstacleContactProvider → BattleMapLoader`); live battle acceptance pending.
