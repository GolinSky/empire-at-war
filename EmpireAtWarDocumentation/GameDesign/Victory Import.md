# Victory Import

## Goal

- Imperial Victory-class Star Destroyer → Empire ship `Victory = 200`, using the supplied `ReV_Victory.ALO`.

## Rules

- Hull / shields / regeneration: `3,400 / 800 / 50 HP/s`; speed / turn rate: `1.6 / 0.5`, entered literally in project data.
- Tech / cost / build / population: `2 / 4,200 / 25 s / 1`.
- Six targets: two `HeavyTurboLaser` batteries, one `IonCannon`, shield generator, engines, fighter bay.
- `VictoryBoostWeaponPower = 12`: active `20 s`, fire delay `×0.5`, speed `×0.5`, shield regeneration `0`, recovery `60 s`; damage and damage taken unchanged.

## Decision

- Use existing heavy-turbolaser and ion weapon profiles; unspecified damage, salvo and range values remain project balance choices.
- Ship damage category: `ShipClass.Frigate`; roster role: heavy cruiser / anti-corvette / anti-frigate.
- TIEs are separately purchasable as well as carrier-launched. Fighter cost/build/level/population: `300 / 10 s / 1 / 1`; bomber: `550 / 17 s / 2 / 1`; each queue limit `10`.
- Costs/build/population come from vanilla `SQUADRONS.XML` skirmish fields; station requirements `1 / 2` map to project availability. Vanilla build-tab restrictions are intentionally removed at the user's request.
- Keep source paint, including Republic roundels; Imperial faction and gameplay do not replace baked source emblems.
- Wreck uses the intact hull with `Ship Wreck` materials; `ReV_Victory_D.ALO` and source animations are not imported.

## Implementation

- `VictoryShipView` → banked hull, six targets with IDs `0–5`, shield volume, bounds, health/fog/weapon bindings and launch point.
- Weapons: `HP_TBL02`, `HP_TBL07`, `HP_LC06`; systems: `HP_Shield`, `HP_Engine`, `HP_Spawn`.
- Visible meshes: hull and lights; source shadow, collision and blast helpers retained disabled.
- Hull shader: `EmpireAtWar/Ship Lit`; additive lights; normal map linear with flipped green; team mask extracted from source alpha.
- Registered data/view Addressables, asset mapping, Empire roster, reinforcement placement, tooltip matchups, icons and ability/audio entries.

### Fighter complement

- Vanilla ALOs extracted read-only from `D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data/models.meg`: `DATA/ART/MODELS/EV_TIEFIGHTER.ALO`, `DATA/ART/MODELS/EV_TIEBOMBER.ALO`; textures from adjacent `textures.meg`.
- ALO → Blender → FBX assets in `Assets/Art/Models/EmpireShips/TIEFighter/TIEFighter.fbx` and `Assets/Art/Models/EmpireShips/TIEBomber/TIEBomber.fbx`.
- `TIEFighter = 200`: seven craft; each `50 HP`, no shields; two `FighterLaser` weapons per craft.
- `TIEBomber = 201`: four craft; each `60 HP`, no shields; one `FighterLaser` and one `FighterProtonTorpedo` per craft.
- Formation sizes and hull values verified against vanilla `SQUADRONS.XML` / `SPACEUNITSFIGHTERS.XML`; no repair or active abilities.
- Highest-detail source `LOD1` visible; lower LOD, shadows, collision and muzzle-flash helper meshes disabled; imported Unity LODGroup removed.
- Hangar reserve includes the first wave: fighter `6 total / 2 active` → four replacements; bomber `3 total / 1 active` → two replacements.
- Existing hangar launches sequentially: initial delay `4 s`, interval `8 s`. Flight values inherit project fighter/bomber profiles.

## Important Values

- Victory visible size: `59.32742 × 37.19255 × 110` project units; bank `±8°`; navigation radius `56`; height tier `1`.
- Banked hull interval: `Y −18.62668 … +18.87795`; launch point below the lower interval by `8` units.
- Provisional Victory limit `10`, range `250`, turn acceleration `0.5`, shield regeneration delay `1 s`.
- Provisional hardpoint HP: weapons `400`, engines `600`, shields `600`, hangar `400`.
- TIE visual maximum extent `5` project units; flight, formation spacing and weapon balance require gameplay review.

## Edge Cases

- Source has `76` Victory bones; importer omitted identity `Root`, restored before FBX export. Bone parenting and bind positions verified.
- All `17` meshes across Victory and TIEs verified against Unity geometry: maximum position error `0.00000693` project units; visible UVs retained.
- Unity can remap UVs on disabled shadow-volume helpers; Blender/FBX retains source UVs. Duplicate faces can share the same comparison match.
- Saved prefabs have no missing scripts/references; unique fighter/weapon/target IDs and registry entries verified.
- Eight team palettes, placement geometry, transparent icons and Victory wreck preview rendered; Unity ready with `0` Console errors.
- Original ALO/texture hashes unchanged. No automated tests or Play Mode run for this import.

## Files

- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; active acceptance plan: [[TODOs/Features/Victory_Import|Victory Import]].
- Victory art: `Assets/Art/{Models,Materials/Models,Textures/Models}/EmpireShips/Victory/`.
- TIE art: corresponding `EmpireShips/TIEFighter/` and `EmpireShips/TIEBomber/` folders.
- Main prefab/data: `Assets/Prefabs/Models/Ships/VictoryShipView.prefab`, `Assets/Settings/Data/Ship/VictoryShipData.asset`.
- Squadrons: `Assets/Prefabs/Models/Squadrons/TIE{Fighter,Bomber}SquadronView.prefab`; `Assets/Settings/Data/Squadron/TIE{Fighter,Bomber}SquadronData.asset`.
- Local evidence: `Temp/VictoryImport/{SavedInspection,TIEInspection,FinalAssets,GeometryVerification}.json`; `Output/SourceHashes.json`; preview PNGs are temporary.

## TODO

- Clean-skirmish acceptance: Empire purchase/placement, movement/banking, all six targets, both TIE launch/replacement counts, boost duration/recovery/stat restoration, destruction and wreck.
- Review provisional scale, movement units, weapon/system HP, range, limit, TIE flight and launch timing.
