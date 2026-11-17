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

## Review fixes (2026-11-17, second commit)
- **Bug:** `ShipMoveComponent._isAnchorBraking` survived the anchor's removal → a Retreat (which cancels the well) or move issued mid-brake was wiped by the deferred `Stop()`. Fix: `SyncAnchor` clears the flag on the falling edge.
- **Ability approach:** `ShipOrderRunner.CastAbility` refuses while anchored (`RejectWhileAnchored(UseAbility)`); in-range casts never reach it.
- **Camera gating:** `GravityWellEffects` is now `ITickable` with `cameraService.SceneCamera`; tracks active emitters (`StartEmitter`/`StopEmitter`) and calls `SetOnScreen(TestPlanesAABB(frustum, CullBounds))` each tick. Off-screen → `emission.enabled = false`, live particles drain. `GravityWellEmitter.cullRadius = 120` (hull bounds 102 × 66 × 173).
- **Refused-order notification:** new `GravityWellOrderNotifier` (battle notifications) posts Feedback "Gravity well active: ship cannot move" (group `gravity-well-anchored`) when the local player's Move/WaypointMove/AttackMove/Guard/Hunt order includes an anchored selected ship.
- **Dead field:** removed unused `GravityWellEmitter.domeAnchors` (prefab reserialized; orphan data gone).
- **Dome placement:** domes are on the **dorsal** hull beside the bridge; vertex clusters span y −21…−1.3 (apex ≈ −1.3). Anchors at y −8 put glows inside the opaque caps. Moved anchors to `(±26.2, 0, −34.9)` / `(±17.8, 0, −5.0)`. Preview-scene renders at 50° and 30° pitch show glow + charge trails on all four domes and the bridge halo.

## Decisions kept
- No emissive `MaterialPropertyBlock` boost on the hull (would fight `UnitRenderStateComponent` property blocks); halo particle instead.
- Enemy AI: rejection is silent and cheap (telemetry only in Editor/Development). Not profiled.

## Tests (authored; not run — local override)
- `ShipAbilitySymmetryTests`: Interdict start/stop/ion/replica/death set and clear `IsAnchored` and emitting; ring follows selection; new `GravityWell_EmitterPausesWhileOffCameraAndStopsWithTheAbility`.
- `InterdictorGravityWellTests` (`Ships`): emitter bound, `cullRadius > 0`, systems non-null and not play-on-awake, effects on 4 distinct dome parents; anchor counter semantics.
- `ShipPursuitTests` (`Navigation`): `AnchoredShip_RefusesMovementOrdersAndStillAttacks` (now also AttackMove + ability approach); new `AnchoredShip_RetreatEndsTheGravityWellAndLeaves`; tractor-approach test now passes a caster.
- `ShipNavigationServiceTests` (`Navigation`): new `GravityWellAnchor_RefusesTravelAndReleasingMidBrakeKeepsTheNextOrder` (regression for the brake-flag bug).

## Outcome & Verification
- Unity compile clean (`get_console_logs --severity error` → 0); prefab/materials reserialized and saved; prefab contract read back via `eval` (`emitter=True anchors=4 systems=9 fogRenderers=67`).
- Review fixes: recompile 0 errors, prefab reserialized (`domeAnchors` gone, `cullRadius: 120`), preview-scene renders checked.
- Not run: EditMode tests (`Abilities`, `Ships`, `Navigation`, `Ui`), in-game check.
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
