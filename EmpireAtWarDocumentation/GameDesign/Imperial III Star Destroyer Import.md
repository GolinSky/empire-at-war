---
category: Features
source: AOTR Workshop 1397421866
updated: 2026-10-06
---
# Imperial III Star Destroyer Import

## Implementation

- Empire `ShipType.ImperialIIIStarDestroyer = 206`; display name `Imperial III Star Destroyer`.
- Source unit `E_Imperial_Star_Destroyer_3`; model `EV_ISD3.ALO`, from `Temp/ALO_MODEL_MAP.txt`.
- Imported according to [[ALO_MODEL_IMPORT_GUIDE]] and [[PROJECT_ORGANIZATION]]. Original source files remain unchanged.
- Five unique model families; 18 mounted turrets follow source XML bones. Source `Hangar.tga` requires explicit resolution because the importer labels it `Hangar.dds`.
- Saved own gameplay prefab, data, placement hologram, wreck, materials, 512×512 transparent icon/silhouette, and eight live/wreck team previews.
- Registered Empire faction, existing Addressables Data/View groups, ship/asset mapping, placement, battle/tooltip/faction icons, weapon/audio profiles, ability catalog, and damage matrix.

## Important Values

- Hull `28,000`; shields `18,000`; speed `250`; shield regeneration `22.5/s`.
- Targetable: 10 heavy dual turbolasers, 2 heavy long-range 2-burst turbo-ions, 2 shield generators, 3 engines, 1 hangar, 1 tractor beam. IDs `0..18`.
- Non-targetable: 5 medium 3-burst turbolasers, 4 medium turbolasers, 4 medium turbo-ions, 6 heavy lasers, 1 composite beam. IDs `19..38`.
- Automatic weapons: `31`; composite weapon profile `45` is ability-only. New heavy dual profile `43`; long-range 2-burst turbo-ion profile `44`.
- `Fire Composite Beam` ability `25`: `6 s`, recovery `45 s`, range `375`, raw damage `1,000`; hull coefficient `1`, shield coefficient `0.1`.
- Composite targets: `Frigate`, `Capital`, `HeavyCapital`; excludes corvettes, strikecraft, and structures. Explicit `ICompositeBeamFacade` muzzle follows banking body.
- Shared Tractor Beam ability `22`: corvettes/frigates only; speed multiplier `0.25`, duration `20 s`, recovery `25 s`, range `150`.
- User-selected hangar: TIE Avenger `202` (`3` total launches, `1` active), TIE Punisher `205` (`2` total launches, `1` active). Punishers substitute for source TIE Scimitars. Launch delay/interval `4/30 s`.
- Campaign economy from source XML: `26,000` credits, build `520 s`, tech `4`, capacity `9`. Limit `10`, shared tuning, size, and copied matchups remain provisional.
- Mounted bounds `115.89669 × 64.00111 × 192.90494`; navigation radius `118`; bank `±8°`; banked vertical range `-35.03626..32.75116`. Gameplay root scale `1`.

## Verification

- Unity asset readback passed: exact stats, weapon composition, 19 ordered targets, hangar/ability references, placement, icons, dependencies, and faction/Addressables registrations.
- No missing scripts or broken serialized references in gameplay readback; source bone/muzzle positions match. Maximum mount error `0`; raw Unity bone error `0.00000254`.
- Source hull `102` meshes / `52,380` triangles / `202` bones. Four turret families: heavy dual `6/1,402/8`, medium triple `3/574/7`, ion `7/896/11`, composite `6/2,406/9`.
- Source→FBX corner UVs match; maximum Blender position error `0.00054932`. Maximum raw Unity geometry position error `0.00000488`.
- Blender reimport retriangulates hidden collision mesh from `17,725` to `17,709` triangles. Unity retains source count. Unity visible surface UV correspondence passed; hidden collision/shadow positions checked separately because unused UV corners differ after import.
- `50` source file hashes unchanged; `26` exact texture pixel comparisons passed. Transparent icon framing and eight live/wreck team palettes checked.
- Automated Unity tests and Play Mode were not run.

## Files

- Pipeline and rebuild procedure: `Tools/Blender/ImperialIIIStarDestroyer/README.md`.
- Saved conversion/evidence: `output/aotr-empire-units/ImperialIIIStarDestroyer-Converted/`.
- Gameplay: `Assets/Prefabs/Models/Ships/ImperialIIIStarDestroyerShipView.prefab`.
- Data: `Assets/Settings/Data/Ship/ImperialIIIStarDestroyerShipData.asset`.
- Composite implementation: `Assets/Scripts/Services/ShipAbilities/Abilities/CompositeBeam/` and `Assets/Scripts/Entities/Ship/Abilities/ICompositeBeamFacade.cs`.

## TODO

- Runtime acceptance when requested: beam timing/armor/shields, class restriction, moving-target rendering, tractor cancellation, AI ability use, hangar clearance, banking, explosions, and wreck transitions.
