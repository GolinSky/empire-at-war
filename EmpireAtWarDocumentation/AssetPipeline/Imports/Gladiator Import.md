---
type: engineering-reference
verified: 2026-11-01
system:
  - asset-pipeline
  - ships
acceptance: pending-user
---
# Gladiator Import

## Decision

- Empire `ShipType.Gladiator = 216`; `ShipClass.Capital`; shared capital shield regeneration tier: 180 per 1 s, delay 1 s.
- User values: hull 6,000; shields 5,000; cost 4,000 credits; population 8; build 40 s; starbase level 4. Installed XML economy differs (5,000 / 9 / 25 s / level 2); user values take precedence.
- User loadout override: 4× `DualMediumTurboLaser` (5), 2× `ConcussionMissile` (13), 2× `PointDefense` (14). Current ranges 517.5 / 550 / 200; radar 600. Shared firing patterns replace raw XML pulse/recharge values. Point-defense lasers target strikecraft; these are not separate missile-interceptor components.
- Ten targetable hardpoints: six normal weapons + two point-defense lasers + shield generator + hangar. `healthOverride=0`, `healthWeight=1`; shared type weights 1 / 0.7 / 1.5 / 1.2. Hull budget 6,900 → normal weapon ≈683.168 HP, point defense ≈478.218 HP, shield ≈1,024.752 HP, hangar ≈819.802 HP.
- Point-defense sockets: mirrored source `DTL_02` / `DTL_05`; full 360° yaw. Prior `LC_01/02` and `TL_01/02` mounts now use dual medium turbolasers with their existing source-derived arcs.
- User carrier expansion: all six current Empire squadron types — TIE Fighter (200), TIE Bomber (201), TIE Avenger (202), TIE Interceptor (203), TIE Brute (204), TIE Punisher (205). Each bay: one active, ten total carried; ship totals six active / sixty carried. After one launch per bay: nine reserve each, fifty-four reserve total. Initial/interval delays remain 4 / 8 s.
- User confirmed existing Empire capital movement: speed 25 units/s, turn 7.5. Source 2.5 / 0.15 is not copied directly.
- Gameplay length 90 units, opaque hull length ≈87.564; root identity. Source alpha is unusable for team paint → existing narrow worn ISDI-mask stripes, source albedo unchanged.

## Implementation

- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Art/Models/Empire_Gladiator_Star_Destroyer.ALO`.
- No separate hardpoint model attachments. Actual source bones: `LC_01/02`, `TL_01/02`, `ML_01/02`, `Shield_Gen`, `Hangar`.
- Weapons fire from source muzzle-pair midpoint (current `WeaponHardPoint.Position` API has one origin); laser/turbolaser yaw follows attachment→muzzle vector; missiles face forward.
- Blender MCP 3.6.23; source-slot audit/repair; `.blend` + FBX; original collision/shadow/shield helpers retained in source export and omitted from unit prefabs.
- Main/wreck/preview addresses belong to `EmpireUnitViews`; data stays in `Data`. Dedicated icon, tooltip, roster, audio, data, reinforcement AssetReference and asset mappings saved.
- Existing model-specific tools used. `Tools/AloImport` currently lacks supported Unity apply/render stages; no unified-pipeline completion claim.
- Reusable guidance updated: [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] now describes current hull-derived health rather than removed `hardPointHealth` data.

## Important Values

- Source/FBX: 10 meshes, 18,907 triangles, 49 bones; 7 visible source meshes. Derived team stripes add 88 triangles.
- Round trip maximum geometry error 0.000043589 source units; bone error 0.000027179 source units. Geometry/UV comparison passed in Blender.
- Unity: all 10 mesh triangle/material counts, external remaps, UV channel coverage and 49 bone names/parents/positions passed; max bone error 0.000000745 units. Exact Unity UV-corner numeric comparison was not performed.
- Saved asset/reference/registration checks passed. 15 original-file hashes unchanged; 13 texture references pixel-identical. Eight distinct live/wreck palette renders; icon 512×512, alpha bounds `(33,117)..(413,389)`.
- 2026-11-01 user-authorized automated tests: Ships 128/141; Combat 220/225; Health 342/343; Squadrons 81/81; Economy 75/75; Ui 232/232; Navigation 164/164; Audio 136/137; Core 134/134. Total 1,512/1,532 passed. Remaining 20 failures concern other-ship/shared behavior; no complete-suite pass claim.
- Four parametrized Health cases naming Gladiator passed: selection anchor/marker, explosion hull renderer bindings, wreck mesh scale and wreck material synchronization. Shared audio/catalog checks and model-specific saved-asset verifier passed; no dedicated Gladiator fixture exists.
- Fixed import defects found by tests: selection sizing omitted sprite transparent padding; livery rebuild removed a stripe renderer referenced by `Ship.explosionHullRenderers`. `BuildLivery.cs` now fits/centers the selection marker and refreshes explosion references; `Verify.cs` rejects missing explosion references.
- Evidence: `Tools/Blender/Gladiator/Evidence/AutomatedTests.json`. Audio failure detail was recovered by rerunning `FactionAnnouncementTests`: `UnitLossUsesFiveSecondCameraMemory(5.0f,True)`. Selected EditMode fixtures enter Play Mode internally; manual battle acceptance remains pending.

## Files

- `Tools/Blender/Gladiator/README.md`, `Evidence/` — rebuild flow and verification.
- `Assets/Prefabs/Models/Ships/Empire/Models/Gladiator.prefab` — geometry.
- `Assets/Prefabs/Models/Ships/Empire/View/GladiatorShipView.prefab` — gameplay.
- `Assets/Settings/Data/Ship/Empire/GladiatorShipData.asset` — stats/loadout.
- `output/remake-empire-units/Gladiator-Converted/` — editable conversion, reports and previews.

## TODO

- User in-game acceptance: spawning/reinforcement placement, source-derived arcs, combat balance, hangar launches, hardpoint destruction, shield/fog/selection and death/wreck.
- Source ALA animations and animated EaW shader distortion are not recreated; engine surfaces use static additive materials.

### Loadout revision — 2026-11-01

- Rebuilt/saved the gameplay prefab, hull-derived health bindings, baked loadout, faction description and import scripts for the user-requested 4 dual medium turbolasers + 2 point defenses. Missile launchers remain 2.
- Saved-asset verification passed for 8 weapon mounts, 10 unique target/injection bindings, point-defense type/anchor placement, data counts and registrations. Console check showed only earlier test-run errors; no new import errors.
- Automated tests were not rerun after this loadout revision under the local execution override. The 1,512/1,532 run above is historical evidence for the prior loadout.

### Carrier expansion — 2026-11-01

- Saved six `hangarBays` rows, IDs 200–205, `reserve=10` / `maxActive=1`; reserve counts total available launches and decrements on launch. Existing one physical hangar hardpoint/launch point services all six types.
- Updated faction description, reproducible builder and saved-asset verifier. Saved-asset verification passed; no automated tests rerun for this revision.
