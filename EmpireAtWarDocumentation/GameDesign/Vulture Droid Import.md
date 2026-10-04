---
type: reference
updated: 2026-10-04
tags:
  - blender
  - unity
  - squadron
  - cis
---
# Vulture Droid Import

## Goal
- CIS **Vulture-class Droid Starfighter**; `SquadronType.Vulture = 102`.
- Static combat-configuration import using [[Architecture/ALO_MODEL_IMPORT_GUIDE]]; existing squadron MVP owns flight, health, weapons and lifecycle.

## Important Values
- User-supplied RaW values: tech `1`, `7` members, cost `350`, build `8 s`, population `1`; each fighter hull `35`, shields `0`, armor `Fighter`.
- Per fighter: `4 × FighterLaser`, existing profile damage `5` per shot; source positions `MuzzleA_00..03`. Missile attachments `MuzzleB_00/01` retained without launchers.
- Project tuning: length `4 units`; cruise/combat `34.5/39 units/s`, acceleration `27 units/s²`, turn `100°/s`, bank `50°`, spacing `3`, height `11`, roster limit `10`.
- Navigation radius `14`, selection diameter `28`; measured formation radius `13.56549`. Individual collision sphere radius `2.4`; visible hull `2.41040 × 1.02828 × 4.00000`.
- Strong: bombers, fighters. Weak: corvettes, dedicated anti-fighter weapons. No hyperdrive; no shields or shield regeneration.

## Implementation
- Source: `C:/Users/golin/Documents/CIS_Space_Units2014_checked/CIS_Space_Units/Vulture/CIS_VULTURE.ALO`; original ALO/DDS SHA-256 hashes unchanged.
- Blender `3.6.23`: binary `14` bones, imported `13` → verified identity `Root` restored. Preserve `7` meshes: visible Hull; hidden Collision, Shadow and four muzzle-flash helpers.
- Imported source `2,471` triangles → FBX/Unity `2,407`; FBX removes `64` repeated-index/zero-area flash triangles. Hull retains all `2,303` triangles. Nondegenerate positions/UVs match at `0.001` source-unit precision.
- Bone displacement: Blender maximum `0.000007154` source units; Unity maximum `0.00000016792` units. All bone names/parents and mesh UV coverage verified.
- FBX scale `0.02`; centered visual geometry scale `2.328506`; bow `+Z`, up `+Y`; visual/gameplay/placement roots scale `1`.
- Hull shader `EmpireAtWar/Ship Lit`; supplied `cisvulture_b.dds` verified as normal data, linear normal-map import with green flip.
- Livery hue `0.67`, range `0.08`, minimum saturation `0.25`, strength `1`; all eight owned palettes rendered and contrasting colors inspected.
- Registered CIS roster, data/view lookup, existing Addressables View/Data groups, HUD/tooltip icons, matchup data and seven-member hologram placement preview.
- Primary + three additional guns follow each banking body and disable through the existing member destruction lifecycle; flight/health/fog/team collections rebound explicitly.

## Edge Cases
- Buzz Droids deliberately excluded by user request.
- Lock S-Foils logic/animation not added in this asset import: one static hull mesh and no ALA animation files supplied; project has no existing S-Foils mechanism.
- Missing `W_LASER_SMALL.dds` affects disabled source flash helpers only; runtime laser shots use project effects.
- Lore designation supplied by user: Variable Geometry Self-Propelled Battle Droid, Mark I; lore lists blaster cannons, concussion/Discord missiles. The imported gameplay loadout uses the supplied laser role.
- Saved asset/reference, geometry, icon/silhouette/palette/squadron and compilation checks complete. Missing scripts `0`; no new Console import/serialization errors; main scene remains clean.
- No automated tests or Play Mode run. Flight, combat, placement interactions, fog and destruction remain unverified in battle; movement/size/limit tuning is provisional.
- User selected Captor complement replacement: `11` Vulture + `8` Droid Bomber total squadron launches, `1` active per type (`2` overall), first delay `4 s`, shared interval `8 s`. Saved data and resolved squadron views verified; runtime launch acceptance remains in [[TODOs/Features/Captor_Import]].

## Files
- Exporter: `Tools/Blender/export_vulture.py`; repeatable conversion details: `Tools/Blender/README.md`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/SeparatistShips/Vulture/`.
- Visual/gameplay: `Assets/Prefabs/Models/Squadrons/Vulture.prefab`, `VultureSquadronView.prefab`.
- Placement: `Assets/Prefabs/Ui/Reinforcement/VultureReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Squadron/VultureSquadronData.asset`; matchups: `Assets/Settings/Data/Tooltip/Matchups/VultureSquadronMatchups.asset`.
- Icons: `Assets/Art/Textures/Ui/Icons/SquadronIcon/VultureIcon.png`, `VultureSilhouette.png` — transparent `512 × 512` model renders.
- Sibling `Vulture-Converted/`: packed Blender source, FBX, PNGs, conversion/saved-reference reports, previews and pack README.
- Source credits: model/textures **Star Wars Battlefront II, Pandemic/LucasArts**; rigging **Nomada_Firefox**. Pack README requests contacting its author before public-mod use.
- User references: [RaW Vulture Droid](https://republicatwar.wiki.gg/wiki/Vulture_Droid), [Vulture-class starfighter Legends](https://starwars.fandom.com/wiki/Vulture-class_starfighter/Legends); supplied facts retained without independent wiki verification.
