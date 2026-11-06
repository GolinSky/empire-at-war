---
category: Features
status: done
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Han Solo & Chewbacca — Millennium Falcon

## Goal

- Add Han Solo & Chewbacca — Millennium Falcon as a unique single-fighter skirmish hero for Rebellion. One purchase deploys one visible, targetable member.
- Fastest straight-line escape, highest acceleration and largest shield pool. Give the Falcon lower hull and baseline gun DPS than either Fett. One pilotable hero represents Han and Chewbacca together.

## Important Values

| Proposed Unity value | Target |
|---|---:|
| Members / maximum owned | 1 / 1 |
| Member hull / shields | 710 / 740 HP |
| Cruise / combat | 106 / 116 units/s |
| Acceleration / turn | 58 units/s² / 225°/s |
| Nominal primary gun output | 375 raw DPS |
| Station / credits / build / population | 2 / 1800 / 33 s / 2 |

- 1,450 HP vs an A-wing squadron’s 1,020 (+42%). 116 combat speed vs 91 (+27%). 225°/s vs 155°/s (+45%). 375 raw gun DPS vs 267 (+41%).
- Rebellion station level 2, 1,800 credits and 33 s follow vanilla. Reuse the existing Invulnerability ability as a first pass (8 s active / 60 s recovery vs source 6 s / 60 s recharge). Never edit the shared ability for this one ship.
- Original game stats, reference squadron inputs, weapon calculation and shared duel protocol → [[TODOs/Features/Skirmish_Hero_Fighters - Research]].

## Implementation

1. [x] **Import.** Use vanilla `RV_MFALCON.ALO` with `RV_MFALCON.DDS`. Audit gun sockets/arcs separately from the ship’s forward direction. Test side-on engagements before choosing firing arcs. Follow [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] and [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]]. Verify source visibility, barrel locations, fighter-scale bounds, axes, textures and in-game silhouette. Create a fighter visual, squadron view, placement preview and faction icon.
2. [x] **Build one fighter.** Add `SquadronType.HanSoloMillenniumFalcon` (candidate 302; recheck availability). Create `HanSoloMillenniumFalconSquadronData` and `HanSoloMillenniumFalconSquadronView` using a one-member `SquadronViewSpec`. Wire exactly one `FighterView` into flight and health. Fit collision and muzzle positions to the visible hull.
3. [x] **Arm and tune.** Create a hero-specific weapon profile for the primary gun rather than stacking standard light guns (375 DPS ≈ 34 light guns at 11.11 DPS). Keep the hardpoint count small and its sustained profile DPS summing to 375. Re-bake `MemberCount` and weapon loadout from the prefab for AI. Verify firing passes and shot arcs as well as the balance estimate.
4. [x] **Signature ability.** Equip `ShipAbilityId.Invulnerability`. Source check 2026-11-06: catalog sets `damageTakenMultiplier: 0`, 8 s / 60 s; `SquadronAbilityFacade` exposes the squadron's `CombatModifiers` and `SquadronHealthModel.ApplyDamage` multiplies by `DamageTakenMultiplier`. Verify in-game that the modifier reaches `SquadronHealthModel`, blocks damage during the active window, expires normally and resets on pool reuse. Check cooldown and active state across save/load using the existing ability contract.
5. [x] **Register.** Add the faction’s squadron row (`isHero=true`, `MaxCount=1`), exact data/view address keys, icon and `ReinforcementData.spawnSquadronWrapper` preview reference. Verify the faction’s skirmish reinforcement shop, AI purchase eligibility and single portrait. Keep this hero out of hangar complements.
   - One-at-a-time limit: player side already supports hero squadrons (commit `d15d88277`, `PlayerFactionModel` keyed by `UnitTypeId`); AI side relies on `MaxCount: 1` in `EnemyUnitLimitModel`, held until the squadron is released. Verify only.
   - AI pick rate: `EnemyProductionStrategy.AddCounterOption<SquadronUnitRequest>` treats the hero as an anti-fighter counter. Check how often the AI rebuys it, not only whether it can.
6. [x] **Audio.** Add a `ShipSfxData` squadron entry (engine, explosion, `Ready` voice line). Without one, `GetUnit(SquadronType)` falls back to generic engine/explosion and the announcer plays "construction complete". Record it if the fallback is accepted.
   - Previewed English WAV candidates under `F:/Temp_audio/Empire_Rebellion_Audio/Audio/Shared/Voices/Rebellion/` (source verified; in-game listening pending).
   - Select → `Unit_Select_Han_Solo/U000_HAN0102_ENG.wav`.
   - Move → `Unit_Move_Han_Solo/U000_HAN0202_ENG.wav`.
   - Attack → `Unit_Attack_Han_Solo/U000_HAN0308_ENG.wav`.
   - Invulnerability activation → `Unit_Invulnerable_Han_Solo/U000_HAN0604_ENG.wav`.
   - Optional alternate attack response: Chewbacca’s battle cry in `Unit_Attack_Han_Solo/U000_HAN0304_ENG.wav`. Han and Chewie share the Falcon’s dialogue bank.
   - Verify one cue per local order or successful ability activation. Select/move/attack/ability clips cover separate triggers from the pending production `Ready` line. Include `Audio` in verification.
7. [x] **Validate in the stationary simulator.** Live flight and ability-on acceptance → [[TODOs/User_Acceptance_Queue]]. Original protocol: First check flight: overshoot and time to turn back onto a target at 225°/s and 116 u/s (roster max 155°/s, 92 u/s). Fix break/extend geometry before balance work. Then run the planned matchup protocol against A-wing and X-wing. Check queued, held and deployed purchase limits, cancellation, death and repurchase, one health target, independent owner limits, save/load and fresh pooled deployment. Record measured results and final tuning.

## Implementation evidence

- Unity imported the vanilla Falcon with two renderers, 1,052 triangles, original hull texture, engine material and original 50 × 45 Han/Chewbacca portrait. Centered bounds: 6.188 × 1.860 × 8 units. Four source muzzle sockets locate dorsal and ventral guns.
- Squadron ID 302. One fighter shared by flight and health, one radius-4 collider, one selection ring and one placement hologram. Data/view address keys: `HanSoloMillenniumFalconSquadronData` and `HanSoloMillenniumFalconSquadronView`. Preview address: `HanSoloMillenniumFalconReinforcementView`.
- Two full-traverse quad laser mounts use weapon 123. Each delivers 2 × 84.375 raw damage per 0.9 s. Baked loadout: one member, two guns, 375 raw DPS. Break / extend: 8 / 45 units. Ideal 180° combat turn: 0.8 s, radius 29.54 units.
- Source verification: shop and AI use the Rebellion roster, player reservation uses `UnitTypeId.Squadron`, AI reservation uses per-owner `MaxCount`. No hangar row includes the Falcon. Hero UI takes the squadron portrait.
- Squadron saves now validate against `SquadronData.Abilities`. Active Invulnerability saves as recovery for remaining active time + 60 s. Stored recovery resumes, and pooled squadron slots clear on respawn. Added a save-validator regression case.
- Audio import and prefab bindings: `EGL_FALCON_1`, Ready `HAN0601`, select `HAN0102`, move `HAN0202`, attack `HAN0308` / Chewbacca `HAN0304`, activation `HAN0604`; standard roster explosion profile.

### Stationary simulator, 2026-11-06

- Live asset snapshots, seeds 1–10, alternating sides, range 100, full arcs, no specials, research or support. Mean remaining HP includes shields. Shots/hits are Falcon totals over ten runs.

| Opponent | Falcon wins | Mean fight (s) | Mean remaining HP, Falcon / enemy | Shots / hits |
|---|---:|---:|---:|---:|
| 1 × A-wing | 10/10 | 8.99 | 771 / 0 | 390 / 314 |
| 2 × A-wing | 0/10 | 5.36 | 0 / 1,295 | 231 / 186 |
| 1 × X-wing | 10/10 | 5.74 | 1,118 / 0 | 247 / 200 |
| 2 × X-wing | 0/10 | 7.92 | 0 / 339 | 350 / 280 |

- Kept first-pass values. Flight, specials-on balance, AI rebuy frequency and end-to-end checks remain in [[TODOs/User_Acceptance_Queue]]. Tests were not run under the checkout override.
- Reusable import/save check → [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].
