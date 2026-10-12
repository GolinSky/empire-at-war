# Liberator Cruiser

- Source: REMAKE 4.0 Workshop `1770851727`, `Data/Art/Models/Rebel_Liberator.ALO`; XML `Liberator_Carrier_R` resolves fifteen attachments, including engine glow and fourteen turret models.
- Textures: `Data/Art/Textures/` in that Workshop. The supplied `Texturesc` path is a typo; no folder with that name exists.
- Balance: vault `GameDesign/Rebel units.md`, Liberator Cruiser section. Workshop XML provides art and attachment positions, not project balance.
- Ship enum `Liberator=321`; ability enum `LiberatorRapidDeployment=51`. Existing IDs remain unchanged.

## Gameplay

- Hull `4,800`; shields `4,200`; speed `25`; cost `4,200`; build time `48 s`.
- Twelve weapons: `4× HeavyTurboLaser (2)`, `2× DualMediumTurboLaser (5)`, `4× MediumIonCannon (32)`, `2× PointDefense (14)`.
- Eighteen targetable hardpoints: twelve weapons, two engines, one shield generator, two hangars, one tractor beam. Every type has a health entry.
- All eight source DTL turrets remain visible. DTL `01–04` → heavy weapons; `05–06` → medium dual weapons; `07–08` → retained source art. DIC → medium ions; DLC → point defense. Muzzles come from each attached model.
- Two engines derive from `HP_Engines` with symmetric offsets; hangars use `HP_Spawn_00/01`; shield uses `Shield`; tractor uses `HP_Facingboi`. Exact world positions: `HardpointMapping.json`.
- X-Wing only: port bay `4` total launches / `2` active; starboard bay `2` total launches / `1` active. Three initial active squadrons leave three replacement squadrons.
- Rapid Deployment: up to two available reserve squadrons, `45 s` recovery; active cap and destroyed bays remain enforced. Launches occupy successive available frame slots, preserving the shared one-squadron-per-frame budget.
- Zero-duration phased abilities wait for `IsComplete`; timed phased abilities retain their duration limit.
- The tractor subsystem enables the existing `TractorBeam` ability.

## Project Choices

- Visible bounds `102.137 × 24.481 × 160` project units; root scale `1`; bow `+Z`; navigation radius `95`.
- Banking `±5°`; vertical envelope `-16.545 .. 12.267` units. Launch points sit `8` units below the visible hull.
- Provisional values not specified by the balance note: level `3`, population `3`, maximum `3`; initial launch delay `4 s`, launch interval `8 s`; shield regeneration `28` per `3 s`; weapon range `600`; subsystem health `400/600/600/700/350` for weapon/engine/shield/hangar/tractor.
- Source red paint uses the existing hue livery path; neutral surfaces retain source colors with the shared team rim. Eight team palettes are rendered for ship and wreck.

## Files

- Model art: `Assets/Art/Models/RebellionShips/Liberator/`; materials/textures use corresponding type-first paths.
- Prefabs: `Assets/Prefabs/Models/Ships/Rebellion/Liberator.prefab`, `LiberatorShipView.prefab`, `Assets/Prefabs/Models/Wrecks/LiberatorWreckView.prefab`, `Assets/Prefabs/Ui/Reinforcement/LiberatorReinforcementView.prefab`.
- Data: `Assets/Settings/Data/Ship/Rebellion/LiberatorShipData.asset`, `Ship/Wreck/LiberatorWreckData.asset`; icon/silhouette and matchups use established locations.
- Editable `.blend` files and detailed evidence: `output/remake-rebel-units/Liberator-Converted/`. Source images are packed in each `.blend`; source ALO/DDS hashes remain unchanged.
- Working evidence: `Temp/LiberatorImport/`; durable summary: `ImportEvidence.json`.

## Verification

- Sixteen converted FBXs: geometry, UVs, bone hierarchy and positions verified against Blender imports; maximum FBX geometry error below `0.000067` source units.
- Unity: `115` source meshes, `80,101` triangles; maximum attachment error `0.0000021` project units.
- Unity removes sixteen verified zero-area triangles from `rebel_armor_neutral_2`; all other mesh triangle counts match. No visible source triangles were removed.
- Saved prefab references, material slots, targetable weapons/subsystems, hangars, launch clearance, hull stats, icons, roster, placement and Addressables pass `Inspect.cs`.
- Hangar model checks cover delayed normal launch, immediate multi-slot launch, capacity, replacements and destruction shutdown.
- Icon, contrasting palettes, placement and wreck visually inspected. Automated Unity tests and Play Mode were not run.

## Rebuild

1. Start the isolated Blender helper with `Start.py` on port `9893`; the shared Blender remains available.
2. `uv run --with pillow python Tools/Blender/Liberator/Prepare.py`, then `Convert.py`, then `Stage.py`.
3. Unity order: `BuildArt.cs` → `BuildGameplay.cs` → `Persist.cs` → `Render.cs` → `Register.cs` → `Inspect.cs`.
4. Use `unity command run_script --file Tools/Blender/Liberator/<File>.cs --timeout_ms 60000 --timeout 75 --json`; check nested `result.success`.
5. Run `uv run --with pillow python Tools/Blender/Liberator/Archive.py` after successful verification.
