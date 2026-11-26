---
category: Bugs
status: in-progress
created: 2026-11-18
---
# Squadron FPS

## Goal
- Find and fix squadron-driven FPS loss in big (7-player online) battles.
- **Out of scope:** squadron icon stacking / grid (`SquadronIconGrouping`, `SquadronGroupingJob`, `BattleMarker*`). The user is rebuilding it. Do not touch.
- Related: [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]], [[TODOs/Optimization/Online_Battle_Low_FPS]], [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] (Phase 5).

## Evidence
- Release `Player.log` has no squadron data. 1v1 Editor telemetry from 2026-11-17 is copied to `Logs/OnlineBattles/20261117T1650Z/editor_telemetry/`. Summary: `squadron_summary.txt` (`scripts/squadron_summary.py`).

| Battle (Editor, 1v1) | Duration | Peak alive squadrons | Launches/min | Median life |
|---|---|---|---|---|
| 133903 | 1714 s | 87 (46 + 41) at 21 s | 20.7 | 27 s |
| 160137 | 305 s | 127 (76 + 51) at 9 s | 12.0 | 37 s |
| 164732 | 198 s | 127 (76 + 51) at 16 s | 31.9 | 27 s |
| 172317 | 294 s | 127 (76 + 51) at 33 s | 21.7 | 42 s |

- Craft per squadron (`FighterView` count): Vulture 13, Belbullab-22 11, TIE Fighter/Avenger/Hyena/Droid Bomber/TIE Brute 10, TIE Interceptor/V-19/ARC-170 8, TIE Bomber/Punisher/V-Wing 6, X-Wing 5, Y-Wing/A-Wing 4. Each prefab lists `fighters` twice (two components).
- ≈ 1,000 craft at the 1v1 peak; 7-player online is estimated at 3–4× (not measured).
- Script CPU already measured: ≈ 1.0 ms/frame for 110 squadrons (`Battle.Squadron.Flight` + `Pilot` + `Squadron.Tick`).

## Per-craft cost (source)
- **Engine trails:** `FighterView.engineTrails` = `TrailRenderer` per engine (TIE Fighter 2/craft, X-Wing 4/craft, V-19 3/craft). ≈ 2,000 trail renderers at the 1v1 peak. Each rebuilds its mesh every frame and is its own draw.
  - `m_Time` 0.6 s; `m_MinVertexDistance` **0.05** at 58–66 u/s → one new vertex every frame.
- **Hit collider:** one `SphereCollider` per craft, no `Rigidbody`, moved by transform every frame. Used only by `CameraService` raycasts (selection). Static colliders moved per frame → PhysX broadphase updates.
- **Fighter guns:** every craft gun is a `WeaponHardPoint` in the squadron's `WeaponComponent` → same target-selection batch and the same bolt caches (`Projectile`/`DualProjectile`) as capital guns.
- **Clients:** `Squadron.Tick` runs `_pilot.Tick` + `_flight.Step` on replicas too. This is needed: fighters fly locally around the replicated root. So client cost scales with squadron count (Argon claim checked; not redundant).
- **Fixed step:** `Maximum Allowed Timestep` 0.333 s, fixed step 0.02 s → at 10 FPS, 5 physics steps + 5 `RadarScanService.FixedTick` per frame (snapshot rebuilt each call) → feeds the low-FPS spiral.

## Fixed
- [x] **Torpedo mounts re-ran target selection every frame (2026-11-18).**
  - `FighterTorpedoHardPoint.Attack` refuses unless its fighter is on a fresh attack run. `SquadronPilot.GetAttackRun` returns 0 outside `Engage` (guard/travel/loiter = most of the time).
  - The mount stayed not busy with no retry delay → `WeaponComponent.Tick` queued it into `TargetSelectionBatch` every frame while a ship was in reach.
  - Mounts per squadron: TIE Punisher 24, Hyena/Droid Bomber 10, ARC-170 8, X-Wing 5, Y-Wing/NTB-630 4, TIE Defender 3.
  - Fix: `WeaponHardPoint.CanStartSalvo` (virtual, default true); `FighterTorpedoHardPoint` overrides it (`run != 0 && run != _firedRun`); `WeaponComponent.Tick` queues only mounts with `CanStartSalvo`. Torpedo behaviour is unchanged.
  - Test: `WeaponCandidateTests.Torpedo_CanStartSalvoOnlyOnFreshAttackRun` (Combat). Existing `Torpedo_RejectedShotDoesNotConsumeAttackRun` reviewed: still valid.
  - Verified: Unity recompile completed, 0 errors. Tests not run (local override). In-game check: bombers still drop torpedoes once per run.

- [x] **Removed per-fighter visuals that cost every frame (2026-11-18, user request).** All 31 `*SquadronView.prefab`:
  - Engine `TrailRenderer`s: 334 removed (~2 per fighter; X-Wing/Luke 4, V-19 3).
  - Fighter `SphereCollider`s: 186 removed. Only `SelectionQuery` raycasts used them; squadrons are picked by icon (`SelectionQuery.TryFindMarkerAt` runs first).
  - World selection ring: `SelectionComponent` + `SelectedCanvas` removed. Selection now goes through `SquadronSelectionFacade` (local owner only; resets on respawn). The icon frame shows the selected state (`SquadronIconComponent` → `BattleMarker.SetColor`).
  - Model muzzle-flash meshes (`*_Flash`): 212 disabled renderers that nothing ever enabled (Droid Bomber 60, Vulture 52, TIE Fighter 20, X-Wing 20, ARC-170 16, V-19 16, Vader 12, Y-Wing 8, NTB-630 8).
  - Fighter shadow casting off: 234 renderers (V-19 72, X-Wing 35, TIE Brute 30, TIE Punisher 24, …). The builder already intended `ShadowCastingMode.Off`; main-light shadows are on (Cinematic distance 3000).
  - Stale entries pruned from `FogVisibilityComponent.renderers` and `EntityBindings.Selection`; 0 dangling references after the pass.
  - Code: `FighterView` (no `engineTrails`/`hitCollider`), `SquadronFlightComponent` (no `ClearTrails`), `SquadronInstaller` (no `RegisterSelection`), new `Entities/Squadron/EntityFacades/SquadronSelectionFacade.cs`, `SquadronViewPrefabBuilder`/`SquadronViewSpec` (no trails/collider/ring).
  - Tests: `FighterViewTests` updated; the `HealthOverlayAnchorTests` squadron ring test was removed because the contract changed; new `SquadronPrefabVisualsTests` (Squadrons) asserts no ring, colliders or trails.
  - Verified: recompile clean; prefabs refreshed, re-serialized and saved; 0 console errors. Tests not run. In-game check: icon click/marquee selects squadrons and the icon shows selection.
  - Not updated (not compiled by Unity): `Tools/Blender/*` integration scripts still reference `engineTrails`.

### Per-fighter movement cost (2026-11-18 review)
- `FighterKinematics.Step` is cheap: per fighter one normalize, one `Acos`, one cross product, a quaternion rotate and two `Atan2`. ≈ 1 ms/110 squadrons in total (all of pilot + flight + tick).
- Real cost is the transform hierarchy. Fighters are children of the squadron root; `QueuePoses` sets `transform.position` of the root every frame (dirties every fighter subtree), then `FighterPoseBatch` writes each fighter's world pose again.
- Hierarchy size per squadron (before flash removal): Droid Bomber 826 transforms (≈ 82/fighter), Vulture 305, V-19 286, Luke Red 258, ARC-170 238.
- Renderers per fighter: Droid Bomber 34, A-Wing 25, V-19 11, X-Wing 11, ARC-170 9, Luke/Y-Wing Bomber 8.
- Triangles per fighter: A-Wing **66,499**, Delta-7 28,448, Belbullab-22 23,078, V-Wing 21,940 (others 1–9k).
- Guns per fighter: TIE Avenger/Vulture/ARC-170/TIE Interceptor 4, V-Wing 4, TIE Defender 5. Each is a `WeaponHardPoint` in target selection.

## Plan
### Playtest 2026-11-18 (4 players, 32 min, after the trail/collider/ring removal)
- ~~164 live at 39 s~~ **Correction:** those 164 `start` rows share one timestamp (39.33 s): the squadron pool pre-creates 41 per player (`SquadronPool`, sum of hangar `MaxActive`). Live (sampled) squadrons: 19 in minute 0, 63 at 3, 95 at 6, then **110–150 (≈30–45 per player) from minute 9**. 1,484 launches (46/min), median life 49 s.
- Launches: ARC-170 343, Vulture 335, Y-Wing 236, Belbullab-22 184, V-Wing 171, Hyena 106, Droid Bomber 67, V-19 42.
- **Squadrons fired 56% of all shots** (377k of 674k; Vulture alone 150k, ARC-170 105k).
- Steady `Squadron.Flight` 0.90 ms/frame (agent capture); squadrons are not the spike owner (the AI is).
- `squadron_summary.py` counts pooled `start` rows as live; use sampled entities per minute instead (fix the script).

### Phase 1 — Measure (Development build)
- [x] Release-build line every 10 s (done 2026-11-18): live squadrons and craft per player (with [[TODOs/Optimization/Online_Battle_Low_FPS]] Phase 0).
- [x] (done 2026-11-18, see Attribution run) Profile ~150 live squadrons with the new attribution tools (`Tools/BattleAudit/Attribution.md`, Repeatable Attribution Run on `fps-mid-game`/`fps-late-game-p2`): split `Squadron.Flight`, fighter target selection, projectiles, transform hierarchy, rendering. Trails/colliders are already removed.
- [x] Explained 2026-11-18: pool pre-creation (`start` rows at load), not live squadrons.

### Phase 2 — Fixes (decide after Phase 1)
- [x] (measured not worth it, 2026-11-18) Fighters out of the root hierarchy: parent fighters to a static container so moving the root (marker/selection anchor) no longer dirties the fighter subtrees; keep pooling/despawn working.
- [x] (measured not worth it, 2026-11-18) Merge fighter meshes to one renderer (one or two materials) per fighter: Droid Bomber, A-Wing, V-19, X-Wing, ARC-170 first.
- [x] (measured not worth it, 2026-11-18) Decimate or LOD the high-poly fighters (A-Wing 66k, Delta-7 28k, Belbullab-22 23k, V-Wing 22k triangles).
- [ ] Fewer gun hardpoints per fighter (one mount with several muzzles, same DPS). Balance-neutral, needs your approval.
- [x] `RadarScanService`: scans once per frame (2026-11-18). `FixedTick` only counts due radars (same stagger/interval in fixed steps, deduped via `RadarRegistration.IsQueued`); new `Tick` applies pending targets and runs one snapshot + job for all due radars. At 10 FPS: 1 snapshot/frame instead of up to 5. Tests: `RadarScanServiceTests` now step via `FixedTick` + `Tick`. Recompile clean; tests not run.
- [x] **Remove muzzle-flash meshes from all models (done 2026-11-18, verified by scan) (user request 2026-11-18).** Muzzle flashes come from the weapon VFX (`ShotEffect` muzzle flash, `Prefabs/Vfx/Projectiles`); model-embedded `*Flash*` meshes are dead weight.
  - Left in imported models (enabled in the source FBX/OBJ): TIE Fighter 2, X-Wing 4, ARC-170 2, NTB-630 2, V-19 2, Y-Wing 2, Droid Bomber 6, Ginivex 4, Hyena Bomber 4, Vulture 4, Corellian Corvette 8, Patrol Frigate 2, AotR Golan III 12 (12 model files).
  - Left in prefabs (disabled renderers): nested fighter prefabs in `Prefabs/Models/Squadrons` 24 (8 prefabs), reinforcement previews `Prefabs/Ui/Reinforcement` 146 (11 prefabs), Corellian Corvette model + view 8 + 8, Patrol Frigate model + view 2 + 2.
  - Do: delete the flash meshes at the source (Blender re-export) or strip `*Flash*` objects on import (`AssetPostprocessor.OnPostprocessModel`), then prune them from prefabs and fog renderer lists. Keep the `Muzzle*` transforms that guns use as fire points.
  - Leave `Prefabs/Vfx/*` flash renderers: they are the real muzzle-flash/explosion effects.
  - Verify: no missing references; guns still fire from their muzzles; reinforcement previews unchanged.
- [x] Fighter guns: own projectile pools — done in `3e4207e7e` (strikecraft/ship pool groups, see the fire-rate plan).
- [ ] Live squadron/craft cap scaling with player count (needs a design decision).

## Attribution run 2026-11-18 13:09 (Editor Play Mode, 60 s, 2207 frames)
- Evidence: `Logs/BattleAudit/20261118T130915435Z-attribution` (`battle_20261118_130915_592_summary.txt`, `analysis.md`).
- Frame: median 15.7 ms (63.6 FPS), p95 29.8 ms, avg main thread 14.7 ms, GPU 0.95 ms, render thread 0.87 ms → CPU-script bound, not GPU.
- Squadron script cost per frame (total ms / 2207 frames): `Squadron.Tick` 0.90, `SquadronOrderScheduler` 0.16, `HangarComponent` 0.026, `SquadronHealthComponent` 0.019, `FighterPoseBatch` 0.014, `SquadronPool` 0.005 → ≈ 1.1 ms = ≈ 7.5% of main thread.
- Sampled markers: `Squadron.Flight` 0.46, `Pilot` 0.20, `UpdateOrder` 0.16 ms (154k flight calls ≈ 70 squadrons/frame).
- Shared weapon cost: `WeaponComponent.Tick` 0.45 ms (364k calls), `CombatAttackCoordinator.LateTick` 0.85 ms, `BoltFlightBatch.Tick` 0.22 ms. Squadron share ≈ 56% of shots (earlier playtest).
- Biggest owners: `EnemyAiScheduler.Tick` 1.41 ms, `CombatAttackCoordinator.LateTick` 0.85, `CaptureSitesSystem` 0.32, `BattleVictoryService` 0.30, `ShipNavigationService.LateTick` 0.24.
- Hitches (>200 ms, 13 frames) are Editor/profiler: `GC.Collect` 4.4 s and 17 s `EditorLoop` stalls, `GameView.Repaint` 8 ms, `Profiler.CollectMethodsMetadata`. No `Squadron.*` marker in any hitch top list. 2 frames allocate 12–19 MB (`gc_allocated_bytes` p99 1.2 MB); per-system bytes not attributed.
- No `Transform`/hierarchy marker in the top-40 of any hitch capture → fighter root-hierarchy dirtying is not a measured cost.

## To finish (2026-11-18, after playtest + attribution tools)
- **Status 2026-11-18 (end of session):** steps 1–5 done or measured not worth it; only step 6 (user decisions) and in-game acceptance remain.
- [x] 1. `Tools/BattleAudit/squadron_summary.py` counts sampled entities per minute (+ `test_squadron_summary.py`).
- [x] 2. Release-build `[Squadrons]` line every 10 s (`PerformanceAnalyticsSampler`, `SquadronRegistry.GetLivingCount/GetLivingCraftCount`).
- [x] 3. Attribution run recorded above.
- [x] 4. Phase 2 by measured cost: fighters-out-of-hierarchy, mesh merge and high-poly LOD **not worth it now** (squadrons ≈ 7.5% of CPU, GPU idle at ~1 ms, no transform marker). Revisit only if a Development-player capture at 150+ squadrons shows otherwise.
- [x] 5. Model muzzle flashes removed: `ModelMuzzleFlashPostprocessor` strips `*Flash*` on import under `Assets/Art/Models`; Unity scan 2026-11-18: 0 `Flash` nodes in 605 model files and 704 prefabs (excluding `Prefabs/Vfx`), 0 dangling renderer references in 563 model/preview prefabs. `Muzzle*` fire points keep their world pose. Test `ModelMuzzleFlashTests` (Combat), not run.
- [x] 6a. **Gun mounts decided (user, 2026-11-18):** fewer mounts per fighter, damage per mount raised to keep raw DPS (not excessive), strength differences between units are fine (rebalance via health/speed), share guns across faction squadrons where possible. Fighters/bombers keep one mount per distinct channel (laser + missile/torpedo/ion); never a single mount.
  - State found in working tree (uncommitted, from `Tools/SquadronRebalance/ConsolidateFighterGuns.cs`): one laser mount per fighter plus one per distinct warhead/ion channel. Mounts per squadron now: Vulture 13 (was 52), ARC-170 16, Droid Bomber/Hyena 20, TIE Avenger 20, TIE Punisher 18, Y-Wing Bomber 9, others 1–2 per fighter. Per-shot damage carries the former battery (`WeaponsData` unit overrides ×2–×4, e.g. X-Wing ×3.78).
  - Not done: sharing one `WeaponType` across faction squadrons (each squadron still has its own `*Laser` type with a unit override). Optional; changes pools/overrides only, no mount-count gain.
- [ ] 6b. Live squadron cap per player count: **user postponed ("later, by me")**. The cap that slipped into the working tree was reverted 2026-11-18.

### Original list

1. Fix `squadron_summary.py` live count (sampled entities, not `start` rows).
2. Release-build squadron/craft counter (shared with Online low-FPS Phase 0).
3. Measure with the Repeatable Attribution Run (one mid-game, one late-game save); record `Squadron.*` time, fighter target selection and squadron allocation in this plan.
4. Pick from Phase 2 by measured cost: fighters out of the root hierarchy → mesh merge (Droid Bomber 34, A-Wing 25 renderers/fighter) → high-poly LOD (A-Wing 66k tris).
5. Remove model muzzle-flash meshes from all models (12 model files + prefabs; list above).
6. Decisions needed from the user: live squadron cap per player count; fewer gun mounts per fighter.
- Done when: steps 1–5 complete or measured as not worth it, and decisions recorded.

## Review 2026-11-18 (uncommitted working tree, after the attribution run)
- OK: muzzle-flash strip (`ModelMuzzleFlashPostprocessor`, ship/squadron/reinforcement prefabs), `[Squadrons]` release log line, `squadron_summary.py` live count, gun consolidation (all 31 squadrons: prefab mounts = `SquadronData.WeaponLoadout`, checked by Unity eval). Tests updated with the contracts.
- [x] **Reverted 2026-11-18 (user):** live squadron cap removed (`SquadronPool`, `ISquadronSpawner.CanSpawn`, hangar/launcher/reinforcement/survival gates, scope move, cap tests). Kept: `SquadronRegistry.GetLivingCount/GetLivingCraftCount` for the `[Squadrons]` log line (test `SquadronLivingCount_CountsOnlyRegisteredLivingSquadronsAndUpdatesOnLoss`). 6b stays a user decision for later. ~~Live squadron cap is in the code although the user postponed the decision (6b).~~ `SquadronPool.LIVE_SQUADRON_BATTLE_BUDGET` 160 / players (4 players → 40 each) gates hangars, station/team launches, squadron reinforcement placement and survival waves; `SquadronPool.Spawn` throws above it. Decide keep or revert before committing.
  - If kept: reinforcement placement shows invalid with no reason when capped (needs a message, check the purchase is not lost); `SurvivalWaveDirector` waits while capped (waves can stall); `TutorialDirector.SpawnUnits` calls `Spawn` without `CanSpawn` (throws at the cap; unlikely in 1v1).
- [ ] Gun consolidation is balance-changing: per-shot damage ×2–×4 keeps raw DPS, but larger single shots overkill small craft more and change hit variance. Needs a `Balance`/fighter-duel check (user approved the direction).
- [ ] "Measured not worth it" for fighter hierarchy / mesh merge / LOD rests on one Editor run at ~70 squadrons and "no Transform marker" (hierarchy cost shows in transform-dispatch/culling, not a named marker). Treat as deferred, not closed; recheck in a Development Player at 150 squadrons.
- Postprocessor risks (low): `filter.sharedMesh.name` throws on a MeshFilter with no mesh; any future node containing "Flash" is deleted on import.
- Tests not run (local override). Nothing committed yet.

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Tests (not run — local override)
- `Combat`: `WeaponCandidateTests` (updated). `Squadrons`: launcher/hangar, `FighterViewTests` if trails change.

## Files
- `Components/ViewComponents/Health/WeaponHardPoint.cs`, `FighterTorpedoHardPoint.cs` (changed)
- `Components/Weapon/WeaponComponent.cs` (changed)
- `Tests/Editor/Combat/WeaponCandidateTests.cs` (changed)
- `Components/ViewComponents/Squadron/FighterView.cs`, `Entities/Squadron/Squadron.cs`, `SquadronPilot.cs`, `Services/Radar/RadarScanService.cs`, `ProjectSettings/TimeManager.asset`
