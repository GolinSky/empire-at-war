---
type: reference
updated: 2026-10-05
tags:
  - unity
  - alo
  - rebellion
  - nebulon-b
---
# Nebulon-B Import

## Goal

- Add vanilla Empire at War `RV_NEBULONB.ALO` to the Rebellion roster with the supplied stats and five hardpoints.
- Follow [[Architecture/ALO_MODEL_IMPORT_GUIDE]].

## Important Values

| Property | Value | Basis |
| --- | ---: | --- |
| Ship identifier | `ShipType.NebulonB = 300` | Project registration |
| Faction | `FactionType.Rebellion` | User / vanilla XML |
| Class / role | Frigate / anti-corvette | User / vanilla XML |
| Hull | 3,600 | User / vanilla XML |
| Shields | 700 | User / vanilla XML |
| Shield recovery | 50 per 1 s | User rate; project interval |
| Speed | 2.2 | User value, stored directly |
| Cost / build time / population | 2,200 / 30 s / 3 | User / vanilla XML |
| Available level / maximum count | 2 / 10 | Project roster settings |
| Visible size | 14.231 × 43.116 × 70.000 units | Provisional project size |
| Navigation radius | 36 units | Measured hull |
| Bank / vertical hull range | ±5° / -21.558 .. 21.892 units | Source bank; measured hull |
| Height tier | `LowMid` | Project placement choice |
| Hardpoint health | 260 each | Vanilla XML |

## Decision

- Ability: `ShipAbilityId.NebulonBBoostShieldStrength = 13`; reuse `BoostShieldPowerSettings` and the existing ability implementation.
- Active 15 s → 60 s recovery; regeneration ×10 = 500/s; movement ×0.8; damage ×0.5; fire delay ×1; damage taken ×1.
- Why: vanilla XML specifies 15 s, 60 s, 20% slower movement and shield-recharge interval ×0.1. The project's existing modifier supports regeneration amount, so ×10 preserves average recovery.
- Weapon damage -50% is a provisional project choice matching the user's requested firepower tradeoff. Vanilla XML has weapon-delay ×1 and no direct damage penalty.
- Avoid changing the shared `BoostShieldPower` definition; other ships retain their settings.
- No hangar, shield-generator hardpoint or additional weapon batteries.

## Implementation

### Hardpoints

| ID | Weapon/system | Imported attachment | Yaw arc |
| --- | --- | --- | --- |
| 0 | Front-left turbolaser | `FP_F-L_00` | -177.5° .. -2.5° |
| 1 | Front-right turbolaser | `FP_F-R_00` | 2.5° .. 177.5° |
| 2 | Back-left laser | `FP_B-L_00` | -177.5° .. -2.5° |
| 3 | Back-right laser | `FP_B-R_00` | 2.5° .. 177.5° |
| 4 | Engines | `HP_E_Bone` | — |

- One gameplay battery per XML hardpoint; use one authored muzzle per battery and existing `TurboLaser` / `Laser` profiles.
- Existing profiles supply projectile, salvo, damage, targeting and range behavior; vanilla weapon timing/accuracy is not recreated.
- Root scale `1`; bow `+Z`; four weapons and five health/fog targets under the banking pivot.
- Refit collision, baked hull shield, ion bounds, selection and engine effects to the 70-unit hull.
- Dedicated hologram placement prefab: two explicit renderer bindings, kinematic Rigidbody, no gravity.
- Wreck uses the visible opaque hull with dedicated wreck material/data. Icon and silhouette rendered at 512 × 512.
- Hull team mask = inverted source albedo alpha. Owned-color renders verified across all eight palette entries; neutral surfaces retained.
- Registered `ShipsData`, `AssetMappingData`, existing Addressables `View`/`Data` groups, Rebellion roster, `ShipUiData`, `TooltipIconData`, `ReinforcementData`, matchups and `ShipIconGenerator.MAPPINGS`.
- Dedicated ability audio and existing Rebellion voice set resolve.

### Conversion and verification

- Blender 3.6.23; ALAMO importer; Blender MCP protocol 13 with safe mode enabled.
- Source has three light objects before its meshes. ALAMO skips the lights but indexes mesh bindings as if they were retained → mesh attachments shift by three.
- Repair connections from binary `0x602` object/bone records; restore all source parents. The partial import retained the actual identity `Root`; no synthetic bone added.
- ALAMO helper cleanup scans all loaded Blender scenes and failed on an earlier scene's collision mesh. Finish helper setup only on the Nebulon-B source scene; isolate copied materials.
- Keep all ten meshes and 39 bones. Healthy visuals enable `RV_NebulonB` and `Lighting`; collision, shadow, shield, engine proxy and four damage-decal meshes remain disabled.
- Blender FBX round trip: maximum geometry displacement 0.00004184 source units; bone displacement 0.00005502.
- Unity import: 10 meshes, 9,104 triangles, 39 bones; no parent mismatches; bone displacement ≤0.000001073 import units.
- All mesh positions compared with corrected source: maximum corner displacement 0.0000007794 import units. Visible/data mesh UVs preserved; disabled shadow-helper UVs excluded from the strict UV comparison.
- Reloaded all four saved prefabs: no broken object references or donor-ship dependencies; matching geometry sizes; unique hardpoint IDs 0–4.
- Loaded faction catalog resolves price 2,200; all ship/data/view/placement registrations occur exactly once. Ability settings, audio and team-renderer ownership verified.
- Unity compilation completed without errors; no new Console import/serialization errors. Original ALO/DDS SHA-256 hashes unchanged.
- No Play Mode combat or automated tests run. The user's dirty `MainMenuScene` remained open and unsaved.

## Files

- Source: `output/eaw-rebel-ships/DATA/ART/MODELS/RV_NEBULONB.ALO`; textures beside `DATA/ART/TEXTURES`.
- Vanilla data: `Temp/VictoryImport/BaseGame/DATA/XML/SPACEUNITSFRIGATES.XML` (`Nebulon_B_Frigate`) and `HARDPOINTS.XML` (`HP_Nebulon_*`).
- Art: `Assets/Art/{Models,Materials/Models,Textures/Models}/RebellionShips/NebulonB/`.
- Prefabs: `Assets/Prefabs/Models/Ships/NebulonB.prefab`, `NebulonBShipView.prefab`; `Assets/Prefabs/Models/Wrecks/NebulonBWreckView.prefab`; `Assets/Prefabs/Ui/Reinforcement/NebulonBReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/NebulonBShipData.asset`, `Ship/Wreck/NebulonBWreckData.asset`, `Tooltip/Matchups/NebulonBMatchups.asset`.
- Conversion artifacts and evidence: `Temp/NebulonBImport/` (`Output/NebulonB.blend`, FBX, audits, geometry comparisons and palette renders).

## Edge Cases

- Vanilla cinematic/death ALA files and dedicated damaged/hulk variants are present but not converted. Runtime wrecks use the project's existing wreck system.
- EaW shader particles, dynamic scorch decals and source light objects are not recreated.
- Speed 2.2 is stored directly; gameplay scale, turn-rate suitability and battle balance remain unverified.
