# Imperial Venator — 2026-10-09

- Empire ship `ImperialVenator = 211`; dedicated ability `ImperialVenatorIntensifyFirepower = 35`.
- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data`.
- Read `ALO_MODEL_IMPORT_GUIDE`, `PROJECT_ORGANIZATION`, `UI_UX_GUIDELINES`, `UI_CODE_BUILD_GUIDE` through `empire-vault` before implementation.
- Vault reference: `GameDesign/Imperial Venator Import`; completion plan: `Done/Features/ImperialVenator_Import`.

## Source evidence

- `XML/GameObjectFiles.xml` registers `Units/Space/Units_Space_Empire_Venator.xml`.
- Exact chain: `Venator_Empire → Venator_Upkeep → Venator_Template → Capital_Base → Vessel_Base → SFX_Base`.
- `Venator_Empire` explicitly sets `Affiliation=Empire`, `Space_Model_Name=Empire_Venator_Star_Destroyer.ALO`.
- Binary hull/bridge slots reference `Empire_Venator_Star_Destroyer_Hull.dds` and `Empire_Venator_Star_Destroyer_Bridge.dds`. This is the Imperial model and texture set.
- Exact inherited attachments in `Hardpoints_Neutral_Republic_Venator.xml`: eight `Neutral_Republic_Venator_Star_Destroyer_HTL_01..08.ALO` on `TL_01..08`; two `Neutral_Republic_Venator_Star_Destroyer_MTL_01..02.ALO` on `TL_09..10`. Their Republic-prefixed names are explicit shared source references.
- Inherited `Death_Clone → Death_Clone_Venator` references `Neutral_Republic_Venator_Star_Destroyer_Deathclone.ALO`. Converted separately for source fidelity; the gameplay wreck uses the assembled Imperial hull and its dedicated Imperial wreck materials.
- `Evidence.json` retains resolved values, attachments, SHA-256 hashes, conversion errors, Unity geometry and actual hardpoint mappings. All 34 audited ALO/DDS/XML hashes remained unchanged.

## Conversion and materials

- Blender `3.6.23`, ALAMO upstream importer, existing Blender MCP protocol `13`, safe mode enabled. No source geometry or UV edits.
- Repaired all binary material slots/shader/texture assignments before export; preserved authored hidden flags, original bones and hierarchy. Source helpers remain in FBX/blend.
- Twelve FBXs: hull `10 meshes / 53,187 triangles / 68 bones`; each heavy turret `7 / 1,292 / 11`; each medium turret `4 / 1,001 / 6`; death `8 / 40,865 / 39`.
- Blender ALO→FBX geometry/bone discrepancies below `0.001` source units. Maximum Unity bone error `0.0000039321003` before gameplay scaling; UVs, submeshes and parents verified.
- Opaque slots use `EmpireAtWar/Ship Lit`; separate additive windows/engine effects. Normal maps are linear normal imports with green-channel flip. Every FBX slot has an explicit external material remap.
- Engine follow-up: mesh effects use URP Unlit; both engine overlays render two-sided with depth writes off. Outer blue glow uses HDR RGB ×2; all ten exhausts retain original positions, geometry, UVs and textures. Source engine meshes contain `540/540/1,400` triangles and remain enabled. Opaque-only and glow stern views confirm intact shells; dark casing bands are authored texture/shading. Source `MeshShield.fx` wave/distortion textures remain in the editable archive; Unity uses a static texture approximation.
- Imperial grey albedo retained; inverse source alpha provides the source colorize mask, `_TeamMaskStrength=0.2`, `_TeamLiveryStrength=0`. Living and wreck materials preserve subtle owned color tint rather than introducing Republic red paint.
- FBX import scale `0.02`; visual geometry scale `11.8270273`. Visible size approximately `54.727 × 26.057 × 120` project units; bow `+Z`, up `+Y`; gameplay/placement root and banking body at identity.
- Turrets use the actual source attachment bones; nested FBX transport basis is cancelled before attachment. No duplicated embedded turret geometry.
- Scoped helper pass removed `32` disabled helpers / `57,278` triangles from the five new prefabs; retained real gameplay `ShieldSurface` and all visible geometry. Repeat pass found zero strippable helpers. Editable packaging removes only unused datablocks in separate background Blender processes; source objects, hierarchy, geometry, UVs and material assignments have identical fingerprints.

## Gameplay mappings

| Value | Source | Project decision |
| --- | --- | --- |
| Hull / shields | 15,000 / 15,000 | Same values |
| Cost / population | 17,200 / 26 | Same raw values; balance remains provisional |
| Build time | 70 s strategic / 55 s tactical | 70 s in the project's single production field |
| Availability | Starbase 2; tech-dependent fighters | Project level 2; fixed user-approved tech-2 complement |
| Movement | Max speed 3; EaW acceleration | Republic donor speed 18 units/s, rotation 7.5, turn acceleration 7.5 |
| Shield refresh | 300 in source units | Donor 15 hull units/s, 3 s delay; no unverified unit conversion |
| Hull length / navigation | Source-space dimensions | 120 / 72 project units; ±15° banking envelope fitted |
| Fighter / bomber | Starting 1/2; Reserve fields 3/6 | Max-active 1/2; launch budgets 3/6, including first launches under `HangarModel` semantics |
| Transport | Gozanti 1/1 at tech 2 | Omitted with user approval |
| Launch timing | Source spawn delay 0.5 s | Donor initial 4 s / interval 8 s; exit below hull by 8 units |
| Max build count | No matching source field | Existing project cap 3 |
| Intensify Firepower | FULL_SALVO, 15 s / recharge 60 s; source heavy battery delay ×0.33 | Own FullSalvo settings: every copied non-interceptable gun delay ×0.33, 15 s duration / 60 s recovery; damage/speed/shield regen unchanged |

- Copied Republic Venator's `15` weapons, firing arcs, system types and hardpoint health. Replaced only its ten `DualHeavyTurboLaserDby827` (`9`) mounts with existing Imperial `HeavyDualTurbolaser` (`43`, green, two shots).
- Preserved two heavy turbolasers (`2`), two heavy ion cannons (`11`) and the SPHAT beam (`12`). Those shared profiles were not recolored or modified.
- All `18` weapon/system targets use unique IDs `0..17`, colliders and health entries; every weapon is targetable. Fog/team/explosion references rebuilt for new geometry.
- Source garrison types use existing `TIEFighter=200`, `TIEBomber=201`; no fighter assets or shared fighter registrations changed. One destroyable shared hangar controls both bays.
- Source strategic upkeep `-250`, death refund dummy and Lua layer/DEFEND autofire are not additional declared Venator tactical abilities in this project. No unsupported strategic or Lua engine behavior added. Static turret geometry; no ALA animation conversion.

| Donor mount | New attachment |
| --- | --- |
| DualProjectile L1/L2/L3/L4 | Heavy turret 08/05/06/07 `FP_01` |
| DualProjectile R4/R1/R2/R3 | Heavy turret 01/02/03/04 `FP_01` |
| DualTurboLaser R/L | Medium turret 01/02 `FP_01` |
| Ion R/L | `Laser_Hardpoint_02_Fire` / `Laser_Hardpoint_01_Fire` — project ion mapping |
| Heavy turbolaser R/L | `Laser_01` / `Laser_08` — retain donor extra batteries |
| Beam / shield / hangar | `SPHAT` / `Shield_Generator` / `SPAWN_00` |
| Engines | Mean of ten authored `Pe_Venator_*` emitters |

## Assets and registrations

- Own visual/gameplay/death prefabs under `Assets/Prefabs/Models/Ships`; own wreck under `Assets/Prefabs/Models/Wrecks`; own preview under `Assets/Prefabs/Ui/Reinforcement`.
- Art under `Assets/Art/{Models,Materials,Textures}/.../EmpireShips/ImperialVenator`; dedicated baked shield, wreck materials, ship/wreck data and matchup asset.
- Empire roster only; `ShipsData`, `AssetMappingData`, existing view/data Addressables groups, `ShipUiData`, `TooltipIconData`, `ReinforcementData`, future icon generator and dedicated ability/audio mappings saved.
- Icon is an actual-model transparent `512 × 512` sprite; silhouette also saved. All three icon consumers and explicit hologram renderer bindings verified.
- Six Republic Venator registration records and five donor prefab/data assets plus metadata match Git HEAD. Existing Republic asset GUIDs preserved; unrelated parallel work retained.

## Verification and limitations

- `VerifyGeometry.cs`: all twelve Unity models match mesh/triangle/UV/submesh/bone hierarchy evidence.
- `Verify.cs`: 1,579 saved-asset checks passed, including engine visibility/materials/triangle counts; 18 targets / 15 weapons / two bays; authored muzzle/system displacement `0`; references, collider/banking envelope, unscaled 144-unit selection ring, placement, shield, wreck, compiled enum IDs and all registrations verified.
- Editor import/compilation finished; `EditorUtility.scriptCompilationFailed=false`; final Console interval contains no new errors. Earlier unrelated Balance/ISD replacement and transient squadron compilation errors remain outside this import. Serena's pre-existing `EditorToolInfo` diagnostic does not appear in Unity's compiled assembly checks.
- `output/ImperialVenator/Previews`: top/stern, eight living and eight wreck palettes, actual placement geometry. Contrasting blue/green and all living palettes inspected; source colours restored after rendering.
- No automated Unity test suite or combat Play Mode run. Runtime firing, hangar launches, ability cooldowns, placement and death behavior remain untested; numeric balance choices above remain provisional.
- `output/ImperialVenator/ImperialVenator-Converted.zip`: editable blends, FBXs, source-derived PNGs, conversion reports and previews. Source ALO/DDS files remain in the Workshop directory.

## Rebuild

1. From repository root: `uv run --with pillow python Tools/Blender/ImperialVenator/Prepare.py`.
2. On the inspected Blender MCP session: `uv run python Tools/Blender/ImperialVenator/Convert.py`; then `uv run --with pillow python Tools/Blender/ImperialVenator/Stage.py`.
3. Run Unity scripts sequentially with `unity command run_script --file Tools/Blender/ImperialVenator/<File>.cs --entry <Type>.Main --timeout_ms 120000 --timeout 125 --json`:
   `BuildArt/BuildImperialVenatorArt`, `BuildShip/BuildImperialVenatorShip`, `BuildPreview/BuildImperialVenatorPreview`, `StripHelpers/StripImperialVenatorHelpers`, `Render/RenderImperialVenator`, `Register/RegisterImperialVenator`, `VerifyGeometry/VerifyImperialVenatorGeometry`, `Verify/VerifyImperialVenator`.
   `RenderEngines/RenderImperialVenatorEngines` saves rear/upper/lower views with and without glow; `InspectEngines/InspectImperialVenatorEngines` records saved engine geometry and material state.
4. `uv run --with pyyaml python Tools/Blender/ImperialVenator/Evidence.py`; `uv run python Tools/Blender/ImperialVenator/Pack.py`.
5. Inspect native output success and diagnostics for every command. The conversion script clears the Blender session's generated objects; preserve unrelated live Blender work before rebuilding. These scripts are ship-specific.
