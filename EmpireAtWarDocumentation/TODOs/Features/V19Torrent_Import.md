---
category: Features
status: in-progress
updated: 2026-10-05
---
# V-19 Torrent Import

## Goal

- Register the supplied V-19 Torrent as a five-craft Republic interceptor.
- Track Hunt, runtime acceptance and provisional balance separately from verified static import.

## Implementation

- [x] Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]] and placement/UI rules; audit source ALO/DDS and local RaW XML.
- [x] Convert through Blender 3.6.23; preserve geometry, UVs, bones, attachments and hidden helpers; save editable blends and FBX.
- [x] Build model/materials/textures, five-member gameplay prefab/data, two lasers per fighter and own placement/icons.
- [x] Register Republic squadron `5`, roster, View/Data addresses, asset mappings, HUD/tooltip and reinforcement references.
- [x] Inspect geometry, saved references, owned palettes, icon/formation, imports and compilation; verify original hashes unchanged.
- [ ] Implement Hunt after user chooses to include the new ability; current import leaves it pending.
- [ ] Verify clean-skirmish deployment, firing, shields, fog, selection and member destruction; no automated tests or Play Mode run.
- [ ] Review provisional flight, size, collider/selection, regen cadence, weapon timing and unit limit.

## Important Values

- User values: 5 craft; hull/shields/regen `70/30/3`; two 5-damage lasers; cost/build/tech/population `400/6 s/1/1`.
- Unity geometry: 12 meshes, 24 bones, 3,960 triangles; UV error `0`; maximum corner/bone displacement `0.000003258/0.000002030` project units.
- Visible member size `7.01667 × 3.24540 × 4`; root scale `1`, centered, bow `+Z`.
- Saved assets contain no missing scripts, broken references or donor models; no new import/Console errors. Runtime/balance remain unverified.

## Files

- Reference: [[GameDesign/V-19 Torrent Import]].
- `Assets/Prefabs/Models/Squadrons/V19TorrentSquadronView.prefab`.
- `Assets/Settings/Data/Squadron/V19TorrentSquadronData.asset`.
- Source sibling `ReV_v19_torrent-Converted/` contains editable source and verification evidence.
