---
category: Bugs
status: in-progress
agent_marker: AGY
created: 2026-11-18
---
# [AGY] Capital Movement and Class Rework Fixes (Audit & Reconciled Plan)

## Goal
- Reconcile contradictions in [[TODOs/Bugs/Capital_Movement_And_Class_Rework_Fixes]] against live git commits (`aec386848`, `d813ee1ea`, `ce000bbd9`) and the 2026-11-17 telemetry (`16:01`, `16:23`, `16:47`, `17:23`).
- Document the proven root cause of `Player0` ISD II (`entityId=256`) driving northeast to `(1678.2, 2140.1)` before turning 180° in place.
- Complete the post-`ce000bbd9` turn-rate verification and the 76-ship root `BoxCollider` corner-to-centre vs `NavigationRadius` footprint audit.

## Reconciled Findings (Corrections to Earlier Plan Note)
1. **Phase 0 ISD I spawn fix status**: Committed as `aec386848` (`Fix ISD I spawn area resolving to a map-corner pocket`, 2026-11-17 15:12), not uncommitted.
2. **H1 contact-range check**: Previously implemented (`f7112a5f1`) then reverted by `d813ee1ea`; Progress checkboxes/notes must not claim H1 is active.
3. **Mandator / Praetor `entityId=247` (`Player1`, `hull0=85000.0`, `shipType=19` in `isdIbug.json`)**:
   - Earlier report claimed `247` stayed frozen at `(1255.9, -1696.3)` for `147 s`.
   - `Loading_20261117_160137_ai_orders.csv` shows `247` was in `Idle` movement phase (`mv=Idle;...`) fighting at range during `65.86–113.15 s` (`99`–`175 s` segments mixed) and `149.48–178 s` as hull dropped `78,831.3 → 51,082.0`; stationary turning (`mv=Moving;...;0.0`) occurred around `15–60 s`, `131–141 s`, and `180–207 s` (moving at `t=207.43 s`).
4. **Re-issued orders cost (`BrakeForReplan`, not turn reset)**:
   - `ShipMoveComponent.MoveTo` / `StartRoute` (`ShipMoveComponent.cs`) preserves `_turnSpeed` across re-plans while stationary.
   - However, when already moving (`_currentSpeed > 0.01f`) and a new route's heading differs by `> 1°`, `BrakeForReplan` triggers active braking (`braking=1`) to a full stop before turning in place (observed on `247` in `16:47` at `50.51 s`: `mv=Moving;1;0;1;0;0;1383.6;-2333.3;13.3` right after switching `Attack → AttackMove`).
5. **Turn acceleration after `ce000bbd9`**:
   - `ce000bbd9` (`Double RotationSpeed of slow-turning ships (<=12 deg/s)`, 50 `ShipData` assets, 2026-11-17 16:13:09) doubled `RotationSpeed` while leaving `MaxRotationAcceleration` at the older half-rate (`~1–2 s` extra ramp per turn). `ShipNavigationService.EstimatePathDuration` omits turn acceleration ramp time.

## Proven Cause: ISD II (`entityId=256`) Driving Away to `(1678.2, 2140.1)` in `16:01`, `16:47`, `17:23`
- **Save source**: `Saves/Skirmish/isdIbug.json` (loaded by `16:01`, `16:23:56`, `16:47`, and `17:23`).
- **`TryUpdateArtilleryMove` ruled out**: `ISDIIShipData.asset` has `usesArtilleryStandoff: 0` → `AttackTargetState.UpdateFormationMove` ran instead.
- **Exact saved order & `AttackOffsets` math**:
  - In `isdIbug.json`, `Player0` `256` (`ISDII`, at `(1087.7, 1405.6)`) and `257`, `259`, `260` (`VictoryII`, `shipType=203`) all hold a saved human `Attack` order (`order.type=2`) on structure `targetId=164` (`(1284.66, -1860.89)`) with **`|offset| = 7,444.31 m`** (`256` has `offset = (2293.34, 0, 7082.25)`).
  - Exact match to `UnitOrderService.AttackOffsets` (`UnitOrderService.cs:83-108`): $1059.4355 / \sin(\pi / 22) = 7444.31\text{ m}$ (an Executor's `NavigationRadius = 1059.4355 m` in a 22-unit selection, where `AttackOffsets` also includes `Squadron` receivers via `IAttackFacade` even though `Squadron.Attack` ignores `formationOffset`).
  - `AttackTargetState.UpdateFormationMove` Stage 1 (`_isClosingRange == false`, `L71-72, L210`) sends `256` to `MovementTargetPosition = (1284.66 + 2293.34, -1860.89 + 7082.25) = (3578, +5221)`, clamped to northeast corner **`(1678.2, 2140.1)`** (`14–59 s`).
  - Upon arrival at `64.3 s`, Stage 1 ends and Stage 2 commands `StandoffPosition = (1434.8, -1397.1)`, forcing `256` to rotate 180° in place (`64–101 s`).

## Post-`ce000bbd9` Turn-Rate Verification (`16:01` vs `16:47` & `17:23`)
- *Note on `16:23`*: Both `16:23:37` (`90` rows, `25.65 s`) and `16:23:56` (`54` rows, `43.14 s`) stopped too early to complete turns; `16:47` (`Loading_20261117_164732_ai_orders.csv`) and `17:23` (`Loading_20261117_172317_ai_orders.csv`) provide full battles. No `Executor` (`hull0=150000`) spawned in `16:01`, `16:47`, or `17:23`.
- **ISD II `256` (`RotationSpeed` `5 → 10°/s`)**: 180° turn-in-place at `(1678.2, 2140.1)` dropped from `8` stationary 5 s samples (`64.3–99.3 s` in `16:01`) to **`0` stationary 5 s samples** (`71.48–81.49 s` shows `spd=0.1`, moving at `10.2 m/s` by `86.49 s` in `16:47` and `102.73 s` in `17:23`).
- **Allegiance `267` (`4 → 8°/s`, `Player0`)**: Stationary turn samples dropped from `13` (`9.25–47.69 s` in `16:01`) to **`4`** (`16.43–36.44 s`, moving at `20.0 m/s` by `41.44 s` in `16:47` and `17:23`).
- **Praetor/Mandator `247` (`hull0=85000`, `Player1`)**: First non-zero speed dropped from `207.43 s` (`16:01`, `29` stationary samples) to `98.85 s` (`16:47`, `9` stationary samples; first initial move at `49.99 s` = `33.5 s` after order, interrupted by AI `Attack → AttackMove` `BrakeForReplan` at `50.51 s`) and `83.07 s` (`17:23`, `19` stationary samples).
- **Titans (`hull0=48300`)**: `446`/`452` in `16:01` and `441` in `17:23` (`3 → 6°/s`): `441` began translating at `130.22 s` (`~27.8 s` after order at `102.36 s`).

## 76-Ship Root `BoxCollider` Corner-to-Centre vs `NavigationRadius` Audit
- *ISD II clarified*: `103.20 m` was pre-rescale (`Logs/ShipScale/before.json`: `hx=50.5, hz=90.0`); post-rescale (`Logs/ShipScale/after.json` and live `ISDII.prefab`) is **`96.32 m`** (`hx=47.13, hz=84.00`) vs `NavigationRadius = 91.46 m` (`+4.85 m`).
- Across `74` parsed root `BoxCollider` ship prefabs: `0` have `NavigationRadius < halfLen`, **`18` have `cornerDiag > NavigationRadius`** (`> 0.05 m`):

| Ship | `NavigationRadius` | Longest Half-Length | Corner-to-Centre Diag | Excess (`Diag - Nav`) | Box Size `(X, Z)` |
|---|---:|---:|---:|---:|---|
| `Praetor` | `317.10` | `317.11` | `399.57` | `+82.47` | `(486.2, 634.2)` |
| `Procurator` | `133.00` | `130.00` | `154.19` | `+21.19` | `(165.8, 260.0)` |
| `DHOmni` | `75.00` | `72.44` | `89.83` | `+14.83` | `(144.9, 101.4)` |
| `Imperator` | `85.05` | `84.00` | `96.08` | `+11.03` | `(93.3, 168.0)` |
| `MonCalCruiser` | `63.00` | `63.00` | `71.62` | `+8.62` | `(68.1, 126.0)` |
| `Lucrehulk` | `229.74` | `185.33` | `237.57` | `+7.83` | `(296.8, 334.6)` |
| `Dispatcher` | `32.05` | `31.50` | `37.91` | `+5.86` | `(42.2, 63.0)` |
| `Victory` | `48.11` | `47.25` | `53.68` | `+5.57` | `(51.0, 94.5)` |
| `MC80bRemonda` | `63.00` | `63.00` | `68.34` | `+5.34` | `(53.0, 126.0)` |
| `ISDII` | `91.46` | `84.00` | `96.32` | `+4.85` | `(94.3, 168.0)` |
| `HomeOne` | `199.49` | `199.49` | `204.15` | `+4.66` | `(86.8, 399.0)` |
| `AssaultFrigateMkII` | `40.42` | `36.75` | `44.77` | `+4.35` | `(51.2, 73.5)` |
| `Acclamator` | `47.46` | `44.85` | `51.24` | `+3.78` | `(49.6, 69.1)` |
| `BulwarkII` | `60.00` | `57.12` | `63.26` | `+3.26` | `(54.4, 114.2)` |
| `Venator` | `72.00` | `67.92` | `74.05` | `+2.05` | `(59.0, 124.0)` |
| `Secutor` | `180.00` | `140.00` | `181.83` | `+1.83` | `(232.0, 280.0)` |
| `MC80Freedom` | `65.58` | `63.58` | `65.95` | `+0.37` | `(35.0, 127.2)` |
| `C9979` | `30.99` | `29.17` | `31.18` | `+0.19` | `(58.3, 22.0)` |

## Remaining TODO
- [ ] Reconcile checkboxes/Progress/Phase-0 status in [[TODOs/Bugs/Capital_Movement_And_Class_Rework_Fixes]] (if keeping original note) and decide whether `NavigationRadius` for the `18` ships above should be bumped to `>= cornerDiag` (or keep tight turning footprint with multi-circle/box clearance)
- [ ] Complete dedicated Executor (`hull=150000`) & Titan move-start verification under clean orders without `isdIbug.json` `7,444 m` offset
- [ ] Route `ISD II 256` wrong-way / map-edge leg fixes through [[TODOs/Bugs/Attack_Order_Hold_In_Range_And_Formation_Offsets_AGY]]
- [ ] Review `BrakeForReplan` `1°` threshold / deceleration vs course blending in `ShipMoveComponent.cs`
- [ ] Address `72` `[ShipNavigation] Tangent route build failed` warnings (`64` code 1, `8` code 3) observed across Host Battles 1–4
