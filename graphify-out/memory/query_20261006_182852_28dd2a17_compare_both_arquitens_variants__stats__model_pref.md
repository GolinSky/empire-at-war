---
type: "query"
date: "2026-10-06T18:28:52.717650+00:00"
question: "Compare both Arquitens variants: stats, model/prefab size, hardpoints and abilities"
contributor: "graphify"
outcome: "useful"
source_nodes: ["Arquitens", "ArquitensImperialCruiser", "ShipAbilityId"]
---

# Q: Compare both Arquitens variants: stats, model/prefab size, hardpoints and abilities

## Answer

Graph vocabulary used: Arquitens, ArquitensImperialCruiser, Ship, Abilities, HardPoints. Live ShipData assets and Unity prefab inspection verified Republic hull/shields/speed/range 2900/1400/54/500; Imperial 1300/1200/30/562.5. Republic regenerates 42 per 3 seconds, Imperial 4 per 1 second. Republic has 9 weapons plus engine and shield targetable hardpoints; Imperial has 8 weapons and an empty health hardpoint list. Republic weapon types: 4 DualMediumTurboLaser, 4 DualLaser, 1 ProtonTorpedo. Imperial: 4 LightLongRangeTurbolaser, 4 BurstLaserCannon. Republic BoostShieldPower lasts 15 seconds with 40 seconds recovery; Imperial ArquitensBoostWeaponPower lasts 20 seconds with 60 seconds recovery. Unity solid prefab bounds W/H/L: Republic 14.80897/6.18353/33.44712; Imperial 17.44437/8.42795/39.99748. Imperial collider 29.5792/15.7410/40 includes lens planes. WeaponComponent initializes all mount firing ranges from ship data rather than profile ranges. Weapon boost halves reload timer but does not scale normal burst shot spacing. No project source or serialized asset changes were made.

## Outcome

- Signal: useful

## Source Nodes

- Arquitens
- ArquitensImperialCruiser
- ShipAbilityId