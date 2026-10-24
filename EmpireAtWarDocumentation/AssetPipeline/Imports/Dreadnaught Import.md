---
type: reference
updated: 2026-10-24
tags:
  - blender
  - unity
  - alo
  - asset-pipeline
system:
  - asset-pipeline
---
# Dreadnaught-class Heavy Cruiser Import

## Decision

- Replace existing Republic `HeavyDreadnought` art; retain its identity, asset GUIDs, six logical weapons, abilities and gameplay tuning.
- Source: Republic at War `1.5.5`, Workshop `1129810972`; XML `Units_space_republic_dreadnaught.xml` → `ReV_dreadnought_heavy_cruiser.alo`.
- Attach all twelve `ReV_dreadnought_turret_00..11.alo` models at `b_attach_00..11`, following `Hardpoints_Republic_Space.xml`.
- Keep the current procedural wreck system. Source death ALO uses the same hull geometry; its EaW death animation is not required by this pipeline.

## Implementation

- Blender MCP `3.6.23` imports ALO, repairs audited material slots/parents, joins turret skeletons without moving geometry, exports FBX and validates a separate round trip.
- Transport: `63` meshes / `40,221` triangles / `141` bones. Runtime: `25` opaque meshes / `8,110` triangles. Collision, shadow and damage decal meshes remain only in transport art.
- Source alpha mask is empty. Use source red paint: hue `0`, range `0.07`, minimum saturation `0.4`, livery strength `1`, mask strength `0`; neutral hull remains unchanged.
- One shared hull material and normal map; wreck material copies livery and sets `_TeamRimStrength = 0`.
- Preserve actual old visible length `73.00743` units. New centered dimensions `13.58614 × 12.15677 × 73.00743`; bow `+Z`, up `+Y`, gameplay root scale `1`.
- Dedicated `Body` pivot owns geometry, all eight hardpoints, fitted shield and six authored exhaust positions. Ownership/fog/explosion arrays rebuilt; selection sprite remains outside fog control.
- IDs `0..5` keep existing weapon profiles/arcs; representative muzzle mounts `06,07,04,05,00,01`. Other source turrets remain visual geometry. IDs `6/7`: generator/engines.
- Refit collider, shield shell, ion bounds, selection ring and placement trigger. Banked height `-6.191002 .. 7.0018487`; navigation radius `40`.
- `ShipWreckBuilder` regenerates all `25` meshes with existing GPU cuts/drift/heat/dissolve. Existing wreck GUID `757a79319c0388c469ac8ac2e2c80f92`, `2–3` parts, lifetime `30 s`, dissolve/glow `8 s` remain intact.
- Regenerate existing icon and hologram preview in place; source data and original ALO/DDS files remain unchanged.

## Important Values

- Hull/shields `3600/2500`; speed `18`; turn/acceleration `7.5/7.5`; bank `15°`; weapon types `2/3/11`, two each; ability `6` unchanged.
- Blender round trip: maximum position error `0.00004578`, UV error `0.00000603`, bone error `0.00003510` source units.
- Unity validates all mesh/material/triangle counts, `3,807` sampled triangle corners and all bones; maximum bone error `0.000000871` Unity units.
- Saved-asset verification passes; all eight live and wreck palette renders differ. No new Console warnings/errors; `MainMenuScene` remains clean.
- Automated Unity tests and Play Mode combat were not run.

## Files

- `Tools/Blender/HeavyDreadnought/` — audit, conversion, integration, verification, evidence and previews.
- `Assets/Art/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought.fbx` — Unity model.
- `Assets/Prefabs/Models/Ships/Republic/HeavyDreadnought.prefab` — geometry/attachments.
- `Assets/Prefabs/Models/Ships/Republic/HeavyDreadnoughtShipView.prefab` — gameplay view.
- `Assets/Prefabs/Models/Wrecks/HeavyDreadnoughtWreckView.prefab` — generated wreck.
- `Assets/Prefabs/Ui/Reinforcement/HeavyDreadnoughtReinforcementView.prefab` — placement preview.
- Credits: mesh `Farseer`; textures `Farseer/z3r0x`; rigging/coding `z3r0x` (RaW `credits.md`, Dreadnaught Cruiser entry).
- [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]]; [[Architecture/PROJECT_ORGANIZATION]].
