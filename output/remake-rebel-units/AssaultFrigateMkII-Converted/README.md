# Assault Frigate Mk II

- Art: Remake 4.0, Workshop `1770851727`; `Data/Art/Models/Rebel_Assault_Frigate_MK2.alo` plus all 14 XML-referenced `Rebel_Assault_Frigate_MK2_QLC_*.ALO` attachments. Textures resolved under `Data/Art/Textures`; supplied `Texturesc` path does not exist.
- Balance: vault `GameDesign/Rebel units`, Assault Frigate Mk II section. This reference defines project balance, not Remake source balance. Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE`.
- Reference values: hull `3500`, shields `3000`, speed `31 units/s`, price `3200 credits`, build `40 s`. No hangar or fighter substitution.
- Gameplay: 4 medium turbolasers (`4`), 2 light quad turbolasers (`82`), 4 heavy laser cannons (`28`), 2 point-defense lasers (`14`), 1 engine, 1 shield generator. All 14 hardpoints are targetable and have health entries.
- Power to Shields (`45`): shield regeneration x1.6, damage/speed x0.8, active `8 s`, recovery after expiry `35 s`. Own managed settings object; other ships' shield settings remain unchanged.
- Light Quad Turbolaser (`82`): existing Light Dual Turbolaser family/caliber/pattern, quad mount and standard range. Uses existing family projectile, faction color and audio rules.
- Provisional choices: Rebellion level `3`, population `3`, limit `10`, length `100` units, navigation radius `55`, ±5-degree bank. Hull bounds `69.5967 x 52.0999 x 100`; banked vertical envelope `-28.9115 .. 26.0566`.
- Provisional regeneration `20 shields / 3 s`; hardpoint health weapons `300`, engine/shield `600`; inherited movement acceleration/turn speed `6`. Matchups: strong corvettes/frigates, weak heavy capitals. Unsupported values are project choices.
- Mount mapping: source `LC_Quad_01..04` → medium turbolasers; `05..06` → light quad turbolasers; `07..10` → heavy lasers; `11..12` → point defense. Muzzles use each separate model's `FP_01`; source `Engines` and `Shield_Generator_01` supply systems. The other two turrets and authored bow battery remain visual geometry rather than extra gameplay weapons.
- Bone transforms preserve original attachment positions. Turrets sit beside the imported hull rig at XML socket positions with their authored geometry orientation; FBX bone scale/basis are not applied twice.
- Conversion: Blender `3.6.23`, protocol `13`, ALAMO importer. Material slots rebuilt from source shader/parameters. Original ALO/DDS/XML SHA-256 hashes verified unchanged. Helpers remain in FBX/source scenes and are stripped from game prefabs.
- Saved geometry: `15 FBX`, `63 meshes`, `61078 triangles`, `163 bones`. Maximum Blender geometry error `0.0000381` source units; Unity bone error `0.000000547` units. Unity UVs, source material assignments, visible per-slot triangles and all separate turret sockets verified.
- Editable `.blend` is a multi-scene package: hull source, individual turret source scenes and FBX validation scenes. It retains pre-existing Blender scenes to preserve the shared session. Select `AssaultFrigateMkII Source` for the hull; corresponding `Part00..13 Source` scenes contain turret art.
- Assets: type-first `Assets/Art/{Models,Materials,Textures}/RebellionShips/AssaultFrigateMkII`, visual/gameplay/wreck/placement prefabs, ship/wreck data, transparent `512 x 512` icon, silhouette and matchup data.
- Registrations: `ShipType=309`, `ShipsData`, `AssetMappingData`, existing View/Data Addressables groups, Rebellion roster, HUD and tooltip icons, reinforcement preview, ability/catalog/audio and future icon generator mapping.
- Verification: saved prefab roots at unit scale, no missing scripts/broken serialized references, 12 weapons in health/fog lists, no hangar bays, exact loadout/stats/ability, all eight live and wreck palettes, placement render and transparent icon crop.
- No automated Unity tests or Play Mode run. Combat arcs, shields, ability application, fog/selection, placement and wreck lifecycle still require runtime verification. ALA animations and EaW animated shield/refraction/proxy effects are not ported; engine glow uses a static additive material.

## Commands

1. `uv run --with pillow python Tools/Blender/AssaultFrigateMkII/Prepare.py`
2. `python Tools/Blender/AssaultFrigateMkII/Convert.py` — fresh source scenes; do not rerun into an existing import with conflicting Blender names.
3. `python Tools/Blender/AssaultFrigateMkII/Stage.py`
4. `unity command run_script --file Tools/Blender/AssaultFrigateMkII/BuildArt.cs --timeout_ms 60000 --json`
5. Run `BuildGameplay.cs`, `Persist.cs`, `Render.cs`, `Register.cs`, `Inspect.cs` in order with the same Unity command; inspect nested `result.success`.
6. `uv run --with pillow python Tools/Blender/AssaultFrigateMkII/Package.py`

- Evidence/package: `output/remake-rebel-units/AssaultFrigateMkII-Converted/`; temporary working files: `Temp/AssaultFrigateMkIIImport/`.
