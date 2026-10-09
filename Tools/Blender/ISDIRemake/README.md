# ISD I Remake model replacement

## Decision

- Empire `ShipType.ISDI = 201` keeps its existing data/view/preview/icon GUIDs and registrations.
- Source: Workshop `1770851727`, `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data`.
- Unit inheritance: `Star_Destroyer → Star_Destroyer_Upkeep → Star_Destroyer_Template → Capital_Base → Vessel_Base → SFX_Base`.
- User choices, 2026-10-09: retain current balance and Interceptor/Brute/Punisher bays. Source weapon layout is rebuilt with existing project weapon profiles; no shared weapon or ability definitions were changed by this replacement.
- Original AOTR visual folders moved to `ISDIObsoleteAOTR` under models, materials, textures and wreck materials. All 63 assets/folders/snapshots have `Obsolete` labels. Original 56 asset/folder GUIDs retained; archived prefab snapshots reference archived visuals, shield and icons.

## Model and materials

- Live assembly: `Empire_Imperial_SD.ALO` + `Empire_Imperial_SD_1.alo` + 13 weapon attachments. `Evidence/SourceAudit.json` records every XML hardpoint, attachment bone, shader parameter and source hash.
- Death assembly: `Empire_Imperial_SD_DeathClone.ALO` + ISD I structure, matching the death unit's `HP_ISD_New_ISD1` attachment. Its source prefab is `Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab`.
- Blender 3.6.23 / ALAMO / MCP safe mode; isolated port 9886 leaves the shared 9876 scene untouched. Sixteen editable conversions preserve rig hierarchy, geometry, UVs and packed textures. Original shader parameters are stored as `EaWShaderParameters` on Blender materials.
- Binary material slots repaired independently before FBX export, including different shaders sharing one texture. Identity `Root` restored only where verified missing. Mesh-to-bone parenting preserves source world transforms.
- Hidden collision/shadow meshes remain in conversion files. Upstream ALAMO `removeShadowDoubles` welds only collision/shadow shader meshes; binary visible triangle counts are unchanged. Helper count differences are recorded in `Evidence/HelperGeometryAudit.json`.
- Opaque surfaces use Ship Lit; RGB diffuse tints retained. Source Phong parameters remain in metadata; project metallic/smoothness `0.1/0.25` approximate its BRDF. Normal maps are linear with flipped green channel. Engine/window effects use additive URP materials and separate RGB-derived coverage; original RGBA PNG copies remain unchanged. Auxiliary wave/distortion textures and original parameters are retained.
- Team paint: four mirrored foredeck stripes per side, clipped across 10 source-material surfaces / 905 derived triangles. Source UVs/albedo remain on each overlay; original hull meshes are unchanged. Ownership, fog, explosion and wreck bindings include the overlays. Eight live and wreck palettes verified.
- FBX import `0.02`; visual geometry scale `10.9126444`, offset `(0, -12.8066845, -1.4443506)`. Nested attachment scale `0.01`, X rotation `90°` cancels FBX transport. Bow `+Z`, up `+Y`, prefab roots `1`.

## Gameplay mappings

| Source group | Count | Existing profile | Source muzzle |
| --- | ---: | --- | --- |
| HICT | 2 | HeavyTurboIon `31` | Parts 01/02 `FP_01` |
| HTLT | 6 | HeavyTurboLaser `2` | Parts 03–08 `FP_02` |
| LC | 6 | Laser `8` | `IC_01..06_FP_01` |
| TL | 5 | LightTurbolaser `33` | `TL_01..05_FP_01` |
| Center TL | 3 | MediumTurboLaser `4` (3-burst) | Parts 09–11 `FP_03` |
| QIC | 2 | MediumTurboIon `32` | Parts 12/13 `FP_01` |

- IDs `0..23`: all 24 weapons targetable. IDs `24..29`: two shields, two engines, tractor and hangar. Matching health entries: weapons 750; shields/engines 1,000; tractor 1,500; hangar 2,000. Dummy art/facing projectile do not become weapons.
- Source adds two laser mounts and one light turbolaser compared with the former 21-weapon integration. Existing profile damage, burst behavior and full-yaw arcs remain project mappings; source projectile/pulse/cone definitions are retained in the audit, rather than changing shared balance.
- Source single shield-generator bone `HP_SG` maps to the retained two-generator setup, offset ±6% of hull width. Outer engine-glow geometry centers map to the retained left/right engine targets; all source engine art remains visible. Tractor uses `HP_TRAC_BONE_00`; hangar uses `SPAWN_00`, with launch 8 units below the opaque hull.
- Source Full Salvo: 15 s / 60 s recharge. Existing engine boost `21` and tractor `22` retained for current balance. Source upkeep, unsupported landing shuttle and source fighter garrison are not added. Tractor uses the live shared settings; this replacement does not overwrite them.

| Value | Remake XML | Retained project |
| --- | ---: | ---: |
| Hull | 40,000 | 4,500 |
| Shields | 15,000 | 4,000 |
| Shield regeneration/s | 300 | 6.6666665 |
| Speed | 3 EaW units | 15 project units |
| Cost | 18,000 | 4,500 |
| Build time | 70 s | 40 s |
| Population | 20 | 5 |

- Level 3 / max count 3; navigation radius `97.998657`; bank ±5°. Opaque hull/collider/placement: `94.26325 × 49.6460953 × 167.997711` project units. Banked Y range `−24.82305 .. 25.089714`.
- Bays: Interceptor `203`, Brute `204`, Punisher `205`; each reserve 2 / active 1. Existing initial/shared delays `4/30 s`; fighter assets and balance unchanged.
- Faction description updated for 24 targetable weapons. Ship/data maps, existing Addressable entries, roster/matchups, HUD/tooltip icon, reinforcement component, icon-generator entry, weapon/ability dependencies and AI loadout verified.

## Verification

- Blender position/UV-corner matching tolerance: `0.001` source units / `0.00001` UV units. Maximum measured geometry error `0.000158`; bone error `0.000099` source units.
- Unity imports: original converted mesh/triangle counts, all UVs/material slots and bone hierarchy verified across 16 FBXs. Maximum bone displacement `0.000002346` project units; gameplay muzzle displacement `0`.
- 56 original source hashes unchanged; 100 decoded texture-reference copies pixel-identical. Original archive FBX/PNG files unchanged; original GUIDs retained.
- Saved active and archived prefabs: no missing scripts/broken references. Active ISD I visual dependencies contain zero `Obsolete` assets. Helper stripping removed 55 source helpers / 67,130 triangles from unit prefabs; gameplay shield retained.
- Final saved verification and ephemeral script compilation passed; Editor compilation-failed flag false, imports idle. Historical Console errors from concurrent ISD II / Balance work were not cleared or modified.
- Transparent 512×512 icon/silhouette, placement, top/stern and all eight live/wreck palettes saved in `Previews/`. This task did not run automated tests or enter Play Mode, per ALO guide.
- Runtime combat acceptance remains unrun: firing/arcs, launches, abilities, shields, fog/selection and death timing. Source ALA animations and animated EaW wave/refraction/proxy effects are not recreated; existing project effects and static source art are used.

## Files and rebuild

- Active art: `Assets/Art/{Models,Materials/Models,Textures/Models}/EmpireShips/ISDI/`; wreck materials `Assets/Art/Materials/Wrecks/ISDI/`.
- Active prefabs: `ISDI.prefab`, `ISDIShipView.prefab`, `ISDIWreckView.prefab`, `ISDIReinforcementView.prefab`. Existing settings remain under `Assets/Settings/Data/Ship/`.
- Editable package: `output/remake-empire-units/ISDI-Converted/` (16 packed `.blend`/FBX sets, textures and reports). Checked evidence: `Evidence/`; working conversion data: ignored `Temp/ISDIRemakeImport/`.
- `Prepare.py` audits the installed source; `Convert.py` uses the isolated Blender MCP; `PreserveParameters.py` keeps auxiliary maps and parameters; `Stage.py` stages art. `Baseline.cs`/`Archive.cs` were the one-time migration steps; keep the captured baseline.
- Unity rebuild order: `BuildArt.cs`, `ApplySourceTints.cs`, `BuildStripes.cs`, `BuildShip.cs`, `BuildPreview.cs`, `Finalize.cs`, `Render.cs`, `Verify.cs`. Run with `unity command run_script --file <file> --timeout_ms 120000 --timeout 150 --json`; inspect nested `result.success`.
- Restore `Baseline.json` and `GameplayMapping.json` from `Evidence/` when rebuilding temporary data. Finish with `uv run --with pillow python Tools/Blender/ISDIRemake/VerifySource.py`.
