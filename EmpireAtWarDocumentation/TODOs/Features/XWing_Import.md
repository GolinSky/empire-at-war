---
category: Features
status: in-progress
updated: 2026-10-05
---
# X-Wing Import

## Goal

- Integrate the vanilla T-65 X-Wing from `RV_XWING.ALO` as a five-craft Rebel squadron.
- Preserve animated S-Foils and source art; register gameplay and UI assets.

## Implementation

- [x] Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], organization/UI rules and live squadron architecture.
- [x] Audit ALO/ALA/DDS; preserve source hashes; export editable packed blend, FBX and lossless textures.
- [x] Preserve 14 bones / 10 meshes / 2,639 triangles; visible hull 1,732 triangles and matching UVs.
- [x] Create geometry-only visual, five-craft gameplay, placement, data, 20 cannon bindings and 20 engine trails.
- [x] Add animated Lock S-Foils toggle: speed ×1.3, fire delay ×3, regeneration ×3; normal configuration restored on second press.
- [x] Register Rebel roster, view/data Addressables, mapping, icons, tooltips, matchups, weapon and audio keys.
- [x] Save/import assets; inspect three prefabs and all registrations; verify six animation poses and eight team palettes.
- [x] Record source/package credits and [[GameDesign/X-Wing Import|integration reference]].
- [ ] Clean-scene in-game acceptance: buy/deploy, fire all four cannons per living member, close/reopen wings, ion disable/teardown, fog/selection and member death.
- [ ] Confirm provisional movement/range/size/economy against project pacing.

## Important Values

- Hull/shields/regeneration: 60/20/3 per fighter; five fighters, four lasers each; no destructible weapon systems.
- Source speed/min/turn/range: 4/2.5/3/450. Provisional project combat/cruise/turn/range: 32/20/110°/s/45 units.
- Closed wings: speed ×1.3, weapon delay ×3, regeneration ×3 → 9/s; indefinite toggle, no cooldown.
- Tactical economy: 500 credits / 15 s / tech 1 / population 1; queue limit 10 provisional.

## Rules

- No automated tests or Play Mode unless explicitly requested; none executed in this import.
- Open `MainMenuScene` is dirty; preserve user scene state.
- Original ALO/ALA/DDS hashes unchanged. Disabled low-detail hull UV merge ≤0.000344 is recorded; source blend/FBX retain original UVs.

## Decision

- Chosen: existing fighter pacing and weapon-range scale after optional scale clarification received no reply.
- Why: EaW values do not establish Unity unit/angle conversions.
- Avoid: treating the source raw values as verified Unity measurements.

## Files

- [[GameDesign/X-Wing Import]]; `output/eaw-rebel-ships/XWing-Converted/`.
- `Assets/Prefabs/Models/Squadrons/XWingSquadronView.prefab`; `Assets/Settings/Data/Squadron/XWingSquadronData.asset`.
- Reports: `GeometryVerification.json`, `AssetVerification.json`, `RegistrationVerification.json`, `TextureVerification.json` in the converted package.

## TODO

- Script compilation clean; no new console import/serialization errors. Three prefabs: zero missing scripts/broken references.
- Geometry: 10 meshes / 2,639 triangles; animated attachment error ≤0.000255 units; rigid section error 0.
- Gameplay/placement: 25 visible sections / 8,660 triangles; five health/flight members, 20 unique muzzles, five animations.
- Acceptance remains active; no runtime success claim.
