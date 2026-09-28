# Scripts Code and Architecture Audit — 2026-09-12

## Scope

- Original revision: `51626e2a`.
- Core framework, Zenject wiring, gameplay entities, UI under `Assets/Scripts`.
- Initial audit: static review; later execution records supersede original proposals.

## Decisions and Results

- Entity initialization replaces `LateInitializableService` / `ILateIInitializable`.
- `IRepository` → `IAssetService`; `AddressableRepository` → `AddressableAssetService`; script GUIDs preserved.
- Production queues belong to `PlayerFactionModel` / `FactionService`; UI renders snapshots.
- Stations/platforms use `WeaponComponent` + `StationCombatPresenter`.
- `EntityComponentLifecycle` owns guarded release; health/movement state uses pure C# models.
- Popup/pause UI has explicit setup and symmetric disposal.

## Verification Record

- Combat assets: 3 prefabs; 17 weapon-origin/hardpoint records preserved.
- Data assets: 13 reserialized; gameplay values preserved.
- Deleted scripts: 19; no serialized references remain.
- 2026-09-12: `EmpireAtWar.Tests` → 166 passed, 0 failed/skipped; 6.39 s after fixture correction.
- 2026-09-13: `PlayerCoreInstaller` binding-order fix → 168 passed, 0 failed/skipped; 6.91 s.
- `IGameCommand.StartGame`: Republic vs Separatist, Coruscant; startup/exit had zero Console errors.

## Edge Cases

- Resolve faction before starting `PlayerFactionModel.WithArguments(...)`; resolving mid-chain finalizes incomplete bindings.
- Full build queue → reject before charging; retain each slot until empty.
- Addressables release/cache ownership remains unresolved by explicit user decision.

## Files

- [[SCRIPTS_CODE_AUDIT_2026-09-12 - Research]] — original findings, source links, migration and validation records.
- [[UI_REFACTORING_PLAYBOOK]] — UI migration procedure.
