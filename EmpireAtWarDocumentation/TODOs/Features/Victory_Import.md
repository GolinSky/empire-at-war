---
category: Features
status: in-progress
---
# Victory Import

## Goal

- Add the Imperial Victory to the Empire roster using `ReV_Victory.ALO` and supplied vanilla gameplay values.
- Verify import assets and retain clean-skirmish acceptance as unfinished work.

## Rules

- Source: `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_Victory.ALO`.
- Hull / shields / regeneration `3,400 / 800 / 50`; speed / turn `1.6 / 0.5`.
- Tech / cost / build / population `2 / 4,200 / 25 s / 1`.
- Six targets: two enhanced turbolaser batteries, one ion battery, shields, engines and hangar.
- Boost: active `20 s`, fire delay `×0.5`, speed `×0.5`, shield regeneration `0`, recovery `60 s`.
- TIE complement: two fighters + one bomber active; four fighters + two bombers additional reserves.

## Implementation

- [x] Read `ALO_MODEL_IMPORT_GUIDE`, placement rules and UI guidelines.
- [x] Audit source, restore identity Root, convert textures/FBX and verify Blender round trip.
- [x] Create Victory art/gameplay/placement/wreck assets, team colors and icons.
- [x] Register Empire roster, ship data, Addressables, UI, tooltip matchups and placement.
- [x] Configure dedicated Victory Boost Weapon Power and audio using existing ability implementation.
- [x] Locate vanilla TIE assets; import seven-fighter/four-bomber squadrons and configure carrier reserves.
- [x] 2026-10-05 follow-up: enable separate Empire TIE purchases. Fighter `300 / 10 s / level 1`; bomber `550 / 17 s / level 2`; population `1`, queue limit `10` each. Saved values read back; existing buy/queue checks inspected.
- [x] Inspect saved references, geometry, bone hierarchy, palettes and Unity compilation/Console.
- [ ] Clean-skirmish acceptance: purchase/placement, movement, combat, hardpoint destruction, TIE launches/replacements, boost timing/restoration and wreck.
- [ ] Review provisional balance and retained source Republic markings.

## Important Values

- Victory: six meshes, `25,758` triangles, `76` bones; visible length `110` project units.
- All three models: `17` meshes; Unity maximum geometry error `0.00000693` project units; source hashes unchanged.
- Saved prefabs/registries have no missing references; unique IDs, six targets and TIE weapon/member bindings verified.
- Unity ready, not compiling, Play Mode stopped, Console errors `0`; no automated tests or Play Mode run.
- Project-specific balance choices and cosmetic limitations: [[GameDesign/Victory Import]].

## Files

- Reference: [[GameDesign/Victory Import]].
- Prefab/data: `Assets/Prefabs/Models/Ships/VictoryShipView.prefab`, `Assets/Settings/Data/Ship/VictoryShipData.asset`.
- Evidence: `Temp/VictoryImport/{SavedInspection,TIEInspection,FinalAssets,GeometryVerification}.json`, `Output/SourceHashes.json`.

## TODO

- Keep this plan active until runtime acceptance and provisional balance review are complete.
