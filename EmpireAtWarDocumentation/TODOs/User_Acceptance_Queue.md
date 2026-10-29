---
category: Documentation
status: in-progress
created: 2026-10-29
---
# User Acceptance Queue

## Rules

- One line per user-only gate (in-game, listening, visual, tests on request) of a closed plan; policy → root `AGENTS.md` Plan Lifecycle step 4.
- Passed → delete the line; when a plan has no lines left, set its `acceptance: passed`.
- Failed → reopen the plan or open a `Bugs` plan; replace the line with its link.
- Imported-unit balance values are provisional; balance review is part of each unit's gate.

## Imported Units

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

## Gameplay and Balance

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

## Main Relay Zone Map Rework

- [[Done/Features/Main_Relay_Zone_Map_Rework]] — In-game check all sizes, 2p/4p: home economy, relay rings/mesh, capture/build options, AI expansion, lane access.
- [[Done/Features/Main_Relay_Zone_Map_Rework]] — Run updated Map, MapGeneration, MapExhaustive, GameFlow, Capture and Core checks on explicit request; no tests run by agents.
- [[Done/Features/Main_Relay_Zone_Map_Rework]] — After gameplay acceptance, update GameDesign map/economy references.

## Note Flags

- [ ] [[Done/Documentation/Note_Flags_And_Durable_Guidance_Plan]] — Open [[Rules/NOTE_FLAGS]] in Obsidian and visually confirm all five native flag views and combined searches render; important finds both pilots, issue/broken reflect their actual tags, and unrelated topic tags are excluded.
