---
category: Refactoring
status: todo
created: 2026-09-30
tags:
  - code-audit
  - conventions
---
# Injection and Guard Conventions

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · **Wave 2** (after Type File Split lane 2c/2d for the same files) · Unity lane: no

## Goal
- Explicit dependencies on entity classes and data; no routine method-argument null guards (AGENTS "Code Simplicity").

## Decision gate (ask the user before starting)
- MonoBehaviour `[Inject]` properties are standard Zenject. Options:
  - A: convert to one `[Inject] void Construct(...)` method per class (explicit, still Zenject).
  - B: leave the MonoBehaviours alone and fix only the non-MonoBehaviour cases.
- Default: **B** unless the user chooses A.

## Findings
- **CO5:** `[Inject]` properties/fields:
  - `Ship` (4: `ShipService`, `Data`, `ShipType`, `RootModel`), `Squadron` (`Data`), `SpaceStation`, `MiningFacility`, `DefendPlatform` (`RootModel`), `SelectionComponent` (`Owner`, `LocalPlayer`), `MiniMapUi` (`PlayerColors`), `WeaponComponent._impactPresenter`
  - `ShipData.ShipType` / `SquadronData.SquadronType`: `[Inject] public … { get; private set; }` on `Mvc.Data` classes
- **CO8:** 39 method-level `throw new ArgumentNullException` (non-test, non-Editor). Largest groups: `ShipNavigationService` (6), `FormationModel` (5), `ShipAvoidancePlanner` (4), `EnemyUnitCommander` (3), `CheatService` (2), `LayerService` (2), `PipelineView` (2), `PlayerFactionModel` (2), `MarqueeSelectionUtility` (3).

## Files (owned)
- CO5: the classes listed above.
- CO8: files containing the guards (`grep -rn "throw new ArgumentNullException" Assets/Scripts --include=*.cs | grep -v Tests | grep -v Editor`).
- Coordinate with Type File Split: run after its lanes touching `Ship.cs`, `SelectionComponent.cs`, `ShipNavigationService.cs`, `FormationModel.cs`.

## Steps
1. [ ] Confirm the decision gate.
2. [ ] CO5 (option A only): one `Construct` method per class; keep the same injection IDs.
3. [ ] CO5 `ShipData`/`SquadronData`: check how the type is bound in the installers; prefer an explicit setter/installer assignment, or keep it documented if Zenject needs it.
4. [ ] CO8: remove guards where the next line dereferences the argument anyway. Keep guards on public API boundaries only where the user asks. List the removed guards in the plan.
5. [ ] Tests that assert `ArgumentNullException` (check `FormationModel`, `MarqueeSelectionUtility`, navigation tests) → update or delete them with the guard. Run only on request.

## Verification
- Compile clean; the grep count of guards drops to the agreed set; no behavior change in skirmish smoke.
