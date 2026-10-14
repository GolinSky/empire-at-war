---
type: reference
updated: 2026-10-14
tags:
  - alo
  - empire
  - ship-import
---
# Allegiance Import

## Decision

- Empire `ShipType.Allegiance = 213`; Remake 4.0 Workshop `1770851727`, unit `Allegiance_Star_Destroyer_MP`.
- Source: `Empire_Allegiance_Star_Destroyer_Hangar.ALO` + 53 XML-referenced turret models. Original ALO/DDS/XML hashes unchanged.
- Squadron bays/reserves/active slots `0`; no `HangarComponent` or hangar hardpoint. Source hangar geometry stays visual-only.
- Power to Main Batteries `24` replaces the requested class-restricted Concentrated Fire under user authorization; existing Tractor Beam `22` retained with its existing tuning.

## Important Values

- Hull/shields/speed: `20,000 / 16,000 / 12`; price/build time: `9,200 credits / 85 s`.
- Weapons: `3` heavy dual turbolasers (`3`), `6` heavy quad turbolasers (`20`), `6` medium dual turbolasers (`5`), `6` medium ion cannons (`32`). Nine heavy mounts marked `mainBattery`.
- Targets: `21` weapons + `3` engines + `2` shield generators + `1` tractor beam + `1` command array = `28`; contiguous IDs `0..27`.
- Venator hull length `119.38` units × `2200/1137` → Allegiance length `230.99`; width/height `141.93/62.62` units. Unit root scale `1`, bow `+Z`, up `+Y`.
- Project choices: level `4`, maximum count `3`, population `12`; navigation radius `141`, bank `±5°`, rotation/acceleration `1.5`, shield regeneration `80/s`.
- Hardpoint health: weapons `600`; engines/generators `1,200`; tractor/command `800`. Destroying individual systems uses existing ship behavior.

## Implementation

- Source binary material assignments restored before FBX export; original hidden state retained in conversion assets. Shader-verified collision/shield/shadow helpers removed from gameplay prefabs.
- Proposed placement adapted to verified Remake sockets; heavy dual mounts use `TL_01..03`, quads use three sockets per side. Exact muzzle origins: `Tools/Blender/Allegiance/Evidence/HardpointMapping.json`.
- Own fitted shield, collision/banked height/selection bounds, wreck, hologram, transparent model icon and silhouette. Complete fog, explosion and team renderer bindings.
- Existing View/Data Addressables groups retained; ship registry, asset mapping, Empire roster, icon consumers, reinforcement mapping and regeneration mapping registered.
- Source alpha has no useful livery mask; neutral hull panels retain shared team rim coloring. Eight live/wreck palettes rendered and compared.
- Engine visual repair (2026-10-14): imported `MeshShield.fx` glow meshes produced detached rings or faint exhaust. They are disabled in the visual prefab; three blue main and four amber secondary additive discs light the original mechanical nozzles. Rebuild with `Tools/Blender/EmpireEngineGlow/FixEmpireEngineGlow.cs` after `BuildArt.cs`.

## Verification

- Blender `3.6.23` / ALAMO / MCP protocol `13`; Unity `6000.4.7f1`.
- `54` ALO→FBX models: mesh/triangle/UV-layer counts, bone hierarchy and bounds checked. Direct vertex/UV-corner comparison: position error `<0.0002` source units; UV error `0`.
- `95` helper renderers / `53,669` triangles stripped from unit-prefab representations; FBX/blend source fidelity retained.
- Allegiance EditMode tests `12/12` passed; saved assets, economy/registrations, disabled squadrons, destruction, main batteries, scale/collision, muzzle origins and VContainer injectable registration verified.
- Spawn repair (2026-10-14): donor injection list retained `30` null hardpoint references after geometry replacement → rebuilt with `28` current hardpoints + `TeamColorView`. Regression reproduced the failure before repair; all entity prefabs now contain no null injectables. Builder and generator retain this binding.
- Deterministic half-bearing simulations at distance `200` units: Allegiance won vs Venator / Victory II / MC80 in `21.75 / 25.55 / 57.55 s`.
- No Play Mode navigation, visual turret tracking or player UI acceptance run. Source death ALA is not converted by this static import.

## Files

- `Tools/Blender/Allegiance/README.md` — repeatable pipeline and source/project differences.
- `Tools/Blender/Allegiance/Evidence/` — source hashes, conversion, geometry, mounts, bounds and test results.
- `Assets/Scripts/Tests/Editor/AllegianceShipTests.cs` — unit regression suite.
- `output/remake-empire-units/Allegiance-Converted/` — packed editable blends, FBXs, textures and previews.
- [[GameDesign/Empire Fleet Import]]; [[TODOs/Features/Empire_Fleet_Import]]; [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
