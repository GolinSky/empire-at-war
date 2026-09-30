---
category: Refactoring
status: in-progress
created: 2026-09-30
tags:
  - code-audit
  - identity
---
# Unit Request Identity Keys

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · Wave 1 · Unity lane: yes (one script move)

## Goal
- Unit kind + id is carried by a typed value, not a string that is re-parsed with `Enum.TryParse` chains.

## Findings (CO3)
- `Entities/Faction/Ui/BuildPipelineView.cs:16`: `Dictionary<string, PipelineView>` keyed by `UnitRequest.Id` (enum `ToString`). There is no collision today (enum names are disjoint; `LevelUnitRequest` uses int ids), but the same risk class was fixed for the enemy side by D5.
- `Entities/Reinforcement/ReinforcementUi.cs:41,113-134`: `Dictionary<string, ISpawnShipUi>`; the kind is inferred by `Enum.TryParse(key, out ShipType)` → then `SquadronType`.
- `Services/Reinforcement/ReinforcementService.cs:247-261` `TrySpawnReinforcement(string id)`: a 4-enum `TryParse` chain (Ship → Squadron → MiningFacility → DefendPlatform). An unknown id silently does nothing.
- `UnitLimitKey` is faction-agnostic but lives in `Entities/EnemyFaction/Models/`.

## Decision
- Move `UnitLimitKey` next to `UnitRequest` (`Entities/Faction/Controller/Requests/`) through `AssetDatabase.MoveAsset` (keep the GUID). Rename only if the user agrees (e.g. `UnitRequestKey`).
- `BuildPipelineView` → key by `UnitLimitKey.From(snapshot.UnitRequest)`.
- Reinforcement: pass a typed reinforcement id (e.g. `readonly struct ReinforcementId { SpawnType Kind; int Value }`, or the existing request objects) from UI → presenter → service. The string stays only as a display label.
- Avoid: a generic string-key utility.

## Files (owned)
- `Assets/Scripts/Entities/Faction/Ui/BuildPipelineView.cs`
- `Assets/Scripts/Entities/Reinforcement/ReinforcementUi.cs`, `SpawnShipUi.cs`, `ISpawnShipUi` (the `UnitType` string), reinforcement presenter/controller in between
- `Assets/Scripts/Services/Reinforcement/ReinforcementService.cs` (only `TrySpawnReinforcement` and its signature)
- `Assets/Scripts/Entities/EnemyFaction/Models/UnitLimitKey.cs` → moved; update `using`s in `EnemyFactionController`, `EnemyUnitLimitModel`, tests
- Do **not** edit `UnitSpawnView.cs` (owned by Teardown/Leaks).

## Steps
1. [x] Unity lane: moved `UnitLimitKey.cs` → `Entities/Faction/Controller/Requests/` (GUID `c347ffe2…` kept); namespace → `EmpireAtWar.Controllers.Factions`; name unchanged.
2. [x] `BuildPipelineView` → `Dictionary<UnitLimitKey, PipelineView>`.
3. [x] Typed id = the existing `UnitRequest` object. `IReinforcementPresenter` / `IReinforcementService.TrySpawnReinforcement(UnitRequest)`; `switch` on request type; unknown → `ArgumentOutOfRangeException`.
4. [x] `IReinforcementModelObserver.OnReinforcementAdded` → `Action<UnitRequest>`; `ReinforcementUi` → `Dictionary<UnitLimitKey, ISpawnShipUi>`; `ISpawnShipUi.UnitType` → `Request`. Not serialized (property only).
5. [x] Tests: `EnemyUnitLimitModelTests` compile; `CheatServiceTests.AddShipReinforcement_NotifiesReinforcementModel` now asserts the same `UnitRequest` round-trips. Not run.

## Progress (2026-09-30)
- Commit `cd7b6edb`. Unity recompile: 0 errors.
- Remaining: manual play check below.

## Verification
- Compile clean. Manual: queue ships and squadrons in the build UI (pipelines appear/complete); spawn every reinforcement kind (ship, squadron, mining facility, defend platform).
