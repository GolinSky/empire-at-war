---
category: Documentation
status: in-progress
created: "2026-10-29\r"
---
# User Acceptance Queue

## Rules

- One line per user-only gate (in-game, listening, visual, tests on request) of a closed plan; policy → root `AGENTS.md` Plan Lifecycle step 4.
- Passed → delete the line; when a plan has no lines left, set its `acceptance: passed`.
- Failed → reopen the plan or open a `Bugs` plan; replace the line with its link.
- Imported-unit balance values are provisional; balance review is part of each unit's gate.

## Imported Units

- [ ] [[Done/Features/Jango_Fett_Slave_I]] — Check placement silhouette, one portrait/target, order and death audio, and Jango’s activation line on a successful bomb drop.
- [ ] [[Done/Features/Jango_Fett_Slave_I]] — Measure overshoot, firing passes and reacquisition against Belbullab-22 and Vulture wings using the shared duel protocol. Check AI purchase and rebuy frequency.
- [ ] [[Done/Features/Jango_Fett_Slave_I]] — Verify each owner’s limit across queue, cancellation, holding, deployment, death and repurchase. Check save/load and pooled respawn.

- [ ] [[Done/Features/Boba_Fett_Slave_I]] — Check placement silhouette, one target/portrait, local order, ready and death (corvette explosion) cues in skirmish.
- [ ] [[Done/Features/Boba_Fett_Slave_I]] — Measure overshoot, reacquisition, mixed-fleet and moving-wing duels; check AI rebuy frequency.
- [ ] [[Done/Features/Boba_Fett_Slave_I]] — Verify independent owner limits across queue, cancel, reserve, death, repurchase, pooled spawn and save/load.
- [[Done/Features/Han_Solo_Millennium_Falcon]]
  - [ ] Check silhouette, preview, one target and portrait, pursuit, overshoot, reacquisition time and broadside fire at 116 u/s.
  - [ ] Fly specials-off duels versus A-wing and X-wing, then specials-on versus two wings. Record win rates, remaining health, hits and elapsed time.
  - [ ] Verify player and AI reservations through queue, cancellation, deployment, death and repurchase; record AI rebuy frequency and independent owner limits.
  - [ ] Check 8 s immunity, 60 s recovery, save/load and fresh pooled deployment.
  - [ ] Listen to Ready, selection, move, attack, activation, engine and destruction in a skirmish.
- [[Done/Features/MC80Independence_Import]]
  - [ ] Clean-skirmish acceptance: construction, placement, combat, shield power, bay destruction/replacement, engine damage, death/wreck and teardown.
  - [ ] Review provisional economy, weapon damage/range, movement, flight height and matchup balance.
- [[Done/Features/YWingBomber_Import]]
  - [ ] Clean-skirmish acceptance: deployment, six craft, flight/attack arcs, weapon impacts, shields, Ion Shot, fog/selection and teardown.
  - [ ] Review provisional economy/movement/weapon values and documented source behavior differences.
- [[Done/Features/ArquitensAdvanced_Import]]
  - [ ] Clean-skirmish acceptance: deployment, hull-only targeting/damage, weapon arcs/bursts, ability/shields, visibility/team colors and destruction.
  - [ ] Review remaining provisional balance, gameplay results and documented source-to-project differences.
- [[Done/Features/VictoryII_Import]]
  - [ ] Clean-skirmish acceptance: combat, targetable systems, abilities, fighter launches, fog/selection, placement and destruction.
  - [ ] Review provisional balance and source-to-project differences in [[AssetPipeline/Imports/Victory II Import]].
- [[Done/Features/VictoryI_Import]]
  - [ ] Clean-skirmish acceptance: combat/hardpoints/abilities/fighter launch/fog/selection/placement/death.
  - [ ] Review provisional balance and source-to-project differences listed in [[AssetPipeline/Imports/Victory I Import]].
- [[Done/Features/Victory_Import]]
  - [ ] Clean-skirmish acceptance: purchase/placement, movement, combat, hardpoint destruction, TIE launches/replacements, boost timing/restoration and wreck.
  - [ ] Review provisional balance and retained source Republic markings.
- [[Done/Features/MC75_Profundity_Import]]
  - [ ] Clean-skirmish acceptance: build/placement, team/fog rendering, targeting and all weapon categories.
  - [ ] Verify fighter launches, shield/engine/hangar destruction, Full Salvo expiry/recovery and wreck lifecycle.
  - [ ] Review provisional scale, flight, weapon/range, economy and build limit.
- [[Done/Features/XWing_Import]]
  - [ ] Clean-scene in-game acceptance: buy/deploy, fire all four cannons per living member, close/reopen wings, ion disable/teardown, fog/selection and member death.
  - [ ] Confirm provisional movement/range/size/economy against project pacing.
- [[Done/Features/VWing_Import]]
  - [ ] Clean-skirmish acceptance and provisional balance review.
- [[Done/Features/StealthCorvette_Import]]
  - [ ] Authorized clean-skirmish runtime acceptance: spawn/placement, fire/shields, cloak visibility/selection/detection/pursuit, cancellation/expiry/recharge, reinforcement rules, death/wreck.
  - [ ] Provisional scale/movement/weapon/armor/audio balance review.
- [[Done/Features/Imperator_Import]]
  - [ ] Runtime combat/ability/hangar/placement/destruction acceptance and provisional balance review.
- [[Done/Features/Dispatcher_Import]]
  - [ ] Clean-skirmish acceptance: movement, firing arcs, Power to Weapons, system damage, placement, fog/selection and death/wreck behavior.
  - [ ] Review provisional balance and inherited matchup categories.
- [[Done/Features/C9979_Import]]
  - [ ] Clean-skirmish acceptance: direct weapon firing, hostile target acquisition of the hull-only transport, shields/regen, movement, fog/selection, placement and destruction/wreck.
  - [ ] Review provisional economy/movement/size/weapon damage/range and Corvette damage-category substitution.
- [[Done/Features/YWing_Import]]
  - [ ] Verify clean-skirmish movement, weapon passes, ability, repairs, shields, fog/selection, placement and death.
  - [ ] Review provisional ability/repair/economy/weapon/flight balance.
- [[Done/Features/Patrol_Frigate_Import]]
  - [ ] In-game acceptance: movement, laser targeting/arcs, Power to Engines, shields, selection/fog, placement, death/wreck.
  - [ ] Provisional balance review. No Play Mode or automated tests run.
- [[Done/Features/Captor_Import]]
  - [ ] In-game movement, weapons, hangar, shield, placement and wreck acceptance; provisional balance review. No Play Mode or automated tests run.
- [[Done/Features/Resolute_Import]]
  - [ ] Verify in a clean skirmish: deployment, beam, Concentrate Fire, aura entry/exit/death, duplicate rejection, replacement and hangar launch.

## Audio

- [ ] [[Done/Features/Ui_Sfx_System]] — Listen for accepted clicks in main menu, battle and pause menus. Check keyboard/gamepad submit, disabled buttons, and dragging off a button before release.
- [ ] [[Done/Features/Ui_Sfx_System]] — Listen for station, ship/squadron, other-unit and hostile cues. Check box selection, selecting from the UI, silent pruning, and ship voice alongside the cue.

- [[Done/Features/Audio_System_Extension_Plan]]
  - [ ] User in-game listening: loudness, engine/flyby seams, shield cadence, ambience fade, announcer timing.
- [[Done/Features/Faction_Interface_And_Ability_Audio_Plan]]
  - [ ] Listen-verify `I000_EHD5503_ENG.wav` (Research Complete vs Tech Upgrade in progress).
  - [ ] User listening acceptance: volume, priority, timing, duplicate suppression.
- [[Done/Features/Music_Replacement_Plan]]
  - [ ] Standalone Windows audition: menu → battle → menu, every faction, track completion, pause, mute, single music source.
  - [ ] Listening quality vs sources; macOS build/playback (needs macOS build support); Audio Profiler memory/Streaming CPU.
- [[Done/Refactoring/Audio_System_Refactor_Plan]]
  - [ ] Manual verification (skirmish)

## UI and Settings

- [ ] [[Done/Refactoring/Shared_Invite_Friends_Popup_Plan]] — With two Steam accounts, receive a game invite over the open picker. Later/Escape restores selection. Join switches lobbies and closes the old picker.
- [ ] [[Done/Refactoring/Shared_Invite_Friends_Popup_Plan]] — In both lobby modes, verify the Steam overlay and in-game fallback, sending success/failure, and disconnect while the picker is open.

- [ ] [[Done/Refactoring/Empty_Interface_Members]] — Open setup from the main menu and launch a simulation.
- [ ] [[Done/Refactoring/Empty_Interface_Members]] — Open setup from the battle HUD and cancel while running and while paused. Start a replacement simulation.
- [ ] [[Done/Refactoring/Empty_Interface_Members]] — Switch Skirmish/Survival tabs on Load, reopen it, and verify Save hides the tabs.
- [ ] [[Done/Refactoring/Empty_Interface_Members]] — Run the `Ui` and `GameFlow` EditMode categories when authorized.

- [ ] [[Done/Features/Display_Modes_And_Monitors_Plan]] — Windows standalone: single/dual monitors at 60/144 Hz; all modes, size/Hz/monitor Keep/Revert, timeout/Escape, unplug fallback, saved-monitor startup and monitor FPS cap.
- [ ] [[Done/Features/Display_Modes_And_Monitors_Plan]] — macOS standalone: Retina, notch MacBook and external display; Native Fullscreen/Borderless/Windowed, actual-size labels, monitor move/revert and read-only Hz.


- [[Done/Features/Settings_Audit_Followups]]
  - [ ] Live settings acceptance: category edits, rebind capture, Keep/Revert display countdown, corrupt-file recovery.
- [[Done/Features/Tooltip_System_Plan]]
  - [ ] Verify the tooltip shows on disabled cards and that the delay and replacement behave correctly.
  - [ ] In-game acceptance: hover disabled station cards, switch targets rapidly, close routes, hide HUD, and exit the scene.
  - [ ] In-game acceptance: live health / production / abilities / rebinding, fog loss, marquee suppression, minimap markers, and screen-edge placement.
  - [ ] Run the authored EditMode tests only when explicitly requested.
- [[Done/Refactoring/Camera_System_Refactor_Plan]]
  - [ ] Manual: enter cinematic / drag minimap / reinforcement placement with pointer on screen edge → camera must not move; release → edge scroll resumes, no residual glide.
  - [ ] Manual: wheel forward zooms in; `R` zooms in; `F` zooms out; `InvertZoom` on reverses wheel + keys.
  - [ ] Manual: start cinematic in skirmish → shot lengths 3–8 s; resized ships fit the frame through all shot types.

- [ ] [[Done/Features/Battle_Notifications_Plan]] — Skirmish: card look/readability at game resolution, slide/fade, ✕ dismiss, no click-through around cards; stack clear of Survival HUD and tutorial card.
- [ ] [[Done/Features/Battle_Notifications_Plan]] — Trigger each card once (attack, hull 25%, hero/capital loss, grouped losses, fleet, enemy/own station level, sites, zones, hazard, research/unit/superweapon, rejections, survival waves); confirm silence during load/save restore and that the Outcome card does not clash with `EndGameUi`.
- [ ] [[Done/Features/Battle_Notifications_Plan]] — Run `Ui` (`BattleNotificationFeedTests`) and `Audio` categories when authorized; not run by agents.

## Gameplay and Balance


- [ ] [[Done/Features/Harmonic_Bomb_Ability]] — With both Fetts, verify stationary drop, 1.5 s fuse, 20 s activation cooldown, one spherical hit per eligible unit/member, allied immunity and 360/720 base damage under existing shield/armor rules; include caster death during the fuse.
- [ ] [[Done/Features/Harmonic_Bomb_Ability]] — Review the blue planar main/secondary rings, white-blue core, 210-unit radius, fog visibility and overlapping/pool reuse at battle zoom levels and quality presets (Performant retains HDR off).
- [ ] [[Done/Features/Harmonic_Bomb_Ability]] — Check three-visible-member autofire, moving targets, and save/load during the fuse (pending bomb discarded; remaining cooldown retained).
- [ ] [[Done/Features/Harmonic_Bomb_Ability]] — Listen to each pilot’s activation voice and one positional seismic blast per detonation, including empty/overlapping drops.




- [ ] [[Done/Features/Auto_Power_Abilities_Plan]] — Battle: player-only automatic starts, individual selection pause, multi-selection use, manual cancellation/non-cancellable effects, settings and saved per-ship preferences; include Allegiance / ISD II Main Batteries auto-start and normal 20 s active → 60 s recovery (2026-11-01 extension; Core regressions authored, not run).
- [ ] [[Done/Features/Auto_Power_Abilities_Plan]] — Balance: shared shield regeneration ×4 and initial automatic weapon/shield thresholds across fleet matchups.
- [ ] [[Done/Features/Auto_Power_Abilities_Plan]] — UI: single/group A, mixed/disabled/paused tooltips, active duration and cooldown feedback at the user's game resolution.

- [[Done/Features/More_Mining_Sites]]
  - [ ] User in-game check of generated maps (all sizes)
- [[Done/Features/AI_Composition_Counters]]
  - [ ] Skirmish Play Mode acceptance (not run).
- [[Done/Features/Game_Modes_Survival_And_Simulation]]
  - [ ] Tests: builder presets; module selection from features; super weapons off → no requests/controllers; skirmish regression (existing EditMode suite green).
- [[Done/Refactoring/Weapon_Taxonomy_Balance_System]]
  - [ ] Play Mode / skirmish acceptance (user playtest).
  - [ ] Balance tuning pass after playtest.
- [[Done/Refactoring/Ship_Scale_Original_Values_Plan]]
  - [ ] In-game visual check of normalized ship scales (Venator = 1137 m); run relevant EditMode suites on request.

## Core Refactors

- [ ] [[Done/Refactoring/Empty_Code_Cleanup]] — Select and deselect a ship. Confirm its selection ring appears and disappears.
- [ ] [[Done/Refactoring/Empty_Code_Cleanup]] — Start a skirmish and confirm the battle loads.
- [ ] [[Done/Refactoring/Empty_Code_Cleanup]] — Run EditMode categories `BattleInput`, `Squadrons`, `GameFlow` and `Core`.


- [[Done/Refactoring/Auto_Power_Refactor]]
  - [ ] Run EditMode `Abilities`, `Ships`, `Core`, `EnemyAi`, `Balance`; skirmish check that auto power still fires shields/weapons/laser beams as before.

- [[Done/Refactoring/Capture_Reinforcement_Tally_Dedup]]
  - [ ] Skirmish check: capture-site and reinforcement squadron strength behave as before.
- [[Done/Refactoring/Unit_Request_Identity_Keys]]
  - [ ] Skirmish check: reinforcement requests and cheat-added ships resolve the correct unit.
- [[Done/Refactoring/Explicit_Component_Binding]]
  - [ ] Run `SerializedReferenceTests` (on request only).
  - [ ] Play check: main menu + skirmish, no missing references; range cheat draws rings.
- [[Done/Refactoring/Battle_Startup_Sequence_Plan]]
  - [ ] Acceptance in Play Mode: no fog fade-in, no camera drift, no first-frame hitch, Zenject graph resolves (orchestrator ↔ `CameraService` cycle relies on method injection), Esc ignored while loading, pause/speed/menu/end/exit.
- [[Done/Refactoring/Ship_Squadron_Entity_Simplification_Plan]]
  - [ ] Validate recorded behavior changes in a skirmish (merged in PR #16).
- [[Done/Refactoring/Zenject_To_VContainer_Migration]]
  - [ ] Compare spawn/startup/teardown and frame timings with baseline; measure spawn bursts (reinforcements, mass squadron launch) for per-entity scope build cost. No performance claims from library change alone.
- [[Done/Bugs/Scene_Teardown_And_Material_Leaks]]
  - [ ] Manual: skirmish → wrecks/explosions → exit to menu → no Console exceptions.
  - [ ] Manual: open/cancel placement ×20 → material count flat.

## Visuals and Performance

- [ ] [[TODOs/Optimization/Battle_FPS_Recovery_Plan]] — November 7 implemented phase: review station-only burst scale 0.75 at near/far zoom with nearby fighters and HUD. [Matched captures](../../Docs/Audits/2026-11-07-Optimization-Fixes/station-comparison.png); packed smoke/fog/pool lifecycle already verified.
- [ ] [[TODOs/Optimization/Asset_Size_Followup_Plan]] — November 7 implemented phase: approve three compressed Providence maps at closest zoom/team colors on Windows and Mac; listen to menu/battle transitions and voice ducking. Numeric gates, Mac packed playback and handle release already verified.

- [ ] [[Done/Optimization/Graph_Pathfinding_Plan]] — Run the Unity EditMode `Navigation` category. Confirm grid parity, disk clearance, overlapping-disk escape, hazard detours and Quasar/Titan/Executor destination reservations. Retain the Editor grid oracle until the parity results are accepted.
- [[Done/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan]]
  - [ ] Visual approval of `Logs/ShipLitConversion/2026-09-28/review.html` before/after gallery.
- [[Done/Optimization/Squadron_Pooling_Plan]]
  - [ ] Run pooling EditMode tests on request (previous run blocked by stale `Assembly-CSharp` domain).
  - [ ] User visual check of respawned squadrons: selection, minimap, fog, audio.
  - [ ] User in-game check: fighter look, explosions (size/fog), loading time with ~100 prewarmed squadrons, hangar spikes.

## Space Hazards

- [[TODOs/Features/Space_Hazards_Plan]]
  - [ ] In-game review: colours/opacity, island layout, asteroid clumps, both hazard types readable; gameplay effects (abilities jammed, shields offline, reinforcement block).
  - [ ] Tests on request: `Map` category, `MapExhaustive` (`--include_explicit true`), clean full-suite comparison.

## Imported Units (records without a plan)

- [[AssetPipeline/Imports/Acclamator Assault Ship Imperial Import]]
  - [ ] Runtime acceptance: firing arcs/salvos, TIE launches, interception, ability, reinforcement placement, death/wreck; playtest provisional tuning.
- [[AssetPipeline/Imports/Bothan Assault Cruiser Import]]
  - [ ] Combat/fire arcs, hangar launches/replacements, subsystem destruction, Coordinated Assault/Tractor Beam timing, fog/selection, placement, wreck.
- [[AssetPipeline/Imports/Corellian Battlecruiser Import]]
  - [ ] Replacements, launch sequence, firing arcs, system destruction, shield ability, fog/selection, placement, death/wreck; provisional balance.
- [[AssetPipeline/Imports/Corellian Corvette Import]]
  - [ ] Clean-skirmish combat, Power to Engines activation/recovery, shields, banking, fog/selection, reinforcement, wreck; provisional size/range/arcs.
- [[AssetPipeline/Imports/Dauntless Import]]
  - [ ] Firing arcs, initial/replacement launches, subsystem destruction, Sensor Jamming accuracy/range/expiry, fog/selection, reinforcement, wreck.
- [[AssetPipeline/Imports/Endurance Fleet Carrier Import]]
  - [ ] Firing/arcs, all four hangars, coordination/tractor beam, shield, selection/fog, death timing.
- [[AssetPipeline/Imports/Home One Import]]
  - [ ] Target selection, damage bonus cleanup, fleet command expiry, shield-generator destruction, engine/bay disable, launches, banking, fog, placement; provisional economy/scale.
- [[AssetPipeline/Imports/ISD III Import]]
  - [ ] Beam timing/armor/shields, class restriction, moving-target rendering, tractor cancellation, AI ability use, hangar clearance, banking, explosions, wreck.
- [[AssetPipeline/Imports/Quasar Fire Import]]
  - [ ] Runtime launch/replacement, tractor beam, fog/selection, death.

## Main Relay Map Layout

- [[Done/Features/Main_Relay_Map_Layout_Adjustments]] — Fresh Small/Medium/Large 2p/4p gameplay: confirm the 1250–1275-wide asteroid fields and preferred hazards create meaningful detours, zones remain reachable, and home sites stay behind stations when unobstructed.
- [[Done/Features/Main_Relay_Map_Layout_Adjustments]] — Run updated Map, MapGeneration, MapExhaustive, Capture and Core checks only on explicit request. No tests or Play Mode run by agents.


## Main Relay Zone Map Rework

- [[Done/Features/Main_Relay_Zone_Map_Rework]] — Run updated Map, MapGeneration, MapExhaustive, GameFlow, Capture and Core checks on explicit request; no tests run by agents.
- [[Done/Features/Main_Relay_Zone_Map_Rework]] — After gameplay acceptance, update GameDesign map/economy references.

## Note Flags

- [ ] [[Done/Documentation/Note_Flags_And_Durable_Guidance_Plan]] — Open [[Rules/NOTE_FLAGS]] in Obsidian and visually confirm all five native flag views and combined searches render; important finds both pilots, issue/broken reflect their actual tags, and unrelated topic tags are excluded.

- [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]] — Mac visual acceptance: 77% FSR 1 sharpness, FXAA-off edges, unchanged hazard silhouettes/opacity (including holes and narrow arms).
- [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]] — Mac loading/first-use acceptance: battle renders beneath the cover before fade-out; confirm no keyword-space errors. Broad warmup removed by [[Done/Bugs/Shader_Warmup_Keyword_Errors]]; later Metal PSO hitches remain possible.
- [[Done/Optimization/Mac_GPU_Frame_Budget_Plan]] — Next Mac-hosted build: confirm automatic post-BuildInfo codesign and strict verification succeed.

## Enemy AI Defense and Zone Recovery

- [ ] [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]] — Ultra Hard Republic/CIS in both player-faction directions: repeat long-range station bombardment; arrived Providence/other defenders intercept before personal damage, and hidden/lost targets end or redirect pursuit.
- [ ] [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]] — Safe-zone expansion/home loss: reinforcement-zone floor beats closer facilities, unsafe nearest target permits an alternative, stopped-outside ships recover, inside ships stay, dead/contested home anchors are replaced.
- [ ] [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]] — Compare new AI/order logs: useful defense orders, objective ownership/progress, retreat duration, interruption causes, Providence effective range/contact/facing before/after incoming attacks; no historical wake cause proven.
- [ ] [[Done/Bugs/Enemy_AI_Defense_And_Zone_Recovery_Plan]] — Run authored regressions only on explicit request; EnemyAi coverage plus affected Capture, Ships, Squadrons, BattleInput and constructor/fake compatibility in Navigation/Simulation. No tests run by agents.

## Enemy AI Recovery Review Fixes

- [ ] [[Done/Bugs/Enemy_AI_Recovery_Review_Fixes]] — Run authored EnemyAi regressions and affected Navigation checks only on explicit request; tests not run by agents.
- [ ] [[Done/Bugs/Enemy_AI_Recovery_Review_Fixes]] — Battle: en-route safe retreat keeps its anchor despite fleet reservations; obstacle/hostile anchors remain rejected; long defense/retreat followed by capture preserves viable objectives and genuine stalled captures still retry after cooldown.

- [ ] [[Done/Bugs/Enemy_AI_Independent_Task_Groups]] — In-game: threatened combat group retreats while a safe capture detachment continues; verify group split/merge, objective handoff, home loss and destruction/reinforcement changes.
- [ ] [[Done/Bugs/Enemy_AI_Independent_Task_Groups]] — In-game: available squadrons spread across capture objectives; scouts, escorts and active carrier guards keep their assignments.
- [ ] [[Done/Bugs/Enemy_AI_Independent_Task_Groups]] — On explicit request, execute EnemyAi regressions (11 new cases); tests were not run under the local override.

## Enemy AI Endgame Defense and Capture

- [ ] [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]] — Siege: no defense gap > 5 s while the station loses durability; defenders have targets in range within 15 s of latch; single superweapon hit does not recall the fleet.
- [ ] [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]] — Critical station: no Mining/Defense/satellite purchases; first emergency unit deploys before station loss when credits allow.
- [ ] [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]] — Centre: main force reaches 100% capture at least once with capitals present; < 5 Capture↔Hold changes per 30 s.
- [ ] [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]] — Phase 5: Carrack/CR70/Centax share drops versus the 17:27 battle (63/205 Carrack); `strikecraftCovered` appears in `counter-selection`; no distant raid while a stronger known force sits on the route.
- [ ] [[Done/Bugs/Enemy_AI_Endgame_Defense_And_Capture_Fix]] — Run authored EnemyAi/Combat regressions only on explicit request; tests not run by agents.

## Reinforcement Drag Batch Spawn

- [ ] [[Done/Features/Reinforcement_Drag_Batch_Spawn]] — Deploy one ship, immediately drag another → preview appears and deploys.
- [ ] [[Done/Features/Reinforcement_Drag_Batch_Spawn]] — Shift-drag with reserve 4, room for 3 → 3 deploy in a grid, panel shows `x1`; blocked slots stay in reserve.
- [ ] [[Done/Features/Reinforcement_Drag_Batch_Spawn]] — Batch slot spacing looks right for small and large hulls (`SLOT_SPACING_FACTOR` 2.2).

## Battle area access

- [ ] [[Done/Refactoring/Battle_Area_Access]] — Compare free structure placement at the same point for player, AI and cheat. Check a friendly relay edge, site order snapping and fog-disabled overlay in battle.
- [ ] [[Done/Refactoring/Battle_Area_Access]] — Run the plan’s affected EditMode categories when authorized, including capture-disabled composition and overlapping area registrations.

## Fog feature access

- [ ] [[Done/Refactoring/Fog_Disabled_Stand_Ins]] — Start a fog-enabled battle. Check reveal before fade-out and visibility on the minimap.
- [ ] [[Done/Refactoring/Fog_Disabled_Stand_Ins]] — Start a fog-disabled battle. Check fade timing and a fully revealed minimap.
- [ ] [[Done/Refactoring/Fog_Disabled_Stand_Ins]] — Restart or restore with different bounds. Check the fog plane fits the new map.
- [ ] [[Done/Refactoring/Fog_Disabled_Stand_Ins]] — Run Vision, Map, Ui, GameFlow, Core and BattleInput when authorized.

## Play menu reroute

- [ ] [[Done/Features/Play_Menu_Reroute_Plan]] — In-game: Play → Single Player (Skirmish, Survival, Load Game), Multiplayer, AI vs AI, Tutorial; every Back/Escape returns one level.
- [ ] [[Done/Features/Play_Menu_Reroute_Plan]] — Two PCs: Host Game, client joins from the Server Browser (and by IP); host switches Skirmish ↔ Survival; Survival blocked with 4 members.
- [ ] [[Done/Features/Play_Menu_Reroute_Plan]] — Run Ui, Networking and GameFlow when authorized.

## Tactical markers

- [ ] [[Done/Features/Tactical_Markers_Plan]] — In-game: B tap/hold/rebind, all six slices, hub/Esc/RMB cancel, world/minimap target attachment, pause, hidden/dead targets, load cleanup and notification focus.
- [ ] [[Done/Features/Tactical_Markers_Plan]] — Two clients: allied markers/replies arrive; enemy clients receive none; disconnect and late scene loading do not retain stale markers.
- [ ] [[Done/Features/Tactical_Markers_Plan]] — Allied AI: accept/delay/decline, capture/defense/scouting, safe attack/rally/retreat, autonomous markers, silent solo enemy AI and one-time tutorial hint.
- [ ] [[Done/Features/Tactical_Markers_Plan]] — Approve world/radial/minimap visuals at battle zoom/resolution and listen to info/urgent cues. Art and radial previews: `Logs/TacticalMarkers/`.


## Ship Route Curvature

- [ ] [[Done/Bugs/Ship_Route_Curvature_Plan]] — Run `Navigation` and `Ships` tests; execute both explicit `ShipArchetypeRouteSweepTests(false/true)` fixtures. Tests were not run under the local override.
- [ ] [[Done/Bugs/Ship_Route_Curvature_Plan]] — In-game: fields/overlapping disks, hazard corridors, map edges, Executor clearance, moving replans and ion disable/resume; confirm continuous bends and safe escape/turnaround behavior.
- [ ] [[Done/Bugs/Ship_Route_Curvature_Plan]] — Matched before/after profiler capture: `NavigationRouteBuild`, movement tick, queue latency with eight fleets on an eight-player Large map. Record source/workload and compare CPU; no improvement measured yet.
- [ ] [[Done/Bugs/Ship_Route_Curvature_Plan]] — Online client: measure keyframes per moving ship and route-message traffic on eight-player Large. Compact piece messages remain a conditional follow-up only if route traffic matters.

## Map Archetypes

- [ ] [[Done/Features/Map_Archetypes_Plan]] — Approve the proposed 57 planet→archetype assignments and generated layout previews in the skirmish gallery.
- [ ] [[Done/Features/Map_Archetypes_Plan]] — In-game visual/gameplay review: capital-ship passage width, grouped/separated ally starts, Orbital planet alignment, hazard readability and neutral-objective access. Gallery artifacts are layout schematics, not battlefield-material captures.
- [ ] [[Done/Features/Map_Archetypes_Plan]] — Measure eight-fleet CPU/GPU budget against [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]; include AI, minimap and battle telemetry workload.
- [ ] [[Done/Features/Map_Archetypes_Plan]] — Live multiplayer with matching builds: 6/8-player lobby, startup, save/load, disconnect and synchronized map generation. Older four-player clients are not covered.


## Planet Decorations

- [ ] [[Done/Features/Planet_Decorations_Plan]] — Approve proposed planet assignments for shipyard docks, space junk and resource containers.
- Visibility gate failed 2026-11-13: no Kuat/Tatooine props → [[Done/Bugs/Planet_Decorations_Loading_Fix]].
- [ ] [[Done/Features/Planet_Decorations_Plan]] — After visibility repair, review model/texture fidelity, dock mix, scale, height, lane readability and always-visible scenery through gameplay fog; include Small Kuat.
- [ ] [[Done/Features/Planet_Decorations_Plan]] — Run authored Map tests only on explicit request; tests were not run during implementation.

- [ ] [[Done/Bugs/Planet_Decorations_Loading_Fix]] — Kuat visibility confirmed by user; verify corrected dock orientation in battle and junk visibility in a fresh Tatooine battle. Spawn now preserves the imported prefab root rotation under world yaw.
- [ ] [[Done/Bugs/Planet_Decorations_Loading_Fix]] — Run the authored loader/view and imported-orientation regressions in category Map only on explicit request; tests not run during this fix.

- [ ] [[Done/Features/Planet_Decorations_Plan]] — In a fresh junk planet battle, confirm 3× junk scale is readable at normal zoom and interior placement looks right; SpaceJunk uses scale 0.18–0.30 and central 70% bounds. Sampled Tatooine seed-7 counts are 3/7/7 for Small/Medium/Large.

## Reinforcement Spawn Near Station

- [ ] [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]] — In-game: small/large ships near/above/below every faction station level (nav disk now hull-sized, not 396 u — check ships path around station hull cleanly); grid/preview parity after refresh, batch spacing, reservations and main-station destruction.
- [ ] [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]] — Profile a large-map ship placement using Battle.SpawnArea.Redraw and Battle.SpawnArea.Reachability; compare frame times with the battle performance budget. No runtime measurements yet.
- [ ] [[Done/Bugs/Reinforcement_Spawn_Near_Station_Fix]] — Run authored Navigation/Map/Economy regressions and affected BattleInput/EnemyAi compatibility checks only on explicit request; tests were not run.

## Small Ship Icons and Squadron Icon Grid
- [ ] [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]] — In-game: inspect all 12 ship markers, Stealth Corvette cloak/fog, individual squadron slots through idle/move/combat, merges/splits and passing traffic, viewport edges, dense screen overlap, zoom and pause.
- [ ] [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]] — Development Player: capture comparable 32/128/512 visible markers and 321 squadrons + 83 ships with dense, dispersed and high-churn layouts. Record p95 main-thread, worker time, candidate counts, canvas rebuild and GC bytes; tune using measured results.
- [ ] [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]] — Run authored Squadrons, Ships, BattleInput, Vision and Core regressions only on explicit request.
