# Project TODOs & Backlog

- Canonical execution index. Plans own progress, blockers and verification; [[GameDesign/00_GameDesign]] owns current gameplay references.
- Detailed pre-reorganization entries: [[Artifacts/History/Backlog_Before_2026-10-20]]. No task status was changed by index compression.

## Active Tasks

- User-only acceptance gates of closed plans → [[TODOs/User_Acceptance_Queue]].

### Features

- [ ] **Ship hull damage visuals (scorches, burnt plating, breaches in `Ship Lit`)** → [[TODOs/Features/Ship_Hull_Damage_Shader_Plan|Ship Hull Damage Shader Plan]] — 2026-11-20: Phase 1 implemented and tuned with the user (procedural soot scorches, off-centre sunken burnt patches with glowing rim, 31 steps, per-ship auto mark scale). Perf review + optimizations R1–R6 done (plane pre-test, shape skip, far-ship LOD, cheap activation hash, quality tier, dead texture removed). Tests not run. Next (user): in-game check, GPU capture, Phase 2 decision.
- [ ] **Ship UI: icon double-tap camera focus (off-screen only) + tap group entry → select that single unit** → [[TODOs/Features/Ship_Ui_Icon_Focus_And_Entry_Select|Ship UI Icon Focus & Entry Select]] — 2026-11-19: plan drafted; 3 open questions (hero guard, on-screen behavior, collapsed groups).
- [ ] **Squadron icon clustering rebuild (screen-space matrix grid, smooth, ship-aware, cheap)** → [[TODOs/Features/Squadron_Icon_Clustering_Rebuild|Squadron Icon Clustering Rebuild]] — 2026-11-18: phases 1–6 implemented and compiled (tests not run); remaining: user capture, station hull obstacles, optional mesh batching.
- [ ] **Squadron bay rotation: ship-wide active squadron cap (Bulwark = 1)** → [[TODOs/Features/Squadron_Bay_Rotation_Plan|Squadron Bay Rotation Plan]]
  - 2026-11-18: Gemini per-bay data edit reverted (`8925de639`); code/data back to pre-Gemini state. Plan: Phase 1 per-ship `HangarMaxActive` code, Phase 2 values (12 ships → 0, 28 → 1), Phase 3 cap-aware AI/preload readers, Phase 4 rotation for all except Venator/VenatorII/Lucrehulk/Secutor/MC80Independence/HomeOne/ImperialVenator/ISDI/ISDII. Bulwark Mk1 = 0 (confirmed).
- [ ] **Forbidden UI SFX** → [[Done/Features/Ui_Sfx_System#Deferred follow-up|Deferred forbidden cue]].
- [ ] **Dynamic in-game tutorial (in-match hints: camera, select, move, minimap, build, counters tooltip, build queue, abilities + auto-power, level up, research, map systems)** → [[TODOs/Features/Ingame_Tutorial_Plan|In-Game Tutorial Plan]]
  - UI spotlight migration 2026-11-11: shared UI frames replaced by a 65% dim overlay with target cutout; added per-element `IUiHighlight` API and own-graphic 1.2 s pulse (compile/bindings verified, visual check pending); optional pointer blocking defaults off. Loading-time minimap `HideTarget` exception fixed 2026-11-11 with marker lifecycle tracking; compiled, startup replay pending. User-authorized tests: Ui 274/274 and Core 150/150 passed; visual acceptance pending.
  - Status: in progress. Phase 0 decided 2026-11-09: in-match hints only (guided battle deferred), plain text, text-only coach card (layout A) + pulsing ring, 3 s gap, basics pause during combat. Phases 1–7 + settings done 2026-11-09 (compiles clean; tests not run; not run in-game): hint card UI, HUD anchors, 10 basics + 18 event hints, Show/Reset hints in Settings, Turn off on card. Remaining: audio cue (needs clip), optional analytics, tests, user in-game check.
  - Input glyphs added 2026-11-09: white mouse/keycaps, red active mouse control, live settings bindings and station-selection prerequisite prompts. Imported/saved and visually previewed; tests not run.
  - Onboarding redesign 2026-11-09: lessons of small steps, auto-inserted prerequisite steps (select unit / station), world targets (mine near station, satellite pad), mine-first order, pause/speed, shortcuts, split research, level up after station. 50 steps, 14 onboarding + 8 event lessons. Compiles clean; tests not run; in-game check pending; audio cue remains open.

- [ ] **Guided tutorial battle (`GameMode.Tutorial`, Play menu button, 16 chapters, weak controlled Easy enemy, central 4-mine zone → enemy mine → destroy station)** → [[TODOs/Features/Guided_Tutorial_Battle_Plan|Guided Tutorial Battle Plan]]
  - UI spotlight migration 2026-11-11 uses the shared hint view; per-element `IUiHighlight` API and own-graphic pulse added (compile/bindings verified, visual check pending); world markers preserved. Ui 274/274 and Core 150/150 passed; in-game acceptance pending.
  - Status: in progress 2026-11-10. Step 0 done (mode class by `GameMode`, `BattleSetupSave.Supports`). Step 1 done: `GameMode.Tutorial`, `TutorialMode`, `TutorialPreset`, Play menu Tutorial button + faction pick; user confirmed the battle loads. Enemy mine: no existing path, options recorded. Tests not run. Step 2 done: tutorial hint track (fresh progress, always shown) + `TutorialDirector` (enemy groups after lessons, money top-up); compiles clean, not run in-game. Playtest 1 fixes: lessons for capturing a mining site, building facilities to the limit, levelling to max with new units; higher money floor. Playtest 2: facility Shift+drag chain, build-fleet lesson, central zone, expansion and enemy-station lessons, deploy marker beside the station. Playtest 3: facility card mark, open-panel/selected checks, order-based move steps (minimap too), 3× tutorial build speed, level-up filler lessons (home mine, satellite, more ships), Shift deploy hint. Playtest 4: fixed early-listening bug (steps completing unseen), live [count], Shift + LMB chord on stack cards, our-side expansion target, build-deploy-capture expedition, select-all attacks. Next: user playtest; then ship-action lessons, first-time popup, Skirmish setup hints, tutorial map.

- [ ] **Online multiplayer (Mirror host-authoritative, 20 Hz tick, Steam relay via FizzySteamworks) alongside offline** → [[TODOs/Features/Multiplayer_Plan|Multiplayer Plan]]; [[TODOs/Features/Multiplayer_Plan - Research|Research]]
  - Status: in progress 2026-11-07. Scope: Skirmish (PvP/co-op, AI slots) + Survival co-op; 2–4 friends. Done: Mirror 96.11.3 + FizzySteamworks 6.0.1 in `Assets/Plugins/Mirror`, Steamworks.NET 2025.164.1 (UPM); Phase 0 spike scene `Assets/Sandbox/Multiplayer/MultiplayerSpike.unity` (code compiles; not run); Phase 1 `IMatchSession`/`OfflineSession`, issues #1, #2, #3, #13 (N-human rules, `Humans`, AI-ally production host, `HumanPlayerScope` sim/local split). Phase 1 code complete 2026-11-07 (#1–#6, #9, #13, #14): command sink for every player action (orders, production, construction, super weapons, abilities, reinforcement deploy), sequential authority ids, per-team victory, Survival co-op (Medium/Large). EditMode: no new failures vs baseline (18 pre-existing). Next: user smoke-tests offline Skirmish/Survival + runs the Phase 0 spike on two machines; then Phase 2 (needs lobby UI design sign-off) and Phase 3 (needs spike bandwidth numbers).

- [ ] **Finish settings implementation** → [[TODOs/Features/Settings_Implementation_Plan|Settings Implementation Plan]]
  - Remaining implementation and planned checks live in the settings plan. Completed audit follow-ups have their own entry under Done.

- [ ] **Gameplay analytics (UGS Analytics: match wins, factions, duration, performance)** → [[TODOs/Features/Gameplay_Analytics|Gameplay Analytics]]
  - Status: in progress 2026-11-02; Phases 0–3 code done (packages, `IAnalyticsService`, UGS adapter, consent popup + Settings toggle, match/abandon/survival-wave tracking, per-minute performance samples; `BattleSimulation` excluded). Next: user links UGS project + checks popup in-game; Phase 4 schemas/dashboards/dev-build verification.

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
  - Ion Pulse now uses a 10 s stun and fills the ring with the existing stun lightning. Unity asset bindings verified.
  - **Remaining**: exact Vulture/bomber squadron assets and launches; in-game combat, placement, ability and destruction acceptance; provisional balance review.

- [ ] **Defense satellites: capturable pads around mining points, 5 satellite types, AI use** → [[TODOs/Features/Defense_Satellites_Plan|Defense Satellites Plan]]
  - Status: implemented 2026-10-28; satellite prefabs have uncommitted changes.
  - Next: commit prefab WIP; user in-game acceptance (combat, visuals, capture/build flow, AI).

- [ ] **Space hazards: Nebula / Ion Storm map areas (+ future map-wide events)** → [[TODOs/Features/Space_Hazards_Plan|Space Hazards Plan]]; [[TODOs/Features/Space_Hazards_Plan - Visuals|Visual art direction]]
  - Status: implemented; single hazards owner since 2026-10-29 (Rework + Shape Rework merged; current geometry = Shape Rework).
  - Next: IonStorm branches → data/material; SDF bake timing. User review/tests → queue; decor perf → FPS Recovery.


### Bugs

- [ ] **Hardpoint-based weapon reach (Praetor/big ships hold with guns out of range; radar + planar leftovers)** → [[TODOs/Bugs/Hardpoint_Based_Weapon_Reach]] — 2026-11-20: multipliers baked on 16 big-ship prefabs, radar raised on 15 ships; planar leftovers and in-game check remain.
- [ ] **Enemy AI economy strategy** → [[TODOs/Bugs/Enemy_AI_Economy_Strategy]]; follows [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]]
  - Status: in progress (2026-11-19). X1–X5 (`8e2ebefe6`, `28d1a41fd`) and review fixes R1–R9 (`89810dcb2`) committed; map M2/M4/M5 in `ac7ff075c`. EditMode: EnemyAi 503/503, Capture, Map pass. 17:31 battle lost → objective-selection rebuild P1–P7 implemented 2026-11-19 (zone verdicts Go/Assemble/Deny, pull-back to gathering point). 19:05 battle: same issues (allocator missing) → N1–N6 implemented 2026-11-19 evening: ship allocator (one reserve + one force per zone), commit at an even fight, unscouted-zone estimate, route rule, pull-back fix, sabotage when behind, squadron power rule, satellites counted as defenders; `EnemyAi` EditMode 518/527 → 3 stale fixtures fixed → `EnemyUnitCommanderTests` 146/146. Open: tests for R3/R5/R6, UltraHard 1v1 Small battle, then 2v2 Medium + 1v1 Large → [[TODOs/Bugs/Enemy_AI_Economy_Strategy#Status 2026-11-19 (current)]].

- [ ] **Capital attack movement + class rework fixes** → [[TODOs/Bugs/Capital_Movement_And_Class_Rework_Fixes]]
  - Status: in progress (2026-11-17). Done: map seed/layout log (`BattleMapTelemetry` → `_map.csv`). ISD I spawn fixed and user-confirmed 2026-11-17 (a station-centred origin tied between the main region and a corner pocket; the larger region now wins). Next: Phase 1/2 movement issues and the uncommitted class-rework edits. Then the Titan/Allegiance freeze, Executor holding out of reach, Tector stern-first retreat at stations, and class-rework review defects.
- [ ] **[AGY] Weapon Fire Rate Degradation and Ability Sync Crash at Low FPS** → [[TODOs/Bugs/Weapon_Fire_Rate_And_Network_Sync_Low_FPS_AGY|Plan]]
- [ ] **[AGY] Capital Movement and Class Rework Fixes (Audit & Reconciled Plan)** → [[TODOs/Bugs/Capital_Movement_And_Class_Rework_Fixes_AGY|Plan]]
- [ ] **[AGY] Ship Attack Order Hold-in-Range Band and Formation Offset Fix** → [[TODOs/Bugs/Attack_Order_Hold_In_Range_And_Formation_Offsets_AGY|Plan]]
- [ ] **[AGY] Enemy AI Mid-Fight Base Defense at Level 3 and Difficulty Consistency** → [[TODOs/Bugs/Enemy_AI_Base_Defense_And_MidGame_Consistency_AGY|Plan]]
- [ ] **Weapon fire rate drops at low FPS (shots gated on visual projectile pool, 1 bolt/frame growth)** → [[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]]
  - 2026-11-18: cause from source after online battles 2026-11-17 (logs `Logs/OnlineBattles/20261117T1650Z/`). Nothing implemented; Phase 1 measurement next.
- [ ] **Attack order: hold while in range, no run-away / map-edge legs** → [[TODOs/Bugs/Attack_Order_Hold_In_Range]]
  - 2026-11-18: attack arc, hold band, same-target refresh and telemetry implemented. The 18:25 battle logged 453 inward or level approach legs and one permitted outward hull-contact retreat. Active: launcher-only artillery fallback, retreat facing verification, and affected Navigation/BattleInput/Combat regression coverage.
- [ ] **Squadron icon anchor lag (0.8 s filter outside Travel mode)** → [[TODOs/Bugs/Squadron_Icon_Anchor_Lag|Squadron Icon Anchor Lag]] — 2026-11-18: plan created; not started.
- [ ] **Squadron FPS (icon stacking/grid out of scope: user rebuild)** → [[TODOs/Bugs/Squadron_Count_Stacking_And_FPS]]
  - 2026-11-18: 1v1 Editor peak 127 live squadrons (≈1,000 craft). Fixed (compiled, prefabs saved, tests not run): torpedo-mount per-frame target selection; removed 334 engine trails, 186 fighter colliders, squadron world selection rings (icon-only selection), 212 dead flash meshes, 234 fighter shadow casters. Next: fighters out of root hierarchy, mesh merge/LOD, remove model-embedded muzzle-flash meshes from all models (12 model files + reinforcement/ship prefabs), split fighter/ship projectile pools, radar per-frame tick.
- [ ] **Enemy AI weak base defense + inconsistent UltraHard** → [[TODOs/Bugs/Enemy_AI_Base_Defense_And_Consistency]]
  - 2026-11-18: UltraHard `defenseThreatRatio` 1.5 (highest bar) + whole-fleet commit. "lvl 3" meaning to confirm.
  - 2026-11-18 playtest: AI order churn ≈14k orders/32 min; patrol corvette re-ordered every 0.5 s and flip-flopped with the fleet (the "random direction" ships) → fixed in `EnemyPatrolShipController`. Spikes: AI capture probe +19.7 ms, patrol select +7.9 ms (see Live CPU Sweep). Logs: `Logs/Playtests/20261118T1013Z/`.
- [ ] **Ship hardpoints, firing angles and balance** → [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]]
  - Status: in progress (2026-11-15). Recusant weapon profile swaps saved and verified with existing arcs. Recusant battle output, Peleset/Recalcitrant wrapped yaw, Dispatcher/Resolute/Arquitens arcs, Mandator economics and shared-prefab telemetry remain under review.


- [ ] **Rebalance carrier deployment and squadron stats** → [[TODOs/Bugs/Carrier_and_Squadron_Rebalance|Carrier and Squadron Rebalance]]
  - 2026-11-14 exact target sheet: 17 regular types with craft/DPS/hull/shield/recovery/weapon budgets; all 34 carriers assigned 1–2 squads per supported role plus total stock. CIS fixed at 13/11/10 F/I/B; per-craft quality verified Rebel > Republic > Empire > CIS. [[TODOs/Bugs/Carrier_and_Squadron_Rebalance - Unit Amount Research|Calculations]]. Implemented:17 regular stats,14 resized prefabs,34 carrier targets, descriptions and AI live/pending launch counting. Unity compilation, saved bindings and live DPS verified; tests not run. Station/purchase economics and gameplay acceptance remain.
  - Reopened 2026-11-12 after failed fresh-match balance acceptance. Original 29 loadouts match; telemetry confirms Republic/Rebel squadron kill advantage. Implemented 1 s CIS launches, no loss-triggered cooldown reset, 5 s recent window and faction station counts 5/4/3/2. Applied approved direct-purchase prices. 2026-11-14 live/reserve counting and34-carrier coverage implemented. Remaining: strength playtest and station/purchase economics; [[TODOs/Bugs/Carrier_and_Squadron_Rebalance - Research|audit evidence]].

- [ ] **Fix defense satellite review findings** → [[TODOs/Bugs/Defense_Satellites_Review_Fixes|Defense Satellites Review Fixes]]; parent: [[TODOs/Features/Defense_Satellites_Plan|Defense Satellites Plan]]
  - Status: fixes #1–#10 implemented 2026-10-28; #3 kept by user.
  - Next: user in-game check (#4 build order, #5 raids, #10 cues, #11 rings, #12 home mines).

- [ ] **Fix enemy AI losing battles and CIS vs Republic balance** → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance|Enemy AI and Faction Balance]]; logs: [[Tooling/Battle_Telemetry|Battle Telemetry]]
  - Evening19:09 CIS battle audit: old-home spawning after primary station loss, retreat→Hold fleet loss and capture oscillation; squadron damage gap3.66×. Fix anchoring/recovery before broad balance tuning; source/log verified, no gameplay edits/tests. Evidence in the dated review's Evening Battle section.
  - Old-home rebuild / recovery placement now owned by [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]] (2026-11-14).
  - 2026-11-13 audit: rapid raid/site-defense switching and order/retreat follow-ups → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review 2026-11-13]]. Ship findings now owned by [[TODOs/Bugs/Ship_Hardpoint_And_Balance_Plan]].
  - Done (commit `6babee5e1`): Republic torpedo nerf, CIS hangar/squadron buffs, Concentrate Fire nerf, AI squadron escort, strength, rally, production, press-advantage and retreat-memory fixes; AI EditMode tests pass.
  - 2026-11-01: mining raid priority/strength checks, capture-squadron reservation and early captured-mine construction fixed; tests authored, not run. Comparison → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Research]].
  - Target-specific task forces implemented 2026-11-01: counter-based strike selection, assembly readiness, retained membership and guard screen. Runtime acceptance pending; tests authored, not run.
  - Battle follow-up implemented 2026-11-01: caliber/population valuation, home defense deployment, persistent main-force assembly and active bounded screens. Latest 17:27 battle: capital buying and main-force attacks improved; siege response, emergency production and capture clearance remain open; parent retains broader balance scope. Evidence/priorities → [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review]].

- [ ] **Replace silent null returns with fail-fast/Try APIs** (Wave 1) → [[TODOs/Bugs/Fail_Fast_Null_Returns|Fail-Fast Null Returns]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

### Optimization



- [ ] **Online battle low FPS (3 humans vs 4 AI, 10–20 FPS) + release-build telemetry** → [[TODOs/Optimization/Online_Battle_Low_FPS]]
  - 2026-11-18: logs archived and split per battle; release `Player.log` has no timing data. Decided: keep `Maximum Allowed Timestep` 0.333 (no slow motion above 3 FPS); move radar scans from fixed steps to one batched pass per frame. Next: Phase 0 release telemetry, Phase 1 Development capture.
- [ ] **[AGY] Online Battle 10-20 FPS Host and Client Performance Optimization** → [[TODOs/Optimization/Online_Battle_Low_FPS_Host_And_Client_AGY|Plan]]

- [ ] **Live battle CPU sweep: AI decision spikes, stalls/GC, pool pre-grow, attack state, squadrons, steady costs** → [[TODOs/Optimization/Live_Battle_CPU_Sweep_Plan|Live Battle CPU Sweep Plan]]
  - 2026-11-16: plan written from post-fix captures (frame avg 14.0 ms, p95 26.3, max 420; main-thread bound). Nothing implemented. Next: Phase 0 baseline + user test run, then Phase 1 AI decision markers.

- [ ] **Shared team AI: world snapshot, one planner per team, budgeted AI scheduler, allied cooperation** → [[TODOs/Optimization/Shared_Team_AI_Plan|Shared Team AI Plan]]
  - 2026-11-16: Phases 1–3 committed in `434b9d97a`. HUD teardown and shared AI fixture fixes committed. User confirmed full EditMode passed. Full-route capture evaluation and scheduler/zone cleanup committed in `b1d549d07`; reviewed, no regressions found; `EnemyAi` rerun pending for its 2 new cases. Three-AI battle and scaling checks deferred for the code-only pass. Phase 4 still requires item approvals.

- [ ] **Remove recurring allocations and redundant scans; gate Jobs/Burst candidates behind captures** → [[TODOs/Optimization/CPU_Allocation_And_Jobs_Plan|CPU Allocation and Jobs Plan]]
  - 2026-11-14 · Phases 1–2, Phase 3 scan/HUD work and Phase 5 loading/editor work committed; independent source review found no actionable correctness regressions. Last Unity compilation reports no errors; committed diff check clean. Tests not run; coverage gaps and matched Editor/Development Player captures remain. Replicated-state timer design and measured Jobs/Burst candidates remain active.

- [ ] **Ship facing solve spikes (35.2 ms of a 60.5 ms frame; once-a-second ship-order spikes)** → [[TODOs/Optimization/Ship_Facing_Solve_CPU_Plan|Ship Facing Solve CPU Plan]]
  - 2026-11-14: fast yaw checks, Burst sweep, refresh budget and reusable health buffer implemented. Unity compiled. In-range probe max 1.11 ms; dense 40×30 probe max 4.16 ms. Exact intervals and matched crowded-battle verification remain active.

- [ ] **November 6 optimization fixes — P0 hitch attribution remains** → [[TODOs/Optimization/Optimization_Audit_Fix_Plan|Retained fixes, evidence and next action]]. Mac content −48.7 MB; 54 duplicates → 0; packed memory, loading, smoke, music and nebula fixes verified. Rare large fleet stalls remain unresolved.
  - 2026-11-07: capture metadata and scoped retained fixes verified. P0 native 296 ms stall attributed to synchronous AssetBundle waiting; exact key and other stalls remain open. Separate satellite warmup omission fixed (43–50 ms → 0.0022 ms in isolated packed probe). No general FPS improvement established; tests disabled.
  - Follow-up: new 576 ms / 1.8 s stalls traced to synchronous Metal GPU-program creation. Original 296 ms asset key and historical 198 ms cause remain unresolved. [[TODOs/Optimization/Optimization_Audit_Fix_Plan - Research|All findings, fixes and evidence]].

- [ ] **Remove battle-start stutter at reveal (deferred asset warm-up after `Running`)** → [[TODOs/Optimization/Battle_Start_Hitch_Plan|Battle Start Hitch Plan]]
  - 2026-11-06: Editor Profiler skirmish start: 25 frames > 25 ms between −2.6 s and +4.4 s around activation; visible window +3.9–4.4 s = Addressables integration (35–117 ms) matching `BattleAssetWarmup`. Next: Phase 0 markers, then Development Player baseline. Nothing implemented.

- [ ] **Recover battle FPS lost after the hazard-system decor asteroids (decor culling/LOD, field rock LOD, nebula, presets, particles)** → [[TODOs/Optimization/Battle_FPS_Recovery_Plan|Battle FPS Recovery Plan]]; [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research|PC capture 2026-10-29 vs Mac audits]]
  - 2026-10-29: Phase 0 tool and Phases 1–6 implementations source-verified. Builds/performance runs deferred by user; absorbed attribution/experiments and visual/performance acceptance remain open.
  - 2026-10-29: re-verified against committed `76e7222f8` (code, prefabs, SceneContext, volume profile, materials); no defects found. Tests/captures not run.
  - In progress 2026-10-29: decor cell/frustum culling + 12,000-unit limit; meshes 1,496/1,196/196 tris; field LOD/material fix; per-quality nebula 48/64/96/128; Performant bloom/motion profile; asynchronous fog and VFX migration. Source/asset verification and exact legacy blend correction complete; live capture and historical Development Player comparison ongoing. No tests run; Mac measurement explicitly skipped by user.
  - 2026-10-29: Phases 0–6 implemented; Mac Editor validation collected under new user authorization: 42 ablation phases, 169-pose culling parity, camera sweep and 3×10 s combat (67–74 FPS). No production changes or automated suite.
  - Findings: nebula ≈7 ms residual frame cost in B view; pan p99 73.54 ms / max 404.51 ms; A ≥90 FPS not sustained. Mac Player blocked by Windows-only BuildInfoWriter callback; capture GPU/resolution/screenshot caveats documented.
  - Remaining: Player/build-support fix, PC matched historical captures, full visual/VFX acceptance and absorbed attribution/experiments; see plan/research. No historical Player build resumed.
  - Single rendering owner since 2026-10-29: absorbed Rendering Audit, Particle Rendering and Projectile VFX plans, plus hazard decor/overdraw measurement; newly absorbed items remain outside this execution's listed phases.

- [ ] **Fix confirmed custom-shader defects, then measure shader optimizations** → [[TODOs/Optimization/Custom_Shader_Fixes_Plan|Custom Shader Fixes Plan]]; [[Artifacts/Audits/Custom_Shader_Audit_2026-11-16|November 16 shader audit]]
  - 2026-11-16: Phases 1–2 resolved: hazard flash guard, wreck noise skip, local soft-particle variant + orthographic depth, wreck zone stencil and ISD III hull alpha fix. User kept rim/fog fade. Compiled output rejected planet/decor edits; 30 targeted D3D stages compiled cleanly. Tests not run. Two Windows Development builds succeeded; startup exposed a shared-Tector catalog defect, now corrected in source. Further builds/tests stopped at user request. Phase 3 matched captures and visual acceptance remain; see [[TODOs/Optimization/Custom_Shader_Fixes_Plan - Research|experiment evidence]].

- [ ] **Nebula + hazard GPU optimization and upscaler settings** → [[TODOs/Optimization/Mac_Nebula_Hazard_GPU_Plan|Mac Nebula and Hazard GPU Plan]]
  - 2026-10-29 PC implementation: nebula bake/toggle, hazard H1/H2, resolution + Bilinear/FSR 1/STP, hazard audit pose and BuildInfo path. DLSS/FSR 2 rejected with reasons in plan.
  - 2026-10-29 PC fixes complete: both hazard `_Noise` bindings saved; builder loads textures after both imports; live nebula restored to 48/48/64/96/128/128 with bake-only 128 override. Unity compilation, builder execution and saved bindings verified; no tests or Play Mode run.
  - 2026-10-29 Mac: installed IL2CPP support; ARM64 Development player built (0 errors / 4 warnings), BuildInfo/signature verified after local re-sign, running battle inspected. Matched Player captures/tuning remain open; Xcode 27.1 Metal trace captured: instrumented hazard GPU median 12.42 ms / p95 17.90 ms, fragment-dominated. Fixed-condition acceptance and per-draw attribution remain open; see capture limitations in plan. PC capture gates remain; tests not run.

- [ ] **Pool ship contexts to remove spawn spikes** → [[TODOs/Optimization/Ship_Pooling_Plan|Ship Pooling]]
  - Next gate: see the linked plan; detailed recorded progress is retained in the historical backlog snapshot.

- [ ] **Centralize UI canvas routing and replace custom capture-site screen UI** → [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan|UI Canvas Routing Optimization]]; [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan - Research|Canvas inventory, view mapping and historical CPU attribution]]
  - **Next**: Play Mode behavior and matched performance/Development Player acceptance. New performance analysis explicitly deferred by the user; no savings claimed.

- [ ] **Apply moderate desktop asset compression** → [[TODOs/Optimization/Desktop_Asset_Compression_Plan|Desktop Asset Compression Plan]]; [[Artifacts/Audits/Asset_Import_Audit_2026-11-16|November 16 asset audit and findings]]
  - **2026-11-16 safe pass**: 266 additional textures compressed (six skybox faces + Providence Specular included), 78 models → Low; 153 textures / 21 models remain as quality, dimension, data/tooling or collider exceptions. Read/Write and already-compressed assets unchanged. Windows/macOS format validity + live import/disk/geometry checks passed; tests/builds/Mac rendering not run. Evidence: `Tools/DesktopAssetCompression/Evidence/SafeDesktop20261116.json`; Read/Write and unused CSVs in `output/DesktopAssetCompression/Audit20261116/`.
  - **2026-11-12 selected retry**: 6/10 large textures retained as explicit Standalone BC7; four restored for alpha-error gate failures. Import/disk readback verified, no new Console errors; no tests/build/runtime memory measurement. Evidence: `Tools/DesktopAssetCompression/Evidence/SelectedTextures20261112.json`.
  - **Pass 2 (2026-10-24)**: 462/507 model textures compressed under screening, 47 meshes Low; Addressables bundles 1.53 GB → 1.03 GB, payload 6.46 → 2.66 GB; related EditMode tests pass. Mesh gate 3° rerun: 478 models at Low, bundles → 989.8 MB. In-game visual check pending.
  - **Next**: remaining categories, matched gameplay memory/loading/DSP captures, audio listening/startup/loop/crossfade and intended-scale visual acceptance; macOS module + actual Mac validation. Plan stays active.

- [ ] **Attribute and reduce remaining battle CPU cost** → [[TODOs/Optimization/Battle_CPU_Performance_Plan|CPU profiling and follow-up]]; [[TODOs/Optimization/Battle_CPU_Performance_Plan - Research|2026-10-11 capture comparison and agent findings]]
  - 2026-11-06 M2 Editor: FPS history and hitch rows verified live. Detailed summaries misreported Game view resolution; follow-up recorded in the plan.
  - Frame 41354 follow-up: a reproduced 2.040 s Editor hitch traced to GC during Unity Pipeline command-list encoding. Original 721.25 ms cause remains unconfirmed. See the CPU plan.
  - Experimental catalog patch reverted; project uses the official registry package. Editor GC hitches persisted during the patched fleet run; see the CPU plan for the GUI allocation stack.
  - **Next**: fixed-camera matched workload + Development Player acceptance and longer repair/respawn coverage. Current frame median **33.76 ms** vs 36.10, with more squadrons and different rendering workload; no isolated speedup claim. Broader attribution remains open.
  - Single battle CPU owner: absorbed open items from Battle CPU Fixes, Battle Attack, Burst Jobs and [[Done/Optimization/Late_Battle_CPU_Fix_Plan|Late Battle CPU]]. Historical M2 spikes, matched runs and late-battle visual checks → [[TODOs/Optimization/Battle_CPU_Performance_Plan#Late-battle CPU follow-up]].
  - 2026-10-29 live audit: M2 Editor/Performant/1× conventional median 264.521 ms, p95 411.922 ms; prewarm Awake 27.244 ms/frame in later Profiler window. CSV + CPU/allocation ancestry + graph/debug evidence → repository `Docs/Audits/2026-10-29-Live-Battle-CPU/analysis.md`. No stop/pause, tests or gameplay edits; broader optimization remains active.

- [ ] **Remove startup/loading-screen freezes (sync Addressables loads, squadron prewarm)** → [[TODOs/Optimization/Loading_Freeze_Plan|Loading Freeze Plan]]
  - Status: async loads, deferred warm-up, additive scene load, CRC off implemented 2026-10-23; Editor freezes remain.
  - Next: Player baseline + matched captures; IL2CPP decision; spinner/transition/leak acceptance.

- [ ] **Shrink remaining build size and runtime memory (textures, UI, music, SFX, Read/Write, player build)** → [[TODOs/Optimization/Asset_Size_Followup_Plan|Asset Size Follow-up]]
  - Start 2026-10-24: 989.8 MB bundles, payload 2,564 MB (textures 90 %). Top levers: unused alpha → DXT1 (≤ −400 MB est.), texel-density resolution caps, 45 restored sharp-alpha textures, music out of `Model` bundle.
  - Providence Albedo/Emissive/Normal pilot completed November 7; Specular compressed to Standalone DXT5 on November 16 under the unchanged pixel gate. No new packed-size measurement; current static reachability does not prove packed inclusion. Preserve known sharp-alpha exceptions. Broader unused-alpha/resolution/audio/runtime work remains.

- [ ] **Remove Addressables bundle duplication and fix asset-mapping tools** → [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan|Addressables Bundle Remediation]]; [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research|Audit 2026-10-24 findings]]
  - Status: Phases 0–6 done 2026-10-24; 0 duplicated payload (1.53 GB content). Phases 3–6 + music fix uncommitted.
  - Next: commit; user packed-player placement, audio mix/ducking, early-spawn sync-load timing.
  - November 6 audit → produce a valid current layout for the Providence pilot, then trial the five shared View entries against that baseline.

### Refactoring

- [ ] **Refresh the RTS HUD and menus with the navy and gold palette** → [[TODOs/Refactoring/RTS_UI_Refresh_Plan|RTS UI Refresh Plan]] · [[TODOs/Refactoring/RTS_UI_Refresh_Plan - Research|Research and audit evidence]]
  - Dropdown correction (2026-11-13): 38 dropdowns share brighter slate fill, clearer borders/chevrons and popup states. Skirmish section numbers removed; Players/Battle Settings heights match at 712. Saved read-back and previews verified; tests not run. Broader phases remain active.
  - First phase implemented (2026-11-12): verified Penpot → shared common/primary/destructive menu prefabs → all-UI palette, Rajdhani/Inter Tight and modest sizing. Follow-up: 47 menu actions across 11 screens inherit shared styles; 156 prefabs pass missing-reference checks; Unity saves/import checks clean. Tests not run.
  - Hover feedback strengthened across project controls; 275 saved controls checked, HUD/menu hover previews inspected. Live battle check blocked by separate map DI cycle; tests not run.
  - HUD correction: faction cards and controls use flat #202E40; card gradient disabled, gold borders retained. Six action labels share size 11.5 and fit; white enabled icons and three removed strips retained. Saved read-back and HUD preview verified; live visual acceptance pending.
  - Later phases: overflow/readability, feedback/input, aspect-ratio captures and large-fleet verification. Plan remains active.

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

- [ ] **Battle hitch and GC attribution instruments (built-in markers + GC counters in hitch CSV, per-system alloc, hitch-triggered Profiler snapshot, allocation sweep, memory snapshots)** → [[TODOs/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]]
  - 2026-11-18: written after the playtest left the ~300 ms hitch (every ~35 s, outside all recorded scopes) and 189 KB/frame GC unattributed. All seven tooling phases implemented; Unity compilation and source checks complete. Tests/replay baselines and H1/H2/H3 verdicts deferred; acceptance pending in the user queue.
  - 2026-11-18: reopened — H1 confirmed (blocking GC 288–296 ms); the recorder's own save/decode causes 4–17 s stalls during play → defer export to disarm, mark `instrument_busy`, fix `uncovered_ms` in the Editor.

- [ ] **Crash and error reporting (Unity Diagnostics)** → [[TODOs/Tooling/Crash_And_Error_Reporting|Crash And Error Reporting]]
  - Status: in progress 2026-11-02; user chose Diagnostics independent of usage consent. Local capture/context, consent wording, development probes and optional Jenkins symbols implemented; Editor/player-branch compilation clean, tests not run. Setup rechecked 2026-11-02: Cloud linked, effective Diagnostics enabled, Editor exceptions disabled, compilation clean. Company set to `GolinSkyCompany` and saved 2026-11-02; symbol credential, installed uploader and live job upload stage absent. Standalone upload verification remains pending. Guidance → [[Tooling/Crash_Reporting]].

- [ ] **Unify ALO imports with recipe-driven tools and concurrent-agent coordination** → [[TODOs/Tooling/Unified_ALO_Import_Pipeline|Unified ALO Import Pipeline]]; [[TODOs/Tooling/Unified_ALO_Import_Pipeline - Research|Tooling audit and reuse candidates]]
  - Status: in progress; job/lock/journal infrastructure and Rothana recipe implemented; `apply` disabled. On 2026-11-16, aligned Rothana and Endurance recipe group GUIDs and prefab paths with saved assets; Editor migration gates remain open.
  - Next: production conversion + Editor acceptance; Endurance/Rebel recipes; station/derivative adapters; concurrency recovery acceptance.

- [ ] **Add Jenkins GitHub draft releases** → [[TODOs/Tooling/Jenkins_GitHub_Releases|Jenkins GitHub Releases]]
  - **Remaining**: user adds `github-releases` credential and selects source build/tag; first live upload and recovery acceptance.
  - Rechecked 2026-11-08: installed tools match `main`; only `github-releases` credential missing (Jenkins has `unity-diagnostics-service-account` only). Build 37 (`v0.1.37`, commit pushed) is a valid source.

- [ ] **Game installer and auto-updater (Velopack + GitHub Releases)** → [[TODOs/Tooling/Game_Installer_Velopack|Game Installer Velopack]]
  - Status: in progress 2026-11-08. Launcher (`Tools/Launcher`, Velopack 1.2.161) and Jenkins pack stage (`New-VelopackRelease.ps1`) implemented and dry-run verified locally. Committed `b532d9bad`, publish job installed and `github-releases` credential present (2026-11-08). Remaining: push, new player build with launcher, install/update acceptance.

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

## Superseded Plans

- **UltraHard AI follow-up (22:12 battle) + end-game crash + player ships idle after kill** → [[TODOs/Bugs/Enemy_AI_UltraHard_Followup_And_Ship_Idle]]; follows [[Done/Bugs/Enemy_AI_UltraHard_Collapse_And_Attack_Order_Loss]] (acceptance failed)
  - Status: blocked (2026-11-19). Superseded by [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]] after the 00:49 battle (AI Medium, idle, no top-left capture, second station, stern-first firing).

## Done

### Optimization

- [x] **Projectile visual reduction: missiles/torpedoes 1 shot, reload ×2, speed ×2, short trails; milder bolt pass** → [[Done/Optimization/Projectile_Visual_Reduction_Plan|Projectile Visual Reduction Plan]] — 2026-11-19: plan drafted from source values; decided: missile and torpedo salvo = 1, fighter warheads speed + trail only, torpedo size unchanged; missile defense stays with a 50–75% stop target (Phase 1b); TIE Avenger seeker countermeasure to be removed (Phase 1c, only squadron with missile defense); ship defense = one roll per mount per missile (Phase 1b); all questions decided.
  - 2026-11-19: Phases 1, 1b, 1c implemented and compiled (family data, trails, one-roll-per-mount defense, 8 mount values, seeker removed). DPS identical for all units; simulator TTK −8…−20% from speed ×2 alone (salvo change alone ±1.5%) → user decision. Tests not run. Next: user in-game check, accept/adjust speed, Phase 2 needs per-family projectile counters, then a battle capture.
  - 2026-11-19: missile "N-burst" descriptions fixed; unrelated failing test `TooltipDisplaysFixedCancelBinding` fixed in `InputBindingService` (key names from binding path); rerun pending.
  - 2026-11-19: Phase 2 bolt cuts applied (DPS identical for all units); missiles accepted in game. Done with acceptance pending: bolt look in battle → [[TODOs/User_Acceptance_Queue]].

- [x] **Live battle CPU and pool demand** → [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]] — implemented/tested; heavy-capital/Boost live verification complete; moving-target cost and admission-latency limitations recorded.

- [x] **Remove enemy AI production spikes** → [[Done/Optimization/Enemy_Production_CPU_Spike_Plan|Enemy Production CPU Spike Plan]]
  - 2026-11-14: cheap checks, threat snapshot and per-frame query reuse. Same map and AI after restoring an autosave: production tick max 4.55 ms versus 294.08 ms in the original live capture. Twelve full-map scans peaked at 3.30 ms. Unity compiled with zero errors. Automated tests not run.

- [x] **Restore FPS during ship reinforcement placement (spawn grid overlay)** → [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan|Spawn Grid Overlay Performance Plan]]
  - Completed 2026-11-14: all four phases implemented — deduplicated clearance, allocation-free contact filtering, cached asynchronous reachability and bucketed point checks. Source/diff checks passed; full compile blocked by 3 unrelated ShipSelectionReuseTests CS1501 errors. Tests and runtime FPS unmeasured; acceptance → [[TODOs/User_Acceptance_Queue]].


- [x] **Switch ship pathfinding from raster grid to Burst triangle graph (BurstTriangulator + triangle A* + funnel)** → [[Done/Optimization/Graph_Pathfinding_Plan|Graph Pathfinding Plan]]; [[Done/Optimization/Graph_Pathfinding_Plan - Research|Verified data and package research]]
  - 2026-11-06: switched the runtime planner to the triangle graph and moved the raster baseline into Editor parity fixtures. Unity compiled both assemblies and Burst emitted native code for eight jobs. Navigation acceptance pending → [[TODOs/User_Acceptance_Queue]].

- [x] **Mac GPU fixes: 77% + FSR 1 Mac default, FXAA off, hazard tight mesh, shader prewarm, Mac re-sign** → [[Done/Optimization/Mac_GPU_Frame_Budget_Plan|Mac GPU Frame Budget Plan]]
  - 2026-10-30 fix plan from existing Mac Metal trace (2940×1846, MSAA 2× + FXAA, 452 ms hitch). No code changed yet.
  - Implemented 2026-10-30; Unity compile/import and diff checks passed. No tests, measurements or Mac build. User acceptance → [[TODOs/User_Acceptance_Queue]].


- [x] **Fix decorative asteroid rendering and follow up both performance audits** → [[Done/Optimization/Battle_Rendering_Performance_Audit|Battle Rendering Performance Audit]]; [[Done/Optimization/Battle_Rendering_Performance_Audit - Research|Both audits, measurements and VFX findings]]
  - **Audited 2026-10-28**: asteroid on/off/restored = 39.228 → 100.912 → 39.353 FPS; Performant = 19.318 → 48.634 → 19.350 FPS. Camera fixed within each comparison; no cross-preset regression claim.
  - **Required**: retain verified instanced asteroid materials/draws; reject asteroids wholly outside the camera frustum using transformed bounds. Distance-only filtering is insufficient.
  - **Remaining**: implement/measure culling and geometry reduction; attribute 128-sample nebula and post-processing cost; review legacy explosion materials/shadow flags; repair Frame Debugger and stale-camera capture errors. No code changes or tests in audit.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

- [x] **Optimize projectile VFX for PC without logic changes** → [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan|Projectile VFX PC Optimization]]; [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan - Research|Prefab, material and runtime variation audit]]
  - **Next**: verify equivalent head/trail material sharing, remove unnecessary bolt texture CPU copies, measure scoped fade/shader experiments and validate the full faction/mount/ability/superweapon variation matrix.
  - Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].

- [x] **Consolidate the late-battle CPU fix record** → [[Done/Optimization/Late_Battle_CPU_Fix_Plan|Late Battle CPU Fix Record]]; evidence `Docs/Audits/2026-10-29-Live-Battle-CPU`
  - Remaining tests completed 2026-10-30: `BattleInput` 206, `Squadrons` 81, `EnemyAi` 193, `Map` 37, `Ui` 224, `Vision` 61, `Navigation` 164, `Core` 127 — all passed (1,093 executions / 1,078 distinct names). Marker tag assertion casing fixed; Map/Core/Ui transient failures passed on rerun. Reports → `Logs/Tests/20261030-LateBattleCPU/summary.json`.
  - 2026-10-30: selection shader/attack-only updates, shared shot storage + loading prewarm, capped squadron/capture updates, shared spatial targeting, transform snapshot job, centralized icon projection and allocation fixes implemented. Unity compilation/shader/import and source checks passed; tests not run.
  - Follow-up 2026-10-30: user-authorized crowded PC battle; capture geometry scan microbenchmark −42.0% with identical tallies; destroyed-target pilot exception fixed. Post-fix capture max 1.2845 ms / Console 0 errors. Actual prewarm 72 creations / 6 prefabs, maximum Awake 0.2357 ms; old 27 ms cost not reproduced.
  - Review fixes 2026-10-30: roster/completion prewarm share a 4 ms loading budget; Add/Invalidate refresh all capture areas on the next tick. Added unrun Capture regressions; scripts imported/compiled, diff check passed. Mac/Metal + PC multi-selection/attack visual checks remain open.
  - Remaining: historical M2 shot/capture root causes and matched Editor + Development Player acceptance. New evidence → `Docs/Audits/2026-10-30-Late-Battle-CPU/analysis.md`; tests not run. These investigations and acceptance transferred to [[TODOs/Optimization/Battle_CPU_Performance_Plan#Late-battle CPU follow-up]] on 2026-11-16.

- [x] **Apply battle CPU fixes (targeting, due events, audio mix, navigation, squadrons)** → [[Done/Optimization/Battle_CPU_Fixes_Plan|Battle CPU Fixes Plan]]; [[Done/Optimization/Battle_CPU_Fixes_Plan - Research|Phase 8 acceptance checklist]]
  - **Remaining**: matched ≥3×10 s captures at 1×/4×, controlled warm-selection/visual and combat behavior gates, Development Player confirmation, with remaining scope in the active CPU plan. Phase 7 readiness was implemented in [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]].
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

- [x] **Squadron idle and guard engagement (idle radius 70, guard radius 800, no leash, idle drift)** → [[Done/Bugs/Squadron_Idle_And_Guard_Engagement|Squadron Idle and Guard Engagement]] — Completed 2026-11-21: idle radius 250, guard radius 400, leash ×1.3 for Guard + idle (bomber exemption for the carrier's target), idle squadrons fly back to their post. Compile passed; tests authored, not run → [[TODOs/User_Acceptance_Queue]].
- [x] **Enemy AI opening and task groups** → [[Done/Bugs/Enemy_AI_Opening_And_Task_Groups]]; follows [[TODOs/Bugs/Enemy_AI_Economy_Strategy]]
  - Round 7 (2026-11-20, acceptance pending → [[TODOs/User_Acceptance_Queue]]): round-6 playtest 17:44 (Rebellion AI) — start good, ship selection awful: 55 Nebulon-B, first level-4 ship at 12:08; 22,000 of the opening 30,000 went to two platforms. Fixed: ships only from the station level and one below (`SHIP_TIER_SPAN`); a fleet below 10 ships takes only the cheapest platform. Compiled, tests updated, not run, uncommitted.
  - Status: done 2026-11-20, acceptance pending (round-6 playtest → [[TODOs/User_Acceptance_Queue]]). Round 6 implemented: G1 forward spawns keep a top-down footprint (the 15:48 pairs were different ship types at different hull heights), G2 platforms counted by purchasable type + station ring (1 from level 2, 2 from level 4) + centre 2 + contested zones, G3 anchors by distance to the task, hostile structures/stations rejected, deep and capital anchors need escorts, G4 save only with ≥ 10 ships and advantage ≥ 1 (else buy affordable; surplus ship at 2 × price from level 2), G5 one battle asteroid per zone, attacked = known ship or 3 wings, G6 second station from level 2 on the home ring corner side, no refund loop, G8 `raidSkip` and G9 `ai_orders.issuer` telemetry. Compile 0 errors; tests written, not run (local override).
  - Round 6 reopen (history): reopened 2026-11-20 after the round-5 playtest (15:48). User: start and early mid-game perfect; mid-to-late bad. Found: capital saving while the fleet fell 22 → 3; two ships on one spawn point (no landing reservation); anchors sorted by kind, so ships scatter; battle asteroids replaced mines (income 504 → 350); satellites counted as platforms, so no platforms; second station late (10:26) and placed 870 from hostiles. Round 6 planned (G1 landing reservation, G2 platforms, G3 anchor choice, G4 buy what fights now, G5 one asteroid per zone, G6 second station early and behind the main station, G7–G10, G11 tests). Not started: whole round first, then one playtest. G4 accepted by the user 2026-11-20.
  - Round 5 closed 2026-11-20 (history; playtest played 15:48). E1 forward spawn beside beacon/wings/ships/structures (rear only for a defended station), patrol wing + spawn beacon from level 2; E2 income 30/level, miner 30, second station +50 %, skirmish money 5,000–30,000; E3 group A spread, battle asteroid, zone guard; E4 route defenders, centre detour, stall by result; E5 turtle; E6 bomber hold; E8 lone wing; E9 pad churn wait + AI order filter; E10 shared auto-ability decision; E11 urgent shield below 40 %. EditMode: EnemyAi 574/574, Ships 225/225, Economy 129/129, Ui 400/400, Core 169/169, Abilities 229/230 (unrelated SuperWeaponTests failure).
  - Before round 5 (reopened 2026-11-20): in progress, reopened 2026-11-20. Battle 13:11 on round-4 code failed the gate: 88 / 88 ships spawned at a station (`hasRear` always true in `EnemyFactionController`), ships died in transit in pairs, no site changed hands 8:42–19:28, turtle leaked, bombers did not raid, patrol empty from 4:20. Round 5 planned (E1 spawn modes: rear under station pressure, else forward at relays, beacons and own wings; E2 economy: starting credits 5,000–30,000 and income floor; E3 held centre + battle asteroid; E4 route rating; E5–E9; E10 one auto-ability system for player and AI; E11 shield boost below 40 % with a ship nearby; E12 tests). Not started: whole round first, then one user playtest. E1 simplified to one cheap call (units in, point out; 8 fixed points per unit, no reachability check); E2 income values accepted; E11 keeps the old shield rule and adds the 40 % rule with priority.
  - Rounds 1–4 closed 2026-11-20 (history). Rounds 1–4 implemented: wing groups and mining-spot capture, wing cap, held-zone defence and deployment, satellite rating by type, joint raids, no shared wing zones, stall release, no drip-feed, remembered platforms, turtle after a station loss, patrol bait, centre waypoint, own side from fixed relays. `EnemyAi` 561/561. Gate: next battle (+ Rebellion vs Empire).
  - 2026-11-19: plan created from the 20:45 and 21:11 battles. 18 issues (station wings never leave, no centre strike at second 0, wings turned into escorts, fleet waits at a gathering point, weak fleet aims at the centre, siege defenders chase wings, wing raids 0). Emergency corvette fallback removed (compiled, tests not run). Plan rewritten with the user's opening rules (wings ordered at 0–6 s: 1–2 to the own-side zone, rest to centre base-side pads; satellites first; fighter + interceptor; no corvette before 30 s or from level 2; centre never excluded; weak-phase attack / retreat / switch loop). A1–A7 committed (`318997f8f`, `70a3e0bb5`; `EnemyAi` 542/542). Battle 22:29: opening better (centre relay 37 s, 3 satellites), then 9 faults: centre mines never taken (capture radii were read reversed: mine 60, pad 500), 12–27 wings circling at the zone edge, 36 wings bought, held centre gets no ships, ships deploy only at home. Round 2 steps B1–B10 planned, not started.
  - 2026-11-19 late: A1–A7 implemented and committed (wing groups A/B, station wings free, in-zone pad choice + hold order, carrier-only escorts, group B raids by power rule; corvette 30 s / level-1-only / max 2, ships wait for satellites and mines, fighter + interceptor first, single-ship deployment; no idle reserve, strongest ship to centre, own satellites count, weak-phase switch loop, route deny removed; relay-first in hostile zones (partial A5); station defence targets ships first and keeps them). Unity compile 0 errors; `EnemyAi` EditMode 542/542 after 9 fixes. Open: A8 battles, I18, I19.
  - 2026-11-19 night: Round 2 B1–B6 + B9 implemented (wings land on mining spots and hold, one wing group per outer zone with zone commitment, opening wing cap 6/4 + bomber cap, held zones under attack top tier and defended around own satellites, new ships arrive at the held anchor nearest the zone that needs them). Unity compile 0 errors; tests updated, not run. Open: B4 satellite threat by type, B7 route waypoint, B8 satellite-first ship order, B10 battle, I18, I19.
  - 2026-11-19 night (2): B4 satellite threat by platform type (real weapon loadouts), bombers raid satellites after mines, B8 ships destroy known hostile satellites before the relay. Compile 0 errors; tests written, not run. Open: B7, B10, I18, I19.
  - 2026-11-20: battle 23:11 analysed (user: best battle so far; AI sabotages, retakes mines, holds one outer mine). Round 3: C1 bombers raid together, C2 outer groups never share a zone + defend a held home zone, C3 stalled sieges released after 90 s. `EnemyAi` 557/557. Open: C4–C7, B7, I18, I19, next battle.
- [x] **Squadron faction balance after gun consolidation and bay rotation** → [[Done/Bugs/Squadron_Faction_Balance|Squadron Faction Balance]]
  - Completed 2026-11-19. Model-tuned stats (DuelSim): strength follows price, X-Wing ≈ 2.7 TIE squadrons in a row (retuned 2026-11-19 after the salvo-targeting model fix); torpedo volleys follow price; Rebel station → X-Wing 2/6; Vader escorts follow TIE. Tests not run; battle check → [[TODOs/User_Acceptance_Queue]].
- [x] **Map generation: enemy station too near allied stations (2v2 / 3v3, Medium + Large)** → [[Done/Bugs/Map_Enemy_Station_Near_Allies_Fix|Map Enemy Station Near Allies Fix]] — Completed 2026-11-19: weighted ring team gaps + legacy Open Field corner pull; Medium/Large nearest enemy ≥ 1.5 × nearest ally (diagnostic eval). Tests authored, not run → [[TODOs/User_Acceptance_Queue]].
- [x] **Enemy AI active play + ship combat** → [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]]
  - Completed 2026-11-19. Reopened R1–R7 and R9–R11 implemented. Standalone C# compilation passed. Battle and EditMode acceptance pending.
  - 12:39 battle (2026-11-19): user — huge improvement; sabotage and centre retake pass. Remaining strategy issues → [[TODOs/Bugs/Enemy_AI_Economy_Strategy]]. EditMode gate still pending.

- [x] **Ship ability event throws for destroyed caster (online host, 9× per session)** → [[Done/Bugs/Ship_Ability_Event_Destroyed_Caster]]
  - Completed 2026-11-18: events use stable `EntityId`; regression test added, not run.
- [x] **Fix 63 EditMode failures from the 2026-11-16 full run (31 from one tutorial-anchor `Destroy`, 4 shared-AI, rest content/HUD/input)** → [[Done/Bugs/EditMode_Failures_2026-11-16_Plan|EditMode Failures 2026-11-16]]
  - Completed 2026-11-16: fixed the full failure set. User confirmed full EditMode passed after the offscreen projection fix.

- [x] **Fix structure status icon projection and unseen expiry** → [[Done/Bugs/Structure_Status_Icons_Projection]]
  - Completed 2026-11-16: viewport-relative placement, fresh-event reprojection and actual-position regressions. Unity compilation and EditMode passed; live battle acceptance pending.

- [x] **Fog hull visibility — review fixes** → [[Done/Bugs/Fog_Hull_Visibility_Review_Fixes]]
  - Completed 2026-11-16: all-attacker reveal, staggered 10 Hz ships, serialized duration, hull bake guard, audio and replication coverage. Tooltip finding disproved. Unity compilation/save verified; tests, gameplay and matched CPU capture → acceptance queue.

- [x] **Multiselect Attack / Attack Move leaves ships overlapped** → [[Done/Bugs/Multiselect_Attack_Overlap|Implementation and acceptance]]
  - Review fixes 2026-11-16: ignore passing ships, re-close from the current target side, clear completed/failed settling intent. Unity compilation: 0 errors; Navigation regression cases added, not run.
  - Initial investigation 2026-11-16: source-verified range-stop bypass, collapsed combat offsets and bounded arrival repair; no runtime reproduction or baseline CPU capture.
  - 2026-11-16: revalidated; added Guard/Hunt/minimum-range stops, clamp-inside-hull case, group replacement-target convergence, O(N) registry clearance cost.
  - Completed 2026-11-16: distinct angular attack slots, replacement intent, shared bounded Burst settling and spatial reservation checks. Existing four-plan/2 ms budget preserved; Unity compilation 0 errors, source/diff review complete. Tests not run; gameplay and matched CPU acceptance queued.


- [x] **Online Skirmish friend join / second-slot closure breaks lobby** → [[Done/Bugs/Online_Skirmish_Join_Slot_Race|Completed fix]]
  - 2026-11-16: authoritative seating before broadcast; closed rows auto-open through 8 humans; stale dropdown actions blocked. Unity compilation and source/diff checks clean; regression tests added, not run. User declined reproduction prerequisite; test execution gate queued.

- [x] **Fix remaining tangent line-repair loop** → [[Done/Bugs/Ship_Tangent_Line_Repair_Loop]]
  - Completed 2026-11-15: remove covered obsolete corner and insert blocker in the same repair. Six failing seeded/mirrored regressions now pass; Navigation 280/280 passed. Live battle acceptance remains queued.

- [x] **Fix captured tangent arc-repair loop** → [[Done/Bugs/Ship_Tangent_Arc_Repair_Loop]]
  - Completed 2026-11-15: captured battle replay fixed (near-180° winding), obsolete arc-copy reinsertion removed, native repair counters corrected. Navigation 270/270 passed; live battle acceptance pending.

- [x] **Fix ship rotation snaps and tangent route failures** → [[Done/Bugs/Ship_Rotation_Snaps_And_Tangent_Failures]]
  - Verification 2026-11-15: residual remove/reinsert loop confirmed in direct job inputs (both mirrors: code 2, 33/32 repairs); normal builder passes the same layout. That probe did not establish gameplay reachability; subsequent battle capture and repair → [[Done/Bugs/Ship_Tangent_Arc_Repair_Loop]].
  - Completed 2026-11-15: bounded heading recovery, actual-hull handoff, corner-safe braking, reproduced overlapping-disk winding repair and Editor/Development-only four-file failure snapshots. EditMode Navigation 260/260 and Ships 209/209 passed; battle/online acceptance → [[TODOs/User_Acceptance_Queue]].

- [x] **Restore a dropped/crashed multiplayer session through in-game Reconnect** → [[Done/Bugs/Multiplayer_Reconnect_Flow|Multiplayer Reconnect Flow]]
  - Initial report/proposal 2026-11-15. Missing recovery UI; running battles hidden from browser; Steam Join Game failure reported, cause unknown. Persist session destination/credential for crash-restart rejoin; add Reconnect/Leave and startup Rejoin. Proposed 5 min grace + bounded 3 min loading allowance; host owns expiry. Parent: [[TODOs/Features/Multiplayer_Plan]].
  - Implemented 2026-11-15: persisted recovery, credential/verified-Steam rebind, fixed 300 s/180 s deadlines, loading progress and lifecycle fixes. Unity compiled; tests not run. Two-PC acceptance and historical host-log diagnosis → [[TODOs/User_Acceptance_Queue]].

- [x] **Boost Weapon Power firing rate** → [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix|Boost Weapon Power Fire Rate Fix]]
  - Completed 2026-11-14: faster burst spacing/reloads, bounded overlap and prewarm admission, pending-wait retiming and effective-interval audio. Unity compile/import verified; tests not run. User accepted on 2026-11-16.


- [x] **Fix AI battle-log follow-ups (capture safety bounce, patrol shutdown, stuck recovery latch, locale log numbers)** → [[Done/Bugs/Enemy_AI_Battle_Log_Followup_Fixes]]
  - Completed 2026-11-14 from battle `Loading_20261114_125624` (checks 3–5 failed). Unity compile clean; tests not run; battle replay → [[TODOs/User_Acceptance_Queue]].

- [x] **Fix AI patrol/recovery review regressions** → [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]]
  - Completed 2026-11-14: threatened-site preemption, defendable-station protection during backup construction, scoped pressure bypass and parseable recovery telemetry. Unity compilation/source checks passed; tests not run. Battle/profiling gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Executor depth, planet clearance and big-ship targeting** → [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]]
  - Completed 2026-11-14: Executor −576, planet ceiling −701, station-over-deep-ship clicks, rotated attack-stall retries. Unity compilation and asset persistence verified; tests authored/not run. Visual, battle and test gates → [[TODOs/User_Acceptance_Queue]].


- [x] **Fix reinforcement spawn near/above station and ship spawn grid** → [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]]
  - Completed 2026-11-14: live station hull spans, ship-aware physics/reachability grid, cell-centred preview/drop positions and safe batch spacing. Unity compilation/import and source checks passed; tests not run. In-game and large-map profiling gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Preserve planet decorations during battle loading and spawning** → [[Done/Bugs/Planet_Decorations_Loading_Fix]]
  - Completed 2026-11-13: loader forwards decorations through its site-filtering layout copy; spawn preserves imported rotation under world yaw. Kuat visibility confirmed by user; rotation/Tatooine acceptance pending. Regressions authored; tests not run.



- [x] **Ship routes not curved; ships slow/stop at graph corners** → [[Done/Bugs/Ship_Route_Curvature_Plan]]; research → [[Done/Bugs/Ship_Route_Curvature_Plan - Research]]
  - Completed 2026-11-13, `acceptance: pending-user`. Tangent lines/arcs, local disk repair, persistent buckets, speed profile and tangent follower replaced the Bezier rejection chain. Unity compilation and source/diff checks passed. Tests, archetype sweeps, runtime CPU and online traffic checks → [[TODOs/User_Acceptance_Queue]].

- [x] **Fix enemy AI endgame siege defense, emergency production and capture oscillation** → [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix|Endgame Defense and Capture Fix]]; parent: [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]
  - Completed 2026-11-02, `acceptance: pending-user`. Phases 1–4 (`cf4a22850`), 4b (`1dff4330e`), 4c (`aa7fcaffa`), Phase 5 (strikecraft-specialist cap, raid route safety). Compilation/diff checks passed; tests not run. Gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Independent AI ship groups and squadron capture distribution** → [[Done/Bugs/Enemy_AI_Independent_Task_Groups]]
  - Completed 2026-10-31: group-local retreat/capture and distributed squadron objectives; Unity compilation passed. User gates → [[TODOs/User_Acceptance_Queue]].


- [x] **Fix enemy AI recovery review findings** → [[Done/Bugs/Enemy_AI_Recovery_Review_Fixes]]
  - 2026-10-31: strategic retreat anchors ignore transient reservations; capture stall budget tracks executed capture time and pauses across other strategies. Four regressions authored; Unity compilation and diff checks passed. Tests/gameplay acceptance → [[TODOs/User_Acceptance_Queue]].

- [x] **Fix enemy station defense, retreat lock and reinforcement-zone recovery** → [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan|Enemy AI Defense and Zone Recovery]]; [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan - Research|Battle evidence]]
  - Completed 2026-10-31; source/diff and Unity compilation clean; user acceptance pending. Covers long-range interception, recovery/home loss, typed zone objectives, capture progress and idle Providence diagnostics.
  - Follow-up to [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]; tests authored, not run; remaining gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Fix Play Mode shader-warmup keyword errors** → [[Done/Bugs/Shader_Warmup_Keyword_Errors|Shader Warmup Keyword Errors]]
  - Completed 2026-10-30: removed broad `Shader.WarmupAllShaders`; retained covered frame and pool prewarming. Reproduced 28 errors before; fresh/restored Play Mode startup now 0 new errors. `GameFlow` 153, `Combat` 220, `Capture` 101 passed; `GameFlow` repeated after fix.

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

- [x] **Update Unity Pipeline package 0.6.0-exp.1 → 1.0.0 and CLI 1.0.0 → 1.0.1** → [[Done/Tooling/Unity_Pipeline_1_0_Update|Unity Pipeline 1.0 Update]]
  - Completed 2026-11-20: package + CLI updated, compile clean, AGENTS.md 1.0 command section, `GEMINI.md` added, worker/PS1 + ~80 docs swept; smoke commands passed. Tests not run. Jenkins build gate in acceptance queue.

- [x] **Battle logic simulator (Editor Hub)** → [[Done/Tooling/Battle_Logic_Simulator|Battle Logic Simulator]]

- [x] **Implement Game Design Balance Editor** → [[Done/Tooling/GameDesign_Balance_Editor|Game Design Balance Editor]]; [[Done/Tooling/GameDesign_Balance_Editor - Research|Data ownership and feasibility]]

### Features

- [x] **Ship engine plumes on all 73 ships (shared scalable prefab, replaces CR70 trail)** → [[Done/Features/Ship_Engine_Plume_Rollout_Plan|Ship Engine Plume Rollout]] — 2026-11-20: done, `acceptance: pending-user`. 73 ship views, 1–5 engine `TrailRenderer`s each; lifetime = 14 × width / pace so length follows speed and trails bend (fixed-length plume dropped after user check). Chimaera/Devastator inherit ISD II plumes; Chimaera fog list fixed. Compile clean; tests not run. Guide → [[AssetPipeline/Guides/SHIP_ENGINE_TRAIL_GUIDE]].
- [x] **Chimaera hero capital (ISD II base, +2 dual medium turbolasers, rust + gold hull, ventral emblem)** → [[Done/Features/Chimaera_Hero_Capital|Chimaera Hero Capital]] — 2026-11-20: done, `acceptance: pending-user`. `ShipType 218`; hull/shields `28,000`, regen delay `0.9`, price `12,000`, max 1. Variant of `ISDIIShipView`, procedural weathering textures, emblem plate, own wreck/icon. Compile clean; `EmpireHeroCapitalTests` not run.
- [x] **Darth Vader — Devastator hero capital (6 Imperial heavy dual TL + 2 heavy dual ion mains, +2 medium ion)** → [[Done/Features/Devastator_Hero_Capital|Devastator Hero Capital]] — 2026-11-20: done, `acceptance: pending-user`. `ShipType 219`; same stats/price; ISD II look and wreck. Compile clean; tests not run.
- [x] **Tractor Beam targets every lower ship class (caster class decides), tooltips + tests** → [[Done/Features/Tractor_Beam_Lower_Class_Targeting|Tractor Beam Lower-Class Targeting]]
  - Completed 2026-11-19, `acceptance: pending-user`. `ShipClass.HullTier()` rule in `TractorBeamSettings`, `targetClasses` removed, tooltips + design doc updated; Unity compile clean; `Abilities` 223/223 and `Core` 169/169 passed. Gates → [[TODOs/User_Acceptance_Queue]].
- [x] **Shift + left-click a unit → add it to the current selection (toggle off on second Shift + click)** → [[Done/Features/Shift_Click_Add_To_Selection|Shift Click Add To Selection]]
  - Completed 2026-11-19, `acceptance: pending-user`. Rebindable `Battle/AddToSelection` (Shift), `SelectionContext.Add`, `SelectionService.ToggleInSelection`; Unity compile clean; `BattleInput` 433/433 passed. Gates → [[TODOs/User_Acceptance_Queue]].
- [x] **Planet biome atmosphere: per-biome sun, ambient and colour grade (Tatooine orange, Coruscant warm, Hoth cold)** → [[Done/Features/Planet_Biome_Atmosphere_Plan|Planet Biome Atmosphere Plan]]
  - Completed 2026-11-19, `acceptance: pending-user`. 10 biomes from surface albedo, 57 planets assigned; `PlanetAtmosphereSettings` + sparse grade Volume (priority 50) + `BattleAtmosphereView` in `SceneContext.prefab`; tints softened ×0.85 after user feedback. Unity compile clean; `PlanetAtmosphereTests` and `Core` 166/166 passed. Gates → [[TODOs/User_Acceptance_Queue]].
- [x] **Spatial pace clusters: per-cluster formation speed, double right-click loose move (green wave)** → [[Done/Features/Spatial_Pace_Clusters_Plan|Spatial Pace Clusters Plan]]
  - Completed 2026-11-19, `acceptance: pending-user`. Replaced `GroupPace` with `PaceCluster` + `PaceClusterBuilder` + `PaceClusterMergeService` for `Move` (formation), `Attack`, hard-point attack, and `AttackMove`; lag correction, straggler/stuck detach, double right-click `MovePace.Loose` + green wave feedback (incl. network). Unity compile clean (`0` errors); tests not run (local override). Gates → [[TODOs/User_Acceptance_Queue]].
- [x] **Group attack speed matching (ships move at slowest selected ship's speed)** → [[Done/Features/Group_Attack_Speed_Matching_Plan|Group Attack Speed Matching Plan]]
  - Completed 2026-11-18, `acceptance: pending-user`. `GroupPace` shared per group `Attack`/hard-point attack (ships only, ≥ 2), `ShipMoveModel.SpeedCap`, release at firing range / new order. Unity compile clean; tests not run (local override). Gates → [[TODOs/User_Acceptance_Queue]].
- [x] **Interdictor Gravity Well follow-up: anchor ship while active (orders rejected, turn + fire allowed), range ring only when selected, dome generator VFX** → [[Done/Features/Interdictor_Gravity_Well_Anchor_And_Vfx_Plan|Gravity Well Anchor & VFX Plan]]
  - Completed 2026-11-17, `acceptance: pending-user`. `CombatModifiers` movement anchor, `ShipMoveComponent` brake/refuse, `ShipOrderRunner` rejects move-type orders (`Retreat` cancels the well), selected-only ring, `GravityWellEmitter` dome VFX on the view prefab. Compile clean; tests not run (local override). Gates → [[TODOs/User_Acceptance_Queue]].
- [x] **Interdictor ability "Activate Gravity Well" (block hostile reinforcement drop-ins in an adjustable bubble around the ship; default range 900)** → [[Done/Features/Interdictor_Gravity_Well_Plan|Interdictor Gravity Well Plan]]
  - Completed 2026-11-17, `acceptance: pending-user`. `Interdict = 57` (`30 s` active, `60 s` recovery, cancellable, `900` range, `HardPointType.Any`), `BattleAreaLayer.SpawnBlock` registration, pooled `GravityWellField.prefab` ring VFX, `InterdictIcon.png` Sprite, `InterdictorStarDestroyerShipData` (`[DauntlessSensorJamming, Interdict]`), save migrations, balance adapter and test updates. Unity compile, reserialization and Console (`0` errors) verified; tests not run (local override). User gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Squad control groups (10 slots, 1–9 then 0, preview + shared replacement confirmation)** → [[Done/Features/Squad_Control_Groups_Plan|Squad Control Groups Plan]]
  - Implemented 2026-11-16: fixed top-row keys, optional Mac Command, heroes-first icons, battle saves, notifier feedback and Alt-tab-preserved replacement popup. Top-center squad preview with slate slots and larger icons. Unity prefab read-back and 1920×1080 preview verified. Tests not run under local override. Play/platform gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Mining, satellite and relay state icons in world/minimap** → [[Done/Features/Structure_Status_Icons_Plan|Structure Status Icons Plan]]; [[Done/Features/Structure_Status_Icons_Plan - Research|Source research]] · Solid Emblems follow-up verified 2026-11-19.
  - Planned 2026-11-16: mining/satellite captured until built; built/destroyed and relay captured until seen on screen for 2 s, regardless of fog; all minimap event icons hold 3 s then fade. Event-driven state, pooled views, independent timers. Battle Asteroid uses mining rules (user-confirmed).
  - Completed 2026-11-16 (`e35feafc0`): event-driven pooled world/minimap icons, independent clocks, shared artwork and per-map snapshot suppression. Unity compilation/assets/isolated preview verified; tests not run. User gates → [[TODOs/User_Acceptance_Queue]].
  - Review fixes 2026-11-16: unified rebuild/live facility anchors, removed unused capture/loss pings, added deterministic presenter coverage and updated interface doubles. Source/diff verified; final Unity compilation successful, tests not run. See plan and acceptance queue.
  - Purchase/fade correction 2026-11-16: satellite reminder dismissed on purchase; all status icons fade/crossfade, independent clocks retained. Ui/Capture/Networking regressions updated; live acceptance pending.

- [x] **Darth Vader hero squadron: Remake TIE Advanced + 6 escorts** → [[Done/Features/DarthVader_Squadron_Import]]
  - Completed 2026-11-16: mixed leader/escort durability, Replenish Wingmen, Empire registrations and saved visual/stat verification; tests not run, user gates in acceptance queue.

- [x] **Import the Recusant Dreadnought as a CIS Heavy Capital** → [[Done/Features/Recusant_Dreadnought_Import|Recusant Dreadnought Import]]
  - Completed 2026-11-16. Saved 2,542 m scale, 30,000 hull, 35,000 shields, 29 weapon hardpoints and Boost Weapon Power. Verified 26 forward-capable mounts at 300 units and 1,393.184 raw all-mount DPS. In-game acceptance is queued.

- [x] **Rejoin an online battle after Pause Exit** → [[Done/Features/Rejoin_After_Battle_Exit_Plan|Rejoin After Battle Exit Plan]]
  - Review fix 2026-11-16: leaving another Steam lobby preserves the saved battle; source and related GameFlow/Networking/Ui tests reviewed, tests not run. Acceptance remains pending.
  - Completed 2026-11-16. Client Exit preserves the 300 s reservation for Multiplayer, Steam, Server Browser and matching IP recovery. Unity compiled. Tests and two-PC acceptance → [[TODOs/User_Acceptance_Queue#Multiplayer Reconnect]]. Parent → [[TODOs/Features/Multiplayer_Plan]].

- [x] **Artillery ships keep launcher distance; ordinary combat after launcher destruction** → [[Done/Features/Artillery_Ship_Standoff_Plan|Artillery Ship Standoff Plan]]
  - 2026-11-15: verified six loadouts; five opted in, Profundity excluded. Explicit Attack uses surviving launcher reach, with ordinary pursuit after launcher loss. Editor and Windows player scripts compiled and five assets saved. Tests not run. Gameplay and CPU acceptance → [[TODOs/User_Acceptance_Queue]].

- [x] **Destroy one ship from its UI card** → [[Done/Features/Ship_Ui_Destroy_Plan|Ship UI destruction plan]]
  - Completed 2026-11-15: right-click → Destroy ship → generic Yes/No confirmation and authoritative normal death. Ownership, card reuse, and replication regressions authored. Unity compilation and saved bindings verified. Source/prefab review of `30e6162ea` on 2026-11-15 found no concrete implementation defects; coverage limits recorded in the plan. Tests not run; offline and online acceptance → [[TODOs/User_Acceptance_Queue]].

- [x] **Capital siege damage and forward main batteries** → [[Done/Features/Capital_Siege_And_Forward_Batteries_Plan]]
  - Completed 2026-11-14. Super-heavy mounted weapons deal ×1.5 against stations and defense platforms. Added forward main-battery arcs, main-battery-first hull facing and a closer Executor siege approach where the live hull permits.
  - Unity compilation and 20 prefab saves verified. Evidence → [[Rules/WEAPON_EFFECTIVE_DPS_LIMITATIONS]]. Cadence → [[Done/Bugs/Boost_Weapon_Power_Fire_Rate_Fix]]. Unrun tests and player checks → [[TODOs/User_Acceptance_Queue#Gameplay and Balance]].

- [x] **Enemy AI map patrol and safe recovery relocation** → [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation|Enemy AI Patrol and Recovery Relocation]]
  - Completed 2026-11-14; acceptance pending-user. Patrol memory with visit staleness; recovery anchor (safe allied zone / empty sector) for station, mining and platform placement after station loss or critical pressure. Owns old-home rebuild finding from [[TODOs/Bugs/Enemy_AI_And_Faction_Balance|Enemy AI and Faction Balance]]. Decisions recorded 2026-11-14: patrol ship on Normal+, trigger = losing badly (fleet advantage), distance scales with power, any ally zone, fog-based empty areas incl. capturing a neutral relay. Step 0 adds decision stability (fixes engage↔retreat flip t230–255 and capture flip t293). New `_ai.csv` event kinds + 10 log acceptance checks. All implementation scope complete; Unity compilation/source checks passed. Tests not run; battle-log gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Small-ship battle icons + stable squadron icon grids** → [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan|Small Ship Icons and Squadron Grid Plan]]
  - Implemented 2026-11-14. Unity compilation and saved asset bindings verified. Live visuals, Development Player profiling and explicitly requested tests → [[TODOs/User_Acceptance_Queue#Small Ship Icons and Squadron Icon Grid]].

- [x] **Planet decorations (Kuat docks, space junk, resource containers)** → [[Done/Features/Planet_Decorations_Plan|Planet Decorations Plan]]
  - Completed 2026-11-13: six imported props, three sets, 13 planet assignments, independent seeded clearance placement. Unity compile/import checks clean; sampled placement counts recorded. Tests not run; planet-list and battle visual acceptance queued.
  - 2026-11-13 follow-up: junk scale 3× (0.18–0.30), central 70% footprint bounds; saved/compiled, sampled Tatooine counts 3/7/7. Normal-zoom visual acceptance pending.

- [x] **Map archetypes: per-planet layout patterns, free station placement, 6–8 players** → [[Done/Features/Map_Archetypes_Plan|Map Archetypes Plan]]; [[Done/Features/Map_Archetypes_Plan - Research|Assignments and decisions]]
  - Completed 2026-11-13, `acceptance: pending-user`. Seven archetypes, 57 profiles/previews, grouped-first station fallback, 4/6/8-player limits, setup/gallery/save/network integration. Optional biome visuals deferred.
  - Verified: Map 83/83, MapGeneration 224/224, MapExhaustive 164/164, Ui 278/278, GameFlow 187/187, Capture 111/111, Networking 31/31, Core 151/151. Assignment/visual, eight-fleet performance and multiplayer gates → [[TODOs/User_Acceptance_Queue]].

- [x] **UI SFX system (button click via global listener, selection sounds station/ship/unit/enemy)** → [[Done/Features/Ui_Sfx_System|UI SFX System]]
  - Completed 2026-11-12. Unity compiled and verified the saved binding, mixer and 15 clips. Tests not run. In-game listening → [[TODOs/User_Acceptance_Queue]].

- [x] **Tactical markers (B ping + radial: Look/Attack/Capture/Defend/Rally/Retreat; team-only online sync; allied AI accepts/declines and sends its own)** → [[Done/Features/Tactical_Markers_Plan|Tactical Markers Plan]]
  - Completed/reviewed 2026-11-12: startup DI cycle, site identity, input cancellation, AI objectives and lifecycle fixed; BattleInput 296/296, Ui 276/276, Networking 30/30, EnemyAi 360/360, Core 151/151 passed. In-game/two-client/visual acceptance queued.




- [x] **Play menu reroute (Single Player / Multiplayer / AI vs AI / Tutorial; Host Game with mode picked in lobby; Server Browser; START GAME everywhere)** → [[Done/Features/Play_Menu_Reroute_Plan|Play Menu Reroute Plan]]
  - Completed 2026-11-11: Single Player screen (Skirmish, Survival, Load Game), Multiplayer (Host Game, Join Game → Server Browser of public Steam lobbies + IP join), host-only Skirmish/Survival switch in the lobby (Survival refused over 3 members), START GAME labels.
  - Unity compile, prefab references, Addressables and preview renders verified; tests not run. In-game routes and 2-PC lobby/browser checks → [[TODOs/User_Acceptance_Queue]].

- [x] **Seismic Charge — shared Boba/Jango bomb rework** → [[Done/Features/Harmonic_Bomb_Ability|Implementation plan]]
  - Completed 2026-11-11: 360 base damage ×2 against squadron members, 210-unit sphere, 1.5 s fuse, 20 s activation cooldown and pooled flat blue Shader Graph shockwaves. Supersedes the 2026-11-06 Harmonic Bomb behavior/visuals; original pilot voices and seismic detonation retained.
  - Unity compile/import, saved bindings, shader passes and isolated render verified; tests not run. In-battle gameplay, visuals and listening → [[TODOs/User_Acceptance_Queue]].

- [x] **Battle notifications (dismissible top-center text cards for urgent, tactical, progress, feedback and outcome events)** → [[Done/Features/Battle_Notifications_Plan|Battle Notifications Plan]]
  - Done 2026-11-09: feed + `BattleNotificationUi` prefab/Addressable, 9 event sources, rejection reasons, survival and outcome cards. Compiled clean; tests authored, not run. In-game checks → [[TODOs/User_Acceptance_Queue]].

- [x] **Jango Fett — Slave I — single-fighter skirmish hero for CIS (`Separatist`)** → [[Done/Features/Jango_Fett_Slave_I|Implementation plan]] · [[Done/Features/Skirmish_Hero_Fighters - Research|Balance research]]
  - Ship, guns, recruitment, audio and Harmonic Bomb equipped. In-game flight, balance and lifecycle checks → [[TODOs/User_Acceptance_Queue]].

- [x] **Han Solo & Chewbacca — Millennium Falcon — single-fighter skirmish hero for Rebellion** → [[Done/Features/Han_Solo_Millennium_Falcon|Completed plan]] · [[Done/Features/Skirmish_Hero_Fighters - Research|Balance research]]
  - Completed 2026-11-06. Unity wiring, 40 stationary matchups and live shop visibility after the hero prewarm fix verified. Remaining flight, audio and skirmish acceptance → [[TODOs/User_Acceptance_Queue]].

- [x] **Boba Fett — Slave I — single-fighter skirmish hero for Empire** → [[Done/Features/Boba_Fett_Slave_I|Implementation plan]] · [[Done/Features/Skirmish_Hero_Fighters - Research|Balance research]]
  - Hero, primary guns, shop, Harmonic Bomb slot and activation voice verified. Stationary duel results recorded; live checks → [[TODOs/User_Acceptance_Queue]].
- [x] **Display settings: resolution in all window modes, refresh rate, monitor selection (Win + macOS)** → [[Done/Features/Display_Modes_And_Monitors_Plan|Display Modes and Monitors Plan]]
  - Completed 2026-11-04: all four phases, macOS native fullscreen and monitor FPS preset. Unity compile/import, saved prefab and Penpot layout verified. Ui 245/245 + Core 136/136 passed on 2026-11-04; no fixes needed. Windows/macOS hardware acceptance → [[TODOs/User_Acceptance_Queue]].

- [x] **Reinforcement drag fix (second drag does nothing) + Shift-drag deploy-all** → [[Done/Features/Reinforcement_Drag_Batch_Spawn|Reinforcement Drag Batch Spawn]]
  - 2026-11-03: silent 1 s `SPAWN_COOLDOWN` removed; Shift-drag deploys min(reserve, population fit) in a grid. Compile clean; tests authored, not run; in-game gate queued.

- [x] **Auto power abilities: player ships auto-use Power to Shields / Power to Weapons / Power to Main Batteries; global + per-ship "A" toggle; one Power to Shields for all ships** → [[Done/Features/Auto_Power_Abilities_Plan|Auto Power Abilities Plan]]
  - 2026-11-01: Main Batteries enabled for Allegiance / ISD II; source diagnostics and Unity refresh clean; two Core lifecycle regressions authored, not run under checkout override.
  - Completed 2026-10-30: ordinary-lifecycle automation, shared shield id 3/save migration, Gameplay setting, per-ship persistence and single/group A controls.
  - Independent source review 2026-10-30: no actionable defect found across runtime, settings/UI and migration/save; scoped diagnostics/diff checks passed. No tests or Play Mode rerun; user acceptance remains pending.
  - Verified: Abilities 142/142; full EditMode 2,542/2,550, with eight unrelated baseline balance/AI failures. User battle/balance/UI gates → [[TODOs/User_Acceptance_Queue]]. Research → [[Done/Features/Auto_Power_Abilities_Plan - Research]].

- [x] **Main relay map layout adjustments: exact-center main relay, edge additional zones, no straight shortcuts, home sites behind station, main mines at half relay radius** → [[Done/Features/Main_Relay_Map_Layout_Adjustments|Main Relay Map Layout Adjustments]]
  - Planned 2026-10-29 from in-game review of [[Done/Features/Main_Relay_Zone_Map_Rework|Main Relay Zone Map Rework]].
  - Decided: main relay only gets range `540` (mines at half radius); additional zones on free map edges; random station corner offset with home mine + pad close behind; no straight line main ↔ additional.
  - Completed 2026-10-29: placement, separate main prefab, edge fill and blockers/lane detours; Unity compilation/import and source checks passed. Tests not run; user-only gates → [[TODOs/User_Acceptance_Queue]].
  - Review fixes completed: 1250–1275-wide connected fields, local-width obstacle checks, preferred hazard placement, behind-site jitter clearance and even-ring validation. Unity compilation passed; tests not run.


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

- [x] **Close settings audit follow-ups** (Wave 1) → [[Done/Features/Settings_Audit_Followups|Settings Audit Follow-ups]]
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

- [x] **Remove ISD III and Thranta from the project; move content to `D:\EmpireAtWarUnusedContent`** → [[Done/Refactoring/ISDIII_Thranta_Removal|ISD III and Thranta Removal]]
  - Completed 2026-11-17, `acceptance: pending-user`. Removed enum values, guards, data/Addressables entries, tests and 107 files (+`.meta`); kept one window texture used by VictoryII. Unity compiles, Console 0 errors; tests not run (local override). User gate → [[TODOs/User_Acceptance_Queue]].

- [x] **Rework ship classes: add HeavyCruiser/Dreadnought, HeavyCapital → BattleCruiser, reassign ships, damage matrix, shield regen, Nebulon-B 600 m** → [[Done/Refactoring/Ship_Class_Rework_Plan|Ship Class Rework Plan]]
  - Completed 2026-11-17, `acceptance: pending-user`. Added `HeavyCruiser` and `Dreadnought`, renamed `HeavyCapital` → `BattleCruiser` (preserving enum IDs + `[FormerlySerializedAs]`), updated siege multipliers (`1.5x`/`1.2x`), rule/matrix/regen/research assets, 27 `ShipData` reassignments, 600 m `NebulonB` resize + vision hull rebake, and 13 EditMode test files. `BalanceInventory.Build()` 0 errors / 157 units; 76/76 `ShipData` verified; Console 0 errors; Unity tests not run (local override). User gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Replace inline namespace-qualified type references with usings (429 lines / 143 files)** → [[Done/Refactoring/Namespace_Qualified_Type_Cleanup|Namespace-Qualified Type Cleanup]]
  - Completed 2026-11-12. Shortened 428 lines across 140 C# files, including test source. Four collision cases retained. All six batches compiled in Unity. Tests not run.

- [x] **Popup core + feature popup services; shared Invite Friends popup** → [[Done/Refactoring/Shared_Invite_Friends_Popup_Plan|Shared Invite Friends Popup Plan]] · contract: [[Architecture/POPUP_SYSTEM]]
  - Completed 2026-11-11. Both lobbies share a picker. Unity EditMode `Ui` 274 passed and `Core` 150 passed. Live Steam checks are in [[TODOs/User_Acceptance_Queue]].

- [x] **Remove empty interface members (simulation launcher pause, save-screen SelectMode)** → [[Done/Refactoring/Empty_Interface_Members|Empty Interface Members]]
  - Completed 2026-11-06. Split battle pause from launch and moved mode selection to the load route. Editor analytics retains its disabled implementation.

- [x] **Delete empty SelectionComponent hooks and empty MenuData** → [[Done/Refactoring/Empty_Code_Cleanup|Empty Code Cleanup]]
  - Completed 2026-11-06. Removed the unused hooks and field from 96 prefabs and retired the menu asset, mapping, preload and registration.

- [x] **Remove NoFogOfWar and RevealedMiniMapFogMask** → [[Done/Refactoring/Fog_Disabled_Stand_Ins|Fog Disabled Stand-Ins]]
  - Completed 2026-11-06. Startup reveal, map bounds and minimap overlay use optional contributors. Unity compilation verified; gameplay and targeted test checks queued.

- [x] **Remove NoCaptureSites and centralize reinforcement area access** → [[Done/Refactoring/Battle_Area_Access|Battle Area Access]]; [[Done/Refactoring/Battle_Area_Access - Research|Source investigation]]
  - Completed 2026-11-06. Layered battle areas and capture contributors implemented; Unity compiler verified. Tests and gameplay acceptance queued.

- [x] **Harden DI scope teardown and build rollback (VContainer)** → [[Done/Refactoring/DI_Container_Failure_Handling|DI Container Failure Handling]]
  - Completed 2026-11-06. Ordered cleanup, child rollback, shared AI combat data and entity decoupling verified. Unity EditMode: 62/62 targeted cases passed.

- [x] **Auto power refactor (data eligibility, remove static rules, dedupe snapshot/monitor)** → [[Done/Refactoring/Auto_Power_Refactor|Auto Power Refactor]]
  - Completed 2026-11-02, `acceptance: pending-user`. All 5 phases; compiles; EditMode tests not run (`Abilities`, `Ships`, `Core`, `EnemyAi`, `Balance`).

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
  - **Remaining**: Play Mode acceptance (fade, fog, camera drift, hitch, VContainer battle-scope startup, Esc during loading, pause/speed/menu/end/exit). Fader added 2026-10-04.
  - Closed 2026-10-29 by backlog triage; user gates → [[TODOs/User_Acceptance_Queue]].

- [x] **Rework UI before VContainer migration** → [[Done/Refactoring/UI_Service_Refactoring|UI Rework Before VContainer]]; [[Done/Refactoring/UI_Service_Refactoring - Research|UI inventory, placement and SoulsLikeTemplate comparison]]

- [x] **Resolve codebase audit 2026-09-27** → [[Done/Refactoring/Codebase Audit 2026-09-27/00 Overview|Codebase audit overview]]; [[Done/Refactoring/Codebase Audit 2026-09-27/11 Re-audit 2026-09-30|the re-audit]]

- [x] **Fix Project Organization & Asset Naming Defects** → [[Done/Refactoring/Project_Organization_Remediation_Plan|Project Organization & Asset Naming Remediation Plan]]; [[Done/Refactoring/Project_Organization_Remediation_Plan - Research|Full Audit & Naming Mapping Catalog]]
