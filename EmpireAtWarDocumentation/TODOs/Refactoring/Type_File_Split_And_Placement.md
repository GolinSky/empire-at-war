---
category: Refactoring
status: todo
created: 2026-09-30
tags:
  - code-audit
  - structure
---
# Type File Split and Placement

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · **Wave 2** (after all Wave 1 merges) · Unity lane: yes

## Goal
- One top-level type per file, named after the type (AGENTS "C# File Organization").
- No empty tracked folders; no loose scripts in `Scripts/Entities` or `Scripts/Components` roots.

## Decision gate (ask the user before starting)
- Should interface + implementation pairs (35 files, e.g. `IShipService`+`ShipService`) be split? The AGENTS rule says yes. Default: split.
- Generic/non-generic pairs (6: `Controller`, `View`, `BaseUi`, `ViewComponent`, `ModelDependency`, `UnitRequest`): default keep, and name the file after the non-generic type. A split would need `Controller`1.cs`-style names.

## Findings
- **CO6:** 77 multi-type files at HEAD `7a0f25d0`. Recount before starting.
  - 4 types: `Services/ShipNavigation/ShipNavigationService.cs`, `Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`
  - 3 types: `MapObstacleContactProvider.cs`, `ISkirmishUiRoute.cs`, `EnemyUnitCommander.cs`, `ShipAiDecisionModel.cs`, `EnemyProductionDecisionModel.cs`, `FactionUi.cs`, `ShipBuildUi.cs`, `ReinforcementUi.cs`, `SelectionComponent.cs`, `MarqueeSelectionModel.cs`, `Components/BaseSystem/Views.cs`
  - Observer interface inside a `*Data.cs`: `GameData`, `LoadingData`, `MenuData` (`IMenuModelModelObserver` → rename typo), `MiniMapData`, `PlanetData`, `SpaceStationData`, `MiningFacilityData`, `DefendPlatformData`, `SceneData`
  - File ≠ type: `Views.cs` (IView/BaseView/ViewComponent), `Controllers.cs` (Controller), `EntityMediator.cs` (IEntityLocator/EntityLocator)
- **CO7:** empty tracked folders `Components/Commands/Ship`, `Components/Utils/Scenes` (L3 leftover). Loose files `Entities/FogVisibilityModel.cs`, `Entities/FogVisibilityGridModel.cs`, `Components/BaseComponent.cs`. `Components/Commands/` holds only `IGameCommand` + `GameTimeMode` (the name is stale after `6f55a2a2`).

## Parallel lanes (Wave 2, file-disjoint)
- **2a Entities data observers:** the 9 `*Data.cs` files + the `IMenuModelModelObserver` rename.
- **2b Services:** `ShipNavigation/*`, `Enemy/*`, `UiRouting/*`, `Economy/IIncomeProvider.cs`, + service interface/impl pairs.
- **2c Components:** `BaseSystem/*`, `Selection/*`, `Movement/Formation/*`, `Ship/Movement/*`, `ViewComponents/Health/HardPoint.cs` + component pairs.
- **2d Entities rest:** `EnemyFaction/Models/*`, `Ship/*`, `Faction/*`, `Reinforcement/*`, `Menu/*`, `MiniMap/Ui/*`, `Economy/*`.
- **2e Placement (Unity lane only):** CO7 folder deletes/moves.
- Lanes 2a–2d create new `.cs` files (Unity generates their `.meta` on refresh) → code-only until one shared refresh.

## Rules
- Splitting a `MonoBehaviour`/`ScriptableObject` class out of a file: the **class that keeps the original file must be the serialized one**, otherwise script GUID references break. Moving a serialized class to a new file → create the new file via Unity and move the old `.meta` GUID with it (`AssetDatabase.MoveAsset` on the original file, then rename).
- Keep namespaces unchanged. No behavior edits.
- Read [[Architecture/PROJECT_ORGANIZATION]] before 2e.

## Steps
1. [ ] Recount; confirm the decision gate with the user.
2. [ ] Lanes 2a–2d in parallel; each compiles alone.
3. [ ] 2e: delete the empty folders via `AssetDatabase.DeleteAsset`; move the fog models into `Entities/FogOfWar/` (or the feature folder chosen per PROJECT_ORGANIZATION); decide the `Components/Commands` rename.
4. [ ] Single Unity refresh, compile, check the Console; verify moved GUIDs are unchanged.

## Verification
- Compile clean; the recount shows 0 non-exempt multi-type files; `git diff -M` shows renames keep their GUID `.meta`.
