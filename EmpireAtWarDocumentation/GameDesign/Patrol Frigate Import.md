---
type: reference
updated: 2026-10-04
tags:
  - patrol-frigate
  - cis
  - alo
  - asset-pipeline
---
# Patrol Frigate Import

## Goal
- CIS **Patrol Frigate** / anti-fighter corvette, distinct from the existing Recusant destroyer.
- Source folder retains the pack name `Recusant Patrol`; Unity identifier: `ShipType.PatrolFrigate = 106`.

## Decision
- Intact `CIS_Recusant_Patrol.ALO` → static FBX/gameplay ship. Original five files retain their recorded SHA-256 hashes.
- Reuse Thranta corvette configuration and existing model/entity/component flow. Shield hardpoint template comes from Providence; all model-dependent bindings rebuilt.
- Eight `WeaponType.Laser` guns; Power to Engines (`BoostEnginePower`); no hangar.
- Wreck uses the project generator. Supplied damaged ALO and death ALA remain unchanged and were not converted or retargeted.

## Important Values

| Value | Saved result | Provenance |
| --- | ---: | --- |
| Tech / cost / build / population | 1 / 1,500 / 15 s / 1 | User-supplied RaW unit info |
| Hull / shields / regeneration | 900 / 700 / 15 | User-supplied RaW unit info |
| Ship class | Corvette | User role → project class |
| Size, X × Y × Z | 11.37934 × 12.45770 × 81.09705 units | User: 60% of existing Recusant length (135.16174 units) |
| Speed / turn / turn acceleration | 36 units/s / 60°/s / 60 | Thranta tuning; provisional |
| Navigation radius / height | 56.14411 / 80 units | Radius scaled with hull; height retains Thranta tuning |
| Banking / vertical hull limits | ±30° / −6.22884 .. +6.22884 units | Saved geometry measurement |
| Maximum count / regen delay | 20 / 3 s | Thranta tuning; provisional |
| Weapon / engine / shield hardpoint health | 160 / 300 / 300 | Project tuning; provisional |
| Laser damage / range | 6 / 100 units | Existing shared Laser profile |

- User source: [RaW Patrol Frigate](https://republicatwar.wiki.gg/wiki/Patrol_Frigate). Values were supplied in the request; no independent version audit.
- Source armor/shield labels: Corellian Gunboat / Corvette. This import uses the existing shared project `Corvette` class; no distinct RaW armor profile was added.

## Implementation
- Blender 3.6.23 + ALAMO importer over existing Blender MCP; Unity 6000.4.7f1 through official `unity` CLI.
- Nine source meshes; three visible: `FOCAllianceCW1.0`, `LightsHull`, `Gun`. Six authored helpers retained with renderers disabled.
- 33 binary-source bones; restore the omitted identity `Root`. Preserve both `PPTW_PTWSA2` attachments; Blender names the second `PPTW_PTWSA2.001`.
- Source 7,021 triangles → Blender FBX 6,989 → Unity 6,987. Removed 32 zero-area flash faces and two zero-area hull faces; all 6,986 nondegenerate faces retained. Visible Unity geometry: 3,491 triangles.
- Blender round trip preserves nondegenerate triangle positions/UVs and bone names/parents. Maximum bone displacement: 0.0000457764 source units; Unity 0.00000145024 project units before visual scaling.
- FBX import scale 0.02; saved `Geometry.localScale` 699.92596 on all axes. Root scale 1; bow +Z, up +Y; visual/gameplay/placement/wreck bounds agree.
- User resize: length 26 → 81.09705 units, uniform factor 3.119117. Hardpoints and engine effect follow enlarged source sockets; collider, shield, ion bounds, selection, navigation and banked hull limits refitted.
- Hull DDS + normal DDS → PNG. Normal uses linear/data import and green-channel flip, matching ALAMO's normal convention. All five FBX material slots explicitly remapped.
- Eight laser sockets: `TurboR01`, `TurboR02`, `TurboR03`, `TurboR04`, `LaserR01`, `LaserR02`, `LaserR03`, `LaserNR02`. Source `Turbo` labels do not determine gameplay weapon type.
- Side sockets use port/starboard arcs; four centerline guns use ±180° yaw. IDs 0–7 = lasers; 8 = `Engines_00`; 9 = `Shield_00`. All hardpoints follow `BodyPivot`.
- Engine effect uses `PE_JEDICRUISER_MED`; unused source attachments remain available. Shield, collider, ion bounds, selection, fog and team renderer references fit the hull.
- Team livery: hue 0.62, range 0.06, minimum saturation 0.25, strength 1; same settings in the wreck material. All eight owned palettes change 735–839 rendered trim pixels.
- Transparent 512 × 512 icon: 20,203 visible pixels; occupied rectangle X 38..472 / Y 146..365. HUD, faction and tooltip sprite references match.
- Registered `ShipsData`, `AssetMappingData`, existing Addressables View/Data entries, CIS roster, `ShipUiData`, `TooltipIconData`, reinforcement lookup and `ShipIconGenerator.MAPPINGS`.
- Matchups: Fighters/Bombers; vulnerable to Acclamator/Arquitens. All four tooltip icon keys resolve.

## Edge Cases
- `W_LASER_SMALL.dds` is absent; only disabled muzzle helpers reference it. Gameplay uses the shared project Laser effects.
- FBX trims the unnamed source material ` Material` to `Material`; external remap uses Unity's actual name.
- Blender changes a validation object's numeric suffix (`FOCAllianceCW1.0` → `FOCAllianceCW1.001`). Export stores `EaWName` to match geometry independently of that suffix.
- EaW proxy particles, animated shader behavior and source death animation are not recreated.
- The source texture is dark; recoloring preserves its brightness and neutral hull panels.

## Files
- FBX: `Assets/Art/Models/SeparatistShips/PatrolFrigate/PatrolFrigate.fbx`.
- Art: `Assets/Art/Materials/Models/SeparatistShips/PatrolFrigate/`, `Assets/Art/Textures/Models/SeparatistShips/PatrolFrigate/`.
- Visual/gameplay: `Assets/Prefabs/Models/Ships/PatrolFrigate.prefab`, `PatrolFrigateShipView.prefab`.
- Data: `Assets/Settings/Data/Ship/PatrolFrigateShipData.asset`; wreck data: `Ship/Wreck/PatrolFrigateWreckData.asset`.
- Placement/wreck: `Assets/Prefabs/Ui/Reinforcement/PatrolFrigateReinforcementView.prefab`, `Assets/Prefabs/Models/Wrecks/PatrolFrigateWreckView.prefab`.
- Icon/matchups: `Assets/Art/Textures/Ui/Icons/ShipIcon/PatrolFrigateIcon.png`, `Assets/Settings/Data/Tooltip/Matchups/PatrolFrigateMatchups.asset`.
- Exporter: `Tools/Blender/export_patrol_frigate.py`; evidence: `Temp/PatrolFrigateImport/`.
- Separate package: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Recusant Patrol-Converted/` — packed Blender source, FBX, PNGs, icon, palette renders, reports and source credits.
- Pack credits: Evillejedi model; Evillejedi/Nawrocki textures; Nomada_Firefox rigging. Original redistribution terms retained in `Source-Readme.txt`.

## TODO
- Saved asset inspections: no missing scripts/references or old donor-model dependencies; all remaps, registrations, dimensions and palette renders checked; Unity compilation/import completed without new errors.
- No Play Mode or automated tests run. Verify movement, eight laser fire arcs/targeting, Power to Engines, shields, selection/fog, placement and wreck behavior in-game.
- Resize inspection: all four prefabs have length ratio 0.6; zero hardpoint/engine attachment error; fitted volumes and saved references verified. Existing UI teardown Console exceptions predate the resize; MainMenuScene's dirty state preserved.
- Review provisional movement, limits and hardpoint balance. Track acceptance in [[TODOs/Features/Patrol_Frigate_Import]].
