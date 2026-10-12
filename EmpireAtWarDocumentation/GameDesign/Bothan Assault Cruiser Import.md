---
type: reference
updated: 2026-10-12
tags:
  - blender
  - unity
  - alo
  - rebellion
---
# Bothan Assault Cruiser Import

## Decision

- Art: Remake 4.0 Workshop `1770851727`, `Bothan_Assault_Cruiser_MP` → `Neutral_Bothan_Cruiser.ALO`.
- Balance: [[GameDesign/Rebel units]], replacing source XML stats. Hull `7,200`; shields `6,500`; speed `22`; price `5,200`; build `58 s`.
- User-approved complement: `1` active X-Wing + `1` active Y-Wing; `1` replacement each. Initial launches consume reserve → serialized reserve `2` per bay.
- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. Ship ID `312`; Coordinated Assault ID `42`; existing Tractor Beam ID `22`.

## Implementation

- Six source meshes, `10,946` triangles, `42` bones retained in FBX. Four visible meshes (`5,334` triangles); source shield/shadow helpers removed from game prefabs.
- Rebuilt binary ALO material slots by source shader/texture; albedo, normal and additive maps resolved from `Data/Art/Textures`. Normal import flips green to match ALAMO.
- `16` weapons: dual heavy turbolaser `3` ×4; medium turbolaser `4` ×4; medium ion cannon `32` ×2; proton torpedo `48` ×4; point defense `14` ×2.
- `6` subsystems: engines ×2; shield ×1; hangars ×2; tractor ×1. All `22` hardpoints are targetable with unique IDs and matching health entries.
- Paired derived mounts: heavy `HP_01/02`, medium `HP_07/08`, torpedo `HP_05/06`, each `Z ±3` units. Ion `HP_03/04`; PD `HP_07/08 + Y3`; engines `HP_Engine + X±6`; hangars `HP_Hangar + X±6`; shield `HP_Shield`; tractor `HP_Shield + (0,−3,10)`.
- Independent hangar destruction/launch bindings; launch positions clear hull by `8` units. Initial/interval `4/8 s` are project choices.
- Coordinated Assault: existing `TorpedoSalvoSettings`, delay ×`0.8` → firing rate +`25%`; duration `8 s`; recovery `40 s`. Tractor Beam uses the existing corvette/frigate implementation.
- Own visual/gameplay/placement/wreck assets, shield mesh, icon/silhouette and matchups. Rebellion roster, `ShipsData`, Addressables, `AssetMappingData`, HUD/tooltip icons and reinforcement lookup registered.
- Red hull paint recolors through Ship Lit HSV livery: hue `0`, range `0.07`, minimum saturation `0.3`, strength `1`; neutral engine texture has livery strength `0`. Wreck materials retain livery.

## Important Values

- Bounds `47.582 × 43.794 × 160` project units; root scale `1`; bow `+Z`; up `+Y`; bank envelope `−21.992 .. 21.897` at `±5°`.
- Provisional: size, navigation radius `90`, level `4`, population `8`, limit `3`, hardpoint health/arcs, matchups, launch timing and shield regeneration `43.333/3 s` (`14.444/s`). Level/population use source XML; other unsupported values follow donor conventions.
- Blender geometry error `0.00004419`, bone error `0.00002369` source units. Unity maximum attachment error `0.00000056` units.
- Verified saved four prefabs, geometry/UVs/material slots/bone hierarchy, balance stats, hardpoints, hangars, references, ability and registrations. Console: `0` captured errors.
- Ten original source hashes unchanged; `31` ship-specific assets have `.meta`. Transparent `512×512` icon bounds `(72,131)..(418,378)`.
- All eight live/wreck palettes differ from palette 0; contrasting blue/green, placement and wreck renders visually inspected.

## Files

- `Tools/Blender/BothanAssaultCruiser/README.md` — commands, mapping, choices, verification limits.
- `Assets/Art/Models/RebellionShips/BothanAssaultCruiser/BothanAssaultCruiser.fbx` — source geometry and helpers.
- `Assets/Prefabs/Models/Ships/BothanAssaultCruiserShipView.prefab`; `Assets/Settings/Data/Ship/BothanAssaultCruiserShipData.asset`.
- `Assets/Prefabs/Ui/Reinforcement/BothanAssaultCruiserReinforcementView.prefab`; `Assets/Prefabs/Models/Wrecks/BothanAssaultCruiserWreckView.prefab`.
- `output/remake-rebel-units/BothanAssaultCruiser-Converted/` — conversion workspace blend, FBX, textures, source audit, mapping, verification and renders. Edit/export the named `BothanAssaultCruiser Source` scene; the saved workspace copy may contain other imported scenes.

## TODO

- No automated tests or Play Mode run. Verify combat/fire arcs, hangar launches/replacements, subsystem destruction, Coordinated Assault/Tractor Beam timing, fog/selection, placement and wreck lifecycle in game.
- EaW ALA animations and particle/shader proxies are not ported.
