---
category: Features
status: in-progress
updated: 2026-10-03
---
# Malevolence Import

## Goal
- Import the intact Malevolence ALO through Blender → FBX → registered Separatist ship entity.
- Preserve the supplied Republic at War specification; separate authored/source values from provisional project balance.

## Implementation
- [x] Blender 3.6.23 conversion; six DDS textures resolved explicitly; original files unchanged.
- [x] Visual/gameplay/wreck/placement prefabs, model icon, owned team colors, ship data, faction roster and Addressables registrations saved.
- [x] 73 health hardpoints; 62 conventional weapons; IDs match health-array order.
- [x] Mass Driver profile uses the existing shield-piercing path → hull/hardpoint damage.
- [x] Ion Pulse model/view/presenter: charge → align surviving side cannon → travelling area wave → timed disable; malfunction disables caster instead.
- [x] Unity compilation and saved-reference inspection clean; all 126 bone positions/parents verified. No automated tests or Play Mode run.
- [ ] Configure exact Vulture/bomber launches when their squadron assets and bomber identity are available.
- [ ] Runtime acceptance: placement, navigation/banking, fire arcs, hull/shields, Ion Pulse, UI disable, death/wreck and teardown.

## Important Values
- User-supplied source target: hull **33,420**; shields **15,000**; recharge **100**; build limit **1**; population **5**.
- Armament: **34 turbolasers + 20 lasers + 8 Mass Drivers**; each side **17 + 10 + 4**. Special weapons: **1 ion cannon per side**.
- Systems: **1 shield generator + 3 engines + 4 hangars + 1 supply dock**.
- Source complement: **48 Vultures + 26 bombers**. No substitute approved; `hangarBays` empty and unbound `HangarComponent` removed.
- Source armor: Super Star Destroyer. Current project mapping: `ShipClass.HeavyCapital`; no separate SSD armor class exists.
- Registration: `ShipType.Malevolence = 104`; `WeaponType.MassDriver = 19`; `DamageType.MassDriver = 11`; `ShipAbilityId.IonPulse = 8`.
- New hardpoint types: `SupplyDock = 5`, `IonPulseCannon = 6`. Supply dock is a destroyable structural target; resupply behavior is absent.

## Decision
- Provisional scale: **89.234 × 54.488 × 320** units; gameplay root scale **1**; navigation radius **180**; bank **±10°**; vertical range **±34.578**.
- Provisional movement/economy: speed **4**, yaw/acceleration **3**; price **20,000**, level **5**, build time **90 s**; existing height tier **2 / −118**.
- Provisional hardpoint HP: weapon **600**, engine **1,200**, shield **1,800**, hangar **900**, supply **1,000**, ion **2,200**.
- Provisional Mass Driver: **35** configured damage, **3 shots**, interval **0.5 s**, reload **5 s**, range **140**, speed **90**; existing turbolaser class multipliers/accuracy, full shield bypass. Shared projectile/audio assets.
- Provisional Ion Pulse: charge **4 s**, alignment timeout **45 s**, tolerance **5°**, range **240**, speed **35**, radius **60**, thickness **8**, disable **12 s**, malfunction **10%**, recovery **60 s**. Lifecycle ceiling **80 s**; tooltip displays the **12 s** effect.
- Disable freezes movement/turning, stops weapon emissions and non-phased active abilities, and blocks new abilities. Already-launched shots continue. Overlapping pulse disables retain independent lifetimes; planetary-ion state remains separate.
- Chosen source: intact `CIS_Malevolence.ALO`. `CIS_Malevolence_D.ALO` and `CIS_Malevolence_D_DIE_00.ala` remain untouched; use the project's wreck system.

## Rules
### Attachment mapping
- Blender source **126** bones includes identity `Root`; importer deleted it → restored after checking the binary header.
- Port turbolasers: interpolate `TurboMR01..10` to **17** anchors; starboard: `TurboMR11..20` to **17**. Original bones remain in the visual asset.
- Port lasers: `LaserR01/04/05/08/09/12`; starboard: `LaserR02/03/06/07/10/11`; interpolate each sequence to **10** anchors.
- Mass Drivers: **4** points per side along the corresponding turbolaser attachment sequence, **2 units lower**. These supplemental anchors are project choices.
- Engines: `Engines_00..02`; shield: `Shield_00`; ion port: `Especial02`, starboard: `Especial01`.
- Hangars: two anchors at each `Spawn_00/01`, **±6 units longitudinally**. Supply dock: spawn midpoint offset **(0, −5, −15)**; provisional position.

## Edge Cases
- Upstream importer scans objects across every scene → unrelated collision objects caused an import error. Saved session backup, then used an empty Blender file; upstream add-on unchanged.
- Blender round trip: **15 meshes / 19,407 triangles / 126 bones**; maximum bone displacement **0.00008178 source units**.
- Unity: **15 meshes / 19,379 triangles / 126 bones**; **8 visible meshes / 15,914 visible triangles**; all meshes have UVs; maximum bone-position error **0.000001801 units** before gameplay scaling.
- Unity dropped **28 degenerate triangles** from hidden `Motor` / `Motor_SML` helpers. Visible geometry unchanged; collision/shadow/engine helpers retained with renderers disabled.
- Livery: hull/engine materials and matching wreck materials use hue **0.63**, range **0.08**, minimum saturation **0.25**, strength **1**. All eight palettes rendered; blue/green visually inspected.
- Move/Stop cancels charging/alignment. Destroyed firing cannon, lost target, ion disable, range loss or alignment timeout cancels before firing. A launched wave and its disables survive caster death; scene teardown cleans them up.
- Source stats are supplied by the user with [Republic at War Subjugator](https://republicatwar.wiki.gg/wiki/Subjugator); web fetch failed, so no independent live-wiki verification is claimed.

## Files
- Source: `C:/Users/golin/Documents/CIS_Hero_Units_Pack_2014/Malevolence/`.
- Editable packed Blender file, FBX, PNGs and reports: `C:/Users/golin/Documents/CIS_Hero_Units_Pack_2014/Malevolence-Converted/`.
- Conversion script: `Tools/Blender/export_malevolence.py`; import reference: [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/SeparatistShips/Malevolence/`.
- Prefabs: `Assets/Prefabs/Models/Ships/Malevolence{,ShipView}.prefab`; `Assets/Prefabs/Models/Wrecks/MalevolenceWreckView.prefab`; `Assets/Prefabs/Ui/Reinforcement/MalevolenceReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/MalevolenceShipData.asset`; `Assets/Settings/Data/Ship/Wreck/MalevolenceWreckData.asset`; `Assets/Settings/Data/Tooltip/Matchups/MalevolenceMatchups.asset`.
- Ability: `Assets/Scripts/Services/ShipAbilities/Abilities/IonPulse/`; view: `Assets/Prefabs/Vfx/IonPulseWave.prefab`; verification: `Temp/MalevolenceImport/{report,verification,audit}.json`.
- Pack credits: model/rig **Nomada_Firefox**, textures **Nawrocki**; pack README requests contacting its author before public-mod use.
