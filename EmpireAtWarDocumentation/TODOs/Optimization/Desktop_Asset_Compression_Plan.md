---
category: Optimization
status: in-progress
created: 2026-10-11
---
# Moderate Desktop Asset Compression

## Goal
- Reduce shipped asset size and texture/audio memory on Windows PC and macOS with moderate importer changes.
- Preserve normal gameplay appearance, readable UI, music quality, and responsive battle audio.
- 2026-10-11: conservative Windows texture batches and scoped audio pilot implemented; remaining categories and desktop runtime/Mac acceptance stay active.

## Decision
- Chosen: category-based import settings, small representative pilot, then bounded batches.
- Why: enable useful compression without broad quality reductions or added battle CPU pressure.
- Avoid: global low-resolution limits, mesh decimation, High mesh compression, blanket audio streaming, Crunch, new streaming systems, and global vertex-compression changes.
- Asset compression is separate from [[TODOs/Optimization/Battle_CPU_Fixes_Plan|battle CPU fixes]]; no FPS improvement is promised.

## Important Values
### Current evidence — 2026-10-11
- Unity `6000.4.7f1`; read-only metadata scan, excluding `Assets/ThirdParty` and `Assets/Plugins`.
- 283 FBX/OBJ ModelImporter records: all `meshCompression: 0`; 230 `isReadable: 1`, 53 disabled.
- 1,196 PNG/TGA/JPG/DDS/PSD TextureImporter records: first/default `textureCompression` is 0 for 671, 1 for 515, 2 for 10; 2 readable.
- All 1,196 texture records have first/default `maxTextureSize: 2048`; this is an import ceiling, not measured dimensions.
- 272 WAV/OGG/MP3 AudioImporter records: all `loadType: 0` and `quality: 1`; `compressionFormat: 0` for 48 and 1 for 224.
- Counts cover these extensions only, not all assets or build dependencies. First/default fields do not establish effective desktop overrides or runtime memory.
- Graph query found no useful compression policy. Source metadata is the baseline; verify effective importer settings in Unity before applying.

## Rules
### Scope and filters
- Include project-owned imported models under `Assets/Art/Models/**`, textures under `Assets/Art/Textures/**`, and clips under `Assets/Audio/**`.
- Start with shipped dependencies; report unused/source-only assets separately. Preserve source files, GUIDs, mesh topology, materials, UVs, bones, and gameplay data.
- Exclude packages, plugins, third-party assets, Editor/test/Sandbox content, font/SDF atlases, generated render textures/3D noise, lookup/data textures, and `Assets/AddressableAssetsData` from automatic application.
- Classification order: explicit per-asset exception → verified runtime use and shader/importer type → category path → review queue.
- Filter groups: model color, normal map, model mask, UI illustration, sharp UI, world-space sprite/VFX, static model, precision/skinned model, music, ambience, engine loop, frequent SFX, voice.
- Paths/names are hints. `Assets/Art/Textures/Ui/UnitSelectionRing.png` may be used in world space; verify its consumer before choosing mipmaps. Sprite type alone does not prove screen-space UI.
- Likewise `Assets/Audio/Music/Ship/**` must be classified by use: engine loops are not music just because of their directory.
- Conflicting/mixed-use or unclassified assets → report and leave unchanged until reviewed. Do not create duplicate assets or reorganize folders for this pass.

### Desktop targets
- Apply the same policy to Windows standalone and macOS standalone; inspect the shared Standalone texture override and effective audio settings for both targets.
- Existing enabled overrides can defeat defaults. Record effective format, size, quality, and load type before/after; do not create contradictory overrides.
- Prefer Unity Automatic desktop format with Normal compression for ordinary textures. Inspect resolved GPU formats on both targets.
- Typical desktop color output: DXT1 for opaque color, DXT5 for alpha; use BC7 selectively for quality-sensitive artwork after target support verification.
- Windows graphics API and Mac Metal/hardware coverage must be recorded during validation; keep macOS acceptance pending until checked on an actual supported Mac.

### Texture rules
| Filter | Moderate target | Exceptions/check |
|---|---|---|
| 3D model albedo/color/emission; usually `Textures/Models/**` | Compression Normal; keep current max size initially, normally 2048; mipmaps on; preserve correct color space | Review only oversized outliers after visual comparison; no blanket 1024 cap |
| Normal maps | Preserve Normal Map importer, linear sampling and normal orientation; compressed Automatic; mipmaps on | Check lighting/specular seams; keep higher quality where needed |
| Model masks: team color, metallic, smoothness, AO, packed channels | Preserve channel meaning and linear sampling; Normal compression pilot; mipmaps appropriate to model use | Inspect team boundaries and material response; restore higher-quality/uncompressed exceptions if artifacts appear |
| UI portraits, unit cards, large illustrations | High Quality compression/BC7 candidate; mipmaps off for screen-space use; retain required resolution | Check transparency edges, gradients and actual display scale; preserve sRGB/alpha setup |
| Sharp UI: thin icons, borders, small glyph-like artwork, nine-slices | Keep uncompressed initially; no downscaling; screen-space mipmaps off | Compression only after an explicit pixel-quality comparison; preserve borders, pivots and sprite slicing |
| World-space sprites, selection rings, particles/VFX | Separate pilot; compression Normal or higher when alpha/gradients need it | Mipmaps by camera scaling; verify soft alpha, additive glow and distant appearance |
| Font/SDF, LUT/data textures, runtime-written/read textures | Exclude from automatic changes | Preserve required format, precision, Read/Write and sampling |

- Keep existing texture filter/wrap modes unless a verified use-specific issue requires a change; do not impose Repeat on UI or Clamp on tiled hull textures.
- Read/Write off only where CPU access is unnecessary. Audit the two readable textures before changing them.
- Keep Crunch off for this pass; imported GPU compression is the priority. Do not introduce mipmap streaming or change global texture limits.
- If Sprite Atlases pack affected sprites, inspect atlas platform settings and packed output; source importer changes alone may not control the shipped format.

### Mesh rules
- Ordinary static visual models → pilot Mesh Compression `Medium`; lower to `Low` or retain `Off` if seams, thin shapes, normals, or UV precision degrade.
- Skinned/animated ships, large-coordinate geometry, attachment-sensitive parts, procedural meshes, and collider sources → review individually; retain current settings until validated.
- Mesh compression targets stored mesh size; it is not a guaranteed reduction in runtime mesh memory. Global Vertex Compression is a separate mechanism and stays unchanged.
- Read/Write off is a separate memory opportunity: check CPU vertex access, runtime deformation, mesh combining, collider cooking and batching requirements before disabling.
- Preserve rigs, blend shapes, scale, normals/tangents, welding and optimization flags. No mesh simplification or animation compression in this pass.
- Existing import evidence/tests may require exact source geometry. Compression introduces precision changes: validate against those constraints and retain exceptions rather than weakening tests silently.

### Audio rules
| Filter | Load Type | Format / proposed starting quality |
|---|---|---|
| Long music tracks | Streaming | Vorbis, quality 70%; preserve stereo and source sample rate |
| Long, low-concurrency ambience | Streaming after listening pilot | Vorbis 65–70%; preserve intentional stereo |
| Short repeated engine loops / many simultaneous ship sounds | Decompress On Load initially | Preserve current format first; audition ADPCM for noisy loops |
| Frequent short weapon/impact/UI sounds | Decompress On Load | ADPCM for noisy effects; PCM for tiny crisp clicks/tonal exceptions |
| Short command/voice clips | Decompress On Load initially | Vorbis 70%; compare clarity and first-play latency |
| Longer, infrequent voice clips | Compressed In Memory candidate | Vorbis 70%; measure DSP CPU before adopting |

- Quality percentages are proposed project starting values, not measured optimal settings; raise quality or keep exceptions after listening.
- Preserve sample rate in this pass. Force To Mono only for verified positional 3D sounds without meaningful stereo; never blanket-convert music/UI.
- Keep preload behavior for short latency-sensitive clips. For streamed music, choose preload/background loading only after checking startup/crossfade scheduling; first playback must be ready without a hitch.
- Streaming has per-clip buffer/I/O overhead. Do not stream many simultaneous engine/weapon sounds; validate music loop boundaries and crossfades.
- Disk compression plus Decompress On Load saves stored size but still expands audio in memory; compare imported and runtime sizes separately.

## Implementation
1. Inventory effective desktop importer settings and build dependencies; save path/GUID/category, old settings, proposed changes, and exception reason in a reviewable manifest.
2. Capture baseline Windows/macOS build size, imported texture/mesh/audio sizes, representative scene memory, loading time, and Audio DSP/streaming CPU under fixed content.
3. Pilot: one ship hull/normal/team mask, one precision-sensitive mesh, UI portrait/thin icon/world ring, music track, engine loop, weapon effect, and command voice.
4. Apply importer-only changes to pilot assets through official Unity tooling; reimport/save, read back settings, inspect Console, and compare visual/audio output.
5. Apply successful category rules in bounded batches; keep a per-asset rollback record. Use existing bulk import controls or a small one-shot tool only if needed; no permanent importer framework required.
6. Build and validate Windows and macOS with the same content/settings; restore exceptions, record measured results, and finalize the plan only after both targets pass.

## Edge Cases
- Unity persistence: save/reimport importer changes immediately. If serialized assets are edited on disk, Refresh, ForceReserializeAssets with explicit changed paths only, SaveAssets, then verify imports/Console.
- Before tests or Play Mode, inspect every open scene. Save dirty named scenes and recheck; dirty untitled scene → `BLOCKED_DIRTY_UNTITLED_SCENE`. Use asynchronous tests and poll completion.
- Before implementation touches UI code/prefabs, read `UI_UX_GUIDELINES`; folder creation/reorganization requires `PROJECT_ORGANIZATION`. Neither operation is needed for writing this plan.
- Unavailable Mac hardware/build support → leave validation pending and plan active; Windows success does not prove macOS compatibility.
- No numerical savings target is claimed before measurement. Accept measurable size/memory reduction without visible/audible regression or material loading/DSP CPU regression in matched captures.

## Implementation — 2026-10-11
- Live inventory: 1,763 importers (`291` models, `1,200` textures, `272` clips); `1,321` declared scene/Resources/Addressables dependencies; `442` outside those roots. No dependent Sprite Atlases found.
- Windows x64 / Direct3D 11 / NVIDIA GeForce RTX 5070. Baseline and compressed builds succeeded: `2,762,002,718 → 2,082,952,003` bytes; saved `679,050,715` bytes (`24.6%`). Both retain the pre-existing planet shader MotionVectors `ObjectSpaceNormal` error.
- Retained `140` texture changes: `136` verified hull color/emission candidates, pilot hull color/normal/team mask, one BC7 UI illustration. Preserved dimensions, mips, sampling/color space, source files, GUIDs and CPU readability.
- Batches: `153` candidates in groups ≤ `32`; `17` restored after RGB/alpha error screening. Gate: linear GPU-readback RGB RMSE ≤ `2/255`, max alpha error ≤ `8/255`, and lower Editor texture memory. Screening does not prove gameplay/Mac acceptance.
- Scoped audio pilot: one Vorbis 70% music track → Streaming; one weapon clip → ADPCM / Decompress On Load. Sample rate, stereo/mono and preload/background flags preserved. Listening and DSP acceptance pending; no wider audio batch.
- Exceptions: precision mesh restored to Off after measured geometry change; world ring restored because compression produced RGBA32 with no savings; command voice restored to quality 100% after low sample comparison SNR. Sharp UI, other meshes and engine loops unchanged.
- Both readable bolt textures preserved for the separate projectile VFX plan. Review queue: `433` further texture consumer/channel/scale reviews, `276` mesh reviews, `226` dependent audio reviews, `3` existing desktop overrides, `219` already-compressed assets, plus the `442` assets outside declared roots.
- Verification: `162` candidate/exception importer paths read back; eight team colors × two fixed views compared; no new import errors; both players passed headless startup without logged exceptions. No Unity automated tests or gameplay/DSP captures run.
- Per-asset Editor texture memory arithmetic: `996,358,349` bytes lower; not a matched gameplay memory measurement.
- Evidence/tooling: `Tools/DesktopAssetCompression/README.md`, `Evidence/Manifest.json`, `Evidence/ReviewQueue.json`, `Evidence/Summary.json`. Full snapshots, builds, images and logs: `output/DesktopAssetCompression/`.
- Remaining: unprocessed categories, matched loading/memory/DSP captures, listening/first-play/loop/crossfade/battle acceptance, macOS build support and actual supported-Mac validation. Plan remains active; Windows results do not complete desktop acceptance.

## TODO
- Addressables audit handoff 2026-10-24: `Logs/AddressablesAudit/2026-10-24-phase4/desktop-uncompressed-textures.json` records 818 effective uncompressed automatic texture importers (771 in the historical layout). Source: `Logs/AddressablesAudit/2026-10-24/texture-import-settings.json`. Phase 4 consolidation removes duplicate paths; resolve canonical replacements through its `canonical-mapping.json` before applying compression. No importer settings changed by bundle remediation.
- [x] Record preliminary metadata inventory and moderate category policy.
- [x] Inventory effective Standalone import settings, declared dependencies, dependent atlases and review exceptions.
- [x] Capture Windows build baseline/comparison and headless startup; record Windows GPU/API.
- [x] Apply texture pilot and bounded verified hull color/emission batches; read back 162 candidate/exception paths and check imports.
- [x] Compare pilot hull/team colors and UI illustration; restore mesh/ring/voice and 17 texture exceptions; record measurements and rollback settings.
- [ ] Review remaining texture/model/audio categories and conflicting consumers in the manifest/review queue.
- [ ] Capture matched gameplay memory, loading time and Audio DSP/streaming CPU under fixed content; headless startup and Editor memory arithmetic are insufficient.
- [ ] Audition retained audio pilot; validate first play, loops/crossfades and battle concurrency before broader audio application.
- [ ] Validate Windows gameplay visuals/UI/world sprites and precision at intended scales.
- [ ] Install/obtain macOS build support, resolve effective Mac formats and validate on actual supported hardware.
- [ ] Complete both desktop acceptance gates; move plan/backlog to Done only after acceptance.

## Files
- Backlog: [[TODOs/TODOs]].
- Existing material work: [[TODOs/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan]]; preserve shader/channel contracts.
- Unity references: [Audio import settings](https://docs.unity3d.com/6000.4/Documentation/Manual/class-AudioClip.html), [mesh compression](https://docs.unity3d.com/6000.4/Documentation/Manual/compressing-mesh-data-optimization.html), [texture formats](https://docs.unity3d.com/6000.4/Documentation/Manual/texture-formats-reference.html).
