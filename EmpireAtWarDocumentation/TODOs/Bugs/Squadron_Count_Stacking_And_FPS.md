---
category: Bugs
status: in-progress
created: 2026-11-18
---
# Squadron FPS

## Goal
- Find and fix squadron-driven FPS loss in big (7-player online) battles.
- **Out of scope:** squadron icon stacking / grid (`SquadronIconGrouping`, `SquadronGroupingJob`, `BattleMarker*`). The user is rebuilding it. Do not touch.
- Related: [[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]], [[TODOs/Optimization/Online_Battle_Low_FPS]], [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]] (Phase 5).

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
- 164 live squadrons at 39 s = **41 per player at battle start** (station + carrier start stock). 1,484 launches (46/min), median life 49 s.
- Launches: ARC-170 343, Vulture 335, Y-Wing 236, Belbullab-22 184, V-Wing 171, Hyena 106, Droid Bomber 67, V-19 42.
- **Squadrons fired 56% of all shots** (377k of 674k; Vulture alone 150k, ARC-170 105k).
- Steady `Squadron.Flight` 0.90 ms/frame (agent capture); squadrons are not the spike owner (the AI is).
- Next: measure with the start stock reduced or capped (41 per player × 4 from the first second), and with the fighter/ship pool split from the fire-rate work.

### Phase 1 — Measure (Development build)
- [ ] Release-build line every 10 s: live squadrons and craft per player (with [[TODOs/Optimization/Online_Battle_Low_FPS]] Phase 0).
- [ ] Profile 100 / 200 / 400 squadrons: split script / trails (`TrailRenderer` update + draws) / physics sync / fighter target selection / projectiles.
- [ ] Explain 76 live squadrons for one player 10–35 s into the battle (station + carrier start burst).

### Phase 2 — Fixes (decide after Phase 1)
- [ ] Fighters out of the root hierarchy: parent fighters to a static container so moving the root (marker/selection anchor) no longer dirties the fighter subtrees; keep pooling/despawn working.
- [ ] Merge fighter meshes to one renderer (one or two materials) per fighter: Droid Bomber, A-Wing, V-19, X-Wing, ARC-170 first.
- [ ] Decimate or LOD the high-poly fighters (A-Wing 66k, Delta-7 28k, Belbullab-22 23k, V-Wing 22k triangles).
- [ ] Fewer gun hardpoints per fighter (one mount with several muzzles, same DPS). Balance-neutral, needs your approval.
- [x] `RadarScanService`: scans once per frame (2026-11-18). `FixedTick` only counts due radars (same stagger/interval in fixed steps, deduped via `RadarRegistration.IsQueued`); new `Tick` applies pending targets and runs one snapshot + job for all due radars. At 10 FPS: 1 snapshot/frame instead of up to 5. Tests: `RadarScanServiceTests` now step via `FixedTick` + `Tick`. Recompile clean; tests not run.
- [ ] **Remove muzzle-flash meshes from all models (user request 2026-11-18).** Muzzle flashes come from the weapon VFX (`ShotEffect` muzzle flash, `Prefabs/Vfx/Projectiles`); model-embedded `*Flash*` meshes are dead weight.
  - Left in imported models (enabled in the source FBX/OBJ): TIE Fighter 2, X-Wing 4, ARC-170 2, NTB-630 2, V-19 2, Y-Wing 2, Droid Bomber 6, Ginivex 4, Hyena Bomber 4, Vulture 4, Corellian Corvette 8, Patrol Frigate 2, AotR Golan III 12 (12 model files).
  - Left in prefabs (disabled renderers): nested fighter prefabs in `Prefabs/Models/Squadrons` 24 (8 prefabs), reinforcement previews `Prefabs/Ui/Reinforcement` 146 (11 prefabs), Corellian Corvette model + view 8 + 8, Patrol Frigate model + view 2 + 2.
  - Do: delete the flash meshes at the source (Blender re-export) or strip `*Flash*` objects on import (`AssetPostprocessor.OnPostprocessModel`), then prune them from prefabs and fog renderer lists. Keep the `Muzzle*` transforms that guns use as fire points.
  - Leave `Prefabs/Vfx/*` flash renderers: they are the real muzzle-flash/explosion effects.
  - Verify: no missing references; guns still fire from their muzzles; reinforcement previews unchanged.
- [ ] Fighter guns: own projectile pools (see [[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]] "Split pools").
- [ ] Live squadron/craft cap scaling with player count (needs a design decision).

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
