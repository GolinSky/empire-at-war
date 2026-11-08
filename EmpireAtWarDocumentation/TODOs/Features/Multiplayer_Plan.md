---
category: Features
status: in-progress
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
  - Why: the sim is not deterministic: `Time.deltaTime`/`Time.time` in 69 files, `UnityEngine.Random` in 20 files (incl. `WeaponComponent`), Guid ids (re-counted 2026-11-07).
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

## Lobby UI and Join Flow

### Menu
```
Play
 ├─ Skirmish        (offline, unchanged)
 ├─ Survival        (offline, unchanged)
 ├─ Online ──► Host Skirmish | Host Survival | Join by IP (dev, KCP only)
 ├─ Simulation
 └─ Load
```
- No server browser. Friends join through Steam (table below).
- Steam not running → Host buttons disabled with a message; Join by IP stays.

### Host Lobby Screen
- Reuse the Skirmish setup screen (`SkirmishUi`, `SkirmishSlotRowView`, `SkirmishModel`); Survival lobby reuses `SurvivalUi` + co-op slots.
- `SkirmishSlotOccupant` gains `Open` (invite) and `RemoteHuman`. A joined row shows Steam name + avatar.
- Row edit rights: a human edits only their own faction/team/color; the host edits AI rows, map, money, victory, super weapons. Others see them read-only.
- Per-human **ready** check. **Start** enabled only when all humans are ready (`SkirmishModel.CanStart` + ready).
- Host: **Invite friends** button, **kick** per remote row. All: **Leave**.
- Lobby state lives in Steam lobby data/member data; `SkirmishModel` renders it through `IOnlineLobby`.

### How Friends Join (Steam)
| Path | Steam API | Build |
|---|---|---|
| Host invites via overlay | `SteamFriends.ActivateGameOverlayInviteDialog(lobbyId)` | Invite button |
| Friend list → "Join Game" | Joinable lobby / rich presence `connect` | — |
| Game not running → started by invite | Launch arg `+connect_lobby <lobbyId>` | Parse at startup → go to lobby |
| Game running → invite accepted | `GameLobbyJoinRequested_t`, `GameRichPresenceJoinRequested_t` | Callback → `JoinLobby` → `StartClient` |
| Optional: in-game Friends panel | Friend list + `GetFriendGamePlayed` → `m_steamIDLobby` | Small list with Join |
| Optional: friends' lobby list | `RequestLobbyList` + `game = empire-at-war` filter | Only if browsing is wanted |
- Rich presence: `connect` + status ("In lobby 2/4", "Survival — wave 7").
- Errors: Steam not running, lobby full/closed, version mismatch, host left, kicked.

### App ID 480 Limits
- Overlay invite and join into a **running** game work.
- Shows as "Spacewar"; "Join Game" with the game closed launches Spacewar → friends start the game first.
- Localized rich-presence text needs own App ID (tokens uploaded on Steamworks); `connect` key works under 480.
- Avoid: Remote Play Together — needs own App ID and local multiplayer, which the game lacks.

## Online Saving (post-v1)

- v1: save/load disabled online (issue #11).
- **Save (small):** host only. `BattleSaveService.Capture` already covers units, structures, capture sites, zones, super weapons, Survival; `PlayerSave` is per `PlayerId`.
  - Add slot → Steam ID map to the save header.
  - Camera pose: host-only, not applied to clients.
  - Autosave: host only; clients never autosave.
- **Load (hard):** host restores via `BattleRestorer` → "Resume" lobby reserves slots for the saved Steam IDs (empty slot → AI) → clients join → need a **full-state baseline on join**.
- Baseline on join = same mechanism as reconnect/late join → build once, use for both.
- Prereqs: issue #13 (`Humans` list) and `HumanProductionSaveSource` handling 2+ humans.

| Piece | Effort |
|---|---|
| Host save + slot → Steam ID map | small |
| Resume lobby (reserved slots, AI fill) | medium |
| Full-state baseline on join (shared with reconnect) | medium–large |
| Multi-human production save sources | small after #13 |

## Readiness: What Already Helps

- `PlayerId` (slot index) ownership on every `IEntity`; `IPlayerRelations`/`TeamId` relations.
- `ILocalPlayer` separates "who sits at this machine" from ownership. Each machine composes entity presentation (fog, audio dialog) from its own viewpoint.
- Per-player DI scopes: `PlayerScopeBootstrap` → `LocalPlayerScope` / `AiPlayerScope`. AI slots run on the host; a disconnected human can be taken over by AI.
- Central order path: `IUnitOrderService` used by player input and AI.
- Map from seed (`MapSpec.Seed`). `SkirmishSetupSave` is a serializable match config → each client generates the same map.
- Restore path: `ShipFactory.Create(owner, type, pos, entityId, ShipSpawnPose)` plus other factories → client spawn path. `SquadronFactory` restore overload takes `Quaternion rotation`, not `ShipSpawnPose`.
- Save sources (`IBattleSaveSource`, `UnitSave`, `PlayerSave`) list the state that needs replicating.
- Physics: no `Rigidbody`, no `NavMeshAgent`, 2 `Physics` query files (`CameraService`, `StructureSpawnClearance`). No mutable static state.
- `LifecycleTickGate`, `OrderedLifecycleCoordinator` → central gate for sim ticks on clients.

## Issues To Fix

| # | Issue | Where | Change |
|---|---|---|---|
| 1 | Exactly one human enforced | `MatchRules.FindHuman`, `MatchRules.Validate`, `LocalPlayer` ctor | Local slot comes from the session's `LocalPlayerId`; online allows N humans. |
| 2 | `PlayerController` is only `Human`/`Ai` | `PlayerController`, `PlayerScopeBootstrap` | Add a remote human; its scope gets sim services only. |
| 3 | Player scope mixes sim and local UI | `LocalPlayerScope.RegisterPlayerServices` (`ReinforcementUiController`, `HeroUiController`, `FactionAnnouncementService`) | Split into `HumanPlayerScope` (sim) and local-only UI registrations. |
| 4 | Player actions call models directly, holding object refs | `UnitOrder` (holds `IEntity`), `FactionService.QueueUnit`, `CaptureSitesSystem.TryStartConstruction`, `ShipAbilityService.Press/SubmitTarget/TryActivate`, `SuperWeaponPresenter` → `SuperWeaponFireService.Fire`, reinforcement placement, research, station upgrade | `IPlayerCommandSink` + serializable `PlayerCommand`s (issuer `PlayerId`, entity ids, points); validate on authority. |
| 5 | No issuer validation | `UnitOrderService` | Authority drops receivers not owned by the issuer. |
| 6 | Random Guid-derived ids made on the spawning machine | `UniqueIdGenerator` + 6 factories/builders | Authority-only sequential id allocator; clients spawn only with server ids. |
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
- [x] Packages (2026-11-07): Mirror `96.11.3` + FizzySteamworks `6.0.1` extracted to `Assets/Plugins/Mirror` (skipped `Examples`, `Hosting`, `Transports/Edgegap`); Steamworks.NET `2025.164.1` as UPM git dep. Mirror added define symbols `MIRROR*`, `STEAMWORKS_NET` (Standalone). `steam_appid.txt` = `480` at project root.
- [x] Spike code (2026-11-07, compiles, weaver OK; **not run**): `Assets/Sandbox/Multiplayer/MultiplayerSpike.unity`, own asmdef `Sandbox.Multiplayer` (not auto-referenced). See Spike below.
- [ ] User: KCP host/join by direct IP (LAN, then Tailscale).
- [ ] User: Steam host lobby → overlay invite → join across two networks.
- [ ] User: command → host → 20 Hz tick round-trip (panel shows ack ms + visible ms).
- [ ] User: measure 50 and 100 units/side, both sync modes: per-client KiB/s, host upload, DR error capital vs squadron → record in Important Values.

### Spike (Phase 0)
- Run: open scene → Play (or build; copy `steam_appid.txt` next to the exe). OnGUI panel, right-click map = move own side.
- `MultiplayerSpike`: KCP Host / Join IP (port 7777); Steam Host lobby (FriendsOnly, lobby data `game`/`version`/`host`) + Invite; `+connect_lobby <id>` launch arg; `GameLobbyJoinRequested_t` → join → `networkAddress = host SteamID`.
- `SpikeHostSimulation`: 2 sides × N units (20% capital 8 m/s 15°/s, rest squadron 40 m/s 120°/s), variable-dt `Update`, 20 Hz accumulator → apply commands (issuer side check) → collect → chunk ≤ 1000 B → `SendToAll` unreliable. ≥ 1 chunk per tick (timeline heartbeat).
- Sync modes: `FullSnapshot` (every unit every tick, client lerps) vs `DeadReckoning` (keyframe when host-predicted error > threshold m, heading > 5°, or 1 s moving / 3 s idle refresh; client extrapolates arc).
- `SpikeUnitState` 10 B: pos 0.125 m (±4096 m), heading 16-bit, speed 0.25 m/s, turn 1°/s.
- Client: `SpikeClientReplica` = Mirror `SnapshotInterpolation.InsertAndAdjust`/`Step` for the timeline (buffer = 3 × 50 ms), per-unit keyframe ring `SpikeUnitTrack`.
- Stats: `SpikeBandwidthMeter` (Mirror `NetworkDiagnostics`, message bytes, no transport headers; per-connection `Send` from host not counted), `SpikeDeadReckoningStats` (host-side mean/max error per kind).
- Join sends a welcome (side + roster) and forces a full keyframe resend; 3rd+ connection = observer.

### Phase 1 — Offline-safe seams (no Mirror/Steam dependency; offline unchanged)
- [x] `IMatchSession` (`IsOnline`, `IsAuthority`, `LocalPlayerId`) + `OfflineSession(PlayerId)` in `Entities/Players` (2026-11-07). Registered per battle mode (roster lives in the battle scope, not `ProjectScope`): Skirmish/Survival → `OfflineSession.ForHuman(roster)` (throws unless exactly one human = offline one-human rule); Simulation → `OfflineSession(PlayerId.None)`.
- [x] Issues #1, #2, #3, #13 (2026-11-07; compile clean, rules checked via Editor eval, tests not run):
  - #1: `MatchRules.Validate` needs ≥ 1 human (was exactly 1). Exactly-one is now enforced only by `OfflineSession.ForHuman` / `MatchRules.FindHuman`. `LocalPlayer(IPlayerRoster, IMatchSession)` takes its slot from `LocalPlayerId`.
  - #13: `BattleDefinition.Humans` replaces `Human`. `BattlePlayer.ProductionHostId` (AiAlly → given host, else self); `GetProductionHost` uses it; `SurvivalPreset` hosts the ally on player 0. `MatchRules`: AiAlly host must be a human on the ally's team. Survival callers (`SurvivalOutcomeRule`, `SurvivalLayoutGenerator`, `SurvivalWaveDirector`) use `Humans.Single()` until #14.
  - #2: no new `PlayerController` value. Remote = `Human` slot whose id ≠ `IMatchSession.LocalPlayerId` (per-machine, not serialized). `PlayerScopeBootstrap` passes `isLocal`.
  - #3: `LocalPlayerScope` → `HumanPlayerScope`. Sim for every human: `RegisterPlayerServices` + `IBattleModule.ConfigurePlayer`. Local only: `RegisterLocalPlayerServices` (`ReinforcementUiController`, `HeroUiController`, `FactionAnnouncementService`) + new `IBattleModule.ConfigureLocalPlayer` (Economy/Faction/ShipBuild UI, cheats, `CaptureSiteAnnouncer`, `SuperWeaponPresenter`).
  - Offline change: local registrations now come after sim registrations → order-0 lifecycle callbacks of those UI types run after the player's sim services (stable `OrderBy`). User smoke test: Skirmish + Survival start, build UI, reinforcements, heroes, super weapon, cheats.
  - Still coupled (→ #4): `ReinforcementService` (pointer/camera/input lock, placement previews, `ReinforcementDeployInput`) and `FactionService` (`ISelectionService`) stay in the sim set; a remote human's scope would react to local input until commands exist.
  - Still open: `MusicService.FindHuman` (project scope; needs the session at project level → Phase 2).
- [x] Issues #4, #5: `PlayerCommand` + `IPlayerCommandSink`. UI/input go through it; AI keeps calling services directly on the authority. (Completed 2026-11-07, incl. abilities + reinforcement deploy below.)
  - Abilities: `ShipAbilityCommand` (caster ids, target id, cancel). `ShipAbilityService` keeps the local targeting UI; `Press`/`SubmitTarget` decide accept/reject with read-only checks (`CanCancel`, `CanActivateNow`), then request execution. `[Inject]` ctor → request = command from `ILocalPlayer.Id`, service self-registers as `IShipAbilityCommandHandler`; 2-arg ctor (tests, standalone) → executes at once. Authority `Execute` = cancel / activate / cast-on-arrival. AI and `PlayerAutoPowerService` stay direct (authority automation).
  - Reinforcements: `DeployReinforcementCommand` (kind + key + slot positions). Previews/placement stay local in `ReinforcementService`; drop sends slots whose preview `CanSpawn`. Authority `Deploy` finds the request in the reserve, re-creates the placement, re-checks reserve count, `IsPositionValid`, capacity; raises `ReinforcementDeployed`/`CapacityRejected` and `InvokeSpawnShipEvent`. Offline order change: deploy now runs before preview teardown (same as before) but the spawn event fires before the lock is released.
- [x] Issue #6 (2026-11-07): `UniqueIdGenerator` moved to battle scope (`SkirmishScope`), sequential, throws unless `IMatchSession.IsAuthority`; every `EntityAdded` (incl. restored) advances the counter past its id. Known risk: a save made with sequential ids that spawns a new entity before all restored entities are registered would collide (duplicate key fails fast in `EntityLocator.AddEntity`).
- [x] Issue #9 (2026-11-07): capture/zone sim already per `PlayerId` (construction via issuer command; `_localPlayer` uses there are vision/presentation only). `BattleVictoryModel` split: `EvaluateTeams` (authority, viewer-independent, `SurvivingTeams`) + `OutcomeFor(team)` (per viewer); `Evaluate` = both, so offline unchanged. `BattleVictoryService.CreateResult` stays per viewer.
- [x] Issue #14 (2026-11-07): `SurvivalSettings.CoopHumanFactions` (new ctor overload; old ctor = empty). `SurvivalPreset`: player 0, co-op humans (team 0), optional AI ally hosted on player 0, wave attacker last. `SurvivalLayoutGenerator`: 1 human → unchanged (center); 2+ humans → stations on a ring `(radius + DefaultZoneGap) / sin(π/n)`, zone per human outward, mining ring pushed out by the ring, satellite pads round-robin between humans; hosted ally shares its host's station. `SurvivalOutcomeRule`: lose when every human station has fallen. `SurvivalWaveDirector`: waves attack the first human with a living station. Max co-op = 4 players incl. attacker (3 humans, or 2 + AI ally). `SurvivalSetupSave` does not store co-op humans (online save is Phase 6).
- Survival co-op needs a Medium or Large map: `SurvivalPreset.Create` throws for co-op on `MapSize.Small` (2 stations + mining ring do not fit; generator threw "do not fit"). Ring spacing: chord between co-op stations = `2·radius + DefaultZoneGap`.
- Tests (user-requested run, 2026-11-07): full EditMode 2862 tests → 2844 passed, 18 failed. The same 18 fail on the HEAD baseline without these changes (stash check, 2837/2819/18): prefab scale/UI (`AllegianceShip`, `ISDII`, `Tartan` reinforcement views, `UiPrefabScale`/`BattleInputPrefab` DesktopDebugConsole), `HarmonicBomb`, `HealthOverlayAnchor` (Boba/Jango squadrons), `ProcursatorShip`, `ReinforcementZonesSystem` relay ×2, `SelectionMarker` ×3, `ShipAutoPowerUi`, `SurvivalGameplay.AuthoredWaves`. New fixtures all pass. Explicit map runs: `MapGeneration` 42/42, `MapExhaustive` 42/42 passed.
- New EditMode fixtures: `PlayerCommandSinkTests` (BattleInput), `MatchSessionTests` + `SurvivalCoopTests` (GameFlow), `UniqueIdGeneratorTests` (Core). `AGENTS.md` test table: `Services/PlayerCommands` → `BattleInput`.
  - Done 2026-11-07 (compile clean; tests not run), `Services/PlayerCommands/`:
    - Commands = readonly structs with ids/keys only: `UnitOrderCommand` (issuer, `UnitActionId`, receiver ids, point, target id, hardpoint, waypoints), `PurchaseUnitCommand` (`UnitRequestKind` + key + tier; covers units, research, station level, super weapon charge), `CancelPurchaseCommand` (request id), `BuildSiteFacilityCommand` (site index + facility), `FireSuperWeaponCommand` (type + target id).
    - `IPlayerCommandSink` → `AuthorityCommandSink` (battle scope; offline and future host). Resolves ids via `IEntityLocator`; **#5: drops receivers whose `Owner != Issuer`**, then calls `IUnitOrderService`. Validation lives in the sink, not `UnitOrderService` (AI calls stay trusted and unchanged).
    - Per-player/optional handlers self-register into battle-scope `PlayerCommandHandlers` on `Initialize` (no sink → handler dependency, no cycles): `HumanProductionCommandHandler` (EconomyModule, human) → `IFactionService.TryPurchaseUnit`/`CancelBuilding` with the request rebuilt by `UnitRequestSave.CreateRequest`; `HumanSuperWeaponCommandHandler` (SuperWeaponsModule, human) → `CanFire` + `CanTarget` + `Consume` + `Fire`; `CaptureSitesSystem.TryStartAlliedConstruction` (same rules as the old local path: site owner allied with payer, payer pays).
    - Callers switched: `PlayerOrderInputHandler`, `UnitActionsPresenter` → `IPlayerUnitOrders` (`PlayerUnitOrders` builds `UnitOrderCommand` with `ILocalPlayer.Id`); `FactionUiController` purchase, `ShipBuildUiController` cancel, `SuperWeaponPresenter` fire (consume moved to the handler), `CaptureSitesSystem.BuildSelected`.
    - Tests updated to compile: `PlayerOrderInputHandlerTests`/`UnitActionsPresenterTests` fakes implement `IPlayerUnitOrders`; `FactionUnitAvailabilityTests` ctor args; `SuperWeaponAudioTests` uses a direct sink + real handler and a non-null target.
  - Still open for #4: ship abilities (`ShipAbilityService.Press/SubmitTarget/TryActivate` mixes targeting UI state with activation → split first), reinforcement deploy/placement (inside `ReinforcementService` with pointer/camera input), `FactionService.ChangeSelection/CloseSelection` (local selection UI), cheats (local debug, stay direct).


### Phase 2 — Session + lobby
- Code 2026-11-08 (compiles; not run, no tests; `Services/Networking/Online/`, `Entities/OnlineMenu/`):
  - `OnlineMatchService` (ProjectScope): Steam init at startup (`SteamPlatform`, App 480, `+connect_lobby`), `EnterOnline` creates the `DontDestroyOnLoad` `OnlineNetwork` root (`OnlineNetworkManager` + `KcpTransport` + `FizzySteamworks`) only on Host/Join. Implements `INetworkPlayerDirectory`.
  - Lobby protocol: `LobbyHello` (name + version) → `LobbyState` broadcast; `LobbyReady`; `LobbyRejected` (full / version / kicked / battle running) then disconnect after 0.5 s. `OnlineLobby` model: max 4, host = connection 0, members take Human slots in line-up order.
  - Start: `OnlineGameCommand` replaces `IGameCommand` in MainMenuScope → while hosting, setup screens' Start → `TryStartMatch` → `BattleStartMessage` (`BattleSetupSave` JSON + slot) per client → existing loading screen.
  - Battle: `IMatchSession` = `OnlineSession` via `CreateSession` in Skirmish/Survival modes; `NetworkBattleRegistrations` picks per role. `IBattleStartGate` awaited in `BattleStartupSequence` before fade-out: client sends `BattleLoaded` and waits for `BattleBegin`; host waits all loaded (60 s timeout) → baseline + begin per client. `OnlineBattleLifetime` leaves the session when the battle scene closes.
  - UI is interim IMGUI `OnlineMenuPanel` (Online button top-right of main menu): host via Steam/IP, join by IP, accept invite, members, ready, kick, friend invite list, Battle setup, Start/Force Start.
  - Open: uGUI lobby prefab per `UI_UX_GUIDELINES`; host must set one Human slot per member manually (Survival menu has no extra Human slot → online Survival cannot start yet); online restart unsupported; avatars/edit rights per row.
- [ ] Packages added (Phase 0). Still open: `NetworkManager` (empty online/offline scenes) lives in a persistent root owned by `ProjectScope`, outside battle scopes; it is not created in offline sessions.
- [ ] `IOnlineLobby` over Steam lobbies: create (friends-only/private), invite, join, slot properties (faction, team, color, AI, ready), host kick. `KcpTransport` path: host IP entry for dev.
- [ ] Host sends `SkirmishSetupSave` (or a Survival setup) → clients run the existing loading pipeline → ready barrier → host starts.
- [ ] Menu: `Online` route (Host Skirmish, Host Survival, Join by IP) → Lobby UI and Join Flow.
- [ ] Lobby screen: `Open`/`RemoteHuman` occupants, Steam name + avatar, per-row edit rights, ready, invite, kick, leave.
- [ ] Steam join paths: overlay invite, `+connect_lobby` launch arg, `GameLobbyJoinRequested_t`/`GameRichPresenceJoinRequested_t`, rich presence `connect` + status.

### Phase 3 — Tick replication
- Code 2026-11-08 (`Services/Networking/`, compiles, weaver ran on Assembly-CSharp; tests written, not run; not wired into DI — needs the Phase 2 online session):
  - Pure: `NetworkTickSettings` (20 Hz, 1000 B chunks, 2 m / 5°, refresh 1 s moving / 3 s idle, 3-tick = 150 ms buffer), `NetworkTickClock`, `EntityMotion` (arc + climb extrapolation, rates from two samples), `EntityKeyframe` 14 B (0.25 m pos ±8192 m, 16-bit heading, speed 0.5 m/s, turn 1°/s, climb 0.25 m/s), `EntityKeyframeCodec`, `NetIndexMap` (long id → ushort, reuse after release), `DeadReckoningSender`, `KeyframeChunker` (69 keyframes/chunk, ≥ 1 chunk per tick), `ClientKeyframeTrack` (extrapolate + blend into next keyframe).
  - `HostTickReplicator`: spawn/despawn from `IEntityLocator`, motion from `IEntityTransformFacade` sampled per tick, `CreateBaselineSpawns` for join/reconnect. `ClientReplicaBuffer`: Mirror `SnapshotInterpolation` timeline + per-entity tracks, `TrySample(entityId)`.
  - Mirror: `EntityKeyframesMessage` (unreliable), `EntitySpawnMessage`/`EntityDespawnMessage`/`MatchStateMessage` (reliable), `PlayerCommandMessage` with custom writer (kind + one command) in `PlayerCommandSerialization`; `NetworkTickService` (host send), `HostCommandReceiver` (drops commands whose issuer ≠ the connection's slot via `INetworkPlayerDirectory`, then `AuthorityCommandSink`), `NetworkCommandSink` (client).
  - Open: spawn message has no unit type yet (client spawn path = Phase 4); `MatchStateMessage` not sent yet; health/weapon-target/event messages (Sync Inventory) not yet.
  - Tests: `NetworkTickReplicationTests` [Category `Networking`] — clock, codec, extrapolation, dead reckoning, chunking, index map, command serialization round trip, host→client spawn/move/despawn. Run 2026-11-08: full EditMode 2871 → 2853 passed, 18 failed (same pre-existing 18), all 9 Networking tests passed. Committed `b9b738321`.
- [ ] Host `NetworkTickService` (20 Hz accumulator, command queue, change collection, MTU chunking).
- [ ] Messages: `CommandMessage`, `TickMessage` (spawns, despawns, deltas, events), `PlayerStateMessage` (owner only), `MatchStateMessage`; custom `NetworkWriter`/`NetworkReader` extensions for quantized values.
- [ ] Client `SnapshotBuffer` + `SnapshotInterpolation` (`INTERPOLATION_DELAY`).

### Phase 4 — Client mode per system (issue #7)
- [ ] Gate authority-only work: AI, economy, production advance, capture, weapon targeting/damage, ability effects, super-weapon damage, `SurvivalWaveDirector`.
- [ ] Order: movement → weapons/bolts → health/hardpoints/death (explosions and wrecks stay client cosmetic) → squadrons/hangars → abilities → super weapons → capture/economy/reinforcement → Survival HUD/waves.

- Code 2026-11-08 (compiles; not run):
  - Spawn: `EntitySpawnMessage.Kind` + `Payload` (`UnitSave`/`StructureSave` JSON, captured at the next host tick); client `ReplicaEntitySpawner` uses the save-restore factories with the host id + health. Client `OnSpawn` idempotent (rejoin baseline).
  - `ClientReplicationReceiver`: sets transforms from the interpolated timeline in `LateTick`; despawn `Destroyed` → `IEntityDestroyFacade.Destroy` (local explosion/wreck); `EntityHealthMessage` (host sends changed hull/shield every 0.25 s) → `RestoreHealth`; `MatchStateMessage` → `Time.timeScale` (host `HostMatchStateSender`).
  - Client gates: AI slots built as remote human scopes; no hosted faction levels; no station spawn at startup; `ClosedSquadronLaunchSlots` (no hangar launches); `ClientAttackDataFactory` (weapons fire visually, `ReplicaHealthFacade` = no damage); `SurvivalWaveDirector` not ticked; commands via `NetworkCommandSink`.
  - Open: non-destroyed despawn (squadron docking) leaves the client copy; weapon target events (client weapons pick targets locally); ability / super-weapon effects not replicated; credits + build queue per player not synced (client economy ticks locally); capture runs locally; client pause/speed buttons act locally.
- [ ] Disconnect → `AiPlayerScope` takeover; host leaves → match ends.
- [ ] Issues #8, #10, #11 v1 rules; error UI (Steam not running, lobby full/closed, connection failure, version mismatch, host left, kicked).

- Code 2026-11-08 (compiles; not run): host keeps a disconnected member's slot; rejoin by same Steam name within `RECONNECT_GRACE_SECONDS` = 60 → `BattleStartMessage` → load → baseline + begin. Past grace → `PlayerAbandoned` → `AbandonedPlayerTakeover` only logs (runtime `AiPlayerScope` swap still open). Client: host drop → `HostLeftBattleExit` → menu shows "The host left". Errors in `OnlineError` + panel text: Steam not running, lobby full, version mismatch, host left, kicked, battle running, connection failed.

### Phase 6 — Online saving (post-v1)
- [ ] Full-state baseline on join (also enables reconnect and late join).
- [ ] Host-only save + slot → Steam ID map; host-only autosave.
- [ ] Resume lobby: host loads save → reserved slots for saved Steam IDs → AI fills empty slots.
- [ ] Optional: in-game Friends panel / friends' lobby list.

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
- Late join and reconnect: not in v1; the Phase 6 full-state baseline enables them and online loading.
- Saved friend not available on resume → slot runs as AI.
- Survival wave AI and every AI slot run only on the host.
- Cosmetic randomness (`FighterManeuver`, `ImpactEffectView`, `UnitWreckService`) stays client-local.
- Fighters: replicate the squadron entity only; individual fighter maneuvers are client cosmetic.
- Steam not running → Steam online options are disabled with a message; direct-IP (KCP) stays available.
- Build version mismatch → lobby version filter + a handshake check on connect (Mirror `NetworkAuthenticator`).

## Verification

- EditMode: command serialization round-trip, issuer validation, N-human `MatchRules`, Survival co-op presets, offline parity, tick message delta encoding. Categories: `GameFlow`, `BattleInput`, `Core` + touched systems.
- User-only: two machines over Steam (App 480) and over Tailscale/KCP; Multiplayer Play Mode or two builds for local iteration.

## Status

- 2026-11-07 re-check vs `26f718498`: nothing implemented (no Mirror/Steam packages, no session/command/tick types, no Sandbox spike). Issues #1–#14 all still present in source. Only counts corrected.
- 2026-11-07: added Lobby UI and Join Flow, Online Saving, Phase 6 (proposals; not yet user-confirmed in detail).
- 2026-11-07 started: packages + Phase 0 spike code + Phase 1 session seam. Verification: Unity compile clean, Mirror weaver ran (`Mirror.GeneratedNetworkCode` in `Sandbox.Multiplayer`), 0 console errors/warnings. Tests not run (local override); spike not run (needs two machines / Play Mode by user). `TestPlayers.CreateLocalPlayer` updated for the new ctor.

## Lobby Decisions (user, 2026-11-08)

- Menu: Play → Online → Host Skirmish | Host Survival | Join by IP. **Join by IP stays in release builds.**
- Lobby screen: reuse the Skirmish setup screen (`SkirmishUi`) with extra columns (Steam name/avatar, ready, kick).
- Edit rights: each human edits own faction/team/color; host edits AI slots, map, money, victory, super weapons.
- Start: enabled when all humans are ready **and** host can force-start.
- Survival co-op: max 3 humans now (Medium/Large maps). Future: multiple stations near each other or one shared station per side.
- Disconnect: AI takes over, match continues, **not instantly — reconnect must be possible** (needs the full-state baseline on join, moved from Phase 6 into v1). Host leaves → match ends after the same grace window.
- Look: build straight in the existing UI style per `UI_UX_GUIDELINES` / `UI_CODE_BUILD_GUIDE`; no Penpot mock first.
- Fog: v1 sends all unit data; a cheating (modified) client could read hidden units. Pending user answer on acceptability.

## TODO / Open Questions

- Max humans in Survival co-op (2? up to 3 with the wave attacker slot)?
- Fog cheating: acceptable for friends-only v1?
- Own Steam App ID: decide before any distribution beyond development testing.
