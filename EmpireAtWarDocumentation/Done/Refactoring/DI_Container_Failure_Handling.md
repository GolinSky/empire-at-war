---
category: Refactoring
status: done
created: 2026-11-06
completed: 2026-11-06
acceptance: passed
---
# DI Container Failure Handling

## Implementation

- [x] `OrderedLifecycleCoordinator` runs all ordered callbacks, then all late callbacks, in reverse order. Repeated disposal is a no-op. One failure preserves the original exception; multiple failures retain callback order.
- [x] `EntityScopeOwnership`, `PlayerScopeBootstrap`, and `PlanetFactory` dispose and destroy every owned child. Entity teardown uses a snapshot because destruction unregisters the scope.
- [x] `AssetLifetimeScope` releases its cache after container cleanup. Project preloader, loader, and cloned project/battle data release even after an earlier failure.
- [x] `PlanetFactory` publishes the planet only after build and activation. Entity and planet creation roll back failed children and report both creation and cleanup errors.
- [x] Failed player bootstrap unsubscribes hosted levels, clears the factory lookup, then disposes and destroys created players in reverse. A failed initial hosted-level sync removes its new subscription.
- [x] AI players inherit the battle’s `UnitCombatProfileCatalog` and `ForceCompositionBuilder`.
- [x] `EntityInstaller` unwraps scope-local component lists. Ship destruction uses `IEntityScopeRelease` with the configured delay. Squadron pool transitions use `IEntityPoolActivation` with the original tick-gate order.
- [x] Source audit and compilation completed. Reusable rules → [[Architecture/VCONTAINER_LIFECYCLE]].

## Edge Cases

- VContainer 1.19.0 also tracks `GraphicsSettingsApplier` and `InputActionsProvider` in project scope, `BattleSaveActions` in battle scope, and the framework entry-point dispatcher. Its stack pops before calling each disposer. `CleanupFailures.DisposeContainer` resumes disposal until VContainer clears the scope.
- Player build failure propagates through `OrderedLifecycleCoordinator.Initialize` and VContainer’s entry-point handler. Scene activation can leave the battle scene loaded with startup incomplete. There is no automatic menu return or retry.
- Play Mode schedules GameObject destruction for the end of the frame. Edit Mode destroys immediately.

## Verification

- Unity 6000.4.7f1 Roslyn compiler with project Bee response files compiled runtime and Editor assemblies, including regression fixtures, with zero errors.
- Coverage authored in `VContainerCompositionTests`, `EntityFactoryLifecycleTests`, `RealEntityCompositionTests`, and `ProductionAccessTests`. Cases cover ordered and late failures, continued raw disposal, cache release, failed entity/planet/player creation, hosted subscriptions, and AI singleton identity.
- Unity 6000.4.7f1 EditMode rerun, 2026-11-06: 62/62 targeted cases passed in an isolated checkout of `01f7aa7b1` with the fixes applied. Entity scopes expose release/activation without duplicate ownership. Teardown releases only acquired native buffers, UI subscriptions/views, and pool roots after a skipped or interrupted initialization.

## Independent Review (2026-11-06)

- Source review of `01f7aa7b1` + `7f1c0ca0b` against the original 8 steps: all implemented. Editor compile clean at 14:30 (no `.cs` newer than `Assembly-CSharp.dll`; 0 console errors). Tests not rerun by this review.
- VContainer 1.19 checks: `WithParameter<T>(Func<IObjectResolver,T>)` exists; `Lifetime.Transient` func registrations are not added to `disposables` → `EntityLifetimeScope` is not disposed by its own container.
- `CleanupFailures.DisposeContainer` terminates: each retry pops ≥1 disposable; loop exits once `DisposeCore` clears `Container`.
- Minor, not blocking:
  - `ProjectScope`/`SkirmishScope.OnDestroy` wrap `base.OnDestroy` (already aggregating) → nested `AggregateException` when 2+ failures.
  - `PlayerScopeBootstrap.Initialize` re-entry guard (`InvalidOperationException`) is outside plan scope; conflicts with the "no routine defensive checks" rule.
  - Fixed 2026-11-06 (`da59d8ed5`): `CleanupFailures.Add` flattens `AggregateException` inner exceptions → one flat list; re-entry guard removed from `PlayerScopeBootstrap.Initialize`. Recompile: 0 errors in these files (2 unrelated errors in in-progress Battle Area Access `CaptureSites` work). Tests not run.
  - `7f1c0ca0b` teardown guards (`_tooltipHover != null`, `IsCreated`, `_root != null`, `_view == null`) make disposal tolerate skipped `Initialize`; required now that disposal continues after failures.
