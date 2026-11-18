---
category: Optimization
status: todo
agent_marker: AGY
created: 2026-11-18
---
# [AGY] Online Battle 10-20 FPS Host and Client Performance Optimization

## Goal
- Raise host and connected-client frame rates from `10–20 FPS` back toward playable targets (`30–60+ FPS`) in large online 3v4 / 3v5 `UltraHard` skirmish battles.
- Add lightweight periodic performance logging in Release builds (`Player.log`) so future online battle slowdowns have direct FPS and entity-count diagnostics.

## Evidence (`Player.log` Host Battles 5–8)
- Hardware: `NVIDIA GeForce RTX 5070` (`11943 MB VRAM`), `16` CPU threads, `2560x1440`, `Cinematic` quality, `nebulaEnabled: true`, `renderResolution: 1.0` (`settings.json`).
- **Battle 7** (`07_host_battle3_3v5_...`, penultimate online battle): 3 humans vs 5 `UltraHard` AIs → peak **`107` AI ships** (`25+24+17+23+18`) + **`56` EnemyShips** (`163+` ships total, plus carrier squadrons, platforms, and mines); `645` `[EnemyAI]`, `462` `[EnemyAI:Production]`, `221` `[EnemyAI:SuperWeapon]` log lines.
- **Battle 8** (`08_host_battle4_...`, final online battle): 3 humans vs 4 `UltraHard` Rebellion AIs → peak **`84` AI ships** (`23+20+19+22`) + **`30` EnemyShips** (`114+` ships total); `1102` `[EnemyAI]`, `634` `[EnemyAI:Production]`, `229` `[EnemyAI:SuperWeapon]` log lines.
- Both Release `Player.log` runs contain zero FPS / frame-time telemetry because diagnostics (`BattleFpsHistory`, `BattleTelemetry`) are compiled out outside `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.

## Root Causes (Source-Verified)
1. **Connected clients run redundant local simulation (`Squadron.cs:203, 270-275`, `Ship.cs:192`, `RadarComponent.cs:66`)**:
   - In `Squadron.Initialize()` (`L200-203`), `_pilot.Loiter(...)` and `_orderScheduler.Register(UpdateOrder)` run unconditionally even when `_isReplica == true` (`!session.IsAuthority`, `L156`).
   - `Squadron.Tick()` (`L270, L275`) unconditionally executes `_pilot.Tick(deltaTime)` and `_flight.Step(deltaTime)` every frame before `ClientReplicationReceiver` overwrites the replica root transform in `LateTick()` (`L276-278`).
   - `Ship.Tick()` (`L192`) runs `_orders.Tick(Time.deltaTime)` + `SynchronizeComponents()` on clients too.
   - `RadarComponent.Initialize()` (`L66`) unconditionally registers every ship and squadron with `RadarScanService` on connected clients even though clients do not run AI or authority target selection.
2. **`RadarScanService.FixedTick()` multiplies CPU work 3–5x at 10–20 FPS (`RadarScanService.cs:23, 98, 116-131`)**:
   - `RadarScanService` runs on `IFixedTickable.FixedTick()` (50 Hz, `fixedDeltaTime = 0.02 s`).
   - When frame time reaches `50–100 ms` (`10–20 FPS`), Unity invokes `FixedUpdate` **`3–5` times per rendered frame**, running `ApplyPendingTargets`, `CollectDueRadars`, `RadarPositionJob`, and `ScheduleDetection` (`ScanDueRadars`) `3–5×` per frame.
3. **Starved projectile pools trigger per-frame `TargetSelectionBatch` churn (`WeaponComponent.cs:610-611`, `TargetSelectionBatch.cs:128-170`, `ShotEffectCache.cs:48-82`)**:
   - When `ShotEffectCache.TryReserve` rejects a hardpoint (`ShotPoolPrewarmer` starved to single-unit capacity + `1` creation/frame), the hardpoint never starts `_sequence` and stays `!IsBusy`.
   - Next frame `WeaponComponent.Tick()` (`L610-611`) queues the same starved hardpoints into `CombatAttackCoordinator.QueueTargetSelection` → `TargetSelectionBatch.Process()` (`WeaponTargetSelectionJob` + candidate capture + `TryReserve` waitlist re-check) over and over every frame across thousands of hardpoints.
4. **Host & Client Networking JSON Allocations and Lookups (`NetworkTickService.cs:51-54, 71-90`, `ClientReplicationReceiver.cs`, `HostReplicatedStateSender.cs:30-47`, `HostTickReplicator.cs:96-112`)**:
   - Every `0.25 s` (`HEALTH_INTERVAL_SECONDS`), `NetworkTickService.SendChangedHealth()` (`L71-90`) allocates `UnitHealthSave` (`CaptureHealth()`) and calls `JsonUtility.ToJson(save)` for every entity whose hull or shields changed (`110–163+` ships regenerating shields/taking fire), and every client runs `JsonUtility.FromJson<UnitHealthSave>`.
   - Every `0.5 s` (`SEND_INTERVAL_SECONDS`), `HostReplicatedStateSender.Tick()` (`L30-47`) calls `state.Capture()` (`JsonUtility.ToJson`) on every `IReplicatedState` just to string-compare `sent == json`.
   - `HostTickReplicator` (`L112`) calls `entity.TryGetFacade<IEntityTransformFacade>()` at `20 Hz` per live entity instead of caching handles on `HandleAdded`/`HandleRemoved`.
5. **No Release-Build Performance Diagnostics (`BattleFpsHistory.cs:1`, `BattleTelemetry.cs:18`)**:
   - `#if UNITY_EDITOR || DEVELOPMENT_BUILD` hides frame-time histograms and shot/pool starvation counters in Release builds.

## Implementation
1. **Eliminate redundant replica simulation on online clients (`Squadron.cs`, `RadarComponent.cs`, `Ship.cs`)**:
   - Gate `Squadron` order scheduling (`_orderScheduler.Register`), AI targeting scans, and authority pilot/flight physics behind `!_isReplica` (keeping visual wingman interpolation/FX lightweight on replicas).
   - Gate `RadarScanService` registration on authority (or limit client radar queries if needed solely for client fog-of-war/local UI).
2. **Break `FixedUpdate` radar spiral (`RadarScanService.cs`)**:
   - Move `RadarScanService` from `IFixedTickable.FixedTick()` to `ITickable.Tick()` (time-sliced using elapsed delta time) so low FPS never triggers `3–5×` duplicate radar scans in one frame.
3. **Eliminate JSON string churn in hot replication paths (`NetworkTickService.cs`, `ClientReplicationReceiver.cs`, `HostReplicatedStateSender.cs`, `HostTickReplicator.cs`)**:
   - Replace per-entity `UnitHealthSave` JSON serialization every `0.25 s` with a compact binary struct/array `EntityHealthMessage` (hull, shields, packed hardpoint/member health floats).
   - Track dirty versions on `IReplicatedState` so `HostReplicatedStateSender` skips `Capture()` when unchanged.
   - Cache `IEntityTransformFacade` in `HostTickReplicator` upon `HandleAdded`.
4. **Stop starved-pool `TargetSelectionBatch` re-work**:
   - Co-resolve `ShotPoolPrewarmer` / `ShotEffectCache` starvation (see [[TODOs/Bugs/Weapon_Fire_Rate_And_Network_Sync_Low_FPS_AGY]]) and avoid re-running target selection every frame while a hardpoint is explicitly throttled or waiting in `_waiting`.
5. **Release-build periodic perf log (`BattleFpsHistory` / lightweight `[BattlePerf]` logger)**:
   - Log a single summary line to `Player.log` every `30 s` during battles (FPS avg/min/1% low, fixed step count/frame, active ships/squadrons, host/client role, projectile in-use/waiting counts).

## Files
- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Entities/Ship/Ship.cs`
- `Assets/Scripts/Components/Radar/RadarComponent.cs`
- `Assets/Scripts/Services/Radar/RadarScanService.cs`
- `Assets/Scripts/Services/Networking/NetworkTickService.cs`
- `Assets/Scripts/Services/Networking/ClientReplicationReceiver.cs`
- `Assets/Scripts/Services/Networking/HostReplicatedStateSender.cs`
- `Assets/Scripts/Services/Networking/HostTickReplicator.cs`
- `Assets/Scripts/Services/Analytics/BattleFpsHistory.cs`
- Affected test suites to review (do not run): `Radar`, `Squadrons`, `Networking` (`HostTickReplicatorTests`, `ClientReplicationTests`), `Core`

## TODO
- [ ] Move `RadarScanService` off `FixedTick()` to once-per-frame `Tick()`
- [ ] Skip authority `Squadron`/`Radar`/order simulation loops when `!session.IsAuthority`
- [ ] Replace `SendChangedHealth` JSON with binary health delta message and add dirty checks to `HostReplicatedStateSender`
- [ ] Cache transform facades in `HostTickReplicator`
- [ ] Add low-overhead Release `[BattlePerf]` periodic logging to `Player.log`
- [ ] Capture matched Development-build Profiler trace in 3v4 battle before/after changes
