---
category: Bugs
status: todo
created: 2026-11-12
---
# Carrier and Squadron Rebalance — Audit

## Goal

- Explain fresh-match Republic/Rebellion air presence reported 2026-11-12; parent → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]].

## Evidence

- Original balance commit `bf632b832` → 2026-11-11 13:36:44 +0200. The two battle logs below postdate it.
- Telemetry directory → `C:/Users/golin/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/`.
- `Battle_20261112_132136.csv` → 533 unscaled seconds; Republic regular squadrons killed 57 CIS squadrons; CIS regular squadrons killed 14 Republic squadrons. All weapon sources: CIS lost 101, Republic 76; ships/stations credited 44 CIS and 58 Republic kills, Jango credited 4 Republic kills.
- `Battle_20261111_204932.csv` → 831 unscaled seconds; Rebel regular squadrons killed 71 Empire squadrons; Empire regular squadrons killed 32 Rebel squadrons. All sources: Empire lost 143, Rebels 117; ships/stations credited 69 Empire and 85 Rebel kills, Han Solo credited 3 Empire kills.
- Counts are whole-squadron final blows: `WeaponComponent.CommitImpact` records `attackData.TargetHealth.IsDestroyed`; `SquadronHealthComponent.IsDestroyed` reflects the model, which dies at `AliveCount == 0`. Not individual craft kills; not normalized duel results.
- Logs contain all three regular CIS roles and Empire fighter/bomber/interceptor firing. They establish launch/combat participation and lopsided squadron exchanges, not initial spawn totals, peak live counts, or causal shares.
- Latest `Battle_20261112_145112.csv` includes a Republic-owned Malevolence credited with 57 CIS squadron kills; that match is not clean evidence for Republic squadron superiority.

## Implementation

- Read-only serialized comparison: 29 original carrier cap/stock/timer sets match; 17 squadron count/role/hull/shield/recovery/speed sets match. No new weapon DPS rebake, tests or gameplay run performed.
- Current deployable one-of-each roster totals (active/stock): CIS 66/264, Empire 40/120, Republic 38/114, Rebellion 38/73.
- Outside original table: Rebel MC80 Justice +3 active, MC80 Freedom +1, Mediator +1, Remonda +2; Imperial Interdictor +1. Matching view prefabs contain bound hangars and launch points. These totals are roster sums, not equal-cost fleets.

### Survival

| Full squadron | Starting hull + shield HP | Intact squad shield recovery HP/s |
|---|---:|---:|
| Vulture | 196 | 0 |
| TIE Fighter | 294 | 0 |
| ARC-170 | 550 | 15 |
| X-Wing | 725 | 20 |
| Belbullab-22 | 172 | 2 |
| TIE Interceptor | 256 | 0 |
| V-Wing | 420 | 10 |
| A-Wing | 750 | 18 |

- Raw HP totals understate shield advantage: fighter lasers do 0.5× raw damage to shields versus 1.5× to fighter hull; interceptor lasers also do 0.5× to shields. See `DamageMatrixData.asset:340–393`, `SquadronHealthModel.ApplyDamage`.
- Recovery continues during combat; `SquadronHealthComponent.Tick` gates shield recovery on its timer/suppression, not a time-since-hit cooldown. V-Wings and Republic Y-Wings also repair surviving hull.
- These are source-derived durability advantages, not simulated time-to-kill. Damage, accuracy, maneuvering, research, abilities and supporting ships affect actual exchanges.

### Launch throughput

- One automatic squadron per carrier per interval: CIS 6 s, Empire 10 s, Republic/Rebellion 8 s; initial delay 4 s.
- Lossless example: Lucrehulk reaches 18 active squads at 106 s; Liberator reaches 3 at 20 s. Larger maximum capacity does not give a proportional opening launch rate.
- `HangarModel.SquadronLost:173–176` resets the **shared** cooldown to at least the launch interval on every squadron death. Deaths more frequent than that interval can continually delay every bay, even with unused stock and active capacity.
- `FindLaunchBay:136–148` retains each launch in a 60 s recent-launch list; squadron death does not remove it. Early deaths can leave bays empty until recent launches age out.
- `EnemyProductionStrategy.CountHangarSquadrons:587–599` counts reserved carrier count × theoretical hangar capacity, not actual surviving wings or remaining stock. Normal direct squadron purchase room is one per two ships, including those theoretical hangar counts; critical-station/no-ship fallbacks can bypass it.

### Quantity coverage

- Stations were explicitly excluded by the plan: every faction gets 2 active / 6 stock, with 1 s initial delay and 3 s interval. Rebel stations launch A-Wings; Republic ARC-170; Empire TIE Fighter; CIS Vulture. Equal quantity inherits the new unequal quality.
- Ordinary purchased squads retain capacity 1 and limit 10. ARC-170 costs 375 / 8 s build versus Vulture 350 / 8 s and TIE Fighter 300 / 10 s. Rebel bomber costs 500 / 15 s versus TIE Bomber 550 / 17 s.
- Carrier-only reductions therefore do not guarantee smaller faction-wide live air forces.

## Decision

- Reopen parent balance plan; keep original implementation and historic verification evidence.
- Why → existing battle telemetry supports stronger Republic/Rebel squadrons winning the air exchange; launch pacing and recovery constrain weaker factions despite larger reserve/cap figures.
- Avoid → blindly increasing stock or assuming a faction-specific spawn failure. No faction gate was found in the shared automatic launch path.
- Remaining → approved target retuning and timeline measurements of launched/live/lost/replaced squads in equal-cost matchups. No gameplay values changed during this diagnostic task.

## Follow-up implementation — 2026-11-12

- The findings above describe the pre-fix audit. Implemented: six CIS carrier intervals 1 s; `SquadronLost` only decrements active count; recent-launch window 5 s; stations active/stock CIS 5/20, Empire 4/12, Republic 3/9, Rebellion 2/4.
- Global `SquadronLaunchSlots` remains limited to 10 launches/s across the battle without a fair queue. Ten or more continuously launching CIS carriers plus other producers can saturate it; nominal 1 s per carrier is not guaranteed under contention.
- New station complements have raw starting hull+shield pools CIS 980, Empire 1,176, Republic 1,650, Rebellion 1,500. Recovery and shield resistance still favor Republic/Rebellion; quantity ordering alone does not guarantee air superiority.
- Non-CIS automatic launches remain interval-limited (Empire 10 s, Republic/Rebellion 8 s), even though the recent-launch window is 5 s.
- Existing station saves restore their old remaining reserve and cooldown. Fresh matches use new stock. No station stock migration added; this preserves spent-stock saves rather than inventing replenishment.
- Existing AI purchase room counts reserved carriers × theoretical hangar capacity, including ships with destroyed bays or exhausted stock. Further work must use appropriate live/pending air strength to avoid suppressing useful replacements.
- Tests authored/updated, not executed. No quality, direct-purchase, global-throttle or save migration changes in this follow-up.

## Empire versus CIS — 2026-11-12 15:54

- Sources: `Battle_20261112_155400.csv`, `Loading_20261112_155240_ai.csv`, `Loading_20261112_155240_ai_orders.csv` in the telemetry directory above. Combat duration 1,170 unscaled seconds (19:30). Price update in this task postdates the battle.
- Regular-squadron final blows against regular squads: Empire 319, CIS 199. Damage to regular squads: Empire 57,782, CIS 53,255 shield+hull HP. All-weapon regular squad losses: Empire 398, CIS 536.
- Leading squadron killers: TIE Interceptor 141, TIE Fighter 137; Vulture 116, Belbullab-22 59, Droid Bomber 24. Boba adds 24 CIS squad kills outside the regular exchange.
- Ships/stations killed 193 CIS squadrons and 199 Imperial squadrons. Leading Imperial AA: ISD I 61, Acclamator Assault 32, Gladiator 23. Leading CIS AA: station 38, quad-laser satellites 32, Dispatcher 24.
- Regular squad damage to ships/structures: Empire 61,500 HP / 6 final blows, CIS 37,724 HP / 2 final blows. Damage totals sum rounded CSV shield/hull losses; these are mixed-fleet outcomes, not controlled duels.
- CIS AI purchased two Vultures at old price 350; logged times 53.0 and 128.2. Carrier purchases: Bulwark 10, Providence 7, Wavecrest 5, Invincible 4, Captor 1, Lucrehulk 0. AI companion covers CIS only.
- Final AI state: no combat ships or living production station. Late samples select Lucrehulk with population 114/120, capacity cost 25, funds 460–654 versus price 15,000; possible production-selection issue, not enough adjacent samples to establish a prolonged stall.
- Conclusion: CIS air support participated heavily; Empire achieved more squadron kills. Price changes alone may have limited effect because CIS bought only two regular squads. Logs lack launch requests, wait time and live/peak counts; no global launch bottleneck is established.


## Historical plan snapshot — before exact target sheet, 2026-11-14

- Preserved prior targets, implementation history and unresolved acceptance. Superseded for new balance targets by [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]]; historical test results below are not verification of the new proposal.


- Current proposed direction (2026-11-14) → unit-amount rework below; calculations and full live carrier inventory → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance - Unit Amount Research]]. Earlier target tables and verification are dated history, not the current asset snapshot.

## Goal

- Every one of the 29 audited carrier hulls launches a fighter, bomber and interceptor. Liberator at **1 / 1 / 1 active** is the minimum.
- Simultaneous capacity and battle stock order → CIS > Empire > Republic > Rebellion.
- Regular per-craft quality within each role → Rebellion > Republic > Empire > CIS. The 2026-11-14 proposal replaces the earlier fixed-member-count constraint with faction/role member counts.

## Rules

- Carrier scope → playable ship roster with deployable hangar bays, including existing hero carriers. Station quantities were added in the 2026-11-12 follow-up. Hero strikecraft, disabled ISD III, unfielded Delta-7 and ships with empty bays are outside this balance pass. Malevolence hangar integration remains in [[TODOs/Features/Malevolence_Import]].
- Active = simultaneous squads. Stock = total launches including the initial deployment. Stock per bay → CIS 4× active, Empire and Republic 3×, Rebellion 2×. A new Liberator can launch three initial squads and three replacements.
- Per-hull targets apply to each hull in a grouped row. Fleet totals use one of each hull. Actual match totals depend on fleet composition.
- Round-robin automatic launch selection across eligible bays. Keep the shared launch throttle, 5 s per-bay recent-launch limit and existing reserve/hardpoint checks. Start delay 4 s for all carriers. Interval → CIS 1 s, Empire 10 s, Republic and Rebellion 8 s. Ability-driven launches keep their existing timing rules.

## Important Values

### Carrier totals

| Faction | Hulls with all roles | Active F / B / I | Active total | Battle stock | Active individual craft |
|---|---:|---:|---:|---:|---:|
| CIS | 6 | 27 / 18 / 21 | 66 | 264 | 345 |
| Empire | 6 | 15 / 13 / 11 | 39 | 117 | 243 |
| Republic | 7 | 17 / 11 / 10 | 38 | 114 | 190 |
| Rebellion | 10 | 11 / 10 / 10 | 31 | 62 | 175 |

### Carrier bays

| Faction | Hull | Active F / B / I | Stock F / B / I |
|---|---|---:|---:|
| CIS | Captor | 6 / 4 / 4 | 24 / 16 / 16 |
| CIS | Lucrehulk | 7 / 5 / 6 | 28 / 20 / 24 |
| CIS | Providence | 4 / 3 / 3 | 16 / 12 / 12 |
| CIS | Invincible | 5 / 3 / 4 | 20 / 12 / 16 |
| CIS | Bulwark | 3 / 2 / 2 | 12 / 8 / 8 |
| CIS | Wavecrest | 2 / 1 / 2 | 8 / 4 / 8 |
| Empire | Acclamator Assault | 2 / 1 / 1 | 6 / 3 / 3 |
| Empire | Imperial Venator | 3 / 3 / 2 | 9 / 9 / 6 |
| Empire | ISD I, ISD II | 2 / 2 / 2 | 6 / 6 / 6 |
| Empire | Executor | 3 / 2 / 2 | 9 / 6 / 6 |
| Empire | Gladiator | 3 / 3 / 2 | 9 / 9 / 6 |
| Republic | Venator | 3 / 2 / 2 | 9 / 6 / 6 |
| Republic | Venator II | 4 / 2 / 2 | 12 / 6 / 6 |
| Republic | Resolute, Acclamator, Imperator | 2 / 1 / 1 | 6 / 3 / 3 |
| Republic | Mandator | 2 / 2 / 2 | 6 / 6 / 6 |
| Republic | Rothana | 2 / 2 / 1 | 6 / 6 / 3 |
| Rebellion | Endurance | 2 / 1 / 1 | 4 / 2 / 2 |
| Rebellion | Liberator, Quasar Fire, Corellian Battlecruiser, Dauntless, Bothan Assault Cruiser, MC75 Profundity, Home One, MC80 Independence, Nebula | 1 / 1 / 1 | 2 / 2 / 2 |

- CIS bays → Vulture fighter, Droid Bomber, Belbullab-22 interceptor. Change Belbullab runtime class to Interceptor and use a unit-scoped interceptor laser variant.
- Empire standard bays → TIE Fighter, TIE Bomber, TIE Interceptor. Gladiator split → TIE Fighter / Brute / Avenger 1 each, TIE Bomber 2, Punisher 1, TIE Interceptor 2. Each Gladiator bay uses 3× its active count as stock.
- Republic standard bays → ARC-170, Republic Y-Wing, V-Wing. Acclamator splits its two fighter slots into 1 ARC-170 and 1 V-19 Torrent, each with stock 3. NTB-630 remains available through direct purchase.
- Rebel bays → X-Wing, BTL-A4 Y-Wing, A-Wing.

### Squadron stats

- HP and recovery values are per craft. Recovery columns give shield regeneration and hull repair in HP/s. A-Wing and Belbullab-22 serialize 6 and 1 HP per 2-second shield tick, respectively. Speeds are cruise and combat values in Unity units/s.
- DPS is full-strength squadron base damage per second summed across mounted weapons. Torpedo volley is full squadron raw damage from one synchronized torpedo salvo. Values are before accuracy, target-class multipliers, ability effects and research. Keep weapon targeting rules, cycle times and member counts. Unlisted stats keep their current values.

| Role | Faction | Squadron | Craft | Hull / shield | Shield regen / hull repair | Cruise / combat | Base DPS | Torpedo volley |
|---|---|---|---:|---:|---:|---:|---:|---:|
| Fighter | CIS | Vulture | 7 | 28 / 0 | 0 / 0 | 54 / 60 | 112 | — |
| Fighter | Empire | TIE Fighter | 7 | 42 / 0 | 0 / 0 | 56 / 63 | 132 | — |
| Fighter | Empire | TIE Brute | 6 | 52 / 0 | 0 / 0 | 55 / 62 | 155 | — |
| Fighter | Empire | TIE Avenger | 6 | 44 / 10 | 1 / 0 | 67 / 75 | 147 | — |
| Fighter | Republic | ARC-170 | 5 | 80 / 30 | 3 / 0 | 69 / 78 | 175 | 180 |
| Fighter | Republic | V-19 Torrent | 5 | 72 / 25 | 2 / 0 | 71 / 81 | 165 | — |
| Fighter | Rebellion | X-Wing | 5 | 95 / 50 | 4 / 0 | 75 / 86 | 230 | 200 |
| Interceptor | CIS | Belbullab-22 | 4 | 38 / 5 | 0.5 / 0 | 62 / 70 | 125 | — |
| Interceptor | Empire | TIE Interceptor | 8 | 32 / 0 | 0 / 0 | 70 / 78 | 160 | — |
| Interceptor | Republic | V-Wing | 5 | 62 / 22 | 2 / 1 | 76 / 84 | 195 | — |
| Interceptor | Rebellion | A-Wing | 6 | 85 / 40 | 3 / 0 | 83 / 94 | 240 | — |
| Bomber | CIS | Droid Bomber | 4 | 32 / 5 | 0.5 / 0 | 29 / 33 | 78 | 180 |
| Bomber | Empire | TIE Bomber | 4 | 45 / 0 | 0 / 0 | 31 / 36 | 95 | 240 |
| Bomber | Empire | TIE Punisher | 4 | 48 / 12 | 1 / 0 | 31 / 36 | 110 | 276 |
| Bomber | Republic | Y-Wing | 5 | 62 / 24 | 2 / 1 | 34 / 39 | 130 | 300 |
| Bomber | Republic | NTB-630 | 4 | 60 / 22 | 2 / 0 | 34 / 39 | 122 | 288 |
| Bomber | Rebellion | BTL-A4 Y-Wing | 6 | 70 / 40 | 3 / 0 | 38 / 44 | 158 | 360 |

### Fighter torpedoes

- X-Wing → one ship-targeting proton torpedo launcher per fighter, 40 damage, 10 s reload, one shot per attack run. Full squadron → 200 volley, 20 torpedo DPS, 210 laser DPS, 230 total base DPS.
- ARC-170 → 36 damage per torpedo, 12 s reload, 180 volley, 15 torpedo DPS, 175 total base DPS.
- BTL-A4 and BTL-B Y-Wings use proton torpedoes. Their full-squadron volleys are 360 and 300, respectively, at 12 s reload.

## Implementation

1. Apply bay targets to ship assets. Preserve existing bay indices and types. Append missing bays and validate every active cap against stock. For Executor, Quasar Fire, Bothan Assault Cruiser and Nebula, retain independent fighter/bomber hardpoints and bind the added interceptor bay to the existing fighter hardpoint and launch point. Extend per-bay hardpoint handling to toggle all linked bays when a shared hardpoint changes.
2. Rotate automatic selection after each successful automatic launch. Skip bays that are disabled, empty, full or cooling down. Verify a fresh, lossless Captor launches all three roles by about 6 s and Liberator by about 20 s, measured from the start of the hangar timer with an available global slot.
3. Set the squadron data fields. Tune weapon damage using unit-scoped definitions for variants that need individual values. Set torpedo damage to the volley target first. Scale the other damage-bearing mounts by the ratio of remaining target DPS to their current summed DPS. Keep mount counts, reloads and firing geometry. Re-bake and reconcile live mount values with AI combat profiles and the balance preview. Update role and count descriptions in the faction data.
4. Migrate old hangar saves before validation. Keep old indices and tracked squadron IDs. For each existing bay use remaining stock = max(0, new stock − max(0, old starting stock − old saved remaining stock)). Initialize appended bays at their new stock. Keep already-launched units and let any over-cap bays fall below the new cap through attrition. Preserve saved launch cooldown. Use pre-change per-hull stock as migration input, including the four independently bound hulls.
5. Verify 29/29 carrier role coverage, the exact bay and squadron targets, correct hardpoint damage behavior, and save/load with deployed squadrons and spent stock. Source-verify Squadrons, Balance, Combat, Ships, GameFlow and EnemyAi paths. Compare equal-cost fleets at levels 2, 3 and 5 and compare each role head-to-head with base stats and with abilities/research. Record spawn times, peak craft count, losses, replacements and anti-ship damage before signing off on balance.

## Files

- `Assets/Settings/Data/Ship/`, `Assets/Settings/Data/Squadron/`, `Assets/Settings/Data/Factions/`
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`, `DamageMatrixData.asset`
- `Assets/Scripts/Components/Hangar/HangarModel.cs`, `HangarComponent.cs`
- `Assets/Scripts/Services/Saves/BattleSaveValidator.cs`, `HangarSave.cs`, `LaunchedSquadronSave.cs`
- [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]] — historical CIS buffs and broader AI scope.

## Verification

- Imported, explicitly reserialized and saved 70 affected Unity assets. Editor Console reported zero errors.
- Editor asset inspection verified 29/29 carrier loadouts against exact caps, stock and timers. Checked squadron asset/view addresses, all three runtime roles per carrier, and every hangar’s bound launch point and applicable hardpoints. Four independent-bay prefabs share their fighter exit with the interceptor bay.
- Verified 17/17 squadron stat sets and live mount counts against baked loadouts. Maximum floating-point deviation was 0.000016 base DPS and 0.000031 torpedo volley damage.
- Source-verified version 4 stock migration before save validation and preservation of tracked squadrons and cooldowns. Captor's first three automatic launch times derive as 4/10/16 s and Liberator's as 4/12/20 s when shared slots are available.
- Reusable combat and carrier rules recorded in [[GameDesign/Combat]].
- Equal-cost fleet balance, live bay destruction, X-Wing torpedo firing and legacy-save load remain in [[TODOs/User_Acceptance_Queue]].
- X-Wing prefab has five `FighterTorpedoHardPoint` launchers, indexed by fighter and registered for attack runs, weapon firing, dependency injection and fighter death. Baked loadout matches 20 laser mounts and 5 proton torpedo launchers.
- Final Editor asset inspection confirmed 29/29 carrier loadouts and 17/17 squadron variants, including 230 X-Wing base DPS and 200 torpedo volley damage.
- EditMode passes → Balance 99/99, Squadrons 83/83, Combat 240/240, GameFlow 177/177. Includes the fighter torpedo and version 4 hangar migration cases.
- Ships 138/141 → existing asset/fixture mismatches on Allegiance ion mounts, ISD II hull and Procursator hull. EnemyAi 339/343 → four `PreferredShipAtLimit` cases hit the existing second-station purchase branch before ship selection.

## Review — 2026-11-12

- Reopened after user reported fresh-match CIS < Republic and Empire < Rebellion visible squadron counts. Initial implementation completed 2026-11-11; combat balance acceptance failed.
- Read-only audit matches all original 29 carrier cap/stock/timer sets and all 17 squadron count/role/durability/recovery/speed sets. Mounted DPS was not independently re-baked in this review. No tests or new gameplay runs executed.
- Existing post-rebalance telemetry confirms all CIS and Empire regular roles fire; regular squadron kill exchanges favor Republic 57:14 and Rebellion 71:32. Detailed evidence → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance - Research]]. These are whole squadron kills, not individual craft or spawn counts.
- Causes: large durability/DPS/recovery advantage; shields take 0.5× fighter/interceptor laser raw damage; every squadron loss resets the carrier-wide launch wait; 60 s recent-launch gate limits replacements; station counts and purchase limits do not enforce faction quantity hierarchy.
- Current deployable roster includes four additional Rebel carriers and Imperial Interdictor outside the 29-hull table: current one-of-each active totals CIS 66, Empire 40, Republic 38, Rebellion 38. These totals do not predict equal-cost fleets or live survivors.
- Reusable verified rule recorded in [[GameDesign/Combat]]: maximum hangar capacity is not current air strength; assess launch throughput and survival together.

## TODO

- [ ] Reconcile current playable carrier coverage and faction quantity targets, including stations and direct purchases.
- [x] Remove the shared cooldown reset on squadron death; recent-launch window 60 → 5 s; six CIS carrier intervals 6 → 1 s (2026-11-12).
- [ ] Retune quality versus quantity using effective shield durability, regeneration and anti-squadron damage; preserve the requested quality ordering.
- [ ] Recheck equal-cost fleets at levels 2/3/5 and role matchups, recording launch/live-count/loss/replacement timelines. Existing telemetry lacks launch and peak-count histories.
- [ ] Retain unresolved launch-role/shared-hardpoint/legacy-save/X-Wing torpedo acceptance checks from the original review.

## Follow-up — 2026-11-12

- User-requested implementation: CIS carrier intervals 1 s; loss leaves the shared timer unchanged; per-bay recent-launch window 5 s globally. Initial carrier delay remains 4 s. Empire/Republic/Rebel intervals remain 10/8/8 s.
- Stations, active / total stock: CIS 5/20, Empire 4/12, Republic 3/9, Rebellion 2/4. Stock follows faction multipliers 4×/3×/3×/2×. Station delay/interval remain 1/3 s.
- Updated Squadrons regression assertions for fast launch history, cross-bay loss, elapsed-timer replacement, restored launches and hangar repair. Tests not run under local override.
- Remaining findings → battle-wide launch ceiling 10/s; AI counts theoretical carrier capacity after attrition/exhaustion; old saves retain saved stock/timer; quality gap and direct-purchase equality remain. Details in research companion.
- Broader plan remains active: quality retuning and full current-roster quantity reconciliation are not implemented by this scoped follow-up.
- Follow-up verification: Unity explicitly reserialized and saved all seven changed assets; live inspection confirmed six 1 s carrier intervals and all four station cap/stock pairs. Compiled `RELAUNCH_COOLDOWN` reads 5; source/diff checks passed. No affected import/serialization errors observed. Shared Editor Console contains unrelated test teardown errors from concurrent work; this task did not execute tests.

## Purchase prices — 2026-11-12

- Applied user-approved prices (credits/squadron): CIS Vulture 200, Belbullab-22 300, Droid Bomber 300; Empire TIE Fighter 300, Interceptor 450, Bomber 450, Brute 450, Avenger 550, Punisher 600; Republic V-19 500, ARC-170 600, V-Wing 650, NTB-630 550, BTL-B 650; Rebellion X-Wing 800, A-Wing 850, BTL-A4 850.
- Four faction assets, 14 changed price fields; three regular prices already matched. Hero prices, build times, limits, capacity and combat stats unchanged.
- Unity refreshed, explicitly reserialized and saved all four faction assets. Live serialized inspection verified all 17 regular prices and unchanged heroes; Console returned no errors; diff whitespace check passed. Balance verification was asset inspection only; tests not run.
- User decisions: keep global launch throttle; strength balance awaits user playtest; old-save migration is out of scope during development. Live/pending hangar strength for AI purchases remains proposed, not implemented.

## Decision — Unit amount rework, 2026-11-14

- User request: fewer squadron entities; faction quantity represented by members per squadron; carriers deploy 1–2 squadrons per type. This pass creates calculations and an implementation plan only.
- Proposed member counts F / I / B: Rebel5/4/3; Empire10/8/6; Republic8/6/4; CIS13/11/10 midpoint, with12–14/10–12/10 sensitivity.
- Recommended interpretation: 1–2 per role to keep ordinary three-role carriers at3–6 squads. Concrete-type interpretation remains an alternative: Gladiator's six types would produce6–12 squads. Decide mixed variants explicitly before implementation.
- Minimum-change path: targeted full-craft prefab resize + explicit bindings + rebake; retain baked MemberCount as derived data. Existing generic builder is incomplete for modern weapon layouts; do not blanket-rebuild.
- Count-only standard raw F/I/B DPS: Rebel230/160/79; Empire188.57/160/142.5; Republic280/234/104; CIS192–224/312.5–375/195. Count-only change is not recommended without weapon/health/economy review.
- Research includes provisional damage targets, all34 nonempty-hangar ship assets excluding disabled ISDIII, stations, stock formulas and carrier tier tradeoffs. These are proposals, not implemented or approved values.
- Current live assets supersede historical quantities: Lucrehulk8F/7I/5B,20 active; current audited carriers use4s initial/1s interval. No gameplay change made by this analysis.
- Keep old-save migration outside development scope as previously recorded; communicate a fresh-match compatibility boundary. Migration remains an alternative if that product requirement changes.
- Reusable findings remain with this active plan/research; no new gameplay rules published as implemented.

## Implementation — Unit amount rework

1. **Freeze a coherent target sheet.** Choose exact CIS counts; map all17 regular variants; preserve three heroes and exclude unfielded Delta-7. Assign per-hull role limits and stock, including partial-role support ships. Verify summed role counts, craft, raw/target-adjusted damage, volley, HP and price/population for each carrier.
2. **Resolve carrier tiers and mixed variants.** Candidate light/support carriers1/1/1; dedicated carriers2/2/2; mixed2/1/1 or2/2/1 where budgets fit. Wavecrest1/1/1 and Lucrehulk2/2/2 illustrate the new range; Lucrehulk loses30.9% raw wing DPS at unchanged per-craft stats. For Gladiator/Acclamator, either select up to two variant squads per role or add shared role capacity; preserve premium roster access. Decide this before indexed bay changes.
3. **Resize actual members.** Clone/remove complete craft subtrees with all weapons, assign contiguous IDs, rebuild health/flight/death/weapon/pose/entity references. Preserve torpedoes, ion cannons, rear guns, abilities and visual setup. Rebake MemberCount/loadout and compare all17 regular types with expected per-craft composition; heroes remain unchanged.
4. **Tune damage and survivability.** Choose torpedo volleys separately, then laser/ion damage; use unit-scoped definitions. Evaluate the research's provisional role DPS table, effective shield durability, regeneration, casualty-driven damage decline and research/ability variants. Avoid shared-family changes that affect unrelated units.
5. **Retune deployment, reserve and economics together.** Set each carrier's active role limits, total-launch stock and replacement cadence. Review station caps/stock explicitly, direct-purchase price/build time/population/limits and faction descriptions. Stock benchmark = old stock×old members/new members; ensure stock≥active. Preserve the global launch throttle unless separately authorized.
6. **Reconcile consumers and compatibility.** AI uses live/pending wing strength and usable reserves rather than theoretical capacity; review capture/retreat/population weighting. Preserve bay identities or explicitly version the change. Verify fresh save/load and matching host/client member-array contracts; document old-save incompatibility without inventing replenishment.
7. **Persist and inspect.** Refresh/import only affected assets, force-reserialize explicit changed paths, SaveAssets and inspect Console. Verify member/binding/loadout parity, role caps, reserve accounting, all weapons disabled on member death, pooled respawn and formation footprint at14 craft.
8. **Acceptance and measurements.** Compare equal-cost/population fleets at levels2/3/5, role duels, carrier opening/replacement timelines, station defense and direct purchases. Record live squads AND craft, per-member losses, torpedo volleys, damage by target class, expenditure and p95 frame time. Fewer entities is not proof of lower craft/weapon cost.

## TODO — Unit amount rework

- [x] Read live squadron data and weapon rules; reconstruct baseline/count-only DPS and torpedo output.
- [x] Verify21/21 prefab member and weapon inventories against baked data; inspect formation, save and networking count contracts.
- [x] Calculate1/type and2/type carrier scenarios; identify mixed-role, station, reserve, economy and builder issues.
- [ ] Decide exact count, role/type cap, per-hull tier and compatibility targets.
- [ ] Implement prefab/binding/loadout and scoped weapon/stat changes.
- [ ] Implement carrier/station/economy/AI updates and matching descriptions.
- [ ] Complete persistence/source verification and acceptance evidence.
- Relevant verification categories: Squadrons, Balance, Combat, Ships, GameFlow, EnemyAi; Networking/Capture/Economy/Ui only if those paths change. Tests not run; repository override prohibits execution without explicit user request.
