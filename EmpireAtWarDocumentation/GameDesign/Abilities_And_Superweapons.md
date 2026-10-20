---
type: design-reference
system:
  - abilities
  - superweapons
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
---
# Abilities and Superweapons

## Goal

- Current activation, targeting, timing and special-weapon rules.
- Related: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]], [[GameDesign/Production_And_Research]], [[GameDesign/Modes_And_Victory]], [[GameDesign/Save_And_Load]].

## Rules

- Ships and squadrons list assigned `ShipAbilityId` values; `ShipAbilityCatalog` supplies shared definitions, settings, active duration, recovery, targeting and AI-use category.
- Identical display names can have different IDs/settings. Resolve the unit's assigned ID before describing an ability.
- Lifecycle: `Ready → Active → Recovering → Ready`. Recovery begins when the active effect stops, including cancellation.
- A duration of `20 s` and recovery of `50 s` means up to `70 s` between activations when the effect runs to completion.
- Toggle effects remain active until stopped; ordinary timed effects expire. Phased effects can finish according to their own execution state.

## Implementation

### Activation and cancellation

- Activation requires a living caster, no ion disable, an assigned ready slot and any ability-specific requirements.
- While cloaked, the service rejects activation of abilities other than Cloak.
- Tractor Beam additionally requires a surviving tractor-beam hardpoint; Dauntless Sensor Jamming requires a surviving sensor array; Fighter Coordination requires a surviving command array.
- Pressing an ability with cancellable active instances cancels those selected instances and targeting; the same press does not also start ready instances.
- Non-targeted abilities attempt activation for each selected caster. Targeted abilities enter pending enemy-target selection.
- Target must be alive, hostile by player-team relations and not cloaked. Ability-specific checks may restrict target classes/capabilities.
- Immediate activation requires planar distance within the definition's range. An eligible out-of-range caster with a cast-order facade receives a move-and-cast order.
- Caster death ends ordinary running effects. Phased abilities may explicitly survive caster death.
- Ion disable stops ordinary active effects; phased implementations own their additional termination rules.

### Checked current ability examples

| ID / definition | Active / recovery | Effect or restriction |
|---|---|---|
| `14` — CR90 Power to Engines | `20 / 50 s` | Speed `×2`, fire delay `×3`, shield regeneration `×0`; cannot manually cancel |
| `5` — Boost Weapon Power | `7 / 50 s` | Damage `×2`, fire delay `×0.3`, speed unchanged; cancellable |
| `18` — Lock S-Foils | Toggle / `0 s` | Speed `×1.3`, fire delay `×3`, shield regeneration `×3`; press again to reopen |
| `11` — Cloak | `80 / 10 s` | Sets cloaked state; cancellable; generic implementation has no recent-damage activation test |
| `22` — Tractor Beam | `20 / 25 s` | Enemy Corvette/Frigate within `225` units; target speed `×0.25`; cancellable |

- These are shared definition/settings values; not every unit has these abilities.
- Tractor Beam ends when its selected hardpoint or target is destroyed, caster is ion-disabled, or the target leaves range; caster death also ends it.
- Old fleet proposals describe Titan cloak and Allegiance Concentrated Fire. Do not infer current assignments from those proposals; check the unit asset and catalog.
- Active ability save handling is distinct from normal expiration; see [[GameDesign/Save_And_Load]].

## Important Values

### Superweapon construction and use

- All four faction definitions currently permit Ion Cannon, Hypervelocity Gun and Plasma Cannon; availability also depends on battle mode/features.
- Each type builds one use: `Unavailable → Charging → Ready`; firing consumes it and returns that type to `Unavailable`.
- Firing starts a `120 s` cooldown for that type. Rebuilding is allowed while cooling; `CanFire` requires both Ready state and expired cooldown.
- Build cancellation returns Charging to Unavailable. Production costs, queue/refund behavior and unlocks are covered by [[GameDesign/Production_And_Research]].

| Superweapon | Credits | Build | Required level | Population / maximum |
|---|---:|---:|---:|---|
| Ion Cannon | 5,600 | 80 s | 3 | 0 / 1 |
| Hypervelocity Gun | 5,600 | 90 s | 3 | 0 / 1 |
| Plasma Cannon | 5,600 | 80 s | 3 | 0 / 1 |

### Superweapon impacts

| Superweapon | Saved shot profile | Additional behavior |
|---|---|---|
| Ion Cannon | `3 × 15,000`, `0.5 s` shot spacing | IonCannon matrix: shield multiplier `1`, hull multiplier `0`; surviving target disabled for `30 s` |
| Hypervelocity Gun | `10 × 300`, `0.1 s` shot spacing | Non-piercing; shield multiplier `2`; hull multiplier `1` except Structure `0.5` |
| Plasma Cannon | `1 × 6,000` | Shield-piercing; `1,000` area damage within `30` planar units; class multipliers still apply |

- Shot numbers are profile values before target modifiers and hardpoint-to-hull scaling, not guaranteed damage to final hull.
- Primary target gate: hostile, alive, non-strikecraft, `HealthModel.HasUnits`, and an `IHealthFacade`. No global range check appears in `SuperWeaponFireService.CanTarget`.
- Superweapon hits select a random surviving hardpoint; with none surviving, existing hardpoint slots still carry hull damage.
- Plasma splash visits the target's allied team, excluding the primary target; it does not select the shooter's team.
- Repeated ion hits refresh an existing disable to at least `30 s`; they do not stack independent disables.
- Ion stun settings: speed `×0.001`, fire delay `×5`, shield regeneration `×0`, plus `IsIonDisabled` for other system restrictions.

## Edge Cases

- Duration, recovery and fire delay are different quantities. A tooltip's word “cooldown” must be checked against the actual definition.
- Ability definition labels/descriptions are descriptive text; concrete settings and implementation determine behavior.
- Ability/superweapon profiles can be serialized directly; their numbers do not necessarily come from general weapon-family rules.
- This reference verifies source and saved assets on `2026-10-20`; it does not claim every ability was exercised in Play Mode or every unit assignment was audited.

## Files

- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset` — checked definition IDs and managed-reference settings.
- `Assets/Settings/Data/Factions/Shared/SuperWeaponCatalog.asset` — prices, build times and levels.
- `Assets/Settings/Data/Models/SuperWeapons/SuperWeaponData.asset` — shot, stun and area profiles.
- `Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset` — IonCannon, HypervelocityGun and PlasmaCannon interactions.
- `Assets/Scripts/Services/ShipAbilities/ShipAbilityService.cs` — `Press`, `TryFindUsableSlot`, `TryActivate`, `SubmitTarget`, `Advance`.
- `Assets/Scripts/Entities/Ship/Abilities/ShipAbilitySlot.cs` — availability and lifecycle; `Assets/Scripts/Services/ShipAbilities/ShipAbilityDefinition.cs` — definition fields.
- `Assets/Scripts/Services/ShipAbilities/Abilities/TractorBeam/TractorBeamAbility.cs` — `CanStart`, `IsComplete`; `Assets/Scripts/Services/ShipAbilities/Abilities/Cloak/CloakAbility.cs` — cloak state.
- `Assets/Scripts/Entities/SuperWeapon/Model/SuperWeaponModel.cs` — `CanPurchase`, `CanFire`, `Consume`, `COOLDOWN_DURATION`.
- `Assets/Scripts/Services/SuperWeapons/SuperWeaponFireService.cs` — `CanTarget`, `ApplyHit`, `ApplyAreaDamage`, `ApplyStun`.
- `Assets/Scripts/Entities/Players/PlayerRoster.cs` — team relations used for targeting.
