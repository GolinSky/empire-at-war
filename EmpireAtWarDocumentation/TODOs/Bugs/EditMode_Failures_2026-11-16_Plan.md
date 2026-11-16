---
category: Bugs
status: in-progress
created: 2026-11-16
---
# EditMode Failures 2026-11-16

## Goal
- Full EditMode suite green on committed `main` so Jenkins can build.
- Fix production defects in production code; change a test only when its fixture or contract is wrong.

## Important Values
- Run: `Assets/TestResults_20261116_202833.xml`, 2026-11-16 18:22 UTC, 32 s.
- Total 3,992 · passed 3,925 · failed 63 · skipped 4. PlayMode passed (user report).
- Initial HEAD at analysis: `d2a8868da`.
- Rerun `Assets/TestResults_20261116_204943.xml`: 3,958 passed, 30 failed, 4 skipped. No newly failing cases. Remaining: A 2, B 12, C 7, F 6, G 3.
- 4 of 63 failures come from the shared-AI work; 31 come from one UI line; 28 from content, HUD and input work.

| Group | Fixture | Fails | Origin |
|---|---|---|---|
| A | `RealEntityCompositionTests` | 31 | Tutorial anchor / faction HUD |
| B | `HealthOverlayAnchorTests` | 12 | Prefab ring/anchor data |
| C | `ControlGroupInputTests` | 7 | Control groups input |
| D | `EnemyDecisionSchedulingTests` | 2 | Shared AI (test doubles) |
| E | `EnemyProductionStrategyTests` | 2 | Shared AI scheduler |
| F | Content: Lupus, Ginivex, Luke, survival waves | 6 | New units / balance |
| G | `StructureStatusIconPresenterTests`, `SquadronIconPrefabTests` layout | 3 | HUD projection / layout |

## Implementation

### A — one line, 31 tests (do first)
- Cause: `TutorialAnchor.Unmark` calls `Destroy(existing)` → Unity logs an error in edit mode → every composition test fails in body or TearDown.
- Path: `FactionUiController.LateDispose` → `FactionUi.Hide` → `RefreshUnitVisibility` → `MarkTutorialButton` → `TutorialAnchor.Unmark` (`Assets/Scripts/Entities/TutorialUi/TutorialAnchor.cs:66`).
- [x] `28f4a760c`: deactivate all production cards before refreshing tutorial markers on an inactive route. Disabled cards unregister anchors; showing the HUD refreshes them. All 31 archived failures followed the changed path.
- User rerun: `RealEntityCompositionTests` 33/35; both failures occur during save restoration. `SavedSkirmish_RestoresPlayerStateStructuresAndOwnerBookkeepingIntoNewBattle` logs from `TutorialAnchor.Mark`; `SquadronPrewarm_AfterRestore_AllocatesIdsPastRestoredEntitiesAndRespawnsWithoutCollision` logs from `FactionUnitUi.Destroy`.
- [x] `456c9a5d9`: use immediate destruction for replaced runtime-created cards and anchors in EditMode, keep deferred destruction in PlayMode.
- [ ] Verify `Core` after the follow-up fix.

### D — shared AI test doubles (2 tests)
- `Commander_ShipRemovedMidMultiFrameCycle_DoesNotThrow`: `NullReferenceException` at `EnemyStrategicContextBuilder.IncludeNearbyOwn` (`:1221`). `TestEntity.GetFacade` returns null for `IEntityTransformFacade`.
- `TeamHostileIntelService_TracksTeamMembershipAcrossJoinAndLeave`: `NullReferenceException` in `VisionQueries.IsVisible` for the same reason.
- [x] `b94af9697`: fixture entities supply `IEntityTransformFacade` with real transforms at their ships' positions. TearDown destroys the backing GameObjects. Both archived stacks required the facade.
- [ ] Production follow-up (same area, optional): `IncludeNearbyOwn` still reads the live `Transform` per entity per force build. `ForceCompositionBuilder.Build` already iterates snapshot entries → add an overload that passes the entry position. Do only with a marker showing it matters.
- Tests: `EnemyAi`.

### E — production strategy after scheduler change (2 cases)
- `UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(5000, Recusant, false)` and `(5000, null, true)`: expected a `ShipUnitRequest`, got null. Cases `1500` and `0` pass.
- [x] `92e7e4fdc`: advance the scheduler after production startup, repeated same-time ticks, and capacity or ship releases. Also advance it before the income-research duplicate assertion.
- `Start` registers due work. `Tick` marks work due on a release-version change. `Step` evaluates purchases. Recusant costs 4,000 against the fixture's 5,000 starting credits.
- [x] User rerun `TestResults_20261116_204943.xml`: `EnemyAi` 481/481; `EnemyDecisionSchedulingTests` 14/14 and `EnemyProductionStrategyTests` 12/12.

### C — control group input (7 tests)
- All recall/assign requests are empty; `CommandSettingControlsPreviewAndAssignment` false.
- Suspect (unverified): `ControlGroupInput.HandleFocusChanged` sets `_waitForRelease = true`; `SetUp` calls `SetFocus(true)` after `Initialize()` and the first `Tick` may not clear it when the Editor window is unfocused or the simulated keyboard reports no release edge.
- [ ] Reproduce with the Editor focused and unfocused. Result differs → test must not depend on `Application.isFocused`; inject focus into `ControlGroupInput` instead of reading the static.
- [ ] Result same → debug `_waitForRelease` / `_pendingIndex` flow against `InputSettingsData.ControlGroupModifier`.
- Tests: `BattleInput`.

### B — selection ring and health anchor fit (12 tests)
- `ShipSelectionRing_InitializationFitsHullWithoutMovingHealthAnchor`.

| Prefab | Expected | Actual |
|---|---|---|
| `BulwarkIIShipView` | 0.88–0.96 | 0.438 |
| `DHOmniShipView` | 0.88–0.96 | 0.686 |
| `RecusantDreadnoughtShipView` | 0.88–0.96 | 0.675 |
| `DiamondShipView` | 0.88–0.96 | 4.764 |
| `LupusShipView` | 0.88–0.96 | 4.234 |
| `AnakinSkywalkerEta2`, `MaceWinduEta2`, `ObiWanKenobiEta2` squadron views | 8.775 ± 0.001 | 7.915 |
| `PloKoonDelta7BSquadronView` | 8.370 ± 0.001 | 7.915 |
| `AsajjVentressGinivex`, `SevranceTannGinivex`, `TIEDefender` squadron views | 1.1–1.2 | 0.963–0.971 |

- [ ] Ships: ring size not fitted to hull on five Separatist prefabs (two are >4× hull → likely wrong unit or missing fit). Re-run the project's ring-fit tool on those prefabs; follow [[Rules/UI_UX_GUIDELINES]] and Unity Asset Persistence rules.
- [ ] Hero squadrons: all four report 7.915 → one shared value overrides per-prefab anchors. Find the shared source before touching prefabs.
- [ ] After prefab edits: confirm each unit still spawns and `ShipData` hardpoint coverage is intact.
- Tests: `Health`.

### F — new unit content (6 tests)
- [ ] `BalanceInventoryTests.FullRegisteredPreset_CanCaptureCurrentSchemaWithoutRuntimeWrites`: `Lupus Missile Frigate / HP_Lupus_Laser_01 / Unlock Level` below minimum 1 → set a valid unlock level on the Lupus hardpoint data.
- [ ] `WeaponTaxonomyTests.EveryUnitRadar_ReachesItsLongestGun`: `LupusShipData` radar range shorter than its longest weapon → raise radar range or correct the weapon.
- [ ] `SquadronIconPrefabTests.SquadronViews_MarkerRootIsAssignedAndHasNoOwnCanvas`: `AsajjVentressGinivexSquadronView` icon is not `.../SquadronIcon/<faction>/AsajjVentressGinivexIcon.png` → add/assign the icon (commit `1745e04fd` changed Ginivex portraits).
- [ ] `UnitSpawnViewTests.PlacementPrefabs_DeclareHologramMaterialUsedByEveryListedRenderer`: `AsajjVentressGinivexReinforcementView` `Box01` has no Hologram slot → add the slot or remove the stray renderer.
- [ ] `LukeRedSquadronTests.SavedFormation_BindsLukeAndFiveWingmenToHealthFlightAndWeapons`: "Object at index 0 is null" → a serialized reference on the Luke squadron prefab is missing.
- [ ] `SurvivalGameplayTests.AuthoredWaves_IncludeEveryUnlockedShipWithEqualTierCounts`: Republic wave 4 does not contain every unlocked ship → update survival wave data for the changed roster.
- Tests: `Balance`, `Combat`, `Squadrons`, `Economy`, `GameFlow`.

### G — HUD projection and layout (3 tests)
- [ ] `StructureStatusIconPresenterTests.DestroyedIconTracksCameraPanRotationAndZoomOnBattleCanvas(false/true)`: icon is not 42 px above the structure. This is the unrun regression listed in [[TODOs/User_Acceptance_Queue]] for [[Done/Bugs/Structure_Status_Icons_Projection]] → reopen that plan or fix here; remove its queue line either way.
- [ ] `SquadronIconPrefabTests.RemovingEdgeMarker_PreservesSurvivorPositionsAndPitchThroughFinalSurvivor(true)`: y 254.4 vs 340.8 (one 86.4 row lower) → survivor marker shifts a row when a ship marker is removed.
- Tests: `Ui`, `Squadrons`.

## Rules
- Order: A → D → E → C → F → B → G. A unblocks 31 tests and reveals real composition results; D and E gate the AI plan.
- One group per commit. After each: run only that group's categories; full EditMode once at the end.
- No weakened assertions, no ignored fixtures, no balance numbers pinned in tests.
- Tests run only on explicit user request; record categories run and results here.

## Edge Cases
- After A, `RealEntityCompositionTests` may show new failures that the log error masked (AI scope resolution, scheduler `Step(0f)`). Treat them as new findings, not regressions of the fix.
- User counted 64 failures; the XML holds 63 failed test cases. If another results file exists, re-check against it.

## Files
- `Assets/TestResults_20261116_202833.xml`
- `Assets/Scripts/Entities/TutorialUi/TutorialAnchor.cs`, `Assets/Scripts/Entities/Faction/Ui/FactionUi.cs`
- `Assets/Scripts/Tests/Editor/EnemyAi/EnemyDecisionSchedulingTests.cs`, `EnemyProductionDecisionModelTests.cs`
- `Assets/Scripts/Services/Input/ControlGroupInput.cs`, `Assets/Scripts/Tests/Editor/BattleInput/ControlGroupInputTests.cs`

## TODO
- Related: [[TODOs/Optimization/Shared_Team_AI_Plan]] (waiting for Core verification after the HUD restore fix), [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]].
