# Project TODOs & Backlog

- Canonical execution index. Plans own progress, blockers and verification; [[GameDesign/00_GameDesign]] owns current gameplay references.
- Detailed pre-reorganization entries: [[Artifacts/History/Backlog_Before_2026-10-20]]. No task status was changed by index compression.

## Active Tasks

### Features

- [ ] **Extend audio system with Republic at War SFX (interface, orbital announcer, ambience, shields, per-unit engines/explosions, alarms)** → [[TODOs/Features/Audio_System_Extension_Plan|Audio System Extension Plan]]; [[TODOs/Features/Audio_System_Extension_Plan - Research|Clip inventory and unit mapping]]
  - **Implemented 2026-10-22**: 33 mapped imports, interface/orbital cues, Burst ambience, shield/ability sounds, per-unit engines/explosions and alarms; compile passed, 73 distinct recorded EditMode cases passed.
  - **Remaining**: user in-game listening acceptance (balance, flyby loop seams, shield cadence, ambience fade and announcer timing); no agent Play Mode.
- [ ] **Replace battle and menu music with categorized Republic at War tracks** → [[TODOs/Features/Music_Replacement_Plan|Music Replacement Plan]]; [[TODOs/Features/Music_Replacement_Plan - Research|Exact track inventory]]
  - **Implemented (2026-10-22)**: 22 battle + 31 menu tracks; 41 legacy MP3s removed; Vorbis streaming; loading-state fix; 8/8 tests and Windows Editor menu playback/pause/mute/completion smoke passed. Windows build Succeeded (3.80 GiB), built playlists verified; unrelated planet shader error recorded.
  - **Remaining**: standalone/battle-scene and listening acceptance, strict single-source review (pre-existing idle scene clone), representative audio profiling; macOS build module/runtime unavailable.

- [ ] **Import Empire fleet: Procursator, Allegiance, Titan and Tartan** → [[TODOs/Features/Empire_Fleet_Import|Empire Fleet Import]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Game modes: Survival and Battle Simulation** → [[TODOs/Features/Game_Modes_Survival_And_Simulation|Game Modes Survival And Simulation]]
  - **Remaining**: Phase 0 full-suite green gate: 13 SensorJamming allowlist failures, Allegiance radar/weapon-scope checks, and stale entity-prefab count. Plan stays active for these pre-existing failures.

- [ ] **AI composition analysis and counter measures** → [[TODOs/Features/AI_Composition_Counters|AI Composition Counters]]
  - **Remaining**: skirmish Play Mode acceptance and threshold tuning against current balance.

- [ ] **Verify MC80 Independence integration acceptance** → [[TODOs/Features/MC80Independence_Import|MC80 Independence Import]]
  - **Remaining**: clean-skirmish acceptance and provisional balance/source-difference review.

- [ ] **Verify BTL-A4 Y-Wing Bomber integration acceptance** → [[TODOs/Features/YWingBomber_Import|BTL-A4 Y-Wing Bomber Import]]
  - **Remaining**: clean-skirmish acceptance; provisional economy/weapon/flight balance and source-difference review. Passive AOTR ion-stunner slowdown is not ported.

- [ ] **Verify ArquitensImperialCruiser integration acceptance** → [[TODOs/Features/ArquitensAdvanced_Import|ArquitensImperialCruiser Import]]
  - **Remaining**: clean-skirmish acceptance and remaining provisional balance/source-difference review.

- [ ] **Verify Victory II integration acceptance** → [[TODOs/Features/VictoryII_Import|Victory II Import]]
  - **Remaining**: clean-skirmish acceptance; review provisional balance and documented engine-effect precision/source differences.

- [ ] **Verify Victory I integration acceptance** → [[TODOs/Features/VictoryI_Import|Victory I Import]]
  - **Remaining**: clean-skirmish acceptance and provisional scale/movement/weapon/system/ability/economy/launch balance review.

- [ ] **Verify MC75 Profundity integration acceptance** → [[TODOs/Features/MC75_Profundity_Import|MC75 Profundity Import]]
  - **Remaining**: clean-skirmish hardpoint/combat/launch/ability/destruction acceptance and provisional balance review.

- [ ] **Verify T-65 X-Wing import acceptance** → [[TODOs/Features/XWing_Import|X-Wing Import]]
  - **Remaining**: clean-scene in-game acceptance and provisional movement/range/balance review. No automated tests or Play Mode run.

- [ ] **Verify Imperial Victory integration acceptance** → [[TODOs/Features/Victory_Import|Victory Import]]
  - **Remaining**: clean-skirmish acceptance and provisional scale/movement/weapon/system/limit/launch balance review; supplied hull retains Republic roundels.

- [ ] **Verify V-Wing squadron integration acceptance** → [[TODOs/Features/VWing_Import|V-Wing Integration]]
  - **Remaining**: clean-skirmish acceptance and provisional flight/repair/weapon/tech/limit/matchup balance review.

- [ ] **Verify IPV-2C Stealth Corvette integration acceptance** → [[TODOs/Features/StealthCorvette_Import|Stealth Corvette Import]]
  - **Remaining**: clean-skirmish cloak/combat/placement/destruction acceptance and provisional scale/movement/weapon/armor/audio balance review.

- [ ] **Finish V-19 Torrent integration acceptance** → [[TODOs/Features/V19Torrent_Import|V-19 Torrent Import]]
  - **Remaining**: Hunt ability decision/implementation, clean-skirmish acceptance and provisional balance review; deployed wing pose is static.

- [ ] **Verify Imperator-class Star Destroyer integration acceptance** → [[TODOs/Features/Imperator_Import|Imperator Import]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Verify Dispatcher-class Frigate integration acceptance** → [[TODOs/Features/Dispatcher_Import|Dispatcher Import]]
  - **Remaining**: clean-skirmish acceptance and provisional movement/weapon/system/limit/matchup balance review; source death ALA remains outside static conversion.

- [ ] **Verify C-9979 Lander integration acceptance** → [[TODOs/Features/C9979_Import|C-9979 Import]]
  - **Remaining**: clean-skirmish acceptance and provisional balance review; project uses Corvette damage category because Transport armor is unavailable.

- [ ] **Verify BTL-B Y-Wing integration acceptance** → [[TODOs/Features/YWing_Import|Y-Wing Import]]
  - **Remaining**: clean-skirmish acceptance and provisional ability/repair/economy/weapon/flight balance review.

- [ ] **Import NTB-630 Naval Bomber** → [[TODOs/Features/NTB630_Import|NTB-630 Import]]
  - **Remaining**: missing original turret textures (neutral metal used), in-game acceptance and provisional balance review.

- [ ] **Import ARC-170 squadron** → [[TODOs/Features/ARC170_Import|ARC-170 Import]]
  - **Remaining**: Lock S-Foils, Astromech Repair, in-game acceptance and provisional balance review.

- [ ] **Import Pride of the Core / Mandator II** → [[TODOs/Features/Mandator_Import|Mandator Import]]
  - **Remaining**: Tractor Beam ability, independent system damage review, clean-skirmish acceptance and provisional balance review.

- [ ] **Import Resolute hero Venator** → [[TODOs/Features/Resolute_Import|Resolute Import]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Import CIS Patrol Frigate** → [[TODOs/Features/Patrol_Frigate_Import|Patrol Frigate Import]]
  - **Remaining**: in-game acceptance and provisional movement/hardpoint balance review.

- [ ] **Import Captor-class Carrier** → [[TODOs/Features/Captor_Import|Captor Import]]
  - **Remaining**: in-game acceptance, including Vulture/Droid Bomber launches, and provisional balance review.

- [ ] **Finish Malevolence integration acceptance** → [[TODOs/Features/Malevolence_Import|Malevolence Import]]
  - **Remaining**: exact Vulture/bomber squadron assets and launches; in-game combat, placement, ability and destruction acceptance; provisional balance review.

- [ ] **Review ship abilities implementation** → [[TODOs/Features/Ship_Abilities_Plan|Ship Abilities Plan]]
  - **Remaining**: implementation review and acceptance evidence; commit `dc869bbb` recorded review as needed.

- [ ] **Implement UI tooltip system** → [[TODOs/Features/Tooltip_System_Plan|Tooltip System Plan]]; [[TODOs/Features/Tooltip_System_Plan - Research|Tooltip coverage catalog and codebase findings]]
  - **Remaining**: in-game acceptance (disabled cards, live refresh/rebind, fog/drag/HUD/route lifecycle, screen edges and teardown). EditMode tests authored; execute only on request.

- [ ] **More mining sites; 2 home mines beside each station + central mines** → [[TODOs/Features/More_Mining_Sites|More Mining Sites]]
  - **Implemented 2026-10-26**: counts 8/10/14 with 1/1/2 central pairs; second home mine steps outward until clear; Small pocket 100, Large spacing 300 to fit 3–4 players. Map 23/23, save 21/21, survival-spawn 2/2 EditMode pass.
  - **Remaining**: user in-game check of generated maps and economy/AI feel.

- [ ] **Close settings audit follow-ups** (Wave 1) → [[TODOs/Features/Settings_Audit_Followups|Settings Audit Follow-ups]]; [[TODOs/Features/Settings_Implementation_Plan|Settings Implementation Plan]]
  - **Remaining**: remaining settings core EditMode tests; live category/rebind interaction; manual restart-persistence, display Keep/Revert/timeout, and corrupt-file checks.


### Bugs

- [ ] **Fix scene-teardown pool errors and material leaks** (Wave 1) → [[TODOs/Bugs/Scene_Teardown_And_Material_Leaks|Scene Teardown and Material Leaks]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Replace silent null returns with fail-fast/Try APIs** (Wave 1) → [[TODOs/Bugs/Fail_Fast_Null_Returns|Fail-Fast Null Returns]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

### Optimization

- [ ] **Pool ship contexts to remove spawn spikes** → [[TODOs/Optimization/Ship_Pooling_Plan|Ship Pooling]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Pool squadron contexts to remove launch spikes** → [[TODOs/Optimization/Squadron_Pooling_Plan|Squadron Pooling]]
  - **Next**: restart Editor → rerun EditMode → user checks launches/deaths/reinforcements in-game.

- [ ] **Centralize UI canvas routing and replace custom capture-site screen UI** → [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan|UI Canvas Routing Optimization]]; [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan - Research|Canvas inventory, view mapping and historical CPU attribution]]
  - **Next**: Play Mode behavior and matched performance/Development Player acceptance. New performance analysis explicitly deferred by the user; no savings claimed.

- [ ] **Optimize projectile VFX for PC without logic changes** → [[TODOs/Optimization/Projectile_VFX_PC_Optimization_Plan|Projectile VFX PC Optimization]]; [[TODOs/Optimization/Projectile_VFX_PC_Optimization_Plan - Research|Prefab, material and runtime variation audit]]
  - **Next**: verify equivalent head/trail material sharing, remove unnecessary bolt texture CPU copies, measure scoped fade/shader experiments and validate the full faction/mount/ability/superweapon variation matrix.

- [ ] **Apply moderate desktop asset compression** → [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop Asset Compression Plan]]
  - **Pass 2 (2026-10-24)**: 462/507 model textures compressed under screening, 47 meshes Low; Addressables bundles 1.53 GB → 1.03 GB, payload 6.46 → 2.66 GB; related EditMode tests pass. Mesh gate 3° rerun: 478 models at Low, bundles → 989.8 MB. In-game visual check pending.
  - **Next**: remaining categories, matched gameplay memory/loading/DSP captures, audio listening/startup/loop/crossfade and intended-scale visual acceptance; macOS module + actual Mac validation. Plan stays active.

- [ ] **Apply battle CPU fixes (targeting, due events, audio mix, navigation, squadrons)** → [[TODOs/Optimization/Battle_CPU_Fixes_Plan|Battle CPU Fixes Plan]]; [[TODOs/Optimization/Battle_CPU_Fixes_Plan - Research|Phase 8 acceptance checklist]]
  - **Remaining**: matched ≥3×10 s captures at 1×/4×, controlled warm-selection/visual and combat behavior gates, Development Player confirmation, and Phase 7 visual-wait removal. Plan stays in progress.

- [ ] **Attribute and reduce remaining battle CPU cost** → [[TODOs/Optimization/Battle_CPU_Performance_Plan|CPU profiling and follow-up]]; [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research|2026-10-11 capture comparison and agent findings]]
  - **Next**: fixed-camera matched workload + Development Player acceptance and longer repair/respawn coverage. Current frame median **33.76 ms** vs 36.10, with more squadrons and different rendering workload; no isolated speedup claim. Broader attribution remains open.

- [ ] **Optimize battle attacks and projectile reuse** → [[TODOs/Optimization/Battle_Attack_Optimization_Plan|Attack preparation, commit and pooling]]; [[TODOs/Optimization/Battle_Attack_Optimization_Plan - Research|Implementation history and phase gates]]
  - **Next**: target snapshot/fallback attribution, measured shared work, due-event lookup/emission/impact breakdown, warm-pool creation/lifecycle checks; preserve combat parity.

- [ ] **Optimize battle particle rendering** → [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan|Effects and squadron indicators]]; [[TODOs/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|Verified rendering and historical navigation evidence]]
  - **Next**: combat appearance/lifecycle acceptance and measured muzzle/bolt/smoke submission work; skip broad idle-session profiling. Shadows/motion blur only after higher-priority effects.

- [ ] **Optimize Burst jobs (lengthsq, select/SIMD, scheduling)** → [[TODOs/Optimization/Burst_Jobs_Optimization_Plan|Burst Jobs Optimization]]; [[TODOs/Optimization/Burst_Jobs_Optimization_Plan - Research|17-job audit and findings]]
  - **Phase 1 harness done 2026-10-22**: perf package + `Tests/Benchmarks` (radar + target-filter if/?:/select kernels); parity 18/18; Editor timings favour branch-free except at low pass ratios.
  - **2026-10-22 execution**: target/fog, shield SoA clipping, radar eligibility/XZ layout, due predicate and navigation stamps/raster/LOS implemented. SSE2/AVX2 assembly captured; 156 distinct EditMode cases pass after Burst cache invalidation, including earlier-kernel reruns and shield-swap integration.
  - **Scheduling 2026-10-22**: bolt Update → LateTick; radar preparation overlap within FixedTick (83 distinct tests pass). Fighter PostTick → pre-combat LateTick now implemented through ordered lifecycle bridge, with spawn/pool/root-write/teardown guards; 73 distinct follow-up tests pass. Compile clean; composition logs unrelated ambience EditMode destruction errors.
  - **Remaining**: user Player baseline + ≥3 `PlayerBranchVariant*` runs, battle pass ratios, matched timing acceptance, batch/branch winners, fighter scheduling performance acceptance, Phase 6 tuning and deferred shield batching decision. Plan remains active.

- [ ] **Remove startup/loading-screen freezes (sync Addressables loads, squadron prewarm)** → [[TODOs/Optimization/Loading_Freeze_Plan|Loading Freeze Plan]]
  - **Editor retest (concurrent batches)**: revision `a583652df`, fixed seed; Running in 5.664 s (prior 20.723 s), no new runtime errors. Worst asset-loading frame 983.782 ms; prewarm max 48.837 ms. Loading freezes remain; Editor restored; evidence in plan/research.
  - **Deferred warm-up + CRC off (2026-10-23)**: unit views/wrecks load after Running; 1 s loading delay removed; ADB Running in 3.731 s, post-Running p95 17.61 ms. Packed mode 20.5 s traced to bundle CRC (View open 15.5 s vs 40 ms); CRC disabled, needs content rebuild + packed/Player recapture. Open: Mono JIT (IL2CPP decision).
  - **Additive target load (2026-10-23)**: battle/menu load additively, loading scene unloaded async; activation `GC.Collect` gone; leak check 0 objects in loading scene, 0 warnings; loading EventSystem removed.
  - **Implemented 2026-10-23**: async mapping/root/battle preload, scope-owned cache handoff, one-squadron-per-frame prewarm and 58 wreck AssetReferences; compilation clean, 77/77 relevant EditMode tests pass. Content build/layout succeeded: Data 15,883,473 B, no implicit wreck prefabs; [[TODOs/Optimization/Loading_Freeze_Plan - Research|Verification and bundle review]].
  - **Editor profile 2026-10-23**: default skirmish reaches Running in 20.723 s after request, no runtime errors. Scope Configure 5.984/9.800 ms; 30 sliced prewarm items, max 41.978 ms. Primary startup/battle peaks 556.749/475.198 ms; confirming asset-load peak 631.154 ms. Editor hitches remain; details in research.
  - **Remaining**: user-deferred Player baseline/matched captures, spinner/transition/leak acceptance; loading-scene async switch and packing changes only if profiling justifies them. Plan stays active.

- [ ] **Shrink remaining build size and runtime memory (textures, UI, music, SFX, Read/Write, player build)** → [[TODOs/Optimization/Asset_Size_Followup_Plan|Asset Size Follow-up]]
  - Start 2026-10-24: 989.8 MB bundles, payload 2,564 MB (textures 90 %). Top levers: unused alpha → DXT1 (≤ −400 MB est.), texel-density resolution caps, 45 restored sharp-alpha textures, music out of `Model` bundle.
  - **Next**: item 2 (Providence RGBA32 anomaly) and item 1 (alpha-aware retry).

- [ ] **Remove Addressables bundle duplication and fix asset-mapping tools** → [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Addressables Bundle Remediation]]; [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research|Audit 2026-10-24 findings]]
  - Audit 2026-10-24: 962 duplicated assets / 1.31 GiB redundant payload (Model+View 852), 128 identical-source sets, View 1.63 GiB; ReinforcementData hard-refs 78 previews; mapping rebuild drops `VenatorIIShipView`, SO keying overwrites variants.
  - **Phases 0–1 done 2026-10-24**: fresh layout baseline 979 dup assets / 1,430,171,798 B (Model+View 864), Analyze 5,272/25/64 recorded; mapping rebuild keeps aliases (`VenatorIIShipView`, `MenuUi`), Add Selected keys by entry address and refuses foreign-GUID overwrites; 8/8 EditMode tests pass.
  - **Phase 2 done 2026-10-24**: previews are AssetReferences in `View`, preloaded only for human/hosted factions; duplicated assets 979 → 115, redundant payload 1.43 GB → 135 MB, Model bundle 447 → 166 MB. Related EditMode tests pass; in-game placement check pending (user).
  - **Phases 3–4 done 2026-10-24**: shared ownership eliminates remaining 115 duplicated assets / 135,383,837 redundant bytes; 72 identical-import sets consolidated (262 files, 275,429,420 source bytes), 56 variant sets queued. Content bundles 1,951,810,468 → 1,532,590,578 B; zero duplicated payload. 394 AssetReferences resolve; VenatorII alias intact.
  - **Phases 5–6 done 2026-10-24**: four faction view/preview/wreck groups; final Windows build 18 bundles / 1,534,762,737 B / zero duplicated payload (417,047,731 B lower vs Phase 2). Split adds 2.17 MB bundles and 18.17 KB catalog; faction closures 120–795 MB vs 1.38 GB unsplit. Audio closure 185 KB; no ship-art dependency. Fresh Analyze: no bundle duplicates, 20 Resources + 47 scene overlaps documented intentionally. Compile/Console clean, mixer regression 1/1 passed.
  - **Verified 2026-10-24**: final layout 0 duplicates; 0 references to the 262 deleted GUIDs; 0 duplicate addresses; VenatorII alias intact. Phases 3–6 + music fix still uncommitted. Storage: 94 % textures; 173 uncompressed 2048² textures = 3.87 GB of 6.46 GB payload → Desktop Asset Compression plan.
  - **Remaining (user)**: packed-player placement, master mute/music/SFX/ducking, early-spawn sync-load timing; no agent Play Mode. Plan stays active for these acceptance gates.

### Refactoring

- [ ] **Project organization follow-up** → [[TODOs/Refactoring/Project_Organization_Followup|Project Organization Follow-up]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Weapon taxonomy and balance system** → [[TODOs/Refactoring/Weapon_Taxonomy_Balance_System|Weapon Taxonomy and Balance System]]; [[TODOs/Refactoring/Weapon_Taxonomy_Balance_System - Mapping|Mapping]]
  - **Remaining**: user playtest; balance tuning (stations outranged, ISD II +200 %, missile reach vs radar); resolve existing StrikeFighterProtonTorpedo variant metadata.

- [ ] **Rebalance to original Republic at War values; remove range classes; simplify Republic and CIS hardpoints** → [[TODOs/Refactoring/Original_Values_Rebalance|Original Values Rebalance]]; [[TODOs/Refactoring/Original_Values_Rebalance - Research|Original vs live tables and range-removal impact]]
  - **Decided 2026-10-25**: Executor untouched; hull-relative reach for all; hardpoint simplification Republic/CIS only; C-9979 corvette band; capital ≥ Medium rule removed; tests updated with the change.
  - **Mapping 2026-10-25**: turbolaser battery → Medium; quad → Heavy only as capital main battery, else new Medium Quad. Mandator/Invincible/Lucrehulk keep live HP/shields.
  - **Phase 0 done 2026-10-25** (`2f45f1371`): 260 same-class duels, per-unit DPS and Executor per-gun snapshot in `Logs/Rebalance/2026-10-25-baseline/`.
  - **Phases 1–3 done 2026-10-25**: range classes removed (step 6 hull-relative standoff dropped — measured it pushed big ships out of reach); Republic/CIS original HP/shields + simplified loadouts on 24 ships; Empire/Rebellion HP/shields clamped into class bands.
  - **Phase 4**: EditMode 2074/2077 (3 unrelated failures); simulator class win rates still uneven (Separatist cruisers 12 %).
  - **Next**: user skirmish playtest; decide whether to tune Empire/Rebellion DPS (hardpoint simplification).

- [ ] **Migrate Zenject to VContainer** → [[TODOs/Refactoring/Zenject_To_VContainer_Migration|Zenject to VContainer Migration]]; [[TODOs/Refactoring/Zenject_To_VContainer_Migration - Research|Injection audit, scopes, factories and lifecycle mapping]]; [[Done/Refactoring/UI_Service_Refactoring|UI rework]]
  - **Cleanup 2026-10-22**: removed 24 stale generated projects (Zenject, legacy MCP/framework/samples, Postprocessing and an obsolete URP test player project) and refreshed the IDE solution/editor inventory. `com.script-utils.math` still names its active math assembly `ScriptUtils.Zenject`; upstream naming correction remains.
  - **Remaining**: matched startup/spawn/teardown/frame measurements. `9c072d3c5` facade-lookup allocation fix confirmed (GC median 89 → 27 KB/frame) and per-tickable markers restored. [Capture](<F:/Private/empire-at-war/Logs/BattleAudit/20261013T1854Z-vcontainer/analysis.md>)

- [ ] **Finish ship movement simplification verification and remaining phases** → [[TODOs/Refactoring/Ship_Movement_Simplification_Plan|Ship Movement Simplification Plan]]
  - **Remaining**: reconcile the recorded initialization-order retry and later phases against current behavior.

- [ ] **Verify ship and squadron entity simplification behavior** → [[TODOs/Refactoring/Ship_Squadron_Entity_Simplification_Plan|Ship and Squadron Entity Simplification Plan]]
  - **Remaining**: recorded behavior checks; implementation and compilation were previously recorded as complete.

- [ ] **Reconcile SkirmishOrchestrator refactor acceptance** → [[TODOs/Refactoring/SkirmishOrchestrator_Refactoring_Plan|SkirmishOrchestrator Refactoring Plan]]
  - **Remaining**: route, visibility, teardown, and prefab verification; implementation exists despite the historical proposed status.

- [ ] **Approve Ship Lit Autodesk conversion visuals** → [[TODOs/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan|Ship Lit Autodesk Material Conversion Plan]]
  - **Blocked**: recorded user visual approval of `Logs/ShipLitConversion/2026-09-28/review.html`.

- [ ] **Coordinate audit remediation (parallel plans)** → [[TODOs/Refactoring/Audit_Remediation_Coordination|Audit Remediation Coordination]]
  - **Remaining**: Wave 1 (6 plans) → Wave 2 (2 plans). Input system deferred.

- [ ] **Deduplicate capture/reinforcement squadron tally** (Wave 1) → [[TODOs/Refactoring/Capture_Reinforcement_Tally_Dedup|Capture Reinforcement Tally Dedup]]
  - **Remaining**: code + tests committed; manual capture/zone playtest pending.

- [ ] **Type unit request / reinforcement identity keys** (Wave 1) → [[TODOs/Refactoring/Unit_Request_Identity_Keys|Unit Request Identity Keys]]
  - **Remaining**: code committed (`cd7b6edb`), compiles clean; manual build-queue + all four reinforcement kinds playtest pending.

- [ ] **Remove runtime component lookups** (Wave 1) → [[TODOs/Refactoring/Explicit_Component_Binding|Explicit Component Binding]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Split multi-type files and fix placement** (Wave 2) → [[TODOs/Refactoring/Type_File_Split_And_Placement|Type File Split and Placement]]
  - **Blocked**: Wave 1 merge; user decision on interface+impl pairs.

- [ ] **Apply injection and guard conventions** (Wave 2) → [[TODOs/Refactoring/Injection_And_Guard_Conventions|Injection and Guard Conventions]]
  - **Blocked**: Wave 1 merge; user decision on MonoBehaviour `[Inject]` style.

- [ ] **Refactor audio system (AudioService / Music / Ship SFX)** → [[TODOs/Refactoring/Audio_System_Refactor_Plan|Audio System Refactor Plan]]
  - **Remaining**: step 8 listening, ≥20-ship profiler, pause/music/exit acceptance. Open `MainMenuScene` has unsaved changes; no Play Mode or automated tests run.

- [ ] **Refactor camera system (input guard, smoothing util, scroll fix, cinematic data)** → [[TODOs/Refactoring/Camera_System_Refactor_Plan|Camera System Refactor Plan]]
  - **Remaining**: manual lock/release, zoom/invert/rebind and cinematic visual acceptance. `MainMenuScene` was clean on 2026-10-09; no Play Mode run.

- [ ] **Battle startup sequence (state/speed notifiers, async map, gated systems)** → [[TODOs/Refactoring/Battle_Startup_Sequence_Plan|Battle Startup Sequence Plan]]
  - **Remaining**: Play Mode acceptance (fade, fog, camera drift, hitch, Zenject graph, Esc during loading, pause/speed/menu/end/exit). Fader added 2026-10-04.

- [ ] **Faction scalability (per-faction data, unit/faction definitions)** → [[TODOs/Refactoring/Faction_Scalability|Faction Scalability]]
  - **Remaining**: Phase 1 playtest; Phases 2–5.

### Tooling

- [ ] **Add Jenkins GitHub draft releases** → [[TODOs/Tooling/Jenkins_GitHub_Releases|Jenkins GitHub Releases]]
  - **Remaining**: user adds `github-releases` credential and selects source build/tag; first live upload and recovery acceptance.

- [ ] **Set up Jenkins local Windows builds**
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

## Architecture Backlog

- [ ] Audit gameplay services for direct `IUiService` or `BaseUi` references.
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

- [ ] Migrate UI creation calls from services into feature UI controllers / presenters.
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

- [ ] Ensure all feature data container classes (`<Feature>Data`) inherit from `Data` ScriptableObject.
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

## Done

### Documentation

- [x] **Vault organization and GameDesign references** → [[Done/Documentation/Vault_Organization_And_GameDesign_References]]
  - Preserve settings/credentials; verify moves, current references and links.
  - Completed 2026-10-20: 65 content moves, 10 current GameDesign references, repaired navigation and historical/proposal labels; static checks passed.

### Tooling

- [x] **Battle logic simulator (Editor Hub)** → [[Done/Tooling/Battle_Logic_Simulator|Battle Logic Simulator]]

- [x] **Implement Game Design Balance Editor** → [[Done/Tooling/GameDesign_Balance_Editor|Game Design Balance Editor]]; [[Done/Tooling/GameDesign_Balance_Editor - Research|Data ownership and feasibility]]

### Features

- [x] **Import Invincible as a separate Separatist ship** → [[Done/Features/Invincible_Import|Invincible Import]]
  - Providence × `2.785714` size; `10000` hull / `6000` shields; `28` weapons, existing system counts.
  - Source import completed 2026-10-24; saved references/geometry checks pass. No Play Mode combat or automated tests run.
  - Follow-up completed 2026-10-24: lowered selection/global-overlay anchor, darker hull/wreck/lights, price/population `12000/10`; saved verification and builder compilation pass.

- [x] **Unit hover/selection highlight in Ship Lit (EaW 2005 style)** → [[Done/Features/Unit_Highlight_Plan|Unit Highlight Plan]]; [[Done/Features/Unit_Highlight_Plan - Research|Shader, batching and material-change inventory]]
  - **Completed 2026-10-22**: render state, settings, entity facade and shared hover/tooltip picking; 95 distinct EditMode tests passed. Runtime battle acceptance/profiling skipped by user request.

- [x] **Battle session save (Skirmish + Survival)** → [[Done/Features/Battle_Session_Save|Battle Session Save]]

- [x] **Import MC30c Frigate** → [[Done/Features/MC30c_Import|MC30c Frigate Import]]

- [x] **Replace existing Tector model with Remake artwork** → [[Done/Features/Tector_Model_Replacement|Tector Model Replacement]]

- [x] **Integrate Imperial Venator into Empire** → [[Done/Features/ImperialVenator_Import|Imperial Venator Import]]

- [x] **Replace ISD I model from Remake** → [[Done/Features/ISDI_Model_Replacement|ISD I Model Replacement]]

- [x] **Add Raider Corvette to Empire** → [[Done/Features/RaiderCorvette_Import|Raider Corvette Import]]

- [x] **Replace ISD II visuals from Workshop 1770851727** → [[Done/Features/ISDII_Import|ISD II Import]]; [[Done/Features/ISDII_Import - Research|Source mappings and evidence]]

- [x] **Spawn blockers and relays (replace reinforcement-zone spawning)** → [[Done/Features/Spawn_Blockers_And_Relays|Spawn Blockers and Relays]]

- [x] **Implement unit actions (ship orders)** → [[Done/Features/SHIP_ACTIONS_PLAN|Unit Actions (Ship Orders) implementation plan]]

### Refactoring

- [x] **Rework UI before VContainer migration** → [[Done/Refactoring/UI_Service_Refactoring|UI Rework Before VContainer]]; [[Done/Refactoring/UI_Service_Refactoring - Research|UI inventory, placement and SoulsLikeTemplate comparison]]

- [x] **Resolve codebase audit 2026-09-27** → [[Done/Refactoring/Codebase Audit 2026-09-27/00 Overview|Codebase audit overview]]; [[Done/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|the re-audit]]

- [x] **Fix Project Organization & Asset Naming Defects** → [[Done/Refactoring/Project_Organization_Remediation_Plan|Project Organization & Asset Naming Remediation Plan]]; [[Done/Refactoring/Project_Organization_Remediation_Plan - Research|Full Audit & Naming Mapping Catalog]]
