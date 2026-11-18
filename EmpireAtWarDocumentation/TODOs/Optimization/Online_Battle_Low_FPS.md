---
category: Optimization
status: todo
created: 2026-11-18
---
# Online Battle Low FPS (3 humans vs 4 AI)

## Goal
- Find why host and both clients ran 10–20 FPS in the 2026-11-17 evening online battles.
- Add release-build evidence so the next report has numbers.

## Evidence
- Build 0.1.69 (launcher `Applying 0.1.69` 16:49Z = 18:49 local), source ≈ `d60ae1930`.
- Host machine: RTX 5070, 16 threads, 2560×1440 Exclusive Fullscreen, `qualityPreset: Cinematic`, no frame cap, vSync off.
- Archive: `Logs/OnlineBattles/20261117T1650Z/` (git-ignored)
  - `raw/Player.log` (64,775 lines, 18:49–23:59), `raw/launcher.log`, `raw/settings.json`.
  - `battles/battleNN_<role>_L<from>-<to>.log` + `_compact.log` (stack traces stripped), `battles/INDEX.txt`.
  - `ai_summary.txt`; scripts in `scripts/` (`split_battles.py`, `ai_summary.py`).
- Battles hosted: 05 (3 Rebellion AI), 06 (3 Empire AI), 07 (5 AI mixed, no outcome logged), 08 (4 Rebellion AI, 4 client connects incl. reconnects). 01–04 were short client joins.
- **Gap:** release `Player.log` has no timestamps, FPS, ping or shot data. `BattleTelemetry/` is Editor/Development only (last file 17:52, none for these battles).

## Findings (source)
- Fire-rate starvation loop drives CPU: starved mounts re-run target selection every frame → [[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]].
- Host network cost: `NetworkTickService.SendChangedHealth` every 0.25 s builds `JsonUtility.ToJson(UnitHealthSave)` per changed entity, broadcast per client → GC pressure in big fights.
- Spawns send full JSON unit saves (`HostTickReplicator.CreateSpawn`); squadron churn re-sends them.
- Clients also run turret/projectile visuals from host target events → client FPS tracks host battle size; damage stays on host.
- AI: 4 UltraHard AIs (`decisionInterval` 0.5 s) + ~650k credits/battle of superweapon purchases in 07/08 (`[EnemyAI:SuperWeapon]` 600+ lines).
- Exception per destroyed ability caster in `Tick` → [[Done/Bugs/Ship_Ability_Event_Destroyed_Caster]].

## Plan
### Phase 0 — Release telemetry (no Development build needed)
- [ ] Every 10 s in battle, one `Player.log` line: battle time, FPS avg/p95/max frame ms, GC count, entity/ship/squadron counts, shots/min, network bytes out, client RTT.
- [ ] Prefix `[EnemyAI]` lines with battle time.

### Phase 1 — Capture
- [ ] Development Player, host, 3 AI UltraHard, Cinematic and Performant presets; `render_audit_profiler` 90 frames at peak fight. Follow `Tools/BattleAudit/README.md`.
- [ ] Split CPU: combat (target selection/due), AI, navigation, networking (health JSON, keyframes), rendering.

### Offline playtest 2026-11-18 (reference for online)
- 4-player Large skirmish in the Editor: 122 → 52–64 fps as entities grow to ~200; 300–350 ms frame every minute; spikes owned by AI capture probe/patrol select. Details: [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] "Playtest 2026-11-18".
- The online host runs the same AI for every AI slot, so these spikes apply to the host; the order churn (≈14k orders / 32 min) also feeds order replication.
- Radar now scans once per frame (done in `e84297792`, test added in `8787a78e1`).

### Fixed-step pile-up (proposal 2026-11-18)
- Fixed step 0.02 s; `Maximum Allowed Timestep` 0.333 s → at 10 FPS Unity runs 5 fixed steps per frame (up to 16 at 3 FPS).
- Only two things run there: physics (needed for `UnitSpawnView` trigger checks; few moving colliders after the fighter-collider removal) and `RadarScanService.FixedTick`.
- Transforms do not move between fixed steps inside one frame → 5 radar passes per frame each rebuild the full target snapshot (`PrepareScan`) from identical positions. Pure waste.
- Proposed change: `RadarScanService` becomes a per-frame `Tick` with an accumulator. Elapsed steps = accumulated time / `Time.fixedDeltaTime`; each radar's `StepsUntilScan` drops by the elapsed steps; one snapshot and one scan per frame for every radar due. Same scan cadence per second, one snapshot instead of up to 5–16.
- Not an extra scan: the per-frame pass **replaces** the fixed-step passes. Each radar still scans once per its `Delay`; only the batching changes (one snapshot per frame instead of one per fixed step).
  - Example at 10 FPS: today 5 fixed steps → 5 snapshots of the same positions, each scanning the radars due that step. New: 1 snapshot, the same radars scanned together.
- **Keep `Maximum Allowed Timestep` 0.333.** It is the clamp on `Time.deltaTime`: game time already runs at real speed down to 3 FPS, and only below 3 FPS does it slow down. Lowering it would bring slow motion up to 10 FPS. Decision: do not change it.
- What prevents the low-FPS spiral instead: keep per-frame work independent of how many fixed steps a frame contains (radar change above), and cut the costs that grow with craft count (squadron plan) and with starved weapons (fire-rate plan).
- Tests: `Vision` (`RadarScanService` tests): same detections after N frames at 10 FPS vs 60 FPS.

### Phase 2 — Fixes (after Phase 1)
- [ ] Fire-rate loop fix (other plan).
- [ ] Health replication: binary `Vector2` hull/shield + hardpoint deltas instead of JSON.
- [ ] Re-check against [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] and [[TODOs/Optimization/Battle_FPS_Recovery_Plan]] to avoid duplicate work.

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Tests
- Not run (local override). Review `Networking` tests if health message format changes.
