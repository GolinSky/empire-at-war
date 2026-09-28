---
tags:
  - code-audit
  - follow-up
created: 2026-09-28
status: open
---

# Follow-up sweep — 28 September 2026

- [[TODOs/Codebase Audit 2026-09-27/00 Overview|← Audit overview]] · [[TODOs/Codebase Audit 2026-09-27/09 Implementation Results|Implementation results]]

- Read-only pass at HEAD f3fdf3d4.
- No project files were changed.
- Serena failed to connect, so this pass used text search (grep) plus reads of the relevant source.
- Findings are candidates until someone checks the callers.

## 1. Re-verification of the 17 entries

- All 17 entries are still in live source: C1 `_requestedTimeMode`, C2 `UiHitTest`/`PointerGestureState`, C3 `TargetSelectionBatch`, C4 `sharedMesh`/one `PositionToPixel`/`FogVisibilityGridModel`, D1–D6, L1–L4, R1–R3.
- No first-party references to removed members (`ActivateInteraction`, `CollectionUtility`, `BuildPathToFile`, radar masks) remain.

- Left over from the implemented work:

| From | Gap | Where |
|---|---|---|
| L3 | Same gap as L1: moving SceneReference left an empty folder with a tracked `.meta` | `Components/Utils/Scenes/` + `Scenes.meta` |
| C4 | The new fog models sit loose in the `Entities` root, with namespace `EmpireAtWar.Models.FogOfWar` | `Entities/FogVisibilityGridModel.cs`, `Entities/FogVisibilityModel.cs` |
| C2 | InputService is still 429 lines after the extraction | `Services/InputService/InputService.cs` |
| C3 | CombatAttackCoordinator is still 506 lines. The batch was extracted (251 lines), but the coordinator is still the largest runtime file | `Components/Weapon/CombatAttackCoordinator.cs` |
| R1 | WeaponComponent grew to 471 lines because it now owns asset lookup and random sampling | `Components/Weapon/WeaponComponent.cs` |
| R3 | `IShipIconProvider` returns `Sprite` but sits in the `Model/` folder. It is correct as a view boundary; the location is misleading | `Entities/ShipUi/Model/IShipIconProvider.cs` |

## 2. Similar issues elsewhere in the codebase

### L4-like: multiple top-level types per file (largest finding)

- L4 split only the files it listed.
- **79 non-generated files** still declare more than one top-level type:
- **32 interface + implementation pairs**, e.g. `IShipService`/`ShipService`, `IEntity`/`Entity`, `IUiService`/`UiService`, `ILayerService`/`LayerService`.
- **6 generic/non-generic pairs**, e.g. `Controller`, `View`, `BaseUi`, `UnitRequest`.
  - These are arguably acceptable; decide on a policy.
- **~41 other files.** Worst cases:
  - `ShipNavigationService.cs` (4 types: agent, plan, service interface, service)
  - `EnemyStrategicDecisionModel.cs` (4: state, snapshot, decision, model)
  - `UnitDeathAnimationService.cs`, `MapObstacleContactProvider.cs`, `ISkirmishUiRoute.cs`, `EnemyUnitCommander.cs`, `ShipAiDecisionModel.cs`, `EnemyProductionDecisionModel.cs`, `FactionUi.cs`, `ShipBuildUi.cs`, `ReinforcementUi.cs`, `SelectionComponent.cs`, `MarqueeSelectionModel.cs` (3 each)
  - **Observer interfaces declared inside `*Data.cs` ScriptableObject files:** `IGameModelObserver`, `ILoadingModelObserver`, `IMenuModelModelObserver` (the doubled "Model" is a typo in the name), `IMiniMapModelObserver`, `IPlanetModelObserver`, `ISpaceStationModelObserver`, `IMiningFacilityModelObserver`, `IDefendPlatformModelObserver`, `ISceneModelObserver`.
    - This is also a placement problem, because model contracts live inside data assets.
  - `FormationPoint` is declared inside `FormationModel.cs`.
  - File name ≠ type name: `Components/BaseSystem/Views.cs` (IView/BaseView/ViewComponent), `Controllers.cs` (Controller), `EntityMediator.cs` (IEntityLocator/EntityLocator).

### R2-like: hidden (property/field) injection

- Pure C# services that use property injection instead of constructor parameters:
- `PlayerService.cs:18`: `[Inject(Id=Player)] private FactionType FactionType { get; }`
- `EnemyService.cs:26`: the same pattern

- The entity MonoBehaviours (`Ship`, `Squadron`, `SpaceStation`, `MiningFacility`, `DefendPlatform`) and `WeaponComponent._impactPresenter` also use `[Inject] private` properties.
- For MonoBehaviours this is standard Zenject practice, but it adds hidden dependencies (Ship has four).
- `ShipData`/`SquadronData` inject `ShipType`/`SquadronType` into a public property with a private setter.

### R1-like: Unity types in models

- The models are mostly clean.
- Remaining cases:
- `ShipMoveModel` (Vector3 ×24, Quaternion, Transform) and `SquadronFlightModel` (Vector3 ×16)
- `IHardPointModel` exposes `Transform`
- `IMapModelObserver` exposes Vector2/Vector3

- Only the `Transform` in `ShipMoveModel`/`IHardPointModel` breaks the MVP boundary.
- Vector math is a softer case.

- `UnityEngine.Random` is used directly in gameplay services: `EnemyShipAbilityController`, `ReinforcementZonesSystem`, `SuperWeaponFireService`, `SquadronPilot`, `AudioDialogShipComponent`.
- These are acceptable in adapters, but the enemy AI choices cannot be tested deterministically.
- R1's "caller supplies the roll" pattern would fit if tests need it.

### C4-like: component lookups (AGENTS rule)

- There are still 8 runtime `GetComponent*` calls:
- `UiInstaller.cs:31`
- `DrawCircle.cs:20`
- `SafeAreaHelper.cs:14`
- `EntityMediator.cs:88` (`GetComponentInParent<IViewEntity>` on a collider hit; this is probably legitimate for physics hits)
- `UnitSpawnView.cs:22` (`GetComponentsInChildren<MeshRenderer>`, plus a `.material` clone per renderer)
- `ReinforcementZoneView.cs:56`
- `AddressableAssetService.cs:22`
- `StaticViewInstaller.cs:22`

- `FogOfWarSystem.cs:79` uses `fogRenderer.material`, which creates an instance.
- This is probably intended; it should be documented or changed to `sharedMaterial`.

### Fail-fast violations (silent null fallbacks)

- `SceneData.GetScene` returns `null` for a missing scene.
  - Mandatory configuration should throw.
- `AddressableAssetService.LoadComponent` returns `prefab != null ? … : null`.
- `AttackDataFactory.ConstructData` returns `null` when there is no health facade.
  - A `TryConstruct` method would state that contract.
- `ShipAbilityService.FindSlot` returns `null`.
  - Check its callers.

### Routine defensive guards (AGENTS "Code Simplicity")

- There are no constructor null guards and no `?.` in first-party code, so the constructor rule is met.
- Still, about 45 method-level `throw new ArgumentNullException` checks remain.
- Most are in `ShipNavigationService` (6), `FormationModel` (5), `ShipAvoidancePlanner` (4), `EnemyUnitCommander` (3), `CheatService`, `LayerService`, `PipelineView` and `UnitDeathAnimationService`.
- These are candidates for removal under "no routine defensive checks".

### D3-like: owner counting

- `BattleVictoryService.cs:68-78` enumerates `_shipService.Ships` and counts Player/Opponent ships by hand.
- That is the rule `ShipPopulation.CountShips` now owns; it could call it with an always-true containment predicate.

### D5-like: string identity keys

- `BuildPipelineView._workingPipelines` is `Dictionary<string, PipelineView>`, keyed by `UnitRequest.Id` (enum `ToString`).
  - This is the same cross-kind collision risk D5 fixed for the enemy.
  - There is no collision today (Ship/Squadron/Research/Platform/Miner/SuperWeapon names are disjoint, and `LevelUnitRequest` uses int ids).
  - `UnitLimitKey` could be reused, but it lives in `EnemyFaction/Models` even though it is faction-agnostic.
  - It belongs next to `UnitRequest`.
- `ReinforcementUi` keys its UI by string and then calls `Enum.TryParse(key, out ShipType)` to tell ships from squadrons.
  - The unit kind is inferred from a string.

### D1/D2-like: duplicated coordinate helpers

- **None found.** There are no private flatten/To* helpers and no inline Numerics↔Unity or FormationPoint conversions outside `FormationConversion`/`NumericsConversionExtensions`.
- D1 is fully closed.

### Structure / cleanup

- Empty tracked folders: `Components/Utils/Scenes/` (L3 leftover) and `Components/Commands/Ship/` (left by commit ea3b8759).
- `Components/Commands/` now holds only `IGameCommand` (a game-start service interface, not a command) and the `GameTimeMode` enum.
  - After the ICommand removal (6f55a2a2) the folder name no longer matches its contents.
- Loose root files: `Components/BaseComponent.cs`, `Entities/FogVisibility*Model.cs`.
- 55 non-test files exceed the 200-line review threshold.
  - The largest runtime files are CombatAttackCoordinator (506), BattlePerformanceCapture (500), ShipNavigationService (487), WeaponComponent (471), ReinforcementZonesSystem (432) and InputService (429).

## Suggested order

1. **Cheap cleanup (P3):** delete the two empty folders through AssetDatabase and move the fog models into a feature folder.
2. **Fail-fast fixes (P2):** SceneData, AddressableAssetService, AttackDataFactory.
3. **Bounded reuse (P3):** BattleVictoryService → ShipPopulation; move UnitLimitKey next to UnitRequest and use it in BuildPipelineView.
4. **R2-like (P3):** switch PlayerService/EnemyService to constructor injection.
5. **L4 sweep (P3, large):** first agree on a policy for interface/impl and generic pairs, then move observer interfaces out of `*Data.cs` files.
6. **Size reviews (only on request):** ShipNavigationService, WeaponComponent, ReinforcementZonesSystem.
