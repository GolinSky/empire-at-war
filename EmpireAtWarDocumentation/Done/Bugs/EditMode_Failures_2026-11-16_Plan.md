---
category: Bugs
status: done
created: 2026-11-16
completed: 2026-11-16
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
- User run `Assets/TestResults_20261116_205825.xml`: 3,960 passed, 28 failed, 4 skipped. Remaining: A 1, B 12, C 7, F 6, G 2.
- Latest user run `Assets/TestResults_20261116_212228.xml`: 3,986 passed, 2 failed, 4 skipped. All prior failures cleared except both G projection cases, now blocked by an Editor backbuffer size mismatch.

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
- Latest user run: `Core` 153/154. The prewarm case passed. The save restoration fixture still expected four removed HUD errors.
- [x] `e9dce256e`: removed stale log expectations while keeping all save restoration assertions.
- [x] User run `212228`: `Core` 154/154 after fixture cleanup.

### D — shared AI test doubles (2 tests)
- `Commander_ShipRemovedMidMultiFrameCycle_DoesNotThrow`: `NullReferenceException` at `EnemyStrategicContextBuilder.IncludeNearbyOwn` (`:1221`). `TestEntity.GetFacade` returns null for `IEntityTransformFacade`.
- `TeamHostileIntelService_TracksTeamMembershipAcrossJoinAndLeave`: `NullReferenceException` in `VisionQueries.IsVisible` for the same reason.
- [x] `b94af9697`: fixture entities supply `IEntityTransformFacade` with real transforms at their ships' positions. TearDown destroys the backing GameObjects. Both archived stacks required the facade.
- Optional position caching remains in [[TODOs/Optimization/Shared_Team_AI_Plan]] and depends on profiling.
- Tests: `EnemyAi`.

### E — production strategy after scheduler change (2 cases)
- `UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(5000, Recusant, false)` and `(5000, null, true)`: expected a `ShipUnitRequest`, got null. Cases `1500` and `0` pass.
- [x] `92e7e4fdc`: advance the scheduler after production startup, repeated same-time ticks, and capacity or ship releases. Also advance it before the income-research duplicate assertion.
- `Start` registers due work. `Tick` marks work due on a release-version change. `Step` evaluates purchases. Recusant costs 4,000 against the fixture's 5,000 starting credits.
- [x] User rerun `TestResults_20261116_204943.xml`: `EnemyAi` 481/481; `EnemyDecisionSchedulingTests` 14/14 and `EnemyProductionStrategyTests` 12/12.

### C — control group input (7 tests)
- [x] `204700835`: EditMode Input System updates default to editor updates, which skip action callbacks. Fixture uses manual player updates and the simulated keyboard. TearDown restores original settings.
- Tests: `BattleInput`. Assertions preserved.

### B — selection ring and health anchor fit (12 tests)
- [x] `74462ddbe`: fit ring geometry for Bulwark II, DH Omni, Diamond, Lupus and Recusant Dreadnought. Unity readback shows 0.92 visible width relative to hull and centered placement. Health anchors retain their authored positions.
- [x] Fit Ginivex and TIE Defender squadron rings to 1.15 × formation dimensions. Synchronize four Jedi hero preview scales to their authored ring sizes. Unity readback confirms all seven preview dimensions and ratios.
- Tests: `Health` (`HealthOverlayAnchorTests`).

### F — new unit content (6 tests)
- [x] `825769050`: eight Lupus hardpoints unlock at level 1. Radar reaches its longest weapon. Editor validation checked all 18,499 registered balance fields with zero errors.
- [x] Asajj and Sevrance use their distinct imported portraits. Both placement previews bind all four declared renderers to the hologram material in Unity.
- [x] Luke fixture resolves the child `WeaponComponent` and checks exact membership of all 24 hardpoints.
- [x] Add Tector and five Separatist ships to their unlocked survival waves. Source checks cover all 20 waves, equal tier totals, hero counts and carryover.
- Tests: `Balance`, `Combat`, `Squadrons`, `Economy`, `GameFlow`.

### G — HUD projection and layout (2 failures, one prior intermittent case)
- [x] `b62c671a2`: render the disabled fixture camera before checking actual icon position. A Unity probe reproduced the 33.54 px orthographic pan error and confirmed rendering updates the camera canvas plane. Both coordinate assertions remain.
- [x] `cb6275285`: render the projection fixture to a temporary 800 × 600 texture. The prior run used the Editor backbuffer and logged an attachment-size mismatch before position assertions. Standalone Unity probe rendered 10 frames across both camera modes with 0.000069 px maximum error. Test rerun pending.
- [x] `RemovingEdgeMarker_PreservesSurvivorPositionsAndPitchThroughFinalSurvivor(true)` passed in `205825`. Initialize the ship marker at its stationary position before registration and check shared group membership to remove the previous fixture's velocity dependence.
- Tests: `Ui`, `Squadrons`. Live structure icon acceptance remains in [[TODOs/User_Acceptance_Queue]].

## Verification
- Unity imported and reserialized all 16 changed prefabs and data assets. Asset reloads and script compilation succeeded, with no new import or compilation errors in Console.
- [x] User confirmed full EditMode passed on 2026-11-16 after the offscreen projection fix. Follow-up XML was not supplied.
- Reusable offscreen canvas and synthetic input fixture guidance → [[Rules/UI_UX_GUIDELINES#EditMode UI fixtures]].

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
- Related: [[TODOs/Optimization/Shared_Team_AI_Plan]] (Core and EnemyAi verification passed in `212228`), [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan]].
