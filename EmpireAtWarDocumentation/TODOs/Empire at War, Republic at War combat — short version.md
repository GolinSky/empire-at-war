# Empire at War / Republic at War Combat

## Ship classes

- **Fighter / Interceptor** → kills bombers and other fighters.
- **Bomber** → attacks large ships and hardpoints.
- **Corvette / Picket** → anti-fighter / anti-bomber screen.
- **Frigate** → fights corvettes and lighter warships.
- **Capital Ship** → heavy ship-to-ship combat.
- **Super Capital** → same idea, but its own armor/category rules.

- The engine explicitly has Fighter, Bomber, Corvette, Frigate, Capital and Super target categories.

### Simplified rock-paper-scissors

### Fighter → Bomber → Capital → Frigate → Corvette → Fighter

- Counter cycle is approximate; weapon loadouts can break it.

- Fighters protect capitals from bombers.
- Corvettes destroy fighters/bombers.
- Frigates punish corvettes.
- Capitals punish frigates and other large ships.
- Bombers bypass the normal capital-vs-capital fight and attack important hardpoints.

- Damage type, accuracy, and fire rate can override class counters.

## Main weapon types

### Laser Cannon

- Fast / accurate.
- Usually anti-fighter and anti-bomber.
- Weak against large armored ships.

### Turbolaser

- Heavy ship weapon.
- Strong against frigates/capitals.
- Very inaccurate against fighters.

- `Fire_Inaccuracy_Distance` varies by Fighter, Corvette, Frigate, Capital, etc.
- Heavy weapon → may hit frigates reliably while missing fighters.

### Ion Cannon

- Primarily attacks shields/energy.
- Used to help disable or expose large ships.
- Exact shield/hull multipliers depend on the mod data.

### Proton Torpedo

- Heavy bomber/ship ordnance.
- Excellent for attacking capital ships and hardpoints.
- In classic EaW/FoC mechanics, proton torpedoes can bypass shields.

### Concussion Missile

- Missile weapon with behavior depending on projectile/configuration.
- Often useful against ships or lighter craft depending on the weapon.

### Flak / point-defense weapons

- Designed primarily for fighter/bomber suppression.

- RaW's source contains damage types for fighters, ion cannons, proton torpedoes, concussion missiles, turbolasers, flak and others.

## How damage basically works

```
Final Damage
≈ Weapon Base Damage
× DamageType-vs-ArmorType modifier
```

### Weapon Fields

```
Damage_Proton_Torp
Damage_Turbolaser
Damage_Fighter
Damage_IonCannon
```

### Target Fields

```
Armor_Type
Shield_Armor_Type
```

- Engine supports a `Damage_To_Armor_Mod` matrix.

```
Turbolaser
    vs Fighter      → low modifier
    vs Frigate      → high modifier

Anti-fighter laser
    vs Fighter      → high modifier
    vs Capital      → low modifier
```

- Multiplier matrix → primary RPS balancing layer.

## Accuracy is the second balancing layer

- Potential damage does not establish hit probability.

### Example

```
Heavy Turbolaser
Fighter  → very inaccurate
Corvette → medium
Frigate  → accurate
Capital  → accurate
```

### Accuracy Fields

```
Fire_Inaccuracy_Distance
Turret_Rotate_Speed
Fire_Cone
Projectile behavior
```

### Damage effectiveness × ability to hit target.

## Hardpoints

- Large ships contain independent subsystems:

```
Hull
├─ Shield Generator
├─ Engines
├─ Turbolaser 1
├─ Turbolaser 2
├─ Ion Cannon
├─ Hangar
└─ Missile/Torpedo launcher
```

- Hardpoints have separate HP and destruction.

- **weapon** → gun stops working
- **engine** → ship becomes much slower; RaW's source uses an engine-disabled speed multiplier of `0.4`
- **shield generator** → shield system is lost/disabled
- **hangar** → fighter capability can be affected

- RaW also links hull and hardpoint health through `Hull_Vs_Hard_Points_Health_Constraint`.

## What I would copy for your Unity clone

```
Weapon
 ├ Damage
 ├ DamageType
 ├ FireRate
 ├ AccuracyByTargetClass
 ├ ProjectileSpeed
 └ ShieldBehavior

Ship
 ├ ShipClass
 ├ ArmorType
 ├ ShieldArmorType
 ├ HullHP
 ├ ShieldHP
 └ Hardpoints[]
```

### Resolution Order

```
1. Can weapon target this ship class?
2. Accuracy / projectile determines hit.
3. Determine shield or hull/hardpoint target.
4. BaseDamage × ArmorModifier.
5. Apply damage.
6. Destroyed hardpoint disables subsystem.
```
