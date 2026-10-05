---
category: Features
status: in-progress
created: 2026-10-05
---
# Stealth Corvette Import

## Goal
- Import `ReV_Stealthship.ALO` and register the Republic IPV-2C Stealth Corvette.
- Add cloaking: enemy rendering/detection/targeting suppressed; weapons disabled; movement and friendly reinforcement vision retained.

## Rules
- Follow [[Architecture/ALO_MODEL_IMPORT_GUIDE]] and [[Architecture/PROJECT_ORGANIZATION]].
- Source ALO/DDS unchanged; no automated tests or Play Mode requested.
- Reference and choices: [[GameDesign/Stealth Corvette Import]].

## Important Values
- Hull/shields/regeneration: `850/900/15` each second; two concussion launchers and two laser cannons; no independently destroyable systems.
- Cost/build/tech/population/limit: `2150/18 s/3/2/1`; RaW starbase requirement `2`.
- Source cloak timing: active `80 s`, recharge `10 s`; manual cancellation decloaks.

## Implementation
- Four meshes, `15184` triangles, fifteen bones; binary-verified identity Root restored. Source hashes unchanged; lossless texture conversion.
- Own art/gameplay/wreck/placement/icons, source alpha team mask, four weapon/fog bindings and all registrations saved.
- Cloak rejects enemy presentation/acquisition/cached pursuit and queued firing; friendly movement/vision retained. Already-fired impacts remain valid.
- Geometry/UV/hierarchy, saved references, ability catalog/audio, six matchup icons, dimensions, eight living/wreck palettes and current Unity compilation/import inspected.
- Reports, packed blend/FBX/PNGs/scripts/previews/XML and full credits saved in sibling `ReV_Stealthship-Converted/`; Warbnull mesh/textures/rigging.
- Provisional scale/movement/shared weapon profiles/generic Corvette armor/audio. Separate starbase-level gate and source shader effects unsupported; missing cloak scanline textures recorded.

## TODO
- [x] Read guide; verify local XML, credits and binary identity Root.
- [x] Convert textures and preserve source geometry, attachments and hierarchy through Blender/FBX; record disabled Shadow UV remap in Unity.
- [x] Build Unity art, gameplay, wreck, reinforcement preview and icons.
- [x] Register data, roster, Addressables, icons and matchups.
- [x] Implement cloaking and compile/import/reference/render verification.
- [ ] Authorized clean-skirmish runtime acceptance: spawn/placement, fire/shields, cloak visibility/selection/detection/pursuit, cancellation/expiry/recharge, reinforcement rules, death/wreck.
- [ ] Provisional scale/movement/weapon/armor/audio balance review.
