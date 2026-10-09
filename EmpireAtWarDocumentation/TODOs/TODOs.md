# Project TODOs & Backlog

## Active Tasks

### Features

- [ ] **AI composition analysis and counter measures**
  - **Plan**: [[TODOs/Features/AI_Composition_Counters|AI Composition Counters]]
  - **Status**: 2026-10-07 implemented: baked weapon loadouts, damage-matrix force ratings, utility strategic rules, counter production and squadron escorts. AI/new tests 119/119; full EditMode 1151/1155 (4 unrelated). Commit `e5daa9ab`.
  - **Remaining**: skirmish Play Mode acceptance and threshold tuning against current balance.

- [ ] **Verify MC80 Independence integration acceptance**
  - **Plan**: [[TODOs/Features/MC80Independence_Import|MC80 Independence Import]]
  - **Reference**: [[GameDesign/MC80 Independence Import]]
  - **Status**: 2026-10-07 Rebellion ship `305` integrated with requested `30,000/40,000/17`, 20 targets/22 non-targetable weapons, Power to Shields and approved X-Wing/Y-Wing/A-Wing bays. Model/geometry/render/registration and final saved-reference checks passed. Nine authored helpers removed from game prefabs; wreck uses eight matching hull renderers. Surface fix restored four original Hull texture slots and opaque meshes 1/3; original per-submesh materials, unchanged UVs, source variants and engine/no-glow renders verified. Requested team stripes added through four 2048px linear masks; eight live/wreck material pairs and ownership bindings, blue/red/green top views and eight palettes verified. Before the surface fix, coordinated Executor Editor regression passed all 11 relevant hangar/engine/MC80 wreck checks (project 565/567; two Acclamator failures); no automated tests rerun for the surface fix. No tests or combat Play Mode started by this import.
  - **Remaining**: clean-skirmish acceptance and provisional balance/source-difference review.

- [ ] **Verify BTL-A4 Y-Wing Bomber integration acceptance**
  - **Plan**: [[TODOs/Features/YWingBomber_Import|BTL-A4 Y-Wing Bomber Import]]
  - **Reference**: [[GameDesign/Y-Wing Bomber Import]]
  - **Status**: 2026-10-06 imported as Rebellion-only squadron `301`; six craft, 24 weapons, hull/shields/speed `30/30/30` per bomber, slow-charging anti-laser shields and existing Ion Shot. Own art/placement/icon/team colors, source hashes, geometry/UV/bones, saved references and import/compile checks passed. Republic BTL-B preserved; no automated tests or combat Play Mode started by this task.
  - **Remaining**: clean-skirmish acceptance; provisional economy/weapon/flight balance and source-difference review. Passive AOTR ion-stunner slowdown is not ported.


- [ ] **Verify ArquitensImperialCruiser integration acceptance**
  - **Plan**: [[TODOs/Features/ArquitensAdvanced_Import|ArquitensImperialCruiser Import]]
  - **Reference**: [[GameDesign/ArquitensImperialCruiser Import]]
  - **Status**: 2026-10-06 AOTR Arquitens imported as Empire ship `204`; Republic-aligned hull/shields/speed `2,600/1,400/48`, regeneration `14/s`, cost/build/population/tech/cap `3,500/30 s/2/3/20`. Zero targets/eight weapons, four two-shot light long-range turbolasers + four two-shot laser cannons, Boost Weapon Power, no fighters. Saved tuning/tooltip/live readback passed; zero console errors. Own fitted shield/placement/wreck/icons/team colors, source hashes, geometry/UVs and prior import/compile/render checks verified. No automated Unity tests or Play Mode run by these tasks.
  - **Remaining**: clean-skirmish acceptance and remaining provisional balance/source-difference review.

- [ ] **Verify Victory II integration acceptance**
  - **Plan**: [[TODOs/Features/VictoryII_Import|Victory II Import]]
  - **Reference**: [[GameDesign/Victory II Import]]
  - **Naming**: 2026-10-06 renamed to `VictoryII = 203` across assets, embedded rigs, tooling and links; GUIDs/geometry retained, saved-ship verifier and reinforcement prefab EditMode test passed.
  - **Status**: 2026-10-06 AOTR Victory II imported as Empire ship `203`; `12,000/10,000/20`, 10 targets/18 weapons, medium dual/single turbo-ions, six 3-burst turbolasers, six heavy lasers, Boost Weapon Power, Tractor Beam and TIE-Interceptors. Own placement/wreck/icons/team colors, saved bindings, source hashes, geometry/UVs and import/compile/render checks verified. No automated Unity tests or Play Mode run.
  - **Team-color fix**: 2026-10-06 all-white masks replaced with four mirrored hull stripes; neutral turrets and matching wreck settings. Red/blue Unity renders, saved assets and imports verified; no new errors. Runtime acceptance remains pending.
  - **Remaining**: clean-skirmish acceptance; review provisional balance and documented engine-effect precision/source differences.

- [ ] **Verify Victory I integration acceptance**
  - **Plan**: [[TODOs/Features/VictoryI_Import|Victory I Import]]
  - **Reference**: [[GameDesign/Victory I Import]]
  - **Status**: 2026-10-06 AOTR Victory I imported as Empire ship `202`; `12,000/8,000/175`, 10 targets/16 weapons, 4/6-shot rocket salvos, Full Salvo, Tractor Beam and TIE-Interceptors. Own placement/wreck/icons/team colors, geometry/UVs/source hashes, saved bindings and import/compile/render checks verified. No automated tests or Play Mode run.
  - **Team-color fix**: 2026-10-06 all-white masks replaced with four mirrored hull stripes; neutral turrets and matching wreck settings. Red/blue Unity renders, saved assets and imports verified; no new errors. Runtime acceptance remains pending.
  - **Rename**: 2026-10-07 Advanced suffix removed across assets/code/tools/docs; ID `202` and current `5,500/2,200/24` balance preserved. All 38 moved GUIDs retained; saved bindings and tooling compilation passed; reinforcement prefab test `1/1` passed.
  - **Remaining**: clean-skirmish acceptance and provisional scale/movement/weapon/system/ability/economy/launch balance review.

- [ ] **Verify MC75 Profundity integration acceptance**
  - **Plan**: [[TODOs/Features/MC75_Profundity_Import|MC75 Profundity Import]]
  - **Reference**: [[GameDesign/MC75 Profundity Import]]
  - **Status**: 2026-10-06 imported AOTR `RV_Profundity.ALO` and registered Rebellion ship `304`; hull/shields/speed `18,000/13,000/22.5`, 12 targets/30 weapons, Full Salvo, approved X-Wing/Y-Wing complement, own shield/placement/wreck/icons/team colors. Source hashes, geometry, saved bindings, imports/compilation and renders inspected; no automated tests or Play Mode run.
  - **Remaining**: clean-skirmish hardpoint/combat/launch/ability/destruction acceptance and provisional balance review.

- [ ] **Verify T-65 X-Wing import acceptance**
  - **Plan**: [[TODOs/Features/XWing_Import|X-Wing Import]]
  - **Reference**: [[GameDesign/X-Wing Import|Integration and source verification]]
  - **Status**: 2026-10-05 imported `RV_XWING.ALO`: five craft, 20 lasers, animated S-Foils, Rebel roster/Addressables/UI. Source geometry/textures, six poses, eight team palettes, saved bindings and compilation verified.
  - **Remaining**: clean-scene in-game acceptance and provisional movement/range/balance review. No automated tests or Play Mode run.


- [ ] **Verify Imperial Victory integration acceptance**
  - **Plan**: [[TODOs/Features/Victory_Import|Victory Import]]
  - **Reference**: [[GameDesign/Victory Import]]
  - **Status**: 2026-10-05 Victory imported/registered as Empire ship `200`; supplied stats, six targets and dedicated 20 s / 60 s Boost Weapon Power configured. Vanilla TIEs imported: 2 fighters + 1 bomber active, 4 fighters + 2 bombers replacements; seven/four craft per squadron. Separate purchases enabled: fighters `300 / 10 s / level 1`, bombers `550 / 17 s / level 2`; population `1`, queue limit `10` each. Own placement/wreck/icons/team colors, geometry/source hashes, saved references and Unity import/compile/render checks passed; no tests or Play Mode run.
  - **Remaining**: clean-skirmish acceptance and provisional scale/movement/weapon/system/limit/launch balance review; supplied hull retains Republic roundels.

- [ ] **Verify V-Wing squadron integration acceptance**
  - **Plan**: [[TODOs/Features/VWing_Import|V-Wing Integration]]
  - **Reference**: [[GameDesign/V-Wing Import]]
  - **Status**: 2026-10-05 local OBJ integrated as Republic squadron `6`; five craft/twenty lasers, hull/shields/regen `80/25/3`, cost/build/population `600/15 s/1`. Existing Hunt order and passive repair `1 HP/s`; own placement/icons/team colors, source hashes and saved-reference/import/compile/render checks complete. No automated tests or Play Mode run.
  - **Remaining**: clean-skirmish acceptance and provisional flight/repair/weapon/tech/limit/matchup balance review.

- [ ] **Verify IPV-2C Stealth Corvette integration acceptance**
  - **Plan**: [[TODOs/Features/StealthCorvette_Import|Stealth Corvette Import]]
  - **Reference**: [[GameDesign/Stealth Corvette Import]]
  - **Status**: 2026-10-05 import and Republic ship `11` registered; hull/shields/regen `850/900/15`, two missiles/two lasers, cloak `80 s`/recharge `10 s`. Own placement/wreck/icons/team colors, saved references, source hashes, geometry and Unity import/compile checks complete. No automated tests or Play Mode run.
  - **Cloak follow-up**: 2026-10-06 friendly translucent hull/shield suppression and active countdown display added; existing `80 s` active / `10 s` recovery retained. Compilation, shader/import, saved bindings and mesh previews verified; no automated tests or Play Mode run.
  - **Remaining**: clean-skirmish cloak/combat/placement/destruction acceptance and provisional scale/movement/weapon/armor/audio balance review.

- [ ] **Finish V-19 Torrent integration acceptance**
  - **Plan**: [[TODOs/Features/V19Torrent_Import|V-19 Torrent Import]]
  - **Reference**: [[GameDesign/V-19 Torrent Import]]
  - **Status**: 2026-10-05 static ALO/Blender/FBX import and Republic squadron `5` registrations complete; five craft, ten lasers, hull/shields/regen `70/30/3`, cost/build/tech/population `400/6 s/1/1`. Own placement, icons, authored team mask, geometry/saved-reference/import/compile checks complete; source hashes unchanged. No automated tests or Play Mode run.
  - **Remaining**: Hunt ability decision/implementation, clean-skirmish acceptance and provisional balance review; deployed wing pose is static.

- [ ] **Verify Imperator-class Star Destroyer integration acceptance**
  - **Plan**: [[TODOs/Features/Imperator_Import|Imperator Import]]
  - **Reference**: [[GameDesign/Imperator Import]]
  - **Status**: 2026-10-05 import/static verification complete; Laser Beam, 14 ARC-170 launches / 4 active and 8 Y-Wings / 2 active configured. StarDestroyer2 disabled in Republic build roster. Runtime acceptance and provisional balance review remain.

- [ ] **Verify Dispatcher-class Frigate integration acceptance**
  - **Plan**: [[TODOs/Features/Dispatcher_Import|Dispatcher Import]]
  - **Reference**: [[GameDesign/Dispatcher Import]]
  - **Status**: 2026-10-05 intact/damaged ALOs imported and CIS ship `108` registered; Munificent length 82.56805, hull/shields/regen 3,500/1,000/50, cost/build/population/tech 3,400/33 s/3/2. Twenty weapons + shield/engine, Power to Weapons, dedicated wreck/placement/icons/team colors complete. Saved-reference/geometry/import/compile checks passed; source hashes unchanged. No automated tests or Play Mode started for this import.
  - **Remaining**: clean-skirmish acceptance and provisional movement/weapon/system/limit/matchup balance review; source death ALA remains outside static conversion.

- [ ] **Verify C-9979 Lander integration acceptance**
  - **Plan**: [[TODOs/Features/C9979_Import|C-9979 Import]]
  - **Reference**: [[GameDesign/C-9979 Import]]
  - **Status**: 2026-10-05 static ALO/Blender/FBX import and CIS ship `107` registrations complete; hull/shields/regen `300/20/2`, one medium laser / `5 s`, zero destructible hardpoints. Own wreck/placement/icons/team mask, hull-only target/damage support, source hashes, saved references and Unity import/compile/render checks complete. No tests or Play Mode run by this task.
  - **Remaining**: clean-skirmish acceptance and provisional balance review; project uses Corvette damage category because Transport armor is unavailable.

- [ ] **Verify BTL-B Y-Wing integration acceptance**
  - **Plan**: [[TODOs/Features/YWing_Import|Y-Wing Import]]
  - **Reference**: [[GameDesign/Y-Wing Import]]
  - **Status**: 2026-10-05 imported and registered as Republic squadron `3`; five craft, fifteen weapons, hull/shields/regen 60/30/3 per member, two-shot torpedo salvos, white Ion Shot and passive astromech repair implemented. Art, placement, icons, team colors, saved references, source hashes and Unity import/compile/render checks complete; no automated tests or Play Mode run.
  - **Remaining**: clean-skirmish acceptance and provisional ability/repair/economy/weapon/flight balance review.

- [ ] **Import NTB-630 Naval Bomber**
  - **Plan**: [[TODOs/Features/NTB630_Import|NTB-630 Import]]
  - **Reference**: [[GameDesign/NTB-630 Import]]
  - **Status**: 2026-10-05 static ALO/Blender/FBX import and Republic squadron `4` registrations complete; four bombers, twelve weapons, hull/shields/refresh 60/30/3 per craft, cost/build/population 550/8 s/1. Own placement/icons/team mask, geometry and saved-reference/Unity import checks passed; source hashes unchanged. Separate Blender process; existing Ion Shot `9` enabled and saved/catalog references verified. No tests or Play Mode run.
  - **Remaining**: missing original turret textures (neutral metal used), in-game acceptance and provisional balance review.

- [ ] **Import ARC-170 squadron**
  - **Plan**: [[TODOs/Features/ARC170_Import|ARC-170 Import]]
  - **Reference**: [[GameDesign/ARC-170 Import]]
  - **Status**: 2026-10-04 ALO/Blender/FBX imported and registered as Republic squadron `2`; five craft, twenty weapons, supplied hull/shields/regen 105/35/5 per member and tech/cost/population 5/375/1. One torpedo maximum per fighter/pass implemented. Own placement, transparent icons, authored team mask, saved-reference/geometry and Unity import/compile checks complete; source hashes unchanged. No automated tests or Play Mode run.
  - **Remaining**: Lock S-Foils, Astromech Repair, in-game acceptance and provisional balance review.

- [ ] **Import Pride of the Core / Mandator II**
  - **Plan**: [[TODOs/Features/Mandator_Import|Mandator Import]]
  - **Reference**: [[GameDesign/Mandator Import]]
  - **Status**: 2026-10-04 living ALO + dedicated damaged wreck imported and registered as Republic ship `9`; 56 weapons / 64 targets, hull/shields/regen 9,000/8,000/100, Power to Weapons, approved Delta-7/A-Wing complement. Length 956.803 = 2× Malevolence; flight Y −520. Art/placement/icon/team colors, saved references, geometry/hierarchy and import/compile checks complete; source files unchanged. No automated tests or Play Mode run.
  - **Remaining**: Tractor Beam ability, independent system damage review, clean-skirmish acceptance and provisional balance review.

- [ ] **Import Resolute hero Venator**
  - **Plan**: [[TODOs/Features/Resolute_Import|Resolute Import]]
  - **Status**: dedicated model, registrations, abilities, one-ship rule and Yularen aura implemented; asset/compile/render checks passed. Clean-skirmish acceptance pending; current Venator hangar inherited.

- [ ] **Import CIS Patrol Frigate**
  - **Plan**: [[TODOs/Features/Patrol_Frigate_Import|Patrol Frigate Import]]
  - **Reference**: [[GameDesign/Patrol Frigate Import]]
  - **Status**: 2026-10-04 ALO/FBX imported and registered as CIS ship `106`; eight lasers + engine/shield systems; Power to Engines; hull/shields/regen 900/700/15; cost/build/population 1,500/15 s/1. Wreck, placement, transparent icon, team colors and saved-reference/import checks complete. Resized to 60% of Recusant length: 81.09705 units, with matching attachments, volumes and wreck/preview. No Play Mode or automated tests run.
  - **Remaining**: in-game acceptance and provisional movement/hardpoint balance review.

- [ ] **Import Captor-class Carrier**
  - **Plan**: [[TODOs/Features/Captor_Import|Captor Import]]
  - **Status**: 2026-10-04 Captor imported/registered as CIS ship `105`; 15 listed hardpoints, hull/shields/regen `3,400/800/50`, cost/build/population `3,500/30 s/2`. Art, placement, wreck, transparent icon, team colors and saved-reference checks complete. User-selected complement configured/verified: 11 Vulture + 8 Droid Bomber squadron launches, 1 active per type (2 overall), 4/8 s timing; data/view mappings loaded. No Play Mode or automated tests run.
  - **Remaining**: in-game acceptance, including Vulture/Droid Bomber launches, and provisional balance review.

- [ ] **Finish Malevolence integration acceptance**
  - **Plan**: [[TODOs/Features/Malevolence_Import|Malevolence Import]]
  - **Status**: 2026-10-03 ALO → Blender → FBX → registered Separatist ship; 73 hardpoints, exact hull/shield/population/limit values, Mass Driver bypass and Ion Pulse implemented. Length resized to **3.676× Providence (478.402 units)**; flight **Y = −370**, below Lucrehulk **−258**. Saved references, matching visual/wreck/placement bounds, team-color renders and compilation verified; no automated tests or Play Mode run.
  - **Remaining**: exact Vulture/bomber squadron assets and launches; in-game combat, placement, ability and destruction acceptance; provisional balance review.

- [ ] **Review ship abilities implementation**
  - **Plan**: [[TODOs/Features/Ship_Abilities_Plan|Ship Abilities Plan]]
  - **Status**: 2026-10-02 balance/descriptions updated: Boost Weapon Power = damage ×2 / reload ×0.3 / speed ×1; Assault = damage ×2 / reload ×0.5 / speed ×2; both active 7 s, then recovery 50 s. Unity live readback and saved asset verified; no new console errors; no automated tests or Play Mode run.
  - **Remaining**: implementation review and acceptance evidence; commit `dc869bbb` recorded review as needed.

- [ ] **Implement UI tooltip system**
  - **Plan**: [[TODOs/Features/Tooltip_System_Plan|Tooltip System Plan]]
  - **Research**: [[TODOs/Features/Tooltip_System_Plan - Research|Tooltip coverage catalog and codebase findings]]
  - **Rule**: only `ITooltipService` shows or hides tooltips; views → presenter → service; `UiController` is not bound to the service.
  - **Status**: Phases 0–8 implemented; pure service, saved MPUIKit UI, presenter integrations, and 13 per-unit matchup assets. Compilation and asset-reference inspection clean; sample tooltip visually checked.
  - **Remaining**: in-game acceptance (disabled cards, live refresh/rebind, fog/drag/HUD/route lifecycle, screen edges and teardown). EditMode tests authored; execute only on request.

- [ ] **Close settings audit follow-ups** (Wave 1)
  - **Plan**: [[TODOs/Features/Settings_Audit_Followups|Settings Audit Follow-ups]]; parent [[TODOs/Features/Settings_Implementation_Plan|Settings Implementation Plan]]
  - **Status**: ST1–ST7 implemented; prior Play Mode smoke check done. 2026-10-02: fullscreen Audio / Display / Controls / Camera Penpot design implemented in the prefab, including main-menu styling and selected-tab states. Import, script diagnostics, Inspector references and all four isolated previews checked; 21 bindings render in Controls. Live category/rebind interaction remains unverified.
  - **Slider fix (2026-10-09)**: all seven matching sliders share a prefab/size variant with 28 px bar hit areas; audio 1%, camera 0.05×, credits $100. Related EditMode checks 21/21; compilation and isolated render verified.
  - **Remaining**: remaining settings core EditMode tests; live category/rebind interaction; manual restart-persistence, display Keep/Revert/timeout, and corrupt-file checks.

### Bugs

- [ ] **Fix scene-teardown pool errors and material leaks** (Wave 1)
  - **Plan**: [[TODOs/Bugs/Scene_Teardown_And_Material_Leaks|Scene Teardown and Material Leaks]]
  - **Status**: steps 1–5 in `38f3e681`; compile clean. **Remaining**: manual skirmish exit check and placement material-count check.

- [ ] **Replace silent null returns with fail-fast/Try APIs** (Wave 1)
  - **Plan**: [[TODOs/Bugs/Fail_Fast_Null_Returns|Fail-Fast Null Returns]]
  - **Status**: code + tests in `32c5f1a1`. **Remaining**: clean compile (blocked by other Wave 1 WIP) and manual skirmish check.

### Optimization

- [ ] **Optimize battle particle rendering**
  - **Plan**: [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan|Particle rendering optimization]]
  - **Research**: [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|2026-09-30 capture and prefab analysis]]
  - **Status**: Profiler export repaired; 2 regression tests passed; original recording recovered; 65 prefabs / 111 particle systems inspected. Jobs review confirms immediate completion, main-thread flood execution and per-Plan grid rebuilds.
  - **Remaining**: compare ~200 FPS battle start with ~12 FPS mid-battle; prioritize ship/navigation CPU growth and accumulating combat effects. Star reduction deferred pending isolation.

- [ ] **Optimize battle attacks and projectile reuse**
  - **Plan**: [[TODOs/Optimization/Battle_Attack_Optimization_Plan|Phased attack, pooling, Jobs + Burst, and instancing plan]]
  - **Execution**: Complete and analyze each phase before advancing; start with readable attack states and busy/cancellation fixes.
  - **Remaining**: manual parity, matched performance captures, measured Jobs thresholds, and actual instanced-draw evidence.

### Refactoring

- [ ] **Fix UI Service (Decouple Gameplay Services from UI)**
  - **Task**: Move UI creation, UI prefab instantiation, and `IUiService` dependencies out of gameplay services into UI prefabs / `<Feature>UiController` presenters.
  - **Details**: See [[TODOs/Refactoring/UI_Service_Refactoring|UI Service Refactoring]]
  - **Reference**: [[UI_REFACTORING_PLAYBOOK|UI_REFACTORING_PLAYBOOK.md]]

- [ ] **Finish ship movement simplification verification and remaining phases**
  - **Plan**: [[TODOs/Refactoring/Ship_Movement_Simplification_Plan|Ship Movement Simplification Plan]]
  - **Remaining**: reconcile the recorded initialization-order retry and later phases against current behavior.

- [ ] **Verify ship and squadron entity simplification behavior**
  - **Plan**: [[TODOs/Refactoring/Ship_Squadron_Entity_Simplification_Plan|Ship and Squadron Entity Simplification Plan]]
  - **Remaining**: recorded behavior checks; implementation and compilation were previously recorded as complete.

- [ ] **Reconcile SkirmishOrchestrator refactor acceptance**
  - **Plan**: [[TODOs/Refactoring/SkirmishOrchestrator_Refactoring_Plan|SkirmishOrchestrator Refactoring Plan]]
  - **Remaining**: route, visibility, teardown, and prefab verification; implementation exists despite the historical proposed status.

- [ ] **Approve Ship Lit Autodesk conversion visuals**
  - **Plan**: [[TODOs/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan|Ship Lit Autodesk Material Conversion Plan]]
  - **Blocked**: recorded user visual approval of `Logs/ShipLitConversion/2026-09-28/review.html`.

- [ ] **Coordinate audit remediation (parallel plans)**
  - **Plan**: [[TODOs/Refactoring/Audit_Remediation_Coordination|Audit Remediation Coordination]]: waves, file ownership, Unity lane.
  - **Remaining**: Wave 1 (6 plans) → Wave 2 (2 plans). Input system deferred.

- [ ] **Deduplicate capture/reinforcement squadron tally** (Wave 1)
  - **Plan**: [[TODOs/Refactoring/Capture_Reinforcement_Tally_Dedup|Capture Reinforcement Tally Dedup]]
  - **Remaining**: code + tests committed; manual capture/zone playtest pending.

- [ ] **Type unit request / reinforcement identity keys** (Wave 1)
  - **Plan**: [[TODOs/Refactoring/Unit_Request_Identity_Keys|Unit Request Identity Keys]]
  - **Remaining**: code committed (`cd7b6edb`), compiles clean; manual build-queue + all four reinforcement kinds playtest pending.

- [ ] **Remove runtime component lookups** (Wave 1)
  - **Plan**: [[TODOs/Refactoring/Explicit_Component_Binding|Explicit Component Binding]]

- [ ] **Split multi-type files and fix placement** (Wave 2)
  - **Plan**: [[TODOs/Refactoring/Type_File_Split_And_Placement|Type File Split and Placement]]
  - **Blocked**: Wave 1 merge; user decision on interface+impl pairs.

- [ ] **Apply injection and guard conventions** (Wave 2)
  - **Plan**: [[TODOs/Refactoring/Injection_And_Guard_Conventions|Injection and Guard Conventions]]
  - **Blocked**: Wave 1 merge; user decision on MonoBehaviour `[Inject]` style.

- [ ] **Refactor audio system (AudioService / Music / Ship SFX)**
  - **Plan**: [[TODOs/Refactoring/Audio_System_Refactor_Plan|Audio System Refactor Plan]]
  - **Rule**: `IAudioService` plays source+clip only; music in `IMusicService`; all ship SFX via `IShipSfxService.Try*` (may refuse).
  - **Status**: steps 1–7 implemented; Unity compile clean; shared clip data, pool, mixer, bindings, and persistence inspected.
  - **Remaining**: step 8 listening, ≥20-ship profiler, pause/music/exit acceptance. Open `MainMenuScene` has unsaved changes; no Play Mode or automated tests run.

- [ ] **Refactor camera system (input guard, smoothing util, scroll fix, cinematic data)**
  - **Plan**: [[TODOs/Refactoring/Camera_System_Refactor_Plan|Camera System Refactor Plan]]
  - **Rule**: `CameraService` owns the input-lock rule; `CameraInput` has no `enabled` fallback.
  - **Decision**: `VelocitySmoothing` stays in `Assets/Scripts/Components/Utils/`.
  - **Status**: sections 1–4 implemented; Unity compile clean; asset values, GUIDs and Addressables entry verified.
  - **Cinematic fix**: 2026-10-07 framing adapts to navigation radius + camera FOV; chase/low-high shots center on the ship. Transitions orbit the look point with minimum clearance; wide shots include focus-offset clearance. Cinematic EditMode tests 17/17 passed; compilation clean.
  - **Map-bounds fix**: 2026-10-09 shots and transitions stay within exact map X/Z bounds and camera zoom Y limits; viewing distance capped at `ZoomRange.Max`. Cinematic tests 23/23 passed; compilation clean.
  - **Remaining**: manual lock/release, zoom/invert/rebind and cinematic visual acceptance. `MainMenuScene` was clean on 2026-10-09; no Play Mode run.

- [ ] **Battle startup sequence (state/speed notifiers, async map, gated systems)**
  - **Plan**: [[TODOs/Refactoring/Battle_Startup_Sequence_Plan|Battle Startup Sequence Plan]]
  - **Rule**: systems gate themselves by observing `BattleState`; never via Zenject order, `LazyInject`, or `Time.timeScale`.
  - **Status**: 2026-10-04 phases 1–4 implemented; Unity compile clean, CoreGameUi prefab re-keyed and read back. No Play Mode or automated tests run.
  - **Remaining**: Play Mode acceptance (fade, fog, camera drift, hitch, Zenject graph, Esc during loading, pause/speed/menu/end/exit). Fader added 2026-10-04.

- [ ] **Faction scalability (per-faction data, unit/faction definitions)**
  - **Plan**: [[TODOs/Refactoring/Faction_Scalability|Faction Scalability]]
  - **Status**: Phase 1 done; Rebellion + Empire factions added (placeholder station/audio from Republic).
  - **Remaining**: Phase 1 playtest; Phases 2–5.

### Tooling


- [ ] **Add Jenkins GitHub draft releases**
  - **Plan**: [[TODOs/Tooling/Jenkins_GitHub_Releases|Jenkins GitHub Releases]]
  - **Status**: 2026-10-03 scripts and draft-only job installed; GitHub CLI/Copy Artifact checksums verified; PowerShell parsing, Jenkins Pipeline validation, configuration readback, and installed hashes passed. No builds, uploads, or automated tests ran.
  - **Remaining**: user adds `github-releases` credential and selects source build/tag; first live upload and recovery acceptance.

- [ ] **Set up Jenkins local Windows builds**
  - **Plan**: private local documentation outside this vault; excluded from Git.

## Architecture Backlog

- [ ] Audit gameplay services for direct `IUiService` or `BaseUi` references.
- [ ] Migrate UI creation calls from services into feature UI controllers / presenters.
- [ ] Ensure all feature data container classes (`<Feature>Data`) inherit from `Data` ScriptableObject.

## Done

### Tooling

- [x] **Implement Game Design Balance Editor**
  - **Completed**: 2026-10-09; installed staged UI Toolkit workbench and allowlisted adapters.
  - **Plan**: [[Done/Tooling/GameDesign_Balance_Editor|Game Design Balance Editor]]
  - **Research**: [[Done/Tooling/GameDesign_Balance_Editor - Research|Data ownership and feasibility]]
  - **Scope**: UI Toolkit; approved ScriptableObject/prefab data fields; faction views, editable same-/cross-faction ship comparisons, shared weapon/ability labels, hardpoints and Editor-applied presets.
  - **Units layout (2026-10-09)**: Compare cards and searchable single/bulk Add units; Edit unit opens full details. No ship sidebar. Unity compile and 27 live UI checks passed.
  - **Deferred**: hard-coded AI configuration and ally/enemy multipliers remain outside this data-only scope.
  - **Outcome**: canonical usage/alias inventory, recoverable draft, editable comparison, tuning panels and scoped preset apply/restore implemented.
  - **Combat grid (2026-10-09)**: Compare-style Add units picker and full-width unit cards with 420 px hardpoint lists; shared selections/drafts, no roster, preserved weapon profiles/damage matrix. Unity compile/import and live UI checks passed: 47 mounts/37 assignment controls, 960/1400/1800 px layouts; no live prefab writes or NUnit/Play Mode run.
  - **UI refinement (2026-10-08)**: Units/Compare/Combat/Changes; compact 32 px roster, nested unit tabs, full-width responsive details, draft stat overview and optional inspector. Layout/field/scroll checks passed; Balance EditMode rerun 24/24.
  - **Verification**: balance acceptance 24/24; full EditMode 1,279/1,284 (five existing AI/engine/Acclamator check failures). Clean skirmish startup/running; Apply blocked in Play Mode. Shutdown exposed two existing null-parent UI route disposal errors.
  - **Review fixes (2026-10-09)**: all six preset, missing-profile, draft-consumer and comparison/roster issues fixed. Added 11 regressions; 34/35 final suite plus isolated 1/1 rerun after a Pipeline timeout-log failure. All 35 cases have passing results; native 1400×850 / 960×550 layouts verified.
  - **Comparison grid (2026-10-09)**: faction/search picker with select/unselect-all and bulk addition; responsive 330 px slots without a row cap; card backgrounds/handles reorder across rows and switch to Manual. Ascending draft-aware Price/Height Level/Availability Level/Class ordering; class sequence squadrons → corvette → frigate → cruiser → capital → heavy capital/dreadnought.
  - **Hardpoint popup (2026-10-09)**: View hardpoints replaces embedded lists; 620 × ≤560 px popup shows all mounts, retains draft weapon edits and stays open across comparison refreshes. Balance EditMode **46/46 passed**, 0 Console errors; no new Play Mode run.
  - **Comparison selection (2026-10-09)**: click selects one card; Ctrl+click toggles cards; Ctrl+A selects all; Delete removes selected cards. Selection survives sorting/redraws; fields/buttons keep normal input; removal preserves drafts. Balance EditMode **49/49 passed** in **36.40 s**; no Play Mode run.
  - **Shift selection (2026-10-09)**: Shift+click selects the inclusive range in displayed order; Ctrl+Shift+click adds a range. Forward/reverse ranges, retained anchor, reorder and removed-anchor behavior verified. Balance EditMode **50/50 passed** in **45.12 s**, 0 Console errors; no Play Mode run.
  - **Editable cards/performance (2026-10-09)**: immediate result-click additions; editable source stats, height enum and existing hardpoint weapon types. Inventory 10.605 s → 1.794 s; 9,135-field preset save 407 ms; one-field Apply/read-back 4,273 ms. UI and temporary-asset persistence/restore verified; live balance values preserved.



### Features

- [x] **Add Raider Corvette to Empire**
  - **Plan**: [[Done/Features/RaiderCorvette_Import|Raider Corvette Import]]
  - **Reference**: [[GameDesign/Raider Corvette Import]]
  - **Completed**: 2026-10-07; Empire ship `208`, hull/shields/speed `600/800/35`, ten non-targetable mounts, no fighters, self-activated Pursuit; dedicated art/shield/wreck/placement/icons and all registrations committed.
  - **Verification**: Raider `8/8` and ship-ability `17/17` tests passed; related ion/missile firing-cone bug fixed. Geometry/UV/bones, seven source hashes, saved references and eight live/wreck palettes verified. Broader observed suite `1,086/1,090`, with four unrelated failures; no combat Play Mode run.

- [x] **Add ISD II**
  - **Plan**: [[Done/Features/ISDII_Import|ISD II Import]]
  - **Completed**: 2026-10-06; Empire ship `205`, exact requested stats/loadout, Power to Main Batteries, Tractor Beam, three fighter types, own art/shield/placement/wreck/icons. Commit `f17fcd22`.
  - **Team-color follow-up**: 2026-10-06 ISD I and ISD II now use four mirrored foredeck stripes per side with neutral hulls and matching wrecks. Saved ownership/fog/banking/explosion/wreck bindings and red/blue ship/wreck renders verified; no import/serialization errors. No combat or automated Unity test run for this fix.
  - **Verification**: all 11 ship/ability tests passed; saved-ship checks 8/8 after naming fixes. Full EditMode 1,066/1,067; remaining failure is existing Victory II renderer bindings. Fixed ISD II broadside arcs and all 99 team bindings; GUID dependencies and renders verified. No manual skirmish/Play Mode run.

- [x] **Spawn blockers and relays (replace reinforcement-zone spawning)**
  - **Plan**: [[Done/Features/Spawn_Blockers_And_Relays|Spawn Blockers and Relays]]
  - **Completed**: 2026-10-05; vision service, spawn blockers, unified spawn rule (ships/squadrons/structures, player + AI), zones → capturable relays, spawn overlay, unified structure clearance, relay model.
  - **Verification**: EditMode 982/1019 pass (37 failures pre-existing or from parallel ship imports); Play Mode skirmish checked live blockers, relay capture flip, AI deployment, overlay; no feature errors.
  - **2026-10-08**: Captured relays keep `RelaySpawnBlockRadius` visible for their team until recaptured. EditMode 32/32 pass across relay, vision, spawn-point and spawn-rule tests; no new Play Mode run.

- [x] **Implement unit actions (ship orders)**
  - **Plan**: [[Done/Features/SHIP_ACTIONS_PLAN|Unit Actions (Ship Orders) implementation plan]]
  - **Completed**: 2026-09-30 closeout; implementation `ea3b8759`, subsequent retreat/UI changes `767da64c`.
  - **Verification**: live order APIs, action-prefab bindings, and acceptance-test sources checked; no tests or Play Mode run in this review.

### Refactoring

- [x] **Resolve codebase audit 2026-09-27**
  - **Plan**: [[Done/Refactoring/Codebase Audit 2026-09-27/00 Overview|Codebase audit overview]]
  - **Completed**: 2026-09-30. The original 17 fixes are implemented. The follow-up findings from [[Done/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|the re-audit]] are superseded by the Audit Remediation plans.
  - **Verification**: live-source checks recorded in notes 09–11; no tests run in the re-audit passes.

- [x] **Fix Project Organization & Asset Naming Defects**
  - **Plan**: [[Done/Refactoring/Project_Organization_Remediation_Plan|Project Organization & Asset Naming Remediation Plan]]
  - **Research**: [[Done/Refactoring/Project_Organization_Remediation_Plan - Research|Full Audit & Naming Mapping Catalog]]
  - **Completed**: 2026-09-30; implementation `3dcee4a6`.
  - **Verification**: all 736 manifest target paths/GUIDs match; additional SceneContext prefab GUID matches; three Resources bootstrap assets remain.
