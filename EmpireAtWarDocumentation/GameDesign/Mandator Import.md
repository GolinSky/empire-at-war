# Mandator Import

## Decision
- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE|ALO Model Import — Blender to Unity]].
- User-approved living source: `ReV_Mandator.ALO`; wreck: `ReV_Mandator_D.ALO` (`Mandator_Death_Clone` in RaW XML).
- User-approved provisional squadrons: Delta-7 + A-Wing.
- Length = 2× existing Malevolence, preserving uniform proportions.

## Important Values
- `ShipType.Mandator = 9`; Republic name **Pride of the Core**, role **Mandator II · Dreadnought**.
- User hull / shields / shield regeneration: **9,000 / 8,000 / 100**; regeneration delay **3 s** inherited.
- Local RaW cost / build / population / maximum / level: **70,000 / 90 s / 5 / 1 / 4**.
- Provisional movement: speed **3 units/s**, yaw / acceleration **1.5**, navigation radius **550**, banking **±3°**.
- Flight Y **−520**, using dedicated `MandatorHeightTiersData`; banked hull range **−63.314 .. 63.441**.
- User SSD armor → existing `HeavyCapital` ship class; no separate SSD armor profile exists.

### Existing Unity units — live readback 2026-10-04
| Value | Mandator | Malevolence |
| --- | ---: | ---: |
| Length, project units | 956.803 | 478.402 |
| Hull | 9,000 | 33,420 |
| Shields | 8,000 | 15,000 |
| Shield regeneration | 100 | 100 |
| Weapons / all hardpoints | 56 / 64 | 62 / 73 |
| Speed, units/s | 3 | 4 |
| Flight Y | −520 | −341 |
| Implemented ability | Power to Weapons | Ion Pulse |
- These are saved project values, not canonical statistics. Malevolence assets were not changed by this import.

## Implementation
- 56 weapons + 2 shield generators + 2 hangars + 3 engines + tractor mount = **64 unique targets**, IDs `0..63`.
- `BodyPivot` contains visual, weapon/system targets, shield and hangar; health/fog arrays **64**, weapon array **56**.
- Quad turbolasers: `HP_TL01..08` → `QuadTurboLaser = 20`; twin: `HP_TL09..16`; heavy: `HP_TL17..20`.
- Ions: `HP_IC01..08`; concussion missiles: `HP_MIS01..08`; proton torpedoes: `HP_TRP01..04`.
- Point defense: `HP_LC01..14` + two paired mounts at `HP_LC01/02 + (0,0,−12)`. Local XML lists 14 / 62 total; user's requested 16 / 64 takes precedence.
- Quad profile provisionally copies dual-heavy damage **50**, reload **10 s**, interval **0.3 s**, range **75**, projectile speed **133**, with **8 shots per salvo** and matching audio.
- Source mount bones are retained. Broadside turbolasers/ions use side-specific **170°** coverage; point defense, missiles and torpedoes use full yaw.
- Delta-7 **24 total / 4 active**; A-Wing **18 / 2**. Source starting + reserve counts → total available launches. Initial / interval **4 / 6 s**.
- Launch `(0.273, −71.314, 144.060)` clears the ventral collider; first hangar binds `HP_SPAWN_01`.

## Rules
- FBX scale **0.02**, uniform visual scale **15.624078**, prefab root scale **1**; bow **+Z**, up **+Y**.
- Living bounds **364.015 × 126.627 × 956.803**; damaged **363.459 × 126.627 × 956.804** project units.
- Source hull alpha → linear `Mandator_Hull_TeamMask.png`; `_TeamMaskStrength=1`, hue livery strength **0**. Preserve neutral plating; apply identical mask to wreck hull material.
- Hull albedo/normal/mask retain **4096 × 2048**; windows retain **640 × 128**. Normal maps flip green.
- Hidden planes, collision, shadow and blast helpers remain in FBX; Unity visual renderers disable them.
- Dedicated wreck uses ten opaque `_D` meshes, **9,388 triangles**. Generic intact-hull wreck regeneration would replace this geometry; preserve the dedicated prefab.

## Edge Cases
- `TractorBeam = 7` identifies a destroyable structural mount; immobilization ability and Lucrehulk immunity are pending.
- Existing hangar shuts down when `HP_SPAWN_01` is destroyed; the second hangar is structural. Independent two-hangar behavior is pending.
- Existing ship logic monitors only the first engine. Destroying either shield generator collapses the shared shield. Source multi-system damage semantics are not recreated.
- Static `_D` frame uses current procedural wreck behavior; source ALA death animation, animated EaW shaders and proxy particles are not converted.
- Damaged UVs expose much less painted surface. All eight palettes change the wreck render, with subdued colors on cooled wreck plating.

## Verification
- Blender 3.6.23 round trip preserves living **15 meshes / 9,897 triangles / 105 bones**, damaged **24 / 23,279 / 70**; source triangle corners and UVs checked.
- Maximum bone error **0.000290 / 0.000339** source units; maximum triangle-corner error **0.000641 / 0.000644**, UV tolerance **0.00001**.
- Unity preserves **105 / 70** bones, no parent mismatches; maximum position error **0.00000514 / 0.00001267** project units.
- Unity triangle totals **9,893 / 22,680**: 4 removed from living Cortex (source has 6 zero-area faces), 599 from disabled damaged Shadow. Visible wreck triangle counts are unchanged.
- Saved art/gameplay/wreck/placement prefabs: no missing scripts or broken references. All roster/data/view/icon/placement/address mappings resolve.
- Eight living + wreck owner palettes rendered; transparent **512 × 512** icon inspected. Hull ratio measured on saved assets.
- Import/compilation and post-save Console clean; original ALO SHA-256 values unchanged. Existing dirty MainMenuScene preserved; no Play Mode or automated tests run.

## Files
- Art: `Assets/Art/Models/RepublicShips/Mandator/`, corresponding `Materials` and `Textures/Models/RepublicShips/Mandator/`.
- Prefabs: `Assets/Prefabs/Models/Ships/Mandator.prefab`, `MandatorShipView.prefab`; `Assets/Prefabs/Models/Wrecks/MandatorWreckView.prefab`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/MandatorReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/MandatorShipData.asset`, `MandatorHeightTiersData.asset`, `Wreck/MandatorWreckData.asset`.
- Icon / matchup: `Assets/Art/Textures/Ui/Icons/ShipIcon/MandatorIcon.png`, `Assets/Settings/Data/Tooltip/Matchups/MandatorMatchups.asset`.
- Exporter: `Tools/Blender/export_mandator.py`; reproducibility and conversion choices: `Tools/Blender/README.md`.
- Packed Blender/FBX/PNG/report/preview package: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_Mandator-Converted/`.
- Temporary evidence: `Temp/MandatorImport/`; active follow-up: [[TODOs/Features/Mandator_Import|Mandator Import plan]].

## TODO
- Implement Tractor Beam immobilization and Lucrehulk immunity if required for gameplay parity.
- Review independent engine, shield and two-hangar damage behavior.
- Accept placement, firing arcs, launch clearance, ability, destruction and movement in a clean skirmish; review provisional balance.
