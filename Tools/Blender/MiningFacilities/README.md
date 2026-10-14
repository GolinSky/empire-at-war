# Vanilla mining facilities

## Source and mapping

- Original installed game: `D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data`.
- Models/textures are extracted read-only from `models.meg` / `textures.meg`. XML comes from `config.meg`; source ALO/DDS hashes are in `Evidence/SourceAudit.json`.
- `NB_ASTEROIDMINING_FULL.ALO` → `MiningFacility.fbx` → completed mining facility.
- `NB_APBASE.ALO` → `BattleMiningFacility.fbx` → completed battle mining facility.
- `NB_CMC22MINING.ALO` → `MiningConstruction.fbx` → shared facility-under-construction visual. This source contains a ground building, not an asteroid.
- `SPACEBUILDABLESSKIRMISH.XML` identifies `Empire_Mineral_Extractor`; `STARBASES.XML` identifies `Pirate_Asteroid_Base`.
- Pirate base XML → `HARDPOINTS.XML` specifies **10 attached pieces / 9 unique ALOs**. `Evidence/Attachments.json` records model and bone mapping: hangar, shield generator, two heavy turrets, four turbolasers, and the same laser-cannon model at two mounts.

## Conversion

Run from the repository root with Blender 3.6.23 / ALAMO enabled and the configured Blender MCP connection running:

```powershell
uv run --with pillow python Tools/Blender/MiningFacilities/Prepare.py
uv run --with pillow python Tools/Blender/MiningFacilities/ConvertRequest.py
uvx --from mcp-for-blender==2.1.3 python Tools/Blender/mcp_call.py Temp/MiningFacilitiesImport/ConvertRequest.json > Temp/MiningFacilitiesImport/ConvertResult.log
uv run --with pillow python Tools/Blender/MiningFacilities/Verify.py
```

- Output: `Temp/MiningFacilitiesImport/Output/` contains editable packed `.blend`, FBX, and lossless decoded DDS → PNG textures. Temporary source copies/logs stay outside committed assets.
- Uses the existing vanilla-station binary parser/extractor. The converter works in a dedicated scene and preserves unrelated scenes.
- Rebuilds material slots from source shader + parameters; converts `CHILD_OF` constraints to source-indexed bone parenting. Restores the verified identity `Root` that ALAMO removes; preserves duplicate bone names with Blender suffixes.
- `Verify.py` compares every non-shadow imported triangle corner directly with the ALO binary, then records a separate FBX roundtrip comparison. Shadow volumes are retained; ALAMO welds their doubled vertices/removes degenerate triangles.

## Visibility and materials

- Keep source collision/shadow/damage geometry in FBX/blend. `EaWSourceHidden` preserves the mesh flag; `EaWHidden` records intended display visibility, also honoring bone visibility.
- Mining `Main_ALT01..04` and corresponding drillers are **four separate intact assemblies**, at distinct source positions with the same intact texture. Keep all four; their names do not indicate mutually exclusive damage states.
- Construction `ALT1/2/3` overlap the intact `ALT0` and use `_D1/_D2/_D3` damage textures. Display `Driller_ALT0`, `Fan_ALT0`, `Main_ALT0`; retain all alternatives in FBX/blend.
- Source-hidden construction girders, collision helpers, shadow volumes, and pirate `*_blast` damage overlays are excluded from gameplay display.
- Mining `Asteroid_shell` uses `MeshAlphaGloss.fx` and is an alpha overlay over `Asteroid`; preserve its alpha and gloss intent. Do not render as an opaque second rock.
- Girder/grate `MeshAlpha.fx` surfaces require alpha; `MeshAdditive.fx` windows/glows require additive materials. FBX Principled materials are transport representations; final Unity materials implement these intents.
- Normal maps are data textures. ALAMO's convention requires green-channel inversion in Unity. Full source shader parameters and texture mappings remain in evidence.

## Conversion verification

Verified 2026-10-14, Blender 3.6.23, MCP protocol 13, telemetry disabled.

| Main model | Meshes | Imported triangles | Bones |
| --- | ---: | ---: | ---: |
| MiningFacility | 21 | 7,138 | 27 |
| BattleMiningFacility | 26 | 5,940 | 68 |
| MiningConstruction | 30 | 16,990 | 63 |

- All 12 models (three main + nine attachments): **zero source position error and zero source UV error** for non-shadow triangles.
- FBX roundtrip: maximum position error `0.000248` source units, bone displacement `0.000137`, UV error `0`; mesh triangle counts and bone names/parents preserved.
- PNG decoded RGBA pixels match original DDS pixels exactly; no source game files modified.
- Source construction model inspected in material-preview viewport; final Unity appearance/size are integration checks.
- No ALA animation or EaW particles/shader animation recreated; model geometry is static.

## Integration and verification

- See the final Unity integration scripts/evidence in this folder for asset paths, gameplay scale, capture radii, attachment placement, persisted references, and rendered verification.

### Unity assets and sizing

- `BuildArt.cs` imports 12 FBXs into `Assets/Art/Models/SpaceStations/VanillaMiningFacilities`, maps source material slots, and creates geometry-only prefabs in `Assets/Prefabs/Models/MiningFacilities/Vanilla`.
- `Integrate.cs` replaces `AsteroidMiningFacilityView` and `AsteroidDefendPlatformView` in place. Existing prefab GUIDs, installer registrations, health, income, weapon types/counts, and costs remain intact. Standalone reinforcement `MiningFacilityView` is outside this replacement.
- Saved mine bounds: `103.30 × 107.34 × 120.00` units; battle mine: `88.48 × 99.91 × 120.00`. Gameplay root scales are `1`.
- Both capture-site root scales: `5 → 2`; logical capture radius `200 → 80` units and visual ring diameter `400 → 160` units (2.5× reduction). Capture duration remains `12 s`.
- Regular site facility height: `-150 → -60` world units; battle site stays `0`. Source asteroid meshes replace old site rocks. Battle site's mesh collider and explicit `rockObstacles` binding match the new rock; regular site remains without navigation rock obstacles.
- Each site's two existing construction wrappers instantiate the same `MiningConstruction.prefab` (65-unit horizontal size), preserving independent active state and the existing progress animation. Child scale accounts for the site scale; wrapper Y remains free for progress growth.
- `CaptureSiteView.asteroidRocks` hides the site rock while operational; the completed ALO supplies its own asteroid. It returns for neutral/owned/construction states after destruction. Cached battle navigation footprint remains the corresponding source rock footprint.
- Original APBASE attachments retained: ten mounts / nine models. Six existing weapons map to `HT1`, `HT2`, and `TL1–TL4` source muzzle bones; `LL1/LL2` stay decorative. No weapon stats or counts added.
- Bounds-dependent collider, selection ring, ion field, shield mesh, fog/team/explosion renderer lists and both wreck prefabs regenerated. Spawn blocker radii shrink `400 → 160` (mine), `500 → 200` (battle).

### Materials and validation

- Opaque surfaces use `EmpireAtWar/Ship Lit`; alpha shell/grates use URP Lit transparency; additive effects retain original texture and tint. Normals import as linear normal maps with green-channel inversion.
- Original grayscale asteroid gloss is packed into alpha of a derived linear metallic/smoothness map, with metallic `0`; the original source PNG is retained. PBR lighting approximates the original EaW shader; UV animation/particles are not recreated.
- Team ownership uses the existing Ship Lit rim coloring. All eight palettes rendered; contrasting blue/green inspected. Original neutral surface texture colors remain unchanged.
- `Verify.cs` reloads six saved gameplay/site/wreck prefabs and checks missing scripts/references, meshes/materials, required renderer/binding arrays, six battle weapon references and 80-unit capture radii. Results: `Evidence/UnityVerification.json`.
- `Render.cs` renders saved geometry plus construction/operational site combinations and wrecks without instantiating uninjected gameplay components. Selected images retained in `Previews`; full render set in `Temp/MiningFacilitiesImport/Previews`.
- Unity `6000.4.7f1`: imports and scripts complete; Console error count `0`; `MainMenuScene` remains clean. No Play Mode or automated tests run (per import-guide scope).
- Legacy asset rebuild menus still describe the previous procedural facility pipeline; use these scoped scripts to reproduce the vanilla replacement. The legacy site builder receives the mandatory new rock binding so it cannot create an unbound `CaptureSiteView`.

```powershell
unity command run_script --file Tools/Blender/MiningFacilities/BuildArt.cs --entry BuildMiningFacilityArt.Main --json
unity command run_script --file Tools/Blender/MiningFacilities/Integrate.cs --entry IntegrateMiningFacilities.Main --json
unity command run_script --file Tools/Blender/MiningFacilities/Verify.cs --entry VerifyMiningFacilities.Main --json
unity command run_script --file Tools/Blender/MiningFacilities/Render.cs --entry RenderMiningFacilities.Main --json
```
