---
category: Features
status: in-progress
created: 2026-10-04
updated: 2026-10-04
---
# Captor Import

## Goal
- Import `Carrier/CIS_Carrier.ALO` as the CIS Captor-class Carrier using [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Preserve source art; integrate ship assets, data, hardpoints, placement preview, wreck, icon and registrations.

## Implementation
- [x] Convert intact ALO; verify Blender/FBX geometry, UVs, attachments, visibility and bounds.
- [x] Create Unity art, visual/gameplay/placement/wreck prefabs, transparent icon and ship/wreck/matchup data.
- [x] Register `ShipType.Captor = 105` in CIS roster, ship/reinforcement/UI/tooltip mappings and existing Addressables View/Data groups.
- [x] Reload saved assets; verify references, dimensions, attachment hierarchy, owned team colors and post-save import/Console state.
- [x] Replace temporary Belbullab-22 launches with user-selected Vulture/Droid Bomber complement: `11` Vulture + `8` Droid Bomber total squadron launches, `1` active per type (`2` overall), first launch `4 s`, interval `8 s`; saved data, squadron mappings and bindings verified.
- [ ] In-game movement, weapons, hangar, shield, placement and wreck acceptance; provisional balance review. No Play Mode or automated tests run.

## Important Values
- User reference: hull `3,400`, shields `800`, regeneration `50`; cost `3,500`, build `30 s`, population `2`.
- Source Tech `2`; build requirement Level `3+` → project roster AvailableLevel `3`. No distinct source Tech 5 variant registered.
- Systems total **15**, matching the supplied list: `2` turbolasers + `6` lasers + `2` ions + shield + `3` engines + hangar. Supplied total `14` is inconsistent.
- Source complement: Tech 2 `11` Vulture + `8` Droid Bomber; Tech 5 `9` Vulture + `6` Tri-Fighter + `4` Hyena Bomber.
- 2026-10-04 user selected original roles after both assets were imported: Vulture (`SquadronType = 102`) `Reserve = 11`, Droid Bomber (`101`) `Reserve = 8`; two bays, `MaxActive = 1` each (`2` overall). Initial delay `4 s`, shared launch interval `8 s`; capacities/timing are project choices. Reserves count total battle launches, including the first active squadron.
- Component/hangar hardpoint/launch transform are bound. Roster description and converted package reflect the configured complement; saved Unity readback verified type/counts/timing.
- Existing `BoostWeaponPower` supplies Power to Weapons. Original shield/engine tradeoffs and Victory/Frigate armor/shield types are not represented separately by current ship data.
- Provisional: size `65.293 × 44.256 × 140` units; speed `7 units/s`, yaw/acceleration `5`; radius `85`, height `−118`; max count `20`.
- Inherited provisional values: weapon/hangar HP `400`, engine/shield HP `600`; shield regen delay `3 s`, hangar delays `4/8 s`, wreck tuning.
- Bank ±10° → hull range `−22.823 .. 22.128`; launch `(0.015, −7.465, 78)` clears the collider's forward edge by `8` units.

## Rules
### Attachment mapping
- `TurboMR01/02` → turbolasers; `LaserR01..06` → lasers; `LaserR07/08` → ions. ALO's extra turret bones remain preserved.
- `Shield_00` → shield; `Engines_00..02` → three engines; `Spawn_00` → hangar.
- All 15 unique IDs follow `BodyPivot`; health/fog collections = `15`, weapons = `10`; team-colored renderer = hull.

## Edge Cases
- Binary source: `64` bones including identity `Root`; importer produced `63` → verified/restored Root.
- Blender round trip: `7` meshes / `6,438` triangles / `64` bones, all UVs preserved; maximum bone displacement `0.00005928` source units.
- Unity: `7` meshes / `6,426` triangles / `64` bones; maximum bone displacement `0.000001051` units; parent mismatches `0`.
- Unity removed **12 verified zero-area faces**: hull `2`, hidden Motor `6`, hidden MotorSML `4`. MotorSML is an empty retained helper; nonempty meshes have complete UVs.
- Visible meshes `3`, visible triangles `5,221`; wreck uses the one opaque hull mesh (`5,051` triangles). Visual/gameplay/placement/wreck bounds agree; root scale `1`.
- Original hull DDS loaded explicitly. Yellow thruster DDS resolved from adjacent CIS hero pack; missing hangar shield textures use project Shields material. Original ALO effects/death animation are not recreated.
- Livery/wreck: hue `0.67`, range `0.08`, saturation `0.25`, strength `1`; all eight palettes rendered; blue/green inspected.
- Icon `512 × 512`, transparent background verified (`199,768` fully transparent pixels). Geometry-only isolated rendering preserves main scene state.
- Saved prefabs: missing scripts `0`, broken references `0`; correct ship-data/view/preview/icon/Addressables targets loaded. Unity compiled; final save/verification produced no new Console warnings/errors. Historical unrelated warnings remain; a setup-time integer assignment error was corrected and the saved 30 s build time verified.
- Earlier failed icon attempts left three temporary Captor preview scenes; all three closed. Cleanup now runs even if palette restoration fails. Forced finalizer/readback confirmed Captor preview scenes `0`; Console cursor `20` → no new warnings/errors.
- Active `MainMenuScene` remains clean and unchanged. Original source files and unrelated Git changes preserved. No automated tests or Play Mode run.

## Files
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Carrier/`.
- Packed blend, FBX, PNGs, reports, icon/palette previews and source credits: sibling `Carrier-Converted/`.
- Exporter: `Tools/Blender/export_captor.py`; conversion details: `Tools/Blender/README.md`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/SeparatistShips/Captor/`.
- Prefabs: `Assets/Prefabs/Models/Ships/Captor{,ShipView}.prefab`; `Assets/Prefabs/Models/Wrecks/CaptorWreckView.prefab`; `Assets/Prefabs/Ui/Reinforcement/CaptorReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/CaptorShipData.asset`, `Ship/Wreck/CaptorWreckData.asset`, `Tooltip/Matchups/CaptorMatchups.asset`; icon: `Assets/Art/Textures/Ui/Icons/ShipIcon/CaptorIcon.png`.
- Evidence: `Temp/CaptorImport/{report,verification}.json`, copied into converted package. Subsequent complement readback: `Temp/VultureImport/captor-complement.json` → `Carrier-Converted/captor-complement.json`; both data/view mappings resolve.
- Source stats: user-supplied [RaW Captor-class Carrier](https://republicatwar.wiki.gg/wiki/Captor-class_Carrier); wiki fetch returned `403`, no independent live verification.
- Credits: model `Evillejedi`; texture `Evillejedi`, modified by `Nawrocki`; rig `Nomada_Firefox`. Pack README requests contact before public-mod use.
