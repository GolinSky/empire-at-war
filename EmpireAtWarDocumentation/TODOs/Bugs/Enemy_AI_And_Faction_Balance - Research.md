---
type: research
category: Bugs
status: in-progress
verified: 2026-11-01
verification: Live C# and installed EaW XML inspection; no gameplay tests.
---
# Enemy AI and Faction Balance — Skirmish Research

## Goal

- Latest results, what changed/why and proposed remaining fixes → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review]].
- Complete original user attachment → [[GameDesign/References/EaW_Global_AI_Original_Attachment]].

- Compare current tactical AI with original EaW; source inspection dated 2026-11-01.
- Original data is reference only; no imported gameplay data.

## Original EaW Evidence

- Archive: `D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/config.meg`; inspected XML in memory. No matching overrides in `patch.meg`, `patch2.meg`, or `64Patch.meg`.
- `DATA/XML/AI/PERCEPTUALEQUATIONS/OFFENSIVESPACEEQUATIONS.XML:632–638`: non-Easy wants additional contestables; Easy stops after two resource structures.
- Same file, `662–684`: suppress non-refinery pads until at least one refinery exists in skirmish.
- Same file, `714–728`: refinery desire 50; local enemy fraction <0.2; enemy starbase distance >2000.
- Same file, `735–758`: open refinery pad + credits <1500 → save for refinery and suppress ordinary unit production.
- `DATA/XML/AI/GOALS/OFFENSIVESPACEGOALS.XML:5–13`: `Destroy_Unit` includes `Enemy_Unit | Enemy_Structure`; failure adjustment -15.
- Same file, `227–235`: refinery construction is `High_Priority`.
- `DATA/XML/AI/GOALFUNCTIONS/BASICOFFENSIVESPACESET.XML:110–123`: secure-pad and refinery goals link to target-specific perceptions.
- These XML rules establish explicit economy priorities and structure attack eligibility, not proof of a dedicated economic-raiding strategy.

## Mod and Proposal Boundaries

- Remake `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Scripts/Ai/Spacemode/DestroyUnit.lua`: `AotR_Destroy_Unit` requests 1–4 Corvette/Capital/Frigate, 0–5 Bombers, 1–5 Fighter/Gunship guards; Contrast 1.1–5.0.
- Mod plan produces/reinforces/attacks/releases units; guards escort. This is not verified vanilla Lua. Vanilla packaged Lua is compiled `Lup`.
- Attached research section 5's manager classes and additive attack formula are proposals, not original engine definitions.
- [Framework reference](https://steamcommunity.com/sharedfiles/filedetails/?id=1171759326): goals ranked by category; plans request task forces; tactical budgets enable categories rather than campaign-style credit allocation.

## Implementation Snapshot Before Latest Battle Follow-up

| Concern | Current behavior | Difference |
|---|---|---|
| Perception | Team sightings; visible, live, uncloaked hostile units; 1 s scan | Capture-site/zone state is globally available |
| Goals | Fixed utility rules per independent ship group | No separate goal budget allocator |
| Economy | Capture/build controllers + production priority rules | Before this fix, structure raid only scored 0.1 |
| Task forces | Stable proximity groups; target-specific strike subset and screen; one fast capture ship | No cross-map or task-owned production request |
| Unassigned units | Planner reconciles ships; squadron role assignment | Practical FreeStore analogue |
| Execution | Attack/move/capture/retreat through existing order controllers | No need for a wholesale architecture rewrite |

## Counter Selection — Initial Implementation Snapshot

- `UnitCombatProfileFactory.Create`: weapon count × damage × salvo shots ÷ reload; apply class accuracy and hull/shield multipliers. Separate piercing damage; include hangar complements.
- `CombatMatchup.Advantage`: sqrt(enemy time-to-kill us ÷ our time-to-kill enemy), clamped 0.01–100.
- `EnemyCounterProductionModel.TrySelect`: hypothetical candidate gain = Δln(advantage) ÷ price × class preference ÷ sqrt(1 + owned/reserved count).
- Class preference = (1 + 0.5 × strong-target durability share) × (1 − 0.5 × weak-target durability share).
- Fighters/interceptors counter strikecraft; corvettes counter strikecraft; bombers favor large ships/structures; frigates favor corvettes; cruisers favor frigates/corvettes; capitals favor cruisers/frigates/structures. Actual loadouts still control damage valuation.
- Unlocks, population and unit limits filter production; queued units count toward the future fleet. Affordability is checked afterward, allowing saving.
- This is aggregate damage/durability valuation. Speed, range, arcs, navigation and active abilities are not simulated.
- 2026-11-01: warship execution now selects a target-specific strike subset from each geographic group using the same combat model. Surplus guards; insufficient cohesive strike forces assemble before attacking. Target-owned production remains absent.

## Knowledge and Edge Cases

- `SelectCaptureObjective` / `AssignCaptureTargets` enumerate every zone and site without fog filtering. Ownership and build state are available globally.
- Built/construction mines cannot be captured; destroy the facility first, then capture the released site.
- Direct structure raids require current visibility. Knowing the site location does not make its hidden building a direct attack target.
- `HostileIntelGatherer`: fresh through 20 s, stale at 90 s, forgotten at 240 s; shared per team. `BuildKnown` keeps recorded strength until expiry rather than gradually discounting it.
- Base targeting uses registered base entities independently of ship-sighting visibility.
- Ultra Hard: 0.5 s decisions; attack 1.1, hunt 1.0, retreat 0.8; commits 100%; minimum 3 mines, target grows by one per two ships.
- Original-source review does not prove combat balance. Validate mining capture, destruction/rebuild, and fleet trades in actual battles.

## Files

- [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]] — broader unfinished balance/decision scope.
- [[Architecture/Enemy_AI_Objectives]] — current implementation rules.

## Battle 2026-11-01 — Empire vs Republic

- Logs: `C:/Users/golin/AppData/LocalLow/DefaultCompany/EmpireAtWar/BattleTelemetry/Loading_20261101_132417_ai.csv`, `Loading_20261101_132417_ai_orders.csv`, `Battle_20261101_132621.csv`.
- AI timeline: scaled Unity t=22.3–2151.4; damage window: 2012 unscaled seconds. Timestamps below use AI clock.
- Scope: read-only diagnosis; no gameplay changes or tests.

### Production and Counter Valuation

- 166 warship selections; 160 used counter scoring. Destruction totals corroborate 58 CR70, 31 Centax, 20 Carrack, 12 Arquitens, 2 Venator, 1 VenatorII, 4 Resolute.
- CR70/Centax/Carrack: 109/166 = 65.7%; ordinary Venators: 3/166 = 1.8%. First Venator t915.9; VenatorII t933.6; second Venator t1529.2.
- 55/58 CR70 selections had >=5800 credits; median wallet 28674. CR70 selected with 64k–73k credits at t1214–1257. Money shortage is not a sufficient explanation.
- `UnitCombatProfileFactory.Create` omits `WeaponProfile.GetAccuracyBias/GetDamageBias`, while `WeaponComponent` applies both during real combat.
- Current Heavy caliber: capital/heavy-capital damage ×1.25/×1.35; Light: ×0.9/×0.85. AI omits both advantages and penalties.
- Counter score optimizes marginal advantage per credit, not population; capacity filters candidates before scoring. Venator needs 8, CR70/Centax 3, global cap 120; no reserved capacity for a capital replacement.
- Actual cap saturation is not logged. Purchase records occur before final validation; treat aggregate purchases as selections unless corroborated by damage/lifecycle evidence.
- CR70 damage to ISDI/ISDII combined: 2503 shield+hull; Venator damage to ISDII: 19037; Resolute to ISDI/ISDII: 57449. Different exposure/positioning; not a controlled balance comparison.

### Coordination and Central Objective

- No HuntFleet or AssaultBase in any recorded group summary. Primary-state time: Capture 27.3%, Hold 22.9%, Raid 13.7%, Defend 12.7%, EngageLocal 11.8%, Retreat 11.0%, Rebuild 0.7%.
- 94/99 recorded target-force action rows selected one primary striker. At t496.2: 25 own ships, groups 12/4/4/3/2; the 12-ship raid group assigned one attacker and eleven guards.
- At t401.8: 20 own ships split across eight groups; at t1099.1: 24 split 1/9/9/3/2, with all combat groups Hold.
- Midgame order samples: t300–600 40.8% None; t600–1200 40.4% None. These are per-ship periodic sample shares, not wall-clock whole-fleet inactivity.
- Centre identified as zone:0 at (0,0,0). 68 primary CaptureZone samples target it; recorded progress remains 0.000. Attempts exist; sustained successful concentration does not.
- `EnemyShipTaskGroupPlanner`: joins/merges within800; splits beyond1600; mergers require matching state/recovery. No deliberate cross-group rally.
- Group-scoped `FleetAdvantage` compares each group's force with the entire known focus-enemy team; fragmented groups become too weak for offensive thresholds.
- Target-specific selection added only inside geographic groups. Surplus guards plus30s single-target sizing can leave large forces with one useful primary attacker. No strategic reassignment of excess force or single ownership of an offensive objective.

### Station Defense and Deployment

- Station pressure detected across12 episodes: first t581.7, final sustained episode t1145.5–1251.4.
- First episode:23 ships;14 logged defenders2592–3739 units from attacker, median2930. Final episode initial distances3369–4868, median3530; still1280–2966 at t1250.
- All9 AI ship starts t900–1251 logged x<-1548; late station attackers near x1962–2308,z1793–2094. Later position samples corroborate remote deployment.
- `ShipSpawnPoints.TryGetRandomSpawnPosition`: random allied relay first; default home only fallback. No threatened-station or assigned-force anchor input.
- Pressure requires a currently visible live attacker in station attack reach. Brief visibility/range loss removes defense; observed Defend→Hold→Defend interruptions stop/reissue ships.
- Capture detachments remain excluded from emergency base defense. Station/platform strength is omitted from mobile-force comparison.
- Logs do not expose actual120-cap usage or selected spawn relay. Structure damage targets aggregate as Structure; exact station-death time cannot be established.

### Recommended Follow-up

1. Correct profile damage/accuracy caliber modifiers; improve population-limited production and reserve capacity for selected capital replacements.
2. Route reinforcements to threatened station, otherwise explicit task-force rally; random relays only fallback.
3. Retain station-defense intent across brief contact loss; use station weapon support only inside actual coverage; allow emergency capture-detachment recall.
4. Add deliberate cross-group assembly for one main objective (centre/station), keeping small capture/raid detachments. Judge the assembled force against local objective opposition.
5. Bound screening by actual defender demand; reuse surplus as offensive reinforcements/another task instead of unlimited passive guards.
6. Log successful purchase/completion, rejected candidates/capacity/scores, spawn anchor, station health and offensive objective rejection.

## Follow-up Implementation (2026-11-01)

- Audited production, deployment, defense and assembly defects addressed through existing systems; implementation details → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance#Implementation — Battle Follow-up (2026-11-01)]].
- Station support uses home-position combat orders; no fabricated station DPS. Strategic fleet thresholds still compare combined main force against remembered hostile fleet; per-target launch checks use local defenders.
- Original logs are baseline evidence, not validation of the new implementation. Fresh battle required for composition, central commitment and station defense outcomes.
