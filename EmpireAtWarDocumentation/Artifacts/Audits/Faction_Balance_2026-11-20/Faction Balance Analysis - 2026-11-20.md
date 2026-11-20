---
type: audit-record
date: 2026-11-20
status: review
record_state: current
system:
  - balance
  - factions
  - ships
  - squadrons
tags:
  - gamedesign
  - units
  - balance
  - analysis
---
# Faction Balance Analysis — 2026-11-20

## Goal
- Check whether the four factions differ by **advantage**, not by matching stats.
- Target (user, 2026-11-20): Empire and Rebellion stronger per unit; Republic and CIS cheaper, faster to build, with a few strong units.
- Read-only audit. No asset, code or test changed. Recommendations only.
- Related: [[Done/Bugs/Squadron_Faction_Balance]], [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]], [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]], [[GameDesign/Economy]].

## Verdict
- **The target identity is not in the data.** Republic is the most cost-efficient faction and has the second-strongest single ship. CIS is the least cost-efficient faction and is not cheaper.
- Build time follows ship class, not faction. No faction builds faster.
- Research, defense platforms and superweapons are **identical** in all four factions.
- Real differences today: unit count caps, speed, shield regeneration, hangars, squadron price tiers.
- Squadrons already follow the target (strength follows price). Ships do not.

## Method
- Source: `Factions/*/*Faction.asset` (price, build time, population, level, max count), `Ship/**/*ShipData.asset`, `Squadron/*SquadronData.asset`, `WeaponsData.asset`, `DamageMatrixData.asset`.
- EHP = hull + shields. `EHP/cr` = EHP ÷ price.
- DPS = `WeaponProfileFactory` formula × mounts in `weaponLoadout`. `vsCap` / `vsFtr` apply damage matrix, accuracy and caliber bias against class Capital / Fighter (`WeaponEngagementRange.EffectiveDamagePerSecond`).
- `capDPS/kcr` = `vsCap` per 1,000 credits.
- Heroes excluded from faction totals.
- **Not modelled:** firing arcs, range, shield damage multipliers, abilities, research, shield regeneration, hangar squadrons. Carriers (Lucrehulk, Captor, DH-Omni, Venator, Quasar) look weaker here than in battle.
- No battle or simulator run. Numbers are paper values.

## Faction Scorecard (non-hero ships)

| Metric | Republic | CIS | Rebellion | Empire |
|---|---:|---:|---:|---:|
| Ships | 18 | 19 | 19 | 17 |
| Median price | 5,000 | 4,000 | 5,000 | 5,000 |
| EHP per credit | **4.92** | **3.16** | 3.80 | 3.73 |
| capDPS per 1,000 cr | **96** | **59** | 75 | 69 |
| Credits per population | 726 | 916 | 882 | 906 |
| Credits produced per build second | 146 | 184 | 154 | 188 |
| Median speed | 30 | 40 | 40 | 25 |
| Carriers | 9 | 9 | 12 | 8 |
| Ship types capped at ≤ 3 | 7 | 1 | 15 | 10 |

- Population cap: `120` for every faction (`ReinforcementData.MaxUnitCapacity`).
- Higher "credits per build second" = value reaches the field faster. Republic is the **slowest**, not the fastest.

### By class: EHP per credit / capDPS per 1,000 cr

| Class | Republic | CIS | Rebellion | Empire |
|---|---|---|---|---|
| Corvette + frigate | 1.46 / 20 | 1.77 / 37 | 2.10 / 36 | 1.52 / 19 |
| Cruiser + heavy cruiser | 2.77 / 65 | 2.46 / 81 | **3.93 / 97** | 3.52 / 60 |
| Capital | 3.75 / 114 | **2.62** / 99 | 3.19 / 82 | 3.68 / **127** |
| Battlecruiser + dreadnought | **6.24 / 101** | 3.72 / 44 | 4.86 / 51 | 3.89 / 52 |

- Rebellion wins the mid game on paper. Empire wins capitals on damage. Republic wins the late game by a wide margin. CIS wins nothing except cruiser damage.

## What Already Differs (keep)
- **Count caps:** Rebellion 15 of 19 ship types capped at 3; Empire all capitals capped at 3; Republic and CIS 10–40 per type. This is the only "mass vs elite" lever that works now.
- **Speed:** Rebel capitals 40, CIS 35, Republic 20–30, Empire 20.
- **Shield regeneration:** Rebellion ×1.1, Empire ×0.95 of the shared tiers (5 / 25 / 90 / 117 / 216 / 400 / 1111).
- **Squadron price tiers:** CIS 200–400, Empire 300–600, Republic 500–650, Rebellion 800–850. Craft per squadron 13/11/10 → 10/8/6 → 8/6/4 → 5/4/3.
- **Abilities:** Empire has Tractor Beam on 10 ships and Power to Main Batteries on 4. Rebellion has the most unique abilities. CIS is 13 × Boost Weapon Power with one unique ability (Ion Pulse).

## Findings — Ships

### 1. Republic late game is too efficient
| Ship | Price | Pop | EHP | vsCap DPS | EHP/cr |
|---|---:|---:|---:|---:|---:|
| Republic Secutor | 14,000 | 15 | 73,100 | 2,249 | 5.22 |
| Republic Praetor | 20,000 | 25 | 165,000 | 3,441 | 8.25 |
| Republic Mandator (Pride of the Core) | 30,000 | 50 | 196,250 | 1,065 | 6.54 |
| Empire Allegiance | 15,000 | 20 | 39,500 | 858 | 2.63 |
| Empire Titan | 25,000 | 35 | 87,900 | 1,203 | 3.52 |
| Empire Executor | 70,000 | 50 | 300,000 | 3,685 | 4.29 |
| Rebel Independence | 20,000 | 25 | 85,000 | 1,029 | 4.25 |
| CIS Invincible | 20,000 | 20 | 85,000 | 1,502 | 4.25 |

- Praetor at 20,000 has 93% of Executor's damage and 55% of its EHP for 29% of the price and half the population.
- Secutor beats Allegiance on every stat for 1,000 less.
- Result: Republic is the strongest per unit **and** per credit at level 5. Opposite of the target.

### 2. CIS is not cheap
- L1 corvettes: C-9979 `1,200`, Tartan `1,200`, CR70 `1,250`, CR90 `1,250`. No price gap.
- CIS capitals pay more for less: Bulwark I `8,000` → 20,000 EHP, 850 DPS. Same price buys ISD II (34,000 EHP, 1,337 DPS), Rothana (40,000 EHP) or MC80 (27,000 EHP, 931 DPS).
- Bulwark Mk II `10,000` → 23,000 EHP, 737 DPS. Worse than Bulwark I per credit on both stats.
- Malevolence `35,000`, pop 30 → 110,000 EHP, 742 DPS (Ion Pulse not modelled).
- DH-Omni `8,000`, **pop 14** → 33 DPS vs capitals. Six squadrons at once is its whole value.
- CIS costs the most per population (916 cr) and gets the least EHP per population (2,897).
- CIS strong units exist and fit the target: Recusant `4,000` → 890 DPS (222 per 1,000 cr, best in game, max 30); Munificent max 40; Dispatcher 127 per 1,000 cr.

### 3. Build time has no faction identity
| Class | Republic | CIS | Rebellion | Empire |
|---|---|---|---|---|
| Corvette | 18–20 s | 15–20 s | 20–25 s | 20–25 s |
| Cruiser / heavy | 25–40 s | 30–55 s | 20–45 s | 30–45 s |
| Capital | 50–65 s | 45–60 s | 40–65 s | 45–70 s |
| Battlecruiser + | 80–110 s | 75–80 s | 65–80 s | 85–140 s |

- Spread inside a faction is larger than the spread between factions.
- Station upgrades cost the same time and credits for everyone (`StationLevelData`: 2,000/80 s → 5,500/220 s).

### 4. Duplicate ships across factions
- **Tector:** same asset values in Republic and Empire (7,000, L4, 28,250 EHP, 1,239 DPS, max 3).
- **Arquitens:** Republic and Empire identical (3,500, L3, 10,400 EHP, 196 DPS). Only the regen tier table differs.
- **Acclamator:** same price and hull. Republic 119 DPS and 4 squadron types; Empire 46 DPS and 3 types.
- **Imperator (Republic) vs ISD I (Empire):** both 14,000 / 11,000. Republic pays 8,000, Empire 7,500.
- **Venator II (Republic, L5) vs Imperial Venator (Empire, L3):** both 6,500 and 10,000 / 11,000. Empire gets it two station levels earlier with 18% more damage.
- **Victory (Republic, L4, 4,200) vs Victory I (Empire, L2, 4,000):** Empire gets more hull, cheaper, two levels earlier.

### 5. Level gating is uneven
| Level | Republic | CIS | Rebellion | Empire |
|---|---:|---:|---:|---:|
| 1 | 1 | 2 | 1 | 2 |
| 2 | 5 | 5 | 3 | 3 |
| 3 | 2 | 3 | 7 | 4 |
| 4 | 3 | 5 | 5 | 5 |
| 5 | 7 | 4 | 3 | 3 |

- Every faction opens with the same thing: one or two ~1,200-credit corvettes.
- Republic is top-heavy: 7 of 18 ships at level 5, only 2 at level 3.
- Rebellion peaks at level 3 with 7 ships, including a capital (Dauntless) and two very tanky ships.

### 6. Outliers to review
- Rebel **Freedom** `5,500`, L3: 38,000 EHP (6.91 per credit). More EHP than MC80 at 8,000. Class tag is heavy cruiser.
- Rebel **Justice** `5,000`, L4: 29,200 EHP (5.84 per credit) plus 5 squadrons at once.
- Rebel **Liberator** `4,200`, L3: 20,670 EHP (4.92 per credit) plus 5 squadrons at once.
- Rebel **Nebulon-B** `2,200`, L2: 7,800 EHP, tagged cruiser. Republic Pelta at 1,800 has 4,160.
- Empire **Devastator** (hero, 12,000): 564 DPS vs capitals. Chimaera at the same price and hull has 1,382; ISD II at 8,000 has 1,337.
- Empire **Raider** `1,800`: same hull and shields as Tartan at 1,200, 0.67 EHP per credit (lowest in game).
- Republic **IPV-2C** `2,150`, max 1: 1,750 EHP, 18 DPS. Weaker than CR70 at 1,250.
- Empire **Gladiator**: first bay is TIE Fighter `0/0` (reserve 0, active 0).

## Findings — Squadrons

| Faction | Unit | Class | Level | Price | Build s | Craft | Squadron EHP | vsFtr DPS | vsCap DPS |
|---|---|---|---:|---:|---:|---:|---:|---:|---:|
| CIS | Vulture | Fighter | 1 | 200 | 8 | 13 | 299 | 243 | 6 |
| CIS | Belbullab-22 | Interceptor | 1 | 300 | 5 | 11 | 330 | 115 | 3 |
| CIS | Droid Bomber | Bomber | 1 | 300 | 8 | 10 | 580 | 93 | 72 |
| CIS | Hyena | Bomber | 3 | 400 | 10 | 10 | 700 | 93 | 95 |
| Empire | TIE Fighter | Fighter | 1 | 300 | 10 | 10 | 510 | 280 | 7 |
| Empire | TIE Interceptor | Interceptor | 1 | 450 | 8 | 8 | 424 | 140 | 4 |
| Empire | TIE Brute | Fighter | 1 | 450 | 7 | 10 | 730 | 333 | 8 |
| Empire | TIE Bomber | Bomber | 2 | 450 | 17 | 6 | 600 | 94 | 106 |
| Empire | TIE Avenger | Fighter | 3 | 550 | 10 | 10 | 760 | 350 | 18 |
| Empire | TIE Punisher | Bomber | 1 | 600 | 12 | 6 | 696 | 99 | 147 |
| Empire | TIE Defender | Fighter | 4 | 1,000 | 22 | 3 | 465 | 192 | 54 |
| Republic | V-19 | Fighter | 1 | 500 | 6 | 8 | 960 | 267 | 7 |
| Republic | NTB-630 | Bomber | 1 | 550 | 8 | 4 | 532 | 94 | 130 |
| Republic | ARC-170 | Fighter | **5** | 600 | 8 | 8 | 1,112 | 267 | 47 |
| Republic | Y-Wing | Bomber | 2 | 650 | 8 | 4 | 552 | 95 | 153 |
| Republic | V-Wing | Interceptor | **5** | 650 | 15 | 6 | 624 | 142 | 4 |
| Rebellion | X-Wing | Fighter | 1 | 800 | 15 | 5 | 1,090 | 270 | 47 |
| Rebellion | A-Wing | Interceptor | 1 | 850 | 6 | 4 | 652 | 190 | 5 |
| Rebellion | Y-Wing | Bomber | 1 | 850 | 15 | 3 | 570 | 85 | 200 |

- Every squadron costs **1 population** (TIE Defender 2). Under the 120 cap, Rebel squadrons are 2–4× better per slot. In credits, swarm squadrons deal more raw damage per credit (Vulture 1,213 vsFtr DPS per 1,000 cr; X-Wing 338). Duel results differ because of salvo overkill → [[Done/Bugs/Squadron_Faction_Balance]].
- **All bombers deal 235–245 vsCap DPS per 1,000 credits.** By design (torpedo follows price), but it means no faction has a bomber advantage per credit.
- **Build time is random:** A-Wing 850 cr in 6 s; TIE Bomber 450 cr in 17 s; Vulture 200 cr in 8 s. The swarm faction builds the least value per second (Vulture 25 cr/s vs A-Wing 142 cr/s).
- **Republic unlocks are odd:** ARC-170 and V-Wing need station level 5, yet cost 600–650. Republic has no interceptor before level 5.
- **TIE Defender:** 1,000 cr, 2 population, 3 craft, 465 EHP, 192 vsFtr DPS. Worst value of any squadron on EHP and anti-fighter damage.
- Roster sizes: Empire 7, Republic 5, CIS 4, Rebellion 3 (non-hero).

## Findings — Shared Systems
- **Research:** 7 lines, same tiers, prices, times and multipliers in all four faction assets (Weapons ×1.25, Shields ×1.25, Engines ×1.15, Defenses ×1.15/1.3/1.5, Structures ×0.85). The October note ([[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]) records Republic Shields vs CIS Engines; that split is gone.
- **Defense platforms** `0, 2, 3` and **superweapons** `0, 1, 2`: same list for all four.
- **Mining:** Republic and CIS build Mining Facility; Empire and Rebellion build Tibanna Station. Same price `1,000` and build time `5 s`.
- **Station:** 72,000 hull / 48,000 shields for all. Only the hangar differs: Rebel X-Wing 2/6, Republic ARC-170 3/9, Empire TIE 4/12, CIS Vulture 5/20.
- **Heroes:** Republic 1 ship + 4 squadrons; Empire 2 ships + 2 squadrons; Rebellion 1 ship + 2 squadrons; CIS 0 ships + 3 squadrons.

## Recommended Direction

### Target identity
| Faction | Per-unit strength | Price | Build time | Counts | Edge |
|---|---|---|---|---|---|
| Empire | Highest | Highest | Slowest | Low caps | Raw firepower, tractor beams |
| Rebellion | High | High | Normal | Caps of 3 | Speed, shields, elite squadrons |
| Republic | Medium | Medium | Fast | Medium | Carriers, balanced fleet, 2–3 strong ships |
| CIS | Lowest | Lowest | Fastest | High | Swarm, cheap hangars, 2–3 glass cannons |

### Levers, in order of value
1. **Faction build-time multiplier.** Start: CIS ×0.7, Republic ×0.85, Rebellion ×1.0, Empire ×1.15 on ships and squadrons. Why: cheapest way to create the "faster build" identity; one number per faction.
2. **Cut CIS prices.** Starting points for parity with the 3.7 EHP-per-credit average: Bulwark I `8,000 → ~5,500`, Bulwark Mk II `10,000 → ~6,500`. Recusant Dreadnought (3.61), Invincible (4.25) and Lucrehulk (4.50) already sit at parity. Then decide how far below parity "cheap" should go, e.g. C-9979 `1,200 → ~900`. Review DH-Omni population `14`.
3. **Fix the Republic top end.** Either raise Praetor (`20,000 → ~35,000`) and Secutor (`14,000 → ~18,000`), or lower their stats. Avoid: buffing Empire to match, which inflates every late-game fight.
4. **Raise Empire heavy ships.** Allegiance (39,500 EHP for 15,000) and Titan (87,900 for 25,000) are below Rebel and CIS peers. They should be the strongest in class.
5. **Differentiate shared hulls.** Tector, Arquitens, Imperator/ISD I, Venator II/Imperial Venator. Rule to pick: Empire version = more hull and damage, +20–30% price; Republic version = cheaper, faster build, better hangar.
6. **Make research faction-specific.** Example: Empire Weapons to tier III, Shields to tier I only; Rebellion Shields and Engines III; Republic Production and hangar; CIS build speed and Production. Why: zero new art, large identity gain.
7. **Squadron build time follows faction.** CIS 3–5 s, Empire 6–8 s, Republic 8–10 s, Rebellion 12–15 s. Fix A-Wing (6 s) and TIE Bomber (17 s).
8. **Move ARC-170 and V-Wing** from level 5 to level 2–3. Review TIE Defender stats.

## Open Questions
- Should Republic keep a top-end ship that rivals Executor ("some strong units"), or should Praetor come down?
- Is "cheaper" meant per credit, per population, or both? Population cost per credit is nearly equal today (726–916 cr per slot).
- Should Rebellion keep the cap of 3 on almost every ship, or only on capitals?
- Should carriers be valued by squadron output? A hangar model is needed before repricing Lucrehulk, Captor, DH-Omni and Venator.

## Limits
- Paper values only. Verify any change with the Battle Simulator at equal credits and at equal population.
- Hangar value, arcs, abilities and research are outside the model. Carrier factions (CIS, Rebellion) are understated.
- Class tags affect damage taken and accuracy; several Rebel ships carry a lighter tag than their EHP suggests (Freedom, Nebulon-B, MC50).

## Files
- `Assets/Settings/Data/Factions/{Republic,Separatist,Rebellion,Empire}/*Faction.asset` — price, build time, population, level, max count, research.
- `Assets/Settings/Data/Ship/**/*ShipData.asset`; `Assets/Settings/Data/Squadron/*SquadronData.asset` — stats, loadouts, hangars.
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`, `DamageMatrixData.asset` — damage model.
- `Assets/Settings/Data/Factions/Shared/StationLevelData.asset`, `MiningFacilityCatalog.asset`; `Assets/Settings/Data/Reinforcement/ReinforcementData.asset` (`MaxUnitCapacity`).
- `Assets/Scripts/Components/AttackComponent/WeaponProfileFactory.cs`; `Assets/Scripts/Components/Weapon/WeaponEngagementRange.cs` — formulas used.
