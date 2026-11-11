---
category: Features
status: done
created: 2026-11-11
completed: 2026-11-11
acceptance: pending-user
---
# Play Menu Reroute Plan

## Goal

- Group the Play screen by how you play: Single Player, Multiplayer, AI vs AI, Tutorial.
- Choose the game mode in the multiplayer lobby, not before hosting.
- Use the same button names everywhere: `START GAME` for playable modes, `START SIMULATION` for AI vs AI.

## Outcome (2026-11-11)

```
Main Menu → Play
├── SINGLE PLAYER → SKIRMISH | SURVIVAL | LOAD GAME → setup → START GAME
├── MULTIPLAYER → HOST GAME → lobby (host switch SKIRMISH | SURVIVAL) → START GAME
│               → JOIN GAME → Server Browser (Steam lobby list, REFRESH, JOIN by IP) → lobby
│               → JOIN <friend> (pending Steam invite only)
├── AI VS AI → Simulation Setup → START SIMULATION
└── TUTORIAL → faction button starts the battle (no extra Start button)
```

## Decision

- **Load Game** → Single Player screen. Why: saves are offline battles.
- **Lobby mode switch** → host-only buttons in the setup screen header; Survival refused while members > `OnlineLineup.MAX_SURVIVAL_HUMANS` (3). Avoid: kicking players on switch.
- **Switch effects** → capacity changes (Steam member limit too), setup snapshot cleared, every client's ready cleared, `OnlineRouteUiController.Route()` swaps the setup screen.
- **Default lobby mode** → Skirmish.
- **Server Browser** → Steam lobbies are now **public** (`k_ELobbyTypePublic`); list filters: `game`, `version`, `state=lobby`, ≥1 free slot, worldwide, max 50. Avoid: friends-only lobbies (`RequestLobbyList` cannot see them).
- Host sets lobby data `host_name`, `mode`, `state`; `state=battle` on battle start hides the lobby from the browser (invite/IP rejoin unchanged).

## Implementation

- `Entities/MainMenu/SinglePlayer/`: `ISinglePlayerRoute`, `ISinglePlayerPresenter`, `SinglePlayerRouteUiController`, `SinglePlayerUi`.
- `Entities/MainMenu/ServerBrowser/`: `IServerBrowserRoute`, `IServerBrowserPresenter`, `ServerBrowserRouteUiController`, `ServerBrowserUi`, `ServerBrowserRowView`, `ServerBrowserRowState`.
- `Online/LobbyModeSwitchView` (sub-view MonoBehaviour) on `SkirmishUi.lobbyModeSwitch` / `SurvivalUi.lobbyModeSwitch`; lobby controllers render it host-only.
- `PlayUi` fields → `singlePlayerButton`, `multiplayerButton`, `simulationButton`, `tutorialButton`, `backButton`.
- `OnlineUi` fields → `hostGameButton`, `joinGameButton`, invite + back + status unchanged. `IOnlinePresenter` → `HostGame`, `OpenServerBrowser`, `AcceptInvite`, `Close`.
- `OnlineMatchService`: `HostLobby()` (no mode), `CanSwitchLobbyMode`, `SwitchLobbyMode`, `JoinListedLobby`. `OnlineLobby.ChangeCapacity`.
- `SteamPlatform`: `CreateLobby(maxMembers, mode)`, `UpdateHostedLobby`, `SetHostedLobbyInBattle`, `RequestLobbyList` → `LobbyListReceived` / `ListedLobbies` (`SteamLobbyListing`).
- `MainMenuOrchestrator`: pending load error opens Play → Single Player → Load Game.
- Labels: `SurvivalUi.prefab` and `OnlineSurvivalLobbyUiController.START_LABEL` → `START GAME`. Main menu Play tooltip updated.
- Registration: `MainMenuScope`, `UiPlacement` (default canvas), Addressables `SinglePlayerUi`, `ServerBrowserUi` in `Ui` group.

## Files

- Prefabs: `Assets/Prefabs/Ui/MainMenu/{PlayUi,SinglePlayerUi,OnlineUi,ServerBrowserUi,SkirmishUi,SurvivalUi}.prefab`.
- Tests: `MainMenuUiControllerLifecycleTests` (`PlayRoute_…`, new `SinglePlayerRoute_ForwardsOfflineModesAndReturnsOnCancel`, orchestrator load-error path), new `OnlineLobbyModeTests` (`Networking`).

## Edge Cases

- Back in Server Browser while connecting → join cancelled (`OnlineMatchService.Leave`).
- Join from browser enters lobby → browser hides; lobby end → Multiplayer screen with reason.
- No Steam → browser shows IP join only; REFRESH disabled.
- Multiplayer invite button stays hidden without a pending invite (same gap as before the reroute).
- Steam App ID is still 480 (Spacewar): the list relies on the `game` + `version` filters.

## Verification

- Unity compile clean (runtime + tests); prefabs reserialized; every `EmpireAtWar.Entities.MainMenu` serialized reference non-null; buttons Color Tint; Addressable keys resolve in `Ui` group.
- Preview renders checked: Play, Single Player, Multiplayer, Server Browser, Skirmish header switch.
- Tests not run (local override). Categories to run: `Ui`, `Networking`, `GameFlow`.
- Reusable guidance: none new; screen setup followed [[Rules/UI_CODE_BUILD_GUIDE]] as written.

## TODO

- [x] Confirm decisions (user: "do all the phases", proposed decisions applied)
- [x] Phase 1 — Play + Single Player screens
- [x] Phase 2 — START GAME labels
- [x] Phase 3 — Host Game + lobby mode select
- [x] Phase 4a — Join Game screen
- [x] Phase 4b — Server Browser
- [ ] User acceptance → [[TODOs/User_Acceptance_Queue]]
