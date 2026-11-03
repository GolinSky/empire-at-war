---
category: Features
status: todo
created: 2026-11-02
tags:
  - multiplayer
  - networking
---
# Multiplayer Plan (Mirror + Steam relay)

## Goal

- Add online play next to the existing offline game.
  - Skirmish: PvP and co-op, 2–4 humans, AI slots allowed.
  - Survival: co-op humans vs waves.
- Offline Skirmish, Survival and Battle Simulation keep working with no network code on their path. Battle Simulation stays offline-only.
- Audience: 2–4 friends (user, 2026-11-02); no public matchmaking.
- Evidence, file lists and rejected options → [[TODOs/Features/Multiplayer_Plan - Research|Multiplayer Plan - Research]].

## Decision

- **v1 scope (user-confirmed 2026-11-02):** Skirmish + Survival co-op; PvP and co-op; AI slots allowed online.
- **Stack (user-confirmed 2026-11-02):**
  - Netcode: **Mirror**.
  - Relay: **`FizzySteamworks`** (Steamworks.NET; Steam Datagram Relay + NAT traversal). **App ID 480** while developing; own App ID (Steam Direct) if the game ships.
  - Dev testing: **`KcpTransport`** over LAN or Tailscale (direct IP), with no Steam needed.
  - Lobby: **Steam lobbies** behind `IOnlineLobby`.
  - Why: Steam relay is free with no quota, so a public repo can't drain a budget. Friends already have Steam. FizzySteamworks is the established Mirror Steam transport. Mirror has built-in `NetworkTime`, `SnapshotInterpolation` and struct messages, which fit our custom-message design.
  - Avoid: shipping under App ID 480 (test app only); NGO + UGS Relay (rejected → Research).
- **Model: host-authoritative listen server with a fixed 20 Hz network tick.** The host runs the current simulation (incl. all AI) unchanged on `Update`. Clients send commands and render replicated, tick-stamped state.
  - Why: the sim is not deterministic: `Time.deltaTime`/`Time.time` in 67 files, `UnityEngine.Random` in 21 files (incl. `WeaponComponent`), Guid ids.
  - Avoid: deterministic lockstep. It needs a fixed-point, fixed-step rewrite of the whole sim.
- **Offline = authority with no transport.** `OfflineSession` (`IsAuthority = true`, commands run immediately). Offline paths never touch Mirror or Steam types.
- **Replication: Mirror `NetworkMessage` structs keyed by entity net index.** Not one `NetworkBehaviour`/`NetworkIdentity`/`SyncVar` per ship or component.
  - Send: `NetworkServer.SendToAll` / `NetworkConnection.Send`, `NetworkClient.Send`; `Channels.Unreliable` for movement/health deltas, `Channels.Reliable` for events and commands.
  - Why: entities are pooled, loaded through Addressables and built by per-entity VContainer scopes. `EntityLocator` already maps `long` id → entity. Restore code already spawns entities with a known id and pose.
  - Unreliable messages stay under the transport MTU → chunk deltas (≈ 1 KB).
- **Mirror owns** connections, transports, batching, `NetworkTime`. **The game owns** the tick, commands, snapshots, events, spawns and interpolation (using Mirror `SnapshotInterpolation` helpers).
- **No Mirror scene management:** `NetworkManager.onlineScene`/`offlineScene` stay empty. Battles load through the existing Addressables/loading pipeline.

## Network Tick Model

- **Host tick: fixed 20 Hz (50 ms)** from our own accumulator in `NetworkTickService` (host only); numbered `uint Tick`; timestamps from `NetworkTime.time`. The sim stays on variable-dt `Update`.
- Each host tick:
  1. Apply queued client `PlayerCommand`s in arrival order (validated: issuer owns receivers, can afford, cooldown ready).
  2. Collect changes since the last tick → `TickMessage { Tick, Time, spawns, despawns, deltas, events }`.
  3. Send unreliable deltas + reliable events.
- What and how often per data type → Sync Inventory. Movement = dead-reckoning keyframes, not per-tick positions. Weapons = target assignments, not per-shot events.
- Events (reliable, tick-stamped): `Spawn`, `Despawn/Death`, `WeaponTarget`, `MissileLaunched`, `AbilityStarted`, `SuperWeaponFired`, `CaptureChanged`, `MatchState` (pause, speed, wave, victory).
- Per-owner reliable state: credits, production queue, research, cooldowns, unit cap.
- Client: buffer tick messages and render at `NetworkTime.time - INTERPOLATION_DELAY` with `SnapshotInterpolation`. Play events when render time reaches their tick.
- Commands: client → host reliable `CommandMessage { LastReceivedTick, PlayerCommand }`. Applied on the next host tick. The client shows order feedback right away (`UnitOrderFeedback`); units move once server state arrives (≈ RTT + delay).
- Pause and speed: host-only. `MatchState` carries them; clients set `Time.timeScale` to match (audio depends on it).

## Steam Specifics

- Packages: Mirror (Asset Store / GitHub release), `FizzySteamworks`, Steamworks.NET. Placement → `PROJECT_ORGANIZATION` (`Assets/Plugins`). Pin versions; check FizzySteamworks supports the pinned Mirror.
- Dev: `steam_appid.txt` containing `480` beside the build exe and the project root (Editor). Steam client must be running and signed in. Players show as "Spacewar".
- Lobby: `SteamMatchmaking.CreateLobby` as `FriendsOnly` or `Private`; set lobby data `game = empire-at-war` + a version string and filter on it (App 480's lobby list is shared with other developers).
- Join: lobby data stores the host SteamID → FizzySteamworks `networkAddress = hostSteamId` → `StartClient()`. Overlay invites → `GameLobbyJoinRequested_t`.
- Transport choice at startup: `IMatchTransportConfig` picks `FizzySteamworks` (Steam) or `KcpTransport` (direct IP), so all game code is shared.
- Ship: replace 480 with own App ID; can stay unreleased and use keys or Steam Playtest for friends.

## Readiness: What Already Helps

- `PlayerId` (slot index) ownership on every `IEntity`; `IPlayerRelations`/`TeamId` relations.
- `ILocalPlayer` separates "who sits at this machine" from ownership. Each machine composes entity presentation (fog, audio dialog) from its own viewpoint.
- Per-player DI scopes: `PlayerScopeBootstrap` → `LocalPlayerScope` / `AiPlayerScope`. AI slots run on the host; a disconnected human can be taken over by AI.
- Central order path: `IUnitOrderService` used by player input and AI.
- Map from seed (`MapSpec.Seed`). `SkirmishSetupSave` is a serializable match config → each client generates the same map.
- Restore path: `ShipFactory.Create(owner, type, pos, entityId, ShipSpawnPose)` plus other factories → client spawn path.
- Save sources (`IBattleSaveSource`, `UnitSave`, `PlayerSave`) list the state that needs replicating.
- Physics: no `Rigidbody`, no `NavMeshAgent`, 1 `Physics` query file. No mutable static state.
- `LifecycleTickGate`, `OrderedLifecycleCoordinator` → central gate for sim ticks on clients.

## Issues To Fix

| # | Issue | Where | Change |
|---|---|---|---|
| 1 | Exactly one human enforced | `MatchRules.FindHuman`, `MatchRules.Validate`, `LocalPlayer` ctor | Local slot comes from the session's `LocalPlayerId`; online allows N humans. |
| 2 | `PlayerController` is only `Human`/`Ai` | `PlayerController`, `PlayerScopeBootstrap` | Add a remote human; its scope gets sim services only. |
| 3 | Player scope mixes sim and local UI | `LocalPlayerScope.RegisterPlayerServices` (`ReinforcementUiController`, `HeroUiController`, `FactionAnnouncementService`) | Split into `HumanPlayerScope` (sim) and local-only UI registrations. |
| 4 | Player actions call models directly, holding object refs | `UnitOrder` (holds `IEntity`), `FactionService.QueueUnit`, `CaptureSitesSystem.TryStartConstruction`, `ShipAbilityService.Press/SubmitTarget/TryActivate`, `SuperWeaponPresenter` → `SuperWeaponFireService.Fire`, reinforcement placement, research, station upgrade | `IPlayerCommandSink` + serializable `PlayerCommand`s (issuer `PlayerId`, entity ids, points); validate on authority. |
| 5 | No issuer validation | `UnitOrderService` | Authority drops receivers not owned by the issuer. |
| 6 | Random Guid-derived ids made on the spawning machine | `UniqueIdGenerator` + 7 factories | Authority-only sequential id allocator; clients spawn only with server ids. |
| 7 | Sim and visuals in the same components | `WeaponComponent` (944 lines), `WeaponHardPoint`, movement, squadrons, health | Client mode: no targeting/damage/AI ticks. Visuals come from `WeaponTarget`/launch events; HP and death from snapshots. |
| 8 | Global pause/speed | `SkirmishOrchestrator.ApplyTimeScale` | Host-only; clients follow `MatchState`. |
| 9 | Sim decisions use the local player | `CaptureSitesSystem`, `ReinforcementZonesSystem`, `BattleVictoryService` | Authority decides per `PlayerId`; local player only for presentation and the per-viewer result. |
| 10 | Fog is local presentation only | `FogOfWarSystem`, `VisionService.IsVisible(PlayerId, pos)` | v1: send all state (trusted friends). Later: per-team interest filter on the host. |
| 11 | Saves assume one machine | `BattleSaveService`, `BattleAutosave`, `SurvivalAutosave` | v1: disable save/load online. |
| 12 | No session lifecycle | — | Steam lobby, ready barrier before `BattleState.Running`, disconnect → AI takeover. |
| 13 | One human assumed in battle data | `BattleDefinition.Human` (`Players.Single(Role == Human)`), `GetProductionHost` (AI ally hosted on "the" human), `SkirmishPreset` | `Humans` list; explicit production host per AI ally. |
| 14 | Survival has one human slot | `SurvivalPreset.Create` (player 0 human, optional `AiAlly`, `WaveAttacker`), `SurvivalSettings`, Survival map (`MapType.Survival`) | Co-op slots (human or AI ally); station spawn for 2+ humans; check that `SurvivalWaveDirector` and `SurvivalModel` scale per player. |

## Implementation

### Phase 0 — Spike
- [x] v1 scope: Skirmish + Survival co-op, PvP + co-op, AI slots (2026-11-02).
- [x] Stack: Mirror + FizzySteamworks (App ID 480 dev) + KcpTransport dev + Steam lobbies (2026-11-02).
- [ ] In `Assets/Sandbox`: Mirror host/client over `KcpTransport` by direct IP (LAN, then Tailscale).
- [ ] Switch the same scene to FizzySteamworks: Steam lobby create → invite → join across two networks.
- [ ] Prove `CommandMessage` → host → 20 Hz `TickMessage` round-trip.
- [ ] Measure with 50–100 active ships per side: bytes/s per client and host upload against Important Values; check dead-reckoning error on capital ships vs squadrons.

### Phase 1 — Offline-safe seams (no Mirror/Steam dependency; offline unchanged)
- [ ] `IMatchSession` (`IsOnline`, `IsAuthority`, `LocalPlayerId`) + `OfflineSession`; registered in `ProjectScope`/`SkirmishScope`.
- [ ] Issues #1, #2, #3, #13: N-human rules, remote-human controller, player scope split, `BattleDefinition.Humans`.
- [ ] Issues #4, #5: `PlayerCommand` + `IPlayerCommandSink`. UI/input go through it; AI keeps calling services directly on the authority.
- [ ] Issue #6: authority id allocator (keep the `Create(..., entityId, pose)` overloads).
- [ ] Issue #9: authority per-`PlayerId` APIs for capture, zones and victory.
- [ ] Issue #14: Survival co-op slots in `SurvivalPreset`/`SurvivalSettings` (works offline with an AI ally first).

### Phase 2 — Session + lobby
- [ ] Add Mirror, FizzySteamworks, Steamworks.NET. `NetworkManager` (empty online/offline scenes) lives in a persistent root owned by `ProjectScope`, outside battle scopes; it is not created in offline sessions.
- [ ] `IOnlineLobby` over Steam lobbies: create (friends-only/private), invite, join, slot properties (faction, team, color, AI, ready), host kick. `KcpTransport` path: host IP entry for dev.
- [ ] Host sends `SkirmishSetupSave` (or a Survival setup) → clients run the existing loading pipeline → ready barrier → host starts.

### Phase 3 — Tick replication
- [ ] Host `NetworkTickService` (20 Hz accumulator, command queue, change collection, MTU chunking).
- [ ] Messages: `CommandMessage`, `TickMessage` (spawns, despawns, deltas, events), `PlayerStateMessage` (owner only), `MatchStateMessage`; custom `NetworkWriter`/`NetworkReader` extensions for quantized values.
- [ ] Client `SnapshotBuffer` + `SnapshotInterpolation` (`INTERPOLATION_DELAY`).

### Phase 4 — Client mode per system (issue #7)
- [ ] Gate authority-only work: AI, economy, production advance, capture, weapon targeting/damage, ability effects, super-weapon damage, `SurvivalWaveDirector`.
- [ ] Order: movement → weapons/bolts → health/hardpoints/death (explosions and wrecks stay client cosmetic) → squadrons/hangars → abilities → super weapons → capture/economy/reinforcement → Survival HUD/waves.

### Phase 5 — Robustness
- [ ] Disconnect → `AiPlayerScope` takeover; host leaves → match ends.
- [ ] Issues #8, #10, #11 v1 rules; online menu entries; error UI (Steam not running, lobby/connection failures, version mismatch).

## Security (public repo)

- Steam relay has no quota or billing → nobody can drain a budget by building the repo. App ID 480 is public by design.
- Lobbies friends-only/private + version filter → strangers can't join your matches.
- Gameplay: the host validates every command (issue #5); clients can't control others' units. Fog maphack is possible in v1 (issue #10).
- Dev over Tailscale: private, invite-only tailnet → no public endpoint.

## Sync Inventory

- Rule: send **state changes and intents, not per-frame state**. Anything the client can derive or fake stays local.

| Data | Direction / delivery | Rate | Est. size |
|---|---|---|---|
| Spawn (net index, type, owner, pose, HP) / despawn+death | host→all, reliable | on event | 12–20 B |
| Movement keyframe (pos x/z 16-bit, heading, speed, turn rate) | host→all, unreliable | on velocity/heading change + 1 Hz refresh while moving; clients dead-reckon | 10 B |
| Squadron center + heading | host→all, unreliable | 4 Hz while moving | 8 B |
| Hull/shield (byte fractions), dirty hardpoints, squadron member alive mask | host→all | on change, coalesced ≤ 4 Hz | 3–8 B |
| Weapon target per hardpoint (target id + hardpoint) | host→all, reliable | on target change; clients fire bolts at the known rate | 5 B |
| Missile/torpedo launch, ability start/end, super weapon fire, hangar launch | host→all, reliable | on event | 6–12 B |
| Capture site/zone owner + progress | host→all | on change, ≤ 2 Hz | 3 B |
| Credits, production queue, research, station level, population, cooldowns | host→owner only | on change; credits ≤ 1 Hz | small |
| Match state (pause, speed, wave, victory) | host→all, reliable | on change | small |
| Player commands | client→host, reliable | on input | 10–50 B |

- Not synced (client-local): bolts/lasers, impacts, explosions, wreck debris (`UnitWreckService`), individual fighter maneuvers (`FighterManeuver`), audio, camera, selection, hover/tooltips, minimap, music.
- Fog rendering: computed locally from replicated positions + vision ranges (`VisionService`); v1 sends all units.
- Map, hazards and planets: generated from `MapSpec.Seed`; zero bytes.
- Net index: map `long` entity id → `ushort` index at spawn; id is sent once.

## Important Values

- `NETWORK_TICK_RATE = 20` Hz (50 ms) = batching/command clock, not a send-every-tick rate. Per-data rates are in Sync Inventory.
- `INTERPOLATION_DELAY` ≈ 150–250 ms (dead reckoning + keyframes). Tune in the spike.
- Scale: `MaxUnitCapacity = 120` per player; unit capacity 1–25 each → ≈ 30–80 units per player, 100–250 per 4-player match (estimate; measure).
- Bandwidth is a quality limit, not a cost (Steam relay is free): per client average ≤ 8 KiB/s, peak ≤ 32 KiB/s; host upload ≤ 3 × peak ≈ 100 KiB/s. A naive 27 KiB/s (all units, positions at 20 Hz) is acceptable for the first playable; optimize toward Sync Inventory after measuring.

## Edge Cases

- Host migration: not in v1 (host leaves → match ends).
- Late join and reconnect: not in v1; baselines + spawn events make them possible later.
- Survival wave AI and every AI slot run only on the host.
- Cosmetic randomness (`FighterManeuver`, `ImpactEffectView`, `UnitWreckService`) stays client-local.
- Fighters: replicate the squadron entity only; individual fighter maneuvers are client cosmetic.
- Steam not running → Steam online options are disabled with a message; direct-IP (KCP) stays available.
- Build version mismatch → lobby version filter + a handshake check on connect (Mirror `NetworkAuthenticator`).

## Verification

- EditMode: command serialization round-trip, issuer validation, N-human `MatchRules`, Survival co-op presets, offline parity, tick message delta encoding. Categories: `GameFlow`, `BattleInput`, `Core` + touched systems.
- User-only: two machines over Steam (App 480) and over Tailscale/KCP; Multiplayer Play Mode or two builds for local iteration.

## TODO / Open Questions

- Max humans in Survival co-op (2? up to 3 with the wave attacker slot)?
- Fog cheating: acceptable for friends-only v1?
- Own Steam App ID: decide before any distribution beyond development testing.
