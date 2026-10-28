# Tibanna gas station

- Source: Workshop `1397421866/Data/ART/MODELS/NB_Tibanna.ALO`; original ALO/DDS unchanged.
- Blender 3.6.23 / ALAMO; fresh conversion scene, existing scenes preserved.
- `Prepare.py` audits source chunks, records SHA-256 hashes and losslessly decodes three DDS textures.
- `ConvertRequest.py` expands the existing mining converter and sends data in bounded MCP requests. Audit chunks are scene custom-property data, never executable code.
- ALAMO's global shadow-cleanup pass fails on unrelated-scene objects. The scoped converter accepts only this known failure after confirming every source mesh loaded; reconstructs source-index bone parenting and preserves helper geometry without welding it.
- All 30 bones preserved. Visible meshes: `Hull` (9,604 triangles), `lights` (144). Shadow (37,772) and collision (156) remain hidden in FBX; exclude them from gameplay geometry.
- Material slots: `TibannaGasStationSlot00` → `MeshBumpColorize.fx`; `01` → additive lights; `02` → shadow; `03` → collision. Source shader properties remain in `Evidence/ConversionReport.json`.
- Direct hull-alpha team mask follows the existing `MeshBumpColorize` integration convention; nonzero coverage **0.00543594%**. Preserve this small authored mask; do not invent paint coverage.
- Authored hull/light normals explicitly restored. Source triangle-position and UV error **0**; FBX maximum position/bone error **0.00003958** source units, UV error **0**.
- Blender visible size **349.549 × 349.549 × 434.720** source units; Unity axis conversion makes height the middle dimension.
- Outputs: `output/TibannaGasStationImport/Output/TibannaGasStation.fbx`, decoded textures, packed Blender session copy. Only scoped FBX/PNG assets are intended for Unity import; the session copy retains other open Blender scenes.
- Static geometry only; original shader animation/particles are not reproduced.

## Rebuild

```powershell
uv run --with pillow python Tools/Blender/TibannaGasStation/Prepare.py
python Tools/Blender/TibannaGasStation/ConvertRequest.py
Get-ChildItem output/TibannaGasStationImport/AuditRequest*.json | ForEach-Object {
    uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py $_.FullName
    if ($LASTEXITCODE -ne 0) { throw 'Audit upload failed' }
}
uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py output/TibannaGasStationImport/ConvertRequest.json > output/TibannaGasStationImport/ConvertResult.log
uv run --with pillow python Tools/Blender/TibannaGasStation/Verify.py
```

## Unity integration

- `MiningFacilityType.TibannaGasStation = 3`; Empire and Rebellion replace `CommonMiner` with this entry. Republic and Separatist retain the original miner. Capture-site facilities remain unchanged.
- Clone the original miner's full component data and catalog balance: 1,000 credits, 5 s build, limit 5, level 0, population 0; hull 1,500, shields 800, regeneration 24 after 3 s, income 20, spawn-block radius 400.
- Canonical asset base name: `TibannaGasStation`; factory, preloader and save validation resolve the same data/view keys.
- Dedicated visual/gameplay prefabs, holographic placement preview, wreck, shield mesh and actual-model icon. Visual footprint matches the original miner's maximum horizontal span; collider and effect bounds fit the new geometry.
- Existing Empire/Rebellion saves containing the removed `CommonMiner` roster entry are not migrated by this replacement.

```powershell
unity command run_script --file Tools/Blender/TibannaGasStation/BuildArt.cs --json
unity command run_script --file Tools/Blender/TibannaGasStation/Integrate.cs --json
unity command run_script --file Tools/Blender/TibannaGasStation/Render.cs --json
unity command run_script --file Tools/Blender/TibannaGasStation/Register.cs --json
unity command run_script --file Tools/Blender/TibannaGasStation/VerifyArt.cs --json
unity command run_script --file Tools/Blender/TibannaGasStation/VerifyRegistration.cs --json
```

## Saved-asset verification — 2026-10-28

- Unity 6000.4.7f1: asset import and C# compilation complete; final captured Console error count 0.
- Saved geometry: 35.99 × 44.76 × 35.99 units; donor maximum horizontal span 35.9888 units. Visible model: 9,748 triangles.
- Source conversion, saved prefab scripts/references/materials, hologram slots, dedicated shield/wreck, icon and faction/asset registrations passed scoped readback checks. Full component-data and income equality verified against original miner; catalog balance parity passed.
- Actual-model icon: transparent 512 × 512. Model, placement and wreck renders inspected, uncropped.
- All eight palette values exercised; renders are identical at this framing because the authored direct-alpha team mask has negligible coverage and the current shader has no team rim. No visible recoloring claimed or invented markings added.
- Regression cases added for mining asset names, faction roster replacement and balance parity. Automated Unity tests and Play Mode were not run, following import-guide scope.
