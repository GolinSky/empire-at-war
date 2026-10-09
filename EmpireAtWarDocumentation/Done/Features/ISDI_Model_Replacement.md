---
category: Features
status: done
created: 2026-10-09
completed: 2026-10-09
outcome: Remake ISD I visual replacement persisted; requested static
  verification passed.
---
# ISD I Model Replacement

## Goal

- Replace existing `ISDI = 201` visuals with Workshop `1770851727` `Star_Destroyer`; assemble all XML attachments.
- Mark previous ISD I visual assets obsolete; preserve GUIDs and unrelated assets.

## Decision

- Keep current balance and Interceptor/Brute/Punisher bays (user, 2026-10-09).
- Use all 24 source weapon mounts with existing profiles/arcs; retain abilities 21/22. Source differences recorded in [[GameDesign/ISD I Import]] and `Tools/Blender/ISDIRemake/README.md`.

## Implementation

- [x] Read full import/organization/UI guides and inspect existing integration.
- [x] Resolve XML inheritance, 14 attachments, 24 real weapons, DeathClone geometry, original material slots/textures and hashes.
- [x] Convert 16 ALOs with Blender; geometry/UVs/bones/source-visible triangle counts checked. Preserve editable packed files and original shader parameters.
- [x] Archive and label 63 obsolete visual records; retain original GUIDs; repair snapshot references to archived visuals.
- [x] Replace visual/gameplay/preview/wreck geometry; fit shield/collider/banking/hangar volumes. Rebuild 30 targetable hardpoints, ownership/fog/explosion lists and AI loadout.
- [x] Verify existing faction/data/asset/Addressable/UI/icon/preview registrations and weapon/ability/fighter dependencies without duplicates.
- [x] Save/import all assets; final compilation and saved-reference checks passed. All 8 live/wreck palettes, icons and placement screenshots inspected.

## Verification

- Retained hull/shields/speed `4500/4000/15`, regeneration `6.6666665/s`; bays `203/204/205`, reserve 2 / active 1, delays 4/30 s.
- Maximum geometry/bone error `0.000158/0.000099` source units; Unity bone error `0.000002346`; gameplay muzzle error 0.
- 56 source hashes unchanged; 100 decoded texture-reference copies identical. No missing scripts/broken saved references; zero obsolete active visual dependencies.
- 55 helpers stripped; roots 1, hull length `167.997711`. Final import/compilation state ready; no manual Editor save required.
- Replacement implementation and requested static verification complete. Combat Play Mode/automated tests unrun per guide; runtime acceptance and animated-source-effect limitations remain recorded in the shared reference.

## Files

- [[GameDesign/ISD I Import]]; [[GameDesign/ISD I Import - AOTR History]].
- `Tools/Blender/ISDIRemake/README.md`, `Evidence/`, `Previews/`.
- `output/remake-empire-units/ISDI-Converted/` — editable conversion package.
