---
category: Bugs
status: in-progress
design_state: implemented-balance-rejected
created: 2026-11-11
system:
  - balance
  - squadrons
  - hangars
acceptance: failed
---
# Carrier and Squadron Rebalance

## Goal

- Every one of the 29 audited carrier hulls launches a fighter, bomber and interceptor. Liberator at **1 / 1 / 1 active** is the minimum.
- Simultaneous capacity and battle stock order → CIS > Empire > Republic > Rebellion.
- Regular squadron quality within each role → Rebellion > Republic > Empire > CIS. Keep existing squadron member counts.

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
