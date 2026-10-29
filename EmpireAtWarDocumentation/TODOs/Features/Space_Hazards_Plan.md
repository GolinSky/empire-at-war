---
category: Features
status: in-progress
created: 2026-10-28
---
# Space Hazards (Nebula / Ion Storm)

## Goal

- Generated map areas that suppress special abilities (Nebula), or abilities and shields (Ion Storm), and block reinforcements.
- Later: map-wide hazard events that apply to every ship for a limited time. Build the source model now; implement events later.

## Decisions

- Placement: `MapType.Full` only; none on `Survival`/`Empty`. Mirrored pairs (point symmetry) plus an optional central hazard.
- Avoid (soft, best effort): stations, mining sites, default zones, capturable relays, asteroid fields. If there is no room, place fewer hazards; never throw.
- An already-fired ability (`SurvivesCasterDeath == true`, e.g. IonShot, fired IonPulse) finishes inside a hazard.
- Shapes: superseded by [[Done/Features/Space_Hazards_Rework_Plan|band rework]] (2026-10-28): generated centerline + tapered widths; one sampled polygon for gameplay, ribbon rendering and minimap.
- Membership check: linear scan with bounds reject. Why: 2–6 zones × N ships every 0.1 s is cheap. Avoid: spatial partitioning.
- Immunity flags: not now. Add one bool per effect to unit data when a unit needs it.
- Minimap: hazards always visible, like obstacles; not fogged.
- Path cost: one cost per hazard type, the same for every ship. Why: `ShipPathGrid` cache is shared. Avoid: per-ship-class costs.
- No new global event bus; observers follow `INotifier<T>` / `IObserver<T>` as `BattleMapLoader` does.

## Reuse Map

| Need | Existing |
|---|---|
| Per-ship status flags | `CombatModifiers` (counters + `Changed`) |
| Tick membership pattern | `FleetCommandService` (`_affected`/`_next` sets, cleanup in `LateDispose`) |
| Ability gating | `ShipAbilitySlot.IsAvailable`, `ShipAbilityService.Advance` (~line 233) |
| Shields | `HealthModel.AbsorbsDamage`/`RegenerateShields`, `HealthComponent.Tick`, `ShieldComponent.UpdateState` |
| Shield fade | `Shield.SetActive` + `_FadeDuration` |
| Reinforcement gate | `ReinforcementSpawnRule.IsOpen` |
| AI routes | `ShipNavigationService`, `ShipPathGrid`, `NavigationRouteJob` (blocked cells only today) |
| Generation | `MapLayoutGenerator` → `MapLayout` → `MapLayoutView.BuildAsync` |
| Minimap | `MiniMapObstacle` (ellipse) |
| Footprint | `ShipHullSpan` / `IShipSpawnClearance` |

## Risks

- **Save compatibility.** Saves regenerate the map from `Map.Seed`. Extra draws on the shared `Random` change old layouts → use `new Random(seed ^ HAZARD_SALT)`, after all other generation.
- **Squadron respawn lock.** `Squadron.CanRespawn` fails while `HasTemporaryEffects` is true → keep hazard counters out of `HasTemporaryEffects`; release memberships on `OnDestroy`.
- **Phased abilities bypass the stop check.** IonPulse, IonShot, CompositeBeam, ProtonBeam, FighterCoordination, FighterReinforcements and TractorBeam check `IsIonDisabled` in their own `IsComplete` → add `CombatModifiers.AbilitiesSuppressed` (ion-disabled OR hazard) and switch those call sites.
- **Shield refill leaks.** `HealthModel.Upgrade`, `Restore` and `RefreshStatModifiers` write `Shields` directly → guard with `IsShieldSuppressed`. Do not reuse `IsLostShieldGenerator`.
- **No placement reason API.** `IReinforcementPlacement.IsPositionValid` returns `bool` (4 placements, cheats, AI) → keep the bool; the UI asks `IHazardService.TryGetHazardAt(pos)` for tooltip text.
- **Naming.** "Ion" already means super-weapon/IonPulse stun (`IsIonDisabled`, `IonStunView`) → use `IsShieldSuppressed`, `HazardType.IonStorm`.
- **Visual confusion.** Decorative `MatchNebula` / `SkirmishVolumetricNebulaClouds` already exists → hazards need clear edges and a minimap marker.
- **Map-wide events + reinforcements.** A global event that blocks reinforcements blocks the whole map → default `BlocksReinforcements = false` for events.

## Mining Site Size (current, verified 2026-10-28)

- Commits `ed1fd4e45`, `52418a551`, `6b09368e1`:
  - `CaptureSite.prefab` rocks scaled `1` → `0.5`, local Y `-30` → `-15`. Capture range unchanged: `radius: 60`.
  - `PointOfInterestPlacer.MINING_SITE_SPACING_SHARE` `1` → `0.5` → `1/6` → `0.25`.
  - Home mining: 3 sites in a tight triangle behind the station (was an arc).
- Cluster spread = `(2 × 60 + 15) × 0.25 / √3` ≈ `19.5`. Cluster footprint = spread + capture radius ≈ `79.5` (was ≈ `138`).
- Mining point outer radius = `GetOuterRadius(_miningSiteSpread + _radii.MiningSite, hasPocket: true, size)` = ≈ `79.5` + `ZoneClearance` (`30`) + `PocketThickness`.
- **Rule:** hazards avoid the mining point node (`MapNode` circle via `PointOfInterestPlacer.GetOuterRadius`), not single sites and not rock meshes. Why: capture range (60) is the gameplay area; rocks are now half size and inside it.
- **Avoid:** recomputing cluster size in `HazardPlacer`; it must call the placer's radius so future spacing changes carry over.
- Effect: mining points take ~40% less radius → more free room for hazards → re-tune hazard counts per size.
- Interacts with [[Done/Features/More_Mining_Sites|More Mining Sites]] (8/10/14 sites): run the hazard fit test on 3–4 player maps too.

## Hazard Sources (zones now, events later)

```csharp
// One thing that can apply hazard effects to ships.
public interface IHazardSource
{
    SpaceHazardData Data { get; }
    bool Contains(Vector3 position, float footprintRadius, bool wasInside); // hysteresis via wasInside
}
// Zone: HazardZone (ellipse on the battle plane).
// Event (later): MapHazardEvent → Contains() always true; has Duration/RemainingTime.
```

- `HazardService` keeps `Dictionary<CombatModifiers, List<IHazardSource>>`; effects come from counters across sources, so overlaps never restore early and events stack with zones.
- Events need extra work later: a scheduler (start/end, warning lead time), a save record (`id`, `remainingTime`); zones regenerate from the seed. Global visual is a light tint/particles; no full-screen post effect. Plus an announcer line and HUD banner.

## Visual Spec

Detailed art direction: [[TODOs/Features/Space_Hazards_Plan - Visuals|Space Hazards Plan - Visuals]].

### Style Split (never confusable, even at far zoom)

| | Nebula | Ion Storm |
|---|---|---|
| Read as | sensor-disruption gas field | charged EM storm, shield killer |
| Colors | `#8658B8`, `#B04F95`, dusty pink, blue tint in shadow | `#466AC5`, `#48CBE9`, `#8F62F6`, white highlights |
| Opacity | 0.15–0.28 | 0.18–0.32 |
| Brightness / contrast | medium-low / soft | medium + sharp flash peaks / strong |
| Motion | slow drift, light swirl, lazy dust | faster noise, turbulent, chaotic sparks |
| Electrical | none | veins, arcs every 1.5–3 s (0.15–0.40 s), local flashes |

### Shared Base (one shader, preset overrides)

- One footprint mesh on the battle plane, 2–3 stacked transparent layers at slightly different heights.
- World-space noise (not UV wallpaper), 2–3 layers, independent panning; uneven density.
- Feathered noisy edge inside the gameplay ellipse → drawn edge = gameplay edge.
- Optional: depth fade near geometry, soft fresnel/emission only in dense pockets.
- Ion preset adds: emissive vein channel, pulse mask, `_FlashMultiplier`, faster scroll.
- Arcs: GPU-friendly sprite/mesh strips from a small pool; not one particle system per arc.

### Ship Feedback

- Nebula enter: soft purple pulse around the ship + faint jam ripple; ability icons dim. Shield bubble unchanged.
- Ion enter: shield flicker → fade out (0.2 s, `Shield._FadeDuration`) + blue-white arc across the hull; small periodic hull flicker while inside; UI "Shields Offline".
- Cues fire once per entry (respect hysteresis), never per tick.

### Readability Rules

- Hazard shape visible at max zoom; inner FX readable at medium zoom.
- Ships, selection circles, health bars, team colors, target lines stay readable (ion storm may partially obscure hulls only).
- No full-screen post effects, no volumetrics.

### MVP vs Full

- MVP Nebula: one irregular mesh, 2 noise layers, purple gradient, light dust, entry pulse.
- MVP Ion Storm: nebula base recolored, simple arc prefab, shield collapse on entry, random flash pulse.
- Full: 3 layers, shimmer/distortion, veins, arcs between cloud points and around entering ships, sparks, hull flicker.

### Height (decided 2026-10-28)

- The cloud fills the whole height band where ships exist: from the lowest hull bottom to the highest hull top over all ships.
- Known extremes: Mandator `Deep` -520 (tier asset `deep: -570`), `Highest` tier 80; band ≈ -600 → +150 including `HullBottom`/`HullTop`.
- `SpaceHazardData` gets no height fields; the band comes from one shared setting (`HazardFloor`/`HazardCeiling` in `MapGenerationSettings`, beside `FieldFloor`/`FieldCeiling`).
- Test like `AsteroidFieldHeightTests`: every `ShipData` tier height + `HullBottom`/`HullTop` lies inside the band. A new deeper ship fails the test instead of poking out.
- Gameplay stays planar (XZ); height is visual only.
- Layers: 4–6 transparent sheets spread through the band, each with the same XZ footprint and feathered edge.
- Risk: the tilted camera sees sheets ~750 units apart at different screen positions (parallax) → the top sheet looks shifted from the gameplay edge. Mitigate: densest sheet at ship mid-band (≈ y -60), outer sheets fainter; check at min/max zoom.

### Fog of War (decided 2026-10-28)

- Hazards are never hidden and have no fog-specific look.
- The fog overlay covers them exactly like the planet and asteroid fields; no fog logic in hazard code.
- Render queue `Transparent-50` (as `MapSpaceDust.shader`) so the `URP_FogOfWar` plane (`Transparent`) always draws over them.
- Minimap: same, area visible from match start; the minimap fog mask covers it like obstacles.

### Minimap (decided 2026-10-28)

- Area: tinted ellipse fill + soft outline per hazard (nebula purple `#8658B8`, ion cyan `#48CBE9`), drawn under unit markers. Add to `MiniMapData` beside `Obstacles`, register in `MiniMapUiController.UpdateState(BattleMap)` from `layout.Hazards`.
- Icon at hazard center: generate 2 icons matching the existing minimap icon style, size and import settings.
  - Nebula: cloud glyph.
  - Ion Storm: cloud + lightning bolt.
- Icons go in the UI textures folder per `PROJECT_ORGANIZATION`; sprite refs via `[SerializeField]` on the minimap view.

### Decorative Nebula (decided: keep separate, confirmed by user 2026-10-28)

- Existing `SkirmishVolumetricNebulaClouds` is a far background backdrop (position y -69000, scale 85000), made by the legacy `VolumetricNebulaGenerator`; `MatchNebula` picks a color variant per match.
- Hazards use their own shader and colors. Why: players must tell gameplay hazards from background scenery at a glance.
- Avoid: background nebula variants in hazard purple/blue next to a hazard → consider excluding those variants if they clash in review.

## Implementation

1. [x] **Data.** `SpaceHazardData` SO: `Id`, `HazardType`, radii, `BlocksSpecialAbilities`, `DisablesShields`, `BlocksReinforcements`, `ShieldRecoveryDelay` (2 s), `ExitHysteresis` (m), `PathCost`, visual material. Presets in `Assets/Settings/Data/Models/Map/Hazards/`. `HazardSpot` + `MapLayout.Hazards`.
   - Verify: fixed seed → identical non-hazard layout with/without hazards.
2. [x] **Generation.** `HazardPlacer`, own RNG, mirrored pairs, avoidance list above; hazard count per size in `MapSizeSettings`.
   - Verify: symmetry, clearance, 20 seeds × 2/3/4 players never throw.
3. [x] **Flags.** `CombatModifiers`: `Add/RemoveAbilitySuppression`, `Add/RemoveShieldSuppression`, `AbilitiesSuppressed`. Wire `ShipAbilitySlot.IsAvailable`, `ShipAbilityService.Advance`, the 7 phased `IsComplete` (respect `SurvivesCasterDeath`).
   - Verify: blocked, cooldowns progress, active abilities stop, fired IonShot finishes.
4. [x] **Shields.** `HealthModel.SetShieldSuppressed(bool)`: `Shields = 0`, `AbsorbsDamage` false; guard `Upgrade`/`Restore`/`RefreshStatModifiers`; `HealthComponent` recovery delay then normal regen from 0; tooltip `ShieldRegeneration` = 0 while suppressed.
   - Verify: hull takes damage, no regen while suppressed, delayed regen, max shields + generator hardpoints unchanged.
5. [x] **`HazardService`** (`ITickable`, `ILateDisposable`, in `SkirmishServiceRegistrations`, observes `BattleMap`). 0.1 s tick; footprint from hull radius; enter at radius, exit at radius + hysteresis; per-source counting; release on death/removal.
   - Verify: overlaps, boundary jitter, death inside a zone, squadron respawn after dying inside.
6. [x] **Reinforcements.** `ReinforcementSpawnRule.IsOpen` += `!_hazards.BlocksReinforcement(pos)`; preview tooltip text from `TryGetHazardAt`.
   - Verify: extend `ReinforcementSpawnRuleTests`.
7. [x] **Visuals MVP.** URP HLSL shader (shared base + ion preset) + `NebulaVisualPresenter`; arc prefab + flash; entry cues (nebula pulse, ion shield collapse + hull arc). See Visual Spec → MVP.
   - Verify: Editor captures at min/max zoom (no Play Mode); both types distinguishable side by side.
8. [ ] **Visuals Full.** Third layer, shimmer, veins, ship-targeted arcs, sparks, hull flicker. Optional; after in-game review of MVP.
9. [x] **UI.** Selected-ship status line ("Abilities Jammed" / "Shields Offline"); ability button override tooltip ("Special abilities disabled by nebula" / "Shields and special abilities disabled"); shield bar zero + recovery state; minimap area fill + generated icons (see Visual Spec → Minimap). Read `UI_UX_GUIDELINES` and `UI_CODE_BUILD_GUIDE` first.
10. [x] **AI.** Per-cell extra cost in `NavigationRouteJob` (never blocked); ion-storm avoidance in `EnemyUnitCommander` target picking, weighted for shield-dependent capitals.
   - Verify: existing navigation tests + route bends around a costly cell.
11. **Final** → tests on request and user review moved to [[TODOs/User_Acceptance_Queue]] (2026-10-29).

## Merged Open Work (2026-10-29)

- Single owner of space-hazard work. [[Done/Features/Space_Hazards_Rework_Plan]] (bands, decor ring, dust) and [[Done/Features/Space_Hazards_Shape_Rework_Plan]] (lobed SDF clouds, current geometry) are closed history; current geometry = Shape Rework.
- [ ] Move remaining `HazardType.IonStorm` branches (`HazardCloudView`, `HazardEntryCue`, editor builders) onto data/material (from Shape Rework).
- [ ] Measure SDF bake time per map; target `< 20 ms` total on Large (from Shape Rework step 8).
- Decor ring frame time and 5-sheet overdraw → [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].
- Tests on request (`Map`, `MapExhaustive`, clean full-suite comparison) and user review → [[TODOs/User_Acceptance_Queue]].

## Files (expected)

- New: `Assets/Scripts/Entities/Map/Hazards/` (`SpaceHazardData`, `HazardType`, `HazardSpot`, `IHazardSource`, `HazardZone`), `Assets/Scripts/Entities/Map/Generation/HazardPlacer.cs`, `Assets/Scripts/Services/Hazards/HazardService.cs`, `Assets/Art/Shaders/Vfx/SpaceHazard.shader`, tests in `Assets/Scripts/Tests/Editor/`.
- Touched: `CombatModifiers`, `HealthModel`, `HealthComponent`, `ShipAbilitySlot`, `ShipAbilityService`, 7 phased abilities, `ReinforcementSpawnRule`, `MapLayout`, `MapLayoutGenerator`, `MapLayoutView`, `MapSizeSettings`, `NavigationRouteJob`, `SkirmishServiceRegistrations`, minimap presenter, `ShipAbilityButtonUi`.

## TODO

- [ ] Later: map-wide hazard events (scheduler, save, HUD warning, announcer).

## Verification

- Fixed 2026-10-28 after user reported invisible map hazards: `BattleMapLoader` reconstructed the generated layout while filtering sites and discarded `Hazards`; reconstruction now preserves them. Prior generator and isolated preview tests missed this handoff; previous MVP completion claim was premature.
- Loader regression: 26/26 hazard EditMode tests passed, including real `LoadAsync` publication and spawned visual count after site filtering. Evidence: `Logs/SpaceHazards/loader-fix-tests.json`.
- Actual Play Mode battle: Medium, Republic/CIS, seed `73517`; four active areas at `(±2021.58, 0, ±433.09)` and `(±690.63, 0, ±2345.47)`. Both cloud types render in captures of the loaded scene; no new Console errors. Evidence: `Logs/SpaceHazards/loaded-map-hazard-*-{max,medium}-zoom.png`. Returned Editor to MainMenuScene after verification. User visual/gameplay acceptance remains open.

- 2026-10-28: implementation and saved Unity assets integrated; compilation succeeds. Presets: `Assets/Settings/Data/Models/Map/Hazards/`; map counts 2/4/6, independent RNG selects first type then alternates pairs.
- Full EditMode rerun: 2,386 total, 2,366 passed, 20 failed. `Logs/SpaceHazards/full-editmode-final.json`. Test bridge retained 1,325 individual results; nine listed failures concern balance values/class, Rothana height, squadron icon path, authored waves and radar range. Eleven failure details were not retained; do not claim a green full suite.
- Fixed full-suite integration findings: dependency cycle moved to `HazardEffectsPresenter`; both ship-card prefabs have status bindings; ability icon fixture and navigation-cache reflection fixture updated. Those listed failures pass in the rerun.
- Final hazard-focused rerun: **25/25 passed**. `Logs/SpaceHazards/hazard-tests-final.json`. Covers seeded generation/type variety, geometry, hull-height coverage, source overlaps/death/removal, reinforcement gating, ability cancellation/cooldowns/persistent fired effects, navigation detours and saved UI/visual bindings.
- Editor-only min/max/medium captures reviewed: `Logs/SpaceHazards/`; types distinguishable and ships/team colors readable. Outer-sheet opacity reduced to 0.015 to limit parallax.
- 2026-10-28 user review: hazards too small (240×160 on ±2000–4500 maps), same type on Small, ion visual edge ≈ 70–80% of gameplay ellipse + hull-radius padding. Shape decision superseded → [[Done/Features/Space_Hazards_Rework_Plan|Space Hazards Rework Plan]].
- Remaining: user in-game acceptance (effects, regen delay, fog/UI readability) and full-suite failures above. Step 8 optional full visuals and map-wide events deferred.
