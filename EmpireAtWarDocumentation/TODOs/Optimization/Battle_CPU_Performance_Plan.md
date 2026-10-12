---
category: Optimization
status: in-progress
created: 2026-10-11
updated: 2026-10-12
---
# Battle CPU performance follow-up

## Goal
- Attribute and reduce remaining main-thread costs after the radar rewrite; preserve combat, movement, visibility and audio behavior.
- Keep separate evidence for sustained cost, occasional stalls, rendering submission and GPU execution.
- Execution order: allocation attribution + target preparation → combat application and squadron ticks → navigation and ship/audio → matched acceptance.

## Decision
- Current baseline: 2026-10-11 15:25 Kyiv, 78 frames / 10.143 s, 4× speed, High Fidelity, 2560×1440; median 123.010 ms / 8.1 FPS.
- Fresh 15:31 sample: 12 frames; median CPU 96.584 ms, GPU 2.965 ms. Short diagnostic sample, not a benchmark.
- Radar: live source now has centralized `RadarScanService`; latest battle marker average 8.581 ms versus historical 656.729 ms. Different workloads/marker coverage → observational improvement, not controlled speedup.
- Fix execution plan: [[TODOs/Optimization/Battle_CPU_Fixes_Plan]].
- Attack implementation remains in [[TODOs/Optimization/Battle_Attack_Optimization_Plan]]; rendering in [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan]].
- Already analyzed: capture comparison, main-thread hotspot ranking, attack/radar source paths; rendering agent verified 949 events, shader/texture groups, squadron indicators and adjacent GPU scopes.
- Not established: exact allocation callers, current audio share, causes of every spike, matched implementation gains or final radar parity.
- Current request: analyze existing captures and related files; skip another battle run. Source review completed at `74368d81`; implementation, raw Profiler replay and runtime acceptance remain pending.

## Rules
- Keep the user's running session in Play Mode. Do not stop it for diagnostics; coordinate any pause/capture with the active request.
- Re-read live source before editing: other agents changed radar during this investigation. Do not reimplement its obsolete per-component scan path.
- One writer per shared combat file and one active Unity capture at a time; record ownership before implementation.
- Record source revision plus dirty diff identity, fleet/squadron/effect counts, camera, resolution, time scale, quality and profiler settings.
- Compare ≥3 representative 10-second runs per variant, with matched workload and separate 1×/4× results; report median/p95/p99 and allocations.
- Nested inclusive/self timings and worker totals are not additive wall time. Frame Debugger events are not draws.
- Follow AGENTS.md test scene safety and asset persistence rules. This planning update runs no tests and changes no game assets.

## Implementation

### Saved-data and source review — 2026-10-11
- [x] Read CPU/attack/rendering plans; verify saved CSV, Profiler JSON and complete historical Frame Debugger against live source.
- [x] Confirm current priorities: target capture 16.459–21.118 ms/frame; due apply 12.010–16.864 ms/frame; squadron Tick 8.469 ms in fresh sample. Values come from different workloads.
- [x] Separate stalls: frame `20777` has 287.191 ms due apply; frame `20800` has 81.268 ms navigation across 35 plans. Navigation attribution must include command callers outside Ship.Tick.
- [x] Preserve existing improvements: centralized radar, retained ordered candidates, counted hardpoint collection, cheaper ring checks, pooled/inactive idle shot effects and 0.5 s squadron aim refresh.
- Remaining candidates: repeated eligibility/position snapshots, fallback reason counters, due-ID lookup, squadron order/steering/pose split, stationary replans and repeated whole-pool audio mixing.
- Capture limitation: summaries omit allocation sizes/ancestry/stacks; Frame Debugger saves only finished frames. Detailed method/line findings and uncertainties: [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research#Source analysis — 2026-10-11]].

### Allocation attribution and GC
- [x] Confirm allocation pressure: latest 3.727 MB/frame average, maximum 9.640 MB; historical 44.275 MB/frame and 266.846 ms GC collection.
- [ ] Inspect existing profiler.data CPU Hierarchy GC Alloc and Timeline before another capture; separate bytes, allocation count, collection time and thread.
- [ ] If caller stacks are absent, enable GC.Alloc call stacks before a short new capture; Deep Profiling off initially. Restore capture settings afterward.
- [ ] Export top allocating stacks with method/file/line, bytes/frame, calls/frame, bytes/call and containing gameplay phase. Separate Editor/tool overhead.
- [ ] Optimize only proven leading sources: repeated temporary data, buffer growth, strings/logging or effect creation according to stacks; do not assume pooling solves all allocations.
- [ ] Verify reduced bytes and collection stalls in matched runs; do not attribute all collected garbage to the scope where GC happens.
- Done gate: ranked allocation report, before/after evidence for selected changes, no gameplay/lifecycle regression.
- Reference: https://docs.unity3d.com/Manual/profiler-cpu-navigating.html ; allocation call stacks affect future frames only.

### Squadron simulation
- [x] Fresh profiler: Squadron.Tick averages 8.469 ms inclusive across 12 frames; earlier audit ~11.35/11.88 ms.
- [x] Trace Squadron.Tick: UpdateOrder → pilot steering → flight Step/death synchronization/pose application. WeaponComponent ticks independently.
- [ ] Measure order acquisition, pilot steering, model integration and pose application separately; do not charge the whole weapon batch to Squadron.Tick.
- [ ] Record live squadrons/craft and active states; identify duplicate/unchanged work before lowering update rates.
- [ ] Validate formation, targeting, member loss, visibility and order transitions; measure per-squadron and total cost.
- Scope excludes visual squadron indicator rendering, owned by the rendering plan.

### Navigation and ship/audio attribution
- [x] Latest navigation average 4.812 ms, maximum 81.268 ms; nested candidate/route/flood costs are 4.678/3.901/2.720 ms average.
- [ ] Record Plan requester/state/reason, retry count, contacts, grid dimensions, candidates and route/flood counts.
- [x] Verify source: stationary pursuit retries and per-Plan grid reconstruction remain; ring candidates already skip edge snapping and reject known unreachable cells.
- [ ] Attribute redundant requests before fixing; preserve nearest-reachable fallback, arrival/deferred commands and reservation ordering.
- [ ] Split Ship.Tick into order/state execution and AudioShipComponent.UpdateAudio. Latest average 7.069 ms; current code calls both under one marker.
- [x] Trace audio: each accepted shot/matching loop renewal can remix all 20 voice slots, plus service Tick; consider one frame-boundary mix only after measuring share and preserving timing.
- [ ] Record SFX requests accepted/refused, loop updates, active sources, voice stealing, source creation and audio allocations. No separate audio capture exists in the reviewed outputs.
- [ ] Measure pooled projectile flight/tracking versus impact completion, muzzle-flash ticking and effect retirement; latest BoltShot marker 8.217 ms/frame.
- [ ] Measure cold versus warm projectile pools: latest 462 creations / 78 frames, 1.325 ms/frame instantiate scope; preserve lease/drain and owner-death behavior.
- Done gate: identify measured child costs, select bounded fixes, verify behavior and matched timings; do not call a broad marker a diagnosed bug.

### Navigation, hangar and capture fixes — 2026-10-12 (uncommitted)
- [x] Grids persist across frames (`ShipNavigationCache`, LRU `MAX_GRIDS = 8`, `Allocator.Persistent`); built once: block raster + `NavigationRegionJob` labels.
- [x] Per-plan Dijkstra flood → goal-directed A* in `NavigationRouteJob`; different region → no search. `NavigationFloodJob` deleted.
- [x] Nearest reachable → Burst ring search (`NavigationNearestReachableJob`, `NavigationGridCells.FindNearestCellInRegion`).
- [x] Queued plans: max 4 and `PLAN_BUDGET_MILLISECONDS = 2` per frame. Marker `Battle.Navigation.Flood` → `Battle.Navigation.PathSearch`.
- [x] Hangars: one reserve launch per frame across all hangars (`ISquadronLaunchSlots`, `HangarModel.AdvanceCooldown`).
- [x] Target capture: eligibility/target-state lookups once per contiguous group run (behavior identical).
- Editor capture before → after (larger second battle, not matched): nav plan worst frame 20.2 → 5.7 ms; frames >3 ms 42 → 12; NearestReachable 2–6 → 0.03 ms; hangar spike 50–95 → 16.7 ms.
- EditMode: 1442 pass; 26 failures predate this change (asset/data tests and `ShipEngineHardpointTests` NRE from `bfc24d67`).
- [x] Target selection rewrite: `TargetSelectionCapture` = stable ref-counted hardpoint slots; weapons keep `int[]` slot + group-index arrays (rebuilt only on target change); batch block-copies them; `WeaponTargetSelectionJob` reads slot positions/flags in Burst. Same candidate order and checks.
- [x] Per-batch cache of cloak/fog per target entity; `Refresh` reads only slots marked by this batch's owners.
- [x] Idle backoff: nearest candidate > `MaxWeaponRange` + hardpoint span → weapon waits `IDLE_RESELECT_INTERVAL = 0.25 s`; any target change resets it. Requests ~277 → ~74/frame.
- [x] Turret turn delay (logic only): `WeaponFamilyRule.turnRate` × `WeaponCaliberRule.turnRateMultiplier`; Squadron platform → 0. Data: Laser 120, Turbolaser 60, Ion 60, MassDriver 40, Missile/Torpedo/IonTorpedo 90, PointDefense 240, Beam 20 °/s; caliber Light 1 / Medium 0.75 / Heavy 0.5. Mount yaw starts mid-arc; salvo waits |Δyaw| / rate, then re-checks target validity and range.
- [x] `IsRouteClear` skips contacts outside the route bounds (exact).
- Bug fixed: zero-candidate owner at a full buffer's end → `NativeArray.Copy` threw each LateTick (broke movement, 10 FPS).
- Capture 3 (before used-slot/eligibility cache): TargetBatch 8.8 → 5.0 ms (Capture 3.9, Job 0.07); nav plan max 3.97 ms; hangar peak 23 ms. Re-measure after cache changes.
- Squadron spawn (editor, frame 1747): 23 ms = Instantiate 10.6 (Copy 5.6) + Zenject self 7.9 + `GameObjectContext.Construct` 3.3. Pooling needs a reset path across ~20 squadron components/models → separate plan.
- EditMode: all non-legacy tests pass; 26 pre-existing failures unchanged.
- [x] Zenject reflection baking: `Assets/Settings/Data/Zenject/ZenjectReflectionBakingSettings.asset` (builds + editor, all generated assemblies, namespace `^EmpireAtWar`). Assembly-CSharp 1.32 → 1.58 MB woven; EditMode 1451 pass / 26 legacy.
  - Plugin patches: `UnityAssemblyResolver` skips in-memory assemblies (empty `Location` → "Invalid path"); weaver writes `<Name>k__BackingField` for get-only `[Inject]` properties (23 in project; previously NRE).
  - Editor baking drops PDB line numbers in Assembly-CSharp stack traces; decide editor on/off after spawn re-measure.
  - **Removed 2026-10-12** (settings asset deleted): weaver reflects on the *loaded* assembly → after code changes the woven DLL references itself ("Assembly-CSharp references itself"), Unity silently keeps the stale domain. Also wove `Zenject-ReflectionBaking-Editor`. Release builds at risk: `[Inject]` fields under `#if UNITY_EDITOR || DEVELOPMENT_BUILD`. Recovery: delete affected DLLs in `Library/ScriptAssemblies` + clean compile. Plugin patches remain, dormant.
- Squadron pooling → [[TODOs/Optimization/Squadron_Pooling_Plan]].
- Capture 4 (baked, all changes; editor, profiler on, 2000 frames): median 49.7 ms; TargetBatch 4.9 ms (Capture 3.8); nav plan max 5.7 ms.
  - Squadron spawns: 49 frames, median 13.8 ms, max 26 ms. Spawn frame 448: Instantiate 4.0, `GameObjectContext.Construct` 1.86 (was 3.3), HangarComponent self 7.16 (was 7.85) → baking saves ~1.5–2 ms/spawn.
  - Live counters (profiler off): ~68 requests/frame, 1.6 candidate rebuilds/frame (0.13 ms), post-capture batch 0.34 ms, **2060 slots refreshed/frame** of ~2.9k → used-slot filter barely helps; capture cost = hardpoint position reads (`HardPointAdapter.Position` → view transform).
  - One 1.6 s frame, all self time in `Battle.Attack.DueBatch.Impact`; same session logged "Stopping profiler … not able to send data fast enough" → possibly profiler artifact. Unconfirmed; re-check without profiler.
- [x] Slot positions via `TargetSlotPositionJob` (`IJobParallelForTransform`, `ScheduleReadOnly`, batch 64) over a slot-aligned `TransformAccessArray`. Verified all `IHardPointModel` positions = `Transform.position` (`HardPoint`, `HullTarget`, `FighterView`). Freed slots keep their transform; destroyed entries skipped via `isValid`. Tests pass (uncommitted, 2026-10-12).
- Capture 5 (2026-10-12, no baking, slot position job + squadron pooling; editor, 1995 frames, ~169 squadrons): median 39.4 ms, p95 53.1, p99 62.7.
  - TargetBatch **1.37 ms** (Capture 1.07, Apply 0.23); `CombatAttackCoordinator.LateTick` 1.88 ms (was 7.35). Nav plan max 2.9 ms.
  - Hangar frames > 3 ms: **2** (was 49) → squadron pooling reuses contexts; `SquadronPool.Tick` 0.002 ms avg.
  - Found during capture: `RadarScanService` applied all adds before removes → pooled squadron despawn+respawn within one radar step threw duplicate-key in `ApplyPendingTargets` every step. Fixed: one ordered `_pendingChanges` queue; 2 regression tests. Re-check in battle pending.
  - Pool binding fixed: `SquadronPool` moved from `SkirmishMainInstaller` (scene, cannot see per-player `SquadronFactory`) to `GameUnitsInstaller` (per player).

### Live verification of `89abf347` + `d520a2a8` — 2026-10-12
- Domain current (`TargetSlotPositionJob`, `SquadronPool` loaded). Capture 6: editor, profiler on, 2000 frames, t≈120–200 s, ~114 squadrons (not matched to capture 5's ~169).
- Frame median **37.1 ms** (was 39.4); p95 80.3 / p99 271.8 inflated by 41 frames > 150 ms = editor-wide stalls (`File.Open` 63 ms, `UnsafeUtility.Free` 65 ms, PhysX 65 ms, UGUI 60–130 ms, ~34 ms per projectile `Instantiate`).
- TargetBatch 1.73 ms (Capture 1.17); `CombatAttackCoordinator.LateTick` 3.47 ms avg, inflated by stalled frame 917 (344 ms projectile `Instantiate` inside DueBatch.Emission).
- Nav plan max 4.7 ms (one plan; budget checked between plans), avg 0.09 ms.
- Squadron.Tick 2.7 ms; FogVisibility LateTick 1.6 + MiniMapUnitMarker LateTick 1.6 ms; CaptureSitesSystem.Tick 1.2 ms; UGUI UpdateBatches 1.75 ms self.
- [x] Old 1.6 s `DueBatch.Impact` frame: same pattern recurred only inside stalled frames (frame 779: 70.8 ms Impact `Instantiate`) → environment/profiler stall, not combat code.
- [x] Radar duplicate-key error: absent; no errors/warnings during battle.
- First-use stall: `Battle.Radar.Apply` → `SoundManager.LoadFMODSound` sync clip load (frame 780, 75 ms under stall). Audio clip not preloaded → candidate follow-up.
- [x] Bug fixed (pre-existing since `8be9b0b7c`): scene unload → Zenject `Dispose` releases `RadarComponent`, then `Ship`/`Squadron`/`DefendPlatform.LateDispose` releases again → `KeyNotFoundException` in `RadarScanService.Unregister` per unit. `RadarComponent.Release` now idempotent; test `RadarComponent_ReleasedAfterDispose_DoesNotUnregisterTwice`. RadarScanServiceTests 8/8 pass.
- Unrelated teardown error not fixed: `CaptureSiteBuildUiController.LateDispose` → destroyed `CanvasGroup`.

### Fog visibility + minimap marker queries — 2026-10-12 (uncommitted)
- Cause (source review): both systems' per-unit cost was the vision query rather than the tick. `OwnedCircleSet.AnyContains` scanned every source (~ships + squadrons + stations + zones) and called `PlayerRoster.IsAllied` (2 dictionary lookups) per source per query.
- [x] `OwnedCircleSet` builds one snapshot per frame for each queried `(team, allied)`: `List<Vector3>` = (x, z, radius) of matching circles. Each query is now a plain distance loop over that team's circles only. `Register`/`Unregister` invalidate it. Shared by `VisionService` and `SpawnBlockerService`, so squadron icons, SFX and AI vision checks get faster too.
- Rejected: updating only units that moved. Visibility also changes when vision sources move. Per-unit batching left as a follow-up if the next capture still shows tick overhead.
- Tests: `VisionServiceTests` 8/8 (+2 same-frame invalidation), SpawnBlocker 5/5, FighterExplosionPool 3/3, ReinforcementSpawnRule 10/10, EnemyStructurePlacement 10/10, AuditSharedOperations 6/6. Full EditMode failures are the existing asset/data set.
- [ ] Re-capture: `FogVisibility LateTick` and `MiniMapUnitMarker LateTick` (were 1.6 ms each in capture 6).
- [x] `CaptureSitesSystem.Tick` (1.2 ms): per-site strength scan (all ships + squadrons) cached, refreshed every `STRENGTH_REFRESH_INTERVAL = 0.25 s`; capture/construction progress, vision and hover still tick every frame with `deltaTime`. Compiles; in-game re-measure pending.

### Target selection position reads — 2026-10-12 (uncommitted)
- Reported cost: target selection 2–3 ms/frame, mostly hardpoint position reads.
- [x] Fighter slots (target health `ShipClass.IsStrikecraft()`): `FighterView.Position` = pose cached in `ApplyPose` from `SquadronFlightModel` → capture reads it on the main thread for used slots; transform job skips them.
- [x] Ship slots: `TargetSlotPositionJob` reads stripe `slot % SHIP_POSITION_STRIPES (4)` per batch → ¼ of ship transforms/batch; positions ≤ 4 batches old.
- [x] Newly acquired ship slots read once directly (`_unreadSlots`); regrown position buffer → next job reads all ship slots (`_readAllSlots`).
- [x] Stale-position guard: `WeaponComponent.CommitTargetSelection` re-checks `HasFiringSolution(weapon, unit.Position)` live → else serial fallback (`FALLBACK_INVALID_CANDIDATE`). Same helper used by `FireTurnedSalvos`.
- Data: ship speed 14–48 u/s, rotation ≤ 60°/s → staleness affects only the choice, never fire legality.
- EditMode: `TargetSelectionCaptureTests` 12/12 (new: stripe cycle, first read, fighter pose). Weapon filter 106/117, Squadron 20/21; all failures are ship-data asset tests (Liberator hangar id, ISD-II hull, loadout bake, CD273 radar, Endurance PD, MC30c variant), unrelated.
- [ ] In-game check by user: TargetBatch Capture time, fighters still hit, no out-of-range shots.

### Profile 2026-10-12 16:15 + follow-up fixes
- Capture (editor, profiler on, 120 frames): median 50.2 ms; GPU 2.5 ms. Main-thread avg: TargetBatch.Capture 2.05, HardPointExplosionPool.Tick 1.87 (regression), ParticleSystem.Update 1.66, Squadron.UpdateOrder 1.49, FogOfWarSystem.Update 1.40, Projectile.Track 1.34, Squadron.Flight 1.23. AI ticks 2–6 ms on decision frames; HangarComponent.Tick 9 ms in one frame.
- [x] `e76b2857e`: hardpoint explosions — 2 looping child emitters never let `IsAlive(true)` end → 1008 live explosions, pool always instantiated. Anchor destroyed → `StopEmitting` → fades → idle.
- [x] `e76b2857e`: `SlowTickService` (1 s, spread across frames) for `MiniMapUnitMarkerPresenter` + `FogVisibilityComponent`; `MarkView` redraws on `MiniMapMarker.Changed`; minimap fog mask 1 s; `SquadronLaunchSlots` 1 launch / 0.1 s.
- [x] Target selection: markers `Capture.Owners` / `Capture.Copy` / `Capture.Positions` + `Battle.Weapon.RebuildCandidates`; per-hardpoint retry 0.25–0.5 s random after a failed selection (target change clears); new Base contact spliced after main target (same order as rebuild); destroyed groups dropped in bulk at ≥ half dead; position job starts after owner prep, completes after copies.
- [x] AI: `EnemyAiStagger` — first decision unchanged, then timer + golden-ratio fraction × interval per player (commander, production, abilities); world change → per-player delay 0.1–0.2 s instead of next frame; abilities: hostiles snapshot once per decision, 4 casters/frame.
- [x] Squadron order upkeep every 0.15 s, random phase, elapsed time passed to hunt/search timers; pilot + flight every frame.
- [x] Burst: `BoltFlightBatch` (tracking/landing main thread, `BoltPlacementJob` places bolts); `FighterPoseBatch` (`FighterPlacementJob` + `FighterBankJob`, LateTick order −2000 before coordinator −1000). Fighter kinematics math stays managed; spawn still places poses directly (trail clear).
- EditMode: 1542 total, 45 failures = same set as before these changes (asset/data, balance tool, `ShipEngineHardpointTests`).
- [ ] User re-profile: TargetBatch sub-markers, `Battle.Squadron.Placement`, `Battle.Projectile.*`, AI tick spikes, enemy fog reveal delay acceptable (≤ 1 s).

### Stress-audit fixes — 2026-10-12 (committed, awaiting battle re-capture)
- [x] AI ability crash: `EnemyShipAbilityController` keeps `(IEntity, IShipAbilityFacade)`; skips caster when `TryGetEntity` fails or destroyed (was `ShipAbilityFacade.Entity` → `GetEntity` throw). Test `CasterRemovedBeforeItsTurn_IsSkipped`.
- [x] Squadron markers: 244 world-space canvases moved/rotated/scaled each LateTick → ~294 `Canvas.BuildBatch`/frame. Now one `SquadronIconLayer` (ScreenSpaceCamera on `SceneCamera`, WorldUi layer → cinema mask still hides it); each squadron's `iconRoot` reparented at spawn, positioned in screen pixels, scale = zoom factor. Off-screen markers hidden before the vision query; `SetActive` only on change. 18 prefabs: `IconCanvas` Canvas removed. `ICameraService.SceneCamera` added. Test `SquadronIconPrefabTests`.
- [x] Bolt tracking: shield clip (`ResolveAimPoint` → `Shield.GetSurfacePosition`, one Burst `Run` per bolt) every `SURFACE_REFRESH_INTERVAL = 0.1 s` (random phase) + exact on landing; offset follows target between clips. Pivot read only during `LEAD_SETTLE_TIME`.
- [x] Fog: `FogVisibilityGridModel.QueueReveal` + `ApplyReveals` → one row-parallel `FogRevealJob` (`IJobParallelFor`, batch 16 rows) instead of one job per source. Used by `FogOfWarSystem` and `MiniMapFogMask`. Test `QueuedReveals_MatchSeparateReveals`.
- [x] Markers: `Battle.Enemy.Build` around enemy production callback (attributes `TimerPoolTick` spikes); `Battle.Ship.State.<StateType>` per ship state Tick.
- EditMode: 1562 total, 1515 pass, 47 fail — all asset/data/balance/weapon-rule/UI-prefab tests outside this change; new and touched tests pass.
- [ ] User battle re-capture: `Canvas.BuildBatch` calls, UI batching, `TransformChangeSystem`, `Battle.Projectile.Track`, `FogOfWarSystem.Update`, `Battle.Enemy.Build` vs `TimerPoolTick` spikes, top `Battle.Ship.State.*`. Visual check: markers follow squadrons, click/selection, hidden in cinema camera, bolts land on shields.
- [x] Battle check 2026-10-12 22:00 Kyiv: marker frame missing → `SquadronFrame.shader` derived border from canvas-space vertices (only valid with one canvas per marker). Now reads centre offsets from UV1 written by `SquadronFrameMeshEffect` (18 prefabs); offscreen render verified. `1319d5ebf`.
- [x] Same check: AI idle = `Hold`. Causes: `AssaultBase` fallback was DestroyOpponentBase-only (DestroyEnemyFleet also needs base + all units destroyed) → fallback now both objectives; with 168 player ships AI at pop cap 60/60 and FleetAdvantage 0.49 → holds by existing rules (design question, unchanged).
- [x] AI idle vs large player fleets (not an optimization regression): `Hold` sent Stop every 0.5 s. New states `EngageLocal` (0.55; local threat and LocalAdvantage ≥ RetreatAdvantage → attack closest local hostile, ships first) and `RaidStructures` (0.1; seen hostile non-base structure with no known ships within 1000 u). Cheat: level up one/all enemy stations. `ReinforcementData.MaxUnitCapacity` 60 → 500 **temporary for stress tests — revert after**. `033ee831c`.
- [ ] If `Battle.Enemy.Build` ≈ timer spikes → enemy ship pooling plan (like [[TODOs/Optimization/Squadron_Pooling_Plan]]).
- [ ] `com.script-utils.time` (git package, GolinSky/ScriptUtilities): `TimerPoolService.Update` removes finished coroutines inside a forward loop → next coroutine skipped one frame. Fix upstream (iterate backwards); cannot edit in PackageCache.

### Radar acceptance and capture reliability
- [ ] Obtain radar owner's implementation/test evidence; validate 1×/4× scheduling, detection delay, cloak/hostility, destruction, range modifiers and obstacle contacts.
- [ ] Retain historical radar diagnosis in research; current stages average Snapshot 2.189 ms, Job 0.751 ms, Apply 5.392 ms.
- [ ] Repair/reconfigure bounded Frame Debugger export so a complete current battle frame saves; 15:25 capture timed out at event 799 with zero saved frames.
- [ ] Investigate unreadable Rendering Debugger camera selector separately; partial panel export is not gameplay failure.
- [ ] Final acceptance: matched sustained and spike metrics, no new errors, full relevant behavior checks; confirm major gains in a Development Player.

## Files
- [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research]]
- `Assets/Scripts/Services/Timing/BattlePerformanceCapture.cs`
- `Assets/Scripts/Editor/RenderAuditProfilerTool.cs`
- `Assets/Scripts/Editor/RenderAuditFrameDebuggerTool.cs`
- `Assets/Scripts/Services/Radar/RadarScanService.cs`
- `Assets/Scripts/Services/ShipNavigation/ShipNavigationService.cs`
- `Assets/Scripts/Entities/Ship/Ship.cs`
- `Assets/Scripts/Components/Ship/Audio/AudioShipComponent.cs`
- `Assets/Scripts/Services/Audio/ShipSfxService.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`
- `Assets/Scripts/Components/Weapon/TargetSelectionBatch.cs`
- `Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs`
- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Services/ShipNavigation/ShipPathGrid.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotEffectPool.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/BoltShot.cs`
