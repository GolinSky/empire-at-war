---
category: Features
status: done
created: 2026-10-12
completed: 2026-10-12
---
# Tector Model Replacement

## Goal

- Replace the existing Tector artwork with Remake 4.0 `Empire_Imperial_SD_Tector.ALO` (Workshop `1770851727`).
- Preserve all current stats, weapons, abilities, identifiers, registrations and core game data.

## Decision

- Use the new hull without turret meshes; no Remake loadout/balance is copied.
- Follow-up: remove all unused legacy Tector FBXs, materials and textures, including unused turret art.
- Keep live hull/shields/speed `8,000/8,000/22`; preserve `25` weapons, `16` targets, `31` IDs/transforms and firing arcs.
- Retain the existing `180`-unit gameplay length, conservative collision/ion/navigation/height values and all prefab/icon GUIDs.

## Implementation

- [x] Read `ALO_MODEL_IMPORT_GUIDE`; inspect Blender, source binary, placement and UI guidance.
- [x] Remove nine inherited normal-map assignments, restore verified source `Root`, pack 30 textures and render four 1920 × 1280 views.
- [x] Capture Unity component/GUID/data baseline and original asset backups.
- [x] Export/import FBX, preserve 31 meshes and 142 source bones; retain exact UVs and lossless texture pixels.
- [x] Replace visual/gameplay/placement/wreck/icon artwork in place; rebuild renderer references and shield hull; fit existing engine particles to source anchors.
- [x] Verify saved assets, unchanged core data, material/normal assignments, references and actual Unity renders.
- [x] Audit legacy dependencies: 88 obsolete assets, zero retained consumers.
- [x] Delete old project assets through Unity; preserve new artwork, prefabs, icons and data.
- [x] Verify removed GUIDs/paths, unchanged retained files and saved references.

## Important Values

- Visible hull: `100.997742 × 47.93703 × 180.000031` project units; root scale `1`, bow `+Z`, up `+Y`.
- FBX round trip: UV error `0`; geometry error `0.000126138`, bone error `0.000063203` source units.
- Passed: `579` unchanged core-data hashes; `46` unchanged component configurations; `31` unchanged hardpoint transforms; `437` checked serialized references; `4` stable prefab GUIDs and stable icon metadata.
- Original ALO + `30` DDS/TGA hashes unchanged; `30` PNG copies preserve RGBA pixels.
- Cleanup: `88` legacy assets + metadata removed (`21` FBXs / `31` materials / `36` textures; `52,999,673` bytes). Zero retained consumers or remaining GUID references; `666` retained file hashes and `437` serialized references passed.
- Unit model: `24` visible hull/effect meshes; wreck/placement: `18` opaque meshes; turret meshes used: `0`.
- Eight living palettes differ; neutral source wreck has no recolorable paint/mask and remains neutral in all eight renders.

## Edge Cases

- Original visual replacement completed 2026-10-12 in commit `6724b8d3`; requested legacy-art cleanup completed on the same local date.

- Helpers and engine-glow planes remain in Blender/FBX and are absent from unit prefabs; source animated shader effects are not recreated.
- User requested waiting for the existing Battle session to finish. Prefabs were applied only after Unity returned to Edit Mode.
- No automated Unity tests or combat Play Mode were run by this replacement. Runtime combat acceptance was outside the requested visual scope.

## Files

- `F:/CodexArtifacts/tector-preview/Tector-preview.blend` and `01-perspective.png`, `02-top.png`, `03-underside.png`, `04-stern.png`.
- `F:/CodexArtifacts/tector-preview/UnityReplacement/` — full audits, backups and Unity previews.
- `Tools/Blender/TectorReplacement/README.md` and `ImportEvidence.json` — durable implementation/verification.
- [[GameDesign/Tector Star Destroyer Import]] — current replacement and initial AOTR integration history.
