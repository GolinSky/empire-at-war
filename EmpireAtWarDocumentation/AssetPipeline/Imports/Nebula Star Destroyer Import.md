---
type: integration-record
updated: 2026-10-12
tags:
  - alo
  - unity
  - rebellion
  - nebula
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# Nebula Star Destroyer Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.

## Decision

- Rebellion `ShipType.Nebula = 330`; source Workshop `1770851727` (Remake).
- Source hull `Rebel_Nebula_Star_Destroyer_Prototype.ALO` + 15 XML-attached turret models; source files unchanged.
- Balance source: [[GameDesign/Proposals/Rebel units#10. Nebula Star Destroyer]]. Import workflow: [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].
- User-approved fighter substitution: X-Wing `300`.

## Important Values

- Hull `16,000`; shields `15,000`; speed `20`; cost `7,200`; build `72 s`.
- Weapons: 8 dual heavy turbolasers, 6 medium turbolasers, 4 medium ion cannons, 2 assault concussion launchers, 4 point defense lasers.
- Subsystems: 2 engines, 2 shield generators, 2 hangars, 1 tractor target → 31 targetable hardpoints total.
- X-Wing budgets `3/2`, active limits `2/1` → 3 active squadrons + 2 replacements; initial launches consume budget.
- Overcharge Weapons `38`: +25% firing rate, shield regeneration disabled, duration `10 s`, recovery `50 s` after expiration.
- Provisional: length `160`, level `5`, population `8`, max count `3`; regeneration `25/s` after `3 s`; hangar launch delays `4/8 s`.
- Visible size `83.07693 × 28.63188 × 160`; bank ±5°, Y range `−17.10768 .. 14.32681`; navigation radius `98.14124`.

## Implementation

- Preserve source hull/attachment geometry and UVs; binary shader slots repaired before FBX export.
- Heavy batteries: two mounts at each `NebulaPart01..04/FP_01`, derived ±0.65-unit offsets. Medium batteries: parts `09..14`; ions: parts `05..08`.
- Concussion launchers use existing heavy missile profile `65`; point defense uses `14`. Exact mapping: `Tools/Blender/Nebula/Evidence/HardpointMapping.json`.
- Engine/shield/hangar pairs use measured hull bounds and named source bones; each hangar has its own launch point and disables its own bay on destruction.
- Source central heavy-ion turret art retained; no extra gameplay weapon. Tractor is a targetable subsystem; reference ability list contains only Overcharge Weapons.
- Opaque Ship Lit surfaces recolor authored red trim; original RGBA texture copies remain unchanged. Additive project copies approximate static light/engine effects.
- Helpers retained in `.blend`/FBX, removed from unit visuals. Gameplay shield retained; roots unit scale, bow +Z, up +Y.

## Files

- `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/Nebula/`.
- `Assets/Prefabs/Models/Ships/Nebula.prefab`, `NebulaShipView.prefab`; dedicated wreck and reinforcement prefabs.
- `Assets/Settings/Data/Ship/NebulaShipData.asset`, `Ship/Wreck/NebulaWreckData.asset`.
- `Tools/Blender/Nebula/README.md` — complete rebuild commands, mappings, provisional values and verification limits.
- `Tools/Blender/Nebula/Evidence/`, `Previews/`; editable package `output/remake-rebel-units/Nebula-Converted/`.

## Edge Cases

- 16 FBX conversions: 56 meshes, 37,957 triangles, 155 bones; maximum geometry error `0.000089055` source units.
- Saved Unity checks: 8,073 passed; Nebula ability settings independent of donor; maximum bone error `0.000000728` project units; compilation-failed flag false.
- 37 source hashes unchanged; 47 staged texture reference copies pixel-identical. Eight team palettes rendered; blue/green, hologram and wreck visually checked.
- Automated tests and Play Mode were not run, per import guide. Runtime combat, launches, ability timing, placement and wreck lifecycle remain unverified.
- Source animated shader effects and ALA animations are not recreated.
