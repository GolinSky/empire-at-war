# TIE Avenger

## Source

- AOTR `2.11.9`, workshop `1397421866`; Empire unit `E_TIE_Avenger`.
- Model: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/TIE_AVENGER.ALO`.
- Textures: `Tie_Fighter_Adv_Bomber.dds`, `Tie_Fighter_Adv_Bomber_NM.dds`; original files unchanged.
- Gameplay: `SpaceUnitsFighters.xml`; weapons: `Hardpoints_Empire_Space.xml`.
- Conversion: `prepare_tie_avenger.py`, then `export_tie_avenger.py` through Blender MCP `3.6.23`.
- Staging and reports: `Temp/TIEAvengerImport/Output/`; editable source: `TIEAvenger.blend`.

## Gameplay

- `SquadronType.TIEAvenger = 202`; six fighters. Per fighter: hull `20`, shields `12` → squadron totals `120` / `72`.
- Source speed `550` → Unity cruise/combat speed `55`, explicitly approved by the user.
- Per fighter: two `LightRepeatingLaser` hardpoints and two `FighterConcussionMissile` hardpoints. Weapons have no separate health targets; health targets are the six fighters.
- Laser muzzles: `MuzzleA_00`, `MuzzleA_01`; missile muzzles: `MuzzleB_00`, `MuzzleB_01`. Four authored laser attachment bones remain available in the FBX.
- `TIEAvengerPowerToWeapons = 20`: existing `BoostWeaponPowerSettings`; fire delay `0.5×`, speed `0.75×`, damage taken `1.5×`; duration `10 s`, recovery `60 s`.
- Seeker countermeasure: one intercept per living fighter per `10 s`; radius `50` project units, minimum projectile travel `10` units. Source values are `500` / `100`, scaled `0.1×`.
- Countermeasure targets incoming concussion-missile damage projectiles headed for the same owner; torpedoes remain eligible for ordinary point defense but are excluded from this passive. Ion disable, cloak and fighter death suppress the passive.
- Existing fighter explosion lifecycle replaces capital-ship wreck/hangar setup. No new hangar composition is implied.

## Project Balance Choices

- Cost `750`, level `3`, build `20 s`, population `1`, maximum `10` squadrons. These are provisional project choices.
- Weapon damage reuses project profiles: repeating laser `5` per shot; concussion missile `40`. Laser salvo `8`, interval `0.1 s`, reload `4.25 s`; one missile per launcher every `15 s`.
- Weapon range `70`, formation spacing `5`, navigation radius `22`; other flight/radar settings inherit TIE Fighter data.
- Shield regeneration `0.12` per fighter every `1 s`; hull repair `0`. Final gameplay length `4` units; root scale `1`.
- Hull uses `EmpireAtWar/Ship Lit`, source normal map with green-channel flip, neutral livery and owner-colored rim. Neutral source panels stay grey.

## Assets

- Visual: `Assets/Prefabs/Models/Squadrons/TIEAvenger.prefab`.
- Gameplay: `Assets/Prefabs/Models/Squadrons/TIEAvengerSquadronView.prefab`.
- Data: `Assets/Settings/Data/Squadron/TIEAvengerSquadronData.asset`.
- Preview: `Assets/Prefabs/Ui/Reinforcement/TIEAvengerReinforcementView.prefab`.
- Icons: `Assets/Art/Textures/Ui/Icons/ShipIcon/TIEAvengerIcon.png`, `TIEAvengerSilhouette.png`.
- Registered in Empire roster, AssetMappingData, existing Addressables View/Data groups, ShipUiData, TooltipIconData, ReinforcementData and weapon/ability audio.
- Import builders and verification snippets: `Temp/TIEAvengerImport/`; these are task staging scripts, not a general integration API.

## Verification — 2026-10-06

- Source: `16` bones including identity `Root`; five meshes, three hull LODs. Highest-detail hull is `Tie_Adv_LOD2`, `1,147` triangles.
- ALAMO's global shadow cleanup failed on another scene's object. Completed its welding/visibility/constraint cleanup on the Avenger scene only, retaining source `Root` hierarchy.
- Export round trip: five meshes, `7,121` post-cleanup triangles, UV layer on every mesh, identical bone names/parents and hidden flags. Maximum bone error `5.3644e-7`, bounds error `2.3842e-7` source units.
- Unity generated a reversed LOD group from the model names. Removed the group in the visual prefab and retained only `Tie_Adv_LOD2` enabled; helper and lower-detail renderers stay disabled.
- Saved gameplay: six fighters, `24` distinct weapon IDs, four weapons per fighter, no missing scripts or broken serialized references.
- Model icons: transparent `512 × 512`; inspected actual-model icon, blue/green team rims and hologram placement. Rendered all eight palette entries; restored shader palette and ambient settings.
- No Play Mode combat verification or automated tests were run. Ability timing, missile interception, banking/firing, deaths and reinforcement placement still require runtime observation.
