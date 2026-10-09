---
category: Features
status: done
completed: 2026-10-09
---
# Imperial Venator Import

## Goal

- Integrate Workshop `1770851727` unit `Venator_Empire` into Empire; preserve Republic Venator assets and registrations.
- Copy Republic Venator hardpoints; replace DBY-827 with existing Imperial green turbolasers.

## Decision

- User approved tech-2 TIE Fighter + TIE Bomber; omit unsupported Gozanti transport.
- Own `ImperialVenator=211`, ability `ImperialVenatorIntensifyFirepower=35`; 15 weapons / 18 targets.
- Keep source ALO/DDS unchanged. Imperial hull/bridge textures and ten shared turret references verified from XML/binary data.
- Use existing unit/view/presenter consumers; no new UI behavior. Source/project balance differences recorded in [[GameDesign/Imperial Venator Import]].

## Implementation

- [x] Exact XML inheritance, Imperial model/textures, ten inherited attachments and death clone resolved.
- [x] Twelve Blender conversions; binary materials repaired, geometry/UV/bone hierarchy round trips verified. Editable blends/FBXs/reports packed with source objects/helpers preserved.
- [x] Own Unity art/gameplay prefab, copied arcs and ten green replacements, unique target IDs/health, approved TIE hangar, Intensify Firepower/audio, fitted shield and wreck.
- [x] Empire-only roster; Addressables/data/asset mappings; own hologram placement, icons, tooltip/HUD/faction icon consumers and future icon generator.
- [x] Saved references/import/compilation, geometry/materials, source/donor preservation and screenshots verified; inherited selection-canvas scale corrected.
- [x] Mappings recorded; completed plan/backlog moved to Done and links read back.

## Engine Follow-up

- [x] Restore engine glow visibility with two-sided URP Unlit/additive mesh materials and HDR blue output; source shells and all engine triangles retained. Rear/upper/lower views with/without glow saved.
- [x] Engine geometry/materials and refreshed evidence verified; scoped integration/engine commit created (392 files). Archive includes twelve editable source blends/FBXs and source textures; Git LFS tracks the ZIP.

## Verification

- `Verify.cs`: 1,579 saved-asset checks passed; 15 weapons / 18 targets / two bays; authored muzzle/system displacement `0`.
- Twelve Unity imports match mesh/triangle/UV/submesh/parent evidence; maximum bone displacement `0.0000039321003` Unity units. Blender differences <`0.001` source units.
- All 34 audited source hashes unchanged; six Republic records and five donor assets/metas match Git HEAD. Existing Republic registrations retained.
- Zero remaining strippable helpers; real shield retained. Gameplay/preview roots at identity; collision/banking/launch/selection bounds and explicit renderer references checked.
- `EditorUtility.scriptCompilationFailed=false`; final Console interval has no new errors. Earlier unrelated Balance/ISD errors and resolved squadron compile errors retained.
- Top/stern, eight living/wreck palettes and placement screenshots in `output/ImperialVenator/Previews/`; transparent icon/silhouette imported. Engine rear/upper/lower glow/opaque views confirm all ten exhausts and complete `540/540/1,400` engine triangles. Mesh glow uses two-sided URP Unlit; static source texture, HDR blue RGB ×2.
- No automated Unity suite or combat Play Mode run. Runtime combat/hangar/ability/placement/death acceptance and numeric balance remain untested/provisional; required import and saved-asset verification complete.

## Files

- [[GameDesign/Imperial Venator Import]] — source facts, mappings and verification limits.
- `Tools/Blender/ImperialVenator/README.md`, `Evidence.json` — reproducible model-specific workflow and evidence.
- `Assets/Prefabs/Models/Ships/ImperialVenatorShipView.prefab`, `Assets/Settings/Data/Ship/ImperialVenatorShipData.asset` — saved gameplay assets.
- `output/ImperialVenator/ImperialVenator-Converted.zip` — twelve editable blends/FBXs, textures, reports and screenshots; CRC/SHA-256 recorded.
