---
category: Bugs
status: done
design_state: applied
created: 2026-11-21
updated: 2026-11-21
system:
  - balance
  - squadrons
  - combat
completed: 2026-11-21
acceptance: pending-user
---
# Fighter and Interceptor Rebalance

## Goal

- Fighter guns get no damage-matrix advantage over interceptors.
- Interceptors kill bombers **and fighters** through bolt damage; they cost clearly more.
- Fighters: slightly cheaper, slower, more hull + shields. They kill bombers too, slower.
- Empire squadrons: fastest, strong weapons, weak durability.
- Follow-up to [[Done/Bugs/Squadron_Faction_Balance]]. Craft counts, bombers' weapons and carriers stay.

## User rules

### 2026-11-21 (first request)

- Fighters have no matrix damage advantage over interceptors.
- Interceptors kill bombers faster through laser damage; fighters kill bombers slower.
- Interceptor damage > current fighter damage.
- Fighters slower than interceptors; fighters have more hull/shields.

### 2026-11-21 (review of candidate v1)

- Tune prices: interceptors expensive, fighters a little cheaper.
- Interceptors must also kill fighters.
- Matrix damage may be tuned if needed.
- Belbullab must be weaker than TIE Interceptor.
- Empire: faster craft, stronger weapons, weak durability (no shields, small hull). Expensive special units with shields are exempt.

## Decisions (user, 2026-11-21)

- "Interceptors kill fighters" → one interceptor squadron beats one base fighter squadron of its faction.
- Empire speed rule covers squadrons only.
- Empire exempt units: TIE Avenger, TIE Punisher, TIE Defender, Darth Vader.
- Interceptor damage to large ships (Corvette…Structure) stays at today's level.
- Accepted as they are: A-Wing vs TIE Interceptor `0.90`; Belbullab no faster than Vulture against Droid Bombers.
- TIE Brute and Avenger are not required to lose to the TIE Interceptor (Brute = Empire's anti-interceptor fighter).
- Both laser rows (`FighterLaser` `13`, `InterceptorLaser` `14`) are identical against Fighter / Bomber / Interceptor.
- Accuracy against interceptors stays `0.75` (not `0.6`) → fighters stay worth buying.
- No open assumptions. Next: step 2.
- 2026-11-21 (implementation start): user approved Rebellion boost ×`1.25` (X-Wing, A-Wing, Luke wingmen) and the Gladiator bay swap.

## Outcome (2026-11-21)

- Applied: candidate v2 matrix rows `13`/`14`, hulls, Empire speeds, prices; Rebellion ×`1.25`; Avenger hull `78`; Defender `420/200`, weapon `158` `9.6` / reload `0.67`; Gladiator Avenger `0/0`, TIE Fighter `1/3`; Darth Vader speed `80/90` (= TIE Fighter, below Mace Windu `80.6/91`).
- Step 3 tuning changed three values from candidate v2:

| Value | Candidate v2 | Final | Why |
|---|---|---|---|
| A-Wing `141` `damageMultiplier` | `11.467` | `10.9` (bolt `81.75`) | thin breakpoints `5 → 1` |
| V-Wing `140` `damageMultiplier` | `6.933` | `7.3` (bolt `54.75`) | thin breakpoints `3 → 0` |
| Belbullab-22 price | `350` | `300` | gap fix from "Known gaps" (hull fix would break fighter > interceptor hull) |

- Kept: TIE Interceptor `4.8`. Tried `5.1` (robust one-hit on TIE Fighter) → kills V-Wing in 2 hits; V-Wing vs TIE Interceptor `1.44 → 0.68`. Robust TIE Fighter one-hit and 3-hit V-Wing are incompatible.
- Kept: Belbullab `0.7333`. Robust one-hit on Vulture (bolt ≥ `16.9`) forces a 2-hit TIE Interceptor kill.
- Thin breakpoints `69 → 62`. Vs large ships (row ÷`2.1`): A-Wing `0.91×`, V-Wing `1.22×`, TIE Interceptor `1.09×`, Belbullab `0.84×` of before.
- DuelSim final (600 trials, in a row): A-Wing → X-Wing `0.91` / X-Wing → A-Wing `0.45`; V-Wing → ARC-170 `1.30` / `0.30`; V-Wing → V-19 `1.32` / `0.31`; TIE Interceptor → TIE Fighter `1.67` / `0.21`; Belbullab → Vulture `0.97` / `0.71`; V-Wing → TIE Interceptor `1.44`; A-Wing → V-Wing `1.07`; TIE Interceptor → Belbullab `1.30`.
- New finding (equal-credit group fights, both sides at once): fighters win `100%` — 2 TIE Fighter vs 1 TIE Interceptor, 2 Vulture vs 1 Belbullab, 5 V-19 vs 3 V-Wing, 7 X-Wing vs 5 A-Wing. Corrects the "Known gaps" claim that fighters lose at equal credits. Not tuned; check in battle.
- Elite: 1 X-Wing vs 1 Avenger X-Wing wins `53%`; 3 X-Wing vs 4 Avenger `0%`; X-Wing / A-Wing vs Defender `0%` / `2%`; 2 TIE Interceptor beat 1 Defender `99%`.
- V-Wing squadron DPS (~`487`) now slightly above A-Wing (~`484`); A-Wing still wins the duel.
- Descriptions updated: X-Wing, A-Wing, TIE Interceptor, Belbullab, Avenger, Defender hull/shields; Gladiator fighter complement. Matchup tooltips reviewed: no stale claims (A-Wing / Belbullab list Fighters only — pre-existing).
- Unity: refresh → `ForceReserializeAssets` on 18 paths → `SaveAssets`; compile clean, 0 console errors; reserialize added no diff.
- Tests: replaced `SquadronRoles_InterceptorsCounterBombers_FightersCounterInterceptors` with `SquadronRoles_FighterAndInterceptorLasersShareStrikecraftRows` and `SquadronRoles_InterceptorsHitHarder_FightersAreTougherSlowerAndCheaper` (7 pairs, relationships only). Reviewed, no change needed: `DarthVaderSquadronTests`, `LukeRedSquadronTests` (Luke DPS ~`675` stays top), `EconomyTuningTests`, `ShipAssetIntegrityTests`, Balance tool tests, `EnemyWingPricing.CHEAP_WING_PRICE` `300` (same cheap set: TIE Fighter, Vulture, Belbullab). Run 2026-11-21 on user request (EditMode, explicit off): `Combat` 320/320, `Squadrons` 232/232, `Balance` 70/70, `Economy` 138/138 passed.
- Reusable guidance → [[GameDesign/Combat]] (roles, per-faction contract, Empire rule, matrix rows); [[GameDesign/Units_And_Factions]] link.

## Current state (live assets, 2026-11-21)

| Row (damage × accuracy) | vs Fighter | vs Bomber | vs Interceptor |
|---|---|---|---|
| `FighterLaser` (`13`) | `1.5 × 0.8` | `1.5 × 0.85` | `1.75 × 0.75` |
| `InterceptorLaser` (`14`) | `1.0 × 0.75` | `2.5 × 0.9` | `1.5 × 0.8` |

- Bolt = family `7.5` × caliber × mount × unit `damageMultiplier`; `2` shots per `1.35 s`.
- Interceptor bolts are not stronger than fighter bolts: TIE Interceptor `15.75` = TIE Fighter `15.75`; Belbullab `9.4` < Vulture `10.5`.
- Fighters destroy interceptors almost for free: TIE Fighter destroys `3.5` TIE Interceptor squadrons in a row; X-Wing `2.1` A-Wing; ARC-170 `3.0` V-Wing.

### Empire check

| Rule | Live | Verdict |
|---|---|---|
| No shields, small hull | TIE `51/0`, Brute `73/0`, Interceptor `53/0`, Bomber `100/0`; X-Wing `170/48`, ARC-170 `123/16` | holds |
| Strong weapons (fighter) | squadron raw DPS: TIE `233`, X-Wing `225`, ARC-170 / V-19 `222`, Vulture `202` | holds, by 4% |
| Strong weapons (interceptor) | TIE Interceptor `187`; A-Wing `253`, V-Wing `190`, Belbullab `153` | fails |
| Faster craft (cruise) | TIE `58.2` vs X-Wing `78`, V-19 `73.8`, ARC-170 `62`, Vulture `56.2` | fails |
| Faster craft (interceptor) | TIE Interceptor `72.8` vs A-Wing `86.3`, V-Wing `79`, Belbullab `64.5` | fails |
| Faster craft (bomber) | TIE Bomber `32.2` vs BTL-A4 `39.5`, BTL-B `35.4`, Droid `30.2` | fails |

## Proposed changes (candidate v2)

### 1. Damage matrix — `DamageMatrixData.asset`

| Row | Field | Now | New |
|---|---|---|---|
| `13` FighterLaser | damage vs Interceptor | `1.75` | `1.5` |
| `14` InterceptorLaser | damage Fighter / Bomber / Interceptor | `1 / 2.5 / 1.5` | `1.5 / 1.5 / 1.5` |
| `14` InterceptorLaser | accuracy Fighter / Bomber / Interceptor | `0.75 / 0.9 / 0.8` | `0.8 / 0.85 / 0.75` |

- Rows equal against strikecraft → no matrix advantage either way.
- Row `13` accuracy unchanged (`0.8 / 0.85 / 0.75`). Rejected: accuracy vs Interceptor `0.6` (interceptors destroyed `2.0` base fighter squadrons in a row).
- Hold damage to large ships: divide row `14` Corvette…Structure damage and `shipShieldMultiplier` by `2.1` (average bolt factor).

| Row `14` field | Now | New |
|---|---|---|
| Corvette / Frigate / Cruiser / HeavyCruiser | `0.1 / 0.05 / 0.03 / 0.025` | `0.048 / 0.024 / 0.0143 / 0.0119` |
| Capital / BattleCruiser / Dreadnought / Structure | `0.02 / 0.015 / 0.01 / 0.02` | `0.0095 / 0.0071 / 0.0048 / 0.0095` |
| `shipShieldMultiplier` | `0.4` | `0.19` |

- One shared row, four bolt factors → result vs large ships: A-Wing `0.96×`, V-Wing `1.16×`, TIE Interceptor `1.09×`, Belbullab `0.84×` of today.
- Hero column unchanged.

### 2. Interceptor bolts — `WeaponsData.asset`

| Unit | Weapon | `damageMultiplier` | Bolt | Squadron raw DPS | Hits to kill base fighter |
|---|---|---|---|---|---|
| A-Wing | `141` | `5.7 → 11.467` | `42.75 → 86` | `253 → 510` | X-Wing `6 → 2` |
| V-Wing | `140` | `2.85 → 6.933` | `21.4 → 52` | `190 → 462` | V-19 `6 → 2`, ARC-170 `7 → 2` |
| TIE Interceptor | `139` | `2.1 → 4.8` | `15.75 → 36` | `187 → 427` | TIE Fighter `4 → 1` |
| Belbullab-22 | `122` | `0.4167 → 0.7333` | `9.4 → 16.5` | `153 → 269` | Vulture `3 → 1` |

- Fighter and bomber weapon definitions unchanged.

### 3. Hull — `*SquadronData.asset`

- TIE Interceptor `MemberHull` `53 → 50` (below TIE Fighter `51`).
- Belbullab-22 `MemberHull` `30 → 22` (below Vulture `23`).

### 4. Prices — `*Faction.asset`

| Unit | Now | New |
|---|---|---|
| X-Wing | `800` | `750` |
| ARC-170 | `600` | `550` |
| V-19 | `500` | `450` |
| TIE Fighter | `300` | `275` |
| TIE Brute | `450` | `425` |
| TIE Avenger | `550` | `525` |
| Vulture | `200` | `180` |
| A-Wing | `850` | `1050` |
| V-Wing | `650` | `750` |
| TIE Interceptor | `450` | `550` |
| Belbullab-22 | `300` | `350` |

- Interceptor ladder: `1050 / 750 / 550 / 350`. Bomber prices unchanged.
- Carrier and station squadrons are free → price changes touch direct purchase and AI only.

### 5. Empire speed — `*SquadronData.asset` (cruise / combat)

| Unit | Now | New | Fastest rival |
|---|---|---|---|
| TIE Fighter | `58.24 / 65.52` | `80 / 90` | X-Wing `78 / 89.4` |
| TIE Brute | `57.2 / 64.48` | `74 / 83` | V-19 `73.8 / 84.2` |
| TIE Interceptor | `72.8 / 81.12` | `90 / 101` | A-Wing `86.3 / 97.8` |
| TIE Bomber | `32.24 / 37.44` | `41 / 47` | BTL-A4 `39.5 / 45.8` |

- Darth Vader escorts follow the TIE Fighter (`DarthVaderSquadronTests` contract).
- Fighters stay slower than interceptors in every faction.

## Expected result (DuelSim model, 600 trials)

### Fighter vs interceptor (squadrons destroyed in a row)

| Pair (new prices) | Interceptor destroys, now → new | Fighter destroys, now → new | Price ratio |
|---|---|---|---|
| A-Wing (1050) vs X-Wing (750) | `0.04 → 1.55` | `2.14 → 0.25` | `1.40` |
| V-Wing (750) vs ARC-170 (550) | `0.01 → 1.29` | `3.00 → 0.31` | `1.36` |
| V-Wing (750) vs V-19 (450) | `0.03 → 1.32` | `2.50 → 0.29` | `1.67` |
| TIE Interceptor (550) vs TIE Fighter (275) | `0.05 → 1.65` | `3.52 → 0.21` | `2.00` |
| TIE Interceptor (550) vs TIE Brute (425) | `0.01 → 0.50` | `4.77 → 1.21` | `1.29` |
| TIE Interceptor (550) vs TIE Avenger (525) | `0.01 → 0.52` | `4.69 → 1.24` | `1.05` |
| Belbullab (350) vs Vulture (180) | `0.10 → 0.96` | `2.40 → 0.75` | `1.94` |

### Interceptor ladder

- TIE Interceptor destroys `1.31` Belbullab squadrons (price ratio `1.57`) → Belbullab is weaker.
- V-Wing destroys `1.37` TIE Interceptor (`1.36`); A-Wing destroys `1.12` V-Wing (`1.40`).

### Bombers — seconds for a full squadron to destroy a bomber squadron

| Faction | Bomber | Interceptor, now → new | Fighters (unchanged) |
|---|---|---|---|
| Rebellion | BTL-A4 | `1.0 → 1.1` | X-Wing `2.4` |
| Republic | BTL-B / NTB-630 | `1.8 → 1.0` | ARC-170, V-19 `2.4 / 2.1` |
| Empire | TIE Bomber | `2.0 → 1.2` | TIE `2.6`, Brute `2.0`, Avenger `2.0` |
| Empire | Punisher | `2.3 → 2.0` | TIE `3.0`, Brute `2.5`, Avenger `2.3` |
| CIS | Droid / Hyena | `2.4 → 2.5` | Vulture `2.6 / 3.3` |

### Empire after the change

- Speed: fastest base fighter, interceptor and bomber.
- Weapons: TIE Interceptor `427` raw DPS at `550` → highest interceptor damage per credit (`0.78`; A-Wing `0.49`, V-Wing `0.62`, Belbullab `0.77`). TIE Fighter keeps the highest base-fighter squadron DPS.
- Durability: lowest hull among non-swarm factions, no shields.

## Rebellion mass-fight boost (user chose 2026-11-21: stronger squadrons, no extra craft)

### Squadrons in battle (population cap `120`, live data)

| Fleet | Empire | Rebellion |
|---|---|---|
| Carriers only, out at once (incl. station) | `41` (19 F / 11 I / 11 B) | `18` (12 F / 3 I / 3 B) |
| Craft in the air | ~`416` | ~`81` |
| Total launches in reserve | ~`195` | ~`144` |
| Carriers + bought squadrons, maximum | ~`83` | ~`45` |

- Assumed: carrier squadrons use no population (not verified in code).
- 3-at-once carriers: Empire ISD I, ISD II, Imperial Venator, Devastator, Chimaera (3 squadrons per 8–10 population); Rebellion Home One, MC80 Independence (3 per 25–30).
- Ship limit `0` (no squadrons out): Corellian Battlecruiser, Dauntless, MC75 Profundity, Nebula, Executor.

### Finding (mass model on candidate v2, all squadrons fight at once)

| Fight | Rebel survivors | Empire survivors |
|---|---|---|
| Carrier fleets 18 vs 41 | `0%` | `89%` |
| Equal credits, 11 X-Wing vs 30 TIE Fighter (8250) | `0%` | `87%` |
| Equal credits, 3 X-Wing + 2 A-Wing vs 6 TIE Fighter + 5 TIE Interceptor | `0%` | `87%` |
| One carrier wing each (X/A/Y vs TIE F/I/B) | `26%`, wins `55%` | `16%` |

- More bolt damage does not help: Rebel bolts already kill in 1–2 hits and a salvo stays on one craft. Damage and hull ×`2` → Empire still keeps `47–59%`.
- Levers that work: hull + shields, and fire rate (`reloadMultiplier`).

| Rebel fire rate | Hull + shields for an even equal-credit fight |
|---|---|
| unchanged | ~×`4` |
| ×`1.5` (reload ×`0.67`) | between ×`2` and ×`3` |
| ×`2` (reload ×`0.5`) | ~×`2` |

### Rejected (user, 2026-11-21): hull + shields ×`2.5`, fire rate ×`1.5`

- Too strong: 6 X-Wing then beat 10 Brute, 8 Avenger or 8 TIE Interceptor (~4200 credits) without losses.

### Proposal v2 (smaller boost)

- X-Wing and A-Wing hull + shields ×`1.25`: X-Wing `170/48 → 213/60`, A-Wing `133/30 → 166/38`. Fire rate and bolt damage unchanged.
- Luke Red Squadron wingmen follow the X-Wing (`LukeRedSquadronTests` contract).
- Remaining weakness, accepted with the smaller boost: equal credits against cheap TIE Fighters still lose (11 X-Wing vs 30 TIE Fighter → Empire keeps `84%`; 1 X-Wing vs 2 TIE Fighter → Rebel wins `0%`).
- One X-Wing squadron still beats one Brute (`99%`) or one TIE Interceptor (`86%`).
- Mass model: random targets, continuous fire, no attack runs, no reinforcement from reserve. Results switch sharply near parity → verify in battle.

## Empire elite squadrons (user rules 2026-11-21)

- TIE Defender is the strongest squadron: it beats any Rebel squadron. Bought only; no carrier or station bay holds type `207` (verified in Empire ship data and `SpaceStationData`).
- TIE Avenger has good chances against the X-Wing.

- User approved both buffs below (2026-11-21).

### Free deployments of elite squadrons (live data)

- TIE Defender (`207`): no bay anywhere.
- TIE Avenger (`202`): one bay, `GladiatorShipData` `1/3` (active/reserve). Ship limit `1` → one squadron out at a time, rotating.
- Gladiator bays now: TIE Fighter `0/0`, TIE Bomber `1/3`, Avenger `1/3`, TIE Interceptor `1/3`, Brute `1/3`, Punisher `1/3`.
- Proposal: Avenger `1/3 → 0/0`, TIE Fighter `0/0 → 1/3`. Bay order and indices stay; total launches stay `15`.
- Effect: Gladiator (4500) loses `3` free Avenger launches worth `1575` credits, gains `3` TIE Fighter launches worth `825`.
- Optional, not requested: Punisher `1/3 → 0/0` with TIE Bomber `1/3 → 1/6`; Brute stays (425, not buffed).
- Update the Gladiator description in `EmpireFaction.asset` ("one TIE Brute and one TIE Avenger squadron") and the Avenger / Defender stat text.

### TIE Avenger (525)

- `MemberHull` `64 → 78`; shields `12`, bolt `19.1`, speed unchanged.
- Why: X-Wing bolt deals `27.6` through the shield, then `45.6` → hull `64` dies in 2 hits, hull `78` needs 3.

| Fight (Rebel hull ×`1.25`) | Now | With hull `78` |
|---|---|---|
| 1 X-Wing vs 1 Avenger, X-Wing wins | `100%` | `52%` |
| 3 X-Wing vs 4 Avenger (2250 / 2100), X-Wing wins | `67%` | `0%` |
| 1 A-Wing vs 1 Avenger, A-Wing wins | `84%` | `94%` |
| 1 TIE Interceptor vs 1 Avenger, Interceptor wins | `14%` | `17%` |

- Gap: at equal credits Avengers beat X-Wings outright. Alternative with a softer result: bolt ×`1.5` at hull `64` → 1 vs 1 X-Wing wins `90%`, 3 vs 4 Avenger wins.

### TIE Defender (1000, 3 craft, population 2, max 3)

- Live: hull `105`, shields `50`, bolt `36` (weapon `158`, multiplier `4.8`), laser squadron DPS `160`. One X-Wing squadron destroys `3.3` Defender squadrons in a row.

| Field | Now | New |
|---|---|---|
| `MemberHull` / `MemberShields` | `105 / 50` | `420 / 200` |
| Weapon `158` `damageMultiplier` | `4.8` | `9.6` (bolt `72`) |
| Weapon `158` `reloadMultiplier` | `1` | `0.67` |
| Laser squadron DPS | `160` | `478` |

| Fight (Rebel hull ×`1.25`) | Rebel wins, now | Rebel wins, new |
|---|---|---|
| 1 X-Wing vs 1 Defender | `100%` | `0%` |
| 1 A-Wing vs 1 Defender | `100%` | `1%` |
| 4 X-Wing vs 3 Defender (3000 each) | `100%` | `0%` |
| 3 A-Wing vs 3 Defender (3150 / 3000) | `100%` | `0%` |

- Smaller steps tested: hull ×`3` + bolt ×`2` without faster fire → A-Wing still wins `57%`.
- Limit: 2 TIE Interceptor squadrons (1100) beat 1 new Defender (`97%`). Strongest per squadron, not per credit against swarms.
- Torpedoes (weapon `159`) unchanged and not modelled.

## Known gaps in candidate v2

- Accepted: A-Wing (1050) destroys only `0.77` TIE Interceptor squadrons (price ratio `1.91`).
- Accepted: Belbullab kills Droid Bombers no faster than Vulture (`2.5 s` vs `2.6 s`).
- TIE Brute and Avenger beat the TIE Interceptor at `0.75` (`1.21` / `1.24` vs `0.50` / `0.52`), not an even match. Allowed by decision; check in step 3.
- Belbullab vs Vulture is narrow: `0.96` vs `0.75`, far below the price ratio `1.94`. Fix in step 3 (Belbullab price `300`, or Vulture needs 3 hits on Belbullab).
- About 45 thin breakpoints on pairs with interceptors (e.g. A-Wing → X-Wing `0.5%`, TIE Interceptor → TIE Punisher `0.2%`). One hull point flips those results → tuning pass required.
- Fighters lose to interceptors of the same faction at equal credits. Their value: price, durability against ship point defence, torpedoes (X-Wing, ARC-170).
- Equal-credit fights with both sides present at once (2 TIE Fighter squadrons vs 1 TIE Interceptor) are not modelled; add to step 2.
- TIE Brute: hull `73` needs 2 interceptor hits (`36 × 1.5 = 54`); Brute needs 2 hits on hull `50` with 10 guns vs 8. One-hit kill needs bolt `≥ 49`.
- Faster TIEs change chase, escort and attack-run timing; not modelled.

## Implementation

1. Confirm assumptions and candidate v2 with the user.
2. Extend `Tools/SquadronRebalance/DuelSim.py`: fighter-vs-interceptor pairs, interceptor ladder, bomber wipe times, new prices. Read-only.
3. Tuning pass in DuelSim: remove thin breakpoints, close the gaps above; record final values here.
4. Apply matrix rows `13`/`14`, weapon multipliers `122`/`139`/`140`/`141`, hull, Empire speeds, prices.
5. Unity refresh → `ForceReserializeAssets` on changed paths → `SaveAssets` → Console check.
6. Tests: replace `WeaponTaxonomyTests.SquadronRoles_InterceptorsCounterBombers_FightersCounterInterceptors` (asserts the old rule). New contract: rows `13` and `14` equal against the three strikecraft classes; per faction interceptor bolt > fighter bolt, fighter hull + shields > interceptor, fighter speed < interceptor, interceptor price > fighter price. Relationships only, no tuned numbers.
7. Review: `EconomyTuningTests`, `DarthVaderSquadronTests`, `UnitMatchupData` tooltips, faction descriptions (hull, speed, price text), `EnemyWingPricing` (`CHEAP_WING_PRICE`), `EnemyProductionStrategy`, `EnemySquadronCommander`.
8. Update [[GameDesign/Combat]] and [[GameDesign/Units_And_Factions]].
9. User battle check → [[TODOs/User_Acceptance_Queue]].

## Edge Cases

- Hero squadrons use `HeroLaser`; untouched. Weapon `127` (family `24`) is in no regular loadout — check its users before step 4.
- Bomber guns are `FighterLaser` → bombers lose the `1.75` bonus and hit interceptors less often.
- Ship `Laser` and `PointDefense` rows keep their own interceptor accuracy (`0.55`, `0.65`).
- Member counts unchanged → old saves stay valid.
- DuelSim ignores attack runs, speed, arcs, regeneration, torpedoes, abilities and research.

## Tests

- Categories: `Combat`, `Balance`, `Squadrons`, `Economy`; `EnemyAi` if AI code changes.
- Not run (local override).

## Files

- `Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset`, `WeaponsData.asset`
- `Assets/Settings/Data/Squadron/{TIEFighter,TIEBrute,TIEInterceptor,TIEBomber,Belbullab22,DarthVaderTIEAdvanced}SquadronData.asset`
- `Assets/Settings/Data/Factions/*/*Faction.asset`
- `Assets/Settings/Data/Tooltip/Matchups/*SquadronMatchups.asset`
- `Assets/Scripts/Tests/Editor/Combat/WeaponTaxonomyTests.cs`
- `Tools/SquadronRebalance/DuelSim.py`

## TODO

- [x] User confirms assumptions and candidate v2 (2026-11-21).
- [x] Steps 2–8 (2026-11-21).
- [ ] Step 9 (user): battle check → [[TODOs/User_Acceptance_Queue]].
