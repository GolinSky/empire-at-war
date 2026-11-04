---
category: Features
created: 2026-11-02
tags:
  - multiplayer
  - networking
  - research
---
# Multiplayer Plan - Research

Plan → [[TODOs/Features/Multiplayer_Plan|Multiplayer Plan]]. Audit date 2026-11-02, commit `549d4c2ab`. Counts exclude `Tests/` and `Editor/`.

## Current Networking State

- No networking package or code: no Mirror/NGO/Photon/PlayFab in `Packages/manifest.json` or in `Assets/Scripts`.
- `com.unity.multiplayer.center` 1.0.1 is installed (info window only).
- `_Heathen Engineering` contains `Common` and `UX` only (no Steamworks).

## Determinism Audit (why not lockstep)

- `Time.deltaTime` / `Time.time` / `Time.fixedDeltaTime`: 67 files (e.g. `BattleClock.Tick`, `WeaponComponent` salvo timing `Time.time`).
- `UnityEngine.Random`: 21 files, including sim code: `WeaponComponent` retry jitter (`Random.Range(MIN_RETRY_INTERVAL, MAX_RETRY_INTERVAL)`) and group-sort offset.
- `System.Random`: map generation (`BattleMapLoader` seeds `new Random(_battle.Map.Seed)`, so it is deterministic); also `FighterManeuver`, `EdgeSpawnPoints`, `UnitWreckService`, `MusicService`, `MatchNebula`.
- Ids: `UniqueIdGenerator.GenerateUniqueId` = `BitConverter.ToInt64(Guid.NewGuid())`. Used by `ShipFactory`, `SquadronFactory`, `SpaceStationFactory`, `MiningFacilityFactory`, `DefendPlatformFactory`, `SiteFacilityBuilder`, `SkirmishPreset`.
- Speed: `SkirmishOrchestrator.ApplyTimeScale` (`FAST_TIME_SCALE`/`PAUSE_TIME_SCALE`); `GameController` resets `Time.timeScale = 1f`.
- Physics: 0 `Rigidbody`, 0 `NavMeshAgent`, 1 file with `Physics.*` queries.

## Ownership / Local Player

- `PlayerId` struct = slot index + 1; `None` = neutral.
- `ILocalPlayer` used by 53 runtime files. Most are presentation (fog, selection, minimap, audio, UI).
- Sim-affecting `ILocalPlayer` uses:
  - `CaptureSitesSystem`: `CanAfford` via `_localPlayer.Id`; construction from the UI calls `TryStartConstruction(_selectedSite, type, _localPlayer.Id)`. A per-owner overload `TryStartConstruction(int, PlayerId, SiteFacilityType)` already exists.
  - `ReinforcementZonesSystem`: visibility for `_localPlayer.Id`.
  - `BattleVictoryService`: result built for `_localPlayer.Slot.Team` / `.Faction`.
  - Entity installers (`ShipInstaller` etc.): `RegisterFog(!IsFriendly(Owner))`, audio dialog only if `IsLocal(Owner)`. These are per-viewer composition, so they are fine for clients.
- `MatchRules`: `MIN_PLAYERS = 2`, `MAX_PLAYERS = 4`, `MAX_TEAM_COLORS = 8`. Exactly one human is enforced by `FindHuman` and `Validate`.

## Command Entry Points

- `IUnitOrderService`. Callers: `PlayerOrderInputHandler`, `UnitActionsPresenter`, `HardPointOverlayPresenter`, `UnitHighlightService`, `UnitOrderFeedbackUiController`, `SimulationTargeting`, `SurvivalWaveDirector`, `EnemyUnitCommander`, `EnemyTaskForceExecutor`, `EnemyShipTaskGroupPlanner`.
- `UnitOrder` = `UnitActionId`, `PlayerId Issuer`, `Vector3 Point`, `IEntity Target`, `IReadOnlyList<Vector3> Waypoints`, `int TargetHardPointId`.
- Production: `FactionService` → `PlayerFactionModel.QueueUnit` / `TryCancelCurrentUnit(string id, ...)`.
- Abilities: `ShipAbilityService.Press(casters, id)`, `SubmitTarget(target)`, `TryActivate(caster, id, target)`. Targeting UI state and activation are in one service.
- Super weapons: `SuperWeaponPresenter` → `ISuperWeaponFireService.Fire(type, target)`.

## Sim / View Coupling

- `WeaponComponent` (944 lines, `MonoComponent`, `ITickable`): target selection, salvo scheduling, `ApplyDamage` → `_attackCoordinator.ScheduleImpact`.
- `WeaponHardPoint` (`Components/ViewComponents/Health`): launches missiles (`_missiles.Launch`) and calls `WeaponPresenter.ApplyDamage`.
- Bolts are view-only batches: `BoltFlightBatch`, `BoltPlacementJob`, `BoltShot` → client cosmetic driven by fire events.

## Composition Roots

- `SkirmishScope.Configure`: battle-level registrations, sim and UI mixed (≈230 lines).
- `PlayerScopeBootstrap.Initialize`: one child scope per slot (`AiPlayerScope` or `LocalPlayerScope`); registers each player's factories in `BattleUnitSpawner`.
- `LocalPlayerScope.RegisterPlayerServices`: sim (`PlayerFactionModel`, `FactionResearchModel`, `SuperWeaponModel`, `ReinforcementService`, `PlayerService`) plus UI (`ReinforcementUiController`, `HeroUiController`, `FactionAnnouncementService`).

## Survival / One-Human Assumptions

- `SurvivalPreset.Create`: player 0 = `Human`, optional `AiAlly` (team 0), last = `WaveAttacker` (team 1, `BattleFeatures.None`); `STATION_LEVEL = 5`.
- `BattleDefinition.Human => Players.Single(p => p.Role == PlayerRole.Human)` → throws with 2 humans.
- `BattleDefinition.GetProductionHost(player)` → an `AiAlly` is hosted on `Human` (`HostedFactionLevel` in `PlayerScopeBootstrap`).

## UGS Relay / Lobby + Mirror (web, 2026-11-02)

- Unity's Mirror + UTP + Relay sample `Unity-Technologies/unity-relay-mirror-sample` is **deprecated**. Built for Mirror v67.1.0; `UtpTransport.cs`, `RelayNetworkManager : NetworkManager`. Needs UGS Authentication + Relay, `com.unity.transport`, Jobs.
- The Relay SDK has a hard dependency on Unity Transport (UTP).
- Multiplayer Services SDK (`com.unity.services.multiplayer`): `SessionOptions.WithRelayNetwork().WithNetworkHandler(handler)`. A custom `INetworkHandler` turns off the built-in NGO/N4E integration. `StartAsync(NetworkConfiguration)` must leave the game listening (host) or connected (client); `StopAsync()` stops networking.
- No Mirror-specific `INetworkHandler` found → write `MirrorNetworkHandler`.
- Pricing (UGS pricing page): Relay first 50 average monthly CCU free, then $0.16/CCU; 3 GiB per CCU free up to 150 GiB/month; overage $0.09/GiB US+EU, $0.16/GiB Asia+AU. Lobby 10 GiB/month free per region group.
- Multiplay Game Server Hosting was deprecated 2026-04-01; Relay, Lobby, Friends and Vivox continue.
- Sources: https://docs.unity.com/relay/mirror, https://github.com/Unity-Technologies/unity-relay-mirror-sample, https://docs.unity.com/en-us/mps-sdk/manage-session-network-connection, https://unity.com/products/gaming-services/pricing

## Stack Decision History (2026-11-02)

1. Mirror + UGS Relay → rejected: Unity's Mirror Relay sample/transport is deprecated (Mirror v67.1.0).
2. NGO + UGS Sessions/Relay/Lobby → rejected:
   - Free bandwidth = 3 GiB × average CCU (≈ 1.19 KiB/s per connected player). For 2–4 friends ≈ $0.20–2/month over the free tier, and a card is needed.
   - The project id ships in builds, so a public repo or build lets others use the quota.
3. EOS P2P → not chosen: free, but heavier setup (Epic portal, Connect login) and the NGO transport was unverified.
4. **Chosen: Mirror + FizzySteamworks (App ID 480 for dev, own App ID to ship) + KcpTransport for LAN/Tailscale dev + Steam lobbies behind `IOnlineLobby`.** Steam relay is free with no quota. Mirror has the established Steam transport and built-in snapshot helpers.
- Steam 480 limits: testing only (no release); shared lobby namespace → filter on lobby data; Steam client required; players show as "Spacewar".
- Sources for the UGS numbers: https://unity.com/products/gaming-services/pricing, https://support.unity.com/hc/en-us/articles/4410136449812-How-is-the-Relay-Service-Priced, https://support.unity.com/hc/en-us/articles/6821475035412-Billing-FAQ-Unity-Gaming-Services

## NGO Facts (reference only; Context7, NGO 2.11 docs)

- `CustomMessagingManager.SendNamedMessage(string name, ulong clientId, FastBufferWriter, NetworkDelivery = ReliableSequenced)`; `SendNamedMessageToAll(...)` is server-only and throws on clients; `RegisterNamedMessageHandler(name, (senderId, FastBufferReader) => ...)`.
- `NetworkTickSystem.Tick` (`event Action`) fires before every network tick; tick number = `NetworkManager.LocalTime.Tick` / `ServerTime.Tick`; rate = `NetworkConfig.TickRate`.

## Mirror Facts (Context7, mirror-networking gitbook)

- Custom spawning: `NetworkClient.RegisterSpawnHandler(assetId, SpawnDelegate, UnSpawnDelegate)` or `RegisterPrefab`. This fits pooling/Addressables if per-entity identities are used later.
- Relay transports: Steam `FizzySteamworks`/`FizzyFacepunch` (NAT punch + Steam relay), Epic Online Services (free relay), Edgegap `EdgegapKcpTransport` (paid distributed relay; you start relay sessions yourself or through Edgegap lobby/matchmaker), LRM (WebGL).
- Mirror hosting guide: NAT punchthrough works about 70% of the time; a relay always works but costs bandwidth.
