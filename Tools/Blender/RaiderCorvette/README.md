# Raider Corvette

- Read vault `Architecture/ALO_MODEL_IMPORT_GUIDE`, `PROJECT_ORGANIZATION` and UI guidelines before integration.
- Source: installed AOTR workshop `1397421866`, `E_Raider_Corvette` → `Raider_Corvette.ALO`. `Temp/ALO_MODEL_MAP.txt` was absent; source catalog/live XML agree.
- `Prepare.py` audits model, DDS/XML files, binary bones, projectile inheritance and original SHA-256 hashes.
- Run `Convert.py` through the existing Blender 3.6.23 MCP connection. It creates fresh source/validation scenes and preserves other scenes. Save the printed `CONVERSION_JSON` payload to `Temp/RaiderCorvetteImport/ConversionReport.json`.
- `VerifyGeometry.py` compares both scenes; save `GEOMETRY_JSON` to `GeometryVerification.json`. Safe mode and telemetry settings remain unchanged.
- Run `Stage.py` with `uv run --with pillow python Tools/Blender/RaiderCorvette/Stage.py`; lossless PNGs and the authored alpha team mask enter the type-first art folders.
- Unity order: `BuildArt.cs` → `BuildShip.cs` → `Render.cs` → `Register.cs` → `Verify.cs`. Use `unity command run_script --file <path> --entry <Class>.Main --timeout_ms 60000 --json`; inspect nested `result.success`.
- `TrimBlend.py` runs in a separate background Blender process on the saved `RaiderCorvette.blend`; it removes unrelated scene data only from that output file. `Package.py` retains Raider-only editable art, credits, hashes, geometry/reference reports and eight live/wreck palettes.
- Source/FBX/Unity: four meshes, 10,301 triangles, 31 bones. Binary-verified identity Root restoration. FBX corner/UV error ≤0.00001526/0; Unity bone error ≤0.0000001245 units.
- Source collision/shadow meshes stay in FBX/blend; runtime prefabs retain their attachment transforms without unused rendering components. Visible hull length 24, bow +Z/up +Y; radius 15, bank ±15°, root scale 1.
- Empire ship `208`: hull/shields/speed `600/800/35`; four two-shot lasers, two dual repeating PD mounts, two two-shot heavy ion blasters and two five-shot heavy concussion launchers. All ten weapons are non-targetable; no hangar or fighters, as requested.
- Dedicated weapon profiles `60..62`; laser profile `40` reused. Source local +X maps to authored muzzle direction; cone centers are 0° for lasers/PD and −45°/+45° for ions/missiles. Limits are serialized relative to the banking parent.
- Pursuit `30` reuses the existing stat ability: speed ×2.5, damage ×2, fire delay ×0.5, shield regeneration ×0, active/recovery 15/60 s. Damage ×2 is a provisional user-required bonus based on Assault; source XML has no outgoing-damage bonus. Self activation requires no enemy target.
- Reference: vault `GameDesign/Raider Corvette Import`. Economy, movement, scale and matchup tuning are provisional. Passive source projectile debuffs, weapon energy and death-clone animation are outside this integration.
- Tests cover requested stats/loadout, arcs, registrations, saved references and repeated Pursuit activation/restoration. First run reproduced a cone-center bug; corrected assets passed eight Raider tests. See retained test reports for final verification.
