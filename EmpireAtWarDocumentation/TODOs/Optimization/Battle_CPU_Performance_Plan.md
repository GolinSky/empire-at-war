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
- Capture 2026-10-12 17:03 (after `7d9333dd6`; editor, 120 frames, t=461 s, 129 squadrons; not matched: draws 886 → 2335, tris 1.7 → 5.4 M): median 50.2 → 36.2 ms, p90 77 → 47, max 301 → 141; GPU 3.7 ms.
  - Avg/frame before → after: explosion pool 1.87 → 0.37; ParticleSystem.Update 1.66 → 0.51; Squadron.Flight 1.23 → 0.61; projectile flight ≈3.0 (Track+Move+Effect) → ≈1.8 (Track 1.27 + ShotEffect.Update 0.54); MiniMapFogMask 0.63 → 0.04; fog/minimap marker ticks gone from top list.
  - TargetBatch ≈ 2.1 → ≈ 1.8: Owners 1.23 (prepare: eligibility/wreck lookups), Apply 0.31, Positions 0.13, Copy 0.10, RebuildCandidates 0.08 → next target = `PrepareTargetSelection`.
  - Bug: Squadron.UpdateOrder synced — all 129 squadrons every 4–5 frames, 5–10 ms (avg 1.67 vs 1.49 before). `_orderTimer = INTERVAL` after a > 0.15 s frame aligns every phase. Cost is the target searches, not the checks.
  - Bug: explosion pool still leaks (live: 146 legit, 49 on restored/respawned hardpoints, 34 on deactivated units). Stop condition must also cover anchor inactive and hardpoint health > 0.
  - Up, cause unknown (workload differs): `Battle.Ship.OrderTick` 0.48 → 1.29; `CanvasUpdate.PreRender` spikes 12.8 ms.

### CPU follow-up implementation — 2026-10-12
- [x] Squadron order timer retains the interval remainder after long frames; missed intervals produce one upkeep call with the full elapsed time, preserving each squadron's phase.
- [x] Hardpoint repair and `OnDisable` stop their active fires immediately; pool Tick also retires destroyed/inactive anchors. Existing particles fade at their last position before reuse; same-frame repair/despawn/respawn cannot revive the old fire.
- [x] Weapon preparation removes the per-owner eligibility dictionary. One shared entity lookup returns acquisition + wreck state; health/fog/cloak reads stay batch-scoped. Live groups only query the rebuild set when a wreck is observed.
- [x] Squadron target searches reject out-of-radius/non-winning candidates before cloak/fog queries; class preference, candidate ordering and local/AI fog rules remain unchanged.
- Verification: **92/92 focused EditMode tests passed** — weapons 68, explosion pool/lifecycle 12, squadron order timing 3, squadron targeting 9. Unity compile clean; Console 0 errors/warnings; `MainMenuScene` clean; new test scripts/metas imported. Evidence: `Logs/BattleFixes/20261012-followup/{weapons,explosions,order-timing,squadron-targeting}.json`.
- Regression: 129 evenly phased squadrons retain 25–27 checks per 30 ms frame after 0.16/0.6/2.01 s hitches. Lifecycle test invokes `OnDisable` directly because EditMode does not dispatch ordinary runtime callbacks; inactive-anchor pool fallback also passes.
- [x] Observational recapture — 2026-10-12 20:37 Kyiv / 17:37 UTC, 120 frames: orders average/peak **1.675/10.188 → 0.604/1.434 ms**; >5 ms frames **27 → 0**. New checks spread **12–73/frame**, median 29, with **147–148 squadrons** (previous 128–129).
- [x] Target owner preparation self **1.226 → 0.668 ms/frame** (−46%, marker present in all frames); full new TargetBatch **1.742 ms** inclusive. No old-window target/request counts → not workload-normalized.
- [x] Fire audit before/after: **113/209 active**, all on active destroyed hardpoints; **0 healthy / 0 inactive / 0 missing anchors** in both snapshots (previous 49 healthy + 34 inactive). After: 9 fading. Pool self ≈0.372 → 0.280 ms/frame; no stale fire observed.
- Frame duration median/p90/worst, recomputed consistently: **36.10/44.60/140.61 → 33.76/39.46/66.72 ms**. GPU median 3.65 → 5.23 ms. Draw/triangle medians 2334.5/5.419 M → 2543.5/6.266 M; camera moved between surrounding snapshots and render counts varied sharply → observational comparison only.
- Supporting 358-frame CSV: first frame has one 5.58 ms / 143-check catch-up, then checks spread again; no recurring sync. Source `8808c03d6` + dirty fix diff; domain current; Play Mode uninterrupted; 0 Console errors; two warnings predate capture. [Evidence](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T173609Z-followup/analysis.md>).
- [ ] Matched fixed-camera workload and Development Player acceptance; longer repair/respawn lifecycle coverage. Snapshot cleanup and the 120-frame distribution pass, but the broader plan remains active.
- [ ] Ship orders 0.48 → 1.29 ms and UI 12.8 ms spike remain unattributed; compare equivalent workload before changing those systems.

### Minimap-disabled battle audit — 2026-10-12 21:09 Kyiv
- [x] Read-only live audit of `2bf5cf847` + existing dirty fixes: 120 raw Profiler frames and two 10 s CSV windows; battle left playing, no setting/source/asset changes, 0 game errors. One legacy MCP timeout warning during export. [Evidence](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T180933Z-minimap-disabled/analysis.md>)
- Initial CSV frame median **49.761 ms / 20.1 FPS**, later recheck **26.360 ms / 37.9 FPS** with squadron minimap binding still disabled. Target candidates **19,772 → 7,492/frame**; projectile creations **341 → 124 / 10 s**. These are different workloads, not a controlled marker A/B.
- Profiler CPU/GPU median **31.139 / 2.328 ms**; frame median **34.779 ms** vs prior 33.760; draw median **1,051** vs 2,543.5. ShotEffect updates **2,040/frame** vs 948; projectile tracking self **1.874 ms** vs 1.310; TransformChangeSystem **1.040 ms** vs 0.543.
- Target owner preparation improves **0.668 → 0.419 self ms/frame**; full target batch **1.742 → 1.617 inclusive ms**. Squadron orders remain spread, **26–71 checks/frame**, peak **1.552 ms**; both fire snapshots have 0 stale anchors. Earlier timer spikes absent in this 120-frame sample.
- [ ] Prioritize projectile pool misses and impact bursts; initial CSV instantiation **1.826 ms/frame**, max **31.881 ms**. UGUI batching inclusive **2.972 ms/frame** and fog self **1.148 mean / 3.545 max ms** remain; do not add nested scopes.
- [ ] Attribute **72.4 MB / 778.534 ms** CSV hitch; source unknown. It precedes the 120-frame window; rolling-buffer lookup was too late, saved binary retained. Distinguish Editor/tooling from gameplay before proposing a fix.

### Increased-unit stress capture — 2026-10-12 21:20 Kyiv
- [x] Initial **212 ships /244 squadrons /480 weapons**; CSV median **53.419 ms /18.7 FPS**, p95 **68.331 ms**, CPU/GPU medians **47.606/2.588 ms**. **126/183 frames >50 ms**. Same minimap-disabled source; camera and live population vary. [Stress capture report](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T181937Z-stress/analysis.md>)
- 120-frame workload means: **199.68 ship order calls /216.21 squadron flights /441.01 weapon ticks**; prior **125.71/156.58/307.11**. ShotEffect updates **2,554 vs 2,040/frame**, Canvas batches **294 vs 161/frame**.
- Scoped costs: UI batching inclusive **5.691 ms** (was 2.972), projectile tracking self **3.628** (1.874), transforms self **2.519** (1.040), fog self **2.043** (1.148), full targeting inclusive **2.749** (1.617). Parents/children overlap; no complete CPU hierarchy or algorithmic scaling claim.
- [ ] Attribute timer callbacks: four **14.38–20.42 ms** spikes, each ~12 ms self; scheduled production is a known consumer but exact callbacks are unconfirmed. CSV ship orders peak **11.16 ms**; navigation max **2.63 ms**.
- [ ] Fix removed-entity AI ability caster lifetime: runtime stack `EntityLocator.GetEntity → ShipAbilityFacade.Entity → EnemyShipAbilityController.UseNextCasters:90`. Later Lucrehulk spawn-position failure/refund occurred outside timed windows.
- No repeated order lockstep: one 218-check catch-up after **80.14 ms EditorLoop**, then spread resumes; upkeep max **2.855 ms**. Both fire snapshots have 0 stale anchors. CSV allocations max **437 KB**, earlier 72.4 MB outlier absent.
- Capture limitation: full export filled F:, binary incomplete; moved only this task's partial **2.46 GB** file to C:. All selected scopes recovered from **120 valid live frames**; later full-summary recovery expired. Gameplay CSV complete; no game changes, no Play Mode transition. Keep matched Player acceptance open.

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
- [x] Stress capture 2026-10-12 22:46 Kyiv (500 pop; 282 ships / 139 squadrons; not matched): median 80.4 ms, GPU 8.1. TargetBatch 7.39 (max 25), UGUI 4.84 (246 selected-unit `SelectedCanvas` world canvases), Radar 4.58, Squadron.UpdateOrder 3.49, Projectile.Track 2.30 (per bolt ≈ −45%), Fog 1.23, TransformChangeSystem 1.75. Timer spikes = enemy spawns (`TimerPoolTick` 15.78 ≈ `Battle.Enemy.Build` 15.76). [Report](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T194652Z-stress-ai/analysis.md>)
- [x] Selection rings → `SpriteRenderer` (76 prefabs); facing solver sweep; main-target switch reorders candidates; sticky AI local/raid targets; shot pools keep peak + background `ShotPoolPrewarmer` (0.5 ms/frame). `72c9d8312`.
- [x] Capture 2026-10-12 23:29 Kyiv (409 ships / 207 squadrons; not matched): FacingSolve max 9.14 → 1.55; RebuildCandidates max 19.5 → 0.18; Canvas.BuildBatch 117 → 2.8 calls; UGUI 4.84 → 1.85; TransformChangeSystem 1.75 → 0.46 ms. New: `AttackMoveState` 120.8 ms/frame avg, two 6.4 s frames (257-ship attack-move; `AttackMoveEngagement` members × contacts × claims) → per-enemy reporter/attacker counts; `IdleState` 20.9 ms → search every 0.25 s. `c22653fd5`. [Report](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T202908Z-stress-fixes/analysis.md>)
- [x] Re-capture after `c22653fd5` (2026-10-12 23:38 Kyiv; 280 ships / 122 squadrons; not matched): median 54.5 ms, max 88. 178-ship attack-move 4.69 ms/frame median (0.026 ms/ship), 28.5 ms on order frame; `IdleState` max 20.9 → 3.0 ms. [Report](<F:/Private/empire-at-war/Logs/BattleAudit/20261012T203856Z-idle-check/analysis.md>)
- [x] Attack-move order frame (28 ms / 178 ships): `AttackMoveState` searches every 0.25 s with random first delay; completes only after a search found nothing. `adbca6b3c`. In-game re-measure pending.
- [ ] Projectile misses at 400+ ships; AI raid scan cost; radar + squadron upkeep at 400+ ships; ship pooling ([[TODOs/Optimization/Ship_Pooling_Plan]]).
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
