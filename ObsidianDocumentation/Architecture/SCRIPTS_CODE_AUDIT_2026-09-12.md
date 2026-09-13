# Scripts code and architecture audit

**Date:** 2026-09-12 · **Revision:** `51626e2a` · **Scope:** core framework, Zenject wiring, gameplay entities and UI under `Assets/Scripts`.

**Verdict: partially unified.** Shared entity registration and component wiring exist, but combat, lifecycle, identity and UI responsibilities still follow different patterns. Fix the behavioral defects below before consolidating abstractions.

Static source/reference review with selected serialized-asset checks; no tests, Play Mode or builds were run. Findings are source-based, not runtime reproductions. No code or assets were changed.

## Fix first

1. **High — Engine damage does not reach ship movement.** `LateInitializableService` is bound only in ProjectContext and receives its initial list there. Ships are created in child contexts; Zenject resolves dependencies locally/upward, not into children. Consequently `Ship.LateInitialize()` never subscribes to engine health changes, disconnecting the intended speed reduction. Initialize this subscription inside each ship context after health initialization. Evidence: [project binding:32](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/SceneContext/ProjectContextInstaller.cs:32), [ship factory:24](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/SceneContext/Skirmish/GameUnitsInstaller.cs:24), [subscription:135](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Ship/Ship.cs:135), [slowdown:369](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Ship/Ship.cs:369). Container lookup verified in the installed [Zenject source:80](/Users/golinsky/Projects/empire-at-war/Assets/Plugins/Zenject/Source/Main/DiContainer.cs:80).

2. **High — Addressable loads have no release owner.** Every repository load increments asset references, but handles are discarded and no matching Addressables release exists in project scripts. Repeated spawning/scene changes retain loaded assets and dependencies. Give cached loads explicit ownership and release them when their consumers finish. Evidence: [AddressableRepository:14](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Repository/AddresableRepository.cs:14). Verified against installed Addressables memory-management documentation; memory growth was not measured.

3. **High — Build queues lose their active slot too early.** Queue two units of one type. Completing the first changes `_count` from 2 to 1 while the second remains active, but `OnFinishPipeline` removes the dictionary entry whenever the count is 1. Adding another unit now creates a second slot for the same ID; the old slot can later remove the new mapping. Remove entries only when the queue is actually empty, with an unambiguous remaining-count contract. Evidence: [PipelineView:72](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Faction/Ui/PipelineView.cs:72), [BuildPipelineView:69](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Faction/Ui/BuildPipelineView.cs:69).

## Unification and architecture

| Area | Assessment and focused action |
|---|---|
| Entity creation | Ship, station, defense platform and mining facility share `DynamicEntityInstaller`, `EntityInstaller`, registration and health/radar/selection concepts. Keep this shared foundation. |
| Combat | Ships bind `WeaponComponent`; stations/platforms still bind **obsolete `AttackComponent`** and the older state-machine path. Unify the combat contract and migrate the remaining consumers before removing legacy code. [Ship:88](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Ship/ShipInstaller.cs:88), [station:70](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/SpaceStation/SpaceStationInstaller.cs:70), [platform:64](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/DefendPlatform/DefendPlatformInstaller.cs:64). |
| Lifecycle | Release guards, component release loops and death-effect coordination are repeated across entity classes. Standardize their lifecycle contract and extract only shared behavior. Movement/AI and mining income are legitimate differences. [Station:84](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/SpaceStation/SpaceStation.cs:84), [facility:79](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/MiningFacility/MiningFacility.cs:79). |
| Identity/contracts | Two `IEntity` interfaces mean different things: framework `string Id` versus runtime `long Id`. Duplicate `IMoveCommand`, `IAttackCommand` and `ISettingsCommand` names also remain. Clarify/rename distinct roles; remove obsolete contracts after reference checks. [Framework:3](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/BaseSystem/FrameworkContracts.cs:3), [runtime entity:9](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/BaseEntity/Entity.cs:9). |
| Models | `PureModel` does not enforce purity: health and movement models inject Unity `Transform` through Zenject. Pass state through presenters; keep Unity objects in views/adapters. [HealthModel:80](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Health/HealthModel.cs:80), [ShipMoveModel:43](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/Ship/Movement/ShipMoveModel.cs:43). |
| UI | Main menu uses manual presenter wiring; pause menu uses injected generic `BaseUi`. Standardize wiring/lifecycle conventions without merging different screens. More seriously, `PipelineView` owns queue counts/timing, and its animation callback triggers actual unit construction. Move build state/completion into the gameplay model/service and let UI display progress. [Menu:26](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/MenuUi/MenuUiView.cs:26), [pause:15](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Menu/Ui/PauseMenuUi.cs:15), [construction:89](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Faction/Ui/ShipBuildUiController.cs:89). |

## Deprecated and unnecessary code

- **Deprecated but active:** `AttackComponent`; `ITimerPoolWrapperService` is also marked obsolete yet remains the scene-loading timer dependency. Complete their migration; neither is safe to delete immediately. [Timer contract:9](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/TimerPoolWrapperService/TimerPoolWrapperService.cs:9).
- **Removal candidates:** `DynamicViewInstaller`, `BaseDynamicViewInstaller` and obsolete `WeaponVfxView` have no discovered consumers or serialized GUID references. The two unused installer families duplicate model/controller/view construction. [Installer folder](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/SceneContext/DynamicViewInstallers), [WeaponVfxView:7](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Components/ViewComponents/Weapon/WeaponVfxView.cs:7).
- **Active no-ops:** `BattleService` subscribes to selection but every handler branch is empty. `PopupService.ClosePopup` is commented out; the view closes itself, so this is an incomplete contract, not a stuck-popup defect. [BattleService:21](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/BattleService/BattleService.cs:21), [PopupService:45](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Services/Popup/PopupService.cs:45).
- **Additional queue risk:** a full pipeline list produces a null dereference instead of a capacity decision. There are seven serialized slots; the model only limits counts per unit ID. Handle capacity before accepting a purchase. [Allocation:48](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Faction/Ui/BuildPipelineView.cs:48), [limit:69](/Users/golinsky/Projects/empire-at-war/Assets/Scripts/Entities/Faction/Model/PlayerFactionModel.cs:69).

**Suggested order:** fix the three high-priority defects → move production ownership out of UI → finish the combat migration → remove unused/no-op scaffolding → standardize contracts and lifecycle incrementally. Use [[UI_REFACTORING_PLAYBOOK]] for the UI migration.

## Execution record — 2026-09-12

The implementation below supersedes the original audit's proposed actions where noted. The findings above describe the pre-change revision.

### User overrides

- Removed `LateInitializableService` and `ILateIInitializable`. Fixed injection and initialization order inside each entity context instead of retaining a delayed initialization service.
- Renamed `IRepository` to `IAssetService`, and `AddressableRepository` to `AddressableAssetService`, including their files and consumers. Unity script GUIDs were preserved.
- Addressables release, caching and handle ownership were deliberately excluded at the user's request. The original asset-retention finding remains unresolved by design.

### Implemented

- Health initializes before the entity subscribes to engine damage. Dynamic entity root Transform binding is registered before prefab injection. Engine slowdown preserves an active movement destination and replans at the reduced speed.
- Production queues, elapsed time, admission and completion now belong to `PlayerFactionModel` and `FactionService`. UI renders snapshots. A queue retains its slot until empty; seven distinct pipelines are admitted before charging; cancellation refunds the actual queued request.
- Stations and defense platforms use `WeaponComponent` through `StationCombatPresenter`. Migrated all three prefabs, including weapon origins, ordered hardpoint references and weapon types formerly stored in the legacy dictionary. Removed the old attack component/model/contracts and their state-machine implementation after checking references.
- Extracted guarded component release into `EntityComponentLifecycle` for ships, stations, platforms and mining facilities while retaining their specific death/income behavior.
- Health, hardpoint and ship movement models use pure C# state and explicit dependencies. Health and movement components/adapters own Unity references and rendering. Removed obsolete serialized runtime-model fields from the affected data assets.
- Renamed the framework string-identity contract to `IFrameworkObject`, retained the runtime entity contract, separated framework contracts into their own files, and removed unused duplicate command contracts.
- Pause menu uses explicit presenter/view setup and symmetric listener disposal. Popup service close requests now reach the view without recursive command dispatch.
- Replaced the obsolete timer wrapper with direct `TimerPoolService` injection and a small Zenject tick adapter. Removed the empty `BattleService`, unused dynamic view installers and unused weapon VFX view.
- Added or updated regression test source for production queues, health state, movement and lifecycle changes. Tests were not executed.

### Verification and remaining validation

- Unity compilation completed successfully after the code changes and legacy removals. The Unity Console reported zero errors.
- All three combat prefabs were saved through Unity. All 17 weapon-origin/hardpoint records, including weapon types and persistent object identities, matched the captured legacy configuration.
- Re-serialized and saved the 13 affected data assets. Comparison confirmed that gameplay values were preserved; Unity removed obsolete model links and empty runtime-model serialization. Remaining radar models were present on all 13 loaded assets.
- No serialized references remain to the 19 deleted scripts. New scripts have Unity metadata, and renamed scripts retain their original GUIDs.
- Source whitespace checks passed. Independent static review found no actionable injection, initialization, movement-conversion or reentrant-release regressions.
- Automated tests, Play Mode and builds were not run. Gameplay behavior still needs runtime validation; the checks above establish compilation, source consistency and serialized-asset persistence.

### Authorized test run — 2026-09-12

After the user requested tests, ran the complete `EmpireAtWar.Tests` Edit Mode suite asynchronously. The first run passed 165 of 166 tests; `PipelineViewLifecycleTests.Destroy_RemovesSkipListener` failed because its Edit Mode setup did not invoke `Awake`. Corrected that test setup without changing production code and reran the complete suite: **166 passed, 0 failed, 0 skipped**, in **6.39 seconds**.

Scene safety checks before execution and afterward confirmed that `MainMenuScene` remained open and clean. Test discovery found no project-owned Play Mode tests. Manual gameplay and build validation remain outstanding.

### Startup injection correction — 2026-09-13

The reported skirmish startup failure exposed a binding-order bug missed by the original direct-construction tests and static review. `PlayerCoreInstaller` called `ResolveId<FactionType>` inside the unfinished `PlayerFactionModel.WithArguments(...)` chain. Zenject's resolve finalized the pending binding and copied its still-empty argument list. The model then could not resolve its unqualified constructor faction, interrupting scene injection and producing the later reinforcement-zone/kernel exceptions.

Moved faction resolution before starting the model binding. Added `PlayerCoreInstallerTests` using the real installer and a parent container's identified faction binding. Both faction cases reproduced the original exception before the fix. After the fix, the complete project Edit Mode suite passed: **168 passed, 0 failed, 0 skipped**, in **6.91 seconds**.

Also verified actual Play Mode startup through `IGameCommand.StartGame`: Republic versus Separatist on Coruscant loaded and ran with zero Console errors. Exiting Play Mode also produced zero errors and restored the clean `MainMenuScene`. The temporary runtime background-execution setting was restored. No reinforcement-zone fallback or initialization-order workaround was added.
