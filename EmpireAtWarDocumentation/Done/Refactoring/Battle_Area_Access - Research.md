---
category: Refactoring
date: 2026-11-06
---
# Battle Area Access — Source Investigation

## Files
- Source snapshot: 2026-11-06, checkout HEAD `762c4d9ed`. Paths below are relative to `Assets/Scripts/`. Checked live source using Serena and text searches.
- Related decision: [[Done/Features/Spawn_Blockers_And_Relays]]. Execution plan: [[Done/Refactoring/Battle_Area_Access]].

## Current rules

| Request | Common requirements | Additional requirements |
|---|---|---|
| Squadron placement | Center within inclusive XZ map bounds, allied gameplay vision, no hostile/neutral spawn blocker, no reinforcement-blocking hazard at center | Player preview trigger and final deployment checks still apply |
| Ship placement | Same | Ship-type clearance, including reserved landing space |
| Free structure placement | Same | Physical clearance + no overlap with any relay or site footprint, including friendly ones, expanded by structure clearance radius |
| Build on a captured site | Capture-site construction rules | Uses the reserved site itself. Applying the free-structure veto here would prevent intended construction |
| Move/approach | Move-order rules | Minimap permits approaches to site rocks; orders snap to site centers |
| AI scout / survival ingress | Separate requests | Scouts seek unseen, unblocked positions; survival edge arrivals have their own clearance and hazard checks |

- Policy: `Services/Reinforcement/ReinforcementSpawnRule.cs:45–65`. `ReinforcementService.cs:135,250` validates previews and rechecks each slot on commit. `Entities/Reinforcement/UnitSpawnView.cs:26` also tracks trigger overlap.
- Player placement, `EnemyStructurePlacementService`, `CheatWorldService`, and `ShipSpawnPoints` use the same rule. `EdgeSpawnPoints` serves a different arrival path.
- `Services/OwnedAreas/OwnedCircleSet.cs:20,46`: transform-keyed registrations, team filtering and cached snapshots. Vision and blockers already use this storage independently.
- `Services/ReinforcementZones/ReinforcementZonesSystem.cs:172,280`: physical relay ring excludes structures regardless of owner; its larger blocker and allied vision can change with ownership. Home anchors supply sight. `Services/Hazards/HazardService.cs:106` checks the hazard source's actual shape.
- `Entities/CaptureSites/CaptureSitePresenter.cs:74`: XZ test uses actual center and scaled view radius plus clearance. `CaptureSiteView.cs:45` derives radius from world scale. Keep boundary behavior through the same geometry predicate.

## Issues

- **Shared constructors force a disabled service.** `Services/VContainer/Battle/Modules/CaptureSitesModule.cs:35` disables the component and binds `NoCaptureSites`. Removing only reinforcement's dependency leaves seven other shared consumers. The private test double of the same name also needs removal.
- **Area permission depends on the action and viewer.** A friendly captured relay can supply sight while its own footprint still excludes new structures. Overlapping blockers remain effective when one source disappears. A shared writable Boolean loses both distinctions.
- **Overlay has a second approximation.** `Entities/SpawnArea/SpawnAreaOverlay.cs:114` paints vision and non-allied blockers. It omits hazards, footprint exclusions and collision checks. With fog disabled, `VisionService.IsVisible` grants vision everywhere, but the overlay still paints source circles. This is a source-level discrepancy if shown, not a reproduced player report.
- **Map replay risk.** `CaptureSitesSystem.UpdateState` appends presenters and event handlers per map notification; its teardown clears them at battle disposal. Introducing writes there without replacement semantics can duplicate or retain footprints. Preserve capture save index/order when rebuilding.
- **Disabled feature still needs geometry settings.** `Entities/Map/MapLayoutView.cs:50` reads `CaptureSiteData` for generator radii. `Entities/Map/BattleMapLoader.cs:64` filters all site records on both CaptureSites and MiningSites. Capture enabled with zero sites is distinct from capture disabled.
- **Composition assertion locks in the workaround.** `Tests/Editor/RealEntityCompositionTests.cs:982` asserts the concrete placeholder. Existing parameterized cases include no capture, no mining, no fog, no economy, no features and a wave attacker.

## Consumer migration

| Current consumer | Required behavior | Proposed ownership |
|---|---|---|
| `ReinforcementSpawnRule` | Structure-only exclusion | Shared area reader; site and relay owners publish footprints |
| `MiniMapUiController`, `PlayerOrderInputHandler` | Approach site rocks and snap to site centers, including when a friendly hull covers a site | Read live site geometry from the site collection; keep move rules separate |
| `WorldTooltipPresenter` | Only show a revealed site under the pointer | Read the same site collection and retain hover priority |
| `BattleVictoryService` | Count constructing sites per owner after station loss | Read site state from the collection |
| `FactionAnnouncementService` | Four capture/construction events; other announcements run independently | Subscribe in capture-enabled composition, respecting existing local audio/economy scope, unsubscribe on teardown |
| `EnemyStrategicContextBuilder` | Site objective ID/index, config radius, owned mines and known-hostile intelligence | Keep AI's strategic filtering and worker snapshot path; read ordered sites |
| `EnemySquadronCommander` | Sites and actual hostile presence when assigning squadrons | Keep existing live presence query for real sites, separate from strategic known-hostile intelligence |
| `EnemySiteConstructionController`, `EnemyDefenseSnapshotBuilder`, `CaptureSiteMiniMapPresenter` | Feature-specific commands, selection, presentation | Retain enabled-only registrations and narrow dependencies there as useful |

## Verification
- Source inspection only. Tests, runtime reproduction and profiling were outside this planning pass.
- Relevant existing fixtures: `ReinforcementSpawnRuleTests`, `ReinforcementZonesSystemTests`, `RealEntityCompositionTests`, `PlayerOrderInputHandlerTests`, `EnemyStructurePlacementServiceTests`, `EnemyUnitCommanderTests` and `BattleVictoryModelTests`.


## Rev 2 findings (2026-11-06)
- Per-consumer member use of `ICaptureSitesSystem`: `ReinforcementSpawnRule` → `IsPositionInAnySite`; `MiniMapUiController:193` → `IsPositionInAnySite`; `PlayerOrderInputHandler:101,183` → `Sites` + `Contains` + `Center` (snap); `WorldTooltipPresenter:84` → `Sites`; `BattleVictoryService:155` → `Sites` state; `FactionAnnouncementService:91–94` → 4 events; `EnemySquadronCommander:388–411` → `Sites`, `HasHostileUnits`; `EnemyStrategicContextBuilder:549,691` → `Sites`.
- `EnemyStrategicContextBuilder` and `EnemySquadronCommander` are registered in `AiPlayerScope` unconditionally; `CaptureSitesModule.ConfigurePlayer` already runs inside player scopes (`AiPlayerScope.cs:31`, `LocalPlayerScope.cs:33`) → per-player capture contributors fit there.
- `FactionAnnouncementService` registered only with `Economy` (`LocalPlayerScope.cs:61`).
- VContainer: unregistered `IReadOnlyList<T>`/`IEnumerable<T>` resolves empty (`Library/PackageCache/jp.hadashikick.vcontainer@dcf16cda6a65/Runtime/Registry.cs:179`).
- `ISpawnBlockerService` readers: `ReinforcementSpawnRule`, `EnemyStructurePlacementService:123`, `SpawnAreaOverlay:124`; writers: `ReinforcementZonesSystem:282`, `AsteroidSpawnBlockers`, `SpawnBlockerSource`, `SpaceStation`.
- `CaptureSitesSystem` writes nothing to shared area/vision registries; relays push blocker + vision circles. Sites are pulled.
- Same placeholder pattern: `NoFogOfWar`, `RevealedMiniMapFogMask` (`FogModule.ConfigureDisabled`). Out of scope.
