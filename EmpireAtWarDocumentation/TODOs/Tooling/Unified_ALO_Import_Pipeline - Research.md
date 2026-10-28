---
category: Tooling
status: todo
created: 2026-10-28
updated: 2026-10-28
tags:
  - alo
  - asset-pipeline
  - research
---
# Unified ALO Import Pipeline — Research

## Goal

- Preserve review evidence supporting [[TODOs/Tooling/Unified_ALO_Import_Pipeline]].
- Findings recorded 2026-10-28; implementation and acceptance tests have not run.

## Decision

- Consolidate existing mechanics; retain model decisions as data.
- Primary gap: no verified general source-to-registered-unit pipeline. Existing station configuration proves partial reuse is practical.
- Do not port historical bugs or stale configuration rules when extracting helpers.

## Implementation

### Reviewed guidance

- [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] — all steps and edge cases read: source audit, conversion, Unity sizing, gameplay, registrations, derivatives, team colors and persistence.
- [[AssetPipeline/Guides/SPACE_STATION_MODEL_IMPORT_GUIDE]] — full station workflow, attachments, common pivot, typed mounts, Rebel derived UV repair and unresolved appearance.
- [[AssetPipeline/Guides/REPUBLIC_SPACE_STATION_MODEL_IMPORT_GUIDE]] — embedded artwork, authored normals, repeated bone names, five-level integration.
- [[AssetPipeline/Guides/CIS_SPACE_STATION_MODEL_IMPORT_GUIDE]] — chunked normals, cutout materials, typed mounts and faction routing.
- [[AssetPipeline/Imports/MC80 Independence Import]] — source-material slot loss and shader-name collision.
- [[Architecture/PROJECT_ORGANIZATION]] — current placement rules; older guide paths are not authoritative.

### Repeated tooling roles

| Filename | Count under Tools/Blender |
|---|---:|
| Convert.py | 44 |
| Stage.py | 43 |
| Prepare.py | 43 |
| BuildArt.cs | 40 |
| Render.cs | 40 |
| Register.cs | 36 |
| Verify.cs | 28 |
| VerifyGeometry.py | 20 |

- Counts are repeated role filenames, not measured byte-identical duplication.
- Tools/Blender/Endurance/PrepareUnity.py reads ISDIRemake/BuildArt.cs and MC75Profundity/Render.cs; rewrites names and code fragments.
- Tools/Blender/Endurance/Convert.py rewrites ISDIRemake conversion source and executes the generated text.
- Tools/Blender/Allegiance/GenerateBuilders.py adapts Executor/BuildExecutorShip.cs through source replacements.
- Tools/Blender/Procursator/Convert.py patches export_isd_i.py; conversion can skip because ConversionReport.json exists.
- Tools/Blender/ISDIRemake/Convert.py also uses report-existence freshness; Tools/Blender/Titan/Stage.py skips existing textures.
- Consequence: changed source/tool/recipe can leave stale outputs; require content fingerprints and verified complete manifests.

### Existing reuse candidates

| Source | Candidate responsibility |
|---|---|
| Tools/Blender/AcclamatorAssault/Prepare.py | XML inheritance, model/hardpoint discovery, source hashes and bone audit |
| Tools/Blender/AcclamatorAssault/Convert.py | Binary-backed Root restoration, isolated conversion and snapshots |
| Tools/Blender/RebelSpaceStation/ | Binary reader, hierarchy/material reconstruction, attachment conversion and fidelity checks |
| Tools/Blender/MC80Independence/AuditMaterials.py and RepairSurfaces.py | Original material-slot/shader repair regression |
| Tools/Blender/Start-Blender.ps1 and mcp_call.py | Existing process/MCP transport; add explicit job ownership |
| Tools/Blender/SpaceStations/BuildStationLevels.cs and faction JSON | Shared level bindings, common pivot, mounts, shields and bounds |
| Assets/Scripts/Editor/Ship/ShieldHullBaker.cs | ShieldHullBaker.Bake |
| Assets/Scripts/Editor/Ship/ShipHullHeightBaker.cs | MeasureHull / MeasureRenderers |
| Assets/Scripts/Editor/Rendering/ShipWreckBuilder.cs | Scoped wreck geometry/material generation |
| Assets/Scripts/Editor/Rendering/UnitHelperMeshStripper.cs | Report / Strip with reference-aware removal |
| Assets/Scripts/Editor/Ship/HardPoints/ShipHardPointValidator.cs | Validate |
| Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs | Future squadron adapter; Build / Populate / SquadronViewSpec |
| Assets/Scripts/Editor/ShipIconGenerator.cs | Extract per-unit operation from fleet-wide generation |

- Candidate means inspect/test before extraction, not assume a safe general API.
- ShipIconGenerator has hardcoded MAPPINGS and a large GenerateAllShipIcons method; one import must not regenerate unrelated icons.
- Allegiance history records donor injection-binding failures; saved-asset validation must inspect injection as well as references.
- Existing helpers may exceed 200 lines; propose only focused extraction required by this pipeline.

### Known correctness traps

- ALAMO drops verified identity Root and can resolve duplicate-name parents incorrectly; original indices/matrices are authoritative.
- MC80 Hull lost 3 of 4 material slots; shader reuse made opaque meshes additive. A FBX round trip alone cannot detect it.
- Sparse HSV paint falls below automatic coverage thresholds; source alpha may select neutral panels. Require explicit livery policy and owned renders.
- Hidden helper names are insufficient for deletion: visible Hull_LOD_1 contains actual engine geometry.
- Attachment rotation comes from source transforms; FBX bone-axis orientation is not the source attachment rotation.
- Older Register.cs scripts write a direct reinforcement prefab reference; current guidance requires an Addressable AssetReference GUID.
- Historical test scene rules conflict with current AGENTS.md. Implementation follows current repository rules, not copied instructions.
- Historical per-unit health, loadout, dimensions and paths are comparison evidence; current assets/user decisions establish migration baselines.

## Important Values

- Recorded compatible converter environment: Blender 3.6.23; ALAMO commit 2b0fb0e34e4f451d2e380b042d88ad1a4c8a09b5; MCP 2.1.3 / protocol 13. Verify installed versions before implementation.
- Rothana source baseline: 13 meshes, 42,813 triangles, 100 bones; 10 visible meshes and 7 opaque wreck meshes.
- Endurance historical assembly: 15 converted pieces; 12 weapons + 10 systems; 40 original source hashes and 52 texture copies.
- Rebel station attached pieces by level: 5/8/11/14/18; 20 unique attachment models; shared-anchor movement limit 0.01 project units.
- MC80 Hull slot triangle counts: 2,519 / 1,895 / 1,829 / 1,332; total source 47 meshes / 137,320 triangles / 214 bones.
- These values are historical seeds, not freshly measured implementation acceptance.

## Edge Cases

- Advisory lock files cannot prevent raw/manual writes. Coordinated tools serialize; detect drift and document the boundary.
- OS lock release after caller death does not prove asynchronous Unity work stopped. Editor operation ownership/journal must survive.
- Unity saves multiple assets non-atomically; recovery should complete forward or report conflict.
- An agent can be waiting while another job changes a registry. Reload and merge owned keys under the Editor gate.
- Multiple machine access is outside v1; do not advertise distributed locking.
- No automated tests, Unity mutations, Blender conversions or new runtime behavior performed for this plan.

## Files

- [[TODOs/Tooling/Unified_ALO_Import_Pipeline]]
- [[TODOs/TODOs]]
- Tools/Blender/README.md
- Tools/Blender/Endurance/README.md
- Tools/Blender/SpaceStations/README.md
