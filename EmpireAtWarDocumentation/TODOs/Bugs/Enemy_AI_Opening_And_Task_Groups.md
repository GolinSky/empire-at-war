---
category: Bugs
status: in-progress
created: 2026-11-19
follows: "[[TODOs/Bugs/Enemy_AI_Economy_Strategy]]"
reopened: 2026-11-21
---
# Enemy AI Opening and Task Groups

## Status (2026-11-22, W23–W28 and W30 implemented; W29 blocked by W8)
- **W23–W28 and W30 implemented and compiled (0 errors) 2026-11-22. Tests written, not run (local override).** → "Open work — implementation notes (W23–W30)". W29 waits for the W8 flag review; W28 cause fix waits for the next host log.
- **Online 2v2 of 2026-11-21 20:13 (build `fa7d138ad`, v0.1.75: W1–W13 + pad expansion, without W15–W22): both AIs lost.** User: opening, early-mid and mid game are great; late-mid and end game the AI is tied to zones, ignores the hostile armada and its ally. Log confirms → "Evidence — online match 2026-11-21 20:13" and **W23–W30** (planned, not started). W15–W22 are still untested by the user.
- **W15–W22 implemented and compiled. Tests green 2026-11-22: `EnemyAi` 613/613, `Combat` 329/329.** → "Open work — implementation notes (W15–W22)".
- Battle 16:04 (first on W1–W13 code): AI lost; causes → W15–W22. Not verified in a battle yet → [[TODOs/User_Acceptance_Queue]].
- W1–W7 and W9–W13 implemented and compiled; tests ran on the user's request (see "Open work — implementation notes"). Still open: **W8 user review** of the role flags (AI does not read them yet) and **W14** (Jenkins release build + Editor battle, user gates).
- Rounds are history. Everything still to do is in **Open work** below.
- Rounds 1–7, the Rebellion balance changes, the economy normalization and the compact AI log are implemented and committed.
- Tests run green on the user's request: full EditMode 4,451 / 4,451 (2026-11-20 18:56); `EnemyAi` 587, `Economy` 137, `Core` 169, `GameFlow` 220, `Navigation` 387, `BattleInput` 447 (2026-11-21).
- Not verified in a battle: round 7, Rebellion balance, economy normalization, attack-move rework, squadron rework, compact log. They wait for the same playtest → [[TODOs/User_Acceptance_Queue]].

## Decisions (user, 2026-11-21)
- **Capture wings:** cheapest fighter first. Interceptors are also bought when the money is there: decided in battle from money left after the mandatory bill, no fixed credit threshold. Reason: interceptors are faster and use a second build queue.
- **Rebellion levels 4–5 damage / prices:** ignored here; the user cut prices in another task.
- **Survival mining:** normalize like skirmish.
- **Opening battle asteroid:** any hostile unit in the zone triggers it (as built). Closed.
- **Attack-move capture pull-back:** fix now.
- **Abilities:** the AI uses the auto-ability system for every ability; no AI rules of its own.
- **Ship choice:** role labels per ship (flags) so the AI picks by role and ship class, not by price alone.
- **Second station:** build it early (income bonus, buffed station).
- **Ships die fast (median life 44 s):** dropped from this plan; the user balances durability (stations and platforms already buffed).
- **Numeric order-rate gate:** removed. No hard-coded orders-per-minute limit.
- **Spawn telemetry:** one merged `notOpen` counter accepted; the "raise wing vision" fallback is closed (149 / 151 spawn attempts succeeded).
- **W10 and W11:** accepted (stall by site changes; keep the attack target).
- **Tests:** only relation and wiring tests; no tuned values (commit `271d02d4c`, `AGENTS.md` "Never pin tuned balance values").

## Explanations asked for (2026-11-21)
- **Stall measure.** Agreed: a zone force that changes no site owner for 120 s leaves. Built: it leaves when the zone's *structure picture* (own structures, hostile statics, held flag) is unchanged for 120 s. Difference: killing a satellite the enemy rebuilds counts as progress in the built version, so a siege like 23:11 (satellite 15 rebuilt within 1 s each time) never counts as stalled. → W10.
- **Order re-issue.** Agreed: re-send only when the target or point changes. Built: an order that has *ended* is re-sent at once (three order-repair tests need it). Measured 17:44: that path sent 2 of 2,127 AI ship orders → harmless. The churn is elsewhere: 1,427 orders (67 %) are `Attack` re-targets from the executor; `None` orders: executor 67, commander 53. Median 8 orders per ship-minute. → W11.
- **Why an orders-per-minute gate existed.** Battle 13:11: ships got `Attack` → `None` loops and sat idle 30+ s after spawning; each new order resets aim, facing and the combat hold. The number was a proxy for "ships are re-ordered before they act". Replaced by W11's rule (keep the target), no numeric limit.
- **Surplus buying.** Agreed: "money above 2 × the unit being saved for → also buy an affordable ship". A unit being saved for is unaffordable by definition, so it was built as: after a purchase with money ≥ 2 × its price, buy one more ship in the same cycle, from level 2. Fired 5 times in 17:44. It is not what limits spending: with 48,000–57,000 banked (18:01–19:01) the AI stood at 118–120 of 120 unit capacity. No change planned.
- **Why no bomber rule was decided.** G8 only added the `raidSkip` row and deferred the rule to "after the next battle"; nobody acted on the 17:44 rows. They say: `noBombers` 10, `noKnownMine` 5, `notWinnable` 5, `holding` 2 → the AI mostly has no bombers and knows no mine, because bombers are bought only for mines it has seen. → W9.

## Open work (all findings; 2026-11-21)
### P0
- [x] **W1 Second station levels up.** `EnemyProductionStrategy.TryBuySecondStationLevel`: station stands ∧ level < main level ∧ none queued ∧ affordable ∧ nothing mandatory pending → buy, one at a time; after the main station falls its upgrades rank before ships. `EnemyFactionController`: accept the request, real build time, `SecondStationLevelModel.CompleteLevelUpgrade()`, refund when the station dies. (was L1–L3)
- [x] **W2 Second station early.** From level 2 its price is reserved before ships (like the level-upgrade reserve), so "buy what fights now" cannot starve it (17:44: bought 6:36). Bonus today: +25 % station income.
- [x] **W3 Placement follows the living base.** `EnemyStructurePlacementService.GetStationCenter()` → `IProductionAccess.TryGetProductionBase`; no living base → no miner, platform-ring or second-station search. A destroyed own station closes station radius + 400 around the wreck for the battle. Miners: none while a known hostile ship is within 1,000 of the base or the turtle runs; then one per 10 s on the side away from the hostiles. (was L9–L11)
- [x] **W4 Wing commander and the squadron rework.** `ISquadron.IsEngaging`; `IssueOrder` skips the capture-hold `MoveTo` and the bomber-hold, station-defence and home attack-moves while the wing is engaged within its idle leash; a wing pulled > 120 from an uncontested spot returns by `MoveTo`. Beacon `MoveTo` stays unconditional. (was L5–L6)
- [x] **W5 Capture pull-back (attack-move).** `EnemyTaskForceExecutor.ExecuteCapture`: the 5 s "stalled" pull-back applies only to a ship that has not been inside the circle since its order; ships that arrived and went out to fight are bounded by the leash only.
- [x] **W6 Wing purchase.** `TrySelectSquadron`: capture wings = cheapest `Fighter`-class type until the capture-wing target is filled. An interceptor wing is added in parallel (own pipeline) when money − pending mandatory costs (miners, level reserve, satellites, mines, W2) ≥ its price. Elite fighters and extra interceptors only through `TrySelectCounterUnit`. (was L7)
- [x] **W7 Abilities fully on the auto system.** Lock S-Foils, Replenish Wingmen, Lucky Shot and Gravity Well get an auto role or `IShipAbilityAutoUseRule`, used by player auto-power and AI alike. Delete `ShipAbilityAiUse`, the AI target picking in `EnemyShipAbilityController` and `AbilityUseChance` / `AbilityTargetPrecision`; the controller only runs `AutoPowerDecider` for AI ships.
  - Consequence: difficulty no longer changes ability use. Player auto-power also gains these four abilities.

### P1
- [ ] **W8 Ship role labels.**
  - `[Flags] enum ShipRole` on ship data. Proposed flags: `Balanced`, `Brawler` (durable for its class), `Dps`, `Fragile`, `Fast`, `Slow`, `Carrier`, `MissileSpecialist` (hull damage), `IonSpecialist` (shield damage), `AntiStrikecraft`, `Artillery`, `Support`.
  - Examples (user): Venator = `Carrier | Balanced`; Recusant = `Dps | Fast | Fragile`; Victory I = `MissileSpecialist | Slow | Fragile`.
  - AI: pick the need (answer hostile hull / shields / wings / carriers) → candidates with the matching role inside the tier span → then price. Replaces price-led choice (Peleset over Recusant) and the J3 value exemption.
  - Validation tests (relations, not values): `Carrier` ⇒ hangar bays > 0; `MissileSpecialist` / `IonSpecialist` ⇒ that weapon family is the largest share of its loadout; `Fast` / `Slow` ⇒ above / below its class median speed; `Brawler` / `Fragile` ⇒ above / below its class median durability; every ship has ≥ 1 flag.
  - Open inside this step: the user reviews the flag list and the per-ship assignment (≈ 77 ships) before the AI reads them.
- [x] **W9 Bomber raids.** A hostile-owned mining site counts as a raid target without having been seen (site ownership is shown on the map; verify it is public to players, else keep the sight rule). Bombers are bought for such sites (cap 3 per site stays) and raid together with their group's fighters. `notWinnable` → wait for the group, never go alone.
- [x] **W10 Stall by result as agreed.** `IsStalledByResult`: no site in the zone changed to the AI's team for 120 s → zone marked like a retreat. Destroyed or rebuilt statics do not count. Held zones under attack stay exempt. Accepted by the user 2026-11-21 (replaces the built measure).
- [x] **W11 Keep the attack target.** `EnemyTaskForceExecutor` EngageLocal: a ship keeps its `Attack` target until it is destroyed, cloaked or out of reach; a new target only then. No numeric gate. Accepted by the user 2026-11-21.
- [x] **W12 Survival mining normalized.** Survival mine income = `MiningPool.PER_PLAYER` × 2 × defenders / ring spots (the defenders hold the whole "map"), split between defenders. Small solo (6 spots) 67 per spot; Medium 2 defenders (8 spots) 50 each per spot; whole ring = 400 per defender in every setup.

### P2
- [x] **W13 Tests, relations only.** Second-station upgrade bought / not above main / refunded; placement origin follows the living base, nothing inside the closed circle; no order to an engaged wing near its point; capture pull-back only before first entry; capture-wing choice (fighter first, interceptor with spare money); every ability has an auto rule; role-flag validation (W8); survival pool pays the same per defender for the whole ring. Old gaps: turtle orders, route defenders, patrol wing and beacon, lone wing, bomber hold, station platform ring, cheapest platform for a small fleet, tier floor. (was L4, L8, L12)
- [ ] **W14 Verification still owed.** Compile the compact AI log with release defines (next Jenkins player build) and read one host-side file. One Editor battle for the attack-move risks: station defenders that never settle against a moving attacker; losses on the approach.

### P0 — battle 16:04 (2026-11-21, implemented the same day)
- [x] **W15 Shield regeneration in the strength math.**
  - `UnitCombatProfile` gains `ShieldRegen` (per second: `ShieldRegenerateValue / ShieldRegenerateDelay` from ship / squadron / platform data). `ForceComposition` sums it and the unit count per class.
  - `CombatMatchup.TimeToDestroyClass`: shield damage that counts = `shieldDps − regen of one unit of the class` (focus fire meets one unit's regeneration at a time; class average = regen sum / units). Result ≤ 0 → shields never fall → only piercing damage counts, else `MAX_TIME`.
  - Reaches every user of `Advantage` / `TimeToDestroy` at once: fleet and local advantage, zone `required`, station time-to-loss, counter-selection.
  - `GetCombatValue` (value per credit, J3 exemption): durability = hull + shields + regen × the unit's own kill window is out of scope; keep, re-check after W8.
  - Own unit with a lost shield generator → regen 0 (live health); hostile sightings use the profile value.
- [x] **W16 Repair in the strength math.**
  - a) A known hostile repair source (`IRepairSourceFacade`; today `RepairSatellite`) within its `RepairProfile.Radius` of a rated fight lowers the hull damage that counts: `FractionPerTick / Interval` × max hull, on at most `MaxTargets` units. Own repair sources the same for the own side.
  - b) A known hostile repair satellite is the first structure target of a force that fights inside its radius (raid-structure list already holds it).
  - c) **AI builds repair satellites (user: yes, 2026-11-21).** `EnemyDefenseSnapshotBuilder.LoadOptions` stops skipping `SiteFacilityType.RepairSatellite`. It has no weapons, so the damage-profile scoring cannot pick it → own rule in the pad choice:
    - at most one per pad group (`PointId`);
    - only when the group already holds ≥ 1 armed satellite (built or constructing) and has ≥ 2 pads;
    - only where something of the team stands inside `RepairProfile.Radius` (400) of the pad: station, platform, mine or another satellite;
    - single-pad points (free pads of [[Done/Features/Map_Satellite_Pad_Expansion]]) never get one.
    - With the pad expansion: home 4 pads → 3 armed + 1 repair beside the station; outer zone 4 → 3 + 1; main zone 8 → 7 + 1.
- [x] **W17 Platforms and satellites in local fights.** `BuildHostilesNear` adds known armed hostile structures (reuse `EnemyZoneObjectiveCollector.AddArmedStructures`); `BuildOwnNear` adds own platforms. Zone ratings already count them; `localAdvantage`, `baseThreat` and `fleetAdvantage` do not, so EngageLocal reads a fight under hostile platforms as even.
- [x] **W18 No stacking in the build queue.** A ship type with a build already *waiting* (bought, not in production) is not bought again; the money goes to another type or to the unit being saved for. `GetArrivalSeconds` returns the real wait (`GetQueueRemainingTime` + build time) always, not only when the station is critical. No number: the rule is "one in production + none waiting per type".
- [x] **W19 Bombers that cannot raid.** 34 Hyena bought, 0 raids, 41 lost (34 at the own base / own zone). They come from counter-selection (answer to capitals), then wait at the raid hold. A bomber wing with no raid (`noKnownMine`, `holding`, `notWinnable`) attacks the ship target of its group's fight instead of holding; `TrySelectCounterUnit` buys bombers only while the group has ships to go with. **Accepted by the user 2026-11-21.**
- [x] **W20 Retarget reason in telemetry.** `ai_orders` `issued` row for `Attack` gets `retarget=dead|cloaked|outOfReach|new`. Needed to prove W11: 194 of 608 retargets left a living ship; the log cannot say why.
- [x] **W22 Ready for more satellite pads** ([[Done/Features/Map_Satellite_Pad_Expansion]], status `done` 2026-11-21: home 1 → 4, outer zone 2 → 4, main zone 6 → 8, free lane pads 2 / 6 / 10 with a unique `PointId` each; ≈ 2.5 × pads).
  - W17 becomes mandatory first: a zone can hold 8 hostile satellites; without them the local rating is wrong by more than today.
  - Satellite spending: "Fill owned pad" / "Hold the zone with satellites" must not outbid ships. Rule: one satellite build waiting at a time (same idea as W18); pads filled in the order threatened point → home → held zone → free pad.
  - Free pads: each is its own defence point with 0 mines → check `EnemyDefenseSnapshotBuilder._sitesByPoint`, `EnemySiteObjectives` and `EnemySquadronCaptureAssignments` accept a point without a mine; a free pad is a capture-wing target, never a ship-force objective.
  - Capture-wing target count must not grow with the pad count (`GetCaptureWingTarget`): wings per contested *point*, not per pad.
  - Home pads overlap in capture range (500) → one wing takes all 4; count them as one target.
  - Order: implement with or right after the pad plan; the review points are also listed in its step 7.
- [x] **W21 Tests (relations only).** Repair satellite chosen only into a group with an armed satellite and ≥ 2 pads, never twice per group, never on a single-pad point; capture-wing target equal for a point with 2 and with 4 pads. Regen ≥ shield damage → shields never rated as breakable; more regen → lower advantage; repair source in radius → lower advantage; local advantage falls when a known hostile platform is added; a type with a waiting build is not chosen; bomber wing without raid gets the group's ship target.

### P0 — late game (online match 20:13; 2026-11-22, W23–W28 and W30 implemented)
- User rule (2026-11-22): **do not change the opening, early-mid and mid game.** The new rules switch on by situation (a hostile armada exists, a team station is under pressure, a zone has no armed defender left), never by clock.
- [x] **W23 A zone takes only the force it needs.** `EnemyShipTaskGroupPlanner`: a zone force = the smallest group whose advantage over the zone's known defenders + statics passes the zone's `required`; the rest of the pool stays with the main force. A zone with no armed hostile left (defenders 0–1, statics 0–1) keeps a capture detachment only (wings first, ships only if no wing is free). Today the whole pool goes: 22:32 `zone:0:ships13:defenders1:statics1`.
- [x] **W24 Help the ally in time.** `TeamAiCooperation` / `SelectAllyDefenseRequest`:
  - request at first station pressure (`HasPressure`), not only when the station is critical (stations fall faster after the 2026-11-22 durability nerf);
  - `recoveryMode` no longer blocks the answer (Player3 was in recovery 15:14–35:11 → could never answer);
  - every allied main force without station pressure of its own answers, not only the nearest; its zone detachments (W23) stay;
  - the answer is rated against the attackers at the ally's station together with the ally's force and station (combined strength), so two weak fleets go together instead of one staying away.
- [x] **W25 Know the hostile armada.** `TeamAiPicture`: group confident hostile ship sightings into armadas (ships within one engagement radius of each other): centre, strength (`ForceComposition`), heading from the centre's movement between refreshes, and the team asset it moves toward or stands at (station, held zone, mine group).
  - New main-force objective **Intercept**: an armada that threatens a team station or held zone is the main force's target when team strength at the meeting point (own + answering allies + statics there, W17) reaches the retreat advantage; else the force gathers at the threatened asset under its platforms instead of holding the centre.
  - An armada that leaves a zone releases the zone force to W23 size at once.
  - `HuntFleet` stays for a clearly stronger fleet; Intercept is the even / weaker case that is missing today (states seen in 40 min: no `HuntFleet`, no `AssaultBase`).
- [x] **W26 Build the armada before committing (late game).** (scoped: see notes) While a hostile armada outmatches the main force and no team station is under pressure: new ships gather at the staging point (`reserve`) until the team's combined strength passes, then go as one. Counter-selection rates candidates against that armada, not against every sighting. No trickle of single ships into it.
- [x] **W27 Platforms never outbid the fleet.** A platform is bought only when (a) the ship choice is blocked by the unit cap, or (b) it is the first platform of a station ring / held zone. Not while a team station is under pressure and a ship can still be bought. Player3: 153,000 credits on 24 platforms (7 Empress, 6 Golan III), 44,000 of it 20:00–24:00 with one ship bought while the ally's base fell.
- [ ] **W28 Late-game money must be explained and spent.** (logging done; cause fix after the next host log) Bank 46,000–93,000 (28:00–32:00) with 8–14 ships alive. The compact log cannot say why: add `production-wait` to `_compactKinds` (one row per AI per `COMPACT_INTERVAL`). Compact `purchase` rows are also incomplete (deployments exceed purchases for 8 ship types; `surplusShip` buys write no `purchase` row) → write one. Then fix the cause from the next host log.
- [ ] **W29 Unit choice by role (wires W8).** See "How the AI picks a unit" below. Critical-station buying drops the tier floor and ranks by arrival time → Player3 bought 11 Nebulon-B, 6 MC50 and 7 MC30c at level 5 (28:00–36:00). With roles: under pressure prefer `Brawler` / `AntiStrikecraft` / what answers the attackers inside the tier span; cheap low-tier ships only when nothing in the span arrives before the loss. **Blocked by the user's W8 flag review.**
- [x] **W30 Tests (relations only).** (written, not run) Zone force not larger than needed; empty zone → detachment only; ally answers at pressure and in recovery; armada grouping (two clusters → two armadas; heading toward the nearer asset); Intercept chosen when combined strength passes, gather otherwise; platform not chosen while a ship is affordable and a station is pressed; `production-wait` in the compact kinds.

### How the AI picks a unit (answer, source 2026-11-22)
- Not by price alone. `EnemyCounterProductionModel.TrySelect`: score = (gain in advantage against the known hostile force when one such unit is added) ÷ (price share of the money, or slot share of the free unit cap, whichever is larger) × designer class matchup preference ÷ √(owned of that type + 1).
- Filters before scoring: station level and one below (`SHIP_TIER_SPAN`), plus any lower ship with better value per credit (J3); affordable only, unless the fleet has ≥ 10 ships and beats the known enemy (then it may save); no type with a build waiting (W18, new); a critical station drops the level filter and ranks by arrival time.
- Why it reads as "level and price": a small or losing fleet may not save, so only the affordable ships compete, and gain per credit favours cheap hulls. The fleet was rated weaker than the enemy for all 40 minutes (0.04–0.87) → it never saved for a better ship.
- **Role labels (W8): the 77 ship assets carry proposed `ShipRole` flags; the AI does not read them.** Used only by the Editor proposal tool and `ShipRoleValidationTests`. Wiring waits for the user's flag review (→ W29).

### Open work — implementation notes (2026-11-21)
- [x] W1 `EnemyProductionStrategy.IsSecondStationLevelDue` / `TryBuySecondStationLevel` (after miners, priority income research and a due level; before combat research, platforms, ships). `EnemyFactionController` accepts `SecondStationLevelUnitRequest` (stands, next level, < main level, one at a time via `IsUpgradingSecondStation`), real build time, `CompleteLevelUpgrade()`, refund on `BuiltChanged(false)`. Restored upgrades keep their time left. Telemetry: `purchase` category `SecondStationLevel`; `station` row `secondLevel=`.
- [x] W2 second station bought at 1 × price; while wanted and unaffordable its price is held before ships/platforms/combat research (`GetSecondStationReserve`); same hold for second-station upgrades once it is the base.
- [x] W3 `EnemyStructurePlacementService.TryGetStationCenter` → living base of the host; none → no search. Lost base (polled: tracked base destroyed) closes station radius + 400 for the battle. `CanPlaceMiner`: no hostile placement threat within 1,000 of the base, not within `TURTLE_SECONDS` of a loss. Lost miners rebought one per 10 s (`MINER_REBUY_INTERVAL`); station-ring search uses the half away from the hostile centroid.
- [x] W4 `ISquadron.IsEngaging` + `IdleLeashRadius`; `EnemyWingOrderRules.KeepsFight` skips station-defence, bomber-hold, capture and home orders for an engaged wing inside its leash; `ReturnsByMove`: a wing that reached an uncontested hold returns by `MoveTo`. Beacon unchanged.
- [x] W5 `EnemyTaskForceExecutor._captureEntered`: the 5 s stall pull-back only for ships never inside the circle.
- [x] W6 `TrySelectSquadron(captureWing)`: cheapest `Fighter` (interceptor only if no fighter); interceptors and elite fighters never in the rotation. `TryBuyParallelInterceptor`: one interceptor on its own pipeline while capture wings are missing, when money − (missing miners, due level, second-station reserves) ≥ price, no priority construction pending.
- [x] W7 `LockSFoilsSettings` (auto-use: travelling; new `IShipAbilityAutoStopRule`: stop when not travelling), `ReplenishWingmenSettings` and `LuckyShotSettings` auto-use rules (targeted rules take `AutoPowerCombatSnapshot.FindTarget`), Gravity Well role Engage. `AutoPowerDecider` gets a cancel callback (`ShipAbilityService.TryRequestCancel` for players). `ShipAbilityAiUse`, `aiUse`, `AbilityUseChance`, `AbilityTargetPrecision`, `AbilityDecisionInterval` deleted; `EnemyShipAbilityController` only runs the decider.
- [ ] W8 review round 1 (user, 2026-11-22) → rules changed and re-proposed: speed / durability / guns compare within the ship's **own faction and class** (`ShipRoleRoster` from faction definitions); durability is **per credit** (Procursator: Balanced); `SPEED_BAND` 5 % (Victory II no longer Slow, Victory I Slow); new `AntiCapital` = damage vs capitals > faction-class median + 15 % (Tector, ISD II, Chimaera); `Support` = has Interdict (Interdictor). Faction marks on `FactionDefinition.shipMarks`: Empire Brawler+Slow, Rebellion Brawler+Dps+Fast, Republic Fragile, Separatist none. Devastator Fast (Imperial Venator and Procursator share speed 25 → also Fast). Still awaiting user sign-off; AI does not read roles.
- [ ] W8 review round 2 (user, 2026-11-22, from an external lore audit) → 5 rule fixes, re-proposed to all 78 ships:
  - Faction class < `ShipRoleRoster.MIN_CLASS_MATES` (3) → compare with that class across all factions (owner entries). Executor: + Dps, Slow, AntiCapital.
  - `IonSpecialist` also from an ion ability (`IonPulse`, `IonShot`) → Malevolence.
  - `Carrier` needs `MIN_CARRIER_BAYS` (2) → Interdictor, MC80 Freedom, Mediator lose Carrier.
  - Weapon share: missile / ion / laser+PD groups and every other family compared group vs group; gun tie → higher DPS; at most one group wins → MC30c MissileSpecialist only; Victory II, Nebulon-B, Pelta lose AntiStrikecraft/Ion (ties won by turbolasers).
  - Faction marks re-proposed: Empire Slow, Rebellion Brawler+Dps, Republic Fragile, Separatist none.
  - Rejected from the audit: hand-assigned lore roles (`Tank`, `Command`, `Transport`, `BattleCarrier`) with no data source. Compiled; tests not run. Still awaiting user sign-off.
- Interdict (Gravity Well) auto-use, source 2026-11-22: role `Engage` → fires when a warship (or 2+ units with enough DPS) is in weapon reach; 30 s, recovery 60 s, range 900. Effect: anchors the caster (Move / AttackMove / Guard / Hunt rejected; Retreat cancels it) and blocks hostile reinforcement drop-ins within 900. Gaps: fires in any fight, not where drop-ins can happen; AI orders to an anchored Interdictor bounce for up to 30 s. Not changed yet.
- [ ] W8 `ShipRole` flags + `ShipData.roles`; first proposal written to all 77 ships by `Tools/Empire At War/Units/Ships/Propose Ship Roles` (`ShipRoleRules`: durability ±15 %, speed ±10 %, guns +15 % vs class median; loadout shares). Matches Recusant; Venator came out Fragile (user: Balanced), Victory I Balanced (user: Fragile). **User reviews flags; AI choice by role not wired.**
- [x] W9 site ownership is not public (minimap shows last seen owner) → sight rule kept. Mine and satellite raids take the group's fighters along; bombers never raid alone.
- [x] W10 `EnemyZoneObjective.OpenSites` (sites not owned by the team); stall when no site turned to the team (fewer open sites or newly held) for 120 s (`IsStallProgress`).
- [x] W11 `EnemyAttackTargetRules.KeepsTarget` (destroyed / cloaked / beyond 1.5 × weapon range + target radius) for EngageLocal strike attacks.
- [x] W12 `MiningPool.GetSurvivalSpotIncome` = 200 × 2 × defenders / ring spots; whole ring = 400 per defender.
- [x] W13 tests: `EnemyOrderRulesTests` (new), `ShipRoleValidationTests` (new, Ships), placement (4 new), controller second-station upgrade, strategy W1/W6, `EnemyShipAbilityControllerTests` rewritten, catalog "every ability has a role or rule", survival pool.
- Verification 2026-11-21 (EditMode, standard, no explicit): EnemyAi 596/596, Abilities 231/231 (after fixing a factory-test clash), Economy 141/141, Ships 327/327, Squadrons 232/232, Core 169/169, GameFlow 221/221, Balance 70/72 — 2 failures from `d181ed8e6`, not this work: `ShipFactions_DoNotShareShieldRegenTierTables` (Republic Victory/Tector use Empire ship data, Empire Quasar Fire uses Rebellion data) and `Grid_ShowsEveryTableAndTierAndEditsDraft` (no draft change on cell edit).
- Reusable guidance: none extracted yet; rules live in code docs (`EnemyWingOrderRules`, `EnemyAttackTargetRules`, `ShipRoleRules`).

### Open work — implementation notes (W15–W22, 2026-11-21)
- [x] W15 `UnitCombatProfile.ShieldRegen` (ship/platform: `ShieldRegenerateValue / ShieldRegenerateDelay`; squadron: × `MemberCount`). `ForceComposition` sums regen, units and max hull per class; `ShieldRegenPerUnit(class)`. `CombatMatchup.TimeToDestroyClass(…, shieldRegen, hullRepair)`: net shield dps ≤ 0 → piercing only, else `MAX_TIME`. `TimeToDestroy` passes the defender's per-unit regen and repair; also `EnemyCounterProductionModel.StrikecraftClearSeconds`, `EnemyDefenseStrategy.GetFit`. `ForceCompositionBuilder.GetShieldRegen`: lost shield generator → 0. Single-target `TimeToDestroyTarget` callers keep regen 0 (optional param).
- [x] W16a `ForceComposition.AddRepairSource(fractionPerSecond, healersPerTarget)`; `RepairFractionPerSecond` = rate × min(sources, healers); `HullRepairPerUnit` = that × class max hull / units, never strikecraft. `EnemyZoneObjectiveCollector.AddHostileRepairSources` (known: visible or remembered, `CanRepair`, centre inside `Radius`) / `AddAlliedRepairSources` → zone defenders, zone own support, local fights.
- [x] W16b `EnemyStrategicContextBuilder`: a hostile repair satellite with another hostile armed structure inside its radius ranks `HOSTILE_REPAIRER_FIRST_BONUS` (3000) > satellites (2000) > relay (1000).
- [x] W16c `LoadOptions` loads the repair option (`_repairRadius` from `DefendPlatformData.RepairProfile`). `EnemyDefensePad.PointPads`, `HasTeamStructureInRepairRange`. `EnemyDefenseStrategy.TrySelectRepair` (money − reserve; `CanTakeRepairSatellite`: no hostiles, ≥ 2 pads, ≥ 1 armed, team structure in reach, none yet); controller order: mandatory → mine → repair → scored → fill. Mandatory and scored counts use `CountArmed` (repair excluded); scored never picks repair.
- [x] W17 `BuildHostilesNear` + `AddArmedStructures` + hostile repairers; `BuildOwnNear` + `AddAlliedArmedStructures` (allied platforms/satellites) + allied repairers. `baseThreat` / `fleetAdvantage` unchanged.
- [x] W18 `IEnemyPurchaseProcessor.HasWaitingBuild` → `EnemyBuildQueue.CountUnfinished(id) > 1`; `CanReserve<ShipUnitRequest>` refuses a type with a waiting build (max 1 in production + 1 waiting). `GetArrivalSeconds` always returns queue + build (+ travel when a threat position is known); only critical paths read it.
- [x] W19 `EnemySquadronCommander.HoldIdleBombers`: a bomber without raid/escort attacks the visible hostile ship nearest it that an own ship has an `Attack` order on (`_bomberStrikes`), else holds as before. `TrySelectCounterUnit`: bomber-class squadrons only while a live own ship exists.
- [x] W20 `ShipOrderTelemetry.Detail` scope + `detail` column (last). `IssueStrikeAttack` groups receivers by reason: `retarget=new|slot|dead|cloaked|outOfReach`.
- [x] W22 `IsZonePoint` needs ≥ 2 pads when mineless; `EnemyDefensePad.IsFreePad` (0 mines, 1 pad) never mandatory; `IsHomePad` (own station ≤ 1000). Priority: threatened 1000 → home 100 → centre 20 / zone 10 → mining point → free −1. `EnemySiteConstructionController`: no satellite start while an own satellite is `Constructing` (`reason=oneWaiting`). `EnemySiteObjectives`: a satellite alone at its `PointId` is no ship objective (still a wing target). `EnemySquadronCaptureAssignments.AddTarget`: a pad inside an added pad's capture circle merges into it. `GetCaptureWingTarget` already counts zones, not pads → no change.
- [x] W21 tests (not run): `CombatMatchupTests` (+9, `Combat`), `EnemyDefenseStrategyTests` (+10; `BuildNext` fixtures now pass an empty `PadDirectory`), `EnemySquadronCaptureAssignmentsTests` (+3), `EnemyFactionControllerLifecycleTests` (+1, `CountUnfinished`), `EnemyUnitCommanderTests` (+3: free pad, bomber strike target ×2; `FakeEntity` gains `IUnitOrderObserverFacade`; two fixtures give their lone pad a point partner). Not covered: the W18 purchase path end to end, W16b ranking, W20 rows.
- Verification 2026-11-22 (EditMode, standard, no explicit, on the user's request): `EnemyAi` 613/613, `Combat` 329/329. First `EnemyAi` run 611/613: W19 blocked bombers for a critical station (`CriticalStation_BuysSquadronOnlyWhenItCanArriveInTime`) → fixed: critical station counts as "ships to go with" (station-defence wings fly to the station).
- Risks to watch in the next battle: W17 makes groups near hostile satellites withdraw sooner; W22 one-at-a-time slows opening zone satellites.

### Open work — implementation notes (W23–W30, 2026-11-22)
- [x] W23 `EnemyZoneAllocator.IsCleared` (not threatened, defenders ≤ 1, statics ≤ 1 = `CLEARED_DEFENDERS`) → `Trim` releases the farthest ships while the rest stays a Go: 1 ship, 0 when own wings in `OwnSupport` beat what is left (`IsWingHeld`; such a zone gets no ship and is no gather zone). `LeanZones` (set from `EnemyStrategicContextBuilder.LateGameSituation`) trims every zone the same way except a held zone under attack. Leftovers join a cleared or lean zone only while its force is not yet a Go → the rest stays in the reserve (main force).
- [x] W24 `AssessStation` requests at `HasPressure` with the known attackers and `BuildOwnNear` at the station. `TeamAiCooperation.TryGetDefenseAssignment(responder, now, minimumAdvantage, …)`: every fresh available main force answers when defenders + all available allied main forces reach the difficulty `RetreatAdvantage` over the attackers. `SelectAllyDefenseRequest()` no longer takes `recovering` and no longer runs the approach-safety check (combined rating replaces it).
- [x] W25 `HostileArmadaTracker` (in `TeamAiPicture.Refresh`): single-linkage over confident ship sightings, `LINK_RADIUS` 1000, `MIN_SHIPS` 6; heading from centre movement over ≥ 2 s; asset = nearest team station (radius 500) or held zone within 1000 of the centre 20 s ahead. Main force: `SelectArmadaResponse` → Attack marker at the armada (team = main force + `AddAvailableAlliedStrength` + allied platforms at the asset ≥ `RetreatAdvantage`) or Defend marker at the asset (gather). Skipped under own pressure and in HuntFleet / AssaultBase / DefendBase / RetreatValue. Point kept until the armada moves > 500. `EnemyShipTaskGroup.IsAnsweringTeam` → the allocator leaves the reserve alone. Telemetry `armadaResponse` (compact).
- [x] W26 scoped to armadas that threaten a team asset (user rule: no change to opening–mid game): such an armada outmatching own fleet + allies with no team station pressure → `TryCopyOutmatchingArmada` → counter-selection rates against it; main force gathers at the asset (W25 Defend); lean zones keep new ships in the reserve. Not built: holding the main force for an outmatching armada that threatens nothing.
- [x] W27 `firstPlatformsOnly` while a ship option exists and is not capacity-blocked → `TryGetDefensePosition(allowExtra, firstOnly)`: ring 1, zones only their first. No platform while `HasTeamStationPressure` and a ship can be bought.
- [ ] W28 `production-wait` and `armadaResponse` in `_compactKinds`; `production-wait` and `zoneAlloc` one row per AI per 15 s. `RecordPurchase` rows for surplus ship, scout, patrol wing, parallel interceptor. Open: read the next host log, fix the cause.
- [x] W30 tests (not run): `HostileArmadaTrackerTests` (new: two clusters, small group, heading → asset), `TeamAiCooperationTests` (every ally answers, combined strength, any-focus allied strength, `HasDefenseRequest`), `EnemyZoneAllocatorTests` (+5: cleared keeps nearest, wing-held needs none, lean smallest group, lean leftovers, held under attack kept; "free zones" test now expects the third ship in the reserve), `EnemyStructurePlacementServiceTests` (+1 first-only), `EnemyUnitCommanderTests.Allocation_EveryShipGetsATaskInTheCycleItArrives` (one-defender zone: extra ships stay main force). Not covered: `SelectArmadaResponse` / intercept vs gather and the platform-under-pressure block end to end.
- Risks to watch: cleared zones (≤ 1 defender) now keep one ship from the mid game on; `MIN_SHIPS` 6 decides when lean zones / intercept switch on; W27 also limits the centre's second platform while ships can be bought.
- Reusable guidance: none extracted; rules live in code docs (`EnemyZoneAllocator`, `HostileArmadaTracker`, `TeamAiCooperation`).

### Closed or ignored (no work)
- Rebellion levels 4–5 balance (ignored, other task). Ship life gate (user balances). Opening asteroid trigger (any unit, accepted). Spawn telemetry counter (accepted). Wing vision fallback (closed). Surplus buying (explained, kept). Republic ARC-170 / V-Wing at level 5 (balance owner, not this plan). "Held zone with no site left" risk (fixed by E3c in round 5).

## Evidence — battle 2026-11-21 16:04 (UltraHard Small 1v1, Separatist AI vs Republic human, 30,000 start, code `02fc12c9a` dirty with regen tiers raised, 1,702 s)
- Files: `Loading_20261121_160414_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261121_160509.csv`, `Battle_20261121_160417_map.csv`.
- Result: AI main station lost 21:08, second station 26:00. AI lost 134 ships (median life 86 s) and 153 wings; Republic lost ≈ 50 ships.
- Income was not the cause: AI 407–544 per s vs player 283–460 until 10:00.

### Faults
- **Shield regeneration unknown to the AI.** 77 % of the AI's damage ended in shields (2,859,611 shield / 873,656 hull); Republic's share 58 %. Secutor: 629,288 shield loss vs 76,009 hull, 1 lost (shields 30,000 → refilled ≈ 20 times). Regen tiers raised in the working copy: capital 216 → 600 per s, heavy cruiser 117 → 300, cruiser 90 → 175. `UnitCombatProfile` = hull + shields + dps; `CombatMatchup` treats shields as a one-time pool. → W15.
- **Repair unknown to the AI.** `RepairService`: 0.5 % max hull per s per repairer, 10 targets, 2 repairers per target. No AI code reads it; the AI does not target or build repair satellites. → W16.
- **Platforms missing from local fights.** `BuildHostilesNear` / `BuildOwnNear` use unit sightings only. 0–8 min: 22 of 27 lost ships died at the centre while the zone rating said `Assemble` (defenders 34, statics 13, advantage 0.58) and the local rating said 1.1–2.4 → EngageLocal. Likely cause, not proven per ship. → W17.
- **Build queue clogged.** 34 Diamonds (2,500 each, 85,000 credits) bought 1:30–7:00, each the moment money reached 2,500 (`candidates=1`). Same-type builds run one at a time → last Diamond delivered 17:22; 17 still waiting at 10:00. Waiting builds hold unit capacity: 116–120 / 120 with 9–15 ships alive, money unspent 28,011 (12:00) and 34,436 (18:01) while the station fell. Round-7 gate "capacity < 100 with < 15 ships alive" failed. `GetArrivalSeconds` returns 0 unless the station is critical. → W18.
- **Bombers never raided.** `raidSkip`: `noBombers` 16, `noKnownMine` 9, `notWinnable` 5, `holding` 4; `raids=0` in all 246 `wingAssignment` rows (up to 12 wings `@raid`). → W19.
- **Retargets.** `Attack` 900 of 1,345 AI ship orders (67 %, as before W11); 608 retargets: old target dead 293, not a ship 121, **still alive 194** (reason not logged). → W20.

### W1–W13 against this log
- W1 ✓ second-station upgrades 3:01, 4:21, 6:55, 10:28 → level 5. W2 ✓ bought 1:27 (17:44 battle: 6:36).
- W3 ✓ platform after the base loss placed on the second station's ring (21:09, 509 from the wreck). Miners: none rebought 19:30–28:00 (income 350 → 70); hostiles stood at the base the whole time → rule may be right, not verified.
- W6 ✓ Vulture wings (cheapest fighter), 2 parallel interceptors at 0:00–0:05 (`mandatory=7000 / 3000`).
- W9 ✗ no raid (above). W11 partly (above).
- W4, W5, W7, W10: no row in this log proves or disproves them. W12: Survival not played.

## Evidence — online match 2026-11-21 20:13 (host log; 2v2 Medium, Republic humans Player0 / Player1 vs Rebellion AI Player2 / Player3, 12,000 start, build `fa7d138ad` v0.1.75, 2,430 s)
- File: `Loading_20261121_201350_ai.csv` (compact log, release build; first real use → W14 "read one host-side file" is satisfied for the host). Matches played as client leave no AI log on this machine (the file stays with the host). `Player.log` / `Player-prev.log`: no exception, 4 `Tangent route build failed` fallbacks.
- Result: Player2 station lost 22:47, Player3 station ≈ 35:00, last station ≈ 40:00. Fleet advantage of both AIs 0.04–0.87 for the whole match (never ≥ 1).
- Income was level until 20:00 (AI 445–723 per s, humans 489–723).

### Faults
- **Zone-bound while the ally falls.** 21:09–22:55: Player3 held zone 0 with 12–13 ships, state `CaptureZone`, local advantage 100 (no hostile near; zone defenders 10 → 1, statics 9 → 1). In the same minutes Player2's station went from shields 568,752 (20:00) to lost (22:47). Player3 was in `CaptureZone` in every 2-minute sample 2:15–26:43. → W23, W24.
- **Ally help cannot fire.** Request only at `IsStationCritical`; responder must not be `recovering`; only the nearest main force answers. Player3 `recoveryMode` 15:14–35:11. → W24.
- **No armada awareness.** No rule reacts to the hostile main force leaving the centre for a base. `HuntFleet` / `AssaultBase` need a clearly stronger fleet → never seen. → W25, W26.
- **Platform spending.** Player3 153,000 on platforms vs 643,800 on ships; Player2 36,000. → W27.
- **Money unspent late.** Player2 55,267 (28:00) and 73,336 (32:00) with 8–14 ships; Player3 46,333 and 49,816. Cause not in the compact log. → W28.
- **Low-tier ships at level 5.** Player3 28:00–36:00: Nebulon-B × 11, MC30c × 7, MC50 × 3 while its station was critical. → W29.
- **Type stacking.** 0:00–4:00 Nebulon-B × 12 (Player2) / × 10 (Player3); 4:00–8:00 Assault Frigate Mk II × 11 / × 9. Same fault as the 34 Diamonds.

### Old-build faults against the new code (`2614f45f9`, not played yet)
- Type stacking → **addressed** by W18 (source: `HasWaitingBuild` in `CanReserve`), not battle-tested.
- Regen / repair / platforms in the strength math → **addressed** by W15–W17, not battle-tested. Not the main cause here: the AI already rated itself weaker all match.
- Bombers idle (`raidSkip` 46 + 39 rows) → **addressed** by W19, not battle-tested.
- Zone-bound, ally help, armada, platform spending, late bank, low-tier under pressure → **not fixed** (W23–W29).
- Role labels → **not wired** (W8 / W29).

## Status history (2026-11-20, round 7 implemented)
- Round-6 playtest played 17:44 (Rebellion AI vs Empire). User: start as good as before; Rebellion ship selection awful. Reopened and closed again the same day.
- Round 7 (J1–J3) implemented 2026-11-20 and committed. Notes → "Round 7". Tests ran green later (see Status).
- Acceptance → round-7 playtest gates (user; [[TODOs/User_Acceptance_Queue]]). A failed gate reopens this plan.
- Round 6 (G1–G11) implemented and compiled 2026-11-20, commit `9d0f57830`. Notes → "Round 6 — implementation notes".
- Round 5 (E1–E12) implemented 2026-11-20, commit `59a2c0776`; playtest 15:48: start and early mid-game perfect, mid-to-late bad.
- Process (user): implement the whole round first, then one playtest.
- Rebellion (AI) vs Empire was played 2026-11-20 17:44.

## Outcome of rounds 1–4 (closed 2026-11-20, reopened the same day)
- All scope implemented across rounds 1–4 (A1–A7, B1–B9, C1–C7, D1–D5, I18, I19). `EnemyAi` EditMode 561/561.
- Gate battle played 13:11 → failed (see Evidence — battle 2026-11-20 13:11).
- The "held zone with no candidate site" risk was fixed in round 5 (E3c).
- Reusable guidance → [[Architecture/Enemy_AI_Objectives]] (sections "Wing capture round 2" and "Rounds 3–4").

## Goal
- AI acts from second 0: wings fly out while the first miners are bought; every ship has a task; no waiting fleet.
- Keep what worked on `529094e87` (every ship captured at once, centre retaken). Adjust current code; no copy of old code.
- Evidence → [[TODOs/Bugs/Enemy_AI_Economy_Strategy]] (battles 20:45, 21:11).

## User rules (2026-11-19, final for this plan)
### Opening (0–30 s)
- Wing orders happen at 0–6 s, together with research / miners / level 2. Not at 20–36 s.
- Station wings are used at once: **1–2 wings → own-side outer zone, all others → centre**.
  - 2 wings to the outer zone when wings are cheap (CIS); 1 when wings are expensive (Rebellion) or money is short.
- Centre wings: take free pads on the **base-side edge**, build **2–3 satellites**, a mine if possible. A wing that dies after 3 satellites are built is a win.
- Outer-zone wings: capture a satellite pad first and build it, even if the wing dies. Then respawn wings and take the relay and the mine under the satellites (2 turbolaser, or laser + turbolaser).
- First buys: **fighter + interceptor** wings. No bombers at the start.
- **No corvette in the first 30 s.** Wings capture; money goes to satellites and mines first. Then 1–2 corvettes against enemy wings.
- **No level-1 corvette after level 2 is reached.** No exception.
- The strongest ship goes to the centre. No ship sits beside the station.

### Wings at a contested target
- The centre zone is big. A wing does not leave it: it takes another free pad, baits, comes back.
- Wings capture pads, mines and relays. They are not pulled off to escort.

### Centre priority
- The centre is **never excluded**. It loses priority when the AI is weak.
- It gains priority with: bigger fleet + reinforcements + economy (≥ 1 outer zone + home mine).
- With own satellites or a mine at the centre the AI **fights around them with a small force**: own satellites count as offensive defence.
- If the enemy moved its ships to the outer zones → the centre is weak → take it.

### When weak or behind
- Patrol all the time: intel and bait.
- Attack an outer zone. Enemy reinforcements arrive → retreat, deploy own reinforcements near the **other** outer zone, attack there. Repeat. Capture and hold.
- Sabotage all the time: kill mines.

### Unchanged
- Flow: zones → centre → defenses and levels → remaining zones or sabotage → base only at the assault gate.
- Ships do not wait. Task force groups as before. One or two wing groups that act.

## Capture check (source, 2026-11-19)
- **Correction 2026-11-19 22:40:** the radii below were read reversed. `CaptureSiteKind.Mining = 0` → radius **60**, 10 s per net ship. `Satellite = 2` → radius **500**, 6 s. One wing = 0.5 ship → a pad in 12 s from anywhere within 500; a mine in 20 s, only inside its 60 circle.
  - Centre layout (22:29 map): mines ≈ 270 from the centre, pads ≈ 470. A group standing **on a mine spot** captures that mine and 2–3 pads at once (user: "a couple of seconds").
  - Steps built on the wrong radii: one wing per pad, the 400 "hold near a pad" move order, mines after pads. Replaced by round 2 (B1).
- ~~Capture works. `CaptureSiteData.asset`: pad radius 60, 10 s per net ship; mining site radius 500, 6 s.~~
- AI builds on an owned site within ≤ 3 s (`EnemySiteConstructionController`, 3 s tick). 21:11: pad 7 owned 45 s → satellite 45 s; pad 6 owned 52 s → satellite 54 s.
- Satellites: 300–1,600 credits, 21–47 s build. Mine 25 s.
- Gate: no site construction at level 1 until the miners are bought and the level-2 upgrade has started (`OpeningMiningPending`, `OpeningLevelReserve`). 21:11: open from 6 s.
- Risks found:
  - Pad radius 60 + `AttackMoveTo`: a wing that engages leaves the circle → capture stops. Needs a hold-inside-circle order for capturing wings.
  - "Wing at the centre switched to escort": `AssignEscorts` takes any anti-fighter wing when a hostile wing is within 2,000 of any AI ship. Not a capture fault.
  - Station wings: `IsHangarLaunched && Guard → skip` → never ordered.

## First 3 minutes — battle 21:11 (AI Separatist vs human Republic)
| Time | AI | Human |
|---|---|---|
| 0–6 s | `IncreasedProduction`, 5 miners, level 2 upgrade | — |
| 1–13 s | Station launches 5 Vultures → all Guard the station | Station launches 3 ARC-170 |
| 7–10 s | Buys 3 Vultures, Patrol Frigate; money 548 | 11–14 s: station wings → centre |
| 15 / 23 / 31 s | Bought Vultures appear one by one | 18–28 s: 4 bought wings → outer zones |
| 20 / 28 / 36 s | First wing orders: centre, own outer zone, far outer zone | — |
| 25–52 s | Patrol Frigate: home mine → centre pad; C-9979: centre pad; 2nd Patrol Frigate stays near home | 44 s centre relay |
| 48–51 s | Wings switched to escort | 70 / 93 s outer relays |
| 50–95 s | 7–8 of 8 wings escort | 6 mines by 94 s |
| 154–206 s | 9 ships at own-side zone → 2 | reinforcements dropped there |

## Issues and fixes
### Opening
- **I1 (P0) Station wings never leave.** Fix: station-launched wings are free wings, ordered when they launch (1–13 s). Only wings of carrier ships stay with their carrier.
- **I2 (P0) First useful order at 20 s.** Fix: opening split at the first decision — outer-zone group (1–2 wings, by wing price and money) → own-side zone pad; centre group (all others) → base-side pads.
- **I3 (P0) Wings turned into escorts.** Fix: group A/B wings are never escorts. Escorts come only from carrier hangar wings and from corvettes.
- **I4 (P0) Wings leave or avoid a contested target.** Fix: inside a zone a wing picks the best **free** pad or mine (base-side first); contested → next free site in the same zone; no free site → bait at the edge, return. The power rule decides only where **new** wings go.
- **I5 (P0) Capturing wing drifts out of the pad.** Fix: capture order keeps the wing inside the circle until the site flips (move, not attack-move, for the last leg).
- **I6 (P0) Satellites first.** Fix: every pad a wing takes gets a satellite at once (already ≤ 3 s); opening money is reserved: satellites and mines before any ship.
- **I7 (P0) First buys.** Fix: fighter + interceptor wings (two types build in parallel). No bomber before the first mine is raided. No corvette before 30 s and before the first satellites and mines are paid.
- **I8 (P0) Corvettes.** Fix: level-1 corvettes only at level 1, max 2, never from level 2. (Emergency fallback already deleted.)

### Wing groups
- **Group A — centre.** Pads (base-side) → mines → relay. Stays, retakes, rebuilds satellites.
- **Group B — outer zone, then sabotage.** Own-side pad → relay → mine; then the other outer zone; no free zone → raid mines (bombers join here).
- Respawn: a lost wing is rebought and rejoins its group.

### Ships
- **I9 (P0) Fleet waits.** Fix: no idle reserve. Each ship gets a task in the cycle it deploys:
  1. Free site or zone → nearest free ship at once.
  2. Zone the free ships beat → they go together.
  3. Nothing beatable → weak-phase loop (below).
- **I10 (P0) Strongest ship sits at home.** Fix: the most expensive free ship takes the centre task; ships deploy one by one until the first outer zone is held (no waves of three).
- **I11 (P0) Weak force dies at the centre.** Fix: centre priority = base value × strength factor (fleet vs known centre force, held reserve, held outer zones + home mine). Own satellites and platforms inside the zone are added to the AI force. Never removed from the list.
- **I12 (P0) Small zones blocked.** Fix: delete `hostileZoneOnRoute` (route around the centre by a waypoint). Station pressure pauses new forces only while critical.
- **I13 (P0) Winning force recalled.** Fix: a force leaves on its own local fight (≤ retreat line), not on a zone recount. Defender reach = zone radius + 500.
- **I14 (P1) Force destroyed at a hostile relay.** Fix: order inside a hostile zone = satellites → relay → pads; reinforcements seen → retreat (weak-phase loop).

### Weak phase loop (new)
- Trigger: behind on zones or fleet advantage < 1.
1. Patrol ship + patrol wing always out (intel, bait).
2. Task force attacks the weaker outer zone.
3. Hostile reinforcements arrive (local advantage ≤ retreat line) → pull back.
4. Own reinforcements deploy near the other outer zone; task force goes there.
5. Zone taken → satellites, mine, platform → hold.
6. Group B and spare ships raid mines the whole time.
- Centre is checked each cycle: defenders gone → take it.

### Station defence
- **I15 (P0) Defenders chase wings.** Fix: target = hostile ship attacking the station → nearest hostile ship → wing only when no ship threatens; target kept while it is a threat; ships that cannot hurt it hold the line.

### Sabotage
- **I16 (P1) Wing raids 0.** Fix: group B raids with the power rule (wings vs satellites and units at the mine); the blanket "structure near route" block is removed for raids.
- **I17 (P2) Ship raid drags the main force.** Fix: raids are done by spare ships and group B, nearest mine.

### Other
- **I18 (P2)** After the station dies every zone counts as enemy side. Fix: own side from the home zone.
- **I19 (P2)** Backup station bought 750 s (20:45), not alive at 918 s. Not analysed.

## Implementation steps
- [x] **A1 Wings.** `EnemySquadronCommander`: station wings free; groups A/B (`EnemyWingGroup`: members, zone, role); opening split; in-zone site choice (free first, base-side first); no escorts from A/B; capture hold order; group B raids. `EnemySquadronCaptureAssignments` → per-group site choice. `EnemyPatrolSafety`: raid rule.
- [x] **A2 Opening production.** `EnemyProductionStrategy` / `EnemyProductionDecisionModel`: fighter + interceptor first; satellite and mine reserve before ships; corvette rule (level 1 only, max 2, not before 30 s); single-ship deployment until the first outer zone (`EnemyFactionController`).
- [x] **A3 Ships.** `EnemyZoneAllocator`, `EnemyShipTaskGroupPlanner`: no idle reserve; rules 1–3; strongest ship → centre; centre priority factor with own satellites; delete route deny; pressure pause only when critical; local-fight release. `EnemyZoneObjectiveCollector`: defender reach.
- [x] **A4 Weak phase loop.** Planner state: target zone, retreat, switch zone; reinforcement rally at the other zone; permanent patrol; raids.
- [x] **A5 Hostile relay order (I14).** Partial, see Implementation notes.
- [x] **A6 Station defence (I15).** `BuildBaseThreat`, `ExecuteDefense`.
- [x] **A7 Tests (`EnemyAi`).** Station wings ordered at launch; opening split 1–2 / rest; wing stays in zone and switches pad; no escort from groups; no corvette before 30 s or from level 2; free ship gets a task in its cycle; weak fleet prefers an outer zone, centre still listed; own satellites raise the centre verdict; force stays while its local fight is won; defence target prefers ships and is kept. No stat-pinning. Written, not run (local override).
- [x] **A8 Battle.** Runs 22:29, 23:11, 09:22 played (CIS vs Republic); Rebellion vs Empire → user acceptance queue.
- [x] **I18 / I19.** I18 fixed in round 4; I19 resolved by the current rule (battle 09:22).

## Implementation notes (2026-11-19, A1–A7)
### Wings
- Station wings: `TrackOwnedSquadrons` → own station nearer than every own ship → tracked as free (`CommandedSquadron` without orders). Else carrier wing (stays on `Guard`). No station → carrier wing.
- Groups: `EnemyWingGroup` + `EnemyWingRole` (`Centre` = A, `Outer` = B). New free wing → B until B has `_outerWingCount` non-bomber wings; bombers always → B; rest → A. Lost wing → next free wing fills the gap.
- `_outerWingCount`: `EnemyFactionController.Initialize` → `SetOuterWingCount(2)` when a Fighter/Interceptor wing costs ≤ 300 (`CHEAP_WING_PRICE`), else 1 (commander default 1).
- Zones: A → `IsMain` zone. B → nearest outer zone (to own station) with any capture target; none left → B raids.
- In-zone choice: `EnemySquadronCaptureAssignments.AssignInZone` → keep own free site; else free site with fewest wings; order pads → (A: mine → relay; B: relay → mine); base-side first. Contested sites skipped; no free site → bait at zone edge toward home; nothing left → hold at centre.
- Capture hold: within `CAPTURE_HOLD_DISTANCE` 400 of a site → `IMoveFacade.MoveTo`, else `AttackMoveTo`.
- Escorts: only carrier wings (`IsHangarLaunched`, ungrouped). Group wings never escort.
- Raids: B members once no outer zone is left; B bombers any time. Power rule `WinsRaid` (wing vs units + armed structures at the mine ≥ 1). `EnemyPatrolSafety` no longer gates wing raids (file unchanged).
### Production
- Level-1 corvette: only at level 1, from `OPENING_NO_CORVETTE_SECONDS` 30 s, max `MAX_LEVEL1_CORVETTES` 2. `CanOrderAnyShip` applies the same rule → before 30 s the squadron fallback buys wings.
- Ships (not wings) wait while `PriorityConstructionPending` (mandatory satellite or mine on an owned site), even with no other production.
- `TrySelectSquadron`: no hero wings; bombers last until a hostile mine is known; fewest reserved copies first (two types side by side), then cheapest.
- Deployment: ships deploy one by one while the team holds no outer zone (`IEnemyAiStateProvider.HoldsOuterZone`, default true).
### Ships
- `EnemyZoneAllocator`: sent force keeps its zone (no retain-line release); leftovers join the nearest force out; no force out → all to `WeakTarget`. Centre force led by the most durable free ship (hull + shields).
- `EnemyZoneObjective.OwnSupport`: own satellites/platforms within 2 × radius; `EnemyZoneEvaluator.Assess` adds them to the force.
- Tier: centre Assemble behind = 4 unless `StrongEconomy` (outer zone + own mine) → 3; centre Go behind = 2.
- Collector: `hostileZoneOnRoute` deleted (no waypoint added; ship navigation paths around). Zone defenders = units within radius + `ZONE_DEFENDER_MARGIN` 500.
- Planner: new force pause only when `StationCritical`. A group sent by the allocator (`AssignedZoneId`) commits to its zone: capture `viable` regardless of verdict; its local fight decides withdrawal.
- Weak loop: zone group in `RetreatValue` → zone marked for `WEAK_ZONE_SWITCH_SECONDS` 90 s, its ships freed; `WeakTarget` = best outer zone by pool advantage, never the centre or the marked zone; reinforcements gather at its staging point.
- Patrol: unchanged (`EnemyPatrolShipController` keeps one patrol ship).
### A5 (partial)
- Inside a hostile zone the relay ranks before pads (`HOSTILE_RELAY_FIRST_BONUS` 1000). Satellites are fought by the attack-move on the way; no explicit satellite-first target.
### Station defence
- `BuildBaseThreat` rank: ship attacking station 0 → ship 1 → wing 2. Previous target (`_defenseTargetId`) kept while it is a threat and a ship (or while no ship threatens).

## Verification (2026-11-19)
- Unity recompile: 0 errors (`Assembly-CSharp`, `Assembly-CSharp-Editor`).
- `EnemyAi` EditMode (user request, 2026-11-19): 533/542 → 9 failures fixed → **542/542 pass** (quick run, explicit tests excluded). Fixes: outer wing count pushed on first `EnemyFactionController.Tick` (not `Initialize`); test catalogs given squadron profiles; evaluator test updated to the new centre rule; production snapshot treats missing capture wings as orderable fleet before 30 s (else income research came before the opening wings). Updated: `EnemyZoneAllocatorTests`, `EnemyUnitCommanderTests` (allocation, scout, raid, groups, station wings, escorts, station defence), `EnemySquadronCaptureAssignmentsTests` (`AssignInZone` ×3), `EnemyProductionStrategyTests` (opening, level-1 corvette). Reviewed, no change: `EnemyDecisionSchedulingTests` (constructor unchanged).
- Reusable guidance → [[Architecture/Enemy_AI_Objectives#Opening and wing groups (2026-11-19, A1–A7) — current]].

## Evidence — battle 2026-11-19 22:29 (A8 run 1; UltraHard Small 1v1, code `70a3e0bb5` dirty) — user: opening better, then broken
Files: `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/Loading_20261119_222941_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_222944_map.csv`, `Battle_20261119_223051.csv`.
- AI Separatist (1015, −1086) vs human Republic (−1015, 1086). Coruscant, seed −94196682. Log 10.9 s = battle 0 s. Recorded 286 s.
- Outer zones: SW (−1555, −1458), NE (1555, 1458); both ≈ 2,600 from the AI station.

### Worked
- Station wings ordered at launch (first order ≤ 6 s). Centre: pads 4 / 9 / 5 / 8 at 28–40 s, relay 37 s, 3 satellites by 39 s. SW zone: pads 51–52 s, relay 59 s, mine 64 s.
- No corvette before 30 s; none from level 2. `escorts` 0–3.

### Faults
- **R1 (P0) Centre mines never taken.** Sites 0–3 `got0` on all 48 `wingAssignment` rows. From 38 s each mine shows `defenders1+` → "contested" → skipped. Human took them at 45 / 76 / 150 / 175 s. AI first centre mine at 168 s (a ship).
  - Wings went one per pad, never onto a mine spot (wrong radii, see Capture check).
- **R2 (P0) Wing swarm stands still.** 12–27 wings circled at the bait point ≈ (370, −400) from 43 s to 250 s, ≈ 250 from the free mine at (213, −166). Rule: "every site contested → wait at the zone edge". With 23 wings vs 5–16 defenders nobody attacked.
- **R3 (P0) Wing overbuy.** 24 wings at 7–29 s (12 Vulture + 12 Belbullab, 6,000 credits), 12 more at 62–86 s (9 bombers) → 36 bought + 5 station wings. Cause: `TrySelectSquadron(ignoreShipRatio: !canOrderShips)` → no cap while ships are forbidden.
  - Effects: first ship deployed 64 s; mine construction `started=False` × 9 (66–96 s, no money); level 3 bought 118 s; first frigate 154 s.
- **R4 (P0) Second outer zone not visited at the start.** Group B took the SW zone, then moved to the NE zone at 74 s. Human took the NE pads 110 s, relay 117 s.
- **R5 (P0) Group B order swapping.** `groupBZone` flipped SW ↔ NE at 74 / 110 / 128 s; 10–11 wings sat on the NE relay point 128–182 s; at 200 s `defenders3` → all recalled. Hostile mine 14 never targeted; `raids=0` on all rows. Turbolaser satellites rated as a flat 60 DPS against wings.
- **R6 (P0) Held centre gets no ships.** AI owned the centre relay 37–273 s with 3 satellites. `zoneAlloc zone:0 ships0` on all 68 rows (`defenders 5–16, statics 2–5`). All 3–10 ships went to the NE zone. Centre lost 273 s.
  - Cause: the centre is rated as an attack on its remaining mines, not as an own zone under threat; own wings in the zone are not counted.
- **R7 (P0) Ships deploy at home only.** All 14 deployments at the station (`tier=A`), rally (674, −721), while the AI held the centre relay and the SW relay. Then a 2,600 march past the centre; human ships intercepted.
- **R8 (P1) NE zone attack.** 3–10 ships there 119–286 s; relay flipped 227 s, lost 263 s; objective switched relay ↔ mine ↔ pads; satellites not targeted first (A5 gap).
- **R9 (P1) Ships late.** Follows R3.

## Round 2 — steps (B1–B6, B8, B9 done 2026-11-19; B7 and B10 open)
### User rules added 2026-11-19 22:40
- Wings fly **onto mining spots** in the centre: mine + nearby pads are captured together. Build mine or battle asteroid at once.
- Both outer zones are visited at the start.
- A swarm never stands still: capture every mining spot, build, attack.
- A raid finishes the job. In a zone with turbolaser satellites, bombers hit the mine first. No retreat of part of the group; no order swapping.
- A held zone is defended: support comes to fight around the satellites.
- Reinforcements spawn **inside held zones**.
- Ship routes do not skirt the enemy-held centre.

- [x] **B1 Wing capture by mining spot (R1, R2).**
  - Group A target = a centre **mine spot**; the whole group (or half per spot when ≥ 6 wings) holds inside its 60 circle until the mine and the pads within 500 flip, then the next spot, base-side first.
  - Delete: one wing per pad, `CAPTURE_HOLD_DISTANCE`, the bait point, "contested → skip".
  - Contested spot: group advantage vs units within 500 ≥ 1 → fight there (attack-move onto the spot); else next spot; no spot → the pad nearest home. Never idle.
- [x] **B2 Wing budget (R3, R9).** Opening purchase cap: free wings (station + bought) ≤ 6 for cheap wings, ≤ 4 otherwise, until the first ship is deployed. Bombers ≤ 3 per known hostile mine. Remove the uncapped `ignoreShipRatio` path. Mines and satellites are paid before extra wings.
- [x] **B3 Outer zones at the start (R4).** One B group per outer zone: 1 wing each from the first decision (2 to the own-side or nearer zone when wings are cheap). Each lands on the zone's mine spot.
- [x] **B4 Group commitment (R5).** Commitment, satellite threat by type and bomber raid order done (see Round 2 notes). A group keeps its zone until it is taken or the group is below 50 % of its wings. No per-pass zone switch. Raid target order inside a zone: mine → satellites → relay for bombers. Satellite threat vs wings by type (turbolaser low, quad laser high) instead of flat 60.
- [x] **B5 Defend held zones (R6).** A zone where the team holds the relay or a satellite and hostiles are present = top tier. Force rated with own satellites **and own wings in the zone**. Ships are sent even below an even fight while own satellites stand (fight around them).
- [x] **B6 Deploy in held zones (R7).** `EnemyFactionController.TryFindShipSpawnPosition`: spawn in the held zone nearest the ship's task (centre for the centre force, outer relay for that zone) when its spawn rule is open; else home. Check `ReinforcementSpawnRule.IsOpen` for AI-held capturable zones.
- [x] **B7 Route (R7).** Waypoint around a hostile-held centre for ship groups (round 4).
- [x] **B8 Hostile zone order (R8, A5 gap).** Satellites → relay → pads for ships.
- [x] **B9 Tests (`EnemyAi`).** Written, not run (local override); spawn-point and own-wing support covered only at allocator level. Group lands on a mine spot; never idle with a free spot in the zone; purchase cap; one group per outer zone; group keeps its zone across passes; held zone under attack is top tier and counts wings; spawn point picks a held zone.
- [x] **B10 Battle.** Runs 23:11 and 09:22 analysed (rounds 3 and 4); next run → user acceptance queue. Same matchup. Gates: all 4 centre mines AI-owned or contested by 60 s; no wing circling > 15 s with a free site in its zone; wings bought ≤ cap before the first ship; first ship ≤ 45 s; NE and SW zones both visited by 40 s; ≥ 50 % of ships at the centre while it is held and attacked; ≥ 1 deployment outside the home zone once a relay is held.

## Round 2 — implementation notes (2026-11-19)
### Wings (B1, B3, B4)
- `EnemySiteObjectives.AddSquadronTargets` → `AddMiningSpot` for every mining site (any owner); pads carry their capture radius.
- `EnemySquadronCaptureAssignments.AssignInZone(wings, centre, reach, home, addWing)`: spot with work (mine target or pad whose radius covers the spot), base-side first. ≥ `SPLIT_GROUP_WINGS` 6 wings + ≥ 2 spots → halves on the two best spots.
- Uncontested spot → hold (`TryGetHold`): attack-move, then `MoveTo` within `HOLD_ENTRY_SCALE` 2 × spot radius. Contested → attack-move only when the part's advantage ≥ 1, else next spot.
- No spot → free site nearest home → contested site nearest home → zone centre. Deleted: bait point, one wing per pad, `CAPTURE_HOLD_DISTANCE`, `IsSite`, relay/mine order.
- `EnemySquadronCommander._outerGroups`: one `EnemyWingGroup` per outer zone (`HomeZone`), nearest home first. Fill: 1 wing per outer group, then group 0 up to `_outerWingCount`; rest → A. Bombers → outer group 0.
- An outer group whose zones are all taken keeps its quota and raids (fix after the 23:31 test run).
- Commitment: zone kept while it has targets and capturers ≥ `Peak` / 2. Taken → next outer zone with targets (unworked first); weak → nearest winnable zone, else stays. `Peak` resets on change.
- Raids: outer groups without a zone with all wings; bombers any time. Telemetry `groupB=n@(x z)|n@raid`, `spots=`.
### Production (B2)
- `EnemyWingPricing`: `CHEAP_WING_PRICE` 300, opening cap 6 cheap / 4 otherwise. `HasSquadronRoom`: ship count 0 → opening cap (station + hangar wings count). `ignoreShipRatio` removed.
- Bombers ≤ `BOMBERS_PER_HOSTILE_MINE` 3 × known hostile mines.
- `PriorityConstructionPending` blocks ships and every wing except missing capture wings.
- "First ship deployed" approximated as first ship **bought** (reserved count > 0).
### Ships (B5, B6)
- `EnemyZoneObjective.IsHeld` (relay allied or own satellite/platform), `OwnStructures`, `IsHeldUnderAttack`. `OwnSupport` += own wings within 2 × radius.
- `GetTier`: held under attack → −1 whatever the verdict. `TryFill`: held under attack with `OwnStructures > 0` → all free ships committed below even.
- `EnemyZoneAllocator.DeployZone` = gather zone → best served zone → weak target. Planner `DeploymentPoint` = its centre; `TryGetReinforcementRally` prefers it over `GatherPoint` → `TryGetSpawnPositionNear` picks the held relay or station nearest it (spawn rule must be open).
### B4 satellite types, B8 (2026-11-19, follow-up)
- `IDefendPlatformTypeFacade` (registered in `DefendPlatformInstaller`) → `UnitCombatProfileCatalog.GetPlatform(type)` from `DefendPlatformData.WeaponLoadout` + damage matrix → `ForceCompositionBuilder.AddStructure`. `STATIC_DEFENSE_DPS` 60 and its flat profile deleted; `AddArmedStructures` takes the builder.
- Bombers: mines first; idle bombers of an outer group then go **together** after the visible hostile satellite nearest them (inside the group's zone while it has one) when together advantage ≥ 1. Relay after that through normal group capture.
- Ships: `EnemySiteObjectives` adds a known hostile satellite on a pad as a blocking-facility candidate (seen once → remembered while standing; matched by `FacilityPosition`). Ranking: `HOSTILE_SATELLITE_FIRST_BONUS` 2000 > relay 1000 → satellites → relay → pads. Executor attacks it through the existing blocking-facility branch.
- Tests: `ArmedStructures_AreRatedByTheirPlatformTypesWeapons`, `SquadronRaid_BombersWithoutAMineHitTheSatelliteOfTheirZoneWhenTheyBeatIt` (×2), `SiteObjectives_HostileSatelliteBlocksItsPad`; static-defense raid test seeds a turbolaser profile. Ranking bonus not unit-tested. Compile 0 errors; not run.
### Open
- **B7 route waypoint** not started.
- A held zone with no candidate site left gives its zone group nothing to capture (not verified).
### Verification
- Unity recompile: 0 errors. Tests not run (local override).
- Updated: `EnemySquadronCaptureAssignmentsTests` (5 `AssignInZone` tests replace 3), `EnemyUnitCommanderTests` (raid, group fill, commitment), `EnemyZoneAllocatorTests` (+3), `EnemyProductionDecisionModelTests` (opening cap step). Reviewed, no change: rally test (no allocation → `GatherPoint` path), `OwnedSiteDefense_*` (data has Mining settings).

## Evidence — battle 2026-11-19 23:11 (B10 run 1; UltraHard Small 1v1, code `4a6ccd830` dirty, 936 s)
Files: `%USERPROFILE%/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/Loading_20261119_231116_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261119_231121_map.csv`, `Battle_20261119_231230.csv`. Log 14.5 s = battle 0 s.
- AI Separatist (1204, 1325) vs human Republic (−1204, −1325). Coruscant, seed −123418381. Outer zones NW (−1556, 1477), SE (1556, −1477).
- **User verdict:** best battle so far. The AI sabotages and retakes mines and holds one outer mine. Dozens of issues remain, but the dynamics are better.
- Result: AI lost at 936 s. Kills Republic → Separatist 186, Separatist → Republic 109.

### Worked
- Opening: home satellite at 8 s; centre satellites at 38 / 45 / 51 s, mines 0 / 1 at 52 / 61 s, centre relay at 57 s; NW zone 2 satellites + mine by 64 s.
- Wing cap: 6 wings bought by 15 s, none extra before the first ships. Corvettes only at 30 / 31 s (2).
- NW outer zone and its mine held from 64 s to 904 s. Centre mines changed hands 8 times: retaken at 123 / 157 / 188 s.
- B8: human satellite 15 destroyed twice (347 s, 466 s).

### Faults (round 3)
- **C1 (P0) Bombers never raid.** `raids=0` on almost all rows; 20 Hyena bombers bought, 27 bomber wings lost. Cause: `WinsRaid` rates **one** bomber against the mine and its satellites.
- **C2 (P0) Both outer groups on one zone; the held zone is left bare.** From 98 s both groups work SE; NW (held) gets no wings; human retakes NW at 885–932 s unopposed. Cause: a group whose zone is taken may share a zone another group works.
- **C3 (P0) Fleet stalls at a zone it cannot take.** 7–12 ships at SE for 262–622 s vs 15–17 defenders (`fleetAdv` 0.4–0.6); no site flipped (human rebuilt satellite 15 within 1 s each time). Then the human army hit the station (645 s); station lost 760 s. Cause: an allocator-sent group commits regardless of verdict; its local fight is never lost (`localRaw` 100).
- **C4 (P1) Centre lost at 162–168 s, never retaken.** `gather=zone:0` with 0 ships for 262–622 s; all ships go to SE.
- **C5 (P1) Money banked during the station siege.** 11–20k credits at 690–766 s (`saving Lucrehulk` × 153, `critical … etaSeconds=Infinity` × 65); 5 `deploymentBlocked` at home.
- **C6 (P1) Patrol ship never patrols.** `patrolShip assigned → released reason=noSafeTarget` every 10 s from 101 s.
- **C7 (P2)** Late centre platform: `defensePlacementBlocked` 78 / 96 candidates blocked (842–933 s).

## Round 3 — steps
- [x] **C1** Raiders of an outer group (bombers, or all wings without a zone) are rated together vs the mine and its cover and go to one mine together; one group per mine (`AssignMineRaid`).
- [x] **C2** Outer group: home zone held (own mine/satellite, no target left) and attacked → defend it, wherever the group works. Zone taken → nearest outer zone with targets **no other group works**; none → raid. `SelectWinnableZone` also skips worked zones.
- [x] **C3** `EnemyShipTaskGroupPlanner.NoteStalledSieges`: a zone force below an even fight (`Assess` not `Go`) for `STALLED_SIEGE_SECONDS` 90 s → zone marked like a retreat (90 s weak-phase switch). Held zones under attack exempt. "No site flipped" not tracked; the verdict alone decides.
- [x] **C4–C7** Resolved in round 4 (see "Round 4 — steps").
- [x] **Tests (`EnemyAi`)**: `SquadronRaid_BombersRaidAGuardedMineOnlyTogether` (×2), `WingGroups_HeldHomeZoneUnderAttackIsDefended_ElseTheGroupNeverSharesAZone` (×2), `Allocation_ForceStalledBelowAnEvenFightIsReleasedAfterNinetySeconds`; updated `SquadronRaid_OuterGroupsRaidTogether_OneMinePerGroup` (3 raids / 1 mine for one group) and the commitment test (taken zone → raid, not share).
- Verification 2026-11-20: compile 0 errors; `EnemyAi` EditMode **557/557 pass** (quick, explicit excluded; user request). First run 555/557: the updated raid test lacked wing entities and catalog profile; fixed in the test.

## Evidence — battle 2026-11-20 09:22 (B10 run 2; UltraHard Small 1v1, code `ddf398f45` dirty, ~1250 s)
Files: `…/BattleTelemetry/Loading_20261120_092240_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261120_092242_map.csv`, `Battle_20261120_092342.csv`. Log 17.4 s = battle 0 s.
- AI Separatist (1237, −1232) vs human Republic (−1237, 1232). Coruscant, seed 367802206. Outer zones SW (−1578, −1331), NE (1578, 1331).
- **User verdict:** same level as the last battle; the centre capture at the start was much better.

### User notes (anchors) → log check
- **2:48** "Enemy has the zone + mines like me, 3 satellites vs my 2, yet does nothing with ships; it must build level-2 frigates/cruisers to control the zone and kill my ships, satellites and mines."
  - Confirmed: AI centre relay 0:43–5:33, satellites 7 / 8 / 9 / 6 by 0:55, mines 3 / 2 / 0. Ships at the centre 1 → 5 from 0:45 to 3:12 vs 6–12 defenders; verdict Assemble (0.92) at 3:02. Level 2 bought 1:27; first Wavecrest 1:36, then one ship every 10–15 s.
- **"Sent one Wavecrest, then more — while owning the zone, why not spawn at the centre relay?"**
  - Confirmed: every deployment 2:51–9:55 had `rally=(0,0)` (the centre) but spawned beside the station (tier A). Likely, not verified: human ships/satellites inside the 900 relay circle block spawning there (`BattleAreaLayer.SpawnBlock`; satellite block 90, XQ6 500).
- **"XQ6 platforms: 4 XQ6 + 2 turbolasers kill the first Wavecrest, then the Munificent; then a Recusant did the job."**
  - Confirmed: SW zone 2:31 `verdict=Go; statics=0` with 1 ship → 4:26 `statics=6; advantage=1.00`. Statics count only while visible, so the zone was rated empty. Recusant took satellite 15 at 5:42 and the SW relay at 5:59.
- **11:38** "Bad sabotage with a single Hyena bomber; waves of small or single ships attacking and dying, many capitals lost. Did it start a station?"
  - Confirmed: `raids=0` 7:03–20:00; the raid groups had no zone and no winnable mine, so their wings were spread **one per target** (`captureWings` 3–5). SecondStation bought 10:31 (placed 12:01 at (−1667, −1667)). Ships: 5–9 at the centre vs 17–28 defenders 5:50–9:53; verdict flickered 0.98 ↔ 1.02, so the C3 stall timer reset each time. Reserve 3–6 gathered at (883, −879) 10:15–13:32 while new ships arrived in pairs.
- **After the station died** "it attacked with everything for 2–3 min instead of waiting, building defence and holding reinforcements for the siege; lost all ships — no grouped armada."
  - Confirmed: main station destroyed 16:45. 16:39 12 ships → centre (1 known defender) → retreat 16:46. From 17:21 ships deployed in ones and twos at the second station and went straight out; new groups 10–13 each `EngageLocal standAndFight` at 0.27–0.63.

### Other findings
- C1 worked: `raids` 2–7 from 3:32 to 6:21. C2 worked: NE taken by 0:57, its group raided.
- Money: 20,167 saved for Invincible (11:48), 18,123 for RecusantDreadnought (12:35), 21,172 Invincible bought 16:35 while the main station was dying (C5 again).
- Patrol: released `noSafeTarget` from 3:01 to the end (C6 again).
- `defensePlacementBlocked` at the SW zone: 55 / 96 blocked (6:04).
- I19: the backup station was bought 10:31 and outlived the main station (16:45 → 20:01). Resolved by the current rule.

## Round 4 — steps
- [x] **D1 No drip-feed.** `EnemyZoneAllocator.AssignLeftovers`: a free ship joins a served zone only if the force with it is a `Go` (`WinsWith`); else it stays in the reserve (gathers). Held-zone defence below even only while force + support advantage ≥ `DEFEND_FLOOR` 0.6. Weak-phase target only while the pool reaches `WEAK_ATTACK_FLOOR` 0.6 (`SelectWeakTarget`).
- [x] **D2** Stall clock resets only at advantage ≥ `STALL_CLEAR_ADVANTAGE` 1.25.
- [x] **D3** `HostileIntelModel.RememberStructure` / `IsStructureKnown`: `AddArmedStructures` counts a known standing satellite/platform out of sight. Collector takes the team intel.
- [x] **D4** `NoteStationLoss`: own station count drops → `TURTLE_SECONDS` 180 s with every assignment set to reserve, no staging point, no deployment point (ships gather at home). Production wave holding unchanged.
- [x] **D5** `AssignGroup`: a group without zone and raid works group A's zone (`AssignInZone`), never the one-per-target spread.
- [x] **C5** `TrySelectShip(requireAffordable: HasPressure)`: under station pressure buy the best ship affordable now. Recovery mode is not visible to production; not covered.
- [x] **C6** Patrol: no safe point → second pass with `EnemyPatrolSafety.IsReachable` (not blocked, clear, reachable; no threat check) → bait patrol.
- [x] **I18** `IsOwnSide`: with no allied station, the team's fixed (non-capturable) relays stand in.
- [x] **I19** Resolved by the current rule (backup station 10:31, outlived the main station). No change.
- [x] **B7** `EnemyStrategicContextBuilder.RouteAroundHostileCentre`: capture target replaced by a waypoint `2R + 500` beside a hostile-held centre when the straight line passes inside it; target inside the centre or group inside the keep-out → direct.
- [x] **C4** Covered by D1/D2: the reserve now gathers for the gather zone (the centre) instead of feeding it ship by ship. No separate change.
- [x] **C7** (Spawn part corrected 2026-11-20: relay spawns were missed because of F1, not block circles.) No change: `blocked` = hostile `SpawnBlock` circles (game rule shared with players). The same rule explains the missed centre-relay spawns (user note 2:48): hostile ships/satellites inside the relay circle block it.
- Tests (2026-11-20): new `ArmedStructures_SeenOnceStayDefendersOutOfSight`, `WingGroups_ZonelessGroupWithoutARaidWorksTheCentreTogether`, `CaptureRoute_PassesAHostileCentreByAWaypoint`, `Allocate_LeftoverShipJoinsOnlyAForceItKeepsWinning`; updated `Allocate_HeldZoneWithOwnSatellites…` (floor case), `Allocation_ForceStalledBelowAnEvenFightIsReleasedAfterNinetySeconds` (kept force set up directly), `Allocation_PoolFarTooWeakGathers_ReinforcedPoolAttacksTogether` (was "pool too weak attacks": contract changed by D1). Not covered by tests: D4 turtle, C5, C6.
- Verification: compile 0 errors; `EnemyAi` EditMode **561/561** (quick, explicit excluded; user request). First run 559/561 — both from the intended D1 contract change; tests updated.

## Acceptance (telemetry)
- First wing order ≤ 6 s. Wings at the centre ≥ 3 by 40 s; outer-zone group 1–2 wings.
- AI satellites: ≥ 2 at the centre and ≥ 1 at the own-side zone by 60 s when uncontested at arrival.
- No corvette purchase before 30 s; none at level ≥ 2.
- `wingAssignment escorts` = 0 for group wings; `raids > 0` once a hostile mine is known.
- Every deployed ship has a non-guard task within 5 s.
- Weak phase: ≥ 1 outer-zone attack per 120 s; target switches after a retreat; patrol never empty.
- Siege: attack orders at squadrons < 20 %; ≤ 4 re-targets per ship per minute.

## Fixed already (2026-11-19, uncommitted)
- [x] Emergency corvette fallback deleted; no level-1 corvette from level 3; critical station saves for the best ship in time. Compiled; tests not run. To tighten in A2: level 2 → 0, not before 30 s.

## Decisions (user, 2026-11-19)
- Outer-zone wing count: 2 when a fighter wing costs ≤ 300 credits (Vulture 200), otherwise 1.
- Interceptor not unlocked at level 1 → buy a second fighter type instead of waiting.


## Evidence — battle 2026-11-20 13:11 (round-4 gate; Small 1v1, code `b07afa568` dirty, 24:12)
Files: `…/BattleTelemetry/Loading_20261120_131143_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261120_131145_map.csv`, `Battle_20261120_131254.csv`. Log 24.9 s = battle 0 s.
- AI Separatist (1293, 1283) vs human Republic (−1293, −1283). Coruscant, seed −906205694. Start money 10,000. Outer zones SE (1531, −1552), NW (−1531, 1552).
- Result: AI main station lost 18:59, second station 22:27. Losses: AI 84 ships + 164 wings; human 36 ships + 121 wings. AI spent 377,500 on ships.

### Worked
- First wing order 0:01; 6 wings by 0:15; corvettes only 0:30 / 0:31.
- Centre: pads 0:37–0:47, relay 0:48, 4 satellites by 0:48, mines 0:48 / 0:57. NW zone: satellite 0:57, relay 1:04, mine 1:05.
- Levels bought ≤ 5 s after they opened (1:27, 3:35, 6:42). Second station bought 10:22, took over 19:00.

### Faults
- **F1 (P0) Every ship spawns at a station.** 88 / 88 deployments at home (or the second station); 15 of them while the AI held the centre relay (0:48–4:34); NW relay held 1:04–21:58.
  - Cause (source): `EnemyFactionController.cs:504` → `hasRear` = station alive ∧ `TryGetRearDeploymentThreats`, which returns true whenever a centre zone exists → `useRearAnchor` always → `TryGetRearSpawnPosition(station)`. `TryGetSpawnPositionNear(rally)` (B6) is never reached.
- **F2 (P0) Ships die in transit, in pairs.** Median life ≈ 80 s. Deaths: 50 inside the centre zone, 29 at home, 5 NW, 4 transit. 1 of 88 came within 700 of the SE zone.
  - 42 ships got `Attack` on SE structures 113–115 (2:42–17:14); the 2 satellites stood all battle (`statics2`).
  - `zoneAlloc`: SE rated `defenders4 statics2` → any pair is a `Go`; `reserve=0` from 0:45 to 15:39. 20 human ships at the centre (minutes 8–16) not rated.
  - B7 waypoint covers `captureTarget` only (`EnemyStrategicContextBuilder.cs:873`), not the blocking-facility `Attack`.
- **F3 (P0) Centre held, not used.** Held 0:48–4:34 with 4 satellites; ships there 1–2 until 2:41, 4 at 3:08 (3 went SE). Group A (8–10 wings) sat on one contested pad 1:26–4:14 (`spots=` empty) while mines 2 / 3 were hostile or free. `siteBuild` = mine only; no battle asteroid built. From 8:05 `zone:0 ships0`; retaken 19:45.
- **F4 (P0) No site changed hands 8:42–19:28.** Two ships bought every ≈ 20 s through it.
- **F5 (P1) Turtle leaks (D4).** Station lost 18:59 → 19:27 ship 267 `Attack` on the SE mine (4,292 away), 19:45 `AttackMove` to the centre; 3 ships died there 20:32–21:08. `EnemyShipTaskGroupPlanner.cs:147` resets the allocator only; the reserve group's own states (`RaidStructures`, `Consolidate`) still send it out.
- **F6 (P1) Bombers do not raid.** 26 Hyena wings bought 4:28–10:18; `raids>0` on 4 of 138 rows (4:28–19:00). Raid groups sat on centre pad 5 vs 16–41 defenders (D5 fallback). 42 Hyena wings lost.
- **F7 (P1) Patrol empty 4:20 → end.** Patrol ship (C-9979) died, `released reason=unavailable`, never replaced.
- **F8 (P1) One wing fed to the SE zone all battle** (`defenders 2–4`); group never switched.
- **F9 (P2) Satellite churn.** 29 satellites / 26,500 credits after 19:20; pads 5 and 9 flipped ≈ every 10 s with 2–5 ships alive.
- **F10 (P2) Order churn.** Median 10.5 orders per ship-minute (gate ≤ 4). 24 ships got `Attack` → `None` loops (81 cancels). 18 / 88 ships idle > 30 s after spawn.
- **Not a fault (user):** saving 99 s for an Invincible (12:18–13:57) under siege is acceptable. C5 stays as is.

### Economy (measured, start 10,000)
- Income ≈ 270 /s at 1:30, ≈ 400 /s from 2:00 to 19:00. Spend 20–24k per minute, almost all ships.
- `production-wait` on 1,179 of ≈ 1,450 s: the AI always had a next buy and waited for money.
- Opening bill, first 60 s: research 1,200 + miners 5,000 + level 2,000 + wings 1,500 + corvettes 2,700 + satellites 2,300 = 14,700 (+ mines) vs 10,000 start.
- Mandatory misses: pad 9 owned 27 s without a satellite (0:56); pad 6 110 s (2:31) while 21,100 went to ships that minute.

## User notes (2026-11-20, after 13:11)
- **Top priority 1:** the AI must spawn ships at held zones, and in open space near its own wings (their fog vision allows it). Not all ships: about 80 %, by ship size.
- F2 and F3 follow from F1: ships arrive at home instead of at the zone.
- Held centre: use the wings already there to capture the remaining mining spots. Build a **battle asteroid** when the zone is lost, about to be lost, or at the start: more defence firepower.
- Patrol replacement is a **wing**, not a corvette.
- **Top priority 2:** economy. Big maps and 2 AIs do better: more money, more time. Money is the main reason.
  - Find the moments the AI cannot pay a mandatory action; raise the min and max starting money from that.
  - Small income bonus from the second station.
  - Revisit income per station level, per research, per mine: a side with 1–2 outer zones and no centre can still buy the mandatory power; the side with more mines buys more units, but the gap is not big.
  - Today even centre (4 mines) + 5 miners + home mine + 1 outer mine is not enough at levels 1–4.
  - Fixes expensive wings at the start too.
- All other faults matter too.

## User decisions (2026-11-20, second pass)
- **Rear spawn stays** for a station under attack or siege: ships arrive behind, out of reach of the whole armada, and can fire from range. One ship at a time is then safe.
- **E1 forward spawn: agreed.** Every ship size may spawn at any forward point. No size limit.
- **Spawn beacon:** a patrol wing finds the enemy, pulls back to a point outside relay and station block circles, stops, and the AI spawns ships there. Smallest ships first: easy to place, and each one widens fog vision.
  - If that does not work: raise fog vision range of units (all, if needed).
- **E3 held centre: accepted.**
- **"Min / max money"** = the skirmish starting-credits slider.
- **Invincible saved under siege:** fine. Not a fault.
- **Patrol replacement:** a wing, never a corvette.
- **Abilities:** one auto-ability system. The player's auto-use also drives the AI.
- **Shield ability:** fires when shields < 40 % and an enemy ship is nearby.
- **Process:** implement all of round 5 first. Then one user playtest. No battle between steps.
- Third pass: E2 income values accepted (station 30 per level, miner 30, second station +50 %).
- Third pass: forward spawn must be cheap and simple: one call that takes units and returns a point; looser spawn rules.
- Third pass: shield ability keeps the old rule and adds the new one; the new one has priority.

## Source checks (2026-11-20)
### Can a dreadnought spawn near a wing?
- `ReinforcementSpawnRule.CanSpawnShip` = in bounds ∧ **centre point** visible ∧ no hazard ∧ not in a hostile block circle ∧ hull box free (`Physics.CheckBox`) ∧ reachable from the station.
- Vision is tested at the centre point only → hull size does not matter for fog. A wing's 350 circle is enough for any ship.
- Hull size matters only for the box check: rocks, structures, other hulls, reserved landings.
- Only structures carry `SpawnBlockRadius` (satellite 90, mine 160, platforms up to 500, relay 900). Ships and wings block nothing.

### Retry cost
- `ShipSpawnPoints.TryFindOpenPosition`: 100 random tries per anchor. Each try = vision lookup + physics box + navigation reachability.
- `EnemyFactionController` retries a blocked ship every 1 s (`DEPLOYMENT_RETRY_INTERVAL`); tiers B / C / D at 4 / 8 / 12 s.
- With every wing as an anchor: 10 wings × 100 tries × waiting ships, each second → replaced by the fixed 8-point pattern in E1b (≤ 24 points per call).

### Abilities today
- Player: `PlayerAutoPowerService` → roles (`AutoPowerRole`), `AutoPowerCombatSnapshot.Assess`, 2 confirmation checks, 0.5 s interval.
- AI: `EnemyShipAbilityController` → separate rules (`ShipAbilityAiUse`, random `AbilityUseChance`, `RetreatShieldThreshold`). Shares only `IShipAbilityAutoUseRule`.
- Shield role now: shields < 60 % (`lowShields`) ∧ shield damage in the last 3 s. No "enemy ship nearby" test.

## Open items from rounds 1–4 → resolution
| Item | Resolution |
|---|---|
| A5 "partial": no satellite-first target | Closed by B8 (round 2). |
| Risk: held zone with no candidate site → zone group has no task | E3c. |
| B2: "first ship deployed" counted as first ship bought | Final rule: the wing cap lifts at the first ship purchase. |
| C3: site flips not tracked, verdict alone decides | E4c. |
| C4 "covered by D1 / D2" | Not confirmed by 13:11 (centre got 0 ships 8:05–19:45) → E1 + E4. |
| C5: recovery mode not visible to production | Dropped (user: saving for a capital is fine). |
| C7 and the 09:22 note: hostile block circles stop relay spawns | Wrong. Cause = F1 (`hasRear` always true) → E1. |
| Tests missing: D4 turtle, C6 patrol, B8 ranking bonus, B6 spawn point | E12. |
| Gates A8 / B10 and the old Acceptance section | Replaced by "Round 5 playtest gates". |
| Rebellion vs Empire battle | Part of the round-5 playtest. |

## Round 5 — plan (2026-11-20; do not start before the user says so)
Order: E1 → E2 → E3 → E4 → E5–E11 → E12. Playtest once, after E12.

### E1 (P0) Spawn modes (simplified 2026-11-20, third pass)
- Decision: one cheap call that takes units and returns a point. Fixed pattern, no random search, no cooldowns, no retry timers.
- Why: the first draft (100 random tries per anchor, caps, 5 s skips, 8 s waits) was heavy and complex.
- Avoid: per-point navigation checks, per-point threat checks, a new anchor registry.
- [x] **E1a Mode.** `EnemyFactionController` ship deployment:
  - Rear mode (unchanged, `TryGetRearSpawnPosition` at the station) only when `TryGetReinforcementRally` returns `defendHome = true` (defended station or station pressure).
  - Else forward mode (E1b). Delete the always-true `hasRear` path.
- [x] **E1b API.** `IShipSpawnPoints.TryGetSpawnPositionNearUnits(PlayerId owner, ShipType shipType, IReadOnlyList<IEntity> units, Vector3 awayFrom, out Vector3 position)`.
  - Per unit: 8 fixed points on a ring 250 from the unit, first point straight away from `awayFrom`, then alternating left / right.
  - Per point, cheap first: in bounds → no hazard → centre visible → not in a hostile block circle → hull box free (`IShipSpawnClearance.IsClear`). First point that passes is returned.
  - At most 3 units per call → ≤ 24 points; the box check runs only on points that passed the cheap tests.
- [x] **E1c Looser rules for forward points.** Dropped: navigation reachability (the unit already stands there) and `PlacementThreatSnapshot.IsSafe`. Kept: bounds, hazard, vision, hostile block circle, hull box: the players' rule minus reachability.
  - Accepted risk: a ship lands in a rock pocket it cannot leave. Check `spawnAttempt` rows and ship idle time in the playtest.
- [x] **E1d Caller.** `EnemyFactionController` passes, nearest the ship's task first (`DeploymentPoint`, else `GatherPoint`), up to 3 own units with no known hostile ship within 800: spawn beacon (E1e) → wings → ships → satellites and mines. `awayFrom` = known hostile centroid, else the map centre.
  - No point → same tick: existing held-relay / station search (`TryGetSpawnPositionNear`). No wait.
- [x] **E1e Spawn beacon.** Patrol wing sees a hostile ship → moves back toward home to the first point ≥ 1,000 from every hostile relay, outside every hostile station block circle and ≥ 1,200 from the seen ships → holds. It is passed first in E1d while it lives.
- [x] **E1f Order.** Ships waiting together deploy smallest hull first (`GetPlanarRadius`). A landed ship is a unit for the next call.
- [x] **E1g Telemetry.** `deployment … anchor=beacon|wing|ship|structure|relay|station`; `spawnAttempt units=…;points=…;notVisible=…;blocked=…;notClear=…`.
- Contingency, only after the playtest: capitals fail on > 50 % of `spawnAttempt` rows → raise wing `VisionRange` 350 → 500 and the ring to 350. Not part of this round.

### E2 (P0) Economy (shared by human and AI)
- [x] **E2a** `SkirmishModel`: starting credits min 500 → 5,000, default 2,000 → 12,000, max 10,000 → 30,000. Slider step and saved setups reviewed.
- [x] **E2b** Station income per level 15 → 30 (`FactionService.DEFAULT_INCOME`, `EnemyFactionController.DEFAULT_INCOME`; one shared value).
- [x] **E2c** Miner (`MiningFacilityData` `Income`) 20 → 30. Asteroid mine stays 60. Battle asteroid stays 10.
- [x] **E2d** Second station: +50 % of station income while it lives.
- [x] **E2e** Telemetry: one `economy` row per player every 15 s (money, income, sources).
- Effect at level 3 with Production II: home mine only 272 → 387 /s; home + 1 outer 356 → 471; 6 mines 692 → 807; gap 1.94× → 1.71×.
- Income values accepted by the user 2026-11-20 (third pass).

### E3 (P0) Held centre
- [x] **E3a** Group A: its site is contested and a mining spot with work exists in the zone → ≥ 2 wings to each such spot (existing fight rule: advantage ≥ 1, else next spot). No parking on a contested pad.
- [x] **E3b** `EnemySiteConstructionController`: battle asteroid instead of a mine on a spot when (1) the zone is held and hostiles are in it, or (2) the zone was lost and retaken, or (3) battle time < 120 s and a hostile wing is inside the zone. Else mine.
- [x] **E3c** A zone group whose held zone has no candidate site left guards the zone's nearest own satellite (`Guard`).

### E4 (P0) No lone transit
- [x] **E4a** `EnemyZoneAllocator.WinsWith` / `Assess`: known hostile ships within 800 of the straight line from the force to the zone count as defenders.
- [x] **E4b** Blocking-facility `Attack`: first a `MoveTo` on the `RouteAroundHostileCentre` waypoint when the line passes a hostile-held centre.
- [x] **E4c** Stall by result: a zone force with no site flip at its zone for 120 s → zone marked like a retreat (90 s switch). Held zones under attack exempt.

### E5–E11
- [x] **E5 (P1) Turtle.** While `_turtleUntil` runs, the reserve group may only `DefendBase` or gather at `GatherPoint`. No raid, capture or consolidate order.
- [x] **E6 (P1) Bombers.** A raid group with no winnable mine holds at the centre of the nearest own held zone. The D5 centre fallback stays for fighters and interceptors only.
- [x] **E7 (P1) Patrol.** From level 2 the patrol unit is a fighter or interceptor wing: the spare wing of group A when it has > 4, else one bought outside the wing cap. Corvette patrol only at level 1.
- [x] **E8 (P1) Lone outer wing.** A 1-wing zone group that died twice at its zone within 180 s leaves it: next outer zone with targets, else raid.
- [x] **E9 (P2) Churn.** A pad lost twice within 60 s gets no satellite for 45 s. A ship order is re-issued only when the target id changes or the destination moves > 50.
- [x] **E10 (P1) One auto-ability system.** Extract the decision from `PlayerAutoPowerService` into a per-owner service; `AutoPowerCombatSnapshot` takes the owner instead of the local player. AI ships use it. `EnemyShipAbilityController` keeps only the difficulty gate (`AbilityDecisionInterval`, `AbilityUseChance`) and the `LockSFoils` cancel; its `ShipAbilityAiUse` rules are deleted with the enum when no ability needs them.
- [x] **E11 (P1) Shield rule (both rules, new one first).** Ship can boost ∧ is not boosted, then:
  1. **New, priority:** shields < 40 % (new tuning `shieldBoostBelow` 0.4) ∧ a visible hostile ship (not strikecraft) inside radar range → Shields. Checked first in `ChooseRole`, before last-stand Weapons; fires without the 2-check confirmation.
  2. **Existing, kept:** shields < 60 % (`lowShields`) ∧ shield damage in the last 3 s → Shields, with the normal confirmation.

### E12 Tests (written with each step; run only on the user's request)
- `EnemyAi`: spawn mode (rear vs forward), unit order passed to the call, fallback to relay / station in the same tick, smallest first, beacon; battle asteroid choice; group A split; route defenders; stall by result; turtle orders; bomber hold; patrol wing; lone wing; satellite rebuy wait. Also the gaps from rounds 2–4: C6 patrol bait, B8 ranking order.
- `Ships`: `TryGetSpawnPositionNearUnits`: first point away from the enemy, skips a blocked or unseen point, false when all 24 fail.
- `Abilities`: shared decision for human and AI owners; shield rules (39 % with a ship nearby → yes, at once; 39 % with only wings and no damage → no; 55 % with recent damage → yes after confirmation; 65 % → no).
- `Economy`, `Ui`: income sources, second station, slider limits. No stat-pinning: assert relations (level 2 > level 1, second station > none), not numbers.

### Round 5 playtest gates (user, after E12; UltraHard Small 1v1 CIS vs Republic, then Rebellion vs Empire)
- ≥ 80 % of deployments outside home while no station is under pressure and a relay, beacon or wing is forward.
- 100 % of deployments in rear mode while the station is under pressure.
- No ship dies within 60 s of spawning outside a station defence.
- Held centre: every mining spot AI-owned or fought over within 60 s of the relay capture; ≥ 1 battle asteroid when the centre is attacked.
- No 5-minute window without a site change while the AI has ≥ 6 ships.
- 180 s after a station loss: no ship order other than defend or gather.
- `raids > 0` on ≥ 50 % of rows while a winnable hostile mine is known; no bomber group on a centre pad with > 10 defenders.
- Patrol unit present ≥ 90 % of the battle from level 2.
- Orders ≤ 4 per ship per minute (median).
- AI ships use shield, weapon and target abilities; shield boost only below 40 % with a ship nearby.

## Round 5 — implementation notes (2026-11-20)
### Spawn (E1)
- Rear mode only when `TryGetReinforcementRally` → `defendHome`. The always-true `hasRear` path and `TryGetRearDeploymentThreats` are deleted; `FillDefenseDeploymentThreats` sets the station attacker as the rear approach.
- Forward mode: `EnemyForwardSpawnAnchors.Collect` (beacon → wings → ships → own mines/satellites, nearest the task first, none within 800 of a known hostile ship; ≤ 3) → `IShipSpawnPoints.TryGetSpawnPositionNearUnits` → `ForwardSpawnRing.TryFind` (8 points, ring 250; `IsOpen` then `IShipSpawnClearance.IsClear`; no reachability, no threat test).
- Deviation: the call takes unit positions (`IReadOnlyList<Vector3>`), not `IEntity`.
- No forward point → `TryGetSpawnPositionNear` (relay/station) in the same tick.
- E1f: `EnemyBuildQueue.Advance(shipDeployOrder)` orders ready ships by `GetHullRadius`; ships landed this tick (`_landedShips`) are anchors for the next.
- E1g: `deployment … anchor=beacon|wing|ship|structure|relayOrStation|rear`; `spawnAttempt units;points;notOpen;notClear;found;anchor`. Deviation: `notOpen` = unseen ∪ hazard ∪ out of bounds ∪ hostile block (one `IsOpen` test).
- E1e/E7: patrol wing in `EnemySquadronCommander` (`SetWingPatrol(level ≥ 2)`): free ungrouped fighter/interceptor first, else a group A wing while group A > 4. Sees a hostile ship within 1,200 → beacon = first point toward home (200 steps) outside hostile block circles, ≥ 1,000 from hostile relays, ≥ 1,200 from known ships; holds 60 s; `TryGetSpawnBeacon` only once arrived (≤ 150). Production `TryBuyPatrolWing`: cheapest fighter/interceptor outside the wing cap, one order per 60 s. Corvette patrol only at level 1.

### Economy (E2)
- `StationIncome` (shared): 30 per level × research × 1.5 with a living second station (`SecondStationLevelModel.BuiltChanged` recalculates). `MiningFacilityData` and `TibannaGasStationData` income 20 → 30.
- Skirmish money 5,000 / 12,000 / 30,000, step 100 → 500 (`SkirmishModel`, `SkirmishSlider.prefab`). Saved setups keep their stored money (not clamped).
- `economy` telemetry row every 15 s per player (`EconomyService`): money, income, sources by provider type.

### Zones and wings (E3, E4, E6, E8)
- E3a `AssignInZone(spread: true)` for group A: parts of ≥ 2 wings, one per winnable spot; a part that wins nothing joins the next; no contested-site parking (zone centre instead).
- E3b `EnemySiteConstructionController.ChooseSpotFacility`: battle asteroid when held ∧ hostiles in zone, or zone lost and retaken, or hostiles in zone before 120 s. Deviation: (3) counts any hostile unit, not only wings.
- E3c zone force with no capture objective → `GuardPosition` = nearest own satellite/platform in its zone.
- E4a `CollectPoolZones(…, origin = pool centre)` adds known hostile ships within 800 of the pool → zone line to that zone's defenders (not held zones; ships already near the zone skipped).
- E4b executor: blocking-facility objective whose capture target is the centre detour → attack-move to the waypoint first.
- E4c `IsStalledByResult`: zone signature (own structures, hostile statics, held) unchanged 120 s → marked like a retreat. The advantage stall (90 s) stays.
- E6 bombers without a raid hold (attack-move) at the nearest own held zone centre, else home; D5 fallback only for fighters/interceptors.
- E8 `EnemyWingGroup.ZoneDeaths`: ≤ 1 capturer and 2 deaths at the zone within 180 s → zone abandoned 180 s; next outer zone, else raid.

### Fleet, churn, abilities (E5, E9–E11)
- E5 `EnemyShipTaskGroup.Turtle` (set on the reserve while `_turtleUntil` runs): any state other than DefendBase/Hold/RebuildFleet/RetreatValue → Hold (gather); allied non-retreat requests ignored.
- E9 pads: lost twice within 60 s → no satellite 45 s (`TrackPadLosses`, `IsPadWaiting`).
- E9 orders: `AiOrderDeduplicator` wraps every `EnemyTaskForceExecutor`. Same move/attack-move point (≤ 50) or same attack target is not resent while running. Deviation: an ended move/attack-move is resent at once (order repair tests); an ended attack on the same target waits 10 s.
- E10 `AutoPowerDecider` (shared): player `PlayerAutoPowerService` and AI `EnemyShipAbilityController` both use it; `AutoPowerCombatSnapshot` reads the caster's owner (`IPlayerRelations`). AI: shield loss sampled every 0.5 s; one use cycle per `AbilityDecisionInterval`, gated by `AbilityUseChance`; direct `TryActivate`.
- `ShipAbilityAiUse` kept: Lock S-Foils (Escape), Replenish Wingmen, Lucky Shot, Gravity Well (Offensive) have no auto-power role. Defensive and NearbyStrikecraft code paths and `RetreatShieldThreshold` removed.
- E11 `AutoPowerTuning.shieldBoostBelow` 0.4: shields < 40 % ∧ visible hostile warship in radar range → Shields first, no confirmation (`AutoPowerSituation.Urgent`). Old 60 % + damage rule kept.

### Tests (E12)
- Written: `ForwardSpawnRingTests` (Ships); controller forward / fallback / rear modes and smallest-first order (`EnemyFactionControllerLifecycleTests`); group A spread and no contested parking (`EnemySquadronCaptureAssignmentsTests`); `AiOrderDeduplicatorTests`; pad churn (`EnemyDefenseStrategyTests`); shield rule (`AutoPowerRulesTests`); second-station income and per-level income as relations (`EconomyTuningTests`); slider range (`SliderInteractionTests`).
- Not written: turtle orders, route defenders, stall by result, patrol wing/beacon, lone wing, bomber hold, battle asteroid choice, C6/B8 gaps.
- Run 2026-11-20: see Verification below.

### Verification (2026-11-20)
- EditMode categories run (explicit tests off): `EnemyAi` 574/574, `Ships` 225/225, `Economy` 129/129, `Ui` 400/400, `Core` 169/169, `Abilities` 229/230.
- First `EnemyAi` run: 3 order-repair tests failed (`CaptureZone_StationaryAttackMoveIsOverriddenUntilInsideTheCircle`, `CaptureZone_StoppedOutsideObjectiveReissuesOneSharedGroupOrder`, `GuardSite_RepairsAnIdleShipsOrderOutsideTheGuardArea`) → filter narrowed (ended moves resend at once); rerun green.
- `Core`: 2 tests pinned station income 15/level → now derived from `StationIncome.Get`.
- `Abilities` failure not related to this plan: `SuperWeaponTests.ProductionQueueOrbitalCancel_RequiresConfirmationWhileBuildIsPending(True)` ("IonCannon cannot move from Unavailable to Unavailable"); its fixture uses none of the changed code. Not checked against a clean checkout.
- No playtest (user gate).

### Outcome
- All round-5 scope implemented and source-verified 2026-11-20. Acceptance → round-5 playtest gates above (user).
- Reusable guidance → [[Architecture/Enemy_AI_Objectives]] section "Round 5".


## Evidence — battle 2026-11-20 15:48 (round-5 playtest; Small 1v1, code `0c44f1fb7` dirty, 19:15)
Files: `…/BattleTelemetry/Loading_20261120_154837_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261120_154841_map.csv`, `Battle_20261120_154952.csv`. Log 10.1 s = battle 0 s.
- AI Separatist (1316, 1030) vs human Republic (−1316, −1030). Coruscant, seed 1412934133. Start money 30,000. Outer zones NW (−1405, 1459), SE (1405, −1459).
- Result: AI station lost 18:26. AI held the centre relay 2:19–16:03.

### User verdict
- Start and early mid-game: perfect, with a lot of money.
- Mid-to-late: bad ship development; much money and builds nothing, or spawns everywhere. (User attacked several zones at once.)
- No defence platforms near zones or the station.
- Second station: built too late and placed badly: near the centre but too close to the user's station and outer zone. Wanted: earlier, near the main station, further into the corner.

### Worked
- Forward spawn: `spawnAttempt found=True` 77 / 78. 51 of 83 ships arrived > 600 from the station; rear mode 4 / 4 under station pressure.
- Patrol wing 70: assigned 1:29, alive to 18:28; 19 `spawnBeacon` rows.
- Median ship life 106 s (13:11 battle: ≈ 80 s); 171 s for ships deployed in minutes 0–4.
- Fleet 22 ships at 5:36 and 7:56. `wingZoneLeft` fired 7 times.

### Faults
- **H1 (P0) Capital saving while the fleet dies.** 10:17–16:51: 5 capitals (2 Invincible, 2 Recusant Dreadnought, 1 Lucrehulk) = 91,000; waits 70 / 45 / 59 / 36 / 93 s; fleet 3–11 ships; unit capacity 55–85 of 120.
  - Capital life: Invincible 56 s (died on its spawn point, never moved) and 105 s; Dreadnought 67 s and 146 s; Lucrehulk 58 s.
  - Fleet: 22 (7:56) → 10 (10:14) → 3 (13:26). Ship life by deploy minute: 0–4 171 s, 4–8 134 s, 8–12 86 s, 12–16 67 s.
  - Earlier, 4:24–10:05, capacity was 102–116 of 120: the fleet sat at the unit limit.
- **H2 (P0, bug) Two ships on one spawn point.** 9 pairs with identical `spawn=` (7:31, 7:52, 8:33, 9:26, 10:03, 10:21, 10:47, 12:41 Invincible + Bulwark, 12:41). Cause: `TryGetSpawnPositionNearUnits` reserves no landing; `IsClear` sees physics only and a ship in hyperspace has none.
- **H3 (P0) Scattered spawns.** `EnemyForwardSpawnAnchors.Collect` sorts by kind (beacon → wing → ship → structure), then by distance to the task. Invincible: task NW zone (−1405, 1459), spawn at the beacon (1506, −100), 3,300 away. Single ships beside lone wings: Recusant (−842, 171) at 13:59, Peleset (−636, 298) at 14:16.
  - Hostile test = ship sightings within 800 only; satellites, platforms and wings are not counted.
- **H4 (P0) Battle asteroids replaced mines.** 10 of 15 `siteBuild` rows. Mining income 504 (3 mines + 3 asteroids, 3:00) → 350 (1 mine + 4 asteroids) from 5:00 to 15:00.
  - AI income 553 /s at 2:00, 564 /s at 13:00. Human 441 → 592 → 1,012 /s (18:00).
  - Cause: `EnemySiteConstructionController._lostZones` never clears ("retaken" forever); `heldUnderAttack` = any hostile unit at any site of the zone.
- **H5 (P0, bug) No defence platforms.** 1 all battle (Xq6, 6:49, SE zone). `CountZonePlatforms` counts every `StructureKind.DefendPlatform`; satellites share that kind; `TryGetHeldZonePosition` requires the count to equal the wanted number → a zone with an own satellite never gets its first platform. No rule places platforms at the station.
- **H6 (P0) Second station.** Bought 10:26 → `placementDeferred noSafeCandidate` 11:56 (refund) → bought 12:07 → placed 13:37 at (−1000, 333), `nearestHostile=870`, recovery mode on.
  - Gate: not before `SECOND_STATION_LATE_SECONDS` 480 s; blocked while a level, research or construction is pending; money ≥ 2 × price.
  - Placement: `MINIMUM_HOME_DISTANCE` 1,400 from the home station and the home zone; candidates = patrol points seen ≤ 45 s ago.
- **H7 (P1) Opening money idle.** 21,000–26,000 unused 0:15–1:27 (wing cap 6, corvette cap 2, level-2 wait).
- **H8 (P1) Bombers.** `raids > 0` on 9 of 173 `wingAssignment` rows.
- **H9 (P2) Order cancels.** 161 `None` orders on 47 ships (13:11: 81 on 24). Issuer not in the log.
- **H10 (P1) Station fell in 56 s** (17:30–18:26) with 6 ships alive.

### Code review scope
- Read: `ForwardSpawnRing`, `EnemyForwardSpawnAnchors`, `EnemyFactionController` diff, `EnemySiteConstructionController` diff, `EnemyStructurePlacementService` (defence, second station), `EnemyProductionStrategy` (second station, defence), `EnemyProductionDecisionModel.Evaluate`.
- Not reviewed in depth: `AiOrderDeduplicator`, `AutoPowerDecider`, `EnemySquadronCommander` changes.

## Round 6 — plan (2026-11-20; implemented the same day)
Order: G1 → G2 → G3 → G4 → G5 → G6 → G7–G10 → G11. Playtest once, after G11.

### G1 (P0, bug H2) Landing reservation
- [x] `EnemyFactionController` forward branch: reserve the hull footprint of the chosen point (`IShipSpawnPoints.ReserveDefenseLanding`) before the next ship of the same tick searches; release it when the ship is deployed into the battle (same release point as the defence wave).
- [x] `ForwardSpawnRing` stays pure; the reservation is seen through `IShipSpawnClearance.IsClear`.
- Implemented differently: see "Round 6 — implementation notes" (cause was hull heights, not a missing reservation).

### G2 (P0, bug H5) Defence platforms
- [x] **G2a** `CountZonePlatforms`: count only platform types from the faction's purchasable list (`EnemyFactionModel.DefendPlatforms`). Satellites and battle asteroids are not counted.
- [x] **G2b** Station ring: `TryGetDefensePosition` also offers the home station (ring search around it): 1 platform from level 2, 2 from level 4. Order: station → centre → captured relays → home relay.
- [x] **G2c** Zone targets: 1 per held outer zone, 2 at a held centre; up to `MAX_ZONE_PLATFORMS` 3 only while ships are blocked by the unit limit (existing rule).
- [x] **G2d** A contested zone still gets its platform when the point passes `IsPlacementSafe` (drop the `IsContested` skip).

### G3 (P0, H3) Anchor choice
- [x] **G3a** `EnemyForwardSpawnAnchors`: sort by distance to the task. Kind is the tie-break only. The beacon is first only when it is within 1,000 of the nearest candidate.
- [x] **G3b** Hostile test: known hostile ships, platforms, satellites and stations within 800 reject the anchor.
- [x] **G3c** Deep anchors: an anchor nearer a hostile station or hostile relay than any own relay or station needs ≥ 3 own ships within 600.
- [x] **G3d** Capitals (`IsAssaultCapital`): forward only at an anchor with ≥ 4 own ships within 600; else held relay or station.
- [x] **G3e** Ships deployed in one tick share the first ship's anchor (landed ships rank before a new search).

### G4 (P0, H1) Buy what fights now
- [x] `EnemyProductionStrategy`: saving for a unit that is not affordable is allowed only when own ships ≥ 10 and fleet advantage ≥ 1.0 and recovery mode is off. Otherwise `TrySelectCounterUnit` and `TrySelectShip` pick the best unit affordable now.
- [x] Money above 2 × the price of the unit being saved for → also buy the best affordable ship in the same cycle.
- Accepted by the user 2026-11-20. Replaces the earlier "saving for an Invincible under siege is fine".

### G5 (P0, H4) Battle asteroids
- [x] At most 1 battle asteroid per zone. Every other spot gets a mine.
- [x] `retaken`: the mark is cleared when the zone's asteroid is built.
- [x] `heldUnderAttack`: a known hostile ship in the zone, or ≥ 3 hostile wings. A single wing does not count.
- [x] `opening` rule unchanged (first 120 s), within the 1-per-zone limit.

### G6 (P0, H6) Second station
- [x] **G6a Timing.** From level 2, when money ≥ 2 × price and no mandatory satellite or mine is pending. Delete the 480 s gate and the "level / research / construction pending" block.
- [x] **G6b Placement.** Ring 400–700 around the home station, on the half away from the map centre (the corner side); in bounds, visible, placeable; farthest from known hostiles wins. Delete `MINIMUM_HOME_DISTANCE` 1,400 and the patrol-point source.
- [x] **G6c Fallback.** No point there → own held outer zone nearest home → retry every 10 s (existing). Never a point nearer a hostile station than the home station.
- [x] **G6d** `placementDeferred` keeps the paid request and retries; no refund-and-rebuy loop.

### G7–G10
- [x] **G7 (P1, H7) Opening surplus.** Covered by G2b and G6a: with ≥ 15,000 after the opening buys, the station platform and the second station are bought as soon as level 2 lands. No wing-cap change.
- [x] **G8 (P1, H8) Bombers.** Telemetry `raidSkip reason=noKnownMine|notWinnable|noBombers|holding` per raid decision. Rule change after the next battle, from that row.
- [x] **G9 (P2, H9) Orders.** `ai_orders` rows carry the issuer (`allocator|executor|commander|dedup`). Rule change after the next battle.
- [x] **G10 (P1, H10) Station defence.** Covered by G2b (platforms) and G4 (ships now). No separate change.

### G11 Tests (written with each step; run only on the user's request)
- `EnemyAi`: landing reserved between two ships of one tick; platform count ignores satellites; station ring offered; anchor order by distance; deep-anchor and capital rules; affordable-now rule (weak fleet → no saving; strong fleet → saving); one asteroid per zone, mark cleared; second-station gate and corner-side placement.
- `Ships`: `IsClear` false on a reserved forward landing.
- Gaps from round 5 still open: turtle orders, route defenders, stall by result, patrol wing and beacon, lone wing, bomber hold. Add with G11.

### Round 6 playtest gates (user, after G11; same matchup, then Rebellion vs Empire)
- New telemetry for the review: `raidSkip reason=…;group;wings;knownMines` (G8), `ai_orders` column `issuer` = `executor|dedup|commander|patrol|` (G9), `surplusShip` (G4), `placement source=stationRing|homeCorner|heldZone` (G2/G6), `deployment anchor=landed|beacon|wing|ship|structure|relayOrStation|rear` (G3).
- 0 pairs of ships with the same `spawn=` point.
- ≥ 1 platform at the station by level 2 + 60 s; ≥ 1 at each zone held > 120 s.
- Second station placed ≤ 60 s after level 2 with ≥ 6,000 credits; ≤ 700 from the home station; farther from the hostile station than the home station is.
- Mining spots: ≤ 1 battle asteroid per zone; mining income never falls while the number of held spots does not fall.
- No `production-wait reason=saving` row while own ships < 10.
- No capital dies < 90 s after spawning outside a station defence.
- ≥ 80 % of forward spawns within 1,000 of the ship's task or of ≥ 3 own ships.

## Round 6 — implementation notes (2026-11-20)
### G1 Spawn pairs (H2) — cause corrected
- All 10 pairs in the 15:48 log were **different ship types** (e.g. Dispatcher + Recusant at 461.5 s). `ShipHullFootprint.Overlaps` rejects on height first; hulls fly at per-type heights → the 3D landing test passes. `ShipMoveComponent` already reserves the landing synchronously (VContainer `IInitializable` runs in `Build`).
- Fix: `ShipSpawnPoints.TryGetSpawnPositionNearUnits` adds a top-down test: no living ship (any height) and no forward landing of the last `FORWARD_LANDING_SECONDS` 2 s (hyperspace 0.6 s) within the two planar radii. No `ReserveDefenseLanding` call; player spawn rules unchanged.
### G2 Platforms (H5)
- `EnemyStructurePlacementService.CountPlatforms`: own entities with `IDefendPlatformTypeFacade` whose type is in `EnemyFactionModel.DefendPlatforms` (satellites, battle asteroids excluded). Constructor: `patrol` → `factionModel`, `relations`.
- Order: station ring (`stationRing`: 1 from level 2, 2 from level 4; only while the AI hosts its own base) → centre → captured relays → home relays. Zone limit: centre 2, others 1; `allowExtra` (ships blocked by the unit limit) → 3. `IsContested` skip removed; points still pass `IsPlacementSafe`.
### G3 Anchors (H3)
- `EnemyForwardSpawnAnchors.Select` (static, plain positions in `AnchorPicture`): landed this tick first → distance to task → kind. Beacon promoted to first only within `BEACON_LEAD_RADIUS` 1,000 of the best other anchor.
- Reject: hostile within 800 = known ships + armed structures seen or remembered (`RememberStructure`; mines excluded) + every hostile station. Deep (nearer a hostile station/relay than an own one) → ≥ 3 own ships within 600. Capital (`IsAssaultCapital`, read only for level-4 ships) → ≥ 4 own ships within 600; else relay/station search.
- `IEnemyForwardSpawnAnchors.Collect` gained `bool capital`; anchors class now needs `IReinforcementZoneSource`, `IVisionService`.
### G4 Production (H1)
- `CanSaveForUnit`: reserved ships ≥ 10, not recovering, `CombatMatchup.Advantage(own live, known hostile)` ≥ 1. Else `TrySelectCounterUnit(requireAffordable)` and `TrySelectShip(requireAffordable)` (also under station pressure, as before).
- Surplus: after a ship purchase with money before it ≥ 2 × its price → best affordable ship in the same cycle (`surplusShip` row). Interpretation: "money ≥ 2 × the unit bought", since a unit being saved for is unaffordable by definition. Deviation: only from level 2 (level-1 corvettes stay one at a time).
### G5 Battle asteroids (H4)
- `EnemySiteConstructionController.ChooseSpotFacility`: zone already has an own asteroid (Constructing/Operational) → mine, and the `retaken` mark is cleared. `heldUnderAttack` = team intel: a known hostile ship, or ≥ `HELD_ATTACK_WINGS` 3 hostile wings in the zone. Opening rule unchanged. Constructor + `TeamIntelRegistry`, `IPlayerRelations`.
### G6 Second station (H6)
- `TryBuySecondStation(critical)`: level ≥ 2, money ≥ 2 × price (1 × when critical), no `PriorityConstructionPending`. 480 s gate and level/research/construction block deleted.
- Placement: rings 400/500/600/700 around the home station, only the half away from the map centre; placeable; never nearer a hostile station than home is; farthest from known hostiles wins (`homeCorner`). Fallback: own held outer zone nearest home (`heldZone`). Patrol-point source and `MINIMUM_HOME_DISTANCE` deleted.
- `EnemyFactionController`: no point at build time → `EnemyBuildQueue.RetryStationPlacement` 10 s; paid request kept (`placementDeferred refund=0`).
### G7–G10
- G7, G10: covered by G2b + G4 + G6a; no separate change.
- G8: `EnemySquadronCommander.RecordRaidSkip` per outer group, one row per reason change: `holding` (station defence), `noBombers`, `noKnownMine`, `notWinnable`.
- G9: `ShipOrderTelemetry.Issue(issuer)` scope → `ai_orders` column `issuer`. `AiOrderDeduplicator` tags `executor`, or `dedup` for a same order resent after it ended; `EnemyUnitCommander` zone-exit orders `commander`; patrol ship `patrol`. Empty = player or ship-internal rows. `Tools/BattleAudit/battle_audit.py` reads by column name (unaffected).
### Tests (G11)
- New: `EnemyForwardSpawnAnchorsTests` (6: distance order, beacon lead, landed first, hostile structure, deep anchor, capital escort), `EnemySpotFacilityTests` (2: one asteroid per zone, 3 wings), `ForwardLandingTests` (Ships: two types never share a point), `UnaffordablePreferredShip_OnlyAStrongFleetSavesForIt` (×2), `DefensePosition_SatellitesAndBattleAsteroidsAreNotPlatforms`, `SecondStation_LandsOnTheHomeRingOnTheCornerSide`.
- Updated (contract changed): `UltraHardRebuild_BuysTheBestShipItCanPayNowAndRechecksLosses` (was …SavesForPriorityShip…; capacity case now buys the Munificent that fits), `Level1Corvette_IsNeverBoughtFromLevelTwo` (asserts no C-9979, not "no purchase"), `SecondStation_RefundsAnUnsupportedUpgrade_AndKeepsAnUnplaceableStationQueued` (G6d), `DefensePosition_EveryHeldZoneDefended_…` (centre takes 2), placement fixture (roster-backed `EnemyFactionModel`, typed `PlatformEntity`), site-controller and anchor fakes (new constructor/params).
- Reviewed, no change: `PreferredShipAtLimit_…` (surplus buys the same type; last request unchanged), opening production test (surplus gated to level ≥ 2), `RealEntityCompositionTests.AiPlayer_RejectsSecondStation…` (level 1 / no base), `AiOrderDeduplicatorTests` (filtering unchanged).
- Not covered: station ring (needs a living host base), contested-zone platform, G6c fallback, raidSkip/issuer telemetry; round-5 gaps (turtle, route defenders, stall by result, patrol wing/beacon, lone wing, bomber hold) still open.
### Verification
- Unity recompile 0 errors (`Assembly-CSharp`, `Assembly-CSharp-Editor`). Tests not run (local override). No playtest yet (user gate).
- `Tools/Jenkins/TEST_INVENTORY.md` not updated (already stale: lacks round-5 fixtures; another session holds edits to it).
### Outcome
- All round-6 scope implemented and source-verified 2026-11-20. Acceptance → round-6 playtest gates (user).
- Reusable guidance → [[Architecture/Enemy_AI_Objectives]] section "Round 6".


## Evidence — battle 2026-11-20 17:44 (round-6 playtest; Small 1v1, code `56e554a0b` dirty, 26:04)
Files: `…/BattleTelemetry/Loading_20261120_174422_ai.csv`, `_ai_orders.csv`, `_squadrons.csv`, `Battle_20261120_174425_map.csv`, `Battle_20261120_174533.csv`. Log 21.8 s = battle 0 s.
- AI Rebellion (−1118, −1139) vs human Empire (1118, 1139). Coruscant, seed 712596945. Start money 30,000.
- Result: AI main station lost 15:30, second station 22:13. Human banked 300,000+ credits from 15:45 (did not spend).
- User verdict: start as good as before; ship selection for Rebellion awful.

### Worked (round 6)
- Platforms: 13 bought (3 Golan III, 3 Empress, 7 XQ-6); `source=stationRing` 1:37, 6:51, 18:40, 19:01.
- Second station: bought 6:36, placed 8:06 at (−1630, −1616), `source=homeCorner`, 1,306 from the nearest hostile; took over 15:30.
- Battle asteroids: 2 of 17 spot builds.
- Forward spawn: `spawnAttempt found=True` 149 / 151.

### Faults
- **K1 (P0) Cheap ships at every level.** Ships bought: Nebulon-B 55 (level 2), Assault Frigate Mk II 31, CD-273 25 (level 2), MC50 22 (level 2), MC30c 13. Level 4 reached 6:45, level 5 at 10:30; first level-4 ship 12:08; 14 ships of level 4–5 all battle.
  - Cause: G4 buy-now filter (`requireAffordable`) + recovery mode on 2:12–15:23 → only ships paid for now are candidates; money stayed 2,200–3,700, so only level 2–3 ships ever qualified (`counter-selection candidates=1–4`).
- **K2 (P0) Opening money on two platforms.** Golan III 12,000 (0:57, NW zone) + Empress 10,000 (1:17, centre) = 22,000 of 30,000. Money 3,875 when level 2 landed (1:26); in the 15:48 battle it was 26,000 and bought 8 ships at once.
  - Cause: `TrySelectDefense` takes the strongest affordable platform; G2 made platforms reachable.
  - Effect: 2 ships at 2:03 vs 10 hostile at 3:03 → recovery mode for 13 min.
- **K3 (P1) Queue clog.** 13 ships bought per minute, 10 arrive: same-type builds run one at a time (`EnemyBuildQueue`). ≈ 22 paid ships waited in the queue at 10:00; unit capacity 103–120 of 120 with 5 ships alive; `production-wait reason=capacity` 334 rows.
- **K4 (P1) Ships die fast.** 177 ships, median life 44 s; 145 spawned within 900 of home, 96 died there. Fleet advantage 0.07–0.38 from 3:00 to 15:00.
- **K5 (P2) Bombers.** `raidSkip`: noBombers 10, noKnownMine 5, notWinnable 5, holding 2 (22 rows only).

## Round 7 (2026-11-20; implemented, committed; tests green 2026-11-20 18:56)
- [x] **J1 (K1) Tier span.** `EnemyProductionStrategy.IsShipTierAllowed`: a ship is bought only when its `AvailableLevel` ≥ station level − `SHIP_TIER_SPAN` (1). `GetShipTierFloor` lowers the floor until a ship type exists in the span. A critical station takes any ship that arrives in time. Level-1 corvette rule unchanged.
  - Rebellion at level 5 → levels 4–5 only (Bothan 4,800, MC80 Justice 5,000, Mon Cal 8,000, Remonda 9,000, Endurance 10,000, Nebula 12,000, Mediator 15,000, MC80 Independence 20,000).
  - With buy-now and no candidate paid for yet, nothing is ordered until the cheapest allowed ship is paid for (≈ 10 s at 500 /s).
- [x] **J2 (K2) Cheap platforms for a small fleet.** `TrySelectDefense(cheapestOnly)`: below `SAVING_MIN_SHIPS` (10) reserved ships only the cheapest buildable platform is chosen (XQ-6, 1,000). From 10 ships the old rule applies (fewest copies, then strongest in budget).
- K3, K4: no change. Expected to ease with J1 (fewer, bigger orders) and J2 (more opening ships); re-measure in the playtest.
- K5: no change; too few `raidSkip` rows to decide.

### Tests
- Updated `EnemyProductionDecisionModelTests` (class `EnemyProductionStrategyTests`): `PreferredShipAtLimit_SelectsAnotherAvailableShip` and `UltraHardRebuild_BuysTheBestShipItCanPayNowAndRechecksLosses` set both ships inside the span (levels 4 / 5); `UnaffordablePreferredShip_OnlyAStrongFleetSavesForIt` gained a level parameter and the case "two levels below → not bought".
- Reviewed, no change: `Level1Corvette_IsNeverBoughtFromLevelTwo` (single non-corvette type → the floor falls to its level), `Opening_…` (level 1–2), critical-station tests (bypass).
- Not covered: J2 platform choice (the fixture reserves every platform type before the decision).
- Verification: `unity recompile` → 0 errors. EditMode not run.

### Round 7 playtest gates (user; Rebellion vs Empire, 30,000 start)
- Level 4 + 60 s onward: no purchase of a level-2 ship. Level 5 + 60 s onward: no purchase of a level-3 ship.
- No platform above 2,000 credits while the AI has fewer than 10 ships.
- ≥ 6 ships bought within 30 s of level 2 landing.
- Unit capacity below 100 of 120 while fewer than 15 ships are alive.
- Median ship life ≥ 90 s.


## Round 7 follow-up (2026-11-20, evening)
### Tests
- Full EditMode run by the user: `Assets/TestResults_20261120_185627.xml` → 4,451 passed, 0 failed, 4 skipped. Includes the round-7 tests (`UnaffordablePreferredShip_…(1,3,False)` and the two re-levelled tests).

### Rebellion balance check (user: "same price, different hull / shields / regen / DPS")
Source: every faction ship through `UnitCombatProfileCatalog` (the AI's own numbers), Editor state of 2026-11-20 evening (7 balance assets had unsaved Editor edits). Table → `Logs/BalanceDump.csv`. Static model; no duels run.
- Value per 1,000 credits = `GetCombatValue()` / price × 1,000 (√(durability × hull DPS)).

| Level | Rebellion min–median–max | Empire | Republic | Separatist |
|---|---|---|---|---|
| 2 | 201–401–489 | 501 | 250–284–448 | 209–290–391 |
| 3 | 414–566–790 | 401–545–639 | 401–422–639 | 364–496–644 |
| 4 | 385–546–561 | 472–548–752 | 514–676–752 | 458–550–603 |
| 5 | 391–455–489 | 369–409–815 | 346–604–815 | 249–415–512 |

- Rebellion is not weak overall: its median is the best at levels 2–3 and equal at level 4.
- Its curve falls with level; the others rise. Best Rebellion ships are level 3: MC80 Freedom 790, MC75 632, Liberator 589, MC30c 566. Best at levels 4–5: 561 (Mediator, Mon Cal) vs 752–815 for Empire and Republic (Tector, ISD II, Secutor).
- Outliers:
  - MC80 Freedom: level 3, 5,500 credits, 38,000 durability (class HeavyCruiser). More durable than the level-4 Mon Cal (27,000, 8,000 credits) and MC80b Remonda (37,000, 9,000).
  - MC80b Remonda: 9,000 credits, hull DPS 267 → 385. Lower value than MC80 Freedom at 5,500.
  - Nebula: level 5, 12,000 credits ≈ Mon Cal stats (value 4,694 vs 4,476) for + 50 % price.
  - Level-4–5 Rebellion hull DPS per 1,000 credits: 30–43 (Justice, Remonda, Mediator, Independence, Home One) vs 107–140 (Providence, Tector, ISD II, Secutor).
  - Same price, different role: MC75 vs MC80 Freedom (5,500): durability 14,300 vs 38,000, hull DPS 845 vs 305. Corellian Battlecruiser vs MC30c (3,500): hull DPS 230 vs 403.
- Effect on the AI: J1 (levels 4–5 only at station level 5) barred Rebellion from its four best ships.

### J3 (implemented, committed; `EnemyAi` 587 / 587 on 2026-11-20)
- [x] `IsShipTierAllowed`: a ship below the tier floor stays buyable while its value per credit is strictly above every available ship of the span (`GetBestSpanValuePerCredit`). Rebellion at level 5 keeps MC80 Freedom, MC75, Liberator, MC30c; Nebulon-B, CD-273, MC50 stay excluded. Empire and Republic: no ship qualifies (best value is in the span).
- Tests reviewed, no change: fixtures use a uniform value per credit, so the strict comparison never exempts a ship. Not covered: the exemption itself.
- Data not touched: prices, levels and stats are the user's balance work (unsaved Editor edits were in progress).

### Balance changes (user decision 2026-11-20, applied, committed)
- [x] MC80 Freedom: level 3 → 4 (`RebellionFaction.asset`).
- [x] MC80b Remonda: price 9,000 → 6,500.
- [x] Nebula: price 12,000 → 9,000.
- Ignored (user, 2026-11-21): Rebellion levels 4–5 hull DPS / prices; prices were cut in another task.
- Survival mode (`SurvivalWaveData.asset`): a wave holds every ship of its level and below; each tier's quantities sum to 2 × the largest tier size of any faction. The Freedom move changed tier 3 from 7 to 6 ships (sum 14 → 12) and tier 4 from 6 to 7 (sum 12 → 14) for every faction.
  - Rebellion: Freedom removed from wave 3; stays in waves 4–5 with quantity 2.
  - Republic: Arquitens 7 → 6, Heavy Dreadnought 6 → 5, Tector 2 → 4.
  - Separatist: Dispatcher 5 → 4, Recalcitrant 5 → 4, Bulwark 2 → 3, Bulwark II 2 → 3.
  - Empire: Arquitens 4 → 3, Imperial Venator 4 → 3, ISD I 2 → 3, Tector 2 → 3.
- Applied through `SerializedObject` + `AssetDatabase.SaveAssets`; values read back from disk.
- Tests: `SurvivalGameplayTests.AuthoredWaves_IncludeEveryUnlockedShipWithEqualTierCounts` reads both assets, so no test code changed; its rules were re-checked by script against the saved data → valid. No other test pins these levels or prices. EditMode not run (`GameFlow`, `Economy`, `EnemyAi` are the categories to run).


## Economy normalization (2026-11-21; user decision, implemented, commit `c2b80479f`; `Economy` 137, `Core` 169, `GameFlow` 220 green)
### Problem (user, after 4 online games)
- Income exploded in 2v2 and on big maps; mining zones stopped mattering; station buffs possibly too strong.
- Wanted: the same economy progression on every map size and player count.

### Cause (source)
- A site mine paid a fixed 60, split between allies (`TeamIncomeSource`). Spots per map = 4 centre + 1 home per player + 2 per outer-zone pair (Small 1, Medium 2, Large 3, + (players − 3) / 2 on archetype layouts).
- Fixed part per player at level 5 with research: station 210 + miners 210 + base 4 = 424.
- Per player, half map / whole map: Small 1v1 760 / 1,096; Large 1v1 928 / 1,432; Small 2v2 634 / 844; Large 4v4 655 / 886.
- Spend ceiling ≈ 450–600 /s (same-type builds run one at a time, 7 pipelines, unit limit 120).

### Decision
- **Mining pool:** site mine income = `MiningPool.PER_PLAYER` 200 × players in the match / mining spots on the map. Team shares are still split between members.
- **Station:** `StationIncome.PER_LEVEL` 30 → 20; `SECOND_STATION_SHARE` 0.5 → 0.25.
- Unchanged: station miners 30, battle asteroid 10, research multipliers, starting-credit slider.
- Result at level 5 with research, every setup: fixed part 354; half the map 634; whole map 914.
- Avoid: fixed pay per spot (scales with map size); per-team pools (a 1-player team would out-earn each member of a 4-player team).

### Implementation
- `MiningPool` (`Entities/Economy/Model`): `GetSpotIncome(playerCount, spotCount)`.
- `MiningFacilityFactory.GetIncome`: `AsteroidMiner` → pool share from `IPlayerRoster.Players.Count` and the `Mining` sites of `ICaptureSiteDirectory`; passed to `MiningFacilityInstaller` as `income`.
- Edge: Survival → data income today; to be normalized (user decision 2026-11-21) → W12. No mining spot on the map → data income. Station miners and Tibanna stations → data income.
- Edge: spot pay examples: Small 1v1 50; Large 1v1 33; Small 2v2 80 (40 per ally); Large 4v4 73 (18 per ally).
- Tests: added `MiningPool_PaysTheSamePerPlayerForTheSameShareOfTheMap` (5 setups). Reviewed, no change: `StationIncome_…` and `SecondStation_…` (relations only), `RealEntityCompositionTests` (uses `StationIncome.Get`; asteroid mine without sites → data income).
- Verification: `unity recompile` → 0 errors. EditMode not run.
- Open (user playtest): 2v2 and a Large map; income per player should match Small 1v1 for the same map share.


## Second station never levels up (2026-11-21; user report, confirmed in source)
### Finding
- The AI cannot upgrade its second station at all. It is not forgotten: it is rejected.
  - `EnemyProductionStrategy` never builds a `SecondStationLevelUnitRequest`; only `FactionUi.CreateSecondLevelUnit` (human) does.
  - `EnemyFactionController.Purchase` rejects it (`purchaseRejected`); `ExecuteBuild` drops it.
- Evidence, battle 17:44: second station `entity=213` took over at 15:30 with `hull=72000;shields=48000` (level 1) and still had level-1 values at 20:45; destroyed 22:13 (≈ 90 s of fire). The main station at level 5 had `hull=252000;shields=201600`.
- Same in battle 13:11: `entity=221;hull=72000;shields=48000`.
- Existing support the fix can reuse: `SecondStationLevelModel` (level, `CompleteLevelUpgrade`, `ResetForNewStation`), `AiProductionSaveSource` (saves `SecondStationLevel`), `AiStationLevelReplicatedState` (replicates it), `StationLevelData.GetSecondStationLevelFactionData`.

### Values (`StationLevelData.asset`)
| Upgrade | Price | Build time |
|---|---|---|
| Level 2 | 2,000 | 80 s |
| Level 3 | 3,200 | 120 s |
| Level 4 | 4,000 | 180 s |
| Level 5 | 5,500 | 220 s |
- Total 14,700 credits, 600 s when bought back to back.

## Round 8 — plan (2026-11-21; superseded by "Open work" W1, W13)
- [ ] **L1 Purchase rule.** `EnemyProductionStrategy.TryBuySecondStationLevel`: second station stands ∧ its level < main station level ∧ no second-station upgrade queued ∧ money ≥ price ∧ no mandatory satellite or mine pending ∧ station not critical → buy. One upgrade at a time.
  - Order in `EvaluateProduction`: after miners and a due main level, before combat research and ships.
  - Main station lost → the second station is the base: its upgrade ranks before ships up to the level the main station had.
- [ ] **L2 Processor.** `EnemyFactionController`: accept the request in `Purchase` (delete the reject), build it with its real build time on its own pipeline id, and on completion call `SecondStationLevelModel.CompleteLevelUpgrade()` in `ExecuteBuild`.
  - Second station destroyed while an upgrade is queued → refund and remove the request (as the human `FactionService` does).
  - New second station → level 1 (`ResetForNewStation`, already called by the factory path; verify for the AI).
- [ ] **L3 Telemetry.** `purchase` row with category `SecondStationLevel`; `station` row gains `secondLevel=`. Add both to the compact release log.
- [ ] **L4 Tests (`EnemyAi`).** Buys when the second station stands below the main level; not without a second station; never above the main level; one at a time; completion raises the level; station lost → refund. Relations only, no prices pinned.
- Avoid: upgrading the second station past the main one (money better spent on the fleet); a zero build time (the human pays the time).

### Round 8 playtest gate (user)
- Second station bought → its first upgrade bought ≤ 30 s after it is placed (money allowing); it reaches the main station's level, or is ≤ 1 level behind when the main station falls.


## Squadron rework check (2026-11-21; source read, no battle)
Changes checked: [[Done/Bugs/Squadron_Idle_And_Guard_Engagement]] (`cc6bd2f23`), [[Done/Bugs/Fighter_Interceptor_Rebalance]] (`4262fc9e4`), attack-move rework (`012594726`).
- Live values: `SquadronData.GuardRadius` 250 (31 assets), `squadronGuardEngageRadius` 400, `squadronLeashFactor` 1.3 → idle leash 325, guard leash 520.

### Adapts without change
- Wing ratings and counters: `UnitCombatProfileCatalog` reads weapons and the damage matrix → new interceptor bolts and hulls are picked up.
- Cheap-wing rule (`EnemyWingPricing.CHEAP_WING_PRICE` 300): Empire (TIE Fighter 275) and CIS (Vulture 180, Belbullab 300) stay "cheap" → opening cap 6, 2 wings on the nearer outer zone. Rebellion and Republic stay at cap 4.
- Station-defence, raid and scout wings: attack-move arrival search is now 250 (was 70) → they find attackers they used to miss.
- Idle wings return to their post after a fight (`ResumeCourse` → `FlyTo(LoiterCenter)`) → wings left without an order no longer drift.
- Hunt is unchanged.

### Problems
- **N1 (P1) The commander interrupts idle fights every 3 s.** `EnemySquadronCommander.IssueOrder` runs for every wing each `DECISION_INTERVAL` 3 s. A wing whose order completed is idle; idle wings now engage within 250. The next pass re-issues the order (order is `None`, so `Matches` fails) → `Disengage()`.
  - Capture hold (`MoveTo` within `HOLD_ENTRY_SCALE` × spot radius = 120): engage → pulled back ≤ 3 s later → engage again. Before, idle reach was 70 ≈ the 60 circle, so this was rare.
  - Wing chased > 120 from the spot → the pass sends `AttackMoveTo(spot)`: it flies back without fighting (new attack-move), then engages under the attack-move order, which has no leash → leaves the circle for the whole fight.
  - Bomber hold, station defence and "home" attack-moves: same re-issue once per new contact.
  - Spawn beacon (`MoveTo(beacon)` every pass): the pull-back is wanted there.
- **N2 (P1) Price-blind wing rotation.** `TrySelectSquadron` picks the type with the fewest copies, then the cheapest. Interceptors now cost 1.4–2.0 × the fighter (A-Wing 1,050 vs X-Wing 750; TIE Interceptor 550 vs TIE Fighter 275; Belbullab 300 vs Vulture 180; V-Wing 750 vs V-19 450).
  - Rebellion opening, 4 wings: 3,600 instead of 3,000 for fighters only. Empire, 6 wings: 2,475 vs 1,650.
  - The rotation also reaches TIE Brute 425, Avenger 525, Defender 1,000 once unlocked.
  - Rebalance result: at equal credits fighters beat interceptors; capture wings need presence and hull, not interceptor bolts.
  - Conflicts with the user rule of 2026-11-19 "first buys: fighter + interceptor wings" → needs a user decision.
- **N3 (P2) Escorts react closer.** Carrier wings on Guard engage within max(guarded radius + 250, 400) instead of 800 and drop targets past 520. `ESCORT_THREAT_RADIUS` 2,000 still assigns them early. Bombers that release outside 400 of the ship are answered later. Not verified in battle.
- **N4 (P2) Republic unlock levels.** Live data: ARC-170 and V-Wing at level 5 → a Republic AI opens with V-19 only (450) plus bombers. Outside this plan; noted for the balance owner.

## Round 8 additions (squadron follow-ups; superseded by "Open work" W4, W6, W13)
- [ ] **L5 (N1) No re-issue into a fight.** `ISquadron` exposes `IsEngaging`. `IssueOrder` skips the capture-hold `MoveTo`, the bomber-hold, station-defence and home attack-moves while the wing is engaged and within the idle leash (`GuardRadius` × `SquadronLeashFactor`) of its point. The idle leash then brings the wing back to its post by itself. The beacon `MoveTo` stays unconditional.
- [ ] **L6 (N1) Capture hold stays a hold.** A wing pulled > 120 from an uncontested spot returns by `MoveTo`, not `AttackMoveTo`, so it cannot leave on an unleashed attack-move.
- [ ] **L7 (N2) Wing choice by job.** Capture wings: cheapest available `Fighter`-class wing. Interceptors and elite fighters only through `TrySelectCounterUnit` (known hostile bombers or fighters). **User decision needed** (replaces "fighter + interceptor first").
- [ ] **L8 Tests (`EnemyAi`).** Engaged wing near its point gets no new order; idle unengaged wing does; wing far from its hold spot gets `MoveTo`; capture-wing choice ignores a dearer interceptor; counter choice still buys it against known bombers.
- N3: no change; check escorts in the next Editor battle (`_squadrons.csv`).


## Structures placed around the destroyed base (2026-11-21; user report, confirmed in source and log)
### Finding
- `EnemyStructurePlacementService.GetStationCenter()` = `_mapModel.GetStationPosition(ProductionHost)` → the main station's **map slot**, alive or not. It is the origin for miners (`SearchPlacement`, line 362), the station platform ring (177), the second-station ring (283) and the map-wide fallback (408).
- After the main station falls the second station builds, but every search still starts at the dead base.
- Destroyed positions block only `DESTROYED_POSITION_EXCLUSION_RADIUS` 32 around each wreck, newest 10.

### Evidence — battle 17:44 (main station (−1118, −1139) lost ≈ 15:23; second station at (−1630, −1616))
| Time | Bought | Where | From dead base | From second station |
|---|---|---|---|---|
| 15:23–15:27 | 5 miners | (1114…1436, −926…−1141), owned SE zone | 2,233–2,563 | 2,785–3,142 |
| 16:24–16:50 | 5 miners | (−1142…−1775, −1651…−1756) | 592–880 | 148–501 |
| 18:40 / 19:01 | Golan III 12,000 + Empress 10,000 | (−1118, −786), (−1314, −846), `source=stationRing` | ≈ 350 | ≈ 980 |
| 19:28 / 20:04 | 2 miners | (−1455, −651), (−1352, −542) | 592 / 640 | 981 / 1,109 |
- 12 miners (12,000 credits) and 22,000 of platforms after the base fell; the first 5 miners were lost within a minute (rebought 16:24).
- Miners are rebought at once whenever the count is below target (`MiningFacilityCount < MiningFacilityTarget`), with hostiles 970–1,320 from the chosen points.

## Round 8 additions (placement after a station loss; superseded by "Open work" W3, W13)
- [ ] **L9 Origin = living base.** `GetStationCenter()` → position of `IProductionAccess.TryGetProductionBase` (the station that builds now). No living base of the host → no miner, platform-ring or second-station search.
- [ ] **L10 Lost-base area is closed.** A destroyed own station closes a circle of its station radius + 400 around the wreck for miners and platforms for the rest of the battle (replaces the 32 exclusion for stations only).
- [ ] **L11 Miner rebuy rule.** No miner purchase while a known hostile ship is within 1,000 of the living base, or while the turtle after a station loss runs (180 s). Then one miner per 10 s, placed on the half of the base ring away from the known hostile centroid.
- [ ] **L12 Tests (`EnemyAi`).** Miner origin follows the second station after the main one dies; no placement inside the closed circle; no miner purchase with a hostile ship near the base; platform ring surrounds the living base.
- Avoid: the map slot as "home" after the station is gone; refilling the miner count while the base is under fire.
