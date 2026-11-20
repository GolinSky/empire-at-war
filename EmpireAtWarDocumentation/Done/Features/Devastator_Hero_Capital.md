---
category: Features
status: done
created: 2026-11-20
completed: 2026-11-20
acceptance: pending-user
---
# Devastator Hero Capital

> Done 2026-11-20. Implemented and source-verified; Unity compile `0` errors. Tests authored, not run (local override). In-game check → [[TODOs/User_Acceptance_Queue]].

## Goal

- Empire hero capital **Darth Vader — Devastator**, built from the ISD II view, data and weapons. Visuals identical to ISD II.
- Companion: [[Done/Features/Chimaera_Hero_Capital|Chimaera Hero Capital]].

## Decision

- `ShipType.Devastator = 219`.
- Main batteries: 8 octuple slots → `6× HeavyDualTurbolaser (43)` "Imperial Heavy Dual Turbolaser" + `2× HeavyDualIonCannon (91)` (user chose `43` over `3`).
- Ion pair = `HP_ISD_New_OTL_02` (+x) and `HP_ISD_New_OTL_07` (−x), mirrored at `z -45.54`. All 8 keep `mainBattery = true` and their yaw arcs.
- `2× ISDIIQuadIonCannon (71)` kept.
- Added `2× MediumIonCannon (32)` → total `6 → 8`.
- Shield regen delay `1 → 0.9`; hero `isHero 1`, `MaxCount 1`.
- Coexists with `Darth Vader - TIE Advanced` (user). No exclusivity code.
- Build `70 s`, population `10`, level `5` → kept (user).
- Wreck → reuses `ISDIIWreckData` (same look).

## Important Values

| Stat | ISD II | Devastator |
|---|---|---|
| Hull | `20,000` | `28,000` (+40%) |
| Shields | `14,000` | `28,000` (+100%) |
| `ShieldRegenerateDelay` | `1` | `0.9` |
| Price | `8,000` | `12,000` (×1.5) |
| `MaxCount` | `3` | `1` |

- Loadout: `4×6, 5×2, 23×4, 32×8, 43×6, 71×2, 91×2` — matches `WeaponLoadoutBaker.DeriveLoadout(view)`.
- New hardpoints: `HP_Devastator_IC_01/02`, ids `32/33`, local `(∓41.79, -10.89, -48)`, yaw copied from `HP_ISD_IC_05/06`.
- Octuple turret meshes stay on the retyped mains (no art change requested).

## Implementation

- [x] `ShipType.Devastator = 219`; `ShipIconGenerator.MAPPINGS` entry.
- [x] `DevastatorShipData`, `DevastatorMatchups` from ISD II.
- [x] `DevastatorShipView` = variant of `ISDIIShipView`; mains retyped; 2 medium ions registered in health, entity bindings and weapon component.
- [x] `DevastatorReinforcementView` = variant of `ISDIIReinforcementView`.
- [x] Registries: `EmpireFaction` (key `219`), `ShipsData`, `ReinforcementData`, `ShipUiData`, `TooltipIconData`, `AssetMappingData`, `ShipSfxData`, Addressables.
- [x] Icon `Empire/DevastatorIcon.png`.
- [x] Persisted; `0` console errors.
- [x] Tests: `EmpireHeroCapitalTests` (`Ships`) incl. mirrored ion pair and +2 medium ions. Not run.
- [ ] User: in-game check (see queue).

## Edge Cases

- Ion-heavy loadout (2 heavy dual ion + 2 quad ion + 8 medium ion): AI combat profile derives from the loadout automatically.
- Weapon ranges of `43` / `91` differ from octuples; ship `Range 750` unchanged.

## Files

- `Assets/Prefabs/Models/Ships/Empire/View/DevastatorShipView.prefab`, `Assets/Prefabs/Ui/Reinforcement/DevastatorReinforcementView.prefab`
- `Assets/Settings/Data/Ship/Empire/DevastatorShipData.asset`, `Assets/Settings/Data/Tooltip/Matchups/DevastatorMatchups.asset`
- `Assets/Scripts/Tests/Editor/Ships/EmpireHeroCapitalTests.cs`

## Reusable Guidance

- See [[Done/Features/Chimaera_Hero_Capital]] (shared work; `ShipSfxData` row added to the import guide).
