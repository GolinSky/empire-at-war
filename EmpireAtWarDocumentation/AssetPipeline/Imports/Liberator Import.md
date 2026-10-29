---
type: integration-record
updated: 2026-10-12
tags:
  - rebellion
  - alo
  - ship-import
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# Liberator Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.
> Plan: none — historical provenance only; no tracked open work (2026-10-29 review).

## Rules

- Balance: [[GameDesign/Proposals/Rebel units]] → Liberator Cruiser. Art: REMAKE 4.0 Workshop `1770851727` → `Rebel_Liberator.ALO` plus fifteen XML-resolved attachments.
- Source texture folder: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data/Art/Textures/`; supplied `Texturesc` folder does not exist.
- Original ALO/DDS/XML files remain unchanged; SHA-256 verification passed.

## Implementation

- Rebellion `ShipType.Liberator = 321`; `ShipAbilityId.LiberatorRapidDeployment = 51`. Existing enum IDs remain unchanged.
- Hull `4,800`; shields `4,200`; speed `25`; cost `4,200`; build time `48 s`.
- Weapons: `4× HeavyTurboLaser`, `2× DualMediumTurboLaser`, `4× MediumIonCannon`, `2× PointDefense`.
- `18` targetable hardpoints → `12` weapons, `2` engines, `1` shield generator, `2` hangars, `1` tractor. Tractor uses the existing tractor-beam ability.
- X-Wing only → `3` active + `3` replacements. Port bay: `4` total launches / `2` active; starboard: `2` total launches / `1` active.
- Rapid Deployment → up to `2` available reserve squadrons; cooldown `45 s`; preserves active limits, destroyed-bay shutdown and shared one-launch-per-frame budget.
- Source DTL `01–04` → heavy weapons; `05–06` → dual medium weapons; `07–08` retain source art. DIC → ions; DLC → point defense. Muzzles use attached model bones.
- Engine hardpoints derive symmetric offsets from `HP_Engines`; launch hardpoints use `HP_Spawn_00/01`. Mapping evidence: `Tools/Blender/Liberator/README.md` and archived `HardpointMapping.json`.

## Important Values

- Gameplay bounds `102.137 × 24.481 × 160` project units; root scale `1`; navigation radius `95`; banking `±5°`; vertical range `-16.545 .. 12.267`.
- Launch clearance `8` units below visible hull; initial delay `4 s`; launch interval `8 s`.
- Provisional project choices: level `3`, population `3`, maximum `3`; range `600`; shield regeneration `28` per `3 s`; hardpoint health weapon/engine/shield/hangar/tractor `400/600/600/700/350`.

## Edge Cases

- Unity removes `16` verified zero-area triangles from `rebel_armor_neutral_2`; all other source mesh triangle counts match.
- Zero-duration phased abilities finish through `IsComplete`; this lets Rapid Deployment wait for available frame slots. Timed phases keep their duration limit.
- A full active complement, exhausted reserve or destroyed hangars → no deployment.
- Decorative source X/Y-Wings remain in the hull art; launched squadrons are X-Wing only.

## Files

- `Tools/Blender/Liberator/README.md` → source mapping, provisional balance, rebuild commands and verification.
- `Assets/Prefabs/Models/Ships/LiberatorShipView.prefab` → gameplay; `Liberator.prefab` → source visual.
- `Assets/Settings/Data/Ship/LiberatorShipData.asset`; `Ship/Wreck/LiberatorWreckData.asset`.
- `Assets/Prefabs/Ui/Reinforcement/LiberatorReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/LiberatorWreckView.prefab`.
- `Assets/Art/Textures/Ui/Icons/ShipIcon/LiberatorIcon.png`; associated silhouette and matchup asset.
- `output/remake-rebel-units/Liberator-Converted/` → sixteen editable packed `.blend` files, source audit, conversions, mapping, verification and previews.

## Decision

- Verified: sixteen FBX round trips; `115` Unity source meshes / `80,101` triangles; maximum Unity attachment error `0.0000021` units; saved references/material slots, hardpoint counts, hangar model behavior and all registrations.
- Visually inspected: icon, contrasting team palettes, placement and wreck. Eight palettes rendered for both ship and wreck.
- Unity import/compilation completed; Console contained no errors. Automated Unity tests and Play Mode were not run.
