# Blender / Rothana conversion

## Mon Calamari Star Cruiser / MC80 — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference: `GameDesign/Mon Calamari Cruiser Import.md`. Vanilla `output/eaw-rebel-ships/DATA/ART/MODELS/RV_MONCALCRUISER.ALO`, damaged `_D.ALO` and six `HP00_*` pod variants; original ALO/DDS hashes unchanged.
- Stage textures with `uv run --with pillow python Tools/Blender/prepare_mon_cal_cruiser_textures.py`. Import each variant into its own `<VARIANT> Source` scene in Blender 3.6.23; run `export_mon_cal_cruiser.py` through MCP with its matching `VARIANT`. Complete ALAMO helper cleanup only on the new scene. No root repair: main/damaged/pods already retain 71/79/77 bones.
- Eight exported models preserve 47 meshes and all source triangle corners/UVs/parents. Main/damaged triangles 7,290/4,177; pod totals F-L/F-R/M-L/M-R/B-L/B-R 52/58/93/130/221/272. Blender max geometry error 0.000070222 source units; Unity corner/bone error ≤0.000002416/0.000001527 project units. Eight DDS→PNG conversions are pixel-identical.
- Type-first `RebellionShips/MonCalCruiser` art folders; own visual/gameplay/placement/damaged wreck assets. Attach pod roots to the model using the main `HP00_*` local positions; do not inherit scaled FBX bone transforms. Centered size 86.52927 × 23.62375 × 160 units, bow +Z/up +Y, gameplay roots scale 1. Hull-alpha inverse team mask; all eight living/wreck palettes rendered, blue/green inspected. Transparent actual-model icon/silhouette 512 × 512, uncropped.
- Rebel `MonCalCruiser = 302`: latest user-approved hull/shields/regen 8,500/2,500/70 per second; speed/turn/acceleration 15/6/6; cost/build/tech/population 4,500/30 s/5/4. Four `HeavyTurboLaser`, four `DualHeavyTurboLaser`, two `TurboLaser`, two `IonCannon` and engines: 13 IDs, no shield generator or hangar. Added mount names: `Turbolaser_FL/FR`, `DualHeavyTurbolaser_ML/MR/BL2/BR2`. All weapon/health/fog bindings bank together. Length 160, radar 500, bank 5°, hardpoint health 400 and queue limit 10 remain project choices.
- Dedicated `MonCalCruiserPowerToShields = 15`: active 15 s, recovery 40 s, speed ×0.8, damage ×0.5 and regen ×10 → 700/s. Registrations and ability audio saved; shared boost tuning unchanged.
- Damaged hull/shadow use scale-compensated `BakeMesh(..., true)` and fresh static vertex/normal/tangent/UV/index arrays. Update existing mesh arrays directly to refresh GPU data while retaining GUIDs. Source girder cutouts use opt-in `_ALPHATEST_ON` in all wreck shader passes; opaque materials retain the existing variant.
- `Temp/MonCalCruiserImport/` holds packed blends, exports, scripts, reports and previews. Original integration scripts retain initial tuning; saved assets include later fixes. Saved references/import/C#/shader inspections pass. No automated tests or Play Mode run; runtime/balance acceptance remains unverified. Source ALA/shader/proxy animation is outside this static conversion.
- Unity compilation is up to date with zero errors at commit preparation; the concurrent `TeamColorView` error reported during import is resolved. This import did not modify `TeamColorView`.

## Corellian Corvette / CR90 — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference: `GameDesign/Corellian Corvette Import.md`. Vanilla `output/eaw-rebel-ships/DATA/ART/MODELS/RV_CORVETTE.ALO`; dedicated wreck `RV_CORVETTE_D.ALO`. Original ALO/DDS hashes unchanged.
- Stage textures with `uv run --with pillow python Tools/Blender/prepare_corellian_corvette_textures.py`. Import into `CorellianCorvette Source` in Blender 3.6.23, then run `export_corellian_corvette.py` once through MCP; set `VARIANT = 'CorellianCorvetteWreck'` for the damaged source scene. Complete any ALAMO global-helper cleanup failure only on the new scene; preserve other scenes and the installed add-on. Restore only the binary-verified identity Root if absent.
- Intact/damaged imports retain `13/3` meshes, `3,684/2,446` triangles and `17/12` bones. Blender maximum corner error `0.000110729/0.0000133514` source units; Unity ≤`0.000000475` project units, correct bone parents/UVs. Helpers and authored hidden engine effects remain disabled; source shaders/ALA/proxy animations are outside static conversion.
- Type-first `RebellionShips/CorellianCorvette` art folders; own visual, gameplay, placement and damaged wreck prefabs. Centered living size `12.72498 × 10.07087 × 30` units; bow +Z/up +Y, roots scale 1. Hull-alpha inverse supplies the team mask. All eight living/wreck colors rendered, blue/green inspected; transparent 512 × 512 icon/silhouette uncropped.
- Rebel `ShipType.CorellianCorvette = 301`; hull/shields/regen `750/600/15` per second; cost/build/level/population `1250/15 s/2/2`. Eight shared Laser mounts use `MuzzleA_00..07`; no destructible systems or hangar. User-approved movement scale: `24.5 units/s`, yaw `12.5°/s`. Length 30, acceleration 24, range 175, bank 15°, height Y 80 and queue limit 10 remain project choices.
- Dedicated `CorellianCorvettePowerToEngines = 14` reuses the existing engine ability: active 20 s, recovery 50 s, speed ×2, weapon delay ×3, shield regen ×0. Shared boost definitions remain unchanged. Roster/data/view/Addressables/icon/tooltip/placement/ability audio registrations saved and reloaded.
- `Temp/CorellianCorvetteImport/` contains editable blends, FBX/PNGs, integration scripts, saved-reference/geometry reports and renders. Asset/compile/import/Console checks passed; zero missing scripts, broken references or donor dependencies. No automated tests or Play Mode run; runtime acceptance and provisional balance remain unverified.

## IPV-2C Stealth Corvette — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/Stealth Corvette Import.md`; acceptance plan `TODOs/Features/StealthCorvette_Import.md`.
- Source: RaW `ReV_Stealthship.ALO`; mesh/textures/rigging Warbnull. Original ALO and both DDS hashes unchanged. Run `uv run --with pillow python Tools/Blender/prepare_stealth_corvette_textures.py`, import into a fresh Blender 3.6.23 `StealthCorvette Source` scene, then run `export_stealth_corvette.py` once through MCP. Restore only the binary-verified identity Root: 14 imported → 15 bones.
- Four meshes / 15,184 triangles retained through FBX and Unity. Blender maximum geometry/bone error 0.000014949/0.000005245 source units; Unity geometry/bone error ≤0.000000225/0.000000090 project units; parents match. Visible hull, effect shell and Collision UVs verified. Unity remaps disabled Shadow UVs by ≤0.000782482; packed blend/FBX preserves original corners.
- Both DDS→PNG conversions are pixel-identical, 1024 × 1024. Source hull alpha supplies the linear team mask (3.6116% coverage); living and wreck ownership/material bindings verified. All eight living/wreck palettes rendered; blue/green inspected. Actual-model transparent 512 × 512 icon/silhouette are uncropped.
- Type-first `RepublicShips/StealthCorvette` art folders; visual/gameplay `StealthCorvette.prefab` / `StealthCorvetteShipView.prefab`, dedicated reinforcement preview and wreck. Centered hull `1.863283 × 2.194350 × 20` units; bow +Z/up +Y, roots scale 1, radius 11, banked Y −1.117181..1.117181.
- Republic `ShipType.StealthCorvette = 11`; hull/shields/regen `850/900/15` per second; cost/build/tech/population/limit `2150/18 s/3/2/1`. Two concussion launchers at `MuzzleB_00/01`, two lasers at paired `MuzzleA_00/01` and `02/03` midpoints. Four mount IDs; zero destructible systems or hangar; all mounts follow the banking body. Shared Laser/ConcussionMissile profiles unchanged.
- `ShipAbilityId.Cloak = 11`: source XML duration 80 s, recharge 10 s, manual cancel. Enemy rendering, minimap, radar, selection, tooltips, target acquisition and cached pursuit reject cloaked entities; weapons and other abilities cannot start while cloaked. Friendly movement/vision/selection stay active. Existing deployment clearance/blocker rules still apply; already-fired impacts remain valid.
- Provisional speed/turn/acceleration `42/24°/s/36`, bank 15°, radar 175, height tier 6/Y 80, length 20. Generic Corvette damage category replaces unavailable Corellian Gunboat armor. Project has one build-level gate: level 3; separate source starbase requirement 2 is recorded rather than implemented.
- `stealth` is a duplicate MeshShield.fx shell; preserve it but disable its Unity renderer alongside Collision/Shadow. Missing `EV_PHANTOM_SCAN_LINES.dds` / `EV_SCANLINES2.dds`; source animated/refraction/proxy effects are not recreated. Local death clone reuses intact geometry, so the project wreck shader uses the dedicated hull material.
- Sibling `ReV_Stealthship-Converted/` contains packed blend, FBX, PNGs, source XML/credits, conversion scripts, saved-reference/geometry reports and previews. Registrations, compilation/import and references pass; no missing scripts, broken references or donor model dependencies. No automated tests or Play Mode run. Runtime acceptance and provisional balance review remain active.

## V-19 Torrent — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/V-19 Torrent Import.md`; acceptance plan `TODOs/Features/V19Torrent_Import.md`.
- Source: RaW `ReV_v19_torrent.ALO`; original ALO and three referenced DDS hashes unchanged. Credits: mesh/textures Evillejedi; rigging z3r0x (`credits.txt`, V-19 entry).
- Stage textures/binary audit with `uv run --with pillow python Tools/Blender/prepare_v19_torrent_textures.py`. Run `export_v19_torrent.py` once through Blender MCP on a fresh `V19Torrent Source` import. Requires Blender 3.6.23 and 23 imported bones; restore the binary-verified identity Root → 24 bones. Preserve deployed wings, attachments and four hidden helpers.
- Source 12 meshes / 4,020 triangles → FBX/Unity 3,960; 60 zero-area engine faces removed. All nondegenerate triangle corners/UVs and bone names/parents match. Blender geometry/bone error ≤0.000164265/0.000106171 source units; Unity ≤0.000003258/0.000002030 project units, UV error 0. DDS→PNG pixels match exactly.
- Type-first `RepublicShips/V19Torrent` art folders; visual/gameplay `V19Torrent.prefab` / `V19TorrentSquadronView.prefab`; own five-member reinforcement preview. FBX scale 0.02, centered member size 7.01667 × 3.24540 × 4 project units, root scale 1, bow +Z/up +Y.
- Republic `SquadronType.V19Torrent = 5`: five fighters, ten `FighterLaser` weapons using `MuzzleA_00/01`, three engine trails per craft using `Object01/02/03`; hull/shields/regen 70/30/3, Fighter armor, cost/build/tech/population 400/6 s/1/1. User values override local RaW damage 8/build 8 s with 5/6 s; no separately targetable fighter systems.
- Provisional cruise/combat 35/40 units/s, acceleration 30 units/s², turn 120°/s, bank 60°, spacing 5, height 11, limit 10; regeneration 3 each second. Navigation radius 17, selection diameter 34, member collider radius 4. Source movement 5/3.5/6 uses different units; no tech-5 V-Wing replacement configured.
- Registered roster, View/Data addresses, asset mappings, HUD/tooltip icons, silhouette, matchups and own placement. Authored alpha team mask covers 20.00885%; all eight palettes rendered, blue/green inspected. Icon 512 × 512 and five-member formation rendered.
- Source sibling `ReV_v19_torrent-Converted/` contains packed editable blends, FBX, PNGs, reports, scripts, previews and full RaW credits. Hunt is absent from the current ability system and remains pending; wing/EaW shader animation is outside the static import.
- Saved references, geometry, imports and compilation verified; no missing scripts, broken references, donor models or new Console errors. MainMenuScene stayed clean. No automated tests or Play Mode run; runtime acceptance and provisional balance remain active.

## Imperator-class Star Destroyer — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/Imperator Import.md`; remaining acceptance plan `TODOs/Features/Imperator_Import.md`.
- Source: RaW 1.2.1 `EV_StarDestroyer_MKI.ALO` and `EV_Stardestroyer_MKI_D.ALO`. Both ALO and eight referenced DDS hashes remain unchanged. Credits: mesh/textures Evillejedi; rigging Warbnull (`credits.txt`, Star Destroyer row).
- Prepare PNGs and binary bone audits with the bundle's `Scripts/Prepare.py` and hull-alpha mask with `PrepareMasks.py`. Run `export_imperator.py` once through Blender MCP on a fresh `Imperator Source` scene; for the damaged variant use `VARIANT = 'ImperatorWreck'` and `ImperatorWreck Source`. Paths are specific to this checkout; create the output folder first. Restore only the identity Root verified in each binary: intact 168 bones, damaged 75.
- Preserve all 26 intact meshes / 34,974 triangles and six damaged meshes / 36,119 triangles, attachment names and parents. Blender maximum bone/geometry errors: intact 0.000669512/0.000111991, wreck 0.000092703/0.000145560 source units. Unity bone error ≤0.000013604 project units; corner positions ≤0.000004053, no parent mismatches. DDS→PNG conversions are pixel-identical.
- Disable FBX vertex welding and mesh compression to retain visible UV seams. Visible and non-shadow helper UVs match within 0.00001; Unity remaps nearby UVs on the four disabled shadow-volume meshes. Original UVs remain in the blend/FBX. Missing `ISDI_shiplights.dds` affects only disabled `HP_Weapon_1C_Blast`; all living surface textures are present.
- Type-first `RepublicShips/Imperator` art folders; geometry/gameplay `Imperator.prefab` / `ImperatorShipView.prefab`, own reinforcement preview and damaged-model wreck. Centered living/placement size 88.84087 × 48.69202 × 160, root scale 1, bow +Z/up +Y. Wreck applies the source XML scale ratio 1.3/3.5 and measures 96.37397 × 53.09503 × 173.73288. Navigation radius 81; banked hull Y −24.34601 .. +24.646 at ±5°.
- Republic `ShipType.Imperator = 10`; hull/shields/regen 7,500/3,500/50 each second; cost/build/population/tech/limit 8,000/90 s/4/3/1. User limit and Y-Wing complement override local XML limit 3 and ARC-170 bombers. Provisional speed 14 units/s maps source 1.4; yaw/turn acceleration 5, range 250, height tier 1; weapon/system health 400/600 (hangar 400).
- Six `DualHeavyTurboLaser` at `HP_Weapon_R1..3/L1..3`, two `HeavyIonCannon` at R4/L4; twins use their paired FP bones. Fourteen `TurboLaser` use 1C_Ext/Extra, 1R/L, 2R/L, MTBL01/02, MidR/L, R1/L1_TBL and R/L_TBL attachments. Five `IonCannon` use 1C and ION01..04. Actual ALO bones replace broken source XML references; retain paired muzzle positions where present. No point defense or tractor weapon.
- Shield/engines/hangar use `HP_Shield_Bone`, `HP_Engines_Bone`, `Spawn_00`. All 30 destroyable target IDs 0..29 and 27 weapon bindings follow the banking body; health/fog/ion/shield/team/explosion and hangar references are rebuilt. Launch exit is eight units below the hull. User-approved ARC-170 substitute has 14 total launches / four simultaneous; Y-Wing has eight / two. Saved ARC-170 view/data mappings resolve. Launch delays 4 s / 8 s are provisional.
- `ShipAbilityId.LaserBeam = 10` uses the existing `ProtonBeamSettings`/beam implementation and its audio profile: damage 6,000, active 8 s, recovery 50 s, target range 500. These are shared project tuning, not RaW Tractor Beam values. The factory resolves the settings type; no new combat logic or tractor hardpoint.
- Registered Republic roster, data/view mappings, existing View/Data Addressables groups, HUD/tooltip icons, matchups, own placement and future icon regeneration. StarDestroyer2's Republic build-roster entry is removed by user request; player UI and AI both consume that roster. Actual-model icon/silhouette 512 × 512; alpha bounds (48,43)..(464,469), uncropped. Hull alpha supplies the linear team mask (3.6773% coverage); all eight living/wreck palettes rendered, blue/green inspected.
- Source sibling `EV_StarDestroyer_MKI-Converted/` contains editable packed converted blends, imported source snapshots, FBXs, PNGs, reports, conversion scripts, palette previews and full RaW credits. Death ALA and EaW animated shader/proxy effects are not converted.
- Saved prefab/reference/import/compilation inspections passed, with no missing scripts, broken references or donor-model dependencies. No automated tests or Play Mode started. Runtime combat/ability/hangar/placement/destruction acceptance and provisional balance review remain active.

## Dispatcher-class Frigate — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/Dispatcher Import.md`; acceptance plan `TODOs/Features/Dispatcher_Import.md`.
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Tecno Destroyer/CIS_TecnoDestroyer.ALO` and `CIS_TecnoDestroyer_D.ALO`. All five original ALO/DDS/ALA hashes and the supplemental RaW `bluethruster.dds` hash remain unchanged.
- Use separate portable Blender 3.6.23 processes with `--background --factory-startup --python`; shared C9979 Blender session is preserved. Package `Scripts/Audit.py` imports each variant; `prepare_dispatcher_textures.py` converts DDS with Pillow; `export_dispatcher.py -- Dispatcher` / `-- DispatcherWreck` exports the audited source blends. No preference or MCP configuration changes.
- Restore only the binary-verified identity `Root`: intact 43 bones, wreck 34. Preserve all seven intact meshes, two wreck meshes, UVs and case-sensitive/duplicate attachment names. Blender FBX geometry error ≤0.000048852 source units; bone error ≤0.000068665. Unity bone error ≤0.000001198 project units, no bone parent mismatches; the armature container folds into the imported model root.
- Intact source/FBX 6,411 triangles → Unity 6,407; wreck 4,551 → 4,547. Unity removes exactly four zero-area faces per model. Every retained position/UV corner matches within 0.000001395 project units. Three DDS→PNG conversions are pixel-identical; hull/normal 1024 × 1024, thruster 256 × 256.
- Type-first `SeparatistShips/Dispatcher` art folders; visual/gameplay `Dispatcher.prefab` / `DispatcherShipView.prefab`, own reinforcement preview and dedicated `_D` wreck. User size: Munificent length 82.56805; centered size 55.29128 × 32.17981 × 82.56805, bow +Z/up +Y, all roots scale 1. Navigation radius 42; banked Y −16.08990 .. +16.09021 at ±20°.
- `ShipType.Dispatcher = 108`; CIS roster, data/view mappings, existing View/Data Addressables groups, actual-model 512 × 512 transparent icon, HUD/tooltip, matchups and own placement registered. Hull/shields/regen 3,500/1,000/50 per second; cost/build/population/tech 3,400/33 s/3/2. Generic project Frigate armor; no separate Assault Frigate profile.
- Listed composition is 20 weapons + shield + engines = 22 targets, IDs 0..21. Four `DualMediumTurboLaser` twins use `TurboY01/04` port and `TurboY03/05` starboard +1.2 units vertically; seven `TurboLaser` use all `TurboY01..07`; lasers use `LaserY04/05`; ions `Ion01/02/03`. Front PD uses `Ion02` +3.5 units vertically; rear PD uses midpoint `LaserY04/05` with a 180° facing parent; side PD `LaserY02/03`. Source bones stay intact; added mount offsets are project choices.
- Shield/engine use `Shield_00` / `Engines_00`; engine VFX uses `PE_JEDICRUISER_SML`. All 22 health/fog and 20 weapon bindings follow the banking body. No hangar despite retained Spawn attachments. Existing `BoostWeaponPower = 5` supplies Power to Weapons; no legacy Assault multipliers copied.
- Provisional: speed 66 units/s versus Munificent 54, yaw/turn acceleration 45, bank 20°, max count 10; weapon/system/PD health 250/400/150. Existing shared weapon/audio profiles and Munificent matchup categories are reused without modification.
- Livery selects source cyan/blue trim at hue/range/saturation/strength 0.55/0.065/0.25/1, including wreck; neutral plating stays unchanged. All eight living/wreck palettes rendered; blue/green inspected. Icon alpha bounds (43,48)..(469,464), uncropped.
- Sibling `Tecno Destroyer-Converted/` retains packed editable blends, FBXs, PNGs, audit/geometry/reference reports, conversion scripts and previews. Credits: model Evillejedi; textures Evillejedi, modified by Nawrocki; rig Nomada_Firefox; supplemental original-game thruster texture retained with RaW provenance and full pack README.
- Saved references/geometry/import/compilation checks passed; no missing scripts, broken references or donor model dependencies. Concurrent work produced unrelated UI teardown errors and test warnings in the shared Editor. No automated tests or Play Mode were started for this import. Death ALA, EaW shader/proxy animation, runtime acceptance and provisional balance review remain outside static conversion.

## C-9979 Lander — 2026-10-05

- Guide: Obsidian `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/C-9979 Import.md`; acceptance plan `TODOs/Features/C9979_Import.md`.
- Source: RaW 1.2.1 `SeV_c9979.ALO`. Original ALO and three DDS hashes unchanged. Credits: Evillejedi mesh/textures; z3r0x rigging.
- Run `prepare_c9979_textures.py` with Pillow first. In Blender 3.6.23, import with animations disabled into `C9979 Source`, then run `export_c9979.py` once through MCP as direct code. Restore only the binary-verified identity Root omitted by ALAMO.
- Three meshes / 4,182 triangles / eight bones, four muzzle attachments and all UVs preserved. Hull/lights visible; Collision disabled. Maximum Blender geometry displacement `0.000003053` source units; Unity bone error `0.000000090946` project units, correct parents. DDS-to-PNG conversions are pixel-identical; hull alpha supplies the linear team mask.
- Art: type-first `SeparatistShips/C9979` folders. Visual/gameplay: `Assets/Prefabs/Models/Ships/C9979.prefab`, `C9979ShipView.prefab`; own reinforcement and wreck prefabs. Centered size `32 × 5.138031 × 12.09517`; matching bounds, root scale 1, bow +Z, up +Y.
- CIS `ShipType.C9979 = 107`; hull/shields/regen `300/20/2` each second. One `MediumLaser = 23` mount at the four-muzzle midpoint, one shot per 5 s; zero destructible hardpoints, abilities or hangar. `HullTarget` and the hull-only health path support whole-hull targeting and damage after shields.
- Authorized provisional values: level/cost/build/population/limit `1/500/10 s/1/20`; speed/turn/acceleration `48/45°/s/36`, bank 15°, flight Y 80, navigation radius 17, selection diameter 36. Laser damage/range `6/100`; source range 800 maps by provisional 1/8. Project lacks Transport armor; uses the existing Corvette damage category pending balance review.
- Registered roster, data/view mapping, existing View/Data Addressables, placement, matchups, weapon audio and HUD/tooltip icons. Actual-model transparent icon/silhouette 512 × 512, uncropped; all eight team palettes rendered, blue/green inspected.
- Source sibling `SeV_c9979-Converted/` contains packed Blender source, FBX, PNGs, reports, previews, converter scripts and full RaW credits. Separate land model/ALA animations and EaW animated effects are not converted.
- Saved geometry/reference checks and Unity compilation passed; no missing scripts/references, donor model dependencies or new import/serialization errors. No automated tests or Play Mode run by this task. Runtime acceptance and provisional balance remain in the active plan.

## NTB-630 — 2026-10-05

- Guide: Obsidian `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/NTB-630 Import.md`; acceptance plan `TODOs/Features/NTB630_Import.md`.
- Source: RaW 1.2.1 `ReV_ntb630.ALO`. Run `prepare_ntb630_textures.py` with Pillow, then `export_ntb630.py` using portable Blender 3.6.23 with `--background --factory-startup --python`. This deliberately avoids the shared Blender instance used by another agent; no preference or MCP connection changes.
- Eight meshes / 5,097 triangles / fourteen verified bones survive Blender and Unity imports. Restore only the binary-verified identity `Root`; preserve all UVs, attachments and five disabled helpers. Maximum Blender bone/geometry error `0.000002876 / 0.000005723` source units; Unity bone error `0.0000001062` project units, no parent mismatches. Original ALO and four DDS hashes unchanged; PNG conversions are pixel-identical.
- Art: type-first `RepublicShips/NTB630` folders. Visual/gameplay: `Assets/Prefabs/Models/Squadrons/NTB630.prefab`, `NTB630SquadronView.prefab`; placement: `Assets/Prefabs/Ui/Reinforcement/NTB630ReinforcementView.prefab`. Centered craft size `2.650344 × 0.853184 × 4`; bow +Z, up +Y, prefab roots scale 1. Gameplay/placement formation bounds match.
- `SquadronType.NTB630 = 4`: four Republic bombers, Bomber armor, hull/shields/refresh `60/30/3` per craft; cost/build/population `550/8 s/1`, early tech level 1. User build time overrides local XML `17 s`. Uses existing `ShipAbilityId.IonShot = 9`; saved data and catalog binding verified. No ability implementation, catalog, VFX or carrier-complement changes.
- Each craft uses two existing `FighterLaser = 15` mounts at `MuzzleA_00/01`, one `FighterProtonTorpedo = 22` at the midpoint of `MuzzleB_00/01`, and one trail at `Pe_TieBomberEngine`. Twelve unique weapon IDs and explicit health/flight/fog/team bindings; existing torpedo attack-run behavior is reused.
- Provisional tuning: cruise/combat `24/27`, acceleration `18`, turn `65°/s`, bank `35°`, spacing `4`, height `11`, limit `10`; shield refresh interval `1 s`. Measured formation/member radii `12.52512/2.37352` → navigation/collision `13/2.4`; selection diameter `26`.
- Registered Republic roster, data/view lookup, existing View/Data Addressables groups, HUD/tooltip icons, matchups and own placement. Actual-model transparent icon/silhouette `512 × 512`; source alpha supplies a linear team mask. All eight palettes rendered; blue/green inspected.
- Supplied package lacks `ReV_ntb2.dds` and `ReV_ntb2_gloss.dds`; intact turret geometry uses an explicit neutral-metal material. Hull gloss source is preserved without recreating the EaW shader. No ALA animations or animated source effects are converted.
- Sibling `ReV_ntb630-Converted/` contains packed Blender source, FBX, PNGs, reports, previews, scripts and RaW credits. Original mesh: Howard Day; low-poly rebuild: Major Payne; textures: Howard Day/Major Payne/Bryant; rigging: z3r0x.
- Saved geometry/reference and visual inspections passed; no new Unity import/serialization/Console errors. No automated tests or Play Mode run. Original turret textures, runtime acceptance and provisional balance review remain in the active vault plan.

## ARC-170 — 2026-10-04

- Guide: Obsidian `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference `GameDesign/ARC-170 Import.md`; remaining work `TODOs/Features/ARC170_Import.md`.
- Source: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_arc170.ALO`; original ALO and four DDS hashes unchanged. Credits: `Mesh-Evillejedi | Texture-Evillejedi/z3r0x | Rigging-z3r0x` (RaW 1.2.1).
- Run `prepare_arc170_textures.py` with Pillow first. In a fresh Blender 3.6.23 file, import the ALO with animations disabled, name the scene `ARC170 Source`, then run `export_arc170.py` once through MCP. The binary has 21 bones; restore only its verified identity Root omitted by ALAMO.
- Preserve 15 meshes, 21 bones, six visible hull/astromech/S-Foil surfaces and nine disabled helpers. Blender source 4,691 triangles → FBX reimport 4,685: six duplicate faces collapse in the disabled flashes; all four unique position/UV triangles per flash remain. Unity imports all 4,691 source triangles.
- Blender maximum bone/corner displacement `0.000005722 / 0.000006694` source units; Unity bone error `0.0000001526915` units, correct parents and UVs throughout. DDS→PNG pixels match exactly; hull alpha is extracted as a separate linear team mask.
- `RepublicShips/ARC170` in the type-first art folders. Visual/gameplay `Assets/Prefabs/Models/Squadrons/ARC170.prefab`, `ARC170SquadronView.prefab`; placement `Assets/Prefabs/Ui/Reinforcement/ARC170ReinforcementView.prefab`. FBX scale 0.02; visual scale 3.41317248; centered member size `7.938759 × 1.267841 × 4`, bow +Z/up +Y, roots scale 1.
- `SquadronType.ARC170 = 2`; five Republic craft, cost 375, level 5, population 1. User health overrides local XML: 105 hull, 35 shields and 5 shield points each second per fighter. Local XML confirms build 8 s; max count 10 is provisional.
- Per member: two `HeavyFighterLaser = 21` guns at `MuzzleA_00/01`, rear `FighterLaser = 15` at `MuzzleC_00` with a rear-facing ±30° mount, `FighterProtonTorpedo = 22` at `MuzzleB_00`. Twenty weapon IDs, health/flight member references, fog and team renderer bindings are explicit.
- `FighterTorpedoHardPoint` reads `IFighterAttackRunObserver`: one launch maximum per fighter/pass, reset when the pilot enters a new approach. Profile is one torpedo per salvo; existing bomber/global torpedo profiles remain unchanged. Heavy damage 8, two shots/1.5 s; torpedo damage 90, reload 8 s, ranges 40/55 units are project tuning.
- Provisional movement: cruise/combat 28/32 units/s, acceleration 21 units/s², turn 80°/s, bank 40°, spacing 6, height 11, radius 22; measured formation radius 18.68598, member collider 4.5. Placement/gameplay bounds agree. Fighter death uses existing explosion/removal behavior, without a capital-ship wreck.
- Republic roster, dynamic view/data mapping, existing View/Data Addressables groups, HUD/tooltip icons, matchups and placement mapping registered. Transparent model icon/silhouette 512 × 512; all eight owned palettes rendered, blue/green inspected.
- Packed Blender file, FBX, PNGs, reports, palette previews and full credits: sibling `ReV_arc170-Converted/`. Source deploy/undeploy ALA animations and original EaW gloss/effect behavior are not converted; hull gloss source is retained. Lock S-Foils and Astromech Repair require separate squadron support.
- Saved asset/geometry/reference inspection and compilation/import/Console checks clean. No automated tests or Play Mode run; dirty MainMenu scene preserved. Abilities, battle acceptance and provisional balance review remain active in the vault plan.

## CIS Patrol Frigate — 2026-10-04

- Reference: Obsidian `GameDesign/Patrol Frigate Import.md`; active acceptance plan `TODOs/Features/Patrol_Frigate_Import.md`. `ShipType.PatrolFrigate = 106`, separate from Recusant.
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Recusant Patrol/CIS_Recusant_Patrol.ALO`; all five original ALO/DDS/ALA hashes unchanged. Credits: Evillejedi model, Evillejedi/Nawrocki textures, Nomada_Firefox rigging.
- Exporter: `Tools/Blender/export_patrol_frigate.py`, once on a fresh matching Blender import; create `Temp/PatrolFrigateImport/Output/Textures` first. Restores the identity Root verified in the 33-bone binary source; preserves duplicate attachment `PPTW_PTWSA2.001` and uses `EaWName` for validation-object suffix changes.
- Nine meshes, three visible, six disabled helpers. Source 7,021 triangles → FBX 6,989 → Unity 6,987; 34 zero-area faces removed, all 6,986 nondegenerate faces retained. Positions/UVs and bone names/parents checked; maximum Blender/Unity bone displacement 0.0000457764 source / 0.00000145024 project units.
- Art uses type-first `SeparatistShips/PatrolFrigate` folders. Visual/gameplay: `Assets/Prefabs/Models/Ships/PatrolFrigate.prefab`, `PatrolFrigateShipView.prefab`. Centered hull 11.37934 × 12.45770 × 81.09705 units; user size = 60% of existing Recusant length (135.16174 units). Uniform resize factor 3.119117 from the original 26-unit import; bow +Z, up +Y; root scale 1. Placement and wreck match; attachments, engine effect and hull volumes refitted.
- Eight shared Laser weapons use `TurboR01/02/03/04`, `LaserR01/02/03`, `LaserNR02`; engine/shield use `Engines_00`/`Shield_00`. Ten unique hardpoint IDs; all follow the body pivot. No hangar. User stats: hull/shields/regen 900/700/15; tech/cost/build/population 1/1,500/15 s/1; Power to Engines.
- Navigation radius 56.14411; banked vertical hull limits −6.22884 .. +6.22884 units. Provisional corvette tuning: speed 36, turn/acceleration 60, height 80, bank 30°, limit 20; weapon/engine/shield health 160/300/300. RaW Corellian Gunboat armor is not a separate project profile.
- Hull and normal DDS converted to PNG; normals use data import and green flip. All five materials remapped; the unnamed source material becomes Unity `Material`. Livery hue/range/saturation/strength 0.62/0.06/0.25/1, including wreck; all eight owned palettes rendered.
- Registered CIS roster, data/view mappings, existing Addressables groups, HUD/tooltip icons, matchups, own placement preview and future icon regeneration. Transparent icon 512 × 512, uncropped.
- `Recusant Patrol-Converted/` beside the source holds packed Blender source, FBX, PNGs, reports, palette renders and credits. Missing `W_LASER_SMALL.dds` affects disabled helpers only; damaged model/death ALA and EaW proxy/shader animations are not converted.
- Saved asset/reference/geometry/visual inspection and compilation/import completed without new errors; no missing scripts/references or donor model dependencies. No Play Mode or automated tests run; runtime acceptance and provisional balance remain in the active plan.

## Vulture Droid — 2026-10-04

- Import reference: Obsidian `GameDesign/Vulture Droid Import.md`; `SquadronType.Vulture = 102`. Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Vulture/CIS_VULTURE.ALO`; original ALO/DDS hashes unchanged.
- Exporter: `Tools/Blender/export_vulture.py`, run once through Blender MCP on a fresh matching import. Create `Temp/VultureImport/Output/Textures` first. Expects 13 imported bones; restores the identity Root verified in the 14-bone binary source.
- Seven meshes: one visible hull and six hidden collision/shadow/muzzle-flash helpers. Source 2,471 triangles → FBX/Unity 2,407; 64 zero-area flash faces removed, all 2,303 hull triangles retained. All nondegenerate triangle positions/UVs, bone names/parents and bounds preserved. Maximum bone displacement: Blender `0.000007154` source units, Unity `0.00000016792` units.
- Art uses `SeparatistShips/Vulture` in the type-first art folders. Visual/gameplay: `Assets/Prefabs/Models/Squadrons/Vulture.prefab`, `VultureSquadronView.prefab`; seven-member placement: `Assets/Prefabs/Ui/Reinforcement/VultureReinforcementView.prefab`. Visible member size `2.41040 × 1.02828 × 4`, bow `+Z`, up `+Y`; root scale 1.
- User-supplied values: tech 1, seven fighters, cost 350, build 8 s, population 1; per fighter hull 35, no shields. Four `FighterLaser` guns per member use `MuzzleA_00..03` and the existing 5-damage profile; `MuzzleB_00/01` remain without launchers. Buzz Droids excluded; no S-Foils logic/animation added to this static import.
- Provisional tuning: cruise/combat 34.5/39 units/s, acceleration 27 units/s², turn 100°/s, bank 50°, spacing 3, height 11, limit 10. Formation radius measured `13.56549`; navigation radius 14, selection diameter 28. Member collision radius 2.4.
- Registered CIS roster, data/view lookup, existing Addressables View/Data groups, HUD/tooltip icons, matchups and reinforcement preview. Team livery hue 0.67, range 0.08, minimum saturation 0.25, strength 1; all eight palettes rendered.
- Sibling `Vulture-Converted/` holds packed Blender source, FBX, PNGs, reports, previews and pack README. Credits: Star Wars Battlefront II/Pandemic/LucasArts model and textures; Nomada_Firefox rigging. Missing `W_LASER_SMALL.dds` affects disabled flash helpers only.
- Saved geometry/reference and visual checks passed; no missing scripts, broken references or new Unity import/serialization errors. No automated tests or Play Mode run; movement/combat and project tuning still need in-game acceptance. Captor now uses the user-selected 11 Vulture + 8 Droid Bomber total squadron launches, one active per type, with the existing 4/8 s delays.

## Advanced Droid Bomber — 2026-10-04

- Import reference: Obsidian `GameDesign/Droid Bomber Import.md`; early-game Trade Federation / Advanced Droid Bomber, `SquadronType.DroidBomber = 101`.
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/DroidBomber/Cis_DroidBomber.ALO`. Original ALO/DDS files are unchanged. Pack credits: Berruga (model), Berruga and Chris Boudreaux (textures), Nomada_Firefox (rigging).
- Exporter: `Tools/Blender/export_droid_bomber.py`, run once through Blender MCP on a fresh matching import. Expects 45 imported bones; restores the identity `Root` verified in the 46-bone binary source. Create `Temp/DroidBomberImport/Output/Textures` first.
- Conversion: 35 meshes, 46 bones and 28 visible meshes; preserve seven hidden collision/muzzle-flash helpers. Blender round trip preserves every nondegenerate triangle's positions and UVs; 306 zero-area source faces are dropped in Blender and another 39 in Unity, leaving 2,278 triangles. Bone displacement: Blender `0.000002563` source units; Unity `0.00000004391` project units.
- Unity visual/gameplay: `Assets/Prefabs/Models/Squadrons/DroidBomber.prefab`, `DroidBomberSquadronView.prefab`; per-bomber visible size `3.32159 × 0.93928 × 4.00000`, bow `+Z`, up `+Y`. FBX/materials/textures use `SeparatistShips/DroidBomber` in their type-first art folders.
- Source-backed roster/member values: tech 1, four bombers, cost 450, build 8 s, population 1, hull/shields 25/10; two lasers and one proton-torpedo launcher per bomber. Port/starboard lasers use `MuzzleA_01`/`MuzzleA_00`; launcher uses the midpoint of `MuzzleB_00..03`. All authored attachments remain.
- Provisional project tuning: cruise/combat 24/27 units/s, acceleration 18 units/s², turn 65°/s, bank 35°, formation spacing 4, navigation radius 10, height 11, limit 10; refresh 3 points every 1 s. Separate RaW Fighter shield resistance is not represented by the current common shield damage multiplier.
- Registered CIS roster, data/view lookup, existing Addressables groups, HUD/tooltip icons, matchup keys and four-member reinforcement preview. Team livery uses blue hue 0.67, range 0.08, minimum saturation 0.25, strength 1; all eight owned palettes rendered.
- `DroidBomber-Converted/` beside the source folder contains packed Blender source, FBX, PNGs, reports, previews and source credits. Missing `W_LASER_SMALL.dds` affects only disabled source helpers; runtime weapons use project effects.
- Saved asset/reference inspection, geometry checks, icon/palette/squadron renders and compilation completed. No missing scripts or references; no new Console errors after correcting serialized field types. No automated tests or Play Mode run. Captor was subsequently updated during the Vulture import to 11 Vulture + 8 Droid Bomber launches, one active per type.

## Installed setup

- Existing Blender: `C:\Program Files\Blender Foundation\Blender 4.2\blender.exe` — 4.2.16 LTS.
- Converter: `%LOCALAPPDATA%\AI-Tools\Blender\blender-3.6.23-windows-x64\blender.exe` — portable 3.6.23. The ALAMO importer uses pre-4.0 material APIs.
- [MCP for Blender](https://github.com/ahujasid/mcp-for-blender): package `mcp-for-blender==2.1.3`, add-on protocol 13. Installed in both versions; enabled and verified in 3.6.
- [ALAMO importer](https://github.com/AndrewFullard/Blender-ALAMO-Plugin): commit `2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5`, installed and enabled through MCP in 3.6. Add-on source is unchanged.
- Converter download SHA-256, verified against Blender's published checksum: `e3296eba7eab32c2e5182459ec7614af32224eee2bd32c9d0a08ffd751c54f3b`.
- `.codex/config.toml` is canonical. Agent JSON mirrors contain the same Blender connection. Safe mode enabled; telemetry disabled; socket binds only `127.0.0.1:9876`.
- Start the converter with `powershell -File Tools/Blender/Start-Blender.ps1`. The enabled add-on starts its listener automatically. Restart Codex after configuration changes to refresh its native tool list.

## Converted asset

- Source: `C:\Users\golin\Downloads\Rothana.2\Models\Rothana_Stardestroyer_Full_Armed.ALO`. Original files were not edited.
- Separate deliverables: `C:\Users\golin\Downloads\Rothana.2-Converted\` — `Rothana.fbx`, `Rothana.blend`, `Textures/`, conversion reports and Unity preview.
- Unity FBX: `Assets/Art/Models/RepublicShips/Rothana/Rothana.fbx`.
- Visual prefab: `Assets/Prefabs/Models/Ships/Rothana.prefab`. Contains geometry and attachment transforms, centered and scaled for gameplay.
- Gameplay prefab: `Assets/Prefabs/Models/Ships/RothanaShipView.prefab`. Republic ship with movement, health, weapons, hangar, selection, shields, fog visibility, team color, audio and wreck configuration.
- Materials: `Assets/Art/Materials/Models/RepublicShips/Rothana/` — 11 materials.
- Textures: `Assets/Art/Textures/Models/RepublicShips/Rothana/` — 16 PNGs converted from the referenced DDS images.
- FBX import scale: `0.02`, matching the existing Venator and Acclamator FBX imports. Raw visible size: approximately `4.918 × 1.783 × 9.680` Unity units; bow faces `+Z`, up is `+Y`.
- Visual prefab scale: approximately `16.53`; centered visible size: `81.289 × 29.477 × 160.000` Unity units. Gameplay root stays at unit scale. This is a project scale choice, not a canonical length in meters.

## Gameplay integration — 2026-10-03

- Source: Obsidian `GameDesign/Republic at War - Republic Unit Stats Reference.md`, Rothana entry. The note's version/provenance caveats still apply.
- `ShipType.Rothana = 7`; registered in `ShipsData`, `AssetMappingData`, Republic `FactionsData`, `ShipUiData`, `TooltipIconData`, and the existing Addressables `View`/`Data` groups.
- `RothanaShipData.asset`: hull `10,300`, shields `3,000`, Power to Shields and Power to Weapons.
- Destroyable systems: `18` weapons + shield generator + engines + hangar = `21` hardpoints. IDs and health/weapon/fog reference lists are assigned explicitly.
- User-approved fighter substitution: Delta-7 and A-Wing. Each bay has `3` reserves and `1` active squadron; `6` total launches, `2` simultaneous. Initial delay `4 s`; launch interval `8 s`.
- Provisional project balance: level `5`, price `7,500`, build time `35 s`, population `8`, maximum count `3`; movement `6.5 units/s`, rotation/turn acceleration `4.5`, navigation radius `95`.
- Inherited Venator tuning: shield regeneration `90` with delay `3 s`; weapon/hangar hardpoint health `400`, engine/shield hardpoint health `600`. These and hangar capacity are project choices; the reference does not supply them.
- Collision, shield, ion-effect bounds, selection marker and hangar exit fit Rothana. Baked hull range with ±15° banking: `-23.337 .. 23.858`.
- `RothanaWreckData.asset` references `Assets/Prefabs/Models/Wrecks/RothanaWreckView.prefab`; seven opaque hull renderers use dedicated wreck materials.
- `RothanaIcon.png`: transparent `512 × 512` render of the actual model, used by roster, ship UI and tooltips. `ShipIconGenerator` includes Rothana. Icon/wreck generation respects disabled helper renderers.
- Reinforcement placement uses `Assets/Prefabs/Ui/Reinforcement/RothanaReinforcementView.prefab`, registered under Rothana in `ReinforcementData`. Its ten visible meshes use the shared hologram material; its trigger bounds and orientation match the gameplay ship. Ship preview lookups require an explicit mapping instead of falling back to Venator.

### Hardpoint mapping

The ALO's turret names/layout differ from the stats reference. Gameplay types use the reference; positions use the supplied model's existing muzzles.

| Gameplay weapon | Count | Model attachment positions |
| --- | ---: | --- |
| DBY-827 heavy twin turbolaser | 8 | Six `MTL_01..06` muzzle-pair midpoints, plus `HTL01` and `HTL02` muzzle-pair midpoints |
| Turbolaser battery | 2 | `TL01`, `TL03` |
| Laser battery | 4 | `MuzzleC_01..04` |
| Point defense | 2 | `MuzzleC_05`, `MuzzleC_08` |
| Ion cannon | 2 | `MuzzleC_06`, `MuzzleC_07` |

- Shield generator uses `HP_Shield`; engine system uses the midpoint of `Eng01`/`Eng02`; hangar uses the `hangar` bone. Launch point clears the ventral hull.
- Side batteries use port/starboard firing arcs; dorsal heavy weapons have ±160° yaw coverage and point defense has full yaw coverage.
- Source weapon geometry is static; gameplay muzzle positions follow the banking body pivot.

### Integration verification

- Unity compilation completed without errors; no new import, serialization or console warnings/errors during integration.
- Reloaded saved assets: `18` weapons, `21` hardpoints, `10` visible mesh renderers and `7` wreck meshes; no missing scripts or broken serialized references.
- All five weapon types already have profiles in `WeaponsData`. New view/data addresses point to the correct assets; all three icon consumers reference the generated sprite.
- Inspected the generated icon. No Play Mode or automated tests were run; battle behavior and provisional balance remain untested. Existing unsaved scene changes were retained.
- Team-color material correction: rendered the model with the existing eight-color palette; inspected blue/green recoloring of the red stripes, with neutral hull panels retained. This is an isolated Editor rendering check, not a battle test.

## Conversion decisions

- Restored the identity `Root` bone removed by the importer, verified against the ALO binary header. Preserved all 100 bones and their hierarchy.
- Converted the importer's `CHILD_OF` constraints to bone parenting while preserving world transforms. No meshes merged, decimated or removed by the conversion script.
- The upstream importer welds duplicate vertices in collision/shadow meshes automatically. Geometry validation compares the imported ALO to the exported FBX.
- Retained `Shield`, `Col`, and `Shadow` meshes; their renderers stay disabled in the Unity prefab.
- `Hull_LOD_1` contains engine geometry. Both hull-named meshes remain visible; their names alone do not establish interchangeable LODs.
- Opaque surfaces use `EmpireAtWar/Ship Lit`; effects use additive URP particle materials. Original EaW animated shield, refraction, proxy particle effects, and shader logic are not recreated.
- Team livery: `Rothana_hull_1_Base`, `Rothana_hull_3_base`, `Rothana_engines` and their wreck materials use hue `0` (red), hue range `0.07`, minimum saturation `0.4`, strength `1`. `TeamColorView` supplies the owner's palette index to their renderers. Unowned previews retain the source red paint.
- Preserve these explicit livery settings after reimport. Automatic detection's `0.5%` texture-coverage cutoff misses the sparse red patches on hull 1 (`0.3%`) and engines (`0.4%`). Leave the yellow hangar markings and unpainted materials at livery strength `0`.
- Normal maps import as linear normal textures with the green channel flipped, matching the ALAMO importer's normal convention. No ALA animation files were supplied.

## Verification — 2026-10-03

- MCP initialization, code execution, add-on installation, import/export and viewport screenshot calls succeeded.
- Blender FBX round trip: 13 meshes, 42,813 triangles, 100 bones; all bone names and hierarchy retained. Maximum bone position difference: `0.0000511` source units. Bounds difference below `0.00003` source units.
- Unity imported 13 meshes and 42,813 triangles, with UVs on every mesh. All 100 bone transforms and parent names verified; maximum position difference `0.00000121` Unity units. Unity's vertex welding changes vertex counts.
- Inspected a rendered Unity preview; verified materials, texture references, hidden helper renderers, and saved asset metadata. No new import/serialization errors. No Unity automated test suites or Play Mode were run.

## Repeat this conversion

1. Use a fresh Blender 3.6 file with the ALAMO and MCP add-ons enabled. Import the Full Armed ALO with `bpy.ops.import_mesh.alo(filepath=SOURCE_PATH, importAnimations=False)`.
2. Create `Temp/BlenderConversion/Output/Textures` from the shell. Run `export_rothana.py` through MCP. It expects this specific original import and writes only to that staging folder.
3. Inspect the printed `ROTHANA_REPORT` and the separate `Rothana_FBX_Validation` scene. Blender appends `.001` to re-imported mesh names because both scenes share its global object namespace.
4. Reconnect Unity materials explicitly when importing another asset; do not assume FBX reproduces EaW shaders.

`mcp_call.py` is an MCP SDK client for sessions whose native tool list predates installation. It reads the canonical project configuration, including safe mode:

```powershell
uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py request.json
```

Example request:

```json
{"tool":"execute_blender_code","arguments":{"code":"import bpy\nprint(bpy.app.version_string)"}}
```

For the saved export script, use `{"tool":"execute_blender_code","code_file":"F:/Private/empire-at-war/Tools/Blender/export_rothana.py"}`. Screenshot requests use `get_viewport_screenshot`; the client saves the returned image beside the request JSON.

## Captor-class Carrier — 2026-10-04

- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Carrier/CIS_Carrier.ALO`; damaged ALO and death ALA remain separate. Original source files are unchanged.
- Editable packed Blender file, FBX, PNGs, source credits, reports and palette previews: sibling `Carrier-Converted/` folder.
- Exporter: `Tools/Blender/export_captor.py`, run once through MCP on a fresh intact Carrier import. It expects 63 imported bones and restores the identity `Root` verified in the 64-bone binary source.
- The importer does not resolve textures beside this source reliably; the exporter loads the hull DDS files explicitly. `Yellow_thruster.dds` comes from `CIS_Hero_Units_Pack_2014/Admiral Trench/`. Missing original hangar-shield effects use the existing project `Shields.mat` appearance.
- Blender round trip: 7 meshes, 6,438 triangles, 64 bones, UVs on every mesh; maximum bone displacement `0.00005928` source units.
- Unity: 7 meshes, 6,426 triangles, 64 bones; maximum bone displacement `0.000001051` units; no parent mismatches. Unity removes 12 verified zero-area faces: hull 2, hidden `Motor` 6 and `MotorSML` 4. `MotorSML` remains as an empty hidden helper.
- FBX import scale `0.02`; centered visual scale approximately `20.01525`; final size `65.293 × 44.256 × 140` project units. Bow `+Z`, up `+Y`, gameplay root scale `1`.
- Visual/gameplay/placement/wreck bounds agree. Banking ±10° gives hull range `−22.823 .. 22.128`; flight height `−118`, navigation radius `85`. These are provisional project choices.
- `ShipType.Captor = 105`; ship data, asset mapping, CIS roster, ship UI, tooltip icon, placement mapping and existing Addressables `View`/`Data` groups are registered.
- User-supplied values: hull `3,400`, shields `800`, regeneration `50`, cost `3,500`, build `30 s`, population `2`. Station level `3` follows the supplied Level 3+ build requirement; source Tech 2/5 variants have no distinct registration here.
- Provisional choices: maximum count `20`, speed `7`, yaw/acceleration `5`, shield regeneration delay `3 s`; weapon/hangar hardpoint HP `400`, engine/shield HP `600`, inherited wreck tuning.
- Hardpoints total **15**, matching the supplied itemized composition; its stated total of 14 is inconsistent. `TurboMR01/02` → 2 turbolasers; `LaserR01..06` → 6 lasers; `LaserR07/08` → 2 ions; `Shield_00`, `Engines_00..02`, `Spawn_00` → shield, 3 engines, hangar. Extra ALO turret attachments remain in the visual asset.
- All hardpoints follow `BodyPivot`; health/fog lists contain 15 unique IDs, weapons list contains 10. Launch `(0.015, −7.465, 78)` clears the collision box's forward edge by 8 units.
- Power to Weapons uses existing `BoostWeaponPower` tuning; original shield/engine tradeoffs and Victory/Frigate armor/shield types are not recreated by the current project data model.
- Hangar uses the user-selected Vulture/Droid Bomber complement: 11 Vulture + 8 Droid Bomber total squadron launches; two bays, one active per type (2 overall); first launch after 4 s, shared interval 8 s. Both squadron data/view mappings resolve. Reserves include the first launched active squadron.
- Hull and wreck livery: hue `0.67`, range `0.08`, minimum saturation `0.25`, strength `1`. All eight palettes rendered; blue/green inspected. Icon is a transparent `512 × 512` render of the model.
- Saved assets have no missing scripts or broken references. Imports/compilation and post-save Console checks passed; no automated tests or Play Mode were run. Pending acceptance is tracked in Obsidian `TODOs/Features/Captor_Import.md`.
- Credits: model `Evillejedi`; textures `Evillejedi`, modified by `Nawrocki`; rig `Nomada_Firefox`. Pack README asks contacting its author before public-mod use.

## Mandator II / Pride of the Core — 2026-10-04

- Read Obsidian `Architecture/ALO_MODEL_IMPORT_GUIDE.md`. User-approved sources: RaW `ReV_Mandator.ALO` for the living ship; `ReV_Mandator_D.ALO` for its wreck. Source SHA-256 values remained unchanged.
- Packed editable Blender files, FBXs, PNGs, reports and palette previews: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_Mandator-Converted/`.
- Exporter: `Tools/Blender/export_mandator.py`. Import each ALO separately in Blender **3.6.23** with ALAMO, animations disabled. Use a fresh file: the upstream shadow-welding step scans objects across scenes.
- Name the source scene `Mandator Source` or `Mandator Wreck Source`. Create `Temp/MandatorImport/Output/Textures`, then run the exporter once through Blender MCP. It expects 104 or 69 imported bones and restores the verified binary identity `Root` → 105 or 70 bones.
- Conversion preserves mesh transforms, UVs, source helper/hidden flags and bone hierarchy; bone parenting replaces importer constraints without changing world geometry. Absent damaged-model `.tga` references resolve explicitly to matching RaW DDS files.
- Blender round trip: living **15 meshes / 9,897 triangles / 105 bones**, damaged **24 / 23,279 / 70**. Maximum triangle-corner displacement: `0.000641` / `0.000644` source units; UV tolerance `0.00001`. Maximum bone displacement: `0.000290` / `0.000339` source units.
- Unity import: living **9,893 triangles**, damaged **22,680**; bone displacement at most `0.00000514` / `0.00001267` project units, no parent mismatches. Unity removes 4 faces from living `Cortex` (source contains 6 zero-area faces) and 599 faces from the disabled damaged `Shadow.001`. All 10 opaque wreck meshes retain their source triangle counts (**9,388** total).
- FBX scale `0.02`; centered uniform visual scale `15.624078`; root scale `1`, bow `+Z`, up `+Y`. Living bounds **364.015 × 126.627 × 956.803** project units; length exactly **2× Malevolence's 478.402**. Damaged bounds **363.459 × 126.627 × 956.804**, retaining authored differences.
- Gameplay `ShipType.Mandator = 9` is registered in Republic roster, ship data, asset mapping, icons, tooltips, reinforcement placement and existing Addressables `View`/`Data` groups. Transparent 512-pixel icon renders the actual model.
- User hull/shields/regen: **9,000 / 8,000 / 100**. Local RaW cost/build/population/limit/tech: **70,000 / 90 s / 5 / 1 / 4**. Provisional speed `3`, yaw/acceleration `1.5`, navigation radius `550`, banking ±3°. Dedicated height tiers set Deep to **−520**; shared height-tier assets remain unchanged.
- **64 targetable hardpoints:** 8 quad, 4 heavy and 8 twin turbolasers; 8 ions, 16 point-defense lasers, 8 concussion missiles, 4 proton torpedoes; 2 shields, 2 hangars, 3 engines and 1 tractor mount. IDs `0..63`; health/fog lists 64, weapon list 56; attachments follow `BodyPivot`.
- Mount mapping: quad `HP_TL01..08`, twin `HP_TL09..16`, heavy `HP_TL17..20`, ions `HP_IC01..08`, missiles `HP_MIS01..08`, torpedoes `HP_TRP01..04`. Source has 14 laser mounts; two extra point-defense positions use `HP_LC01/02 + (0,0,−12)` to match the user's 16-laser loadout. Original source bones remain intact.
- `QuadTurboLaser = 20` uses a provisional dual-heavy profile with **8 shots per salvo**, plus explicit matching audio. Power to Weapons uses the existing ability. `TractorBeam = 7` is a destroyable structural mount; the immobilization ability is pending.
- Approved provisional complement: Delta-7 **24 total / 4 active**, A-Wing **18 / 2**; initial/interval **4 / 6 s**. Launch `(0.273, −71.314, 144.060)` clears the ventral collider. Existing hangar behavior binds `HP_SPAWN_01`; the second hangar is structural. Existing engine logic monitors the first engine; destroying either shield generator collapses the shield.
- Livery uses the source hull alpha as a separate linear `_TeamMaskMap`; hue recoloring stays disabled. Living and wreck palette renders cover all eight owners; neutral plating retains its color. Hull textures retain **4096 × 2048**, windows retain **640 × 128**; normals flip green. Damaged-source UVs expose much less painted surface.
- The static `_D` geometry feeds existing procedural wreck behavior; source ALA death animation and animated EaW shader/proxy effects are not converted. Do not regenerate this wreck from the generic intact-hull builder: it would replace the dedicated damaged geometry.
- Saved visual/gameplay/wreck/placement prefabs have no missing scripts or references; imports, compilation and post-save Console checks are clean. No automated tests or Play Mode were run. Current scene changes were preserved. Remaining ability, independent system damage and battle acceptance work is tracked in `TODOs/Features/Mandator_Import.md`.

## BTL-B Y-Wing — 2026-10-05

- Read `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; source `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_ywing.ALO`. Original ALO and three DDS SHA-256 values remain unchanged.
- Packed Blender source, FBX, lossless PNGs, scripts, reports, credits and previews: sibling `ReV_ywing-Converted/` folder. `prepare_ywing_textures.py` stages the DDS conversions; run `export_ywing.py` once through MCP on a fresh Blender 3.6.23 import named `YWing Source`.
- Binary source has 13 bones; restore only its verified identity `Root`, removed by ALAMO. Two source engine attachments share `PE_Ywing_Proto`; the importer names the second `PE_Ywing_Proto.001`. Both positions and their parent remain intact.
- Source: 4 meshes / 2,846 triangles. Blender FBX reimport and Unity: 4 / 2,814. Hull retains 2,786 triangles and collision 12; each disabled flash helper changes 24 → 8 by collapsing duplicate faces. Unique position/UV geometry remains unchanged.
- Blender bone/corner displacement ≤0.000018061 / 0.000027080 source units. Unity: 13 bones, matching parents, displacement ≤0.000000426956 project units, UVs throughout. Converted hull/normal PNGs retain 1024 × 1024; flash 64 × 64. Decoded DDS pixels match exactly.
- FBX scale 0.02; centered member size 1.787514 × 0.529589 × 4 units. Roots scale 1, bow +Z/up +Y. Five-member gameplay and hologram bounds agree: 17.787514 × 1.489589 × 10.4. Formation spacing 4, navigation radius 15, member collider 2.2, selection diameter 30; size/tuning are provisional.
- `SquadronType.YWing = 3`: Republic roster, view/data mapping, existing Addressables groups, own placement, matchups and three icon consumers registered. Transparent 512 × 512 model icon and HUD silhouette generated.
- User values: five craft, hull/shields/regen 60/30/3 per craft; two lasers and one proton torpedo launcher per member. Fifteen unique weapon IDs. `MuzzleA_00/01` → lasers; `MuzzleB_00` → existing two-shot proton profile and attack-run gate. Extra source attachments remain in the visual asset.
- `ShipAbilityId.IonShot = 9`: copied torpedo effect with white particle colors and dedicated white materials. Each surviving craft fires one visual projectile; arrival disables target movement, weapons and ability availability without hull damage. Provisional disable/recovery/range/speed: 3 s / 20 s / 100 units / 35 units/s. Overlapping disables use existing modifier tokens; cleanup releases only this ability's tokens.
- Passive astromech repair: provisional 1 hull point/s on each surviving craft, capped at its maximum; destroyed members stay destroyed. Other squadron assets default to no repair and no abilities.
- Provisional economy: tech 2, cost 500, build 8 s, population 1, limit 10. Cruise/combat 22/25 units/s, below NTB-630's current 24/27. Shield regeneration interval 1 s. Existing global weapon profiles remain unchanged.
- Hull alpha supplies the linear team mask, HSV livery disabled; normal green channel flipped. Eight palettes rendered, blue/green inspected; neutral hull details retained. Isolated Ion Shot render confirms white head/trail.
- Reloaded saved prefabs: five hulls, five health/flight members, fifteen weapons; no missing scripts, broken references or donor dependencies. Import/compile checks passed; unrelated existing camera/empty-third-party-assembly warnings remain. MainMenu scene stayed clean; no automated tests or Play Mode run. Runtime and balance acceptance: `TODOs/Features/YWing_Import.md`.
- Full supplied RaW credits retained. No Y-Wing-specific attribution line was identifiable; `SOURCE_CREDITS.txt` records unresolved individual authors rather than guessing.

## Home One / Admiral Ackbar — 2026-10-05

- Read Obsidian `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; import reference: `GameDesign/Home One Import.md`.
- Sources: `output/eaw-rebel-ships/DATA/ART/MODELS/RV_HOMEONE.ALO`, `RV_HOMEONE_D.ALO`, eight weapon pods and shield-generator pod. Original ALO/DDS hashes remain unchanged.
- Preparation/export: `prepare_home_one_textures.py`, `export_home_one.py`; Blender 3.6.23 + ALAMO, animations disabled. Import each variant into its `<VARIANT> Source` scene; audited source roots remain intact.
- Packed Blender files, FBXs, textures, reports and previews are staged in ignored `Temp/HomeOneImport/`. The exporter is model-specific; choose its `VARIANT` for each source.
- Source → FBX → Unity: living 16 meshes / 9,002 triangles / 107 bones; damaged 13 / 5,948 / 50; each of nine pods retains 111 bones. All 56 Unity meshes retain triangle counts and UV corners; maximum corner/bone displacement 0.000002720 / 0.000003054 project units, no parent mismatches.
- FBX scale 0.02, `preserveHierarchy=true`; centered visual scale 13.3160114. Ship/gameplay/placement bounds 39.14969 × 34.43432 × 180; damaged bounds 39.14969 × 29.92235 × 179.99995. Root scale 1, bow +Z, up +Y.
- Pods use `HP_*_Bone`, avoiding similarly named offset helpers. Eight weapons use `FP_{TBL/IC}_{FL/FR/BL/BR}_00`; shield/engine use `HP_SHG_Bone` / `HP_E_Bone`. Fighter bay and side launch point are project-authored; launch clears the collider by 8 units.
- Dedicated damaged skinned meshes use `BakeMesh(mesh, true)` and explicit static vertex/index assignment. `EditorUtility.CopySerialized` alone left stale GPU buffers; saved mesh import and the wreck render were verified afterward.
- `ShipType.HomeOne = 303`: Rebellion hero, maximum 1, population 4; all ship data, view/data Addressables, icons/tooltips, placement, ability and audio mappings resolve. User values: hull 8,100, shields 2,500, regen 80/s, speed 1.5, turn 0.3.
- Eleven hardpoints: 4 turbolasers, 4 ions, shield generator, engines, fighter bay; IDs 0..10, health/fog bindings 11, weapon bindings 8; all bank with the body.
- `HomeOneConcentrateFire = 16`: caster deals +50% against the selected enemy; own fleet within 400 receives focus orders. `HomeOnePowerToShields = 17`: provisional 10× regeneration, 80% speed, 3× fire delay. Both last 15 s with 20 s recovery after expiry.
- Ackbar's fleet-wide Rebel passive reuses non-stacking project bonuses: damage/speed +10%, hull +20%, shields +10%, vision +50%, incoming damage ×0.65; excludes itself, disabled on death/ion stun.
- Provisional choices: level 5, cost 6,500, build 45 s, range 500, ±5° bank, navigation radius 90, weapon/system HP 400/600. A-Wing + Y-Wing bays each have 3 total reserve launches and 1 active slot; initial/interval 4/8 s. Fighter types were unspecified.
- Hull/wreck use inverted hull alpha as linear team mask; normal green flipped, HSV livery disabled. Eight palettes rendered; blue/green inspected. Own-model transparent 512×512 icon/silhouette saved.
- Four reloaded prefabs have no missing scripts or broken references; all `.meta` files exist. Final compilation/import/serialization checks are clean. No automated tests or Play Mode run; runtime combat and balance acceptance remain unverified. Source death animation and animated EaW shader/proxy effects are not converted.

## T-65 X-Wing — 2026-10-05

- Guide: `Architecture/ALO_MODEL_IMPORT_GUIDE.md`; reference: `GameDesign/X-Wing Import.md`. Source is vanilla `output/eaw-rebel-ships/DATA/ART/MODELS/RV_XWING.ALO`, with Deploy/Undeploy ALA and three DDS files. Source hashes remain unchanged.
- Run `prepare_xwing.py` with Pillow, then `prepare_xwing_animations.py` from the repository root. Import a fresh ALO into Blender's `XWing Source` scene, execute generated `Temp/XWingImport/ImportAnimations.py` through MCP, then execute `export_xwing.py`. Do not repeat the root repair on an already prepared rig.
- Original ALA uses an 18-byte header; the installed importer expects a different compressed header. The model-specific parser decodes 31 frames at 30 fps, quantized translations and quaternion tracks. Restore only the binary-verified identity `Root`; preserve duplicate engine attachment names and parents.
- Source/FBX/Unity: 14 bones, 10 meshes, 2,639 triangles. Unity rest-bone error ≤0.000001672 units; triangle-corner error ≤0.000000158. Visible hull UVs match; Unity merges nearby UVs on the disabled low-detail hull by ≤0.000344. Packed blend/FBX retain source UVs.
- Visible hull is 1,732 triangles with exactly one bone per vertex and no triangles crossing bones. Five rigid body/wing mesh sections retain the complete hull, normals and UVs, with zero displacement against the skinned geometry at six sampled poses. This supports the existing MeshRenderer team/fog APIs without shader changes.
- Legacy clips: `Deploy` → `CloseSFoils`, `Undeploy` → `OpenSFoils`, both 1 s. Sampled Unity animated attachment error ≤0.000255 units. Muzzles and engine trails are parented to the moving wing bones.
- Five-craft gameplay and placement each contain 25 visible sections / 8,660 triangles. Member length 4; formation bounds 23.16832 × 2.48642 × 12; navigation radius 16, spacing 5, selection diameter 32. Sizes and movement/range tuning are provisional.
- `SquadronType.XWing = 300`, `WeaponType.XWingLaser = 24`, `ShipAbilityId.LockSFoils = 18`. Rebellion roster, Addressables, mapping, placement, HUD/tooltip icons, audio and matchups are registered. Health/shield/regen 60/20/3 per fighter; 20 unique lasers. Closed wings apply speed ×1.3, fire delay ×3, regeneration ×3 until toggled off.
- All DDS conversions and alpha-derived team mask are pixel-identical; all eight palettes rendered, blue/green inspected. Transparent 512 × 512 icon and silhouette are uncropped. Three prefabs reload without missing scripts or broken references; final compilation/import checks are clean. No automated tests or Play Mode run.
- Packed source, textures, FBX, integration scripts and reports: `output/eaw-rebel-ships/XWing-Converted/`. Runtime and balance acceptance remain recorded in `TODOs/Features/XWing_Import.md`.
