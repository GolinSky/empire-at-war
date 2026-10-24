---
type: reference
updated: 2026-10-24
tags:
  - blender
  - unity
  - alo
  - republic
---
# Pelta-class Frigate Import

## Files

- Guide: [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]]; layout: [[Architecture/PROJECT_ORGANIZATION]].
- Source: Republic at War Workshop `1129810972`, `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/`.
- `XML/Units_Space_Republic_Pelta.xml`: `Pelta_Frigate` → `Art/Models/ReV_Pelta.ALO`; death clone → `ReV_Pelta_D.ALO`.
- Tooling: `Tools/Blender/Pelta/`; working conversion/evidence: `Temp/PeltaImport/`.
- FBX: `Assets/Art/Models/RepublicShips/Pelta/Pelta.fbx`; visual/gameplay: `Assets/Prefabs/Models/Ships/Republic/Pelta.prefab`, `PeltaShipView.prefab`.
- Data: `Assets/Settings/Data/Ship/Republic/PeltaShipData.asset`; wreck: `Assets/Settings/Data/Models/Wrecks/Republic/PeltaWreckData.asset`.

## Implementation

- Converted through Blender MCP, Blender `3.6.23`, protocol `13`, telemetry disabled. Original mod files remain unchanged.
- ALAMO stopped in its global shadow cleanup because another scene owns `Collision`; repaired only the Pelta hierarchy/constraints/materials from the binary audit. Duplicate proxy names retain importer suffixes; source bone indices determine parents.
- Source → FBX: `11` meshes, `18,818` triangles, `45` bones; all UV corners unchanged. Maximum position/bone errors `0.0001283 / 0.0000792` source units.
- Unity: all material slots/triangle counts/bone parents verified; all opaque UV corners plus sampled helper corners checked. Maximum position/bone errors `0.00000318 / 0.00000153` project units.
- Game model: `6` visible meshes; opaque `Hull_Fixed`, `Wing_R`, `Wing_L` total `2,668` triangles; three additive light meshes total `54`. Collision, source shield and stencil-shadow geometry remain in source FBX, absent from game renderers.
- Source albedo/normal/light DDS files decoded losslessly. Normal map uses linear sampling and green-channel flip; light shader is a static additive approximation.
- Republic registration: `ShipType.Pelta = 14`; own roster, data/view Addressables, icon consumers, matchups and reinforcement mapping. Existing Addressables groups reused.

## Important Values

- Requested: hull `2,200`; shields `1,000`; population `2`; cost `1,800`; build `20 s`.
- Weapons: `4 × TurboLaser (1)`, `4 × Laser (8)`, `2 × IonCannon (10)`; `12` targetable hardpoints including engines/generator.
- Turbolaser socket pairs: `HP01/HP02`, `HP06/HP07`, `HP03/HP04`, `HP08/HP09`; lasers: `HP11..HP14`; ions: `HP05`, `HP10`.
- One emitter per logical weapon → paired turbolasers fire from source-pair midpoints. All original attachment bones remain preserved.
- Engines: derived nearest stern hull surface; generator: derived dorsal hull surface above `Hull_Fixed`. Weapon health `250`, engines/generator `400` each; all hull-damage multipliers `1`.
- Project choices: length `45`, size `35.342 × 12.194 × 45`, navigation/fog radius `31`, banking `±15°`; banked Y range `−6.85987 .. 6.09690`.
- Frigate defaults: speed `30`, turn/acceleration `45/45`, range `500`, height tier `7`, shield regeneration `8.3333/s` after `1 s`, roster level `2`, cap `20`. No abilities or hangar.

## Rules

- Team livery: source-red HSV selection, hue `0`, range `.07`, minimum saturation `.3`, strength `1`; direct mask strength `0`. Albedo alpha does not provide a selective paint mask.
- Live model binds all renderers through `UnitRenderStateComponent`; verified ownership user values `1..8` and rendered all eight live/wreck palettes.
- Current `ShipWreckBuilder` generates an Addressable wreck from the three opaque meshes. Current GPU cuts/drift/tilt/sink/dissolve apply; source death animation is not used.
- Wreck preserves livery/material settings; explicitly set wreck-only `_TeamRimStrength=0` to keep neutral hull panels neutral. Live Ship Lit no longer exposes that property.
- Wreck defaults: lifetime `30 s`, dissolve/glow `8 s`, `2–3` parts, cut ratio `.5–.7`, separation/tilt/sink `1.5/2/1`, pool limit `32`.
- Placement uses own hull and shared hologram material. Transparent `512 × 512` icon renders the actual Pelta.

## Edge Cases

- Deployment/undeployment and death ALA animations are not converted; this is the source static pose.
- Dedicated saved-asset verification passes: exact requested values, weapons, source mount positions, ownership, wreck parity, placement, icons and registrations. Unity import/production compilation and final Console check: no errors.
- EditMode: team-color prefab tests `2/2`; placement tests `3/3`; weapon-loadout tests `5/5`; broader wreck filter `135/138`. Both Pelta material/geometry cases, dependency checks and cut-plan tests pass.
- Unrelated failures: Raider/Tartan registration tests still read retired `<Prefab>k__BackingField`; MiningFacility wreck has pre-existing `_TeamLiveryHue`/`_TeamLiveryHueRange` differences. No unrelated assets or tests changed.
- Play Mode combat has not been run. Firing arcs use inherited full-circle arcs; timings, combat balance and live destruction remain runtime acceptance work.
