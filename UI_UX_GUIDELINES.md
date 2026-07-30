# UI/UX Recipe Manual & MPUIKit Standards

This manual defines the mandatory UI/UX architecture, visual design rules, and MPUIKit procedural styling standards for all user interfaces in this repository. 

Every AI agent (Codex, Claude, Gemini, Antigravity) **MUST** read and adhere to these guidelines whenever creating, modifying, or refactoring UI prefabs or components.

---

## 1. Procedural Component Rule (MPUIKit Integration)

- **Use `MPUIKIT.MPImage` for Popup & Menu Panels**: Use `MPUIKIT.MPImage` instead of standard `UnityEngine.UI.Image` components on panels, popup cards, main menu buttons, dropdowns, slider tracks/fills, and standalone dialog containers.
- **No Legacy Bitmap Sprites**: Do not use low-resolution or fixed bitmap sprites that scale poorly. Remove redundant child `Background` GameObjects containing legacy sprites. Let `MPImage` render shapes, fills, corner radii, outlines, and falloff directly.
- **Corner Radii (`Rectangle.CornerRadius`)**:
  - Root Panels: `Vector4(16, 16, 16, 16)`
  - Sub-Cards & Action Buttons: `Vector4(10, 10, 10, 10)`
  - Input & Dropdown Boxes: `Vector4(8, 8, 8, 8)`
  - Divider Lines & Track Fills: `Vector4(1, 1, 1, 1)`

---

## 2. Harmonious 3-Color Palette System

All UI elements must follow the **3-Color Wheel Harmony Rule** (Dark Low-Brightness Canvas, Muted Structural Accents, High-Contrast Bright Text & Actions). Never use arbitrary or high-luminance canvas backgrounds.

### Color 1: Dominant Dark Base (Low-Luminance Canvas & Containers)
- **Root Panel Background**: Deep Dark Space Navy (`#080C14` / `rgba(3, 5, 8, 0.96)`). Low-brightness dark canvas.
- **Field Cards / Section Containers**: Dark Slate Navy (`#0F172A` / `rgba(6, 9, 16, 0.85)`).
- **Control Boxes & Dropdowns**: Dark Control Box (`#182232` / `rgba(9, 13, 20, 0.95)`).
- **Dropdown List Item Popups (`Template` & `Item Background`)**: Dark Slate Item Row (`#141E2C` / `rgba(8, 12, 17, 0.95)`). **Never leave item backgrounds bright grey!**

### Color 2: Structural Accents & Secondary Labels
- **Field Labels (`FactionName`, etc.)**: Muted Slate Blue (`#94A3B8` / `rgba(148, 163, 184, 1.0)`).
- **Header Accent Line (`HeaderDivider`)**: Glowing Cyan Line (`#38BDF8` / `rgba(56, 189, 248, 0.9)`).
- **Container Outlines**: Muted Slate Blue (`#1E293B` / `rgba(30, 41, 59, 0.60)`).

### Color 3: High-Contrast Bright Text & Action Accents
- **Dropdown Captions (`Label`) & List Item Text (`Item Label`)**: Bright Crisp White (`#F8FAFC` / `rgba(248, 250, 252, 1.0)`). **NEVER use dark greyed text for preview values!**
- **Dropdown Arrows (`Arrow`) & Checkmarks (`Item Checkmark`)**: Bright Accent White (`#F8FAFC`).
- **Primary Action Buttons (`StartGameButton`, `StartDemoButton`)**: Electric Blue (`#2563EB`) with bright glowing cyan/blue border (`#60A5FA` / `OutlineWidth = 1.5f`).
- **Numeric & Value Highlights (`StartingMoneyValueText`)**: Bright Cyan (`#38BDF8`).

---

## 3. Component Design Rules

### Header Banners (`HeaderBanner`)
- **Header Title Label Rule**: Header banners must **NOT** look like clickable buttons (no rounded box shape or border outline on the banner container).
- Style headers as clean title labels (`TitleText`): Font Size = 28, Bold, Uppercase, High-Contrast Cyan/White (`#E2E8F0` / `#38BDF8`).
- Position a 2px high horizontal cyan accent divider line (`HeaderDivider`, `#38BDF8`) directly beneath the title text.

### Close Buttons (`CloseButton`)
- Container: `MPImage` (`DrawShape.Circle`, dark slate `#1E293B`, subtle red border outline `#EF4444`, `OutlineWidth = 1.5f`).
- Visual Icon: Must contain an explicit child `CloseButtonText` (`TextMeshProUGUI`) displaying a bold standard ASCII `"X"` text icon (Font Size = 22, `#F8FAFC`).

### Dropdowns (`TMP_Dropdown`)
- Closed caption `Label` text: `enableAutoSizing = true`, `fontSizeMin = 6` (strictly < 12), `fontSizeMax = 20`, color = `#F8FAFC`.
- Template item `Item Label` text: `enableAutoSizing = true`, `fontSizeMin = 6` (strictly < 12), `fontSizeMax = 20`, color = `#F8FAFC`.
- Template item background: `#141E2C` (dark slate).

### Money & Value Controls (Sliders)
- For numerical ranges (e.g. Starting Money), prefer an `MPImage` procedural Slider over a Dropdown.
- Value display text (`StartingMoneyValueText`): `enableAutoSizing = true`, `fontSizeMin = 6`, `fontSizeMax = initial max` (e.g. 26), text color = `#38BDF8`, container width enlarged (`sizeDelta = (110, 40)`) to prevent overlapping or truncation when values scale.

---

## 4. Class Inheritance & Serialized Image Binding Safety

- **BaseUi Inheritance**: Only top-level MVP view containers (such as `CoreGameUi`, `EconomyUi`, `FactionUi`, `MenuUi`, `MiniMapUi`, `ReinforcementUi`, `ShipUi`, `SkirmishPopupUi`) inherit from `BaseUi<TModel, TCommand>` or `BaseUi`. Sub-item components (`FactionUnitUi`, `PipelineView`, `MarkView`, `FpsCounter`) MUST inherit directly from `MonoBehaviour` to avoid Zenject DI binding failures.
- **Serialized Image & DOTween Safety**: Do NOT replace standard `UnityEngine.UI.Image` components with `MPImage` on GameObjects where C# scripts have serialized `[SerializeField] private Image ...` fields (e.g. `ReinforcementUi.signalImage`, `CoreGameUi.timeImage`, `ShipUi.shipIconImage`) or DOTween animation calls (`signalImage.DOColor(...)`). Replacing serialized `Image` components breaks script inspector references and causes `NullReferenceException` at runtime.
- **Root GameObject & Icon Preservation**: NEVER add an `Image` or `MPImage` background component directly to the ROOT GameObject of a UI view prefab (`FpsUi`, `ShipSystemUi`, `FactionUi`, `MiniMapUi`). Doing so covers the entire canvas view in a solid dark box, breaking gameplay visuals. ONLY convert explicit child background GameObjects (`Background`, `BackCounter`, `MiniMapRect`, `Panel`, `Back`). NEVER destroy or replace pre-existing icon GameObjects (`Your icon`, `Icon`, `MoneyIcon`, `ShipIcon`).

---

## 5. Layout & Paradigm Standards

- **Symmetrical Column Grid**: Organize setup fields into symmetrical 2-column or grid layouts under root panels.
- **Root Panel Containment**: All UI elements must fit inside root panel bounds without overflowing or overlapping adjacent components.
- **Standard Control Heights**:
  - Main Action Buttons: Height = 68px, Width = 340–360px.
  - Field Cards: Height = 52px.
  - Header Divider Line: Height = 2px.

---

## 6. Summary Checklist for AI Agents

Before committing any UI change, verify:
- [ ] Are popup/menu panel `Image` components converted to `MPUIKIT.MPImage`?
- [ ] Are root GameObjects kept free of background `MPImage` overlays?
- [ ] Are pre-existing icon GameObjects (`Your icon`, `Icon`, `MoneyIcon`, `ShipIcon`) kept 100% intact?
- [ ] Have serialized `[SerializeField] Image` fields on gameplay views (`ReinforcementUi`, `CoreGameUi`, `ShipUi`) been preserved as standard `Image`?
- [ ] Do sub-item views (`FactionUnitUi`, `PipelineView`, `MarkView`, `FpsCounter`) inherit from `MonoBehaviour`?
- [ ] Does the UI follow the 3-Color Wheel Harmony System (Dark Base, Muted Accent, Bright Text)?
- [ ] Are dropdown preview values bright crisp white (`#F8FAFC`), not greyed out?
- [ ] Does `HeaderBanner` look like a clean title label with a divider line (and not a button box)?
- [ ] Does `CloseButton` have a clear ASCII `"X"` text icon visual?
- [ ] Is `fontSizeMin` set to less than 12 (e.g. 6) on all auto-sized text objects?
- [ ] Do all EditMode unit tests pass cleanly?
