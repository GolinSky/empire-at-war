---
type: reference
updated: 2026-10-14
tags:
  - alo
  - empire
  - ship-import
---
# Galactic Empire Fleet — Ship Balance & ALO Mapping

- Import plan: [[TODOs/Features/Empire_Fleet_Import]].
- Scope: Procursator, Allegiance, Titan and Tartan for the Galactic Empire faction.
- Evidence: user-supplied proposal recorded 2026-10-14; model paths, source claims and current Unity balance have not been independently verified.

## General Rules

- Faction: Galactic Empire
- Source mod: Empire at War Remake 4.0
- Workshop ID: `1770851727`
- Venator reference length: 1,137 meters
- Venator reference size: 1.0x
- Speed: Unity game units per second
- Price: Base credits
- Build time: Seconds
- Stats: Proposed project balance
- Weapon counts: Individual weapon mounts, not barrels
- Subsystems: Targetable gameplay hardpoints

**Important:** Weapon configurations are proposed for the Unity project. They are inspired by lore and Remake, but have not been verified against individual ALO hardpoint transforms.

## Size Reference

- Venator: 1,137 m — 1.00x
- Procursator: 1,200 m — 1.06x
- Allegiance: 2,200 m — 1.94x
- Titan: approximately 4,500 m — 3.96x (provisional)
- Tartan: 250 m — 0.22x

These are target relative ship lengths, not guaranteed Unity `transform.localScale` values. Imported ALO meshes may use different native scales.

---

## 1. Procursator-class Star Destroyer

**Class:** Capital Ship / Destroyer  
**Role:** Anti-frigate, capital-ship support and long-range bombardment

### ALO Mapping

- Unit: Procursator
- ALO: `Empire_Procursator.ALO`
- Build Menu: `Empire_Capital_Shipyard`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Procursator.ALO`

### Size

- Length: 1,200 meters
- Venator Size Modifier: 1.06x
- Size Source: Star Wars Legends

### Stats

- Hull: 6,800
- Shields: 4,200
- Speed: 19
- Price: 5,500
- Build Time: 55s

### Weapon Hardpoints

- 3x Quad Heavy Turbolaser
- 2x Medium Dual Turbolaser
- 8x Light Triple Turbolaser
- 2x Medium Ion Cannon

### Subsystem Hardpoints

- 1x Engine
- 1x Shield Generator
- 0x Hangar

### Squadron Capacity

- Active: 0
- Reserve: 0

### Abilities

**Overcharge Primary Battery**
- Heavy turbolaser firing rate: +30%
- Duration: 8s
- Cooldown: 40s

### Balance Notes

- Dedicated ship-to-ship combat destroyer.
- Strong against frigates and cruisers.
- Three main axial batteries should offer strong frontal firepower.
- Ion cannons provide shield-breaking capability.
- No fighter complement.
- Poor independent anti-squadron defense.
- More durable than Venator, but less versatile.
- Requires escort ships against bomber attacks.

### Weapon Placement

- 3x Quad Heavy Turbolaser: Dorsal centerline.
- 2x Medium Dual Turbolaser: Port and starboard.
- 8x Light Triple Turbolaser: Dorsal and side batteries.
- 2x Medium Ion Cannon: Forward-facing sections.

Placement must be checked against the imported model.

---

## 2. Allegiance-class Battlecruiser

**Class:** Heavy Capital / Battlecruiser  
**Role:** Anti-capital combat and heavy fleet command

### ALO Mapping

- Unit: `Allegiance_Star_Destroyer_MP`
- ALO: `Empire_Allegiance_Star_Destroyer_Hangar.alo`
- Build Menu: `Empire_Capital_Shipyard`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Allegiance_Star_Destroyer_Hangar.alo`

### Size

- Length: 2,200 meters
- Venator Size Modifier: 1.94x
- Size Source: Star Wars Legends

### Stats

- Hull: 20,000
- Shields: 16,000
- Speed: 12
- Price: 9,200
- Build Time: 85s

### Weapon Hardpoints

- 3x Dual Heavy Turbolaser — Axial batteries
- 6x Quad Heavy Turbolaser — Broadside batteries
- 6x Medium Dual Turbolaser — Side batteries
- 6x Medium Ion Cannon — Trench batteries

### Subsystem Hardpoints

- 3x Engine
- 2x Shield Generator
- 1x Tractor Beam
- 1x Command Array
- 0x Hangar — Default lore configuration

### Squadron Capacity

- Active: 0
- Reserve: 0

### Abilities

**Concentrated Fire**
- Weapon damage against cruisers and larger ships: +25%
- Duration: 10s
- Cooldown: 45s

**Tractor Beam**
- Target: Corvette or frigate
- Target movement speed: -70%
- Duration: 6s
- Cooldown: 35s

### Balance Notes

- Heavy ship-to-ship battlecruiser.
- Strong against cruisers and capital ships.
- Excellent concentrated damage against large targets.
- Very limited effectiveness against fighter swarms.
- Low maneuverability.
- Requires dedicated anti-squadron escorts.
- More durable and heavily armed than regular destroyers.
- Significantly more expensive than the Victory II.

### Weapon Placement

- 3x Dual Heavy Turbolaser: Dorsal centerline.
- 6x Quad Heavy Turbolaser: 3 port / 3 starboard.
- 6x Medium Dual Turbolaser: 3 port / 3 starboard.
- 6x Medium Ion Cannon: Along trench sections.

These groupings are adapted from the documented axial, side and trench battery arrangement. The precise weapon types are gameplay choices.

### ALO Import Warning

The selected model filename contains `Hangar`.

However, the Legends Allegiance-class battlecruiser has no internal hangar bays or regular fighter complement.

- Default gameplay: No hangar.
- Inspect the ALO mesh for hangar geometry.
- If this specific model includes a functional hangar, treat it as an optional mod variant.

---

## 3. Titan-class Battlecruiser

**Class:** Heavy Battlecruiser  
**Role:** Stealth assault, flanking and heavy capital combat

### ALO Mapping

- Unit: `Titan_Star_Destroyer_MP`
- ALO: `Empire_Titan_Star_Destroyer.ALO`
- Build Menu: `Empire_Capital_Shipyard`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Titan_Star_Destroyer.ALO`

### Size

- Estimated Length: 4,500 meters
- Provisional Venator Size Modifier: 3.96x
- Source: Secondary reference to the Remake model
- Verification Required: Yes

This is not a confirmed official Legends length or verified ALO measurement.

### Stats

- Hull: 28,000
- Shields: 18,000
- Speed: 16
- Price: 13,000
- Build Time: 100s
- Suggested Build Limit: 1

### Weapon Hardpoints

- 12x Quad Heavy Turbolaser
- 6x Medium Dual Turbolaser
- 4x Heavy Ion Cannon
- 2x Point Defense Laser

### Subsystem Hardpoints

- 4x Engine
- 3x Shield Generator
- 1x Tractor Beam
- 1x Sensor Array
- 0x Hangar (provisional gameplay choice)

### Squadron Capacity

- Active: 0
- Reserve: 0

### Abilities

**Cloaking Device**
- Ship becomes invisible to enemy sensors.
- Cannot fire weapons while cloaked.
- Attacking automatically disables cloak.
- Cannot activate while taking direct damage.
- Maximum duration: 15s
- Cooldown: 60s

**Tractor Beam**
- Target: Corvette or frigate
- Movement speed: -70%
- Duration: 6s
- Cooldown: 40s

### Balance Notes

- Very large battlecruiser designed for ambushes.
- Strong against capital ships and fleet formations.
- Faster than the Allegiance despite its size.
- Cloaking allows repositioning before attacks.
- Poor anti-squadron protection relative to its size.
- High cost prevents early deployment.
- Requires escorts against bomber swarms.
- Should not have the same raw durability as a dedicated dreadnought.

### Weapon Placement

- 12x Quad Heavy Turbolaser: Main dorsal turret positions.
- 6x Medium Dual Turbolaser: Secondary broadside mounts.
- 4x Heavy Ion Cannon: Heavy support batteries.
- 2x Point Defense Laser: Defensive coverage near the superstructure.

### Mod Accuracy Notes

- The stealth role is based on the Titan from Empire at War Remake.
- A separate Remake 5.0 Titan Refit submod uses 12 quad turbolaser mounts.
- That refit is not proof that all 12 are functional in the supplied Remake 4.0 ALO.
- Confirm the available turret locations after importing the model.
- The proposed configuration intentionally retains weak anti-fighter defenses.

---

## 4. Tartan-class Patrol Cruiser

**Class:** Corvette  
**Role:** Anti-squadron escort and point defense

### ALO Mapping

- Unit: `Tartan_Patrol_Cruiser`
- ALO: `Empire_Tartan.alo`

**Source Path:**

`D:\SteamLibrary\steamapps\workshop\content\32470\1770851727\Data\Art\Models\Empire_Tartan.alo`

### Build Menu

- `Skirmish_Empire_Star_Base_1`
- `Skirmish_Empire_Star_Base_2`
- `Skirmish_Empire_Star_Base_3`
- `Skirmish_Empire_Star_Base_4`
- `Skirmish_Empire_Star_Base_5`

### Size

- Length: 250 meters
- Venator Size Modifier: 0.22x
- Size Source: Star Wars Legends

### Stats

- Hull: 650
- Shields: 850
- Speed: 70
- Price: 1,450
- Build Time: 28s

### Weapon Hardpoints

- 4x Large Repeating Laser Cannon
- 1x Heavy Laser Cannon
- 4x Point Defense Laser

### Subsystem Hardpoints

- No targetable engine hardpoint
- No targetable shield-generator hardpoint
- No hangar

### Squadron Capacity

- Active: 0
- Reserve: 0

### Abilities

**Laser Defense**
- Activates additional anti-projectile interception.
- Missile interception effectiveness: +60%
- Duration: 8s
- Cooldown: 35s

Point-defense weapons remain operational when the ability is inactive.

### Balance Notes

- Primary Imperial anti-squadron corvette.
- Particularly effective against bomber formations.
- Effective against fighters and interceptors.
- Can engage other corvettes.
- Low effectiveness against frigates.
- Extremely vulnerable to medium and heavy turbolasers.
- More anti-fighter specialization than the CR90.
- Slightly slower and more expensive than the CR90.

### Weapon Placement

- 4x Large Repeating Laser Cannon: Distributed around the hull.
- 1x Heavy Laser Cannon: Main forward battery.
- 4x Point Defense Laser: Forward, port, starboard and rear coverage.

Placement must be adjusted to available model geometry.

---

## Imperial Point Defense Rules

### Corvettes

- Dedicated anti-squadron weapons.
- Very high tracking speed.
- High accuracy against small targets.
- Short engagement range.
- Low damage against frigate and capital hulls.

### Tartan vs CR90

**Tartan**

- Hull: 650
- Shields: 850
- Speed: 70
- Price: 1,450
- Stronger dedicated anti-squadron capability.

**CR90**

- Hull: 500
- Shields: 800
- Speed: 80
- Price: 1,250
- More mobile general-purpose corvette.

### Target Priorities

**Point Defense Laser**
- Bombers
- Fighters
- Interceptors

**Repeating Laser Cannon**
- Squadrons
- Gunships
- Corvettes

**Medium Turbolaser**
- Frigates
- Cruisers
- Capitals

**Heavy Turbolaser**
- Capitals
- Heavy Capitals
- Cruisers

**Heavy Ion Cannon**
- Shielded capitals
- Shield generators
- Large enemy ships

---

## Imperial Fleet Progression

### Early Game

**Tartan Patrol Cruiser**
- Anti-fighter screening.
- Escort and missile defense.
- Cheap fleet support.

### Mid Game

**Procursator**
- Long-range combat support.
- Anti-frigate and anti-cruiser weapons.
- Minimal fighter support.

### Late Game

**Allegiance**
- Heavy capital combat.
- Excellent direct firepower.
- No regular fighter complement.

### Advanced Battlecruiser

**Titan**
- Stealth attack.
- Capital-ship ambush.
- High build cost.
- Suggested limited availability.

---

## Balance Reference

### Supplied Unity Ship Comparison

- Snapshot from the pasted proposal; not a live asset audit. Preserve these numbers as comparison inputs until checked against current assets.

**CR90 Corvette**
- Hull: 500
- Shields: 800
- Speed: 80
- Price: 1,250
- Build Time: 25s

**Venator**
- Hull: 5,000
- Shields: 3,000
- Speed: 18
- Price: 5,800
- Build Time: 55s

**Victory II**
- Hull: 4,000
- Shields: 4,000
- Speed: 20
- Price: 4,000
- Build Time: 50s

**MC80 Star Cruiser**
- Hull: 8,500
- Shields: 2,500
- Speed: 15
- Price: 4,500
- Build Time: 30s

**MC80 Independence**
- Hull: 30,000
- Shields: 40,000
- Speed: 17
- Price: 7,000
- Build Time: 68s

### Balance Considerations

- Procursator costs less than Venator because it lacks the Venator's carrier capabilities, despite stronger conventional durability.
- Allegiance should defeat ordinary cruisers through heavier batteries and concentrated fire.
- Titan should gain tactical value from stealth and flanking, rather than simply being the toughest available ship.
- Tartan should defeat bombers much more efficiently than frigates can, but lose sustained direct combat against a Nebulon-B.
- Existing MC80 Independence remains an exceptional balance outlier and should be treated as a flagship unless its stats are revised.
- Weapon DPS, range, accuracy and tracking still require separate balancing. Mount count alone does not determine damage.

---

### TODO

- Follow [[TODOs/Features/Empire_Fleet_Import]] for the import checklist.
---

## Sources

- [Empire at War Remake 4.0](https://steamcommunity.com/sharedfiles/filedetails/?id=1770851727)
- [Procursator — Wookieepedia](https://starwars.fandom.com/wiki/Procursator-class_Star_Destroyer)
- [Procursator — Remake Wiki](https://empire-at-war-remake.fandom.com/wiki/Procursator-class_Star_Destroyer)
- [Allegiance — Wookieepedia](https://starwars.fandom.com/wiki/Allegiance-class_battlecruiser)
- [Tartan — Wookieepedia](https://starwars.fandom.com/wiki/Tartan-class_patrol_cruiser)
- [Tartan — Remake Wiki](https://empire-at-war-remake.fandom.com/wiki/Tartan_Patrol_Gunship_Squadron)
- [Titan — Remake 4.0 Community Guide](https://steamcommunity.com/sharedfiles/filedetails/?id=2780215273)
- [Titan — Remake 5.0 Refit](https://steamcommunity.com/sharedfiles/filedetails/?id=3395375982)
- [Titan — Project Stardust Wiki](https://project-stardust.fandom.com/wiki/Titan)

The Remake mod's Workshop page also restricts reuse of its assets in other mods or games without the creators' permission. Verify usage rights before distributing converted models.

