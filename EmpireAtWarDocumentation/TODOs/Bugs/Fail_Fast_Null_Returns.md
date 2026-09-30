---
category: Bugs
status: in-progress
created: 2026-09-30
tags:
  - code-audit
  - fail-fast
---
# Fail-Fast Null Returns

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · Wave 1 · Unity lane: no

## Goal
- Mandatory lookups throw with a clear message; optional lookups use the `TryX(out …)` shape. Nothing returns a silent `null` (AGENTS "Avoid Silent Null References").

## Findings (CO1)

| Site | Current | Nature | Target |
|---|---|---|---|
| `Services/SceneService/SceneData.cs:29` `GetScene` | `null` when the key is missing | mandatory config | direct dictionary indexer → `KeyNotFoundException`; or throw naming the `SceneType` |
| `Services/Repository/AddressableAssetService.cs:22` `LoadComponent` | `prefab != null ? prefab.GetComponent<T>() : null` | mandatory asset | load the prefab and return the component directly. Also removes a CO4 `GetComponent`: prefer `Addressables.LoadAssetAsync<GameObject>` + `TryGetComponent` + throw, or load the component type if Addressables supports it for the key |
| `Components/AttackComponent/AttackDataFactory.cs:25` `ConstructData` | `null` without `IHealthFacade` | optional? | decide: every attackable entity has `IHealthFacade` → `GetFacade` (throws); otherwise `TryConstruct(out AttackData)` and update callers |
| `Services/ShipAbilities/ShipAbilityService.cs:176` `FindSlot` | `null` | optional (callers use `is ShipAbilitySlot slot` / `slot == null`) | `TryFindSlot(caster, id, out ShipAbilitySlot slot)` |

## Files (owned)
- The four files above + `Components/BaseSystem/IAssetService.cs`
- `AttackDataFactory` callers: `Components/Weapon/StationCombatPresenter.cs:50`, `Entities/Ship/EntityFacades/StationaryAttackFacade.cs:23`, `Entities/Ship/StateMachine/AttackMoveState.cs:73`, `AttackTargetState.cs:101,120`, `GuardState.cs:76`, `HuntState.cs:103`, `Entities/Squadron/Squadron.cs:279` (edit only the call lines)
- `LoadComponent` callers (read-only check): `HardPoint.cs:74`, `AudioService.cs:47,49`
- `SceneData` caller (read-only check): `SceneService.cs:93`

## Steps
1. [x] `SceneData.GetScene` → throw on a missing key. Check that the `SceneData` asset contains every `SceneType` the game loads.
2. [x] `AddressableAssetService.LoadComponent` → no null branch; a missing prefab/component throws with the key in the message.
3. [x] `AttackDataFactory`: list the entity kinds reaching each caller (ship, squadron, station, platform, mining facility, capture structures). If all have `IHealthFacade` → use `GetFacade`. Otherwise → `TryConstruct` and make each caller skip the target explicitly.
4. [x] `ShipAbilityService` → `TryFindSlot`; update the 4 call sites (lines 35, 48, 60, 110).
5. [x] Add Edit Mode tests: `SceneData` missing key throws; `ShipAbilityService` unknown id → `TryActivate` false. Run only on request.

## Progress (2026-09-30, commit `32c5f1a1`)
- `SceneData.GetScene` → `KeyNotFoundException` naming the `SceneType`. `SceneData.asset` maps 0/2/4/999 → every `SceneType` covered.
- `LoadComponent` → `TryGetComponent`; missing prefab or component → `MissingComponentException` with the key. `IAssetService` unchanged.
- `ConstructData` → `GetFacade<IHealthFacade>()` (throws). All 5 entity installers bind `HealthFacade`: Ship/Station/Platform/MiningFacility via `BindHealthFeature`, Squadron via `SquadronInstaller`. No other `DynamicEntityInstaller` exists; capture structures are not entities.
- Null checks removed from `StationCombatPresenter.HandleEnemyAdded` and `StationaryAttackFacade.FocusFire`. Other 6 callers unchanged.
- `AttackTargetState.Enter` still wraps in `TryGetFacade(out IHealthFacade)` — now redundant; left (outside "call lines only").
- Tests added: `SceneDataTests.GetScene_MissingKey_Throws`, `ShipAbilityServiceTests.UnknownAbilityId_TryActivateReturnsFalse`. Not run.

## Remaining
- [ ] Compile clean: blocked by other Wave 1 plans' in-progress edits in the shared tree (`CaptureSitesSystem.cs:342-343` CS0123; `UnitWreckService`/`UnitExplosionService` missing `Services.Pooling`). No errors reported in this plan's files.
- [ ] Manual skirmish check (see Verification).

## Edge Cases
- `WeaponComponent.AddTarget(null, …)` currently hits `attackData.SameSource` → NRE deep in combat. The explicit throw/skip moves the failure to the source.
- `ConstructData` for a target destroyed mid-frame: the facade may still exist → unchanged behavior.

## Verification
- Compile clean. Manual skirmish: attack ships, stations, platforms and squadrons; use abilities; load menu ↔ skirmish.
