---
type: reference
updated: 2026-10-04
tags:
  - alo
  - blender
  - unity
  - squadron
  - cis
---
# Droid Bomber Import

## Goal

- Early-game **Advanced Droid Bomber / Trade Federation Droid Bomber**; `SquadronType.DroidBomber = 101`.

## Important Values

- User-supplied identity: Haor Chall Engineering / Baktoid Armor Workshop; autonomous bomber, Trade Federation → CIS; no hyperdrive. Lore: 4 laser cannons + bomb bay.
- [Republic at War Droid Bomber](https://republicatwar.wiki.gg/wiki/Droid_Bomber): tech `1`, `4` members, cost `450`, build `8 s`, population `1`; each bomber hull `25`, shields `10`, refresh `3`, armor `Bomber`, reference shield type `Fighter`.
- Gameplay per bomber: `2 × FighterLaser` + `1 × ProtonTorpedo`; existing projectile profiles retained. Strong: capital ships, frigates, stations. Weak: fighters and anti-fighter weapons.
- Project tuning: length `4 units`; cruise/combat `24/27 units/s`, acceleration `18 units/s²`, turn `65°/s`, bank `35°`, formation spacing `4`, navigation radius `10`, height `11`, roster limit `10`.
- Refresh value mapped to `3 shield points per 1 s` per surviving bomber; the `1 s` interval is a project choice. Squadron shields use the existing common shield damage multipliers; separate RaW `Fighter` shield resistance is not modeled.

## Implementation

- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/DroidBomber/Cis_DroidBomber.ALO`; original ALO/DDS hashes unchanged.
- Source pack credits: model **Berruga**, textures **Berruga + Chris Boudreaux**, rigging **Nomada_Firefox**.
- Blender `3.6.23`: `35` meshes, `46` bones; restored identity `Root` confirmed in the ALO binary. Preserve `28` visible meshes and `7` hidden collision/flash helpers.
- Source `2,623` faces include `345` zero-area triangles. Blender FBX reimport removes `306`; Unity removes the remaining `39` → `2,278` rendered triangles. Every nondegenerate triangle position/UV matched in the Blender round trip at `0.001` source-unit precision.
- Maximum bone displacement: Blender `0.000002563` source units; Unity `0.00000004391` project units before gameplay scaling. All bone names/parents, mesh counts and UV coverage verified.
- Unity hull: `EmpireAtWar/Ship Lit`; supplied `_B.dds` verified as normal map, imported linear with green flip. Livery: hue `0.67`, range `0.08`, minimum saturation `0.25`, strength `1`; all eight owned palettes rendered, contrasting colors inspected.
- Lasers: port `MuzzleA_01`, starboard `MuzzleA_00`; one launcher uses the midpoint of `MuzzleB_00..03`. Other authored attachments remain. Guns follow the banking body; primary + extra weapons disable on member death.
- Registered in CIS roster, `AssetMappingData`, existing Addressables `View`/`Data` groups, `ShipUiData`, `TooltipIconData`, matchup data and `ReinforcementData`. Four-member hologram preview matches gameplay geometry.

## Files

- `Tools/Blender/export_droid_bomber.py` — model-specific fresh-import exporter.
- `Assets/Art/Models/SeparatistShips/DroidBomber/DroidBomber.fbx`; matching materials/textures under `Assets/Art/Materials/Models/SeparatistShips/DroidBomber/` and `Assets/Art/Textures/Models/SeparatistShips/DroidBomber/`.
- `Assets/Prefabs/Models/Squadrons/DroidBomber.prefab`, `DroidBomberSquadronView.prefab`.
- `Assets/Prefabs/Ui/Reinforcement/DroidBomberReinforcementView.prefab`.
- `Assets/Settings/Data/Squadron/DroidBomberSquadronData.asset`; `Assets/Settings/Data/Tooltip/Matchups/DroidBomberSquadronMatchups.asset`.
- `Assets/Art/Textures/Ui/Icons/SquadronIcon/DroidBomberIcon.png`, `DroidBomberSilhouette.png` — transparent `512 × 512` model renders.
- Sibling `DroidBomber-Converted/` — packed `.blend`, FBX, PNGs, reports, previews and source credits.

## Edge Cases

- `W_LASER_SMALL.dds` is absent; its six authored flash helpers remain disabled. Runtime weapons use existing project effects.
- Squadrons use the existing member explosion/hidden-body lifecycle, not capital-ship wreck assets. Captor complement updated by the subsequent [[GameDesign/Vulture Droid Import]]: `11` Vulture + `8` Droid Bomber launches, `1` active per type; first delay `4 s`, shared interval `8 s`.
- Saved references, member/weapon counts, hull collision, geometry/preview bounds, source hashes, registrations, compilation and isolated renders verified. No missing scripts or broken references; no new Console errors after field-type corrections.
- No automated tests or Play Mode run. Combat, shields, flight, fog, placement interaction and destruction remain unverified in battle; project tuning is provisional.
