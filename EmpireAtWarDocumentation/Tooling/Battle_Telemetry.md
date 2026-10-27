---
type: technical-reference
system:
  - tooling
  - balance
  - ai
verified: 2026-10-27
verification: source-and-battle-logs
---
# Battle Telemetry

## Goal

- Explain real battle results the Battle Simulator cannot: actual damage per unit/weapon, losses, AI decisions and purchases.

## Rules

- Editor and `DEVELOPMENT_BUILD` only; compiled out of release builds.
- Recording runs only in Play Mode; EditMode tests drive AI code without writing files.
- One battle → two CSVs with the same timestamp; a new pair starts on every Single scene load.
- Files are rewritten/appended every `10 s` and on quit; stop Play Mode before reading the last seconds.
- One battle is one sample: fleet composition and player habits vary. Compare several battles before retuning.

## Files

- Output folder: `C:/Users/<user>/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleTelemetry/`
- `<scene>_<yyyyMMdd_HHmmss>.csv` — damage telemetry (`BattleTelemetry`).
- `<scene>_<yyyyMMdd_HHmmss>_ai.csv` — AI decision/purchase timeline (`EnemyAiTelemetry`).
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

- `kind=decision` — AI state changed; `kind=sample` — same state, written every `5 s`.
- Decision columns: `state, committed, ownShips, body, rallying, enemyShips, fleetAdvantage, localAdvantage, localThreat, baseThreat, reason`.
- `fleetAdvantage` / `localAdvantage`: AI strength ÷ known hostile strength (1 = even, Lanchester). `100` = no hostiles known.
- `body` = ships given the offensive order; `rallying` = committed ships still guarding the anchor (see `FleetCohesion`).
- `kind=purchase` — `state, category, counter (1 = counter-production pick), unit, price, money`.

## Implementation

1. Play a battle; stop Play Mode.
2. Open the newest pair in the folder above.
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
