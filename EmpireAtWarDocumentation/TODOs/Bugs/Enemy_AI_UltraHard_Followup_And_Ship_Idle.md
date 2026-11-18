---
category: Bugs
status: in-progress
created: 2026-11-18
parent: "[[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]"
follows: "[[Done/Bugs/Enemy_AI_UltraHard_Collapse_And_Attack_Order_Loss]]"
---
# Enemy AI UltraHard Follow-up And Ship Idle

## Goal
- Fix defects confirmed in the 2026-11-18 22:12 UltraHard battle (code `d245b2d59`), including two regressions from [[Done/Bugs/Enemy_AI_UltraHard_Collapse_And_Attack_Order_Loss]].
- Fix the end-game battle-report crash and player ships idling after a kill.
- Plan only. Every item is verified against source and logs; no playtest steps added.

## Evidence
- Battle: Small Coruscant, seed `-668316619`, AI `UltraHard` (115 `Editor.log` lines), Republic win.
- Logs: `BattleTelemetry/Loading_20261118_221247_ai.csv`, `_ai_orders.csv`, `Battle_20261118_222402.csv`, `Battle_20261118_221251_map.csv`, autosave at 600 s.
- Player0 = human Republic. Player1 = AI Separatist. AI main station entity `2`, second station `185`.
- 21:34 battle was `Medium` (lobby default `AiMedium`, `SkirmishModel.cs:25`); excluded from comparison.

### UltraHard comparison (different seeds/maps)
| Metric | 20:43 (`a001966b7`) | 22:12 (`d245b2d59`) |
|---|---|---|
| AI peak ships | 9 at 270 s | 14 at 257 s |
| AI fleet ratio 220–280 s | < 1 | 1.2–1.7 (AI stronger) |
| ~300 s own/enemy ships | 9 / 11 | 9 / 13 |
| Last AI ship alive | 614 s | 749 s |
| `zone:0` as objective | 4 | 0 |
| `deploymentBlocked` | 0 | 447 |
| Spend: Ship / Level / Research / Mining / Defense | 108.6k / 14.4k / 16.3k / 6k / 5k | 140.2k / 19.9k / 16.3k / 11k / 7k |
| Sites at 300 s (20:43) / 600 s (22:12) | human 7 mines + 3 relays | human 16 sites + 3 relays, AI 1 empty site; credits 62,370 vs 3,101 |

## Verdict On External Analysis (22:12)
- Not authoritative. Checked line by line against source and logs.
- **Confirmed** — `zone:0` never an objective (0 rows); relay/mine value in `GetCaptureScore` applies only when `IsMainForce` (≥ `MAIN_FORCE_SHIP_COUNT = 6`); otherwise squared distance.
- **Confirmed** — owned satellite pads count as threatened (`EnemySiteObjectives.IsThreatened` has no kind filter) and threatened sites rank first. **Partial**: only 2 `threatenedSite` preemptions logged (65.0 s `site:0→site:4`, 539.5 s `site:1→site:19`); "130 s on pads" and "52.6 % progress" not verified.
- **Confirmed** — main station died with `stationPressure=False` (420→480 s: hull 159,305 → 27,426, shields 0) while the fleet raided. Code: `FindOwnBase` = station closest to fleet center (`EnemyStrategicContextBuilder.cs:1270`); `EnemyStationPressureTracker.Update` resets on station-id change. Which id was passed is not logged.
- **Confirmed** — 7 finished ships held 607.7–713.2 s, then spawned in the west default home zone at 713–714 s.
- **Confirmed, mechanism corrected** — ships starved by spending (Level 4 at 5,814.6/6,000 for a Providence; Patrol Frigate waited 24–91 s). Cause is UltraHard priority order, not a failed-purchase fallthrough: `EvaluateUltraHard` picks Mining → income Research → Level → combat Research → Defense → Ship every tick. Satellites are a second spender (`EnemySiteConstructionController`).
- **Confirmed** — critical-pressure squadron cap lifted (`hasSquadronRoom = IsStationCritical || …`, `EnemyProductionStrategy.cs:190`). Critical buys: 22 Hyena (8.8k) + 29 Droid (8.7k).
- **Confirmed** — 521.14 s: 13 of 14 player ships idle after killing `99`; only ship 171 (715 radar) saw AI ship 212 at 684.3.
- **Confirmed** — `AsteroidMiner` crash.
- **Declined** — invented symbols in the earlier 21:34 analysis (`StrategicGoal`, `TryBuyStationUpgrade`, `ShouldRetreat`, `CanSpawnAt`, `ClearOrder()`, `SavedTarget`, `EnemyRecoveryCoordinator`).
- **Declined** — "Move order turns off AI weapons": weapons keep base targets; the ship stops chasing/facing.

## Fixes

### P0 — End-game crash
- `EndGameLossCatalog.Describe` (`Entities/EndGameUi/EndGameLossCatalog.cs:51-56`) reads purchase catalogs.
- `MiningFacilityCatalog.asset` keys: 0, 3 → missing `AsteroidMiner` (2), `BattleMiner` (1).
- `DefendPlatformCatalog.asset` keys: 0, 2–8 → missing `BattleAsteroid` (1).
- [x] Give site-built types (`AsteroidMiner`, `BattleAsteroid`) a name/icon source for the loss report (site facility data, not purchase catalogs). `EndGameLossCatalog` now takes `CaptureSiteData`; `BattleMiner` → `CommonMiner` entry.
- [x] Every enum value recorded by `BattleLossTracker` must resolve; coverage test `EndGameLossCatalogTests` (`Ui`).
- Tests: `Ui`, `GameFlow` (`BattleVictoryServiceTests`). Not run.

### P0 — Finished ships held forever (regression)
- `EnemyBuildQueue.RetryShipDeployment` → `WaitForDeployment(float.PositiveInfinity)` (`EnemyBuildQueue.cs:44`).
- `ShipSpawnPoints.TryGetDefenseSpawnPosition` rejects any point within remembered hostile range + `HOSTILE_MARGIN = 100` (`EnemyRecoveryAnchor.cs:144-150`); a fleet at the station blocks all.
- No rally → `TryGetDefaultZoneSpawnPosition` = original home zone, even when the only living station is elsewhere.
- Log: 386–425 s (3 ships), 607.7–713.2 s (7 ships, ~16.6k credits).
- [x] Bounded hold: `MAX_DEPLOYMENT_HOLD_SECONDS = 8` in `EnemyFactionController`, then spawn ignoring hostile range (`TryGetSpawnPositionNear` → random); event `deploymentForced`. Best point: `TryGetBestDefenseSpawnPosition` (farthest from threat centroid) used by the forced path.
- [x] Try every living owned station before giving up. `IShipSpawnPoints.TryGetStationSpawnPosition` tries each living station, nearest the home zone first.
- [x] Without a rally, spawn at the nearest living owned station, not the fixed home zone. Station spawn runs before the home-zone fallback; the forced path also tries stations.
- Tests: `EnemyAi` (`EnemyFactionControllerLifecycleTests`), `Ships` (`ShipSpawnPointsTests`). Not run.

### P0 — Player ships idle after a kill
- `ShipOrderRunner.CompleteOrder` (`ShipOrderRunner.cs:394-410`) → `ShipEngagement.FindNearestEnemy(_radar, …)` scans only this ship's `radar.Enemies` (500–750); on the kill frame that list held only the dead target.
- `IdleState` engages only enemies already in weapon reach (`inRangeOnly: true`) and never moves.
- Log: 65 "no replacement order" events; 521.14 s → 13 of 14 ships idle.
- [x] (open) Replacement search: own radar, then visible hostiles known to the owner within a bounded radius of the dead target (team vision), preferring the previous target class. `ITeamContacts`/`TeamContactService` (0.5 s cache per viewer, radius 1,500 around the dead target).
- [x] Group attack: ships ordered together share the replacement; one ship's contact serves the group. `GroupPace.SharedReplacement`, used within 3,000 range.
- [x] Nothing found → attack-move to the dead target's last position (`AttackTargetState.LastTargetPosition`) instead of Idle.
- [x] Idle ships attacked by a visible hostile inside radar turn and engage it (bounded pursuit) instead of only facing in-range targets. Hit within 3 s + radar contact → Attack with a 1,500 leash, then Move back.
- Tests: `Ships`, `BattleInput`. Not run.

### P1 — AI order churn
- Raid/attack execution recomputes the anchor every decision (`EnemyTaskForceExecutor.SplitBody` → `FleetCohesion.FindAnchor`, then the highest-`TargetDps` ship).
- `GuardSupport`/`RallyStragglers` re-issue Guard whenever the anchor changes; `ready` toggles Attack ↔ Guard; a new strike ship rebuilds `BattleOffsets` → re-issues Attack to all strike ships.
- Log: 85 order replacements in 49 s (246–295 s) for ~13 ships. Each `Issue` resets approach and weapon targets.
- [x] Sticky anchor: keep it while alive and inside the body; switch only by a margin. `FleetCohesion.FindAnchor(…, heldIndex)` margin 2; DPS anchor switch factor 1.25.
- [x] Hysteresis on `ready` and on strike/screen roles. ready: start 3/4, keep 2/4 of body.
- [x] Adding a ship must not rebuild slots for ships already engaged. `BattleOffsets` slots only new ships.
- Tests: `EnemyAi` (`EnemyUnitCommanderTests`). Not run.

### P1 — Station loss not seen while a second station exists
- `FindOwnBase(origin)` picks the station closest to the fleet center; `EnemyStationPressureTracker` resets on id change.
- [x] Track pressure per owned station; station selection = most threatened/damaged living station, not nearest. `FindOwnBase` keeps the pressured station, else most damaged (<98 %), else nearest.
- Tests: `EnemyAi`. Not run.

### P1 — Recovery retreat overrides station defense
- `EnemyStrategicDecisionModel.Evaluate:303-314`: in recovery mode with low local advantage, state becomes `RetreatValue`, replacing `DefendBase`.
- Log: anchor relay at (1265.8, 1535.1), ~2,400 units from the attacked station; 8 DefendBase↔RetreatValue flips 328–394 s.
- [x] Station pressure wins over the recovery override; retreat target = the threatened station, not a far relay. recovery override skipped while a living station is under pressure and state is `DefendBase`.
- Tests: `EnemyAi` (`EnemyDecisionStabilityTests`). Not run.

### P1 — Raid against a mine under construction
- 247–294 s: 9 strike ships on mine `101`; its hull rose 292 → 3,700 (construction finished); ships started 1,200–1,700 units away and arrived with the human fleet.
- [x] Raid target choice weights travel time and nearby hostile fleet arrival, not only "exposed income". contested targets ×4 score.
- Tests: `EnemyAi`. Not run.

### P1 — Capture scoring and satellite pads
- [x] Apply relay/mine value in `GetCaptureScore` to all groups, not only `IsMainForce`.
- [x] Owned satellite pads are not threatened objectives for ship groups (squadron assignments may keep them).
- [x] Capture stall: `stalled` → AttackMove to circle edge (0.9 radius); Move-lock removed.
- Tests: `EnemyAi`, `Capture`. Not run.

### P1 — Threat model ignores defenses
- AI intel records only entities with `IUnitTypeFacade` (ships, squadrons) (`AiWorldSnapshot.cs:228`); satellites, battle asteroids, stations are invisible to safety checks.
- [x] Add static defenses with their weapon range to objective/route threat. `IsInsideStaticDefense` in `IsRouteSafe` (visible armed structures).
- Tests: `EnemyAi`. Not run.

### P2 — Production priority on UltraHard
- `EvaluateUltraHard` puts Ship last; Level 4 bought at 440.7 s at fleet ratio 0.47.
- [x] Ship reservation: when fleet ratio < 1 or ships < main-force size, save for the selected ship; levels/research/defense wait. snapshot `NeedsFleet` (ships < 6 or advantage < 1).
- [x] Share the reservation with `EnemySiteConstructionController` (satellites spend the same wallet). satellites need 6 ships.
- [x] Critical pressure: cap squadron purchases instead of lifting the cap. cap ×2 + 2.
- Tests: `EnemyAi` (`EnemyProductionDecisionModelTests`). Not run; no stat-pinning asserts.

### P2 — Missing expansion behaviour
- AI builds satellites only for threatened points or mining income; it never uses a satellite on a held relay to push to nearby mines (the human's winning pattern).
- [x] Add a satellite-led expansion option after capturing a relay/site. Decided 2026-11-18: satellites are the expansion; see Satellite-first below.
- [x] Log `EnemySiteConstructionController` attempts and failures (currently no telemetry). event `siteBuild`.
- Tests: `EnemyAi`, `Capture`. Not run.

### P2 — Lobby difficulty
- AI slot defaults to `AiMedium` and is not remembered.
- [x] Persist the last AI difficulty per slot. `LobbySettingsData` + `ISettingsService.SetLobbyOccupant`; offline lobby only.
- Tests: `Ui`. Not run.

## Order
1. P0 crash, deploy hold, ship idle.
2. P1 order churn, station pressure, recovery override.
3. P1 capture/raid/threat model.
4. P2 production, expansion, lobby.

## Files
- `Entities/EndGameUi/EndGameLossCatalog.cs`, `Services/BattleService/BattleLossTracker.cs`, `Settings/Data/Factions/Shared/MiningFacilityCatalog.asset`, `DefendPlatformCatalog.asset`
- `Entities/EnemyFaction/Models/EnemyBuildQueue.cs`, `Entities/EnemyFaction/Controllers/EnemyFactionController.cs`, `Services/ShipSpawning/ShipSpawnPoints.cs`, `Services/Enemy/EnemyRecoveryAnchor.cs`
- `Entities/Ship/Orders/ShipOrderRunner.cs`, `Entities/Ship/StateMachine/ShipEngagement.cs`, `IdleState.cs`
- `Services/Enemy/EnemyTaskForceExecutor.cs`, `EnemyStrategicContextBuilder.cs`, `EnemyStationPressureTracker.cs`, `EnemySiteObjectives.cs`, `AiWorldSnapshot.cs`, `EnemyProductionStrategy.cs`, `EnemySiteConstructionController.cs`
- `Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs`, `EnemyProductionDecisionModel.cs`
- `Entities/MainMenu/Skirmish/SkirmishModel.cs`

## Progress 2026-11-18
- Done: all items checked above. Not done: online lobby difficulty persistence.
- Tests added/updated: `EndGameLossCatalogTests`, `FleetCohesionTests`, `EnemyProductionDecisionModelTests`, `EnemyFactionControllerLifecycleTests`, `LobbySettingsDataTests`. Not run (local override).

## Satellite-first (decided 2026-11-18)
- Rules from the user: satellites on a relay/centre zone are the way to take it (3 turbolasers; no level-1 ship breaks them); one at every mining point is mandatory; satellites extend vision and are strong early.
- Type: turbolaser by default; quad laser when hostile squadrons are ≥ 40 % of durability; missile or mass driver (best fit) once money ≥ 1.5× its cost and the point has ≥ 1 satellite.
- `EnemyDefenseStrategy.TrySelect` = `TrySelectMandatory` (zones first, ignores production reserve) then `TrySelectScored`.
- `EnemyDefensePad.IsZonePad` = point without mine sites. `EnemySiteObjectives`: pads of mine-less zones are capturable and carry priority bonus 600.
- `EnemySiteConstructionController`: no fleet gate; a mine build no longer ends the tick.
- Pad capture is by presence in the capture range: the capture force enters at 0.7 × radius on its arrival side instead of the centre (`approachPoint` in `ExecuteCapture`).
- Starting-station pad: no explicit late-build rule; cost ordering is accepted (user, 2026-11-18).

## Open Decisions
- Deploy hold length before forced spawn.
- Team-vision radius for replacement targets.
