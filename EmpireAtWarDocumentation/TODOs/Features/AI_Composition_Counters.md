---
category: Features
status: in-progress
created: 2026-10-07
---
# AI Composition Analysis and Counter Measures

## Goal
- AI judges forces by composition and damage matchups, not by ship counts.
- Counter production and squadron roles come from `DamageMatrixData` + `WeaponsData` + unit stats, not hand-written if/else.
- Must stay sane while balance (HP/shields/DPS) is unstable: only ratios are compared; stats are read live from data.

## Decision
- Weapon layout (prefab hardpoints) is baked into `ShipData` / `SquadronData` by `Tools/AI/Bake Weapon Loadouts`. Hull, shields, class, DPS, damage matrix are read live → balance edits need no re-bake.
  - Why: weapon hardpoints live only on view prefabs; loading every prefab at runtime is too heavy.
  - Staleness guard: EditMode test re-derives loadouts and compares.
- Strength = Lanchester-style kill times per `ShipClass`: `T(attacker→defender) = Σ_c shields_c/shieldDps_c + hull_c/hullDps_c`.
- `Advantage = sqrt(T(hostile→own) / T(own→hostile))`, clamped `[0.01, 100]`; equal forces → `1`, double force → `≈2`.
- Strategic state = highest-scoring rule (utility) + hysteresis; thresholds come from `EnemyAiDifficultyProfile`.
- Production: candidate score = `(ln Adv(own+unit) − ln Adv(own)) / price`; fallback to old selection when no gain/no hostiles.
- Squadron roles: escort own ships when squadron kills hostile strikecraft faster than hostile ships.

## Implementation
- [x] Bake: `WeaponLoadoutEntry`, `ShipData.WeaponLoadout`, `SquadronData.WeaponLoadout/MemberCount`, `Editor/AI/WeaponLoadoutBaker` (`Tools/AI/Bake Weapon Loadouts`). Baked 36 ShipData + 18 SquadronData.
- [x] Pure models (`Entities/EnemyFaction/Models/Combat`): `UnitCombatProfile`, `ForceComposition`, `CombatMatchup`, `EnemyCounterProductionModel`, `ProductionCandidate`.
- [x] Services (`Services/Enemy`): `UnitCombatProfileFactory`, `UnitCombatProfileCatalog`, `ForceCompositionBuilder`; bound per AI in `AiPlayerInstaller`.
- [x] Snapshot: `FleetAdvantage`, `BaseThreatRatio` replace `EnemyShipsNearOwnBase` / outnumbered count.
- [x] Utility rules: `EnemyStrategicRule` table + `ResponseCurve` (±25% soft ramp) + hysteresis `0.05` in `EnemyStrategicDecisionModel`.
- [x] Profile: `OutnumberedRetreatCount` → `RetreatAdvantage` (0.8/0.7/0.6/0.5) + `HuntAdvantage` (1.1/1.0/0.9/0.8).
- [x] Counter production in `EnemyProductionStrategy` (ships + squadrons compete; old selection = fallback).
- [x] Squadron escort role in `EnemySquadronCommander` (`Command(squadron, squadronType)`).
- [x] Tests: `CombatMatchupTests` 12/12, `WeaponLoadoutBakeTests` 3/3, AI suite (`Enemy` filter) 104/104. Full EditMode 1151/1155; 4 failures unrelated (ShipEngineHardpoint NRE in `ShipMoveComponent`, AcclamatorAssault team-color/helper-mesh prefab WIP).
- [ ] Skirmish Play Mode acceptance (not run).

## Fix 2026-10-07: squadron spam, no level-ups, no captures
- Cause: counter path skipped the `1 squadron / SHIPS_PER_SQUADRON (2) ships` cap → cheap squadrons always won `gain / price`.
- Cause: level-up was leftover spending; always-affordable cheap units drained money. UltraHard due rule fell through to ships when level unaffordable.
- Cause: every anti-strikecraft squadron escorted while any hostile strikecraft existed → none captured relays/sites.
- Fix: `HasSquadronRoom(shipCount)` gates squadron counter candidates (with `NeedsMinimumFleet`).
- Fix: `EnemyAiDifficultyProfile.ShipOrdersPerLevel` (3/2/2/1). Level due when `ShipsOrdered ≥ level × k` → buy or save (`None`); combat research and ships wait. `DefendBase` exempt.
- Fix: escorts assigned best-first only until `Advantage(escorts, hostileStrikecraft) ≥ 1`; only ships hostile strikecraft can damage are guarded.
- Commit `4cc45a1f`.

## Fix 2026-10-07 (2v2 log review)
- Empire has no ship at station level 1 (earliest `VictoryI` L2). UltraHard zero-fleet branch waited for a ship or first miner; neutral relays (`RelaySpawnBlockRadius` 900) can block every placement ring → idle at L1 with 54,776 credits.
- Fix: zero-fleet branch only when `HasShipOption`; `ShouldSaveForLevel` also true when no ship is buyable → level up first.
- 2v2: `FleetAdvantage` compared own fleet vs whole enemy team → constant `RetreatValue` (0.05–0.3), 0 relays held. Fix: own team (`IsAllied`) vs focus team. `BaseThreatRatio` still uses own force.
- Logs now include `Player=<id>:<faction>`.

## Fix 2026-10-07 (user rules: level up / fighters, scouting, expand)
- No ship type orderable (`EnemyProductionSnapshot.CanOrderShips`) → level up at once; if unaffordable, buy squadrons (cap lifted) instead of saving/idling. All difficulties.
- Structure placement: station rings → captured relays → map-wide rings from station (spacing 48), nearest visible open spot. Old "no map-wide fallback" test replaced.
- `IEnemyStructurePlacementService.TryGetScoutTarget`: nearest fogged, unblocked spot. Production buys one cheapest squadron when mining is needed, nothing is placeable and no squadron is owned; squadron commander sends the closest non-escort squadron there.
- Retreat uses `LocalAdvantage` (own fleet vs hostiles within 1000 of fleet center = 2× common weapon range 500); far-away enemy armies no longer cause retreat.
- Expansion first: Capture (any target) 0.45; fleet-objective Hunt 0.5 needs `RequiredAttackRatio`; generic Hunt 0.3 (`HuntAdvantage`) when nothing is left to capture.
- Tests: `EnemyProductionDecisionModelTests` 47/47; `Enemy` filter otherwise green except pre-existing `UltraHardRebuild…(5000,Recusant)`.
- Fix: scouting skipped until a squadron exists (map layout loads async → `MapModel.GetStationPosition` NRE). Commit `84ca1dfd`.

## Fix 2026-10-07 (fighter lasers, variety, matchup data, hangar cap, fog intel)
- `DamageType.FighterLaser = 13`: weapons 15/21/24/29/46 (fighter-only) moved off `Laser`. Matrix row: ×1.5 vs fighter/bomber/interceptor; corvette 0.15, frigate 0.08, cruiser 0.05, capital 0.03, heavyCapital 0.02, structure 0.03; Laser accuracy; vsShield 0.5. Squadron laser-shield multiplier applies to both. Vulture: 373 dps vs fighters, 9.3 vs capitals.
- Production score = gain / price × `IShipClassMatchups.GetPreference` / (1 + owned of type).
- `Assets/Settings/Data/AI/ShipClassMatchupData.asset` (Addressables `Model`, key `ShipClassMatchupData`): strong/weak per class; preference = (1 + 0.5 × strongShare) × (1 − 0.5 × weakShare) of hostile hull+shields.
- Squadron cap counts carrier hangars (`maxActive` × reserved carriers); carrier hangars valued only while squadron room remains.
- Real data, Separatist L5 vs 2 Venator + Arquitens + Acclamator, 10 picks: Recusant, Lucrehulk, PatrolFrigate, Dispatcher, Providence, Captor, Munificent, Lucrehulk, PatrolFrigate, Recusant.
- Fog: AI sees hostiles only via `TeamIntelRegistry` (scene, per team) → `HostileIntelModel`. `HostileIntelGatherer` (per AI, 1 s) reports team-visible hostiles; allies share records; newer report wins.
- Fuzzy `IntelConfidence`: Fresh ≤20 s → 0 at 90 s; Stale peaks 90 s → 0 at 240 s; confidence = max(fresh, 0.5 × stale); forgotten at 0. Forces weighted by confidence.
- Scouting priority: build space → stale record with max (hull+shields)×(1−confidence) → nearest hostile base when intel empty.
- Known gap: units destroyed out of sight are removed from intel on death (`EntityRemoved`); station positions are treated as map knowledge.
- Commit `44faadd6`.
- Tests: full EditMode 1247/1252; 5 failures pre-existing (ShipEngineHardpoint ×2, TeamColorViewPrefab, UnitHelperMesh, UltraHardRebuild Recusant).
- TODO: 2v2 skirmish acceptance; watch map-wide placement scan cost (runs each production check and every 3 s in squadron commander).
- Tests: `Enemy` filter 124/125 (same pre-existing `UltraHardRebuild…(5000,Recusant)` failure).
- Tests: `EnemyProduction` 50/51; failing `UltraHardRebuild_SavesForPriorityShipAndRechecksLosses(5000,Recusant)` also fails on HEAD without these changes.

## Rules Table (weight × consideration)
| Weight | State | Consideration |
|---|---|---|
| 1.0 | RebuildFleet | no own ships |
| 0.9 | DefendBase | own base × `BaseThreatRatio ≥ DefenseThreatRatio` |
| 0.8 | RetreatValue | own base × enemy ships × `FleetAdvantage ≤ RetreatAdvantage` |
| 0.7 | CaptureZone | threatened own site |
| 0.6 | CaptureZone | zones < `MinimumControlledZones` |
| 0.5 | AssaultBase | base objective × `FleetAdvantage ≥ RequiredAttackRatio` |
| 0.5 | HuntFleet | fleet objective × `FleetAdvantage ≥ HuntAdvantage` |
| 0.4 | CaptureZone | any capture target |
| 0.3 | HuntFleet | base objective × `FleetAdvantage ≥ HuntAdvantage` |
| 0.2 | AssaultBase | base objective, no enemy ships |
| 0.05 | Hold | always |

## Real-Data Check (2026-10-07)
- 2× Venator vs 8× TIE Bomber squadrons → advantage `0.37` (bombers pierce shields).
- Adding one unit: Venator `0.56`, Acclamator `0.88`, Delta-7 `1.03`, A-Wing `1.11`, Arquitens `1.30`, V-19 `1.35`, CR90 `1.36`, V-Wing `1.87`.

## Edge Cases
- No hostiles → `Advantage = 100`, production falls back to old priority.
- Zero DPS vs a class → that class is skipped and kill time is stretched by `totalDurability / killableDurability`; fully unkillable → `1e6 s`.
- Balance or damage-matrix edits need no re-bake; adding/removing weapon hardpoints on a view prefab does → `WeaponLoadoutBakeTests` fails until re-baked.
- Ignored by the rating: shield regen, abilities, research/level modifiers, squadron `LaserShieldDamageMultiplier`, weapon range/arcs.
- Production score is `gain / price` (population cap not weighted).
- Live units scaled by current hull/shields and alive hardpoint fraction.
- Pending (reserved − alive) purchases count toward own force so the AI does not re-buy the same counter.
- Structures (bases, platforms) excluded from fleet strength.

## TODO
- Fog of war: AI still sees all hostiles (unchanged behavior).
