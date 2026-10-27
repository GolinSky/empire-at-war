---
category: Features
status: done
completed: 2026-10-27
---
# Wavecrest-class Frigate Import

## Goal
- Added `wavecrest_frigate` to CIS as `ShipType.Wavecrest = 111` with Medium-caliber weapons.

## Implementation
- [x] Power to Shields follow-up: dedicated ability53, active20s/recovery60s, regeneration×10 (300/s), speed×0.8, unchanged weapons; shield icon/defensive AI/audio bound. Saved ability verification passed2026-10-27; no Play Mode or automated tests run.
- [x] Converted original hull, eight attached turret models, hidden engine helper and three textures; source hashes/UVs/hierarchy verified.
- [x] Built own gameplay, fitted shield/collision/selection, wreck, placement preview and actual-model icon.
- [x] Registered CIS roster, data, Addressables, reinforcement, audio and tooltip/icon consumers.
- [x] Verified 17 actual/bound hardpoints, initialized/simulator HP, shared Medium profiles and saved references.
- [x] Inspected icon, hologram, wreck and contrasting team-color renders; no new Console errors after baseline cursor467.

## Important Values
- Hull3100; shields1500; regeneration30/s through own `WavecrestShieldRegenTiersData.asset`; shared frigate25/s unchanged.
- Weapons: eight turbolasers, four lasers, two ions, one proton torpedo launcher; engines and hangar targetable.
- Weapon HP250 except ions600; engines400; hangar250.
- Vulture squadrons: two active, three total launches (one reserve); initial/launch interval5s.
- Cost3500; build45s; population3; station4. XML tech3 recorded; project exposes one Station Level gate.
- Provisional: length90 units; speed29 units/s; turn/acceleration60 degrees/s; bank15°; range550; maximum10.
- Visible bounds21.0336 × 73.2647 × 90; navigation radius49; banked Y[-36.63236,36.63237].

## Rules
- Shared Medium profiles govern combat timing/damage/range; source XML values retained in audit.
- No automated Unity tests or Play Mode run, per import guide; runtime balance remains unverified.
- Source armor strings omitted; existing CIS Providence audio reused; matchup claims empty.

- Source shield regen interval×0.1 maps to continuous regeneration×10; source energy modifiers have no project energy-system equivalent.

## Files
- Source: `D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/XML/Units_Space_cis_Wavecrest_Frigate.xml`.
- Builders/provenance: `Tools/Blender/Wavecrest/README.md`; `Tools/Blender/Wavecrest/Evidence/`.
- Gameplay: `Assets/Prefabs/Models/Ships/Separatist/WavecrestShipView.prefab`.
- Data: `Assets/Settings/Data/Ship/Separatist/WavecrestShipData.asset`.
