---
category: Features
status: done
created: 2026-11-20
completed: 2026-11-20
acceptance: pending-user
---
# Chimaera Hero Capital

> Done 2026-11-20. Implemented and source-verified; Unity compile `0` errors. Tests authored, not run (local override). In-game check → [[TODOs/User_Acceptance_Queue]].

## Goal

- Empire hero capital **Chimaera**, built from the ISD II view, data and weapons.
- Own look: partial rust + gold hull weathering (textures only) + Chimaera emblem on the ventral hull.
- Companion: [[Done/Features/Devastator_Hero_Capital|Devastator Hero Capital]].

## References

- Emblem: ![[Done/Features/Chimaera_Hero_Capital_Refs/Chimaera_Emblem.png]]
- Rusty look: ![[Done/Features/Chimaera_Hero_Capital_Refs/Chimaera_Rusty_Ref.webp]]

## Decision

- Base → ISD II (user). `ShipType.Chimaera = 218`.
- Main batteries unchanged: `8× ISDIIOctupleTurbolaser (70)` + `2× ISDIIQuadIonCannon (71)`.
- Added `2× DualMediumTurboLaser (5)` → total `2 → 4`.
- Shield regen "0.9 multiplier" → `ShieldRegenerateDelay 1 → 0.9` (precedent `ResoluteShipData`).
- Hero: `isHero 1`, `MaxCount 1`; existing reservation in `PlayerFactionModel` / `FactionService`.
- Build `70 s`, population `10`, level `5` → kept (user).
- Look → rust + gold, textures only (user). No overlay meshes. `Ship Lit` unchanged.
- Emblem → semi-transparent ornament on a low-poly hull-following grid (user 2026-11-20: no white field, hull shows through, 1.5× bigger, toward the pivot, clear of the hangar, optimized, semi-transparent).
  - Material: `Universal Render Pipeline/Particles/Unlit`, settings copied from `Assets/Art/Materials/Vfx/ShipExplosionSmoke.mat` (alpha blend, `_SURFACE_TYPE_TRANSPARENT`, queue `3000`). Why: reuses a variant already in the build; no new shader variant (Metal compile stalls, see [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]).
  - Texture `Chimaera_EmblemMask.png` `512²`: black RGB, alpha = soft ornament mask `clamp((200 − L) / 120)`; `_BaseColor.a = 0.7`.
  - Why not `Ship Lit`: no transparency or alpha clip. Avoid: URP decal projector (extra pass).
- History: v1 white triangular plate (too small, white field) → v2 opaque per-pixel-run strips (`6,470` tris) → v3 this grid (`804` tris).
- Model → material overrides on the `ChimaeraShipView` variant, not a separate `Chimaera.prefab` model. Why: a variant cannot swap its nested model instance; overrides keep ISD II edits flowing in.

## Important Values

| Stat | ISD II | Chimaera |
|---|---|---|
| Hull | `20,000` | `28,000` (+40%) |
| Shields | `14,000` | `28,000` (+100%) |
| `ShieldRegenerateDelay` | `1` | `0.9` |
| Price | `8,000` | `12,000` (×1.5) |
| `MaxCount` | `3` | `1` |

- Loadout: `4×6, 5×4, 23×4, 32×6, 70×8, 71×2` — matches `WeaponLoadoutBaker.DeriveLoadout(view)`.
- New hardpoints: `HP_Chimaera_DTL_01/02`, ids `32/33`, local `(∓22.99, -12.24, 15)`, yaw copied from `HP_ISD_New_TL_01/02`.
- Weathering per tile (gold / rust coverage): heavy `0.16/0.20` (HighContrast, Kitbash_Pieces, Trench), medium `0.09/0.12` (Trench_Inset, Bridge), light `0.035/0.06` (Hull_16, Hull_2_SD, both Superstructure_Base).
- Metallic/smoothness map (materials `_Metallic = _Smoothness = 1`): plate `0.10/0.25` (= old scalars), gold `0.40/0.45`, rust `0.05/0.12`.
- Gold metallic first `0.9` → rendered dark brown (reflects black space). Lowered to `0.4` so albedo carries the gold.
- Emblem: scale `0.194` units/px (old plate ×1.5); ornament `57.3 × 60.6` units, `z -7 → -67.6`, centred `x 0`. Hangar ends at `z -4.6` → `2.4` clearance.
- Grid: columns ≤ `3` units with one on the keel `x = 0`, rows ≤ `2` units; `402` of `620` cells kept (cells with no ornament pixels dropped); `497` verts / `804` tris; each vertex `0.25` below the lowest hull hit (all hull renderers, back faces on), `0` misses, faces down. White vertex colours (particle shader multiplies them). Shadow casting off.
- Wreck: `ShipWreckBuilder` copies only `Ship Lit` renderers → the wreck has no emblem; orphan `Chimaera_Emblem_Wreck.mat` deleted.
- Gotcha: `EditorUtility.CopySerialized` onto a mesh asset leaves the old GPU buffer in the Editor; `Resources.UnloadAsset` + `ImportAsset(ForceUpdate)` after saving.
- `39` renderers swapped to `9` Chimaera materials; `Hull_1` (turrets), hangar, lights, team stripes stay ISD II.

## Implementation

- [x] `ShipType.Chimaera = 218`; `ShipIconGenerator.MAPPINGS` entry.
- [x] `ChimaeraShipData`, `ChimaeraMatchups` from ISD II; stats and loadout above.
- [x] `ChimaeraShipView` = variant of `ISDIIShipView`; 2 hardpoints registered in `HealthComponent.ShipUnits`, `EntityBindings.Injectables`, `WeaponComponent.hardPoints`.
- [x] Textures via `Tools/Textures/generate_chimaera_weathering.py` (periodic noise → seamless tiles).
- [x] Emblem rebuilt ornament-only, 1.5×, moved forward to `z -7` (white emblem texture deleted); then optimized to an `804`-tri semi-transparent grid.
- [x] Emblem mesh in `FogVisibilityComponent.renderers` + `UnitRenderStateComponent.meshRenderers`.
- [x] Own wreck: `ShipWreckBuilder.Build` → `ChimaeraWreckView` + `ChimaeraWreckData` (rust stays on the wreck).
- [x] `ChimaeraReinforcementView` = variant of `ISDIIReinforcementView`.
- [x] Registries: `EmpireFaction` (key `218`), `ShipsData`, `ReinforcementData`, `ShipUiData`, `TooltipIconData`, `AssetMappingData`, `ShipSfxData` (`ships`, `shipVoices` cloned from ISD II), Addressables (`EmpireUnitViews`, `Data`, wreck in `View`).
- [x] Icon `Empire/ChimaeraIcon.png` (generator run filtered to the two heroes; other icons untouched).
- [x] Preview-scene renders: 3/4, underside, low side. Emblem readable, bow-pointing; gold reads as gold after metallic fix.
- [x] Persisted (Refresh → ForceReserialize changed paths → SaveAssets); `0` console errors.
- [x] Tests: new `EmpireHeroCapitalTests` (`Ships`); `WeaponLoadoutBakeTests.ShipLoadouts_MatchViewPrefabs` covers the data. Not run.
- [ ] User: in-game check (see queue).

## Edge Cases

- Bridge domes: not recoloured gold/white — the domes have no separate material; Bridge tile weathering only.
- Wreck has no emblem (transparent material is not `Ship Lit`).
- Ring interior below the serpents' crossing is open in the source art → bare hull shows there by design.
- ISD II assets unchanged; later ISD II prefab edits flow into the variant.
- Preview lighting overexposes both ISD II and Chimaera equally; judge colours in game.

## Files

- `Assets/Prefabs/Models/Ships/Empire/View/ChimaeraShipView.prefab`, `Assets/Prefabs/Ui/Reinforcement/ChimaeraReinforcementView.prefab`, `Assets/Prefabs/Models/Wrecks/ChimaeraWreckView.prefab`
- `Assets/Settings/Data/Ship/Empire/ChimaeraShipData.asset`, `Assets/Settings/Data/Models/Wrecks/Empire/ChimaeraWreckData.asset`, `Assets/Settings/Data/Tooltip/Matchups/ChimaeraMatchups.asset`
- `Assets/Art/Textures/Models/EmpireShips/Chimaera/`, `Assets/Art/Materials/Models/EmpireShips/Chimaera/`, `Assets/Art/Models/EmpireShips/Chimaera/Chimaera_EmblemPlate.asset`
- `Tools/Textures/generate_chimaera_weathering.py`
- `Assets/Scripts/Tests/Editor/Ships/EmpireHeroCapitalTests.cs`

## Reusable Guidance

- `ShipSfxData` registration row added to [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]] step 6 (was missing; fallback sounds hid it).
- Metallic patches in space scenes: keep metallic ≲ `0.4` for coloured metal, else it renders dark — recorded here only (single case).
