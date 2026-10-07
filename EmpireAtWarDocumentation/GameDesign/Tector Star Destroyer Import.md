---
type: reference
updated: 2026-10-07
tags:
  - empire
  - ship
  - alo
  - asset-pipeline
---
# Tector Star Destroyer Import

## Important Values

- Empire `ShipType.Tector = 210`; hull `22,000`, shields `16,000`, speed `22`.
- Targetable: `8` heavy 2-burst turbolasers + `2` medium long-range dual turbolasers + `2` shield generators + `3` engines + `1` tractor beam → `16` targets, IDs `0..15`.
- Non-targetable: `7` medium 3-burst turbolasers + `4` light turbolasers + `4` heavy lasers → `25` weapons / `31` unique mount IDs overall.
- No hangar hardpoint or fighter bays. User targetability overrides the import guide's default.
- Boost Weapon Power `32`: active `20 s`, recovery `60 s`; fire delay ×`0.5`, damage ×`1`, speed ×`0.25`, shield regeneration ×`0`, incoming damage ×`1.5`. Tractor Beam uses existing ability `22` and its target/range rules.
- Provisional ISD II balance: cost `25,000`, build `500 s`, level `3`, population `8`, limit `3`, turn/acceleration `5`, shield regeneration `20/s`. Source cost/build: `22,000 / 440 s`.

## Implementation

- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]] before implementation. `Temp/ALO_MODEL_MAP.txt` was absent; extracted `output/aotr-empire-units/unit-list.txt` and installed XML identify `E_Tector_Star_Destroyer` → `Empire_Imperial_SD.ALO`.
- AOTR workshop `1397421866`: `21` source assemblies; reuse `11` hash-matched ISD I models, convert `10` Tector attachments in Blender `3.6.23` through MCP. Original `41` source hashes unchanged.
- Include Tector armor, superstructure, eight heavy dual turrets, seven triple turrets and two side turrets. Actual source attachment/fire bones drive all mounts and three engine effects.
- Restore `Hull_SS`'s four original material slots: `712 / 116 / 330 / 318` triangles. Geometry/UVs unchanged; Unity static mesh refresh retains submeshes.
- New conversions: `43` meshes / `40,716` decoded triangles. Blender max geometry/bone errors `0.000122071 / 0.000085773` source units; Unity bone error ≤`0.000001986` project units. Slots, shaders, textures, visible/collision triangles, UVs and bone parents verified.
- Visible bounds `102.612 × 68.559 × 180` project units; root scale `1`, bow `+Z`, up `+Y`. Banked hull range `−38.620 .. 34.579`; navigation radius `109`. Shield, ion bounds, collision, selection and banking fit this hull.
- Own gameplay/visual/placement/wreck prefabs, data, matchups and actual-model transparent `512 × 512` icon/silhouette. Empire roster, ShipsData, asset mapping, existing Addressables groups, HUD/tooltip icons and placement registry resolve.
- Remove `21` unused prefab helpers / `8,378` triangles; retain source representations. Eight living/wreck team palettes rendered; icon, blue/green, placement and wreck inspected. Saved checks pass; no missing scripts, broken references, mount displacement, or Console errors.

## Edge Cases

- ALAMO decodes shadow volumes to disabled helper surfaces; source shadow-volume, animated shader/proxy effects and ALA animation are not reproduced.
- Shared weapon profiles `43 / 53 / 38 / 33 / 28` preserve requested composition/bursts; projectile tuning remains project balance.
- No automated Unity tests or Play Mode run. Combat arcs, ability targeting, reinforcement deployment and destruction behavior remain unverified at runtime.

## Files

- `Tools/Blender/Tector/README.md` — rebuild order and balance decisions; `ImportEvidence.json` — source hashes, conversion/saved-asset evidence.
- `Temp/TectorImport/` — editable blends, source/binary reports, round-trip and Unity checks, mappings, renders.
- `Assets/Prefabs/Models/Ships/Tector.prefab`, `TectorShipView.prefab`; `Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab`.
- `Assets/Settings/Data/Ship/TectorShipData.asset`, `Ship/Wreck/TectorWreckData.asset`; `Assets/Art/Textures/Ui/Icons/ShipIcon/TectorIcon.png`.
