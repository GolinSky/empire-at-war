---
type: technical-reference
system:
  - tooling
  - balance
  - ai
verified: 2026-10-31
verification: Historical battle evidence plus 2026-10-31 live-source review and
  Unity compilation; new telemetry not runtime-tested
---
# Battle Telemetry

## Goal

- Explain real battle results the Battle Simulator cannot: actual damage per unit/weapon, losses, AI decisions and purchases.

## Rules

- Editor and `DEVELOPMENT_BUILD` only; compiled out of release builds.
- Recording runs only in Play Mode; EditMode tests drive AI code without writing files.
- One battle → damage, AI decision and ship-order CSVs; each writer resets on every Single scene load. Match scene and nearby filename timestamps.
- Files flush every `10 s` and on quit; AI decisions and ship orders also flush when Editor Play Mode exits.
- One battle is one sample: fleet composition and player habits vary. Compare several battles before retuning.

## Files

- Output folder: `C:/Users/<user>/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleTelemetry/`
- `<scene>_<yyyyMMdd_HHmmss>.csv` — damage telemetry (`BattleTelemetry`).
- `<scene>_<yyyyMMdd_HHmmss>_ai.csv` — AI decision/purchase timeline (`EnemyAiTelemetry`).
- `<scene>_<yyyyMMdd_HHmmss>_ai_orders.csv` — per-ship order/contact timeline (`ShipOrderTelemetry`).
- `Assets/Scripts/Services/Enemy/ShipOrderTelemetry.cs`; `ShipOrderTelemetrySource.cs` — runner lifecycle hooks, stable injected identity and live radar source.
- `Assets/Scripts/Components/Weapon/BattleTelemetry.cs` — writer; hooks in `WeaponComponent.OnShotEmitted` / `CommitImpact`.
- `Assets/Scripts/Services/Enemy/EnemyAiTelemetry.cs` — writer; hooks in `EnemyUnitCommander.PublishDecision`, `EnemyProductionStrategy` purchase.

## Damage CSV (`<scene>_<time>.csv`)

- First line: `# battle seconds (unscaled),<n>`.
- `shots` rows: `faction, unit, shots, activeSeconds` (first → last shot of that unit type).
- `impact` rows: `faction, unit, damageType, targetFaction, target, impacts, fleetCommandImpacts, rawDamage, shieldLoss, hullLoss, effective/raw, kills`.
- `unit` = attacker prefab name (`MunificentShipView`); `target` = `UnitTypeId` (`Munificent`) or `Structure` (platforms, stations).
- `effective/raw` < 1 → matrix/accuracy/Fleet Command mitigation; > 1 → shield-piercing or hardpoint hull multiplier.
- `fleetCommandImpacts` = hits fired while the attacker had Resolute/Home One Fleet Command.
- Local player station appears as `Player0_SpaceStation`; the AI's as `Player1_SpaceStation` → identifies which faction the AI played.

## AI CSV (`<scene>_<time>_ai.csv`)

- `kind=decision` — AI state or objective/defense-target/retreat-anchor changed; `kind=sample` — same state, written every `5 s`.
- Decision columns: `state, committed, ownShips, body, rallying, enemyShips, fleetAdvantage, localAdvantage, localThreat, baseThreat, reason`.
- `fleetAdvantage` / `localAdvantage`: AI strength ÷ known hostile strength (1 = even, Lanchester). `100` = no hostiles known.
- `body` = ships given the offensive order; `rallying` = committed ships still guarding the anchor (see `FleetCohesion`).
- `kind=purchase` — `state, category, counter (1 = counter-production pick), unit, price, money`.

- `time` uses Unity `Time.time`: scaled seconds since Play startup, not saved `battleTime`. Save clock and damage totals have separate origins.
- Decision `reason` adds objective ID/type/owner, destination, progress, regroup state, recovery seconds, pressure source, defense target, issued-action reason and candidate rejection/retention reason.
- Bounded decision samples include each own ship's stable ID, current order and planar distance to the strategic destination. Join IDs to the order CSV.

## AI order CSV (`<scene>_<time>_ai_orders.csv`)

- Lifecycle rows: start, issued, cancel, complete, release, ability approach/facing/resume/complete and waypoint advance; periodic sample every `5 s` per ship.
- Explicit clocks: `timeScaledSinceStartup` / `timeUnscaledSinceStartup` = Unity Play session clocks, not save battle clock.
- Identity/state: player, entity ID, order, state, target ID, requested destination XZ, current XZ, moving flag.
- Contacts: count, preferred contact ID/distance/in-range, first live weapon-tracked contact ID. A candidate alone does not prove actual targeting.
- Effective live radar/weapon range, vision multiplier, ion/cloak state, facing dot, hull/shields and lifecycle reason.
- Join decision assignment and order rows by stable entity ID; distinguish approaching, fighting en route, stopped outside, inside capturing, stalled and idle.
- Historical Providence `5065384297670371146` has only saved Idle/no-target evidence; before/after damage/facing attribution requires a new recording.

## Implementation

1. Play a battle; stop Play Mode.
2. Open the matching scene's latest damage, AI and ship-order files.
3. Damage: sum `shieldLoss + hullLoss` per faction/unit; divide by battle seconds for rates; `kills × price` for value lost.
4. Squadron health check: `shots ÷ squadrons lost` per side (player ≈ 200–280 is healthy; AI was 60–140).
5. AI: time per state (`decision` row gaps), count `CaptureZone ↔ RetreatValue` flips, `rallying` share, purchase mix.
6. Exchange ratio = AI ship value lost ÷ player ship value lost; exclude squadrons (hangar launches are free).

## Edge Cases

- Squadrons in "value lost" inflate totals: hangar-launched squadrons cost nothing.
- Beam/DoT weapons log one impact per damage tick.
- Hull loss on hardpoint hits uses the hardpoint's `hullDamageMultiplier`.
- `_ai.csv` with only a header/one row = a menu or loading scene; skip it.

## Related

- [[Tooling/GameDesign_Tools]] — Balance Editor, Battle Simulator.
- [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]] — findings these logs produced.

- [[Architecture/Enemy_AI_Objectives]] — defense/recovery/capture contracts.
- [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]] — implementation and pending acceptance.
