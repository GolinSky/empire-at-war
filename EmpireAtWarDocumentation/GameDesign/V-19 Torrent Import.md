---
type: reference
updated: 2026-10-05
tags:
  - blender
  - unity
  - alo
  - squadron
---
# V-19 Torrent Import

## Decision

- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. Republic interceptor, `SquadronType.V19Torrent = 5`.
- User values: 5 craft; hull/shields/regen `70/30/3` per craft; 2 lasers per fighter; damage `5`; Fighter armor; cost/build/tech/population `400/6 s/1/1`.
- Local RaW 1.2.1 XML agrees on health, shields, regen, count, cost and population; records damage `8` and build `8 s`. User-supplied current documentation values take precedence.
- Source speed/min speed/turn `5/3.5/6` are EaW values, not Unity units or degrees/s. No automatic V-Wing replacement at tech 5 is configured.

## Implementation

- Source: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_v19_torrent.ALO`. Original ALO and three referenced DDS hashes unchanged.
- Blender `3.6.23`, existing ALAMO importer and MCP protocol `13`; safe mode on, telemetry off. Restore only the binary-verified identity `Root`.
- Preserve all 12 meshes, 24 bones, attachment hierarchy, UVs and deployed wing pose. Eight visible meshes; collision, shadow and two muzzle flashes retain disabled renderers.
- `MeshBumpColorize.fx` → Ship Lit with normal map/green-channel flip and linear alpha team mask; `MeshAdditive.fx` → additive Unlit. All five FBX material slots explicitly remapped.
- Guns use `MuzzleA_00/01`; `FighterLaser` shared profile supplies 5 damage. Ten unique weapon IDs, no separate targetable fighter systems; health targets are the five fighters.
- Three engine trails per craft use `Object01/02/03` source attachments. Common fighter destruction effects retained; no capital-ship wreck/hangar configuration.
- Own five-member placement prefab, roster/HUD/tooltip icon, silhouette, matchups, View/Data Addressables and asset mappings saved.

## Important Values

- FBX scale `0.02`; visible member bounds `7.01667 × 3.24540 × 4.00000` project units; root scale `1`, centered, bow `+Z`, up `+Y`.
- Provisional flight: cruise/combat `35/40 units/s`, acceleration `30 units/s²`, turn `120°/s`, bank `60°`, height `11`, formation spacing `5`.
- Provisional limit `10`; shield refresh `3` every `1 s`; no passive hull repair. Navigation radius `17`, selection diameter `34`, member collider radius `4`.
- Authored hull alpha team-mask coverage `20.00885%`; all eight palette renders saved, blue/green visually inspected. Neutral hull panels retained.

## Verification

- Source `4,020` triangles → FBX/Unity `3,960`; exactly 60 zero-area faces removed from `Object01/02/03`. All nondegenerate triangle corners and UVs preserved.
- Blender maximum geometry/bone error `0.000164265/0.000106171` source units; visible bounds delta `0.000022889`.
- Unity maximum geometry/bone error `0.000003258/0.000002030` project units; UV error `0`, no bone-parent mismatches.
- Reloaded prefabs: five members, ten lasers, three engine trails per craft; health/flight arrays `5`, weapons/fog targets `10`, team renderers `40`; no missing scripts, broken references or donor dependencies.
- Icon/silhouette: transparent `512 × 512`, uncropped; isolated five-member formation render inspected. Unity imports/compilation complete; no new Console errors.
- No automated tests or Play Mode run. Existing MainMenuScene remained clean and unchanged.

## Files

- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RepublicShips/V19Torrent/`.
- Prefabs: `Assets/Prefabs/Models/Squadrons/V19Torrent.prefab`, `V19TorrentSquadronView.prefab`; `Assets/Prefabs/Ui/Reinforcement/V19TorrentReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Squadron/V19TorrentSquadronData.asset`; `Assets/Settings/Data/Tooltip/Matchups/V19TorrentSquadronMatchups.asset`.
- Conversion: `Tools/Blender/prepare_v19_torrent_textures.py`, `export_v19_torrent.py`; packed blends, FBX, textures, scripts and reports in source sibling `ReV_v19_torrent-Converted/`.
- Credits: mesh/textures Evillejedi; rigging z3r0x, from the supplied RaW `credits.txt`.

## TODO

- Hunt is absent from the current ability system; no Hunt button/behavior registered. Follow-up choice requested; default import scope records it pending.
- Clean-skirmish acceptance: deployment, firing, shields, fog, selection, team color and individual fighter destruction.
- Review provisional movement, size, selection, regen cadence, weapon timing and unit limit; wing animation and EaW animated shader effects are outside the static conversion.
- Acceptance plan: [[TODOs/Features/V19Torrent_Import]].
