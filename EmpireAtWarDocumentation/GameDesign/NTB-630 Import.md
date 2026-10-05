---
type: reference
updated: 2026-10-05
tags:
  - blender
  - unity
  - squadron
  - alo
---
# NTB-630 Naval Bomber Import

## Decision
- Source: RaW 1.2.1 `Data/Art/Models/ReV_ntb630.ALO`; user-requested Republic early-game anti-capital bomber.
- Four craft; per craft hull 60, shields 30, refresh 3; Bomber armor. Cost 550 credits, build 8 s, population 1, level 1.
- User build 8 s overrides local `Units_Space_republic_ntb_630_fighter.xml` multiplayer 17 s. Local XML confirms four craft, hull/shields/refresh and cost/population.
- Ion Shot excluded by user; `Abilities` remains empty. No ability, catalog, projectile or carrier changes.
- Separate factory-startup background Blender 3.6.23 avoids the other agent's live Blender instance.

## Implementation
- [[Architecture/ALO_MODEL_IMPORT_GUIDE]] → source audit → lossless PNG staging → packed Blender/FBX → registered Unity assets.
- Eight meshes / 5,097 triangles / fourteen bones retained; restore only verified identity `Root` omitted by ALAMO. Three visible meshes: `main`, `turret`, `glowy`; five authored helpers remain disabled.
- Two existing `FighterLaser` barrel mounts use `MuzzleA_00/01`; one `FighterProtonTorpedo` launcher uses midpoint `MuzzleB_00/01`. One trail at `Pe_TieBomberEngine`; twelve unique weapon IDs `0..11`.
- Torpedo behavior reuses existing one-launch-per-craft/pass component; no new combat logic. Current shared profile: damage 90, one shot, reload 8 s, range 55 units. Laser: damage 5, two shots, reload 0.9 s, range 35 units. These are project tuning.
- Hull source alpha → separate linear team mask; all eight owned palettes rendered, blue/green visually inspected. Neutral plating preserved.
- Registered `SquadronType.NTB630 = 4`, Republic roster, own data/view mapping, existing View/Data Addressables groups, placement, HUD/tooltip icons and opponent matchups.

## Important Values
- FBX import scale 0.02; uniform resize factor 6.372638 (model `localScale` 637.2638); bow +Z, up +Y, prefab roots scale 1.
- Centered craft bounds `2.650344 × 0.853184 × 4` project units; gameplay/placement bounds `14.650345 × 1.813184 × 10.4`, center `(-2, 0, -3.2)`.
- Measured formation radius 12.52512 → navigation 13; member radius 2.37352 → collider 2.4; selection diameter 26.
- Provisional flight: cruise/combat 24/27 units/s, acceleration 18 units/s², turn 65°/s, bank 35°, spacing 4, height 11, loiter radius 20, limit 10.
- Refresh 3 points each 1 s; no passive hull repair.
- Blender max bone/geometry displacement `0.000002876 / 0.000005723` source units; Unity bone error ≤ `0.0000001062` units, no parent mismatches.
- Original ALO and four DDS hashes unchanged; DDS→PNG RGBA pixels identical. Unity retains UVs and all 5,097 triangles.

## Files
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RepublicShips/NTB630/`.
- Prefabs: `Assets/Prefabs/Models/Squadrons/NTB630.prefab`, `NTB630SquadronView.prefab`; `Assets/Prefabs/Ui/Reinforcement/NTB630ReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Squadron/NTB630SquadronData.asset`; matchups `Assets/Settings/Data/Tooltip/Matchups/NTB630SquadronMatchups.asset`.
- Transparent 512 × 512 icon/silhouette: `Assets/Art/Textures/Ui/Icons/SquadronIcon/NTB630{Icon,Silhouette}.png`.
- Scripts: `Tools/Blender/prepare_ntb630_textures.py`, `export_ntb630.py`; Unity integration/render/inspection scripts included in conversion pack.
- Deliverable: sibling `ReV_ntb630-Converted/` holds packed `.blend`, FBX, PNGs, source/conversion/Unity reports, eight palettes, scripts and original RaW credits.

## Edge Cases
- Missing originals `ReV_ntb2.dds` and `ReV_ntb2_gloss.dds` → explicit neutral-metal turret material. Geometry/UVs preserved for later restoration; no substitute texture fabricated.
- Hull gloss source retained; EaW animated shader behavior and ALA animations not recreated.
- Saved reference checks show no missing scripts, broken references or donor-model dependencies. Unity import/serialization/Console checks have no new errors.
- No automated tests or Play Mode run; static checks do not verify combat, shields, fog, placement, death or balance.
- Credits: original mesh Howard Day; low-poly rebuild Major Payne; textures Howard Day/Major Payne/Bryant; rigging z3r0x. Full credits copied from supplied RaW package.

## TODO
- [[TODOs/Features/NTB630_Import]]: restore original turret textures if supplied; in-game acceptance and provisional balance review.
