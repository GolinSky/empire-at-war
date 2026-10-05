# T-65 X-Wing conversion

- Source: `../DATA/ART/MODELS/RV_XWING.ALO`, Deploy/Undeploy ALA, `RV_XWING.DDS`, `RV_XWING_GLOSS.DDS`, `W_LASER_SMALL.DDS`.
- `XWing.blend` contains the editable rig, original meshes, packed textures and S-Foils actions. `XWing.fbx` carries both animations and custom source metadata.
- Source/FBX/Unity: 14 bones, 10 meshes, 2,639 triangles; visible hull 1,732. Original files remain unchanged; `SourceHashes.json` records SHA-256 values.
- Original ALA: 31 frames at 30 fps. `Deploy` closes the wings; `Undeploy` opens them. Unity uses `CloseSFoils` / `OpenSFoils` Legacy clips.
- `Scripts/prepare_xwing.py` stages lossless PNGs and the binary bone audit. Run `prepare_xwing_animations.py` next from `F:/Private/empire-at-war`; execute its generated `Temp/XWingImport/ImportAnimations.py` through Blender MCP on a fresh ALO import in `XWing Source`. Then execute `export_xwing.py`. Never repeat root repair on a prepared rig.
- Unity visual: `Assets/Prefabs/Models/Squadrons/XWing.prefab`. Gameplay: `XWingSquadronView.prefab`; placement: `Assets/Prefabs/Ui/Reinforcement/XWingReinforcementView.prefab`. Unity assets and their existing `.meta` files are the authoritative saved integration.
- Five rigid mesh sections reproduce the single-weight skinned hull exactly at six sampled poses. Original hidden helpers remain in the FBX; the project uses its existing muzzle, engine and death effects.
- Rebellion squadron ID 300, laser ID 24, S-Foils ID 18. Five fighters with 60 hull, 20 shields and 3/s regeneration each; four lasers per fighter. Closed wings: speed ×1.3, fire delay ×3, regeneration ×3, no cooldown.
- Provisional project tuning: member length 4; cruise/combat 20/32 units/s, turn 110°/s, weapon range 45. EaW reference values are speed/minimum 4/2.5, turn 3, range 450; their unit scales are different. Economy uses tactical XML cost 500, build 15 s, level 1, population 1; queue limit 10 is provisional.
- `Previews/` contains open/closed poses and eight team palettes. Actual-model icon/silhouette are transparent 512 × 512. `*Verification.json` records geometry, texture, binding and registration readbacks.
- `Scripts/Integration/` captures the one-time Unity import/build/render/registration operations. Run in documented order: BuildArt → SplitHull → FitArt → BuildGameplay → Render → Register. SplitHull and Register are intended for a fresh integration; repeating them creates assets/entries again. Do not overwrite the already integrated assets with these scripts.
- No automated tests or Play Mode were run. Runtime combat, destruction, placement, toggle lifecycle and balance acceptance remain pending in vault `TODOs/Features/XWing_Import.md`.
- Disabled low-detail hull UVs may merge by ≤0.000344 during Unity import. Visible hull UVs match; the packed blend and FBX retain original UVs. Gloss texture is preserved as source data; EaW animated material/proxy effects are not reproduced.

See vault `GameDesign/X-Wing Import.md` and repository `Tools/Blender/README.md` for the integration reference.
