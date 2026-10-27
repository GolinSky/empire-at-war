---
type: reference
updated: 2026-10-27
tags:
  - asset-pipeline
  - cis
  - ship-import
---
# Recalcitrant Import

## Decision

- Added `ShipType.Recalcitrant = 113` to CIS; source unit `Recalcitrant_Light_Cruiser`, RaW workshop `1129810972`.
- Hull `2800`, shields `2500`, shared Cruiser regeneration `90 shields/s`, delay `1 s`; source Frigate classification retained.
- Ability: existing self-activated `Power to Weapons` (`TIEAvengerPowerToWeapons`, ID `20`); shared catalog owns effects and timing.
- All weapon calibers Medium. Shared profiles: `DualMediumTurboLaser` (5), `MediumMissileLauncher` (93), `MediumDualIonCannon` (37).
- Armor strings and Felucia prerequisite omitted. Source weapon timings/ranges remain provenance; shared profiles define combat behavior.

## Implementation

- `SeV_Commerce_Guild_Light_Cruiser.ALO` + eight separate `SeV_Commerce_Guild_Light_Cruiser_TL_01..08.ALO` turrets at `TL_01..08`.
- 17 targetable mounts: eight dual turbolasers × `270 HP`, six missile launchers × `300 HP`, dual ion × `575 HP`, engines/shield × `350 HP`.
- Per-mount overrides verified in initialized health model and simulator.
- Registered own view/data, CIS roster, icon consumers, Addressable placement preview, wreck, audio mapping and icon generator entry.
- Blue hull/purple turret markings use existing team livery; engine/window shaders approximated with additive materials.
- Cost `3600`, build `40 s`, station level `3`, population `2`; provisional maximum count `10`.
- Provisional length `100` units, bounds `29.642 × 25.522 × 100`, navigation radius `55`; source speed ×10 → `22 units/s`, turn ×100 → `70°/s`.
- Source yaw widths `160°` turbolasers, `130°` missiles, `35°` ion. Gun bearings follow source muzzles; missile bearings provisionally face outward.

## Edge Cases

- Blender FBX reimport re-tessellates hidden hull `Col`: `9917 → 9916` triangles. Unity preserves original `9917`; runtime helpers excluded.
- Hidden shadow UV quantization maximum `0.00085`; helper-only threshold `0.001`, visible threshold `0.00002`.
- Thin antennae, spines and ion barrels match original ALO-imported source renders.
- Editor dictionary wrappers cache lookups; registration tooling invalidates those caches after serialized row edits.

## Files

- Workflow: [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].
- Tooling/evidence: `Tools/Blender/Recalcitrant/README.md`, `Tools/Blender/Recalcitrant/Evidence/`.
- Gameplay: `Assets/Prefabs/Models/Ships/Separatist/RecalcitrantShipView.prefab`.
- Data: `Assets/Settings/Data/Ship/Separatist/RecalcitrantShipData.asset`.

## Important Values

- Verified `2026-10-27`: saved references, HP, simulator HP, Medium profiles, regeneration, economy and registrations passed.
- Nine FBXs, `51` transport meshes, `161` bones, `31` visible surfaces; all `83,934` baked visible vertices match Unity FBX, maximum displacement `0.00000103` units.
- Icon alpha/crop, source silhouette, placement, wreck and team-color renders inspected; no Unity Console errors after integration.
- Automated Unity tests and Play Mode were not run.
