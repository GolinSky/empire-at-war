---
category: Optimization
status: in-progress
created: 2026-10-24
---
# Addressables Audit 2026-10-24 — Findings

## Context
- Unity `6000.4.7f1`, Addressables `2.9.1`, Windows. Read-only audit; no fixes, builds, tests or Play Mode.
- Full report: `C:/Users/golin/.agent/diagrams/addressables-audit-2026-10-24.html`. Evidence: `Logs/AddressablesAudit/2026-10-24/`.
- Detailed layout is from 2026-10-23 (`buildlayout_2026.10.23.16.59.48.json`, 280 explicit assets); current catalog 2026-10-24 has 292 entries (+4 ship data, +8 ship/wreck views). Historical totals ≠ current.

## Bundle sizes (current, on disk)
| Bundle | MiB |
|---|---|
| view | 1,667.76 |
| model | 428.02 |
| data | 15.70 |
| viewrelatedgroup | 4.32 |
| ui | 4.19 |
| audio | 0.17 |
- Total 2,223,199,088 B; View+Model = 98.85 %. LZ4, PackTogether everywhere.

## Duplication by bundle set (2026-10-23 layout)
| Set | Assets | Redundant MiB |
|---|---|---|
| Model + View | 852 | 1,220.43 |
| Data + Model | 64 | 59.03 |
| Data + Model + View | 18 | 38.59 |
| Model + ViewRelatedGroup | 12 | 13.35 |
| Data + View | 1 | 6.02 |
| Data + Model + Ui | 5 | 4.39 |
| Model + Ui | 4 | 1.52 |
- Totals: 962 assets, 2,617 objects, 1,409,053,713 B uncompressed (not disk savings).
- Chain example: `ReinforcementData.asset → ExecutorReinforcementView.prefab → Executor.prefab → Executor.fbx`.

## Findings
| ID | Pri | Finding | Key evidence |
|---|---|---|---|
| A1 | P1 | ReinforcementData hard-refs 78 preview prefabs, loaded for all factions | `ReinforcementData.cs:16`, `BattleAssetPreload.cs:149` |
| A2 | P2 | 128 byte-identical sets / 647 files under different GUIDs (519 redundant, 741 MB source) | `identical-source-assets.json`; 72 sets identical `.meta` minus GUID |
| A3 | P2 | 818 of 1,186 reachable texture importers effectively uncompressed (771 in build) | Endurance additive ≈ 22.37 MB/copy; Standalone override `false` |
| A4 | P2 | View/Model huge PackTogether lifetimes | View 146 entries, Model 30 |
| A5 | P2 | Built-in scenes/Resources overlap bundles | Battle 53, MainMenu 1, Resources 5 paths |
| A6 | P2 | Build layout disabled; layout stale | 6/9 bundle filenames differ |
| A7 | P2 | Rebuild From Addressables drops `VenatorIIShipView` alias | `AssetMappingContextMenu.cs:59/79`, `AssetMappingData.asset:535` |
| A8 | P2 | Add Selected Assets keys SOs by type → variant overwrites base | `AssetMappingContextMenu.cs:100/139`; `MiningFacilityData` |

## Top identical-source sets
| File | Copies | Importer variants | Redundant MiB |
|---|---|---|---|
| `EndurancePart00_Rebel_Nebula_Star_Destroyer_Prototype_Main.png` | 31 | 1 | 108.64 |
| `ISDIPart00_Hull_Kitbash_Pieces_ISD.png` | 21 | 3 | 92.29 |
| `ArquitensImperialCruiser_Hull_Kitbash_Pieces_ISD_B_Normal.png` | 23 | 2 | 71.55 |
| `Allegiance_Empire_Procursator_Super_Structure_Albedo.png` | 14 | 1 | 28.54 |
| `EndurancePart00_…_Main_B.png` | 31 | 1 | 25.05 |

## Passing checks
- All 292 addresses/GUIDs in binary catalog; no duplicate addresses; no missing entry paths.
- All 309 serialized AssetReferences target explicit entries; all 190 mapping records point to explicit entries.
- All 9 bundle files exist with matching sizes. CRC off deliberately (Loading Freeze plan).

## Live-source verification (2026-10-24)
- `ReinforcementData`: 4 `DictionaryWrapper<…, UnitSpawnView>` fields confirmed; `BattleAssetPreload.BeginShared` preloads it unconditionally.
- `RebuildFromAddressables`: `mappings.ClearArray()` then `SetMapping(entry.address, entry.guid)` only — alias loss confirmed.
- `GetMappingKey`: `asset is ScriptableObject → asset.GetType().Name` confirmed.
- `ShipFactory.Create`: `Assets.LoadComponent<Ship>(shipType + "ShipView")` confirmed.
- `CachedAssetService.cs:32`: `if (!handle.IsDone) handle.WaitForCompletion();` confirmed.

## Implementation — 2026-10-24
- Phases 1–2 live source verified before continuation; aliases retained and previews use AssetReferences.
- Phase 3 explicit ownership: 115 remaining duplicated assets / 135,383,837 redundant bytes → zero; 1,951,810,468 → 1,927,606,772 bundle bytes.
- Phase 4: 72 identical-import sets; 1,438 references remapped in 763 assets; 262 files / 275,429,420 source bytes removed through Unity; 56 importer-variant sets preserved. Bundle bytes → 1,532,590,578; zero stale GUIDs and zero duplicated payload.
- Phase 5: combined per-faction main/wreck/preview groups chosen after dry runs (90 shared promotions vs 2,924 for separate preview groups). 213 entries moved; 14 common entries retain View. Existing addresses/labels preserved.
- Final Windows layout `buildlayout_2026.10.24.18.35.02.json`: 579 explicit assets; 18 bundles / 1,534,762,737 B; zero duplicated assets/objects/payload. Catalog 117,720 B (+18,170 vs unsplit); bundle overhead +2,172,159 B vs Phase 4.
- Bundle closure bytes: Empire 795,307,346; Rebellion 423,450,976; Republic 152,693,051; Separatist 120,012,344; common View 89,733,815. Previous unsplit View 1,380,274,052. These are dependency closure sizes, not runtime residency measurements.
- Music fan-out corrected: mixer and shared audio-view entry belong to Audio, not SharedShipArt; final Audio closure 185,416 B. MusicService explicitly binds cloned source output to injected bootstrap mixer Music group. Independent mixer-copy regression 1/1 passed.
- Fresh Analyze (no fixes): no bundle dependency duplicates; 20 Resources + 47 scene results classified in `Logs/AddressablesAudit/2026-10-24-phase6/current-overlap-decisions.csv`. Shader variant/player review remains deliberate; Resources fallbacks retained.
- Verification: compilation clean, zero new import/serialization/Console errors, no Play Mode, 394 serialized AssetReferences resolve, no duplicate addresses, VenatorII alias intact; independent source/deletion/review-queue checks pass.
- Evidence: `Logs/AddressablesAudit/2026-10-24-phase{3,4,5,5-final,6}/`; final matching catalog/layout and verification in `2026-10-24-phase5-final/`. Unrelated concurrent test/render-quality changes left untouched.
- Remaining user acceptance: packed-player preview placement, master mute/music/SFX/ducking, early-spawn synchronous-load timing. Main plan remains active.
