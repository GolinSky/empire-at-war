---
type: integration-record
updated: 2026-10-12
tags:
  - unity
  - alo
  - rebellion
  - asset-pipeline
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# Assault Frigate Mk II Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.
> Plan: none — historical provenance only; no tracked open work (2026-10-29 review).

## Decision

- Art: Remake 4.0, Workshop `1770851727`; `Rebel_Assault_Frigate_MK2.alo` + 14 XML-linked turret ALOs. Original ALO/DDS/XML hashes unchanged.
- Stats: [[GameDesign/Proposals/Rebel units]] → hull `3,500`, shields `3,000`, speed `31 units/s`, price `3,200 credits`, build `40 s`. Project balance takes precedence over Remake XML values.
- Weapons: `4` medium turbolasers, `2` light quad turbolasers, `4` heavy laser cannons, `2` point-defense lasers; `1` engine + `1` shield generator. All `14` hardpoints targetable; no hangar.
- Power to Shields: regeneration ×`1.6`, speed/damage ×`0.8`, duration `8 s`, recovery `35 s` after expiry. Own ability settings; shared defaults preserved.

## Implementation

- `ShipType.AssaultFrigateMkII = 309`; ability `AssaultFrigateMkIIPowerToShields = 45`; weapon `LightQuadTurbolaser = 82`.
- Hull, 14 separate turret pieces, materials/textures, visual/gameplay/wreck/placement prefabs, ship/wreck data, matchups, icon and silhouette saved.
- Registrations: Rebellion roster, `ShipsData`, `AssetMappingData`, existing View/Data Addressables groups, `ShipUiData`, `TooltipIconData`, `ReinforcementData`, ability catalog/audio, `ShipIconGenerator.MAPPINGS`.
- Source `LC_Quad_01..04` → medium turbolasers; `05..06` → light quads; `07..10` → heavy lasers; `11..12` → point defense. Muzzles: corresponding turret `FP_01`. Systems: `Engines`, `Shield_Generator_01`.
- All 14 turret models remain visible; `LC_Quad_13..14` and source bow battery add no independent gameplay weapons. FBX/source retain authored helpers; game prefabs strip them.
- Ship Lit recolors authored red markings through HSV; static additive engine/window materials. Normal import uses green-channel flip. All eight live/wreck palettes rendered.

## Important Values

- Provisional roster: level `3`, population `3`, maximum count `10`. Geometry: length `100`, bounds `69.5967 × 52.0999 × 100` game units; navigation radius `55`; root scale `1`, bow `+Z`, up `+Y`.
- Banking ±`5°` → vertical range `-28.9115 .. 26.0566`. Shield regeneration `20 / 3 s`; weapon hardpoint health `300`, engine/shield `600`; turn speed/acceleration `6`. These are provisional choices absent from the unit reference.
- Blender `3.6.23`, MCP protocol `13`; `15` FBXs, `63` meshes, `61,078` triangles, `163` bones. Maximum Blender geometry displacement `0.0000381` source units; Unity attachment displacement `0.000000547` units.
- Verification: source material assignments/per-slot visible triangle counts, UVs, bone hierarchy, all 14 turret sockets, gameplay muzzle positions, health/fog/team lists, identity roots, missing-script/broken-reference scan, saved stats/loadout/ability and all icon/preview/data registrations passed.
- Latest Editor compilation check: `EditorUtility.scriptCompilationFailed = false`; transient compile errors from concurrent ability/hangar edits were not part of this import. No import/serialization errors found.

## Edge Cases

- Supplied `Data/Art/Texturesc` does not exist → verified adjacent `Data/Art/Textures` used.
- Editable `.blend` keeps shared-session scenes, source scenes and FBX validation scenes; model FBXs remain separate. Shared Blender session may be changed by another task after conversion; saved files are the conversion evidence.
- No automated Unity tests or Play Mode run. Combat arcs, shield/ability behavior, fog/selection, reinforcement placement and wreck lifecycle remain unverified at runtime.
- ALA animation and animated EaW refraction/proxy/shield effects are not ported.

## Files

- `Tools/Blender/AssaultFrigateMkII/README.md` — workflow, source mapping, provisional values and verification.
- `output/remake-rebel-units/AssaultFrigateMkII-Converted/` — conversion package, source audit, Unity verification and preview renders.
- `Assets/Prefabs/Models/Ships/AssaultFrigateMkIIShipView.prefab` — gameplay; `Assets/Settings/Data/Ship/AssaultFrigateMkIIShipData.asset` — ship values.
- [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] — integration requirements.
