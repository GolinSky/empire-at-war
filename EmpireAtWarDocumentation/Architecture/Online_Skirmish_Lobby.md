---
type: engineering-reference
system:
  - networking
  - ui
verified: 2026-11-16
verification: Source/diff review and Unity compilation up_to_date, failed=false,
  errors=[]; automated tests not run.
---
# Online Skirmish Lobby

## Rules

- Capacity: 8 total humans, including host. Joining friends take Vacant → Closed → AI rows; closing unused rows does not prevent invitations from joining.
- A full human lobby rejects player 9 with `LobbyFull`. Map capacity still controls battle start; 8 players require a Large map.
- `OnlineMatchService.BroadcastLobby` reconciles Skirmish seats before sending Members + SetupJson and before notifying UI observers.
- Host/client views consume the same authoritative snapshot. Seating must not depend on the host screen being visible or rendering successfully.
- Occupied-row removal is explicit Kick. Closing an unused row does not disconnect anyone.

## Implementation

- `SynchronizeSkirmishSetup`: restore current setup (or default host setup), seat connected members, serialize once before broadcast. Keeps faction/team/color/map choices.
- `OnlineSkirmishLobbyUiController.Sync`: apply service snapshot on host and client; suppress publication while applying. Host edits publish before rendering.
- `Show`: initialize local role before enabling model-change publication; opening the view must not overwrite the authoritative roster with host-only defaults.
- `SkirmishSlotRowView`: invalidate expanded occupant dropdown on changed options, member ID, occupant kind or editability. Hide old popup and reject selections until TMP destroys the closing list; preserve fresh explicit Kick.
- TMP `Hide` retains popup during fade → `IsExpanded` remains true. Replacing `options` alone does not rebuild cloned popup choices.

## Files

- `Assets/Scripts/Services/Networking/Online/OnlineMatchService.cs`
- `Assets/Scripts/Entities/MainMenu/Online/OnlineSkirmishLobbyUiController.cs`
- `Assets/Scripts/Entities/MainMenu/Skirmish/SkirmishLobbySeating.cs`
- `Assets/Scripts/Entities/MainMenu/Skirmish/SkirmishSlotRowView.cs`
- Regression fixtures: `OnlineLobbyModeTests` (`Networking`), `SkirmishModelTests` (`GameFlow`), `SkirmishUiPrefabTests` (`Ui`).
- [[Done/Bugs/Online_Skirmish_Join_Slot_Race]] — implementation evidence; automated runs pending authorization. Reproduction was explicitly declined by user.
