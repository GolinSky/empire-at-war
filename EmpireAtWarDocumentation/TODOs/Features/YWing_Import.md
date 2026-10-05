---
category: Features
status: in-progress
created: 2026-10-05
updated: 2026-10-05
---
# Y-Wing Import

## Goal

- Import `ReV_ywing.ALO` through [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; integrate a Republic five-craft BTL-B heavy bomber squadron.
- Use supplied hull/shields/regeneration 60/30/3 per craft, two lasers and one two-shot proton launcher, Ion Shot and passive astromech repair.

## Implementation

- [x] Read import, placement and UI notes; inspect live Blender/Unity and squadron systems.
- [x] Audit ALO/DDS files, identity Root and duplicate engine attachment names; preserve source hashes.
- [x] Save packed Blender source, FBX and lossless PNGs; verify geometry, UVs, bone hierarchy and hidden helpers.
- [x] Import external materials, authored team mask, centered/oriented visual and five-craft gameplay prefab.
- [x] Bind fifteen weapons, health/flight/fog/team references, member colliders and selection; create matching own placement.
- [x] Add squadron ability slots, white torpedo-derived Ion Shot disabling ability and passive survivor hull repair.
- [x] Register Republic roster, data/view mapping, existing Addressables groups, icons, silhouette and matchups.
- [x] Inspect saved references, geometry, eight team palettes, white effect and Unity import/compile results.
- [x] Archive blend, FBX, textures, scripts, source hashes, reports, previews and supplied credits beside the ALO.
- [ ] Verify clean-skirmish movement, weapon passes, ability, repairs, shields, fog/selection, placement and death.
- [ ] Review provisional ability/repair/economy/weapon/flight balance.

## Decision

- Implemented all requested behaviors; unspecified tuning is provisional: Ion disable 3 s, recovery 20 s, range 100; repair 1 hull point/s per survivor.
- No response to the optional tuning question before implementation; these values remain reviewable data choices.
- Cruise/combat 22/25 units/s is slower than current NTB-630 24/27. Tech/cost/build/population/limit 2/500/8 s/1/10 are provisional.
- No automated tests or Play Mode requested; neither ran. Asset/compile/render verification does not establish battle acceptance.
- Keep this plan active until runtime and balance acceptance are satisfied.

## Important Values

- `SquadronType.YWing = 3`; `ShipAbilityId.IonShot = 9`; five fighters, ten lasers, five torpedo launchers, unique weapon IDs 0..14.
- Hull/shields/regeneration per craft 60/30/3; regeneration interval 1 s. Destroyed members never repair or revive.
- Source four meshes / 2,846 triangles / thirteen bones; FBX/Unity 2,814 triangles after duplicate disabled-flash collapse. Visible hull retains 2,786.
- Blender bone/corner error ≤0.000018061 / 0.000027080 source units; Unity bone error ≤0.000000426956 units; no parent mismatches.
- Member size 1.787514 × 0.529589 × 4 units; formation radius including colliders 12.445, navigation 15, selection diameter 30.
- Saved refs/registries and geometry checked; no missing scripts, broken refs or donor dependencies. Existing unrelated compiler warnings remain; no import/serialization errors.
- MainMenu scene stayed clean; source hashes unchanged. Full RaW credits preserved, Y-Wing-specific individual authors unresolved.

## Files

- [[GameDesign/Y-Wing Import]] — exact values, behavior, verification limits and paths.
- `Assets/Prefabs/Models/Squadrons/YWingSquadronView.prefab`; `Assets/Settings/Data/Squadron/YWingSquadronData.asset`.
- Source `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_ywing.ALO`; deliverables in sibling `ReV_ywing-Converted/`.
- `Tools/Blender/export_ywing.py`, `prepare_ywing_textures.py`, `README.md`.

## TODO

- Runtime: verify two-shot salvos and cooldown/pass gate, no hull damage from Ion Shot, overlapping disable cleanup and caster/target death.
- Runtime: verify per-survivor repair caps without reviving dead craft, shields, movement, deployment, targeting, selection/fog and member explosions.
- Resolve individual asset attribution before public redistribution; retain supplied credits.
