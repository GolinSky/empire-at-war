# Ginivex fighter art

- Source: RaW 1.2.1 `Data/Art/Models/SeV_fanblade_fighter.ALO`, SHA-256 `9789db0c08fa7b3ebe057176d58bbbd54528e8b76805a3a8973893f97acbf691`.
- Installed `credits.txt`, “Asajj Ventress Fighter”: mesh/textures Evillejedi; animations/rigging z3r0x.
- Shared art for Asajj Ventress and Sev’rance Tann. Static source geometry; no ALA animation or animated source shader recreation.
- Read the vault ALO import, Addressables and project organization guides before rebuilding. Use an isolated Blender 3.6.23 process with installed ALAMO; never execute conversion in a shared artist scene.

## Conversion

1. `uv run --with pillow python Tools/Blender/Ginivex/Prepare.py` stages lossless PNGs and a source/material specification under `Temp/GinivexImport`.
2. Run `Convert.py` with isolated Blender `--background --factory-startup --python-exit-code 1 --python`. Uses the existing source-audited `Tools/AloImport/blender_worker.py`.
3. Run `Render.py` in another isolated Blender process for the transparent 512 × 512 icon.
4. Copy the FBX to `Assets/Art/Models/SeparatistShips/Ginivex/Ginivex.fbx`; copy hull albedo/normal, cockpit albedo and engine albedo to their corresponding `Ginivex_*` paths under `Assets/Art/Textures/Models/SeparatistShips/Ginivex/`. Muzzle-flash art is retained only in conversion output because gameplay uses existing project weapon effects.
5. Initial authoring only: `unity command eval_file --file Tools/Blender/Ginivex/Import.cs --timeout 30000` creates external materials and importer settings. Do not rerun this initial asset-creation script over existing authored materials.
6. `unity command eval_file --file Tools/Blender/Ginivex/BuildVisual.cs --timeout 30000` creates the geometry-only prefab with four intended surfaces. It operates in an isolated preview scene and saves assets.

## Verified values

- Source/FBX: 16 bones, 11 source meshes, 11,510 source triangles including collision/shadow helpers. Bone hierarchy preserved; maximum FBX bone displacement 0.000013829 source units.
- Opaque source hull/cockpit positions, UVs and normals checked against binary ALO and exported FBX. Source flat helper topology restored by the shared converter.
- Additive flash/engine quads lose duplicated and degenerate corners on FBX roundtrip; every nondegenerate position/UV corner checked in both directions at tolerance 0.001 source units. These effect planes do not use the opaque normal comparison.
- Unity prefab: centered bounds `1.676563 × 6.802032 × 4.038580`, bow `+Z`, vertical sail. Model import scale `0.04`; nested model Z offset `1.058876`.
- Source muzzle positions in prefab coordinates: X `0`, Z `1.264558`; Y `-3.349846`, `-3.259618`, `3.261219`, `3.343357`. Pairs at approximately Y `±3.30` support the two gameplay guns.
- Visible surfaces: `Hull`, `Object01` (cockpit), `Box01`/`Box02` (engine planes). Collision, shadow and source muzzle-flash mesh GameObjects stripped from the prefab; source FBX retains them.
- Opaque hull/cockpit use Ship Lit with red-hue team-livery opt-in; hull normal map imported as normal data with green flip. Engine planes use additive URP particle material. Icon rendered from actual source geometry and visually checked uncropped with transparent background.
- Unity imported and saved art/materials/prefab; no new Console errors during import. No automated tests or Play Mode run. Combat and owned-team render acceptance belong to the hero integration.

## Hero integration

- CIS roster: `AsajjVentressGinivex = 104`, `SevranceTannGinivex = 105`; one fighter and maximum one owned per hero. Station level 2, 1,500 credits, 25 s, population 2. No abilities.
- Ventress: 2,800 hull / 1,400 shields, cruise/combat 58/64. Tann: 3,000 hull / 1,200 shields, cruise/combat 56/60. Both match Jango's 4,200 combined HP and 400 nominal raw gun DPS; damage mitigation and firing opportunities change actual output.
- Two unit-scoped laser profiles: `AsajjVentressLaser = 167`, `SevranceTannLaser = 168`; source gun pairs at Y ±3.30. Shared Ginivex icon and geometry; existing CIS dialogue and Jango engine/explosion profiles. No Jango-specific ability voice or production-ready line.
- Data → `Model`; views/previews → `SeparatistUnitViews`; icon → `SharedIcons`. Factory mapping and reinforcement preview entries registered separately for both heroes.
- Verification 2026-11-16: Unity imported/saved assets; inspection confirmed one matching flight/health member, two unique mounts, matching baked loadouts, 400 raw DPS, empty ability lists and intended Addressables owners. Compilation status clear; no new Console errors. Icon visually inspected.
- Added `SeparatistGinivexHeroTests` (`Squadrons`). Reviewed dynamic `WeaponTaxonomyTests`, `BalanceInventoryTests`, `BattleAssetPreloaderTests`, `SquadronIconPrefabTests`; no existing expectation changes required. Tests and Play Mode not run under the local execution restriction; combat balance remains unplaytested.
