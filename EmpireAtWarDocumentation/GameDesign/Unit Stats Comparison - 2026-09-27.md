---
tags:
  - gamedesign
  - units
  - balance
  - analysis
date: 2026-09-27
status: review
---
# Unit Stats Comparison — 2026-09-27

## Assessment

Four recognizable shared ships match the attached Republic at War reference exactly in base hull and shields: Arquitens, Acclamator, Heavy Dreadnought (mapped to the reference Dreadnaught), and Venator. Their weapons and abilities differ, sometimes enough to change their tactical role. Matching durability does not mean matching balance.

The current roster contains 11 ShipType entries and two SquadronType entries. The reference describes a much wider Republic roster, including bombers, escorts, heroes and relay units. This report compares saved project assets and live source, not a running battle or externally verified RaW version.

Reference: [[Republic at War - Republic Unit Stats Reference]]. Existing advisory reference: [[Unit description]]. The attachment has no source URLs, numeric weapon damage/reload, costs, population, or numeric speed scale. Its external claims are treated as supplied reference material.

## Base durability: full current ship roster

HP in the attachment is compared with our Hull. “Total” is simply Hull + Shields, not effective health; damage multipliers, shield piercing, regeneration and subsystem losses change real survivability. Speed is our serialized project value and cannot be converted to the attachment's qualitative labels.

| Current asset/unit | Hull | Shields | Total | Speed | Attachment comparison |
|---|---:|---:|---:|---:|---|
| Thranta | 1,250 | 900 | 2,150 | 6 | No same-name counterpart |
| Arquitens | 2,900 | 1,400 | 4,300 | 4.5 | Exact match |
| Acclamator | 4,700 | 2,000 | 6,700 | 3 | Exact match |
| HeavyDreadnought | 3,600 | 2,500 | 6,100 | 1.5 | Exact match to Dreadnaught, assuming intended identity |
| Venator | 5,000 | 3,000 | 8,000 | 1.5 | Exact match |
| StarDestroyer1 | 4,900 | 3,100 | 8,000 | 1.2 | Numerically identical to Victory; identity not confirmed |
| StarDestroyer2 | 5,200 | 3,300 | 8,500 | 1.2 | Identity not confirmed; see conditional comparison |
| Munificent | 2,500 | 1,200 | 3,700 | 4.5 | Not in the supplied Republic attachment |
| Recusant | 3,800 | 2,600 | 6,400 | 2.25 | Not in the supplied Republic attachment |
| Providence | 5,000 | 3,000 | 8,000 | 1.5 | Not in the supplied Republic attachment |
| Lucrehulk | 12,000 | 6,600 | 18,600 | 1.05 | Not in the supplied Republic attachment |

The existing Unit description note separately matches our Munificent, Recusant and Providence hull/shields, but remains advisory rather than proof of RaW's current values.

If StarDestroyer2 is intended to represent the attachment's Imperator (7,500 / 3,500), it is 30.7% lower in hull, 5.7% lower in shields and 22.7% lower in combined raw durability. Do not treat this as a confirmed discrepancy until its intended class is established. Its present total is only 6.25% above StarDestroyer1, with the same speed and weapon-type counts.

Within our fleet, Venator and Providence share hull, shields, speed, shield regeneration settings and hangar limits. Lucrehulk has 2.325 times their raw durability. Those similarities/differences can support faction balance, but weapons, squadron composition and economy must also be evaluated.

## Fighters: a different durability scale

| Current squadron | Hull/member | Shield/member | Raw total/member | Members | Raw squadron total | Cruise / combat speed |
|---|---:|---:|---:|---:|---:|---|
| Delta-7 | 160 | 50 | 210 | 5 | 1,050 | 26 / 30 |
| Belbullab-22 | 220 | 70 | 290 | 4 | 1,160 | 23 / 26 |

The attachment's ordinary fighters/bombers span 60–105 hull, 20–35 shields and 80–140 combined raw durability. Neither current fighter has a direct ordinary-unit match in the attachment.

- Delta-7 is 1.5 times an ARC-170's listed raw durability and 2.625 times an A-6's.
- Belbullab-22 is 2.071 times an ARC-170's and 3.625 times an A-6's.
- Against each other, Belbullab has 38.1% more raw durability per member but only 10.5% more per squadron. Delta-7 has five firing members rather than four, and is faster.
- Both squadron prefabs use FighterLaser. Its shared profile is 5 damage, 2 shots/salvo, 0.08 shot interval, 0.9 reload and 70 range. This is a profile comparison, not a measured DPS result.
- Higher durability alone does not establish that our fighters are overpowered: target-class multipliers, accuracy, firing opportunities and price matter.
- Hero fighter values in the attachment are a separate category and should not set ordinary-fighter baselines.

No bomber entry exists in the inspected SquadronType roster. The attachment's torpedo/ion bomber roles and fighter abilities therefore cannot be reproduced by merely changing these two squadron health assets.

## Weapons: significant role differences

These are weapon-type assignments counted in the saved ship prefabs, including nested-prefab overrides, decoded against the live WeaponType enum. Counts denote configured mounts, not barrels, simultaneous firing arcs, measured DPS or verified runtime performance.

| Ship | Current prefab weapon assignments | Attachment |
|---|---|---|
| Arquitens | 4 DualMediumTurboLaser, 4 DualLaser, 1 LaserBeam | 4 quad turbolaser, 4 laser, 1 concussion missile |
| Acclamator | 4 MediumTurboLaser, 2 Laser, 1 ProtonTorpedo | 3 quad turbolaser, 2 point defense, 1 proton torpedo |
| HeavyDreadnought | 2 HeavyTurboLaser, 2 DualHeavyTurboLaser, 2 HeavyIonCannon | 4 dual turbolaser, 8 laser |
| Venator | 10 DualHeavyTurboLaserDby827, 2 HeavyTurboLaser, 2 HeavyIonCannon, 1 LaserBeam | 8 heavy turbolaser, 2 medium turbolaser, 4 laser, 1 proton torpedo; SPHA normally inoperable |
| StarDestroyer1 | 8 DualHeavyTurboLaserDby827, 2 HeavyTurboLaser, 2 DualHeavyTurboLaser, 2 HeavyIonCannon | If Victory: missiles, mixed turbolasers, lasers and torpedo |
| StarDestroyer2 | Same weapon-type counts as StarDestroyer1 | If Imperator: turbolaser/ion mix with tractor beam |

The most consequential differences are Venator's missing laser/torpedo assignments, Dreadnought's missing laser screen, Arquitens' beam replacing the missile slot, and Acclamator's lasers replacing dedicated point defense. Our weapon enum has no explicit quad-turbolaser type; names alone cannot establish equivalent firepower.

The DamageMatrixData asset distinguishes anti-fighter and anti-capital damage/accuracy and shield interactions. For example, laser damage has a fighter multiplier of 1.5 and capital multiplier of 0.25, while the heavy-turbolaser damage category has fighter multiplier 0.1 and capital multiplier 1.25. Thus replacing lasers with heavy turbolasers changes intended targets even when hull/shields remain identical.

Our Dreadnought prefab also includes a ShieldGenerator hardpoint in its health list, whereas the attachment describes a non-targetable shield generator. Runtime targetability was not exercised, so this is a configuration difference to review rather than a tested targeting result.

## Abilities and carrier complement

| Ship | Current assigned abilities | Attachment |
|---|---|---|
| Arquitens | BoostShieldPower | Broadly aligned with shield boost |
| Acclamator | BoostWeaponPower, BoostShieldPower | Broadly aligned at the label/role level |
| HeavyDreadnought | Assault | Point Defense — different ability |
| Venator | BoostShieldPower, BoostWeaponPower | Weapon power / Fire All Batteries; current shield boost is additional |
| StarDestroyer1 | None | Victory has Power to Weapons, if this mapping is intended |
| StarDestroyer2 | ProtonBeam | Imperator has Tractor Beam, if this mapping is intended |

Ability names are not proof of equal duration, cooldown or effect strength.

| Carrier | Squadron | Initial reserve | Max active |
|---|---|---:|---:|
| Acclamator | Delta-7 | 4 | 1 |
| Venator | Delta-7 | 6 | 2 |
| Providence | Belbullab-22 | 6 | 2 |
| Lucrehulk | Belbullab-22 | 8 | 3 |

HangarModel consumes one reserve for every launch. Reserve and maximum active should not be added as independent supplies. The other inspected ship data assets have no hangar bays.

The attachment gives Venator up to 8 fighter + 5 bomber squadrons in reserve; our configured reserve is 6 fighter squadrons and no bombers. That is 25% fewer fighter reserves and 53.8% fewer total listed reserve squadrons, with different squadron types and potentially different external reserve semantics. This removes much of the reference carrier's anti-capital strike flexibility.

## Reference roster coverage

Present or plausible matches: Arquitens, Acclamator, Dreadnaught, Venator, and the two unresolved StarDestroyer mappings.

Not registered as distinct current ShipType/SquadronType entries: A-6, V-19, NTB-630, Y-Wing, Clone Z-95, ARC-170, V-Wing, prototype TIE; C-70, C-65, Carrack, Pelta, Centax; Mandator II, Modular Taskforce Cruiser, Gladiator and Rothana. The listed named heroes and elite squadrons are likewise not distinct entries in these rosters. This statement concerns the inspected gameplay rosters, not the presence or absence of art elsewhere.

The current roster has Thranta as a light escort but lacks the attachment's variety of specialist screens and bomber squadrons. A broader reference fleet will require role design as well as numbers.

## Recommended design priorities

1. Resolve the intended classes of StarDestroyer1 and StarDestroyer2 before applying Victory/Imperator targets.
2. Decide whether matching RaW roles is the objective. If yes, weapon loadouts, Dreadnought's defensive ability and Venator's bomber complement deserve attention before adjusting the four already-matching durability pairs.
3. Choose a deliberate ordinary-fighter durability scale. Current fighters are substantially tougher than the supplied range; document whether that compensates for squadron size, weapon balance or battle pacing.
4. Differentiate the two Star Destroyers through intended role, weapons, abilities, carrier capacity and price. Their current weapon-type counts and movement match closely.
5. Build a future balance comparison around cost/population, target-specific DPS, time to kill, fighter losses and carrier attrition. Those data are required before concluding that any unit is overpowered.

These are design recommendations, not applied balance changes.

## Evidence and verification

Primary evidence, relative to F:/Private/empire-at-war:

- Assets/Settings/Data/Ship/*ShipData.asset — hull, shields, speed, assigned abilities and hangar bays.
- Assets/Settings/Data/Ship/ShipsData.asset — ship asset catalog.
- Assets/Settings/Data/Squadron/Delta7SquadronData.asset and Belbullab22SquadronData.asset — per-member health and movement.
- Assets/Prefabs/Models/Ships/*ShipView.prefab — serialized weapon assignments and subsystem configuration.
- Assets/Prefabs/Models/Squadrons/*SquadronView.prefab — fighter lists and weapon assignments.
- Assets/Settings/Data/Models/Weapon/WeaponsData.asset and DamageMatrixData.asset — weapon profiles and damage-class rules.
- Assets/Scripts/Entities/Faction/Model/ShipType.cs; Assets/Scripts/Entities/Squadron/SquadronType.cs — registered unit identities.
- Assets/Scripts/Services/ShipAbilities/ShipAbilityId.cs and Assets/Scripts/Components/AttackComponent/WeaponType.cs — numeric ID meanings.
- Assets/Scripts/Entities/Ship/Data/ShipData.cs — current serialized schema.
- Assets/Scripts/Components/Hangar/HangarModel.cs, TryLaunch — reserve consumption.

Graphify supplied navigation hints; findings were checked against live source and serialized assets. Analysis covers saved base configuration, not scene overrides, research upgrades, active ability modifiers, combat simulation or economic efficiency. No automated tests or gameplay runs were performed, and no gameplay assets were changed.
