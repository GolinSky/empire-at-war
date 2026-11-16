---
category: Bugs
status: done
created: 2026-11-16
completed: 2026-11-16
acceptance: pending-user
---
# Online Skirmish Join Slot Race

## Goal

- Friend joins open closed rows automatically; host and clients display the same seated roster, up to 8 players including host.
- Stale slot dropdown choices never kick a newly joined player.

## Decision

- User confirmed Online Skirmish and explicitly requested implementation without reproduction on 2026-11-16.
- Preserve Vacant → Closed → AI seating priority. Earlier proposal to reject joins with all unused rows closed was rejected and is superseded.
- Full human capacity rejects player 9. Occupied-row removal remains explicit Kick; map-size battle capacity is unchanged.
- Canonical reusable guidance → [[Architecture/Online_Skirmish_Lobby]].

## Implementation

- `OnlineMatchService.SynchronizeSkirmishSetup` restores current setup, seats every connected member and serializes before `BroadcastLobby` sends or notifies observers.
- `HostLobby` publishes initialized seating. Join, leave, Kick, ready changes and returning to Skirmish use the same broadcast reconciliation.
- `OnlineSkirmishLobbyUiController` applies service snapshots on host and client; suppresses republishing during apply and publishes host edits before rendering.
- View initialization no longer publishes a temporary host-only roster. Seating/publication no longer requires the UI to be open.
- `SkirmishSlotRowView` invalidates expanded occupant choices on options, member identity, occupant kind or editability changes; blocks stale selections/reopening during TMP fade and preserves fresh Kick.
- New hosted lobbies initialize default setup; existing lobby snapshot edits retain map/team/color/faction choices.

### Investigation evidence

- Old order: new Members + old SetupJson sent → host UI seats/render → corrected snapshot. Rendering failure could prevent correction; no matching local exception found.
- TMP popup items are cloned on Show. Updating options/caption does not rebuild an expanded popup; stale positive index could clamp to new Name/Kick index 1.
- Exact reported persistent visual failure was not reproduced. User declined reproduction as a prerequisite; fixes target verified logic paths.

### Verification — 2026-11-16

- Unity `recompile` and `recompile_status`: `up_to_date`, `failed:false`, `errors:[]`. Both script assemblies timestamped after the final changed test source.
- Source and test diff reviewed together; targeted `git diff --check` clean. Independent review found no actionable production regression.
- No automated tests executed. New `Ui` regression includes Play Mode lifecycle but was authored only; no Editor scenes changed/prepared.
- Reviewed/extended `OnlineLobbyModeTests` (`Networking`): actual OnHello joins through 8, complete snapshot before Changed without UI, closed-seat reopening preserves choices, mode switching, ninth rejection.
- Reviewed/extended `SkirmishModelTests` (`GameFlow`): stable eight-member seating, client row permissions, departure/rejoin preserves remaining choices.
- Reviewed/extended `SkirmishUiPrefabTests` (`Ui`): Closed/Vacant/AI → Human, same-name replacement, editability revocation, stale callbacks and fresh Kick.
- Existing unrelated recovery/LeaveLobby changes preserved. No prefab or serialized asset changes.

## TODO

- [x] Fix authoritative reconciliation and preserve automatic closed-slot opening up to 8 humans.
- [x] Fix stale dropdown ownership/actions through popup fade-out.
- [x] Add regression coverage; review source/test diff and verify Unity compilation.
- [x] Extract reusable rules to [[Architecture/Online_Skirmish_Lobby]].
- Automated execution is a user-only gate → [[TODOs/User_Acceptance_Queue]]. Runtime reproduction is not required by the accepted task scope.

## Files

- `Assets/Scripts/Services/Networking/Online/OnlineMatchService.cs`
- `Assets/Scripts/Entities/MainMenu/Online/OnlineSkirmishLobbyUiController.cs`
- `Assets/Scripts/Entities/MainMenu/Skirmish/SkirmishSlotRowView.cs`
- `Assets/Scripts/Tests/Editor/Networking/OnlineLobbyModeTests.cs`
- `Assets/Scripts/Tests/Editor/GameFlow/SkirmishModelTests.cs`
- `Assets/Scripts/Tests/Editor/Ui/SkirmishUiPrefabTests.cs`
