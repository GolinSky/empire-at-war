---
category: Bugs
status: in-progress
created: 2026-10-28
---
# Defense Satellites — Review Fixes

## Goal

- Fix issues found in the read-only review of commit `ed1b5313b` ("Add capturable defense satellites") against [[TODOs/Features/Defense_Satellites_Plan|Defense Satellites Plan]].
- Keep the parent plan active until these fixes are done and accepted in-game.

## Findings

| # | Severity | Issue | Where |
|---|---|---|---|
| 1 | ~~High~~ Fixed | All five satellites had radar `Range`/`VisionRange` `600`; stations only acquire radar contacts (`Enemies.ItemAdded`). Measured `WeaponProfile.Range` (all factions): Turbolaser `517.5`, Massdriver `517.5`, Ion `575`, Quad laser `200`, Missile `715` → only Missile was capped. Fixed 2026-10-28: Missile `Range`/`VisionRange` `600 → 715`. | `Assets/Settings/Data/Models/DefendPlatform/*SatelliteData.asset`, `StationCombatPresenter.cs:43` |
| 2 | ~~High~~ Fixed | AI scored raw `WeaponProfile.Range`. Fixed 2026-10-28: `LoadOptions` caps option range at `ComponentData.Range`. | `EnemyDefenseSnapshotBuilder.cs:121`, `EnemyDefenseStrategy.cs:101` |
| 3 | Medium | Survival scope change: each point went from 1 mine → 3 mines (+3 pads). User decision only asked for pads around mining zones. Triples Survival mining income; validator formula changed to match. | `SurvivalLayoutGenerator.cs:47–58`, `BattleSaveValidator.cs:94` |
| 4 | Medium | AI site build order: satellite choice runs first each 3 s tick and `return`s → owned empty mines wait while satellites are affordable; satellites skip the `MINIMUM_FLEET_SIZE` gate mines use. Plan rule "satellites have no income; reserve gate prevents starvation" does not cover mine priority. | `EnemySiteConstructionController.Tick` |
| 5 | Medium | `TryGetRaidTarget` = closest hostile Operational site → AI raids now target 1000/1000 six-gun satellites as readily as mines. Not addressed in plan. | `CaptureSitesSystem.cs:211` |
| 6 | Low | `EnemyStrategicSnapshot` default before the first strategic tick → `Difficulty = Easy`, `LocalAdvantage = 0` (pressure ×4), `FleetAdvantage = 0`. Hard/UltraHard AI behaves like Easy on early ticks. | `EnemyDefenseSnapshotBuilder.Build` (`_strategy.LastSnapshot`) |
| 7 | Low | Two sources of truth for satellite weapons: `DefendPlatformData.weaponLoadout` (AI profile) vs prefab `WeaponHardPoint` types. Can drift silently. | `DefendPlatformData.cs`, `*SatelliteView.prefab` |
| 8 | Low | `CaptureRingView.Apply` uses string `SetFloat("_BorderWidth", …)` ×4 every frame while blinking; pre-refactor code cached `Shader.PropertyToID`. | `CaptureRingView.cs:62–69` |
| 9 | Low | `GetPadValue` sums `ProtectedIncome` (credits/s) with unitless `Exposure`/`recent` (0–1) → income term dominates or vanishes depending on `IncomeDelay`. Tuning risk, not a crash. | `EnemyDefenseStrategy.GetPadValue` |
| 10 | Info | Plan registrations not done: `FactionAnnouncementService` only handles `SiteFacilityType.Mining`; no satellite built/lost announcement. Confirm whether wanted. | `FactionAnnouncementService.cs:205–213` |
| 11 | Info | Ring visibility semantic change for **all** sites: `CaptureSitePresenter.SetVisibility` now passes `true` → rings always drawn (fog keeps last-seen). Before: ring only while capturing. Matches the plan's state table, but affects mining/battle sites too — confirm in-game. | `CaptureSitePresenter.cs:126` |
| 12 | Info | Home mining triangles moved outward: home distance = `max(mine, satellite)` requirement. Changes existing home-mine spacing. Confirm in-game. | `PointOfInterestPlacer.TryAddHomeMiningSites` |

### Verified OK

- `CaptureControl` preserves relay/site semantics (tie, ally reset, carry-over, not-capturable reset for sites only).
- Six `ShipUnits` per satellite; Quad laser hardpoints `PointDefense`, others `Weapon`; `hardPointHealth` covers both at `300`; hull/shields `1000/1000`.
- Save restore relinks satellites via `context.DefendPlatforms` → `ReleaseFacility`; validator maps all five view prefabs.
- `TryStartConstruction` now checks `CanStartConstruction`, ally owner and `CanBuild(kind, type)`.
- Build UI clones rows from `facilityOptions[0]`; layout runs only after `SetFacilities`.

## Decisions (user, 2026-10-28)

- #3: keep 3 mines per Survival point → accepted, no change.
- #4: fix → fleet gate (`MINIMUM_FLEET_SIZE` 2) applies to all site builds; an owned empty non-satellite site blocks satellite spending until built.
- #5: fix → raids target the closest hostile operational non-satellite site; satellites only when none exists.
- #10: yes, reuse existing clips → satellite built = `ConstructionComplete`; satellite lost = `UnitDestroyed` (`LOSS_COOLDOWN`).
- #11, #12: no code change; in-game check only.

## Implemented 2026-10-28

- #4 `EnemySiteConstructionController.Tick`: fleet gate → `HasEmptyOwnedMine` → `TryBuildOnOwnedSite`, else satellite.
- #5 `CaptureSitesSystem.TryGetRaidTarget`: non-satellite pass, then any-site fallback.
- #6 `IEnemyAiDebugInfo.HasDecision` (from `_hasDecision`) → `EnemyDefenseSnapshotBuilder.IsReady`; satellite choice skipped until true.
- #7 contract test: every prefab mount `TryGetWeaponType` == loadout weapon.
- #8 `CaptureRingView`: 4 cached `Shader.PropertyToID` fields.
- #9 `EnemyDefensePad.ProtectedIncome` → `ProtectedMineShare` = operational own mines ÷ `SITES_PER_MINING_POINT` (0–1).
- #10 new `ICaptureSitesSystem.ConstructionCompleted(owner, type)` raised on build finish; `SiteFacilityBuilder.IsSatellite`; `FactionAnnouncementService` handlers.
- Tests added: `CaptureSitePresenterTests.CaptureSystem_RaidsPreferMinesOverCloserSatellites`, `FactionAnnouncementTests.LocalSatelliteCompletionAndLossReuseConstructionAndUnitLossCues`.
- No unit test for controller build order (`EnemyDefenseSnapshotBuilder` is concrete, 16 deps).
- Verified 2026-10-28 (categories only, not full suite): `Capture` 91/91, `EnemyAi` 183/183, `Audio` 139/148.
  - Audio failures unrelated: 8× `FactionAudioAssetTests.ShieldAbilitiesUseShieldCues` (cue volume `0.15` < `0.25` minimum, asset data); 1× `FactionAnnouncementTests.UnitLossUsesFiveSecondCameraMemory(5.0)` (float precision at the 5 s boundary on long Editor uptime; unit-loss camera path untouched).

## Implementation

1. **Radar range (#1)**: set each satellite `Range`/`VisionRange` ≥ its max weapon `WeaponProfile.Range` (compute from `WeaponsData`; do not invent values). Edit via Unity API + `SetDirty`/`SaveAssets`.
   - Add contract test: `ComponentData.Range >= max(weaponLoadout profile range)` for all 5.
2. **AI range (#2)**: use `min(weaponRange, ComponentData.Range)` as option range in `LoadOptions`.
3. **Single weapon source (#7)**: contract test that `weaponLoadout` matches prefab hardpoint `WeaponType` × count (or derive loadout from prefab in `BalanceStructureAdapter`).
4. **Survival (#3)**: apply user decision; update `BattleSaveValidator` and `MapLayoutGeneratorTests.Survival_*`.
5. **Build order (#4)**: in `EnemySiteConstructionController.Tick`, try `TryBuildOnOwnedSite` first when fleet gate passes and no urgent pad; otherwise satellite. Test with a fake `ICaptureSitesSystem`.
6. **Raid target (#5)**: filter/prefer `Kind != Satellite` in `TryGetRaidTarget`; EditMode test.
7. **Snapshot default (#6)**: skip satellite decision until `LastSnapshot` is populated (e.g. `OwnShipCount`/tick flag), or pass configured difficulty directly.
8. **Ring IDs (#8)**: cache `BORDER_WIDTH_ID`, `OUTLINE_WIDTH_ID`, `BORDER_OPACITY_ID`, `OUTLINE_OPACITY_ID` as `static readonly int`.
9. **Pad value units (#9)**: normalize `ProtectedIncome` (e.g. ÷ mine income per point → 0–3) before summing; re-run `EnemyDefenseStrategyTests`.
10. Compile → EditMode filters `Capture`, `Satellite`, `EnemyDefense`, `MapLayout`, `BattleSave` (follow Unity Test Safety) → user in-game check. No agent Play Mode.

## Files

- `Assets/Settings/Data/Models/DefendPlatform/{QuadLaser,Turbolaser,Ion,MassDriver,Missile}SatelliteData.asset`
- `Assets/Scripts/Services/Enemy/{EnemySiteConstructionController,EnemyDefenseSnapshotBuilder}.cs`
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyDefenseStrategy.cs`
- `Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs`
- `Assets/Scripts/Entities/Capture/CaptureRingView.cs`
- `Assets/Scripts/Entities/Map/Generation/SurvivalLayoutGenerator.cs`, `Assets/Scripts/Services/Saves/BattleSaveValidator.cs`
- Tests: `DefenseSatelliteContractTests`, `EnemyDefenseStrategyTests`, `MapLayoutGeneratorTests`

## TODO

- [x] User decisions #3, #4, #5, #10 (2026-10-28)
- [x] Radar range + contract test (#1) — 2026-10-28; `DefenseSatelliteContractTests` 7/7
- [x] AI effective range (#2) — 2026-10-28; `EnemyDefenseStrategyTests` 11/11; Console errors 0
- [x] Weapon loadout consistency test (#7)
- [x] Survival layout per decision (#3) — kept as is
- [x] AI build order (#4)
- [x] Raid target filter (#5)
- [x] Snapshot default guard (#6)
- [x] Ring property IDs (#8)
- [x] Pad value normalization (#9)
- [x] Satellite announcements (#10)
- [x] EditMode categories `Capture`, `EnemyAi`, `Audio` run (2026-10-28); 9 unrelated Audio failures recorded
- [x] Commit fixes (2026-10-28)
- [ ] User in-game check (#4 AI build order, #5 raids, #10 cues, #11 rings, #12 home mines)
