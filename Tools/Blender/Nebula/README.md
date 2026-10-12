# Nebula Star Destroyer

## Decision

- Rebellion `ShipType.Nebula = 330`; `NebulaOverchargeWeapons = 38`.
- Source: Remake Workshop `1770851727`, `Data/Art/Models/Rebel_Nebula_Star_Destroyer_Prototype.ALO` and 15 XML-attached turret ALOs.
- Source textures: `Data/Art/Textures/`; original ALO/DDS files remain unchanged.
- Gameplay reference: vault `GameDesign/Rebel units.md`, Nebula section; these project stats replace Remake XML balance.
- User-approved squadron: X-Wing `300`.

## Gameplay

- Hull `16,000`; shields `15,000`; speed `20`; cost `7,200`; build `72 s`.
- Targetable hardpoints: `24` weapons + `2` engines + `2` shield generators + `2` hangars + `1` tractor subsystem = `31`.
- Weapons: `8` dual heavy turbolasers / profile `3`; `6` medium turbolasers / `4`; `4` medium ions / `32`; `2` assault concussion launchers mapped to existing heavy missile profile `65`; `4` point defense lasers / `14`.
- Heavy batteries: two gameplay mounts per source `NebulaPart01..04/FP_01`, offset ±`0.65` project units. Medium batteries: parts `09..14`; ions: parts `05..08`.
- Missile mounts: `MUZZLEA_01/02`; point defense: `MUZZLEA_03..06`; tractor target: `MUZZLEA_00`. Source central heavy-ion art remains visible; it adds no extra gameplay weapon.
- Paired shields/hangars use `Shield_Gen` / `Hanger` with derived lateral offsets; engine targets use stern bounds. `Evidence/HardpointMapping.json` records positions and derivation.
- Two X-Wing bays: total budgets `3/2`, simultaneous limits `2/1`. Initial launches consume the budget → `3` active squadrons + `2` replacements total.
- Launches sit `8` units below the hull. Destroying a hangar disables its own bay.
- Overcharge Weapons: fire delay ×`0.8` = firing rate +`25%`; shield regeneration ×`0`; duration `10 s`; recovery `50 s` after expiration. Uses existing `BoostWeaponPowerSettings` and ability implementation.
- Provisional project choices: length `160`, level `5`, population `8`, maximum count `3`, regeneration `25/s` after `3 s`, weapon yaw ±`85°`, hangar delays `4/8 s`, subsystem health `1,000–1,500`, weapon health `750`.
- Final visible bounds `83.07693 × 28.63188 × 160`; navigation radius `98.14124`; bank ±`5°`, hull Y range `−17.10768 .. 14.32681`.
- Tractor is a targetable subsystem; the reference ability list contains only Overcharge Weapons. This import does not add a separate tractor-beam activation.

## Art

- Blender `3.6.23`; isolated safe-mode MCP port `9886` preserves shared Blender scene on `9876`.
- Original binary shader/material slots repaired before export. Editable blends and FBXs retain authored helpers and original UVs.
- FBX import scale `0.02`; visual geometry scale `16.8175144`; attachment local scale `0.01`, X rotation `90°`. Gameplay and preview roots remain at unit scale; bow `+Z`, up `+Y`.
- Original source hidden flags remove collision/shadow/shield helper renderers from unit art; gameplay shield is separate and retained.
- Opaque materials use Ship Lit. Hull primary/secondary and their turret textures recolor red livery by hue `0`, range `0.07`, minimum saturation `0.3`; engine materials remain neutral.
- Additive lights and engine shader surfaces use URP particle materials. Original RGBA textures remain lossless; separate RGB-derived coverage copies support project effects.
- Original wave/distortion texture copies and binary shader parameters are in the evidence/package. Animated EaW shader behavior and ALA animations are not recreated.
- Geometry-only visual prefab supplies the dedicated hologram preview, wreck and transparent `512×512` icon/silhouette.

## Verification

- `16` converted FBXs; `56` source meshes; `37,957` triangles; `155` bones across the conversion set.
- Blender geometry/UV-corner tolerance `0.001` source units / `0.00001` UV units; maximum geometry error `0.000089055`; bone error `0.000066621`.
- Saved Unity verification: `8,073` checks passed; maximum bone transport error `0.000000728` project units; compilation-failed flag `false`; Nebula ability settings are independent of the donor.
- Checked hardpoint counts/colliders/health, hangar bindings, geometry/UVs/material remaps, missing scripts/references, preview, roster/icons, asset maps and existing Addressable groups.
- `37` original source hashes unchanged; `47` texture reference copies pixel-identical to source in conversion and Unity staging.
- All eight team palettes rendered; blue/green, placement and wreck images visually inspected. Icon alpha bounds `(57,143)..(426,346)` fit within `512×512`.
- No Unity automated tests or Play Mode run, per import guide. Combat, launches, reinforcement placement, ability timing and wreck lifecycle remain runtime acceptance checks.

## Files

- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/Nebula/`.
- Prefabs: `Assets/Prefabs/Models/Ships/Rebellion/Nebula.prefab`, `NebulaShipView.prefab`; dedicated wreck and reinforcement prefabs in their existing folders.
- Data: `Assets/Settings/Data/Ship/Rebellion/NebulaShipData.asset`, `Ship/Wreck/NebulaWreckData.asset`.
- Evidence: `Tools/Blender/Nebula/Evidence/`; review images: `Previews/`.
- Editable conversion package: `output/remake-rebel-units/Nebula-Converted/`.

## Implementation

1. `uv run --with pillow python Tools/Blender/Nebula/Prepare.py`.
2. Start the isolated Blender with `Tools/Blender/ISDIRemake/Start.py`; run `uv run python Tools/Blender/Nebula/Convert.py`.
3. `uv run --with pillow python Tools/Blender/Nebula/Stage.py`; `uv run python Tools/Blender/Nebula/GenerateBuilders.py`.
4. Run `BuildArt.cs` → `BuildGameplay.cs` → `Persist.cs` → `Render.cs` → `Register.cs` → `Verify.cs` using `unity command run_script --file Tools/Blender/Nebula/<file> --timeout_ms 120000 --timeout 150 --json`; inspect nested `result.success`.
5. `uv run --with pillow python Tools/Blender/Nebula/Preserve.py` verifies source fidelity and copies evidence/editable outputs before Unity clears `Temp`.
