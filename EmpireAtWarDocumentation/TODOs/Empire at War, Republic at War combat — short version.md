### Ship classes

The important combat classes are roughly:

- **Fighter / Interceptor** → kills bombers and other fighters.
- **Bomber** → attacks large ships and hardpoints.
- **Corvette / Picket** → anti-fighter / anti-bomber screen.
- **Frigate** → fights corvettes and lighter warships.
- **Capital Ship** → heavy ship-to-ship combat.
- **Super Capital** → same idea, but its own armor/category rules.

The engine explicitly has Fighter, Bomber, Corvette, Frigate, Capital and Super target categories.

### Simplified rock-paper-scissors

Think of it like:

**Fighter → Bomber → Capital → Frigate → Corvette → Fighter**

But it is **not a strict circle**.

For example:

- Fighters protect capitals from bombers.
- Corvettes destroy fighters/bombers.
- Frigates punish corvettes.
- Capitals punish frigates and other large ships.
- Bombers bypass the normal capital-vs-capital fight and attack important hardpoints.

Individual ships can break these rules because their weapons have different damage types, accuracy and fire rates.

---

## Main weapon types

**Laser Cannon**

- Fast / accurate.
- Usually anti-fighter and anti-bomber.
- Weak against large armored ships.

**Turbolaser**

- Heavy ship weapon.
- Strong against frigates/capitals.
- Very inaccurate against fighters.

EaW can literally define different `Fire_Inaccuracy_Distance` values for Fighter, Corvette, Frigate, Capital, etc. A heavy weapon may therefore be accurate against a frigate but almost useless against a fighter.

**Ion Cannon**

- Primarily attacks shields/energy.
- Used to help disable or expose large ships.
- Exact shield/hull multipliers depend on the mod data.

**Proton Torpedo**

- Heavy bomber/ship ordnance.
- Excellent for attacking capital ships and hardpoints.
- In classic EaW/FoC mechanics, proton torpedoes can bypass shields.

**Concussion Missile**

- Missile weapon with behavior depending on projectile/configuration.
- Often useful against ships or lighter craft depending on the weapon.

**Flak / point-defense weapons**

- Designed primarily for fighter/bomber suppression.

RaW's source contains damage types for fighters, ion cannons, proton torpedoes, concussion missiles, turbolasers, flak and others.

---

## How damage basically works

The core idea you described is correct:

```
Final Damage
≈ Weapon Base Damage
× DamageType-vs-ArmorType modifier
```

A weapon has something such as:

```
Damage_Proton_Torp
Damage_Turbolaser
Damage_Fighter
Damage_IonCannon
```

A target has:

```
Armor_Type
Shield_Armor_Type
```

And the engine supports a `Damage_To_Armor_Mod` matrix.

So:

```
Turbolaser
    vs Fighter      → low modifier
    vs Frigate      → high modifier

Anti-fighter laser
    vs Fighter      → high modifier
    vs Capital      → low modifier
```

That multiplier matrix is the **main RPS balancing layer**.

---

## Accuracy is the second balancing layer

Damage alone is not enough.

A weapon may theoretically hurt a fighter but almost never hit it.

Example:

```
Heavy Turbolaser
Fighter  → very inaccurate
Corvette → medium
Frigate  → accurate
Capital  → accurate
```

This is configured through things like:

```
Fire_Inaccuracy_Distance
Turret_Rotate_Speed
Fire_Cone
Projectile behavior
```

So you get:

**Damage effectiveness × ability to hit target.**

---

## Hardpoints

Large ships are effectively collections of subsystems:

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

Each hardpoint can have its own HP and be destroyed separately.

Destroying them removes capabilities:

- **weapon** → gun stops working
- **engine** → ship becomes much slower; RaW's source uses an engine-disabled speed multiplier of `0.4`
- **shield generator** → shield system is lost/disabled
- **hangar** → fighter capability can be affected

RaW also links hull and hardpoint health through `Hull_Vs_Hard_Points_Health_Constraint`.

---

## What I would copy for your Unity clone

The clean model is:

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

Then combat becomes:

```
1. Can weapon target this ship class?
2. Accuracy / projectile determines hit.
3. Determine shield or hull/hardpoint target.
4. BaseDamage × ArmorModifier.
5. Apply damage.
6. Destroyed hardpoint disables subsystem.
```