# MC75 Profundity Import

## Goal
- Rebellion capital ship `ShipType.MC75Profundity = 304`; AOTR model `RV_Profundity.ALO`.
- User values: hull `18,000`, shields `13,000`, speed display `225` → approved `22.5` Unity units/s.

## Decision
- Approved fighter complement: X-Wings and Y-Wings instead of source Z-95/Y-Wing.
- Static model conversion; own placement, wreck, shield mesh, icon and silhouette. Existing procedural wreck shader replaces source death effects.
- Full Salvo `19`: active `20 s`, recovery `60 s`; interceptable missile/rocket/torpedo shot intervals and reloads ×`0.33`, other weapons ×`3`. Stop restores both multipliers to `1`.

## Important Values
- Targetable `12`: six `Med Lrg Proton TRP`, two medium ion torpedoes, two shield generators, one engine, one hangar. Health IDs `0..11`.
- Non-targetable `22`: twelve light close-range turbolasers, four heavy lasers, six existing point-defense mounts. Weapons `30`; fog hardpoints `34`.
- Torpedo/ion damage `80/80`, reload `7.5/15 s`; light turbolaser damage `18`, three shots at `0.2 s`, reload `3.75 s`; heavy laser damage `17.5`, reload `2.25 s`.
- Weapon profiles `25..28` are dedicated. Existing point defense `14` remains shared. Ion torpedoes use existing ion damage multipliers, without source 10-second stun.
- Weapon/engine/shield/hangar health `750/3000/750/500`; shield regen `21.66/s`, delay `1 s`.
- Source economy: cost `13,000`, build `260 s`, station level `3`, galactic population `8`. Provisional project limit `3`; heavy-capital damage class.
- Each fighter bay: `2` total launches including initial squadron, `1` active slot. Initial/shared launch interval `4/30 s`.
- Visual bounds `36.64993 × 56.72248 × 160`; root scale `1`, bow `+Z`, up `+Y`. Opaque wreck/placement geometry `30.58704 × 56.64001 × 159.87341`.
- Provisional navigation radius `90`, radar range `400`, yaw/acceleration `6`, banking `±5°`, inherited height tier `2`. Banked hull Y `−28.29502..28.25862`.
- Launch `(-0.00619, -36.36124, 44.15976)` relative to body; collider clearance `8` units below hull.

## Implementation
- Guide read before import: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. Placement/UI rules read before asset creation.
- Blender `3.6.23` + ALAMO: `146` imported bones → restore only verified binary identity `Root` → `147`. Mesh constraints converted to bone parenting with world transforms preserved.
- Source/FBX/Unity: `17` meshes, `73,588` triangles; `7` source shadow meshes disabled, `10` visible meshes. Unity triangle counts and UV presence match every source mesh.
- Blender bone/corner displacement ≤`0.000455058/0.000344001` source units; Unity bone displacement ≤`0.000001585` project units, no parent mismatches. All hardpoint attachment errors `0`.
- Seven DDS conversions retain decoded pixels exactly. Original ALO and seven DDS SHA-256 values unchanged.
- Hull alpha max `115`: inverted alpha would recolor neutral plating. Separate linear team masks select authored cyan/red paint; HSV livery disabled, normal green flipped.
- Eight owner palettes, unowned icon, opaque-only placement and fresh-wreck shader preview rendered; blue/green and placement/wreck inspected. Icons `512 × 512`, alpha bounds `(52,119)..(449,359)`.
- Saved prefab references, roster, ship-data/view mappings, existing Addressables `View`/`Data`, three icon consumers, reinforcement, abilities and audio registrations inspected. No donor-model dependencies remain.
- Unity imports and compilation complete; no MC75 import/serialization errors. Unrelated concurrent TIE Avenger builder logged a float-property error. Main menu scene remained clean.
- No automated tests or Play Mode run. Runtime and provisional balance acceptance remain in [[TODOs/Features/MC75_Profundity_Import]].

## Files
- Installed source: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/RV_Profundity.ALO`; XML `SpaceUnitsCapital.xml`, `Hardpoints_Rebel_Space.xml`.
- Tools: `Tools/Blender/prepare_mc75_profundity.py`, `export_mc75_profundity.py`, `MC75Profundity/{BuildArt,BuildGameplay,Register,Render,Inspect}.cs`.
- Packed Blender, FBX, textures, source/conversion/Unity reports, attachment mapping and previews: ignored `Temp/MC75ProfundityImport/`.
- Runtime art `Assets/Art/{Models,Textures,Materials}/.../MC75Profundity/`; own shield `Assets/Art/Models/Shields/MC75ProfundityShield.asset`.
- Prefabs: `MC75Profundity`, `MC75ProfundityShipView`, `MC75ProfundityWreckView`, `MC75ProfundityReinforcementView`; data `MC75ProfundityShipData`, `MC75ProfundityWreckData`, `MC75ProfundityMatchups`.

## TODO
- Clean-skirmish build, placement, ownership/fog, hardpoint destruction, ion/proton fire and point-defense acceptance.
- Verify both fighter bays, engine/hangar damage, Full Salvo expiry/recovery and destruction/wreck lifecycle.
- Review provisional scale, movement/range, point-defense tuning, shield behavior, economy and build limit.
