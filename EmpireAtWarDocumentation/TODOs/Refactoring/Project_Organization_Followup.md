---
category: Refactoring
status: todo
created: 2026-10-13
---
# Project Organization Follow-up

## Goal

- Align `PROJECT_ORGANIZATION` with current architecture and enforce type-first asset placement.
- Preserve asset GUIDs, subasset IDs, importer mappings, visuals and behavior.
- Scope: proposed remediation; no asset moves or source edits performed.

## Decision

- Chosen: correct policy gaps first, then migrate explicit per-file batches.
- Why: the guide's mandatory MVP folder split conflicts with AGENTS.md and the approved UI colocation plan.
- Avoid: broad script reshuffling, duplicate deletion, vendor cleanup, Addressables restructuring or Obsidian configuration changes.

## Important Values

- Audit: 2026-10-13; live filesystem, serialized asset headers, importer metadata and exact path searches.
- Confirmed misplaced files: 9 materials + 7 images under Models; 2 lighting settings under Scenes; 2 configuration assets directly under Settings.
- `Assets/Settings/AssetMappingData.asset`: path literals in 58 source/tool files (`.cs`, `.py`, `.ps1`); update active callers when moving.
- Resources: exactly ProjectContext.prefab, SceneContext.prefab and DOTweenSettings.asset; all allowed.
- Six nebula `.asset` files under Textures are Texture3D objects, not configuration assets.
- Two tracked Thumbs.db files were explicitly retained by the September remediation; separate cleanup decision.
- Tracked outside-vault documentation: 5 files under Docs and 6 under ObsidianDocumentation; output contains 1,122 tracked files.
- Audit is static; no Unity dependency scan, tests, Play Mode or build performed.

## Findings

### Guide drift

- `Scripts/BaseSystem` and `Scripts/Utils` exist but are undocumented; Utils has its own verified placement README.
- Entities contains views, presenters and installers; Components contains models and presenters. Examples: UnitActionsView/UnitActionsPresenter, ShipInstaller, HealthModel.
- AGENTS.md permits optional MVP, cohesive feature components and entity-owned composition.
- Approved [[Done/Refactoring/UI_Service_Refactoring]] colocates UI triplets and explicitly overrides this guide.
- Add Art/Fonts, EmpireShips, RebellionShips, model Shields/Vfx, audio mixer placement and generated Texture3D rules.
- Explain scene-owned LightingData and lightmaps separately from reusable `.lighting` settings.

### Placement and naming

- `Art/Models/RepublicShips/Venator/Materials`: 3 materials; organized Venator materials already exist with different GUIDs.
- `Art/Models/SpaceStations/SpaceStationModular/Materials`: 6 generically named materials; Slot00–05 materials already exist.
- `Art/Models/RepublicShips/Vwing/Textures`: 7 images. Choose neutral Source/Set tokens where semantic roles are unverified.
- `Scenes/Planets/Corusant/New Lighting Settings.lighting` and `Scenes/Planets/Kamino/New Lighting Settings.lighting` belong under Settings/Lighting with scene-specific names.
- `Settings/AssetMappingData.asset` and `Settings/UnitOrderSettings.asset` belong under Settings/Data.
- `Art/Models/SeparatistShip` contains only two Thumbs.db files plus metadata; its singular category is leftover debris.
- `Settings/Data/Balance/Presets` is empty with tracked folder metadata; determine whether tooling intentionally reserves it.

### Repository hygiene

- Docs/TODOs.md overlaps the canonical backlog; ObsidianDocumentation contains an advisory ship-movement plan.
- Define documentation ownership and repair links before archiving or removing anything.
- Graphify Markdown reports/history are tracked by deliberate .gitignore exceptions; review retention separately.
- output combines conversion sources, evidence and previews; classify reproducible scratch vs retained evidence before changing tracking.
- Untracked Python __pycache__ directories recur under Tools; propose targeted ignore rules.
- Do not treat Unclassified assets, absent Sandbox, bootstrap Resources, fonts, Texture3D assets or vendor folder layouts as automatic defects.

## Implementation

1. [ ] Update PROJECT_ORGANIZATION from verified current conventions and the approved UI plan; document BaseSystem, Utils and asset-type exceptions.
   - Verify: placement rules match AGENTS.md, the UI plan and representative live classes; no forced MVP migration.
2. [ ] Build a per-file manifest for the 20 misplaced assets: old/new path, GUID, subasset IDs, importer maps, consumers, hashes and naming-dependent tools.
   - Verify: unique unoccupied targets; distinguish separate asset identities from duplicate content; inspect FBX externalObjects and OBJ/MTL links.
3. [ ] Move art batches through AssetDatabase.MoveAsset; preserve source material remap keys and all existing identities.
   - Verify: GUID/localID and importer maps unchanged; materials, textures and renderer slots resolve; no changed geometry or pixels.
4. [ ] Move lighting/configuration assets; proposed targets: Settings/Lighting/Planets/<Scene>.lighting, Settings/Data/Repository/AssetMappingData.asset and Settings/Data/UnitOrders/UnitOrderSettings.asset.
   - Verify: update active source/tool path literals, preserve mapping entries and scene references; compile clean relative to baseline.
5. [ ] Review OS debris, empty folder purpose, documentation ownership and generated-output retention as explicit separate cleanup decisions.
   - Verify: no required tool inputs/evidence lost; no competing active backlog; links resolve.
6. [ ] After each asset batch: Refresh → explicit changed-path ForceReserializeAssets → SaveAssets → inspect Console and persisted files.
   - Verify: no new import/serialization/reference errors; retained GUIDs and source hashes; no manual Editor interaction.
7. [ ] Re-audit placement and update this note/backlog with evidence; move to Done only after agreed scope and verification finish.

## Edge Cases

- Existing working tree has unrelated source, prefab, settings and vault changes; preserve them.
- Different GUIDs or equal hashes do not establish that assets can be consolidated.
- Do not move scene-generated lightmaps/LightingData merely because they sit under Scenes.
- Do not change serialized field names, namespaces or source importer identifiers for folder consistency.
- Type splitting remains owned by [[TODOs/Refactoring/Type_File_Split_And_Placement]]; UI behavior changes remain in the UI plan.
- Before any Unity test, apply AGENTS.md scene-safety checks; no tests needed for this planning audit.

## Files

- [[Architecture/PROJECT_ORGANIZATION]]
- [[Done/Refactoring/Project_Organization_Remediation_Plan]]
- [[Done/Refactoring/Project_Organization_Remediation_Plan - Research]]
- [[Done/Refactoring/UI_Service_Refactoring]]
- [[TODOs/Refactoring/Type_File_Split_And_Placement]]
- `Assets/Scripts/Utils/README.md`
- `Assets/Scripts/Editor/AssetMappingContextMenu.cs`
