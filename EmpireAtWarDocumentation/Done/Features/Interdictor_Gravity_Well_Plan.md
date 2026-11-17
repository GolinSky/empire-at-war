---
category: Features
status: done
created: 2026-11-17
completed: 2026-11-17
acceptance: pending-user
tags:
  - abilities
  - reinforcement
  - interdictor
---
# Interdictor Gravity Well Ability

## Goal
- New ship ability **Activate Gravity Well** (`ShipAbilityId.Interdict = 57`) for `InterdictorStarDestroyer`.
- While active: hostile teams cannot drop reinforcements (ships, squadrons, structures) inside a circle around the caster → they must deploy outside the bubble.
- Range adjustable in data; default = relay zone spawn-block radius (`ReinforcementZoneData.RelaySpawnBlockRadius` = 900).
- CPU-friendly; correct in online matches (host authority + client replica).

## Decision
- **Chosen:** register the caster's view `Transform` in the existing `BattleAreaLayer.SpawnBlock` layer via `IBattleAreaWriter.Register(BattleAreaLayer.SpawnBlock, owner, transform, range)`; unregister on stop or caster destruction.
- **Why:**
  - `ReinforcementSpawnRule.IsOpen` rejects points where `IBattleAreaService.IsBlocked(team, pos)` (hostile circles only, via `IPlayerRelations`). Ships, squadrons and structures all pass through `IsOpen` → one hook covers every deploy type.
  - `OwnedCircle` reads `Transform.position` live → bubble follows the ship with **zero per-frame ability tick work**.
  - `SpawnAreaOverlay.Redraw` already paints hostile `SpawnBlock` circles red at `updateInterval = 0.1 s` → deploy overlay shows and tracks the moving bubble for free.
  - AI deploy search `ShipSpawnPoints` uses `CanSpawnShip` → AI automatically picks points outside the bubble. `EnemyPatrolSafety`, `EnemyPatrolService` and `EnemyRecoveryAnchor` read `IsBlocked` too (retained as desired threat avoidance).
- **Avoid:**
  - Per-frame entity scans (`SensorJammingAbility.Advance` pattern) — not needed; the effect is area-based, not per-unit.
  - A new layer or new service — `SpawnBlock` semantics match exactly.
  - Networked bubble state messages — ability activation already replicates through `ShipAbilityEventMessage`.

## Important Values
- `ShipAbilityId.Interdict = 57` (next free after `LuckyShot = 56`).
- Range: `ShipAbilityDefinition.range = 900` (matches `ReinforcementZoneData.RelaySpawnBlockRadius`, per-ability tunable in `ShipAbilityCatalog.asset`).
- Interdictor abilities (`InterdictorStarDestroyerShipData.asset` → `abilities: 2700000039000000`): `DauntlessSensorJamming (39)` + `Interdict (57)`; `BoostWeaponPower (5)` dropped.
- Timing (confirmed 2026-11-17): `duration = 30 s`, `recoveryDelay = 60 s`, `canCancel = true`, `isToggle = false`, `requiresEnemyTarget = false`, `aiUse = ShipAbilityAiUse.Offensive`, `autoPower = AutoPowerRole.None`.
- Required subsystem (confirmed 2026-11-17): none (`HardPointType.Any`) — ends early only on caster death or ion/ability suppression.
- World bubble VFX (confirmed 2026-11-17): pooled `GravityWellField.prefab` (`GravityWellView` + 96-segment `LineRenderer` using `IonPulseGlow.mat`, cyan friendly / red hostile via `MaterialPropertyBlock`).

## Implementation
1. **Icon**
   - Converted `C:\Users\golin\Downloads\InterdictShipAbility.jpg` (2048²) → `Assets/Art/Textures/Ui/Icons/ShipAbilities/InterdictIcon.png` (256×256 ARGB PNG, corner flood-fill + anti-aliased alpha edge).
   - Imported with Sprite settings matching `SensorJammingIcon.png.meta` (`guid: 917c0ed2f47f71f42b24d04e609f4908`).
2. **Code**
   - `ShipAbilityId.Interdict = 57` in `Assets/Scripts/Services/ShipAbilities/ShipAbilityId.cs`.
   - `GravityWellView` (`Assets/Scripts/Components/ViewComponents/Weapon/GravityWellView.cs`): positions a 96-point `LineRenderer` ring on `PLANE_Y = -2f` tracking `_caster.position` in `LateUpdate` with friendly/hostile `MaterialPropertyBlock` tinting.
   - `GravityWellEffects` (`Assets/Scripts/Services/ShipAbilities/Abilities/GravityWell/GravityWellEffects.cs`): `ViewPool<GravityWellView>` (`MAX_IDLE_VIEWS = 4`), `ILateDisposable`, registered in `SkirmishScope`.
   - `GravityWellSettings` + `GravityWellAbility : IStoppableShipAbility, IReplicaPersistentShipAbility`: registers/unregisters `BattleAreaLayer.SpawnBlock` via `IBattleAreaWriter`, subscribes to `_caster.Health.OnDestroy += Stop`, shows/hides pooled `GravityWellView`, idempotent on `RestoreReplicaState`.
   - `ShipAbilityDependencies`: added `IBattleAreaWriter BattleAreas` and `GravityWellEffects GravityWells`.
   - `ShipAbilityService.Advance`: moved the caster-death check above `if (slot.IsReplicaState)` so destroyed replica casters also stop active persistent abilities.
   - `BalanceAbilityAdapter`: added `GravityWellSettings` entry.
   - `AbilitySlotSave.WithId` + `BattleSaveValidator.MigrateShipAbilities`: migrates legacy 1-slot `[BoostWeaponPower]` → `[DauntlessSensorJamming, Interdict]` and 2-slot `[BoostWeaponPower, DauntlessSensorJamming]` → `[DauntlessSensorJamming, Interdict]`.
3. **Data & VFX**
   - Created `Assets/Prefabs/Vfx/Combat/GravityWellField.prefab` (`LineRenderer` with `IonPulseGlow.mat` + bound `GravityWellView`).
   - Updated `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset` with key `57` (`Activate Gravity Well`, duration 30, recovery 60, range 900, `aiUse = Offensive`, `InterdictIcon`, `GravityWellSettings` → `GravityWellField.prefab`).
   - Updated `Assets/Settings/Data/Ship/Empire/InterdictorStarDestroyerShipData.asset` (`abilities = [DauntlessSensorJamming, Interdict]`).
4. **Online & Overlay Verification (Source)**
   - Host `ReinforcementService.Deploy` validates every dropped slot through `placement.IsPositionValid(slot)` → `ReinforcementSpawnRule.IsOpen` → `IBattleAreaService.IsBlocked`, rejecting blocked slots and invoking `PlacementRejected`.
   - `SpawnAreaOverlay.Update` already triggers `Redraw()` every `updateInterval = 0.1 s` while visible, keeping moving `SpawnBlock` circles current.

## Outcome & Verification
- **Outcome (2026-11-17):** Implemented `Activate Gravity Well` (`Interdict = 57`), pooled ring VFX, catalog/ship data, save migrations, balance registration, and test coverage.
- **Verification performed:**
  - `InterdictIcon.png` visually inspected; `InterdictIcon.png.meta` matches `SensorJammingIcon.png.meta` Sprite import settings.
  - `GravityWellField.prefab`, `ShipAbilityCatalog.asset`, `InterdictorStarDestroyerShipData.asset`, and `ShipSfxData.asset` (`abilityId: 57`) refreshed, reserialized (`ForceReserializeAssets`), and saved (`AssetDatabase.SaveAssets()`); live Unity deserialization verified via `eval`.
  - Unity `AssetDatabase.Refresh()` compiled cleanly; `get_console_logs --severity error` returned `0` errors.
  - Added/updated EditMode tests in `ShipAbilitySymmetryTests.cs`, `BalanceInventoryTests.cs`, `BattleSaveServiceTests.cs`, `ShipAbilityFactoryTests.cs`, `FighterCoordinationModifierTests.cs`, and `ShipSfxDataCoverageTests.cs` (`EveryConfiguredShipAbilityResolvesSfx`).
- **Not run:** Automated EditMode/PlayMode tests (disabled under local test execution override) and live skirmish/multiplayer playtesting — queued in [[TODOs/User_Acceptance_Queue]].
- **Reusable guidance:** No new reusable guidance extracted beyond the existing `BattleAreaLayer.SpawnBlock` pattern.

## Files
- `Assets/Art/Textures/Ui/Icons/ShipAbilities/InterdictIcon.png`
- `Assets/Prefabs/Vfx/Combat/GravityWellField.prefab`
- `Assets/Scripts/Components/ViewComponents/Weapon/GravityWellView.cs`
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs`
- `Assets/Scripts/Services/ShipAbilities/Abilities/GravityWell/{GravityWellAbility.cs,GravityWellEffects.cs,GravityWellSettings.cs}`
- `Assets/Scripts/Services/ShipAbilities/{ShipAbilityId.cs,ShipAbilityDependencies.cs,ShipAbilityService.cs}`
- `Assets/Scripts/Services/VContainer/Scopes/SkirmishScope.cs`
- `Assets/Scripts/Editor/Balance/BalanceAbilityAdapter.cs`
- `Assets/Scripts/Services/Saves/{AbilitySlotSave.cs,BattleSaveValidator.cs}`
- `Assets/Settings/Data/Models/Audio/ShipSfxData.asset`
- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`
- `Assets/Settings/Data/Ship/Empire/InterdictorStarDestroyerShipData.asset`
- `Assets/Scripts/Tests/Editor/{Abilities/ShipAbilitySymmetryTests.cs,Abilities/ShipAbilityFactoryTests.cs,Audio/ShipSfxDataCoverageTests.cs,Balance/BalanceInventoryTests.cs,GameFlow/BattleSaveServiceTests.cs,Squadrons/FighterCoordinationModifierTests.cs}`

## TODO
- [x] Decide: Interdictor slot → Sensor Jamming + Gravity Well; `BoostWeaponPower` dropped (2026-11-17).
- [x] Decide: duration 30 s, recovery 60 s, `canCancel = true`, `isToggle = false`, `HardPointType.Any` (2026-11-17).
- [x] Confirm default range: `900` (`RelaySpawnBlockRadius`) (2026-11-17).
- [x] Icon PNG with transparent background + import.
- [x] `GravityWellSettings` / `GravityWellAbility` / `GravityWellEffects` / `GravityWellView` + `ShipAbilityId.Interdict` + dependency wiring.
- [x] Catalog entry + Interdictor data + save migration + balance adapter.
- [x] Verify host-side deploy validation, overlay refresh, and replica cleanup paths.
- [x] Tests authored/updated; compilation verified (0 Console errors).
- [x] User in-game check: confirmed working (2026-11-17).
- [ ] Authorized test run → [[TODOs/User_Acceptance_Queue#Interdictor Gravity Well]].
- Follow-up (anchor, selected-only ring, dome VFX) → [[Done/Features/Interdictor_Gravity_Well_Anchor_And_Vfx_Plan]].
