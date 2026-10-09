> ISD I visuals were superseded on 2026-10-09 by Workshop 1770851727. Current integration: [ISDIRemake/README.md](../ISDIRemake/README.md). This legacy workflow remains historical/fighter reference; its ISD I builders and shared stripe builder must not be run over the replacement.

# ISD I

- Imported 2026-10-06 after reading `ALO_MODEL_IMPORT_GUIDE`, `PROJECT_ORGANIZATION` and `UI_UX_GUIDELINES` through `empire-vault`.
- `Temp/ALO_MODEL_MAP.txt` resolves `E_Imperial_Star_Destroyer_1_Fighters` → inherited `T_Imperial_Star_Destroyer_1` → `Empire_Imperial_SD.ALO`. Standard and advanced loadouts share the hull.
- Source: AOTR workshop `1397421866`, `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/`. Source XML: `SpaceUnitsCapital.xml`, `SpaceUnitsFighters.xml`, squadron definitions and `Hardpoints_Empire_Space.xml`.
- All 36 audited ALO/DDS source hashes remain unchanged. Lossless RGBA PNGs match decoded DDS pixels; separate additive textures derive coverage from RGB brightness.

## Conversion

1. Run `Tools/Blender/prepare_isd_i.py` with Pillow from the repository root.
2. Use an isolated Blender 3.6.23 instance with ALAMO. ALAMO's global shadow cleanup can modify other scenes; importing into a separate scene in a shared process is insufficient.
3. For each of the 16 `MODELS` variants, prepend `VARIANT`, `SOURCE`, `AUDIT` from `BinaryAudit.json`, and `OUTPUT` to `Tools/Blender/export_isd_i.py`. Execute through Blender MCP, retain its `REPORT_JSON` as `<VARIANT>/ConversionReport.json`.
4. Run `StageArt.py`, then `FixAdditive.py`. Packed sources, FBXs, textures, conversion reports and previews live in ignored `Temp/ISDIImport/`.
5. Run these model-specific builders using `unity command run_script --file Tools/Blender/ISDI/<file> --json`: `BuildArt.cs`, `FixAdditive.cs`, `BuildShip.cs`, `BuildFighters.cs`, `BuildPreviews.cs`, `Register.cs`.
6. Run `Inspect.cs` to inspect saved assets without Play Mode. It writes `Temp/ISDIImport/UnityInspection.json`.

## Team Colors

- ISD I and ISD II share four mirrored foredeck stripes per side; the hull and turrets remain neutral. Tiled hull UVs require fitted geometry instead of a repeating texture mask.
- `ISDI_TeamStripes.asset`: 540 vertices / 250 triangles, width 2.6 units, surface offset 0.015 units. Original hull albedo, UVs and normal map are preserved; stripe mask strength 1, hull mask/rim strengths 0.
- The stripe renderer is under `BankingBody` and explicitly bound to ownership, fog and explosion rendering. Both wrecks include the matching renderer/filter pair and wreck material. Team bindings: ISD I 90, ISD II 100; wreck pairs 40/43.
- After rebuilding either ship, run `unity command run_script --file Tools/Blender/ISDI/BuildImperialTeamStripes.cs --entry BuildImperialTeamStripes.Main --json` after both gameplay/wreck prefabs exist. Repeated runs preserve asset GUIDs and avoid duplicate bindings.
- 2026-10-06: red/blue top and angled ship/wreck renders inspected; all four saved prefab checks passed; no import or serialization errors. Evidence: `Temp/ImperialTeamColorFix/Verification.json`, `After/`, `WreckAfter/`. No combat or automated Unity test run for this fix.

- Hull: 26 meshes / 67,925 triangles / 245 bones. ISD-I parts: 8 / 5,024 / 11.
- Six heavy turrets: each 5 / 1,137 / 9. Two ion turrets: each 6 / 1,206 / 10. Three triple turrets: each 4 / 1,447 / 6.
- Interceptor: 4 / 2,850 / 14; Brute: 5 / 3,015 / 9; Punisher: 6 / 6,172 / 25.
- Blender round trips preserve geometry, UV corners, bone names and parents; maximum geometry displacement ≤0.000224 source units. Unity preserves all mesh/bone counts and UV presence; maximum bone displacement 0.000001986 project units.
- Unity removes 14 exactly zero-area Punisher hull triangles; final Punisher total is 6,158. Verified against the packed source; visible geometry is unaffected.
- FBX scale 0.02, hierarchy preserved, no animations. Restore only the binary-verified identity root. Static copies of visible skinned meshes support the existing mesh-renderer team/fog paths; disabled original skins and helpers retain the source rig.
- Apply all material remaps together after texture reimports. Texture reimports can reload a partially edited model importer.
- Nested turret FBXs cancel their 100× / −90° X armature transport basis before attachment. Hull keeps its imported basis; fighters rotate 180° around Y. Final bow +Z, up +Y, root scale 1.

## Gameplay

- `ShipType.ISDI = 201`; hull/shields/speed = **20,000 / 16,000 / 250**, exactly as requested. AOTR displays speed 250 but stores `Max_Speed=2.5`; this project stores the user's literal 250 units/s.
- Visible hull/effects bounds: 102.61 × 68.56 × 180. Opaque collider, placement and wreck bounds: 100.89 × 53.10 × 180. Navigation radius 105; bank ±5°; banked hull Y range −18.816692 .. 34.578710.
- Targetable IDs 0..15; health bindings 16. All IDs 0..26 are unique; weapon bindings 21, fog hardpoint bindings 27.

| IDs | Count/type | Source position |
| --- | --- | --- |
| 0..5 | 6 heavy 2-burst turbolasers | TLD01..06 `FP_02` |
| 6..7 | 2 heavy 2-burst turbo-ion cannons | ICD01..02 `FP_02` |
| 8..9 | 2 medium turbo-ion cannons | `IC_03_FP_01`, `IC_04_FP_01` |
| 10..11 | 2 shield generators | `HP_Shield_Fire_01`, `HP_Shield_Fire_02` |
| 12..13 | 2 engines | `HP_Engine_L`, `HP_Engine_R` |
| 14 | Tractor beam | `HP_TRAC_BONE_00` |
| 15 | Hangar | `SPAWN_00` |
| 16..18 | 3 non-targetable medium 3-burst turbolasers | TLT01..03 `FP_01` |
| 19..22 | 4 non-targetable light turbolasers | `TL_01..04_FP_01` |
| 23..26 | 4 non-targetable laser cannons | `IC_01/02/05/06_FP_01` |

- Source has three engine hardpoints; only the outer two are targetable to match the request. All source engine geometry remains visible.
- Weapon/system HP: weapons 750, engines/shields 1,000, hangar 2,000, tractor 1,500; shield regeneration 20/s. Shields fail after **both** generators are destroyed; existing single-generator ships retain their behavior.
- `ImperialBoostEnginePower=21`: speed ×2, weapon delay ×4, regeneration ×0 for 20 s; 50 s recovery. Multipliers/timing come from the source ability.
- `TractorBeam=22`: target speed ×0.4; 25 s recovery. Live beam hardpoint required; ends on hardpoint/caster/target destruction, target removal, caster ion stun or leaving range. Uses a dedicated modifier, removed on stop, and the existing beam visual.
- `HeavyTurboIon=31` reuses the heavy ion damage profile with 2 shots; `MediumTurboIon=32` reuses medium ion with 1 shot; `LightTurbolaser=33` uses 1 shot / 10 damage. Existing heavy/medium turbolasers and laser profile are reused.
- Advanced bays: Interceptor 203, Brute 204, Punisher 205; each 2 total launches / 1 active squadron. Initial/shared launch delay 4/30 s. Exit is 8 units below the hull collider at (0.51, −26.82, −2.82).
- Squadrons contain 8 Interceptors, 6 Brutes or 4 Punishers. Per-member hull/shields: 15/0, 50/0, 55/30; Punisher regeneration 0.15/s. Interceptors have 4 lasers, Brutes 2 heavy lasers, Punishers 1 laser + 2 torpedo + 2 missile launchers. Warheads fire once per attack run.
- Faction cost/build/level/population: ship 22,000 / 440 s / 3 / 8; Interceptor 525 / 18 s / 1 / 1; Brute 600 / 7 s / 1 / 1; Punisher 1,500 / 50 s / 1 / 1. Values follow source campaign definitions.
- Provisional project choices: ship limit 3; fighter limit 10; size, turn/range/fire arcs, fighter movement tuning, light turbolaser damage, 20 s tractor duration and 150-unit tractor range. Missile launchers reuse the project's concussion-missile profile.

## Verification

- 13 saved prefabs reload with no missing scripts, broken references or embedded FBX materials. Gameplay views contain the requested models; placement excludes source additive planes and uses the shared hologram material.
- Empire roster, `ShipsData`, data/view asset mappings and existing Addressables groups, HUD/tooltip icons, reinforcement component mappings, matchups, abilities, audio and future ship icon generation are registered.
- Opaque hull/wreck retain source surface textures, with HSV livery disabled and normal green flipped. Foredeck stripe geometry provides localized team color as described above. The original import rendered all eight palettes; blue and green inspected for every model. Own 512×512 transparent icons/silhouettes, hologram and fresh-wreck previews are uncropped.
- Production scripts compiled and imports/persistence checks succeeded. Final Console error count is 0; `MainMenuScene` stayed clean.
- No automated tests or Play Mode combat verification were run. Combat arcs, hangar replacement launches, tractor/boost lifecycle, shields, fog/selection, reinforcement and death/wreck behavior still need runtime acceptance. Balance is unverified; source animations and animated shader effects were not recreated.
