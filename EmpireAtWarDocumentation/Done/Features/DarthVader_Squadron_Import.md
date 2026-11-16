---
category: Features
status: done
completed: 2026-11-16
acceptance: pending-user
---
# Darth Vader Squadron Import

## Goal
- Empire hero: active Remake TIE Advanced + six existing TIE Fighter escorts.

## Decision
- Source: Workshop `1770851727/Data/Art/Models/Empire_TIE_Advanced.ALO`; converted through Blender MCP with source geometry, UV, normals, attachments and texture evidence.
- Vader 3200 hull / 2400 shields / 400 raw laser DPS. Six escorts each 58 hull / 0 shields / 21 DPS. Total 3548 hull / 2400 shields / 526 DPS.
- Other squadron maxima at verification: 3000 hull, 2220 shields. Republic Anakin 365 DPS; slowest Jedi fighter cruise/combat 62/70. Vader speed 58/66.
- HeroStrikecraft armor; leader protected until escorts lost. Replenish Wingmen replaces only dead escorts, requires live Vader, no healing survivors, 60 s cooldown; AI combat trigger with missing-escort readiness.
- Provisional economy: level 4, price 5000, build 35 s, population 2, maximum one; existing TIE audio set.
- Paired source `MuzzleB_00/01` selected for project weapons; Remake XML uses shared `HP_01`. Final model length 6 units; multiply imported root scale during normalization.

## Implementation
- [x] Source conversion/material/attachment evidence; packed Blender file and five-model comparison preview preserved in Temp.
- [x] Mixed leader/escort health, totals, regeneration, target generation, view/flight revival, ability and meaningful tests.
- [x] Gameplay/visual/hologram/icon assets, 14 mounts, Empire faction/UI/audio/reinforcement/mapping and existing Addressables groups.
- [x] Saved asset readback confirms seven fighters, correct mounts/stats, zero missing materials; formation and leader rendered and inspected. Unity imports/build scripts compile; no new import/serialization errors observed.
- [x] Reviewed/updated DarthVaderSquadronTests, SquadronHealthModelTests, FighterFlightTests, SquadronTargetSelectorTests, WeaponLoadoutBakeTests, BattleBindingTests and BalanceInventoryTests; reviewed WeaponTaxonomyTests and HealthOverlayAnchorTests contracts. Tests not run under checkout restriction.
- [x] Reusable import scale and mixed-health/revival guidance extracted to [[AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE]].

## Files
- `Tools/Blender/DarthVader/README.md`, `BuildArt.cs`, `Integrate.cs`, `Inspect.cs`, `Evidence/`.
- `Assets/Settings/Data/Squadron/DarthVaderTIEAdvancedSquadronData.asset`.
- `Assets/Prefabs/Models/Squadrons/DarthVaderTIEAdvanced{,SquadronView}.prefab` and reinforcement preview.
- `Temp/VaderImport/Previews/Readback.json`, `Formation.png`, `Leader.png`.

## TODO
- User acceptance: in-battle deployment, appearance, combat balance, escort loss/replenishment/cooldown and multiplayer/save restoration.
- Automated suites remain unrun: Squadrons, Abilities, Combat, Balance, Simulation and affected generic asset contracts; run only on explicit request.
