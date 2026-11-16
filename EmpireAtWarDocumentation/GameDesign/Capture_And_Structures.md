---
type: design-reference
system:
  - capture
  - structures
design_state: implemented
verified: 2026-11-16
verification: Inspected capture-site data and binding, construction dispatch,
  structure catalog times and station stats. Other behavior is described by the
  linked source and its dated checks.
---
# Capture and Structures

## Goal
- Current site and relay capture, reinforcement deployment, site construction, ordinary structure placement, and station rules.
- Related: [[GameDesign/00_GameDesign]], [[GameDesign/Economy]], [[GameDesign/Production_And_Research]], [[GameDesign/Visibility]], [[GameDesign/Combat]].

## Rules

### Capture ownership
- Capture-site states: Neutral → Owned → Constructing → Operational.
- Move eligible ships/squadrons inside the site's planar ring to contribute capture strength.
- Each ship contributes 1; each living squadron contributes 0.5, regardless of its remaining individual fighter count.
- Allied players' strength is combined by team.
- Capture advantage = strongest team's total strength minus the second-strongest team's strength; other teams are not summed against the leader.
- The strongest individual player within the leading team becomes the capturing player; equal individual strength preserves roster iteration order.
- Progress per update = `deltaTime / captureDuration × advantage × CaptureSpeedPerNetShip`.
- Same capturing team → progress carries over if its leading player changes.
- Opposing capturing team → progress restarts from zero.
- No units, allied ownership, or an operational site → capture progress resets.
- Equal leading team strength → capture pauses at its current progress.
- Multiple teams make the site contested, but a nonzero leading advantage still advances capture.

### Construction and destruction
- Capturing grants the location only. A facility must be purchased separately after ownership is established.
- Only Owned sites start construction; each site supports one construction/facility.
- Human construction requires a currently revealed friendly/allied owned site and sufficient credits.
- The selected human player pays; the finished facility is created for the site's owner.
- Facility choices are validated against the site kind. Mining sites allow Asteroid Mine and Battle Asteroid. Satellite sites allow Quad Laser, Turbolaser, Ion, Mass Driver and Missile satellites.
- Construction advances by `deltaTime / BuildTime` and completes at 100%; the entity then spawns at the site's facility position.
- Constructing sites remain capturable; enemy completion of capture deletes construction progress without refund.
- Contested capture alone does not pause construction.
- Operational sites cannot be captured. Destroy facility → site becomes Neutral → capture is available again.
- AI construction on its own empty sites requires the absence of hostile units in the ring. Its fallback builds an Asteroid Mine on eligible mining sites; the objective-driven route uses its selected site and facility.
- Site construction does not enter the ordinary production queue or reserve-pool type limits.

### Structure status icons
- Local mine capture → world reminder remains through construction; completion replaces it. Satellite capture → reminder fades out on successful construction purchase. Ownership loss fades either reminder out.
- Built/destroyed facilities and captured relays → 2 s accumulated displayed world time. Offscreen, hidden HUD, cinematic and modal input lock suspend acknowledgement; fog does not hide icons.
- All status icons: 0.2 s fade-in / 0.3 s fade-out; world transient fades fit inside 2 s. Minimap: independent 0.2 s in + full 3 s hold + 0.3 s out (3.5 s total). New states crossfade artwork in the source's existing slot.
- Battle Asteroid uses the mining-site glyph and timing. Empty-site neutralization never creates a destroyed notification.
- Save/first online snapshot restores local non-operational mining and unpurchased satellite reminders only; no historical transient replay. Map replacement resets presentation and snapshot baselines.
- Startup/map rebuild and live site notifications use `ICaptureSite.FacilityPosition` (`Center + FacilityHeightOffset`); the anchor survives facility release. Generic minimap alerts retain combat only.
- Source/Unity compilation and isolated artwork render verified 2026-11-16; tests and live visual/performance acceptance pending → [[Done/Features/Structure_Status_Icons_Plan]].

### Relay capture and reinforcement deployment
- Relays use `ReinforcementZoneModel`, separately from facility capture sites. Capture immediately changes ownership; it requires no facility purchase or construction.
- Relays remain recapturable. They use the team strength, leading-player and advantage formula above; same-team progress carries over, opposing-team progress restarts, ties pause, and no units reset progress. Allies cannot take one another's relays.
- Relay strength is collected each frame: 1 per ship and 0.5 per living squadron. This does not use the facility site's 0.25 s refresh interval.
- Current relay prefab: capture radius 270 world units; base capture duration 10 s; capture-speed multiplier 1. One uncontested ship → nominal 10 s; one squadron → nominal 20 s. Instance overrides remain authoritative.
- A neutral relay blocks everyone's deployment within 900 world units. After capture, that relay exempts its owner's team and grants allied vision over the same radius; overlapping non-allied blockers still apply.
- Home areas are fixed, noncapturable rally/AI arrival anchors with 270-unit vision. Human deployment is allowed anywhere inside map XZ bounds that is currently visible and unblocked; it is not limited to home or relay circles.
- Human ship placement additionally requires ship-specific hull clearance. Human squadron placement calls `IsOpen` without the ship-clearance check; ordinary structures use the stricter exclusions below.
- Vision and cloak rules: [[GameDesign/Visibility]]. Reserve availability, population and deployment accounting: [[GameDesign/Production_And_Research]].

### Reinforcement placement grid
- Source/Unity compilation verified 2026-11-14; tests and live large-map performance remain pending in [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]].
- Ship placement requires visibility, no hostile spawn block/hazard, actual hull/landing-reservation clearance and a reachable navigation region from the station anchor.
- Station navigation keeps its planar radius but uses the current level's world-space collider bottom/top through `IStationHullFacade`; never use planar radius as vertical reach.
- Ship previews and drops sample grid cell centres (default 256 × 256); maximum snap displacement = half a cell per axis. Batch spacing reserves one cell for rounding.
- Ship preview validity comes solely from the placement rule, avoiding a second trigger-overlap veto. Structures/squadrons retain their existing trigger behavior.
- Physics candidates refresh every 0.1 s; reachability checks its graph/origin/grid cache every 1 s. A changed selection forces an asynchronous Burst batch; initial cells stay blocked until completion. Later refreshes retain the previous mask. Preview/authority still recheck live validity.
- Ship overlay clearance must not call full `CanSpawnShip` per cell: consume existing visibility/blocker/reachability masks and final hazard mask. Union collider/landing candidate bounds into a reusable mask; check each open candidate once with one hull/facing snapshot per redraw. Preview/drop/authority retain full live checks.
- 2026-11-14 performance follow-up: all four phases source-reviewed; full compile blocked by unrelated UI test errors; runtime savings unmeasured → [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]].
- Profile `Battle.SpawnArea.Redraw` and `Battle.SpawnArea.Reachability`; runtime budget is not established by source inspection or compilation.

### Enemy AI map control
- The reinforcement-zone minimum selects viable relays specifically; emergency owned-site defense can interrupt it. After the minimum, zones and facilities compete normally.
- Regrouped retreaters can capture or defend locally without global fleet parity; lost/unsafe home access triggers anchor/objective reassessment.
- Implementation, safety limits and pending runtime acceptance → [[Architecture/Enemy_AI_Objectives]]. Source/compilation checked 2026-10-31; no tests or Play Mode run.

### Ordinary structures and placement
- Ordinary Mining Facility and defense platforms are purchased through production, then placed from the human reinforcement reserve.
- Their placement uses `CanSpawnStructure`: inside map XZ bounds, currently visible, unblocked, outside every relay zone and capture-site ring plus clearance, and clear of units/obstacles.
- The current placement path explicitly excludes capture-site rings; the catalog prose saying ordinary structures are constructed at a capture site does not match that path.
- Structure clearance uses a sphere enclosing the largest registered structure collider, including scale, rotation, and pivot offset.
- Human structure slots stay reserved while queued, awaiting placement, or alive; destruction frees the slot.
- AI searches near its production station, then owned capturable zones, then visible valid positions across the map.
- AI uses the same structure spawn rule, additionally keeps the clearance sphere within map bounds, and avoids its last 10 destroyed structure positions within 1 unit.
- Failure to find an AI structure position causes build failure; the controller refunds and releases its reservation.

## Important Values

### Capture-site defaults
| Setting | Current value |
|---|---:|
| Mining site base radius / duration | 60 world units / 10 s |
| Satellite site base radius / duration | 500 world units / 6 s |
| Capture speed per net ship | 1 |
| Squadron capture weight | 0.5 |
| Strength refresh interval | 0.25 s |

- One uncontested ship → 10 s Mining / 6 s Satellite. One squadron → 20 s / 12 s. Two net ships → 5 s / 3 s.
- Timings assume continuous presence and a fresh strength sample. Sampling can delay recognition of entry or exit.
- `CaptureSiteData` supplies duration and base radius by kind. The view multiplies the radius by the largest absolute X/Z transform scale.

### Site facilities
| Facility | Credits | Build time | Base hull / shields | Income/payment | Spawn block radius |
|---|---:|---:|---|---:|---:|
| Asteroid Mine | 600 | 25 s | 2,500 / 1,200 | 60 | 160 |
| Battle Asteroid | 1,600 | 30 s | 3,500 / 1,800 | 10 | 200 |

- Asteroid Mine and Battle Asteroid pay allied players. Research multipliers and payment cadence are defined in [[GameDesign/Economy]].
- Construction cost/time comes from `CaptureSiteData`; operational stats come from the facility/platform model assets.
- Satellite-site construction (credits / seconds): Quad Laser `300 / 21`, Turbolaser `500 / 25`, Ion `400 / 23`, Mass Driver `1,600 / 47`, Missile `1,000 / 35`.

### Ordinary catalog entries
| Structure | Required level | Credits | Build time | MaxCount | Catalog population |
|---|---:|---:|---:|---:|---:|
| Mining Facility | 0 | 1,000 | 5 s | 5 | 0 |
| XQ-6 | 1 | 1,000 | 5 s | 5 | 0 |
| Golan III | 1 | 8,250 | 50 s | 2 | 3 |
| XQ-3 Empress | 1 | 7,500 | 50 s | 2 | 3 |

- Ordinary Mining Facility base hull/shields: 1,500/800; income: 20/payment; spawn block radius: 400.
- Faction roster membership determines which catalog entries are offered. These rows describe the shared catalogs.
- Human/AI population handling differs for structures; see [[GameDesign/Production_And_Research]].

### Stations
- Shared station base values: 36,000 hull, 24,000 shields, 240 shield regeneration value, 3 s regeneration delay.
- Station base radar detection range and fog vision radius: 900 world units each; hostile spawn block radius: 1,200 world units.
- Station mounts use their own `profile.Range × VisionMultiplier`; `StationCombatPresenter` forwards radar-acquired enemies but does not give every weapon a 900-unit reach.
- Level 1–5 hull multipliers: 1, 1.4, 1.9, 2.6, 3.5.
- Level 1–5 shield multipliers: 1, 1.5, 2.2, 3.1, 4.2.
- Level 1–5 shield-regeneration multipliers: 1, 1.15, 1.35, 1.6, 1.9.
- Level changes apply health upgrades and, when configured, swap the level model and update combat bounds.
- Station construction/upgrades and loss of production access are defined in [[GameDesign/Production_And_Research]].

## Edge Cases
- Team ownership prevents allies taking sites from one another; paying for an allied site's facility does not transfer ownership.
- Facility death returns the site to neutral, rather than transferring it directly to the attacking player.
- A capture completion is processed before construction completion in each site update; an enemy capture that completes first cancels that build.
- Once entered, a site remains known terrain; current vision is required for updated ownership rendering and the human build selection.
- Generated map loading removes all sites when `CaptureSites` is disabled and removes mining-layout sites when `MiningSites` is disabled.
- The 2026-11-16 check covers site settings, construction dispatch and authored structure values.

## Files
- `Assets/Scripts/Entities/ReinforcementZones/ReinforcementZoneModel.cs`; `Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs` — relay capture, per-frame strength, ownership, home/relay vision and blockers.
- `Assets/Scripts/Services/SpawnBlocking/BattleAreaService.cs`; `Assets/Scripts/Services/OwnedAreas/OwnedCircleSet.cs` — non-allied circle exclusions, including neutral blockers.
- `Assets/Scripts/Services/Reinforcement/ShipReinforcementPlacement.cs`; `Assets/Scripts/Services/Reinforcement/SquadronReinforcementPlacement.cs` — human deployment checks.
- `Assets/Prefabs/View/ReinforcementZones/ReinforcementZone.prefab`; `Assets/Settings/Data/ReinforcementZones/ReinforcementZoneData.asset` — relay radius, duration, weights and spawn-block/vision radii.
- `Assets/Scripts/Entities/BaseEntity/EntityComponentData.cs`; `Assets/Scripts/Entities/SpaceStation/SpaceStationInstaller.cs`; `Assets/Scripts/Components/Weapon/StationCombatPresenter.cs`; `Assets/Scripts/Components/Weapon/WeaponComponent.cs` — radar/vision binding and per-mount weapon range.
- `Assets/Scripts/Entities/CaptureSites/CaptureSiteModel.cs` — `TickCapture`, `TickConstruction`, `ReleaseFacility`.
- `Assets/Scripts/Entities/Players/CaptureStrengthBuilder.cs`; `Assets/Scripts/Services/Ship/ShipService.cs`; `Assets/Scripts/Services/Squadron/SquadronRegistry.cs` — team/ship/squadron strength.
- `Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs` — update order, human/AI build selection, strength sampling.
- `Assets/Scripts/Entities/CaptureSites/CaptureSitePresenter.cs`; `Assets/Scripts/Services/CaptureSites/SiteFacilityBuilder.cs` — visibility, payer, owner, spawn/destruction.
- `Assets/Scripts/Services/Reinforcement/ReinforcementSpawnRule.cs`; `Assets/Scripts/Services/Reinforcement/StructureSpawnClearance.cs`; `Assets/Scripts/Services/Enemy/EnemyStructurePlacementService.cs` — placement constraints.
- `Assets/Scripts/Services/Reinforcement/MiningFacilityReinforcementPlacement.cs`; `Assets/Scripts/Services/Reinforcement/DefendPlatformReinforcementPlacement.cs` — placement and slot release.
- `Assets/Scripts/Entities/SpaceStation/SpaceStation.cs`; `Assets/Scripts/Entities/Map/BattleMapLoader.cs` — station level changes and map feature filtering.
- `Assets/Prefabs/View/CaptureSites/CaptureSite.prefab`; `Assets/Prefabs/View/CaptureSites/SatelliteCaptureSite.prefab` — mining and satellite view bindings.
- `Assets/Scripts/Entities/Map/MapLayoutView.cs`; `Assets/Scripts/Entities/CaptureSites/CaptureSiteView.cs`; `Assets/Settings/Data/Models/CaptureSites/CaptureSiteData.asset` — settings binding, radius, duration, weights and facility construction.
- `Assets/Settings/Data/Models/MiningFacilities/MiningFacilityData.asset`; `Assets/Settings/Data/Models/MiningFacilities/AsteroidMiningFacilityData.asset`; `Assets/Settings/Data/Models/DefendPlatform/AsteroidDefendPlatformData.asset` — facility stats.
- `Assets/Settings/Data/Factions/Shared/MiningFacilityCatalog.asset`; `Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset`; `Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset` — ordinary structure/catalog/station values.
