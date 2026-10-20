---
category: Features
status: in-progress
created: 2026-10-14
---
# Empire Fleet Import

## Goal

- Import Procursator, Allegiance, Titan and Tartan for the Galactic Empire.
- Use [[GameDesign/Proposals/Empire Fleet Import]] for proposed balance, exact ALO paths, weapons, abilities and source links.

## Rules

- Source: Empire at War Remake 4.0, Workshop `1770851727`.
- This plan records future work; no model conversion, asset configuration or gameplay verification was performed when recording it.
- Weapon counts describe mounts, not barrels; proposed layouts require inspection of actual ALO geometry and transforms.
- Relative lengths use Venator = 1,137 m / 1.00×; do not apply ratios blindly as `transform.localScale`.
- Titan length 4,500 m / 3.96× and no-hangar configuration remain provisional.
- Allegiance defaults to no hangar despite the selected model's `Hangar` filename; inspect geometry before choosing a mod variant.
- Verify source usage rights before distributing converted assets.

## Files

- [[GameDesign/Proposals/Empire Fleet Import]] — supplied specifications and sources.
- [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] — conversion workflow.
- [[Architecture/PROJECT_ORGANIZATION]] — asset placement.

## TODO

- [x] Procursator — imported 2026-10-14; 12 models, 15 weapons/17 targets, no hangar, existing Power to Weapons. Saved checks 7,385; Procursator EditMode 6/6 and actual local/AI factory spawning passed after injection-list repair; 30 stationary simulation encounters. Combat Play Mode/balance acceptance remains unrun. Rebuild/evidence: `Tools/Blender/Procursator/`.
- [x] Allegiance — completed 2026-10-14 as Empire `213`; `20,000/16,000/12`, 21 weapons + 7 subsystems, zero squadron bays, existing Power to Main Batteries + Tractor Beam. Geometry/UV verified; Allegiance EditMode tests `12/12` passed, including the repaired spawn-injection bindings. Reference: [[AssetPipeline/Imports/Allegiance Import]].
- [x] Titan — completed 2026-10-14 as Empire `214`; `28,000/18,000/16`, 24 weapons + 9 subsystems, zero squadron bays, existing Power to Weapons + Tractor Beam. Saved verification 52,482 checks; Titan EditMode `12/12`, three capital simulations and actual local/AI scope composition passed. Reference: [[AssetPipeline/Imports/Titan Import]]; commit `c80046172`.
- [x] Tartan — completed 2026-10-14; Empire `215`, `650/850/70`, 9 targetable weapons, no engine/shield-generator/hangar subsystem or squadrons. Existing Power to Weapons `20`; geometry/source hashes/saved references and eight live/wreck palettes passed; final EditMode `9/9`, 30 capital-opponent simulations completed. Commit `58bb0d846`.
- [ ] Verify supplied comparison stats against current Unity assets before balance tuning.
- [ ] Balance weapon DPS, range, accuracy and tracking separately from mount counts.

### Per-ship checklist

- [ ] Convert ALO to FBX using Blender.
- [ ] Verify imported material assignments.
- [ ] Identify actual mesh bounds and orientation.
- [ ] Compare model length with the imported Venator.
- [ ] Apply the target relative size modifier.
- [ ] Inspect turret meshes and hardpoint positions.
- [ ] Match gameplay weapon mounts to visual geometry.
- [ ] Place engine and shield-generator hardpoints.
- [ ] Validate hangar geometry where applicable.
- [ ] Configure weapon firing arcs.
- [ ] Configure ship stats and abilities.
- [ ] Configure faction and build menu.
- [ ] Create Unity prefab.
- [ ] Test scale and collision.
- [ ] Test weapon targeting and projectile origins.
- [ ] Test subsystem destruction.
- [ ] Test against existing Venator, Victory II and MC80.

## Procursator Progress — 2026-10-14

- Status: Procursator implementation and automated/saved verification complete; scoped commit `e55f4ac8c`. Other fleet units remain outside this request; overall fleet plan remains active.
- Assets: 12 source models (hull + 11 attached turrets), 15 weapons / 17 targetable hardpoints; hull/shields/speed `6,800/4,200/19`.
- Gameplay length `126.5464` units = verified Venator `119.3834` × `1.06`; unit root scale `1`.
- Hangar: zero bays and no injectable `HangarComponent`. Ability: existing `BoostWeaponPower = 5`, user-approved substitute for Overcharge Primary Battery.
- Final verification: 7,395 saved checks and 5/5 Procursator EditMode tests passed after ship-relative yaw, scoped quad profile, documented triple profile and 900-unit radar corrections. Thirty stationary simulation encounters completed (10 each vs Venator, Victory II and MC80); movement, arcs, abilities and hangars excluded by simulator v1.
- Files: `Tools/Blender/Procursator/`; dedicated Empire art, view/data, placement, wreck and icons.

## Allegiance Progress — 2026-10-14

- Assets: selected Remake hangar hull + 53 source turret models; binary shader/material assignments restored before FBX export. Original sources unchanged.
- Geometry: 54 ALO→FBX models; triangle counts/bones/parents/bounds checked. Position error `<0.0002` source units; UV error `0`.
- Gameplay: ship `213`; 21 targetable weapons, 3 engines, 2 shield generators, tractor beam and command array. Squadron bays `0`; no `HangarComponent`.
- Requested values: hull/shields/speed `20,000/16,000/12`; price/build time `9,200/85 s`. Existing Power to Main Batteries `24` substitutes for the unavailable class-restricted Concentrated Fire; existing Tractor Beam `22` retained.
- Scale: Venator visible hull `119.38` units × `2200/1137` → Allegiance length `230.99` units. Width/height `141.93/62.62`; bank `±5°`; navigation radius `141`.
- Placement adapts the proposed centerline/trench grouping to actual Remake sockets. `HardpointMapping.json` records all projectile origins. Weapon types/counts remain `3×3`, `20×6`, `5×6`, `32×6`; nine heavy mounts marked main batteries.
- Ten direct verification checks passed, including independent subsystem destruction and simulations against Venator/Victory II/MC80. Half-bearing, 200-unit-distance simulations won in `21.75/25.55/57.55 s`; these do not verify Play Mode navigation or visual turret tracking.
- Verified model icon, separate hologram/wreck, eight live/wreck team palettes; `95` helper renderers stripped. Source alpha carries no usable livery mask; neutral panels retain palette rim coloring.
- Project choices: level `4`, max count `3`, population `12`, shield regeneration `80/s`, subsystem health `600..1200`; source death animation unsupported by static import.
- Completed: `2026-10-14`; Allegiance EditMode tests `11/11` passed, including scale/collision/muzzle placement, saved references, main batteries, zero squadrons, independent subsystem destruction and three capital simulations. Broader fleet-plan scope remains active.
- Files: `Tools/Blender/Allegiance/`; dedicated Empire art/prefabs/data/icons, `AllegianceShipTests.cs`, [[AssetPipeline/Imports/Allegiance Import]]. Editable package: `output/remake-empire-units/Allegiance-Converted`. Commit: `52cdd8350`.

## Tartan Progress — 2026-10-14

- Completed Tartan scope; commit `58bb0d846`. Fleet plan remains active for unfinished ships and separate balance/Play Mode acceptance.
- Empire ship `215`; hull/shields/speed `650/850/70`, price/build `1,450/28 s`, level `1`. Nine targetable mounts: four large repeating lasers, one heavy laser, four point-defense lasers. No engine/shield-generator/hangar subsystem; zero squadron bays.
- Existing Power to Weapons `20` replaces unavailable Laser Defense. Duration/recovery `10/60 s`; fire rate ×2, speed ×0.75, damage taken ×1.5.
- Source hull + attached heavy turret → 13 meshes/13,228 triangles/32 bones. Visible triangle counts/material slots verified against binary; FBX corner error ≤`0.000002862`, UV error ≤`0.000007868`; 15 unchanged source hashes and lossless PNG copies.
- Gameplay length `26.3852` units = imported Imperial Venator `120` × `250/1137`; bank `±15°`, navigation radius `16`. Original outward repeating-laser directions retained; four point-defense arcs are project adaptations.
- Fixed unit issues: stale donor injection entries replaced with nine hardpoints/team-color view; persistent superstructure team mask added for wreck recoloring; collision regression reads serialized prefab dimensions. Final Tartan EditMode `9/9`; two wreck checks passed in observed broader run. Broader run `1,288/1,308`, 20 failures outside Tartan; final Console clean.
- Ten simulator seeds each against Venator/Victory II/MC80 completed; Tartan lost all 30. Movement/arcs/abilities/hangars/interception excluded by simulator; no combat Play Mode claim. All eight live/wreck palettes, transparent icon, placement and saved references verified.
- Files: `Tools/Blender/Tartan/`, `TartanTests.cs`; editable conversion retained at `output/remake-empire-units/Tartan-Converted/`.

## Titan Progress — 2026-10-14

- Completed Titan import scope; Empire `214`, requested stats/economy, 24 weapons/33 targetable hardpoints, zero squadron bays. Existing Power to Weapons `5` replaces the proposed cloak; Tractor Beam `22` retains shared tuning.
- Source hull + 106 turret attachments converted; original material-slot triangles, geometry/UVs/bones/source hashes verified. Five source-albedo/UV overlays add 724 team-stripe triangles; eight live/wreck palettes and placement/icon inspected.
- Length `472.49353` = measured Venator `119.3834` × `4500/1137`; physical length remains provisional.
- Related fixes: donor injection references replaced with 33 targets/team-color view, all MeshRenderers bound, Mandator-scoped quad tuning copied to Titan profile `88`, radar raised to longest gun `877.49994`.
- Verification: saved checks `52,482`; Titan EditMode `12/12` after reproducing/fixing two integration failures; two Titan wreck checks passed in observed broader run. Three deterministic stationary combat simulations against Venator/Victory II/MC80 passed. Actual project/battle/local/AI scopes spawned Titan and verified health, voice, independent state and teardown.
- Broader pre-fix snapshot: `1,288/1,308`; failures in other ship integrations, balance validation and concurrent Editor timeout logs. No full-suite green claim. Simulator excludes movement/arcs/abilities; combat Play Mode/balance acceptance remains separate fleet work.
- Files: `Tools/Blender/Titan/`, `TitanShipTests.cs`, [[AssetPipeline/Imports/Titan Import]]; editable local package `output/remake-empire-units/Titan-Converted/`.

## Procursator Spawn Fix — 2026-10-14

- Reproduced: `EntityBindings.Injectables` retained 30 null slots after donor hardpoints/hangar deletion → `DynamicEntityInstaller.Install` called `RegisterComponent(null)`.
- Fix: saved 17 current hardpoints + team-color view; builder now rebuilds the explicit injection list. Verifier asserts non-null/unique/full current bindings.
- Verification: regression failed before repair; Procursator EditMode `6/6` passed; `7,385` saved checks passed (fewer slots after cleanup). Actual project/battle/local/AI factory spawning, health, voice, independent state and teardown passed; no new Console errors.
- Files: `ProcursatorShipView.prefab`, `ProcursatorShipTests.cs`, `Tools/Blender/Procursator/{BuildGameplay.cs,RepairInjection.cs,Verify.cs,VerifyFactory.py}`.
- Commit: `1f3afe058` — fix Procursator component injection during factory spawning.

### Allegiance Spawn Repair — 2026-10-14

- Cheat and reinforcement spawning failed in `DynamicEntityInstaller.Install`: donor `EntityBindings.Injectables` retained `30` null hardpoint entries.
- Rebuilt saved prefab bindings: `28` current hardpoints + `TeamColorView`; updated import builder/generator to preserve the repair on rebuild.
- New registration regression failed before repair; Allegiance EditMode `12/12` passed afterward. Read-only entity-prefab audit found no remaining null injectables.

## Fleet Livery Correction — 2026-10-14

- User scope: all four imported Empire ships; small shabby stripes using Acclamator's albedo hue recoloring.
- Removed whole-mesh palette rim/emission tint and blanket masks. Neutral original hull/turret albedos retained; stripe hue `0.968`, range `0.07`, minimum saturation `0.4`.
- Three mirrored worn bands; width `0.9%` of ship length, source UV/detail preserved. Procursator/Allegiance/Titan/Tartan overlay surfaces `2/2/3/2`; triangles `256/168/254/100`. Supersedes earlier Titan five-overlay stripes and Tartan superstructure mask.
- Live/wreck materials, team/fog bindings, hologram placement overlays and icons saved. Original source geometry/albedo hashes unchanged; no gameplay or hangar changes.
- Verification: existing ship EditMode tests `6+12+12+9 = 39/39`; `91,968` saved-reference/material/binding checks passed. Eight live/wreck palettes distinct; blue/green changes affect about `0.3–0.5%` of visible live pixels.
- Rebuild/evidence/current renders: `Tools/Blender/EmpireFleetLivery/`. Commit `42bef84ca`; overall plan remains active for separate balance/combat acceptance.
