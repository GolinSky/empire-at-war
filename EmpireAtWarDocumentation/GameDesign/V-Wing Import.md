---
type: reference
updated: 2026-10-05
tags:
  - unity
  - squadron
  - republic
  - asset-pipeline
---
# V-Wing Import

## Goal

- Republic fighter/interceptor; `VWing = 6`, five craft. Local model supplied; ALO/Blender steps skipped.
- Plan: [[TODOs/Features/VWing_Import]]. Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]].

## Important Values

- User-supplied RaW values: per craft hull `80`, shields `25`, shield regen `3`; four laser cannons, damage `5`; no destructible weapon/system hardpoints.
- Squadron price `600`, build `15 s`, population `1`; configured exactly as supplied.
- RaW speed max/min `6.0/5.5`, turn `3.0`; retained as source reference, not Unity flight units.
- Project flight: cruise/combat `45/50 units/s`, acceleration `40 units/s²`, turn `150°/s`, bank `65°`, bank response `5`, height `11`, formation spacing `5`, navigation radius `15`.
- Project health: shields regenerate `3` every `1 s`; passive astromech repair `1 HP/s` per surviving craft, matching existing Y-Wing. Repair rate is provisional.
- Project economy: station tech `5`, max count `10`; provisional choices for late-game availability.
- Existing `FighterLaser = 15`: damage `5`, two shots/salvo, interval `0.08 s`, reload `0.9 s`, range `35`, projectile speed `140`, yaw `±15°`. Twenty weapons total; salvo/range/timing are project balance.

## Decision

- Hunt uses the existing squadron order; no separate ability button. Existing target selector favors strikecraft by distance score and can choose other enemies; this is broader than strict bomber-only hunting.
- Passive repair uses existing `SquadronHealthComponent.Tick → SquadronHealthModel.RepairHull`; destroyed craft stay destroyed.
- Five `FighterView` members are health targets. Guns are weapon emitters; no independently destroyable gun/system targets.
- Existing fighter explosion/death behavior reused; no capital-ship wreck asset. Wing pose is static.
- Other ships' hangars and previous approved substitutions remain outside this task.

## Implementation

- Supplied OBJ: `21,940` triangles, `24,175` imported vertices, one merged mesh, `12` material slots. Bow `+Z`, up `+Y`.
- Geometry-only prefab: centered at origin, root scale `(1,1,1)`, visible size `1.92147 × 2.98078 × 4.00000` project units.
- Per-fighter sphere radius `2.203`; measured vertex radius `2.153`. Four gun anchors derive from source barrel centers; two exhaust trails per craft.
- Source barrel centers: `x = ±2.1`, `y = 0.012511/-0.28749`, `z = 0.944731`, transformed through the saved model fit.
- Gameplay/placement formation bounds: `21.92147 × 4.18078 × 12`; bounds center `(0,0,-4)`; placement trigger and 30-unit selection diameter fitted.
- Opaque hull/wing/interior/astromech materials use `EmpireAtWar/Ship Lit`; cockpit and indicator/glow slots use URP Lit.
- Body/wing red paint: hue `0`, range `0.07`, min saturation `0.4`, livery strength `1`; neutral/droid materials do not recolor.
- OpenGL normal uses NormalMap import, no green flip. Derived packed texture: metallic red, smoothness alpha `1 − roughness`.
- Placement has five bound mesh renderers, twelve hologram slots each, kinematic Rigidbody, gravity off, fitted trigger; height `11`.

## Files

- Source: `Assets/Art/Models/RepublicShips/Vwing/V-wing.obj`, `V-wing.mtl`, `Textures/`; source bytes preserved.
- Art: `Assets/Art/Materials/Models/RepublicShips/VWing/`, `Assets/Art/Textures/Models/RepublicShips/VWing/VWing_MetallicSmoothness.png`.
- Visual/gameplay: `Assets/Prefabs/Models/Squadrons/VWing.prefab`, `VWingSquadronView.prefab`.
- Placement/data: `Assets/Prefabs/Ui/Reinforcement/VWingReinforcementView.prefab`, `Assets/Settings/Data/Squadron/VWingSquadronData.asset`.
- Icons: `Assets/Art/Textures/Ui/Icons/SquadronIcon/VWingIcon.png`, `VWingSilhouette.png`; matchup `Assets/Settings/Data/Tooltip/Matchups/VWingSquadronMatchups.asset`.
- Registrations: `SquadronType`, `RepublicFaction.squadrons`, `ShipUiData.squadronIconWrapper`, `TooltipIconData.icons`, `ReinforcementData.spawnSquadronWrapper`, `AssetMappingData`, existing Addressables `View`/`Data` groups.
- Local inspection/render scripts and reports: `Temp/VWingImport/`; temporary and ignored by Git.

## Rules

- Source provenance: user-supplied [RaW V-Wing wiki](https://republicatwar.wiki.gg/wiki/V-Wing) values; wiki fetch returned HTTP 403. Public XML/changelog were not independently verified in this task.
- Saved assets and explicit references inspected: five craft, twenty unique guns, ten trails, five health targets, all three icon consumers, own placement mapping and correct addresses; no donor dependencies/missing scripts/broken serialized references.
- Transparent `512 × 512` model icon/silhouette checked for visible alpha and clear border; eight owned-color renders produced, contrasting blue/green inspected.
- Nine original model/MTL/texture SHA-256 hashes unchanged. Unity import/compilation complete; no new errors after baseline; existing CameraService/empty-assembly warnings persisted.
- Open `MainMenuScene` remains dirty. No automated tests or Play Mode run.

## TODO

- Clean-skirmish acceptance: build/deploy, placement validity tint, Hunt, damage, repair, shields, weapon emission, fog/selection/team markers, death and teardown.
- Review provisional flight/scale/repair/weapon timing/tech/limit/matchup balance.
