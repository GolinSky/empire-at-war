# Construction Pod

- Source: base game `GameData/Data/models.meg` → `DATA/ART/MODELS/EB_CONSTRUCTION_POD.ALO`.
- Model-only import. No gameplay, animation, roster or Addressables registration.
- FBX: `Assets/Art/Models/SpaceStations/ConstructionPod/ConstructionPod.fbx`.
- Visual prefab: `Assets/Prefabs/Models/SpaceStations/ConstructionPod.prefab`.
- Materials/textures: corresponding `Assets/Art/{Materials,Textures}/Models/SpaceStations/ConstructionPod/` folders.
- Native source scale retained; no gameplay size chosen. Unity visible bounds: `411.1422 × 431.9194 × 472.7024` units.

## Source fidelity

- 9 source meshes, 6,279 triangles, 22 bones. Duplicate attachment names retain distinct indexed identities.
- Visible prefab: `main body`, `alphas`, `scaf`; 4,508 triangles.
- FBX preserves helpers. Prefab removes shadow/collision renderer and mesh-filter components; source-hidden lights/electrical boxes remain disabled. Attachment transforms remain intact.
- Hull uses `EmpireAtWar/Ship Lit`; girders use transparent, double-sided URP Lit. DirectX normal map uses green-channel flip.
- Original hull contains 24 zero-length normals affecting 28 triangle corners. Only these normals are derived from area-weighted incident faces; original ALO remains unchanged.
- Shadow-volume helper normals are not representable faithfully in Blender; only that helper's normal comparison is excluded. Its geometry and UVs remain checked; source normals are retained in conversion metadata.
- Strict source/FBX checks pass for geometry, winding, UVs, material slots and bone hierarchy/positions. Valid authored normals and repaired hull normals pass strict comparison.
- Unity import, saved dependencies, visible triangle count and two rendered views verified on 2026-11-13. Automated tests not run per repository policy.

## Reproduce

1. Run `python Tools/Blender/ConstructionPod/Prepare.py` with Pillow installed.
2. Run portable Blender 3.6.23 with `--background --factory-startup --python Tools/Blender/ConstructionPod/Convert.py`. Requires installed ALAMO and FBX add-ons. Use an owned background process: shared conversion code clears all objects.
3. Run `unity command run_script --file Tools/Blender/ConstructionPod/BuildArt.cs --entry BuildConstructionPodArt.Main --json`.
4. Run `unity command run_script --file Tools/Blender/ConstructionPod/Render.cs --entry RenderConstructionPod.Main --json`.

- Extracted unchanged ALO/DDS, editable packed `.blend`, full conversion report and previews: ignored `Temp/ConstructionPodImport/`.
- Compact provenance, hashes and verification: `ImportEvidence.json`.
