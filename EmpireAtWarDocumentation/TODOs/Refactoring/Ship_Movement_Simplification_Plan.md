---
category: Refactoring
status: in-progress
---
# Ship Movement Simplification Plan

## Lifecycle Review

- Reviewed: 2026-09-30; recorded initialization-order retry and later phases remain unverified. Keep active; do not infer completion from newer movement code.

## Recorded Status

- Created: 2026-09-23; advisory snapshot, verify against live source.
- Phases 0–2 implemented and manually verified on 2026-09-23.
- Phase 3: initialization-order fix awaiting skirmish retry; Phases 4–5 pending; Phase 6 optional.
- User directed removal of `FormerlySerializedAs` after 10 prefabs were rewired.

## Goal

- Restore serialized references and correct StarDestroyer2 yaw pivot.
- Smooth yaw acceleration/banking; simplify movement state and contracts.
- Preserve hyperspace entry, queued/blocked/deferred orders, pursue, stop, route line.

## Implementation

1. Restore/rewrite renamed serialized fields; compare original fileIDs on all 10 ship prefabs.
2. Shift SD2 root children + collider by horizontal hull-center offset; keep root/Y and body roll semantics.
3. Use one yaw writer, angular acceleration/braking, rate-based bank, turn-radius-aware curves.
4. Original Phase 3 target: `ShipMoveModel` + presenter + view; check the later simplification plan before implementation.
5. Slim interface, replace mediator wiring, move screen conversion/vision ownership to appropriate boundaries.
6. Simplify navigation parameters; optional other pivots/AI work require approval.

## Important Values

- `BANK_SMOOTH_TIME = 0.6f`; position tolerance `0.05`; height tolerance `0.5`.
- Original rotation rates (deg/s): Arquitens 45; Munificent 35; Acclamator 30; Recusant 25.
- Venator/Providence/HeavyDreadnought 7.5; SD1/SD2 5; Lucrehulk 2.5.
- SD2 collider center before: `(0.1206, 10.4224)` in xz; after: `(0, 0)`.
- Main hull center after: `(−0.12, −0.05)`; aggregate bounds are skewed by TrenchPlating/Base meshes.

## Edge Cases

- Removed serialization aliases before prefab rewrite → null renderer/body references on reload.
- Aggregate renderer bounds → invalid SD2 pivot acceptance criterion; use approved collider/main-hull evidence.
- Moving look-at target → update desired facing without restarting bank/velocity.
- Small-ship data tuning → measure first, then user sign-off; capitals unchanged in the original suggestion.
- Structural phases → compile, save/import assets, verify behavior before advancing; automated tests require explicit request.

## Files

- [[TODOs/Refactoring/Ship_Movement_Simplification_Plan - Research]] — original interfaces, phases, values, asset GUID, measurements and implementation records.
- Later component-merge plan: `ObsidianDocumentation/TODOs/Ship_Movement_Simplification_Plan.md`.
