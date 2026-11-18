---
category: Bugs
status: done
created: 2026-11-18
completed: 2026-11-18
acceptance: pending-user
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
- [x] Caster id comes from `IShipAbilityFacade.EntityId` (stored id, no locator lookup) in `Capture`, `OnActivated`, `OnCanceled` (commit e84297792).
- [x] Teardown order no longer matters: `Capture` cannot throw for a despawned caster.
- [x] Test `ShipAbilityServiceTests.EventCapture_AfterCasterLeftLocator_UsesStableCasterId` (`Abilities`). Not run (local override).

## Outcome
- Done 2026-11-18. Verified by source inspection only; tests not run. No reusable guidance beyond: ability facades expose a stable `EntityId`; never read `Entity` on stop/teardown paths.
- Pending user: confirm no `Not entity found` in next hosted battle log.

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Files
- `Services/Networking/Messages/ShipAbilityEventMessage.cs`, `Services/Networking/HostCombatEventSender.cs`, `Services/ShipAbilities/ShipAbilityService.cs`
