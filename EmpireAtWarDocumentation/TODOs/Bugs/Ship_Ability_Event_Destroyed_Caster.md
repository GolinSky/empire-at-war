---
category: Bugs
status: todo
created: 2026-11-18
---
# Ship Ability Event Throws for a Destroyed Caster (online host)

## Evidence
- `Logs/OnlineBattles/20261117T1650Z/raw/Player.log` lines 5021, 8412, 8623, 13363, 26886, 42111, 52088, 56892, 64703 (9×, every hosted battle).
- `Exception: Not entity found with id: N` at `EntityLocator.GetEntity` ← `ShipAbilityEventMessage.Capture` ← `HostCombatEventSender.OnAbilityStateChanged` ← `ShipAbilityService.Stop` ← `Advance` (in battle) / `LateDispose` (teardown).
- Each in-battle throw is followed by `Uploading Crash Report`.

## Cause
- `Capture` reads `slot.Owner.Entity.Id`; the caster has already left `EntityLocator` when its ability stops.
- The exception leaves `OrderedLifecycleCoordinator.Tick` → later tickables likely skip that frame (verify).

## Plan
- [ ] Send the caster id stored on the slot/caster at activation, not via locator lookup; or skip the event when the caster was despawned (clients already despawn it).
- [ ] Teardown: `HostCombatEventSender.LateDispose` unsubscribes before `ShipAbilityService.LateDispose` stops slots (check order).
- [ ] Test (`Networking`/`Abilities`): ability stop after caster removal sends no throw.

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Files
- `Services/Networking/Messages/ShipAbilityEventMessage.cs`, `Services/Networking/HostCombatEventSender.cs`, `Services/ShipAbilities/ShipAbilityService.cs`
