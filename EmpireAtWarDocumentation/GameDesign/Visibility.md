---
type: design-reference
system:
  - visibility
  - fog
  - radar
  - stealth
design_state: implemented
verified: 2026-11-16
verification: Source and affected fixtures reviewed; Unity compilation completed
  without errors; 72 ship hull outlines baked, saved and read back. Tests, live
  gameplay, multiplayer and CPU profiling not run.
---
# Visibility

## Goal

- Current team vision, fog presentation, radar detection and cloak rules.
- Hull/firing visibility snapshot: 2026-11-16. Other settings retain the 2026-10-20 reference; no runtime acceptance or CPU capture ran.

## Rules

### Team vision

- Vision is a set of owned circles in the XZ plane. Height does not change the circle containment test.
- A viewer uses allied circles; any matching circle reveals the queried position or footprint.
- Point visibility uses radius 0. Legacy circular areas add the target reveal radius to the source radius.
- Ship visibility uses overlap between allied vision circles and the baked convex XZ hull outline. Any overlap reveals the whole ship and permits targeting within existing radar/weapon limits.
- The circle test does not raycast terrain/obstacles for line-of-sight occlusion.
- Ship vision comes from its radar range; it is refreshed when combat modifiers change.
- Squadron vision is registered by its flight component. Stations and other structures register their configured vision ranges.
- Home areas reveal their configured circle. Captured relays provide their owner/team vision until ownership changes.
- With the Fog feature disabled, all position/area visibility queries return true; Simulation disables fog.
- [[GameDesign/Capture_And_Structures#Relay capture and reinforcement deployment|Capture and Structures]] owns relay capture and reinforcement placement rules; [[GameDesign/Modes_And_Victory]] owns mode feature flags.

### Entity presentation

- Friendly entities use an always-visible fog facade. Hostile entities use `FogVisibilityComponent`.
- A hostile ship is hidden when cloaked, or when neither hull overlap nor a current firing reveal grants visibility. Other hostile entities retain their existing point/circular rules.
- `FogVisibilityComponent` updates renderers/ion-stun effects on the slow fog tick. Cloak and matching firing-reveal transitions refresh synchronously.
- All four faction station view prefabs set `staysRevealedOnceSeen = true`: once revealed, those station renderers stay drawn.
- Ships use `ShipData.VisionHull` through `IVisionFootprintFacade`; their legacy reveal radius no longer controls hull detection. Ships do not stay permanently revealed.
- Station discovery uses its footprint radius; vision need not reach the station's center.
- Small-ship battle markers use the hull fog facade and visibility event. Friendly markers stay visible in cloak.
- Squadron battle markers sample allied point vision at 10 Hz. Enemy cloak and hull concealment immediately remove the marker from selection and group layout. Hidden members never contribute to the displayed group center.
- Ship minimap markers use the same hull/firing query; matching firing-reveal transitions refresh immediately. Friendly markers remain visible.

### Radar and targeting

- Radar detects living, nonempty hostile entities within planar range and excludes cloaked targets.
- Effective radar range = configured `IRadarData.Range × CombatModifiers.VisionMultiplier`.
- Radar refresh delay comes from unit data; detection and fog are separate queries.
- Ship weapons, Hunt, squadron targeting, targeted abilities and entity-aware AI queries use `VisionQueries.IsVisible(viewer, entity)`. Radar contact alone does not establish visibility.
- Attack Move, Guard, Hunt, ship attack and squadron targeting explicitly drop or reject cloaked targets.
- Point/ground queries and per-fighter checks retain their existing rules. Radar and weapon range are separate; hull visibility does not increase their ranges.
- [[GameDesign/Combat]] owns weapon range and targeting details.

### Firing reveal

- An authoritative actual weapon emission reveals the firing ship to the attacked player and allies for **5 scaled seconds after its last shot**; misses also reveal.
- Proton/Composite Beam emission and live damage ticks refresh the timer. Ion Shot requires an emitted projectile; attack orders, target assignment and failed emission do not reveal.
- Firing reveal exposes only that ship. It does not reveal nearby ships or clear terrain fog; cloak still wins.
- The host owns expiry. Reliable reveal/hide transitions are coalesced per entity/viewer; repeated shots refresh the timer without a packet per projectile.
- Reconnect baseline sends current reveals after entity spawns. Clients retain reveal state until the host hides it; despawn clears state for reused IDs.

### Cloak

- Cloak sets `CombatModifiers.IsCloaked`; ending the ability clears it.
- Hostile cloak hides hull/shield presentation through fog visibility and removes radar detectability.
- Friendly cloak uses the assigned cloak material for its hull and suppresses the shield renderer; allies can still locate the unit.
- Cloaked weapon owners cannot start the checked firing paths; target selection excludes cloaked targets.
- The ability service prevents starting other abilities while cloaked and rejects cloaked enemy ability targets.
- Hostile minimap unit markers require current vision and a non-cloaked target; friendly markers remain visible.
- Durations, cooldowns and eligibility belong to [[GameDesign/Abilities_And_Superweapons]].

## Implementation

- `ShipVisionHullBaker.BakeAll()` bakes explicitly bound hull meshes into `ShipData.visionHull`; all 72 mapped ship data assets are saved. Executor: 26 points; Mandator: 21.
- Persistent prefab assets require authored `activeSelf` ancestor checks; `activeInHierarchy` is false outside a scene. Disabled renderers/inactive helpers are excluded.
- `ShipVisionFootprint` reuses world vertices/bounds and refreshes only when its root matrix changes. Vision uses cached allied circles, circle/bounds rejection, then polygon edges/containment; no runtime mesh scan.
- Firing expiry scans only active reveal records. Target capture retains its per-entity/viewer visibility cache; no additional fleet scan or per-projectile network message.

## Important Values

- Home vision radius: 270 world units. Captured relay vision radius: 900 world units.
- Station reveal footprint radii below are serialized world-unit values, rounded for reading.

| Station prefab | Reveal radius | Stays drawn after discovery |
|---|---:|---|
| Republic | 211.65738 | Yes |
| Separatist | 318.19165 | Yes |
| Empire | 341.08334 | Yes |
| Rebellion | 237.78355 | Yes |

- Example current unit: Stealth Corvette radar range 550 world units, scan delay 0.25 s; do not use this as a global default.

## Edge Cases

- Convex outlines approximate concave gaps. The root-relative rest-pose outline follows rotation/scale but deliberately ignores cosmetic body banking.
- A rotated hull remains visible while it overlaps vision; rotation that genuinely removes the last overlap can hide it after firing reveal expires.
- Re-bake vision hulls after changing ship hull geometry or its prefab transforms.
- Live fog-edge visuals, five-second return fire, online/reconnect behavior and matched CPU workload remain user acceptance gates; source inspection is not a passing test run.

- Disabling fog reveals positions but does not remove the separate cloak checks in radar/target selection.
- Permanent station renderer discovery is distinct from current vision-circle coverage.
- Save files do not store fog-grid or station-discovery presentation state; vision is rebuilt from restored entities and relays: [[GameDesign/Save_And_Load]].
- Current values describe saved configuration, not validated sight-range balance or complete runtime rendering behavior.

## Files

- `Assets/Scripts/Services/Vision/{VisionService,VisionQueries,VisionHullGeometry}.cs`; `Assets/Scripts/Services/OwnedAreas/OwnedCircleSet.cs`.
- `Assets/Scripts/Entities/Ship/EntityFacades/ShipVisionFootprint.cs`; `Assets/Scripts/Editor/Ship/ShipVisionHullBaker.cs`; `Assets/Scripts/Entities/Ship/Data/ShipData.cs`.
- `Assets/Scripts/Services/Networking/{HostVisionRevealSender,ClientVisionRevealReceiver}.cs`; `Assets/Scripts/Services/Networking/Messages/VisionRevealMessage.cs`.
- `Assets/Scripts/Services/VContainer/Battle/Modules/FogModule.cs` — enabled/disabled fog behavior.
- `Assets/Scripts/Components/FogOfWar/FogVisibilityComponent.cs`; `Assets/Scripts/Entities/BaseEntity/EntityFeatureBindings.cs`.
- `Assets/Scripts/Components/Radar/RadarModel.cs`; `Assets/Scripts/Services/Radar/RadarDetectionJob.cs`; `Assets/Scripts/Services/Radar/RadarScanService.cs`.
- `Assets/Scripts/Components/Weapon/TargetSelectionCapture.cs`; `Assets/Scripts/Components/Weapon/WeaponComponent.cs`.
- `Assets/Scripts/Services/ShipAbilities/Abilities/Cloak/CloakAbility.cs`; `Assets/Scripts/Components/ViewComponents/CloakView.cs`.
- `Assets/Scripts/Entities/MiniMap/Presenter/MiniMapUnitMarkerPresenter.cs`.
- `Assets/Settings/Data/ReinforcementZones/ReinforcementZoneData.asset`; `Assets/Settings/Data/Ship/Republic/StealthCorvetteShipData.asset`.
- `Assets/Prefabs/Models/Stations/Republic/RepublicSpaceStationView.prefab`; `Assets/Prefabs/Models/Stations/Separatist/SeparatistSpaceStationView.prefab`.
- `Assets/Prefabs/Models/Stations/Empire/EmpireSpaceStationView.prefab`; `Assets/Prefabs/Models/Stations/Rebellion/RebellionSpaceStationView.prefab`.
- [[GameDesign/00_GameDesign]].
