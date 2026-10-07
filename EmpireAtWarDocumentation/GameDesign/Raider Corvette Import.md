---
type: reference
updated: 2026-10-07
---
# Raider Corvette Import

## Important Values

- Empire `ShipType.RaiderCorvette = 208`; hull/shields/speed `600/800/35`; no targetable hardpoints or hangar.
- Four `BurstLaserCannon = 40` mounts, two shots each; two `DualRepeatingPointDefense = 62` mounts, six repeating shots each.
- Two `HeavyBurstIonBlaster = 60` mounts, two shots each; two `HeavyBurstConcussionMissile = 61` mounts, five shots each.
- `Pursuit = 30`: self activation, speed ×`2.5`, damage ×`2`, fire delay ×`0.5`, shield regeneration ×`0`; active/recovery `15/60 s`. Existing `BoostEnginePowerSettings` implements the stat lifecycle without an attack target.
- Source lacks a damage bonus; user requested one. Damage ×`2` reuses Assault's current bonus provisionally. Source extra incoming-damage/energy-regeneration penalties are outside the supplied specification.
- Regeneration `8/s`; project length/radius `24/15`; tech/cost/build/population/cap `3/2,800/56 s/2/10`. Scale, movement, economy and matchup tuning remain provisional.

## Implementation

- Source: installed AOTR workshop `1397421866`, `E_Raider_Corvette` in `SPACEUNITSCORVETTES.XML` → `Raider_Corvette.ALO`. Requested `Temp/ALO_MODEL_MAP.txt` was absent; current source catalog and live XML agree.
- Blender `3.6.23`, ALAMO, MCP protocol `13`, telemetry disabled/safe mode enabled. Fresh source/validation scenes; other scenes preserved. Background process trims saved blend to Raider-only data.
- Source/FBX/Unity: `4` meshes, `10,301` triangles, `31` bones. Restore only binary-verified identity Root. DDS→PNG pixels match; all seven original file hashes unchanged.
- Source hull/light meshes render; collision/shadow remain in editable blend/FBX. Runtime prefab drops their unused rendering components, retaining attachment transforms.
- FBX maximum corner/UV displacement `0.00001526/0`; Unity maximum bone-position error `0.0000001245` units; source parent relationships match.
- Alpha team mask, normal green-channel flip, centered bow `+Z`/up `+Y`, root scale `1`; visible bounds `9.642543 × 4.807955 × 24` units. Bank ±`15°`; vertical hull `−2.403978..2.449754`.
- Ten muzzle bindings follow banking body; source local `+X` maps to firing direction. Ion/missile yaw centers `−45°/+45°`, width `100°`; laser/PD center `0°`, width `240°`. Serialized limits are relative to the banking parent.
- Saved own gameplay/shield/wreck/hologram/icons, faction/data/view Addressables and three icon consumers. Point defense targets strikecraft; missiles are interceptable.

## Edge Cases

- Imported bone rotation alone does not define a firing cone. Initial test reproduced wrong ion/missile cone centers; fix writes source-derived ship-relative limits.
- Pursuit must activate with no enemy target and remove only its own modifier; repeated start/stop preserves other active buffs.
- No source passive projectile debuffs, weapon energy or death-clone animation was added. Manual skirmish combat/visual acceptance was not performed.

## Files

- `Tools/Blender/RaiderCorvette/README.md` — conversion/build order and evidence.
- `Assets/Scripts/Tests/Editor/RaiderCorvetteTests.cs` — saved assets, loadout, arcs, registrations and Pursuit lifecycle.
- `output/aotr-empire-units/RaiderCorvette-Converted/` — editable blend, FBX, textures, source credits, renders and verification reports.
