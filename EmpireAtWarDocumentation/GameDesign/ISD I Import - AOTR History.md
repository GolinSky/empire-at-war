---
category: Features
type: history
updated: 2026-10-09
status: obsolete
---
# ISD I Import — AOTR History

- Historical reference: superseded ISD I visuals and original balance values. Current integration: [[GameDesign/ISD I Import]]. Original fighter-import notes remain historical evidence.
- Obsolete visual archive removed on 2026-10-10; original archive recoverable from commit `327da32c`. Shared source assets needed by Tector moved to its art folders with original GUIDs and visual content.

## Decision

- Add `E_Imperial_Star_Destroyer_1_Fighters` to Empire as `ShipType.ISDI = 201`.
- Model map → `Empire_Imperial_SD.ALO`; standard/advanced share this hull. Attach ISD-I parts, 6 TLD, 2 ICD and 3 triple turrets.
- Preserve requested 20,000 hull / 16,000 shields / speed 250. Source displays 250 but stores `Max_Speed=2.5`; project data uses literal 250 units/s.
- Source has 3 engines → expose only left/right as targetable, matching the request. Keep all visible engine geometry.

## Implementation

- Targetable hardpoints: 6 heavy 2-burst turbolasers; 2 heavy 2-burst turbo-ion; 2 medium turbo-ion; 2 shields; 2 engines; tractor; hangar. IDs 0..15.
- Non-targetable: 3 medium 3-burst turbolasers, 4 light turbolasers, 4 lasers. IDs 16..26; 21 weapon bindings / 27 fog hardpoint bindings / 16 health bindings.
- Shields fail only when both generators are destroyed. HP: weapon 750; shield/engine 1,000; hangar 2,000; tractor 1,500. Shield regeneration 20/s.
- `ImperialBoostEnginePower=21`: speed ×2, weapon delay ×4, shield regeneration ×0; 20 s duration / 50 s recovery.
- `TractorBeam=22`: enemy speed ×0.25; 25 s recovery. Targets only `TractorBeamSettings.targetClasses` (default Corvette, Frigate); ineligible clicks are rejected, caster does not fly to them. Stops on beam/caster/target loss, target removal, ion stun or leaving planar range; modifier removed on stop.
- Advanced bays: TIEInterceptor 203 / TIEBrute 204 / TIEPunisher 205; each 2 total launches and 1 active squadron. Initial/shared interval 4/30 s.
- Squadron counts 8/6/4; member hull/shields 15/0, 50/0, 55/30; Punisher regeneration 0.15/s. Dedicated models, gameplay/placement prefabs, data, icons and Empire registrations.
- Registrations: Empire roster; ShipsData; data/view maps and existing Addressables groups; HUD/tooltip; reinforcement; wreck; matchups; abilities/audio; ShipIconGenerator.
- Team-color fix 2026-10-06: neutral hull/turrets, four mirrored foredeck stripes per side shared with ISD II. Fitted 540-vertex / 250-triangle mesh preserves source albedo/UVs/normals; avoids repeated markings from tiled hull UVs. Stripe mask strength 1; hull mask/rim strengths 0.
- Stripe renderer follows `BankingBody`; explicit ownership/fog/explosion bindings and matching wreck renderer/filter pair. Team arrays I/II 90/100; wreck pairs 40/43. Red/blue top/angled ship and wreck renders plus all four saved-prefab checks passed; no import/serialization errors. No combat or automated Unity tests run for this fix.

## Important Values

- Visible hull/effects 102.61 × 68.56 × 180; opaque collider/placement/wreck 100.89 × 53.10 × 180 project units. Root scale 1; bow +Z; up +Y.
- Navigation radius 105; bank ±5°; hull Y −18.816692 .. 34.578710. Hangar exit (0.51, −26.82, −2.82), 8 units below collider.
- Ship cost/build/level/population 22,000 / 440 s / 3 / 8. Fighter cost/build: Interceptor 525/18 s, Brute 600/7 s, Punisher 1,500/50 s; level/population 1/1.
- Provisional: ship limit 3; fighter limit 10; size, turn/range/arcs, fighter movement tuning, light turbolaser damage 10; tractor duration/range 20 s / 150 units. Punisher missiles reuse the project concussion profile.
- 16 conversions verified in Blender; Unity preserves mesh/bone counts and UV presence. Maximum Unity bone displacement 0.000001986 units. Punisher loses 14 confirmed zero-area triangles: 6,172 → 6,158.
- 36 source ALO/DDS hashes unchanged; decoded PNG pixels match. Separate RGB-derived additive coverage avoids opaque glow planes.
- 13 saved prefabs: no missing scripts, broken references or embedded FBX materials. Eight palettes rendered; blue/green, icons, hologram and fresh wreck inspected. Final Console error count 0; scripts compile and assets persist; MainMenuScene remains clean.

## Edge Cases

- ALAMO global shadow cleanup can alter unrelated scenes → convert in an isolated Blender process, not just a separate scene.
- Texture imports can reload ModelImporter → apply every material remap together after texture reimports.
- Nested turret FBXs contain 100× / −90° X transport → cancel that basis before attachment. Keep disabled helpers and rig hierarchy.
- Collider/preview/wreck use opaque geometry; additive planes stay out of hologram and wreck geometry.
- No automated tests or Play Mode verification.

## Files

- `Tools/Blender/ISDI/README.md` — source, exact muzzle mapping, conversion workflow, tuning and verification.
- `Tools/Blender/prepare_isd_i.py`, `export_isd_i.py` — source-specific conversion.
- `Assets/Prefabs/Models/Ships/ISDIShipView.prefab`, `Assets/Settings/Data/Ship/ISDIShipData.asset` — gameplay.
- `Temp/ISDIImport/` — ignored packed sources, conversion/Unity reports and previews.

## TODO

- Runtime acceptance: firing arcs, hangar launches/replacements, boost/tractor cleanup, shields, fog/selection, reinforcement and wreck/death behavior.
- Balance acceptance for provisional project values. Source animations and animated shader effects were not recreated.
