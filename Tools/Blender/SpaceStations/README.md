# Leveled space stations

Shared final step for every faction whose station swaps a distinct model per level. Dedicated guide: `Architecture/SPACE_STATION_MODEL_IMPORT_GUIDE.md` in `empire-vault`.

```powershell
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Rebellion"]' --json
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Empire"]' --json
```

Run it after the faction's art, attachment and view builders. It saves every asset it changes.

## `<Faction>.json`

- `view`: gameplay prefab; `levels`: visual prefabs for levels 1–5; `artPrefix`: attached source artwork name prefix.
- `mounts`: one row per gameplay hardpoint id. `anchors` maps the level where an anchor takes effect → source anchor name; the first key must equal the hardpoint's `unlockLevel`.
- Artwork for `FPnn_TYPE_00` is the `artPrefix` child of `HPnn_TYPE_BONE`; an `HP…` anchor is its own bone.
- Anchor type must match the hardpoint: `SHG` shield generator, `TBL`/`TBL2` turbolaser, `LC` laser, `CCM`/`CM` concussion missile, `PRT` proton torpedo, `IC` ion cannon.

## What it enforces

- One pivot: every level's nested source model uses level 1's offset. A shared anchor moving more than `0.01` units between levels fails the build.
- Hull bounds, mounts, launch exit (`Spawn_00` X/Z, `8` units below the hull), and a fitted shield with its center per level.
- View wiring: `StationLevelView`, explosion hull renderers, fog reveal radius and ion field bounds.
