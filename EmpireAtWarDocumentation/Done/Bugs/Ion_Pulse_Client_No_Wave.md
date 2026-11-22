---
category: Bugs
status: done
completed: 2026-11-22
acceptance: pending-user
created: 2026-11-22
---
# Ion Pulse: No Wave on Online Clients

## Goal
- Malevolence Ion Pulse shows its wave on every machine whenever the host fires it.
- A pulse that never fired is easy to tell from one that did.
- Related: [[TODOs/Features/Malevolence_Import]], [[Architecture/Ship_Ability_Readiness_Targeting_And_Lifecycle]], [[TODOs/Bugs/Multiplayer_Connection_Analysis]].

## Report (2026-11-22)
- Multiplayer. Ion Pulse pressed right after the Malevolence lands.
- Button goes Active → no wave → cooldown.

## Findings (source read 2026-11-22; not reproduced in-game)

### Cause 1 — client re-decides the fire moment (likely main cause)
- Host starts the ability → `HostCombatEventSender.OnActivated` sends only caster, ability, target.
- Client `ShipAbilityService.ActivateReplica` → runs the full `IonPulseAbility` locally: picks a cannon, waits for alignment, fires its own wave.
- Host never sends "fired". Client wave depends on the client reaching the same decision from its approximate pose.
- Cannon side: `FindBestCannon` picks the side with the smaller turn, once. Target dead ahead → both sides ≈ 90° → tie.
  - Spawn pose faces the enemy (`IStationFacingService.GetRotation`) → target is ahead → tie is the normal case right after landing.
  - Client pose differs: render delay 150 ms (`INTERPOLATION_BUFFER_TICKS` 3), position error ≤ 2 u, heading error ≤ 5° (`NetworkTickSettings`).
  - Client picks the other side → its angle grows to 180° while the host turns → client never fires.
- Alignment: `alignmentTolerance` 5° == `HEADING_ERROR_THRESHOLD` 5°. Host fires at the first frame ≤ 5°, then `EndAbilityFacing` resumes the order; `IdleState.LookAtTarget` can turn the hull away. Client heading can stay > 5° → never fires.
- Host finishes (wave 1000 u at 350 u/s ≈ 3 s, +10 s when something is stunned) → `RestoreState Recovering` → client `Stop()` → cooldown. Matches the report.
- Side effect: client applies the stun (`Disable`) only from its own wave → no stun arcs on clients when the wave is missing. Host stun still applies.

### Cause 2 — cancelled turn costs the full cooldown (all modes, by design)
- Turn before firing: up to ~17 s (90° at `RotationSpeed` 6°/s, `TurnAcceleration` 3°/s²).
- Any new order (Move, Attack, Stop, another cast) → `ShipOrderRunner.Issue` → `IsAbilityFacing = false` → `IonPulseAbility._cancelled` → `Stop` → `slot.Recover()` → 60 s, no wave, no message.
- Same result: no live ion cannon, ion disable, hazard suppression before firing.
- A player usually orders a fresh ship at once → looks identical to the report on host and offline.

### Auto ability logic — no defect found
- Ion Pulse `autoPower: 3` = `AutoPowerRole.Target` → wanted whenever a visible hostile is in radar range.
- `PlayerAutoPowerService` → `AutoPowerDecider.Check` → `ShipAbilityService.TryRequestActivation` → same `ShipAbilityCommand` path as a press; host re-validates in `Execute(automatic: true)`.
- Spawn gate: `IonPulseSettings.IsReady` (not `IsArriving`), checked per machine. Client lands after the host (same 0.6 s jump, started on spawn receipt) → client cannot request before the host is ready.
- Manual press → `HoldManual` blocks auto for the hold time. Sole-selected ship → auto suspended.
- Consequence only: auto can start the long turn right after landing; the player's next order then triggers Cause 2.

## Decision
- Chosen: host sends one "fired" event with the wave geometry; clients play that wave and never decide.
- Why: one source of truth; removes side tie, heading tolerance and render-delay dependence.
- Avoid: sending only the cannon side (alignment race stays); widening client tolerance (wave from a wrong origin/direction).
- Open (user): Cause 2 → keep full cooldown, or no cooldown when the pulse never fired. Not in scope until decided.

## Implementation
1. [x] `ShipAbilityFire` struct: `Origin`, `Direction` (planar), `Rise`, `Range`. New `IFiringShipAbility { event Action<ShipAbilityFire> Fired; void FireReplica(ShipAbilityFire fire); }` (one type per file).
2. [x] `IonPulseAbility`: take `IMatchSession` through `ShipAbilityDependencies` (as `IonShotAbility`).
   - Authority: unchanged; raise `Fired` right after `_model.Fire`.
   - Replica: `Start` skips `BeginAbilityFacing`; pre-fire `Advance` does nothing (no cannon pick, no cancel); `FireReplica` sets `_origin`, `_direction`, `_rise`, calls `_model.Fire(range)`, creates the view.
   - `Stop` on a replica skips `EndAbilityFacing`.
3. [x] `ShipAbilityService`: on `Start`, subscribe to `IFiringShipAbility.Fired` → `event Action<IShipAbilityFacade, ShipAbilityId, ShipAbilityFire> Fired`; unsubscribe in `Stop`. Add `FireReplica(caster, id, fire)` → Active slot's running ability, else ignore.
4. [x] `ShipAbilityFireMessage` (reliable) + `HostCombatEventSender`: queue with `_events.Next()`, flush after `_pendingAbilities` in `Flush`. `ClientCombatEventReceiver`: register handler → `FireReplica`.
5. [x] Tests, category `Abilities` (`ShipAbilityServiceTests`), not run (local override):
   - Authority Ion Pulse raises `Fired` once with its origin/direction/range.
   - Replica Ion Pulse aligned to the target does not fire by itself.
   - Replica fires on `FireReplica` and uses the given geometry; ignores it when the slot is not Active.
   - Review `Networking` fixtures for message registration lists; `RealEntityCompositionTests` for `ShipAbilityDependencies` construction.
6. [x] Recompile, console check. Update [[Architecture/Ship_Ability_Readiness_Targeting_And_Lifecycle]] rules (replicated fire).
7. [ ] User acceptance, 2 machines: client casts at a target dead ahead right after landing ×5 → wave each time on both; host casts → client sees wave; stun arcs on client.

## Outcome (2026-11-22)
- Host `IonPulseAbility` raises `IFiringShipAbility.Fired` with `ShipAbilityFire` (origin, planar direction, rise, range) → `ShipAbilityService.Fired` → `HostCombatEventSender` queues `ShipAbilityFireMessage` (flushed after activations) → `ClientCombatEventReceiver.OnAbilityFire` → `ShipAbilityService.FireReplica`.
- Replica (`IMatchSession.IsAuthority == false`): no `BeginAbilityFacing`/`EndAbilityFacing`, no cannon pick, no self-cancel, no self-fire; plays only the host geometry.
- Service subscribes once per started ability (abilities are single-use); no unsubscribe needed.
- Cause 2 resolved 2026-11-22 by user decision: no ability can be cancelled by its owner for now; new orders wait until the Ion Pulse fires. See [[Architecture/Ship_Ability_Readiness_Targeting_And_Lifecycle]] Rules.
- Reusable guidance → [[Architecture/Ship_Ability_Readiness_Targeting_And_Lifecycle]] Rules (replicated fire).

## Verification
- Unity recompile → 0 errors in changed files; `IFiringShipAbility` and new test methods present in the loaded assemblies.
- Tests reviewed: `ShipAbilityServiceTests` (+3: authority fires once with geometry; replica waits; replica `FireReplica` plays host geometry only while Active), `HealthModelTests` (constructor), `ShipAbilityFactoryTests` / `ShipAbilitySymmetryTests` / `FighterCoordinationModifierTests` (dependencies already carry `Session`, no change), `UnitOrderServiceTests` (receiver ctor unchanged). Category `Abilities`; not run (local override).
- In-game 2-machine check not done → acceptance queue.

## Edge Cases
- Fire message for an unknown caster or non-Active slot → dropped (activation was dropped too).
- Client joins mid-turn → baseline `RestoreState Active` has no running ability → later fire message ignored; cooldown still follows the host. Accepted.
- Host cancels before firing → client gets `Recovering` only; no wave (correct).
- Caster destroyed after firing → `SurvivesCasterDeath` keeps the wave on both sides.
- Client hit list comes from client positions → edge-of-wave targets can differ visually; host stun is authoritative. TODO if visible: replicate stun per target.

## Files
- `Assets/Scripts/Services/ShipAbilities/Abilities/IonPulse/IonPulseAbility.cs`, `IonPulseSettings.cs`
- `Assets/Scripts/Services/ShipAbilities/ShipAbilityService.cs`, `ShipAbilityDependencies.cs`
- `Assets/Scripts/Services/Networking/HostCombatEventSender.cs`, `ClientCombatEventReceiver.cs`, `Messages/ShipAbilityEventMessage.cs`
- `Assets/Scripts/Entities/Ship/Orders/ShipOrderRunner.cs` (cancel path, read only)
- `Assets/Scripts/Tests/Editor/Abilities/ShipAbilityServiceTests.cs`
