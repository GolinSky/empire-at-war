---
category: Bugs
status: todo
created: 2026-11-14
design_state: proposed
system:
  - balance
  - squadrons
  - hangars
verification: Source and serialized arithmetic; 21/21 prefab/member/weapon
  inventory parity. No tests or gameplay runs.
updated: 2026-11-14
---
# Squadron Unit Amount Rework Research

- Current exact DPS/HP/recovery/weapon and carrier targets → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]]. Count-only scenarios and earlier provisional candidates below are preserved as alternatives/history, not implementation targets.

## Goal

- Quantify the requested member counts and 1–2 squadron deployments using live serialized values on 2026-11-14.
- Owner → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]]. Analysis only; no gameplay assets changed.

## Rules

- Counts below are craft per squadron. F / I / B = fighter / interceptor / bomber; carrier historical tables often use F / B / I instead.
- Hold weapons, per-craft health, reloads and abilities unchanged for the count-only scenario. CIS midpoint = 13 / 11 / 10; retain 12–14 / 10–12 / 10 as sensitivity ranges.
- Base DPS = sum(mount count × damage × salvo shots / max(reload, (shots − 1) × shot interval)), including squadron warhead multipliers.
- Reconstructed weapon profiles from live WeaponsData families, calibers, mount barrel counts and unit multipliers, matching WeaponProfileFactory.Create and WeaponProfile.CycleSeconds. Verified all 21 squadron prefabs: health member counts and weapon hardpoints resolved by fileID exactly match baked MemberCount and weaponLoadout (17 regular types, Delta-7 and three heroes).
- New DPS = old DPS × new members / old members, assuming every added craft copies that type's complete weapon complement.
- Raw DPS is a firing-cycle ceiling, not damage delivered in a battle. Accuracy, target-class/size multipliers, shields, firing arcs, attack-run uptime, intercepted warheads, deaths, abilities and research matter. Summed laser/ion/torpedo DPS is not an anti-fighter or anti-capital score.
- Carrier tables exclude ship guns. All wings assumed full, simultaneously active and intact; one/type and two/type are scenarios, not approved targets.

## Important Values

### Standard squadrons: count-only scenario

| Faction / unit | Role | Members old → new | Raw DPS old → new | Change | Starting hull + shields old → new |
|---|---|---:|---:|---:|---:|
| Rebel X-Wing | F | 5 → 5 | 230 → 230 | 0% | 725 → 725 |
| Rebel A-Wing | I | 6 → 4 | 240 → 160 | −33.3% | 750 → 500 |
| Rebel BTL-A4 | B | 6 → 3 | 158 → 79 | −50% | 660 → 330 |
| Empire TIE Fighter | F | 7 → 10 | 132 → 188.57 | +42.9% | 294 → 420 |
| Empire TIE Interceptor | I | 8 → 8 | 160 → 160 | 0% | 256 → 256 |
| Empire TIE Bomber | B | 4 → 6 | 95 → 142.5 | +50% | 180 → 270 |
| Republic ARC-170 | F | 5 → 8 | 175 → 280 | +60% | 550 → 880 |
| Republic V-Wing | I | 5 → 6 | 195 → 234 | +20% | 420 → 504 |
| Republic BTL-B | B | 5 → 4 | 130 → 104 | −20% | 430 → 344 |
| CIS Vulture | F | 7 → 12–14 | 112 → 192–224 | +71.4–100% | 196 → 336–392 |
| CIS Belbullab-22 | I | 4 → 10–12 | 125 → 312.5–375 | +150–200% | 172 → 430–516 |
| CIS Droid Bomber | B | 4 → 10 | 78 → 195 | +150% | 148 → 370 |

- Alternate regular types, applying the same role counts: TIE Brute 6→10, 155→258.33 DPS; TIE Avenger 6→10, 147→245; TIE Punisher 4→6, 110→165; V-19 5→8, 165→264; NTB-630 4→4, 122→122.
- Hero strikecraft remain one member; unfielded Delta-7 excluded. Do not apply fighter count defaults to heroes.
- Standard squadron per-craft DPS, F / I / B: Rebel 46 / 40 / 26.33; Republic 35 / 39 / 26; Empire 18.86 / 20 / 23.75; CIS 16 / 31.25 / 19.5.
- Belbullab already exceeds Imperial interceptor per-craft DPS by 56.25%; increasing its count magnifies that existing exception to the desired quality ordering.

### Equal deployment scenarios

| Faction | Craft, one F+I+B | DPS, one F+I+B | DPS, two of each | Old DPS, one of each |
|---|---:|---:|---:|---:|
| Rebellion | 12 | 469 | 938 | 628 |
| Empire | 24 | 491.07 | 982.14 | 387 |
| Republic | 18 | 618 | 1236 | 500 |
| CIS | 32–36 | 699.5–794 | 1399–1588 | 315 |

- CIS midpoint: 34 craft, 746.75 DPS per one-of-each wing; two each = 68 craft / 1493.5 DPS.
- Equal squadron counts do not imply equal budgets: compare actual carrier prices, population, ship guns and durability separately.

### Warhead output

| Unit | Torpedo DPS old → new | Raw synchronized torpedo volley old → new |
|---|---:|---:|
| X-Wing | 20 → 20 | 200 → 200 |
| ARC-170 | 15 → 24 | 180 → 288 |
| Rebel BTL-A4 | 30 → 15 | 360 → 180 |
| Republic BTL-B | 25 → 20 | 300 → 240 |
| TIE Bomber | 20 → 30 | 240 → 360 |
| Droid Bomber | 15 → 37.5 | 180 → 450 |

- Bomber reloads are 12 s; X-Wing 10 s and ARC-170 12 s. One salvo per attack run can lower realized output.
- Count-only CIS bomber volley becomes 2.5× Rebel bomber volley. Retune torpedoes separately from lasers if bomber roles should remain comparable.

### Full live carrier inventory

- Source: Assets/Settings/Data/Ship/{Empire,Rebellion,Republic,Separatist}/*ShipData.asset.
- 34 nonempty-hangar ship assets excluding disabled ISDIII; asset presence alone does not prove a faction entry is currently purchasable.
- Existing bays only: partial-role carriers are not silently given missing roles. New counts applied to all regular variants.
- Two/type assumes total stock is raised to at least two where currently one; otherwise that scenario cannot fill. Current Reserve includes initial launches.
- CIS values use midpoint 13 / 11 / 10.

| Faction / hull | Current bays (type: active/stock) | Current wing DPS | 1/type DPS | 2/type DPS |
|---|---|---:|---:|---:|
| Empire/AcclamatorAssault | TIEFighter 3/6; TIEBomber 1/3; TIEInterceptor 1/3 | 651.00 | 491.07 | 982.14 |
| Empire/Executor | TIEFighter 4/9; TIEBomber 2/6; TIEInterceptor 2/6 | 1038.00 | 491.07 | 982.14 |
| Empire/Gladiator | TIEFighter 1/3; TIEBomber 3/6; TIEAvenger 1/3; TIEInterceptor 2/6; TIEBrute 1/3; TIEPunisher 1/3 | 1149.00 | 1159.40 | 2318.81 |
| Empire/ImperialVenator | TIEFighter 4/9; TIEBomber 3/9; TIEInterceptor 2/6 | 1133.00 | 491.07 | 982.14 |
| Empire/InterdictorStarDestroyer | TIEFighter 2/3 | 264.00 | 188.57 | 377.14 |
| Empire/ISDII | TIEFighter 3/6; TIEInterceptor 2/6; TIEBomber 2/6 | 906.00 | 491.07 | 982.14 |
| Empire/ISDI | TIEFighter 3/6; TIEInterceptor 2/6; TIEBomber 2/6 | 906.00 | 491.07 | 982.14 |
| Rebellion/BothanAssaultCruiser | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/CorellianBattlecruiser | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/Dauntless | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/Endurance | XWing 2/4; YWingBomber 1/2; AWing 1/2 | 858.00 | 469.00 | 938.00 |
| Rebellion/HomeOne | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/Liberator | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/MC75Profundity | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/MC80bRemonda | XWing 1/3; YWingBomber 1/3 | 388.00 | 309.00 | 618.00 |
| Rebellion/MC80Freedom | YWingBomber 1/1 | 158.00 | 79.00 | 158.00 |
| Rebellion/MC80Independence | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/MC80Justice | XWing 1/1; YWingBomber 1/1; AWing 1/1 | 628.00 | 469.00 | 938.00 |
| Rebellion/MediatorBattlecruiser | YWingBomber 1/1 | 158.00 | 79.00 | 158.00 |
| Rebellion/Nebula | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Rebellion/QuasarFire | XWing 1/2; YWingBomber 1/2; AWing 1/2 | 628.00 | 469.00 | 938.00 |
| Republic/Acclamator | ARC170 1/3; YWing 1/3; V19Torrent 1/3; VWing 1/3 | 665.00 | 882.00 | 1764.00 |
| Republic/Imperator | ARC170 2/6; YWing 1/3; VWing 1/3 | 675.00 | 618.00 | 1236.00 |
| Republic/Mandator | ARC170 2/6; YWing 2/6; VWing 2/6 | 1000.00 | 618.00 | 1236.00 |
| Republic/Resolute | ARC170 2/6; YWing 1/3; VWing 1/3 | 675.00 | 618.00 | 1236.00 |
| Republic/Rothana | ARC170 1/6; YWing 1/6; VWing 1/3 | 500.00 | 618.00 | 1236.00 |
| Republic/VenatorII | ARC170 4/12; YWing 2/6; VWing 2/6 | 1350.00 | 618.00 | 1236.00 |
| Republic/Venator | ARC170 3/9; YWing 2/6; VWing 2/6 | 1175.00 | 618.00 | 1236.00 |
| Separatist/Bulwark | Vulture 4/12; DroidBomber 2/8; Belbullab22 2/8 | 854.00 | 746.75 | 1493.50 |
| Separatist/Captor | Belbullab22 5/16; DroidBomber 4/16; Vulture 7/24 | 1721.00 | 746.75 | 1493.50 |
| Separatist/Invincible | Belbullab22 5/16; DroidBomber 3/12; Vulture 6/20 | 1531.00 | 746.75 | 1493.50 |
| Separatist/Lucrehulk | Belbullab22 7/24; DroidBomber 5/20; Vulture 8/28 | 2161.00 | 746.75 | 1493.50 |
| Separatist/Providence | Belbullab22 3/12; DroidBomber 3/12; Vulture 5/16 | 1169.00 | 746.75 | 1493.50 |
| Separatist/Wavecrest | Vulture 3/8; DroidBomber 1/4; Belbullab22 2/8 | 664.00 | 746.75 | 1493.50 |


### Carrier consequences

- Lucrehulk: 20 squads / 104 craft / 2161 DPS → 6 squads / 68 craft / 1493.5 DPS at two each: −70% entities, −34.6% craft, −30.9% DPS.
- Wavecrest: 6 squads / 33 craft / 664 DPS → 3 squads / 34 craft / 746.75 DPS at one each: −50% entities, +3% craft, +12.5% DPS.
- Bulwark at two each: 854→1493.5 DPS (+74.9%); Providence 1169→1493.5 (+27.8%). A uniform cap2 is not a neutral consolidation.
- Rebel standard carrier at one each: 628→469 DPS (−25.3%), 17→12 craft; at two each: 938 DPS (+49.4%), 24 craft.
- Gladiator has three fighter types and two bomber types. Two per concrete type produces 12 squads / 100 craft / 2318.81 DPS versus current 9 / 51 / 1149.
- Republic Acclamator has ARC-170 and V-19 fighter bays: two per concrete type produces eight squads, not six.
- Preserve carrier tiers through a deliberate 1/2 per-role mix, reserve endurance, replacement rate and carrier price/armament. A hard six-squadron ceiling necessarily reduces the largest carrier's active wing relative to current; document that nerf.

### Stations and purchases

- Current station active/stock: Rebel A-Wing 2/4; Empire TIE Fighter 4/12; Republic ARC-170 3/9; CIS Vulture 5/20.
- Keeping station counts produces: Rebel 12→8 craft, 480→320 DPS; Empire 28→40 craft, 528→754.29 DPS; Republic 15→24 craft, 525→840 DPS; CIS 35→60–70 craft, 560→960–1120 DPS.
- At unchanged prices, midpoint Belbullab = 343.75 DPS / 300 credits versus A-Wing 160 / 850: 6.09× raw DPS per credit. This is not a duel result, but leaving prices untouched is not balance-neutral.
- Existing purchase population remains one per ordinary squadron and max count ten; larger squadrons change power per population and per build slot too.
- Initial price/build-time sensitivity: old value × new members / old members preserves cost/time per craft only. It is a benchmark, not a final recommendation; final prices must follow damage and survivability tuning.

## Decision

- Recommended interpretation: 1–2 active squadrons per role, not per concrete variant; retain faction identity through member counts and per-craft quality. This interpretation requires a decision for mixed-role rosters before implementation.
- Use CIS 13 / 11 / 10 for the first authored candidate; test 12/10 and 14/12 sensitivity before locking it.
- Avoid shipping the count-only values unchanged. Biggest outliers: CIS interceptors, Rebel bombers, Republic fighter durability and station/purchase power.
- Minimal implementation: author concrete prefab member counts by targeted complete-craft subtree resizing and rebake derived data. Do not make the existing baked MemberCount a second conflicting runtime authority.
- Larger runtime design alternative: count-driven assembly from a reusable member prefab; only pursue if frequent runtime size changes become a requirement. Adds lifecycle, bindings, pooling, save and replication work unnecessary for this static rework.

### Provisional damage candidate, not approved balance

| Faction | F / I / B squadron raw DPS | Per-craft F / I / B DPS |
|---|---:|---:|
| Rebellion | 230 / 200 / 140 | 46 / 50 / 46.67 |
| Republic | 220 / 190 / 135 | 27.5 / 31.67 / 33.75 |
| Empire | 210 / 180 / 130 | 21 / 22.5 / 21.67 |
| CIS midpoint | 200 / 170 / 125 | 15.38 / 15.45 / 12.5 |

- Purpose: explicit starting hypothesis that restores per-craft quality ordering in every role and prevents 2–3× interceptor/bomber squadron gaps. It does not establish equal-cost or faction balance.
- Relative to count-only DPS, total-profile scale F/I/B: Rebel 1/1.25/1.7722; Republic 0.7857/0.8120/1.2981; Empire 1.1136/1.125/0.9123; CIS 0.9615/0.4945/0.6410.
- Prefer separate torpedo-volley targets first, then tune remaining lasers/ion output to the target; do not blindly scale shared family profiles or erase premium variant roles.
- Starting HP remains unequal: ARC wing 880 versus X-Wing 725, TIE 420, Vulture364. Shields take 0.5× raw fighter/interceptor laser damage before hull-class multipliers; shield regeneration and V-Wing/BTL-B repairs continue to affect sustain. Do not claim the damage candidate resolves survivability.
- Preserve hero and premium type identity; choose explicit Brute/Avenger/Punisher/V-19/NTB targets rather than flattening them to the standard type.

## Edge Cases

- Existing SquadronViewPrefabBuilder.BuildAll only covers Delta-7, Belbullab-22 and A-Wing; Populate registers one fighter.Gun per craft. Do not blindly rebuild modern multi-gun/torpedo/ion layouts with it. Resize full craft subtrees and reconstruct explicit bindings, or extend authoring support per type.
- No fixed eight-member limit found: health/flight arrays follow fighter count, FighterPoseBatch grows beyond its initial256 global slots. Pool prewarm cap12 counts squadrons, not members. Matching host/client content remains required by replicated member arrays.
- Runtime health comes from SquadronHealthComponent.fighters; MemberCount and weaponLoadout are derived AI data. Resize member objects, all health/flight/weapon/death/pose bindings and stable member IDs together.
- Formation slots grow with rank: index13 at spacing5 reaches 35 units laterally and 28 behind the leader. Review navigation radius, formation spacing, launch overlap and target reach; reducing entity count does not reduce per-craft weapon/visual cost automatically.
- Saved hull/shield arrays must match live prefab fighter count; old saves otherwise fail BattleSaveValidator. Existing parent decision excludes old-save migration during development: recommend a clearly communicated fresh-match compatibility boundary for this rework. Versioned migration is an alternative only if support is required.
- Hangar saves use bay indices and restore stock/cooldowns. Keep existing bay identity/order where possible; role sharing or roster replacement requires explicit compatibility handling. Lower caps do not delete previously deployed squads.
- Stock budget benchmark: new stock ≈ old stock × old members / new members; round deliberately and ensure stock ≥ active. Example Lucrehulk midpoint: fighter28×7/13≈15, interceptor24×4/11≈9, bomber20×4/10=8. This preserves lifetime craft approximately, unlike applying the former faction stock multipliers blindly.
- Current ship assets use 4 s initial delay and 1 s launch interval in the audited carrier set; old plan's 10/8/8 s faction intervals are historical. Shared global slots and 5 s per-bay recent-launch gating still constrain launch time.
- AI theoretical capacity is not surviving strength. Reconcile live/pending wings and usable reserves; include capture weight, population valuation, targeting/retreat estimates and purchases.
- Tests were not run under repository override; no Unity mutations or performance measurements. Arithmetic and source/serialized inspection only.

## Files

- Assets/Scripts/Entities/Squadron/Data/SquadronData.cs — baked member/loadout fields.
- Assets/Scripts/Components/Squadron/Health/SquadronHealthComponent.cs — runtime members.
- Assets/Scripts/Components/Squadron/Health/SquadronHealthModel.cs — shields, hull and attrition.
- Assets/Scripts/Components/Squadron/Flight/SquadronFormation.cs — formation footprint.
- Assets/Scripts/Components/AttackComponent/WeaponProfileFactory.cs; WeaponProfile.cs; DamageMatrixData.cs.
- Assets/Scripts/Editor/Balance/BalanceDpsEstimator.cs; BalanceDerivedData.cs.
- Assets/Scripts/Editor/Squadron/SquadronViewPrefabBuilder.cs; SquadronViewSpec.cs.
- Assets/Scripts/Services/Saves/BattleSaveValidator.cs; HangarStockMigration.cs.
- Assets/Settings/Data/Squadron/; Assets/Settings/Data/Models/Weapon/WeaponsData.asset; DamageMatrixData.asset.
- Assets/Settings/Data/Ship/; Assets/Settings/Data/Factions/; Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset.

## Important Values — Exact target validation, 2026-11-14

- Counts fixed F/I/B: Rebel5/4/3, Republic8/6/4, Empire10/8/6, CIS13/11/10. Canonical per-type values and all34 carrier rows live only in the parent plan.
- Standard whole-wing DPS with one squad per role: Rebel570, Republic545, Empire520, CIS495. Two each doubles these values; mixed carrier variants use their own specified budgets.
- Per-craft DPS F:46 >27.5 >21 >15.384615; I:50 >31.666667 >22.5 >15.454545; B:46.666667 >33.75 >21.666667 >12.5 (Rebel/Republic/Empire/CIS).
- Per-craft hull+shield HP F:110 >68 >58 >44; I:115 >72 >62 >44; B:190 >138 >100 >58. Premium variants also remain inside faction quality bands.
- Effective durability below = N×(hull/hull multiplier + shields/0.5). Raw incoming damage before hit chance/weapon size bias; no regeneration, overkill or attack-run behavior.

| Role / incoming damage | Rebel | Republic | Empire | CIS |
|---|---:|---:|---:|---:|
| F / fighter laser | 466.67 | 448.00 | 386.67 | 381.33 |
| F / interceptor laser | 625.00 | 608.00 | 580.00 | 572.00 |
| I / fighter laser | 348.57 | 315.43 | 283.43 | 276.57 |
| I / interceptor laser | 386.67 | 352.00 | 330.67 | 322.67 |
| B / fighter laser | 420.00 | 410.67 | 400.00 | 386.67 |
| B / interceptor laser | 276.00 | 272.00 | 240.00 | 232.00 |

- Illustrative squad DPS×fighter-laser durability advantage Rebel vs CIS: F≈40.7%, I≈48.3%, B≈21.7%. This is not a duel simulation; stronger Rebel squads need corresponding carrier/economic tradeoffs.
- Total shield recovery per intact standard squad, Rebel/Republic: F1.5/1.6 HP/s; I1.2/1.2; B0.6/0.6. Both are substantially below previous recovery. All passive hull repair becomes0.
- Carrier target count checked: Empire7 + Rebellion14 + Republic7 + CIS6 =34. Every supported role cap is1 or2; partial support roles stay0. Gladiator totals2F/1I/2B across five enabled concrete bays; Acclamator2F/1I/1B across four.
- Zero/zero bay source check: HangarBay fields allow0; FindLaunchBay requires stock>0 and active<cap; replacement requires positive cap; BattleAssetPreload skips it. Existing indexed bay can remain without new runtime role-sharing code.
- Verification: arithmetic and read-only source/asset review; no tests, simulations or Unity asset edits. Proposed balance remains unplayed.
