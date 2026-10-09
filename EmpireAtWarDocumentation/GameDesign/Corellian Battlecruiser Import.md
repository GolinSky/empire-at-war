---
type: reference
updated: 2026-10-09
tags:
  - alo
  - rebellion
  - asset-pipeline
---
# Corellian Battlecruiser Import

## Rules

- Import guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. Build/evidence commands: `Tools/Blender/CorellianBattlecruiser/README.md`.
- `Temp/ALO_MODEL_MAP.txt` was absent. `output/aotr-rebel-units/ship-catalog.csv` + installed AOTR XML identify `R_Corellian_Battlecruiser` → `CV_Battlecruiser.ALO`, Workshop `1397421866`.
- Original ALO/DDS inputs remain unchanged; 25 SHA-256 hashes verified. No automated Unity tests or Play Mode run.

## Implementation

- Rebellion ship `306`: level `2`, tactical population `3`, role `Fleet reinforcement / Squadron support`.
- All 20 hardpoints targetable: 4 heavy two-burst barrage rocket launchers + 8 medium turbolasers + 5 laser cannons + shield generator + engine + one hangar.
- Weapon profiles: new rocket `64` (damage `50`, 2 shots, interval `1 s`, reload `2 s`, range `600` units); existing medium turbolaser `4` and laser `8`. Shared profiles retain existing balance.
- Four source turret ALOs included as visual attachments. Their additional source weapons are excluded to preserve the user-requested counts.
- Hangar: X-Wing `300`, A-Wing `1`, Y-Wing bomber `3`; 6 total launches/type, 1 active/type (3 overall). Initial/interval `4/8 s`; destroyed hangar stops all launches. Launch clearance `8` units.
- Power to Shields `34`: existing `BoostShieldPowerSettings`; shield regeneration ×2, weapon damage/speed ×0.75, active `15 s`, recovery `40 s`.
- Source hull/shields `14,000/8,000`; price/build `2,000/81 s`; build limit `3`. Price uses the requested override; source tactical cost is `12,080`. Size `140` units, speed `25`, navigation radius `80`, hangar count/timing/position and matchups are provisional project choices.

## Important Values

- Frigate regeneration: Acclamator/Captor both restore `0.6667%` of maximum shields every `3 s` → Battlecruiser `53.3333/tick`, `17.7778/s`.
- Existing frigates have no passive hull regeneration; Battlecruiser also has none. No new hull-regeneration logic was added.
- Blender/FBX: 5 models, 37 meshes, 135 bones; maximum triangle-corner/bone error `0.00006993/0.00005150` source units; UVs and bone parents retained.
- Unity: 9,751 imported triangles; attachment error ≤`0.00000123` units. ALAMO removes degenerate shadow-helper triangles; visible material/triangle assignments match the binary source.
- Saved live hull: `43.5615 × 36.2110 × 140` units including engine effects; root scale `1`, bow `+Z`, up `+Y`; ±5° bank envelope `−18.1068 .. 19.0168`.
- Four hidden hull helpers remain in the FBX and are omitted from game prefabs. Placement/wreck retain 25 opaque renderers; live hull has 33 visible renderers.

## Edge Cases

- Imported bones contain a `100×` scale and `−90°` basis conversion. Parent separate turret roots beside the armature and position them at authored sockets; direct bone parenting magnifies/rotates turret art.
- ALAMO `hideLODs` scans global objects. During turret import, temporarily link existing objects to the active view layer, then unlink and restore visibility.
- Source alpha team masks are empty. Use existing red hull markings through HSV livery; leave empty masks disabled. Eight live/wreck palette renders verified; global palette state restored.
- All three squadron configurations share one physical hangar. Empty per-bay hardpoint/launch arrays select existing whole-hangar shutdown behavior.

## Files

- `Assets/Prefabs/Models/Ships/CorellianBattlecruiser.prefab`, `CorellianBattlecruiserShipView.prefab` — dedicated art/gameplay.
- `Assets/Prefabs/Ui/Reinforcement/CorellianBattlecruiserReinforcementView.prefab` — dedicated hologram; saved slots and registration verified.
- `Assets/Prefabs/Models/Wrecks/CorellianBattlecruiserWreckView.prefab`; `Assets/Settings/Data/Ship/CorellianBattlecruiserShipData.asset` and `Ship/Wreck/CorellianBattlecruiserWreckData.asset`.
- `Assets/Art/Models/RebellionShips/CorellianBattlecruiser/` — 5 source FBXs and static game meshes; corresponding materials/textures use the same category.
- `Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianBattlecruiserIcon.png` — actual-model transparent 512×512 sprite; crop `[71,144]..[419,356]`; roster/HUD/tooltip assignments verified.
- `output/aotr-rebel-units/CorellianBattlecruiser-Converted/` — packed Blender files, FBXs, PNGs, source/material/geometry/registration reports and renders.

## TODO

- In-game acceptance: replacements, launch sequence, firing arcs, system destruction, shield ability, fog/selection, reinforcement placement and death/wreck lifecycle.
- Review provisional balance; EaW ALA animations and shader proxies were not ported.
