---
category: Bugs
status: in-progress
created: 2026-11-22
tags:
  - multiplayer
  - networking
---
# Multiplayer Connection Analysis

Parent → [[TODOs/Features/Multiplayer_Plan|Multiplayer Plan]]. Related → [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]], [[TODOs/Optimization/Online_Battle_Low_FPS]].

## Goal

- Find the issues the online game has now. Matching the old plan is **not** a goal.
- Symptoms on a connected player: ships drag/slide while rotating; weapons fire slower with good client FPS.
- Add a network status overlay (cheat panel toggle) + per-battle CSV so the next match gives numbers on host and clients.
- Test match: online 3 humans vs 3 AI.
- Status: Phase 1 implemented 2026-11-22 (compiles, tests not run). Phase 2 measured: host match 2026-11-22 (`Results`), two client matches 2026-11-23 (`Results 2`). Main defect = client squadron fighters 1.5–2 km off the host position (D1). Phase 3 items 1–3 + telemetry part implemented 2026-11-23 (compile clean, tests not run). Next = user re-measures in an online match as client (Phase 3 step 4).

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
   - Never measured ([[Review/Bugs/Weapon_Fire_Rate_Low_FPS]] Phase 1 open); counters are dev-only.
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

### Phase 2 — Results (host only, 2026-11-22)

Source: `BattleTelemetry/Loading_20261122_201250_Host_net.csv` (3029 rows, no gaps), `_Host_links.csv`, `_Host_pools.csv`, `Player.log`. Build `0.1.77`, release exe, Steam relay.

- Match: user = host, **1 client**, 2 humans (Empire) vs 2 AI (Rebellion), 3037 s, `Outcome=PlayerVictory`, 0 exceptions, 1 disconnect (after leaving the lobby).
- Not the planned test: no 3v3, no KCP client. **No client CSV on this machine** → `corrections_*`, `squadron_drift_*`, `reliable_lag_ms`, client shots/s, client pools are still unmeasured.

| Measure | Mean | p95 | Max |
|---|---|---|---|
| Host FPS | 202 | min 82 | — |
| `frame_max_ms` | 11.8 | 46 | 158 (load) |
| `tick_rate` | 20.0 | 20.9 | min 19.5 |
| `entities` | 166 | 213 | 227 |
| `rtt_ms` | 16.8 | 22.3 | 27.6 |
| `out_Bps` | 42 005 | 58 885 | 74 260 |
| `in_Bps` | 1 343 | 1 407 | 1 586 |
| `keyframes_s` | 689 | 934 | 1 122 |
| `shots_s` | 109 | 239 | 408 |
| `steam_pending_reliable_B` | 287 | 2 074 | 14 348 |
| `steam_queue_ms` | 0.9 | 8.6 | 51.8 |

Bytes out by type (whole match):

| Message | msgs/s | B/msg | Share |
|---|---|---|---|
| `ReplicatedStateMessage` (`msg_state`) | 4.6 | 3 489 | 38.3% |
| `EntityHealthMessage` | 47.4 | 303 | 34.1% |
| `EntityKeyframesMessage` | 20.1 | 461 (≈ 34 keyframes) | 22.1% |
| `EntitySpawnMessage` | 0.5 | 634 | 0.7% |
| `WeaponTargetMessage` | 21.6 (max 729) | 9 | 0.5% |
| route + order state + attack target | 6.4 | — | 0.3% |

Findings:

1. **Link is clean.** RTT ≤ 28 ms, Steam quality 1.0 both ways all match, queue ≤ 52 ms, reliable backlog ≤ 14 KB and drains within the second. → Anything the client saw is our protocol, not the network.
2. **Host is not the limit.** Tick 20.0 Hz all match, FPS ≥ 82. → B4 (low host FPS) ruled out for this match.
3. *(Corrected by Results 2, finding 2: the excess keyframes are squadron roots, not ships.)* **Dead reckoning mispredicts all the time.** 689 keyframes/s ÷ 166 entities = **4.1 keyframes per entity per second** (p95 5.3, max 9.4). Refresh alone gives ≤ 1/s moving, 0.33/s idle → ≈ 3 of 4 keyframes are threshold corrections (> 2 m or > 5°). Entity count includes idle stations/platforms/sites, so moving units are corrected more often than 4×/s. → A1–A4 confirmed from the host side; the "send only on error" saving is not happening.
4. **Steam send-rate cap = 262 144 B/s per connection** (`steam_send_rate_Bps`, constant). Peak out = 28% of it.
5. **Approach B cost with measured counts:** 166 × 14 B × 20 Hz = 46.5 KB/s mean, 63.6 KB/s at 227 entities; replaces today's 9.3 KB/s. Total ≈ 79 KB/s mean, ≈ 125 KB/s worst = 48% of the cap. Fits at 20 Hz; 10 Hz not needed for 1–2 clients.
6. **Reliable JSON = 72% of bytes.** `ReplicatedStateMessage` is flat ≈ 18 KB/s (3.5 KB × 4–5/s) regardless of fight size → a full snapshot resent 4×/s. No backlog on this link, so not today's symptom; first thing to shrink (delta or binary) before more clients.
7. **Host shot pools are fine.** Ships: 0 waits all match. Fighters: 2 events (`DualProjectile`, 4 shots waiting, max wait 766 ms, at 1752 s and 1947 s). → B1 ruled out on the host; client pools unknown.
8. Pool stock is oversized: `pool_total` 9 678–9 757, `pool_in_use` max 707 (7%). Memory/load cost only; revisit in [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]].
9. **≈ 50 ms hitch every ≈ 15 s** (131 windows at 50–60 ms; p50 8.6 ms). No match with `_ai.csv` row times. Cause unknown (GC candidate). One late network tick each; inside the 150 ms buffer.
10. `[ShipNavigation] Tangent route build failed … turns in place` × 160 in `Player.log` → those ships stop at corners and rotate on the spot (host-side, all machines see it).

### Phase 2 — Results 2 (client, 2 matches, 2026-11-22/23)

Source: `BattleTelemetry/Loading_20261122_223506_Client_*.csv` (C1), `Loading_20261122_235951_Client_*.csv` (C2), `Player.log`. Build `0.1.77` (same build as the host match), user = **client**, Steam relay, other machine hosts. 0 exceptions; disconnects only at match end.

| Measure | C1 (3835 s) | C2 (3765 s) |
|---|---|---|
| Entities mean / max | 224 / 305 | 186 / 318 |
| Squadrons mean / max | 64 / 90 | 58 / 112 |
| `rtt_ms` mean / max | 75 / 84 | 74 / 85 |
| Steam ping, quality | 54 ms, 1.0 | 54 ms, 1.0 |
| `lost_chunks_s` | 0 | 0 |
| `in_Bps` mean / max | 48 459 / 95 672 | 42 460 / 71 166 |
| Client FPS mean / min | 207 / 112 | 208 / 107 |
| Host FPS mean / min, tick | 166 / 98, 20.0 | 158 / 73, 19.9 |
| `keyframes_s` mean | 1 100 | 1 024 |
| `correction_mean_m` | 4.4 | 4.5 |
| `correction_heading_mean_deg` | 0.0 (max p50 5.2) | 0.0 (max p50 5.2) |
| `squadron_drift_mean_m` / max | **1 971 / 11 026** | **1 464 / 7 758** |
| Squadrons beyond loiter radius | 62.4 of 63.8 (**98%**) | 56.8 of 57.8 (**98%**) |
| Shots client / host | 348 807 / 373 374 = **93%** | 443 778 / 495 086 = **90%** |
| Shots ratio, first 10 min | **73%** | **78%** |
| Pool waits | 0 | 1 (`ProtonTorpedoShot`, 35 ms) |
| `reliable_lag_ms` mean / max | −18 / 18 | −12 / 83 |
| `timeline_delay_ms` first → last 10 min | 117 → 79 | 128 → 75 |
| Underruns (first 20 min / total) | 0 / 42 | 0 / 57 |

Findings:

1. **D1 confirmed, and it is the largest defect.** Client fighters are on average 1.5–2 km from the host's squadron position; 98% of squadrons are outside their loiter radius (18–20 m) for the whole match. Drift starts at spawn: 5 m at 4 s → 206 m at 14 s (≈ 40 m/s = squadron speed). Source: `Squadron.Tick` flies fighters locally; `ApplyReplicaOrder` replays only attack orders (`Squadron.cs:587`).
2. **The keyframe storm is squadrons, not ships.** Regression on C1: `keyframes_s ≈ 13.7 × squadrons + 1.3 × other entities`. Squadron roots are re-sent on 14 of 20 ticks; ships ≈ 1.3/s (refresh + rare corrections). → **Corrects host finding 3**: dead reckoning holds for ships; it fails for squadron roots.
   - Cause (source, likely): squadron root = fighter centroid, position only (`SquadronFlightComponent.QueuePoses`); its heading does not follow its velocity, the motion model is "speed along the nose" → 2 m error within 1–2 ticks.
   - `correction_mean_m` 4.4 m is dominated by these; heading corrections are ≈ 0.
3. **Client fires 7–10% fewer shots than the host; 22–27% fewer in the first 10 min.** Pool gate ruled out (0 waits, B1). Late targets ruled out (reliable lag ≤ 83 ms, no Steam backlog, B2). Left: client-local gates on wrong positions (B3 + D1) — fighters that are kilometres away cannot fire at the host's target.
4. **Render buffer erodes during the match.** `timeline_delay_ms` falls from ≈ 120 ms to ≈ 77 ms in 20 min, then underruns start (≈ 1.5/min). Mirror corrects the client timeline only after drift exceeds ±1 tick (`catchupNegativeThreshold`/`catchupPositiveThreshold`), so it rests on the low edge. Effective buffer ≈ 1.5 ticks, not 3.
5. **Link and reliable stream are fine at 75 ms RTT**: 0 lost chunks, quality 1.0, no pending bytes. Phase 3 rows "binary health, own channel" and "pool Option A" are not needed for these symptoms.
6. Large single corrections: `correction_max_m` > 50 m in 14–31% of seconds (max 801 m / 3 672 m), heading up to 179°. Candidates: hyperspace arrivals, reinforcement spawns, squadron launch. Not split by cause.
7. Client hitches: `frame_max_ms` > 40 in ≈ 5% of seconds on C1, 3% on C2.

Telemetry defects found:

- `timeline_delay_ms` keeps counting after the host stops ticking (−10 488 ms at C2 end) → filter rows with `host_tick_rate` 0.
- `chunk_loss_pct` is noise (1 s windows on two machines are not aligned): 0.7–1.7% while `lost_chunks_s` = 0. Use `lost_chunks_s`.
- No split of keyframes, corrections or shots by unit kind (squadron / ship) → finding 2 and 3 needed regression and inference.

### Why online felt worse after Phase 1 (user report 2026-11-23)

- **Phase 1 code changes nothing that is sent or applied** (diff `dff5d8082~1..36298a567`): counters, `TryPredict` (read-only), `NetworkDebugStatsMessage` 1 Hz, CSV row 1 Hz, read-only Steam status, pool counters compiled into release. `ClientReplicaBuffer.OnKeyframes` adds keyframes exactly as before.
- Cost not measurable: no pre-Phase-1 numbers exist. Nothing points at it: client FPS 207 mean / ≥ 107, 0 pool waits, no backlog.
- What differed from the 20:12 match:
  1. **Role.** 20:12 = host (sees none of the client defects). C1/C2 = client → D1, squadron corrections, 150 ms delay and buffer underruns are all visible.
  2. **RTT 75 ms** (20:12 host link: 17 ms).
  3. **Bigger fights**: up to 318 entities / 112 squadrons (20:12: 227 entities).
- Hypothesis, not verified: squadron commits `f720ba112` (2026-11-20, escort in paced groups) and `cc6bd2f23` (2026-11-21, leash back to post, tighter guard) added host-only squadron movement that clients do not replay → more drift than in the 2026-11-21 matches.
- Open: user to say what exactly got worse (fighters off position / units jumping / fire rate / FPS).

### Rotation drag (user report 2026-11-22, user was host)

**Answer 2026-11-22: seen on the host's own screen → ship movement, not networking.** The host draws its ships from the local simulation; no network smoothing runs there. The client branch below stays valid from source but is not what the user reported.

Host-side candidates (source only; no telemetry records yaw):

1. **Turn easing is long for small turns.** `StepYaw` ramps at `TurnAcceleration` and brakes on `sqrt(2·a·remaining)`. Max rate is reached only for turns > `RotationSpeed² / TurnAcceleration` = **30°** (15/7.5), 24° (12/6), 20° (10/5). Below that, turn time = `2·sqrt(angle / a)`: 20° at 7.5 °/s² = 3.3 s (1.3 s at full rate); 20° at 5 °/s² = 4.0 s (2.0 s).
2. **Stop-and-rotate.** `_isTurningToRoute` (`ShipMovementTweenPlayer.cs:256, 272, 320`) holds speed 0 until heading is within 0.01° of the route; set at route start, at corner pieces, and after heading recovery. 160 `Tangent route build failed … turns in place` lines in this match.
3. **Bank lags yaw.** `SmoothBank` = `SmoothDampAngle` with `BANK_SMOOTH_TIME = 0.6f` → hull roll settles ≈ 1.5–2 s after the yaw stops.
4. Last movement change: `8d1b515c2` (2026-11-22, "keep moving on speed-cap drops and blocked start turns") — committed 09:31, before the telemetry commits the 20:12 build clearly contains (`_links.csv` exists) → the drag is present with that fix in.

Decision needed: which of 1–3 the user means → own `Bugs` plan (ship movement), outside this note. Options: raise `TurnAcceleration` (balance), cap ease time instead of using a pure acceleration model, shorten `BANK_SMOOTH_TIME`, fix the tangent-route fallback.

- **Seen on the client's screen → network (source + finding 3).**
  - Keyframe carries one whole-°/s `TurnRate` taken from a 50 ms pose difference (`HostTickReplicator.TrySampleMotion` → `EntityMotion.FromSamples`); client holds it constant (`EntityMotion.Extrapolate`).
  - Host yaw is never constant-rate: `ShipRotationKinematics.StepYaw` ramps up at `TurnAcceleration` and brakes on `sqrt(2·a·remaining)`.
  - Turn start → client heading lags; turn end → client overshoots, then is pulled back.
  - Error after `t` s = ½·a·t². Capital ships (`TurnAcceleration` 1.5–7.5 °/s²) stay under 5° within the 1 s refresh → **no correction for a full second**, then up to 3.75° applied late. Fighters (60 °/s²) cross 5° every 0.41 s.
  - `ClientKeyframeTrack.Sample`: newest keyframe arrives ≈ 150 ms before its time → blend factor jumps from 0 to `(render − tA) / (tB − tA)` in one frame, then lerps (`Mathf.LerpAngle`) → heading moves in steps + slow catch-up = "dragging".
  - Fix = movement approach B (heading sent every tick, pure interpolation). No turn-rate guess left.
- **Seen on the host's own screen → ship movement, not networking.**
  - Every heading change eases in and out: ramp time = `RotationSpeed / TurnAcceleration` = **2.0 s** for the three main capital classes (15/7.5, 12/6, 10/5) and 3/1.5; brake phase is the same length again.
  - `_isTurningToRoute` / `_isRecoveringHeading` brake the ship to rest and hold it until heading is within 0.01° (`ShipMovementTweenPlayer.TickRoute`).
  - Finding 10 adds stop-and-rotate at corners.
  - Fix = balance (`TurnAcceleration` up → shorter ease) and/or route fallback; separate plan, not this one.

### Phase 3 — Fix by measurement

Order after Results 2 (2026-11-23), recommended:

1. **D1: anchor client fighters to the host squadron root** (fix direction in section D). Removes km-scale drift; expected to close most of the shot deficit.
2. **Squadron roots: send every tick, interpolate** (approach B for squadrons only). Already 14 of 20 ticks today → ≈ +30% keyframe bytes, no guessing.
3. **Render buffer**: tighten the catch-up thresholds or use 4 ticks; target 0 underruns over 60 min.
4. Re-measure shots client/host and ship corrections. Approach B for ships only if ship corrections are still visible.
5. Telemetry: per-kind split (see TODO).

### Phase 3 — Implemented 2026-11-23 (items 1–3, telemetry part)

User go: "execute plan" 2026-11-23 = recommended order. Editor compile clean; tests not run (local override); not played.

1. **D1 — client fighters ride the host root.** New `Entities/Squadron/SquadronReplicaAnchor.cs`, called from `Squadron.Tick` on replicas before `_pilot.Tick`, while the root still holds the host pose.
   - Root velocity = finite difference, smoothed 0.3 s. Speed > 25% cruise → `SquadronPilot.Follow(root, velocity dir, speed)` (wedge); else new `SquadronPilot.Orbit(root)` (moving centre, orbit not restarted).
   - Pull: planar drift > `LoiterRadius` → fighters shifted back, time constant 0.75 s, ≤ 50% cruise speed. Drift > 10 × `LoiterRadius` → shifted at once (launch, respawn, hyperspace) and pilot restarts from the root.
   - Replayed attack (`IsFighting`) keeps the local swarm; only the pull applies.
   - `Squadron.UpdateOrder` on replicas: no local target picks (no `DefendArea`/leash/hunt); only drops a replayed attack whose target died or cloaked.
   - New API: `ISquadronFlightComponent.FighterCentroid` + `Shift`, `SquadronFlightModel.Shift`, `FighterKinematics.Translate`. Registered in `SquadronInstaller`.
   - Why deadband = loiter radius: an orbiting group's centroid sits up to one radius off its centre; a smaller deadband fights the orbit.
2. **Squadron roots every tick, interpolated.** `HostTickReplicator.IsInterpolated` = entity has `ISquadronFighterSpreadFacade` → motion with zero speed/turn/climb, `DeadReckoningSender.TryCreateKeyframe(..., everyTick: true)`. Zero rates → `ClientKeyframeTrack.Sample` = pure lerp between ticks. `EntitySpawnMessage.IsInterpolated` tells the client.
3. **Render buffer.** `ClientReplicaBuffer` passes `NetworkTickSettings.SLOWDOWN_THRESHOLD_TICKS = -0.25` / `CATCHUP_THRESHOLD_TICKS = 0.5` to Mirror (was ±1). Buffer stays 3 ticks (150 ms).
4. **Telemetry.** Client `corrections_*` now exclude interpolated roots (= ships, stations, other units); new `squadron_keyframes_s`; `timeline_delay_ms` = NaN once `host_tick_rate` ≤ 0; `chunk_loss_pct` removed.
- Tests (not run): new `SquadronReplicaAnchorTests` (`Squadrons`: snap + orbit, moving root followed, gradual pull); `SquadronTargetSelectorTests` (`FlightStub` gains `FighterCentroid`/`Shift`, icon test sets `_replicaAnchor`, new `ReplicaSquadron_DoesNotPickItsOwnTargets`); `NetworkTickReplicationTests` (`Networking`: squadron roots every tick, zero rates, no corrections); `NetworkDiagnosticsTests` (`squadron_keyframes_s` column). Reviewed, no change: `FighterFlightTests`, `SquadronOrderTimingTests`, `ReconnectOrderBaselineTests`.
- Expected on re-measure: `squadron_drift_mean_m` < `LoiterRadius`, `squadrons_beyond_loiter` ≈ 0; `squadron_keyframes_s` ≈ 20 × squadrons; ship `corrections_s` small; `timeline_delay_ms` ≈ 120–150 ms all match; underruns ≈ 0.
- Not done: host-side keyframe split by kind/reason, shots by kind, large-correction cause logging.

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
- [x] Phase 2 host half: 2v2 (1 client) Steam match, 3037 s, recorded 2026-11-22.
- [x] Phase 2 client half: two client matches recorded 2026-11-23 (`Results 2`). D1 confirmed; pool gate and late targets ruled out.
- [ ] User: what exactly got worse after Phase 1 (fighters off position / units jumping / fire rate / FPS)?
- [x] User: approve fix order in Phase 3 (D1 → squadron roots every tick → render buffer) — "execute plan", 2026-11-23.
- [x] Phase 3.1 D1: client fighters anchored to the host root (`SquadronReplicaAnchor`), no local target picks on replicas — 2026-11-23, compile clean, tests not run.
- [x] Phase 3.2: squadron roots sent every tick without rates, clients interpolate — 2026-11-23.
- [x] Phase 3.3: Mirror catch-up thresholds −0.25 / +0.5 tick — 2026-11-23.
- [ ] Phase 3.4 (user): online match as client on a build with these changes; send `_net.csv` + `Player.log`. Check drift, `squadron_keyframes_s`, ship corrections, timeline delay, underruns, shots client/host.
- [x] Telemetry: client `corrections_*` exclude squadron roots, `squadron_keyframes_s`, `timeline_delay_ms` NaN when the host stops ticking, `chunk_loss_pct` dropped — 2026-11-23.
- [ ] Telemetry: `shots_s` by squadron / ship.
- [ ] Large corrections (> 50 m in 14–31% of seconds): log cause (hyperspace arrival, spawn, launch).
- [x] User: rotation drag seen on the **host** screen (2026-11-22) → ship movement, not networking.
- [ ] Rotation drag on host: user picks the symptom (slow small turns / stop-and-rotate / roll settling) → separate ship-movement `Bugs` plan.
- [ ] Phase 2 rest: 3v3 with 3 factions + one KCP client (optional once the client CSV confirms A).
- [ ] Telemetry gap: host CSV cannot split keyframes by unit kind or reason → add `keyframes_ships_s`, `keyframes_squadrons_s`, `keyframes_drift_pos_s`, `keyframes_drift_heading_s`, `keyframes_refresh_s`.
- [ ] `ReplicatedStateMessage` 3.5 KB × 4–5/s: find the sender, send deltas or binary.
- [ ] 50 ms hitch every ≈ 15 s on the host: identify (GC vs periodic job).
- [x] Mirror `sendRate`: not set on `OnlineNetworkManager` → `NetworkManager` default 60 Hz (source). CSV header records the runtime value.
- [x] Steam send rate: `steam_send_rate_Bps` = 262 144 B/s per connection (Phase 2 host CSV).
- [x] Phase 1 review gaps closed (D1 drift columns, pools/links CSV, per-window max wait, guarded unique files, tests) — 2026-11-22, compile clean, tests not run.
- [x] D1 measured 2026-11-23: 98% of squadrons beyond loiter radius, mean drift 1.5–2 km → confirmed. Fix implemented 2026-11-23 (Phase 3 — Implemented).
- [ ] D2/D4 checks.
- [ ] Phase 3: user picks movement approach (B recommended) and weapon fix from the table.
