---
category: Features
status: in-progress
created: 2026-10-04
---
# Resolute Import

## Goal

- Import `ReV_resolute.ALO` as a dedicated Republic hero ship; preserve the standard Venator.
- Match Venator scale; hull/shields `7,000/5,000`, regeneration `90`, Proton Beam + Concentrate Fire, Yularen command aura.
- Enforce one Resolute at a time per player, including production, reserve and deployed states.

## Rules

- Follow [[Architecture/ALO_MODEL_IMPORT_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]] and UI rules.
- Preserve original ALO/DDS assets and unrelated changes. No automated tests requested or run.
- Open `MainMenuScene` is dirty; scene and unsaved edits preserved. No Play Mode verification performed.

## Decision

- One at a time → replacement allowed after destruction; cancellation or failed production releases the reservation.
- Hangar → inherit current Venator complement: `Delta7`, reserve `6`, active `2`, initial delay `4 s`, launch interval `8 s`.
- Named Shadow Squadron, Blues Brothers, V-19/V-Wing, NTB/Y-Wing and tech-dependent ARC-170 complement → not configured; separate unit content required.
- Command radius `400` project units → matches existing Concentrate Fire command radius.
- Defense bonus → `35%` less incoming damage (`×0.65`); project has no independent armor stat.
- Aura affects allied Republic ships and squadrons; excludes its own source, does not stack, stops when source dies or is ion-disabled.
- Existing weapon profiles supply weapon damage; hero abilities use existing implementations.

## Implementation

- [x] Audit source geometry, bones, helpers, material roles and seven textures.
- [x] Export FBX/PNG and verify Blender round trip; create independent visual/gameplay assets at Venator scale.
- [x] Map source weapon/system attachments; bind shield, banking, hangar, selection, fog and team colors.
- [x] Add ship data, unit registration, placement preview, wreck, rendered icon and tooltip references.
- [x] Implement fleet bonuses: vision `×1.5`, incoming damage `×0.65`, hull `×1.2`, damage `×1.1`, shields `×1.1`, speed `×1.1`.
- [x] Preserve current hull/shield fractions on aura changes; destroyed squadron members remain destroyed.
- [x] Block duplicate human production; AI uses existing roster reservation limit. Production card shows `LIMIT REACHED`.
- [x] Verify persisted assets, references, compilation, bounds, icon, hologram, wreck and eight team palette renders.
- [ ] Verify in a clean skirmish: deployment, beam, Concentrate Fire, aura entry/exit/death, duplicate rejection, replacement and hangar launch.

## Important Values

- Source: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_resolute.ALO`.
- Source skeleton `78` bones; importer removed identity `Root`, exporter restored it after binary verification.
- Blender `4.2.16` importer already uses bone parents; no CHILD_OF conversion required. Guide's older Blender workflow adapted to live importer.
- FBX round trip: `13` meshes, `15,461` total triangles, UV layers retained; `8` visible meshes, `5` authored helpers hidden.
- Unity skeleton: `78` bones, `0` parent mismatches, maximum head error `0.000002106` units.
- Visible dimensions: `54.75552 × 24.73471 × 119.38335` units; length matches donor Venator `119.38336`. Roots scale `1,1,1`.
- Bank `±15°` → shield, collider and ion-field bounds fitted; hull vertical bounds `-14.79685 / 12.83165`.
- Weapons: `8× DBY-827`, `2× dual medium turbolaser`, `4× laser`, `1× proton torpedo`; `18` unique hardpoints including shield, engines, hangar.
- Hero hull `7,000`, shields `5,000`, regen `90`, speed `9`, radar `500`; abilities only `ProtonBeam` and `ConcentrateFire`.
- Roster: `ShipType.Resolute = 8`, Republic level `4`, cost `6,000`, build `10 s`, population `3`, maximum `1`.
- PNG maps: hull/engine/turret albedo, hull/turret normals, window emission, thruster. Normal imports flip green; additive source meshes use additive URP materials.
- Team livery: red hue `0`, range `0.07`, minimum saturation `0.4`; yellow insignia preserved. Shield receives owner palette.
- Icon `512×512`, transparent background; dedicated placement hologram and own wreck.
- Unity `6000.4.7f1`: imports and compilation complete; `0` console errors/warnings, `0` missing scripts or broken prefab references.
- Standard Venator prefab/data files unchanged. Existing Captor/Vulture work and Obsidian configuration untouched.

## Files

- Exporter: `Tools/Blender/export_resolute.py`.
- Model: `Assets/Art/Models/RepublicShips/Resolute/Resolute.fbx`.
- Maps/materials: `Assets/Art/{Textures,Materials}/Models/RepublicShips/Resolute/`.
- Prefabs: `Assets/Prefabs/Models/Ships/{Resolute,ResoluteShipView}.prefab`; `Assets/Prefabs/Ui/Reinforcement/ResoluteReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/ResoluteWreckView.prefab`.
- Data: `Assets/Settings/Data/Ship/ResoluteShipData.asset`, `Ship/Wreck/ResoluteWreckData.asset`, `Tooltip/Matchups/ResoluteMatchups.asset`.
- Aura coordination: `Assets/Scripts/Services/FleetCommand/FleetCommandService.cs`; pure stat rules in `CombatModifiers`.
- Unique production: `PlayerFactionModel`, `FactionService`; availability state in faction UI.
- Addressables: `ResoluteShipView` in `View`; `ResoluteShipData` in `Data`.
- Inspection reports and temporary import scripts: `Temp/ResoluteImport/` (ignored).
- Packed Blender source and Unity preview: `C:/Users/golin/.codex/visualizations/2026/10/04/01a10706-647d-73a0-bc89-3d06945229ed/resolute-preview/{ReV_resolute_UnitySource.blend,Resolute_Unity.png}`.

## TODO

- Runtime acceptance remains unverified; keep plan active until checked.
- Named hero squadrons and tech-dependent hangar complement require a separate content decision.
