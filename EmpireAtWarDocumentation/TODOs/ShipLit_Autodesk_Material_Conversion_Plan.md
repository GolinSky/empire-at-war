# Ship Lit: Autodesk Interactive material conversion plan

- Created: 2026-09-28
- Status: Phase 0 approved on 2026-09-28. User approved the verified shader mapping and four shared-material copies; Phases 1–3 are in progress.
- Audience: Codex (or any implementing agent). Read `AGENTS.md` first. This note is advisory: check every claim against the live source and assets before editing.
- Goal: every unit prefab renders with `EmpireAtWar/Ship Lit` so every unit shows team colors, while keeping its current look as close as possible.
- Scope: the 10 Autodesk Interactive materials used by unit prefabs, and a new converter step in `Assets/Scripts/Editor/Rendering/ShipLitSetupTool.cs`.
- Out of scope: changing `ShipLit.shader` feature set (no new keywords), non-unit materials (planets, asteroids fields, VFX), transparent/cut-out materials.

Do the phases **in order**. Each phase must compile, persist its assets, and be verified before the next starts. Do not run automated tests unless the user asks (see `AGENTS.md` → Unity Test Safety). Where this plan says "write tests", write them and leave them for the user.

---

## 1. Background

### 1.1 How team colors work today

- Shader: `Assets/Art/Shaders/Units/ShipLit.shader` (+ `ShipLitInput.hlsl`, `ShipLitForwardPass.hlsl`, `ShipLitDepthNormalsPass.hlsl`).
- One compiled variant for all unit materials: **no per-material keywords**; every map is always sampled with neutral defaults (`white` / `bump` / `black`). This is what lets the SRP Batcher group all unit materials. **Do not add keywords.**
- Team color comes from `MeshRenderer.SetShaderUserValue(uint)` set by `Assets/Scripts/Components/TeamColor/TeamColorView.cs` (0 = no team, n = palette index n−1). Palette: `Assets/Settings/Data/Models/Players/TeamColorPalette.asset`, uploaded by `TeamColorService`.
- Converter: `ShipLitSetupTool` (menus `Tools/Rendering/Convert Unit Materials To Ship Lit` and `Tools/Rendering/Add Team Color Views To Unit Prefabs`). It converts only `Universal Render Pipeline/Lit` and `Complex Lit` materials (89 converted on 2026-09-28). All 20 unit prefabs already have a `TeamColorView`.

### 1.2 Ship Lit material inputs (target format)

| Property | Meaning |
| --- | --- |
| `_BaseMap` (+ `_BaseMap_ST` tiling/offset), `_BaseColor` | albedo × color |
| `_MetallicGlossMap` | **R = metallic, A = smoothness** (URP Lit packing) |
| `_Metallic`, `_Smoothness` | multipliers: metallic = map.R × `_Metallic`, smoothness = map.A × `_Smoothness` |
| `_BumpMap`, `_BumpScale` | tangent-space normal map (`bump` default = flat) |
| `_OcclusionMap` (G channel), `_OcclusionStrength` | ambient occlusion |
| `_EmissionMap`, `_EmissionColor` | emission = map.rgb × color (black color = off) |
| `_TeamMaskMap` (R), `_TeamMaskStrength`, `_TeamRimStrength`, `_TeamRimPower`, `_TeamEmissionTint` | team color controls (keep defaults) |

---

## 2. Evidence (2026-09-28)

### 2.1 Slots per prefab (Autodesk Interactive only)

| Prefab | Slots |
| --- | --- |
| StarDestroyer2ShipView | 299 |
| AsteroidDefendPlatformView | 108 |
| HeavyDreadnoughtShipView | 28 |
| StarDestroyer1ShipView | 18 |
| VenatorShipView | 18 |
| SeparatistSpaceStationView | 3 |
| DefendPlatformView | 1 |

Total ≈ 475 slots, but only **10 distinct materials**.

### 2.2 The 10 materials

Autodesk property names: `_MainTex`, `_Color`, `_MetallicGlossMap` (metallic), `_SpecGlossMap` (**roughness**), `_Glossiness` (scalar used when no roughness map; verify), `_BumpMap`, `_OcclusionMap`, `_EmissionMap`, `_EmissionColor`, toggles `_UseColorMap`, `_UseMetallicMap`, `_UseRoughnessMap`, `_UseNormalMap`, `_UseAoMap`, `_UseEmissiveMap`, and tiling `_UvTiling` / `_UvOffset` (not `_MainTex_ST`).

| Material | Used by | Color map | Metallic map | Roughness map | Normal | AO | Emission | Tiling |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Dull_Metal.mat` | HeavyDreadnought | dull_metal_albedo, color 0.451 | dull_metal_metallic | dull_metal_roughness | off | off | off | 1 |
| `Other/sci-fi-lamps/source/shell.mat` | HD, SD1, SD2, Venator | metal_diffuse, color 0.906 | metal_metalness | metal_gloss (**name says gloss, flag says roughness: verify visually**) | on | lamps_ao | off | 1 |
| `Other/sci-fi-lamps/source/parts1.mat` | HD, SD1, SD2, Venator | metal_diffuse | metal_metalness | off (`_Glossiness` 0) | on | on | off | 1 |
| `Other/sci-fi-lamps/source/parts2.mat` | HD, SD1, SD2, Venator | metal_diffuse | metal_metalness | metal_gloss | on | on | off | 1 |
| `RepublicModels/star-destroyer/source/Materials/ISD_Color_Baked.mat` | SD2 | isd_tile_texture | off | off (`_Glossiness` 0.5) | off | off | ISD_Emission, white | **11 × 11** |
| `Station/gangut-space-hub/source/set1.mat` | Separatist station | stationSet01_albedo | on | on | on | on | on, white | 1 |
| `Station/gangut-space-hub/source/set2.mat` | Separatist station | StationSet02_albedo | on | on | on | on | on (uses set01 emissive) | 1 |
| `Station/gangut-space-hub/source/set3.mat` | Separatist station | StationSet03_albedo | on | on | on | on | on | 1 |
| `Cannons/heavy-turbolaser-cannon-v1/lambert1.mat` | AsteroidDefendPlatform | **`_UseColorMap` = 0** (color only 0.585; map rep_ven_turrets present but ignored) | off | StationSet02_roughness | on | on | on | 1 |
| `DefendPlatforms/XQ6/defaultMat.mat` | DefendPlatform | ScratchedMetal2 | Material.001_metallic | Material.001_roughness | on | ScratchedMetal2Red (**suspicious AO texture**) | on | 1 |

Notes:
- `sci-fi-lamps` materials are **shared** with non-unit content? Check with a reference search before converting; if shared, duplicate the material for units instead of converting in place (see Phase 2.4).
- `lambert1.mat` and `defaultMat.mat` reuse textures from other sets (StationSet02/03). Keep the references; do not "fix" art in this task. List oddities for the user.

### 2.3 Open questions to verify in Phase 0

1. Exact channel each Autodesk map is read from: metallic (R?), roughness (R?), AO (R or G?). Read `Library/PackageCache/com.unity.render-pipelines.universal@*/Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph` (JSON: search for the property reference names and follow the Split / Sample nodes). URP/Lit reads AO from **G**; if Autodesk reads R, the bake must copy R → G.
2. What `_Glossiness` means when `_UseRoughnessMap` = 0: smoothness or roughness scalar.
3. Whether `_UvTiling` / `_UvOffset` apply to all maps (expected) → map to `_BaseMap_ST` (Ship Lit uses one UV transform for every map).
4. Color space and import settings of metallic/roughness textures (must be read as linear data; "sRGB (Color Texture)" off on the baked output).

---

## 3. Target design

For every Autodesk unit material:

- Bake **one new packed texture** `<MaterialName>_MetallicSmoothness.png` next to the material's source textures:
  - R = metallic (from metallic map if `_UseMetallicMap`, else `_Metallic` scalar, broadcast)
  - G = 0, B = 0 (unused)
  - A = smoothness = 1 − roughness (from roughness map if `_UseRoughnessMap`, else from the `_Glossiness` scalar per Phase 0 finding)
  - Resolution: the larger of the two source maps; resample the smaller with bilinear sampling.
  - Import settings: sRGB off, alpha source = input alpha, alpha is transparency off, mipmaps on, same compression class as other mask maps in the project (check an existing `_MetallicGlossMap` texture used by a converted Lit material).
- If AO needs a channel move (Phase 0 answer 1), bake `<MaterialName>_Occlusion.png` with AO in G; otherwise reuse the source AO texture.
- Switch the material to Ship Lit and map properties:

| Ship Lit | Source |
| --- | --- |
| `_BaseMap` | `_MainTex` if `_UseColorMap` = 1, else `null` (white) |
| `_BaseColor` | `_Color` |
| `_BaseMap_ST` | `(_UvTiling.xy, _UvOffset.xy)` |
| `_MetallicGlossMap` | baked packed texture |
| `_Metallic`, `_Smoothness` | 1, 1 (values live in the baked texture) |
| `_BumpMap`, `_BumpScale` | `_BumpMap` if `_UseNormalMap` = 1, else `null`; scale 1 |
| `_OcclusionMap`, `_OcclusionStrength` | AO (source or baked) if `_UseAoMap` = 1, else `null`; strength 1 |
| `_EmissionMap`, `_EmissionColor` | `_EmissionMap` + `_EmissionColor` if `_UseEmissiveMap` = 1, else `_EmissionColor` = black |
| `shaderKeywords` | cleared |

Implementation lives in `ShipLitSetupTool` as a new focused class `AutodeskMaterialConverter` (same `Editor/Rendering` folder, one type per file) so the tool stays under 200 lines. The pure channel math (metallic/roughness/scalar → packed pixel) goes in a small static class `MetallicSmoothnessPacker` that tests can call without assets.

---

## 4. Phases

### Phase 0: Verify semantics (read-only)

1. Answer §2.3 questions 1–4 by reading the shader graph JSON and texture importers. Record the answers in this note under **Findings**.
2. Reference search: for each of the 10 materials, list every prefab/scene/asset that references it (`AssetDatabase.GetDependencies` on all prefabs/scenes, or GUID grep). Mark each as *unit-only* or *shared*.
3. Capture "before" reference renders: for each affected prefab (table 2.1), render off-screen with a directional light (see the preview render approach used on 2026-09-28: preview scene + camera + `RenderTexture`, saved as PNG outside `Assets`). Keep the PNGs for the Phase 3 comparison.

Acceptance: Findings section filled; user confirms any surprising answer (especially `metal_gloss` being gloss vs roughness).

### Phase 1: Packing logic + tests (code only)

1. Add `Assets/Scripts/Editor/Rendering/MetallicSmoothnessPacker.cs`: pure functions, e.g.
   ```csharp
   public static Color32 Pack(float metallic, float roughness)   // → (metallic, 0, 0, 1 - roughness)
   public static Color32[] Pack(Color[] metallicPixels, Color[] roughnessPixels, int metallicChannel, int roughnessChannel,
       float metallicFallback, float roughnessFallback, bool useMetallicMap, bool useRoughnessMap)
   ```
   Channel indices come from Phase 0 findings; name them as `const int` in `UPPER_SNAKE_CASE`.
2. Write (do not run) `Assets/Scripts/Tests/Editor/MetallicSmoothnessPackerTests.cs`: map on/off combinations, roughness inversion, fallback scalars, mismatched sizes rejected (caller resamples first).

Acceptance: compiles.

### Phase 2: Converter

1. Add `AutodeskMaterialConverter` with one public entry point `Convert(Material material, Shader shipLit)`.
2. Reading source textures: they are usually not readable. Do **not** toggle `isReadable` on source importers. Instead blit each source texture into a temporary `RenderTexture` (linear, `RenderTextureReadWrite.Linear`) and `ReadPixels` into a temporary `Texture2D`. Release temporaries in `finally`.
3. Write the baked PNG with `File.WriteAllBytes`, then `AssetDatabase.ImportAsset`, then set `TextureImporter` settings (§3), `SaveAndReimport`.
4. **Shared materials** (Phase 0.2): if a material is referenced by non-unit content, create `<Name>_ShipLit.mat` next to it with `AssetDatabase.CopyAsset`, convert the copy, and repoint only unit prefab renderers to the copy (`PrefabUtility.LoadPrefabContents` → edit `sharedMaterials` → `SaveAsPrefabAsset` → `UnloadPrefabContents`). Otherwise convert in place.
5. Hook into `ShipLitSetupTool.ConvertUnitMaterials()`: Autodesk materials call the new converter; Lit/Complex Lit keep the existing path. Keep the report log: converted / copied / skipped with reason.
6. Idempotency: running the menu twice must not re-bake (skip materials already on Ship Lit) and must not create duplicate copies.

Acceptance: compiles; running on a single material in isolation (temporarily call `Convert` via `unity command eval` on `Dull_Metal.mat`) produces the packed texture and a Ship Lit material with no console errors.

### Phase 3: Run + verify

1. Run `Tools/Rendering/Convert Unit Materials To Ship Lit`.
2. Asset persistence per `AGENTS.md`: `AssetDatabase.Refresh()`, `ForceReserializeAssets(<explicit list of changed .mat/.prefab paths>)`, `SaveAssets()`, check console.
3. Re-render the Phase 0.3 views ("after") and put before/after pairs side by side for the user. Expected differences: none except the team rim. Flag any material whose brightness/gloss changed noticeably.
4. Render one prefab with two different `SetShaderUserValue` values to confirm team colors appear on the formerly Autodesk parts (StarDestroyer2 and SeparatistSpaceStation are the most visible).
5. Re-run `Tools/Rendering/Add Team Color Views To Unit Prefabs` only if Phase 2.4 repointed renderers (renderer lists are unchanged otherwise).

Acceptance: user approves before/after renders; no console errors; `git status` shows only the expected `.mat`, new `.png` (+ `.meta`), and repointed prefabs.

### Phase 4 (optional, ask the user): team masks

`_TeamMaskMap` is black on every unit material, so only the rim/emission tint shows team color. With user approval, author or generate masks for hero ships (start with StarDestroyer2 and the Separatist station). Out of scope unless requested.

---

## 5. Rules for the implementer

- Serena for C# symbol work; Unity tooling (`unity command …`, official `unity mcp`) for assets. Never hand-edit `.mat`/`.prefab` YAML.
- Naming: `const` → `UPPER_SNAKE_CASE`, private fields → `_camelCase`, `[SerializeField]` → unprefixed camelCase, one top-level type per file.
- No constructor null guards, no `?.` on required dependencies, no `GetComponent`/`Find` in runtime code (editor authoring collection is fine).
- Do not add shader keywords or new passes to Ship Lit. If a material cannot be represented, report it instead of extending the shader.
- Do not touch `Assets/AddressableAssetsData` or Obsidian configuration.
- Commits: one phase per commit, no branches unless asked, no AI attribution lines.
- Stop and ask when: a Phase 0 finding contradicts this note, a material is shared, or before/after renders differ visibly.

## 6. Manual verification checklist (user)

- [ ] StarDestroyer2, HeavyDreadnought, StarDestroyer1, Venator look unchanged apart from the team rim.
- [ ] Separatist station and both defend platforms look unchanged apart from the team rim.
- [ ] Two players' identical ships show different team colors in a skirmish.
- [ ] Frame Debugger: unit draws share the Ship Lit SRP Batcher batches (no new variants).

## Findings

### Phase 0 audit — 2026-09-28

Status: read-only audit and seven baseline renders captured. **Implementation is awaiting the confirmation required below because live shader semantics contradict the draft mapping and four materials are shared.** No project scripts or Unity assets have been changed; no automated tests have been run.

#### Verified shader semantics

Source: `Library/PackageCache/com.unity.render-pipelines.universal@7228970dbdbf/Shaders/AutodeskInteractive/AutodeskInteractive.shadergraph`, traced through its serialized nodes and edges. Editor: Unity 6000.4.7f1.

- Metallic: enabled map **R**; otherwise `_Metallic`. Map replaces the scalar.
- Roughness: enabled `_SpecGlossMap.R`; otherwise `_Glossiness`. **Both paths feed Square Root, then One Minus: smoothness = 1 - sqrt(roughness)**. For example, ISD scalar 0.5 gives approximately 0.292893 smoothness, not 0.5.
- `metal_gloss.png` is unequivocally consumed as **roughness** by the current shader when enabled. Its filename does not determine its current rendering. Preserve this behavior; any art correction would be separate.
- Base color: `_UseColorMap` selects **the map OR `_Color`**, not their product. Ship Lit therefore needs white `_BaseColor` when the map is enabled; otherwise preserve `_Color` and use the white default map. Multiplying Dull_Metal's map by its stored 0.451 color would darken it.
- Emission: `_UseEmissiveMap` selects **the map OR `_EmissionColor`**, not their product. With a map, use white target emission color; without a map, use source emission color with Ship Lit's white default emission map (all currently disabled cases have black source color).
- AO: `_OcclusionMap` RGBA feeds the scalar Occlusion input directly, so **R is used**. Confirmed vector4-to-scalar conversion is `.x` in Shader Graph's `GenerationUtils.AdaptNodeOutput`. **`_UseAoMap` is exposed but has no graph node or edge and is ignored by this shader.** Preserve the assigned AO texture regardless of this unused toggle; no AO texture gives the white default. Bake sampled red into target green.
- `_UvTiling.xy` and `_UvOffset.xy` transform the same UV stream for all six maps. Map to `_BaseMap` scale/offset; ISD's 11 x 11 tiling must survive.
- Every assigned metallic, roughness and AO source texture among these ten materials is currently imported with **sRGB enabled**, compressed, and not readable. Preserve the GPU-sampled values (including existing sRGB decoding) in a linear readback and write new outputs with sRGB disabled; changing source import settings would change existing appearance.
- Existing converted Ship Lit mask example: `Mat_Top_Hull_Final.mat` references `Assets/Art/Models/DefendPlatforms/XQ6/ScratchedMetal2.jpeg`, importer compression `Compressed`, max size 2048. Use the same compression class on new packed outputs, with input alpha, alpha-is-transparency off and mipmaps on.

#### Reference audit

Scanned **1,462 prefab, scene and .asset paths** using recursive `AssetDatabase.GetDependencies`, plus exact GUID search of serialized assets and model importer metadata. Confirmed all ten materials and all **475 slots** listed in the plan.

| Material | Classification | Prefab consumers (including indirect model references) |
| --- | --- | --- |
| Dull_Metal | **Shared** | HeavyDreadnoughtShipView; Ui/Reinforcement/HeavyDreadnoughtReinforcementView |
| ISD_Color_Baked | **Shared** | StarDestroyer2ShipView; Ui/Reinforcement/StarDestroyer1ReinforcementView and StarDestroyer2ReinforcementView |
| lambert1 | **Shared** | AsteroidDefendPlatformView; Vfx/Heavy Turbolaser Cannon V1 |
| defaultMat | **Shared** | DefendPlatformView; Ui/Reinforcement/DefendPlatformReinforcementView |
| shell / parts1 / parts2 | Unit-only prefab consumers | HeavyDreadnoughtShipView, StarDestroyer1ShipView, StarDestroyer2ShipView, VenatorShipView |
| set1 / set2 / set3 | Unit-only prefab consumers | SeparatistSpaceStationView |

Imported source models also reference their original materials through importer remaps; these were recorded separately and are not additional non-unit prefab consumers. Full lists, including scene/data dependencies, are preserved in the audit files below.

`lambert1` is additionally reached through capture-site prefabs, SceneContext, SceneData, and Battle/Corusant/Kamino scenes. The three shared reinforcement materials also appear in ReinforcementData through their UI previews. The six unit-only materials have no other prefab/scene/.asset consumers.

Proposed adjustment: create exactly four `_ShipLit.mat` copies and repoint only their unit-prefab renderer slots; convert the remaining six materials in place. The Phase 2 isolated Dull_Metal check must operate on its unit copy because the original is shared. Keep VFX and reinforcement previews on their original Autodesk materials.

#### Baseline evidence

Saved outside Assets at `Logs/ShipLitConversion/2026-09-28/`:
- `review.html`: baseline gallery for all seven affected prefabs.
- Seven `<PrefabName>_before.png` files (1024 x 768).
- `audit.json`: current materials, toggles, texture references and import settings.
- `references.json`: exact GUID reference search.
- `dependencies.json`: full recursive prefab/scene/asset reference lists.
- `shader-edges.txt`: identified shader-graph connections.
- `RenderBefore.cs`, `Audit.cs`, `Dependencies.cs`: repeatable official Unity CLI eval scripts.

Preview rendering uses mesh-only copies in an isolated preview scene, first LOD geometry, a fixed orthographic camera and directional light, and shader user value 0. No gameplay scripts are instantiated. PNGs were inspected for SD2 and the Separatist station. The original MainMenuScene remains clean. No new Unity console errors were captured after baseline cursor 2513; older unrelated disposal errors already existed.

Art oddities retained: lambert1's ignored color texture and cross-set station textures; defaultMat's red AO texture; set2's set1 emission texture. None should be corrected during conversion.

#### Confirmation requested by the plan

Approve preserving the **verified current shader behavior** above (square-root roughness, branch-based colors/emission, always-sampled red AO, current source color-space decoding, roughness interpretation of metal_gloss), and making the four unit-only material copies. Then continue Phases 1–3. The shader feature set and keywords remain unchanged.

