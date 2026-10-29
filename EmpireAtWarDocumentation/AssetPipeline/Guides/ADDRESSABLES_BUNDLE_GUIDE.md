---
type: engineering-reference
system:
  - Addressables
  - AssetPipeline
verified: 2026-10-29
verification: Read live mapping, preview/preload, cached-load and music-routing
  symbols; inspected current group/schema YAML, saved aliases and installed
  Addressables 2.9.1 source; counted 608 address rows/0 duplicate addresses;
  reread dated 2026-10-24 layout/overlap reports. No Unity API execution, tests,
  builds, Play Mode or runtime measurements.
---
# Addressables Bundle Guide

## Goal

- Keep bundle ownership aligned with runtime lifetime; prevent cross-faction preview dependencies and alias regressions.
- Use this reference before unit imports or Addressables changes; live source/assets/output override historical evidence.

## Rules

- Main, wreck and placement-preview entries for a faction belong together in `EmpireUnitViews`, `RepublicUnitViews`, `SeparatistUnitViews` or `RebellionUnitViews`.
- Common facility/platform views remain in `View`; global catalogs/data retain their existing `Model` owners. Confirm an asset's actual lifetime before assigning it.
- Explicit shared owners: `SharedShipArt`, `SharedIcons`, `SharedVfx`, `SharedFonts`, `SharedShaders`. Mixer/audio-view dependencies belong to `Audio`, avoiding shared ship-art fan-out.
- Preserve GUIDs, addresses, aliases and labels when moving existing entries. Do not reorganize the `Assets/AddressableAssetsData` directory structure; edit groups/schemas through Addressables APIs.
- Keep faction schemas `PackTogether`; CRC is deliberately off under [[TODOs/Optimization/Loading_Freeze_Plan]]. Separate preview bundles or blanket `PackSeparately` require dependency/lifetime evidence.
- Preview registries use `AssetReferenceGameObject` and `AssetGUID`, not hard prefab references that pull every faction's ship art into global data.
- Before destructive source deduplication, compare content and importer settings; remap references and delete through Unity-aware tooling. Equal bytes with different import settings are not interchangeable.

## Decision

- Chosen in the 2026-10-24 split: combine main/wreck/preview entries per faction.
- Why: previews depend on full views; combined groups required 90 shared promotions versus 2,924 for separate preview groups in that dry run.
- Avoid treating that dated inventory as exhaustive current ownership: later imports have introduced exceptions listed below.

## Implementation

### Asset assignment

1. Re-read current settings/groups; identify the faction/lifetime owner and the existing entry by GUID. Record its address, labels, aliases and previous group.
2. Use `AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid)`, `FindGroup(name)` and `CreateOrMoveEntry(guid, targetGroup)`; moving an existing entry keeps the entry object. Set a canonical address only for a new entry.
3. Mark settings and both changed groups dirty; mark changed schemas/data dirty; call `AssetDatabase.SaveAssets()` in the same operation.
4. Verify the entry's owner, address and labels through the Addressables API; check saved GUID mappings and alias identity. New entries need explicit intended addresses before mapping.
5. Import/persist changed assets under repository `AGENTS.md`; direct serialized edits require refresh, reserialize only changed paths, save, then inspect Console.
6. For an authorized content-build validation, keep matching catalog/layout/platform/content revision together. Compare explicit entries, duplicate assets/objects/redundant payload, bundle/closure sizes and catalog overhead.
7. Run Analyze without auto-fixes; classify Resources/scene overlaps individually. Preserve intentional routes until behavior and shader-variant evidence justify a change.

- `AssetMappingContextMenu` Add Selected creates missing entries in `settings.DefaultGroup`; it does not select a faction group. Rebuild From Addressables refreshes mappings and preserves surviving aliases; neither replaces ownership review.
- Existing entry address wins in `GetMappingKey`; Add Selected refuses a key already owned by another GUID. Give same-type ScriptableObject variants distinct addresses first.
- `ReinforcementData.GetPreviewKeys` returns faction ships/squadrons plus facility/platform previews; `BattleAssetPreload.PreviewKeys` selects human or hosted players.
- Installed Addressables `2.9.1` source confirms `CreateOrMoveEntry` behavior and `BundlePackingMode.PackTogether = 0`; all four faction schemas currently serialize mode `0`, CRC `0`.

## Important Values

- Historical Windows acceptance layout: `buildlayout_2026.10.24.18.35.02.json` → 579 explicit assets, 18 bundles, 1,534,762,737 B, duplicate assets/objects/payload `0/0/0`.
- Historical catalog: 117,720 B; split overhead versus Phase 4: 2,172,159 bundle bytes + 18,170 catalog bytes.
- Historical Audio closure: 185,416 B. Bundle closure bytes describe dependencies, not measured runtime residency.
- Current 2026-10-29 serialized scan: 608 explicit address rows, zero duplicate addresses. No new content build/layout validation performed; historical sizes are neither a budget nor a current-build claim.
- `VenatorIIShipView` remains an alias to Resolute GUID `d9d6eb346b0497649911724f20048daf`; `MenuUi` alias also remains in `Assets/Settings/AssetMappingData.asset`.

## Edge Cases

- **Ownership review open:** `View.asset` currently holds `RecalcitrantWreckView`, `PelesetWreckView`, `BulwarkWreckView`, `BulwarkReinforcementView`, `WavecrestWreckView`; corresponding main entries are in `SeparatistUnitViews`. Serialized ownership is confirmed; residency/duplication impact is unmeasured. Next: review lifetime intent and reconcile ownership with a matching layout under [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan]]. This is a concern, not a proven runtime failure.
- **Resolved mapping regression (2026-10-24; source rechecked 2026-10-29):** rebuild formerly discarded aliases and type-derived keys collided → `BuildMappings` retains aliases whose GUID still has an entry; existing address wins; conflicting selected keys are skipped with an error. Saved alias identity checked; historical tests remain historical, not rerun.
- **Resolved preview dependency coupling (2026-10-24; source rechecked 2026-10-29):** global hard preview refs pulled unit art into data → AssetReferences plus human/hosted faction preload; historical layout removed Model+View duplication. Runtime placement remains an open user gate in the owning plan.
- **Mixer identity, code fix verified; packed-player check open:** `MusicService` rebinds the cloned source to the injected bootstrap mixer's exact `Music` group via `FindMatchingGroups("Music").Single()`. Remaining master mute/music/SFX/ducking validation → [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan]].
- **Sync loading concern:** `CachedAssetService.Load<TSource>` still calls `WaitForCompletion` on unfinished handles; early placement/spawn can reach uncached assets. Proposed resolution depends on measured preload timing; packed-player hitch measurement remains with the Addressables plan and [[TODOs/Optimization/Loading_Freeze_Plan]].
- **Intentional overlap:** TMP Resources defaults/fallbacks, built-in mining-site art, battle effects/SFX and shader path overlaps were classified in the 2026-10-24 report. File overlap alone does not prove duplicated shader variants. Recheck changed routes; do not delete fallbacks as a blanket fix.
- **Importer variants:** the historical consolidation retained 56 variant sets. Preserve them until importer/visual differences are reviewed; compression work belongs to [[TODOs/Optimization/Desktop_Asset_Compression_Plan]].

## Files

- Provenance/remaining work: [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan]]; [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research]].
- Ownership/schema source: `Assets/AddressableAssetsData/AssetGroups/*.asset`; `Assets/AddressableAssetsData/AssetGroups/Schemas/*UnitViews_BundledAssetGroupSchema.asset`.
- Registry/load source: `Assets/Scripts/Editor/AssetMappingContextMenu.cs`; `Assets/Scripts/Entities/Reinforcement/ReinforcementData.cs`; `Assets/Scripts/Services/Repository/BattleAssetPreload.cs`; `Assets/Scripts/Services/Repository/CachedAssetService.cs`.
- Mixer source: `Assets/Scripts/Services/Audio/MusicService.cs`; serialized mapping: `Assets/Settings/AssetMappingData.asset`.
- Historical evidence: `Logs/AddressablesAudit/2026-10-24-phase5-final/{layout-summary.json,verification-result.json,catalog.bin,buildlayout_2026.10.24.18.35.02.json}`; `Logs/AddressablesAudit/2026-10-24-phase6/{current-overlap-decisions.csv,overlap-review.md}`.
- Installed package behavior: `Library/PackageCache/com.unity.addressables@8460f1c9c927/Editor/Settings/{AddressableAssetSettings.cs,GroupSchemas/BundledAssetGroupSchema.cs}` (local cache path; resolve installed package before reuse).
