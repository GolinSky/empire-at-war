---
type: reference
updated: 2026-10-09
---
# Imperial Venator Import

## Goal

- Empire `ImperialVenator=211`; Workshop `1770851727` unit `Venator_Empire`.
- Copy Republic Venator hardpoints; replace ten DBY-827 with existing Imperial green `HeavyDualTurbolaser=43`.

## Source

- Root: `D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data`.
- XML: `Units/Space/Units_Space_Empire_Venator.xml`, registered in `GameObjectFiles.xml`.
- Chain: `Venator_Empire → Venator_Upkeep → Venator_Template → Capital_Base → Vessel_Base → SFX_Base`.
- Exact model: `Empire_Venator_Star_Destroyer.ALO`; binary slots use dedicated `Empire_Venator_Star_Destroyer_Hull.dds` / `Empire_Venator_Star_Destroyer_Bridge.dds`.
- Ten inherited attachments: `Neutral_Republic_Venator_Star_Destroyer_HTL_01..08.ALO` → `TL_01..08`; `Neutral_Republic_Venator_Star_Destroyer_MTL_01..02.ALO` → `TL_09..10`. Republic-prefixed names are explicit shared XML references.
- Death: inherited `Death_Clone_Venator` → `Neutral_Republic_Venator_Star_Destroyer_Deathclone.ALO`; separate converted source variant. Gameplay wreck uses actual assembled Imperial hull/materials.

## Decision

- User approved fixed tech-2 `TIEFighter=200` / `TIEBomber=201`; omit Gozanti transport. Tech-1 ARC-170/V-Wing/LAAT configuration not selected.
- Source hull/shields `15,000/15,000`; price/population `17,200/26` retained. Production `70 s` chosen over source tactical `55 s`; project availability `2`, cap `3`.
- Donor movement: speed `18 units/s`, rotation/turn acceleration `7.5`; regeneration `15/s`, delay `3 s`. Source speed `3`, refresh `300` have no verified unit conversion. Balance provisional.
- Source starting/reserve fields `1/3` Fighter, `2/6` Bomber → project max-active `1/2`, total launch budgets `3/6` including initial launches. Timing `4 s` initial / `8 s` interval; shared destroyable hangar.
- Ability `ImperialVenatorIntensifyFirepower=35`: FullSalvo, `15 s`, recovery `60 s`, non-interceptable delay ×`0.33`; all copied guns benefit. Source heavy-battery effect broadened through existing project FullSalvo convention; damage/speed/regeneration unchanged.
- Strategic upkeep `-250`, death refund dummy, Lua DEFEND/layer autofire and ALA animation are not implemented as additional project tactical abilities. Turret geometry remains static.

## Implementation

- Blender `3.6.23`, existing MCP protocol `13`; upstream ALAMO import; binary material-slot repair, source hidden flags, UVs, hierarchy and attachments preserved. Source helpers retained in blends/FBXs.
- Twelve models converted: hull `10 meshes / 53,187 triangles / 68 bones`; heavy turret each `7/1,292/11`; medium each `4/1,001/6`; death `8/40,865/39`.
- Opaque `Ship Lit` and separate additive windows/engines; explicit FBX remaps. Linear normal maps, green-channel flip; Imperial grey albedo, inverse source alpha mask ×`0.2`, HSV livery strength `0`.
- Length `120` project units; visible size `54.727 × 26.057 × 120`; bow `+Z`, up `+Y`; identity gameplay/placement roots and banking body. Navigation radius `72`; bank ±`15°`; launch `8` units below opaque hull.
- Fifteen weapons / eighteen unique targets `0..17`: ten green heavy dual, two donor heavy turbolasers, two heavy ions, one SPHAT; plus shield/engine/hangar. Arcs copied unchanged; all weapons in health list, colliders and matching health types.
- Heavy L1/L2/L3/L4 → turret 08/05/06/07 `FP_01`; R4/R1/R2/R3 → 01/02/03/04. Side dual R/L → medium 01/02 `FP_01`.
- Ion R/L → `Laser_Hardpoint_02_Fire` / `Laser_Hardpoint_01_Fire`; extra heavy R/L → `Laser_01` / `Laser_08`; beam/shield/hangar → `SPHAT` / `Shield_Generator` / `SPAWN_00`. Engine target → ten-emitter mean.
- Own shield/wreck/placement/icons/matchups; Empire-only roster, view/data Addressables, asset/ship/UI/tooltip/reinforcement registries, icon generator and ability/audio entries saved. No new UI behavior.

## Verification

- 2026-10-09: `Verify.cs` passed `1,579` saved-asset checks; `15` weapons / `18` targets / two bays; actual mount displacement `0`.
- Blender geometry/UV/bone round-trip differences <`0.001` source units. All twelve Unity mesh/triangle/UV/submesh/parent checks passed; maximum bone displacement `0.0000039321003` Unity units before gameplay scaling.
- All `34` audited ALO/DDS/XML source hashes unchanged. Six Republic registration records and five donor assets plus metadata match Git HEAD.
- Scoped helper pass: `32` helpers / `57,278` triangles removed; real shield retained; repeat finds zero strippable helpers.
- Saved prefab references, gameplay colliders/banking envelope, hangar exit, shield/wreck ownership, hologram, three icon consumers, registries and compiled enum IDs checked. `EditorUtility.scriptCompilationFailed=false`; final Console interval contains no new import/serialization/compile errors. Earlier unrelated Balance/ISD errors and resolved transient squadron compile errors retained.
- Top/stern, eight living/wreck palettes and actual placement renders saved. All living palettes and contrasting blue/green inspected; rendering palette restored.
- No NUnit suite or combat Play Mode run. Runtime combat/launches/abilities/placement/death remain untested; balance values above are provisional.

## Files

- `Tools/Blender/ImperialVenator/README.md` — source decisions, exact mappings and rebuild commands.
- `Tools/Blender/ImperialVenator/Evidence.json` — source hashes/resolved stats, attachments, conversion/Unity errors and saved-asset verification.
- `Assets/Prefabs/Models/Ships/ImperialVenatorShipView.prefab`, `Assets/Settings/Data/Ship/ImperialVenatorShipData.asset` — gameplay assets.
- `Assets/Prefabs/Ui/Reinforcement/ImperialVenatorReinforcementView.prefab` — placement; dedicated shield/wreck/icon references in evidence.
- `output/ImperialVenator/Previews/` — ship, palettes, wreck and placement screenshots.
- `output/ImperialVenator/ImperialVenator-Converted.zip` — twelve editable blends/FBXs, textures/reports/previews; CRC and SHA-256 verified.
- [[Done/Features/ImperialVenator_Import|Completed import plan]].

## Engine Rendering Follow-up

- Source engine triangles retained: `Engine_Exhaust=540`, `Engine_Exhaust_Glow=540`, `Engine_Glow.005=1,400`; all enabled. No missing source geometry found.
- Both engine effect materials use two-sided URP Unlit, additive blending and no depth writes; outer blue glow uses HDR RGB ×2. Static source-texture approximation; original wave/distortion textures retained in editable archive.
- Rear/upper/lower glow and opaque-only screenshots verified; all ten engine faces visible. Authored dark casing bands retained; source geometry/UVs/bones/textures unchanged.
- `Verify.cs`: 1,579 checks passed; twelve Unity geometry imports and 34 source hashes unchanged. No tests or Play Mode started for the follow-up.
- Concurrent Balance compiler errors were transient and resolved; final `scriptCompilationFailed=false`, Editor idle, clean saved scene. No Imperial Venator shader/import errors observed.
