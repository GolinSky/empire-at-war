# Procursator-class Star Destroyer import

## Decision

- Empire `ShipType.Procursator = 212`; approved stats `6,800` hull / `4,200` shields / `19` speed; cost `5,500`, build `55 s`.
- Source: Workshop `1770851727`, `Empire_Procursator.ALO` plus eleven XML-attached turret ALOs.
- Weapons: three quad heavy turbolasers (`87`), two medium dual turbolasers (`5`), eight light triple turbolasers (`86`), two medium ion cannons (`32`). Counts are mounts, not barrels.
- `ProcursatorQuadHeavyTurbolaser = 87` copies the heavy long-range quad rule with Procursator ownership and green projectiles. Mandator data remains unchanged.
- `ProcursatorLightTripleTurbolaser = 86` uses the existing light turbolaser family with triple mount taxonomy and green projectiles; no new firing logic.
- No hangar bays or `HangarComponent`. Existing `BoostWeaponPower = 5` replaces unsupported Overcharge Primary Battery, as requested. Existing ability settings remain unchanged.
- Provisional project choices: tech `3`, population `4`, maximum count `3`, navigation radius `75`, radar range `900`, weapon HP `750`, engine/shield HP `1,000`, regeneration `7/s` after `3 s`. Donor ISD I movement and effect defaults retained.

## Implementation

- Twelve model round trips preserve geometry, UVs, bone hierarchy and material slots repaired against original ALO binary definitions. Original source files remain unchanged.
- Hull `119.3834`-unit measured Venator reference × `1.06` = `126.5464`-unit target length; root scale `1`, bow `+Z`, up `+Y`. Bounds `80.10595 × 38.138073 × 126.54641`.
- Full ±5° bank envelope `−19.06904 .. 19.82913`; own shield mesh, fitted collision/ion/selection and placement bounds.
- Three heavy attachments `TLT_01..03`; eight light attachments `QLC_01..08`; XML fire sockets mapped in saved verification. Medium/ion sockets `TL_03/04_FP_01`, `TL_01/02_FP_01`; systems `Engines`, `Shield_Generator`.
- All 15 weapons and two systems targetable with unique IDs and explicit health bindings. Heavy/ion arcs centered forward; medium/light arcs centered on their respective ±90° broadside, using runtime ship-relative yaw limits.
- Separate visual/gameplay/placement/wreck prefabs, data, matchups, actual-model transparent icon and silhouette; registrations in existing View/Data Addressables groups, mapping, Empire roster, ship data, HUD/tooltip and reinforcements.
- Source hidden collision/shadow/shield helpers retained in FBX/blends; omitted from gameplay assets. Opaque Ship Lit / two-sided additive effects; source inverse-alpha paint masks, matching wreck masks and eight palettes verified.
- Source ALA animations and animated EaW shader behavior are not recreated. Existing weapon profile choices and inherited movement defaults require separate gameplay balancing.

## Verification

- `53` source meshes, `38,984` triangles, `152` total bones; twelve round trips passed. Maximum Unity source bone error `0.000001568` project units.
- Final saved-asset verifier: `7,385` checks passed, including exact source muzzle positions, all external materials, prefab references and registrations.
- `28` original file hashes unchanged; `34` texture references pixel-identical. Icon `512×512`, transparent crop `(22,140)..(422,366)`; eight distinct live/wreck palettes.
- Procursator EditMode regressions: `6/6` passed, including runtime HealthModel shield-generator destruction, all targetable systems, firing arcs, hull/bank bounds, no hangar and registrations.
- Thirty stationary battle-simulator runs completed: 10/10 wins vs Venator; 10/10 vs Victory II; 4/10 vs MC80. Mean encounter durations 24.760 / 25.525 / 50.950 s. Simulator v1 excludes movement, arcs, abilities and hangar launches; these results are a data smoke check, not balance acceptance.
- Initial broad Weapon run 128/138 passed; Procursator profile ownership/reason and caliber-rule failures corrected. Unrelated failures concerned Balance Editor SensorJammingSettings and Allegiance.
- Unity compile/import clean at final saved verification. Combat Play Mode and full fleet balance acceptance not run.

## Spawn binding fix — 2026-10-14

- Reproduced empty donor injection slots: one live team view + 30 null references. `DynamicEntityInstaller.Install` attempted `RegisterComponent(null)`.
- Saved the injection list as 17 current hardpoints + team-color view. Builder creates this explicit list; verifier rejects null, duplicate or missing target bindings.
- Regression failed before repair, then all six Procursator EditMode tests passed. Actual project/battle/local/AI factory spawning, health, voice, independent state and teardown passed; no new Console errors.
- Reproduce factory check: `python Tools/Blender/Procursator/VerifyFactory.py`, then `unity command run_script --file Temp/ProcursatorCompositionCheck.cs --entry ProcursatorCompositionRunner.Main --timeout_ms 60000 --json` after confirming clean scene state.

## Files

- Rebuild: `Prepare.py` → `Convert.py` → `Stage.py` → `BuildArt.cs` → `BuildGameplay.cs` → `Persist.cs` → `Render.cs` → `Register.cs` → `Verify.cs` → `Package.py`.
- Unity builder command: `unity command run_script --file Tools/Blender/Procursator/<Script>.cs --timeout_ms 60000 --json`; check nested `result.success` before the next dependent step.
- Blender 3.6.23, dedicated port `9891`, `Start.py` / `Call.py` pin that listener. Start a separate hidden Blender process; do not point the converter at a user's working scene.
- Converter repairs material slots using `ISDIRemake/Convert.py` workflow; exporter `export_isd_i.py`. Preparation uses the established binary material/source audit helpers.
- Evidence: `Tools/Blender/Procursator/Evidence/`; editable source archive and previews `output/remake-empire-units/Procursator-Converted/`.
