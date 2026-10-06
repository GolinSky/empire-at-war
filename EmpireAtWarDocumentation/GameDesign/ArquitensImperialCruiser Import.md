# ArquitensImperialCruiser Import

## Goal

- Empire light cruiser `ArquitensImperialCruiser = 204`; source AOTR 2.11.9 `E_Arquitens_Light_Cruiser`.
- Import/static integration complete on `2026-10-06`; gameplay acceptance remains in [[TODOs/Features/ArquitensImperialCruiser_Import]].

## Decision

- Supplied hull/shields/speed: `1,300/1,200/30`; source raw speed `3.0`. User speed takes precedence.
- Eight weapons are non-targetable and non-destroyable; hull-only targeting, no independent shield/engine/hangar targets or fighter complement.
- All eight source hardpoints have `Fire_Pulse_Count = 2`; the explanatory wiki prose's four-shot turbolaser claim does not match installed XML.
- Separate Empire identity; preserve existing Republic `Arquitens` and other weapon/ability definitions.

## Implementation

- Source `ev_arquitens.ALO`: `10 meshes / 13,861 triangles / 78 bones`.
- Quad turret `T_Quad_Medium_01..04.ALO`: byte-identical; one converted asset, four instances, `6 meshes / 680 triangles / 8 bones` each.
- Dual turret `T_Dual_Medium_01..04.ALO`: byte-identical; one converted asset, four instances, `4 meshes / 436 triangles / 7 bones` each.
- Quad mounts `HP_T01..HP_T04`; dual mounts `HP_01..HP_04`. Weapons use midpoint of source `MuzzleA_00/01`; source bone heading/fire-cone widths define yaw arcs.
- Blender `3.6.23` + ALAMO, isolated MCP port `9882`. Binary-audited identity Root restored; original parents, bone positions, UVs and hidden flags retained.
- FBX scale `0.02`; vertex welding/compression disabled. Centered visual/gameplay bounds `29.57915 × 15.74097 × 40` units; bow `+Z`, up `+Y`, root scale `1`.
- Own hull-fitted shield, hull-only health, eight weapon/fog bindings, all `51` team renderer bindings, collision/ion volumes, selection, opaque-hull hologram and shader-breaking wreck.
- Team bindings include disabled source helper meshes; visibility stays disabled. Weapon and Boost activation/execution/end/recovery audio are registered.
- Own model-derived transparent icon/silhouette, Empire roster, ship/data/view Addressables, reinforcement mapping, HUD/tooltip icons, matchups, weapon/audio definitions.

## Important Values

- Turbolaser profile `LightLongRangeTurbolaser = 39`: damage `67.5/shot`, `2` shots, pulse spacing `4.75 s`, reload `9.75 s`, range `562.5` units.
- Laser profile `BurstLaserCannon = 40`: damage `7.5/shot`, `2` shots, pulse spacing `0.5 s`, reload `1.25 s`, range `200` units.
- Range conversion `1/8`: source `4,500/1,600`; reload midpoint of source `9.5–10/1–1.5 s`. Projectile speed `133` project units/s, green bolts.
- `ArquitensBoostWeaponPower = 23`: active `20 s`, recovery `60 s`, fire delay ×`0.5`, speed ×`0.25`, shield regeneration ×`0`, incoming damage ×`1.5`; own settings instance.
- Shield regeneration `4/s`, delay `1 s`; bank ±`15°`, navigation radius `27`, banked hull range `−11.43013..+11.43013` units.
- Provisional economy: cost `3,250`, build `65 s`, tech `1`, population `3`, maximum `10`.
- Provisional movement: yaw `45°/s`, turn acceleration `45`, height tier `5`, hyperspace `0.6 s`; inherited Republic Arquitens movement/matchups.
- Main Colorize texture alpha is zero → neutral hull. Turret alpha team-mask coverage `5.593%`; normal green channel flipped. Metallic/smoothness `0.15/0.25` are provisional.

## Edge Cases

- Source turbolasers exclude fighter/bomber/gunship targets; project profiles cannot express that exclusion. Current turbolasers can target strikecraft.
- Source ability disables weapon-energy regeneration; project weapons have no energy model. Defense/speed/fire-rate effects are configured.
- Source death clone `EV_Arquitens_DC.ALO` and five death ALA animations are not converted. Project wreck uses living opaque geometry with the existing cut/drift/dissolve shader.
- Source alpha specular response is not reproduced exactly; project metallic/smoothness values are provisional.
- No automated Unity tests or Play Mode started by this import. Saved assets and compilation passed; in-game acceptance remains unverified.
- A concurrent Play session overlapped the final prefab build and logged `UnitWreckView.Awake` during temporary construction. Persisted wreck bindings and all saved assets passed subsequent readback. Construction helpers now reject Play Mode; the other session remains undisturbed.

## Files

- Installed source: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/`; XML `SpaceUnitsFrigates.xml`, `Hardpoints_Empire_Space.xml`, `PROJECTILES_SPACE.XML`.
- Gameplay/data: `Assets/Prefabs/Models/Ships/ArquitensImperialCruiserShipView.prefab`; `Assets/Settings/Data/Ship/ArquitensImperialCruiserShipData.asset`.
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/EmpireShips/ArquitensImperialCruiser{,QuadTurret,DualTurret}/`; fitted shield `Assets/Art/Models/Shields/ArquitensImperialCruiserShipViewShield.asset`.
- Placement/wreck: `Assets/Prefabs/Ui/Reinforcement/ArquitensImperialCruiserReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/ArquitensImperialCruiserWreckView.prefab`.
- Procedure: `Tools/Blender/ArquitensImperialCruiser/README.md`. Editable blends/FBXs/textures/credits/evidence: `output/aotr-empire-units/ArquitensImperialCruiser-Converted/`.
- Full supplied `Credits_and_Permissions.pdf` retained unchanged; model-specific author assignment remains unverified.

## TODO

- [x] Verify source/FBX geometry and UV corners: maximum position error `0.00001574` raw units; UV error `0`.
- [x] Verify all Unity mesh/triangle counts, UVs, bone parents and positions: maximum bone displacement `0.000000452` project units.
- [x] Verify saved registrations, abilities, weapons, dependencies and materials: zero missing scripts or broken serialized references.
- [x] Render eight live/wreck palettes, placement, icon/silhouette and top/stern views; source hashes unchanged for all `21` audited files.
- [x] Verify compilation/imports and clean `MainMenuScene`; all changed assets saved/imported.
- [ ] Clean-skirmish acceptance: deployment, hull-only damage, eight weapons/arcs, Boost Weapon Power, shields, visibility/team colors and destruction.
- [ ] Review provisional scale, movement, weapon timing/ranges, economy/matchups and documented source differences.
