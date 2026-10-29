---
type: integration-record
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# Dauntless Heavy Cruiser Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.
> Plan: none (imported before plan tracking). Open user acceptance → [[TODOs/User_Acceptance_Queue]].

## Decision

- Imported Workshop `1770851727`, `Neutral_Dauntless_Cruiser.ALO`; source inheritance `Dauntless_Cruiser_R → Dauntless_Cruiser_Upkeep → Dauntless_Cruiser_Template → Capital_Base → Vessel_Base → SFX_Base`.
- Balance source: [[GameDesign/Proposals/Rebel units#7. Dauntless Heavy Cruiser]]. Use project proposal values, not Remake XML balance.
- Source `Model_To_Attach` values are empty; hull includes the required weapon art. Separate `w_turbolaser_r4c_dauntless.ALO` is not added.
- Damaged `Neutral_Dauntless_Cruiser_D.ALO` references absent `destruction_for_dcs.dds`. Wreck uses the verified live hull and existing project wreck shader.
- User-approved fighters, 2026-10-12: X-Wing + Y-Wing; one active and one replacement each. Serialized `reserve=2` per type includes its initial launch.

## Important Values

- Hull `10,000`; shields `5,500`; speed `17 game units/s`; price `5,600 credits`; build `60 s`; project combat class `Cruiser (8)`.
- Weapons: `8 × Heavy Turbolaser (2)`, `4 × Medium Dual Turbolaser (5)`, `4 × Heavy Ion Cannon (11)`, `2 × Light Point Defense Laser (14)`.
- Subsystems: `2 engines + 1 shield generator + 1 hangar + 1 sensor array`; all `23` hardpoints targetable.
- Ship enum `Dauntless=310`; ability `DauntlessSensorJamming=39`; hardpoint type `SensorArray=10`. Existing IDs preserved.
- Sensor Jamming: enemy accuracy `×0.8`, active `8 s`, recovery `40 s`. Refreshes nearby hostile entities through facades; leaving range, sensor destruction, expiry or caster loss → remove debuff. Multiple jammers do not stack; one ending leaves other active jammers intact.
- Provisional choices: jamming radius `500 units`; level `3`, population `3`, max count `3`; visual length `160 units`; navigation radius `95`; bank `±5°`; launch delay/interval `4/8 s`.
- Bounds `32.695 × 42.564 × 160`; banked Y range `−21.28209 .. 21.28209`; launch point `8 units` below visible geometry.
- Provisional regeneration: shields `36.6667` every `3 s` (`12.2222/s`), proportional to existing donor ships. Hardpoint health: weapons `750`, engines `1,500`, shield `1,000`, hangar/sensor `600`, point defense `500`.

## Implementation

- Blender `3.6.23`, MCP protocol `13`, safe mode; separate source/validation scenes. Imported without ALA animations.
- Binary material definitions reconstructed before export; `13` meshes, `102` bones, `29,040` converted triangles including retained source helpers. ALAMO welds shadow-helper geometry; visible material triangle counts match original binary.
- FBX round trip: geometry error `0.00005344`, bone error `0.00004983` source units; UVs and bone parents verified.
- Opaque surfaces use `EmpireAtWar/Ship Lit`; normals imported linear with green-channel flip. Engine/hangar lights and shield-effect surfaces use static additive materials. Original wave/distortion textures preserved; EaW animated shader behavior is not recreated.
- Hull red paint enables HSV team livery; neutral hangar markings remain unchanged. Gameplay references cover team color, fog, health, weapons, shield, collision, ion bounds, hangar and selection.
- Weapon IDs `0..17` map to `HP_01_FP_01..HP_18_FP_01` in listed weapon order. Systems `18..21` map to `HP_Engine1`, `HP_Engine2`, `HP_Shield`, `HP_Hangar`. Sensor `22` uses a derived dorsal position because no sensor socket exists.
- Game prefabs strip authored hidden source collision/shadow/shield meshes; FBX and editable blend preserve them. Placement drops the disabled 2-triangle hangar-shield helper; gameplay shield stays.
- Registered ship/data maps, existing Addressables View/Data groups, Rebellion roster, matchups, HUD/tooltip icons, placement prefab, ability/audio and sensor overlay. Existing weapon-family audio reused.

## Files

- Builders/evidence: `Tools/Blender/Dauntless/`; editable package: `output/remake-rebel-units/Dauntless-Converted/`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/Dauntless/`.
- Prefabs: `Assets/Prefabs/Models/Ships/Dauntless.prefab`, `DauntlessShipView.prefab`; `Assets/Prefabs/Models/Wrecks/DauntlessWreckView.prefab`; `Assets/Prefabs/Ui/Reinforcement/DauntlessReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/DauntlessShipData.asset`, `Ship/Wreck/DauntlessWreckData.asset`; icon `Assets/Art/Textures/Ui/Icons/ShipIcon/DauntlessIcon.png`.
- Sensor implementation: `Assets/Scripts/Services/ShipAbilities/Abilities/SensorJamming/`; shared accuracy multiplier in `CombatModifiers` consumed by `WeaponComponent.RollHit`.

## Rules

- Rebuild: `Prepare.py → Convert.py → Stage.py → GenerateBuilders.py`; Unity `BuildArt.cs → BuildGameplay.cs → Persist.cs → Render.cs → Register.cs → StripHelpers.cs → Inspect.cs`; finish `Package.py` with Pillow.
- Unity command: `unity command run_script --file Tools/Blender/Dauntless/<File>.cs --timeout_ms 60000 --timeout 70 --json`; inspect nested `result.success`.
- No automated Unity tests or Play Mode run, per [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]]. Preserve other concurrent imports and unrelated changes.

## Goal

- Saved-asset verification on 2026-10-12: `3,694` checks passed; Unity bone displacement `≤0.000001508 units`; no broken references/missing scripts in four prefabs.
- `18` source hashes unchanged; `13` decoded textures pixel-identical; transparent `512×512` icon bounds `(98,158)..(425,334)`; eight live/wreck palette renders distinct.
- Latest Unity check: imports/compilation idle, `scriptCompilationFailed=false`. Historical Console errors belonged to concurrent RapidDeployment work; no Dauntless import/serialization errors found.

## TODO

- In-battle acceptance unrun: firing arcs, initial/replacement launches, subsystem destruction, Sensor Jamming accuracy/range/expiry, fog/selection, reinforcement and wreck lifecycle.
- Source ALA death animation and animated EaW material effects are not ported.
