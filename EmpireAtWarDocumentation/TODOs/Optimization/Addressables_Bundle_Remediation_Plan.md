---
category: Optimization
status: in-progress
created: 2026-10-24
tags:
  - flag/important
  - flag/issue
---
# Addressables Bundle Remediation

## Goal
- Remove cross-bundle duplication (last layout: 962 assets, 1.31 GiB redundant uncompressed payload) and all-faction preview loading.
- Fix the two asset-mapping tool defects before anyone runs them again.
- Measure every change against a fresh, matching build layout; no savings claimed before measurement.

## Decision
- Chosen order: fresh baseline → mapping tool fixes → ReinforcementData preview refs → shared-art ownership → identical-source consolidation → bundle split → built-in overlap.
- Why: A1 (preview hard refs) is the root of most Model+View duplication; splitting bundles first would only move the duplicates. Tool fixes are small and prevent silent mapping breakage during the later steps.
- Avoid: blanket `PackSeparately`, Analyze "Fix" auto-apply, deleting TMP Resources fallbacks, adding source-file bytes (A2) to redundant-object bytes (A1).
- Texture format work (A3) is owned by [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop Asset Compression]]; this plan only feeds it the 818/771 path list.
- Loading behaviour/warmup is owned by [[TODOs/Optimization/Loading_Freeze_Plan|Loading Freeze Plan]]; coordinate preload changes there.

## Rules
- Move/delete assets only through Unity-aware tooling (GUID + `.meta` preserved). Do not change `Assets/AddressableAssetsData` structure; group/schema edits go through Addressables APIs.
- After serialized-asset edits: `AssetDatabase.Refresh()` → `ForceReserializeAssets(<changed paths>)` → `SaveAssets()` → check Console.
- Compare only reports from the same platform (Windows) and content revision.
- No agent Play Mode; verify by compile + EditMode tests + content build/layout. User checks in-game.

## Implementation

### Phase 0 — Baseline (A6)
1. Enable `GenerateBuildLayout` (Addressables project config) for controlled content builds.
2. Clean content build → keep matching `catalog.bin` + `buildlayout_*.json` pair under `Logs/AddressablesAudit/<date>/`.
3. Run Analyze rules (duplicate bundle dependencies, Resources/scene overlap) **without** fixes; record counts.
4. Record bundle sizes, duplicated asset/object counts and redundant bytes per bundle pair.
- [x] Baseline layout + Analyze captured (2026-10-24).

#### Baseline — 2026-10-24
- `ProjectConfigData.GenerateBuildLayout = true`. `BuildPlayerContent` succeeded, no error, 730 s. Layout: `Library/com.unity.addressables/BuildReports/buildlayout_2026.10.24.17.05.30.json` (295 explicit assets).
- Bundles: View `1,757,276,488` B, Model `447,394,422`, Data `16,702,846`, ViewRelatedGroup `4,533,644`, Ui `4,401,158`, Audio `174,611`; total `2,230,532,684` B.
- Duplication: **979 assets / 2,607 objects / 1,430,171,798 B** redundant uncompressed payload.
- By set: Model+View 864 / 1,294,787,653 B; Data+Model 69 / 67,936,732; Data+Model+View 18 / 40,464,744; Model+ViewRelatedGroup 12 / 13,996,863; Data+View 1 / 6,313,252; Data+Model+Ui 5 / 4,606,040; Model+Ui 4 / 1,597,518.
- Analyze (no fixes): Duplicate Bundle Dependencies 5,272 results (first level Model 2,604, View 2,442, Data 186, ViewRelatedGroup 21, Ui 19); Resources dupes 25 (AudioMixer via `ProjectContext`, TMP Settings shaders/fonts); Scene dupes 64.
- Evidence: `Logs/AddressablesAudit/2026-10-24-baseline/` (`build-result.txt`, `baseline-summary.json`, `duplicate-assets.csv`, `analyze-results.txt`). Compare Phases 2–6 against these numbers.

### Phase 1 — Asset mapping tool defects (A7, A8)
- `AssetMappingContextMenu.RebuildFromAddressables` clears all mappings and rebuilds from `entry.address` → drops `VenatorIIShipView` alias (→ `ResoluteShipView` GUID `d9d6eb346b0497649911724f20048daf`) used by `ShipFactory.Create` (`shipType + "ShipView"`).
- `GetMappingKey` uses `asset.GetType().Name` for ScriptableObjects → asteroid `MiningFacilityData` overwrites the base `MiningFacilityData` key via `SetMapping`.
1. Choose fix: preserve explicit aliases on rebuild **or** add a real `VenatorIIShipView` address/entry matching the runtime naming contract (user decision).
2. Key ScriptableObjects by existing Addressables entry address; refuse/flag replacement when a different GUID already owns the key.
3. EditMode regression tests: VenatorII alias survives rebuild; two same-type SO assets keep distinct keys.
- [x] Tool fixed + tests pass (2026-10-24).

#### Implemented — 2026-10-24
- Decision: preserve aliases (no new Addressables entry). Live aliases: `VenatorIIShipView → ResoluteShipView`, `MenuUi → PauseMenuUi`; all other 191 keys equal their entry address.
- `BuildMappings(existing, entries)`: key every entry by address, then re-add existing mappings whose GUID is still an entry but whose key ≠ that address. Stale aliases (GUID no longer an entry) are dropped.
- `GetMappingKey(asset, entryAddress)`: existing entry address wins; derived type/component/name key only for non-addressable assets.
- Add Selected refuses to repoint a key owned by another GUID → `Debug.LogError`, asset skipped.
- Tests: `AssetMappingContextMenuTests` (alias kept/stale dropped; same-type `MiningFacilityData` distinct keys; saved mapping keeps VenatorII alias and non-asteroid base key) 3/3; `AddressableHandleLoaderTests` 5/5. Compile clean. Rebuild menu not executed on the real asset.

### Phase 2 — ReinforcementData previews (A1, P1)
- `ReinforcementData` holds 4 `DictionaryWrapper<…, UnitSpawnView>` hard refs (79 mappings → 78 preview prefabs); `BattleAssetPreload.BeginShared` loads it unconditionally → full ship FBX/material/texture graph of every faction enters Data/Model.
- Consumer: `ReinforcementPlacementFactory` (`GetSpawnPrefab` ×4). Also injected in `EnemyFactionController`, `EnemyProductionStrategy`, `ReinforcementModel`, `ReinforcementUi(Controller)`, `CheatServiceTests`.
1. Replace `UnitSpawnView` values with `AssetReferenceT`/address keys; keep `ReinforcementData` a pure data asset.
2. Load previews per player faction on demand (or preload the owned faction set) via `CachedAssetService` with scoped handle ownership.
3. Update placement factory to await/use the loaded preview; keep fail-fast on missing keys (no `FirstOrDefault` fallback added).
4. Verify `Data` bundle no longer pulls ship FBX/textures (layout diff).
- [x] Preview refs addressable; Model no longer embeds preview ship art (2026-10-24). In-game placement check pending (user).

#### Implemented — 2026-10-24
- `ReinforcementData` wrappers → `AssetReferenceGameObject`; getters `GetSpawnPrefabKey(...)` return `AssetGUID` (wreck pattern). Facility/platform fallback now `First()` (throws on empty instead of null). `GetPreviewKeys(FactionDefinition)` = faction ships + squadrons + all facility/platform previews.
- `ReinforcementPlacementFactory.LoadPreview(key)` → `IAssetService.LoadComponent<UnitSpawnView>`.
- `BattleAssetPreload.LoadAsync` batch 2 preloads previews only for `PlayerRole.Human` and hosted (`AiAlly`) players' factions; others never load. Unpreloaded keys still load synchronously on first placement.
- Asset: 80 YAML refs converted to `m_AssetGUID` (79 unique prefabs; VenatorII reuses Resolute preview); `ForceReserializeAssets` + `SaveAssets`. 79 prefabs added to `View` group, address = prefab name. Console clean.
- Tests updated: `ExecutorShipTests`, `ISDIIShipTests`, `RaiderCorvetteTests`, `TartanTests` read `value.m_AssetGUID`.
- Tests: AddressableHandleLoader 5/5, AssetMappingContextMenu 3/3, BattleAssetPreloader 10/10, CheatService 6/6, ReinforcementInteraction 3/3, ReinforcementSpawnRule 10/10, ShipReinforcementUiPrefab 1/1, UnitSpawnView 3/3, Executor 8/8. Unrelated pre-existing failures: ISDII weapon count 27≠28; RaiderCorvette/Tartan registrations NRE at wreck `<Prefab>k__BackingField` (field renamed to `prefab`), RaiderCorvette weapon count, Tartan muzzle binding.
- Content build (688 s, no error), layout `buildlayout_2026.10.24.17.29.37.json`, 374 explicit assets:
  - Duplicated assets **979 → 115**; objects 2,607 → 216; redundant payload **1,430,171,798 → 135,383,837 B**.
  - Model+View set (864 assets) eliminated. Model bundle 447,394,422 → 166,120,482 B; View 1,757,276,488 → 1,759,828,214 B; total 2,230,532,684 → 1,951,810,468 B.
  - Remaining sets: Data+Model 69 / 67.9 MB, Data+Model+View 18 / 40.5 MB, Model+ViewRelatedGroup 12 / 14.0 MB, others < 7 MB.
- Evidence: `Logs/AddressablesAudit/2026-10-24-phase2/`. Import guide updated for AssetReference previews.

### Phase 3 — Shared artwork ownership (A1)
1. From the fresh layout, list implicit assets shared by ≥2 bundles (Model+View 852 assets / 1,220 MiB historically).
2. Assign explicit ownership (e.g. shared-art group by faction/ship family) so one bundle holds each GUID; control dependency fan-out.
3. Rebuild → duplicated-object count and redundant bytes vs baseline.
- [x] Duplicate payload reduced and measured (2026-10-24).
- Build `buildlayout_2026.10.24.17.52.39.json` succeeded in 698 s: **115 → 0 duplicated assets**, **216 → 0 objects**, **135,383,837 → 0 B redundant payload**. Total bundle bytes **1,951,810,468 → 1,927,606,772**; 9 → 13 bundles, 489 explicit assets. Data 16,702,846 → 1,710,775 B; Model 166,120,482 → 146,411,813 B. Matching catalog/layout saved under phase3 evidence.
- Execution 2026-10-24: 115 remaining implicit duplicate GUIDs assigned through Addressables APIs to `SharedIcons`, `SharedVfx`, `SharedFonts`, `SharedShaders`; schemas copied from `View` (CRC policy preserved). Phase 3 build passed; measurements above. Evidence: `Logs/AddressablesAudit/2026-10-24-phase3/`.

### Phase 4 — Identical source files (A2)
- 128 identical-content sets, 647 files, 519 redundant (741 MB source); 72 sets also have identical `.meta` minus GUID. Inventory: `Logs/AddressablesAudit/2026-10-24/identical-source-assets.json`.
1. Start with the 72 identical-import sets (e.g. 31× `EndurancePart00_…_Main.png` across Endurance/Nebula).
2. Pick canonical asset per set; remap material/prefab references via Unity tooling; delete redundant files + metas.
3. Sets with importer differences → review queue; keep until visual review.
4. Hand texture path list (818 uncompressed effective desktop importers) to Desktop Asset Compression plan.
- [x] Identical-import sets consolidated; variants queued (2026-10-24).
- 72 identical-import sets consolidated: **262 redundant files / 275,429,420 source bytes** removed through `AssetDatabase.DeleteAsset`; 763 material/audio-data assets remapped through `SerializedObject`. Canonical paths/GUIDs retained. Independent verification: 72 canonical hashes/GUIDs valid, zero stale GUID references, all 56 importer-variant sets preserved. Evidence: `Logs/AddressablesAudit/2026-10-24-phase4/{canonical-mapping.json,importer-review-queue.json,verification-result.json}`. Content build succeeded in 547 s: **1,927,606,772 → 1,532,590,578 bundle bytes** (395,016,194 B lower); 13 bundles, zero duplicated assets/objects/payload. Matching catalog/layout saved in phase4 evidence.
- Compression handoff: 818 effective uncompressed automatic importer paths (771 historical packed paths) exported to `desktop-uncompressed-textures.json`; Desktop Asset Compression plan updated. Resolve removed paths through the canonical mapping.

### Phase 5 — Bundle lifetime split (A4)
- View 1.63 GiB (146 entries), Model 428 MiB (30 entries), all `PackTogether`.
1. Only after Phases 2–4: split by measured load/lifetime (shared art, per-faction previews, per-faction views).
2. Measure bundle count/catalog overhead; no blanket `PackSeparately`.
- [x] Split applied and measured (2026-10-24).
- Final Windows build `buildlayout_2026.10.24.18.35.02.json` passed: **18 bundles / 1,534,762,737 B / zero duplicated assets, objects or payload**. Split overhead vs Phase 4: +5 bundles / +2,172,159 B; catalog 99,550 → 117,720 B (+18,170 B). Total vs Phase 2: **417,047,731 B lower**.
- Faction bundle closures: Empire 795,307,346 B; Rebellion 423,450,976; Republic 152,693,051; Separatist 120,012,344. Previous unsplit View closure 1,380,274,052 B. Common View closure 89,733,815 B. These are bundle closures, not measured runtime memory.
- Audio dependencies own the existing `Audio` group; closure **185,416 B**, no shared ship-art fan-out. Final evidence: `Logs/AddressablesAudit/2026-10-24-phase5-final/` (matching catalog/layout, address validation, verification result).
- Applied 2026-10-24: 213 main/wreck/preview entries moved to `EmpireUnitViews`, `RepublicUnitViews`, `SeparatistUnitViews`, `RebellionUnitViews`; 14 common entries retain `View`. 90 implicit shared assets promoted (final owners: 63 shared ship art, 2 Audio, 11 shaders, 10 VFX, 2 fonts, 2 icons). Existing addresses/labels preserved; new schemas copy `View`, PackTogether, CRC off. 579 entries; 394 serialized AssetReferences resolve, no duplicate addresses, VenatorII alias intact. First split build passed (394 s): zero duplicates, 18 bundles, 1,534,917,629 B (+2,327,051 B vs unsplit), catalog 99,550 → 117,732 B. Shared mixer/audio-view dependencies exposed a 29.8 MB music closure; moved those 2 entries to existing `Audio` group to remove ship-art fan-out. Final confirmation build: `Logs/AddressablesAudit/2026-10-24-phase5-final/`.
- Dry-run decision 2026-10-24: keep preview/main/wreck entries together in four faction `UnitViews` groups; common facility/platform views remain `View`, global catalogs remain `Model`. Separate preview groups would require 2,924 promoted dependencies; combined faction groups need 90. Previews already depend on full unit views, so the extra separation offers no art isolation. Evidence: `Logs/AddressablesAudit/2026-10-24-phase5/`.

### Phase 6 — Built-in / Resources overlap (A5)
- Battle scene ∩ bundles: 53 paths; MainMenu: 1; Resources: 5 (ExplosionVfx, Point Light, alarm/hyperspace clips, mining-facility art, AudioMixer, TMP fonts).
1. Per asset choose one route: load via Addressables reference, or exclude from bundles.
2. Review TMP Resources fallbacks deliberately; shaders need variant-level review.
- [x] Overlaps resolved or documented as intentional (2026-10-24).
- Fresh Analyze (no fixes): duplicate bundle dependencies **No issues found**; Resources **20** results; scene **47** results. All 67 overlap rows classified in `Logs/AddressablesAudit/2026-10-24-phase6/current-overlap-decisions.csv`. Built-in asteroid/map art, effects/SFX, TMP defaults/fallbacks and shader variants retain deliberate routes; canonical music mixer binding tested.
- Final compile clean; no new import/serialization/Console errors; Editor not in Play Mode. 394 serialized AssetReferences resolve, zero duplicate addresses, VenatorII alias preserved. Music mixer identity EditMode regression 1/1 passed. User runtime gates remain Phase 7.
- Review 2026-10-24: all 74 historical scene/Resources path pairs classified in `Logs/AddressablesAudit/2026-10-24-phase6/overlap-decisions.csv`; TMP fallbacks, map-site art, battle effects/SFX and shader path overlaps retained deliberately. Fresh Analyze captured above.

### Phase 7 — Runtime risks (user-run checks)
- **Mixer identity (P1 candidate)**: `ProjectContext` injects built-in `AudioMixer` into `AudioSettingsApplier`; Addressable `MusicSource.prefab` references a bundled copy. Check: packed player, master exposed param → silence; music must mute. Fix: one canonical mixer, bind outputs explicitly.
- **Sync load**: `CachedAssetService.Load` calls `WaitForCompletion` if warmup (`BattleAssetWarmup`, 4 in flight after `Running`) hasn't finished; early reinforcement/`ShipSpawnClearance` can hit it. Check in packed build; coordinate with Loading Freeze plan.
- [ ] Mixer identity verified/fixed in packed player (user).
- Code fix 2026-10-24: `MusicService` injects bootstrap `AudioMixer` and rebinds instantiated music output to its `Music` group. True independent-mixer EditMode regression test **1/1 passed**; compile clean. Bundled dormant `AudioDialogSource` mixer route retained intentionally; no live consumer. Packed-player master mute/music/SFX/ducking check remains required.
- [ ] Early-spawn sync-load hitch measured.

### Independent verification — 2026-10-24
- Final layout `buildlayout_2026.10.24.18.35.02.json` re-analysed: no error, 579 explicit, 18 bundles, **1,534,762,737 B**, duplicated assets/objects/payload **0/0/0**. Bundle files on disk match (18:35).
- Phase 4: 262 deleted files (247 png, 2 jpg, 13 wav) + 262 metas; scan of 6,954 Assets/ProjectSettings text files → **0 references** to the 262 deleted GUIDs.
- Groups: 579 entries, **0 duplicate addresses**; 4 faction `UnitViews` + 5 `Shared*` groups present; `VenatorIIShipView` alias → `d9d6eb346b0497649911724f20048daf` intact.
- Phase 7 code: `MusicService` takes bootstrap `AudioMixer`, sets `outputAudioMixerGroup = FindMatchingGroups("Music").Single()`; `MusicServiceTests` updated.
- Not re-run: EditMode tests/Analyze (Editor was in Play Mode). Phases 3–6 + Phase 7 code are **uncommitted** (776 M / 524 D / 54 ??). Files changed after final build: URP pipeline assets, `TartanTests`, `BalanceCompareSelectionTests` (not bundle content).
- Remaining: Phase 7 packed-player mixer check and early-spawn sync-load measurement (user) → plan stays active.

### Storage breakdown — final build
- On disk 1.53 GB (LZ4) ← **6,464 MB uncompressed payload**. Textures **6,100 MB / 94.4 %** (908); meshes 204 MB (3.1 %); audio 144 MB (2.2 %); everything else < 0.5 %.
- **173 textures at 22.4 MB each = 3,870 MB** (60 % of payload): 2048² RGBA32 with mips, i.e. no GPU compression (`textureCompression: 0` default, Standalone not overridden). 168/173 already in the Desktop Asset Compression handoff list.
- Other classes: 174 × 5–10 MB (DXT5/BC7, 981 MB), 44 × 10–20 MB (705 MB), 69 × 2–5 MB (DXT1, 254 MB), 433 < 2 MB (286 MB).
- By bundle (uncompressed): EmpireUnitViews 3,682 MB, RebellionUnitViews 1,559, RepublicUnitViews 372, SeparatistUnitViews 217, View 176, SharedShipArt 163.
- By ship texture folder: ISDI 1,064 MB (87 tex: 37 uncompressed + 38 DXT5), Liberator 508, Allegiance 445, Tector 433, Titan 412, Procursator 319, Endurance 281, Tartan 261, ISDIIReplacement 242.
- Next lever is texture GPU compression (owned by [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop Asset Compression]]): 22.4 → 5.6 MB (BC7/DXT5) per texture ≈ −2.9 GB uncompressed payload and VRAM; disk saving needs a rebuild to measure. Evidence: `Logs/AddressablesAudit/2026-10-24-verify/storage-breakdown.txt`.

### Hygiene (optional)
- 3 empty groups, orphan legacy `PlayerDataGroupSchema`, stale Android/iOS content-state files. Remove only with user approval.

## Edge Cases
- Removing preview hard refs changes load timing → placement UI must handle preview-not-yet-loaded without silent null.
- Canonicalizing textures can break team-color/material channel contracts → compare ship appearance per set.
- CRC is deliberately off (Loading Freeze plan); do not re-enable as part of this work.
- Other sessions edit Addressables groups concurrently (ship imports) → re-read group assets before each mutation.

## Files
- Research/evidence: [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research|Audit 2026-10-24 findings]]
- Code: `Assets/Scripts/Entities/Reinforcement/ReinforcementData.cs`, `Assets/Scripts/Services/Repository/BattleAssetPreload.cs`, `Assets/Scripts/Services/Reinforcement/ReinforcementPlacementFactory.cs`, `Assets/Scripts/Editor/AssetMappingContextMenu.cs`, `Assets/Scripts/Services/Repository/CachedAssetService.cs`
- Data: `Assets/Settings/Data/Reinforcement/ReinforcementData.asset`, `Assets/Settings/AssetMappingData.asset`, `Assets/AddressableAssetsData/AssetGroups/{View,Model,Data}.asset`
- Backlog: [[TODOs/TODOs]]

## Guidance extraction — 2026-10-29

- `flag/important`: bundle ownership, alias preservation and preview loading decisions prevent cross-faction dependency regressions.
- Canonical guidance: [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]]. Selected ownership, alias, preview, mixer and overlap lessons reconciled against live source/assets and dated reports; extraction revisit resolved.
- `flag/issue`: current `View.asset` holds Recalcitrant/Peleset/Wavecrest wrecks and Bulwark wreck/preview while their main views are in `SeparatistUnitViews`. Serialized drift confirmed; impact unmeasured. Review lifetime intent and reconcile ownership with a matching layout here; no assets moved in this documentation task.
- Source verification 2026-10-29: 608 address rows, zero duplicate addresses; four faction schemas PackTogether/CRC off; VenatorII alias intact. Historical build totals remain dated evidence; no tests/builds/Play Mode run. Existing Phase 7 runtime gates and plan status remain open.
