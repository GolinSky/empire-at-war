---
type: engineering-reference
system:
  - networking
verified: 2026-11-15
verification: Live source and reliable baseline ordering inspected; automated
  tests and two-PC acceptance not run.
---
# Multiplayer Reconnect

## Rules

- Client recovery only; host process must survive. No host migration, new-player late join or battle snapshot persistence.
- `ReconnectSessionStore` writes `reconnect-session.dat` under `Application.persistentDataPath` before battle loading: temporary file + durable flush + atomic replace/move. Never log or commit credentials.
- Record includes original KCP/Steam transport, host, port, lobby, account, match ID, private credential and slot. Rejoin uses that destination directly; KCP is not a Steam fallback.
- `OnlineLobby` keeps credentials private; never add them to broadcast `LobbyMember`. Names are display-only. Steam identity comes from the connection address supplied by FizzySteamworks, never the client's `SteamId` field.
- A valid credential may replace a still-connected stale connection. Rebind ownership before disconnecting it; its later disconnect callback must not reserve the new binding.
- Transport loss means `ConnectionLost`. `HostLeft` requires an explicit host announcement; unreachable hosts cannot prove a terminal outcome.

## Important Values

- Host disconnect grace: **300 s**. First accepted recovery: deadline = `max(original grace deadline, acceptance time + 180 s)`.
- Retries, repeated loading crashes and rebinds do not reset the deadline. Maximum late acceptance reservation: **480 s** from detected loss.
- Host removes the deadline only after `BattleLoadedMessage`, reliable baseline delivery and `BattleSynchronizedMessage`. Expiry removes the member and raises `PlayerAbandoned` once.
- Connect attempt timeout: **30 s**; accepted lobby state cancels it. No automatic retry loop.
- Protocol revision: `OnlineMatchService.RECONNECT_PROTOCOL = 1`; both peers need the reconnect-compatible handshake.

## Implementation

1. Host sends private match credential with `BattleStartMessage`; client persists before starting scene loading.
2. Disconnect preserves the record; Multiplayer offers Reconnect/Leave. Startup restores Rejoin previous match/Dismiss.
3. Rejoin validates match, credential and transport identity before replacing ownership. Expired/kicked/completed/mismatched sessions reject recovery.
4. `HostBattleStartGate` sends spawns/health, replicated state, weapon targets, combat/ability state, routes, current orders and match state before `BattleBeginMessage`.
5. Client input waits for the battle start gate. `HostUnitOrderSender.SendBaseline` reads live orders rather than the previous tick cache.
6. `OnlineRecoveryProgress` shares presentation with loading without a `SceneService` → `OnlineMatchService` dependency cycle. The loading canvas remains through synchronization.

## Edge Cases

- Project-lifetime `HostLeftBattleExit` handles loss during preloading as well as an active battle. `OnlineBattleLifetime` captures `BattleGeneration`; stale scene disposal cannot close a replacement session.
- Client Pause Exit and application quit stop the transport without `LobbyLeaveMessage`. The saved record survives, and the host starts its normal reservation on disconnect. Host Pause Exit announces `HostLeft`.
- Same KCP host and port → Join by IP uses the saved record. Steam Join Game and invites reuse the record for the matching lobby, host and account. Server Browser shows a private REJOIN row from the current Steam account's saved record and connects to the stored host.
- A Steam request for the current battle lobby is ignored. Switching to another Steam game from battle suspends the current seat and keeps the record while joining the other lobby. Starting the replacement battle saves its own record.
- Temporary failures retain recovery. Leave/Dismiss, kick, completed match and terminal rejection of the recorded session clear it.
- Early Steam rejoin is transport-safe: FizzySteamworks `NextServer` (default `UseNextGenSteamNetworking = true`) keys connections by `HSteamNetConnection`, so a second connection from the same SteamID is accepted. `LegacyServer` keys by SteamID and would not be.
- Recovery countdown text rebuilds and raises `Changed` only when the whole-second value changes, not every frame. Explicit connected Leave revokes the host credential; unreachable Dismiss clears only local state.
- Host restart preserves match ID/credentials and outstanding reservation deadlines. Successful recovery keeps the record for another crash.
- Corrupt/unsupported record → discard + visible error. Changed Steam account → reject; never fall back to display-name matching.
- Historical 2026-11-14 client logs show lobby entry and Steam transport connection followed by server disconnect. Exact historical rejection needs the host log; App ID 480 is not established as the cause.
- Two-PC restored ownership, no duplicate units, near-expiry loading, host exit and measured startup times remain user acceptance gates.

## Files

- `Assets/Scripts/Services/Networking/Online/OnlineMatchService.cs`, `OnlineLobby.cs`, `ReconnectReservations.cs`, `ReconnectSessionStore.cs`.
- `Assets/Scripts/Services/Networking/HostBattleStartGate.cs`, `HostUnitOrderSender.cs`, `OnlineBattleLifetime.cs`, `HostLeftBattleExit.cs`.
- `Assets/Scripts/Entities/MainMenu/Online/`, `Assets/Scripts/Entities/Loading/`, `Assets/Scripts/Services/SceneService/SceneService.cs`.
- [[Done/Bugs/Multiplayer_Reconnect_Flow]], [[Done/Features/Rejoin_After_Battle_Exit_Plan]], [[TODOs/Features/Multiplayer_Plan]].
