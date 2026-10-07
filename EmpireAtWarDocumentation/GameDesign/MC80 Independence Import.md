---
type: reference
updated: 2026-10-07
tags:
  - alo
  - rebellion
  - ship-import
---
# MC80 Independence Import

## Decision

- Rebellion ship `MC80Independence = 305`; requested hull/shields/speed `30,000 / 40,000 / 17`.
- Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. Requested `Temp/ALO_MODEL_MAP.txt` was absent; installed catalog and AOTR XML resolve `R_MC80_Independence` → `RV_MC80_Independence.ALO`.
- Source: installed Awakening of the Rebellion, Workshop `1397421866`. Preserve original ALO/DDS files and supplied credits; individual model authors remain unresolved.
- User-approved fighters: existing X-Wings, Y-Wings and A-Wings; one type per targetable hangar. B-Wings/U-Wings are excluded.

## Implementation

- Targetable: eight medium dual turbolasers, six medium dual turbo-ion cannons, three engines and three hangars; 20 unique targets.
- Non-targetable: eight light turbolasers, six light turbo-ion cannons and eight heavy laser cannons; 22 mounts. Total weapons: 36.
- Medium turbo-ions use `HP_01/02/06/08/07/09_FP_01`; medium turbolasers use `HP_05/03/04/11/12/13/14/15_FP_01`. Light counterparts use `FP_02`.
- Heavy lasers use `HP_04/03/06/08/10/12/14/15_FP_01`; hangars use `HP_HANGAR_01..03`; engines use `HP_Engines_M/L/R`.
- Power to Shields `26`: shield regeneration ×8, speed ×0.8, weapon delays ×2, active 30 s; recovery 60 s after expiry. Source regeneration amount ×4 / interval ×0.5 maps to ×8.
- Hangar reserves: three total launches per type, one active squadron each; initial/shared launch delays 4/15 s. Destroying a mapped hangar disables only its bay.
- Shared unmapped carriers stop launching after all targetable hangars are destroyed. Engines apply surviving-engine fraction to speed coefficient 1 → 0.4.
- Team paint: paired tapered longitudinal stripes, aft transverse band and bow accent. Four 2048px linear UV masks use strength 1 on eight live/wreck hull material pairs; HSV livery stays disabled, team rim 0.6 retained. Original albedo/normal maps, geometry and UVs are unchanged; eight palettes and blue/red/green top views verified.
- Stripe evidence: `TeamStripeReport.json`; author/inspect through `PaintTeamStripes.cs` / `InspectTeamStripes.cs`. All eight ownership renderers and matching wreck masks persist in saved assets.

## Important Values

- Source shield regeneration `80/s`; weapon/system health `1,500/4,000/1,100` for weapons/engines/hangars.
- Source economy `34,000 credits / 680 s / population 14 / cap 3`; project tech 3.
- Gameplay length 220 units; visual bounds `53.8071 × 31.6089 × 220`; root scale 1; navigation radius 120.
- Continuous ±5° bank envelope `−15.80457 .. 15.80529`; all three launch points clear the underside collider by 8 units.
- Profiles `55/56/57`: medium dual turbolaser / medium dual turbo-ion / light turbo-ion. Source dual damage is split into two shots.
- Existing light turbolaser `33` and heavy laser `28` reused; light turbolaser tuning differs from AOTR. Economy, movement, size, weapon ranges and matchups remain provisional.
- Source tactical population override 62, weapon energy, passive repair and death ALA are not ported.

## Files

- Source ALO: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/RV_MC80_Independence.ALO`.
- Tooling: `Tools/Blender/MC80Independence/`; isolated Blender 3.6.23 MCP port `9883`, canonical configuration otherwise unchanged.
- Art: `Assets/Art/Models/RebellionShips/MC80Independence/`; materials/textures in matching type-first folders.
- Gameplay: `Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab`; data: `Assets/Settings/Data/Ship/MC80IndependenceShipData.asset`.
- Separate shield, wreck, placement, transparent icon/silhouette and matchup assets use `MC80Independence` names.
- Editable conversion, textures, previews, reports, scripts and supplied credits: `output/aotr-rebel-units/MC80Independence-Converted/`.
- Acceptance plan: [[TODOs/Features/MC80Independence_Import]].

## Rules

- Source/FBX retain all 47 meshes, 137,320 triangles, 214 bones and UVs. Nine authored helper meshes are stripped only from game prefabs.
- Wreck uses all eight opaque hull renderers via `ShipWreckBuilder`; refresh its wreck-data component reference after rebuilding.
- 2026-10-07 surface fix: ALAMO cleared three of `Hull`'s four material slots and reused a fixed `Material1` suffix, allowing additive light materials to overwrite opaque meshes `1`/`3`. Restore all original slots and material definitions from the binary ALO; keep stable Unity GUIDs.
- `Hull` slot triangles: `2,519 / 1,895 / 1,829 / 1,332`; restored opaque meshes `1`/`3`: `9,023 / 6,757`. All 38 visible meshes match original per-slot texture/shader/triangle assignments; Repeat sampling, UV-range error 0.
- Twenty related ALOs audited: complete living Independence = 47 meshes, `_DC` death clone = 26; no replacement variant is needed. Only the nine verified helpers remain excluded; aft no-glow render confirms solid engine nozzles.
- Geometry round trip: Blender corner/bone error ≤0.000160807/0.000146389 source units; Unity bone error ≤0.000002950 units; no triangle/parent mismatch.
- Saved-prefab inspection verifies unique IDs, authored attachment positions, references and every registration. Original 22 source hashes stay unchanged.
- No automated Unity tests or combat Play Mode started by this import. Coordinated Executor Editor regression passed all 11 relevant hangar/engine/MC80 wreck cases; project total 565/567, both failures from Acclamator. Evidence: `CoordinatedEditorVerification.json`. The coordinated test report predates the surface fix; no automated tests were rerun for that fix. Updated source/material/submesh/UV, saved-reference, wreck and render inspections passed. Combat acceptance remains unverified.
