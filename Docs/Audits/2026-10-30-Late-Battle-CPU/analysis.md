# Late-battle CPU follow-up — 2026-10-30

## Outcome

- Fixed measured capture-strength overhead: snapshot each area's current center/radius once per due scan, instead of rereading its Unity transform for every unit. Generic predicate-based sampling remains supported.
- Fixed a target-lifetime exception exposed by crowded play: `Squadron.Tick` now retires dead order/engagement targets before `SquadronPilot` reads cached hardpoint transforms. Target searches remain budgeted; the lifetime check is constant work.
- No shot prefab reproduced the historical M2 ~27 ms creation cost on this Windows host. No prefab components or shot scheduling were changed without evidence.
- The historical 283 ms capture-site event is not attributed to a specific operation. Its retained summary has almost no instrumented child time, and the native M2 recording is absent from this checkout.

## Setup

- Revision: `5986d1aac079ae124e1a39bb57f939848ad1ec6f` plus the uncommitted CPU implementation and this follow-up. This is not a clean historical-build comparison.
- Unity `6000.4.7f1`, Windows 11, Ryzen 7 9800X3D; Editor. Original evidence is Apple M2/Metal. No cross-hardware speedup is claimed.
- Current graphics: Direct3D11, Cinematic, 2560×1440, nebula variant 1; original M2 audit was Performant/Metal/variant 2. Graphics preferences were preserved, so these are different rendering conditions.
- User authorized a crowded battle, level-5 stations, enemy credits, and waiting 60 seconds. Normal Full skirmish: seed `1875278133`, Small map, Republic human / Separatist UltraHard AI.
- Used real station cheats for both upgrades and added 1,000,000 credits per side. Human cheat fleet used 120 population. Additional enemy stress units used existing factory/spawner plus `EnemyFactionController.AdoptShip` / `AdoptSquadron` for capacity and death tracking; this deliberate stress setup exceeds the ordinary enemy capacity limit.
- First crowd setup reached 77 ships / 320 squadrons / 402 weapon components. At the crowded probe it was 73 / 464 / 544 because carriers kept launching and combat changed the population.
- First battle used DestroyEnemyFleet and eventually ended. Later ended-battle captures are excluded from combat conclusions. The validation restart uses DestroyOpponentBase so the diagnostic window is not cut short by a fleet loss.
- Diagnostic sources live outside `Assets` and run through official `unity command run_script`. Temporary native-component experiments use disposable preview scenes; user scenes remain untouched.

## Capture-site attribution

| Recording | Largest capture Tick | Attribution |
|---|---:|---|
| Original M2, frame 535 | 283.3271 ms | 283.3207 ms self; only 0.006338 ms instrumented children |
| Available older PC raw recording, frame 243 | 2.1854 ms | 2.1730 ms self; GC.Alloc + Deactivate account for only 0.0124 ms |
| Smaller current battle, frame 1 | 0.5330 ms | Ships 0.2192 ms; squadrons 0.1281 ms; visibility 0.0990 ms |
| Crowded current battle, frame 304 | 1.9953 ms | Squadrons 1.6101 ms; ships 0.1648 ms; visibility 0.1799 ms |
| Post-fix crowded restart, frame 11 | 1.2845 ms | Squadrons 0.9657 ms; ships 0.0461 ms; visibility 0.1578 ms |

- Crowded raw frame range is **302–483**, not 0–181. The profiler buffer retained the earlier capture. `capture-site-live-crowded.json` contains the correct range.
- `Site.Model` encloses capture events and their announcement callback. The current maximum is strength sampling, not rendering, capture-model transitions or a large observed GC operation.
- Original summary is top-40 marker-ID totals, not a sample tree. Inclusive minus self does not reconstruct missing managed/native work or establish GC/OS-suspension causality.
- Decoder: `CaptureSiteRawDecoder.Run(outputPath, firstFrame, lastFrame)` reads an already-loaded native recording and exports the maximum Tick's full descendants, direct children, self totals and allocation metadata.
- Native recordings remain under `Logs/RenderAudit`; portable decoded data is beside this report. Raw files are not required to inspect the exported maximum sample trees.

## Geometry measurement and fix

- Same-frame paired actual registry scans: 20 sites × 12 passes, 81 ships / 599 live squadrons. One warmup pair/site; alternating baseline/snapshot order; snapshot cost included.
- Baseline: **185.2264 ms** total (ships 13.8985; squadrons 171.3279).
- Snapshot: **107.3877 ms** total (ships 5.7293; squadrons 101.5095; snapshot 0.1489).
- Reduction: **42.0% of this scan microbenchmark**, with identical leading player, advantage and team count in every pair. This is not a whole-frame speedup or proof about the old 283 ms event.
- Production change: optional per-area preparation callback in `CaptureStrengthSampler`; `CaptureSitesSystem` refreshes center/radius in it. A cached strength read does no preparation. Moved/resized site geometry takes effect at the next due sample; no lifetime-immutability assumption.
- Added unrun `CaptureStrengthSamplerTests`: preparation timing, geometry movement/scaling, invalidation and predicate-only compatibility.

## Shot-prefab experiments

| Prefab | Isolated warm Instantiate median | Crowded live probe median | Crowded live probe maximum |
|---|---:|---:|---:|
| ConcussionMissileShot | 0.3923 ms | 0.5778 ms | 0.7029 ms |
| DualProjectile | 0.0866 ms | 0.3441 ms | 0.4152 ms |
| LaserProjectile | 0.3691 ms | 0.5534 ms | 0.6325 ms |
| OctupleProjectile | 0.1010 ms | 0.3862 ms | 0.6403 ms |
| Projectile | 0.0863 ms | 0.3299 ms | 0.3656 ms |
| ProtonTorpedoShot | 0.3798 ms | 0.5687 ms | 0.7205 ms |
| QuadProjectile | 0.1122 ms | 0.3851 ms | 0.4903 ms |

- Isolated: 12 warm samples/prefab plus a separately recorded first call, with actual scripts and separate ResetForPool timing. First call is not an asset-cache reset.
- Live probe: 12 actual unfired clones/prefab, round-robin one clone/frame, separate Instantiate/Reset markers, deferred destruction. The Awaitable continuation phase differs from the ordered prewarm Tick; genuine cache markers are classified separately.
- Native ablations stripped particles/renderers/lines/lights only from disposable templates. They do not identify an M2 offender.
- Pending-transform-job control: clone creation on an independent hierarchy stayed <0.6 ms while explicit job completion afterward waited about 69–90 ms. This rejects a blanket global transform-write fence under these conditions; it does not reject same-hierarchy or native particle-job synchronization in a live M2 battle.
- Population control: up to 1,024 stopped native-only copies / 5,120 particle systems. Active stopped bolt/beam/torpedo Instantiate medians at that population: 0.09965 / 0.4137 / 0.3695 ms. No 27 ms reproduction.
- Preview experiments omit live simulation/render scheduling and Mac hardware. Global net allocated-memory deltas include deferred cleanup and are not per-shot memory estimates.

### Exact ordered-prewarm validation

- Restart after code fixes: 80 ships / 320 squadrons / 402 weapons immediately after stress setup; after >60 seconds, 54 / 459 / 519, state Running. DestroyOpponentBase objective; source imports finished before Play Mode began.
- Queued 12 additional shots for each of six currently used prefab caches after a 3-game-second delay. Captured the actual `ShotPoolPrewarmer.Tick` creation path, not the Awaitable probe continuation.
- Recording: `Logs/RenderAudit/20261030T093234365Z-profiler/profiler.data`; raw indices 0–301 after explicitly clearing the previous profiler buffer. `shot-awake-attribution-20261030T093427396Z.json` contains all 72 prewarm calls and their ancestry.

| Prefab | Prewarm Awake median | Prewarm Awake maximum | Calls |
|---|---:|---:|---:|
| DualProjectile | 0.1754 ms | 0.1858 ms | 12 |
| Projectile | 0.1769 ms | 0.1968 ms | 12 |
| QuadProjectile | 0.2190 ms | 0.2357 ms | 12 |
| LaserProjectile | 0.0283 ms | 0.0293 ms | 12 |
| ProtonTorpedoShot | 0.0237 ms | 0.0273 ms | 12 |
| ConcussionMissileShot | 0.0235 ms | 0.0244 ms | 12 |

- Largest prewarm Awake is QuadProjectile at 0.2357 ms; this does not establish it as the historical M2 offender. OctupleProjectile was covered by the seven-prefab live probe but was not a participating roster cache.
- Copy and Produce timings are separate in the JSON. Do not call these Awake values total Instantiate latency, or add nested marker durations twice.
- The post-fix capture maximum occurred in strength sampling; 876 B were allocated in that Tick. Before/after populations and objectives differ, so the 1.9953→1.2845 ms maxima are validation observations, not a matched speedup claim.
- Fresh-session Console reported 0 errors after the crowded validation. The run later ended; post-run metadata records timeScale 0. Ended-battle recordings from 09:19 and 09:20 were excluded from prewarm conclusions because normal prewarmer ticks did not execute.

## Runtime correctness

- First stress run logged `MissingReferenceException`: `HardPoint.Transform` → `HardPointAdapter.Position` → `SquadronPilot.TickEngage` → `Squadron.Tick`.
- Pilot movement runs every frame while order searches are capped. Dead Attack/Guard targets now trigger `Stop`; a dead opportunistic engagement triggers `Disengage` + `ResumeCourse` before pilot work, preserving Hunt.
- Individual hardpoint death does not destroy its view; source hides/updates its effects. Whole target lifetime validation addresses the observed destroyed-view path without extra per-frame cache scans or constructor/null fallbacks.
- Added unrun Attack/Guard/Hunt regression coverage in `SquadronTargetSelectorTests`.

## Verification and remaining work

- Geometry and target-lifetime changes imported and compiled in Unity; automated tests remain disabled and were not run.
- Final Editor: compilation failed=false, compiling/updating=false, Play Mode stopped, profiler disabled; clean saved MainMenuScene active. No scene/prefab/font assets changed by this follow-up.
- Historical M2 expensive-prefab identity and 283 ms root cause still need a new M2 recording with the current markers or access to the original native recording. The current data supports no speculative prefab repair.
- Original historical investigations remain active in the vault plan; current scan improvement and target-lifetime fix are implemented and runtime-checked. Source/diagnostic tools and portable results are retained for an equivalent M2 capture.
