---
category: Refactoring
status: done
completed: 2026-09-30
---
# Project Organization Remediation Plan

## Lifecycle Review

- Closed out: 2026-09-30; implementation committed in `3dcee4a6`.
- Verification: all 736 manifest destinations exist with matching metadata GUIDs; relocated SceneContext GUID also matches. Resources contains only ProjectContext, SceneContext, and DOTweenSettings bootstrap assets.
- Earlier completion record covers persistence/reference verification. This review performed read-only file/GUID checks; no Unity mutation, Play Mode, or automated tests.

## Goal

- Apply the exact type-first asset destinations and filenames in [[Done/Refactoring/Project_Organization_Remediation_Plan - Research]].
- Preserve GUIDs, referenced subasset fileIDs, importer mappings, renderer slots, sprite identities and visual settings.
- Finish with persisted assets, updated Editor path literals, and zero migration-induced reference/import/serialization failures.

## Important Values

- Research revision: **2026-09-30**; Unity **6000.4.7f1**, URP **17.4.0**.
- Audited scope: **736** files, **30** model sources, **251** materials, **407** texture files, **71** wreck materials.
- Exact Art/Resources manifest: **602 moves/renames + 134 retained files + 0 deletions**.
- Additional prefab relocation: **1** SceneContext prefab → total planned moves/renames **603**.
- Static file destination checks: **0** case-insensitive collisions; **0** occupied destinations. Recheck before execution.
- Hardcoded asset paths: **8** updates in **4** Editor scripts.
- Model material maps: **21** models with explicit metadata remaps; **9** need live identity capture before movement.
- Resources settings: **6** URP pipeline/renderer assets + **1** sample volume profile.
- Current status: **remediation plan implemented; all 603 asset moves verified at target, 134 KEEP assets verified, 8 C# paths updated, and empty obsolete folders cleaned up**.

## Rules

- PROJECT_ORGANIZATION and AGENTS.md govern placement, Unity tooling, serialization, metadata and testing.
- Read PROJECT_ORGANIZATION again before creating/moving asset folders. Read UI_UX_GUIDELINES before any UI code/prefab edits; UI_CODE_BUILD_GUIDE before adding UI features.
- Scope is naming/placement. Preserve pixels, file extensions, shaders, keyword/property values, texture scale/offset, renderer slots, source geometry and game behavior.
- Use official `unity` CLI and its Pipeline bridge; never legacy Coplay automation, unity-cli or unity-mcp-cli.
- Use Serena for C# symbol work; exact-text search for asset paths and source-file tokens.
- Do not run automated tests, enter Play Mode, build the player or modify unrelated user files as part of this plan unless explicitly requested.
- No OS/scratch/image/material deletion, deduplication, TGA conversion, lore correction or reference/shader repair bundled into migration.
- Do not alter Obsidian configuration, credentials, plugin state or authentication. Do not restructure AddressableAssetsData.
- File operations use AssetDatabase.MoveAsset; preserve metadata and check its returned error string.
- Retain a per-batch journal and pre-mutation copies; rollback only the agent's own asset/content changes.

## Decision

### Placement and naming

- Meshes and source companions: `Assets/Art/Models/<Category>/<Asset>/`.
- External material files: `Assets/Art/Materials/Models/<Category>/<Asset>/`.
- Model image files: `Assets/Art/Textures/Models/<Category>/<Asset>/`.
- Shared effects: `Art/Materials/Vfx` and `Art/Textures/Vfx`; UI imagery: `Art/Textures/Ui`.
- Categories: RepublicShips, SeparatistShips, SpaceStations, DefendPlatforms, Asteroids, Planets, Cannons, Other.
- Material name: `<Asset>_<PartOrSlot>[_Variant].mat`; texture name: `<Asset>_<PartOrSet>_<MapOrSource>[_Variant].<original extension>`.
- Wreck file/object name: `<renamed source material object name>_Wreck`; retain current wreck unit folders.
- Keep identity suffixes: Republic, HQ, OpenGL, ShipLit, Imported, numbered sets/slots, tiles and unknown Source tokens.
- Neutral names are deliberate: Acclamator Slot00/01, Lucrehulk Slot019–022, SpaceStationModular Slot00–05. Do not guess Core/Ring/Engines roles.
- Every research row is either MOVE/RENAME or KEEP. The manifest contains no wildcard deletion or rename authority.

### Preserve distinct assets

- Keep ArmorTest as renamed live Munificent armor texture; keep CaptionRex as renamed loading-scene image.
- Preserve both Arquitens material/color variants and all six Venator materials.
- Preserve all four Venator TGAs, Gangut Set01/02/03, Coruscant JPG/HQ PNG alternatives and both AcclamatorAssault texture sets.
- Keep FogOfWar variants, numbered grid sources, Test material, Render preview, stock image sources and hash-identical assets.
- Keep AO/source roughness/metallic separate from generated Occlusion/MetallicSmoothness textures.
- Preserve shared consumers even when the asset's original folder/model differs from the consuming model.
- No new external Hull material is created for AcclamatorAssault merely to make names conform.

## Implementation

### Phase 0 — Revalidate scope and preserve current work

1. Read both notes and current PROJECT_ORGANIZATION. Refresh filesystem/metadata/reference inventory; compare all source GUIDs with the research rows.
2. Inspect git status and relevant diffs. Record existing user changes and in-progress agent changes; do not overwrite or commit them incidentally.
3. Expand the complete per-file execution manifest: source path, final path, expected GUID, file type, action, batch, source-link edits and dependent path updates.
4. Include the additional SceneContext prefab GUID `78c9a2efe6e06854c820c86f9d0360a7`; inspect its consumers and destination.
5. Capture folder GUIDs and their consumers; distinguish old retained folders, new target folders and safe-to-remove empty folders.
6. Check all final paths case-insensitively against the whole checkout; reject duplicate targets, existing unrelated assets, missing metadata and wrong source GUIDs.
7. Save copies of every file/metadata item that a batch may change, plus source-token edits, material object names and relevant importer state.
8. Inspect official CLI schemas before unfamiliar commands; inspect Editor status and all open scenes with `unity command list_open_scenes --json`.
9. Dirty existing scenes or unknown scene state → stop affected Editor work; never discard/save someone else's scene changes or invoke save dialogs.
10. Create no asset migration tool until preflight identities and scope are concrete.

- Verify: exact row coverage; source files/GUIDs match; no target/folder collision; prior user work recorded; clean known scene state.
- Failure: update the affected research rows or pause the affected batch; do not substitute arbitrary assets.

### Phase 1 — Align the organization guide

1. Update PROJECT_ORGANIZATION project identity and stale Mirror/VContainer examples from verified live dependencies.
2. Document Audio/Music, Audio/SFX, Art/Shaders, existing Prefabs/Vfx and Prefabs/Light.
3. Record the chosen external-material/texture type-first roots, naming grammar and neutral-name/source-variant exceptions.
4. Record Plugins/Zenject and Plugins/Demigiant versus ThirdParty/TextMesh Pro; do not move third-party assets or change packages.
5. Define Resources bootstrap exceptions for ProjectContext, SceneContext and DOTweenSettings.
6. Keep Sandbox creation on demand; do not create unused prototype folders.
7. Remove alternative destination wording such as RepublicModels “or” RepublicShips and Station “or” SpaceStations.

- Verify: one exact destination policy agrees with the research manifest; no claim that unverified source assets have confirmed geometry/map roles.
- Current documentation task does not modify this guide; this is the first later asset-implementation prerequisite.

### Phase 2 — Capture live Unity identity and resolve importer gates

1. Capture every affected imported object's GUID/local fileID, type and source identity using the Object overload of TryGetGUIDAndLocalFileIdentifier with a long localID.
2. Capture importer external-object maps and source material identifiers, material import/search settings and current renderer material slots.
3. Capture all source/wreck prefab slot pairs and their meshes/material identities without entering Play Mode.
4. Capture texture importer type, sRGB, normal-map settings, sprite IDs/local fileIDs, platform overrides, compression and atlas dependencies.
5. For the nine models with no explicit metadata remaps, establish the current material identity through imported objects and actual prefab/scene slots.
6. Retain existing source identifiers; do not rename internal object_0, Material.001, BLUETHRUSTER_CL, newmtl/usemtl names or DAE material/mesh IDs.
7. Where material search would change resolution after relocation, establish explicit remaps to the same original external material objects and verify them first.
8. For embedded materials, preserve embedded identities; do not extract/recreate materials or replace referenced subassets without a separately verified mapping.
9. Inventory all OBJ→MTL→image and DAE image links, including URL encoding, options, duplicate basenames, absolute paths and missing source files.
10. Block affected batches if exact identity cannot be established. In particular, capture the missing Providence/Recusant MTLs and pre-existing invalid exported source paths.

- Verify: every referenced mesh/material/sprite identity is captured; each affected source link is either exactly mapped or explicitly blocked.
- Failure: do not auto-repair shaders, generate missing companion files, clear material remaps or guess replacement textures.
- Research is static evidence; it does not replace this live Editor gate.

### Phase 3 — Implement a small explicit Editor migration operation

- Proposed file: `Assets/Scripts/Editor/Maintenance/ProjectOrganizationRemediationTool.cs`; create only after placement/preflight gates.
- Responsibility: validate explicit rows, move assets, patch proven source path tokens, align main material names, persist and produce before/after comparison.
- Keep one top-level type per file. Avoid generic migration frameworks, ongoing runtime services and automatic startup/import callbacks.
- Dry-run output: planned file/folder operations, failed preconditions, source edits, eight C# path updates and snapshot/journal locations.
- Journal each successful move and content edit; fail immediately on a nonempty MoveAsset error. A batch is not a transaction with automatic rollback.
- Create required target directories via Unity-aware APIs. Preserve any referenced folder GUID through an explicit mapping; do not delete folders by filesystem recursion.
- Handle case-only renames through a unique temporary Unity path and record both steps.
- If batching imports with StartAssetEditing, always StopAssetEditing in finally; do not load/check not-yet-imported targets until imports resume.
- Use material object-name changes only after import; mark modified material objects dirty and save them.
- No tests are added or executed merely to validate this reversible documentation/placement work.

- Verify: dry run covers exactly the intended rows, no overwrite/delete/conversion, explicit error handling, and an inverse journal.
- Keep existing safe project tooling where sufficient; the temporary class is optional if direct official CLI operations can perform the same recorded steps.

### Phase 4 — Execute bounded dependency batches

| Batch | Operations | Required verification before proceeding |
| --- | --- | --- |
| A — settings/context | Move seven Resources settings, Coruscant lighting asset and Installers/SceneContext prefab | Same pipeline/profile/lighting/context GUIDs; Graphics/Quality and scene references unchanged |
| B — shared VFX/UI | Move root effect materials, loose images, shader noise images and Art/Ui images; punctuation cleanup | All shared consumers and sprite identities preserved; associated Hologram/Engines/ShipShield literals updated |
| C — asteroid/cannon/platform sources | Asteroid01, HeavyTurbolaserCannon, XQ6Platform and AcclamatorAssault full file sets after importer/source gates | Same imported mesh/material identities and corrected proven source paths |
| D — Republic models | AWing, Acclamator, Arquitens, Delta7, both StarDestroyers, HeavyDreadnought, Thranta, Venator | Variant count preserved; source/ShipLit pairing; no lost fourth Venator texture; squadron path literals updated |
| E — Separatist models | Belbullab22, Lucrehulk, Munificent, Providence, Recusant | Slots/sets, live ArmorTest bindings, existing importer identities and all source-link gates satisfied |
| F — stations/other | SpaceStationModular, Freeport, OpenSpaceStation, Gangut, RefuelingStation, AsteroidMiningFacility, Vesta, JediCouncil, SciFiLamps, Moon/planets, Halo texture collection | Correct type roots; all source/material/image files retained; mining/cannon builder literals updated |
| G — wreck materials | Rename existing wreck material objects/files paired to renamed source materials | Same wreck GUIDs and renderer slots; builder-derived filenames now match |

- Batch lists define ownership; expand a unique batch ID for every MOVE/RENAME row before execution. No row may be executed twice.
- Batch B excludes the model-owned Lucrehulk and RefuelingStation root materials; execute those with E/F. KEEP rows have no mutation batch.
- Source/wreck companion moves execute with their source batch where possible; G handles only unprocessed wreck rows.
- Every file operation still comes from its exact manifest row.
- Use smaller per-model sub-batches inside D/E/F; do not migrate a blocked importer just to finish its faction.
- Generated XQ6/cannon maps are moved with their consumer/material batch, regardless of their current Asteroids/Gangut directory.
- Shared texture consumers are included in verification even when they belong to another batch.
- Source material + ShipLit copy + generated texture prefixes + existing wreck names form a dependent naming set.
- Rename corresponding wrecks in the same dependent sub-batch where practical; otherwise do not run conversion/wreck-generation tools between source and wreck rename steps.
- Patch C# paths in the same batch as their assets; preserve surrounding code and values.
- Preserve source file bytes except explicit source path-token edits; do not edit geometry or convert image formats.

### Phase 5 — Patch source links and naming dependencies

1. Update exact OBJ mtllib tokens to renamed retained companion files; preserve all usemtl keys and geometry.
2. Update proven MTL image path tokens to computed relative target paths; preserve map options, material identifiers and unknown links.
3. Update proven DAE image init_from values with relative paths and correct encoding; retain XML structure/namespaces and IDs.
4. Do not blindly normalize missing absolute DDS/PSD references to similarly named PNGs. Establish identity or keep the batch blocked.
5. Leave embedded FBX/BLEND source data intact; verify importer maps preserve current external assignments.
6. Set the main external material object's name to its final filename stem; do the same for existing wreck material objects.
7. Keep source filename + _ShipLit.mat pairs in the same destination material directory.
8. Confirm Autodesk converter output prefix/directory matches relocated source maps; do not rerun conversion merely to rename.
9. Apply all eight exact path rewrites from the research table.
10. Search project-owned code, source companions and configuration for old executable paths; historical docs/source-origin labels may remain only where explicitly retained.

- Verify: companion links resolve where previously proven; importers retain the same mappings; existing tools address final filenames without generating duplicate assets.

### Phase 6 — Import, persist and compare every sub-batch

1. Resume imports if paused; refresh with synchronous import and wait for imports/recompilation to settle.
2. Re-serialize only the explicit changed serialized asset/importer paths and verified affected serialized consumers. Never call parameterless ForceReserializeAssets.
3. Save all modified asset objects with SetDirty + SaveAssets in the operation; do not require manual Editor focus or Ctrl+S.
4. If intentionally modifying a prefab, load/save/unload its contents with PrefabUtility and SaveAssets. Do not open/reload unrelated scenes.
5. Save any intentionally changed scene through scene-specific APIs only after its clean baseline and authorized changes are established.
6. Check Console for new import/serialization/reference errors and compare with the captured baseline.
7. Verify every moved file now resolves at the final path with the original GUID; verify all referenced local fileIDs still resolve.
8. Compare renderer material order, imported external maps, material property values, sprite IDs/import settings and source-image dimensions.
9. For pure image moves/renames, compare hashes: all original bytes must match. Source-token and material-name edits are the only allowed content changes.
10. Recheck C# compilation and migrated asset persistence. Complete verification before starting the next batch.

- Order: `Refresh → explicit ForceReserializeAssets paths → SaveAssets → Console/reference comparison`.
- Do not reserialize raw PNG/JPG/TGA/OBJ/DAE/FBX/BLEND source bytes as a substitute for import; include only supported serialized assets/importer metadata where required.
- ForceReserializeAssets runs as a direct operation, not from OnEnable or import callbacks.
- Verify persisted files on disk; an in-memory successful move is insufficient.

### Phase 7 — Final audit and completion

- Confirm 602 scoped Art/Resources moves/renames and the one additional context move, or document any refreshed manifest difference.
- Confirm all 134 KEEP files still exist with unchanged identity/content, except explicitly documented current-user changes.
- Confirm all 30 model sources, 251 materials, 407 images, 71 wreck materials and 10 source MTLs remain present; no unintended duplicates/deletions.
- Confirm no images/external materials remain in obsolete model bundle locations, except the explicitly retained OS junk awaiting separate cleanup.
- Confirm Art/Ui image relocation, Resources settings relocation and exact category naming; referenced old folders are not deleted implicitly.
- Confirm no new missing mesh/material/texture/sprite references, changed subasset IDs, importer errors or serialization errors.
- Compare active shader references against the pre-existing unresolved-token baseline; do not claim zero total existing failures without evidence.
- Confirm GraphicsSettings/QualitySettings retain pipeline GUIDs and the three Resources bootstrap assets remain.
- Confirm all eight Editor path updates compile and naming-dependent tools resolve their expected assets through read-only inspection.
- Review git diff for unintended property/geometry/import-setting changes and unrelated user work.
- Update both notes with completed batch results and remaining exceptions; remove any temporary Editor tool through Unity-aware tooling only after verification.

## Edge Cases

- Existing dirty/untitled scene → no test, Play Mode, open_scene, scene reload/discard or automated save dialog.
- Losing metadata → abort/restore original metadata; do not accept newly generated GUIDs as a repair.
- Referenced imported localID changes → stop the batch and restore original paths/source/importer state.
- A filename rename cannot convert TGA to PNG or establish that a map is Albedo/Normal/Emission.
- Importer GetExternalObjectMap returns a copy; map edits require actual importer API application and persistence.
- Duplicate hashes do not authorize deduplication; zero scanned consumers do not authorize deletion.
- An existing URP sample GUID in package cache does not prove that reference is imported or valid in the project.
- Existing shader/material assignments may be unusual or inactive; placement changes must not silently repair them.
- Windows case-only rename and folder splitting need explicit intermediate/folder operations; never issue a broad filesystem move.
- Moving Resources settings does not prove a specific memory/build-size saving; other references may still include them.

### Rollback

1. Stop further mutations; ensure any paused import scope exits through finally.
2. Reverse successful file moves in journal order, newest first, preserving the same metadata.
3. Restore only agent-edited source path tokens, material names, importer changes, C# paths and serialized contents from the pre-batch copies.
4. Refresh/import restored paths, explicitly reserialize affected supported assets and SaveAssets.
5. Compare restored GUID/localIDs, importer maps, renderer/sprite references and Console with the original baseline.
6. Preserve unrelated user changes; never use repository-wide reset/clean or restore all scenes.
7. Record the failed batch and exact gate; do not rerun blindly.

## TODO

- [x] Independently audit the current asset tree, metadata, references, filename dependencies, hashes and image dimensions.
- [x] Replace the incomplete example naming catalog with a complete per-file proposal.
- [x] Remove unsupported deletion, format-conversion, duplicate and material-role assumptions.
- [x] Check per-file proposed target uniqueness and current target availability.
- [x] Revalidate current working tree and complete live Unity preflight.
- [x] Align PROJECT_ORGANIZATION with the exact chosen conventions.
- [x] Resolve the nine importer/source-identity gates and record folder mappings.
- [x] Implement and verify bounded migration batches.
- [x] Perform final persistence/reference audit and update completion records.
- [ ] Optional separate work: source/shader reference repair, unused-asset cleanup, deduplication or format conversion.
- [ ] Automated tests only after an explicit user request and AGENTS.md scene-safety checks.

## Files

- [[Done/Refactoring/Project_Organization_Remediation_Plan - Research]] — exact 736-file manifest, additional prefab move, metadata evidence, bindings, duplicate groups, baseline failures and path rewrites.
- [[PROJECT_ORGANIZATION]] — authoritative organization policy; implementation alignment target.
- `AGENTS.md` — metadata/persistence, official CLI, Serena, UI note and no-test rules.
- `Assets/Scripts/Editor/Rendering/ShipLitSetupTool.cs`, `AutodeskMaterialConverter.cs`, `ShipWreckBuilder.cs` — filename-derived behavior.
- `Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs`, `Assets/Scripts/Editor/CaptureSites/*.cs` — eight asset-path rewrites.
- `ProjectSettings/GraphicsSettings.asset`, `ProjectSettings/QualitySettings.asset` — pipeline reference verification.
- Proposed temporary tool: `Assets/Scripts/Editor/Maintenance/ProjectOrganizationRemediationTool.cs`; not created by this planning task.
