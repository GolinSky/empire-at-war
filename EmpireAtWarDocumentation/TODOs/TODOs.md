# Project TODOs & Backlog

- Canonical execution index. Plans own progress, blockers and verification; [[GameDesign/00_GameDesign]] owns current gameplay references.
- Detailed pre-reorganization entries: [[Artifacts/History/Backlog_Before_2026-10-20]]. No task status was changed by index compression.

## Active Tasks

- User-only acceptance gates of closed plans → [[TODOs/User_Acceptance_Queue]].

### Features

- [ ] **Import Empire fleet: Procursator, Allegiance, Titan and Tartan** → [[TODOs/Features/Empire_Fleet_Import|Empire Fleet Import]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Finish V-19 Torrent integration acceptance** → [[TODOs/Features/V19Torrent_Import|V-19 Torrent Import]]
  - **Remaining**: Hunt ability decision/implementation, clean-skirmish acceptance and provisional balance review; deployed wing pose is static.

- [ ] **Import NTB-630 Naval Bomber** → [[TODOs/Features/NTB630_Import|NTB-630 Import]]
  - **Remaining**: missing original turret textures (neutral metal used), in-game acceptance and provisional balance review.

- [ ] **Import ARC-170 squadron** → [[TODOs/Features/ARC170_Import|ARC-170 Import]]
  - **Remaining**: Lock S-Foils, Astromech Repair, in-game acceptance and provisional balance review.

- [ ] **Import Pride of the Core / Mandator II** → [[TODOs/Features/Mandator_Import|Mandator Import]]
  - **Remaining**: Tractor Beam ability, independent system damage review, clean-skirmish acceptance and provisional balance review.

- [ ] **Finish Malevolence integration acceptance** → [[TODOs/Features/Malevolence_Import|Malevolence Import]]
  - **Remaining**: exact Vulture/bomber squadron assets and launches; in-game combat, placement, ability and destruction acceptance; provisional balance review.

- [ ] **Defense satellites: capturable pads around mining points, 5 satellite types, AI use** → [[TODOs/Features/Defense_Satellites_Plan|Defense Satellites Plan]]
  - Status: implemented 2026-10-28; satellite prefabs have uncommitted changes.
  - Next: commit prefab WIP; user in-game acceptance (combat, visuals, capture/build flow, AI).

- [ ] **Space hazards: Nebula / Ion Storm map areas (+ future map-wide events)** → [[TODOs/Features/Space_Hazards_Plan|Space Hazards Plan]]; [[TODOs/Features/Space_Hazards_Plan - Visuals|Visual art direction]]
  - Status: implemented; single hazards owner since 2026-10-29 (Rework + Shape Rework merged; current geometry = Shape Rework).
  - Next: IonStorm branches → data/material; SDF bake timing. User review/tests → queue; decor perf → FPS Recovery.



### Bugs

- [ ] **Fix defense satellite review findings** → [[TODOs/Bugs/Defense_Satellites_Review_Fixes|Defense Satellites Review Fixes]]; parent: [[TODOs/Features/Defense_Satellites_Plan|Defense Satellites Plan]]
  - Status: fixes #1–#10 implemented 2026-10-28; #3 kept by user.
  - Next: user in-game check (#4 build order, #5 raids, #10 cues, #11 rings, #12 home mines).

- [ ] **Fix enemy AI losing battles and CIS vs Republic balance** → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance|Enemy AI and Faction Balance]]; logs: [[Tooling/Battle_Telemetry|Battle Telemetry]]
  - Done (commit `6babee5e1`): Republic torpedo nerf, CIS hangar/squadron buffs, Concentrate Fire nerf, AI squadron escort, strength, rally, production, press-advantage and retreat-memory fixes; AI EditMode tests pass.
  - Next gate: 2–3 Ultra Hard battles on both factions; confirm AI attack states > 0% and fewer capture ↔ retreat flips in `_ai.csv`.

- [ ] **Replace silent null returns with fail-fast/Try APIs** (Wave 1) → [[TODOs/Bugs/Fail_Fast_Null_Returns|Fail-Fast Null Returns]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

### Optimization

- [ ] **Recover battle FPS lost after the hazard-system decor asteroids (decor culling/LOD, field rock LOD, nebula, presets, particles)** → [[TODOs/Optimization/Battle_FPS_Recovery_Plan|Battle FPS Recovery Plan]]; [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research|PC capture 2026-10-29 vs Mac audits]]
  - 2026-10-29: Phase 0 tool and Phases 1–6 implementations source-verified. Builds/performance runs deferred by user; absorbed attribution/experiments and visual/performance acceptance remain open.
  - 2026-10-29: re-verified against committed `76e7222f8` (code, prefabs, SceneContext, volume profile, materials); no defects found. Tests/captures not run.
  - In progress 2026-10-29: decor cell/frustum culling + 12,000-unit limit; meshes 1,496/1,196/196 tris; field LOD/material fix; per-quality nebula 48/64/96/128; Performant bloom/motion profile; asynchronous fog and VFX migration. Source/asset verification and exact legacy blend correction complete; live capture and historical Development Player comparison ongoing. No tests run; Mac measurement explicitly skipped by user.
  - 2026-10-29: Phases 0–6 implemented; Mac Editor validation collected under new user authorization: 42 ablation phases, 169-pose culling parity, camera sweep and 3×10 s combat (67–74 FPS). No production changes or automated suite.
  - Findings: nebula ≈7 ms residual frame cost in B view; pan p99 73.54 ms / max 404.51 ms; A ≥90 FPS not sustained. Mac Player blocked by Windows-only BuildInfoWriter callback; capture GPU/resolution/screenshot caveats documented.
  - Remaining: Player/build-support fix, PC matched historical captures, full visual/VFX acceptance and absorbed attribution/experiments; see plan/research. No historical Player build resumed.
  - Single rendering owner since 2026-10-29: absorbed Rendering Audit, Particle Rendering and Projectile VFX plans, plus hazard decor/overdraw measurement; newly absorbed items remain outside this execution's listed phases.

- [ ] **Nebula + hazard GPU optimization and upscaler settings** → [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan|Mac Nebula and Hazard GPU Plan]]
  - 2026-10-29 PC implementation: nebula bake/toggle, hazard H1/H2, resolution + Bilinear/FSR 1/STP, hazard audit pose and BuildInfo path. DLSS/FSR 2 rejected with reasons in plan.
  - 2026-10-29 PC fixes complete: both hazard `_Noise` bindings saved; builder loads textures after both imports; live nebula restored to 48/48/64/96/128/128 with bake-only 128 override. Unity compilation, builder execution and saved bindings verified; no tests or Play Mode run.
  - Tests not run. Prepared-battle PC captures pending; all Mac tasks skipped by user and left untouched.

- [ ] **Pool ship contexts to remove spawn spikes** → [[TODOs/Optimization/Ship_Pooling_Plan|Ship Pooling]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Centralize UI canvas routing and replace custom capture-site screen UI** → [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan|UI Canvas Routing Optimization]]; [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan - Research|Canvas inventory, view mapping and historical CPU attribution]]
  - **Next**: Play Mode behavior and matched performance/Development Player acceptance. New performance analysis explicitly deferred by the user; no savings claimed.

- [ ] **Apply moderate desktop asset compression** → [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop Asset Compression Plan]]
  - **Pass 2 (2026-10-24)**: 462/507 model textures compressed under screening, 47 meshes Low; Addressables bundles 1.53 GB → 1.03 GB, payload 6.46 → 2.66 GB; related EditMode tests pass. Mesh gate 3° rerun: 478 models at Low, bundles → 989.8 MB. In-game visual check pending.
  - **Next**: remaining categories, matched gameplay memory/loading/DSP captures, audio listening/startup/loop/crossfade and intended-scale visual acceptance; macOS module + actual Mac validation. Plan stays active.

- [ ] **Attribute and reduce remaining battle CPU cost** → [[TODOs/Optimization/Battle_CPU_Performance_Plan|CPU profiling and follow-up]]; [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research|2026-10-11 capture comparison and agent findings]]
  - **Next**: fixed-camera matched workload + Development Player acceptance and longer repair/respawn coverage. Current frame median **33.76 ms** vs 36.10, with more squadrons and different rendering workload; no isolated speedup claim. Broader attribution remains open.
  - Single CPU owner since 2026-10-29: absorbed open items of Battle CPU Fixes, Battle Attack and Burst Jobs plans.

- [ ] **Remove startup/loading-screen freezes (sync Addressables loads, squadron prewarm)** → [[TODOs/Optimization/Loading_Freeze_Plan|Loading Freeze Plan]]
  - Status: async loads, deferred warm-up, additive scene load, CRC off implemented 2026-10-23; Editor freezes remain.
  - Next: Player baseline + matched captures; IL2CPP decision; spinner/transition/leak acceptance.

- [ ] **Shrink remaining build size and runtime memory (textures, UI, music, SFX, Read/Write, player build)** → [[TODOs/Optimization/Asset_Size_Followup_Plan|Asset Size Follow-up]]
  - Start 2026-10-24: 989.8 MB bundles, payload 2,564 MB (textures 90 %). Top levers: unused alpha → DXT1 (≤ −400 MB est.), texel-density resolution caps, 45 restored sharp-alpha textures, music out of `Model` bundle.
  - **Next**: item 2 (Providence RGBA32 anomaly) and item 1 (alpha-aware retry).

- [ ] **Remove Addressables bundle duplication and fix asset-mapping tools** → [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Addressables Bundle Remediation]]; [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research|Audit 2026-10-24 findings]]
  - Status: Phases 0–6 done 2026-10-24; 0 duplicated payload (1.53 GB content). Phases 3–6 + music fix uncommitted.
  - Next: commit; user packed-player placement, audio mix/ducking, early-spawn sync-load timing.

### Refactoring

- [ ] **Derive hardpoint HP from hull HP (budget ratio, type weights, per-class minimum)** → [[TODOs/Refactoring/Hardpoint_Health_From_Hull_Plan|Hardpoint Health From Hull]]; [[TODOs/Refactoring/Hardpoint_Health_From_Hull_Plan - Research|Current Σ HP / hull table]]
  - Status: implemented 2026-10-29 (`R = 1.15`, 62 data / 61 prefabs migrated); tests not run.
  - Next: user skirmish/tuning acceptance; refresh ALO recipe HP baselines before next import.

- [ ] **Project organization follow-up** → [[TODOs/Refactoring/Project_Organization_Followup|Project Organization Follow-up]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Rebalance to original Republic at War values; remove range classes; simplify Republic and CIS hardpoints** → [[TODOs/Refactoring/Original_Values_Rebalance|Original Values Rebalance]]; [[TODOs/Refactoring/Original_Values_Rebalance - Research|Original vs live tables and range-removal impact]]
  - Status: Phases 0–4 done 2026-10-25; class win rates still uneven (Separatist cruisers 12 %); WIP uncommitted.
  - Next: user playtest; decide Empire/Rebellion DPS tuning (hardpoint simplification).

- [ ] **Split multi-type files and fix placement** (Wave 2) → [[TODOs/Refactoring/Type_File_Split_And_Placement|Type File Split and Placement]]
  - **Blocked**: Wave 1 merge; user decision on interface+impl pairs.

- [ ] **Apply injection and guard conventions** (Wave 2) → [[TODOs/Refactoring/Injection_And_Guard_Conventions|Injection and Guard Conventions]]
  - **Blocked**: Wave 1 merge; user decision on MonoBehaviour `[Inject]` style.

- [ ] **Faction scalability (per-faction data, unit/faction definitions)** → [[TODOs/Refactoring/Faction_Scalability|Faction Scalability]]
  - **Remaining**: Phase 1 playtest; Phases 2–5.

### Tooling

- [ ] **Unify ALO imports with recipe-driven tools and concurrent-agent coordination** → [[TODOs/Tooling/Unified_ALO_Import_Pipeline|Unified ALO Import Pipeline]]; [[TODOs/Tooling/Unified_ALO_Import_Pipeline - Research|Tooling audit and reuse candidates]]
  - Status: in progress 2026-10-28; job/lock/journal infrastructure and Rothana recipe implemented; `apply` disabled.
  - Next: production conversion + Editor acceptance; Endurance/Rebel recipes; station/derivative adapters; concurrency recovery acceptance.

- [ ] **Add Jenkins GitHub draft releases** → [[TODOs/Tooling/Jenkins_GitHub_Releases|Jenkins GitHub Releases]]
  - **Remaining**: user adds `github-releases` credential and selects source build/tag; first live upload and recovery acceptance.

- [ ] **Set up Jenkins local Windows builds**
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

### Documentation

## Architecture Backlog

- [ ] Audit gameplay services for direct `IUiService` or `BaseUi` references.
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

- [ ] Migrate UI creation calls from services into feature UI controllers / presenters.
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

- [ ] Ensure all feature data container classes (`<Feature>Data`) inherit from `Data` ScriptableObject.
  - Ownership/acceptance gate remains unassigned; retained as an open backlog item.

## Done

### Optimization

- [x] **Fix decorative asteroid rendering and follow up both performance audits** → [[Done/Optimization/Battle_Rendering_Performance_Audit|Battle Rendering Performance Audit]]; [[Done/Optimization/Battle_Rendering_Performance_Audit - Research|Both audits, measurements and VFX findings]]
  - **Audited 2026-10-28**: asteroid on/off/restored = 39.228 → 100.912 → 39.353 FPS; Performant = 19.318 → 48.634 → 19.350 FPS. Camera fixed within each comparison; no cross-preset regression claim.
  - **Required**: retain verified instanced asteroid materials/draws; reject asteroids wholly outside the camera frustum using transformed bounds. Distance-only filtering is insufficient.
  - **Remaining**: implement/measure culling and geometry reduction; attribute 128-sample nebula and post-processing cost; review legacy explosion materials/shadow flags; repair Frame Debugger and stale-camera capture errors. No code changes or tests in audit.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

- [x] **Optimize projectile VFX for PC without logic changes** → [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan|Projectile VFX PC Optimization]]; [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan - Research|Prefab, material and runtime variation audit]]
  - **Next**: verify equivalent head/trail material sharing, remove unnecessary bolt texture CPU copies, measure scoped fade/shader experiments and validate the full faction/mount/ability/superweapon variation matrix.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

- [x] **Apply battle CPU fixes (targeting, due events, audio mix, navigation, squadrons)** → [[Done/Optimization/Battle_CPU_Fixes_Plan|Battle CPU Fixes Plan]]; [[Done/Optimization/Battle_CPU_Fixes_Plan - Research|Phase 8 acceptance checklist]]
  - **Remaining**: matched ≥3×10 s captures at 1×/4×, controlled warm-selection/visual and combat behavior gates, Development Player confirmation, and Phase 7 visual-wait removal. Plan stays in progress.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_CPU_Performance_Plan]].

- [x] **Optimize battle attacks and projectile reuse** → [[Done/Optimization/Battle_Attack_Optimization_Plan|Attack preparation, commit and pooling]]; [[Done/Optimization/Battle_Attack_Optimization_Plan - Research|Implementation history and phase gates]]
  - **Next**: target snapshot/fallback attribution, measured shared work, due-event lookup/emission/impact breakdown, warm-pool creation/lifecycle checks; preserve combat parity.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_CPU_Performance_Plan]].

- [x] **Optimize battle particle rendering** → [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan|Effects and squadron indicators]]; [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan - Research|Verified rendering and historical navigation evidence]]
  - **Next**: combat appearance/lifecycle acceptance and measured muzzle/bolt/smoke submission work; skip broad idle-session profiling. Shadows/motion blur only after higher-priority effects.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

- [x] **Optimize Burst jobs (lengthsq, select/SIMD, scheduling)** → [[Done/Optimization/Burst_Jobs_Optimization_Plan|Burst Jobs Optimization]]; [[Done/Optimization/Burst_Jobs_Optimization_Plan - Research|17-job audit and findings]]
  - **Phase 1 harness done 2026-10-22**: perf package + `Tests/Benchmarks` (radar + target-filter if/?:/select kernels); parity 18/18; Editor timings favour branch-free except at low pass ratios.
  - **2026-10-22 execution**: target/fog, shield SoA clipping, radar eligibility/XZ layout, due predicate and navigation stamps/raster/LOS implemented. SSE2/AVX2 assembly captured; 156 distinct EditMode cases pass after Burst cache invalidation, including earlier-kernel reruns and shield-swap integration.
  - **Scheduling 2026-10-22**: bolt Update → LateTick; radar preparation overlap within FixedTick (83 distinct tests pass). Fighter PostTick → pre-combat LateTick now implemented through ordered lifecycle bridge, with spawn/pool/root-write/teardown guards; 73 distinct follow-up tests pass. Compile clean; composition logs unrelated ambience EditMode destruction errors.
  - **Remaining**: user Player baseline + ≥3 `PlayerBranchVariant*` runs, battle pass ratios, matched timing acceptance, batch/branch winners, fighter scheduling performance acceptance, Phase 6 tuning and deferred shield batching decision. Plan remains active.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_CPU_Performance_Plan]].

- [x] **Pool squadron contexts to remove launch spikes** → [[Done/Optimization/Squadron_Pooling_Plan|Squadron Pooling]]
  - **Next**: restart Editor → rerun EditMode → user checks launches/deaths/reinforcements in-game.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

### Bugs

- [x] **Fix scene-teardown pool errors and material leaks** (Wave 1) → [[Done/Bugs/Scene_Teardown_And_Material_Leaks|Scene Teardown and Material Leaks]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

### Documentation

- [x] **Add searchable note flags and extract durable guidance from high-impact plans** → [[Done/Documentation/Note_Flags_And_Durable_Guidance_Plan|Note Flags and Durable Guidance]]
  - Planned 2026-10-29: five native flags; Addressables and battle FPS pilots; known issues/solutions; shared AGENTS.md routing and plan-close handoff.
  - Completed 2026-10-29: flag contract/dashboard, source-verified pilot guides and shared routing/handoff; pilot plans retain unfinished scope. Dashboard visual gate → [[TODOs/User_Acceptance_Queue]]. No tests/builds/captures run.

- [x] **Triage backlog (close code-complete plans), then fix vault guidance and navigation** → [[Done/Documentation/Vault_Guidance_And_Backlog_Cleanup|Cleanup plan]]; [[Done/Documentation/Vault_Guidance_And_Backlog_Cleanup - Research|Audit + triage 2026-10-29]]
  - Done 2026-10-29: active plans 80 → 30, acceptance queue added, guidance G1–G8 and navigation N1–N3 fixed.

- [x] **Vault organization and GameDesign references** → [[Done/Documentation/Vault_Organization_And_GameDesign_References]]
  - Preserve settings/credentials; verify moves, current references and links.
  - Completed 2026-10-20: 65 content moves, 10 current GameDesign references, repaired navigation and historical/proposal labels; static checks passed.

### Tooling

- [x] **Battle logic simulator (Editor Hub)** → [[Done/Tooling/Battle_Logic_Simulator|Battle Logic Simulator]]

- [x] **Implement Game Design Balance Editor** → [[Done/Tooling/GameDesign_Balance_Editor|Game Design Balance Editor]]; [[Done/Tooling/GameDesign_Balance_Editor - Research|Data ownership and feasibility]]

### Features

- [x] **Main relay zone map rework: central contested relay (4 mines + 6 satellites), 1 home mine + 1 satellite, additional relay zones S2/M4/L6** → [[Done/Features/Main_Relay_Zone_Map_Rework|Main Relay Zone Map Rework]]
  - Status: implemented 2026-10-29; generation, capture-site merge, settings, prefab migration, save validation and test expectations complete.
  - Verification: Unity compilation/import and source review; tests not run. User gameplay/tests and post-acceptance GameDesign update → [[TODOs/User_Acceptance_Queue]].



- [x] **Review ship abilities implementation** → [[Done/Features/Ship_Abilities_Plan|Ship Abilities Plan]]
  - **Remaining**: implementation review and acceptance evidence; commit `dc869bbb` recorded review as needed.
  - Closed 2026-10-29 by user decision; see plan banner for unfinished scope.

- [x] **Space hazards rework: level-design bands + asteroid/dust edge continuation** → [[Done/Features/Space_Hazards_Rework_Plan|Space Hazards Rework Plan]]
  - **Implemented 2026-10-28**: shared band geometry, center membership, ribbon/minimap rendering, hazard-first fields, decorative asteroid ring and dust continuation; prefab/preset bindings saved.
  - **Verified**: 29/29 focused hazard tests, including 180 layouts across sizes/player counts and loader integration; visual captures and rock-surface clearance checks saved in `Logs/SpaceHazardsRework/`.
  - **Integration verified**: deterministic final Small-band candidates fix seed-7 sampling with satellite nodes; combined current map-generator suite `33/33` passes.
  - **Final hazard rerun**: `29/29` pass after the fallback and satellite integration; saved current-source evidence.
  - **Acceptance pending**: clean full-suite comparison, matched frame-performance check and user in-game review. Old saves regenerate with the new layout.
  - Merged 2026-10-29 into [[TODOs/Features/Space_Hazards_Plan]].

- [x] **Space hazards shape rework: organic lobed clouds + SDF geometry** → [[Done/Features/Space_Hazards_Shape_Rework_Plan|Space Hazards Shape Rework Plan]]
  - **Review 2026-10-28**: spindle profile (`sin` widths, pointed tips), straight collinear Small bands (dashed wall), cut-out shader edge + rim, flat stacked sheets, outline-tracing asteroid fringe, `HazardBand` rebuilt in 6 places.
  - **Implemented 2026-10-28**: lobe-chain clouds on curved spines, signed-distance grid shared by gameplay/fields/navigation/shader/minimap, Small islands folded across the team-equidistant axis, clumped fringe, 5-sheet noise shader with local ion flashes.
  - **Verified**: compile clean; focused fixtures 67/67 (zone, service, height, presentation, navigation, AI, decor); captures + boundary overlays in `Logs/SpaceHazardsShapeRework/`.
  - **Remaining**: `Map` category + `MapExhaustive` runs (lost to bridge disconnects), bake/overdraw measurement, `HazardType` view branches, user in-game review.
  - Merged 2026-10-29 into [[TODOs/Features/Space_Hazards_Plan]].

- [x] **Faction interface and ship ability audio — listening acceptance** → [[Done/Features/Faction_Interface_And_Ability_Audio_Plan|Faction Interface and Ship Ability Audio Plan]]
  - **Menu unload fix 2026-10-26**: cache/destroy the owned GameObject directly; no access through an already-destroyed AudioSource. Exact exception reproduced before fix; compilation clean, menu tests 10/10 after fix. User battle-load acceptance pending.
  - **Review fixes 2026-10-26**: single mine destruction cue, orbital interruption priority, cooldowns only after accepted playback, deterministic construction follow-up; simple cleanup complete, larger class split deferred. Compilation and 71/71 focused EditMode tests pass. Listening/wording acceptance remains open.
  - **Implemented 2026-10-26**: 62 mapped WAV imports (4.91 MB source), 21 ability mappings, faction announcements and priorities, restore-safe hooks, unit-ready/warnings/loss/cap/fog cues, human menu selection sounds. Republic/CIS → Rebellion voices.
  - **Verified**: compilation/import/save; EditMode Audio 63/63, Sfx 10/10, Faction 88/88, fog 1/1; health 23/24 (unrelated unchanged point-defense timer test fails at EditMode time zero). Counts overlap; evidence in `Logs/FactionAudio/`.
  - **Startup fix**: menu mixer lookup now selects exact `Sfx`, excluding child `Ducked`; compiled; focused menu tests now pass 9/9, including real-mixer initialization.
  - **Audio tuning**: ability gain doubled (166 saved volume values); ship-loss speech requires current camera visibility or ≤5 s since seen, evaluated at actual death. Announcement tests 22/22 passed. Production-order voices now imported/wired across four factions (Empire/Separatist: “Constructing Unit”; Republic/Rebellion: “Unit in production”); announcement tests now 30/30 passed.
  - **Building/mix/ion follow-up**: mining/battle asteroid start voices wired; shield cues centered 2D/high-priority and independent of weapon burst budget; music ducks to 20% under speech; native orbital ion firing SFX wired. Tests: announcements 36/36, ShipSfx 18/18, construction 13/13, superweapons 17/17, music duck 1/1. Plasma effect selected/wired; ion/hypervelocity now use dedicated charging/ready/firing voices, plasma ready now says “Magnepulse Cannon Available” (user-selected 2026-10-28); Planetary Turbolaser charging/firing remain. Superweapon tests now 44/44.
  - **Remaining**: user listening acceptance (volume/priority/timing/duplicates), verify `I000_EHD5503_ENG.wav` wording, choose optional ability-recharge/superweapon-ready replacements. No agent Play Mode.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Extend audio system with Republic at War SFX (interface, orbital announcer, ambience, shields, per-unit engines/explosions, alarms)** → [[Done/Features/Audio_System_Extension_Plan|Audio System Extension Plan]]; [[Done/Features/Audio_System_Extension_Plan - Research|Clip inventory and unit mapping]]
  - **Implemented 2026-10-22**: 33 mapped imports, interface/orbital cues, Burst ambience, shield/ability sounds, per-unit engines/explosions and alarms; compile passed, 73 distinct recorded EditMode cases passed.
  - **Remaining**: user in-game listening acceptance (balance, flyby loop seams, shield cadence, ambience fade and announcer timing); no agent Play Mode.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Replace battle and menu music with categorized Republic at War tracks** → [[Done/Features/Music_Replacement_Plan|Music Replacement Plan]]; [[Done/Features/Music_Replacement_Plan - Research|Exact track inventory]]
  - **Implemented (2026-10-22)**: 22 battle + 31 menu tracks; 41 legacy MP3s removed; Vorbis streaming; loading-state fix; 8/8 tests and Windows Editor menu playback/pause/mute/completion smoke passed. Windows build Succeeded (3.80 GiB), built playlists verified; unrelated planet shader error recorded.
  - **Remaining**: standalone/battle-scene and listening acceptance, strict single-source review (pre-existing idle scene clone), representative audio profiling; macOS build module/runtime unavailable.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Game modes: Survival and Battle Simulation** → [[Done/Features/Game_Modes_Survival_And_Simulation|Game Modes Survival And Simulation]]
  - **Remaining**: Phase 0 full-suite green gate: 13 SensorJamming allowlist failures, Allegiance radar/weapon-scope checks, and stale entity-prefab count. Plan stays active for these pre-existing failures.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **AI composition analysis and counter measures** → [[Done/Features/AI_Composition_Counters|AI Composition Counters]]
  - **Remaining**: skirmish Play Mode acceptance and threshold tuning against current balance.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify MC80 Independence integration acceptance** → [[Done/Features/MC80Independence_Import|MC80 Independence Import]]
  - **Remaining**: clean-skirmish acceptance and provisional balance/source-difference review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify BTL-A4 Y-Wing Bomber integration acceptance** → [[Done/Features/YWingBomber_Import|BTL-A4 Y-Wing Bomber Import]]
  - **Remaining**: clean-skirmish acceptance; provisional economy/weapon/flight balance and source-difference review. Passive AOTR ion-stunner slowdown is not ported.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify ArquitensImperialCruiser integration acceptance** → [[Done/Features/ArquitensAdvanced_Import|ArquitensImperialCruiser Import]]
  - **Remaining**: clean-skirmish acceptance and remaining provisional balance/source-difference review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify Victory II integration acceptance** → [[Done/Features/VictoryII_Import|Victory II Import]]
  - **Remaining**: clean-skirmish acceptance; review provisional balance and documented engine-effect precision/source differences.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify Victory I integration acceptance** → [[Done/Features/VictoryI_Import|Victory I Import]]
  - **Remaining**: clean-skirmish acceptance and provisional scale/movement/weapon/system/ability/economy/launch balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify MC75 Profundity integration acceptance** → [[Done/Features/MC75_Profundity_Import|MC75 Profundity Import]]
  - **Remaining**: clean-skirmish hardpoint/combat/launch/ability/destruction acceptance and provisional balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify T-65 X-Wing import acceptance** → [[Done/Features/XWing_Import|X-Wing Import]]
  - **Remaining**: clean-scene in-game acceptance and provisional movement/range/balance review. No automated tests or Play Mode run.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify Imperial Victory integration acceptance** → [[Done/Features/Victory_Import|Victory Import]]
  - **Remaining**: clean-skirmish acceptance and provisional scale/movement/weapon/system/limit/launch balance review; supplied hull retains Republic roundels.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify V-Wing squadron integration acceptance** → [[Done/Features/VWing_Import|V-Wing Integration]]
  - **Remaining**: clean-skirmish acceptance and provisional flight/repair/weapon/tech/limit/matchup balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify IPV-2C Stealth Corvette integration acceptance** → [[Done/Features/StealthCorvette_Import|Stealth Corvette Import]]
  - **Remaining**: clean-skirmish cloak/combat/placement/destruction acceptance and provisional scale/movement/weapon/armor/audio balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify Imperator-class Star Destroyer integration acceptance** → [[Done/Features/Imperator_Import|Imperator Import]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify Dispatcher-class Frigate integration acceptance** → [[Done/Features/Dispatcher_Import|Dispatcher Import]]
  - **Remaining**: clean-skirmish acceptance and provisional movement/weapon/system/limit/matchup balance review; source death ALA remains outside static conversion.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify C-9979 Lander integration acceptance** → [[Done/Features/C9979_Import|C-9979 Import]]
  - **Remaining**: clean-skirmish acceptance and provisional balance review; project uses Corvette damage category because Transport armor is unavailable.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify BTL-B Y-Wing integration acceptance** → [[Done/Features/YWing_Import|Y-Wing Import]]
  - **Remaining**: clean-skirmish acceptance and provisional ability/repair/economy/weapon/flight balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Import Resolute hero Venator** → [[Done/Features/Resolute_Import|Resolute Import]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Import CIS Patrol Frigate** → [[Done/Features/Patrol_Frigate_Import|Patrol Frigate Import]]
  - **Remaining**: in-game acceptance and provisional movement/hardpoint balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Import Captor-class Carrier** → [[Done/Features/Captor_Import|Captor Import]]
  - **Remaining**: in-game acceptance, including Vulture/Droid Bomber launches, and provisional balance review.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Implement UI tooltip system** → [[Done/Features/Tooltip_System_Plan|Tooltip System Plan]]; [[Done/Features/Tooltip_System_Plan - Research|Tooltip coverage catalog and codebase findings]]
  - **Remaining**: in-game acceptance (disabled cards, live refresh/rebind, fog/drag/HUD/route lifecycle, screen edges and teardown). EditMode tests authored; execute only on request.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **More mining sites; 2 home mines beside each station + central mines** → [[Done/Features/More_Mining_Sites|More Mining Sites]]
  - **Implemented 2026-10-26**: counts 8/10/14 with 1/1/2 central pairs; second home mine steps outward until clear; Small pocket 100, Large spacing 300 to fit 3–4 players. Map 23/23, save 21/21, survival-spawn 2/2 EditMode pass.
  - **Remaining**: user in-game check of generated maps and economy/AI feel.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Close settings audit follow-ups** (Wave 1) → [[Done/Features/Settings_Audit_Followups|Settings Audit Follow-ups]]; [[TODOs/Features/Settings_Implementation_Plan|Settings Implementation Plan]]
  - **Remaining**: remaining settings core EditMode tests; live category/rebind interaction; manual restart-persistence, display Keep/Revert/timeout, and corrupt-file checks.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Unified on-map structure construction** → [[Done/Features/Structure_Construction_Flow|Structure Construction Flow]]
  - Follow-up 2026-10-28: restored human structure production delay, added white construction silhouette, disabled construction hardpoint markers/picking; Unity compilation and shader checks clean. Tests not run.
  - Completed 2026-10-28: immediate spawn, shared health, hologram reveal and saved progress; compilation/shader checks and 12 prefab reloads clean. Tests not run.

- [x] **Import Wavecrest-class frigate into CIS** → [[Done/Features/Wavecrest_Import|Wavecrest Import]]
  - 2026-10-27: 17 mounts and Medium weapons; source art/data integration verified. Power to Shields added:20s active/60s recovery,300 shields/s,20% speed penalty; saved bindings/audio checks pass. No Play Mode or automated Unity tests run.

- [x] **Add Peleset-class Cruiser to CIS** → [[Done/Features/Peleset_Import|Peleset Import]]
  - Completed 2026-10-27: complete art/roster integration, 15 targetable mounts, Medium weapons, cruiser regeneration and per-mount 500 HP turrets.
  - Source round trip, saved asset/HP/reference checks and renders pass; no automated tests or Play Mode run.

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

- [x] **Finish ship movement simplification verification and remaining phases** → [[Done/Refactoring/Ship_Movement_Simplification_Plan|Ship Movement Simplification Plan]]
  - **Remaining**: reconcile the recorded initialization-order retry and later phases against current behavior.
  - Closed 2026-10-29 by user decision; see plan banner for unfinished scope.

- [x] **Reconcile SkirmishOrchestrator refactor acceptance** → [[Done/Refactoring/SkirmishOrchestrator_Refactoring_Plan|SkirmishOrchestrator Refactoring Plan]]
  - **Remaining**: route, visibility, teardown, and prefab verification; implementation exists despite the historical proposed status.
  - Closed 2026-10-29 by user decision; see plan banner for unfinished scope.

- [x] **Coordinate audit remediation (parallel plans)** → [[Done/Refactoring/Audit_Remediation_Coordination|Audit Remediation Coordination]]
  - **Remaining**: Wave 1 (6 plans) → Wave 2 (2 plans). Input system deferred.
  - Closed 2026-10-29 by user decision; see plan banner for unfinished scope.

- [x] **Normalize ship scales from original length targets (Venator = 1137 m)** → [[Done/Refactoring/Ship_Scale_Original_Values_Plan|Ship Scale Original Values Plan]]
  - **Implemented 2026-10-26**: 27 ship/wreck pairs and geometry data normalized; Home One/MC80 Independence unchanged, Captor restored, Patrol Frigate 400 m. All 56 dimensions and 253324 dependency checks passed; engine particle geometry 98/98 passed.
  - **Remaining**: Unity tests and in-game visual acceptance. User requested commit with Unity tests pending; plan stays active.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Weapon taxonomy and balance system** → [[Done/Refactoring/Weapon_Taxonomy_Balance_System|Weapon Taxonomy and Balance System]]; [[Done/Refactoring/Weapon_Taxonomy_Balance_System - Mapping|Mapping]]
  - **Remaining**: user playtest; balance tuning (stations outranged, ISD II +200 %, missile reach vs radar); resolve existing StrikeFighterProtonTorpedo variant metadata.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Migrate Zenject to VContainer** → [[Done/Refactoring/Zenject_To_VContainer_Migration|Zenject to VContainer Migration]]; [[Done/Refactoring/Zenject_To_VContainer_Migration - Research|Injection audit, scopes, factories and lifecycle mapping]]; [[Done/Refactoring/UI_Service_Refactoring|UI rework]]
  - **Cleanup 2026-10-22**: removed 24 stale generated projects (Zenject, legacy MCP/framework/samples, Postprocessing and an obsolete URP test player project) and refreshed the IDE solution/editor inventory. `com.script-utils.math` still names its active math assembly `ScriptUtils.Zenject`; upstream naming correction remains.
  - **Remaining**: matched startup/spawn/teardown/frame measurements. `9c072d3c5` facade-lookup allocation fix confirmed (GC median 89 → 27 KB/frame) and per-tickable markers restored. [Capture](<F:/Private/empire-at-war/Logs/BattleAudit/20261013T1854Z-vcontainer/analysis.md>)
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Verify ship and squadron entity simplification behavior** → [[Done/Refactoring/Ship_Squadron_Entity_Simplification_Plan|Ship and Squadron Entity Simplification Plan]]
  - **Remaining**: recorded behavior checks; implementation and compilation were previously recorded as complete.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Approve Ship Lit Autodesk conversion visuals** → [[Done/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan|Ship Lit Autodesk Material Conversion Plan]]
  - **Blocked**: recorded user visual approval of `Logs/ShipLitConversion/2026-09-28/review.html`.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Deduplicate capture/reinforcement squadron tally** (Wave 1) → [[Done/Refactoring/Capture_Reinforcement_Tally_Dedup|Capture Reinforcement Tally Dedup]]
  - **Remaining**: code + tests committed; manual capture/zone playtest pending.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Type unit request / reinforcement identity keys** (Wave 1) → [[Done/Refactoring/Unit_Request_Identity_Keys|Unit Request Identity Keys]]
  - **Remaining**: code committed (`cd7b6edb`), compiles clean; manual build-queue + all four reinforcement kinds playtest pending.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Remove runtime component lookups** (Wave 1) → [[Done/Refactoring/Explicit_Component_Binding|Explicit Component Binding]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Refactor audio system (AudioService / Music / Ship SFX)** → [[Done/Refactoring/Audio_System_Refactor_Plan|Audio System Refactor Plan]]
  - **Remaining**: step 8 listening, ≥20-ship profiler, pause/music/exit acceptance. Open `MainMenuScene` has unsaved changes; no Play Mode or automated tests run.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Refactor camera system (input guard, smoothing util, scroll fix, cinematic data)** → [[Done/Refactoring/Camera_System_Refactor_Plan|Camera System Refactor Plan]]
  - **Remaining**: manual lock/release, zoom/invert/rebind and cinematic visual acceptance. `MainMenuScene` was clean on 2026-10-09; no Play Mode run.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Battle startup sequence (state/speed notifiers, async map, gated systems)** → [[Done/Refactoring/Battle_Startup_Sequence_Plan|Battle Startup Sequence Plan]]
  - **Remaining**: Play Mode acceptance (fade, fog, camera drift, hitch, Zenject graph, Esc during loading, pause/speed/menu/end/exit). Fader added 2026-10-04.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Rework UI before VContainer migration** → [[Done/Refactoring/UI_Service_Refactoring|UI Rework Before VContainer]]; [[Done/Refactoring/UI_Service_Refactoring - Research|UI inventory, placement and SoulsLikeTemplate comparison]]

- [x] **Resolve codebase audit 2026-09-27** → [[Done/Refactoring/Codebase Audit 2026-09-27/00 Overview|Codebase audit overview]]; [[Done/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|the re-audit]]

- [x] **Fix Project Organization & Asset Naming Defects** → [[Done/Refactoring/Project_Organization_Remediation_Plan|Project Organization & Asset Naming Remediation Plan]]; [[Done/Refactoring/Project_Organization_Remediation_Plan - Research|Full Audit & Naming Mapping Catalog]]
