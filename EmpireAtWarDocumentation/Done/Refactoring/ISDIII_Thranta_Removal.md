---
category: Refactoring
status: done
completed: 2026-11-17
acceptance: pending-user
created: 2026-11-17
tags:
  - ships
  - content-removal
  - empire
  - republic
---
# ISD III and Thranta Removal

## Goal
- Remove `ShipType.ISDIII` (206, Empire) and `ShipType.Thranta` (6, Republic) from the project.
- Strip every faction, tooltip, UI, audio, reinforcement, Addressables and mapping entry.
- Move their source/content files (with `.meta`) to `D:\EmpireAtWarUnusedContent`, keeping the `Assets/...` relative layout.

## Current State (2026-11-17, source scan)
- Neither ship is in any faction `ships:` roster (`EmpireFaction.asset`, `RepublicFaction.asset`). ISDIII is hard-blocked in `ShipFactory` and `BattleSaveValidator`.
- `ISDIIIMatchups.asset` / `ThrantaMatchups.asset` → no GUID references (orphaned tooltip data).
- Ship icons are still referenced by `TooltipIconData.asset`, `ShipUiData.asset`, `SharedIcons` group.
- 109 files (excl. `.meta`) match `*ISDIII*` / `*Thranta*` under `Assets`.

## Decision
- Remove both enum values from `ShipType.cs`. Why: no roster uses them; `Enum.IsDefined` already rejects unknown types in saves. Avoid: keeping dead values plus ISDIII special-case guards.
- Move files out with the OS, not `AssetDatabase.MoveAsset` (target is outside the project). Copy files + `.meta` first, then delete in Unity so references are cleaned on reimport.

## Implementation

### 1. Pre-check shared usage
- [x] For every file in the set, grep its `.meta` GUID across `Assets`/`ProjectSettings`, excluding the set itself and the data files listed in step 2. Any outside hit → keep that file (e.g. textures/turrets reused by ISD II).
- [x] `Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ISDIIIcon.png` is the **ISD II** icon — keep it (glob `*ISDIII*` matches it).

### 2. Data entries to remove (keys 6 / 206 — confirm each dictionary is keyed by `ShipType`)
- [x] `Assets/Settings/Data/Ship/Shared/ShipsData.asset` — key 6 (l.73), key 206 (l.227).
- [x] `Assets/Settings/Data/Models/ShipUi/ShipUiData.asset` — keys 6/206 (l.33, 77, 189, 211).
- [x] `Assets/Settings/Data/Reinforcement/ReinforcementData.asset` — keys 6/206 (l.73, 220, 614, 691) + reinforcement view refs.
- [x] `Assets/Settings/Data/Models/Audio/ShipSfxData.asset` — key 206 (l.2113, 3144, 4405, 5472).
- [x] `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset` — key 6 (l.246) — verify it is Thranta.
- [x] `Assets/Settings/Data/Tooltip/TooltipIconData.asset` — ISDIII/Thranta icon entries.
- [x] `Assets/Settings/Data/Models/Weapon/WeaponsData.asset` — `unit: ISDIII` rule (l.1160).
- [x] `Assets/Settings/AssetMappingData.asset` — `ThrantaShipData`, `ThrantaShipView`, `ISDIIIShipView`, `ISDIIIShipData`.
- [x] Addressables groups via Addressables API (don't edit structure by hand): `Data`, `EmpireUnitViews`, `RepublicUnitViews`, `SharedIcons`.
- [x] Faction assets: re-check no `ShipType` 6/206 entry remains (rosters, AI, survival waves, research `shipTypes`).

### 3. Code
- [x] `ShipType.cs` — delete `Thranta = 6`, `ISDIII = 206`.
- [x] `ShipFactory.cs:35` — drop ISDIII guard.
- [x] `BattleSaveValidator.cs:174, 262` — drop `== ShipType.ISDIII` clauses (`Enum.IsDefined` covers removed values).
- [x] `Editor/ShipIconGenerator.cs:31, 63` — drop both mappings.

### 4. Move files to `D:\EmpireAtWarUnusedContent`
- [x] Copy each file + `.meta` to `D:\EmpireAtWarUnusedContent\Assets\...` (same relative path).
- [x] Delete originals through Unity (`AssetDatabase.DeleteAssets`), then `AssetDatabase.Refresh` + `SaveAssets`; check Console for missing-reference errors.
- Folders: `Art/Models/EmpireShips/ISDIII`, `Art/Models/RepublicShips/Thranta`, `Art/Textures/Models/{EmpireShips/ISDIII,RepublicShips/Thranta}`, `Art/Materials/Models/{EmpireShips/ISDIII,RepublicShips/Thranta}`, `Art/Materials/Wrecks/{Empire/ISDIII,Republic/Thranta}`, `Prefabs/Models/Cannons/Empire/ISDIII`.
- Files: `Art/Models/Shields/ThrantaShipViewShield.asset`, `Art/Textures/Ui/Icons/ShipIcon/Empire/ISDIIIIcon.png`, `Art/Textures/Ui/Icons/ShipIcon/Republic/ThrantaIcon.png`, `Prefabs/Models/Ships/Empire/{Models/ISDIII,View/ISDIIIShipView}.prefab`, `Prefabs/Models/Ships/Republic/View/ThrantaShipView.prefab`, `Prefabs/Models/Wrecks/{ISDIII,Thranta}WreckView.prefab`, `Prefabs/Ui/Reinforcement/{ISDIII,Thranta}ReinforcementView.prefab`, `Settings/Data/Ship/{Empire/ISDIII,Republic/Thranta}ShipData.asset`, `Settings/Data/Models/Wrecks/{Empire/ISDIII,Republic/Thranta}WreckData.asset`, `Settings/Data/Tooltip/Matchups/{ISDIII,Thranta}Matchups.asset`.

### 5. Tests (review + update; execution disabled locally)
- [x] `GameFlow/SurvivalGameplayTests.cs` — remove ISDIII spawn-reject + roster-exclusion tests (contract gone with enum).
- [x] `Health/WreckMaterialSyncTests.cs:72-79` — remove `IsdiiiWreck_*` cases.
- [x] `Navigation/ShipHeightTierTests.cs:34, 53` — remove ISDIII / Thranta cases.
- [x] `Ships/ShipAssetIntegrityTests.cs:43` — remove `ISDIII` case.
- [x] `Ships/ShipBattleMarkerAssetTests.cs:20` — remove Thranta case.
- [x] `Combat/WeaponTaxonomyTests.cs:330` — drop ISDIII skip.
- Categories: `Ships`, `Health`, `Navigation`, `Combat`, `GameFlow`, `Ui`, `Audio`.

### 6. Verify
- [x] Recompile clean; `grep -ri "thranta\|ISDIII\b"` in `Assets` → only ISD II icon/tests that mean ISD II.
- [x] Addressables build-layout has no missing entries; Unity Console clean after refresh.
- [x] `D:\EmpireAtWarUnusedContent` contains every moved file + `.meta`.

## Edge Cases
- Glob `*ISDIII*` also matches `ISDIIIcon.png` (ISD II icon) → keep.
- Old saves containing type 6/206 → rejected by `Enum.IsDefined` (intended).
- Vault notes referencing these ships (`ISD III Import`, balance research tables) are historical; leave unchanged.
- `Custom_Shader_Fixes_Plan` references `ISDIII_ISDII_diff_Material1` → update that plan when the material leaves.

## Outcome
- Completed 2026-11-17. Enum values, factory/validator guards, icon mappings, data entries, Addressables entries, tests and 107 files (+ `.meta`, 24 folder `.meta`) removed; originals copied to `D:\EmpireAtWarUnusedContent` first.
- Kept `Art/Textures/Models/EmpireShips/ISDIII/ISDIII_Lightmesh_Windows_Albedo.png` (+ folder): used by `VictoryII_Lightmesh_Windows_Material.mat`.
- `ShipAbilityCatalog` key 6 is the Assault ability, not Thranta → untouched. Key 206 in `EmpireFaction` / squadron SFX / squadron UI dicts = squadrons → untouched.
- Removed `WeaponsData` ISDIII Composite Beam rule; removed tests `ShipFactory_ISDIII_*`, `PlayableRostersAndAuthoredWaves_ExcludeISDIII`, `IsdiiiWreck_*`, ISDIII/Thranta cases in height-tier/asset-integrity/marker/taxonomy tests.
- Verification: Unity recompile OK, Console 0 errors, no `ISDIII`/`Thranta` left in `Assets` except the kept texture, ISD II icon. Tests not run (local override).
- Reusable guidance: none (one-off content removal).
