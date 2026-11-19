---
category: Bugs
status: blocked
created: 2026-11-18
parent: "[[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]"
follows: "[[Done/Bugs/Enemy_AI_UltraHard_Collapse_And_Attack_Order_Loss]]"
---
# Enemy AI UltraHard Follow-up And Ship Idle

## Blocked 2026-11-19
- Battle 2026-11-19 00:49 (`c43df90dc`): human won in ~9 min; AI idle/retreating, never took the top-left mining zone, built a second station near home, ships fire stern-first, aggro-when-shot added. AI ran on **Medium**, so UltraHard-only fixes never applied.
- User verdict: good direction, poor execution. Work continues in [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]]. Do not continue this plan.

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
- [x] Replacement search: own radar, then visible hostiles known to the owner within a bounded radius of the dead target (team vision), preferring the previous target class. `ITeamContacts`/`TeamContactService` (0.5 s cache per viewer, radius 1,500 around the dead target).
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
- [x] Station pressure wins over the recovery override; retreat target = the threatened station, not a far relay. **Reverted in `1ae2c9236`**: skipping the override broke 3 `EnemyDecisionStabilityTests` recovery cases that require an outmatched fleet to withdraw. Redo without blocking withdrawal: keep the withdraw, retarget the recovery anchor to the pressured living station instead of the far relay. Those 3 cases must keep passing. Redone 2026-11-19: `EnemyRecoveryAnchor` adds a `pressuredStation` candidate (score 2) for the pressured living station and `FindRetreatAnchor` returns it unfiltered; the withdraw decision is unchanged.
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
- [x] Share the reservation with `EnemySiteConstructionController` (satellites spend the same wallet). Superseded by Satellite-first: the 6-ship gate was removed in `7c3dfd19b` and mandatory satellites ignore the reserve; only `TrySelectScored` still respects it.
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
- [x] Online lobby: persist the last AI difficulty per slot too. See Open Issues 7.
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
- Done: all items checked above. Not done: online lobby difficulty persistence. 2026-11-19: recovery-retreat item reverted; see Open Issues.
- Tests added/updated: `EndGameLossCatalogTests`, `FleetCohesionTests`, `EnemyProductionDecisionModelTests`, `EnemyFactionControllerLifecycleTests`, `LobbySettingsDataTests`. Not run (local override).

## Satellite-first (decided 2026-11-18)
- Rules from the user: satellites on a relay/centre zone are the way to take it (3 turbolasers; no level-1 ship breaks them); one at every mining point is mandatory; satellites extend vision and are strong early.
- Type: turbolaser by default; quad laser when hostile squadrons are ≥ 40 % of durability; missile or mass driver (best fit) once money ≥ 1.5× its cost and the point has ≥ 1 satellite.
- `EnemyDefenseStrategy.TrySelect` = `TrySelectMandatory` (zones first, ignores production reserve) then `TrySelectScored`.
- `EnemyDefensePad.IsZonePad` = point without mine sites. `EnemySiteObjectives`: pads of mine-less zones are capturable and carry priority bonus 600.
- `EnemySiteConstructionController`: no fleet gate; a mine build no longer ends the tick.
- Pad capture is by presence in the capture range: the capture force enters at 0.7 × radius on its arrival side instead of the centre (`approachPoint` in `ExecuteCapture`).
- Starting-station pad: no explicit late-build rule; cost ordering is accepted (user, 2026-11-18).

## Decided
- Deploy hold before forced spawn: `8 s`.
- Team-vision replacement radius: `1,500`; shared group contact range `3,000`.

## Verification 2026-11-19
- Commits: `5d95cc29f`, `7c3dfd19b`, `0241671e8`, `1ae2c9236`. Source-inspected; no tests run by the agent.
- User run (before `1ae2c9236`): 4,157 tests, 4,146 passed; the 7 failures were fixed in `1ae2c9236`.
- User run after `1ae2c9236`: 1 failure left (below).

## Open Issues (decided 2026-11-19)

### 1. Failing test — capture edge distance
- `EnemyUnitCommanderTests.CaptureZone_DistantCombatDiversionReturnsToObjective` (`:1401`): expected `0.9`, got `9`.
- Cause: test forgot the radius; code returns `radius (10) × CAPTURE_EDGE_FRACTION (0.9)`.
- [x] Fix the test: expect `10f * 0.9f`. Production code unchanged. Category `EnemyAi`. Done.

### 2. Recovery retreat to a far relay (reverted)
- [x] Keep the outmatched-fleet withdrawal; retarget the recovery anchor to the pressured living station, not the far relay. Done (see P1 recovery item).
- The 3 `EnemyDecisionStabilityTests` recovery cases must keep passing.

### 3. Opening and economy (user rules — follow, do not re-decide)
- Mining = economy. Satellites = protection for mining and the way to take/hold a zone. Ships alone die fast → no money, no defense, no economy.
- Opening order:
  1. Buy all 5 spawnable mining facilities (`MiningFacilityCatalog.asset`: `MaxCount 5`, `1,000` each).
  2. Groups split in parallel: one to the nearest neighbour zone, one to the central zone (4 mines + 6 pads), one to another zone (capture/patrol). No single "centre first" target.
  3. At each captured point: capture the close satellite pads, build satellites, build mining on the sites.
- Always: mining builds and satellites that protect mining or push/hold a zone outrank ships.

Evidence (22:12 log, `Loading_20261118_221247_ai.csv`):
- AI bought ships first: PatrolFrigate, C9979, Vulture, PatrolFrigate at 15.1–18.1 s. Cause: `NeedsMinimumFleet` (< 3 ships) runs before `EvaluateUltraHard` in `EnemyProductionDecisionModel`.
- AI miners at 19.1, 20.1, 21.2, 42.2 s; 5th only at 160.8 s. Cause: UltraHard target = `MinimumMiningFacilities 3` + ships / 2. Research/level bought at 22–32 s before the 4th miner.
- AI built 3 quad satellites on centre pads 7/9/8 at 66–72 s, then lost the centre.
- Human: no ship until 45.8 s; first ships to the centre (79.7 s) and neighbour zone 3 (86.1 s); human ships spawned at the centre from 194 s; full fleet hit the AI home at 317.6 s. Human purchases are not logged.

Actions:
- [x] `EnemyProductionDecisionModel`: Mining until 5 facilities comes before `NeedsMinimumFleet`, ships, levels and research. UltraHard rule placed before `NeedsMinimumFleet`; ship-first tests updated.
- [x] UltraHard `MiningFacilityTarget` = catalog `MaxCount` (5) from the start, not `3 + ships / 2` (`EnemyProductionStrategy.cs:248`). `Math.Max(mining.MaxCount, floor)` when a mining option exists.
- [x] Opening capture (see R3): groups go to the nearest neighbour zone and the centre in parallel; the next group takes another zone or patrols. `EnemyShipTaskGroupPlanner`: up to 2 capture detachments in parallel until a main force exists (one ship each, at least one ship stays behind); reservations keep them on different objectives.
- [x] Site construction at a captured point: close pads → satellites → mining. Required satellites and mining builds stay exempt from the ship reservation (`TrySelectMandatory`, mine build in `EnemySiteConstructionController.Tick` — keep). `EnemySiteConstructionController.Tick`: mandatory satellites → mine → scored satellites.
- [x] Ship reservation (`NeedsFleet`) holds back only levels, research and scored (optional) defense. `NeedsFleet` now sits above income research too; mandatory satellites and mining are outside it.
- Tests: `EnemyAi` (`EnemyProductionDecisionModelTests`); no stat-pinning asserts.

### 4. Pursuit does not return to its post
- `ShipOrderRunner.UpdatePursuit`: attacker dies, no replacement → attack-move to the dead target's spot; ship stays there.
- [x] Before pursuit, save the previous order and position (`_pursuitOrigin` + prior order). Pursuit starts only from Idle, so the saved state is the origin position.
- [x] Pursuit ends (kill with no replacement, leash, lost target) → restore the previous order, else Move back to the saved position. Kill without replacement and a chase that ends on its own both `MoveTo(_pursuitOrigin)`; an outside order wins.

### 5. Replacement prefers the previous target class
- [x] `FindReplacement`: prefer a candidate of the dead target's class (own radar, shared group contact, team contacts); fall back to the current weapon preference, then nearest. Tiers in `ShipEngagement`: previous class → loadout preference → nearest; `AttackTargetState.LastTargetClass`.
- Record the dead target's class in `AttackTargetState` next to `LastTargetPosition`.

### 6. Team vision must be a required dependency
- `ShipOrderRunner` ctor: `ITeamContacts teamContacts = null, PlayerId owner = default`; `FindReplacement` silently skips team vision when null.
- [x] Make both parameters required (no defaults, no null check); `ShipInstaller` already passes them. Done.
- [x] Tests that build a runner (`ShipPursuitTests` and others) pass a fake `ITeamContacts`. `NoTeamContacts` fake.

### 7. Online lobby AI difficulty
- [x] Persist the last AI difficulty per slot in the online lobby (offline already done). Separate `onlineOccupantChoices`; `FakeSettingsService` for `PopupServiceTests`.

### Tests
- [x] Affected tests reviewed and updated (see R5; two gaps listed there). User re-runs `EnemyAi`, `Ships`, `BattleInput`, `Navigation`, `Capture`, `Ui`.

## Verification of `2955d807a` (2026-11-19)
Source-inspected. No tests run by the agent.

### Correct (no action)
- Mining first: UltraHard rule sits before `NeedsMinimumFleet` (`EnemyProductionDecisionModel.Evaluate`); unaffordable miner → `None` (saves).
- Mining target = catalog `MaxCount` 5 (`EnemyProductionStrategy.cs:249`).
- `NeedsFleet` now above income research; mining + required satellites outside it.
- Recovery: pressured living station added as anchor candidate, score `2` vs relays `≤ 1` → always wins; `FindRetreatAnchor` returns it without route filters. Withdrawal unchanged.
- Pursuit: kill without replacement or chase ending on its own → `MoveTo(_pursuitOrigin)`; outside order wins. Pursuit starts only from Idle, so origin position = previous order.
- Replacement tiers: previous class → loadout preference → nearest (`ShipEngagement.Offer`).
- `ITeamContacts` + owner required; registered in `SkirmishScope.cs:227`; owner via `ShipInstaller` `WithParameter(Owner)`.
- Online lobby: separate `onlineOccupantChoices`; host restores non-human rows on `BeginHostLobby`.
- Capture edge test: expects `10f * 0.9f`.

### Real issues
- [x] **R1 — Centre pads locked behind all 4 centre mines.** `EnemySiteObjectives.cs:103`: a pad at a point with mines is capturable only when `OwnsPointMines` (every mine at the point owned). Centre = 4 mines + 6 pads → AI must take all 4 mines before any pad. Rule 3.3 says pads → satellites → mining. Fix: pads at a point are capturable without owning its mines. Fixed 2026-11-19: pads are capturable without owning the point mines (`EnemySiteObjectives.IsCapturable`); test renamed `CaptureSelection_SatellitePadIsCapturableBeforeItsPointMinesAreOwned`.
- [x] **R2 — Centre gets 1 required satellite, not 3.** `EnemyDefensePad.IsZonePad` = point without mines → centre counts as a mining point → `MINING_POINT_SATELLITES = 1`. Satellite-first rule (2026-11-18): relay/centre zone = 3. Fix: the centre point (and relays) require 3 even with mines; other mining points 1. Fixed: `EnemyDefenseSnapshotBuilder.IsZonePoint` = no mines or more mine sites than `SITES_PER_MINING_POINT` (centre) → 3 required satellites.
- [x] **R3 — Opening split does not match rule 3.2.** Fixed: `EnemyOpeningFocus` (Neighbour, Centre) on the group builder; `GetOpeningFocusAdjustment` shifts capture scores by 10,000 around the centre (largest capturable zone, reach 2 radii). `EnemyShipTaskGroupPlanner`: first detachment Neighbour, second Centre; no mines-only restriction and no left-behind rule in the opening; up to 2 detachments while no main force exists.
  - First detachment with 1 AI ship: `CanDetachForCapture(mineOnly: ships.Count == 1)` + `CreateGroup(unbuiltMinesOnly: ...)` → mine sites only, never the centre or a zone.
  - Second detachment needs `ships.Count - detached > 1` → only at 3+ ships (commit note "second decision cycle" is wrong).
  - Each detachment = 1 ship. Neighbour zone / centre are not targeted; the split depends on capture score + reservation only.
  - Fix: opening detachments target the nearest neighbour zone and the centre explicitly (centre pads/mines included), and the remaining group captures/patrols another zone.
- [x] **R4 — Required-satellite start failure blocks mining.** `EnemySiteConstructionController.Tick` returns after `StartSatellite` even when `TryStartConstruction` fails (`siteBuild started=False`); the same pad is picked every 3 s → owned empty mines never build. Fix: fall through to mining when the start fails. Fixed: only a started required satellite ends the tick; a failed start falls through to mining.
- [x] **R5 — No tests for new behaviour** (AGENTS.md test maintenance). Missing: pressured-station recovery anchor (`EnemyAi`), pursuit return to origin + outside-order wins (`Navigation`/`Ships`), previous-class replacement tier (`Navigation`), two opening capture detachments (`EnemyAi`), site construction order mandatory → mine → scored (`EnemyAi`/`Capture`), online occupant persistence in `LobbySettingsDataTests` (`Ui`). Added: `OnlineOccupantChoice_IsKeptSeparatelyFromOffline`, `TaskGroups_OpeningDetachmentsAreAimedAtNeighbourThenCentre`, `FindNearestEnemy_PrefersThePreviousTargetClassOverANearerContact`, `IdlePursuit_ReturnsToTheStartingPostWhenTheAttackerDies`. Still missing: pressured-station recovery anchor and the mandatory → mine → scored construction order (both need heavy collaborator setups).

### Not issues (misunderstandings)
- "Pursuit should restore the previous order": pursuit only starts with no order, so returning to the position is the full restore.
- Mining before ships delays the first ship until 5 miners (5,000 credits): this is the rule; the human's first ship came at 45.8 s.

