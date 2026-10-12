---
type: reference
updated: 2026-10-12
tags:
  - rebellion
  - alo
  - asset-pipeline
---
# CD-273 Corellian Destroyer Import

## Files

- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; balance: [[GameDesign/Rebel units]], unit 4.
- Remake 4.0 Workshop `1770851727`: `Data/Xml/Units/Space/Units_Space_Rebel_CHG.xml` → `Corellian_Destroyer` → `Rebel_CD_273.ALO`.
- Models/textures: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Art/{Models,Textures}/`. Supplied `Texturesc` directory does not exist.
- Tools: `Tools/Blender/CD273/README.md`; package: `output/remake-rebel-units/CD273-Converted/`.
- Gameplay/model: `Assets/Prefabs/Models/Ships/CD273ShipView.prefab`, `CD273.prefab`; data: `Assets/Settings/Data/Ship/CD273ShipData.asset`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/CD273ReinforcementView.prefab`; wreck: `Assets/Prefabs/Models/Wrecks/CD273WreckView.prefab`; icon: `Assets/Art/Textures/Ui/Icons/ShipIcon/CD273Icon.png`.

## Important Values

- Requested hull `2,400`, shields `1,700`, speed `52 units/s`, cost `2,200`, build `32 s`; Remake source stats differ and are preserved in `SourceAudit.json`.
- Weapons: four medium lasers (`23`), two concussion missile launchers (`13`), two point-defense lasers (`14`); one engine + one shield generator → ten targetable hardpoints.
- IDs: `ShipType.CD273=308`, `ShipAbilityId.CD273PowerToEngines=37`; no existing values renumbered.
- User-selected Power to Engines: speed ×`1.35`, other modifiers ×`1`. Provisional existing timing: `20 s` active, `50 s` recovery after expiry.
- Provisional gameplay: heavy corvette; level `2`, population `2`, limit `3`, length `60`, navigation radius `33`, turn speed/acceleration `45`, banking `±15°`; shields regenerate `11.3333` every `3 s`; no hangar or passive hull regeneration.
- Visible size `10.9375 × 8.6903 × 60`; complete bank range `−4.513483 .. 4.513483`; root scale `1`, bow `+Z`, up `+Y`.

## Implementation

- Isolated Blender `3.6.23`, MCP protocol `13`, port `9896`, telemetry disabled. Shared Blender work preserved.
- Material slots reconstructed from binary shader assignments; original source files unchanged. FBX/blend retain every mesh, UV, bone and authored hidden state.
- Four medium lasers → `HP_LC_01..04`; missiles → `HP_ML_01_FP_01`, `HP_ML_02_FP_01`; point defense → `HP_LC_05..06`; engine → `HP_Engines`.
- Shield-generator location derived on the upper rear hull; source has no matching generator socket.
- Saved prefabs exclude hidden collision/shield/shadow helpers. Model/gameplay/placement retain nine visible meshes; wreck retains five opaque meshes.
- Normal `_B` maps: linear NormalMap import + green-channel flip. Hull red trim: HSV hue `0`, range `.07`, minimum saturation `.3`, strength `1`; team rim `0` preserves neutral panels.
- Source `MeshShield` engine glow → static additive approximation with source color. Original alpha is not a useful paint mask.
- Registered Rebellion roster, ShipsData, AssetMappingData, existing Addressables groups, ShipUiData, TooltipIconData, ReinforcementData, icon generator, wreck data and ability/audio. Weapon family audio reused.

## Decision

- Chosen: Power to Engines, explicitly selected by the user; existing BoostEnginePower behavior with ship-specific settings.
- Why: requested alternative needs no new combat implementation. No inherited fire-rate or shield-regeneration penalties.
- Avoid: importing Remake source balance over the requested Rebel units values; enabling hidden helpers; modifying source ALO/DDS files.

## Edge Cases

- Source `IDLE_00` / `DIE_00` ALA animations are present but unconverted. Wreck is derived from the live opaque hull (`59.48125` length, excluding engine effects).
- Unsupported EaW animated/refraction/particle shaders are not recreated.
- No automated Unity tests or Play Mode run. Runtime firing arcs, ability timing, system destruction, fog/selection, reinforcement and death/wreck behavior remain unverified.

## Verification

- Geometry: `15` meshes, `25,512` imported triangles, `107` bones; all source visible material-slot triangle counts match.
- Blender FBX round trip: max geometry error `0.00007205`, bone error `0.00003514` source units; hierarchy and UV checks pass.
- Unity attachment error `0.0000007261` units before gameplay scaling; four saved prefabs, targetable mounts, normals, registrations and object references pass `Inspect.cs`.
- `Package.py`: `13` source hashes unchanged, `50` assets with metadata, transparent `512 × 512` icon; alpha bounds `(74,154)..(417,333)`.
- Eight live and wreck palettes rendered and checked; blue/green inspected. Placement and Blender source previews inspected. Console clean at final render check; unrelated transient compilation errors occurred during concurrent imports.
