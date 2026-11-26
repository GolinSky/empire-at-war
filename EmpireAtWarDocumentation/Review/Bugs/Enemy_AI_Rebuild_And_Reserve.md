---
category: Bugs
status: review
created: 2026-11-26
tags:
  - enemy-ai
  - production
acceptance: pending-user
---
# Enemy AI Rebuild And Reserve

## Goal
- A wiped or stationless AI always rebuilds a fleet (no zero-ship stall).
- The AI may buy past the fleet cap into a reserve, like the human.
- A saved big ship (Executor) eventually deploys.

## Evidence (player build, 2026-11-26 20:55, `Loading_20261126_205509_ai.csv`)
- Rebel P1: both stations lost ~1925 s → 0 ships, 3/120 pop, bank 7k → 140k for ~370 s; bought only squadrons.
- Cause: tier floor (`SHIP_TIER_SPAN = 1`, level 5 → only L4+ ships) + W56 `CanLandShip` (no own station and < `CAPITAL_ESCORT_SHIPS` (4) ships → no assault capital). Together: no ship type left.
- 75 `deploymentBlocked tier=body` rows; capitals refunded after 15 s (`capitalRefunded` not in player telemetry).
- Empire P2: saved for Executor (cap 50) from ~1500 s to the end, bank 125k → 606k. Every freed slot refilled by the W46 fitting ship → 50 free never reached.
- Human queues past the cap (capacity checked at spawn); AI reserved pop at purchase → no reserve.

## Decision (user 2026-11-26)
- Rebuild by role, not by name list: fleet < 4 ships and no landable affordable capital → frigates, cruisers, heavy cruisers with `AntiCapital`, `Artillery`, `Carrier` or `Dps` bypass the tier floor; counter model picks among them.
- Reserve beyond the cap: one full fleet (120 → purchase limit 240). Ships deploy only into live capacity.
- Deployment by capacity is FIFO: the oldest capacity-held ship keeps freed slots unless both fit; station-critical defense skips this.
- Refund telemetry in player builds, aggregated (no spam); a refund pauses stationless capital purchases.
- Hit-and-run tactics (2–3 heavy cruisers strike capitals, pull back, repeat, also on defense) → separate task.

## Steps
- [x] Role-based rebuild pool: `EnemyRebuildRolePolicy` (Frigate/Cruiser/HeavyCruiser + `AntiCapital|Artillery|Carrier|Dps`); `IsShipTierAllowed` lets them past the floor while `_rebuilding` (active ships < `CAPITAL_ESCORT_SHIPS` and no landable, affordable assault capital). Roles ride on `UnitCombatProfile.Roles` (from `ShipData.Roles`).
- [x] Capital refund: `capitalRefunded` in compact (player) telemetry, one row per AI per 60 s with `refunds=N`; `EnemyFactionModel.LastCapitalRefundAt` + `CAPITAL_REFUND_PAUSE_SECONDS` (60 s) block stationless capital purchases (`CanLandShip`).
- [x] Reserve: `EnemyUnitLimitModel.RESERVE_FLEETS = 1` → purchase limit 2 × `MaxUnitCapacity`; `DeployedUnitCapacity` via `MarkDeployed` (in `Track`) / `Release`. Ships and squadrons held with `EnemyBuildQueue.HoldForCapacity` (no blocked seconds → never forced/refunded for the wait).
- [x] FIFO capacity priority: `_capacityWaiter` (oldest unit that does not fit) keeps freed slots; others land only when both fit; station defense (`defendHome`) skips the order, never the cap. Waiter cleared on deploy or `ReleaseUnit`.
- [x] Critical-station defender check uses live capacity (`DeployedUnitCapacity`).
- [x] Tests.

## Verification (2026-11-26)
- Compile clean.
- `EnemyAi` category: 769/769 passed. `RealEntityCompositionTests`: 37/37 passed.
- New: `EnemyRebuildRolePolicyTests` (incl. every faction roster has a rebuild ship), `WipedStationlessFleet_RebuildsFromRoleShipsBelowTheTierFloor`, `RefundedStationlessCapital_PausesCapitalPurchases`, `ReserveShips_LandIntoLiveCapacity_OldestWaitingShipFirst`, `PurchaseCapacityLimit_IsTheFleetCapPlusTheReserve`, `DeployedCapacity_CountsLandedUnitsOnly_AndFallsWhenTheyAreLost`.
- Updated (contract now "purchase capacity" = cap + reserve): `UltraHardRebuild_BuysTheBestShipItCanPayNowAndRechecksLosses`, `ChoiceThatDoesNotFit_BuysTheFittingShipOnlyWhenMoneyCoversBoth`.
- Not run: full suite, Play Mode, in-game battle.

## Acceptance (user)
- Skirmish: a wiped/stationless AI rebuilds with cruisers/heavy cruisers (telemetry `counter-selection ... rebuild=True`).
- Late game: AI bank stops piling up; reserve ships land as the fleet takes losses; Empire AI eventually fields an Executor.

## Known limits
- `GetReinforcementBudget` still counts only free fleet capacity (reserve not credited).
- One held ship per type (`HasWaitingBuild`) still limits how fast the reserve fills.
- Reusable guidance: none beyond this plan; the rules live in code comments (W56, W46 references).

## TODO
- Hit-and-run tactics plan (separate task, user 2026-11-26).
