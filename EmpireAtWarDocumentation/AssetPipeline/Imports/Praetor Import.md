---
type: integration-record
updated: 2026-11-15
system:
  - asset-pipeline
  - units
---
# Praetor Import

## Source

- Remake 4.0 Workshop `1770851727` → `Data/Art/Models/Neutral_Republic_Praetor.ALO`. Exported the static hull and sockets from the Praetor collection in `output/workshop-ship-preview/Workshop Ship Preview.blend`.
- Installed `Units_Space_Neutral_Republic_Praetor.xml` lists 450,000 hull, 100,000 shields and 66 medium dual turbolasers. Saved gameplay uses the workshop reference note's provisional 50,000 hull and 50,000 shields with all installed sockets.
- Separate turret art, source animation and ambient overlay are outside the static conversion. The current squadron catalog supplies ARC-170 and V-wing bays. The remaining hangar is a structural target.

## Implementation

- `ShipType.Praetor = 19` → Republic station level 5, 18,000 credits, 90 s, 15 population, limit 1.
- Heavy capital. Length 425 project units, navigation radius 236, speed 6, bank ±5°, heavy-capital shield regeneration and Full Salvo.
- 130 weapon mounts + 4 engines + 3 hangars. All 137 are targetable and use the shared hull health budget.
- Weapon loadout: 66 medium dual turbolasers, 16 heavy quad turbolasers, 38 medium ion cannons, 4 heavy dual missile launchers, 3 heavy lasers, 3 heavy proton torpedo launchers.
- ARC-170 and V-wing → 8 launches per type including initial launch capacity, maximum 2 active per type. Each bay has a separate destruction binding and launch exit.
- `Assets/Prefabs/Models/Ships/Republic/View/PraetorShipView.prefab`. Living, placement and wreck views → `RepublicUnitViews`. Ship data → `Data`. Icon → `SharedIcons`.

## Verification

- Unity readback verified the faction gate, five Addressables entries, registry routes, projectile profiles, health and weapon bindings, hangar exits, and saved prefab references.
- Inspected actual-model icon, two team palettes, wreck and hologram renders. No Praetor import or serialization errors observed.
- Automated tests, Play Mode combat and packed Addressables build were not run. Economy, combat balance and runtime performance remain unmeasured.
