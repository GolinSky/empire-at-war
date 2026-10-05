---
type: reference
updated: 2026-10-05
tags:
  - ywing
  - squadron
  - alo
  - asset-pipeline
---
# Y-Wing Import

## Goal

- Five Republic BTL-B heavy bombers / anti-capital strikecraft, imported through [[Architecture/ALO_MODEL_IMPORT_GUIDE]].
- Runtime and balance acceptance: [[TODOs/Features/YWing_Import]].

## Important Values

| Value | Implemented |
| --- | ---: |
| `SquadronType.YWing` / `ShipAbilityId.IonShot` | 3 / 9 |
| Craft / weapons | 5 / 15 |
| Hull / shields per craft | 60 / 30 |
| Shield regeneration | 3 per surviving craft every 1 s |
| Passive hull repair | 1 per surviving craft per second |
| Ion disable / recovery / range | 3 s / 20 s / 100 units |
| Ion visual speed | 35 units/s |
| Cruise / combat speed | 22 / 25 units/s |
| Acceleration / turn / banking | 16 units/s² / 60°/s / 35° |
| Member size | 1.787514 × 0.529589 × 4 units |
| Formation spacing / navigation radius | 4 / 15 units |
| Formation radius including member colliders | 12.445 units |
| Member collider / selection diameter | 2.2 / 30 units |
| Tech / cost / build / population / limit | 2 / 500 / 8 s / 1 / 10 |

## Rules

- User supplies craft count, per-craft hull/shields/regeneration, weapon composition, Ion Shot and passive astromech repair.
- Repair rate, Ion disable/recovery/range, economy, weapon damage, dimensions and flight tuning are provisional project choices.
- Y-Wing 22/25 → slower than current NTB-630 24/27; do not infer canonical speed units from source.
- User values override local hero Shadow Squadron values. Local generic Y-Wing XML has three craft and a different model; do not copy that squadron count.
- Other squadron data defaults to zero passive repair and empty abilities. No global weapon-profile changes.

## Implementation

- Per member: `MuzzleA_00/01` → two `FighterLaser` mounts; `MuzzleB_00` → one `ProtonTorpedo` launcher. Fifteen unique IDs `0..14`.
- Existing proton profile: two shots, 0.4 s spacing, 12 s reload, 75-unit range, 90 damage each. `FighterTorpedoHardPoint` permits one salvo per attack pass; reload can skip a pass.
- `SquadronAbilityFacade` exposes data-defined slots through existing `IShipAbilityFacade`; current ability UI/AI use that contract.
- Ion Shot copies `ProtonTorpedoShot.prefab`; dedicated white head/trail materials and particle colors leave normal torpedoes unchanged.
- Each surviving craft launches one white visual projectile. Latest arrival → target speed multiplier 0 plus ion-disable token → 3 s → remove this ability's tokens → 20 s recovery. No raw hull/shield damage.
- Launched Ion Shot survives caster death; target destruction or service disposal releases any applied modifiers. Existing token counting preserves overlapping disables.
- `SquadronHealthComponent` applies per-second repair through `SquadronHealthModel.RepairHull` and `HardPointModel.Repair`. Clamp to maximum; never revive destroyed members.
- Health/flight references: five members; weapon/fog hardpoint references: fifteen; team hull renderers: five. Existing member explosion/removal death behavior retained.
- Own five-member hologram placement, neutral model icon, HUD silhouette, matchups, Republic roster, asset mappings and existing Addressables `View`/`Data` entries.

## Decision

- Static source pose retained; original EaW shader/proxy effects are represented by existing Unity trails/projectiles.
- Hull alpha → separate linear `_TeamMaskMap`, strength 1; HSV recoloring disabled. Normal green channel flipped.
- Restored identity `Root` only after inspecting binary source. Duplicate engine bone name `PE_Ywing_Proto` → second occurrence `PE_Ywing_Proto.001`; positions retained.
- No capital-ship wreck asset; individual fighters already have destruction effects.
- Full supplied RaW credits retained; no identifiable Y-Wing-specific author line. Individual model/texture/rig attribution remains unresolved before redistribution.

## Verification

- Original ALO plus three DDS SHA-256 values unchanged; converted PNGs match decoded source pixels. Hull/normal 1024 × 1024; flash 64 × 64.
- Source: four meshes, 2,846 triangles, thirteen bones. Blender FBX/Unity: four meshes, 2,814 triangles; hull retains all 2,786.
- Two disabled flash helpers: 24 → 8 triangles each by duplicate-face collapse; unique position/UV geometry unchanged. Collision retains twelve triangles.
- Maximum Blender bone/corner displacement: 0.000018061 / 0.000027080 source units. Unity bone displacement: 0.000000426956 units; all thirteen bones/parents and UVs verified.
- Saved gameplay and placement bounds match: 17.787514 × 1.489589 × 10.4 units. Roots scale 1, bow +Z/up +Y; colliders and selection fit the formation.
- No missing scripts, broken references or donor dependencies. Registries resolve to saved Y-Wing assets; icon consumers share the correct sprite.
- Eight team palettes rendered; blue/green inspected with neutral hull details retained. White Ion Shot head/trail inspected in isolated Editor render.
- Unity import/compilation passed. Existing camera-member and empty third-party assembly warnings remain unrelated.
- MainMenu scene remained clean. No automated tests or Play Mode run; runtime behavior remains unverified.

## Files

- `Assets/Prefabs/Models/Squadrons/YWing.prefab`, `YWingSquadronView.prefab`.
- `Assets/Settings/Data/Squadron/YWingSquadronData.asset`; `Assets/Prefabs/Ui/Reinforcement/YWingReinforcementView.prefab`.
- Art: `RepublicShips/YWing` under type-first model/material/texture folders; model folder retains `SOURCE_CREDITS.txt` and full RaW credits.
- `Assets/Prefabs/Vfx/IonShot.prefab`; `Assets/Art/Materials/Vfx/IonShotHead.mat`, `IonShotTrail.mat`.
- `Assets/Art/Textures/Ui/Icons/SquadronIcon/YWingIcon.png`, `YWingSilhouette.png`.
- Source `F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art/Models/ReV_ywing.ALO`; sibling `ReV_ywing-Converted/` has packed blend, FBX, textures, scripts, reports, credits and previews.
- `Tools/Blender/prepare_ywing_textures.py`, `export_ywing.py`, `README.md`; staging evidence `Temp/YWingImport/`.

## TODO

- Clean-skirmish acceptance: movement/formation, laser and two-torpedo passes, Ion Shot target/hit/overlap/death/cooldown, passive repair, shields, fog, selection, placement and member death.
- Review provisional ability, repair, economy and weapon/flight balance.
