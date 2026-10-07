---
category: Features
status: done
completed: 2026-10-07
---
# Raider Corvette Import

## Goal

- Add Empire Raider Corvette with hull/shields/speed `600/800/35`, ten non-targetable weapons, no fighters, and Pursuit.
- Run relevant Unity tests, fix related failures, and commit only task changes.

## Decision

- Model: installed AOTR `E_Raider_Corvette` → `Raider_Corvette.ALO`; requested `Temp/ALO_MODEL_MAP.txt` is absent. Catalog and live XML agree.
- User weapon targetability overrides import-guide defaults: hull-only targeting.
- Pursuit: reuse existing stat-modifier behavior after inspecting Assault; self activation must work without an enemy target.
- Damage bonus is user-required; source XML lacks one. Use Assault damage ×2 provisionally; source speed ×2.5, firing delay ×0.5, regeneration ×0, active/recovery `15/60 s`. Keep incoming damage ×1 (no extra user-requested penalty).

## Implementation

- [x] Audit source, convert ALO/FBX, verify geometry/UVs/bones/textures.
- [x] Build own art/gameplay/shield/wreck/placement/icons and complete Empire registrations.
- Source: `4` meshes / `10,301` triangles / `31` bones; FBX corner/UV error `0.00001526/0`, Unity bone error `0.0000001245` units. Seven original hashes unchanged.
- Raider `208`, Pursuit `30`, dedicated weapons `60..62`; existing 2-burst laser `40` reused. Length `24`, radius `15`, regen `8/s`, tech/cost/build/population/cap `3/2,800/56 s/2/10` (project economy/movement/scale provisional).
- Initial arc test reproduced wrong ion/missile cone centers; builder now maps ALO local +X and writes ship-relative yaw limits. Raider tests `8/8` passed after fix.
- FBX/blend retain source hidden collision/shadow meshes; runtime prefab removes their unused rendering components and keeps attachment transforms. Team/shield bindings verified; eight live/wreck palettes and uncropped icons checked.
- [x] Verify Pursuit lifecycle and saved bindings with Unity tests; fix related failures.
- Final targeted Raider tests: `8/8` passed; reviewed suite includes `17/17` ship-ability tests passed. Saved references, geometry, source hashes and eight live/wreck team palettes verified.
- Broader observed EditMode suite: `1,086/1,090`; four failures in unrelated AcclamatorAssault team bindings, global helper cleanup and MC80Independence wreck synchronization. Raider entries are absent from the remaining helper-cleanup report after its runtime helper fix.
- No manual combat Play Mode acceptance performed; source passive debuffs/weapon energy/death animation remain outside the agreed unit specification.
- [x] Review diff, persist assets, commit task changes, move completed plan to Done.
- Outcome: Empire Raider Corvette and Pursuit committed as `Add Empire Raider Corvette with Pursuit`; only task changes included. Related weapon-cone bug fixed; requested automated verification complete.

## Rules

- Read `ALO_MODEL_IMPORT_GUIDE`, placement and UI notes before implementation.
- Preserve unrelated changes and the existing Git index; never modify Obsidian configuration.
