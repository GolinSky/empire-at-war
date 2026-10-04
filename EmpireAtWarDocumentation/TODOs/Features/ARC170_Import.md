---
category: Features
status: in-progress
created: 2026-10-04
updated: 2026-10-04
---
# ARC-170 Import

## Goal

- Import `ReV_arc170.ALO` through [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; register a Republic five-craft heavy fighter / fighter-bomber squadron.
- Use user values: tech 5, hull 105 per craft, shields 35, shield regeneration 5, cost 375, population 1.

## Implementation

- [x] Read import, placement and UI rules; inspect live Blender/Unity and existing squadrons.
- [x] Audit source; verify binary 21-bone hierarchy and identity Root; preserve original ALO/DDS hashes.
- [x] Convert packed Blender source, FBX and lossless PNGs; preserve 15 meshes, six visible surfaces and source hidden states.
- [x] Verify Blender round trip: bone names/parents, UVs, visible bounds and geometry; duplicate disabled-flash triangles documented.
- [x] Import Unity art, authored alpha team mask and five external materials; center/scale/orient visual prefab.
- [x] Create five-member squadron with twenty weapons, health/flight references, colliders and own hologram preview.
- [x] Implement one torpedo maximum per fighter/pass through a read-only pilot observer and dedicated launcher/profile; preserve global bomber profiles.
- [x] Register Republic roster, data/view mappings, existing Addressables groups, HUD/tooltip icons and matchups.
- [x] Inspect saved assets, geometry, owned palette renders and Console; retain the dirty MainMenu scene.
- [x] Package Blender source, FBX, PNGs, scripts, reports, hashes, previews and original credits beside the ALO.
- [ ] Implement Lock S-Foils / Astromech Repair through explicit squadron support.
- [ ] Verify battle movement, rear arcs, torpedo pass/cooldown behavior, shields, fog/selection, placement and death on a clean scene.
- [ ] Review provisional movement/weapon balance.

## Decision

- User stats override local XML hull 95 / regeneration 3; local XML confirms shields 35, five craft, cost 375 and build 8 s.
- User torpedo limit overrides local XML two-shot pulses. `FighterProtonTorpedo = 22` uses one shot; `HeavyFighterLaser = 21` uses provisional 8 damage / two shots.
- Existing squadron systems provide health, shields, flight and member explosion/removal death. No separate capital-ship wreck is needed.
- Default import scope tracks Lock S-Foils and Astromech Repair separately; no answer to the optional scope question was received. Neither ability is advertised as implemented.
- No automated tests or Play Mode requested; neither ran. Do not mark runtime acceptance complete from asset inspection.

## Important Values

- `SquadronType.ARC170 = 2`; per craft 105 hull, 35 shields and 5 shield points every 1 s; tech/cost/build/population/max count 5/375/8 s/1/10. Max count and weapon/flight tuning are provisional.
- Blender source 15 meshes / 4,691 triangles / 21 bones; FBX reimport 4,685 triangles. Six duplicate disabled-helper faces collapse; all unique helper geometry/UVs remain.
- Blender bone/corner displacement ≤0.000005722 / 0.000006694 source units; Unity retains 4,691 triangles and matching UVs/parents, bone displacement ≤0.0000001526915 units.
- Member size 7.938759 × 1.267841 × 4; root scale 1, bow +Z/up +Y. Formation radius 18.68598, navigation 22, collider 4.5, selection diameter 44 units.
- Thirty visible meshes per five-craft gameplay/placement prefab; bounds match. No missing scripts, broken references or donor dependencies; registries resolve.
- Transparent model icon 512 × 512, uncropped; all eight team palettes rendered, blue/green inspected. Import/Unity compilation and post-save Console checks clean.

## Files

- [[GameDesign/ARC-170 Import]] — values, conversion decisions, paths and verified limits.
- `Assets/Prefabs/Models/Squadrons/ARC170.prefab`, `ARC170SquadronView.prefab`; `Assets/Settings/Data/Squadron/ARC170SquadronData.asset`.
- `Assets/Prefabs/Ui/Reinforcement/ARC170ReinforcementView.prefab`; `RepublicShips/ARC170` type-first art folders.
- Source `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_arc170.ALO`; sibling `ReV_arc170-Converted/` holds separate deliverables and credits.
- `Tools/Blender/export_arc170.py`, `prepare_arc170_textures.py`, `README.md`; staging evidence `Temp/ARC170Import/`.

## TODO

- Keep this plan active until abilities and runtime acceptance are complete.
- `SquadronPilot` already exceeds 200 lines; consider separating attack-pass coordination before expanding squadron abilities. No broader refactor belongs to this import.
