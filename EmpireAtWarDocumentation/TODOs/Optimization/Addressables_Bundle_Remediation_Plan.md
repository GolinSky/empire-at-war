---
category: Optimization
status: in-progress
created: 2026-10-24
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
- [ ] Duplicate payload reduced and measured.

### Phase 4 — Identical source files (A2)
- 128 identical-content sets, 647 files, 519 redundant (741 MB source); 72 sets also have identical `.meta` minus GUID. Inventory: `Logs/AddressablesAudit/2026-10-24/identical-source-assets.json`.
1. Start with the 72 identical-import sets (e.g. 31× `EndurancePart00_…_Main.png` across Endurance/Nebula).
2. Pick canonical asset per set; remap material/prefab references via Unity tooling; delete redundant files + metas.
3. Sets with importer differences → review queue; keep until visual review.
4. Hand texture path list (818 uncompressed effective desktop importers) to Desktop Asset Compression plan.
- [ ] Identical-import sets consolidated; variants queued.

### Phase 5 — Bundle lifetime split (A4)
- View 1.63 GiB (146 entries), Model 428 MiB (30 entries), all `PackTogether`.
1. Only after Phases 2–4: split by measured load/lifetime (shared art, per-faction previews, per-faction views).
2. Measure bundle count/catalog overhead; no blanket `PackSeparately`.
- [ ] Split applied and measured.

### Phase 6 — Built-in / Resources overlap (A5)
- Battle scene ∩ bundles: 53 paths; MainMenu: 1; Resources: 5 (ExplosionVfx, Point Light, alarm/hyperspace clips, mining-facility art, AudioMixer, TMP fonts).
1. Per asset choose one route: load via Addressables reference, or exclude from bundles.
2. Review TMP Resources fallbacks deliberately; shaders need variant-level review.
- [ ] Overlaps resolved or documented as intentional.

### Phase 7 — Runtime risks (user-run checks)
- **Mixer identity (P1 candidate)**: `ProjectContext` injects built-in `AudioMixer` into `AudioSettingsApplier`; Addressable `MusicSource.prefab` references a bundled copy. Check: packed player, master exposed param → silence; music must mute. Fix: one canonical mixer, bind outputs explicitly.
- **Sync load**: `CachedAssetService.Load` calls `WaitForCompletion` if warmup (`BattleAssetWarmup`, 4 in flight after `Running`) hasn't finished; early reinforcement/`ShipSpawnClearance` can hit it. Check in packed build; coordinate with Loading Freeze plan.
- [ ] Mixer identity verified/fixed.
- [ ] Early-spawn sync-load hitch measured.

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
