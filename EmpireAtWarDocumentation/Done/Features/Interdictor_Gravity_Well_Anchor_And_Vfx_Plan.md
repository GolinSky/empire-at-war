---
category: Features
status: done
created: 2026-11-17
completed: 2026-11-17
acceptance: pending-user
tags:
  - abilities
  - interdictor
  - vfx
  - movement
---

# Interdictor Gravity Well — Anchor, Selected-Only Ring, Dome VFX

## Goal
- While `ShipAbilityId.Interdict` (57) is active, the Interdictor cannot travel; it may still turn in place and fire.
- The world range ring is visible only while the ability is active **and** that Interdictor is selected.
- The 4 hull gravity-well domes show "generator at full load" VFX while the ability is active.

## Decision
- **Move orders while active (user, 2026-11-17):** reject. Move / WaypointMove / AttackMove / Guard / Hunt do not translate the ship; well ends only by expiry (30 s), manual cancel, caster death or ion/ability suppression.
- **Turning (user):** turn + fire allowed. Only translation is blocked.
- **Retreat (own call, per plan recommendation):** `Retreat` first cancels the well (`TryCancel(Interdict)`), then retreats → battle retreat never soft-locks.
- **Realtime light (own call, per recommendation):** none. Blue wash = one soft additive halo particle over the bridge.
- **Anchor:** counted flag on `CombatModifiers` (`AddMovementAnchor()` / `RemoveMovementAnchor()` / `IsAnchored`), same pattern as ion disable. Avoid `SpeedMultiplier = 0` and reusing `IsIonDisabled`.
- **Ring:** `GravityWellView` subscribes to the caster's `ISelectionModelObserver.OnSelected` (read via new `IEntitySelectionObserverFacade`, implemented by `SelectionFacade`) and toggles `ring.enabled`.
- **Dome VFX:** `GravityWellEmitter` MonoBehaviour on `InterdictorStarDestroyerShipView.prefab`, bound through `EntityBindings.GravityWellEmitter`, registered in `ShipInstaller` only when `Data.Abilities` has `Interdict`; ability resolves `IGravityWellEmitterFacade` with `GetFacade` (fail-fast).

## Implementation (source, 2026-11-17)
- `CombatModifiers`: `_movementAnchors`, `IsAnchored`, included in `HasTemporaryEffects`; remove without add throws.
- `ShipMoveComponent`: `MoveToPosition`/`QueuePlan` no-op while anchored; `CanSettle` false; rising edge → `BrakeForReplan()` when moving (stops on the checked route), `Stop()` when idle; `Tick` finishes the stop once braking ends. `LookAtTarget` / `LookInDirection` untouched.
- `ShipOrderRunner`: `RejectWhileAnchored` for `MoveTo`, `MoveAlong`, `AttackMoveTo`, `Guard`, `Hunt` (telemetry `rejected`, order kept, nothing queued); `Attack`/`AttackHardPoint` accepted; `CastAbility` approach self-ends after 3 stalled attempts.
- `GravityWellAbility`: adds/removes anchor once, `SetEmitting(true/false)`, fail-fast removal of `_effects`/pool/prefab null guards in `GravityWellEffects`.
- Dome measurement (model space, `BankingBody` local): 4 domes, radius ≈ 10, centres `(±26.2, -8, -34.9)` and `(±17.8, -8, -5.0)`.
- Prefab: `GravityWellEmitter` → 4 dome anchors, per dome `Charge` (sphere-shell radius 26, radial velocity −28, trails, 45/s) + `Glow` (26 u pulse, 1.4 s), plus one `BridgeWash` halo. 9 systems total; renderers added to `FogVisibilityComponent.renderers` (enemy domes hidden under fog).
- New assets: `Assets/Art/Textures/Vfx/GravityWellSoftDot.png`, `Assets/Art/Materials/Vfx/GravityWellCharge.mat`, `GravityWellGlow.mat` (`EmpireAtWar/Vfx/Particle Legacy Unlit`, additive).

## Deviations / Deferred
- No emissive `MaterialPropertyBlock` boost on the hull (would fight `UnitRenderStateComponent` property blocks); halo particle instead.
- No battle-camera-bounds emission gating (`ExplosionVfx.sustainedEmitters` style); fog gating only.
- No player-facing notification for a rejected order (no existing order-rejection feedback path); telemetry only.
- Enemy AI: commanders re-issue orders on their own cadence; rejection is silent and cheap. Not profiled.
- Domes sit on the ventral side (y −21.8…−0.2); look from the in-game camera is unverified.

## Tests (authored; not run — local override)
- `ShipAbilitySymmetryTests`: Interdict start/stop/ion/replica/death set and clear `IsAnchored` and `SetEmitting`; ring follows selection and stops after release; fakes now supply real `GravityWellEffects` + test view prefab.
- `InterdictorGravityWellTests` (`Ships`): prefab binds emitter with 4 anchors and non-null systems; anchor counter semantics.
- `ShipPursuitTests.AnchoredShip_RefusesMovementOrdersAndStillAttacks`: move-type orders refused, `Attack` accepted, current order kept.
- Not covered: `ShipMoveComponent` brake-on-anchor (MonoBehaviour with navigation service) — in-game gate.

## Outcome & Verification
- Unity compile clean (`get_console_logs --severity error` → 0); prefab/materials reserialized and saved; prefab contract read back via `eval` (`emitter=True anchors=4 systems=9 fogRenderers=67`).
- Not run: EditMode tests, in-game check.
- Reusable guidance: none extracted beyond the counted-flag `CombatModifiers` pattern (same as ion pulse disable).

## TODO
- [x] Verify previous plan against source.
- [x] Decide move orders (reject), turning, `Retreat` (cancels well), light (none).
- [x] Anchor flag + movement + order rejection.
- [x] AI tolerance check (source only).
- [x] Selected-only ring + fail-fast cleanup in `GravityWellEffects`.
- [x] Measure domes; author `GravityWellEmitter` + particles; bind on prefab.
- [x] Tests authored/updated; compile 0 errors.
- [ ] User in-game acceptance → [[TODOs/User_Acceptance_Queue#Interdictor Gravity Well]].
