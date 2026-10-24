---
type: import
updated: 2026-10-24
tags:
  - blender
  - unity
  - alo
  - republic
---
# Carrack Import

## Goal

- Add Carrack-class Cruiser to Republic level 2 from Workshop mod `1129810972`.

## Implementation

- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/Art/Models/ReV_carrack.ALO`; unit XML: `Units_space_republic_carrack.xml`.
- Pipeline: [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] → Blender MCP `3.6.23` → `Carrack.fbx` → Unity. Original ALO/DDS unchanged.
- Source: 4 meshes, 23,393 total triangles, 28 bones. Runtime uses opaque `HULL`: 4,638 triangles. Source `shadow`, `collision`, hidden `engines` helpers retain transforms without runtime renderers.
- ALAMO global cleanup failed on another scene's object; complete cleanup and restore binary-audited hierarchy only in `Carrack Source`. Preserve other Blender scenes.
- Hull/normal DDS → pixel-identical PNG. Normal uses linear normal-map import with green flip. Inverse hull alpha → linear team mask; strength `0.35` on living/wreck materials.
- Twelve `WeaponType.DualLaser` mounts at `MuzzleA_00..11`; engine at `Thruster`, generator at `Shieldgen`. IDs `0..13`, all targetable and explicitly bound to health/DI.
- Current ownership: `UnitRenderStateComponent`; all hull/shield renderers bound. No obsolete `TeamColorView` component.
- Wreck: current `ShipWreckBuilder` + `ShipWreck` shader, own opaque hull/material, owner tint retained; wreck data holds Addressable reference. Runtime defaults: 2–3 parts, lifetime `30 s`, glow/dissolve `8 s`.
- Registrations: `ShipType.Carrack = 15`, Republic roster, ShipsData, asset mappings, existing Addressables groups, HUD/tooltip icons, own matchups, placement preview and icon-generator mapping.

## Important Values

- Requested: hull `1,600`, shields `1,000`, `12` dual laser mounts, cost `1,600`, population `1`, available level `2`.
- Existing Power to Weapons ID `20`: active `10 s`, recovery `60 s`, weapon delay ×`0.5`, speed ×`0.75`, incoming damage ×`1.5`. Reuses project ability; RaW's source modifiers differ.
- Project defaults: gameplay length `24 units`, speed `40 units/s`, turn/acceleration `60/60`, full-circle weapon arcs, range `300`, bank `15°`, height tier `7`, shield regeneration `1000/120 = 8.333333/s` after `1 s`. Build `20 s`, roster cap `10`, no hangar.

## Files

- `Tools/Blender/Carrack/`: source audit, conversion, staging, Unity builders, rendering and verification scripts.
- `Assets/Art/Models/RepublicShips/Carrack/Carrack.fbx`; textures/materials in corresponding type-first `RepublicShips/Carrack/` folders.
- `Assets/Prefabs/Models/Ships/Republic/Carrack.prefab`, `CarrackShipView.prefab`; own wreck and reinforcement prefabs.
- `Assets/Settings/Data/Ship/Republic/CarrackShipData.asset`; `Assets/Settings/Data/Models/Wrecks/Republic/CarrackWreckData.asset`.
- `Temp/CarrackImport/`: editable blend/FBX, source hashes, geometry reports, saved-asset report and palette/preview renders.

## Verification

- Blender ALO → FBX: every triangle corner/UV, material slot, bone name/parent verified. Position error ≤`0.000015691`, UV error ≤`0.0000007153`, bone error ≤`0.000014306` source units.
- Unity: all triangle/material counts, 28 bone transforms/parents and sampled source corners verified; maximum position/bone error ≤`0.000000466` / `0.000000365` units.
- Saved-asset verifier passes exact stats/roster, 14 hardpoints, source sockets, health/DI/fog/ownership, collider, wreck Addressable, hologram, all icon mappings, no broken references or donor visual dependencies.
- Eight living/wreck palettes rendered; green hull/wreck, model icon and placement visually inspected. No Carrack import, serialization or compile errors.
- Automated Unity test suite and Play Mode combat were not run. Runtime firing, ability and death behavior remain untested.
