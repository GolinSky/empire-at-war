---
type: engineering-reference
category: Optimization
verified: 2026-11-16
verification: Live Unity importer/API readback, metadata comparison, numerical
  texture/mesh screening and static reference/test review. No tests, Player
  builds or actual Mac rendering.
---
# Asset Import Audit — 2026-11-16

## Goal
- Audit textures, audio, meshes and large source files; apply moderate desktop compression while preserving quality exceptions.
- Report Read/Write candidates and unused-content candidates. No Read/Write changes or deletions authorized/performed.

## Important Values
- Unity `6000.4.7f1`, live target `StandaloneWindows64`; configured Windows D3D11/D3D12, macOS Metal.
- `Assets`: 15,529 source files excluding `.meta`, 3.800 GiB. Live importers: 4,690 textures + 598 models + 1,090 audio = 6,378.
- Initial effective uncompressed textures: 419; 221 reachable from declared scene/Resources/Addressables roots (135 UI, 77 model, six skybox, three VFX).
- Final effective uncompressed textures: **153**. Additional compressed: **266** (143 DXT1, 24 DXT5, 99 BC7).
- Models: **78 Off → Low**; final counts 556 Low, 21 High, 21 Off. Existing compressed models/textures unchanged.
- Texture payload arithmetic: −2,382,748,275 B across all retained textures; −250,411,532 B across declared roots. Most total savings concern vendor assets outside those roots; neither figure measures build size or simultaneous Player memory.

## Implementation
### Texture decisions
- Six `Assets/ThirdParty/Stagit/SkyboxEarthPlanets/textures/skyboxv1_stars_only/stars001.png`–`stars006.png` → Standalone DXT1; retain 2048² and mipmaps. Linear RGB RMSE 0.048–0.059/255; alpha error 0.
- `Assets/Art/Textures/Models/SeparatistShips/Providence/Providence_Hull_Specular.png` → Standalone DXT5; retain 2048²/mips/color space/GUID. RGB RMSE 0.184/255, alpha error 0. Explicit uncompressed default format previously defeated the compression checkbox.
- Providence Specular is reachable in static dependencies, but November 7 packed layouts excluded it. No new build establishes packed inclusion or byte savings for this texture.
- Preserve dimensions, mipmaps, color space, sprite identity, Read/Write and non-desktop settings. No Crunch, global resolution reduction, source edits or added streaming system.
- Existing gates: linear RGB RMSE ≤2/255, max alpha error ≤8/255; normal mean ≤1.5°, p99 ≤6°. All eleven retained normal maps remain below 3.3° using conservative percentile-bin upper bounds.
- Remaining 153: **45 historical quality exceptions**, **86 newly restored quality failures**, **16 incompatible block dimensions without resizing**, **two hazard data textures**, **four Editor/tooling exclusions**.
- Known examples kept uncompressed: AssaultFrigateMkII albedo, Pelta hull, Liberator hangar additive, Endurance HomeOne1 additive. Full preserved and rejected lists → linked outcome CSV/JSON.
- Only one initial imported texture was readable: vendor MPUIKit Editor background outside declared roots. All 1,306 declared-root imported textures had Read/Write off; unchanged.
- Model textures have mipmaps except the tiny 8×8 Praetor Superstructure TeamMask. Mipmap streaming remains off by existing policy.
- Sci-Fi UI Sprite Atlas has `bindAsDefault: 1` and uncompressed packing. Protected as potential build content; atlas packing was not changed/verified by this TextureImporter pass.

### Mesh decisions
- Low compression only for eligible Off importers; no escalation of existing Low/High assets, no decimation.
- Preserve local IDs, mesh/vertex counts, indices/topology, colors, bones and tangent signs. Compare all eight UV channels.
- Gates: position ≤0.05% bounds diagonal, UV ≤1/4096, normal/tangent ≤3°. Retained maxima: position ratio 7.87×10⁻⁷, UV 8.94×10⁻⁶, normals 2.9475°, tangents 2.9687°.
- Remaining 21 Off: 13 historical quality failures, five Rebel station exact-copy geometry contracts, two historical collider exclusions, one current collider source.
- Executor: 18.00 MiB source, 307 imported meshes, 888,262 vertices, ~86.84 MiB summed Editor diagnostic memory. Already Low; Read/Write remains on.
- AsteroidMiningFacility DAE: 99.86 MiB source, 32 imported meshes, 931,413 vertices, ~97.12 MiB Editor diagnostic memory; outside declared roots but used by authoring prefabs/tools. Already Low, Read/Write off.
- XQ6Platform OBJ: 13.19 MiB source, 196,370 vertices, reachable; already Low, Read/Write off.

### Read/Write review — no flags changed
- Initial 502 readable models: **498 project candidates**, **two collider holds**, **one vendor VFX hold**, **one source/helper review**. Of 498 candidates, 496 are reachable from declared roots.
- Largest candidates: Executor, AotrGolanIII, Procurator, MC80Independence, Praetor and ISDI.
- Collider holds: `Assets/Art/Models/SpaceStations/VanillaMiningFacilities/MiningFacility.fbx`; `Assets/Art/Models/DefendPlatforms/DefenseSatellites/Turret_Space_Base.fbx`.
- Separate review: vendor `ParticleProFX/Content/Mesh/beam.obj`; `WavecrestEngineHelper.fbx` source/helper.
- No imported-mesh CPU vertex/index reads, runtime combining, runtime NavMesh build or CanvasRenderer mesh assignment found in project runtime source. Wreck/fog read bounds; NebulaImpostor creates its own quad.
- Static scans do not fully resolve imported/nested hierarchies, batching, vendor behavior or Player requirements. Candidate list is **not a proven-safe disable whitelist**.

### Audio — settings retained
- 1,090 clips, 312.31 MiB source; 53 music tracks account for 247.04 MiB.
- All 53 music tracks: Streaming, Vorbis quality 0.7, preload off, background loading on.
- Other load types: 799 Compressed In Memory, 238 Decompress On Load. Codecs: 1,042 Vorbis, 47 PCM, one ADPCM.
- PCM exceptions total 3.48 MiB and last ~0.22–3 s; preserve short crisp effects unless listening/profiling supports a change.
- Review highly concurrent Compressed In Memory SFX/voices for DSP cost; current settings alone do not prove a defect.
- Largest music: `AOTC_Across_The_Stars.mp3` 12.72 MiB; `ROTS_Palpatines_Teachings.mp3` 12.42 MiB; `TPM_Jar_Jars_Introduction.mp3` 11.74 MiB. All streaming.

### Generated volumes and large source files
- `VioletPillarsField.asset`, `EmberWingsField.asset`, `AzureRiftField.asset`: ~60 MiB serialized each; live 512×320×48 RGBA32 Texture3D, one mip, Read/Write off.
- Each volume has 30 MiB raw voxel payload; 90 MiB if all three resident. Serialized size is not GPU size. Generator resolution/channel precision/lifetime requires separate review; unchanged.
- Other largest source files: `AsteroidMiningFacility.dae` 99.86 MiB; Sci-Fi UI `atlas.png` 64.03 MiB; mining-facility Glass Normal PNG 44.27 MiB; SpaceSkies skybox faces up to 30.25 MiB.
- Large PNG/WAV/source geometry does not imply a large packed asset. Initial Glass Normal imports as 2048² DXT5; Sci-Fi source atlas as 2048² DXT5, while atlas packing is separate.

## Edge Cases
### Unused-content findings — nothing deleted
- Expanded roots include ProjectSettings, all Resources, StreamingAssets, enabled scenes, explicit Addressables and include-in-build atlas content; 23 additional protected files beyond the initial scan.
- 5,217 content files / 1,390.65 MiB outside expanded roots. **Not an unused/deletable count.**
- Narrow shortlist: **1,210 files / 167.06 MiB**, no serialized GUID or literal filename references found. Includes source/demo content; owner/use review required.
- SpaceSkies Free: **67 files / 482.20 MiB**, outside roots, no external serialized consumers found. Strong group-level cleanup candidate.
- Stagit skybox unused subset: **170 files / 291.02 MiB**. Preserve active `stars_only` textures and material; do not remove the entire pack.
- HeavyDreadnought texture folder: nine files / 69.79 MiB outside roots but referenced by alternate model/wreck materials; review as a group.
- Largest project no-reference candidates: GangutStation Set03 MetallicSmoothness 7.71 MiB; SpaceStationModular FBX 5.86 MiB; Gangut Set01 5.56 MiB; Gangut Set02 5.02 MiB; HeavyTurbolaserCannon Surface ShipLit MetallicSmoothness 4.22 MiB. Exact paths/referrers in CSV.
- Preserve mining-facility DAE: AsteroidDebrisA/B and AsteroidRockMedium prefabs plus builder use it. Preserve HeavyDreadnought Blend: conversion/verification tools use it. Preserve Sci-Fi UI atlas include-in-build content.
- No-reference scans can miss constructed paths, directory enumeration, binary importer name references and external tooling. These inventories authorize no deletion.

### Verification and limits
- All 6,378 importers reread; inventoried non-compression properties unchanged. All Read/Write settings unchanged; 71 original exception metas byte-identical.
- All 266 retained textures preserve dimensions/mips/GUIDs and pass Windows/macOS format-validity APIs. Actual Mac rendering not run.
- Unity upgraded legacy importer serialization; four newly restored vendor textures retain equivalent serialization-only diffs. Sources unchanged.
- No new Console errors. Reimports surfaced Mediator/Bulwark LOD naming, HeavyDreadnought self-intersecting polygon/missing-normal and Venator/SpaceStationModular obsolete external-material warnings. Retained imported topology matches baseline; source repair is separate.
- Reviewed texture/sprite/material and exact retained-model fixture contracts across Map, Ships, Health, Combat, BattleInput, Squadrons, Ui, Navigation, Abilities, Core and Economy; no expected-value changes required. Detailed fixture list in evidence/report.
- Tests not run under the local override. No Player build, packed-layout measurement, listening check or matched runtime capture; no FPS/build-size/runtime-memory claim.

## Files
- Owner: [[TODOs/Optimization/Desktop_Asset_Compression_Plan]]; related [[TODOs/Optimization/Asset_Size_Followup_Plan]]; reusable rules [[Rules/EMPIRE_AT_WAR_OPTIMIZATION_COOKBOOK]].
- Retained outcomes: [SafeDesktop20261116.json](../../../Tools/DesktopAssetCompression/Evidence/SafeDesktop20261116.json).
- [All compression outcomes](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/compression-results.csv).
- [Full Read/Write candidates](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/mesh-readwrite-review.csv); [reasoning](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/mesh-readwrite-review.txt); [affected-test review](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/mesh-retained-review.txt).
- [No-reference shortlist](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/unreferenced-leaf-candidates.csv); [outside-root content and referrers](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/unused-content-candidates.csv); [unused report](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/unused-assets-report.txt).
- [Every source file by size](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/largest-assets.csv); [platform verification](../../../Tools/DesktopAssetCompression/Evidence/Audit20261116/platform-verification.json).
- Reproducible one-shot scripts: `Tools/DesktopAssetCompression/DesktopCompressionSafeTextures.cs`, `DesktopCompressionSafeMeshes.cs`. Local rollback snapshots and raw results: `output/DesktopAssetCompression/Audit20261116/`.

## TODO
- User visual acceptance on Windows/macOS; matched build/layout and Player-memory checks remain in the owning plan.
- Resolve readable candidates through hierarchy/Player validation before any future flag changes.
- Review unused groups and dynamic/tool dependencies before any future deletion.
- Keep quality/data/dimension/collider exclusions unless a separately verified solution preserves their contracts.
