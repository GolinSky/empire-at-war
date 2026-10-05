---
category: Features
status: in-progress
updated: 2026-10-05
---
# C-9979 Lander Import

## Goal

- Convert `SeV_c9979.ALO` and register the CIS space/skirmish transport using [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Source, decisions and values: [[GameDesign/C-9979 Import]].

## Rules

- User stats: hull/shields/regen `300/20/2`; source speed/turn/thrust `1.5/0.5/0.8`; Transport armor, Corvette layer.
- One medium laser profile: cooldown `5 s`, source range `800`; no destructible hardpoints, hangar or abilities.
- User authorized provisional corvette size/economy/Unity movement tuning. No automated tests without explicit request.

## Implementation

- [x] Audit ALO bones/materials/textures; convert in live Blender `3.6.23` / MCP protocol `13`.
- [x] Restore only verified identity Root; preserve three meshes, `4,182` triangles, eight bones, UVs, four muzzle attachments and hidden collision helper.
- [x] Verify Blender geometry/UV round trip and Unity counts/hierarchy; maximum errors `0.000003053` source geometry / `0.000000090946` Unity bones. Original ALO/three DDS hashes unchanged.
- [x] Create type-first art, centered visual/gameplay/placement/wreck prefabs; saved bounds match `32 × 5.138031 × 12.09517`, root scale `1`, bow `+Z`, up `+Y`.
- [x] Register `ShipType.C9979=107`, `MediumLaser=23`, CIS roster, data/view mappings, existing View/Data Addressables, own placement/icons/matchups and weapon audio.
- [x] Add hull-only targeting/direct hull damage support; saved health subsystem count `0`, weapon mounts `1`, original four muzzles preserved. No abilities/hangar.
- [x] Render actual-model transparent `512 × 512` icon/silhouette and eight team palettes; inspect blue/green. Alpha bounds `(54,151)..(462,337)`, no cropping.
- [x] Read saved references: no missing scripts/references or donor dependencies; tooltip/roster/HUD icons match. Current Unity compilation successful; no new import/serialization errors in final Console slice.
- [x] Save packed `.blend`, FBX, PNGs, reports, previews, converter scripts and credits to source sibling `SeV_c9979-Converted/`.
- [ ] Clean-skirmish acceptance: direct weapon firing, hostile target acquisition of the hull-only transport, shields/regen, movement, fog/selection, placement and destruction/wreck.
- [ ] Review provisional economy/movement/size/weapon damage/range and Corvette damage-category substitution.

## Important Values

- Hull/shields/regen: `300/20/2` each `1 s`; one laser shot / `5 s`; provisional damage/range `6/100` units (source `800` → project `100`).
- Economy: tech/cost/build/population/limit `1/500/10 s/1/20`.
- Movement: speed `48`, turn `45°/s`, acceleration `36`, bank `15°`, flight Y `80`, navigation radius `17` (measured `16.33902`).
- Banked hull Y: `−3.298028 .. 5.625829`; selection diameter `36`.

## Edge Cases

- Project lacks Transport armor; uses existing Corvette `ShipClass=2` pending balance review.
- This task ran no automated tests or Play Mode. Other concurrent workflows produced unrelated historical Console errors; they are not this import's verification.
- Plan remains active for runtime acceptance and provisional balance review; do not mark the entire plan done from saved-asset checks.

## Files

- Evidence: `Temp/C9979Import/Output/UnityInspection.json`, `VerificationSummary.json`, `ConversionReport.json`, `SourceAudit.json`.
- Conversion package: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/SeV_c9979-Converted/`.
