---
category: Optimization
status: in-progress
created: 2026-10-24
---
# Asset Size and Memory Follow-up

## Goal
- Cut the remaining 990 MB Addressables build and runtime VRAM/RAM further without visible/audible regressions.
- Measure every step with a fresh build layout; runtime gains need matched memory captures.

## Starting point — 2026-10-24
- Build `buildlayout_2026.10.24.19.42.45.json`: **989,838,944 B** on disk, 18 bundles, 0 duplicates.
- Uncompressed payload 2,564 MB: textures 2,300 MB (90 %), audio 144 MB (6 %), meshes 104 MB (4 %), other 16 MB.
- Texture classes (2048² equivalents): 12 × 22.4 MB RGBA32 (268 MB), 5 × 11.2 MB (62 MB), 255 × 5.6 MB DXT5/BC7 (1,429 MB), 60 × 2.8 MB DXT1 (170 MB), 561 small (367 MB).
- Shipped texture formats: DXT5 524, DXT1 200, RGBA32 147, BC7 56, RGB24 4. Sizes: 322 at 2048, 199 at 1024.
- Previous work: [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Addressables Remediation]], [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop Asset Compression]] (pass 2).

## Rules
- Same screening as pass 2: restore anything that fails; never weaken a gate after seeing results without a user decision.
- Importer changes through Unity APIs; reconcile `.meta` on disk after each pass (file locks seen on 2026-10-24).
- No agent Play Mode; user does visual/listening acceptance. No Crunch, no global texture limits, no mesh decimation.

## Items (October 24 estimates)

- November 6 execution order → [[TODOs/Optimization/Optimization_Audit_Fix_Plan|November 6 fix order and verification]]. Pilot item 2 first once a successful same-target baseline layout and catalog exist. Items 6 and 8 remain later measurement candidates.

### 1. Restored sharp-alpha textures — est. −150 to −250 MB payload
- 45 textures restored in pass 2, incl. 12 still at 22.4 MB (lights/additive/hangar glow, Providence/Pelta hulls). Gate failure was max alpha error 10–24/255 on hard edges.
- Try alpha-aware gate: alpha RMSE ≤ 2/255 + p99.9 alpha error ≤ 16/255, BC7 first for alpha textures.
- Alternative for additive textures: alpha unused by `Particles/Unlit` additive blend → verify shader/blend mode, then DXT1.

### 2. Compressed-but-RGBA32 anomaly — est. −50 MB
- [x] Cause verified November 6. Albedo, Normal, Emissive and Specular are all ship dependencies with explicit RGBA32 default format and Standalone override off. The four resolved formats and Editor memory readings are in [[Artifacts/Audits/Optimization_2026-11-06/Optimization_Audit|the audit]].

### 3. Unused alpha → DXT1 — est. up to −400 MB payload/VRAM
- 524 DXT5 textures; 162 are normal maps (need DXT5). ~362 color/mask textures use DXT5 because the source has an alpha channel.
- Scan pixels: alpha all 255 **and** no shader reads alpha (`_BaseMap` alpha for cutout/smoothness?) → set `alphaSource = None` → DXT1 (half size). Check Ship Lit alpha usage first.

### 4. Texture resolution by on-screen size — est. −300 MB+, visual trade-off
- All model textures capped at 2048. Corvettes/frigates/turrets rarely cover enough screen pixels for 2048.
- Rule draft: max size from ship length × max zoom (texel density); start with corvettes/frigates and turret textures → 1024. Requires screenshot comparison at closest zoom.

### 5. UI textures — est. −60 to −90 MB
- 132 UI textures = 126 MB, mostly RGBA32. Large illustrations/cards → BC7 (no mips); thin icons stay uncompressed.
- Consider Sprite Atlas for small icons (draw calls + padding waste). Read `UI_UX_GUIDELINES` before touching UI.

### 6. Music out of the Model bundle — load lifetime + est. −40 MB
- `Model` bundle (146 MB) is ~all **menu/battle music** (53 tracks, Vorbis q70, Streaming) pulled in by `MusicAudioData` hard refs. Loading any catalog keeps the music bundle file open.
- Move music to its own group (or AssetReferences per playlist); listening test Vorbis q70 → q50 (~−30 %).

### 7. SFX runtime memory
- 159 SFX are Vorbis 100 % + Decompress On Load → full PCM in RAM. Apply plan's audio rules: ADPCM for noisy effects, Vorbis 70 % for voice; keep tiny UI clicks PCM. Disk saving small; RAM/CPU decode saving real. Needs listening + DSP capture.

### 8. Mesh Read/Write — runtime RAM only
- 465 of 517 shipped models have Read/Write on (4.58 M vertices) → CPU + GPU copies. Disable where no CPU access; keep for wreck cutting (`UnitWreckView` reads vertices), shield baking, collider sources.

### 9. Player build outside Addressables — unmeasured
- Scenes (Battle: 53 bundle-overlap paths kept deliberately), Resources, shaders, URP variants not measured since 2026-10-11. Run a Windows player build report; strip unused URP shader variants.

### 10. Leftovers / hygiene
- 14 meshes failing the 3° gate + 4 Rebel station sources Off (baked copy contract).
- 56 identical-source sets with importer differences (`importer-review-queue.json`): review, then consolidate.
- 3 empty groups, orphan `PlayerDataGroupSchema`, stale Android/iOS content-state files (needs user approval).

### 11. Runtime confirmation
- Matched gameplay memory capture (VRAM/RAM) and loading time vs a pre-compression build; profile bundle open/load with CRC off.

## Edge Cases
- DXT1 has 1-bit alpha only → never for textures whose alpha is read (cutout, smoothness, emission masks).
- Downscaling changes team-mask boundaries → compare team colors.
- Moving music changes load timing in the menu → check first-play latency and crossfades.

## TODO
- [ ] 1. Alpha-aware retry for 45 restored textures.
- [x] 2. Providence pilot completed: retain Albedo/Emissive BC7 and Normal DXT5; restore unshipped Specular. Packed bytes, Player memory and numerical/render gates recorded below. Windows/perceptual approval → [[TODOs/User_Acceptance_Queue]].
- [ ] 3. Unused-alpha scan → DXT1.
- [ ] 4. Texel-density resolution rule + pilot.
- [ ] 5. UI texture compression/atlas.
- [x] 6. Playlist lifetime implemented and packed-verified; references/order/codec preserved, Model closure reduced, selected-track handle released on handoff. Listening/ducking approval → [[TODOs/User_Acceptance_Queue]].
- [ ] 7. SFX load/format pass.
- [x] 8. Readability pilot completed for 30 shipped static satellite FBXs; CPU consumers/colliders screened, vertex counts preserved and Player memory measured. Shield/wreck sources excluded. Broader mesh screening is future scope.
- [ ] 9. Player build size report + shader stripping.
- [ ] 10. Leftovers and hygiene.
- [ ] 11. Matched runtime memory/loading capture.

## Files
- Evidence: `Logs/AddressablesAudit/2026-10-24-mesh3/`, `Tools/DesktopAssetCompression/Evidence/Pass2Summary.json`, `output/DesktopAssetCompression/Inventory.json`.
- Tools: `Tools/DesktopAssetCompression/*Pass2.cs`, `DesktopCompressionReconcile.cs`.
- Backlog: [[TODOs/TODOs]]

### Implementation · 2026-11-07

- Providence: retain BC7 Albedo/Emissive and DXT5 Normal. Linear RGB RMSE 0.507/0.411; max alpha error 8/1. Normal mean 0.675°, p99 4.4°. All meet the existing gates.
- Specular passed its pixel gate but is absent from both packed layouts → original importer restored. `AssetDatabase.GetDependencies` alone overstates this shipped dependency.
- Three shipped textures: Player native object memory 67,110,444 → 16,778,880 B. Faction bundle 96,960,634 → 88,787,537 B in the isolated texture step. Eight team colors × two angles captured; foreground render RMSE below 1/255.
- Music: retain 119 ordered bindings / 53 GUIDs as typed AssetReferences. Existing Audio group uses labels `MusicMenu`/`MusicBattle`; original mixer/SFX entries stay together. Model bundle 146,471,297 → 2,654,062 B; baseline → clean final closure 172,468,675 → 28,636,447 B. No codec/volume changes.
- Readability: 30 shipped render-only satellite FBXs; exclude collider base, animated/skinned geometry, shields and wreck hulls. Five packed prefab loads: 248 unique meshes, 329,046 vertices unchanged; 240 become non-readable. Reported native memory 32,741,320 → 13,821,774 B. Editor object memory did not expose the saving.
- Evidence → [content, memory and import tables](../../../Docs/Audits/2026-11-07-Optimization-Fixes/README.md). Windows and perceptual listening/visual gates remain separate; broader unused-alpha, UI, SFX and shader work stays active.
