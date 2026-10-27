# Wavecrest-class Frigate

- CIS `ShipType.Wavecrest = 111`; source `Wavecrest_Frigate`, Republic at War workshop `1129810972`.
- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/XML/Units_Space_cis_Wavecrest_Frigate.xml`.
- Workflow: vault `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE`.
- Hull `SeV_Wavecrest.ALO`; eight separate `SeV_Wavecrest_Turret_01..08.ALO` at `HP_Turret_01..08`. Source XML uses uppercase socket aliases; binary bone case is authoritative.
- Original `RV_ASSAULTFRIGATE_HP00_E.ALO` from extracted base-game files is a hidden collision helper, retained in transport files without runtime geometry.

## Conversion

- Run `uv run --with pillow python Tools/Blender/Wavecrest/Prepare.py`.
- Existing Blender MCP session was inspected (3.6.23, Peleset scene). Conversion used an isolated background process of the same portable Blender because the importer mutates global object data; the live scene was preserved.
- `Convert.py` is a payload template: replace `__SOURCE_AUDIT__` with `repr(json.loads(SourceAudit.json))`, write `Temp/WavecrestImport/ConvertResolved.py`, then execute that resolved payload with portable Blender 3.6.23 `--background --python-exit-code 1 --python <payload>`. Do not execute the unresolved template.
- Then run `python Tools/Blender/Wavecrest/VerifyGeometry.py` and `python Tools/Blender/Wavecrest/Stage.py`.
- Original source hashes unchanged; three DDS-to-PNG conversions pixel-identical. Source bones/parents and UVs preserved; evidence contains geometry tolerances.
- FBX/blend output: `Temp/WavecrestImport/Output/`. Source helpers stay in transport files, without runtime renderer/mesh components.

## Unity

- Execute serially: `unity command run_script --file Tools/Blender/Wavecrest/<File>.cs --entry <Class>.Main --timeout_ms 60000 --json`.
- Order: `BuildArt.cs` (`BuildWavecrestArt`) → `BuildShip.cs` (`BuildWavecrestShip`) → `Render.cs` (`RenderWavecrest`) → `Register.cs` (`RegisterWavecrest`) → `Verify.cs` (`VerifyWavecrest`).
- Root scale 1; bow +Z/up +Y. Tall vertical crescent shape verified against source; visible size 21.0336 × 73.2647 × 90 units.
- Opaque hull uses `EmpireAtWar/Ship Lit`. Source green recoloring hue .37/range .07/saturation .2; yellow trim preserved. All eight owned palettes rendered.

## Gameplay

- Hull 3100; shields 1500; Wavecrest-only frigate regeneration config 30 shields/s. Shared frigate regeneration remains 25/s.
- 17 actual/bound targetable hardpoints: eight Medium Turbolaser, four Medium Laser Cannon, two Medium Ion Cannon, one Medium Proton Torpedo, engines and hangar.
- Weapon HP250 except ions600; engines400; hangar250. Initialized combat and simulator HP verified.
- Hangar: Vulture Droid Squadron102; reserve3 total launches, maxActive2, initial/interval5s. Launch point outside fitted hull.
- Cost3500; build45s; population3; station4. XML tech3 retained as provenance; the project has one Station Level gate, so station4 is binding.
- Provisional project choices: length90, speed29 units/s (XML2.9 ×10), yaw/acceleration60 degrees/s (XML.6 ×100), banking15 degrees, range550, maxCount10, HighMid height tier.
- Shared Medium weapon profiles determine damage/range/burst/reload; original projectile strings and XML timing are provenance, not custom weapon balance.
- Original armor strings, planet prerequisite system and unrequested PowerToShields ability are not ported. Audio reuses the existing Providence CIS profile; matchup claims are empty pending balance evidence.

## Verification

- 2026-10-27: saved assets/references, exact17 hardpoints, Medium profiles, actual/simulator HP, 30/s regeneration, Vulture complement, own data/view/icon/placement/wreck, roster/economy and Addressables all pass.
- Actual-model transparent512px icon, hologram, wreck and contrasting team colors visually inspected; source-to-FBX geometry/UV/hierarchy checks pass.
- No new Unity Console errors after import baseline cursor467. Scripts compile through `run_script`; no automated Unity tests or Play Mode run, per import guide.
- Evidence: `Tools/Blender/Wavecrest/Evidence/`; renders: `Temp/WavecrestImport/Previews/`.
