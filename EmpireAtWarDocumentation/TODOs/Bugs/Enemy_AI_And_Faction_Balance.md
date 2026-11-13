---
category: Bugs
status: in-progress
created: 2026-10-27
system:
  - ai
  - balance
commit: 6babee5e1
---
# Enemy AI and Faction Balance

## Goal

- CIS vs Republic: neither faction wins by unit stats alone.
- Ultra Hard AI must not lose to the player on either faction at 2:1 ship value.

## Findings

- Paper (Battle Simulator, equal cost): CIS wins L2–L3; Munificent outclasses every Republic frigate. Republic wins with heroes.
- Real battles disagreed → hidden factors: Resolute Fleet Command (×0.65 damage taken, ×1.2 hull, ×1.1 shields/damage, 400 radius); Republic Enhanced Shields research vs CIS Engines.
- Telemetry battle 1 (player CIS): ship trades near even (Rep 232k vs CIS 207k lost); CIS squadrons fired 17 shots per squadron lost vs Republic 197.
- Player wins as either faction (AI loses 1.6–2.0× ship value) → root cause is AI, not faction stats.
- AI log: `CaptureZone` 51%, `RetreatValue` 36%, attack states 0%. Ignored a 4–11× early lead; 35 capture ↔ retreat flips.

## Decision

- Republic-only torpedo nerf via faction-scoped weapon types. Why: types 6/48 are shared with CIS/Empire/Rebellion. Avoid: editing shared definitions.
- Victory (shared with Empire) and fighter torpedoes unchanged.
- Fleet Command passive left unchanged (shared with Home One); Concentrate Fire nerfed instead.
- Retreat recovery update 2026-10-31 → [[Architecture/Enemy_AI_Objectives]]: regrouping permits safe local action without global parity; full-fleet offense retains normal strength thresholds.

## Implementation — Balance

| Change | Value |
|---|---|
| `RepublicLightProtonTorpedo` (120), `RepublicMediumProtonTorpedo` (121) | ×0.7 damage, Faction scope; Centax, Acclamator, Venator, Venator II (Resolute prefab), Resolute, Rothana, Mandator |
| CIS hangar `maxActive` | ×3: Captor 2→6, Providence/Invincible/Lucrehulk 1→3, Bulwark 2→6 / 1→3, Wavecrest 2→6 |
| Droid Bomber / Vulture `MemberHull` | 25→50 / 35→70 |
| Belbullab-22 | `BelbullabTripleLaser` (122): Unit scope, Triple mount, ×1.5 → ~200 DPS (was 44); hull 220→300 |
| Carrack `ShieldRegenTier` | Corvette → Frigate (5 → 25) |
| Concentrate Fire (Resolute) | damage 1.25→1.15, fire delay 0.4→0.6, duration 12→8 s |
| `UnitOrderSettings.squadronGuardEngageRadius` | 800: guarding squadrons attack enemies near the guarded unit |

## Implementation — AI

1. Hangar squadrons: `EnemySquadronCommander` leaves hangar-launched squadrons on Guard; capture/hunt only after the guard ends.
2. `LocalAdvantage` counts own units within 1000 of the fleet only.
3. Thresholds (attack / hunt / retreat): Ultra Hard 0.75/0.8/0.5 → 1.1/1.0/0.8; Hard 1.0/0.9/0.6 → 1.15/1.0/0.7.
4. `ForceCompositionBuilder.BuildKnown`: remembered hostiles at full strength until forgotten (240 s).
5. `FleetCohesion` (radius 800, leave ×1.5): offensive orders to the main body; stragglers Guard the anchor.
6. Production: priority ÷ combat value per credit; counter variety penalty ÷ √(1 + owned).
7. Press advantage (weight 0.75): `FleetAdvantage ≥ 2 × RequiredAttackRatio` → AssaultBase / HuntFleet over capture.
8. Retreat memory (2026-10-31): regrouping gates safe local action, without global parity. Lost home + no safe anchor can rally via viable local capture/engagement; station pressure overrides. Current contracts → [[Architecture/Enemy_AI_Objectives]].

## Implementation — AI Economy (2026-10-28)

- Findings (code read, no battle data): AI below Ultra Hard stopped at 1–2 Mining Facilities; capture sites alternated mine/Battle Asteroid (1600 for 10/s vs 600 for 60/s); unaffordable ship → bought XQ-6 instead (up to 5, 0 income); cheapest platform only; IPV-2C (limit 1, capacity 2) hit the flagship rule.
- Mining target, all difficulties: `MinimumMiningFacilities + ActiveShipCount / 3` (Ultra Hard `/ 2`, unchanged). Above the floor: bought only when affordable, never saved for.
- Easy/Medium/Hard: with a ship option, save for the chosen ship (`None`) instead of Defense/Level/Research fallbacks.
- `TrySelectDefense`: most expensive affordable platform; none affordable → cheapest (what Ultra Hard saves for).
- Flagship rule also needs `unitCapacity >= 5`.
- `CaptureSitesSystem.TryBuildOnOwnedSite`: AI always builds `SiteFacilityType.Mining`; `ChooseFacilityType` removed.
- Team income: Mining Facility / Asteroid Mine / Battle Asteroid income is split evenly between team economies (`Income / _teamEconomies.Count`). Before, every ally received the full amount → team mining ×team size; the human's wallet also got ally-built mines in full.
- Not changed: hosted AI ally still earns station income `3 + 15 × host level` with no station of its own; station income keeps flowing after the station dies.

## Implementation — Mining Objectives (2026-11-01)

- Scout assignment preserves the final eligible capture squadron while capture objectives remain; carrier guards and escorts are excluded.
- Mining raid utility `0.66` beats expansion `0.6 + 0.05` hysteresis. Threatened-site defense disables this rule; base defense, retreat and decisive attack retain priority.
- Raid selection prefers visible hostile mining facades over other structures, including previously retained non-mining targets.
- Raid safety compares this group's strength against known defenders within 1000 units using the difficulty's attack threshold; remembered strikecraft count too.
- Captured uncontested mines can be built before owning two warships. The two-warship guard remains for satellite defense.
- Built mines require destruction before site recapture. Capture detachments continue expanding while combat groups can raid.
- Original EaW comparison and ship counter formulas → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].
- Reusable rules → [[Architecture/Enemy_AI_Objectives]]. Broader faction balance and Fleet Command decision remain outside this fix.

## Implementation — Target-Specific Task Forces (2026-11-01)

- Existing geographic groups remain recruitment boundaries. `EnemyAttackTaskForce` maintains a stable strike subset for each group's current target.
- `EnemyTaskForceSelectionModel` rates actual current-health weapon forces against target class/hull/shields and known defending units within 1000 units. Dedicated defender counters can join even without primary-target DPS.
- Select up to the strategic committed count; aim for 30 s target clear time. This is sizing only: a larger target does not prevent a viable attack.
- Reuse viable members; rebuild inadequate composition after losses or changing defenders. Visible fleet targets persist until invalid/hidden/hazardous.
- Entire available strike force insufficient → reevaluate existing non-offensive goals before issuing orders; capture/defense/local action/hold remain available.
- Launch only when the cohesive body can damage the target and meets difficulty attack advantage; distant selected ships rally while the anchor waits. Surplus ships Guard the strike anchor.
- Defender specialists Guard the strongest primary-target attacker instead of receiving an ineffective primary-target Attack order; role selection compares class-effective DPS. Guard orders persist while role/anchor remains valid.
- Base defense and immediate local engagement bypass assembly waiting. Capture/retreat state changes discard strike assignment.
- Reuses `ForceComposition`, `CombatMatchup`, `FleetCohesion`, existing order APIs and production. No cross-map recruitment or task-specific production queue added.
- Remaining estimate limits: structure health counts for weapon suitability, but static structure weapon DPS, range/arcs and abilities are not added to the existing unit-intel model.
- Reusable guidance → [[Architecture/Enemy_AI_Objectives]]. User battle acceptance: counter composition, rally completion, screening and emergency response; no automated tests run.

## Implementation — Battle Follow-up (2026-11-01)

- Weapon profiles now apply caliber accuracy and damage biases. Counter production prices credit and population scarcity; rank before free-capacity filtering and wait for the selected reinforcement.
- Preserve early small-group activity; six available combat ships form a persistent main force with distant recruitment. Station pressure immediately concentrates combat ships and recalls ship capture detachments.
- Main force commits all ships and reserves its capture objective first. Central-relay scoring uses the station midpoint; capture detachment continues other expansion.
- Attacks fill the useful committed budget. At least 75% must assemble; large captures use the same gate. A scattered main force with no viable attack rallies rather than idling.
- Defender-specialist screens use active AttackMove with demand bounds; other capable ships attack the primary target.
- Station defense stays at home under station coverage; retain recent pressure for 20 s, recall purchased/nonhangar squadrons, prioritize home arrivals. Blocked arrivals retain paid reservations and retry.
- Other reinforcements choose the valid home/held-relay anchor nearest the fleet rally. Telemetry records selections, acceptance/rejection, completion, deployment/blockage, capacity waits and station health/loss.
- Static station DPS is not fabricated: actual defensive positioning supplies support; numeric combat estimates still exclude runtime station hardpoints.
- Reusable rules updated → [[Architecture/Enemy_AI_Objectives]]. Tests authored, not executed under checkout policy. No gameplay/win-rate claim until fresh battle evidence.

## Verification

- Battle follow-up 2026-11-01: Unity refreshed/imported scripts; compilation and updating finished, `scriptCompilationFailed=false`, error console empty. New rally/deployment methods loaded. Scoped `git diff --check` clean; independent integration review completed. Authored regression categories: EnemyAi, Combat, Ships; Abilities fixture updated for interface compatibility. Tests and Play Mode not run under checkout override.

- Target-force update 2026-11-01: Unity compilation/import complete, new runtime types loaded, error console empty and scoped diff checks clean. Regression cases authored for counter selection, force caps, retained membership, dedicated screens, assembly readiness, guard-order stability, emergency bypass and insufficient-force fallback. Tests not run.

- 2026-11-01 mining fixes: source diagnostics and scoped diff checks clean; regression cases authored, not run (local test override). Unity refresh/compilation completed; Editor idle and error console empty. No Play Mode run.
- Remaining battle gate: Ultra Hard captures/builds mines, destroys visible hostile mines after relay capture, and still responds to base pressure. This is not a claim of improved win rate.

- EditMode: all 183 enemy-AI tests, cohesion, intel-strength and counter-production tests pass (2026-10-27).
- Battle after fixes 1–5 (player Republic): AI squadron shots per loss 64→83; exchange still 2.0 → led to fixes 7–8.
- Fixes 7–8: unit tests only; not yet verified in a battle.
- Unrelated EditMode failures at commit time belong to concurrent work (Recalcitrant, Wavecrest, ship value rebalance, audio cues, selection markers, survival waves, ISD II).

## Latest Battle Findings (2026-11-01)

- Early capture/economy behavior improved; midgame still loses initiative. Battle evidence → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research#Battle 2026-11-01 — Empire vs Republic]].
- Confirmed defects/gaps: caliber modifiers missing from AI profiles; credit-only counter value with no capital capacity reserve; random relay deployment during station defense; no cross-group offensive assembly; excessive guarding in one-target raids.
- 58 CR70 /31 Centax /20 Carrack lost versus2 Venator /1 VenatorII; purchases often had ample credits. No HuntFleet/AssaultBase in group summaries.
- Defense orders exist but defenders/reinforcements are remote, and pressure interruptions cancel response. Exact120-cap use and station-death time are not available in current telemetry.
- Follow-up implemented 2026-11-01; fresh battle acceptance remains pending. Broader faction balance/Fleet Command decisions keep this parent plan active.

## Latest Battle Acceptance — 17:27 Session (2026-11-01)

- User: overall good; late-game failure remains. Full assessment and change-purpose table → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review]].
- Confirmed:205 deployed warships,36 Venator variants;122 ready targeted attacks average8.02 strike ships;81 home-defense deployments. Centre not secured; late siege defeated station.
- Fresh evidence supersedes the earlier pending-battle gate only for production/concentration/deployment. Central capture and endgame defense remain unresolved.
- Original attachment preserved → [[GameDesign/References/EaW_Global_AI_Original_Attachment]]. Reusable implementation guidance remains [[Architecture/Enemy_AI_Objectives]].

## Battle Findings (2026-11-13)

- Ship findings now have a separate owner → [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]. Keep AI objective/order work here; the dated review links the separated ship plan.

- Current review → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review 2026-11-13]]. Rapid raid/site-defense switching and Peleset/Recalcitrant wrapped-yaw contract mismatch confirmed; Recusant coverage and narrow-arc candidates documented. Retreat/order causes and battle damage impact need follow-up; production/recovery worked in this sample.
- [ ] Resolve the dated review's objective-stability and order-replacement follow-ups; no gameplay fix implemented by this audit. Broader balance scope remains active.

## TODO

- Endgame implementation and secondary tuning → [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]] (done 2026-11-02, in-game acceptance pending).
- [x] Keep station defense active from observed station damage; use threat-facing siege interception instead of unconditional station-centre orders. Implemented 2026-11-01; runtime acceptance pending.
- [x] Add emergency production deadline/queue-travel valuation and suspend nonessential economy spending during critical siege. Implemented 2026-11-01; runtime acceptance pending.
- [x] Evaluate capture clearance for capturing units; place capital escorts outside the circle; stabilize detachment objective ownership. Implemented 2026-11-01; runtime acceptance pending.
- [x] Strikecraft-specialist demand cap and raid route/local danger (2026-11-02) → [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]].
- [ ] Range/arrival factor in counter scoring (deliberately not implemented; revisit if the next battle still shows low in-range samples).

- [x] Correct combat profile caliber fidelity and population-aware capital replacement selection.
- [x] Add defense/task-force-aware deployment, persistent station defense and emergency capture recall.
- [x] Assemble combat groups for a shared main objective; bound screen demand and reassign surplus.
- [x] Extend telemetry with purchase success/rejections/capacity, spawn anchors and station lifecycle.


- [ ] Play 2–3 battles on Ultra Hard (both factions) with the fixes 7–8 build; compare via [[Tooling/Battle_Telemetry]].
- [ ] Check `_ai.csv`: attack states > 0%, capture ↔ retreat flips well below 35, early lead converted.
- [ ] Retreat turtling reproduced in the 2026-10-30 battle; remediation owner → [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]]. Covers defense/interception, local recovery, home loss, reinforcement-zone selection and idle ships; replaces the threshold-only follow-up proposal.
- [ ] Watch Droid Bomber torpedo output (104k in one battle) after the ×3 hangar change.
- [ ] AI economy changes (2026-10-28): compiled only; run EditMode tests (`EnemyProductionDecisionModelTests`, `DefendPlatformTests`) and play a battle.
- [x] Hosted AI ally station income: kept as is (user decision, 2026-10-28).
- [ ] Decide on Fleet Command strength (×0.65 damage taken) if Republic still dominates.

## Files

- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyStrategicDecisionModel.cs` — rules, press advantage, retreat memory.
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs` — thresholds.
- `Assets/Scripts/Entities/EnemyFaction/Models/FleetCohesion.cs`; `Assets/Scripts/Services/Enemy/EnemyTaskForceExecutor.cs` — rally.
- `Assets/Scripts/Services/Enemy/EnemyStrategicContextBuilder.cs` — local strength, `IsRegrouped`.
- `Assets/Scripts/Services/Enemy/EnemySquadronCommander.cs`, `CommandedSquadron.cs` — hangar escorts.
- `Assets/Scripts/Services/Enemy/EnemyProductionStrategy.cs`; `EnemyProductionDecisionModel.cs`; `EnemyCounterProductionModel.cs`; `UnitCombatProfile.cs` — production.
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset` (types 120–122); `ShipAbilityCatalog.asset` (Concentrate Fire).
