---
category: Bugs
status: in-progress
created: 2026-11-17
---
# Capital Movement and Class Rework Fixes

## Goal
- Fix capital-ship attack movement seen in the 2026-11-17 13:40 battle: ships head the wrong way, freeze, or never leave their spawn point.
- Fix ISD I spawn/collider problem (needs a repro first).
- Close the defects found while reviewing [[Done/Refactoring/Ship_Class_Rework_Plan]].

## Evidence — battle 2026-11-17 13:40–14:05
- Source: `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/`
  - `Loading_20261117_133903_ai_orders.csv`: per-ship orders plus a sample every 5–10 s. The human player is `Player0` (Empire).
  - `Battle_20261117_134024.csv`: shot and impact totals (1532 s).
- Ship identity comes from the hull column: ISD I 14000, ISD II 20000, Tector 17250, Allegiance 23000, Titan 48300, Executor 150000, enemy Mandator 86250.
- Editor.log has no errors or warnings for this play session.

| Entity | Ship (class) | What happened | Telemetry |
|---|---|---|---|
| 213 | ISD I (Capital) | Spawned at (98.8, −170.7) at 720 s, idle until an AttackMove at 1073 s, moved normally, destroyed at 1133 s | No stuck samples. **ISD I spawn issue not visible** |
| 239 | Titan (BattleCruiser) | Attack on Mandator 242 at 1204 s. `moving=1`, but **position frozen at spawn (242.6, −655.9) for 55 s** while the bow turned away (facingDot 0.98 → −0.91). Drifted north (away) to (251, −545), froze again for 35 s, then approached at 1308 s | Matches "Titan goes the other way after ~20 s" |
| 243 | Executor (Dreadnought) | **Never left its spawn point (727.7, −566) for 280 s** under Attack orders on station 164, Mandator 242 and others. `moving=0`, target ~1340 away, weapon range 975 | Holds out of weapon range |
| 267 | Allegiance (BattleCruiser) | Frozen with `moving=1` from 1538 to 1558 s | Same symptom as Titan |
| 270 | Tector (Capital) | Group Attack on station 164 at 1638 s. Closed to 441, then **retreated stern-first** to 624–725 (facingDot 1 → −1) | Artillery retreat band |
| 278 | Arquitens-class (Cruiser) | Same order. **Ran ~500 north, away from the station**, then came back | preferredContact switched to squadrons 66/63 |
| 289, 291, 277, 286 | ISD II / Tector / cruisers | Same order. Closed and held at 260–340 | Normal |

- Scan result: ships flagged `moving=1` with an unchanged position for 3 or more samples were **only 239, 243 and 267**. Those are the BattleCruiser and Dreadnought hulls.

## Root-cause analysis

### Is the class rework the cause?
- No ISD I, ISD II, Tector or Titan `ShipData` or prefab is in the class-rework diff.
- The class checks on these paths do the same thing as the old `HeavyCapital` checks for these ships:
  - `AttackTargetState.UpdateMoveState`: target is `BattleCruiser || Dreadnought` (was `HeavyCapital`). Titan, Allegiance and Mandator were all `HeavyCapital` before.
  - `WeaponComponent.CanUseSiegeApproach`: `BattleCruiser || Dreadnought` (was `HeavyCapital`). Executor, Titan and Allegiance were `HeavyCapital`.
- What the rework did change for these ships:
  1. Mandator, Praetor and Executor are now `Dreadnought`. Engagement range is rebuilt per class by `WeaponEngagementRange.Calculate` from the **new `dreadnought` matrix column**: laser 0.15, turbolaser 0.7, concussion 0.15, torpedo 1.75, caliber-0 bias 0.8. Against a Dreadnought this weights torpedo/launcher mounts more heavily, so it can change engagement range, artillery reach and standoff distance.
  2. The hardpoint approach is unchanged: Mandator `NavigationRadius` 482.9 is below Titan AttackDistance 1300 and Executor's 975, so that branch never runs against it.
- Committed before this battle, never play-tested together, and directly on these paths:
  - `417b02029` siege damage and forward main batteries (14 Nov).
  - `b848a70cc` artillery ships hold at live launcher range (15 Nov).
  - `c338d9325` close to oversized hardpoints (15 Nov).
  - `bf9a0b564` / `4bc04255e` multiselect combat overlap settling (16 Nov).
  - `4b9b2a693` stop stern turning; changed hardpoint positions and arcs on ISD I, ISD II, ISD III, Tector, Victory and others (16 Nov 23:54).
  - `434b9d97a` AI shared job optimization (16 Nov).
- **Decision:** treat the class rework as *possible*, not proven. Phase 1 bisects it before code changes.

### Hypotheses (ranked)
- **H1 — Executor holds by contact, not by weapon reach** (243):
  - `ShipEngagement.IsInRange` → `WeaponComponent.HasEnoughRange(distance, contact)` passes when `distance <= contact * CONTACT_ENGAGE_FACTOR`.
  - Contact = Executor `NavigationRadius` 1059.4 + Mandator 482.9 = 1542 > 1340 actual distance → `HoldAndLookAtTarget` while all mounts (975) are out of reach.
  - Large own radius (`872b28513` unrestricted navigation) + large target radius → permanent hold.
  - Not class-related.
  - **Disproven 2026-11-17** (`Battle_20261117_134024.csv`): from its spawn point the Executor landed 986 turbolaser, 86 ion and 135 concussion impacts on the Mandator and got the kill. The bow mounts were in reach; holding was correct. The H1 change in `f7112a5f1` (`ShipEngagement.IsInRange` near-edge check) fixes no observed bug; it changes range logic for every ship and adds a per-tick mount scan inside contact slack. **Reverted in `d813ee1ea`** (`ShipEngagement.IsInRange`, `IWeaponComponent.CanEngageMainWeapon`, three test stubs, the `IsInRange_ContactSlack…` test).
  - `f7112a5f1` also broke the build (CS0535: `WeaponComponent` lacked `IArtilleryCapability.HasMainBatteryCover`). Fixed in `d813ee1ea`; Unity recompile clean. The H3 main-battery hold stays in.
- **H2 — Titan/Allegiance freeze while "moving"** (239, 267):
  - Movement is active, but the ship only rotates in place. The destination is behind it, or settling/overlap code cancels translation (multiselect overlap settling `bf9a0b564`, spawn-blocking release).
  - The freeze began at the spawn point → check spawn-blocking hold, settle budget, and whether `MoveToPursuitDestination(preserveCourse)` picks a bearing away from the target. `StandoffPosition` uses `fromTarget`, so the leg should point *toward* the target.
- **H3 — Tector stern-first retreat at the station** (270):
  - Path: `AttackTargetState.TryUpdateArtilleryMove`. `retreat = distance < max(reach × 0.8, contact)`, then a move leg to `approachDistance` along `fromTarget` → the ship turns its stern to move away.
  - `reach` = min(launcher reach, surviving main-battery reach) (`ArtilleryCapability.GetArtilleryRange`).
  - The main-battery arcs and positions from `4b9b2a693` changed `mount.MaxAttackDistance − offset` → reach.
  - Retreat moves should reverse or hold facing, not turn the stern.
- **H4 — cruiser 278 runs off**:
  - Its preferred contact switched to enemy squadrons (66/63) while the order target was the station.
  - Check whether `AttackTargetState` pursuit uses `_mainTarget` only, or whether the facing/standoff picks the preferred contact.
- **H5 — ISD I cannot be placed (user repro 2026-11-17):** the whole spawn overlay is red near a captured zone, with only some map-edge corners green. Every other ship places normally.
  - `ReinforcementSpawnRule.CanSpawnShip` = `IsOpen` (vision/areas/hazards; not ship-specific) && `ShipSpawnClearance.IsClear` (root `BoxCollider` box at `ShipData.Height`) && `ShipSpawnReachability.IsReachable`.
  - **Clearance ruled out.** Live Editor eval, in memory = disk, not dirty:
    - ISD I box 94.3×49.6×168 @ scale 1 → 47.1×24.8×84 half-extents.
    - ISD II 101×49.5×180 @ 0.933 → 47.1×23.1×84.
    - Practically identical; both at `Height` −154, height tier 2.
  - **Data ruled out:** ISD I `ShipData` and prefab are unchanged by the class rework; the preview prefab is mapped correctly (`ReinforcementData` key 201 → `ISDIReinforcementView`, trigger collider like all others); `heightTiers` are shared.
  - **Reachability is the cause (strong):**
    - Profile = `NavigationRadius` 97.99866 + span [h−24.8, h+25.1]. This radius is unique to ISD I, so it builds its own `ShipNavMesh` (the cache key is clearance plus contacts filtered by span).
    - The origin is always the player's **main station** (`IMapModelObserver.GetStationPosition(owner)`), not the captured zone.
    - `ShipNavMesh.BeginSearch`: `FindNearest` → if blocked, `FindEscape`. Either can leave `_originTriangle = -1` for one clearance/span combination; this is not monotonic, because the obstacle set changes with the hull span.
    - **Bug 1 — overlay:** `ScheduleReachability` passes `OriginRegion = _originTriangle < 0 ? 0 : Regions[_originTriangle]`. An unresolved origin silently floods **region 0** (an arbitrary pocket, here the map-edge corners) → the overlay shows the whole map red except those corners. This matches the repro exactly.
    - **Bug 2 — placement:** `FindReachableGoal` returns −1 when `_originTriangle < 0`, so `IsReachable` rejects every click, including the green corners.
    - **Bug 3 — origin shared with movement:** `ShipNavigationCache.GetGraph` calls `graph.SetOrigin(origin)` for every user of the same clearance graph: spawn checks (station origin) and live ships of the same type planning moves (ship origin). An in-flight ISD I moves the shared origin. The reachability cache then recomputes against whichever origin called last.
  - Not caused by the class rework (no ISD I input changed). It probably depends on the map layout/seed around the station.
  - Separate footprint issue: ISD II collider half-diagonal 103.2 > `NavigationRadius` 91.46 (hull may overlap neighbours).

## Plan

### Done — map info in battle logs (2026-11-17)
- [x] `Assets/Scripts/Entities/Map/BattleMapTelemetry.cs` (new). It is called once from `BattleMapLoader.LoadAsync` after `MapModel.SetLayout`.
- Output: `persistentDataPath/BattleTelemetry/<scene>_<yyyyMMdd_HHmmss>_map.csv`, written next to `_ai_orders.csv` / `_squadrons.csv`.
  - `map` rows: `started_utc`, `source_revision`, `source_state`, `game_mode`, `map_type`, `map_size`, `planet`, `map_seed`, `victory_condition`, `features`, `map_min`/`map_max`, `edge_margin`, `planet_position`, `planet_obstacle_radius`, counts of fields/decor rocks/lanes.
  - `player` rows: id, faction, team, controller, station x/z (the reachability origin for spawn checks).
  - `zone` rows: owner, main/capturable/fixed, x/z. `site` rows: owner, kind, x/z, point id.
  - `field` rows: rock count, floor, ceiling. `hazard` rows: bounds centre and size.
- Editor and Development builds only (`[Conditional]`). Skipped outside Play Mode, so EditMode map tests (`BattleMapLoaderHazardTests`) write nothing.
- Verified: Unity recompile completed, 0 errors; 0 console errors. Not run in battle (user gate).
- Already available for older battles: `BattleCaptures/fps_history_editor.csv` stores `game_mode,map_type,map_size,map_seed` per run (match `started_utc` to the telemetry file time; UTC+2 locally).
- **13:40 battle recovered:** run `f93ba3480483477ba174a1b57985fb65`, started 2026-11-17T11:39:05Z → **Skirmish, Full, Small, seed −1028345868**. The planet is not in FPS history; the new map log records it.

### Report 2026-11-17 (after play test)

#### Outcome
- ISD I spawn: fixed (user's own commit `aec386848`: embedded origins always use the escape search, ranked by region size). User verified.
- All ships spawn without problems; NebulonB (doubled) verified in battle; ships work fine (user).
- Titan/Allegiance/Executor "freeze": **not a bug.** Cause found with the new `movement` telemetry column.
- Class rework: committed (`87d46f8b5`) with the Phase 3 defect fixes.

#### Evidence — battle 160137 (`Loading_20261117_160137_ai_orders.csv`)
- 565 samples; 436 with `moving=1`; 59 (13.5%) were `Moving` with speed 0, no queued plan, not braking/settling, no failed destination.
- Entity 247 (Player1, hull 78831): spawn (1255.9, −1696.3) for the whole 147 s window, `Moving;0;0;0;0;0;…;0.0`, destination changing every 5–10 s.
- Entity 267 (Player0, hull 21500): frozen 14–31 s at spawn, moved once its destination changed.
- Cause: `ShipMovementTweenPlayer` turns the hull in place before a route starts (`_isTurningToRoute`; speed stays 0). `RotationSpeed` (°/s): Executor 1.5, Mandator 1.5, Praetor 2.5, Titan 3, Allegiance 4, ISD I/II 5.
  - 180° turn: Executor 120 s, Titan 60 s, Allegiance 45 s.
  - AI re-issues Attack/AttackMove every few seconds → destination changes → turn restarts.
- Rotation values were not changed by the class rework (only NebulonB changed, to 30).
- Stern-first moves while attacking (`facingDot < −0.5`): 2–3 samples per ship, none sustained; Tector 441 had 2 samples at speed 9.8.

#### Done
- Map telemetry `_map.csv` (`BattleMapTelemetry`).
- Spawn reachability: unresolved station origin falls back to nearest free region (superseded by `aec386848`).
- H1: contact-range hull-reach check added (`f7112a5f1`), then **reverted by the user** (`d813ee1ea`). Executor hold at contact is unproven in telemetry; revisit only if it recurs.
- H3: artillery with main-battery cover holds and keeps facing instead of backing off (`IArtilleryCapability.HasMainBatteryCover`).
- H4: no change; movement follows the order target only.
- H2 telemetry: `movement` column = `phase;queuedPlan;inPlanQueue;braking;settling;failedDestination;destX;destZ;speed` (`IShipMovementDiagnostics`). Fixed a spawn crash it caused on save restore (`109822995`).
- Phase 3: `WeaponLoadoutBakeTests` derives expected values from weapon biases; `HeavyCruiser` in `ShipMarqueeHull`; stale faction/matchup text; Blender tool scripts.
- Footprint audit (all ship data): `NavigationRadius` ≈ longest hull half-extent for every ship (ratio 0.98–1.04) except Praetor (0.74: 236 vs 317). Praetor set to 317.1. VenatorII aliases the Resolute prefab (ratio 1.21, generous, left).
- NebulonB doubling: prefabs (ship, wreck, reinforcement) committed to match data; collider, wreck mesh, reinforcement, shield and 8 hardpoints consistent.

#### Turn-rate change (decision: raise `RotationSpeed`)
- Every ship with `RotationSpeed` ≤ 12 °/s was doubled (50 `ShipData` assets). Ships at 30 °/s or more (NebulonB, Acclamator, corvettes, Arquitens, Carrack, etc.) unchanged.
- Before → after (°/s), 180° turn time after:
  - Dreadnought: Executor 1.5→3 (60 s), Mandator 1.5→3 (60 s), Praetor 2.5→5 (36 s).
  - BattleCruiser: Lucrehulk 2.5→5; Malevolence, Procurator, Secutor, Titan 3→6 (30 s); HomeOne, MC80 Independence 3.5→7; Allegiance 4→8 (23 s); Mediator 6→12; Invincible, Recusant Dreadnought 7.5→15.
  - Capital: Rothana 4.5→9; ISD I/II, Imperator, Interdictor, Procursator, Tector 5→10 (18 s); Dauntless, Endurance, MC80 Justice, MC80b Remonda, Mon Cal Cruiser, Nebula 6→12; Bulwark/II, Imperial Venator, Providence, Resolute, Venator/II 7.5→15.
  - HeavyCruiser: DHOmni 5→10; Bothan, Corellian BC, MC75, MC80 Freedom 6→12; Gladiator, Heavy Dreadnought 7.5→15; Peleset, Recusant 10→20; Liberator, Victory/I/II 12→24. Unchanged: MC30c 35, Dispatcher 45.
  - Cruiser/Frigate: Captor 5→10; MC50, Recalcitrant 12→24. Unchanged: Acclamator(+Assault) 30, NebulonB 30, Munificent 35, Arquitens 45, Centax 45.
- Not run in battle. Verify: Executor/Titan start moving within ~1 min of an order; large hulls not too agile.

#### Open
- Remaining option for slow turns: let the hull move while turning, or stop the AI re-issuing orders to ships mid-turn.
- Height-aware contact distance (ships on non-overlapping height spans still get contact slack).
- Tests not run (local override).

### Follow-up 2026-11-18
- Wrong-way / map-edge Attack legs (ISD II 256, 16:01) and "moves although in range" moved to [[TODOs/Bugs/Attack_Order_Hold_In_Range]] (artillery retreat band 0.8×reach → 0.9×reach leg; formation-side bearing; same-target re-issue resets hold).
- Review corrections still to apply to this note: Phase 2/3 checkboxes not ticked though Report says done/decided; "Progress" still claims H1 check added (reverted `d813ee1ea`); Phase 0 says fix uncommitted (committed `aec386848`); footprint audit used half-length, Phase 2 item asks half-diagonal (ISD II 103.2 vs 91.46 open); note not committed.
- 147 s Mandator 247 example: `Idle` 65–110 s and 154–175 s (fighting); turning only 15–59, 136–146, 185–207 s. Re-issued orders do not restart the turn; moving ships brake (`BrakeForReplan`) for routes > 1° off heading.

### Progress 2026-11-17 (agent run)
- Phase 0 step 3/4 done in `ShipNavMesh.cs`: spawn checks (`IsReachable`, overlay job) use `SpawnOriginRegion()`. An unresolved origin falls back to the nearest free triangle's region instead of rejecting everything; routing keeps the strict origin (`RoutingOriginRegion()`). The overlay job already treats region 0 as blocked, so "floods region 0" did not occur in current code.
- Not done: offline seed repro (step 1), per-origin isolation (step 5), regression tests (step 6). Origin sharing is correctness-neutral (origin is reset before each call) but thrashes the reachability cache.
- Phase 1 needs the user's two battle runs; Phase 2 H1–H4 not implemented (unconfirmed, speculative without Phase 1 data).
- Phase 3 done (source only, tests not run): `WeaponLoadoutBakeTests` derives expected values from weapon biases; `HeavyCruiser` added to `ShipMarqueeHull`; stale faction/matchup text and Blender tool scripts updated. `ISDIII/Register.cs` and `LukeRedSquadron/ConfigureAbility.cs` had no `HeavyCapital` references.
- Phase 2 executed from source analysis (tests not run; Phase 1 data still pending):
  - H1: `ShipEngagement.IsInRange` — contact slack counts only if `IWeaponComponent.CanEngageMainWeapon` reaches the target's near edge. Added to `IWeaponComponent`; stubs updated.
  - H3: `IArtilleryCapability.HasMainBatteryCover`; with main-battery cover `AttackTargetState` retreats only at `contact + 1`, otherwise the old 0.8×reach band.
  - H4: no change; `AttackTargetState` moves on the order target only (`preferredContactId` is telemetry).
  - H2 (Titan/Allegiance freeze): not changed, no cause found in source; needs pursuit-destination telemetry from a repro run.
  - Footprint audit: not done (needs an edit-mode eval over all `ShipData`).
  - Tests added in `ShipPursuitTests`: main-battery hold, contact range needs hull reach.
- Phase 3 committed with the class rework (`Assets/Scripts`, `Assets/Settings`). Left out: `MainMenuScene.unity`, `TestResults_*.xml`, `__pycache__`, NebulonB prefabs, font asset, `.codex/config.toml`.
- Plan stays `in-progress` (Phase 1 repro, H2, footprint audit, Phase 0 steps 1/5/6 open).

### Phase 0 — confirmed root cause and fix (live probe, save `isdIbug`, 2026-11-17)
- The `3d27f7631` fallback (`SpawnOriginRegion`) did **not** fix it; the user still reproduced it in save `Saves/Skirmish/isdIbug.json` (seed −1028345868, Small, planet 30).
- Live read-only probe (`SpawnAreaOverlay` reflection, ISD I placement open, Player0):

| Ship | Reachable cells | Sampled clear cells |
|---|---|---|
| ISD I (clearance 97.99866) | **163 / 65536** | 5519 / 9363 |
| ISD II (91.46416) | 36413 / 65536 | 5519 / 9363 |
| Tector (101.73192) | 35178 / 65536 | 5509 / 9363 |

- → Clearance (`Physics.CheckBox`) is identical. **Reachability** is the cause.
- ISD I graph: origin = station (−1722.9, 1659.9); `_originTriangle` = 2359 (resolved, so `3d27f7631` never ran). Entry (−2095.8, 1891.8) is in **region 3 (47 triangles, map-corner pocket)**; the main region is 1 (2099 triangles).
- The only disk containing the origin is the station keep-out disk (r 341.1), **centred exactly on the origin**. Every direction is a legal escape; the nearest legal vertex per region from the origin:
  - ISD I: region 1 **439.3**, region 3 **439.3** (tie).
  - Tector: region 1 443.1, region 2 443.1 (tie; lucky pick).
  - ISD II: region 1 only (432.8).
- Mechanism: `BeginSearch` → `FindNearest` takes the geometrically nearest triangle. With the origin at the disk centre, `CanConnect` always passes, so `FindEscape` never runs. The tie resolves by triangle order → the corner pocket → the overlay is red except the corner.
- **Fix (uncommitted, not compiled yet: the user was in Play Mode):**
  - `NavMeshEscapeJob`: exits within one `Clearance` of the nearest are ranked by region triangle count (larger wins), then by distance. Pruning uses `best + Clearance`.
  - `ShipNavMesh.BeginSearch`: an origin inside an inflated disk (`IsInsideObstacle`) always uses `FindEscape`. Otherwise the old `FindNearest` → `FindEscape` fallback applies.
  - Regression test: `ShipNavMeshTests.StationCentredOriginNearCornerResolvesToMainRegionNotCornerPocket` (Navigation; not run).
- First attempt threw `InvalidOperationException: NavMeshEscapeJob._regionSizes has not been assigned` on schedule. A `NativeArray` **field** in a job struct must be valid when scheduling, even if it is allocated in `Execute`. Fixed by making it an `Allocator.Temp` local passed to `Consider`/`ConsiderIntersection`.
- Verified 2026-11-17: recompile completed, 0 console errors. Edit-mode smoke via reflection (station disk r 39 at (−60, −60), clearance 3, map ±100): escape triangle 89, entry (−79.2, −22.7); (60, 60), (60, −60) and (−60, 60) are reachable. Tests not run.
- **User gate passed 2026-11-17:** ISD I spawns near the captured zone in save `isdIbug`. Phase 0 is done (`ShipNavMeshTests` regression test not run).
- Remaining (superseded): user gate = reload `isdIbug` → ISD I green area ≈ ISD II. The `3d27f7631` `SpawnOriginRegion` fallback stays as a guard for a truly unresolved origin.
- Steps 1–4 below are superseded by this section; step 5 (origin isolation) is still open as a cache/perf item, not a correctness bug.

### Phase 0 — original steps (superseded)
1. **Offline repro with the recovered seed** (edit-mode `unity command script eval`, no Play Mode): generate `MapLayoutGenerator.Generate(MapSize.Small, players, new Random(-1028345868), -1028345868, planet)`. Build the map obstacle contacts for the ISD I span; create `ShipNavMesh(contacts, station, 97.99866, raster)`. Read `_originTriangle` (reflection) and compare with clearances 91.46416 (ISD II) and 101.73192 (Tector). Expected: ISD I −1, others ≥ 0. If the planet matters, try the skirmish planets or wait for the next `_map.csv`.
2. Live confirmation, only if step 1 does not reproduce. During the user's battle, run a read-only eval with ISD I placement open: split `CanSpawnShip` into `IsOpen` / `IsClear` / `IsReachable` at a cell near the captured zone.
3. Fix the overlay fallback in `ShipNavMesh.ScheduleReachability`: never use `OriginRegion = 0` when `_originTriangle < 0`. Mark every goal unreachable and resolve the origin (step 4).
4. Make origin resolution robust in `ShipNavMesh.BeginSearch`: when `FindNearest`/`FindEscape` fail for a station-centred origin, escape to the nearest free triangle **outside the station's own keep-out disk**, with search radius ≥ station hull radius + clearance. `IsReachable` and `TryFillReachability` must share this result.
5. Isolate the spawn origin from ship movement: `ShipNavigationCache.GetGraph` currently calls `SetOrigin` for every caller with the same clearance, and live ships of the same type move it. Give spawn reachability its own origin-keyed graph/result so a moving ISD I cannot change spawn results.
6. Regression tests (add/review, not run):
   - Seed −1028345868 fixture: the ISD I clearance resolves a station origin, and the overlay has more than map-corner cells reachable.
   - An origin enclosed by an obstacle never floods region 0.
   - The spawn result is unchanged after another agent calls `SetOrigin` on the same graph.
   - Categories: `Navigation` (`ShipNavMeshTests`, `ShipNavigationServiceTests`), `Economy` (`ReinforcementSpawnRuleTests`), `Map` (`BattleMapLoaderHazardTests` unchanged).
- Files: `Services/ShipNavigation/ShipNavMesh.cs`, `ShipNavigationCache.cs`, `NavMeshReachabilityJob`, `Services/ShipSpawning/ShipSpawnReachability.cs`.
- User gate: in skirmish seed −1028345868, ISD I places near the captured zone with the same green area as ISD II.

### Phase 1 — Attribute movement issues (no code changes)
1. Rebuild the 13:40 scenario: same map seed (−1028345868, Small) and faction, with Empire Titan, Executor, ISD I/II and Tector vs a Republic station plus a Mandator.
2. User runs it twice: (a) current tree; (b) class rework stashed (`git stash push -- Assets/Scripts Assets/Settings Assets/Prefabs`). Compare telemetry with the scan below.
3. Freeze scan (repeatable):
   ```sh
   awk -F, 'NR>1 && $3=="sample" && $4=="Player0" { k=$5; if ($13=="1" && $11==px[k] && $12==pz[k]) { c[k]++ } else { if (c[k]>=3) print k, $25, $7, c[k]; c[k]=0 } px[k]=$11; pz[k]=$12 }' <ai_orders.csv>
   ```
4. Run in edit mode with `unity command script eval` (not Play Mode or tests). Print `WeaponEngagementRange.Calculate` and the artillery reach for Titan, Executor, ISD I, ISD II and Tector against `Structure`, `Capital` and `Dreadnought`. Compare the new matrix with the `heavyCapital` values from git HEAD.
5. Record which hypothesis each symptom confirms in this note.

### Phase 2 — Movement fixes (after Phase 1 confirms)
- [ ] **H1:** `IsInRange` must also require weapon reach. Treat contact as reached only when at least one ship-capable mount can reach the target hull or a hardpoint (`CanEngageMainWeapon`). Otherwise approach to `max(contact, engagement)`.
  - Files: `ShipEngagement.cs`, `WeaponComponent.HasEnoughRange`.
- [ ] **H2:** Trace the Titan freeze. Log the pursuit destination, `IsMoving`, settle/overlap state and spawn-blocking state each sample (add `destination` to `ShipOrderTelemetry` samples). Fix the component that cancels translation; do not add a retry.
  - Files: `ShipOrderTelemetry.cs`, `ShipMoveComponent`, overlap settling (`bf9a0b564`), `Services/SpawnBlocking`.
- [ ] **H3:** An artillery retreat backs off without turning the stern: keep `LookAtTarget` while retreating, or skip the retreat when the main batteries can still reach. Set the minimum hold distance from `contact + margin`, not from the launcher band, when the main batteries already cover.
  - Files: `AttackTargetState.TryUpdateArtilleryMove`, `ArtilleryCapability`.
- [ ] **H4:** On an explicit Attack order, movement uses the order target only; squadron contacts may only change weapon targeting.
- [ ] **H5 — ISD I spawn:** moved to Phase 0.
- [ ] **Footprint audit:** rebake `NavigationRadius` for ISD II (collider half-diagonal 103.2 > 91.46) and any ship where collider half-diagonal > `NavigationRadius`; run one edit-mode eval over all 76 `ShipData`.

### Phase 3 — Class rework defects (from review)
- [ ] `WeaponLoadoutBakeTests.CombatRating_HeavyWeaponsApplyCaliberDamageAndAccuracy`: HeavyCruiser expects 1.12 but the data gives 1.175 × 0.975 = **1.145625**; Dreadnought expects 1.40 but the data gives **1.45**. Better: derive the expected value from `weapon.GetDamageBias × GetAccuracyBias` (no stat pinning).
- [ ] Decide the HeavyCruiser capital privileges. Gladiator, MC80 Freedom and DH-Omni were `Capital` and lost these:
  - `ShipInstaller` → `ShipMarqueeHull` (drag-select by hull).
  - `ThreatNotifier` urgent "destroyed" alert; `MapNotifier` hazard alert.
  - `AllyAiMarkerEmitter` capital sighting marker; `EnemyStrategicContextBuilder` ion-risk weight.
  - Recommendation: include `HeavyCruiser` in `ShipMarqueeHull` (they are large hulls); keep notifications/AI capital-only.
- [ ] Stale text in `RebellionFaction.asset`: Mon Cal cruiser (key 302) description says "198/s" → **238/s**. Role lines saying "Heavy capital": Praetor → Dreadnought; Secutor and Lucrehulk → Battlecruiser; MC80 key 302 → capital ship. Matchup labels "Heavy capital ships" (`AssaultFrigateMkII`, `Endurance`, `MC50`, `QuasarFire`) → "Battlecruisers and dreadnoughts".
- [ ] Update `HeavyCapital` in tool scripts that are not compiled by Unity: `Tools/Blender/RecusantDreadnought/RegisterRecusantDreadnought.cs` (`ShipClass.HeavyCapital` → `BattleCruiser`), `ISDIII/Register.cs`, `LukeRedSquadron/ConfigureAbility.cs` (field list → new classes), `Titan/Finalize.cs` (message).
- [ ] Leave `MainMenuScene.unity` (Venator/MC50 instances), `Assets/TestResults_*.xml` and `__pycache__` out of the rework commit.

## Verification
- Source: inspect the diff; edit-mode eval of engagement ranges, artillery reach and the footprint audit before and after.
- Tests (not run — local override): review and update `Navigation` (`ShipPursuitTests`, `ShipEngagement`/AttackTargetState tests), `Combat` (`WeaponLoadoutBakeTests`, artillery tests), `Ships` (footprint/ShipData), `Ui` (marquee), `BattleInput` (selection).
- User gate: replay the Phase 1 scenario. The freeze scan must report no BattleCruiser/Dreadnought freezes, no stern-first retreat at stations, and a clean ISD I/II spawn.

## Open questions
- ISD I spawn: answered 2026-11-17. Placement near a captured zone shows the whole overlay red except map-edge corners; only ISD I is affected → H5 reachability.
- Map seed: recovered from FPS history (−1028345868, Small). The planet for that run is unknown; later battles record it in `_map.csv`.
- Executor: should it close to weapon reach even when its 1059 navigation radius touches the target footprint?

## Files
- `Assets/Scripts/Entities/Ship/StateMachine/AttackTargetState.cs`, `ShipEngagement.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`, `ArtilleryCapability.cs`, `WeaponEngagementRange.cs`
- `Assets/Scripts/Services/Enemy/ShipOrderTelemetry.cs`
- `Assets/Settings/Data/Ship/Empire/ISDIIShipData.asset` (+ footprint audit)
- `Assets/Scripts/Tests/Editor/Combat/WeaponLoadoutBakeTests.cs`
- `Assets/Settings/Data/Factions/*/…Faction.asset`, `Assets/Settings/Data/Tooltip/Matchups/*.asset`
