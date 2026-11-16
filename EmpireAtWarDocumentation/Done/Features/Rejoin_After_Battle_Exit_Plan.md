---
category: Features
status: done
created: 2026-11-16
parent: TODOs/Features/Multiplayer_Plan
completed: 2026-11-16
acceptance: pending-user
---
# Rejoin After Battle Exit

## Goal

- Client Pause Exit → resume the same online battle through Multiplayer Reconnect, Steam Join Game, Steam invite, Server Browser Rejoin, or Join by IP for the same KCP endpoint.

## Decisions

- Client Exit suspends the connection and keeps recovery. The host reserves the slot for 300 s from disconnect and gives it to AI on expiry. Existing 180 s loading allowance applies.
- Multiplayer Leave while disconnected clears the local record. The host reservation expires on its normal deadline.
- A Steam request for the live battle's current lobby is ignored. A different Steam game suspends the old seat. Entering a replacement battle replaces the saved record.
- Server Browser adds one account-matched saved Steam battle row. It reuses direct Reconnect. Public Steam discovery remains restricted to open lobbies.
- Host Exit announces `HostLeft` and ends the match.

## Implementation

- `OnlineBattleLifetime` preserves generation and restart guards, and calls `OnlineMatchService.SuspendBattle` for recoverable client battles. Match end clears recovery.
- `SuspendBattle` stops the transport without `LobbyLeaveMessage` and keeps `reconnect-session.dat`. Returning to Multiplayer shows Reconnect and Leave. Quitting afterward offers Rejoin previous match at startup.
- Join by IP checks the stored KCP host and port. Rejoin sends the existing credential. A different endpoint starts a fresh join.
- Steam rejoin matches saved lobby, host and account. Back from a Server Browser reconnect cancels the attempt and preserves the record.
- Pause Exit tooltip explains the reconnect window to clients and the match-ending action to hosts.
- Reusable rules and edge cases → [[Architecture/Multiplayer_Reconnect]].

## Verification

- Unity script compilation completed with zero reported errors on 2026-11-16.
- Updated `OnlineBattleLifetimeTests`, `OnlineMatchRecoveryTests`, `SteamPlatformLifecycleTests`, and `OnlineRecoveryUiTests`. Reviewed `PauseMenuUiPrefabTests` and `ModalRouteInputTests`; existing wiring and assertions still apply.
- Automated tests and two-PC acceptance are pending → [[TODOs/User_Acceptance_Queue#Multiplayer Reconnect]].
