# Recalcitrant-class Light Cruiser

- Workflow: vault `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE`.
- Source: Republic at War workshop `1129810972`, `Units_Space_Cis_Recalcitrant_Light_Cruiser.xml`, unit `Recalcitrant_Light_Cruiser`.
- Hull: `SeV_Commerce_Guild_Light_Cruiser.ALO`; eight separate `SeV_Commerce_Guild_Light_Cruiser_TL_01..08.ALO` turret models at `TL_01..08`.
- Originals remain unchanged. Source audit records binary materials, bones, XML mounts and SHA-256 hashes.

## Conversion

1. `uv run --with pillow python Tools/Blender/Recalcitrant/Prepare.py`
2. `python Tools/Blender/Recalcitrant/RunConversion.py`
3. `python Tools/Blender/Recalcitrant/VerifyGeometry.py`
4. `python Tools/Blender/Recalcitrant/Stage.py`

- Uses isolated Blender 3.6.23; preserves the existing live Blender MCP scene.
- Source: 89 hull bones, nine bones per turret; original parent hierarchy retained.
- Visible source/FBX geometry and UVs match. Blender FBX reimport re-tessellates hidden hull `Col` with one fewer triangle; Unity retains the original 9,917. Transport files retain helpers; runtime prefabs exclude them.
- Editable `.blend`, FBX, source/FBX corner arrays and working reports: `Temp/RecalcitrantImport/`.

## Unity

- Run serially: `unity command run_script --file Tools/Blender/Recalcitrant/<File>.cs --entry <Class>.Main --timeout_ms 60000 --json`.
- Order: `BuildArt.cs` (`BuildRecalcitrantArt`) → `BuildShip.cs` (`BuildRecalcitrantShip`) → `Render.cs` (`RenderRecalcitrant`) → `Register.cs` (`RegisterRecalcitrant`) → `Verify.cs` (`VerifyRecalcitrant`) → `VerifyUnityGeometry.cs` (`VerifyRecalcitrantUnityGeometry`).
- `ShipType.Recalcitrant = 113`; CIS roster and existing Addressables groups.
- Own data, visual/gameplay, icon, placement preview, shield mesh, wreck, audio mapping and matchups.
- Blue hull stripes and purple turret markings use per-material team livery. Engine/window effects approximate source shaders with additive materials.
- Registry builder refreshes the dictionary wrapper's lazy editor caches after serialized row edits.

## Gameplay

- Hull `2800`, shields `2500`; shared Cruiser regeneration tier `90 shields/s`, delay `1 s`.
- Source Frigate classification retained independently from regeneration tier.
- All 17 mounts targetable: eight dual turbolasers, six concussion missile launchers, one dual ion cannon, engines and shield generator.
- Shared Medium profiles: `DualMediumTurboLaser` (5), `MediumMissileLauncher` (93), `MediumDualIonCannon` (37).
- HP: turbolasers `270` each; missiles `300` each; ion `575`; engines/shield `350` each. Per-mount overrides also reach the simulator.
- Source yaw widths: turbolasers `160°`, missiles `130°`, ion `35°`; turret/ion directions follow actual source muzzles. Missile outward bearings are provisional because source fire bones equal their mount sockets. Project hardpoints support yaw arcs.
- Cost `3600`, build `40 s`, available station level `3`, population `2`; provisional maximum count `10`.
- Provisional movement conversion: source speed ×10 → `22 units/s`; turn ×100 → `70°/s`; bank `10°`.
- Provisional length `100` project units; visible bounds `29.642 × 25.522 × 100`, navigation radius `55`.
- Armor strings and planet prerequisite omitted. Shared weapon profiles supply combat balance; source range/burst/recharge remain provenance. No hangar. Uses the existing self-activated `Power to Weapons` ability (`TIEAvengerPowerToWeapons`, ID `20`); shared catalog owns its effects and timing.
- Automated Unity tests and Play Mode were not run.

## Verification � 2026-10-27

- Saved references, all 17 initialized HP values, simulator HP, Medium profiles, cruiser regeneration, economy, Power to Weapons loadout/catalog/icon and faction registrations passed.
- Nine FBXs: 51 transport meshes, 161 bones; 31 visible surfaces. All 83,934 baked visible vertices match raw Unity FBX, maximum displacement `0.00000103` units; visible UVs/topology match.
- Hidden shadow UV quantization uses an explicit `0.001` helper-only threshold; visible UV threshold remains `0.00002`. See `UnityGeometryReport.json`.
- Icon alpha/crop, source silhouette, placement, wreck and contrasting team renders inspected. Thin antennae/spines/barrels match original source art.
- No Unity Console errors after integration. Automated tests and Play Mode were not run.
- `Evidence/` preserves source audit, conversion, geometry, saved-asset checks, hardpoint mapping and source silhouette.
