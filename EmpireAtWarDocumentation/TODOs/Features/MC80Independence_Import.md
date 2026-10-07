---
category: Features
status: in-progress
updated: 2026-10-07
---
# MC80 Independence Import

## Goal

- Add the requested MC80 Independence to Rebellion with verified source geometry, stats, weapons, systems, ability and approved fighters.
- Keep source assets unchanged; preserve existing GUIDs, Addressables structure and unrelated imports.

## Implementation

- [x] Read [[Architecture/ALO_MODEL_IMPORT_GUIDE]], placement/UI rules and source mapping; document missing `Temp/ALO_MODEL_MAP.txt` and verified catalog/XML fallback.
- [x] Convert AOTR `RV_MC80_Independence.ALO` through isolated Blender MCP; verify geometry, UVs, 214 bones and unchanged source hashes.
- [x] Create art/gameplay/data/shield/placement/wreck/icons and register Rebellion ship `305`.
- [x] Configure supplied `30,000 / 40,000 / 17`, 20 targetable hardpoints, 22 non-targetable weapons and Power to Shields.
- [x] Configure approved X-Wing/Y-Wing/A-Wing bays and independent hangar/engine destruction; preserve legacy carrier behavior.
- [x] Correct helper/wreck assets: nine authored helpers absent from all four saved prefabs; eight wreck hull meshes, transforms and shared material properties match the live view; wreck-data reference, all registrations and saved references verified.
- [x] Correct user-reported albedo/engine disappearance: restored four `Hull` slots and opaque meshes `1`/`3` from original binary material records; fixed shared-texture shader collisions, retained geometry/UVs, verified 20 ALO variants and all 38 visible meshes. Eight opaque meshes now bind to live/preview/wreck/team/explosion views; 1024px hull/aft/no-glow renders inspected. No automated tests rerun for this fix.
- [x] Add requested team-colored hull stripes: paired longitudinal paint, aft band and bow accent; four linear 2048px UV masks, eight live/wreck material pairs and eight ownership bindings verified. Blue/red/green top views and eight live/wreck palettes inspected; geometry, UVs and registrations remain verified. No automated tests started for this art change.
- [ ] Clean-skirmish acceptance: construction, placement, combat, shield power, bay destruction/replacement, engine damage, death/wreck and teardown.
- [ ] Review provisional economy, weapon damage/range, movement, flight height and matchup balance.

## Important Values

- Power to Shields: shield regeneration ×8, speed ×0.8, weapon delays ×2; active/recovery 30/60 s.
- Hangars: three total launches per type, one active each; initial/shared delay 4/15 s.
- Geometry: 47 source meshes, 137,320 triangles, 214 bones; gameplay length 220 units.
- Shared code: `HangarComponent`, `HangarModel`, `Ship`; two bay-disabling regression tests added. No test command or combat Play Mode started by this import.
- Coordinated Executor Editor regression after correction: 2026-10-07 all 11 relevant checks passed (seven hangar, two multi-engine and two MC80 wreck cases). Project total 565/567; both remaining failures belong to Acclamator. Evidence: `CoordinatedEditorVerification.json`, full source report `Temp/ExecutorImport/FinalEditorTests.json`.

## Files

- Reference: [[GameDesign/MC80 Independence Import]].
- Tooling: `Tools/Blender/MC80Independence/`.
- Evidence/editable source: `output/aotr-rebel-units/MC80Independence-Converted/`.
- Gameplay/data: `Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab`, `Assets/Settings/Data/Ship/MC80IndependenceShipData.asset`.
