# Invincible Import

## Goal

- Separate Separatist Providence-class dreadnought, `ShipType.Invincible = 109`.
- User values: Providence size × `39/14`; hull/shields × `2`; weapon count × `2`, utility systems unchanged.

## Important Values

- Hull `10000`; shields `6000`; `28` weapons + engine/shield-generator/hangar = `31` destroyable targets, IDs `0..30`.
- Weapons: `12` `DualHeavyTurboLaser`, `4` `MediumTurboLaser`, `8` `DualMediumTurboLaser`, `4` `HeavyIonCannon`; preserve donor arcs and hardpoint health.
- Size `59.28171 × 109.37660 × 318.23566`; Providence length `114.23844`. Bow `+Z`, up `+Y`, centered visual, prefab root scale `1`.
- Bank `±10°`; Y `−54.68830..54.68830`; navigation radius `162`. Own collider, shield, ion-field bounds, selection and clear hangar launch point.
- Inherit Providence's remaining movement/regeneration/range/abilities/hangar values and price `6000`, build `60 s`, tech `4`, population `5`, limit `20`, non-hero roster flag.

## Implementation

- RaW Workshop `1129810972`; `Units_hero_cis_trench.xml` → `SeV_Invincible.ALO`. Avoid unrelated `ReV_invincible.ALO` CSA cruiser and hero `GUI_Model_Name` pointing to standard Providence.
- Preserve source ALO/DDS/XML hashes. Full RaW credits copied beside staged source; individual Invincible model authorship is not separately established by the available credits.
- Isolated Blender `3.6.23`; `85` bones, `7` transport meshes, `21421` triangles. Visible hull `9810`, additive lights `836` triangles.
- Source-alpha team mask `4.52621%`; normal green flip; eight live/wreck ownership colors rendered, blue/green inspected.
- Raw source parent indices/world matrices independently verified; restore only verified identity Root. Repeated damage-bone names require source-index → unique-name mapping.
- All FBX corners verified; all `64263` Unity corners verified. Max Unity position/bone errors `0.000008021` / `0.000005812` units; visible UV error `0`.
- Saved reference/data/count/size/bounds/shield/lifecycle/faction/icon checks pass; zero Console errors. Original Providence unchanged; MainMenuScene clean.

## Edge Cases

- Unity remaps `83` unused Shadow UV corners by ≤ `0.000994811`. Upstream shadow welding and hidden weapon-flash helpers stay transport-only; runtime geometry contains intended hull/lights.
- Donor right full-arc turbolaser is positioned left. Invincible mirrors only its placement target; hardpoint mapping records donor position, chosen side and authored socket.
- No source ALA/shader animation recreation, automated Unity tests, Play Mode combat or packed content build. Runtime balance remains unverified.

## Files

- `Tools/Blender/Invincible/README.md` — rebuild commands and source/geometry evidence.
- `Assets/Art/Models/SeparatistShips/Invincible/Invincible.fbx`.
- `Assets/Prefabs/Models/Ships/Separatist/Invincible.prefab`, `InvincibleShipView.prefab`.
- `Assets/Settings/Data/Ship/Separatist/InvincibleShipData.asset`.
- `Assets/Prefabs/Models/Wrecks/InvincibleWreckView.prefab`, `Assets/Prefabs/Ui/Reinforcement/InvincibleReinforcementView.prefab`.
- [[Done/Features/Invincible_Import]] — completed import scope and verification.
- [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] — shared workflow.
