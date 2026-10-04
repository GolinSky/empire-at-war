---
type: reference
updated: 2026-10-04
tags:
  - arc170
  - squadron
  - alo
  - asset-pipeline
---
# ARC-170 Import

## Goal

- Five Republic ARC-170 heavy / multirole fighters; limited torpedo support against ship hardpoints.
- Imported through [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; remaining work in [[TODOs/Features/ARC170_Import]].

## Important Values

| Value | Implemented |
| --- | ---: |
| `SquadronType.ARC170` | 2 |
| Craft | 5 |
| Hull / shields per craft | 105 / 35 |
| Shield regeneration | 5 per craft every 1 s |
| Tech / price / population | 5 / 375 / 1 |
| Build / maximum count | 8 s / 10 |
| Member size | 7.938759 × 1.267841 × 4 project units |
| Cruise / combat speed | 28 / 32 units/s |
| Acceleration / turn | 21 units/s² / 80°/s |
| Banking / formation spacing | 40° / 6 units |
| Height / navigation radius | 11 / 22 units |
| Measured formation radius | 18.68598 units |
| Member collision radius | 4.5 units |

## Rules

- User hull/regeneration values override local RaW 1.2.1 XML values 95 / 3. Local XML confirms five craft, shields 35, cost 375 and build 8 s.
- User torpedo limit overrides the local XML two-shot pulse. Keep existing bomber/global profiles unchanged.
- Size, flight tuning, navigation/selection volumes, maximum count and weapon balance are provisional project choices.
- Fighters have member health targets and explosion/removal death behavior; no separate capital-ship wreck.

## Implementation

- Per member: `MuzzleA_00/01` → two heavy lasers; `MuzzleC_00` → rear laser; `MuzzleB_00` → proton torpedo. Twenty unique weapon IDs.
- Heavy laser profile `21`: 8 damage, two shots, reload 1.5 s, range 40. Rear uses existing fighter profile `15`, rear-facing ±30° mount.
- Torpedo profile `22`: 90 damage, one shot, reload 8 s, range 55. `FighterTorpedoHardPoint` reads `IFighterAttackRunObserver`; at most one torpedo per member/pass, rearmed when the pilot starts a new approach. Cooldown may skip a pass.
- Republic roster, view/data lookup, existing Addressables groups, HUD/tooltip icons, matchup keys and five-member reinforcement preview resolve to ARC-170 assets.
- Hull alpha → separate linear `_TeamMaskMap`, strength 1. HSV recoloring disabled; neutral plating and astromech keep their source colors. All eight palettes rendered; blue/green inspected.
- Static source pose retained; deploy/undeploy ALA animations and original animated EaW effects are not converted. Gloss source retained; Unity uses the project's standard hull material tuning.

## Verification

- Blender source: 15 meshes, 4,691 triangles, 21 bones, six visible surfaces. Identity Root restored only after checking the binary source.
- Blender FBX reimport: 4,685 triangles. Six duplicate disabled-flash faces collapse; each helper's four unique position/UV triangles remain unchanged.
- Maximum Blender bone/corner displacement: 0.000005722 / 0.000006694 source units; UV tolerance 0.00001.
- Unity: all 15 meshes and 4,691 triangles retained, UVs throughout, 21 bones and matching parents; maximum bone displacement 0.0000001526915 project units.
- Visual/gameplay/placement roots scale 1; bow +Z/up +Y. Gameplay and placement bounds match; each collider encloses its member.
- Saved references, all member/gun bindings, registries and team-mask imports checked; no missing scripts, donor dependencies or broken references. Compiler/import/Console checks clean.
- Transparent 512 × 512 icon bounds: x 39..477, y 61..457; no crop. Four converted PNGs match DDS pixels exactly; original five ALO/DDS hashes unchanged.
- No automated tests or Play Mode run; dirty MainMenu scene preserved.

## Files

- `Assets/Prefabs/Models/Squadrons/ARC170.prefab`, `ARC170SquadronView.prefab`.
- `Assets/Prefabs/Ui/Reinforcement/ARC170ReinforcementView.prefab`.
- `Assets/Settings/Data/Squadron/ARC170SquadronData.asset`.
- FBX/materials/textures: `RepublicShips/ARC170` in the type-first art folders; model companion `SOURCE_CREDITS.txt` preserves the exact RaW attribution.
- Source `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_arc170.ALO`.
- Sibling `ReV_arc170-Converted/`: packed blend, FBX, PNGs, scripts, reports, previews, hashes and full source credits.
- `Tools/Blender/prepare_arc170_textures.py`, `export_arc170.py`, `README.md`.

## TODO

- Implement Lock S-Foils and Astromech Repair through explicit squadron support; no substitutes are advertised in the roster.
- Verify battle movement, rear turret targeting, torpedo pass/cooldown behavior, shields, fog, selection, deployment and death on a clean scene.
- Review provisional balance after runtime acceptance.
