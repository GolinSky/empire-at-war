---
type: integration-record
updated: 2026-10-12
tags:
  - endurance
  - rebellion
  - ship-import
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# Endurance Fleet Carrier Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.
> Plan: none (imported before plan tracking). Open user acceptance → [[TODOs/User_Acceptance_Queue]].

## Decision

- Source: Remake 4.0, Workshop `1770851727`, `Endurance_Fleet_Carrier_MP`.
- Balance: [[GameDesign/Proposals/Rebel units]] §9; hull `9,000`, shields `7,000`, speed `18`, price `6,000`, build time `65 s`.
- User-approved squadron substitution: X-Wings only; `6` active + `6` replacement squadrons.
- Keep the fleet-carrier hull, command bridge, control tower and `12` attached turret pieces. Avoid the combat-bridge variant.

## Implementation

- `ShipType.Endurance = 322`; `ShipAbilityId.FighterCoordination = 52`; `HardPointType.CommandArray = 11`.
- Weapons: `4 × MediumTurboLaser`, `2 × MediumIonCannon`, `6 × PointDefense`; every weapon is targetable.
- Systems: `2` engines, `1` shield generator, `4` hangars, `2` tractor beams, `1` command array → `22` targetable hardpoints total.
- Hangar active caps `2/2/1/1`; stored reserves `4/4/2/2`. Initial deployment consumes `6` → `6` replacement squadrons remain.
- Fighter Coordination: allied squadrons within `750` units at activation → accuracy `×1.20`, movement `×1.15`, duration `10 s`, recovery `45 s`.
- Command array destroyed / carrier destroyed / caster ion-disabled → coordination ends. Later arrivals receive no existing effect; overlapping carriers retain one bonus until all effects end.
- Existing Tractor Beam ability uses the two targetable mounts.
- Team color uses authored red paint: `_TeamMaskStrength=0`, `_TeamLiveryStrength=1`, hue `0`, hue range `0.06`, minimum saturation `0.5`; same settings on wreck materials.
- Source inverse alpha covered neutral plating and is disabled. Blue/green comparison changes `9.22%` of live hull pixels and `8.90%` of wreck pixels; neutral hull stays gray.
- Mixed opaque/effect submeshes are separated in gameplay visuals; source FBXs remain complete.

## Important Values

- Visible bounds `93.462 × 43.440 × 180.000` project units; root scale `1`; navigation radius `105`.
- Banked hull range at ±5°: `-24.839 .. 21.727`; launch points lie below the banked hull.
- Project choices: regeneration `11.667/s`, launch delays `4/8 s`, roster level `4`, limit `3`, population `8`, command radius `750`, subsystem health and size.
- Source mod stats are provenance only; project balance follows [[GameDesign/Proposals/Rebel units]].

## Verification

- `15` FBXs converted; maximum geometry error `0.000089055`, bone error `0.000061510` source units.
- `40` source-file hashes unchanged; `52` decoded texture copies pixel-identical.
- `10,197` saved-asset checks passed: meshes/UVs/slots/bones, references, hardpoints, registries, Addressables, preview and icons.
- Unity drops `14` confirmed zero-area hull triangles: `6` in `Hull Primary`, `8` in `hull`.
- Removed `72` helpers / `54,912` triangles from this ship’s game prefabs; FBXs retain helpers and gameplay shield remains.
- Transparent `512 × 512` icon/silhouette; all `8` live and `8` wreck palettes differ; blue/green, wreck and placement inspected.
- Unity imports/compilation complete; Console errors `0`. Focused regression tests added, not run; automated tests and Play Mode require an explicit request under [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].

## Files

- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Art/Models/Endurance_Fleet_Carrier.ALO`; textures in `Data/Art/Textures/`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/Endurance/`.
- Prefabs: `Endurance.prefab`, `EnduranceShipView.prefab`, `EnduranceWreckView.prefab`, `EnduranceReinforcementView.prefab` in their type-first folders.
- Data: `Assets/Settings/Data/Ship/EnduranceShipData.asset`, `Ship/Wreck/EnduranceWreckData.asset`.
- Rebuild recipe, mount mapping and evidence: `Tools/Blender/Endurance/README.md`, `Evidence/`, `Previews/`.
- Editable source pieces: `Temp/EnduranceImport/<Piece>/<Piece>.blend`.

## TODO

- Gameplay acceptance: firing/arcs, all four hangars, coordination/tractor beam, shield, selection/fog and death timing.
- Source ALA/proxy animations are not recreated; project effects and static art are used.
