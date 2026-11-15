---
category: Bugs
status: done
created: 2026-11-15
updated: 2026-11-15
completed: 2026-11-15
acceptance: pending-user
---
# Multiplayer Reconnect Flow

## Goal

- Restore a disconnected/crashed client's running battle through Reconnect or startup Rejoin previous match.
- Parent: [[TODOs/Features/Multiplayer_Plan]]. No host migration, new-player late join or online save/load.

## Decision

- Implemented 2026-11-15. Source and Unity compilation verified; automated tests and two-PC runtime acceptance not run.
- Closed with `acceptance: pending-user`; acceptance and historical two-machine diagnosis → [[TODOs/User_Acceptance_Queue]].
- Reusable ownership, persistence, deadlines, baseline and loading rules → [[Architecture/Multiplayer_Reconnect]].

## Implementation

### Recovery and identity

- [x] Persist original transport, host/lobby, account, match ID, slot and private credential before battle loading. Temporary write + flush + atomic replace/move; no battle snapshot or logged credentials.
- [x] Startup restore + corrupt/unsupported-record error; changed Steam account rejects recovery.
- [x] Reconnect/Leave and startup Rejoin previous match/Dismiss; duplicate attempts disabled, retries retain destination, no automatic retry loop.
- [x] Transient loss preserves the record and reports Connection lost. Leave, kick, completed match and terminal rejection clear it; deletion failures remain visible.
- [x] Private match-scoped credential replaces display-name matching. Fizzy connection address supplies verified Steam identity; client hello SteamId is not trusted.
- [x] Valid early rejoin supersedes stale connection after rebinding; wrong credential/account leaves ownership untouched. Extra bounded transport capacity permits handshake in a full match.
- [x] Host restart preserves match credential/reservation. Explicit connected Leave revokes membership; inaccessible Dismiss clears only local state.

### Deadlines and synchronization

- [x] Host grace **300 s**; first accepted rejoin allowance **180 s**. Deadline = later of original grace and first acceptance + 180 s; retries/loading crashes cannot extend it.
- [x] Reservation remains until loaded membership + begun battle + client synchronization acknowledgment. Expiry removes connected/loading members too and commits AI takeover.
- [x] Reuse reliable full baseline: units/health, credits/production/research, reserve/cooldowns, Survival, targets/abilities, routes, orders and match state before BattleBegin/input release.
- [x] Corrected order baseline to read live current order rather than previous tick cache.
- [x] Visible Connecting → Loading battle → Synchronizing → Playing; accepted host allowance displayed during loading. Startup uses Checking session until acceptance, without inventing disconnect time.

### Lifecycle and UI

- [x] Project-lifetime HostLeftBattleExit handles disconnect during preloading; battle teardown preserves transient recovery.
- [x] BattleGeneration ownership prevents old scene disposal/completion from closing a newer session.
- [x] Canceled attempt callbacks and delayed disconnects cannot mutate a replacement session. Accepted lobby cancels the 30 s connection timeout.
- [x] Existing loading canvas remains through synchronization; camera/audio ownership hands off at target activation. Independent OnlineRecoveryProgress avoids a SceneService/network/game-command dependency cycle.
- [x] OnlineUi prefab and Loading scene imported/saved; serialized references checked and both 1920×1080 previews inspected.

### Verification

- [x] Authored Networking fixtures: OnlineReconnectTests, OnlineMatchRecoveryTests, ReconnectSessionStoreTests, ReconnectOrderBaselineTests.
- [x] Authored GameFlow fixture: OnlineBattleLifetimeTests. Authored Ui fixture: OnlineRecoveryUiTests; updated LoadingUiIsolationTests.
- [x] Updated constructor dependencies in OnlineLobbyModeTests, TacticalMarkerNetworkingTests, MatchSessionTests and PopupServiceTests.
- [x] Reviewed NetworkTickReplicationTests, SuperWeaponCooldownReplicationTests and baseline send/receive ordering; existing assertions remain valid.
- [x] Unity refresh/compilation completed; current assembly exposes new service constructor, Editor not compiling/updating, Console reported zero errors. Saved asset bindings and source diff checked.
- Tests **not run** under the checkout's local override; source inspection is not a passing test run.
- Serena transport remained unreachable after idempotent launcher check; Graphify query timed out. Live source + Unity output used; no tool/configuration changes.

## Edge Cases

- Pre-fix source: 60 s grace, display-name rebind, early rejoin rejected while old connection looked connected, rebind removed timer before loading; no dedicated reconnect action.
- Running battles remain hidden from ordinary server discovery. Direct Steam recovery uses saved host destination; IP is not a Steam fallback.
- Host remains authoritative. A last-second accepted recovery can reserve up to **480 s** from detected loss; simulation continues without temporary AI/reclaim swapping.
- Graceful host announcement → Host ended the match. Transport loss alone cannot prove host termination.
- Historical Steam Join Game report: local Player.log modified 2026-11-14 23:05 shows lobby entry (line 443), connection established (496), peer/server disconnect (508); earlier cycle 342→354 agrees. Exact host rejection cause remains unproven; do not attribute it to App ID 480.
- OnlineMatchService remains a large existing coordinator. Persistence and deadline policy are extracted; a broader coordinator refactor is outside this fix.

## TODO

- User gate: two-PC Steam + KCP loss/rejoin and forced application termination/restart; measure startup/load, validate near-expiry acceptance, repeated crash deadline, AI takeover and host exit.
- User gate: no duplicate units; restored ownership, units/health/orders, credits/production/reserve/research/cooldowns and Survival before input.
- User gate: authorized Networking, Ui and GameFlow test execution.
- User gate: capture matching host/client Steam Join Game logs to establish the historical rejection cause; current client log proves connection succeeded, not why the server disconnected it.

## Files

- `Assets/Scripts/Services/Networking/Online/` — handshake, credential reservation, persistence and progress.
- `Assets/Scripts/Services/Networking/HostUnitOrderSender.cs`, `OnlineBattleLifetime.cs`, `HostLeftBattleExit.cs`.
- `Assets/Scripts/Services/SceneService/SceneService.cs`; project/battle VContainer registrations.
- `Assets/Scripts/Entities/MainMenu/Online/`, `Assets/Prefabs/Ui/MainMenu/OnlineUi.prefab`.
- `Assets/Scripts/Entities/Loading/`, `Assets/Scenes/Loading.unity`.
