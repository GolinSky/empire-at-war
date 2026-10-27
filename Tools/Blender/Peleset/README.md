# Peleset-class Cruiser

- Source: Republic at War workshop `1129810972`, unit `TU_Ordance_Cruiser`, `Units_Space_cis_technounion_cruiser.xml`.
- Workflow: vault `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE`.
- Source models: `SeV_Techno_Union_Ord_Cruiser.ALO` and separate `_Turret_01.ALO` / `_Turret_02.ALO`, mounted at `T01` / `T02`.
- Preserve original ALO/DDS/XML files. `Prepare.py` records binary bones/materials, exact texture references and SHA-256 hashes.
- Hull normal is the binary-referenced `SeV_Techno_Union_Tambor_Cruiser_N.dds`; do not substitute a similarly named texture.
- Right point-defense attachment is `FP_B-R_Bone` (source XML spelling), not `HP_B-R_Bone`.

## Conversion

1. `uv run --with pillow python Tools/Blender/Peleset/Prepare.py`
2. Start the compatible Blender 3.6.23 MCP session with `Tools/Blender/Start-Blender.ps1`.
3. `python Tools/Blender/Peleset/RunConversion.py`
4. `python Tools/Blender/Peleset/VerifyGeometry.py`
5. `python Tools/Blender/Peleset/Stage.py`

- `RunConversion.py` resolves the source audit into a temporary safe-mode payload and collects the JSON report from MCP output.
- Conversion requires a fresh Blender session; importer cleanup uses global object data. The parent guide's existing-session inspection remains mandatory.
- Reports and editable blend/FBX output: `Temp/PelesetImport/`.
- Verified: 104 hull bones, 5 bones per turret; source parent hierarchy retained; maximum raw bone displacement `0.000119` source units; FBX UV error `0`.

## Unity

- Run builders serially with `unity command run_script --file Tools/Blender/Peleset/<File>.cs --entry <Class>.Main --timeout_ms 60000 --json`.
- Order: `BuildArt.cs` (`BuildPelesetArt`) → `BuildShip.cs` (`BuildPelesetShip`) → `Render.cs` (`RenderPeleset`) → `Register.cs` (`RegisterPeleset`) → `Verify.cs` (`VerifyPeleset`). Inspect actual class names before rerunning.
- Art uses `SeparatistShips/Peleset`; visual/gameplay use `Assets/Prefabs/Models/Ships/Separatist/`.
- Length `124.6333` project units is provisional, matching the normalized Recusant. Gameplay root has identity scale; bow is `+Z`.
- Source collision/shadow helpers stay in transport files but have no runtime renderers. Opaque hull uses `EmpireAtWar/Ship Lit`; source blue paint uses the existing team-livery path.

## Gameplay choices

- Hull `7000`; shields `3000`; cruiser regeneration tier `90 shields/s`.
- Abilities: shared `BoostShieldPower` and `BoostWeaponPower`, matching Rothana's power abilities.
- 15 targetable mounts: 6 dual turbolasers, 2 point defense, 2 ion, 2 concussion launchers, 1 proton launcher, engines, shield generator.
- All weapon calibers Medium. Shared weapon-family profiles define combat balance; source range/burst/recharge values remain provenance, not new weapon profiles.
- Weapon HP `400`, two attached turrets `500`, engines/shield `800`. Per-mount `healthOverride` preserves default health for other ships.
- Economy: cost `6000`, build `60 s`, level `4`, population `4`; provisional maximum count `10`. Movement maps source speed `1.9` to `19 units/s`, source turn `0.3` to `30 degrees/s`.
- Armor-type strings and planet prerequisites are omitted. Physical scale and movement conversion remain provisional.
- Saved-asset checks and rendering do not establish in-battle balance. Automated Unity tests and Play Mode require a separate explicit request under the import guide.

## Verification (2026-10-27)

- `Evidence/` preserves source audit, conversion, geometry, fitted bounds, socket mapping and saved-asset verification results.
- Saved verification passed: all 15 hardpoints, actual initialized HP, shared Medium profiles, cruiser regeneration, own prefab/data/icon/preview/wreck references, CIS roster and Addressables registrations.
- Per-mount HP is propagated into both live combat and the battle simulator. Upgrade and save/restore retain it.
- Icon, placement, wreck and contrasting team-color renders inspected. No new Unity Console errors during integration.
- Focused health and simulator regression cases added; automated Unity tests and Play Mode were not run.
