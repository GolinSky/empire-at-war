# Empire At War Editor Hub

Open **Tools > Empire At War > Editor Hub**. Category tabs, grouped tool lists and category search provide one entry point for project tools. Window tools render inside the hub; commands call their existing Unity menu handlers and preserve their validators.

Open window tools use the full content area with a compact **Switch tool** menu. The launcher sidebar and duplicate headings are hidden while editing; switching to a command restores the category browser. The hub preserves Unity's default styles so navigation text and controls render correctly.

The inventory below covers project-owned tools in `Assets/Scripts/Editor`, plus the vendor editors under `Assets/Plugins` and `Assets/ThirdParty`. Unity's built-in and Package Manager editors keep their standard locations.

## Status assessment — 2026-10-09

- **Current** means the tool still serves live project types, assets or workflows. This is a source-based assessment, not a claim about how often anyone clicks it; there is no usage telemetry.
- **Maintenance** means an occasional builder, migration or repair for current content. These remain in their functional category, because infrequent use does not make them obsolete.
- **Legacy / Outdated** means a verified stale path or superseded workflow. All implementations remain available, with the reason shown in the hub.
- Balance Editor is existing, separate uncommitted work. Only its local menu/open routing was adapted; the feature is excluded from the hub commit. The hub discovers it when present and compiles without it.
- Current local catalog: **35 entries**, including Balance Editor; **34** without it. Five entries are Legacy. The hub itself is a separate menu command.

## Full project menu inventory

Every path below is relative to `Tools/Empire At War/`.

| Category / group | Tool or command | Status | Implementation / evidence |
|---|---|---|---|
| Game Design | Balance Editor | Current, separate work | `Balance/BalanceEditorWindow.cs`; data inventory, draft, compare, combat, review/apply views. Embedded UI Toolkit editor. |
| Units / AI | Bake Weapon Loadouts | Current | `AI/WeaponLoadoutBaker.cs`; current ship/squadron data and prefabs; `WeaponLoadoutBakeTests`. |
| Units / Ships | Bake Hull Heights | Current | `Ship/ShipHullHeightBaker.cs`; current ship data and views. |
| Units / Ships | Bake Shield Hulls | Current | `Ship/ShieldHullBaker.cs`; current unit prefabs and shield meshes. |
| Units / Ships | Set Up Ship Hardpoint IDs | Current, manual setup | `Ship/ShipUnitViewEditor.cs`; assigns current `IHardPointProvider` IDs. Select a ship-view root; the original command still requires saving the edited scene/prefab. |
| Units / Squadrons | Build Squadron Views | Maintenance | `Squadron/SquadronViewPrefabBuilder.cs`; rebuilds Delta-7 and Belbullab-22 from explicit specifications. |
| Units / Squadrons | Build A-Wing Squadron View | Maintenance | Same builder; separate A-Wing specification. |
| World / Capture Sites | Build Sites | Maintenance | `CaptureSites/CaptureSiteEditorTool.cs`; rebuilds current site data and prefabs. |
| World / Capture Sites | Rebuild Site Prefabs | Maintenance | Same tool; prefab-only workflow. |
| World / Capture Sites | Apply Obstacles And Mini Map Icon | Maintenance | Same tool; obstacle and mini-map setup. |
| World / Capture Sites | Build Battle Asteroid Assets | Maintenance | `CaptureSites/BattleAsteroidAssetBuilder.cs`; current defense-platform view, installer and data paths. |
| World / Capture Sites | Build Asteroid Mine Assets | Maintenance | `CaptureSites/AsteroidMiningFacilityAssetBuilder.cs`; current mining-facility view, installer and data paths. |
| World / Capture Sites | Fix Mining Facility Shield | Maintenance | Same builder; targeted shield repair. A repair command is not automatically deprecated. |
| Rendering / Icons | Generate Ship Icons | Current | `ShipIconGenerator.cs`; writes `Assets/Art/Textures/Ui/Icons/ShipIcon`, updates UI data and factions. |
| Rendering / Materials | Convert Unit Materials To Ship Lit | Maintenance | `Rendering/ShipLitSetupTool.cs`; current `Assets/Art/Shaders/Units/ShipLit.shader`. |
| Rendering / Materials | Add Team Color Views To Unit Prefabs | Maintenance | Same tool; current team-color view setup; `TeamColorViewPrefabTests`. |
| Rendering / Materials | Detect Team Livery Colors | Current | `Rendering/TeamLiveryAnalyzer.cs`; analyzes current unit materials. |
| Rendering / Materials | Build Wreck From Selected View | Current | `Rendering/ShipWreckBuilder.cs`; retains selection validation. |
| Rendering / Materials | Sync Wreck Materials | Maintenance | Same builder; current wreck materials; `WreckMaterialSyncTests`. |
| Rendering / Materials | Report Helper Meshes In Unit Prefabs | Current | `Rendering/UnitHelperMeshStripper.cs`; reports helpers without stripping. |
| Rendering / Materials | Strip Helper Meshes From Unit Prefabs | Maintenance | Same tool; existing removal behavior retained; `UnitHelperMeshTests`. Reorganization itself does not run this command. |
| Diagnostics / Performance | Audit Battle Instancing | Current | `BattleInstancingAudit.cs`; battle rendering audit. |
| Diagnostics / Performance | Capture Battle (10 Seconds) | Current | `BattlePerformanceCaptureMenu.cs`; retains Play Mode validation. |
| Diagnostics / Render Audit | Capture All (2 frames, 1 Frame Debugger frame) | Current | `RenderAuditOrchestrator.cs`; coordinated capture, including Render Graph and Rendering Debugger. |
| Diagnostics / Render Audit | Capture Profiler (2 frames) | Current | `RenderAuditProfilerTool.cs`; bounded profiler capture. |
| Diagnostics / Render Audit | Capture Frame Debugger (1 frame) | Current | `RenderAuditFrameDebuggerTool.cs`; bounded Frame Debugger capture. |
| Diagnostics / Render Audit | Cancel Capture | Current | Existing cancellation and cleanup in `RenderAuditOrchestrator.cs`. |
| Workflow / Asset Mapping | Add Selected Assets | Current | `AssetMappingContextMenu.cs`; retains its `Assets/Add to Asset Mapping` context shortcut and validator. |
| Workflow / Asset Mapping | Rebuild From Addressables | Maintenance | Same tool; current `Assets/Settings/AssetMappingData.asset`. |
| Workflow / Scene Selector | Add to Main Toolbar | Current | `SceneToolbarDropdown.cs`; current `Assets/Scenes` navigation. |
| Legacy / External Apps | Configure... | Legacy workflow | `AutoAppStartupWindow` in `AutoAppStartup.cs`; embedded IMGUI settings. The old default launches Obsidian; current `empire-vault` uses stdio without requiring the app. Existing preferences and automatic-start behavior are unchanged. |
| Legacy / External Apps | Launch Required Applications Now | Legacy workflow | `AutoAppStartup.cs`; retained manual launcher. |
| Legacy / Noise | Generate Main (Low-Freq) Noise | Outdated | `NoiseTextureGenerator.cs`; writes to nonexistent `Assets/Graphics/Shaders`. Current nebula noise is under `Assets/Art/Textures/Vfx/Nebula`. |
| Legacy / Noise | Generate Detail (High-Freq) Noise | Outdated | Same stale output path. |
| Legacy / Volumetric Nebula | Setup Full Vfx | Outdated | `VolumetricNebulaGenerator.cs`; reads/writes retired `Assets/Graphics` paths. The shader is now under `Assets/Art/Shaders/Vfx`, and textures/materials under `Assets/Art`. Update the workflow before reuse. |

## Other project editor extensions

These are contextual UI or implementation helpers, not independent tool windows to move into tabs.

| Extension | Status / access |
|---|---|
| `SubclassSelectorDrawer` | Current property drawer for `[SerializeReference, SubclassSelector]`, used by `ShipAbilityDefinition.settings`; appears in the Inspector. |
| `ToolbarTimeScale` | Current main-toolbar time-scale control: 0%, 5%, 10%, 25%, 50%, 100%. |
| `SceneToolbarDropdown` | Current main-toolbar scene selector; installation command appears in the hub. |
| `AutodeskMaterialConverter`, `AutodeskUnitMaterialConversion`, `MetallicSmoothnessPacker` | Material-conversion helpers used by rendering tooling; no independent UI. |
| `SquadronViewSpec` | Input data for the squadron builder. |
| `RenderAuditCaptureStatus` | Shared capture output/status helper. |
| `RenderAuditRenderGraphTool`, `RenderAuditRenderingDebuggerTool` | Current CLI capture tools, also reached by the hub's Capture All command. Standalone CLI commands remain `render_audit_render_graph` and `render_audit_rendering_debugger`. |
| `Balance/*` supporting types | Existing separate Balance feature: registration, drafts, persistence, adapters, roster/compare/combat/change views. Hosted through its existing window. |

## Vendor editor inventory

Vendor code and conventional Inspector / Assets / GameObject / Window integration are preserved. Presence alone does not establish usage or deprecation.

| Vendor | Editors / extensions | Assessment |
|---|---|---|
| MPUIKit | `MPUIKitUtilityWindow`; `MPUIKitSettingsEditor`; `MPImageEditor`; `MPImageBasicEditor`; `MPEditorUtility` creation/conversion menus | Current optional UI dependency. Utility window: `Window/MPUIKit/Utility Panel`; inspectors remain selection-driven. |
| MPUIKit drawers | `GradeintEffectPropertyDrawer`, `ChamferBoxPropertyDrawer`, `CirclePropertyDrawer`, `HexagonPropertyDrawer`, `NStarPolygonPropertyDrawer`, `ParallelogramPropertyDrawer`, `PentagonPropertyDrawer`, `RectanglePropertyDrawer`, `TrianglePropertyDrawer` | Supporting Inspector drawers; not standalone windows. |
| MPUIKit helper | `EditorGUILayoutExtended` | Editor drawing helper; not a registered custom inspector. |
| Zenject | `SceneDecoratorContextEditor`, `SceneContextEditor`, `ProjectContextEditor`, `GameObjectContextEditor`, `ZenjectReflectionBakingSettingsEditor` | Current dependency/configuration inspectors. |
| Zenject tools | `ZenMenuItems`, `ReflectionBakingMenuItems`, `MpmWindow`, abstract `ZenjectEditorWindow` | Validation, context/script/test creation, reflection settings, pool monitor and editor base. Existing vendor menus retained. |
| Particle Pro FX | `PPFXWindow`, `PPFXRendererLayerEditor`, `PPFXChainReactionInspector` | Vendor VFX authoring UI; package usage was not audited. `Window/Particle Pro FX` remains available. |
| SceneReference | `SceneReferencePropertyDrawer` | Scene-reference Inspector UI; preserved. |
| TutorialInfo | `ReadmeEditor`, `Tutorial/Show Tutorial Instructions` | Template documentation utility, not a production authoring workflow. Low-value in my assessment, but retained in its vendor location. |

## Adding or maintaining tools

- Register project commands below `Tools/Empire At War/<Category>/<Group>/<Title>`.
- `MenuItem` paths are the source of truth. `EditorToolCatalog` discovers handlers and validators; no second command list needs maintenance.
- Apply `EditorToolInfo` to the handler or class to explain its purpose, selection requirements or Legacy reason. Method metadata takes precedence.
- For a menu declared on an `EditorWindow`, the hub infers the window type. If the menu lives elsewhere, supply `typeof(TheWindow)` in `EditorToolInfo`.
- Embedded windows use their existing `CreateGUI` or `OnGUI`, remain alive across category changes, and receive their original lifecycle callbacks. Save/discard requests are forwarded to embedded windows; the hub mirrors their unsaved state.
- This hosting convention is verified for the current Balance and Auto App Startup windows. New windows that assume their own native window geometry or lifecycle need a hosting check.
- Contextual property drawers and vendor tools retain their standard integration.

## Verification

After checking that open scenes are clean:

```powershell
unity command list_open_scenes --json
unity command run_script --file Tools/EditorHub/VerifyEditorHub.cs --entry VerifyEditorHub.Run --timeout_ms 30000 --json
```

- Unity 6000.4.7f1 compilation passed with no compilation errors.
- Navigation smoke check passed for all 35 local entries, seven categories, five Legacy actions, search, existing validator states, both embedded windows and Balance UI preservation across navigation.
- No builder, repair, capture, noise-generation or app-launch command was executed during verification.
- The smoke check leaves the hub open. It does not run the full EditMode/PlayMode suite.
