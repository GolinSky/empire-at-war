---
category: Features
status: done
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Harmonic Bomb Ability

## Goal

- Shared Slave I signature ability for [[Done/Features/Jango_Fett_Slave_I]] and [[Done/Features/Boba_Fett_Slave_I]].
- Original tuning and source evidence → [[Done/Features/Skirmish_Hero_Fighters - Research]]. Source values were 300 damage and 600 source units.

## Implementation

1. [x] `ShipAbilityId.HarmonicBomb` (54), catalog settings and factory. Both Fett squadron data assets contain ID 54. Manual activation uses the caster’s current world position with no target and no approach order.
2. [x] Battle service snapshots live hostile squadron members at detonation. An XZ radius of 35 includes the boundary. Equal thirds deal 120 / 72 / 30 base damage. Ships, structures and friendly squadrons are immune.
3. [x] Existing `DamageType.HypervelocityGun` profile. Unity read back 1× fighter, bomber and interceptor hull, 2× squadron shields, no piercing. `SquadronHealthModel` absorbs a shielded hit without hull spillover. Laser-only shield mitigation does not apply.
4. [x] Each drop owns its captured player, stationary position and 1.5 s fuse. Caster death or pool return does not cancel it. Before applying hits the service rechecks entity registration and member identity, including after damage callbacks.
5. [x] Pooled marker expands into a 0.35 s ring at detonation. Local area vision controls drawing. Battle disposal and simulation reset clear pending timers and views.
6. [x] AI and player autofire share a threshold of three live, visible hostile members within radius, excluding cloaked squadrons. Player autofire follows the global setting, single-selection suspension and manual hold. Manual drops have no enemy-count requirement.
7. [x] A surviving caster recovers for 20 s after the active slot ends. Saving during the fuse records remaining active time plus 20 s as recovery. Loading discards the pending blast.

8. [x] Original `EXP_SIZEMIC_CHARGE_2.WAV` plays at the captured drop position once per detonating bomb. Both Fett prefabs bind their own line to successful activation. Audio follow-up completed 2026-11-06.

## Audio source

- `PROJECTILES.XML` → `Proj_Harmonic_Bomb_Slave_I.Death_SFXEvent_Start_Die` → `SFX_Sizemic_Detonation`. `SFXEVENTSDEATHS.XML` → `EXP_Sizemic_Charge_2.wav`.
- Source WAV: mono 16-bit PCM, 44,100 Hz, 132,877 samples (3.013 s). Original event predelay is 1000 ms at both limits. Project trigger is fuse expiry; no additional predelay.
- Pilot voices at activation: Jango `JANGO_SHIP_ATTACK_06_ENG`, Boba `U000_BFT1612_ENG`.

## Verification

- Unity compiled the runtime and Editor fixture. Reloaded catalog settings, icon, prefab reference and both saved Fett assignments. Prefab verification covered the serialized line renderer, material and 48-point closed XZ ring.
- `HarmonicBombTests` covers fuse timing, overlapping drops, live positions, falloff, allies and visible-enemy counts. The fixture checks one correct positional cue per bomb, including an empty drop, and no cue before fuse expiry. Test execution disabled by checkout policy.
- The source and imported explosion have identical SHA-256 hashes. Unity read back 132,877 mono samples, 3.013084 s, both pilot clips and both ID 54 slots.
- User-only gates → [[TODOs/User_Acceptance_Queue]]. Reusable rules → [[GameDesign/Abilities_And_Superweapons]].
