# Hyena Bomber Import

## Source

- Republic at War 1.5.5, Steam Workshop `1129810972`, `Data/Art/Models/SeV_hyena_bomber.ALO`. Mesh, textures and rig credited to EvilleJedi and Warb_Null. [Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=1129810972)
- ALO SHA-256 `f8c163b00b0dcb4f63eb80f8e7fdeB18e022abaa0a73789a537d838f2f7995e1`.
- Hull and wing textures: `SeV_hyena_bomber.dds`, `SeV_hyena_bomber_wings.dds`.
- `Data/XML/Units_space_cis_hyena.XML` → 6 craft, 40 hull each, 0 shields, bomber armor, speed 3.5 max / 2.5 min, turn rate 1.3, 200 tactical credits and 15 s build time.
- `Hardpoints_CIS_Space.xml` and `Projectiles.xml` → two lasers, each with two 5-damage pulses, 500 range and 0.3–0.8 s recharge. Proton torpedo deals 90 hull damage, range 700, 8 s recharge. Rocket Attack has a 30 s recharge and uses a 25-damage concussion missile.

## Unity

- `SquadronType.HyenaBomber = 106`. Ten craft, 70 hull per craft, 700 total hull, 0 shields. Existing Droid Bomber baseline is 580 total hull.
- 20 laser mounts and 10 torpedo mounts with the existing Droid Bomber weapon profiles. Squadron warhead multiplier 1.2 → 368.64 raw damage per full torpedo volley, up from 307.2. Cruise/combat speed stays at 23.2/26.4.
- CIS production unlocks at level 3 for 400 credits, 10 s build time and 1 population. Stock Lucrehulk, Providence and Invincible bomber bays select Hyena at their existing positions and reserve/active limits of 8/2, 6/2 and 8/2.
- The flight prefab renders 4,264 visible triangles with one mesh renderer and three material slots per craft. It uses the source laser and torpedo attachment positions and excludes collision and muzzle-flash helpers from the rendered mesh.
- Static Blender/FBX comparison preserved material assignments and UVs. Maximum point error 0.000037 source units and normal vector error 0.00249. Unity Editor read back the saved assets, Addressables and carrier bays. Automated tests and live battle playtest pending.
