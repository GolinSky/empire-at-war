---
category: Refactoring
status: done
completed: 2026-11-02
acceptance: pending-user
created: 2026-11-02
---
# Auto Power Refactor

## Goal
- Fewer types and less repeated code in the player auto power system. Behavior stays the same.
- Remove the static helpers from the decision path. Make ability eligibility come from data, not hard-coded types and ids.
- Origin: [[Done/Features/Auto_Power_Abilities_Plan|Auto Power Abilities]] (feature done, `acceptance: pending-user`).

## Current Shape (2026-11-02, live source)
| File | Lines | Role |
|---|---|---|
| `Services/ShipAbilities/PlayerAutoPowerService.cs` | 139 | batching, gating, slot pick, manual hold |
| `Services/ShipAbilities/AutoPowerCombatSnapshot.cs` | 126 | spatial grid, force gather, meaningful-force, beam target |
| `Services/ShipAbilities/AutoPowerMonitor.cs` | 47 | shield-loss window, confirmation, manual hold |
| `Services/ShipAbilities/AutoPowerRules.cs` | 16 | **static** 11-arg rule |
| `Services/ShipAbilities/AutoPowerEligibility.cs` | 18 | **static** type/id checks |
| `Services/ShipAbilities/AutoPowerDecision.cs` / `AutoPowerTuning.cs` | 4 / 32 | enum / tuning |
| `Entities/Ship/Abilities/ShipAutoPowerFacade.cs` | 60 | per-ship toggle, shield revision, weapon proxy |

## Issues
### Duplication
1. **Eligibility defined twice.** `AutoPowerEligibility.IsEligible` lists the settings types. `PlayerAutoPowerService.Check` (lines 92–96) lists the same types again to match a slot to a decision.
2. **Hard-coded ids in eligibility.** `ImperialVenatorIntensifyFirepower` is excluded and `LaserBeam`/`ExecutorLaserBeam` are added by id. Every new ship ability means editing code. `EnemyShipAbilityController` already uses data (`ShipAbilityDefinition.AiUse`), so the two systems work differently.
3. **The liveness check appears 3 times**: `TryGetEntity(id) && ReferenceEquals(entity, registered) && !IsDestroyed`, in `CombatSnapshot.FindBeamTarget`, `CombatSnapshot.Decide` and `Service.CanMonitor`.
4. **Range is checked twice in `Decide`.** It checks against the cached `unit.Position` (line 94), then reads the live transform and checks again (line 97).
5. **Force add is repeated.** `_hostile.Add` and `_engagedHostile.Add` take the same 4 arguments, and `GetOffenseScale` runs twice per unit. `ForceCompositionBuilder.AddUnit` already does this, but it is registered only in `AiPlayerScope`.
6. **Shield-reset detection is repeated.** `ShipAutoPowerFacade` raises `ShieldRevision` when max shields or suppression change. `AutoPowerMonitor.Sample` also compares `maximum`/`suppressed` against its last values. Only the revision is needed (it also catches suppression that starts and ends between checks).
7. **Slots are scanned twice per check**: once for any `Active` slot, then again to find the matching slot.

### Static helpers in business logic
- `AutoPowerRules.Decide`: static, 11 positional args (5 of them tuning values). The rule lives in 2 places: "meaningful force" in the snapshot, the thresholds in the static method.
- `AutoPowerEligibility`: static type switch (see above).
- `ForceCompositionBuilder.GetOffenseScale`: static, called from the snapshot.

### Coupling / API
- `AutoPowerTuning` is passed into every call (`Sample(..., tuning)`, `Decide(..., tuning)`) instead of being injected once.
- `AutoPowerMonitor.ManualUntil` has a public setter. The service writes the hold and calls `ResetConfirmation()` from 5 places, so monitor rules leak into the service.
- `ShipAutoPowerFacade.BindWeapons` is a setter called from the `Ship` constructor. `Ship` already gets `IAutoPowerWeaponObserver` from the same container, so the facade can take it in its constructor.
- `FindBeamTarget` scans all units and ignores the grid that `Decide` uses.
- The corvette reference profile is looked up on every `Decide`. It never changes during a battle.
- Several statements per line (`_a = a; _b = b; ...`). This differs from the surrounding services, for example `ShipAbilityService`.

### Minor (out of scope unless asked)
- "Individually selected → paused" is computed separately by `ShipUiController` (UI state) and the service (gate).
- `ShipUiController.IsAutoPowerEligible` repeats the owner/alive/eligible filter from `CanMonitor`.
- `ShipAutoPowerUiState.Description` hard-codes tooltip strings. `ShipAutoPowerButtonUi` hard-codes label colors.

## Decision
- **Eligibility from data.** Add `[SerializeField] AutoPowerRole autoPower` to `ShipAbilityDefinition`. Rename `AutoPowerDecision` → `AutoPowerRole { None, Shields, Weapons }` and use it both as the slot role and as the decision.
  - Why: one source of truth, no id exceptions, same approach as `AiUse`.
  - Avoid: a new `ShipAbilitySettings` virtual such as `AutoPowerRole` on settings, because aliased/shared settings (id 26 history) would leak roles between abilities.
- **Beam target from data.** Use `definition.RequiresEnemyTarget` instead of `IsLaserBeam`.
- **One decision owner.** The snapshot keeps the spatial index and the force gathering. The rule moves into it as an instance method that reads the injected tuning. Delete `AutoPowerRules`.
- **Monitor owns its state.** Inject the tuning; replace `ManualUntil` with `HoldManual(now)` / `IsHeld(now)`; reset on revision only.

## Implementation
### Phase 1 — Data eligibility (done 2026-11-02)
1. [x] Rename `AutoPowerDecision` → `AutoPowerRole` (`git mv` .cs + .meta, identifier sed; Serena offline).
2. [x] Add `[Header("Auto Power")] autoPower` to `ShipAbilityDefinition` between `Ai` and `Ability`.
3. [x] Migrate `ShipAbilityCatalog.asset` with `unity command eval_file` (SerializedObject; save + `ForceReserializeAssets` on that path only). Diff: +36 `autoPower` lines and nothing else; 0 console errors.
4. [x] Ids `10`/`33` have `requiresEnemyTarget: 1` → the service picks the beam target via `definition.RequiresEnemyTarget`.
5. [x] Deleted `AutoPowerEligibility.cs` (+ meta). Facade: `AutoPower != None`. Service: `AutoPower == decision`; unused `Abilities` using removed.
6. [x] Eligible set unchanged: Shields `3`; Weapons `5, 10, 12, 20, 23, 24, 31, 32, 33, 38`. Id `35` is `FullSalvoSettings`, so the old id exclusion never matched anything.
- Verification: Unity recompile completed with 0 errors. Tests not run (local override). Categories to run: `Abilities`, `Ships`, `Core`, `Balance`.
- A new ability defaults to `None` (opt-in). No validation test was added.

### Phase 2 — Single decision owner (done 2026-11-02)
1. [x] `AutoPowerCombatSnapshot` takes `ShipAbilityCatalog` and stores `_tuning`; `Decide(caster, automation, heavyLoss)` no longer takes a tuning argument. Constructor uses one assignment per line.
2. [x] The rule moved to the public instance method `ChooseRole(hull, shields, shieldFraction, canRegenerate, heavyLoss, meaningfulForce, canAttack, hostileAdvantage)`, which reads its thresholds from `_tuning`. Deleted `AutoPowerRules.cs` (+ meta).
3. [x] `AutoPowerRulesTests` call `ChooseRole` on a snapshot built with `null` entity/player/vision/profile dependencies and a `ScriptableObject.CreateInstance<ShipAbilityCatalog>()` that has the default tuning (0.6 / 0.35 / 1.5, the same values the old tests passed). `RealEntityCompositionTests` passes the catalog.
- Corvette reference caching was skipped. It is one dictionary lookup per `Decide`, and caching it in the constructor would make the rule tests need a real `UnitCombatProfileCatalog`.
- Verification: Unity recompile completed with 0 errors; 0 console errors. Tests not run (local override); run `Abilities` and `Core`.

### Phase 3 — Snapshot cleanup (done 2026-11-02)
1. [x] One private `IsLive(IEntity)` (registered + same reference + not destroyed). One range check, against the live position. `_units` now stores only `IEntity`; the captured position is used only for the cell key.
2. [x] Shared private `CollectNearby(origin, range)` fills a reused `_nearby` list. `Decide` and `FindBeamTarget` both use it, so the beam target search now uses the grid. Shared `IsVisibleHostile(entity, position)`.
3. [x] Forces are added through the injected `ForceCompositionBuilder.AddUnit`; the snapshot no longer calls the static `GetOffenseScale`. `ForceCompositionBuilder` is now registered in `SkirmishScope`.
- Behavior notes: the cached-position prefilter is gone (one more `Transform.position` read per unit in nearby cells, which have to be read anyway when in range). `AddUnit` looks up the unit profile per add instead of using the profile cached at capture. The beam target can miss a hostile that crossed a cell boundary since capture, the same limit `Decide` already had.
- Follow-up (not done): `AiPlayerScope` still registers its own `UnitCombatProfileCatalog` and `ForceCompositionBuilder`, duplicating the `SkirmishScope` ones.
- Verification: Unity recompile completed with 0 errors; 0 console errors. Tests not run; run `Abilities`, `Core`, `EnemyAi`.

### Phase 4 — Monitor and service (done 2026-11-02)
1. [x] `AutoPowerMonitor(AutoPowerTuning)`: `Sample(now, shields, maximum, suppressed, shieldRevision)` and `Confirm(decision)` no longer take tuning or a check count. The `_maxShields`/`_suppressed` reset comparison is gone; only a new `shieldRevision` (or a gap longer than the window) resets the history. Verified: every `HealthModel` mutator of max shields or suppression (`RefreshStatModifiers`, `SetConstructionProgress`, `Upgrade`, `SetShieldSuppressed`, `Restore`) raises `OnValueChanged` → `ShipAutoPowerFacade.ShieldRevision++`.
2. [x] Public `ManualUntil` replaced by `HoldManual(now)` (sets the hold and resets confirmation) and `IsHeld(now)`. `Suspend` keeps the hold. `HandleManualAction` is now one line.
3. [~] `Check`: kept two slot passes, because "any slot Active" must be known before `Decide` runs. The second pass is flat; confirmation resets once after a confirmed decision (before, it reset only when a matching slot existed; without one nothing could fire anyway). The redundant reset during the manual hold is dropped (`HoldManual` already resets, and `Confirm` is not called while held).
4. [x] Service constructor uses one assignment per line.
- Tests: `AutoPowerRulesTests` updated. New: `ShieldRevisionResetsDamageHistory` (30 loss across a revision change stays under the 25 % threshold), `SuppressedDropsAreNotDamage`, `ManualHoldResetsConfirmationAndSurvivesSuspend`. The confirmation test now uses the default `ConfirmationChecks = 2`.
- Verification: Unity recompile completed with 0 errors; 0 console errors. Tests not run; run `Abilities`, `Core`.

### Phase 5 — Facade wiring
1. [x] (done 2026-11-02) `ShipAutoPowerFacade` takes `IAutoPowerWeaponObserver` in its constructor (`readonly _weapons`). Removed `BindWeapons` and `Ship`'s `autoPower`/`autoPowerWeapons` constructor parameters. No cycle: `WeaponComponent.Construct` does not depend on the facade. The facade is still created and initialized because `OrderedLifecycleCoordinator` resolves every `IInitializable` in the entity container.
- Verification: Unity recompile completed with 0 errors; 0 console errors. Tests not run; run `Ships`, `Core`.

## Outcome (2026-11-02)
- All 5 phases implemented. Removed `AutoPowerEligibility.cs`, `AutoPowerRules.cs` and `BindWeapons`; renamed `AutoPowerDecision` → `AutoPowerRole`. Eligibility is catalog data (`ShipAbilityDefinition.autoPower`).
- Eligible set unchanged (Shields `3`; Weapons `5, 10, 12, 20, 23, 24, 31, 32, 33, 38`).
- Each phase compiled with 0 errors in Unity. EditMode tests not run (local override) → user acceptance queue.
- Guidance extracted → [[GameDesign/Abilities_And_Superweapons#Automatic player ship powers]] (data eligibility, beam targeting by `requiresEnemyTarget`).
- Open follow-up: `AiPlayerScope` duplicates the `UnitCombatProfileCatalog`/`ForceCompositionBuilder` registrations from `SkirmishScope`.

## Important Values
- Unchanged: `CELL_SIZE = 512`, `DecisionInterval 0.5 s`, `CastersPerFrame 4`, `ConfirmationChecks 2`, `ManualActionDelay 2 s`, all thresholds in `AutoPowerTuning`.
- Expected: 2 files deleted (`AutoPowerEligibility`, `AutoPowerRules`), about 60–90 lines removed, no new types (one enum renamed).

## Edge Cases
- Entity id reuse: `ReferenceEquals` protects against a new entity under an old id. Check whether `EntityLocator` ids are ever reused before dropping it; otherwise keep it inside `IsLive`.
- An ability added later without the `autoPower` value → `None` (opt-in). The balance validation could flag `Boost*Power` settings with `None`.
- Save files are unaffected (`UnitSave.autoPowerDisabled` stays).

## Open Questions
- ~~Rule tests~~ → resolved 2026-11-02: public instance `ChooseRole` on the snapshot.
- ~~`GetOffenseScale`~~ → resolved 2026-11-02: inject `ForceCompositionBuilder` (registered in `SkirmishScope`).

## Verification
- Categories: `Abilities` (Services/ShipAbilities), `Ships` (Entities/Ship), `Core` (`RealEntityCompositionTests` auto power cases), `Ui` (`ShipAutoPowerUiTests`), `Balance` (catalog asset).
- Tests are disabled locally ([[AGENTS]] override): verify by compile + source review; the user runs the tests and checks in game.

## Files
- See Current Shape; plus `ShipAbilityDefinition.cs`, `Entities/Ship/Ship.cs`, `ShipAbilityCatalog` asset, `Tests/Editor/AutoPowerRulesTests.cs`, `Tests/Editor/RealEntityCompositionTests.cs`.
