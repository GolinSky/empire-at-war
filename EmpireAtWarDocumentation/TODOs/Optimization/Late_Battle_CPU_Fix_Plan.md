---
category: Optimization
status: in-progress
created: 2026-10-30
updated: 2026-10-30
---
# Late-battle CPU fix plan (Mac M2)

## Goal
- Fix the late-match CPU collapse: 2250 s battle, 83 ships / 321 squadrons / 454 weapons → median 264.5 ms (3.8 FPS), p95 411.9 ms. GPU only ~18 ms → CPU bound.
- Evidence: `Docs/Audits/2026-10-29-Live-Battle-CPU/analysis.md` (commit `5986d1aa`). Parent scope: [[TODOs/Optimization/Battle_CPU_Performance_Plan]].

## Findings (ranked, source-verified 2026-10-30)
1. **Shot prewarm Instantiate** — `ShotPoolPrewarmer.Tick` → 91 `Instantiate.Awake` / 2452 ms = **27 ms/frame**, ~27 ms per call (on-demand creates ~3 ms). 0.5 ms budget only checked *between* creations.
   - Pools are per hardpoint (`WeaponHardPoint.GetPool`); a released pool destroys its shots → every reinforced ship re-instantiates `ExpectedShotsInFlight` shots. Cost grows with reinforcements late in match.
2. **Squadron orders** — `Battle.Squadron.UpdateOrder` 16.7–19.5 ms/frame, 320 calls/frame. `ORDER_UPDATE_INTERVAL = 0.15 s` < frame time → every squadron runs every frame (death spiral: slower frame → no amortization). Each call does `SelectNear(_radar.Enemies, …)`.
3. **Capture-site Tick** — mean 12.6 ms, max 283 ms. `CaptureStrengthSampler` `REFRESH_INTERVAL = 0.25 s` → also every frame at 4 FPS; each area iterates all ships + squadrons. Spike descendant unknown.
4. **Target snapshot Copy** — 10–15.9 ms/frame. Per request (~2831/frame): `HardPoint.transform.position` + `parent.rotation` native reads + dictionary lookup (`TargetSelectionBatch.cs:201–235`).
5. **Radar** — scan 10.7 ms, snapshot 6.7 ms mean.
6. **Selection marker (user-reported)** — `SelectionComponent.LateUpdate` on every unit (~420: 95 prefabs incl. 18 squadron views). Selected + turning units read world `rotation` and write it every frame → transform-change dispatch + SpriteRenderer bounds per marker. Mass select of squadrons = hundreds of writes/frame; PC lost ~10 FPS.
   - `SelectionService.NotifyObservers` calls `UpdateState` on **all** ~420 components on every selection change and every selected-unit death → `localScale` write on unselected units too.
   - `SelectionContext.Replace` uses list `Contains` → O(N²) on big marquee.
7. **Squadron icon layer** — 321 `SquadronIconComponent.LateTick`: `WorldToViewportPoint`, 4× `Screen.width/height`, vision query, `anchoredPosition` write → shared icon canvas rebatches every frame (`UGUI.Rendering.UpdateBatches` up to 1.7 ms).
8. **Allocations** — 506 KB/frame mean, 5.6 MB spikes every ~20 frames:
   - `BattleTelemetry.Flush` (Editor/Dev only): every 10 s rebuilds whole CSV via `string.Format` + sync `File.WriteAllText`; rows grow with unit/target pairs → ≥1.38 MB per flush.
   - `HazardService.RefreshMemberships` 464 KB / 11,581 calls (40 B each) — likely boxed enumerator / facade lookup.
   - Enemy AI: `GetIonCost` 194 KB, `EnemyBuildQueue.CanAdd` 180 KB, context `Build` 123 KB; `ResearchHealthData.Shields` 141 KB; `CaptureSitesSystem.HasHostileUnits` 82 KB; `WorldTooltipPresenter.Tick` 32 KB.
   - Debug-only: `EnemyEconomyDebugView.OnGUI` 99 KB (IMGUI) — ensure off in normal play.
9. Editor overhead (EditorLoop ~60 ms) inflates numbers → confirm wins in a Development Player.

## Decision — selection marker (zero per-frame CPU)
- **Chosen:** keep `SpriteRenderer` child; make the marker shader **world-aligned**: vertex shader rebuilds the quad from object origin + per-axis scale of `unity_ObjectToWorld`, ignoring parent rotation. Delete `LateUpdate` rotation sync.
  - Why: no per-frame code, no transform writes, works for any count; position follows parent for free.
  - Attack feedback (spin + lerp to target, 0.9 s) keeps its CPU path only while `_attackSequence != null`; spin angle via shader property (MaterialPropertyBlock on that one renderer).
  - Check: SRP Batcher / dynamic batching must not pre-bake vertices to world space (dynamic batching off for this material); enlarge bounds if pitched parents clip culling.
- **Fallback:** `SelectionMarkerService` — registry of *visible* markers only, one `LateTick`, `TransformAccessArray` + Burst `IJobParallelForTransform` setting world rotation. No per-unit `LateUpdate`.
- Avoid: per-unit `LateUpdate`; world-space canvas per unit; reading `transform.rotation` every frame.

## Implementation
### Phase 1 — Selection (user-reported, quick win)
- [x] Existing `SelectionBrackets.shader` now world-aligns the sprite; saved `SharedSelectionData.SelectionMaterial` binding verified. `SelectionComponent` is disabled outside attack feedback.
- [x] Attack feedback enables the component only during its sequence; reused `MaterialPropertyBlock` supplies shader spin.
- [x] Only selected markers observe selection counts; cached 1↔2 scale group suppresses redundant writes. Conservative bounds cover world alignment and spin.
- [x] `SelectionContext.Replace` uses HashSet membership; shared cached death callback replaces per-entity closures.
- [ ] Verify on Mac/Metal and PC: select many units (~400); markers retain each unit's position/orientation and stay visible while turning/pitched. Play one attack animation; check spin/target feedback and no steady per-frame `SelectionComponent` Profiler cost.

### Phase 2 — Shot pools
- [x] Added per-prefab `Battle.Projectile.Prewarm.<prefab>` / `Battle.Projectile.Instantiate.<prefab>` markers and loading target-count logs.
- [ ] Reproduce and identify the original M2 ~27 ms prewarm cost before repairing native components. Windows crowded real-Tick capture measured 72 creations / 6 prefabs; maximum Awake 0.2357 ms (`QuadProjectile`). Seven-prefab live probe maximum total Instantiate 0.7205 ms. No 27 ms offender reproduced or speculative prefab change made.
- [x] Battle-scoped `ShotEffectCache` stores shots per prefab; mount leases retain profile/callback isolation. Released-owner shots finish and return for reuse; battle teardown cancels all remaining effects.
- [x] Loading warms the maximum per-prefab demand of one roster ship/squadron/platform; live station profiles request exact demand before fade-out. `PrewarmRosterAsync` queues prefab targets and drains through `CompletePrewarmAsync` with a shared 4 ms creation budget per frame; yields no longer occur after every shot. Runtime growth remains ≤1 creation/frame with the existing 0.5 ms budget; an individual Instantiate remains non-preemptible.
- Shared demand beyond roster/live-station targets still creates shots on demand. Per-mount targets below the shared cache target add no runtime work; targets are not aggregate simultaneous-fleet demand.

### Phase 3 — Fixed-frequency AI/order work
- [x] `SquadronOrderScheduler`: ≤64 visits / ≤32 updates per frame, 0.15 s minimum interval; elapsed time retained, no catch-up burst. Pilot/flight and explicit commands remain immediate.
- [x] `SquadronTargetIndex`: one shared 500-unit-cell snapshot/frame; hostility filtered per viewer. O(1) radar membership, radar/locator tie order, exact distance, cloak and vision filters retained.
- [x] Capture strength: ordinary ticks retain ≤4 area visits / ≤2 refreshes per frame, 0.25 s minimum interval. `Add` and `Invalidate` request a full refresh on the next tick, bypassing deadlines/caps before all sites/zones advance capture; `Clear` resets the request. Added strength ship/squadron/build and site model/render/visibility markers.
- [x] Traced current crowded capture cost: maximum Tick 1.9953 ms, including 1.6101 ms squadron strength sampling. Geometry snapshots per due scan reduced paired scan microbenchmark 185.2264 → 107.3877 ms (42.0%), identical tallies; post-fix live maximum 1.2845 ms under a different evolving workload.
- [x] Crowded play exposed destroyed target views between scheduled order checks; `Squadron.Tick` now validates target lifetime before pilot work. Follow-up crowd capture logged 0 errors; unrun Attack/Guard/Hunt regression added.
- [ ] Attribute the historical 283 ms M2 capture-site spike. Frame 535 was 283.3207 ms uninstrumented self time; old native recording is absent locally. Current PC trace does not establish that cause.

### Phase 4 — Target snapshot + radar
- [x] `TargetOriginCapture` registers transforms once per mount, removes owner slots on release, and runs `TargetOriginReadJob`. Parent rotation = world × inverse(local), including identity for unparented mounts; no native transform reads in the per-request copy loop.
- [x] Captured owner spans and request origin slots use cached indices; copies no longer repeat owner dictionary lookups. Copy timing remains a measurement gate.

### Phase 5 — Squadron icons
- [x] One `SquadronIconLayer.LateTick` caches screen/camera matrices/zoom, centrally projects registered icons, and writes positions only after >0.5 px movement. Existing dedicated icon canvas already isolates moving markers from HUD; no redundant sub-canvas added.

### Phase 6 — Allocations
- [x] `BattleTelemetry` flushes only when its tracked battle scene unloads or the app quits. Session starts at the first recorded combat event in the active battle scene; loading/auxiliary scene unloads do not flush it.
- [x] Hazard per-entity closure moved into a subscription helper: allocation occurs only on a new membership. Live entity enumeration is already a struct and facade lookup already indexed; no speculative facade cache added.
- [x] Indexed hazard/research iteration; build-queue explicit HashSet enumeration; per-player AI ship/receiver/formation buffers; direct fleet-center reduction; cached capture-site/hostility delegates. Builder collections are borrowed until its next synchronous Build; consumers do not retain them.
- [x] `EconomyModule` creates the Editor/Development debug view disabled; enable the component explicitly for debugging.

### Phase 7 — Acceptance
- [ ] Matched late-match capture (same seed `1875278133`, Full map), Editor + Development Player, Mac M2; compare against 264.5 ms median.

## Files
- `Assets/Scripts/Components/Selection/SelectionComponent.cs`, `Services/Selection/SelectionService.cs`, `Services/Selection/SelectionContext.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotPoolPrewarmer.cs`, `ShotEffectPool.cs`, `Components/ViewComponents/Health/WeaponHardPoint.cs`
- `Assets/Scripts/Entities/Squadron/Squadron.cs`, `Entities/Capture/CaptureStrengthSampler.cs`, `Services/CaptureSites/CaptureSitesSystem.cs`
- `Assets/Scripts/Components/Weapon/TargetSelectionBatch.cs`, `Components/Weapon/BattleTelemetry.cs`, `Services/Hazards/HazardService.cs`
- `Assets/Scripts/Components/Squadron/Icon/SquadronIconComponent.cs`

## Tests (2026-10-30)

- User explicitly authorized execution. Remaining categories: 1,093 passing executions / 1,078 distinct test names; no failures or skips in final runs. Full EditMode suite and explicit map-generation sweeps were not run.

| Category | Passed / total | Run |
|---|---:|---|
| BattleInput | 206/206 | Final rerun |
| Squadrons | 81/81 | This follow-up |
| EnemyAi | 193/193 | This follow-up |
| Map | 37/37 | Final rerun |
| Ui | 224/224 | Final rerun |
| Vision | 61/61 | This follow-up |
| Navigation | 164/164 | This follow-up |
| Core | 127/127 | Final rerun |
| Combat | 220/220 | Earlier shader-warmup follow-up |
| Capture | 101/101 | Earlier shader-warmup follow-up |
| GameFlow | 153/153 | Earlier shader-warmup follow-up; repeated after startup fix |

- Evidence: `Logs/Tests/20261030-LateBattleCPU/summary.json`; per-category initial and final reports retained beside it.
- Historical “tests not run” entries below describe their original checkpoints; current results are above.

## Implementation — 2026-10-30
- Unity `6000.4.7f1`, Windows checkout: scripts imported, compilation finished with `scriptCompilationFailed = false`; Console returned 0 errors.
- `SelectionBrackets.shader`: 0 shader messages, supported on the connected Editor; saved material points to it and reports `DisableBatching = true`.
- New script/test `.meta` files generated by Unity. `git diff --check -- Assets` passed. No Play Mode, automated tests, Player build or performance capture run.
- Added/updated unrun regressions: marker behavior, shared shot lifecycle, capped order timing, spatial/radar targeting, target-origin pose/swapback/reuse. Categories: `BattleInput`, `Combat`, `Squadrons`, `Capture`, `EnemyAi`, `Map`, `Ui`, `Vision`, `Navigation`, `Core`, `GameFlow`; none ran.
- Independent source review covered pool teardown, target-job completion/slot remapping, scheduler lifecycle/ties, and synchronous AI buffer use.
- Reusable rules extracted to [[Rules/BATTLE_PERFORMANCE_GUIDELINES#CPU scheduling and snapshots]].

## TODO
- Remaining implementation investigation: expensive shot prefab identification/repair and capture-spike attribution. Keep this plan active; instrumentation does not establish those fixes.
- Runtime gates: select-all marker orientation/culling/attack feedback, cross-owner shot colors/impact/teardown, loading duration, order response/capture latency, icon projection/visibility/clicks and telemetry scene-exit output.
- Matched acceptance: same Full-map seed `1875278133`, comparable late-match populations/orders/camera/settings, M2 Editor + Development Player; report median/p95/p99, Copy time, instantiate identity/cost and allocation rate. This Windows Editor check does not establish M2 speedup.
- Accepted overload tradeoff: 321 due squadrons at ≤32 updates/frame need at least 11 frames per full pass (~0.37 s at 30 FPS); ordinary capped capture sampling retains cached strength between refreshes. Registration/restore invalidation refreshes all areas together.
- Residual selection cost: `SelectionService.AddObserver` uses list `Contains`; `RemoveObserver` uses list `Remove`. Repeated large-selection subscription changes remain O(N²) (~400² = 160,000 comparison scale); this is a complexity estimate, not a measured timing. No observer-container change in this follow-up.

## Profiling follow-up — 2026-10-30
- User authorized real crowded battle + station cheats/enemy money + 60 s settling. Full/Small seed `1875278133`, Republic human vs UltraHard Separatist, level 5 both; direct stress spawns adopted through normal accounting.
- Pre-fix probe census 73 ships / 464 squadrons / 544 weapons; validation census 54 / 459 / 519 after >60 s. Cinematic/D3D11/2560×1440/nebula 1 on Ryzen 7 9800X3D; original M2 was Performant/Metal/nebula 2. These are not matched frame-rate comparisons.
- Evidence, decoders and reproduction commands: `Docs/Audits/2026-10-30-Late-Battle-CPU/analysis.md`; `Tools/Performance/README.md`. Portable raw-descendant exports retained; native recordings remain in Logs.
- Isolated seven-prefab, native ablation, independent transform-fence and stopped-population controls did not reproduce 27 ms. Real ordered prewarm maximum Awake 0.2357 ms; no prefab identity for the old M2 event established.
- Capture geometry is refreshed once per due area, not frozen for the site's lifetime. Generic callback samplers retain their old behavior. Added unrun `CaptureStrengthSamplerTests`.
- New fixes imported/compiled; fresh crowded validation Console = 0 errors. Final Editor stopped, profiler disabled, clean saved `MainMenuScene`; no scene/prefab/font mutations.
- Tests not run (`Capture`, `Squadrons`), per local override. Source and paired diagnostic parity checks are not automated-test results.
- Reusable snapshot/lifetime rules updated in [[Rules/BATTLE_PERFORMANCE_GUIDELINES#CPU scheduling and snapshots]]. Plan stays active for the unreproduced historical M2 causes and matched acceptance.

## Implementation — review fixes 2026-10-30
- Loading shot creation now shares a 4 ms budget across all prefab targets; cancellation is checked per iteration and frame wait. One expensive Awake can exceed the budget; runtime caps are unchanged.
- Capture registration/invalidation refreshes all areas before the next site's/zone's capture update. Two added `Capture` regressions cover five initial areas, invalidation while fresh, resumed 2/2/1 refreshes and adding a sixth area while existing samples are fresh; tests not run.
- Unity imported/compiled the three changed scripts; `isCompiling = false`, `isUpdating = false`, `scriptCompilationFailed = false`; `git diff --check` passed. Console retains 28 earlier shader-warmup keyword-space errors from 10:18 UTC; no new errors after refresh (cursor 106).
- Independent source review found no actionable loading-queue/cancellation/runtime-cap issue. No Play Mode, performance capture, Player build or automated tests run for this follow-up.
- Selection batching/origin/culling and attack animation checks remain open on Mac/Metal and PC. Historical M2 prewarm/capture-spike investigations keep the overall plan active.
- Reusable rules updated in [[Rules/BATTLE_PERFORMANCE_GUIDELINES#CPU scheduling and snapshots]].
- Follow-up 2026-10-30: earlier shader-warmup Console errors were reproduced and resolved in [[Done/Bugs/Shader_Warmup_Keyword_Errors]]. `GameFlow` 153/153, `Combat` 220/220 and `Capture` 101/101 now passed; `GameFlow` repeated after the startup fix. Fresh/restored Windows Play Mode startup produced 0 new errors. Historical M2 investigations and marker acceptance remain open.

## Implementation — remaining tests 2026-10-30
- Fixed two marker test cases: `Material.GetTag("DisableBatching", false)` returns `true` on this Editor; assertion now compares case-insensitively. Shader/material and runtime marker code unchanged.
- Initial `Map` failure caught a transient `UnitHighlightSettings.Color` compiler error while unrelated highlight source/tests changed in the shared checkout. Compilation recovered; `Map` rerun 37/37 passed. This task did not modify highlight work.
- Initial `Core` failure caught an unrelated Pipeline `/api/exec` main-thread timeout (5,000 ms). `Core` rerun 127/127 passed without restore-code changes.
- Initial spinner timing assertion returned 0° rotation. Source uses `Time.unscaledDeltaTime`; full `Ui` rerun passed 224/224 unchanged. Shared Editor activity may have affected timing; no spinner production/test change made.
- Final Editor: stopped, clean saved `MainMenuScene`, compilation/update idle, `scriptCompilationFailed=false`; changed-test `git diff --check` passed.
- No new production guidance: only a test assertion changed. Historical M2 root-cause investigations and visual/performance acceptance remain open; plan stays active.
