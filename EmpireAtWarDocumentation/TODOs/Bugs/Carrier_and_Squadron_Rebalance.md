---
category: Bugs
status: in-progress
design_state: exact-targets-proposed
created: 2026-11-11
system:
  - balance
  - squadrons
  - hangars
acceptance: failed
updated: 2026-11-14
---
# Carrier and Squadron Rebalance

## Goal

- Reduce squadron entities using faction-specific craft counts and 1–2 active squadrons per supported role.
- Per-craft quality: Rebellion > Republic > Empire > CIS for base DPS, hull+shield HP and effective durability against common strikecraft lasers.
- Exact proposed targets dated 2026-11-14. No gameplay changes applied; balance requires match acceptance.

## Decision

- This note is the authoritative proposed target sheet; supersedes earlier count-only and provisional targets. F / I / B always means fighter / interceptor / bomber.
- Craft per squadron: Rebel 5/4/3; Republic 8/6/4; Empire 10/8/6; CIS **13/11/10**. CIS ranges are resolved to these fixed values.
- Role caps aggregate concrete variants. Use explicit bay assignments below; no new runtime shared-role-cap system required.
- Dedicated carriers receive larger wings than general gunships. Four existing partial-role supports retain absent roles at zero; no new role or launch hardpoint is added to them.
- Three hero strikecraft retain one member and current stats. Unfielded Delta-7, disabled ISD III and unfinished Malevolence hangar integration remain excluded.
- Existing save migration remains outside development scope: require fresh matches across the member-count change; preserve clear incompatible-save rejection and matching host/client content.
- Earlier audit, implementation and acceptance history → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance - Research]]. Count-only baseline/full carrier comparisons → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance - Unit Amount Research]].

## Important Values

### Squadron targets

- DPS = full-strength squadron raw firing-cycle DPS, before accuracy, target multipliers, attack-run downtime, research and abilities.
- Hull and shields are **per craft**. Squad HP = members × (hull + shields); shield resistance makes raw HP totals insufficient for comparison.
- Squad DPS is the exact authoring target; DPS/craft is displayed rounded to four decimals. Use the exact ratio when allocating mount damage.
- All 17 regular types are specified, including premium variants. Per-craft ordering also holds across the listed variants within each role.

| Faction / unit | Role | Craft | Squad DPS | DPS/craft | Hull/craft | Shields/craft | Squad hull+shields | Shield HP/s/craft |
|---|---|---:|---:|---:|---:|---:|---:|---:|
| Rebellion / X-Wing | F | 5 | 230 | 46.0000 | 95 | 15 | 550 | 0.3 |
| Republic / ARC-170 | F | 8 | 220 | 27.5000 | 60 | 8 | 544 | 0.2 |
| Empire / TIE Fighter | F | 10 | 210 | 21.0000 | 58 | 0 | 580 | 0 |
| CIS / Vulture | F | 13 | 200 | 15.3846 | 44 | 0 | 572 | 0 |
| Rebellion / A-Wing | I | 4 | 200 | 50.0000 | 100 | 15 | 460 | 0.3 |
| Republic / V-Wing | I | 6 | 190 | 31.6667 | 64 | 8 | 432 | 0.2 |
| Empire / TIE Interceptor | I | 8 | 180 | 22.5000 | 62 | 0 | 496 | 0 |
| CIS / Belbullab-22 | I | 11 | 170 | 15.4545 | 44 | 0 | 484 | 0 |
| Rebellion / BTL-A4 | B | 3 | 140 | 46.6667 | 180 | 10 | 570 | 0.2 |
| Republic / BTL-B | B | 4 | 135 | 33.7500 | 130 | 8 | 552 | 0.15 |
| Empire / TIE Bomber | B | 6 | 130 | 21.6667 | 100 | 0 | 600 | 0 |
| CIS / Droid Bomber | B | 10 | 125 | 12.5000 | 58 | 0 | 580 | 0 |
| Republic / V-19 | F | 8 | 210 | 26.2500 | 58 | 8 | 528 | 0.2 |
| Republic / NTB-630 | B | 4 | 130 | 32.5000 | 125 | 8 | 532 | 0.15 |
| Empire / TIE Brute | F | 10 | 220 | 22.0000 | 64 | 0 | 640 | 0 |
| Empire / TIE Avenger | F | 10 | 225 | 22.5000 | 54 | 8 | 620 | 0.1 |
| Empire / TIE Punisher | B | 6 | 145 | 24.1667 | 110 | 6 | 696 | 0.1 |


### Recovery and unchanged values

- **HullRepairPerSecond = 0 for all 17 regular types**, including V-Wing and BTL-B. This explicitly removes their current passive hull repair; update descriptions accordingly.
- Shield regeneration uses the HP/s/craft column. Preserve each asset's current tick interval; serialize ShieldRegenerateValue = target HP/s × ShieldRegenerateDelay. A-Wing's 2 s interval → 0.6 HP/tick.
- LaserShieldDamageMultiplier = 1 for all 17 types. Keep the damage matrix unchanged; common fighter/interceptor lasers deal 0.5× raw damage to squadron shields.
- CIS shields and regeneration become zero for all three types; Imperial standard types and Brute remain unshielded. Avenger/Punisher retain small shields and 0.1 HP/s/craft recovery.
- These shield/recovery reductions are intentional balance changes: fewer high-quality craft should not combine large resistant shield pools with rapid in-combat recovery.
- Preserve current movement, targeting, weapon cycle/salvo timings, ranges, research and ability behavior for the first candidate. No new repair ability is introduced.
- Ability-modified/researched quality ordering is not guaranteed by this base-stat target; evaluate those separately. Premium units retain their existing abilities.

### Exact weapon budget per squadron

| Unit | Laser DPS | Ion DPS | Missile DPS | Torpedo DPS | Total DPS | Torpedo volley / cycle |
|---|---:|---:|---:|---:|---:|---:|
| X-Wing | 210 | 0 | 0 | 20 | 230 | 200 / 10 s |
| ARC-170 | 200 | 0 | 0 | 20 | 220 | 240 / 12 s |
| V-19 | 210 | 0 | 0 | 0 | 210 | — |
| TIE Fighter | 210 | 0 | 0 | 0 | 210 | — |
| TIE Brute | 220 | 0 | 0 | 0 | 220 | — |
| TIE Avenger | 205 | 0 | 20 | 0 | 225 | — |
| Vulture | 200 | 0 | 0 | 0 | 200 | — |
| A-Wing | 200 | 0 | 0 | 0 | 200 | — |
| V-Wing | 190 | 0 | 0 | 0 | 190 | — |
| TIE Interceptor | 180 | 0 | 0 | 0 | 180 | — |
| Belbullab-22 | 170 | 0 | 0 | 0 | 170 | — |
| Rebel BTL-A4 | 70 | 10 | 0 | 60 | 140 | 720 / 12 s |
| Republic BTL-B | 79 | 0 | 0 | 56 | 135 | 672 / 12 s |
| NTB-630 | 78 | 0 | 0 | 52 | 130 | 624 / 12 s |
| TIE Bomber | 78 | 0 | 0 | 52 | 130 | 624 / 12 s |
| TIE Punisher | 77 | 0 | 12 | 56 | 145 | 672 / 12 s |
| Droid Bomber | 77 | 0 | 0 | 48 | 125 | 576 / 12 s |

- Bomber torpedo budgets deliberately increase anti-ship contribution: current Rebel BTL-A4 volley360 → target720 despite six→three craft. This is an explicit role rebalance, not a side effect of member scaling.
- Per-craft bomber torpedo volley: Rebel240 > Republic168 (NTB156) > Empire104 (Punisher112) > CIS57.6.
- Preserve all existing mounts per craft and salvo shot counts. For each weapon family on a type, multiply its current resized damage by target family DPS / current resized family DPS. Preserve relative damage among that family's mounts, including ARC rear/forward guns.
- Use unit-scoped weapon definitions; do not retune shared family rules. This algorithm plus family totals specifies every mount's damage without guessing barrel/salvo counts.
- Missile total-DPS targets use existing cycle times. Set base WarheadDamageMultiplier and WarheadReloadMultiplier to 1 and encode the budget in unit definitions, avoiding duplicate tuning.

### Carrier targets

- Active cap = simultaneous squads. Total stock = all launches including the first wing, not additional replacements.
- Stock factor is explicit per row: support2×, dedicated3×, flagship4× active. This replaces the old faction stock multiplier; faction quantity now primarily comes from members per squadron.
- Wing DPS excludes carrier guns and assumes every listed squad is intact and deployed.
- Standard types: Rebel X-Wing/A-Wing/BTL-A4; Republic ARC-170/V-Wing/BTL-B; Empire TIE Fighter/TIE Interceptor/TIE Bomber; CIS Vulture/Belbullab-22/Droid Bomber.

| Faction / carrier | Active F / I / B | Total stock F / I / B | Wing raw DPS |
|---|---:|---:|---:|
| Empire / Acclamator Assault | 2/1/1 | 4/2/2 | 730 |
| Empire / Executor | 2/2/2 | 8/8/8 | 1040 |
| Empire / Gladiator | 2/1/2 | 6/3/6 | 900 |
| Empire / Imperial Venator | 2/2/2 | 6/6/6 | 1040 |
| Empire / Interdictor | 1/0/0 | 2/0/0 | 210 |
| Empire / ISD I | 2/1/2 | 6/3/6 | 860 |
| Empire / ISD II | 2/1/1 | 4/2/2 | 730 |
| Rebellion / Bothan Assault Cruiser | 1/1/1 | 2/2/2 | 570 |
| Rebellion / Corellian Battlecruiser | 1/1/1 | 2/2/2 | 570 |
| Rebellion / Dauntless | 1/1/1 | 2/2/2 | 570 |
| Rebellion / MC75 Profundity | 1/1/1 | 2/2/2 | 570 |
| Rebellion / Nebula | 1/1/1 | 2/2/2 | 570 |
| Rebellion / Home One | 2/2/1 | 8/8/4 | 1000 |
| Rebellion / MC80 Independence | 2/1/2 | 8/4/8 | 940 |
| Rebellion / Endurance | 2/2/2 | 8/8/8 | 1140 |
| Rebellion / Liberator | 2/1/2 | 6/3/6 | 940 |
| Rebellion / MC80 Justice | 2/1/2 | 6/3/6 | 940 |
| Rebellion / Quasar Fire | 2/1/1 | 6/3/3 | 800 |
| Rebellion / MC80b Remonda | 1/0/1 | 2/0/2 | 370 |
| Rebellion / MC80 Freedom | 0/0/1 | 0/0/2 | 140 |
| Rebellion / Mediator | 0/0/1 | 0/0/2 | 140 |
| Republic / Acclamator | 2/1/1 | 4/2/2 | 755 |
| Republic / Imperator | 1/1/1 | 2/2/2 | 545 |
| Republic / Resolute | 2/2/1 | 6/6/3 | 955 |
| Republic / Mandator | 2/2/2 | 8/8/8 | 1090 |
| Republic / Rothana | 2/1/2 | 6/3/6 | 900 |
| Republic / Venator II | 2/1/2 | 6/3/6 | 900 |
| Republic / Venator I | 2/2/2 | 6/6/6 | 1090 |
| CIS / Wavecrest | 1/1/1 | 2/2/2 | 495 |
| CIS / Bulwark | 2/1/1 | 4/2/2 | 695 |
| CIS / Providence | 2/1/2 | 6/3/6 | 820 |
| CIS / Captor | 2/2/2 | 6/6/6 | 990 |
| CIS / Invincible | 2/2/2 | 8/8/8 | 990 |
| CIS / Lucrehulk | 2/2/2 | 8/8/8 | 990 |


### Mixed bays and timing

- Gladiator exact active/stock: TIE Fighter0/0; Brute1/3; Avenger1/3; TIE Interceptor1/3; TIE Bomber1/3; Punisher1/3. Preserve all existing indices/bindings; Brute/Avenger fill two fighter slots and Bomber/Punisher fill two bomber slots. Standard TIE direct purchase remains available.
- Zero/zero bay is supported: HangarBay permits0; launch selection requires stock>0 and active<cap; preload skips disabled bays. No new runtime capacity mechanism required.
- Republic Acclamator: ARC-1701/2, V-19 1/2, V-Wing1/2, BTL-B1/2; fighter total2/4.
- Interdictor supports fighters only; Remonda fighters/bombers; Freedom and Mediator bombers only. Their zero-role entries are intentional exceptions to the three-role carrier convention.
- Every carrier uses initial delay4 s and interval1 s, retaining round-robin selection, global10 launches/s and per-bay5 s recent-launch window. Preserve existing ability-driven timing rules and destroyed-hardpoint eligibility.
- Same six-squadron ceiling means Captor and Lucrehulk share990 active wing DPS; Lucrehulk has8/8/8 stock versus Captor6/6/6 plus its own hull/guns. Do not claim their air wings alone justify the hull-price difference.
- Lucrehulk raw active wing DPS2161→990 (−54.2%) is intentional consolidation plus quality normalization; its craft104→68 and entities20→6. Revisit carrier value if this excessive nerf appears in equal-cost matches.

### Scope boundaries

- This requested target sheet fixes squadron combat stats and all34 carrier loadouts. Direct-purchase prices/build times/population, station deployment and whole-faction economic balance are not declared balanced by these numbers.
- Existing station caps2 Rebel/3 Republic/4 Empire/5 CIS would yield400/660/840/1000 base DPS with new squad stats. Resolve station targets before shipping the overall rebalance; carrier rules do not silently change station roles.
- Existing low CIS purchase prices and high Rebel prices cannot be assumed appropriate when whole squads are now closer in strength. Keep a separate economics acceptance item rather than silently changing approved prices in this proposal.

## Implementation

1. Resize17 regular prefabs to the exact counts; preserve three heroes and excluded Delta-7. Clone/remove complete craft subtrees and rebuild contiguous IDs plus health/flight/weapon/death/pose/entity registrations.
2. Preserve each type's full multi-gun/torpedo/ion layout. The generic SquadronViewPrefabBuilder only covers three types and registers one Gun/member; do not blanket-rebuild modern layouts with it.
3. Apply per-craft hull/shields/recovery and family DPS budgets through unit-scoped definitions. Rebake MemberCount and weaponLoadout; both remain derived data. Verify counts, exact damage totals, volley budgets and unchanged weapon cycles.
4. Apply all34 carrier active/stock targets and explicit mixed bays. Preserve bay indices and hardpoint/exit bindings. Update faction descriptions, count/health/repair text and carrier stock descriptions.
5. Reconcile AI live/pending wing strength and usable reserves; inspect capture/retreat/population valuation. Theoretical carrier capacity is not surviving strength.
6. Source-check fresh save/load, member-death weapon shutdown, complete pooled respawn and matching host/client member arrays. Old member-array lengths are incompatible; no silent healing or truncation migration.
7. Review13/11-member formations, navigation radius, launch overlap and targeting footprint. No fixed eight-member limit was found; increased craft still adds per-member weapon/visual cost.
8. Refresh/import assets, explicitly reserialize only changed paths, SaveAssets and inspect Console. Do not require manual Editor saving.
9. Validate equal-cost/population fleets at levels2/3/5, strikecraft role matchups, anti-ship volleys, replacement timelines, station/purchase changes and abilities/research. Record living squads AND craft, losses, damage by target, expenditure and frame time.

## Edge Cases

- Effective durability against a chosen common laser = members × (hull / hull-class multiplier + shields / 0.5), excluding recovery, accuracy, targeting bias and casualty distribution.
- Fighter laser hull multipliers F/I/B =1.5/1.75/1.5; interceptor laser =1/1.5/2.5. This avoids overvaluing raw unshielded HP or undervaluing shield resistance.
- Exact target math confirms per-craft DPS and HP ordering for standard and listed premium types. It does not prove equal-cost match balance.
- Shielded recovery, Rebel speed, overkill against fragile craft, attack-run uptime and premium abilities can widen the Rebel advantage; do not compensate by changing counts without updating the target sheet.
- Local test execution remains disabled unless explicitly requested. Current verification is arithmetic, prior live source/prefab inspection and current carrier role/price inspection, not a battle or test pass.

## TODO

- [x] Audit live member/loadout data; verify21/21 prefab inventories.
- [x] Calculate count-only impact and identify save, builder, economy and role-cap issues.
- [x] Set exact17-type count/DPS/hull/shield/recovery/weapon budgets and34-carrier caps/stock.
- [x] Verify strict per-craft quality ordering and both common-laser durability calculations; verify zero/zero disabled bay support.
- [ ] Implement prefab, stats, weapon budgets and carrier values.
- [ ] Reconcile station and direct-purchase economics before release; preserve global launch throttle.
- [ ] Update AI strength consumers, descriptions and fresh-match compatibility messaging.
- [ ] Complete asset persistence/source checks and user gameplay acceptance.
- Relevant categories when authorized: Squadrons, Balance, Combat, Ships, GameFlow, EnemyAi; Networking/Capture/Economy/Ui only where implementation touches them.
- No reusable implemented rule extracted: these are proposed targets; prior verified engineering findings remain linked in the research and GameDesign/Combat.

## Files

- Assets/Prefabs/Models/Squadrons/; Assets/Settings/Data/Squadron/
- Assets/Settings/Data/Models/Weapon/WeaponsData.asset; DamageMatrixData.asset
- Assets/Settings/Data/Ship/{Empire,Rebellion,Republic,Separatist}/; Assets/Settings/Data/Factions/
- Assets/Scripts/Components/Hangar/HangarBay.cs; HangarModel.cs
- Assets/Scripts/Components/Squadron/Health/SquadronHealthComponent.cs; SquadronHealthModel.cs
- Assets/Scripts/Editor/AI/WeaponLoadoutBaker.cs; Assets/Scripts/Editor/Balance/BalanceDerivedData.cs
- Assets/Scripts/Services/Saves/BattleSaveValidator.cs; Assets/Scripts/Services/Repository/BattleAssetPreload.cs
- [[TODOs/Features/Malevolence_Import]]; [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]
