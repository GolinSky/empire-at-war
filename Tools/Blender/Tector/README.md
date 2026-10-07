# Tector Star Destroyer

- Empire `ShipType.Tector = 210`; `TectorBoostWeaponPower = 32`.
- Guide: vault `Architecture/ALO_MODEL_IMPORT_GUIDE.md`, read before implementation.
- `Temp/ALO_MODEL_MAP.txt` was absent. `output/aotr-empire-units/unit-list.txt` maps `E_Tector_Star_Destroyer` to `Empire_Imperial_SD.ALO`; installed AOTR XML confirms it.
- Source: AOTR workshop `1397421866`; 21 model assemblies. Eleven common ISD I models reuse hash-matched, previously verified art; ten Tector attachments are converted.

## Gameplay

- Hull / shields / speed: **22,000 / 16,000 / 22**.
- Targetable: eight heavy 2-burst turbolasers, two medium long-range dual turbolasers, two shield generators, three engines, one tractor beam. **16 targets** with IDs `0..15` and explicit colliders.
- Non-targetable: seven medium 3-burst turbolasers, four light turbolasers, four heavy lasers. **25 automatic weapons**, **31 unique mount IDs** overall.
- User targetability takes precedence over the guide's default. Source dummy facing and extra hull hardpoints are excluded.
- No hangar hardpoint or fighter bays. The shared hangar component has zero bays and is non-destroyable.
- Shared weapon profiles: `43 / 53 / 38 / 33 / 28`; projectile tuning remains project balance rather than an exact AOTR recreation.
- Dedicated Boost Weapon Power: duration `20 s`, recovery `60 s`, fire delay ×`0.5`, damage ×`1`, speed ×`0.25`, shield regeneration ×`0`, incoming damage ×`1.5`. Existing Tractor Beam `22` restricts enemy movement using its current target/range rules.
- Provisional ISD II tuning: price `25,000`, build `500 s`, tech `3`, population `8`, limit `3`, turn speed/acceleration `5`, shield regeneration `20/s`. Source cost/build `22,000 / 440 s` are recorded separately in `SourceAudit.json`.

## Art and verification

- Own visual/gameplay/placement/wreck prefabs, ship/wreck data, matchups, transparent `512 × 512` icon and silhouette. All Empire/data/view/Addressables/HUD/tooltip/placement registrations resolve.
- Centered visible hull: `102.612 × 68.559 × 180` project units; roots scale `1`, bow `+Z`, up `+Y`. Banked vertical range `−38.620 .. 34.579`; navigation radius `109`.
- Three particle emitters use actual `Engine_L/R/M_Particles` bones under the banking body. Shield hull, collision, selection, fog, ownership and ion-field bindings fit this model.
- Restored `Hull_SS`'s four binary material slots: `712 / 116 / 330 / 318` triangles. Geometry/UVs stayed unchanged. Static mesh refresh preserves submeshes.
- Ten conversions preserve `43` meshes / `40,716` decoded triangles. Maximum Blender geometry/bone errors: `0.000122071 / 0.000085773` source units; Unity bone error ≤`0.000001986` project units. Mesh/triangle/submesh/UV/bone-parent checks pass.
- Binary slots/shaders/textures and visible/collision triangle counts match. ALAMO represents source shadow volumes as disabled helper surfaces; source shadow-volume effects are not reproduced.
- Removed `21` unused helper meshes (`8,378` triangles) from the new prefabs. FBX/blend source representations remain intact.
- Saved asset checks pass (`2,646` checks); zero mount displacement, missing scripts, broken references, or Console errors. All eight living/wreck palettes rendered; blue/green, icon, placement and wreck inspected. Original `41` source hashes unchanged.
- No automated Unity tests or Play Mode run. Combat, ability targeting, reinforcement deployment and destruction behavior remain unverified at runtime.

## Rebuild

1. Use a fresh Blender `3.6.23` MCP session; `Convert.py` clears that session's objects. Preserve unrelated Blender work.
2. Run `Prepare.py`, then `uv run --with pillow python Tools/Blender/Tector/StageTextures.py`.
3. Run `AuditMaterials.py --source-only`, `Convert.py`, `AuditMaterials.py`, then `Stage.py`.
4. Run official `unity command run_script --file Tools/Blender/Tector/<File>.cs --entry <Class>.Main --json`: `BuildArt` / `BuildTectorArt`, `BuildShip` / `BuildTectorShip`, `StripHelpers` / `StripTectorHelpers`, `Render` / `RenderTector`, `Register` / `RegisterTector`.
5. Inspect `Verify.cs` / `VerifyTector` and `VerifyGeometry.cs` / `VerifyTectorGeometry`; check Console and saved references.

- Working evidence and editable blends: `Temp/TectorImport/`; durable verification summary: `ImportEvidence.json`.
