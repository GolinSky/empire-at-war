---
type: design-reference
system:
  - visibility
  - fog
  - radar
  - stealth
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
---
# Visibility

## Goal

- Current team vision, fog presentation, radar detection and cloak rules.
- Source/asset snapshot: 2026-10-20. No runtime visibility acceptance test ran for this reference.

## Rules

### Team vision

- Vision is a set of owned circles in the XZ plane. Height does not change the circle containment test.
- A viewer uses allied circles; any matching circle reveals the queried position or footprint.
- Point visibility uses radius 0. Footprint visibility adds the target reveal radius to the source radius.
- The circle test does not raycast terrain/obstacles for line-of-sight occlusion.
- Ship vision comes from its radar range; it is refreshed when combat modifiers change.
- Squadron vision is registered by its flight component. Stations and other structures register their configured vision ranges.
- Home areas reveal their configured circle. Captured relays provide their owner/team vision until ownership changes.
- With the Fog feature disabled, all position/area visibility queries return true; Simulation disables fog.
- [[GameDesign/Capture_And_Structures#Relay capture and reinforcement deployment|Capture and Structures]] owns relay capture and reinforcement placement rules; [[GameDesign/Modes_And_Victory]] owns mode feature flags.

### Entity presentation

- Friendly entities use an always-visible fog facade. Hostile entities use `FogVisibilityComponent`.
- A hostile entity is hidden when cloaked or outside local allied vision.
- The visibility component toggles its configured renderers and tracked ion-stun renderers on a slow update.
- All four faction station view prefabs set `staysRevealedOnceSeen = true`: once revealed, those station renderers stay drawn.
- Ordinary ship prefabs inspected use `staysRevealedOnceSeen = false` and reveal radius 0.
- Station discovery uses its footprint radius; vision need not reach the station's center.
- Squadron icon/minimap systems own their marker presentation; do not infer marker visibility solely from a hull renderer.

### Radar and targeting

- Radar detects living, nonempty hostile entities within planar range and excludes cloaked targets.
- Effective radar range = configured `IRadarData.Range × CombatModifiers.VisionMultiplier`.
- Radar refresh delay comes from unit data; detection and fog are separate queries.
- Player weapon target selection rejects fog-hidden targets where `respectsLocalFog` is enabled; radar contact alone does not establish player visibility.
- Attack Move, Guard, Hunt, ship attack and squadron targeting explicitly drop or reject cloaked targets.
- Local Hunt also respects fog; target-selection paths should not be assumed to share identical visibility policy.
- [[GameDesign/Combat]] owns weapon range and targeting details.

### Cloak

- Cloak sets `CombatModifiers.IsCloaked`; ending the ability clears it.
- Hostile cloak hides hull/shield presentation through fog visibility and removes radar detectability.
- Friendly cloak uses the assigned cloak material for its hull and suppresses the shield renderer; allies can still locate the unit.
- Cloaked weapon owners cannot start the checked firing paths; target selection excludes cloaked targets.
- The ability service prevents starting other abilities while cloaked and rejects cloaked enemy ability targets.
- Hostile minimap unit markers require current vision and a non-cloaked target; friendly markers remain visible.
- Durations, cooldowns and eligibility belong to [[GameDesign/Abilities_And_Superweapons]].

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

- Disabling fog reveals positions but does not remove the separate cloak checks in radar/target selection.
- Permanent station renderer discovery is distinct from current vision-circle coverage.
- Save files do not store fog-grid or station-discovery presentation state; vision is rebuilt from restored entities and relays: [[GameDesign/Save_And_Load]].
- Current values describe saved configuration, not validated sight-range balance or complete runtime rendering behavior.

## Files

- `Assets/Scripts/Services/Vision/VisionService.cs`; `Assets/Scripts/Services/OwnedAreas/OwnedCircleSet.cs` — allied XZ circles.
- `Assets/Scripts/Services/VContainer/Battle/Modules/FogModule.cs` — enabled/disabled fog behavior.
- `Assets/Scripts/Components/FogOfWar/FogVisibilityComponent.cs`; `Assets/Scripts/Entities/BaseEntity/EntityFeatureBindings.cs`.
- `Assets/Scripts/Components/Radar/RadarModel.cs`; `Assets/Scripts/Services/Radar/RadarDetectionJob.cs`; `Assets/Scripts/Services/Radar/RadarScanService.cs`.
- `Assets/Scripts/Components/Weapon/TargetSelectionCapture.cs`; `Assets/Scripts/Components/Weapon/WeaponComponent.cs`.
- `Assets/Scripts/Services/ShipAbilities/Abilities/Cloak/CloakAbility.cs`; `Assets/Scripts/Components/ViewComponents/CloakView.cs`.
- `Assets/Scripts/Entities/MiniMap/Presenter/MiniMapUnitMarkerPresenter.cs`.
- `Assets/Settings/Data/ReinforcementZones/ReinforcementZoneData.asset`; `Assets/Settings/Data/Ship/Republic/StealthCorvetteShipData.asset`.
- `Assets/Prefabs/Models/Stations/RepublicSpaceStationView.prefab`; `Assets/Prefabs/Models/Stations/SeparatistSpaceStationView.prefab`.
- `Assets/Prefabs/Models/Stations/EmpireSpaceStationView.prefab`; `Assets/Prefabs/Models/Stations/RebellionSpaceStationView.prefab`.
- [[GameDesign/00_GameDesign]].
