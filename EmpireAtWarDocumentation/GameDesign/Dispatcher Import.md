---
type: reference
updated: 2026-10-05
---
# Dispatcher-class Frigate Import

## Decision

- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; acceptance: [[TODOs/Features/Dispatcher_Import]].
- Unit: CIS Dispatcher-class Frigate, formerly Techno Union Frigate. `Tecno Destroyer` remains source provenance, not another roster unit.
- Size: user requested existing Munificent length → `82.56805` project units.
- Source ALOs: `CIS_TecnoDestroyer.ALO` and `CIS_TecnoDestroyer_D.ALO`; static damaged model supplies the wreck.
- Armor: existing `ShipClass.Frigate`; project has no separate Assault Frigate damage profile.

## Important Values

- User values: hull/shields/regeneration `3,500/1,000/50 per second`; cost/build/population/tech `3,400/33 s/3/2`.
- User source: [RaW Dispatcher-class Frigate](https://republicatwar.wiki.gg/wiki/Dispatcher-class_Frigate). Direct fetch returned HTTP 403; current balance was not independently confirmed.
- Legacy only: XML speed `2.5`; Assault duration/cooldown `7/50 s`; no legacy multiplier copied into shared abilities.
- Listed counts: `4` twin turbolasers + `7` turbolasers + `2` lasers + `4` PD + `3` ions = `20` weapons; shield + engines → `22` targets. Supplied prose says 22 weapons, but its explicit sum is 20.
- Existing ability: `ShipAbilityId.BoostWeaponPower = 5`, displayed as Power to Weapons. No hangar.
- Provisional movement: speed `66 units/s` vs Munificent `54`; yaw/turn acceleration `45`; bank `20°`; navigation radius `42`; max count `10`.
- Provisional hardpoint health: weapons `250`, PD `150`, engines/shield `400`; shared weapon/audio profiles reused.
- Centered hull: `55.29128 × 32.17981 × 82.56805`; bow `+Z`, up `+Y`, roots scale `1`; banked vertical limits `−16.08990 .. +16.09021`.

## Implementation

- `ShipType.Dispatcher = 108`; CIS roster, ship data/view mapping, existing View/Data Addressables groups, reinforcement preview, icon/HUD/tooltip and matchups registered.
- Twin batteries: `TurboY01/04` port and `TurboY03/05` starboard plus `1.2` project units upward. Seven turbolasers preserve `TurboY01..07`; front `02`, port `01/04/06`, starboard `03/05/07`.
- Lasers: `LaserY04/05`; PD port/starboard: `LaserY02/03`; front PD: `Ion02 + (0,3.5,0)`; rear PD: midpoint `LaserY04/05`, parent yaw `180°`, arc `±60°`.
- Ions: `Ion02` front, `Ion01` port, `Ion03` starboard. Systems: `Shield_00` and `Engines_00`; engine VFX: `PE_JEDICRUISER_SML`.
- Added twin/front/rear PD positions are project mount choices; no additional matching source hardpoints exist. Source bones remain unchanged.
- Health/fog lists `22`; weapon list `20`; unique IDs `0..21`; every target follows the banking body. Placement uses the same geometry/size and existing hologram material.
- Living/wreck livery hue/range/min saturation/strength `0.55/0.065/0.25/1`; blue trim recolors, neutral plating stays unchanged. Eight living + eight wreck palettes rendered; blue/green inspected.
- Transparent icon/silhouette `512 × 512`; alpha bounds `(43,48)..(469,464)`, uncropped. Three icon consumers resolve the actual-model sprite.

## Rules

- Source directory: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Tecno Destroyer`; all five supplied files retain original SHA-256 hashes.
- Source pack omits `bluethruster.dds`; exact named RaW texture resolved from `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Textures/`. Its hash also remains unchanged.
- Credits: model `Evillejedi`; textures `Evillejedi`, modified by `Nawrocki`; rig `Nomada_Firefox`. Full source pack README retained in the conversion package.
- Use separate Blender `3.6.23` background processes; shared C9979 file/session and Blender/MCP preferences preserved.
- Restore only source-verified identity `Root`: intact `43` bones; wreck `34`. Case-sensitive and duplicate attachment names survive.

## Edge Cases

- Blender source → FBX: intact `7 meshes / 6,411 triangles`, wreck `2 / 4,551`; maximum bone/geometry displacement `0.000068665 / 0.000048852` source units.
- Unity: intact `6,407` triangles; wreck `4,547`. Exactly four zero-area faces removed from each; every other position/UV triangle survives, corner error ≤`0.000001395` project units.
- Unity maximum bone displacement `0.000001198` project units; no bone parent mismatches. Unity folds the armature container into the model root.
- DDS→PNG pixels match exactly: hull/normal `1024 × 1024`; thruster `256 × 256`. Normal import flips green; disabled `Collision`, `Shadow`, `Motores` stay disabled.
- Saved prefab inspection: root scale `1`, matching centered visual/gameplay/placement/wreck bounds, no missing scripts/broken references/donor model dependencies; all five weapon profiles have existing audio.
- Import/compilation checks passed. Shared Editor Console contained unrelated UI teardown exceptions and test warnings from concurrent work; no import/serialization error identified for Dispatcher. This task started no automated tests or Play Mode.
- Source death ALA, original EaW light/proxy animations and legacy Assault behavior are not converted.

## Files

- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/SeparatistShips/Dispatcher/`.
- Prefabs: `Assets/Prefabs/Models/Ships/Dispatcher.prefab`, `DispatcherShipView.prefab`; `Models/Wrecks/DispatcherWreckView.prefab`; `Ui/Reinforcement/DispatcherReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/DispatcherShipData.asset`; `Ship/Wreck/DispatcherWreckData.asset`; `Tooltip/Matchups/DispatcherMatchups.asset`.
- Conversion: `Tools/Blender/prepare_dispatcher_textures.py`, `export_dispatcher.py`; sibling `Tecno Destroyer-Converted/` includes packed blends, FBXs, textures, scripts, reports, previews and credits.

## TODO

- Verify firing arcs, movement, Power to Weapons, shield/engine damage, placement, selection/fog and wreck transition in a clean skirmish.
- Review provisional movement, weapon/system health, limit and inherited matchup categories against project balance.
