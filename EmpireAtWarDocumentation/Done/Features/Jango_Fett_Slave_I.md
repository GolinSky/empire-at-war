---
category: Features
status: done
updated: 2026-11-06
acceptance: pending-user
completed: 2026-11-06
---
# Jango Fett — Slave I

## Goal

- Add Jango Fett — Slave I as a unique single-fighter skirmish hero for CIS (`Separatist`). One purchase deploys one visible, targetable member.
- Durable close-range hunter. Give Jango the largest hull and less shield than Boba. Keep RaW’s hull advantage in direction, but compress the original 3:1 hull gap for skirmish.

## Important Values

| Proposed Unity value | Target |
|---|---:|
| Members / maximum owned | 1 / 1 |
| Member hull / shields | 1000 / 400 HP |
| Cruise / combat | 100 / 110 units/s |
| Acceleration / turn | 54 units/s² / 215°/s |
| Nominal primary gun output | 400 raw DPS |
| Station / credits / build / population | 2 / 1500 / 25 s / 2 |

- 1,400 HP vs Belbullab-22’s 880 (+59%). 110 combat speed vs 66 (+67%). 215°/s vs 100°/s (+115%). 400 raw gun DPS vs Vulture’s 311 (+29%).
- Use the existing CIS faction reinforcement shop at station level 2. RaW’s original Jango comes from the Mercenary Relay. Use the relay’s actual 1,500 credits and 25 s as starting prices.
- Original game stats, reference squadron inputs, weapon calculation and shared duel protocol → [[Done/Features/Skirmish_Hero_Fighters - Research]].

## Implementation

1. [x] **Import.** Vanilla `EV_SLAVE1_SPACE.ALO`, RaW `Ev_slave1.dds` and the vanilla gloss map. Dedicated material and 512×512 icon. Unity imported one visible hull (1,580 triangles), centered bounds 5.777 × 5.915 × 2.667 units, bow +Z.
2. [x] **Build one fighter.** `SquadronType.JangoFettSlaveI = 103`. Flight and health bind the same member. One hit collider, two independently bound muzzle hardpoints, one engine trail. Visual and placement prefab use the same geometry.
3. [x] **Arm and tune.** `WeaponType.JangoFettLaser = 125`. Two guns × 90 damage × two pulses / 0.9 s = 400 raw DPS. Inherited fighter laser damage matrix. Per-gun half arc 12°, range 70. Break/extend 10/60 units. Expected combat turn radius 29.3 units. Baked from the saved prefab: one member, two guns.
4. [x] **Signature ability.** `JangoFettSlaveISquadronData.Abilities` contains `ShipAbilityId.HarmonicBomb` (54). Its squadron prefab binds `JANGO_SHIP_ATTACK_06_ENG` to the same ID for successful activation. Shared fuse, damage, autofire and recharge → [[Done/Features/Harmonic_Bomb_Ability]].
5. [x] **Register.** CIS hero at station level 2. Cost 1,500 credits, build time 25 s, population 2, maximum one. Faction roster, HUD and tooltip share one icon. Data, view and placement keys resolved in Unity. Existing player reservation and AI anti-fighter purchase paths apply. No hangar entries.
6. [x] **Audio.** RaW Slave I engine and corvette explosion bank. Production Ready → `JANGO_RESPAWN_00_ENG`; select → `JANGO_SHIP_SELECT_05_ENG`; move → `JANGO_SHIP_MOVE_09_ENG`; attack → `JANGO_SHIP_ATTACK_05_ENG`. One local dialogue binding. Destruction requests the explosion at the squadron centroid before pool return. Unity resolved the audio registrations.
7. [ ] **In-game acceptance.** Check silhouette, trail, firing passes and overshoot, shot arcs, Belbullab-22/Vulture duels, AI buy frequency, portrait, local dialogue and death audio. Check reservation across queue, cancellation, holding, deployment, death and repurchase on both owners; save/load and pooled respawn. Measurements and final tuning pending → [[Done/Features/Skirmish_Hero_Fighters - Research]].

## TODO

- Unity verified one ability slot, the Harmonic Bomb marker, Jango’s activation clip, target-free casting and 20 s recovery. Editor Console returned zero errors. In-game activation and tests were not run.
- Completed the CIS hero with its shared Harmonic Bomb. In-game checks → [[TODOs/User_Acceptance_Queue]].
- Shared ability behavior and activation audio → [[GameDesign/Abilities_And_Superweapons]]. Jango uses the existing squadron loadout and dialogue binding.
- Unity reloaded and saved the assets, derived one member and two guns, and evaluated 200 raw DPS per gun. Editor Console returned zero errors. Tests were not run under the checkout override.
