---
category: Features
status: done
created: 2026-10-06
completed: 2026-10-06
---
# Imperial II Star Destroyer

## Goal

- Add the requested Imperial II loadout to Empire without `Advanced` in new unit or asset names.
- Run related Unity tests, fix ship issues and commit only this task's changes.

## Implementation

- [x] Read `ALO_MODEL_IMPORT_GUIDE`, organization/UI rules and model map; verify live source definitions.
- [x] Import 11 Imperial II attachments; reuse hash-verified common hull and triple turrets. FBX roundtrip maximum geometry error `0.000103` source units; bone error `0.00000301`.
- [x] Configure 21,000 hull / 18,000 shields / speed 25; 17 targetable hardpoints, 23 weapons and three engines.
- [x] Add Power to Main Batteries: 33% shot/reload delay, other weapons disabled, shield regeneration zero, speed 25%, duration 20 s / recovery 60 s. Register existing Tractor Beam and TIE Interceptor/Brute/Punisher hangar; each bay launches one squadron plus one replacement.
- [x] Save own shield, preview, wreck, icons, faction/asset/UI/audio registrations. Inspect hull, placement, wreck and eight team renders.
- [x] Fix all 99 team-renderer bindings and broadside arcs; remove inherited `Advanced` names from new wreck materials.
- [x] Final full EditMode suite: 1,066/1,067; all 11 Imperial II/main-battery checks passed. Remaining failure: existing Victory II renderer bindings, outside this import.
- [x] After wreck-material renames: saved-ship suite 8/8; console errors zero. Verify all staged Unity GUID dependencies and new asset names.
- [x] Commit 123 scoped files: `f17fcd22` — `Add Imperial II Star Destroyer to Empire`.
- [x] Team-color follow-up 2026-10-06: neutral hull/turrets and four mirrored foredeck stripes per side, shared with Imperial I Advanced; matching wreck material. Team bindings now 100, fog 57, explosion/wreck pairs 43. Red/blue top/angled ship and wreck renders and all four saved-prefab checks passed; no import/serialization errors. No combat or automated Unity test run for this follow-up.

## Decision

- Source: AOTR `E_Imperial_Star_Destroyer_2_Fighters`, inheriting `T_Imperial_Star_Destroyer_2`.
- Common hull `Empire_Imperial_SD.ALO` matches existing conversion SHA-256 `2e7537437ef1c86a5b3116f370c96ab52f343319e4cbe626cdbd1d6bfae6922a`.
- Existing user/parallel import changes remain outside this task's commit.
- No manual skirmish or Play Mode run performed; automated checks cover saved configuration, registrations, hangar reserves and main-battery behavior.

## Files

- `Tools/Blender/ImperialII/README.md` — rebuild procedure, source differences and provisional values.
- `Tools/Blender/ImperialII/ImportEvidence.json` — source hashes, 11 model roundtrip reports and test evidence.
