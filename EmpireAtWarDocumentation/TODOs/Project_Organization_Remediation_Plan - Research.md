# Project Organization Remediation Plan - Research

## Goal

- Define exact material, texture, mesh, and configuration destinations from the live checkout.
- Preserve Unity identities, importer remaps, source links, material variants, and shared consumers.
- Supply evidence and execution gates for [[Project_Organization_Remediation_Plan]].

## Important Values

- Audit: **2026-09-30**; checkout: `F:\Private\empire-at-war`; Unity: **6000.4.7f1**; URP: **17.4.0**.
- Inventory: **736 non-meta files** in `Assets/Art/**` and `Assets/Resources/Settings/**`; every file has a corresponding GUID.
- Models: **30**: 12 FBX, 10 OBJ, 6 DAE, 2 BLEND. Previously omitted JediCouncil and SciFiLamps are included.
- Materials: **251**, including **136** under the current Models tree and **71** wreck materials.
- Textures: **407**: 312 PNG, 82 JPG, 9 JPEG, 4 TGA. Dimensions read from every image header.
- Models directory: **467** files: 285 images, 136 materials, 30 models, 10 MTLs, 2 mesh assets, 2 Thumbs.db, 1 lighting asset, 1 credits file.
- Manifest: **602 MOVE/RENAME**, **134 KEEP**, **0 DELETE**. **0** case-insensitive target collisions; **0** currently occupied move destinations.
- Resources settings: **6 URP pipeline/renderer assets + 1 SampleSceneProfile.asset**, not seven pipeline profiles.
- Halo-station: **54 PNGs**, no model; previous count of 40 was incorrect.
- Static audit only: no Editor import, Play Mode, build, scene save, or automated tests executed.

## Rules

- Evidence: live filenames, metadata GUIDs/remaps, serialized references, OBJ/MTL/DAE path strings, C# symbols, image headers, SHA-256 hashes.
- Serialized consumer counts include importer metadata; exclude the asset's own metadata GUID declaration.
- Refs **0** means no scanned serialized GUID consumer, not proven unused. Filename/name-based loads and source-image links require separate checks.
- Material bindings below are serialized assignments; not all saved properties are active in the current shader. Preserve inactive/stale assignments during naming-only work.
- Neutral Slot/Set/Source/Imported tokens retain distinct assets whose visual role is unverified.
- No format conversion, shader/property fixes, deduplication, faction rebalance, prefab hierarchy rename, or destructive cleanup in this migration.
- Preserve file GUIDs and referenced local fileIDs. New folders receive new metadata; referenced old folder GUIDs need explicit mappings before removal.
- The main inventory excludes package-cache contents and embedded FBX/BLEND data; the unresolved-GUID section records a separate package-metadata search. Live Editor verification remains an implementation gate.

## Decision

- **Choose strict type-first placement:** meshes/source companions → `Art/Models/<Category>/<Asset>/`; external materials → `Art/Materials/Models/<Category>/<Asset>/`; model textures → `Art/Textures/Models/<Category>/<Asset>/`.
  - Why: predictable ownership and the guide's type-first roots. Avoid the previous mixture of textures and shared materials inside Models.
- **Keep current gameplay grouping:** RepublicModels → RepublicShips; SeparatistShip → SeparatistShips; stations → SpaceStations; Moon → Planets/Moon.
  - Why: placement/naming only. AWing and Imperial ship models retain current project-side grouping; lore-based faction corrections are out of scope.
- **Materials:** `<Asset>_<PartOrSlot>[_Variant].mat`; **wrecks:** `<SourceMaterialName>_Wreck.mat`.
- **Textures:** `<Asset>_<PartOrSet>_<MapOrSource>[_Variant].<original extension>`.
  - Preserve Republic, HQ, OpenGL, numbered sets/tiles, source origin, ShipLit, and packed-map distinctions.
- **Map tokens:** Albedo, Normal, Emissive, Metallic, Roughness, Height, Specular, Opacity, AO, MetallicSmoothness, Occlusion.
  - AO retains source identity; Occlusion retains generated-map identity. Do not collapse them by filename.
- **Unknown/mixed maps:** keep Source, BumpSource, SpecularSource, MixedAO_SP, ColorMapD, and source-specific tokens; do not invent channel semantics.
- **Keep duplicate copies:** identical bytes do not imply identical GUIDs/import settings/source links. Hash groups are later review candidates.
- **Retain organized assets:** KEEP rows define retained scope; shader/animation filenames and established icon abbreviations are not redesigned.

## Implementation

### Corrections to previous research

| Previous claim | Live evidence | Revised decision |
| --- | --- | --- |
| Delete ArmorTest.png | ArmorShape.mat and its wreck material reference it | Keep; rename Munificent_Armor_Albedo.png |
| Delete unused CaptionRex.jpg | Assets/Scenes/Loading.unity references it | Keep; move/rename Ui/Loading/CaptainRex_Loading.jpg |
| Arquitens gets one Hull material | Two materials have different _BaseMap textures | Keep Republic and SourceVariant materials/textures |
| Acclamator materials are Hull/Engines | Both use the same texture set; importer keys object_0/object_1 | Use neutral Slot00/Slot01 |
| Remove duplicate Venator subfolder | Importer remaps to its three materials; all four TGAs have consumers | Keep six materials; distinguish _Imported copies |
| Rename Venator TGAs to PNG | Four real 1024×1024 TGAs | Preserve .tga; no conversion |
| EngineGlow is an emission map | Stored in thruster _BaseMap | Name Thruster_Albedo; retain shader settings |
| Gangut maps become Set01 | Three sets and 25 textures | Preserve Set01/02/03 and cannon-generated maps |
| Heavy cannon has no textures | Materials reference Gangut/Venator textures | Preserve cross-model assignments |
| MiningFacilityHull belongs to asteroid mining | Uses RefuelingStation/FuelStation.png; consumer MiningFacilityView.prefab | Move under RefuelingStation materials |
| FogOfWar 1 is disposable duplicate | Corusant scene uses it; other material used by Installers/SceneContext; hashes differ | Keep distinct FogOfWar_Coruscant variant |
| Numbered grid images are junk duplicates | Grid05→LineBorderInner; Grid06→LineBorder/LineBorderWall; hashes differ | Keep all; used images get nebula names |
| Coruscant HQ is higher resolution | HQ PNGs: 1254×1254; JPG alternatives: 4096×4096 | Keep both; HQ is a preserved source label |
| Freeport filename containing rough proves roughness | Assigned to _BaseMap/_MainTex | Name Floor_Albedo_Source08; preserve pixels |
| Lucrehulk textures are proven Core/Ring/Details | Slot019→Set03, Slot020→Set01, Slots021/022→Set02 | Keep four Slot materials and three sets |
| Recusant textures are proven Front/Rear | RecA bump, RecB detail normal; geometry role unverified | Use Set01/02 |
| Zero GUID consumers means unused | OBJ/MTL/DAE use paths; importers can search names | Keep until Editor/source-link verification |

### Target layout and boundaries

```text
Assets/Art/Models/<Category>/<Asset>/<Asset>.<model extension>
Assets/Art/Models/<Category>/<Asset>/<Asset>.mtl
Assets/Art/Materials/Models/<Category>/<Asset>/<Material>.mat
Assets/Art/Textures/Models/<Category>/<Asset>/<Texture>.<original extension>
Assets/Art/Materials/Wrecks/<existing unit folder>/<SourceMaterial>_Wreck.mat
Assets/Art/Materials/Vfx/...          shared effects
Assets/Art/Textures/Vfx/...           shared effect images
Assets/Art/Textures/Ui/...            sprites, icons, loading imagery
Assets/Art/Materials/Unclassified/... retained assets awaiting usage review
Assets/Art/Textures/Unclassified/...  retained source images awaiting usage review
Assets/Settings/Render Pipelines/... six URP assets + sample volume profile
Assets/Settings/Lighting/CoruscantLighting.lighting
```

- The guide permits imported materials alongside models; external materials under Materials are a stricter, valid type-first choice.
- Record this layout and neutral-name exceptions in PROJECT_ORGANIZATION before execution. That guide is unchanged by this documentation task.
- Document existing Prefabs/Light; remove the old Light/Lighting contradiction without renaming lighting prefabs.
- Later move `Assets/Prefabs/Installers/SceneContext.prefab` → `Assets/Prefabs/View/ZenjectContext/SceneContext.prefab`.
- Keep distinct Resources/SceneContext.prefab, Resources/ProjectContext.prefab, Resources/DOTweenSettings.asset; no bootstrap merge by basename.
- Live roots: Plugins/Zenject, Plugins/Demigiant, Audio/Music, Audio/SFX, ThirdParty/TextMesh Pro, Art/Shaders.
- Correct template identity and Mirror/VContainer examples in the guide to verified project dependencies; package changes are separate scope.

### Manifest reading

- Every inventoried file has exactly one file-manifest row; dependency/hash tables below may repeat paths as evidence. Sources are relative to the stated source root; targets are relative to Assets/.
- MOVE/RENAME preserves original content/format and GUID; KEEP retains its current path.
- Size is source width×height in pixels. Refs counts serialized consumer files, including importer metadata.
- Recheck source path + GUID immediately before implementation; refresh rows if the checkout changes.
- Implement exact rows, not wildcard rename assumptions. Folder mappings are a separate dependency-aware manifest.

### Asteroid_01 — file manifest

- Source root: `Assets/Art/Models/Asteroids/`.
- The two defaultMat_ShipLit generated images belong to XQ6 material consumers; their target is the XQ6 texture category, not Asteroid01.
- AO/emissive/metallic JPGs share identical bytes; preserve their separate identities and assignments.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Material_001` → `ff43801ad8a50574db1db7d2a0e86aca`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Material_001.mat` | `Art/Materials/Models/Asteroids/Asteroid01/Asteroid_01_Surface.mat` | `ff43801ad8a50574db1db7d2a0e86aca` | — | 3 | MOVE/RENAME |
| `model.dae` | `Art/Models/Asteroids/Asteroid01/Asteroid_01.dae` | `bc2f319e378053f4b9ea18ace01cdfdc` | — | 3 | MOVE/RENAME |
| `textures/defaultMat_ShipLit_MetallicSmoothness.png` | `Art/Textures/Models/DefendPlatforms/XQ6Platform/XQ6Platform_Surface_ShipLit_MetallicSmoothness.png` | `58532cc7be844f043af503fa418a1b4e` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/defaultMat_ShipLit_Occlusion.png` | `Art/Textures/Models/DefendPlatforms/XQ6Platform/XQ6Platform_Surface_ShipLit_Occlusion.png` | `e7f97c1f468afd8498937eff88ea2f00` | 512×512 | 2 | MOVE/RENAME |
| `textures/Material.001_albedo.jpg` | `Art/Textures/Models/Asteroids/Asteroid01/Asteroid_01_Surface_Albedo.jpg` | `24e664ffc14dbee4f9edfe78da816532` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/Material.001_AO.jpg` | `Art/Textures/Models/Asteroids/Asteroid01/Asteroid_01_Surface_AO.jpg` | `a7bfe042f9d6d7146901e23d3018eaa4` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/Material.001_emissive.jpg` | `Art/Textures/Models/Asteroids/Asteroid01/Asteroid_01_Surface_Emissive.jpg` | `05917d4699058a34591b906de559032c` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/Material.001_metallic.jpg` | `Art/Textures/Models/Asteroids/Asteroid01/Asteroid_01_Surface_Metallic.jpg` | `ea8b69d11000c0a4fbf2c2b58b2d21f9` | 4096×4096 | 2 | MOVE/RENAME |
| `textures/Material.001_normal.jpg` | `Art/Textures/Models/Asteroids/Asteroid01/Asteroid_01_Surface_Normal.jpg` | `3709b0189c4158c48bc4fef4dd8785fe` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/Material.001_roughness.jpg` | `Art/Textures/Models/Asteroids/Asteroid01/Asteroid_01_Surface_Roughness.jpg` | `5ef07c46408a06c449b25e24fa561db8` | 4096×4096 | 4 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Material_001.mat`: `_BumpMap` → `Material.001_normal.jpg`; `_EmissionMap` → `Material.001_emissive.jpg`; `_MainTex` → `Material.001_albedo.jpg`; `_MetallicGlossMap` → `Material.001_metallic.jpg`; `_OcclusionMap` → `Material.001_AO.jpg`; `_SpecGlossMap` → `Material.001_roughness.jpg`.

### HeavyTurbolaserCannon — file manifest

- Source root: `Assets/Art/Models/Cannons/heavy-turbolaser-cannon-v1/`.
- Contrary to the old audit, both materials use external textures. Cannon-generated mask/occlusion maps currently reside in Gangut; their rows appear in that group.
- Retain Surface + Surface_ShipLit pairing; preserve shared Venator/Gangut bindings.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `lambert1` → `d82290ef635ecb749afd7519ce81cdf8`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Heavy Turbolaser Cannon V1.fbx` | `Art/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon.fbx` | `9fbbe54c48254f545896b573927ce387` | — | 5 | MOVE/RENAME |
| `lambert1.mat` | `Art/Materials/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon_Surface.mat` | `d82290ef635ecb749afd7519ce81cdf8` | — | 1 | MOVE/RENAME |
| `lambert1_ShipLit.mat` | `Art/Materials/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon_Surface_ShipLit.mat` | `a918a451117a8254691e1da37e138d1c` | — | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `lambert1.mat`: `_BaseMap` → `StationSet02_metallic.jpg`; `_BumpMap` → `StationSet02_normal.jpg`; `_EmissionMap` → `StationSet03_emissive.jpg`; `_MainTex` → `rep_ven_turrets.tga`; `_OcclusionMap` → `StationSet02_AO.jpg`; `_SpecGlossMap` → `StationSet02_roughness.jpg`.
  - `lambert1_ShipLit.mat`: `_BumpMap` → `StationSet02_normal.jpg`; `_EmissionMap` → `StationSet03_emissive.jpg`; `_MainTex` → `rep_ven_turrets.tga`; `_MetallicGlossMap` → `lambert1_ShipLit_MetallicSmoothness.png`; `_OcclusionMap` → `lambert1_ShipLit_Occlusion.png`; `_SpecGlossMap` → `StationSet02_roughness.jpg`.

### XQ6Platform — file manifest

- Source root: `Assets/Art/Models/DefendPlatforms/XQ6/`.
- ScratchedMetal2Red is assigned to _OcclusionMap, not proven to be a second albedo; neutral MetalRed_Source is intentional.
- Generated packed/occlusion maps reside in Asteroids; their destinations are XQ6.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `defaultMat` → `a8d6548e15867104e99cd6e668db4728`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `defaultMat.mat` | `Art/Materials/Models/DefendPlatforms/XQ6Platform/XQ6Platform_Surface.mat` | `a8d6548e15867104e99cd6e668db4728` | — | 1 | MOVE/RENAME |
| `defaultMat_ShipLit.mat` | `Art/Materials/Models/DefendPlatforms/XQ6Platform/XQ6Platform_Surface_ShipLit.mat` | `1db429693d5d64e44ad9bf6962423b20` | — | 1 | MOVE/RENAME |
| `ScratchedMetal2.jpeg` | `Art/Textures/Models/DefendPlatforms/XQ6Platform/XQ6Platform_Metal_Albedo.jpeg` | `99d7a26f4eece104c933dfc31a0a08e1` | 512×512 | 5 | MOVE/RENAME |
| `ScratchedMetal2Red.jpeg` | `Art/Textures/Models/DefendPlatforms/XQ6Platform/XQ6Platform_MetalRed_Source.jpeg` | `08a7f5202cc825944b9b12de35d7c9eb` | 512×512 | 1 | MOVE/RENAME |
| `XQ6 Platform.mtl` | `Art/Models/DefendPlatforms/XQ6Platform/XQ6Platform.mtl` | `5fa7826896a430743a91c04c172af583` | — | 0 | MOVE/RENAME |
| `XQ6 Platform.obj` | `Art/Models/DefendPlatforms/XQ6Platform/XQ6Platform.obj` | `34ee5eb124da77040ada6dfecb479691` | — | 3 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `defaultMat.mat`: `_BaseMap` → `ScratchedMetal2.jpeg`; `_BumpMap` → `StationSet03_normal.jpg`; `_EmissionMap` → `StationSet03_emissive.jpg`; `_MainTex` → `ScratchedMetal2.jpeg`; `_MetallicGlossMap` → `Material.001_metallic.jpg`; `_OcclusionMap` → `ScratchedMetal2Red.jpeg`; `_SpecGlossMap` → `Material.001_roughness.jpg`.
  - `defaultMat_ShipLit.mat`: `_BaseMap` → `ScratchedMetal2.jpeg`; `_BumpMap` → `StationSet03_normal.jpg`; `_EmissionMap` → `StationSet03_emissive.jpg`; `_MainTex` → `ScratchedMetal2.jpeg`; `_MetallicGlossMap` → `defaultMat_ShipLit_MetallicSmoothness.png`; `_OcclusionMap` → `defaultMat_ShipLit_Occlusion.png`; `_SpecGlossMap` → `Material.001_roughness.jpg`.

### Moon — file manifest

- Source root: `Assets/Art/Models/Moon/our-moon/`.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `lroc_color_poles_16k` → `bcfab3c9c68fa604797c39249a08b673`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/lroc_color_poles_16k.mat` | `Art/Materials/Models/Planets/Moon/Moon_Surface.mat` | `bcfab3c9c68fa604797c39249a08b673` | — | 1 | MOVE/RENAME |
| `source/MoonDisplacement.fbx` | `Art/Models/Planets/Moon/Moon.fbx` | `d2a3d300e656f974084156ee3864d711` | — | 2 | MOVE/RENAME |
| `textures/8k_moon.jpg` | `Art/Textures/Models/Planets/Moon/Moon_Surface_Albedo_8K.jpg` | `070d45591bb5b4142a7e524e34833a91` | 8192×4096 | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/lroc_color_poles_16k.mat`: `_BaseMap` → `8k_moon.jpg`; `_MainTex` → `8k_moon.jpg`.

### JediCouncil — file manifest

- Source root: `Assets/Art/Models/Other/JediCouncul/`.
- Previously omitted model. Move mesh out of textures and correct JediCouncul spelling.
- No explicit externalObjects remaps detected; establish its current material-slot mapping before moving external materials.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `textures/Chairs.jpeg` | `Art/Textures/Models/Other/JediCouncil/JediCouncil_Chairs_Source.jpeg` | `0366e64d076cff540b611bb1dfff774e` | 4096×4096 | 0 | MOVE/RENAME |
| `textures/Column.001.mat` | `Art/Materials/Models/Other/JediCouncil/JediCouncil_Columns.mat` | `d665927918dbfa944af6b7441bdc30e8` | — | 1 | MOVE/RENAME |
| `textures/Columns.jpeg` | `Art/Textures/Models/Other/JediCouncil/JediCouncil_Columns_Albedo.jpeg` | `d9fefeb981e142d4eaf6dd1516c15209` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/Coruscant.jpeg` | `Art/Textures/Models/Other/JediCouncil/JediCouncil_Coruscant_Backdrop.jpeg` | `e9db036ff9127ec47847d81a84ade7bf` | 8192×2048 | 1 | MOVE/RENAME |
| `textures/Dome.jpeg` | `Art/Textures/Models/Other/JediCouncil/JediCouncil_Dome_Source.jpeg` | `b39053acc5536ec4aae8fa6d55e37151` | 4096×4096 | 0 | MOVE/RENAME |
| `textures/Door.jpeg` | `Art/Textures/Models/Other/JediCouncil/JediCouncil_Door_Source.jpeg` | `64fff464b3447544594175fa3fd420cb` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Floor.001.mat` | `Art/Materials/Models/Other/JediCouncil/JediCouncil_Floor.mat` | `3f8c3a98add3fc44187300bf9b499f27` | — | 1 | MOVE/RENAME |
| `textures/Floor.jpeg` | `Art/Textures/Models/Other/JediCouncil/JediCouncil_Floor_Albedo.jpeg` | `ce3f1200cb09f8946bb3d40a4649164f` | 8192×8192 | 1 | MOVE/RENAME |
| `textures/JediCouncil.fbx` | `Art/Models/Other/JediCouncil/JediCouncil.fbx` | `cf132909f2d3f6846b62ac83377ff4b1` | — | 1 | MOVE/RENAME |
| `textures/Skybox.mat` | `Art/Materials/Models/Other/JediCouncil/JediCouncil_Skybox.mat` | `cbfad0266f89a0d4f98a1a3a8483e7ca` | — | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `textures/Column.001.mat`: `_BaseMap` → `Columns.jpeg`; `_MainTex` → `Columns.jpeg`.
  - `textures/Floor.001.mat`: `_BaseMap` → `Floor.jpeg`; `_MainTex` → `Floor.jpeg`.
  - `textures/Skybox.mat`: `_BaseMap` → `Coruscant.jpeg`; `_MainTex` → `Coruscant.jpeg`.

### SciFiLamps — file manifest

- Source root: `Assets/Art/Models/Other/sci-fi-lamps/`.
- Previously omitted model. Keep both four-material sets; current source root has textured/converted materials, source/Materials has separate copies.
- _Imported names distinguish source/Materials copies; no deduplication.
- Importer: `2` materialImportMode; 4 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `glass` → `2e63fe7cf85bc0f4399eabf56b815911`; `parts1` → `66dadf91af11c864a85cb4430c6ed02e`; `parts2` → `f8dfc9e359e839f4ab2cf54a177ed5f1`; `shell` → `13ea34e93bd44a640bd40e5a57bfdded`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/glass.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Glass.mat` | `2e63fe7cf85bc0f4399eabf56b815911` | — | 5 | MOVE/RENAME |
| `source/lamps3export.fbx` | `Art/Models/Other/SciFiLamps/SciFiLamps.fbx` | `a8119784ae6ec8f4b8c85373be9f09c0` | — | 4 | MOVE/RENAME |
| `source/lamps_ao.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Lamps_AO.png` | `b2da28e9da6ebb74298f47ef65a162c3` | 2048×2048 | 0 | MOVE/RENAME |
| `source/Materials/glass.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Glass_Imported.mat` | `0488edfd756dc0e468c666217d1d1b6b` | — | 0 | MOVE/RENAME |
| `source/Materials/parts1.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Parts01_Imported.mat` | `87efdc54265c80c4684bd6b027cbadc2` | — | 0 | MOVE/RENAME |
| `source/Materials/parts2.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Parts02_Imported.mat` | `cccc21f97eaab9c4eae36c0039f2c59a` | — | 0 | MOVE/RENAME |
| `source/Materials/shell.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Shell_Imported.mat` | `d9e473b80fcba9a4c9e68602125ac4e1` | — | 0 | MOVE/RENAME |
| `source/metal_diffuse.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Metal_Albedo.png` | `920384c1e9c1806458b9ba681ffaa341` | 2048×2048 | 3 | MOVE/RENAME |
| `source/metal_gloss.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Metal_Gloss.png` | `e769f7b0f7d1da542a4f36fe6d1b2774` | 2048×2048 | 2 | MOVE/RENAME |
| `source/metal_metalness.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Metal_Metallic.png` | `12ed07621c7c7744ebb45d4e0c4a262f` | 2048×2048 | 0 | MOVE/RENAME |
| `source/metal_normalmap.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Metal_Normal.png` | `83dae2a954867c0428604f2ee944a920` | 2048×2048 | 3 | MOVE/RENAME |
| `source/metal_specular.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Metal_Specular.png` | `0efca199547979f4984a8074a79863aa` | 2048×2048 | 0 | MOVE/RENAME |
| `source/parts1.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Parts01.mat` | `66dadf91af11c864a85cb4430c6ed02e` | — | 5 | MOVE/RENAME |
| `source/parts1_MetallicSmoothness.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Parts01_MetallicSmoothness.png` | `27642f16edd62ff45971ae877711e753` | 2048×2048 | 1 | MOVE/RENAME |
| `source/parts1_Occlusion.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Parts01_Occlusion.png` | `1b618f25af74e8147959eda0e1f37e00` | 2048×2048 | 1 | MOVE/RENAME |
| `source/parts2.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Parts02.mat` | `f8dfc9e359e839f4ab2cf54a177ed5f1` | — | 1 | MOVE/RENAME |
| `source/parts2_MetallicSmoothness.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Parts02_MetallicSmoothness.png` | `a0dce26211446a144a47c62524c07a21` | 2048×2048 | 1 | MOVE/RENAME |
| `source/parts2_Occlusion.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Parts02_Occlusion.png` | `fe15eba735951fe4b8a96d4412ffd623` | 2048×2048 | 1 | MOVE/RENAME |
| `source/shell.mat` | `Art/Materials/Models/Other/SciFiLamps/SciFiLamps_Shell.mat` | `13ea34e93bd44a640bd40e5a57bfdded` | — | 5 | MOVE/RENAME |
| `source/shell_MetallicSmoothness.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Shell_MetallicSmoothness.png` | `f78481b9ac4b2f846a8eb2273ab4e3fd` | 2048×2048 | 1 | MOVE/RENAME |
| `source/shell_Occlusion.png` | `Art/Textures/Models/Other/SciFiLamps/SciFiLamps_Shell_Occlusion.png` | `d27a8a2d59811134fac86658605bc3c3` | 2048×2048 | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/parts1.mat`: `_BaseMap` → `metal_diffuse.png`; `_BumpMap` → `metal_normalmap.png`; `_MainTex` → `metal_diffuse.png`; `_MetallicGlossMap` → `parts1_MetallicSmoothness.png`; `_OcclusionMap` → `parts1_Occlusion.png`.
  - `source/parts2.mat`: `_BaseMap` → `metal_diffuse.png`; `_BumpMap` → `metal_normalmap.png`; `_MainTex` → `metal_diffuse.png`; `_MetallicGlossMap` → `parts2_MetallicSmoothness.png`; `_OcclusionMap` → `parts2_Occlusion.png`; `_SpecGlossMap` → `metal_gloss.png`.
  - `source/shell.mat`: `_BaseMap` → `metal_diffuse.png`; `_BumpMap` → `metal_normalmap.png`; `_MainTex` → `metal_diffuse.png`; `_MetallicGlossMap` → `shell_MetallicSmoothness.png`; `_OcclusionMap` → `shell_Occlusion.png`; `_SpecGlossMap` → `metal_gloss.png`.

### Coruscant — file manifest

- Source root: `Assets/Art/Models/Planets/Coruscant/`.
- Preserve both JPG and HQ PNG variants; HQ indicates source naming, not higher resolution.
- Move the scene-referenced lighting asset to Settings/Lighting; keep cloud opacity assignments unchanged.
- Importer: `2` materialImportMode; 2 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `clouds` → `3c4b51a775ca5c74a8e956d318b8b744`; `planet` → `2d370365e58727f4db0df35c347fc381`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/clouds.mat` | `Art/Materials/Models/Planets/Coruscant/Coruscant_Clouds.mat` | `3c4b51a775ca5c74a8e956d318b8b744` | — | 5 | MOVE/RENAME |
| `source/courscant.fbx` | `Art/Models/Planets/Coruscant/Coruscant.fbx` | `64e6a04015a3b564ca5afe4bab414bb1` | — | 4 | MOVE/RENAME |
| `source/New Lighting Settings.lighting` | `Settings/Lighting/CoruscantLighting.lighting` | `72fa76d13c85cdc43a9df84e1e051392` | — | 1 | MOVE/RENAME |
| `source/planet.mat` | `Art/Materials/Models/Planets/Coruscant/Coruscant_Surface.mat` | `2d370365e58727f4db0df35c347fc381` | — | 3 | MOVE/RENAME |
| `textures/clouds_albedo.jpg` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Clouds_Albedo.jpg` | `66b393a47b8cee444a2bb6f630a9ae13` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/clouds_normal.png` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Clouds_Normal.png` | `0c2b71f76386b6a46887ea28d67e90ea` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/clouds_opacity.jpg` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Clouds_Opacity.jpg` | `88ef65ecf8d5cbf4ebdf4d95ba640e42` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/courscant_planet_Emissive.jpg` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Surface_Emissive.jpg` | `81f08d62ea3324249818689213becfdc` | 4096×4096 | 0 | MOVE/RENAME |
| `textures/courscant_planet_Emissive_hq.png` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Surface_Emissive_HQ.png` | `4cd9dff3f86cc1048ba75c8b3fa839f2` | 1254×1254 | 1 | MOVE/RENAME |
| `textures/courscant_planet_Height.jpg` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Surface_Height.jpg` | `2b39e9f0f4f905040a9d32eec5ae6fae` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/planet_albedo.jpg` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Surface_Albedo.jpg` | `b43e4604bc0face478482a7ea48d5a0b` | 4096×4096 | 0 | MOVE/RENAME |
| `textures/planet_albedo_hq.png` | `Art/Textures/Models/Planets/Coruscant/Coruscant_Surface_Albedo_HQ.png` | `bac6c9c041bb49847bd05a6c3efa54a8` | 1254×1254 | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/clouds.mat`: `_BaseMap` → `clouds_albedo.jpg`; `_BumpMap` → `clouds_normal.png`; `_EmissionMap` → `clouds_opacity.jpg`; `_MainTex` → `clouds_albedo.jpg`; `_OcclusionMap` → `clouds_opacity.jpg`.
  - `source/planet.mat`: `_BaseMap` → `planet_albedo_hq.png`; `_EmissionMap` → `courscant_planet_Emissive_hq.png`; `_MainTex` → `planet_albedo_hq.png`; `_ParallaxMap` → `courscant_planet_Height.jpg`.

### Kamino — file manifest

- Source root: `Assets/Art/Models/Planets/kamino/`.
- Dathomir source token retained because the actual assigned normal has that filename; no claim that it depicts Kamino geology.
- Kamino model has no scanned GUID consumers; this is not a deletion decision.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Material` → `91bae4b7948fb1248a3c6bfa60c414cc`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/Kamino.blend` | `Art/Models/Planets/Kamino/Kamino.blend` | `8e81866d79eeef141a125ccad43e57a3` | — | 0 | MOVE/RENAME |
| `source/Material.mat` | `Art/Materials/Models/Planets/Kamino/Kamino_Surface.mat` | `91bae4b7948fb1248a3c6bfa60c414cc` | — | 3 | MOVE/RENAME |
| `textures/Dathomir.normal.png` | `Art/Textures/Models/Planets/Kamino/Kamino_Surface_Normal_DathomirSource.png` | `43f2834cab96dea4b8309f0bae1a2768` | 1028×1028 | 1 | MOVE/RENAME |
| `textures/Kamino.Base.Color.png` | `Art/Textures/Models/Planets/Kamino/Kamino_Surface_Albedo.png` | `cd05d86218c8ec54f8e5e50e90b70b21` | 2048×2048 | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/Material.mat`: `_BaseMap` → `Kamino.Base.Color.png`; `_BumpMap` → `Dathomir.normal.png`; `_MainTex` → `Kamino.Base.Color.png`.

### AWing — file manifest

- Source root: `Assets/Art/Models/RepublicModels/A-wing/`.
- Importer: `2` materialImportMode; 22 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Cockpit_Inside` → `bfa49ebbfa62a1b428769ae60242a282`; `Cockpit_Rear` → `eb01f2084bc534e4098d5f47d6285e59`; `Cockpit_Side_L` → `5423f860c3e5ef44898b61483c79c9bf`; `Cockpit_Side_R` → `845b295de5bd5e246a4913b4f169de5a`; `Engine_FX` → `599b12ed1dc4a8542a6354c9af6f64ae`; `Engine_L` → `ac86369c15d1d834a99b9cf33be0f338`; `Engine_R` → `fd567fc53c547a24fa0750ed54c63046`; `Hull_Dorsal` → `e849b904d0feff84cada641d012c32cf`; `Hull_Front` → `8bf24c7b3a8855b4eac357a12e59352e`; `Hull_Inside` → `196d660158d91e643b3b94b4bae25364`; `Hull_Rear` → `f7e46a3a8c63f39449266877706504b3`; `Hull_Side_1` → `9cb531413f02abe4b90f94e3faff804b`; `Hull_Side_2` → `5acb34d7072c5704f9bdd0d9630bb850`; `Hull_Ventral` → `8944bc2a0dd275b43b40b677b0baf3f5`; `Jet_Housing` → `aafaabcdba816354382102a13ccd571d`; `Jet_Middle` → `df4ea5c9a9094f547ad3ee4e5b94865b`; `Jet_Rear` → `3f114343d4f85c446b117ca5b3260fc4`; `Laser` → `4415bce66d29aa044abb23dda8e95f73`; `Laser_Holder` → `622c5c7558e9fe041bcc5ce83146ebb4`; `Pilot` → `649e723cb6379e04cbd5a90ea88a173c`; `Seat` → `4b58c854f22cdf34abf713b83e8d8b29`; `Window` → `70c86b09e7070bb45a07ebee06f9c954`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `A-Wing.dae` | `Art/Models/RepublicShips/AWing/AWing.dae` | `41714dc343ae02347961e85e8a6fda89` | — | 2 | MOVE/RENAME |
| `Cockpit_Inside.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_CockpitInside.mat` | `bfa49ebbfa62a1b428769ae60242a282` | — | 1 | MOVE/RENAME |
| `Cockpit_Rear.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_CockpitRear.mat` | `eb01f2084bc534e4098d5f47d6285e59` | — | 1 | MOVE/RENAME |
| `Cockpit_Side_L.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_CockpitSideLeft.mat` | `5423f860c3e5ef44898b61483c79c9bf` | — | 1 | MOVE/RENAME |
| `Cockpit_Side_R.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_CockpitSideRight.mat` | `845b295de5bd5e246a4913b4f169de5a` | — | 1 | MOVE/RENAME |
| `Engine_FX.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_EngineFX.mat` | `599b12ed1dc4a8542a6354c9af6f64ae` | — | 1 | MOVE/RENAME |
| `Engine_L.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_EngineLeft.mat` | `ac86369c15d1d834a99b9cf33be0f338` | — | 1 | MOVE/RENAME |
| `Engine_R.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_EngineRight.mat` | `fd567fc53c547a24fa0750ed54c63046` | — | 1 | MOVE/RENAME |
| `Hull_Dorsal.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullDorsal.mat` | `e849b904d0feff84cada641d012c32cf` | — | 1 | MOVE/RENAME |
| `Hull_Front.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullFront.mat` | `8bf24c7b3a8855b4eac357a12e59352e` | — | 1 | MOVE/RENAME |
| `Hull_Inside.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullInside.mat` | `196d660158d91e643b3b94b4bae25364` | — | 1 | MOVE/RENAME |
| `Hull_Rear.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullRear.mat` | `f7e46a3a8c63f39449266877706504b3` | — | 1 | MOVE/RENAME |
| `Hull_Side_1.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullSide01.mat` | `9cb531413f02abe4b90f94e3faff804b` | — | 1 | MOVE/RENAME |
| `Hull_Side_2.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullSide02.mat` | `5acb34d7072c5704f9bdd0d9630bb850` | — | 1 | MOVE/RENAME |
| `Hull_Ventral.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_HullVentral.mat` | `8944bc2a0dd275b43b40b677b0baf3f5` | — | 1 | MOVE/RENAME |
| `Jet_Housing.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_JetHousing.mat` | `aafaabcdba816354382102a13ccd571d` | — | 1 | MOVE/RENAME |
| `Jet_Middle.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_JetMiddle.mat` | `df4ea5c9a9094f547ad3ee4e5b94865b` | — | 1 | MOVE/RENAME |
| `Jet_Rear.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_JetRear.mat` | `3f114343d4f85c446b117ca5b3260fc4` | — | 1 | MOVE/RENAME |
| `Laser.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_Laser.mat` | `4415bce66d29aa044abb23dda8e95f73` | — | 1 | MOVE/RENAME |
| `Laser_Holder.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_LaserHolder.mat` | `622c5c7558e9fe041bcc5ce83146ebb4` | — | 1 | MOVE/RENAME |
| `Pilot.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_Pilot.mat` | `649e723cb6379e04cbd5a90ea88a173c` | — | 1 | MOVE/RENAME |
| `Seat.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_Seat.mat` | `4b58c854f22cdf34abf713b83e8d8b29` | — | 1 | MOVE/RENAME |
| `Textures/A-Wing Cockpit Inside Top.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_CockpitInsideTop_Albedo.jpg` | `4cfa850d3265a9e4e9287d163f257795` | 726×337 | 1 | MOVE/RENAME |
| `Textures/A-Wing Cockpit Rear.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_CockpitRear_Albedo.jpg` | `91890faba20be9149b53ab89466e3c3d` | 136×95 | 1 | MOVE/RENAME |
| `Textures/A-Wing Cockpit Side Left.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_CockpitSideLeft_Albedo.jpg` | `0db70f558559587498363a96304eca4f` | 700×159 | 1 | MOVE/RENAME |
| `Textures/A-Wing Cockpit Side Right.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_CockpitSideRight_Albedo.jpg` | `2c5642485e2569843aab1c2ed0c24617` | 700×159 | 1 | MOVE/RENAME |
| `Textures/A-Wing Engine Left Bump.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_EngineLeft_BumpSource.jpg` | `1d357e997607c6546a99a5a1580dbe52` | 500×386 | 1 | MOVE/RENAME |
| `Textures/A-Wing Engine Left.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_EngineLeft_Albedo.jpg` | `25686e30493d5944a84c582b95d4e42f` | 500×386 | 1 | MOVE/RENAME |
| `Textures/A-Wing Engine Right Bump.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_EngineRight_BumpSource.jpg` | `dfa4badc82c328643b92dde91477416f` | 500×386 | 1 | MOVE/RENAME |
| `Textures/A-Wing Engine Right.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_EngineRight_Albedo.jpg` | `2e12a6704897c794187d4d6dc65d425a` | 500×386 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Dorsal Bump.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullDorsal_BumpSource.jpg` | `329558f1efc109d43869fdb0f68c6e34` | 1000×737 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Dorsal.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullDorsal_Albedo.jpg` | `b760cf35c77f8244082f6dc097e5de8d` | 1000×737 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Front.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullFront_Albedo.jpg` | `6b70a6bbcc2522f479c5d6ac94afa84c` | 289×122 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Inside.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullInside_Albedo.jpg` | `17af9a2525122f84a8fe96fefb3d1981` | 223×430 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Rear.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullRear_Albedo.jpg` | `3d1a7bbe624c5f7419032192d96c6956` | 623×155 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Side 01.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullSide01_Albedo.jpg` | `f7d11cf70b43bf44794e0c31d878b0a6` | 730×120 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Side 02.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullSide02_Albedo.jpg` | `7873fc6f93b544d45a919bd9932845d8` | 867×177 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Ventral Bump.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullVentral_BumpSource.jpg` | `eb56d66019d16834d82efbb6476a7abd` | 1000×737 | 1 | MOVE/RENAME |
| `Textures/A-Wing Hull Ventral.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_HullVentral_Albedo.jpg` | `d28c9a76ae7053e4196e895a8f5ef9c8` | 1000×737 | 1 | MOVE/RENAME |
| `Textures/A-Wing Jet Housing.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_JetHousing_Albedo.jpg` | `ce973082c1052914ba369ce91217471a` | 314×300 | 1 | MOVE/RENAME |
| `Textures/A-Wing Jet Middle.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_JetMiddle_Albedo.jpg` | `d51f21a2eeabccc47bfd07ca6a2e8470` | 314×300 | 1 | MOVE/RENAME |
| `Textures/A-Wing Jet Rear.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_JetRear_Albedo.jpg` | `a9a4297a3b6481940a3f05793e9e6dba` | 300×223 | 1 | MOVE/RENAME |
| `Textures/A-Wing Laser Holder.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_LaserHolder_Albedo.jpg` | `c366f04d7bf116d4ea097da9bea38f2f` | 79×120 | 1 | MOVE/RENAME |
| `Textures/A-Wing Laser.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_Laser_Albedo.jpg` | `7dc3716d4686bb14dabc45a3070bd04a` | 400×58 | 1 | MOVE/RENAME |
| `Textures/A-Wing Pilot.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_Pilot_Albedo.jpg` | `41f0cd977d5448b48a7e494fcf997789` | 250×400 | 1 | MOVE/RENAME |
| `Textures/A-Wing Seat.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_Seat_Albedo.jpg` | `18db6092003f4fb4badb12012798fbb1` | 264×409 | 1 | MOVE/RENAME |
| `Textures/Credits.txt` | `Art/Models/RepublicShips/AWing/Credits.txt` | `e7aa9262c95def64788ff01d210bc499` | — | 0 | MOVE/RENAME |
| `Textures/Render.jpg` | `Art/Textures/Models/RepublicShips/AWing/AWing_RenderPreview.jpg` | `6031a4855f781284281ae37a793400ad` | 640×480 | 0 | MOVE/RENAME |
| `Window.mat` | `Art/Materials/Models/RepublicShips/AWing/AWing_Window.mat` | `70c86b09e7070bb45a07ebee06f9c954` | — | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Cockpit_Inside.mat`: `_BaseMap` → `A-Wing Cockpit Inside Top.jpg`; `_MainTex` → `A-Wing Cockpit Inside Top.jpg`.
  - `Cockpit_Rear.mat`: `_BaseMap` → `A-Wing Cockpit Rear.jpg`; `_MainTex` → `A-Wing Cockpit Rear.jpg`.
  - `Cockpit_Side_L.mat`: `_BaseMap` → `A-Wing Cockpit Side Left.jpg`; `_MainTex` → `A-Wing Cockpit Side Left.jpg`.
  - `Cockpit_Side_R.mat`: `_BaseMap` → `A-Wing Cockpit Side Right.jpg`; `_MainTex` → `A-Wing Cockpit Side Right.jpg`.
  - `Engine_L.mat`: `_BaseMap` → `A-Wing Engine Left.jpg`; `_BumpMap` → `A-Wing Engine Left Bump.jpg`; `_MainTex` → `A-Wing Engine Left.jpg`.
  - `Engine_R.mat`: `_BaseMap` → `A-Wing Engine Right.jpg`; `_BumpMap` → `A-Wing Engine Right Bump.jpg`; `_MainTex` → `A-Wing Engine Right.jpg`.
  - `Hull_Dorsal.mat`: `_BaseMap` → `A-Wing Hull Dorsal.jpg`; `_BumpMap` → `A-Wing Hull Dorsal Bump.jpg`; `_MainTex` → `A-Wing Hull Dorsal.jpg`.
  - `Hull_Front.mat`: `_BaseMap` → `A-Wing Hull Front.jpg`; `_MainTex` → `A-Wing Hull Front.jpg`.
  - `Hull_Inside.mat`: `_BaseMap` → `A-Wing Hull Inside.jpg`; `_MainTex` → `A-Wing Hull Inside.jpg`.
  - `Hull_Rear.mat`: `_BaseMap` → `A-Wing Hull Rear.jpg`; `_MainTex` → `A-Wing Hull Rear.jpg`.
  - `Hull_Side_1.mat`: `_BaseMap` → `A-Wing Hull Side 01.jpg`; `_MainTex` → `A-Wing Hull Side 01.jpg`.
  - `Hull_Side_2.mat`: `_BaseMap` → `A-Wing Hull Side 02.jpg`; `_MainTex` → `A-Wing Hull Side 02.jpg`.
  - `Hull_Ventral.mat`: `_BaseMap` → `A-Wing Hull Ventral.jpg`; `_BumpMap` → `A-Wing Hull Ventral Bump.jpg`; `_MainTex` → `A-Wing Hull Ventral.jpg`.
  - `Jet_Housing.mat`: `_BaseMap` → `A-Wing Jet Housing.jpg`; `_MainTex` → `A-Wing Jet Housing.jpg`.
  - `Jet_Middle.mat`: `_BaseMap` → `A-Wing Jet Middle.jpg`; `_MainTex` → `A-Wing Jet Middle.jpg`.
  - `Jet_Rear.mat`: `_BaseMap` → `A-Wing Jet Rear.jpg`; `_MainTex` → `A-Wing Jet Rear.jpg`.
  - `Laser.mat`: `_BaseMap` → `A-Wing Laser.jpg`; `_MainTex` → `A-Wing Laser.jpg`.
  - `Laser_Holder.mat`: `_BaseMap` → `A-Wing Laser Holder.jpg`; `_MainTex` → `A-Wing Laser Holder.jpg`.
  - `Pilot.mat`: `_BaseMap` → `A-Wing Pilot.jpg`; `_MainTex` → `A-Wing Pilot.jpg`.
  - `Seat.mat`: `_BaseMap` → `A-Wing Seat.jpg`; `_MainTex` → `A-Wing Seat.jpg`.

### Acclamator — file manifest

- Source root: `Assets/Art/Models/RepublicModels/Acclamator/`.
- Use Slot00/01 until renderer geometry proves a better part name; both materials share the texture set.
- Keep source importer identifiers object_0/object_1 unchanged; external asset names may differ.
- Importer: `2` materialImportMode; 2 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `object_0` → `d91fd22c7396cda4bb94388cb465a5f8`; `object_1` → `5d520fcb8dd9e8140abfc9e4883bd39a`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Acclamator.fbx` | `Art/Models/RepublicShips/Acclamator/Acclamator.fbx` | `676452b8c7363624cb31c588f1eee23c` | — | 3 | MOVE/RENAME |
| `object_0.mat` | `Art/Materials/Models/RepublicShips/Acclamator/Acclamator_Slot00.mat` | `d91fd22c7396cda4bb94388cb465a5f8` | — | 1 | MOVE/RENAME |
| `object_1.mat` | `Art/Materials/Models/RepublicShips/Acclamator/Acclamator_Slot01.mat` | `5d520fcb8dd9e8140abfc9e4883bd39a` | — | 1 | MOVE/RENAME |
| `R_9cb7962f52ca4d4e9a1e9823c27fac48_TEX_PRP_Destroyer_REP_Class_Acclamator_AO.png` | `Art/Textures/Models/RepublicShips/Acclamator/Acclamator_Surface_AO.png` | `d6eeae8028469d744a72095987f7671b` | 4096×4096 | 4 | MOVE/RENAME |
| `RGB_4c519b72a1244474bdcacee792d89eaf_TEX_PRP_Destroyer_REP_Class_Acclamator_WP_.png` | `Art/Textures/Models/RepublicShips/Acclamator/Acclamator_Surface_Albedo.png` | `affed1928a980044c98345d4142b2b19` | 2048×2048 | 4 | MOVE/RENAME |
| `RGB_9ecb7988cadd4daea1858d1b57f5f291_TEX_PRP_Destroyer_REP_Class_Acclamator_E.png` | `Art/Textures/Models/RepublicShips/Acclamator/Acclamator_Surface_Emissive.png` | `2889f65260efa384f98d0ed5ca4a6f78` | 2048×2048 | 4 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `object_0.mat`: `_BaseMap` → `RGB_4c519b72a1244474bdcacee792d89eaf_TEX_PRP_Destroyer_REP_Class_Acclamator_WP_.png`; `_EmissionMap` → `RGB_9ecb7988cadd4daea1858d1b57f5f291_TEX_PRP_Destroyer_REP_Class_Acclamator_E.png`; `_MainTex` → `RGB_4c519b72a1244474bdcacee792d89eaf_TEX_PRP_Destroyer_REP_Class_Acclamator_WP_.png`; `_ParallaxMap` → `R_9cb7962f52ca4d4e9a1e9823c27fac48_TEX_PRP_Destroyer_REP_Class_Acclamator_AO.png`.
  - `object_1.mat`: `_BaseMap` → `RGB_4c519b72a1244474bdcacee792d89eaf_TEX_PRP_Destroyer_REP_Class_Acclamator_WP_.png`; `_EmissionMap` → `RGB_9ecb7988cadd4daea1858d1b57f5f291_TEX_PRP_Destroyer_REP_Class_Acclamator_E.png`; `_MainTex` → `RGB_4c519b72a1244474bdcacee792d89eaf_TEX_PRP_Destroyer_REP_Class_Acclamator_WP_.png`; `_ParallaxMap` → `R_9cb7962f52ca4d4e9a1e9823c27fac48_TEX_PRP_Destroyer_REP_Class_Acclamator_AO.png`.

### Arquitens — file manifest

- Source root: `Assets/Art/Models/RepublicModels/Arquitens/`.
- Keep both material variants and their distinct base images; do not overwrite one with the other.
- Both materials also serialize _MainTex pointing to Venator/ReV_venator.tga; preserve it during migration even if inactive.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Arquitens_Albedo_Republic.png` | `Art/Textures/Models/RepublicShips/Arquitens/Arquitens_Hull_Albedo_Republic.png` | `92a05ce505e515a449e96e3ad79ebf5d` | 1024×1024 | 1 | MOVE/RENAME |
| `Arquitens_Republic.mat` | `Art/Materials/Models/RepublicShips/Arquitens/Arquitens_Hull_Republic.mat` | `61fdc88192cb8814c84dfc4b9b6ac965` | — | 1 | MOVE/RENAME |
| `Cruero LigeroImperial Arquitens.mtl` | `Art/Models/RepublicShips/Arquitens/Arquitens.mtl` | `db58b0d09796ac747a803972b8055732` | — | 0 | MOVE/RENAME |
| `Cruero LigeroImperial Arquitens.obj` | `Art/Models/RepublicShips/Arquitens/Arquitens.obj` | `bf7a8fa108400824bb3f02f05b94d38a` | — | 3 | MOVE/RENAME |
| `Cruero LigeroImperial Arquitens1.jpg` | `Art/Textures/Models/RepublicShips/Arquitens/Arquitens_Hull_Albedo_SourceVariant.jpg` | `9b34407d876064c4db356e25c0e80f47` | 1024×1024 | 1 | MOVE/RENAME |
| `ObjectMat.mat` | `Art/Materials/Models/RepublicShips/Arquitens/Arquitens_Hull_SourceVariant.mat` | `3dda1ce42d3c20e47902b8cbe2dd8c06` | — | 0 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Arquitens_Republic.mat`: `_BaseMap` → `Arquitens_Albedo_Republic.png`; `_MainTex` → `ReV_venator.tga`.
  - `ObjectMat.mat`: `_BaseMap` → `Cruero LigeroImperial Arquitens1.jpg`; `_MainTex` → `ReV_venator.tga`.

### Delta7 — file manifest

- Source root: `Assets/Art/Models/RepublicModels/Delta7/`.
- Keep all 11 materials and all 7 images, including VariantB and DullMetal sources.
- MTL path references include the dull-metal normal even though its serialized GUID consumer count is zero.
- Importer: `2` materialImportMode; 11 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `ARC_cockpitGlass` → `5aa5e516a6e76fa4b97d764c579d2a65`; `Astromech_VW` → `9e5cc83cb06fbfe4183828b730d9169d`; `BlueGlow` → `af114052fad20f94f97aaf22ac8b1355`; `BlueGlow_trans` → `3ecb2b69ce18d64439c2f258d0dc1994`; `Delta7_hull` → `3e4a14699d67d914485075d44fc483b2`; `Delta7_other` → `8dec6a0da4fc64445bda78f4b84b2f8b`; `R2D2_eye` → `75e96dfb5f9f3144ca2f0b55bedf98e6`; `R2_glow` → `e92910e2d9ecdda4fa97c1515915d48f`; `R2_glow_green` → `b62e5fcd3d56f014082e8d8d1e36cdf3`; `RedGlow` → `16986d14c96940d48a5154bb3616317c`; `WhiteGlow` → `f3a431c1e2ed0b94686f51f66ee0a573`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Delta7.mtl` | `Art/Models/RepublicShips/Delta7/Delta7.mtl` | `1a85031ae55c8ec488fa9106523e0cfe` | — | 0 | MOVE/RENAME |
| `Delta7.obj` | `Art/Models/RepublicShips/Delta7/Delta7.obj` | `b82042cfadf201941b9483a51dccf9ae` | — | 2 | MOVE/RENAME |
| `Materials/ARC_cockpitGlass.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_CockpitGlass.mat` | `5aa5e516a6e76fa4b97d764c579d2a65` | — | 1 | MOVE/RENAME |
| `Materials/Astromech_VW.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_Astromech.mat` | `9e5cc83cb06fbfe4183828b730d9169d` | — | 1 | MOVE/RENAME |
| `Materials/BlueGlow.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_BlueGlow.mat` | `af114052fad20f94f97aaf22ac8b1355` | — | 1 | MOVE/RENAME |
| `Materials/BlueGlow_trans.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_BlueGlow_Transparent.mat` | `3ecb2b69ce18d64439c2f258d0dc1994` | — | 1 | MOVE/RENAME |
| `Materials/Delta7_hull.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_Hull.mat` | `3e4a14699d67d914485075d44fc483b2` | — | 1 | MOVE/RENAME |
| `Materials/Delta7_other.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_Details.mat` | `8dec6a0da4fc64445bda78f4b84b2f8b` | — | 1 | MOVE/RENAME |
| `Materials/R2_glow.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_AstromechGlow.mat` | `e92910e2d9ecdda4fa97c1515915d48f` | — | 1 | MOVE/RENAME |
| `Materials/R2_glow_green.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_AstromechGlow_Green.mat` | `b62e5fcd3d56f014082e8d8d1e36cdf3` | — | 1 | MOVE/RENAME |
| `Materials/R2D2_eye.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_AstromechEye.mat` | `75e96dfb5f9f3144ca2f0b55bedf98e6` | — | 1 | MOVE/RENAME |
| `Materials/RedGlow.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_RedGlow.mat` | `16986d14c96940d48a5154bb3616317c` | — | 1 | MOVE/RENAME |
| `Materials/WhiteGlow.mat` | `Art/Materials/Models/RepublicShips/Delta7/Delta7_WhiteGlow.mat` | `f3a431c1e2ed0b94686f51f66ee0a573` | — | 1 | MOVE/RENAME |
| `Textures/Astromech_Vwing.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_Astromech_Albedo_VWingSource.png` | `3abda18150757f4408923acd197bd3c4` | 50×50 | 2 | MOVE/RENAME |
| `Textures/Delta-7 hull.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_Hull_Albedo.png` | `fc90177d6fd6e334bbeada077e00e104` | 2048×2048 | 1 | MOVE/RENAME |
| `Textures/Delta-7 other bits.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_Details_Albedo.png` | `2c11b8f4c0894204c8059ffed44f4166` | 2048×2048 | 1 | MOVE/RENAME |
| `Textures/Delta-7b other bits.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_Details_Albedo_VariantB.png` | `214bda6754721064399e955da4a3c9a2` | 2048×2048 | 0 | MOVE/RENAME |
| `Textures/dull_metal_metallic.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_DullMetal_Metallic.png` | `c5cb02bd5626d41408aa4f84965ef81d` | 2048×2048 | 0 | MOVE/RENAME |
| `Textures/dull_metal_normal-ogl.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_DullMetal_Normal_OpenGL.png` | `5a47cdc8b748fdc4b8892e2e6e1a8dcb` | 2048×2048 | 0 | MOVE/RENAME |
| `Textures/dull_metal_roughness.png` | `Art/Textures/Models/RepublicShips/Delta7/Delta7_DullMetal_Roughness.png` | `bb0caecd155ae234da46205687955e9c` | 2048×2048 | 0 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Materials/Astromech_VW.mat`: `_BaseMap` → `Astromech_Vwing.png`; `_MainTex` → `Astromech_Vwing.png`.
  - `Materials/Delta7_hull.mat`: `_BaseMap` → `Delta-7 hull.png`; `_MainTex` → `Delta-7 hull.png`.
  - `Materials/Delta7_other.mat`: `_BaseMap` → `Delta-7 other bits.png`; `_MainTex` → `Delta-7 other bits.png`.
  - `Materials/R2D2_eye.mat`: `_BaseMap` → `Astromech_Vwing.png`; `_MainTex` → `Astromech_Vwing.png`.

### StarDestroyer1 — file manifest

- Source root: `Assets/Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/`.
- Preserve 10 materials and 48 images. MixedAO_SP/render tokens are retained rather than assuming a single-channel AO format.
- Materials share StarDestroyer2 texture assignments; the local original images remain source assets.
- No explicit importer remaps detected; capture renderer slots/remaps before changing names.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Materials/Mat_Bottom_Hull_Detail.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_BottomHullDetail.mat` | `58a83a60ca9902f4cab49d3113702dd6` | — | 0 | MOVE/RENAME |
| `Materials/mat_destruct.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_Destructibles.mat` | `b7f09ffc026cf1d4ba4b8460655daf32` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Engine_Block.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_EngineBlock.mat` | `3487eec18983aed46916d448459fce9f` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Head_Neck_Final.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_HeadNeck.mat` | `96b1544da83b7424285903859e85bcf4` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Lower_Deck.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerDeck.mat` | `f1cfb66dadbc51745b5c27ab7c089837` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Lower_Hull_Bottom.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerHullBottom.mat` | `03cbaf83a3a5a2d49baa404fc764f7de` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Top_Deck_Final.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopDeck.mat` | `01a28a13022de3e43a5eaef5fefecdcb` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Top_Hull_Detail.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHullDetail.mat` | `45e3c1c192ef92d419a8a912a4eb6a7c` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Top_Hull_Final.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHull.mat` | `092a2315d6263144c81a4bc6c61dfb26` | — | 0 | MOVE/RENAME |
| `Materials/Mat_Trench_Side.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TrenchSide.mat` | `051effa798f14b2489400a466978a439` | — | 0 | MOVE/RENAME |
| `sketchfablowpolystardestroyer.mtl` | `Art/Models/RepublicShips/StarDestroyer1/StarDestroyer1.mtl` | `d31298bb08fdce94d9e12f21073ba06f` | — | 0 | MOVE/RENAME |
| `sketchfablowpolystardestroyer.obj` | `Art/Models/RepublicShips/StarDestroyer1/StarDestroyer1.obj` | `49eb7ed51f07af744a8589a34f963e01` | — | 3 | MOVE/RENAME |
| `textures/destructables_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_Destructibles_Emissive.png` | `0e33acef2c205bd4fb8ce4fe187a9aaf` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Bottom_Hull_Detail_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_BottomHullDetail_Albedo.png` | `21a625cf3872f0e458d6e9f82ce293ff` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Bottom_Hull_Detail_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_BottomHullDetail_Emissive.png` | `3b2badbc9a8e5d3469ace5c7e2ba7243` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Bottom_Hull_Detail_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_BottomHullDetail_MixedAO_SP.png` | `109f0885532473e4fb28215a621730d8` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Bottom_Hull_Detail_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_BottomHullDetail_Normal_OpenGL.png` | `c8d3d2fa64c43494fa60f1b26b1b4be4` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Bottom_Hull_Detail_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_BottomHullDetail_Roughness.png` | `4c847880522528c47826459186df698f` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/mat_destruct_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_Destructibles_Albedo.png` | `da134d42dc06fc24bbf24126e7db86f6` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/mat_destruct_Mixed_AO.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_Destructibles_MixedAO.png` | `c8cbb14e0b9a1844b85a5e65dbcf7f1c` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/mat_destruct_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_Destructibles_Normal_OpenGL.png` | `3263c1ab76c487d4f8d666ecf5a50077` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/mat_destruct_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_Destructibles_Roughness.png` | `22b9988df7c8ae4419cd20d0f1e75cca` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Engine_Block_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_EngineBlock_Albedo.png` | `eafa2b6b8c798cf4692362c666d4593c` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Engine_Block_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_EngineBlock_MixedAO_SP.png` | `f9f32b3bc7d94284ba9566877fd00ec8` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Engine_Block_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_EngineBlock_Normal_OpenGL.png` | `8905c5f1d7947e343ae7a46065f1f1b2` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Engine_Block_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_EngineBlock_Roughness.png` | `71624fde33ef2e94abcb1269ce7464f9` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Head_Neck_Final_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_HeadNeck_Albedo.png` | `eb169b07d8767cc46b4d14dd5dce1b25` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Head_Neck_Final_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_HeadNeck_Emissive.png` | `6a4adb19183e0fc4895bc183ac03b10b` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Head_Neck_Final_Mixed_AO_render.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_HeadNeck_MixedAO_render.png` | `e2b4e9d1d8e91154e9584557ea8b5b3e` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Head_Neck_Final_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_HeadNeck_Normal_OpenGL.png` | `61ce594d768ff3a4da1843a7352937d1` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Head_Neck_Final_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_HeadNeck_Roughness.png` | `58efb48753af2d548a81013f37edd684` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Lower_Deck_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerDeck_Albedo.png` | `c277963b02065934d8a80ca0a3ea965b` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Lower_Deck_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerDeck_Emissive.png` | `8d195efe67342a745a72cfd7f71a562c` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Lower_Deck_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerDeck_MixedAO_SP.png` | `6875379460ae27e4691c7e7cb9d53dc6` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Lower_Deck_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerDeck_Normal_OpenGL.png` | `76ea2bc29245ad649ae882242b6fb7e5` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Lower_Deck_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerDeck_Roughness.png` | `6c61743aefa0e9c41a30006e2dea899d` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Lower_Hull_Bottom_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerHullBottom_Albedo.png` | `3135267bcf4ba6d4e9cb2da1a2135c26` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Lower_Hull_Bottom_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerHullBottom_MixedAO_SP.png` | `5d11baeaa31f759418f4cb6290998386` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Lower_Hull_Bottom_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerHullBottom_Normal_OpenGL.png` | `5de75b1ab8bdacb44ab3e9a6a9d3b642` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Lower_Hull_Bottom_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_LowerHullBottom_Roughness.png` | `9aef041e87ef39a4ea71af9fb857727e` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Top_Deck_Final_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopDeck_Albedo.png` | `c0a6bbf6008db7045b97b6ac9b9075e5` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Deck_Final_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopDeck_Emissive.png` | `24428f6cea101694ea487387478c41e3` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Deck_Final_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopDeck_MixedAO_SP.png` | `0bd3a56df65b2744dac7cda3c6db4fad` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Top_Deck_Final_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopDeck_Normal_OpenGL.png` | `57c9f2d738b05b54bb6e3beb44691e50` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Deck_Final_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopDeck_Roughness.png` | `11ce0d973d334cf438465ebd293fcd52` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Detail_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHullDetail_Albedo.png` | `d26b627b1448c3c45934691d9c430d66` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Detail_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHullDetail_Emissive.png` | `01f794bc447b2b54f8e79425efabc974` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Detail_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHullDetail_MixedAO_SP.png` | `2cfa1856883583841b20b8a195e1db39` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Detail_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHullDetail_Normal_OpenGL.png` | `29307c2b08c85704c901620c6051ee0d` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Detail_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHullDetail_Roughness.png` | `f79951f8882e5fd41a7eec64ac168ee4` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Final_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHull_Albedo.png` | `0e7dc7cc42132b8409d00c44fd8a484c` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Final_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHull_MixedAO_SP.png` | `4ccf6d4f66bb54249ad25a29fb14ad4c` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Final_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHull_Normal_OpenGL.png` | `b04b33e0e5db2cd409983b40e69ef13a` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Top_Hull_Final_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TopHull_Roughness.png` | `85364881b17f2d245a4b4ec2be6f34c7` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Trench_Side_Base_Color.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TrenchSide_Albedo.png` | `ce6033dbf2fbec1418e9ffb582a6b1ad` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Trench_Side_Emissive.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TrenchSide_Emissive.png` | `b0ad0f2c39c63674faab5f16ea198e93` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Trench_Side_Mixed_AO_SP.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TrenchSide_MixedAO_SP.png` | `3d9e6b3d224521f44bb22c35ce49a572` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mat_Trench_Side_Normal_OpenGL.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TrenchSide_Normal_OpenGL.png` | `6f9107716623b2847a071e8c7a39228b` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mat_Trench_Side_Roughness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_TrenchSide_Roughness.png` | `e22d0854c9006ea4499f0635c530569d` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/smallengine_luminance.png` | `Art/Textures/Models/RepublicShips/StarDestroyer1/StarDestroyer1_SmallEngine_LuminanceSource.png` | `06fceeafd7f381245820c0049cbd0000` | 2048×2048 | 0 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Materials/Mat_Bottom_Hull_Detail.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Bottom_Hull_Detail_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Bottom_Hull_Detail_Roughness.png`.
  - `Materials/mat_destruct.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `mat_destruct_Mixed_AO.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `mat_destruct_Roughness.png`.
  - `Materials/Mat_Engine_Block.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Engine_Block_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Engine_Block_Roughness.png`.
  - `Materials/Mat_Head_Neck_Final.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Head_Neck_Final_Mixed_AO_render.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Head_Neck_Final_Roughness.png`.
  - `Materials/Mat_Lower_Deck.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Lower_Deck_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Lower_Deck_Roughness.png`.
  - `Materials/Mat_Lower_Hull_Bottom.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Lower_Hull_Bottom_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Lower_Hull_Bottom_Roughness.png`.
  - `Materials/Mat_Top_Deck_Final.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Top_Deck_Final_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Top_Deck_Final_Roughness.png`.
  - `Materials/Mat_Top_Hull_Detail.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Top_Hull_Detail_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Top_Hull_Detail_Roughness.png`.
  - `Materials/Mat_Top_Hull_Final.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_MetallicGlossMap` → `ScratchedMetal2.jpeg`; `_OcclusionMap` → `Mat_Top_Hull_Final_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Top_Hull_Final_Roughness.png`.
  - `Materials/Mat_Trench_Side.mat`: `_BaseMap` → `isd_tile_texture.png`; `_BumpMap` → `star_destroyer_normal_map_fixed.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_OcclusionMap` → `Mat_Trench_Side_Mixed_AO_SP.png`; `_ParallaxMap` → `star_destroyer_height_map.png`; `_SpecGlossMap` → `Mat_Trench_Side_Roughness.png`.

### SpaceStationModular — file manifest

- Source root: `Assets/Art/Models/RepublicModels/SpaceStation2/`.
- Six generic material roles are unverified; use Slot00–05 rather than inventing Core/Panels assignments.
- No explicit importer remaps detected; pre-existing unresolved texture tokens require baseline capture.
- Importer: `1` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/Materials/Material.001.mat` | `Art/Materials/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Slot01.mat` | `e7eee5b6624903e4c959dad269e14382` | — | 0 | MOVE/RENAME |
| `source/Materials/Material.002.mat` | `Art/Materials/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Slot02.mat` | `91d516c4b9263e047941af1dd72df1a0` | — | 0 | MOVE/RENAME |
| `source/Materials/Material.003.mat` | `Art/Materials/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Slot03.mat` | `7517464915a2a9d48982a81c91c87a0a` | — | 0 | MOVE/RENAME |
| `source/Materials/Material.004.mat` | `Art/Materials/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Slot04.mat` | `5c8c096f5b06a5d4f8247a3540d37cd9` | — | 0 | MOVE/RENAME |
| `source/Materials/Material.005.mat` | `Art/Materials/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Slot05.mat` | `0cc3ac2d54471034a9568819cb70515f` | — | 0 | MOVE/RENAME |
| `source/Materials/Material.mat` | `Art/Materials/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Slot00.mat` | `d926fc9aea1510c41975c78aafb32638` | — | 0 | MOVE/RENAME |
| `source/untitled.fbx` | `Art/Models/SpaceStations/SpaceStationModular/SpaceStationModular.fbx` | `767c6ab96e2543a4ba3d1831b7fcfbf6` | — | 0 | MOVE/RENAME |
| `textures/Tiles071_2K_AmbientOcclusion.jpg` | `Art/Textures/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Tiles071_AO_2K.jpg` | `53a1d79f9174a774b88202f8529faea1` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Tiles071_2K_Normal.jpg` | `Art/Textures/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Tiles071_Normal_2K.jpg` | `45df096af2304c2459c7d4d22eadf427` | 2048×2048 | 3 | MOVE/RENAME |
| `textures/Tiles071_2K_Roughness.jpg` | `Art/Textures/Models/SpaceStations/SpaceStationModular/SpaceStationModular_Tiles071_Roughness_2K.jpg` | `1cbb8789fb88eb34fb9e55e55968bb51` | 2048×2048 | 3 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/Materials/Material.002.mat`: `_BumpMap` → `Tiles071_2K_Normal.jpg`; `_SpecGlossMap` → `Tiles071_2K_Roughness.jpg`.
  - `source/Materials/Material.003.mat`: `_BumpMap` → `Tiles071_2K_Normal.jpg`; `_EmissionMap` → `c5917249e89888346a4bd606beae9578`; `_SpecGlossMap` → `Tiles071_2K_Roughness.jpg`.
  - `source/Materials/Material.mat`: `_BaseMap` → `fr_floor_03.png`; `_BumpMap` → `Tiles071_2K_Normal.jpg`; `_EmissionMap` → `fr_holograms_07.png`; `_MainTex` → `fr_floor_03.png`; `_MetallicGlossMap` → `38cec185e7d5c3e45acd5838295f23b1`; `_SpecGlossMap` → `Tiles071_2K_Roughness.jpg`.

### StarDestroyer2 — file manifest

- Source root: `Assets/Art/Models/RepublicModels/star-destroyer/`.
- Keep all four materials and eight images; ColorBaked and HullColorBaked are separate assets.
- Surface maps are also shared by StarDestroyer1 and wreck materials; preserve these consumers.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/Imperial-Class-StarDestroyer.fbx` | `Art/Models/RepublicShips/StarDestroyer2/StarDestroyer2.fbx` | `4de91b602cd0dd14f958147960261ea5` | — | 4 | MOVE/RENAME |
| `source/Imperial-Class-StarDestroyer.mtl` | `Art/Models/RepublicShips/StarDestroyer2/StarDestroyer2.mtl` | `1690dadcd48cef5448793801cec9b924` | — | 0 | MOVE/RENAME |
| `source/Materials/Emission1.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer2/StarDestroyer2_Emission01.mat` | `f0126a6e35658a542baa0a1dd9b27bc4` | — | 2 | MOVE/RENAME |
| `source/Materials/ISD_Color_Baked.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer2/StarDestroyer2_ColorBaked.mat` | `575d28085fd06e742b23a13b85b3e214` | — | 2 | MOVE/RENAME |
| `source/Materials/ISD_Color_Baked_ShipLit.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer2/StarDestroyer2_ColorBaked_ShipLit.mat` | `f31399a424c8de54485c260e5e6ec8e2` | — | 1 | MOVE/RENAME |
| `source/Materials/ISD_Hull_Color_Baked.mat` | `Art/Materials/Models/RepublicShips/StarDestroyer2/StarDestroyer2_HullColorBaked.mat` | `0a739e694770e524c8e8c0ee49fa4b88` | — | 0 | MOVE/RENAME |
| `textures/ISD_Color_Baked.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_ColorBaked_Albedo.png` | `df1c78e893a44774aaee740370f7be50` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/ISD_Color_Baked_ShipLit_MetallicSmoothness.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_ColorBaked_ShipLit_MetallicSmoothness.png` | `6d8281d87a53fab48ab60f2e49b3ac8a` | 1×1 | 2 | MOVE/RENAME |
| `textures/ISD_Emission.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_Surface_Emissive.png` | `50d775de739b8104b8e47e55e023d3f5` | 8192×8192 | 25 | MOVE/RENAME |
| `textures/ISD_Hull_Color_Baked.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_HullColorBaked_Albedo.png` | `734126b1f849d1a4e998546f34da4610` | 8192×4096 | 0 | MOVE/RENAME |
| `textures/ISD_Hull_Height_Render.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_Hull_Height_Render.png` | `7c54441d87bc4a74a9b3aa84b4bfc8f3` | 8192×4096 | 2 | MOVE/RENAME |
| `textures/isd_tile_texture.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_Tile_Source.png` | `8e73f5606b78361469abd12b03f4e4df` | 1024×1024 | 25 | MOVE/RENAME |
| `textures/star_destroyer_height_map.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_Surface_Height.png` | `f25049b0077d175469664a56c6103763` | 1024×1024 | 20 | MOVE/RENAME |
| `textures/star_destroyer_normal_map_fixed.png` | `Art/Textures/Models/RepublicShips/StarDestroyer2/StarDestroyer2_Surface_Normal_FixedSource.png` | `25d2b00f14589004fbdf30f7cbd7eafb` | 1024×1024 | 20 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/Materials/ISD_Color_Baked.mat`: `_BaseMap` → `ISD_Color_Baked.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`.
  - `source/Materials/ISD_Color_Baked_ShipLit.mat`: `_BaseMap` → `isd_tile_texture.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_MetallicGlossMap` → `ISD_Color_Baked_ShipLit_MetallicSmoothness.png`.
  - `source/Materials/ISD_Hull_Color_Baked.mat`: `_BaseMap` → `isd_tile_texture.png`; `_EmissionMap` → `ISD_Emission.png`; `_MainTex` → `isd_tile_texture.png`; `_ParallaxMap` → `ISD_Hull_Height_Render.png`.

### HeavyDreadnought — file manifest

- Source root: `Assets/Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/`.
- Preserve all 13 materials and all 11 textures, including Republic textures, ISD plating, and DullMetal_ShipLit.
- ISD texture copies are byte-identical to StarDestroyer2 files but have separate GUIDs; no merge.
- Importer: `2` materialImportMode; 10 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Black` → `dbe2878378207994199f1996e4465a01`; `Dull_Metal` → `c45e184114245f64ba4b7bb573548ce3`; `Dull_Metal_Greeble` → `f7b617a67f995f942b97e9dc5273b972`; `Engine_Glow` → `03f04895a0936f94f87224ebcc14da9c`; `Grey Hull.001` → `f842c6b5a7676c44c94c4caed8286478`; `Hangar_Light` → `74c3b0f1091e2fd4e9762c0f80d08755`; `ISD Plating` → `0a7c33002051f5443b56913ef16e0762`; `ISD Plating_Radar` → `b5537bebbdf14444ba0da47400ef5749`; `Window_Light` → `fff2416433007ee4e8258161f3413e63`; `Windows` → `1b7a0254e745840458b39a947b1db77a`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/Black.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Black.mat` | `dbe2878378207994199f1996e4465a01` | — | 2 | MOVE/RENAME |
| `source/Dreadnaught_Model.blend` | `Art/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought.blend` | `4baab00e80f7e184e8776a3d3195bf15` | — | 3 | MOVE/RENAME |
| `source/Dull_Metal.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetal.mat` | `c45e184114245f64ba4b7bb573548ce3` | — | 1 | MOVE/RENAME |
| `source/Dull_Metal_Greeble.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetalGreeble.mat` | `f7b617a67f995f942b97e9dc5273b972` | — | 1 | MOVE/RENAME |
| `source/Dull_Metal_ShipLit.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetal_ShipLit.mat` | `e704ab0bf0cca654791dfdd5ef7d32e8` | — | 1 | MOVE/RENAME |
| `source/Engine_Glow.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_EngineGlow.mat` | `03f04895a0936f94f87224ebcc14da9c` | — | 1 | MOVE/RENAME |
| `source/Grey Hull.001.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_GreyHull01.mat` | `f842c6b5a7676c44c94c4caed8286478` | — | 1 | MOVE/RENAME |
| `source/Hangar_Light.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_HangarLight.mat` | `74c3b0f1091e2fd4e9762c0f80d08755` | — | 1 | MOVE/RENAME |
| `source/HeavyDreadnought_Bow_Republic.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Bow_Republic.mat` | `452c178d40d5c8149818f4b309ec1eb6` | — | 1 | MOVE/RENAME |
| `source/HeavyDreadnought_Hull_Republic.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Hull_Republic.mat` | `2da466887aa05024fb601af178793ed8` | — | 1 | MOVE/RENAME |
| `source/ISD Plating.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_ISDPlating.mat` | `0a7c33002051f5443b56913ef16e0762` | — | 1 | MOVE/RENAME |
| `source/ISD Plating_Radar.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_ISDPlatingRadar.mat` | `b5537bebbdf14444ba0da47400ef5749` | — | 1 | MOVE/RENAME |
| `source/Window_Light.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_WindowLight.mat` | `fff2416433007ee4e8258161f3413e63` | — | 1 | MOVE/RENAME |
| `source/Windows.mat` | `Art/Materials/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Windows.mat` | `1b7a0254e745840458b39a947b1db77a` | — | 1 | MOVE/RENAME |
| `textures/dull_metal_albedo.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetal_Albedo.png` | `b300612fc533d3e43a5195a90b10f1ab` | 2048×2048 | 3 | MOVE/RENAME |
| `textures/dull_metal_metallic.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetal_Metallic.png` | `ea786311031566545a3aae1161376f1e` | 2048×2048 | 1 | MOVE/RENAME |
| `textures/dull_metal_roughness.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetal_Roughness.png` | `1930fc9bf6501f446ad7cc5cfcf4abbb` | 2048×2048 | 3 | MOVE/RENAME |
| `textures/Dull_Metal_ShipLit_MetallicSmoothness.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_DullMetal_ShipLit_MetallicSmoothness.png` | `d200dcbfad9e5654582767c5fcbc05d4` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/HeavyDreadnought_Bow_Republic.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Bow_Albedo_Republic.png` | `066aff2428155c343ba9bf8b9b83ccbb` | 8192×4096 | 2 | MOVE/RENAME |
| `textures/HeavyDreadnought_Hull_Republic.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Hull_Albedo_Republic.png` | `4978534a72f94d147b6e633f5ee535e8` | 8192×4096 | 2 | MOVE/RENAME |
| `textures/ISD_Hull_Color_Baked.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_ISDPlating_Albedo.png` | `c47aae8b9ae310e4d8b7273a3790c8a4` | 8192×4096 | 6 | MOVE/RENAME |
| `textures/ISD_Hull_Height_Render.png` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_ISDPlating_Height.png` | `51d1210cfada61f4eac96141ed9f1298` | 8192×4096 | 6 | MOVE/RENAME |
| `textures/Metal027_4K_Color.jpg` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Metal027_Albedo_4K.jpg` | `7efd157188e02a04bb42042bdb76d3a7` | 4096×4096 | 2 | MOVE/RENAME |
| `textures/Metal027_4K_Metalness.jpg` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Metal027_Metallic_4K.jpg` | `c0040ee566d0943489931747bfa377d2` | 4096×4096 | 0 | MOVE/RENAME |
| `textures/Metal027_4K_Roughness.jpg` | `Art/Textures/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought_Metal027_Roughness_4K.jpg` | `4a97d38112272ce4f82cb7e33d813279` | 4096×4096 | 0 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/Dull_Metal.mat`: `_BaseMap` → `dull_metal_albedo.png`; `_MainTex` → `dull_metal_albedo.png`; `_MetallicGlossMap` → `dull_metal_metallic.png`; `_SpecGlossMap` → `dull_metal_roughness.png`.
  - `source/Dull_Metal_ShipLit.mat`: `_BaseMap` → `dull_metal_albedo.png`; `_MainTex` → `dull_metal_albedo.png`; `_MetallicGlossMap` → `Dull_Metal_ShipLit_MetallicSmoothness.png`; `_SpecGlossMap` → `dull_metal_roughness.png`.
  - `source/Grey Hull.001.mat`: `_BaseMap` → `Metal027_4K_Color.jpg`; `_MainTex` → `Metal027_4K_Color.jpg`.
  - `source/HeavyDreadnought_Bow_Republic.mat`: `_BaseMap` → `HeavyDreadnought_Bow_Republic.png`; `_MainTex` → `ISD_Hull_Color_Baked.png`; `_ParallaxMap` → `ISD_Hull_Height_Render.png`.
  - `source/HeavyDreadnought_Hull_Republic.mat`: `_BaseMap` → `HeavyDreadnought_Hull_Republic.png`; `_MainTex` → `ISD_Hull_Color_Baked.png`; `_ParallaxMap` → `ISD_Hull_Height_Render.png`.
  - `source/ISD Plating.mat`: `_BaseMap` → `ISD_Hull_Color_Baked.png`; `_MainTex` → `ISD_Hull_Color_Baked.png`; `_ParallaxMap` → `ISD_Hull_Height_Render.png`.

### Thranta — file manifest

- Source root: `Assets/Art/Models/RepublicModels/Thranta/`.
- Importer: `2` materialImportMode; 5 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `veh_rep_destroyer_detail_d` → `2b3536b95b9327e49a68062d29d1eb42`; `veh_rep_destroyer_engine_d` → `4d0256ac27698ca4a90fe49f1998d206`; `veh_rep_destroyer_metal01_d` → `17b44dd1721ff0d44b9a405035688c84`; `veh_rep_destroyer_metal02_d` → `339b6d08f9439a54893995d489ee86cd`; `veh_rep_destroyer_trim_d` → `ab65a4d1932656847903d876a32d509a`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Materials/veh_rep_destroyer_detail_d.mat` | `Art/Materials/Models/RepublicShips/Thranta/Thranta_Detail.mat` | `2b3536b95b9327e49a68062d29d1eb42` | — | 2 | MOVE/RENAME |
| `Materials/veh_rep_destroyer_engine_d.mat` | `Art/Materials/Models/RepublicShips/Thranta/Thranta_Engine.mat` | `4d0256ac27698ca4a90fe49f1998d206` | — | 2 | MOVE/RENAME |
| `Materials/veh_rep_destroyer_metal01_d.mat` | `Art/Materials/Models/RepublicShips/Thranta/Thranta_Metal01.mat` | `17b44dd1721ff0d44b9a405035688c84` | — | 2 | MOVE/RENAME |
| `Materials/veh_rep_destroyer_metal02_d.mat` | `Art/Materials/Models/RepublicShips/Thranta/Thranta_Metal02.mat` | `339b6d08f9439a54893995d489ee86cd` | — | 2 | MOVE/RENAME |
| `Materials/veh_rep_destroyer_trim_d.mat` | `Art/Materials/Models/RepublicShips/Thranta/Thranta_Trim.mat` | `ab65a4d1932656847903d876a32d509a` | — | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_detail_d.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Detail_Albedo.png` | `229c0fe418b3973478e5802c31c73103` | 1024×1024 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_detail_n.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Detail_Normal.png` | `23b33e920bc167d4f9973e5968cff814` | 1024×1024 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_engine_d.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Engine_Albedo.png` | `188b44a47b8013046ba1452f7627b941` | 1024×1024 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_engine_n.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Engine_Normal.png` | `46348e3722abdde4e9ff5bfd7cbced5d` | 1024×1024 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_metal01_d.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Metal01_Albedo.png` | `b85cbd22625c683458b3a030b5756cc5` | 512×512 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_metal01_n.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Metal01_Normal.png` | `cd28c6895f9af69419f769d63571c575` | 512×512 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_metal02_d.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Metal02_Albedo.png` | `5e14563770fd1234b924c6060fdb7e2c` | 512×512 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_metal02_n.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Metal02_Normal.png` | `89c68f7c53a1e7e4ca9d03f429049ab8` | 512×512 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_trim_d.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Trim_Albedo.png` | `c04e842d10dfcde4983d1d02d8909e15` | 512×512 | 2 | MOVE/RENAME |
| `Textures/veh_rep_destroyer_trim_n.png` | `Art/Textures/Models/RepublicShips/Thranta/Thranta_Trim_Normal.png` | `bf1f858e77975f945bdd9dedba97c683` | 512×512 | 2 | MOVE/RENAME |
| `Thranta-class.mtl` | `Art/Models/RepublicShips/Thranta/Thranta.mtl` | `85ecb2a1d5cd7f0439b3418d35e2025e` | — | 0 | MOVE/RENAME |
| `Thranta-class.obj` | `Art/Models/RepublicShips/Thranta/Thranta.obj` | `1547c75ffc39a514280feaf45a41b09e` | — | 2 | MOVE/RENAME |
| `ThrantaMergedMesh.asset` | `Art/Models/RepublicShips/Thranta/ThrantaMergedMesh.asset` | `7f7488fe8f2169a46a7cac6bb22d4740` | — | 2 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Materials/veh_rep_destroyer_detail_d.mat`: `_BaseMap` → `veh_rep_destroyer_detail_d.png`; `_BumpMap` → `veh_rep_destroyer_detail_n.png`; `_MainTex` → `veh_rep_destroyer_detail_d.png`.
  - `Materials/veh_rep_destroyer_engine_d.mat`: `_BaseMap` → `veh_rep_destroyer_engine_d.png`; `_BumpMap` → `veh_rep_destroyer_engine_n.png`; `_MainTex` → `veh_rep_destroyer_engine_d.png`.
  - `Materials/veh_rep_destroyer_metal01_d.mat`: `_BaseMap` → `veh_rep_destroyer_metal01_d.png`; `_BumpMap` → `veh_rep_destroyer_metal01_n.png`; `_MainTex` → `veh_rep_destroyer_metal01_d.png`.
  - `Materials/veh_rep_destroyer_metal02_d.mat`: `_BaseMap` → `veh_rep_destroyer_metal02_d.png`; `_BumpMap` → `veh_rep_destroyer_metal02_n.png`; `_MainTex` → `veh_rep_destroyer_metal02_d.png`.
  - `Materials/veh_rep_destroyer_trim_d.mat`: `_BaseMap` → `veh_rep_destroyer_trim_d.png`; `_BumpMap` → `veh_rep_destroyer_trim_n.png`; `_MainTex` → `veh_rep_destroyer_trim_d.png`.

### Venator — file manifest

- Source root: `Assets/Art/Models/RepublicModels/Venator/`.
- Preserve all six materials: prefab Materials copies and model-importer Venator copies get distinct names.
- All four TGAs have live references; ReV_VenatorTd is assigned to _SpecGlossMap, so retain SpecularSource distinction.
- Importer remap keys stay BLUETHRUSTER_CL / Capital_Repvenator_CL / rep_ven_turrets.
- Importer: `1` materialImportMode; 3 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `BLUETHRUSTER_CL` → `ddda979529feb0147adee1fe7c5bcde0`; `Capital_Repvenator_CL` → `d8b85746b89b73a43a63eef07971df34`; `rep_ven_turrets` → `cee3e304ae845f94d8528e5eec791ed2`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Materials/BLUETHRUSTER_CL.mat` | `Art/Materials/Models/RepublicShips/Venator/Venator_ThrusterBlue.mat` | `dbcb41a11086a94469932414e7cfd4aa` | — | 1 | MOVE/RENAME |
| `Materials/Capital_Repvenator_CL.mat` | `Art/Materials/Models/RepublicShips/Venator/Venator_Hull.mat` | `efefdecf283508448b976062285bad92` | — | 1 | MOVE/RENAME |
| `Materials/rep_ven_turrets.mat` | `Art/Materials/Models/RepublicShips/Venator/Venator_Turrets.mat` | `b5ae708b9b1062747b8152725313eee6` | — | 0 | MOVE/RENAME |
| `RepublicVenator2.fbx` | `Art/Models/RepublicShips/Venator/Venator.fbx` | `41356a7c2ecbe9a4fb11fd8398dbca64` | — | 3 | MOVE/RENAME |
| `Venator/BLUETHRUSTER_CL.mat` | `Art/Materials/Models/RepublicShips/Venator/Venator_ThrusterBlue_Imported.mat` | `ddda979529feb0147adee1fe7c5bcde0` | — | 1 | MOVE/RENAME |
| `Venator/Capital_Repvenator_CL.mat` | `Art/Materials/Models/RepublicShips/Venator/Venator_Hull_Imported.mat` | `d8b85746b89b73a43a63eef07971df34` | — | 1 | MOVE/RENAME |
| `Venator/EngineGlow.tga` | `Art/Textures/Models/RepublicShips/Venator/Venator_Thruster_Albedo.tga` | `021a87a8e492aeb4abddf2a4db053e3b` | 1024×1024 | 2 | MOVE/RENAME |
| `Venator/rep_ven_turrets.mat` | `Art/Materials/Models/RepublicShips/Venator/Venator_Turrets_Imported.mat` | `cee3e304ae845f94d8528e5eec791ed2` | — | 1 | MOVE/RENAME |
| `Venator/rep_ven_turrets.tga` | `Art/Textures/Models/RepublicShips/Venator/Venator_Turrets_Albedo.tga` | `e4216344423000f46be2e912772d8bb7` | 1024×1024 | 4 | MOVE/RENAME |
| `Venator/ReV_venator.tga` | `Art/Textures/Models/RepublicShips/Venator/Venator_Hull_Albedo.tga` | `eaff03988099b6548b75bf0cab129f30` | 1024×1024 | 4 | MOVE/RENAME |
| `Venator/ReV_VenatorTd.tga` | `Art/Textures/Models/RepublicShips/Venator/Venator_Hull_SpecularSource.tga` | `6212b067dc23cbd488824bf36c75a62d` | 1024×1024 | 2 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Materials/BLUETHRUSTER_CL.mat`: `_BaseMap` → `EngineGlow.tga`; `_MainTex` → `EngineGlow.tga`.
  - `Materials/Capital_Repvenator_CL.mat`: `_BaseMap` → `ReV_venator.tga`; `_MainTex` → `ReV_venator.tga`; `_SpecGlossMap` → `ReV_VenatorTd.tga`.
  - `Materials/rep_ven_turrets.mat`: `_BaseMap` → `rep_ven_turrets.tga`; `_MainTex` → `rep_ven_turrets.tga`.

### Belbullab22 — file manifest

- Source root: `Assets/Art/Models/SeparatistShip/belbullab/`.
- Importer: `2` materialImportMode; 4 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `B22_body` → `d6913728abcd54e459fbc1ac855f60f8`; `B22_interior` → `e9fbe4a5ac680bb43b8ee18c3d594fb5`; `B22_wing` → `d974328d670d92c4ea3409503f9cc4b5`; `VW_transBlack` → `0bc08c527a081344190926523e69a566`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `B22_whole.mtl` | `Art/Models/SeparatistShips/Belbullab22/Belbullab22.mtl` | `2fe82edbeda76fe43a8fbd567349010e` | — | 0 | MOVE/RENAME |
| `B22_whole.obj` | `Art/Models/SeparatistShips/Belbullab22/Belbullab22.obj` | `4795fd6558beea2459791805dcf880bb` | — | 2 | MOVE/RENAME |
| `Materials/B22_body.mat` | `Art/Materials/Models/SeparatistShips/Belbullab22/Belbullab22_Body.mat` | `d6913728abcd54e459fbc1ac855f60f8` | — | 1 | MOVE/RENAME |
| `Materials/B22_interior.mat` | `Art/Materials/Models/SeparatistShips/Belbullab22/Belbullab22_Interior.mat` | `e9fbe4a5ac680bb43b8ee18c3d594fb5` | — | 1 | MOVE/RENAME |
| `Materials/B22_wing.mat` | `Art/Materials/Models/SeparatistShips/Belbullab22/Belbullab22_Wing.mat` | `d974328d670d92c4ea3409503f9cc4b5` | — | 1 | MOVE/RENAME |
| `Materials/VW_transBlack.mat` | `Art/Materials/Models/SeparatistShips/Belbullab22/Belbullab22_GlassBlack_Transparent.mat` | `0bc08c527a081344190926523e69a566` | — | 1 | MOVE/RENAME |
| `Textures/Belbullab22_Body.png` | `Art/Textures/Models/SeparatistShips/Belbullab22/Belbullab22_Body_Albedo.png` | `d361e668c1ae01542b6bc2e81d247656` | 2048×2048 | 1 | MOVE/RENAME |
| `Textures/Belbullab22_Interior.png` | `Art/Textures/Models/SeparatistShips/Belbullab22/Belbullab22_Interior_Albedo.png` | `185fe0e18c0285847ab026a25f04a506` | 128×128 | 1 | MOVE/RENAME |
| `Textures/Belbullab22_Wing.png` | `Art/Textures/Models/SeparatistShips/Belbullab22/Belbullab22_Wing_Albedo.png` | `0cfb425a3dd4ec7478018369ecb444fb` | 2048×2048 | 1 | MOVE/RENAME |
| `Textures/dull_metal_metallic.png` | `Art/Textures/Models/SeparatistShips/Belbullab22/Belbullab22_DullMetal_Metallic.png` | `a17207880c2155947881a959e65462dc` | 2048×2048 | 0 | MOVE/RENAME |
| `Textures/dull_metal_normal-ogl.png` | `Art/Textures/Models/SeparatistShips/Belbullab22/Belbullab22_DullMetal_Normal_OpenGL.png` | `90c4718b3bf2b7244bf1943ee7e0026f` | 2048×2048 | 0 | MOVE/RENAME |
| `Textures/dull_metal_roughness.png` | `Art/Textures/Models/SeparatistShips/Belbullab22/Belbullab22_DullMetal_Roughness.png` | `212ff9ff7eefb4e45bc1a41d25dcd7dc` | 2048×2048 | 0 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Materials/B22_body.mat`: `_BaseMap` → `Belbullab22_Body.png`; `_MainTex` → `Belbullab22_Body.png`.
  - `Materials/B22_interior.mat`: `_BaseMap` → `Belbullab22_Interior.png`; `_MainTex` → `Belbullab22_Interior.png`.
  - `Materials/B22_wing.mat`: `_BaseMap` → `Belbullab22_Wing.png`; `_MainTex` → `Belbullab22_Wing.png`.

### Lucrehulk — file manifest

- Source root: `Assets/Art/Models/SeparatistShip/CIS Lucrehulk/`.
- Four live root materials are mapped in Loose materials: Slot019/020/021/022.
- Slot019 uses Set03; Slot020 Set01; Slots021/022 Set02. No Core/Ring geometry assumption.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `cis_cap_fedcoreship.mtl` | `Art/Models/SeparatistShips/Lucrehulk/Lucrehulk.mtl` | `4ad4f52151cf0cb4989199f912a2ca6e` | — | 0 | MOVE/RENAME |
| `cis_cap_fedcoreship.obj` | `Art/Models/SeparatistShips/Lucrehulk/Lucrehulk.obj` | `9b7c3b65d3406f84d8fa8fe27ba2deb3` | — | 3 | MOVE/RENAME |
| `cis_cap_fedcoreship.png` | `Art/Textures/Models/SeparatistShips/Lucrehulk/Lucrehulk_Set01_Albedo.png` | `d58b7ca7a8047ae4fbfef0fde42fc97f` | 256×256 | 2 | MOVE/RENAME |
| `cis_cap_fedcoreship_02.png` | `Art/Textures/Models/SeparatistShips/Lucrehulk/Lucrehulk_Set02_Albedo.png` | `e009c964be1305a479cdb26efa98fd91` | 256×256 | 4 | MOVE/RENAME |
| `cis_cap_fedcoreship_03.png` | `Art/Textures/Models/SeparatistShips/Lucrehulk/Lucrehulk_Set03_Albedo.png` | `b4000477c9a85904392f58ebf28fe3af` | 256×256 | 2 | MOVE/RENAME |

### Munificent — file manifest

- Source root: `Assets/Art/Models/SeparatistShip/Munificent/`.
- ArmorTest remains live armor albedo. Preserve all seven materials and seven images.
- BL remains neutral; Set04 image is shared by Engines and Weapons; WingsS7 is used in both base and emission assignments.
- Importer: `2` materialImportMode; 7 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `ArmorShape` → `e43a1dcc7055d814fb937d936e7386bc`; `BL_mat` → `aded22a292d8ece4284cd4f7dc26c900`; `EngineShape` → `c517a36f7cc7b0249b8127a1513e7b01`; `SecAr` → `6d357a8574fd625418c73bee6387c172`; `Shields_Mat` → `63b7273c8be79d74fb4c03565b926f8d`; `SuperstructureShape` → `e8925f376f1e2ef479bd9e221655d44f`; `WeaponsShape` → `5d8cbf2f3bc537e4db90669a3e6907a1`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `ArmorShape.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_Armor.mat` | `e43a1dcc7055d814fb937d936e7386bc` | — | 1 | MOVE/RENAME |
| `BL_mat.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_BL.mat` | `aded22a292d8ece4284cd4f7dc26c900` | — | 1 | MOVE/RENAME |
| `EngineShape.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_Engines.mat` | `c517a36f7cc7b0249b8127a1513e7b01` | — | 1 | MOVE/RENAME |
| `Munificent-CloneWars-Style.fbx` | `Art/Models/SeparatistShips/Munificent/Munificent.fbx` | `64333c4e9ab351f4ba631db128e2cdc2` | — | 3 | MOVE/RENAME |
| `Munificent-CloneWars-Style.mtl` | `Art/Models/SeparatistShips/Munificent/Munificent.mtl` | `654977de67165e24880083b9c4918c08` | — | 0 | MOVE/RENAME |
| `SecAr.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_SecondaryArmor.mat` | `6d357a8574fd625418c73bee6387c172` | — | 1 | MOVE/RENAME |
| `Shields_Mat.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_Shields.mat` | `63b7273c8be79d74fb4c03565b926f8d` | — | 1 | MOVE/RENAME |
| `SuperstructureShape.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_Superstructure.mat` | `e8925f376f1e2ef479bd9e221655d44f` | — | 1 | MOVE/RENAME |
| `textures/ArmorTest.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Armor_Albedo.png` | `8e1aaf9d097a3c24aa54142089bd1702` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/CWS7-Shields.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Shields_Albedo_S7.png` | `1eb3aeb4c54fa2f4ba8555321ac5c326` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/Mun_A_P_2_ColorMap.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Set02_ColorMap.png` | `940f178d631e7e54a9205c976e014941` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Mun_A_P_4_ColorMapD.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Set04_ColorMapD.png` | `615ea6181779ce04f99132c23d0d1f6d` | 1024×1024 | 4 | MOVE/RENAME |
| `textures/Munificent_MIL-S7.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Military_Source_S7.png` | `5aa3624ec80289544a1de20b5262ce35` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/Munificent_wings-S7.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Wings_Source_S7.png` | `e74b697102f137b4da63eb18078a8e48` | 2048×2048 | 4 | MOVE/RENAME |
| `textures/Munificent_wings.png` | `Art/Textures/Models/SeparatistShips/Munificent/Munificent_Wings_Source.png` | `90b4fb59fa737b542827aaedf15e54a7` | 2048×2048 | 2 | MOVE/RENAME |
| `WeaponsShape.mat` | `Art/Materials/Models/SeparatistShips/Munificent/Munificent_Weapons.mat` | `5d8cbf2f3bc537e4db90669a3e6907a1` | — | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `ArmorShape.mat`: `_BaseMap` → `ArmorTest.png`; `_MainTex` → `ArmorTest.png`.
  - `EngineShape.mat`: `_BaseMap` → `Mun_A_P_4_ColorMapD.png`; `_EmissionMap` → `Mun_A_P_4_ColorMapD.png`; `_MainTex` → `Mun_A_P_4_ColorMapD.png`.
  - `SecAr.mat`: `_BaseMap` → `Munificent_wings-S7.png`; `_MainTex` → `Munificent_wings-S7.png`.
  - `Shields_Mat.mat`: `_BaseMap` → `CWS7-Shields.png`; `_MainTex` → `CWS7-Shields.png`.
  - `SuperstructureShape.mat`: `_BaseMap` → `Munificent_wings.png`; `_EmissionMap` → `Munificent_wings-S7.png`; `_MainTex` → `Munificent_wings.png`.
  - `WeaponsShape.mat`: `_BaseMap` → `Mun_A_P_4_ColorMapD.png`; `_MainTex` → `Mun_A_P_4_ColorMapD.png`.

### Providence — file manifest

- Source root: `Assets/Art/Models/SeparatistShip/Providence/`.
- OBJ names missing CW-Providence.mtl; explicit importer remap currently exists.
- Keep external material mapping; source repair is an explicit separate preflight decision.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `PDXmat_jorodoxShape` → `ced9830874250b5459a94322fb841505`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `CW-Providence.obj` | `Art/Models/SeparatistShips/Providence/Providence.obj` | `4ebda68e096499f4a93f35798ef2c8f3` | — | 3 | MOVE/RENAME |
| `DefaultNormal.png` | `Art/Textures/Models/SeparatistShips/Providence/Providence_Hull_Normal.png` | `c1d0acc6eb916134aab3dfa218fef36e` | 2048×2048 | 2 | MOVE/RENAME |
| `Emission.png` | `Art/Textures/Models/SeparatistShips/Providence/Providence_Hull_Emissive.png` | `96ff56a3812ffbc44a993184e52b83d5` | 2048×2048 | 2 | MOVE/RENAME |
| `PDXmat_jorodoxShape.mat` | `Art/Materials/Models/SeparatistShips/Providence/Providence_Hull.mat` | `ced9830874250b5459a94322fb841505` | — | 2 | MOVE/RENAME |
| `Prov_A_1_Specular.png` | `Art/Textures/Models/SeparatistShips/Providence/Providence_Hull_Specular.png` | `66a68db85f368ef498c69afce18dec82` | 2048×2048 | 2 | MOVE/RENAME |
| `StandartMilitaryTexture-CW.png` | `Art/Textures/Models/SeparatistShips/Providence/Providence_Hull_Albedo.png` | `12cd8132d379db44589b527b113bfbae` | 2048×2048 | 2 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `PDXmat_jorodoxShape.mat`: `_BaseMap` → `StandartMilitaryTexture-CW.png`; `_BumpMap` → `DefaultNormal.png`; `_EmissionMap` → `Emission.png`; `_MainTex` → `StandartMilitaryTexture-CW.png`; `_SpecGlossMap` → `Prov_A_1_Specular.png`.

### Recusant — file manifest

- Source root: `Assets/Art/Models/SeparatistShip/Recusant/`.
- Set01 normal is _BumpMap; Set02 normal is _DetailNormalMap. Keep current channel/UV behavior.
- The OBJ names a missing CW-Recusant-S3E2.mtl; do not invent a replacement.
- Importer: `2` materialImportMode; 1 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `defaultMat` → `6ceae1a63daa0df4fa62084c298f008e`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `CW-Recusant-S3E2.obj` | `Art/Models/SeparatistShips/Recusant/Recusant.obj` | `dde8788016e7b38419c94ee83235b659` | — | 3 | MOVE/RENAME |
| `defaultMat.mat` | `Art/Materials/Models/SeparatistShips/Recusant/Recusant_Hull.mat` | `6ceae1a63daa0df4fa62084c298f008e` | — | 1 | MOVE/RENAME |
| `textures/CWS3E2-Recusant.png` | `Art/Textures/Models/SeparatistShips/Recusant/Recusant_Set01_Albedo.png` | `d720b8e5c54801a4ea397e620ed379e1` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/CWS3E2-RecusantP2.png` | `Art/Textures/Models/SeparatistShips/Recusant/Recusant_Set02_Albedo.png` | `8b5ff1f7b655ee34ab1994e19fca0e5a` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/RecA-N.png` | `Art/Textures/Models/SeparatistShips/Recusant/Recusant_Set01_Normal.png` | `225984047ac6960489c2f3e1d07d0ba4` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/RecB-N.png` | `Art/Textures/Models/SeparatistShips/Recusant/Recusant_Set02_Normal.png` | `5e0a613a96c29c64ab37ed26224747c2` | 2048×2048 | 2 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `defaultMat.mat`: `_BaseMap` → `CWS3E2-Recusant.png`; `_BumpMap` → `RecA-N.png`; `_DetailNormalMap` → `RecB-N.png`; `_MainTex` → `CWS3E2-Recusant.png`.

### AcclamatorAssault — file manifest

- Source root: `Assets/Art/Models/SeparatistShip/republic assault ship/`.
- Preserve 10 images across textures and .fbm; each five-file pair is byte-identical but uses different GUIDs.
- No new Hull material is created; existing embedded/source material behavior must be captured.
- Thumbs.db files remain KEEP in this naming manifest; OS cleanup is a separate operation.
- Importer: `1` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `republic assault ship.dae` | `Art/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault.dae` | `305e7a01bfca3324cbdd21c9278d77a7` | — | 0 | MOVE/RENAME |
| `republic assault ship.fbm/Dirty shiny.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_DirtyShiny_Source_FbmCopy.jpg` | `0058c8abde44d674e92fe38c2ffbeb2c` | 200×200 | 0 | MOVE/RENAME |
| `republic assault ship.fbm/Dirty2 Spec.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_Dirty02_Specular_FbmCopy.jpg` | `186c5271e74c7e544bcae086e9d8994c` | 1000×172 | 0 | MOVE/RENAME |
| `republic assault ship.fbm/Dirty2.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_Dirty02_Source_FbmCopy.jpg` | `7c0d681a6a8fdaa4798263864b08701c` | 1000×172 | 0 | MOVE/RENAME |
| `republic assault ship.fbm/Engine panels.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_EnginePanels_Source_FbmCopy.jpg` | `f6807c2d3f3fd1a41b00523b1012237e` | 1000×302 | 0 | MOVE/RENAME |
| `republic assault ship.fbm/General Panels.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_GeneralPanels_Source_FbmCopy.jpg` | `a5b27a14a7520a145884006d752d3d43` | 1000×1000 | 0 | MOVE/RENAME |
| `republic assault ship.fbm/Thumbs.db` | `Art/Models/SeparatistShip/republic assault ship/republic assault ship.fbm/Thumbs.db` | `3d2e40f5a784ac344be2f667878cddda` | — | 0 | KEEP |
| `textures/Dirty shiny.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_DirtyShiny_Source.jpg` | `11d7b6c7a84e7a04d9c3bea13bd71905` | 200×200 | 0 | MOVE/RENAME |
| `textures/Dirty2 Spec.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_Dirty02_Specular.jpg` | `7c5e8ab4099eadf43ba9a5f95facfbfe` | 1000×172 | 0 | MOVE/RENAME |
| `textures/Dirty2.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_Dirty02_Source.jpg` | `a094a056a67e8dc44b465280a1d7bca9` | 1000×172 | 0 | MOVE/RENAME |
| `textures/Engine panels.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_EnginePanels_Source.jpg` | `17f4b4f5894743f408613a7a85866cd6` | 1000×302 | 0 | MOVE/RENAME |
| `textures/General Panels.jpg` | `Art/Textures/Models/RepublicShips/AcclamatorAssault/AcclamatorAssault_GeneralPanels_Source.jpg` | `7b6fd099878b5ba41992b4d74d33ec21` | 1000×1000 | 0 | MOVE/RENAME |
| `textures/Thumbs.db` | `Art/Models/SeparatistShip/republic assault ship/textures/Thumbs.db` | `b02f1af2d171904478dfbeda291a2a94` | — | 0 | KEEP |

### Freeport — file manifest

- Source root: `Assets/Art/Models/Station/FreePort/`.
- Keep six materials, including separate Window and WindowSG identities; no four-material collapse.
- Floor source is bound as color, despite rough in its filename.
- Importer: `2` materialImportMode; 6 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Material #2142150880` → `4dfba61746931cb409bb84a4f611fb54`; `ORDNUNGSKRAFT Graviton` → `950c3f722ef803545ae1f12e1639834c`; `SG-Gold-glow` → `ba3a6bc51d419db49b5087f0aba77405`; `SG-LightW` → `d818221022de5d847978308668a19c52`; `SG-Window` → `f2fe5dc07cc7cff44a01d7537372dd8a`; `Window` → `008f07fd63527d74db67c3f064448619`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Freeport Space Station1.dae` | `Art/Models/SpaceStations/Freeport/Freeport.dae` | `617ac06f5dd43644bb3e455b463f9c32` | — | 2 | MOVE/RENAME |
| `Material #2142150880.mat` | `Art/Materials/Models/SpaceStations/Freeport/Freeport_Floor.mat` | `4dfba61746931cb409bb84a4f611fb54` | — | 2 | MOVE/RENAME |
| `ORDNUNGSKRAFT Graviton.mat` | `Art/Materials/Models/SpaceStations/Freeport/Freeport_Graviton.mat` | `950c3f722ef803545ae1f12e1639834c` | — | 2 | MOVE/RENAME |
| `SG-Gold-glow.mat` | `Art/Materials/Models/SpaceStations/Freeport/Freeport_GlowGold.mat` | `ba3a6bc51d419db49b5087f0aba77405` | — | 2 | MOVE/RENAME |
| `SG-LightW.mat` | `Art/Materials/Models/SpaceStations/Freeport/Freeport_LightW.mat` | `d818221022de5d847978308668a19c52` | — | 2 | MOVE/RENAME |
| `SG-Window.mat` | `Art/Materials/Models/SpaceStations/Freeport/Freeport_WindowSG.mat` | `f2fe5dc07cc7cff44a01d7537372dd8a` | — | 2 | MOVE/RENAME |
| `textures/tech_floor_8_rough_by_artofsoulburn_d4t7ff.jpg` | `Art/Textures/Models/SpaceStations/Freeport/Freeport_Floor_Albedo_Source08.jpg` | `3012209c80ffa234ea3f7125db44a5aa` | 661×1024 | 2 | MOVE/RENAME |
| `Window.mat` | `Art/Materials/Models/SpaceStations/Freeport/Freeport_Window.mat` | `008f07fd63527d74db67c3f064448619` | — | 2 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Material #2142150880.mat`: `_BaseMap` → `tech_floor_8_rough_by_artofsoulburn_d4t7ff.jpg`; `_MainTex` → `tech_floor_8_rough_by_artofsoulburn_d4t7ff.jpg`.

### OpenSpaceStation — file manifest

- Source root: `Assets/Art/Models/Station/futuristic-open-concept-space-station/`.
- Keep six materials and three images. Unknown numeric materials retain numeric slot identities.
- Do not label both hull images as one interchangeable albedo.
- Importer: `2` materialImportMode; 6 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Engine` → `4779a40844d58454782fb3cc7158fbe8`; `LIGHTKRAFT GRAVITON_` → `64c0cf51fb7fb4a4baa262fa35fcee68`; `Material #2142147988` → `73aa3bdf7bebfd04d83edb2f52ec31f5`; `Material #2142150746` → `d0a3335f3e39f7e4eafc0f9beeb2daca`; `ORDNUNGSKRAFT Graviton` → `a2022a288266f714c9c9f42cd2137a03`; `SG-Gold-glow` → `43b74a9d824acf54393a328c61ca0dbc`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Engine.mat` | `Art/Materials/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Engine.mat` | `4779a40844d58454782fb3cc7158fbe8` | — | 1 | MOVE/RENAME |
| `LIGHTKRAFT GRAVITON_.mat` | `Art/Materials/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_LightkraftGraviton.mat` | `64c0cf51fb7fb4a4baa262fa35fcee68` | — | 1 | MOVE/RENAME |
| `Material #2142147988.mat` | `Art/Materials/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Slot2142147988.mat` | `73aa3bdf7bebfd04d83edb2f52ec31f5` | — | 1 | MOVE/RENAME |
| `Material #2142150746.mat` | `Art/Materials/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Slot2142150746.mat` | `d0a3335f3e39f7e4eafc0f9beeb2daca` | — | 1 | MOVE/RENAME |
| `OpenFreeSpaceStiation.dae` | `Art/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation.dae` | `445b03b7472e15c44bf58ea59c5df65a` | — | 0 | MOVE/RENAME |
| `ORDNUNGSKRAFT Graviton.mat` | `Art/Materials/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Graviton.mat` | `a2022a288266f714c9c9f42cd2137a03` | — | 1 | MOVE/RENAME |
| `SG-Gold-glow.mat` | `Art/Materials/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_GlowGold.mat` | `43b74a9d824acf54393a328c61ca0dbc` | — | 1 | MOVE/RENAME |
| `textures/8657.jpg` | `Art/Textures/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Set8657_Albedo.jpg` | `749b504b064c2b04aa72718e933b6091` | 512×512 | 1 | MOVE/RENAME |
| `textures/starship_hull_12_by_artofsoulburn_d91kpch.jpg` | `Art/Textures/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Hull12_Source.jpg` | `65c86586038ffcb4eb8cd7dc8bbf7f80` | 661×1024 | 0 | MOVE/RENAME |
| `textures/starship_hull_13_by_artofsoulburn_d91kpjr.jpg` | `Art/Textures/Models/SpaceStations/OpenSpaceStation/OpenSpaceStation_Hull13_Albedo.jpg` | `ddc89333ca8bc6b4e905577868fbb7a8` | 661×1024 | 1 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Material #2142147988.mat`: `_BaseMap` → `8657.jpg`; `_MainTex` → `8657.jpg`.
  - `Material #2142150746.mat`: `_BaseMap` → `starship_hull_13_by_artofsoulburn_d91kpjr.jpg`; `_MainTex` → `starship_hull_13_by_artofsoulburn_d91kpjr.jpg`.

### GangutStation — file manifest

- Source root: `Assets/Art/Models/Station/gangut-space-hub/`.
- Retain Set01/02/03 material/texture identities. The two lambert1_ShipLit maps are cannon-owned.
- Set02 emission currently uses Set01 emission; this assignment is preserved, not corrected.
- Importer: `2` materialImportMode; 3 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `set1` → `115e4628d37a6c9469368102084b126b`; `set2` → `236beb0066d7e744db2de3242ea86855`; `set3` → `a01ae0773473bf247ba98bcad41beb8e`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `source/Gangut.fbx` | `Art/Models/SpaceStations/GangutStation/GangutStation.fbx` | `9aa986855d4a67240bdf0c3060ad971e` | — | 2 | MOVE/RENAME |
| `source/set1.mat` | `Art/Materials/Models/SpaceStations/GangutStation/GangutStation_Set01.mat` | `115e4628d37a6c9469368102084b126b` | — | 2 | MOVE/RENAME |
| `source/set2.mat` | `Art/Materials/Models/SpaceStations/GangutStation/GangutStation_Set02.mat` | `236beb0066d7e744db2de3242ea86855` | — | 2 | MOVE/RENAME |
| `source/set3.mat` | `Art/Materials/Models/SpaceStations/GangutStation/GangutStation_Set03.mat` | `a01ae0773473bf247ba98bcad41beb8e` | — | 2 | MOVE/RENAME |
| `textures/lambert1_ShipLit_MetallicSmoothness.png` | `Art/Textures/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon_Surface_ShipLit_MetallicSmoothness.png` | `5c155bdca19ecc747aae2eb9357451f7` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/lambert1_ShipLit_Occlusion.png` | `Art/Textures/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon_Surface_ShipLit_Occlusion.png` | `edafdcb457e823d48b00031a87b5dde6` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/set1_MetallicSmoothness.png` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_MetallicSmoothness.png` | `75a8efd4478ffb64a863392bbf317cae` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/set1_Occlusion.png` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_Occlusion.png` | `366433e7a92bd704cbf081c32e2aff79` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/set2_MetallicSmoothness.png` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_MetallicSmoothness.png` | `0e1548d7d2861e743a0227f5ba927218` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/set2_Occlusion.png` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_Occlusion.png` | `74220f77dbcc71c4d9fbba3df85e782f` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/set3_MetallicSmoothness.png` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_MetallicSmoothness.png` | `9a7cad7770a49f44f85e7bb7576821ee` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/set3_Occlusion.png` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_Occlusion.png` | `3ad6a4353f65a07468dd433efd9644a4` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/stationSet01_albedo.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_Albedo.jpg` | `490a2e2c117ff6d43aa7a266b22e4ae1` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/stationSet01_AO.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_AO.jpg` | `2d961b2216afabf499404f629f5d0beb` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/stationSet01_emissive.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_Emissive.jpg` | `dabfc226401f65045ab42f1c3ee89690` | 2048×2048 | 4 | MOVE/RENAME |
| `textures/stationSet01_metallic.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_Metallic.jpg` | `f3e045d7cb9b40f4a908277e5023c1c0` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/stationSet01_normal.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_Normal.jpg` | `9d7aef5d1c4e66942a8e8e3896337bb3` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/stationSet01_roughness.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set01_Roughness.jpg` | `2aa47e500b1ec8843bc91ac389bddb48` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/StationSet02_albedo.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_Albedo.jpg` | `244ec405b35d77840853508f5d7cb8a0` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/StationSet02_AO.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_AO.jpg` | `aff66505c3444434c9e63e706c1c31da` | 2048×2048 | 1 | MOVE/RENAME |
| `textures/StationSet02_metallic.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_Metallic.jpg` | `d4bb4341531fdfb4ea12d19414eeff12` | 2048×2048 | 1 | MOVE/RENAME |
| `textures/StationSet02_normal.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_Normal.jpg` | `f83324aa5c835344ea222ed22fc7bb7d` | 2048×2048 | 5 | MOVE/RENAME |
| `textures/StationSet02_roughness.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set02_Roughness.jpg` | `217a561592e5db4439f1e9a907cd202b` | 2048×2048 | 5 | MOVE/RENAME |
| `textures/StationSet03_albedo.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_Albedo.jpg` | `bbf874610eec57f4d81cdb4dbc9d194a` | 2048×2048 | 2 | MOVE/RENAME |
| `textures/StationSet03_AO.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_AO.jpg` | `27833308f2b9fcf44ac1e1a6f062c0cc` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/StationSet03_emissive.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_Emissive.jpg` | `a0380963705d8584781fa04eac22d59e` | 2048×2048 | 8 | MOVE/RENAME |
| `textures/StationSet03_metallic.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_Metallic.jpg` | `327f2531b4f91ea4c838de5b825743b6` | 2048×2048 | 0 | MOVE/RENAME |
| `textures/StationSet03_normal.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_Normal.jpg` | `efa799da11f408447b1e8f5dd29c11c2` | 2048×2048 | 5 | MOVE/RENAME |
| `textures/StationSet03_roughness.jpg` | `Art/Textures/Models/SpaceStations/GangutStation/GangutStation_Set03_Roughness.jpg` | `d44f500588fdd264097b23e9e1d7d3f1` | 2048×2048 | 2 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `source/set1.mat`: `_BaseMap` → `stationSet01_albedo.jpg`; `_BumpMap` → `stationSet01_normal.jpg`; `_EmissionMap` → `stationSet01_emissive.jpg`; `_MainTex` → `stationSet01_albedo.jpg`; `_MetallicGlossMap` → `set1_MetallicSmoothness.png`; `_OcclusionMap` → `set1_Occlusion.png`; `_SpecGlossMap` → `stationSet01_roughness.jpg`.
  - `source/set2.mat`: `_BaseMap` → `StationSet02_albedo.jpg`; `_BumpMap` → `StationSet02_normal.jpg`; `_EmissionMap` → `stationSet01_emissive.jpg`; `_MainTex` → `StationSet02_albedo.jpg`; `_MetallicGlossMap` → `set2_MetallicSmoothness.png`; `_OcclusionMap` → `set2_Occlusion.png`; `_SpecGlossMap` → `StationSet02_roughness.jpg`.
  - `source/set3.mat`: `_BaseMap` → `StationSet03_albedo.jpg`; `_BumpMap` → `StationSet03_normal.jpg`; `_EmissionMap` → `StationSet03_emissive.jpg`; `_MainTex` → `StationSet03_albedo.jpg`; `_MetallicGlossMap` → `set3_MetallicSmoothness.png`; `_OcclusionMap` → `set3_Occlusion.png`; `_SpecGlossMap` → `StationSet03_roughness.jpg`.

### RefuelingStation — file manifest

- Source root: `Assets/Art/Models/Station/refueling-station/`.
- MiningFacilityHull root material uses this model's FuelStation.png and maps here as RefuelingStation_Hull.mat.
- No explicit importer remaps detected; embedded/current renderer slots require live capture.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Refueling Station.fbx` | `Art/Models/SpaceStations/RefuelingStation/RefuelingStation.fbx` | `6b8d5eeffe1ec114ebe2d4c6ae7efc5b` | — | 3 | MOVE/RENAME |
| `textures/FuelStation.png` | `Art/Textures/Models/SpaceStations/RefuelingStation/RefuelingStation_Hull_Albedo.png` | `eb8c1db110d61974a91a2de7df0a5a0f` | 4096×4096 | 2 | MOVE/RENAME |
| `textures/RSHangar.jpeg` | `Art/Textures/Models/SpaceStations/RefuelingStation/RefuelingStation_Hangar_Source.jpeg` | `377f80dc6ac61204691e8c828420d9cb` | 4096×4096 | 0 | MOVE/RENAME |

### AsteroidMiningFacility — file manifest

- Source root: `Assets/Art/Models/Station/space-station-asteroid-mining-facility/`.
- Keep seven materials and five textures; Glass_AOSource is also used by shared Shields material.
- Shared mining mesh consumers include asteroid obstacles, defense platform, capture-site prefabs and scenes.
- Importer: `2` materialImportMode; 7 explicit remaps. Mode values are raw serialized data; do not change them by convention.
- Remap keys → GUID: `Material #2142150779` → `2456d64a955fcb8448e4998a3c9aaff2`; `Material #2142150880` → `c0aa5a53142995f4b84b87043f6301ff`; `ORDNUNGSKRAFT Graviton` → `35b27de8c71f21444847384702df21a6`; `SG-Gold-glow` → `4c76a321d6cb7454788619638122c535`; `SG-LightW` → `fb482b55402f30b4098a3461e916c290`; `SG-Window` → `6e46be2e004073f4dbf43b9bf8cc19fe`; `Window` → `409dfbe9fc617f644aac577bd18b3f6b`.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Material #2142150779.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Glass.mat` | `2456d64a955fcb8448e4998a3c9aaff2` | — | 6 | MOVE/RENAME |
| `Material #2142150880.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Hull.mat` | `c0aa5a53142995f4b84b87043f6301ff` | — | 3 | MOVE/RENAME |
| `ORDNUNGSKRAFT Graviton.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Graviton.mat` | `35b27de8c71f21444847384702df21a6` | — | 3 | MOVE/RENAME |
| `SG-Gold-glow.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_GlowGold.mat` | `4c76a321d6cb7454788619638122c535` | — | 3 | MOVE/RENAME |
| `SG-LightW.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_LightW.mat` | `fb482b55402f30b4098a3461e916c290` | — | 3 | MOVE/RENAME |
| `SG-Window.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_WindowSG.mat` | `6e46be2e004073f4dbf43b9bf8cc19fe` | — | 3 | MOVE/RENAME |
| `Space Mining Facility.dae` | `Art/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility.dae` | `3bfdf381e8652d543a4fd74f46116c42` | — | 11 | MOVE/RENAME |
| `textures/glass2_AO.png` | `Art/Textures/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Glass_AOSource.png` | `4977b2be150a30f4188cb3abc3c4f24f` | 4096×4096 | 2 | MOVE/RENAME |
| `textures/glass2_Normal.png` | `Art/Textures/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Glass_Normal.png` | `5c276352f1b289a4880c5c5569399072` | 4096×4096 | 1 | MOVE/RENAME |
| `textures/starship_hull_12_by_artofsoulburn_d91kpch.jpg` | `Art/Textures/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Hull12_Source.jpg` | `ed051ef0294e0b04c9c72cdf834c654c` | 661×1024 | 0 | MOVE/RENAME |
| `textures/starship_hull_13_by_artofsoulburn_d91kpjr.jpg` | `Art/Textures/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Hull13_Albedo.jpg` | `5003604624768ea4ca68e7354b53faf5` | 661×1024 | 3 | MOVE/RENAME |
| `textures/tech_floor_8_rough_by_artofsoulburn_d4t7ff.jpg` | `Art/Textures/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Floor_Source08.jpg` | `ffbdb7324b2fd994881958b98a99d345` | 661×1024 | 0 | MOVE/RENAME |
| `Window.mat` | `Art/Materials/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility_Window.mat` | `409dfbe9fc617f644aac577bd18b3f6b` | — | 3 | MOVE/RENAME |

- Serialized texture assignments (original filenames; includes inactive properties):
  - `Material #2142150779.mat`: `_BaseMap` → `glass2_AO.png`; `_BumpMap` → `glass2_Normal.png`; `_MainTex` → `glass2_AO.png`.
  - `Material #2142150880.mat`: `_BaseMap` → `starship_hull_13_by_artofsoulburn_d91kpjr.jpg`; `_MainTex` → `starship_hull_13_by_artofsoulburn_d91kpjr.jpg`.

### VestaStation — file manifest

- Source root: `Assets/Art/Models/Station/Vesta/`.
- Retain TileU1V1 identity. OBJ→MTL→texture paths must move together.
- Importer: `2` materialImportMode; 0 explicit remaps. Mode values are raw serialized data; do not change them by convention.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `model.mtl` | `Art/Models/SpaceStations/VestaStation/VestaStation.mtl` | `4ceecdadcdce706408d26ae3550ad8f8` | — | 0 | MOVE/RENAME |
| `model.obj` | `Art/Models/SpaceStations/VestaStation/VestaStation.obj` | `032c4beffbf988044b6ff9e0a0bb6ff2` | — | 1 | MOVE/RENAME |
| `tex_u1_v1.jpg` | `Art/Textures/Models/SpaceStations/VestaStation/VestaStation_TileU1V1_Albedo.jpg` | `c43fdae1eebd79b49bd0831fdb01ab80` | 1024×1024 | 0 | MOVE/RENAME |

### HaloStation — file manifest

- Source root: `Assets/Art/Models/Station/Halo-station/`.
- 54 image-only assets; relocate to Textures/Models/SpaceStations/HaloStation rather than pretending this is a mesh bundle.
- Map roles are unknown; Source suffixes preserve filename identity.
- fr_floor_03 and fr_holograms_07 have serialized consumers; do not delete this collection.

| Source relative path | Proposed target relative to Assets | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `fr_arch_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch01_Source.png` | `229ec281732a7d04087105fc7993d813` | 512×1024 | 0 | MOVE/RENAME |
| `fr_arch_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch02_Source.png` | `f6f2f45ef9ddcb541a3ec4c6e4ac88e8` | 512×1024 | 0 | MOVE/RENAME |
| `fr_arch_03.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch03_Source.png` | `e842e769b74ed1543909a1f1eb6cdece` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_arch_04.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch04_Source.png` | `315070f0b15a79243b96cd437dfb0852` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_arch_05.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch05_Source.png` | `ea8d3edaa69bacc40b880f405bfe4e34` | 1024×512 | 0 | MOVE/RENAME |
| `fr_arch_06.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch06_Source.png` | `9bf24670b0a47c545b0c2a26e7ef8a68` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_arch_07.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrArch07_Source.png` | `7a4f5a078de647341a85fa67aea484c7` | 512×1024 | 0 | MOVE/RENAME |
| `fr_box_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrBox01_Source.png` | `a89a61d6bc9abd547a21f6205c20718d` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_column_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrColumn01_Source.png` | `1cc4c67237494b141929a728813efe5f` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_column_02_a.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrColumn02A_Source.png` | `e1fab5b13b96e4c4485ea2a1831ba6d2` | 2048×2048 | 0 | MOVE/RENAME |
| `fr_column_04.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrColumn04_Source.png` | `af7318f086b588240b6b55c23e4db0b7` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_column_05.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrColumn05_Source.png` | `279d8e42246031543add778e33fdc4df` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_corridor_inser_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrCorridorInser01_Source.png` | `30203c2a38b633c41b744e4e132bb1b2` | 2048×2048 | 0 | MOVE/RENAME |
| `fr_dish_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrDish02_Source.png` | `dfde42a887bc79447a83838a4d92d963` | 2048×2048 | 0 | MOVE/RENAME |
| `fr_door_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrDoor01_Source.png` | `963745b4592b55a46ace770caefd2adb` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_door_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrDoor02_Source.png` | `6dc2b4d65d18fb3409295dd9fd000e5d` | 512×1024 | 0 | MOVE/RENAME |
| `fr_door_03.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrDoor03_Source.png` | `1ee32064e2c976d4f9abc61549dcb10e` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_door_04.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrDoor04_Source.png` | `e76fda77274a1ac4bb322a396cc5871a` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_door_case_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrDoorCase01_Source.png` | `f331b7d7e94a2da489ced655824a1255` | 1024×512 | 0 | MOVE/RENAME |
| `fr_fencing_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrFencing01_Source.png` | `cb998d24503e38f41aa0612d227f3dec` | 512×512 | 0 | MOVE/RENAME |
| `fr_floor_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrFloor01_Source.png` | `853ca6c33b38a8849ad77346d638a365` | 512×512 | 0 | MOVE/RENAME |
| `fr_floor_03.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrFloor03_Source.png` | `775d1a1f1bd5f4748bf679487b495e38` | 256×256 | 1 | MOVE/RENAME |
| `fr_girder_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrGirder01_Source.png` | `1e2dc00afc0a5094cba3a489daaf2181` | 512×1024 | 0 | MOVE/RENAME |
| `fr_glass_generic_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrGlassGeneric01_Source.png` | `fbbbe733206060e4eb2c58c8c8263ed5` | 256×256 | 0 | MOVE/RENAME |
| `fr_holograms_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrHolograms02_Source.png` | `d0f29a5707d6948499a23b7cc51b3054` | 512×512 | 0 | MOVE/RENAME |
| `fr_holograms_03.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrHolograms03_Source.png` | `4d576f2929015f64191a414b8fb346f4` | 512×512 | 0 | MOVE/RENAME |
| `fr_holograms_05-06.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrHolograms0506_Source.png` | `0554e526373e3344e9962c6a618a5d80` | 512×512 | 0 | MOVE/RENAME |
| `fr_holograms_07.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrHolograms07_Source.png` | `a15d47a5c33a99c44b79bc1ae7ae6f71` | 1024×1024 | 1 | MOVE/RENAME |
| `fr_holograms_11.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrHolograms11_Source.png` | `894349d7ecf4a344897ee1895a368ab5` | 1024×512 | 0 | MOVE/RENAME |
| `fr_panel_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel01_Source.png` | `a25f546dae7ee87448a59161859e73a8` | 256×1024 | 0 | MOVE/RENAME |
| `fr_panel_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel02_Source.png` | `6af021f1ccdd6a94cbfa874116accfc9` | 256×256 | 0 | MOVE/RENAME |
| `fr_panel_03.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel03_Source.png` | `974237d0ff3142e429f5c61612f01795` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_panel_04.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel04_Source.png` | `d49463c350964cd4aaca05baa670cb34` | 256×128 | 0 | MOVE/RENAME |
| `fr_panel_05.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel05_Source.png` | `00583352bf71cfc4aa862c64450db225` | 256×256 | 0 | MOVE/RENAME |
| `fr_panel_06.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel06_Source.png` | `e5d71d023320edd41adb00e0e0691688` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_panel_08.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel08_Source.png` | `b79066fea045aa94cb6d635286fb709a` | 512×1024 | 0 | MOVE/RENAME |
| `fr_panel_09.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanel09_Source.png` | `082ba12cd40d04b49abc00388e554249` | 512×1024 | 0 | MOVE/RENAME |
| `fr_panel_alcove_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelAlcove01_Source.png` | `f9c0adc5efb811a4197f62ee95014c0c` | 512×512 | 0 | MOVE/RENAME |
| `fr_panel_generic_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelGeneric01_Source.png` | `d844984aa38956044b348b60e2b83e6e` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_panel_hall_floor_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelHallFloor01_Source.png` | `2136c29023131fc458ea6202d61c7d51` | 1024×512 | 0 | MOVE/RENAME |
| `fr_panel_pipe_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelPipe01_Source.png` | `3169bc381cb725841886de1034df4ffc` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_panel_techtrim_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelTechtrim01_Source.png` | `72256e65bfd6252419c8f737585fa08b` | 512×1024 | 0 | MOVE/RENAME |
| `fr_panel_wall_vert_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelWallVert01_Source.png` | `38fc275789e9ac046b93053feb85aa84` | 256×1024 | 0 | MOVE/RENAME |
| `fr_panel_wide_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPanelWide01_Source.png` | `e4aa33e0a9ddace4ca6fbaebf9f53034` | 1024×256 | 0 | MOVE/RENAME |
| `fr_plate_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPlate01_Source.png` | `bae1321da83bf5a4a9f1d02a0eaf9a9a` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_plate_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrPlate02_Source.png` | `c1dd4342a90528147acb1a2d1aa3fece` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_terminal_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrTerminal01_Source.png` | `04b3177f6825dae4eb6e7c1272a96b81` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_terminal_column_01_a.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrTerminalColumn01A_Source.png` | `b6c9ae7dedb45a94a8fe8d0474d61953` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_terminal_column_01_b.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrTerminalColumn01B_Source.png` | `1bdf3b06d232ec74fafd33fc6b3e0a72` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_tunnel_01.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrTunnel01_Source.png` | `c5aea0139de9946469f515973c760b9f` | 512×256 | 0 | MOVE/RENAME |
| `fr_tunnel_02.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrTunnel02_Source.png` | `fb9d6ed45c93dbf418e441cde8ec687c` | 1024×1024 | 0 | MOVE/RENAME |
| `fr_tunnel_03.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_FrTunnel03_Source.png` | `5e17b38edcc107e479bbb41b4fba4f3a` | 1024×1024 | 0 | MOVE/RENAME |
| `lig_fr_lamp1.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_LigFrLamp1_Source.png` | `9404160edcb62e84baa89def5cf91af0` | 256×256 | 0 | MOVE/RENAME |
| `under_tile_04.png` | `Art/Textures/Models/SpaceStations/HaloStation/HaloStation_UnderTile04_Source.png` | `3310fa01ba20f9a41bf6603c1f38e246` | 512×512 | 0 | MOVE/RENAME |

### Retained Assets/Art/Animation — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Animation/Menu/Ship/ShipAnimatorController.controller` | `Art/Animation/Menu/Ship/ShipAnimatorController.controller` | `8f0c7a63b87e8f1419fcc290541b8cb3` | — | 1 | KEEP |
| `Art/Animation/Menu/Ship/ShipCorrusantIdle.anim` | `Art/Animation/Menu/Ship/ShipCorrusantIdle.anim` | `7b294939c784fd74e94ac137100b59b3` | — | 1 | KEEP |

### Loose materials — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Materials/DualProjectileMaterial.mat` | `Art/Materials/Vfx/DualProjectile.mat` | `8c4ff248983300645b2cc553764a063d` | — | 1 | MOVE/RENAME |
| `Art/Materials/Engines.mat` | `Art/Materials/Vfx/Engines.mat` | `f72c9b041839f7549b306c7f3e0db949` | — | 8 | MOVE/RENAME |
| `Art/Materials/FogOfWar 1.mat` | `Art/Materials/FogOfWar/FogOfWar_Coruscant.mat` | `b116f3d3ebb29b34080831046e90a082` | — | 1 | MOVE/RENAME |
| `Art/Materials/FogOfWar.mat` | `Art/Materials/FogOfWar/FogOfWar.mat` | `3a546fe1580b3ee43a66b6cf90f1d216` | — | 1 | MOVE/RENAME |
| `Art/Materials/HexagonTile_HGT.mat` | `Art/Materials/Vfx/HexagonTile_HGT.mat` | `b94cee69189ee3d4ca8d7d1c2d6b10ce` | — | 0 | MOVE/RENAME |
| `Art/Materials/Hologram.mat` | `Art/Materials/Vfx/Hologram.mat` | `4a28dce88e7c1ee4a97e1b87b8b54550` | — | 19 | MOVE/RENAME |
| `Art/Materials/ImpactFlash.mat` | `Art/Materials/Vfx/ImpactFlash.mat` | `e313dbdc95bdbf0469da603499a273e4` | — | 2 | MOVE/RENAME |
| `Art/Materials/ImpactShieldRing.mat` | `Art/Materials/Vfx/ImpactShieldRing.mat` | `20f064b62f11f3d42bdd2d0a22ac8573` | — | 1 | MOVE/RENAME |
| `Art/Materials/ImpactSpark.mat` | `Art/Materials/Vfx/ImpactSpark.mat` | `583ae47d40b49714ab6dc29446ed2c13` | — | 1 | MOVE/RENAME |
| `Art/Materials/LazerProjectileMaterial.mat` | `Art/Materials/Vfx/LaserProjectile.mat` | `13b57cb2bd9f3444d9e0df8c7d7fddbb` | — | 1 | MOVE/RENAME |
| `Art/Materials/Light.mat` | `Art/Materials/Lighting/Light.mat` | `d894af6acce3d9545a6980fc4e5268ac` | — | 0 | MOVE/RENAME |
| `Art/Materials/LineRenderMaterial.mat` | `Art/Materials/Vfx/UnitOrderLine.mat` | `e22dd2524a2ced64b850f380b8488317` | — | 11 | MOVE/RENAME |
| `Art/Materials/Lucrehulk_Shape_019.mat` | `Art/Materials/Models/SeparatistShips/Lucrehulk/Lucrehulk_Slot019.mat` | `c8dc29ce37c90431b9fdcbcfdd1cde22` | — | 1 | MOVE/RENAME |
| `Art/Materials/Lucrehulk_Shape_020.mat` | `Art/Materials/Models/SeparatistShips/Lucrehulk/Lucrehulk_Slot020.mat` | `b0d8b8bb7142047198d067359442e46f` | — | 1 | MOVE/RENAME |
| `Art/Materials/Lucrehulk_Shape_021.mat` | `Art/Materials/Models/SeparatistShips/Lucrehulk/Lucrehulk_Slot021.mat` | `ffb043d0554c542ebad54aeb5cfe3476` | — | 1 | MOVE/RENAME |
| `Art/Materials/Lucrehulk_Shape_022.mat` | `Art/Materials/Models/SeparatistShips/Lucrehulk/Lucrehulk_Slot022.mat` | `099e11d376a584b8fbaecb07ccd26425` | — | 1 | MOVE/RENAME |
| `Art/Materials/MiningFacilityHull.mat` | `Art/Materials/Models/SpaceStations/RefuelingStation/RefuelingStation_Hull.mat` | `5abf28ce5d2764b8fa6680f89a723ad5` | — | 1 | MOVE/RENAME |
| `Art/Materials/ParticlesUnlit.mat` | `Art/Materials/Vfx/ParticlesUnlit.mat` | `18f9c3516faf1de419fdcfbab0dc537f` | — | 0 | MOVE/RENAME |
| `Art/Materials/ProjectileDeploy.mat` | `Art/Materials/Vfx/ProjectileDeploy.mat` | `ca415eaa0f0ee0a47b87445dc484d1ea` | — | 0 | MOVE/RENAME |
| `Art/Materials/ProjectileMaterial.mat` | `Art/Materials/Vfx/Projectile.mat` | `488f2cb48ae5fc54c9b2b9e7dba2614e` | — | 2 | MOVE/RENAME |
| `Art/Materials/ProtonBeamMaterial.mat` | `Art/Materials/Vfx/ProtonBeam.mat` | `0c4432a9db9f312499162b1626ea7864` | — | 1 | MOVE/RENAME |
| `Art/Materials/ProtonTorpedoTrail.mat` | `Art/Materials/Vfx/ProtonTorpedoTrail.mat` | `a6a484c652f974649bb5b01ea39994ee` | — | 1 | MOVE/RENAME |
| `Art/Materials/Shields.mat` | `Art/Materials/Vfx/Shields.mat` | `e8a89786737f96344932ad5bf02e90b2` | — | 7 | MOVE/RENAME |
| `Art/Materials/ShipShield.mat` | `Art/Materials/Vfx/ShipShield.mat` | `95ddb46ba62b3544bbfda394a9a3040b` | — | 14 | MOVE/RENAME |
| `Art/Materials/Test.mat` | `Art/Materials/Unclassified/Test.mat` | `fa616bc7c835da44487034919b76db8b` | — | 0 | MOVE/RENAME |
| `Art/Materials/Transparent.mat` | `Art/Materials/Unclassified/Transparent.mat` | `1daff12fd60684c41b6661e12454971f` | — | 0 | MOVE/RENAME |
| `Art/Materials/UnitDefaultLit.mat` | `Art/Materials/Units/UnitDefaultLit.mat` | `c29b50db6ad614f71bfc897aa4b6401d` | — | 3 | MOVE/RENAME |
| `Art/Materials/VolumetricNebulaMat.mat` | `Art/Materials/Vfx/Nebula/VolumetricNebula.mat` | `32f2be3ffa99e4d46b238fb3c6acc39f` | — | 0 | MOVE/RENAME |

- Serialized texture assignments:
  - `DualProjectileMaterial.mat`: `_BaseMap` → `21bf9efbdbbbd4e34af72f1de5e334f6`; `_EmissionMap` → `21bf9efbdbbbd4e34af72f1de5e334f6`; `_MainTex` → `Art/Textures/BlasterBoltTilled.png`.
  - `Engines.mat`: `_BaseMap` → `ThirdParty/ParticleProFX/Resources/Textures/smokeSheet03.png`; `_MainTex` → `ThirdParty/ParticleProFX/Resources/Textures/smokeSheet03.png`.
  - `HexagonTile_HGT.mat`: `_BaseMap` → `Art/Textures/HexagonTile_HGT.png`; `_MainTex` → `Art/Textures/HexagonTile_HGT.png`.
  - `ImpactFlash.mat`: `_BaseMap` → `ThirdParty/ParticleProFX/Resources/Textures/star01.png`.
  - `ImpactShieldRing.mat`: `_BaseMap` → `ThirdParty/ParticleProFX/Resources/Textures/shockwaveSingle.png`.
  - `ImpactSpark.mat`: `_BaseMap` → `ThirdParty/ParticleProFX/Resources/Textures/fadedline.png`.
  - `LazerProjectileMaterial.mat`: `_BaseMap` → `Art/Textures/BlasterBolt.png`; `_EmissionMap` → `21bf9efbdbbbd4e34af72f1de5e334f6`.
  - `Lucrehulk_Shape_019.mat`: `_BaseMap` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship_03.png`; `_MainTex` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship_03.png`.
  - `Lucrehulk_Shape_020.mat`: `_BaseMap` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship.png`; `_MainTex` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship.png`.
  - `Lucrehulk_Shape_021.mat`: `_BaseMap` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship_02.png`; `_MainTex` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship_02.png`.
  - `Lucrehulk_Shape_022.mat`: `_BaseMap` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship_02.png`; `_MainTex` → `Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship_02.png`.
  - `MiningFacilityHull.mat`: `_BaseMap` → `Art/Models/Station/refueling-station/textures/FuelStation.png`; `_MainTex` → `Art/Models/Station/refueling-station/textures/FuelStation.png`.
  - `ParticlesUnlit.mat`: `_BaseMap` → `Art/Textures/BlasterBolt.png`; `_MainTex` → `0000000000000000f000000000000000`.
  - `ProjectileDeploy.mat`: `_BaseMap` → `ThirdParty/ParticleProFX/Resources/Textures/smoke04_sheet.png`; `_EmissionMap` → `21bf9efbdbbbd4e34af72f1de5e334f6`; `_MainTex` → `ea3fc985fb308433eb7ea824447f0426`.
  - `ProjectileMaterial.mat`: `_BaseMap` → `Art/Textures/BlasterBolt.png`; `_EmissionMap` → `21bf9efbdbbbd4e34af72f1de5e334f6`.
  - `ProtonBeamMaterial.mat`: `_BaseMap` → `Art/Textures/laser-beam-effect-photoshop-free-overlay-texture.jpg`; `_EmissionMap` → `Art/Textures/laser-beam-effect-photoshop-free-overlay-texture.jpg`.
  - `ProtonTorpedoTrail.mat`: `_BaseMap` → `ThirdParty/ParticleProFX/Resources/Textures/default.png`.
  - `Shields.mat`: `_BaseMap` → `Art/Models/Station/space-station-asteroid-mining-facility/textures/glass2_AO.png`; `_MainTex` → `Art/Models/Station/space-station-asteroid-mining-facility/textures/glass2_AO.png`; `_Maintexture` → `ThirdParty/Dark UI/Free/64.png`; `_Secondarytexture` → `ThirdParty/Dark UI/Free/32.png`.
  - `Test.mat`: `_MainTex` → `1b62799c2814d6d4b81ee977cfc9e12f`.
  - `Transparent.mat`: `_BaseMap` → `26a0dfdea3e2a466d97d51af0372a6c5`; `_MainTex` → `26a0dfdea3e2a466d97d51af0372a6c5`.
  - `VolumetricNebulaMat.mat`: `_DetailTex` → `Art/Shaders/noise_detail_2025_05_24_16_57_32.png`; `_MainTex` → `Art/Shaders/noise_main_2025_05_24_16_57_36.png`.

### Retained Assets/Art/Materials — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Materials/ReinforcementZones/ReinforcementZone.mat` | `Art/Materials/ReinforcementZones/ReinforcementZone.mat` | `e8752bffdc76c164587ad8c6f6c23ab3` | — | 3 | KEEP |
| `Art/Materials/Vfx/IonArcGlow.mat` | `Art/Materials/Vfx/IonArcGlow.mat` | `352d1200573a78d44aa21acf46e36671` | — | 1 | KEEP |
| `Art/Materials/Vfx/IonField.mat` | `Art/Materials/Vfx/IonField.mat` | `95b0920aa75e7ca4d8ae0c4d15b826c6` | — | 1 | KEEP |
| `Art/Materials/Vfx/IonImpactFlash.mat` | `Art/Materials/Vfx/IonImpactFlash.mat` | `53865f11afcb82047b4ee76e68de4be7` | — | 1 | KEEP |
| `Art/Materials/Vfx/IonPulseGlow.mat` | `Art/Materials/Vfx/IonPulseGlow.mat` | `7e9d1d0469190b14a896ff3d8696a013` | — | 1 | KEEP |
| `Art/Materials/Vfx/MuzzleFlash.mat` | `Art/Materials/Vfx/MuzzleFlash.mat` | `538aea15efdeb0d41a5a9b46ede4b2e5` | — | 8 | KEEP |
| `Art/Materials/Vfx/Nebula/LineBorder.mat` | `Art/Materials/Vfx/Nebula/LineBorder.mat` | `8e7900743b81a5f4a9841a4079d5dc50` | — | 2 | KEEP |
| `Art/Materials/Vfx/Nebula/LineBorderInner.mat` | `Art/Materials/Vfx/Nebula/LineBorderInner.mat` | `f07811d0333a0ae4db421d3ea69fdcc2` | — | 1 | KEEP |
| `Art/Materials/Vfx/Nebula/LineBorderWall.mat` | `Art/Materials/Vfx/Nebula/LineBorderWall.mat` | `cb6b48c35db48cc44b1a7d5e70c5a08a` | — | 1 | KEEP |
| `Art/Materials/Vfx/Nebula/NebulaCloudVolume.mat` | `Art/Materials/Vfx/Nebula/NebulaCloudVolume.mat` | `2f9fb832425c34770ad5452ae6808704` | — | 1 | KEEP |
| `Art/Materials/Vfx/Nebula/NebulaMultiply.mat` | `Art/Materials/Vfx/Nebula/NebulaMultiply.mat` | `7d012fe999631904ab2aa8298ed5eebe` | — | 2 | KEEP |
| `Art/Materials/Vfx/Nebula/NebulaOneVfx.mat` | `Art/Materials/Vfx/Nebula/NebulaOneVfx.mat` | `5a43442928c59e24e807f0049f0ca7ea` | — | 0 | KEEP |
| `Art/Materials/Vfx/Nebula/StarMaterial.mat` | `Art/Materials/Vfx/Nebula/StarMaterial.mat` | `2a837ac21b6b98f46a8ba2ea4606a8c9` | — | 1 | KEEP |
| `Art/Materials/Vfx/Nebula/VolumetricNebulaSystemMat.mat` | `Art/Materials/Vfx/Nebula/VolumetricNebulaSystemMat.mat` | `bc6295f4aad0d4d4caf721307e873136` | — | 1 | KEEP |

### Additional punctuation cleanup — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Materials/Vfx/Nebula/NebulaTwoVfx 1.mat` | `Art/Materials/Vfx/Nebula/NebulaTwoVfx_01.mat` | `4ea73f663a8401c4892cf9a152551000` | — | 1 | MOVE/RENAME |
| `Art/Materials/Vfx/Nebula/NebulaTwoVfx 2.mat` | `Art/Materials/Vfx/Nebula/NebulaTwoVfx_02.mat` | `0b3922197b20d77459bcebf713bed3fe` | — | 0 | MOVE/RENAME |
| `Art/Textures/Particles/Nebula-1.png` | `Art/Textures/Particles/Nebula01.png` | `665f440806abcdf40bf4d1983b0030d6` | 256×256 | 1 | MOVE/RENAME |
| `Art/Textures/Particles/Nebula-2.png` | `Art/Textures/Particles/Nebula02.png` | `6e2f728562a032b47b75218c978629d2` | 512×512 | 2 | MOVE/RENAME |
| `Art/Textures/Particles/Starry-Sky-2K.png` | `Art/Textures/Particles/StarrySky_2K.png` | `50873c9842f29ff4b8d3b131b47ec23e` | 2048×2048 | 0 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/ShipIcon/Imperial-Star Destroyer-128.png` | `Art/Textures/Ui/Icons/ShipIcon/ImperialStarDestroyer_128.png` | `1930ae3e03e02e045a6158c91e50b9f7` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/Victory-I-icon-upsidedown.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VictoryI_UpsideDown.png` | `674a48de425d4ca42befbccb785f3e5d` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/Ui/fast-forward.png` | `Art/Textures/Ui/Icons/Ui/FastForward.png` | `28894ecd212bb4747b457aba06a96224` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/Ui/level-up.png` | `Art/Textures/Ui/Icons/Ui/LevelUp.png` | `2c7347a89cf4cc4448b118d2c696cc1b` | 1254×1254 | 1 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/Ui/minus-sign.png` | `Art/Textures/Ui/Icons/Ui/MinusSign.png` | `647de4577e016e64ba0cd03cba76718c` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/Ui/pause-play.png` | `Art/Textures/Ui/Icons/Ui/PausePlay.png` | `87b283b89b9abda41a3e87fa8097b1f0` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/Ui/Icons/Ui/play-button.png` | `Art/Textures/Ui/Icons/Ui/PlayButton.png` | `e039a9696f7e9e046a884e69d784bcc9` | 512×512 | 0 | MOVE/RENAME |

### Wreck materials — file manifest

- Both paths below are relative to `Assets/`.
- Wreck association is derived from original source names and existing builder behavior; verify source/wreck renderer slots before execution.
- Keep each existing unit folder; rename existing material objects and preserve GUIDs rather than recreating them.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Materials/Wrecks/Acclamator/object_0_Wreck.mat` | `Art/Materials/Wrecks/Acclamator/Acclamator_Slot00_Wreck.mat` | `3897d22a61f596c48a99679f315a25f6` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Acclamator/object_1_Wreck.mat` | `Art/Materials/Wrecks/Acclamator/Acclamator_Slot01_Wreck.mat` | `ca83d167b71ba0246b4189a0ee8d1e96` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidDefendPlatform/lambert1_ShipLit_Wreck.mat` | `Art/Materials/Wrecks/AsteroidDefendPlatform/HeavyTurbolaserCannon_Surface_ShipLit_Wreck.mat` | `3ed00b56f4f34184a9caf8b7b5dacc87` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidDefendPlatform/Material #2142150880_Wreck.mat` | `Art/Materials/Wrecks/AsteroidDefendPlatform/AsteroidMiningFacility_Hull_Wreck.mat` | `216a12c6373e3cc40ba355c1af0fd76c` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidDefendPlatform/ORDNUNGSKRAFT Graviton_Wreck.mat` | `Art/Materials/Wrecks/AsteroidDefendPlatform/AsteroidMiningFacility_Graviton_Wreck.mat` | `04f5c1d238bd25941a7a2408db4c7eab` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidDefendPlatform/SG-Gold-glow_Wreck.mat` | `Art/Materials/Wrecks/AsteroidDefendPlatform/AsteroidMiningFacility_GlowGold_Wreck.mat` | `60692e31153015f498b6456b347211b9` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidDefendPlatform/SG-LightW_Wreck.mat` | `Art/Materials/Wrecks/AsteroidDefendPlatform/AsteroidMiningFacility_LightW_Wreck.mat` | `7a05e5911b5991842aa6d2ae1d4beabe` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidDefendPlatform/Window_Wreck.mat` | `Art/Materials/Wrecks/AsteroidDefendPlatform/AsteroidMiningFacility_Window_Wreck.mat` | `8f5c8ccc4b55a3d48ac743e1b49d73aa` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidMiningFacility/Material #2142150880_Wreck.mat` | `Art/Materials/Wrecks/AsteroidMiningFacility/AsteroidMiningFacility_Hull_Wreck.mat` | `f6bc7848f35f906459fe785b5ea3df6f` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidMiningFacility/ORDNUNGSKRAFT Graviton_Wreck.mat` | `Art/Materials/Wrecks/AsteroidMiningFacility/AsteroidMiningFacility_Graviton_Wreck.mat` | `edfec69d16be7054086aa91d6f0e06e9` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidMiningFacility/SG-Gold-glow_Wreck.mat` | `Art/Materials/Wrecks/AsteroidMiningFacility/AsteroidMiningFacility_GlowGold_Wreck.mat` | `ef53830f10ae6bd4eac37ee12fdbe2a0` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidMiningFacility/SG-LightW_Wreck.mat` | `Art/Materials/Wrecks/AsteroidMiningFacility/AsteroidMiningFacility_LightW_Wreck.mat` | `a138279c0f348e444b0f257a2da4454a` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/AsteroidMiningFacility/Window_Wreck.mat` | `Art/Materials/Wrecks/AsteroidMiningFacility/AsteroidMiningFacility_Window_Wreck.mat` | `f8c37e4d16109164aa86beb35a8b2618` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/DefendPlatform/defaultMat_ShipLit_Wreck.mat` | `Art/Materials/Wrecks/DefendPlatform/XQ6Platform_Surface_ShipLit_Wreck.mat` | `96e6ef3ee31192544bbb9b0379282330` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Black_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_Black_Wreck.mat` | `1d1bd04959a59bd4dbec694c5ac94bd6` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Dull_Metal_Greeble_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_DullMetalGreeble_Wreck.mat` | `75160d33c1719354f968c74572f24b68` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Dull_Metal_ShipLit_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_DullMetal_ShipLit_Wreck.mat` | `5a3e392e09d728e448f8ccfa05c52f75` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Engine_Glow_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_EngineGlow_Wreck.mat` | `862e6a6d1a3c6834db1c2e9b6e49a399` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Grey Hull.001_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_GreyHull01_Wreck.mat` | `c53a476a2d8435d48b4ad0ca26b0a7cb` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Hangar_Light_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_HangarLight_Wreck.mat` | `f2d250c3610b2d44c9374c0a755bdb5c` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_Bow_Republic_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_Bow_Republic_Wreck.mat` | `483e877193fdc474bae7ee2ac80fc4f5` | — | 1 | KEEP |
| `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_Hull_Republic_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_Hull_Republic_Wreck.mat` | `13bac934f8a9cfb44b89073afe33972f` | — | 1 | KEEP |
| `Art/Materials/Wrecks/HeavyDreadnought/ISD Plating_Radar_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_ISDPlatingRadar_Wreck.mat` | `c9ea4fabc1114f641831e05aaa76b9e4` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/ISD Plating_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_ISDPlating_Wreck.mat` | `4c12b1df0560ff54e94aceaf4be1ceaa` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/UnitDefaultLit_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/UnitDefaultLit_Wreck.mat` | `874648cb3545c1d46955499f31e219d7` | — | 1 | KEEP |
| `Art/Materials/Wrecks/HeavyDreadnought/Window_Light_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_WindowLight_Wreck.mat` | `911cf50171499894aa114d0f4b8f7fcb` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/HeavyDreadnought/Windows_Wreck.mat` | `Art/Materials/Wrecks/HeavyDreadnought/HeavyDreadnought_Windows_Wreck.mat` | `68a0fd08a059d354b859312ef74dabce` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Shape_019_Wreck.mat` | `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Slot019_Wreck.mat` | `cb264c2a44c5236418c7ebfb082aad41` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Shape_020_Wreck.mat` | `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Slot020_Wreck.mat` | `1aabf87e14a6b2b4988e4d2f21b4efba` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Shape_021_Wreck.mat` | `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Slot021_Wreck.mat` | `ad0cc3e7235ed894ca5c1493b5e4c00b` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Shape_022_Wreck.mat` | `Art/Materials/Wrecks/Lucrehulk/Lucrehulk_Slot022_Wreck.mat` | `c2f28f326b6b7e140878e8a2c50c06f8` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/MiningFacility/MiningFacilityHull_Wreck.mat` | `Art/Materials/Wrecks/MiningFacility/RefuelingStation_Hull_Wreck.mat` | `fc934013df226354a9de2aeefc12374b` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/ArmorShape_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_Armor_Wreck.mat` | `3ee2c34643f110d49b2521b0b170341c` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/BL_mat_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_BL_Wreck.mat` | `7d6e3cf246ec89c4b9c1aa25601de6e9` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/EngineShape_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_Engines_Wreck.mat` | `6d6fed61b34161c41a1a9759988eea79` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/SecAr_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_SecondaryArmor_Wreck.mat` | `f64277cd1190d3d4d80c14bf60474f74` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/Shields_Mat_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_Shields_Wreck.mat` | `e72159b2dd49ad94489b1bfde7c9e8cf` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/SuperstructureShape_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_Superstructure_Wreck.mat` | `1752198f65f0ca44286b13d2f168b94f` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Munificent/WeaponsShape_Wreck.mat` | `Art/Materials/Wrecks/Munificent/Munificent_Weapons_Wreck.mat` | `649fb95bbebe41b41ac5230fab0cd578` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Providence/PDXmat_jorodoxShape_Wreck.mat` | `Art/Materials/Wrecks/Providence/Providence_Hull_Wreck.mat` | `bc6108c95b5cac94390350ce4e6fd18b` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Recusant/defaultMat_Wreck.mat` | `Art/Materials/Wrecks/Recusant/Recusant_Hull_Wreck.mat` | `18ca576d00cef0f4a85ee8228c3ee5d5` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/RepublicSpaceStation/Material #2142150880_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/Freeport_Floor_Wreck.mat` | `63661b77971af0746b1f2e22e0e178ab` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/RepublicSpaceStation/ORDNUNGSKRAFT Graviton_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/Freeport_Graviton_Wreck.mat` | `35d6e8f7285e2bf4d81ef11189fe2e30` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/RepublicSpaceStation/SG-Gold-glow_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/Freeport_GlowGold_Wreck.mat` | `ef7adbbc517a04648ae51fd778c3a29e` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/RepublicSpaceStation/SG-LightW_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/Freeport_LightW_Wreck.mat` | `c2405a63b6f5fa64fa88a2a6cd85afe5` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/RepublicSpaceStation/SG-Window_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/Freeport_WindowSG_Wreck.mat` | `b0a20b222c969e64195ad3596a21515f` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/RepublicSpaceStation/UnitDefaultLit_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/UnitDefaultLit_Wreck.mat` | `d2beeabe99ca70443bbecabb63cc8e03` | — | 1 | KEEP |
| `Art/Materials/Wrecks/RepublicSpaceStation/Window_Wreck.mat` | `Art/Materials/Wrecks/RepublicSpaceStation/Freeport_Window_Wreck.mat` | `c0987ed3ad8cfc94dac413513c9e92dd` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/SeparatistSpaceStation/set1_Wreck.mat` | `Art/Materials/Wrecks/SeparatistSpaceStation/GangutStation_Set01_Wreck.mat` | `3ef4f561510dedb428a3e59567d3dc58` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/SeparatistSpaceStation/set2_Wreck.mat` | `Art/Materials/Wrecks/SeparatistSpaceStation/GangutStation_Set02_Wreck.mat` | `0003f570f69fba74e84f77147dff347c` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/SeparatistSpaceStation/set3_Wreck.mat` | `Art/Materials/Wrecks/SeparatistSpaceStation/GangutStation_Set03_Wreck.mat` | `f170da33d85c9bc4abea761ff7568387` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Bottom_Hull_Detail_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_BottomHullDetail_Wreck.mat` | `6bb934d0aada9db4f86b1f69e4996e4f` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/mat_destruct_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_Destructibles_Wreck.mat` | `533e50d5b66e08b468abdb4251addaca` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Engine_Block_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_EngineBlock_Wreck.mat` | `6a6736a218638d64590e4dc59b94b456` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Head_Neck_Final_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_HeadNeck_Wreck.mat` | `bbca2eb45d68606408c8eaa3e4f0b71b` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Lower_Deck_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_LowerDeck_Wreck.mat` | `9a6e8175e4e85624ca7ca3cae197bb46` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Lower_Hull_Bottom_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_LowerHullBottom_Wreck.mat` | `f5bda1427c2aba5499441e2c9907e55f` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Top_Deck_Final_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_TopDeck_Wreck.mat` | `5589d34aed31ea84cb4f47b596ffa4e2` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Top_Hull_Detail_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_TopHullDetail_Wreck.mat` | `0c39e9ae2a208d2469eab8504fffafc8` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Top_Hull_Final_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_TopHull_Wreck.mat` | `9057ba146bd67654282c54793787188c` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer1/Mat_Trench_Side_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer1/StarDestroyer1_TrenchSide_Wreck.mat` | `5af891cfc2fb075408ab758b6797d5dd` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer2/ISD_Color_Baked_ShipLit_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer2/StarDestroyer2_ColorBaked_ShipLit_Wreck.mat` | `3b58a9e9f587f3a4d892c987e119ae00` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer2/ISD_Hull_Color_Baked_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer2/StarDestroyer2_HullColorBaked_Wreck.mat` | `fca66fe1672ef66499731cf997d96f2c` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/StarDestroyer2/UnitDefaultLit_Wreck.mat` | `Art/Materials/Wrecks/StarDestroyer2/UnitDefaultLit_Wreck.mat` | `477cbb26653d59f45a9507e396197b99` | — | 1 | KEEP |
| `Art/Materials/Wrecks/Thranta/veh_rep_destroyer_detail_d_Wreck.mat` | `Art/Materials/Wrecks/Thranta/Thranta_Detail_Wreck.mat` | `337994bacf4319544abfac8a2b49c19e` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Thranta/veh_rep_destroyer_engine_d_Wreck.mat` | `Art/Materials/Wrecks/Thranta/Thranta_Engine_Wreck.mat` | `15822d3201648e8419424ee0cbfbcaf4` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Thranta/veh_rep_destroyer_metal01_d_Wreck.mat` | `Art/Materials/Wrecks/Thranta/Thranta_Metal01_Wreck.mat` | `b82eb6dbc2d836b4d851331f6f2ffdff` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Thranta/veh_rep_destroyer_metal02_d_Wreck.mat` | `Art/Materials/Wrecks/Thranta/Thranta_Metal02_Wreck.mat` | `6982e099d66ca1a4da39de2b5eb9e918` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Thranta/veh_rep_destroyer_trim_d_Wreck.mat` | `Art/Materials/Wrecks/Thranta/Thranta_Trim_Wreck.mat` | `e1b09b177b60ead41a25d2724145b557` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Venator/BLUETHRUSTER_CL_Wreck.mat` | `Art/Materials/Wrecks/Venator/Venator_ThrusterBlue_Wreck.mat` | `bf757c5a8c873da42982637c6a3958eb` | — | 1 | MOVE/RENAME |
| `Art/Materials/Wrecks/Venator/Capital_Repvenator_CL_Wreck.mat` | `Art/Materials/Wrecks/Venator/Venator_Hull_Wreck.mat` | `2039a180785631840ad3130ac28e1c99` | — | 1 | MOVE/RENAME |

- Source → wreck naming pairs (source paths relative to Assets):
  - `Art/Models/RepublicModels/Acclamator/object_0.mat` → `Acclamator_Slot00_Wreck.mat`.
  - `Art/Models/RepublicModels/Acclamator/object_1.mat` → `Acclamator_Slot01_Wreck.mat`.
  - `Art/Models/Cannons/heavy-turbolaser-cannon-v1/lambert1_ShipLit.mat` → `HeavyTurbolaserCannon_Surface_ShipLit_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/Material #2142150880.mat` → `AsteroidMiningFacility_Hull_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/ORDNUNGSKRAFT Graviton.mat` → `AsteroidMiningFacility_Graviton_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/SG-Gold-glow.mat` → `AsteroidMiningFacility_GlowGold_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/SG-LightW.mat` → `AsteroidMiningFacility_LightW_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/Window.mat` → `AsteroidMiningFacility_Window_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/Material #2142150880.mat` → `AsteroidMiningFacility_Hull_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/ORDNUNGSKRAFT Graviton.mat` → `AsteroidMiningFacility_Graviton_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/SG-Gold-glow.mat` → `AsteroidMiningFacility_GlowGold_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/SG-LightW.mat` → `AsteroidMiningFacility_LightW_Wreck.mat`.
  - `Art/Models/Station/space-station-asteroid-mining-facility/Window.mat` → `AsteroidMiningFacility_Window_Wreck.mat`.
  - `Art/Models/DefendPlatforms/XQ6/defaultMat_ShipLit.mat` → `XQ6Platform_Surface_ShipLit_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Black.mat` → `HeavyDreadnought_Black_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Dull_Metal_Greeble.mat` → `HeavyDreadnought_DullMetalGreeble_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Dull_Metal_ShipLit.mat` → `HeavyDreadnought_DullMetal_ShipLit_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Engine_Glow.mat` → `HeavyDreadnought_EngineGlow_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Grey Hull.001.mat` → `HeavyDreadnought_GreyHull01_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Hangar_Light.mat` → `HeavyDreadnought_HangarLight_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/HeavyDreadnought_Bow_Republic.mat` → `HeavyDreadnought_Bow_Republic_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/HeavyDreadnought_Hull_Republic.mat` → `HeavyDreadnought_Hull_Republic_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/ISD Plating_Radar.mat` → `HeavyDreadnought_ISDPlatingRadar_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/ISD Plating.mat` → `HeavyDreadnought_ISDPlating_Wreck.mat`.
  - `Art/Materials/UnitDefaultLit.mat` → `UnitDefaultLit_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Window_Light.mat` → `HeavyDreadnought_WindowLight_Wreck.mat`.
  - `Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/source/Windows.mat` → `HeavyDreadnought_Windows_Wreck.mat`.
  - `Art/Materials/Lucrehulk_Shape_019.mat` → `Lucrehulk_Slot019_Wreck.mat`.
  - `Art/Materials/Lucrehulk_Shape_020.mat` → `Lucrehulk_Slot020_Wreck.mat`.
  - `Art/Materials/Lucrehulk_Shape_021.mat` → `Lucrehulk_Slot021_Wreck.mat`.
  - `Art/Materials/Lucrehulk_Shape_022.mat` → `Lucrehulk_Slot022_Wreck.mat`.
  - `Art/Materials/MiningFacilityHull.mat` → `RefuelingStation_Hull_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/ArmorShape.mat` → `Munificent_Armor_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/BL_mat.mat` → `Munificent_BL_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/EngineShape.mat` → `Munificent_Engines_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/SecAr.mat` → `Munificent_SecondaryArmor_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/Shields_Mat.mat` → `Munificent_Shields_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/SuperstructureShape.mat` → `Munificent_Superstructure_Wreck.mat`.
  - `Art/Models/SeparatistShip/Munificent/WeaponsShape.mat` → `Munificent_Weapons_Wreck.mat`.
  - `Art/Models/SeparatistShip/Providence/PDXmat_jorodoxShape.mat` → `Providence_Hull_Wreck.mat`.
  - `Art/Models/SeparatistShip/Recusant/defaultMat.mat` → `Recusant_Hull_Wreck.mat`.
  - `Art/Models/Station/FreePort/Material #2142150880.mat` → `Freeport_Floor_Wreck.mat`.
  - `Art/Models/Station/FreePort/ORDNUNGSKRAFT Graviton.mat` → `Freeport_Graviton_Wreck.mat`.
  - `Art/Models/Station/FreePort/SG-Gold-glow.mat` → `Freeport_GlowGold_Wreck.mat`.
  - `Art/Models/Station/FreePort/SG-LightW.mat` → `Freeport_LightW_Wreck.mat`.
  - `Art/Models/Station/FreePort/SG-Window.mat` → `Freeport_WindowSG_Wreck.mat`.
  - `Art/Materials/UnitDefaultLit.mat` → `UnitDefaultLit_Wreck.mat`.
  - `Art/Models/Station/FreePort/Window.mat` → `Freeport_Window_Wreck.mat`.
  - `Art/Models/Station/gangut-space-hub/source/set1.mat` → `GangutStation_Set01_Wreck.mat`.
  - `Art/Models/Station/gangut-space-hub/source/set2.mat` → `GangutStation_Set02_Wreck.mat`.
  - `Art/Models/Station/gangut-space-hub/source/set3.mat` → `GangutStation_Set03_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Bottom_Hull_Detail.mat` → `StarDestroyer1_BottomHullDetail_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/mat_destruct.mat` → `StarDestroyer1_Destructibles_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Engine_Block.mat` → `StarDestroyer1_EngineBlock_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Head_Neck_Final.mat` → `StarDestroyer1_HeadNeck_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Lower_Deck.mat` → `StarDestroyer1_LowerDeck_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Lower_Hull_Bottom.mat` → `StarDestroyer1_LowerHullBottom_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Top_Deck_Final.mat` → `StarDestroyer1_TopDeck_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Top_Hull_Detail.mat` → `StarDestroyer1_TopHullDetail_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Top_Hull_Final.mat` → `StarDestroyer1_TopHull_Wreck.mat`.
  - `Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/Materials/Mat_Trench_Side.mat` → `StarDestroyer1_TrenchSide_Wreck.mat`.
  - `Art/Models/RepublicModels/star-destroyer/source/Materials/ISD_Color_Baked_ShipLit.mat` → `StarDestroyer2_ColorBaked_ShipLit_Wreck.mat`.
  - `Art/Models/RepublicModels/star-destroyer/source/Materials/ISD_Hull_Color_Baked.mat` → `StarDestroyer2_HullColorBaked_Wreck.mat`.
  - `Art/Materials/UnitDefaultLit.mat` → `UnitDefaultLit_Wreck.mat`.
  - `Art/Models/RepublicModels/Thranta/Materials/veh_rep_destroyer_detail_d.mat` → `Thranta_Detail_Wreck.mat`.
  - `Art/Models/RepublicModels/Thranta/Materials/veh_rep_destroyer_engine_d.mat` → `Thranta_Engine_Wreck.mat`.
  - `Art/Models/RepublicModels/Thranta/Materials/veh_rep_destroyer_metal01_d.mat` → `Thranta_Metal01_Wreck.mat`.
  - `Art/Models/RepublicModels/Thranta/Materials/veh_rep_destroyer_metal02_d.mat` → `Thranta_Metal02_Wreck.mat`.
  - `Art/Models/RepublicModels/Thranta/Materials/veh_rep_destroyer_trim_d.mat` → `Thranta_Trim_Wreck.mat`.
  - `Art/Models/RepublicModels/Venator/Materials/BLUETHRUSTER_CL.mat` → `Venator_ThrusterBlue_Wreck.mat`.
  - `Art/Models/RepublicModels/Venator/Materials/Capital_Repvenator_CL.mat` → `Venator_Hull_Wreck.mat`.

### Retained Assets/Art/Models — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Models/ShieldSurface.asset` | `Art/Models/ShieldSurface.asset` | `9f300a37e7a6efa4fb1a8104a7ab310a` | — | 14 | KEEP |

### Shader textures — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Shaders/noise_detail_2025_05_24_16_57_32.png` | `Art/Textures/Vfx/Nebula/Nebula_NoiseDetail.png` | `306b3d184cb0f4041b63fb84874ff728` | 256×256 | 1 | MOVE/RENAME |
| `Art/Shaders/noise_main_2025_05_24_16_57_36.png` | `Art/Textures/Vfx/Nebula/Nebula_NoiseMain.png` | `4e5ef547f292763479afd3cd8a228f8d` | 256×256 | 1 | MOVE/RENAME |

### Retained Assets/Art/Shaders — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Shaders/PlanetAutodeskInteractiveMaskedNoShadows.shadergraph` | `Art/Shaders/PlanetAutodeskInteractiveMaskedNoShadows.shadergraph` | `673d90c486faf4b80a9ee62ec3eab53a` | — | 0 | KEEP |
| `Art/Shaders/PlanetAutodeskInteractiveNoShadows.shadergraph` | `Art/Shaders/PlanetAutodeskInteractiveNoShadows.shadergraph` | `1826ad25785424a068bc22500aec6928` | — | 1 | KEEP |
| `Art/Shaders/Units/ShipLit.shader` | `Art/Shaders/Units/ShipLit.shader` | `f2a9a9f1727de0c4a912563406aea950` | — | 102 | KEEP |
| `Art/Shaders/Units/ShipLitDepthNormalsPass.hlsl` | `Art/Shaders/Units/ShipLitDepthNormalsPass.hlsl` | `4a6223f7df60f9a4b9487b80975cf4ec` | — | 0 | KEEP |
| `Art/Shaders/Units/ShipLitForwardPass.hlsl` | `Art/Shaders/Units/ShipLitForwardPass.hlsl` | `a509f3be24db4b44a89c459d814aeeb9` | — | 0 | KEEP |
| `Art/Shaders/Units/ShipLitInput.hlsl` | `Art/Shaders/Units/ShipLitInput.hlsl` | `6d91e2d47672e8d409f6f9c1ba42d628` | — | 0 | KEEP |
| `Art/Shaders/Units/ShipWreck.shader` | `Art/Shaders/Units/ShipWreck.shader` | `423a9599c199c944e999717f28ca7991` | — | 71 | KEEP |
| `Art/Shaders/Units/ShipWreckDeformation.hlsl` | `Art/Shaders/Units/ShipWreckDeformation.hlsl` | `43fb00062747dac43a5dd344b94cf0b1` | — | 0 | KEEP |
| `Art/Shaders/Units/ShipWreckDepthPasses.hlsl` | `Art/Shaders/Units/ShipWreckDepthPasses.hlsl` | `1255dab8ffb844949a22ed0cb8db93d0` | — | 0 | KEEP |
| `Art/Shaders/Units/ShipWreckForwardPass.hlsl` | `Art/Shaders/Units/ShipWreckForwardPass.hlsl` | `ef3900fbf38a90a43a0a35987cd12d91` | — | 0 | KEEP |
| `Art/Shaders/Vfx/FogOfWar.shader` | `Art/Shaders/Vfx/FogOfWar.shader` | `8939b9e038821914a8b1dde24fd32000` | — | 2 | KEEP |
| `Art/Shaders/Vfx/IonField.shader` | `Art/Shaders/Vfx/IonField.shader` | `dd015a7846d689b4a83e9d1d55e118a4` | — | 1 | KEEP |
| `Art/Shaders/Vfx/IonGlow.shader` | `Art/Shaders/Vfx/IonGlow.shader` | `917046565b1515f4e803a71d5fb8d58d` | — | 4 | KEEP |
| `Art/Shaders/Vfx/NebulaBillowVolume.shader` | `Art/Shaders/Vfx/NebulaBillowVolume.shader` | `d6468db222ce34d7f954b17f79905345` | — | 1 | KEEP |
| `Art/Shaders/Vfx/NebulaCloudVolume.shader` | `Art/Shaders/Vfx/NebulaCloudVolume.shader` | `ee1d3f4d8a3aa405ca907e97cf4f939b` | — | 0 | KEEP |
| `Art/Shaders/Vfx/ReinforcementZone.shader` | `Art/Shaders/Vfx/ReinforcementZone.shader` | `27de027496a55034bb42cba3e91d2adb` | — | 1 | KEEP |
| `Art/Shaders/Vfx/ShipShield.shader` | `Art/Shaders/Vfx/ShipShield.shader` | `d8fb92f8b585586489876523502cea8f` | — | 1 | KEEP |
| `Art/Shaders/Vfx/VolumetricNebulaRaymarch.shader` | `Art/Shaders/Vfx/VolumetricNebulaRaymarch.shader` | `4fd79ec52f6ae1a4e8777bf6b859cc5d` | — | 1 | KEEP |
| `Art/Shaders/VolumetricNebula.shader` | `Art/Shaders/VolumetricNebula.shader` | `9da78518a96f87941bf91282c09e0384` | — | 1 | KEEP |
| `Art/Shaders/VolumetricShader.shadergraph` | `Art/Shaders/VolumetricShader.shadergraph` | `fb569194d670eec4a9dfde2e429bc472` | — | 0 | KEEP |

### Loose textures — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Textures/4834461_2531449-ai.png` | `Art/Textures/Unclassified/StockVector4834461_Source.png` | `ba6abc6b95d69d34c8d124296c11e938` | 1000×1000 | 0 | MOVE/RENAME |
| `Art/Textures/BlasterBolt.png` | `Art/Textures/Vfx/BlasterBolt.png` | `95f35aa44de2c334a8a9d2abd88322aa` | 1000×50 | 3 | MOVE/RENAME |
| `Art/Textures/BlasterBoltTilled.png` | `Art/Textures/Vfx/BlasterBolt_Tiled.png` | `fccd61f90fd0c054fa09fa6d7014c980` | 1000×50 | 1 | MOVE/RENAME |
| `Art/Textures/CaptionRex.jpg` | `Art/Textures/Ui/Loading/CaptainRex_Loading.jpg` | `a0f948fcc5427364da06ae6db0dd8d43` | 1672×941 | 1 | MOVE/RENAME |
| `Art/Textures/grid (2).png` | `Art/Textures/Unclassified/Grid02_Source.png` | `bdf0d996426e14d4a8c75d0a56b9f97f` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/grid (5).png` | `Art/Textures/Vfx/Nebula/NebulaBorderInner_Grid.png` | `99480efd92dbb034da01119c4da3982e` | 512×512 | 1 | MOVE/RENAME |
| `Art/Textures/grid (6).png` | `Art/Textures/Vfx/Nebula/NebulaBorder_Grid.png` | `b5e6de1e0e9d1e74fa2855451f426e21` | 512×512 | 2 | MOVE/RENAME |
| `Art/Textures/grid (7).png` | `Art/Textures/Unclassified/Grid07_Source.png` | `6f30416ee295bb04fb205b133e6cd60d` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/grid.png` | `Art/Textures/Unclassified/Grid_Source.png` | `6b949ed7ca00a7f4ba6159b588102d5c` | 512×512 | 0 | MOVE/RENAME |
| `Art/Textures/HexagonTile_HGT.png` | `Art/Textures/Vfx/HexagonTile_HGT.png` | `409f0194e63b6f94da04a76e4e4976a3` | 2048×2048 | 1 | MOVE/RENAME |
| `Art/Textures/laser-beam-effect-photoshop-free-overlay-texture.jpg` | `Art/Textures/Vfx/ProtonBeam_Albedo_Source.jpg` | `6c0e09c4d427ac0438964f2cdd59a21d` | 2107×974 | 1 | MOVE/RENAME |
| `Art/Textures/maxresdefault.jpg` | `Art/Textures/Unclassified/Thumbnail_Source.jpg` | `b5efdcb5b5b48bd4f949cc0500b5640b` | 1280×720 | 0 | MOVE/RENAME |

### Retained Assets/Art/Textures — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Textures/Particles/40.png` | `Art/Textures/Particles/40.png` | `db2437e172cbdc34b8802005f1161ea0` | 1533×796 | 1 | KEEP |
| `Art/Textures/Particles/55.png` | `Art/Textures/Particles/55.png` | `5a91ad74ab4bd7840b008ab97dd7d80b` | 2018×1235 | 0 | KEEP |
| `Art/Textures/Particles/57.png` | `Art/Textures/Particles/57.png` | `c1f6a2daec3f76a49b6658f1e285cb4f` | 1731×956 | 0 | KEEP |
| `Art/Textures/Particles/81.png` | `Art/Textures/Particles/81.png` | `f97abe16280107548a9e882075c9b0ee` | 4052×2163 | 0 | KEEP |
| `Art/Textures/Particles/99.png` | `Art/Textures/Particles/99.png` | `537c8b0281218374ab8297693cb2629e` | 3071×1506 | 0 | KEEP |
| `Art/Textures/Particles/nebula.png` | `Art/Textures/Particles/nebula.png` | `b7f63e249d4deec459d9ecce75c958b4` | 1024×1024 | 1 | KEEP |
| `Art/Textures/Particles/nebula_v2.png` | `Art/Textures/Particles/nebula_v2.png` | `83c0ff63dba3f6e49b7734d98d125452` | 1024×1024 | 1 | KEEP |
| `Art/Textures/Particles/NebulaBillowField.asset` | `Art/Textures/Particles/NebulaBillowField.asset` | `27440aebbb95e4c30aca71bac63a1f76` | — | 1 | KEEP |
| `Art/Textures/Particles/NebulaCloudDensity.asset` | `Art/Textures/Particles/NebulaCloudDensity.asset` | `cf62adca173b3454f9db026d4962fcd8` | — | 0 | KEEP |
| `Art/Textures/Particles/NebulaVolumeNoise3D.asset` | `Art/Textures/Particles/NebulaVolumeNoise3D.asset` | `c8a4cea7d16be0942b1588b86d8302c6` | — | 1 | KEEP |
| `Art/Textures/Ui/HardPoints/Engines.png` | `Art/Textures/Ui/HardPoints/Engines.png` | `bf58ccdf2a3217f41a9ea9d694f66bc9` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/HardPoints/Hangar.png` | `Art/Textures/Ui/HardPoints/Hangar.png` | `5ad1f04dcc37ae540aee98855aadce92` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/HardPoints/HullSection.png` | `Art/Textures/Ui/HardPoints/HullSection.png` | `e8a5df89602a9cd458a9a619ba30ab56` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/HardPoints/ShieldGenerator.png` | `Art/Textures/Ui/HardPoints/ShieldGenerator.png` | `33e58938768151045937fd04102c2bd5` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/HardPoints/WeaponBattery.png` | `Art/Textures/Ui/HardPoints/WeaponBattery.png` | `e85f0391617ebec40a9db56824234617` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/MiniMap/AsteroidWhite.png` | `Art/Textures/Ui/Icons/MiniMap/AsteroidWhite.png` | `cc3f903160b7cb34d965600b57915031` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/MiniMap/DefendPlatformWhite.png` | `Art/Textures/Ui/Icons/MiniMap/DefendPlatformWhite.png` | `104554077a380465180036e258f26014` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/MiniMap/MiningFacilityWhite.png` | `Art/Textures/Ui/Icons/MiniMap/MiningFacilityWhite.png` | `e1a3b7efd1e34449599253d8289fb2cd` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/MiniMap/ReinforcementZoneWhite.png` | `Art/Textures/Ui/Icons/MiniMap/ReinforcementZoneWhite.png` | `fc0dadd1192f642d3ac0a3ac7da36e06` | 1254×1254 | 0 | KEEP |
| `Art/Textures/Ui/Icons/MiniMap/ShipWhite.png` | `Art/Textures/Ui/Icons/MiniMap/ShipWhite.png` | `3c0da0980bf83446a860e70b33e0693e` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/MiniMap/SpaceStationWhite.png` | `Art/Textures/Ui/Icons/MiniMap/SpaceStationWhite.png` | `3d8c2c187dbb7c149b2b91ea81cc45fd` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Mining/MiningIcon.png` | `Art/Textures/Ui/Icons/Mining/MiningIcon.png` | `c54029d425acba74c9c5d702e49710b6` | 512×512 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/EnhancedEnginesIIcon.png` | `Art/Textures/Ui/Icons/Research/EnhancedEnginesIIcon.png` | `129f6cb74bcbfd146abb8dcd483d2864` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/EnhancedEnginesIIIcon.png` | `Art/Textures/Ui/Icons/Research/EnhancedEnginesIIIcon.png` | `f40f252f163e62c49ab2093396b1a551` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/EnhancedEnginesIIIIcon.png` | `Art/Textures/Ui/Icons/Research/EnhancedEnginesIIIIcon.png` | `e22f2c4c60208dd4292fcdc9811151ef` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/EnhancedShieldsIIcon.png` | `Art/Textures/Ui/Icons/Research/EnhancedShieldsIIcon.png` | `46e861879378cc441bb7260e899b7559` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/EnhancedShieldsIIIcon.png` | `Art/Textures/Ui/Icons/Research/EnhancedShieldsIIIcon.png` | `8dc6d5756d4d85e4a82d1107e1b2de33` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/EnhancedShieldsIIIIcon.png` | `Art/Textures/Ui/Icons/Research/EnhancedShieldsIIIIcon.png` | `151b2593d21bbfb4a83f12dc07572dc0` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ImprovedDefensesIIcon.png` | `Art/Textures/Ui/Icons/Research/ImprovedDefensesIIcon.png` | `fd2f2005d81ff1545af20e1feec39c0e` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ImprovedDefensesIIIcon.png` | `Art/Textures/Ui/Icons/Research/ImprovedDefensesIIIcon.png` | `fba4e190fee1ba649bfd1055db270112` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ImprovedDefensesIIIIcon.png` | `Art/Textures/Ui/Icons/Research/ImprovedDefensesIIIIcon.png` | `78c07bc3183d5cd48a7e826898d024e0` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ImprovedWeaponsIIcon.png` | `Art/Textures/Ui/Icons/Research/ImprovedWeaponsIIcon.png` | `84c93b9211168f64bb4f04d91c77bf0a` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ImprovedWeaponsIIIcon.png` | `Art/Textures/Ui/Icons/Research/ImprovedWeaponsIIIcon.png` | `de8c71c05a08b16478644e8902c29205` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ImprovedWeaponsIIIIcon.png` | `Art/Textures/Ui/Icons/Research/ImprovedWeaponsIIIIcon.png` | `1fe567856ec608444bd8fdb96f5b5f81` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/IncreasedProductionIIcon.png` | `Art/Textures/Ui/Icons/Research/IncreasedProductionIIcon.png` | `1ef035bcae2fad145ab8ce8fa5776004` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/IncreasedProductionIIIcon.png` | `Art/Textures/Ui/Icons/Research/IncreasedProductionIIIcon.png` | `3439a93046dd31a448344298833364aa` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Research/ReinforcedStructuresIcon.png` | `Art/Textures/Ui/Icons/Research/ReinforcedStructuresIcon.png` | `b73f9718576eb694bb3badc9b47afc43` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/AssaultIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/AssaultIcon.png` | `3cbfaeb7158d3ec4089863ac834197b4` | 256×256 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/BoostEnginePowerIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/BoostEnginePowerIcon.png` | `4c317a4042ef0264383cfd55904a5e0a` | 256×256 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/BoostShieldPowerIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/BoostShieldPowerIcon.png` | `c75079b4b4d09794b8a289fefb0d3b53` | 256×256 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/BoostWeaponPowerIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/BoostWeaponPowerIcon.png` | `867b122ef4b6b9342ad0f3f66a60f584` | 256×256 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/ConcentrateFireIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/ConcentrateFireIcon.png` | `a4e2bc47fe148694ca2314ad9ae2f3d6` | 256×256 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/InvulnerabilityIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/InvulnerabilityIcon.png` | `5491174c7918a494cb8e2898869953fd` | 256×256 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipAbilities/ProtonBeamIcon.png` | `Art/Textures/Ui/Icons/ShipAbilities/ProtonBeamIcon.png` | `704f7450173727e43becc6f5e49c86ed` | 256×256 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/AcclamatorIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/AcclamatorIcon.png` | `2ce1aa3deeeb0a541bb85a35a38c9390` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png` | `ae699c8c0850c9245b84a34c5fb8d767` | 512×512 | 3 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/BlueprintVenatorTest.png` | `Art/Textures/Ui/Icons/ShipIcon/BlueprintVenatorTest.png` | `a395303da530dd243a91cb24811dd697` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/HeavyDreadnoughtIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/HeavyDreadnoughtIcon.png` | `003f227a5109a5745879774b3be70886` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/LucrehulkIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/LucrehulkIcon.png` | `8128fcc8b8e454e47ab592309d37208e` | 512×512 | 3 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/MunificentIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/MunificentIcon.png` | `abf3987a701188747b639680e9203c3e` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_1233668.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_1233668.png` | `6c7ebf51c071e73408a8a593f571b0de` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_1233777.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_1233777.png` | `3483e2ea8c3953b49be48aab2fc422ad` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_1348282.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_1348282.png` | `0edc1baa30ab2a84c8ef002bea1375a9` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_1348347.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_1348347.png` | `16fe756bf7147cb4bba0f9f818d06a81` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_2267232.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_2267232.png` | `7306266a3cfddc840b55088c6ada0e61` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_2572164.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_2572164.png` | `e073c0b9e9255614285e8c33c4fd909f` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/PngItem_2725253.png` | `Art/Textures/Ui/Icons/ShipIcon/PngItem_2725253.png` | `e3710cc7a6d1680499eefbcb9e347fb2` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/ProvidenceIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/ProvidenceIcon.png` | `60d91c69afc54ad4fa9b45f6990ff8af` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/RecusantIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/RecusantIcon.png` | `dcdfa3aa3e1fabb44b5e337f8aa8370f` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/StarDestroyer1Icon.png` | `Art/Textures/Ui/Icons/ShipIcon/StarDestroyer1Icon.png` | `10c0dc8aea43af944a4261f5e9c16921` | 512×512 | 1 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/StarDestroyer2Icon.png` | `Art/Textures/Ui/Icons/ShipIcon/StarDestroyer2Icon.png` | `71f48afd28fd17340a87652ce16e4712` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/ThrantaIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/ThrantaIcon.png` | `28476024afa7c5f47bd010e6e4460dc4` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/AcclamatorShipUi.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/AcclamatorShipUi.png` | `e2e8d89cbc31dc54780b38424325ec80` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/AcclamatorShipUiUnits.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/AcclamatorShipUiUnits.png` | `eff2a3777f3753d428beaf299fbf4d05` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArachneIconUpsideDown.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArachneIconUpsideDown.png` | `7327db67e2e8c2140a4b3e79dd5c8c4f` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArqShipIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArqShipIcon.png` | `0e4076218887c7445aca7668b8a1d466` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArqShipIconUnits.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArqShipIconUnits.png` | `edfdc96411a9a6d4da5478e75882eb1d` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/lucrehulkUpsideDownIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/lucrehulkUpsideDownIcon.png` | `cd6f8890674477645a018690eb8ee944` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/munificentUpsideDownIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/munificentUpsideDownIcon.png` | `003a7e95e0bfff145a3d4312c8388c6c` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ProvidenceShipUi.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ProvidenceShipUi.png` | `3225a1ec9f9fef54496ad508fb054b2c` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/RecusantIconUpsideDown.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/RecusantIconUpsideDown.png` | `5a73d6b52a19af14db973c62b8734c25` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/StarDestroyerIconUpsideDown.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/StarDestroyerIconUpsideDown.png` | `124dd34bf8e404f42ac341d9eb5fd7e5` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VenatorUpsidedownIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VenatorUpsidedownIcon.png` | `6f17df29a9779b14abfb3843cc051902` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VenatorUpsidedownIconCannons.png` | `Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VenatorUpsidedownIconCannons.png` | `d6628f732f126864e999704de4d5317a` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/ShipIcon/VenatorIcon.png` | `Art/Textures/Ui/Icons/ShipIcon/VenatorIcon.png` | `d899a22bd6d129e46a5aed14df2b81fa` | 512×512 | 4 | KEEP |
| `Art/Textures/Ui/Icons/SquadronIcon/AWingIcon.png` | `Art/Textures/Ui/Icons/SquadronIcon/AWingIcon.png` | `524d65b06058f0a428ee3d2a9b9b78e9` | 1254×1254 | 2 | KEEP |
| `Art/Textures/Ui/Icons/SquadronIcon/AWingSilhouette.png` | `Art/Textures/Ui/Icons/SquadronIcon/AWingSilhouette.png` | `d748ad24c6085834f9f499d09e8ca252` | 1254×1254 | 1 | KEEP |
| `Art/Textures/Ui/Icons/SquadronIcon/Belbullab22Icon.png` | `Art/Textures/Ui/Icons/SquadronIcon/Belbullab22Icon.png` | `6475693b4e9a23943aa6f6d43d698437` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/SquadronIcon/Belbullab22Silhouette.png` | `Art/Textures/Ui/Icons/SquadronIcon/Belbullab22Silhouette.png` | `66df557cbf5c3c641aed4edee94814d6` | 512×512 | 1 | KEEP |
| `Art/Textures/Ui/Icons/SquadronIcon/Delta7Icon.png` | `Art/Textures/Ui/Icons/SquadronIcon/Delta7Icon.png` | `41429e00e6d997249adaf10dc6d292a6` | 512×512 | 2 | KEEP |
| `Art/Textures/Ui/Icons/SquadronIcon/Delta7Silhouette.png` | `Art/Textures/Ui/Icons/SquadronIcon/Delta7Silhouette.png` | `a79e6a43830d246449552f0ec5f0e9d8` | 512×512 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Ui/add.png` | `Art/Textures/Ui/Icons/Ui/add.png` | `e39b5981395ec2348a8181d1392e9964` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/Ui/camera.png` | `Art/Textures/Ui/Icons/Ui/camera.png` | `8a3e30c27c63ebc4cab9d36883547fb0` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/Ui/cancelIcon.png` | `Art/Textures/Ui/Icons/Ui/cancelIcon.png` | `d399716888a1eeb4295fae9176e2970a` | 64×64 | 0 | KEEP |
| `Art/Textures/Ui/Icons/Ui/DefensePlatformIcon.png` | `Art/Textures/Ui/Icons/Ui/DefensePlatformIcon.png` | `9e99f16d754a042a3b1b0f2d25045f52` | 512×512 | 1 | KEEP |
| `Art/Textures/Ui/Icons/Ui/hologram.png` | `Art/Textures/Ui/Icons/Ui/hologram.png` | `98147b81d4eeb6b45b006a156c178677` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/Ui/hologramMap.png` | `Art/Textures/Ui/Icons/Ui/hologramMap.png` | `ca3df0244780c02469fcd5f01b0b9dea` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Icons/Ui/pause.png` | `Art/Textures/Ui/Icons/Ui/pause.png` | `1a28ce64457baba4d984e0fc4c6c7101` | 512×512 | 0 | KEEP |
| `Art/Textures/Ui/Money/PngItem_311436.png` | `Art/Textures/Ui/Money/PngItem_311436.png` | `61108d23960a8c34caacfb024ec0f057` | 521×958 | 0 | KEEP |
| `Art/Textures/Ui/ShipUnit/target.png` | `Art/Textures/Ui/ShipUnit/target.png` | `d55d32978efc94740a41cd98f837795c` | 1254×1254 | 1 | KEEP |

### UI relocation — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Art/Ui/SuperWeapons/HypervelocityGunIcon.png` | `Art/Textures/Ui/SuperWeapons/HypervelocityGunIcon.png` | `c09ba886d721a5b48ae81ff6b7ddf9bb` | 256×256 | 2 | MOVE/RENAME |
| `Art/Ui/SuperWeapons/IonCannonIcon.png` | `Art/Textures/Ui/SuperWeapons/IonCannonIcon.png` | `2e60ab884151e074caf67bb570ba533b` | 256×256 | 2 | MOVE/RENAME |
| `Art/Ui/SuperWeapons/PlasmaCannonIcon.png` | `Art/Textures/Ui/SuperWeapons/PlasmaCannonIcon.png` | `3f8628327c2f7ab4d905d41954f545b1` | 256×256 | 2 | MOVE/RENAME |
| `Art/Ui/UnitActions/Attack.png` | `Art/Textures/Ui/UnitActions/Attack.png` | `52f242eac84af934294f2738415ebcdd` | 64×64 | 1 | MOVE/RENAME |
| `Art/Ui/UnitActions/AttackMove.png` | `Art/Textures/Ui/UnitActions/AttackMove.png` | `935bdf7fa1b5a2b49aa52a55dac664bf` | 64×64 | 1 | MOVE/RENAME |
| `Art/Ui/UnitActions/Guard.png` | `Art/Textures/Ui/UnitActions/Guard.png` | `6419c6c8a033ca44f8d6ee43dbfc6174` | 64×64 | 1 | MOVE/RENAME |
| `Art/Ui/UnitActions/Hunt.png` | `Art/Textures/Ui/UnitActions/Hunt.png` | `1fdf56b45c5e6dd4bb970693171029bc` | 64×64 | 1 | MOVE/RENAME |
| `Art/Ui/UnitActions/Move.png` | `Art/Textures/Ui/UnitActions/Move.png` | `71024beb37910e44b8fd263673ea6944` | 64×64 | 0 | MOVE/RENAME |
| `Art/Ui/UnitActions/Retreat.png` | `Art/Textures/Ui/UnitActions/Retreat.png` | `f2fdbcd564b6d4740928bb4897d07795` | 64×64 | 1 | MOVE/RENAME |
| `Art/Ui/UnitActions/Stop.png` | `Art/Textures/Ui/UnitActions/Stop.png` | `d942cd4e6e14de54aa5d96cefc719f6c` | 64×64 | 1 | MOVE/RENAME |
| `Art/Ui/UnitActions/WaypointMove.png` | `Art/Textures/Ui/UnitActions/WaypointMove.png` | `3234f92f86c3fd24886dbd89ec9b6ee7` | 64×64 | 1 | MOVE/RENAME |

### Render pipeline settings — file manifest

- Both paths below are relative to `Assets/`.

| Current path | Proposed target | GUID | Size px | Refs | Action |
| --- | --- | --- | --- | ---: | --- |
| `Resources/Settings/SampleSceneProfile.asset` | `Settings/Render Pipelines/SampleSceneProfile.asset` | `a6560a915ef98420e9faacc1c7438823` | — | 0 | MOVE/RENAME |
| `Resources/Settings/URP-Balanced-Renderer.asset` | `Settings/Render Pipelines/URP-Balanced-Renderer.asset` | `e634585d5c4544dd297acaee93dc2beb` | — | 1 | MOVE/RENAME |
| `Resources/Settings/URP-Balanced.asset` | `Settings/Render Pipelines/URP-Balanced.asset` | `e1260c1148f6143b28bae5ace5e9c5d1` | — | 1 | MOVE/RENAME |
| `Resources/Settings/URP-HighFidelity-Renderer.asset` | `Settings/Render Pipelines/URP-HighFidelity-Renderer.asset` | `c40be3174f62c4acf8c1216858c64956` | — | 3 | MOVE/RENAME |
| `Resources/Settings/URP-HighFidelity.asset` | `Settings/Render Pipelines/URP-HighFidelity.asset` | `7b7fd9122c28c4d15b667c7040e3b3fd` | — | 1 | MOVE/RENAME |
| `Resources/Settings/URP-Performant-Renderer.asset` | `Settings/Render Pipelines/URP-Performant-Renderer.asset` | `707360a9c581a4bd7aa53bfeb1429f71` | — | 1 | MOVE/RENAME |
| `Resources/Settings/URP-Performant.asset` | `Settings/Render Pipelines/URP-Performant.asset` | `d0e2fc18fe036412f8223b3b3d9ad574` | — | 2 | MOVE/RENAME |

### Additional prefab placement

| Current path | Target path | GUID | Action |
| --- | --- | --- | --- |
| `Assets/Prefabs/Installers/SceneContext.prefab` | `Assets/Prefabs/View/ZenjectContext/SceneContext.prefab` | `78c9a2efe6e06854c820c86f9d0360a7` | MOVE |

- This is **one additional move outside the 736-file Art/Resources inventory**: total planned file moves/renames including this prefab = **603**.
- Snapshot its nested prefab/scene consumers and verify its destination is free before execution; keep the Resources bootstrap prefab distinct.
- No lighting prefab filename changes. No AddressableAssetsData structure changes.

### Hardcoded path changes

- Eight literal path changes were found in four Editor scripts. Update them in the same batch as their referenced assets.
- Re-run exact-text searches after migration; reference-preserving Unity moves cannot repair C# path literals.
- Source lines are audit-time locations; use Serena symbols/current text when implementing.

| Source file and line | Old asset path | New asset path |
| --- | --- | --- |
| `Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs:29` | `Assets/Art/Materials/Engines.mat` | `Assets/Art/Materials/Vfx/Engines.mat` |
| `Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs:46` | `Assets/Art/Models/RepublicModels/Delta7/Delta7.obj` | `Assets/Art/Models/RepublicShips/Delta7/Delta7.obj` |
| `Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs:51` | `Assets/Art/Models/SeparatistShip/belbullab/B22_whole.obj` | `Assets/Art/Models/SeparatistShips/Belbullab22/Belbullab22.obj` |
| `Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs:63` | `Assets/Art/Models/RepublicModels/A-wing/A-Wing.dae` | `Assets/Art/Models/RepublicShips/AWing/AWing.dae` |
| `Assets/Scripts/Editor/CaptureSites/CaptureSiteEditorTool.cs:27` | `Assets/Art/Materials/Hologram.mat` | `Assets/Art/Materials/Vfx/Hologram.mat` |
| `Assets/Scripts/Editor/CaptureSites/BattleAsteroidAssetBuilder.cs:20` | `Assets/Art/Models/Cannons/heavy-turbolaser-cannon-v1/Heavy Turbolaser Cannon V1.fbx` | `Assets/Art/Models/Cannons/HeavyTurbolaserCannon/HeavyTurbolaserCannon.fbx` |
| `Assets/Scripts/Editor/CaptureSites/AsteroidMiningFacilityAssetBuilder.cs:18` | `Assets/Art/Models/Station/space-station-asteroid-mining-facility/Space Mining Facility.dae` | `Assets/Art/Models/SpaceStations/AsteroidMiningFacility/AsteroidMiningFacility.dae` |
| `Assets/Scripts/Editor/CaptureSites/AsteroidMiningFacilityAssetBuilder.cs:28` | `Assets/Art/Materials/ShipShield.mat` | `Assets/Art/Materials/Vfx/ShipShield.mat` |

### Naming-dependent Editor tools

- `ShipLitSetupTool.UseShipLitMaterialCopies` computes `<source filename>_ShipLit.mat` in the source material directory.
  - Keep both members in the same destination directory and preserve suffix pairing; no speculative lookup rewrite.
- `AutodeskMaterialConverter.Convert` chooses the output directory from the metallic/roughness/color texture and uses the material filename as output prefix.
  - Generated `<material>_MetallicSmoothness.png` / `<material>_Occlusion.png` targets follow renamed material prefixes.
  - Cannon-generated maps currently under Gangut and XQ6-generated maps under Asteroids are mapped by actual consumers.
- `ShipWreckBuilder.CreateWreckMaterials` creates a material named `sources[i].name + "_Wreck"` and saves by that name.
  - Rename existing source material object names and existing wreck objects/files together; verify renderer slot pairing.
  - Do not run Sync Wreck Materials as a shortcut: it can create new material files if old wrecks were not migrated.
- `ShipWreckBuilder.SyncAllMaterials` uses existing unit-view names for wreck folders. Keep those folders and view names.
- Existing `WreckMaterialSyncTests` is an optional later check only when the user explicitly requests tests.

### Importer and source-link gates

- **21** models have explicit serialized external material remaps; **9** have none in the scanned metadata.
- Capture all imported objects, source material identifiers, external maps, renderer slots and mesh GUID/local fileIDs through the live Editor before mutation.
- Models without explicit remaps:
- `Assets/Art/Models/Other/JediCouncul/textures/JediCouncil.fbx`.
- `Assets/Art/Models/RepublicModels/Arquitens/Cruero LigeroImperial Arquitens.obj`.
- `Assets/Art/Models/RepublicModels/low-poly-imperial-i-class-star-destroyer/sketchfablowpolystardestroyer.obj`.
- `Assets/Art/Models/RepublicModels/SpaceStation2/source/untitled.fbx`.
- `Assets/Art/Models/RepublicModels/star-destroyer/source/Imperial-Class-StarDestroyer.fbx`.
- `Assets/Art/Models/SeparatistShip/CIS Lucrehulk/cis_cap_fedcoreship.obj`.
- `Assets/Art/Models/SeparatistShip/republic assault ship/republic assault ship.dae`.
- `Assets/Art/Models/Station/refueling-station/Refueling Station.fbx`.
- `Assets/Art/Models/Station/Vesta/model.obj`.

- Existing remap keys are source identifiers, not external filenames. Keep keys such as `object_0`, `Material.001`, `BLUETHRUSTER_CL`, and `veh_rep_destroyer_detail_d` unchanged.
- Use existing explicit mappings as identity evidence; resolve ambiguous name-based material searches to the same original objects before moving.
- `GetExternalObjectMap()` returns a copy; editing the returned dictionary alone does not change the importer. Apply verified mappings through the importer API and persist them. [Unity API](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetImporter.GetExternalObjectMap.html)
- OBJ companions found: XQ6, Arquitens, Delta7, StarDestroyer1, Thranta, Belbullab22, Lucrehulk, Vesta. StarDestroyer2 and Munificent also retain source MTL files beside their FBX.
- Providence OBJ references missing `CW-Providence.mtl`; Recusant OBJ references missing `CW-Recusant-S3E2.mtl`. Capture these pre-existing failures; do not invent files.
- Arquitens OBJ names its MTL; MTL `map_Kd` names its JPG. Vesta has the same dependency chain.
- StarDestroyer1 MTL contains existing invalid path forms, including `textures/destructables/...`; Delta7/Belbullab paths retain exported extra prefixes.
- Munificent/StarDestroyer2 MTL files contain source-author absolute paths and missing DDS/PSD references; automatic basename replacement is insufficient.
- AWing DAE uses absolute URL-encoded source image paths. Other DAE image paths must be inventoried per source.
- For proven source links, change only file path tokens to correctly computed relative target paths; preserve OBJ geometry, `usemtl`/`newmtl` names and DAE mesh/material IDs.
- Keep XML namespaces/encoding and MTL options. Never reconstruct models or edit embedded FBX/BLEND data by text replacement.
- Unresolved source links require an explicit remap/source-repair decision and before/after validation; their batches remain blocked until identity is established.

### Shared-reference evidence

- `Venator/EngineGlow.tga` → live Venator thruster + wreck material.
- `Venator/rep_ven_turrets.tga` → cannon materials, Venator turret material and defense-platform wreck.
- `Venator/ReV_venator.tga` → Venator hull/wreck and both Arquitens materials' saved `_MainTex`.
- `Venator/ReV_VenatorTd.tga` → live Venator hull + wreck material; do not omit the fourth texture.
- Gangut Set02 normal/roughness and Set03 normal/emission are shared with cannon/XQ6 materials and wrecks.
- Asteroid source metallic/roughness and generated XQ6 maps are consumed outside the Asteroid model.
- AsteroidMiningFacility glass AO is shared with `Assets/Art/Materials/Shields.mat`.
- Shared bindings follow GUIDs to their original asset; moving them does not authorize duplicating/rebinding by nearest folder.
- Keep all explicit shader keyword, texture scale/offset, color, transparency, livery and render-queue values.

### Resources/settings evidence

- URP Performant is referenced by GraphicsSettings and QualitySettings; Balanced and HighFidelity are referenced by QualitySettings.
- Renderer assets are referenced by pipeline assets; HighFidelity renderer also appears in other renderer assets.
- SampleSceneProfile has no scanned serialized GUID consumers; retain it as a configuration asset.
- No Resources.Load/LoadAsync/LoadAll or matching settings-path literals found in the scanned project-owned Scripts/Tools/ProjectSettings searches.
- Move assets while preserving GUIDs; verify Graphics/Quality settings resolve the same GUIDs. Do not replace configured pipeline assignments routinely.
- Resources placement affects inclusion/indexing; removing these paths does not prove a specific RAM saving or exclusion from builds when other references remain. [Unity Resources guidance](https://docs.unity3d.com/6000.4/Documentation/Manual/LoadingResourcesatRuntime.html)

### Pre-existing texture-reference baseline

- Eight distinct texture GUID tokens had no match in the scanned Assets metadata index.
- This is a candidate list, not eight proven active missing textures; saved inactive properties, built-in IDs and package samples must be distinguished by the Editor.
- The reference index matches YAML-style `guid:` tokens; ShaderGraph JSON dependencies and embedded/binary references require Editor checks.

| Token | Serialized assignments | Classification |
| --- | --- | --- |
| `21bf9efbdbbbd4e34af72f1de5e334f6` | DualProjectileMaterial BaseMap/EmissionMap; LazerProjectileMaterial, ProjectileDeploy, ProjectileMaterial EmissionMap | No metadata match in Assets or searched package cache; verify active shader use |
| `0000000000000000f000000000000000` | ParticlesUnlit MainTex | Reserved/built-in-looking token; do not classify as missing project asset |
| `ea3fc985fb308433eb7ea824447f0426` | ProjectileDeploy MainTex | No local/package metadata match; may be inactive |
| `1b62799c2814d6d4b81ee977cfc9e12f` | Test MainTex | No local/package metadata match; cleanup remains separate |
| `26a0dfdea3e2a466d97d51af0372a6c5` | Transparent BaseMap/MainTex | Found in URP package-cache Samples~/SharedAssets/Textures/Checker.png.meta; sample not present under Assets |
| `690ad5cb0d03dfa4d8107b2084ff9803` | NebulaTwoVfx 2 MainTex | No local/package metadata match; verify active use |
| `c5917249e89888346a4bd606beae9578` | SpaceStation2 Material.003 EmissionMap | No local/package metadata match; verify active use |
| `38cec185e7d5c3e45acd5838295f23b1` | SpaceStation2 Material MetallicGlossMap | No local/package metadata match; verify active use |

- Pass condition is **zero new unresolved active references**, not an unsupported promise that every pre-existing reference is already valid.
- Record existing Console errors separately; migration must add no import, serialization or reference errors.

### Byte-identical groups

- **28** SHA-256-equal groups were found among audited materials/images. Keep all identities; this migration performs no deduplication.
- Equality covers source file bytes only; metadata/import settings and source consumers can differ.
- Group 1: `Assets/Art/Materials/Wrecks/AsteroidDefendPlatform/Material #2142150880_Wreck.mat`; `Assets/Art/Materials/Wrecks/AsteroidMiningFacility/Material #2142150880_Wreck.mat`.
- Group 2: `Assets/Art/Materials/Wrecks/AsteroidDefendPlatform/ORDNUNGSKRAFT Graviton_Wreck.mat`; `Assets/Art/Materials/Wrecks/AsteroidMiningFacility/ORDNUNGSKRAFT Graviton_Wreck.mat`.
- Group 3: `Assets/Art/Materials/Wrecks/AsteroidDefendPlatform/SG-Gold-glow_Wreck.mat`; `Assets/Art/Materials/Wrecks/AsteroidMiningFacility/SG-Gold-glow_Wreck.mat`.
- Group 4: `Assets/Art/Materials/Wrecks/AsteroidDefendPlatform/SG-LightW_Wreck.mat`; `Assets/Art/Materials/Wrecks/AsteroidMiningFacility/SG-LightW_Wreck.mat`.
- Group 5: `Assets/Art/Materials/Wrecks/AsteroidDefendPlatform/Window_Wreck.mat`; `Assets/Art/Materials/Wrecks/AsteroidMiningFacility/Window_Wreck.mat`.
- Group 6: `Assets/Art/Materials/Wrecks/HeavyDreadnought/UnitDefaultLit_Wreck.mat`; `Assets/Art/Materials/Wrecks/RepublicSpaceStation/UnitDefaultLit_Wreck.mat`; `Assets/Art/Materials/Wrecks/StarDestroyer2/UnitDefaultLit_Wreck.mat`.
- Group 7: `Assets/Art/Models/Asteroids/textures/Material.001_AO.jpg`; `Assets/Art/Models/Asteroids/textures/Material.001_emissive.jpg`; `Assets/Art/Models/Asteroids/textures/Material.001_metallic.jpg`.
- Group 8: `Assets/Art/Models/Other/sci-fi-lamps/source/parts1_Occlusion.png`; `Assets/Art/Models/Other/sci-fi-lamps/source/parts2_Occlusion.png`; `Assets/Art/Models/Other/sci-fi-lamps/source/shell_Occlusion.png`.
- Group 9: `Assets/Art/Models/Other/sci-fi-lamps/source/parts2_MetallicSmoothness.png`; `Assets/Art/Models/Other/sci-fi-lamps/source/shell_MetallicSmoothness.png`.
- Group 10: `Assets/Art/Models/RepublicModels/Delta7/Textures/dull_metal_metallic.png`; `Assets/Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/textures/dull_metal_metallic.png`; `Assets/Art/Models/SeparatistShip/belbullab/Textures/dull_metal_metallic.png`.
- Group 11: `Assets/Art/Models/RepublicModels/Delta7/Textures/dull_metal_normal-ogl.png`; `Assets/Art/Models/SeparatistShip/belbullab/Textures/dull_metal_normal-ogl.png`.
- Group 12: `Assets/Art/Models/RepublicModels/Delta7/Textures/dull_metal_roughness.png`; `Assets/Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/textures/dull_metal_roughness.png`; `Assets/Art/Models/SeparatistShip/belbullab/Textures/dull_metal_roughness.png`.
- Group 13: `Assets/Art/Models/RepublicModels/star-destroyer/textures/ISD_Hull_Color_Baked.png`; `Assets/Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/textures/ISD_Hull_Color_Baked.png`.
- Group 14: `Assets/Art/Models/RepublicModels/star-destroyer/textures/ISD_Hull_Height_Render.png`; `Assets/Art/Models/RepublicModels/star-wars-dreadnaught-class-heavy-cruiser/textures/ISD_Hull_Height_Render.png`.
- Group 15: `Assets/Art/Models/RepublicModels/Thranta/Textures/veh_rep_destroyer_metal01_n.png`; `Assets/Art/Models/RepublicModels/Thranta/Textures/veh_rep_destroyer_metal02_n.png`.
- Group 16: `Assets/Art/Models/SeparatistShip/republic assault ship/republic assault ship.fbm/Dirty shiny.jpg`; `Assets/Art/Models/SeparatistShip/republic assault ship/textures/Dirty shiny.jpg`.
- Group 17: `Assets/Art/Models/SeparatistShip/republic assault ship/republic assault ship.fbm/Dirty2 Spec.jpg`; `Assets/Art/Models/SeparatistShip/republic assault ship/textures/Dirty2 Spec.jpg`.
- Group 18: `Assets/Art/Models/SeparatistShip/republic assault ship/republic assault ship.fbm/Dirty2.jpg`; `Assets/Art/Models/SeparatistShip/republic assault ship/textures/Dirty2.jpg`.
- Group 19: `Assets/Art/Models/SeparatistShip/republic assault ship/republic assault ship.fbm/Engine panels.jpg`; `Assets/Art/Models/SeparatistShip/republic assault ship/textures/Engine panels.jpg`.
- Group 20: `Assets/Art/Models/SeparatistShip/republic assault ship/republic assault ship.fbm/General Panels.jpg`; `Assets/Art/Models/SeparatistShip/republic assault ship/textures/General Panels.jpg`.
- Group 21: `Assets/Art/Models/Station/FreePort/textures/tech_floor_8_rough_by_artofsoulburn_d4t7ff.jpg`; `Assets/Art/Models/Station/space-station-asteroid-mining-facility/textures/tech_floor_8_rough_by_artofsoulburn_d4t7ff.jpg`.
- Group 22: `Assets/Art/Models/Station/futuristic-open-concept-space-station/textures/starship_hull_12_by_artofsoulburn_d91kpch.jpg`; `Assets/Art/Models/Station/space-station-asteroid-mining-facility/textures/starship_hull_12_by_artofsoulburn_d91kpch.jpg`.
- Group 23: `Assets/Art/Models/Station/futuristic-open-concept-space-station/textures/starship_hull_13_by_artofsoulburn_d91kpjr.jpg`; `Assets/Art/Models/Station/space-station-asteroid-mining-facility/textures/starship_hull_13_by_artofsoulburn_d91kpjr.jpg`.
- Group 24: `Assets/Art/Models/Station/gangut-space-hub/textures/lambert1_ShipLit_Occlusion.png`; `Assets/Art/Models/Station/gangut-space-hub/textures/set2_Occlusion.png`.
- Group 25: `Assets/Art/Textures/BlasterBolt.png`; `Assets/Art/Textures/BlasterBoltTilled.png`.
- Group 26: `Assets/Art/Textures/Ui/Icons/ShipIcon/UpsideDown/AcclamatorShipUi.png`; `Assets/Art/Textures/Ui/Icons/ShipIcon/UpsideDown/AcclamatorShipUiUnits.png`.
- Group 27: `Assets/Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArqShipIcon.png`; `Assets/Art/Textures/Ui/Icons/ShipIcon/UpsideDown/ArqShipIconUnits.png`.
- Group 28: `Assets/Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VenatorUpsidedownIcon.png`; `Assets/Art/Textures/Ui/Icons/ShipIcon/UpsideDown/VenatorUpsidedownIconCannons.png`.

## Edge Cases

- A preserved file GUID does not prove preserved imported mesh/material local fileIDs; compare referenced subassets before/after every model batch.
- Texture sprite subassets, sprite IDs, atlas membership, import type, sRGB, normal-map flags, platform overrides and compression settings must remain unchanged.
- Material filename and serialized object name can differ; set only the main material name to the target filename stem, mark dirty and save.
- Referenced material variants are not duplicates solely because they share albedo or bytes.
- Name-based import search can change resolution after external material relocation; establish exact pre-move mappings first.
- Case-only rename on Windows → unique temporary Unity asset path → final path; preserve the same metadata/GUID.
- Split type-first bundles cannot be executed as a single parent-folder move. Move exact files; remove old empty folders only after folder-GUID dependency checks.
- Importer failures, Blender unavailable, unresolved source identities or dirty untitled scenes → stop affected batch; keep unrelated user work.
- Folder/source-link corrections can expose existing importer defects; compare baseline instead of silently generating replacement assets.
- OS junk: two Thumbs.db files in AcclamatorAssault sources, `Assets/Plugins/.DS_Store`, and `Assets/Plugins/Zenject/.DS_Store`.
- OS cleanup is optional separate work after inventory/metadata checks; no speculative deletion of Test, Render, CaptionRex, grid variants, stock images or duplicate textures.
- Static collision verification applies to files at audit time; implementation rechecks folder collisions, case sensitivity, source GUIDs, target availability and concurrent work.

## Files

- [[PROJECT_ORGANIZATION]] — current organization authority; proposed conventions must be recorded there before asset changes.
- [[Project_Organization_Remediation_Plan]] — phases, preflight, persistence, verification, rollback and completion criteria.
- `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` — version evidence.
- `Assets/Scripts/Editor/Rendering/ShipLitSetupTool.cs` — filename-based ShipLit pairing.
- `Assets/Scripts/Editor/Rendering/AutodeskMaterialConverter.cs` — generated texture paths and packed-map naming.
- `Assets/Scripts/Editor/Rendering/ShipWreckBuilder.cs` — source-name-derived wreck filenames.
- `Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs` and `Assets/Scripts/Editor/CaptureSites/*.cs` — literal path updates.
- [Unity asset metadata](https://docs.unity3d.com/Manual/AssetMetadata.html) — asset identity and metadata association.
- [MoveAsset](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetDatabase.MoveAsset.html) — project-relative file/folder move; nonempty returned string indicates failure.
- [TryGetGUIDAndLocalFileIdentifier](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetDatabase.TryGetGUIDAndLocalFileIdentifier.html) — capture GUID/localID with the Object overload and long localID.
- [Material import/remapping](https://docs.unity3d.com/Manual/FBXImporter-Materials.html) — naming/search behavior and external material remaps.
- [StartAssetEditing](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetDatabase.StartAssetEditing.html) — pause imports; always resume in finally.
- [ForceReserializeAssets](https://docs.unity3d.com/6000.4/Documentation/ScriptReference/AssetDatabase.ForceReserializeAssets.html) — explicit paths only; direct operation, not import/OnEnable callback.
- Documentation lookup: Context7 → local Context index (Unity package unavailable) → official Unity 6.4 references.

## TODO

- [x] Recheck all manifest rows and external consumers against the live checkout at implementation start.
- [x] Capture live imported subassets, renderer/material slots, external remaps, sprite IDs and Console baseline.
- [x] Establish exact identity for nine models without explicit remaps; resolve source-link blockers before their batches.
- [x] Update PROJECT_ORGANIZATION with final type-first roots, naming rules and retained exceptions.
- [x] Implement the plan; this research task changed documentation only.
- [ ] Keep format conversion, shader/reference repair, deduplication, lore corrections and scratch cleanup in separately defined work.
