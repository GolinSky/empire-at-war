# Zone relay sensor-node import

- Source: `GameData/Data/models.meg` → `DATA/ART/MODELS/NB_SPACESENSORNODE.ALO`; textures from `GameData/Data/textures.meg`.
- Imported on 2026-11-13 with Blender 3.6.23 and Unity 6000.4.7f1. Original files remain unchanged.
- `Prepare.py`: extract source and both diffuse/normal textures into `Temp/ZoneRelayImport`.
- `Convert.py`: run in an isolated factory-startup background Blender process. Imports through ALAMO, preserves all 10 bones and 5 meshes, reconstructs original vertex order, exports FBX and compares source/round-trip triangles, UVs and normals.
- Normal fidelity requires sharp-edge normal spaces and FBX `mesh_smooth_type='EDGE'`. Source smooth/custom normals remain assigned; omitting edge transport caused a 0.00369 normal-vector error after round trip. Final maximum error: 0.000212; UV error: 0 pixels; position error: 0.000024 source units.
- `Apply.cs` is a **one-shot migration from the original relay**. Do not rerun against the replaced prefab. Invoke with `unity command run_script --file Tools/Blender/ZoneRelay/Apply.cs --entry Apply.Run --json`.
- `Verify.cs` reads saved base/variant assets and renders only the geometry prefab into `Temp/ZoneRelayImport/UnityPreview.png`. This is an asset inspection, not a test-suite or Play Mode run.
- Runtime visual: `Assets/Prefabs/Models/Other/SpaceSensorNode.prefab`; two visible meshes, 1,088 triangles, no hidden collision helpers. `Shadow_02` is source-visible textured geometry and is deliberately retained.
- Material: `EmpireAtWar/Ship Lit`, diffuse texture with alpha ignored, linear normal map with DirectX green-channel flip. No new team-color behavior.
- Shared `ReinforcementZone.prefab` center visual replaced; `MainReinforcementZone.prefab` inherits it. Final bounds: `40.5774 × 48.5287 × 45.0621`; center `(0, 80, 0)`. Preserves previous maximum horizontal size.
- Raised center from Y `4.0249` to `80` after reported overlap with the single-zone mining asteroid. Relay bottom Y `55.7356`; inspected mining-prefab bounds reach at most Y `49.9556` at their authored origin. Capture-site asteroid top is Y `-12.7903`. Live in-game clearance remains a visual acceptance check.
- Capture radii, spawn-block radius, rings, UI and Addressables registrations remain unchanged. Art loads through existing zone-prefab dependencies.
- `Evidence/`: exact archive/member hashes, successful conversion measurements, saved base/variant verification. Unity preview inspected; no new console errors after import. Tests and in-game acceptance not run.
