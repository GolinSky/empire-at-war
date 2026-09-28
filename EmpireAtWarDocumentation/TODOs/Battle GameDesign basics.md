# Battle Combat Design

- Reference: *Star Wars: Empire at War*, Petroglyph’s Alamo Engine, *Republic at War* (RaW).
- Combat layers: scalar multiplier matrix → tracking/accuracy filters → hierarchical XML hardpoints.

### 1. The Game Design Triangle

- **Fighters (Interceptors)** shred Bombers via high speed and anti-light weapon tracking.

- **Corvettes / Pickets** screen Fleets and kill Fighters with high tracking, rapid-fire point defense.

- **Frigates / Escorts** hunt Corvettes and screen Capitals using medium turbolasers and concussion missiles.

- **Capital Ships** shred Frigates and opposing Capitals using heavy turbolasers and massive shield/hull pools.

- **Bombers** bypass or smash Capital defenses using shield-piercing ordnance and heavy payload torpedoes.

- _RaW divergence:_ RaW tightens these counters into hard cliffs.
- If a player fields unescorted capital ships (e.g., Venators or Providences), a single swarm of ARC-170s or Hyena Bombers will strip their hardpoints in seconds due to extreme damage multipliers.

### 2. Technical Code Architecture (Alamo Engine)

- Engine XML resolves combat across four systems.

### A. Damage-to-Armor Multiplier Matrix

- Damage lookup: `Damage_Type` × `Armor_Type` → scaled base damage.
- No runtime armor-physics simulation.

- Projectile → `Damage_Type`.
- Ship/hardpoint → `Armor_Type`.
- Matrix file: `DamageTypes.xml`.

```
<!-- Example Damage-to-Armor mapping logic in Alamo XML -->
<DamageType Name="Damage_Capital_Turbolaser">
    <Damage_To_Armor_Mod Name="Armor_Fighter"> 0.05 </Damage_To_Armor_Mod>
    <Damage_To_Armor_Mod Name="Armor_Corvette"> 0.50 </Damage_To_Armor_Mod>
    <Damage_To_Armor_Mod Name="Armor_Frigate">  1.00 </Damage_To_Armor_Mod>
    <Damage_To_Armor_Mod Name="Armor_Capital">  1.50 </Damage_To_Armor_Mod>
    <Damage_To_Armor_Mod Name="Armor_Shield">   1.00 </Damage_To_Armor_Mod>
</DamageType>
```

- **Anti-Fighter Laser:** 2.0x vs `Armor_Fighter`, 0.05x vs `Armor_Capital`.

- **Proton Torpedo:** 2.5x vs `Armor_Capital`, 0.1x vs `Armor_Fighter`.

### B. Ballistic & Tracking Gating

- Heavy turbolaser may deal 1% damage to fighters; tracking can still prevent a hit.

- `**Max_Rate_Of_Turn**` **/** `**Turret_Rotate_Speed**`**:** Turbolaser bones rotate slowly.
  - High-speed craft exceed the turret’s angular tracking limit.

- `**Fire_Inaccuracy_Distance**` **&** `**Fire_Variance**`**:** Hard-coded projectile spread.
  - Small collision radii (fighters) evade almost all volume fire.

- `**Projectile_Speed**` **vs Target Speed:** Slow heavy bolts allow high-maneuverability objects to clear the hit volume before projectile arrival.

### C. Shield Routing & Penetration

- `Projectiles.xml` tags route damage to shield/hull HP pools.

- `**Shield_Piercing**` **(True/False):** Concussion missiles and proton torpedoes ignore the global shield pool and strike local hardpoints directly.

- `**Energy_Defense_Mod**` **/** `**Shield_Damage_Multiplier**`**:** Ion cannons apply 3.0x+ damage to shields and 0.0x to physical hull hardpoints.

### D. Composite Hardpoint Hierarchy

- Capital ship → composite `HardPoint` entries in `HardPoints.xml`.

```
<HardPoint Name="HP_Venator_Heavy_Turbolaser_01">
    <Type> HARD_POINT_WEAPON_TURBOLASER </Type>
    <Health> 800 </Health>
    <Armor_Type> Armor_Capital </Armor_Type>
    <Fire_Bone_A> Turret_Bone_01 </Fire_Bone_A>
</HardPoint>
```

- When bombers focus-fire engines or shield generator hardpoints, localized HP reaching 0 triggers socket-level disabling: shields drop permanently for the parent ship, or max turn rate drops to near zero without destroying the entire entity.
