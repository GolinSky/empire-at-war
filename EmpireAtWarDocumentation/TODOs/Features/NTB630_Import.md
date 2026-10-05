---
category: Features
status: in-progress
updated: 2026-10-05
---
# NTB-630 Import

## Goal
- Import `ReV_ntb630.ALO` and register a four-craft Republic bomber squadron.
- Supplied values: 60 hull / 30 shields / refresh 3 per craft; cost 550, build 8 s, population 1, early tech.

## Rules
- Original ALO/DDS remain unchanged; preserve hidden geometry, UVs, bones and attachments.
- Separate background Blender 3.6.23 process; shared Blender instance belongs to another agent.
- Follow-up request enables existing `ShipAbilityId.IonShot = 9`; no ability code, catalog, projectile or effect changes.
- No automated tests or Play Mode requested; carrier changes outside scope.

## Implementation
- [x] Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], placement and UI rules; inspect live source and donor assets.
- [x] Convert packed Blender/FBX/textures; eight meshes, fourteen bones, 5,097 triangles preserved, UVs and parents verified.
- [x] Create visual/gameplay/data/placement; four members, twelve unique weapons, one engine trail/member and fitted collision/navigation/selection bounds.
- [x] Register Republic roster, own asset mapping, existing View/Data Addressables groups, icons, team colors and matchup assets.
- [x] Read back saved references and values; no missing scripts, broken references or donor geometry. Matching placement/gameplay bounds; blue/green and transparent icon visually inspected.
- [x] Verify original source hashes and pixel-identical PNGs; no new Unity import, serialization, compile or Console errors.
- [ ] Restore original turret textures if supplied: missing `ReV_ntb2.dds` / `ReV_ntb2_gloss.dds`; current material is explicit neutral metal.
- [x] Enable existing Ion Shot in squadron data; verify saved ability ID, catalog settings/icon and clean Unity import/Console checks.
- [ ] In-game acceptance: production, placement, flight, laser/torpedo runs, Ion Shot targeting/disruption, hull/shields, fog/selection, team colors and craft death.
- [ ] Review provisional movement, weapon profiles, scale, shield-refresh interval and unit limit.

## Important Values
- `SquadronType.NTB630 = 4`; level/cost/build/population `1/550/8 s/1`. User build overrides local XML `17 s`.
- Per member: Bomber armor, hull/shields/refresh `60/30/3`; refresh interval 1 s, no passive repair; existing Ion Shot ability `9`.
- Centered craft size `2.650344 × 0.853184 × 4`; formation radius 12.52512 → navigation 13; member radius 2.37352 → collider 2.4; selection diameter 26.
- Source/FBX/Unity all `8 / 5,097 / 14` meshes/triangles/bones. Max Blender geometry error `0.000005723` source units; Unity bone error `0.0000001062` units; no parent mismatches.

## Files
- Reference: [[GameDesign/NTB-630 Import]]. Conversion/reproduction notes: `Tools/Blender/README.md`.
- `Assets/Prefabs/Models/Squadrons/NTB630.prefab`, `NTB630SquadronView.prefab`; `Assets/Settings/Data/Squadron/NTB630SquadronData.asset`.
- Type-first art `RepublicShips/NTB630`; own placement, icon/silhouette and matchup assets registered.
- Sibling `ReV_ntb630-Converted/`: packed source, FBX, textures, reports, previews, scripts and credits.

## Edge Cases
- All static import/integration steps complete on 2026-10-05; plan stays active for missing original textures and runtime/balance acceptance.
- No automated tests or Play Mode run; static inspection does not establish runtime behavior or balance.
- Source animated shader effects, ALA animation conversion and carrier complements are outside scope.
