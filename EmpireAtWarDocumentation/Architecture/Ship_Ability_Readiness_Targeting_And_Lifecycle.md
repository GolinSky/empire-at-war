---
type: engineering-reference
system:
  - abilities
  - combat
  - networking
verified: 2026-11-16
verification: Source and fixtures reviewed, Unity recompiled, two assets
  reserialized, and three typed beam prefab bindings read back. Tests and
  multiplayer playtesting not run.
---
# Ship Ability Readiness, Targeting, and Lifecycle

## Goal

- Use shared rules for player commands, enemy AI, auto-power, and move-and-cast orders.
- Keep readiness requirements on the ability settings and run only the lifecycle work each effect needs.
- Gameplay reference → [[GameDesign/Abilities_And_Superweapons]].

## Issues and fixes

| Issue | Implemented fix |
|---|---|
| A host accepted a known hostile ID through fog. | `ShipAbilityService.CanTarget` checks live uncloaked hostiles, caster-team hull visibility or current firing reveal for ships (point visibility for other targets), and the settings target rule. Authority rechecks on execution and on arrival for queued casts. |
| AI, auto-power, and squadron approach used center distance alone. | Use `ShipAbilityRange`. Planar range is the greater of the configured range and `1.15 × (caster footprint radius + target footprint radius)`. Auto-power expands its spatial search using the largest captured footprint and checks the exact range per candidate. |
| Fighter Reinforcements required a local launch slot even on remote clients. | `IHangarCommand.CanRequestReplacement` permits a replica to submit a request. The host checks the live reserve, active bay capacity, and launch budget before spawning. |
| Tractor Beam and Fighter Coordination had empty `Advance` methods. | `ICompletableShipAbility` handles completion and cleanup. `IAdvancingShipAbility` adds ticking. `IShipAbilityInterruptionPolicy` declares caster-death and hazard survival independently. |
| Beam starts cast a generic `ShotEffect` to `BeamShot`. | Weapon profiles expose a typed beam prefab. `WeaponsData.asset` and the Tractor Beam catalog entry keep their existing prefab references in typed fields. Weapon startup and shot prewarming cast the catalog settings to `BoostWeaponPowerSettings`; a wrong settings type fails immediately. |
| `ShipAbilitySlot` hardcoded four ability IDs for subsystem gates. | The settings own the required hardpoint type. Slot checks and effect startup use that setting. The slot also applies the settings-owned Cloak exception. |

## Rules

- Target preview and submission use the same eligibility. Out-of-range casters require a cast-order facade to approach.
- Every slot requires non-null settings. Readiness uses `HasRequiredSubsystem`; missing settings fail instead of granting availability.
- Both `ShipAbilityService` constructors require an injected `IVisionService`. Standalone callers explicitly select fog behavior.
- A remote replacement request indicates intent. Launch and cooldown acceptance depend on host readiness. Fighter Reinforcements has a catalog entry and currently has no stock ship assignment.
- The runner owns `Ready → Active → Recovering → Ready`. Completion-only effects are polled. Advancing effects tick and can complete in the same frame. Replica baselines follow host state transitions.
- No owner cancels (2026-11-22, temporary): players, auto power and AI end only toggles and the Interdict gravity well (`ShipAbilityDefinition.CanPlayerCancel` = `canCancel && (isToggle || settings is GravityWellSettings)`; `ShipAbilityService.TryOwnerCancel`). Game rules still end effects via `TryCancel`: Cloak on Attack/Attack-move/Guard/Hunt, Interdict on Retreat. Effects still end by duration, ion disable, hazards, death.
- While an ability steers the hull (`IsAbilityFacing`, Ion Pulse turn), `ShipOrderRunner.Issue` stores the new order and starts it from `EndAbilityFacing`; `CastAbility` is refused.
- Replicated fire: an ability whose fire moment depends on pose (`IFiringShipAbility`, e.g. Ion Pulse) fires only on the authority. Host → `ShipAbilityService.Fired` → `ShipAbilityFireMessage` → client `FireReplica`. Client replicas never aim, turn or fire on their own. Why: client pose lags 150 ms / ±5°, so a local decision can pick another cannon or never align. See [[Done/Bugs/Ion_Pulse_Client_No_Wave]].
- Launched Ion Shot and fired Ion Pulse can survive caster death and hazard suppression. The two policies are independent. Ion-disable behavior remains effect-specific for completable effects.

## Files

- `Assets/Scripts/Services/ShipAbilities/ShipAbilityService.cs`, `ShipAbilityRange.cs`, `ShipAbilitySettings.cs`
- `Assets/Scripts/Entities/Ship/Abilities/ShipAbilitySlot.cs`, `Assets/Scripts/Components/Hangar/HangarComponent.cs`
- `Assets/Scripts/Services/Enemy/EnemyShipAbilityController.cs`, `Assets/Scripts/Services/ShipAbilities/AutoPowerCombatSnapshot.cs`, `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`, `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`

## Verification

- Unity `6000.4.7f1` recompile → 0 errors. Editor Console → 0 errors.
- Refreshed and reserialized the two changed assets. Unity readback confirmed Laser and Composite still resolve to `LaserProjectile.prefab` and Tractor to `TractorBeamView.prefab`.
- Reviewed ability service, subsystem availability, symmetry, tractor settings, AI targeting, hangar, real entity composition, and weapon fixtures. Added regression coverage for fog-hidden command targets, footprint range, remote readiness, completion-only effects, and typed beam binding.
- Review fixes (2026-11-16): restored typed boost-settings access, required slot settings, and removed implicit fog-off vision construction. Updated 9 dependent fixtures and added a missing-settings availability assertion; existing fog-targeting assertions remain.
- Unity recompile after review fixes → 0 errors; Editor Console → 0 errors. `git diff --check` passed for the source changes.
- Local test execution remains disabled. Unity tests and multiplayer playtesting were not run.
