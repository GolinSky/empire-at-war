# Planet decorations

- Source: original base-game `models.meg` and `textures.meg`; extracted unchanged ALO/DDS and editable packed Blender files remain in ignored `Temp/PlanetDecorationsImport`.
- Six static models, no colliders, no shadow casting/receiving. URP Lit materials are instancing-enabled; scenery remains visible through gameplay fog and has no minimap markers.
- Source alpha slots use double-sided cutout at 0.35; visual acceptance remains pending.
- Source geometry, oriented triangle winding and UV comparisons use the existing ALAMO worker. Blender split-normal quantization tolerance is 0.01; unrepresentable legacy split normals are explicitly reported per mesh. Zero normals derive from the first nondegenerate incident face.
- Scoped transport limitation: `EB_DOCK_BREAK` mesh 4 (`L_iBeam_2`) loses one triangle on FBX roundtrip. The source Blender mesh passes geometry/UV checks. The affected FBX slot records the count discrepancy rather than claiming fidelity; visual review pending.
- Junk footprints stay inside the centered 70% of map width/depth; outer 15% on each edge remains empty of junk. Docks and containers retain full-map placement.
- Radius is the farthest planar corner of visible bounds relative to prefab origin, plus 5 source units. Placement multiplies this by instance scale.
- Docks: intact 3–5, broken 1–2, scale 0.10–0.14. Junk: large 1–2, medium/small 1–3 each, scale 0.06–0.10. Containers 3–6, scale 0.5–0.8. Height jitter 15 units.
- Seed 7 / two-player inspection: Kuat 5/4/4, Tatooine 3/7/7, Kessel 3/3/3 for Small/Medium/Large. This is an asset inspection, not automated test-suite execution.
- Unity imports and saves completed; recompile status completed without errors. Larger/interior tuning compile completed with no errors; no new console errors after cursor 1141. Historical unrelated planet shader errors remain in console history. Tests not run.

## Reproduce
1. `python Tools/Blender/PlanetDecorations/Prepare.py`
2. Owned Blender 3.6.23 background process: `--background --factory-startup --python Tools/Blender/PlanetDecorations/Convert.py` (ALAMO and FBX add-ons required).
3. `unity command run_script --file Tools/Blender/PlanetDecorations/BuildArt.cs --entry BuildPlanetDecorationArt.Main --timeout_ms 180000 --json`
4. `unity command run_script --file Tools/Blender/PlanetDecorations/BuildSets.cs --entry BuildPlanetDecorationSets.Main --json`
5. `unity command run_script --file Tools/Blender/PlanetDecorations/InspectPlacement.cs --entry InspectPlanetDecorations.Main --timeout_ms 180000 --json`

