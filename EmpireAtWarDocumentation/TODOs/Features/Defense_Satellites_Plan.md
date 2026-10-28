---
category: Features
status: in-progress
created: 2026-10-28
---
# Defense Satellites

## Goal

- New capturable **satellite pads**: 3 pads around every mining point (3-mine triangle), placed as a second triangle.
- Capture pad (same rules as mining) → select → pick 1 of 5 satellite types → pay → satellite auto-attacks enemies.
- Global AI builds the best-suited satellite per pad from game state (economy, hostiles, own fleet, location).
- Relay, mining and satellite zones share one capture implementation.

## Status

- Implemented 2026-10-28: shared capture, 31 imported models, five satellite entities, dual-triangle pads, dynamic build menu, AI strategy, save/load and registrations.
- Automated verification recorded below; user in-game acceptance remains open. No agent Play Mode.
- Placement: `SatellitePadOffset = 120`, pad radius `20`; mine prefab scale is included in clearance.
- Runtime team color uses `UnitRenderStateComponent` (current renderer implementation).
- AI uses marginal `CombatMatchup` effectiveness with the own fleet to value shield stripping and piercing. Raw DPS/cost alone made expensive types unreachable with current profiles.
- Pads cannot overlap hazards; no separate AI hazard penalty or hazard-service dependency is needed.
- Import evidence: `Tools/Blender/DefenseSatellites/ImportEvidence.json`; 31 FBX round trips, maximum geometry error `0.00000853` source units; five 512×512 icons and eight-team previews.

## User Decisions (2026-10-28)

- Capture: every ship can capture (same as mining; squadrons keep `SquadronCaptureWeight`).
- Start owner: Skirmish pads start Neutral; Survival pads start owned by the player. Code must support spawning any pad with an owner.
- Survival: pads around its mining zones.
- Satellite hull `1000`, shields `1000`.
- Hardpoint health: agent's choice, unified with the existing system. Destroyed turrets are **not** repaired.
- Weapons: Quad laser = anti-fighter (closest existing analog); Turbolaser = medium dual turbolaser; others = closest existing analogs.
- Costs: Quad laser `300`, Turbolaser `500`, Ion `400`, Massdriver `1600`, Missile `1000`.
- Zone visual: captured empty site **blinks (alpha, 1 s period)** until something is built; once built, the zone visual **disappears**. Replaces the "green ring" idea.
- AI: no fixed rule ("Massdriver at home"); a clear defense strategy that picks the right weapon for each case from game state.

## Shared Capture Code — Before Implementation (2026-10-28)

- **No shared capture zone component.** Shared today:
  - `CaptureStrength` / `CaptureStrengthBuilder` (`Entities/Players/`) — tally.
  - `IShipService.AddShipStrength(contains, builder)`, `ISquadronRegistry.AddSquadronStrength(contains, weight, builder)` (from [[TODOs/Refactoring/Capture_Reinforcement_Tally_Dedup|Tally Dedup]]).
- **Duplicated** between relays and capture sites:

| Concern | Relay | Mining/battle site |
|---|---|---|
| Capture progress tick | `ReinforcementZoneModel.Tick` | `CaptureSiteModel.TickCapture` (near-identical body) |
| Starting owner | ctor `startingOwner` | none (always `PlayerId.None`) |
| Capturable flag | ctor `isCapturable` | `State != Operational` |
| Strength refresh loop | `ReinforcementZonesSystem.TickZones` (0.25 s, cached `_zoneContains`) | `CaptureSitesSystem.Tick` (0.25 s, lambda per site) |
| Ring coloring | `ReinforcementZoneView.GetColor` (5 colors, `sphereRenderer`) | `CaptureSiteView.GetColor` (5 colors, `ringRenderer`) |
| Settings | `ReinforcementZoneData.SquadronCaptureWeight`, capture speed | `CaptureSiteData.SquadronCaptureWeight`, `CaptureSpeedPerNetShip` |
| Minimap | `ReinforcementZoneMiniMapPresenter` | `CaptureSiteMiniMapPresenter` |

- Behavior differences to preserve: relay `IsContested` set even when not capturable; site only while capturable. Site resets progress when not capturable.

### Decision: extract shared capture parts (Phase 0)

- `CaptureControl` (pure C#, `Entities/Capture/`): `Owner`, `CapturingPlayer`, `CaptureProgress`, `IsContested`; ctor `(IPlayerRelations, PlayerId startingOwner, float captureDuration, float captureSpeedPerNetShip)`; `bool Tick(float dt, CaptureStrength tally, bool isCapturable)`; `Restore(...)`; `SetOwner(PlayerId)` / `Release()`.
  - `ReinforcementZoneModel` and `CaptureSiteModel` own one each; site keeps construction state on top.
  - Why: one capture rule for relay/mining/satellite; gives sites a starting owner for free.
  - Avoid: a base class for both models/systems (Tally Dedup decision); merging the two systems.
- `CaptureStrengthSampler` (pure C#): 0.25 s refresh, cached `Func<Vector3,bool>` per area, ship + squadron strength → `CaptureStrength[]`. Used by both systems.
- `CaptureRingView` (MonoComponent): ring renderer, relation palette, contested color, blink, hide. Used by `ReinforcementZoneView` and `CaptureSiteView` via `[SerializeField]`.
  - Palette values stay per prefab (relay and site colors differ today).
- Keep separate: systems (relay registration vs construction), data assets (different weights allowed), minimap presenters.
- Tests first: characterize current relay + site capture behavior (tie, ally, contested, progress carry-over), then refactor → same tests green.
- `CaptureSitesSystem.cs` 353 lines → sampler extraction shrinks it; satellite logic must not grow it (AI policy lives in Enemy services).

## Source Facts (AOTR 1397421866, reference only)

| Item | Value |
|---|---|
| Base model | `Data/ART/MODELS/Turret_Space_Base.alo` |
| Weapon models | `Turret_Space_<QL\|TL\|Ion\|Massdriver\|Missile>_00..05.ALO` → base bones `TURRET_00..05` |
| Optional extras | `Turret_Space_Buildpad.ALO` (+ `_IDLE_00.ALA`), `Turret_Space_<X>_UC.alo` + `_UC_Build_00.ALA` |
| Source satellite | hull `1500`, shields `1000`, build `25 s`; turret health `250` |

| Satellite | Source projectile | Recharge s | Pulses | Range |
|---|---|---|---|---|
| Quad laser | `Proj_Space_PD_Repeater_Quad_Green` | 9.443 | 6 | 2100 |
| Turbolaser | `Proj_Space_TL_Light_Dual_Green` | 8.858 | 1 | 3750 |
| Ion | `Proj_Space_IonCannon_Stun_Light_LR` | 15.295 | 1 | 3750 |
| Massdriver | `Proj_Ship_MassDriver_Light` | 6.65 | 1 | 4125 |
| Missile | `Proj_Ship_Assault_Missile` | 19.95 | 2 | 3750 |

- XML: `Data/XML/SPACEBUILDABLESSKIRMISH.XML` (`Template_Defense_Satellite`), `Data/XML/Hardpoints_Starbases.xml` (`HP_DefSat_*`).

## Decision

### Satellite types

| Satellite | `SiteFacilityType` | `DefendPlatformType` | `WeaponType` ×6 | `HardPointType` | Cost | Role |
|---|---|---|---|---|---|---|
| Quad laser | `QuadLaserSatellite = 2` | `QuadLaserSatellite = 4` | `DualRepeatingPointDefense` | `PointDefense` | 300 | anti-fighter/bomber |
| Turbolaser | `TurbolaserSatellite = 3` | `TurbolaserSatellite = 5` | `DualMediumTurboLaser` | `Weapon` | 500 | generalist anti-ship |
| Ion | `IonSatellite = 4` | `IonSatellite = 6` | `MediumIonCannon` | `Weapon` | 400 | shield strip / disable |
| Massdriver | `MassDriverSatellite = 5` | `MassDriverSatellite = 7` | `MassDriver` | `Weapon` | 1600 | long-range anti-capital |
| Missile | `MissileSatellite = 6` | `MissileSatellite = 8` | `HeavyAssaultMissile` | `Weapon` | 1000 | burst anti-hull (cruiser/capital) |

- Analogs: Quad laser ← source PD repeater; Missile ← source `Proj_Ship_Assault_Missile`; Massdriver/Ion/Turbolaser direct. Enums append only.
- Build time scales with cost (user 2026-10-28): `time = 15 s + cost / 50` → Quad laser `21 s`, Ion `23 s`, Turbolaser `25 s` (= source), Missile `35 s`, Massdriver `47 s`. Stored per type in `SiteFacilityCost` (no runtime formula).
- Ranges/damage from existing weapon profiles (post-rebalance), not source distances.

### Health (unified with existing platforms)

- `EntityComponentData`: hull `1000`, shields `1000`; shield regen = existing platform values.
- `hardPointHealth` matches every other platform/mining asset: `Weapon` `300`, `hullDamageMultiplier` `1`; add `PointDefense` `300` for the Quad laser.
- 6 hardpoints in `HealthComponent.ShipUnits` (all targetable — project rule).
- No repair: `HardPointModel.Repair` already returns when `IsDestroyed`. Add a test that a destroyed satellite turret stays at 0 under hull/shield regen and repair abilities.

### Site kind, owner, facilities

- `CaptureSiteKind { Mining = 0, Battle = 1, Satellite = 2 }` replaces the implicit `SiteSpot.FacilityType` kind; `MapNodeKind.SatelliteSite = 4`.
- `CaptureSiteData`: `kind → allowed SiteFacilityType[]`; Mining → `Mining, BattleAsteroid` (unchanged); Satellite → 5 types; costs in `facilityCosts`.
- Starting owner: `SiteSpot.Owner` (from `MapNode.owner`, already a field) → `CaptureControl` starting owner → site state `Owned` (empty) when owner ≠ None.
  - Skirmish placer: `PlayerId.None`. Survival generator: player id.
- Build menu (`CaptureSiteBuildUi`/`Controller`): rows generated from the selected site's allowed list; replaces the two hardcoded rows (lines 79–80).

### Placement ("second triangle")

- Per mining point (home + contested + Survival ring): 3 pads at the dual triangle — same center, `+60°` from the mining triangle, radius `_miningSiteSpread + SatellitePadOffset`.
- Home: rotation from the station `corner` vector; contested: same random `turn + 60°`; mirror indices like `TryAddMiningPairs`.
- `MapGenerationSettings.SatellitePadOffset` (single source); pad radius from `satelliteSitePrefab.Radius` via `MapFeatureRadii`.
- `IsClear` vs other points, zones, stations, hazard bands; failure → layout retry (never drop pads).
- `BattleSaveValidator.ValidateMapCounts`: satellite pads = mining sites. Old saves rejected (count mismatch).

### Zone visual: blink → hide

- State-driven, in `CaptureRingView`:

| Site state | Local relation | Ring |
|---|---|---|
| Neutral | any | relation color, steady |
| Capturing / contested | any | capture/contested color, steady |
| Owned, empty | own | **blink**: alpha `0 ↔ base` with 1 s period (`Mathf.PingPong` on unscaled time) |
| Owned, empty | ally/enemy | relation color, steady |
| Constructing / Operational | any | **hidden** |
| Facility destroyed → Neutral | any | shown again |

- Applies to mining and satellite sites. Relays are never "built" → unchanged.
- Hidden ring still keeps its `Contains` hit area for selection/minimap; status canvas unchanged.
- Confirmed 2026-10-28: ring hidden while Constructing is fine (capture progress shows in the status bar). Blink uses unscaled time.

### Minimap

- `CaptureSiteMiniMapPresenter`: satellite pad marker; neutral pads shown as capturable objectives; owned pads relation-colored.

### Entity

- Satellite = `DefendPlatform` entity (health/shields, radar, weapons, stationary combat, fog, selection, save facade, tooltip); `Income = 0`.
- `SiteFacilityBuilder.Build`: satellite types → `SatelliteDefendPlatformFactory` with matching `DefendPlatformType`.
- Destroyed → `onDestroyed` → `ReleaseFacility` → Neutral (existing).
- Faction-agnostic; team color via `UnitRenderStateComponent`.

### Visuals / import

- Follow [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE|ALO Model Import Guide]] steps 1–9.
- Import `Turret_Space_Base` + 30 weapon ALOs; weapons attached at base bones `TURRET_00..05` (positions from the rig).
- Pad prefab `Assets/Prefabs/View/CaptureSites/SatelliteCaptureSite.prefab`: base model = capture object (hidden when Operational, like `asteroidRocks`).
- Entity prefabs `Assets/Prefabs/Models/DefendStation/<Type>SatelliteView.prefab`; data `<Type>SatelliteData.asset`. Confirm folders with `PROJECT_ORGANIZATION`.
- Construction frameworks per facility in `CaptureSiteView.constructionFrameworks` (weapon set scaling up, or `_UC` models).
- Registrations: `DefendPlatformCatalog`, `AssetMappingData`, Addressables `View`, tooltip icons, `FactionAnnouncementService`, `BalanceStructureAdapter`, cheats enumerating `SiteFacilityType`.
- 5 icons `512 × 512`.

## AI Defense Strategy

### Rules

- AI decides **whether**, **where** and **which** satellite each decision tick (existing 3 s `EnemySiteConstructionController` cadence).
- One build per tick max. Never build on a pad with hostiles inside (`HasHostileUnits`).
- Decision = argmax over `(pad, type)` of `Score`; build only if best `Score ≥ BuildThreshold` (per difficulty).
- Everything below is a pure C# model (`EnemyDefenseStrategy`, `Entities/EnemyFaction/Models/`) fed by a snapshot; the controller only gathers inputs and calls `ICaptureSitesSystem`.

### Inputs (snapshot)

| Group | Inputs | Source |
|---|---|---|
| Economy | money, income/s, reserve for next production item | `EconomyModel`, `EnemyProductionStrategy` |
| Hostiles | known enemy `ForceComposition` (hull/shields/DPS per `ShipClass`), squadron count/strength | `ForceCompositionBuilder.BuildKnown`, `TeamIntelRegistry` |
| Own fleet | own `ForceComposition`, anti-fighter DPS, anti-capital DPS | `ForceCompositionBuilder` |
| Strategic | `FleetAdvantage`, `BaseThreatRatio`, `LocalAdvantage`, `HasThreatenedSite`, difficulty | `EnemyStrategicSnapshot` |
| Per pad | own built mines in its point, distance own station vs nearest enemy station, existing satellites in its point (types), recent hostile presence (s since last seen), pad inside hazard band | capture sites, station registry, intel |
| Per type | cost, `UnitCombatProfile` of the satellite (hull/shields/DPS per target class, range) | `UnitCombatProfileFactory` from satellite data |

### Score

```
Score(pad, type) = PadValue(pad) × TypeFit(type) × Affordability(type) × Diversity(pad, type)
```

- **PadValue** — how much this pad needs defending:
  - `+` income protected: built own mines in the point × mine income.
  - `+` exposure: `d_own / (d_own + d_enemy)` (frontline → 1, home → 0).
  - `+` home pressure: home points scale with `BaseThreatRatio` (station under threat → home pads rise above frontline).
  - `+` recent hostile presence / `HasThreatenedSite` at this point.
  - `−` existing satellites in the point (diminishing: `1 / (1 + count)`); cap per point = 3 pads.
- **TypeFit** — expected effectiveness vs the threat, minus what the fleet already covers:
  - `Effect(type) = Σ_class threatWeight[class] × satelliteDps(type → class)` using the satellite's combat profile vs known enemy hull/shields (same math as [[TODOs/Features/AI_Composition_Counters|AI Composition Counters]]).
  - `threatWeight` = enemy hull+shields share per class; squadrons as their own class.
  - `Gap` boost: if own fleet anti-class DPS is low for a high-threat class, × `(1 + gap)`.
  - Per cost: `TypeFit = Effect / cost`, normalized across the 5 types.
  - No intel → neutral weights → Turbolaser (generalist) wins by design.
- **Affordability**:
  - `spendable = money − reserve`; reserve dropped when the pad's point is threatened or `BaseThreatRatio` high.
  - `cost > spendable` → `0`. Else `1 − cost / (spendable + income × HORIZON)` (`HORIZON` 30 s) → cheap types win when poor, expensive types allowed when rich.
- **Diversity**: same type already in the point → × `0.6` per copy (avoid 3 identical when the threat is mixed).

### Expected outcomes (test cases)

| Game state | Expected pick |
|---|---|
| Enemy mostly fighters/bombers, poor economy | Quad laser |
| Enemy corvettes/frigates, mid economy | Turbolaser |
| Enemy shield-heavy cruisers, fleet lacks shield damage | Ion |
| Enemy capitals besieging the station, rich economy, home pad | Massdriver |
| Enemy cruisers/capitals at a frontline mining point, mid–rich economy | Missile |
| No intel | Turbolaser |
| Money < reserve, no threat | build nothing (ships first) |
| Hostiles on the pad | build nothing there |

### Difficulty

- Easy: higher `BuildThreshold`, ignores `Gap`, max 1 satellite per point.
- Normal/Hard: full score.
- UltraHard: full score + lower threshold on threatened points.

### Capture targeting

- Satellite pads join `TryGetCaptureTarget` after the mines of the same point (pad value ≈ 0 until mines are owned).

### Telemetry

- `_ai.csv`: satellites built per type, chosen pad, top-3 scores and reason (see [[Tooling/Battle_Telemetry|Battle Telemetry]]).

## Implementation

0. **Shared capture**: characterization tests → `CaptureControl`, `CaptureStrengthSampler`, `CaptureRingView`; relays + sites migrated; tests green.
1. **Art import** (ALO guide): base + 30 weapon ALOs; counts, bone positions, renders.
2. **Types & data**: `CaptureSiteKind`, `SiteFacilityType` 2–6, `DefendPlatformType` 4–8, `MapNodeKind.SatelliteSite`; allowed lists + costs; 5 `DefendPlatformData` (1000/1000, unified hardpoint health).
3. **Entity prefabs**: 5 satellites, 6 `ShipUnits` each; factory + builder.
4. **Map generation**: dual-triangle pads (Skirmish neutral, Survival owned); validator.
5. **Site view**: pad prefab, blink → hide via `CaptureRingView`, construction frameworks; minimap marker.
6. **Build menu**: rows from allowed list.
7. **AI**: `EnemyDefenseStrategy` model + controller inputs, capture ordering, telemetry.
8. **Save/load**: site owner/state/facility round trip; `StructureSave` for satellites.
9. **Tests (EditMode)** → compile → user in-game acceptance (no agent Play Mode).

## Tests (EditMode)

- Verified 2026-10-28 in the shared checkout, including concurrent hazard implementation: map `33/33`, save `47/47`, reinforcement zones `23/23`, AI defense `11/11`, satellite contracts `7/7`, actual entity composition `8/8` (five satellite types for local and AI owners across faction cases).
- Final `Capture` filter: `127/128`; all capture/ring/build-menu tests pass. Unrelated `BalanceInventoryTests.FullRegisteredPreset_CanCaptureCurrentSchemaWithoutRuntimeWrites` fails on Wavecrest `HP_Turret_01` unlock level `0`; unchanged by this task.
- Editor ready after tests; Console error count `0`. In-game acceptance not run. Exact test reports retained in `Temp/Satellite-*-tests.json`.

- Capture characterization (before/after Phase 0): ties, allies, contested, carry-over, relay starting owner, site not capturable while Operational.
- `MapLayoutGeneratorTests`: 3 pads per mining point; point-symmetric; clear of mines/zones/stations; all sizes × difficulties × seeds; Survival pads owned by player.
- Site: owned pad with starting owner → `Owned`; blink only for own empty site; hidden while Constructing/Operational; shown after destruction.
- Build UI: satellite site → 5 rows with given costs; mining site unchanged.
- Builder: each satellite `SiteFacilityType` → correct `DefendPlatformType`.
- Prefab contract: 6 `ShipUnits`, hull 1000, shields 1000, `hardPointHealth` covers `Weapon`/`PointDefense`.
- No repair: destroyed turret stays destroyed.
- `EnemyDefenseStrategy`: every row of the expected-outcomes table.
- Save: pad owner/state/facility round trip; old save count mismatch rejected.

## Edge Cases

- Pad overlaps mine mesh/rock → layout retry; tune `SatellitePadOffset`.
- Site count +1 pad per mining site → more retries on Small; check 20/20 seeds, 2–4 players, every size.
- Operational satellite locks pad capture until destroyed (existing rule).
- Blink uses unscaled time → keeps blinking while paused; confirm vs scaled time.
- Hidden ring while Constructing: enemy capture of a constructing pad only visible in status bar.
- Fog: enemy satellites hidden until seen; pad ring keeps last seen state.
- Ion satellites vs squadrons/structures follow existing ion rules.
- AI economy: satellites have no income; reserve gate prevents production starvation.

## Open Questions

- None. All decisions recorded 2026-10-28.

## Files

- Capture: `Assets/Scripts/Entities/ReinforcementZones/{ReinforcementZoneModel,ReinforcementZoneView,ReinforcementZonePresenter}.cs`, `Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs`, `Assets/Scripts/Entities/Players/{CaptureStrength,CaptureStrengthBuilder}.cs`
- Sites: `Assets/Scripts/Entities/CaptureSites/{CaptureSiteModel,CaptureSiteView,CaptureSiteData,CaptureSitePresenter,CaptureSiteBuildUi,CaptureSiteBuildUiController,SiteFacilityType}.cs`, `Assets/Scripts/Services/CaptureSites/{CaptureSitesSystem,SiteFacilityBuilder,ICaptureSitesSystem}.cs`
- Platform: `Assets/Scripts/Entities/DefendPlatform/*.cs`, `Assets/Scripts/Entities/BaseEntity/EntityComponentData.cs`, `Assets/Scripts/Components/Health/HardPoints/HardPointModel.cs`
- Map: `Assets/Scripts/Entities/Map/Generation/{PointOfInterestPlacer,MapNodeKind,SurvivalLayoutGenerator}.cs`, `Assets/Scripts/Entities/Map/{MapLayoutView,SiteSpot,MapGenerationSettings}.cs`
- Minimap: `Assets/Scripts/Entities/MiniMap/Presenter/{CaptureSiteMiniMapPresenter,ReinforcementZoneMiniMapPresenter}.cs`
- AI: `Assets/Scripts/Services/Enemy/{EnemySiteConstructionController,EnemyProductionStrategy,ForceCompositionBuilder,UnitCombatProfileFactory,TeamIntelRegistry}.cs`, `Assets/Scripts/Entities/EnemyFaction/Models/{EnemyStrategicDecisionModel,Combat/ForceComposition}.cs`
- Saves: `Assets/Scripts/Services/Saves/{BattleSaveValidator,CaptureSiteSave,StructureSave,ReinforcementZoneSave}.cs`
- Related: [[TODOs/Features/More_Mining_Sites|More Mining Sites]], [[TODOs/Features/AI_Composition_Counters|AI Composition Counters]], [[TODOs/Refactoring/Capture_Reinforcement_Tally_Dedup|Tally Dedup]]

## TODO

- [x] User answers open questions (2026-10-28)
- [x] User confirms build time + blink details (2026-10-28: scale with cost, unscaled blink, hidden while constructing)
- [x] Phase 0: shared capture (`CaptureControl`, sampler, ring view)
- [x] Import base + 30 weapon ALOs
- [x] Types, data assets, costs
- [x] 5 satellite entity prefabs
- [x] Dual-triangle pads + starting owner + validator + Survival
- [x] Blink → hide ring, minimap marker
- [x] Build menu from allowed list
- [x] AI `EnemyDefenseStrategy` + capture ordering + telemetry
- [x] Save/load round trip
- [x] Satellite EditMode gates pass and Editor compiles clean; unrelated balance-data failure recorded below
- [ ] User in-game acceptance
