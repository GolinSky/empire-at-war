---
type: plan
category: Features
status: done
updated: 2026-11-23
completed: 2026-11-23
acceptance: pending-user
---
# Xyston Import (Empire superlaser glass cannon)

## Goal

- Add the Xyston-class Star Destroyer to the Empire roster: level 5, anti-capital artillery / glass cannon.
- Superlaser must destroy a mid capital ship (full-health ISD I) in one shot.

## Outcome

- `ShipType.Xyston = 220`, new ability `ShipAbilityId.XystonSuperlaser = 58`. Saved-asset audit `Tools/Blender/Xyston/Verify.cs` → ALL CHECKS PASSED; compile clean. Tests written/reviewed, not run (local override). Gates → [[TODOs/User_Acceptance_Queue]].

## Decision (user, 2026-11-23)

| Item | Value |
|---|---|
| Superlaser | `46,800` Beam damage (36k raised +30% on 2026-11-23; now also kills a full ISD II) over `6 s`, recovery `60 s`, range `1000`; frigate+, stations and every `ShipClass.Structure` (user, 2026-11-23); auto-power `Target` (Capital > BattleCruiser > Dreadnought > station = structure); beam colour red `(1, 0.12, 0.08)` via `SuperlaserSettings.beamColor` → `WeaponProfile.WithColor` (LaserBeam profile stays light blue) |
| Hull / shields | `28,000 / 11,000`, class BattleCruiser, shield regen tier `BattleCruiser`, delay `1` (user, 2026-11-23; was Capital tier) |
| Guns | source-like: 6 `DualHeavyTurboLaser`, 6 `HeavyDualLaser`, 5 `DualMediumTurboLaser`, 2 `HeavyDualIonCannon`, 2 `HeavyTripleIonCannon` |
| Hangar | none (source TIE Defender/Scimitar dropped) |
| Abilities | Superlaser + Tractor Beam (source tractor hardpoint) |
| Roster (provisional, not asked) | lvl 5, price 14,000, build 80 s, capacity 15, max 2 |
| Roles | `Fragile | AntiCapital` (proposer gives Fragile; AntiCapital = superlaser, which the gun-only proposer ignores) |

- Length `2,406 m` → `252.63` units (0.105 u/m); saved `141.75 × 74.66 × 252.63`, nav radius 129 (user, 2026-11-23; was ISD I 168).
- Team colour: albedo hue livery (hue `0`, range `0.07`, min sat `0.4`, strength `1`) on `Hull_Armor_Red` only; foredeck stripes removed (user).
- Selection: BattleCruiser → convex `Xyston_PickHull` MeshCollider, root box disabled (`ShipPickHullBaker.NeedsPickHull`; `XystonBuilder.BakePickHull` bakes the Xyston only). Earlier Capital fix (enabled box) superseded.
- Roles unchanged after the class move: proposer gives Fragile for the 4 Empire battlecruisers (Titan, Allegiance, Resurgent, Xyston); Allegiance/Titan/Resurgent proposals match their saved roles.
- Ring: `2abaf7e31` set `ringSize` to 1.15× diameter over all hull renderers (audit ratio 0.986). `BuildXyston.cs` still writes the old 0.920 ring → re-apply that fix after a full rebuild.
- Superlaser SFX: `ShipSfxData.abilities` row cloned from `ExecutorLaserBeam` (spawn threw `Missing ship SFX for XystonSuperlaser`).
- Why 36k: Beam hits shields ×1, Capital hull ×1.5 → raw needed from full: Venator 21k, Interdictor 25.7k, ISD I/Imperator 29.7k, Tector 34k, Mon Cal 36.5k, ISD II 40.7k.
- Avoid: new weapon types; `HardPointType` for the cannon (superlaser is not a destroyable subsystem).

## Important Values

| Ship | Base DPS | vs Capital |
|---|---:|---:|
| Allegiance | 2,501 | 2,473 |
| ISD II | 1,514 | 1,373 |
| ISD I | 852 | 596 |
| Xyston guns | 912 | 601 |
| Xyston superlaser avg | 709 raw (46.8k / 66 s) | burst 7,800/s for 6 s |

- Source (mod `1770851727`): beam `Proj_Xyston_Beam` 60 × 167 = 10,020 per volley, recharge 15–25 s; hull 40,000 / shields 15,000.
- 21 weapons + shield + 2 engines + tractor (ids 0..24); ring ratio 0.920.
- Tests run 2026-11-23 (user-authorized): `Audio` 134/134, `Abilities` 254/254, `Ships` 353/353.

## Implementation

- [x] `Prepare.py` audit + PNGs; `Convert.py` Blender 3.6.23 hull (bone err 6.3e-5 / geo 1.6e-4) and cannon; `Stage.py`.
- [x] `BuildXyston.cs`: ISD I parts (superstructure + 10 turrets) cloned from `ISDI.prefab`; cannon on `HP_Cannon_Attach`; Allegiance view donor (no hangar), ISD I data/wreck/preview/matchups/audio donors; ISD I arcs copied by source hardpoint name; ISD I foredeck team stripes (846 tris) + `Hull_Armor_Red` on worn mask.
- [x] Code: `SuperlaserSettings`/`SuperlaserAbility` (SustainedBeam, muzzle = `Ship.compositeBeamMuzzle` via `ICompositeBeamFacade`), `ShipInstaller` registers the facade for 58, `BalanceValidation` muzzle check, `ShipIconGenerator` mapping.
- [x] Registrations: ShipsData, AssetMappingData, EmpireUnitViews/Data/SharedIcons, EmpireFaction, ShipUiData, TooltipIconData, ReinforcementData (synced), ShipSfxData (ISD I rows), catalog 58.
- [x] Tests: `XystonShipTests` (Ships), `SuperlaserTargetRuleTests` (Abilities), `ShipSystemHardpointTests` + Xyston case.

## Edge Cases

- `HP_Cannon_Attach` is pitched 15°; `Empire_Xyston_SD_Show.ALO` places the barrel level → cancel the bone rotation. Reusable rule → [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].
- Source hull albedo alpha is empty → inverted-alpha team mask would recolor the whole hull; team colour uses stripe overlays.
- Engine glow uses ISD I additive rings (same look as ISD I).
- `HeavyLaserBeam.shader` ignores LineRenderer vertex RGB (alpha only) → the first red superlaser still drew blue. Fixed 2026-11-23: `BeamVisuals.Begin(width, origin, color)` sets `_BodyColor`/`_CoreColor` (peaks 2.2/3.5) and re-hues glows + muzzle light. Colours: Venator-class `LaserBeam` weapon, Imperator `LaserBeam`, Resolute `ProtonBeam` blue `(0.04, 0.32, 1)` (= old material look); Executor `ExecutorLaserBeam` orange `(1, 0.45, 0.05)`; Xyston red. Test: `BeamColorDataTests`, `BeamVisualsTests`.
- Superlaser reuses the `compositeBeamMuzzle` field; rename to a generic beam muzzle only if a third beam ship appears.

## Files

- Tooling: `Tools/Blender/Xyston/` (`Prepare.py`, `Convert.py`, `Stage.py`, `XystonMounts.json`, `*Materials.json`, `BuildXyston.cs` `Build` → `Register`, `Render.cs`, `Verify.cs`).
- Previews: `Temp/XystonImport/Previews/` (ignored; rerun `Render.cs`).
