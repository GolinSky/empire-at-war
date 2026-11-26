---
category: Bugs
status: done
acceptance: pending-user
design_state: applied-v4-salvo-model
created: 2026-11-19
updated: 2026-11-19
completed: 2026-11-19
system:
  - balance
  - squadrons
  - hangars
---
# Squadron Faction Balance

## Goal
- Squadron strength follows faction identity after gun consolidation (`8f23d1266`) and bay rotation ([[Review/Features/Squadron_Bay_Rotation_Plan]]).
- **Strength follows purchase price** (user, 2026-11-19): X-Wing `800` ≈ 2.67 TIE Fighter squadrons `300`. Prices stay.
- Follow-up to [[Review/Bugs/Carrier_and_Squadron_Rebalance]]. Craft counts unchanged: Rebel 5/4/3, Republic 8/6/4, Empire 10/8/6, CIS 13/11/10.

## Outcome (2026-11-19)
- Applied **model-tuned values** (user chose them over the v2 table): `Tools/SquadronRebalance/DuelSim.py` disagreed with the v2 model (v2 gave X-Wing `2.13`, A-Wing `0.88` TIE squadrons in a row).
- Rebel station bay A-Wing `2/4` → **X-Wing `2/6`** (user chose `2/6`).
- Darth Vader escorts follow the TIE Fighter (hull `50`, laser `165` dm `2.1`): keeps `DarthVaderSquadronTests` contract "escorts are ordinary TIEs".
- Verification: Unity refresh + explicit reserialize + `SaveAssets`; no asset import/serialization errors. Unrelated compile errors from uncommitted Enemy AI WIP (`EnemyStrategicContextBuilder.cs`, `EnemyShipTaskGroupPlanner.cs`). Tests not run (local override).

## Faction identity (user, 2026-11-19)
- Rebellion: strongest squadrons, fewest craft. Republic: balanced; ARC-170 slow, more hull.
- Empire: strong weapons, many craft, no shields, small/medium hull. CIS: swarm, weakest craft.

## User decisions 2026-11-19
- No extra squadrons out per ship (FPS, vision) → deployment caps unchanged.
- Quasar Fire is Empire; asset still in `Ship/Rebellion/` (folder only).
- Keep prices; tune strength to price.
- Interceptors and bombers follow price.

## Finding — why TIE Fighters beat X-Wings (live before change)
- One bolt per fighter since consolidation → **hits-to-kill breakpoints decide duels**.
- X-Wing bolt `18.9` × `1.5` = `28.35`; two hits `56.7` vs TIE hull `58` → 3 hits.
- DuelSim live: X-Wing destroys `0.57` TIE squadrons in a row; TIE destroys `0.86` X-Wing squadrons.

## Applied stats
- Bolt = family damage `5` × caliber (`2.86` heavy) × mount × unit `damageMultiplier`.
- In a row = DuelSim, 800 trials, vs TIE Fighter unless noted.

| Unit | Craft | Price | Bolt | `damageMultiplier` (type) | Hull | Shields | In a row (target) |
|---|---:|---:|---|---|---|---|---|
| X-Wing | 5 | 800 | 18.9 → 20.25 | 3.78 → 4.05 (`24`) | 95 → 170 | 15 → 48 | 2.71 (2.67) |
| ARC-170 | 8 | 600 | 11.25 → 12.5 | 0.7867 → 0.8741 (`135`) | 60 → 123 | 8 → 16 | 2.07 (2.0) |
| V-19 | 8 | 500 | 11.81 → 12.5 | 2.3625 → 2.5 (`138`) | 58 → 104 | 8 → 16 | 1.82 (1.67) |
| TIE Avenger | 10 | 550 | 9.225 → 12.75 | 1.845 → 2.55 (`133`) | 54 → 64 | 8 → 12 | 1.79 (1.83) |
| TIE Brute | 10 | 450 | 9.9 → 12.5 | 0.6923 → 0.8741 (`132`) | 64 → 73 | 0 | 1.39 (1.5) |
| TIE Fighter | 10 | 300 | 9.45 → 10.5 | 1.89 → 2.1 (`131`, `165`) | 58 → 51 | 0 | anchor |
| Vulture | 13 | 200 | 6.92 → 7.0 | 1.3846 → 1.4 (`130`) | 44 → 23 | 0 | TIE destroys 1.78 (1.5) |
| A-Wing | 4 | 850 | 22.5 → 28.5 | 4.5 → 5.7 (`141`) | 100 → 133 | 15 → 30 | vs TIE Int 1.96 (1.89); vs TIE 1.08 |
| V-Wing | 6 | 650 | 14.25 keep | keep | 64 → 88 | 8 → 16 | vs TIE Int 1.27 (1.44) |
| TIE Interceptor | 8 | 450 | 10.125 → 10.5 | 2.025 → 2.1 (`139`) | 62 → 53 | 0 | interceptor anchor |
| Belbullab-22 | 11 | 300 | 6.95 → 6.25 | 0.4636 → 0.4167 (`122`) | 44 → 30 | 0 | TIE Int destroys 1.88 (1.5) |

- ARC-170 speed: cruise `71.76 → 62`, combat `81.12 → 70`.
- X-Wing vs others (in a row): ARC-170 `0.93`, Avenger `1.01`, Brute `1.02`, A-Wing `1.80`. Avenger destroys `0.28` X-Wing squadrons; TIE `0.00`.
- Luke Red Squadron wingmen follow the X-Wing: `MemberHull` `120 → 180`, escort laser `170` multiplier `3.6 → 4.05` (squadron DPS `650 → 675`). `LukeRedSquadronTests` expectations updated.

### Bombers — torpedo volley follows price
- Volley = `72` × caliber × unit multiplier × craft × `WarheadDamageMultiplier`. Target = `333` × price ÷ `450`.
- Torpedo multipliers: BTL-A4 `157` `1.3169 → 2.157` (volley `629`); BTL-B `150` `1.2444 → 1.67` (`481`); NTB-630 `152` `1.1556 → 1.413` (`407`); Punisher `147` `0.8296 → 1.028` (`444`); Droid/Hyena `143` `0.4267 → 0.3083` (Droid `222`); Hyena `WarheadDamageMultiplier` `1.2 → 1.333` (`296`). TIE Bomber `333` anchor.
- Bomber hulls, lasers, ion, missiles unchanged.

## DuelSim model
- Reads live `WeaponsData`, `DamageMatrixData`, `Squadron/*SquadronData`.
- **One random alive target per salvo**; bolts after the kill are wasted. Matches `WeaponHardPoint`: one attack sequence = one target craft.
- Ignores torpedoes/ion vs craft, attack runs, arcs, speed, regeneration, abilities, research.
- Thin rule (kills in ≤ 6 hits): hull left before the killing hit < 5% (base, ×1.1 hull, ×1.1 damage) or overkill on the killing hit < 2% (base).
- Remaining thin fighter pairs, none at base overkill: Avenger → Brute, TIE → Brute, V-Wing → Brute, TIE Int → TIE Int, TIE Int → Belbullab, Belbullab → Vulture, Belbullab → TIE Int. Fighter → bomber pairs untouched (bomber hulls out of scope).

## Follow-up 2026-11-19 (verification fixes)
- v3 model picked a new target per bolt → understated few-craft, big-bolt squadrons. Corrected model on v3 values: X-Wing `2.91`, A-Wing `2.18` vs TIE Int, V-19 `1.49`, Brute `1.37`.
- v3 check missed kills with zero overkill: ARC/V-19/Brute → Brute `75.0` vs hull `75`; Belbullab → Vulture `25.0` vs `25`; Vulture/TIE Int → TIE `50.6` vs `50`; TIE Int → Belbullab `30.4` vs `30`.
- Retuned (v3 → v4): X-Wing hull `182 → 170`, ARC-170 `106 → 123`, V-19 `96 → 104`, Avenger `57 → 64`, Brute `75 → 73`, TIE Fighter and Vader escorts `50 → 51`, TIE Interceptor `56 → 53` and bolt `10.125 → 10.5`, Vulture `25 → 23` and bolt `6.75 → 7.0`, A-Wing `142 → 133`.
- Results are stepwise (one hit more or less): Brute `1.39`, V-Wing `1.27`, Vulture and Belbullab sit off target; next step up overshoots (Brute hull `80` → `1.76`; Belbullab hull `32` → TIE Int destroys `0.91`).
- Verification: Unity refresh + explicit reserialize + `SaveAssets`; Console 0 errors; values read back from disk. Tests not run (local override). Not committed.

## Limits
- All-at-once brawls compound differently from wave fights; credit parity there not targeted.
- Carrier and station squadrons are free: stronger X-Wings raise Rebel bay value.
- Brute (`1.39` vs `1.5`) and V-Wing (`1.27` vs `1.44`) stay below target; V-19 (`1.82` vs `1.67`) above.

## Deployment (unchanged, for reference)
| Faction | 3 at once | 2 at once | 1, rotating | None |
|---|---|---|---|---|
| Empire | ISD I, ISD II, Imperial Venator | — | Acclamator Assault, Gladiator, Interdictor, Quasar Fire | Executor |
| Rebellion | Home One, MC80 Independence | — | Bothan, Endurance, Liberator, MC80 Justice, Remonda, Freedom, Mediator | Corellian BC, Dauntless, MC75 Profundity, Nebula |
| Republic | Venator, Venator II | Secutor | Acclamator, Praetor, Resolute | Imperator, Mandator, Procurator, Rothana |
| CIS | Lucrehulk | — | Captor, DH-Omni, Invincible, Providence, Wavecrest | Bulwark, Bulwark II, Recusant |

- Stations (active/reserve): Rebel **X-Wing `2/6`**, Republic ARC-170 `3/9`, Empire TIE Fighter `4/12`, CIS Vulture `5/20`.

## Edge Cases
- Ship point-defence vs fighter hull ×`2`: Vulture hull `23` dies much faster to ships; X-Wing hull `170` lasts much longer.
- Member counts unchanged → old saves stay valid.
- Faction descriptions updated (hull, shields, torpedo volleys, Hyena "33% harder").

## Tests
- Categories: `Balance`, `Squadrons`, `Combat`; `GameFlow`/`Ships` for station bay.
- Reviewed, no edits needed: `DarthVaderSquadronTests` (escort hull/laser kept equal to TIE), `LukeRedSquadronTests` (updated: wingmen hull `180`, total hull `4500`, DPS `675`), `BalanceInventoryTests`, `HangarModelTests`, `QuasarFireHangarTests`, `SquadronHealthModelTests`.
- Not run (local override).

## Reusable guidance
- No new canonical rule; DuelSim usage recorded in `Tools/SquadronRebalance/README.md`.

## TODO
- [x] User approves stats (model-tuned, 2026-11-19).
- [x] Open questions answered (Rebel station `2/6`).
- [x] Steps 1–5: DuelSim, stats, station bay, descriptions, refresh/reserialize/save.
- [ ] Step 6 (user): battle check X-Wing vs TIE Fighter, TIE vs Vulture, A-Wing vs TIE Fighter, Rebel station launch → [[TODOs/User_Acceptance_Queue]].

## Files
- `Assets/Settings/Data/Squadron/*SquadronData.asset` (incl. `DarthVaderTIEAdvanced`)
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`
- `Assets/Settings/Data/Models/SpaceStation/SpaceStationData.asset`
- `Assets/Settings/Data/Factions/*/*Faction.asset`
- `Tools/SquadronRebalance/DuelSim.py`, `README.md`
