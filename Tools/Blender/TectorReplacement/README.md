# Tector Remake visual replacement

- Completed: 2026-10-12. Read vault `Architecture/ALO_MODEL_IMPORT_GUIDE.md` before implementation.
- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Art/Models/Empire_Imperial_SD_Tector.ALO`; textures from the adjacent `Textures/` directory.
- User decision: use the new hull without turret meshes; no Remake loadout or balance is copied. Follow-up cleanup removed unused legacy hull/turret assets from the project.
- Existing visual/gameplay/placement/wreck prefab GUIDs and icon/silhouette metadata are preserved. No data or Addressables changes.
- Live hull/shields/speed remain `8,000/8,000/22`. All 25 weapons, 16 targetable hardpoints, 31 mount IDs/transforms, firing arcs, abilities and core component configuration remain unchanged.
- Visible hull: `100.997742 × 47.93703 × 180.000031` project units; root scale 1, bow +Z, up +Y. Existing conservative collision, ion, navigation, selection and height values remain unchanged.
- Renderer references rebuilt; shield hull re-baked in place; three existing engine particle effects moved to the new source engine anchors. Opaque wreck and hologram placement use the same hull.
- Original ALO and all 30 source textures retain their hashes. PNG copies preserve decoded RGBA pixels; originals are never modified.
- Blender 4.2.16 with the already installed adapted importer. Corrected nine material definitions that inherited unrelated normal maps; restored the verified identity `Root` to preserve all 142 source bones.
- FBX preserves 31 meshes/101,098 decoded triangles, 142 bones and exact UVs. Maximum round-trip geometry/bone errors: `0.000126138 / 0.000063203` source units. Collision/shadow helpers are the importer's decoded representation.
- Helpers and engine-glow planes remain in Blender/FBX; unit prefabs use 24 visible hull/effect meshes. Wreck/placement use 18 opaque meshes; turret meshes used = 0.
- Opaque materials use Ship Lit; source normals use linear normal-map import with the established Remake green flip. Additive hull lights use URP particle materials. Animated EaW shader effects are not reproduced.
- Living hull retains the existing 0.6 ownership rim; eight live palettes differ. Neutral source wreck has no recolorable paint/mask; all eight wreck previews intentionally share its neutral appearance.
- Verification: 579 core-data hashes, 46 component configurations, 31 hardpoint transforms, four prefab GUIDs, icon metadata, 30 lossless texture copies and 437 serialized references passed. Blender/Unity mesh, UV, hierarchy and material remaps passed.
- Legacy cleanup: 21 FBXs, 31 materials and 36 textures removed with metadata after a zero-consumer dependency audit. Freed 52,999,673 bytes; 666 retained file hashes and 437 saved references verified; removed GUIDs absent from all remaining Assets files.
- No automated Unity tests or combat Play Mode were run by this replacement. The user's existing Battle session was allowed to finish before applying prefabs.

## Files

- Editable packed scene and four 1920 × 1280 Blender PNGs: `F:/CodexArtifacts/tector-preview/Tector-preview.blend`, `01-perspective.png`, `02-top.png`, `03-underside.png`, `04-stern.png`.
- Full evidence, asset backups and Unity previews: `F:/CodexArtifacts/tector-preview/UnityReplacement/`.
- Source FBX: `Assets/Art/Models/EmpireShips/Tector/Tector_Remake.fbx`.
- Art: `Tector_Remake_*` textures/materials in existing Tector folders; wreck materials in `Assets/Art/Materials/Wrecks/Tector/`.
- Active prefabs: `Tector.prefab`, `TectorShipView.prefab`, `TectorWreckView.prefab`, `TectorReinforcementView.prefab`.

## Tool order

1. Capture `InspectTectorReplacement.Main` and run `Stage.py` once before replacing the original visual; these create the immutable original baseline/backups.
2. Export the verified Blender hull to `Tector_Remake.fbx`, including rig/helpers and custom metadata; validate its round trip separately.
3. In Edit Mode run `ImportTectorReplacementArt.Main`, then `ReplaceTectorVisuals.Main` with official `unity command run_script --file Tools/Blender/TectorReplacement/<Class>.cs --entry <Class>.Main --json`.
4. Run `RenderTectorReplacement.Main`, `InspectTectorReplacement.Verify`, `VerifyTectorReplacementAssets.Main`, then `uv run --with pillow python Tools/Blender/TectorReplacement/Verify.py`.
5. Inspect rendered PNGs and import diagnostics; keep baseline/backups separate from new captures.

- `CleanupTectorLegacyArt.Main(false)` audits legacy art; `Main(true)` deletes only unreferenced old Tector assets through Unity and verifies retained hashes. Full report: `F:/CodexArtifacts/tector-preview/UnityReplacement/LegacyCleanup.json`.
