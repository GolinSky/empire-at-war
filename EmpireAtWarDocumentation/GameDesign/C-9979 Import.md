---
type: reference
updated: 2026-10-05
tags:
  - c9979
  - alo
  - cis
  - asset-pipeline
---
# C-9979 Lander Import

## Goal

- Static space/skirmish transport from RaW 1.2.1 `SeV_c9979.ALO`; [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Remaining acceptance: [[TODOs/Features/C9979_Import]].

## Rules

- Original ALO and three DDS files unchanged; lossless PNGs. Credits: Evillejedi mesh/textures, z3r0x rigging (`Republic_At_War/credits.txt`).
- Local `Data/XML/Units_Space_cis_c9979.xml` confirms hull `300`, shields `20`, refresh `2`, source speed/turn/thrust `1.5/0.5/0.8`, Transport armor and Corvette space layer.
- `Affiliation=Rebel` is an EaW faction slot; project registration is CIS as requested. Linked wiki could not be fetched (HTTP 403); local XML supplies combat evidence.
- XML weapon: `Proj_Ship_Medium_Laser_Cannon_Green`; one pulse, recharge `5 s`, range `800`. No hardpoint list, abilities or hangar.
- Land variant and its Land/Takeoff ALA files are separate; this space import does not convert them.

## Implementation

- Blender `3.6.23`, live MCP protocol `13`; ALAMO imported seven bones. Restore only the binary-verified identity `Root`: eight source/export/Unity bones.
- Three meshes / `4,182` triangles: `Hull=4,122`, `lights=36`, hidden `Collision=24`. FBX preserves UVs and authored visibility; Unity welds vertices without changing triangles.
- Maximum Blender bone/geometry displacement: `0.000002568 / 0.000003053` source units. Maximum Unity bone error: `0.000000090946` project units; parents match.
- FBX import scale `0.02`; source → Unity coordinates `(-x,z,-y)`. Visual/gameplay/placement/wreck roots scale `1`; centered dimensions `32 × 5.138031 × 12.09517`, bow `+Z`, up `+Y`.
- Hull uses `EmpireAtWar/Ship Lit`, linear source-alpha team mask, data normal map with green flip. Lights use additive URP material; collision renderer disabled. Eight team palettes rendered; blue/green inspected.
- `ShipType.C9979=107`; `WeaponType.MediumLaser=23`. One mount at the midpoint of `MuzzleA_00..03`; all four original attachments retained. Full yaw; one green laser per `5 s`; zero destructible weapon/engine/shield hardpoints.
- `HullTarget` supplies the whole-hull aim point. `HealthComponent` keeps the UI subsystem list empty; `HealthModel` supports direct hull damage after shield handling and reports living hull-only units as targetable.
- Saved data/view, CIS roster, existing View/Data Addressables, placement, HUD/tooltip icons, matchups, weapon audio and icon-generator mapping registered. No donor model references or broken prefab references.

## Important Values

| Value | Configured | Provenance |
| --- | ---: | --- |
| Hull / shields | `300 / 20` | User + local XML |
| Shield regeneration | `2` each `1 s` | Source rate; project interval |
| Laser damage / shots / reload | `6 / 1 / 5 s` | Existing Laser damage provisional; source count/cooldown |
| Range / radar | `100 / 112.5` units | Source `800 / 900` scaled by provisional `1/8` |
| Width / height / length | `32 / 5.138031 / 12.09517` | Authorized provisional size |
| Speed / turn / acceleration | `48 / 45°/s / 36` | Authorized provisional corvette tuning |
| Bank / flight Y | `15° / 80` | Source bank; existing highest tier + offset |
| Navigation radius | `17` | Measured planar radius `16.33902` |
| Banked hull Y | `−3.298028 .. 5.625829` | Measured ±15° |
| Selection diameter | `36` | Fitted to hull |
| Level / cost / build / population / limit | `1 / 500 / 10 s / 1 / 20` | Authorized provisional economy |

## Edge Cases

- Project has no Transport armor profile; uses existing Corvette `ShipClass=2` damage category. Exact RaW armor parity remains a separate balance limitation.
- Weapon mounts remain active until hull destruction or ion disable; no separately targetable subsystems are invented.
- Wreck uses only opaque `Hull`; lights/collision remain disabled. Placement uses the same geometry/bounds with hologram material.
- Saved inspections and current Unity compilation passed; final import checks had no new asset/serialization errors. Other concurrent workflow/test errors remain in Console history.
- This task ran no automated tests or Play Mode; combat, shield regeneration, placement, fog/selection and destruction runtime acceptance remain unverified.

## Files

- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/SeparatistShips/C9979/`; shield `Assets/Art/Models/Shields/C9979ShipViewShield.asset`.
- Prefabs: `Assets/Prefabs/Models/Ships/C9979.prefab`, `C9979ShipView.prefab`; `Assets/Prefabs/Models/Wrecks/C9979WreckView.prefab`; `Assets/Prefabs/Ui/Reinforcement/C9979ReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/C9979ShipData.asset`, `Ship/Wreck/C9979WreckData.asset`, `Tooltip/Matchups/C9979Matchups.asset`.
- Icons: `Assets/Art/Textures/Ui/Icons/ShipIcon/C9979Icon.png`, `C9979Silhouette.png`.
- Conversion: `Tools/Blender/prepare_c9979_textures.py`, `export_c9979.py`; evidence `Temp/C9979Import/Output/`.
- Packed source/output: sibling `Data/Art/Models/SeV_c9979-Converted/`.
