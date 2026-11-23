---
type: engineering-reference
system:
  - ui
verified: 2026-11-16
verification: Read the shared menu button variants and sampled Skirmish prefab
  colors, dropdown states, typography and layout. No runtime review for this update.
---
# UI/UX Recipe Manual

- Mandatory standards: UI/UX architecture and visual design.
- `Assets/ThirdParty/MPUIKit` is an optional tool. Use it when it helps the design; conversion to MPUIKit is not required.

- Codex, Claude, Gemini, and Antigravity **MUST** read and follow this note before creating, modifying, or refactoring UI prefabs/components.

## Visual Refresh Override · 2026-11-12

- Standard rectangular actions across all 11 menu screens inherited font, tint and interaction states in the 2026-11-12 review (47 instances).
- Initial size adjustment: common menu labels 27 → 23 TMP units; small authored text 10–17 → +2 TMP units. These values are not rendered glyph heights; full viewport/combat acceptance remains with [[TODOs/Refactoring/RTS_UI_Refresh_Plan]].
- Verification: saved prefab inheritance/references and 1920×1080 isolated menu renders. Do not treat prefab previews as live menu-route, online availability or battle readability validation.

## 1. Component Choice (Optional MPUIKit Integration)

- **Choose components for the design**: Standard `UnityEngine.UI.Image`, suitable scalable sprites, and `MPUIKIT.MPImage` are allowed on panels, popup cards, buttons, dropdowns, slider tracks/fills, and dialog containers.
- Use `MPUIKIT.MPImage` when procedural geometry is useful; do not convert existing components solely to satisfy a toolkit rule.
- **No Legacy Bitmap Sprites**: Do not use low-resolution or fixed bitmap sprites that scale poorly.
  - Remove redundant child `Background` GameObjects containing legacy sprites.
  - When using `MPImage`, it can render shapes, fills, corner radii, outlines, and falloff directly.
- **Corner Radii (use `Rectangle.CornerRadius` when using MPUIKit)**:
  - Root Panels: `Vector4(16, 16, 16, 16)`
  - Sub-Cards & Action Buttons: `Vector4(10, 10, 10, 10)`
  - Input & Dropdown Boxes: `Vector4(8, 8, 8, 8)`
  - Divider Lines & Track Fills: `Vector4(1, 1, 1, 1)`

## 2. Button Transition & SpriteState Rules

- **Use `Color Tint` (`m_Transition: 1`)**: All `UnityEngine.UI.Button` components **MUST** have transition set to `Color Tint` (`m_Transition: 1`).
  - **NEVER** use `Sprite Swap` (`m_Transition: 2`).
- **Clear `m_SpriteState` Sprites**: When converting UI buttons to procedural `MPImage` backgrounds, all sprites in `Selectable.m_SpriteState` (`m_HighlightedSprite`, `m_PressedSprite`, `m_SelectedSprite`, `m_DisabledSprite`) **MUST be cleared to `{fileID: 0}`**.
- **Rationale**: If `m_Transition` is set to `Sprite Swap` (`2`) or `m_SpriteState` retains legacy bitmap sprite references (such as `guid: 8d8dc128f5ad49d41b7b43b0cc943b9c`), Unity's `Button` component will swap in the old bitmap sprite image whenever the button is hovered or clicked, hiding the procedural `MPImage` visuals.

## 3. Existing Panel MPImage Preservation Rule

- Preserve authored border colors, widths and corner radii on existing panel `MPImage` components (`MiddlePanel`, `ShipActions`, `Panel`, `UpperPanel`).
- **Rationale**: Existing panel containers have curated structural border outlines and corner radii designed for HUD layout alignment.
  - UI refactors must focus on buttons, controls, and legacy bitmap conversion without disturbing intentional panel container styling.

## 4. Palette and Typography

| Role | Color |
|---|---|
| Navy panel base | `#101A29` |
| Slate surface and common button fill | `#202E40` |
| Common outline and dropdown fill | `#344357` |
| Enabled menu text | `#F2F0E6` |
| Primary action, selection and header divider | `#D2A95E` |
| Information, shield and value accents | `#6DB8E2` |
| Warning and destructive accents | `#E86E6C` |

- Preserve health and team colors, artwork tint, authored alpha and border geometry.
- Penpot font roles: Rajdhani 600 HUD, Rajdhani 700 menu headings, Inter Tight 700 menu controls and Inter Tight 500 supporting menu text.

## 5. Component Design Rules

### Menu Buttons

- Shared prefabs in `Assets/Prefabs/Ui/ButtonsTemplate/`: `CommonMenuButton.prefab` uses off-white text on slate, `PrimaryMenuButton.prefab` uses navy text on gold, `DestructiveMenuButton.prefab` uses a red outline.
- Inherit typography, tint and interaction states. Override labels, tooltip keys, layout and availability on instances. Main Menu QUIT uses common and Pause EXIT uses destructive. Close/X controls and compound list rows have specialized layouts.
- Every call-to-action (confirm, accept, invite, start, return, buy) is a nested `PrimaryMenuButton` instance; its secondary partner (decline, refresh, turn off) is `CommonMenuButton`. Never hand-build a gold/cyan/navy action button in a screen, popup or HUD.
- Compact HUD instances may override size and enable label autosize (`fontSizeMax` = authored size, `fontSizeMin` 6). Fill, outline, font asset and font color stay inherited.
- Templates default to `Navigation.Mode.Automatic`; battle prefab instances override it to `None` (`BattleInputPrefabTests.BattleHudControls_DoNotAcquireKeyboardFocusOrNavigate`).
- 2026-11-23: converted `AcceptPopupUi`, `SteamInvitePopupUi`, `EndGameUi`, `HintUi`, `SurvivalHudUi`, `SimulationHudUi` and the `CoreGameUi` orbital "Buy charge" buttons (15 instances).

### Header Banners (`HeaderBanner`)

- **Header Title Label Rule**: Header banners must **NOT** look like clickable buttons (no rounded box shape or border outline on the banner container).
- Use a legible title label and a thin gold `HeaderDivider`. The Skirmish divider is `1` canvas unit high.
- Skirmish section titles start at x `36` and omit numeric prefixes. Battle Settings and Players panels both use height `712` canvas units.

### Close Buttons (`CloseButton`)

- Preserve the specialized close control layout and outline. For a circular MPUIKit control, use `MPImage` with `DrawShape.Circle`.
- Use a dedicated `CloseButtonText` (`TextMeshProUGUI`) child with an off-white ASCII `X`. The Skirmish close icon uses `24` TMP units.

### Dropdowns (`TMP_Dropdown`)

- Use off-white captions and option labels with autosizing for long values. Skirmish controls commonly use `6`–`24` TMP units.
- Control fill `#344357`, outline `#6D8BA8`, procedural chevron. Hover/focus `#415670`, pressed `#202E40`. Disabled fill tints RGB by `0.75` with alpha `1`; preserve separate runtime content fading.
- Popup surface `#202E40`. Rows share the control fill and states. Selected checkmarks use gold.

### Money & Value Controls (Sliders)

- For numerical ranges (e.g. Starting Money), prefer a Slider over a Dropdown. MPUIKit styling is optional.
- Use the value accent for `StartingMoneyValueText`. The saved Skirmish control uses a `120 × 30` canvas-unit label with autosizing from `6` to `40` TMP units.

## 6. Class Inheritance & Serialized Image Binding Safety

- **BaseUi Inheritance**: Only top-level MVP view containers (such as `CoreGameUi`, `EconomyUi`, `FactionUi`, `MenuUi`, `MiniMapUi`, `ReinforcementUi`, `ShipUi`, `SkirmishPopupUi`) inherit from the non-generic `BaseUi` (generic `BaseUi<TModel, TCommand>` no longer exists).
  - Sub-item components (`FactionUnitUi`, `PipelineView`, `MarkView`) MUST inherit directly from `MonoBehaviour` to keep feature dependency setup owned by the top-level view controller.
- **Serialized Image & DOTween Safety**: Do NOT replace standard `UnityEngine.UI.Image` components with `MPImage` on GameObjects where C# scripts have serialized `[SerializeField] private Image ...` fields (e.g. `ReinforcementUi.signalImage`, `CoreGameUi.timeImage`, `ShipUi.shipIconImage`) or DOTween animation calls (`signalImage.DOColor(...)`).
  - Replacing serialized `Image` components breaks script inspector references and causes `NullReferenceException` at runtime.
- **Root GameObject & Icon Preservation**: NEVER add an `Image` or `MPImage` background component directly to the ROOT GameObject of a UI view prefab (`FpsUi`, `ShipSystemUi`, `FactionUi`, `MiniMapUi`).
  - Doing so covers the entire canvas view in a solid dark box, breaking gameplay visuals.
  - ONLY convert explicit child background GameObjects (`Background`, `BackCounter`, `MiniMapRect`, `Panel`, `Back`).
  - NEVER destroy or replace pre-existing icon GameObjects (`Your icon`, `Icon`, `MoneyIcon`, `ShipIcon`).

## 7. Summary Checklist for AI Agents

- Before committing any UI change, verify:
- [ ] Do the chosen components support the design? MPUIKit is optional; conversion is not required.
- [ ] Are all `Button` components configured with `Color Tint` (`m_Transition: 1`) and `m_SpriteState` cleared to `{fileID: 0}`?
- [ ] Are authored panel `MPImage` outlines and widths preserved?
- [ ] Are root GameObjects kept free of background `MPImage` overlays?
- [ ] Are pre-existing icon GameObjects (`Your icon`, `Icon`, `MoneyIcon`, `ShipIcon`) kept 100% intact?
- [ ] Have serialized `[SerializeField] Image` fields on gameplay views (`ReinforcementUi`, `CoreGameUi`, `ShipUi`) been preserved as standard `Image`?
- [ ] Do sub-item views (`FactionUnitUi`, `PipelineView`, `MarkView`) inherit from `MonoBehaviour`?
- [ ] Do controls use the palette and inherit shared menu button states?
- [ ] Are enabled dropdown values off-white and legible?
- [ ] Does `HeaderBanner` look like a clean title label with a divider line (and not a button box)?
- [ ] Does `CloseButton` have a clear ASCII `"X"` text icon visual?
- [ ] Is `fontSizeMin` set to less than 12 (e.g. 6) on all auto-sized text objects?
- [ ] If the user requested tests: do the relevant EditMode categories pass? Otherwise report tests as not run.

## 8. Tutorial UI spotlights

- `HintUi` + `HintSpotlightView`: 65% black dim overlay; rectangular transparent cutout with 6 canvas-unit padding; coach card above overlay. Four UI mesh quads use the standard UI material.
- Resolve fixed/dynamic targets through `TutorialAnchor`; missing/off-screen targets and closed hints hide the overlay. Multiple matching anchors share bounding bounds.
- `UiPlacement` routes `HintUi` to `tooltipSlot`; keep it after tooltip views and below the fade slot. Modal `IInputLock.IsLocked` hides the coach UI.
- `HintDefinition.blockPointerOutsideSpotlight` defaults false. Opt-in blocks pointer input outside cutout; keyboard shortcuts remain available. World-click and reinforcement-drag lessons require non-blocking input.
- World/minimap tutorial markers remain separate from UI spotlight rendering.

- Minimap marker lifecycle: the view is created on route activation after map loading. Tutorial input-lock handling must not call `HideTarget` before showing a marker; `HintUiController` tracks marker visibility explicitly.

- Target emphasis uses `Components/Ui/IUiHighlight` (`Highlight`, `StopHighlight`) and `UiHighlight` on the Graphic itself. Explicitly bind `targetGraphic` on the same GameObject. Pulse = visible colour → opposite hue → back, 1.2 s cosine, unscaled time.
  - Visible colour = `Graphic.color` × material `_Color` × average MPImage gradient (corner colours or gradient keys).
  - Opposite = hue + 0.5, saturation ≥ 0.7, value clamped 0.65–0.8 (dark navy `#0C1D2A` → `#A6632F`); white labels stay readable.
  - `IMeshModifier` lerps vertex RGB to the opposite; `IMaterialModifier` fades `_Color`, `_CornerGradientColor0–3`, `_GradientColor0–7` to white on an owned material copy by the same pulse.
  - Avoid: brightness multipliers (old 1.8–3.5× `_Color`) — blew white-text buttons out to white squares. Preserve source alpha, `Graphic.color`, Selectable states and current stencil/gradient properties; stop returns the normal live material/colors. Destroy the owned material with the component. Do not draw target tint/glow in the spotlight overlay.
- `TutorialAnchor.highlights` binds fixed targets; dynamic build/ability/reinforcement views supply their serialized `tutorialHighlight`. Geometry-only container anchors without a Graphic retain the dim cutout, with no fabricated highlight surface.

- Core HUD hierarchy (corrected after user review, 2026-11-12): panel base `#101A29`; command/time/reserve/camera/orbital control fill `#202E40`. Faction production cards use the same flat `#202E40` control fill, with MPUIKit corner gradient disabled and gold border retained (latest user correction removes the fog-like gradient).
- Enabled icons remain white/alpha 1. Disabled background tint uses neutral RGB `0.75`, alpha `1` to avoid multiplying a dark fill into black; runtime content fading and availability remain separate. Production-panel, ShipActions and minimap HudFrame accent widths remain `0`. The earlier flat-navy-for-everything rule was rejected by the user.

- ShipActions main button labels share fixed font size `11.5`; `ATTACK MOVE` measures `63.83` in its `66`-unit label. Keep secondary target labels and panel heading separate.

- Hover feedback: dark control surfaces → `#415670`, primary gold → `#F6D089`, fade `0.08 s`; use per-channel Color Tint relative to the target Graphic fill. Preserve normal, selected, pressed and disabled state colors. Keep shared menu state styling inherited.

## EditMode UI fixtures

- Camera-space canvas tests that manually render a disabled camera should use a fixed offscreen `RenderTexture`. Release the temporary target after destroying or detaching the camera. Test viewport offsets separately because a texture target renders over its full surface.
- `Canvas.ForceUpdateCanvases` updates layout. A render synchronizes the camera canvas plane after pan, rotation or zoom.
- Input System action fixtures use isolated manual player updates, restrict actions to the simulated device, and restore global settings on teardown. Editor updates alone skip action callbacks.
