---
category: Documentation
status: in-progress
created: "2026-10-29\r"
---
# User Acceptance Queue

## Repair Satellite

- [ ] [[Done/Features/Repair_Feature_Plan]] — In battle: build a Repair Satellite on a captured satellite pad; check the model/scale on the pad, the radius ring when selected, that damaged own/allied ships and structures in range regain hull + live hardpoints (not shields, not destroyed hardpoints), and that it stops while ion-disabled. Tune `0.5 %/s`, radius `400`, `10` targets, `2` per target, cost `500`. Decide on ship repair beams.
- [ ] [[Done/Features/Repair_Feature_Plan]] — Run `Health`, `Capture`, `Core`, `GameFlow`, `Audio` EditMode categories when explicitly authorized; compile passed, tests not run.

## Engine Hardpoints

- [ ] [[Done/Bugs/Invincible_Single_Engine_Slowdown]] — In battle: destroy one engine on Invincible and Secutor → speed 25 → 20 (not 10); check the 3 engine plumes sit on Invincible's nozzles; load an old save with either ship. Run `Ships`, `Health`, `GameFlow` when authorized; compile passed, tests not run.

## Online Skirmish Lobby

- [ ] [[Done/Bugs/Online_Skirmish_Join_Slot_Race]] — Run authored regression coverage in `Networking`, `GameFlow` and `Ui` when explicitly authorized; source review and Unity compilation passed. User declined reproduction as a prerequisite.

## Online Battle

- [ ] [[Done/Bugs/Ship_Ability_Event_Destroyed_Caster]] — Host a battle; confirm no `Not entity found` in `Player.log`. `Abilities` test added, not run.
- [ ] [[Done/Bugs/Ion_Pulse_Client_No_Wave]] — 2 machines: client casts Ion Pulse at a target dead ahead right after the Malevolence lands ×5 → wave on both each time; host casts → client sees the wave; stun arcs on client. Run `Abilities` when authorized; compile passed, tests not run.

## Group Attack Pace

- [ ] [[Done/Features/Spatial_Pace_Clusters_Plan]] — In battle: test formation Move / Attack / AttackMove clusters, double right-click loose move (green wave, own speeds), asteroid-split groups, squadron breakaway on engagement, and damaged-engine / stuck ship detach.
- [ ] [[Done/Features/Spatial_Pace_Clusters_Plan]] — Run `Navigation`, `BattleInput`, `Networking`, `Ui`, `Abilities`, `EnemyAi`, `Map`, and `Simulation` EditMode categories when explicitly authorized; Unity compile passed (`0` errors), tests not run under local override.
- [ ] [[Done/Features/Group_Attack_Speed_Matching_Plan]] — In battle, attack with a Venator + its squadrons: confirm the fighters stay with the ships until the attack zone, then fly their own passes.
- [ ] [[Done/Features/Group_Attack_Speed_Matching_Plan]] — Run `BattleInput` and `Squadrons` EditMode categories (`UnitOrderServiceTests`, `FighterFlightTests`) when authorized; compile passed.

## Fighter and Interceptor Rebalance

- [ ] [[Done/Bugs/Fighter_Interceptor_Rebalance]] — In battle: interceptors beat same-faction fighters 1v1 and shred bombers; fighters still worth buying (equal credits); faster TIEs keep escort/attack-run timing; X-Wing vs TIE Avenger close; TIE Defender beats any Rebel squadron; Gladiator launches TIE Fighters; V-Wing (`1.22×`) not too strong vs ships.

## Squadron Engagement

- [ ] [[Done/Bugs/Squadron_Idle_And_Guard_Engagement]] — In battle: an idle hero squadron attacks enemies ~250 u away and flies back to its post after the fight; a guarding squadron breaks off a chase ~520 u from its ship; bombers still follow the carrier's target. Run `Squadrons` when authorized; compile passed.

## Planet Atmosphere

- [ ] [[Done/Features/Planet_Biome_Atmosphere_Plan]] — In battle, check light/tint on Tatooine, Coruscant, Hoth, Mustafar, Kamino, Dagobah; confirm team colours stay readable and the menu has no tint after leaving.

## Map Generation

- [ ] [[Done/Bugs/Map_Enemy_Station_Near_Allies_Fix]] — In game: Medium + Large, 2v2 + 3v3, one Open Field planet (Coruscant) and one Orbital Ring planet (Alderaan/Endor): allies together, enemies clearly farther.

## Content Removal

- [ ] [[Done/Refactoring/ISDIII_Thranta_Removal]] — Run `Ships`, `Health`, `Navigation`, `Combat`, `GameFlow`, `Ui`, `Audio` EditMode categories when authorized; source review and Unity compilation passed.

## Diagram Tooling

- [ ] [[Done/Tooling/PlantUml_Diagram_Tooling]] — Run `unity command diagram_generate --paths "Assets/Scripts/Services/Enemy" --name enemy_ai --open rider`; confirm the plantuml4idea preview in Rider, then `diagram_open --name enemy_ai --target browser` and `--target editor` (zoom/pan); `Tools/Diagram Generator` window still shows its icons.
- [ ] [[Done/Tooling/PlantUml_Diagram_Tooling]] — Run `Core` EditMode category (`DiagramToolingTests`) when authorized; compile passed, tests not run.

## Rules

- One line per user-only gate (in-game, listening, visual, tests on request) of a closed plan; policy → root `AGENTS.md` Plan Lifecycle step 4.
- Passed → delete the line; when a plan has no lines left, set its `acceptance: passed`.
- Failed → reopen the plan or open a `Bugs` plan; replace the line with its link.
- Imported-unit balance values are provisional; balance review is part of each unit's gate.

## Imported Units

- [[Done/Features/DarthVader_Squadron_Import]] — In-battle deployment, Remake model + six escorts, balance, escort loss/replenishment/60 s cooldown, multiplayer/save restoration.
- [[Done/Features/DarthVader_Squadron_Import]] — Automated Squadrons/Abilities/Combat/Balance/Simulation and asset checks remain unrun under local restriction; explicit test request required.

- [ ] [[Done/Features/Recusant_Dreadnought_Import]] — Build and deploy the CIS dreadnought in skirmish. Check roster availability, placement clearance, forward convergence, Boost Weapon Power, squadron launches and wreck transition.
- [ ] [[Done/Features/Recusant_Dreadnought_Import]] — Check team markings and engine and ship-order sound in battle.

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

- [ ] [[Done/Features/Ship_Ui_Destroy_Plan]] — Offline: destroy one owned ship and verify normal effects, cleanup and no refund. Verify selection/ability interactions, menu placement/dismissal and expanded/collapsed card reuse.
- [ ] [[Done/Features/Ship_Ui_Destroy_Plan]] — Online: host and remote co-op/PvP clients each destroy their own ship; verify ally/enemy requests fail and all peers see the same death and selection cleanup.
- [ ] [[Done/Features/Ship_Ui_Destroy_Plan]] — Repeat under latency and disconnect while the confirmation is open or the request is pending.
- [ ] [[Done/Features/Ship_Ui_Destroy_Plan]] — Run the affected `Ui`, `BattleInput` and `Networking` EditMode fixtures when requested.

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

- [ ] [[Done/Features/Artillery_Ship_Standoff_Plan]] — In battle, confirm approach and retreat for the five artillery ships with individual and formation Attack. Check main/secondary facing, auto-power, shorter launcher tradeoff, partial and full launcher loss, restoration, moving targets and blocked routes. Include a 4+ ship formation Attack (far-side slots) and a blocked approach against a stationary target. Spot-check ordinary behavior on Profundity and AttackMove/Guard/Hunt.
- [ ] [[Done/Features/Artillery_Ship_Standoff_Plan]] — Compare matched baseline/changed Editor and Windows Development Player fleet captures: same seed, counts, orders and map; record revisions and dirty files. Check path requests, `NavigationPathSearch`, `NavigationNearestReachable`, steady-state allocations, and CPU median/p95/p99.

- [ ] [[Done/Features/Capital_Siege_And_Forward_Batteries_Plan]] — At fixed distances against a same-level station and platform, compare HeavyCapital/Capital shield and hull DPS with identical research and game speed.
- [ ] [[Done/Features/Capital_Siege_And_Forward_Batteries_Plan]] — In an uncheated battle, confirm close/far Executor reach, forward batteries and stable stationary facing. Disable/destroy the main battery and confirm fallback and recovery, including Pelta/Centax and Power to Main Batteries.
- [ ] [[Done/Features/Capital_Siege_And_Forward_Batteries_Plan]] — Run the authored Combat, Ships, Navigation, Squadrons, Simulation and EnemyAi regressions when explicitly requested.


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
  - [ ] Acceptance in Play Mode: no fog fade-in, no camera drift, no first-frame hitch, the VContainer battle scope resolves its startup dependencies, Esc ignored while loading, pause/speed/menu/end/exit.
- [[Done/Refactoring/Ship_Squadron_Entity_Simplification_Plan]]
  - [ ] Validate recorded behavior changes in a skirmish (merged in PR #16).
- [[Done/Refactoring/Zenject_To_VContainer_Migration]]
  - [ ] Compare spawn/startup/teardown and frame timings with baseline; measure spawn bursts (reinforcements, mass squadron launch) for per-entity scope build cost. No performance claims from library change alone.
- [[Done/Bugs/Scene_Teardown_And_Material_Leaks]]
  - [ ] Manual: skirmish → wrecks/explosions → exit to menu → no Console exceptions.
  - [ ] Manual: open/cancel placement ×20 → material count flat.

## Visuals and Performance

- [ ] [[Done/Optimization/Enemy_Production_CPU_Spike_Plan]] — Run the `EnemyAi` EditMode regressions for threat snapshot parity and placement reuse.

- [ ] [[TODOs/Optimization/Battle_FPS_Recovery_Plan]] — November 7 implemented phase: review station-only burst scale 0.75 at near/far zoom with nearby fighters and HUD. [Matched captures](../../Docs/Audits/2026-11-07-Optimization-Fixes/station-comparison.png); packed smoke/fog/pool lifecycle already verified.
- [ ] [[TODOs/Optimization/Asset_Size_Followup_Plan]] — November 7 implemented phase: approve three compressed Providence maps at closest zoom/team colors on Windows and Mac; listen to menu/battle transitions and voice ducking. Numeric gates, Mac packed playback and handle release already verified.

- [ ] [[Done/Optimization/Graph_Pathfinding_Plan]] — Run the Unity EditMode `Navigation` category. Confirm grid parity, disk clearance, overlapping-disk escape, hazard detours and Quasar/Titan/Executor destination reservations. Retain the Editor grid oracle until the parity results are accepted.
- [[Done/Refactoring/ShipLit_Autodesk_Material_Conversion_Plan]]
  - [ ] Visual approval of `Logs/ShipLitConversion/2026-09-28/review.html` before/after gallery.
- [[Done/Optimization/Squadron_Pooling_Plan]]
  - [ ] Run pooling EditMode tests on request (previous run blocked by stale `Assembly-CSharp` domain).
  - [ ] User visual check of respawned squadrons: selection, minimap, fog, audio.
  - [ ] User in-game check: fighter look, explosions (size/fog), loading time with ~100 prewarmed squadrons, hangar spikes.

- [ ] [[Done/Optimization/Projectile_Visual_Reduction_Plan]] — In a large battle: check bolt look and feel after Phase 2 (point defense 4 bolts, lasers/turbolasers/mass drivers 2, ion 1 faster bolt, fighter lasers slower cadence); rerun `InputBindingServiceTests.TooltipDisplaysFixedCancelBinding`.

## Space Hazards

- [[TODOs/Features/Space_Hazards_Plan]]
  - [ ] In-game review: colours/opacity, island layout, asteroid clumps, both hazard types readable; gameplay effects (abilities jammed, shields offline, reinforcement block).
  - [ ] Tests on request: `Map` category, `MapExhaustive` (`--include-explicit true`), clean full-suite comparison.

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

## Enemy AI UltraHard Collapse and Attack Order Loss


## Enemy AI Active Play and Ship Combat

- [ ] [[Done/Bugs/Enemy_AI_Active_Play_And_Ship_Combat]] — Run affected EditMode fixtures when explicitly authorized (EnemyAi, Capture, Ships, Navigation, BattleInput, Economy, GameFlow and dependent fixture bindings).

## Enemy AI Economy Strategy

- [ ] [[TODOs/Bugs/Enemy_AI_Economy_Strategy]] — Run affected EnemyAi, Capture, Economy, Ships and Core composition fixtures when authorized.
- [ ] [[TODOs/Bugs/Enemy_AI_Economy_Strategy#Fixes N1–N6 (implemented 2026-11-19 evening; Unity compile 0 errors; tests not run; battle pending)|Enemy AI allocator N1–N6]] — UltraHard Small 1v1: ships gather and go as one force (no lone ships into the centre); no attack on a stronger zone; pull-back happens; wings and ships sabotage mines when behind; wings avoid sites they cannot win.
- [ ] [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]] — Next UltraHard battle: `deploymentBlocked` with `heldSeconds > 15` = 0 and `heldShips` ≤ 2; ≤ 2 level-1 corvettes bought after level 2; `fleetAdvantage` ≥ 1 at 30–120 s with no hostile ship seen; centre pads owned before 120 s vs squadrons; home + centre + ≥ 1 outer zone by ≈ 240 s when uncontested (`zoneOwner` timeline).
- [ ] [[Done/Bugs/Enemy_AI_Economy_Strategy_Regression_Fixes]] — Run authored `Ships`, `EnemyAi`, `Capture`, `Economy` EditMode regressions when explicitly authorized (compiled with 0 errors; tests not run under local override).

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

## Executor Depth and Big-Ship Targeting

- [ ] [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]] — Executor at mid zoom: about 20% smaller apparent size, clearly below stations; cinematic framing, overlays and save/load remain correct.
- [ ] [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]] — Small map: no planet/hull intersections, planet framing acceptable, superweapon beam starts on the surface.
- [ ] [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]] — Enemy station above Executor: click selects station, right-click attacks station; exposed Executor hull remains selectable.
- [ ] [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]] — Fleet attack on a surrounded Executor: outer ships retry toward open slots and fire; inspect large-battle retry cost.
- [ ] [[Done/Bugs/Executor_Depth_And_Big_Ship_Targeting_Fix]] — Run targeted Ships, Map, BattleInput, Navigation and Combat tests only when explicitly requested; regression sources were authored but execution was disabled.

## Small Ship Icons and Squadron Icon Grid

- [ ] [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]] — In-game: inspect all 12 ship markers, Stealth Corvette cloak/fog, individual squadron slots through idle/move/combat, merges/splits and passing traffic, viewport edges, dense screen overlap, zoom and pause.
- [ ] [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]] — Development Player: capture comparable 32/128/512 visible markers and 321 squadrons + 83 ships with dense, dispersed and high-churn layouts. Record p95 main-thread, worker time, candidate counts, canvas rebuild and GC bytes; tune using measured results.
- [ ] [[Done/Features/Small_Ship_Icons_And_Squadron_Grid_Plan]] — Run authored Squadrons, Ships, BattleInput, Vision and Core regressions only on explicit request.

## Enemy AI patrol and recovery relocation

- [ ] [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]] — Battle `Loading_20261114_125624` passed checks 1, 2, 6, 7, 10; checks 3–5 failed → [[Done/Bugs/Enemy_AI_Battle_Log_Followup_Fixes]]. Remaining: weak/parity near-far (8) and allied/neutral-anchor (9) scenarios in a team game; tune starting values.
- [ ] [[Done/Features/Enemy_AI_Patrol_And_Recovery_Relocation]] — Run authored EnemyAi regression tests only when explicitly requested; tests were not run under the local override.

## AI patrol/recovery review fixes

- [ ] [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]] — Battle-check distant attacked mines, near-parity station defense during backup construction, outmatched withdrawal and stable pressure transitions; verify recovery log parsing.
- [ ] [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]] — Run authored EnemyAi regressions on explicit request; tests not run under the local override.
- [ ] [[Done/Bugs/Enemy_AI_Battle_Log_Followup_Fixes]] — Replay a battle: capture `objectiveSwitch` none-bounces near 0, `patrolTarget` rows ≥ 1/min through sieges, no long recovery latch with a standing backup, patrol/production-wait numbers parse with `.` decimals.
- [ ] [[Done/Bugs/Enemy_AI_Battle_Log_Followup_Fixes]] — Run authored EnemyAi regressions on explicit request; tests not run under the local override.
- [ ] [[Done/Bugs/Enemy_AI_Patrol_Recovery_Review_Fixes]] — Profile a large blocked map with multiple AI: placement/scout query duration and candidate/threat counts. Repeated per-candidate threat scans remain unoptimized; no runtime timing measured.


## Squadron member-count rework

- [ ] [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]] — Fresh-match role duels and equal-cost fleets (levels2/3/5): carrier deployment/replacements, torpedo burst damage and faction strength. Stats/carrier implementation verified; station/purchase economics remain separately active.
- [ ] [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]] — Check13/11-craft CIS formations, launch overlap, member death/pooled respawn visuals, fresh save/load and matching-client multiplayer. Old member-array saves require a fresh match; no migration added.


## Spawn grid overlay performance

- [ ] [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]] — Large-map small/Executor-class placement and deploy-all: compare Battle.SpawnArea.Redraw, Battle.SpawnArea.Reachability, worker time and GC allocations. No measured speedup yet.
- [ ] [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]] — Verify initial pending mask, rapid selection changes, moved/destroyed/upgraded stations, reservations, preview/drop parity and battle exit while a batch is pending.
- [ ] [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]] — Run authored Navigation and affected Economy/EnemyAi regressions only on explicit request. Tests not run under local override.
- [ ] [[Done/Optimization/Spawn_Grid_Overlay_Performance_Plan]] — Recheck full Unity compilation after unrelated ShipSelectionReuseTests.cs:43,49,53 Configure-signature errors are resolved; no spawn/navigation errors reported by the current compile.

## Multiplayer Reconnect

- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — On two PCs, use Pause Exit → Multiplayer Reconnect. Verify the original seat and battle state are restored.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — On two PCs, use Pause Exit → Steam Join Game and repeat via Steam invite. Verify an invite to the current lobby is ignored during battle and switching games reserves the old seat.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — On two PCs, use Pause Exit → Server Browser REJOIN. Verify only the saved battle appears as a Rejoin entry and that cancelling a reconnect retains recovery.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — On two PCs, use Pause Exit → Join by IP with the saved KCP endpoint. Verify the original seat is restored.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — Use Pause Exit, quit the game, then launch and choose Rejoin previous match.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — Use Pause Exit and wait past 300 s. Verify host AI takes over and the client can no longer rejoin. Repeat with disconnected Leave to verify it clears the local record and AI takes over on expiry.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — Pause Exit → join another Steam lobby → Leave it (or Back while connecting). Verify Multiplayer still offers Reconnect to the saved battle.
- [ ] [[Done/Features/Rejoin_After_Battle_Exit_Plan]] — Run the updated GameFlow, Networking and Ui fixtures (incl. new `ServerBrowserRecoveryTests`) when explicitly authorized.

- [ ] [[Done/Bugs/Multiplayer_Reconnect_Flow]] — Two-PC Steam/KCP loss and forced-process crash → restart/rejoin; measure startup/loading, near-expiry acceptance, repeated crash deadline, AI takeover, kick/Leave and host exit.
- [ ] [[Done/Bugs/Multiplayer_Reconnect_Flow]] — Verify no duplicate units and restored ownership/health/orders, credits/production/reserve/research/cooldowns and Survival before input; inspect recovery/loading UI on both PCs.
- [ ] [[Done/Bugs/Multiplayer_Reconnect_Flow]] — Run Networking, Ui and GameFlow tests when explicitly authorized; authored/updated fixtures were compiled but not executed.
- [ ] [[Done/Bugs/Multiplayer_Reconnect_Flow]] — Capture matching host/client Steam Join Game logs to establish historical server-disconnect cause; client evidence proves lobby/transport success only.

### Ship Rotation Snaps and Tangent Failures

- User confirmed working rotation on 2026-11-15. Remaining battle code-2 failure is captured and owned by [[Done/Bugs/Ship_Tangent_Arc_Repair_Loop]].
- [ ] [[Done/Bugs/Ship_Rotation_Snaps_And_Tangent_Failures]] — Verify online clients display smooth host-driven heading and route changes.

- [ ] [[Done/Bugs/Ship_Tangent_Arc_Repair_Loop]] + [[Done/Bugs/Ship_Tangent_Line_Repair_Loop]] — Repeat the affected battle/reroute scenario; verify no tangent-repair error or visible regression. Exact captured input and smaller loop cases pass automated tests; live battle replay remains pending.

## Multiselect Attack Overlap

- [ ] [[Done/Bugs/Multiselect_Attack_Overlap]] — Verify passing ships do not displace holding ships, stopped overlaps still settle, and ordinary/artillery re-closing stays on the current side after target drift.

- [ ] [[Done/Bugs/Multiselect_Attack_Overlap]] — Run authorized Navigation/BattleInput/EnemyAi regressions; Combat/Ships behavior review covered by navigation integration fixtures. Tests were not run under the checkout override.
- [ ] [[Done/Bugs/Multiselect_Attack_Overlap]] — Check same-target Attack/Attack Move, Guard/Hunt, minimum-range/hardpoint/artillery holds, dense/mixed-height fleets, map edges/full firing space, ion disable, Stop/new orders, target replacement and multiplayer authority in-game.
- [ ] [[Done/Bugs/Multiselect_Attack_Overlap]] — Compare matched Editor/Development Player overlap duration, CPU p95/p99, GC/frame, queue age, saturation, correction and route-build counters. Source preserves CPU limits; no measured CPU neutrality or improvement yet.

## Structure Status Icons

- [ ] [[Done/Features/Structure_Status_Icons_Plan]] — Check Captured and Destroyed in the HUD and minimap, and Build available over the site. Capture confirmation lasts 2 s of displayed world time, then locally owned empty sites show Build available over the site. Successful purchase dismisses it, failed purchase keeps it, and completion leaves the overlay clear. Check online/save-load restores only empty-site reminders and capture/destruction minimap fades remain independent.

- [ ] [[Done/Features/Structure_Status_Icons_Plan]] — Run authored Capture/Ui/Networking regressions and affected BattleInput/EnemyAi/GameFlow interface fixtures when explicitly authorized; local test execution was disabled. Final purchase/fade correction compiled successfully on 2026-11-16.
- [ ] [[Done/Features/Structure_Status_Icons_Plan]] — Profile matched idle/many-reminder/camera-pan/destruction workloads; record source/active/visible counts, CPU and allocations. Zero-allocation target and CPU budget are unmeasured.

## Fog Hull Visibility

- [ ] [[Done/Bugs/Fog_Hull_Visibility_Review_Fixes]] — Run non-explicit Vision, Combat, Networking, Ui, Squadrons, Abilities, BattleInput, Capture and Audio checks on explicit request; tests not run.
- [ ] [[Done/Bugs/Fog_Hull_Visibility_Review_Fixes]] — Matched large-battle CPU capture versus `d19023145`: vision, acquisition, 10 Hz fog checks and reveal fan-out; remove `profiler.data` after analysis. No performance claim yet.
- [ ] [[Done/Bugs/Fog_Hull_Visibility_Review_Fixes]] — In-game hull edge/rotation, squadron/structure/ship firing reveal, marker/audio visibility, five-second expiry and cloak precedence.
- [ ] [[Done/Bugs/Fog_Hull_Visibility_Review_Fixes]] — Online reveal/hide parity and reconnect mid-reveal baseline.

## Structure Status Icon Projection

- [ ] [[Done/Bugs/Structure_Status_Icons_Projection]] — Check local mining/Battle Asteroid and satellite destruction under fog, camera pan/rotation/zoom and offscreen return; icons stay above structures and receive their full displayed lifetime.

## Live battle CPU and pool demand
- [ ] Matched Development Player timing and acceptable admission-latency budget → [[Done/Optimization/Live_Battle_CPU_And_Pool_Demand]]. Heavy-capital and boosted-overflow live verification completed: moving target cost ~4.41 ms/frame before the 2026-11-16 scan fix (re-measure Praetor vs moving Executor; run `Navigation` + `Combat`); stressed requests waited up to 6.24 s.

## Squad Control Groups

- [ ] [[Done/Features/Squad_Control_Groups_Plan]] — Windows battle play: hold Ctrl, assign/recall all ten top-row slots (0 last), numpad exclusion, overlapping groups, death/pool reuse, Replace/Cancel/Escape, Alt-tab popup retention, fresh inputs and notifier feedback; save/load preserves slots.
- [ ] [[Done/Features/Squad_Control_Groups_Plan]] — Mac hardware: left/right Ctrl and optional Command setting, US/non-US top-row layouts, OS shortcut interactions; no numpad activation.
- [ ] [[Done/Features/Squad_Control_Groups_Plan]] — Visual acceptance: top-center squad preview and notification spacing, heroes/type icon priority, counts, and selected/empty/highlight contrast across battle backgrounds and supported resolutions.
- [ ] [[Done/Features/Squad_Control_Groups_Plan]] — Run authored/affected BattleInput, Ui, GameFlow and Core coverage plus changed Abilities/Economy mocks only when explicitly authorized; tests were not run under local override.

## Ship Class Rework

- [ ] [[Done/Refactoring/Ship_Class_Rework_Plan]] — In-game skirmish, balance, UI class frames/sorting, siege multipliers (`BattleCruiser` 1.2× / `Dreadnought` 1.5×), and 600 m `NebulonB` visual/collision/shield/wreck verification.
- [ ] [[Done/Refactoring/Ship_Class_Rework_Plan]] — Run affected EditMode test suites (`Abilities`, `Balance`, `BattleInput`, `Simulation`, `Combat`, `EnemyAi`, `Navigation`, `Ui`, `Ships`) when explicitly authorized; 13 test files were updated cleanly and not executed under local override.

## Interdictor Gravity Well

- [ ] [[Done/Features/Interdictor_Gravity_Well_Anchor_And_Vfx_Plan]] — In-game: Interdictor brakes and stays put while the well is active (move orders ignored, turns and fires, `Retreat` cancels the well); ring visible only while selected; refused move shows "Gravity well active" card; Retreat right after activation still leaves; 4 dorsal dome VFX (look in battle, pause off-screen, fog hiding on enemy ships). Also run `Abilities`, `Ships` and `Navigation` EditMode suites when explicitly authorized (not run under local override).
- [ ] [[Done/Features/Interdictor_Gravity_Well_Plan]] — Run affected EditMode test suites (`Abilities`, `Balance`, `GameFlow`, `Squadrons`, `Economy`, `Navigation`) when explicitly authorized; tests were updated and compiled cleanly (0 errors) but not executed under local override.

## Battle Hitch and GC Attribution Instruments

- [ ] [[TODOs/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]] — Run authored `Core` and Python parser regressions when requested; all execution deferred by user.
- [ ] [[TODOs/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]] — Replay `fps-mid-game`; ≥80% of >200 ms duration named; decide H1/H2 and correlate H3 with native callers.
- [ ] [[TODOs/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]] — ≥5 min armed playtest produces a >200 ms native snapshot/report; verify ≥80% allocation coverage and top10 callers for mid/late saves.
- [ ] [[TODOs/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]] — Capture 5/30 min memory pair, inspect type/direct-owner growth and retained references; decide leak/cache/pool from evidence.
- [ ] [[TODOs/Tooling/Battle_Hitch_And_GC_Attribution_Instruments]] — Matched Editor/Development Player 3 min replays for mid/late saves; verify cleanup, complete output folder/report and record a new baseline in Live Battle CPU Sweep.
- [ ] [[TODOs/Features/Squadron_Icon_Clustering_Rebuild]] — In-game: icons never stack, matrix grids, smooth peel/merge, no drag while panning/fighting, grids stay off ships (ships clickable), leader lines, zoom. Then Development Player capture (150 squadrons + 80 ships): `Battle.Markers.*`, `Canvas.BuildBatch`, GC. Run `Squadrons`, `BattleInput`, `Ships` tests on request.

## Selection

- [ ] [[Done/Features/Shift_Click_Add_To_Selection]] — In battle: select ship A, Shift + click ship B → both selected, group UI, one move order moves both; Shift + click B again → only A; Shift + click empty space / enemy keeps selection; plain click replaces. Note whether the select cue on toggle-off feels wrong.
- [ ] [[Done/Features/Shift_Click_Add_To_Selection]] — Settings: `Add To Selection` slot shows `Shift`; rebind → new key + click adds, Shift + click replaces; Reset restores Shift; override persists after restart.

## Squadron Faction Balance

- [ ] [[Done/Bugs/Squadron_Faction_Balance]] — In battle: X-Wing vs TIE Fighter, TIE Fighter vs Vulture, A-Wing vs TIE Fighter, Rebel station launches X-Wings (2 active, 6 reserve). Run `Balance`, `Squadrons`, `Combat`, `Ships` when explicitly authorized.

## Tractor Beam Lower-Class Targeting

- [ ] [[Done/Features/Tractor_Beam_Lower_Class_Targeting]] — In battle: ISD II beams a heavy cruiser; refuses another capital and a fighter squadron; ability and roster tooltips read correctly.

## Unity Pipeline 1.0

- [ ] [[Done/Tooling/Unity_Pipeline_1_0_Update]] — Confirm the next Jenkins player build of committed `main` succeeds on Pipeline `1.0.0`.

## Enemy AI Opening and Task Groups

- [ ] Round-7 playtest (Rebellion AI vs Empire, 30,000 start; then CIS vs Republic) → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Round 7 playtest gates (user; Rebellion vs Empire, 30,000 start)]]: no level-2 ships from level 4, no platform above 2,000 credits below 10 ships, ≥ 6 ships within 30 s of level 2, capacity < 100 with < 15 ships alive, median ship life ≥ 90 s. Round-6 playtest played 17:44 (ship selection failed → round 7). Tests not run.
- [ ] Economy playtest → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Economy normalization (2026-11-21; user decision, implemented, commit `c2b80479f`; `Economy` 137, `Core` 169, `GameFlow` 220 green)]]: one 2v2 and one Large-map game; income per player for the same map share should match Small 1v1 (about 634 per second at half the map, level 5, full research).
- [ ] Attack-move battle check (Editor battle) → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Open work (all findings; 2026-11-21)]] W14: AI station defenders settle against a moving attacker; AI capture forces do not take heavy losses on the approach.
- [ ] W1–W12 playtest → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Open work — implementation notes (2026-11-21)]]: second station bought early and upgraded to the main level; no miners/platforms around a dead base; wings stay in fights near their post; capture wings cheapest fighter (+ interceptor with spare money); AI uses S-Foils, Replenish Wingmen, Lucky Shot, Gravity Well; Survival ring pays 400 per defender.
- [ ] Round 9 playtest (Medium 1v1, Empire AI vs Rebellion) → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Round 9 — battle 14:32 (2026-11-22; implemented 2026-11-22, not played)]] W45: no AI ship idle > 30 s while its group attacks or captures; no `localAdvantage` 100 within 10 s of a group ship death; `AssaultBase` held vs 0 hostile ships at advantage ≥ 2; no `saving` row for a unit that does not fit, bank > 50,000 < 10 min; critical buys never the two lowest value-per-slot ships with a full bank; `armadaResponse` ≤ 30 s after station pressure; no capital landing at a contested relay; Interdictor fires near stations / relays / retreats only. Tests not run.
- [ ] W15–W22 battle check → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Open work — implementation notes (W15–W22, 2026-11-21)]]: AI no longer engages into regenerating shields / repaired fleets / hostile platforms it cannot beat; no same-type queue stacking (capacity not held by waiting builds); repair satellites built beside armed ones and shot first when hostile; idle bombers fight with the ships (`raidSkip` bombers not parked); `ai_orders.detail` shows `retarget=` reasons. Tests `EnemyAi` 613/613, `Combat` 329/329 green (2026-11-22).
- [ ] Round 10 playtest (2v2 vs 2 AI, you as host so the AI log is written) → [[TODOs/Bugs/Enemy_AI_Opening_And_Task_Groups#Round 10 — online 2v2 20:12 (2026-11-22; implemented 2026-11-23 except W58, W60)]] W60: AI no longer retreats from even fights or leaves a zone with hostiles alive; captured zone keeps its force ≥ 60 s and a garrison while you are near; bank > 50,000 with free capacity ≥ 8 for < 2 min; ≤ 1 state change per group per 8 s outside urgent switches; big platforms ≥ 150 s from payment to armed, none in a zone held < 60 s; `idleUnderFire` 0 rows > 10 s (dev build). Tests run green (except 2 BattleInput failures from `d33d5b46b`).
- [ ] W8 ship role review → same plan: check the proposed `roles` on the 77 ShipData assets before the AI reads them.
- [ ] Compact AI log → [[TODOs/Features/Gameplay_Analytics]]: next release build compiles; the host of one online game finds a `*_ai.csv` in `BattleTelemetry`.


## Empire Hero Capitals

- [ ] [[Done/Features/Chimaera_Hero_Capital]] — In battle: build Chimaera (hero, limit 1), check rust + gold reads on the hull, the emblem from below, the 2 new dual medium turbolasers fire, the wreck stays rusty.
- [ ] [[Done/Features/Devastator_Hero_Capital]] — In battle: build Devastator alongside Vader's TIE Advanced; check the 6 heavy dual turbolasers + 2 heavy dual ion mains and the 2 new medium ions fire.
- [ ] [[Done/Features/Chimaera_Hero_Capital]] — Run `Ships` (`EmpireHeroCapitalTests`) and `Combat` (`WeaponLoadoutBakeTests`) when authorized; compile passed, tests not run.

## Ship Engine Plumes

- [ ] [[Done/Features/Ship_Engine_Plume_Rollout_Plan]] — In battle: check engine trails on CR70, ISD I/II, Tector and Executor (grow from 0 when departing, bend in turns, fade on stop, none during hyperspace, hidden by fog; capital turn-in-place linger acceptable?); spot-check other ships for trails starting off the nozzle.
- [ ] [[Done/Features/Ship_Engine_Plume_Rollout_Plan]] — Battle capture with many ships moving: plume draw calls and transparent overdraw.
- [ ] [[Done/Features/Ship_Engine_Plume_Rollout_Plan]] — Run `Ships` (`ShipEnginePlumePrefabTests`) and `Navigation` (`ShipNavigationServiceTests`) when authorized; compile passed, tests not run.

## Map Satellite Pads

- [ ] [[Done/Features/Map_Satellite_Pad_Expansion]] — Confirm decisions: old Skirmish (Full) saves no longer load; free pads only inside lane corridors.
- [ ] [[Done/Features/Map_Satellite_Pad_Expansion]] — In a fresh Skirmish: check 4 home pads per station, 4 pads per outer relay, 8 main pads, free pads on routes (2/6/10); AI satellite spending/squadron spread; performance on Large 8-player.
- [ ] [[Done/Features/Map_Satellite_Pad_Expansion]] — Run `Map`, `MapGeneration` (explicit), `GameFlow`, `EnemyAi`, `Capture` when authorized; compile passed, tests not run.

## Map Outer Zones

- [ ] [[Done/Features/Map_Outer_Zone_Reduction]] — In a fresh Skirmish (Medium and Large, 2 and 6–8 players): outer relays spread far apart and far from the main relay; AI still captures and defends them; economy feel with fewer neutral mines.
- [ ] [[Done/Features/Map_Outer_Zone_Reduction]] — Run `Map`, `MapGeneration` (explicit), `GameFlow`, `EnemyAi` when authorized; compile passed, 630 scripted layouts passed, tests not run.

## Viscount Import

- [ ] [[Done/Features/Viscount_Import]] — In battle (Rebellion): build Viscount Prototype and Viscount Dreadnought; check hull scale vs Home One / Executor, textures and lights, engine plumes on the nozzles, team colours, selection ring, shield fit, hangar launches, wreck.
- [ ] [[Done/Features/Viscount_Import]] — Balance check: Dreadnought vs Executor 1v1 and Prototype vs ISD II / Praetor; review provisional price, build time, capacity and speed.
- [ ] [[Done/Features/Viscount_Import]] — Run `Ships`, `Health`, `Combat`, `Audio`, `Balance` when authorized; compile passed, tests not run.
