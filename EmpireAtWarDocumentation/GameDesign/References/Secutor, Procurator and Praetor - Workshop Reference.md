---
type: design-reference
system:
  - combat
  - ships
  - asset-pipeline
tags:
  - empire-at-war
  - workshop
  - blender
verified: 2026-11-15
verification: Inspected installed mod unit, hardpoint and tooltip data. Reopened
  the saved Blender file and verified three ship collections, the comparison
  camera and 43 packed images.
---
# Secutor, Procurator and Praetor

## Files

- [Open the Blender preview](<file:///F:/Private/empire-at-war/output/workshop-ship-preview/Workshop Ship Preview.blend>) · `F:/Private/empire-at-war/output/workshop-ship-preview/Workshop Ship Preview.blend`
- Blender 3.6.23 · `Workshop Ship Preview` scene · `Comparison Camera` · 43 packed textures.
- Static hulls at equal display length. Separate weapon attachments and animations are omitted.
- Workshop root · `D:/SteamLibrary/steamapps/workshop/content/32470/`. XML paths below are relative to that root.

## Important Values

| Unit and source variant           | Hull HP | Shields | Shield refresh (XML) | Defined hardpoints | Targetable / destroyable |
| --------------------------------- | ------: | ------: | -------------------: | -----------------: | -----------------------: |
| Secutor · Holdout Imperial        |  45,000 |  30,000 |                  600 |                 47 |                       41 |
| Procurator · Fall of the Republic |  15,000 |  15,000 |                 22.5 |                 31 |               31 defined |
| Praetor · Remake 4.0 base         |  50,000 |  50,000 |                2,000 |                137 |                        0 |

- Values are source-mod balance. Each row uses the unit variant that references the previewed hull. Refresh values reproduce `Shield_Refresh_Rate` as stored.

## Secutor-class Star Destroyer

- Late-Republic heavy warship focused on ship-to-ship combat, with a large fighter garrison. The Imperial variant switches to TIEs at tech 3.
- **40 weapon banks** · 24 quad turbolasers, 5 heavy dual turbolasers, 6 heavy lasers, 3 dual ion cannons, 2 heavy proton torpedo launchers.
- **7 other entries** · 2 shield generators, 2 engines, 2 fighter bays(hangar), 

- [Clone Wars Holdout Factions](https://steamcommunity.com/sharedfiles/filedetails/?id=2794270450) · `2794270450/Data/Art/Models/Empire_Secutor.ALO`.
- Unit and inheritance · `2794270450/Data/Xml/Units/Space/Units_Space_Neutral_Republic_Secutor.xml`, lines 40–42, 334–384 and 408–426. Hardpoint definitions · `2794270450/Data/Xml/Units/Space/Hardpoints/Hardpoints_Neutral_Republic_Secutor.xml`. Description · `2794270450/Data/Text/xml/TranslationManifest.xml`, line 51762.

## Procurator-class Battlecruiser

- Kuat Drive Yards sector-defense battlecruiser used in the Core and Colonies before the Praetor superseded it. In Fall of the Republic, it anchors a fleet with turbolasers and proton torpedoes.
- **28 weapon banks** · 8 heavy dual turbolasers, 14 dual medium turbolasers, 6 proton torpedo launchers.
- **3 systems** · 1 hangar, 2 engines. 
- The unit list omits a comma between `HP_Procurator_Turbo17` and `HP_Procurator_Turbo18`. The count reflects the definitions. The in-game effect of the missing comma is unverified.
- [Fall of the Republic](https://steamcommunity.com/sharedfiles/filedetails/?id=1976399102) · `1976399102/Data/Art/Models/Procurator-class_battlecruiser.alo`.
- Unit · `1976399102/Data/XML/Units/Republic_Space_Units.xml`, lines 3653, 3698–3701 and 3747–3752. Hardpoint definitions · `1976399102/Data/XML/Hardpoints/HardPoints_FotR_Capitals.xml`, starting at line 11273. Description and weapon names · `1976399102/Data/Text/FotR_text.txt`, lines 6021–6028.

## Praetor-class Battlecruiser

- Republic-era defensive battlecruiser built to hold the line with layered armor and heavy shielding.
-  30 medium dual turbolasers, 16 heavy quad turbolasers, 38 medium ion cannon, 4 heavy concussion missile launchers, 3 heavy laser cannons, 3 heavy proton torpedo launchers.
- **7 systems** · 3 hangars, 4 engines. 
- [Remake 4.0](https://steamcommunity.com/sharedfiles/filedetails/?id=1770851727) · `1770851727/Data/Art/Models/Neutral_Republic_Praetor.ALO`.
- Unit · `1770851727/Data/Xml/Units/Space/Units_Space_Neutral_Republic_Praetor.xml`, lines 41–43 and 72–88. Hardpoint definitions · `1770851727/Data/Xml/Units/Space/Hardpoints/Hardpoints_Neutral_Republic_Praetor.xml`. Description · `1770851727/Data/Text/xml/TranslationManifest.xml`, line 87494.
