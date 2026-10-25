---
type: design-reference
system:
  - combat
  - weapons
  - health
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
---
# Combat

## Goal

- Current weapon, targeting, damage, shield and hardpoint rules.
- Related: [[GameDesign/Units_And_Factions]], [[GameDesign/Abilities_And_Superweapons]], [[GameDesign/Commands_And_Controls]], [[GameDesign/Visibility]].

## Rules

- Weapon identity, weapon family, damage type and target class are separate concepts. Resolve IDs through current enums/catalogs; old import names are not current damage formulas.
- Each installed mount uses its own weapon profile and firing arc. Mount count alone does not establish DPS, coverage, time to kill or matchup outcome.
- Unit movement/radar data does not supply a universal gun range. `WeaponComponent.Initialize` sets each mount's reach from `profile.Range × VisionMultiplier`.
- `DamageMatrixData` owns class damage/accuracy, shield multipliers and shield-piercing flags. Weapon caliber and current modifiers add separate multipliers.

## Implementation

### Weapon taxonomy

- `WeaponDefinition` selects family, caliber, range class, mount and variant multipliers; `WeaponProfileFactory.Create` builds the runtime profile.
- Single-shot damage = family damage × caliber damage × range-class damage × barrel count × definition damage multiplier.
- Range = family range × caliber range × range-class range × definition range multiplier.
- Reload = family reload × range-class reload × definition reload multiplier.
- Every weapon fires the family's `ShotsPerSalvo` at `ShotInterval`; burst fire patterns were removed 2026-10-25.
- Mount enum value supplies barrel count. A multi-barrel mount multiplies damage per shot; it does not automatically multiply the salvo's shot count.
- Squadron-platform weapons have zero turret turn rate because flight provides aiming.

### Firing cycle and comparison

- Reload starts at salvo start. `CycleSeconds = max(reload, (shotsPerSalvo − 1) × shotInterval)`.
- Raw profile DPS = `damage × shotsPerSalvo / CycleSeconds`; this excludes accuracy, armor/shields, arcs, travel, movement, interception and ability effects.
- Effective DPS for engagement-range selection includes class accuracy, accuracy bias, range-class accuracy, class damage and damage bias.
- Engagement distance is the longest reach covering at least `60%` of effective DPS against the target class. No effective damage → use the longest weapon reach.
- This engagement heuristic uses the hull/class matrix; it is not a full shield-aware combat simulator.

### Target selection

- Candidate targets must be eligible and alive; strikecraft-only profiles reject non-strikecraft.
- Preferred target category is considered first; other eligible targets are considered when the preferred category cannot be reached.
- Range uses planar X/Z distance, so height tiers alone do not move ships outside weapon reach.
- A mount must fit its local yaw arc; a turnable mount aligns before starting its salvo. Destroyed mounts do not start new salvos.
- Hit roll compares a random value against matrix accuracy × profile accuracy multiplier × caliber accuracy bias × current accuracy modifier.
- Successful shots schedule their impact; continuous damage is divided across damage ticks. Interceptable projectiles can be removed before impact.

### Hull, shields and hardpoints

- Incoming damage first receives the target's `DamageTakenMultiplier`.
- Live shields absorb non-piercing damage with the damage type's shield multiplier. Shield damage does not also apply the hull/class multiplier.
- A shield-breaking hit clamps shields to zero; excess damage from that hit does not spill into hull.
- Shield-piercing damage, absent shields or lost shield generators → hull/class damage path.
- No installed hardpoints → direct hull damage. With hardpoints → selected hardpoint damage, plus hull damage × that hardpoint's `HullDamageMultiplier`.
- A destroyed hardpoint still carries its configured contribution to hull damage on later hits; destroying every hardpoint is not a separate replacement for reducing hull to zero.
- Destroying the last shield-generator hardpoint sets shields to zero and prevents regeneration.
- Hull reaching zero destroys the entity. Weapon fire, subsystem loss and death behavior depend on the installed components.

### Regeneration and squadron health

- Ship regeneration adds `ShieldRegenerateValue × ShieldRegenMultiplier` each configured regeneration interval, up to maximum shields.
- `ShieldRegenerateDelay` is the timer interval in this implementation; taking damage does not reset it in `HealthComponent.ApplyDamage`.
- Squadron health is per member, including separate member shields; losing the final member destroys the squadron.
- Squadron laser/fighter-laser damage against shields also uses `LaserShieldDamageMultiplier`.
- Squadron shield-breaking hits also discard excess damage. Hull repair affects surviving damaged members; it does not resurrect destroyed members.

## Important Values

| Saved rule | Value / effect |
|---|---|
| Laser family baseline | `6` damage, `3` shots, `0.1 s` spacing, `1.5 s` reload, `300` range before caliber/range/variant multipliers |
| Turbolaser family baseline | `16` damage, `3` shots, `0.2 s` spacing, `4 s` reload, `450` range before multipliers |
| Laser damage matrix | Fighter/bomber/interceptor `1.5×`; Capital `0.25×`; shields `0.5×` |
| FighterLaser damage matrix | Fighter/bomber/interceptor `1.5×`; Capital `0.03×`; shields `0.5×` |
| Ion damage matrix | Shields `3×`; ordinary ship/structure hull `0.1×`; strikecraft hull `0×`; non-piercing |
| ProtonTorpedo matrix | Shield-piercing; Capital hull `1.25×`, HeavyCapital hull `1.5×` |

- Matrix rows describe one stage of the calculation, not final damage after weapon caliber, research and abilities.
- Example: CR90's saved loadout is eight `Light Laser Cannon` mounts. Its current base durability and purchase cost are in [[GameDesign/Units_And_Factions]].

## Edge Cases

- A visible turret mesh is not proof of an installed firing mount. Check the gameplay prefab and effective nested overrides.
- `ShipData.Range` is radar; historical notes describing it as weapon range predate the per-weapon system.
- Point-defense target restrictions and missile interception are separate mechanisms; inspect `StrikecraftOnly`, `Interceptable` and installed interceptor components.
- General weapon profiles are derived; ability/superweapon settings also contain serialized profiles with their own values. Do not apply the family factory formula to every stored profile.
- Verified from current source and saved rule assets on `2026-10-20`; runtime behavior and balance were not playtested in this documentation pass.

## Files

- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset` — family/caliber/range rules and weapon definitions.
- `Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset` — class accuracy/damage and shield rules.
- `Assets/Scripts/Components/AttackComponent/WeaponProfileFactory.cs` — `Create`; `Assets/Scripts/Components/AttackComponent/WeaponProfile.cs` — `CycleSeconds`, `DamagePerSecond`.
- `Assets/Scripts/Components/AttackComponent/DamageType.cs`, `Assets/Scripts/Components/Health/Data/ShipClass.cs` — ID meanings.
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs` — `Initialize`, `RollHit`, `ApplyDamage`; `Assets/Scripts/Components/Weapon/WeaponModel.cs` — `RollHit`.
- `Assets/Scripts/Components/Weapon/WeaponEngagementRange.cs` — `Calculate`, `EffectiveDamagePerSecond`.
- `Assets/Scripts/Components/Weapon/WeaponTargetSelectionJob.cs` — preferred targets, range and arcs; `Assets/Scripts/Components/Weapon/WeaponTargetSelector.cs` — planar aim checks.
- `Assets/Scripts/Components/Health/HealthModel.cs` — `ApplyDamage`, `AbsorbsDamage`, `DamageHardPoint`, `DamageHull`.
- `Assets/Scripts/Components/Health/HealthComponent.cs` — `Tick`, `ApplyDamage`; `Assets/Scripts/Components/Squadron/Health/SquadronHealthModel.cs` — `ApplyDamage`, `RepairHull`.
