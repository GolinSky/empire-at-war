---
category: Refactoring
status: done
created: 2026-11-12
tags:
  - code-style
  - namespaces
completed: 2026-11-12
---
# Namespace-Qualified Type Cleanup

Source report: `C:/Users/golin/.codex/visualizations/2026/11/12/01a1cf2d-986e-70f2-8c58-08c204a7e01e/namespace-usage-report.md` (2026-11-12, outside vault).

## Goal

- Replace inline `EmpireAtWar.X.Y.Type` references in declarations (fields, params, properties, base lists, attributes, generic args, `typeof`, casts, `new`, static calls) with `using` directives + short names.
- Keep qualification only where a real name collision needs it; resolve those with a file-level alias.
- Tests are in scope (user decision 2026-11-12): Batch 6 covers all 234 test lines.
- Preserve type bindings, behavior, and declared namespaces.

## Scope (from report)

- 429 lines / 143 files under `Assets/Scripts`; text search, not compiler analysis.
- By area: Tests 234 · Entities 73 · Services 51 · Editor 34 · Components 23 · BaseSystem 14.
- Biggest files: `Tests/Editor/RealEntityCompositionTests.cs` 65 · `MainMenuUiControllerLifecycleTests.cs` 29 · `ShipPursuitTests.cs` 17 · `Entities/Faction/Ui/FactionUiController.cs` 14 · `BaseSystem/Ui/UiPlacement.cs` 12 · `MainMenuScope.cs` 9 · `SuperWeaponPresenter.cs` 9.
- Most qualified namespaces: `EmpireAtWar.Lifecycle` (48 incl. `LifecycleOrders`) · `EmpireAtWar.Ship` 23 · `EmpireAtWar.Editor` 19 · `Tests.Editor.TestPlayers` 11 · `Services.Input` 11 · `Components.Hangar` 11 · `Services.Saves` 10 · `Mvc` 9 · `Entities.BaseEntity` 9.
- Out of scope: framework qualifiers (`UnityEngine.Camera`, `System.Numerics.Vector2`, `global::VContainer`) — they are already collision fixes.

## Collisions (verified in live source 2026-11-12)

Shortening these breaks or silently rebinds. Use an alias, never a bare short name.

| Qualified use | Conflict | Rule |
|---|---|---|
| `EmpireAtWar.Lifecycle.IInitializable` / `ILateDisposable` / `ITickable` / `ILateTickable` | `VContainer.Unity` shares several lifecycle interface names | Use explicit project lifecycle aliases where both sets are in scope. `ILateDisposable` follows the same alias pattern in those files. |
| `EmpireAtWar.Lifecycle.LifecycleOrder(...)` / `LifecycleOrders.*` attribute | none known | `using EmpireAtWar.Lifecycle;` → `[LifecycleOrder(Initialize = LifecycleOrders.X)]` |
| `EmpireAtWar.IObserver<T>` (`Utils/Notifications/IObserver.cs`) | `System.IObserver<T>` when file has `using System;` (`SelectionComponent`, `HazardService`; check `ShipUiController`) | Keep `EmpireAtWar.IObserver<T>` — C# aliases cannot name an open generic |
| `EmpireAtWar.Ship.Ship` | namespace `EmpireAtWar.Ship` = class `Ship`; inside `EmpireAtWar.Entities.Ship.*`, `Ship` binds to namespace | Alias `using ShipEntity = EmpireAtWar.Ship.Ship;` or keep qualified |
| `EmpireAtWar.SpaceStation.SpaceStationInstaller`, `EmpireAtWar.MiningFacility.MiningFacilityInstaller` | namespace names equal class names `SpaceStation` / `MiningFacility` | Same as `Ship`: alias or keep |
| `EmpireAtWar.Mvc.ViewComponent` base of `EmpireAtWar.ViewComponents.ViewComponent` | same simple name | Keep qualified (`Components/ViewComponents/Abstraction/ViewComponent.cs:7`) |
| `EmpireAtWar.Mvc.Data` (`AssetMappingData`) | generic name `Data`; check per file | Shorten only if file compiles without ambiguity |
| `EmpireAtWar.Editor.EditorToolInfo` in `EmpireAtWar.Editor.*` files | `UnityEditor.Editor` class vs `Editor` namespace segment | Inside `namespace EmpireAtWar.Editor*` use bare `[EditorToolInfo(...)]`; never write `Editor.X` |

## Decision

- Chosen: per-file `using` directives; aliases only for the collisions above.
- Why: `AGENTS.md` code simplicity; inline qualifiers hide dependencies and bloat signatures.
- Avoid: global usings (`global using`) — hide per-file deps, Unity asmdef scope issues; renaming namespaces (`EmpireAtWar.Ship` → `EmpireAtWar.Entities.Ship`) — separate plan, touches serialized/asmdef refs.
- Avoid: IDE "simplify names" bulk apply without compile check per batch.

## Implementation

Work in batches by area; each batch = edit → Unity compile clean → commit. Keep `using` blocks in file's existing order/style.

1. [x] **Batch 0 — tooling:** regenerate inventory with a script (regex from report) into scratch; mark each line `shorten` / `alias` / `keep` using the Collisions table.
2. [x] **Batch 1 — Lifecycle attributes** (≈16 files): `[EmpireAtWar.Lifecycle.LifecycleOrder(...)]` → `[LifecycleOrder(...)]` + `using EmpireAtWar.Lifecycle;`. Check each file for `VContainer.Unity` import first.
3. [x] **Batch 2 — Components + BaseSystem** (37 lines): `CombatModifiers`, `NativeArrayBuffer`, `IAssetService`, `UiPlacement` `typeof(...)` table (12 lines, needs ~10 usings — acceptable).
4. [x] **Batch 3 — Entities** (73 lines): UI controllers/presenters (`FactionUiController`, `EconomyUiController`, `ShipUiController`, `SuperWeaponPresenter`, `CoreGameUiController`, `UnitActionsPresenter`), facades. Apply `Ship`/`SpaceStation`/`MiningFacility` aliases.
5. [x] **Batch 4 — Services** (51 lines): VContainer scopes/registrations (watch `Lifecycle` vs `VContainer.Unity`), Enemy, TacticalMarkers, Tutorial.
6. [x] **Batch 5 — Editor** (34 lines): bare `[EditorToolInfo]`; `EntityLifetimeScope`, `SkirmishScope`, `GameInputActions`, `StationLevelView` usings.
7. [x] **Batch 6 — Tests** (234 lines): largest files first; `TestPlayers` nested type (`EmpireAtWar.Tests.Editor.TestPlayers.X`) → `TestPlayers.X` in the declaring namespace.
8. [x] Re-run inventory; remaining lines must each be a documented `keep`/alias.
9. [x] Add rule to canonical guidance (see Guidance).

## Verification

- Per batch: Unity recompile, zero errors/new warnings (`unity` CLI `recompile` + console).
- Watch for silent rebinding: `IInitializable` etc. resolving to VContainer instead of `EmpireAtWar.Lifecycle` compiles but changes lifecycle registration → grep each scope file's `.As<...>` after edit.
- Tests: not run (local override). Categories affected if user runs them: `Core`, `Ui`, `GameFlow`, `EnemyAi`, `Combat`, `Squadrons`, `Ships`, `Abilities`, `BattleInput`, `Balance`.
- No serialized asset changes expected (type names/namespaces unchanged → no `.prefab`/`.asset` GUID or `m_Script` impact).

## Edge Cases

- Partially qualified names (`Services.Input.X`) and aliases are missed by the report regex → spot-check with compiler.
- Static member calls (`EmpireAtWar.Utils.NativeArrayBuffer.EnsureCapacity`) → `using EmpireAtWar.Utils;`, call `NativeArrayBuffer.EnsureCapacity`.
- Uncommitted working-tree changes in `MarkView.cs`, `UiPlacement.cs`, `EnemyTaskForceExecutor.cs` etc. → do those batches after that WIP is committed.
- Enum casts like `(EmpireAtWar.Services.TacticalMarkers.TacticalMarkerType)` → shorten; enum and namespace names differ.

## Verification Results

- Live inventory: 432 candidate lines before cleanup → 4 retained across `Assets/Scripts`. Changed 140 C# files, including test source.
- Retained `EmpireAtWar.IObserver<T>` in `SelectionComponent`, `ShipUiController`, and `HazardService` to disambiguate `System.IObserver<T>`.
- Retained `EmpireAtWar.Mvc.ViewComponent` as the base of the identically named `EmpireAtWar.ViewComponents.ViewComponent`.
- Roslyn verified type and member bindings against Unity compilation inputs. Scope registrations still bind project lifecycle interfaces.
- Unity completed all six compilation batches with zero errors and no new warnings. Tests were not run under the checkout override.
- Batch commits: `ed8ba9cde`, `14b3d699d`, `e3e616a05`, `93321e950`, `f575fc6ba`, `4d238180d`.

## Guidance

- Recorded the project type reference rule in repository `AGENTS.md` under Code Simplicity.

## Open Questions

- Follow-up: rename namespaces `EmpireAtWar.Ship` / `SpaceStation` / `MiningFacility` to remove the namespace = class collision? (separate plan)
