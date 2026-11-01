# Gladiator import

- Empire ship ID 216. Hull 6000, shields 5000, capital class/regeneration.
- Cost 4000, population 8, build 40 seconds, starbase level 4.
- Shared weapon profiles: 4x DualMediumTurboLaser (5), 2x ConcussionMissile (13), 2x PointDefense (14); ranges 517.5/550/200, radar 600. Shared firing cadence replaces raw XML cadence.
- Ten targetable hull-budget hardpoints; All six Empire squadron bays: TIE Fighter, TIE Bomber, TIE Avenger, TIE Interceptor, TIE Brute and TIE Punisher (10 carried per type, 1 active per type; 60 total carried, 6 active).
- Existing capital movement defaults: speed 25, turn 7.5. Source 2.5/0.15 is not directly converted.

## Rebuild

1. `uv run --with pillow python Tools/Blender/Gladiator/Prepare.py` audits installed source XML, binary slots and textures.
2. `python Tools/Blender/Gladiator/Convert.py` writes `Temp/GladiatorImport/ConvertMcp.py`. Send its contents to Blender MCP `execute_blender_code` in a fresh Blender 3.6.23 session. This follows the existing audited exporter, which clears scene objects. Save the returned `REPORT_JSON` as `Temp/GladiatorImport/Gladiator/ConversionReport.json` through the host, keeping Blender safe mode enabled.
3. `python Tools/Blender/Gladiator/Stage.py` stages source FBX/textures.
4. Run `BuildArt.cs`, `BuildShip.cs`, `BuildLivery.cs`, `Render.cs`, `Register.cs`, then `Verify.cs` using `unity command run_script --file Tools/Blender/Gladiator/<File>.cs --entry <Class>.Main --json`. Inspect nested `result.success` after each stage. Class names: BuildGladiatorArt, BuildGladiatorShip, BuildGladiatorLivery, RenderGladiator, RegisterGladiator, VerifyGladiator.
5. `uv run --with pillow python Tools/Blender/Gladiator/Package.py` verifies unchanged sources, lossless pixels, icon alpha and eight palette differences; packages editable conversion under `output/remake-empire-units/Gladiator-Converted`.

`Verify.cs` inspects saved assets; it does not run tests or enter Play Mode. Registration must follow BuildShip because the shared wreck builder initially assigns its default Addressables group.

## Verification

- Latest loadout revision: eight weapon mounts and ten targetable hardpoints verified in saved assets. Point defenses use mirrored DTL_02/DTL_05 source deck anchors, full yaw, HardPointType.PointDefense. Automated test counts below precede this loadout revision; tests were not rerun.

- Blender source/FBX geometry, UVs and bone hierarchy round trip passed: 10 meshes, 49 bones; max geometry error 0.000043589 source units.
- Unity saved references, eight targets, data/roster/icon/preview/wreck registrations, all mesh/material counts, external remaps and bone positions passed. UV channel coverage checked in Unity; exact UV corners compared in Blender only.
- 15 source-file hashes unchanged, 13 texture references pixel-identical; actual-model icon 512x512, transparent uncropped silhouette. Eight live/wreck palettes differ.
- Automated tests run on 2026-11-01: Ships 128/141, Combat 220/225, Health 342/343, Squadrons 81/81, Economy 75/75, Ui 232/232, Navigation 164/164, Audio 136/137, Core 134/134. Total 1512/1532 passed; remaining 20 failures concern other ships/shared behavior. All four parametrized cases naming Gladiator passed. Evidence: `Evidence/AutomatedTests.json`.
- Fixed two Gladiator failures: selection ring sprite-padding sizing/centering and stale explosion renderer references after stripe rebuild. `BuildLivery.cs` owns both repairs; `Verify.cs` rejects missing explosion references.
- No dedicated Gladiator test fixture exists. In-game combat, hangar, placement and damage acceptance remain pending; selected automated fixtures enter Play Mode internally.
- Full source shader animation/distortion is not recreated. Source-hidden helpers stay in FBX/blend and are absent from game prefabs.
- Unified `Tools/AloImport` Unity apply/render stages are unsupported; this import uses existing model-specific tools.

See the vault note `AssetPipeline/Imports/Gladiator Import.md` for source provenance, values and verification limits.
