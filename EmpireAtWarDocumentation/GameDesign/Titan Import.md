---
type: reference
updated: 2026-10-14
tags:
  - alo
  - empire
  - ship-import
---
# Titan Import

## Decision

- Empire `ShipType.Titan = 214`; source `Titan_Star_Destroyer_MP` / `Empire_Titan_Star_Destroyer.ALO`, Remake Workshop `1770851727`.
- Balance: hull/shields/speed `28,000 / 18,000 / 16`; cost/build `13,000 / 100 s`; level `5`, population `12`, maximum count `1`, HeavyCapital.
- Hangar squadrons disabled: zero bays, zero hangar hardpoints.
- Existing Power to Weapons `5` replaces proposed cloak; current cloak lacks the requested direct-damage activation restriction. Existing Tractor Beam `22` retains shared timings/25% target speed.

## Implementation

- 107 source models: hull + 106 visual turret attachments; 24 gameplay weapons + 9 subsystems.
- Weapons: 12 heavy quad turbolasers `88` (Titan-owned copy of profile `20`), 6 medium dual turbolasers `5`, 4 heavy ions `11`, 2 point defense lasers `14`.
- Fixed donor injection references/all-renderer bindings; radar `877.49994` reaches the longest gun.
- Subsystems: 4 engines, 3 shield generators, tractor, sensor array. All 33 targets have matching health and colliders.
- Fitted length `472.49353` = current Venator `119.3834` × `4500/1137`; physical 4,500 m remains provisional.
- Own gameplay, reinforcement preview, wreck, icons, team stripes, faction/tooltip/data maps and existing-group Addressable entries.
- Original ALO/DDS unchanged; five source-albedo/UV stripe overlays add 724 derived triangles. Original source meshes and UVs remain unchanged.
- Engine visual repair (2026-10-14): imported `MeshShield.fx` `Engine_Glow` and `Engine_Exhaust_Glow` renderers produced detached rings or invisible exhaust. Both are disabled in the visual prefab; six fitted white additive discs light the original mechanical nozzles. Rebuild with `Tools/Blender/EmpireEngineGlow/FixEmpireEngineGlow.cs` after `BuildArt.cs`.

## Edge Cases

- Source XML missing comma after `HP_Titan_Hanger_03` → Titan audit tokenizes commas/whitespace.
- Source `EV_PHANTOM_SCAN_LINES.DDS` → base-game `corruption/Data/textures.meg`.
- Missing optional `W_te_rock_f_02_b.DDS` recorded; animated EaW cloak distortion unsupported. Source Stealth surface disabled in game geometry.
- Source alpha masks cover nearly the entire hull → derived stripes provide limited team paint.

## Files

- `Tools/Blender/Titan/README.md` — mapping, choices, rebuild commands and verification.
- `Tools/Blender/Titan/Evidence/` and `Previews/` — conversion audit and images.
- `output/remake-empire-units/Titan-Converted/` — editable local blend/FBX package with source shader parameters.
- [[TODOs/Features/Empire_Fleet_Import]] — fleet plan; other ships remain separate scope.

## TODO

- Completed `2026-10-14`: saved verification `52,482`, Titan EditMode `12/12`, three capital simulations, two observed wreck checks and actual local/AI scope composition passed. Commit recorded in Git.
- Manual skirmish balance, live targeting/tractor/long-duration combat acceptance.
