---
category: Features
status: done
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Boba Fett — Slave I

## Goal

- Add Boba Fett — Slave I as a unique single-fighter skirmish hero for Empire. One purchase deploys one visible, targetable member.
- Tightest turning circle and highest sustained gun output of the three. Give Boba less hull than Jango and keep his bomb threatening against clustered fighters.

## Important Values

| Proposed Unity value | Target |
|---|---:|
| Members / maximum owned | 1 / 1 |
| Member hull / shields | 820 / 480 HP |
| Cruise / combat | 103 / 112 units/s |
| Acceleration / turn | 52 units/s² / 240°/s |
| Nominal primary gun output | 425 raw DPS |
| Station / credits / build / population | 2 / 1700 / 28 s / 2 |

- 1,300 HP vs an Interceptor squadron’s 200 (6.5×). 425 raw gun DPS vs 356 (+20%). 112 combat speed vs the Avenger’s 92 (+22%). 240°/s vs 120°/s (+100%).
- Empire station level 2, 1,700 credits and 28 s follow the vanilla skirmish purchase. Compare cost and sustained all-guns-up damage as well as individual duels.
- Original game stats, reference squadron inputs, weapon calculation and shared duel protocol → [[Done/Features/Skirmish_Hero_Fighters - Research]].

## Implementation

1. [x] **Import.** Use vanilla `EV_SLAVE1_SPACE.ALO` and the vanilla `EV_SLAVE1.DDS` hull. Reuse Jango’s verified geometry, UVs and sockets with an independent material binding. Follow [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] and [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]]. Verify source visibility, barrel locations, fighter-scale bounds, axes, textures and in-game silhouette. Create a fighter visual, squadron view, placement preview and faction icon.
2. [x] **Build one fighter.** Add `SquadronType.BobaFettSlaveI` (candidate 206; recheck availability). Create `BobaFettSlaveISquadronData` and `BobaFettSlaveISquadronView` using a one-member `SquadronViewSpec`. Wire exactly one `FighterView` into flight and health. Fit collision and muzzle positions to the visible hull.
3. [x] **Arm and tune.** Create a hero-specific weapon profile for the primary gun rather than stacking standard light guns (425 DPS ≈ 38 light guns at 11.11 DPS). Keep the hardpoint count small and its sustained profile DPS summing to 425. Re-bake `MemberCount` and weapon loadout from the prefab for AI. Verify firing passes and shot arcs as well as the balance estimate.
4. [x] **Signature ability.** Unity loaded Boba’s sole ability slot (ID 54), its Harmonic Bomb prefab, and activation voice `U000_BFT1612_ENG` from the saved squadron view. Fuse 1.5 s; recovery 20 s. Shared implementation and in-game acceptance → [[Done/Features/Harmonic_Bomb_Ability]].
5. [x] **Register.** Add the faction’s squadron row (`isHero=true`, `MaxCount=1`), exact data/view address keys, icon and `ReinforcementData.spawnSquadronWrapper` preview reference. Verify the faction’s skirmish reinforcement shop, AI purchase eligibility and single portrait. Keep this hero out of hangar complements.
   - One-at-a-time limit: player side already supports hero squadrons (commit `d15d88277`, `PlayerFactionModel` keyed by `UnitTypeId`); AI side relies on `MaxCount: 1` in `EnemyUnitLimitModel`, held until the squadron is released. Verify only.
   - AI pick rate: `EnemyProductionStrategy.AddCounterOption<SquadronUnitRequest>` treats the hero as an anti-fighter counter. Check how often the AI rebuys it, not only whether it can.
6. [x] **Audio.** Add a `ShipSfxData` squadron entry (engine, explosion, `Ready` voice line). Without one, `GetUnit(SquadronType)` falls back to generic engine/explosion and the announcer plays "construction complete". Record it if the fallback is accepted.
   - Previewed English WAV candidates under `F:/Temp_audio/Empire_Rebellion_Audio/Audio/Shared/Voices/Empire/` (source verified; in-game listening pending).
   - Select → `Unit_Select_Ship_Boba_Fett/U000_BFT1403_ENG.wav`.
   - Move → `Unit_Move_Ship_Boba_Fett/U000_BFT1502_ENG.wav`.
   - Attack → `Unit_Attack_Ship_Boba_Fett/U000_BFT1602_ENG.wav`.
   - Harmonic bomb activation → `Unit_Ability_Boba_Fett/U000_BFT1612_ENG.wav`.
   - Use vanilla Slave I ship-order events. The separate `Unit_Ability_Boba_Fett` cue plays on bomb activation.
   - Verify one cue per local order or successful ability activation. Select/move/attack/ability clips cover separate triggers from the pending production `Ready` line. Include `Audio` in verification.
7. [x] **Validate (source and stationary simulator).** Live acceptance is queued below. First check flight: overshoot and time to turn back onto a target at 240°/s and 112 u/s (roster max 155°/s, 92 u/s). Fix break/extend geometry before balance work. Then run the planned matchup protocol against TIE Interceptor and TIE Avenger. Check queued, held and deployed purchase limits, cancellation, death and repurchase, one health target, independent owner limits, save/load and fresh pooled deployment. Record measured results and final tuning.

## Verification

- Visual and preview share `SlaveI_Hull.asset`: 1,580 triangles, one renderer, four recovered muzzle sockets. Unity confirmed matching vertices, normals, UVs and triangle indices against the imported vanilla hull. The Boba material uses the vanilla paint and gloss; recursive prefab dependencies exclude Jango's paint.
- Empire squadron key `206` resolves `BobaFettSlaveISquadronData` and `BobaFettSlaveISquadronView`. Both flight and health bind the same single fighter. Two forward gun mounts use `BobaFettTwinLaser` (`126`), 425 raw sustained DPS combined. Each muzzle coincides with its source socket; yaw is ±22.5°.
- The shop, deployed hero portrait, world icon and tooltip use the vanilla Boba portrait. The placement preview uses the Empire view group. Empire station level 2, 1,700 credits, 28 s, two population, one reserved copy. No hangar complement was edited.
- `PlayerFactionModel`, `FactionService` and reinforcement save restoration reserve/release the owning player's typed hero across queue, cancellation, reserve, deployment, destruction and reload. `EnemyProductionStrategy` reads the Empire squadron row and `EnemyUnitLimitModel` enforces max one. AI rebuy frequency requires battle telemetry.
- Unity resolved the Slave I engine loop and Boba's ready line (`U000_BFT0801_ENG`).
- Review fix 2026-11-06: the explosion entry was the unused roster default (`EXP_CRUISER_1/2`) and the squadron view had `playDestructionSound: 0`, so Boba had no hero death sound. Now uses Jango's corvette bank (`EXP_CORVETTE_1/2`) with `playDestructionSound: 1`; Unity read back the flag. Shop description now mentions the harmonic bomb. Select/move/attack resolve the three planned clips through separate order handlers.

### Stationary gun balance

- Project Battle Simulator, live assets, separation 100 units, seeds 1–10, both starting sides, no abilities or research. Each pair of columns below gives Boba as side A / side B. Time is mean battle duration in seconds. Seed 1 HP is Boba's hull + shields. Shots/hits are Boba's.

| Opponent | Wins out of 10 (A / B) | Mean seconds | Seed 1 HP | Seed 1 shots/hits |
|---|---|---|---|---|
| 1 Interceptor wing | 10 / 10 | 3.52 / 3.35 | 820+247 / 820+199 | 16/13 · 20/16 |
| 2 Interceptor wings | 0 / 0 | 4.07 / 4.10 | 0 / 0 | 20/16 · 21/17 |
| 1 Avenger wing | 10 / 10 | 7.19 / 7.79 | 760+284 / 820+289 | 32/25 · 28/22 |
| 2 Avenger wings | 3 / 2 | 10.49 / 10.74 | 388+0 / 0 | 52/42 · 48/37 |

- Keep the proposed gun and flight values for live acceptance. Nominal combat turn radius 26.7 units; a half-turn at the full member turn rate takes 0.75 s. These are calculated limits. The simulator assumes stationary units and omits attack passes, missile interception and abilities.

## TODO

- Shared bomb integration completed separately in [[Done/Features/Harmonic_Bomb_Ability]].
- Reusable balance evidence → [[Done/Features/Skirmish_Hero_Fighters - Research]].
- Live skirmish acceptance → [[TODOs/User_Acceptance_Queue]]: silhouette and placement, overshoot/reacquisition and moving-target duels, AI purchase frequency, queue/cancel/death/repurchase for independent owners, pool reuse, save/load and listening. Automated tests were not run under the checkout override.