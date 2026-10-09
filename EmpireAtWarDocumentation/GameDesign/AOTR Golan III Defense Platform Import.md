---
type: reference
updated: 2026-10-09
tags:
  - alo
  - aotr
  - defense-platform
  - unity
---
# AOTR Golan III Defense Platform Import

## Decision

- Shared defense platform: `DefendPlatformType.GolanIII = 2`; available in Empire, Rebellion, Republic and Separatist rosters.
- User values: hull **10,000**, shields **12,000**; **12 heavy dual turbolasers / 4 heavy assault missiles / 12 heavy proton torpedoes / 8 lasers**.
- Role: heavy anti-capital defense; long-range firepower and torpedoes; comparatively vulnerable to concentrated bomber attacks.
- `T_Defense_Golan3` / `E_Defense_Golan3` → `SB_Golan_3.ALO`; same physical hull as the Rebel skirmish Golan III. Earlier local preview had no converted Unity artwork to reuse.
- Source XML lists 67 hardpoints and 24 separate turret models. All artwork is retained; surplus light-turbolaser artwork is decorative for the requested loadout.

## Implementation

- Shared FBXs: `Assets/Art/Models/SpaceStations/AotrGolanIII/`; materials/textures: `Assets/Art/{Materials,Textures}/Models/SpaceStations/AotrGolanIII/`.
- Visual: `Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatform.prefab`; gameplay: sibling `AotrGolanIIIDefensePlatformView.prefab`.
- Data: `Assets/Settings/Data/Models/DefendPlatform/AotrGolanIIIDefensePlatformData.asset`; catalog: `Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/AotrGolanIIIDefensePlatformReinforcementView.prefab`; sprite: `Assets/Art/Textures/Ui/Icons/Ui/AotrGolanIIIDefensePlatformIcon.png`.
- Isolated Blender 3.6.23 / ALAMO; original source-index hierarchy, geometry, UVs, authored normals and attachment poses preserved. Missing DDS references resolve from the base-game archive.
- `EmpireAtWar/Ship Lit`, direct alpha masks, green-flipped normal maps and existing additive light convention. Source helpers remain in FBX/blend; prefab strips disabled helper renderer components and restores repeated source bone names.
- 36 targetable weapons + 3 shield generators; fitted collider, 1,024-plane shield, fog/team renderer lists and own placement registration.

## Important Values

- Source purchase values: 8,250 credits / 275 s / population 3 / level 1; source per-planet limit 2 maps to roster limit 2.
- Project scale: maximum XZ diameter 300 units; bounds `132.363 × 146.360 × 300`; identity prefab roots.
- Heavy weapon profiles: existing long-range dual turbolaser `50`, laser `8`; new heavy assault missile `65` and heavy proton torpedo `66`.
- Launcher damage 50 / 150; pulses 3 / 1; intervals 1.5 / 0 s; reload 15 / 10 s; source ranges ×0.05 → 300 / 200 project units. Existing projectile speeds and damage matrices reused.
- Donor shield regeneration / hardpoint health retained; radial weapon yaw uses source cone widths. These choices do not establish AOTR balance equivalence.

## Files

- Rebuild and detailed decisions: `Tools/Blender/AotrGolanIII/README.md`.
- Editable base blend: `output/aotr-space-stations/GolanIII-Converted/AotrGolanIII.blend`; per-model blends/reports: `Temp/AotrGolanIIIImport/`.
- Unity previews: `Temp/AotrGolanIIIImport/Previews/GolanIII.png`, `Team0.png` through `Team7.png`.
- Saved checks: `VerifiedRegistration.json`, `VerifiedSourceGeometry.json`, `GameplayMounts.json` in the task folder.

## Edge Cases

- Verified 25 FBXs, 403 base bones, 79 base visible meshes, 24 attachments and 125,592 visible source triangles. Submesh texture assignments and all XML attachment poses pass.
- Max raw Unity error: vertex `0.000006950` units; bone `0.000003358` units; UV `0.000074751`; normal vector `0.0000003724`.
- 21 source DDS textures → lossless PNGs; 7 alpha masks. Source ALO/DDS/XML hashes unchanged; 212 existing station files and existing asset mappings unchanged.
- Saved prefabs/catalog/data/rosters/placement/Addressables reload correctly. Unity compilation/import completed with no new import/serialization errors.
- Unowned + eight team palette previews rendered; unowned and blue/green inspected. No Unity automated tests or battle playthrough run.
- No garrison, source abilities, death clone, ALA animation or animated EaW shader behavior recreated.
