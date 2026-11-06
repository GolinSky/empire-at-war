---
category: Refactoring
status: done
created: 2026-11-06
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Battle Area Access and Removal of NoCaptureSites

## Goal
- Delete `NoCaptureSites` (runtime + test double). Capture-disabled battles register **zero** capture types; no shared service references `ICaptureSitesSystem`.
- One battle-scoped area service owns "who may deploy/build/snap here". Features (relays, capture sites, asteroids, stations) write their areas; placement, overlay and order code read them.
- Evidence → [[Done/Refactoring/Battle_Area_Access - Research]].

## Root cause
- `NoCaptureSites` exists because 8 always-registered services inject the whole feature. Dependency points shared → feature; it must point feature → shared.
- `ICaptureSitesSystem` is a fat interface: 4 events, `Sites`, 6 queries, 2 commands. Each shared consumer uses 1–2 members.
- Asymmetry: relays already **push** blocker/vision circles (`ReinforcementZonesSystem.cs:282–286`); capture sites are **pulled** (`ReinforcementSpawnRule.cs:62`).
- Same smell elsewhere (out of scope): `NoFogOfWar`, `RevealedMiniMapFogMask` in `FogModule.ConfigureDisabled` → [[TODOs/Refactoring/Fog_Disabled_Stand_Ins]].

## Decision
- **Chosen:** grow `SpawnBlockerService` into `BattleAreaService` (`IBattleAreaService` reader + `IBattleAreaWriter`). Storage stays `OwnedCircleSet` keyed by `Transform`; each layer is a separate set.

  | Layer | Rule | Writers | Readers |
  |---|---|---|---|
  | `SpawnBlock` | Blocks teams not allied with owner; `PlayerId.None` blocks all | relays (blocker ring), `AsteroidSpawnBlockers`, `SpawnBlockerSource`, `SpaceStation` | `ReinforcementSpawnRule.IsOpen`, `EnemyStructurePlacementService`, `SpawnAreaOverlay` |
  | `StructureExclusion` | Blocks structures for every team, margin = structure clearance | relay physical ring, capture site ring | `ReinforcementSpawnRule.CanSpawnStructure`, overlay (optional) |
  | `OrderSnap` | Point inside → snap target = circle center | capture site ring | `PlayerOrderInputHandler`, `MiniMapUiController` |
- **Why typed layers, not a free/blocked flag:** answers depend on team (blockers hostile-only, vision allied-only) and action (friendly relay ring gives sight yet blocks structures). Overlapping writers must not clear each other → transform-keyed entries, any match vetoes.
- **Not merged:** `VisionService` (also drives fog render, targeting, tooltips; fog-off flag already lives there). `HazardService` (non-circular shapes; stays a live reader). Fog rendering never grants deployment.
- **Remaining capture consumers → feature plugs into shared extension points.** `CaptureSitesModule` registers contributors; shared code injects `IReadOnlyList<T>`. Unregistered collection resolves empty (verified: `VContainer Registry.cs:179 TryFallbackToSingleElementCollection`). No stub class needed.

  | Consumer (shared) | Extension point | Capture contributor |
  |---|---|---|
  | `WorldTooltipPresenter` (site hover after entity hover) | `IWorldTooltipSource` | `CaptureSiteTooltipSource` |
  | `BattleVictoryService` (constructing site keeps player alive) | `IBattleSurvivalSource` (adds per-player counts) | `ConstructingSitesSurvival` |
  | `FactionAnnouncementService` (4 site events) | none — move the 4 handlers out | `CaptureSiteAnnouncer` in `ConfigurePlayer`, same local + `Economy` gate as `LocalPlayerScope.cs:61` |
  | `EnemyStrategicContextBuilder`, `EnemySquadronCommander` (site objectives, `HasHostileUnits`, `OwnsPointMines`) | `IEnemyCaptureObjectiveSource` | `EnemySiteObjectives` in `ConfigurePlayer` for AI, next to `EnemySiteConstructionController` |
- **Avoid:** an always-registered empty site collection in `SkirmishScope` — it is `NoCaptureSites` renamed; shared code still knows capture.
- **Split `ICaptureSitesSystem`** after migration: feature-internal users (`CaptureSiteMiniMapPresenter`, `EnemySiteConstructionController`, `EnemyDefenseSnapshotBuilder`, save source) keep only what they call.

## Implementation
1. [x] Inventory readers, writers, gates. Source verified 2026-11-06 at `762c4d9ed`.
2. [x] `BattleAreaService`: rename `SpawnBlockerService`/`ISpawnBlockerService` via Serena (keep `.meta` through Unity), add `StructureExclusion` and `OrderSnap` sets, writer API `Register(layer, owner, transform, radius)` / `Unregister(layer, transform)`, `IReadOnlyList<OwnedCircle> Get(layer)` for overlay. Register in `SkirmishScope.cs:226`.
3. [x] Writers. Relays: register physical ring in `StructureExclusion` beside existing blocker writes; drop `IReinforcementZonesSystem.IsPositionInAnyZone` if no reader remains. Capture: register each site's view transform with scaled radius in `StructureExclusion` + `OrderSnap` in `UpdateState`; unregister in `LateDispose`.
4. [x] Fix map replay in `CaptureSitesSystem.UpdateState`: unregister + dispose existing presenters before appending (currently appends per notification). Preserve layout order for save indexes.
5. [x] Readers. `ReinforcementSpawnRule`: drop `IReinforcementZonesSystem` and `ICaptureSitesSystem`; inject area service only. `MiniMapUiController.cs:193` and `PlayerOrderInputHandler.cs:101,183` → `OrderSnap`.
6. [x] Extension points per Decision table: tooltip, survival, announcer, AI objectives. Keep tooltip hover priority and AI snapshot/worker boundaries; keep strategic known-hostile filtering separate from squadron live `HasHostileUnits`.
7. [x] Delete `NoCaptureSites.cs` + `.meta` (Unity-aware delete) and the test double in `EnemyStructurePlacementServiceTests.cs:324`. `ConfigureDisabled` keeps `CaptureSiteData` registration (map generator radii, `MapLayoutView.cs:50`) and `SetActive(false)`. Replace `RealEntityCompositionTests.cs:982` with "`ICaptureSitesSystem` not resolvable; shared services resolve".
8. [x] Overlay: `SpawnAreaOverlay.cs:114` reads `SpawnBlock` from area service; fog-off paints full map instead of vision circles. Hazards/footprint shading optional — preview/commit stay authoritative.
9. [x] Split `ICaptureSitesSystem` (step 6 leftovers). Compile check; source search `NoCaptureSites` → 0 hits. Tests run only when authorized.

## Edge Cases
| Case | Expected |
|---|---|
| Friendly relay: sight + ring | Ship/squadron may deploy; structure touching ring denied |
| Two exclusions overlap, one unregisters | Other still vetoes |
| Fog off | Vision passes everywhere; blockers, hazards, exclusions still veto |
| Capture disabled | No `ICaptureSitesSystem`; empty contributor lists; no site exclusions |
| Capture enabled, 0 sites | Valid; same as above but feature services alive |
| Map replaced / save restored / battle re-entered | No stale circles, no duplicate presenters or handlers; site order = saved index |
| Site under friendly hull | Move order still snaps to site center |
| Preview vs commit | Changed blocker/vision rejects at commit; no charge |

## Verification
- Categories when tests are authorized: `Navigation` (SpawnBlocking), `Capture`, `Economy`, `Vision`, `BattleInput`, `EnemyAi`, `GameFlow`, `Audio`, `Ui`, `Core`.
- Fixtures: `ReinforcementSpawnRuleTests`, `ReinforcementZonesSystemTests`, `RealEntityCompositionTests` (all feature combos), `PlayerOrderInputHandlerTests`, `EnemyStructurePlacementServiceTests`, `EnemyUnitCommanderTests`, `BattleVictoryModelTests`.
- Acceptance: same structure placement result for player, AI and cheat at one point; capture-off battle has no capture types registered.

## Outcome
- `BattleAreaService` replaces the blocker registry. The enabled capture module contributes tooltip, victory, audio and AI behavior. Disabled capture keeps only the layout settings.
- Site replay unregisters both layers, clears selection and capture sampling, detaches event handlers, and rebuilds minimap markers and presenters in save-index order.
- `ICaptureSitesSystem` was removed. Capture-specific consumers depend on directory, presence, events, construction or save state as needed.
- Shared rules extracted to [[Architecture/Map_Generation#Battle area access]].
- Source verification: player, AI and cheat free structure placement call `CanSpawnStructure`; runtime and fixture search under `Assets` found zero placeholder or old service references.
- Unity imported the scripts. The Unity 6000.4.7f1 compiler compiled the current runtime and Editor source sets with the project's Bee references (1,384 and 439 files, zero errors). Live Editor reload later confirmed (see Verification pass). A separate MSBuild attempt failed in the render-pipeline package with CS8168/CS8347.
- Automated tests and in-game acceptance are queued in [[TODOs/User_Acceptance_Queue]].

## Verification pass (2026-11-06)
- Live Editor: `recompile` → up to date; `scriptCompilationFailed` false; 0 console errors. Loaded domain has `BattleAreaService`, `CaptureSiteAnnouncer`, `EnemySiteObjectives`; no `NoCaptureSites`, `ICaptureSitesSystem`, `SpawnBlockerService`.
- Steps 2–9 confirmed in source. `Assets` search for removed names (`.cs`, `.prefab`, `.unity`, `.asset`) → 0. Deleted types were plain C#, so no missing-script references. Every `.cs` in `Services/SpawnBlocking` and `Services/CaptureSites` has a `.meta`.
- Behavior parity: AI site filters equivalent (De Morgan); hover gate same `IHoveredEntityObserver.CanHover`; announcer gate = local + `Economy` (`player.Features`); per-player `CaptureSites` equals battle flag for all roles (`BattlePlayer.cs:22` strips only Economy/SuperWeapons/MiningSites).
- Follow-up fixes (2026-11-06):
  - Site exclusion radius captured once at `UpdateState` → no change needed: site views are instantiated at prefab scale and never rescaled (`MapLayoutView.cs:104`).
  - Removed unused `CaptureSitesSystem.TryGetCaptureTarget/TryGetThreatenedSite/TryGetRaidTarget` and helpers `OwnsPointMines`, `TryGetClosestSite`, `GroundCenter`, plus their 3 tests in `CaptureSitePresenterTests`.
  - `CaptureSiteAnnouncer` → `IFactionAnnouncer` (`Clips`, `Announce`). Cooldowns live once in `AnnouncementCooldowns` (`EVENT` 1.5 s, `CONSTRUCTION` 8 s, `LOSS` 8 s); `FactionAnnouncementService` members back to private.
  - Updated `ICaptureSitesSystem` references in [[TODOs/Features/Defense_Satellites_Plan]] and [[TODOs/Bugs/Defense_Satellites_Review_Fixes]].
  - Live Editor recompile after fixes: completed, 0 errors.
- Tests not run (local override).

## History
- 2026-11-06 rev 2: replaced "shared read-only site collection" with feature-contributed extension points; folded spawn blockers + structure exclusions + order snap into one layered area service; added replay bug step and concrete names.
