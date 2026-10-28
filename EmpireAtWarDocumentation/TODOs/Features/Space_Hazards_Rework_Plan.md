---
category: Features
status: in-progress
created: 2026-10-28
---
# Space Hazards Rework (Level-Design Bands + Map Edge Continuation)

Follow-up to [[TODOs/Features/Space_Hazards_Plan|Space Hazards Plan]] after user in-game review 2026-10-28.

## Goal

- Hazards become large elongated bands that claim map territory: route around them or accept the risk (level-design tool, not a bubble).
- Visible hazard edge == gameplay edge for both types.
- No hard edge of asteroids or space dust visible at max zoom or in cinematic camera.

## User Feedback (2026-10-28)

- Small map → 2 hazards of the same type. Want 2 different types.
- Hazards read as small bubbles. Want big prolonged shapes.
- May reduce asteroid fields to make room.
- Ion storm: visual size ≠ real area.
- Hazard visuals hard to see (screenshots: reference look = long coloured gas stream with asteroids along it).
- Asteroid fields and dust stop at the map border → hard edge visible in cinema mode / max zoom.

## Pre-rework Source Snapshot (2026-10-28)

- Steps 1–7, 9, 10 of the original plan exist in source. Step 11 (full suite green + user acceptance) still open.
- `MapGenerationSettings.asset` sizes: Small `±2000`, 2 hazards; Medium `±3000`, 4; Large `±4500`, 6.
- Both presets `Radii = (240, 160)` → Small map hazard area ≈ 0.75% of the map each. Root cause of "bubbles".

## Findings

### Root causes of reported issues

1. **Same type on Small.** `HazardPlacer.Place`: one preset per mirrored pair (`_settings.Hazards[(firstPreset + pair) % Count]`). `HazardCount 2` → 1 pair → both same type, by design.
2. **Bubble size/shape.** Fixed `Radii` per preset, independent of map size. Placement runs **after** asteroid fields and rejects any overlap with a `FieldVolume`, node outer radius, station → only small free holes exist. Fits uses `max(Radii)` circle for bounds and hazard–hazard gap (over-conservative).
3. **Ion visual ≠ gameplay.** Combination of:
   - `SpaceHazard.shader`: alpha fades from `r ≈ 0.65–0.8` to `0` at `r = 1` and is × `_BaseColor.a` (0.22–0.26) × density → perceived edge ≈ 70–80% of the ellipse; ion noise erodes more.
   - `HazardService` pads by full hull radius (`IShipSpawnClearance.GetPlanarRadius`) + `ExitHysteresis 10` → large capitals lose shields while visibly outside the cloud.
   - Ion arcs only inside `0.55` radius (`NebulaVisualPresenter.Update`) → bright core reads as the whole storm.
   - Only one sheet at full opacity (`i == layers.Length - 2`, y ≈ -27.5); others 0.015 → effectively one flat sheet.
   - Entry cue size uses `IMoveFacade.NavigationRadius` while membership uses `GetPlanarRadius` → two different ship radii.
4. **Low visibility.** Max alpha ≈ 0.26 on one sheet; colours close to the backdrop nebula (`MatchNebula`).
5. **Hard map edges.** `FieldGrid` rasterises only `size.Bounds`. `MapSpaceDust.shader` multiplies alpha by `saturate(min(mapEdge)/100)` → dust ends in 100 m at the border. Camera `ZoomRange.Max 1175`; cinematic camera can look toward the horizon.

### Code / architecture issues

- `HazardEffectsPresenter` (Services) depends on concrete `MapLayoutView` (MonoBehaviour) → violates DIP; `MapLayoutView` now also owns per-ship entry VFX (layout spawner + effect view). Move to `IHazardEffectsView` + dedicated `HazardEffectsView`.
- `NebulaVisualPresenter` is a View (MonoBehaviour rendering only) named "Presenter" → rename `HazardCloudView`.
- `NebulaVisualPresenter.Update`: Get/SetPropertyBlock on every layer every frame for ion storms; string property names. Set only on flash start/end; cache `Shader.PropertyToID` like `MapSpaceDust`.
- `HazardEntryCue`: `Instantiate`/`Destroy` per entry; no pooling. Acceptable now; pool if many squadrons enter at once.
- Type → presentation mapping duplicated: `ReinforcementService` title (`IonStorm ? "Ion Storm" : "Nebula"`), `MiniMapHazard.IsIonStorm`, `NebulaVisualPresenter._isIon`, `HazardEntryCue._isIon`, `ShipUi`. Move `DisplayName`, minimap colour/icon onto `SpaceHazardData`.
- `MapLayoutGenerator.Generate(..., int hazardSeed = 0)`: optional default hides a missing seed. Make it required.
- `HazardPlacer` silently places fewer/zero hazards after `MAX_PAIR_ATTEMPTS`; no test asserts Small always gets 2.
- Inline fully qualified names (`EmpireAtWar.Services.Hazards.IHazardService` in `ReinforcementSpawnRule`, `SkirmishServiceRegistrations`, `MiniMapUiController`) → `using`.
- Decision text "Shapes: circle + rotated ellipse only" must be superseded (see Decisions).

## Decisions (confirmed by user 2026-10-28)

- **Shape = band.** Smoothed centerline polyline (4–6 points, bend ≤ 0.25 × length) + half-width profile (wide middle, tapered ends). Why: long, territory-claiming, still generated. Avoid: authored polygons, ellipse aspect hacks.
- **One geometry source.** `HazardBand` (new `IHazardSource`) holds polyline + widths; gameplay `Contains`, ribbon mesh, minimap outline and AI cost all sample it → visual edge == gameplay edge by construction.
- **Membership by ship center** (+ `ExitHysteresis`), not hull radius. Why: "inside the cloud" must look inside. Avoid: `GetPlanarRadius` padding.
- **Placement before asteroid fields.** Order: stations → nodes → lanes → planet → **hazards** → fields. `FieldDensity` treats bands (+ margin) as clearing, with a thin asteroid fringe along band edges (screenshot 1 look). The fringe sits outside the band edge, so it never puts rocks inside the hazard. Bands may cross lanes (that is the risk choice); never cover stations, default zones, node outer radii.
- **Small map (1 pair) uses 2 types.** Fairness: both centers on the axis equidistant to the teams (duel: the free diagonal; 4 players: team-split axis), each band runs perpendicular to the team axis and bulges along it, mirror-symmetric about the axis. Avoid: a band along the axis (a no-man's-land wall). Pairs ≥ 2: keep same-type mirrored pairs + alternate.
- **Size scales with map.** Band length/width as share of map side in `MapSizeSettings` (start: length 0.35–0.5 × side, half-width 120–260 m Small, scaled up). `SpaceHazardData.Radii` removed.
- **Fewer asteroids.** Raise `FieldThreshold` 0.5 → ~0.56 and/or `RidgeThreshold`; tune per size with a coverage test.
- **Edge continuation is visual only.** Decorative ring outside `Bounds`: same noise as `FieldDensity` (no lanes/pockets/clearings), density fades to 0 at outer margin. No `MapObstacle`, no layout/gameplay data, no fog/minimap. Rendered instanced (no GameObject per rock).
- **Save compatibility.** Saves regenerate from `Map.Seed`; hazard-first ordering changes field layouts of old saves. **Accepted**: old saves regenerate with new layouts; no layout version gate.

## Implementation

1. [x] **Band geometry.** `HazardBand : IHazardSource` (polyline, widths, AABB reject, distance-to-segment + width lerp). Replace `HazardZone`; `HazardSpot` stores points + widths.
   - Verify: `HazardZoneTests` ported: inside/outside, taper ends, hysteresis, AABB, rotation-free cases.
2. [x] **Placer rework.** `HazardPlacer` runs before fields; size-scaled band generator; mirrored pairs; Small = 2 types on equidistant axis; avoidance via `PointOfInterestPlacer.GetOuterRadius` (unchanged rule); exact band-vs-band and band-vs-bounds tests (no max-radius circles). Make `hazardSeed` required.
   - Verify: 20 seeds × 2/3/4 players × 3 sizes: count == `HazardCount`, Small types differ, point symmetry, no station/zone/node overlap, never throws.
3. [x] **Fields around hazards.** `FieldDensity.IsInClearing` += band clearing; thin field fringe just outside band edges (fringe width per size; must not seal lanes); retune `FieldThreshold`/`RidgeThreshold`/`MinFieldArea` per size.
   - Verify: field coverage share per size within target (record before/after); lanes still passable; `MapLayoutGeneratorTests` green.
4. [x] **Membership by center.** `HazardService` drops hull padding; one radius source for cues.
   - Verify: `HazardServiceTests` updated (enter at center crossing, exit + hysteresis, overlaps, death inside).
5. [x] **Ribbon visuals.** Generate a ribbon mesh per band (UV.x along, UV.y across, end taper) in `HazardCloudView` (renamed); shader edge from across-width + taper with feather only in the last ~10%, plus faint rim at the gameplay edge; raise opacity (nebula ~0.35, ion ~0.4 peak) and saturation for readability; 2–3 effective sheets around ship mid-band; arcs sampled along the whole band; property block only on flash change.
   - Verify: Editor captures (min/medium/max zoom) of both types; rim matches `Contains` boundary (overlay debug gizmo); ships/selection readable.
6. [x] **Consumers.** Minimap: draw band polygon outline/fill from sampled geometry, icon at band midpoint (`MiniMapHazardView.OnPopulateMesh`). `ShipPathGrid`/`EnemyStrategicContextBuilder` unchanged (use `Contains`). Move display name/colour/icon to `SpaceHazardData`.
7. [x] **Architecture cleanup.** `IHazardEffectsView` + `HazardEffectsView`; `HazardEffectsPresenter` depends on the interface; remove `ShowHazardEntry`/`hazardEntryPrefab` from `MapLayoutView`; `using` instead of inline FQNs; update `SpaceHazardVisualBuilder` / `SceneContext.prefab` bindings via Unity tooling.
8. [ ] **Decorative asteroid ring.** `DecorAsteroidBuilder`: ring `Bounds` → `Bounds + EdgeMargin` (start 1500 m Small, 2000 m Large), noise from `FieldDensity` shape/ridge only, density × falloff to 0. Output plain instance data (`MapLayout.DecorRocks` or separate view-only result). `DecorAsteroidRenderer` draws with `Graphics.RenderMeshInstanced` per rock mesh. Budget: ≤ 10–15k instances on Large; LOD by distance.
   - Implemented and saved; deterministic exterior-only/budget tests, prefab bindings and max-zoom/cinematic captures pass.
   - Still open: matched frame time vs baseline. Current measurements cover CPU submission only; keep this step unchecked until frame profiling is complete.
9. [x] **Dust continuation.** `MapSpaceDust`: replace hard `saturate(mapEdge/100)` with fade over `EdgeMargin` outside the map (bounds = map + margin); renderer bounds updated.
   - Verify: no visible edge at max zoom/cinema; dust budget unchanged (fixed card count).
10. [ ] **Final.** Compile, hazard + map EditMode tests, full suite (compare to 20 pre-existing failures), user in-game acceptance.

## Verification (2026-10-28)

- Shared geometry: `HazardBand.Boundary` is a sampled ribbon polygon; containment uses polygon membership and edge distance. Rendering and minimap triangulate the same cross-sections.
- Small fairness: straight centerline on the equidistant axis; symmetric width bulges toward both teams. Curved, same-type mirrored pairs use five control points, 25 samples and bend ≤ `0.08 × length`.
- Seed sweep: `20 seeds × 2/3/4 players × 3 sizes = 180` layouts passed exact counts, mixed Small types, symmetry, bounds and station/site/field clearance checks.
- Saved settings: length `0.35–0.5 × side`; half-width `0.03–0.065 × side`; fringe `60/90/135 m`; exterior margin `1500/1700/2000 m`; field threshold `0.56`.
- Controlled field raster coverage snapshot before the concurrent satellite integration (five seeds/size, identical features/noise): Small `11.23% → 9.23%`, Medium `20.78% → 13.70%`, Large `18.33% → 12.76%`. Generated navigation-volume coverage `4.10% / 7.10% / 7.61%`; see `Logs/SpaceHazardsRework/coverage.txt`.
- Large exterior preview: `11,803` rocks; cap `15,000`. Median CPU draw submission `1.0024 ms` max zoom / `0.9442 ms` cinematic; empty baseline ~`0.0045 ms`, 12 samples + three warmups. Not GPU or total battle-frame timing.
- Captures: `Logs/SpaceHazardsRework/` contains both hazard types at min/medium/max zoom, cinematic view, boundary overlay and exterior-ring previews.
- Final focused run: `29/29` pass in `53.98 s`, including the 180-layout sweep and real-loader integration. Loader fixture enters/exits Play Mode to exercise player-frame waits. Evidence: `Logs/SpaceHazardsRework/hazard-tests-final.json`.
- Placement optimization: sample band centers inside their admissible bounds; reject segment pairs by AABB before exact intersection; Small uses 600 attempts. The earlier slow full-suite attempt was cancelled. Final full-suite run completed in `546.46 s`: `2,392/2,420` passed, `28` failed. All decor (4) and dust (6) tests passed. Nine failures match retained baseline names; the baseline reports 20 failures but retains only nine details. Concurrent capture-site compilation and a main-thread tool timeout polluted this run; it is not a clean no-regression comparison. Small mixed-roster seed-5 placement failed during the run; after concurrent satellite integration supplied its missing prefab, the exact current-source case passed with two hazards in `192 ms`. Current Small-roster rerun fails at four Republic players, Easy, seed `7`; independent reproduction throws after 60 attempts in `1,610 ms`. Valid fair bands exist at node-layout attempt 1: center offsets `±1040 m`, length `1400 m`, half-width `120 m`; the random 600 candidates miss them. Coordinated fix in `HazardPlacer`: final 128 of 600 Small candidates sweep admissible center positions at configured minimum dimensions; first 472 random candidates remain. Bounds, node clearance, symmetry and overlap rules unchanged. Combined current-source `MapLayoutGeneratorTests`: `33/33` pass in `373.35 s`, including all Small rosters/difficulties, hazard counts/clearance, determinism and satellite integration. Evidence: `Logs/SpaceHazardsRework/combined-map-tests.json`. Diagnostics: `Logs/SpaceHazardsRework/small-four-diagnosis.cs` and `small-four-reproduction.cs`. Evidence: `full-suite-final.json` and `failure-comparison.json`.
- Saved rock mesh verification: largest scaled radius `44.23 m`; minimum band clearance `116.36/155.15/155.15 m` by map size. All four mesh variants clear the band surface; prefab/settings persisted clean.
- Final current-source hazard rerun: `29/29` pass in `45.63 s` after the deterministic fallback and satellite integration; includes real-loader and 180-layout regression coverage. Evidence: `Logs/SpaceHazardsRework/current-hazard-tests.json`. Unity released with no pending operations.
- Remaining: clean full-suite baseline comparison after concurrent Editor work ends; user in-game visual/gameplay acceptance; matched battle-frame performance check. Small-map satellite/hazard sampling integration is verified. Plan remains active.

## Follow-up (2026-10-28)

- Code + capture review: band visuals read as geometric spindles, not nebula/ion storm. Shape, geometry and visual rework planned in [[TODOs/Features/Space_Hazards_Shape_Rework_Plan|Space Hazards Shape Rework Plan]]. Steps 8 and 10 here stay open.

## Edge Cases

- Band touching the map border: allowed to clip into bounds edge? → keep inside `Bounds` with clearance ≥ 0; decor ring may continue nebula visuals later (out of scope now).
- Two bands crossing: forbid (exact band-vs-band test).
- Band covering a lane end at a node pocket entrance → forbidden by node outer radius.
- Squadrons: same center rule.
- Reinforcement gate: `BlocksReinforcement` unchanged (center point).

## Files

- New: `Entities/Map/Hazards/HazardBand.cs`, `Components/Environment/HazardCloudView.cs` (rename), `Services/Hazards/IHazardEffectsView.cs`, `HazardEffectsView.cs`, `Entities/Map/Generation/DecorAsteroidBuilder.cs`, `Components/Environment/DecorAsteroidRenderer.cs`.
- Touched: `HazardPlacer`, `HazardSpot`, `SpaceHazardData`, `MapSizeSettings` (+asset), `MapLayoutGenerator`, `FieldDensity`, `MapLayout`, `MapLayoutView`, `HazardService`, `HazardEffectsPresenter`, `SpaceHazard.shader`, `MapSpaceDust(.shader)`, `MiniMapHazard(View)`, `MiniMapUiController`, `ReinforcementService`, `SpaceHazardVisualBuilder`, tests.
- Removed: `HazardZone.cs` (after port).

## Resolved Questions (2026-10-28)

- Old saves: accept the field-layout change.
- Small-map split pair: bands perpendicular to the team axis.
- Asteroid fringe along band edges: yes.
