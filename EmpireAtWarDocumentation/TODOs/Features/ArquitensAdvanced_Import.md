---
category: Features
status: in-progress
---
# ArquitensImperialCruiser Import

## Goal

- Import AOTR `ev_arquitens.ALO` with eight separate turret instances and register the Empire ship.
- Preserve source geometry/attachments/visibility; verify saved Unity assets, registration and rendered appearance.
- Complete gameplay acceptance when requested; reference [[GameDesign/ArquitensImperialCruiser Import]].

## Decision

- Separate `ArquitensImperialCruiser = 204`; existing Republic Arquitens remains unchanged.
- Hull/shields/speed `1,300/1,200/30`; eight non-targetable weapons; no targetable systems or fighter complement.
- Installed XML specifies two shots for all eight weapons; raw speed `3.0`. User speed `30` takes precedence.
- Import uses static checks only; no automated Unity tests or Play Mode requested/run by this task.

## Implementation

- `2026-10-06`: hull and two byte-identical turret families converted; visual/gameplay/shield/placement/wreck/icons created and Empire registrations saved.
- Renamed ship identity, roster/tooltip title and asset family to `ArquitensImperialCruiser`; numeric ID `204` and all `57` asset/folder GUIDs retained. Saved geometry/reference checks passed after the rename.
- Weapons `39/40`, source-specific Boost Weapon Power `23`, weapon/ability audio, ship data/view Addressables and faction/HUD/tooltip/placement mappings registered.
- All `51` mesh renderers are explicitly bound to TeamColorView; source collision/shadow visibility remains disabled. Saved coverage check matches the existing asset-test contract.
- Source/FBX UV error `0`; maximum position error `0.00001574` raw units. Unity triangles/bone parents preserved; maximum attachment displacement `0.000000452` units.
- Saved-reference checks: zero missing scripts/broken references. Eight live/wreck palettes and opaque-hull placement/icon renders inspected.
- All `21` source hashes unchanged; compilation passed and MainMenuScene stayed clean before another chat entered Battle Play Mode. Final saved-asset checks passed without changing that session.
- Temporary wreck construction logged Awake during the overlapping Play session; saved wreck bindings passed readback. Construction helpers now reject Play Mode. Editable art, source credits and evidence retained in `output/aotr-empire-units/ArquitensImperialCruiser-Converted/`.

## Edge Cases

- Turbolaser strikecraft exclusion, weapon-energy regeneration and source death ALA are outside current project support.
- Scale/movement/range conversion/reload midpoint/economy/matchups and metallic/smoothness remain provisional; details in reference note.

## TODO

- [x] Read import/placement/UI guides and audit installed source models, textures and XML.
- [x] Convert hull and two turret families; validate FBX geometry/UV/bone round trips.
- [x] Build visual/gameplay/fitted-shield/opaque-placement/wreck assets and actual-model icons.
- [x] Register Empire roster, abilities, weapon/audio profiles, ship data, Addressables and UI/placement mappings.
- [x] Verify saved geometry/references, team palettes and Unity import/compilation; retain credits/evidence and record source differences.
- [ ] Clean-skirmish acceptance: deployment, hull-only targeting/damage, weapon arcs/bursts, ability/shields, visibility/team colors and destruction.
- [ ] Review provisional balance and documented source-to-project differences.
