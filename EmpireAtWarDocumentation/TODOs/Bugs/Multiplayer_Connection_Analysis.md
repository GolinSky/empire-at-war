---
category: Bugs
status: in-progress
created: 2026-11-22
tags:
  - multiplayer
  - networking
---
# Multiplayer Connection Analysis

Parent → [[TODOs/Features/Multiplayer_Plan|Multiplayer Plan]]. Related → [[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]], [[TODOs/Optimization/Online_Battle_Low_FPS]].

## Goal

- Find the issues the online game has now. Matching the old plan is **not** a goal.
- Symptoms on a connected player: ships drag/slide while rotating; weapons fire slower with good client FPS.
- Add a network status overlay (cheat panel toggle) + per-battle CSV so the next match gives numbers on host and clients.
- Test match: online 3 humans vs 3 AI.
- Status: Phase 1 implemented 2026-11-22 (compiles, tests not run). Next = Phase 2 match by the user.

## Evidence (2026-11-21, `%USERPROFILE%\AppData\LocalLow\GolinSkyCompany\EmpireAtWar`)

- `Player.log` (build `0.1.76`, release) holds two matches:
  - Match `8ea0bae3…`, user = **client** (3-member Steam lobby). 7 player slots, battle time 213 → 5091 s. No host or AI data.
  - Match `8a460d0c…`, user = **host**. 2 humans vs 2 UltraHard Rebellion AI, ≈ 2400 s, `Outcome=PlayerVictory`, 0 exceptions, 0 disconnects.
- Neither is 3 humans vs 3 AI → that log is not on this machine.
- **No network numbers in any log**: no RTT, loss, bytes/s, buffer state, host FPS.
- **No projectile pool numbers either**: `ShotEffectCache` counters and `[ShotPoolPrewarmer]` lines are `DEVELOPMENT_BUILD`-only; 0 lines in both logs.
- → Both symptoms are unconfirmed by logs. Causes below are from source.

## Why 20 Hz, and where the design is tight

- 20 Hz itself is not the limit. `NETWORK_TICK_RATE = 20` is a batching/command clock ("not a send-every-tick rate", parent plan).
- The tight part is **what is sent per tick**. The parent plan's Sync Inventory rule = "send state changes and intents, not per-frame state":
  - Movement = dead-reckoning keyframe only when client prediction is off by > 2 m or > 5°, else 1 s refresh.
  - Budget ≤ 8 KiB/s average, ≤ 32 KiB/s peak per client.
- Origin: Research notes on Unity Relay free tier = 3 GiB × CCU ≈ **1.19 KiB/s per player**. Stack moved to Steam relay (no quota) on 2026-11-02, but the low-bandwidth movement design stayed.
- The plan itself says: "Bandwidth is a quality limit, not a cost" and "a naive 27 KiB/s (all units, positions at 20 Hz) is acceptable for the first playable". The code implemented the tight variant instead.
- → Confirmed: the bandwidth-saving design is a leftover of the Unity Relay limit and is the direct source of the movement symptom.

## Issues Now (source-verified 2026-11-22)

### A. Ships drag while rotating — design issue, high confidence

1. **Client guesses motion between sparse keyframes** (`DeadReckoningSender`, `ClientKeyframeTrack`). A visible error (2 m / 5°) is required before the host sends a correction.
2. **Motion model = speed along the nose** (`EntityMotion.Extrapolate`). Host moves the ship along the route while yaw still catches up (`ShipMovementTweenPlayer.cs:287-294`) → client predicts along the nose → sideways slide.
3. **Corrections snap.** New keyframe B arrives when render time ≈ `tB − 150 ms` → blend factor jumps to `1 − 0.15 / (tB − tA)` in one frame (85% for a 1 s gap). Position and heading blend separately.
4. **Turn acceleration not sent.** Host yaw accelerates (`StepYaw`, `_turnAcceleration`); keyframe has one whole-°/s `TurnRate` → heading reaches the 5° threshold several times per turn.
5. Lost unreliable keyframe → nothing until the 1 s refresh.
6. Side finding: ships never bank on clients (`SmoothBank` runs only in host movement).

### B. Client weapons fire slower — cause open, three candidates

1. **Projectile pool gate (user's hypothesis; still in code).** `WeaponHardPoint.cs:215` `TryReserveSalvo` → no free visual shot = salvo does not start. On a client the shot is cosmetic only, yet it still blocks.
   - Fixed 2026-11-18: any-fitting admission, deficit-based growth, ships/fighters split, stock per faction × slots.
   - Still open: Option A (never wait for a visual) undecided; runtime growth budget 0.5 ms/frame; fighter stock = station starting squadron only (carrier squadrons grow at runtime).
   - 3 factions: stock = one of every ship of each faction per slot; shot prefabs are shared across factions → mixed lobbies raise demand on the same pools.
   - Client-only difference: host prewarms squadrons during loading; client builds them on demand (`BattleStartupSequence.cs:85-89`) → their pool demand appears mid-battle.
   - Never measured ([[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]] Phase 1 open); counters are dev-only.
2. **Late host targets.** Weapon targets share one ordered reliable stream with health JSON (`EntityHealthMessage.HealthJson`, 4 Hz per changed unit), spawn JSON, state JSON. Backlog or one lost packet delays every target behind it. Not measured.
3. **Client-local gates on delayed poses.** `HasFiringSolution` (range + yaw arc) and `CanAcquire` (`VisionService.IsVisible`) run on client positions that are ≥ 150 ms old and popping.
   - Correction of the first draft: a pose correction does **not** restart a running salvo. It adds `yaw delta / TurnRate` delay per new salvo (`WeaponHardPoint.TurnTowards`) and cancels a pending salvo only if the target left arc/range when the turn ends (`FireTurnedSalvos`). Expected small; fixing A removes it.
4. Rule out: low host FPS lowers the real fire rate (host-side, separate plan).

### D. Client ships shoot at empty space (user report 2026-11-22; source, not measured)

1. **Squadron fighters fly a local simulation on clients; only the root follows the host.** High confidence.
   - `Squadron.Tick` runs `_pilot.Tick` + `_flight.Step` on replicas; `SquadronFlightComponent.QueuePoses` puts fighters at `Model` (local) positions in world space.
   - `ClientReplicationReceiver.LateTick` then moves only the root to the host pose → icon, selection, vision, health bar = host; fighters = local.
   - Clients replay only attacks (`ClientCombatEventReceiver.OnAttack` → `Squadron.Attack`). A host Move/Guard/Escort → `ApplyReplicaOrder` → `Stop()` → fighters loiter where they were. No client code flies them after the host.
   - Targeting uses fighter positions: `FighterView.Position` (local) for turrets firing at a squadron; `SquadronPilot` uses the target's root (host) for the attack zone.
   - → Turrets fire at where the client's fighters are, not where the host's squadron (icon) is; squadrons attack-run toward a root with no fighters near it. With fog deciding visibility at the root, the fighters being shot can be hidden.
   - Evidence of the same split: commit `d4711e085` "Fix multiplayer squadron icon position drift" moved the icon to the host root, not the fighters.
   - Also explains part of B: host says "engage squadron X", client fighters of X are out of range/arc locally → mount does not fire.
2. **Non-destroyed despawn leaves a ghost.** `ClientReplicationReceiver.OnDespawn` removes the track but destroys the entity only when `message.Destroyed`. An entity the host removed alive stays on the client, frozen, still in client target lists. To check: which host removals are not deaths (pool returns, handover leftovers).
3. **Stale death.** `EntityDiedMessage` is sent from the 0.25 s health pass → client keeps firing at a dead target ≤ 0.25 s + latency. Short; aimed at a wreck, not empty space.
4. **Position range.** Keyframe range ±8191 m. Large map ±4500 × `BoundsMultiplier` × √(players / 4) → 8 players ≈ ±6364 × multiplier. Off-map hyperspace entry points may exceed the range → clamped pose. To check per archetype.
5. Client base target lists are only added to, never pruned (host prunes its record at 32).

Fix direction for 1 (decision needed): drive client fighters from the host root — formation/loiter around the replicated root, local swarm only while the replayed attack is active and anchored to the host root. Overlay addition: distance between squadron root and local fighter centroid (max per second).

### C. Other verified facts

- `FizzySteamworks` `NextCommon.cs:20`: plain reliable/unreliable flags, Nagle on. No Steam send-rate config set → default (to verify).
- Reliable events apply on arrival; positions render 150 ms later → events run ahead of motion.
- Render delay fixed at 150 ms; never tuned (parent plan Phase 0 measurements not run).

## What Already Helps (keep)

- Steam relay: free, no quota → bandwidth can be spent on quality.
- `EntityKeyframe` 14 B + `KeyframeChunker` (69 per 1000 B chunk) → sending every moving unit every tick needs no new format.
- `ClientReplicaBuffer` already runs Mirror `SnapshotInterpolation` (150 ms timeline).
- `ShipRouteMessage` already sends each ship's route to clients (used for the route line only).
- Client ships run no local movement → one pose source, nothing to fight.
- `WeaponTargetMessage` + local firing → no per-shot traffic.
- Host listens on Steam and KCP at once → same match can compare relay vs direct IP.
- `SpikeBandwidthMeter` (Sandbox) shows how to read Mirror `NetworkDiagnostics`.

## Implementation

### Phase 1 — Done 2026-11-22

- Toggle: Cheats (Ctrl+Shift+F12) ▸ Debug ▸ "Show network status (online battle)". Shown only online. Cheat panel = local player scope, no build guard → reachable on release clients.
- CSV: `persistentDataPath/BattleTelemetry/<scene>_<yyyyMMdd_HHmmss>_<role>_net.csv` (+ `_pools.csv`, `_links.csv`), 1 row/s, flushed per window, every online machine. Header comments: role, transport, build, `mirror_send_rate_hz`, tick rate, render delay target.
- Shared columns: `begun, fps, frame_max_ms, shots_s, in/out_Bps, in/out_msgs_s`, `msg_<type>_s/_Bps` (keyframes, health, weapon_target, spawn, route, state, order_state, attack_target), `pool_waiting_shots, pool_oldest_wait_ms, pool_in_use, pool_total, pool_created_s, pool_blocked_attempts_s, pool_max_wait_ms`.
- Host columns: `tick_rate, keyframes_s, keyframe_chunks_s, entities, clients, rtt_ms` (max), `steam_*` (worst over clients). Sends `NetworkDebugStatsMessage` 1 Hz reliable.
- Client columns: `rtt_ms, rtt_sd_ms, steam_*, timeline_delay_ms, underruns_s, keyframes_s, keyframe_chunks_s, lost_chunks_s, corrections_s, correction_mean/max_m, correction_heading_mean/max_deg, entities, host_fps, host_tick_rate, host_shots_s, host_keyframe_chunks_s, host_entities, chunk_loss_pct, reliable_lag_ms, reliable_lag_max_ms`.
- `steam_*` = `ping_ms, quality_local/remote, out/in_Bps, send_rate_Bps, pending_reliable/unreliable_B, unacked_reliable_B, queue_ms` from `GetConnectionRealTimeStatus`. KCP links → empty.
- Definitions: correction = newest keyframe vs client prediction for that time. Reliable lag = newest keyframe host time − debug message send time; ≤ 0 = no backlog, up to −50 ms is same-frame order. `lost_chunks_s` sees only partly lost ticks; `chunk_loss_pct` compares with host chunks/s.
- Release changes: `ShotEffectCache` counters + `ShotPoolPrewarmer.Caches` + `AttackSequenceDiagnostics.EmittedShots` no longer dev-only.
- Vendored patch: `FizzySteamworks.TryGetClient/ServerConnectionStatus`, `NextClient.Connection`, `NextServer.TryGetConnection` (marked "Empire at War").
- Overlay overhead: our counters allocate nothing per message; Mirror boxes each message to raise `NetworkDiagnostics` events (release too). Lines/CSV row allocate once per second.
- Files: `Services/Networking/Diagnostics/*`, `Messages/NetworkDebugStatsMessage.cs`, `Entities/Cheats/Ui/NetworkStatusCheatPresenter.cs`, `ClientReplicaBuffer`, `ClientKeyframeTrack.TryPredict`, `NetworkTickService`, `HostTickReplicator.EntityCount`, `NetworkBattleRegistrations`, `EconomyModule`, `CheatView`/`ICheatView`.
- Tests: new `NetworkDiagnosticsTests` (`Networking`); `CheatPresenterTests` fake updated. Reviewed, no change: `NetworkTickReplicationTests`, `ShotEffectPoolLifecycleTests`. Not run (local override).
- Reusable guidance: none extracted yet; record measured results here in Phase 2.
- Source review 2026-11-22 (commit `dff5d8082`; nothing run): wiring, column consistency, Mirror counters in release, Steam status on client (`UseTransport(_fizzy)`) and host (`MultiplexTransport.OriginalId`) check out; last Editor compile succeeded after the commit. Gaps, all closed 2026-11-22 (Editor compile clean, tests not run):
  - [x] D1 not collected → client columns `squadrons, squadron_drift_mean_m, squadron_drift_max_m, squadrons_beyond_loiter` + overlay line. Drift = horizontal distance host pose (`ClientReplicaBuffer.TrySample`) ↔ local fighter centroid (`ISquadronFighterSpreadFacade` → `SquadronFlightModel.GetCentroid`); beyond = drift > `SquadronData.LoiterRadius`. Sampled once per window over all live replica squadrons (mean/max over squadrons, not over frames).
  - [x] Starving pool overlay-only → new columns `pool_ships_waiting, pool_fighters_waiting, pool_starving` + `<stem>_pools.csv` (1 row/s per pool with a salvo waiting: prefab, ships/fighters, waiting, in use, total, runtime created, oldest/max wait ms). `ShotPoolStats` owns the aggregation.
  - [x] Per-client links overlay-only → `<stem>_links.csv` (1 row/s per connection, host: every loaded client, client: `Host`; rtt + all `steam_*`, empty on KCP). Roles return `NetworkLinkSample`s; the collector writes overlay lines and rows.
  - [x] `pool_max_wait_ms` since start → per window (`ShotEffectCache.TakeWindowMaxAdmissionWaitSeconds`).
  - [x] CSV errors / name clash → `NetworkStatsFiles`: stem = `<scene>_<yyyyMMdd_HHmmss>_<role>`, `_net.csv` opened `CreateNew`, taken stem → `_2`, `_3`…; IO/permission error → one `Debug.LogError`, CSV stops, overlay keeps running.
  - [x] Tests (`NetworkDiagnosticsTests`, `Networking`): traffic counter (`RecordIn/RecordOut`), Steam worst-link aggregate, squadron drift math, pool stats ships/fighters split, file naming + rows, uncreatable folder, client column stability. `HostNetworkStats` still untested (needs `NetworkTickService` + `OnlineMatchService` + Mirror server).

### Phase 1 — original spec

- Toggle: checkbox in the IMGUI cheat panel (`CheatView`, beside "Show attack / radar range"). Overlay = IMGUI. Online battles only.
- Check first: cheat panel is reachable on clients in the release build.
- All machines: role, transport, build; RTT + variance (`NetworkTime`); Steam link quality + pending reliable/unreliable bytes (check Steamworks.NET API via Context7); bytes/s + messages/s in/out, top message types; FPS.
- Client: timeline delay vs 150 ms target, underruns/s; keyframes/s, lost ticks; correction size at keyframe arrival (m, °, mean/max per s); local shots/s vs host shots/s; weapon-target messages/s; reliable delay.
- **Projectile pools (both roles)**: per cache waiting shots, in use / total, runtime-created, max admission wait. Needs the `ShotEffectCache` counters available in release builds.
- Host: real tick rate, keyframes/s, health messages/s + bytes/s, entity count.
- New 1 Hz `NetworkDebugStatsMessage` host → clients: host FPS, tick rate, shots/s, send time.
- CSV: 1 row/s to `BattleTelemetry/<scene>_<time>_net.csv` on every machine in every online battle, overlay on or off.
- Files: `Services/Networking/Diagnostics/*`, `Messages/NetworkDebugStatsMessage.cs`, `ClientReplicaBuffer`, `ClientKeyframeTrack`, `ShotEffectCache`, `CheatView`/`ICheatView`/`CheatPresenter`, `NetworkBattleRegistrations`.
- Tests (`Networking`, `Combat`): collector math with built inputs; review `NetworkTickReplicationTests`, `ShotEffectCache*` tests.

### Phase 2 — Measure

1. 3 humans vs 3 AI over Steam, 3 different factions, 10+ min with one large fight. Collect `_net.csv` + `Player.log` from all 3 machines.
2. One client joins the same lobby by IP → separates Steam relay from our protocol.
3. Record here: RTT, loss, bytes/s per type, correction sizes, pool waiting shots, client vs host shots/s, reliable delay.

### Phase 3 — Fix by measurement

| Measurement | Fix |
|---|---|
| Corrections visible on a clean link (expected) | Movement approach B or C below |
| Pool waiting shots > 0 while fire is slow | Option A: salvo never waits for a visual (at least on clients) |
| Reliable delay > 200 ms / pending bytes growing | Binary health, own channel |
| Client shots/s < host with empty pools and on-time targets | Fire events (W2) |
| Underruns | Buffer 200–250 ms |
| Host tick < 20 Hz | [[TODOs/Optimization/Online_Battle_Low_FPS]] |

## Approaches

Movement:
- **A. Patch dead reckoning** — velocity direction separate from heading, smooth corrections over ≈ 0.3 s, thresholds 0.5 m / 2°. Smallest; guessing stays.
- **B. Send every moving unit every tick, interpolate** — no guessing, no pops. Uses existing keyframe + chunker + timeline. ≈ 14 B × 200 moving × 20 Hz ≈ 55 KiB/s per client at 20 Hz, ≈ 28 at 10 Hz. Free on Steam relay. **Recommended first.**
- **C. Replicate the route** — client runs the same `ShipMovementTweenPlayer` on the host's route with time correction. Exact turns + banking, lowest bandwidth, largest change (replans, pace clusters, combat hold).
- **D. Hybrid** — C for ships, B for squadrons.
- Avoid: deterministic lockstep (sim not deterministic → parent plan).

Weapons:
- **W0. Never gate a salvo on a visual shot** (Option A of the fire-rate plan); skip the visual when the pool is empty.
- **W1. Unblock the reliable stream** — binary health, separate channel.
- **W2. Fire events** — host sends per-salvo fire; client rate = host rate.
- **W3. Tick-stamp reliable events**, apply at render time.

## Edge Cases

- Overlay must not change the measurement: counters only, no per-message allocations.
- Verify Mirror `NetworkDiagnostics` events fire in the release build; otherwise count in our own send/receive paths.
- Host's local client has no battle handlers → debug stats go through `HostBroadcast.ToClients` only.
- B raises host upload ×(clients): 5 clients × 55 KiB/s ≈ 275 KiB/s → check against the Steam send-rate default before choosing 20 Hz vs 10 Hz.

## TODO

- [x] User: approve Phase 1 (2026-11-22).
- [x] Phase 1: overlay + pool counters + CSV + `NetworkDebugStatsMessage` (compiles; tests not run).
- [ ] Phase 2: 3v3 Steam match + one KCP client; record numbers here.
- [x] Mirror `sendRate`: not set on `OnlineNetworkManager` → `NetworkManager` default 60 Hz (source). CSV header records the runtime value.
- [ ] Steam send rate: read `steam_send_rate_Bps` from the Phase 2 CSV (runtime value, not yet seen).
- [x] Phase 1 review gaps closed (D1 drift columns, pools/links CSV, per-window max wait, guarded unique files, tests) — 2026-11-22, compile clean, tests not run.
- [ ] D1: **measure first (user, 2026-11-22)** — no fighter-anchoring fix until the Phase 2 client CSV shows `squadron_drift_*`. Confirmed if `squadrons_beyond_loiter` stays > 0 after host move orders.
- [ ] D2/D4 checks.
- [ ] Phase 3: user picks movement approach (B recommended) and weapon fix from the table.
