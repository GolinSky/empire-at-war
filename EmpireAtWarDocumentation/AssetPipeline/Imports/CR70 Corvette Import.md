---
type: reference
updated: 2026-10-24
tags:
  - asset-pipeline
  - blender
  - unity
  - republic
---
# CR70 Charger Corvette Import

> Plan: none — historical provenance only; no tracked open work (2026-10-29 review).

## Decision

- Correct source: Republic at War Workshop `1129810972`, installed `1.5.5`; `Data/Art/Models/ReV_light_frigate.ALO`.
- `Units_Space_republic_light_frigate.xml`: C-70 / `Republic_light_frigate`; `Hardpoints_Republic_Space.xml`: “Charger c70 (Light Frigate).”
- Model and DDS textures are loose files inside the mod. Avoid base-game `RV_CORVETTE.ALO`: earlier substitution rejected by the user and replaced; 11 unused assets removed through Unity.
- Project unit remains `CR70 Corvette`, `ShipType.CR70 = 13`; prefab/data/icon GUIDs and Republic registrations preserved.

## Implementation

- Guide: [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]]; tooling: `Tools/Blender/CR70/README.md`; corrected evidence/editable blend: `Temp/CR70ChargerImport/`.
- Blender MCP `3.6.23`: fresh Charger scene, binary material/bone audit, five meshes / 4,222 triangles / 14 bones retained through FBX. Binary-backed Root parents restored after ALAMO global cleanup failure; no extra bone added.
- Visible opaque `Hull` + `Object02`: 4,161 triangles. Visible additive `Engine`: 48 triangles, source yellow RGB tint retained. Hidden `Engines`/`Collision`: 13 triangles, retained in FBX and stripped of runtime rendering.
- Mod DDS→PNG pixel equality checked: hull/normal 1024²; engine emission 64². Inverse hull alpha → linear team mask; normal green channel flipped in Unity.
- User-requested team tint reduction: `_TeamMaskStrength` `1 → 0.35` on both living hull pieces and their wreck materials; neutral hull detail remains prominent.
- `UnitRenderStateComponent` explicitly binds hull/effect/shield renderers; current `ShipWreckBuilder`/`UnitWreckView` uses two opaque hull meshes with team masks, cuts/separates/sinks/dissolves.
- New hull-fitted shield, collision/selection/ion bounds, destruction VFX anchors, hologram preview, actual-model icon/silhouette and existing Addressables registrations saved.

## Important Values

- User configuration: hull/shields `750/650`, cost `1,250`, population `2`; source stats differ and do not override the request.
- Eight logical `Laser` components: two per authored `HP01..HP04`; two `PointDefense` at `HP05/HP06`; engine at authored `Engines`; generator at actual dorsal hull surface above `Hull` bone. All 12 targetable IDs explicitly bound.
- Paired guns share the authored mount socket. No source generator socket; its surface fitting is a project choice. No vanilla mount positions retained.
- Existing engine ability `14`: active/recovery `20/50 s`, speed ×2, weapon delay ×3, shield regeneration ×0.
- Existing defaults retained: speed `40 units/s`, turn/acceleration `60/60`, range `300`, bank `15°`, tier `7`, regeneration `650/120 = 5.416667/s`, delay `1 s`; build `15 s`, level `2`, roster cap `10`.
- Centered dimensions `9.223910 × 2.717261 × 15.749785` project units; bow `+Z`, up `+Y`, root scale 1. Wreck lifetime/dissolve `30/8 s`.

## Files

- `Assets/Art/Models/RepublicShips/CR70/CR70.fbx`; materials/textures in matching type-first `RepublicShips/CR70` folders.
- `Assets/Prefabs/Models/Ships/Republic/CR70.prefab`, `CR70ShipView.prefab`; `Assets/Settings/Data/Ship/Republic/CR70ShipData.asset`.
- `Assets/Prefabs/Models/Wrecks/CR70WreckView.prefab`; `Assets/Settings/Data/Models/Wrecks/Republic/CR70WreckData.asset`.
- `Assets/Prefabs/Ui/Reinforcement/CR70ReinforcementView.prefab`; `Assets/Art/Textures/Ui/Icons/ShipIcon/CR70Icon.png`.

## Rules

- `Prepare.py` → Blender MCP import/`Convert.py`/`VerifyGeometry.py` → capture JSON evidence → `Stage.py`.
- Unity order: `BuildArt` → `VerifyUnityGeometry` → `BuildShip` → `Render` → `Register` → `CleanupRejectedHull` → `Verify`.
- Unity entry points and exact commands: `Tools/Blender/CR70/README.md`. Builders save assets; no manual Editor interaction required.
- Source `exported_name` records global Blender naming suffixes explicitly; map helpers by recorded names, never guessed suffix removal in Unity.

## Edge Cases

- Blender corner/UV/bone error ≤`0.000018120` source units / `0.000001908` / `0.000019074`. Unity geometry/bone error ≤`0.000000457` / `0.000000299` project units; mesh/UV/material/bone checks passed.
- Saved verification passes exact stats, 12 hardpoints, new-model sockets, ownership values 1–8, masks/wreck, preview/icons, registrations and broken-reference checks; no vanilla Corvette visual dependencies.
- Eight living/wreck palettes rendered; blue/green, three-engine stern, placement and wreck inspected. Transparent 512² icon/silhouette.
- Unity imports/production compilation complete with no new Console errors on `2026-10-24`; temporary verifier has one obsolete-API warning.
- No automated Unity tests or Play Mode run. Runtime combat/deployment/ability/death acceptance remains untested.
