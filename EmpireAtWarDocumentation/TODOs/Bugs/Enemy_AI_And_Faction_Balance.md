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

## Verification

- EditMode: all 183 enemy-AI tests, cohesion, intel-strength and counter-production tests pass (2026-10-27).
- Battle after fixes 1–5 (player Republic): AI squadron shots per loss 64→83; exchange still 2.0 → led to fixes 7–8.
- Fixes 7–8: unit tests only; not yet verified in a battle.
- Unrelated EditMode failures at commit time belong to concurrent work (Recalcitrant, Wavecrest, ship value rebalance, audio cues, selection markers, survival waves, ISD II).

## TODO

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
