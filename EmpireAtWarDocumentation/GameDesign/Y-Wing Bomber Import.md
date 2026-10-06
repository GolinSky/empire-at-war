---
type: reference
updated: 2026-10-06
tags:
  - aotr
  - rebellion
  - squadron
  - asset-pipeline
---
# BTL-A4 Y-Wing Bomber Import

## Decision

- User correction: **Rebellion only**. New `SquadronType.YWingBomber = 301`; Republic `YWing = 3` remains the separate BTL-B.
- AOTR 2.11.9 unit `R_Y_Wing`; map `Temp/ALO_MODEL_MAP.txt` → `RV_Y_WING.ALO`. Separate `RV_Y_WING_TURRET.ALO` → `Turretbase`.
- Installed source: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART`. Original ALO/DDS hashes unchanged; source credits retained in the conversion archive.
- Formation: six bombers; no separate three-craft flight entry. Only individual craft are damage targets; no targetable weapon/system hardpoints.

## Important Values

| Per craft | Value |
| --- | ---: |
| Hull / shields / cruise and combat speed | 30 / 30 / 30 |
| Shield regeneration | 0.15 points/s |
| Extra laser shield damage multiplier | 0.7 |
| Weapons | 1 light dual laser + 1 heavy dual ion mount + 2 medium proton-torpedo launchers |
| Squadron total hull / shields | 180 / 180 |

- Six lasers + six ion mounts + twelve torpedo launchers = **24 weapons**, unique IDs `0..23`; member IDs `0..5`.
- Laser `46` → `MuzzleA_00`; ion `47` → turret `MuzzleB_00`; torpedoes `48` → `MuzzleC_00/01`.
- Project profiles: laser `2 × 5` damage, `0.785 s` reload, range `50`; ion `2 × 4`, `4.91 s`, range `70`; each torpedo launcher `1 × 90`, `20 s`, range `70`. Damage/ranges are provisional.
- Existing Ion Shot `9`: white projectile, target disabled `3 s` on arrival; recovery `20 s`, range `100`, projectile speed `35`.
- Project economy: cost `500`, build `15 s`, tech `1`, population `1`, max `10`; provisional.
- Gameplay hull+turret bounds `2.11971 × 0.60260 × 4.00000` project units; centered at zero, root scale `1`, bow `+Z`, up `+Y`. Navigation radius `18`, formation spacing `4`.

## Implementation

- Scoped ALAMO cleanup avoids touching other Blender scenes. Both source rigs contain `Root`; no root repair. Preserve `Collision` / `Shadow` as disabled helpers; retain four engine-glow meshes.
- Hull texture alpha is uniformly zero → opaque Ship Lit surface; normal map imported as data. Additive engine material is separate.
- Team paint: yellow HSV hue `0.145`, range `0.06`, minimum saturation `0.35`, strength `1`; no alpha team mask.
- New laser shield multiplier defaults to `1` for other squadrons; verified every loaded existing squadron retained `1`. BTL-A4 uses `0.7` in addition to the existing damage matrix.
- Saved six health/flight members; 24 weapon/fog bindings; 24 opaque team-color renderers. Dedicated six-craft hologram placement and actual-model transparent icon.
- Rebellion roster, AssetMappingData, existing View/Data Addressables groups, ShipUiData, TooltipIconData, ReinforcementData and weapon audio registered.

## Edge Cases

- Ion mount currently deals project Ion damage. AOTR's passive `7.5 s` speed/fire-rate `25%` slowdown is **not ported**; active Ion Shot uses the existing project behavior.
- Anti-laser adaptation affects the project's unified Laser damage type; it does not reproduce every AOTR armor-table entry.
- Three-craft flights, source rocket-mode ability timing, astromech repair and animated shader/particle effects are outside this implementation. Ion turret geometry is static.
- Existing Republic BTL-B data, visuals and roster are preserved.
- Editable `.blend` snapshots remain in `Temp/YWingBomberImport/Output`; they include other connected-session scenes. Scoped archive contains this unit's FBXs/textures/reports/previews only.

## Files

- `Assets/Prefabs/Models/Squadrons/YWingBomber.prefab` — geometry/attachments.
- `Assets/Prefabs/Models/Squadrons/YWingBomberSquadronView.prefab` — gameplay squadron.
- `Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset` — per-craft stats and Ion Shot.
- `Assets/Prefabs/Ui/Reinforcement/YWingBomberReinforcementView.prefab` — six-craft placement.
- `Assets/Art/Models/RebellionShips/YWingBomber/`; matching type-first materials/textures folders; `Assets/Art/Textures/Ui/Icons/ShipIcon/YWingBomberIcon.png`.
- `Tools/Blender/YWingBomber/README.md` — exact conversion/integration scripts and readback commands.
- `output/aotr-rebel-units/YWingBomber-Converted/` — scoped evidence and source `Credits_and_Permissions.pdf`.

## Verification

- Blender geometry/UV FBX round trips passed: hull maximum geometry error `1.37e-6`; turret `2.34e-7` source units.
- Unity mesh/triangle/UV parity passed: hull `8 / 5,404`; turret `2 / 272`; `17 + 6` bones/parents preserved. Maximum Unity bone-position error `8.01e-7` before gameplay scaling.
- Saved/live references passed: zero missing scripts/broken references; Rebellion-only entry and correct mappings/addresses; donor visuals absent.
- Transparent `512×512` icon crop `[146,138,437,348]`; eight distinct palette renders; blue/green paint and six-craft placement visually inspected.
- Unity imports and compilation passed; current console readback contained no errors. No automated Unity tests or combat Play Mode started by this task.

## TODO

- [[TODOs/Features/YWingBomber_Import|Clean-skirmish acceptance and provisional balance/source-difference review]].
