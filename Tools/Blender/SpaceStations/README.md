# Leveled space stations

Shared final step for every faction whose station swaps a distinct model per level. Dedicated guide: `Architecture/SPACE_STATION_MODEL_IMPORT_GUIDE.md` in `empire-vault`.

```powershell
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Rebellion"]' --json
unity command run_script --file Tools/Blender/SpaceStations/BuildStationLevels.cs --args '["Empire"]' --json
```

Run it after the faction's art, attachment and view builders. It saves every asset it changes.

## Gameplay armament

After `BuildStationLevels`, apply the expanded armament and inspect the saved prefabs:

```powershell
./Tools/Blender/SpaceStations/Update-StationArmament.ps1 -Faction Empire
./Tools/Blender/SpaceStations/Update-StationArmament.ps1 -Faction Empire -Inspect
```

- Repeat for `Rebellion`, `Republic`, and `Separatist`.
- Every weapon type has `1 / 2 / 4 / 6 / 8` installed mounts at levels 1–5; shield generator remains one.
- Original IDs, source art bindings and authored unlock levels remain; clones fill each level up to the installed count.
- Additional mounts use exposed hull triangles near existing weapons, with `4–10` units of clearance. A mount is placed once at its unlock level on a point that is hull surface on every later level, then keeps that position; anchors are named `GPnn_TYPE_00`. Separate source weapon art is copied where available; Republic/CIS retain their embedded-art convention.
- `BuildStationLevels` restores authored mounts; always run the armament step afterward. Both steps save assets automatically.
- Inspection reloads saved assets, checks mount/unlock counts and health/weapon/injection bindings, measures new anchor distance to hull geometry, and renders top/side previews with colored hardpoint markers.
- Evidence: `Temp/StationArmament/<Faction>.json` and `<Faction>Level<N>{Top,Side}.png`. This inspection does not run Unity tests.

## `<Faction>.json`

- `view`: gameplay prefab; `levels`: visual prefabs for levels 1–5; `artPrefix`: attached source artwork name prefix.
- `mounts`: one row per gameplay hardpoint id. `anchors` maps the level where an anchor takes effect → source anchor name; the first key must equal the hardpoint's `unlockLevel`.
- Artwork for `FPnn_TYPE_00` is the `artPrefix` child of `HPnn_TYPE_BONE`; an `HP…` anchor is its own bone.
- Anchor type must match the hardpoint: `SHG` shield generator, `TBL`/`TBL2` turbolaser, `LC` laser, `CCM`/`CM` concussion missile, `PRT` proton torpedo, `IC` ion cannon.

## What it enforces

- One pivot: every level's nested source model uses level 1's offset. A shared anchor moving more than `0.01` units between levels fails the build.
- Hull bounds, mounts, launch exit (`Spawn_00` X/Z, `8` units below the hull), and a fitted shield with its center per level.
- View wiring: `StationLevelView`, explosion hull renderers, fog reveal radius and ion field bounds.
